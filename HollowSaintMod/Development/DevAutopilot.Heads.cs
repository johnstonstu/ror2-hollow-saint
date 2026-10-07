using System.Collections;
using System.Collections.Generic;
using HollowSaint.FoundationKit;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.11 (HS_SEGMENTS=heads): every item and equipment whose borrowed display rides the Head or
    /// Halo mount, one at a time, shot close from the front and from the gameplay camera behind, so
    /// displays that clip the halo can be found and refitted. Writes heads.txt (item, child, rule).
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator HeadSegments()
        {
            var model = pilot.modelLocator && pilot.modelLocator.modelTransform ? pilot.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            var set = model ? model.itemDisplayRuleSet : null;
            var log = new System.Text.StringBuilder();
            if (!set) { trace.AppendLine("HEADS no rule set"); yield break; }
            var items = new List<ItemDef>();
            var equipment = new List<EquipmentDef>();
            foreach (var group in set.keyAssetRuleGroups)
            {
                if (group.displayRuleGroup.rules == null) continue;
                bool head = false;
                foreach (var rule in group.displayRuleGroup.rules)
                    if (rule.childName == "Head" || rule.childName == "Halo") head = true;
                if (!head) continue;
                foreach (var rule in group.displayRuleGroup.rules)
                    log.AppendLine((group.keyAsset ? group.keyAsset.name : "?") + " child=" + rule.childName + " pos=" + rule.localPos.ToString("F3") +
                        " ang=" + rule.localAngles.ToString("F1") + " scale=" + rule.localScale.ToString("F3") + " type=" + rule.ruleType);
                var item = group.keyAsset as ItemDef;
                var equip = group.keyAsset as EquipmentDef;
                if (item && item.itemIndex != ItemIndex.None) items.Add(item);
                else if (equip && equip.equipmentIndex != EquipmentIndex.None) equipment.Add(equip);
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(output, "heads.txt"), log.ToString());
            trace.AppendLine("HEADS items=" + items.Count + " equipment=" + equipment.Count);
            yield return Segment("heads");
            foreach (var item in items)
            {
                pilot.inventory.GiveItemPermanent(item.itemIndex, 1);
                yield return Wait(0.35f);
                HeadShot("h-" + item.name);
                yield return Wait(0.3f);
                pilot.inventory.RemoveItemPermanent(item.itemIndex, 1);
                yield return Wait(0.1f);
            }
            foreach (var equip in equipment)
            {
                pilot.inventory.SetEquipmentIndex(equip.equipmentIndex);
                yield return Wait(0.35f);
                HeadShot("h-" + equip.name);
                yield return Wait(0.3f);
            }
        }

        private void HeadShot(string name)
        {
            lastShotReal = Time.realtimeSinceStartup;
            StartCoroutine(HeadShotRoutine(name));
        }

        private IEnumerator HeadShotRoutine(string name)
        {
            yield return new WaitForEndOfFrame();
            if (!shotCamera || !pilot) yield break;
            Vector3 eye = KitUtil.EyePosition(pilot);
            Render(name + "_front", eye + facing * 1.7f + Right * 0.7f + Vector3.up * 0.15f, eye);
            Render(name + "_back", eye - facing * 2.2f + Vector3.up * 0.6f, eye + Vector3.up * 0.1f);
            lastShotReal = Time.realtimeSinceStartup;
        }
    }
}
