using System.Collections.Generic;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.Storm;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    /// <summary>Server-only Closed Circuit: Electrocutes near the open crown refund the
    /// charges fed into it; charges still owed when it closes burst out as a crown nova. Nothing here runs on clients; effects are networked.</summary>
    [DisallowMultipleComponent]
    public sealed class ClosedCircuitDriver : MonoBehaviour
    {
        private CharacterBody body;
        private Stage stage;
        private readonly ClosedCircuitLedger ledger = new ClosedCircuitLedger();
        private bool wasOpen;

        internal static void Install() { OpenCircuitBuff.Opened += OnOpened; }

        private static void OnOpened(CharacterBody body, int fed)
        {
            if (!NetworkServer.active || !body || !ChargedStormTuning.ClosedCircuit) return;
            var driver = body.GetComponent<ClosedCircuitDriver>();
            if (!driver) driver = body.gameObject.AddComponent<ClosedCircuitDriver>();
            driver.Begin(fed);
        }

        private void Awake() { body = GetComponent<CharacterBody>(); }

        private void Begin(int fed)
        {
            stage = Stage.instance;
            int previous = ledger.Begin(fed);
            if (previous > 0) Discharge(previous);
            wasOpen = true;
            KitLog.Event("CLOSED_CIRCUIT_BEGIN", "fed=" + fed + " carried=" + previous);
        }

        /// <summary>Called by StormServer after an Electrocute awards its charge.</summary>
        internal static void OnElectrocute(CharacterBody attacker, Vector3 point)
        {
            if (!NetworkServer.active || !attacker) return;
            var driver = attacker.GetComponent<ClosedCircuitDriver>();
            if (driver) driver.TryRefund(point);
        }

        private void TryRefund(Vector3 point)
        {
            if (!body || !ledger.Active || !OpenCircuitBuff.Def || !body.HasBuff(OpenCircuitBuff.Def)) return;
            float reach = Mathf.Max(KitTuning.OpenCircuitRadius, ChargedStormTuning.ClosedCircuitReach);
            if ((point - body.corePosition).sqrMagnitude > reach * reach) return;
            var meter = body.GetComponent<DischargeMeter>();
            if (!meter || !meter.CanBank || meter.GazeOwnsBank || !ledger.TryRefund()) return;
            meter.AddCharge();
            StormTelemetry.RecordRefund();
            // The charge visibly travels home: an arc from the victim back to the crown.
            KitFx.Server(Beat.CircuitArc, point, HaloRing.CenterOf(body), 1f, sound: false, owner: body);
            KitLog.Event("CLOSED_CIRCUIT_REFUND", "refunded=" + ledger.Refunded + "/" + ledger.Fed);
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || !body) return;
            bool alive = body.healthComponent && body.healthComponent.alive;
            if (!alive || stage != Stage.instance || (body.master && body.master.GetBody() != body))
            { ledger.Cancel(); wasOpen = false; return; }
            bool open = OpenCircuitBuff.Def && body.HasBuff(OpenCircuitBuff.Def);
            if (wasOpen && !open)
            {
                int owed = ledger.Close();
                if (owed > 0) Discharge(owed);
            }
            wasOpen = open;
        }

        /// <summary>Owed charges burst out of the crown as one close-range nova (no sky strikes).</summary>
        private void Discharge(int owed)
        {
            float radius = ChargedStormTuning.Bound(ChargedStormTuning.ClosingNovaRadius, 4f, 30f);
            var targets = ChargedStormTargeting.Find(body, body.corePosition, radius, Vector3.forward, 180f, false);
            if (targets.Count == 0)
            {
                // Nothing to hit: the crown returns what it still held.
                var meter = body.GetComponent<DischargeMeter>();
                if (meter) meter.ReturnCharges(owed);
                KitLog.Event("CLOSED_CIRCUIT_RETURN", "owed=" + owed);
                return;
            }
            float damage = body.damage * KitDamagePolicy.Effective(ChargedStormTuning.ClosingNovaDamage * owed);
            bool crit = body.RollCrit();
            Vector3 crown = HaloRing.CenterOf(body);
            try { KitFx.Server(Beat.CircuitPulse, body.footPosition, default(Vector3), radius, sound: true, owner: body); }
            catch (System.Exception error) { Plugin.Log.LogWarning("HOLLOW_SAINT_CLOSED_CIRCUIT_FX " + error); }
            for (int i = 0; i < targets.Count; i++)
            {
                var victim = targets[i];
                Vector3 point = ChargedStormTargeting.Point(victim);
                ChargedStormDamage.Hit(body, victim, damage, crit, .5f, DamageSource.Special);
                StormTelemetry.RecordClosingStrike();
                if (victim.alive) StormServer.PrimeStatic(victim, body, StaticPrimePolicy.Amount(ChargedStormTuning.ClosingStrikePrime, 1f));
                if (i < 10) KitFx.Server(Beat.CircuitArc, point, crown, 1f, sound: false, delay: i * .03f, owner: body);
            }
            KitLog.Event("CLOSED_CIRCUIT_NOVA", "owed=" + owed + " targets=" + targets.Count);
        }

        private void OnDisable() { ledger.Cancel(); wasOpen = false; }
    }
}
