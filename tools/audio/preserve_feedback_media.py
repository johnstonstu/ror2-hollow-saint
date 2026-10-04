"""Restore untouched WEMs after recovering a missing authoring WAV.

Wwise reprocesses a recovered sampled source even when its input came from the
old bank. Keep that sampled WEM byte-identical to the installed bank. Abort if
any other unrelated source changes. Only BoltImpact is allowed to ship changed.
"""
from pathlib import Path
import hashlib
import struct
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / 'art/audio/HollowSaintAudio'


def chunks(data, offset=0):
    result = {}
    while offset < len(data):
        tag, size = struct.unpack_from('<4sI', data, offset)
        result[tag] = (offset + 8, data[offset+8:offset+8+size])
        offset += 8 + size
    return result


def preserve(baseline):
    old = baseline.read_bytes()
    assert hashlib.sha256(old).hexdigest().upper() == 'F244D5F28C5672C984F79844202393C809FAB965E0AD623F9FA76FAAE53732E5'
    path = PROJECT / 'GeneratedSoundBanks/Windows/HollowSaint.bnk'
    new = path.read_bytes()
    before, after = chunks(old), chunks(new)
    assert before[b'HIRC'][1] == after[b'HIRC'][1]
    assert before[b'DIDX'][1] == after[b'DIDX'][1]
    actor = ET.parse(PROJECT / 'Actor-Mixer Hierarchy/Default Work Unit.wwu')
    impact = actor.find(".//Sound[@Name='HS_BoltImpact']")
    allowed = int(impact.find('ChildrenList/AudioFileSource/MediaIDList/MediaID').get('ID'))
    recovered = actor.find(".//Sound[@Name='HS_SpearBurst']")
    recoverable = int(recovered.find('ChildrenList/AudioFileSource/MediaIDList/MediaID').get('ID'))
    result, restored = bytearray(new), 0
    for key,offset,size in struct.iter_unpack('<III', before[b'DIDX'][1]):
        if key == allowed:
            continue
        old_wem = before[b'DATA'][1][offset:offset+size]
        new_wem = after[b'DATA'][1][offset:offset+size]
        if old_wem != new_wem:
            assert key == recoverable, ('Unexpected unrelated media rewrite', key)
            start = after[b'DATA'][0] + offset
            result[start:start+size] = old_wem
            restored += 1
    path.write_bytes(result)
    print(f'UNRELATED_MEDIA_PRESERVED: {restored} recovered sample WEM restored; all unrelated media now byte-identical')


if __name__ == '__main__':
    preserve(Path(sys.argv[1]))
