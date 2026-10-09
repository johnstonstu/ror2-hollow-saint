using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    [DisallowMultipleComponent]
    public sealed class StoredChargeDriver : MonoBehaviour
    {
        private CharacterBody body;
        private uint next;
        private Thundercloud.ServerThundercloud preparedCloud;
        private readonly List<Thundercloud.ServerThundercloud> clouds = new List<Thundercloud.ServerThundercloud>();
        private readonly List<HollowedOrb.ServerHollowedOrb> orbs = new List<HollowedOrb.ServerHollowedOrb>();
        internal uint NextToken() { next++; if (next == 0) next++; return next; }
        internal void RememberToken(uint token) { if (token > next) next = token; }
        private void Awake() { body = GetComponent<CharacterBody>(); }
        internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal bool Prepare(byte kind, int count, Vector3 direction)
        {
            if (!NetworkServer.active || !body || !body.healthComponent || !body.healthComponent.alive ||
                clouds.Count + orbs.Count >= 16) return false;
            preparedCloud = null;
            if (kind == 1 || kind == 2) return true;
            if (kind != 0) return false;
            preparedCloud = Thundercloud.ServerThundercloud.Prepare(body, count, direction);
            return preparedCloud != null;
        }
        internal float Launch(byte kind, uint cast, int charges, Vector3 direction)
        {
            if (!NetworkServer.active) throw new System.InvalidOperationException("Stored charge launch requires server ownership.");
            if (kind == 2) return OpenCircuit.OpenCircuitTuning.CastClipSeconds;
            if (kind == 0)
            {
                var cloud = preparedCloud; preparedCloud = null;
                if (cloud == null) throw new System.InvalidOperationException("Cloud was not prepared before charge commitment.");
                clouds.Add(cloud); cloud.Begin();
                return cloud.Duration;
            }
            var orb = new HollowedOrb.ServerHollowedOrb(body, charges, direction);
            orbs.Add(orb); orb.Begin();
            return HollowedOrb.OrbCastFlow.Recovery;
        }
        private void FixedUpdate()
        {
            if (!NetworkServer.active) return;
            try
            {
                for (int i = clouds.Count - 1; i >= 0; i--)
                    if (!clouds[i].Tick(Time.fixedDeltaTime)) clouds.RemoveAt(i);
                for (int i = orbs.Count - 1; i >= 0; i--)
                    if (!orbs[i].Tick(Time.fixedDeltaTime)) { orbs[i].End(); orbs.RemoveAt(i); }
            }
            catch (System.Exception error)
            {
                clouds.Clear(); orbs.Clear();
                Plugin.Log.LogError("HOLLOW_SAINT_CHARGED_STORM_SERVER_TICK body=" + (body ? body.name : "lost") + " " + error);
                throw;
            }
        }
        private void OnDisable()
        {
            if (body) foreach (var machine in body.GetComponents<EntityStateMachine>())
                if (machine.state is StoredChargeState state) state.AbortForBodyLoss();
            if (NetworkServer.active) foreach (var orb in orbs) orb.End();
            clouds.Clear(); orbs.Clear(); preparedCloud = null;
        }
    }
}
