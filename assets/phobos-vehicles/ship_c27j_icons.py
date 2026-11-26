"""Ship the owner's picks of the C-27J's paid icons of 26 Nov 2026 (requests/c27j-icons-paid.json and c27j-icons-paid-fal.json;
the record is requests/c27j-overlays-record.md).

    python assets/phobos-vehicles/ship_c27j_icons.py

The owner's card of 26 Nov 2026 chose the paid route for these four ("Paid, US$1.19 cap"), the work kept off the graphics card. Each pick
is exported by scripts/art/export_sprite.py at 128 px (the background AI had looked at every try when its card was drawn), and each
texture gets its record under assets/records. The gun kit and the fire tank ship ahead of their fit nodes, which come next.
"""
import hashlib
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

GENERATED = ROOT / 'assets' / 'phobos-vehicles' / 'generated'
VS = 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
C27J = 'mods/PhobosVehicles/Textures/Phobos/Vehicles/C27J/Upgrades'
SIZE = 128
OPENAI = ('OpenAI gpt-image-2 through the image API, at medium quality', 'assets/phobos-vehicles/requests/c27j-icons-paid.json')
FAL = ('Google Nano Banana Pro through fal.ai, after OpenAI\'s filter refused both tries as a weapon',
       'assets/phobos-vehicles/requests/c27j-icons-paid-fal.json')

# request name, the owner's pick (Picks page, 26 Nov 2026), target without extension, the engine, what it is
PICKS = [
    ('lapes-button', 1, f'{VS}/Airdrop/Lapes', OPENAI, "the aircraft's Low extraction (LAPES) button, which had shown the drop pass's icon"),
    ('c27j-icon-defensiveaids', 2, f'{C27J}/DefensiveAids', OPENAI,
     "the C-27J's defensive aids upgrade, which had borrowed the Osprey's infrared jammer icon"),
    ('c27j-icon-gunkit-fal', 1, f'{C27J}/GunKit', FAL, "the C-27J's MC-27J gun kit upgrade"),
    ('c27j-icon-firetank', 1, f'{C27J}/FireTank', OPENAI, "the C-27J's fire-fighting tank upgrade"),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    for name, pick, target, (engine, request), what in PICKS:
        master = GENERATED / name / f'{name}-{pick}.png'
        base = ROOT / target
        single = base.with_suffix('.png')
        single.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(single),
                        '--size', str(SIZE)], check=True, stdout=subprocess.DEVNULL)
        lines = [f'# {base.name}', '',
                 f'Made on 26 Nov 2026 with {engine}: **AI-generated**, {what}. Picked by the owner from two attempts (attempt {pick}) '
                 f'on the Picks page. Exported by `scripts/art/export_sprite.py` at {SIZE} px (1:1) after the background AI had looked '
                 'at the master, by `assets/phobos-vehicles/ship_c27j_icons.py`. The prompt and its references are in '
                 f'`{request}`; the master and its record go to the `art-masters` branch, the other attempt to `art-references`. '
                 'The only references uploaded were our own approved art, for style only. No game or other author\'s art was '
                 'uploaded or used.',
                 '',
                 f'- Master: `{name}-{pick}`, SHA-256 `{sha(master)}`',
                 f'- `{single.name}`: SHA-256 `{sha(single)}` (see its export.json)']
        record = art_paths.record_path(base.with_name(base.name + '.record.md'))
        record.parent.mkdir(parents=True, exist_ok=True)
        record.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        print('shipped', target)


if __name__ == '__main__':
    main()
