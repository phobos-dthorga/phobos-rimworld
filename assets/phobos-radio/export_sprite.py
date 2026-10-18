"""Turn an approved generated master (object on a white background) into a game sprite.

White becomes transparent (soft edges kept), the image is cropped to its content with a margin, padded
to the requested aspect, downscaled with Lanczos to the game size and written with a record beside it.
Needs Pillow: run it with ComfyUI's bundled Python when the repository Python lacks it:

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe assets/phobos-radio/export_sprite.py \
        assets/phobos-radio/generated/base-station/base-station-2.png \
        mods/PhobosRadioFramework/Common/Textures/Phobos/Radio/Things/BaseStation.png --size 128 --aspect 1:1
"""
import argparse
import datetime as _dt
import hashlib
import json
from pathlib import Path

from PIL import Image


def white_to_alpha(image, threshold=235, soft=30):
    """Alpha from how far a pixel is from pure white: fully opaque below threshold-soft, clear above threshold."""
    rgba = image.convert('RGBA')
    pixels = rgba.load()
    width, height = rgba.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            whiteness = min(r, g, b)
            if whiteness >= threshold:
                alpha = 0
            elif whiteness <= threshold - soft:
                alpha = 255
            else:
                alpha = int(255 * (threshold - whiteness) / soft)
            pixels[x, y] = (r, g, b, min(a, alpha))
    return rgba


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


def export(source, target, size, aspect, threshold, soft):
    source = Path(source); target = Path(target)
    aspect_w, aspect_h = (int(part) for part in aspect.split(':'))
    master = Image.open(source)
    sprite = pad_to_aspect(crop_to_content(white_to_alpha(master, threshold, soft)), aspect_w, aspect_h)
    sprite = sprite.resize((size * aspect_w // min(aspect_w, aspect_h), size * aspect_h // min(aspect_w, aspect_h)), Image.LANCZOS)
    target.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(target, optimize=True)
    record = {
        'target': str(target).replace('\\', '/'),
        'source': str(source).replace('\\', '/'),
        'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
        'target_sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
        'size': list(sprite.size),
        'white_threshold': threshold,
        'soft_edge': soft,
        'date': _dt.datetime.now().isoformat(timespec='seconds'),
        'tool': 'export_sprite.py: white to alpha, crop, pad, Lanczos downscale',
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
    args = parser.parse_args()
    export(args.source, args.target, args.size, args.aspect, args.threshold, args.soft)
