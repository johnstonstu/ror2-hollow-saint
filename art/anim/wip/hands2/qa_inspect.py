"""Scratch (hands2): print chosen QA fields per clip from preview logs.
python qa_inspect.py <log> [title substring|*] key[.sub] ...
"""
import json
import sys
from pathlib import Path


def read_log(p):
    raw = Path(p).read_bytes()
    return raw.decode('utf-16' if raw[:2] in (b'\xff\xfe', b'\xfe\xff') else 'utf-8', errors='replace')


def get(c, key):
    for k in key.split('.'):
        c = c.get(k) if isinstance(c, dict) else None
    return c


log, want, keys = sys.argv[1], sys.argv[2], sys.argv[3:]
for l in read_log(log).splitlines():
    if l.startswith('QA {'):
        c = json.loads(l[3:])
        if want != '*' and want not in c['title']:
            continue
        print(c['title'])
        for k in keys:
            print('   ', k, json.dumps(get(c, k)))
