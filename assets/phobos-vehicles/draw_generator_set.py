"""Draw stand-ins for the generator set, its overlays, its upgrade icons and its cable end with Pillow, until the owner wants real art. No image generator, reference
image or other author's art is involved; each file set has a record beside it. Drawn at 96 pixels a cell, 1.5 times the game's 64 (the rule for vehicle art).

    python assets/phobos-vehicles/draw_generator_set.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
VEHICLE = ROOT / 'mods/PhobosVehicles/Textures/Phobos/Vehicles/GeneratorSet'
FRAMEWORK = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems/Stations'
CELL = 96
WIDTH, HEIGHT = 3 * CELL, 7 * CELL
OUTLINE = (24, 24, 26, 255)
STEEL = (160, 164, 168, 255)
DARK = (92, 96, 100, 255)
OLIVE = (118, 122, 80, 255)
SAND = (162, 156, 123, 255)
ORANGE = (214, 124, 44, 255)
BLUE = (96, 148, 196, 255)
YELLOW = (226, 196, 70, 255)
LINE = 6


def box(draw, xy, fill, radius=10):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=OUTLINE, width=LINE)


def body(draw):
    box(draw, (48, 90, 240, 640), SAND, 16)
    box(draw, (66, 40, 222, 120), DARK)
    box(draw, (60, 120, 228, 240), SAND)
    draw.rectangle((78, 140, 210, 196), fill=BLUE, outline=OUTLINE, width=LINE)
    box(draw, (72, 270, 216, 470), OLIVE)
    for y in (300, 345, 390, 435):
        draw.line([(92, y), (196, y)], fill=OUTLINE, width=8)
    box(draw, (72, 495, 216, 610), DARK)


def set_up(draw):
    for x in (18, 228):
        box(draw, (x, 596, x + 42, 640), YELLOW, 6)
    draw.line([(144, 640), (144, 668), (220, 668)], fill=ORANGE, width=14)


def reel(draw):
    draw.ellipse((150, 515, 216, 581), fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.ellipse((172, 537, 194, 559), fill=DARK, outline=OUTLINE, width=LINE)


def bigger(draw):
    box(draw, (60, 258, 228, 330), STEEL)
    draw.rectangle((92, 278, 196, 310), fill=ORANGE, outline=OUTLINE, width=LINE)


def battery(draw):
    box(draw, (84, 395, 204, 470), BLUE)
    draw.line([(120, 432), (168, 432)], fill=OUTLINE, width=10)
    draw.line([(144, 412), (144, 452)], fill=OUTLINE, width=10)


def facings(name, drawing, what):
    north = Image.new('RGBA', (WIDTH, HEIGHT), (0, 0, 0, 0))
    drawing(ImageDraw.Draw(north))
    VEHICLE.mkdir(parents=True, exist_ok=True)
    digests = []
    for facing, turn in (('north', 0), ('east', -90), ('south', 180)):
        image = north.rotate(turn, expand=True)
        path = VEHICLE / f'{name}_{facing}.png'
        image.save(path)
        digests.append(f'- `{path.name}`: SHA-256 `{hashlib.sha256(path.read_bytes()).hexdigest()}`')
        print(path.name)
    (VEHICLE / f'{name}.record.md').write_text(
        f'# {name}\n\n**A stand-in, drawn in code** by `assets/phobos-vehicles/draw_generator_set.py` (Pillow): {what}. No image generator, no reference images and no game or '
        'other author\'s art.\n\n' + '\n'.join(digests) + '\n', encoding='utf-8', newline='\n')


def icon(path, drawing, what, size=128):
    image = Image.new('RGBA', (512, 512), (0, 0, 0, 0))
    drawing(ImageDraw.Draw(image))
    path.parent.mkdir(parents=True, exist_ok=True)
    image.resize((size, size), Image.LANCZOS).save(path)
    path.with_suffix('.record.md').write_text(
        f'# {path.stem}\n\n**A stand-in, drawn in code** by `assets/phobos-vehicles/draw_generator_set.py` (Pillow): {what}. No image generator, no reference images and no game or '
        f'other author\'s art.\n\n- `{path.name}`: SHA-256 `{hashlib.sha256(path.read_bytes()).hexdigest()}`\n', encoding='utf-8', newline='\n')
    print(path.name)


def alternator_icon(draw):
    box(draw, (90, 150, 422, 362), STEEL, 26)
    draw.polygon([(280, 170), (200, 275), (252, 275), (226, 342), (316, 232), (262, 232)], fill=YELLOW, outline=OUTLINE, width=8)


def reel_icon(draw):
    draw.ellipse((96, 96, 416, 416), fill=ORANGE, outline=OUTLINE, width=16)
    draw.ellipse((206, 206, 306, 306), fill=DARK, outline=OUTLINE, width=16)


def battery_icon(draw):
    box(draw, (110, 160, 402, 370), BLUE, 24)
    box(draw, (200, 120, 312, 170), DARK, 10)
    draw.line([(190, 265), (322, 265)], fill=OUTLINE, width=22)
    draw.line([(256, 200), (256, 330)], fill=OUTLINE, width=22)


def cable_end(draw):
    box(draw, (150, 150, 362, 362), DARK, 24)
    box(draw, (200, 200, 312, 312), ORANGE, 12)
    draw.line([(256, 30), (256, 150)], fill=OUTLINE, width=40)
    draw.line([(256, 30), (256, 150)], fill=ORANGE, width=20)


def main():
    facings('GeneratorSet', body, 'a light truck from above: sand body, dark bonnet, blue windscreen, olive generator housing and dark deck')
    facings('GeneratorSet_SetUp', set_up, 'yellow outrigger feet and an orange cable leaving the rear')
    facings('GeneratorSet_Reel', reel, 'an orange cable drum on the rear deck')
    facings('GeneratorSet_BiggerGenerator', bigger, 'a taller steel housing with an orange band behind the cab')
    facings('GeneratorSet_Battery', battery, 'a blue battery box on the deck')
    upgrades = VEHICLE / 'Upgrades'
    icon(upgrades / 'BiggerAlternator.png', alternator_icon, 'a generator casing with a lightning bolt')
    icon(upgrades / 'CableReel.png', reel_icon, 'a cable drum')
    icon(upgrades / 'BatteryBuffer.png', battery_icon, 'a blue battery with a plus sign')
    icon(FRAMEWORK / 'CableEnd.png', cable_end, 'a dark junction box with an orange cable', 64)


if __name__ == '__main__':
    main()
