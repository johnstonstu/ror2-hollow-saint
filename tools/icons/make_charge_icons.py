"""Extend the existing code-drawn skill icon system for the local charge prototype."""
import math
from pathlib import Path
from PIL import ImageDraw
import make_icons as base


def cloud():
    canvas, mask = base.skill_canvas()
    d = ImageDraw.Draw(canvas)
    s = base.CANVAS
    for x, y, r in [(.28, .34, .13), (.45, .26, .17), (.65, .32, .17), (.76, .39, .10)]:
        d.ellipse(((x-r)*s, (y-r)*s, (x+r)*s, (y+r)*s), fill=(76, 115, 139, 255))
    d.rounded_rectangle((.18*s, .33*s, .83*s, .49*s), radius=.08*s, fill=(76, 115, 139, 255))
    for i, x in enumerate([.30, .50, .70]):
        pts = base.jagged_path((x*s, .44*s), ((x-.06)*s, (.78+.05*(i % 2))*s), 31+i, segments=4)
        base.composite_glow_core(canvas, lambda draw, color, width, pts=pts: base.draw_polyline(draw, pts, color, width))
    base.finalize_skill(canvas, mask, "skill_thundercloud.png")


def orb():
    canvas, mask = base.skill_canvas()
    s = base.CANVAS
    d = ImageDraw.Draw(canvas)
    d.ellipse((.28*s, .26*s, .72*s, .70*s), fill=(17, 96, 127, 255), outline=base.ARC_CYAN, width=6*base.SS)
    for i in range(3):
        a = i*math.pi*2/3
        start = ((.5+.18*math.cos(a))*s, (.48+.18*math.sin(a))*s)
        end = ((.5+.18*math.cos(a+2.0))*s, (.48+.18*math.sin(a+2.0))*s)
        pts = base.jagged_path(start, end, 61+i, segments=4)
        base.composite_glow_core(canvas, lambda draw, color, width, pts=pts: base.draw_polyline(draw, pts, color, width))
    for pts in [[(.18,.77),(.27,.84),(.38,.81),(.30,.73)], [(.82,.77),(.73,.84),(.62,.81),(.70,.73)]]:
        d.line([(x*s,y*s) for x,y in pts], fill=base.IVORY, width=7*base.SS)
    base.finalize_skill(canvas, mask, "skill_hollowed_orb.png")


if __name__ == "__main__":
    base.OUT_DIR = str(Path(__file__).resolve().parents[2] / "HollowSaintMod" / "Icons")
    for name in ["skill_thundercloud.png", "skill_hollowed_orb.png"]:
        if (Path(base.OUT_DIR) / name).exists():
            raise SystemExit(f"Refusing to overwrite existing icon without review: {name}")
    cloud()
    orb()
