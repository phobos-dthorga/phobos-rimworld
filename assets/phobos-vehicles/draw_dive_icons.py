"""Draw the submersible's Dive and Surface button icons with Pillow, in the game's look. No image generator and no game or other author's art is
involved; each has a record beside it, as the vehicle systems icons do.

    python assets/phobos-vehicles/draw_dive_icons.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
CRAWLER = ROOT / 'mods/PhobosVehicles/Textures/Phobos/Vehicles/SeabedCrawler/Upgrades'
SAND = (176, 150, 104, 255)
RUBBER = (56, 58, 62, 255)
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
HULL = (92, 100, 110, 255)
WATER = (84, 140, 190, 255)
ORANGE = (214, 124, 44, 255)
LINE = 18


def water_line(draw, y):
    """Three humps of a wave across the picture."""
    points = []
    for x in range(30, 483, 6):
        phase = (x - 30) / 151 * 2 * 3.14159
        from math import sin
        points.append((x, y + 14 * sin(phase)))
    draw.line(points, fill=OUTLINE, width=LINE + 14, joint='curve')
    draw.line(points, fill=WATER, width=LINE - 2, joint='curve')


def hull(draw, y):
    """A submarine from the side: a long rounded body with a small tower."""
    draw.rounded_rectangle((200, y - 62, 300, y + 10), radius=14, fill=HULL, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((60, y, 452, y + 96), radius=48, fill=HULL, outline=OUTLINE, width=LINE)


def arrow(draw, x, top, bottom, down):
    """A short thick arrow, pointing down or up."""
    head = 58
    if down:
        points = [(x - 22, top), (x + 22, top), (x + 22, bottom - head), (x + 54, bottom - head), (x, bottom), (x - 54, bottom - head), (x - 22, bottom - head)]
    else:
        points = [(x - 22, bottom), (x + 22, bottom), (x + 22, top + head), (x + 54, top + head), (x, top), (x - 54, top + head), (x - 22, top + head)]
    draw.polygon(points, fill=ORANGE, outline=OUTLINE)
    draw.line(points + [points[0]], fill=OUTLINE, width=LINE, joint='curve')


def dive(draw):
    water_line(draw, 120)
    hull(draw, 300)
    arrow(draw, 404, 150, 300, True)


def surface(draw):
    hull(draw, 170)
    water_line(draw, 340)
    arrow(draw, 256, 330, 490, False)


def stay_down(draw):
    water_line(draw, 120)
    hull(draw, 340)
    draw.rounded_rectangle((110, 188, 402, 244), radius=14, fill=ORANGE, outline=OUTLINE, width=LINE)


def wheel(draw, x, y, radius):
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=RUBBER, outline=OUTLINE, width=LINE)
    draw.ellipse((x - radius // 3, y - radius // 3, x + radius // 3, y + radius // 3), fill=ORANGE, outline=OUTLINE, width=LINE // 2)


def settle(draw):
    water_line(draw, 80)
    hull(draw, 210)
    wheel(draw, 150, 345, 42)
    wheel(draw, 362, 345, 42)
    draw.rounded_rectangle((30, 388, 482, 480), radius=20, fill=SAND, outline=OUTLINE, width=LINE)


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
    ('Dive', dive, 'a submarine below a wave line with an orange arrow pointing down', TEXTURES),
    ('Surface', surface, 'a submarine above a wave line with an orange arrow pointing up', TEXTURES),
    ('StayDown', stay_down, 'a submarine below a wave line with an orange bar between them', TEXTURES),
    ('Settle', settle, 'a submarine on two wheels resting on a sandy bottom below a wave line', TEXTURES),
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
