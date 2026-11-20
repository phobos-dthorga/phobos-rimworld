"""Export D'thorga Farming's machines from their picked masters (docs/development/farming-batch-one.md, "Art and sound plan").

    python assets/phobos-vehicles/export_farming.py [--mods mods] [--batch one|two|all]

The combine's body and the chaser bin's body go through export_vehicle.py's own body export. The combine's grain fronts were drawn alone
(the 8-cell front is wider than the body's picture), so they are placed here: cut from the white with the background AI's mask, scaled so
the front is exactly as many cells wide as it cuts (6 or 8, owner's widths of 20 Nov 2026) at 128 px a cell, centred across the combine,
and set with the bottom of its throat just over the top of the body, on a canvas the size of the body's. Run the background AI on the
masters first (scripts/art/clean_backgrounds.py masks --also ...). Free: no generation, and the masters are never touched.
"""
import argparse
import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
from export_sprite import ai_background, export as export_sprite, white_to_alpha  # noqa: E402
from export_vehicle import PIXELS_PER_CELL, export_body, record, separate_paint, sha, write_facings  # noqa: E402
import art_paths  # noqa: E402
import paint_check  # noqa: E402

GENERATED = ROOT / 'assets' / 'phobos-vehicles' / 'generated'
COMBINE = GENERATED / 'farming-combine-body-north' / 'farming-combine-body-north-1.png'
CHASER_BIN = GENERATED / 'farming-chaser-bin-body-north' / 'farming-chaser-bin-body-north-3.png'
# (picture name, master, cells wide): the owner's picks of 20 Nov 2026, from the sharper round (high quality at about 3072 px wide) that
# replaced the first medium-quality picks once the corn header showed the first way lost the fine details.
FRONTS = [
    ('Combine_FrontStandard', GENERATED / 'farming-combine-front-standard-sharp' / 'farming-combine-front-standard-sharp-2.png', 6),
    ('Combine_FrontWide', GENERATED / 'farming-combine-front-wide-sharp' / 'farming-combine-front-wide-sharp-2.png', 8),
    # The corn header (owner, 20 Nov 2026): the sharper round's attempt 2, high quality at 3072 x 1024.
    ('Combine_FrontCorn', GENERATED / 'farming-combine-front-corn-sharp' / 'farming-combine-front-corn-sharp-2.png', 6),
]
# (picture name, master, cells square): the field bin and the silo, attempt 2 of each the default (owner, 20 Nov 2026; attempt 1 of each is
# kept as an alternate look, exported later). Single pictures, round from above, so one facing does for all four.
BUILDINGS = [
    ('FieldBin', GENERATED / 'farming-field-bin' / 'farming-field-bin-2.png', 3),
    ('Silo', GENERATED / 'farming-silo' / 'farming-silo-2.png', 4),
]
# Batch two (docs/development/farming-batch-two.md, "Art"): (picture name, master, cells long, draw size in cells). The owner's picks of
# 20 Nov 2026: both attempts of the T60, T130 and cultivator as two models each, both sides of the reversible plough, the drill's attempt 2.
# Every body is painted 'repaired': regions, then flecks repaired by Segment Anything's parts map (paint_segments.py first).
BATCH_TWO = [
    ('TractorT60', GENERATED / 'farming-tractor-t60-body-north' / 'farming-tractor-t60-body-north-1.png', 5, (4, 6)),
    ('TractorT60F', GENERATED / 'farming-tractor-t60-body-north' / 'farming-tractor-t60-body-north-2.png', 5, (4, 6)),
    ('TractorT130', GENERATED / 'farming-tractor-t130-body-north' / 'farming-tractor-t130-body-north-1.png', 6, (4, 7)),
    ('TractorT130RC', GENERATED / 'farming-tractor-t130-body-north' / 'farming-tractor-t130-body-north-2.png', 6, (4, 7)),
    ('Plough', GENERATED / 'farming-plough-body-north' / 'farming-plough-body-north-1.png', 3, (4, 4)),
    ('Plough_Turned', GENERATED / 'farming-plough-body-north' / 'farming-plough-body-north-2.png', 3, (4, 4)),
    ('Cultivator', GENERATED / 'farming-cultivator-body-north' / 'farming-cultivator-body-north-1.png', 3, (4, 4)),
    ('CultivatorLight', GENERATED / 'farming-cultivator-body-north' / 'farming-cultivator-body-north-2.png', 3, (4, 4)),
    ('SeedDrill', GENERATED / 'farming-seed-drill-body-north' / 'farming-seed-drill-body-north-2.png', 4, (4, 5)),
]
# The articulated T400 (owner, 20 Nov 2026: two halves on our hitch): drawn whole, 8 cells long, then cut at its pivot into two vehicles
# of 4 x 4, each on a 5 x 5 picture. Attempt 1 is the ballasted T400 B, attempt 2 the standard (the agent's proposal, kept by the owner).
ARTICULATED = [
    ('TractorT400B', GENERATED / 'farming-tractor-t400-body-north' / 'farming-tractor-t400-body-north-1.png'),
    ('TractorT400', GENERATED / 'farming-tractor-t400-body-north' / 'farming-tractor-t400-body-north-2.png'),
]
ARTICULATED_LONG = 8
# Each body's paint saturation floor: the one with the fewest flecks in a sweep of 0.10 to 0.28 (paint_check.py, 20 Nov 2026); 0.16 otherwise.
SATURATION = {'TractorT60': 0.10, 'TractorT60F': 0.10, 'TractorT130': 0.13, 'TractorT130RC': 0.19, 'Plough': 0.10, 'Plough_Turned': 0.10,
              'Cultivator': 0.25, 'CultivatorLight': 0.10, 'SeedDrill': 0.10, 'TractorT400B': 0.13, 'TractorT400': 0.13}
ARTICULATED_DRAW = (5, 10)
HALF_DRAW = (5, 5)
# Where to look for the pivot: the narrowest row of the picture between these shares of the body's length.
PIVOT_SEARCH = (0.40, 0.70)

# The body 9 cells long on an 8 x 14 picture: wide enough for the 8-cell front, long enough for its depth ahead of the body.
COMBINE_DRAW = (8, 14)
# Harvest green as the main paint, the muted yellow as the accent, in degrees of hue (measured by eye from the masters).
MAIN_HUE = (70, 150)
ACCENT_HUE = (35, 60)
# How far the throat reaches over the body's front edge, in pixels at game size, so the two join with no gap.
THROAT_OVERLAP = 6


def opaque_box(image):
    return image.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox()


def body_top(master, transform):
    """Where the body's front edge lies on the canvas, in pixels from the top."""
    cut = white_to_alpha(Image.open(master), background=ai_background(master))
    return round(transform['offset'][1] + opaque_box(cut)[1] * transform['scale'])


def export_front(name, master, cells, out, transform, top):
    background = ai_background(master)
    if background is None:
        raise SystemExit(f'{master.name}: the background AI has not looked at it yet (clean_backgrounds.py masks --also {master})')
    cut = white_to_alpha(Image.open(master), background=background)
    content = cut.crop(opaque_box(cut))
    width = cells * PIXELS_PER_CELL
    height = round(content.height * width / content.width)
    content = content.convert('RGBa').resize((width, height), Image.LANCZOS).convert('RGBA')
    canvas = tuple(transform['canvas'])
    if height > top + THROAT_OVERLAP:
        raise SystemExit(f'{name}: {height} px deep, but only {top} px lie ahead of the body: make the picture longer')
    image = Image.new('RGBA', canvas, (0, 0, 0, 0))
    image.alpha_composite(content, ((canvas[0] - width) // 2, top + THROAT_OVERLAP - height))
    written = write_facings(image, out, name)
    record(out, name, master, written, {'kind': 'front', 'cells_wide': cells, 'like': 'Combine.transform.json', 'top': top,
                                        'throat_overlap': THROAT_OVERLAP})
    return written


# A row narrower than this share of the widest row is the pivot (the hinge plate and its stub), not the body.
PIVOT_WIDTH_SHARE = 0.3


def row_widths(image):
    alpha = image.getchannel('A')
    return [sum(1 for x in range(image.width) if alpha.getpixel((x, y)) >= 128) for y in range(image.height)]


def pivot_cut(image, start, stop):
    """Where to cut an articulated body, drawn with a gap at its pivot: from the narrowest row between start and stop, up to the first
    row of the front body, so the whole hinge goes with the rear half and no arc of it shows below the front half when it bends."""
    widths = row_widths(image)
    narrowest = min(range(start, stop), key=lambda y: widths[y])
    limit = PIVOT_WIDTH_SHARE * max(widths)
    cut = narrowest
    while cut > start and widths[cut - 1] < limit:
        cut -= 1
    return cut


def clean_flecks(name, master, out, colours):
    """The flecks left in a body's paint mask after the export, repainted with the paint that rings each (paint_check.fill_flecks), and
    its mask facings and record written again. The rule: no speckled mask reaches the owner (assets/AGENTS.md). colours: the Def's paint,
    from the export, so a fleck is judged by whether it shows in those colours."""
    flecks, problem = paint_check.check(name, name, master, out, tuple(colours['color']), tuple(colours['colorTwo']),
                                        transform_path=out / f'{name}.transform.json')
    if problem:
        raise SystemExit(f'{name}: {problem}')
    if not flecks:
        return 0
    mask = Image.open(out / f'{name}_northm.png').convert('RGBA')
    changed = paint_check.fill_flecks(mask, flecks)
    written = write_facings(mask, out, name, 'm')
    record_path = out / f'{name}.export.json'
    data = json.loads(record_path.read_text(encoding='utf-8'))
    for path in written:
        data['files'][path.name] = sha(path)
    data['flecks_filled'] = len(flecks)
    record_path.write_text(json.dumps(data, indent=2), encoding='utf-8')
    return len(flecks)


def export_articulated(name, master, out):
    """The T400 cut at its pivot into a front half (engine and cab) and a rear half (rear axle, linkage, drawbar), each its own picture
    centred on its own canvas, with the pivot's distance from each half's middle recorded for the hitch figures. The uncut picture is
    never shipped: it is made in the art tools' working folder (art_paths), where the halves are cut from."""
    whole_dir = art_paths.work('farming-export', name)
    whole = export_body(master, whole_dir, name, ARTICULATED_LONG, ARTICULATED_DRAW, MAIN_HUE, ACCENT_HUE,
                        min_saturation=SATURATION.get(name, 0.16), paint='repaired')
    clean_flecks(name, master, whole_dir, whole['colours'])
    texture = Image.open(whole_dir / f'{name}_north.png').convert('RGBA')
    mask = Image.open(whole_dir / f'{name}_northm.png').convert('RGBA')
    box = opaque_box(texture)
    top, bottom = box[1], box[3]
    pivot = pivot_cut(texture, round(top + PIVOT_SEARCH[0] * (bottom - top)), round(top + PIVOT_SEARCH[1] * (bottom - top)))
    canvas = (HALF_DRAW[0] * PIXELS_PER_CELL, HALF_DRAW[1] * PIXELS_PER_CELL)
    out.mkdir(parents=True, exist_ok=True)
    figures = {}
    for half, cut in (('Front', (0, 0, texture.width, pivot)), ('Rear', (0, pivot, texture.width, texture.height))):
        piece, piece_mask = texture.crop(cut), mask.crop(cut)
        content = opaque_box(piece)
        piece, piece_mask = piece.crop(content), piece_mask.crop(content)
        image = Image.new('RGBA', canvas, (0, 0, 0, 0))
        image_mask = Image.new('RGBA', canvas, (0, 0, 0, 255))
        at = ((canvas[0] - piece.width) // 2, (canvas[1] - piece.height) // 2)
        image.alpha_composite(piece, at)
        image_mask.paste(piece_mask, at)
        half_name = f'{name}{half}'
        written = write_facings(image, out, half_name) + write_facings(image_mask, out, half_name, 'm')
        # The pivot from the half's middle, in cells: behind it for the front half, ahead of it for the rear.
        middle = at[1] + piece.height / 2
        edge = at[1] + piece.height if half == 'Front' else at[1]
        figures[half] = {'length_cells': round(piece.height / PIXELS_PER_CELL, 2),
                         'pivot_from_middle_cells': round(abs(edge - middle) / PIXELS_PER_CELL, 2)}
        record(out, half_name, master, written, {'kind': 'articulated half', 'half': half.lower(), 'cut_row_on_whole': pivot,
                                                 'cut_from': f'{name}, {ARTICULATED_LONG} cells long on a {ARTICULATED_DRAW[0]} by {ARTICULATED_DRAW[1]} picture',
                                                 'draw_size': list(HALF_DRAW), **figures[half],
                                                 'colours': whole['colours']})
    return figures


# Batch two's upgrade parts (the owner's picks of 20 Nov 2026), each drawn alone and fitted here onto the bodies' exported pictures. A box is
# (x0, y0, x1, y1) as shares of the body's outline in its north picture (0 to 1 across it; beyond for a part outside it), with whether the
# part is mirrored; the part is scaled to fit the box, keeping its shape, and centred in it. Boxes are the agent's, set by eye from the
# pictures and checked tinted. An overlay's canvas may be wider than its body's (wings), centred on it: canvas cells (width, height).
PART = lambda key: GENERATED / f'farming-{key}' / f'farming-{key}-1.png'
WEIGHTS = [((0.33, -0.10, 0.67, 0.02), False)]
DUALS = [((-0.17, 0.49, 0.0, 0.87), False), ((1.0, 0.49, 1.17, 0.87), True)]
TANK = [((0.17, 0.28, 0.31, 0.50), False)]
TANK_T400 = [((0.10, 0.62, 0.26, 0.93), False)]
FITS = [
    # (overlay name, folder, body picture name, part, boxes, overlay canvas cells or None for the body's)
    ('TractorT60_Weights', 'TractorT60', 'TractorT60', 'front-weights', WEIGHTS, None),
    ('TractorT60F_Weights', 'TractorT60F', 'TractorT60F', 'front-weights', WEIGHTS, None),
    ('TractorT130RC_Weights', 'TractorT130RC', 'TractorT130RC', 'front-weights', WEIGHTS, None),
    ('TractorT400Front_Weights', 'TractorT400', 'TractorT400Front', 'front-weights', WEIGHTS, (5, 6)),
    ('TractorT60_Duals', 'TractorT60', 'TractorT60', 'dual-tyre', DUALS, (5, 6)),
    ('TractorT60F_Duals', 'TractorT60F', 'TractorT60F', 'dual-tyre', DUALS, (5, 6)),
    ('TractorT130_Duals', 'TractorT130', 'TractorT130', 'dual-tyre', DUALS, (5, 7)),
    ('TractorT130RC_Duals', 'TractorT130RC', 'TractorT130RC', 'dual-tyre', DUALS, (5, 7)),
    ('TractorT60_Tank', 'TractorT60', 'TractorT60', 'saddle-tank', TANK, None),
    ('TractorT60F_Tank', 'TractorT60F', 'TractorT60F', 'saddle-tank', TANK, None),
    ('TractorT130_Tank', 'TractorT130', 'TractorT130', 'saddle-tank', TANK, None),
    ('TractorT130RC_Tank', 'TractorT130RC', 'TractorT130RC', 'saddle-tank', TANK, None),
    ('TractorT400Front_Tank', 'TractorT400', 'TractorT400Front', 'saddle-tank', TANK_T400, None),
    ('TractorT400BFront_Tank', 'TractorT400B', 'TractorT400BFront', 'saddle-tank', TANK_T400, None),
    ('Plough_Wings', 'Plough', 'Plough', 'plough-wings', [((0.30, 0.93, 0.86, 1.34), False)], (4, 7)),
    ('Plough_Turned_Wings', 'Plough', 'Plough_Turned', 'plough-wings', [((0.30, 0.95, 0.86, 1.36), True)], (4, 7)),
    ('Cultivator_Wings', 'Cultivator', 'Cultivator', 'cultivator-wing', [((-0.46, 0.22, 0.03, 1.0), True), ((0.97, 0.22, 1.46, 1.0), False)], (8, 4)),
    ('CultivatorLight_Wings', 'CultivatorLight', 'CultivatorLight', 'cultivator-wing', [((-0.46, 0.27, 0.03, 1.0), True), ((0.97, 0.27, 1.46, 1.0), False)], (8, 4)),
    ('SeedDrill_Wings', 'SeedDrill', 'SeedDrill', 'drill-wing', [((-0.55, 0.63, 0.03, 0.93), True), ((0.97, 0.63, 1.55, 0.93), False)], (8, 5)),
    ('SeedDrill_Hopper', 'SeedDrill', 'SeedDrill', 'hopper-extension', [((0.13, 0.44, 0.87, 0.66), False)], None),
]


# A part whose drawn colours should not take the accent paint: the hopper extension's grain is beige, which the accent's hue range would
# paint yellow (seen tinted, 20 Nov 2026); with no accent range its grain and thin stripe stay as drawn.
NO_ACCENT = {'SeedDrill_Hopper'}


def fit_part(name, folder, body, key, boxes, canvas_cells, textures):
    """A part fitted onto a body's north picture by shares of the body's outline, painted by the body's rules, written as an overlay."""
    out = textures / folder
    master = PART(key)
    background = ai_background(master)
    if background is None:
        raise SystemExit(f'{master.name}: the background AI has not looked at it yet (clean_backgrounds.py masks --also {master})')
    cut = white_to_alpha(Image.open(master), background=background)
    part = cut.crop(opaque_box(cut))
    body_picture = Image.open(out / f'{body}_north.png').convert('RGBA')
    left, top, right, bottom = opaque_box(body_picture)
    width, height = right - left, bottom - top
    canvas = tuple(c * PIXELS_PER_CELL for c in canvas_cells) if canvas_cells else body_picture.size
    shift = ((canvas[0] - body_picture.width) // 2, (canvas[1] - body_picture.height) // 2)
    layer = Image.new('RGBA', canvas, (0, 0, 0, 0))
    for (x0, y0, x1, y1), mirrored in boxes:
        bx0, by0 = left + x0 * width + shift[0], top + y0 * height + shift[1]
        bw, bh = (x1 - x0) * width, (y1 - y0) * height
        factor = min(bw / part.width, bh / part.height)
        size = (max(1, round(part.width * factor)), max(1, round(part.height * factor)))
        piece = part.transpose(Image.FLIP_LEFT_RIGHT) if mirrored else part
        piece = piece.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')
        layer.alpha_composite(piece, (round(bx0 + (bw - size[0]) / 2), round(by0 + (bh - size[1]) / 2)))
    texture, mask, colours = separate_paint(layer, MAIN_HUE, (0, 0) if name in NO_ACCENT else ACCENT_HUE)
    written = write_facings(texture, out, name) + write_facings(mask, out, name, 'm')
    record(out, name, master, written, {'kind': 'fitted part', 'on': body, 'boxes': [[list(b), m] for b, m in boxes],
                                        'canvas_cells': list(canvas_cells) if canvas_cells else None, 'colours': colours,
                                        'ai_mask': True})
    return written


# Node icons and the seed sack's item picture, from the parts' own colour masters (the owner's picks of 20 Nov 2026): (path under
# Textures/Phobos/Farming, part, pixels). The liquid ballast's icon has no part (it is invisible) and is drawn by the local model.
PART_PICTURES = [
    ('Upgrades/FrontWeights', 'front-weights', 128),
    ('Upgrades/DualWheels', 'dual-tyre', 128),
    ('Upgrades/PloughWings', 'plough-wings', 128),
    ('Upgrades/CultivatorWings', 'cultivator-wing', 128),
    ('Upgrades/DrillWings', 'drill-wing', 128),
    ('Upgrades/HopperExtension', 'hopper-extension', 128),
    ('Things/Seed', 'seed-sack', 128),
]


def export_batch_two(textures):
    for name, master, cells, draw in BATCH_TWO:
        result = export_body(master, textures / name.split('_')[0], name, cells, draw, MAIN_HUE, ACCENT_HUE,
                             min_saturation=SATURATION.get(name, 0.16), paint='repaired')
        filled = clean_flecks(name, master, textures / name.split('_')[0], result['colours'])
        print(f"{name}: {result['width_cells']:.2f} cells wide; {filled} flecks filled; Def colours {result['colours']}")
    for name, master in ARTICULATED:
        print(f'{name}: {export_articulated(name, master, textures / name)}')
    for name, folder, body, key, boxes, canvas_cells in FITS:
        fit_part(name, folder, body, key, boxes, canvas_cells, textures)
        print(f'{name}: fitted on {body}')
    for path, key, size in PART_PICTURES:
        target = textures / f'{path}.png'
        target.parent.mkdir(parents=True, exist_ok=True)
        export_sprite(PART(key), target, size, '1:1', 235, 30)
        print(f'  {target}')


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mods', default=str(ROOT / 'mods'), help='the mods folder to write into')
    parser.add_argument('--batch', choices=('one', 'two', 'all'), default='all', help='which batch of machines to export')
    args = parser.parse_args()
    textures = Path(args.mods) / 'PhobosFarming' / 'Common' / 'Textures' / 'Phobos' / 'Farming'
    if args.batch in ('two', 'all'):
        export_batch_two(textures)
    if args.batch == 'two':
        return
    combine = export_body(COMBINE, textures / 'Combine', 'Combine', 9, COMBINE_DRAW, MAIN_HUE, ACCENT_HUE)
    print(f"Combine: {combine['width_cells']:.2f} cells wide; Def colours {combine['colours']}")
    chaser = export_body(CHASER_BIN, textures / 'ChaserBin', 'ChaserBin', 7, (4, 8), MAIN_HUE, ACCENT_HUE)
    print(f"ChaserBin: {chaser['width_cells']:.2f} cells wide; Def colours {chaser['colours']}")
    for name, master, cells in BUILDINGS:
        target = textures / name / f'{name}.png'
        target.parent.mkdir(parents=True, exist_ok=True)
        export_sprite(master, target, cells * PIXELS_PER_CELL, '1:1', 235, 30)
        print(f'  {target}')
    top = body_top(COMBINE, combine['transform'])
    for name, master, cells in FRONTS:
        for path in export_front(name, master, cells, textures / 'Combine', combine['transform'], top):
            print(f'  {path}')


if __name__ == '__main__':
    main()
