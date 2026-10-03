using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear.Fx
{
    /// <summary>
    /// v0.9.10 lodged Stormspear (Beat.SpearStuck). The thrown spear sticks where it hit: into the
    /// struck enemy (riding the nearest hurtbox, so it moves with the enemy) or into the ground. It
    /// crackles harder for the stick time, then the server's burst goes off and the spear burns out.
    /// Pure presentation on every machine; damage is SpearDetonation on the server.
    /// </summary>
    internal sealed class StuckSpearFx : MonoBehaviour
    {
        private const float BurnOut = 0.18f;   // after the burst: shrink and flare out
        private const float Bury = 0.3f;        // metres of the tip inside the target at full size

        private Transform anchor;
        private Vector3 localPos;
        private Quaternion localRot;
        private float life, age, size;
        private SpearVisual visual;
        private GameObject model;
        private SkinFxPalette palette;
        private float nextCrackle;

        internal static void Spawn(Vector3 point, Vector3 direction, float charge, float seconds, GameObject victim, SkinFxPalette palette)
        {
            if (!FoundationContent.SpearModel) return;
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.down;
            direction.Normalize();
            float size = Mathf.Lerp(0.6f, 1.3f, Mathf.Clamp01(charge));
            var root = new GameObject("HS_StuckSpear");
            root.transform.SetPositionAndRotation(point + direction * Bury * size, Quaternion.LookRotation(direction));
            var fx = root.AddComponent<StuckSpearFx>();
            fx.size = size;
            fx.life = Mathf.Max(0.05f, seconds);
            fx.palette = palette;

            // Same fitted model and arcs as the flying ghost (LanceGhost), so the spear in flight and
            // the spear in the target are one object to the eye.
            fx.model = Instantiate(FoundationContent.SpearModel, root.transform, false);
            var tipMarker = SpearDischarge.SpearCarry.Find(fx.model, "SpearTip");
            if (tipMarker) fx.model.transform.localRotation = Quaternion.FromToRotation(tipMarker.localPosition.normalized, Vector3.forward);
            fx.model.transform.localPosition = -Vector3.forward * (0.95f * size);
            fx.model.transform.localScale = Vector3.one * size;
            fx.visual = fx.model.AddComponent<SpearVisual>();
            fx.visual.Powered = true;
            fx.visual.PaletteOverride = palette;
            Ghosts.TintRoot(fx.model, palette);

            fx.anchor = NearestHurtBox(victim, point);
            if (fx.anchor)
            {
                fx.localPos = fx.anchor.InverseTransformPoint(root.transform.position);
                fx.localRot = Quaternion.Inverse(fx.anchor.rotation) * root.transform.rotation;
            }
            VfxParticles.FlashLight(point - direction * 0.4f * size, palette.Arc, 2.5f + 2f * charge, 3.5f + 2f * charge, fx.life);
            VfxParticles.Burst(point, Quaternion.identity, palette.Material(VfxAssets.Spark), 10, 0.3f, new Vector2(3f, 7f), new Vector2(0.05f, 0.12f), palette.Core, stretch: 0.08f);
        }

        /// <summary>The struck enemy's hurtbox closest to the impact, or null for a ground hit.</summary>
        private static Transform NearestHurtBox(GameObject victim, Vector3 point)
        {
            if (!victim) return null;
            var body = victim.GetComponent<CharacterBody>();
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            var group = model ? model.GetComponent<HurtBoxGroup>() : null;
            Transform best = body ? body.coreTransform : victim.transform;
            if (group == null || group.hurtBoxes == null) return best;
            float bestD = float.MaxValue;
            foreach (var box in group.hurtBoxes)
            {
                if (!box || !box.collider) continue;
                float d = (box.collider.ClosestPoint(point) - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = box.transform; }
            }
            return best;
        }

        private void LateUpdate()
        {
            age += Time.deltaTime;
            if (anchor) transform.SetPositionAndRotation(anchor.TransformPoint(localPos), anchor.rotation * localRot);
            if (age < life)
            {
                // Charging up to the burst: the sheath arcs swell.
                if (visual) visual.Gain = 1f + 1.6f * (age / life);
                // v0.9.16: the struck enemy crackles in 3D around the lodged tip until the burst.
                if (age >= nextCrackle && palette != null)
                {
                    nextCrackle = age + 0.04f;
                    Vector3 tip = transform.position;
                    for (int i = 0; i < 2; i++)
                    {
                        var arc = LightningLine.Spawn(tip, tip + Random.onUnitSphere * Random.Range(0.6f, 1.4f) * size, 0.08f, 0.35f + 0.2f * size, 1, 0.25f, palette: palette);
                        arc.drawTime = 0.02f;
                    }
                }
                return;
            }
            float t = (age - life) / BurnOut;
            if (t >= 1f) { Destroy(gameObject); return; }
            if (visual) visual.Gain = 2.6f * (1f - t);
            if (model) model.transform.localScale = Vector3.one * size * Mathf.Lerp(1f, 0.2f, t);
        }
    }
}
