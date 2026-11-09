"""Draw the upgrade and button icons for the survey truck and the drill rig, and the power point, with Pillow, in the game's look. No image generator, reference image or other author's art is
involved; each has a record beside it. The vehicles' own pictures are left for real art (owner, 9 Nov 2026).

    python assets/phobos-vehicles/draw_survey_and_rig.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
VEHICLES = ROOT / 'mods/PhobosVehicles/Textures/Phobos/Vehicles'
STATIONS = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems/Stations'
OUTLINE = (24, 24, 26, 255)
STEEL = (160, 164, 168, 255)
DARK = (92, 96, 100, 255)
OLIVE = (118, 122, 80, 255)
ORANGE = (214, 124, 44, 255)
BLUE = (96, 148, 196, 255)
YELLOW = (226, 196, 70, 255)
RED = (176, 66, 58, 255)
SOURCE = 'assets/phobos-vehicles/draw_survey_and_rig.py'


def box(draw, xy, fill, radius=10):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=OUTLINE, width=6)


def icon(path, drawing, what, size=128):
    image = Image.new('RGBA', (512, 512), (0, 0, 0, 0))
    drawing(ImageDraw.Draw(image))
    path.parent.mkdir(parents=True, exist_ok=True)
    image.resize((size, size), Image.LANCZOS).save(path)
    path.with_suffix('.record.md').write_text(
        f'# {path.stem}\n\n**Drawn in code** by `{SOURCE}` (Pillow): {what}. No image generator, no reference images and no game or other author\'s art.\n\n'
        f'- `{path.name}`: SHA-256 `{hashlib.sha256(path.read_bytes()).hexdigest()}`\n', encoding='utf-8', newline='\n')
    print(path.name)


def badge(colour, mark):
    def drawing(draw):
        box(draw, (70, 70, 442, 442), colour, 36)
        mark(draw)
    return drawing


def dots(draw):
    for x in (170, 256, 342):
        draw.ellipse((x - 30, 226, x + 30, 286), fill=YELLOW, outline=OUTLINE, width=12)


def hazard(draw):
    draw.polygon([(256, 130), (390, 370), (122, 370)], fill=YELLOW, outline=OUTLINE)


def dish_icon(draw):
    draw.ellipse((130, 130, 382, 382), fill=STEEL, outline=OUTLINE, width=16)
    draw.ellipse((226, 226, 286, 286), fill=DARK, outline=OUTLINE, width=12)


def bars(draw):
    for x in (160, 236, 312):
        box(draw, (x, 140, x + 44, 372), YELLOW, 8)


def plus(draw):
    draw.line([(170, 256), (342, 256)], fill=OUTLINE, width=32)
    draw.line([(256, 170), (256, 342)], fill=OUTLINE, width=32)


def lamp(draw):
    draw.ellipse((166, 166, 346, 346), fill=YELLOW, outline=OUTLINE, width=16)


def diamond_icon(draw):
    draw.polygon([(256, 120), (370, 256), (256, 392), (142, 256)], fill=BLUE, outline=OUTLINE)


def hammer_icon(draw):
    box(draw, (160, 130, 352, 380), RED, 20)


def mast_icon(draw):
    box(draw, (220, 100, 292, 412), YELLOW, 12)


def bolt(draw):
    draw.polygon([(290, 110), (180, 280), (250, 280), (216, 402), (340, 230), (268, 230)], fill=YELLOW, outline=OUTLINE)


def tip_icon(draw):
    draw.polygon([(120, 300), (360, 140), (400, 200), (160, 360)], fill=STEEL, outline=OUTLINE)
    for x, y in ((120, 380), (190, 400), (90, 410)):
        draw.ellipse((x - 24, y - 24, x + 24, y + 24), fill=DARK, outline=OUTLINE, width=8)


def drop(draw):
    draw.polygon([(256, 110), (350, 270), (162, 270)], fill=RED, outline=OUTLINE)
    draw.ellipse((162, 190, 350, 390), fill=RED, outline=OUTLINE, width=12)


def main():
    upgrades = VEHICLES / 'SurveyTruck/Upgrades'
    for key, colour, mark, what in (
        ('GeophoneArray', OLIVE, dots, 'three geophone dots'),
        ('RadiometricProbe', DARK, hazard, 'a hazard triangle'),
        ('Hydrogeology', BLUE, drop, 'a drop of water'),
        ('LongRangeArray', BLUE, dish_icon, 'a dish'),
        ('PoweredArray', ORANGE, bars, 'three bars'),
        ('SurveyBattery', BLUE, plus, 'a plus sign'),
        ('WorkLights', DARK, lamp, 'a lamp'),
    ):
        icon(upgrades / f'{key}.png', badge(colour, mark), what)
    upgrades = VEHICLES / 'DrillRig/Upgrades'
    for key, colour, mark, what in (
        ('DiamondBit', DARK, diamond_icon, 'a diamond'),
        ('RcHammer', DARK, hammer_icon, 'a hammer head'),
        ('TallMast', OLIVE, mast_icon, 'a mast'),
        ('ElectricPowerPack', BLUE, bolt, 'a lightning bolt'),
        ('WorkLights', DARK, lamp, 'a lamp'),
        ('DeepchemHead', OLIVE, drop, 'a drop of liquid'),
        ('WellDrillingKit', DARK, drop, 'a drop of oil'),
    ):
        icon(upgrades / f'{key}.png', badge(colour, mark), what)
    upgrades = VEHICLES / 'Tipper/Upgrades'
    for key, colour, mark, what in (
        ('HighSides', OLIVE, bars, 'three tall bars'),
        ('RockBody', DARK, hammer_icon, 'a thick block'),
        ('BulkBody', BLUE, mast_icon, 'a long body'),
    ):
        icon(upgrades / f'{key}.png', badge(colour, mark), what)
    icon(STATIONS / 'Tip.png', badge(ORANGE, tip_icon), 'a tilted body shedding its load')
    icon(STATIONS / 'SurveyLongRange.png', badge(BLUE, dish_icon), 'a dish')
    icon(STATIONS / 'SurveyArray.png', badge(ORANGE, bars), 'three bars')
    icon(STATIONS / 'PowerPoint.png', badge(DARK, bolt), 'a junction box with a lightning bolt', 64)


if __name__ == '__main__':
    main()
