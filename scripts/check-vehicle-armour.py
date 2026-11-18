"""Check our vehicles' armour without the game: every part by class on both scales, one outer part to a cell, and no upgrade that armours
a vehicle before it is fitted.

    python scripts/check-vehicle-armour.py             check (part of the gate)
    python scripts/check-vehicle-armour.py show        each armoured vehicle's parts, classes and outer cells, as text

The design is docs/development/vehicle-armour.md ("The gate check"); the owner's choices of 19 Nov 2026 for the service trucks are in
docs/development/vehicle-militarised-service-vehicles.md. Reads XML only. A vehicle is checked when its VehicleDef carries
Phobos.VehicleSystems.PhobosArmourExtension, our mark for a design armoured by class. For each one it fails on:

- a part with no armour of its own, or whose sharp and blunt ratings are not one class's in
  mods/PhobosFramework/.../ArmourClasses.xml (Vehicle Framework's scale);
- a part the Combat Extended patches (mods/*/Mods/CombatExtended/Patches) leave at its game-scale figure, or give figures that are not
  the same class's millimetres and megapascals (a 0.10 rating read as 0.10 mm is the fault this exists for);
- a cell of its footprint with two outer parts, or none (owner, 19 Nov 2026: each cell keeps exactly one outer part).

And for every upgrade tree of ours: an armour entry of type Set on a node with no upgradeExplanation, which Vehicle Framework applies as
soon as the Upgrades tab shows the node (VehicleUpgrade's description writes it), and a stat entry of type Set, which does nothing.
Exit code 1 when anything is wrong.
"""
import argparse
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CLASSES_FILE = Path('mods/PhobosFramework/Mods/VehicleFramework/Common/Defs/Phobos/Framework/VehicleSystems/ArmourClasses.xml')
CLASS_TAG = 'Phobos.VehicleSystems.PhobosArmourClassDef'
VEHICLE_TAG = 'Vehicles.VehicleDef'
TREE_TAG = 'Vehicles.UpgradeTreeDef'
ARMOUR_EXTENSION = 'Phobos.VehicleSystems.PhobosArmourExtension'
CE_PATCHES = 'Mods/CombatExtended/Patches'
ALL_PARTS = re.compile(r'^Defs/Vehicles\.VehicleDef\[defName="([^"]+)"\]/components/li/armor$')
ONE_PART = re.compile(r'^Defs/Vehicles\.VehicleDef\[defName="([^"]+)"\]/components/li\[key="([^"]+)"\]/armor$')
CELL = re.compile(r'\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)')


@dataclass
class ArmourClass:
    name: str
    sharp_rating: float
    blunt_rating: float
    sharp_mm: float
    blunt_mpa: float


@dataclass
class Part:
    key: str
    outer: bool
    cells: set
    sharp: float | None
    blunt: float | None
    ce_sharp: float | None = None
    ce_blunt: float | None = None


@dataclass
class Vehicle:
    def_name: str
    source: str
    size: tuple
    parts: list = field(default_factory=list)


def same(figure, class_figure):
    return figure is not None and abs(figure - class_figure) <= 0.001 + 0.001 * abs(class_figure)


def number(text):
    try:
        return float(text)
    except (TypeError, ValueError):
        return None


def load_classes(root):
    path = root / CLASSES_FILE
    if not path.exists():
        return []
    found = []
    for el in ET.parse(path).getroot().iter(CLASS_TAG):
        found.append(ArmourClass(el.findtext('defName', ''), float(el.findtext('sharpRating', 'nan')),
                                 float(el.findtext('bluntRating', 'nan')), float(el.findtext('sharpMillimetres', 'nan')),
                                 float(el.findtext('bluntMegapascals', 'nan'))))
    return found


def footprint(size):
    width, length = size
    return {(x, z) for x in range(-((width - 1) // 2), width // 2 + 1) for z in range(-((length - 1) // 2), length // 2 + 1)}


def part_cells(component, size):
    hitbox = component.find('hitbox')
    if hitbox is None:
        return footprint(size)
    cells = hitbox.find('cells')
    if cells is not None and len(cells):
        return {(int(m.group(1)), int(m.group(2))) for li in cells for m in [CELL.search(li.text or '')] if m}
    frm, to = CELL.search(hitbox.findtext('from', '') or ''), CELL.search(hitbox.findtext('to', '') or '')
    if frm and to:
        x0, z0, x1, z1 = int(frm.group(1)), int(frm.group(2)), int(to.group(1)), int(to.group(2))
        return {(x, z) for x in range(min(x0, x1), max(x0, x1) + 1) for z in range(min(z0, z1), max(z0, z1) + 1)}
    return footprint(size)


def armour(component):
    el = component.find('armor')
    if el is None:
        return None, None
    return number(el.findtext('ArmorRating_Sharp')), number(el.findtext('ArmorRating_Blunt'))


def xml_files(root):
    for path in sorted((root / 'mods').rglob('*.xml')):
        if 'Languages' in path.parts or 'Textures' in path.parts:
            continue
        yield path


def parse(path):
    try:
        return ET.parse(path).getroot()
    except ET.ParseError:
        return None


def load_vehicles(root):
    vehicles = {}
    for path in xml_files(root):
        if CE_PATCHES.replace('/', '\\') in str(path) or CE_PATCHES in path.as_posix():
            continue
        doc = parse(path)
        if doc is None:
            continue
        for el in doc.iter(VEHICLE_TAG):
            exts = el.find('modExtensions')
            if exts is None or not any(li.get('Class') == ARMOUR_EXTENSION for li in exts):
                continue
            m = CELL.search(el.findtext('size', '') or '')
            size = (int(m.group(1)), int(m.group(2))) if m else (1, 1)
            vehicle = Vehicle(el.findtext('defName', ''), path.relative_to(root).as_posix(), size)
            for component in el.find('components') if el.find('components') is not None else []:
                sharp, blunt = armour(component)
                vehicle.parts.append(Part(component.findtext('key', ''), (component.findtext('depth') or 'External') == 'External',
                                          part_cells(component, size), sharp, blunt))
            vehicles[vehicle.def_name] = vehicle
    return vehicles


def apply_ce_patches(root, vehicles):
    problems = []
    for path in sorted((root / 'mods').glob(f'*/{CE_PATCHES}/*.xml')):
        doc = parse(path)
        if doc is None:
            problems.append(f'{path.relative_to(root).as_posix()}: not readable XML')
            continue
        for op in doc.iter('Operation'):
            xpath = (op.findtext('xpath') or '').strip()
            every, one = ALL_PARTS.match(xpath), ONE_PART.match(xpath)
            if not every and not one:
                continue
            def_name = (every or one).group(1)
            vehicle = vehicles.get(def_name)
            if vehicle is None:
                continue
            if op.get('Class') != 'PatchOperationReplace':
                problems.append(f'{path.relative_to(root).as_posix()}: armour of {def_name} must be replaced (PatchOperationReplace)')
                continue
            value = op.find('value/armor')
            sharp = number(value.findtext('ArmorRating_Sharp')) if value is not None else None
            blunt = number(value.findtext('ArmorRating_Blunt')) if value is not None else None
            for part in vehicle.parts:
                if every or part.key == one.group(2):
                    part.ce_sharp, part.ce_blunt = sharp, blunt
    return problems


def class_of(classes, sharp, blunt, scale):
    for c in classes:
        if scale == 'rating' and same(sharp, c.sharp_rating) and same(blunt, c.blunt_rating):
            return c
        if scale == 'ce' and same(sharp, c.sharp_mm) and same(blunt, c.blunt_mpa):
            return c
    return None


def check_vehicle(vehicle, classes):
    problems = []
    where = f'{vehicle.source}: {vehicle.def_name}'
    for part in vehicle.parts:
        if part.sharp is None or part.blunt is None:
            problems.append(f'{where}: part {part.key} has no armour of its own (sharp and blunt)')
            continue
        game = class_of(classes, part.sharp, part.blunt, 'rating')
        if game is None:
            problems.append(f'{where}: part {part.key} armour {part.sharp}/{part.blunt} is no class\'s rating')
            continue
        if part.ce_sharp is None:
            problems.append(f'{where}: part {part.key} has no Combat Extended armour, so it would read {part.sharp} mm')
            continue
        ce = class_of(classes, part.ce_sharp, part.ce_blunt, 'ce')
        if ce is None or ce.name != game.name:
            problems.append(f'{where}: part {part.key} is {game.name} in the Def but {part.ce_sharp} mm / {part.ce_blunt} MPa with Combat '
                            f'Extended; {game.name} is {game.sharp_mm:g} / {game.blunt_mpa:g}')
    cells = footprint(vehicle.size)
    for cell in sorted(cells, key=lambda c: (-c[1], c[0])):
        outer = [p.key for p in vehicle.parts if p.outer and cell in p.cells]
        if len(outer) > 1:
            problems.append(f'{where}: cell {cell} has {len(outer)} outer parts ({", ".join(outer)}); Combat Extended stacks their armour')
        elif not outer:
            problems.append(f'{where}: cell {cell} has no outer part')
    for part in vehicle.parts:
        outside = sorted(part.cells - cells)
        if outside:
            problems.append(f'{where}: part {part.key} has cells outside its {vehicle.size[0]} x {vehicle.size[1]} footprint: {outside}')
    return problems


def check_trees(root):
    problems = []
    for path in xml_files(root):
        doc = parse(path)
        if doc is None:
            continue
        for node in doc.iter('li'):
            upgrades = node.find('upgrades')
            key = node.findtext('key')
            if upgrades is None or key is None:
                continue
            where = f'{path.relative_to(root).as_posix()}: node {key}'
            explained = node.find('upgradeExplanation') is not None
            for upgrade in upgrades:
                kind = upgrade.get('Class') or ''
                if kind.endswith('VehicleUpgrade'):
                    for entry in upgrade.findall('armor/li'):
                        if (entry.findtext('type') or '').strip() == 'Set' and not explained:
                            problems.append(f'{where}: armour of type Set with no upgradeExplanation is applied as soon as the Upgrades '
                                            f'tab shows the node; use Add, or give the node an upgradeExplanation')
                if kind.endswith('StatUpgrade'):
                    for entry in upgrade.findall('stats/li') + upgrade.findall('vehicleStats/li'):
                        if (entry.findtext('type') or '').strip() == 'Set':
                            problems.append(f'{where}: a stat of type Set does nothing in Vehicle Framework; use Add')
    return problems


def run(root):
    classes = load_classes(root)
    if not classes:
        return [f'{CLASSES_FILE.as_posix()}: no armour classes found'], {}
    vehicles = load_vehicles(root)
    problems = apply_ce_patches(root, vehicles)
    for vehicle in vehicles.values():
        problems += check_vehicle(vehicle, classes)
    problems += check_trees(root)
    return problems, vehicles


def show(vehicles, classes):
    for vehicle in vehicles.values():
        print(f'{vehicle.def_name} ({vehicle.size[0]} x {vehicle.size[1]}), {vehicle.source}')
        for part in vehicle.parts:
            game = class_of(classes, part.sharp, part.blunt, 'rating')
            print(f'  {part.key:22} {"outer" if part.outer else "inner":5} {game.name if game else "no class":28} cells {len(part.cells)}')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    parser.add_argument('mode', nargs='?', choices=['check', 'show'], default='check')
    parser.add_argument('--root', type=Path, default=ROOT)
    args = parser.parse_args(argv)
    problems, vehicles = run(args.root)
    if args.mode == 'show':
        show(vehicles, load_classes(args.root))
    if problems:
        print(f'Vehicle armour: {len(problems)} faults.')
        for problem in problems:
            print('- ' + problem)
        return 1
    print(f'Vehicle armour: {len(vehicles)} armoured vehicles checked; every part by class on both scales, one outer part to a cell.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
