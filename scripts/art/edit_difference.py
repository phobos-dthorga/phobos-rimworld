"""Lift an added part out of an edited picture by comparing it with the picture it was edited from.

    python scripts/art/edit_difference.py BODY EDIT OUT.png [--box x0,y0,x1,y1] [--threshold 40] [--line 9]

Found 18 Nov 2026 with the Osprey's overlays: an editing model asked to draw a part "alone, on white" redraws the whole aircraft with
the part added instead. The redraw matches the original closely: compared with it, what differs is thin outline lines everywhere
(the redraw's small shifts) and one solid shape where the part was added. So the part is what differs, once lines thinner than
--line pixels are taken away. The original picture is never changed, and the edit is kept byte for byte (the art rule); the
result is a new file, the part on transparency, with a record beside it.

--box keeps only what lies inside x0,y0,x1,y1 (shares of the picture, 0 to 1), for a part whose place is known. Pure Pillow.
"""
import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def difference(body, edit):
    """The largest difference in any channel, pixel by pixel, as a greyscale picture."""
    r, g, b = ImageChops.difference(edit, body).split()
    return ImageChops.lighter(ImageChops.lighter(r, g), b)


def part_mask(diff, threshold, line, box=None):
    """Where the part is: a strong difference, with thin lines taken away and the part's own outline given back."""
    strong = diff.point(lambda v: 255 if v >= threshold else 0)
    solid = strong.filter(ImageFilter.MinFilter(line)).filter(ImageFilter.MaxFilter(line))
    grown = solid.filter(ImageFilter.MaxFilter(line + 4))            # Back out over the part's own outline.
    weak = diff.point(lambda v: 255 if v >= threshold // 2 else 0)
    mask = ImageChops.multiply(grown, weak)
    if box:
        keep = Image.new('L', mask.size, 0)
        w, h = mask.size
        keep.paste(255, (int(box[0] * w), int(box[1] * h), int(box[2] * w), int(box[3] * h)))
        mask = ImageChops.multiply(mask, keep)
    return mask.filter(ImageFilter.GaussianBlur(0.8))                # A soft edge one pixel wide, as the exporters give.


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('body')
    parser.add_argument('edit')
    parser.add_argument('out')
    parser.add_argument('--box', help='x0,y0,x1,y1 as shares of the picture')
    parser.add_argument('--threshold', type=int, default=40, help='the difference, 0 to 255, that counts as changed')
    parser.add_argument('--line', type=int, default=9, help='pixels: changes thinner than this are the redraw, not the part')
    args = parser.parse_args(argv)
    edit = Image.open(args.edit).convert('RGB')
    body = Image.open(args.body).convert('RGB')
    if body.size != edit.size:
        body = body.resize(edit.size, Image.LANCZOS)
    box = [float(v) for v in args.box.split(',')] if args.box else None
    mask = part_mask(difference(body, edit), args.threshold, args.line, box)
    part = edit.convert('RGBA')
    part.putalpha(mask)
    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    part.save(out)
    bounds = mask.point(lambda v: 255 if v >= 128 else 0).getbbox()
    record = {'made_by': 'scripts/art/edit_difference.py (Pillow; no generator)', 'body': args.body.replace('\\', '/'),
              'body_sha256': sha(args.body), 'edit': args.edit.replace('\\', '/'), 'edit_sha256': sha(args.edit),
              'threshold': args.threshold, 'line': args.line, 'box': box, 'part_bounds': list(bounds) if bounds else None,
              'sha256': sha(out)}
    out.with_suffix('.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'{out}: part within {bounds}')
    return 0 if bounds else 1


if __name__ == '__main__':
    raise SystemExit(main())
