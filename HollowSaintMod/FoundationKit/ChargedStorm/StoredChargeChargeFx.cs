using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    public sealed class StoredChargeChargeFx : MonoBehaviour
    {
        private CharacterBody body;
        private byte kind;
        private int gathered;
        private GameObject ball;
        private LineRenderer preview;
        private Gaze.Fx.GazeChargeUpFx crown;
        private HollowedOrb.HollowedOrbPose hands;
        private SkinFxPalette palette;
        private bool ended;
        private float nextArc;
        private float startedAt;
        private readonly Transform[] feeds = new Transform[2];
        internal static StoredChargeChargeFx Begin(CharacterBody body, byte kind, int available)
        {
            if (!body) return null;
            var root = new GameObject("HS_StoredChargeGather"); root.transform.SetParent(body.gameObject.transform, false);
            var fx = root.AddComponent<StoredChargeChargeFx>(); fx.body = body; fx.kind = kind;
            fx.startedAt = Time.time;
            try
            {
                fx.palette = SkinFxPalette.ForBody(body);
                if (kind != 1)
                {
                    fx.crown = Gaze.Fx.GazeChargeUpFx.Begin(body, available);
                    if (kind == 0) fx.preview = StormVisualPrimitives.Ring(root.transform, fx.palette);
                    KitAnim.PlayGestureOnBody(body, kind == 2 ? OpenCircuit.OpenCircuitTuning.HoldArmsState : OpenCircuit.OpenCircuitTuning.CastArmsState, 1.2f);
                }
                else
                {
                    fx.ball = StormVisualPrimitives.Orb(fx.palette, ChargedStormTuning.Diameter(0)); fx.ball.transform.SetParent(root.transform, true);
                    fx.hands = HollowedOrb.HollowedOrbPose.For(body); if (fx.hands) fx.hands.Gather(0);
                    var model = body.modelLocator ? body.modelLocator.modelTransform : null;
                    if (model) foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                    {
                        if (bone.name == "L forearm") fx.feeds[0] = bone;
                        if (bone.name == "R forearm") fx.feeds[1] = bone;
                    }
                }
                return fx;
            }
            catch
            {
                // OnDestroy unwinds any crown/hand owner acquired before an
                // optional material or renderer failed; the state logs context.
                Destroy(root); throw;
            }
        }
        internal void Gather(int count)
        {
            gathered = count;
            if (crown) crown.Absorb(count);
            if (hands && !Stormspear.StormspearCharge.InCrown(body)) hands.Gather(count);
            if (kind == 1)
            {
                Util.PlaySound(CustomSoundBank.Ready ? "Play_HS_GazeLoad" + Mathf.Clamp(count, 1, 5) : "Play_HS_ChargeTick", gameObject);
                LightningLine.Spawn(HaloRing.CenterOf(body), BallPoint, .28f, 1.7f, 1, palette: palette).drawTime = .025f;
            }
        }
        private Vector3 BallPoint => HollowedOrb.OrbCastGeometry.Point(body,
            body.inputBank ? body.inputBank.aimDirection.normalized : body.gameObject.transform.forward, gathered);
        private void Update()
        {
            if (ended) return;
            if (!body || !body.healthComponent || !body.healthComponent.alive) { End(false); return; }
            if (ball)
            {
                ball.transform.position = BallPoint;
                float form = Mathf.SmoothStep(.55f, 1f, Mathf.Clamp01((Time.time - startedAt) / HollowedOrb.OrbCastFlow.MinimumWindup));
                ball.transform.localScale = Vector3.one * ChargedStormTuning.Diameter(gathered) * form;
                if (hands)
                {
                    if (Stormspear.StormspearCharge.InCrown(body)) hands.Release(false);
                    else hands.Gather(gathered);
                }
                if (Time.time >= nextArc)
                {
                    nextArc = Time.time + .12f;
                    var point = BallPoint;
                    var side = body.gameObject.transform.right * ChargedStormTuning.Diameter(gathered) * .48f;
                    // Bright sustained body-to-ball feeds distinguish forming the
                    // free base Orb from the separate stored-charge intake flash.
                    LightningLine.Spawn(body.corePosition, point, .19f, 1.1f, 1, palette: palette).drawTime = .04f;
                    for (int i = 0; i < feeds.Length; i++)
                        if (feeds[i]) LightningLine.Spawn(feeds[i].position, point + (i == 0 ? -side : side), .17f, .8f, 1, palette: palette).drawTime = .03f;
                }
            }
            if (preview)
            {
                var direction = body.inputBank ? body.inputBank.aimDirection.normalized : body.gameObject.transform.forward;
                var aim = ChargedStormTargeting.AimPoint(body, direction, ChargedStormTuning.Bound(ChargedStormTuning.CloudRange, 20f, 120f));
                StormVisualPrimitives.SetRing(preview, aim + Vector3.up * .15f, ChargedStormTuning.Radius(Mathf.Max(1, gathered)));
            }
        }
        internal void Confirm(int count)
        {
            // Observers may predict another intake while release is in flight.
            // Release strength follows the server's committed charge count.
            gathered = count;
            if (hands && !Stormspear.StormspearCharge.InCrown(body)) hands.Gather(count);
        }
        internal void End(bool fired)
        {
            if (ended) return; ended = true;
            if (crown) crown.End(gathered, fired);
            if (hands) hands.Release(fired && !Stormspear.StormspearCharge.InCrown(body));
            if (kind != 1) OpenCircuit.CrownGestureFlow.Recover(body);
            Destroy(gameObject);
        }
        private void OnDestroy()
        {
            if (ball) Destroy(ball);
            if (crown && !ended) crown.End(gathered, false);
            if (hands && !ended) hands.Release(false);
        }
    }
}
