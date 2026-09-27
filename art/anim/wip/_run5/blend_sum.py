import json
import sys

for p in sys.argv[1:]:
    d = json.load(open(p))
    print(p, 'ok', d['ok'], 'slide_max_mm', d['slide_max_mm'])
    for pr in d['pairs']:
        print(' ', {k: v for k, v in pr.items() if not isinstance(v, (list, dict))})
