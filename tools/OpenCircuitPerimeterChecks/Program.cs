using System;
using System.Reflection;
using UnityEngine;
using RoR2;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Gaze;
using HollowSaint.FoundationKit.OpenCircuit.Fx;
static class Program {
 static int checks; static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 static bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.001f;
 static Action Method(object o,string name)=>(Action)Delegate.CreateDelegate(typeof(Action),o,o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic));
 static void Main(){
  var body=new GameObject().AddComponent<CharacterBody>();var model=new Transform{localScale=new Vector3(1.7f,1.2f,.8f)};
  var root=new Transform{name="halo root",localPosition=new Vector3(.2f,1.2f,.3f),localScale=new Vector3(1.2f,.9f,1.4f)};root.SetParent(model);
  body.modelLocator=new ModelLocator{modelTransform=model}; var heads=new Transform[4];var saved=new Vector3[4];
  for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;heads[i]=new Transform{name="halo "+(i+1),localPosition=new Vector3(.4f*MathF.Cos(a),.2f,.4f*MathF.Sin(a))};heads[i].SetParent(root);saved[i]=heads[i].localPosition;}
  Vector3 rootScale=root.localScale,rootPosition=root.localPosition;Quaternion rootRotation=root.localRotation;
  var pose=new OpenCircuitCrownPose();pose.Bind(heads);var center=new Vector3(3,2,-4);
  foreach(float radius in new[]{.5f,3f,8f,25f})for(int repeat=0;repeat<250;repeat++){
   Check(pose.Apply(center,radius,1),"valid pose");Check(Near(pose.Shape.Center,center),"physical crown centered on damage sphere");
   for(int i=0;i<4;i++){Check(Math.Abs(Vector3.Distance(heads[i].position,center)-radius)<.001f,"actual metal docks at exact damage radius");Check(Math.Abs(heads[i].position.y-center.y)<.001f,"final perimeter is equator, not inflated overhead radius");}
   pose.Apply(center,radius,1);Check(Math.Abs(pose.Shape.Radius-radius)<.001f,"same-frame reapply cannot compound");pose.Restore();
   for(int i=0;i<4;i++)Check(Near(heads[i].localPosition,saved[i]),"restore all authored arc positions");
   Check(Near(root.localScale,rootScale)&&Near(root.localPosition,rootPosition)&&Quaternion.Same(root.localRotation,rootRotation),"root scale/rotation/position untouched");
  }
  pose.Apply(center,8,1);pose.Apply(center,float.NaN,1);for(int i=0;i<4;i++)Check(Near(heads[i].localPosition,saved[i]),"invalid config restores pose");
  pose.Apply(center,8,1);pose.Bind(new Transform[4]);for(int i=0;i<4;i++)Check(Near(heads[i].localPosition,saved[i]),"model rebind restores previous rig");pose.Bind(heads);
  pose.Apply(center,8,.5f);pose.Release();pose.Release();Check(pose.Weight==0&&Near(heads[0].localPosition,saved[0]),"idempotent release");
  pose.Apply(center,8,1);pose.Restore();long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++){pose.Apply(center,8,1);pose.Restore();}Check(GC.GetAllocatedBytesForCurrentThread()==before,"pose hot path has no managed allocations");
  body.corePosition=center;body.buff=true;var gaze=body.gameObject.AddComponent<GazeBeam>();var fx=body.gameObject.AddComponent<OpenCircuitDomeFx>();
  var awake=Method(fx,"Awake");var update=Method(fx,"Update");var late=Method(fx,"LateUpdate");var disable=Method(fx,"OnDisable");awake();
  void Frame(){Physics.Rays=Physics.Capsules=0;Time.time+=.02f;Time.unscaledTime=Time.time;update();late();Check(Physics.Rays<=4&&Physics.Capsules<=96,"bounded per-frame world queries");}
  for(int frame=0;frame<40;frame++)Frame();Check(fx.Expansion>.999f,"late-join buff opens physical perimeter without cast event");
  var strokes=(Array)typeof(OpenCircuitDomeFx).GetField("strokes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(fx);
  Check(strokes.Length==6,"four perimeter hops plus only two upward arcs");
  var typ=strokes.GetValue(0).GetType();var glowField=typ.GetField("glow",BindingFlags.Instance|BindingFlags.NonPublic);var pointsField=typ.GetField("points",BindingFlags.Instance|BindingFlags.NonPublic);
  int built=GameObject.Created;
  for(uint skin=0;skin<6;skin++)foreach(float radius in new[]{3f,8f,25f}){
   body.skinIndex=skin;KitTuning.OpenCircuitRadius=radius;
   for(int frame=0;frame<60;frame++){
    Frame();int visibleLinks=0,visibleUp=0;
    for(int j=0;j<strokes.Length;j++){
     var stroke=strokes.GetValue(j);var line=(LineRenderer)glowField.GetValue(stroke);if(!line.enabled)continue;
     if(j<4)visibleLinks++;else visibleUp++;
     foreach(var point in (Vector3[])pointsField.GetValue(stroke))Check(Vector3.Distance(point,center)+line.widthMultiplier*.5f<=radius+.001f,"perimeter lightning including half-width stays within damage sphere");
    }
    Check(visibleLinks<=2&&visibleUp<=2,"no enclosing wire cage");
   }
  }
  before=GC.GetAllocatedBytesForCurrentThread();for(int frame=0;frame<1000;frame++)Frame();Check(GC.GetAllocatedBytesForCurrentThread()==before&&GameObject.Created==built,"steady perimeter rendering reuses bounded pool");
  KitTuning.OpenCircuitRadius=8f; Physics.Ground=true; Physics.GroundY=center.y-1f;
  for(int frame=0;frame<80;frame++)Frame();
  var terrainField=typeof(OpenCircuitDomeFx).GetField("terrain",BindingFlags.Instance|BindingFlags.NonPublic);
  var validField=typeof(OpenCircuitDomeFx).GetField("terrainValid",BindingFlags.Instance|BindingFlags.NonPublic);
  var samples=(Vector3[])terrainField.GetValue(fx);var valid=(bool[])validField.GetValue(fx);
  for(int i=0;i<32;i++)Check(valid[i]&&Math.Abs(samples[i].y-(Physics.GroundY+.1f))<.001f,"cached samples follow supplied ground");
  bool contours=false;for(int frame=0;frame<60;frame++){Frame();for(int i=0;i<4;i++){var line=(LineRenderer)glowField.GetValue(strokes.GetValue(i));if(line.enabled)foreach(var p in line.points)if(p.y<center.y-.5f)contours=true;}}
  Check(contours,"electrical sweeps acquire terrain height while physical docks remain equatorial");
  var strikes=(Array)typeof(OpenCircuitDomeFx).GetField("strikes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(fx);
  var st=strikes.GetValue(0).GetType();var strokeField=st.GetField("stroke",BindingFlags.Instance|BindingFlags.NonPublic);
  LineRenderer StrikeLine(int i)=>(LineRenderer)glowField.GetValue(strokeField.GetValue(strikes.GetValue(i)));
  Vector3 victim=center+new Vector3(2,1,2);
  Check(!fx.ShowConfirmedStrike(new Vector3(float.NaN,0,0)),"reject invalid victim");
  for(int i=0;i<4;i++)Check(fx.ShowConfirmedStrike(victim),"accept bounded authoritative event");
  Check(!fx.ShowConfirmedStrike(victim),"full pool drops fifth event without allocating");Frame();
  int shown=0;for(int i=0;i<4;i++){var line=StrikeLine(i);if(!line.enabled)continue;shown++;Check(Near(line.points[16],victim),"exact confirmed victim endpoint");bool dock=Near(line.points[0],body.corePosition);for(int j=0;j<4;j++)dock|=Near(line.points[0],heads[j].position);Check(dock,"source is actual body or crown element");}
  Check(shown>0,"confirmed events produce visible connections");
  Physics.Blocked=true;Frame();for(int i=0;i<4;i++)Check(!StrikeLine(i).enabled,"walls suppress confirmed connections");for(int i=0;i<6;i++)Check(!((LineRenderer)glowField.GetValue(strokes.GetValue(i))).enabled,"walls suppress ambient strokes");
  Physics.Blocked=false;for(int i=0;i<15;i++)Frame();for(int i=0;i<4;i++)Check(!StrikeLine(i).enabled,"event TTL expires without automatic repeats");
  Check(fx.ShowConfirmedStrike(victim),"pool reusable");body.buff=false;for(int i=0;i<20;i++)Frame();Check(!fx.ShowConfirmedStrike(victim),"closed crown rejects events");body.buff=true;for(int i=0;i<40;i++)Frame();for(int i=0;i<4;i++)Check(!StrikeLine(i).enabled,"reopening cannot replay prior events");
  before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++){fx.ShowConfirmedStrike(victim);Frame();}Check(GC.GetAllocatedBytesForCurrentThread()==before&&GameObject.Created==built,"confirmed-event hot path is allocation-free and pooled");
  Physics.Ground=false;body.corePosition+=new Vector3(1,0,0);Frame();valid=(bool[])validField.GetValue(fx);for(int i=0;i<32;i++)Check(!valid[i],"movement invalidates old terrain cache");body.corePosition=center;
  body.buff=false;for(int frame=0;frame<20;frame++)Frame();Check(fx.Expansion==0,"normal expiry finishes return");for(int i=0;i<4;i++)Check(Near(heads[i].localPosition,saved[i]),"expiry leaves animator pose intact");
  body.buff=true;for(int frame=0;frame<40;frame++)Frame();update();gaze.Current=GazeBeam.Phase.Beam;heads[0].localPosition=saved[0]+Vector3.up;late();Check(Near(heads[0].localPosition,saved[0]+Vector3.up)&&fx.Expansion==0,"yield cannot overwrite Gaze pose applied earlier that LateUpdate");heads[0].localPosition=saved[0];gaze.Current=GazeBeam.Phase.Idle;
  for(int frame=0;frame<40;frame++)Frame();body.healthComponent.alive=false;late();Check(fx.Expansion==0&&Near(heads[0].localPosition,saved[0]),"death restores pose immediately");body.healthComponent.alive=true;
  for(int frame=0;frame<40;frame++)Frame();body.modelLocator=null;late();Check(fx.Expansion==0&&Near(heads[0].localPosition,saved[0]),"model disappearance restores original rig");
  body.modelLocator=new ModelLocator{modelTransform=model};for(int frame=0;frame<60;frame++)Frame();disable();Check(fx.Expansion==0&&Near(heads[0].localPosition,saved[0]),"disable restores then releases geometry");
  Console.WriteLine("PASS "+checks+" actual crown-pose/perimeter assertions, 1000-cycle restoration and warm allocation checks. Hierarchy/render substitutes, not native Unity.");
 }
}
