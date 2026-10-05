using System.Reflection;
using HollowSaint.FoundationKit.Stormspear;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using HollowSaint.FoundationKit.Vfx;

static class Program
{
    static int checks;
    static void Check(bool pass,string message){checks++;if(!pass)throw new Exception(message);}
    static void Call(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(target,null);
    static void Run()
    {
        var body=new CharacterBody();MonoBehaviour.SelectedBody=body;
        var charge=new StormspearCharge();MonoBehaviour.Charge=charge;Call(charge,"Awake");
        var audio=new StormspearAudio();Call(audio,"Awake");Call(audio,"OnEnable");
        for(int cast=0;cast<1000;cast++)
        {
            Time.time=cast*10;Util.Sounds.Clear();AkSoundEngine.Stops.Clear();
            charge.Begin(SpearForm.Hand);
            Check(Util.Sounds.Count==1 && Util.Sounds[0].name=="Play_HS_SpearChargeStart","one prefix per cast");
            Time.time+=.19f;Call(audio,"Update");Check(Util.Sounds.Count==1,"loop waits for charge prefix");
            Time.time+=.02f;Call(audio,"Update");uint held=Util.Sounds[^1].id;
            Check(Util.Sounds.Count==2 && Util.Sounds[^1].name=="Play_HS_SpearChargeLoop","one held static loop during buildup");
            charge.SetCharge(1);Call(audio,"Update");
            Check(Util.Sounds.Count==3 && Util.Sounds[^1].name=="Play_HS_MeterFull","crossing every tier together emits only highest ready cue");
            for(int frame=0;frame<5;frame++){Time.time+=.1f;Call(audio,"Update");}
            Check(Util.Sounds.Count==3,"holding full never restarts or stacks voices");
            if(cast%2==0)
            {
                charge.Release();Check(AkSoundEngine.Stops.Contains(held),"release stops exact loop playing ID");
                Call(audio,"Update");Check(Util.Sounds.Count==3,"hand release respects apex delay");
                Time.time+=.13f;Call(audio,"Update");Check(Util.Sounds.Count==4 && Util.Sounds[^1].name=="Play_HS_SpearThrowHeavy","one heavy release instead of layered throw");
                Call(audio,"Update");Check(Util.Sounds.Count==4,"release is idempotent");
            }
            else
            {
                charge.Cancel();Check(AkSoundEngine.Stops.Contains(held),"cancellation stops exact loop ID");
                Time.time+=1;Call(audio,"Update");Check(Util.Sounds.Count==3,"cancel creates no release sound");
            }
        }
        void Begin(){charge.Cancel();Time.time+=1;Util.Sounds.Clear();AkSoundEngine.Stops.Clear();charge.Begin(SpearForm.Hand);Time.time+=.21f;Call(audio,"Update");}
        Begin();body.healthComponent.alive=false;Call(audio,"Update");Check(AkSoundEngine.Stops.Count==2,"death stops prefix and loop");body.healthComponent.alive=true;
        Begin();Stage.instance=new();Call(audio,"Update");Check(AkSoundEngine.Stops.Count==2,"stage change stops owned audio");
        Begin();charge.isActiveAndEnabled=false;Call(audio,"Update");Check(AkSoundEngine.Stops.Count==2,"disabled source stops audio");charge.isActiveAndEnabled=true;
        Begin();Call(audio,"OnDisable");Check(AkSoundEngine.Stops.Count==2,"driver disable stops and unsubscribes");
        Util.Sounds.Clear();charge.Cancel();charge.Begin(SpearForm.Hand);Check(Util.Sounds.Count==0,"disabled driver receives no events");
        Call(audio,"OnEnable");Check(Util.Sounds.Count==1,"re-enable binds currently charging source once");
        charge.Release();body.healthComponent.alive=false;Time.time+=.2f;Call(audio,"Update");Check(Util.Sounds.Count==1,"death during release delay drops pending cue");body.healthComponent.alive=true;
        CustomSoundBank.Ready=false;Begin();Check(Util.Sounds.Count==1 && Util.Sounds[0].name=="Play_mage_m1_cast_lightning","failed bank has finite prefix fallback and no substitute infinite loop");
        charge.Release();Time.time+=.2f;Call(audio,"Update");Check(Util.Sounds[^1].name=="Play_captain_m2_tazer_shoot","release fallback preserved");CustomSoundBank.Ready=true;
        NetworkClient.active=false;Begin();Check(Util.Sounds.Count==0,"dedicated server stays silent");NetworkClient.active=true;
        Util.Throw=true;Begin();Call(audio,"Update");charge.Cancel();Check(HollowSaint.Plugin.Log.Warnings==1,"cosmetic post failures are isolated and warned once");Util.Throw=false;
        Begin();Call(audio,"OnDestroy");Check(AkSoundEngine.Stops.Count==2,"destroy stops owned IDs");
    }
    static int Main(){try{Run();Console.WriteLine($"PASS {checks} production spear audio lifecycle assertions over 1000 casts; native mix pending");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
