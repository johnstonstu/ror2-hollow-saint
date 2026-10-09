using System;
using RoR2;
using HollowSaint.FoundationKit.OpenCircuit;

// Closed Circuit: fed charges refund one per nearby Electrocute, never beyond what
// was fed; the remainder is owed to closing strikes. Ledger only; the runtime
// driver (targeting, effects, bank) needs gameplay acceptance.
static partial class Program
{
    static void IncomeGuard()
    {
        var g=new HollowSaint.FoundationKit.Storm.ChargeIncomeBucket();
        Check(g.TryTake(2,0) && g.TryTake(2,0) && !g.TryTake(2,0),"burst of two banks, third waits");
        Check(!g.Ready(2,.4f) && g.Ready(2,.5f) && g.TryTake(2,.5f) && !g.TryTake(2,.5f),"refills at the configured rate");
        Check(g.TryTake(2,10) && g.TryTake(2,10) && !g.TryTake(2,10),"long idle never stores more than one burst");
        var off=new HollowSaint.FoundationKit.Storm.ChargeIncomeBucket();
        for(int i=0;i<20;i++) Check(off.TryTake(0,0),"zero rate disables the guard");
        var slow=new HollowSaint.FoundationKit.Storm.ChargeIncomeBucket();
        Check(slow.TryTake(.5f,0) && !slow.TryTake(.5f,1) && slow.TryTake(.5f,2),"sub-one rates still allow single charges");
        Check(new HollowSaint.FoundationKit.Storm.ChargeIncomeBucket().TryTake(float.NaN,0),"invalid rate fails open");
    }
    static void ClosedCircuit()
    {
        IncomeGuard();
        var l=new ClosedCircuitLedger();
        Check(!l.TryRefund() && l.Owed==0,"no refund without an open circuit");
        Check(l.Begin(3)==0 && l.Active && l.Owed==3,"three fed charges are owed");
        Check(l.TryRefund() && l.TryRefund() && l.Owed==1,"each Electrocute refunds one");
        Check(l.TryRefund() && !l.TryRefund() && l.Owed==0,"refunds never exceed fed charges");
        Check(l.Close()==0 && !l.Active,"fully refunded circuit closes with nothing owed");
        l.Begin(5);l.TryRefund();
        Check(l.Begin(2)==4 && l.Owed==2 && l.Refunded==0,"recast discharges the old window's debt and starts fresh");
        Check(l.Close()==2 && l.Close()==0,"close pays out once");
        l.Begin(4);l.Cancel();Check(l.Owed==0 && l.Close()==0,"death/stage cancel owes nothing");
        Check(l.Begin(0)==0 && !l.Active,"zero fed opens no ledger");
        Check(l.Begin(50)==0 && l.Fed==20,"fed count is bounded");l.Cancel();

        int seen=-1; CharacterBody who=null;
        Action<CharacterBody,int> spy=(b,c)=>{who=b;seen=c;};
        OpenCircuitBuff.Opened+=spy;
        try
        {
            Reset();var b=Body();OpenCircuitBuff.Open(b,3);
            Check(who==b && seen==3,"opening a paid crown reports its fed charges");
        }
        finally { OpenCircuitBuff.Opened-=spy; }
    }
}
