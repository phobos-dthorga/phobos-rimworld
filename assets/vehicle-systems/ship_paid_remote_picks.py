"""Ship the owner's picks of 27 Nov 2026 from the paid redraws of night 16's icons (requests/remote-icons-paid.json): round stop,
the optionally piloted kit, pop smoke and the ammo locker.

    python assets/vehicle-systems/ship_paid_remote_picks.py

Each pick ships where its request's art_plan.ships_as says, exported by scripts/art/export_sprite.py at 128 px (the background AI had
looked at every try when its card was drawn), with its record under assets/records. Mech link and unlink were turned down again and
went to fal's Nano Banana Pro (requests/mech-icons-paid-fal.json).
"""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

GENERATED = ROOT / 'assets' / 'vehicle-systems' / 'generated'
REQUEST = 'assets/vehicle-systems/requests/remote-icons-paid.json'

# request name, the owner's pick (Picks page, 27 Nov 2026), what it is
PICKS = [
    ('round-icon-stop', '1', "the round trip's Stop its round button"),
    ('fitting-icon-optionally-piloted-kit', '2', "the optionally piloted kit's fitting icon"),
    ('switch-icon-pop-smoke', '2', "the smoke dischargers' Pop smoke when hit switch"),
    ('upgrade-icon-ammo-locker', '2', "the ammunition locker's upgrade node icon"),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    entries = {e['name']: e for e in json.loads((ROOT / REQUEST).read_text(encoding='utf-8'))}
    for name, pick, what in PICKS:
        master = GENERATED / name / f'{name}-{pick}.png'
        target = ROOT / entries[name]['art_plan']['ships_as']
        target.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(target), '--size', '128'],
                       check=True, stdout=subprocess.DEVNULL)
        base = target.with_suffix('')
        lines = [f'# {base.name}', '',
                 'Made on 27 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, '
                 f'{what}. Picked by the owner from two attempts (attempt {pick}) on the Picks page, after the night-16 local tries were '
                 f'turned down. The prompt and its references are in `{REQUEST}`; the only references uploaded were our own approved '
                 'icons, for style only. No game or other author\'s art was uploaded or used. Exported by `scripts/art/export_sprite.py` at '
                 '128 px (1:1) after the background AI had looked at the master, by `assets/vehicle-systems/ship_paid_remote_picks.py`. '
                 'The master and its record go to the `art-masters` branch, the other attempt to `art-references`.',
                 '',
                 f'- Master: `{name}-{pick}`, SHA-256 `{sha(master)}`',
                 f'- `{target.name}`: SHA-256 `{sha(target)}` (see its export.json)']
        art_paths.record_path(base.with_name(base.name + '.record.md')).write_text('\n'.join(lines) + '\n', encoding='utf-8')
        print('shipped', target.relative_to(ROOT).as_posix())


if __name__ == '__main__':
    main()
