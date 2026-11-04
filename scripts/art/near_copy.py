"""Compare generated images with the source images behind them and flag anything close to a copy. Every candidate that drew on
game art passes through this before the owner sees it as a pick; a flagged image must not ship. Writes near-copy.json in the
output folder and exits 1 if anything is flagged. Needs Pillow and NumPy: run it with ComfyUI's bundled Python.

    <ComfyUI python> -I scripts/art/near_copy.py assets/phobos-medical/generated .local/references/vanilla-medical [more source folders]

scripts/art-run.py runs this by itself after every batch, against the folders holding that batch's references.
"""
import argparse
import json
from pathlib import Path
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sort_candidates  # noqa: E402
import sort_rules  # noqa: E402

COMPARE_SIZE = 256
WINDOW = 8
C1, C2 = (0.01 * 255) ** 2, (0.03 * 255) ** 2  # The constants from Wang and colleagues' SSIM paper (2004).


def flattened_gray(path):
    """On white, because generated images are on white and sources may be transparent. Cropped to the subject, so a sprite
    filling its frame still lines up with the same shape drawn smaller in a 1024 px image."""
    image = Image.open(path).convert('RGBA')
    canvas = Image.new('RGBA', image.size, (255, 255, 255, 255))
    canvas.alpha_composite(image)
    gray = canvas.convert('L')
    box = gray.point(lambda value: 255 if value < sort_candidates.NEAR_WHITE else 0).getbbox()
    return gray.crop(box) if box else gray


def ssim(a, b):
    """Mean structural similarity over non-overlapping windows, at one fixed size."""
    first = np.asarray(a.resize((COMPARE_SIZE, COMPARE_SIZE), Image.LANCZOS), dtype=np.float64)
    second = np.asarray(b.resize((COMPARE_SIZE, COMPARE_SIZE), Image.LANCZOS), dtype=np.float64)
    values = []
    for y in range(0, COMPARE_SIZE - WINDOW + 1, WINDOW):
        for x in range(0, COMPARE_SIZE - WINDOW + 1, WINDOW):
            p, q = first[y:y + WINDOW, x:x + WINDOW], second[y:y + WINDOW, x:x + WINDOW]
            covariance = ((p - p.mean()) * (q - q.mean())).mean()
            values.append(((2 * p.mean() * q.mean() + C1) * (2 * covariance + C2)) /
                          ((p.mean() ** 2 + q.mean() ** 2 + C1) * (p.var() + q.var() + C2)))
    return float(np.mean(values))


def pngs(folder):
    return sorted(p for p in Path(folder).rglob('*.png') if not p.name.endswith('-sheet.png'))


def compare(outputs, *sources):
    source_data = [(p, *_prepared(p)) for folder in sources for p in pngs(folder)]
    flagged, closest = [], []
    for output in pngs(outputs):
        image, value = _prepared(output)
        best = None
        for source, source_image, source_hash in source_data:
            distance = sort_rules.hamming(value, source_hash)
            similarity = ssim(image, source_image)
            entry = {'output': output.name, 'source': source.name, 'hash_distance': distance, 'ssim': round(similarity, 3)}
            if sort_rules.is_near_copy(distance, similarity):
                flagged.append(entry)
            if best is None or similarity > best['ssim']:
                best = entry
        if best:
            closest.append(best)
    return flagged, closest


def _prepared(path):
    gray = flattened_gray(path)
    return gray, sort_candidates.difference_hash(gray)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('outputs')
    parser.add_argument('sources', nargs='+')
    args = parser.parse_args()
    flagged_entries, closest_entries = compare(args.outputs, *args.sources)
    report = {'flagged': flagged_entries, 'closest_per_output': closest_entries,
              'thresholds': {'hash_distance_at_most': sort_rules.NEAR_COPY_DISTANCE, 'ssim_at_least': sort_rules.NEAR_COPY_SSIM}}
    (Path(args.outputs) / 'near-copy.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(f'{len(closest_entries)} outputs compared, {len(flagged_entries)} near-copy flags')
    sys.exit(1 if flagged_entries else 0)
