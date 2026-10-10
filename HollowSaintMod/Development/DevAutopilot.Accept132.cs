using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EntityStates;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ArcBolt;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Gaze;
using HollowSaint.FoundationKit.Stormspear;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace HollowSaint
{
    /// <summary>
    /// HS_SEGMENTS=accept-132: native in-game acceptance for the 1.3.2 build. Arc Bolt keeps
    /// firing through the Gaze charge-up and the Thundercloud gather, bolts pause during a Stormspear
    /// charge and resume after the throw, and a plain Primary hold still fires at its normal cadence.
    /// Stationary fixtures and an invulnerable pilot; not survival or multiplayer QA.
    /// Arc Bolt shots are counted by watching new Arc Bolt projectile objects owned by the pilot
    /// (KitLog only prints the first few occurrences of an event).
    /// </summary>
    internal sealed partial class DevAutopilot
    {
        private sealed class A132Hit { internal float t; internal DamageSource source; internal float damage; }

        private readonly List<float> a132Bolts = new List<float>();
        private readonly List<float> a132Casts = new List<float>();
        private readonly List<float> a132Throws = new List<float>();
        private readonly List<KeyValuePair<float, string>> a132SpearStates = new List<KeyValuePair<float, string>>();
        private readonly List<A132Hit> a132Hits = new List<A132Hit>();
        private readonly HashSet<int> a132Seen = new HashSet<int>();
        private readonly List<string> a132LogErrors = new List<string>();
        private bool a132Watching;
        private float a132T0;

        // ------------------------------------------------------------ observers
        private void RecordAccept132Hit(DamageReport report)
        {
            if (report.attackerBody != pilot) return;
            a132Hits.Add(new A132Hit { t = Time.time, source = report.damageInfo.damageType.damageSource, damage = report.damageDealt });
        }

        private void A132Log(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            if (!scripting) return;
            a132LogErrors.Add(message);
        }

        private IEnumerator A132Watcher()
        {
            string boltName = ArcBoltProjectile.Prefab ? ArcBoltProjectile.Prefab.name : "HollowSaintArcBoltProjectile";
            string spearName = StormspearProjectile.Prefab ? StormspearProjectile.Prefab.name : "HollowSaintStormspearProjectile";
            var weapon = EntityStateMachine.FindByCustomName(pilot.gameObject, "Weapon");
            var spear = EntityStateMachine.FindByCustomName(pilot.gameObject, StormspearRegistration.MachineName);
            EntityState lastWeapon = null, lastSpear = null;
            trace.AppendLine("A132 watcher bolt=" + boltName + " spear=" + spearName + " weapon=" + (weapon != null) + " spearMachine=" + (spear != null));
            while (a132Watching && pilot)
            {
                float now = Time.time;
                foreach (var projectile in FindObjectsOfType<ProjectileController>())
                {
                    if (!projectile || projectile.owner != pilot.gameObject) continue;
                    if (!a132Seen.Add(projectile.gameObject.GetInstanceID())) continue;
                    string n = projectile.gameObject.name;
                    if (n.StartsWith(boltName, StringComparison.Ordinal)) a132Bolts.Add(now);
                    else if (n.StartsWith(spearName, StringComparison.Ordinal)) a132Throws.Add(now);
                }
                if (weapon && weapon.state != lastWeapon)
                {
                    lastWeapon = weapon.state;
                    if (lastWeapon is ArcBoltState) a132Casts.Add(now);
                }
                if (spear && spear.state != lastSpear)
                {
                    lastSpear = spear.state;
                    a132SpearStates.Add(new KeyValuePair<float, string>(now, lastSpear != null ? lastSpear.GetType().Name : "-"));
                }
                // Physics-step sampling reduces misses, but a projectile born and destroyed
                // between samples is still invisible here. Damage observations supplement this trace.
                yield return new WaitForFixedUpdate();
            }
        }

        private void A132Reset()
        {
            a132Bolts.Clear(); a132Casts.Clear(); a132Throws.Clear(); a132SpearStates.Clear(); a132Hits.Clear();
            a132T0 = Time.time;
        }

        private string A132Rel(float t) { return (t - a132T0).ToString("0.00"); }
        private string A132Times(IEnumerable<float> times) { return string.Join(",", times.Select(t => A132Rel(t)).ToArray()); }
        private static string A132Name(EntityStateMachine machine) { return machine && machine.state != null ? machine.state.GetType().Name : "-"; }
        private int A132Count(List<float> times, float from, float to) { return times.Count(t => t >= from && t <= to); }
        private static bool A132Has(DamageSource source, DamageSource flag) { return (source & flag) == flag; }

        private StoredChargeState A132StoredState()
        {
            foreach (var machine in pilot.GetComponents<EntityStateMachine>())
            {
                var stored = machine.state as StoredChargeState;
                if (stored != null) return stored;
            }
            return null;
        }

        private void A132Trace(string tag)
        {
            var meter = pilot.GetComponent<DischargeMeter>();
            var crown = EntityStateMachine.FindByCustomName(pilot.gameObject, KitRegistration.CrownMachineName);
            var weapon = EntityStateMachine.FindByCustomName(pilot.gameObject, "Weapon");
            var spear = EntityStateMachine.FindByCustomName(pilot.gameObject, StormspearRegistration.MachineName);
            var gaze = crown ? crown.state as GazeState : null;
            var fuel = pilot.GetComponent<GazeFuelController>();
            var stored = A132StoredState();
            trace.AppendLine("A132 " + tag + " t=" + A132Rel(Time.time) + " bank=" + (meter ? meter.Charge : -1) +
                " crown=" + A132Name(crown) + (gaze != null ? " absorbed=" + gaze.ChargeAbsorbed + " opening=" + gaze.OpeningCharges : "") +
                (stored != null ? " stored.released=" + stored.Released : "") +
                " fuelEntry=" + (fuel ? fuel.AvailableEntry : -1) + " weapon=" + A132Name(weapon) + " spear=" + A132Name(spear) +
                " secondaryStock=" + pilot.skillLocator.secondary.stock + " bolts=" + a132Bolts.Count);
        }

        private IEnumerator A132Until(Func<bool> done, float timeout, string tag, float every = .5f)
        {
            float end = Time.time + timeout, next = Time.time;
            while (!done() && Time.time < end)
            {
                if (Time.time >= next) { A132Trace(tag); next = Time.time + every; }
                yield return Wait(.05f);
            }
        }

        /// <summary>Waits (up to timeout) for a fresh Arc Bolt cast to start, then lets it run `into`
        /// seconds, so a screenshot lands in the middle of that cast (the bolt leaves at about 0.1 s).</summary>
        private IEnumerator A132WaitFreshBolt(float timeout, float into)
        {
            int seen = a132Casts.Count;
            float end = Time.time + timeout;
            while (a132Casts.Count == seen && Time.time < end) yield return Wait(.01f);
            ReleaseCheck(a132Casts.Count > seen, "fresh Arc Bolt cast observed before screenshot timeout");
            if (a132Casts.Count == seen) yield break;
            yield return Wait(into);
        }

        /// <summary>Dev stand-in for a bolt that earned a Static Charge (real earning is an Electrocute
        /// chance, so a short window may earn none). Server only, through the real meter.</summary>
        private bool A132SimulateEarn(string tag)
        {
            var meter = pilot.GetComponent<DischargeMeter>();
            int before = meter.Charge;
            bool filled = meter.AddCharge();
            trace.AppendLine("A132 simulated earn " + tag + " bank " + before + "->" + meter.Charge + " filled=" + filled + " incomeLimited=" + meter.LastIncomeLimited);
            return meter.Charge == before + 1;
        }

        // ------------------------------------------------------------ 1. bolts during the Gaze charge-up
        private IEnumerator A132GazeCharge(int bank, bool shots, bool simulateEarn)
        {
            string name = "accept132-gaze-charge-b" + bank + (simulateEarn ? "-earn" : "");
            yield return BalanceTarget(name, KitTuning.ArcBoltDamageCoefficient, 9f);
            if (!balanceVictim) yield break;
            Toughen(balanceVictim);
            var special = pilot.skillLocator.special;
            var gaze = GazeRegistration.SkillDef;
            var meter = pilot.GetComponent<DischargeMeter>();
            var fuel = pilot.GetComponent<GazeFuelController>();
            var crown = EntityStateMachine.FindByCustomName(pilot.gameObject, KitRegistration.CrownMachineName);
            special.SetSkillOverride(this, gaze, GenericSkill.SkillOverridePriority.Replacement);
            special.Reset();
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, bank);
            aimTarget = balanceVictim.corePosition;
            yield return Wait(.6f);
            ReleaseCheck(special.skillDef == gaze && special.CanExecute() && meter.Charge == bank,
                name + " setup: Gaze equipped and ready, bank " + bank + " (bank " + meter.Charge + ")");
            A132Reset();
            int logErrorsBefore = a132LogErrors.Count;
            Clip(segment, true);
            A132Trace("start");
            int before = meter.Charge;
            float pressed = Time.time;
            fire1 = true; fire4 = true;
            yield return Wait(.35f);
            var chargePhase = crown.state as GazeState;
            A132Trace("charging");
            ReleaseCheck(chargePhase != null && chargePhase.ChargeAbsorbed >= 1,
                name + " Gaze charge phase running and absorbing (absorbed " + (chargePhase != null ? chargePhase.ChargeAbsorbed : -1) + ")");
            bool earnedOne = false;
            if (simulateEarn) earnedOne = A132SimulateEarn("gaze charge");
            if (shots)
            {
                yield return A132WaitFreshBolt(.8f, .1f);
                var weapon = EntityStateMachine.FindByCustomName(pilot.gameObject, "Weapon");
                trace.AppendLine("A132 gaze shot t=" + A132Rel(Time.time) + " weapon=" + A132Name(weapon) + " crown=" + A132Name(crown));
                Shot("accept132-gaze-charge-bolt");
            }
            while (Time.time - pressed < 1.40f) yield return Wait(.02f);
            fire1 = false;
            yield return Wait(.08f);
            var preRelease = crown.state as GazeState;
            int absorbed = preRelease != null ? preRelease.ChargeAbsorbed : -1;
            int bankAtRelease = meter.Charge;
            float released = Time.time;
            A132Trace("pre-release");
            fire4 = false;
            // Only the original bank becomes entry fuel. Gather income remains visible
            // as reserve during the beam, so never infer entry fuel from the buff dropping.
            int bankEntry = before, openingSeen = -1;
            float watchEnd = Time.time + 1.2f;
            while (Time.time < watchEnd)
            {
                var s = crown.state as GazeState;
                if (s != null && s.OpeningCharges > openingSeen) openingSeen = s.OpeningCharges;
                yield return null;
            }
            A132Trace("post-ignite");
            int entryAfterOpening = fuel ? fuel.AvailableEntry : -1;
            int reserveDuringBeam = meter.Charge;
            yield return A132Until(() => !(crown.state is GazeState), 14f, "beam");
            yield return Wait(2.5f);
            A132Trace("after");
            int bankAfter = meter.Charge;
            Clip(segment, false);
            int boltsCharge = A132Count(a132Bolts, pressed, released);
            int primaryHits = a132Hits.Count(h => h.t >= pressed && h.t <= released + .4f && A132Has(h.source, DamageSource.Primary));
            var beamHits = a132Hits.Where(h => h.t > released && A132Has(h.source, DamageSource.Special)).ToList();
            int spent = bankEntry - entryAfterOpening;
            float hold = released - pressed;
            int expectedAbsorb = Mathf.Min(bank, 1 + Mathf.FloorToInt(Mathf.Max(0f, hold - 0.12f) / 0.30f));
            int primaryHitsInWindow = a132Hits.Count(h => h.t >= pressed && h.t <= released && A132Has(h.source, DamageSource.Primary));
            trace.AppendLine("ACCEPT132_GAZE bank=" + bank + " simulatedEarn=" + simulateEarn + "/" + earnedOne + " primaryHitsInWindow=" + primaryHitsInWindow +
                " bankBefore=" + before + " hold=" + hold.ToString("0.00") +
                " boltsDuringCharge=" + boltsCharge + " boltTimes=[" + A132Times(a132Bolts) + "] casts=[" + A132Times(a132Casts) + "] primaryHits=" + primaryHits +
                " absorbed=" + absorbed + " expectedAbsorb=" + expectedAbsorb + " bankAtRelease=" + bankAtRelease + " earnedDuringCharge=" + (bankAtRelease - before) +
                " bankEntry=" + bankEntry + " openingSeen=" + openingSeen + " fuelEntryAfterOpening=" + entryAfterOpening + " spent=" + spent + " reserveDuringBeam=" + reserveDuringBeam +
                " beamHits=" + beamHits.Count + " maxBeamHit=" + (beamHits.Count > 0 ? beamHits.Max(h => h.damage) : 0f).ToString("0") +
                " bankAfter=" + bankAfter + " specialStock=" + special.stock + " logErrors=" + (a132LogErrors.Count - logErrorsBefore));
            ReleaseCheck(boltsCharge >= 2, name + " at least 2 Arc Bolts fired during the charge-up (" + boltsCharge + ")");
            ReleaseCheck(absorbed >= 1 && absorbed <= bank && Mathf.Abs(absorbed - expectedAbsorb) <= 1,
                name + " absorbed " + absorbed + " charges (expected about " + expectedAbsorb + ")");
            ReleaseCheck(openingSeen == absorbed, name + " beam phase carries the absorbed count as the opening (" + openingSeen + ")");
            ReleaseCheck(beamHits.Count > 0, name + " beam ignited and damaged the target after release (" + beamHits.Count + " hits)");
            ReleaseCheck(spent == absorbed, name + " opening blast spent exactly the absorbed count (spent " + spent + ", absorbed " + absorbed + ")");
            ReleaseCheck(reserveDuringBeam >= bankAtRelease - bankEntry,
                name + " gather income remains visible in the reserve bank during the beam (" + reserveDuringBeam + ")");
            ReleaseCheck(bankAfter >= 0 && bankAfter >= bankAtRelease - absorbed,
                name + " bank after the beam holds the unspent and earned charges (entry " + bankEntry + ", absorbed " + absorbed + ", after " + bankAfter + ")");
            if (simulateEarn)
                ReleaseCheck(earnedOne && bankAtRelease >= bank + 1 && reserveDuringBeam >= 1 && bankAfter >= 1,
                    name + " a charge earned during the charge-up stays banked throughout the beam (entry " + bankEntry + ", reserve " + reserveDuringBeam + ", after " + bankAfter + ")");
            ReleaseCheck(a132LogErrors.Count == logErrorsBefore, name + " no errors or exceptions logged (" + (a132LogErrors.Count - logErrorsBefore) + ")");
            special.UnsetSkillOverride(this, gaze, GenericSkill.SkillOverridePriority.Replacement);
            Shot("accept132-gaze-charge-b" + bank + "-end");
        }

        // ------------------------------------------------------------ 2. bolts during the Thundercloud gather
        private IEnumerator A132CloudGather()
        {
            string name = "accept132-cloud-gather";
            yield return BalanceTarget(name, KitTuning.ArcBoltDamageCoefficient, 9f);
            if (!balanceVictim) yield break;
            Toughen(balanceVictim);
            var special = pilot.skillLocator.special;
            var meter = pilot.GetComponent<DischargeMeter>();
            special.SetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            special.Reset();
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 3);
            aimTarget = balanceVictim.corePosition;
            yield return Wait(.6f);
            ReleaseCheck(special.skillDef == ChargedStormRegistration.Cloud && special.CanExecute() && meter.Charge == 3,
                name + " setup: Thundercloud equipped and ready, bank 3 (bank " + meter.Charge + ")");
            A132Reset();
            int logErrorsBefore = a132LogErrors.Count;
            Clip(segment, true);
            A132Trace("start");
            int before = meter.Charge;
            float pressed = Time.time;
            fire1 = true; fire4 = true;
            yield return Wait(.4f);
            A132Trace("gathering");
            ReleaseCheck(StoredChargeState.IsGathering(pilot), name + " Thundercloud gather running with Primary held");
            bool earnedOne = A132SimulateEarn("cloud gather");
            yield return A132WaitFreshBolt(.8f, .1f);
            var weapon = EntityStateMachine.FindByCustomName(pilot.gameObject, "Weapon");
            trace.AppendLine("A132 cloud shot t=" + A132Rel(Time.time) + " weapon=" + A132Name(weapon));
            Shot("accept132-cloud-gather-bolt");
            while (Time.time - pressed < 1.10f) yield return Wait(.02f);
            int bankAtRelease = meter.Charge;
            int entry = meter.StoredCastEntry;
            float released = Time.time;
            fire1 = false; fire4 = false;
            A132Trace("released");
            bool sawRelease = false;
            yield return A132Until(() =>
            {
                var s = A132StoredState();
                if (s != null && s.Released) sawRelease = true;
                return sawRelease;
            }, 3f, "await-release", .25f);
            yield return Wait(1.0f);
            int bankSettled = meter.Charge;
            A132Trace("settled");
            yield return A132Until(() => A132StoredState() == null, 16f, "cloud");
            bool dismissed = false;
            if (A132StoredState() != null) { dismissed = true; yield return Press(4); yield return Wait(1f); }
            yield return Wait(.5f);
            A132Trace("after");
            Clip(segment, false);
            float hold = released - pressed;
            int gathered = StoredChargeCastLedger.Gathered(hold, entry, 0.3f);
            int expectedSpent = Mathf.Min(entry, gathered);
            int earned = bankAtRelease - before;
            int spent = bankAtRelease - bankSettled;
            int boltsGather = A132Count(a132Bolts, pressed, released);
            int primaryHits = a132Hits.Count(h => h.t >= pressed && h.t <= released + .4f && A132Has(h.source, DamageSource.Primary));
            int strikes = a132Hits.Count(h => h.t > released && A132Has(h.source, DamageSource.Special));
            int primaryHitsInWindow = a132Hits.Count(h => h.t >= pressed && h.t <= released && A132Has(h.source, DamageSource.Primary));
            trace.AppendLine("ACCEPT132_CLOUD simulatedEarn=" + earnedOne + " primaryHitsInWindow=" + primaryHitsInWindow +
                " bankBefore=" + before + " hold=" + hold.ToString("0.00") + " entry=" + entry +
                " boltsDuringGather=" + boltsGather + " boltTimes=[" + A132Times(a132Bolts) + "] casts=[" + A132Times(a132Casts) + "] primaryHits=" + primaryHits +
                " earned=" + earned + " bankAtRelease=" + bankAtRelease + " gatheredExpected=" + gathered + " spent=" + spent +
                " bankSettled=" + bankSettled + " expectedAfter=" + (before + earned - expectedSpent) +
                " sawRelease=" + sawRelease + " strikes=" + strikes + " dismissedByScript=" + dismissed +
                " specialStock=" + special.stock + " logErrors=" + (a132LogErrors.Count - logErrorsBefore));
            ReleaseCheck(boltsGather >= 1, name + " at least 1 Arc Bolt fired during the gather (" + boltsGather + ")");
            ReleaseCheck(entry == 3, name + " cast entry stays frozen at the bank when the cast began (" + entry + ")");
            ReleaseCheck(sawRelease, name + " the cloud released");
            ReleaseCheck(strikes > 0, name + " the cloud struck (" + strikes + " Special hits)");
            ReleaseCheck(spent == expectedSpent && bankSettled == before + earned - expectedSpent,
                name + " charges spent equal the gathered count (spent " + spent + ", expected " + expectedSpent + "; before " + before + " + earned " + earned + " - spent = " + (before + earned - spent) + ", bank " + bankSettled + ")");
            ReleaseCheck(!dismissed && A132StoredState() == null, name + " cloud ran its course and the state ended");
            ReleaseCheck(a132LogErrors.Count == logErrorsBefore, name + " no errors or exceptions logged (" + (a132LogErrors.Count - logErrorsBefore) + ")");
            special.UnsetSkillOverride(this, ChargedStormRegistration.Cloud, GenericSkill.SkillOverridePriority.Replacement);
            Shot("accept132-cloud-gather-end");
        }

        // ------------------------------------------------------------ 3. spear + bolt rhythm
        private IEnumerator A132SpearRhythm()
        {
            string name = "accept132-spear-rhythm";
            yield return BalanceTarget(name, KitTuning.ArcBoltDamageCoefficient, 12f);
            if (!balanceVictim) yield break;
            Toughen(balanceVictim);
            var secondary = pilot.skillLocator.secondary;
            var spearMachine = EntityStateMachine.FindByCustomName(pilot.gameObject, StormspearRegistration.MachineName);
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 0);
            secondary.Reset();
            aimTarget = balanceVictim.corePosition;
            yield return Wait(.5f);
            ReleaseCheck(secondary.skillDef is StormspearSkillDef && secondary.stock >= 1 && spearMachine != null,
                name + " setup: default Secondary is Stormspear with stock (stock " + secondary.stock + ")");
            A132Reset();
            int logErrorsBefore = a132LogErrors.Count;
            Clip(segment, true);
            A132Trace("start");
            float t0 = Time.time;
            fire1 = true;
            yield return Wait(.6f);
            A132Trace("bolts-only");
            float spearPress = Time.time;
            fire2 = true;
            yield return Wait(1.0f);
            A132Trace("spear-charging");
            Shot("accept132-spear-charge");
            float chargeFor = StormspearTuning.ChargeSeconds + .2f;
            while (Time.time - spearPress < chargeFor) yield return Wait(.02f);
            A132Trace("spear-full");
            float releaseAt = Time.time;
            fire2 = false;
            yield return Wait(1.0f);
            A132Trace("after-throw");
            int stockAfterThrow = secondary.stock;
            int chargeStartsBefore = a132SpearStates.Count(s => s.Value == "StormspearChargeState");
            float secondPress = Time.time;
            fire2 = true;
            // Hold through real recharge: resetting stock here would conceal a held-input regression.
            yield return A132Until(() => spearMachine.state is StormspearChargeState,
                15f, "await-natural-spear-recharge");
            yield return Wait(.4f);
            A132Trace("second-press");
            bool secondCharging = spearMachine.state is StormspearChargeState;
            int chargeStartsAfter = a132SpearStates.Count(s => s.Value == "StormspearChargeState");
            fire2 = false;
            yield return Wait(.6f);
            fire1 = false;
            yield return Wait(1.0f);
            A132Trace("end");
            Clip(segment, false);

            float tc = float.NaN, tThrowState = float.NaN, tProjectile = float.NaN;
            foreach (var state in a132SpearStates)
            {
                if (float.IsNaN(tc) && state.Key >= spearPress - .05f && state.Value == "StormspearChargeState") tc = state.Key;
                else if (!float.IsNaN(tc) && float.IsNaN(tThrowState) && state.Value == "StormspearThrowState") tThrowState = state.Key;
            }
            foreach (float t in a132Throws) if (!float.IsNaN(tc) && t >= tc) { tProjectile = t; break; }
            bool haveThrow = !float.IsNaN(tc) && !float.IsNaN(tProjectile);
            int boltsBefore = haveThrow ? a132Bolts.Count(t => t >= t0 && t < tc) : -1;
            int boltsDuring = haveThrow ? a132Bolts.Count(t => t > tc + .15f && t < tProjectile) : -1;
            float firstAfter = float.NaN;
            if (haveThrow) foreach (float t in a132Bolts) if (t > tProjectile) { firstAfter = t; break; }
            float resumeGap = float.IsNaN(firstAfter) ? float.NaN : firstAfter - tProjectile;
            trace.AppendLine("ACCEPT132_SPEAR chargeSeconds=" + StormspearTuning.ChargeSeconds + " spearPress=" + A132Rel(spearPress) + " releaseAt=" + A132Rel(releaseAt) +
                " chargeStart=" + A132Rel(tc) + " throwState=" + A132Rel(tThrowState) + " throwProjectile=" + A132Rel(tProjectile) +
                " throws=[" + A132Times(a132Throws) + "] boltTimes=[" + A132Times(a132Bolts) + "] casts=[" + A132Times(a132Casts) + "]" +
                " boltsBeforeCharge=" + boltsBefore + " boltsDuringCharge=" + boltsDuring + " firstBoltAfterThrow=" + A132Rel(firstAfter) +
                " resumeGap=" + resumeGap.ToString("0.00") + " stockAfterThrow=" + stockAfterThrow + " secondPress=" + A132Rel(secondPress) +
                " secondCharging=" + secondCharging + " chargeStarts=" + chargeStartsBefore + "->" + chargeStartsAfter +
                " spearStates=[" + string.Join(",", a132SpearStates.Select(s => A132Rel(s.Key) + ":" + s.Value).ToArray()) + "]" +
                " logErrors=" + (a132LogErrors.Count - logErrorsBefore));
            ReleaseCheck(haveThrow, name + " spear charged and was thrown (charge start " + A132Rel(tc) + ", projectile " + A132Rel(tProjectile) + ")");
            ReleaseCheck(boltsBefore >= 1, name + " bolts fired before the spear charge (" + boltsBefore + ")");
            ReleaseCheck(haveThrow && boltsDuring == 0, name + " no bolt fired between charge start + 0.15 s and the throw (" + boltsDuring + ")");
            ReleaseCheck(haveThrow && !float.IsNaN(resumeGap) && resumeGap <= .6f, name + " a bolt fires within 0.6 s after the throw (gap " + resumeGap.ToString("0.00") + ")");
            ReleaseCheck(secondCharging && chargeStartsAfter > chargeStartsBefore, name + " second spear charge starts after natural recharge with both inputs held (stock after throw " + stockAfterThrow + ")");
            ReleaseCheck(a132LogErrors.Count == logErrorsBefore, name + " no errors or exceptions logged (" + (a132LogErrors.Count - logErrorsBefore) + ")");
        }

        // ------------------------------------------------------------ 4. plain Primary regression
        private IEnumerator A132PrimaryRegression()
        {
            string name = "accept132-primary-regression";
            yield return BalanceTarget(name, KitTuning.ArcBoltDamageCoefficient, 9f);
            if (!balanceVictim) yield break;
            Toughen(balanceVictim);
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, 0);
            aimTarget = balanceVictim.corePosition;
            yield return Wait(.6f);
            A132Reset();
            int logErrorsBefore = a132LogErrors.Count;
            Clip(segment, true);
            float start = Time.time;
            fire1 = true;
            yield return Wait(2.0f);
            fire1 = false;
            float end = Time.time;
            yield return Wait(.5f);
            Clip(segment, false);
            var inWindow = a132Bolts.Where(t => t >= start && t <= end).ToList();
            float meanInterval = inWindow.Count > 1 ? (inWindow[inWindow.Count - 1] - inWindow[0]) / (inWindow.Count - 1) : float.NaN;
            trace.AppendLine("ACCEPT132_PRIMARY window=" + (end - start).ToString("0.00") + " bolts=" + inWindow.Count + " boltTimes=[" + A132Times(a132Bolts) + "]" +
                " meanInterval=" + meanInterval.ToString("0.000") + " attackSpeed=" + pilot.attackSpeed + " interval=" + KitTuning.ArcBoltInterval +
                " logErrors=" + (a132LogErrors.Count - logErrorsBefore));
            ReleaseCheck(inWindow.Count >= 3 && inWindow.Count <= 5, name + " plain Primary hold for 2 s fires about 4 bolts (" + inWindow.Count + ")");
            ReleaseCheck(!float.IsNaN(meanInterval) && Mathf.Abs(meanInterval - KitTuning.ArcBoltInterval) < .1f,
                name + " normal cadence, mean interval " + meanInterval.ToString("0.000") + " s (expected " + KitTuning.ArcBoltInterval + ")");
            ReleaseCheck(a132LogErrors.Count == logErrorsBefore, name + " no errors or exceptions logged (" + (a132LogErrors.Count - logErrorsBefore) + ")");
            Shot("accept132-primary-end");
        }

        // ------------------------------------------------------------ 5. pose: Arc Bolt over the crown hold
        // HS_SEGMENTS=accept-132-pose. Shots at fixed points of successive bolts during the Gaze
        // charge-up and the Thundercloud gather, then a baseline of the same charge without bolts
        // shot at the same times, plus a per-frame POSE trace of the layer memo and the arm pin.
        private bool a132PoseTracing;
        private bool a132PoseWindowReady;
        private readonly List<float> a132PoseShotTimes = new List<float>();
        private static readonly float[][] A132PoseBoltShots =
        {
            new[] { 1f, .04f }, // bolt cuts the hold
            new[] { 2f, .12f }, // just after release
            new[] { 3f, .33f }, // arms heading back to the hold
            new[] { 4f, .46f }, // settled, before the next bolt
        };

        private Transform A132Model() { return pilot && pilot.modelLocator ? pilot.modelLocator.modelTransform : null; }

        private static Transform A132Bone(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        private IEnumerator A132PoseTracer(string tag)
        {
            var model = A132Model();
            var weights = model ? model.GetComponent<FoundationLayerWeights>() : null;
            var arms = model ? model.GetComponent<FoundationArmPose>() : null;
            var weapon = EntityStateMachine.FindByCustomName(pilot.gameObject, "Weapon");
            Transform lh = model ? A132Bone(model, "L hand") : null, rh = model ? A132Bone(model, "R hand") : null, chest = model ? A132Bone(model, "chest") : null;
            while (a132PoseTracing && pilot)
            {
                yield return new WaitForEndOfFrame();
                string hands = lh && rh && chest ? " Lh=" + ((lh.position.y - chest.position.y) * 100f).ToString("0") + "cm Rh=" + ((rh.position.y - chest.position.y) * 100f).ToString("0") + "cm" : "";
                trace.AppendLine("POSE " + tag + " t=" + A132Rel(Time.time) + " dt=" + Time.deltaTime.ToString("0.000") + " weapon=" + A132Name(weapon) + hands +
                    " pin=" + (arms ? arms.DebugPinArm + " L" + arms.DebugPinLeft.ToString("0.00") + " R" + arms.DebugPinRight.ToString("0.00") +
                        " sampled=" + arms.DebugPinSampled + " sampleErr=" + arms.DebugSampleError.ToString("0.0") : "-") +
                    " " + (weights ? weights.DebugState() + " lastResume[" + weights.DebugLastResume + "]" : "no-weights"));
            }
        }

        /// <summary>Waits for bolt k (0-based, counted from the press) to start, then `offset` seconds.</summary>
        private IEnumerator A132AtBolt(int k, float offset, float timeout)
        {
            a132PoseWindowReady = false;
            float end = Time.time + timeout;
            while (a132Casts.Count <= k && Time.time < end) yield return null;
            ReleaseCheck(a132Casts.Count > k, "pose bolt " + k + " observed before timeout");
            if (a132Casts.Count <= k) yield break;
            float at = a132Casts[k] + offset;
            while (Time.time < at) yield return null;
            a132PoseWindowReady = Time.time - at <= .12f;
            ReleaseCheck(a132PoseWindowReady, "pose bolt " + k + " capture within 120 ms of requested offset");
        }

        /// <summary>One crown charge (Gaze charge-up, or Thundercloud gather when cloud) with or without
        /// Primary held. With bolts, shots land at A132PoseBoltShots and their times are kept; without,
        /// the same times are reused so each pair compares the same point of the rise.</summary>
        private IEnumerator A132PoseRun(bool cloud, bool bolts)
        {
            string name = "pose-" + (cloud ? "cloud" : "gaze") + "-" + (bolts ? "bolts" : "base");
            yield return BalanceTarget(name, KitTuning.ArcBoltDamageCoefficient, 9f);
            if (!balanceVictim) yield break;
            Toughen(balanceVictim);
            var special = pilot.skillLocator.special;
            var def = cloud ? (RoR2.Skills.SkillDef)ChargedStormRegistration.Cloud : GazeRegistration.SkillDef;
            var crown = EntityStateMachine.FindByCustomName(pilot.gameObject, KitRegistration.CrownMachineName);
            special.SetSkillOverride(this, def, GenericSkill.SkillOverridePriority.Replacement);
            special.Reset();
            pilot.SetBuffCount(DischargeMeter.ChargeBuff.buffIndex, cloud ? 3 : 5);
            aimTarget = balanceVictim.corePosition;
            yield return Wait(.6f);
            if (bolts) a132PoseShotTimes.Clear();
            A132Reset();
            FoundationArmPose.DebugCompare = true;
            a132PoseTracing = true;
            StartCoroutine(A132PoseTracer(name));
            Clip(segment, true);
            float pressed = Time.time;
            fire4 = true; fire1 = bolts;
            for (int i = 0; i < A132PoseBoltShots.Length; i++)
            {
                string shot = name + "-" + i;
                if (bolts)
                {
                    yield return A132AtBolt((int)A132PoseBoltShots[i][0], A132PoseBoltShots[i][1], 2f);
                    if (!a132PoseWindowReady) break;
                    a132PoseShotTimes.Add(Time.time - pressed);
                }
                else
                {
                    if (i >= a132PoseShotTimes.Count) break;
                    while (Time.time - pressed < a132PoseShotTimes[i]) yield return null;
                }
                trace.AppendLine("A132 POSE_SHOT " + shot + " t=" + (Time.time - pressed).ToString("0.000") + " crown=" + A132Name(crown) +
                    " stored=" + (A132StoredState() != null) + " casts=[" + A132Times(a132Casts) + "]");
                Shot(shot);
            }
            ReleaseCheck(a132PoseShotTimes.Count == A132PoseBoltShots.Length,
                name + " captured every requested bolt comparison window");
            yield return Wait(.15f);
            fire1 = false;
            if (cloud)
            {
                fire4 = false;
                yield return Wait(1.0f);
                if (A132StoredState() != null) yield return Press(4);
                yield return A132Until(() => A132StoredState() == null, 16f, name + "-end");
            }
            else
            {
                yield return Press(3); // Utility backs out of the charge-up without the beam
                fire4 = false;
                yield return A132Until(() => !(crown.state is GazeState), 14f, name + "-end");
            }
            yield return Wait(.5f);
            a132PoseTracing = false;
            FoundationArmPose.DebugCompare = false;
            Clip(segment, false);
            special.UnsetSkillOverride(this, def, GenericSkill.SkillOverridePriority.Replacement);
        }

        private IEnumerator Accept132PoseSegments()
        {
            isolateReviewFixtures = true;
            foreach (var dummy in dummies) if (dummy) { dummy.healthComponent.godMode = false; dummy.healthComponent.Suicide(); } dummies.Clear();
            StartCoroutine(CameraFollow()); SetCameraDistance(8.5f, .5f);
            a132Watching = true;
            StartCoroutine(A132Watcher());
            try
            {
                yield return A132PoseRun(false, true);
                yield return A132PoseRun(false, false);
                yield return A132PoseRun(true, true);
                yield return A132PoseRun(true, false);
                a132PoseTracing = true;
                StartCoroutine(A132PoseTracer("primary"));
                yield return A132PrimaryRegression();
                a132PoseTracing = false;
            }
            finally
            {
                a132Watching = false;
                a132PoseTracing = false;
                FoundationArmPose.DebugCompare = false;
                fire1 = fire2 = fire3 = fire4 = false;
            }
        }

        private IEnumerator Accept132Segments()
        {
            isolateReviewFixtures = true;
            foreach (var dummy in dummies) if (dummy) { dummy.healthComponent.godMode = false; dummy.healthComponent.Suicide(); } dummies.Clear();
            StartCoroutine(CameraFollow()); SetCameraDistance(8.5f, .5f);
            float oldCrit = pilot.baseCrit;
            GlobalEventManager.onServerDamageDealt += RecordAccept132Hit;
            Application.logMessageReceived += A132Log;
            a132Watching = true;
            StartCoroutine(A132Watcher());
            try
            {
                ReleaseCheck(Mathf.Abs(KitTuning.ArcBoltInterval - 0.5f) < .001f, "Arc Bolt interval 0.5 s");
                yield return A132GazeCharge(5, true, false);
                yield return A132GazeCharge(2, false, true);
                yield return A132CloudGather();
                yield return A132SpearRhythm();
                yield return A132PrimaryRegression();
            }
            finally
            {
                a132Watching = false;
                GlobalEventManager.onServerDamageDealt -= RecordAccept132Hit;
                Application.logMessageReceived -= A132Log;
                fire1 = fire2 = fire3 = fire4 = false;
                pilot.baseCrit = oldCrit; pilot.RecalculateStats();
            }
        }
    }
}
