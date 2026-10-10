using EntityStates;
using RoR2.Skills;
using UnityEngine;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    internal static class ChargedStormRegistration
    {
        internal static SkillDef Cloud { get; private set; }
        internal static SkillDef Orb { get; private set; }
        internal static void Register()
        {
            if (Cloud) return;
            Cloud = Make("HollowSaintThundercloud", "HS_SKILL_THUNDERCLOUD", typeof(Thundercloud.ThundercloudState),
                KitRegistration.CrownMachineName, ChargedStormTuning.CloudCooldown, "skill_thundercloud");
            Orb = Make("HollowSaintHollowedOrb", "HS_SKILL_HOLLOWED_ORB", typeof(HollowedOrb.HollowedOrbState),
                Stormspear.StormspearRegistration.MachineName, ChargedStormTuning.OrbCooldown, "skill_hollowed_orb");
            ((StoredChargeSkillDef)Orb).allowsUnchargedCast = true;
            ((StoredChargeSkillDef)Cloud).followsCloudFreeCast = true;
            Orb.dontAllowPastMaxStocks = true; // 1.3.1: Backup Magazine adds Orb hits, not casts (StoredChargeDriver)
            // Keywords explain the storm interaction: both prime Static, the cloud also Shocks.
            Orb.keywordTokens = new[] { KitTokens.KeywordStatic };
            Cloud.keywordTokens = new[] { KitTokens.KeywordShocked, KitTokens.KeywordStatic };
            StoredChargeTransport.Install();
            try { ChargedStormEffects.Register(); }
            catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_CHARGED_STORM_FX_REGISTER " + error); }
        }
        private static SkillDef Make(string name, string token, System.Type state, string machine, float cooldown, string icon)
        {
            var def = ScriptableObject.CreateInstance<StoredChargeSkillDef>();
            ((ScriptableObject)def).name = def.skillName = name;
            def.skillNameToken = token + "_NAME"; def.skillDescriptionToken = token + "_DESC";
            def.icon = KitIcons.Sprite(icon); def.keywordTokens = System.Array.Empty<string>();
            def.activationState = KitContent.AddState(state); def.activationStateMachineName = machine;
            def.interruptPriority = InterruptPriority.PrioritySkill;
            def.baseRechargeInterval = ChargedStormTuning.Bound(cooldown, 1f, 60f);
            def.baseMaxStock = def.requiredStock = def.stockToConsume = def.rechargeStock = 1;
            def.beginSkillCooldownOnSkillEnd = true; def.resetCooldownTimerOnUse = false;
            def.mustKeyPress = true; def.isCombatSkill = true; def.cancelSprintingOnActivation = true;
            def.canceledFromSprinting = false; def.hideStockCount = true;
            return KitContent.AddSkillDef(def);
        }
    }
}
