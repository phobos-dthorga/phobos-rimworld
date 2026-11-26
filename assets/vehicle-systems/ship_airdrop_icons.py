"""Ship the owner's picks of the paid airdrop icons of 26 Nov 2026 (assets/vehicle-systems/requests/airdrop-icons-paid.json).

    python assets/vehicle-systems/ship_airdrop_icons.py

The owner rejected every local try of night 14 (26 Nov 2026: "they'll have to be paid"), so these six were drawn by the paid
route. Each pick is exported by scripts/art/export_sprite.py at 128 px (the background AI had looked at every try when its card
was drawn), and each texture gets its record under assets/records.
"""
import hashlib
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

REQUEST = 'assets/vehicle-systems/requests/airdrop-icons-paid.json'
GENERATED = ROOT / 'assets' / 'vehicle-systems' / 'generated'
VS = 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
SIZE = 128

# request name, the owner's pick (Picks page, 26 Nov 2026), target without extension, what it is
PICKS = [
    ('airdrop-icon-drop-here', 1, f'{VS}/Airdrop/DropHere', "the aircraft's Drop here button (a hover drop from its drop height)"),
    ('airdrop-icon-drop-low', 1, f'{VS}/Airdrop/DropLow', "the aircraft's Drop low button (a drop from below the minimum height)"),
    ('airdrop-icon-load-hold', 1, f'{VS}/Airdrop/LoadHold', "the hold's Load into hold button"),
    ('airdrop-icon-unload-hold', 1, f'{VS}/Airdrop/UnloadHold', "the hold's Unload hold button"),
    ('airdrop-icon-airdrop-on', 2, f'{VS}/Airdrop/AirdropOn', "the world map's Airdrop on button and its targeter's mouse picture"),
    ('airdrop-icon-category', 2, f'{VS}/Categories/Parachutes',
     'the Parachutes storage category, which had borrowed the packed personnel parachute picture'),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    for name, pick, target, what in PICKS:
        master = GENERATED / name / f'{name}-{pick}.png'
        base = ROOT / target
        single = base.with_suffix('.png')
        single.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(single),
                        '--size', str(SIZE)], check=True, stdout=subprocess.DEVNULL)
        lines = [f'# {base.name}', '',
                 f'Made on 26 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, {what}. '
                 f'Picked by the owner from two attempts (attempt {pick}) on the Picks page, after the owner rejected both local tries '
                 f'of night 14. Exported by `scripts/art/export_sprite.py` at {SIZE} px (1:1) after the background AI had looked at '
                 'the master, by `assets/vehicle-systems/ship_airdrop_icons.py`. The prompt and its references are in '
                 f'`{REQUEST}`; the master and its record go to the `art-masters` branch, the other attempt to `art-references`. '
                 'The only references uploaded were our own approved art, for style only. No game or other author\'s art was '
                 'uploaded or used.',
                 '',
                 f'- Master: `{name}-{pick}`, SHA-256 `{sha(master)}`',
                 f'- `{single.name}`: SHA-256 `{sha(single)}` (see its export.json)']
        record = art_paths.record_path(base.with_name(base.name + '.record.md'))
        record.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        print('shipped', target)


if __name__ == '__main__':
    main()
