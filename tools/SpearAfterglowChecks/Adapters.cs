using System;
using System.Collections.Generic;
using N = System.Numerics;

namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; public static void Destroy(Object o) {} }
    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject.transform; public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>(); }
    public class GameObject : Object
    {
        public static int Created; public readonly Transform transform = new Transform(); private readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>(); public bool activeSelf=true; public GameObject(string name=""){Created++;transform.gameObject=this;} public void SetActive(bool v){activeSelf=v;} public T GetComponent<T>() where T:Component=>components.TryGetValue(typeof(T),out var v)?(T)v:null;
        public T AddComponent<T>() where T : Component, new() {var c=new T { gameObject=this };components[typeof(T)]=c;return c;}
    }
    public class Transform : Object
    {
        public GameObject gameObject; public T GetComponent<T>() where T:Component=>gameObject?.GetComponent<T>(); public string name;
        private Transform parent;
        private readonly List<Transform> children = new List<Transform>();
        public Vector3 localPosition, localScale = Vector3.one;
        public Quaternion localRotation = Quaternion.identity;
        public Vector3 position { get => parent ? parent.TransformPoint(localPosition) : localPosition;
            set => localPosition = parent ? parent.InverseTransformPoint(value) : value; }
        public Quaternion rotation { get => parent ? parent.rotation * localRotation : localRotation;
            set => localRotation = parent ? Quaternion.Inverse(parent.rotation) * value : value; }
        public Vector3 lossyScale => parent ? Vector3.Scale(parent.lossyScale, localScale) : localScale;
        public Vector3 up => rotation * Vector3.up;
        public Vector3 forward => rotation * Vector3.forward;
        public void SetParent(Transform value,bool unused) {SetParent(value);} public void SetParent(Transform value) { parent = value; value.children.Add(this); }
        public void SetPositionAndRotation(Vector3 point, Quaternion orientation) { position = point; rotation = orientation; }
        public Vector3 TransformPoint(Vector3 point) => position + rotation * Vector3.Scale(point, lossyScale);
        public Vector3 InverseTransformPoint(Vector3 point)
        {
            var p = Quaternion.Inverse(rotation) * (point - position);
            return new Vector3(p.x / lossyScale.x, p.y / lossyScale.y, p.z / lossyScale.z);
        }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Object
        {
            var found = new List<T>();
            void Visit(Transform value) { if (value is T item) found.Add(item); foreach (var child in value.children) Visit(child); }
            Visit(this); return found.ToArray();
        }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 down => new Vector3(0,-1,0); public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public float magnitude=>MathF.Sqrt(sqrMagnitude); public static Vector3 right=>new Vector3(1,0,0); public static Vector3 operator -(Vector3 a)=>a*-1; public static Vector3 operator /(Vector3 a,float b)=>a*(1/b); public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x); public static Vector3 ClampMagnitude(Vector3 v,float r)=>v.magnitude>r?v.normalized*r:v; public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized => this * (1f / MathF.Max(.00001f, MathF.Sqrt(sqrMagnitude)));
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x * b, a.y * b, a.z * b);
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;
        public static Vector3 ProjectOnPlane(Vector3 v, Vector3 normal) => v - normal * Dot(v, normal);
        public static float Distance(Vector3 a, Vector3 b) => MathF.Sqrt((a - b).sqrMagnitude);
    }
    public struct Quaternion
    {
        private N.Quaternion value;
        private Quaternion(N.Quaternion value) { this.value = value; }
        public static Quaternion identity => new Quaternion(N.Quaternion.Identity);
        public static Quaternion Inverse(Quaternion q) => new Quaternion(N.Quaternion.Inverse(q.value));
        public static Quaternion AngleAxis(float angle, Vector3 axis) => new Quaternion(N.Quaternion.CreateFromAxisAngle(new N.Vector3(axis.x, axis.y, axis.z), angle * MathF.PI / 180f));
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(a.value * b.value);
        public static Vector3 operator *(Quaternion q, Vector3 p)
        { var v = N.Vector3.Transform(new N.Vector3(p.x, p.y, p.z), q.value); return new Vector3(v.X, v.Y, v.Z); }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => new Quaternion(N.Quaternion.Slerp(a.value, b.value, t));
        public static Quaternion LookRotation(Vector3 forward, Vector3 up)
        {
            var f = N.Vector3.Normalize(new N.Vector3(forward.x, forward.y, forward.z));
            var r = N.Vector3.Normalize(N.Vector3.Cross(new N.Vector3(up.x, up.y, up.z), f));
            var u = N.Vector3.Cross(f, r);
            return new Quaternion(N.Quaternion.CreateFromRotationMatrix(new N.Matrix4x4(r.X,r.Y,r.Z,0,u.X,u.Y,u.Z,0,f.X,f.Y,f.Z,0,0,0,0,1)));
        }
        public static bool Same(Quaternion a, Quaternion b) => MathF.Abs(N.Quaternion.Dot(a.value, b.value)) > .99999f;
    }
    public static class Mathf {public const float Deg2Rad=MathF.PI/180; public static float Abs(float v)=>MathF.Abs(v); public static float Cos(float v)=>MathF.Cos(v);public static float Sqrt(float v)=>MathF.Sqrt(v);public static float Atan2(float y,float x)=>MathF.Atan2(y,x);public static float Exp(float v)=>MathF.Exp(v);public static float Floor(float v)=>MathF.Floor(v);public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>MathF.Min(a,b);public static float Max(float a,float b)=>MathF.Max(a,b);public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static float MoveTowards(float a,float b,float d)=>a<b?Min(b,a+d):Max(b,a-d);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return Lerp(a,b,t*t*(3-2*t));} public const float PI = MathF.PI; public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f); public static float Sin(float value) => MathF.Sin(value); }
}
namespace RoR2
{
    public class CharacterBody : UnityEngine.Component { public ModelLocator modelLocator; public UnityEngine.Transform Socket;public HealthComponent healthComponent=new HealthComponent();public UnityEngine.Vector3 corePosition; public uint skinIndex;public bool buff;public bool HasBuff(UnityEngine.Object d)=>buff; }
    public class ModelLocator : UnityEngine.Object { public UnityEngine.Transform modelTransform; }
}
namespace HollowSaint.FoundationKit
{
    public static class KitUtil { public static UnityEngine.Transform ResolveSocket(RoR2.CharacterBody body, string alias) => body.Socket; }
}
namespace HollowSaint.FoundationKit.Vfx
{
    public class HaloRing : UnityEngine.Object
    {
        public void EnsureFitted(bool force = false) { }
        public bool Valid = true;
        public readonly ShapeData Shape = new ShapeData();
        public class ShapeData { public UnityEngine.Vector3 Center; }
        public static HaloRing For(RoR2.CharacterBody body)
        {
            var ring = new HaloRing(); int count = 0; if(body?.modelLocator?.modelTransform==null)return ring;
            foreach (var t in body.modelLocator.modelTransform.GetComponentsInChildren<UnityEngine.Transform>(true))
                if (t.name != null && t.name.StartsWith("halo ") && t.name != "halo root") { ring.Shape.Center += t.position; count++; }
            ring.Shape.Center *= 1f / count; return ring;
        }
    }
}
