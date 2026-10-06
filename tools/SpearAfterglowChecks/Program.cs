using System;
using System.Reflection;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit.Vfx;
using HollowSaint.FoundationKit.Stormspear.Fx;
static class Program {
 static int assertions;
 static void Check(bool b,string why){assertions++;if(!b)throw new Exception(why);}
 static object Field(object obj,string name)=>obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);
 static Action Method(object obj,string name)=>(Action)Delegate.CreateDelegate(typeof(Action),obj,obj.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance));
 static void Main(){
  var body=new GameObject().AddComponent<CharacterBody>();
  var pool=(SpearAfterglowFx[])typeof(SpearAfterglowFx).GetField("pool",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  Physics.Ground=true;Physics.GroundY=0;
  var pal=SkinFxPalette.ForBody(body);
  Check(!SpearAfterglowFx.Play(new Vector3(0,3,0),Vector3.up,8,pal),"air target cannot paint floor three metres below");
  Check(!SpearAfterglowFx.Play(Vector3.zero,Vector3.right,8,pal),"wall normal cannot make floor ring");
  Check(!SpearAfterglowFx.Play(Vector3.zero,Vector3.up,float.NaN,pal),"invalid radius rejected");
  Check(!SpearAfterglowFx.Play(Vector3.zero,Vector3.zero,8,pal),"unknown surface normal rejected");
  for(uint skin=0;skin<6;skin++)for(int profile=0;profile<=6;profile++)foreach(float radius in new[]{.5f,3f,8f,12f}){
   body.skinIndex=skin;pal=SkinFxPalette.ForBody(body);Physics.Profile=profile;
   Vector3 origin=new Vector3(0,Physics.Height(0,0),0),normal=Physics.Normal(0,0);
   Physics.Rays=Physics.Capsules=0;Check(SpearAfterglowFx.Play(origin,normal,radius,pal),"confirmed nearby surface accepted");
   Check(Physics.Rays<=129&&Physics.Capsules<=128,"bounded one-shot terrain queries");
   var fx=pool[0];var update=Method(fx,"Update");var lines=(LineRenderer[])Field(fx,"glow");var cores=(LineRenderer[])Field(fx,"core");
   int rays=Physics.Rays,caps=Physics.Capsules;Time.time+=.11f;update();int visible=0;
   foreach(var line in lines){if(!line.enabled)continue;visible++;Check(Math.Abs(line.startColor.r-pal.Arc.r)<.001f,"all skins preserve primary glow");foreach(var p in line.points)Check(Vector3.Distance(p,origin)+.04f<=radius,"exact supplied blast radius bounds including width");
    for(int k=1;k<17;k++)for(int n=0;n<5;n++){var p=Vector3.Lerp(line.points[k-1],line.points[k],n/4f);Check(Physics.Exists(p.x,p.z),"no segment bridges ledge");float gap=(p.y-Physics.Height(p.x,p.z))*Physics.Normal(p.x,p.z).y;Check(gap>=.04f&&gap<=.23f,"ground trail hugs profile without clipping");}
   }
   Check(visible>0,"terrain retains valid branches");float firstAlpha=lines[0].startColor.a;
   Time.time+=.3f;update();Check(lines[0].startColor.a<firstAlpha,"lingering electricity fades gradually");
   Time.time+=.23f;update();Check(lines[0].startColor.a<.01f,"tail nearly transparent before expiry");
   Time.time+=.02f;update();foreach(var line in lines)Check(!line.enabled,"all branches expire by .65s");foreach(var line in cores)Check(!line.enabled,"all cores expire");
   Check(Physics.Rays==rays&&Physics.Capsules==caps,"fade performs no terrain resampling");
  }
  Physics.Profile=0;pal=SkinFxPalette.ForBody(body);
  for(int i=0;i<4;i++)Check(SpearAfterglowFx.Play(Vector3.zero,Vector3.up,8,pal),"four event slots available");
  int queries=Physics.Rays;Check(!SpearAfterglowFx.Play(Vector3.zero,Vector3.up,8,pal)&&Physics.Rays==queries,"fifth event drops without queries or allocations");
  foreach(var fx in pool)Method(fx,"OnDisable")();
  var tick=Method(pool[0],"Update");int built=GameObject.Created;long before=GC.GetAllocatedBytesForCurrentThread();
  for(int i=0;i<1000;i++){SpearAfterglowFx.Play(Vector3.zero,Vector3.up,8,pal);Time.time+=.3f;tick();Time.time+=.4f;tick();}
  Check(GC.GetAllocatedBytesForCurrentThread()==before&&GameObject.Created==built,"1000 warmed events reuse fixed render pool without allocations");
  Console.WriteLine("PASS "+assertions+" spear afterglow assertions; synthetic nonflat physics and render substitutes, not native appearance.");
 }
}
