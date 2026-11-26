"""Lay the reverse-osmosis kit's cabinet on the water bowser and the Ullage, and export its cartridge and icon, reproducibly (D'thorga
Framework 0.318.0, Vehicles 0.124.0; docs/development/vehicle-road-work.md). Free: it reads the owner's picks and writes new files.

    python assets/phobos-vehicles/export_ro_kit.py stage       the picks laid on, in the art working folder, with a preview
    python assets/phobos-vehicles/export_ro_kit.py ship        into the mods

The cabinet was drawn alone on white (assets/phobos-vehicles/requests/ro-kit-paid.json), as the owner's rule says for a part that sits on a
vehicle, and is laid on by script, as export_remote_kit.py lays the antenna: its drawn bounds are fitted, keeping their proportions, into
a box given in the body's in-game pixels (128 a cell, north facing), turned into the master's pixels through the body's own transform, so
east and south register by construction. On the bowser it lies across the rear deck in the dark box's place, beside the hose reel, so it
is turned a quarter first; on the Ullage it stands on the left frame strip between the brackets behind the cab, where the tank leaves
room. Boxes read by the agent off the shipped north textures on a 64-pixel grid, 26 Nov 2026: agent placement, to revise once seen in game.
"""
import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import export_batch_b as base  # noqa: E402
import art_paths  # noqa: E402
from export_remote_kit import master_box  # noqa: E402
from repaint_vehicles import tinted  # noqa: E402

CABINET = ('ro-cabinet', 2)  # The owner's pick of 26 Nov 2026: "2: squarer, neater pipes".
CARTRIDGE = ('item-membrane-cartridge', 1)  # The owner's pick of 26 Nov 2026: "1: crisper".
NAME = 'ROKit'
# vehicle folder: (box in the body's in-game pixels, north facing; turned a quarter first)
BOXES = {
    'PintleBowser': ((308, 974, 404, 1046), True),
    'BulkTanker': ((110, 656, 168, 734), False),
}
TEXTURES = ROOT / 'mods' / 'PhobosVehicles' / 'Textures' / 'Phobos' / 'Vehicles'
HYGIENE = ROOT / 'mods' / 'PhobosFramework' / 'Mods' / 'VehiclesHygiene' / 'Common' / 'Textures' / 'Phobos' / 'VehicleSystems'
ITEM = HYGIENE / 'Parts' / 'MembraneCartridge.png'
ICON = HYGIENE / 'Tank' / 'ReverseOsmosisKit.png'


def turned(master):
    """The cabinet turned a quarter clockwise, as a new file beside the derived masters (the original is never replaced)."""
    base.DERIVED.mkdir(parents=True, exist_ok=True)
    target = base.DERIVED / f'{Path(master).stem}-turned.png'
    if not target.exists():
        Image.open(master).convert('RGB').transpose(Image.Transpose.ROTATE_270).save(target)
    return target


def lay(out):
    master = base.pick(*CABINET)
    written = []
    for folder, (game_box, turn) in BOXES.items():
        target = out / folder
        target.mkdir(parents=True, exist_ok=True)
        name = f'{folder}_{NAME}'
        base.export_overlay(turned(master) if turn else master, base.EXPORT / f'{folder}.transform.json', target, name,
                            master_box(f'{folder}.transform.json', game_box))
        written.append((folder, name, target))
    return master, written


def sprite(master, target, size):
    target.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(target), '--size', str(size)],
                   check=True, stdout=subprocess.DEVNULL)


def preview(written, work):
    """Each vehicle tinted on dark ground, north facing: as it is, and with the cabinet."""
    cells = []
    for folder, name, target in written:
        colours = json.loads((base.EXPORT / f'{folder}.transform.json').read_text(encoding='utf-8'))['colours']
        body = tinted(TEXTURES / folder / f'{folder}_north.png', TEXTURES / folder / f'{folder}_northm.png',
                      tuple(colours['color']), tuple(colours['colorTwo']))
        if folder == 'BulkTanker':
            body.alpha_composite(Image.open(TEXTURES / folder / f'{folder}_LiquidTank_north.png').convert('RGBA'))
        fitted = body.copy()
        fitted.alpha_composite(Image.open(target / f'{name}_north.png').convert('RGBA'))
        for picture in (body, fitted):
            ground = Image.new('RGBA', picture.size, (40, 40, 44, 255))
            ground.alpha_composite(picture)
            cells.append(ground)
    height = max(c.height for c in cells)
    sheet = Image.new('RGB', (sum(c.width + 8 for c in cells), height), (20, 20, 20))
    x = 0
    for cell in cells:
        sheet.paste(cell.convert('RGB'), (x, 0))
        x += cell.width + 8
    target = work / 'ro-kit-preview.png'
    sheet.save(target)
    print('preview', target)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('command', choices=['stage', 'ship'])
    args = parser.parse_args()
    if args.command == 'stage':
        work = art_paths.work('ro-kit')
        work.mkdir(parents=True, exist_ok=True)
        _, written = lay(work)
        preview(written, work)
        return
    master, written = lay(TEXTURES)
    for folder, name, target in written:
        for record in art_paths.records_under(target, f'{name}.export.json'):
            shutil.move(record, base.EXPORT / record.name)  # beside the bodies' records, where the clean-up tools read them
        print('wrote', target / f'{name}_north.png')
    sprite(base.pick(*CARTRIDGE), ITEM, 64)
    print('wrote', ITEM)
    sprite(master, ICON, 128)
    print('wrote', ICON)


if __name__ == '__main__':
    main()
