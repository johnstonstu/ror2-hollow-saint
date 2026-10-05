namespace UnityEngine
{
    public class Object {public static implicit operator bool(Object o)=>o!=null;}
    public class GameObject:Object {public bool activeInHierarchy=true;}
    public class MonoBehaviour:Object {public static RoR2.CharacterBody Body;public GameObject gameObject=new();public bool enabled=true;protected T GetComponent<T>()where T:class=>Body as T;}
    public class DisallowMultipleComponent:Attribute {}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float yMax=>y+height;}
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new(1,1,1);}
    public class Canvas:Object {public Rect pixelRect=new(0,0,1000,800);}
    public class Texture2D {public static Texture2D whiteTexture=new();}
    public enum EventType {Layout,Repaint}
    public class Event {public static Event current=new(){type=EventType.Repaint};public EventType type;}
    public enum TextAnchor {MiddleCenter,MiddleLeft,MiddleRight}
    public enum FontStyle {Normal,Bold}
    public class GUIStyleState {public Color textColor;}
    public class GUIStyle {public GUIStyle(){}public GUIStyle(GUIStyle style){}public TextAnchor alignment;public int fontSize;public FontStyle fontStyle;public GUIStyleState normal=new();}
    public class GUISkin {public GUIStyle label=new();}
    public static class GUI {public static GUISkin skin=new();public static Color color;public static List<Rect> Rects=new();public static List<string> Labels=new();public static string LastLabel;public static void DrawTexture(Rect rect,Texture2D texture)=>Rects.Add(rect);public static void Label(Rect rect,string text,GUIStyle style){LastLabel=text;Labels.Add(text);}}
    public static class Screen {public static int height=800;}
    public static class Time {public static float unscaledTime;}
    public static class Mathf {public static float Ceil(float n)=>(float)Math.Ceiling(n);public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp(float v,float min,float max)=>Math.Clamp(v,min,max);public static int Clamp(int v,int min,int max)=>Math.Clamp(v,min,max);public static int RoundToInt(float v)=>(int)Math.Round(v);}
    public struct Vector3 {}
    public class Transform {public Vector3 position;}
}
namespace RoR2
{
    public class HealthComponent:UnityEngine.Object {public bool alive=true;}
    public class CharacterBody:UnityEngine.Object {public HealthComponent healthComponent=new();public UnityEngine.GameObject gameObject=new();}
    public class LocalUser {public CharacterBody cachedBody;public CameraRigController cameraRigController;}
    public class CameraRigController:UnityEngine.Object {public CharacterBody targetBody;public UnityEngine.Transform transform=new();}
    public static class LocalUserManager {public static List<LocalUser> readOnlyLocalUsersList=new();}
    public static class ShakeEmitter {public static int Count;public static float Amplitude;public static void CreateSimpleShakeEmitter(UnityEngine.Vector3 p,Wave wave,float duration,float radius,bool decay){Count++;Amplitude=wave.amplitude;}}
    public class EntityStateMachine:UnityEngine.Object {public static EntityStateMachine Machine;public object state;public static EntityStateMachine FindByCustomName(UnityEngine.GameObject go,string name)=>Machine;}
    public static class Language {public static string GetString(string token)=>"Gaze";}
    public static class Util {public static List<string> Sounds=new();public static void PlaySound(string name,UnityEngine.GameObject source)=>Sounds.Add(name);}
}
namespace RoR2.UI
{
    public class HUD:UnityEngine.Object {public static List<HUD> readOnlyInstanceList=new();public static BoolConVar cvHudEnable=new();public RoR2.LocalUser localUserViewer;public UnityEngine.GameObject mainContainer=new();public UnityEngine.Canvas mainContainerCanvas=new();}
    public class BoolConVar {public bool value=true;}
}
namespace HollowSaint
{
    public static class KitRegistration {public const string CrownMachineName="Crown";}
    public static class Plugin {public static Logger Log=new();}
    public class Logger {public void LogWarning(string message)=>Console.Error.WriteLine(message);}
}
public struct Wave {public float amplitude,frequency,cycleOffset;}
namespace HollowSaint.FoundationKit.Gaze {internal class GazeState {internal bool TimerVisible;internal bool FuelAdmissionOpen=true;internal float RemainingBeamSeconds;internal float ActualBeamSeconds;internal int SuccessfulLaunches;}}
namespace HollowSaint.FoundationKit.Vfx {internal static class CustomSoundBank {internal static bool Ready=true;}internal static class ImpactFeelSettings {internal static bool Enabled=true;}}
