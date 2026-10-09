using System.Collections;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.HollowedOrb;
using HollowSaint.FoundationKit.OpenCircuit;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal sealed partial class DevAutopilot
    {
        private void RecordOrbVfxImpact(DamageReport report)
        {
            if (!recordingCharges || report.attackerBody != pilot || report.damageInfo.damageType.damageSource != DamageSource.Secondary) return;
            if (chargeReports.Count <= 3 && report.victimBody)
                StartCoroutine(OrbVfxImpactShot(segment + "-impact-" + chargeReports.Count, report.victimBody.corePosition));
        }
        private IEnumerator OrbVfxImpactShot(string name, Vector3 point)
        {
            yield return new WaitForSeconds(.055f); yield return new WaitForEndOfFrame();
            Render(name, point - facing * 2f + Right * 2f + Vector3.up * 4.5f, point);
            lastShotReal = Time.realtimeSinceStartup;
        }
        private IEnumerator OrbVfxFlightShot(string name)
        {
            float deadline = Time.time + 1f;
            while (Time.time < deadline)
            {
                yield return new WaitForEndOfFrame();
                var flight = UnityEngine.Object.FindObjectOfType<HollowedOrbFlightFx>();
                if (!flight || Vector3.Distance(flight.transform.position, pilot.corePosition) < 3f) continue;
                var point = flight.transform.position;
                Render(name, point - facing * 3f + Right * 2.5f + Vector3.up, point);
                lastShotReal = Time.realtimeSinceStartup; yield break;
            }
            ReleaseCheck(false, "native travelling Orb capture exists " + name);
        }
        private IEnumerator OrbVfxCast(int count, string name)
        {
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
            aimTarget = DummyChest(0); chargeSource = DamageSource.Secondary;
            chargeReports.Clear(); recordingCharges = true;
            fire2 = true; yield return Wait(count == 0 ? .3f : .57f + (count - 1) * .3f);
            Shot(name + "-gather"); yield return Wait(.025f); fire2 = false;
            StartCoroutine(OrbVfxFlightShot(name + "-flight"));
            yield return Wait(4f); recordingCharges = false;
            ReleaseCheck(pilot.GetComponent<DischargeMeter>().Charge == 5 - count, "VFX pass preserves optional spend " + name);
            ReleaseCheck(chargeReports.Count == ChargedStormTuning.HitBudget(count), "VFX pass preserves finite native hits " + name);
            ReleaseCheck(UnityEngine.Object.FindObjectsOfType<HollowedOrbCrackle>().Length == 0 &&
                UnityEngine.Object.FindObjectsOfType<HollowedOrbFlightFx>().Length == 0 &&
                UnityEngine.Object.FindObjectsOfType<HollowedOrbImpactFx>().Length == 0, "native Orb presentation cleans up " + name);
        }
        private IEnumerator OrbVfxSegments()
        {
            // Success: real native casts retain costs/hits, presentation cleans
            // up, all skins render, overhead casts render without owning hands.
            var secondary = pilot.skillLocator.secondary; var special = pilot.skillLocator.special;
            var model = pilot.modelLocator.modelTransform.GetComponent<ModelSkinController>();
            int original = (int)pilot.skinIndex;
            secondary.SetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
            special.SetSkillOverride(this, OpenCircuitRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
            foreach (var dummy in dummies)
            {
                dummy.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 1000);
                dummy.RecalculateStats(); dummy.healthComponent.HealFraction(1f, default(ProcChainMask));
                dummy.healthComponent.godMode = false; dummy.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
            GlobalEventManager.onServerDamageDealt += RecordChargeDamage;
            GlobalEventManager.onServerDamageDealt += RecordOrbVfxImpact;
            try
            {
                foreach (int count in new[] { 0, 1 })
                {
                    yield return Segment("orb-vfx-tier-" + count);
                    yield return OrbVfxCast(count, segment);
                }
                for (int i = 0; i < model.skins.Length; i++)
                {
                    pilot.skinIndex = (uint)i; model.ApplySkin(i);
                    yield return Segment("orb-vfx-skin-" + i);
                    yield return OrbVfxCast(5, segment);
                }
                pilot.skinIndex = (uint)original; model.ApplySkin(original);
                yield return Segment("orb-vfx-overhead");
                pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 5);
                fire4 = true; yield return Wait(.2f); fire4 = false; yield return Wait(1.6f);
                yield return OrbVfxCast(3, segment);
                yield return WaitCrownEnd();
            }
            finally
            {
                recordingCharges = fire2 = fire4 = false;
                GlobalEventManager.onServerDamageDealt -= RecordChargeDamage;
                GlobalEventManager.onServerDamageDealt -= RecordOrbVfxImpact;
                pilot.skinIndex = (uint)original; model.ApplySkin(original);
                secondary.UnsetSkillOverride(this, ChargedStormRegistration.Orb, GenericSkill.SkillOverridePriority.Replacement);
                special.UnsetSkillOverride(this, OpenCircuitRegistration.SkillDef, GenericSkill.SkillOverridePriority.Replacement);
            }
        }
    }
}
