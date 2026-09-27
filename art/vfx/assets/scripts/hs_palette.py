"""Hollow Saint VFX palette. Linear RGB (Blender / Unity HDR colour pickers) plus sRGB for PNG pixels.

Cyan values: tools/blender/anim/vfx.py (read-only reference).
Copper: tools/blender/build_hybrid_v14.py 'V11 aged copper blocks' light tone (0.21, 0.072, 0.028) and
build_hybrid_v11.py 'V11 tabard copper trim' (0.40, 0.17, 0.055). COPPER_GLOW is the trim hue normalised to
max 1.0, used for emissive copper accents (tail sparks, meter ramp mid band).
"""

CORE = (0.85, 0.97, 1.0)      # white-hot cyan core, vfx.py strength 22
OUTER = (0.25, 0.8, 1.0)      # outer cyan, vfx.py strength 9
ARC = (0.55, 0.9, 1.0)        # arc cyan, vfx.py strength 16
COPPER_DARK = (0.055, 0.02, 0.009)
COPPER = (0.21, 0.072, 0.028)
COPPER_TRIM = (0.40, 0.17, 0.055)
COPPER_GLOW = (1.0, 0.425, 0.1375)
GRAPHITE = (0.012, 0.011, 0.012)

STRENGTH = {'core': 22.0, 'outer': 9.0, 'arc': 16.0}


def lin_to_srgb(c):
    out = []
    for v in c:
        v = max(0.0, min(1.0, v))
        out.append(v * 12.92 if v <= 0.0031308 else 1.055 * v ** (1 / 2.4) - 0.055)
    return tuple(out)


def srgb_to_lin(c):
    out = []
    for v in c:
        out.append(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4)
    return tuple(out)
