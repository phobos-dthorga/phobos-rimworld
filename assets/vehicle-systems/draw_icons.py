"""Draw D'thorga Framework's vehicle systems button icons with Pillow, in the game's look: a thick near-black outline, pale fills
and flat cel shading. No image generator and no game or other author's art is involved.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/vehicle-systems/draw_icons.py [name ...]

Name icons to draw only those. Each icon goes into the mod with a record saying it is a stand-in drawn in code; generated art can
replace it on the owner's go, and then its entry here is removed so this can't overwrite the pick.

- Switch: a rocker switch, the fallback for any load a loadout gives no icon of its own. Kept by the owner on 10 Nov 2026, when every
  generated version came out as a medical cross or a blank plate.

The other buttons this once drew have generated pictures the owner picked (9 and 10 Nov 2026).
"""
import datetime as dt
import hashlib
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
WORK = 1024  # Drawn large and scaled down, which smooths the edges.
FINAL = 128
LINE = 44
OUTLINE = (24, 24, 26, 255)
PALE = (226, 222, 210, 255)
PALE_SHADE = (184, 180, 168, 255)
STEEL_SHADE = (118, 122, 128, 255)
AMBER = (240, 196, 92, 255)
AMBER_SHADE = (206, 156, 60, 255)


def canvas():
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def outlined_polygon(draw, points, fill, shade=None, shade_points=None):
    draw.polygon(points, fill=OUTLINE)
    inner = shrink(points, LINE * 0.75)
    draw.polygon(inner, fill=fill)
    if shade and shade_points:
        draw.polygon(shade_points, fill=shade)


def shrink(points, amount):
    cx = sum(p[0] for p in points) / len(points)
    cy = sum(p[1] for p in points) / len(points)
    result = []
    for x, y in points:
        dx, dy = x - cx, y - cy
        length = math.hypot(dx, dy) or 1
        result.append((x - dx / length * amount, y - dy / length * amount))
    return result


def switch():
    image, draw = canvas()
    plate = [(250, 170), (774, 170), (774, 854), (250, 854)]
    outlined_polygon(draw, plate, PALE, PALE_SHADE, [(700, 220), (730, 220), (730, 810), (700, 810)])
    rocker_top = [(370, 260), (654, 330), (654, 540), (370, 540)]
    rocker_bottom = [(370, 540), (654, 540), (654, 750), (370, 750)]
    outlined_polygon(draw, rocker_bottom, STEEL_SHADE)
    outlined_polygon(draw, rocker_top, AMBER, AMBER_SHADE, [(560, 340), (630, 356), (630, 520), (560, 520)])
    return image


DRAWINGS = {
    'Switch': (switch, 'a rocker switch on a pale plate, the fallback icon for any switchable load'),
}


def write(name):
    draw_fn, description = DRAWINGS[name]
    image = draw_fn().resize((FINAL, FINAL), Image.LANCZOS)
    folder = TEXTURES
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / (name + '.png')
    image.save(path)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    today = dt.date.today().strftime('%d %B %Y')
    record = (
        f'# {name}\n\n'
        f'**A stand-in, drawn in code** by `assets/vehicle-systems/draw_icons.py` (Pillow) on {today}: {description}. '
        'No image generator, no reference images and no game or other author\'s art was used. '
        f'{FINAL} x {FINAL} px, transparent background.\n\n'
        '**Outstanding:** generated art can replace it on the owner\'s go (docs/development/art-backlog.md).\n\n'
        f'- `{name}.png`: SHA-256 `{digest}`\n'
    )
    (folder / (name + '.record.md')).write_text(record, encoding='utf-8', newline='\n')
    print(name, digest)


if __name__ == '__main__':
    for name in sys.argv[1:] or DRAWINGS:
        write(name)
