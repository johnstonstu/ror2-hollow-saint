using System;
using System.IO;
using Path = System.IO.Path;
using GUI = UnityEngine.GUI;
using GUIStyle = UnityEngine.GUIStyle;
using Rect = UnityEngine.Rect;
using Color = UnityEngine.Color;
using FontStyle = UnityEngine.FontStyle;
using BepInEx;
using BepInEx.Logging;
using RoR2;
using RoR2.ContentManagement;
using HollowSaint.FoundationKit.SpearDischarge;

namespace HollowSaint
{
    [BepInPlugin(Guid, "Hollow Saint", Version)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.prefab", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.bepis.r2api.language", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.johnstonstu.hollowsaint";
        // Bump on every staged build; keep in sync with Package/manifest.json. 1.0.0 = first public release.
        public const string Version = "1.0.1";
        // Two keywords describing what this build changed; shown in the top-left build tag (0.x builds only).
        public const string BuildKeywords = "release";
        private GUIStyle tagStyle;

        /// <summary>Set by the dev showcase recording.</summary>
        internal static bool HideBuildTag;

        /// <summary>Only dev builds (0.x) show the version tag in the top-left corner.</summary>
        private static bool ShowBuildTag { get { return !HideBuildTag && Version.StartsWith("0."); } }

        private void OnGUI()
        {
            if (!ShowBuildTag) return;
            if (tagStyle == null)
            {
                tagStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            }
            string text = "Hollow Saint v" + Version + " | " + BuildKeywords;
            var rect = new Rect(10f, 30f, 600f, 22f);
            tagStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, tagStyle);
            tagStyle.normal.textColor = new Color(0.45f, 0.95f, 1f, 0.95f);
            GUI.Label(rect, text, tagStyle);
        }
        internal static HsLog Log;
        internal static string DirectoryPath;
        private FoundationContent content;

        private void Awake()
        {
            Log = new HsLog(Logger);
            DirectoryPath = Path.GetDirectoryName(Info.Location);
            DevAutopilot.TryStart();
            try
            {
                // Static strings (name, lore, skins) come from HollowSaint.language. English in that
                // file is the fallback for every other language. Descriptions are filled in after Bind.
                HollowSaint.FoundationKit.KitLanguage.Install();

                HollowSaint.FoundationKit.KitConfig.Bind(Config);
                HollowSaint.FoundationKit.KitConfig.TryRegisterOptionsMenu();

                // Server-side kit hooks. Content (states, skills, buffs) is registered from
                // FoundationContent so everything lands in one content pack.
                HollowSaint.FoundationKit.Storm.StormServer.Install();

                content = new FoundationContent();
                ContentManager.collectContentPackProviders += Collect;
                RoR2Application.onLoad += VerifyCatalog;
                CharacterBody.onBodyStartGlobal += ReportBody;
                Log.LogAlways("Hollow Saint " + Version + " loaded.");
            }
            catch (Exception error) { Log.LogError("Hollow Saint startup failed: " + error); throw; }
        }

        private void Update()
        {
            HollowSaint.FoundationKit.Vfx.MarkReticles.Tick(UnityEngine.Time.deltaTime);
            HollowSaint.FoundationKit.Storm.StaticFx.Tick(UnityEngine.Time.deltaTime);
        }

        private void FixedUpdate()
        {
            HollowSaint.FoundationKit.Storm.StormServer.Tick(UnityEngine.Time.fixedDeltaTime);
        }

        private void Collect(ContentManager.AddContentPackProviderDelegate add) => add(content);
        private void VerifyCatalog()
        {
            try
            {
                HollowSaint.FoundationKit.Storm.RoyalCapacitorFx.VerifyCatalog();
                string detail;
                bool kitOk = KitRegistration.Verify(content.Body, out detail);
                if (kitOk) Log.LogInfo("HOLLOW_SAINT_KIT_VERIFIED " + detail);
                else Log.LogError("HOLLOW_SAINT_KIT_VERIFY_FAILED " + detail);
            }
            catch (Exception error) { Log.LogError("Hollow Saint kit verification threw: " + error); }
            try
            {
                var s = content.Survivor;
                var body = content.Body ? content.Body.GetComponent<CharacterBody>() : null;
                int ordered = Array.IndexOf(SurvivorCatalog.orderedSurvivorDefs as SurvivorDef[] ?? new System.Collections.Generic.List<SurvivorDef>(SurvivorCatalog.orderedSurvivorDefs).ToArray(), s);
                Log.LogInfo("HOLLOW_SAINT_SELECT_DIAG index=" + (int)s.survivorIndex + " orderedPos=" + ordered +
                    " hidden=" + s.hidden + " unlockable=" + (s.unlockableDef ? s.unlockableDef.cachedName : "none") +
                    " portrait=" + (body && body.portraitIcon ? body.portraitIcon.name + " " + body.portraitIcon.width + "x" + body.portraitIcon.height : "NULL") +
                    " expansionReq=" + (content.Body && content.Body.GetComponent<RoR2.ExpansionManagement.ExpansionRequirementComponent>() ? "YES" : "no") +
                    " display=" + (s.displayPrefab ? s.displayPrefab.name : "NULL"));
            }
            catch (Exception error) { Log.LogError("HOLLOW_SAINT_SELECT_DIAG threw: " + error); }
            try { FoundationAudit.Write(content.Body, content.Survivor, content.Display); }
            catch (Exception error) { Log.LogError("Hollow Saint catalog verification failed: " + error); throw; }
        }
        private void ReportBody(CharacterBody body)
        {
            if (body.baseNameToken != "HS_NAME") return;
            FoundationAudit.ReportGroundReference(body, "body-start");
            Log.LogInfo("HOLLOW_SAINT_BODY_STARTED nativeInput=" + (body.inputBank != null) +
                " nativeMotor=" + (body.characterMotor != null) + " authority=" + body.hasEffectiveAuthority +
                " server=" + UnityEngine.Networking.NetworkServer.active +
                " skills=" + SkillName(body.skillLocator ? body.skillLocator.primary : null) + "," +
                SkillName(body.skillLocator ? body.skillLocator.secondary : null) + "," +
                SkillName(body.skillLocator ? body.skillLocator.utility : null) + "," +
                SkillName(body.skillLocator ? body.skillLocator.special : null));
        }
        private static string SkillName(GenericSkill slot)
        {
            return slot != null && slot.skillDef != null ? slot.skillDef.skillName : "none";
        }
        private void OnDestroy()
        {
            ContentManager.collectContentPackProviders -= Collect;
            RoR2Application.onLoad -= VerifyCatalog;
            CharacterBody.onBodyStartGlobal -= ReportBody;
            HollowSaint.FoundationKit.Storm.StormServer.Uninstall();
            HollowSaint.FoundationKit.Vfx.CustomSoundBank.Unload();
        }
    }
}
