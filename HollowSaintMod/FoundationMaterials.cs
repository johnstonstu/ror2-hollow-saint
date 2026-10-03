using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowSaint
{
    internal static class FoundationMaterials
    {
        // Temporary game-facing tuning. The Unity art proof keeps its original materials.
        private const float MaxEmission = 0.9f;

        internal static void Apply(GameObject model)
        {
            var replacements = new Dictionary<Material, Material>();
            int rearCount = 0;
            int emissionCount = 0;

            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (!source) continue;
                    if (!replacements.TryGetValue(source, out var tuned))
                    {
                        tuned = Tune(source, ref rearCount, ref emissionCount);
                        replacements.Add(source, tuned);
                    }
                    if (tuned == source) continue;
                    materials[i] = tuned;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }

            int haloCount = ApplyHaloBone(model);
            Plugin.Log.LogInfo("Foundation material pass: rear=" + rearCount +
                " emissive=" + emissionCount + " haloBone=" + haloCount + " model=" + model.name);
        }

        /// <summary>Bone/ivory for the halo segments (art direction: bone, not the atlas maroon).
        /// Gap lights and other emissive pieces keep their glow.</summary>
        internal static readonly Color HaloBone = new Color(0.86f, 0.82f, 0.72f, 1f);

        private static int ApplyHaloBone(GameObject model)
        {
            var boneMats = new Dictionary<Material, Material>();
            int count = 0;
            var names = new List<string>();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer) continue;
                if (!IsHalo(renderer, model.transform)) continue;
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (!source) continue;
                    Color emission = source.HasProperty("_EmissionColor") ? source.GetColor("_EmissionColor") : Color.black;
                    string n = source.name.ToLowerInvariant();
                    if (emission.maxColorComponent > 0.25f || n.Contains("cyan") || n.Contains("gap") || n.Contains("light") || n.Contains("conductor"))
                        continue;
                    if (!boneMats.TryGetValue(source, out var bone))
                    {
                        bone = new Material(source) { name = source.name + " (halo bone)" };
                        bone.mainTexture = null;
                        bone.color = HaloBone;
                        if (bone.HasProperty("_EmissionColor")) bone.SetColor("_EmissionColor", Color.black);
                        bone.DisableKeyword("_EMISSION");
                        boneMats.Add(source, bone);
                    }
                    materials[i] = bone;
                    changed = true;
                }
                if (changed) { renderer.sharedMaterials = materials; count++; if (names.Count < 8) names.Add(renderer.name); }
            }
            if (count > 0) Plugin.Log.LogInfo("HOLLOW_SAINT_HALO_BONE renderers=" + count + " e.g. " + string.Join(", ", names));
            else Plugin.Log.LogWarning("HOLLOW_SAINT_HALO_BONE no halo renderers found under halo bones");
            return count;
        }

        private static bool IsHalo(Renderer renderer, Transform root)
        {
            for (var t = renderer.transform; t != null && t != root; t = t.parent)
                if (t.name.IndexOf("halo", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (renderer is SkinnedMeshRenderer skinned && skinned.bones != null && skinned.bones.Length > 0)
            {
                foreach (var b in skinned.bones)
                    if (b == null || b.name.IndexOf("halo", StringComparison.OrdinalIgnoreCase) < 0) return false;
                return true;
            }
            return false;
        }

        private static Material Tune(Material source, ref int rearCount, ref int emissionCount)
        {
            bool rear = source.name.IndexOf("posterior_graphite", StringComparison.OrdinalIgnoreCase) >= 0;
            Color emission = source.HasProperty("_EmissionColor")
                ? source.GetColor("_EmissionColor") : Color.black;
            bool bright = emission.maxColorComponent > MaxEmission;
            if (!rear && !bright) return source;

            var tuned = new Material(source) { name = source.name + " (Hollow Saint game)" };
            if (rear)
            {
                // The atlas-backed rear material is washed out in the game preview.
                // A solid graphite test surface makes missing geometry easy to spot.
                tuned.mainTexture = null;
                tuned.color = new Color(0.025f, 0.035f, 0.042f, 1f);
                tuned.SetTexture("_EmissionMap", null);
                tuned.SetColor("_EmissionColor", Color.black);
                tuned.DisableKeyword("_EMISSION");
                rearCount++;
            }
            else
            {
                tuned.SetColor("_EmissionColor", emission * (MaxEmission / emission.maxColorComponent));
                emissionCount++;
            }
            return tuned;
        }
    }
}
