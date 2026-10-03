using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Last contributor's color for shared enemy effects. Buffs replicate cosmetic snapshots.</summary>
    internal static class VictimFxTheme
    {
        private static BuffDef staticTheme, markTheme;
        private static float scan;
        internal static void Register()
        {
            if (staticTheme) return;
            staticTheme = KitContent.MakeBuff("bdHsStaticColor", Color.white, canStack: true, isDebuff: false, hidden: true);
            markTheme = KitContent.MakeBuff("bdHsMarkColor", Color.white, canStack: true, isDebuff: false, hidden: true);
        }
        internal static void Remember(CharacterBody victim, CharacterBody source, bool mark = false)
        {
            if (!NetworkServer.active || !victim) return;
            var buff = mark ? markTheme : staticTheme;
            if (!buff) return;
            int code = SkinFxPalette.ForBody(source).Index + 1;
            if (victim.GetBuffCount(buff) != code) victim.SetBuffCount(buff.buffIndex, code);
        }
        internal static SkinFxPalette ForVictim(CharacterBody victim, bool mark = false)
        {
            var buff = mark ? markTheme : staticTheme;
            return SkinFxPalette.ForIndex((uint)Mathf.Max(0, victim && buff ? victim.GetBuffCount(buff) - 1 : 0));
        }
        internal static void Tick(float dt)
        {
            if (!NetworkServer.active || !staticTheme || !markTheme) return;
            scan -= dt;
            if (scan > 0f) return;
            scan = 0.5f;
            foreach (var victim in CharacterBody.readOnlyInstancesList)
            {
                if (!victim) continue;
                bool charge = Has(victim, Storm.StormServer.StaticBuff) || Has(victim, Storm.StormServer.ShockedBuff) || Has(victim, Storm.StormServer.ElectrocutedBuff);
                ClearIfUnused(victim, staticTheme, charge);
                ClearIfUnused(victim, markTheme, false); // v0.9: no Conductor Mark; clear any leftover theme buff
            }
        }
        private static bool Has(CharacterBody victim, BuffDef buff) => buff && victim.HasBuff(buff);
        private static void ClearIfUnused(CharacterBody victim, BuffDef color, bool active)
        {
            if (!active && victim.GetBuffCount(color) != 0) victim.SetBuffCount(color.buffIndex, 0);
        }
    }
}
