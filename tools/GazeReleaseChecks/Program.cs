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
        try { Input(); Holds(); Groups(); Focus(); Opening(); FocusLifecycleChecks.Run(Check); Console.WriteLine($"PASS {checks} release policy/resource assertions"); return 0; }
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
    static bool Near(float a, float b) => Math.Abs(a - b) < 1e-3f;
    // 1.3.1 focus ramp: builds with time on target, grace then fade off target, capped.
    static void Focus()
    {
        float f = GazeFocusPolicy.Next(0f, -1f, 0f);
        Check(f == 0f && Near(GazeFocusPolicy.Multiplier(f), 1f), "fresh target starts unfocused");
        float t = 0f;
        for (int i = 0; i < 15; i++) { t += .2f; f = GazeFocusPolicy.Next(f, t - .2f, t); }
        Check(Near(f, 1f), "3 s of steady 0.2 s ticks reaches full focus");
        Check(Near(GazeFocusPolicy.Multiplier(f), 1f + GazeFocusPolicy.MaxBonus), "full focus applies the bonus");
        Check(GazeFocusPolicy.Tier(f) == 5 && GazeFocusPolicy.Tier(.21f) == 1 && GazeFocusPolicy.Tier(.19f) == 0 && GazeFocusPolicy.Tier(.61f) == 3, "five audible focus steps");
        float half = 0f; for (int i = 0; i < 7; i++) half = GazeFocusPolicy.Next(half, i * .2f, (i + 1) * .2f);
        Check(half > .4f && half < .5f, "1.4 s on target is under half focus");
        Check(GazeFocusPolicy.Next(1f, 0f, .45f) >= .99f, "a miss inside the grace keeps full focus");
        float back = GazeFocusPolicy.Next(1f, 0f, 1f);
        Check(back > .45f && back < .7f, "1 s away loses about half, then the hit re-credits a step");
        Check(GazeFocusPolicy.Next(1f, 0f, 3f) < .15f, "long gaps fade focus to (almost) nothing");
        Check(GazeFocusPolicy.Next(.5f, 0f, 30f) <= GazeFocusPolicy.MaxStep / GazeFocusPolicy.RampSeconds + 1e-4f, "a single late hit credits at most one step");
        Check(GazeFocusPolicy.Next(float.NaN, 0f, .2f) == 0f && GazeFocusPolicy.Next(.5f, 1f, .5f) >= .5f, "NaN and clock skew are safe");
    }
    static void Opening()
    {
        Check(Near(GazeReleaseTuning.OpeningRadius(1), 8f) && Near(GazeReleaseTuning.OpeningRadius(5), 16f), "opening radius 6 m + 2 m per charge");
        Check(Near(GazeReleaseTuning.OpeningRadius(20), 20f), "opening radius capped at 20 m");
        Check(!GazeReleaseTuning.MidBeamSurges, "mid-beam surges are off by default");
    }
}
