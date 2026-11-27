"""Draw the systems schematic button's icon by script: a tank and an engine block joined by a pipe through a pump.

    python scripts/art/draw_schematic_icon.py                  draw it to the art working folder, with a card
    python scripts/art/draw_schematic_icon.py --ship FOLDER    write the shipped icon into FOLDER and its record beside the mirror

Why by script (agent's choice, 28 Nov 2026, for the owner to revise; docs/development/vehicle-schematic.md): the button stands for a flow
sheet of plain symbols, and a drawn set of symbols is exactly that; it needs no graphics card, and the same file comes out every time. A
generated icon from the night run can replace it on the owner's word.

The look follows the game's icons as recorded in the art pipeline, as draw_signal_bars.py does: a thick near-black outline, flat cel
shading (a lit band on the left, a shade band on the right, hard edges), rounded corners and a muted palette. Drawn at eight times its
size and shrunk, so its edges are smooth with no stray pixels. Game-free: a next game passes its own size, outline and colours.
"""
from __future__ import annotations

import argparse
import hashlib
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402

SIZE = 128
SCALE = 8
OUTLINE = (24, 24, 26, 255)
OUTLINE_PX = 5
RADIUS_PX = 6

TANK = (92, 132, 168)      # a muted blue: the store
ENGINE = (132, 136, 140)   # steel: the engine block
PIPE = (196, 164, 96)      # a muted tan: what flows
PUMP = (112, 164, 84)      # the game's muted green: a working part

# At shipped size: (left, top, right, bottom).
TANK_BOX = (10, 70, 50, 116)
ENGINE_BOX = (66, 14, 118, 66)
PIPE_PATH = [(30, 70), (30, 40), (66, 40)]       # up out of the tank, across into the engine
PIPE_DOWN = [(92, 66), (92, 92), (50, 92)]        # back down from the engine to the tank
PIPE_WIDTH = 10
PUMP_CENTRE = (30, 52)
PUMP_RADIUS = 13


def shade(colour, amount):
    return tuple(max(0, min(255, c + amount)) for c in colour[:3]) + (255,)


def block(d: ImageDraw.ImageDraw, box, fill):
    """A rounded block with its outline, a lit band on the left and a shade band on the right."""
    s = SCALE
    outer = tuple(v * s for v in box)
    d.rounded_rectangle(outer, radius=RADIUS_PX * s, fill=OUTLINE)
    o = OUTLINE_PX * s
    inner = (outer[0] + o, outer[1] + o, outer[2] - o, outer[3] - o)
    r = max(1, (RADIUS_PX - OUTLINE_PX // 2) * s)
    d.rounded_rectangle(inner, radius=r, fill=shade(fill, 0))
    width = inner[2] - inner[0]
    d.rectangle((inner[0], inner[1] + r, inner[0] + int(width * 0.24), inner[3] - r), fill=shade(fill, 30))
    d.rectangle((inner[2] - int(width * 0.2), inner[1] + r, inner[2], inner[3] - r), fill=shade(fill, -26))


def pipe(d: ImageDraw.ImageDraw, path, fill):
    """A pipe: the outline drawn wide first, then the fill inside it, with round joints."""
    s = SCALE
    points = [(x * s, y * s) for x, y in path]
    for width, colour in ((PIPE_WIDTH + OUTLINE_PX * 2, OUTLINE), (PIPE_WIDTH, shade(fill, 0))):
        d.line(points, fill=colour, width=width * s, joint='curve')
        for x, y in points:
            rad = width * s // 2
            d.ellipse((x - rad, y - rad, x + rad, y + rad), fill=colour)


def pump(d: ImageDraw.ImageDraw):
    s = SCALE
    cx, cy = PUMP_CENTRE[0] * s, PUMP_CENTRE[1] * s
    r = PUMP_RADIUS * s
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=OUTLINE)
    ri = (PUMP_RADIUS - OUTLINE_PX + 1) * s
    d.ellipse((cx - ri, cy - ri, cx + ri, cy + ri), fill=shade(PUMP, 0))
    d.pieslice((cx - ri, cy - ri, cx + ri, cy + ri), 110, 250, fill=shade(PUMP, 30))
    # An arrow pointing the way the pipe flows: up.
    a = 6 * s
    d.polygon([(cx, cy - a), (cx + a, cy + a // 2), (cx - a, cy + a // 2)], fill=OUTLINE)


def draw() -> Image.Image:
    big = Image.new('RGBA', (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    pipe(d, PIPE_DOWN, PIPE)
    pipe(d, PIPE_PATH, PIPE)
    block(d, TANK_BOX, TANK)
    block(d, ENGINE_BOX, ENGINE)
    pump(d)
    return big


RECORD = """# Schematic

**Drawn by script, no image generator** (28 Nov 2026), by `scripts/art/draw_schematic_icon.py`: the systems schematic's button on the
vehicle Dashboard, a tank and an engine block joined by a pipe through a pump, drawn at eight times and shrunk to 128 x 128 px with
Lanczos. Drawn by script as the agent's choice, for the owner to revise: the button stands for a flow sheet of plain symbols
(docs/development/vehicle-schematic.md). The look follows vanilla's icons (thick near-black outline, flat two-tone shading, rounded
corners). No game or other author's art was used; running the script again draws the same file.

- `Schematic.png`: SHA-256 `{sha}`
"""


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--ship', help='write the icon into this folder, and its record beside the mirror')
    args = parser.parse_args(argv)
    folder = art_paths.work('schematic-icon')
    folder.mkdir(parents=True, exist_ok=True)
    big = draw()
    big.save(folder / 'Schematic-master.png')
    small = big.resize((SIZE, SIZE), Image.LANCZOS)
    small.save(folder / 'Schematic.png')
    print(folder / 'Schematic.png')
    if args.ship:
        target = Path(args.ship) / 'Schematic.png'
        small.save(target)
        sha = hashlib.sha256(target.read_bytes()).hexdigest()
        record = art_paths.record_path(target.with_name('Schematic.record.md'))
        record.write_text(RECORD.format(sha=sha), encoding='utf-8', newline='\n')
        print(target)
        print(record)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
