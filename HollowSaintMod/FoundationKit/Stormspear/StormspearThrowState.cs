using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Stormspear
{
    /// <summary>
    /// Stormspear throw. Serializes the charge and form so every machine sees the exact value.
    /// OnEnter pushes the charge into StormspearCharge and releases it (presentation reads that);
    /// the authority fires the projectile through ProjectileManager. Lasts MinThrowInterval /
    /// attackSpeed, which is the floor between tap throws.
    /// </summary>
    public class StormspearThrowState : BaseSkillState
    {
        public float charge;
        public SpearForm form = SpearForm.Hand;
        private float duration, releaseAt;
        private bool fired;
        private bool attempted;
        internal bool CooldownReleased => fired || (!isAuthority && fixedAge >= releaseAt);
        private bool spearLeft = true, gotHand;

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(charge);
            writer.Write((byte)form);
            writer.Write(SpearDischarge.SpearCarry.NetworkHandOf(characterBody));
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            charge = reader.ReadSingle();
            form = (SpearForm)reader.ReadByte();
            spearLeft = reader.ReadBoolean();
            gotHand = true;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (gotHand) SpearDischarge.SpearCarry.ApplyNetworkHand(characterBody, spearLeft);
            duration = StormspearTuning.MinThrowInterval / Mathf.Max(0.1f, attackSpeedStat);
            // v0.9.3: a hand throw leaves at the apex of the whip, so the arm visibly throws the spear.
            releaseAt = form == SpearForm.Hand ? Mathf.Max(0f, StormspearTuning.HandReleaseDelay) : 0f;
            duration = Mathf.Max(duration, releaseAt + 0.02f);
            var comp = StormspearCharge.Of(characterBody);
            if (comp)
            {
                // A machine that cancelled on the state swap (not authority) re-enters here.
                if (!comp.Charging) comp.Begin(form);
                comp.SetForm(form);
                comp.SetCharge(charge);
                comp.Release();
            }
            if (characterBody) characterBody.SetAimTimer(2f);
            KitLog.Event("STORMSPEAR_THROW", "charge=" + charge.ToString("0.00") + " form=" + form);
            if (isAuthority && releaseAt <= 0f) FireOnce();
        }

        private void FireOnce()
        {
            if (attempted) return;
            attempted = true;
            fired = Fire();
            if (!fired) return;
            AddRecoil(-0.8f * (0.5f + charge), -1.4f * (0.5f + charge), -0.4f, 0.4f);
        }

        public override void OnExit()
        {
            if (isAuthority && !fired) FireOnce(); // interrupted before the apex: the throw still happens
            if (isAuthority && !fired && characterBody && characterBody.healthComponent && characterBody.healthComponent.alive)
            {
                var slot = activatorSkillSlot ? activatorSkillSlot : (skillLocator ? skillLocator.secondary : null);
                if (slot && StormspearCooldownPolicy.CanRefund(fired, isAuthority, true, slot.stock, slot.maxStock))
                {
                    float progress = slot.rechargeStopwatch;
                    slot.AddOneStock();
                    slot.rechargeStopwatch = progress;
                }
            }
            base.OnExit();
        }

        private bool Fire()
        {
            var prefab = StormspearProjectile.Prefab;
            if (prefab == null || ProjectileManager.instance == null) return false;
            Ray launch = ReleaseRay();
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = prefab,
                position = launch.origin,
                rotation = Util.QuaternionSafeLookRotation(launch.direction),
                owner = gameObject,
                damage = SpearFeedbackPolicy.Direct(StormspearTuning.DamageAt(charge), charge) * damageStat,
                force = StormspearShot.ForceForCharge(charge),
                // Reserved only on our custom projectile; transported for remote owners too.
                comboNumber = form == SpearForm.Crown ? StormspearShot.CrownCombo : (byte)0,
                crit = RollCrit(),
                damageColorIndex = DamageColorIndex.Default,
                damageTypeOverride = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Secondary)
            });
            return true;
        }

        private Ray ReleaseRay()
        {
            Ray aim = GetAimRay();
            Vector3 hand;
            if (form == SpearForm.Crown) hand = KitUtil.EyePosition(characterBody) + Vector3.up * StormspearTuning.CrownSpearHeight;
            else if (!SpearDischarge.SpearCarry.TryReleasePoint(characterBody, out hand)) hand = Vfx.KitFx.Socket(characterBody, SpearDischarge.SpearCarry.SpearMuzzleOf(characterBody));
            Vector3 target = aim.origin + aim.direction * 1000f;
            RaycastHit hit;
            if (Util.CharacterRaycast(gameObject, aim, out hit, 1000f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
                target = hit.point;
            Vector3 origin = (hand - aim.origin).sqrMagnitude < 16f ? hand : aim.origin;
            Vector3 dir = target - origin;
            if (dir.sqrMagnitude < 0.01f || Vector3.Dot(dir, aim.direction) <= 0f) dir = aim.direction;
            return new Ray(origin, dir.normalized);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority && !fired && fixedAge >= releaseAt) FireOnce();
            if (isAuthority && fixedAge >= duration) outer.SetNextStateToMain();
        }

        // Committed for the minimum interval so tap throws cannot exceed the stated rate.
        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.PrioritySkill; }
    }
}
