using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Small helpers that build one-shot or looping particle systems in code.
    /// Particles billboard themselves, so flashes and sparks need no camera math.</summary>
    public static class VfxParticles
    {
        /// <summary>One-shot burst that destroys itself.</summary>
        public static ParticleSystem Burst(Vector3 position, Quaternion rotation, Material material, int count,
            float lifetime, Vector2 speed, Vector2 size, Color color, float stretch = 0f, float spreadAngle = 180f)
        {
            var go = new GameObject("HS_Burst");
            go.transform.SetPositionAndRotation(position, rotation);
            var ps = Configure(go, material, lifetime, speed, size, color, stretch, spreadAngle, looping: false);
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ps.Play();
            Object.Destroy(go, lifetime + 0.5f);
            return ps;
        }

        /// <summary>Looping emitter parented to a transform (caller controls lifetime).</summary>
        public static ParticleSystem Loop(Transform parent, Material material, float rate, float lifetime,
            Vector2 speed, Vector2 size, Color color, float stretch = 0f, float spreadAngle = 180f)
        {
            var go = new GameObject("HS_Loop");
            go.transform.SetParent(parent, false);
            var ps = Configure(go, material, lifetime, speed, size, color, stretch, spreadAngle, looping: true);
            var emission = ps.emission;
            emission.rateOverTime = rate;
            ps.Play();
            return ps;
        }

        private static ParticleSystem Configure(GameObject go, Material material, float lifetime, Vector2 speed,
            Vector2 size, Color color, float stretch, float spreadAngle, bool looping)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = Mathf.Max(0.05f, lifetime);
            main.loop = looping;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 200;
            main.gravityModifier = 0f;
            // Sparks (bursts with real speed) fall a little and lose speed fast so they read
            // as flung embers; flash bursts (speed 0) and loops keep the plain behaviour.
            bool spark = !looping && speed.y > 0.01f;
            if (spark) main.gravityModifier = 0.5f;

            var shape = ps.shape;
            shape.enabled = true;
            if (spreadAngle >= 179f)
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.05f;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = spreadAngle;
                shape.radius = 0.02f;
            }

            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            if (spark)
            {
                var palette = SkinFxPalette.ForMaterial(material);
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(palette.Arc, 0.55f), new GradientColorKey(palette.Outer, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });

                var limit = ps.limitVelocityOverLifetime;
                limit.enabled = true;
                limit.dampen = 0.15f;
                limit.limit = new ParticleSystem.MinMaxCurve(3f);
            }
            else
            {
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            }
            fade.color = gradient;

            var shrink = ps.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (stretch > 0f)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = stretch;
                renderer.lengthScale = 1f;
            }
            else
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
            return ps;
        }

        /// <summary>A short-lived point light that fades out.</summary>
        public static void FlashLight(Vector3 position, Color color, float intensity, float range, float duration)
        {
            var go = new GameObject("HS_FlashLight");
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            go.AddComponent<LightFader>().Init(intensity, duration);
        }

        private sealed class LightFader : MonoBehaviour
        {
            private Light target;
            private float start;
            private float duration;
            private float age;

            public void Init(float intensity, float seconds)
            {
                target = GetComponent<Light>();
                start = intensity;
                duration = Mathf.Max(0.01f, seconds);
            }

            private void Update()
            {
                age += Time.deltaTime;
                float t = age / duration;
                if (t >= 1f) { Destroy(gameObject); return; }
                if (target) target.intensity = start * (1f - t) * (1f - t);
            }
        }

        /// <summary>A flat circle of LineRenderer points that expands and fades.</summary>
        public static void Ring(Vector3 center, Vector3 normal, float startRadius, float endRadius, float duration, float width, Material material, SkinFxPalette palette = null)
        {
            var go = new GameObject("HS_Ring");
            go.transform.position = center;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, normal);
            palette = palette ?? SkinFxPalette.ForIndex(0);
            go.AddComponent<ExpandingRing>().Init(startRadius, endRadius, duration, width, palette.Material(material), palette.Arc);
        }

        private sealed class ExpandingRing : MonoBehaviour
        {
            private LineRenderer line;
            private float r0, r1, duration, width, age;
            private const int Points = 48;
            private Color color;

            public void Init(float startRadius, float endRadius, float seconds, float lineWidth, Material material, Color tint)
            {
                color = tint;
                r0 = startRadius; r1 = endRadius; duration = Mathf.Max(0.01f, seconds); width = lineWidth;
                line = gameObject.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = Points;
                line.alignment = LineAlignment.View;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.textureMode = LineTextureMode.Stretch;
                Apply(0f);
            }

            private void Update()
            {
                age += Time.deltaTime;
                float t = age / duration;
                if (t >= 1f) { Destroy(gameObject); return; }
                Apply(t);
            }

            private void Apply(float t)
            {
                float eased = 1f - (1f - t) * (1f - t);
                float r = Mathf.Lerp(r0, r1, eased);
                for (int i = 0; i < Points; i++)
                {
                    float a = i / (float)Points * Mathf.PI * 2f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
                float w = width * (1f - t);
                line.startWidth = w;
                line.endWidth = w;
                var c = new Color(color.r, color.g, color.b, 1f - t * t);
                line.startColor = c;
                line.endColor = c;
            }
        }
    }
}
