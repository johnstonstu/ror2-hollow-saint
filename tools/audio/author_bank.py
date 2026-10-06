"""Apply deterministic sound properties/routing after Wwise tab import, then build.

Only the content bank HollowSaint.bnk belongs in the mod. Never ship/load Init.bnk.
Property names/types/enums are from installed Wwise 2023.1.4 WObjects.xml.
"""
from pathlib import Path
import json
import subprocess
import uuid
import wave
import struct
import shutil
import copy
import xml.etree.ElementTree as ET
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "art/audio/HollowSaintAudio"
CONSOLE = Path(r"C:\Audiokinetic\Wwise_2023.1.4.8496\Authoring\x64\Release\bin\WwiseConsole.exe")
BUS_ID = "{238451CA-7226-4DFA-BEB5-3AB191D75E11}"


def guid(name):
    return "{" + str(uuid.uuid5(uuid.NAMESPACE_URL, "HollowSaintAudio/" + name)).upper() + "}"


def ensure(node, tag, **attrs):
    match = node.find(tag + "".join(f"[@{key}='{value}']" for key, value in attrs.items()))
    return match if match is not None else ET.SubElement(node, tag, attrs)


def prop(node, name, kind, value):
    props = ensure(node, "PropertyList")
    node.remove(props)
    node.insert(0, props)
    old = props.find(f"Property[@Name='{name}']")
    if old is not None:
        props.remove(old)
    ET.SubElement(props, "Property", Name=name, Type=kind, Value=str(value))


def save(path, tree):
    ET.indent(tree, space="\t")
    tree.write(path, encoding="utf-8", xml_declaration=True)


def add_missing_sounds(actor):
    """Extend the existing valid Wwise sound schema with stable source/media IDs."""
    children = actor.find(".//WorkUnit/ChildrenList")
    existing = {node.get("Name") for node in children.findall("Sound")}
    template = children.find("Sound[@Name='HS_AirJump']")
    for source in sorted((ROOT / "art/audio/source").glob("*.wav")):
        name = "HS_" + source.stem
        if name in existing:
            continue
        sound = copy.deepcopy(template)
        sound.attrib.update(Name=name, ID=guid(name), ShortID=str(uuid.UUID(guid(name)).int & 0x3fffffff))
        audio = sound.find("ChildrenList/AudioFileSource")
        audio.attrib.update(Name=source.stem, ID=guid(name + "/source"))
        audio.find("AudioFile").text = source.name
        audio.find("MediaIDList/MediaID").set("ID", str(uuid.UUID(guid(name + "/media")).int & 0x3fffffff))
        sound.find("ActiveSourceList/ActiveSource").attrib.update(Name=source.stem, ID=audio.get("ID"))
        children.append(sound)


MIX_TRIM_DB = {
    "HS_CircuitPulse": 4.0,   # rose ~1 dB over the Circuit loop: the 0.5 s damage rhythm was inaudible
    "HS_CircuitClose": 3.0,   # crown close masked (rise ~1.4 dB)
}


def author():
    # Parse every existing document before modifying it.
    files = {name: PROJECT / name / "Default Work Unit.wwu" for name in
             ("Master-Mixer Hierarchy", "Actor-Mixer Hierarchy", "Events", "Attenuations", "SoundBanks")}
    trees = {name: ET.parse(path) for name, path in files.items()}
    bus_tree = trees["Master-Mixer Hierarchy"]
    master = bus_tree.find(".//Bus[@Name='Master Audio Bus']")
    bus = ensure(ensure(master, "ChildrenList"), "Bus", Name="SFX_BUS", ID=BUS_ID)
    bus.set("ShortID", "213475909")
    bus_workunit = bus_tree.find(".//WorkUnit").get("ID")

    atten_tree = trees["Attenuations"]
    atten_unit = atten_tree.find(".//WorkUnit")
    attenuation = ensure(ensure(atten_unit, "ChildrenList"), "Attenuation", Name="HS_Spatial", ID=guid("attenuation"))
    attenuation.set("ShortID", "819376501")
    prop(attenuation, "RadiusMax", "Real64", 40)
    usage = ensure(attenuation, "CurveUsageInfoList")
    usage.clear()
    curve = ET.SubElement(ET.SubElement(ET.SubElement(usage, "VolumeDryUsage"), "CurveUsageInfo",
                          Platform="Linked", CurveToUse="Custom"), "Curve", Name="VolumeDry", ID=guid("curve"))
    prop(curve, "Flags", "int32", 1)
    points = ET.SubElement(curve, "PointList")
    for index, (x, y) in enumerate(((0, 0), (5, 0), (12, -9), (24, -24), (40, -60))):
        point = ET.SubElement(points, "Point")
        for tag, value in (("XPos", x), ("YPos", y), ("Flags", 37 if index == 4 else 5)):
            ET.SubElement(point, tag).text = str(value)
    for name in ("VolumeWetGameUsage", "VolumeWetUserUsage"):
        ET.SubElement(ET.SubElement(usage, name), "CurveUsageInfo", Platform="Linked", CurveToUse="UseVolumeDry")
    for name in ("LowPassFilterUsage", "HighPassFilterUsage", "SpreadUsage", "FocusUsage"):
        ET.SubElement(ET.SubElement(usage, name), "CurveUsageInfo", Platform="Linked", CurveToUse="None")
    for prefix in ("Obstruction", "Occlusion", "Diffraction", "Transmission"):
        for suffix in ("VolumeUsage", "LPFUsage", "HPFUsage"):
            ET.SubElement(ET.SubElement(usage, prefix + suffix), "CurveUsageInfo", Platform="Linked", CurveToUse="UseProject")

    actor = trees["Actor-Mixer Hierarchy"]
    add_missing_sounds(actor)
    sounds = {node.get("Name"): node for node in actor.findall(".//Sound")}
    for name, node in sounds.items():
        ref = node.find("ReferenceList/Reference[@Name='OutputBus']/ObjectRef")
        ref.attrib.update(Name="SFX_BUS", ID=BUS_ID, WorkUnitID=bus_workunit)
        for key, kind, value in (("OverridePositioning", "bool", "True"), ("ListenerRelativeRouting", "bool", "True"),
                                  ("3DSpatialization", "int16", 1), ("EnableAttenuation", "bool", "True")):
            prop(node, key, kind, value)
        reference = ensure(ensure(node, "ReferenceList"), "Reference", Name="Attenuation")
        reference.clear()
        reference.set("Name", "Attenuation")
        ET.SubElement(reference, "ObjectRef", Name="HS_Spatial", ID=guid("attenuation"), WorkUnitID=atten_unit.get("ID"))
        # 1.2 in-game mix pass (tools/audio/analyze_capture.py on loopback captures):
        # per-sound Wwise volume trims in dB; source PCM stays untouched.
        if name in MIX_TRIM_DB:
            prop(node, "Volume", "Real64", MIX_TRIM_DB[name])
        if name.endswith("Loop"):
            prop(node, "IsLoopingEnabled", "bool", "True")
            prop(node, "IsLoopingInfinite", "bool", "True")

    events = trees["Events"]
    children = ensure(events.find(".//WorkUnit"), "ChildrenList")
    # Regenerate only the imported HS events, one action each, no duplicate import actions.
    for event in list(children):
        if event.get("Name", "").startswith(("Play_HS_", "Stop_HS_")):
            children.remove(event)
    event_names = []
    for name, node in sorted(sounds.items()):
        for stop in ((False, True) if name.endswith("Loop") or name == "HS_ThunderTelegraph" else (False,)):
            event_name = ("Stop_" if stop else "Play_") + name
            event_names.append(event_name)
            event = ET.SubElement(children, "Event", Name=event_name, ID=guid(event_name))
            action = ET.SubElement(ET.SubElement(event, "ChildrenList"), "Action", Name="", ID=guid(event_name + "/action"))
            prop(action, "ActionType", "int16", 2 if stop else 1)
            if stop or name.endswith("Loop"):
                prop(action, "FadeTime", "Real64", .03 if name == "HS_ThunderTelegraph" else .12)
            if stop:
                prop(action, "Scope", "int16", 0)  # stop only this event's emitter, never all Saints
            ref = ET.SubElement(ET.SubElement(action, "ReferenceList"), "Reference", Name="Target")
            ET.SubElement(ref, "ObjectRef", Name=name, ID=node.get("ID"), WorkUnitID=actor.find(".//WorkUnit").get("ID"))
    banks = trees["SoundBanks"].find(".//WorkUnit/ChildrenList")
    for bank in list(banks):
        if bank.get("Name", "").startswith(("Play_HS_", "Stop_HS_")):
            banks.remove(bank)
    inclusion = banks.find("SoundBank[@Name='HollowSaint']/ObjectInclusionList")
    inclusion.clear()
    for event in children:
        ET.SubElement(inclusion, "ObjectRef", Name=event.get("Name"), ID=event.get("ID"),
                      WorkUnitID=events.find(".//WorkUnit").get("ID"), Origin="Manual", Filter="7")
    originals = PROJECT / "Originals/SFX"
    originals.mkdir(parents=True, exist_ok=True)
    for source in (ROOT / "art/audio/source").glob("*.wav"):
        shutil.copyfile(source, originals / source.name)
    for name, path in files.items():
        save(path, trees[name])
    (ROOT / "art/audio/events.json").write_text(json.dumps(event_names, indent=2) + "\n", encoding="utf-8")


def audition():
    roles = ["Footstep", "FootstepRun", "Land", "GlideEnter", "GlideLoop", "GlideExit",
             "CircuitUnfold", "CircuitLoop", "CircuitPulse", "CircuitClose", "ArcBoltCast", "BoltImpact", "ChainHop"]
    chunks, timings = [], []
    count = 0
    for name in roles:
        with wave.open(str(ROOT / "art/audio/source" / (name + ".wav")), "rb") as stream:
            pcm = np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2").copy()
        if name.endswith("Loop"):
            fade = np.minimum(1, np.arange(len(pcm)) / 5760) * np.minimum(1, np.arange(len(pcm))[::-1] / 5760)
            pcm = (pcm * fade).astype("<i2")
        timings.append({"name": name, "start_seconds": round(count / 48000, 3)})
        chunks += [pcm, np.zeros(24000, dtype="<i2")]
        count += len(pcm) + 24000
    # Actual attack timing: gather at0, launch at0.35, impact at0.60 seconds.
    parts = []
    for name, offset in (("ThunderTelegraph", 0), ("ThunderRelease", .35), ("ThunderStrike", .60)):
        with wave.open(str(ROOT / "art/audio/source" / (name + ".wav")), "rb") as stream:
            parts.append((round(offset * 48000), np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2")))
    # v0.9.1: sized to the longest cue (the sampled ThunderStrike runs 2.4 s).
    sequence = np.zeros(max(round(1.85 * 48000), max(s + len(p) for s, p in parts)), dtype=np.int16)
    for (start, pcm), name in zip(parts, ("ThunderTelegraph", "ThunderRelease", "ThunderStrike")):
        offset = start / 48000
        sequence[start:start + len(pcm)] = pcm
        timings.append({"name": name, "start_seconds": round(count / 48000 + offset, 3)})
    chunks.append(sequence)
    with wave.open(str(ROOT / "art/audio/hollow-saint-audition.wav"), "wb") as stream:
        stream.setparams((1, 2, 48000, 0, "NONE", "not compressed"))
        stream.writeframes(np.concatenate(chunks).tobytes())
    (ROOT / "art/audio/audition-cues.json").write_text(json.dumps(timings, indent=2) + "\n", encoding="utf-8")


def audition_v08():
    roles = ["SpearRecall", "SpearCatch", "FanStart", "FanLoop", "FanEnd", "SpearPulse", "SpearStruck", "ThunderStrike"]
    chunks, timings, count = [], [], 0
    for name in roles:
        with wave.open(str(ROOT / "art/audio/source" / (name + ".wav")), "rb") as stream:
            pcm = np.frombuffer(stream.readframes(stream.getnframes()), dtype="<i2").copy()
        if name.endswith("Loop"):
            fade = np.minimum(1, np.arange(len(pcm)) / 5760) * np.minimum(1, np.arange(len(pcm))[::-1] / 5760)
            pcm = (pcm * fade).astype("<i2")
        timings.append({"name": name, "start_seconds": round(count / 48000, 3)})
        chunks += [pcm, np.zeros(24000, dtype="<i2")]
        count += len(pcm) + 24000
    with wave.open(str(ROOT / "art/audio/hollow-saint-audition-v08.wav"), "wb") as stream:
        stream.setparams((1, 2, 48000, 0, "NONE", "not compressed"))
        stream.writeframes(np.concatenate(chunks).tobytes())
    (ROOT / "art/audio/audition-cues-v08.json").write_text(json.dumps(timings, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    author()
    audition()
    audition_v08()
    subprocess.run([str(CONSOLE), "generate-soundbank", str(PROJECT / "HollowSaintAudio.wproj"),
                    "--platform", "Windows",
                    "--bank", "HollowSaint", "--save", "--no-source-control", "--header-file"], check=True)
    bank = PROJECT / "GeneratedSoundBanks/Windows/HollowSaint.bnk"
    data = bank.read_bytes()
    assert data[:4] == b"BKHD" and struct.unpack_from("<I", data, 8)[0] == 150
    print(f"BANK_VERIFIED {bank} bytes={len(data)} version=150")
