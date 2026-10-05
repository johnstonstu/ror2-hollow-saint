"""Add isolated spear charge cues while preserving the audited Gaze bank verbatim."""
from pathlib import Path
import hashlib
import json
import struct
import sys
import wave
from gaze_surges import chunks, media, hierarchy

ROOT = Path(__file__).resolve().parents[2]
BANK = ROOT / 'art/audio/HollowSaintAudio/GeneratedSoundBanks/Windows/HollowSaint.bnk'
BASE_SHA = '16813B4B13471635796BECCBABD1ACA75463F705AE3589350FB69DE9E0E57524'
CUES = (('SpearChargeStart', 'FanStart', 1.25, .2, False),
        ('SpearChargeLoop', 'FanLoop', 2., 2., True))


def baseline(path):
    data = path.read_bytes()
    assert hashlib.sha256(data).hexdigest().upper() == BASE_SHA
    return data


def prepare(path):
    baseline(path)
    source = ROOT / 'art/audio/source'
    manifest_path = source / 'manifest.json'
    manifest = [entry for entry in json.loads(manifest_path.read_text())
                if not entry['name'].startswith('SpearCharge')]
    for name, original, gain, seconds, loop in CUES:
        with wave.open(str(source / (original + '.wav')), 'rb') as wav:
            assert wav.getparams()[:3] == (1, 2, 48000)
            pcm = [round(value[0] * gain) for value in struct.iter_unpack('<h', wav.readframes(wav.getnframes()))]
        assert len(pcm) == round(seconds * 48000)
        peak = max(abs(value) for value in pcm)
        assert peak < 16384
        with wave.open(str(source / (name + '.wav')), 'wb') as wav:
            wav.setparams((1, 2, 48000, 0, 'NONE', 'not compressed'))
            wav.writeframes(struct.pack('<' + 'h' * len(pcm), *pcm))
        manifest.append(dict(name=name, event='Play_HS_' + name, seconds=seconds,
                             loop=loop, peak_pcm16=peak, gain=gain,
                             source='Audited existing ' + original + ' waveform'))
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
    print('SPEAR_CHARGE_PREPARED', json.dumps(manifest[-2:]))


def preserve_and_check(path):
    old = baseline(path)
    current = bytearray(BANK.read_bytes())
    old_hirc, new_hirc = hierarchy(old), hierarchy(current)
    for key, record in old_hirc.items():
        assert new_hirc[key] == record, key
    old_media, new_media = media(old), media(current)
    assert len(old_media) == 40 and len(new_media) == 42
    data_start = chunks(current)[b'DATA'][0]
    for key, (_, _, wem) in old_media.items():
        offset, length, authored = new_media[key]
        assert length == len(wem)
        if authored != wem:
            current[data_start + offset:data_start + offset + length] = wem
    BANK.write_bytes(current)
    preserved = media(current)
    assert all(preserved[key][2] == wem for key, (_, _, wem) in old_media.items())
    result = dict(result='SPEAR_CHARGE_AUDIO_PASS', bank_sha256=hashlib.sha256(current).hexdigest().upper(),
                  baseline_sha256=BASE_SHA, unchanged_media=40, unchanged_routing_objects=len(old_hirc),
                  added_media=2, added_loop=1, loop_cleanup='Per-playing-ID stop; emitter-local authored stop also provided',
                  release='Existing single SpearThrow or SpearThrowHeavy event; no duplicated throw layer')
    (ROOT / 'art/audio/spear-charge-validation.json').write_text(json.dumps(result, indent=2) + '\n')
    print('SPEAR_CHARGE_AUDIO_PASS', json.dumps(result))


if __name__ == '__main__':
    {'prepare': prepare, 'check': preserve_and_check}[sys.argv[1]](Path(sys.argv[2]))
