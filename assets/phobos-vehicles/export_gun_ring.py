"""Export the gun ring set's pictures (D'thorga Vehicles 0.121.0, Framework 0.296.0; vehicle-militarised-service-vehicles.md, MV8),
reproducibly. Free: it reads the owner's picks and writes new files.

    python assets/phobos-vehicles/export_gun_ring.py --out <artWorkPath>/gun-ring-stage --preview     stage and preview
    python assets/phobos-vehicles/export_gun_ring.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles/Turrets

Every part was drawn alone on white with its pivot (the ring's centre post, each gun's pintle, the shield's turning centre) at the
canvas centre (requests/gun-ring-set.json), so each is exported whole, centred where it was drawn, each on its own scale (each was
drawn filling its canvas): the ring and the guns then stand on the same point and turn about it, as Vehicle Framework turns a turret about its picture's centre. The cut-out is
the sprite exporter's own (the background AI's mask, white to alpha, premultiplied shrinking), with the ring's enclosed middle cleared.
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
REQUEST = HERE / 'requests' / 'gun-ring-set.json'
# The owner's picks of 25 Nov 2026 (D'thorga Companion's Picks page and question cards in chat).
PICKS = {
    'GunRing': ('gun-ring', 2),
    'RingGun762': ('ring-gun-762', 1),
    'RingGun50': ('ring-gun-50', 2),
    'RingGun40': ('ring-gun-40', 3),
    'GunShield': ('gun-shield', 2),
}
PIXELS_PER_CELL = 128
# Each picture's whole canvas in cells [A], since each was drawn filling its own: the guns two cells, so a .50's barrel reaches 0.84
# cells forward of its pintle; the ring and the shield one and a half, so the ring is about a cell across on a cab roof about 1.8 cells
# wide beside the remote kit's antenna (seen laid on the four trucks, 25 Nov 2026: at two cells the ring filled the roof and the shield
# stood wider than the cab), and the shield's arc is about a cell wide, 0.3 cells ahead of the pintle.
DRAW_CELLS = {'GunRing': 1.5, 'RingGun762': 2, 'RingGun50': 2, 'RingGun40': 2, 'GunShield': 1.5}
ICON_SIZE = 128


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def holes(name, seed):
    """The enclosed white a request names as background for this try (the ring's open quarters)."""
    for entry in json.loads(REQUEST.read_text(encoding='utf-8')):
        if entry['name'] == name:
            return [tuple(point) for point in entry.get('holes', {}).get(str(seed), [])]
    return []


def icon(picture):
    shape = picture.crop(picture.getchannel('A').getbbox())
    room = round(ICON_SIZE * 0.84)
    factor = room / max(shape.size)
    shape = shape.convert('RGBa').resize((max(1, round(shape.width * factor)), max(1, round(shape.height * factor))), Image.LANCZOS).convert('RGBA')
    square = Image.new('RGBA', (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    square.alpha_composite(shape, ((ICON_SIZE - shape.width) // 2, (ICON_SIZE - shape.height) // 2))
    return square


def record(path, pick, threshold, soft, made_holes, left, how, cells):
    data = {
        'target': es.repo_path(path), 'source': es.repo_path(pick), 'source_sha256': sha(pick), 'target_sha256': sha(path),
        'size': list(Image.open(path).size), 'white_threshold': threshold, 'soft_edge': soft, 'ai_mask': sha(pick),
        'holes': [list(point) for point in made_holes], 'white_left': left, 'draw_cells': cells,
        'date': _dt.datetime.now().isoformat(timespec='seconds'),
        'tool': f'assets/phobos-vehicles/export_gun_ring.py: export_sprite cut-out, {how}, premultiplied Lanczos',
    }
    art_paths.record_path(path.with_suffix('.export.json')).write_text(json.dumps(data, indent=2), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', required=True, help='the turrets folder (icons go in its Upgrades folder beside it)')
    args = parser.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    threshold, soft = 235, 30
    for name, (folder, seed) in PICKS.items():
        pick = GEN / folder / f'{folder}-{seed}.png'
        master = Image.open(pick)
        background = es.ai_background(pick)
        if background is None:
            raise SystemExit(f'{pick.name}: the background AI has not looked at it: run clean_backgrounds.py masks --cpu --also <pick>')
        made_holes = holes(folder, seed)
        cut = es.white_to_alpha(master, threshold, soft, False, made_holes, background=background)
        left = es.white_left(master, cut, threshold)
        # The whole square canvas, its centre the pivot, shrunk to its scale.
        side = round(DRAW_CELLS[name] * PIXELS_PER_CELL)
        picture = cut.convert('RGBa').resize((side, side), Image.LANCZOS).convert('RGBA')
        target = out / f'{name}.png'
        picture.save(target, optimize=True)
        record(target, pick, threshold, soft, made_holes, left, 'the whole canvas, its centre the pivot', DRAW_CELLS[name])
        icon_target = out.parent / 'Upgrades' / f'{name}.png'
        icon_target.parent.mkdir(parents=True, exist_ok=True)
        icon(picture).save(icon_target, optimize=True)
        record(icon_target, pick, threshold, soft, made_holes, left, 'upgrade icon fitted from the shipped picture', DRAW_CELLS[name])
        print(f'wrote {name}: {len(left)} white patches left')


if __name__ == '__main__':
    main()
