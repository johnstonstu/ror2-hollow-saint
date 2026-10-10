using HollowSaint.FoundationKit.HollowedOrb;
using HollowSaint.FoundationKit.Vfx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

static class OrbAudioCases
{
    // Success: one prefix and at most one loop per gather, no post after any
    // terminal edge, exact owned-ID fades, and only the first flight marker
    // creates a stationary throw anchor independent of the transient flight.
    internal static void Run(Action<bool,string> check)
    {
        CustomSoundBank.Ready=true;NetworkClient.active=true;
        for(int cast=0;cast<1000;cast++)
        {
            Util.Sounds.Clear();AkSoundEngine.Stops.Clear();
            var audio=new OrbChargeAudio(new GameObject());
            audio.Begin(0);audio.Begin(0);
            check(Util.Sounds.Count==1 && Util.Sounds[0].name=="Play_HS_OrbChargeStart","orb begins once with its own rising cue");
            audio.Tick(.19f,true);check(Util.Sounds.Count==1,"orb loop waits for prefix");
            audio.Tick(.21f,true);
            check(Util.Sounds.Count==2 && Util.Sounds[^1].name=="Play_HS_OrbChargeLoop","orb starts one custom sustained loop");
            for(int i=0;i<100;i++)audio.Tick(1+i,true);
            check(Util.Sounds.Count==2,"long held orb never stacks loops");
            audio.PlayCue("Play_HS_GazeLoad1");audio.PlayCue("Play_loader_R_shock");
            bool release=cast%2==0;
            audio.End(release);audio.End(!release);audio.Dispose();audio.Tick(200,true);audio.Begin(201);
            check(Util.Sounds.Count==4,"orb terminal edge cannot restart or throw");
            check(AkSoundEngine.Stops.Count==4 && Util.Sounds.All(s=>AkSoundEngine.Stops.Contains(s.id)),"orb stops every owned ID exactly once");
            check(Util.Sounds.All(s=>AkSoundEngine.Fades[s.id]==(release?70:180)),"orb release/cancel uses bounded intended fade");
        }
        void Terminal(Action<OrbChargeAudio> action,string label)
        {
            Util.Sounds.Clear();AkSoundEngine.Stops.Clear();
            var audio=new OrbChargeAudio(new GameObject());audio.Begin(0);audio.Tick(.21f,true);
            action(audio);audio.Tick(10,true);audio.Dispose();
            check(Util.Sounds.Count==2 && AkSoundEngine.Stops.Count==2,label);
        }
        Terminal(a=>a.Tick(.3f,false),"orb owner death stops all voices");
        Terminal(a=>{Stage.instance=new();a.Tick(.3f,true);},"orb stage loss stops all voices");
        Terminal(a=>a.Dispose(),"orb disable/destroy disposal stops all voices");
        Util.Sounds.Clear();AkSoundEngine.Stops.Clear();
        var partial=new OrbChargeAudio(new GameObject());partial.Begin(0);partial.Dispose();partial.Tick(1,true);
        check(Util.Sounds.Count==1 && AkSoundEngine.Stops.Count==1,"partial presentation setup disposal stops prefix and prevents loop");
        CustomSoundBank.Ready=false;Util.Sounds.Clear();
        using(var fallback=new OrbChargeAudio(new GameObject()))
        {
            fallback.Begin(0);fallback.Tick(1,true);fallback.Tick(2,true);
            check(Util.Sounds.Count==1 && Util.Sounds[0].name=="Play_mage_m1_cast_lightning","missing orb bank stays finite with no fallback loop");
        }
        CustomSoundBank.Ready=true;Util.Sounds.Clear();AkSoundEngine.Stops.Clear();Util.Zero=true;
        using(var zero=new OrbChargeAudio(new GameObject())){zero.Begin(0);zero.Tick(1,true);}
        check(AkSoundEngine.Stops.Count==0,"invalid playing IDs are never stopped");Util.Zero=false;
        int warnings=HollowSaint.Plugin.Log.Warnings;Util.Throw=true;
        using(var failed=new OrbChargeAudio(new GameObject())){failed.Begin(0);failed.Tick(1,true);failed.Tick(2,true);}
        check(HollowSaint.Plugin.Log.Warnings==warnings+1,"orb post failures are isolated and warned once per gather");Util.Throw=false;
        NetworkClient.active=false;Util.Sounds.Clear();
        using(var silent=new OrbChargeAudio(new GameObject())){silent.Begin(0);silent.Tick(1,true);}
        OrbThrowAudio.Play(true,new Vector3());
        check(Util.Sounds.Count==0,"dedicated server creates no orb voices");NetworkClient.active=true;
        ThrowCases(check);
    }

    static void ThrowCases(Action<bool,string> check)
    {
        Util.Sounds.Clear();UnityEngine.Object.Destroyed.Clear();
        var origin=new Vector3(2,3,4);
        OrbThrowAudio.Play(false,origin);
        check(Util.Sounds.Count==0 && UnityEngine.Object.Destroyed.Count==0,"bounces never create throw sounds or anchors");
        OrbThrowAudio.Play(true,origin);
        check(Util.Sounds.Count==1 && Util.Sounds[0].name=="Play_HS_OrbThrow","first segment plays one dedicated orb discharge");
        var anchor=Util.Sources[Util.Sounds[0].id];
        check(anchor.transform.parent==null && anchor.transform.position.Equals(origin),"throw anchor is stationary and independent of flight/body hierarchy");
        check(UnityEngine.Object.Destroyed.Any(x=>x.source==anchor && x.delay==2f),"throw emitter has a bounded tail lifetime");
        for(int i=0;i<100;i++)OrbThrowAudio.Play(false,origin);
        check(Util.Sounds.Count==1,"all later bounce markers remain silent");
        CustomSoundBank.Ready=false;OrbThrowAudio.Play(true,origin);
        check(Util.Sounds[^1].name=="Play_captain_m2_tazer_shoot","missing throw bank uses finite electrical fallback");CustomSoundBank.Ready=true;
        int warnings=HollowSaint.Plugin.Log.Warnings;Util.Throw=true;
        OrbThrowAudio.Play(true,origin);Util.Throw=false;
        check(HollowSaint.Plugin.Log.Warnings==warnings+1 && UnityEngine.Object.Destroyed[^1].delay==0,"failed throw post warns and destroys its anchor immediately");
    }
}
