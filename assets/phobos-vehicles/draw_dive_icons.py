"""Draw the Seabed Crawler's upgrade icons with Pillow, in the game's look. No image generator and no game or other author's art is
involved; each has a record beside it, as the vehicle systems icons do.

    python assets/phobos-vehicles/draw_dive_icons.py

The Dive, Surface, Stay down and Settle buttons have generated pictures the owner picked (10 Nov 2026), so this no longer draws them.
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
CRAWLER = ROOT / 'mods/PhobosVehicles/Textures/Phobos/Vehicles/SeabedCrawler/Upgrades'
RUBBER = (56, 58, 62, 255)
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
HULL = (92, 100, 110, 255)
WATER = (84, 140, 190, 255)
ORANGE = (214, 124, 44, 255)
LINE = 18


def wheel(draw, x, y, radius):
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=RUBBER, outline=OUTLINE, width=LINE)
    draw.ellipse((x - radius // 3, y - radius // 3, x + radius // 3, y + radius // 3), fill=ORANGE, outline=OUTLINE, width=LINE // 2)


def bottoming_wheels(draw):
    draw.rounded_rectangle((90, 110, 422, 170), radius=16, fill=HULL, outline=OUTLINE, width=LINE)
    for x in (150, 362):
        draw.rectangle((x - 22, 160, x + 22, 300), fill=HULL, outline=OUTLINE, width=LINE)
        wheel(draw, x, 350, 92)


def scrubber(draw):
    draw.rounded_rectangle((60, 70, 452, 442), radius=26, fill=HULL, outline=OUTLINE, width=LINE)
    for x in (146, 256, 366):
        draw.rounded_rectangle((x - 40, 120, x + 40, 392), radius=26, fill=(214, 216, 212, 255), outline=OUTLINE, width=LINE)
        draw.rectangle((x - 31, 220, x + 31, 276), fill=WATER)


ICONS = [
    ('BottomingWheels', bottoming_wheels, 'two rubber-tyred wheels on struts from a bar', CRAWLER),
    ('Scrubber', scrubber, 'three pale canisters with blue bands in a grey rack', CRAWLER),
]


def main():
    CRAWLER.mkdir(parents=True, exist_ok=True)
    for name, drawing, what, folder in ICONS:
        image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        drawing(ImageDraw.Draw(image))
        path = folder / f'{name}.png'
        image.resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {path.stem}\n\n**Drawn in code** by `assets/phobos-vehicles/draw_dive_icons.py` (Pillow): {what}. No image generator, no reference '
            f'images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.name)


if __name__ == '__main__':
    main()
