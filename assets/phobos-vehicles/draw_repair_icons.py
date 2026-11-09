"""Draw the vehicle repair buttons and the transmission and turbine part icons with Pillow, in the game's look. No image generator and no
other author's art is involved; each is a stand-in with a record beside it, as the vehicle systems icons are.

    python assets/phobos-vehicles/draw_repair_icons.py
"""
import hashlib
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
STEEL = (160, 164, 168, 255)
DARK = (92, 96, 100, 255)
OLIVE = (118, 122, 80, 255)
ORANGE = (214, 124, 44, 255)
BLUE = (96, 148, 196, 255)
LINE = 18


def canvas():
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def box(draw, xy, fill):
    draw.rounded_rectangle(xy, radius=22, fill=fill, outline=OUTLINE, width=LINE)


def transmission(draw):
    box(draw, (110, 170, 400, 360), OLIVE)
    draw.polygon([(400, 210), (480, 240), (480, 290), (400, 320)], fill=DARK, outline=OUTLINE)
    box(draw, (40, 225, 120, 305), STEEL)
    for x in (170, 240, 310):
        draw.ellipse((x - 30, 235, x + 30, 295), fill=STEEL, outline=OUTLINE, width=LINE)


def turbine(draw):
    draw.polygon([(60, 180), (360, 150), (460, 215), (460, 295), (360, 360), (60, 330)], fill=STEEL, outline=OUTLINE)
    draw.ellipse((30, 165, 150, 345), fill=DARK, outline=OUTLINE, width=LINE)
    for angle in range(0, 180, 45):
        image_centre = (90, 255)
        offset = {0: (0, 70), 45: (42, 42), 90: (42, 0), 135: (42, -42)}[angle]
        draw.line([(image_centre[0] - offset[0], image_centre[1] - offset[1]), (image_centre[0] + offset[0], image_centre[1] + offset[1])], fill=OUTLINE, width=12)
    draw.rectangle((230, 150, 270, 355), fill=ORANGE, outline=OUTLINE, width=12)


def field_fix(draw):
    draw.rounded_rectangle((60, 190, 452, 322), radius=66, fill=STEEL, outline=OUTLINE, width=LINE)
    box(draw, (190, 130, 322, 382), ORANGE)
    for y in (195, 256, 317):
        draw.line([(215, y), (297, y)], fill=OUTLINE, width=14)


def caravan_repair(draw):
    draw.line([(120, 410), (340, 190)], fill=STEEL, width=70)
    draw.line([(120, 410), (340, 190)], fill=OUTLINE, width=8)
    draw.pieslice((280, 60, 470, 250), 300, 150, fill=STEEL, outline=OUTLINE, width=LINE)
    draw.ellipse((60, 350, 180, 470), fill=BLUE, outline=OUTLINE, width=LINE)


def fittings(draw):
    centre = (216, 280)
    teeth = 10
    outer, inner = 186, 146
    points = []
    for tooth in range(teeth * 4):
        radius = outer if tooth % 4 in (0, 1) else inner
        angle = math.pi * 2 * tooth / (teeth * 4)
        points.append((centre[0] + radius * math.cos(angle), centre[1] + radius * math.sin(angle)))
    draw.polygon(points, fill=STEEL, outline=OUTLINE, width=LINE)
    draw.ellipse((centre[0] - 62, centre[1] - 62, centre[0] + 62, centre[1] + 62), fill=DARK, outline=OUTLINE, width=LINE)
    draw.ellipse((270, 54, 470, 254), fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.ellipse((328, 112, 412, 196), fill=DARK, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((420, 136, 492, 214), radius=14, fill=ORANGE, outline=OUTLINE, width=LINE)


ICONS = [
    ('Vehicles/Parts/Transmission', transmission, 'a gearbox casing with an input shaft and three gear covers'),
    ('Vehicles/Parts/TurbineAssembly', turbine, 'a gas turbine with an intake and an orange band'),
    ('VehicleSystems/FieldFix', field_fix, 'a pipe wrapped with an orange repair bandage'),
    ('VehicleSystems/CaravanRepair', caravan_repair, 'a spanner with a blue handle end'),
    ('VehicleSystems/Fittings', fittings, 'a gear wheel with an orange turbine snail bolted to it'),
]


def main(only=None):
    for relative, drawing, what in ICONS:
        if only and only not in relative:
            continue
        image, draw = canvas()
        drawing(draw)
        path = TEXTURES / f'{relative}.png'
        path.parent.mkdir(parents=True, exist_ok=True)
        image.resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {path.stem}\n\n**A stand-in, drawn in code** by `assets/phobos-vehicles/draw_repair_icons.py` (Pillow): {what}. No image '
            f'generator, no reference images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.name)


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else None)
