"""Fit the owner's picked parts of 12 Nov 2026 onto their vehicles' bodies, reproducibly. Free: it only reads the picked masters and writes
new files. Each part is placed by a box in its body master's pixels (export_vehicle.py overlay --fit), read off the picked body; mirrored
parts are made by flipping the picked master into a new derived file, never by changing it. Pictures that show together on one overlay
(the four outriggers and the cable when the generator set is set up) are layered into one texture.

    python assets/phobos-vehicles/export_batch_b.py --out .local/art/batch-b-export        stage and preview
    python assets/phobos-vehicles/export_batch_b.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles   ship

The record of the picks is assets/phobos-vehicles/requests/night-2026-11-12-record.md.
"""
import argparse
import json
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageOps

GEN = Path('assets/phobos-vehicles/generated')
DERIVED = GEN / 'derived-2026-11-12'
EXPORT = GEN / 'export'


def pick(name, seed):
    return GEN / name / f'{name}-{seed}.png'


def mirrored(source):
    """A left-right flip of a picked master, as a new file beside the others."""
    DERIVED.mkdir(parents=True, exist_ok=True)
    target = DERIVED / f'{source.stem}-mirrored.png'
    if not target.exists():
        ImageOps.mirror(Image.open(source)).save(target)
    return target


def stretched(source, factor):
    """A lengthwise stretch of a picked master (the tall mast from the folded one), on a canvas tall enough to hold it."""
    DERIVED.mkdir(parents=True, exist_ok=True)
    target = DERIVED / f'{source.stem}-stretched-{factor:g}.png'
    if not target.exists():
        image = Image.open(source).convert('RGB')
        box = image.convert('L').point(lambda v: 255 if v < 235 else 0).getbbox()
        part = image.crop(box)
        part = part.resize((part.width, round(part.height * factor)), Image.LANCZOS)
        canvas = Image.new('RGB', (image.width, max(image.height, part.height + 64)), (255, 255, 255))
        canvas.paste(part, ((canvas.width - part.width) // 2, (canvas.height - part.height) // 2))
        canvas.save(target)
    return target


# Every overlay: (vehicle folder, texture name, [(part master, fit box in the body master's pixels), ...]).
# Boxes read off the picked bodies on 12 Nov 2026 (agent placement, owner to revise once seen in game).
OUTRIGGER = pick('survey-truck-outrigger', 1)
LAMPS = pick('survey-truck-work-lamps', 2)
BATTERY = pick('survey-truck-battery-box', 1)

VEHICLES = {
    'GeneratorSet': {
        'transform': 'GeneratorSet.transform.json',
        'overlays': [
            ('GeneratorSet_Fan', [(pick('generator-set-fan', 1), (322, 754, 446, 878))]),
            ('GeneratorSet_FanHeavy', [(pick('generator-set-fan', 2), (322, 754, 446, 878))]),
            ('GeneratorSet_SetUp', [
                (OUTRIGGER, (548, 430, 708, 500)), ('mirror', OUTRIGGER, (60, 430, 220, 500)),
                (OUTRIGGER, (548, 1380, 708, 1450)), ('mirror', OUTRIGGER, (60, 1380, 220, 1450)),
                (pick('generator-set-cable-out', 1), (70, 1300, 300, 1530)),
            ]),
            ('GeneratorSet_BiggerGenerator', [(pick('generator-set-bigger-alternator', 1), (262, 1112, 506, 1222))]),
            ('GeneratorSet_Reel', [(pick('generator-set-cable-reel', 2), (262, 1345, 506, 1450))]),
            ('GeneratorSet_Battery', [(BATTERY, (262, 444, 506, 536))]),
            ('GeneratorSet_HeatExchanger', [(pick('generator-set-heat-exchanger', 1), (430, 560, 530, 700))]),
        ],
    },
    'DrillRig': {
        'transform': 'DrillRig.transform.json',
        'overlays': [
            ('DrillRig_Mast', [(pick('drill-rig-mast-folded', 2), (448, 900, 576, 1930))]),
            ('DrillRig_MastTall', [(('stretch', pick('drill-rig-mast-folded', 2), 1.35), (448, 640, 576, 1930))]),
            ('DrillRig_RotaryHead', [(pick('drill-rig-rotary-head', 2), (416, 2040, 608, 2228))]),
            ('DrillRig_SetUp', [
                (OUTRIGGER, (740, 630, 920, 700)), ('mirror', OUTRIGGER, (104, 630, 284, 700)),
                (OUTRIGGER, (740, 1935, 920, 2005)), ('mirror', OUTRIGGER, (104, 1935, 284, 2005)),
            ]),
            ('DrillRig_PowerPack', [(pick('drill-rig-power-pack', 1), (572, 960, 742, 1130))]),
            ('DrillRig_DeepchemHead', [(pick('drill-rig-deepchem-tank', 1), (572, 1180, 742, 1290))]),
            ('DrillRig_DiamondBit', [(pick('drill-rig-diamond-bit', 1), (600, 1790, 700, 1890))]),
            ('DrillRig_Hammer', [(pick('drill-rig-hammer', 1), (600, 1790, 700, 1890))]),
            ('DrillRig_WorkLights', [(LAMPS, (380, 585, 650, 650))]),
        ],
    },
    'Tipper': {
        'transform': 'Tipper.transform.json',
        'overlays': [
            ('Tipper_Body', [(pick('tipper-box', 3), (262, 712, 762, 2010))]),
            ('Tipper_RockBody', [(pick('tipper-rock-body', 1), (262, 712, 762, 2010))]),
            ('Tipper_BulkBody', [(pick('tipper-bulk-body', 2), (262, 712, 762, 2010))]),
        ],
    },
    # The Crawler's places follow docs/development/vehicle-seabed-crawler-art.md, except the listening strips: the picked hull has its
    # forward thrusters where the plan put them, so they sit along the flanks aft of the diving planes (agent, 12 Nov 2026).
    'SeabedCrawler': {
        'transform': 'SeabedCrawler.transform.json',
        'overlays': [
            ('SeabedCrawler_BottomingWheels', [(pick('seabed-crawler-wheel-housing', 3), (880, 870, 990, 1060)),
                                               ('mirror', pick('seabed-crawler-wheel-housing', 3), (34, 870, 144, 1060))]),
            ('SeabedCrawler_Hydrophones', [(pick('seabed-crawler-hydrophone-strip', 1), (845, 1030, 895, 1340)),
                                           ('mirror', pick('seabed-crawler-hydrophone-strip', 1), (129, 1030, 179, 1340))]),
            ('SeabedCrawler_ActiveSonar', [(pick('seabed-crawler-sonar-dome', 3), (340, 45, 684, 290))]),
            ('SeabedCrawler_Manipulator', [(pick('seabed-crawler-manipulator', 1), (190, 180, 370, 420))]),
            ('SeabedCrawler_RadarMast', [(pick('seabed-crawler-radar-mast', 1), (470, 590, 554, 980))]),
            ('SeabedCrawler_SeabedDrill', [(pick('seabed-crawler-seabed-drill', 2), (400, 1150, 624, 1374))]),
        ],
    },
    'SurveyTruck': {
        'transform': 'SurveyTruck.transform.json',
        'overlays': [
            ('SurveyTruck_SetUp', [
                (OUTRIGGER, (790, 1120, 950, 1190)), ('mirror', OUTRIGGER, (74, 1120, 234, 1190)),
                (OUTRIGGER, (790, 1850, 950, 1920)), ('mirror', OUTRIGGER, (74, 1850, 234, 1920)),
            ]),
            ('SurveyTruck_RadiometricProbe', [(pick('survey-truck-probe', 1), (590, 1290, 760, 1600))]),
            ('SurveyTruck_LongRangeArray', [(pick('survey-truck-dish', 2), (400, 800, 624, 1024))]),
            ('SurveyTruck_PoweredArray', [(pick('survey-truck-powered-array', 1), (285, 1280, 455, 1820))]),
            ('SurveyTruck_SurveyBattery', [(BATTERY, (270, 1130, 460, 1262))]),
            ('SurveyTruck_WorkLights', [(LAMPS, (300, 745, 724, 805))]),
        ],
    },
}


def resolve(spec):
    """A part entry to (master file, box)."""
    if spec[0] == 'mirror':
        return mirrored(spec[1]), spec[2]
    source, box = spec
    if isinstance(source, tuple) and source[0] == 'stretch':
        return stretched(source[1], source[2]), box
    return source, box


def on_body_canvas(master, like):
    """The part pasted unscaled at the centre of a white canvas the size of the body's master, as a new file: the exporter registers an
    overlay only on its body's canvas, and --fit then moves and scales the part into its box (as the survey truck's boom was, 9 Nov)."""
    size = tuple(json.loads(Path(like).read_text(encoding='utf-8'))['master_size'])
    part = Image.open(master).convert('RGB')
    if part.size == size:
        return master
    DERIVED.mkdir(parents=True, exist_ok=True)
    target = DERIVED / f'{Path(master).stem}-on-{size[0]}x{size[1]}.png'
    if not target.exists():
        if part.width > size[0] or part.height > size[1]:
            part.thumbnail(size, Image.LANCZOS)
        canvas = Image.new('RGB', size, (255, 255, 255))
        canvas.paste(part, ((size[0] - part.width) // 2, (size[1] - part.height) // 2))
        canvas.save(target)
    return target


def export_overlay(master, like, out, name, box):
    master = on_body_canvas(master, like)
    result = subprocess.run([sys.executable, 'scripts/art/export_vehicle.py', 'overlay', str(master), '--like', str(like), '--out',
                             str(out), '--name', name, '--fit', *map(str, box)], capture_output=True, text=True)
    if result.returncode:
        raise SystemExit(f'{name}: {result.stderr.strip().splitlines()[-1]}')


def layer(out, name, parts):
    """Layers several fitted parts into one texture per facing, in order."""
    for facing in ('north', 'east', 'south'):
        base = None
        for part in parts:
            image = Image.open(out / f'{part}_{facing}.png').convert('RGBA')
            base = image if base is None else Image.alpha_composite(base, image)
        base.save(out / f'{name}_{facing}.png')
    for part in parts:
        for facing in ('north', 'east', 'south'):
            (out / f'{part}_{facing}.png').unlink()
        for extra in out.glob(f'{part}.*'):
            extra.unlink()


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', required=True, help='folder holding one subfolder per vehicle')
    parser.add_argument('--only', action='append', default=[], help='only this vehicle; repeat for more')
    args = parser.parse_args()
    report = {}
    for vehicle, plan in VEHICLES.items():
        if args.only and vehicle not in args.only:
            continue
        out = Path(args.out) / vehicle
        out.mkdir(parents=True, exist_ok=True)
        like = EXPORT / plan['transform']
        for name, specs in plan['overlays']:
            parts = []
            for index, spec in enumerate(specs):
                master, box = resolve(spec)
                part_name = name if len(specs) == 1 else f'{name}__part{index}'
                export_overlay(master, like, out, part_name, box)
                parts.append(part_name)
                report.setdefault(f'{vehicle}/{name}', []).append({'master': str(master).replace('\\', '/'), 'fit': list(box)})
            if len(parts) > 1:
                layer(out, name, parts)
            print('wrote', vehicle, name)
    (Path(args.out) / 'batch-b-overlays.json').write_text(json.dumps(report, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
