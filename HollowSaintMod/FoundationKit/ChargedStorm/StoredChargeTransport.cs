using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace HollowSaint.FoundationKit.ChargedStorm
{
    /// <summary>Independent IDs; native state authenticates each release before server commitment.</summary>
    internal static class StoredChargeTransport
    {
        private const short RequestId = 29040, ReplyId = 29041;
        private static bool installed;
        internal static bool Ready
        {
            get
            {
                if (!installed) return false;
                if (NetworkServer.active) return NetworkServer.handlers.TryGetValue(RequestId, out var request) && request == (NetworkMessageDelegate)ReceiveRequest;
                if (ClientScene.readyConnection == null || !ClientScene.readyConnection.isReady) return false;
                foreach (var client in NetworkClient.allClients)
                    if (client.connection == ClientScene.readyConnection && client.handlers.TryGetValue(ReplyId, out var reply) && reply == (NetworkMessageDelegate)ReceiveReply) return true;
                return false;
            }
        }
        internal sealed class Packet : MessageBase
        {
            internal NetworkInstanceId owner;
            internal uint cast;
            internal byte kind, count;
            internal bool cancel, reply;
            internal Vector3 direction;
            internal float duration;
            public override void Serialize(NetworkWriter w)
            { w.Write(owner); w.Write(cast); w.Write(kind); w.Write(count); w.Write(cancel); w.Write(reply); w.Write(direction); w.Write(duration); }
            public override void Deserialize(NetworkReader r)
            { owner = r.ReadNetworkId(); cast = r.ReadUInt32(); kind = r.ReadByte(); count = r.ReadByte(); cancel = r.ReadBoolean(); reply = r.ReadBoolean(); direction = r.ReadVector3(); duration = r.ReadSingle(); }
        }
        internal static void Install()
        {
            if (installed) return;
            installed = true;
            NetworkManagerSystem.onStartServerGlobal += RegisterServer;
            NetworkManagerSystem.onStartClientGlobal += RegisterClient;
            if (NetworkServer.active) RegisterServer();
            foreach (var client in NetworkClient.allClients) RegisterClient(client);
        }
        private static void RegisterServer()
        {
            if (NetworkServer.handlers.TryGetValue(RequestId, out var existing))
            {
                if (existing == (NetworkMessageDelegate)ReceiveRequest) return;
                Plugin.Log.LogError("HOLLOW_SAINT_STORED_CHARGE_UNAVAILABLE request ID collision=" + RequestId);
                return;
            }
            NetworkServer.RegisterHandler(RequestId, ReceiveRequest);
        }
        private static void RegisterClient(NetworkClient client)
        {
            if (client.handlers.TryGetValue(ReplyId, out var existing))
            {
                if (existing == (NetworkMessageDelegate)ReceiveReply) return;
                Plugin.Log.LogError("HOLLOW_SAINT_STORED_CHARGE_UNAVAILABLE reply ID collision=" + ReplyId);
                return;
            }
            client.RegisterHandler(ReplyId, ReceiveReply);
        }
        internal static void Uninstall()
        {
            if (!installed) return;
            installed = false;
            NetworkManagerSystem.onStartServerGlobal -= RegisterServer;
            NetworkManagerSystem.onStartClientGlobal -= RegisterClient;
            if (NetworkServer.handlers.TryGetValue(RequestId, out var request) && request == (NetworkMessageDelegate)ReceiveRequest)
                NetworkServer.UnregisterHandler(RequestId);
            foreach (var client in NetworkClient.allClients)
                if (client.handlers.TryGetValue(ReplyId, out var reply) && reply == (NetworkMessageDelegate)ReceiveReply)
                    client.UnregisterHandler(ReplyId);
        }
        internal static void Request(CharacterBody body, Packet p)
        {
            var identity = body ? body.GetComponent<NetworkIdentity>() : null;
            if (!identity || !body.hasEffectiveAuthority) return;
            p.owner = identity.netId;
            if (NetworkServer.active) State(body, p.kind)?.ServerRequest(p, null, true);
            else if (ClientScene.readyConnection != null && ClientScene.readyConnection.isReady) ClientScene.readyConnection.Send(RequestId, p);
        }
        internal static void Reply(CharacterBody body, Packet p)
        {
            if (!NetworkServer.active || !body) return;
            var identity = body.GetComponent<NetworkIdentity>();
            if (!identity) return;
            p.owner = identity.netId; p.reply = true;
            // Host has already applied the outcome; duplicate replies are idempotent.
            NetworkServer.SendToAll(ReplyId, p);
        }
        private static StoredChargeState State(CharacterBody body, byte kind)
        {
            if (!body || kind > 2) return null;
            var machine = EntityStateMachine.FindByCustomName(body.gameObject, kind == 1 ? Stormspear.StormspearRegistration.MachineName : KitRegistration.CrownMachineName);
            var state = machine ? machine.state as StoredChargeState : null;
            return state != null && state.Kind == kind ? state : null;
        }
        private static void ReceiveRequest(NetworkMessage message)
        {
            try
            {
                var p = message.ReadMessage<Packet>();
                var obj = NetworkServer.FindLocalObject(p.owner);
                State(obj ? obj.GetComponent<CharacterBody>() : null, p.kind)?.ServerRequest(p, message.conn, false);
            }
            catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_STORED_CHARGE_REQUEST " + error); throw; }
        }
        private static void ReceiveReply(NetworkMessage message)
        {
            try
            {
                var p = message.ReadMessage<Packet>();
                var obj = ClientScene.FindLocalObject(p.owner);
                State(obj ? obj.GetComponent<CharacterBody>() : null, p.kind)?.ReceiveReply(p);
            }
            catch (System.Exception error) { Plugin.Log.LogError("HOLLOW_SAINT_STORED_CHARGE_REPLY " + error); throw; }
        }
    }
}
