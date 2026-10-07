"""Keep plugin, assembly and package versions consistent; --check never writes."""
import argparse
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
PATTERNS = {
    'HollowSaintMod/Plugin.cs': r'(const string Version = ")([^"]+)(")',
    'HollowSaintMod/HollowSaint.csproj': r'(<Version>)([^<]+)(</Version>)',
    'HollowSaintMod/Package/manifest.json': r'("version_number"\s*:\s*")([^"]+)(")',
}


def inputs(root):
    values = {}
    for name, pattern in PATTERNS.items():
        path = root / name
        raw = path.read_bytes()
        text = raw.decode('utf-8-sig')
        matches = list(re.finditer(pattern, text))
        if len(matches) != 1:
            raise ValueError(f'{name}: expected exactly one version declaration')
        values[path] = (raw, text, pattern, matches[0].group(2))
    return values


def check(root=ROOT):
    versions = {value[3] for value in inputs(root).values()}
    if len(versions) != 1:
        raise ValueError('Plugin, assembly and manifest versions differ: ' + ', '.join(sorted(versions)))
    version = versions.pop()
    if not re.fullmatch(r'(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)', version):
        raise ValueError('Version must be major.minor.patch without leading zeros')
    return version


def bump(version, keywords, root=ROOT):
    if not re.fullmatch(r'(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)', version):
        raise ValueError('Version must be major.minor.patch without leading zeros')
    if any(ord(char) < 32 for char in keywords):
        raise ValueError('Build keywords must be one line without control characters')
    values = inputs(root)
    updates = {}
    for path, (_, text, pattern, _) in values.items():
        text = re.sub(pattern, lambda m: m[1] + version + m[3], text)
        if path.name == 'Plugin.cs':
            pattern = r'const string BuildKeywords = "(?:\\.|[^"\\])*"'
            if len(re.findall(pattern, text)) != 1:
                raise ValueError('Plugin.cs: expected exactly one BuildKeywords declaration')
            escaped = keywords.replace('\\', '\\\\').replace('"', '\\"')
            text = re.sub(pattern, lambda _: 'const string BuildKeywords = "' + escaped + '"', text)
        if path.suffix == '.json':
            json.loads(text)
        updates[path] = text.encode('utf-8')
    written = []
    try:
        for path, data in updates.items():
            written.append(path)
            path.write_bytes(data)
    except OSError:
        for path in written:
            path.write_bytes(values[path][0])
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('version', nargs='?')
    parser.add_argument('keywords', nargs='?')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    try:
        if args.check:
            if args.version or args.keywords:
                parser.error('--check takes no version or keywords')
            print('VERSION_PARITY_PASS ' + check())
        else:
            if args.version is None or args.keywords is None:
                parser.error('provide a version and quoted build keywords, or --check')
            bump(args.version, args.keywords)
            print('Updated all three version declarations: ' + check())
        return 0
    except (OSError, ValueError) as error:
        print(f'Version operation failed: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
