using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal static class FoundationAudit
    {
        internal static void Write(GameObject prefab, SurvivorDef survivor, GameObject display)
        {
            var checks = new List<string>();
            void Require(bool pass, string message)
            {
                checks.Add((pass ? "PASS " : "FAIL ") + message);
                if (!pass) throw new InvalidOperationException(message);
            }
            try
            {
                Require(prefab && BodyCatalog.FindBodyIndex(prefab) != BodyIndex.None, "Body registered in catalog");
                Require(SurvivorCatalog.allSurvivorDefs.Contains(survivor), "Survivor registered in catalog");
                Require(prefab.GetComponent<InputBankTest>(), "Native input bank retained");
                Require(prefab.GetComponent<CharacterMotor>(), "Native character motor retained");
                Require(prefab.GetComponent<SkillLocator>(), "Native skill slots retained");
                Require(prefab.GetComponent<EquipmentSlot>(), "Native equipment slot retained");
                var model = prefab.GetComponent<ModelLocator>().modelTransform;
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                Require(renderers.Length == 140, "All source surfaces retained in 140 game renderers");
                var bodyParts = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(r => r.sharedMesh && r.sharedMesh.name.StartsWith("BodySurface", StringComparison.Ordinal)).ToArray();
                Require(bodyParts.Length == 4 && Enumerable.Range(0, 4).All(i =>
                    bodyParts.Count(r => r.sharedMesh.name == "BodySurface" + i && r.updateWhenOffscreen &&
                        r.sharedMaterials.Length == 1 && r.sharedMesh &&
                        r.sharedMesh.subMeshCount == 1 && r.sharedMesh.GetTriangles(0).Length > 0) == 1),
                    "Four body material slots each have visible geometry and a dedicated renderer");
                var characterModel = model.GetComponent<CharacterModel>();
                Require(characterModel && characterModel.baseRendererInfos.Length == renderers.Length,
                    "CharacterModel manages every split renderer");
                Require(model.GetComponent<Animator>().avatar.isValid, "Imported avatar remains valid in game");
                var locator = model.GetComponent<ChildLocator>();
                Require(locator.Count == 23 && Enumerable.Range(0, locator.Count).All(i => locator.FindChild(i)), "All 23 attachment aliases resolve");
                Require(model.GetComponent<HurtBoxGroup>().mainHurtBox.healthComponent == prefab.GetComponent<HealthComponent>(), "Main hurtbox linked to health");
                Require(!prefab.GetComponentsInChildren<MonoBehaviour>(true).Any(c => c && c.GetType().Namespace == "HollowSaint.Preview"), "No preview scripts shipped");
                // Character-select mannequin guard. RoR2 dereferences
                // GetComponentInChildren<ModelSkinController>() without a null check, so a
                // display missing either half of the pair throws on every loadout change.
                Require(display && survivor.displayPrefab == display, "Display prefab is the survivor's registered display");
                var displaySkinController = display ? display.GetComponentInChildren<ModelSkinController>(true) : null;
                // Require throws on failure, so this also guards the dereferences below.
                Require(displaySkinController, "Select-screen display has a ModelSkinController");
                Require(display.GetComponentInChildren<CharacterModel>(true), "Select-screen display has a CharacterModel for the skin controller to write to");
                Require(display.GetComponentsInChildren<Renderer>(true).Length == renderers.Length &&
                    display.GetComponentsInChildren<Renderer>(true).All(r => r.sharedMaterials.Length == 1),
                    "Display uses the same complete single-material surface layout as the body");
                var displaySkins = FoundationSkin.OwnSkins(displaySkinController);
                var bodySkins = FoundationSkin.OwnSkins(model.GetComponent<ModelSkinController>());
                // Compare against the same enumeration FoundationSkin.Attach uses when it
                // builds rendererInfos. Using a different includeInactive setting here would
                // make the counts disagree.
                Require(displaySkins.Length > 0 &&
                    displaySkins[0].skinDefParams != null &&
                    displaySkins[0].skinDefParams.rendererInfos.Length ==
                        display.GetComponentsInChildren<Renderer>().Length,
                    "Display skin carries a renderer info per display renderer");
                Require(display.GetComponentInChildren<Animator>(true), "Select-screen display has an Animator");
                Require(bodySkins.Length == 5 && displaySkins.Length == 5 &&
                    bodySkins.Select(s => s.nameToken).SequenceEqual(displaySkins.Select(s => s.nameToken)),
                    "All five skins have matching body and mannequin order");
                Require(bodySkins.All(s => s.skinDefParams != null &&
                    s.skinDefParams.rendererInfos.Length == renderers.Length),
                    "Every body skin retains all renderers");
                Require(model.GetComponent<FoundationSkinAnimation>() &&
                    display.GetComponentInChildren<FoundationSkinAnimation>(true),
                    "Body and mannequin both animate cosmetic skin emission");
                ReportGroundReference(prefab.GetComponent<CharacterBody>(), "prefab");
                checks.Add("UNVERIFIED: physical controller feel, gameplay movement/death, equipment, multiplayer and final kit.");
                Plugin.Log.LogInfo("HOLLOW_SAINT_CATALOG_CHECKS_PASS");
            }
            // This runs inside RoR2Application.onLoad; an exception here aborts the game's init routine.
            catch (Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CATALOG_CHECK_FAILED " + error); }
            finally
            {
                // v0.9.17: the check file is a dev artifact; players' plugin folders stay untouched.
                try { if (HsLog.Enabled) File.WriteAllLines(Path.Combine(Plugin.DirectoryPath, "foundation-catalog-checks.txt"), checks); }
                catch (Exception error) { Plugin.Log.LogWarning("Hollow Saint could not write catalog checks: " + error.Message); }
            }
        }

        internal static void ReportGroundReference(CharacterBody body, string context)
        {
            var capsule = body ? body.GetComponent<CapsuleCollider>() : null;
            var locator = body ? body.GetComponent<ModelLocator>() : null;
            var model = locator ? locator.modelTransform : null;
            if (!capsule || !model)
            {
                Plugin.Log.LogWarning("HOLLOW_SAINT_GROUND_REFERENCE unavailable context=" + context);
                return;
            }
            Vector3 center = capsule.transform.TransformPoint(capsule.center);
            float bottom = center.y - capsule.height * Mathf.Abs(capsule.transform.lossyScale.y) * 0.5f;
            Plugin.Log.LogInfo("HOLLOW_SAINT_GROUND_REFERENCE context=" + context +
                " capsuleHeight=" + capsule.height.ToString("F3") + " capsuleCenter=" + capsule.center +
                " modelOriginAboveCapsuleBottom=" + (model.position.y - bottom).ToString("F3"));
        }
    }
}
