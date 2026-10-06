using HollowSaint.FoundationKit.Gaze;

// Success: hold cannot spend or auto-fire; early release spends 1/2/3 exactly once;
// entry-held/cooldown presses cannot queue later; cancel/expiry conserve unspent fuel;
// grouped scheduling cannot overspend and healing preserves the per-charge budget.
static class Program
{
    static int checks;
    static void Check(bool pass, string name) { checks++; if (!pass) throw new Exception(name); }
    static int Main()
    {
        try { Input(); Holds(); Groups(); Console.WriteLine($"PASS {checks} release policy/resource assertions"); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    static void Input()
    {
        var input = new GazeReleaseInput(); input.Begin(true);
        Check(input.Observe(true, true, true) == GazeReleaseEdge.None, "held entry ignored");
        Check(input.Observe(false, true, true) == GazeReleaseEdge.None, "entry release cannot fire");
        Check(input.Observe(true, false, true) == GazeReleaseEdge.None, "unavailable press ignored");
        Check(input.Observe(true, true, true) == GazeReleaseEdge.None, "held press never deferred");
        input.Observe(false, true, true);
        Check(input.Observe(true, true, true) == GazeReleaseEdge.Begin, "fresh press begins");
        Check(input.Observe(true, true, true) == GazeReleaseEdge.None, "hold emits no repeated edges");
        Check(input.Observe(false, false, true) == GazeReleaseEdge.Release, "release observed even if admission closed");
        Check(input.Observe(false, true, true) == GazeReleaseEdge.None, "release emits once");
        input.Observe(true, true, true);
        Check(input.Observe(false, true, false) == GazeReleaseEdge.Cancel, "exit beats simultaneous release");
    }
    static readonly float Step = GazeReleaseTuning.SecondsPerExtraCharge;
    static void Holds()
    {
        foreach (int bank in new[] { 0, 1, 2, 3, 5, 20 })
        foreach (var sample in new[] { (0f, 1), (Step - .01f, 1), (Step, 2), (2 * Step - .01f, 2), (2 * Step, 3), (3f, 3) })
        {
            var hold = new GazeReleaseHold(); var ledger = new GazeFuelLedger(); ledger.Begin(bank, Math.Max(5, bank));
            Check(hold.Begin(1f, bank) == (bank > 0), "empty holds rejected");
            int expected = Math.Min(bank, sample.Item2);
            Check(hold.Loaded(1f + sample.Item1, bank) == expected, "server time sets tier");
            Check(ledger.Spent == 0 && ledger.Unspent == bank, "holding never spends");
            Check(hold.Release(1f + sample.Item1, bank, true) == expected, "early/full release count");
            Check(hold.Release(1f + sample.Item1, bank, true) == 0, "duplicate release empty");
            if (bank > 0) Check(!hold.Begin(1.01f + sample.Item1, bank), "recovery blocks next hold");
            hold.Reset(); hold.Begin(1f, bank); hold.Cancel();
            Check(hold.Release(3f, bank, true) == 0, "cancel cannot fire");
            hold.Reset(); hold.Begin(1f, bank);
            Check(hold.Release(3f, bank, false) == 0 && ledger.End(true) == bank, "expiry refunds all");
        }
        var invalid = new GazeReleaseHold();
        Check(!invalid.Begin(float.NaN, 5) && !invalid.Begin(float.PositiveInfinity, 5), "invalid times rejected");
        Check(GazeReleaseTuning.ArrivalFits(2f, 8f) && !GazeReleaseTuning.ArrivalFits(7.7f, 8f), "late arrival rejected");
        Check(!GazeReleaseTuning.ArrivalFits(float.NaN, 8f), "invalid admission rejected");
    }
    static void Groups()
    {
        foreach (int entry in new[] { 1, 2, 3, 5, 20 })
        {
            var bank = new GazeFuelLedger(); bank.Begin(entry, Math.Max(5, entry));
            var queue = new GazeFuelSchedule(); queue.Begin(entry);
            var recovery = new GazeRecoveryBudget(); recovery.Begin(100f, bank.Capacity, entry);
            float healing = 0f; int nextOrb = 0;
            for (int left = entry; left > 0;)
            {
                int count = Math.Min(3, left); float now = nextOrb + 1f;
                Check(queue.QueueIntake(now, count, .06f, out int phase), "group admitted");
                Check(queue.OrbStart(phase) == nextOrb && queue.GroupSize(phase) == count, "distinct entry orb groups");
                Check(queue.PendingCount == count && bank.Spent == nextOrb, "reservation does not spend");
                Check(!queue.TakeLaunch(now, out _), "not before intake");
                Check(queue.TakeLaunch(now + .061f, out int launched) && launched == phase, "launch once after intake");
                Check(bank.TrySpend(queue.GroupSize(launched)), "atomic grouped spend");
                for (int spent = bank.Spent - count + 1; spent <= bank.Spent; spent++) healing += recovery.Claim(spent);
                Check(!queue.TakeLaunch(now + .1f, out _), "no duplicate launch");
                left -= count; nextOrb += count;
            }
            Check(bank.Spent == entry && queue.PendingCount == 0, "conserve full bank");
            Check(!queue.QueueIntake(99f, 1, .06f, out _), "no overbooking");
            Check(Math.Abs(healing - 5f * entry / bank.Capacity) < .0001f, "group healing remains per charge");
        }
        var pending = new GazeFuelSchedule(); pending.Begin(5); pending.QueueIntake(1f, 3, .06f, out _); pending.Cancel();
        Check(!pending.TakeLaunch(2f, out _), "cancelled intake cannot launch");
    }
}
