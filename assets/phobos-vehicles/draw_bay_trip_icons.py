"""Draw the vehicle park's button with Pillow, in the game's look: a parking sign. No image generator and no game or other author's art is
involved; it has a record beside it, as the hitch's buttons do (D'thorga Framework 0.201.0; docs/development/vehicle-bay-trips.md).

    python assets/phobos-vehicles/draw_bay_trip_icons.py

A sign is flat shapes, so it is drawn rather than generated. The local icon batch may still replace it, on the owner's word.
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems/BayTrips'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
BLUE = (52, 110, 190, 255)
WHITE = (236, 238, 240, 255)
LINE = 18


def park(draw):
    # The sign's plate, then a capital P from a stem and a bowl with its middle cut out.
    draw.rounded_rectangle((56, 56, 456, 456), radius=64, fill=BLUE, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((176, 128, 232, 392), radius=10, fill=WHITE)
    draw.rounded_rectangle((176, 128, 352, 288), radius=80, fill=WHITE)
    draw.rounded_rectangle((232, 176, 300, 240), radius=32, fill=BLUE)


ICONS = [
    ('VehiclePark', park, 'a parking sign, a white P on a blue plate, for the vehicle park zone'),
]


def main():
    FOLDER.mkdir(parents=True, exist_ok=True)
    for name, drawing, what in ICONS:
        image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        drawing(ImageDraw.Draw(image))
        path = FOLDER / f'{name}.png'
        image.resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {path.stem}\n\n**Drawn in code** by `assets/phobos-vehicles/draw_bay_trip_icons.py` (Pillow): {what}. No image generator, no '
            f'reference images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.name)


if __name__ == '__main__':
    main()
