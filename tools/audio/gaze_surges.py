"""Five bounded discharge takes; preserve every installed cue and routing object."""
from pathlib import Path
import hashlib
import json
import math
import struct
import sys
import wave
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / 'art/audio/HollowSaintAudio'
BANK = PROJECT / 'GeneratedSoundBanks/Windows/HollowSaint.bnk'
BASE_SHA = '80FE181AA5FF9910EA7D90D8684AE95B3DCB012F53DC17A3FC411D293F4F5E99'
GAINS = (1., 1.15, 1.30, 1.45, 1.60)


def chunks(data, offset=0):
    result = {}
    while offset < len(data):
        tag, size = struct.unpack_from('<4sI', data, offset)
        result[tag] = (offset+8, data[offset+8:offset+8+size])
        offset += 8 + size + (size % 2 if data[:4] == b'RIFF' else 0)
    return result


def media(data):
    parts = chunks(data)
    return {key: (start, size, parts[b'DATA'][1][start:start+size])
            for key, start, size in struct.iter_unpack('<III', parts[b'DIDX'][1])}


def hierarchy(data):
    raw = chunks(data)[b'HIRC'][1]
    count, = struct.unpack_from('<I', raw)
    result, pos = {}, 4
    for _ in range(count):
        _, size = struct.unpack_from('<BI', raw, pos)
        key, = struct.unpack_from('<I', raw, pos+5)
        result[key] = raw[pos:pos+5+size]
        pos += 5+size
    assert pos == len(raw)
    return result


def baseline(path):
    data = path.read_bytes()
    assert hashlib.sha256(data).hexdigest().upper() == BASE_SHA
    return data


def prepare(path):
    data = baseline(path)
    original = media(data)
    actor = ET.parse(PROJECT / 'Actor-Mixer Hierarchy/Default Work Unit.wwu')
    # Recovered sampled inputs are authoring-only. Preservation restores exact WEMs.
    for name in ('ThunderStrike', 'SpearBurst', 'SpearThrowHeavy'):
        target = ROOT / f'art/audio/source/{name}.wav'
        if target.exists():
            continue
        sound = actor.find(f".//Sound[@Name='HS_{name}']")
        key = int(sound.find('ChildrenList/AudioFileSource/MediaIDList/MediaID').get('ID'))
        pcm = chunks(original[key][2], 12)[b'data'][1]
        with wave.open(str(target), 'wb') as output:
            output.setparams((1, 2, 48000, 0, 'NONE', 'not compressed'))
            output.writeframes(pcm)
    with wave.open(str(ROOT / 'art/audio/source/ThunderRelease.wav'), 'rb') as source:
        assert source.getparams()[:3] == (1, 2, 48000)
        pcm = tuple(value[0] for value in struct.iter_unpack('<h', source.readframes(source.getnframes())))
    assert len(pcm) == 11520
    manifest_path = ROOT / 'art/audio/source/manifest.json'
    manifest = [entry for entry in json.loads(manifest_path.read_text()) if not entry['name'].startswith('GazeSurge')]
    for step, gain in enumerate(GAINS, 1):
        samples = tuple(round(value * gain) for value in pcm)
        peak = max(abs(value) for value in samples)
        assert peak < 16384, 'At least 6 dB PCM peak headroom required'
        name = f'GazeSurge{step}'
        with wave.open(str(ROOT / f'art/audio/source/{name}.wav'), 'wb') as output:
            output.setparams((1, 2, 48000, 0, 'NONE', 'not compressed'))
            output.writeframes(b''.join(struct.pack('<h', value) for value in samples))
        rms = math.sqrt(sum(value*value for value in samples)/len(samples))/32768
        manifest.append(dict(name=name, event='Play_HS_'+name, seconds=.24, loop=False,
                             peak_dbfs=round(20*math.log10(peak/32768), 2), rms_dbfs=round(20*math.log10(rms), 2)))
    manifest_path.write_text(json.dumps(manifest, indent=2)+'\n')
    print('GAZE_SURGES_PREPARED: five finite .24-second gains', GAINS)


def preserve_and_check(path):
    old, new = baseline(path), BANK.read_bytes()
    old_objects, new_objects = hierarchy(old), hierarchy(new)
    assert all(new_objects.get(key) == value for key, value in old_objects.items()), 'Existing routing/event object changed'
    before, after = media(old), media(new)
    assert len(before) == 35 and len(after) == 40
    result = bytearray(new)
    data_start = chunks(new)[b'DATA'][0]
    restored = 0
    for key, (_, size, wem) in before.items():
        offset, new_size, new_wem = after[key]
        assert size == new_size, ('Changed media duration', key)
        if wem != new_wem:
            result[data_start+offset:data_start+offset+size] = wem
            restored += 1
    BANK.write_bytes(result)
    checked = media(result)
    assert all(checked[key][2] == value[2] for key, value in before.items())
    actor = ET.parse(PROJECT / 'Actor-Mixer Hierarchy/Default Work Unit.wwu')
    peaks = []
    for step in range(1, 6):
        sound = actor.find(f".//Sound[@Name='HS_GazeSurge{step}']")
        key = int(sound.find('ChildrenList/AudioFileSource/MediaIDList/MediaID').get('ID'))
        pcm = chunks(checked[key][2], 12)[b'data'][1]
        assert len(pcm) == 23040
        peaks.append(max(abs(value[0]) for value in struct.iter_unpack('<h', pcm)))
    assert peaks == sorted(set(peaks)) and peaks[-1] < 16384
    report = dict(result='GAZE_SURGES_PASS', bank_sha256=hashlib.sha256(result).hexdigest().upper(),
                  preserved_existing_media=35, preserved_existing_objects=len(old_objects), restored_media=restored,
                  added_media=5, gains=GAINS, embedded_peak_pcm16=peaks, duration_seconds=.24, loops_added=0)
    (ROOT / 'art/audio/gaze-surges-validation.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(report))


if __name__ == '__main__':
    (prepare if sys.argv[1] == 'prepare' else preserve_and_check)(Path(sys.argv[2]))
