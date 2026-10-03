using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Brief hand-to-release connection, then the projectile travels independently.</summary>
    [DefaultExecutionOrder(184)]
    public sealed class DischargeLink : MonoBehaviour
    {
        private const float Seconds = 0.1f;
        private readonly LightningLine[] lines = new LightningLine[2];
        private readonly Transform[] hands = new Transform[2];
        private readonly Vector3[] origins = new Vector3[2], directions = new Vector3[2];
        private readonly float[] started = { -999f, -999f }, speeds = new float[2];
        private CharacterBody body;
        public static void Play(CharacterBody owner, bool left, Vector3 direction, float speed)
        {
            if (!owner) return;
            var fx = owner.GetComponent<DischargeLink>() ?? owner.gameObject.AddComponent<DischargeLink>();
            fx.body = owner;
            int i = left ? 0 : 1;
            fx.hands[i] = KitUtil.ResolveSocket(owner, left ? "MuzzleLeft" : "MuzzleRight");
            if (!fx.hands[i]) return;
            fx.origins[i] = fx.hands[i].position;
            fx.directions[i] = direction.normalized;
            fx.speeds[i] = Mathf.Max(1f, speed);
            fx.started[i] = Time.time;
        }
        private void LateUpdate()
        {
            bool alive = body && body.healthComponent && body.healthComponent.alive;
            var model = body && body.modelLocator && body.modelLocator.modelTransform ? body.modelLocator.modelTransform.GetComponent<CharacterModel>() : null;
            bool visible = alive && (!model || model.invisibilityCount <= 0);
            for (int i = 0; i < lines.Length; i++)
            {
                float age = Time.time - started[i];
                bool show = visible && hands[i] && age >= 0f && age < Seconds;
                if (!show) { if (lines[i]) lines[i].gameObject.SetActive(false); continue; }
                if (!lines[i])
                {
                    var line = new GameObject("HS_ReleaseConnection" + i).AddComponent<LightningLine>();
                    line.transform.SetParent(transform, false);
                    line.loop = line.manualTick = true; line.drawTime = 0f;
                    line.branches = 2; line.jag = 0.055f;
                    lines[i] = line;
                }
                var ray = lines[i];
                ray.gameObject.SetActive(true);
                Vector3 endpoint = origins[i] + directions[i] * Mathf.Min(12f, 0.15f + age * speeds[i]);
                Vector3 delta = endpoint - hands[i].position;
                RaycastHit hit;
                if (Physics.Raycast(hands[i].position, delta.normalized, out hit, delta.magnitude, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)) endpoint = hit.point;
                ray.start = hands[i].position; ray.end = endpoint;
                ray.width = 0.9f * (1f - age / Seconds);
                ray.SetPalette(SkinFxPalette.ForBody(body)); ray.Tick(Time.deltaTime);
            }
        }
        private void OnDisable()
        {
            for (int i = 0; i < lines.Length; i++) { started[i] = -999f; if (lines[i]) lines[i].gameObject.SetActive(false); }
        }
    }
}
