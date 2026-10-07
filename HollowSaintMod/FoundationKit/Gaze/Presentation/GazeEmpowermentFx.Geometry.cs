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
            internal void Arc(Vector3 center, Vector3 normal, float radius, float angle, float sweep, float width, Color color, float alpha)
            {
                Vector3 u, v; Basis(normal, out u, out v);
                for (int i = 0; i < loop.Length; i++)
                {
                    float t = i / (float)(loop.Length - 1), a = angle + sweep * t;
                    float r = radius * (1f - .025f * Mathf.Sin(t * Mathf.PI) * Mathf.Sin(i * 2.7f + angle));
                    r = Mathf.Min(radius, r);
                    loop[i] = center + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * r;
                }
                Draw(loop, loop.Length, width, color, alpha);
            }
            // Center bolt follows buff_discharge_charge.png; a separate stroke avoids a ring connector.
            internal void Bolt(Vector3 center, Vector3 normal, float radius, float width, Color color, float alpha)
            {
                Vector3 u, v; Basis(normal, out u, out v);
                loop[0] = center + (u * 0.14f + v * 0.55f) * radius;
                loop[1] = center + (-u * 0.24f - v * 0.04f) * radius;
                loop[2] = center + (u * 0.22f + v * 0.04f) * radius;
                loop[3] = center + (-u * 0.14f - v * 0.55f) * radius;
                Draw(loop, 4, width, color, alpha);
            }
            // An open, irregular cluster of four crackling arms, not a closed ring/glyph.
            // Outline and energy use the same seed and clock so their silhouettes agree.
            internal void Knot(Vector3 center, Vector3 normal, float radius, float phase, float width, Color color, float alpha, bool reduced)
            {
                Vector3 u, v; Basis(normal, out u, out v);
                int tick = (int)(Time.time * (reduced ? 6f : 12f));
                for (int i = 0; i < loop.Length; i++)
                {
                    int arm = i / 4, step = i % 4;
                    float a = phase + arm * 1.63f + 0.32f * Mathf.Sin(i * 2.7f + tick + phase);
                    float r = (step == 0 ? 0.12f : step == 1 ? 0.5f : step == 2 ? 0.94f : 0.38f);
                    r *= 0.84f + 0.16f * Mathf.Sin(i * 4.1f + tick + phase);
                    loop[i] = center + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * (radius * r);
                }
                Draw(loop, loop.Length, width, color, alpha);
            }
            // One short crawling filament follows the energy knot.
            // Reuse this stroke's buffer, with bounded radial teeth and no random state.
            internal void Crawl(Vector3 center, Vector3 normal, float radius, float phase, Color color, float alpha)
            {
                Vector3 u, v; Basis(normal, out u, out v);
                int tick = (int)(Time.time * 18f);
                for (int i = 0; i < 9; i++)
                {
                    float a = phase + i * 0.28f;
                    float tooth = i == 0 || i == 8 ? 1f : 0.88f + 0.22f * Mathf.Sin(i * 2.4f + tick);
                    loop[i] = center + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * (radius * tooth);
                }
                Draw(loop, 9, 0.025f, color, alpha);
            }
            internal void Diamond(Vector3 center, Vector3 normal, float radius, float width, Color color, float alpha)
            {
                Vector3 u, v; Basis(normal, out u, out v);
                loop[0] = center + v * radius * 1.25f; loop[1] = center + u * radius;
                loop[2] = center - v * radius * 1.25f; loop[3] = center - u * radius; loop[4] = loop[0];
                Draw(loop, 5, width, color, alpha);
            }
        }

        private void EnsureBuilt()
        {
            if (root) return;
            body = GetComponent<CharacterBody>(); beam = GetComponent<GazeBeam>();
            palette = SkinFxPalette.ForBody(body);
            VfxAssets.Load();
            GazeContrastAssets.Load();
            root = new GameObject("HS_GazeEmpowermentPool").transform;
            root.SetParent(transform, false);
            for (int i = 0; i < MaxCharges; i++)
            {
                fuel[i] = new Orb { stroke = NewStroke("Fuel", false), outline = NewOutline("FuelOutline"), filament = NewStroke("FuelFilament", true) };
                reserve[i] = new Orb { stroke = NewStroke("Reserve", false), outline = NewOutline("ReserveOutline"), filament = NewStroke("ReserveFilament", true) };
            }
            for (int i = 0; i < intakeTrails.Length; i++)
            {
                intakeTrails[i] = NewStroke("RearIntake", false);
                intakeOutlines[i] = NewOutline("RearIntakeOutline");
                intakeFilaments[i] = NewStroke("RearIntakeFilament", true);
            }
            crownRim = NewStroke("AbsorptionRim", false);
            for (int i = 0; i < chargingArcs.Length; i++) chargingArcs[i] = NewStroke("ChargingCrownArc", false);
            for (int i = 0; i < pulses.Length; i++)
            {
                var pulse = new Pulse { sleeve = NewStroke("PulseSleeve", false), spine = NewStroke("PulseSpine", true), front = NewStroke("PulseFront", false), glyph = NewStroke("PulseChargeBolt", true), outline = NewOutline("PulseOutline") };
                for (int j = 0; j < pulse.wave.Length; j++) pulse.wave[j] = NewStroke("ReleasedCrownArc", false);
                // A travelling tapered sleeve, not a uniform full-length beam flare.
                pulse.sleeve.line.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.18f), new Keyframe(0.8f, 0.9f), new Keyframe(0.95f, 1f), new Keyframe(1f, 0.1f));
                pulse.spine.line.widthCurve = pulse.sleeve.line.widthCurve;
                pulse.outline.line.widthCurve = pulse.sleeve.line.widthCurve;
                for (int j = 0; j < pulse.forks.Length; j++) pulse.forks[j] = NewStroke("SurgeFork", true);
                for (int j = 0; j < pulse.roots.Length; j++) pulse.roots[j] = new GroundStroke { stroke = NewStroke("GroundBranch", false), outline = NewOutline("GroundOutline") };
                pulses[i] = pulse;
            }
            for (int i = 0; i < contacts.Length; i++)
                contacts[i] = new Strike { glow = NewStroke("WeakStrike", false), core = NewStroke("WeakStrikeCore", true), branch = NewStroke("WeakStrikeBranch", false), outline = NewOutline("StrikeOutline"), stamp = NewStroke("StrikeStamp", false), glyph = NewStroke("StrikeChargeBolt", true) };
        }

        private Stroke NewStroke(string name, bool core)
        { return new Stroke(root, name, core ? GazeContrastAssets.Core : GazeContrastAssets.Glow); }
        private Stroke NewOutline(string name) { return new Stroke(root, name, GazeContrastAssets.Outline); }

        private void HideTransient()
        {
            for (int i = 0; i < intakeTrails.Length; i++)
            {
                if (intakeTrails[i] != null) intakeTrails[i].Hide();
                if (intakeOutlines[i] != null) intakeOutlines[i].Hide();
                if (intakeFilaments[i] != null) intakeFilaments[i].Hide();
            }
            foreach (var orb in fuel) if (orb != null) orb.filament.Hide();
            foreach (var orb in reserve) if (orb != null) orb.filament.Hide();
            if (crownRim != null) crownRim.Hide();
            foreach (var arc in chargingArcs) if (arc != null) arc.Hide();
            foreach (var p in pulses)
            {
                if (p == null) continue;
                p.active = false; p.sleeve.Hide(); p.spine.Hide(); p.front.Hide(); p.glyph.Hide(); p.outline.Hide();
                foreach (var fork in p.forks) fork.Hide();
                foreach (var wave in p.wave) wave.Hide();
                foreach (var g in p.roots) { g.stroke.Hide(); g.outline.Hide(); }
            }
            foreach (var s in contacts)
            {
                if (s == null) continue;
                s.active = false; s.glow.Hide(); s.core.Hide(); s.branch.Hide(); s.outline.Hide(); s.stamp.Hide(); s.glyph.Hide();
            }
        }
    }
}
