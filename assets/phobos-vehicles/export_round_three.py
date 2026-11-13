"""Fit the owner's picks of 12 Nov 2026 (evening) onto their vehicles, reproducibly: the Pintle trailer's greedy boards, the tipper's loading
grab, and the recovery winch and auxiliary fuel tank, one picture of each placed on every truck that can fit them. Free: it reads the picked
masters and writes new files, using batch B's helpers (export_batch_b.py). Boxes are given in each body's in-game pixels (128 a cell, north
facing, as the texture is drawn) and turned into its master's pixels here, so they can be read straight off the textures. Agent placement,
owner to revise once seen in game.

    python assets/phobos-vehicles/export_round_three.py --out .local/art/round-three-export        stage and preview
    python assets/phobos-vehicles/export_round_three.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles   ship
    ... --only FlatbedTrailer                                                                                one vehicle, after its body changed

The record of the picks is assets/phobos-vehicles/requests/round-three-record.md.
"""
import argparse
import json
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import export_batch_b as base  # noqa: E402

TRAILER_BOARDS = 2  # Owner's pick (12 Nov 2026).
GRAB = 3  # Owner's pick.
WINCH = 1  # Owner's pick.
TANK = 2  # Owner's pick.


def turned(source, degrees):
    """A picked master turned (the winch's hook drawn at the bottom, turned to face forward), as a new file."""
    base.DERIVED.mkdir(parents=True, exist_ok=True)
    target = base.DERIVED / f'{source.stem}-turned-{degrees}.png'
    if not target.exists():
        Image.open(source).convert('RGB').rotate(degrees, fillcolor=(255, 255, 255)).save(target)
    return target


def to_box_shape(source, box):
    """A picked master's subject stretched to a box's proportions on a white canvas the master's size, as a new file: the trailer's boards
    must meet its rails on all four sides, which a uniform fit cannot do when the drawn frame is a little wider than the deck."""
    base.DERIVED.mkdir(parents=True, exist_ok=True)
    target = base.DERIVED / f'{source.stem}-shaped-{box[2] - box[0]}x{box[3] - box[1]}.png'
    if not target.exists():
        image = Image.open(source).convert('RGB')
        part = image.crop(image.convert('L').point(lambda v: 255 if v < 235 else 0).getbbox())
        part = part.resize((box[2] - box[0], box[3] - box[1]), Image.LANCZOS)
        canvas = Image.new('RGB', image.size, (255, 255, 255))
        canvas.paste(part, (box[0], box[1]))
        canvas.save(target)
    return target


def master_box(transform, box):
    """A box in the body's in-game pixels to its master's pixels: the inverse of the exporter's scale and offset."""
    data = json.loads(Path(base.EXPORT / transform).read_text(encoding='utf-8'))
    scale, (left, top) = data['scale'], data['offset']
    return tuple(round((value - (left if index % 2 == 0 else top)) / scale) for index, value in enumerate(box))


def plan(boards, grab, winch, tank):
    winch_master = turned(base.pick('fitting-recovery-winch-overlay', winch), 180)
    tank_master = base.pick('fitting-aux-fuel-tank-overlay', tank)
    # The trailer's deck in its picked master's pixels, measured 12 Nov 2026: rails x 224 to 544, headboard y 262 to the rear board 1505.
    deck = (222, 262, 546, 1506)
    boards_master = to_box_shape(base.pick('flatbed-trailer-greedy-boards', boards), deck)
    # The winch over the front bumper, centred, overlapping the bonnet's front edge: every body's front is at y 64 in game.
    bumper = (198, 46, 314, 112)
    trucks = {
        # vehicle folder: (transform, texture prefix, aux tank box in game pixels: between the wheels, in line with their outer edge)
        'Tipper': ('Tipper.transform.json', 'Tipper', (98, 470, 142, 690)),
        'BulkTanker': ('BulkTanker.transform.json', 'BulkTanker', (78, 560, 122, 780)),
        'SurveyTruck': ('SurveyTruck.transform.json', 'SurveyTruck', (74, 360, 116, 580)),
        'DrillRig': ('DrillRig.transform.json', 'DrillRig', (86, 520, 128, 740)),
    }
    # The frame's empty middle is background the frame encloses: cleared from its centre.
    middle = ((deck[0] + deck[2]) // 2, (deck[1] + deck[3]) // 2)
    work = {'FlatbedTrailer': ('FlatbedTrailer.transform.json', [('FlatbedTrailer_GreedyBoards', boards_master, deck, [middle])])}
    for folder, (transform, prefix, tank_box) in trucks.items():
        work[folder] = (transform, [(f'{prefix}_Winch', winch_master, master_box(transform, bumper), []),
                                    (f'{prefix}_AuxFuelTank', tank_master, master_box(transform, tank_box), [])])
    grab_box = master_box('Tipper.transform.json', (131, 352, 381, 418))
    work['Tipper'][1].append(('Tipper_LoadingGrab', base.pick('tipper-loading-grab', grab), grab_box, []))
    return work


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', required=True, help='folder holding one subfolder per vehicle')
    parser.add_argument('--boards', type=int, default=TRAILER_BOARDS)
    parser.add_argument('--grab', type=int, default=GRAB)
    parser.add_argument('--winch', type=int, default=WINCH)
    parser.add_argument('--tank', type=int, default=TANK)
    parser.add_argument('--only', action='append', default=[], metavar='FOLDER',
                        help='re-export one vehicle folder only, such as FlatbedTrailer after its body was re-exported; repeatable')
    args = parser.parse_args()
    report_path = base.EXPORT / 'round-three-overlays.json'
    report = json.loads(report_path.read_text(encoding='utf-8')) if args.only and report_path.is_file() else {}
    for folder, (transform, overlays) in plan(args.boards, args.grab, args.winch, args.tank).items():
        if args.only and folder not in args.only:
            continue
        out = Path(args.out) / folder
        out.mkdir(parents=True, exist_ok=True)
        for name, master, box, holes in overlays:
            base.export_overlay(master, base.EXPORT / transform, out, name, box, holes)
            for record in out.glob(f'{name}.export.json'):
                record.replace(base.EXPORT / record.name)
            report[f'{folder}/{name}'] = {'master': str(master).replace('\\', '/'), 'fit': list(box)}
            print('wrote', folder, name)
    report_path.write_text(json.dumps(report, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
