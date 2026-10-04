using System;
using HollowSaint.FoundationKit.Stormspear;

// Success: actual production schedules hit exactly four boundaries, frozen charge/damage
// stay independent of changing owner stats, and unordered duplicate hurtboxes cannot exceed two targets.
static class Program
{
    private static int checks;
    private static void Check(bool value, string name)
    { checks++; if (!value) throw new Exception("FAIL " + name); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < 0.0001f;
    static void Main()
    {
        foreach (float charge in new[] { 0f, 0.25f, 0.5f, 1f })
        {
            var shot = new StormspearShot(StormspearShot.ForceForCharge(charge), StormspearShot.CrownCombo);
            float directCoefficient = 3.5f + 10.5f * charge;
            var snapshot = new SpearConductorSchedule(shot.Charge, directCoefficient * 17f, directCoefficient);
            Check(Near(snapshot.Radius, 4f + 2f * charge), "charge radius");
            Check(Near(snapshot.Damage, 17f * (0.2f + 0.15f * charge) * 0.9f), "damage frozen from projectile");
            float ownerDamageAfterImpact = 10000f;
            Check(snapshot.Damage < ownerDamageAfterImpact * 0.2f, "owner stat change cannot alter snapshot");
            int tick = 0;
            for (int i = 1; i <= 4; i++)
            {
                float at = i * 0.75f;
                Check(!SpearConductorSchedule.Due(tick, at - 0.001f), "no early tick");
                Check(SpearConductorSchedule.Due(tick, at), "tick on exact boundary"); tick++;
                Check(!SpearConductorSchedule.Due(tick, at), "no duplicate boundary");
            }
            Check(!SpearConductorSchedule.Due(tick, 999f), "no unbounded sustain");
            Check(4f * snapshot.Damage <= 17f * 1.2601f, "per victim full window <=126% before captured crit");
        }
        Check(Near(new SpearConductorSchedule(-10f, 35f, 3.5f).Radius, 4f), "negative charge clamped");
        Check(Near(new SpearConductorSchedule(10f, 140f, 14f).Radius, 6f), "excess charge clamped");
        Check(Near(new SpearConductorSchedule(1f, 140f, 0f).Damage, 0f), "invalid coefficient no damage");
        // Live tuning change after server projectile initialization must not renormalize in-flight shots.
        float liveTap = 3.5f, liveFull = 14f;
        float frozenCharge = 0.5f;
        float coefficientAtSpawn = liveTap + (liveFull - liveTap) * frozenCharge;
        var inFlight = new SpearConductorSchedule(frozenCharge, 17f * coefficientAtSpawn, coefficientAtSpawn);
        liveTap = 12f; liveFull = 30f;
        float coefficientAtImpact = liveTap + (liveFull - liveTap) * frozenCharge;
        Check(Near(inFlight.Damage, 17f * 0.275f * 0.9f), "live tap/full edits during flight cannot change tick damage");
        Check(!Near(inFlight.Damage, new SpearConductorSchedule(frozenCharge, 17f * coefficientAtSpawn, coefficientAtImpact).Damage),
            "regression detects erroneous impact-time normalization");
        Check(Near(SpearConductorSchedule.TapCoefficient, 0.2f) && Near(SpearConductorSchedule.FullCoefficient, 0.35f) &&
            Near(SpearConductorSchedule.TapRadius, 4f) && Near(SpearConductorSchedule.FullRadius, 6f), "tooltip endpoint constants match rules");
        var selected = new SpearConductorTargets();
        Check(selected.Select(1, 20f) == 0, "first target");
        Check(selected.Select(1, 21f) == -1 && selected.Count == 1, "duplicate hurtbox cannot spend second slot");
        Check(selected.Select(1, 10f) == 0 && selected.Count == 1, "closer duplicate replaces its own geometry");
        Check(selected.Select(2, 30f) == 1, "second distinct target");
        Check(selected.Select(3, 40f) == -1, "farther third rejected");
        Check(selected.Select(3, 2f) == 1, "nearer third replaces farthest");
        Check(selected.Select(4, 3f) == 0 && selected.Count == 2, "unordered candidates bounded");
        selected.Clear();
        Check(selected.Count == 0 && selected.Select(4, 100f) == 0, "new pulse/new conductor resets candidates");
        Check(SpearConductorSchedule.SearchCapacity == 64 && SpearConductorSchedule.TickCount * SpearConductorSchedule.VictimsPerTick == 8,
            "bounded search and damage events");
        foreach (uint id in new[] { 0u, 1u, 65536u, SpearStuckEvent.MaxOwnerId })
        {
            uint word = SpearStuckEvent.Encode(id);
            Check(SpearStuckEvent.IsStuck(word) && SpearStuckEvent.OwnerId(word) == id, "owner packet lossless bounded ID");
        }
        Check(SpearStuckEvent.OwnerId(SpearStuckEvent.Encode(SpearStuckEvent.MaxOwnerId + 1u)) == 0u, "oversized ID fails to short anonymous FX");
        foreach (uint beat in new[] { 1u, 20u, 35u, 40u, 42u })
            Check(!SpearStuckEvent.IsStuck(beat) && SpearStuckEvent.OwnerId(beat) == 0u, "other beat remains untouched");
        Console.WriteLine($"PASS {checks} production conductor checks");
    }
}
