using System.Collections;
using System.Reflection;
using HollowSaint.FoundationKit.Gaze;
using RoR2;
using UnityEngine;

static class FocusLifecycleChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Invoke(GazeFocusTracker tracker, string name) => typeof(GazeFocusTracker).GetMethod(name, Private)!.Invoke(tracker, null);
    static int Count(GazeFocusTracker tracker) => ((IDictionary)typeof(GazeFocusTracker).GetField("entries", Private)!.GetValue(tracker)!).Count;
    static GazeFocusTracker Create()
    {
        var tracker = new GazeFocusTracker { attachedOwner = new HealthComponent() };
        Invoke(tracker, "Awake"); return tracker;
    }
    internal static void Run(Action<bool, string> check)
    {
        // Success: separate attackers ramp independently, prune without further
        // attacks, and retain no victims after death, disable or stage change.
        var a = Create(); var b = Create(); var victim = new HealthComponent();
        a.Hit(victim, 0f, out _); b.Hit(victim, 0f, out _);
        float bonus = 1f;
        for (int i = 1; i <= 15; i++) bonus = a.Hit(victim, i * .2f, out _);
        check(Math.Abs(bonus - 2f) < .001f, "tracker preserves 3-second focus ramp");
        check(b.Hit(victim, .2f, out _) < 1.1f, "other attacker does not inherit focus");
        Time.time = 8f; Invoke(a, "FixedUpdate"); Invoke(b, "FixedUpdate");
        check(Count(a) == 0 && Count(b) == 0, "both trackers prune idle victims regardless of attacker order");
        a.Hit(victim, 8f, out _); b.Hit(victim, 8f, out _);
        victim.destroyed = true; Time.time = 10f; Invoke(b, "FixedUpdate"); Invoke(a, "FixedUpdate");
        check(Count(a) == 0 && Count(b) == 0, "destroyed victims removed from both trackers");
        victim = new HealthComponent(); a.Hit(victim, 10f, out _);
        a.attachedOwner.alive = false; Invoke(a, "FixedUpdate");
        check(Count(a) == 0 && a.Hit(victim, 10f, out _) == 1f, "owner death clears focus and prevents admission");
        a.attachedOwner.alive = true; a.Hit(victim, 11f, out _); Invoke(a, "OnDisable");
        check(Count(a) == 0, "body disable clears references immediately");
        a.Hit(victim, 12f, out _); b.Hit(victim, 12f, out _);
        Stage.instance = new Stage(); Time.time = 12.1f; Invoke(a, "FixedUpdate"); Invoke(b, "FixedUpdate");
        check(Count(a) == 0 && Count(b) == 0, "stage replacement clears every surviving body tracker");
        check(a.Hit(victim, 12.2f, out _) == 1f, "new stage begins without carried focus");
        float grace = GazeFocusPolicy.GraceSeconds, fade = GazeFocusPolicy.DecaySeconds;
        try
        {
            GazeFocusPolicy.GraceSeconds = 2f; GazeFocusPolicy.DecaySeconds = 4f;
            var slow = Create(); slow.Hit(victim, 20f, out _);
            Time.time = 24.5f; Invoke(slow, "FixedUpdate");
            check(Count(slow) == 1, "custom grace/fade prevents premature pruning");
            Time.time = 27f; Invoke(slow, "FixedUpdate");
            check(Count(slow) == 0, "custom focus eventually expires while idle");
        }
        finally { GazeFocusPolicy.GraceSeconds = grace; GazeFocusPolicy.DecaySeconds = fade; }
    }
}
