using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.ArcBolt
{
    /// <summary>
    /// Builds and owns the Arc Bolt projectile prefab. The projectile is a clone of the
    /// Mage's lightning bolt (the closest vanilla fast straight-flying bolt); its vanilla
    /// impact behaviour is stripped and replaced by <see cref="ArcBoltProjectileImpact"/>
    /// so hits, chains and Discharge charge are resolved in one server-side place.
    /// Fired by the casting authority through ProjectileManager (see ArcBoltState).
    /// </summary>
    public static class ArcBoltProjectile
    {
        /// <summary>Legacy resources path of the vanilla template projectile. UNVERIFIED at
        /// runtime (no game launch in this phase) — the same legacy mapping already works for
        /// "Prefabs/CharacterBodies/CommandoBody" in the shipped foundation build, but the
        /// projectile subtree may differ. If this load fails the error is logged loudly and
        /// the skill degrades to no projectile instead of throwing.</summary>
        public const string TemplateLegacyPath = "Prefabs/Projectiles/MageLightningboltBasic";

        public const string PrefabName = "HollowSaintArcBoltProjectile";

        /// <summary>The registered projectile prefab. Null until EnsurePrefab succeeds.</summary>
        public static GameObject Prefab { get; private set; }

        /// <summary>Builds (once) the projectile prefab from a vanilla template. Safe to call
        /// repeatedly; returns the existing prefab on later calls.</summary>
        public static GameObject EnsurePrefab()
        {
            if (Prefab != null) return Prefab;
            GameObject template;
            try
            {
                template = LegacyResourcesAPI.Load<GameObject>(TemplateLegacyPath);
            }
            catch (System.Exception error)
            {
                Plugin.Log.LogError("Arc Bolt: failed to load projectile template '" + TemplateLegacyPath + "': " + error);
                return null;
            }
            if (template == null)
            {
                Plugin.Log.LogError("Arc Bolt: projectile template '" + TemplateLegacyPath + "' does not exist in the legacy resources map. The projectile must be re-templated (see RESULT-ARCBOLT.md).");
                return null;
            }
            var clone = PrefabAPI.InstantiateClone(template, PrefabName, true);
            if (clone == null)
            {
                Plugin.Log.LogError("Arc Bolt: PrefabAPI.InstantiateClone returned null for the Arc Bolt projectile.");
                return null;
            }

            // Strip the vanilla impact behaviours so exactly one component resolves hits.
            // Keeping them would double-apply damage (vanilla applies TakeDamage itself).
            var vanillaImpact = clone.GetComponent<ProjectileSingleTargetImpact>();
            if (vanillaImpact != null) Object.DestroyImmediate(vanillaImpact);
            var explosion = clone.GetComponent<ProjectileExplosion>();
            if (explosion != null) Object.DestroyImmediate(explosion);

            // Native solid collision keeps ProjectileController's impact filters and
            // single damage dispatcher. Unity's swept CCD does not protect triggers.
            var sphere = clone.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                sphere = clone.AddComponent<SphereCollider>();
            }
            sphere.isTrigger = false;
            sphere.center = Vector3.zero;
            sphere.radius = KitTuning.ArcBoltRadius;
            var body = clone.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else Plugin.Log.LogWarning("Arc Bolt: template has no Rigidbody; swept collision is unavailable.");

            // Proc coefficient has no approved number in KitTuning — PROPOSAL 1.0
            // (see RESULT-ARCBOLT.md, Proposals).
            var controller = clone.GetComponent<ProjectileController>();
            if (controller != null)
            {
                Plugin.Log.LogInfo("Arc Bolt: native controller " + controller.GetType().Name +
                    "; solid sphere radius " + sphere.radius + "; Rigidbody " +
                    (body != null ? "nonkinematic, gravity off, ContinuousDynamic" : "missing"));
                controller.procCoefficient = KitTuning.ArcBoltProcCoefficient; // v0.9.13: 0.8 (was 1.0); config, restart
                controller.allowPrediction = false; // server homing must also drive the visible flight
                HollowSaint.FoundationKit.Vfx.Ghosts.Assign(controller, HollowSaint.FoundationKit.Vfx.Ghosts.ArcBolt, "Arc Bolt");
            }

            // ProjectileSimple drives Rigidbody velocity. Preserve the previous nominal
            // range (80 m/s * serialized template lifetime) when increasing speed.
            var simple = clone.GetComponent<ProjectileSimple>();
            if (simple != null)
            {
                float templateLifetime = simple.lifetime;
                simple.lifetime = ArcBoltReliabilityRules.Lifetime(templateLifetime, KitTuning.ArcBoltProjectileSpeed);
                simple.desiredForwardSpeed = KitTuning.ArcBoltProjectileSpeed;
                simple.updateAfterFiring = true; // follow the small in-flight aim correction
                Plugin.Log.LogInfo("Arc Bolt: bolt speed set to " +
                    KitTuning.ArcBoltProjectileSpeed + " m/s; lifetime " + simple.lifetime +
                    " s (template " + templateLifetime + " s; range capped to previous speed).");
            }
            else
            {
                // Loud rather than silent: a wrong-speed bolt is a tuning bug that would
                // otherwise only surface as "the feel is off" much later.
                Plugin.Log.LogWarning("Arc Bolt: template carries no ProjectileSimple, so the " +
                    "approved speed " + KitTuning.ArcBoltProjectileSpeed + " m/s was NOT applied.");
            }

            clone.AddComponent<ProjectileAimForgiveness>();
            var impact = clone.AddComponent<ArcBoltProjectileImpact>();
            // Placeholder impact VFX: none. Declared as required VFX in RESULT-ARCBOLT.md;
            // the field is the drop-in hook so no code change is needed when art lands.
            impact.impactEffectPrefab = null;

            Prefab = clone;
            return Prefab;
        }
    }

    /// <summary>
    /// Server-authoritative hit resolution for one Arc Bolt projectile. Mirrors the
    /// (decompiled, real-body) ProjectileSingleTargetImpact.OnProjectileImpact damage
    /// pipeline, then reports the confirmed hit to ArcBoltChainServer, which applies the
    /// chain and the Discharge charge. Damage is applied ONLY under NetworkServer.active.
    /// Lives on the projectile rather than the casting state so a bolt whose cast was
    /// interrupted mid-flight still resolves and still awards charge.
    /// </summary>
    [DisallowMultipleComponent]
    public class ArcBoltProjectileImpact : MonoBehaviour, IProjectileImpactBehavior
    {
        public ProjectileController projectileController;
        public ProjectileDamage projectileDamage;

        /// <summary>Optional impact VFX, spawned through EffectManager on the server.
        /// Null until the VFX pass supplies a prefab.</summary>
        public GameObject impactEffectPrefab;

        public bool destroyOnWorld = true;

        private bool consumed;

        private void Awake()
        {
            projectileController = GetComponent<ProjectileController>();
            projectileDamage = GetComponent<ProjectileDamage>();
        }

        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            if (consumed) return;
            var collider = impactInfo.collider;
            if (projectileController != null && collider != null && collider.gameObject == projectileController.owner) return;

            consumed = true; // single impact semantics, like vanilla 'alive = false'
            var damageInfo = new DamageInfo();
            if (projectileDamage != null)
            {
                damageInfo.damage = projectileDamage.damage;
                damageInfo.crit = projectileDamage.crit;
                damageInfo.attacker = projectileController != null ? projectileController.owner : null;
                damageInfo.inflictor = gameObject;
                damageInfo.position = impactInfo.estimatedPointOfImpact;
                damageInfo.force = projectileDamage.force * transform.forward;
                damageInfo.procChainMask = projectileController != null ? projectileController.procChainMask : default(ProcChainMask);
                damageInfo.procCoefficient = projectileController != null ? projectileController.procCoefficient : 1f;
                damageInfo.damageColorIndex = projectileDamage.damageColorIndex;
                damageInfo.damageType = projectileDamage.damageType;
            }

            HurtBox hurtBox = collider != null ? collider.GetComponent<HurtBox>() : null;
            if (hurtBox != null)
            {
                var healthComponent = hurtBox.healthComponent;
                if (healthComponent != null)
                {
                    if (projectileController != null && projectileController.owner != null &&
                        (healthComponent.gameObject == projectileController.owner ||
                         healthComponent == projectileController.owner.GetComponent<HealthComponent>()))
                    {
                        consumed = false; // hit the shooter's own hurtbox: not an impact, keep flying
                        return; // never damage the shooter
                    }
                    if (NetworkServer.active && FriendlyFireManager.ShouldDirectHitProceed(healthComponent, projectileController.teamFilter.teamIndex))
                    {
                        damageInfo.inflictedHurtbox = hurtBox;
                        damageInfo.ModifyDamageInfo(hurtBox.damageModifier);
                        healthComponent.TakeDamage(damageInfo);
                        if (GlobalEventManager.instance != null)
                        {
                            GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
                            GlobalEventManager.instance.OnHitAll(damageInfo, collider.gameObject);
                        }
                        // CONFIRMED successful hit on the server → chain + Discharge charge.
                        var attackerBody = projectileController != null && projectileController.owner != null
                            ? projectileController.owner.GetComponent<CharacterBody>()
                            : null;
                        ArcBoltChainServer.ResolveConfirmedHit(
                            attackerBody,
                            healthComponent,
                            impactInfo.estimatedPointOfImpact,
                            projectileDamage != null ? projectileDamage.damage : 0f,
                            projectileDamage != null && projectileDamage.crit);
                    }
                }
            }
            else if (!destroyOnWorld)
            {
                consumed = false; // pass through world hits when configured to survive
                return;
            }

            if (consumed) Kill(impactInfo.estimatedPointOfImpact);
        }

        private void Kill(Vector3 hitPosition, bool sound = true)
        {
            if (NetworkServer.active) HollowSaint.FoundationKit.Vfx.KitFx.Server(HollowSaint.FoundationKit.Vfx.Beat.BoltImpact, hitPosition, sound: sound, owner: projectileController && projectileController.owner ? projectileController.owner.GetComponent<CharacterBody>() : null);
            Destroy(gameObject);
        }
    }
}
