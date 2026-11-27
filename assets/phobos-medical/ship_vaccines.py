"""Ship the owner's pick of the vaccine picture (27 Nov 2026), and derive the four vaccines from it by script, reproducibly.

    python assets/phobos-medical/ship_vaccines.py PICK [--out FOLDER]

One paid master (a tray of three vials with green caps) is picked on the Picks page. The four vaccines are made from it: the caps keep
green for bacterial, and are turned blue for viral, amber for protozoan and violet for worms, by rotating the hue of the green pixels
only (their saturation and brightness kept), so the four match exactly (paid rule 11: a free derivation over a paid one). Each is exported
by scripts/art/export_sprite.py at 128 px, as Medical's other items are, and gets its record under assets/records. With --out, the
pictures go to that folder instead and no record is written (a look before the pick).
"""
import argparse
import colorsys
import hashlib
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REQUEST = 'assets/phobos-medical/requests/vaccines-paid.json'
GENERATED = ROOT / 'assets' / 'phobos-medical' / 'generated' / 'vaccine-tray'
TARGETS = ROOT / 'mods/PhobosMedical/Common/Textures/Phobos/Medical/Things'
RECORDS = ROOT / 'assets/records/PhobosMedical/Common/Textures/Phobos/Medical/Things'
SIZE = 128
# Class, the hue its caps are turned to (None keeps the green), and the colour named in the item's description.
CLASSES = [('Bacterial', None, 'green'), ('Viral', 0.60, 'blue'), ('Protozoan', 0.105, 'amber'), ('Worms', 0.78, 'violet')]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def recolour(master, hue, into):
    picture = Image.open(master).convert('RGB')
    if hue is not None:
        pixels = picture.load()
        width, height = picture.size
        for y in range(height):
            for x in range(width):
                r, g, b = pixels[x, y]
                h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
                # The caps' green, and its blend into the outline and the crimp: only clearly green pixels move.
                if 0.22 <= h <= 0.47 and s > 0.2:
                    nr, ng, nb = colorsys.hsv_to_rgb(hue, s, v)
                    pixels[x, y] = (round(nr * 255), round(ng * 255), round(nb * 255))
    picture.save(into)


def export(master, target):
    target.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(target), '--size', str(SIZE)],
                   check=True, stdout=subprocess.DEVNULL)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('pick', type=int)
    parser.add_argument('--out', help='write the pictures here for a look, with no records')
    args = parser.parse_args()
    master = GENERATED / f'vaccine-tray-{args.pick}.png'
    out = Path(args.out) if args.out else None
    for name, hue, colour in CLASSES:
        derived = GENERATED / f'vaccine-{name.lower()}-derived.png'
        recolour(master, hue, derived)
        target = (out / f'Vaccine{name}.png') if out else TARGETS / f'Vaccine{name}.png'
        export(derived, target)
        if out:
            continue
        how = ('Picked by the owner from two attempts (attempt %d) on the Picks page; its green caps kept.' % args.pick if hue is None else
               'Derived by script from the owner\'s pick (attempt %d): its green caps turned %s, nothing else changed. No picture was paid '
               'for it.' % (args.pick, colour))
        lines = [f'# Vaccine{name}', '',
                 f'**AI-generated, picked by the owner on 27 November 2026.** Made on 27 Nov 2026 with OpenAI gpt-image-2 through the image API, at '
                 f'medium quality: the {name.lower()} vaccine, a tray of three vials whose {colour} caps mark the kind. {how} Exported by '
                 f'`scripts/art/export_sprite.py` (white background to transparency, {SIZE} x {SIZE} px) by `assets/phobos-medical/ship_vaccines.py`. '
                 f'The prompt and its references are in `{REQUEST}`; the master and its record go to the `art-masters` branch, the other attempt to '
                 '`art-references`. The only references uploaded were our own approved medicine icons. No game or other author\'s art is in this file.',
                 '', f'- Master: `{derived.name}`, SHA-256 `{sha(derived)}`', f'- `Vaccine{name}.png`: SHA-256 `{sha(target)}`', '']
        RECORDS.mkdir(parents=True, exist_ok=True)
        (RECORDS / f'Vaccine{name}.record.md').write_text('\n'.join(lines), encoding='utf-8', newline='\n')
        print(target.relative_to(ROOT))


if __name__ == '__main__':
    main()
