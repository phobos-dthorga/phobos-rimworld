"""Lay out every candidate of one generated request as a labelled contact sheet, for review.

    <ComfyUI python> assets/phobos-radio/contact_sheet.py base-station [--out path] [--thumb 512]

Reads assets/phobos-radio/generated/<name>/*.png and writes <name>-sheet.png beside them (ignored by
git through the generated folder's own .gitignore). Needs Pillow.
"""
import argparse
import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
GENERATED = ROOT / 'assets' / 'phobos-radio' / 'generated'


def sheet(name, thumb, out=None):
    folder = GENERATED / name
    files = sorted(p for p in folder.glob(f'{name}-*.png') if not p.name.endswith('-sheet.png'))
    if not files:
        raise FileNotFoundError(f'no candidates under {folder}')
    columns = min(len(files), 4)
    rows = math.ceil(len(files) / columns)
    label_h = 28
    canvas = Image.new('RGB', (columns * thumb, rows * (thumb + label_h)), (0x17, 0x18, 0x14))
    draw = ImageDraw.Draw(canvas)
    for i, path in enumerate(files):
        image = Image.open(path).convert('RGB')
        image.thumbnail((thumb, thumb), Image.LANCZOS)
        x = (i % columns) * thumb
        y = (i // columns) * (thumb + label_h)
        canvas.paste(image, (x + (thumb - image.width) // 2, y + label_h + (thumb - image.height) // 2))
        draw.text((x + 8, y + 6), path.stem, fill=(0xE6, 0xE1, 0xD3))
    target = Path(out) if out else folder / f'{name}-sheet.png'
    canvas.save(target, optimize=True)
    print(f'wrote {target} ({len(files)} candidates)')
    return target


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('name')
    parser.add_argument('--out')
    parser.add_argument('--thumb', type=int, default=512)
    args = parser.parse_args()
    sheet(args.name, args.thumb, args.out)
