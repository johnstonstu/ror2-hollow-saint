using System.Collections.Generic;
using HollowSaint.FoundationKit.ArcStep;
using HollowSaint.FoundationKit.SpearDischarge;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>
    /// Lives on the body prefab, so it exists on every machine. Watches the replicated
    /// Open Circuit buff: every machine plays the hold/end animation from the buff edges,
    /// and only the server deals the pulses (which build Static like any other hit).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OpenCircuitPulseDriver : MonoBehaviour
    {
        private CharacterBody body;
        private bool wasOpen;
        private readonly CircuitPulseCadence cadence = new CircuitPulseCadence();
        private CircuitDwellDriver dwell;

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            dwell = GetComponent<CircuitDwellDriver>() ?? gameObject.AddComponent<CircuitDwellDriver>();
        }

        private void OnDestroy()
        {
            if (wasOpen && body) Util.PlaySound(Vfx.KitSfx.CrownLoopStop, body.gameObject);
        }

        private void OnDisable()
        {
            if (wasOpen && body) Util.PlaySound(Vfx.KitSfx.CrownLoopStop, body.gameObject);
            wasOpen = false;
        }

        // ---- v0.8 crown state ----
        // The Halo layer is a small state machine in the controller (Empty -> Open Circuit ->
        // Open Circuit hold -> Open Circuit end -> Empty) driven by one bool, crownOpen. It is
        // true while the crown buff is up OR the cast is still running, so a cast interrupted
        // before its buff closes the ring, a recast during the crown simply stays in the hold,
        // and the close always plays to the rest pose. No code plays Halo states directly.
        private static readonly int CrownOpenHash = Animator.StringToHash("crownOpen");
        private EntityStateMachine crownMachine;
        private Animator crownAnimator;
        private bool hasCrownParam, warnedNoParam;
        private float looseRingTime;
        private bool looseRingLogged;

        /// <summary>True while the crown should be open: buff up, or the cast state running.</summary>
        public bool CrownOpen { get; private set; }

        private bool CastRunning()
        {
            if (!crownMachine) crownMachine = EntityStateMachine.FindByCustomName(body.gameObject, KitRegistration.CrownMachineName);
            return crownMachine && crownMachine.state is OpenCircuitState;
        }

        private void Update()
        {
            if (body == null) return;
            bool alive = body.healthComponent && body.healthComponent.alive;
            CrownOpen = alive && ((OpenCircuitBuff.Def && body.HasBuff(OpenCircuitBuff.Def)) || CastRunning());
            var animator = KitAnim.AnimatorOf(body);
            if (animator != crownAnimator)
            {
                crownAnimator = animator;
                hasCrownParam = false;
                if (animator)
                    foreach (var p in animator.parameters)
                        if (p.nameHash == CrownOpenHash && p.type == AnimatorControllerParameterType.Bool) hasCrownParam = true;
                if (animator && !hasCrownParam && !warnedNoParam)
                {
                    warnedNoParam = true;
                    Plugin.Log.LogError("HOLLOW_SAINT_CROWN controller lacks crownOpen; bundle14 is required with this DLL");
                }
            }
            if (hasCrownParam) animator.SetBool(CrownOpenHash, CrownOpen);
            WatchRing();
        }

        /// <summary>Diagnostic only: a ring left open without a crown is the failure Stu saw;
        /// report it once per occurrence so playtest logs show whether it ever happens.</summary>
        private void WatchRing()
        {
            var ring = Vfx.HaloRing.For(body);
            bool loose = !CrownOpen && ring && ring.Valid && ring.Openness > 0.35f;
            looseRingTime = loose ? looseRingTime + Time.deltaTime : 0f;
            if (!loose) { looseRingLogged = false; return; }
            if (looseRingTime > 1.5f && !looseRingLogged)
            {
                looseRingLogged = true;
                Plugin.Log.LogWarning("HOLLOW_SAINT_DIAG ring_open_without_crown openness=" + ring.Openness.ToString("0.00"));
            }
        }

        private float armsRestTime;

        /// <summary>bundle06: keeps "Open Circuit arms hold" up for the buff window. The cast chains
        /// into it in the controller; if another gesture (an Arc Bolt) takes the arm layers, the hold
        /// comes back once they have rested briefly. No-op on controllers without the hold state.</summary>
        private void MaintainArmsHold()
        {
            var animator = KitAnim.AnimatorOf(body);
            if (animator == null || !KitAnim.HasState(animator, KitAnim.UpperBodyLayer, OpenCircuitTuning.HoldArmsState)) return;
            if (!KitAnim.UpperBodyIdle(body)) { armsRestTime = 0f; return; }
            armsRestTime += Time.fixedDeltaTime;
            if (armsRestTime < OpenCircuitTuning.HoldArmsResumeDelay) return;
            armsRestTime = 0f;
            KitAnim.PlayGesture(body, animator, OpenCircuitTuning.HoldArmsState, OpenCircuitTuning.HoldLoopSeconds);
        }

        private void HoldCooldown()
        {
            var locator = body.skillLocator;
            if (!locator) return;
            foreach (var skill in new[] { locator.special, locator.secondary, locator.utility, locator.primary })
                if (skill && skill.skillDef == OpenCircuitRegistration.SkillDef && skill.stock < skill.maxStock)
                    skill.rechargeStopwatch = 0f;
        }

        private void FixedUpdate()
        {
            if (body == null || OpenCircuitBuff.Def == null) return;

            bool open = body.healthComponent && body.healthComponent.alive && body.HasBuff(OpenCircuitBuff.Def);
            if (open != wasOpen)
            {
                wasOpen = open;
                cadence.Reset();
                if (open)
                {
                    Vfx.KitFx.Local(Vfx.Beat.CircuitOpen, body, body.corePosition);
                    Util.PlaySound(Vfx.KitSfx.CrownLoopStart, body.gameObject);
                }
                else
                {
                    // The Halo layer closes itself from crownOpen; only the arms need the end gesture.
                    CrownGestureFlow.Recover(body);
                    Util.PlaySound(Vfx.KitSfx.CrownLoopStop, body.gameObject);
                    Vfx.KitFx.Local(Vfx.Beat.CircuitClose, body, Vfx.HaloRing.CenterOf(body));
                    if (body.healthComponent && body.healthComponent.alive) OpenCircuitVfxHooks.RaiseRecall(body);
                }
            }

            if (open) MaintainArmsHold();
            // v0.9.15: the cooldown starts when the crown closes, not on the cast, so cooldown items
            // shorten the wait between crowns instead of making the crown permanent.
            if (open && body.hasEffectiveAuthority && OpenCircuitTuning.CooldownAfterCrown) HoldCooldown();
            if (!open || !NetworkServer.active) return;
            if (!OpenCircuitTuning.AllowPulsesDuringGlideAndArcStep && ArcStepState.IsBodyDashing(body)) return;

            float baseline = CircuitChargePolicy.Interval(KitTuning.OpenCircuitPulseInterval, 1);
            float interval = CircuitChargePolicy.Interval(baseline, OpenCircuitBuff.Charges(body));
            if (cadence.Tick(Time.fixedDeltaTime, interval, baseline, OpenCircuitTuning.FirstPulseIsImmediate, out bool fundsStatic)) Pulse(fundsStatic);
        }

        private void Pulse(bool fundsStatic)
        {
            if (body.healthComponent == null || !body.healthComponent.alive) return;
            float damage = KitDamagePolicy.Effective(KitTuning.OpenCircuitPulseDamageCoefficient) * body.damage;
            if (!fundsStatic) Storm.StormServer.BeginStormDamage();
            BlastAttack.Result result;
            try { result = new BlastAttack
            {
                attacker = body.gameObject,
                inflictor = body.gameObject,
                teamIndex = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.None,
                attackerFiltering = AttackerFiltering.NeverHitSelf,
                position = body.corePosition,
                radius = KitTuning.OpenCircuitRadius,
                falloffModel = BlastAttack.FalloffModel.None,
                baseDamage = damage,
                baseForce = 0f,
                crit = false,
                damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                damageColorIndex = DamageColorIndex.Nearby,
                procCoefficient = 0f, // pulses do not proc items; StormServer still builds Static from them (Open Circuit weight)
                losType = BlastAttack.LoSType.None
            }.Fire(); }
            finally { if (!fundsStatic) Storm.StormServer.EndStormDamage(); }
            // Empowered pulses never grow baseline Static, but they do finish primed enemies:
            // Circuit is the kit's close-range finisher for what Orb, cloud and Gaze prime.
            if (!fundsStatic && ChargedStorm.ChargedStormTuning.CircuitFeedsPrimed && result.hitPoints != null)
                for (int i = 0; i < result.hitCount && i < result.hitPoints.Length; i++)
                {
                    var box = result.hitPoints[i].hurtBox;
                    if (!box || !box.healthComponent) continue;
                    // A primed enemy killed by a storm-scoped pulse still discharges like an ordinary kill.
                    if (!box.healthComponent.alive) Storm.StormServer.FinishPrimedDeath(box.healthComponent, body);
                    else Storm.StormServer.FeedPrimed(box.healthComponent, body, damage, KitTuning.StaticOpenCircuitWeight);
                }

            var points = new List<Vector3>();
            if (result.hitPoints != null)
                for (int i = 0; i < result.hitCount && i < result.hitPoints.Length; i++)
                {
                    points.Add(result.hitPoints[i].hitPosition);
                    if (dwell) dwell.Confirm(result.hitPoints[i].hurtBox);
                }
            OpenCircuitVfxHooks.RaisePulse(body, KitUtil.EyePosition(body), points.ToArray(), damage);
            Vfx.KitFx.Server(Vfx.Beat.CircuitPulse, body.footPosition, default(Vector3), KitTuning.OpenCircuitRadius, sound: true, owner: body);
            // Nearest first, so the six arcs go to the closest enemies; staggered so they read
            // as the crown lashing out one after another rather than a single flash. Each
            // tendril leaves the ring point nearest its target; clients recompute that point
            // from their own live ring (the beat carries the owner), this is the fallback.
            var ring = Vfx.HaloRing.For(body);
            if (ring) ring.EnsureFitted();
            bool hasRing = ring && ring.Valid;
            Vector3 haloPos = hasRing ? ring.Shape.Center : Vfx.KitFx.Socket(body, "Halo");
            Vector3 self = body.corePosition;
            points.Sort((a, b) => (a - self).sqrMagnitude.CompareTo((b - self).sqrMagnitude));
            for (int i = 0; i < points.Count && i < CircuitChargePolicy.Arcs(OpenCircuitBuff.Charges(body)); i++)
            {
                Vector3 from = hasRing ? ring.Shape.Nearest(points[i]) : haloPos;
                Vfx.KitFx.Server(Vfx.Beat.CircuitArc, points[i], from, 1f, sound: false, delay: i * 0.04f, owner: body);
            }
            KitLog.Event("OPEN_CIRCUIT_PULSE", "hits=" + points.Count + " charges=" + OpenCircuitBuff.Charges(body) + " fundsStatic=" + fundsStatic);
        }
    }
}
