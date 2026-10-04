using System;
using HollowSaint.FoundationKit.Gaze;
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
    static void Main()
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
        var seq=new GazeFuelSequence();Check(!seq.Accept(1,2,false,false),"orphan launch rejected");
        Check(seq.Accept(1,1,true,false)&&seq.Accept(1,2,false,false)&&!seq.Accept(1,2,false,false),"ordered and duplicate packets");
        Check(seq.Accept(1,4,false,true)&&!seq.Accept(1,5,false,false)&&!seq.Accept(1,1,true,false),"retired cannot resurrect");
        Check(seq.Accept(2,8,false,true)&&!seq.Accept(2,1,true,false),"end before missing begin retires");
        Check(seq.Accept(3,1,true,false),"new cast starts");seq.Retire();Check(!seq.Accept(3,2,false,false),"disable rejects late traffic");
        Console.WriteLine("PASS manual edges, owner/cast/sequence/rate/deadline, duration, cancellation boundary and event retirement");
        Console.WriteLine("PASS "+checks+" production assertions");
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
