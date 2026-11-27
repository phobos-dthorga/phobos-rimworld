"""Draw the systems schematic's diesel cutaway by script: one cylinder of a diesel engine in section, as white region layers and an outline.

    python scripts/art/draw_diesel_cutaway.py                  draw it to the art working folder, with a tinted preview
    python scripts/art/draw_diesel_cutaway.py --ship FOLDER    write the shipped layers into FOLDER and a record for each beside the mirror

Why by script (docs/development/vehicle-schematic.md, SC2 and SC7; the owner's card of 28 Nov 2026, "The first cutaway"): an image
generator draws a section with nonsense inside, and its regions would never match a mask. Here every region is drawn from one list of
shapes, so the layers fit each other exactly, and the window tints each by a live reading (the record's "white layers" route). It is a
plain section of an inline diesel's cylinder, not a drawing of any real engine.

Each layer is white with light grey shading (the window multiplies it by its tint, so the shading stays) on a clear ground; the outline
is near-black, drawn round every region from the regions themselves (cutaway_shapes.py, shared with the turboshaft's). Drawn at four
times and shrunk with Lanczos. The canvas is 448 x 256: its height a power of two, as the art rules ask.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402
import cutaway_shapes as shapes  # noqa: E402

W, H = 448, 256

# Each region: its shapes in shipped pixels, drawn in this order; a later region covers an earlier one where they meet. A shape is
# ('rect', box), ('poly', points), ('ellipse', box) or ('line', points, width).
REGIONS: dict[str, list] = {
    # The block and head, the crankcase walls: everything solid that is not a moving part.
    'Casing': [
        ('poly', [(140, 64), (308, 64), (308, 176), (330, 222), (118, 222), (140, 176)]),
        ('rect', (132, 22, 316, 66)),
        ('rect', (108, 222, 340, 250)),
    ],
    # The water jacket either side of the bore and over the head.
    'Jacket': [
        ('rect', (156, 74, 176, 170)),
        ('rect', (272, 74, 292, 170)),
        ('rect', (156, 30, 292, 40)),
        ('rect', (156, 30, 166, 74)),
        ('rect', (282, 30, 292, 74)),
    ],
    # The oil: the sump pan, and the gallery up the crankcase wall to the main bearing.
    'Oil': [
        ('rect', (116, 230, 332, 246)),
        ('line', [(304, 232), (304, 200), (248, 200)], 7),
    ],
    # The combustion chamber above the piston.
    'Chamber': [
        ('rect', (182, 70, 266, 116)),
    ],
    # The moving steel: the piston, its rod, the crankshaft and its pin, and the two valves.
    'Steel': [
        ('rect', (184, 116, 264, 148)),
        ('line', [(224, 140), (238, 196)], 12),
        ('ellipse', (200, 178, 248, 226)),
        ('ellipse', (230, 186, 248, 204)),
        ('line', [(196, 24), (196, 66)], 5),
        ('poly', [(184, 66), (208, 66), (204, 72), (188, 72)]),
        ('line', [(252, 24), (252, 66)], 5),
        ('poly', [(240, 66), (264, 66), (260, 72), (244, 72)]),
    ],
    # The intake: the manifold from the left, down the port to its valve.
    'Intake': [
        ('poly', [(0, 34), (150, 34), (186, 58), (186, 66), (172, 66), (146, 52), (0, 52)]),
    ],
    # The exhaust: up from its valve, out through the manifold to the right.
    'Exhaust': [
        ('poly', [(448, 34), (298, 34), (262, 58), (262, 66), (276, 66), (302, 52), (448, 52)]),
    ],
    # The fuel: the injector in the head, and its line over the top and down the side to where the page's fuel pipe meets the picture.
    'Fuel': [
        ('rect', (219, 8, 229, 70)),
        ('line', [(224, 10), (430, 10), (430, 233), (448, 233)], 6),
    ],
}

# The preview's tints only: the window takes each layer's colour from the page (Schematics.xml).
PREVIEW = {
    'Casing': (110, 190, 120), 'Jacket': (74, 158, 242), 'Oil': (148, 97, 56), 'Chamber': (168, 158, 168),
    'Steel': (170, 176, 182), 'Intake': (158, 224, 245), 'Exhaust': (168, 158, 168), 'Fuel': (204, 184, 128),
}

RECORD = """# {name}

**Drawn by script, no image generator** (28 Nov 2026), by `scripts/art/draw_diesel_cutaway.py`: the "{name}" layer of the systems schematic's
diesel cutaway (one cylinder of an inline diesel in section), drawn at four times and shrunk to 448 x 256 px with Lanczos. White with light
grey shading on a clear ground, which the schematic window tints by a live reading; the outline layer is near-black. Every layer is drawn from
one list of shapes, so the layers fit exactly (docs/development/vehicle-schematic.md, SC7). A plain section, not a drawing of any real
engine; no game or other author's art was used. Running the script again draws the same file.

- `{name}.png`: SHA-256 `{sha}`
"""


def draw() -> dict:
    return shapes.draw(REGIONS, W, H)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--ship', help='write the layers into this folder, and a record for each beside the mirror')
    args = parser.parse_args(argv)
    layers = draw()
    folder = art_paths.work('diesel-cutaway')
    folder.mkdir(parents=True, exist_ok=True)
    for name, img in layers.items():
        img.save(folder / f'{name}.png')
    shapes.preview(layers, PREVIEW, W, H).resize((W * 2, H * 2), Image.NEAREST).save(folder / 'preview.png')
    print(folder / 'preview.png')
    if args.ship:
        for target in shapes.ship(layers, Path(args.ship), art_paths.record_path, RECORD):
            print(target)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
