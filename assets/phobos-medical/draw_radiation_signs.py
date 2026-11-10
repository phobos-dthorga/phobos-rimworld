"""Draw D'thorga Medical's radiation control pictures with Pillow, in the game's look: thick near-black outlines, flat colours.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/phobos-medical/draw_radiation_signs.py

The radiation trefoil is the international symbol (ISO 361), magenta on yellow, drawn here from its standard proportions: a centre disc
of radius R, three 60-degree blades from 1.5 R to 5 R, one pointing down. No image generator, no reference images and no game or other
author's art. Each picture gets a record beside it. Generated art can replace these on the owner's go.
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods/PhobosMedical/Mods/Rimatomics/Textures/Phobos/Medical/Protection'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
YELLOW = (240, 200, 60, 255)
MAGENTA = (176, 40, 150, 255)
STEEL = (150, 154, 160, 255)
GREEN = (90, 170, 80, 255)
RED = (196, 70, 60, 255)
LINE = 22


def trefoil(draw, cx, cy, r):
    """The trefoil centred at (cx, cy) with centre-disc radius r: blades at 90, 210 and 330 degrees (Pillow's angles run clockwise
    from three o'clock, so 90 is straight down)."""
    outer, inner = 5 * r, 1.5 * r
    for centre in (90, 210, 330):
        draw.pieslice((cx - outer, cy - outer, cx + outer, cy + outer), centre - 30, centre + 30, fill=MAGENTA)
    draw.ellipse((cx - inner, cy - inner, cx + inner, cy + inner), fill=YELLOW)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=MAGENTA)


def control_point():
    """A yellow sign plate on a short steel post, the trefoil on it."""
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rectangle((236, 330, 276, 470), fill=STEEL, outline=OUTLINE, width=LINE // 2)
    draw.rounded_rectangle((176, 450, 336, 486), radius=12, fill=STEEL, outline=OUTLINE, width=LINE // 2)
    draw.rounded_rectangle((70, 40, 442, 350), radius=28, fill=YELLOW, outline=OUTLINE, width=LINE)
    trefoil(draw, 256, 198, 24)
    return image


def area(plus):
    """The trefoil on a yellow disc, with a green plus or a red minus badge, like the game's own area tools."""
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.ellipse((40, 40, 400, 400), fill=YELLOW, outline=OUTLINE, width=LINE)
    trefoil(draw, 220, 226, 26)
    draw.ellipse((310, 310, 490, 490), fill=GREEN if plus else RED, outline=OUTLINE, width=LINE)
    draw.rectangle((350, 385, 450, 415), fill=(255, 255, 255, 255))
    if plus:
        draw.rectangle((385, 350, 415, 450), fill=(255, 255, 255, 255))
    return image


PICTURES = [
    ('RadiationControlPoint', control_point, 'a yellow sign plate on a short steel post, bearing the radiation trefoil'),
    ('RadiationAreaExpand', lambda: area(True), 'the radiation trefoil on a yellow disc with a green plus badge'),
    ('RadiationAreaClear', lambda: area(False), 'the radiation trefoil on a yellow disc with a red minus badge'),
]


def main():
    TEXTURES.mkdir(parents=True, exist_ok=True)
    for name, drawing, what in PICTURES:
        path = TEXTURES / f'{name}.png'
        drawing().resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {name}\n\n**Drawn in code** by `assets/phobos-medical/draw_radiation_signs.py` (Pillow): {what}. The trefoil is the international '
            f'radiation symbol (ISO 361), drawn from its standard proportions. No image generator, no reference images and no game or other '
            f'author\'s art. {FINAL} x {FINAL} px, transparent background.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.name)


if __name__ == '__main__':
    main()
