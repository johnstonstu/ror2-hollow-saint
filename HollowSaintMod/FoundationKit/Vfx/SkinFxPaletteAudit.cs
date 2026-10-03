using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Startup checks use actual Unity materials and RoR2 packet serialization.</summary>
    internal static class SkinFxPaletteAudit
    {
        internal static void Verify()
        {
            for (uint i = 0; i < 5; i++)
            {
                var palette = SkinFxPalette.ForIndex(i);
                var sent = new EffectData { color = palette.NetworkColor, genericUInt = (uint)Beat.ChainHop,
                    start = new Vector3(1f, 2f, 3f), scale = 0.8f, genericFloat = 0.14f };
                var writer = new NetworkWriter();
                sent.Serialize(writer);
                var received = new EffectData();
                received.Deserialize(new NetworkReader(writer.ToArray()));
                Require(SkinFxPalette.FromNetwork(received.color).Index == (i == 1 ? 0 : (int)i), "network palette " + i);
                Require(received.genericUInt == sent.genericUInt && received.start == sent.start &&
                    received.scale == sent.scale && received.genericFloat == sent.genericFloat, "packet fields " + i);
            }
            Require(SkinFxPalette.ForIndex(999).Index == 0 && SkinFxPalette.FromNetwork(Color.white).Index == 0, "fallback");
            var source = VfxAssets.ArcGlow;
            Require(source, "source material unavailable");
            var before = source.HasProperty("_TintColor") ? source.GetColor("_TintColor") : Color.white;
            var ramp = source.HasProperty("_RemapTex") ? source.GetTexture("_RemapTex") : null;
            var purple = SkinFxPalette.ForIndex(4);
            var green = SkinFxPalette.ForIndex(2);
            var p = purple.Material(source);
            var g = green.Material(source);
            Require(p && g && p != source && g != source && p != g, "isolated materials");
            Require(purple.Material(source) == p && purple.Material(p) == p && green.Material(p) == g &&
                SkinFxPalette.ForIndex(0).Material(p) == source, "canonical material cache");
            Require(!source.HasProperty("_TintColor") || source.GetColor("_TintColor") == before, "source tint mutation");
            Require(!source.HasProperty("_RemapTex") || source.GetTexture("_RemapTex") == ramp, "source ramp mutation");
            var colors = new ParticleSystem.MinMaxGradient(new Color(0f, 1f, 1f, 0.2f), new Color(1f, 1f, 1f, 0.7f));
            var tinted = purple.TintParticleColor(colors);
            Require(tinted.mode == colors.mode && tinted.colorMin.a == 0.2f && tinted.colorMax.a == 0.7f, "two-color alpha");
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.15f, 0f), new GradientAlphaKey(0f, 1f) });
            var gradients = new ParticleSystem.MinMaxGradient(gradient, gradient);
            Color originalGradientStart = gradient.Evaluate(0f);
            var tintedGradients = purple.TintParticleColor(gradients);
            Require(tintedGradients.mode == gradients.mode &&
                tintedGradients.gradientMin.Evaluate(0f).a == gradient.Evaluate(0f).a &&
                tintedGradients.gradientMax.Evaluate(1f).a == gradient.Evaluate(1f).a &&
                gradient.Evaluate(0f) == originalGradientStart, "two-gradient alpha/source isolation");
            Plugin.Log.LogInfo("HOLLOW_SAINT_SKIN_FX_CHECK_PASS themes=5 packetRoundTrips=5 materialIsolation=true alphaModes=true");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("Skin FX contract: " + message);
        }
    }
}
