"""Scratch (hands2): tools/blender/anim/preview.py with module overrides. Read results from the log's QA lines
(qa_compare.py --log): parallel runs of one module share art/anim/wip/<module>/qa.json.

blender --background --factory-startup --python-exit-code 1 --python art/anim/wip/hands2/preview_set.py --
    <tag> <module> --no-render [--set handpose.WRIST_FLEX=6 ...]
"""
import importlib
import runpy
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[4]/'tools/blender/anim'
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS/'clips'))
i = sys.argv.index('--')
args = sys.argv[i+1:]
tag, rest = args[0], args[1:]
sets = [rest[k+1] for k, a in enumerate(rest) if a == '--set']
rest = [a for k, a in enumerate(rest) if a != '--set' and (k == 0 or rest[k-1] != '--set')]
for kv in sets:
    name, value = kv.split('=', 1)
    mname, attr = name.rsplit('.', 1)
    setattr(importlib.import_module(mname), attr, eval(value))
    print('SET', mname, attr, value, flush=True)
sys.argv = sys.argv[:i+1]+rest
runpy.run_path(str(TOOLS/'preview.py'), run_name='__main__')
