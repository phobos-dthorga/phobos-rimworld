"""Draw the systems schematic's turbofan cutaway by script: a bypass jet engine in section, fan to nozzle, cut into three tiles.

    python scripts/art/draw_turbofan_cutaway.py                  draw it to the art working folder, with a tinted preview
    python scripts/art/draw_turbofan_cutaway.py --ship FOLDER    write each tile's layers into FOLDER/<Tile> and a record for each

Why by script (docs/development/vehicle-schematic.md, SC2 and SC8; the owner's word of 28 Nov 2026, "Let's finish the schematic then"): a
jet's page showed the turboshaft's section until Framework 0.382.0. As the turboshaft's, every region comes from one list of shapes and the
section is cut into three tiles of the aircraft page's engine parts (compressor 80, hot section 100, turbine 80 drawing units wide), so the
same page serves both and only the pictures differ. A fan at the front feeds a cold bypass duct round the core, and the core's gas path is cut
into the same ten heat bands at the same places as the turboshaft's, so the window colours both alike. A plain section of a two-spool
high-bypass turbofan, not a drawing of any real engine.

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
# The same short tile names as the turboshaft's: Windows refuses a longer record path.
TILES = {'Comp': (0, 256), 'Hot': (256, 576), 'Turb': (576, 832)}

# Radii from the centreline at stations along the engine. The nacelle's inner wall (the bypass duct's outer edge) runs from the intake to
# the bypass exit at x 600; the core's gas path narrows through the compressors, swells round the burners and opens through the turbine.
NACELLE = [(0, 112), (90, 116), (520, 104), (600, 92)]
NACELLE_WALL = 10
NACELLE_END = 600
CORE_START = 76
CORE_OUT = [(CORE_START, 58), (256, 42), (300, 54), (540, 54), (576, 44), (832, 60)]
CORE_IN = [(CORE_START, 18), (256, 22), (576, 22), (832, 14)]
COWL = 9


def nacelle_in(x):
    return shapes.edge(NACELLE, x)


def core_out(x):
    return shapes.edge(CORE_OUT, x)


def core_in(x):
    return shapes.edge(CORE_IN, x)


def cowl_out(x):
    return core_out(x) + COWL


def casing():
    """The nacelle round the bypass, and the core's cowl between the bypass and the gas path."""
    nacelle = shapes.annulus(lambda x: nacelle_in(x) + NACELLE_WALL, nacelle_in, 0, NACELLE_END, CY, 24)
    cowl = shapes.annulus(cowl_out, core_out, CORE_START, W, CY, 32)
    return nacelle + cowl


def air():
    """The cold air: in at the intake round the spinner, through the fan, and down the bypass duct to its exit."""
    intake = shapes.annulus(nacelle_in, lambda x: 4, 0, CORE_START, CY, 8)
    bypass = shapes.annulus(nacelle_in, cowl_out, CORE_START, NACELLE_END, CY, 24)
    return intake + bypass


def blades(xs, width):
    out = []
    for x in xs:
        out.append(('poly', [(x, CY - core_out(x) + 2), (x + width, CY - core_out(x + width) + 2),
                             (x + width, CY - core_in(x + width)), (x, CY - core_in(x))]))
        out.append(('poly', [(x, CY + core_in(x)), (x + width, CY + core_in(x + width)),
                             (x + width, CY + core_out(x + width) - 2), (x, CY + core_out(x) - 2)]))
    return out


def fan():
    """The spinner and the fan's blades, across the whole intake."""
    spinner = [('poly', [(0, CY), (12, CY - 12), (34, CY - 18), (CORE_START, CY - 18), (CORE_START, CY + 18), (34, CY + 18), (12, CY + 12)])]
    row = []
    for x in (32, 52):
        row.append(('poly', [(x, CY - nacelle_in(x) + 3), (x + 11, CY - nacelle_in(x + 11) + 3), (x + 11, CY - 18), (x, CY - 16)]))
        row.append(('poly', [(x, CY + 16), (x + 11, CY + 18), (x + 11, CY + nacelle_in(x + 11) - 3), (x, CY + nacelle_in(x) - 3)]))
    return spinner + row


def hub():
    xs = [CORE_START + (W - CORE_START) * i / 32 for i in range(33)]
    return [('poly', [(x, CY - core_in(x)) for x in xs] + [(x, CY + core_in(x)) for x in reversed(xs)])]


# Heat bands along the core's gas path: (name, x from, x to, place along the path); the places are the turboshaft's, so both engines colour
# alike. The first band starts where the core takes its air from behind the fan.
BANDS = [
    ('Heat01', CORE_START, 120, 0.05), ('Heat02', 120, 165, 0.15), ('Heat03', 165, 210, 0.25), ('Heat04', 210, 256, 0.35),
    ('Heat05', 256, 416, 0.45), ('Heat06', 416, 576, 0.55),
    ('Heat07', 576, 640, 0.65), ('Heat08', 640, 704, 0.75), ('Heat09', 704, 768, 0.85), ('Heat10', 768, 832, 0.95),
]

REGIONS: dict[str, list] = {'Casing': casing(), 'Air': air()}
for name, x0, x1, _ in BANDS:
    REGIONS[name] = shapes.annulus(core_out, core_in, x0, x1, CY)
REGIONS.update({
    # The rotors: the fan and spinner, the hub and shafts down the middle, and the blade rows of the compressors and the turbines.
    'Steel': fan() + hub() + blades([92, 120, 150, 178, 206, 232], 6) + blades([600, 640, 690, 740, 790], 9) + [
        # The combustor's liner walls.
        ('line', [(300, CY - 49), (540, CY - 49)], 4), ('line', [(300, CY - 28), (540, CY - 28)], 4),
        ('line', [(300, CY + 49), (540, CY + 49)], 4), ('line', [(300, CY + 28), (540, CY + 28)], 4),
    ],
    # The fuel: down through a strut to a nozzle at the head of each burner.
    'Fuel': [('line', [(312, 0), (312, CY - 42)], 6), ('ellipse', (303, CY - 46, 321, CY - 32)),
             ('line', [(312, H), (312, CY + 42)], 6), ('ellipse', (303, CY + 32, 321, CY + 46))],
    # The oil: the front and rear bearings on the shafts.
    'Oil': [('ellipse', (244, CY - 13, 268, CY + 13)), ('ellipse', (580, CY - 13, 604, CY + 13))],
})

PREVIEW = {'Casing': (110, 190, 120), 'Air': (140, 190, 235), 'Steel': (170, 176, 182), 'Fuel': (204, 184, 128), 'Oil': (148, 97, 56)}
_ramp = [(76, 133, 242), (120, 170, 232), (158, 199, 224), (190, 200, 180), (250, 140, 46), (250, 140, 46),
         (245, 180, 80), (240, 200, 110), (230, 205, 140), (210, 200, 170)]
for (name, *_), colour in zip(BANDS, _ramp):
    PREVIEW[name] = colour

RECORD = """# {name}

**Drawn by script, no image generator** (28 Nov 2026), by `scripts/art/draw_turbofan_cutaway.py`: the "{name}" layer of one tile of the
systems schematic's turbofan cutaway (a bypass jet engine in section, fan to nozzle, cut into three tiles that meet with no seam), drawn at
four times and shrunk with Lanczos. White with light grey shading on a clear ground, which the schematic window tints by a live reading (a
gas-path band by the gas's heat there, the bypass air by its flow); the outline layer is near-black. Every layer is drawn from one list of
shapes, so the layers fit exactly (docs/development/vehicle-schematic.md, SC7 and SC8). A plain section, not a drawing of any real engine; no
game or other author's art was used. Running the script again draws the same file.

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
    folder = art_paths.work('turbofan-cutaway')
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
