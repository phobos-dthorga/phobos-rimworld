"""Draw the hitch's two buttons (Hitch a trailer, Unhitch) with Pillow, in the game's look. No image generator and no game or other author's
art is involved; each has a record beside it, as the vehicle systems icons do (D'thorga Framework 0.101.0).

    python assets/phobos-vehicles/draw_hitch_icons.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems/Hitch'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
STEEL = (150, 156, 162, 255)
DARK = (92, 100, 110, 255)
ORANGE = (214, 124, 44, 255)
LINE = 18


def ball(draw, x, y):
    # A tow ball on its neck, seen from the side.
    draw.rectangle((x - 26, y, x + 26, y + 120), fill=DARK, outline=OUTLINE, width=LINE)
    draw.ellipse((x - 70, y - 120, x + 70, y + 20), fill=STEEL, outline=OUTLINE, width=LINE)


def coupling(draw, x, y):
    # The trailer's coupling head: a cup over the ball and its drawbar running off to the right.
    draw.rounded_rectangle((x + 40, y - 40, 492, y + 30), radius=14, fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.chord((x - 90, y - 140, x + 90, y + 40), start=180, end=360, fill=ORANGE, outline=OUTLINE, width=LINE)


def bumper(draw):
    draw.rounded_rectangle((20, 330, 300, 400), radius=14, fill=DARK, outline=OUTLINE, width=LINE)


def hitch(draw):
    bumper(draw)
    ball(draw, 180, 230)
    coupling(draw, 180, 190)


def unhitch(draw):
    bumper(draw)
    ball(draw, 180, 230)
    coupling(draw, 200, 150)
    # An upward arrow between them: lifted off the ball.
    draw.polygon([(410, 210), (360, 280), (395, 280), (395, 330), (425, 330), (425, 280), (460, 280)], fill=STEEL, outline=OUTLINE, width=LINE // 2)


ICONS = [
    ('Hitch', hitch, 'a steel tow ball on a dark bumper with an orange trailer coupling seated over it'),
    ('Unhitch', unhitch, 'a steel tow ball on a dark bumper with an orange trailer coupling lifted clear of it, and a pale arrow pointing up'),
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
            f'# {path.stem}\n\n**Drawn in code** by `assets/phobos-vehicles/draw_hitch_icons.py` (Pillow): {what}. No image generator, no reference '
            f'images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.name)


if __name__ == '__main__':
    main()
