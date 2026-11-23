"""Draw D'thorga Vehicle Patches' stand-in item textures with Pillow, in the game's look: a thick near-black outline and flat cel
shading. No image generator and no game or other author's art is involved.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/vehicle-patches/draw_standins.py [name ...]

Name drawings to draw only those. Each texture goes into the mod with a record saying it is a stand-in; generated art replaces it
once the owner picks one (owner's direction, 7 Nov 2026), and then its entry here is removed so this can't overwrite the pick.

The bomb cases are empty Mk. 82 and Mk. 84 bodies seen from above: olive-drab steel, an ogive nose with the fuze well open, an open
tail with its threaded ring, and two suspension lugs on the top. No fins, which are fitted with the fill, and no yellow band: on US
bombs a yellow band marks an explosive filling, and an empty case has none. Proportions are simplified to fit a square icon.

The Mk. 84 fills (0.20.0) are canisters seen from the side, tilted like the cases, with a coloured band after the US ordnance colour
code as a hint at what is inside (light red for incendiary, light green for smoke, grey with a dark green band for a toxic fill: the
code is MIL-STD-709, read from secondary sources, approximate). The EMP coil pack is a steel frame holding copper coils.
"""
import datetime as dt
import hashlib
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'scripts' / 'art'))
from art_paths import record_path  # noqa: E402  (records of shipped files are kept under assets/records/)
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
RED_BAND = (204, 96, 84, 255)
GREEN_BAND = (126, 184, 112, 255)
GREY = (138, 142, 140, 255)
GREY_LIGHT = (168, 172, 170, 255)
GREY_DARK = (104, 108, 106, 255)
DARK_GREEN_BAND = (58, 104, 64, 255)
COPPER = (184, 112, 64, 255)
COPPER_LIGHT = (214, 146, 92, 255)


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


def canister(body, light, dark, band):
    """A sealed fill canister, level and then tilted: a rounded steel body, a coloured band, a filler cap and a lifting handle."""
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    left, right, top, bottom = 230, 794, 330, 694
    draw.rounded_rectangle((left, top, right, bottom), radius=120, fill=body)
    draw.rounded_rectangle((left + 30, top + 40, right - 30, top + 120), radius=40, fill=light)
    draw.rounded_rectangle((left + 30, bottom - 110, right - 30, bottom - 30), radius=40, fill=dark)
    draw.rectangle((450, top + 6, 540, bottom - 6), fill=band)
    for x in (330, 690):
        draw.line((x, top + 20, x, bottom - 20), fill=dark, width=LINE // 3)
    draw.rounded_rectangle((left, top, right, bottom), radius=120, outline=OUTLINE, width=LINE)
    draw.line((450, top + 4, 450, bottom - 4), fill=OUTLINE, width=LINE // 2)
    draw.line((540, top + 4, 540, bottom - 4), fill=OUTLINE, width=LINE // 2)
    draw.rounded_rectangle((right - 18, 450, right + 70, 574), radius=24, fill=STEEL, outline=OUTLINE, width=LINE // 2)
    draw.arc((570, top - 70, 750, top + 110), 200, 340, fill=OUTLINE, width=LINE)
    return image.rotate(TILT, resample=Image.BICUBIC)


def coil_pack():
    """An EMP coil pack: a steel frame with three copper coils, level and then tilted."""
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((200, 330, 824, 694), radius=48, fill=STEEL_DARK, outline=OUTLINE, width=LINE)
    for x in (250, 430, 610):
        draw.rounded_rectangle((x, 370, x + 164, 654), radius=60, fill=COPPER, outline=OUTLINE, width=LINE // 2)
        for y in range(410, 640, 46):
            draw.line((x + 20, y, x + 144, y), fill=COPPER_LIGHT, width=12)
        draw.ellipse((x + 52, 482, x + 112, 542), fill=HOLE, outline=OUTLINE, width=LINE // 3)
    draw.rounded_rectangle((790, 470, 880, 554), radius=18, fill=STEEL, outline=OUTLINE, width=LINE // 2)
    return image.rotate(TILT, resample=Image.BICUBIC)


def write(name, what, image):
    """Save the texture at game size and its record beside it."""
    TEXTURES.mkdir(parents=True, exist_ok=True)
    image = image.resize((FINAL, FINAL), Image.LANCZOS)
    path = TEXTURES / f'{name}.png'
    image.save(path, optimize=True)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    record_path(TEXTURES / f'{name}.record.md').write_text(
        f'# {name}\n\n**A stand-in**, drawn with Pillow by `assets/vehicle-patches/draw_standins.py` on {dt.date.today():%d %B %Y}: '
        f'{what}. No image generator, game art or other author\'s art was used. {FINAL} x {FINAL} px, transparent background.\n\n'
        f'**Outstanding:** generated art, on the owner\'s go for GPU work (listed in `docs/development/art-backlog.md`). This file '
        f'is replaced when the owner picks one.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8')
    print('wrote', path.name)


def reference(name, what, image):
    """The same drawing at full size, as a layout reference for generated art; references are not tracked on main."""
    folder = Path(__file__).resolve().parent / 'references'
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / f'{name}-large.png'
    image.save(path, optimize=True)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    record_path(path.with_suffix('.record.md')).write_text(
        f'# {name}, large\n\nA {WORK} x {WORK} px drawing of {what}, from `assets/vehicle-patches/draw_standins.py`, {dt.date.today():%d %B %Y}. '
        f'No image generator, game art or other author\'s art was used. SHA-256 `{digest}`. A layout reference; it does not ship.\n', encoding='utf-8')
    print('wrote', path.name)


DRAWINGS = {
    'BombCaseMk82': lambda: write('BombCaseMk82', 'an empty Mk. 82 bomb case, seen from above, no fins or band',
                                  bomb_case(780, 100, (0.42, 0.6))),
    'BombCaseMk84': lambda: write('BombCaseMk84', 'an empty Mk. 84 bomb case, seen from above, no fins or band',
                                  bomb_case(900, 160, (0.36, 0.64))),
    'BombFillIncendiary': lambda: write('BombFillIncendiary', 'a sealed olive canister with a light red band, seen from the side',
                                        canister(OLIVE, OLIVE_LIGHT, OLIVE_DARK, RED_BAND)),
    'BombFillSmoke': lambda: write('BombFillSmoke', 'a sealed olive canister with a light green band, seen from the side',
                                   canister(OLIVE, OLIVE_LIGHT, OLIVE_DARK, GREEN_BAND)),
    'BombFillToxGas': lambda: write('BombFillToxGas', 'a sealed grey canister with a dark green band, seen from the side',
                                    canister(GREY, GREY_LIGHT, GREY_DARK, DARK_GREEN_BAND)),
    'BombCoilEMP': lambda: write('BombCoilEMP', 'a steel frame holding three copper coils', coil_pack()),
    'References': lambda: (reference('BombCaseMk82', 'an empty Mk. 82 bomb case, seen from above', bomb_case(780, 100, (0.42, 0.6))),
                           reference('BombCaseMk84', 'an empty Mk. 84 bomb case, seen from above', bomb_case(900, 160, (0.36, 0.64)))),
}

if __name__ == '__main__':
    for chosen in sys.argv[1:] or DRAWINGS:
        DRAWINGS[chosen]()
