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


CHANGED = 40      # A channel difference this large or more counts as changed (as edit_difference.py's default).
CLOSE = 31        # Pixels: gaps in the changed area narrower than this are closed, so the kit is one solid piece.
PAPER = 235       # A pixel this light in every channel is the white background, never part of the kit.


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
    mask = ImageChops.multiply(changed, truck_only).filter(ImageFilter.GaussianBlur(0.8))
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
    """Each truck tinted on dark ground, north facing: as it is, with the armoured cab, and with the heavy plates over it."""
    rows = []
    for truck, (folder, transform) in TRUCKS.items():
        colours = json.loads((base.EXPORT / transform).read_text(encoding='utf-8'))['colours']
        colour, accent = tuple(colours['color']), tuple(colours['colorTwo'])
        textures = base.TEXTURES / folder if hasattr(base, 'TEXTURES') else HERE.parents[1] / 'mods/PhobosVehicles/Textures/Phobos/Vehicles' / folder
        body = tinted(textures / f'{folder}_north.png', textures / f'{folder}_northm.png', colour, accent)
        cells = [body]
        for kits in (['armoured-cab'], ['armoured-cab', 'heavy-cab-plates']):
            picture = body.copy()
            for kit in kits:
                name = f'{folder}_{KITS[kit]}'
                # Overlays are drawn in their own colours (Cutout, as the winch and grab are): laid on untinted.
                picture.alpha_composite(Image.open(out / folder / f'{name}_north.png').convert('RGBA'))
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
                record.replace(work / record.name)
            report[f'{folder}/{name}'] = {'edit': f'paid-kit-{truck}-{kit}-{PICK}', 'fit': list(box)}
            print('wrote', folder, name)
    (work / 'armour-kits.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    if args.preview:
        preview(Path(args.out), work)


if __name__ == '__main__':
    main()
