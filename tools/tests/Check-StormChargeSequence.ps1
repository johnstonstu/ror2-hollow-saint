# Success: charge cues can be keyed to accepted revisions, duplicate/stale packets
# cannot replay release or cancel a newer gather, and cancellation restores orbs.
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = [IO.File]::ReadAllText((Join-Path $repo 'HollowSaintMod\FoundationKit\Storm\StormChargeSequence.cs'))
$checks = @'
namespace HollowSaint.FoundationKit.Vfx
{
    public enum Beat { ThunderGather = 31, ThunderRelease = 32, ThunderCancel = 33 }
}
namespace HollowSaint.FoundationKit.Storm
{
    public static class ChargeSequenceChecks
    {
        private static void Check(bool ok, string message)
        { if (!ok) throw new System.Exception(message); }
        public static string Run()
        {
            var sequence = new StormChargeSequence();
            Check(!sequence.Receive(Vfx.Beat.ThunderGather, .35f, 1, 10f), "Gather released early");
            Check(sequence.LastRevision == 1, "Accepted gather cannot be distinguished for audio");
            sequence.Tick(10.175f, .175f, true);
            Check(sequence.GatherAmount > .49f && sequence.GatherAmount < .51f, "Gather lost its timing");
            Check(sequence.Receive(Vfx.Beat.ThunderRelease, 0, 2, 10.35f), "Launch rejected");
            Check(!sequence.Receive(Vfx.Beat.ThunderRelease, 0, 2, 10.36f), "Duplicate launch replayed");
            Check(sequence.LastRevision == 2 && sequence.Released(10.4f), "Duplicate changed release state");
            sequence.Receive(Vfx.Beat.ThunderGather, .35f, 3, 11f);
            sequence.Tick(11.175f, .175f, true);
            sequence.Receive(Vfx.Beat.ThunderCancel, 0, 1, 11.176f);
            sequence.Tick(11.2f, .025f, true);
            Check(sequence.LastRevision == 3 && sequence.GatherAmount > .5f, "Stale cancel stopped current gather");
            sequence.Receive(Vfx.Beat.ThunderCancel, 0, 4, 11.21f);
            sequence.Tick(11.4f, .2f, true);
            Check(sequence.LastRevision == 4 && sequence.GatherAmount == 0f, "Cancel failed to restore orbit");
            Check(!sequence.Released(11.4f), "Old release persisted after its deadline");
            return "STORM_SEQUENCE_PASS: accepted revisions, gather timing, duplicate/stale suppression, cancellation.";
        }
    }
}
'@
Add-Type -TypeDefinition ($source + [Environment]::NewLine + $checks)
[HollowSaint.FoundationKit.Storm.ChargeSequenceChecks]::Run()
