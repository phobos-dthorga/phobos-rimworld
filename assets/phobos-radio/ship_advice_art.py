"""Ship the owner's picks of the paid advice and relay art of 26 Nov 2026 (assets/phobos-radio/requests/advice-and-relays-paid.json).

    python assets/phobos-radio/ship_advice_art.py

Three pictures for radio advice and relays, drawn by the paid route because the numbered medical chest lies in the game world and the
owner asked for the kits' pictures to be paid too (26 Nov 2026: the local model was not up to the stereo kit). The owner picked one try
of two for each (27 Nov 2026). Each pick is exported by scripts/art/export_sprite.py at 128 px, square (the background AI had looked at
every try when its card was drawn), and each texture gets its record under assets/records.
"""
import hashlib
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

REQUEST = 'assets/phobos-radio/requests/advice-and-relays-paid.json'
GENERATED = ROOT / 'assets' / 'phobos-radio' / 'generated'
RADIO = 'mods/PhobosRadioFramework/Common/Textures/Phobos/Radio'
RADIO_VF = 'mods/PhobosRadioFramework/Mods/VehicleFramework/Common/Textures/Phobos/Radio'
SIZE = 128

# request name, the owner's pick (27 Nov 2026), target without extension, what it is
PICKS = [
    ('item-medical-chest', 2, f'{RADIO}/Things/MedicalChest', 'the numbered medical chest, an item lying in the game world'),
    ('icon-repeater-kit', 1, f'{RADIO_VF}/Fittings/RepeaterKit',
     "the repeater kit's icon in the vehicle fittings window and its power load, which had borrowed the mast antenna's picture"),
    ('relay-mast', 2, f'{RADIO}/World/RelayMast',
     "the relay mast's and the radio relay post's world map icon and the convoy's Put up a relay mast button, which had borrowed the "
     "mast antenna's picture"),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    for name, pick, target, what in PICKS:
        master = GENERATED / name / f'{name}-{pick}.png'
        other = 3 - pick
        base = ROOT / target
        single = base.with_suffix('.png')
        single.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(single),
                        '--size', str(SIZE), '--aspect', '1:1'], check=True, stdout=subprocess.DEVNULL)
        lines = [f'# {base.name}', '',
                 f'Made on 26 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, {what}. '
                 f'Picked by the owner from two attempts (attempt {pick}, 27 Nov 2026), from the review card drawn by '
                 f'`scripts/art/review_cards.py sprites`. Exported by `scripts/art/export_sprite.py` at {SIZE} px (1:1) after the '
                 'background AI had looked at the master, by `assets/phobos-radio/ship_advice_art.py`. The prompt and its references '
                 f'are in `{REQUEST}`; the master and its record go to the `art-masters` branch, attempt {other} to `art-references`. '
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
