using HollowSaint.FoundationKit;
using HollowSaint.FoundationKit.Gaze;
using UnityEngine;
using UnityEngine.Networking;

static partial class Program
{
    static void GatherIncome()
    {
        // Success: gather income remains visible reserve during Gaze's beam,
        // cannot fuel this cast, and merges exactly on cancel/completion. A new
        // gather replaces a canceled snapshot; clients cannot author the snapshot.
        foreach (bool alive in new[]{true,false}) foreach (int spend in new[]{0,1,2})
        {
            Reset(); var body=Body(); var meter=body.GetComponent<DischargeMeter>();
            meter.RegisterForTest(2); meter.SnapshotGazeGather();
            Time.time+=1; meter.AddCharge();
            Time.time+=1; meter.AddCharge();
            Check(meter.Charge==4,"bolts bank two charges during Gaze gather");
            var ledger=new GazeFuelLedger(); meter.ClaimGazeFuel(ledger);
            Check(ledger.Entry==2 && ledger.Reserve==2 && meter.Charge==2,"beam claims frozen entry and keeps gather income visible");
            Check(!ledger.TrySpend(3),"new gather income cannot be spent as entry fuel");
            if (spend>0) Check(ledger.TrySpend(spend),"opening can spend original entry");
            Time.time+=1; meter.AddCharge();
            Check(ledger.Reserve==3 && meter.Charge==3,"beam income joins reserve without changing original entry");
            Check(meter.ReleaseGazeFuel(alive)==(alive?5-spend:0),"exit conserves unspent entry plus reserve only for a living owner");
        }
        Reset(); var next=Body(); var nextMeter=next.GetComponent<DischargeMeter>();
        nextMeter.RegisterForTest(1); nextMeter.SnapshotGazeGather();
        // A canceled gather leaves its bank intact; the next fresh entry snapshots again.
        nextMeter.RegisterForTest(3); nextMeter.SnapshotGazeGather();
        nextMeter.RegisterForTest(4);
        var nextLedger=new GazeFuelLedger(); nextMeter.ClaimGazeFuel(nextLedger);
        Check(nextLedger.Entry==3 && nextLedger.Reserve==1,"new gather supersedes canceled snapshot");
        nextMeter.ReleaseGazeFuel(true);
        NetworkServer.active=false; nextMeter.SnapshotGazeGather();
        NetworkServer.active=true; nextMeter.RegisterForTest(5); nextMeter.ClaimGazeFuel(nextLedger);
        Check(nextLedger.Entry==5 && nextLedger.Reserve==0,"snapshot is consumed once and clients cannot replace it");
        nextMeter.ReleaseGazeFuel(true);

        Reset(); var cloudBody=Body(); var cloudMeter=cloudBody.GetComponent<DischargeMeter>();
        cloudMeter.RegisterForTest(2); var cloud=State(cloudBody,0,true);
        cloudBody.inputBank.skill1.down=true; cloudBody.inputBank.skill4.down=true;
        cloud.Update();
        Check(!cloudBody.inputBank.skill1.hasPressBeenClaimed,"Thundercloud update leaves held Primary unclaimed");
        Time.time+=1; cloudMeter.AddCharge(); Time.time+=1; cloudMeter.AddCharge();
        cloud.Age(2); cloud.FixedUpdate();
        var writer=new NetworkWriter(); cloud.OnSerialize(writer);
        var reader=new NetworkReader(writer.Stream.ToArray()); uint token=reader.ReadUInt32();
        var request=Request(0,token); request.count=5; cloud.ServerRequest(request,null,true);
        Check(cloud.Released && cloudBody.GetComponent<HollowSaint.FoundationKit.ChargedStorm.StoredChargeDriver>().spent==2 && cloudMeter.Charge==2,
            "Thundercloud release spends frozen entry and banks new gather income");
        cloud.OnExit();
    }
}
