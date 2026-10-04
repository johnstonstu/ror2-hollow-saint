# Hollow Saint 1.1.1 release preparation

Prepared locally from user-accepted source `a6841d51fa9b5515865de39b99c139f2ba1a6f75` on the separate `codex/release-1.1.1` branch. This release preparation changes version metadata and documentation only. The plugin, project assembly metadata and Thunderstore manifest all identify 1.1.1. Dependency versions remain unchanged. Dev-profile registration derives the version from the manifest; no installed profile or saved config was edited.

The accepted changes are Gaze ambient ground tendrils and confirmed-hit connectors; per-projectile Stormspear charge/crown snapshots; Arc Bolt 100% to 120%; gesture ownership and movement cleanup; Stormspear 400-1600% to 350-1400%, with default-only migration; and English README skill roles, combos and approved player-view media. See the packaged CHANGELOG for player-facing notes. Candidate reports and old playtest checklists retain historical numbers and pending statements under explicit historical labels.

## Validation

- Release compilation: 0 errors, 19 existing warnings. Restore uses the existing NuGet cache with audit disabled; build uses `--no-restore` and the original repository's read-only RiskOfOptions DLL.
- Language: 33 tokens, four language sections, placeholders, tags, plurals and English legacy text.
- Arc Bolt defaults: 12 cases; Stormspear defaults: 20 cases; crown gesture flow: 27 cases; animation rules pass.
- Snapshot transport runs in PowerShell 7 because Windows PowerShell's legacy compiler rejects the production readonly struct. It checks 67 behavioral assertions plus installed-engine transport and initialization wiring.
- Read-only real-game DLL access check: PASS. Release package validation: PASS. Their results are recorded in the sibling `release-111-*.log` files. No game was launched in this release task.

The user accepted a single-player playtest before release preparation. These local checks do not establish multiplayer, physical controller, native collision edge cases or frame-time performance. Those remain unverified; the README retains that limitation.

## Package and publication handoff

Run `tools/release/Make-Package.ps1 -SkipBuild` after the verified Release build. The script validates and uses the default pinned bundle, SHA256 `7238F181B17B011A0BCC0BF13AB8C7D951CAA7930A24DB4F5B83F278BEA111B4`; no bundle override is used. The ZIP has exactly eight allowlisted root/forward-slash entries: manifest, README, CHANGELOG, icon, LICENSE, DLL, bundle and language file. No capture harness, build references, loose audio, PDB, configs or profiles are added. Existing development autopilot code remains as in the accepted production base; the temporary screenshot-only harness was removed in `38e7b3c1`.

Output: `artifacts/release/JohnstonStu-Hollow_Saint-1.1.1.zip`. The user will upload it manually. Nothing is pushed, merged, tagged, uploaded or published by this task.

The new packaged README image URL requires later publication to public GitHub `main`:

`https://raw.githubusercontent.com/johnstonstu/ror2-hollow-saint/main/docs/media/gaze-player-view.png`

Minimum files for the matching public showcase are `README.md`, `HollowSaintMod/Package/README.md`, and `docs/media/gaze-player-view.png`. The ZIP contains its own updated README, but externally hosted images require their public URLs. This task preserves the approved PNG unchanged and does not publish those files. Full source publication should include the release commit and its accepted ancestors.
