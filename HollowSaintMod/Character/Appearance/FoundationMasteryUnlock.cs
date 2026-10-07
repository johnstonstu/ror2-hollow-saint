using RoR2;
using RoR2.Achievements;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>One native unlock shared by the playable and display mastery skins.
    /// Include this definition in the survivor's content pack before catalogs initialize.</summary>
    internal static class FoundationMasteryUnlock
    {
        internal const string AchievementIdentifier = "HollowSaintClearGameMonsoon";
        internal const string UnlockableIdentifier = "Skins.HollowSaint.Mastery";
        private static UnlockableDef unlockable;

        internal static UnlockableDef EnsureUnlockable(Sprite icon = null)
        {
            if (!unlockable)
            {
                unlockable = ScriptableObject.CreateInstance<UnlockableDef>();
                unlockable.cachedName = UnlockableIdentifier;
                unlockable.nameToken = "HS_SKIN_CRIMSON_VOW_NAME";
                unlockable.hidden = false;
            }
            if (icon) unlockable.achievementIcon = icon;
            return unlockable;
        }
    }

    /// <summary>Uses the installed game's own survivor mastery policy: a native win
    /// ending on a difficulty marked countsAsHardMode while meeting this body requirement.
    /// Native AchievementManager discovers the attribute in loaded mod assemblies when
    /// RoR2Application.isModded is set (the normal BepInEx/R2API modded startup).
    /// This tracker observes future run endings; it does not replay historical clears.
    /// The game's ordinary achievement manager owns persistence and the coin reward.</summary>
    [RegisterAchievement(FoundationMasteryUnlock.AchievementIdentifier,
        FoundationMasteryUnlock.UnlockableIdentifier, null, 10u, null)]
    public sealed class HollowSaintClearGameMonsoonAchievement : BasePerSurvivorClearGameMonsoonAchievement
    {
        // GameLibs exposes this native protected virtual as public in its publicized API.
        public override BodyIndex LookUpRequiredBodyIndex() => BodyCatalog.FindBodyIndex("HollowSaintBody");
    }
}
