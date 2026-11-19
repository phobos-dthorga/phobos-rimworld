"""Export D'thorga Farming's machines from their picked masters (docs/development/farming-batch-one.md, "Art and sound plan").

    python assets/phobos-vehicles/export_farming.py [--mods mods]

The combine's body and the chaser bin's body go through export_vehicle.py's own body export. The combine's grain fronts were drawn alone
(the 8-cell front is wider than the body's picture), so they are placed here: cut from the white with the background AI's mask, scaled so
the front is exactly as many cells wide as it cuts (6 or 8, owner's widths of 20 Nov 2026) at 128 px a cell, centred across the combine,
and set with the bottom of its throat just over the top of the body, on a canvas the size of the body's. Run the background AI on the
masters first (scripts/art/clean_backgrounds.py masks --also ...). Free: no generation, and the masters are never touched.
"""
import argparse
import json
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
from export_sprite import ai_background, export as export_sprite, white_to_alpha  # noqa: E402
from export_vehicle import PIXELS_PER_CELL, export_body, record, write_facings  # noqa: E402

GENERATED = ROOT / 'assets' / 'phobos-vehicles' / 'generated'
COMBINE = GENERATED / 'farming-combine-body-north' / 'farming-combine-body-north-1.png'
CHASER_BIN = GENERATED / 'farming-chaser-bin-body-north' / 'farming-chaser-bin-body-north-3.png'
# (picture name, master, cells wide): the owner's picks of 20 Nov 2026, from the sharper round (high quality at about 3072 px wide) that
# replaced the first medium-quality picks once the corn header showed the first way lost the fine details.
FRONTS = [
    ('Combine_FrontStandard', GENERATED / 'farming-combine-front-standard-sharp' / 'farming-combine-front-standard-sharp-2.png', 6),
    ('Combine_FrontWide', GENERATED / 'farming-combine-front-wide-sharp' / 'farming-combine-front-wide-sharp-2.png', 8),
    # The corn header (owner, 20 Nov 2026): the sharper round's attempt 2, high quality at 3072 x 1024.
    ('Combine_FrontCorn', GENERATED / 'farming-combine-front-corn-sharp' / 'farming-combine-front-corn-sharp-2.png', 6),
]
# (picture name, master, cells square): the field bin and the silo, attempt 2 of each the default (owner, 20 Nov 2026; attempt 1 of each is
# kept as an alternate look, exported later). Single pictures, round from above, so one facing does for all four.
BUILDINGS = [
    ('FieldBin', GENERATED / 'farming-field-bin' / 'farming-field-bin-2.png', 3),
    ('Silo', GENERATED / 'farming-silo' / 'farming-silo-2.png', 4),
]
# The body 9 cells long on an 8 x 14 picture: wide enough for the 8-cell front, long enough for its depth ahead of the body.
COMBINE_DRAW = (8, 14)
# Harvest green as the main paint, the muted yellow as the accent, in degrees of hue (measured by eye from the masters).
MAIN_HUE = (70, 150)
ACCENT_HUE = (35, 60)
# How far the throat reaches over the body's front edge, in pixels at game size, so the two join with no gap.
THROAT_OVERLAP = 6


def opaque_box(image):
    return image.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox()


def body_top(master, transform):
    """Where the body's front edge lies on the canvas, in pixels from the top."""
    cut = white_to_alpha(Image.open(master), background=ai_background(master))
    return round(transform['offset'][1] + opaque_box(cut)[1] * transform['scale'])


def export_front(name, master, cells, out, transform, top):
    background = ai_background(master)
    if background is None:
        raise SystemExit(f'{master.name}: the background AI has not looked at it yet (clean_backgrounds.py masks --also {master})')
    cut = white_to_alpha(Image.open(master), background=background)
    content = cut.crop(opaque_box(cut))
    width = cells * PIXELS_PER_CELL
    height = round(content.height * width / content.width)
    content = content.convert('RGBa').resize((width, height), Image.LANCZOS).convert('RGBA')
    canvas = tuple(transform['canvas'])
    if height > top + THROAT_OVERLAP:
        raise SystemExit(f'{name}: {height} px deep, but only {top} px lie ahead of the body: make the picture longer')
    image = Image.new('RGBA', canvas, (0, 0, 0, 0))
    image.alpha_composite(content, ((canvas[0] - width) // 2, top + THROAT_OVERLAP - height))
    written = write_facings(image, out, name)
    record(out, name, master, written, {'kind': 'front', 'cells_wide': cells, 'like': 'Combine.transform.json', 'top': top,
                                        'throat_overlap': THROAT_OVERLAP})
    return written


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--mods', default=str(ROOT / 'mods'), help='the mods folder to write into')
    args = parser.parse_args()
    textures = Path(args.mods) / 'PhobosFarming' / 'Common' / 'Textures' / 'Phobos' / 'Farming'
    combine = export_body(COMBINE, textures / 'Combine', 'Combine', 9, COMBINE_DRAW, MAIN_HUE, ACCENT_HUE)
    print(f"Combine: {combine['width_cells']:.2f} cells wide; Def colours {combine['colours']}")
    chaser = export_body(CHASER_BIN, textures / 'ChaserBin', 'ChaserBin', 7, (4, 8), MAIN_HUE, ACCENT_HUE)
    print(f"ChaserBin: {chaser['width_cells']:.2f} cells wide; Def colours {chaser['colours']}")
    for name, master, cells in BUILDINGS:
        target = textures / name / f'{name}.png'
        target.parent.mkdir(parents=True, exist_ok=True)
        export_sprite(master, target, cells * PIXELS_PER_CELL, '1:1', 235, 30)
        print(f'  {target}')
    top = body_top(COMBINE, combine['transform'])
    for name, master, cells in FRONTS:
        for path in export_front(name, master, cells, textures / 'Combine', combine['transform'], top):
            print(f'  {path}')


if __name__ == '__main__':
    main()
