using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object o) => o != null; public static void Destroy(Object o) {} }
    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject.transform; public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>(); }
    public class MonoBehaviour : Component {}
    public class Transform : Component { public Vector3 position, forward=Vector3.forward, right=Vector3.right; public void SetParent(Transform p,bool b) {} }
    public class GameObject : Object
    {
        public static int Created; public bool activeSelf=true; public Transform transform; readonly Dictionary<Type,Component> parts=new Dictionary<Type,Component>();
        public GameObject(string n="") { Created++; transform=new Transform { gameObject=this }; }
        public T AddComponent<T>() where T:Component,new() { var p=new T { gameObject=this }; parts[typeof(T)]=p; return p; }
        public T GetComponent<T>() where T:Component { Component c; return parts.TryGetValue(typeof(T),out c)?(T)c:null; }
        public void SetActive(bool b) {activeSelf=b;}
    }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder:Attribute { public DefaultExecutionOrder(int n){} }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent:Attribute {}
    public struct Vector3
    {
        public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3(); public static Vector3 up=>new Vector3(0,1,0); public static Vector3 down=>-up; public static Vector3 forward=>new Vector3(0,0,1); public static Vector3 right=>new Vector3(1,0,0);
        public float sqrMagnitude=>x*x+y*y+z*z; public float magnitude=>(float)Math.Sqrt(sqrMagnitude); public Vector3 normalized=>magnitude>1e-6f?this/magnitude:zero;
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator -(Vector3 a)=>a*-1;
        public static Vector3 operator *(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);
        public static Vector3 operator *(float f,Vector3 a)=>a*f;
        public static Vector3 operator /(Vector3 a,float f)=>a*(1/f);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
        public static Vector3 ProjectOnPlane(Vector3 a,Vector3 n)=>a-n*Dot(a,n);
        public static Vector3 ClampMagnitude(Vector3 a,float m)=>a.magnitude>m?a.normalized*m:a;
        public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
    }
    public static class Mathf
    {
        public const float PI=(float)Math.PI;
        public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x)); public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(b,x));
        public static float Clamp01(float x)=>Clamp(x,0,1); public static float Abs(float f)=>Math.Abs(f); public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Sin(float f)=>(float)Math.Sin(f); public static float Cos(float f)=>(float)Math.Cos(f); public static float Pow(float f,float e)=>(float)Math.Pow(f,e);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float InverseLerp(float a,float b,float t)=>a==b?0:Clamp01((t-a)/(b-a));
        public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return Lerp(a,b,t*t*(3-2*t));}
    }
    public struct Color {public float r,g,b,a; public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;} public static Color white=>new Color(1,1,1);public static Color Lerp(Color x,Color y,float t)=>new Color(Mathf.Lerp(x.r,y.r,t),Mathf.Lerp(x.g,y.g,t),Mathf.Lerp(x.b,y.b,t),Mathf.Lerp(x.a,y.a,t)); }
    public class Shader:Object {public static Shader Find(string name)=>new Shader();}
    public class Material:Object {public string name="test";public int renderQueue;public Dictionary<string,Color> Colors=new Dictionary<string,Color>();public Dictionary<string,Texture2D> Textures=new Dictionary<string,Texture2D>();public Material(){}public Material(Material m){}public Material(Shader s){}public bool HasProperty(string p)=>true;public void SetColor(string p,Color c){Colors[p]=c;}public void SetTexture(string p,Texture2D t){Textures[p]=t;} }
    public enum TextureFormat {RGBA32}public enum TextureWrapMode {Clamp}
    public class Texture2D:Object {public string name;public TextureWrapMode wrapMode;public readonly Color[] pixels;public Texture2D(int w,int h,TextureFormat f,bool m){pixels=new Color[w*h];}public void SetPixel(int x,int y,Color c){pixels[x]=c;}public void Apply(bool x,bool y){} }
    public enum LineAlignment { View } public enum LineTextureMode { Stretch }
    public class LineRenderer:Component { public bool useWorldSpace,receiveShadows,enabled; public LineAlignment alignment; public LineTextureMode textureMode; public Material sharedMaterial; public Rendering.ShadowCastingMode shadowCastingMode; public int numCapVertices,numCornerVertices,positionCount; public float widthMultiplier; public Color startColor,endColor; public AnimationCurve widthCurve; public readonly Vector3[] points=new Vector3[32]; public void SetPosition(int i,Vector3 p){points[i]=p;} }
    namespace Rendering {public enum ShadowCastingMode { Off }}
    public class AnimationCurve { public AnimationCurve(params Keyframe[] f){} } public struct Keyframe { public Keyframe(float a,float b){} }
    public static class Time {public static float time;}
    public enum QueryTriggerInteraction { Ignore }
    public struct RaycastHit {public Vector3 point,normal;}
    public static class Physics
    {
        public static int Rays,Segments; public static bool Void,Wall; public static float GapAt=float.PositiveInfinity;
        public static bool Raycast(Vector3 p,Vector3 d,out RaycastHit h,float length,int mask,QueryTriggerInteraction q)
        {Rays++;h=new RaycastHit();if(Void||d.y>=0||p.y<0||p.y>length||Math.Abs(p.x)>GapAt)return false;h.point=new Vector3(p.x,0,p.z);h.normal=Vector3.up;return true;}
        public static bool Linecast(Vector3 a,Vector3 b,int mask,QueryTriggerInteraction q){Segments++;return Wall;}
    }
}
namespace RoR2
{
    using UnityEngine;
    public class CharacterBody:Component {public uint skinIndex;public HealthComponent healthComponent=new HealthComponent(); public Vector3 corePosition; public ModelLocator modelLocator;}
    public class HealthComponent:Object {public bool alive=true;}
    public class ModelLocator:Object {public Transform modelTransform;}
    public class CharacterModel:Component {public int invisibilityCount;}
    public struct LayerIndex {public int mask;public static LayerIndex world=>new LayerIndex();}
    public static class Util {public static int Sounds;public static void PlaySound(string s,GameObject o){Sounds++;}}
}
namespace HollowSaint.FoundationKit.Vfx
{
    using UnityEngine;using RoR2;
    public class SkinFxPalette {public int Index;public Color Arc=new Color(.3f,.9f,1),Outer=new Color(.1f,.5f,.8f),Core=new Color(1,1,1);public static SkinFxPalette ForBody(CharacterBody b)=>new SkinFxPalette {Index=(int)b.skinIndex};public Material Material(Material m)=>m;}
    public static class VfxAssets {public static Material ArcCore=new Material(),ArcGlow=new Material();public static void Load(){} }
    public class HaloRing:Object {public bool Valid=true;public float RadiusScale=1;public ShapeData Shape=new ShapeData();public class ShapeData {public Vector3 Center,Axis=Vector3.forward,Binormal=Vector3.right;}public static HaloRing For(CharacterBody b)=>null;}
}
namespace HollowSaint.FoundationKit.Gaze {public class GazeBeam:UnityEngine.Component {public UnityEngine.Vector3 Origin,Direction=UnityEngine.Vector3.forward;}}
namespace HollowSaint.FoundationKit.Gaze.Fx {public static class GazeSfx {public const string ForkHit="hit";}}
