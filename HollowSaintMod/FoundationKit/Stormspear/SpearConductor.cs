using HollowSaint.FoundationKit.Storm;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>One reusable, server-only conductor per owner. New enemy impacts replace its window.
    /// Secondary damage has no item callbacks, Static, chaining, refuel or independent critical rolls.</summary>
    [DisallowMultipleComponent]
    internal sealed class SpearConductor : MonoBehaviour
    {
        private readonly Collider[] scratch = new Collider[SpearConductorSchedule.SearchCapacity];
        private readonly HealthComponent[] victims = new HealthComponent[SpearConductorSchedule.VictimsPerTick];
        private readonly HurtBox[] boxes = new HurtBox[SpearConductorSchedule.VictimsPerTick];
        private readonly SpearConductorTargets selection = new SpearConductorTargets();
        private CharacterBody owner;
        private HealthComponent host;
        private Transform anchor;
        private Vector3 localPoint;
        private Stage stage;
        private SpearConductorSchedule schedule;
        private float age;
        private int ticks;
        private bool active, crit;
        private bool hadStage;

        internal static void Begin(CharacterBody owner, HealthComponent host, Transform anchor,
            Vector3 point, bool crit, SpearConductorSchedule snapshot)
        {
            if (!NetworkServer.active || !owner || !owner.isActiveAndEnabled || !owner.healthComponent || !owner.healthComponent.alive ||
                !host || !host.alive || !anchor) return;
            var conductor = owner.GetComponent<SpearConductor>();
            if (!conductor) conductor = owner.gameObject.AddComponent<SpearConductor>();
            if (!conductor.isActiveAndEnabled) return;
            conductor.owner = owner;
            conductor.host = host;
            conductor.anchor = anchor;
            conductor.localPoint = anchor.InverseTransformPoint(point);
            conductor.stage = Stage.instance;
            conductor.hadStage = conductor.stage != null;
            conductor.schedule = snapshot;
            conductor.crit = crit; // reuse the throw's one crit roll
            conductor.age = 0f;
            conductor.ticks = 0;
            conductor.active = true;
        }

        private void FixedUpdate()
        {
            if (!active) return;
            if (!NetworkServer.active || !owner || !owner.isActiveAndEnabled || !owner.healthComponent ||
                !owner.healthComponent.alive || !host || !host.alive || !anchor || stage != Stage.instance || (hadStage && !stage))
            { Clear(); return; }
            age += Time.fixedDeltaTime;
            while (SpearConductorSchedule.Due(ticks, age))
            {
                ticks++;
                Pulse();
                if (!host || !host.alive || !owner.healthComponent.alive) { Clear(); return; }
            }
            if (ticks >= SpearConductorSchedule.TickCount) Clear();
        }

        private void Pulse()
        {
            Vector3 origin = anchor.TransformPoint(localPoint);
            var team = owner.teamComponent ? owner.teamComponent.teamIndex : TeamIndex.None;
            var enemies = TeamMask.GetEnemyTeams(team);
            int found = Physics.OverlapSphereNonAlloc(origin, schedule.Radius, scratch,
                LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
            selection.Clear();
            for (int i = 0; i < found; i++)
            {
                var collider = scratch[i];
                scratch[i] = null;
                var box = collider ? collider.GetComponent<HurtBox>() : null;
                var health = box ? box.healthComponent : null;
                if (!health || !health.alive || health == host || health == owner.healthComponent ||
                    !health.body || !health.body.teamComponent || !enemies.HasTeam(health.body.teamComponent.teamIndex) ||
                    !FriendlyFireManager.ShouldDirectHitProceed(health, team)) continue;
                Vector3 target = collider.bounds.center;
                float distance = (target - origin).sqrMagnitude;
                if (distance > schedule.Radius * schedule.Radius || Physics.Linecast(origin, target,
                    LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) continue;
                int index = selection.Select(health.GetInstanceID(), distance);
                if (index < 0) continue;
                victims[index] = health; boxes[index] = box;
            }
            for (int i = 0; i < selection.Count; i++)
            {
                var health = victims[i];
                var box = boxes[i];
                victims[i] = null; boxes[i] = null;
                if (!health || !health.alive || !box) continue;
                var info = new DamageInfo
                {
                    attacker = owner.gameObject, inflictor = owner.gameObject, damage = schedule.Damage,
                    crit = crit, position = box.collider ? box.collider.bounds.center : box.transform.position,
                    procCoefficient = 0f, force = Vector3.zero, inflictedHurtbox = box,
                    damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Secondary),
                    damageColorIndex = DamageColorIndex.Electrocution
                };
                StormServer.BeginStormDamage();
                try { health.TakeDamage(info); }
                finally { StormServer.EndStormDamage(); }
                if (!info.rejected) Vfx.KitFx.Server(Vfx.Beat.SpearPulseArc, info.position, origin,
                    0.35f, sound: false, owner: owner);
            }
        }

        private void Clear()
        {
            active = false; host = null; anchor = null; owner = null; stage = null;
            System.Array.Clear(victims, 0, victims.Length);
            System.Array.Clear(boxes, 0, boxes.Length);
            System.Array.Clear(scratch, 0, scratch.Length);
        }
        private void OnDisable() { Clear(); }
        private void OnDestroy() { Clear(); }
    }
}
