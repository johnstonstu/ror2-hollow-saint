using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindLogging(ConfigFile c)
        {
            B(c, misc, "Verbose log", HsLog.Verbose, v => HsLog.Verbose = v, "Write Hollow Saint's load diagnostics to the BepInEx log and show the build tag. For bug reports; off keeps the log clean.");
            EventLog = c.Bind(misc, "Event log", false, "Log the first few occurrences of each gameplay event (HOLLOW_SAINT_EVENT) for playtesting.");
            Bools.Add(EventLog);
            // v0.9.11 (release hygiene): the event log defaults off. Bound after the migrations above, so
            // it is migrated here; an untouched true from an older config moves to false.
            var logVersion = c.Bind(misc, "Event log defaults version", 0, "Internal. Do not edit.");
            if (logVersion.Value < 1) { if (EventLog.Value) EventLog.Value = false; logVersion.Value = 1; }
        }

    }
}
