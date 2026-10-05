using System.Reflection;
using HollowSaint.FoundationKit.Gaze.Fx;
using HollowSaint.FoundationKit.Vfx;
using UnityEngine;

static class Program
{
 static int checks;
 static void Check(bool pass,string name){checks++;if(!pass)throw new Exception(name);}
 static void Reset(){typeof(GazeContrastAssets).GetField("loaded",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,false);typeof(GazeContrastAssets).GetField("<Outline>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,null);}
 static int Main()
 {
  try
  {
   Shader.Throw=true;Reset();GazeContrastAssets.Load();
   Check(GazeContrastAssets.Glow && GazeContrastAssets.Core && !GazeContrastAssets.Outline,"missing legacy shader retains core/glow and suppresses only optional outline");
   Check(HollowSaint.Plugin.Log.Warnings==1,"failed optional lookup logs once");
   var core=GazeContrastAssets.Core;
   for(int cast=0;cast<1000;cast++){GazeContrastAssets.Load();Check(ReferenceEquals(core,GazeContrastAssets.Core),"repeat casts do not retry/reallocate after optional shader failure");}
   Check(Shader.Loads==1,"optional missing shader is not retried per cast");
   Check(VfxAssets.ArcCore.name=="OriginalCore" && VfxAssets.ArcCore.renderQueue==2000,"shared source material remains unchanged");
   Shader.Throw=false;Shader.Missing=true;Reset();GazeContrastAssets.Load();Check(!GazeContrastAssets.Outline && GazeContrastAssets.Core,"null shader is cosmetic");
   Shader.Missing=false;Reset();GazeContrastAssets.Load();Check(GazeContrastAssets.Outline && GazeContrastAssets.Outline.renderQueue==3098,"available alpha shader retains original ink outline");
   VfxAssets.ArcGlow=null;Reset();GazeContrastAssets.Load();Check(!GazeContrastAssets.Glow && GazeContrastAssets.Core,"missing optional glow does not discard core");
   Console.WriteLine($"PASS {checks} production Gaze optional-asset failure assertions; native rendering pending");return 0;
  }
  catch(Exception error){Console.Error.WriteLine(error);return 1;}
 }
}

namespace UnityEngine
{
 public class Object {public static implicit operator bool(Object o)=>o!=null;}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new(1,1,1);public static Color Lerp(Color a,Color b,float t)=>a;}
 public enum TextureFormat {RGBA32}public enum TextureWrapMode {Clamp}
 public class Texture2D:Object {public static Texture2D whiteTexture=new(1,1,TextureFormat.RGBA32,false);public string name;public TextureWrapMode wrapMode;public Texture2D(int w,int h,TextureFormat f,bool mip){}public void SetPixel(int x,int y,Color c){}public void Apply(bool a,bool b){}}
 public class Shader:Object {public static bool Throw,Missing;public static int Loads;public static Shader Find(string name)=>throw new Exception("Unsupported name lookup must not be used");}
 public class Material:Object {public string name;public int renderQueue=2000;public Material(Material source){name=source.name;}public Material(Shader shader){}public bool HasProperty(string name)=>true;public void SetColor(string name,Color value){}public void SetTexture(string name,Texture2D value){}}
}
namespace UnityEngine.AddressableAssets
{
 public static class Addressables {public static Operation<T> LoadAssetAsync<T>(string key){if(key!="94d33eec5bbd5c141b95960873e4d0cc")throw new Exception("wrong native shader reference");return new();}}
 public class Operation<T> {public T WaitForCompletion(){UnityEngine.Shader.Loads++;if(UnityEngine.Shader.Throw)throw new Exception("Addressable shader unavailable");return UnityEngine.Shader.Missing?default:(T)(object)new UnityEngine.Shader();}}
}
namespace RoR2BepInExPack.GameAssetPathsBetter {public static class RoR2_Base_Shaders {public const string ParticleSimpleAlpha_switch_shader="94d33eec5bbd5c141b95960873e4d0cc";}}
namespace HollowSaint {public static class Plugin {public static Logger Log=new();}public class Logger {public int Warnings;public void LogWarning(string text){Warnings++;}}}
namespace HollowSaint.FoundationKit.Vfx
{
 public static class VfxAssets {public static Material ArcGlow=new(new Shader()){name="OriginalGlow"};public static Material ArcCore=new(new Shader()){name="OriginalCore"};public static void Load(){}}
 public static class CrimsonMasteryVisuals {public static Color Pulse, PulseEdge;}
}
