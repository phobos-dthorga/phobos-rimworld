"""Draw D'thorga Medical Equipment's layout drawings and placeholder textures with Pillow, in the game's look: a thick near-black
outline, pale panels and soft flat shading. No image generator and no game art is involved.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/phobos-medical/draw_placeholders.py [name ...]

Name drawings to draw only those, so the others' files are left alone. Two kinds:

- References (OxygenConcentrator, HyperbaricChamber) guide the image generator. They go to assets/phobos-medical/references/, which
  is not tracked on main: the art-references branch keeps them (owner, 7 Nov 2026), and this script draws them again.
- Placeholders (ElectrolyserPlaceholder, HyperbaricChamberPlaceholder) are game textures for buildings whose generated art is still
  outstanding. They go into the mod, each with a record saying so, and are replaced when the owner picks generated art. A building
  with picked art never has a placeholder here, so this can't overwrite it.
"""
import datetime as dt
import hashlib
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
REFERENCES = ROOT / 'assets/phobos-medical/references'
TEXTURES = ROOT / 'mods/PhobosMedical/Common/Textures/Phobos/Medical/Things'
WORK = 1024  # Drawn large and scaled down, which smooths the edges.
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
OXYGEN = (201, 163, 79, 255)  # The oxygen network's overlay colour, so the outlet reads as oxygen.
HYDROGEN = (196, 86, 74, 255)
WATER = (86, 158, 204, 255)


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


def electrolyser(draw):
    """A 2x2 electrolyser from above: a stack of grey cell plates between two end plates, an oxygen outlet and a hydrogen vent at the
    back, a water inlet at the side and a small control panel at the front."""
    box(draw, (110, 150, 914, 830), PANEL_DARK, 40)  # Base frame.
    box(draw, (170, 200, 854, 700), PANEL, 30)  # The cell stack.
    for x in range(230, 820, 56):
        draw.line((x, 230, x, 670), fill=PANEL_DEEP, width=22)
    for x in (150, 834):  # End plates.
        box(draw, (x - 40, 180, x + 40, 720), PANEL_DEEP, 20)
    box(draw, (300, 40, 420, 210), OXYGEN, 30)  # Oxygen outlet to the pipe.
    box(draw, (604, 40, 724, 210), HYDROGEN, 30)  # Hydrogen vent.
    box(draw, (30, 380, 150, 500), WATER, 30)  # Water inlet.
    box(draw, (380, 740, 644, 950), PANEL, 30)  # Control panel.
    box(draw, (420, 780, 604, 870), SCREEN, 20)
    draw.ellipse((470, 884, 520, 934), fill=GREEN, outline=OUTLINE, width=12)
    draw.ellipse((540, 884, 590, 934), fill=HYDROGEN, outline=OUTLINE, width=12)


def hyperbaric_chamber_narrow(draw):
    """The chamber as built, 1 tile by 3, head to the north, in the middle third of a square canvas (the game draws beds on square
    textures): the white vessel, the clear top over the padded tray, the round door and wheel at the foot, a gauge on the door end."""
    box(draw, (540, 50, 996, 1486), VESSEL, 220)
    box(draw, (600, 150, 936, 1250), ACRYLIC, 160)
    box(draw, (650, 210, 886, 1190), PAD, 110)
    draw.ellipse((710, 240, 826, 356), fill=PANEL, outline=OUTLINE, width=28)  # Pillow at the head.
    box(draw, (590, 1290, 946, 1470), PANEL_DARK, 80)  # The door at the foot.
    draw.ellipse((708, 1320, 828, 1440), fill=BRASS, outline=OUTLINE, width=28)
    draw.line((768, 1332, 768, 1428), fill=OUTLINE, width=16)
    draw.line((720, 1380, 816, 1380), fill=OUTLINE, width=16)
    draw.ellipse((870, 1330, 930, 1390), fill=SCREEN, outline=OUTLINE, width=12)  # Pressure gauge.


def placeholder(painter, name, what, canvas, final, multi, outstanding):
    """A game texture standing in for outstanding generated art, with its record. multi writes the south view and the east and north
    views turned from it (anticlockwise 90 and 180 degrees, as the burn unit bed's), for a bed drawn from directly above."""
    image = Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))
    painter(ImageDraw.Draw(image))
    image = image.resize((final, final), Image.LANCZOS)
    TEXTURES.mkdir(parents=True, exist_ok=True)
    views = {'_south': image, '_east': image.rotate(90), '_north': image.rotate(180)} if multi else {'': image}
    lines = [f'# {name}', '', f'**A placeholder**, drawn with Pillow by `assets/phobos-medical/draw_placeholders.py` on '
             f'{dt.date.today():%d %B %Y}: {what}. No image generator, game art or other author\'s art was used. {final} x {final} px.', '',
             f'**Outstanding:** {outstanding} (listed in `docs/development/art-backlog.md`).', '']
    for suffix, view in views.items():
        path = TEXTURES / f'{name}{suffix}.png'
        view.save(path, optimize=True)
        lines.append(f'- `{path.name}`: SHA-256 `{hashlib.sha256(path.read_bytes()).hexdigest()}`')
        print('wrote', path.name)
    (TEXTURES / f'{name}.record.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')


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
    # Reference only since 7 Nov 2026: the owner picked generated art for the game texture, which this must never overwrite.
    'OxygenConcentrator': lambda: reference_only(
        concentrator, 'OxygenConcentrator', 'a bedside oxygen concentrator', (WORK, WORK),
        'The owner liked this design, so the generator is given it as a reference.'),
    'HyperbaricChamber': lambda: reference_only(
        hyperbaric_chamber, 'HyperbaricChamber', 'a hyperbaric oxygen chamber seen from above, head to the north', (WORK, WORK * 3 // 2),
        'The owner disliked the angles of the generated chambers (5 Nov 2026), so this flat layout guides the redraw.'),
    'ElectrolyserPlaceholder': lambda: placeholder(
        electrolyser, 'Electrolyser', 'an oxygen electrolyser, 2 by 2, from above', WORK, 256, False,
        'generated art; the owner found none of the first candidates suitable (7 Nov 2026)'),
    'HyperbaricChamberPlaceholder': lambda: placeholder(
        hyperbaric_chamber_narrow, 'HyperbaricChamber', 'a one-patient hyperbaric chamber, 1 by 3, from above, head to the north', 1536,
        384, True, "a top-down redraw at 1 by 3, with the owner's pick hyperbaric-chamber-102 as the design reference"),
}

if __name__ == '__main__':
    for chosen in sys.argv[1:] or DRAWINGS:
        DRAWINGS[chosen]()
