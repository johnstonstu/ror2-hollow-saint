using System;
using RoR2;
using UnityEngine;
using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.ChargedStorm;
using HollowSaint.FoundationKit.OpenCircuit;

namespace HollowSaint.FoundationKit.OpenCircuit
{
    public static class OpenCircuitTuning
    {
        public const float CastClipSeconds=29f/24f, UnfoldNormalizedTime=5f/29f, CrownActiveNormalizedTime=21f/29f;
        public const string CastArmsState="Open Circuit arms";
    }
    public static class OpenCircuitVfxHooks
    {
        public static int unfolds,activations;
        public static void RaiseUnfold(CharacterBody body) { unfolds++; }
        public static void RaiseCrownActivated(CharacterBody body) { activations++; }
    }
    public static class CrownGestureFlow { public static void Cancel(CharacterBody body) {} }
}
static partial class Program
{
    static void CircuitRefinement()
    {
        // Success: pay once before native marker activation; empowered density
        // grows damage events without increasing the baseline Static cadence.
        Near(CircuitChargePolicy.Interval(.5f,1),.5f,"one-charge Circuit keeps baseline interval");
        Near(CircuitChargePolicy.Interval(.5f,3),1f/3f,"three charges increase pulse density by half");
        Near(CircuitChargePolicy.Interval(.5f,5),.25f,"five charges double area pulse density");
        Check(CircuitChargePolicy.Arcs(5)==14,"charged area gains more simultaneous visual arcs");
        var clock=new CircuitPulseCadence();int pulses=0,funded=0;
        for(int i=0;i<=100;i++) if(clock.Tick(.02f,.25f,.5f,true,out bool funds)) { pulses++;if(funds)funded++; }
        Check(pulses==9 && funded==5,"two seconds of full Circuit doubles strikes but retains baseline Static events");
        clock.Reset();pulses=funded=0;
        for(int i=0;i<=100;i++) if(clock.Tick(.02f,.5f,.5f,true,out bool funds)) { pulses++;if(funds)funded++; }
        Check(pulses==5 && funded==5,"baseline pulse timing and Static funding preserved");
        Reset();var b=Body();var m=b.GetComponent<DischargeMeter>();m.RegisterForTest(5);
        var s=State(b,2,false);s.Age(1.4f);
        OpenCircuitVfxHooks.unfolds=OpenCircuitVfxHooks.activations=0;
        s.ServerRequest(Request(2,8,5),b.master.playerCharacterMasterController.networkUser.connectionToClient,false);
        Check(s.Released && m.Charge==0 && b.GetBuffCount(OpenCircuitBuff.Def)==0,"Circuit spends five once before authored activation");
        s.Age(1.61f);s.FixedUpdate();
        Check(OpenCircuitVfxHooks.unfolds==1 && b.GetBuffCount(OpenCircuitBuff.Def)==0,"authored unfold occurs before crown activation");
        s.Age(2.28f);s.FixedUpdate();
        Check(b.GetBuffCount(OpenCircuitBuff.Def)==1 && OpenCircuitBuff.Charges(b)==5 && OpenCircuitVfxHooks.activations==1,"native activation applies confirmed fuel and crown buff at marker");
        s.ServerRequest(Request(2,8,5),b.master.playerCharacterMasterController.networkUser.connectionToClient,false);
        s.OnExit();Check(OpenCircuitVfxHooks.activations==1 && m.Charge==0,"duplicate/exit cannot reopen completed crown or debit fuel");
        Reset();b=Body();b.GetComponent<DischargeMeter>().RegisterForTest(3);s=State(b,2,false);s.Age(.5f);
        s.ServerRequest(Request(2,8,2),b.master.playerCharacterMasterController.networkUser.connectionToClient,false);s.OnExit();
        Check(b.GetBuffCount(OpenCircuitBuff.Def)==1 && OpenCircuitBuff.Charges(b)==2,"committed interruption still grants the paid crown window");
        Reset();b=Body();m=b.GetComponent<DischargeMeter>();m.RegisterForTest(3);s=State(b,2,false);s.Age(.1f);s.OnExit();
        Check(m.Charge==3 && b.GetBuffCount(OpenCircuitBuff.Def)==0,"pre-commit Circuit interruption preserves bank without crown");
    }
}
