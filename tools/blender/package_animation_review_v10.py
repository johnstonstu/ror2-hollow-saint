"""Package Blender-rendered frames as GIFs, contact sheets and a local player."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'art/hybrid/v10-animation'
catalog = json.loads((OUT/'catalog.json').read_text())
sheet = Image.new('RGB', (1536, 1104), '#161a1e')
draw = ImageDraw.Draw(sheet)
for i, spec in enumerate(catalog):
    frames = [Image.open(p).convert('RGB') for p in sorted((OUT/f'clip-{i}').glob('*.png'))]
    expected = len(range(1, spec['end']+1, 2))
    assert len(frames) == expected, (i, len(frames), expected)
    # The duplicate endpoint is omitted for seamless looping previews.
    if spec['loop']:
        frames = frames[:-1]
    duration = [83, 83, 84] * (len(frames)//3+1)
    duration = duration[:len(frames)]
    if not spec['loop']:
        duration[-1] += 500
    frames[0].save(OUT/f'clip-{i}.gif', save_all=True, append_images=frames[1:],
                   duration=duration, loop=0, optimize=False)
    col, row = i % 3, i // 3
    sheet.paste(Image.open(OUT/f'pose-{i}-hero.png'), (col*512, row*552))
    draw.text((col*512+16, row*552+524), spec['title'], fill='#a9e6ec')
sheet.save(OUT/'contact-sheet.jpg', quality=93)

html = '''<!doctype html><html lang="en"><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Hollow Saint — animation studies</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#11171a;color:#e8eeee;font:16px system-ui}
main{max-width:1180px;margin:40px auto;padding:0 24px}small{color:#93b9bd;letter-spacing:.16em}
h1{font-size:clamp(28px,5vw,48px);margin:8px 0}p{color:#b3c2c8;line-height:1.6}
.layout{display:grid;grid-template-columns:minmax(320px,720px) 1fr;gap:28px;margin-top:28px}
#motion{width:100%;background:#242422;border-radius:12px}button,select{background:#253439;color:#fff;
border:1px solid #47616a;border-radius:7px;padding:11px;font:inherit;cursor:pointer}
select{width:100%;margin-bottom:16px}input{width:100%;accent-color:#8de0e9}
.controls{display:flex;align-items:center;gap:15px;margin:14px 0}.controls input{flex:1}
#pose{width:100%;border-radius:8px}a{color:#93e3ef}li{margin:10px 0}output{white-space:nowrap}
@media(max-width:760px){.layout{grid-template-columns:1fr}main{margin-top:22px}}
</style><main><small>HOLLOW SAINT / V10 / BLENDER STUDY</small>
<h1>A contained storm, set in motion.</h1>
<p>Six editable custom clips. Select a motion, play it, or scrub through its poses.
These are rendered animation studies; lighting and camera stay fixed.</p>
<div class="layout"><section><img id="motion" alt="Animated Hollow Saint pose">
<div class="controls"><button id="play">Pause</button><select id="speed" aria-label="Playback speed" style="width:105px;margin:0"><option value="1">1×</option><option value="0.5">0.5×</option></select><output id="frame"></output></div>
<input id="scrub" type="range" min="0" value="0" aria-label="Animation frame"></section>
<aside><select id="clips" aria-label="Animation clip"></select><h2 id="title"></h2>
<p id="timing"></p><ul id="events"></ul><h3>Side pose</h3><img id="pose" alt="Side view of the selected key pose">
<p><a href="../hollow-saint-hybrid-v10.blend">Editable Blender scene</a> · <a href="qa.json">Numerical checks</a></p></aside></div>
<p>Rig repairs: anatomical body weights, fitted finger pivots, independent halo pivots,
and complete keyed pose resets. Original v7–v9 files are unchanged.</p>
<p>Still provisional: fused shoulder/elbow deformation, cloth, and locomotion. Arc Step is
in place, with no displacement or leg cycle. No lightning VFX, export, Unity, or in-game validation.</p></main>
<script>
const clips=CATALOG;
const select=document.querySelector('#clips'),scrub=document.querySelector('#scrub'),motion=document.querySelector('#motion');
let clip=0,index=0,playing=true,last=0,images=[];
clips.forEach((c,i)=>{const o=document.createElement('option');o.value=i;o.textContent=c.title;select.append(o)});
function show(){motion.src=images[index].src;scrub.value=index;document.querySelector('#frame').textContent='Frame '+(1+index*2)}
function load(){const c=clips[clip];index=0;images=[];for(let f=1;f<=c.end;f+=2){let img=new Image();img.src=`clip-${clip}/${String(images.length).padStart(3,'0')}.png`;images.push(img)}
if(c.loop)images.pop();scrub.max=images.length-1;document.querySelector('#title').textContent=c.title;
document.querySelector('#timing').textContent=((c.end-1)/24).toFixed(2)+' seconds · '+(c.loop?'loop':'one-shot, repeated for review');
document.querySelector('#events').replaceChildren(...Object.entries(c.events).map(([label,frame])=>{const li=document.createElement('li');li.textContent=label+' — frame '+frame;return li}));
document.querySelector('#pose').src=`pose-${clip}-side.png`;show();last=0}
select.onchange=()=>{clip=Number(select.value);load()};
document.querySelector('#play').onclick=()=>{playing=!playing;document.querySelector('#play').textContent=playing?'Pause':'Play'};
scrub.oninput=()=>{playing=false;document.querySelector('#play').textContent='Play';index=Number(scrub.value);show()};
function tick(t){const delay=1000/12/Number(document.querySelector('#speed').value);
if(playing&&t-last>=delay){index=(index+1)%images.length;show();last=t}requestAnimationFrame(tick)}load();requestAnimationFrame(tick);
</script></html>'''
(OUT/'review.html').write_text(html.replace('CATALOG', json.dumps(catalog)), encoding='utf-8')
print('Packaged six motion GIFs, contact sheet, and interactive review')
