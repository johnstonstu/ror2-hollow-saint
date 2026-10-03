using System;
using HollowSaint.FoundationKit.OpenCircuit;
using RoR2;
using UnityEngine;

namespace HollowSaint.FoundationKit.Stormspear
{
    public enum SpearForm { None = 0, Hand = 1, Crown = 2 }

    /// <summary>
    /// v0.9 SHARED CONTRACT between gameplay (the charge/throw state) and presentation (VFX,
    /// arm pose, Arc Bolt off-hand). One per Hollow Saint body, on every machine.
    ///
    /// Writers: only the Stormspear charge EntityState calls Begin / SetCharge / Release / Cancel.
    /// EntityStates run on every machine (authority, server copy, other clients), so every
    /// machine's component reflects the same charge locally; nothing here is networked.
    ///
    /// Readers: Arc Bolt (OffHandOnly: half rate, from the hand without the spear), VFX (Charging, Charge01, Form,
    /// events), projectile ghost (LastReleaseCharge).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StormspearCharge : MonoBehaviour
    {
        public CharacterBody Body { get; private set; }
        public bool Charging { get; private set; }
        /// <summary>0 at the press, 1 at full. Already includes attack speed and the crown multiplier.</summary>
        public float Charge01 { get; private set; }
        public SpearForm Form { get; private set; }
        public bool Full => Charging && Charge01 >= 0.999f;
        /// <summary>Hand-form charge: Arc Bolt fires from the off hand (the one without the spear) at reduced rate.</summary>
        public bool OffHandOnly => Charging && Form == SpearForm.Hand;
        public float ChargeStartTime { get; private set; }

        public float LastReleaseCharge { get; private set; }
        public SpearForm LastReleaseForm { get; private set; }
        public float LastReleaseTime { get; private set; } = -100f;

        /// <summary>Raised on every machine. Release fires after LastRelease* are set.</summary>
        public event Action<StormspearCharge> Begun, Released, Cancelled;
        /// <summary>Raised when Charge01 crosses 1/3, 2/3 and 1 (argument: 1, 2 or 3).</summary>
        public event Action<StormspearCharge, int> Tick;
        private int ticks;

        private void Awake() { Body = GetComponent<CharacterBody>(); }

        public static StormspearCharge Of(CharacterBody body) { return body ? body.GetComponent<StormspearCharge>() : null; }
        public static bool InCrown(CharacterBody body) { return body && OpenCircuitBuff.Def && body.HasBuff(OpenCircuitBuff.Def); }
        public static bool OffHand(CharacterBody body) { var c = Of(body); return c && c.OffHandOnly; }

        public void Begin(SpearForm form)
        {
            cancelAt = 0f;
            Charging = true; Form = form; Charge01 = 0f; ticks = 0; ChargeStartTime = Time.time;
            Begun?.Invoke(this);
        }
        public void SetCharge(float charge01)
        {
            if (!Charging) return;
            Charge01 = Mathf.Clamp01(charge01);
            int want = Charge01 >= 0.999f ? 3 : Charge01 >= 2f / 3f ? 2 : Charge01 >= 1f / 3f ? 1 : 0;
            while (ticks < want) { ticks++; Tick?.Invoke(this, ticks); }
        }
        /// <summary>Form can change mid-charge (crown opens or closes); presentation reads it every frame.</summary>
        public void SetForm(SpearForm form) { if (Charging) Form = form; }
        public void Release()
        {
            cancelAt = 0f;
            if (!Charging) return;
            LastReleaseCharge = Charge01; LastReleaseForm = Form; LastReleaseTime = Time.time;
            Charging = false;
            Released?.Invoke(this);
            Form = SpearForm.None; Charge01 = 0f;
        }
        /// <summary>Non-authority machines cannot tell an interrupted charge from the same-frame
        /// networked hand-off to the throw state. They call this instead of Cancel: the cancel
        /// fires after a short delay unless Begin or Release arrives first.</summary>
        public void CancelDeferred() { if (Charging) cancelAt = Time.time + 0.2f; }
        private float cancelAt;
        private void Update()
        {
            if (cancelAt > 0f && Time.time >= cancelAt) { cancelAt = 0f; Cancel(); }
        }

        public void Cancel()
        {
            cancelAt = 0f;
            if (!Charging) return;
            Charging = false;
            Cancelled?.Invoke(this);
            Form = SpearForm.None; Charge01 = 0f;
        }
        private void OnDisable() { Cancel(); }
    }
}
