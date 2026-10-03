# Git setup result (Sep 27 2026)

**Result: SUCCESS.** Pushed to https://github.com/johnstonstu/ror2-hollow-saint, branch `main`.

| Item | Value |
|---|---|
| Snapshot commit | `06f582c` "Hollow Saint snapshot: anim v18, VFX hs-vfx-v07, plans and audits" |
| Pushed head (`origin/main`) | `063b665` (merge of the snapshot with GitHub's placeholder initial commit) |
| Tracked files | 12,724 |
| Total snapshot size | ~3.5 GB (about 134 MB of regular git content plus LFS) |
| LFS | 12,057 files, 11,791 unique objects, ~3.4 GB uploaded (no quota rejection) |
| Tools | git 2.53.0.windows.2, git-lfs 3.7.1 |

## Push notes
- The repo was not empty when the push landed. While the LFS upload was running (about 15 minutes), a commit `d2d0f37` "Initial commit" (author "Cursor Agent", 02:01 PT) was pushed to `main`, containing only a one-line `README.md` (`# ror2-hollow-saint`). The first push was rejected with `! [remote rejected] main -> main (cannot lock ref 'refs/heads/main': reference already exists)`.
- Resolved without force-pushing or rewriting history: `git merge -s ours --allow-unrelated-histories origin/main`. That kept the local tree exactly as it was (the project `README.md` wins, and the placeholder README remains in history), and then a normal `git push -u origin main` succeeded.
- GitHub warning (informational): "GH010: Your push referenced at least 11791 Git LFS objects, but we only validated a random sample of 10000."

## Secrets
- `.env` is ignored. It was never read, staged, or committed; `git ls-tree -r HEAD | findstr /i env` returns nothing.
- Secret scan of the staged text files (api_key, token, secret, password, sk-, gh*_, AKIA, Bearer, private key headers, HIGGSFIELD_API_KEY assignments) found no real secrets. The hits were only code that reads the variable name (`tools/higgsfield/Test-ApiAccess.ps1`), docs and ignore-file comments, this task prompt, and token-usage counters in `art/anim/wip/refine-run-log.txt`. No files were excluded for secrets.

## Excluded files and why
- `.env`: secret.
- `*.log` (~380 files), `*.err` (~106), and the folders `art/anim/wip/logs/`, `art/anim/wip/_run3/logs/`, `art/anim/wip/_run4/logs/`, `art/anim/wip/_run3/contact/` (logs/err only): run logs.
- 453 interrupted-write temp copies with a PID suffix (e.g. `arc_short_024.png.48204`), which covers all of `art/vfx/assets/_old/previews/frames/`: junk.
- `*.blend1` (2 files): Blender backups.
- `__pycache__/` (4 folders): Python cache.
- Empty Blender `.thumbnails` cache folders at the repo root (`@` and several garbled-name folders): empty, so git would not track them anyway.
- No file was over 2 GB; nothing was excluded for size. The largest files (the hands GIFs, 22 to 31 MB) went to LFS.
- Kept: timestamped backups (`*.fbx.20260927-*`, `*.gif.20260927-*`, etc., via LFS), and `art/vfx/assets/scripts/logs/` (.txt/.json, re-included).

## Not in the snapshot
The other agent (run4) kept editing while this ran. After the commit, `git status` showed 2,294 modified and 16 new files, mostly re-rendered frames under `art/anim/wip/{presentation,special,primary,air,run_dirs,arcstep,glide}`. These were not committed. To save them later: `git add . && git commit -m "..." && git push`.

## Files created
- `.gitattributes` (new): `* text=auto`; LFS for `*.blend *.fbx *.glb *.psd *.exr *.png *.jpg *.gif *.wav *.zip` plus timestamped backups `*.fbx.* *.blend.* *.png.* *.gif.* *.jpg.* *.glb.*`.
- `.gitignore`: this already existed. I appended the "Other secrets" section and everything below it. Local repo config: `core.longpaths=true`, `core.quotepath=false`.

## .gitignore contents
```
# Secrets and machine-specific settings
.env
.env.*
# Include alternate names and editor/backup copies of environment files.
.env*
*.env
*.env.*
*.env~
*.user
*.suo
.vs/
*.local.props
local.settings.json

# C# build outputs
**/bin/
**/obj/
TestResults/
artifacts/
dist/

# Unity caches and generated files; retain Assets and their .meta files
**/[Ll]ibrary/
**/[Tt]emp/
**/[Oo]bj/
**/[Ll]ogs/
**/[Uu]ser[Ss]ettings/
**/[Bb]uilds/
**/MemoryCaptures/

# Local game references and extracted game assets
references/game/
references/extracted/

# Blender backups
*.blend1
*.blend2

# Local OS files
Thumbs.db
.DS_Store

# Other secrets
*.key
*.pem
*credentials*

# Logs and run output
*.log
*.err
!art/vfx/assets/scripts/logs/

# Blender autosave/temp and thumbnail caches
*.blend@
.thumbnails/

# Interrupted-write temp copies (PID suffix, e.g. frame.png.48204)
*.[0-9][0-9][0-9][0-9][0-9]

# Python / Node
__pycache__/
*.pyc
node_modules/
.venv*/
```

## Update: Sep 27 2026, 09:00 to 09:12 PT

**Result: SUCCESS.** `origin/main` = local `HEAD` = `0df5c2ec5d2d8913d044c1c75cafcbdeec2de367` (checked with `git ls-remote`).

| Item | Value |
|---|---|
| Commit | `0df5c2e` "Hollow Saint update: anim v23, 9f thumbs + 9g shoulders + 9i/9h WIP" (parent `063b665`) |
| Files changed | 10,665 (450,325 insertions, 10,951 deletions in text) |
| Size added/modified | ~3.56 GB (sizes of added and modified files on disk) |
| LFS | 10,198 objects, 3.5 GB uploaded at about 5.2 MB/s, taking roughly 12 minutes |
| Newest anim file | `art/anim/hollow-saint-anim-v23.blend` |

- **Excluded files:** only the existing `.gitignore` rules applied (`.env`, `*.log`, `*.err`, `*.blend1`, `*.blend@`, PID-suffix temp copies, `__pycache__/`). The scan found no secrets, so nothing extra was excluded. `.env` was not staged.
- **Secret scan:** covered 408 newly staged or modified text files. The only hits were docs (this file and `art/anim/wip/git-update-prompt.txt`) and `art/anim/wip/refine-run-log.txt` (`"apiKeySource":"login"` and token-usage counters). None of them contain a real key.
- **Errors:** the first `git add -A` failed with `short read while indexing art/anim/wip/spear/qa.json` because run5 was writing that file at the time. A retry a few seconds later succeeded. GitHub gave an informational warning (GH010: validated a random sample of 10,000 of the 10,198 LFS objects). There were no push rejections, no force push, and no hung processes.
- **Not in this commit:** run5 kept editing, and about 700 paths showed as modified or new after the push. Those will go in the next update.

## Update: Sep 27 2026, 18:17 to 18:31 PT

**Result: SUCCESS.** `origin/main` = local `HEAD` = `f38d5215a4c47ed7cc34f386f965e692b82e36eb` (checked with `git ls-remote`).

| Item | Value |
|---|---|
| Commit | `f38d521` "Hollow Saint update: anim v27 (8-dir, spear, transitions, natural hands), new HANDOFF.md" (parent `0df5c2e`) |
| Files changed | 9,474 added or modified, 0 deleted (1,292,333 insertions, 18,745 deletions in text) |
| Size added/modified | ~3.0 GB total: ~25 MB of regular git blobs plus 2.98 GB of LFS content |
| LFS | 7,443 objects uploaded, 3.0 GB at about 5.7 MB/s, taking roughly 9.5 minutes |
| Newest anim file | `art/anim/hollow-saint-anim-v27.blend` (v24 to v27 added in this commit) |

- **Excluded files:** only the existing `.gitignore` rules applied (`.env`, `*.log`, `*.err`, `*.blend1`, `*.blend@`, PID-suffix temp copies, `__pycache__/`). The scan found no secrets, so nothing extra was excluded. `.env` was not staged.
- **Secret scan:** covered the ~1,950 newly staged or modified non-LFS text files. The only hits were docs (this file and `art/anim/wip/git-update2-prompt.txt`) and `art/anim/wip/refine-run-log.txt` (`"apiKeySource":"login"` and token-usage counters). None of them contain a real key.
- **Errors:** none. `git add -A` succeeded on the first try. There were no push rejections, no force push, and no hung processes.
- **Not in this commit:** 8 untracked folders appeared after the commit (`art/anim/wip/polish-20260928/`, `polish_dash_launchA/` to `polish_dash_launchF/`, `polish_dash_wide/`), so something was still writing files. This file's own update is also uncommitted. Both will go in the next update.
