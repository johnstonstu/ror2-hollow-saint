using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace HollowSaint.FoundationKit.Gaze
{
    /// <summary>Fixed-size server-to-client cosmetic messages on UNet's reliable channel.
    /// No per-frame traffic or dependency. Collision is checked instead of replacing another handler.</summary>
    internal static class GazeFuelTransport
    {
        private const short MessageId = 29037;
        private static bool installed, warned;
        private const int MaxPendingOwners = 16, MaxPendingPackets = 64;
        private const float PendingLifetime = 2f;
        private sealed class Pending
        {
            public float expires;
            public readonly List<Packet> packets = new List<Packet>(MaxPendingPackets);
        }
        private static readonly Dictionary<NetworkInstanceId, Pending> pending = new Dictionary<NetworkInstanceId, Pending>();
        private static readonly List<NetworkInstanceId> expired = new List<NetworkInstanceId>(MaxPendingOwners);
        internal enum Kind : byte { Begin, Swallow, Launch, Strike, Reserve, End }
        internal sealed class Packet : MessageBase
        {
            public NetworkInstanceId owner;
            public uint cast, sequence;
            public Kind kind;
            public byte phase, count, capacity, reserve, unspent, spent, reason, orbIndex, retained;
            public bool full, ground;
            public float age, travel, spread, radius, sentAt;
            public Vector3 origin, impact, groundPoint, normal;
            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(owner); writer.Write(cast); writer.Write(sequence); writer.Write((byte)kind);
                writer.Write(phase); writer.Write(count); writer.Write(capacity); writer.Write(reserve);
                writer.Write(unspent); writer.Write(spent); writer.Write(reason); writer.Write(full); writer.Write(ground);
                writer.Write(orbIndex); writer.Write(retained);
                writer.Write(age); writer.Write(travel); writer.Write(spread); writer.Write(radius);
                writer.Write(sentAt);
                writer.Write(origin); writer.Write(impact); writer.Write(groundPoint); writer.Write(normal);
            }
            public override void Deserialize(NetworkReader reader)
            {
                owner = reader.ReadNetworkId(); cast = reader.ReadUInt32(); sequence = reader.ReadUInt32(); kind = (Kind)reader.ReadByte();
                phase = reader.ReadByte(); count = reader.ReadByte(); capacity = reader.ReadByte(); reserve = reader.ReadByte();
                unspent = reader.ReadByte(); spent = reader.ReadByte(); reason = reader.ReadByte(); full = reader.ReadBoolean(); ground = reader.ReadBoolean();
                orbIndex = reader.ReadByte(); retained = reader.ReadByte();
                age = reader.ReadSingle(); travel = reader.ReadSingle(); spread = reader.ReadSingle(); radius = reader.ReadSingle();
                sentAt = reader.ReadSingle();
                origin = reader.ReadVector3(); impact = reader.ReadVector3(); groundPoint = reader.ReadVector3(); normal = reader.ReadVector3();
            }
        }

        public static void Install()
        {
            if (installed) return;
            installed = true;
            NetworkManagerSystem.onStartClientGlobal += Register;
            foreach (var client in NetworkClient.allClients) Register(client);
        }

        private static void Register(NetworkClient client)
        {
            if (client == null) return;
            try
            {
                if (client.handlers.ContainsKey(MessageId)) { Warn("UNet message id already registered; cosmetic transport unavailable", null); return; }
                client.RegisterHandler(MessageId, Receive);
            }
            catch (System.Exception error) { Warn("handler registration failed", error); }
        }

        public static void Send(CharacterBody body, Packet packet)
        {
            if (!NetworkServer.active || !body) return;
            var identity = body.GetComponent<NetworkIdentity>();
            if (!identity) return;
            packet.owner = identity.netId;
            packet.sentAt = Clock();
            try { NetworkServer.SendToAll(MessageId, packet); }
            catch (System.Exception error) { Warn("send failed", error); }
        }
        private static float Clock() => NetworkManagerSystem.singleton ? NetworkManagerSystem.singleton.serverFixedTime : Time.fixedTime;
        internal static float EventAge(Packet packet) => Mathf.Max(0f, Clock() - packet.sentAt);

        private static void Receive(NetworkMessage message)
        {
            try
            {
                var packet = message.ReadMessage<Packet>();
                var owner = ClientScene.FindLocalObject(packet.owner);
                var driver = owner ? owner.GetComponent<GazeFuelController>() : null;
                if (driver) driver.Receive(packet);
                else Queue(packet);
            }
            catch (System.Exception error) { Warn("receive failed", error); }
        }

        private static void Prune()
        {
            expired.Clear();
            foreach (var pair in pending) if (Time.time >= pair.Value.expires) expired.Add(pair.Key);
            for (int i = 0; i < expired.Count; i++) pending.Remove(expired[i]);
        }
        private static void Queue(Packet packet)
        {
            Prune();
            Pending entry;
            if (!pending.TryGetValue(packet.owner, out entry))
            {
                if (pending.Count >= MaxPendingOwners) return;
                entry = new Pending { expires = Time.time + PendingLifetime };
                pending.Add(packet.owner, entry);
            }
            if (entry.packets.Count < MaxPendingPackets) entry.packets.Add(packet);
        }
        internal static void Flush(CharacterBody body)
        {
            if (pending.Count == 0 || !body) return;
            Prune();
            var identity = body.GetComponent<NetworkIdentity>();
            Pending entry;
            if (!identity || !pending.TryGetValue(identity.netId, out entry)) return;
            pending.Remove(identity.netId);
            var driver = body.GetComponent<GazeFuelController>();
            if (!driver) return;
            for (int i = 0; i < entry.packets.Count; i++) driver.Receive(entry.packets[i]);
        }
        internal static void Forget(CharacterBody body)
        {
            var identity = body ? body.GetComponent<NetworkIdentity>() : null;
            if (identity) pending.Remove(identity.netId);
        }

        internal static void Warn(string operation, System.Exception error)
        {
            if (warned) return;
            warned = true;
            Plugin.Log.LogWarning("HOLLOW_SAINT_GAZE_FUEL_PRESENTATION " + operation + (error != null ? " " + error : ""));
        }
    }
}
