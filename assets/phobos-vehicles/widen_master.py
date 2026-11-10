"""Widen a generated vehicle master across its length, for a body the generator drew slimmer than asked. Free, and never touches the
master: it writes a new file beside a record of what was done.

    python assets/phobos-vehicles/widen_master.py MASTER OUT --factor 1.35 [--paint-out L T R B ...]

The picture is stretched sideways about the vehicle's own centre line (found from the non-white pixels), on a canvas of the same size,
so the length and the bow and stern stay where the generator put them. --paint-out fills a box (in the widened picture's pixels) with the
median colour of a ring just outside it, for small stray marks such as glyphs the prompt forbade; give one box per mark.
Run with the Python that has Pillow and NumPy (ComfyUI's, on the owner's PC).
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image

WHITE_FLOOR = 245  # Paler than this on every channel is the white background.
RING = 6  # Pixels around a painted-out box whose colour fills it.


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def content_columns(image):
    pixels = np.asarray(image.convert('RGB'))
    solid = (pixels < WHITE_FLOOR).any(axis=2)
    columns = np.where(solid.any(axis=0))[0]
    return int(columns.min()), int(columns.max())


def widen(image, factor):
    left, right = content_columns(image)
    centre = (left + right) / 2
    width, height = image.size
    stretched = image.resize((round(width * factor), height), Image.LANCZOS)
    offset = round(centre * factor - centre)
    canvas = Image.new('RGB', (width, height), (255, 255, 255))
    canvas.paste(stretched.crop((offset, 0, offset + width, height)), (0, 0))
    new_left, new_right = content_columns(canvas)
    if new_left == 0 or new_right == width - 1:
        raise SystemExit(f'widened by {factor} the vehicle would run off the canvas; use a smaller factor')
    return canvas, (left, right), (new_left, new_right)


def paint_out(image, box):
    pixels = np.asarray(image).copy()
    left, top, right, bottom = box
    ring = np.concatenate([
        pixels[top - RING:top, left - RING:right + RING].reshape(-1, 3),
        pixels[bottom:bottom + RING, left - RING:right + RING].reshape(-1, 3),
        pixels[top:bottom, left - RING:left].reshape(-1, 3),
        pixels[top:bottom, right:right + RING].reshape(-1, 3),
    ])
    pixels[top:bottom, left:right] = np.median(ring, axis=0).astype(np.uint8)
    return Image.fromarray(pixels)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('master', type=Path)
    parser.add_argument('out', type=Path)
    parser.add_argument('--factor', type=float, required=True)
    parser.add_argument('--paint-out', type=int, nargs=4, action='append', default=[], metavar=('LEFT', 'TOP', 'RIGHT', 'BOTTOM'))
    args = parser.parse_args()
    if args.out.resolve() == args.master.resolve():
        raise SystemExit('the master is never overwritten; name a new file')
    image, before, after = widen(Image.open(args.master).convert('RGB'), args.factor)
    for box in args.paint_out:
        image = paint_out(image, box)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    image.save(args.out)
    record = {
        'master': args.master.name, 'master_sha256': sha(args.master), 'factor': args.factor,
        'content_columns_before': before, 'content_columns_after': after, 'painted_out': args.paint_out,
        'output': args.out.name, 'output_sha256': sha(args.out),
    }
    args.out.with_suffix('.widen.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(json.dumps(record))


if __name__ == '__main__':
    main()
