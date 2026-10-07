# Success: production crown recovery preserves queued/incoming/active casts,
# yields to Gaze and Arc Step, and permits normal crown recovery and migration.
# Lightweight Animator/game adapters; this does not verify native pose rendering.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$crown = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\OpenCircuit\CrownGestureFlow.cs'))
$shared = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\Character\Animation\KitAnim.cs'))
$first = $shared.IndexOf('public static bool MoveGesture(')
$last = $shared.IndexOf('public static Animator AnimatorOf(', $first)
if ($first -lt 0 -or $last -lt 0) { throw 'Gesture method boundaries changed' }
$move = 'namespace HollowSaint.FoundationKit { public static partial class KitAnim {' + $shared.Substring($first, $last - $first) + '} }'
$adapters = @'
using System;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit.OpenCircuit;
namespace UnityEngine {
    public class Object { public static implicit operator bool(Object value) { return value != null; } }
    public class GameObject : Object { }
    public static class Mathf { public static float Repeat(float t,float length) { return t % length; } public static float Min(float a,float b) { return Math.Min(a,b); } }
    public struct AnimatorStateInfo { public int shortNameHash; public bool loop; public float normalizedTime; }
    public class Animator : Object {
        public int layerCount = 2;
        public int[] Current = { StringToHash("Empty"), StringToHash("Empty") }, Next = new int[2], Pending = new int[2];
        public bool[] Transition = new bool[2]; public float[] Weight = new float[2]; public int Writes;
        public static int StringToHash(string name) { return name.GetHashCode(); }
        public int GetLayerIndex(string name) { return name == "UpperBody" ? 0 : name == "UpperArms" ? 1 : -1; }
        public bool HasState(int layer,int state) { return true; }
        public bool IsInTransition(int layer) { return Transition[layer]; }
        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layer) { return new AnimatorStateInfo { shortNameHash=Current[layer], normalizedTime=0.4f, loop=true }; }
        public AnimatorStateInfo GetNextAnimatorStateInfo(int layer) { return new AnimatorStateInfo { shortNameHash=Next[layer] }; }
        public float GetLayerWeight(int layer) { return Weight[layer]; }
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
    }
    public class EntityStateMachine : UnityEngine.Object {
        public object state; public static EntityStateMachine Crown;
        public static EntityStateMachine FindByCustomName(GameObject body,string name) { return Crown; }
    }
}
namespace HollowSaint.FoundationKit {
    public static class KitRegistration { public const string CrownMachineName="Crown"; }
    public static partial class KitAnim {
        public const string UpperBodyLayer="UpperBody", UpperArmsLayer="UpperArms";
        private static readonly int EmptyHash=Animator.StringToHash("Empty"); public static int Recoveries, Stops;
        public static Animator AnimatorOf(CharacterBody b) { return b == null ? null : b.Animator; }
        public static int PendingState(Animator a,int layer) { return a.Pending[layer]; }
        private static void MarkPending(Animator a,int layer,int hash) { a.Pending[layer]=hash; }
        public static void Stop(CharacterBody b,string layer) { Stops++; MarkPending(b.Animator,b.Animator.GetLayerIndex(layer),EmptyHash); }
        public static void PlayGestureOnBody(CharacterBody b,string state,float duration) { Recoveries++; MarkPending(b.Animator,0,Animator.StringToHash(state)); }
    }
}
namespace HollowSaint.FoundationKit.ArcStep { public static class ArcStepState { public static bool IsBodyDashing(CharacterBody b) { return b.Dashing; } } }
namespace HollowSaint.FoundationKit.Gaze { public class GazeState {} }
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
            KitAnim.Recoveries=KitAnim.Stops=0; EntityStateMachine.Crown=null;
            var b=new CharacterBody(); b.Animator.Current[0]=Animator.StringToHash(state); return b;
        }
        public static string Run() {
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
Add-Type -TypeDefinition (($adapters, $crown, $move, $checks) -join [Environment]::NewLine)
[HollowSaint.FoundationKit.CrownGestureChecks]::Run()
