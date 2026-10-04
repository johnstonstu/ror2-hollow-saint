using System;
using HollowSaint.FoundationKit.Stormspear;

// Success: no recharge during charge/pre-release (even with spare stocks), start at release,
// and only a living authority can refund one uncommitted consumed stock without exceeding capacity.
static class Program
{
    private static int checks;
    private static void Check(bool value, string name)
    { checks++; if (!value) throw new Exception("FAIL " + name); }
    static void Main()
    {
        Check(StormspearCooldownPolicy.Pause(true, false, false), "charge pauses");
        Check(StormspearCooldownPolicy.Pause(false, true, false), "hand release delay pauses");
        Check(!StormspearCooldownPolicy.Pause(false, true, true), "actual crown/hand throw resumes during recovery");
        Check(!StormspearCooldownPolicy.Pause(false, false, false), "cancellation resumes existing queue");
        foreach (int capacity in new[] { 1, 2, 5, 20 })
        {
            for (int stock = 0; stock <= capacity; stock++)
            {
                Check(StormspearCooldownPolicy.Pause(true, false, false), "spare stock cannot recharge during charge");
                Check(StormspearCooldownPolicy.CanRefund(false, true, true, stock, capacity) == (stock < capacity), "bounded refund");
                Check(!StormspearCooldownPolicy.CanRefund(true, true, true, stock, capacity), "committed throw never refunds");
                Check(!StormspearCooldownPolicy.CanRefund(false, false, true, stock, capacity), "remote presentation never refunds");
                Check(!StormspearCooldownPolicy.CanRefund(false, true, false, stock, capacity), "death never refunds");
            }
        }
        // Vanilla queue input is preserved rather than reset; elapsed charge cannot be banked.
        float progress = 3.25f;
        for (int tick = 0; tick < 10000; tick++)
            if (!StormspearCooldownPolicy.Pause(true, false, false)) progress += 0.02f;
        Check(progress == 3.25f, "indefinite charge does not bank recharge");
        if (!StormspearCooldownPolicy.Pause(false, true, true)) progress += 0.02f;
        Check(Math.Abs(progress - 3.27f) < 0.0001f, "existing spare stock progress resumes on throw");
        Console.WriteLine($"PASS {checks} production cooldown checks");
    }
}
