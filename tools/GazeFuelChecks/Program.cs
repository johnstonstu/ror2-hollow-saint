using System;
using HollowSaint.FoundationKit.Gaze;

// Executes the actual production ledger/schedule/retirement logic without Unity.
// Success: accepted charges conserved, reserve never spent by this cast, <=5 phases,
// normalized full-cast coefficient, correct intake/arrival boundaries, cancellation
// prevents later launches, death discards, and stale packets never resurrect a cast.
static class Program
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        checks++;
        if (!condition) throw new Exception("FAIL " + name);
    }
    static void Main()
    {
        foreach (int capacity in new[] { 2, 5, 6, 20 })
        {
            for (int entry = 0; entry <= capacity; entry++)
            {
                var ledger = new GazeFuelLedger(); ledger.Begin(entry, capacity);
                var schedule = new GazeFuelSchedule(); schedule.Begin(entry);
                Check(schedule.Count <= 5, "bounded phases");
                int grouped = 0; float coefficient = 0f;
                for (int phase = 0; phase < schedule.Count; phase++)
                {
                    int group = schedule.Group(phase); grouped += group;
                    coefficient += GazeFuelSchedule.Coefficient(group, ledger.Capacity);
                    float intake = schedule.IntakeAt(phase);
                    Check(!schedule.TakeIntake(intake - 0.001f, out _), "no intake before scheduled boundary");
                    Check(schedule.TakeIntake(intake, out int shown) && shown == phase, "intake at boundary");
                    Check(!schedule.TakeLaunch(intake + 0.319f, out _), "intake cannot spend early");
                    Check(ledger.Unspent + ledger.Reserve <= ledger.Capacity, "combined bank bounded");
                    Check(schedule.TakeLaunch(intake + GazeFuelSchedule.IntakeDuration, out int launched) && launched == phase,
                        "launch after full intake");
                    Check(ledger.TrySpend(group), "entry group launch spends once");
                    Check(ledger.Entry + ledger.AcceptedGains == ledger.Spent + ledger.Unspent + ledger.Reserve, "conservation during launch");
                    while (ledger.TryGain()) { }
                    Check(ledger.RejectedGains > 0, "overflow explicitly counted");
                }
                Check(grouped == entry, "all entry orbs grouped exactly once");
                Check(Math.Abs(coefficient - 2.5f * entry / capacity) < 0.0001f, "capacity normalized coefficient");
                Check(!ledger.TrySpend(1), "reserve cannot fuel current cast");
                int spent = ledger.Spent, accepted = ledger.AcceptedGains;
                int retained = ledger.End(true);
                Check(entry + accepted == spent + retained, "living conservation");
                Check(ledger.HoldAfterMerge == (retained >= capacity), "only full merge holds automatic passive");
                Check(!ledger.TryGain() && !ledger.TrySpend(1), "closed ledger rejects mutation");
            }
            var cancelled = new GazeFuelLedger(); cancelled.Begin(capacity, capacity);
            var plan = new GazeFuelSchedule(); plan.Begin(capacity);
            Check(plan.TakeIntake(0.15f, out _), "first intake begun");
            Check(cancelled.Spent == 0 && cancelled.End(true) == capacity && cancelled.HoldAfterMerge,
                "intaking fuel fully refunded and full merge held");
            plan.Cancel();
            Check(!plan.TakeLaunch(99f, out _) && !plan.TakeIntake(99f, out _), "cancelled schedule cannot launch");
            var dead = new GazeFuelLedger(); dead.Begin(capacity, capacity);
            Check(dead.TrySpend(1) && dead.TryGain(), "death fixture spent and reserved");
            Check(dead.End(false) == 0 && !dead.HoldAfterMerge, "death discards unused fuel and reserve");
            int phaseCount = Math.Min(capacity, GazeFuelSchedule.MaxPhases);
            for (int cancelledPhase = 0; cancelledPhase < phaseCount; cancelledPhase++)
            {
                // Cut each phase before intake, during intake, or just after launch.
                foreach (float cutOffset in new[] { -0.01f, 0.16f, 0.33f })
                {
                    var cutLedger = new GazeFuelLedger(); cutLedger.Begin(capacity, capacity);
                    var cutPlan = new GazeFuelSchedule(); cutPlan.Begin(capacity);
                    float cutAt = cutPlan.IntakeAt(cancelledPhase) + cutOffset;
                    for (float clock = 0f; clock <= cutAt; clock += 0.01f)
                    {
                        while (cutPlan.TakeIntake(clock, out _)) { }
                        while (cutPlan.TakeLaunch(clock, out int phase))
                        {
                            Check(cutLedger.TrySpend(cutPlan.Group(phase)), "cancel fixture launch once");
                            cutLedger.TryGain();
                        }
                    }
                    int committed = cutLedger.Spent;
                    int gained = cutLedger.AcceptedGains;
                    cutPlan.Cancel();
                    int refunded = cutLedger.End(true);
                    Check(!cutPlan.TakeLaunch(100f, out _) && !cutPlan.TakeIntake(100f, out _), "every-phase cancellation stops future launch");
                    Check(capacity + gained == committed + refunded, "every-phase cancellation conserves accepted resource");
                    Check(cutLedger.Spent == committed, "launched charges never refunded");
                }
            }
            Console.WriteLine("PASS capacity=" + capacity + " all entry amounts, conservation, bounded grouping, cancellation, death");
        }
        var changing = new GazeFuelLedger(); changing.Begin(20, 20);
        Check(changing.End(true) == 20 && changing.HoldAfterMerge, "full retained bank twenty");
        changing.Begin(20, 2);
        Check(changing.Capacity == 20 && changing.Entry == 20, "config lowering preserves retained bank");
        Check(changing.TrySpend(18) && changing.End(true) == 2, "bank can decrease under old snapshot");
        changing.Begin(2, 2);
        Check(changing.Capacity == 2, "lower cap applies after bank decreases");
        Check(GazeFuelLedger.ClampCapacity(-1) == 2 && GazeFuelLedger.ClampCapacity(100) == 20, "configured capacity bounds");
        Check(GazeFuelSchedule.Travel(1f) == 0.12f && GazeFuelSchedule.Travel(100f) == 0.22f, "travel clamp");
        Check(Math.Abs(GazeFuelSchedule.SpreadRadius(0f, 4f, 8f, 0.4f, 1.6f) - 3.2f) < 0.0001f,
            "ground radius starts at 3.2 metres, dimensionless reach");
        Check(Math.Abs(GazeFuelSchedule.SpreadRadius(4f, 4f, 8f, 0.4f, 1.6f) - 12.8f) < 0.0001f,
            "ground radius ends at 12.8 metres, not 64 metres");
        float[] launchRadii = { 4.328f, 6.008f, 7.688f, 9.368f, 11.048f };
        for (int phase = 0; phase < launchRadii.Length; phase++)
            Check(Math.Abs(GazeFuelSchedule.SpreadRadius(0.15f + phase * 0.70f + 0.32f,
                4f, 8f, 0.4f, 1.6f) - launchRadii[phase]) < 0.0001f, "scheduled radius in metres");
        Check(Math.Abs(GazeFuelSchedule.StrikeAt(1f, 0.2f, 4f, 8f) - 1.35f) < 0.0001f, "arrival plus radial spread delay");
        Check(Math.Abs(GazeFuelSchedule.StrikeAt(1f, 0.2f, 8f, 8f) - 1.5f) < 0.0001f, "groundwave ends at 300ms");
        var gate = new GazeFuelSequence();
        Check(!gate.Accept(1, 2, false, false), "orphan launch rejected");
        Check(gate.Accept(1, 1, true, false), "begin accepted");
        Check(gate.Accept(1, 2, false, false), "ordered pulse accepted");
        Check(!gate.Accept(1, 2, false, false), "duplicate rejected");
        Check(gate.Accept(1, 4, false, true), "end accepted");
        Check(!gate.Accept(1, 5, false, false) && !gate.Accept(1, 1, true, false), "late pulse and begin cannot resurrect retired cast");
        Check(gate.Accept(2, 8, false, true) && !gate.Accept(2, 1, true, false), "end before missing begin still retires cast");
        Check(gate.Accept(3, 1, true, false), "new cast accepted"); gate.Retire();
        Check(!gate.Accept(3, 2, false, false), "disable retirement rejects late hits");
        Check(gate.Accept(4, 1, true, false) && !gate.Accept(3, 9, false, true), "old end cannot clear newer cast");
        Console.WriteLine("PASS config reduction, arrival timing, stale transport retirement");
        Console.WriteLine("PASS " + checks + " production assertions");
    }
}
