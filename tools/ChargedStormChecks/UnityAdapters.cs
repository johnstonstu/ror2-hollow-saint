using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Explicit in-memory native adapters. No Unity rendering or UNet execution is implied.
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object obj) => obj != null; }
    public class Component : Object
    {
        public GameObject gameObject;
        public string name => gameObject.name;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
        public T[] GetComponents<T>() => gameObject.GetComponents<T>();
    }
    public class MonoBehaviour : Component { public bool isActiveAndEnabled = true; }
    public class Transform { public Vector3 position, forward = Vector3.forward; }
    public class GameObject : Object
    {
        public string name = "adapter";
        public int layer;
        public Transform transform = new();
        readonly List<Component> components = new();
        public T AddComponent<T>() where T : Component, new()
        {
            var c = new T { gameObject = this }; components.Add(c);
            typeof(T).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(c, null);
            return c;
        }
        public T GetComponent<T>() where T : class => components.OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() => components.OfType<T>().ToArray();
    }
    public class DisallowMultipleComponent : Attribute { }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new();
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 forward => new(0, 0, 1);
        public float sqrMagnitude => x*x + y*y + z*z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > 0 ? this / magnitude : zero;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a, float b) => new(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a, float b) => new(a.x/b,a.y/b,a.z/b);
        public static float Distance(Vector3 a, Vector3 b) => (a-b).magnitude;
        public static float Dot(Vector3 a, Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static float Angle(Vector3 a, Vector3 b) => MathF.Acos(Math.Clamp(Dot(a.normalized,b.normalized),-1f,1f))*180f/MathF.PI;
    }
    public struct Ray { public Vector3 origin, direction; public Ray(Vector3 origin,Vector3 direction) { this.origin=origin;this.direction=direction; } }
    public struct Quaternion { }
    public class Collider : Component { }
    public struct RaycastHit { public Vector3 point; public float distance; public Collider collider; }
    public enum QueryTriggerInteraction { Ignore }
    public static class Physics
    {
        public static Func<Vector3,Vector3,bool> Obstructed = (a,b) => false;
        public static RaycastHit[] Collisions = Array.Empty<RaycastHit>();
        public static Vector3? AimHit;
        public static bool WorldOverlap;
        public static float? SphereObstruction;
        public static bool SphereCast(Vector3 a,float radius,Vector3 d,out RaycastHit hit,float length,int mask,QueryTriggerInteraction query)
        { hit = new() { distance = SphereObstruction ?? 0 }; return SphereObstruction.HasValue; }
        public static bool CheckSphere(Vector3 point,float radius,int mask,QueryTriggerInteraction query) => WorldOverlap;
        public static bool Linecast(Vector3 a,Vector3 b,int mask,QueryTriggerInteraction query) => Obstructed(a,b);
        public static bool Raycast(Vector3 a,Vector3 d,out RaycastHit hit,float range,int mask,QueryTriggerInteraction query)
        { hit = new() { point = AimHit ?? Vector3.zero }; return AimHit.HasValue && d.z > .5f; }
        public static RaycastHit[] SphereCastAll(Vector3 a,float radius,Vector3 d,float length,int mask,QueryTriggerInteraction query)
        { var result = Collisions; Collisions = Array.Empty<RaycastHit>(); return result; }
    }
    public static class Time { public static float fixedDeltaTime = .02f, unscaledTime, time; }
    public static class Mathf
    {
        public static float Abs(float x) => Math.Abs(x);
        public static float Max(float a,float b) => Math.Max(a,b);
        public static int Max(int a,int b) => Math.Max(a,b);
        public static float Min(float a,float b) => Math.Min(a,b);
        public static int Min(int a,int b) => Math.Min(a,b);
        public static int Clamp(int x,int a,int b) => Math.Clamp(x,a,b);
        public static float Clamp(float x,float a,float b) => Math.Clamp(x,a,b);
        public static float Clamp01(float x) => Math.Clamp(x,0,1);
    }
    public struct Color { public Color(float r,float g,float b) {} }
}
namespace UnityEngine.Networking
{
    public delegate void NetworkMessageDelegate(NetworkMessage message);
    public struct NetworkInstanceId { public uint Value; public NetworkInstanceId(uint value) { Value=value; } }
    public class NetworkIdentity : UnityEngine.Component { public NetworkInstanceId netId = new(1); }
    public class NetworkConnection
    {
        public bool isReady = true;
        public MessageBase Last;
        public void Send(short id, MessageBase p) { Last = p; }
    }
    public class MessageBase { public virtual void Serialize(NetworkWriter w) {} public virtual void Deserialize(NetworkReader r) {} }
    public class NetworkWriter
    {
        public readonly MemoryStream Stream = new();
        BinaryWriter writer; public NetworkWriter() { writer = new(Stream); }
        public void Write(uint v) => writer.Write(v); public void Write(byte v) => writer.Write(v);
        public void Write(bool v) => writer.Write(v); public void Write(float v) => writer.Write(v);
        public void Write(NetworkInstanceId v) => Write(v.Value);
        public void Write(UnityEngine.Vector3 v) { Write(v.x); Write(v.y); Write(v.z); }
    }
    public class NetworkReader
    {
        BinaryReader reader; public NetworkReader(byte[] bytes) { reader = new(new MemoryStream(bytes)); }
        public uint ReadUInt32() => reader.ReadUInt32(); public byte ReadByte() => reader.ReadByte();
        public bool ReadBoolean() => reader.ReadBoolean(); public float ReadSingle() => reader.ReadSingle();
        public NetworkInstanceId ReadNetworkId() => new(ReadUInt32());
        public UnityEngine.Vector3 ReadVector3() => new(ReadSingle(),ReadSingle(),ReadSingle());
    }
    public class NetworkMessage
    { public NetworkConnection conn; public MessageBase packet; public T ReadMessage<T>() where T:MessageBase => (T)packet; }
    public class NetworkClient
    {
        public static List<NetworkClient> allClients = new();
        public NetworkConnection connection = ClientScene.readyConnection;
        public Dictionary<short,NetworkMessageDelegate> handlers = new();
        public void RegisterHandler(short id,NetworkMessageDelegate fn) => handlers.Add(id,fn);
        public void UnregisterHandler(short id) => handlers.Remove(id);
    }
    public static class NetworkServer
    {
        public static bool active = true;
        public static Dictionary<short,NetworkMessageDelegate> handlers = new();
        public static Dictionary<uint,UnityEngine.GameObject> objects = new();
        public static List<MessageBase> replies = new();
        public static void RegisterHandler(short id,NetworkMessageDelegate fn) => handlers.Add(id,fn);
        public static void UnregisterHandler(short id) => handlers.Remove(id);
        public static void SendToAll(short id,MessageBase p) { replies.Add(p); }
        public static UnityEngine.GameObject FindLocalObject(NetworkInstanceId id) => objects.GetValueOrDefault(id.Value);
    }
    public static class ClientScene
    {
        public static NetworkConnection readyConnection = new();
        public static UnityEngine.GameObject FindLocalObject(NetworkInstanceId id) => NetworkServer.FindLocalObject(id);
    }
}
namespace RoR2.Networking
{
    public static class NetworkManagerSystem
    {
        public static event Action onStartServerGlobal;
        public static event Action<UnityEngine.Networking.NetworkClient> onStartClientGlobal;
        public static void StartServer() => onStartServerGlobal?.Invoke();
        public static void StartClient(UnityEngine.Networking.NetworkClient client) => onStartClientGlobal?.Invoke(client);
    }
}
