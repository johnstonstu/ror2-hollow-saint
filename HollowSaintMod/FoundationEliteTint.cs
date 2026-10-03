using RoR2;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.11: elite body colour for the Standard-shaded Saint. RoR2 tints elites through its own
    /// shader's ramp (_EliteIndex), which Unity Standard ignores, so with the default shading an elite
    /// Saint showed the affix crown and aura but kept his skin colours. This blends each renderer's
    /// albedo toward the elite's colour through the renderer's property block. CharacterModel reads
    /// the existing block before writing its own values, so the tint survives its updates.
    /// Does nothing with Hopoo shading on (the game's ramp already does it).
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class FoundationEliteTint : MonoBehaviour
    {
        internal static float Strength = 0.55f;
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private CharacterModel model;
        private EliteIndex current = EliteIndex.None;
        private Material tintedMaterial;
        private float nextCheck;
        private MaterialPropertyBlock block;

        private void Awake() { model = GetComponent<CharacterModel>(); block = new MaterialPropertyBlock(); }

        private void LateUpdate()
        {
            if (FoundationHopoo.Enabled || !model || !model.body || Time.time < nextCheck) return;
            nextCheck = Time.time + 0.25f;
            var inventory = model.body.inventory;
            var equipment = inventory ? EquipmentCatalog.GetEquipmentDef(inventory.currentEquipmentIndex) : null;
            var def = equipment && equipment.passiveBuffDef ? equipment.passiveBuffDef.eliteDef : null;
            var elite = def ? def.eliteIndex : EliteIndex.None;
            // A skin change swaps the shared materials under the tint, so re-apply when they change.
            var firstMaterial = model.baseRendererInfos.Length > 0 && model.baseRendererInfos[0].renderer ? model.baseRendererInfos[0].renderer.sharedMaterial : null;
            if (elite == current && (elite == EliteIndex.None || firstMaterial == tintedMaterial)) return;
            current = elite;
            tintedMaterial = firstMaterial;
            Color tint = def ? (Color)def.color : Color.white;
            foreach (var info in model.baseRendererInfos)
            {
                var renderer = info.renderer;
                var material = renderer ? renderer.sharedMaterial : null;
                if (!material || !material.HasProperty(ColorId)) continue;
                renderer.GetPropertyBlock(block);
                Color baseColor = material.GetColor(ColorId);
                block.SetColor(ColorId, def ? Color.Lerp(baseColor, tint * Mathf.Max(0.35f, baseColor.maxColorComponent), Strength) : baseColor);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
