"""Validate this PCM Wwise 2023.1 bank and its authoring contract; no game required.

Success: version150, every declared role/event embedded, emitter-local Stops,
infinite loops, SFX_BUS references without owning any bus, and unclipped PCM.
Binary field offsets below apply only to this version150 PCM sound layout.
"""
from pathlib import Path
from collections import Counter
import hashlib
import json
import struct
import wave
import xml.etree.ElementTree as ET
import numpy as np
from synthesize_sfx import ROLES as SYNTH_ROLES
from import_samples import CUES

# v0.9.1: sampled cues (import_samples.py) replace or extend the synthesized set; only the duration
# (index 0) is checked here.
ROLES = dict(SYNTH_ROLES)
for _name, _cue in CUES.items():
    ROLES[_name] = (_cue[2],)

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "art/audio/HollowSaintAudio"
BANK = PROJECT / "GeneratedSoundBanks/Windows/HollowSaint.bnk"


def hash_id(name):
    value = 2166136261
    for char in name.lower().encode("ascii"):
        value = ((value * 16777619) & 0xffffffff) ^ char
    return value


def check():
    data = BANK.read_bytes()
    chunks, pos = {}, 0
    while pos < len(data):
        tag, size = struct.unpack_from("<4sI", data, pos)
        chunks[tag] = data[pos + 8:pos + 8 + size]
        pos += size + 8
    assert pos == len(data)
    assert struct.unpack_from("<I", chunks[b"BKHD"])[0] == 150
    source_count = len(ROLES)
    stop_roles = {name for name in ROLES if name.endswith("Loop")} | {"ThunderTelegraph"}
    expected_events = {"Play_HS_" + name for name in ROLES} | {"Stop_HS_" + name for name in stop_roles}
    assert len(chunks[b"DIDX"]) == source_count * 12
    records, types, pos = {}, Counter(), 4
    hirc = chunks[b"HIRC"]
    for _ in range(struct.unpack_from("<I", hirc)[0]):
        kind, size = struct.unpack_from("<BI", hirc, pos)
        payload = hirc[pos + 5:pos + 5 + size]
        records[struct.unpack_from("<I", payload)[0]] = (kind, payload)
        types[kind] += 1
        pos += size + 5
    assert pos == len(hirc)
    assert types == {2: source_count, 3: len(expected_events), 4: len(expected_events), 14: 1}, types
    names = json.loads((ROOT / "art/audio/events.json").read_text())
    assert len(names) == len(expected_events) and set(names) == expected_events
    actor = ET.parse(PROJECT / "Actor-Mixer Hierarchy/Default Work Unit.wwu")
    sounds = {node.get("Name"): node for node in actor.findall(".//Sound")}
    assert set(sounds) == {"HS_" + name for name in ROLES}
    events = ET.parse(PROJECT / "Events/Default Work Unit.wwu")
    event_nodes = {node.get("Name"): node for node in events.findall(".//Event")}
    for name in names:
        kind, event = records[hash_id(name)]
        assert kind == 4 and event[4] == 1
        kind, action = records[struct.unpack_from("<I", event, 5)[0]]
        assert kind == 3
        stop = name.startswith("Stop_")
        assert action[4:6] == (b"\x03\x01" if stop else b"\x03\x04")
        sound = sounds[name.split("_", 1)[1]]
        assert struct.unpack_from("<I", action, 6)[0] == int(sound.get("ShortID"))
        if stop:
            props = event_nodes[name].find("ChildrenList/Action/PropertyList")
            assert props.find("Property[@Name='Scope']").get("Value") == "0"
            fade = "0.03" if name == "Stop_HS_ThunderTelegraph" else "0.12"
            assert props.find("Property[@Name='FadeTime']").get("Value") == fade
    for name, node in sounds.items():
        kind, sound = records[int(node.get("ShortID"))]
        assert kind == 2 and struct.unpack_from("<I", sound, 22)[0] == 213475909
        if name.endswith("Loop"):
            # Property bundle: LoopCount(0x54)=0 means infinite; 0x55 attenuation ID.
            assert sound[31:38] == b"\x02\x54\x55\x00\x00\x00\x00"
            for prop in ("IsLoopingEnabled", "IsLoopingInfinite"):
                assert node.find(f"PropertyList/Property[@Name='{prop}']").get("Value").lower() == "true"
    metrics = []
    for source in sorted((ROOT / "art/audio/source").glob("*.wav")):
        with wave.open(str(source), "rb") as stream:
            assert stream.getnchannels() == 1 and stream.getframerate() == 48000
            assert stream.getnframes() == round(ROLES[source.stem][0] * 48000)
            pcm = np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2")
        peak = int(np.abs(pcm.astype(np.int32)).max())
        assert peak < 32767
        metrics.append({"name": source.stem, "peak_pcm16": peak, "clipped_samples": 0})
    result = {"bank_version": 150, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest(),
              "events": len(names), "local_stop_actions": len(stop_roles),
              "infinite_loops": sum(name.endswith("Loop") for name in ROLES), "embedded_pcm_sources": source_count,
              "bus_reference": 213475909, "owned_busses": 0, "source_clipping": metrics}
    (ROOT / "art/audio/bank-validation.json").write_text(json.dumps(result, indent=2) + "\n")
    print("BANK_CHECK_PASS", json.dumps({key: value for key, value in result.items() if key != "source_clipping"}))


if __name__ == "__main__":
    check()
