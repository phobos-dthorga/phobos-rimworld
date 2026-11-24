"""Export the remote weapon station's turret picture (D'thorga Vehicles 0.119.0; vehicle-militarised-service-vehicles.md, MV8), reproducibly.
Free: it reads the owner's pick and writes new files.

    python assets/phobos-vehicles/export_remote_weapon_station.py --out <artWorkPath>/rws-stage --preview      stage and preview
    python assets/phobos-vehicles/export_remote_weapon_station.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles/Turrets

The station was drawn alone on white, barrel up (requests/remote-weapon-station-fal.json; the owner picked try 1 on D'thorga Companion's
Picks page, 25 Nov 2026). Vehicle Framework turns a turret's picture about the picture's centre, so the square is framed on the turret
ring's centre, not on the drawing's bounds: the long barrel put the drawn ring below the middle. The cut-out is the sprite exporter's own
(the background AI's mask, white to alpha, premultiplied shrinking), so the edges match every other sprite of ours.
"""
import argparse
import datetime as _dt
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402
import export_sprite as es  # noqa: E402

PICK = HERE / 'generated' / 'remote-weapon-station-fal' / 'remote-weapon-station-fal-1.png'
NAME = 'RemoteWeaponStation'
PIXELS_PER_CELL = 128
# The turret's square in cells, the Def's drawSize. The ring is drawn about 1.15 cells across (a real station's ring is about a metre, on
# a cab roof about 2.6 cells wide), so the barrel reaches about 1.33 cells forward of the ring's centre and the square must hold it [A]:
# three cells, as the Osprey's ramp guns.
DRAW_CELLS = 3
RING_CELLS = 1.15
MARGIN = 0.04   # Clear room round the barrel's tip, as a share of the square's half-side
ICON_SIZE = 128


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def ring(cut):
    """The turret ring's centre and radius in the pick's pixels: the widest row of the drawing below the sight and ammunition boxes,
    which stand out past the ring higher up. Rows are measured on solid pixels only, so a soft edge does not widen them."""
    alpha = cut.getchannel('A').point(lambda a: 255 if a >= 128 else 0)
    box = alpha.getbbox()
    bottom = box[3]
    pixels = alpha.load()
    widest, at, span = 0, bottom, (0, 0)
    # Only the lower third of the drawing holds the ring alone; its widest row is its diameter.
    for y in range(bottom - (bottom - box[1]) // 3, bottom):
        xs = [x for x in range(box[0], box[2]) if pixels[x, y]]
        if xs and xs[-1] - xs[0] > widest:
            widest, at, span = xs[-1] - xs[0], y, (xs[0], xs[-1])
    radius = widest / 2
    centre_x = (span[0] + span[1]) / 2
    # The widest row is the ring's middle only if the drawing is a circle there; take the centre a radius above the ring's bottom edge,
    # which is the same for a circle and safer where the base flares.
    centre_y = bottom - radius
    return (centre_x, centre_y), radius, at


def frame(cut, centre, radius):
    """A square about the ring's centre holding the whole drawing, shrunk so the ring is RING_CELLS across in game pixels."""
    box = cut.getchannel('A').getbbox()
    reach = max(centre[0] - box[0], box[2] - centre[0], centre[1] - box[1], box[3] - centre[1])
    side_px = round(DRAW_CELLS * PIXELS_PER_CELL)
    scale = RING_CELLS * PIXELS_PER_CELL / (2 * radius)
    half = side_px / 2 / scale
    if reach > half * (1 - MARGIN):
        raise SystemExit(f'the drawing reaches {reach * scale / PIXELS_PER_CELL:.2f} cells from the ring centre: raise DRAW_CELLS')
    square = Image.new('RGBA', (round(2 * half), round(2 * half)), (0, 0, 0, 0))
    square.alpha_composite(cut, (round(half - centre[0]), round(half - centre[1])))
    picture = square.convert('RGBa').resize((side_px, side_px), Image.LANCZOS).convert('RGBA')
    return picture, scale, reach * scale / PIXELS_PER_CELL


def icon(picture):
    """The upgrade node's icon: the station itself, fitted into the icon's square, until a drawn icon is picked."""
    shape = picture.crop(picture.getchannel('A').getbbox())
    room = round(ICON_SIZE * 0.84)
    factor = room / max(shape.size)
    shape = shape.convert('RGBa').resize((max(1, round(shape.width * factor)), max(1, round(shape.height * factor))), Image.LANCZOS).convert('RGBA')
    square = Image.new('RGBA', (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    square.alpha_composite(shape, ((ICON_SIZE - shape.width) // 2, (ICON_SIZE - shape.height) // 2))
    return square


def preview(picture, work):
    """The turret on dark ground at three times, unsmoothed, with the turning centre marked, and turned a quarter."""
    big = picture.resize((picture.width * 3, picture.height * 3), Image.NEAREST)
    cells = []
    for angle in (0, -90):
        ground = Image.new('RGBA', big.size, (40, 40, 44, 255))
        ground.alpha_composite(big.rotate(angle, resample=Image.NEAREST))
        draw = ImageDraw.Draw(ground)
        c = big.width / 2
        draw.line([(c - 12, c), (c + 12, c)], fill=(255, 220, 0, 255))
        draw.line([(c, c - 12), (c, c + 12)], fill=(255, 220, 0, 255))
        cells.append(ground)
    sheet = Image.new('RGB', (sum(cell.width + 12 for cell in cells), big.height), (20, 20, 20))
    x = 0
    for cell in cells:
        sheet.paste(cell.convert('RGB'), (x, 0))
        x += cell.width + 12
    target = work / 'rws-preview.png'
    sheet.save(target)
    print('preview', target)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', required=True, help='the turrets folder (the icon goes in its Upgrades folder beside it)')
    parser.add_argument('--preview', action='store_true', help='also draw the turret on dark ground, its turning centre marked')
    args = parser.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    work = art_paths.work('rws-export')
    work.mkdir(parents=True, exist_ok=True)

    master = Image.open(PICK)
    background = es.ai_background(PICK)
    if background is None:
        raise SystemExit('the background AI has not looked at the pick: run clean_backgrounds.py masks --cpu --also <pick> first')
    threshold, soft = 235, 30
    cut = es.white_to_alpha(master, threshold, soft, False, [], background=background)
    left = es.white_left(master, cut, threshold)
    centre, radius, row = ring(cut)
    picture, scale, reach = frame(cut, centre, radius)
    target = out / f'{NAME}.png'
    picture.save(target, optimize=True)
    icon_target = out.parent / 'Upgrades' / f'{NAME}.png'
    icon_target.parent.mkdir(parents=True, exist_ok=True)
    icon(picture).save(icon_target, optimize=True)

    for path, how in ((target, 'turret picture framed on the ring centre'), (icon_target, 'upgrade icon fitted from the turret picture')):
        record = {
            'target': es.repo_path(path),
            'source': es.repo_path(PICK),
            'source_sha256': sha(PICK),
            'target_sha256': sha(path),
            'size': list(Image.open(path).size),
            'white_threshold': threshold,
            'soft_edge': soft,
            'ai_mask': sha(PICK),
            'white_left': left,
            'ring_centre_px': [round(centre[0], 1), round(centre[1], 1)],
            'ring_radius_px': round(radius, 1),
            'draw_cells': DRAW_CELLS,
            'ring_cells': RING_CELLS,
            'reach_cells': round(reach, 3),
            'date': _dt.datetime.now().isoformat(timespec='seconds'),
            'tool': f'assets/phobos-vehicles/export_remote_weapon_station.py: export_sprite cut-out, {how}, premultiplied Lanczos',
        }
        art_paths.record_path(path.with_suffix('.export.json')).write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'ring centre {centre[0]:.0f},{centre[1]:.0f} radius {radius:.0f} px (widest row {row}); reach {reach:.2f} cells; '
          f'{len(left)} white patches left')
    print('wrote', target, icon_target)
    if args.preview:
        preview(picture, work)


if __name__ == '__main__':
    main()
