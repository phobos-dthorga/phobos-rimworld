"""Draw the bulk tanker's button icons and the fallback part icons with Pillow, in the game's look. No image generator and no game or
other author's art is involved; each is a stand-in with a record beside it, as the vehicle systems icons are.

    python assets/phobos-vehicles/draw_tank_icons.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
BUTTONS = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems/Tank'
PARTS = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/Vehicles/Parts'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
STEEL = (160, 164, 168, 255)
DARK = (92, 96, 100, 255)
OLIVE = (118, 122, 80, 255)
ORANGE = (214, 124, 44, 255)
BLUE = (96, 148, 196, 255)
GREEN = (116, 170, 96, 255)
RED = (196, 84, 70, 255)
LINE = 18


def canvas():
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def box(draw, rect, fill, radius=30):
    draw.rounded_rectangle(rect, radius=radius, fill=fill, outline=OUTLINE, width=LINE)


def arrow(draw, down, fill):
    top, bottom = 60, 450
    mid = 256
    points = [(mid - 60, top), (mid + 60, top), (mid + 60, 250), (mid + 130, 250), (mid, bottom), (mid - 130, 250), (mid - 60, 250)]
    if not down:
        points = [(x, WORK - y) for x, y in points]
    draw.polygon(points, fill=fill, outline=OUTLINE)
    draw.line(points + [points[0]], fill=OUTLINE, width=LINE, joint='curve')


def tank(draw, fill):
    draw.rounded_rectangle((70, 170, 442, 340), radius=85, fill=fill, outline=OUTLINE, width=LINE)
    for x in (180, 256, 332):
        draw.line((x, 180, x, 330), fill=OUTLINE, width=8)


def contents():
    image, draw = canvas()
    tank(draw, STEEL)
    draw.ellipse((205, 60, 307, 162), fill=BLUE, outline=OUTLINE, width=LINE)
    return image


def set_up():
    image, draw = canvas()
    tank(draw, STEEL)
    draw.arc((150, 300, 430, 500), 180, 360, fill=OUTLINE, width=34)
    draw.arc((150, 300, 430, 500), 180, 360, fill=DARK, width=16)
    box(draw, (396, 372, 470, 452), ORANGE, 12)
    return image


def pack_up():
    image, draw = canvas()
    tank(draw, STEEL)
    draw.ellipse((190, 350, 322, 482), fill=DARK, outline=OUTLINE, width=LINE)
    draw.ellipse((238, 398, 274, 434), fill=STEEL, outline=OUTLINE, width=8)
    return image


def load():
    image, draw = canvas()
    arrow(draw, True, GREEN)
    return image


def unload():
    image, draw = canvas()
    arrow(draw, False, ORANGE)
    return image


def hold():
    image, draw = canvas()
    box(draw, (150, 90, 230, 422), RED, 18)
    box(draw, (282, 90, 362, 422), RED, 18)
    return image


def engine():
    image, draw = canvas()
    box(draw, (110, 170, 402, 410), OLIVE)
    for x in (160, 226, 292):
        box(draw, (x, 90, x + 60, 180), STEEL, 10)
    draw.line((60, 290, 110, 290), fill=OUTLINE, width=34)
    return image


def drivetrain():
    image, draw = canvas()
    draw.line((90, 256, 422, 256), fill=OUTLINE, width=42)
    draw.line((90, 256, 422, 256), fill=STEEL, width=16)
    for x in (60, 372):
        box(draw, (x, 130, x + 80, 382), DARK, 22)
    box(draw, (196, 196, 316, 316), OLIVE, 18)
    return image


def electrical():
    image, draw = canvas()
    box(draw, (110, 130, 402, 420), OLIVE)
    bolt = [(280, 150), (190, 290), (250, 290), (226, 400), (326, 244), (262, 244)]
    draw.polygon(bolt, fill=ORANGE)
    draw.line(bolt + [bolt[0]], fill=OUTLINE, width=12, joint='curve')
    return image


def divided(parts, highlight=None):
    image, draw = canvas()
    left, top, right, bottom = 60, 150, 452, 362
    draw.rounded_rectangle((left, top, right, bottom), radius=90, fill=STEEL, outline=OUTLINE, width=LINE)
    width = (right - left) / parts
    if highlight is not None:
        x = left + width * highlight
        draw.rectangle((x + 12, top + 18, x + width - 12, bottom - 18), fill=ORANGE)
    for i in range(1, parts):
        x = left + width * i
        draw.line((x, top + 8, x, bottom - 8), fill=OUTLINE, width=LINE)
    return image


def compartment():
    return divided(3, 1)


def two_compartments():
    return divided(2)


def three_compartments():
    return divided(3)


def bulk_order():
    image, draw = canvas()
    tank(draw, STEEL)
    draw.ellipse((300, 300, 470, 470), fill=(214, 214, 220, 255), outline=OUTLINE, width=LINE)
    draw.ellipse((345, 345, 425, 425), outline=OUTLINE, width=12)
    return image


def write(folder, name, draw_fn, what):
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / f'{name}.png'
    draw_fn().convert('RGBa').resize((FINAL, FINAL), Image.LANCZOS).convert('RGBA').save(path)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    (folder / f'{name}.record.md').write_text(
        f'# {name}\n\n**A stand-in, drawn in code** by `assets/phobos-vehicles/draw_tank_icons.py` (Pillow): {what}. '
        f'No image generator, no reference images and no game or other author\'s art was used. {FINAL} x {FINAL} px, transparent background.\n\n'
        f'- `{name}.png`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')


if __name__ == '__main__':
    write(BUTTONS, 'Contents', contents, 'a steel tank under a blue droplet')
    write(BUTTONS, 'SetUp', set_up, 'a tank with a hose laid out and a coupling')
    write(BUTTONS, 'PackUp', pack_up, 'a tank above a coiled hose')
    write(BUTTONS, 'Load', load, 'a green arrow pointing down')
    write(BUTTONS, 'Unload', unload, 'an orange arrow pointing up')
    write(BUTTONS, 'Hold', hold, 'two red bars')
    write(BUTTONS, 'Compartment', compartment, 'a tank in three divisions with the middle one lit')
    write(BUTTONS, 'TwoCompartments', two_compartments, 'a tank divided in two')
    write(BUTTONS, 'ThreeCompartments', three_compartments, 'a tank divided in three')
    write(BUTTONS, 'BulkOrder', bulk_order, 'a tank with a silver coin')
    write(PARTS, 'EngineAssembly', engine, 'an olive engine block with three cylinder heads')
    write(PARTS, 'DrivetrainKit', drivetrain, 'an axle with two tyres and a gearbox')
    write(PARTS, 'ElectricalKit', electrical, 'an olive box with an orange lightning bolt')
