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
        if (GazeReleaseTuning.Enabled)
        {
            Draw(timer);
            Check(GUI.Rects.Count==0 && GUI.Labels.Count==0,"release trial adds no text, charge pips or timer panel");
            PulseAudio(); PulseKick(body); return;
        }
        Draw(timer);Check(GUI.Rects.Count==9 && GUI.LastLabel=="4.0s" && GUI.Labels[0]=="GAZE","local owner draws compact actual seconds and five energy pips");
        Check(GUI.Rects[0].width<150 && GUI.Rects[0].height<=30,"compact responsive timer panel");
        float width=GUI.Rects[3].width;state.RemainingBeamSeconds=6;state.ActualBeamSeconds=6;Draw(timer);
        Check(GUI.Rects[3].width==width && GUI.Rects[3].width==GUI.Rects[2].width && GUI.LastLabel=="6.0s" && GUI.Labels[0]=="GAZE  +2s","entry and acknowledged extension use full width and actual grant");
        Time.unscaledTime=1;Draw(timer);Check(GUI.Labels[0]=="GAZE","extension cue expires without looping");
        state.RemainingBeamSeconds=2;Draw(timer);Check(Math.Abs(GUI.Rects[3].width/width-1f/3f)<.001f,"fixed launch scale drains instead of renormalizing every frame");
        state.SuccessfulLaunches=5;Draw(timer);Check((int)typeof(GazeTimerHud).GetField("energyTier",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(timer)! == 5,"five successful launch tiers exposed without extra bars");
        hud.localUserViewer.cachedBody=new CharacterBody();Draw(timer);Check(GUI.Rects.Count==0,"no meter for observer or other body");
        hud.localUserViewer.cachedBody=body;hud.mainContainer.activeInHierarchy=false;Draw(timer);Check(GUI.Rects.Count==0,"hidden native HUD hides meter");
        hud.mainContainer.activeInHierarchy=true;HUD.cvHudEnable.value=false;Draw(timer);Check(GUI.Rects.Count==0,"native HUD setting hides meter");HUD.cvHudEnable.value=true;
        body.healthComponent.alive=false;Draw(timer);Check(GUI.Rects.Count==0,"death leaves no meter");body.healthComponent.alive=true;
        state.TimerVisible=false;Draw(timer);Check(GUI.Rects.Count==0,"end phase leaves no meter");state.TimerVisible=true;
        EntityStateMachine.Machine.state=null;Draw(timer);Check(GUI.Rects.Count==0,"state replacement leaves no meter");EntityStateMachine.Machine.state=state;
        Event.current.type=EventType.Layout;Draw(timer);Check(GUI.Rects.Count==0,"layout pass adds no drawings");Event.current.type=EventType.Repaint;
        hud.mainContainerCanvas.pixelRect=new Rect(500,0,500,400);Draw(timer);Check(GUI.Rects[0].x>=500 && GUI.Rects[0].x+GUI.Rects[0].width<=1000,"split viewport keeps timer within local canvas");
        GUI.color=new Color(.2f,.3f,.4f,.5f);Draw(timer);Check(GUI.color.r==.2f && GUI.color.a==.5f,"meter restores shared GUI color");
        GUI.matrix=new Matrix4x4 {marker=42};GUI.DrawMatrices.Clear();Draw(timer);
        Check(GUI.DrawMatrices.All(matrix=>matrix.marker==1) && GUI.matrix.marker==42,"pixel viewport renders with identity and restores another OnGUI participant's matrix");
        state.RemainingBeamSeconds=.01f;Draw(timer);Check(GUI.LastLabel=="0.1s","remaining label rounds upward at last tenth");
        state.RemainingBeamSeconds=0;Draw(timer);Check(GUI.Rects[3].width==0,"expired actual timer draws no fill");
        state.ActualBeamSeconds=6.1f;Draw(timer);Check(GUI.Labels[0]=="GAZE  +0.1s","fractional last grant remains visible and actual");
        EntityStateMachine.Machine.state=new GazeState {TimerVisible=true,RemainingBeamSeconds=4,ActualBeamSeconds=4};
        Draw(timer);Check(GUI.Labels[0]=="GAZE" && GUI.LastLabel=="4.0s","new cast clears old extension cue");
        PulseAudio();
        PulseKick(body);
    }
    static void PulseAudio()
    {
        var pulse=new GazePulseAudio();var source=new GameObject();Time.unscaledTime=0;
        Check(pulse.Play(source) && Util.Sounds[^1]=="Play_HS_GazeSurge1","first confirmed spend uses first authored surge");
        Time.unscaledTime=.1f;Check(!pulse.Play(source),"delayed clustered launch never stacks");
        Time.unscaledTime=.239f;Check(!pulse.Play(source),"voice duration protected");
        Time.unscaledTime=.24f;Check(pulse.Play(source,99) && Util.Sounds[^1]=="Play_HS_GazeSurge5","next launch allowed at end, intensity capped at five");
        HollowSaint.FoundationKit.Vfx.CustomSoundBank.Ready=false;Time.unscaledTime=1;
        Check(pulse.Play(source) && Util.Sounds[^1]=="Play_captain_m2_tazer_shoot","missing custom bank uses indexed electrical fallback");
        Time.unscaledTime=float.NaN;Check(!pulse.Play(source),"invalid clock adds no sound");
        Time.unscaledTime=2;Check(!pulse.Play(null),"destroyed source adds no sound");
        Check(Util.Sounds.Count==3,"one event each, no startup/loop layers");
    }
    static void PulseKick(CharacterBody body)
    {
        var kick=new GazePulseKick();Time.unscaledTime=3;
        Check(!kick.Play(body,1),"no local viewer means no observer shake");
        var local=new LocalUser{cachedBody=body,cameraRigController=new CameraRigController{targetBody=body}};
        LocalUserManager.readOnlyLocalUsersList.Add(local);
        Check(kick.Play(body,1) && ShakeEmitter.Count==1,"confirmed active owner gets one native impulse");
        Check(!kick.Play(body,2),"clustered acknowledgement cannot stack shake");
        Time.unscaledTime=4;HollowSaint.FoundationKit.Vfx.ImpactFeelSettings.Enabled=false;
        Check(!kick.Play(body,5),"impact feel setting disables pulse shake");
        HollowSaint.FoundationKit.Vfx.ImpactFeelSettings.Enabled=true;
        local.cameraRigController.targetBody=new CharacterBody();Check(!kick.Play(body,5),"spectator camera never shakes");
        local.cameraRigController.targetBody=body;
        LocalUserManager.readOnlyLocalUsersList.Add(new LocalUser());Check(!kick.Play(body,5),"split local view suppresses global emitter cross-talk");
        LocalUserManager.readOnlyLocalUsersList.RemoveAt(1);
        Check(kick.Play(body,99) && ShakeEmitter.Amplitude<=.2251f,"owner amplitude bounded at five spends");
        Time.unscaledTime=5;((GazeState)EntityStateMachine.Machine.state).FuelAdmissionOpen=false;
        Check(!kick.Play(body,5),"late acknowledgement after local cancellation produces no impulse");
        Time.unscaledTime=float.NaN;Check(!kick.Play(body,5),"invalid clock produces no impulse");
    }
}
