"""Draw the bulk-handling buildings' stand-in textures with Pillow, in the game's look: flat fills, a thick near-black outline, seen
from directly above (docs/development/universal-hoppers.md).

Run with any Python that has Pillow (the ComfyUI one does):
    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/phobos-vehicles/draw_bulk_standins.py [name ...]

- BulkBunker (UH2, Vehicles 0.63.0): 3 by 4 cells, open at the south end, where a truck backs up and tips in.
- LoadingChute (UH13, Vehicles 0.64.0): one cell, a steel funnel whose spout is on its south edge, the side a vehicle parks across.

Each is drawn on a transparent canvas at twice the size and scaled down, which smooths the edges, so there is no background to clean.
No image generator, game art or other author's art is used. Generated art replaces each on the owner's go; then its entry here goes, so
this script cannot overwrite the pick (the owner chose the OpenAI image API for the bunker, 17 Nov 2026).
"""
import datetime as dt
import hashlib
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods/PhobosVehicles/Textures/Phobos/Vehicles'
CELL = 256  # final pixels a cell, as the trailer pieces' stand-ins
SCALE = 2  # drawn larger, then scaled down

INK = (30, 30, 32, 255)
FLOOR = (138, 136, 130, 255)
FLOOR_SHADOW = (112, 110, 105, 255)
JOINT = (120, 118, 112, 255)
WALL = (176, 173, 165, 255)
WALL_SHADE = (150, 147, 140, 255)
CAP = (118, 122, 128, 255)
CAP_LIGHT = (150, 154, 160, 255)
STEEL = (160, 164, 170, 255)
STEEL_DARK = (96, 100, 106, 255)
YELLOW = (226, 184, 60, 255)
GRATE = (64, 64, 68, 255)


def stripes(d, box, height):
    """Warning stripes across a beam: yellow, with dark bars leaning right, kept inside the beam."""
    left, top, right, bottom = box
    beam = Image.new('RGBA', (right - left, bottom - top), YELLOW)
    b = ImageDraw.Draw(beam)
    w, h = beam.size
    x = -height
    while x < w:
        b.polygon([(x, h), (x + height // 2, h), (x + height // 2 + height, 0), (x + height, 0)], fill=INK)
        x += height
    d._image.paste(beam, (left, top))


def bunker():
    cells = (3, 4)
    c = CELL * SCALE
    w, h = cells[0] * c, cells[1] * c
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
    for x in range(1, cells[0]):
        d.line((x * c, top + wall, x * c, bottom), fill=JOINT, width=3 * SCALE)
    for y in range(1, cells[1]):
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
    stripes(d, (left + wall, sy, right - wall, bottom), stop_h)
    # Clear the stripes that ran past the beam's ends.
    d.rectangle((left, sy, left + wall, bottom), fill=WALL)
    d.rectangle((right - wall, sy, right, bottom), fill=WALL)
    d.rectangle((left + wall, sy, right - wall, bottom), outline=INK, width=line // 2)

    # Outlines: the walls' inner edges and the whole footprint.
    d.line((left + wall, top + wall, left + wall, bottom), fill=INK, width=line // 2)
    d.line((right - wall, top + wall, right - wall, bottom), fill=INK, width=line // 2)
    d.line((left + wall, top + wall, right - wall, top + wall), fill=INK, width=line // 2)
    d.rectangle((left, top, right, bottom), outline=INK, width=line)
    return image.resize((cells[0] * CELL, cells[1] * CELL), Image.LANCZOS)


def chute():
    c = CELL * SCALE
    image = Image.new('RGBA', (c, c), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)
    line = 9 * SCALE
    edge = 10 * SCALE
    left, top, right, bottom = edge, edge, c - edge, c - edge
    rim = int(0.12 * c)

    # The square steel rim of the funnel's mouth.
    d.rectangle((left, top, right, bottom - int(0.16 * c)), fill=STEEL, outline=INK, width=line)
    # The funnel's sloping sides, darker towards the spout, seen from above.
    slot_w = int(0.40 * c)
    slot_top = bottom - int(0.30 * c)
    mid = c // 2
    inner = (left + rim, top + rim, right - rim, bottom - int(0.16 * c) - rim // 2)
    d.polygon([(inner[0], inner[1]), (inner[2], inner[1]), (mid + slot_w // 2, slot_top), (mid - slot_w // 2, slot_top)], fill=CAP_LIGHT)
    d.polygon([(inner[0], inner[1]), (mid - slot_w // 2, slot_top), (mid - slot_w // 2, inner[3]), (inner[0], inner[3])], fill=CAP)
    d.polygon([(inner[2], inner[1]), (inner[2], inner[3]), (mid + slot_w // 2, inner[3]), (mid + slot_w // 2, slot_top)], fill=STEEL_DARK)
    d.rectangle(inner, outline=INK, width=line // 2)
    # The spout: a dark opening running out over the south edge, with a striped lip where the vehicle parks across it.
    d.rectangle((mid - slot_w // 2, slot_top, mid + slot_w // 2, bottom), fill=GRATE, outline=INK, width=line // 2)
    lip_h = int(0.09 * c)
    stripes(d, (mid - slot_w // 2, bottom - lip_h, mid + slot_w // 2, bottom), lip_h)
    d.rectangle((mid - slot_w // 2, bottom - lip_h, mid + slot_w // 2, bottom), outline=INK, width=line // 2)
    for x in (left + rim // 2, right - rim // 2):
        for y in (top + rim // 2, bottom - int(0.16 * c) - rim // 2):
            d.ellipse((x - 9 * SCALE, y - 9 * SCALE, x + 9 * SCALE, y + 9 * SCALE), fill=STEEL_DARK, outline=INK, width=3 * SCALE)
    return image.resize((CELL, CELL), Image.LANCZOS)


DRAWINGS = {
    'BulkBunker': ('BulkBunker/BulkBunker', bunker,
                   'the bulk bunker seen from directly above, a concrete bay with steel-capped walls on three sides and a striped wheel '
                   'stop across its open south end',
                   'the OpenAI image API (owner, 17 Nov 2026); request `assets/phobos-vehicles/requests/bulk-bunker-pilot.json`'),
    'LoadingChute': ('LoadingChute/LoadingChute', chute,
                     'the loading chute seen from directly above, a square steel funnel narrowing to a spout over its south edge, with a '
                     'striped lip',
                     'not yet planned; on the owner\'s go'),
}


def write(name):
    relative, draw, what, route = DRAWINGS[name]
    target = TEXTURES / (relative + '.png')
    target.parent.mkdir(parents=True, exist_ok=True)
    image = draw()
    image.save(target, optimize=True)
    digest = hashlib.sha256(target.read_bytes()).hexdigest()
    target.with_suffix('.record.md').write_text(
        f'# {target.stem}\n\n**A stand-in**, drawn with Pillow by `assets/phobos-vehicles/draw_bulk_standins.py` on {dt.date.today():%d %B %Y}: '
        f'{what}. No image generator, game art or other author\'s art was used. '
        f'{image.width} x {image.height} px, transparent background, drawn on a clear canvas.\n\n'
        f'**Outstanding:** generated art, by {route}. This file is replaced when the owner picks one.\n\n'
        f'- `{target.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
    print('wrote', target.relative_to(ROOT), digest[:12])


if __name__ == '__main__':
    for chosen in sys.argv[1:] or DRAWINGS:
        write(chosen)
