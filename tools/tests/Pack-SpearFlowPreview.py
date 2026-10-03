"""Assemble unchanged native Unity frame captures into motion-review GIFs."""
from pathlib import Path
import argparse
from PIL import Image

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2] / "artifacts" / "spear-flow01")
parser.add_argument("--names", nargs="+", default=["standing", "running"])
args = parser.parse_args()
root = args.root
for name in args.names:
    paths = sorted((root / name).glob("frame*.png"))
    if len(paths) != 120:
        raise RuntimeError(f"Expected 120 native frames for {name}, got {len(paths)}")
    frames = [Image.open(path).convert("RGB") for path in paths]
    output = root / f"{name}.gif"
    frames[0].save(output, save_all=True, append_images=frames[1:],
                   duration=[30, 30, 40] * 40, loop=0, disposal=2)
    print(output)
