"""Lay the two parts bought on 25 Nov 2026 on their vehicles, reproducibly: the drill rig's well-pipe rack and the Busbar's jammer mast.

    python assets/phobos-vehicles/export_paid25_parts.py stage                       every try, for the owner's cards
    python assets/phobos-vehicles/export_paid25_parts.py ship --rack N --mast N      the picks, into mods/PhobosVehicles

Each part was drawn alone on white (assets/phobos-vehicles/requests/paid-2026-11-25-parts.json), as the owner's rule says for a part
that sits on a vehicle and stands alone (assets/AGENTS.md), and is laid on by script: its drawn bounds are fitted, keeping their
proportions, into a box on the deck given in the body's in-game pixels (128 a cell, north facing), turned into the master's pixels
through the body's own transform, so east and south register by construction (export_remote_kit.py does the same for the antenna).
Boxes read by the agent off the shipped north textures on a 64-pixel grid, 25 Nov 2026: agent placement, to revise once seen in game.
The mast's upgrade icon is cut from the same picked part (export_sprite.py), so the node and the truck show one thing.
"""
import argparse
import shutil
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import export_batch_b as base  # noqa: E402
import art_paths  # noqa: E402
from export_remote_kit import master_box  # noqa: E402

TEXTURES = ROOT / 'mods' / 'PhobosVehicles' / 'Textures' / 'Phobos' / 'Vehicles'

# part: (request name, vehicle folder, overlay name, box in the body's in-game pixels, north facing)
PARTS = {
    # The rack lies lengthwise on the deck's right-hand strip, between the mast slot and the deck edge, behind the power unit.
    'rack': ('paid25-well-pipe-rack', 'DrillRig', 'DrillRig_WellRack', (280, 600, 392, 1080)),
    # The folded mast lies across the rear deck, between the generator housing and the rear locker.
    'mast': ('paid25-jammer-mast', 'GeneratorSet', 'GeneratorSet_JammerMast', (160, 842, 356, 900)),
}
ICON = TEXTURES / 'GeneratorSet' / 'Upgrades' / 'JammerMast.png'


def lay(part, seed, out):
    request, folder, name, game_box = PARTS[part]
    master = base.pick(request, seed)
    target = out / folder
    target.mkdir(parents=True, exist_ok=True)
    base.export_overlay(master, base.EXPORT / f'{folder}.transform.json', target, name,
                        master_box(f'{folder}.transform.json', game_box))
    return master, target / f'{name}_north.png'


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest='command', required=True)
    sub.add_parser('stage', help="every try laid on its vehicle in the art working folder, for the owner's cards")
    ship = sub.add_parser('ship', help='the picks, into mods/PhobosVehicles')
    ship.add_argument('--rack', type=int, help='the picked try of the pipe rack')
    ship.add_argument('--mast', type=int, help='the picked try of the jammer mast')
    args = parser.parse_args()
    if args.command == 'stage':
        for part in PARTS:
            for seed in (1, 2):
                if base.pick(PARTS[part][0], seed).is_file():
                    _, north = lay(part, seed, art_paths.work('paid25-parts', part, str(seed)))
                    print(part, seed, north)
        return
    for part, seed in (('rack', args.rack), ('mast', args.mast)):
        if seed is None:
            continue
        master, north = lay(part, seed, TEXTURES)
        name = PARTS[part][2]
        for record in art_paths.records_under(north.parent, f'{name}.export.json'):
            shutil.move(record, base.EXPORT / record.name)  # beside the bodies' records, where the clean-up tools read them
        print('wrote', north)
        if part == 'mast':
            ICON.parent.mkdir(parents=True, exist_ok=True)
            subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(ICON), '--size', '128'],
                           check=True, stdout=subprocess.DEVNULL)
            print('wrote', ICON)


if __name__ == '__main__':
    main()
