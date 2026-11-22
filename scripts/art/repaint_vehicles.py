"""Make our vehicle bodies' paint masks again, by regions or by Segment Anything's parts, and show them side by side before any ships.

    python scripts/art/repaint_vehicles.py stage --paint regions        every body into .local/art/paint-stage/regions
    python scripts/art/repaint_vehicles.py stage --paint parts          the same from Segment Anything's maps (paint_segments.py first)
    python scripts/art/repaint_vehicles.py review                       .local/art/paint-review/index.html: now, regions, parts, tinted
    python scripts/art/repaint_vehicles.py apply --paint regions [--only NAME ...]   copy into the mods and update the texture records
    python scripts/art/repaint_vehicles.py masters                      the masters, one a line, to hand to paint_segments.py

Owner, 16 Nov 2026: the Pintle bowser's paint mask, made a pixel at a time, came out speckled; all eight vehicles with paint masks are made
again both ways and compared tinted on dark ground, as the game draws them. Each body reuses its own saved transform, so it registers with
every overlay already fitted to it, and only its facings and masks change. Nothing in the mods changes before apply.
"""
import argparse
import hashlib
import html
import json
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
from export_vehicle import repaint_body  # noqa: E402

GEN = ROOT / 'assets' / 'phobos-vehicles' / 'generated'
EXPORT = GEN / 'export'
TEXTURES = ROOT / 'mods' / 'PhobosVehicles' / 'Textures' / 'Phobos' / 'Vehicles'
STAGE = ROOT / '.local' / 'art' / 'paint-stage'
REVIEW = ROOT / '.local' / 'art' / 'paint-review'
DEFAULT_MAIN, DEFAULT_ACCENT = (38, 95), (8, 34)
SAND, ORANGE = (162, 156, 123), (195, 110, 36)

# Every body with a paint mask: its texture name, folder, master, transform, hue ranges, saturation floor, holes, and the Def's colours
# for the tinted preview. Hues and holes are those each was exported with (their texture records); the bowser's from 16 Nov 2026.
BODIES = [
    ('BulkTanker', 'BulkTanker', 'bulk-tanker-body-north/bulk-tanker-body-north-2.png', DEFAULT_MAIN, DEFAULT_ACCENT, 0.16, [], SAND, ORANGE),
    ('SurveyTruck', 'SurveyTruck', 'survey-truck-body-north/survey-truck-body-north-1.png', DEFAULT_MAIN, DEFAULT_ACCENT, 0.16, [], SAND, ORANGE),
    ('DrillRig', 'DrillRig', 'drill-rig-body-north/drill-rig-body-north-2.png', DEFAULT_MAIN, DEFAULT_ACCENT, 0.16, [], SAND, ORANGE),
    ('GeneratorSet', 'GeneratorSet', 'generator-set-body-north/generator-set-body-north-3.png', DEFAULT_MAIN, DEFAULT_ACCENT, 0.16, [], SAND, ORANGE),
    ('Tipper', 'Tipper', 'tipper-body-north/tipper-body-north-3.png', DEFAULT_MAIN, DEFAULT_ACCENT, 0.16, [], SAND, ORANGE),
    # Pintle bowser (23 Nov 2026): its stripe is drawn orange (the Def tints it blue) at hues up to 42, where the olive body starts; the
    # stripe is strongly coloured (saturation 0.35 and up) and the body dull, so MAIN_CEILING keeps the stripe out of the main paint.
    ('PintleBowser', 'PintleBowser', 'pintle-bowser-body-north/pintle-bowser-body-north-1.png', DEFAULT_MAIN, (8, 42), 0.12,
     [(446, 80), (425, 195), (467, 195), (410, 261), (495, 261)], (194, 187, 158), (64, 124, 190)),
    ('SeabedCrawler', 'SeabedCrawler', 'seabed-crawler-body-north/widened/seabed-crawler-3-wide.png', (185, 240), (8, 32), 0.05, [], (70, 78, 86), (214, 124, 44)),
    ('SeabedCrawler_Research', 'SeabedCrawler', 'seabed-crawler-body-north/widened/seabed-crawler-1-wide.png', (185, 240), (8, 32), 0.05, [], (70, 78, 86), (214, 124, 44)),
    ('SeabedCrawler_Sensor', 'SeabedCrawler', 'seabed-crawler-body-north/widened/seabed-crawler-4-wide.png', (185, 240), (8, 32), 0.05, [], (70, 78, 86), (214, 124, 44)),
]
FACINGS = ('north', 'east', 'south')
# Bodies shipped in their own colours with no paint mask, so Vehicle Framework offers no paint for them (the owner's choice of 23 Nov 2026,
# D'thorga Vehicles 0.106.3: repainted, the field kitchen's shutter frames and the flatbed's pale sand rails stayed patchy after the trucks'
# fixes; docs/development/vehicle-paint-masks.md has what was tried). Name, folder, master, holes; staged with any --paint and applied the same.
UNPAINTED = [
    ('FlatbedTrailer', 'FlatbedTrailer', 'flatbed-trailer-body-north/flatbed-trailer-body-north-2.png',
     [(406, 184), (362, 193), (419, 258), (353, 253), (384, 66)]),
    ('BillyFieldKitchen', 'FieldKitchen', 'billy-field-kitchen-body-north/billy-field-kitchen-body-north-3.png',
     [(446, 77), (424, 198), (468, 198), (413, 254), (479, 254)]),
]
# A body whose main paint shares hues with its accent: no main paint is more saturated than this (the Pintle bowser's red dashes along
# its tank when painted red, the owner's review of 23 Nov 2026: stripe pixels at hue 38 to 42 had taken the main paint).
MAIN_CEILING = {'PintleBowser': 0.35}
# Bodies whose faint paint is settled by Segment Anything's parts (export_vehicle.settle_doubtful_paint), with how far above the floor paint
# counts as faint: the Pintle bowser's tank edge (0.11 to 0.13 over a 0.12 floor).
SETTLE_FAINT = {'PintleBowser': 0.03}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def mend_flecks(name, folder, master, out, colour, accent):
    """The flecks paint_check.py still finds at game size, after the shrink blends the master's pixels, mended with its own fill_flecks:
    each takes the paint that rings it (D'thorga Vehicles 0.106.1, when the paint's own lines joined the paint; the owner's rule that a
    mask never reaches them speckled). East and south are written again from the mended north, as the exporter turns them. Returns how
    many flecks it mended."""
    import paint_check  # noqa: PLC0415  paint_check imports this module; imported here, when both are loaded
    from export_vehicle import FACINGS  # noqa: PLC0415
    flecks, problem = paint_check.check(name, folder, master, out, colour, accent)
    if problem or not flecks:
        return 0
    north = Image.open(out / f'{name}_northm.png').convert('RGBA')
    paint_check.fill_flecks(north, flecks)
    record_path = out / f'{name}.export.json'
    record = json.loads(record_path.read_text(encoding='utf-8'))
    for facing, turn in FACINGS.items():
        path = out / f'{name}_{facing}m.png'
        (north if turn is None else north.transpose(turn)).save(path, optimize=True)
        record['files'][path.name] = sha(path)
    record['flecks_mended'] = len(flecks)
    record_path.write_text(json.dumps(record, indent=2), encoding='utf-8')
    return len(flecks)


def stage(paint, only):
    for name, folder, master, main_hue, accent_hue, saturation, holes, colour, accent in BODIES:
        if only and name not in only:
            continue
        out = STAGE / paint / folder
        repaint_body(GEN / master, EXPORT / f'{name}.transform.json', out, name, main_hue, accent_hue, saturation, holes, paint,
                     main_max_saturation=MAIN_CEILING.get(name, 1.0), settle_faint=SETTLE_FAINT.get(name, False))
        mended = mend_flecks(name, folder, GEN / master, out, colour, accent) if paint == 'repaired' else 0
        print(f'staged {paint}: {name}' + (f' ({mended} fleck{"" if mended == 1 else "s"} mended)' if mended else ''), flush=True)
    for name, folder, master, holes in UNPAINTED:
        if only and name not in only:
            continue
        out = STAGE / paint / folder
        for facing in FACINGS:
            (out / f'{name}_{facing}m.png').unlink(missing_ok=True)
        repaint_body(GEN / master, EXPORT / f'{name}.transform.json', out, name, None, None, holes=holes, paint='natural')
        print(f'staged {paint}: {name} (own colours, no paint mask)', flush=True)


def tinted(texture, mask, colour, accent):
    """Roughly as the game's pattern shader draws it: the texture times the main colour where the mask is red, the accent where green."""
    texture = Image.open(texture).convert('RGBA')
    mask = Image.open(mask).convert('RGB')
    red, green, _ = mask.split()
    rgb = texture.convert('RGB')
    main = ImageChops.multiply(rgb, Image.new('RGB', rgb.size, colour))
    second = ImageChops.multiply(rgb, Image.new('RGB', rgb.size, accent))
    result = Image.composite(main, rgb, red)
    result = Image.composite(second, result, green)
    result.putalpha(texture.getchannel('A'))
    ground = Image.new('RGBA', result.size, (62, 57, 47, 255))
    ground.alpha_composite(result)
    return ground


def review():
    import paint_check  # here, not at the top: paint_check reads this module's list of bodies
    REVIEW.mkdir(parents=True, exist_ok=True)
    rows = []
    for name, folder, master, *_rest, colour, accent in BODIES:
        cells = []
        for label, base in (('now', TEXTURES / folder), ('repaired', STAGE / 'repaired' / folder), ('parts', STAGE / 'parts' / folder)):
            texture, mask = base / f'{name}_north.png', base / f'{name}_northm.png'
            if not texture.is_file():
                cells.append(f'<figure><div class=missing>not staged</div><figcaption>{label}</figcaption></figure>')
                continue
            flecks, problem = paint_check.check(name, folder, GEN / master, base, colour, accent)
            caption = f'{label}: ' + (problem if problem else f'{len(flecks)} fleck{"" if len(flecks) == 1 else "s"}')
            picture = tinted(texture, mask, colour, accent)
            whole = REVIEW / f'{name}-{label}.png'
            picture.save(whole)
            # A close-up of the middle, four times, so a fleck shows.
            w, h = picture.size
            box = (w // 2 - 70, h // 2 - 90, w // 2 + 70, h // 2 + 90)
            close = REVIEW / f'{name}-{label}-close.png'
            picture.crop(box).resize(((box[2] - box[0]) * 3, (box[3] - box[1]) * 3), Image.NEAREST).save(close)
            mask_close = REVIEW / f'{name}-{label}-mask.png'
            Image.open(mask).convert('RGB').crop(box).resize(((box[2] - box[0]) * 3, (box[3] - box[1]) * 3), Image.NEAREST).save(mask_close)
            cells.append(f'<figure><img src="{whole.name}" class=whole><img src="{close.name}" class=close><img src="{mask_close.name}" class=close>'
                         f'<figcaption>{caption}</figcaption></figure>')
        rows.append(f'<h2>{html.escape(name)}</h2><div class=row>{"".join(cells)}</div>')
    page = ('<!doctype html><meta charset=utf-8><title>Paint masks</title><style>body{background:#1b1d21;color:#ddd;font-family:Segoe UI,sans-serif;'
            'margin:16px}.row{display:flex;gap:24px;flex-wrap:wrap;align-items:flex-start}figure{margin:0;text-align:center}'
            '.whole{height:420px;display:block;margin:auto}.close{width:210px;display:block;margin:6px auto 0}.missing{width:210px;height:420px;'
            'display:flex;align-items:center;justify-content:center;border:1px dashed #555}figcaption{margin-top:6px;font-weight:600}</style>'
            '<h1>Paint masks: now, by regions, by Segment Anything\'s parts</h1><p>Each tinted roughly as the game draws it, on dark ground; '
            'under it the middle three times larger, then its paint mask (red the main paint, green the accent).</p>' + ''.join(rows))
    (REVIEW / 'index.html').write_text(page, encoding='utf-8')
    print(REVIEW / 'index.html')


def apply(paint, only):
    unpainted = {row[0] for row in UNPAINTED}
    for name, folder, *_ in BODIES + UNPAINTED:
        if only and name not in only:
            continue
        source, target = STAGE / paint / folder, TEXTURES / folder
        changed = {}
        for facing in FACINGS:
            for suffix in ('', 'm'):
                file = f'{name}_{facing}{suffix}.png'
                old = target / file
                old_sha = sha(old) if old.is_file() else None
                if suffix and name in unpainted:
                    old.unlink(missing_ok=True)   # no paint mask: Vehicle Framework offers no paint without one
                    continue
                shutil.copyfile(source / file, old)
                changed[old_sha] = sha(old)
        for record in target.glob('*.record.md'):
            text = record.read_text(encoding='utf-8')
            updated = text
            for before, after in changed.items():
                if before:
                    updated = updated.replace(before, after)
            if updated != text:
                record.write_text(updated, encoding='utf-8')
        print(f'applied {paint}: {name}')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest='command', required=True)
    for command in ('stage', 'apply'):
        sub = commands.add_parser(command)
        sub.add_argument('--paint', choices=('regions', 'repaired', 'parts', 'pixels'), required=True)
        sub.add_argument('--only', action='append', default=[])
    commands.add_parser('review')
    commands.add_parser('masters')
    args = parser.parse_args(argv)
    if args.command == 'stage':
        stage(args.paint, args.only)
    elif args.command == 'apply':
        apply(args.paint, args.only)
    elif args.command == 'review':
        review()
    else:
        for _, _, master, *_ in BODIES:
            print((GEN / master).relative_to(ROOT).as_posix())
    return 0


if __name__ == '__main__':
    sys.exit(main())
