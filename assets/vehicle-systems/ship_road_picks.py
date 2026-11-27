"""Ship the owner's picks of 27 Nov 2026 for the road work and the sling: the cargo net (paid) and four night-16 icons (local).

    python assets/vehicle-systems/ship_road_picks.py

Each pick is exported by scripts/art/export_sprite.py (the background AI had looked at every try when its card was drawn), and each
texture gets its record under assets/records. The cargo net is drawn on 2 by 2 cells, so it ships at 256 px; the icons at 128 px.
"""
import hashlib
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

GENERATED = ROOT / 'assets' / 'vehicle-systems' / 'generated'
VS = 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
PAID = 'assets/vehicle-systems/requests/cargo-net-paid.json'
NIGHT = 'assets/vehicle-systems/requests/night-16-road-icons.json'

# request name, the owner's pick (Picks page, 27 Nov 2026), target without extension, size, paid, what it is
PICKS = [
    ('building-cargo-net', '1', f'{VS}/Things/CargoNet', 256, True, 'the cargo net lying on the ground, a stockpile the sling lifts'),
    ('n16-road-survey', '172', f'{VS}/RoadWork/RoadSurvey', 128, False, "the Datum's Road survey button"),
    ('n16-supply-plan', '190', f'{VS}/SupplyRun/Plan', 128, False, "the supply run's Plan a supply run button"),
    ('n16-supply-stops', '192', f'{VS}/SupplyRun/Stops', 128, False, "the supply run's Its stops button"),
    ('n16-supply-start', '199', f'{VS}/SupplyRun/Start', 128, False, "the supply run's Start the run button"),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    for name, pick, target, size, paid, what in PICKS:
        master = GENERATED / name / f'{name}-{pick}.png'
        base = ROOT / target
        single = base.with_suffix('.png')
        single.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(single),
                        '--size', str(size)], check=True, stdout=subprocess.DEVNULL)
        if paid:
            made = ('Made on 27 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, '
                    f'{what}. Picked by the owner from two attempts (attempt {pick}) on the Picks page. The prompt and its references '
                    f'are in `{PAID}`; the only references uploaded were our own approved art (the sling set and the fire bucket), '
                    'for style only. No game or other author\'s art was uploaded or used.')
        else:
            made = ('Made on 26 to 27 Nov 2026 on the owner\'s own graphics card with FLUX.2 Klein 4B base (klein-base, Apache 2.0) '
                    'through `scripts/art-run.py` (night 16), with our private style model (`phobos-private-vanilla`, trained on the '
                    f'game\'s sprites; never published): **AI-generated**, {what}. Picked by the owner on the Picks page (seed {pick}). '
                    f'The request is `{NIGHT}`; its references were our own approved icons (`Hitch.png`, `FlyTo.png`), for style only.')
        lines = [f'# {base.name}', '',
                 made + f' Exported by `scripts/art/export_sprite.py` at {size} px (1:1) after the background AI had looked at the '
                 'master, by `assets/vehicle-systems/ship_road_picks.py`. The master and its record go to the `art-masters` branch, '
                 'the other attempts to `art-references`.',
                 '',
                 f'- Master: `{name}-{pick}`, SHA-256 `{sha(master)}`',
                 f'- `{single.name}`: SHA-256 `{sha(single)}` (see its export.json)']
        record = art_paths.record_path(base.with_name(base.name + '.record.md'))
        record.write_text('\n'.join(lines) + '\n', encoding='utf-8')
        print('shipped', target)


if __name__ == '__main__':
    main()
