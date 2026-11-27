"""Draw the systems schematic button's icon by script: a tank and an engine block joined by a pipe through a pump.

    python scripts/art/draw_schematic_icon.py                  draw it to the art working folder
    python scripts/art/draw_schematic_icon.py --ship FOLDER    write the shipped icon into FOLDER and its record beside the mirror

Why by script (agent's choice, 28 Nov 2026, for the owner to revise; docs/development/vehicle-schematic.md): the button stands for a flow
sheet of plain symbols, and a drawn set of symbols is exactly that; it needs no graphics card, and the same file comes out every time. A
generated icon from the night run can replace it on the owner's word.

The look follows the game's icons as recorded in the art pipeline, as draw_signal_bars.py does: a thick near-black outline, flat cel
shading (a lit band on the left, a shade band on the right, hard edges), rounded corners and a muted palette. The blocks and pipes come
from icon_shapes.py (Framework 0.365.0, shared with the intake separator's icon); the pump with its arrow is this icon's own.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402
import icon_shapes as shapes  # noqa: E402

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


def pump(d: ImageDraw.ImageDraw):
    """The pump: a round part with an arrow pointing the way the pipe flows, up."""
    shapes.disc(d, PUMP_CENTRE, PUMP_RADIUS, PUMP)
    s = shapes.SCALE
    cx, cy = PUMP_CENTRE[0] * s, PUMP_CENTRE[1] * s
    a = 6 * s
    d.polygon([(cx, cy - a), (cx + a, cy + a // 2), (cx - a, cy + a // 2)], fill=shapes.OUTLINE)


def draw():
    big, d = shapes.canvas()
    shapes.pipe(d, PIPE_DOWN, PIPE, PIPE_WIDTH)
    shapes.pipe(d, PIPE_PATH, PIPE, PIPE_WIDTH)
    shapes.block(d, TANK_BOX, TANK)
    shapes.block(d, ENGINE_BOX, ENGINE)
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
    shapes.ship(draw(), art_paths.work('schematic-icon'), 'Schematic', art_paths, RECORD, args.ship)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
