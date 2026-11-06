"""Draw D'thorga Medical Equipment's placeholder building textures with Pillow, in the game's look: a thick near-black outline, pale
panels and soft flat shading. No image generator and no game art is involved. They stand in until the owner picks generated art.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/phobos-medical/draw_placeholders.py [name ...]

Name drawings (OxygenConcentrator, HyperbaricChamber) to draw only those, so the others' files are left alone.

Writes mods/PhobosMedicalEquipment/Common/Textures/Phobos/Medical/Things/*.png and a record beside each, and the large drawing of
any the generator uses as a reference to assets/phobos-medical/references/.
"""
import datetime as dt
import hashlib
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'mods/PhobosMedicalEquipment/Common/Textures/Phobos/Medical/Things'
REFERENCES = ROOT / 'assets/phobos-medical/references'
WORK = 1024  # Drawn large and scaled down, which smooths the edges.
FINAL = 64
LINE = 44
OUTLINE = (24, 24, 26, 255)
PANEL = (206, 208, 206, 255)
PANEL_DARK = (156, 160, 160, 255)
PANEL_DEEP = (112, 116, 118, 255)
SCREEN = (54, 150, 190, 255)
VESSEL = (226, 228, 224, 255)
ACRYLIC = (170, 208, 222, 255)
PAD = (120, 150, 176, 255)
BRASS = (196, 164, 92, 255)
TUBE = (178, 214, 226, 255)
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


def write_record(path, name, what):
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    path.with_suffix('.record.md').write_text(
        f'# {name}\n\nA placeholder of {what}, drawn with Pillow by `assets/phobos-medical/draw_placeholders.py` on {dt.date.today():%d %B %Y}. '
        f'No image generator, game art or other author\'s art was used. {FINAL} x {FINAL} px. SHA-256 `{digest}`.\n\n'
        'It stands in until the owner picks generated art from `assets/phobos-medical/requests/first-batch.json`.\n', encoding='utf-8')


def write_reference(image, name, what):
    path = REFERENCES / f'{name}-large.png'
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    path.with_suffix('.record.md').write_text(
        f'# {name}, large\n\nThe {WORK} x {WORK} px drawing of {what}, before scaling down, from `assets/phobos-medical/draw_placeholders.py`, '
        f'{dt.date.today():%d %B %Y}. No image generator, game art or other author\'s art was used. SHA-256 `{digest}`.\n\n'
        'The owner liked this design, so the generator is given it as a reference. It does not ship in a mod.\n', encoding='utf-8')


def render(painter, name, what, reference=False):
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    painter(ImageDraw.Draw(image))
    path = OUT / f'{name}.png'
    path.parent.mkdir(parents=True, exist_ok=True)
    image.resize((FINAL, FINAL), Image.LANCZOS).save(path, optimize=True)
    write_record(path, name, what)
    if reference:
        write_reference(image, name, what)
    print('wrote', path.name)


def hyperbaric_chamber(draw):
    """A 2x3 chamber seen from above, head to the north: a long white vessel with rounded ends, a clear acrylic top showing the
    patient's padded tray, a heavy round door with a hinge and wheel at the south end, two dark cradle bands, and a control pedestal
    with a pressure gauge beside it. Drawn on a 1024 x 1536 canvas, the building's 2:3 shape, so the generator keeps the layout."""
    for y in (380, 1060):  # Cradle bands under the vessel.
        box(draw, (150, y, 874, y + 110), PANEL_DEEP, 30)
    box(draw, (220, 110, 804, 1300), VESSEL, 290)
    box(draw, (330, 230, 694, 1130), ACRYLIC, 180)
    box(draw, (400, 300, 624, 1060), PAD, 110)
    draw.ellipse((452, 330, 572, 450), fill=PANEL, outline=OUTLINE, width=30)  # Pillow at the head.
    box(draw, (300, 1230, 724, 1440), PANEL_DARK, 100)  # The door, shut across the south end.
    draw.ellipse((452, 1270, 572, 1390), fill=BRASS, outline=OUTLINE, width=30)
    draw.line((512, 1282, 512, 1378), fill=OUTLINE, width=18)
    draw.line((464, 1330, 560, 1330), fill=OUTLINE, width=18)
    box(draw, (260, 1290, 320, 1380), PANEL_DEEP, 14)  # Hinge.
    box(draw, (820, 700, 1010, 1060), PANEL, 34)  # Control pedestal.
    box(draw, (850, 736, 980, 866), SCREEN, 65)
    draw.line((915, 801, 955, 761), fill=OUTLINE, width=18)
    box(draw, (850, 910, 980, 1020), GREEN, 20)


def reference_only(painter, name, what, size, why):
    """A drawing that only guides the generator, for a building with no Def or texture yet."""
    image = Image.new('RGBA', size, (0, 0, 0, 0))
    painter(ImageDraw.Draw(image))
    path = REFERENCES / f'{name}-large.png'
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    path.with_suffix('.record.md').write_text(
        f'# {name}, large\n\nA {size[0]} x {size[1]} px drawing of {what}, from `assets/phobos-medical/draw_placeholders.py`, '
        f'{dt.date.today():%d %B %Y}. No image generator, game art or other author\'s art was used. SHA-256 `{digest}`.\n\n'
        f'{why} It does not ship in a mod.\n', encoding='utf-8')
    print('wrote', path.name)


DRAWINGS = {
    'OxygenConcentrator': lambda: render(concentrator, 'OxygenConcentrator', 'a bedside oxygen concentrator', reference=True),
    'HyperbaricChamber': lambda: reference_only(
        hyperbaric_chamber, 'HyperbaricChamber', 'a hyperbaric oxygen chamber seen from above, head to the north', (WORK, WORK * 3 // 2),
        'The owner disliked the angles of the generated chambers (5 Nov 2026), so this flat layout guides the redraw.'),
}

if __name__ == '__main__':
    for chosen in sys.argv[1:] or DRAWINGS:
        DRAWINGS[chosen]()
