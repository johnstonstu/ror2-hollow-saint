using System.Reflection;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ArcBolt;
using HollowSaint.FoundationKit.ArcStep;
using HollowSaint.FoundationKit.OpenCircuit;
using HollowSaint.FoundationKit.SpearDischarge;
using HollowSaint.FoundationKit.Stormspear;

namespace HollowSaint
{
    /// <summary>
    /// Installs the kit on the Hollow Saint body.
    ///
    /// How skills get onto the body: every GenericSkill slot on the cloned CommandoBody
    /// is re-pointed at a SkillFamily of our own. GenericSkill.Awake assigns
    /// skillFamily.defaultSkillDef on every spawn (verified in RoR2.dll IL), and the
    /// loadout screen lists skillFamily.variants, so the family is the only assignment
    /// that survives into a live body. The previous approach, SetSkillInternal on the
    /// prefab, was overwritten by Awake on spawn, which would have left Commando's
    /// skills in game. The clone also still pointed at Commando's own families, so
    /// editing those would have changed Commando too.
    ///
    /// The GenericSkill components themselves are untouched, so native input
    /// (PlayerCharacterMasterController -> InputBankTest -> SkillLocator) is unchanged.
    /// </summary>
    internal static class KitRegistration
    {
        private static readonly FieldInfo SkillFamilyField =
            typeof(GenericSkill).GetField("_skillFamily", BindingFlags.Instance | BindingFlags.NonPublic);

        private static SkillDef arcBolt, spear, arcStep, openCircuit, gaze;
        /// <summary>Open Circuit (alternate Special since v0.9.8); the autopilot equips it for the crown segments.</summary>
        internal static SkillDef OpenCircuitDef { get { return openCircuit; } }
        private static bool contentRegistered;

        /// <summary>Registers states, skills, buffs and projectiles into KitContent.
        /// Call once inside LoadStaticContentAsync, before the body is built.</summary>
        internal static void RegisterContent()
        {
            if (contentRegistered) return;
            contentRegistered = true;
            RegisterTokens();
            DischargeMeter.RegisterBuff();
            HollowSaint.FoundationKit.Storm.StormServer.RegisterBuffs();
            // Presentation must never be able to abort the content load.
            try
            {
                HollowSaint.FoundationKit.Vfx.KitFx.Register();
                HollowSaint.FoundationKit.Vfx.Ghosts.Build();
            }
            catch (System.Exception error)
            {
                Plugin.Log.LogError("HOLLOW_SAINT_VFX_REGISTER_FAILED (kit continues without custom VFX): " + error);
            }
            arcBolt = ArcBoltRegistration.RegisterArcBolt();
            spear = StormspearRegistration.Register();
            arcStep = ArcStepRegistration.RegisterArcStep();
            openCircuit = OpenCircuitRegistration.RegisterOpenCircuit();
            gaze = HollowSaint.FoundationKit.Gaze.GazeRegistration.Register();
        }

        /// <summary>Points the body's four slots at Hollow Saint families, sets the
        /// passive, and adds the per-body kit components.</summary>
        internal static void InstallOnBody(GameObject bodyObject)
        {
            var locator = bodyObject.GetComponent<SkillLocator>();
            if (locator == null)
            {
                Plugin.Log.LogError("Hollow Saint kit: body has no SkillLocator; kit not installed.");
                return;
            }
            if (SkillFamilyField == null)
            {
                Plugin.Log.LogError("Hollow Saint kit: GenericSkill._skillFamily not found; kit not installed.");
                return;
            }

            AddCrownMachine(bodyObject);
            AddMachine(bodyObject, HollowSaint.FoundationKit.Stormspear.StormspearRegistration.MachineName);

            int installed = 0;
            installed += Install(bodyObject, locator.primary, arcBolt, "Primary", s => locator.primary = s);
            installed += Install(bodyObject, locator.secondary, spear, "Secondary", s => locator.secondary = s);
            installed += Install(bodyObject, locator.utility, arcStep, "Utility", s => locator.utility = s);
            // v0.9.8 (Stu): Gaze of the Hollow is the default Special; Open Circuit is the alternate.
            installed += Install(bodyObject, locator.special, gaze ?? openCircuit, "Special", s => locator.special = s, gaze != null ? openCircuit : null);

            locator.passiveSkill = new SkillLocator.PassiveSkill
            {
                enabled = true,
                skillNameToken = KitTokens.StormName,
                skillDescriptionToken = KitTokens.StormDesc,
                keywordToken = KitTokens.KeywordStorm,
                icon = KitIcons.Sprite("passive_discharge")
            };

            var body = bodyObject.GetComponent<CharacterBody>();
            var portrait = KitIcons.Texture("portrait");
            if (body && portrait) body.portraitIcon = portrait;

            if (!bodyObject.GetComponent<DischargeMeter>()) bodyObject.AddComponent<DischargeMeter>();
            if (!bodyObject.GetComponent<HollowSaint.FoundationKit.Gaze.GazeFuelController>()) bodyObject.AddComponent<HollowSaint.FoundationKit.Gaze.GazeFuelController>();
            try
            {
                if (!bodyObject.GetComponent<HollowSaint.FoundationKit.Gaze.GazeTimerHud>())
                    bodyObject.AddComponent<HollowSaint.FoundationKit.Gaze.GazeTimerHud>();
            }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_TIMER_REGISTRATION_FAILED " + error); }
            if (!bodyObject.GetComponent<SpearCarry>()) bodyObject.AddComponent<SpearCarry>();
            if (!bodyObject.GetComponent<HollowSaint.FoundationKit.Stormspear.StormspearCharge>()) bodyObject.AddComponent<HollowSaint.FoundationKit.Stormspear.StormspearCharge>();
            if (!bodyObject.GetComponent<HollowSaint.FoundationKit.Storm.ThunderboltDriver>()) bodyObject.AddComponent<HollowSaint.FoundationKit.Storm.ThunderboltDriver>();
            if (!bodyObject.GetComponent<OpenCircuitPulseDriver>()) bodyObject.AddComponent<OpenCircuitPulseDriver>();
            try
            {
                if (!bodyObject.GetComponent<FoundationKit.OpenCircuit.Fx.OpenCircuitDomeFx>())
                    bodyObject.AddComponent<FoundationKit.OpenCircuit.Fx.OpenCircuitDomeFx>();
            }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CIRCUIT_DOME_REGISTRATION_FAILED " + error); }
            if (!bodyObject.GetComponent<HollowSaint.FoundationKit.Vfx.BodyFx>()) bodyObject.AddComponent<HollowSaint.FoundationKit.Vfx.BodyFx>();
            if (!bodyObject.GetComponent<HollowSaint.FoundationKit.Storm.StormChargeHalo>()) bodyObject.AddComponent<HollowSaint.FoundationKit.Storm.StormChargeHalo>();

            Plugin.Log.LogInfo("HOLLOW_SAINT_KIT_INSTALLED slots=" + installed +
                " primary=" + Describe(locator.primary) + " secondary=" + Describe(locator.secondary) +
                " utility=" + Describe(locator.utility) + " special=" + Describe(locator.special));
        }

        /// <summary>
        /// Open Circuit runs on its own state machine so its 1.2 s cast never blocks Arc Bolt
        /// or the spear on "Weapon", and pressing those during the unfold cannot cancel a
        /// crown whose cooldown is already spent. The machine is appended to the body's
        /// NetworkStateMachine (so the server runs a copy for remote players, which is where
        /// the buff is applied) and to the death and stun idle lists (so dying or being
        /// stunned mid-cast resets it like the vanilla machines).
        /// </summary>
        internal const string CrownMachineName = "Crown";

        private static void AddCrownMachine(GameObject bodyObject) { AddMachine(bodyObject, CrownMachineName); }

        /// <summary>Adds a networked, death- and stun-reset side machine ("Crown", "Spear").</summary>
        private static void AddMachine(GameObject bodyObject, string machineName)
        {
            foreach (var existing in bodyObject.GetComponents<EntityStateMachine>())
                if (existing.customName == machineName) return;

            var machine = bodyObject.AddComponent<EntityStateMachine>();
            machine.customName = machineName;
            machine.initialStateType = new EntityStates.SerializableEntityStateType(typeof(EntityStates.Idle));
            machine.mainStateType = new EntityStates.SerializableEntityStateType(typeof(EntityStates.Idle));

            var networker = bodyObject.GetComponent<NetworkStateMachine>();
            var field = typeof(NetworkStateMachine).GetField("stateMachines", BindingFlags.Instance | BindingFlags.NonPublic);
            if (networker != null && field != null)
                field.SetValue(networker, Append((EntityStateMachine[])field.GetValue(networker), machine));
            else
                Plugin.Log.LogError("Hollow Saint kit: could not network the " + machineName + " state machine; it will not work for clients.");

            var death = bodyObject.GetComponent<CharacterDeathBehavior>();
            if (death != null) death.idleStateMachine = Append(death.idleStateMachine, machine);
            var hurt = bodyObject.GetComponent<SetStateOnHurt>();
            if (hurt != null) hurt.idleStateMachine = Append(hurt.idleStateMachine, machine);
        }

        private static EntityStateMachine[] Append(EntityStateMachine[] source, EntityStateMachine item)
        {
            var list = new System.Collections.Generic.List<EntityStateMachine>(source ?? new EntityStateMachine[0]);
            if (!list.Contains(item)) list.Add(item);
            return list.ToArray();
        }

        /// <summary>The first def is the slot's default; alternates are extra loadout variants.</summary>
        private static int Install(GameObject bodyObject, GenericSkill slot, SkillDef def, string slotName,
            System.Action<GenericSkill> assignToLocator, params SkillDef[] alternates)
        {
            if (def == null)
            {
                Plugin.Log.LogError("Hollow Saint kit: no SkillDef for " + slotName + "; slot left as is.");
                return 0;
            }
            if (slot == null)
            {
                slot = bodyObject.AddComponent<GenericSkill>();
                assignToLocator(slot);
            }
            var family = ScriptableObject.CreateInstance<SkillFamily>();
            ((ScriptableObject)family).name = "HollowSaint" + slotName + "Family";
            var variants = new System.Collections.Generic.List<SkillFamily.Variant> { Variant(def) };
            foreach (var alternate in alternates)
                if (alternate != null) variants.Add(Variant(alternate));
            family.variants = variants.ToArray();
            KitContent.AddSkillFamily(family);
            SkillFamilyField.SetValue(slot, family);
            slot.skillName = def.skillName;
            return 1;
        }

        private static SkillFamily.Variant Variant(SkillDef def)
        {
            return new SkillFamily.Variant
            {
                skillDef = def,
                unlockableDef = null,
                viewableNode = new ViewablesCatalog.Node(def.skillNameToken, false, null)
            };
        }

        private static string Describe(GenericSkill slot)
        {
            if (slot == null || slot.skillFamily == null) return "none";
            var def = slot.skillFamily.defaultSkillDef;
            return def != null ? def.skillName : "empty";
        }

        /// <summary>Checks, after catalogs are built, that the live body prefab resolves
        /// each slot to our skill. Logged for the playtest.</summary>
        internal static bool Verify(GameObject bodyPrefab, out string detail)
        {
            var locator = bodyPrefab != null ? bodyPrefab.GetComponent<SkillLocator>() : null;
            if (locator == null) { detail = "no SkillLocator"; return false; }
            bool ok = Is(locator.primary, arcBolt) && Is(locator.secondary, spear) &&
                      Is(locator.utility, arcStep) && Is(locator.special, gaze ?? openCircuit);
            detail = "primary=" + Describe(locator.primary) + " secondary=" + Describe(locator.secondary) +
                     " utility=" + Describe(locator.utility) + " special=" + Describe(locator.special);
            return ok;
        }

        private static bool Is(GenericSkill slot, SkillDef def)
        {
            return slot != null && slot.skillFamily != null && def != null &&
                   slot.skillFamily.defaultSkillDef == def && def.skillIndex >= 0;
        }

        private static void RegisterTokens()
        {
            // Names, lore, outros and skins are in HollowSaint.language (Install is safe to repeat).
            // The logbook looks up lore by swapping _NAME for _LORE on the body's name token, so
            // HS_NAME becomes HS_LORE. HS_BODY_LORE covers the other form. Both are in the file.
            // Descriptions carry live numbers, so KitDescriptions fills those templates on every
            // config change and again when the player changes language.
            HollowSaint.FoundationKit.KitLanguage.Install();
            HollowSaint.FoundationKit.KitDescriptions.RegisterAll();
        }
    }
}
