"""Find flecks in a vehicle's paint mask: a few pixels of one paint inside a part that is plainly another (owner, 16 Nov 2026: "Absolutely",
to an automatic inspector after the Pintle bowser's speckled mask reached the owner). Segment Anything's map of the master
(scripts/art/paint_segments.py) says which pixels belong to one part; the shipped mask says what paint each pixel has. A fleck is a
small island of one paint (FLECK_MAX pixels or fewer, outline left out) inside a part whose pixels are at least PART_SURE of another paint.
A thin stripe is long, not small, so it is never a fleck; a bolt head drawn in its own colour may be, so the report shows each one.

    python scripts/art/paint_check.py                     every body repaint_vehicles.py knows, as shipped
    python scripts/art/paint_check.py --stage regions     the same bodies as staged, before apply
    python scripts/art/paint_check.py --only PintleBowser

Exits 1 when any body has more flecks than --allow (0). Draws each body's flecks on its picture in .local/art/paint-check. Reads the
north facing only: east and south are the north turned by the exporter. Processor only, Pillow only; needs the parts maps, which live
in .local/art/segments on the owner's PC, so it is an art step, not a gate step.
"""
import argparse
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402
from export_sprite import file_sha256  # noqa: E402
from export_vehicle import OUTLINE_VALUE  # noqa: E402
import repaint_vehicles  # noqa: E402

FLECK_MAX = 12      # pixels of the shipped texture; an island this small inside a sure part is a fleck (authored)
VISIBLE = 24        # how far, in any channel of 0 to 255, a pixel's drawn colour must move if painted like its panel to count (authored)
PART_SURE = 0.8     # the share of a part's pixels that must have one paint for an island of another to count (authored)
OUT = ROOT / '.local' / 'art' / 'paint-check'


def paint_of(mask_pixel):
    red, green = mask_pixel[0], mask_pixel[1]
    if red >= 128:
        return 'main'
    if green >= 128:
        return 'accent'
    return None


def find_flecks(kinds, parts, considered, width, height, fleck_max=FLECK_MAX, part_sure=PART_SURE, visible=None):
    """The flecks in one picture, as lists of pixel indexes. kinds: the paint of each pixel; parts: the part number of each pixel (0 for
    none); considered: whether a pixel counts (opaque and not outline); visible, when given, says whether an island would look different
    painted with the given paint (the weathering drawn into a panel is near-dark, and looks the same painted or not). Game-free, so it is
    tested on made-up pictures."""
    count = width * height
    tallies = {}
    for index in range(count):
        if considered[index] and parts[index]:
            tally = tallies.setdefault(parts[index], {})
            tally[kinds[index]] = tally.get(kinds[index], 0) + 1
    majority = {}
    for part, tally in tallies.items():
        kind, most = max(tally.items(), key=lambda item: item[1])
        if most / sum(tally.values()) >= part_sure:
            majority[part] = kind
    seen = [False] * count
    flecks = []
    for start in range(count):
        if seen[start] or not considered[start]:
            continue
        kind = kinds[start]
        island = [start]
        seen[start] = True
        head = 0
        while head < len(island) and len(island) <= fleck_max:
            here = island[head]
            head += 1
            x, y = here % width, here // width
            for there in ((here - 1) if x > 0 else -1, (here + 1) if x < width - 1 else -1, (here - width) if y > 0 else -1, (here + width) if y < height - 1 else -1):
                if there >= 0 and not seen[there] and considered[there] and kinds[there] == kind:
                    seen[there] = True
                    island.append(there)
        if len(island) > fleck_max:
            # Too big to be a fleck; mark the rest of it seen without measuring it further.
            while head < len(island):
                here = island[head]
                head += 1
                x, y = here % width, here // width
                for there in ((here - 1) if x > 0 else -1, (here + 1) if x < width - 1 else -1, (here - width) if y > 0 else -1, (here + width) if y < height - 1 else -1):
                    if there >= 0 and not seen[there] and considered[there] and kinds[there] == kind:
                        seen[there] = True
                        island.append(there)
            continue
        votes = {}
        for member in island:
            if parts[member]:
                votes[parts[member]] = votes.get(parts[member], 0) + 1
        if not votes:
            continue
        part = max(votes, key=votes.get)
        if part not in majority or majority[part] == kind:
            continue
        # Only an island inside the panel, ringed on every side by the panel's own paint, is what the eye calls a fleck: one that touches
        # outline (a bolt head drawn in its own ring), the edge of the part or another part is a detail or a seam, not a fleck.
        members = set(island)
        inside = True
        for member in island:
            x, y = member % width, member // width
            for there in ((member - 1) if x > 0 else -1, (member + 1) if x < width - 1 else -1, (member - width) if y > 0 else -1, (member + width) if y < height - 1 else -1):
                if there in members:
                    continue
                if there < 0 or not considered[there] or parts[there] != part or kinds[there] != majority[part]:
                    inside = False
                    break
            if not inside:
                break
        if inside and (visible is None or visible(island, majority[part])):
            flecks.append(island)
    return flecks


def drawn(texture_pixel, mask_pixel, colour, accent):
    """Roughly how the game's pattern shader draws one pixel: its texture times the main colour by the mask's red, the accent by its green."""
    red_weight, green_weight = mask_pixel[0] / 255, mask_pixel[1] / 255
    plain = max(0.0, 1 - red_weight - green_weight)
    return tuple(texture_pixel[c] * (plain + red_weight * colour[c] / 255 + green_weight * accent[c] / 255) for c in range(3))


def check(name, folder, master, base, colour=(255, 255, 255), accent=(255, 255, 255)):
    texture = Image.open(base / f'{name}_north.png').convert('RGBA')
    mask = Image.open(base / f'{name}_northm.png').convert('RGB')
    transform = json.loads((repaint_vehicles.EXPORT / f'{name}.transform.json').read_text(encoding='utf-8'))
    map_path = art_paths.find('segments', f'{file_sha256(master)}.png') or art_paths.work('segments', f'{file_sha256(master)}.png')
    if not map_path.is_file():
        return None, f'no parts map: run scripts/art/paint_segments.py on {master.name}'
    labels = Image.open(map_path)
    width, height = texture.size
    scale, (left, top) = transform['scale'], transform['offset']
    master_width, master_height = labels.size
    label_pixels = labels.load()
    pixels = texture.load()
    mask_pixels = mask.load()
    kinds, parts, considered = [], [], []
    for y in range(height):
        for x in range(width):
            red, green, blue, alpha = pixels[x, y]
            kinds.append(paint_of(mask_pixels[x, y]))
            mx, my = int((x + 0.5 - left) / scale), int((y + 0.5 - top) / scale)
            parts.append(label_pixels[mx, my] if 0 <= mx < master_width and 0 <= my < master_height else 0)
            considered.append(alpha >= 128 and max(red, green, blue) / 255 >= OUTLINE_VALUE)
    full = {'main': (255, 0, 0), 'accent': (0, 255, 0), None: (0, 0, 0)}

    def visible(island, paint):
        for index in island:
            x, y = index % width, index // width
            now = drawn(pixels[x, y], mask_pixels[x, y], colour, accent)
            painted = drawn(pixels[x, y], full[paint], colour, accent)
            if max(abs(a - b) for a, b in zip(now, painted)) >= VISIBLE:
                return True
        return False

    flecks = find_flecks(kinds, parts, considered, width, height, visible=visible)
    OUT.mkdir(parents=True, exist_ok=True)
    picture = Image.new('RGBA', texture.size, (62, 57, 47, 255))
    picture.alpha_composite(texture)
    draw = ImageDraw.Draw(picture)
    for island in flecks:
        x, y = island[0] % width, island[0] // width
        draw.ellipse((x - 5, y - 5, x + 5, y + 5), outline=(255, 0, 255, 255), width=2)
    picture.save(OUT / f'{name}.png')
    return flecks, None


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--stage', choices=('regions', 'repaired', 'parts', 'pixels'), help='check the staged bodies of this paint instead of the shipped ones')
    parser.add_argument('--only', action='append', default=[])
    parser.add_argument('--allow', type=int, default=0, help='flecks a body may have before the check fails')
    args = parser.parse_args(argv)
    failed = False
    for name, folder, master, *_rest, colour, accent in repaint_vehicles.BODIES:
        if args.only and name not in args.only:
            continue
        base = (repaint_vehicles.STAGE / args.stage / folder) if args.stage else (repaint_vehicles.TEXTURES / folder)
        flecks, problem = check(name, folder, repaint_vehicles.GEN / master, base, colour, accent)
        if problem:
            print(f'{name}: {problem}')
            failed = True
            continue
        verdict = 'ok' if len(flecks) <= args.allow else 'FLECKS'
        failed |= len(flecks) > args.allow
        print(f'{name}: {len(flecks)} fleck{"" if len(flecks) == 1 else "s"} ({verdict}); picture in {(OUT / f"{name}.png").relative_to(ROOT).as_posix()}')
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
