using System.Collections.Generic;
using HollowSaint.FoundationKit.SpearDischarge;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.Storm
{
    /// <summary>
    /// Server half of the Answered Prayer passive (docs/storm-passive.md).
    ///
    ///   Static      Hollow Saint hits build Static (0..1) on enemies. The value lives in a
    ///               per-victim dictionary on the server; clients see it as the stack count
    ///               (tier 0..4) of the hidden buff bdHsStatic.
    ///   Electrocute At 100% Static the enemy is stunned (or Shocked if it cannot be
    ///               stunned), pops a capped arc burst that spreads Static to neighbours,
    ///               and adds one charge to the Saint that caused it.
    ///   Thunderbolt Spent by ThunderboltDriver when the charge is full.
    ///
    /// Storm's own damage (pop, Thunderbolt) is dealt inside a "storm damage" scope so the
    /// damage hook ignores it; cascades happen through explicit AddStatic calls instead.
    /// </summary>
    public static class StormServer
    {
        public static BuffDef StaticBuff { get; private set; }
        public static BuffDef ShockedBuff { get; private set; }
        public static BuffDef ElectrocutedBuff { get; private set; }

        private sealed class StaticState
        {
            public HealthComponent health;
            public CharacterBody body;
            public CharacterBody attacker;
            public float value;
            public float lastHit;
            public float immuneUntil;
            public int tier;
        }

        private static readonly Dictionary<HealthComponent, StaticState> states = new Dictionary<HealthComponent, StaticState>();
        private static readonly List<HealthComponent> removeScratch = new List<HealthComponent>();
        private static readonly List<StaticState> tickScratch = new List<StaticState>();
        private static readonly Dictionary<CharacterBody, Queue<float>> electrocuteTimes = new Dictionary<CharacterBody, Queue<float>>();

        private static bool hooked;
        private static int stormDamageDepth;
        private static int cascadeDepth;
        private const int MaxCascadeDepth = 3;

        /// <summary>True while Storm itself is dealing damage (pop, Thunderbolt, splash).</summary>
        public static bool IsDealingStormDamage { get { return stormDamageDepth > 0; } }

        internal static void RegisterBuffs()
        {
            if (StaticBuff != null) return;
            VictimFxTheme.Register();
            RoyalCapacitorFx.Load();
            // Hidden: stack count 0..4 is the Static tier, read by StaticFx on every machine.
            StaticBuff = KitContent.MakeBuff("bdHsStatic", new Color(0.3f, 0.92f, 1f),
                canStack: true, isDebuff: true, hidden: true);
            // Hidden: present while an enemy is stunned or Shocked, so clients keep it crackling.
            ElectrocutedBuff = KitContent.MakeBuff("bdHsElectrocuted", new Color(0.3f, 0.92f, 1f),
                canStack: false, isDebuff: true, hidden: true);
            // Visible debuff: extra damage taken (KitTuning.ShockedDamageMultiplier).
            ShockedBuff = KitContent.MakeBuff("bdHsShocked", new Color(0.6f, 0.95f, 1f),
                canStack: false, isDebuff: true, hidden: false, icon: "buff_shocked");
        }

        internal static void Install()
        {
            if (hooked) return;
            GlobalEventManager.onServerDamageDealt += OnServerDamageDealt;
            On.RoR2.HealthComponent.TakeDamageProcess += AmplifyShockedDamage;
            StormTelemetry.Install();
            hooked = true;
        }

        internal static void Uninstall()
        {
            if (!hooked) return;
            GlobalEventManager.onServerDamageDealt -= OnServerDamageDealt;
            On.RoR2.HealthComponent.TakeDamageProcess -= AmplifyShockedDamage;
            StormTelemetry.Uninstall();
            hooked = false;
        }

        /// <summary>Shocked: ALL damage the enemy takes, from any source, is multiplied. Applied in a
        /// hook on HealthComponent.TakeDamageProcess, BEFORE the game subtracts health
        /// (GlobalEventManager.onServerDamageDealt only fires after health is already reduced).</summary>
        private static void AmplifyShockedDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig,
            HealthComponent self, DamageInfo damageInfo)
        {
            try
            {
                if (damageInfo != null && self != null && ShockedBuff != null)
                {
                    var victimBody = self.body;
                    if (victimBody != null && victimBody.HasBuff(ShockedBuff))
                        damageInfo.damage *= KitTuning.ShockedDamageMultiplier;
                }
            }
            catch (System.Exception error)
            {
                Plugin.Log.LogError("Shocked damage hook error (damage passed through unchanged): " + error);
            }
            orig(self, damageInfo);
        }

        // ------------------------------------------------------------------ Static build-up

        private static void OnServerDamageDealt(DamageReport report)
        {
            try
            {
                if (!NetworkServer.active || stormDamageDepth > 0 || report == null || report.damageInfo == null) return;
                var attacker = report.attackerBody;
                if (!KitUtil.IsHollowSaint(attacker)) return;
                var victim = report.victim;
                var victimBody = report.victimBody;
                if (victim == null || victimBody == null) return;
                if (!victim.alive)
                {
                    // v0.9.16: the killing blow used to be ignored, so Static on anything that died
                    // fast (most packs) was lost and the storm only fed on tanky targets.
                    TryDeathDischarge(victim, victimBody, attacker);
                    return;
                }
                if (report.damageDealt <= 0f) return;
                if (attacker.teamComponent == null || victimBody.teamComponent == null) return;
                if (attacker.teamComponent.teamIndex == victimBody.teamComponent.teamIndex) return;

                var info = report.damageInfo;
                float proc = info.procCoefficient;
                if (proc <= 0f)
                {
                    // Open Circuit pulses carry proc 0 (they do not trigger items) but still build Static.
                    if (KitUtil.SourceOf(info) == HsDamageSource.OpenCircuit) proc = KitTuning.StaticOpenCircuitWeight;
                    else return;
                }

                float full = victim.fullCombinedHealth * Mathf.Max(0.01f, KitTuning.StaticThreshold);
                float gain = Mathf.Clamp(report.damageDealt / Mathf.Max(1f, full), KitTuning.StaticMinGain, 1f) * proc;
                if (info.crit) gain *= KitTuning.StaticCritMultiplier;

                StormTelemetry.RecordHit();
                AddStatic(victim, victimBody, attacker, gain, true);
            }
            catch (System.Exception error)
            {
                Plugin.Log.LogError("HOLLOW_SAINT_STATIC_HOOK_ERROR " + error);
            }
        }

        /// <summary>Server only. Adds Static to a victim, electrocuting it at 100%.</summary>
        internal static void AddStatic(HealthComponent victim, CharacterBody victimBody, CharacterBody attacker, float amount, bool directHit)
        {
            if (!NetworkServer.active || victim == null || victimBody == null || !victim.alive || amount <= 0f) return;
            float now = Time.time;
            StaticState s = GetState(victim, victimBody);
            if (now < s.immuneUntil) return;
            s.attacker = attacker;
            VictimFxTheme.Remember(victimBody, attacker);
            s.value = Mathf.Min(1f, s.value + amount);
            s.lastHit = now;
            if (s.value >= 1f) TryElectrocute(s, true);
            else SetTier(s);
        }

        /// <summary>v0.9.16 (playtest: Thunderbolts too rare; harness storm-a: 52 s of packs gave 4
        /// Electrocutes, 0 Thunderbolts): an enemy that dies holding at least
        /// KitTuning.DeathDischargeStatic of Static discharges. It counts as an Electrocute for the storm
        /// (one orb) and arcs to its neighbours, spreading Static, but a fresh enemy one-shot still gives
        /// nothing, so building Static is what pays. Respects the Electrocute rate cap and cascade depth.</summary>
        private static void TryDeathDischarge(HealthComponent victim, CharacterBody victimBody, CharacterBody attacker)
        {
            if (KitTuning.DeathDischargeStatic <= 0f || KitTuning.DeathDischargeStatic > 1f) return;
            StaticState s;
            if (!states.TryGetValue(victim, out s)) return;
            float now = Time.time;
            if (s.value < KitTuning.DeathDischargeStatic || now < s.immuneUntil) return;
            if (cascadeDepth >= MaxCascadeDepth || !UnderRateCap(attacker, now)) return;
            s.value = 0f;
            s.immuneUntil = now + KitTuning.ElectrocuteImmuneSeconds;
            Queue<float> q;
            if (electrocuteTimes.TryGetValue(attacker, out q)) q.Enqueue(now);
            Vector3 center = victimBody.corePosition;
            KitFx.Server(Beat.Electrocute, center, default(Vector3), Mathf.Clamp(victimBody.radius, 0.3f, 3f), sound: true, owner: attacker);
            KitLog.Event("DEATH_DISCHARGE", "victim=" + victimBody.name);
            bool addedCharge = false;
            var meter = attacker.GetComponent<DischargeMeter>();
            if (meter != null)
            {
                int before = meter.Charge;
                meter.AddCharge();
                addedCharge = meter.Charge > before;
            }
            StormTelemetry.RecordElectrocute(true, addedCharge);
            Pop(attacker, victim, center);
        }

        private static StaticState GetState(HealthComponent victim, CharacterBody victimBody)
        {
            StaticState s;
            if (!states.TryGetValue(victim, out s))
            {
                s = new StaticState { health = victim, body = victimBody };
                states[victim] = s;
            }
            return s;
        }

        private static void SetTier(StaticState s)
        {
            int tier = s.value <= 0.005f ? 0 : Mathf.Clamp(Mathf.CeilToInt(s.value * 4f - 0.0001f), 1, 4);
            if (tier == s.tier) return;
            s.tier = tier;
            if (s.body != null && StaticBuff != null) s.body.SetBuffCount(StaticBuff.buffIndex, tier);
        }

        /// <summary>Server tick (Plugin.FixedUpdate): decay, cleanup, retry of capped Electrocutes.</summary>
        internal static void Tick(float dt)
        {
            VictimFxTheme.Tick(dt);
            StormTelemetry.Tick();
            if (!NetworkServer.active)
            {
                if (states.Count > 0) states.Clear();
                if (electrocuteTimes.Count > 0) electrocuteTimes.Clear();
                return;
            }
            float now = Time.time;
            removeScratch.Clear();
            // Snapshot: an Electrocute in this loop can add states (cascade), which would
            // invalidate a live enumerator.
            tickScratch.Clear();
            foreach (var pair in states) tickScratch.Add(pair.Value);
            for (int n = 0; n < tickScratch.Count; n++)
            {
                var s = tickScratch[n];
                var key0 = s.health;
                if (s.health == null || s.body == null || !s.health.alive) { removeScratch.Add(key0); continue; }
                if (s.value >= 1f && now >= s.immuneUntil)
                {
                    // Held at 100% by the Electrocute cap: try again.
                    if (s.attacker != null) TryElectrocute(s, false);
                    if (s.value >= 1f) continue;
                }
                if (s.value > 0f && now - s.lastHit >= KitTuning.StaticDecayDelay)
                {
                    s.value -= KitTuning.StaticDecayPerSecond * dt;
                    if (s.value <= 0f) { s.value = 0f; SetTier(s); if (now >= s.immuneUntil) removeScratch.Add(key0); }
                    else SetTier(s);
                }
                else if (s.value <= 0f && now >= s.immuneUntil) removeScratch.Add(key0);
            }
            for (int i = 0; i < removeScratch.Count; i++)
            {
                StaticState s;
                var key = removeScratch[i];
                if (states.TryGetValue(key, out s))
                {
                    if (s.body != null && s.tier != 0 && StaticBuff != null && key != null && key.alive)
                        s.body.SetBuffCount(StaticBuff.buffIndex, 0);
                    states.Remove(key);
                }
            }
            removeScratch.Clear();
            tickScratch.Clear();
            if (electrocuteTimes.Count > 16)
            {
                var dead = new List<CharacterBody>();
                foreach (var pair in electrocuteTimes) if (pair.Key == null) dead.Add(pair.Key);
                for (int i = 0; i < dead.Count; i++) electrocuteTimes.Remove(dead[i]);
            }
        }

        // ------------------------------------------------------------------ Electrocute

        private static bool UnderRateCap(CharacterBody attacker, float now)
        {
            Queue<float> q;
            if (!electrocuteTimes.TryGetValue(attacker, out q))
            {
                q = new Queue<float>();
                electrocuteTimes[attacker] = q;
            }
            while (q.Count > 0 && now - q.Peek() > 1f) q.Dequeue();
            return q.Count < Mathf.Max(1, KitTuning.ElectrocutesPerSecondCap);
        }

        private static void TryElectrocute(StaticState s, bool fromHit)
        {
            var attacker = s.attacker;
            float now = Time.time;
            if (attacker == null || cascadeDepth >= MaxCascadeDepth || !UnderRateCap(attacker, now))
            {
                // Held at 100% (tier 4); the tick retries when the window clears.
                s.value = 1f;
                SetTier(s);
                return;
            }
            Electrocute(s, attacker, true, true);
        }

        /// <summary>Resets Static, applies the stun or Shocked, starts immunity. When
        /// popAndCharge is set it also pops arcs at neighbours and charges the Saint.</summary>
        private static void Electrocute(StaticState s, CharacterBody attacker, bool pop, bool awardCharge)
        {
            float now = Time.time;
            var victim = s.health;
            var body = s.body;
            s.value = 0f;
            s.lastHit = now;
            s.immuneUntil = now + KitTuning.ElectrocuteImmuneSeconds;
            SetTier(s);
            if (attacker != null && pop)
            {
                Queue<float> q;
                if (electrocuteTimes.TryGetValue(attacker, out q)) q.Enqueue(now);
            }

            ApplyStunOrShock(body);
            Vector3 center = body.corePosition;
            KitFx.Server(Beat.Electrocute, center, default(Vector3), Mathf.Clamp(body.radius, 0.3f, 3f), sound: true, owner: attacker);
            KitLog.Event("ELECTROCUTE", "victim=" + body.name + " pop=" + pop + " charge=" + awardCharge);

            bool addedCharge = false;
            if (awardCharge && attacker != null)
            {
                var meter = attacker.GetComponent<DischargeMeter>();
                if (meter != null)
                {
                    int before = meter.Charge;
                    meter.AddCharge();
                    addedCharge = meter.Charge > before;
                }
            }
            StormTelemetry.RecordElectrocute(awardCharge, addedCharge);
            if (pop && attacker != null) Pop(attacker, victim, center);
        }

        /// <summary>Server only. The Thunderbolt's Electrocute: stun or Shocked, Static reset and
        /// immunity, no arc burst and no charge (the strike has its own splash).</summary>
        internal static void ElectrocuteFromStrike(HealthComponent victim, CharacterBody victimBody, CharacterBody attacker)
        {
            if (!NetworkServer.active || victim == null || victimBody == null || !victim.alive) return;
            var s = GetState(victim, victimBody);
            s.attacker = attacker;
            VictimFxTheme.Remember(victimBody, attacker);
            Electrocute(s, attacker, false, false);
        }

        private static void ApplyStunOrShock(CharacterBody body)
        {
            var hurt = body.GetComponent<SetStateOnHurt>();
            // v0.9.13 (Stu: too much CC early, stacked with Stun Grenade): a short jolt that interrupts,
            // instead of a long stun, and Shocked on everyone (it was bosses only).
            if (hurt != null && hurt.canBeStunned && !body.isBoss && KitTuning.ElectrocuteStunSeconds > 0f)
                hurt.SetStun(KitTuning.ElectrocuteStunSeconds);
            if (ShockedBuff != null) body.AddTimedBuff(ShockedBuff, KitTuning.ShockedSeconds);
            if (ElectrocutedBuff != null) body.AddTimedBuff(ElectrocutedBuff, Mathf.Max(0.6f, KitTuning.ElectrocuteStunSeconds));
        }

        private static void Pop(CharacterBody attacker, HealthComponent origin, Vector3 center)
        {
            var targets = FindEnemies(attacker, center, KitTuning.ElectrocutePopRadius, KitTuning.ElectrocutePopTargets, origin);
            if (targets.Count == 0) return;
            float damage = KitTuning.ElectrocutePopDamageCoefficient * attacker.damage;
            Vector3 from = center;
            var hit = new List<HurtBox>();
            BeginStormDamage();
            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var box = targets[i];
                    var health = box.healthComponent;
                    if (health == null || !health.alive) continue;
                    var info = MakeInfo(attacker, box, damage, false, KitTuning.ElectrocutePopProc);
                    health.TakeDamage(info);
                    KitUtil.ReportHit(info, health.gameObject);
                    hit.Add(box);
                    KitFx.Server(Beat.ElectrocuteArc, info.position, from, 1f, sound: false, delay: hit.Count * 0.04f, owner: attacker);
                }
            }
            finally { EndStormDamage(); }

            // Cascade: neighbours gain Static (and may Electrocute in turn, up to MaxCascadeDepth deep).
            cascadeDepth++;
            try
            {
                for (int i = 0; i < hit.Count; i++)
                {
                    var health = hit[i].healthComponent;
                    if (health == null || !health.alive || health.body == null) continue;
                    AddStatic(health, health.body, attacker, KitTuning.ElectrocutePopStatic, false);
                }
            }
            finally { cascadeDepth--; }
        }

        // ------------------------------------------------------------------ Shared helpers

        internal static void BeginStormDamage() { stormDamageDepth++; }
        internal static void EndStormDamage() { if (stormDamageDepth > 0) stormDamageDepth--; }

        internal static DamageInfo MakeInfo(CharacterBody attacker, HurtBox box, float damage, bool crit, float proc)
        {
            return new DamageInfo
            {
                damage = damage,
                crit = crit,
                attacker = attacker.gameObject,
                inflictor = attacker.gameObject,
                position = box.transform.position,
                force = Vector3.zero,
                procCoefficient = proc,
                damageColorIndex = DamageColorIndex.Electrocution,
                damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.NoneSpecified),
                inflictedHurtbox = box
            };
        }

        /// <summary>Distinct living enemies of the attacker within radius, nearest first,
        /// excluding one victim (the source of the burst) and the attacker.</summary>
        internal static List<HurtBox> FindEnemies(CharacterBody attacker, Vector3 origin, float radius, int max, HealthComponent exclude)
        {
            var results = new List<HurtBox>();
            var team = attacker.teamComponent != null ? attacker.teamComponent.teamIndex : TeamIndex.None;
            var search = new BullseyeSearch
            {
                searchOrigin = origin,
                searchDirection = Vector3.up,
                minAngleFilter = 0f,
                maxAngleFilter = 180f,
                minDistanceFilter = 0f,
                maxDistanceFilter = radius,
                teamMaskFilter = TeamMask.GetEnemyTeams(team),
                filterByLoS = false,
                filterByDistinctEntity = true,
                sortMode = BullseyeSearch.SortMode.Distance
            };
            search.RefreshCandidates();
            search.FilterOutGameObject(attacker.gameObject);
            if (exclude != null) search.FilterOutGameObject(exclude.gameObject);
            foreach (var box in search.GetResults())
            {
                if (results.Count >= max) break;
                var health = box != null ? box.healthComponent : null;
                if (health == null || !health.alive) continue;
                results.Add(box);
            }
            return results;
        }
    }
}
