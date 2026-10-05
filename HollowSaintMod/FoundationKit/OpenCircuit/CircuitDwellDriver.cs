using System.Collections.Generic;
using HollowSaint.FoundationKit.ArcStep;
using HollowSaint.FoundationKit.Storm;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>At most 64 distinct victims per crown, admitted only by actual pulse hits.
    /// Fixed-time dwell does not scale with attack speed or add a sustained multiplier.</summary>
    [DisallowMultipleComponent]
    internal sealed class CircuitDwellDriver : MonoBehaviour
    {
        private sealed class Victim
        {
            internal HurtBox box;
            internal HealthComponent health;
            internal CircuitDwellPolicy dwell;
            internal int tier;
        }
        private readonly List<Victim> victims = new List<Victim>(CircuitDwellPolicy.Capacity);
        private CharacterBody body;
        private OpenCircuitPulseDriver pulseDriver;
        private Stage stage;
        private void Awake() { body = GetComponent<CharacterBody>(); pulseDriver = GetComponent<OpenCircuitPulseDriver>(); stage = Stage.instance; }
        internal void Confirm(HurtBox box)
        {
            if (!NetworkServer.active || !box || !box.healthComponent || !box.healthComponent.alive) return;
            for (int i = 0; i < victims.Count; i++)
                if (victims[i].health == box.healthComponent) { victims[i].box = box; return; }
            if (victims.Count < CircuitDwellPolicy.Capacity)
                victims.Add(new Victim { box = box, health = box.healthComponent });
        }
        private bool Inside(Victim victim)
        {
            var health = victim.health;
            var target = health ? health.body : null;
            if (!health || !health.alive || !target || !target.teamComponent || !body.teamComponent ||
                target.teamComponent.teamIndex == body.teamComponent.teamIndex) return false;
            var group = target.hurtBoxGroup;
            if (!group || group.hurtBoxes == null) return false;
            float radius = Mathf.Max(0f, KitTuning.OpenCircuitRadius);
            for (int i = 0; i < group.hurtBoxes.Length && i < 32; i++)
            {
                var box = group.hurtBoxes[i];
                if (!box || box.healthComponent != health || !box.collider || !box.collider.enabled ||
                    !box.gameObject.activeInHierarchy) continue;
                if ((box.collider.ClosestPoint(body.corePosition) - body.corePosition).sqrMagnitude <= radius * radius)
                { victim.box = box; return true; }
            }
            return false;
        }
        private void FixedUpdate()
        {
            if (!NetworkServer.active) return;
            if (stage != Stage.instance) { Clear(); stage = Stage.instance; }
            if (!body || !body.healthComponent || !body.healthComponent.alive || !OpenCircuitBuff.Def ||
                !body.HasBuff(OpenCircuitBuff.Def) || (pulseDriver && !pulseDriver.isActiveAndEnabled)) { Clear(); return; }
            bool paused = !OpenCircuitTuning.AllowPulsesDuringGlideAndArcStep && ArcStepState.IsBodyDashing(body);
            for (int i = 0; i < victims.Count; i++)
            {
                var victim = victims[i];
                bool inside = !paused && Inside(victim);
                bool zap = victim.dwell.Tick(inside, Time.fixedDeltaTime);
                int tier = inside ? Mathf.FloorToInt(victim.dwell.Progress * 8f) : 0;
                if (zap) Zap(victim);
                else if (!victim.dwell.Spent && tier != victim.tier)
                    CircuitDwellHooks.Raise(body, victim.box, victim.dwell.Progress,
                        inside ? CircuitDwellBeat.Progress : CircuitDwellBeat.Reset);
                victim.tier = tier;
            }
        }
        private void Zap(Victim victim)
        {
            float damage = KitDamagePolicy.Effective(CircuitDwellPolicy.RawZapCoefficient) * body.damage;
            var info = StormServer.MakeInfo(body, victim.box, damage, body.RollCrit(), 1f);
            // Normal hit/item callbacks remain. The existing depth guard prevents this
            // new burst and synchronous descendants from self-refuelling Prayer/Static.
            StormServer.BeginStormDamage();
            try { victim.health.TakeDamage(info); KitUtil.ReportHit(info, victim.health.gameObject); }
            finally { StormServer.EndStormDamage(); }
            if (info.rejected) return;
            // EffectManager relays once to host and observers; no additional local path.
            try { Vfx.KitFx.Server(Vfx.Beat.CircuitDwellZap, info.position, owner: body, sound: false); }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CIRCUIT_ZAP_SEND " + error); }
            CircuitDwellHooks.Raise(body, victim.box, 1f, CircuitDwellBeat.Zap);
        }
        private void Clear()
        {
            for (int i = 0; i < victims.Count; i++)
                CircuitDwellHooks.Raise(body, victims[i].box, 0f, CircuitDwellBeat.Reset);
            victims.Clear();
        }
        private void OnDisable() { Clear(); }
        private void OnDestroy() { Clear(); }
    }
}
