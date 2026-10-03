# Foundation checkpoint — September 27, 2026

**Historical checkpoint.** The foundation has since launched, spawned, passed its
catalog audit and received a user-confirmed upper-back fix. Read
[`next-agent-handoff-20260927.md`](next-agent-handoff-20260927.md) for current status.

## Working agreement

Stuart handles game launches and playtesting through r2modman. Do not launch or
drive the game automatically. Finish profile updates before requesting a test.
Focus agent work on Unity, character implementation and log diagnosis. Keep all
work local; no remote pushes or releases.

## Installed development profile

`Hollow Saint Dev` now contains the same 34 supporting packages as `demo time new`,
excluding AH-64, plus the manually installed Hollow Saint foundation DLL and bundle.
This includes the complete reference R2API suite, MiscFixes, SeekersPatcher,
BepInEx GUI, DebugToolkit and Risk of Options. These are reference-profile versions,
not newly downloaded upgrades. Both reference profiles remain unchanged.

181 support files were verified by SHA256. Loader root files `.doorstop_version`,
`doorstop_config.ini` and `winhttp.dll` are present. Missing loader files in the
initial profile caused r2modman to choose Doorstop 3 arguments for Doorstop 4.
Restoring them corrected the launch without custom global arguments.

Latest staged DLL SHA256:
`84196A873E287DAA44A79A65C42B61AA84C0AA88552BDADAC97886FC16F2228F`

Evidence: `artifacts/foundation/profile-support-sync.txt`.
Prior DLL and profile manifest are backed up in the directory recorded there.
The local Hollow Saint plugin does not yet have a managed entry in r2modman's list.

## Implementation and verification

Unity bundle contains 137 renderers and 15 animation clips. Native RoR2 input,
motor, skill slots and equipment are retained; skills are temporary Commando
abilities, not the final lightning kit. 23 attachment aliases are implemented;
per-item display fitting remains pending.

The most recent launch confirmed BepInEx loaded Hollow Saint, but content loading
stopped at a nonpublic ContentPack.identifier setter. The staged build removes
that access and populates the game's output collections through public APIs.
Earlier ChildLocator private-field access was replaced with AddChild.
85 RoR2 member references were checked against installed assembly visibility.
Build succeeds with one existing transitive MMHOOK NU1701 compatibility warning.

The newest build has NOT been launched. Survivor selection, in-run appearance,
animation, controller feel and gameplay remain unverified. Do not mark M1 complete.
Next user test: select Hollow Saint Dev in r2modman and Start modded. Inspect fresh
LogOutput.log and foundation-catalog-checks.txt before asking for gameplay checks.

Next Unity work: native animation-state compatibility, skin/controller retention,
game shader conversion, visible portrait and display presentation, then fitted
item displays. Physical controller acceptance is tracked in controller-acceptance.md.
