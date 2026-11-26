"""Compare generated images with the source images behind them and flag anything close to a copy. Every candidate that drew on
game art passes through this before the owner sees it as a pick; a flagged image must not ship. Writes near-copy.json in the
output folder and exits 1 if anything is flagged. Needs Pillow and NumPy: run it with ComfyUI's bundled Python.

    <ComfyUI python> -I scripts/art/near_copy.py assets/phobos-medical/generated .local/references/vanilla-medical [more source folders]
    <ComfyUI python> -I scripts/art/near_copy.py --only bulk-tanker-body-north assets/phobos-vehicles/generated <source folders>

scripts/art-run.py runs this by itself after every batch, against the folders holding that batch's references, with --only for each
request folder the batch wrote to: without it every earlier batch in the generated folder is compared again (11 Nov 2026: a run of
four flagged 38 pictures from other batches). --only reads a folder's own images, not its subfolders, so review sheets kept beside
the attempts are left out.
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
    """Mean structural similarity over non-overlapping windows, at one fixed size, counting only windows where at least one
    picture has something drawn (sort_rules.compares_structure: blank-on-blank windows matched perfectly and flagged a thin
    cable against unrelated sprites, 12 Nov 2026)."""
    first = np.asarray(a.resize((COMPARE_SIZE, COMPARE_SIZE), Image.LANCZOS), dtype=np.float64)
    second = np.asarray(b.resize((COMPARE_SIZE, COMPARE_SIZE), Image.LANCZOS), dtype=np.float64)
    values = []
    for y in range(0, COMPARE_SIZE - WINDOW + 1, WINDOW):
        for x in range(0, COMPARE_SIZE - WINDOW + 1, WINDOW):
            p, q = first[y:y + WINDOW, x:x + WINDOW], second[y:y + WINDOW, x:x + WINDOW]
            if not sort_rules.compares_structure(p.min(), q.min()):
                continue
            covariance = ((p - p.mean()) * (q - q.mean())).mean()
            values.append(((2 * p.mean() * q.mean() + C1) * (2 * covariance + C2)) /
                          ((p.mean() ** 2 + q.mean() ** 2 + C1) * (p.var() + q.var() + C2)))
    return float(sort_rules.mean_or_zero(values))


def pngs(folder):
    return sorted(p for p in Path(folder).rglob('*.png') if not p.name.endswith('-sheet.png'))


def candidates(outputs, only=()):
    """The images to check: every one under outputs, or only those directly inside the named request folders."""
    if not only:
        return pngs(outputs)
    return [p for name in only for p in sorted((Path(outputs) / name).glob('*.png')) if not p.name.endswith('-sheet.png')]


def compare(outputs, *sources, only=(), own=()):
    """Returns (flags against others' art, likenesses to our own art, the closest source for each output). A source folder is our
    own when it carries sort_rules.OWN_ART_MARKER or is named with --own."""
    source_data = [(p, sort_rules.is_own_art_folder(folder, own), *_prepared(p)) for folder in sources for p in pngs(folder)]
    found, closest = [], []
    for output in candidates(outputs, only):
        image, value = _prepared(output)
        best = None
        for source, ours, source_image, source_hash in source_data:
            distance = sort_rules.hamming(value, source_hash)
            similarity = ssim(image, source_image)
            alike = sort_rules.same_proportions(_ratio(image), _ratio(source_image))
            entry = {'output': output.name, 'source': source.name, 'hash_distance': distance, 'ssim': round(similarity, 3),
                     'same_proportions': alike, 'own': ours}
            if sort_rules.is_near_copy(distance, similarity, proportions_agree=alike):
                found.append(entry)
            if best is None or similarity > best['ssim']:
                best = entry
        if best:
            closest.append(best)
    flagged, close_to_own = sort_rules.split_flags(found)
    return flagged, close_to_own, closest


def mark_sidecars(checked, flagged):
    """Writes each checked try's flags against others' art into its own record (the .json beside it), and takes away a mark a
    recheck no longer finds. near-copy.json holds only the latest batch, so a flag must live with the picture itself for the
    picture cards to leave it out later (27 Nov 2026: night 16's two flagged supply-run tries would otherwise have been offered).
    A record with no flag and no old mark is never rewritten, so it stays byte for byte as generated."""
    by_name = {}
    for entry in flagged:
        by_name.setdefault(entry['output'], []).append(
            {'source': entry['source'], 'hash_distance': entry['hash_distance'], 'ssim': entry['ssim']})
    marked = 0
    for picture in checked:
        sidecar = picture.with_suffix('.json')
        if not sidecar.is_file():
            continue
        record = json.loads(sidecar.read_text(encoding='utf-8'))
        flags = by_name.get(picture.name)
        if flags:
            record[sort_rules.NEAR_COPY_RECORD_KEY] = flags
            marked += 1
        elif sort_rules.NEAR_COPY_RECORD_KEY in record:
            del record[sort_rules.NEAR_COPY_RECORD_KEY]
        else:
            continue
        sidecar.write_text(json.dumps(record, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    return marked


def _ratio(image):
    """Width to height of a subject already cropped by flattened_gray."""
    return image.width / image.height if image.height else 0.0


def _prepared(path):
    gray = flattened_gray(path)
    return gray, sort_candidates.difference_hash(gray)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--only', action='append', default=[], metavar='FOLDER',
                        help='check only the images directly inside this request folder of outputs; repeat for more')
    parser.add_argument('--own', action='append', default=[], metavar='FOLDER',
                        help='a source folder holding only our own art: likenesses to it are reported apart and never block a pick')
    parser.add_argument('outputs')
    parser.add_argument('sources', nargs='+')
    args = parser.parse_args()
    flagged_entries, own_entries, closest_entries = compare(args.outputs, *args.sources, only=args.only, own=args.own)
    report = {'flagged': flagged_entries, 'close_to_own': own_entries, 'closest_per_output': closest_entries,
              'thresholds': {'hash_distance_at_most': sort_rules.NEAR_COPY_DISTANCE, 'ssim_at_least': sort_rules.NEAR_COPY_SSIM,
                             'hash_needs_proportions_within': sort_rules.SAME_PROPORTIONS}}
    (Path(args.outputs) / 'near-copy.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    mark_sidecars(candidates(args.outputs, args.only), flagged_entries)
    print(f'{len(closest_entries)} outputs compared, {len(flagged_entries)} near-copy flags, '
          f'{len(own_entries)} likenesses to our own art')
    sys.exit(1 if flagged_entries else 0)
