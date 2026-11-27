"""Ship the owner's picks of the paid road work icons of 27 Nov 2026 (assets/vehicle-systems/requests/road-icons-paid.json).

    python assets/vehicle-systems/ship_road_icons_paid.py

The owner answered every night-16 local try of these eight "Paid solution" (27 Nov 2026), so they were drawn by the paid route. Each
pick is exported by scripts/art/export_sprite.py at 128 px (the background AI had looked at every try when its card was drawn), and each
texture gets its record under assets/records.
"""
import hashlib
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

REQUEST = 'assets/vehicle-systems/requests/road-icons-paid.json'
GENERATED = ROOT / 'assets' / 'vehicle-systems' / 'generated'
VS = 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
SIZE = 128

# request name, the owner's pick (Picks page, 27 Nov 2026), target without extension, what it is
PICKS = [
    ('road-icon-road-bore', 1, f'{VS}/RoadWork/Bore', "the Kelly's Drill a water bore button, which had borrowed the water drop's picture"),
    ('road-icon-survey-halt', 1, f'{VS}/RoadWork/SurveyHalt', "the Datum's Survey halt button, which had borrowed the survey array's"),
    ('road-icon-tip-load', 2, f'{VS}/RoadWork/TipLoad', "the Talus's Tip the load here button, which had borrowed the tip station's"),
    ('road-icon-test-hole', 2, f'{VS}/RoadWork/TestHole', "the Kelly's Drill a test hole button, which had borrowed the deep drill's"),
    ('road-icon-quarry', 1, f'{VS}/RoadWork/Quarry', "the Kelly's Quarry stone button, which had borrowed the stone chunk's"),
    ('road-icon-grit', 1, f'{VS}/RoadWork/Grit', "the Talus's Spread grit button, which had borrowed the tip station's"),
    ('road-icon-stone-under-wheels', 2, f'{VS}/RoadWork/StoneUnderWheels', "the Lay stone under the wheels button, which had borrowed the tip station's"),
    ('road-icon-hook-from-depot', 1, f'{VS}/RoadWork/HookFromDepot', "the Hook up from the depot button, which had borrowed the hitch's"),
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
                 f'Made on 27 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, {what}. '
                 f'Picked by the owner from two attempts (attempt {pick}) on the Picks page, after the owner answered both local tries '
                 f'of night 16 "Paid solution". Exported by `scripts/art/export_sprite.py` at {SIZE} px (1:1) after the background AI had looked at '
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
