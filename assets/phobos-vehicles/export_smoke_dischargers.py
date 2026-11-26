"""Export the smoke discharger bank's picture (D'thorga Vehicles 0.125.0, Framework 0.319.0; vehicle-militarised-service-vehicles.md,
"Smoke dischargers", MV8), reproducibly. Free: it reads the owner's pick and writes new files.

    python assets/phobos-vehicles/export_smoke_dischargers.py --out <artWorkPath>/smoke-stage
    python assets/phobos-vehicles/export_smoke_dischargers.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles/Turrets

One bank of four tubes was drawn alone on white, pointing to the top (requests/smoke-dischargers.json). Each truck's node fits it twice, as
two fixed turrets at the cab's front corners turned 30 degrees out, and Vehicle Framework turns a turret's picture about its centre, so the
bank is cut out with the sprite exporter's own cut-out (the background AI's mask, white to alpha, premultiplied shrinking), cropped to
itself, scaled to its width on the cab and centred on a small square canvas. The upgrade icon is fitted from the shipped picture.
"""
import argparse
import datetime as _dt
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402
import export_sprite as es  # noqa: E402

GEN = HERE / 'generated'
# The tries the owner chose between (26 Nov 2026): try 2 (truck and station as style references) and try 3 (the truck alone). Try 1 redrew
# the remote weapon station and was never offered.
TRIES = {2: ('smoke-discharger-bank', 2), 3: ('smoke-discharger-bank-truck-only', 3)}
PICK = 3  # the owner's pick of 26 Nov 2026 ("Try #3")
PIXELS_PER_CELL = 128
# The bank's width on the cab, cells [A]: four tubes of about 80 mm side by side on a plate, about 0.55 of a cell on a cab three cells
# wide; its canvas 0.75 of a cell (96 pixels), so the turret Def's drawSize is (0.75, 0.75).
BANK_CELLS = 0.55
CANVAS_CELLS = 0.75
ICON_SIZE = 128


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def icon(picture):
    shape = picture.crop(picture.getchannel('A').getbbox())
    room = round(ICON_SIZE * 0.84)
    factor = room / max(shape.size)
    shape = shape.convert('RGBa').resize((max(1, round(shape.width * factor)), max(1, round(shape.height * factor))), Image.LANCZOS).convert('RGBA')
    square = Image.new('RGBA', (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    square.alpha_composite(shape, ((ICON_SIZE - shape.width) // 2, (ICON_SIZE - shape.height) // 2))
    return square


def record(path, pick, threshold, soft, left, how):
    data = {
        'target': es.repo_path(path), 'source': es.repo_path(pick), 'source_sha256': sha(pick), 'target_sha256': sha(path),
        'size': list(Image.open(path).size), 'white_threshold': threshold, 'soft_edge': soft, 'ai_mask': sha(pick),
        'white_left': left, 'bank_cells': BANK_CELLS, 'canvas_cells': CANVAS_CELLS,
        'date': _dt.datetime.now().isoformat(timespec='seconds'),
        'tool': f'assets/phobos-vehicles/export_smoke_dischargers.py: export_sprite cut-out, {how}, premultiplied Lanczos',
    }
    art_paths.record_path(path.with_suffix('.export.json')).write_text(json.dumps(data, indent=2), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', required=True, help='the turrets folder (the icon goes in its Upgrades folder beside it)')
    parser.add_argument('--try', dest='attempt', type=int, choices=sorted(TRIES), default=PICK,
                        help='which try to export (default: the owner\'s pick, once set)')
    args = parser.parse_args()
    if args.attempt is None:
        raise SystemExit('no pick yet: pass --try 2 or --try 3 to stage one, or set PICK to the owner\'s pick')
    folder, seed = TRIES[args.attempt]
    pick = GEN / folder / f'{folder}-{seed}.png'
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    threshold, soft = 235, 30
    master = Image.open(pick)
    background = es.ai_background(pick)
    if background is None:
        raise SystemExit(f'{pick.name}: the background AI has not looked at it: run clean_backgrounds.py masks --cpu --also <pick>')
    cut = es.white_to_alpha(master, threshold, soft, False, [], background=background)
    left = es.white_left(master, cut, threshold)
    bank = cut.crop(cut.getchannel('A').getbbox())
    width = round(BANK_CELLS * PIXELS_PER_CELL)
    height = max(1, round(bank.height * width / bank.width))
    bank = bank.convert('RGBa').resize((width, height), Image.LANCZOS).convert('RGBA')
    side = round(CANVAS_CELLS * PIXELS_PER_CELL)
    picture = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    picture.alpha_composite(bank, ((side - bank.width) // 2, (side - bank.height) // 2))
    target = out / 'SmokeDischarger.png'
    picture.save(target, optimize=True)
    record(target, pick, threshold, soft, left, 'the bank cropped to itself, centred, its centre the pivot')
    icon_target = out.parent / 'Upgrades' / 'SmokeDischargers.png'
    icon_target.parent.mkdir(parents=True, exist_ok=True)
    icon(picture).save(icon_target, optimize=True)
    record(icon_target, pick, threshold, soft, left, 'upgrade icon fitted from the shipped picture')
    print(f'wrote the bank from try {args.attempt}: {len(left)} white patches left')


if __name__ == '__main__':
    main()
