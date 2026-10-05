using System.Reflection;
using HollowSaint.FoundationKit.OpenCircuit;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

static class Program
{
    static int checks;
    static void Check(bool pass,string reason){checks++;if(!pass)throw new Exception(reason);}
    static void Call(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(target,null);
    static CircuitDwellDriver Driver(CharacterBody owner){MonoBehaviour.Body=owner;var driver=new CircuitDwellDriver();Call(driver,"Awake");return driver;}
    static HurtBox Enemy(float distance=2)
    {
        var target=new CharacterBody();target.teamComponent.teamIndex=1;target.healthComponent.body=target;
        var box=new HurtBox{healthComponent=target.healthComponent};box.collider.Point=box.transform.position=new Vector3(distance,0,0);
        target.hurtBoxGroup.hurtBoxes=new[]{box};return box;
    }
    static void Tick(CircuitDwellDriver driver,int frames){for(int i=0;i<frames;i++)Call(driver,"FixedUpdate");}
    static void Main()
    {
        foreach(float dt in new[]{.01f,.02f,.05f,.1f})
        {
            var p=new CircuitDwellPolicy();int zaps=0;
            for(int i=0;i<1000;i++){if(p.Tick(true,dt))zaps++;Check(p.Progress<=1,"dwell bounded after arbitrary uptime");}
            Check(zaps==1&&p.Spent,"one zap per crown, attack speed independent");
            p.Tick(false,dt);for(int i=0;i<400;i++)Check(!p.Tick(true,dt),"leaving cannot refresh spent zap");
        }
        var interrupted=new CircuitDwellPolicy();for(int i=0;i<100;i++)interrupted.Tick(true,.02f);
        interrupted.Tick(false,.02f);Check(interrupted.Seconds==0&&!interrupted.Spent,"leaving resets partial dwell");
        Check(!interrupted.Tick(true,float.NaN)&&!interrupted.Tick(true,float.PositiveInfinity),"invalid delta cannot grant dwell");
        var owner=new CharacterBody();var driver=Driver(owner);var victim=Enemy();driver.Confirm(victim);driver.Confirm(victim);
        Tick(driver,149);Check(victim.healthComponent.Hits==0,"not before three seconds");
        Tick(driver,1);Check(victim.healthComponent.Hits==1&&Math.Abs(victim.healthComponent.Damage-45.9f)<.001f,"actual driver 270 percent current body damage");
        Check(victim.healthComponent.Proc==1&&victim.healthComponent.StaticHits==0&&HollowSaint.FoundationKit.Storm.StormServer.Depth==0,"normal item proc coefficient, guarded no Static, depth restored");
        Check(HollowSaint.FoundationKit.Vfx.KitFx.Sends==1,"one confirmed network route, no local duplicate");
        Tick(driver,600);Check(victim.healthComponent.Hits==1,"long crown cannot repeat victim zap");
        victim.collider.Point=new Vector3(9,0,0);Tick(driver,1);victim.collider.Point=new Vector3(2,0,0);Tick(driver,200);Check(victim.healthComponent.Hits==1,"leave/reentry preserves spent tombstone");
        owner.Open=false;Tick(driver,1);owner.Open=true;driver.Confirm(victim);Tick(driver,150);Check(victim.healthComponent.Hits==2,"new crown allows new bounded zap");
        foreach(string blocked in new[]{"outside","friendly","dead victim","dead owner","dash","disabled collider","inactive hurtbox","server inactive"})
        {
            var caster=new CharacterBody();var d=Driver(caster);var box=Enemy();d.Confirm(box);
            if(blocked=="outside")box.collider.Point=new Vector3(8.01f,0,0);
            if(blocked=="friendly")box.healthComponent.body.teamComponent.teamIndex=0;
            if(blocked=="dead victim")box.healthComponent.alive=false;
            if(blocked=="dead owner")caster.healthComponent.alive=false;
            if(blocked=="dash")caster.Dash=true;
            if(blocked=="disabled collider")box.collider.enabled=false;
            if(blocked=="inactive hurtbox")box.gameObject.activeInHierarchy=false;
            if(blocked=="server inactive")NetworkServer.active=false;
            Tick(d,200);Check(box.healthComponent.Hits==0,blocked+" cannot earn zap");NetworkServer.active=true;
        }
        var many=Driver(new CharacterBody());var boxes=Enumerable.Range(0,65).Select(_=>Enemy()).ToArray();foreach(var box in boxes)many.Confirm(box);
        Tick(many,150);Check(boxes.Take(64).All(box=>box.healthComponent.Hits==1)&&boxes[64].healthComponent.Hits==0,"distinct victim budget exactly 64, multiple hurtboxes deduped");
        Call(many,"OnDisable");Tick(many,200);Check(boxes.All(box=>box.healthComponent.Hits<=1),"disable clears all retained dwell");
        var reject=Driver(new CharacterBody());var rejected=Enemy();rejected.healthComponent.Reject=true;reject.Confirm(rejected);
        int sent=HollowSaint.FoundationKit.Vfx.KitFx.Sends;Tick(reject,150);Check(HollowSaint.FoundationKit.Vfx.KitFx.Sends==sent,"rejected damage produces no confirmed connection");
        var stageDriver=Driver(new CharacterBody());var stageVictim=Enemy();stageDriver.Confirm(stageVictim);Tick(stageDriver,100);
        Stage.instance=new Stage();Tick(stageDriver,200);Check(stageVictim.healthComponent.Hits==0,"stage change clears old dwell and requires fresh confirmed admission");
        Console.WriteLine($"PASS {checks} actual Circuit dwell policy/driver assertions with collider/network substitutes; native acceptance pending");
    }
}
