"""Draw the systems schematic's locomotive boiler cutaway by script: a fire-tube boiler in side section, firebox to chimney.

    python scripts/art/draw_boiler_cutaway.py                  draw it to the art working folder, with a tinted preview
    python scripts/art/draw_boiler_cutaway.py --ship FOLDER    write the layers into FOLDER and a record for each

Why by script (docs/development/vehicle-schematic.md, SC2; the owner's word of 28 Nov 2026, "Let's finish the schematic then"): as the other
cutaways, every region comes from one list of shapes, so the layers fit exactly and the window tints each by a live reading. The water is cut
into horizontal bands, each its own layer at the height the gauge glass would read it, so the window shows the water line where the glass
says (tint Level): steam above it, water below. The crown sheet, the firebox's top, is where the glass reads 0.1 (Systems_Boiler.xml: below
it the crown is uncovered), and the glass's full 1 is the top of the shell; below the crown, the last tenth of the glass is spread down to the
foundation ring. Authored proportions, not a drawing of any real locomotive's boiler.

Shared drawing lives in cutaway_shapes.py. The section is 512 x 256: heights a power of two, as the art rules ask.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402
import cutaway_shapes as shapes  # noqa: E402

W, H = 512, 256
WALL = 8
# The shell's inside: the firebox wrapper (deep, at the back), the barrel, the smokebox at the front.
WRAPPER = (16, 56, 182, 240)
BARREL = (170, 56, 430, 206)
SMOKEBOX = (424, 56, 500, 206)
INSIDE_TOP = WRAPPER[1] + WALL          # 64: the glass reads 1 here
CROWN = 104                              # the firebox's top: the glass reads 0.1 here
INSIDE_BOTTOM = WRAPPER[3] - WALL        # 232: the foundation ring
FIREBOX = (34, CROWN, 160, 222)
GLASS_CROWN = 0.1


def glass_at(y: float) -> float:
    """What the gauge glass reads when the water stands at height y."""
    if y <= CROWN:
        return GLASS_CROWN + (CROWN - y) / (CROWN - INSIDE_TOP) * (1 - GLASS_CROWN)
    return GLASS_CROWN * (1 - (y - CROWN) / (INSIDE_BOTTOM - CROWN))


def rect(box):
    return ('rect', box)


def inset(box, by=WALL):
    x0, y0, x1, y1 = box
    return (x0 + by, y0 + by, x1 - by, y1 - by)


# Water bands from the top of the shell down: seven between the top and the crown, two below it.
_edges = [INSIDE_TOP + (CROWN - INSIDE_TOP) * i / 7 for i in range(8)] + [CROWN + (INSIDE_BOTTOM - CROWN) * i / 2 for i in (1, 2)]
BANDS = [(f'Water{i + 1:02d}', top, bottom, round(glass_at((top + bottom) / 2), 3)) for i, (top, bottom) in enumerate(zip(_edges, _edges[1:]))]

# The shell's whole inside, which the water bands are cut from: the wrapper and the barrel are one space, with no wall between them.
_inside = [rect(inset(WRAPPER)), rect((WRAPPER[2] - WALL, BARREL[1] + WALL, BARREL[2] - WALL, BARREL[3] - WALL))]


def band(top, bottom):
    """The shell's inside between two heights, as rectangles (the firebox and tubes cover it after)."""
    out = []
    for kind, (x0, y0, x1, y1) in _inside:
        a, b = max(y0, top), min(y1, bottom)
        if b > a:
            out.append(rect((x0, a, x1, b)))
    return out


TUBES = [rect((FIREBOX[2], y, SMOKEBOX[0] + WALL, y + 11)) for y in range(116, 190, 20)]

REGIONS: dict[str, list] = {
    # The shell: firebox wrapper, barrel, smokebox, steam dome and chimney, solid; what is inside covers all but the walls.
    'Casing': [rect(WRAPPER), rect(BARREL), rect(SMOKEBOX), ('ellipse', (262, 34, 308, 70)), rect((452, 4, 484, 64))],
}
for name, top, bottom, _ in BANDS:
    REGIONS[name] = band(top, bottom)
REGIONS.update({
    # The fire on the grate, and the hot gas through the tubes, the smokebox and up the chimney.
    'Fire': [rect(inset(FIREBOX, 4))],
    'Smoke': TUBES + [rect(inset(SMOKEBOX)), rect((460, 0, 476, 56))],
    # The firebox's plates, the tube plates and the grate.
    'Steel': [
        ('line', [(FIREBOX[0], FIREBOX[1]), (FIREBOX[2], FIREBOX[1])], 4),
        ('line', [(FIREBOX[0], FIREBOX[1]), (FIREBOX[0], FIREBOX[3])], 4),
        ('line', [(FIREBOX[2], FIREBOX[1]), (FIREBOX[2], FIREBOX[3])], 4),
        ('line', [(SMOKEBOX[0] + WALL, BARREL[1] + WALL), (SMOKEBOX[0] + WALL, BARREL[3] - WALL)], 3),
    ] + [('line', [(x, FIREBOX[3] - 6), (x + 10, FIREBOX[3] - 6)], 3) for x in range(42, 150, 16)],
    # The fusible plug, in the middle of the crown sheet.
    'Plug': [('ellipse', (91, CROWN - 5, 103, CROWN + 5))],
})

STEAM = (229, 232, 224)
WATER = (77, 204, 189)
PREVIEW = {'Casing': (110, 190, 120), 'Fire': (240, 140, 50), 'Smoke': (168, 158, 168), 'Steel': (170, 176, 182), 'Plug': (110, 190, 120)}
PREVIEW_LEVEL = 0.8
for name, _, _, position in BANDS:
    PREVIEW[name] = WATER if PREVIEW_LEVEL >= position else STEAM

RECORD = """# {name}

**Drawn by script, no image generator** (28 Nov 2026), by `scripts/art/draw_boiler_cutaway.py`: the "{name}" layer of the systems
schematic's locomotive boiler cutaway (a fire-tube boiler in side section, firebox to chimney), drawn at four times and shrunk with Lanczos.
White with light grey shading on a clear ground, which the schematic window tints by a live reading (a water band as water or steam by the
gauge glass, the fire and smoke while the fire burns); the outline layer is near-black. Every layer is drawn from one list of shapes, so the
layers fit exactly (docs/development/vehicle-schematic.md, SC2 and SC7). A plain section with authored proportions, not a drawing of any real
locomotive's boiler; no game or other author's art was used. Running the script again draws the same file.

- `{name}.png`: SHA-256 `{sha}`
"""


def draw() -> dict[str, Image.Image]:
    return shapes.draw(REGIONS, W, H, outline_as_one=tuple(name for name, *_ in BANDS))


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--ship', help='write the layers into this folder, and a record for each')
    args = parser.parse_args(argv)
    layers = draw()
    folder = art_paths.work('boiler-cutaway')
    folder.mkdir(parents=True, exist_ok=True)
    shapes.preview(layers, PREVIEW, W, H).save(folder / 'preview.png')
    print(folder / 'preview.png')
    for name, top, bottom, position in BANDS:
        print(f'{name}: {top:.0f} to {bottom:.0f}, glass {position}')
    if args.ship:
        for target in shapes.ship(layers, Path(args.ship), art_paths.record_path, RECORD):
            print(target)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
