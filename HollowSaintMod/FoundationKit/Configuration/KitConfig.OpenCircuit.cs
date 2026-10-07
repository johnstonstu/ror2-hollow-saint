using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        private static void BindOpenCircuit(ConfigFile c)
        {
            F(c, circuit, "Pulse damage", KitTuning.OpenCircuitPulseDamageCoefficient, v => KitTuning.OpenCircuitPulseDamageCoefficient = v, 0.1f, 3f, 0.05f, "Damage coefficient per pulse. Stored raw; effective native damage is 90% of this value. Inherited splash/chain fractions are applied afterward.");
            F(c, circuit, "Pulse interval", KitTuning.OpenCircuitPulseInterval, v => KitTuning.OpenCircuitPulseInterval = v, 0.2f, 2f, 0.05f, "Seconds between pulses.");
            F(c, circuit, "Radius", KitTuning.OpenCircuitRadius, v => KitTuning.OpenCircuitRadius = v, 3f, 25f, 0.5f, "Metres.");
            F(c, circuit, "Duration", KitTuning.OpenCircuitBuffSeconds, v => KitTuning.OpenCircuitBuffSeconds = v, 2f, 20f, 0.5f, "Seconds the crown stays open.");
            F(c, circuit, "Cooldown", KitTuning.OpenCircuitCooldown, v => KitTuning.OpenCircuitCooldown = v, 3f, 30f, 0.5f, "Seconds (restart). Counted from when the crown closes unless Cooldown after crown is off.", restart: true);
            B(c, circuit, "Cooldown after crown", OpenCircuit.OpenCircuitTuning.CooldownAfterCrown, v => OpenCircuit.OpenCircuitTuning.CooldownAfterCrown = v, "The cooldown starts when the crown closes instead of on the cast, so cooldown items can't keep the crown up forever.");
        }

    }
}
