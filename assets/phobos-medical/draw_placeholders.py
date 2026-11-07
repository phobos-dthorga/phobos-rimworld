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
SAFETY_GREEN = (64, 156, 92, 255)
SAFETY_YELLOW = (226, 196, 64, 255)
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


def ventilator(draw):
    """A bedside ventilator from above: a grey cabinet on castors, a screen with a breathing wave, a clear bellows column and a
    corrugated tube curling off to the bed."""
    for x in (258, 666):
        box(draw, (x, 804, x + 100, 924), PANEL_DEEP, 24)
    box(draw, (190, 120, 834, 852), PANEL)
    box(draw, (250, 180, 774, 470), SCREEN, 40)
    points = [(290, 330), (380, 330), (420, 240), (470, 410), (520, 330), (600, 330), (640, 260), (690, 400), (734, 330)]
    draw.line(points, fill=VESSEL, width=22, joint='curve')
    box(draw, (260, 520, 460, 800), ACRYLIC, 60)  # Bellows column.
    for y in range(570, 780, 50):
        draw.line((280, y, 440, y), fill=PANEL_DARK, width=16)
    for i, x in enumerate((540, 620, 700)):
        draw.ellipse((x, 560, x + 60, 620), fill=(GREEN, BRASS, HYDROGEN)[i], outline=OUTLINE, width=12)
    box(draw, (520, 680, 780, 780), PANEL_DARK, 20)
    draw.arc((600, 600, 960, 960), 280, 60, fill=OUTLINE, width=70)
    draw.arc((600, 600, 960, 960), 280, 60, fill=TUBE, width=40)


def intubation_kit(draw):
    """An intubation kit as an item: a laryngoscope (a handle with a curved blade and a lamp at its tip) beside a clear breathing tube
    with its blue cuff and connector."""
    blade = [(255, 470), (290, 330), (390, 240), (540, 215)]
    draw.line(blade, fill=OUTLINE, width=100, joint='curve')
    draw.line(blade, fill=VESSEL, width=56, joint='curve')
    draw.ellipse((510, 175, 600, 265), fill=BRASS, outline=OUTLINE, width=18)  # Lamp at the tip.
    box(draw, (175, 430, 335, 900), PANEL_DARK, 50)  # Handle, over the blade's root.
    for y in (520, 600, 680, 760):
        draw.line((205, y, 305, y), fill=PANEL_DEEP, width=18)
    tube = [(470, 900), (640, 850), (780, 720), (850, 540), (850, 330)]
    draw.line(tube, fill=OUTLINE, width=96, joint='curve')
    draw.line(tube, fill=TUBE, width=54, joint='curve')
    draw.ellipse((780, 360, 920, 520), fill=PAD, outline=OUTLINE, width=20)  # Cuff.
    box(draw, (400, 850, 520, 950), GREEN, 20)  # Connector.


def safety_shower(draw):
    """A safety shower and eyewash from above: a green drench head on a yellow-banded stand, a pull handle, and a round eyewash bowl
    with two spray nozzles."""
    box(draw, (150, 150, 874, 874), PANEL_DARK, 60)  # Grated floor tray.
    for x in range(220, 820, 80):
        draw.line((x, 200, x, 824), fill=PANEL_DEEP, width=14)
    draw.ellipse((250, 180, 774, 704), fill=SAFETY_GREEN, outline=OUTLINE, width=LINE)  # Drench head.
    draw.ellipse((420, 350, 604, 534), fill=PANEL, outline=OUTLINE, width=28)
    for angle_x, angle_y in ((512, 260), (512, 624), (330, 442), (694, 442)):
        draw.ellipse((angle_x - 22, angle_y - 22, angle_x + 22, angle_y + 22), fill=WATER, outline=OUTLINE, width=10)
    draw.line((700, 640, 860, 860), fill=OUTLINE, width=50)  # Pull rod and handle.
    draw.line((700, 640, 860, 860), fill=SAFETY_YELLOW, width=26)
    draw.ellipse((820, 820, 920, 920), fill=SAFETY_YELLOW, outline=OUTLINE, width=16)
    draw.ellipse((170, 640, 450, 920), fill=VESSEL, outline=OUTLINE, width=LINE)  # Eyewash bowl.
    for x in (250, 330):
        draw.ellipse((x, 740, x + 60, 800), fill=SAFETY_GREEN, outline=OUTLINE, width=12)


def gas_alarm(draw):
    """A ceiling alarm from below: a white disc with a slotted grille, a test button and a red light."""
    draw.ellipse((140, 140, 884, 884), fill=VESSEL, outline=OUTLINE, width=LINE)
    draw.ellipse((300, 300, 724, 724), fill=PANEL, outline=OUTLINE, width=30)
    for y in range(380, 660, 70):
        draw.line((370, y, 654, y), fill=PANEL_DEEP, width=24)
    draw.ellipse((640, 200, 740, 300), fill=HYDROGEN, outline=OUTLINE, width=18)  # Red light.
    box(draw, (440, 760, 584, 840), PANEL_DARK, 20)  # Test button.


def oxygen_trolley(draw):
    """An oxygen cylinder trolley from above: a dark wheeled frame with a push handle, two cylinders seen end-on with their valves, and a
    regulator with a coiled mask tube."""
    for x, y in ((210, 230), (734, 230), (210, 770), (734, 770)):  # Wheels.
        draw.ellipse((x - 50, y - 50, x + 50, y + 50), fill=PANEL_DEEP, outline=OUTLINE, width=18)
    box(draw, (200, 200, 824, 824), PANEL_DARK, 60)  # Frame.
    draw.line((200, 900, 824, 900), fill=OUTLINE, width=60)  # Push handle.
    draw.line((200, 900, 824, 900), fill=PANEL, width=30)
    for cx in (370, 654):
        draw.ellipse((cx - 130, 300, cx + 130, 560), fill=OXYGEN, outline=OUTLINE, width=LINE)
        draw.ellipse((cx - 56, 374, cx + 56, 486), fill=PANEL, outline=OUTLINE, width=22)  # Valve.
    box(draw, (420, 610, 604, 760), PANEL, 30)  # Regulator.
    draw.ellipse((470, 640, 554, 724), fill=SCREEN, outline=OUTLINE, width=14)
    draw.arc((560, 600, 900, 940), 200, 340, fill=OUTLINE, width=50)
    draw.arc((560, 600, 900, 940), 200, 340, fill=TUBE, width=26)


def nebuliser(draw):
    """A nebuliser from above: a small compressor box with a dial, a tube to a mask with its cup of drug and a puff of mist."""
    box(draw, (180, 300, 640, 800), PANEL, 50)
    for y in range(380, 720, 60):
        draw.line((230, y, 420, y), fill=PANEL_DARK, width=20)
    draw.ellipse((470, 380, 590, 500), fill=SCREEN, outline=OUTLINE, width=16)
    box(draw, (470, 560, 590, 640), GREEN, 16)
    draw.arc((500, 120, 900, 520), 90, 200, fill=OUTLINE, width=50)
    draw.arc((500, 120, 900, 520), 90, 200, fill=TUBE, width=26)
    draw.ellipse((640, 160, 860, 380), fill=ACRYLIC, outline=OUTLINE, width=LINE)  # Mask.
    box(draw, (700, 380, 800, 480), BRASS, 20)  # Drug cup.
    for x, y, r in ((720, 110, 50), (800, 80, 40), (870, 130, 34)):
        draw.ellipse((x - r, y - r, x + r, y + r), fill=VESSEL, outline=OUTLINE, width=10)  # Mist.


def bronchodilator_vial(draw):
    """A strip of three small twist-top plastic vials, each showing its drug."""
    for x in (170, 410, 650):
        draw.rounded_rectangle((x, 300, x + 210, 840), radius=60, fill=ACRYLIC, outline=OUTLINE, width=26)
        draw.rounded_rectangle((x + 50, 170, x + 160, 320), radius=24, fill=VESSEL, outline=OUTLINE, width=22)
        draw.rounded_rectangle((x + 34, 500, x + 176, 810), radius=40, fill=BRASS, outline=OUTLINE, width=16)


def anaesthesia_machine(draw):
    """An anaesthesia machine from above: a trolley with a monitor, two coloured vaporisers, a row of flow meters and a breathing bag."""
    for x in (258, 666):
        box(draw, (x, 804, x + 100, 924), PANEL_DEEP, 24)
    box(draw, (170, 120, 854, 852), PANEL)
    box(draw, (230, 170, 794, 400), SCREEN, 30)
    draw.line([(270, 300), (360, 300), (390, 230), (430, 360), (470, 300), (760, 300)], fill=VESSEL, width=18, joint='curve')
    for x, colour in ((250, HYDROGEN), (400, SAFETY_YELLOW)):
        box(draw, (x, 450, x + 120, 680), colour, 30)  # Vaporisers.
    for x in range(570, 790, 60):
        box(draw, (x, 450, x + 40, 680), ACRYLIC, 14)  # Flow meters.
    draw.ellipse((700, 700, 900, 900), fill=PAD, outline=OUTLINE, width=LINE)  # Breathing bag.


def decompression_kit(draw):
    """A needle decompression kit: a long needle with a cannula hub, beside its sheath."""
    box(draw, (160, 640, 860, 760), ACRYLIC, 60)  # Sheath.
    draw.line((220, 420, 760, 420), fill=OUTLINE, width=70)
    draw.line((220, 420, 760, 420), fill=VESSEL, width=36)  # Needle.
    box(draw, (700, 340, 880, 500), SAFETY_GREEN, 30)  # Hub.
    draw.polygon([(160, 420), (230, 385), (230, 455)], fill=OUTLINE)


def steriliser(draw):
    """A steriliser from above: a square cabinet with a round pressure door, a locking wheel, a gauge and a control strip."""
    box(draw, (130, 130, 894, 894), PANEL)
    draw.ellipse((220, 220, 720, 720), fill=VESSEL, outline=OUTLINE, width=LINE)
    draw.ellipse((360, 360, 580, 580), fill=PANEL_DARK, outline=OUTLINE, width=28)
    draw.line((470, 380, 470, 560), fill=OUTLINE, width=24)
    draw.line((380, 470, 560, 470), fill=OUTLINE, width=24)
    draw.ellipse((740, 220, 860, 340), fill=SCREEN, outline=OUTLINE, width=16)  # Gauge.
    box(draw, (740, 400, 860, 820), PANEL_DARK, 20)
    for y in (460, 560, 660, 760):
        draw.ellipse((775, y - 25, 825, y + 25), fill=GREEN, outline=OUTLINE, width=10)


def fluids_kit(draw):
    """A fluids kit: a clear bag of fluid with its label and a coiled giving set ending in a needle."""
    draw.rounded_rectangle((250, 120, 690, 700), radius=90, fill=ACRYLIC, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((330, 260, 610, 460), radius=30, fill=VESSEL, outline=OUTLINE, width=20)
    draw.line((380, 360, 560, 360), fill=WATER, width=22)
    box(draw, (420, 680, 520, 780), PANEL_DARK, 20)
    draw.arc((300, 700, 760, 960), 180, 360, fill=OUTLINE, width=44)
    draw.arc((300, 700, 760, 960), 180, 360, fill=TUBE, width=22)
    draw.line((760, 830, 880, 780), fill=OUTLINE, width=34)
    draw.line((760, 830, 880, 780), fill=VESSEL, width=14)


def depth_reference(painter, name, what, size):
    """A depth map for the depth-control add-on, drawn from a stand-in's own layout: black is far (the floor), white is near (the
    tallest part, seen from directly above), softened so the model reads shapes rather than edges. Goes beside the references."""
    from PIL import ImageFilter
    image = Image.new('L', size, 0)
    painter(ImageDraw.Draw(image))
    image = image.filter(ImageFilter.GaussianBlur(10)).convert('RGB')
    path = REFERENCES / f'{name}-depth.png'
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    path.with_suffix('.record.md').write_text(
        f'# {name}, depth\n\nA {size[0]} x {size[1]} px depth map of {what}, from `assets/phobos-medical/draw_placeholders.py`, '
        f'{dt.date.today():%d %B %Y}: near is white, far is black, drawn from the stand-in\'s layout. No image generator, game art or '
        f'other author\'s art was used. SHA-256 `{digest}`. It guides the depth-control trial and does not ship in a mod.\n', encoding='utf-8')
    print('wrote', path.name)


def electrolyser_depth(draw):
    """The electrolyser's heights, from its layout: base frame low, cell stack higher, end plates and gas outlets highest."""
    draw.rounded_rectangle((110, 150, 914, 830), radius=40, fill=70)
    draw.rounded_rectangle((170, 200, 854, 700), radius=30, fill=170)
    for x in (150, 834):
        draw.rounded_rectangle((x - 40, 180, x + 40, 720), radius=20, fill=205)
    draw.rounded_rectangle((300, 40, 420, 210), radius=30, fill=235)
    draw.rounded_rectangle((604, 40, 724, 210), radius=30, fill=235)
    draw.rounded_rectangle((30, 380, 150, 500), radius=30, fill=120)
    draw.rounded_rectangle((380, 740, 644, 950), radius=30, fill=140)


def hyperbaric_chamber_narrow_depth(draw):
    """The chamber's heights, 1 by 3 on a square canvas: a lying cylinder (brightest along its top), the door at the foot, the wheel
    standing proud."""
    left, right = 540, 996
    for x in range(left, right):
        share = (x - left) / (right - left)
        height = int(120 + 110 * (1 - (2 * share - 1) ** 2))
        draw.line((x, 70, x, 1466), fill=height)
    draw.rounded_rectangle((590, 1290, 946, 1470), radius=80, fill=205)
    draw.ellipse((708, 1320, 828, 1440), fill=245)
    draw.ellipse((870, 1330, 930, 1390), fill=215)


NIGHT_REFERENCES = (
    ('ventilator', 'Ventilator', 'a bedside ventilator'),
    ('intubation_kit', 'IntubationKit', 'an intubation kit'),
    ('safety_shower', 'SafetyShower', 'a safety shower and eyewash'),
    ('gas_alarm', 'GasAlarm', 'a ceiling carbon monoxide and smoke alarm'),
    ('oxygen_trolley', 'OxygenTrolley', 'an oxygen cylinder trolley'),
    ('nebuliser', 'Nebuliser', 'a bedside nebuliser'),
    ('bronchodilator_vial', 'BronchodilatorVial', 'a strip of bronchodilator vials'),
    ('anaesthesia_machine', 'AnaesthesiaMachine', 'an anaesthesia machine'),
    ('decompression_kit', 'DecompressionKit', 'a needle decompression kit'),
    ('steriliser', 'Steriliser', 'a steriliser'),
    ('fluids_kit', 'FluidsKit', 'a fluids kit'),
    ('electrolyser', 'Electrolyser', 'an oxygen electrolyser, 2 by 2'),
)


def night_references():
    """Every large layout reference and depth map the night-3 requests use (7 Nov 2026)."""
    for painter_name, name, what in NIGHT_REFERENCES:
        reference_only(globals()[painter_name], name, f'{what}, from above', (WORK, WORK), 'The layout reference for its generated art.')
    reference_only(hyperbaric_chamber_narrow, 'HyperbaricChamberNarrow', 'the hyperbaric chamber as built, 1 by 3 on a square canvas', (1536, 1536),
                   'The layout reference for its top-down redraw.')
    depth_reference(electrolyser_depth, 'Electrolyser', 'the oxygen electrolyser', (WORK, WORK))
    depth_reference(hyperbaric_chamber_narrow_depth, 'HyperbaricChamberNarrow', 'the hyperbaric chamber as built, 1 by 3', (1536, 1536))


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
    'VentilatorPlaceholder': lambda: placeholder(
        ventilator, 'Ventilator', 'a bedside ventilator, 1 by 1, from above', WORK, 256, False, 'generated art'),
    'IntubationKitPlaceholder': lambda: placeholder(
        intubation_kit, 'IntubationKit', 'an intubation kit (laryngoscope and breathing tube) as an item', WORK, 128, False, 'generated art'),
    'SafetyShowerPlaceholder': lambda: placeholder(
        safety_shower, 'SafetyShower', 'a safety shower and eyewash, 1 by 1, from above', WORK, 256, False, 'generated art'),
    'GasAlarmPlaceholder': lambda: placeholder(
        gas_alarm, 'GasAlarm', 'a ceiling carbon monoxide and smoke alarm, seen from below', WORK, 128, False, 'generated art'),
    'OxygenTrolleyPlaceholder': lambda: placeholder(
        oxygen_trolley, 'OxygenTrolley', 'an oxygen cylinder trolley, 1 by 1, from above', WORK, 256, False, 'generated art'),
    'NebuliserPlaceholder': lambda: placeholder(
        nebuliser, 'Nebuliser', 'a bedside nebuliser, 1 by 1, from above', WORK, 256, False, 'generated art'),
    'BronchodilatorVialPlaceholder': lambda: placeholder(
        bronchodilator_vial, 'BronchodilatorVial', 'a strip of bronchodilator vials as an item', WORK, 128, False, 'generated art'),
    'AnaesthesiaMachinePlaceholder': lambda: placeholder(
        anaesthesia_machine, 'AnaesthesiaMachine', 'an anaesthesia machine, 1 by 1, from above', WORK, 256, False, 'generated art'),
    'DecompressionKitPlaceholder': lambda: placeholder(
        decompression_kit, 'DecompressionKit', 'a needle decompression kit as an item', WORK, 128, False, 'generated art'),
    'SteriliserPlaceholder': lambda: placeholder(
        steriliser, 'Steriliser', 'a steriliser (autoclave), 1 by 1, from above', WORK, 256, False, 'generated art'),
    'NightReferences': night_references,
    'FluidsKitPlaceholder': lambda: placeholder(
        fluids_kit, 'FluidsKit', 'a fluids kit (a bag of fluid and a giving set) as an item', WORK, 128, False, 'generated art'),
}

if __name__ == '__main__':
    for chosen in sys.argv[1:] or DRAWINGS:
        DRAWINGS[chosen]()
