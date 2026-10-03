"""Bump the dev build version and its two build-tag keywords.
Usage: python tools/bump.py 0.2.3 "halo-bone | hand-bolt"
Keeps Plugin.cs and Package/manifest.json in sync, UTF-8 safe."""
import re, sys, os
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "HollowSaintMod")
version, keywords = sys.argv[1], sys.argv[2]
p = os.path.join(root, "Plugin.cs")
t = open(p, encoding="utf-8").read()
t = re.sub(r'Version = "[^"]*"', 'Version = "%s"' % version, t, count=1)
t = re.sub(r'BuildKeywords = "[^"]*"', 'BuildKeywords = "%s"' % keywords, t, count=1)
open(p, "w", encoding="utf-8").write(t)
m = os.path.join(root, "Package", "manifest.json")
t = open(m, encoding="utf-8").read()
t = re.sub(r'"version_number": "[^"]*"', '"version_number": "%s"' % version, t)
open(m, "w", encoding="utf-8").write(t)
print("bumped", version, "|", keywords)
