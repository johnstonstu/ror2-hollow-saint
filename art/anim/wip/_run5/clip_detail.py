"""python clip_detail.py <module> "<title>" [keys...]: print selected qa.json fields for one clip."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
m, title, *keys = sys.argv[1:]
for c in json.loads((ROOT/'art/anim/wip'/m/'qa.json').read_text()):
    if c['title'] == title:
        for k in keys or c.keys():
            v = c.get(k)
            s = json.dumps(v)
            print(k, s[:1500])
