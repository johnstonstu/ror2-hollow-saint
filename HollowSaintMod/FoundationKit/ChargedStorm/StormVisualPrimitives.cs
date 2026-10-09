using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using HollowSaint.FoundationKit.Vfx;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    internal static class StormVisualPrimitives
    {
        private static Material smoke;
        private static Material cloudMass;
        private static readonly Dictionary<int, Material> orbMaterials = new Dictionary<int, Material>();
        internal static GameObject Orb(SkinFxPalette palette, float diameter)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                ball.name = "HS_HollowedOrbVisual";
                var collider = ball.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
                var renderer = ball.GetComponent<MeshRenderer>(); renderer.sharedMaterial = OrbMaterial(palette);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                ball.transform.localScale = Vector3.one * diameter;
                ball.AddComponent<HollowSaint.FoundationKit.HollowedOrb.HollowedOrbCrackle>().Init(palette);
                return ball;
            }
            catch
            {
                // The caller logs the optional effect failure; it cannot own a
                // partially constructed ball until this factory returns.
                Object.Destroy(ball); throw;
            }
        }
        internal static LineRenderer Ring(Transform parent, SkinFxPalette palette)
        {
            var root = new GameObject("HS_StormFootprint"); root.transform.SetParent(parent, false);
            var line = root.AddComponent<LineRenderer>(); line.useWorldSpace = true; line.loop = true;
            line.positionCount = 64; line.widthMultiplier = .07f; line.sharedMaterial = palette.Material(VfxAssets.Trail);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return line;
        }
        internal static void SetRing(LineRenderer line, Vector3 center, float radius)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius);
            }
        }
        internal static Material Smoke()
        {
            if (smoke) return smoke;
            // Shader.Find is detoured by the installed game and throws for legacy
            // Unity names. Clone native smoke, including its authored blend state.
            var source = Addressables.LoadAssetAsync<Material>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_VFX.matGenericSmoke_mat).WaitForCompletion();
            if (!source) throw new System.InvalidOperationException("Native storm-cloud smoke material unavailable.");
            smoke = new Material(source) { name = "HS_ThundercloudSmoke" };
            return smoke;
        }
        private static Material OrbMaterial(SkinFxPalette palette)
        {
            if (orbMaterials.TryGetValue(palette.Index, out var material) && material) return material;
            var shader = Addressables.LoadAssetAsync<Shader>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Shaders.HGStandard_shader).WaitForCompletion();
            if (!shader) throw new System.InvalidOperationException("Native Orb shell shader unavailable.");
            material = new Material(shader) { name = "HS_HollowedOrbShell_" + palette.Index };
            // The native mesh shader gives the ball depth. Lightning particle
            // materials made it a flat saturated disk at ordinary camera angles.
            material.SetTexture("_MainTex", Texture2D.whiteTexture);
            material.SetColor("_Color", palette.Arc * .28f);
            if (material.HasProperty("_EmTex")) material.SetTexture("_EmTex", Texture2D.whiteTexture);
            if (material.HasProperty("_EmColor")) material.SetColor("_EmColor", palette.Arc);
            if (material.HasProperty("_EmPower")) material.SetFloat("_EmPower", .3f);
            if (material.HasProperty("_SpecularStrength")) material.SetFloat("_SpecularStrength", .25f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .6f);
            orbMaterials[palette.Index] = material; return material;
        }
        internal static void CloudVolume(Transform parent, float radius)
        {
            if (!cloudMass)
            {
                var shader = Addressables.LoadAssetAsync<Shader>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Shaders.HGStandard_shader).WaitForCompletion();
                if (!shader) throw new System.InvalidOperationException("Native thundercloud volume shader unavailable.");
                cloudMass = new Material(shader) { name = "HS_ThundercloudVolume" };
                cloudMass.SetTexture("_MainTex", Texture2D.whiteTexture);
                cloudMass.SetColor("_Color", new Color(.13f, .16f, .22f));
                if (cloudMass.HasProperty("_SpecularStrength")) cloudMass.SetFloat("_SpecularStrength", 0f);
                if (cloudMass.HasProperty("_Smoothness")) cloudMass.SetFloat("_Smoothness", 0f);
            }
            // Broad, shaded lobes establish an actual cloud silhouette. Native
            // smoke supplies the soft edge; its additive shader alone disappeared
            // against the bright stage sky with dark particle vertex colors.
            for (int i = 0; i < 16; i++)
            {
                float angle = i * 2.39996f, spread = Mathf.Sqrt((i + .5f) / 16f) * radius * .72f;
                var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere); lobe.name = "HS_CloudLobe";
                var collider = lobe.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
                lobe.transform.SetParent(parent, false);
                // Flattened anvil lobes: a broad storm deck, not a wall of spheres.
                lobe.transform.localPosition = new Vector3(Mathf.Cos(angle) * spread, Mathf.Sin(i * 1.7f) * radius * .045f + radius * .04f, Mathf.Sin(angle) * spread);
                lobe.transform.localScale = new Vector3(radius * .5f, radius * .2f, radius * .46f);
                var renderer = lobe.GetComponent<MeshRenderer>(); renderer.sharedMaterial = cloudMass;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            }
        }
    }
}
