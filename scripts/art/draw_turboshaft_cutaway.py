"""Draw the systems schematic's turboshaft cutaway by script: a gas turbine in section, intake to exhaust, cut into three tiles.

    python scripts/art/draw_turboshaft_cutaway.py                  draw it to the art working folder, with a tinted preview
    python scripts/art/draw_turboshaft_cutaway.py --ship FOLDER    write each tile's layers into FOLDER/<Tile> and a record for each

Why by script (docs/development/vehicle-schematic.md, SC2, SC7 and SC8; the owner's word of 28 Nov 2026, "Let's proceed with the still not
built"): as the diesel's, every region comes from one list of shapes, so the layers fit exactly and the window tints each by a reading.
The aircraft page keeps three parts for the engine (compressor, hot section, turbine), each with its own reading, so the section is drawn
whole and cut into three tiles of the same proportions as those parts (80, 100 and 80 drawing units wide, 80 high), which then meet with
no seam. The gas path is cut into bands, each its own layer, so the window can colour each by the gas's heat at its place along the path
(the heat gradient, SC8). A plain section of a gas turbine with an axial compressor and an annular combustor, not a drawing of any real
engine.

Shared drawing lives in cutaway_shapes.py. The whole section is 832 x 256 (tiles 256, 320 and 256 wide): heights a power of two, as the art
rules ask.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402
import cutaway_shapes as shapes  # noqa: E402

W, H = 832, 256
CY = 128
# Short folder names: the repository's records sit deep enough that Windows refuses a longer path.
TILES = {'Comp': (0, 256), 'Hot': (256, 576), 'Turb': (576, 832)}

# The gas path's outer and inner edge (distance from the centreline) at stations along the engine: the annulus narrows through the
# compressor, swells round the burners, and opens through the turbine to the exhaust.
OUTER = [(0, 100), (256, 66), (300, 86), (540, 86), (576, 66), (832, 104)]
INNER = [(0, 26), (256, 46), (576, 46), (832, 30)]
CASING = 14


edge = shapes.edge


def band(x0, x1, outer=OUTER, inner=INNER):
    """The gas path between two stations, above and below the centreline, as two polygons."""
    xs = [x0 + (x1 - x0) * i / 8 for i in range(9)]
    top = [(x, CY - edge(outer, x)) for x in xs] + [(x, CY - edge(inner, x)) for x in reversed(xs)]
    bottom = [(x, CY + edge(outer, x)) for x in xs] + [(x, CY + edge(inner, x)) for x in reversed(xs)]
    return [('poly', top), ('poly', bottom)]


def casing():
    xs = [i * W / 32 for i in range(33)]
    top = [(x, CY - edge(OUTER, x) - CASING) for x in xs] + [(x, CY - edge(OUTER, x)) for x in reversed(xs)]
    bottom = [(x, CY + edge(OUTER, x) + CASING) for x in xs] + [(x, CY + edge(OUTER, x)) for x in reversed(xs)]
    return [('poly', top), ('poly', bottom)]


def blades(xs, width):
    out = []
    for x in xs:
        out.append(('poly', [(x, CY - edge(OUTER, x) + 2), (x + width, CY - edge(OUTER, x + width) + 2),
                             (x + width, CY - edge(INNER, x + width)), (x, CY - edge(INNER, x))]))
        out.append(('poly', [(x, CY + edge(INNER, x)), (x + width, CY + edge(INNER, x + width)),
                             (x + width, CY + edge(OUTER, x + width) - 2), (x, CY + edge(OUTER, x) - 2)]))
    return out


def hub():
    xs = [i * W / 32 for i in range(33)]
    return [('poly', [(x, CY - edge(INNER, x)) for x in xs] + [(x, CY + edge(INNER, x)) for x in reversed(xs)])]


# Heat bands along the gas path: (name, x from, x to, place along the path for the window's heat tint).
BANDS = [
    ('Heat01', 0, 64, 0.05), ('Heat02', 64, 128, 0.15), ('Heat03', 128, 192, 0.25), ('Heat04', 192, 256, 0.35),
    ('Heat05', 256, 416, 0.45), ('Heat06', 416, 576, 0.55),
    ('Heat07', 576, 640, 0.65), ('Heat08', 640, 704, 0.75), ('Heat09', 704, 768, 0.85), ('Heat10', 768, 832, 0.95),
]

REGIONS: dict[str, list] = {'Casing': casing()}
for name, x0, x1, _ in BANDS:
    REGIONS[name] = band(x0, x1)
REGIONS.update({
    # The rotor: the hub and shaft down the middle, and the blade rows of the compressor and the turbine.
    'Steel': hub() + blades([24, 64, 104, 144, 184, 222], 7) + blades([600, 650, 700, 750], 10) + [
        # The combustor's liner walls and its nozzle guide vanes.
        ('line', [(300, CY - 80), (540, CY - 80)], 4), ('line', [(300, CY - 52), (540, CY - 52)], 4),
        ('line', [(300, CY + 80), (540, CY + 80)], 4), ('line', [(300, CY + 52), (540, CY + 52)], 4),
    ],
    # The fuel: down through the casing to a nozzle at the head of each burner.
    'Fuel': [('line', [(312, 0), (312, CY - 66)], 6), ('ellipse', (302, CY - 74, 322, CY - 58)),
             ('line', [(312, H), (312, CY + 66)], 6), ('ellipse', (302, CY + 58, 322, CY + 74))],
    # The oil: the front and rear bearings on the shaft.
    'Oil': [('ellipse', (244, CY - 14, 268, CY + 14)), ('ellipse', (580, CY - 14, 604, CY + 14))],
})

PREVIEW = {'Casing': (110, 190, 120), 'Steel': (170, 176, 182), 'Fuel': (204, 184, 128), 'Oil': (148, 97, 56)}
_ramp = [(76, 133, 242), (120, 170, 232), (158, 199, 224), (190, 200, 180), (250, 140, 46), (250, 140, 46),
         (245, 180, 80), (240, 200, 110), (230, 205, 140), (210, 200, 170)]
for (name, *_), colour in zip(BANDS, _ramp):
    PREVIEW[name] = colour

RECORD = """# {name}

**Drawn by script, no image generator** (28 Nov 2026), by `scripts/art/draw_turboshaft_cutaway.py`: the "{name}" layer of one tile of the
systems schematic's turboshaft cutaway (a gas turbine in section, intake to exhaust, cut into three tiles that meet with no seam), drawn at
four times and shrunk with Lanczos. White with light grey shading on a clear ground, which the schematic window tints by a live reading (a
gas-path band by the gas's heat there); the outline layer is near-black. Every layer is drawn from one list of shapes, so the layers fit
exactly (docs/development/vehicle-schematic.md, SC7 and SC8). A plain section, not a drawing of any real engine; no game or other author's
art was used. Running the script again draws the same file.

- `{name}.png`: SHA-256 `{sha}`
"""


def draw() -> dict[str, Image.Image]:
    return shapes.draw(REGIONS, W, H, outline_as_one=tuple(name for name, *_ in BANDS))


def tiles(layers: dict[str, Image.Image]) -> dict[str, dict[str, Image.Image]]:
    """Each tile's crop of every layer."""
    return {tile: {name: img.crop((x0, 0, x1, H)) for name, img in layers.items()} for tile, (x0, x1) in TILES.items()}


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--ship', help='write each tile\'s layers into this folder\'s <Tile> folder, and a record for each')
    args = parser.parse_args(argv)
    layers = draw()
    folder = art_paths.work('turboshaft-cutaway')
    folder.mkdir(parents=True, exist_ok=True)
    shapes.preview(layers, PREVIEW, W, H).save(folder / 'preview.png')
    print(folder / 'preview.png')
    if args.ship:
        for tile, cut in tiles(layers).items():
            for target in shapes.ship(cut, Path(args.ship) / tile, art_paths.record_path, RECORD):
                print(target)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
