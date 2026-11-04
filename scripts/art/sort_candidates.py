"""Measure every candidate in a generated folder and write _sort.json, which the review page uses to show the promising ones
first and mark repeats. Deletes nothing. Needs Pillow: run it with ComfyUI's bundled Python.

    <ComfyUI python> -I scripts/art/sort_candidates.py assets/phobos-medical/generated/oxygen-concentrator
"""
import argparse
import json
from pathlib import Path
import sys

from PIL import Image, ImageFilter, ImageStat

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sort_rules  # noqa: E402

NEAR_WHITE = 235  # The same threshold export_sprite.py treats as background.
HASH_SIZE = 8


def difference_hash(gray):
    """64 bits: whether each pixel of an 9x8 thumbnail is brighter than its right-hand neighbour."""
    small = gray.resize((HASH_SIZE + 1, HASH_SIZE), Image.LANCZOS)
    pixels = small.load()
    value = 0
    for y in range(HASH_SIZE):
        for x in range(HASH_SIZE):
            value = (value << 1) | (1 if pixels[x, y] > pixels[x + 1, y] else 0)
    return value


def measure(path):
    image = Image.open(path).convert('RGB')
    gray = image.convert('L')
    sharpness = ImageStat.Stat(gray.filter(ImageFilter.FIND_EDGES)).var[0]
    width, height = gray.size
    pixels = gray.load()
    border = [pixels[x, y] for x in range(width) for y in (0, height - 1)] + [pixels[x, y] for y in range(height) for x in (0, width - 1)]
    border_white = sum(1 for value in border if value >= NEAR_WHITE) / len(border)
    box = gray.point(lambda value: 255 if value < NEAR_WHITE else 0).getbbox()
    content = ((box[2] - box[0]) * (box[3] - box[1]) / (width * height)) if box else 0.0
    return {'file': path.name, 'sharpness': round(sharpness, 1), 'border_white': round(border_white, 3), 'content': round(content, 3),
            'hash': difference_hash(gray)}


def sort_folder(folder):
    folder = Path(folder)
    files = sorted(p for p in folder.glob('*.png') if not p.name.endswith('-sheet.png'))
    ranked = sort_rules.rank([measure(p) for p in files])
    (folder / '_sort.json').write_text(json.dumps(ranked, indent=2), encoding='utf-8')
    print(f'sorted {len(ranked)} in {folder.name}')
    return ranked


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('folders', nargs='+')
    for folder_name in parser.parse_args().folders:
        sort_folder(folder_name)
