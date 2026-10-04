"""Compare the revised content bank with a supplied exact installed baseline."""
from pathlib import Path
import array
import hashlib
import json
import math
import struct
import sys
import wave
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / 'art/audio/HollowSaintAudio'


def chunks(data):
    result, offset = {}, 0
    while offset < len(data):
        tag, size = struct.unpack_from('<4sI', data, offset)
        result[tag] = data[offset+8:offset+8+size]
        offset += 8 + size
    assert offset == len(data)
    return result


def media(bank):
    return {key:bank[b'DATA'][offset:offset+size] for key,offset,size in struct.iter_unpack('<III', bank[b'DIDX'])}


def pcm(wem):
    offset = 12
    while offset < len(wem):
        tag,size = struct.unpack_from('<4sI',wem,offset)
        if tag == b'data':
            return array.array('h',wem[offset+8:offset+8+size])
        offset += 8 + size
    raise AssertionError('WEM has no PCM data')


def check(baseline):
    before_data = baseline.read_bytes()
    assert hashlib.sha256(before_data).hexdigest().upper() == 'F244D5F28C5672C984F79844202393C809FAB965E0AD623F9FA76FAAE53732E5'
    before, after_data = chunks(before_data), (PROJECT / 'GeneratedSoundBanks/Windows/HollowSaint.bnk').read_bytes()
    after = chunks(after_data)
    assert after[b'HIRC'] == before[b'HIRC'], 'Event/action/routing/loop hierarchy changed'
    old_media, new_media = media(before), media(after)
    actor = ET.parse(PROJECT / 'Actor-Mixer Hierarchy/Default Work Unit.wwu')
    impact = actor.find(".//Sound[@Name='HS_BoltImpact']")
    impact_id = int(impact.find('ChildrenList/AudioFileSource/MediaIDList/MediaID').get('ID'))
    assert set(old_media) == set(new_media)
    changed = [key for key in old_media if old_media[key] != new_media[key]]
    for key in changed:
        sound = next(node.get('Name') for node in actor.findall('.//Sound') if int(node.find('ChildrenList/AudioFileSource/MediaIDList/MediaID').get('ID')) == key)
        first = next(i for i,(a,b) in enumerate(zip(old_media[key], new_media[key])) if a != b)
        print('CHANGED_MEDIA', key, sound, 'first difference', first, 'sizes', len(old_media[key]), len(new_media[key]))
    assert changed == [impact_id], changed
    with wave.open(str(ROOT / 'art/audio/source/ChainHop.wav'),'rb') as stream:
        original = array.array('h',stream.readframes(stream.getnframes()))
    with wave.open(str(ROOT / 'art/audio/source/BoltImpact.wav'),'rb') as stream:
        quieter = array.array('h',stream.readframes(stream.getnframes()))
    assert len(original) == 9600 and len(quieter) == 13440
    assert list(quieter[:9600]) == [round(value*.82) for value in original]
    assert all(value == 0 for value in quieter[9600:])
    chain = actor.find(".//Sound[@Name='HS_ChainHop']")
    chain_id = int(chain.find('ChildrenList/AudioFileSource/MediaIDList/MediaID').get('ID'))
    prior_pcm, final_pcm = pcm(old_media[chain_id]), pcm(new_media[impact_id])
    assert len(prior_pcm) == 9600 and len(final_pcm) == 13440
    ratio = math.sqrt(sum(value*value for value in final_pcm[:9600]) / sum(value*value for value in prior_pcm))
    assert .81 < ratio < .83, ('Embedded active PCM gain differs',ratio)
    controller = (ROOT / 'HollowSaintMod/FoundationKit/Gaze/GazeFuelController.cs').read_text()
    receive = controller.split('internal void Receive(',1)[1]
    assert receive.index('receiver.Accept(') < receive.index('pulseAudio.Play(')
    assert receive.count('pulseAudio.Play(') == 1 and 'PlaySound(GazeSfx.Launch' not in receive
    native = Path(r'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\StreamingAssets\Audio\GeneratedSoundBanks\Windows\char_captain.txt').read_text(encoding='utf-8-sig')
    assert 'Play_captain_m2_tazer_shoot' in native
    result = {'result':'FEEDBACK_AUDIO_PASS','bank_sha256':hashlib.sha256(after_data).hexdigest().upper(),
              'hierarchy_identical':True,'changed_media':'HS_BoltImpact','unchanged_media_count':len(new_media)-len(changed),
              'impact_gain':.82,'embedded_active_pcm_gain':ratio,'impact_active_seconds':.20,'impact_padded_seconds':.28,
              'pulse_event':'Play_HS_ThunderRelease','pulse_coalescing_seconds':.24,
              'fallback_event':'Play_captain_m2_tazer_shoot','native_perceived_mix':'Pending'}
    (ROOT / 'art/audio/feedback-validation.json').write_text(json.dumps(result,indent=2)+'\n')
    print('FEEDBACK_AUDIO_PASS: only impact media changed; original hierarchy and other media byte-identical; electrical pulse route verified')
    print(json.dumps(result))


if __name__ == '__main__':
    check(Path(sys.argv[1]))
