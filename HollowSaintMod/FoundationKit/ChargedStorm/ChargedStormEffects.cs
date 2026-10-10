using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using HollowSaint.FoundationKit.Vfx;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    internal static class ChargedStormEffects
    {
        private static GameObject cloud, strike, orb, impact;
        internal static void Register()
        {
            cloud = Prefab<Thundercloud.ThundercloudFx>("HollowSaintThundercloudEffect");
            strike = Prefab<ChargedStormStrikeFx>("HollowSaintCloudStrikeEffect");
            orb = Prefab<HollowedOrb.HollowedOrbFlightFx>("HollowSaintHollowedOrbFlightEffect");
            impact = Prefab<HollowedOrb.HollowedOrbImpactFx>("HollowSaintHollowedOrbImpactEffect");
        }
        private static GameObject Prefab<T>(string name) where T : Component
        {
            var root = VfxAssets.NewPrefab(name);
            var effect = root.AddComponent<EffectComponent>();
            // scale is a radius/diameter payload already consumed by the visual.
            // Native root scaling would multiply it a second time.
            effect.applyScale = false;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;
            var attributes = root.AddComponent<VFXAttributes>(); attributes.DoNotPool = true;
            attributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            root.AddComponent<T>(); root.AddComponent<DestroyOnTimer>().duration = 15f;
            KitContent.AddEffect(root); return root;
        }
        private static void Send(GameObject prefab, EffectData data)
        {
            if (!NetworkServer.active || !prefab) return;
            try { EffectManager.SpawnEffect(prefab, data, true); }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CHARGED_STORM_EFFECT " + prefab.name + " " + error); }
        }
        internal static void Cloud(CharacterBody owner, Vector3 sky, float radius, float duration)
        {
            var data = new EffectData { origin = sky, start = HaloRing.CenterOf(owner), scale = radius,
                genericFloat = duration, color = SkinFxPalette.ForBody(owner).NetworkColor };
            data.SetNetworkedObjectReference(owner.gameObject); Send(cloud, data);
        }
        internal static void CloudDismiss(CharacterBody owner, Vector3 sky)
        {
            var data = new EffectData { origin = sky, start = sky, scale = 1f, genericFloat = -1f, color = SkinFxPalette.ForBody(owner).NetworkColor };
            data.SetNetworkedObjectReference(owner.gameObject); Send(cloud, data);
        }
        internal static void Strike(CharacterBody owner, Vector3 sky, Vector3 point, float radius, HealthComponent victim, Vector3 center, int strokes = Thundercloud.ThundercloudSchedule.ReturnStrokeCount)
        {
            // Existing EffectData fields carry presentation bounds; root scaling
            // is disabled. The cloud center shares sky's X/Z coordinates.
            var data = new EffectData { origin = point, start = sky, scale = radius, genericFloat = sky.y - center.y,
                genericUInt = (uint)Mathf.Clamp(strokes, 1, Thundercloud.ThundercloudSchedule.ReturnStrokeCount), color = SkinFxPalette.ForBody(owner).NetworkColor };
            if (victim && victim.body) data.SetNetworkedObjectReference(victim.body.gameObject);
            Send(strike, data);
        }
        internal static void Orb(CharacterBody owner, Vector3 from, Vector3 to, float diameter, float speed, HealthComponent target, uint flight, bool launched = false)
        {
            var data = new EffectData { origin = from, start = to, scale = diameter, genericFloat = speed,
                genericUInt = flight, genericBool = launched, color = SkinFxPalette.ForBody(owner).NetworkColor };
            if (target && target.body) data.SetNetworkedObjectReference(target.body.gameObject);
            Send(orb, data);
        }
        internal static void OrbBurst(CharacterBody owner, Vector3 point, float radius)
        {
            if (!NetworkServer.active || !owner) return;
            Vector3 ground = point, normal = Vector3.up;
            if (Physics.Raycast(point + Vector3.up * .5f, Vector3.down, out var floor, 6f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            { ground = floor.point; normal = floor.normal; }
            try { KitFx.Server(Beat.SpearBurst, ground, normal, radius, owner: owner); }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_ORB_BURST_FX " + error); }
        }
        internal static void OrbImpact(CharacterBody owner, Vector3 point, float diameter, uint flight, bool sound = true)
        {
            Send(impact, new EffectData { origin = point, scale = diameter, genericUInt = flight,
                genericBool = sound, color = SkinFxPalette.ForBody(owner).NetworkColor });
        }
    }
    public sealed class ChargedStormStrikeFx : MonoBehaviour
    {
        internal static System.Action<int> DiagnosticStroke;
        private EffectData data;
        private SkinFxPalette palette;
        private HealthComponent victim;
        private bool following;
        private float age;
        private int nextStroke, strokeCount = Thundercloud.ThundercloudSchedule.ReturnStrokeCount;
        private Light flash;
        private float flashAge = 1f;
        // 1.3.1 (Stu): a thunder crack per strike (rate-limited) and a deep boom once per strike wave.
        private static float lastCrack = -10f, lastBoom = -10f;
        private Vector3 root;
        private void Start()
        {
            try
            {
                data = GetComponent<EffectComponent>().effectData;
                if (data == null) { Destroy(gameObject); return; }
                palette = SkinFxPalette.FromNetwork(data.color);
                if (data.genericUInt > 0) strokeCount = (int)Mathf.Clamp(data.genericUInt, 1, Thundercloud.ThundercloudSchedule.ReturnStrokeCount);
                var obj = data.ResolveNetworkedObjectReference();
                var body = obj ? obj.GetComponent<CharacterBody>() : null;
                victim = body ? body.healthComponent : null; following = victim;
                // Bolts leave the cloud's underside above their target, not its centre point.
                Vector3 target = victim && victim.body ? victim.body.corePosition : data.origin;
                Vector2 jitter = Random.insideUnitCircle * data.scale * .12f;
                root = new Vector3(Mathf.Lerp(data.start.x, target.x, .75f) + jitter.x, data.start.y - Mathf.Clamp(data.scale * .08f, .5f, 2.5f),
                    Mathf.Lerp(data.start.z, target.z, .75f) + jitter.y);
                Stroke();
                float now = Time.unscaledTime;
                if (now - lastCrack > .12f) { lastCrack = now; Util.PlaySound("Play_item_use_lighningArm", gameObject); }
                if (now - lastBoom > .5f) { lastBoom = now; Util.PlaySound(Storm.ThunderLayerSound.Boom, gameObject); }
                try
                {
                    // Each committed strike lights the cloud from inside; presentation only.
                    VfxParticles.Burst(root + Vector3.up * .8f, Quaternion.identity, palette.Material(VfxAssets.Flash), 1, .2f,
                        Vector2.zero, Vector2.one * Mathf.Clamp(data.scale * .55f, 4f, 14f), palette.Arc * .55f);
                    var glow = new GameObject("HS_CloudStrikeFlash"); glow.transform.SetParent(transform, false);
                    glow.transform.position = root;
                    flash = glow.AddComponent<Light>(); flash.color = palette.Arc; flash.shadows = LightShadows.None;
                    flash.range = Mathf.Clamp(data.scale * 1.6f, 14f, 60f); flashAge = 0f;
                }
                catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CLOUD_STRIKE_FLASH " + error); }
            }
            catch (System.Exception error) { Fail(error); }
        }
        private void Update()
        {
            if (data == null) return;
            age += Time.deltaTime;
            if (flash) { flashAge += Time.deltaTime; flash.intensity = Mathf.Max(0f, 6f * (1f - flashAge / .26f)); flash.enabled = flashAge < .26f; }
            if (following && (!victim || !victim.alive)) { Destroy(gameObject); return; }
            try
            {
                while (nextStroke < strokeCount &&
                    age >= Thundercloud.ThundercloudSchedule.StrokeAt(nextStroke)) Stroke();
                float end = Thundercloud.ThundercloudSchedule.StrokeAt(strokeCount - 1) +
                    Thundercloud.ThundercloudSchedule.BoltLifetime * 1.4f;
                if (age >= end) Destroy(gameObject);
            }
            catch (System.Exception error) { Fail(error); }
        }
        private void Stroke()
        {
            Vector3 point = victim && victim.body ? victim.body.corePosition : data.origin;
            Vector3 center = data.start - Vector3.up * data.genericFloat;
            if (nextStroke > 0 && !ChargedStormTargeting.CloudVisible(data.start, center, point, data.scale))
            { nextStroke = strokeCount; Destroy(gameObject); return; }
            // Return strokes are local presentation only: one networked impact
            // corresponds to one server damage hit, with no repeated procs.
            var bolt = LightningLine.Spawn(root == Vector3.zero ? data.start : root, point, Thundercloud.ThundercloudSchedule.BoltLifetime,
                nextStroke == 0 ? 4.2f : 3.5f, 3, .11f, .035f, palette);
            bolt.drawTime = .025f;
            if (victim && victim.body) bolt.endAnchor = victim.body.coreTransform;
            try { Storm.RoyalCapacitorFx.Splash(point, palette, nextStroke == 0 ? .3f : .16f); }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CLOUD_IMPACT_FX " + error); }
            DiagnosticStroke?.Invoke(nextStroke);
            nextStroke++;
        }
        private void Fail(System.Exception error)
        {
            Plugin.Log.LogWarning("HOLLOW_SAINT_CLOUD_STRIKE_VISUAL " + error);
            Destroy(gameObject);
        }
    }
}
