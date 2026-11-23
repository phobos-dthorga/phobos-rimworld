"""Draw the service yard's tool and the building upgrades button with Pillow, in the game's look: flat signs, as the vehicle park's is.
No image generator and no game or other author's art is involved; each has a record beside it (D'thorga Framework 0.210.0;
docs/development/vehicle-service-yard.md).

    python assets/phobos-vehicles/draw_service_yard_icons.py

A sign is flat shapes, so it is drawn rather than generated. The local icon batch may still replace them, on the owner's word.
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'scripts' / 'art'))
from art_paths import record_path  # noqa: E402  (records of shipped files are kept under assets/records/)
YARD_FOLDER = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems/BayTrips'
CORE_FOLDER = ROOT / 'mods/PhobosFramework/Common/Textures/Phobos/Framework/Upgrades'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
YELLOW = (214, 170, 40, 255)
BLUE = (52, 110, 190, 255)
WHITE = (236, 238, 240, 255)
DARK = (40, 42, 46, 255)
LINE = 18


def yard(draw):
    # A safety-yellow plate with a spanner across it: the yard's colour on the map, and the work done there.
    draw.rounded_rectangle((56, 56, 456, 456), radius=64, fill=YELLOW, outline=OUTLINE, width=LINE)
    # The spanner's handle, drawn as a thick bar from lower left to upper right.
    draw.line((150, 362, 318, 194), fill=DARK, width=58)
    # Its open jaw: a ring with a notch cut towards the upper right.
    draw.ellipse((272, 108, 404, 240), fill=DARK)
    draw.ellipse((314, 150, 362, 198), fill=YELLOW)
    draw.polygon([(338, 174), (420, 92), (440, 136), (382, 200)], fill=YELLOW)
    # The handle's round end.
    draw.ellipse((112, 324, 188, 400), fill=DARK)


def upgrades(draw):
    # A blue plate with a white arrow pointing up over a bar: something added to a building.
    draw.rounded_rectangle((56, 56, 456, 456), radius=64, fill=BLUE, outline=OUTLINE, width=LINE)
    draw.polygon([(256, 108), (378, 252), (306, 252), (306, 348), (206, 348), (206, 252), (134, 252)], fill=WHITE)
    draw.rounded_rectangle((150, 372, 362, 408), radius=12, fill=WHITE)


ICONS = [
    (YARD_FOLDER, 'ServiceYard', yard, 'a spanner on a safety-yellow plate, for the service yard zone'),
    (CORE_FOLDER, 'BuildingUpgrades', upgrades, 'a white arrow pointing up over a bar on a blue plate, for the building upgrades button'),
]


def main():
    for folder, name, drawing, what in ICONS:
        folder.mkdir(parents=True, exist_ok=True)
        image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        drawing(ImageDraw.Draw(image))
        path = folder / f'{name}.png'
        image.resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        record_path(path.with_suffix('.record.md')).write_text(
            f'# {path.stem}\n\n**Drawn in code** by `assets/phobos-vehicles/draw_service_yard_icons.py` (Pillow): {what}. No image generator, '
            f'no reference images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.name)


if __name__ == '__main__':
    main()
