using System.Collections.Generic;
using HollowSaint.FoundationKit.Vfx;
using UnityEngine;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    /// <summary>Local-space surface bolts shared by held and travelling Orbs.
    /// Fixed renderers follow the sphere; no per-frame objects or gameplay RNG.</summary>
    internal sealed class HollowedOrbCrackle : MonoBehaviour
    {
        private const int Arcs = 6, Points = 19, Forks = 3;
        private static readonly Dictionary<Material, Material> surfaceMaterials = new Dictionary<Material, Material>();
        private readonly LineRenderer[] cores = new LineRenderer[Arcs];
        private readonly LineRenderer[] glows = new LineRenderer[Arcs];
        private readonly LineRenderer[] forks = new LineRenderer[Forks];
        private readonly Vector3[] points = new Vector3[Points];
        private System.Random random;
        private float age, nextShape;

        internal void Init(SkinFxPalette palette)
        {
            for (int i = 0; i < Arcs; i++)
            {
                // Variant materials already contain the colored highlight. Tint
                // it once so small dark-skin Orbs retain visible surface bolts.
                cores[i] = MakeLine("surface-core", palette.Material(VfxAssets.ArcCore), palette.Index < 2 ? palette.Core : Color.white, Points);
                glows[i] = MakeLine("surface-glow", palette.Material(VfxAssets.ArcGlow), Color.white, Points);
            }
            for (int i = 0; i < Forks; i++)
                forks[i] = MakeLine("surface-fork", palette.SecondaryMaterial(VfxAssets.ArcCore), Color.white, 4);
            random = new System.Random(GetInstanceID());
            Reshape();
        }

        private LineRenderer MakeLine(string name, Material material, Color color, int count)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = SurfaceMaterial(material); line.useWorldSpace = false; line.positionCount = count;
            line.startColor = line.endColor = color; line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch; line.numCapVertices = 1; line.numCornerVertices = 1;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }

        private static Material SurfaceMaterial(Material source)
        {
            if (!source) return null;
            if (surfaceMaterials.TryGetValue(source, out var cached) && cached) return cached;
            var material = new Material(source) { name = source.name + "_OrbSurface" };
            // Particle depth softening hides bolts only centimeters above an
            // opaque shell. Keep normal depth testing, but harden the fade on
            // these owned surface materials; other kit lightning stays soft.
            material.DisableKeyword("SOFTPARTICLES_ON");
            if (material.HasProperty("_InvFade")) material.SetFloat("_InvFade", 100f);
            surfaceMaterials[source] = material;
            Plugin.Log.LogInfo("HOLLOW_SAINT_ORB_SURFACE_MATERIAL shader=" + material.shader.name +
                " hardenedDepthFade=" + material.HasProperty("_InvFade"));
            return material;
        }

        private void LateUpdate()
        {
            if (random == null) return;
            age += Time.deltaTime;
            if (age >= nextShape) { nextShape = age + .075f; Reshape(); }
            float diameter = Mathf.Abs(transform.lossyScale.x);
            for (int i = 0; i < Arcs; i++)
            {
                float pulse = .8f + .2f * Mathf.Sin(age * 38f + i * 1.7f);
                cores[i].widthMultiplier = diameter * .022f * pulse;
                glows[i].widthMultiplier = diameter * .075f * pulse;
            }
            foreach (var fork in forks) fork.widthMultiplier = diameter * .012f;
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        private void Reshape()
        {
            for (int i = 0; i < Arcs; i++)
            {
                var rotation = Quaternion.Euler(Range(0, 360), Range(0, 360), Range(0, 360));
                float length = Range(1.9f, 3.6f);
                for (int p = 0; p < Points; p++)
                {
                    float a = p * length / (Points - 1);
                    // Keep even the connecting chords outside the opaque shell.
                    points[p] = rotation * new Vector3(Mathf.Cos(a), Range(-.085f, .085f), Mathf.Sin(a)).normalized * Range(.545f, .565f);
                }
                cores[i].SetPositions(points); glows[i].SetPositions(points);
                if (i >= Forks) continue;
                var root = points[Points / 2]; var direction = root.normalized;
                var tangent = rotation * Vector3.up;
                forks[i].SetPosition(0, root);
                forks[i].SetPosition(1, root + direction * .09f + tangent * .045f);
                forks[i].SetPosition(2, root + direction * .16f - tangent * .035f);
                forks[i].SetPosition(3, root + direction * Range(.22f, .34f));
            }
        }
    }
}
