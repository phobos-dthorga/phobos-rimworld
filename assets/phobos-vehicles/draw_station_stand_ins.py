"""Draw stand-ins for the vehicle parts bench and the tuning rig with Pillow, in the game's look, until they get proper art. No image generator,
reference image or other author's art is involved; each has a record beside it.

    python assets/phobos-vehicles/draw_station_stand_ins.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems/Stations'
OUTLINE = (24, 24, 26, 255)
STEEL = (160, 164, 168, 255)
DARK = (92, 96, 100, 255)
OLIVE = (118, 122, 80, 255)
ORANGE = (214, 124, 44, 255)
BLUE = (96, 148, 196, 255)
LINE = 10


def box(draw, xy, fill, radius=14):
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=OUTLINE, width=LINE)


def parts_bench(draw):
    """1400 by 600, drawn at 3.5 by 1.5 cells: the top fills the middle 3 by 1 cells."""
    box(draw, (100, 100, 1300, 500), OLIVE)
    box(draw, (150, 150, 520, 450), STEEL)
    for x in (230, 335, 440):
        draw.ellipse((x - 48, 252, x + 48, 348), fill=DARK, outline=OUTLINE, width=LINE)
    box(draw, (580, 170, 900, 430), DARK)
    draw.rectangle((640, 225, 840, 375), fill=ORANGE, outline=OUTLINE, width=LINE)
    box(draw, (960, 150, 1250, 300), BLUE)
    box(draw, (960, 330, 1250, 450), STEEL)


def tuning_rig(draw):
    """800 by 800, drawn at 2 by 2 cells."""
    box(draw, (70, 70, 730, 730), DARK, 26)
    box(draw, (130, 130, 670, 400), OLIVE)
    draw.rectangle((180, 180, 620, 350), fill=BLUE, outline=OUTLINE, width=LINE)
    draw.line([(220, 310), (300, 240), (380, 290), (460, 215), (580, 265)], fill=ORANGE, width=18)
    box(draw, (130, 450, 370, 670), STEEL)
    draw.ellipse((185, 500, 315, 630), fill=DARK, outline=OUTLINE, width=LINE)
    box(draw, (430, 450, 670, 670), ORANGE)
    for y in (510, 560, 610):
        draw.line([(480, y), (620, y)], fill=OUTLINE, width=14)


STATIONS = [
    ('PartsBench', parts_bench, (1400, 600), (350, 150), 'a long olive bench with a three-cylinder engine block, a machine in orange and two parts trays'),
    ('TuningRig', tuning_rig, (800, 800), (256, 256), 'a steel frame with a blue trace display, a dynamometer roller and an orange vented cabinet'),
]


def main():
    TEXTURES.mkdir(parents=True, exist_ok=True)
    for name, drawing, work, final, what in STATIONS:
        image = Image.new('RGBA', work, (0, 0, 0, 0))
        drawing(ImageDraw.Draw(image))
        path = TEXTURES / f'{name}.png'
        image.resize(final, Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {name}\n\n**A stand-in, drawn in code** by `assets/phobos-vehicles/draw_station_stand_ins.py` (Pillow): {what}. No image generator, '
            f'no reference images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.name)


if __name__ == '__main__':
    main()
