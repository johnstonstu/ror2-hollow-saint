using System.Collections.Generic;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.HollowedOrb
{
    public sealed class HollowedOrbFlightFx : MonoBehaviour
    {
        private static readonly Dictionary<uint, HollowedOrbFlightFx> active = new Dictionary<uint, HollowedOrbFlightFx>();
        private uint id;
        private Vector3 to;
        private CharacterBody target;
        private float age, speed, diameter, nextArc;
        private GameObject ball;
        private SkinFxPalette palette;
        internal static void Stop(uint id)
        { if (active.TryGetValue(id, out var fx) && fx) Destroy(fx.gameObject); active.Remove(id); }
        private void Start()
        {
            try
            {
                var data = GetComponent<EffectComponent>().effectData;
                if (data == null) { Destroy(gameObject); return; }
                id = data.genericUInt; Stop(id); active[id] = this;
                to = data.start; speed = ChargedStormTuning.Bound(data.genericFloat, 10f, 80f); diameter = data.scale;
                var obj = data.ResolveNetworkedObjectReference();
                target = obj ? obj.GetComponent<CharacterBody>() : null;
                palette = SkinFxPalette.FromNetwork(data.color);
                ball = StormVisualPrimitives.Orb(palette, diameter); ball.transform.SetParent(transform, false);
                transform.position = data.origin;
                if (data.genericBool) Util.PlaySound(KitSfx.For(Beat.SpearThrow), gameObject);
            }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_HOLLOWED_ORB_FLIGHT_FX " + error); Destroy(gameObject); }
        }
        private void Update()
        {
            if (!ball) return;
            age += Time.deltaTime; Vector3 destination = target ? target.corePosition : to;
            Vector3 old = transform.position;
            transform.position = OrbFlightMotion.Advance(old, destination, speed, Time.deltaTime);
            ball.transform.localScale = Vector3.one * diameter * (1f + .035f * Mathf.Sin(age * 35f));
            if (age >= nextArc)
            {
                nextArc = age + .06f;
                if ((transform.position - old).sqrMagnitude > .001f)
                {
                    var side = Vector3.Cross((transform.position - old).normalized, Vector3.up).normalized * diameter * .3f;
                    LightningLine.Spawn(old + side, transform.position + side, .16f, diameter * .45f, 0, palette: palette).drawTime = 0f;
                    LightningLine.Spawn(old - side, transform.position - side, .12f, diameter * .3f, 0, palette: palette).drawTime = 0f;
                }
            }
            // Network impact or the next segment stops this effect; lifetime bounds orphaned packets.
            if (age >= 12.4f) Destroy(gameObject);
        }
        private void OnDestroy() { if (active.TryGetValue(id, out var fx) && fx == this) active.Remove(id); }
    }
    public sealed class HollowedOrbImpactFx : MonoBehaviour
    {
        private void Start()
        {
            try
            {
                var data = GetComponent<EffectComponent>().effectData;
                if (data == null) { Destroy(gameObject); return; }
                HollowedOrbFlightFx.Stop(data.genericUInt);
                if (!data.genericBool) { Destroy(gameObject); return; }
                var palette = SkinFxPalette.FromNetwork(data.color);
                float diameter = Mathf.Clamp(data.scale, .4f, 1.4f);
                for (int i = 0; i < 9; i++)
                {
                    float a = i * 2.39996f, y = 1f - 2f * (i + .5f) / 9f;
                    float radius = Mathf.Sqrt(1f - y * y);
                    var direction = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
                    LightningLine.Spawn(data.origin + direction * diameter * .15f,
                        data.origin + direction * diameter * (1.15f + (i % 3) * .2f),
                        .23f, diameter * .65f, 1, palette: palette).drawTime = .02f;
                }
                VfxParticles.Burst(data.origin, Quaternion.identity, palette.Material(VfxAssets.Spark), 18,
                    .28f, new Vector2(3f, 7f), new Vector2(.035f, .075f) * diameter, palette.Arc, .15f);
                VfxParticles.Ring(data.origin, Vector3.up, .15f, diameter * 1.5f, .3f, .065f, VfxAssets.Trail, palette);
                VfxParticles.Ring(data.origin, Vector3.forward, .1f, diameter * .9f, .22f, .04f, VfxAssets.Trail, palette);
                VfxParticles.FlashLight(data.origin, palette.Arc, 1.2f, diameter * 3.5f, .13f);
                KitSfx.Play(Beat.SpearStruck, gameObject);
                KitSfx.Play(Beat.BoltImpact, gameObject);
            }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_HOLLOWED_ORB_IMPACT_FX " + error); }
            Destroy(gameObject, .6f);
        }
    }
}
