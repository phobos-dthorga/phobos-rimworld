"""Draw the intake separator switch's icon by script: an engine intake whose air turns in, while grit flies straight on and out.

    python scripts/art/draw_separator_icon.py                  draw it to the art working folder
    python scripts/art/draw_separator_icon.py --ship FOLDER    write the shipped icon into FOLDER and its record beside the mirror

Why by script (agent's choice, 28 Nov 2026, for the owner to revise; docs/development/vehicle-inlet-separator.md): it matches the
systems schematic's button, drawn the same way, needs no graphics card, and draws the same file every time. Shapes come from
icon_shapes.py, which draw_schematic_icon.py shares.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402
import icon_shapes as shapes  # noqa: E402

DUCT = (132, 136, 140)    # steel: the intake
AIR = (120, 196, 220)     # a muted sky blue: the air that turns in
GRIT = (150, 112, 72)     # a muted brown: the grit thrown out

RECORD = """# IntakeSeparator

**Drawn by script, no image generator** (28 Nov 2026), by `scripts/art/draw_separator_icon.py`: the intake separator switch on a turbine
aircraft's Dashboard, an intake whose air turns in while grit flies straight on and out, drawn at eight times and shrunk to 128 x 128 px
with Lanczos. Drawn by script as the agent's choice, for the owner to revise, to match the systems schematic's button
(docs/development/vehicle-inlet-separator.md). The look follows vanilla's icons (thick near-black outline, flat two-tone shading, rounded
corners). No game or other author's art was used; running the script again draws the same file.

- `IntakeSeparator.png`: SHA-256 `{sha}`
"""


def draw():
    big, d = shapes.canvas()
    # The intake: an upper and a lower lip, the mouth open to the left.
    shapes.block(d, (44, 14, 118, 40), DUCT)
    shapes.block(d, (44, 88, 118, 114), DUCT)
    # The air: in at the mouth, turned up and into the engine behind the upper lip.
    shapes.pipe(d, [(10, 64), (58, 64), (78, 52), (98, 52)], AIR, width=9)
    shapes.arrowhead(d, (118, 52), (1, 0), 14, AIR)
    # The grit: heavier, it carries straight on and drops out of the bypass below.
    for centre in ((70, 74), (86, 80), (102, 86)):
        shapes.disc(d, centre, 7, GRIT, lit=False)
    return big


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--ship', help='write the icon into this folder, and its record beside the mirror')
    args = parser.parse_args(argv)
    shapes.ship(draw(), art_paths.work('separator-icon'), 'IntakeSeparator', art_paths, RECORD, args.ship)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
