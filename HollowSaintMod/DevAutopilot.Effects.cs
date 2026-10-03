using System.Collections;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.10 (HS_SEGMENTS=effects): status and item effects on the Saint's own body. Captures every
    /// skin, then shield, cloak, immune, crit, energized and the six elite affixes, each from the three
    /// fixed cameras. Run it with Hopoo shading off and on to compare (the overlays need RoR2's shader).
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator EffectSegments()
        {
            trace.AppendLine("EFFECTS hopoo=" + FoundationHopoo.Enabled);
            // v0.9.10 lodged spear: full charge into an enemy, a tap into an enemy, a full charge into the ground.
            for (int k = 0; k < 3; k++)
            {
                string label = k == 0 ? "full" : k == 1 ? "tap" : "ground";
                yield return Segment("stick-" + label);
                int target = k == 1 ? 1 : 0;
                Vector3 at = k == 2 ? mark + facing * 5f : DummyChest(target);
                aimTarget = at;
                fire2 = true; yield return Wait(k == 1 ? 0.05f : 2.2f); fire2 = false;
                yield return Wait(0.12f);
                Time.timeScale = 0.15f;
                yield return Wait(0.03f); TargetShot("stick-" + label + "-a", at);
                yield return Wait(0.08f); TargetShot("stick-" + label + "-b", at);
                yield return Wait(0.12f); TargetShot("stick-" + label + "-c", at);
                yield return Wait(0.1f); TargetShot("stick-" + label + "-burst", at);
                Time.timeScale = 1f;
                yield return Wait(5.5f);
            }
            var skins = pilot.modelLocator && pilot.modelLocator.modelTransform ? pilot.modelLocator.modelTransform.GetComponent<ModelSkinController>() : null;
            for (int skin = 0; skin < 5 && skins; skin++)
            {
                yield return Segment("fx-skin" + skin);
                pilot.skinIndex = (uint)skin; skins.ApplySkin(skin);
                yield return Wait(0.8f); Shot("fx-skin" + skin);
                yield return Wait(0.4f);
            }
            if (skins) { pilot.skinIndex = 0u; skins.ApplySkin(0); }

            // v0.9.11: a boss. The lodged spear on a big target, and Gaze on it.
            yield return Segment("fx-boss");
            SpawnDummy("TitanMaster", mark + facing * 18f);
            yield return Wait(2.5f);
            var titan = dummies.Count > 3 ? dummies[dummies.Count - 1] : null;
            if (titan)
            {
                Vector3 chest = titan.corePosition;
                aimTarget = chest;
                fire2 = true; yield return Wait(2.2f); fire2 = false;
                yield return Wait(0.13f);
                Time.timeScale = 0.15f;
                yield return Wait(0.04f); TargetShot("fx-boss-stick", chest);
                yield return Wait(0.12f); TargetShot("fx-boss-burst", chest);
                Time.timeScale = 1f;
                yield return Wait(1f);
                aimTarget = chest; yield return Press(4); yield return Wait(2.2f); WideShot("fx-boss-gaze"); Shot("fx-boss-gaze");
                yield return Wait(3.5f);
                titan.healthComponent.godMode = false; titan.healthComponent.Suicide();
                yield return Wait(2f);
            }
            else trace.AppendLine("EFFECTS no titan");

            yield return Segment("fx-shield");
            var shield = Give("PersonalShield", 6);
            yield return Wait(0.5f);
            if (pilot.healthComponent) pilot.healthComponent.RechargeShieldFull();
            yield return Wait(0.5f); Shot("fx-shield");
            yield return Wait(0.3f);
            Take(shield);

            yield return Buffed("fx-cloak", RoR2Content.Buffs.Cloak);
            yield return Buffed("fx-immune", RoR2Content.Buffs.Immune);
            yield return Buffed("fx-fullcrit", RoR2Content.Buffs.FullCrit);
            yield return Buffed("fx-energized", RoR2Content.Buffs.Energized);
            yield return Buffed("fx-warcry", RoR2Content.Buffs.WarCryBuff);

            foreach (var affix in new[] { "EliteFireEquipment", "EliteLightningEquipment", "EliteIceEquipment", "ElitePoisonEquipment", "EliteHauntedEquipment", "EliteLunarEquipment", "EliteEarthEquipment", "EliteVoidEquipment" })
            {
                yield return Segment("fx-" + affix);
                var index = EquipmentCatalog.FindEquipmentIndex(affix);
                if (index == EquipmentIndex.None) { trace.AppendLine("EFFECTS missing " + affix); continue; }
                pilot.inventory.SetEquipmentIndex(index);
                yield return Wait(1.0f); Shot("fx-" + affix);
                aimTarget = DummyChest(0); fire1 = true; yield return Wait(0.8f); Shot("fx-" + affix + "-firing"); fire1 = false;
                yield return Wait(0.3f);
                // Not cleared between affixes: a change to no equipment trips a vanilla null reference in
                // CharacterModel.HighlightEquipentDisplay (every survivor), which would read as our error.
                yield return Wait(0.5f);
            }
        }

        private void TargetShot(string name, Vector3 at)
        {
            lastShotReal = Time.realtimeSinceStartup;
            StartCoroutine(TargetShotRoutine(name, at));
        }

        private IEnumerator TargetShotRoutine(string name, Vector3 at)
        {
            yield return new WaitForEndOfFrame();
            trace.AppendLine(scriptTime.ToString("000.00") + " SHOT " + name);
            if (!shotCamera) yield break;
            // From the thrower's side and a little behind, close on the target.
            Render(name, at + Right * 2.6f - facing * 2.2f + Vector3.up * 1.1f, at);
            lastShotReal = Time.realtimeSinceStartup;
        }

        private IEnumerator Buffed(string name, BuffDef buff)
        {
            yield return Segment(name);
            if (!buff) { trace.AppendLine("EFFECTS missing buff for " + name); yield break; }
            pilot.AddTimedBuff(buff, 3f);
            yield return Wait(0.6f); Shot(name);
            yield return Wait(0.4f);
            pilot.ClearTimedBuffs(buff);
            yield return Wait(0.4f);
        }
    }
}
