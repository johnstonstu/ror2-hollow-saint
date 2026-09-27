"""Collect every previewed clip into art/anim/review.html (system Python).

Run after preview.py + sheet.py: python tools/blender/anim/package_review.py
"""
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WIP = ROOT/'art/anim/wip'
cards = []
for info_path in sorted(WIP.glob('*/*/clip.json')):
    d = info_path.parent
    if d.parent.name in ('hands', 'transitions'):   # hand close-ups join their clip's card; stitches get their own section
        continue
    info = json.loads(info_path.read_text())
    gifs = sorted(d.glob('*.gif'))+sorted((WIP/'hands'/d.name).glob('hands.gif'))
    stills = sorted(d.glob('still-*.png'))
    rel = lambda p: p.relative_to(ROOT/'art/anim').as_posix()
    facts = [f"{info['frames'][1]-info['frames'][0]} frames", 'loop' if info['loop'] else 'one-shot']
    if info.get('speed_mps'):
        facts.append(f"{info['speed_mps']} m/s")
    facts.append(f"QA {info.get('status', '?')}")
    markers = ', '.join(f'{k} f{v}' for k, v in info.get('markers', {}).items())
    media = ''.join(f'<figure><img src="{rel(g)}" alt="{html.escape(info["title"])} {g.stem}"><figcaption>{g.stem}</figcaption></figure>' for g in gifs)
    media += ''.join(f'<figure><img src="{rel(s)}" alt="{html.escape(info["title"])} {s.stem}"><figcaption>{s.stem.replace("still-", "Cycles ")}</figcaption></figure>' for s in stills)
    cards.append(f'''<section><h2>{html.escape(info['title'])}</h2>
<p>{' · '.join(facts)}<br><small>{html.escape(markers)} · module <code>{info.get('module', d.parent.name)}</code></small></p>
<div class="media">{media}</div></section>''')

stitches = []
for sj in sorted((WIP/'transitions').glob('*/stitch.json')):
    d = sj.parent
    s = json.loads(sj.read_text())
    rel = lambda p: p.relative_to(ROOT/'art/anim').as_posix()
    gifs = sorted(d.glob('*.gif'))
    rows = ''.join(f"<tr><td>f{h['frame']}</td><td>{html.escape(h['from'])} &rarr; {html.escape(h['to'])}</td>"
                   f"<td>{h['blend'] or 'cut'}</td><td>{html.escape(h['worst_bone'])}</td><td>{h['acc']*1000:.1f}</td>"
                   f"<td>{h['clip_max_acc']*1000:.1f}</td></tr>" for h in s['handoffs'])
    media = ''.join(f'<figure><img src="{rel(g)}" alt="{html.escape(s["name"])} {g.stem}"><figcaption>{g.stem}</figcaption></figure>' for g in gifs)
    stitches.append(f'''<section><h2>{html.escape(s['name'])}</h2>
<table><tr><th>at</th><th>handoff</th><th>blend</th><th>worst bone</th><th>acc mm/f&sup2;</th><th>clip max</th></tr>{rows}</table>
<div class="media">{media}</div></section>''')
matrix = WIP/'transitions'/'TRANSITIONS.md'
stitch_html = ''
if stitches:
    link = f' Matrix: <code>{matrix.relative_to(ROOT).as_posix()}</code>.' if matrix.exists() else ''
    stitch_html = (f'<h1 id="transitions">Transitions</h1><p>Clips played back to back (<code>stitch.py</code>): hard cuts '
                   f'where the handoff is exact, Unity-style cross-fades otherwise. Acceleration is measured within 2 frames '
                   f'of each handoff against the worst inside the clips.{link}</p>'+''.join(stitches))

page = f'''<!doctype html><html lang="en"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>Hollow Saint: locomotion review</title>
<style>body{{margin:0;background:#11171a;color:#e8eeee;font:16px system-ui}}main{{max-width:1400px;margin:32px auto;padding:0 20px}}
h1{{margin:0 0 6px}}p{{color:#b3c2c8}}section{{border-top:1px solid #2b3a40;padding:18px 0}}
.media{{display:flex;flex-wrap:wrap;gap:12px}}figure{{margin:0}}img{{height:360px;border-radius:8px;background:#242422}}
figcaption{{color:#93b9bd;font-size:13px}}code{{color:#93e3ef}}small{{color:#8aa}}
table{{border-collapse:collapse;margin:8px 0;font-size:14px}}td,th{{border:1px solid #2b3a40;padding:3px 8px;text-align:left}}</style>
<main><h1>Hollow Saint: locomotion block-out</h1>
<p>In-place clips built on hybrid v18 and baked to the regular bones. GIFs are EEVEE previews at half frame rate unless noted;
Cycles stills show the first marker frame. Work in progress.</p>
{''.join(cards) or '<p>No clips previewed yet.</p>'}{stitch_html}</main></html>'''
(ROOT/'art/anim/review.html').write_text(page, encoding='utf-8')
print('WROTE art/anim/review.html', len(cards), 'clips', len(stitches), 'transitions')
