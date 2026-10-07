using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze.Fx
{
    /// <summary>1.2 Gaze charge-up presentation (runs on every machine from GazeState's charge
    /// phase). Each absorbed Static Charge spirals from a ring around the Saint into the crown
    /// over the head on a lightning tether; its rising power-up cue starts with the flight so the
    /// cue's end snap lands with the orb. The crown glow, light and body-to-crown arcs grow with
    /// every charge. Purely cosmetic: the server ledger spends the charges at ignition.</summary>
    public sealed class GazeChargeUpFx : MonoBehaviour
    {
        private const float Flight = 0.26f, RingRadius = 1.7f;
        private CharacterBody body;
        private SkinFxPalette palette;
        private int available, absorbed;
        private GameObject glowBall, emitter;
        private Light glow;
        private float nextArc, born, endAt = -1f;
        private readonly Transform[] orbs = new Transform[20];
        private readonly Vector3[] orbFrom = new Vector3[20];
        private readonly float[] orbAt = new float[20];

        public static GazeChargeUpFx Begin(CharacterBody body, int available)
        {
            if (!body) return null;
            var go = new GameObject("HS_GazeChargeUp");
            var fx = go.AddComponent<GazeChargeUpFx>();
            fx.body = body; fx.available = Mathf.Clamp(available, 1, 20); fx.born = Time.time;
            fx.palette = SkinFxPalette.ForBody(body);
            fx.emitter = new GameObject("HS_GazeChargeEmitter");
            fx.emitter.transform.SetParent(body.gameObject.transform, false);
            fx.glowBall = Ball(fx.palette, 0.18f);
            fx.glow = fx.glowBall.AddComponent<Light>();
            fx.glow.type = LightType.Point; fx.glow.color = fx.palette.Arc; fx.glow.range = 4f; fx.glow.intensity = 0.6f;
            fx.glow.shadows = LightShadows.None;
            return fx;
        }

        private static GameObject Ball(SkinFxPalette palette, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "HS_GazeChargeOrb";
            var collider = go.GetComponent<Collider>(); if (collider) { collider.enabled = false; Destroy(collider); }
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = palette.Material(VfxAssets.ArcCore);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            go.transform.localScale = Vector3.one * size;
            return go;
        }

        private Vector3 Crown => body ? HaloRing.CenterOf(body) + Vector3.up * 0.25f : transform.position;

        /// <summary>Charge n (1-based) starts its flight into the crown.</summary>
        public void Absorb(int n)
        {
            if (!body || n < 1 || n > orbs.Length) return;
            absorbed = Mathf.Max(absorbed, n);
            float angle = (n - 1) * Mathf.PI * 2f / Mathf.Max(3, available) + born;
            Vector3 from = body.corePosition + new Vector3(Mathf.Cos(angle), -0.2f, Mathf.Sin(angle)) * RingRadius;
            var orb = Ball(palette, 0.22f + 0.02f * n).transform;
            orb.position = from;
            orbs[n - 1] = orb; orbFrom[n - 1] = from; orbAt[n - 1] = Time.time;
            VfxParticles.Burst(from, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .15f, Vector2.zero, new Vector2(.6f, .8f), palette.Arc);
            int tier = Mathf.Clamp(n, 1, 5);
            Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_GazeLoad" + tier : "Play_HS_ChargeTick", emitter);
        }

        private void Arrive(int n)
        {
            Vector3 c = Crown;
            float k = Mathf.Clamp(n, 1, 5);
            VfxParticles.Burst(c, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .18f, Vector2.zero, Vector2.one * (.9f + .25f * k), palette.Core);
            VfxParticles.Burst(c, Quaternion.identity, palette.Material(VfxAssets.Spark), 8 + 3 * n, .3f, new Vector2(2f, 5f + k), new Vector2(.05f, .1f), palette.Arc, stretch: .06f);
            VfxParticles.Ring(c, Vector3.up, .25f, .9f + .25f * k, .25f, .05f + .01f * k, palette.Material(VfxAssets.Trail), palette);
        }

        public void End(int count, bool fired)
        {
            if (endAt >= 0f) return;
            endAt = Time.time;
            if (fired && body && count > 0)
            {
                Vector3 c = Crown;
                VfxParticles.Burst(c, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .3f, Vector2.zero, Vector2.one * (1.6f + .4f * count), palette.Core);
                VfxParticles.Burst(c, Quaternion.identity, palette.Material(VfxAssets.Spark), 14 + 5 * count, .4f, new Vector2(4f, 10f), new Vector2(.06f, .12f), palette.Arc, stretch: .07f);
                VfxParticles.FlashLight(c, palette.Arc, 3f + count, 8f + count, .3f);
            }
            for (int i = 0; i < orbs.Length; i++) if (orbs[i]) Destroy(orbs[i].gameObject);
            if (glowBall) Destroy(glowBall);
            if (emitter) Destroy(emitter, 1.5f);
            Destroy(gameObject, 0.05f);
        }

        private void Update()
        {
            if (!body || !body.healthComponent || !body.healthComponent.alive) { End(absorbed, false); return; }
            if (endAt >= 0f) return;
            Vector3 c = Crown;
            float t = Time.time;
            // Orbs spiral up into the crown on a tether.
            for (int i = 0; i < orbs.Length; i++)
            {
                var orb = orbs[i];
                if (!orb) continue;
                float u = Mathf.Clamp01((t - orbAt[i]) / Flight);
                if (u >= 1f) { Destroy(orb.gameObject); orbs[i] = null; Arrive(i + 1); continue; }
                float e = u * u * (3f - 2f * u);
                Vector3 axis = c - orbFrom[i];
                Vector3 side = Vector3.Cross(axis.normalized, Vector3.up);
                Vector3 p = Vector3.Lerp(orbFrom[i], c, e) + side * Mathf.Sin(u * Mathf.PI) * 0.6f + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.5f;
                orb.position = p;
                if (Random.value < 0.6f) LightningLine.Spawn(p, c, 0.05f, 0.06f, 0, 0.18f, palette: palette);
            }
            // Crown glow and light grow with every absorbed charge.
            float k = absorbed / (float)Mathf.Max(1, available);
            float pulse = 1f + 0.12f * Mathf.Sin(t * (14f + 10f * k));
            glowBall.transform.position = c;
            glowBall.transform.localScale = Vector3.one * (0.18f + 0.07f * absorbed) * pulse;
            glow.intensity = 0.6f + 1.2f * absorbed;
            glow.range = 4f + 1.2f * absorbed;
            // Body-to-crown crackle: denser and thicker as the bank fills.
            if (t >= nextArc && absorbed > 0)
            {
                nextArc = t + Mathf.Lerp(0.12f, 0.04f, k);
                Vector3 right = body.gameObject.transform.right;
                Vector3 hand = body.corePosition + right * (Random.value < 0.5f ? -0.45f : 0.45f) + Vector3.up * 0.1f;
                LightningLine.Spawn(hand, c, 0.07f + 0.02f * k, 0.05f + 0.025f * absorbed, absorbed >= 3 ? 1 : 0, 0.22f, palette: palette);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < orbs.Length; i++) if (orbs[i]) Destroy(orbs[i].gameObject);
            if (glowBall) Destroy(glowBall);
        }
    }
}
