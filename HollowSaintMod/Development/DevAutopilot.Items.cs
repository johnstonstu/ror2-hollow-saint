using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HollowSaint.FoundationKit;
using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;

namespace HollowSaint
{
    /// <summary>
    /// v0.9.9 item playtest (HS_SEGMENTS=items). Gives the Saint a series of item loadouts and
    /// plays the same combat routine against a healed tank (Stone Golem) with a killable pack of
    /// Lemurians beside it. For every loadout it records damage by source, OnHitEnemy / OnHitAll
    /// calls by source (Brilliant Behemoth needs OnHitAll), the deepest proc recursion, kills,
    /// skill stocks, slow frames and any errors, so item behaviour is measured rather than guessed.
    /// Then: Heretic replacements, movement items, no-cooldown spam, and a Dio's death + respawn.
    /// Results: items.txt next to trace.txt.
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private sealed class Tally { public float damage; public int hits, onHit, onHitAll; }
        private readonly Dictionary<string, Tally> tally = new Dictionary<string, Tally>();
        private readonly StringBuilder items = new StringBuilder();
        private bool tallying;
        private int hitDepth, maxHitDepth, onHitCalls, kills, slowFrames, statsErrors;
        private float worstFrame, lastShotReal = -10f, tallyStart;
        private CharacterBody tank;
        private readonly List<CharacterBody> pack = new List<CharacterBody>();

        private IEnumerator ItemSegments()
        {
            GlobalEventManager.onServerDamageDealt += OnDamageDealt;
            GlobalEventManager.onCharacterDeathGlobal += OnDeath;
            On.RoR2.GlobalEventManager.OnHitEnemy += CountHitEnemy;
            On.RoR2.GlobalEventManager.OnHitAll += CountHitAll;
            StartCoroutine(FrameWatch());
            try
            {
                // The three invulnerable pose dummies would soak shots meant for the tank.
                foreach (var d in dummies) if (d && d.healthComponent) { d.healthComponent.godMode = false; d.healthComponent.Suicide(); }
                dummies.Clear();
                SpawnTank();
                yield return Wait(1.5f);
                items.AppendLine("ITEMS version=" + Plugin.Version + " level=" + (pilot ? pilot.level : 0) + " baseDamage=" + (pilot ? pilot.damage.ToString("0.0") : "-"));

                yield return DisplaySegment();
                yield return Loadout("base", false);
                yield return Loadout("onhit", false, "Missile", 3, "ChainLightning", 3, "StickyBomb", 3, "BleedOnHit", 3, "BounceNearby", 2, "StunChanceOnHit", 2);
                yield return Loadout("behemoth", false, "Behemoth", 2);
                yield return Loadout("bands", false, "IceRing", 1, "FireRing", 1);
                yield return Loadout("singularity", false, "ElementalRingVoid", 1);
                yield return Loadout("void", false, "ChainLightningVoid", 3, "MissileVoid", 3, "BleedOnHitVoid", 3, "CritGlassesVoid", 3);
                yield return Loadout("primary-items", false, "PrimarySkillShuriken", 3, "IncreasePrimaryDamage", 3, "LightningStrikeOnHit", 2, "StunAndPierce", 2);
                yield return Loadout("crit", false, "CritGlasses", 10, "AttackSpeedOnCrit", 2, "HealOnCrit", 2);
                yield return Loadout("onkill", false, "ExplodeOnDeath", 3, "IgniteOnKill", 3, "BarrierOnKill", 3, "ExplodeOnDeathVoid", 2);
                yield return Loadout("atkspd", false, "Syringe", 15, "AttackSpeedAndMoveSpeed", 5);
                yield return Loadout("lightflux", false, "HalfAttackSpeedHalfCooldowns", 2);
                yield return Loadout("stocks", false, "SecondarySkillMagazine", 4, "AlienHead", 3, "UtilitySkillMagazine", 2, "EquipmentMagazineVoid", 2, "LunarBadLuck", 1);
                string[] late = { "Missile", "3", "ChainLightning", "3", "StickyBomb", "2", "BleedOnHit", "3", "BounceNearby", "2", "Behemoth", "1",
                    "IceRing", "1", "FireRing", "1", "ChainLightningVoid", "2", "LightningStrikeOnHit", "2", "Syringe", "20", "CritGlasses", "8",
                    "SecondarySkillMagazine", "3", "AlienHead", "2", "ExplodeOnDeath", "2", "IgniteOnKill", "2", "PrimarySkillShuriken", "2", "LunarDagger", "1" };
                yield return Loadout("late-gaze", false, Pairs(late));
                yield return Loadout("late-crown", true, Pairs(late));

                yield return HereticSegment();
                yield return MovementSegment();
                yield return NoCooldownSegment();
                yield return DiosSegment();
            }
            finally
            {
                GlobalEventManager.onServerDamageDealt -= OnDamageDealt;
                GlobalEventManager.onCharacterDeathGlobal -= OnDeath;
                On.RoR2.GlobalEventManager.OnHitEnemy -= CountHitEnemy;
                On.RoR2.GlobalEventManager.OnHitAll -= CountHitAll;
                tallying = false;
                try { IOFile.WriteAllText(IOPath.Combine(output, "items.txt"), items.ToString()); } catch (Exception e) { Plugin.Log.LogError(e); }
            }
        }

        private static object[] Pairs(string[] flat)
        {
            var list = new List<object>();
            for (int i = 0; i + 1 < flat.Length; i += 2) { list.Add(flat[i]); list.Add(int.Parse(flat[i + 1])); }
            return list.ToArray();
        }

        // ------------------------------------------------------------ one loadout
        private IEnumerator Loadout(string name, bool crown, params object[] spec)
        {
            yield return Segment("items-" + name);
            var given = Give(spec);
            if (crown && KitRegistration.OpenCircuitDef) pilot.skillLocator.special.SetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
            yield return Wait(0.4f); // stats recalc
            pilot.skillLocator.ResetSkills();
            SpawnPack();
            yield return Wait(0.8f);
            string stats = BodyStats();
            BeginTally();
            Vector3 aim = tank ? tank.corePosition : TankAim();

            // Arc Bolt at the tank; the pack is inside chain range on either side.
            aimTarget = aim; fire1 = true;
            yield return Wait(1.2f); ShotBack("items-" + name + "-bolt");
            yield return Wait(1.3f);
            // Charged spear with the off hand still firing (in the crown for the Open Circuit run).
            if (crown) { yield return Press(4); yield return Wait(1.4f); aimTarget = aim; }
            fire2 = true;
            yield return Wait(crown ? 0.9f : 2.2f); ShotBack("items-" + name + "-charged");
            fire2 = false;
            yield return Wait(0.5f);
            // Dump every remaining spear stock with taps.
            int dumps = pilot.skillLocator.secondary ? pilot.skillLocator.secondary.stock : 0;
            for (int i = 0; i < Mathf.Min(dumps, 6); i++) { yield return Press(2); yield return Wait(0.15f); }
            yield return Wait(0.5f);
            // Arc Step out and back.
            fire1 = false;
            move = Right; yield return Press(3); yield return Wait(0.6f); move = -Right; yield return Press(3); yield return Wait(0.6f); move = Vector3.zero;
            if (!crown)
            {
                // Gaze of the Hollow (the default special) for its full beam.
                aimTarget = aim;
                yield return Press(4); yield return Wait(1.6f);
                fire1 = true; yield return Wait(1.0f); ShotBack("items-" + name + "-gaze");
                if (pilot.skillLocator.special && pilot.skillLocator.special.stock > 0)
                {
                    // Spare special stock (Lysate Cell): a press during the beam must end it, not relaunch.
                    trace.AppendLine(scriptTime.ToString("000.00") + " GAZE_RECAST_WITH_STOCK stock=" + pilot.skillLocator.special.stock);
                    yield return Press(4); yield return Wait(0.6f);
                    trace.AppendLine(scriptTime.ToString("000.00") + " GAZE_AFTER_RECAST stock=" + pilot.skillLocator.special.stock);
                    yield return Wait(2.4f);
                }
                else yield return Wait(3.0f);
                fire1 = false;
            }
            else
            {
                aimTarget = aim; fire1 = true; yield return Wait(2.5f); ShotBack("items-" + name + "-crown"); fire1 = false;
                yield return WaitCrownEnd();
            }
            yield return Wait(1.2f); // let delayed procs (missiles, sticky bombs, bleed) land
            EndTally(name, stats, given);

            if (crown && KitRegistration.OpenCircuitDef) pilot.skillLocator.special.UnsetSkillOverride(this, KitRegistration.OpenCircuitDef, GenericSkill.SkillOverridePriority.Replacement);
            Take(given);
            ClearPack();
            yield return Wait(0.6f);
        }

        // ------------------------------------------------------------ special cases
        private IEnumerator DisplaySegment()
        {
            string[][] batches = {
                new[] { "Syringe", "Hoof", "Feather", "CritGlasses", "Bear", "Mushroom", "Crowbar", "Tooth", "SprintBonus", "StickyBomb", "FlatHealth", "Medkit" },
                new[] { "Missile", "ChainLightning", "Behemoth", "IceRing", "FireRing", "BleedOnHit", "AttackSpeedOnCrit", "HealOnCrit", "Seed", "BounceNearby", "WardOnLevel", "Firework" },
                new[] { "SecondarySkillMagazine", "AlienHead", "UtilitySkillMagazine", "ExtraLife", "Infusion", "Bandolier", "ShockNearby", "Thorns", "NovaOnHeal", "Clover", "Dagger", "Icicle" },
                new[] { "ChainLightningVoid", "MissileVoid", "CritGlassesVoid", "EquipmentMagazineVoid", "ElementalRingVoid", "AttackSpeedAndMoveSpeed", "PrimarySkillShuriken", "StunAndPierce", "IncreasePrimaryDamage", "LunarDagger", "Pearl", "ShinyPearl" },
            };
            for (int b = 0; b < batches.Length; b++)
            {
                yield return Segment("items-display" + b);
                var spec = new List<object>(); foreach (var n in batches[b]) { spec.Add(n); spec.Add(1); }
                var given = Give(spec.ToArray());
                yield return Wait(1.0f);
                Shot("x-display" + b);
                yield return Wait(0.6f);
                aimTarget = TankAim(); fire2 = true; yield return Wait(1.6f); Shot("x-display" + b + "-charge"); fire2 = false;
                yield return Wait(0.8f);
                Take(given);
            }
            // Lingering item effects (a Singularity Band black hole) would bleed into the base numbers.
            yield return Wait(5f);
        }

        // ------------------------------------------------------------ special cases (cont.)
        private IEnumerator HereticSegment()
        {
            yield return Segment("items-heretic");
            // All four would (correctly) transform the Saint into the Heretic; three keep the body.
            var given = Give("LunarPrimaryReplacement", 1, "LunarSecondaryReplacement", 1, "LunarSpecialReplacement", 1);
            yield return Wait(0.5f);
            SpawnPack(); yield return Wait(0.6f);
            BeginTally();
            aimTarget = tank ? tank.corePosition : TankAim();
            fire1 = true; yield return Wait(1.5f); ShotBack("items-heretic-primary");
            fire2 = true; yield return Wait(1.0f); fire2 = false; yield return Wait(0.6f); fire1 = false;
            yield return Press(3); yield return Wait(3.5f); ShotBack("items-heretic-utility");
            ResetToMark(); aimTarget = tank ? tank.corePosition : TankAim();
            yield return Press(4); yield return Wait(1.5f);
            fire1 = true; yield return Wait(1.0f); fire1 = false;
            EndTally("heretic", BodyStats(), given);
            Take(given); ClearPack();
            yield return Wait(1.0f);
            // After the replacements are removed every own skill must work again.
            yield return Segment("items-heretic-after");
            aimTarget = TankAim();
            fire1 = true; yield return Wait(0.8f);
            fire2 = true; yield return Wait(1.0f); ShotBack("items-heretic-after-charge"); fire2 = false; yield return Wait(0.6f); fire1 = false;
            items.AppendLine("HERETIC_AFTER " + SkillNames());
        }

        private IEnumerator MovementSegment()
        {
            yield return Segment("items-move");
            var given = Give("Feather", 3, "Hoof", 12, "SprintBonus", 5, "JumpBoost", 2);
            yield return Wait(0.5f);
            int popsBefore = pops;
            sprint = true; move = facing; yield return Wait(1.0f); ShotBack("items-move-sprint");
            jump = true; yield return Wait(0.1f); jump = false; yield return Wait(0.3f);
            jump = true; yield return Wait(0.1f); jump = false; yield return Wait(0.3f); ShotBack("items-move-doublejump");
            jump = true; yield return Wait(0.1f); jump = false; yield return Wait(0.3f);
            jump = true; yield return Wait(0.1f); jump = false; yield return Wait(1.6f);
            sprint = false; move = Vector3.zero; yield return Wait(0.6f); ShotBack("items-move-land");
            fire2 = true; move = Right; yield return Wait(1.2f); ShotBack("items-move-strafe-charge"); fire2 = false; move = Vector3.zero;
            yield return Wait(0.8f);
            items.AppendLine("MOVE moveSpeed=" + pilot.moveSpeed.ToString("0.0") + " jumps=" + pilot.maxJumpCount + " pops=" + (pops - popsBefore));
            Take(given);
        }

        private IEnumerator NoCooldownSegment()
        {
            yield return Segment("items-nocooldowns");
            var buff = RoR2Content.Buffs.NoCooldowns;
            if (buff) pilot.AddTimedBuff(buff, 9f);
            SpawnPack(); yield return Wait(0.5f);
            BeginTally();
            aimTarget = tank ? tank.corePosition : TankAim();
            fire1 = true;
            for (int i = 0; i < 12; i++) { yield return Press(2); yield return Wait(0.12f); }
            ShotBack("items-nocd-spam");
            for (int i = 0; i < 4; i++) { move = i % 2 == 0 ? Right : -Right; yield return Press(3); yield return Wait(0.3f); }
            move = Vector3.zero; aimTarget = tank ? tank.corePosition : TankAim();
            yield return Press(4); yield return Wait(2.0f);
            yield return Press(4); yield return Wait(0.3f); // recast while the beam runs
            yield return Press(4); yield return Wait(2.5f); ShotBack("items-nocd-gaze");
            fire1 = false; yield return Wait(1.5f);
            EndTally("nocooldowns", BodyStats(), new List<KeyValuePair<ItemIndex, int>>());
            ClearPack();
            if (buff) pilot.ClearTimedBuffs(buff);
            yield return Wait(4.0f);
        }

        private IEnumerator DiosSegment()
        {
            yield return Segment("items-dios");
            var master = pilot.master;
            var given = Give("ExtraLife", 1);
            yield return Wait(0.4f);
            // Die mid-charge with the hand spear out: the worst case for leftover visuals.
            aimTarget = TankAim(); fire2 = true; fire1 = true; yield return Wait(1.0f);
            if (pilot.healthComponent) pilot.healthComponent.godMode = false;
            var old = pilot;
            pilot.healthComponent.Suicide();
            fire1 = fire2 = false;
            yield return Wait(0.5f); ShotBack("items-dios-dead");
            float t = 0f;
            while ((!master.GetBody() || master.GetBody() == old) && t < 8f) { t += 0.1f; yield return Wait(0.1f); }
            var body = master.GetBody();
            if (!body || body == old) { errors++; items.AppendLine("DIOS_FAIL no respawned body"); yield break; }
            pilot = body;
            if (pilot.healthComponent) pilot.healthComponent.godMode = true;
            StartCoroutine(Monitor());
            yield return Wait(2.0f);
            yield return Segment("items-dios-respawned");
            ShotBack("items-dios-respawn-idle");
            aimTarget = TankAim(); fire1 = true; yield return Wait(0.8f);
            fire2 = true; yield return Wait(1.2f); ShotBack("items-dios-respawn-charge"); fire2 = false; yield return Wait(0.6f); fire1 = false;
            move = Right; yield return Press(3); yield return Wait(0.6f); move = Vector3.zero;
            aimTarget = TankAim(); yield return Press(4); yield return Wait(2.6f); ShotBack("items-dios-respawn-gaze"); yield return Wait(3.0f);
            int leftovers = 0;
            foreach (var carry in FindObjectsOfType<FoundationKit.SpearDischarge.SpearCarry>()) if (carry && carry.gameObject != pilot.gameObject) leftovers++;
            items.AppendLine("DIOS respawned=1 strayCarries=" + leftovers + " skills=" + SkillNames());
            Take(given);
        }

        // ------------------------------------------------------------ arena pieces
        private Vector3 TankAim() { return tank ? tank.corePosition : mark + facing * 14f + Vector3.up; }

        private void SpawnTank()
        {
            var prefab = MasterCatalog.FindMasterPrefab("GolemMaster");
            if (!prefab) { items.AppendLine("TANK missing"); return; }
            Vector3 at = Ground(mark + facing * 14f);
            var master = new MasterSummon { masterPrefab = prefab, position = at, rotation = Quaternion.LookRotation(-facing), teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform();
            if (!master) { items.AppendLine("TANK failed"); return; }
            foreach (var ai in master.GetComponents<BaseAI>()) ai.enabled = false;
            master.inventory.GiveItemPermanent(RoR2Content.Items.BoostHp, 400);
            StartCoroutine(HoldTarget(master, at, true));
        }

        private void SpawnPack()
        {
            ClearPack();
            var prefab = MasterCatalog.FindMasterPrefab("LemurianMaster");
            if (!prefab) return;
            Vector3 centre = mark + facing * 14f;
            for (int i = 0; i < 5; i++)
            {
                float side = (i % 2 == 0 ? 1f : -1f) * (3.5f + 1.5f * (i / 2));
                Vector3 at = Ground(centre + Right * side - facing * (i == 4 ? 3f : 0f));
                var master = new MasterSummon { masterPrefab = prefab, position = at, rotation = Quaternion.LookRotation(-facing), teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true }.Perform();
                if (!master) continue;
                foreach (var ai in master.GetComponents<BaseAI>()) ai.enabled = false;
                StartCoroutine(HoldTarget(master, at, false));
            }
        }

        private void ClearPack()
        {
            foreach (var body in pack.ToArray()) if (body && body.healthComponent && body.healthComponent.alive) body.healthComponent.Suicide();
            pack.Clear();
        }

        private Vector3 Ground(Vector3 near)
        {
            RaycastHit hit;
            return Physics.Raycast(near + Vector3.up * 6f, Vector3.down, out hit, 20f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? hit.point : near;
        }

        private IEnumerator HoldTarget(CharacterMaster master, Vector3 spot, bool isTank)
        {
            CharacterBody body = null; float t = 0f;
            while (!body && t < 5f) { body = master ? master.GetBody() : null; t += Time.deltaTime; yield return null; }
            if (!body) yield break;
            if (isTank) tank = body; else pack.Add(body);
            TeleportHelper.TeleportBody(body, spot);
            while (body && body.healthComponent && body.healthComponent.alive && scriptingOrSetup)
            {
                if (body.inputBank) body.inputBank.moveVector = Vector3.zero;
                if ((body.footPosition - spot).sqrMagnitude > 0.25f) TeleportHelper.TeleportBody(body, spot);
                // The tank is a damage sponge: topped up so it never dies and never triggers on-kill items.
                if (isTank && body.healthComponent.combinedHealthFraction < 0.6f) body.healthComponent.Heal(body.healthComponent.fullHealth, default(ProcChainMask), false);
                yield return new WaitForSeconds(0.1f);
            }
        }

        // ------------------------------------------------------------ items
        private List<KeyValuePair<ItemIndex, int>> Give(params object[] spec)
        {
            var given = new List<KeyValuePair<ItemIndex, int>>();
            for (int i = 0; i + 1 < spec.Length; i += 2)
            {
                string name = (string)spec[i]; int count = (int)spec[i + 1];
                var index = ItemCatalog.FindItemIndex(name);
                if (index == ItemIndex.None) { items.AppendLine("ITEM_MISSING " + name); continue; }
                pilot.inventory.GiveItemPermanent(index, count);
                given.Add(new KeyValuePair<ItemIndex, int>(index, count));
            }
            return given;
        }

        // The Saint starts empty, so a loadout ends by clearing everything: void items convert their
        // plain counterparts on pickup, so removing only what was given would leave the converted stacks.
        private void Take(List<KeyValuePair<ItemIndex, int>> given)
        {
            if (!pilot || !pilot.inventory) return;
            foreach (var index in pilot.inventory.itemAcquisitionOrder.ToArray())
            {
                int have = pilot.inventory.GetItemCountPermanent(index);
                if (have > 0) pilot.inventory.RemoveItemPermanent(index, have);
            }
        }

        private string BodyStats()
        {
            var s = pilot.skillLocator;
            return "dmg=" + pilot.damage.ToString("0.0") + " atkspd=" + pilot.attackSpeed.ToString("0.00") + " crit=" + pilot.crit.ToString("0") +
                " stocks=" + (s.primary ? s.primary.maxStock : 0) + "/" + (s.secondary ? s.secondary.maxStock : 0) + "/" + (s.utility ? s.utility.maxStock : 0) + "/" + (s.special ? s.special.maxStock : 0) +
                " cdScale=" + (s.secondary ? s.secondary.cooldownScale.ToString("0.00") : "-");
        }

        private string SkillNames()
        {
            var s = pilot.skillLocator;
            Func<GenericSkill, string> n = g => g && g.skillDef ? g.skillDef.skillName : "-";
            return n(s.primary) + "/" + n(s.secondary) + "/" + n(s.utility) + "/" + n(s.special);
        }

        // ------------------------------------------------------------ measurement
        private void BeginTally()
        {
            tally.Clear(); maxHitDepth = 0; onHitCalls = 0; kills = 0; slowFrames = 0; worstFrame = 0f; statsErrors = errors;
            tallyStart = scriptTime; tallying = true;
            if (!tank || !tank.healthComponent || !tank.healthComponent.alive) { items.AppendLine("  TANK_RESPAWN (died last loadout)"); SpawnTank(); }
            int alive = 0; var where = new StringBuilder();
            foreach (var b in pack) if (b && b.healthComponent && b.healthComponent.alive) { alive++; where.Append(" " + (b.corePosition - mark).magnitude.ToString("0") + "m/" + b.teamComponent.teamIndex); }
            int totalItems = 0;
            if (pilot && pilot.inventory) foreach (var idx in pilot.inventory.itemAcquisitionOrder) totalItems += pilot.inventory.GetItemCountPermanent(idx);
            var monsters = TeamComponent.GetTeamMembers(TeamIndex.Monster);
            items.AppendLine("  ARENA packAlive=" + alive + where + " tank=" + (tank && tank.healthComponent && tank.healthComponent.alive ? (tank.corePosition - mark).magnitude.ToString("0") + "m" : "dead") +
                " monsters=" + monsters.Count + " saintItems=" + totalItems + " saintAt=" + (pilot.footPosition - mark).magnitude.ToString("0.0") + "m");
        }

        private void EndTally(string name, string stats, List<KeyValuePair<ItemIndex, int>> given)
        {
            tallying = false;
            float seconds = Mathf.Max(0.1f, scriptTime - tallyStart);
            float total = tally.Values.Sum(v => v.damage);
            float skill = tally.Where(kv => kv.Key.StartsWith("skill:", StringComparison.Ordinal)).Sum(kv => kv.Value.damage);
            float baseDamage = pilot ? Mathf.Max(0.01f, pilot.damage) : 1f;
            items.AppendLine("LOADOUT " + name + " items=" + string.Join(",", given.Select(p => ItemCatalog.GetItemDef(p.Key).name + "x" + p.Value)));
            items.AppendLine("  " + stats);
            items.AppendLine("  total=" + total.ToString("0") + " (" + (total / baseDamage * 100f / seconds).ToString("0") + "%/s over " + seconds.ToString("0.0") + "s) skillShare=" + (total > 0 ? skill / total : 0).ToString("0.00") +
                " kills=" + kills + " onHitCalls=" + onHitCalls + " maxProcDepth=" + maxHitDepth + " slowFrames=" + slowFrames + " worstFrameMs=" + (worstFrame * 1000f).ToString("0") + " newErrors=" + (errors - statsErrors));
            foreach (var kv in tally.OrderByDescending(kv => kv.Value.damage))
                items.AppendLine("    " + kv.Key.PadRight(44) + " dmg=" + kv.Value.damage.ToString("0").PadLeft(7) + " (" + (kv.Value.damage / Mathf.Max(1f, total) * 100f).ToString("0").PadLeft(3) + "%) hits=" + kv.Value.hits + " onHitEnemy=" + kv.Value.onHit + " onHitAll=" + kv.Value.onHitAll);
            trace.AppendLine(scriptTime.ToString("000.00") + " ITEMS " + name + " total=" + total.ToString("0") + " depth=" + maxHitDepth + " slow=" + slowFrames);
        }

        private string Bucket(DamageInfo info)
        {
            if (info == null) return "?";
            switch (info.damageType.damageSource)
            {
                case DamageSource.Primary: return "skill:primary";
                case DamageSource.Secondary: return "skill:secondary";
                case DamageSource.Utility: return "skill:utility";
                case DamageSource.Special: return "skill:special";
            }
            string inflictor = info.inflictor ? info.inflictor.name.Replace("(Clone)", "") : "none";
            if (pilot && info.inflictor == pilot.gameObject) inflictor = "saint";
            return "other:" + inflictor + "/" + info.damageColorIndex;
        }

        private Tally Get(string key)
        {
            Tally t; if (!tally.TryGetValue(key, out t)) { t = new Tally(); tally[key] = t; }
            return t;
        }

        private bool Mine(GameObject attacker) { return pilot && attacker == pilot.gameObject; }

        private void OnDamageDealt(DamageReport report)
        {
            if (!tallying || report == null || report.damageInfo == null || !Mine(report.damageInfo.attacker)) return;
            var t = Get(Bucket(report.damageInfo)); t.damage += report.damageDealt; t.hits++;
        }

        private void OnDeath(DamageReport report)
        {
            if (tallying && report != null && report.attackerBody == pilot) kills++;
        }

        private void CountHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo info, GameObject victim)
        {
            bool mine = tallying && info != null && Mine(info.attacker);
            if (mine) { onHitCalls++; hitDepth++; if (hitDepth > maxHitDepth) maxHitDepth = hitDepth; Get(Bucket(info)).onHit++; }
            try { orig(self, info, victim); }
            finally { if (mine) hitDepth--; }
        }

        private void CountHitAll(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo info, GameObject hitObject)
        {
            if (tallying && info != null && Mine(info.attacker)) Get(Bucket(info)).onHitAll++;
            orig(self, info, hitObject);
        }

        private IEnumerator FrameWatch()
        {
            while (scripting)
            {
                yield return null;
                if (!tallying || Time.timeScale <= 0f) continue;
                if (Time.realtimeSinceStartup - lastShotReal < 0.35f) continue; // screenshot stalls are ours, not the game's
                float dt = Time.unscaledDeltaTime;
                if (dt > worstFrame) worstFrame = dt;
                if (dt > 0.05f) slowFrames++;
            }
        }

        private void ShotBack(string name)
        {
            lastShotReal = Time.realtimeSinceStartup;
            StartCoroutine(ShotBackRoutine(name));
        }

        private IEnumerator ShotBackRoutine(string name)
        {
            yield return new WaitForEndOfFrame();
            trace.AppendLine(scriptTime.ToString("000.00") + " SHOT " + name);
            if (!shotCamera || !pilot) yield break;
            lastShotReal = Time.realtimeSinceStartup;
            Vector3 chest = pilot.corePosition;
            // Roughly the gameplay camera: behind and above the Saint, looking along the aim.
            Vector3 aim = pilot.inputBank ? Vector3.ProjectOnPlane(pilot.inputBank.aimDirection, Vector3.up).normalized : facing;
            if (aim.sqrMagnitude < 0.01f) aim = facing;
            Render(name, chest - aim * 5.5f + Vector3.up * 1.4f, chest + aim * 6f);
            lastShotReal = Time.realtimeSinceStartup;
        }
    }
}
