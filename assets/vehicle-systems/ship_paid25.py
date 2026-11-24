"""Ship the owner's picks of the paid pictures of 25 Nov 2026 (assets/vehicle-systems/requests/paid-2026-11-25.json), reproducibly.

    python assets/vehicle-systems/ship_paid25.py

Each pick is exported by scripts/art/export_sprite.py (the background AI had looked at every try when its card was drawn), a
piece with facings is drawn north and turned by script for east and south (the game mirrors east for west), as the camp pieces
of 22 Nov 2026 were, and each texture gets its record under assets/records. The rack and mast are laid on their vehicles by
assets/phobos-vehicles/export_paid25_parts.py.
"""
import hashlib
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

REQUEST = 'assets/vehicle-systems/requests/paid-2026-11-25.json'
GENERATED = ROOT / 'assets' / 'vehicle-systems' / 'generated'
VS = 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'

# request name, the owner's pick (Picks page, 25 Nov 2026), target without extension, size, turned facings, what it is
PICKS = [
    ('paid25-loading-chute', 1, 'mods/PhobosVehicles/Textures/Phobos/Vehicles/LoadingChute/LoadingChute', 256, False,
     "the bulk bunker's loading chute, 1 x 1, replacing the Pillow stand-in of 17 Nov 2026"),
    ('paid25-canvas-flap', 1, f'{VS}/Camp/CanvasFlap', 128, False,
     "the canvas flap of Billy's field tent (a door leaf), replacing the base game's simple door tinted; also its menu icon"),
    ('paid25-cabin-seat-north', 2, f'{VS}/Cabins/CabinSeat', 128, 'chair',
     "a crew seat in a vehicle's walk-in cabin, 1 x 1, replacing the base game's dining chair"),
    ('paid25-cabin-litter-north', 1, f'{VS}/Cabins/CabinLitter', 256, 'bed',
     "a casualty litter in a vehicle's walk-in cabin, 1 x 2 drawn at 2 x 2, replacing the base game's hospital bed"),
    ('paid25-cabin-way-out', 1, f'{VS}/Cabins/CabinWayOut', 128, False,
     "the walk-in cabin's way out (a floor hatch), replacing the base game's simple door"),
    ('paid25-ecm-pod', 2, f'{VS}/Fittings/EcmPod', 128, False,
     "the ECM pod fitting's icon, which had no picture (four local tries of nights 12 and 13 came out as torpedoes)"),
    ('paid25-coax-icon', 1, 'mods/PhobosRadioFramework/Common/Textures/Phobos/Radio/UI/CoaxIcon', 128, False,
     'the coax feedline icon, replacing the local picture of 12 Nov 2026 that read as a black tyre'),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def export(master, target, size):
    target.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(target), '--size', str(size)],
                   check=True, stdout=subprocess.DEVNULL)


def main():
    for name, pick, target, size, turned, what in PICKS:
        master = GENERATED / name / f'{name}-{pick}.png'
        base = ROOT / target
        files = []
        if turned:
            # A chair faces the way its sitter looks: the master (backrest at the bottom) is north, east turned clockwise.
            # A bed faces from its head to its foot, as the game's own do (HospitalBed_south has its pillow at the top): the
            # master (pillow at the top) is south, north turned 180 degrees, east turned anticlockwise (pillow on the left).
            drawn, turns = (('north', (('east', Image.Transpose.ROTATE_270), ('south', Image.Transpose.ROTATE_180))) if turned == 'chair'
                            else ('south', (('east', Image.Transpose.ROTATE_90), ('north', Image.Transpose.ROTATE_180))))
            first = base.with_name(f'{base.name}_{drawn}.png')
            export(master, first, size)
            picture = Image.open(first)
            for facing, turn in turns:
                picture.transpose(turn).save(base.with_name(f'{base.name}_{facing}.png'))
            files = [first] + [base.with_name(f'{base.name}_{facing}.png') for facing, _ in turns]
        else:
            single = base.with_suffix('.png')
            export(master, single, size)
            files = [single]
        lines = [f'# {base.name}', '',
                 f'Made on 25 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, {what}. '
                 f'Picked by the owner from two attempts (attempt {pick}) on the Picks page. Exported by `scripts/art/export_sprite.py` '
                 f'at {size} px on its shorter side (1:1) after the background AI had looked at the master, by '
                 f'`assets/vehicle-systems/ship_paid25.py`. The prompt and its references are in `{REQUEST}`; the master and its record '
                 'go to the `art-masters` branch, the other attempt to `art-references`. The only references uploaded were our own approved '
                 'art, for style only. No game or other author\'s art was uploaded or used.']
        if turned == 'chair':
            lines.append('The east and south facings are the north one turned by script (90 degrees clockwise and 180 degrees); the '
                         'game mirrors east for west.')
        elif turned == 'bed':
            lines.append('The master is the south facing, the pillow at the top as on the game\'s own beds (a bed faces from its head '
                         'to its foot); north and east are it turned by script (180 degrees, and 90 degrees anticlockwise so the pillow '
                         'is on the left); the game mirrors east for west.')
        lines += ['', f'- Master: `{name}-{pick}`, SHA-256 `{sha(master)}`']
        lines += [f'- `{f.name}`: SHA-256 `{sha(f)}`' + (' (see its export.json)' if i == 0 else '') for i, f in enumerate(files)]
        record = art_paths.record_path(base.with_name(base.name + '.record.md'))
        record.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        print('shipped', target, len(files), 'file(s)')


if __name__ == '__main__':
    main()
