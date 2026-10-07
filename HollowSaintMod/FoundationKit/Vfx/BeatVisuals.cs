using System;
using System.Collections;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{

    /// <summary>What each beat looks like. All visuals are independent GameObjects that
    /// clean themselves up, so nothing here depends on the effect prefab's lifetime.</summary>
    public static class BeatVisuals
    {
        /// <summary>Drops a point to the ground below it (within 4 m) so rings lie flat on the floor.</summary>
        private static Vector3 GroundUnder(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out hit, 4.5f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.08f;
            return p;
        }

        private static void ImpactFeel(Beat beat, Vector3 origin, CharacterBody body)
        {
            if (!ImpactFeelSettings.Enabled) return;
            float pause = 0f, amplitude = 0f, frequency = 0f, duration = 0f, radius = 0f;
            switch (beat)
            {
                case Beat.SpearImpact: pause = 0.06f; amplitude = 0.7f; frequency = 18f; duration = 0.22f; radius = 22f; break;
                case Beat.ThunderStrike: pause = 0.07f; amplitude = 1.6f; frequency = 13f; duration = 0.4f; radius = 40f; break;
                case Beat.SpearStruck: pause = 0.04f; amplitude = 0.4f; frequency = 20f; duration = 0.15f; radius = 16f; break;
                default: return;
            }
            var model = body && body.modelLocator ? body.modelLocator.modelTransform : null;
            var presentation = model ? model.GetComponent<HollowSaint.FoundationPresentation>() : null;
            if (presentation) presentation.HitPause(pause);
            ShakeEmitter.CreateSimpleShakeEmitter(origin, new Wave { amplitude = amplitude, frequency = frequency, cycleOffset = 0f }, duration, radius, true);
        }

        public static void Play(Beat beat, Vector3 origin, Vector3 start, float scale, CharacterBody body, SkinFxPalette palette = null)
        {
            palette = palette ?? SkinFxPalette.ForBody(body);
            ImpactFeel(beat, origin, body);
            switch (beat)
            {
                case Beat.ArcBoltCast:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.4f, 0.5f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.18f, new Vector2(4f, 9f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    {
                        Vector3 aim = Vector3.zero;
                        if (body && body.inputBank) aim = body.inputBank.aimDirection;
                        for (int i = 0; i < 2; i++)
                        {
                            Vector3 offset = UnityEngine.Random.onUnitSphere * 0.45f;
                            if (aim.sqrMagnitude > 0.01f) offset = aim * 0.4f + UnityEngine.Random.onUnitSphere * 0.22f;
                            LightningLine.Spawn(origin, origin + offset, 0.13f, 0.9f, 1, palette: palette);
                        }
                    }
                    break;

                case Beat.BoltImpact:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.09f, Vector2.zero, new Vector2(0.35f, 0.5f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.2f, new Vector2(4f, 9f), new Vector2(0.06f, 0.12f), palette.Core, stretch: 0.06f);
                    LightningLine.Spawn(origin, origin + UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.35f, 0.7f), 0.09f, 0.4f, 0, palette: palette);
                    VfxParticles.FlashLight(origin, palette.Arc, 0.6f, 2f, 0.09f);
                    break;

                case Beat.ChainHop:
                    if (ChainLightningFx.TryPlay(start, origin, scale, palette)) break;
                    // Playtest: hops were hard to see. Thicker, longer-lived main arc and a
                    // brighter lingering burn so the path between enemies reads.
                    LightningLine.Spawn(start, origin, 0.32f, 1.6f * scale, 2, 0.14f, palette: palette);
                    LightningLine.Spawn(start, origin, 0.7f, 0.5f * scale, 0, 0.03f, 10f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 12, 0.25f, new Vector2(6f, 16f), new Vector2(0.08f, 0.18f), palette.Core, stretch: 0.09f);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.45f, 0.55f), palette.Arc);
                    VfxParticles.FlashLight(origin, palette.Arc, 1.2f, 3.5f, 0.09f);
                    for (int i = 0; i < 2; i++)
                        LightningLine.Spawn(origin, origin + UnityEngine.Random.onUnitSphere * 0.35f, 0.08f, 0.5f, 0, palette: palette);
                    break;

                case Beat.SpearThrow:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.12f, Vector2.zero, new Vector2(0.5f, 0.6f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 10, 0.25f, new Vector2(5f, 12f), new Vector2(0.08f, 0.16f), palette.Arc, stretch: 0.09f);
                    break;

                case Beat.SpearImpact:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.18f, Vector2.zero, new Vector2(1.0f, 1.2f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 26, 0.45f, new Vector2(8f, 20f), new Vector2(0.1f, 0.18f), palette.Arc, stretch: 0.09f);
                    VfxParticles.Ring(origin, Vector3.up, 0.3f, 2.2f, 0.3f, 0.08f, palette.Material(VfxAssets.Trail), palette: palette);
                    for (int i = 0; i < 5; i++)
                        LightningLine.Spawn(origin, origin + UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(1.2f, 2.4f), 0.2f, 0.8f, 1, palette: palette);
                    VfxParticles.FlashLight(origin, palette.Arc, 3f, 6f, 0.25f);
                    break;

                case Beat.ArcStepStart:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 16, 0.3f, new Vector2(5f, 12f), new Vector2(0.08f, 0.18f), palette.Arc, stretch: 0.09f);
                    VfxParticles.Ring(origin + Vector3.up * 0.05f, Vector3.up, 0.2f, 1.2f, 0.22f, 0.05f, palette.Material(VfxAssets.Trail), palette: palette);
                    break;

                case Beat.ArcStepEnd:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 8, 0.22f, new Vector2(3f, 7f), new Vector2(0.08f, 0.14f), palette.Arc, stretch: 0.09f);
                    break;

                case Beat.CircuitUnfold:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 20, 0.6f, new Vector2(1f, 3f), new Vector2(0.08f, 0.16f), palette.Arc);
                    break;

                case Beat.CircuitOpen:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.25f, Vector2.zero, new Vector2(1.2f, 1.4f), palette.Arc);
                    VfxParticles.Ring(GroundUnder(origin), Vector3.up, 0.5f, KitTuning.OpenCircuitRadius, 0.45f, 0.1f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.FlashLight(origin, palette.Arc, 4f, 10f, 0.35f);
                    break;

                case Beat.CircuitClose:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 12, 0.4f, new Vector2(1f, 4f), new Vector2(0.08f, 0.14f), palette.Outer);
                    break;

                case Beat.CircuitPulse:
                    VfxParticles.Ring(GroundUnder(origin), Vector3.up, 0.6f, Mathf.Max(1f, scale), 0.3f, 0.04f, palette.Material(VfxAssets.Trail), palette: palette);
                    break;

                case Beat.SpearPulse:
                    // origin = anchor centre, start = spear butt, scale = radius. Deliberately faint.
                    VfxParticles.Ring(GroundUnder(origin), Vector3.up, Mathf.Max(1f, scale) * 0.82f, Mathf.Max(1f, scale), 0.4f, 0.03f, palette.Material(VfxAssets.Trail), palette: palette);
                    for (int i = 0; i < 2; i++)
                        LightningLine.Spawn(start, start + UnityEngine.Random.onUnitSphere * 0.7f, 0.1f, 0.25f, 0, palette: palette);
                    VfxParticles.FlashLight(start, palette.Arc, 1f, 4f, 0.1f);
                    break;

                case Beat.SpearPulseArc:
                    LightningLine.Spawn(start, origin, 0.12f, 0.3f, 0, 0.12f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 3, 0.15f, new Vector2(2f, 5f), new Vector2(0.05f, 0.1f), palette.Arc, stretch: 0.06f);
                    break;

                case Beat.SpearConduct:
                {
                    // start = Arc Bolt hit, origin = spear butt. The spear flares as it takes the current.
                    // scale 1 = spread followed, 0.6 = link only (nothing else in range).
                    float s = Mathf.Clamp(scale, 0.3f, 1f);
                    LightningLine.Spawn(start, origin, 0.22f, 0.9f * s, s >= 1f ? 1 : 0, 0.14f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.16f, Vector2.zero, new Vector2(0.9f, 1.1f) * s, palette.Arc);
                    LightningLine.Spawn(origin, origin + UnityEngine.Random.onUnitSphere * 0.5f, 0.12f, 0.3f, 0, 0.2f, palette: palette);
                    VfxParticles.FlashLight(origin, palette.Arc, 2.5f * s, 6f, 0.18f);
                    LanceGhost.FlareNear(origin, 2.5f, 0.5f * s);
                    break;
                }

                case Beat.SpearStruck:
                {
                    // origin = shaft point (in the ground or enemy), start = exposed butt.
                    // White-hot pop at the butt, crackle running the shaft, small fast ring:
                    // brighter and tighter than the faint radius-wide pulse ring.
                    Vector3 butt = start, tip = origin;
                    Vector3 axis = butt - tip;
                    VfxParticles.Burst(butt, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.14f, Vector2.zero, new Vector2(1.3f, 1.5f), palette.Core);
                    VfxParticles.Burst(butt, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(0.8f, 0.9f), palette.Arc);
                    VfxParticles.Burst(butt, Quaternion.identity, palette.Material(VfxAssets.Spark), 16, 0.3f, new Vector2(6f, 14f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    LightningLine.Spawn(tip, butt, 0.16f, 0.55f, 1, 0.22f, 0.03f, palette: palette);
                    LightningLine.Spawn(butt, tip, 0.1f, 0.35f, 0, 0.3f, 0.03f, palette: palette);
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 a = tip + axis * UnityEngine.Random.Range(0.1f, 0.9f);
                        LightningLine.Spawn(a, a + UnityEngine.Random.onUnitSphere * 0.45f, 0.1f, 0.25f, 0, 0.25f, palette: palette);
                    }
                    VfxParticles.Ring(GroundUnder(tip), Vector3.up, 0.25f, 2.4f, 0.22f, 0.09f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.FlashLight(butt, palette.Core, 3f, 7f, 0.14f);
                    LanceGhost.FlareNear(Vector3.Lerp(tip, butt, 0.5f), 3f, 1f);
                    break;
                }

                case Beat.SpearBurst:
                    // v0.9: origin = impact, start = surface normal, scale = burst radius (m).
                    Stormspear.Fx.SpearBurstFx.Play(origin, start, scale, palette);
                    break;

                case Beat.SpearSpread:
                    // start = spear butt, origin = spread target. Thick, branched, lingering.
                    LightningLine.Spawn(start, origin, 0.28f, 1.3f, 2, 0.16f, palette: palette);
                    LightningLine.Spawn(start, origin, 0.5f, 0.45f, 0, 0.03f, 10f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 10, 0.25f, new Vector2(5f, 12f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.45f, 0.55f), palette.Arc);
                    break;

                case Beat.SpearRecall:
                    // start = spear butt (where it leaves), origin = the hand it returns to.
                    var recall = LightningLine.Spawn(start, origin, 0.25f, 0.6f, 1, 0.1f, palette: palette);
                    if (body) recall.endAnchor = SpearDischarge.SpearCarry.GripSocketOf(body);
                    VfxParticles.Burst(start, Quaternion.identity, palette.Material(VfxAssets.Spark), 12, 0.3f, new Vector2(3f, 8f), new Vector2(0.08f, 0.14f), palette.Arc, stretch: 0.09f);
                    break;

                case Beat.CircuitArc:
                    // start = the point on the owner's halo ring nearest the target.
                    VfxParticles.Burst(start, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.22f, 0.3f), palette.Core);
                    LightningLine.Spawn(start, origin, 0.2f, 1.3f, 2, 0.16f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.2f, new Vector2(3f, 8f), new Vector2(0.08f, 0.14f), palette.Core, stretch: 0.09f);
                    break;

                case Beat.MeterFull:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.3f, Vector2.zero, new Vector2(0.8f, 0.9f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 18, 0.4f, new Vector2(2f, 6f), new Vector2(0.08f, 0.16f), palette.Arc);
                    break;

                case Beat.StaticTier:
                    // origin/start are the two ends of one small crackle arc; scale is the tier (1..4).
                    LightningLine.Spawn(origin, start, 0.12f, 0.12f + 0.03f * scale, 0, 0.2f, palette: palette);
                    if (scale >= 3f)
                        VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 3, 0.18f, new Vector2(1f, 3f), new Vector2(0.05f, 0.1f), palette.Arc, stretch: 0.06f);
                    break;

                case Beat.ChargeTick:
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.12f, Vector2.zero, new Vector2(0.3f, 0.4f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 6, 0.25f, new Vector2(2f, 5f), new Vector2(0.05f, 0.1f), palette.Arc, stretch: 0.06f);
                    break;

                case Beat.Electrocute:
                {
                    // scale = victim body radius. Small flash (max 1 m), arcs crawling over the body, sparks.
                    float r = Mathf.Clamp(scale, 0.3f, 3f);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.15f, Vector2.zero, new Vector2(0.7f, 1.0f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 16, 0.35f, new Vector2(4f, 10f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    int arcs = UnityEngine.Random.Range(3, 5);
                    for (int i = 0; i < arcs; i++)
                    {
                        Vector3 a = origin + UnityEngine.Random.onUnitSphere * r;
                        Vector3 b = origin + UnityEngine.Random.onUnitSphere * r;
                        LightningLine.Spawn(a, b, 0.2f, 0.32f, 0, 0.2f, palette: palette);
                    }
                    VfxParticles.FlashLight(origin, palette.Arc, 1.5f, 5f, 0.12f);
                    break;
                }

                case Beat.ElectrocuteArc:
                    // start = Electrocuted enemy, origin = pop target.
                    LightningLine.Spawn(start, origin, 0.2f, 0.5f, 2, 0.14f, palette: palette);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.1f, Vector2.zero, new Vector2(0.5f, 0.7f), palette.Arc);
                    VfxParticles.Burst(origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 10, 0.3f, new Vector2(5f, 12f), new Vector2(0.08f, 0.16f), palette.Core, stretch: 0.09f);
                    break;

                case Beat.ThunderTelegraph:
                {
                    // origin = target's feet, scale = seconds until the strike (v0.9.15). Ring shrinking
                    // inward on the ground until the bolt lands, flicker overhead.
                    Vector3 ground = GroundUnder(origin);
                    float until = scale > 0.05f && scale < 5f ? scale : KitTuning.ThunderboltTelegraphSeconds;
                    VfxParticles.Ring(ground, Vector3.up, 2.6f, 0.4f, until, 0.07f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.Ring(ground, Vector3.up, 1.6f, 0.2f, until, 0.04f, palette.Material(VfxAssets.Trail), palette: palette);
                    Vector3 sky = origin + Vector3.up * 25f;
                    LightningLine.Spawn(sky + UnityEngine.Random.insideUnitSphere * 3f, sky + Vector3.down * 8f + UnityEngine.Random.insideUnitSphere * 2f, 0.18f, 0.5f, 1, 0.15f, palette: palette);
                    VfxParticles.FlashLight(origin + Vector3.up * 12f, palette.Arc, 1.5f, 12f, 0.25f);
                    break;
                }

                case Beat.ThunderStrike:
                {
                    // origin = struck enemy's feet. Thick bolt from 25 m up, afterglow, ring, sparks, one light.
                    Vector3 ground = GroundUnder(origin);
                    Vector3 top = origin + Vector3.up * 25f;
                    LightningLine.Spawn(top, origin, 0.25f, 3f, 3, 0.05f, 0.05f, palette: palette);
                    LightningLine.Spawn(top, origin, 0.45f, 1.4f, 0, 0.04f, 0.09f, palette: palette);
                    VfxParticles.Ring(ground, Vector3.up, 0.4f, 3.5f, 0.35f, 0.12f, palette.Material(VfxAssets.Trail), palette: palette);
                    VfxParticles.Burst(origin + Vector3.up * 0.5f, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, 0.2f, Vector2.zero, new Vector2(1.1f, 1.4f), palette.Arc);
                    VfxParticles.Burst(origin + Vector3.up * 0.3f, Quaternion.identity, palette.Material(VfxAssets.Spark), 32, 0.6f, new Vector2(6f, 16f), new Vector2(0.08f, 0.18f), palette.Core, stretch: 0.09f);
                    VfxParticles.FlashLight(origin + Vector3.up * 1.5f, palette.Arc, 3f, 12f, 0.3f);
                    break;
                }
            }
        }
    }
}
