using System;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Thundercloud;
using RoR2;
using UnityEngine.Networking;

static partial class Program
{
    // Success: either input order dismisses without consuming spare stock; full
    // restocks do not gain a refund; custom intervals get at most 2x strike rate.
    static void CloudAudit()
    {
        foreach (bool nativeFirst in new[] { false, true })
        foreach (int stock in new[] { 1, 2 })
        {
            Reset(); StoredChargeTransport.Install();
            var body = Body(); var slot = body.skillLocator.special;
            slot.skillDef = new StoredChargeSkillDef { followsCloudFreeCast = true };
            slot.stock = stock; slot.maxStock = 2; slot.rechargeStopwatch = 0f;
            var state = State(body, 0, true);
            var writer = new NetworkWriter(); state.OnSerialize(writer);
            uint token = new NetworkReader(writer.Stream.ToArray()).ReadUInt32();
            state.ReceiveReply(new StoredChargeTransport.Packet { cast = token, kind = 0, duration = 8f });
            body.inputBank.skill4.down = false; state.Update();
            body.inputBank.skill4.down = true;
            if (nativeFirst) Check(!slot.ExecuteIfReady(), "native input cannot spend spare stock before dismissal");
            state.Update();
            if (!nativeFirst) Check(!slot.ExecuteIfReady(), "native input cannot spend spare stock after dismissal");
            Check(!StoredChargeState.BlocksPrimary(body), "pending dismissal leaves Primary available");
            state.FixedUpdate();
            Check(body.GetComponent<StoredChargeDriver>().dismissals == 1, "dismiss still reaches server with spare stock");
            state.Age(.25f); state.FixedUpdate(); state.OnExit();
            Check(state.outer.ended && slot.stock == stock && slot.executions == 0, "dismiss exits without consuming spare stock");
            Near(slot.rechargeStopwatch, stock == 1 ? 5f : 0f, "refund only advances a missing stock");
            Check(slot.CanExecute(), "remaining stock can cast again after dismissal exits");
        }
        float saved = ChargedStormTuning.CloudStrikeInterval;
        try
        {
            foreach (float interval in new[] { .25f, .5f, .75f, 1.5f, 3f })
            {
                ChargedStormTuning.CloudStrikeInterval = interval;
                foreach (float speed in new[] { -1f, 0f, 1f, float.NaN })
                    Near(ThundercloudSchedule.IntervalFor(speed), interval, "base cadence below 1x/invalid attack speed");
                Near(ThundercloudSchedule.IntervalFor(1.5f), interval / 1.5f, "custom interval scales below cap");
                foreach (float speed in new[] { 2f, 4f, 100f, float.PositiveInfinity })
                    Near(ThundercloudSchedule.IntervalFor(speed), interval / 2f, "2x cap relative to custom interval");
            }
        }
        finally { ChargedStormTuning.CloudStrikeInterval = saved; }
    }
}
