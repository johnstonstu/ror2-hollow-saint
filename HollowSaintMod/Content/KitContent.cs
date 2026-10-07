using System;
using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit
{

    /// <summary>
    /// Everything the kit contributes to the game catalogs. Modules add to this during
    /// FoundationContent.LoadStaticContentAsync; GenerateContentPackAsync copies it into
    /// the mod's own ContentPack. One pack, one path. Nothing goes through R2API
    /// ContentAddition, so there is no second pack registered under the same plugin.
    /// </summary>
    public static class KitContent
    {
        private static readonly List<Type> states = new List<Type>();
        private static readonly List<SkillDef> skillDefs = new List<SkillDef>();
        private static readonly List<SkillFamily> skillFamilies = new List<SkillFamily>();
        private static readonly List<BuffDef> buffDefs = new List<BuffDef>();
        private static readonly List<GameObject> projectiles = new List<GameObject>();
        private static readonly List<EffectDef> effects = new List<EffectDef>();

        public static SerializableEntityStateType AddState(Type stateType)
        {
            if (!states.Contains(stateType)) states.Add(stateType);
            return new SerializableEntityStateType(stateType);
        }

        public static SkillDef AddSkillDef(SkillDef def)
        {
            if (def != null && !skillDefs.Contains(def)) skillDefs.Add(def);
            return def;
        }

        public static SkillFamily AddSkillFamily(SkillFamily family)
        {
            if (family != null && !skillFamilies.Contains(family)) skillFamilies.Add(family);
            return family;
        }

        public static BuffDef AddBuff(BuffDef buff)
        {
            if (buff != null && !buffDefs.Contains(buff)) buffDefs.Add(buff);
            return buff;
        }

        public static GameObject AddProjectile(GameObject prefab)
        {
            if (prefab != null && !projectiles.Contains(prefab)) projectiles.Add(prefab);
            return prefab;
        }

        public static void AddEffect(GameObject effectPrefab)
        {
            if (effectPrefab != null) effects.Add(new EffectDef(effectPrefab));
        }

        internal static void PopulateInto(ContentPack pack)
        {
            pack.entityStateTypes.Add(states.ToArray());
            pack.skillDefs.Add(skillDefs.ToArray());
            pack.skillFamilies.Add(skillFamilies.ToArray());
            pack.buffDefs.Add(buffDefs.ToArray());
            pack.projectilePrefabs.Add(projectiles.ToArray());
            pack.effectDefs.Add(effects.ToArray());
            Plugin.Log.LogInfo("HOLLOW_SAINT_KIT_CONTENT states=" + states.Count +
                " skillDefs=" + skillDefs.Count + " families=" + skillFamilies.Count +
                " buffs=" + buffDefs.Count + " projectiles=" + projectiles.Count +
                " effects=" + effects.Count);
        }

        /// <summary>Builds and registers a buff. BuffDef.name must be unique.</summary>
        public static BuffDef MakeBuff(string name, Color color, bool canStack, bool isDebuff, bool hidden, string icon = null)
        {
            var buff = ScriptableObject.CreateInstance<BuffDef>();
            buff.name = name;
            buff.buffColor = color;
            buff.canStack = canStack;
            buff.isDebuff = isDebuff;
            buff.isHidden = hidden;
            buff.iconSprite = icon != null ? KitIcons.Sprite(icon) : null;
            if (buff.iconSprite == null) buff.isHidden = true; // never show an empty HUD slot
            return AddBuff(buff);
        }
    }
}
