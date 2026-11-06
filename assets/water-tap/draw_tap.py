"""Draw the bottle tap's layout drawing with Pillow, the reference the image generator redraws. No image generator, no source images.

    python assets/water-tap/draw_tap.py

Needs Pillow. Run it with any Python that has it (the repository's Python doesn't need it for anything else). Writes
assets/water-tap/references/BottleTap-large.png, which is not tracked on main; the art-references branch keeps a copy. Since 7 Nov
2026 the game texture and the Workshop preview are generated art the owner picked, so this no longer writes them and can't overwrite them.
"""
import datetime as dt
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
REFERENCE = ROOT / 'assets/water-tap/references/BottleTap-large.png'

# Drawn large and scaled down, which smooths the edges.
WORK = 1024
FINAL = 128
OUTLINE = (28, 26, 24, 255)
STEEL = (146, 150, 152, 255)
STEEL_DARK = (98, 102, 106, 255)
STEEL_LIGHT = (188, 192, 194, 255)
SHADOW = (66, 70, 74, 255)
WATER = (86, 158, 204, 255)
WATER_LIGHT = (150, 204, 232, 255)
BRASS = (196, 158, 84, 255)
BRASS_DARK = (138, 106, 50, 255)
HANDLE = (168, 54, 44, 255)


def box(draw, x0, y0, x1, y1, fill, radius=40, outline=OUTLINE, width=26):
    draw.rounded_rectangle((x0, y0, x1, y1), radius=radius, fill=fill, outline=outline, width=width)


def draw_tap(final=FINAL):
    """The tap seen from above in RimWorld's style: a steel basin, water in its base, a spout and handle at the back."""
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    # The basin: an outer wall, an inner shadow where the wall falls away, then the water the bottles stand in.
    box(draw, 70, 170, 954, 954, STEEL, radius=90)
    draw.rounded_rectangle((96, 196, 928, 300), radius=60, fill=STEEL_LIGHT)
    box(draw, 170, 290, 854, 870, SHADOW, radius=56, width=20)
    draw.rounded_rectangle((200, 340, 824, 840), radius=44, fill=WATER)
    draw.arc((270, 410, 590, 650), start=190, end=290, fill=WATER_LIGHT, width=22)
    draw.arc((470, 600, 750, 790), start=10, end=110, fill=WATER_LIGHT, width=18)
    # The feed pipe and spout, at the back of the basin so a stack of bottles in the middle doesn't hide them.
    box(draw, 402, 60, 622, 250, BRASS, radius=30)
    draw.rectangle((430, 90, 470, 225), fill=(226, 192, 124, 255))
    box(draw, 452, 190, 572, 430, BRASS, radius=24)
    draw.rectangle((478, 215, 502, 400), fill=(226, 192, 124, 255))
    box(draw, 438, 402, 586, 462, BRASS_DARK, radius=16, width=20)
    # The handle: a red wheel on the feed pipe.
    draw.ellipse((412, 40, 612, 240), fill=HANDLE, outline=OUTLINE, width=26)
    draw.ellipse((482, 110, 542, 170), fill=BRASS, outline=OUTLINE, width=14)
    return image if final is None else image.resize((final, final), Image.LANCZOS)


def write_reference():
    REFERENCE.parent.mkdir(parents=True, exist_ok=True)
    draw_tap(final=None).save(REFERENCE, optimize=True)
    digest = hashlib.sha256(REFERENCE.read_bytes()).hexdigest()
    REFERENCE.with_suffix('.record.md').write_text(
        f'# BottleTap, large\n\nThe {WORK} x {WORK} px drawing of the bottle tap before scaling down, from '
        f'`assets/water-tap/draw_tap.py`, {dt.date.today():%d %B %Y}. No image generator, game art or other author\'s art '
        f'was used. SHA-256 `{digest}`.\n\n'
        'The image generator is given it as a reference, to keep the layout: spout and handle at the back, so a stack of bottles in '
        'the middle does not hide them. It does not ship in a mod.\n', encoding='utf-8')
    print(f'wrote {REFERENCE}')


if __name__ == '__main__':
    write_reference()
