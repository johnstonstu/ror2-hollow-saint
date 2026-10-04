using System;
using HollowSaint.FoundationKit.Gaze;
using HollowSaint.FoundationKit.Gaze.Fx;
// Actual production policy/ledger/queue checks. Unity physics and delivery need playtesting.
static class Program
{
    static int checks;
    static void Check(bool pass,string name) { checks++; if(!pass)throw new Exception("FAIL "+name); }
    static void Resolve(GazeFuelSchedule plan,GazeFuelLedger ledger,float age)
    {
        while(plan.TakeLaunch(age,out _)) {
            Check(ledger.TrySpend(1),"one entry orb per launch"); ledger.TryGain();
            Check(ledger.Entry+ledger.AcceptedGains==ledger.Spent+ledger.Unspent+ledger.Reserve,"launch conservation");
        }
    }
    static int Main()
    {
        try { Run(); return 0; }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    static void Run()
    {
        foreach(int cap in new[]{2,5,6,20}) {
            for(int entry=0;entry<=cap;entry++) {
                var bank=new GazeFuelLedger();bank.Begin(entry,cap);
                var plan=new GazeFuelSchedule();plan.Begin(entry);
                var gate=new GazeManualRequestPolicy();gate.Begin(42);
                while(bank.TryGain()){}
                for(int orb=0;orb<entry;orb++) {
                    float tap=1f+orb*.25f;Resolve(plan,bank,tap);
                    Check(gate.TryAccept(42,(uint)orb+1,true,true,tap,1f,7f,bank.Unspent-plan.PendingCount),"all twenty fit six seconds");
                    int spent=bank.Spent;
                    Check(plan.QueueIntake(tap,out int phase)&&phase==orb,"one intake per tap");
                    Check(bank.Spent==spent,"intake is refundable, not spent");
                    Check(plan.Count<=20&&plan.PendingCount<=2,"bounded count and overlap");
                }
                Resolve(plan,bank,6.1f);
                Check(bank.Spent==entry&&bank.Unspent==0,"all requested entry launches once");
                Check(!plan.QueueIntake(9f,out _)&&!bank.TrySpend(1),"earned reserve never entry fuel");
                Check(Math.Abs(entry*GazeFuelSchedule.Coefficient(1,cap)-2.5f*entry/cap)<.0001f,"normalized full bank 2.5");
                int gains=bank.AcceptedGains,spentTotal=bank.Spent;int retained=bank.End(true);
                Check(entry+gains==spentTotal+retained,"living conservation");
                Check(bank.HoldAfterMerge==(retained>=cap),"only full merge holds passive");
                Check(bank.RejectedGains>0&&!bank.TryGain(),"overflow counted and closed ledger immutable");
            }
            for(int cutOrb=0;cutOrb<cap;cutOrb++)foreach(float offset in new[]{-.01f,.16f,.34f,.95f}) {
                var bank=new GazeFuelLedger();bank.Begin(cap,cap);
                var plan=new GazeFuelSchedule();plan.Begin(cap);float cut=1f+cutOrb*.25f+offset;
                for(int orb=0;orb<=cutOrb;orb++) {
                    float tap=1f+orb*.25f;if(tap>cut)break;Resolve(plan,bank,tap);
                    Check(plan.QueueIntake(tap,out _),"cancellation fixture intake");
                }
                Resolve(plan,bank,cut);int spent=bank.Spent,gained=bank.AcceptedGains;plan.Cancel();int refund=bank.End(true);
                Check(!plan.TakeLaunch(100f,out _)&&!plan.QueueIntake(100f,out _),"cancel blocks pending and future launches");
                Check(cap+gained==spent+refund&&bank.Spent==spent,"unlaunched refund and launched remain spent");
            }
            var dead=new GazeFuelLedger();dead.Begin(cap,cap);Check(dead.TrySpend(1)&&dead.TryGain(),"death fixture");
            Check(dead.End(false)==0&&!dead.HoldAfterMerge,"death discards unspent and reserve");
            Console.WriteLine("PASS capacity="+cap+" all entry counts, manual queue, conservation, every-orb cancellation and death");
        }
        var changed=new GazeFuelLedger();changed.Begin(20,20);Check(changed.End(true)==20&&changed.HoldAfterMerge,"retained twenty held");
        changed.Begin(20,2);Check(changed.Capacity==20&&changed.Entry==20,"lowered config preserves bank");
        Check(changed.TrySpend(18)&&changed.End(true)==2,"old capacity until bank decreases");changed.Begin(2,2);Check(changed.Capacity==2,"new lower cap applies");
        Check(GazeFuelLedger.ClampCapacity(-1)==2&&GazeFuelLedger.ClampCapacity(99)==20,"capacity bounds");
        Check(Math.Abs(GazeFuelSchedule.Travel(0)-.35f)<.0001f&&Math.Abs(GazeFuelSchedule.Travel(60)-.55f)<.0001f&&GazeFuelSchedule.Travel(600)==GazeFuelSchedule.Travel(60),"shared readable travel");
        Check(Math.Abs(GazeFuelSchedule.SpreadRadius(0,4,8,.4f,1.6f)-3.2f)<.0001f&&Math.Abs(GazeFuelSchedule.SpreadRadius(4,4,8,.4f,1.6f)-12.8f)<.0001f,"radius in metres");
        Check(Math.Abs(GazeFuelSchedule.StrikeAt(1,.55f,8,8)-1.85f)<.0001f,"travel plus ground arrival");
        InputChecks();
        ExtensionChecks();
        RampChecks();
        WidthChecks();
        Check(GazeTimerPolicy.Fill(0)==0 && GazeTimerPolicy.Fill(-1)==0 && GazeTimerPolicy.Fill(14)==1 && GazeTimerPolicy.Fill(30)==1,"timer clamps to actual fourteen-second scale");
        Check(GazeTimerPolicy.Fill(float.NaN)==0 && GazeTimerPolicy.Fill(float.PositiveInfinity)==0,"timer rejects nonfinite durations");
        Check(GazeTimerPolicy.Fill(6)>GazeTimerPolicy.Fill(4) && GazeTimerPolicy.Fill(3)<GazeTimerPolicy.Fill(4),"successful extension grows meter and elapsed time drains it");
        var audioSequence=new GazeFuelSequence(); int launchSounds=0;
        foreach(var packet in new[]{(seq:1u,begin:true,end:false,launch:false),(seq:2u,begin:false,end:false,launch:false),(seq:3u,begin:false,end:false,launch:true),(seq:3u,begin:false,end:false,launch:true),(seq:4u,begin:false,end:true,launch:false),(seq:5u,begin:false,end:false,launch:true)})
            if(audioSequence.Accept(88,packet.seq,packet.begin,packet.end)&&packet.launch)launchSounds++;
        Check(launchSounds==1,"only accepted launch plays pulse cue; intake duplicate and retired cast do not");
        var seq=new GazeFuelSequence();Check(!seq.Accept(1,2,false,false),"orphan launch rejected");
        Check(seq.Accept(1,1,true,false)&&seq.Accept(1,2,false,false)&&!seq.Accept(1,2,false,false),"ordered and duplicate packets");
        Check(seq.Accept(1,4,false,true)&&!seq.Accept(1,5,false,false)&&!seq.Accept(1,1,true,false),"retired cannot resurrect");
        Check(seq.Accept(2,8,false,true)&&!seq.Accept(2,1,true,false),"end before missing begin retires");
        Check(seq.Accept(3,1,true,false),"new cast starts");seq.Retire();Check(!seq.Accept(3,2,false,false),"disable rejects late traffic");
        Console.WriteLine("PASS manual edges, owner/cast/sequence/rate/deadline, duration, cancellation boundary and event retirement");
        Console.WriteLine("PASS "+checks+" production assertions");
    }
    static void WidthChecks()
    {
        Func<float,float,float,float>[] widths={GazeBeamWidthPolicy.Body,GazeBeamWidthPolicy.Haze,GazeBeamWidthPolicy.Sheath,GazeBeamWidthPolicy.Core};
        float[] baseline={.55f,.8f,.7f,.075f},full={1.05f,1.3f,1.2f,.115f};
        for(int layer=0;layer<widths.Length;layer++) {
            var width=widths[layer];
            Check(Math.Abs(width(0,1,1.5f)-baseline[layer])<.0001f&&Math.Abs(width(5,1,1.5f)-full[layer])<.0001f,"each settled layer matches baseline and five-step width");
            for(int step=1;step<=5;step++)Check(width(step,1,1.5f)>width(step-1,1,1.5f),"each layer grows monotonically through five steps");
            Check(width(20,1,1.5f)==width(5,1,1.5f)&&width(-1,1,1.5f)==width(0,1,1.5f),"width clamps absolute steps");
            Check(width(2.5f,1,1.5f)>width(2,1,1.5f)&&width(2.5f,1,1.5f)<width(3,1,1.5f),"fractional shown steps interpolate continuously");
            foreach(float radius in new[]{.5f,1.5f,4f})foreach(float envelope in new[]{1.32f,1.52f}) {
                float previous=0;
                for(int step=0;step<=5;step++) {
                    float shown=width(step,envelope,radius);
                    Check(shown>=previous&&shown<=radius*2&&shown>0&&!float.IsInfinity(shown),"all envelopes stay within configured hit diameter");
                    previous=shown;
                }
            }
            foreach(float invalid in new[]{-1f,0f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
                Check(width(5,invalid,1.5f)==0&&width(5,1,invalid)==0,"invalid radius or envelope produces no width");
            Check(width(float.NaN,1,1.5f)==baseline[layer]&&width(float.PositiveInfinity,1,1.5f)==baseline[layer],"invalid steps safely show baseline");
        }
        Check(GazeBeamWidthPolicy.Advance(0,5,.1f)==.4f&&GazeBeamWidthPolicy.Advance(4.9f,5,1)==5,"smooth rise rate and target clamp");
        Check(GazeBeamWidthPolicy.Advance(5,0,.1f)==4.6f&&GazeBeamWidthPolicy.Advance(.1f,0,1)==0,"smooth fall without undershoot");
        foreach(float dt in new[]{-1f,0f,float.NaN,float.PositiveInfinity})Check(GazeBeamWidthPolicy.Advance(2,5,dt)==2,"negative or invalid dt never advances width");
        Check(GazeBeamWidthPolicy.Advance(float.NaN,5,.1f)==.4f&&GazeBeamWidthPolicy.Advance(99,20,1)==5&&GazeBeamWidthPolicy.Advance(-5,-1,1)==0,"invalid shown state and extreme targets bounded");
        Console.WriteLine("PASS actual width policy endpoints, fractional smoothing, finite guards and diameter bounds at three radii");
    }
    static void RampChecks()
    {
        Check(GazeRampPolicy.Steps(-1)==0&&GazeRampPolicy.DamageMultiplier(0)==1,"uncharged cast starts without ramp");
        Check(GazeRampPolicy.Steps(int.MaxValue)==5&&GazeRampPolicy.DamageMultiplier(20)==1.25f,"ramp bounded at twenty-five percent");
        var bank=new GazeFuelLedger();bank.Begin(20,20);
        var plan=new GazeFuelSchedule();plan.Begin(20);
        for(int orb=0;orb<20;orb++) {
            float tap=1+orb*.25f;
            while(plan.TakeLaunch(tap,out _))Check(bank.TrySpend(1),"ramp fixture launch spends once");
            int before=GazeRampPolicy.Steps(bank.Spent);
            Check(plan.QueueIntake(tap,out _)&&GazeRampPolicy.Steps(bank.Spent)==before,"intake alone never grants ramp");
            Check(GazeRampPolicy.Steps(bank.Spent)==Math.Min(5,bank.Spent),"absolute launch count saturates at five");
        }
        while(plan.TakeLaunch(7,out _))Check(bank.TrySpend(1),"remaining ramp fixture launches");
        Check(bank.Spent==20&&GazeRampPolicy.DamageMultiplier(bank.Spent)==1.25f,"twenty successful pulses remain capped at five ramp steps");
        Check(!bank.TrySpend(1)&&GazeRampPolicy.Steps(bank.Spent)==5,"empty launch cannot advance ramp");
        for(int step=0;step<=5;step++) {
            float multiplier=GazeRampPolicy.DamageMultiplier(step);
            Check(Math.Abs(multiplier-(1+step*.05f))<.0001f,"five percent per confirmed launch");
        }
        Check(bank.Spent*GazeFuelSchedule.Coefficient(1,20)==2.5f,"full fuel damage remains normalized independently of ramp");
        bank.End(true);bank.Begin(5,5);plan.Begin(5);
        Check(bank.Spent==0&&GazeRampPolicy.Steps(bank.Spent)==0,"new cast resets successful count");
        plan.QueueIntake(1,out _);plan.Cancel();
        Check(!plan.TakeLaunch(2,out _)&&bank.End(true)==5&&GazeRampPolicy.Steps(bank.Spent)==0,"cancelled intake refunds without ramp");
        bank.Begin(5,5);Check(bank.TrySpend(1)&&bank.TryGain(),"reserve ramp fixture");
        Check(GazeRampPolicy.Steps(bank.Spent)==1&&bank.Reserve==1,"reserve gain does not grant a ramp step");
        bank.End(false);bank.Begin(5,5);
        Check(GazeRampPolicy.Steps(bank.Spent)==0,"death then new cast resets ramp");
        var packets=new GazeFuelSequence();int acknowledged=0;
        Check(packets.Accept(70,1,true,false),"ramp network begin");
        if(packets.Accept(70,2,false,false))acknowledged=GazeRampPolicy.Steps(1);
        Check(!packets.Accept(70,2,false,false)&&acknowledged==1,"duplicate launch cannot increment ramp");
        if(packets.Accept(70,3,false,false))acknowledged=GazeRampPolicy.Steps(4);
        Check(acknowledged==4,"absolute count catches up without per-packet increments");
        Check(packets.Accept(70,4,false,true),"ramp network end");acknowledged=0;
        Check(!packets.Accept(70,5,false,false)&&!packets.Accept(70,1,true,false)&&acknowledged==0,"retired launch cannot restore ramp");
        Check(packets.Accept(71,1,true,false)&&acknowledged==0,"next cast begins at zero");packets.Retire();
        Check(!packets.Accept(71,2,false,false),"disabled body cannot apply late ramp");
        Console.WriteLine("PASS launch-only ramp, five-step cap, cancelled intake, reserve exclusion, reset and idempotent snapshots");
    }
    static void ExtensionChecks()
    {
        foreach(float baseline in new[]{4f,5.9f,6f}) {
            var bank=new GazeFuelLedger();bank.Begin(20,20);
            Check(GazeLaunchDurationPolicy.Duration(baseline,bank.Spent)==baseline,"no entry/no launch grants no time");
            for(int launch=1;launch<=20;launch++) {
                Check(bank.TrySpend(1),"extension fixture spends one entry orb");
                float duration=GazeLaunchDurationPolicy.Duration(baseline,bank.Spent);
                Check(Math.Abs(duration-Math.Min(14,baseline+launch*2))<.0001f&&duration<=14,"successful launch earns two seconds up to fourteen");
            }
            float earned=GazeLaunchDurationPolicy.Duration(baseline,bank.Spent);
            Check(!bank.TrySpend(1)&&GazeLaunchDurationPolicy.Duration(baseline,bank.Spent)==earned,"no ammunition cannot earn extension");
        }
        Check(GazeLaunchDurationPolicy.Duration(4,5)==14&&GazeLaunchDurationPolicy.Duration(6,4)==14,"default five and level twenty-one four pulses reach fourteen");
        Check(GazeLaunchDurationPolicy.Progress(3,4)==.75f&&GazeLaunchDurationPolicy.Duration(4,1)==6&&GazeLaunchDurationPolicy.Progress(4,4)==1,"growth keeps frozen baseline while lifetime extends");
        Check(GazeFuelSchedule.SpreadRadius(3,4,8,.4f,1.6f)==8* (.4f+1.2f*.75f),"spread retains frozen baseline radius at launch");
        Check(GazeLaunchDurationPolicy.CanAdmit(4.6f,5,1,0)&&!GazeManualRequestPolicy.HasArrivalRoom(4.6f,5,true),"late intake can use own prospective grant");
        Check(GazeLaunchDurationPolicy.CanAdmit(4.62f,5,1,0)&&!GazeLaunchDurationPolicy.CanAdmit(4.64f,5,1,0),"intake must finish before actual end with fixed-step safety");
        Check(!GazeLaunchDurationPolicy.CanAdmit(4.9f,5,1,0),"prospective time cannot bridge intake beyond actual end");
        Check(!GazeLaunchDurationPolicy.CanAdmit(12.6f,13,1,1)&&!GazeLaunchDurationPolicy.CanAdmit(10.6f,11,1,2),"queued intakes reserve headroom without supplying unearned time");
        Check(!GazeLaunchDurationPolicy.CanAdmit(5,5,1,0)&&!GazeLaunchDurationPolicy.CanLaunch(5,5,1),"expired cast cannot revive via prospective extension");
        Check(GazeLaunchDurationPolicy.CanLaunch(14.05f,14.9f,1)&&!GazeManualRequestPolicy.HasArrivalRoom(14.05f,14.9f,false)&&!GazeLaunchDurationPolicy.CanLaunch(14.11f,15,1),"fractional last grant can finish arrival cap grants none");
        Check(Math.Abs(GazeLaunchDurationPolicy.Duration(5.9f,4)-13.9f)<.0001f&&GazeLaunchDurationPolicy.Duration(5.9f,5)==14,"fractional base receives final point-one second");
        Check(GazeLaunchDurationPolicy.CanAdmit(13.75f,14.9f,1,0)&&!GazeLaunchDurationPolicy.CanAdmit(13.75f,14.9f,1,1),"fractional remaining headroom cannot be borrowed twice");
        Check(GazeLaunchDurationPolicy.CanAdmit(13.77f,15,1,0)&&!GazeLaunchDurationPolicy.CanAdmit(13.79f,15,1,0),"at cap intake requires full unextended arrival horizon");
        Check(!GazeLaunchDurationPolicy.CanLaunch(5.01f,5,1),"hitch past actual expiry cannot spend");
        var pending=new GazeFuelSchedule();pending.Begin(5);var cancelled=new GazeFuelLedger();cancelled.Begin(5,5);pending.QueueIntake(3.9f,out _);
        Check(GazeLaunchDurationPolicy.Duration(4,cancelled.Spent)==4,"acknowledged intake alone grants no time");pending.Cancel();
        Check(!pending.TakeLaunch(4.3f,out _)&&cancelled.End(true)==5&&GazeLaunchDurationPolicy.Duration(4,cancelled.Spent)==4,"cancelled intake refunds without extension");
        var hitched=new GazeFuelLedger();hitched.Begin(5,5);
        if(GazeLaunchDurationPolicy.CanLaunch(5.1f,5,1))hitched.TrySpend(1);
        Check(hitched.Spent==0&&hitched.End(true)==5&&GazeLaunchDurationPolicy.Duration(4,hitched.Spent)==4,"late hitch keeps orb refundable and duration unchanged");
        Check(GazeDurationPolicy.ValidSnapshot(6)&&!GazeDurationPolicy.ValidSnapshot(14)&&GazeLaunchDurationPolicy.ValidActualDuration(14)&&!GazeLaunchDurationPolicy.ValidActualDuration(14.01f)&&!GazeLaunchDurationPolicy.ValidActualDuration(float.NaN),"frozen baseline six distinct from validated actual fourteen");
        Console.WriteLine("PASS launch-only bounded extension, pending budget, cancellation and worst-arrival deadlines");
    }
    static void InputChecks()
    {
        var edge=new GazeTapEdges();edge.Begin(true);for(int i=0;i<1000;i++)Check(!edge.Observe(true),"initial held press never autorepeats");
        Check(!edge.Observe(false)&&edge.Observe(true)&&!edge.Observe(true),"one release-press edge");
        var gate=new GazeManualRequestPolicy();gate.Begin(7);
        Check(!gate.TryAccept(7,1,false,true,1,1,7,20),"wrong owner connection");
        Check(!gate.TryAccept(8,1,true,true,1,1,7,20),"wrong cast");
        Check(!gate.TryAccept(7,1,true,false,1,1,7,20),"dead or noncurrent state");
        Check(gate.TryAccept(7,1,true,true,1,1,7,20),"unauthorized sequence does not poison owner");
        Check(!gate.TryAccept(7,1,true,true,1.5f,1,7,19),"accepted replay rejected");
        Check(!gate.TryAccept(7,2,true,true,1.24f,1,7,19)&&!gate.TryAccept(7,2,true,true,1.5f,1,7,19),"mash and rejected replay blocked");
        Check(gate.TryAccept(7,3,true,true,1.25f,1,7,19),"250ms boundary accepted");
        Check(!gate.TryAccept(7,4,true,true,6,1,7,19),"late request rejected before spend");
        Check(!gate.TryAccept(7,5,true,true,2,1,7,0),"empty entry rejected");gate.Cancel();Check(!gate.TryAccept(7,6,true,true,2.5f,1,7,19),"ended request gate");
        var early=new GazeManualRequestPolicy();early.Begin(1);Check(!early.TryAccept(1,1,true,true,.9f,1,5,5),"windup request rejected");
        Check(!early.TryAccept(1,2,true,true,float.NaN,1,5,5),"invalid clock rejected");
        Check(GazeDurationPolicy.ForLevel(4,1)==4&&GazeDurationPolicy.ForLevel(4,11)==5&&GazeDurationPolicy.ForLevel(4,21)==6&&GazeDurationPolicy.ForLevel(4,1000)==6,"bounded level curve");
        Check(GazeDurationPolicy.ForLevel(10,1)==6&&GazeDurationPolicy.ForLevel(-5,1)==1&&GazeDurationPolicy.ForLevel(float.NaN,1)==4,"config hard cap and invalid fallback");
        Check(!GazeDurationPolicy.ValidSnapshot(7)&&!GazeDurationPolicy.ValidSnapshot(float.NaN)&&GazeDurationPolicy.ValidSnapshot(6),"network duration validation");
        var twenty=new GazeManualRequestPolicy();twenty.Begin(2);for(int i=0;i<20;i++)Check(twenty.TryAccept(2,(uint)i+1,true,true,1+i*.25f,1,7,20-i),"twenty taps retain complete arrival margin");
        var pending=new GazeFuelSchedule();pending.Begin(5);pending.QueueIntake(1,out _);var bank=new GazeFuelLedger();bank.Begin(5,5);
        // Native interrupt invokes OnExit directly, which cancels the pending queue.
        pending.Cancel();
        Check(bank.Spent==0&&bank.End(true)==5&&!pending.TakeLaunch(2,out _),"cancel boundary refunds intake before spending");
        Check(GazeManualLifetime.StopBeforeWork(5,5)&&!GazeManualLifetime.StopBeforeWork(4.99f,5),"natural end stops work at boundary");
        var primary=new GazePrimaryTapGate();primary.Begin(true);
        Check(!primary.Observe(true)&&!primary.Take(true),"held Primary on entry never pulses");
        Check(!primary.Observe(false)&&primary.Observe(true)&&primary.Take(true)&&!primary.Take(true),"poll before native execute fires once");
        primary.Observe(false);Check(primary.Take(true)&&!primary.Observe(true),"native execute before state poll fires once");
        Check(!primary.Take(false),"release never pulses");
        Check(GazeManualRequestPolicy.HasArrivalRoom(3.77f,5,true)&&!GazeManualRequestPolicy.HasArrivalRoom(3.79f,5,true),"intake admission includes safety and complete arrival");
        var delayed=new GazeFuelLedger();delayed.Begin(5,5);
        if(GazeManualRequestPolicy.HasArrivalRoom(4.2f,5,false))delayed.TrySpend(1);
        Check(delayed.Spent==0&&delayed.End(true)==5,"deferred intake cannot spend into impossible arrival");
    }
}
