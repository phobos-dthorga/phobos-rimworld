"""D'thorga Farming's upgrade and button icons, cut from its own exported machine pictures (docs/development/farming-batch-one.md).

    python assets/phobos-vehicles/draw_farming_icons.py [--mods mods]

Each icon is a crop of the part it stands for, from the textures export_farming.py wrote: a front from its own picture, the tank, the tail
and the auger from the bodies. Crops in cells of the north picture, 128 px a cell, from its top left. Each is fitted into a square with a
little margin, at 128 px, and gets a record. Free and deterministic; run export_farming.py first. The local icon batch may replace them.
"""
import argparse
import datetime as _dt
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SIZE = 128
MARGIN = 8

# (icon path under Textures/Phobos/Farming, source picture, crop box in pixels of the north picture or None for its whole content,
# what it shows)
ICONS = [
    ('Upgrades/FrontStandard', 'Combine/Combine_FrontStandard_north.png', None, 'the standard grain front'),
    ('Upgrades/FrontWide', 'Combine/Combine_FrontWide_north.png', None, 'the wide grain front'),
    ('Upgrades/FrontCorn', 'Combine/Combine_FrontCorn_north.png', None, 'the corn header'),
    ('Upgrades/TankExtension', 'Combine/Combine_north.png', (340, 560, 684, 1060), "the combine's grain tank"),
    ('Upgrades/StrawChopper', 'Combine/Combine_north.png', (380, 1330, 644, 1520), "the combine's straw outlet at its tail"),
    ('Upgrades/FastAuger', 'ChaserBin/ChaserBin_north.png', (20, 100, 260, 440), "the chaser bin's unloading auger"),
    ('UI/Harvest', 'Combine/Combine_FrontStandard_north.png', None, 'the standard grain front, for the harvest order'),
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def tinted(textures, source):
    """The picture as the game shows it: a body's painted parts are stored grey and tinted by its paint mask with the Def's colours
    (written to the body's transform file); a picture with no mask is as it is."""
    path = textures / source
    picture = Image.open(path).convert('RGBA')
    mask_path = path.with_name(path.stem + 'm.png')
    transform = next(path.parent.glob('*.transform.json'), None)
    if not mask_path.exists() or transform is None:
        return picture
    colours = json.loads(transform.read_text(encoding='utf-8'))['colours']
    main, accent = colours['color'], colours['colorTwo']
    mask = Image.open(mask_path).convert('RGB')
    pixels, marks = picture.load(), mask.load()
    for y in range(picture.height):
        for x in range(picture.width):
            red, green, blue, alpha = pixels[x, y]
            if alpha == 0:
                continue
            mask_red, mask_green, _ = marks[x, y]
            tint = main if mask_red > 127 else accent if mask_green > 127 else None
            if tint:
                pixels[x, y] = (red * tint[0] // 255, green * tint[1] // 255, blue * tint[2] // 255, alpha)
    return picture


def fit(image):
    box = image.getchannel('A').getbbox()
    image = image.crop(box)
    room = SIZE - 2 * MARGIN
    factor = min(room / image.width, room / image.height)
    image = image.convert('RGBa').resize((max(1, round(image.width * factor)), max(1, round(image.height * factor))), Image.LANCZOS).convert('RGBA')
    square = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 0))
    square.alpha_composite(image, ((SIZE - image.width) // 2, (SIZE - image.height) // 2))
    return square


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mods', default=str(ROOT / 'mods'))
    args = parser.parse_args()
    textures = Path(args.mods) / 'PhobosFarming' / 'Common' / 'Textures' / 'Phobos' / 'Farming'
    for name, source, box, shows in ICONS:
        picture = tinted(textures, source)
        if box:
            picture = picture.crop(box)
        out = textures / f'{name}.png'
        out.parent.mkdir(parents=True, exist_ok=True)
        fit(picture).save(out, optimize=True)
        record = out.with_suffix('.record.md')
        record.write_text(
            f'# {out.stem}: asset record\n\n'
            f'**Cut by script from our own picture**, not generated: {shows}, cropped from `{source}` (SHA-256 `{sha(textures / source)}`) '
            f'by `assets/phobos-vehicles/draw_farming_icons.py` on {_dt.date.today().isoformat()}, tinted with its paint mask and the colours of its Def as the game shows it, '
            f'and fitted into a {SIZE} px square. '
            'That picture is AI-generated (OpenAI gpt-image-2), picked by the owner; its record is beside it. '
            'A stand-in cut from the machine itself until the local icon batch draws its own.\n\n'
            f'- `{out.name}`: SHA-256 `{sha(out)}`\n', encoding='utf-8')
        print(out)


if __name__ == '__main__':
    main()
