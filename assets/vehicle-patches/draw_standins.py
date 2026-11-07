"""Draw D'thorga Vehicle Patches' stand-in item textures with Pillow, in the game's look: a thick near-black outline and flat cel
shading. No image generator and no game or other author's art is involved.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/vehicle-patches/draw_standins.py [name ...]

Name drawings to draw only those. Each texture goes into the mod with a record saying it is a stand-in; generated art replaces it
once the owner picks one (owner's direction, 7 Nov 2026), and then its entry here is removed so this can't overwrite the pick.

The bomb cases are empty Mk. 82 and Mk. 84 bodies seen from above: olive-drab steel, an ogive nose with the fuze well open, an open
tail with its threaded ring, and two suspension lugs on the top. No fins, which are fitted with the fill, and no yellow band: on US
bombs a yellow band marks an explosive filling, and an empty case has none. Proportions are simplified to fit a square icon.
"""
import datetime as dt
import hashlib
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods/PhobosPatches/Mods/HarrierVtol/Textures/Phobos/VehiclePatches'
WORK = 1024  # Drawn large and scaled down, which smooths the edges.
FINAL = 128
LINE = 40
TILT = 35  # Degrees anticlockwise, so the long body fits the square.
OUTLINE = (24, 24, 26, 255)
OLIVE = (104, 110, 70, 255)
OLIVE_LIGHT = (134, 140, 94, 255)
OLIVE_DARK = (76, 80, 52, 255)
STEEL_DARK = (62, 64, 66, 255)
STEEL = (150, 152, 150, 255)
HOLE = (34, 34, 36, 255)


def profile(length, radius, nose_share, tail_share, tail_ratio, tip_ratio):
    """Half-widths along the body from nose (x = 0) to tail: an ogive nose cut off at the fuze well, a straight middle and a
    tapered tail ending open."""
    nose, tail_start = length * nose_share, length * (1 - tail_share)
    points = []
    for i in range(121):
        x = length * i / 120
        if x < nose:
            t = x / nose
            r = radius * max(tip_ratio, math.sqrt(1 - (1 - t) ** 2))
        elif x <= tail_start:
            r = radius
        else:
            t = (x - tail_start) / (length - tail_start)
            r = radius * (1 - (1 - tail_ratio) * t)
        points.append((x, r))
    return points


def bomb_case(length, radius, lugs, nose_share=0.3, tail_share=0.28):
    """One case, drawn level on a transparent canvas and then tilted."""
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    left, mid = (WORK - length) / 2, WORK / 2
    half = profile(length, radius, nose_share, tail_share, 0.62, 0.24)
    top = [(left + x, mid - r) for x, r in half]
    bottom = [(left + x, mid + r) for x, r in reversed(half)]
    body = top + bottom
    draw.polygon(body, fill=OLIVE)
    # Cel shading: a light band along the upper side and a dark one along the lower, both following the profile.
    draw.polygon([(left + x, mid - r * 0.82) for x, r in half] + [(left + x, mid - r * 0.38) for x, r in reversed(half)],
                 fill=OLIVE_LIGHT)
    draw.polygon([(left + x, mid + r * 0.45) for x, r in half] + [(left + x, mid + r) for x, r in reversed(half)],
                 fill=OLIVE_DARK)
    # Weld seam where the nose meets the body, as a thin line.
    seam = left + length * nose_share
    draw.line((seam, mid - radius + LINE / 2, seam, mid + radius - LINE / 2), fill=OLIVE_DARK, width=LINE // 3)
    # Suspension lugs on the top centre line.
    for share in lugs:
        x = left + length * share
        draw.rounded_rectangle((x - 34, mid - 26, x + 34, mid + 26), radius=12, fill=STEEL, outline=OUTLINE, width=LINE // 2)
        draw.rectangle((x - 12, mid - 10, x + 12, mid + 10), fill=HOLE)
    # The open fuze well at the nose and the open tail with its threaded ring.
    nose_r, tail_r = half[0][1], half[-1][1]
    end = left + length
    draw.rounded_rectangle((end - 60, mid - tail_r, end + 24, mid + tail_r), radius=22, fill=STEEL_DARK, outline=OUTLINE,
                           width=LINE // 2)
    for dx in (-36, -14):
        draw.line((end + dx, mid - tail_r + 22, end + dx, mid + tail_r - 22), fill=STEEL, width=8)
    draw.line(body + [body[0]], fill=OUTLINE, width=LINE, joint='curve')
    # The fuze well's threaded ring, drawn over the outline so the open nose shows.
    ring = nose_r + 22
    draw.ellipse((left - ring * 0.7, mid - ring, left + ring * 0.7, mid + ring), fill=STEEL, outline=OUTLINE, width=LINE // 2)
    draw.ellipse((left - ring * 0.3, mid - ring * 0.45, left + ring * 0.3, mid + ring * 0.45), fill=HOLE)
    return image.rotate(TILT, resample=Image.BICUBIC)


def write(name, what, image):
    """Save the texture at game size and its record beside it."""
    TEXTURES.mkdir(parents=True, exist_ok=True)
    image = image.resize((FINAL, FINAL), Image.LANCZOS)
    path = TEXTURES / f'{name}.png'
    image.save(path, optimize=True)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    (TEXTURES / f'{name}.record.md').write_text(
        f'# {name}\n\n**A stand-in**, drawn with Pillow by `assets/vehicle-patches/draw_standins.py` on {dt.date.today():%d %B %Y}: '
        f'{what}. No image generator, game art or other author\'s art was used. {FINAL} x {FINAL} px, transparent background.\n\n'
        f'**Outstanding:** generated art, on the owner\'s go for GPU work (listed in `docs/development/art-backlog.md`). This file '
        f'is replaced when the owner picks one.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8')
    print('wrote', path.name)


DRAWINGS = {
    'BombCaseMk82': lambda: write('BombCaseMk82', 'an empty Mk. 82 bomb case, seen from above, no fins or band',
                                  bomb_case(780, 100, (0.42, 0.6))),
    'BombCaseMk84': lambda: write('BombCaseMk84', 'an empty Mk. 84 bomb case, seen from above, no fins or band',
                                  bomb_case(900, 160, (0.36, 0.64))),
}

if __name__ == '__main__':
    for chosen in sys.argv[1:] or DRAWINGS:
        DRAWINGS[chosen]()
