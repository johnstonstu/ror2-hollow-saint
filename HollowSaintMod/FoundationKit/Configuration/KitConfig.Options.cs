using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace HollowSaint.FoundationKit
{
    public static partial class KitConfig
    {
        /// <summary>Adds the options page when Risk of Options is loaded. Kept in a separate
        /// method so the RiskOfOptions types are only touched when the mod is present.</summary>
        internal static void TryRegisterOptionsMenu()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions")) return;
            try { RegisterOptionsMenu(); }
            catch (Exception error) { Plugin.Log.LogWarning("Risk of Options page failed: " + error.Message); }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RegisterOptionsMenu()
        {
            RiskOfOptions.ModSettingsManager.SetModDescription("Hollow Saint balance and debug settings. Values marked restart apply on the next launch.");
            var icon = KitIcons.Sprite("portrait");
            if (icon) RiskOfOptions.ModSettingsManager.SetModIcon(icon);
            foreach (var f in Floats)
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.StepSliderOption(f.Entry,
                    new RiskOfOptions.OptionConfigs.StepSliderConfig { min = f.Min, max = f.Max, increment = f.Step, restartRequired = f.Restart }));
            foreach (var i in Ints)
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.IntSliderOption(i.Entry,
                    new RiskOfOptions.OptionConfigs.IntSliderConfig { min = i.Min, max = i.Max, restartRequired = i.Restart }));
            foreach (var b in Bools)
                RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(b));
            if (SpearHand != null)
            {
                // Own tokens (HollowSaint.language) so the name, description and choices are translated;
                // Risk of Options would register fixed English tokens for them otherwise.
                var hand = new RiskOfOptions.Options.ChoiceOption(SpearHand);
                RiskOfOptions.ModSettingsManager.AddOption(hand, Plugin.Guid, "Hollow Saint", "HS_OPTION_SPEAR_HAND_NAME", "HS_OPTION_SPEAR_HAND_DESC");
                var tokens = typeof(RiskOfOptions.Options.ChoiceOption).GetField("_nameTokens", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (tokens != null && tokens.GetValue(hand) is string[] names && names.Length == 3)
                    tokens.SetValue(hand, new[] { "HS_OPTION_SPEAR_HAND_AUTO", "HS_OPTION_SPEAR_HAND_LEFT", "HS_OPTION_SPEAR_HAND_RIGHT" });
            }
        }
    }
}
