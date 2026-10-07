using System.Collections;
using System.Linq;
using HollowSaint.FoundationKit.Storm;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.16 (HS_SEGMENTS=storm): how often the storm actually fires in a sustained fight. 45 s at
    /// level 1 with no items against the tank (a Stone Golem with lots of health) and a pack of
    /// Lemurians that respawns whenever it is wiped: Arc Bolt held, a charged Stormspear every 7 s.
    /// Writes STORM_RATE (Electrocutes and Thunderbolts per minute) to the trace.
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private IEnumerator StormSegments()
        {
            foreach (var d in dummies) if (d && d.healthComponent) { d.healthComponent.godMode = false; d.healthComponent.Suicide(); }
            dummies.Clear();
            SpawnTank();
            yield return Wait(1.5f);
            float mg;
            if (float.TryParse(System.Environment.GetEnvironmentVariable("HS_STORM_MINGAIN"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out mg)) FoundationKit.KitTuning.StaticMinGain = mg;
            if (float.TryParse(System.Environment.GetEnvironmentVariable("HS_STORM_DD"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out mg)) FoundationKit.KitTuning.DeathDischargeStatic = mg;
            yield return Segment("storm-sustained");
            long e0 = StormTelemetry.ChargeElectrocutes, t0 = StormTelemetry.Thunderbolts;
            float start = scriptTime;
            int waves = 1;
            SpawnPack();
            yield return Wait(0.8f);
            fire1 = true;
            // Like a player: bolts on the nearest living pack member (the tank when the pack is down),
            // a charged spear into the pack every 7 s, a new wave 2 s after the pack is wiped.
            float cycle = 0f, wipedAt = -1f;
            while (scriptTime - start < 45f)
            {
                var alive = pack.Where(b => b && b.healthComponent && b.healthComponent.alive).ToList();
                aimTarget = alive.Count > 0 ? alive.OrderBy(b => (b.corePosition - pilot.corePosition).sqrMagnitude).First().corePosition : TankAim();
                if (alive.Count == 0)
                {
                    if (wipedAt < 0f) wipedAt = scriptTime;
                    else if (scriptTime - wipedAt > 2f) { SpawnPack(); waves++; wipedAt = -1f; }
                }
                cycle += 0.1f;
                if (cycle >= 7f) cycle = 0f;
                fire2 = cycle < 2.2f;
                yield return Wait(0.1f);
            }
            fire2 = false;
            fire1 = false;
            yield return Wait(2f);
            float minutes = (scriptTime - start) / 60f;
            long e = StormTelemetry.ChargeElectrocutes - e0, t = StormTelemetry.Thunderbolts - t0;
            trace.AppendLine("STORM_RATE seconds=" + (minutes * 60f).ToString("0") + " waves=" + waves + " electrocutes=" + e + " thunderbolts=" + t
                + " perMinute=" + (e / minutes).ToString("0.0") + "/" + (t / minutes).ToString("0.0") + " chargesPerBolt=" + FoundationKit.KitTuning.StormChargeMax
                + " minGain=" + FoundationKit.KitTuning.StaticMinGain.ToString("0.00") + " deathDischarge=" + FoundationKit.KitTuning.DeathDischargeStatic.ToString("0.00"));
        }
    }
}
