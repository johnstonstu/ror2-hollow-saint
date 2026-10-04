using System;
using UnityEngine;
using RoR2;
namespace HollowSaint.FoundationKit.Gaze.Fx
{
    public sealed partial class GazeEmpowermentFx
    {
        private static int checks;
        private static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
        public static void Main()
        {
            var go=new GameObject();var body=go.AddComponent<CharacterBody>();var fx=go.AddComponent<GazeEmpowermentFx>();
            fx.EnableAudio=false;Time.time=0;
            fx.BeginCast(1,5,5,true,0);fx.SetAnchors(new Vector3(0,2,0),Vector3.forward,new Vector3(0,1,0));
            Check(fx.OwnsChargePresentation&&fx.capacity==5,"begin");
            int allocated=GameObject.Created;
            var original=fx.FuelPosition(0);fx.Swallow(1,0,0,0,.32f);
            Check(Vector3.Distance(original,fx.IntakePosition(fx.fuel[0],0))<.001f,"intake continuity");
            var end=fx.IntakePosition(fx.fuel[0],1);
            Check(Vector3.Dot(end-fx.crown,fx.direction)<0,"rear intake endpoint");
            fx.Swallow(1,0,0,0,.32f);Check(fx.swallows.Count==1,"duplicate intake");
            Time.time=.33f;fx.LateUpdate();Check(!fx.fuel[0].visible,"swallowed orb removed");
            fx.SetReserve(1,2);Check(fx.reserveCount==2&&fx.reserve[1].visible,"reserve visible");
            Physics.Rays=Physics.Segments=0;
            fx.LaunchPulse(1,0,new Vector3(0,2,0),new Vector3(0,0,10),new Vector3(0,0,10),Vector3.up,true,0,.2f,4,.4f,false);
            Check(Physics.Rays<=108&&Physics.Segments<=96,"bounded terrain queries");
            var p=fx.pulses[0];Check(p.roots[0].count>1,"ground path built");
            foreach(var g in p.roots)for(int j=0;j<g.count;j++)Check(Vector3.Distance(g.path[j],p.end)<=4,"footprint");
            Check(Vector3.Distance(p.roots[1].path[0],p.roots[0].path[4])<.001f,"branch attached");
            fx.SetAnchors(new Vector3(20,2,0),Vector3.right,new Vector3(0,1,0));Check(p.origin.x==0,"frozen launch");
            Time.time=.43f;fx.LateUpdate();Check(p.sleeve.line.enabled&&p.front.line.enabled,"travelling sleeve");
            Check(p.line[11].z<10,"not instantaneous endpoint");
            Time.time=.73f;fx.LateUpdate();Check(!p.sleeve.line.enabled&&p.roots[0].stroke.line.enabled,"arrival becomes ground spread");
            fx.ConfirmStrike(1,0,new Vector3(1,0,10),0);fx.LateUpdate();Check(fx.contacts[0].core.line.enabled,"confirmed strike");
            fx.ConfirmStrike(1,0,new Vector3(1,0,10),0);Check(fx.nextStrike==1,"strike duplicate");
            Check(fx.reserveCount==2,"effects leave reserve unchanged");
            fx.EndCast(1,4,1,5,EndReason.Cancelled);Check(fx.ending&&!p.active&&!fx.contacts[0].active,"cancel clears transient");
            Check(fx.mergedCount==5&&fx.fuel[4].visible&&!fx.reserve[0].visible,"merge authoritative count");
            fx.ConfirmStrike(1,1,Vector3.zero,0);Check(fx.nextStrike==1,"late event rejected");
            Time.time=1.2f;fx.LateUpdate();Check(!fx.OwnsChargePresentation,"return ownership released");
            Check(Util.Sounds==0,"no automatic merge sound or strike");
            fx.BeginCast(1,5,5,true,0);Check(!fx.OwnsChargePresentation,"ended cast cannot resurrect");
            fx.BeginCast(2,20,20,true,0);Check(fx.fuel[19].visible,"configurable capacity");
            fx.SetReserve(2,999);Check(fx.reserveCount==20,"reserve cap");
            Physics.Void=true;fx.LaunchPulse(2,0,Vector3.up,Vector3.zero,Vector3.zero,Vector3.up,true,0,.2f,4,.4f,false);
            foreach(var g in fx.pulses[0].roots)Check(g.count==0,"no phantom ground");
            Physics.Void=false;Physics.GapAt=.5f;fx.LaunchPulse(2,1,Vector3.up,Vector3.zero,Vector3.zero,Vector3.up,true,0,.2f,4,.4f,false);
            foreach(var g in fx.pulses[1].roots)for(int j=0;j<g.count;j++)Check(Math.Abs(g.path[j].x)<=.5f,"gap clipping");
            Physics.GapAt=float.PositiveInfinity;
            fx.LaunchPulse(2,2,Vector3.up,Vector3.forward*10,Vector3.zero,Vector3.up,false,0,.2f,4,.4f,false);
            Check(!fx.pulses[2].ground,"sky miss no ground");
            var airTarget=new Vector3(3,8,10);var groundPoint=new Vector3(6,0,10);
            fx.LaunchPulse(2,3,Vector3.up,airTarget,groundPoint,Vector3.up,true,0,.2f,4,.4f,false);
            var separate=fx.pulses[3];
            Check(Vector3.Distance(separate.end,airTarget)<.001f,"beam keeps elevated endpoint");
            Check(separate.ground&&separate.roots[0].count>1,"air target has independent ground spread");
            Check(Vector3.Distance(separate.roots[0].path[0],groundPoint+Vector3.up*.07f)<.001f,"roots start at supplied ground point");
            foreach(var g in separate.roots)for(int j=0;j<g.count;j++)
            {
                Check(Vector3.Distance(g.path[j],groundPoint)<=4,"separate ground footprint");
                Check(Math.Abs(g.path[j].y-.07f)<.001f,"no floating ground roots");
            }
            Physics.Rays=0;
            fx.LaunchPulse(2,4,Vector3.up,airTarget,groundPoint,Vector3.up,false,0,.2f,4,.4f,false);
            Check(Physics.Rays==0&&!fx.pulses[4].ground,"hasGround false forbids ground inference");
            foreach(var g in fx.pulses[4].roots)Check(g.count==0,"hasGround false clears roots");
            fx.LaunchPulse(2,5,Vector3.up,airTarget,groundPoint,Vector3.zero,true,0,.2f,4,.4f,false);
            Check(!fx.pulses[0].ground,"invalid ground normal rejected");
            fx.EndCast(2,0,0,0,EndReason.Death);Check(!fx.OwnsChargePresentation,"death clears immediately");
            for(uint id=3;id<1003;id++)
            {
                fx.BeginCast(id,2,5,false,0);fx.SetReserve(id,1);fx.Clear();
            }
            Check(GameObject.Created==allocated,"1000 casts reuse geometry");
            fx.BeginCast(1003,2,5,false,0);fx.Swallow(1003,0,0,.5f,.3f);Check(!fx.fuel[0].visible,"late intake catchup");
            fx.ConfirmStrike(1003,0,Vector3.zero,1);Check(!fx.contacts[0].active,"expired strike not replayed");
            fx.OnDisable();Check(!fx.OwnsChargePresentation&&!fx.root.gameObject.activeSelf,"disable hides pool");
            Color[] primary = { new Color(.3f,.92f,1), new Color(.3f,.92f,1), new Color(.45f,1,.72f), new Color(1,.72f,.22f), new Color(.75f,.35f,1), CrimsonMasteryVisuals.Arc };
            for (uint skin=0;skin<6;skin++)
            {
                body.skinIndex=skin;fx.BeginCast(1004+skin,2,5,false,0);fx.LateUpdate();
                Color chosen=GazeContrastAssets.Accent((int)skin), baseColor=primary[skin];
                Check(Math.Abs(fx.accent.r-chosen.r)<.001f&&Math.Abs(fx.accent.b-chosen.b)<.001f,"reused pool recolors for skin");
                float distance=(float)Math.Sqrt(Math.Pow(chosen.r-baseColor.r,2)+Math.Pow(chosen.g-baseColor.g,2)+Math.Pow(chosen.b-baseColor.b,2));
                float baseValue=.2126f*baseColor.r+.7152f*baseColor.g+.0722f*baseColor.b;
                float accentValue=.2126f*chosen.r+.7152f*chosen.g+.0722f*chosen.b;
                Check(distance>.65f,"secondary RGB separation all skins");
                Check(Math.Abs(baseValue-accentValue)>.12f,"secondary value separation all skins");
                Check(fx.fuel[0].stroke.line.positionCount==5&&fx.fuel[0].outline.line.enabled,"outlined diamond fuel");
                fx.SetReserve(1004+skin,1);fx.LateUpdate();
                Check(fx.reserve[0].stroke.line.positionCount==17,"round reserve differs without color");
                Check(fx.ReadabilityFocus==0f,"no focus at rest");
                fx.Swallow(1004+skin,0,0,0,.32f);Time.time+=.1f;
                Check(fx.ReadabilityFocus>.95f,"focus available before renderer update");
                fx.LateUpdate();Check(fx.intakeOutline.line.enabled,"outlined intake trail");
                fx.LaunchPulse(1004+skin,0,Vector3.up,new Vector3(0,0,10),new Vector3(0,0,10),Vector3.up,true,0,.2f,4,.3f,false);
                fx.ReducedEffects=true;Time.time+=.1f;fx.LateUpdate();
                Check(fx.pulses[0].front.line.enabled&&fx.pulses[0].outline.line.enabled,"reduced effects keep pulse silhouette");
                Time.time+=.2f;fx.LateUpdate();
                Check(fx.pulses[0].roots[0].stroke.line.enabled&&!fx.pulses[0].roots[1].stroke.line.enabled,"reduced effects omit secondary roots");
                fx.EndCast(1004+skin,1,1,2,EndReason.Cancelled);
                Check(fx.ReadabilityFocus==0f&&!fx.pulses[0].outline.line.enabled,"cancel restores baseline and clears outlines");
                fx.Clear();fx.ReducedEffects=false;
            }
            Check(GameObject.Created==allocated,"all skins reuse same geometry");
            Check(!System.Object.ReferenceEquals(GazeContrastAssets.Glow,HollowSaint.FoundationKit.Vfx.VfxAssets.ArcGlow),"accent owns material clone");
            var neutral=GazeContrastAssets.Glow.Textures["_RemapTex"];
            foreach(var c in neutral.pixels)Check(c.r==c.g&&c.g==c.b,"neutral ramp cannot reimpose primary hue");
            Check(GazeContrastAssets.Glow.Colors["_TintColor"].g==1f,"neutral material does not multiply primary tint");
            fx.BeginCast(2000,5,5,true,0);
            Time.time+=30f;fx.LateUpdate();
            Check(fx.CrownExpansion==0f&&fx.CrownApertureScale==1f&&fx.nextPulse==0,"manual aim hold never auto-fires or opens crown");
            fx.Swallow(2000,0,0,0,.32f);Time.time+=.24f;
            Check(fx.CrownExpansion>.7f&&fx.CrownApertureScale>1.4f,"actual intake opens pose-owner envelope");
            fx.LaunchPulse(2000,0,Vector3.zero,Vector3.forward*20,Vector3.forward*20,Vector3.up,true,0,.5f,4,.3f,false);
            Check(fx.CrownExpansion==1f&&Math.Abs(fx.CrownApertureScale-1.6f)<.001f,"launch peaks crown aperture");
            Time.time+=.25f;fx.LateUpdate();
            var slow=fx.pulses[0];
            Check(Vector3.Distance(slow.line[11],Vector3.forward*10)<.01f,"caller supplied half-second travel stays synchronized");
            Check(slow.sleeve.line.widthMultiplier>3.8f,"fat pulse geometry");
            Check(fx.CrownExpansion==0f,"crown recovers while pulse travels");
            Check(!slow.roots[0].stroke.line.enabled,"ground burst waits for supplied arrival");
            Time.time+=.25f;fx.LateUpdate();Check(!slow.sleeve.line.enabled&&slow.front.line.enabled,"arrival punctuation begins at arrival");
            fx.Swallow(2000,1,1,0,.32f);Time.time+=.24f;
            Check(fx.CrownExpansion>.7f,"another manual event opens independently");
            body.healthComponent.alive=false;Check(fx.CrownExpansion==0f&&fx.CrownApertureScale==1f,"death immediately releases pose hook");
            body.healthComponent.alive=true;fx.Clear();Check(fx.CrownExpansion==0f,"clear releases pose hook");
            fx.BeginCast(3000,20,20,true,0);
            Check(fx.pulses.Length==5&&fx.contacts.Length==24,"bounded manual overlap pools");
            for(int eventIndex=0;eventIndex<20;eventIndex++)
            {
                Time.time+=.25f;
                fx.LaunchPulse(3000,eventIndex,Vector3.up,Vector3.forward*20,Vector3.forward*20,Vector3.up,true,0,.55f,4,.3f,false);
                fx.LateUpdate();
                int livePulses=0;
                foreach(var pulse in fx.pulses)if(pulse.active)livePulses++;
                Check(livePulses>=Math.Min(eventIndex+1,4),"manual cadence keeps overlapping pulses");
                for(int target=0;target<8;target++)fx.ConfirmStrike(3000,eventIndex*8+target,new Vector3(target,0,20),0);
                fx.LateUpdate();
                Check(GameObject.Created==allocated,"twenty manual pulses allocate no geometry");
            }
            fx.OnDisable();
            foreach(var pulse in fx.pulses)Check(!pulse.active,"disable clears all manual pulse slots");
            foreach(var contact in fx.contacts)Check(!contact.active,"disable clears all manual contacts");
            Console.WriteLine("PASS "+checks+" assertions; 1000 cast reuse; production presentation files with physics/render substitutes. Not Unity runtime validation.");
        }
    }
}
