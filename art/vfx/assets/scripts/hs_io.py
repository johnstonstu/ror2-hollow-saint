"""Never overwrite, never delete: an existing output is moved into art/vfx/assets/_old/ first."""
import datetime
import os
import shutil

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OLD = os.path.join(ROOT, '_old')


def fresh(path):
    """Return path, after moving any existing file there into _old/<same relative dir>/<name>.<stamp>."""
    path = os.path.abspath(path)
    if os.path.exists(path):
        rel = os.path.relpath(path, ROOT)
        if rel.lower().endswith('.blend'):
            raise SystemExit(f'refusing to replace a .blend: {path}')
        stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
        dst = os.path.join(OLD, os.path.dirname(rel), f'{os.path.basename(rel)}.{stamp}')
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.move(path, dst)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    return path


def log(msg):
    stamp = datetime.datetime.now().strftime('%Y-%m-%d %H:%M')
    with open(os.path.join(ROOT, 'vfx-run1-progress.txt'), 'a', encoding='utf-8') as f:
        f.write(f'{stamp} PT  {msg}\n')
