// Isolated game-state adapters used by the Editor runner. Production sources are copied by
// Prepare-FxValidation.ps1; this is not a game/multiplayer simulation.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("HollowSaint.FxValidation.Editor")]

namespace RoR2
{
    public class HealthComponent : MonoBehaviour { public bool alive = true; }
    public class ModelLocator : MonoBehaviour { public Transform modelTransform; }
    public class CharacterDirection : MonoBehaviour { public Vector3 forward => transform.forward; }
    public class InputBankTest : MonoBehaviour
    {
        public Vector3 aimDirection = Vector3.forward, moveVector;
        public Button skill1, skill2, skill3, skill4, jump;
        public struct Button { public bool down; }
    }
    public class CharacterMotor : MonoBehaviour { public Vector3 velocity; public bool isGrounded = true; public int jumpCount; }
    public class EntityStateMachine : MonoBehaviour
    {
        public string customName = "Weapon"; public object state;
        public static EntityStateMachine FindByCustomName(GameObject root, string name) => root.GetComponents<EntityStateMachine>().FirstOrDefault(e => e.customName == name);
    }
    public static class Util
    {
        public static readonly List<string> Sounds = new List<string>();
        public static uint PlaySound(string sound, GameObject target) { Sounds.Add(sound); return 0; }
    }
    public class BuffDef : ScriptableObject { public int buffIndex; }
    public class CharacterBody : MonoBehaviour
    {
        public static readonly List<CharacterBody> readOnlyInstancesList = new List<CharacterBody>();
        public HealthComponent healthComponent;
        public ModelLocator modelLocator;
        public CharacterDirection characterDirection;
        public InputBankTest inputBank;
        public CharacterMotor characterMotor;
        public uint skinIndex;
        public bool IsSaint = true, Dashing;
        public bool hasEffectiveAuthority = true;
        public bool isSprinting, outOfCombat;
        public Vector3 DashBlend = Vector3.forward;
        public Vector3 corePosition => transform.position + Vector3.up;
        private readonly Dictionary<int, int> buffs = new Dictionary<int, int>();
        public bool HasBuff(BuffDef buff) => GetBuffCount(buff) > 0;
        public int GetBuffCount(BuffDef buff) => buff && buffs.TryGetValue(buff.buffIndex, out var count) ? count : 0;
        public void SetBuffCount(int index, int count) { buffs[index] = count; }
    }
    public class CharacterModel : MonoBehaviour
    {
        public int invisibilityCount;
        public CharacterBody body;
        public RendererInfo[] baseRendererInfos = Array.Empty<RendererInfo>();
        public struct RendererInfo { public Renderer renderer; public Material defaultMaterial; }
    }
    public static class LayerIndex { public static readonly Layer world = new Layer(); public class Layer { public int mask = 1 << 30; } }
}
namespace UnityEngine.Networking { public static class NetworkServer { public static bool active = true; } }
namespace HollowSaint
{
    public static class PreviewFrame { public static float DeltaTime = 1f / 60f; }
    public class FoundationPresentation : MonoBehaviour
    {
        public bool IsPresentingAlive = true, GroundedForPresentation = true;
        public float GlideWeight;
    }
    public static class FoundationContent { public static GameObject SpearModel; }
    public static class Plugin
    {
        public static readonly Logger Log = new Logger();
        public class Logger
        {
            public readonly List<string> Warnings = new List<string>();
            public void LogInfo(string s) { Debug.Log(s); }
            public void LogWarning(string s) { Warnings.Add(s); Debug.LogWarning(s); }
            public void LogError(string s) { Debug.LogError(s); }
        }
    }
}
namespace HollowSaint.FoundationKit
{
    // Native Animator adapter for isolated carry tests, not a simulation of skill states.
    public static partial class KitAnim
    {
        public const string SpearCarryLayer = "SpearCarry", UpperArmsLayer = "UpperArms", UpperBodyLayer = "UpperBody", OverlayLayer = "Overlay";
        public static bool LayerIdle(Animator a, int i) => !a || i < 0 || (a.GetCurrentAnimatorStateInfo(i).IsName("Empty") && !a.IsInTransition(i));
        public static Animator AnimatorOf(RoR2.CharacterBody b) => b && b.modelLocator && b.modelLocator.modelTransform ? b.modelLocator.modelTransform.GetComponent<Animator>() : null;
        public static void Stop(RoR2.CharacterBody b, string layer, float fade)
        { var a = AnimatorOf(b); int i = a ? a.GetLayerIndex(layer) : -1; if (i >= 0) a.CrossFadeInFixedTime("Empty", fade, i); }
        public static bool PlayOnBody(RoR2.CharacterBody b, string layer, string state, float duration)
        { var a = AnimatorOf(b); int i = a ? a.GetLayerIndex(layer) : -1; if (i < 0) return false; a.CrossFadeInFixedTime(state, 0.09f, i); return true; }
    }
    public static class KitUtil
    {
        public static bool IsHollowSaint(RoR2.CharacterBody b) => b && b.IsSaint;
        public static Transform ResolveSocket(RoR2.CharacterBody b, string name)
        {
            if (!b || !b.modelLocator || !b.modelLocator.modelTransform) return null;
            switch (name) { case "Core": name = "core socket"; break; case "Halo": name = "halo socket"; break; case "Chest": name = "chest"; break;
                case "MuzzleLeft": name = "L muzzle"; break; case "MuzzleRight": name = "R muzzle"; break; case "HeelL": name = "L heel socket"; break; case "HeelR": name = "R heel socket"; break; }
            return b.modelLocator.modelTransform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        }
    }
    public static class KitTuning { public static float SpearFanRange = 8f, SpearFanAngle = 60f; }
    public static class KitContent
    {
        private static int next = 20;
        public static RoR2.BuffDef MakeBuff(string name, Color color, bool canStack, bool isDebuff, bool hidden)
        { var buff = ScriptableObject.CreateInstance<RoR2.BuffDef>(); buff.name = name; buff.buffIndex = next++; return buff; }
    }
}
namespace HollowSaint.FoundationKit.ArcBolt { public class ArcBoltState { } }
namespace HollowSaint.FoundationKit.ArcStep
{
    public static class ArcStepState
    {
        public static bool IsBodyDashing(RoR2.CharacterBody b) => b && b.Dashing;
        public static bool TryGetDashBlend(RoR2.CharacterBody b, out Vector3 blend) { blend = b ? b.DashBlend : Vector3.zero; return b; }
    }
}
namespace HollowSaint.FoundationKit.OpenCircuit
{
    public static class OpenCircuitBuff { public static RoR2.BuffDef Def; }
    public static class OpenCircuitVfxHooks { public static Action<RoR2.CharacterBody> UnfoldStarted; }
}
namespace HollowSaint.FoundationKit.SpearDischarge
{
    public static class ConduitSpearAnchor { public static RoR2.BuffDef OutBuff, RecallBuff; }
    public static class ConductorMarkServer { public static RoR2.BuffDef ConductorMarkBuff; }
}
namespace HollowSaint.FoundationKit.Storm { public static class StormServer { public static RoR2.BuffDef StaticBuff, ShockedBuff, ElectrocutedBuff; } }
namespace HollowSaint.FoundationKit.Vfx
{
    public static class KitSfx
    {
        public const string Land = "Land", GlideEnter = "GlideEnter", GlideLoopStart = "GlideLoopStart", GlideLoopStop = "GlideLoopStop", GlideExit = "GlideExit", FootstepRun = "FootstepRun", Footstep = "Footstep";
    }
    public static class HsPalette
    {
        public static readonly Color WhiteHot = new Color(1f, 0.98f, 0.94f), ArcCyan = new Color(0.30f, 0.92f, 1f), OuterCyan = new Color(0.04f, 0.62f, 0.85f);
    }
    public static class KitFx { public static Vector3 Socket(RoR2.CharacterBody b, string name) { var t = KitUtil.ResolveSocket(b, name); return t ? t.position : b.corePosition; } }
    public static class VfxAssets
    {
        public static Material ArcCore, ArcGlow, Spark, Flash, Trail, Ring, Afterimage;
        public static void Load()
        {
            if (ArcCore) return;
            Material Make(string name, Color tint)
            { var m = new Material(Shader.Find("HollowSaint/PreviewLightning")) { name = name }; m.SetColor("_TintColor", tint); return m; }
            ArcCore = Make("Core", HsPalette.WhiteHot * 1.3f); ArcGlow = Make("Glow", HsPalette.ArcCyan);
            Spark = Make("Spark", HsPalette.WhiteHot); Flash = Make("Flash", HsPalette.ArcCyan);
            Trail = Make("Trail", HsPalette.ArcCyan); Ring = Make("Ring", HsPalette.ArcCyan); Afterimage = Make("Afterimage", HsPalette.OuterCyan);
        }
    }
}
