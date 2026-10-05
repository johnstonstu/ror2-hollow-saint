using System;
namespace UnityEngine {
 public struct Color32 { public byte r,g,b,a; public static implicit operator Color32(Color c)=>new Color32{r=(byte)(Mathf.Clamp01(c.r)*255),g=(byte)(Mathf.Clamp01(c.g)*255),b=(byte)(Mathf.Clamp01(c.b)*255),a=(byte)(Mathf.Clamp01(c.a)*255)}; }
 public class Renderer:Component { public Material sharedMaterial; public Material[] sharedMaterials=Array.Empty<Material>(); public Gradient colorGradient=new Gradient(); }
 public class TrailRenderer:Renderer {}
 public class Light:Component { public Color color; }
 public struct GradientColorKey {public Color color; public float time; public GradientColorKey(Color c,float t){color=c;time=t;} }
 public struct GradientAlphaKey {public float alpha,time; public GradientAlphaKey(float a,float t){alpha=a;time=t;} }
 public class Gradient { public GradientColorKey[] colorKeys=Array.Empty<GradientColorKey>();public GradientAlphaKey[] alphaKeys=Array.Empty<GradientAlphaKey>();public int mode;public void SetKeys(GradientColorKey[] c,GradientAlphaKey[] a){colorKeys=c;alphaKeys=a;} public Color Evaluate(float t){ if(colorKeys.Length==0)return Color.white; Color c=colorKeys[colorKeys.Length-1].color; for(int i=1;i<colorKeys.Length;i++)if(t<=colorKeys[i].time){c=Color.Lerp(colorKeys[i-1].color,colorKeys[i].color,Mathf.InverseLerp(colorKeys[i-1].time,colorKeys[i].time,t));break;} c.a=alphaKeys.Length>0?alphaKeys[alphaKeys.Length-1].alpha:1;for(int i=1;i<alphaKeys.Length;i++)if(t<=alphaKeys[i].time){c.a=Mathf.Lerp(alphaKeys[i-1].alpha,alphaKeys[i].alpha,Mathf.InverseLerp(alphaKeys[i-1].time,alphaKeys[i].time,t));break;}return c;} }
 public enum ParticleSystemGradientMode {Color,TwoColors,Gradient,TwoGradients,RandomColor}
 public class ParticleSystem:Component { public MainModule main=new MainModule(); public ColorOverLifetimeModule colorOverLifetime=new ColorOverLifetimeModule(); public class MainModule {public MinMaxGradient startColor;} public class ColorOverLifetimeModule {public bool enabled;public MinMaxGradient color;} public struct MinMaxGradient {public ParticleSystemGradientMode mode; public Color color,colorMin,colorMax;public Gradient gradient,gradientMin,gradientMax;} }
}
namespace HollowSaint { public static class HsPalette {public static UnityEngine.Color ArcCyan=>new UnityEngine.Color(.3f,.92f,1); public static UnityEngine.Color OuterCyan=>new UnityEngine.Color(.1f,.5f,.8f);} }
namespace HollowSaint.FoundationKit {public static class KitUtil { public static bool IsHollowSaint(RoR2.CharacterBody b)=>b!=null; } }
namespace HollowSaint.FoundationKit.Gaze {
 public static class GazeTuning {public const float ReachStart=1,ReachEnd=1,SplashRadius=4,Radius=1.5f;}
 public static class GazeLaunchDurationPolicy {public static float Progress(float age,float duration)=>UnityEngine.Mathf.Clamp01(age/duration);}
 public static class GazeServer {public struct Impact {public UnityEngine.Vector3 Point;}}
}
