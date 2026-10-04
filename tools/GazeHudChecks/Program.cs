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
    static void Draw(GazeTimerHud timer) { GUI.Rects.Clear();Call(timer,"OnGUI"); }
    static int Main()
    {
        try { Run();Console.WriteLine($"PASS {checks} production HUD routing/drawing assertions with native substitutes; actual Unity placement pending");return 0; }
        catch(Exception error) {Console.Error.WriteLine(error);return 1;}
    }
    static void Run()
    {
        var body=new CharacterBody();MonoBehaviour.Body=body;
        var state=new GazeState {TimerVisible=true,RemainingBeamSeconds=4};
        EntityStateMachine.Machine=new EntityStateMachine {state=state};
        var hud=new HUD {localUserViewer=new LocalUser {cachedBody=body}};
        HUD.readOnlyInstanceList.Add(hud);var timer=new GazeTimerHud();Call(timer,"Awake");
        Draw(timer);Check(GUI.Rects.Count==2 && GUI.LastLabel=="Gaze 4.0s","local owner draws actual seconds and bar");
        float width=GUI.Rects[1].width;state.RemainingBeamSeconds=6;Draw(timer);
        Check(GUI.Rects[1].width>width && GUI.LastLabel=="Gaze 6.0s","acknowledged extension visibly grows bar");
        state.RemainingBeamSeconds=2;Draw(timer);Check(GUI.Rects[1].width<width,"elapsed time drains meter");
        hud.localUserViewer.cachedBody=new CharacterBody();Draw(timer);Check(GUI.Rects.Count==0,"no meter for observer or other body");
        hud.localUserViewer.cachedBody=body;hud.mainContainer.activeInHierarchy=false;Draw(timer);Check(GUI.Rects.Count==0,"hidden native HUD hides meter");
        hud.mainContainer.activeInHierarchy=true;HUD.cvHudEnable.value=false;Draw(timer);Check(GUI.Rects.Count==0,"native HUD setting hides meter");HUD.cvHudEnable.value=true;
        body.healthComponent.alive=false;Draw(timer);Check(GUI.Rects.Count==0,"death leaves no meter");body.healthComponent.alive=true;
        state.TimerVisible=false;Draw(timer);Check(GUI.Rects.Count==0,"end phase leaves no meter");state.TimerVisible=true;
        EntityStateMachine.Machine.state=null;Draw(timer);Check(GUI.Rects.Count==0,"state replacement leaves no meter");EntityStateMachine.Machine.state=state;
        Event.current.type=EventType.Layout;Draw(timer);Check(GUI.Rects.Count==0,"layout pass adds no drawings");Event.current.type=EventType.Repaint;
        hud.mainContainerCanvas.pixelRect=new Rect(500,0,500,400);Draw(timer);Check(GUI.Rects[1].x>=500 && GUI.Rects[1].x+GUI.Rects[1].width<=1000,"split viewport keeps timer within local canvas");
        GUI.color=new Color(.2f,.3f,.4f,.5f);Draw(timer);Check(GUI.color.r==.2f && GUI.color.a==.5f,"meter restores shared GUI color");
        state.RemainingBeamSeconds=.01f;Draw(timer);Check(GUI.LastLabel=="Gaze 0.1s","remaining label rounds upward at last tenth");
        state.RemainingBeamSeconds=0;Draw(timer);Check(GUI.Rects[1].width==0,"expired actual timer draws no fill");
    }
}
