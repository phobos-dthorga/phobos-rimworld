"""Find ragged edges in shipped pictures: loose specks and dashed fringes left round a cut-out.

    python scripts/art/edge_check.py check            every shipped texture under mods/; fails on one flagged and not accepted (the gate)
    python scripts/art/edge_check.py scan PATH ...    any pictures, each with its figures, flagged or not
    python scripts/art/edge_check.py check --list     every flagged texture, accepted ones too

The owner's review of 22 Nov 2026 found roof tents with parts "scrubbed out" and dithered edges, and rolled bags with a dashed fringe:
pieces of whatever they were cut from, a few pixels each, lying loose beside the picture. The game draws them at full size, so the owner
sees them. This check counts such pieces: little islands of solid pixels, separate from everything else (8-connected), no bigger than
SPECK_MOST pixels. A clean cut-out has none or one or two (a rivet, a lamp's glint); a fringe has dozens. A picture with at least
SPECKS_FLAG of them is flagged. It also counts loose strokes: bigger islands, but a line a pixel or two thick and small beside the
picture, as the armour kits' ghost outlines were (22 Nov 2026); one flags the picture. Islands are labelled with scipy (the owner
installed numpy and scipy on 23 Nov 2026, BSD licences), every picture in seconds, and each file's answer is kept by its SHA-256 in
the shared tools' cache, so the gate reads only new or changed pictures.

Pictures the owner has seen and kept (sparks, rain, a scatter of rubble drawn on purpose) are listed in config/edge-check.json with why.
Game-free apart from where it looks (mods/**/Textures): a next game names its own texture folders.
"""
from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import _modkit  # noqa: E402  (the shared tools' import path)
_modkit.ensure()
from phobos_modkit import cache  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
ACCEPTED = ROOT / 'config' / 'edge-check.json'
EIGHT = np.ones((3, 3), bool)  # islands are 8-connected
SOLID = 96            # alpha at or above this counts as part of the picture
SPECK_MOST = 24       # an island of at most this many pixels is a speck
SPECKS_FLAG = 6       # this many specks or more flags the picture
THIN = 0.15           # an island bigger than a speck that keeps no more than this share of itself through a 3 x 3 opening is a stroke...
STROKE_SHARE = 0.02   # ...when it is also no more than this share of the picture's solid pixels (never the picture itself)
STROKES_FLAG = 1      # one loose stroke flags the picture: the armour kits' ghost outlines were two to four (22 Nov 2026)
FAINT = 8             # alpha from this up to SOLID is faint: a soft edge's own pixels, or haze
HAZE_REACH = 2        # a faint pixel more than this many pixels from any solid one is haze, not a soft edge
HAZE_FLAG = 60        # this many haze pixels or more is worth a look (shown by --list, never a failure)...
HAZE_SHARE = 0.004    # ...when they are also at least this share of the solid pixels
VERSION = 3           # raise when the figures above change, so cached answers are worked out again


def flagged(m: dict) -> bool:
    """Loose specks or a loose stroke flag a picture. Haze is shown, never flagged: glass, a lamp's glow and a landing pad are faint on purpose."""
    return m['specks'] >= SPECKS_FLAG or m.get('strokes', 0) >= STROKES_FLAG


def hazy(m: dict) -> bool:
    return m['haze'] >= HAZE_FLAG and m['haze'] >= HAZE_SHARE * max(1, m['solid'])


def describe(m: dict) -> str:
    return f"{m['specks']:4} specks ({m['speck_pixels']} px), {m.get('strokes', 0):2} strokes ({m.get('stroke_pixels', 0)} px), {m['haze']:5} haze px"


def measure(path: Path) -> dict:
    """The picture's specks (islands of at most SPECK_MOST solid pixels), its loose strokes (bigger islands, but thin and small beside
    the picture) and its haze (faint pixels far from any solid one)."""
    with Image.open(path) as im:
        if im.mode not in ('RGBA', 'LA', 'PA') and 'transparency' not in im.info:
            return {'specks': 0, 'speck_pixels': 0, 'strokes': 0, 'stroke_pixels': 0, 'haze': 0, 'solid': 0, 'opaque': True}
        alpha = np.asarray(im.convert('RGBA').getchannel('A'))
    solid = alpha >= SOLID
    labels, count = ndimage.label(solid, structure=EIGHT)
    sizes = np.bincount(labels.ravel(), minlength=count + 1)
    sizes[0] = 0
    total = int(sizes.sum())
    specks = (sizes > 0) & (sizes <= SPECK_MOST)
    # What of each island survives a 3 x 3 opening: almost nothing of a line two pixels thick, most of a part with any body.
    kept = np.bincount(labels[ndimage.binary_opening(solid, structure=EIGHT)].ravel(), minlength=count + 1)
    strokes = (sizes > SPECK_MOST) & (sizes <= STROKE_SHARE * total) & (kept <= THIN * sizes)
    # Haze: faint pixels beyond a soft edge's reach of every solid pixel (a fringe left beside a cut-out, a smear of background).
    reach = ndimage.binary_dilation(solid, structure=np.ones((2 * HAZE_REACH + 1,) * 2, bool))
    haze = int(((alpha >= FAINT) & (alpha < SOLID) & ~reach).sum())
    return {'specks': int(specks.sum()), 'speck_pixels': int(sizes[specks].sum()), 'strokes': int(strokes.sum()),
            'stroke_pixels': int(sizes[strokes].sum()), 'haze': haze, 'solid': total, 'opaque': False}


def measured(path: Path, store: dict) -> dict:
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    key = f'{VERSION}:{digest}'
    if key not in store:
        store[key] = measure(path)
    return store[key]


def shipped() -> list[Path]:
    return sorted(p for p in (ROOT / 'mods').rglob('*.png') if 'Textures' in p.parts)


def load_store() -> tuple[Path, dict]:
    folder = cache.folder('edge-check')
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / 'measures.json'
    try:
        return path, json.loads(path.read_text(encoding='utf-8'))
    except (OSError, ValueError):
        return path, {}


def lists() -> tuple[dict, dict]:
    """The owner's accepted pictures (specks drawn on purpose) and the known faults waiting to be fixed, each by path or wildcard."""
    try:
        data = json.loads(ACCEPTED.read_text(encoding='utf-8'))
    except (OSError, ValueError):
        return {}, {}
    return data.get('accepted', {}), data.get('known', {})


def listed(rel: str, entries: dict) -> str | None:
    for pattern, why in entries.items():
        if fnmatch.fnmatchcase(rel, pattern):
            return why
    return None


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest='command', required=True)
    check = sub.add_parser('check', help='every shipped texture; fails on a flagged one neither accepted nor a known fault')
    check.add_argument('--list', action='store_true', help='also list accepted pictures and pictures with haze worth a look')
    scan = sub.add_parser('scan', help='any pictures, with their figures')
    scan.add_argument('paths', nargs='+')
    args = parser.parse_args(argv)

    if args.command == 'scan':
        for name in args.paths:
            m = measure(Path(name))
            note = ' (haze worth a look)' if hazy(m) else ''
            print(f"{'FLAGGED' if flagged(m) else 'clean':8} {describe(m)}  {name}{note}")
        return 0

    store_path, store = load_store()
    ok, known = lists()
    failing = 0
    pictures = shipped()
    for path in pictures:
        m = measured(path, store)
        rel = path.relative_to(ROOT).as_posix()
        if flagged(m):
            if listed(rel, ok) is not None:
                if args.list:
                    print(f"accepted {describe(m)}  {rel}")
            elif (why := listed(rel, known)) is not None:
                print(f"KNOWN    {describe(m)}  {rel}  ({why})")
            else:
                failing += 1
                print(f"FLAGGED  {describe(m)}  {rel}")
        elif args.list and hazy(m):
            print(f"haze     {describe(m)}  {rel}")
    store_path.write_text(json.dumps(store), encoding='utf-8')
    if failing:
        print()
        print(f'{failing} shipped picture(s) with loose specks round their edges: crumbs left from a cut-out. Look at each at game size, '
              f'enlarged without smoothing; fix the export, or, if the specks are drawn on purpose, list it in '
              f'{ACCEPTED.relative_to(ROOT).as_posix()} with why.')
        return 1
    print(f'edge check: {len(pictures)} shipped pictures, no unexplained loose specks')
    return 0


if __name__ == '__main__':
    sys.exit(main())
