using System;
using System.Linq;
using R2API;
using RoR2;
using UnityEngine;

namespace HollowSaint
{
    internal static class FoundationBody
    {
        // Movement (config section "0. Movement"). Commando: speed 7, jump 15, sprint 1.45.
        // Playtest: the Saint felt slow, so it is a little faster and floatier than Commando.
        internal static float BaseMoveSpeed = 7.7f;
        internal static float SprintMultiplier = 1.45f;
        internal static float BaseJumpPower = 17f;
        // Damage. Commando: 12 +2.4/level. 1.0.1 raises only the flat base for an early-game buff.
        internal static float BaseDamage = 15f;
        // Armor. Commando: 0. Flat, no per-level growth.
        internal static float BaseArmor = 15f;
        private static GameObject prefab;

        /// <summary>Applies the movement numbers to the body prefab and, when a run is live,
        /// to spawned Hollow Saint bodies (called at build and whenever a slider changes).</summary>
        internal static void ApplyMovement()
        {
            try
            {
                if (prefab != null)
                {
                    var prefabBody = prefab.GetComponent<CharacterBody>();
                    if (prefabBody != null) ApplyTo(prefabBody);
                }
                var list = CharacterBody.readOnlyInstancesList;
                for (int i = 0; i < list.Count; i++)
                {
                    var live = list[i];
                    if (live == null || live.baseNameToken != "HS_NAME") continue;
                    ApplyTo(live);
                    live.MarkAllStatsDirty();
                }
            }
            catch (Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_MOVEMENT_APPLY_FAILED " + error.Message); }
        }

        private static void ApplyTo(CharacterBody target)
        {
            target.baseMoveSpeed = BaseMoveSpeed;
            target.sprintingSpeedMultiplier = SprintMultiplier;
            target.baseJumpPower = BaseJumpPower;
        }

        public static bool ItemDisplays = true;

        internal static GameObject Build(GameObject modelAsset)
        {
            var bodyObject = PrefabAPI.InstantiateClone(LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody"), "HollowSaintBody", true);
            var body = bodyObject.GetComponent<CharacterBody>();
            var locator = bodyObject.GetComponent<ModelLocator>();
            var oldModel = locator.modelTransform;
            var commandoModel = oldModel ? oldModel.GetComponent<CharacterModel>() : null;
            var commandoDisplays = commandoModel ? commandoModel.itemDisplayRuleSet : null;
            var model = UnityEngine.Object.Instantiate(modelAsset, locator.modelBaseTransform);
            model.name = "mdlHollowSaint";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var mainRenderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Single(r => r.name.StartsWith("HF BODY", StringComparison.Ordinal));
            int bodyParts = FoundationMeshSplitter.Split(mainRenderer);
            if (bodyParts == 0)
            {
                var surfaces = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(r => r.sharedMesh && r.sharedMesh.name.StartsWith("BodySurface", StringComparison.Ordinal)).ToArray();
                foreach (var surface in surfaces) surface.updateWhenOffscreen = true;
                bodyParts = surfaces.Length;
            }
            Plugin.Log.LogInfo("Hollow Saint body material slots split into " + bodyParts + " renderers.");
            FoundationMaterials.Apply(model);
            locator.modelBaseTransform.localPosition = new Vector3(0, -0.9f, 0);
            locator.modelTransform = model.transform;
            // Destroy only the cloned vanilla visual hierarchy, never the source prefab.
            UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
            body.baseNameToken = "HS_NAME";
            body.baseJumpCount = 2; // storm double jump (BodyFx draws the lightning cloud)
            prefab = bodyObject;
            ApplyTo(body); // levelMoveSpeed / levelJumpPower stay at Commando's values
            body.baseDamage = BaseDamage;
            body.levelDamage = 2.4f;
            body.baseArmor = BaseArmor;
            body.levelArmor = 0f;
            body.subtitleNameToken = "HS_SUBTITLE";
            body.bodyColor = new Color(0.2f, 0.9f, 1f);
            body.rootMotionInMainState = false;
            body.preferredPodPrefab = null;
            body.aimOriginTransform = MakeChild(bodyObject.transform, "HollowSaintAimOrigin", new Vector3(0, 0.65f, 0));
            var direction = bodyObject.GetComponent<CharacterDirection>();
            direction.targetTransform = locator.modelBaseTransform;
            direction.modelAnimator = model.GetComponent<Animator>();
            direction.overrideAnimatorForwardTransform = null;
            direction.rootMotionAccumulator = null;
            direction.driveFromRootRotation = false;
            var saintModel = FoundationSkin.Attach(model, body);
            // v0.8 first pass: borrow Commando's item display rules. The Saint's ChildLocator uses the
            // same mount names (Head, Chest, Stomach, Pelvis, limbs, hands); gun-mounted items skip.
            if (ItemDisplays && commandoDisplays && saintModel) saintModel.itemDisplayRuleSet = commandoDisplays;
            var main = MakeChild(model.transform, "MainHurtbox", new Vector3(0, 1.2f, 0));
            main.gameObject.layer = LayerIndex.entityPrecise.intVal;
            var collider = main.gameObject.AddComponent<CapsuleCollider>();
            collider.height = 1.8f;
            collider.radius = 0.3f;
            collider.isTrigger = false;
            var hurt = main.gameObject.AddComponent<HurtBox>();
            var group = model.AddComponent<HurtBoxGroup>();
            hurt.healthComponent = bodyObject.GetComponent<HealthComponent>();
            hurt.isBullseye = true;
            hurt.isSniperTarget = true;
            hurt.damageModifier = HurtBox.DamageModifier.Normal;
            hurt.hurtBoxGroup = group;
            hurt.indexInGroup = 0;
            group.hurtBoxes = new[] { hurt };
            group.mainHurtBox = hurt;
            group.bullseyeCount = 1;
            FoundationMounts.Add(model, main);
            FoundationRagdoll.Add(model);
            model.AddComponent<FoundationPresentation>();
            return bodyObject;
        }

        internal static Transform MakeChild(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }
    }
}
