# Development map

Start with [the crown-driven Gaze feedback pass](1.2-gaze-crown-feedback.md) for the current visual direction and verification. [The first hold/release trial](1.2-gaze-release-trial.md) records the unchanged mechanics and earlier test results.

- [1.2 research](1.2-gaze-research.md): audited installed candidate, baseline captures, damage analysis and kit polish findings. Its prototype table predates Stuart's selection of hold/release.
- `tools/GazeReleaseChecks`: input edges, timed preparation, grouped spending and recovery.
- `HollowSaintMod/DevAutopilot.GazeRelease.cs`: known-bank native gameplay checks and HUD captures (`HS_SEGMENTS=gaze-release`).
- `tools/analysis/Measure-Gaze120.ps1`: source-pinned analytical baseline; this is not observed game DPS.
- `tools/dev-profile/Stage-Build.ps1`: private profile staging with backup and native access checks.

The older `GAZE-VFX-CANDIDATE`, `KIT-REFINEMENT-CANDIDATE`, `PLAYTEST`, `RELEASE-1.1.1` and `TODO` notes are historical context, not proof of acceptance for this trial. Public-facing README and release metadata remain release documentation. No public release is approved here.
