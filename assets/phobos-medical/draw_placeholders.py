"""Draw D'thorga Medical Equipment's placeholder building textures with Pillow, in the game's look: a thick near-black outline, pale
panels and soft flat shading. No image generator and no game art is involved. They stand in until the owner picks generated art.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/phobos-medical/draw_placeholders.py

Writes mods/PhobosMedicalEquipment/Common/Textures/Phobos/Medical/Things/*.png and a record beside each.
"""
import datetime as dt
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'mods/PhobosMedicalEquipment/Common/Textures/Phobos/Medical/Things'
WORK = 1024  # Drawn large and scaled down, which smooths the edges.
FINAL = 64
LINE = 44
OUTLINE = (24, 24, 26, 255)
PANEL = (206, 208, 206, 255)
PANEL_DARK = (156, 160, 160, 255)
PANEL_DEEP = (112, 116, 118, 255)
STEEL = (136, 140, 144, 255)
SCREEN = (54, 150, 190, 255)
TUBE = (178, 214, 226, 255)
BRASS = (196, 158, 84, 255)
GREEN = (116, 176, 132, 255)


def box(draw, xy, fill, radius=46):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=OUTLINE, width=LINE)


def concentrator(draw):
    """A squat cabinet with two sieve columns behind a side window, a flow dial and castors."""
    for x in (258, 666):
        box(draw, (x, 804, x + 100, 924), PANEL_DEEP, 24)
    box(draw, (190, 120, 834, 852), PANEL)
    draw.rectangle((212, 660, 812, 830), fill=PANEL_DARK)
    box(draw, (254, 196, 556, 620), PANEL_DEEP, 30)
    for x in (292, 416):
        box(draw, (x, 234, x + 104, 584), GREEN, 40)
    box(draw, (612, 208, 772, 368), SCREEN, 80)
    draw.line((692, 288, 742, 238), fill=OUTLINE, width=26)
    box(draw, (612, 430, 772, 500), PANEL_DARK, 20)
    draw.arc((560, 610, 900, 900), 300, 80, fill=OUTLINE, width=56)
    draw.arc((560, 610, 900, 900), 300, 80, fill=TUBE, width=24)


def stand(draw):
    """A pole on a wheeled foot, holding a regulator, a flowmeter and a clear bottle, with a mask on a loop of tube."""
    box(draw, (300, 800, 724, 910), STEEL, 50)
    box(draw, (472, 90, 552, 840), STEEL, 30)
    box(draw, (332, 170, 692, 342), BRASS, 40)
    box(draw, (446, 206, 578, 306), SCREEN, 50)
    box(draw, (362, 400, 532, 680), TUBE, 50)
    draw.rectangle((396, 560, 498, 646), fill=SCREEN)
    draw.arc((540, 330, 860, 730), 270, 90, fill=OUTLINE, width=56)
    draw.arc((540, 330, 860, 730), 270, 90, fill=TUBE, width=24)
    box(draw, (610, 660, 800, 830), GREEN, 70)


def write_record(path, name, what):
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    path.with_suffix('.record.md').write_text(
        f'# {name}\n\nA placeholder of {what}, drawn with Pillow by `assets/phobos-medical/draw_placeholders.py` on {dt.date.today():%d %B %Y}. '
        f'No image generator, game art or other author\'s art was used. {FINAL} x {FINAL} px. SHA-256 `{digest}`.\n\n'
        'It stands in until the owner picks generated art from `assets/phobos-medical/requests/first-batch.json`.\n', encoding='utf-8')


def render(painter, name, what):
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    painter(ImageDraw.Draw(image))
    path = OUT / f'{name}.png'
    path.parent.mkdir(parents=True, exist_ok=True)
    image.resize((FINAL, FINAL), Image.LANCZOS).save(path, optimize=True)
    write_record(path, name, what)
    print('wrote', path.name)


if __name__ == '__main__':
    render(concentrator, 'OxygenConcentrator', 'a bedside oxygen concentrator')
    render(stand, 'BedsideOxygenStand', 'a bedside oxygen stand')
