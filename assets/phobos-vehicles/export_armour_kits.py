"""Fit the trucks' drawn armour kits onto their bodies, reproducibly: the armoured cab and the heavy cab plates, lifted out of paid edits
of each picked body's cab (assets/phobos-vehicles/requests/truck-cab-kits-paid-record.md). Free: it reads the edits and writes new files,
using batch B's helpers (export_batch_b.py). Needs Pillow (ComfyUI's Python).

    <python> assets/phobos-vehicles/export_armour_kits.py --out <artWorkPath>/armour-kits-stage --preview      stage and preview
    <python> assets/phobos-vehicles/export_armour_kits.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles  ship (after the picks)

How: each edit came back a little larger than the cab cut from the body (draw_kit_crops.py), so it is scaled back to the cut's size,
the kit is lifted out as the changed area made solid (see lifted), and laid on a white canvas the size of the body's master, at the
cut's place. Its own bounds are the box the exporter fits it into, so it moves nowhere:
the overlay registers with the body by construction. The heavy plates are drawn over the armoured cab in game (one slot, the heavy
plates first), as the record designs. Agent's process; the pictures are the owner's to pick.
"""
import argparse
import json
import shutil
from pathlib import Path
import sys

from PIL import Image

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parents[1] / 'scripts' / 'art'))
import export_batch_b as base  # noqa: E402
import art_paths  # noqa: E402
from repaint_vehicles import tinted  # noqa: E402

CROPS = HERE / 'references'
KITS = {'armoured-cab': 'ArmouredCab', 'heavy-cab-plates': 'HeavyCabPlates'}
# crop name: (vehicle texture folder and prefix, transform)
TRUCKS = {
    'tipper': ('Tipper', 'Tipper.transform.json'),
    'tanker': ('BulkTanker', 'BulkTanker.transform.json'),
    'drill-rig': ('DrillRig', 'DrillRig.transform.json'),
    'survey-truck': ('SurveyTruck', 'SurveyTruck.transform.json'),
    'generator-set': ('GeneratorSet', 'GeneratorSet.transform.json'),
}
PICK = 1  # One try each (the owner's cap of 19 Nov 2026); the pick is the only picture until the owner says otherwise.
RAM_BAR = 11  # The owner's pick of 19 Nov 2026 (kit-part-ram-bar).
SKIRT = 14  # The owner's pick (kit-part-body-skirt).
# The ram bar across the front bumper, in each body's in-game pixels (128 a cell, north facing): every body's front is at y 64 (round
# three). One box for all five, the bodies being as wide; agent placement, to revise once seen in game.
RAM_BOX = (148, 38, 364, 82)
# The body plates' left skirt, in game pixels: the whole stretch between the front and rear wheels, against the bed's own edge (the
# Talus's tipping body x 144 to 370, the Ullage's tank x 157 to 354, the Kelly's deck from x 111; wheels and edges measured from the shipped
# textures, 19 Nov 2026). The right skirt is its mirror about the canvas's middle. Agent placement, to revise once seen in game.
SKIRT_BOXES = {'Tipper': (126, 430, 146, 900), 'BulkTanker': (139, 540, 159, 940), 'DrillRig': (106, 300, 126, 900)}
# What rides on each bed by default, drawn under the kits in the preview so the skirts are seen in place (the game draws it too).
BED_LOOKS = {'Tipper': 'Tipper_Body', 'BulkTanker': 'BulkTanker_LiquidTank'}


CHANGED = 40      # A channel difference this large or more counts as changed (as edit_difference.py's default).
CLOSE = 31        # Pixels: gaps in the changed area narrower than this are closed, so the kit is one solid piece.
PAPER = 235       # A pixel this light in every channel is the white background, never part of the kit.
# Closing the changed area also swallows the old cab's own outline strokes lying on the white paper beside the kit, which shipped as
# ghost lines floating by the cab (found by scripts/art/edge_check.py, 22 Nov 2026). Opening the lifted mask by this many pixels drops
# every stroke thinner than it that stands alone; the kit's own edge is then given back within REACH of what survived.
OPEN = 9
REACH = OPEN + 2
TRIM = 1          # Pixels taken off the kit's outer edge: the edit's anti-aliasing into the paper, a pale rim on dark ground.
RIM = 3           # Within this many pixels of the kit's edge, a pixel this pale in every channel is paper bleeding in, never paint:
RIM_PALE = 150    # the kit's paint and outline are darker (agent's measure on the five trucks' cards, 22 Nov 2026).
# The kit's own dark outline matches the old cab's, so it never counts as changed: without it the body's pale roof showed through a
# gap a game pixel or two wide beside the kit (the cards of 22 Nov 2026). Giving back the edit's dark pixels there brought the old
# cab's strokes back too (agent's try), so the kit is drawn a new outline instead, OUTLINE pixels wide (about one game pixel), in INK.
OUTLINE = 2
INK = (28, 28, 26)


def solid(mask):
    """The mask with its holes filled: everything the background cannot reach from the picture's edge."""
    from PIL import ImageDraw
    outside = mask.point(lambda v: 0 if v else 255).convert('L')
    # Flood the background from a frame one pixel wide, so it reaches every edge.
    framed = Image.new('L', (outside.width + 2, outside.height + 2), 255)
    framed.paste(outside, (1, 1))
    ImageDraw.floodfill(framed, (0, 0), 128)
    reached = framed.crop((1, 1, outside.width + 1, outside.height + 1)).point(lambda v: 255 if v == 128 else 0)
    return reached.point(lambda v: 0 if v else 255)


def lifted(truck, kit, work):
    """The kit alone, transparent elsewhere, at the cut's size. The paid edits keep the cab's outline (19 Nov 2026), so the kit is what
    changed, closed into one solid piece and its holes filled, kept only where the edit draws the truck (never its white ground). A
    plain difference left holes where the edit matched the cab, and the old roof showed through them (the agent's first try)."""
    from PIL import ImageChops, ImageFilter
    edit = Image.open(base.GEN / f'paid-kit-{truck}-{kit}' / f'paid-kit-{truck}-{kit}-{PICK}.png').convert('RGB')
    crop = Image.open(CROPS / f'kit-crop-{truck}-cab.png').convert('RGB')
    edit = edit.resize(crop.size, Image.LANCZOS)
    r, g, b = ImageChops.difference(edit, crop).split()
    changed = ImageChops.lighter(ImageChops.lighter(r, g), b).point(lambda v: 255 if v >= CHANGED else 0)
    changed = changed.filter(ImageFilter.MaxFilter(CLOSE)).filter(ImageFilter.MinFilter(CLOSE))
    changed = solid(changed)
    truck_only = edit.convert('L').point(lambda v: 255 if v < PAPER else 0)
    truck_only = ImageChops.lighter(truck_only, ImageChops.lighter(*[c.point(lambda v: 255 if v < PAPER else 0) for c in edit.split()[:2]]))
    mask = ImageChops.multiply(changed, truck_only)
    body = mask.filter(ImageFilter.MinFilter(OPEN)).filter(ImageFilter.MaxFilter(OPEN))
    mask = ImageChops.multiply(mask, body.filter(ImageFilter.MaxFilter(2 * REACH + 1)))
    mask = mask.filter(ImageFilter.MinFilter(2 * TRIM + 1))
    rim = ImageChops.subtract(mask, mask.filter(ImageFilter.MinFilter(2 * RIM + 1)))
    pale = ImageChops.darker(ImageChops.darker(*edit.split()[:2]), edit.split()[2]).point(lambda v: 255 if v > RIM_PALE else 0)
    mask = ImageChops.subtract(mask, ImageChops.multiply(rim, pale))
    shape = mask.point(lambda v: 255 if v >= 128 else 0)
    outline = ImageChops.subtract(shape.filter(ImageFilter.MaxFilter(2 * OUTLINE + 1)), shape)
    edit = Image.composite(Image.new('RGB', edit.size, INK), edit, outline)
    # A hard edge: the kit is laid on white for the exporter, and a softened edge took white into its outer pixels, a pale rim
    # outside the dark outline (the fringe check, 23 Nov 2026). The exporter's scaling smooths the edge without white.
    mask = ImageChops.lighter(shape, outline)
    part = edit.convert('RGBA')
    part.putalpha(mask)
    part.save(work / f'lifted-{truck}-{kit}.png')
    return part


def master_and_box(truck, kit, work):
    """The kit on a white canvas the body master's size at the cut's place, and its bounds there: the box it is fitted into."""
    folder, transform = TRUCKS[truck]
    crops = json.loads((CROPS / 'kit-crops.json').read_text(encoding='utf-8'))
    x0, y0, _, _ = crops[f'kit-crop-{truck}-cab.png']['box_pixels']
    size = tuple(json.loads((base.EXPORT / transform).read_text(encoding='utf-8'))['master_size'])
    part = lifted(truck, kit, work)
    canvas = Image.new('RGBA', size, (255, 255, 255, 255))
    canvas.alpha_composite(part, (x0, y0))
    bounds = part.getchannel('A').point(lambda v: 255 if v >= 128 else 0).getbbox()
    master = work / f'master-{truck}-{kit}.png'
    canvas.convert('RGB').save(master)
    return master, (bounds[0] + x0, bounds[1] + y0, bounds[2] + x0, bounds[3] + y0)


def preview(out, work):
    """Each truck tinted on dark ground, north facing: as it is, armoured cab, heavy plates, and every kit (plates, skirts, ram bar)."""
    rows = []
    for truck, (folder, transform) in TRUCKS.items():
        colours = json.loads((base.EXPORT / transform).read_text(encoding='utf-8'))['colours']
        colour, accent = tuple(colours['color']), tuple(colours['colorTwo'])
        textures = base.TEXTURES / folder if hasattr(base, 'TEXTURES') else HERE.parents[1] / 'mods/PhobosVehicles/Textures/Phobos/Vehicles' / folder
        body = tinted(textures / f'{folder}_north.png', textures / f'{folder}_northm.png', colour, accent)
        if folder in BED_LOOKS:  # What rides on the bed, as the game draws it.
            bed = textures / f'{BED_LOOKS[folder]}_north.png'
            mask = textures / f'{BED_LOOKS[folder]}_northm.png'
            body.alpha_composite(tinted(bed, mask, colour, accent) if mask.is_file() else Image.open(bed).convert('RGBA'))
        cells = [body]
        for names in (['ArmouredCab'], ['HeavyCabPlates'], ['HeavyCabPlates', 'BodyPlates', 'RamBar']):
            picture = body.copy()
            for suffix in names:
                path = out / folder / f'{folder}_{suffix}_north.png'
                if path.is_file():  # Not every truck takes body plates.
                    # Overlays are drawn in their own colours (Cutout, as the winch and grab are): laid on untinted.
                    picture.alpha_composite(Image.open(path).convert('RGBA'))
            cells.append(picture)
        rows.append(cells)
    width = sum(c.width for c in rows[0]) + 8 * len(rows[0])
    height = sum(max(c.height for c in r) for r in rows) + 8 * len(rows)
    sheet = Image.new('RGB', (max(sum(c.width for c in r) + 8 * len(r) for r in rows), height), (24, 24, 24))
    y = 0
    for r in rows:
        x = 0
        for c in r:
            sheet.paste(c.convert('RGB'), (x, y))
            x += c.width + 8
        y += max(c.height for c in r) + 8
    target = work / 'armour-kits-preview.png'
    sheet.save(target)
    print('preview', target)


def master_box(transform, box):
    """A box in the body's in-game pixels to its master's pixels: the inverse of the exporter's scale and offset (as round three)."""
    data = json.loads((base.EXPORT / transform).read_text(encoding='utf-8'))
    scale, (left, top) = data['scale'], data['offset']
    return tuple(round((value - (left if index % 2 == 0 else top)) / scale) for index, value in enumerate(box))


def tiled_skirt(skirt, size, box, name):
    """The picked skirt repeated end to end down a box (in master pixels) on a white canvas the body master's size, as a new file: real
    skirts come in sections, and stretching one plate the whole length would smear its bolts."""
    work = art_paths.work('armour-kits')
    target = work / f'{name}.png'
    plate = Image.open(skirt).convert('RGB')
    plate = plate.crop(plate.convert('L').point(lambda v: 255 if v < 235 else 0).getbbox())
    width = box[2] - box[0]
    plate = plate.resize((width, max(1, round(plate.height * width / plate.width))), Image.LANCZOS)
    canvas = Image.new('RGB', size, (255, 255, 255))
    y = box[1]
    while y < box[3]:
        piece = plate.crop((0, 0, width, min(plate.height, box[3] - y)))
        canvas.paste(piece, (box[0], y))
        y += plate.height
    canvas.save(target)
    return target


def export_parts(out_root, report):
    """The ram bar on all five trucks and the body plates (a skirt each side) on the three that take them."""
    ram = base.pick('kit-part-ram-bar', RAM_BAR)
    skirt = base.pick('kit-part-body-skirt', SKIRT)
    for truck, (folder, transform) in TRUCKS.items():
        out = out_root / folder
        out.mkdir(parents=True, exist_ok=True)
        like = base.EXPORT / transform
        canvas_width = json.loads(like.read_text(encoding='utf-8'))['canvas'][0]
        box = master_box(transform, RAM_BOX)
        base.export_overlay(ram, like, out, f'{folder}_RamBar', box)
        report[f'{folder}/{folder}_RamBar'] = {'master': ram.name, 'fit': list(box)}
        if folder in SKIRT_BOXES:
            x0, y0, x1, y1 = SKIRT_BOXES[folder]
            left, right = master_box(transform, (x0, y0, x1, y1)), master_box(transform, (canvas_width - x1, y0, canvas_width - x0, y1))
            size = tuple(json.loads(like.read_text(encoding='utf-8'))['master_size'])
            base.export_overlay(tiled_skirt(skirt, size, left, f'skirt-{folder}-left'), like, out, f'{folder}_BodyPlatesLeft', left)
            base.export_overlay(tiled_skirt(base.mirrored(skirt), size, right, f'skirt-{folder}-right'), like, out, f'{folder}_BodyPlatesRight', right)
            base.layer(out, f'{folder}_BodyPlates', [f'{folder}_BodyPlatesLeft', f'{folder}_BodyPlatesRight'])
            report[f'{folder}/{folder}_BodyPlates'] = {'master': skirt.name, 'fit': [list(left), list(right)]}
        for record in out.glob('*.export.json'):
            if record.name.startswith((f'{folder}_RamBar', f'{folder}_BodyPlates')):
                shutil.move(record, base.EXPORT / record.name)  # a staging folder may be on another drive
        print('wrote', folder, 'ram bar' + (' and body plates' if folder in SKIRT_BOXES else ''))


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', required=True, help='folder holding one subfolder per vehicle')
    parser.add_argument('--preview', action='store_true', help='also draw each truck tinted, as it is and with each kit')
    args = parser.parse_args()
    work = art_paths.work('armour-kits')
    work.mkdir(parents=True, exist_ok=True)
    report = {}
    for truck, (folder, transform) in TRUCKS.items():
        out = Path(args.out) / folder
        out.mkdir(parents=True, exist_ok=True)
        for kit, suffix in KITS.items():
            master, box = master_and_box(truck, kit, work)
            name = f'{folder}_{suffix}'
            base.export_overlay(master, base.EXPORT / transform, out, name, box)
            for record in out.glob(f'{name}.export.json'):
                shutil.move(record, base.EXPORT / record.name)  # beside the bodies' records, where the clean-up tools read them
            report[f'{folder}/{name}'] = {'edit': f'paid-kit-{truck}-{kit}-{PICK}', 'fit': list(box)}
            print('wrote', folder, name)
    export_parts(Path(args.out), report)
    (work / 'armour-kits.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    if args.preview:
        preview(Path(args.out), work)


if __name__ == '__main__':
    main()
