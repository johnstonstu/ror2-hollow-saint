using HollowSaint.FoundationKit.Vfx;
using UnityEngine;

namespace HollowSaint.FoundationKit.OpenCircuit.Fx
{
    // Runtime-only half of the ground crawl (uses particle helpers the test stubs do not provide).
    public sealed partial class OpenCircuitDomeFx
    {
        partial void GroundTipContact(Vector3 tip, Vector3 previous)
        {
            if (palette == null || !float.IsFinite(tip.x) || (tip - previous).sqrMagnitude < 0.0001f) return;
            Vector3 along = (tip - previous).normalized;
            // Small, low and arc-tinted: the crawl "bites" the ground, it does not explode.
            VfxParticles.Burst(tip, Quaternion.LookRotation(along + Vector3.up * .6f), palette.Material(VfxAssets.Spark), 5, .22f,
                new Vector2(1.5f, 3.5f), new Vector2(.05f, .09f), palette.Arc, stretch: .05f, spreadAngle: 55f);
            VfxParticles.Burst(tip + Vector3.up * .05f, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .12f,
                Vector2.zero, new Vector2(.45f, .6f), palette.Arc);
        }
    }
}
