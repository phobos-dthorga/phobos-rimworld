"""Turn an approved generated master (object on a white background) into a game sprite.

The white background becomes transparent (soft edges kept, white inside the object stays), the image is cropped to its content with a margin, padded
to the requested aspect, downscaled with Lanczos to the game size and written with a record beside it.
Needs Pillow: run it with ComfyUI's bundled Python when the repository Python lacks it:

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe scripts/art/export_sprite.py \
        assets/phobos-radio/generated/base-station/base-station-2.png \
        mods/PhobosRadioFramework/Common/Textures/Phobos/Radio/Things/BaseStation.png --size 128 --aspect 1:1
"""
import argparse
import datetime as _dt
import hashlib
import json
from pathlib import Path

from PIL import Image


import sys  # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402

# Masks from the background AI (scripts/art/clean_backgrounds.py), named by the SHA-256 of the picture they were made from: white where
# the picture shows its object, black where it shows background. Kept on this PC only, in the art tools' working folder (art_paths.py;
# K: on the owner's PC since 19 Nov 2026); masks made before the move are still read from .local/art/bgmasks.
AI_MASKS = art_paths.work('bgmasks')
AI_SEED_BELOW = 64  # The AI is sure this is background: clearing may start here (authored, 12 Nov 2026).
AI_OBJECT_FROM = 192  # The AI is sure this is the object: clearing never enters it, however white it is (authored).
AI_WHOLE_PATCH = 0.9  # An enclosed white patch is cleared only if the AI would clear this share of it or more, else none (authored).
# White the exporter kept inside the object that may be background after all (21 Nov 2026: the clutch kit's centre hole, and the
# run-flat tyres' since 12 Nov, showed white over the ground). Real white parts are cel-shaded off-white; background left behind is
# the master's own flat background colour. Measured on 13 exports: the two faults kept 1.0% and 5.4% of the master at 86% and 99%
# background colour; real white parts (a steriliser's door, a survey dish) matched too, and every other kept patch was under 0.6% or
# under 57%. The sweep of all 193 shipped sprites then warned on 14: three were holes showing white (the run-flat tyres, the service
# spanner's ring at 0.4%, the long gearing's big gear at 0.3%), the rest real white parts (planes, paper, glassware). So it warns,
# asking for a look on coloured ground, and never refuses (authored thresholds).
WHITE_LEFT_SHARE = 0.0025  # A kept white patch this share of the master or more is worth a look.
WHITE_LEFT_FLAT = 0.8  # ... when this share of it or more is within WHITE_LEFT_TOLERANCE of the background's own colour.
WHITE_LEFT_TOLERANCE = 3


REPO = Path(__file__).resolve().parents[2]


def repo_path(path):
    """A path for a record: relative to the repository when inside it, else its file name alone, so no machine path is ever
    published (20 Nov 2026: 43 committed records held the owner's full paths)."""
    resolved = Path(path).resolve()
    try:
        return resolved.relative_to(REPO).as_posix()
    except ValueError:
        pass
    # The art tools' working folder (K: on the owner's PC): relative to it, which clean_backgrounds.resolve reads back through art_paths.
    import art_paths
    try:
        return resolved.relative_to(art_paths.work_root().resolve()).as_posix()
    except ValueError:
        return resolved.name


def file_sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def ai_background(path):
    """The background AI's mask for this picture file, or None if the AI has not looked at it."""
    mask = art_paths.find('bgmasks', f'{file_sha256(path)}.png')
    return Image.open(mask).convert('L') if mask else None


def unmixed(value, alpha):
    """One colour channel of a pixel that was part white background, with the white taken out: the pixel showed alpha * object +
    (1 - alpha) * white, so the object's own colour is recovered exactly. Without it, a soft edge keeps its whiteness and shows as a
    pale rim over dark ground (owner, 12 Nov 2026: white "on the borders")."""
    if alpha <= 0:
        return value
    share = alpha / 255
    return max(0, min(255, round((value - 255 * (1 - share)) / share)))


def white_to_alpha(image, threshold=235, soft=30, all_white=False, holes=(), background=None):
    """Alpha from how far a pixel is from pure white, for the background only: near-white reached from the image's edge. White inside
    the object, such as a mattress, stays solid. Fully opaque below threshold-soft, clear above threshold. all_white clears every
    near-white pixel, for an object with no white of its own and background showing through it, such as a loop of tube. holes names
    (x, y) points of background enclosed by the object, each cleared as the edge is, such as the frame of a trailer's drawbar
    (12 Nov 2026). background is the background AI's mask for the picture (ai_background): white the object encloses is cleared too
    where the AI is sure it is background, spreading only through white it does not take for the object, so a hook's opening clears
    and a bed sheet beside it stays. A soft edge has the white taken out of its colour (unmixed)."""
    rgba = image.convert('RGBA')
    pixels = rgba.load()
    width, height = rgba.size
    floor = threshold - soft

    def clear(x, y):
        r, g, b, a = pixels[x, y]
        whiteness = min(r, g, b)
        alpha = min(a, 0 if whiteness >= threshold else int(255 * (threshold - whiteness) / soft))
        pixels[x, y] = (unmixed(r, alpha), unmixed(g, alpha), unmixed(b, alpha), alpha)

    if all_white:
        for y in range(height):
            for x in range(width):
                if min(pixels[x, y][:3]) > floor:
                    clear(x, y)
        return rgba
    seen = bytearray(width * height)

    def flood(stack, allowed=lambda x, y: True):
        while stack:
            x, y = stack.pop()
            if seen[y * width + x]:
                continue
            if min(pixels[x, y][:3]) <= floor or not allowed(x, y):
                continue
            seen[y * width + x] = 1
            clear(x, y)
            stack += [(nx, ny) for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)) if 0 <= nx < width and 0 <= ny < height]

    stack = [(x, y) for x in range(width) for y in (0, height - 1)] + [(x, y) for y in range(height) for x in (0, width - 1)]
    stack += [(int(x), int(y)) for x, y in holes if 0 <= x < width and 0 <= y < height]
    flood(stack)
    if background is not None:
        edge_seen = bytes(seen)
        original = rgba.copy().load()
        sure = background.convert('L').resize((width, height), Image.BILINEAR).load()
        seeds = [(x, y) for y in range(height) for x in range(width)
                 if not seen[y * width + x] and sure[x, y] < AI_SEED_BELOW and min(pixels[x, y][:3]) >= threshold]
        flood(seeds, lambda x, y: sure[x, y] < AI_OBJECT_FROM)
        _whole_patches_only(original, pixels, edge_seen, seen, width, height, threshold)
    return rgba


def _neighbours(index, width, height):
    x, y = index % width, index // width
    return [ny * width + nx for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)) if 0 <= nx < width and 0 <= ny < height]


def _whole_patches_only(original, pixels, edge_seen, seen, width, height, threshold):
    """Puts back every enclosed white patch the AI cleared only part of. Measured 12 Nov 2026: where the AI is right (a hook's opening,
    a gear's holes, the sling set's triangle) it clears the whole patch; where it is wrong (a clipboard's paper, a gauge's face, a dish)
    it is unsure and clears blotches. So a patch, near-pure white joined to near-pure white, is cleared whole or not at all; when it is
    put back, so is every pixel the AI cleared that joins it, its soft rim included. When unsure, the white stays."""
    def white(index):
        return min(original[index % width, index // width][:3]) >= threshold

    by_ai = lambda index: seen[index] and not edge_seen[index]
    done = bytearray(width * height)
    for start in range(width * height):
        if done[start] or edge_seen[start] or not white(start):
            continue
        patch, stack = [], [start]
        done[start] = 1
        while stack:
            index = stack.pop()
            patch.append(index)
            for neighbour in _neighbours(index, width, height):
                if not done[neighbour] and not edge_seen[neighbour] and white(neighbour):
                    done[neighbour] = 1
                    stack.append(neighbour)
        cleared = sum(1 for index in patch if by_ai(index))
        if not cleared or cleared >= AI_WHOLE_PATCH * len(patch):
            continue
        stack = [index for index in patch if by_ai(index)]
        restored = set(stack)
        while stack:
            index = stack.pop()
            pixels[index % width, index // width] = original[index % width, index // width]
            for neighbour in _neighbours(index, width, height):
                if neighbour not in restored and by_ai(neighbour):
                    restored.add(neighbour)
                    stack.append(neighbour)


def silhouette(image, threshold=235, soft=30):
    """A white shape on transparency from a dark shape on white, for pictures the game tints, such as the dashboard's warning lights: opacity
    comes from darkness, so anti-aliased edges stay soft and white details inside the shape become holes."""
    grey = image.convert('L')
    floor = threshold - soft * 6  # Dark enough to count as solid; generated black is rarely pure black.
    alpha = grey.point(lambda v: 255 if v <= floor else 0 if v >= threshold else int(255 * (threshold - v) / (threshold - floor)))
    shape = Image.new('RGBA', image.size, (255, 255, 255, 0))
    shape.putalpha(alpha)
    return shape


def background_colour(image):
    """The median colour of the master's border: the plain background the generators draw on."""
    rgb = image.convert('RGB')
    width, height = rgb.size
    pixels = rgb.load()
    border = [pixels[x, y] for x in range(width) for y in (0, height - 1)] + [pixels[x, y] for y in range(height) for x in (0, width - 1)]
    return tuple(sorted(colour[channel] for colour in border)[len(border) // 2] for channel in range(3))


def white_left(master, cut, threshold=235):
    """The white patches the cut kept opaque that look like background left behind (WHITE_LEFT_*), largest first, each as
    {"share" of the master, "flat" share in the background's colour, "at" one of its pixels}. Empty when nothing looks left."""
    rgb = master.convert('RGB').load()
    alpha = cut.getchannel('A').load()
    width, height = cut.size
    background = background_colour(master)
    kept = lambda x, y: alpha[x, y] >= 250 and min(rgb[x, y]) >= threshold
    seen = bytearray(width * height)
    found = []
    for start in range(width * height):
        x, y = start % width, start // width
        if seen[start] or not kept(x, y):
            continue
        stack, size, flat = [start], 0, 0
        seen[start] = 1
        while stack:
            index = stack.pop()
            cx, cy = index % width, index // width
            size += 1
            flat += max(abs(a - b) for a, b in zip(rgb[cx, cy], background)) <= WHITE_LEFT_TOLERANCE
            for neighbour in _neighbours(index, width, height):
                if not seen[neighbour] and kept(neighbour % width, neighbour // width):
                    seen[neighbour] = 1
                    stack.append(neighbour)
        share, flat_share = size / (width * height), flat / size
        if share >= WHITE_LEFT_SHARE and flat_share >= WHITE_LEFT_FLAT:
            found.append({'share': round(share, 4), 'flat': round(flat_share, 2), 'at': [x, y]})
    return sorted(found, key=lambda patch: -patch['share'])


def white_left_warning(patches):
    """The console line for white_left's findings."""
    where = '; '.join(f'{patch["share"] * 100:.1f}% of the master at {patch["at"][0]},{patch["at"][1]}' for patch in patches)
    return (f'WARNING: white inside the object may be background left behind ({where}). Look at the sprite on coloured ground; '
            'if it is a hole, export again with --all-white, or clean its mask (clean_backgrounds.py)')


def crop_to_content(image, margin_fraction=0.06):
    box = image.getchannel('A').getbbox()
    if box is None:
        return image
    left, top, right, bottom = box
    margin = int(max(right - left, bottom - top) * margin_fraction)
    return image.crop((max(left - margin, 0), max(top - margin, 0), min(right + margin, image.width), min(bottom + margin, image.height)))


def pad_to_aspect(image, aspect_w, aspect_h):
    target_ratio = aspect_w / aspect_h
    width, height = image.size
    if width / height > target_ratio:
        new_h = int(round(width / target_ratio)); new_w = width
    else:
        new_w = int(round(height * target_ratio)); new_h = height
    canvas = Image.new('RGBA', (new_w, new_h), (0, 0, 0, 0))
    canvas.paste(image, ((new_w - width) // 2, (new_h - height) // 2))
    return canvas


def export(source, target, size, aspect, threshold, soft, all_white=False, as_silhouette=False):
    source = Path(source); target = Path(target)
    aspect_w, aspect_h = (int(part) for part in aspect.split(':'))
    master = Image.open(source)
    background = None if as_silhouette or all_white else ai_background(source)
    cut = silhouette(master, threshold, soft) if as_silhouette else white_to_alpha(master, threshold, soft, all_white, background=background)
    # A silhouette is white on purpose, and --all-white keeps no white at all: only an ordinary cut can leave background behind.
    left = [] if as_silhouette or all_white else white_left(master, cut, threshold)
    sprite = pad_to_aspect(crop_to_content(cut), aspect_w, aspect_h)
    # Shrunk premultiplied, so the clear background's white never bleeds into the edge (12 Nov 2026: it left a pale rim on every icon).
    sprite = sprite.convert('RGBa').resize((size * aspect_w // min(aspect_w, aspect_h), size * aspect_h // min(aspect_w, aspect_h)),
                                           Image.LANCZOS).convert('RGBA')
    target.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(target, optimize=True)
    record = {
        'target': repo_path(target),
        'source': repo_path(source),
        'source_sha256': file_sha256(source),
        'target_sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
        'size': list(sprite.size),
        'white_threshold': threshold,
        'soft_edge': soft,
        'all_white': all_white,
        'silhouette': as_silhouette,
        'ai_mask': file_sha256(source) if background is not None else None,
        'white_left': left,
        'date': _dt.datetime.now().isoformat(timespec='seconds'),
        'tool': 'export_sprite.py: ' + ('dark shape to a white silhouette' if as_silhouette else 'background white to alpha') + ', crop, pad, Lanczos downscale',
    }
    target.with_suffix('.export.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'wrote {target} {sprite.size}')
    if left:
        print(white_left_warning(left))
    return record


def sweep(root):
    """Every shipped sprite's record under root, its master cut again as it was exported, and the ones that look as if they kept
    background white (white_left). Reads only; a master no longer on this PC is listed as such."""
    suspects, missing, checked = [], [], 0
    for record_path in sorted(Path(root).rglob('*.export.json')):
        record = json.loads(record_path.read_text(encoding='utf-8'))
        if record.get('silhouette') or record.get('all_white') or 'source' not in record:
            continue
        source = REPO / record['source']
        if not source.is_file():
            missing.append(record['target'])
            continue
        checked += 1
        master = Image.open(source)
        background = ai_background(source) if record.get('ai_mask') else None
        cut = white_to_alpha(master, record.get('white_threshold', 235), record.get('soft_edge', 30), background=background)
        left = white_left(master, cut, record.get('white_threshold', 235))
        if left:
            suspects.append((record['target'], left))
    print(f'{checked} sprites checked, {len(suspects)} may show background white, {len(missing)} masters not on this PC')
    for target, left in suspects:
        print(f'- {target}: ' + '; '.join(f'{patch["share"] * 100:.1f}% at {patch["at"][0]},{patch["at"][1]}' for patch in left))
    return suspects


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('source', nargs='?')
    parser.add_argument('target', nargs='?')
    parser.add_argument('--sweep', nargs='?', const='mods', metavar='FOLDER',
                        help='instead of exporting, list every shipped sprite under FOLDER (default mods) that may show background white')
    parser.add_argument('--size', type=int, default=128, help='pixels along the shorter side')
    parser.add_argument('--aspect', default='1:1', help='width:height of the sprite, e.g. 1:1 or 2:5')
    parser.add_argument('--threshold', type=int, default=235)
    parser.add_argument('--soft', type=int, default=30)
    parser.add_argument('--all-white', action='store_true', help='clear white inside the object too')
    parser.add_argument('--silhouette', action='store_true', help='a white shape from a dark one, for a picture the game tints')
    args = parser.parse_args()
    if args.sweep:
        sweep(REPO / args.sweep if not Path(args.sweep).is_absolute() else args.sweep)
    elif not (args.source and args.target):
        parser.error('give a source and a target, or --sweep')
    else:
        export(args.source, args.target, args.size, args.aspect, args.threshold, args.soft, args.all_white, args.silhouette)
