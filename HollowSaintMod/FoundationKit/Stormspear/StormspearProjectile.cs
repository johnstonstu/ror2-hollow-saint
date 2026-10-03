using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.Storm;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>
    /// Builds the Stormspear projectile prefab: a clone of the Mage lightning bolt with the vanilla
    /// impact and explosion stripped, replaced by StormspearImpact. No anchor, no planting; a
    /// short lifetime backstop. Fired by the casting authority through ProjectileManager.
    /// </summary>
    public static class StormspearProjectile
    {
        public const string TemplateLegacyPath = "Prefabs/Projectiles/MageLightningboltBasic";
        public const string PrefabName = "HollowSaintStormspearProjectile";
        public const float Radius = 0.35f;
        public const float LifetimeSeconds = 3f;

        public static GameObject Prefab { get; private set; }

        public static GameObject EnsurePrefab()
        {
            if (Prefab != null) return Prefab;

            GameObject template;
            try { template = LegacyResourcesAPI.Load<GameObject>(TemplateLegacyPath); }
            catch (System.Exception error)
            {
                Plugin.Log.LogError("Stormspear: failed to load projectile template '" + TemplateLegacyPath + "': " + error);
                return null;
            }
            if (template == null)
            {
                Plugin.Log.LogError("Stormspear: projectile template '" + TemplateLegacyPath + "' not found.");
                return null;
            }
            var clone = PrefabAPI.InstantiateClone(template, PrefabName, true);
            if (clone == null) { Plugin.Log.LogError("Stormspear: InstantiateClone returned null."); return null; }

            // ProjectileController dispatches to every IProjectileImpactBehavior, so the vanilla
            // single-target impact must go or damage would apply twice.
            var vanillaImpact = clone.GetComponent<ProjectileSingleTargetImpact>();
            if (vanillaImpact != null) Object.DestroyImmediate(vanillaImpact);
            var explosion = clone.GetComponent<ProjectileExplosion>();
            if (explosion != null) Object.DestroyImmediate(explosion);

            var sphere = clone.GetComponent<SphereCollider>();
            if (sphere == null) { sphere = clone.AddComponent<SphereCollider>(); sphere.isTrigger = true; }
            sphere.radius = Radius;

            var simple = clone.GetComponent<ProjectileSimple>();
            if (simple != null)
            {
                simple.desiredForwardSpeed = StormspearTuning.ProjectileSpeed;
                simple.lifetime = LifetimeSeconds;
                simple.updateAfterFiring = true;
            }

            var controller = clone.GetComponent<ProjectileController>();
            if (controller != null)
            {
                controller.procCoefficient = StormspearTuning.ProcCoefficient;
                controller.allowPrediction = false; // avoid a straight client-predicted ghost
                if (HollowSaint.FoundationKit.Vfx.Ghosts.Spear) controller.ghostPrefab = HollowSaint.FoundationKit.Vfx.Ghosts.Spear;
            }

            clone.AddComponent<ProjectileAimForgiveness>().isSpear = true;
            clone.AddComponent<StormspearImpact>();
            Prefab = clone;
            return Prefab;
        }
    }

    /// <summary>
    /// Server-authoritative Stormspear impact (v0.9.10). The direct hit runs the vanilla single-target
    /// pipeline (FriendlyFire check, TakeDamage, OnHitEnemy/OnHitAll). The spear then lodges in what it
    /// hit (Beat.SpearStuck, drawn on every machine and riding the struck enemy), crackles for
    /// StickSeconds and bursts (SpearDetonation): on an enemy, every OTHER enemy in the radius takes
    /// the burst; on terrain it is a weaker fizzle (GroundBurstScale). The crown Thunderbolt calls at
    /// the burst. The charge is recovered from the projectile damage so no extra networking is needed.
    /// </summary>
    [DisallowMultipleComponent]
    public class StormspearImpact : MonoBehaviour, IProjectileImpactBehavior
    {
        private ProjectileController projectileController;
        private ProjectileDamage projectileDamage;
        private bool consumed;

        private void Awake()
        {
            projectileController = GetComponent<ProjectileController>();
            projectileDamage = GetComponent<ProjectileDamage>();
        }

        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            if (consumed || !NetworkServer.active) return;
            var collider = impactInfo.collider;
            if (collider == null) return;
            var owner = projectileController != null ? projectileController.owner : null;
            if (owner != null && collider.gameObject == owner) return;

            HurtBox hurtBox = collider.GetComponent<HurtBox>();
            HealthComponent victim = null;
            if (hurtBox != null)
            {
                victim = hurtBox.healthComponent;
                if (victim != null && owner != null &&
                    (victim.gameObject == owner || victim == owner.GetComponent<HealthComponent>()))
                    return; // never spear the shooter; keep flying
            }

            consumed = true;
            Vector3 point = impactInfo.estimatedPointOfImpact;
            float directDamage = projectileDamage != null ? projectileDamage.damage : 0f;
            bool crit = projectileDamage != null && projectileDamage.crit;

            HealthComponent struck = null;
            if (victim != null && projectileController != null &&
                FriendlyFireManager.ShouldDirectHitProceed(victim, projectileController.teamFilter.teamIndex))
            {
                var info = new DamageInfo();
                if (projectileDamage != null)
                {
                    info.damage = projectileDamage.damage;
                    info.crit = projectileDamage.crit;
                    info.attacker = owner;
                    info.inflictor = gameObject;
                    info.position = point;
                    info.force = projectileDamage.force * transform.forward;
                    info.procChainMask = projectileController.procChainMask;
                    info.procCoefficient = projectileController.procCoefficient;
                    info.damageColorIndex = projectileDamage.damageColorIndex;
                    info.damageType = projectileDamage.damageType;
                }
                info.inflictedHurtbox = hurtBox;
                info.ModifyDamageInfo(hurtBox.damageModifier);
                victim.TakeDamage(info);
                if (GlobalEventManager.instance != null)
                {
                    GlobalEventManager.instance.OnHitEnemy(info, victim.gameObject);
                    GlobalEventManager.instance.OnHitAll(info, collider.gameObject);
                }
                struck = victim;
            }

            var body = owner != null ? owner.GetComponent<CharacterBody>() : null;
            float coefficient = body != null && body.damage > 0.0001f ? directDamage / body.damage : 0f;
            float charge = StormspearTuning.ChargeFromCoefficient(coefficient);
            Vector3 dir = transform.forward;
            // Lodge in the struck enemy (riding its hurtbox) or in the ground.
            Transform anchor = struck != null && hurtBox != null ? hurtBox.transform : null;
            Vfx.KitFx.ServerStuck(point, dir, charge, StormspearTuning.StickSeconds, struck != null ? struck.gameObject : null, body);
            SpearDetonation.Begin(body, owner, gameObject, point, impactInfo.estimatedImpactNormal, anchor, directDamage, crit, charge, struck,
                projectileController != null ? projectileController.procChainMask : default(ProcChainMask),
                projectileDamage != null ? projectileDamage.damageType : new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Secondary));
            Destroy(gameObject);
        }
    }

    /// <summary>Server only: the lodged spear's countdown and burst. Follows the struck hurtbox so the
    /// burst goes off where the enemy is now, not where it was hit.</summary>
    public class SpearDetonation : MonoBehaviour
    {
        private CharacterBody body;
        private GameObject owner;
        private Transform anchor;
        private Vector3 localPoint, point, normal;
        private float damage, charge, age;
        private bool crit;
        private HealthComponent struck;
        private ProcChainMask procChainMask;
        private DamageTypeCombo damageType;

        internal static void Begin(CharacterBody body, GameObject owner, GameObject inflictor, Vector3 point, Vector3 normal, Transform anchor,
            float damage, bool crit, float charge, HealthComponent struck, ProcChainMask mask, DamageTypeCombo damageType)
        {
            var d = new GameObject("HS_SpearDetonation").AddComponent<SpearDetonation>();
            d.body = body; d.owner = owner; d.anchor = anchor; d.point = point;
            d.localPoint = anchor ? anchor.InverseTransformPoint(point) : point;
            d.normal = normal.sqrMagnitude > 0.001f ? normal : Vector3.up;
            d.damage = damage; d.crit = crit; d.charge = charge; d.struck = struck;
            d.procChainMask = mask; d.damageType = damageType;
            if (StormspearTuning.StickSeconds <= 0f) d.Detonate();
        }

        private void FixedUpdate()
        {
            if (anchor) point = anchor.TransformPoint(localPoint);
            age += Time.fixedDeltaTime;
            if (age >= StormspearTuning.StickSeconds) Detonate();
        }

        private void Detonate()
        {
            if (!this || !enabled) return;
            enabled = false;
            bool onEnemy = struck != null;
            float scale = onEnemy ? 1f : StormspearTuning.GroundBurstScale;
            float radius = StormspearTuning.BurstRadiusAt(charge) * scale;
            var hits = new System.Collections.Generic.List<Vector3>();
            float fraction = Mathf.Lerp(StormspearTuning.BurstDamageFraction, StormspearTuning.BurstDamageFractionFull, Mathf.Clamp01(charge));
            if (body != null && fraction > 0f && scale > 0f)
            {
                hits = KitUtil.CappedBlast(body, point, radius, 64, damage * fraction * scale, crit,
                    StormspearTuning.BurstProcCoefficient, damageType, DamageColorIndex.Default, false, struck, procChainMask);
            }
            KitLog.Event("STORMSPEAR_BURST", "radius=" + radius.ToString("0.0") + " hits=" + hits.Count + " onEnemy=" + onEnemy);
            Vfx.KitFx.Server(Vfx.Beat.SpearBurst, point, onEnemy ? Vector3.up : normal, radius, owner: body);

            if (body != null && StormspearTuning.CrownThunderbolt &&
                charge >= StormspearTuning.CrownThunderboltMinCharge - 0.03f &&
                StormspearCharge.InCrown(body))
            {
                var driver = body.GetComponent<ThunderboltDriver>();
                if (driver != null)
                {
                    KitLog.Event("STORMSPEAR_CROWN_THUNDERBOLT", "charge=" + charge.ToString("0.00"));
                    driver.ServerStrikeAt(point, struck != null && struck.alive ? struck : null);
                }
            }
            Destroy(gameObject);
        }
    }
}
