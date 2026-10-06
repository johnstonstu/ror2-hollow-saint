"""Revoice only Gaze surge PCM; preserve bank layout/events and accepted first cue.

Usage: python gaze_surge_escalation.py prepare|check baseline.bnk
The baseline must be the accepted 8d6696d3 bank, retained outside the worktree.
"""
from pathlib import Path
import hashlib
import json
import math
import struct
import sys
import wave
import xml.etree.ElementTree as ET
from gaze_surges import chunks, media, hierarchy, ROOT, PROJECT, BANK

BASE_SHA = 'EFA769269A0A1A1546D872256B163A0C46172BC1E6D533DAFE831929EFE58F59'


def samples(pcm):
    return [item[0] for item in struct.iter_unpack('<h', pcm)]


def metrics(values):
    peak = max(abs(value) for value in values)
    rms = math.sqrt(sum(value * value for value in values) / len(values))
    return dict(peak_pcm16=peak, peak_dbfs=round(20 * math.log10(peak / 32768), 2),
                rms_dbfs=round(20 * math.log10(rms / 32768), 2))


def run(mode, baseline):
    old = baseline.read_bytes()
    assert hashlib.sha256(old).hexdigest().upper() == BASE_SHA
    original = media(old)
    actor = ET.parse(PROJECT / 'Actor-Mixer Hierarchy/Default Work Unit.wwu')
    ids = [int(actor.find(f".//Sound[@Name='HS_GazeSurge{step}']/ChildrenList/AudioFileSource/MediaIDList/MediaID").get('ID')) for step in range(1, 6)]
    base_pcm = samples(chunks(original[ids[0]][2], 12)[b'data'][1])
    assert len(base_pcm) == 11520
    if mode == 'prepare':
        output = bytearray(old)
        data_start = chunks(old)[b'DATA'][0]
        manifest_path = ROOT / 'art/audio/source/manifest.json'
        manifest = json.loads(manifest_path.read_text())
        for step, key in enumerate(ids, 1):
            if step == 1:
                continue  # Accepted first pulse and baseline Gaze audio are untouched.
            strength = (step - 1) / 4
            values = []
            for index, value in enumerate(base_pcm):
                t = index / 48000
                # Finite attack/decay and increasingly low electrical rumble with
                # sharp harmonics; no periodic source/loop or random clipping.
                envelope = min(1, t / .008) * max(0, 1 - t / .24) ** 1.5
                phase = 2 * math.pi * (105 * t + 180 * t * t)
                rumble = math.sin(phase) + .45 * math.sin(3 * phase) + .22 * math.sin(7 * phase)
                enhanced = value * (1 + .9 * strength) + 2900 * strength * envelope * rumble
                values.append(round(enhanced))
            assert max(abs(value) for value in values) < 16384, '6 dB minimum headroom'
            pcm = struct.pack('<' + 'h' * len(values), *values)
            start, size, wem = original[key]
            pcm_start, prior_pcm = chunks(wem, 12)[b'data']
            assert len(prior_pcm) == len(pcm)
            output[data_start + start + pcm_start:data_start + start + pcm_start + len(pcm)] = pcm
            name = f'GazeSurge{step}'
            for path in (ROOT / f'art/audio/source/{name}.wav', PROJECT / f'Originals/SFX/{name}.wav'):
                with wave.open(str(path), 'wb') as target:
                    target.setparams((1, 2, 48000, 0, 'NONE', 'not compressed'))
                    target.writeframes(pcm)
            entry = next(entry for entry in manifest if entry['name'] == name)
            entry.update({k: v for k, v in metrics(values).items() if k != 'peak_pcm16'})
        BANK.write_bytes(output)
        manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
    new = BANK.read_bytes()
    current = media(new)
    assert len(original) == len(current) == 42
    assert hierarchy(old) == hierarchy(new)
    assert len(old) == len(new)
    assert {k: v for k, v in chunks(old).items() if k != b'DATA'} == {k: v for k, v in chunks(new).items() if k != b'DATA'}
    modified = set(ids[1:])
    for key, (start, size, wem) in original.items():
        assert current[key][:2] == (start, size)
        if key not in modified:
            assert current[key][2] == wem
        else:
            before, after = chunks(wem, 12), chunks(current[key][2], 12)
            assert {k: v for k, v in before.items() if k != b'data'} == {k: v for k, v in after.items() if k != b'data'}
            assert len(before[b'data'][1]) == len(after[b'data'][1]) == 23040
            for path in (ROOT / f'art/audio/source/GazeSurge{ids.index(key)+1}.wav', PROJECT / f'Originals/SFX/GazeSurge{ids.index(key)+1}.wav'):
                with wave.open(str(path), 'rb') as source:
                    assert source.getparams()[:3] == (1, 2, 48000)
                    assert source.readframes(source.getnframes()) == after[b'data'][1]
    readings = [metrics(samples(chunks(current[key][2], 12)[b'data'][1])) for key in ids]
    peaks = [reading['peak_pcm16'] for reading in readings]
    rms = [reading['rms_dbfs'] for reading in readings]
    assert peaks == sorted(set(peaks)) and max(peaks) < 16384
    assert rms == sorted(set(rms))
    report = dict(result='PASS', baseline_sha256=BASE_SHA, bank_sha256=hashlib.sha256(new).hexdigest().upper(),
                  preserved_media=38, modified_media=4, hierarchy_objects=len(hierarchy(old)),
                  bank_layout_and_routing_unchanged=True, first_pulse_unchanged=True,
                  duration_seconds=.24, loops_added=0, master_volume_changes=0, stages=readings)
    (ROOT / 'art/audio/gaze-surge-escalation-validation.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report))


if __name__ == '__main__':
    assert sys.argv[1] in ('prepare', 'check')
    run(sys.argv[1], Path(sys.argv[2]))
