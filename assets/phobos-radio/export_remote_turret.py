"""Export the base turret's pictures (Radio Framework 0.18.0; docs/development/autonomous-vehicles.md, the base turret), reproducibly.
Free: it reads the owner's picks and writes new files.

    python assets/phobos-radio/export_remote_turret.py --out <artWorkPath>/remote-turret-stage
    python assets/phobos-radio/export_remote_turret.py --out mods/PhobosRadioFramework/Common/Textures/Phobos/Radio/Things

Two pictures (the owner's word of 25 Nov 2026 on the remote weapon station: "use both", the second try for ground stations):
- the pedestal, the owner's pick of 26 Nov 2026 (requests/remote-turret-pedestal.json, try 1), exported whole about the canvas centre;
- the turning top, the remote weapon station's try 2 (assets/phobos-vehicles/requests/remote-weapon-station-fal.json), framed on its
  turret ring's centre as the trucks' station is (export_remote_weapon_station.ring), so it turns on its ring in the pedestal's socket.
The cut-out is the sprite exporter's own (the background AI's mask, white to alpha, premultiplied shrinking).
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
sys.path.insert(0, str(ROOT / 'assets' / 'phobos-vehicles'))
import art_paths  # noqa: E402
import export_sprite as es  # noqa: E402
import export_remote_weapon_station as rws  # noqa: E402

PEDESTAL = HERE / 'generated' / 'remote-turret-pedestal' / 'remote-turret-pedestal-1.png'
TOP = ROOT / 'assets' / 'phobos-vehicles' / 'generated' / 'remote-weapon-station-fal' / 'remote-weapon-station-fal-2.png'
PIXELS_PER_CELL = 128
# The pedestal's whole canvas on a one-cell building [A]: its plinth, drawn at 80 percent of the canvas, is about 1.1 cells, a little
# over its cell as the game's turrets' bases are; its ring socket, at 55 percent, about 0.77 cells.
PEDESTAL_CELLS = 1.4
# The top's ring sized to sit in that socket [A], and its square large enough for the barrel, as the trucks' station's.
TOP_RING_CELLS = 0.85
TOP_CELLS = 2.25


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def cut(pick):
    background = es.ai_background(pick)
    if background is None:
        raise SystemExit(f'{pick.name}: the background AI has not looked at it: run clean_backgrounds.py masks --cpu --also <pick>')
    master = Image.open(pick)
    picture = es.white_to_alpha(master, 235, 30, False, [], background=background)
    return picture, es.white_left(master, picture, 235)


def record(path, pick, left, how, extra):
    data = {'target': es.repo_path(path), 'source': es.repo_path(pick), 'source_sha256': sha(pick), 'target_sha256': sha(path),
            'size': list(Image.open(path).size), 'white_threshold': 235, 'soft_edge': 30, 'ai_mask': sha(pick), 'white_left': left,
            'date': _dt.datetime.now().isoformat(timespec='seconds'),
            'tool': f'assets/phobos-radio/export_remote_turret.py: export_sprite cut-out, {how}, premultiplied Lanczos', **extra}
    art_paths.record_path(path.with_suffix('.export.json')).write_text(json.dumps(data, indent=2), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', required=True, help='the folder for both pictures')
    args = parser.parse_args()
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    pedestal, left = cut(PEDESTAL)
    side = round(PEDESTAL_CELLS * PIXELS_PER_CELL)
    target = out / 'RemoteTurretBase.png'
    pedestal.convert('RGBa').resize((side, side), Image.LANCZOS).convert('RGBA').save(target, optimize=True)
    record(target, PEDESTAL, left, 'the whole canvas about its centre', {'draw_cells': PEDESTAL_CELLS})
    print('wrote', target)

    top, left = cut(TOP)
    centre, radius, _ = rws.ring(top)
    rws.RING_CELLS, rws.DRAW_CELLS = TOP_RING_CELLS, TOP_CELLS
    picture, scale, reach = rws.frame(top, centre, radius)
    target = out / 'RemoteTurretTop.png'
    picture.save(target, optimize=True)
    record(target, TOP, left, 'framed on the turret ring centre', {'ring_centre_px': [round(centre[0], 1), round(centre[1], 1)],
                                                                    'ring_cells': TOP_RING_CELLS, 'draw_cells': TOP_CELLS, 'reach_cells': round(reach, 3)})
    print(f'wrote {target}: ring centre {centre[0]:.0f},{centre[1]:.0f}, reach {reach:.2f} cells')


if __name__ == '__main__':
    main()
