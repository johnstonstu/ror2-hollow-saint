"""Add Orb charge/throw and a quiet dash accent; preserve all prior bank objects.

prepare <baseline-bank> <output-dir>, then WwiseConsole generate-soundbank,
then check <baseline-bank> <output-dir>. Original synthesis and existing original
ChainHop/ThunderRelease/SpearThrow sources only; no external recordings.
"""
from pathlib import Path
import copy
import hashlib
import json
import struct
import sys
import uuid
import wave
import xml.etree.ElementTree as ET
import numpy as np
from author_bank import guid, prop, save
from gaze_surges import chunks, media, hierarchy

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / 'art/audio/HollowSaintAudio'
SOURCE = ROOT / 'art/audio/source'
BANK = PROJECT / 'GeneratedSoundBanks/Windows/HollowSaint.bnk'
BASE_SHA = '06F01337E219EBFE0781B71AB544312888C23324918ACC9F47B598E0AEC88117'
RATE = 48000
ROLES = {'OrbChargeStart': .55, 'OrbChargeLoop': 2., 'OrbThrow': .48, 'ArcStepCrackle': .22}


def baseline(path):
    data = path.read_bytes()
    assert hashlib.sha256(data).hexdigest().upper() == BASE_SHA, 'Unexpected pre-polish bank'
    return data


def read(name):
    with wave.open(str(SOURCE / (name + '.wav')), 'rb') as wav:
        assert wav.getparams()[:3] == (1, 2, RATE)
        return np.frombuffer(wav.readframes(wav.getnframes()), '<i2').astype(float) / 32768


def normalized(x):
    return x / max(1e-9, np.max(np.abs(x)))


def band(x, lo, hi):
    f = np.fft.rfftfreq(len(x), 1 / RATE)
    weight = np.clip((f - lo * .6) / (lo * .8), 0, 1)
    weight *= np.clip((hi * 1.4 - f) / (hi * .8), 0, 1)
    return np.fft.irfft(np.fft.rfft(x) * weight, len(x))


def place(x, sample, at, gain):
    start = round(at * RATE)
    length = min(len(sample), len(x) - start)
    x[start:start + length] += sample[:length] * gain


def finish(x, peak_db, fade=.008):
    x = x - x.mean()
    n = min(round(fade * RATE), len(x) // 2)
    x[:n] *= np.linspace(0, 1, n)
    x[-n:] *= np.linspace(1, 0, n)
    return normalized(x) * 10 ** (peak_db / 20)


def sounds():
    rng = np.random.default_rng(1321010)
    crackle = normalized(band(read('ChainHop'), 400, 6200))
    release = normalized(read('ThunderRelease'))
    whoosh = normalized(band(read('SpearThrow'), 140, 2100))
    t = np.arange(round(.55 * RATE)) / RATE
    progress = t / .55
    phase = 2 * np.pi * (135 * t + 180 * t * t / 1.1)
    buzz = np.sin(phase) + .28 * np.sin(phase * 3)
    charge = .38 * buzz * (.18 + .82 * progress) * np.minimum(1, t / .04)
    charge += .34 * normalized(band(rng.normal(size=len(t)), 500, 4600)) * progress
    for at, gain in ((.025, .12), (.15, .18), (.29, .24), (.40, .32)):
        place(charge, crackle, at, gain)

    t = np.arange(2 * RATE) / RATE
    # Periodic low bed with irregular sparks: electrical texture, not a repeated ready beep.
    loop = .16 * (np.sin(2*np.pi*165*t) + .22*np.sin(2*np.pi*330*t))
    loop *= .8 + .2 * np.sin(2*np.pi*3*t)
    loop += .15 * normalized(band(rng.normal(size=len(t)), 450, 5200))
    for at in (.08, .31, .57, .76, 1.02, 1.36, 1.64, 1.79):
        place(loop, crackle, at, rng.uniform(.15, .35))

    t = np.arange(round(.48 * RATE)) / RATE
    throw = np.zeros(len(t))
    place(throw, release, .0, .9)
    place(throw, whoosh, .012, .7)
    place(throw, crackle, .085, .35)
    place(throw, crackle, .22, .17)
    throw += .30 * np.sin(2*np.pi*(145*t-65*t*t)) * np.exp(-t/.065)

    dash = np.zeros(round(.22 * RATE))
    place(dash, crackle, 0, 1)
    place(dash, release, .018, .2)
    return {'OrbChargeStart': finish(charge, -9),
            'OrbChargeLoop': finish(loop, -17, .004),
            'OrbThrow': finish(throw, -9),
            'ArcStepCrackle': finish(dash, -23)}


def write_wav(path, x):
    with wave.open(str(path), 'wb') as wav:
        wav.setparams((1, 2, RATE, 0, 'NONE', 'not compressed'))
        wav.writeframes(np.rint(x * 32767).astype('<i2').tobytes())


def author_additions():
    paths = {name: PROJECT / name / 'Default Work Unit.wwu'
             for name in ('Actor-Mixer Hierarchy', 'Events', 'SoundBanks')}
    trees = {name: ET.parse(path) for name, path in paths.items()}
    actor, events, banks = (trees[name] for name in paths)
    children = actor.find('.//WorkUnit/ChildrenList')
    event_children = events.find('.//WorkUnit/ChildrenList')
    inclusion = banks.find(".//SoundBank[@Name='HollowSaint']/ObjectInclusionList")
    for role in ROLES:
        name = 'HS_' + role
        assert actor.find(f".//Sound[@Name='{name}']") is None, 'Already authored: ' + name
        template = 'HS_SpearChargeLoop' if role.endswith('Loop') else 'HS_SpearChargeStart'
        sound = copy.deepcopy(actor.find(f".//Sound[@Name='{template}']"))
        sound.attrib.update(Name=name, ID=guid(name), ShortID=str(uuid.UUID(guid(name)).int & 0x3fffffff))
        # Source levels own the new mix. Do not inherit the spear loop's -5 dB trim.
        volume = sound.find("PropertyList/Property[@Name='Volume']")
        if volume is not None:
            sound.find('PropertyList').remove(volume)
        prop(sound, 'Priority', 'int16', 70 if role.endswith('Loop') else 85)
        audio = sound.find('ChildrenList/AudioFileSource')
        audio.attrib.update(Name=role, ID=guid(name + '/source'))
        audio.find('AudioFile').text = role + '.wav'
        audio.find('MediaIDList/MediaID').set('ID', str(uuid.UUID(guid(name + '/media')).int & 0x3fffffff))
        sound.find('ActiveSourceList/ActiveSource').attrib.update(Name=role, ID=audio.get('ID'))
        children.append(sound)
        for stop in ((False, True) if role.endswith('Loop') else (False,)):
            prefix = 'Stop_' if stop else 'Play_'
            event_name = prefix + name
            event = copy.deepcopy(events.find(f".//Event[@Name='{prefix + template}']"))
            event.attrib.update(Name=event_name, ID=guid(event_name))
            action = event.find('ChildrenList/Action')
            action.set('ID', guid(event_name + '/action'))
            action.find("ReferenceList/Reference[@Name='Target']/ObjectRef").attrib.update(Name=name, ID=sound.get('ID'))
            event_children.append(event)
            ET.SubElement(inclusion, 'ObjectRef', Name=event_name, ID=event.get('ID'),
                          WorkUnitID=events.find('.//WorkUnit').get('ID'), Origin='Manual', Filter='7')
        (PROJECT / 'Originals/SFX' / (role + '.wav')).write_bytes((SOURCE / (role + '.wav')).read_bytes())
    for name, path in paths.items():
        save(path, trees[name])
    event_path = ROOT / 'art/audio/events.json'
    names = json.loads(event_path.read_text())
    names += ['Play_HS_' + role for role in ROLES] + ['Stop_HS_OrbChargeLoop']
    event_path.write_text(json.dumps(sorted(names), indent=2) + '\n')


def prepare(path, out):
    baseline(path)
    out.mkdir(parents=True, exist_ok=True)
    manifest_path = SOURCE / 'manifest.json'
    manifest = json.loads(manifest_path.read_text())
    rendered = sounds()
    for name, x in rendered.items():
        assert not (SOURCE / (name + '.wav')).exists(), 'Do not overwrite an existing cue'
        write_wav(SOURCE / (name + '.wav'), x)
        manifest.append(dict(name=name, event='Play_HS_' + name, seconds=ROLES[name],
                             loop=name.endswith('Loop'), peak_dbfs=round(20*np.log10(np.max(np.abs(x))), 2),
                             rms_dbfs=round(20*np.log10(np.sqrt(np.mean(x*x))), 2),
                             source='Original synthesis plus original ChainHop/ThunderRelease/SpearThrow'))
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
    author_additions()
    # Dry design preview: charged cast, gap, original dash, gap, dash with accent.
    preview = np.zeros(round(7.5 * RATE))
    place(preview, rendered['OrbChargeStart'], 0, 1)
    loop = rendered['OrbChargeLoop'].copy()
    loop[-round(.07*RATE):] *= np.linspace(1, 0, round(.07*RATE))
    place(preview, loop, .22, 1)
    place(preview, rendered['OrbThrow'], 2.15, 1)
    dash = read('ArcStepStart') * 10**(2/20)  # existing authored Wwise trim
    place(preview, dash, 4.0, 1)
    place(preview, dash, 5.6, 1)
    place(preview, rendered['ArcStepCrackle'], 5.6, 1)
    assert np.max(np.abs(preview)) < 1
    write_wav(out / 'orb-and-dash-preview.wav', preview)
    print('Prepared four isolated cues; dry preview:', out / 'orb-and-dash-preview.wav')


def check(path, out):
    old, current = baseline(path), bytearray(BANK.read_bytes())
    before, after = media(old), media(current)
    old_objects, new_objects = hierarchy(old), hierarchy(current)
    assert all(new_objects.get(key) == record for key, record in old_objects.items()), 'Existing bank routing changed'
    assert len(after) == len(before) + 4 and len(new_objects) == len(old_objects) + 14
    # Wwise may re-convert untouched recorded assets. Restore only identical-sized old slots.
    data_start = chunks(current)[b'DATA'][0]
    restored = 0
    for key, (_, size, wem) in before.items():
        offset, new_size, authored = after[key]
        assert new_size == size, ('Existing media size changed', key)
        if authored != wem:
            current[data_start + offset:data_start + offset + size] = wem
            restored += 1
    checked = media(current)
    assert all(checked[key][2] == value[2] for key, value in before.items())
    peaks = {}
    for name, seconds in ROLES.items():
        key = uuid.UUID(guid('HS_' + name + '/media')).int & 0x3fffffff
        pcm = chunks(checked[key][2], 12)[b'data'][1]
        samples = np.frombuffer(pcm, '<i2').astype(np.int32)
        assert len(samples) == round(seconds * RATE)
        peak = np.abs(samples).max()
        assert 0 < peak < 16384 and abs(samples[0]) < 4 and abs(samples[-1]) < 4
        peaks[name] = int(peak)
    assert peaks['ArcStepCrackle'] < peaks['OrbThrow'] / 4
    BANK.write_bytes(current)
    result = dict(result='ORB_DASH_AUDIO_PASS', baseline_sha256=BASE_SHA,
                  bank_sha256=hashlib.sha256(current).hexdigest().upper(),
                  unchanged_media=len(before), unchanged_routing_objects=len(old_objects),
                  restored_media=restored, added_media=4, added_loop=1, peaks=peaks,
                  limits='Offline source/bank validation and dry preview; native mix requires playtest.')
    (ROOT / 'art/audio/orb-dash-validation.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))


if __name__ == '__main__':
    {'prepare': prepare, 'check': check}[sys.argv[1]](Path(sys.argv[2]), Path(sys.argv[3]))
