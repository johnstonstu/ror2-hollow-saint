"""Dedicated main impact: current ChainHop crackle at 0.82 gain, other media untouched.

Run after synthesis/sample import and before author_bank.py. Keep the historical
0.28-second media duration/ID, padding the 0.20-second cue with silence.
"""
from pathlib import Path
import array
import json
import math
import wave

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/audio/source'


def quiet():
    original = SOURCE / 'BoltImpact.wav'
    with wave.open(str(original), 'rb') as stream:
        assert stream.getparams()[:3] == (1, 2, 48000)
        assert stream.getnframes() == 13440
    with wave.open(str(SOURCE / 'ChainHop.wav'), 'rb') as stream:
        assert stream.getparams()[:3] == (1, 2, 48000)
        samples = array.array('h', stream.readframes(stream.getnframes()))
    assert len(samples) == 9600
    quieter = array.array('h', (round(value * .82) for value in samples))
    quieter.extend([0] * (13440 - len(quieter)))
    with wave.open(str(original), 'wb') as stream:
        stream.setparams((1, 2, 48000, 0, 'NONE', 'not compressed'))
        stream.writeframes(quieter.tobytes())
    manifest = SOURCE / 'manifest.json'
    entries = json.loads(manifest.read_text())
    entry = next(value for value in entries if value['name'] == 'BoltImpact')
    entry['peak_dbfs'] = round(20 * math.log10(max(abs(x) for x in quieter) / 32768), 2)
    entry['rms_dbfs'] = round(20 * math.log10(math.sqrt(sum(x*x for x in quieter) / len(quieter)) / 32768), 2)
    manifest.write_text(json.dumps(entries, indent=2) + '\n', encoding='utf-8')
    print('ARC_IMPACT_SOURCE_GAIN_PASS: ChainHop copy at0.82; other source media unchanged')


if __name__ == '__main__':
    quiet()
