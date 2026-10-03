using System;
using System.Collections;
using System.IO;
using System.Linq;
using Path = System.IO.Path;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;

namespace HollowSaint
{
    internal sealed class FoundationContent : IContentPackProvider
    {
        private readonly ContentPack pack = new ContentPack();
        private AssetBundle bundle;
        private bool loaded;
        internal static GameObject SpearModel { get; private set; }
        internal GameObject Body { get; private set; }
        internal GameObject Display { get; private set; }
        internal SurvivorDef Survivor { get; private set; }
        public string identifier => Plugin.Guid;

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            try
            {
                bundle = AssetBundle.LoadFromFile(Path.Combine(Plugin.DirectoryPath, "hollowsaintassets"));
                if (!bundle) throw new InvalidOperationException("Hollow Saint asset bundle could not be loaded");
                FoundationLightMaps.Load(bundle);
                var modelAsset = bundle.LoadAsset<GameObject>("mdlHollowSaint");
                if (!modelAsset) throw new InvalidOperationException("Bundle lacks mdlHollowSaint");
                SpearModel = bundle.LoadAsset<GameObject>("mdlConduitSpear");
                if (!SpearModel) throw new InvalidOperationException("Bundle lacks fitted mdlConduitSpear; use bundle13 with this DLL");
                // Kit content first: skill families are created while installing on the body
                // and must exist before GenerateContentPackAsync copies KitContent.
                KitRegistration.RegisterContent();
                Body = FoundationBody.Build(modelAsset);
                KitRegistration.InstallOnBody(Body);
                var master = PrefabAPI.InstantiateClone(LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterMasters/CommandoMonsterMaster"), "HollowSaintMonsterMaster", true);
                master.GetComponent<CharacterMaster>().bodyPrefab = Body;
                var display = PrefabAPI.InstantiateClone(modelAsset, "HollowSaintDisplay", false);
                Display = display;
                FoundationMaterials.Apply(display);
                // The select-screen mannequin needs this pair. RoR2's
                // SurvivorMannequinSlotController.ApplyLoadoutToMannequinInstance does
                // GetComponentInChildren<ModelSkinController>() and dereferences the
                // result unguarded, so omitting it throws a NullReferenceException on
                // every loadout change. body is null here and is safe: Start() guards on
                // it and ApplySkinAsync only touches characterModel.forceUpdate.
                FoundationSkin.Attach(display, null);
                display.AddComponent<FoundationPresentation>();
                display.AddComponent<HollowSaint.FoundationKit.Vfx.DisplayFx>();
                Survivor = ScriptableObject.CreateInstance<SurvivorDef>();
                Survivor.cachedName = "HollowSaint";
                Survivor.bodyPrefab = Body;
                Survivor.displayPrefab = display;
                Survivor.displayNameToken = "HS_NAME";
                Survivor.descriptionToken = "HS_DESCRIPTION";
                Survivor.outroFlavorToken = "HS_OUTRO";
                Survivor.mainEndingEscapeFailureFlavorToken = "HS_OUTRO_FAILURE";
                Survivor.primaryColor = new Color(0.2f, 0.9f, 1f);
                Survivor.desiredSortPosition = 100;
                pack.bodyPrefabs.Add(new[] { Body });
                pack.masterPrefabs.Add(new[] { master });
                pack.survivorDefs.Add(new[] { Survivor });
                loaded = true;
                Plugin.Log.LogInfo("Hollow Saint assets and native body constructed.");
            }
            // Content loading is part of the game's init routine; rethrowing would stop the whole
            // game from launching, so a broken install only leaves Hollow Saint unregistered.
            catch (Exception error) { Plugin.Log.LogError("Hollow Saint content load failed; Hollow Saint is disabled: " + error); }
            args.ReportProgress(1);
            yield break;
        }
        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            if (!loaded) { args.ReportProgress(1); yield break; }
            // The game owns the output identifier; populate its public collections directly.
            args.output.bodyPrefabs.Add(pack.bodyPrefabs.ToArray());
            args.output.masterPrefabs.Add(pack.masterPrefabs.ToArray());
            args.output.survivorDefs.Add(pack.survivorDefs.ToArray());
            HollowSaint.FoundationKit.KitContent.PopulateInto(args.output);
            args.ReportProgress(1);
            yield break;
        }
        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args) { args.ReportProgress(1); yield break; }
    }
}
