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
    /// impact and explosion stripped, replaced by StormspearImpact. No terrain planting; a
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
    /// the burst on unfunded Crown shots. A full stored Prayer bank is claimed at server
    /// initialization and adds one snapshotted strike at the first qualifying landing instead.
    /// Charge and crown form are captured from the initialized projectile, not the
    /// owner's mutable damage stat or crown buff at impact. Enemy impacts also replace the owner's
    /// bounded three-second conductor; that weak secondary path is separate from this unchanged burst.
    /// </summary>
    [DisallowMultipleComponent]
    public class StormspearImpact : MonoBehaviour, IProjectileImpactBehavior
    {
        private ProjectileController projectileController;
        private ProjectileDamage projectileDamage;
        private bool consumed;
        private StormspearShot shot;
        private SpearConductorSchedule conductorShot;
        private PrayerStrikeSnapshot prayer;
        private PrayerImpactClaim prayerImpact;
        private CharacterBody launchOwner;
        private Stage launchStage;
        private TeamIndex launchTeam;
        private bool captured;

        private void Awake()
        {
            projectileController = GetComponent<ProjectileController>();
            projectileDamage = GetComponent<ProjectileDamage>();
            if (projectileController != null) projectileController.onInitialized += CaptureShot;
        }

        private void CaptureShot(ProjectileController controller)
        {
            if (captured) return;
            captured = true;
            // InitializeProjectile has assigned both fields before this callback on the server,
            // including throws forwarded from a remote casting authority. Never read them in Awake.
            shot = new StormspearShot(projectileDamage != null ? projectileDamage.force : 0f, controller.combo);
            // Tuning changes live. Freeze normalization at initialized server spawn, not later impact.
            // Remote launches normalize with the server-at-spawn coefficient under config agreement;
            // the remote owner's private tuning is not part of the projectile transport.
            conductorShot = new SpearConductorSchedule(shot.Charge, projectileDamage != null ? projectileDamage.damage : 0f,
                SpearFeedbackPolicy.Direct(StormspearTuning.DamageAt(shot.Charge), shot.Charge));
            controller.onInitialized -= CaptureShot;
            if (!NetworkServer.active || !controller.owner) return;
            launchTeam = controller.teamFilter ? controller.teamFilter.teamIndex : TeamIndex.None;
            launchOwner = controller.owner.GetComponent<CharacterBody>();
            var meter = launchOwner ? launchOwner.GetComponent<DischargeMeter>() : null;
            int spent;
            // The server has created and initialized this exact projectile, including
            // forwarded remote throws. Authority-side FireProjectile never spends.
            if (!meter || !meter.TryClaimSpearPrayer(out spent)) return;
            launchStage = Stage.instance;
            prayer = new PrayerStrikeSnapshot(launchOwner.damage, KitTuning.ThunderboltDamageCoefficient,
                KitTuning.ThunderboltSplashFraction, KitTuning.ThunderboltSplashRadius,
                projectileDamage != null && projectileDamage.crit, funded: true);
            KitLog.Event("ANSWERED_PRAYER_SPEAR_COMMITTED", "bank=" + spent);
        }

        private void OnDestroy()
        {
            prayerImpact.Cancel();
            if (projectileController != null) projectileController.onInitialized -= CaptureShot;
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
            bool enemyLanding = victim && victim.alive && projectileController && projectileController.teamFilter &&
                victim.body && victim.body.teamComponent &&
                TeamMask.GetEnemyTeams(launchTeam).HasTeam(victim.body.teamComponent.teamIndex) &&
                FriendlyFireManager.ShouldDirectHitProceed(victim, projectileController.teamFilter.teamIndex);
            bool worldLanding = !hurtBox && (LayerIndex.world.mask & (1 << collider.gameObject.layer)) != 0;
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
            float charge = shot.Charge;
            Vector3 dir = transform.forward;
            // Lodge in the struck enemy (riding its hurtbox) or in the ground.
            Transform anchor = struck != null && hurtBox != null ? hurtBox.transform : null;
            bool conducts = struck != null && struck.alive && body != null && anchor != null;
            Vfx.KitFx.ServerStuck(point, dir, charge, conducts ? SpearConductorSchedule.Lifetime : StormspearTuning.StickSeconds, struck != null ? struck.gameObject : null, body);
            if (conducts) SpearConductor.Begin(body, struck, anchor, point, crit, conductorShot);
            SpearDetonation.Begin(body, owner, gameObject, point, impactInfo.estimatedImpactNormal, anchor, directDamage, crit, shot, struck,
                projectileController != null ? projectileController.procChainMask : default(ProcChainMask),
                projectileDamage != null ? projectileDamage.damageType : new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Secondary), prayer.Empowered);
            bool ownerValid = launchOwner && launchOwner == body && launchOwner.isActiveAndEnabled &&
                launchOwner.healthComponent && launchOwner.healthComponent.alive &&
                (!launchOwner.master || launchOwner.master.GetBody() == launchOwner);
            if (prayerImpact.TryResolve(prayer.Empowered, enemyLanding || worldLanding, ownerValid,
                launchStage && launchStage == Stage.instance))
            {
                var driver = body.GetComponent<ThunderboltDriver>();
                if (driver) driver.ServerPrayerStrikeAt(point, struck && struck.alive ? struck : null, prayer);
            }
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
        private StormspearShot shot;
        private bool crit;
        private HealthComponent struck;
        private ProcChainMask procChainMask;
        private DamageTypeCombo damageType;
        private bool prayerFunded;

        internal static void Begin(CharacterBody body, GameObject owner, GameObject inflictor, Vector3 point, Vector3 normal, Transform anchor,
            float damage, bool crit, StormspearShot shot, HealthComponent struck, ProcChainMask mask, DamageTypeCombo damageType, bool prayerFunded = false)
        {
            var d = new GameObject("HS_SpearDetonation").AddComponent<SpearDetonation>();
            d.body = body; d.owner = owner; d.anchor = anchor; d.point = point;
            d.localPoint = anchor ? anchor.InverseTransformPoint(point) : point;
            d.normal = normal.sqrMagnitude > 0.001f ? normal : Vector3.up;
            d.damage = damage; d.crit = crit; d.shot = shot; d.charge = shot.Charge; d.struck = struck;
            d.procChainMask = mask; d.damageType = damageType;
            d.prayerFunded = prayerFunded;
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

            if (body != null && shot.CallsThunderbolt(StormspearTuning.CrownThunderbolt,
                StormspearTuning.CrownThunderboltMinCharge, prayerFunded))
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
