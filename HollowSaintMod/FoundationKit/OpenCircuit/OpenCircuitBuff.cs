using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>The 8 s "crown open" window. The buff is the single source of truth:
    /// it replicates, so every machine can animate from HasBuff, and the server pulses
    /// while it is present.</summary>
    public static class OpenCircuitBuff
    {
        public const string BuffName = "HollowSaintOpenCircuit";

        public static BuffDef Def { get; private set; }

        internal static void Register()
        {
            if (Def != null) return;
            Def = KitContent.MakeBuff(BuffName, new Color(0.3f, 0.92f, 1f),
                canStack: false, isDebuff: false, hidden: false, icon: "buff_open_circuit");
        }
    }
}
