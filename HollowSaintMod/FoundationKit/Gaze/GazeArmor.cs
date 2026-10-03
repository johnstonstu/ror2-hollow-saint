using System;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>
    /// v0.9.15 (Stu): a small armor buff while Gaze of the Hollow channels, so hovering in the open
    /// for five seconds is less of a death sentence. A visible buff, added and removed by the
    /// server's copy of GazeState; RecalculateStats adds GazeTuning.Armor while it is on.
    /// CharacterBody.armor has a private setter, so it is written through a cached delegate.
    /// </summary>
    internal static class GazeArmor
    {
        public static BuffDef Def { get; private set; }
        private static Action<CharacterBody, float> setArmor;

        internal static void Register()
        {
            if (Def != null) return;
            Def = KitContent.MakeBuff("HollowSaintGazeArmor", new Color(1f, 0.82f, 0.45f), canStack: false, isDebuff: false, hidden: false, icon: "buff_open_circuit");
            var setter = typeof(CharacterBody).GetProperty("armor")?.GetSetMethod(true);
            if (setter != null) setArmor = (Action<CharacterBody, float>)Delegate.CreateDelegate(typeof(Action<CharacterBody, float>), setter);
            if (setArmor == null) { Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_ARMOR no armor setter; buff is cosmetic"); return; }
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) =>
            {
                orig(self);
                if (self && Def && self.HasBuff(Def) && GazeTuning.Armor != 0f) setArmor(self, self.armor + GazeTuning.Armor);
            };
        }
    }
}
