using System.Reflection;
using HollowSaint.FoundationKit.Gaze;
using RoR2;
using RoR2.UI;
using UnityEngine;

static class Program
{
    static int checks;
    static void Check(bool pass,string name) { checks++; if(!pass)throw new Exception(name); }
    static void Call(GazeTimerHud timer,string method) => typeof(GazeTimerHud).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(timer,null);
    static void Draw(GazeTimerHud timer) { GUI.Rects.Clear();GUI.Labels.Clear();Call(timer,"OnGUI"); }
    static int Main()
    {
        try { Run();Console.WriteLine($"PASS {checks} production HUD routing/drawing assertions with native substitutes; actual Unity placement pending");return 0; }
        catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
    static void Run()
    {
        var body=new CharacterBody();MonoBehaviour.Body=body;
        var state=new GazeState {TimerVisible=true,RemainingBeamSeconds=4,ActualBeamSeconds=4};
        EntityStateMachine.Machine=new EntityStateMachine {state=state};
        var hud=new HUD {localUserViewer=new LocalUser {cachedBody=body}};
        HUD.readOnlyInstanceList.Add(hud);var timer=new GazeTimerHud();Call(timer,"Awake");
        Draw(timer);Check(GUI.Rects.Count==10 && GUI.LastLabel=="4.0s" && GUI.Labels[0]=="GAZE","local owner draws compact actual seconds and titled bar");
        Check(GUI.Rects[0].width<150 && GUI.Rects[0].height<=30,"compact responsive timer panel");
        float width=GUI.Rects[3].width;state.RemainingBeamSeconds=6;state.ActualBeamSeconds=6;Draw(timer);
        Check(GUI.Rects[3].width>width && GUI.LastLabel=="6.0s" && GUI.Labels[0]=="GAZE  +2s","acknowledged extension grows bar and readable gain");
        Time.unscaledTime=1;Draw(timer);Check(GUI.Labels[0]=="GAZE","extension cue expires without looping");
        state.RemainingBeamSeconds=2;Draw(timer);Check(GUI.Rects[3].width<width,"elapsed time drains meter");
        hud.localUserViewer.cachedBody=new CharacterBody();Draw(timer);Check(GUI.Rects.Count==0,"no meter for observer or other body");
        hud.localUserViewer.cachedBody=body;hud.mainContainer.activeInHierarchy=false;Draw(timer);Check(GUI.Rects.Count==0,"hidden native HUD hides meter");
        hud.mainContainer.activeInHierarchy=true;HUD.cvHudEnable.value=false;Draw(timer);Check(GUI.Rects.Count==0,"native HUD setting hides meter");HUD.cvHudEnable.value=true;
        body.healthComponent.alive=false;Draw(timer);Check(GUI.Rects.Count==0,"death leaves no meter");body.healthComponent.alive=true;
        state.TimerVisible=false;Draw(timer);Check(GUI.Rects.Count==0,"end phase leaves no meter");state.TimerVisible=true;
        EntityStateMachine.Machine.state=null;Draw(timer);Check(GUI.Rects.Count==0,"state replacement leaves no meter");EntityStateMachine.Machine.state=state;
        Event.current.type=EventType.Layout;Draw(timer);Check(GUI.Rects.Count==0,"layout pass adds no drawings");Event.current.type=EventType.Repaint;
        hud.mainContainerCanvas.pixelRect=new Rect(500,0,500,400);Draw(timer);Check(GUI.Rects[0].x>=500 && GUI.Rects[0].x+GUI.Rects[0].width<=1000,"split viewport keeps timer within local canvas");
        GUI.color=new Color(.2f,.3f,.4f,.5f);Draw(timer);Check(GUI.color.r==.2f && GUI.color.a==.5f,"meter restores shared GUI color");
        state.RemainingBeamSeconds=.01f;Draw(timer);Check(GUI.LastLabel=="0.1s","remaining label rounds upward at last tenth");
        state.RemainingBeamSeconds=0;Draw(timer);Check(GUI.Rects[3].width==0,"expired actual timer draws no fill");
        state.ActualBeamSeconds=6.1f;Draw(timer);Check(GUI.Labels[0]=="GAZE  +0.1s","fractional last grant remains visible and actual");
        EntityStateMachine.Machine.state=new GazeState {TimerVisible=true,RemainingBeamSeconds=4,ActualBeamSeconds=4};
        Draw(timer);Check(GUI.Labels[0]=="GAZE" && GUI.LastLabel=="4.0s","new cast clears old extension cue");
        PulseAudio();
    }
    static void PulseAudio()
    {
        var pulse=new GazePulseAudio();var source=new GameObject();Time.unscaledTime=0;
        Check(pulse.Play(source) && Util.Sounds[^1]=="Play_HS_ThunderRelease","confirmed pulse uses actual discharge event");
        Time.unscaledTime=.1f;Check(!pulse.Play(source),"delayed clustered launch never stacks");
        Time.unscaledTime=.239f;Check(!pulse.Play(source),"voice duration protected");
        Time.unscaledTime=.24f;Check(pulse.Play(source),"next launch allowed when discharge ends");
        HollowSaint.FoundationKit.Vfx.CustomSoundBank.Ready=false;Time.unscaledTime=1;
        Check(pulse.Play(source) && Util.Sounds[^1]=="Play_captain_m2_tazer_shoot","missing custom bank uses indexed electrical fallback");
        Time.unscaledTime=float.NaN;Check(!pulse.Play(source),"invalid clock adds no sound");
        Time.unscaledTime=2;Check(!pulse.Play(null),"destroyed source adds no sound");
        Check(Util.Sounds.Count==3,"one event each, no startup/loop layers");
    }
}
