namespace UnityEngine
{
    public class Object {public static implicit operator bool(Object o)=>o!=null;}
    public class GameObject:Object {}
    public class MonoBehaviour:Object
    {
        public static RoR2.CharacterBody SelectedBody;
        public static HollowSaint.FoundationKit.Stormspear.StormspearCharge Charge;
        public GameObject gameObject=new();public bool isActiveAndEnabled=true;
        protected T GetComponent<T>()where T:class => typeof(T)==typeof(RoR2.CharacterBody)?SelectedBody as T:Charge as T;
    }
    public class DisallowMultipleComponent:Attribute {}
    public static class Time {public static float time;}
    public static class Mathf {public static float Clamp01(float value)=>Math.Clamp(value,0,1);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int value,int min,int max)=>Math.Clamp(value,min,max);}
}
namespace RoR2
{
    public class CharacterBody:UnityEngine.Object {public HealthComponent healthComponent=new();public bool HasBuff(object buff)=>false;public T GetComponent<T>()where T:class=>UnityEngine.MonoBehaviour.Charge as T;}
    public class HealthComponent:UnityEngine.Object {public bool alive=true;}
    public class Stage:UnityEngine.Object {public static Stage instance=new();}
    public static class Util
    {
        public static List<(uint id,string name)> Sounds=new();public static uint Next=1;public static bool Throw;
        public static uint PlaySound(string name,UnityEngine.GameObject source){if(Throw)throw new Exception("synthetic cosmetic failure");uint id=Next++;Sounds.Add((id,name));return id;}
    }
}
namespace UnityEngine.Networking {public static class NetworkServer {public static bool active=true;}public static class NetworkClient {public static bool active=true;}}
namespace HollowSaint {public static class Plugin {public static Logger Log=new();}public class Logger {public int Warnings;public void LogWarning(string value){Warnings++;}}}
namespace HollowSaint.FoundationKit.Vfx {public static class CustomSoundBank {public static bool Ready=true;}}
namespace HollowSaint.FoundationKit.OpenCircuit {public static class OpenCircuitBuff {public static UnityEngine.Object Def;}}
namespace HollowSaint.FoundationKit.Stormspear {public static class StormspearTuning {public static float HandReleaseDelay=.12f;}}
public static class AkSoundEngine {public static List<uint> Stops=new();public static void StopPlayingID(uint id,int fade){if(fade!=30)throw new Exception("wrong stop fade");Stops.Add(id);}}
