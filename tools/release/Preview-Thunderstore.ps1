# Renders a local mock of the Thunderstore package page and the browse grid, to check the README,
# description and icon before uploading. The README goes through GitHub's Markdown API (Thunderstore
# renders the same GitHub-flavoured Markdown), so the GIFs load from main exactly as they will live.
# Usage: powershell -ExecutionPolicy Bypass -File tools\release\Preview-Thunderstore.ps1 [-Icons a.png,b.png] [-Screenshot]
param([string[]]$Icons, [switch]$Screenshot, [int]$PageHeight = 2400)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$pkg = Join-Path $repo 'HollowSaintMod\Package'
$out = Join-Path $repo 'artifacts\thunderstore-preview'
New-Item -ItemType Directory -Force $out | Out-Null
$manifest = Get-Content (Join-Path $pkg 'manifest.json') -Raw | ConvertFrom-Json
$manifest.name = $manifest.name -replace '_', ' '
$Icons = @($Icons | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
if (-not $Icons) { $Icons = @((Join-Path $pkg 'icon.png')) }
$iconFiles = @(foreach ($i in $Icons) { $p = (Resolve-Path $i).Path; $name = Split-Path $p -Leaf; if ((Split-Path $p) -ne $out) { Copy-Item $p (Join-Path $out $name) -Force }; $name })

[Console]::OutputEncoding = New-Object Text.UTF8Encoding $false
$readme = (gh api markdown/raw -H 'Content-Type: text/plain' --input (Join-Path $pkg 'README.md')) -join "`n"
if ($LASTEXITCODE -ne 0) { throw 'gh api markdown/raw failed (is gh logged in?)' }
$enc = [Net.WebUtility]
$deps = ($manifest.dependencies | ForEach-Object { $parts = $_ -split '-'; '<li><span class="dep-icon"></span><div><b>' + $parts[1] + '</b><small>' + $parts[0] + ' &middot; ' + $parts[2] + '</small></div></li>' }) -join "`n"

$css = @'
body{margin:0;background:#1a1b29;color:#e4e4ee;font:15px/1.55 "Segoe UI",Roboto,sans-serif}
a{color:#4fb8ff}.top{background:#11121d;padding:12px 32px;font-weight:600;letter-spacing:.3px;color:#9aa0c0}
.wrap{max-width:1180px;margin:0 auto;padding:24px 32px}.head{display:flex;gap:24px;align-items:flex-start}
.head img{width:256px;height:256px;border-radius:6px;background:#0d0e17}.head h1{margin:0 0 4px;font-size:30px}
.by{color:#9aa0c0;margin-bottom:12px}.desc{font-size:16px;max-width:620px}.meta{display:flex;gap:28px;margin-top:16px;color:#9aa0c0;font-size:13px}
.meta b{display:block;color:#e4e4ee;font-size:15px}.btn{display:inline-block;margin-top:18px;background:#2a6bd8;color:#fff;padding:9px 18px;border-radius:4px;font-weight:600;margin-right:8px}
.btn.alt{background:#2b2d42}.tabs{display:flex;gap:4px;margin:28px 0 0;border-bottom:1px solid #2b2d42}
.tabs span{padding:10px 18px;color:#9aa0c0}.tabs span.on{color:#fff;border-bottom:2px solid #4fb8ff}
.cols{display:grid;grid-template-columns:1fr 300px;gap:28px;margin-top:20px}.readme{background:#222336;border-radius:6px;padding:8px 28px 24px}
.readme img{max-width:100%}.readme table{border-collapse:collapse}.readme td,.readme th{border:1px solid #34364f;padding:6px 10px}
.readme code{background:#2b2d42;padding:1px 5px;border-radius:3px}.side{background:#222336;border-radius:6px;padding:16px 18px;align-self:start}
.side h3{margin:0 0 10px;font-size:14px;color:#9aa0c0;text-transform:uppercase;letter-spacing:.5px}.side ul{list-style:none;margin:0;padding:0}
.side li{display:flex;gap:10px;align-items:center;padding:6px 0;border-bottom:1px solid #2b2d42}.side small{display:block;color:#9aa0c0}
.dep-icon{width:34px;height:34px;border-radius:4px;background:#34364f;flex:none}
.grid{display:grid;grid-template-columns:repeat(5,200px);gap:18px}.card{background:#222336;border-radius:6px;overflow:hidden}
.card img{width:200px;height:200px;display:block}.card .t{padding:10px 12px}.card b{display:block}.card small{color:#9aa0c0;display:block}
.card p{margin:6px 0 0;font-size:13px;color:#c4c6d8;height:58px;overflow:hidden}.label{color:#9aa0c0;font-size:13px;margin:26px 0 10px}
.tiny{display:flex;gap:26px;align-items:center}.tiny img{border-radius:4px}
'@

foreach ($icon in $iconFiles) {
    $page = @"
<!doctype html><html><head><meta charset="utf-8"><style>$css</style></head><body>
<div class="top">THUNDERSTORE &nbsp;/&nbsp; Risk of Rain 2 &nbsp;/&nbsp; Mods</div>
<div class="wrap"><div class="head"><img src="$icon"><div>
<h1>$($manifest.name)</h1><div class="by">by <a>JohnstonStu</a></div>
<div class="desc">$($enc::HtmlEncode($manifest.description))</div>
<div class="meta"><div>Version<b>$($manifest.version_number)</b></div><div>Downloads<b>0</b></div><div>Categories<b>Mods &middot; Survivors</b></div></div>
<a class="btn">Install with Mod Manager</a><a class="btn alt">Manual Download</a><a class="btn alt">Website</a>
</div></div>
<div class="tabs"><span class="on">Details</span><span>Changelog</span><span>Versions</span><span>Wiki</span></div>
<div class="cols"><div class="readme">$readme</div>
<div class="side"><h3>Dependencies ($($manifest.dependencies.Count))</h3><ul>$deps</ul></div></div></div></body></html>
"@
    [IO.File]::WriteAllText((Join-Path $out ('page_' + [IO.Path]::GetFileNameWithoutExtension($icon) + '.html')), $page, (New-Object Text.UTF8Encoding $false))
}

$cards = ($iconFiles | ForEach-Object { '<div class="card"><img src="' + $_ + '"><div class="t"><b>' + $manifest.name + '</b><small>JohnstonStu &middot; ' + [IO.Path]::GetFileNameWithoutExtension($_) + '</small><p>' + $enc::HtmlEncode($manifest.description) + '</p></div></div>' }) -join "`n"
$tiny = ($iconFiles | ForEach-Object { '<img src="' + $_ + '" width="64" height="64"><img src="' + $_ + '" width="32" height="32">' }) -join "`n"
$grid = @"
<!doctype html><html><head><meta charset="utf-8"><style>$css</style></head><body>
<div class="top">THUNDERSTORE &nbsp;/&nbsp; Risk of Rain 2 &nbsp;/&nbsp; Browse</div><div class="wrap">
<div class="label">Browse grid (200 px tiles)</div><div class="grid">$cards</div>
<div class="label">Mod manager list sizes (64 px and 32 px)</div><div class="tiny">$tiny</div></div></body></html>
"@
[IO.File]::WriteAllText((Join-Path $out 'grid.html'), $grid, (New-Object Text.UTF8Encoding $false))

if ($Screenshot) {
    $edge = 'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe'
    $shots = @(@{ html = 'grid.html'; png = 'grid.png'; size = '1220,640' })
    $shots += @{ html = 'page_' + [IO.Path]::GetFileNameWithoutExtension($iconFiles[0]) + '.html'; png = 'page.png'; size = '1240,' + $PageHeight }
    $ErrorActionPreference = 'Continue'
    foreach ($s in $shots) {
        $url = 'file:///' + (Join-Path $out $s.html).Replace('\', '/')
        $profileDir = Join-Path $env:TEMP ('hs-preview-' + [guid]::NewGuid())
        & $edge --headless=new --disable-gpu --hide-scrollbars --virtual-time-budget=15000 ('--user-data-dir=' + $profileDir) ('--window-size=' + $s.size) ('--screenshot=' + (Join-Path $out $s.png)) $url 2>&1 | Out-Null
        Remove-Item $profileDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
Get-ChildItem $out -Filter *.html | ForEach-Object { 'preview: ' + $_.FullName }
