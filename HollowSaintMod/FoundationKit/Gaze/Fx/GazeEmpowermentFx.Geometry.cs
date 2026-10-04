using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    public sealed partial class GazeEmpowermentFx
    {
        private sealed class Stroke
        {
            internal readonly LineRenderer line;
            private readonly Vector3[] loop = new Vector3[17];
            internal Stroke(Transform parent, string name, Material material)
            {
                var go = new GameObject(name); go.transform.SetParent(parent, false);
                line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true; line.alignment = LineAlignment.View;
                line.textureMode = LineTextureMode.Stretch;
                line.sharedMaterial = material; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false; line.numCapVertices = 0; line.numCornerVertices = 0;
                line.enabled = false;
            }
            internal void Draw(Vector3[] points, int count, float width, Color color, float alpha)
            {
                if (count < 2 || alpha <= 0f || !line.sharedMaterial) { Hide(); return; }
                line.enabled = true; line.positionCount = count; line.widthMultiplier = width;
                color.a = Mathf.Clamp01(alpha); line.startColor = line.endColor = color;
                for (int i = 0; i < count; i++) line.SetPosition(i, points[i]);
            }
            internal void Loop(Vector3 center, Vector3 normal, float radius, float width, Color color, float alpha)
            {
                Vector3 u, v; Basis(normal, out u, out v);
                for (int i = 0; i < loop.Length; i++)
                {
                    float a = i * 2f * Mathf.PI / (loop.Length - 1);
                    loop[i] = center + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * radius;
                }
                Draw(loop, loop.Length, width, color, alpha);
            }
            internal void Hide() { line.enabled = false; }
        }

        private void EnsureBuilt()
        {
            if (root) return;
            body = GetComponent<CharacterBody>(); beam = GetComponent<GazeBeam>();
            palette = SkinFxPalette.ForBody(body);
            VfxAssets.Load();
            root = new GameObject("HS_GazeEmpowermentPool").transform;
            root.SetParent(transform, false);
            for (int i = 0; i < MaxCharges; i++)
            {
                fuel[i] = new Orb { stroke = NewStroke("Fuel", false) };
                reserve[i] = new Orb { stroke = NewStroke("Reserve", false) };
            }
            intakeTrail = NewStroke("RearIntake", false);
            crownRim = NewStroke("AbsorptionRim", false);
            for (int i = 0; i < pulses.Length; i++)
            {
                var pulse = new Pulse { sleeve = NewStroke("PulseSleeve", false), spine = NewStroke("PulseSpine", true), front = NewStroke("PulseFront", false) };
                // A travelling tapered sleeve, not a uniform full-length beam flare.
                pulse.sleeve.line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(0.55f, 0.6f), new Keyframe(0.85f, 1f), new Keyframe(1f, 0.15f));
                pulse.spine.line.widthCurve = pulse.sleeve.line.widthCurve;
                for (int j = 0; j < pulse.roots.Length; j++) pulse.roots[j] = new GroundStroke { stroke = NewStroke("GroundBranch", false) };
                pulses[i] = pulse;
            }
            for (int i = 0; i < contacts.Length; i++)
                contacts[i] = new Strike { glow = NewStroke("WeakStrike", false), core = NewStroke("WeakStrikeCore", true), branch = NewStroke("WeakStrikeBranch", false) };
        }

        private Stroke NewStroke(string name, bool core)
        { return new Stroke(root, name, palette.Material(core ? VfxAssets.ArcCore : VfxAssets.ArcGlow)); }

        private void HideTransient()
        {
            if (intakeTrail != null) intakeTrail.Hide();
            if (crownRim != null) crownRim.Hide();
            foreach (var p in pulses)
            {
                if (p == null) continue;
                p.active = false; p.sleeve.Hide(); p.spine.Hide(); p.front.Hide();
                foreach (var g in p.roots) g.stroke.Hide();
            }
            foreach (var s in contacts)
            {
                if (s == null) continue;
                s.active = false; s.glow.Hide(); s.core.Hide(); s.branch.Hide();
            }
        }
    }
}
