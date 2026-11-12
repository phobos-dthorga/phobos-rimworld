"""Draw the helicopter signatures' pictures (the water bucket and the litter kit) with Pillow, in the look of the batch 3 buttons: the Water
drop button, the fire bucket item, the litter kit fitting and the Unload casualties button. No image generator and no game or other
author's art is involved; each has a record beside it (D'thorga Framework 0.101.0).

    python assets/phobos-vehicles/draw_ambulance_icons.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
STEEL = (150, 156, 162, 255)
DARK = (92, 100, 110, 255)
ORANGE = (214, 124, 44, 255)
RED = (190, 60, 50, 255)
WATER = (86, 150, 210, 255)
FLAME = (236, 170, 60, 255)
CANVAS = (204, 190, 150, 255)
WHITE = (236, 236, 230, 255)
LINE = 18


def bucket_shape(draw, left, top, right, bottom):
    # A bucket wider at the mouth, with a dark rim and a steel bail to its hook.
    draw.polygon([(left, top), (right, top), (right - 40, bottom), (left + 40, bottom)], fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.rectangle((left - 6, top - 14, right + 6, top + 22), fill=DARK, outline=OUTLINE, width=LINE // 2)
    middle = (left + right) // 2
    draw.line([(left + 10, top - 6), (middle, top - 110), (right - 10, top - 6)], fill=OUTLINE, width=14, joint='curve')
    draw.ellipse((middle - 22, top - 140, middle + 22, top - 96), fill=STEEL, outline=OUTLINE, width=LINE // 2)


def water_drop(draw):
    # A bucket hanging from a line, pouring water down onto flames.
    draw.line([(150, 0), (150, 70)], fill=OUTLINE, width=14)
    bucket_shape(draw, 60, 180, 250, 300)
    for x in (100, 150, 200):
        draw.line([(x, 300), (x + 40, 420)], fill=WATER, width=26)
    draw.polygon([(250, 490), (300, 330), (330, 410), (370, 280), (420, 400), (450, 340), (490, 490)], fill=FLAME, outline=OUTLINE, width=LINE)
    draw.polygon([(320, 490), (350, 410), (380, 450), (410, 390), (440, 490)], fill=RED, outline=OUTLINE, width=LINE // 2)


def fire_bucket(draw):
    # The item: the bucket alone, with a dump valve at its foot.
    bucket_shape(draw, 110, 200, 400, 440)
    draw.rectangle((220, 440, 290, 480), fill=DARK, outline=OUTLINE, width=LINE // 2)
    draw.line([(150, 280), (360, 280)], fill=OUTLINE, width=10)


def stretcher(draw, top):
    # A canvas stretcher with two poles and short legs, seen from the side and a little above.
    draw.rounded_rectangle((40, top, 472, top + 90), radius=20, fill=CANVAS, outline=OUTLINE, width=LINE)
    draw.line([(20, top + 20), (492, top + 20)], fill=DARK, width=22)
    draw.line([(20, top + 70), (492, top + 70)], fill=DARK, width=22)
    for x in (90, 422):
        draw.line([(x, top + 90), (x, top + 150)], fill=OUTLINE, width=20)


def red_cross(draw, cx, cy, size):
    draw.rounded_rectangle((cx - size, cy - size, cx + size, cy + size), radius=24, fill=WHITE, outline=OUTLINE, width=LINE)
    arm = size * 0.32
    draw.rectangle((cx - arm, cy - size + 26, cx + arm, cy + size - 26), fill=RED)
    draw.rectangle((cx - size + 26, cy - arm, cx + size - 26, cy + arm), fill=RED)


def litter_kit(draw):
    stretcher(draw, 290)
    red_cross(draw, 256, 150, 110)


def unload(draw):
    stretcher(draw, 320)
    # An arrow down onto the ground beside it.
    draw.polygon([(196, 40), (316, 40), (316, 150), (380, 150), (256, 270), (132, 150), (196, 150)], fill=WATER, outline=OUTLINE, width=LINE)


ICONS = [
    ('Sling/WaterDrop', water_drop, 'an orange bucket on a line pouring blue water onto orange and red flames'),
    ('Things/FireBucket', fire_bucket, 'an orange bucket with a dark rim, a steel bail and ring, and a dark valve at its foot'),
    ('Fittings/LitterKit', litter_kit, 'a canvas stretcher on dark poles below a white square with a red cross'),
    ('Litter/Unload', unload, 'a canvas stretcher on dark poles below a blue arrow pointing down'),
]


def main():
    for name, drawing, what in ICONS:
        path = FOLDER / f'{name}.png'
        path.parent.mkdir(parents=True, exist_ok=True)
        image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        drawing(ImageDraw.Draw(image))
        image.resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {path.stem}\n\n**Drawn in code** by `assets/phobos-vehicles/draw_ambulance_icons.py` (Pillow): {what}. No image generator, no '
            f'reference images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.relative_to(ROOT))


if __name__ == '__main__':
    main()
