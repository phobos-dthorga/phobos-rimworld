"""Draw the bulk bunker's stand-in texture with Pillow, in the game's look: flat fills, a thick near-black outline, seen from directly above.

Run with any Python that has Pillow (the ComfyUI one does):
    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/phobos-vehicles/draw_bunker_standin.py

The bunker is 3 by 4 cells, open at the south end, where a truck backs up and tips in (docs/development/universal-hoppers.md, UH2).
It is drawn on a transparent canvas at twice the size and scaled down, which smooths the edges, so there is no background to clean.
No image generator, game art or other author's art is used. Generated art replaces it on the owner's go; then this script goes.
"""
import datetime as dt
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TARGET = ROOT / 'mods/PhobosVehicles/Textures/Phobos/Vehicles/BulkBunker/BulkBunker.png'
CELL = 256  # final pixels a cell, as the trailer pieces' stand-ins
CELLS = (3, 4)
SCALE = 2  # drawn larger, then scaled down

INK = (30, 30, 32, 255)
FLOOR = (138, 136, 130, 255)
FLOOR_SHADOW = (112, 110, 105, 255)
JOINT = (120, 118, 112, 255)
WALL = (176, 173, 165, 255)
WALL_SHADE = (150, 147, 140, 255)
CAP = (118, 122, 128, 255)
CAP_LIGHT = (150, 154, 160, 255)
YELLOW = (226, 184, 60, 255)
GRATE = (64, 64, 68, 255)


def draw():
    c = CELL * SCALE
    w, h = CELLS[0] * c, CELLS[1] * c
    image = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    line = 9 * SCALE
    edge = 6 * SCALE  # a small margin so the outline is inside the footprint
    wall = int(0.30 * c)
    left, top, right, bottom = edge, edge, w - edge, h - edge

    # The floor slab, with its expansion joints one cell apart and a shadow band inside the walls.
    d.rectangle((left, top, right, bottom), fill=FLOOR)
    d.rectangle((left + wall, top + wall, right - wall, top + wall + int(0.18 * c)), fill=FLOOR_SHADOW)
    d.rectangle((left + wall, top + wall, left + wall + int(0.12 * c), bottom), fill=FLOOR_SHADOW)
    for x in range(1, CELLS[0]):
        d.line((x * c, top + wall, x * c, bottom), fill=JOINT, width=3 * SCALE)
    for y in range(1, CELLS[1]):
        d.line((left + wall, y * c, right - wall, y * c), fill=JOINT, width=3 * SCALE)

    # A drain grate in the floor, near the back wall.
    gx, gy = w // 2, top + wall + int(0.55 * c)
    gw, gh = int(0.55 * c), int(0.22 * c)
    d.rectangle((gx - gw // 2, gy - gh // 2, gx + gw // 2, gy + gh // 2), fill=GRATE, outline=INK, width=line // 2)
    for k in range(1, 6):
        x = gx - gw // 2 + k * gw // 6
        d.line((x, gy - gh // 2 + 6 * SCALE, x, gy + gh // 2 - 6 * SCALE), fill=CAP, width=3 * SCALE)

    # Walls on the north, west and east, each with a steel cap along its inner edge.
    for box in ((left, top, right, top + wall), (left, top, left + wall, bottom), (right - wall, top, right, bottom)):
        d.rectangle(box, fill=WALL)
    d.rectangle((left, top + wall - int(0.08 * c), right, top + wall), fill=WALL_SHADE)
    cap = int(0.07 * c)
    d.rectangle((left + wall - cap, top + wall - cap, right - wall + cap, top + wall), fill=CAP)
    d.rectangle((left + wall - cap, top + wall - cap, left + wall, bottom), fill=CAP)
    d.rectangle((right - wall, top + wall - cap, right - wall + cap, bottom), fill=CAP)
    d.line((left + wall - cap, top + wall - cap, right - wall + cap, top + wall - cap), fill=CAP_LIGHT, width=3 * SCALE)

    # The wheel stop across the open south end: a low steel beam with warning stripes, where the tailgate meets the bunker.
    stop_h = int(0.16 * c)
    sy = bottom - stop_h
    d.rectangle((left + wall, sy, right - wall, bottom), fill=YELLOW)
    stripe = int(0.16 * c)
    x = left + wall - stop_h
    while x < right - wall:
        d.polygon([(x, bottom), (x + stripe // 2, bottom), (x + stripe // 2 + stop_h, sy), (x + stop_h, sy)], fill=INK)
        x += stripe
    # Clear the stripes that ran past the beam's ends.
    d.rectangle((left, sy, left + wall, bottom), fill=WALL)
    d.rectangle((right - wall, sy, right, bottom), fill=WALL)
    d.rectangle((left + wall, sy, right - wall, bottom), outline=INK, width=line // 2)

    # Outlines: the walls' inner edges and the whole footprint.
    d.line((left + wall, top + wall, left + wall, bottom), fill=INK, width=line // 2)
    d.line((right - wall, top + wall, right - wall, bottom), fill=INK, width=line // 2)
    d.line((left + wall, top + wall, right - wall, top + wall), fill=INK, width=line // 2)
    d.rectangle((left, top, right, bottom), outline=INK, width=line)
    return image.resize((CELLS[0] * CELL, CELLS[1] * CELL), Image.LANCZOS)


def main():
    TARGET.parent.mkdir(parents=True, exist_ok=True)
    draw().save(TARGET, optimize=True)
    digest = hashlib.sha256(TARGET.read_bytes()).hexdigest()
    size = (CELLS[0] * CELL, CELLS[1] * CELL)
    TARGET.with_suffix('.record.md').write_text(
        f'# {TARGET.stem}\n\n**A stand-in**, drawn with Pillow by `assets/phobos-vehicles/draw_bunker_standin.py` on {dt.date.today():%d %B %Y}: '
        'the bulk bunker seen from directly above, a concrete bay with steel-capped walls on three sides and a striped wheel stop across '
        'its open south end. No image generator, game art or other author\'s art was used. '
        f'{size[0]} x {size[1]} px, transparent background, drawn on a clear canvas.\n\n'
        '**Outstanding:** generated art, on the owner\'s go (owner, 17 Nov 2026: a stand-in until what is wanted from the image '
        'generator is clearer). This file is replaced when the owner picks one.\n\n'
        f'- `{TARGET.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
    print('wrote', TARGET.relative_to(ROOT), digest[:12])


if __name__ == '__main__':
    main()
