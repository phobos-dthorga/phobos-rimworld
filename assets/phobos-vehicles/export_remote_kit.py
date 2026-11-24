"""Fit the remote control kit's antenna onto the five service trucks' cabs, reproducibly (D'thorga Vehicles 0.117.2;
docs/development/autonomous-vehicles.md, step 4). Free: it reads the owner's pick and writes new files, with batch B's helpers.

    python assets/phobos-vehicles/export_remote_kit.py --out <artWorkPath>/remote-kit-stage --preview      stage and preview
    python assets/phobos-vehicles/export_remote_kit.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles  ship

The antenna was drawn alone on white (assets/phobos-vehicles/requests/remote-kit-antenna-record.md) and is laid on each truck by
script, as the owner's rule says for a part that sits on a vehicle: its drawn bounds are fitted into a box on the cab roof, given in
each body's in-game pixels (128 a cell, north facing), turned into the master's pixels through the body's own transform. East and
south follow from the body's transform, so they register by construction. Agent placement, to revise once seen in game.
"""
import argparse
import json
import shutil
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parents[1] / 'scripts' / 'art'))
import export_batch_b as base  # noqa: E402
import art_paths  # noqa: E402
from repaint_vehicles import tinted  # noqa: E402

PICK = 1  # The owner's pick of 25 Nov 2026: "Perhaps #1?"
NAME = 'RemoteKit'
# The mount near the front of each cab roof, on its right, the whip tied back along the roof: 190 game pixels long (about 1.5 cells)
# and as wide as the picture's own shape gives (0.18 of its length). Roofs read from the shipped north textures on a 64-pixel grid,
# 25 Nov 2026: the roof is the pale panel between the windscreen and the orange stripe. Agent placement, to revise once seen in game.
BOXES = {
    'Tipper': (321, 170, 355, 360),
    'BulkTanker': (336, 242, 370, 432),
    'DrillRig': (316, 200, 350, 390),
    'SurveyTruck': (336, 108, 370, 298),
    'GeneratorSet': (321, 175, 355, 365),
}
TEXTURES = HERE.parents[1] / 'mods' / 'PhobosVehicles' / 'Textures' / 'Phobos' / 'Vehicles'


def master_box(transform, box):
    """A box in the body's in-game pixels to its master's pixels: the inverse of the exporter's scale and offset."""
    data = json.loads((base.EXPORT / transform).read_text(encoding='utf-8'))
    scale, (left, top) = data['scale'], data['offset']
    return tuple(round((value - (left if index % 2 == 0 else top)) / scale) for index, value in enumerate(box))


def preview(out, work):
    """Each truck tinted on dark ground, north facing, its cab end: as it is, and with the antenna."""
    cells = []
    for folder in BOXES:
        colours = json.loads((base.EXPORT / f'{folder}.transform.json').read_text(encoding='utf-8'))['colours']
        body = tinted(TEXTURES / folder / f'{folder}_north.png', TEXTURES / folder / f'{folder}_northm.png',
                      tuple(colours['color']), tuple(colours['colorTwo']))
        fitted = body.copy()
        fitted.alpha_composite(Image.open(out / folder / f'{folder}_{NAME}_north.png').convert('RGBA'))
        for picture in (body, fitted):
            ground = Image.new('RGBA', (picture.width, 560), (40, 40, 44, 255))
            ground.alpha_composite(picture.crop((0, 0, picture.width, 560)))
            cells.append(ground)
    sheet = Image.new('RGB', (sum(c.width + 8 for c in cells), 560), (20, 20, 20))
    x = 0
    for cell in cells:
        sheet.paste(cell.convert('RGB'), (x, 0))
        x += cell.width + 8
    target = work / 'remote-kit-preview.png'
    sheet.save(target)
    print('preview', target)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', required=True, help='folder holding one subfolder per vehicle')
    parser.add_argument('--preview', action='store_true', help='also draw each truck tinted, without and with the antenna')
    args = parser.parse_args()
    work = art_paths.work('remote-kit')
    work.mkdir(parents=True, exist_ok=True)
    antenna = base.pick('remote-kit-antenna', PICK)
    report = {}
    for folder, game_box in BOXES.items():
        out = Path(args.out) / folder
        out.mkdir(parents=True, exist_ok=True)
        like = base.EXPORT / f'{folder}.transform.json'
        box = master_box(f'{folder}.transform.json', game_box)
        name = f'{folder}_{NAME}'
        base.export_overlay(antenna, like, out, name, box)
        for record in art_paths.records_under(out, f'{name}.export.json'):
            shutil.move(record, base.EXPORT / record.name)  # beside the bodies' records, where the clean-up tools read them
        report[f'{folder}/{name}'] = {'master': antenna.name, 'game_box': list(game_box), 'fit': list(box)}
        print('wrote', folder, name)
    (work / 'remote-kit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    if args.preview:
        preview(Path(args.out), work)


if __name__ == '__main__':
    main()
