"""Make local 1.3 review media from accepted game-window recordings; never upload."""
import argparse
import html
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def run(*args):
    subprocess.run([str(x) for x in args], check=True, cwd=ROOT)


def probe(path):
    data = subprocess.check_output([
        'ffprobe', '-v', 'error', '-show_streams', '-show_format', '-of', 'json', str(path)
    ], text=True)
    info = json.loads(data)
    video = next(s for s in info['streams'] if s['codec_type'] == 'video')
    audio = next(s for s in info['streams'] if s['codec_type'] == 'audio')
    if (video['width'], video['height'], video['avg_frame_rate']) != (1280, 720, '30/1'):
        raise ValueError(f'{path}: unexpected video dimensions or cadence')
    if audio['codec_name'] != 'aac' or audio['channels'] != 2:
        raise ValueError(f'{path}: missing stereo AAC audio')
    return {'file': str(path.relative_to(ROOT)), 'seconds': float(info['format']['duration']),
            'bytes': path.stat().st_size, 'video': '1280x720 / 30 fps', 'audio': 'stereo AAC'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--showcase', required=True)
    parser.add_argument('--acceptance', required=True)
    parser.add_argument('--baseline', required=True)
    parser.add_argument('--output', default='review-1.3')
    opts = parser.parse_args()
    sources = {name: ROOT / 'artifacts' / getattr(opts, name) / 'review'
               for name in ('showcase', 'acceptance', 'baseline')}
    out = ROOT / 'artifacts' / opts.output
    if out.exists():
        raise FileExistsError(f'Preserve prior evidence: choose another output than {out}')
    out.mkdir()
    selected = [
        ('showcase', 'hollowed-orb-13-0', 'Free Hollowed Orb'),
        ('showcase', 'hollowed-orb-13-5', 'Five-charge Hollowed Orb'),
        ('acceptance', 'orb-two-player-relay', 'Harmless player relay: A–B alternation'),
        ('acceptance', 'orb-titan-0-relay', 'Free Orb returning through the player beside a tall boss'),
        ('showcase', 'thundercloud-13-5', 'Five-charge Thundercloud'),
        ('showcase', 'circuit-overhead-orb-13-5', 'Open Circuit with overhead Orb and Primary'),
        ('showcase', 'stormspear-13', 'Stormspear'),
        ('showcase', 'gaze-13', 'Gaze of the Hollow'),
        ('showcase', 'arc-step-13', 'Arc Step'),
    ]
    manifest, cards, paths = [], [], []
    for source, name, label in selected + [
        ('showcase', 'hollowed-orb-13-3', 'Three-charge Hollowed Orb'),
        ('showcase', 'thundercloud-13-1', 'One-charge Thundercloud'),
        ('showcase', 'thundercloud-13-3', 'Three-charge Thundercloud'),
        ('showcase', 'circuit-overhead-orb-13-1', 'One-charge Open Circuit'),
        ('showcase', 'circuit-overhead-orb-13-3', 'Three-charge Open Circuit'),
        ('acceptance', 'orb-titan-5-relay', 'Seven finite boss hits: empowered Orb and harmless player relay'),
        ('baseline', 'primary-baseline-108', 'Previous 108% Primary: isolated itemless comparison'),
        ('baseline', 'primary-proposal-144', 'New 144% Primary: isolated itemless comparison'),
        ('acceptance', 'itemless-titan-primary-free-orb', 'Itemless Titan: Primary and free Orb'),
    ]:
        path = sources[source] / (name + '.mp4')
        manifest.append(probe(path))
        relative = '../' + path.relative_to(ROOT / 'artifacts').as_posix()
        cards.append(f'<article><h2>{html.escape(label)}</h2><video controls preload="metadata" '
                     f'src="{html.escape(relative)}"></video></article>')
        if (source, name, label) in selected:
            paths.append(path)
    concat = out / 'concat.txt'
    concat.write_text(''.join("file '" + p.as_posix().replace("'", "'\\''") + "'\n" for p in paths), encoding='utf-8')
    highlight = out / 'showcase-1.3.mp4'
    run('ffmpeg', '-hide_banner', '-loglevel', 'error', '-n', '-f', 'concat', '-safe', '0',
        '-i', concat, '-c', 'copy', '-movflags', '+faststart', highlight)
    manifest.append(probe(highlight))
    (out / 'media.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    (out / 'index.html').write_text('''<!doctype html><html lang="en"><meta charset="utf-8">
<title>Hollow Saint 1.3 local review</title><style>
body{background:#131021;color:#efeafb;font:17px system-ui;max-width:1200px;margin:36px auto;padding:0 24px}
h1{font-size:32px}h2{font-size:20px}p{line-height:1.6;color:#c6bdd8}
video{width:100%;border-radius:12px;background:#000}article{margin:36px 0}
a{color:#c5a0ff}</style><h1>Hollow Saint 1.3 · local review</h1>
<p>Fresh game-window captures with sound. Ability clips use preloaded charges and durable stationary enemies.
The isolated boss comparisons start at level one with no items, attack speed 1 and zero crit.
The pilot is protected during the boss benchmark; relay health checks use a vulnerable pilot.
These controlled recordings do not establish ordinary combat, physical controller or multiplayer acceptance.</p>
<h2>Whole-kit showcase</h2><video controls preload="metadata" src="showcase-1.3.mp4"></video>
<details><summary>Individual abilities, charge tiers and itemless benchmarks</summary>
''' + '\n'.join(cards) + '</details></html>\n', encoding='utf-8')
    media = ROOT / 'docs' / 'media'
    for filename, clip, start, length in [
        ('hollowed-orb-13', 'hollowed-orb-13-5', .25, 5.8),
        ('thundercloud-13', 'thundercloud-13-5', .25, 4.8),
        ('circuit-orb-13', 'circuit-overhead-orb-13-5', .25, 7.0),
    ]:
        destination = media / (filename + '.webp')
        if destination.exists():
            raise FileExistsError(f'Read existing media before replacing it: {destination}')
        run('ffmpeg', '-hide_banner', '-loglevel', 'error', '-n', '-ss', start, '-t', length,
            '-i', sources['showcase'] / (clip + '.mp4'), '-vf', 'fps=20,scale=960:-2:flags=lanczos',
            '-c:v', 'libwebp_anim', '-quality', 85, '-compression_level', 6, '-loop', 0, '-an', destination)
    print(out)


if __name__ == '__main__':
    main()
