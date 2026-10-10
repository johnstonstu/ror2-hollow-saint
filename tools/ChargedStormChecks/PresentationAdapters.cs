using System;
using UnityEngine;
using RoR2;

namespace HollowSaint
{
    public static class Plugin { public static LogAdapter Log = new(); }
    public class LogAdapter { public void LogWarning(string text) {} public void LogError(string text) {} }
    internal static class KitRegistration { internal const string CrownMachineName = "Crown"; }
}
namespace HollowSaint.FoundationKit
{
    public static class KitTuning { public static int StormChargeMax = 5; public static float OpenCircuitBuffSeconds=10, OpenCircuitPulseInterval=.5f;
        public static float ArcBoltInterval=.5f,ArcBoltReleaseNormalizedTime=.21f,ArcBoltMinGestureSeconds=.1f,ArcBoltProjectileSpeed=120f,ArcBoltDamageCoefficient=1.6f,ArcBoltRadius=.75f; }
    public static class KitContent
    { static int nextBuff; public static BuffDef MakeBuff(string name,Color color,bool canStack,bool isDebuff,bool hidden,string icon=null) => new() { buffIndex=++nextBuff,canStack=canStack }; }
    public static class KitAnim
    { public const string OverlayLayer = "Overlay"; public static bool UpperBodyIdle(CharacterBody b) => true; public static void PlayOnBody(CharacterBody b,string layer,string state,float time) {} public static void PlayGesture(CharacterBody b,object animator,string state,float duration) {} public static void PlayBoltGesture(CharacterBody b,object animator,string state,float duration) {} }
    public static class KitLog { public static void Event(string name,string detail="") {} }
    public static class KitUtil { public static void ReportHit(DamageInfo info,GameObject victim) {} public static Vector3 EyePosition(CharacterBody body) => body.corePosition + Vector3.up; }
}
namespace HollowSaint.FoundationKit.Storm
{
    public static class StormServer
    {
        public static int Depth; public static void BeginStormDamage() { Depth++; } public static void EndStormDamage() { Depth--; }
        // Priming is recorded outside the damage scope: it is not a damage report and never Electrocutes.
        public static readonly System.Collections.Generic.List<(HealthComponent victim, float amount)> primes = new();
        public static readonly System.Collections.Generic.List<CharacterBody> shocks = new();
        internal static void PrimeStatic(HealthComponent victim, CharacterBody attacker, float amount)
        { if (Depth != 0) throw new System.Exception("Priming inside storm damage scope."); if (victim != null && amount > 0f) primes.Add((victim, amount)); }
        internal static void Shock(CharacterBody body) { if (body != null) shocks.Add(body); }
    }
}
namespace HollowSaint.FoundationKit.Gaze
{
    public class GazeState : EntityStates.EntityState { }
    public class GazeFuelController : MonoBehaviour
    { public void ReserveChanged() {} public static bool OwnsPresentation(CharacterBody body) => false; }
}
namespace HollowSaint.FoundationKit.Vfx
{
    public enum Beat { MeterFull, ChargeTick, CircuitUnfold, ArcBoltCast }
    public static class BodyCurrentFx { public static int released,cancelled; public static void PulseCore(CharacterBody body,float duration) {}
        public static uint BeginArm(CharacterBody body,bool left,float duration,float release) => 1;
        public static void ReleaseArm(CharacterBody body,bool left,uint token) { released++; }
        public static void CancelArm(CharacterBody body,bool left,uint token) { cancelled++; } }
    public static class DischargeLink { public static void Play(CharacterBody body,bool left,Vector3 direction,float speed) {} }
    public static class KitFx { public static void Local(Beat beat,CharacterBody body,Vector3 point) {} public static Vector3 Socket(CharacterBody b,string name) => Vector3.zero; }
}
namespace HollowSaint.FoundationKit.Stormspear
{
    public static class StormspearCharge { public static bool crown; public static bool InCrown(CharacterBody body) => crown; }
    public static class StormspearTuning { public static float CrownSpearHeight = .55f; }
    public class StormspearChargeState : EntityStates.EntityState { }
    public class StormspearThrowState : EntityStates.EntityState { public bool CooldownReleased; }
    public static class StormspearRegistration { public const string MachineName = "Spear"; }
}
namespace HollowSaint.FoundationKit.ArcStep { public class ArcStepState : EntityStates.EntityState {} }
namespace HollowSaint.FoundationKit.SpearDischarge { public static class SpearCarry { public static bool left=true; public static bool NetworkHandOf(CharacterBody owner) => left; } }
namespace HollowSaint.FoundationKit.ChargedStorm
{
    public class StoredChargeDriver : MonoBehaviour
    {
        uint next; public bool canPrepare = true, failLaunch; public int launches, spent;
        public uint NextToken() => ++next;
        public void RememberToken(uint token) { if(token>next)next=token; }
        public static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        public bool Prepare(byte kind,int count,Vector3 direction) => canPrepare;
        public int dismissals; public int DismissClouds() { dismissals++; return 1; }
        public float Launch(byte kind,uint cast,int count,Vector3 direction) { launches++; spent=count; if(failLaunch) throw new Exception("Synthetic launch failure"); return kind==2 ? 1.208333f : kind==1 ? .3f : .45f; }
    }
    public class StoredChargeChargeFx : UnityEngine.Object
    { public static int confirmed; public static StoredChargeChargeFx Begin(CharacterBody body,byte kind,int count) => new(); public void Gather(int count) {} public void Confirm(int count) { confirmed=count; } public void End(bool fired) {} }
    public class StoredChargeHover
    { public static int held; public void Begin(CharacterBody body,bool authority,Vector3 aim) { held++; } public void Tick(CharacterBody body,bool authority,float age) {} public void Freeze(CharacterBody body) {} public void End(CharacterBody body) { held--; } }
    public static class ChargedStormEffects
    {
        public static System.Collections.Generic.List<(Vector3 point,Vector3 sky)> strikes = new();
        public static void Cloud(CharacterBody owner,Vector3 sky,float radius,float duration) {}
        public static int cloudDismissals; public static void CloudDismiss(CharacterBody owner,Vector3 sky) { cloudDismissals++; }
        public static System.Collections.Generic.List<int> strikeStrokes = new();
        public static void Strike(CharacterBody owner,Vector3 sky,Vector3 point,float radius,HealthComponent victim,Vector3 center,int strokes=4) { strikes.Add((point,sky)); strikeStrokes.Add(strokes); }
        public static System.Collections.Generic.List<(Vector3 point,float radius)> bursts = new();
        public static void OrbBurst(CharacterBody owner,Vector3 point,float radius) { bursts.Add((point,radius)); }
        public static float lastOrbSpeed;
        public static HealthComponent lastOrbTarget;
        public static void Orb(CharacterBody owner,Vector3 from,Vector3 to,float diameter,float speed,HealthComponent target,uint flight,bool launched=false) { lastOrbSpeed=speed; lastOrbTarget=target; }
        public static void OrbImpact(CharacterBody owner,Vector3 point,float diameter,uint flight,bool sound=true) {}
    }
}
