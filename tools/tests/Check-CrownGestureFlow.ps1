# Success: production crown recovery preserves queued/incoming/active casts,
# yields to Gaze and Arc Step, and permits normal crown recovery and migration.
# Crown-layer success: only the memo's live bolt is owned; queued replacements,
# canceled owners and migrated bolts keep correct ownership, timing and fades.
# Lightweight Animator/game adapters; this does not verify native pose rendering.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$crown = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\OpenCircuit\CrownGestureFlow.cs'))
$weights = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Animation\FoundationLayerWeights.cs'))
$shared = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Animation\KitAnim.cs'))
$first = $shared.IndexOf('public static bool MoveGesture(')
$last = $shared.IndexOf('public static Animator AnimatorOf(', $first)
if ($first -lt 0 -or $last -lt 0) { throw 'Gesture method boundaries changed' }
$move = 'namespace HollowSaint.FoundationKit { public static partial class KitAnim {' + $shared.Substring($first, $last - $first) + '} }'
$adapters = @'
using System;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.OpenCircuit;
namespace UnityEngine {
    public class Object { public static implicit operator bool(Object value) { return value != null; } }
    public class GameObject : Object { }
    public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) {} }
    public class DisallowMultipleComponent : Attribute {}
    public class MonoBehaviour : Object {
        public bool enabled=true, isActiveAndEnabled=true;
        public Animator TestAnimator; public CharacterBody TestBody;
        public T GetComponent<T>() where T : class { return TestAnimator as T; }
        public T GetComponentInParent<T>() where T : class { return TestBody as T; }
    }
    public static class Time { public static float time, deltaTime; }
    public static class Mathf {
        public static float Repeat(float t,float length) { return t % length; }
        public static float Min(float a,float b) { return Math.Min(a,b); }
        public static float Max(float a,float b) { return Math.Max(a,b); }
        public static float MoveTowards(float value,float target,float step) { return value < target ? Min(value+step,target) : Max(value-step,target); }
    }
    public struct AnimatorStateInfo { public int shortNameHash; public bool loop; public float normalizedTime, length, speed, speedMultiplier; }
    public struct AnimatorTransitionInfo { public float normalizedTime; }
    public class Animator : Object {
        public int layerCount = 2;
        public bool isActiveAndEnabled=true; public float Rate=1f, ResumeTime, ResumeFade;
        public int[] Current = { StringToHash("Empty"), StringToHash("Empty") }, Next = new int[2], Pending = new int[2];
        public float[] Position = { .4f,.4f };
        public bool[] Transition = new bool[2]; public float[] Weight = new float[2]; public int Writes;
        public static int StringToHash(string name) { return name.GetHashCode(); }
        public int GetLayerIndex(string name) { return name == "UpperBody" ? 0 : name == "UpperArms" ? 1 : -1; }
        public bool HasState(int layer,int state) { return true; }
        public bool IsInTransition(int layer) { return Transition[layer]; }
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layer) { return new AnimatorStateInfo { shortNameHash=Current[layer], normalizedTime=Position[layer], loop=true }; }
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layer) { return new AnimatorStateInfo { shortNameHash=Next[layer] }; }
        public float GetLayerWeight(int layer) { return Weight[layer]; }
        public void SetLayerWeight(int layer,float weight) { Weight[layer]=weight; }
        public float GetFloat(int hash) { return Rate; }
        public void SetFloat(int hash,float value) { Rate=value; }
        public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layer) { return new AnimatorTransitionInfo(); }
        public void Play(int state,int layer,float at) { Writes++; Next[layer]=state; }
        public void CrossFade(int state,float fade,int layer,float at) { Writes++; Next[layer]=state; }
        public void CrossFadeInFixedTime(int state,float fade,int layer) { Writes++; Next[layer]=state; }
    }
}
namespace RoR2 {
    public class HealthComponent : UnityEngine.Object { public bool alive=true; }
    public class CharacterBody : UnityEngine.Object {
        public GameObject gameObject=new GameObject(); public HealthComponent healthComponent=new HealthComponent();
        public Animator Animator=new Animator(); public bool Dashing;
        public InputBank inputBank=new InputBank();
        public T GetComponentInChildren<T>() where T : class { return null; }
    }
    public class InputBank : UnityEngine.Object { public Button skill1=new Button(); }
    public class Button { public bool down; }
    public class EntityStateMachine : UnityEngine.Object {
        public object state; public static EntityStateMachine Crown;
        public static EntityStateMachine FindByCustomName(GameObject body,string name) { return Crown; }
    }
}
namespace HollowSaint.FoundationKit {
    public static class KitRegistration { public const string CrownMachineName="Crown"; }
    public static partial class KitAnim {
        public const string UpperBodyLayer="UpperBody", UpperArmsLayer="UpperArms", OverlayLayer="Overlay", SpearCarryLayer="SpearCarry", PlaybackRateParam="attackSpeed";
        private static readonly int EmptyHash=Animator.StringToHash("Empty"); public static int Recoveries, Stops;
        public static Animator AnimatorOf(CharacterBody b) { return b == null ? null : b.Animator; }
        public static int PendingState(Animator a,int layer) { return a.Pending[layer]; }
        public static int BoltOverHoldLayer=-1;
        public static bool BoltOverHold(CharacterBody b,string layer) { return b.Animator.GetLayerIndex(layer)==BoltOverHoldLayer; }
        public static bool HasState(Animator a,string layer,string state) { return true; }
        public static float ClipLength(Animator a,string state) { return 1.2f; }
        public static bool Play(Animator a,string layer,string state,float duration) {
            a.Rate=1.2f/duration; MarkPending(a,a.GetLayerIndex(layer),Animator.StringToHash(state)); return true;
        }
        public static void ResumeHold(Animator a,int layer,int hash,float at,float clip,float fade) {
            a.Writes++; a.ResumeTime=at; a.ResumeFade=fade; MarkPending(a,layer,hash);
        }
        private static void MarkPending(Animator a,int layer,int hash) { a.Pending[layer]=hash; }
        public static void Stop(CharacterBody b,string layer) { Stops++; MarkPending(b.Animator,b.Animator.GetLayerIndex(layer),EmptyHash); }
        public static void PlayGestureOnBody(CharacterBody b,string state,float duration) { Recoveries++; MarkPending(b.Animator,0,Animator.StringToHash(state)); }
    }
}
namespace HollowSaint.FoundationKit.ArcStep { public static class ArcStepState { public static bool IsBodyDashing(CharacterBody b) { return b.Dashing; } } }
namespace HollowSaint.FoundationKit.Gaze { public class GazeState {} }
namespace HollowSaint.FoundationKit.ChargedStorm { public class StoredChargeChargeFx : UnityEngine.Object {} }
namespace HollowSaint.FoundationKit.ArcBolt { public static class ArcBoltState { public const string StateLeft="Arc Bolt left", StateRight="Arc Bolt right"; } }
namespace HollowSaint.FoundationKit.OpenCircuit {
    public static class OpenCircuitTuning {
        public const string CastArmsState="Open Circuit arms", HoldArmsState="Open Circuit arms hold", EndAnimState="Open Circuit end";
        public const float EndClipSeconds=0.875f;
    }
}
'@
$checks = @'
namespace HollowSaint.FoundationKit {
    public static class CrownGestureChecks {
        private static int count;
        private static void Check(bool ok,string why) { count++; if (!ok) throw new Exception(why); }
        private static CharacterBody Body(string state) {
            KitAnim.Recoveries=KitAnim.Stops=0; KitAnim.BoltOverHoldLayer=-1; EntityStateMachine.Crown=null;
            var b=new CharacterBody(); b.Animator.Current[0]=Animator.StringToHash(state); return b;
        }
        private static void Tick(FoundationLayerWeights weights) {
            typeof(FoundationLayerWeights).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(weights,null);
        }
        private static FoundationLayerWeights Weights(CharacterBody body) {
            var w=new FoundationLayerWeights { TestAnimator=body.Animator, TestBody=body };
            typeof(FoundationLayerWeights).GetMethod("Start",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(w,null);
            EntityStateMachine.Crown=new EntityStateMachine { state=new Gaze.GazeState() };
            Time.time=0f; Time.deltaTime=.02f; return w;
        }
        private static void LayerChecks() {
            foreach (int layer in new[] { 0,1 }) {
                var b=Body("Empty"); var w=Weights(b); var a=b.Animator;
                string own=layer==0 ? KitAnim.UpperBodyLayer : KitAnim.UpperArmsLayer;
                string other=layer==0 ? KitAnim.UpperArmsLayer : KitAnim.UpperBodyLayer;
                a.Current[layer]=Animator.StringToHash(OpenCircuitTuning.CastArmsState);
                a.Rate=.4f; string played;
                Check(w.TryPlayBoltOverHold(b,"Arc Bolt right",.5f,out played) && played==own,"Bolt failed to reuse crown layer");
                Check(w.BoltOverHoldActive(own) && !w.BoltOverHoldActive(other),"Memo owns unrelated arm layer");
                a.Pending[layer]=Animator.StringToHash("Stormspear throw");
                Check(!w.BoltOverHoldActive(own),"Memo owns a queued replacement");
                int hold; float holdTime;
                Check(w.PinTarget(out hold,out holdTime)==0,"Queued replacement retains the crown arm pin");
                Tick(w); Check(a.Writes==0,"Memo resumes over queued replacement");

                b=Body("Empty"); w=Weights(b); a=b.Animator;
                a.Current[layer]=Animator.StringToHash(OpenCircuitTuning.HoldArmsState);
                Check(w.TryPlayBoltOverHold(b,"Arc Bolt left",.5f,out played),"Hold-loop bolt rejected");
                a.Pending[layer]=Animator.StringToHash("Empty"); a.Pending[1-layer]=Animator.StringToHash("Arc Bolt left");
                Check(!w.BoltOverHoldActive(own) && w.BoltOverHoldActive(other),"Migration loses layer ownership");
                Check(w.PinTarget(out hold,out holdTime)==-1,"Migration drops the off-arm pin before Update");
                Tick(w); a.Pending[0]=a.Pending[1]=0; a.Current[layer]=Animator.StringToHash("Empty");
                a.Current[1-layer]=Animator.StringToHash("Arc Bolt left"); a.Position[1-layer]=.71f; Time.time=.4f;
                Tick(w);
                Check(a.Writes==1 && a.Pending[1-layer]==Animator.StringToHash(OpenCircuitTuning.HoldArmsState),"Migrated bolt resumes wrong layer");
                Check(Math.Abs(a.ResumeTime-(.4f+.4f/1.2f))<.001f && Math.Abs(a.ResumeFade-.15f)<.001f,"Hold resumption loses elapsed time or authored fade");
            }
            var body=Body(OpenCircuitTuning.CastArmsState); var weights=Weights(body); string name;
            weights.TryPlayBoltOverHold(body,"Arc Bolt right",.5f,out name);
            body.Animator.Pending[0]=0; body.Animator.Current[0]=Animator.StringToHash("Arc Bolt right"); body.Animator.Position[0]=.9f;
            EntityStateMachine.Crown.state=new object(); Tick(weights);
            Check(body.Animator.Writes==0 && !weights.BoltOverHoldActive(KitAnim.UpperBodyLayer),"Canceled owner resurrects crown hold");
            body=Body(OpenCircuitTuning.HoldArmsState); weights=Weights(body);
            weights.TryPlayBoltOverHold(body,"Arc Bolt right",.5f,out name);
            body.Animator.Pending[0]=0; body.Animator.Current[0]=Animator.StringToHash("Empty");
            int hash; float at;
            Check(weights.PinTarget(out hash,out at)==1,"Natural bolt exit loses the hold during a long frame");
            Tick(weights); Check(body.Animator.Writes==1,"A skipped resume window leaves the crown lowered");
            body=Body("Empty"); weights=Weights(body); body.Animator.Weight[0]=1f; body.inputBank.skill1.down=true;
            weights.NoteBolt(body,KitAnim.UpperBodyLayer,.5f); Time.time=.53f; Tick(weights);
            Check(body.Animator.Weight[0]==1f,"Primary stream fades before the next bolt");
            body.inputBank.skill1.down=false; Tick(weights);
            Check(body.Animator.Weight[0]<1f,"Released Primary retains stream weight");
        }
        public static string Run() {
            LayerChecks();
            foreach (string replacement in new[] { "Arc Bolt right", "Arc Bolt left", "Conduit Spear", "Stormspear throw" }) {
                var b=Body(OpenCircuitTuning.HoldArmsState); b.Animator.Pending[0]=Animator.StringToHash(replacement);
                CrownGestureFlow.Recover(b); CrownGestureFlow.Cancel(b);
                Check(KitAnim.Recoveries==0 && KitAnim.Stops==0 && b.Animator.Pending[0]==Animator.StringToHash(replacement), "Crown overwrites queued " + replacement);
                b=Body(OpenCircuitTuning.HoldArmsState); b.Animator.Transition[0]=true; b.Animator.Next[0]=Animator.StringToHash(replacement);
                CrownGestureFlow.Recover(b); CrownGestureFlow.Cancel(b);
                Check(KitAnim.Recoveries==0 && KitAnim.Stops==0, "Crown overwrites incoming " + replacement);
                b=Body(replacement); CrownGestureFlow.Recover(b);
                Check(KitAnim.Recoveries==0, "Crown overwrites active " + replacement);
                b=Body(OpenCircuitTuning.HoldArmsState); b.Animator.Pending[1]=Animator.StringToHash(replacement); CrownGestureFlow.Recover(b);
                Check(KitAnim.Recoveries==0, "Recovery ignores other arm layer " + replacement);
            }
            // A bolt temporarily owned by the crown permits recovery on its own layer only.
            foreach (int boltLayer in new[] { 0,1 }) {
                var b=Body("Empty"); KitAnim.BoltOverHoldLayer=boltLayer;
                b.Animator.Pending[boltLayer]=Animator.StringToHash("Arc Bolt right");
                CrownGestureFlow.Recover(b);
                Check(KitAnim.Recoveries==1,"Crown-owned bolt blocks recovery on layer " + boltLayer);
                b=Body("Empty"); KitAnim.BoltOverHoldLayer=boltLayer;
                b.Animator.Pending[boltLayer]=Animator.StringToHash("Arc Bolt right");
                int other=1-boltLayer, replacement=Animator.StringToHash("Stormspear throw");
                b.Animator.Pending[other]=replacement;
                CrownGestureFlow.Recover(b); CrownGestureFlow.Cancel(b);
                Check(KitAnim.Recoveries==0 && KitAnim.Stops==1 && b.Animator.Pending[other]==replacement,
                    "Crown-owned bolt cancels an unrelated gesture on layer " + other);
            }
            foreach (string state in new[] { "Empty", OpenCircuitTuning.CastArmsState, OpenCircuitTuning.HoldArmsState }) {
                var b=Body(state); CrownGestureFlow.Recover(b); Check(KitAnim.Recoveries==1, "Normal recovery omitted for " + state);
            }
            foreach (string state in new[] { OpenCircuitTuning.CastArmsState, OpenCircuitTuning.HoldArmsState }) {
                var b=Body(state); CrownGestureFlow.Cancel(b);
                Check(KitAnim.Stops==1 && b.Animator.Pending[0]==Animator.StringToHash("Empty"), "Canceled startup/hold survives: " + state);
            }
            var dying=Body(OpenCircuitTuning.HoldArmsState); dying.healthComponent.alive=false; CrownGestureFlow.Recover(dying);
            Check(KitAnim.Recoveries==0 && KitAnim.Stops==1,"Death plays recovery or retains hold");
            var dash=Body(OpenCircuitTuning.HoldArmsState); dash.Dashing=true; CrownGestureFlow.Recover(dash);
            Check(KitAnim.Recoveries==0 && KitAnim.Stops==1,"Dash cancel obscured by recovery");
            var gaze=Body(OpenCircuitTuning.HoldArmsState); EntityStateMachine.Crown=new EntityStateMachine { state=new Gaze.GazeState() }; CrownGestureFlow.Recover(gaze);
            Check(KitAnim.Recoveries==0 && KitAnim.Stops==0,"Buff expiry lowers Gaze hold");
            foreach (int layer in new[] { 0,1 }) {
                var b=Body(OpenCircuitTuning.HoldArmsState); b.Animator.Pending[layer]=Animator.StringToHash("Arc Bolt right");
                Check(!KitAnim.MoveGesture(b.Animator,0,1,0.15f) && b.Animator.Writes==0,"Migration replaces pending cast on layer " + layer);
            }
            var migrating=Body(OpenCircuitTuning.HoldArmsState);
            Check(KitAnim.MoveGesture(migrating.Animator,0,1,0.15f) && migrating.Animator.Writes==2,"Settled gesture fails normal migration");
            return "CROWN_GESTURE_FLOW_PASS: " + count + " production ownership/recovery/migration checks with explicit adapters.";
        }
    }
}
'@
$crown = [regex]::Replace($crown, '(?m)^using [^;]+;\r?\n', '')
$weights = [regex]::Replace($weights, '(?m)^using [^;]+;\r?\n', '')
Add-Type -TypeDefinition (($adapters, $crown, $move, $weights, $checks) -join [Environment]::NewLine)
[HollowSaint.FoundationKit.CrownGestureChecks]::Run()
