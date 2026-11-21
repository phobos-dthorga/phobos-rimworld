"""Find ragged edges in shipped pictures: loose specks and dashed fringes left round a cut-out.

    python scripts/art/edge_check.py check            every shipped texture under mods/; fails on one flagged and not accepted (the gate)
    python scripts/art/edge_check.py scan PATH ...    any pictures, each with its figures, flagged or not
    python scripts/art/edge_check.py check --list     every flagged texture, accepted ones too

The owner's review of 22 Nov 2026 found roof tents with parts "scrubbed out" and dithered edges, and rolled bags with a dashed fringe:
pieces of whatever they were cut from, a few pixels each, lying loose beside the picture. The game draws them at full size, so the owner
sees them. This check counts such pieces: little islands of solid pixels, separate from everything else (8-connected), no bigger than
SPECK_MOST pixels. A clean cut-out has none or one or two (a rivet, a lamp's glint); a fringe has dozens. A picture with at least
SPECKS_FLAG of them is flagged. Pillow only: islands are searched from the pixels in sparse neighbourhoods, so a big picture costs little,
and each file's answer is kept by its SHA-256 in the shared tools' cache, so the gate reads only new or changed pictures.

Pictures the owner has seen and kept (sparks, rain, a scatter of rubble drawn on purpose) are listed in config/edge-check.json with why.
Game-free apart from where it looks (mods/**/Textures): a next game names its own texture folders.
"""
from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import sys
from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import _modkit  # noqa: E402  (the shared tools' import path)
_modkit.ensure()
from phobos_modkit import cache  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
ACCEPTED = ROOT / 'config' / 'edge-check.json'
SOLID = 96            # alpha at or above this counts as part of the picture
SPARSE = 12           # a pixel with this many or fewer solid pixels in its 5 x 5 neighbourhood may belong to a speck
SPECK_MOST = 24       # an island of at most this many pixels is a speck
SPECKS_FLAG = 6       # this many specks or more flags the picture
FAINT = 8             # alpha from this up to SOLID is faint: a soft edge's own pixels, or haze
HAZE_REACH = 2        # a faint pixel more than this many pixels from any solid one is haze, not a soft edge
HAZE_FLAG = 60        # this many haze pixels or more is worth a look (shown by --list, never a failure)...
HAZE_SHARE = 0.004    # ...when they are also at least this share of the solid pixels
VERSION = 2           # raise when the figures above change, so cached answers are worked out again


def flagged(m: dict) -> bool:
    """Loose specks flag a picture. Haze is shown, never flagged: glass, a lamp's glow and a landing pad are faint on purpose."""
    return m['specks'] >= SPECKS_FLAG


def hazy(m: dict) -> bool:
    return m['haze'] >= HAZE_FLAG and m['haze'] >= HAZE_SHARE * max(1, m['solid'])


def describe(m: dict) -> str:
    return f"{m['specks']:4} specks ({m['speck_pixels']} px), {m['haze']:5} haze px"


def measure(path: Path) -> dict:
    """The picture's specks (islands of at most SPECK_MOST solid pixels) and its haze (faint pixels far from any solid one)."""
    with Image.open(path) as im:
        if im.mode not in ('RGBA', 'LA', 'PA') and 'transparency' not in im.info:
            return {'specks': 0, 'speck_pixels': 0, 'haze': 0, 'solid': 0, 'opaque': True}
        alpha = im.convert('RGBA').getchannel('A')
    width, height = alpha.size
    solid = alpha.point(lambda a: 255 if a >= SOLID else 0)
    # Haze: faint pixels beyond a soft edge's reach of every solid pixel (a fringe left beside a cut-out, a smear of background).
    reach = solid.filter(ImageFilter.MaxFilter(2 * HAZE_REACH + 1)).tobytes()
    haze = sum(1 for a, r in zip(alpha.tobytes(), reach) if FAINT <= a < SOLID and not r)
    density = solid.filter(ImageFilter.BoxBlur(2))
    mask = solid.tobytes()
    near = density.tobytes()
    limit = 255 * SPARSE // 25
    seen = bytearray(width * height)
    specks = 0
    speck_pixels = 0
    for start, (m, d) in enumerate(zip(mask, near)):
        if not m or d > limit or seen[start]:
            continue
        # Walk this island, but no further than a speck can be: a bigger island is part of the picture.
        queue = deque([start])
        seen[start] = 1
        size = 0
        while queue and size <= SPECK_MOST:
            here = queue.popleft()
            size += 1
            y, x = divmod(here, width)
            for dy in (-1, 0, 1):
                ny = y + dy
                if ny < 0 or ny >= height:
                    continue
                row = ny * width
                for dx in (-1, 0, 1):
                    nx = x + dx
                    if (dy or dx) and 0 <= nx < width:
                        other = row + nx
                        if mask[other] and not seen[other]:
                            seen[other] = 1
                            queue.append(other)
        if size <= SPECK_MOST and not queue:
            specks += 1
            speck_pixels += size
    return {'specks': specks, 'speck_pixels': speck_pixels, 'haze': haze, 'solid': sum(1 for m in mask if m), 'opaque': False}


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
