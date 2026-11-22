"""Make a painted vehicle picture unpainted: its paint baked in, its paint mask dropped, its pale rim cleaned.

    python scripts/art/bake_paint.py PLAN.json            write each picture's baked facings into the art tools' staging folder
    python scripts/art/bake_paint.py PLAN.json --apply    write them over the shipped pictures and delete their masks

A plan is JSON: {"textures": "<folder the pictures live in>", "pictures": [{"name": "Folder/Name", "color":
[r, g, b], "colorTwo": [r, g, b]}, ...]}. A picture with no mask (``"unmasked": true``) only has its rim cleaned; one that draws nothing
(``"mask_only": true``, such as a blank body under its overlays) keeps its pictures and only loses its mask.

Why (the owner's choice of 23 Nov 2026, D'thorga Farming): repainted, D'thorga Farming's machines kept lines of their drawn green, and the
owner chose to make them non-recolourable rather than ship a repaint that might still show it; the same review found white dithering at
every border. Vehicle Framework offers paint only when a body's shader takes a colour mask, so a body drawn in its own colours with no mask
and a plain Cutout shader has no paint button. Baking keeps the picture exactly as the game drew it in its own colours (the grey texture
times the Def's colour where the mask says so), so nothing moves: no re-export, so every outline, position and overlay's fit stays.

The rim: these pictures were exported before export_sprite.py's soft-rim fix (5c6d04e8), so an outline's fade to white left a
half-transparent pale grey pixel outside it. ``clean_rim`` gives every partly transparent pixel on the outer edge the colour of the most
opaque pixel beside it, inside the picture, keeping its transparency: the edge stays soft but takes the outline's colour, not the white's.

Game-free: any game whose vehicles are a grey texture plus a red/green paint mask can use it with its own plan.
"""
import argparse
import json
import sys
from pathlib import Path

from PIL import Image, ImageChops

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402  The art tools' working folder (K: on the owner's PC), never a path written into the plan

FACINGS = {'north': None, 'east': Image.ROTATE_270, 'south': Image.ROTATE_180}
RIM_PALER = 24  # a rim pixel this much lighter (0 to 255) than the pixel inside it is pale rim, not the picture's own light edge (authored)
RIM_PASSES = 2  # rim pixels are cleaned this many pixels deep from the transparent outside (authored: the rim is one or two pixels at game size)


def baked(texture, mask, colour, accent):
    """The picture as the game's pattern shader draws it in the Def's own colours: the texture times the main colour where the mask's red
    says so, times the second colour where its green does, as drawn elsewhere. Alpha is kept."""
    rgb = texture.convert('RGB')
    red, green, _ = mask.convert('RGB').split()
    main = ImageChops.multiply(rgb, Image.new('RGB', rgb.size, tuple(colour)))
    second = ImageChops.multiply(rgb, Image.new('RGB', rgb.size, tuple(accent)))
    result = Image.composite(main, rgb, red)
    result = Image.composite(second, result, green)
    result.putalpha(texture.getchannel('A'))
    return result


def _light(pixel):
    return (pixel[0] + pixel[1] + pixel[2]) / 3


def clean_rim(image):
    """Gives partly transparent pixels on the outer edge the colour of the most opaque pixel beside them, when they are paler than it,
    keeping their transparency. Returns (image, pixels changed)."""
    image = image.copy()
    pixels = image.load()
    width, height = image.size
    changed = 0
    for _ in range(RIM_PASSES):
        updates = []
        for y in range(height):
            for x in range(width):
                here = pixels[x, y]
                if here[3] == 0 or here[3] == 255:
                    continue
                best = None
                outside = False
                for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)):
                    nx, ny = x + dx, y + dy
                    if not (0 <= nx < width and 0 <= ny < height):
                        outside = True
                        continue
                    there = pixels[nx, ny]
                    if there[3] == 0:
                        outside = True
                    elif there[3] > here[3] and (best is None or there[3] > best[3]):
                        best = there
                if outside and best is not None and _light(here) > _light(best) + RIM_PALER:
                    updates.append((x, y, (best[0], best[1], best[2], here[3])))
        for x, y, value in updates:
            pixels[x, y] = value
        changed += len(updates)
        if not updates:
            break
    return image, changed


def write_facings(north, folder, name):
    written = []
    for facing, turn in FACINGS.items():
        path = folder / f'{name}_{facing}.png'
        (north if turn is None else north.transpose(turn)).save(path, optimize=True)
        written.append(path)
    return written


def run(plan, apply=False):
    textures = Path(plan['textures'])
    out = textures if apply else Path(plan['out']) if 'out' in plan else art_paths.work('paint-stage', 'baked')
    report = []
    for picture in plan['pictures']:
        folder, name = picture['name'].rsplit('/', 1)
        source = textures / folder
        if picture.get('mask_only'):
            if apply:
                for facing in FACINGS:
                    (source / f'{name}_{facing}m.png').unlink(missing_ok=True)
            report.append((picture['name'], 0))
            continue
        north = Image.open(source / f'{name}_north.png').convert('RGBA')
        mask_path = source / f'{name}_northm.png'
        if not picture.get('unmasked') and mask_path.is_file():   # no mask: baked already (an export of the other batch)
            north = baked(north, Image.open(mask_path), picture['color'], picture['colorTwo'])
        north, rim = clean_rim(north)
        target = out / folder
        target.mkdir(parents=True, exist_ok=True)
        has_facings = (source / f'{name}_east.png').is_file()
        if has_facings:
            write_facings(north, target, name)
        else:
            north.save(target / f'{name}_north.png', optimize=True)
        if apply and not picture.get('unmasked'):
            for facing in FACINGS:
                (source / f'{name}_{facing}m.png').unlink(missing_ok=True)
        report.append((picture['name'], rim))
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('plan')
    parser.add_argument('--apply', action='store_true', help='write over the shipped pictures and delete their masks')
    args = parser.parse_args(argv)
    plan = json.loads(Path(args.plan).read_text(encoding='utf-8'))
    for name, rim in run(plan, args.apply):
        print(f'{name}: {rim} rim pixels cleaned' + (' (applied)' if args.apply else ''))
    return 0


if __name__ == '__main__':
    sys.exit(main())
