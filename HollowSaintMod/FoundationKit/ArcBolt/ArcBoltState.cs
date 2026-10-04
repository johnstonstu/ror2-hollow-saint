using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace HollowSaint.FoundationKit.ArcBolt
{
    /// <summary>
    /// Arc Bolt cast. One shot per state; duration = ArcBoltInterval / attackSpeed, so the
    /// hold-to-fire cadence is 0.5 s at 1x and speeds up with attack speed. Hands
    /// alternate. The bolt is fired by the authority through ProjectileManager, which
    /// relays to the server when this runs on a client. Hit resolution, chaining and
    /// Discharge charge happen server-side in ArcBoltProjectileImpact.
    /// </summary>
    public class ArcBoltState : BaseSkillState
    {
        public const string StateRight = "Arc Bolt right";
        public const string StateLeft = "Arc Bolt left";

        // Per-body hand alternation (was one static counter shared by every Saint and machine).
        private static readonly Dictionary<GameObject, int> handCounters = new Dictionary<GameObject, int>();

        private static int NextHand(GameObject body)
        {
            if (handCounters.Count > 16)
            {
                var dead = new List<GameObject>();
                foreach (var kv in handCounters) if (kv.Key == null) dead.Add(kv.Key);
                for (int i = 0; i < dead.Count; i++) handCounters.Remove(dead[i]);
            }
            int n;
            handCounters.TryGetValue(body, out n);
            n = (n + 1) % 2;
            handCounters[body] = n;
            return n;
        }

        private int handCounter;

        private float duration;
        private bool hasFired, guideCast;
        private uint currentToken;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = KitTuning.ArcBoltInterval / attackSpeedStat;
            // While a Stormspear charges in the hand, Arc Bolt keeps firing from the off hand (the right
            // one since v0.9.14, the spear is in the left) at a reduced rate. The alternation counter
            // does not advance while forced.
            bool offHand = Stormspear.StormspearCharge.OffHand(characterBody);
            if (offHand) duration /= Mathf.Max(0.05f, Stormspear.StormspearTuning.OffHandRateMultiplier);
            if (characterBody) characterBody.SetAimTimer(2f);
            if (isAuthority) AddRecoil(-0.35f, -0.7f, -0.25f, 0.25f); // v0.8: light per-bolt kick
            handCounter = offHand ? (SpearDischarge.SpearCarry.SpearInLeft(characterBody) ? 0 : 1) : NextHand(gameObject);
            currentToken = Vfx.BodyCurrentFx.BeginArm(characterBody, handCounter != 0,
                duration, KitTuning.ArcBoltReleaseNormalizedTime);
            // bundle06: arms-only layer while moving/airborne, UpperBody when standing.
            // v0.9.8: while a hand spear charges, the off hand is already held out in front (SpearCarry's
            // guide arm) and the bolt just leaves it with a recoil kick, so no arm gesture plays.
            guideCast = offHand && SpearDischarge.SpearCarry.HoldingFor(characterBody);
            if (!guideCast)
                // v0.9.10: the gesture never plays faster than ArcBoltMinGestureSeconds. At 3.6x attack
                // speed the 0.14 s clip snapped each hand 20 cm in a frame; now the next bolt's gesture
                // crossfades in over the tail of the previous one instead.
                KitAnim.PlayGesture(characterBody, GetModelAnimator(),
                    handCounter == 0 ? StateRight : StateLeft, Mathf.Max(duration, KitTuning.ArcBoltMinGestureSeconds));
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!hasFired && fixedAge >= duration * KitTuning.ArcBoltReleaseNormalizedTime)
            {
                hasFired = true;
                Vfx.BodyCurrentFx.ReleaseArm(characterBody, handCounter != 0, currentToken);
                if (guideCast) SpearDischarge.SpearCarry.OffHandShot(characterBody);
                CastFx();
                if (isAuthority) Fire();
            }
            if (isAuthority && fixedAge >= duration)
            {
                outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            if (!hasFired) Vfx.BodyCurrentFx.CancelArm(characterBody, handCounter != 0, currentToken);
            base.OnExit();
        }

        private void CastFx()
        {
            string muzzle = handCounter == 0 ? "MuzzleRight" : "MuzzleLeft";
            Vfx.KitFx.Local(Vfx.Beat.ArcBoltCast, characterBody, Vfx.KitFx.Socket(characterBody, muzzle));
            Vfx.DischargeLink.Play(characterBody, handCounter != 0, ReleaseRay().direction, KitTuning.ArcBoltProjectileSpeed);
        }

        private void Fire()
        {
            var prefab = ArcBoltProjectile.Prefab;
            if (prefab == null || ProjectileManager.instance == null) return;
            KitLog.Event("ARC_BOLT_FIRED", "attackSpeed=" + attackSpeedStat.ToString("0.00"));
            Ray launch = ReleaseRay();
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = prefab,
                position = launch.origin,
                rotation = Util.QuaternionSafeLookRotation(launch.direction),
                owner = gameObject,
                damage = KitDamagePolicy.Effective(KitTuning.ArcBoltDamageCoefficient) * damageStat,
                force = 0f,
                crit = RollCrit(),
                damageColorIndex = DamageColorIndex.Default,
                damageTypeOverride = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Primary)
            });
        }
        private Ray ReleaseRay()
        {
            Ray aim = GetAimRay();
            // Launch from the casting hand, aimed at whatever the crosshair is on, so the bolt
            // visibly leaves the fingertips but still lands where the player is aiming.
            string muzzle = handCounter == 0 ? "MuzzleRight" : "MuzzleLeft";
            Vector3 hand = Vfx.KitFx.Socket(characterBody, muzzle);
            Vector3 target = aim.origin + aim.direction * 1000f;
            RaycastHit hit;
            if (Util.CharacterRaycast(gameObject, aim, out hit, 1000f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                target = hit.point;
            Vector3 origin = (hand - aim.origin).sqrMagnitude < 9f ? hand : aim.origin;
            Vector3 dir = target - origin;
            if (dir.sqrMagnitude < 0.01f || Vector3.Dot(dir, aim.direction) <= 0f) dir = aim.direction;
            return new Ray(origin, dir.normalized);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            // Other kit skills (PrioritySkill) can cut a bolt at any time; the next bolt
            // (priority Any) waits for this one to finish.
            return InterruptPriority.Skill;
        }
    }
}
