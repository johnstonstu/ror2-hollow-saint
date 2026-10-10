using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Thundercloud
{
    public sealed class ThundercloudFx : MonoBehaviour
    {
        private CharacterBody owner;
        private SkinFxPalette palette;
        private Vector3 from, sky;
        private float age, duration, radius, nextFlash;
        private ParticleSystem cloud;
        private LineRenderer crown;
        private Light glow;
        private static readonly System.Collections.Generic.List<ThundercloudFx> live = new System.Collections.Generic.List<ThundercloudFx>();
        private void OnDestroy() { live.Remove(this); }
        /// <summary>1.3.1: a dismissed storm fades now instead of at its scheduled end.</summary>
        private void Dismiss()
        {
            float end = Mathf.Max(age, ThundercloudSchedule.Ascent) + ThundercloudSchedule.Fade;
            if (end < duration) duration = end;
            nextFlash = float.MaxValue;
        }
        private void Start()
        {
            try
            {
                var data = GetComponent<EffectComponent>().effectData;
                if (data == null) { Destroy(gameObject); return; }
                var obj = data.ResolveNetworkedObjectReference(); owner = obj ? obj.GetComponent<CharacterBody>() : null;
                if (data.genericFloat < 0f)
                {
                    // Dismiss signal (same effect, negative duration): fade this owner's storm at that sky point.
                    foreach (var other in live.ToArray())
                        if (other && other.owner == owner && (other.sky - data.origin).sqrMagnitude < 4f) other.Dismiss();
                    ThundercloudCrownPose.Recall(owner);
                    Destroy(gameObject); return;
                }
                live.Add(this);
                from = data.start; sky = data.origin; radius = data.scale; duration = data.genericFloat;
                palette = SkinFxPalette.FromNetwork(data.color);
                ThundercloudCrownPose.Begin(owner, from, sky, duration);
                var root = new GameObject("HS_ThundercloudPuffs"); root.transform.SetParent(transform, false);
                cloud = root.AddComponent<ParticleSystem>(); cloud.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = cloud.main; main.loop = false; main.startSpeed = 0f; main.startLifetime = duration;
                main.maxParticles = 64; main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var emission = cloud.emission; emission.enabled = false;
                var shape = cloud.shape; shape.enabled = false;
                var renderer = cloud.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = StormVisualPrimitives.Smoke();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                StormVisualPrimitives.CloudVolume(root.transform, radius);
                var alpha = cloud.colorOverLifetime; alpha.enabled = true;
                var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                    new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.7f, .25f), new GradientAlphaKey(.7f, .65f), new GradientAlphaKey(0, 1) });
                alpha.color = gradient;
                cloud.Play();
                for (int i = 0; i < 48; i++)
                {
                    float a = i * 2.39996f, r = Mathf.Sqrt((i + .5f) / 48f) * radius * .75f;
                    cloud.Emit(new ParticleSystem.EmitParams { position = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(i * 1.7f) * radius * .06f, Mathf.Sin(a) * r),
                        startSize = radius * (.52f + .1f * Mathf.Sin(i)), startLifetime = duration,
                        startColor = new Color(.12f, .17f, .23f, .8f), velocity = Vector3.zero }, 1);
                }
                crown = StormVisualPrimitives.Ring(transform, palette);
                glow = root.AddComponent<Light>(); glow.color = palette.Arc; glow.range = radius; glow.shadows = LightShadows.None;
                Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_ThunderRelease" : "Play_loader_R_shock", gameObject);
            }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_THUNDERCLOUD_VISUAL " + error); Destroy(gameObject); }
        }
        private void Update()
        {
            if (!cloud) return;
            if (owner && (!owner.healthComponent || !owner.healthComponent.alive)) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            float ascent = Mathf.SmoothStep(0f, 1f, age / ThundercloudSchedule.Ascent);
            transform.position = Vector3.Lerp(from, sky, ascent);
            float fade = Mathf.Clamp01((age - (duration - ThundercloudSchedule.Fade)) / ThundercloudSchedule.Fade);
            cloud.transform.localScale = Vector3.one * Mathf.Lerp(.08f, 1f, ascent) * (1f - Mathf.SmoothStep(0f, 1f, fade));
            crown.enabled = age < ThundercloudSchedule.Ascent;
            if (crown) StormVisualPrimitives.SetRing(crown, transform.position, Mathf.Lerp(.8f, radius * .6f, ascent));
            glow.intensity = age < ThundercloudSchedule.Ascent ? ascent * 1.5f : .3f;
            if (age >= nextFlash && ascent > .6f && age < duration - ThundercloudSchedule.Fade)
            {
                nextFlash = age + .18f;
                Vector3 a = sky + new Vector3(Mathf.Sin(age * 9f) * .45f, -.16f, Mathf.Cos(age * 7f) * .45f) * radius;
                Vector3 b = sky + new Vector3(Mathf.Cos(age * 11f) * .4f, -.16f, Mathf.Sin(age * 5f) * .4f) * radius;
                LightningLine.Spawn(a, b, .13f, .12f, 1, palette: palette);
                glow.intensity = 2f;
            }
            if (age >= duration) Destroy(gameObject);
        }
    }
}
