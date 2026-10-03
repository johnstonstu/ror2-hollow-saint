using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>
    /// Local visual launched when the server commits a Thunderbolt. v0.9.15 (Stu: "a lot slower and
    /// more dramatic"): the flight now spans KitTuning.ThunderboltFlightSeconds (was a fixed 0.25 s)
    /// in four beats, as fractions of that time:
    ///   rise   0.00-0.35  the fused charge climbs off the crown on a lightning tether, growing;
    ///   hold   0.35-0.55  it hangs at the apex, swells and crackles (the "answered" beat);
    ///   travel 0.55-0.85  it streaks across the sky, leaving a lightning trail, to above the target;
    ///   hover  0.85-1.00  it sits over the target throwing short leaders down, then the server's
    ///                     strike (RoyalCapacitorFx) lands on the target and this orb is gone.
    /// The end point follows the targeted enemy (its nearest hurtbox), like the server impact does.
    /// </summary>
    internal sealed class StormChargeRelease : MonoBehaviour
    {
        private const float RiseEnd = 0.35f, HoldEnd = 0.55f, TravelEnd = 0.85f;
        private const float ApexHeight = 9f, SkyHeight = 16f, ArcLift = 7f;

        private Transform crown, targetAnchor;
        private Vector3 targetOffset, targetPoint, apex, skyFrom;
        private SkinFxPalette palette;
        private GameObject soundSource;
        private Light glow;
        private float duration, age, nextCrackle;
        private bool heldBeat, travelBeat;
        private Vector3 lastTrail;

        internal static void Play(Vector3 from, Vector3 target, SkinFxPalette palette, float duration, Transform crown, GameObject soundSource)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "HS_StormReleasedCharge";
            go.transform.position = from;
            go.transform.localScale = Vector3.one * 0.26f;
            var collider = go.GetComponent<Collider>();
            if (collider) { collider.enabled = false; Destroy(collider); }
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = palette.Material(VfxAssets.ArcCore);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var release = go.AddComponent<StormChargeRelease>();
            release.palette = palette;
            release.duration = Mathf.Max(0.2f, duration);
            release.crown = crown;
            release.soundSource = soundSource;
            release.apex = from + Vector3.up * ApexHeight;
            release.targetPoint = target;
            release.targetAnchor = NearestHurtBox(target);
            if (release.targetAnchor) release.targetOffset = target - release.targetAnchor.position;
            release.glow = go.AddComponent<Light>();
            release.glow.type = LightType.Point;
            release.glow.color = palette.Arc;
            release.glow.range = 7f;
            release.glow.intensity = 2f;
            release.glow.shadows = LightShadows.None;

            // Leaving the body: a flash at the crown and a thick tether that stays hooked to the halo.
            VfxParticles.Burst(from, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(0.6f, 0.8f), palette.Core);
            VfxParticles.Burst(from, Quaternion.identity, palette.Material(VfxAssets.Spark), 14, 0.4f, new Vector2(3f, 8f), new Vector2(0.05f, 0.12f), palette.Core, stretch: 0.08f);
            var tether = LightningLine.Spawn(from, release.apex, release.duration * HoldEnd + 0.08f, 1.0f, 1, 0.08f, palette: palette);
            tether.drawTime = 0f;
            tether.startAnchor = crown;
            tether.endAnchor = go.transform;
            Destroy(go, release.duration + 0.05f);
        }

        private static Transform NearestHurtBox(Vector3 point)
        {
            Transform best = null;
            float bestD = 9f; // within 3 m of the server's aim point
            var hits = Physics.OverlapSphere(point, 3f, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                var box = hit ? hit.GetComponent<HurtBox>() : null;
                if (!box) continue;
                float d = (hit.ClosestPoint(point) - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = box.transform; }
            }
            return best;
        }

        private Vector3 Target { get { return targetAnchor ? targetAnchor.position + targetOffset : targetPoint; } }

        private void Update()
        {
            age += Time.deltaTime;
            float t = age / duration;
            Vector3 sky = Target + Vector3.up * SkyHeight;
            Vector3 pos;
            float size;
            if (t < RiseEnd)
            {
                float p = Mathf.SmoothStep(0f, 1f, t / RiseEnd);
                Vector3 from = crown ? crown.position : apex - Vector3.up * ApexHeight;
                pos = Vector3.Lerp(from, apex, p);
                size = Mathf.Lerp(0.26f, 0.5f, p);
                Crackle(pos, 0.09f, 1.2f, 2.2f, 0.1f);
            }
            else if (t < HoldEnd)
            {
                if (!heldBeat)
                {
                    heldBeat = true;
                    VfxParticles.Burst(apex, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.25f, Vector2.zero, new Vector2(1.4f, 1.9f), palette.Arc);
                    VfxParticles.Ring(apex, Vector3.up, 0.4f, 3.2f, 0.35f, 0.08f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.FlashLight(apex, palette.Arc, 3.5f, 14f, 0.3f);
                    if (soundSource) Util.PlaySound("Play_captain_m2_tazer_bounce", soundSource);
                }
                float p = (t - RiseEnd) / (HoldEnd - RiseEnd);
                pos = apex + Vector3.up * (0.4f * Mathf.Sin(p * Mathf.PI));
                size = 0.5f + 0.25f * p + 0.08f * Mathf.Sin(age * 40f);
                Crackle(pos, 0.05f, 1.8f, 3.5f, 0.12f);
                skyFrom = pos;
            }
            else if (t < TravelEnd)
            {
                if (!travelBeat) { travelBeat = true; lastTrail = skyFrom; }
                float p = Mathf.SmoothStep(0f, 1f, (t - HoldEnd) / (TravelEnd - HoldEnd));
                Vector3 ctrl = (skyFrom + sky) * 0.5f + Vector3.up * ArcLift;
                float u = 1f - p;
                pos = u * u * skyFrom + 2f * u * p * ctrl + p * p * sky;
                size = 0.6f;
                if ((pos - lastTrail).sqrMagnitude > 0.8f)
                {
                    var trail = LightningLine.Spawn(lastTrail, pos, 0.35f, 0.9f, 0, 0.1f, palette: palette);
                    trail.drawTime = 0f;
                    lastTrail = pos;
                }
            }
            else
            {
                float p = Mathf.Clamp01((t - TravelEnd) / (1f - TravelEnd));
                pos = sky;
                size = Mathf.Lerp(0.6f, 0.85f, p) + 0.08f * Mathf.Sin(age * 55f);
                // Leaders reaching down for the target just before the bolt.
                if (age >= nextCrackle)
                {
                    nextCrackle = age + 0.045f;
                    Vector3 down = Vector3.Lerp(pos, Target, Random.Range(0.25f, 0.6f)) + Random.insideUnitSphere * 1.5f;
                    LightningLine.Spawn(pos, down, 0.09f, 0.6f, 1, 0.2f, palette: palette).drawTime = 0.03f;
                }
            }
            transform.position = pos;
            transform.localScale = Vector3.one * size;
            if (glow) glow.intensity = 2f + 3f * size;
        }

        /// <summary>Short arcs jumping off the orb in random directions.</summary>
        private void Crackle(Vector3 at, float interval, float minLen, float maxLen, float life)
        {
            if (age < nextCrackle) return;
            nextCrackle = age + interval;
            Vector3 dir = Random.onUnitSphere;
            LightningLine.Spawn(at, at + dir * Random.Range(minLen, maxLen), life, 0.45f, 1, 0.25f, palette: palette).drawTime = 0.02f;
        }
    }
}
