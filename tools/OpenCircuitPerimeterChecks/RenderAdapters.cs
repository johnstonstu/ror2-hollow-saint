using System;
namespace UnityEngine {
 public class MonoBehaviour:Component {public bool isActiveAndEnabled=true;}
 [AttributeUsage(AttributeTargets.Class)]public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int n){}}
 [AttributeUsage(AttributeTargets.Class)]public class DisallowMultipleComponent:Attribute {}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;} }
 public class Material:Object {}
 public enum LineAlignment {View} public enum LineTextureMode {Stretch}
 namespace Rendering {public enum ShadowCastingMode {Off}}
 public class LineRenderer:Component {public Material sharedMaterial; public bool useWorldSpace,receiveShadows,enabled;public LineAlignment alignment;public LineTextureMode textureMode;public int positionCount;public Rendering.ShadowCastingMode shadowCastingMode;public AnimationCurve widthCurve;public float widthMultiplier;public Color startColor,endColor; public readonly Vector3[] points=new Vector3[17]; public void SetPositions(Vector3[] p){Array.Copy(p,points,p.Length);} }
 public class AnimationCurve {public AnimationCurve(params Keyframe[] k){}}
 public struct Keyframe {public Keyframe(float a,float b){}}
 public static class Time {public static float time,unscaledTime,deltaTime=.02f;}
}
namespace UnityEngine.Networking {public static class NetworkServer {public static bool active;}public static class NetworkClient {public static bool active=true;} }
namespace RoR2 {public class HealthComponent:UnityEngine.Object {public bool alive=true;}public class CharacterModel:UnityEngine.Component {public int invisibilityCount;} }
namespace HollowSaint.FoundationKit {public static class KitTuning {public static float OpenCircuitRadius=8;} }
namespace HollowSaint.FoundationKit.Gaze {public class GazeBeam:UnityEngine.Component {public enum Phase {Idle,Windup,Beam,Ending} public Phase Current;} }
namespace HollowSaint.FoundationKit.Gaze.Fx {public static class GazeContrastAssets {public static UnityEngine.Material Glow=new UnityEngine.Material(),Core=new UnityEngine.Material();public static void Load(){}} }
namespace HollowSaint.FoundationKit.OpenCircuit {public class OpenCircuitPulseDriver:UnityEngine.Component {public bool CrownOpen;} public static class OpenCircuitBuff {public static UnityEngine.Object Def=new UnityEngine.Object();} }
namespace HollowSaint.FoundationKit.Vfx {public class SkinFxPalette {private static readonly SkinFxPalette[] skins={new SkinFxPalette(0),new SkinFxPalette(1),new SkinFxPalette(2),new SkinFxPalette(3),new SkinFxPalette(4),new SkinFxPalette(5)};public UnityEngine.Color Arc,Core,Secondary; private SkinFxPalette(int i){Arc=new UnityEngine.Color(.2f+i*.1f,.9f-i*.1f,1);Core=Arc;Secondary=new UnityEngine.Color(1,.5f,.2f);}public static SkinFxPalette ForBody(RoR2.CharacterBody body)=>skins[body.skinIndex];} }
