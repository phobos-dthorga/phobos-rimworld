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


# Masks from the background AI (scripts/art/clean_backgrounds.py), named by the SHA-256 of the picture they were made from: white where
# the picture shows its object, black where it shows background. Kept on this PC only.
AI_MASKS = Path(__file__).resolve().parents[2] / '.local' / 'art' / 'bgmasks'
AI_SEED_BELOW = 64  # The AI is sure this is background: clearing may start here (authored, 12 Nov 2026).
AI_OBJECT_FROM = 192  # The AI is sure this is the object: clearing never enters it, however white it is (authored).
AI_WHOLE_PATCH = 0.9  # An enclosed white patch is cleared only if the AI would clear this share of it or more, else none (authored).


def file_sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def ai_background(path):
    """The background AI's mask for this picture file, or None if the AI has not looked at it."""
    mask = AI_MASKS / f'{file_sha256(path)}.png'
    return Image.open(mask).convert('L') if mask.is_file() else None


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
    sprite = pad_to_aspect(crop_to_content(cut), aspect_w, aspect_h)
    # Shrunk premultiplied, so the clear background's white never bleeds into the edge (12 Nov 2026: it left a pale rim on every icon).
    sprite = sprite.convert('RGBa').resize((size * aspect_w // min(aspect_w, aspect_h), size * aspect_h // min(aspect_w, aspect_h)),
                                           Image.LANCZOS).convert('RGBA')
    target.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(target, optimize=True)
    record = {
        'target': str(target).replace('\\', '/'),
        'source': str(source).replace('\\', '/'),
        'source_sha256': file_sha256(source),
        'target_sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
        'size': list(sprite.size),
        'white_threshold': threshold,
        'soft_edge': soft,
        'all_white': all_white,
        'silhouette': as_silhouette,
        'ai_mask': file_sha256(source) if background is not None else None,
        'date': _dt.datetime.now().isoformat(timespec='seconds'),
        'tool': 'export_sprite.py: ' + ('dark shape to a white silhouette' if as_silhouette else 'background white to alpha') + ', crop, pad, Lanczos downscale',
    }
    target.with_suffix('.export.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'wrote {target} {sprite.size}')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('source')
    parser.add_argument('target')
    parser.add_argument('--size', type=int, default=128, help='pixels along the shorter side')
    parser.add_argument('--aspect', default='1:1', help='width:height of the sprite, e.g. 1:1 or 2:5')
    parser.add_argument('--threshold', type=int, default=235)
    parser.add_argument('--soft', type=int, default=30)
    parser.add_argument('--all-white', action='store_true', help='clear white inside the object too')
    parser.add_argument('--silhouette', action='store_true', help='a white shape from a dark one, for a picture the game tints')
    args = parser.parse_args()
    export(args.source, args.target, args.size, args.aspect, args.threshold, args.soft, args.all_white, args.silhouette)
