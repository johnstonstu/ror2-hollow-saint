#!/usr/bin/env python3
from PIL import Image, ImageDraw, ImageFont

OUT_DIR = "/mnt/user-data/outputs/icons"
NAMES = [
    "skill_arc_bolt.png",
    "skill_conduit_spear.png",
    "skill_arc_step.png",
    "skill_open_circuit.png",
    "passive_discharge.png",
    "buff_conductor_mark.png",
    "buff_open_circuit.png",
    "buff_discharge_charge.png",
    "portrait.png",
]

CHECKER = (110, 110, 110)
CHECKER2 = (130, 130, 130)
BG = (128, 128, 128)


def checker_bg(size):
    img = Image.new("RGB", (size, size), CHECKER)
    d = ImageDraw.Draw(img)
    step = max(4, size // 16)
    for y in range(0, size, step):
        for x in range(0, size, step):
            if (x // step + y // step) % 2 == 0:
                d.rectangle([x, y, x + step, y + step], fill=CHECKER2)
    return img


def paste_icon(sheet, icon, x, y, size, label=None, font=None, draw=None):
    disp_size = size
    im = icon.resize((disp_size, disp_size), Image.LANCZOS)
    bg = checker_bg(disp_size).convert("RGBA")
    bg.alpha_composite(im)
    sheet.paste(bg, (x, y))
    if label and draw and font:
        draw.text((x, y + disp_size + 4), label, fill=(20, 20, 20), font=font)


def main():
    icons = {n: Image.open(f"{OUT_DIR}/{n}").convert("RGBA") for n in NAMES}

    cols = 3
    rows = 3
    cell_full = 256
    cell_64 = 64
    cell_32 = 32
    pad = 20
    label_h = 20
    row_h = cell_full + label_h * 3 + cell_64 + cell_32 + pad
    col_w = cell_full + pad

    sheet_w = cols * col_w + pad
    sheet_h = rows * row_h + pad + 60

    sheet = Image.new("RGB", (sheet_w, sheet_h), BG)
    draw = ImageDraw.Draw(sheet)

    try:
        font = ImageFont.truetype(
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 16)
        font_title = ImageFont.truetype(
            "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 24)
    except Exception:
        font = ImageFont.load_default()
        font_title = font

    draw.text((pad, 10), "Hollow Saint icon contact sheet", fill=(20, 20, 20),
               font=font_title)

    top = 60
    for i, name in enumerate(NAMES):
        col = i % cols
        row = i // cols
        x = pad + col * col_w
        y = top + pad + row * row_h

        icon = icons[name]

        # full size
        bg_full = checker_bg(cell_full).convert("RGBA")
        bg_full.alpha_composite(icon.resize((cell_full, cell_full), Image.LANCZOS))
        sheet.paste(bg_full, (x, y))
        draw.text((x, y + cell_full + 2), f"{name} (256px)", fill=(20, 20, 20), font=font)

        y2 = y + cell_full + label_h
        bg_64 = checker_bg(cell_64).convert("RGBA")
        bg_64.alpha_composite(icon.resize((cell_64, cell_64), Image.LANCZOS))
        sheet.paste(bg_64, (x, y2))
        draw.text((x + cell_64 + 8, y2 + cell_64 // 2 - 8), "64px", fill=(20, 20, 20),
                   font=font)

        bg_32 = checker_bg(cell_32).convert("RGBA")
        bg_32.alpha_composite(icon.resize((cell_32, cell_32), Image.LANCZOS))
        sheet.paste(bg_32, (x + cell_64 + 60, y2 + (cell_64 - cell_32) // 2))
        draw.text((x + cell_64 + 60 + cell_32 + 8, y2 + cell_64 // 2 - 8), "32px",
                   fill=(20, 20, 20), font=font)

    sheet.save(f"{OUT_DIR}/contact_sheet.png")
    print("saved contact_sheet.png", sheet.size)


if __name__ == "__main__":
    main()
