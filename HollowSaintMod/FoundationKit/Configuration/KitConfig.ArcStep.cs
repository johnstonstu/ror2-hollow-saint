using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindArcStep(ConfigFile c)
        {
            F(c, step, "Look lift", KitTuning.ArcStepLookLift, v => KitTuning.ArcStepLookLift = v, 0f, 1f, 0.05f, "How much the step follows where you look, up or down (0 = flat; 0.35 lifts about 2 m looking 45 degrees up).");
            F(c, step, "Speed", KitTuning.ArcStepSpeed, v => KitTuning.ArcStepSpeed = v, 6f, 40f, 0.5f, "Dash speed in m/s at the start of the step.");
            F(c, step, "Duration", KitTuning.ArcStepDuration, v => KitTuning.ArcStepDuration = v, 0.15f, 1.2f, 0.05f, "Seconds per step.");
            F(c, step, "Recharge", KitTuning.ArcStepRecharge, v => KitTuning.ArcStepRecharge = v, 1f, 15f, 0.5f, "Seconds per charge (restart).", restart: true);
            I(c, step, "Charges", KitTuning.ArcStepMaxStock, v => KitTuning.ArcStepMaxStock = v, 1, 5, "Stock (restart).", restart: true);
            B(c, step, "Invulnerable while stepping", KitTuning.ArcStepGrantsIFrames, v => KitTuning.ArcStepGrantsIFrames = v, "Undecided design question; off by default.");
        }

    }
}
