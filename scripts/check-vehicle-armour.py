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

And for each armoured vehicle's kits (nodes whose VehicleUpgrade adds armour): every set of kits that can be fitted together (no two
either/or, each with the kits it needs) must leave each part on one class on both scales, so two independent kits on one part are
summed (Framework 0.162.0, after the fault-read of 19 Nov 2026); and each kit's Combat Extended patch must name exactly the parts the
node names, each of type Add (an extra or misspelt key reaches a part the vehicle lacks, which Vehicle Framework's unlock throws on).
Not checked: parts our loadouts append at start-up, and a vehicle armoured by class whose mark is added by a patch rather than its Def.
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
    tree: str | None = None


@dataclass
class Kit:
    """An upgrade node that adds armour to named parts: per part, (type, sharp, blunt) on the game's scale and on Combat Extended's."""
    key: str
    source: str
    prereqs: list
    armour: dict = field(default_factory=dict)
    ce_armour: dict | None = None
    excludes: list = field(default_factory=list)
    ce_source: str = ''


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
            comps = el.find('comps')
            for comp in comps if comps is not None else []:
                if comp.get('Class') == 'Vehicles.CompProperties_UpgradeTree':
                    vehicle.tree = comp.findtext('def')
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


KIT_ARMOUR = re.compile(r'^Defs/Vehicles\.UpgradeTreeDef\[defName="([^"]+)"\]/nodes/li\[key="([^"]+)"\]/upgrades/'
                        r'li\[@Class="Vehicles\.VehicleUpgrade"\]/armor$')


def armour_entries(armor):
    """Per part key: (type, sharp, blunt) from a VehicleUpgrade's armor list."""
    found = {}
    for entry in armor.findall('li') if armor is not None else []:
        mods = entry.find('statModifiers')
        found[entry.findtext('key', '')] = ((entry.findtext('type') or 'Add').strip(),
                                            number(mods.findtext('ArmorRating_Sharp')) if mods is not None else None,
                                            number(mods.findtext('ArmorRating_Blunt')) if mods is not None else None)
    return found


def load_kits(root, trees):
    """Every node of these trees that adds armour, with its Combat Extended figures from the patches."""
    kits = {tree: {} for tree in trees}
    for path in xml_files(root):
        if CE_PATCHES in path.as_posix():
            continue
        doc = parse(path)
        if doc is None:
            continue
        for tree in doc.iter('Vehicles.UpgradeTreeDef'):
            name = tree.findtext('defName')
            if name not in kits:
                continue
            for node in tree.findall('nodes/li'):
                armour = {}
                for upgrade in node.findall('upgrades/li'):
                    if (upgrade.get('Class') or '').endswith('VehicleUpgrade'):
                        armour.update(armour_entries(upgrade.find('armor')))
                if armour:
                    key = node.findtext('key', '')
                    prereqs = [li.text for li in node.findall('prerequisiteNodes/li')]
                    excludes = [li.text for li in node.findall('disableIfUpgradeNodesEnabled/li')]
                    kits[name][key] = Kit(key, path.relative_to(root).as_posix(), prereqs, armour, excludes=excludes)
    stray = []
    for path in sorted((root / 'mods').glob(f'*/{CE_PATCHES}/*.xml')):
        doc = parse(path)
        for op in doc.iter('Operation') if doc is not None else []:
            m = KIT_ARMOUR.match((op.findtext('xpath') or '').strip())
            if not m or m.group(1) not in kits:
                continue
            where = f'{path.relative_to(root).as_posix()}: {m.group(1)} node {m.group(2)}'
            if m.group(2) not in kits[m.group(1)]:
                stray.append(f'{where}: Combat Extended armour for a node that adds no armour in the tree, or does not exist')
                continue
            kit = kits[m.group(1)][m.group(2)]
            kit.ce_armour, kit.ce_source = armour_entries(op.find('value/armor')), where
    return kits, stray


def fittable_sets(kits, part_key):
    """Every set of kits touching a part that can be fitted together: closed under the kits each needs, with no two either/or."""
    touching = [k for k in kits.values() if part_key in k.armour]
    sets = []
    for mask in range(1, 1 << len(touching)):
        needed = {touching[i].key for i in range(len(touching)) if mask >> i & 1}
        todo = list(needed)
        while todo:
            for need in kits[todo.pop()].prereqs:
                if need in kits and need not in needed:
                    needed.add(need)
                    todo.append(need)
        if any(other in needed for key in needed for other in kits[key].excludes):
            continue
        group = frozenset(k for k in needed if part_key in kits[k].armour)
        if group and group not in sets:
            sets.append(group)
    return sets


def check_kits(vehicle, kits, classes):
    """Each kit adds by class: whatever kits can be fitted together, each part they touch ends on one class on both scales."""
    problems = []
    parts = {p.key: p for p in vehicle.parts}
    usable = {}
    for kit in kits.values():
        where = f'{kit.source}: {vehicle.def_name} kit {kit.key}'
        for part_key, (kind, _, _) in kit.armour.items():
            if part_key not in parts:
                problems.append(f'{where}: names part {part_key}, which {vehicle.def_name} does not have')
            elif kind != 'Add':
                problems.append(f'{where}: armour on {part_key} must be of type Add on a vehicle armoured by class')
            elif kit.ce_armour is None or part_key not in kit.ce_armour:
                problems.append(f'{where}: no Combat Extended figures for {part_key}, so it would add {kit.armour[part_key][1]} mm')
            else:
                usable.setdefault(part_key, True)
                continue
            usable[part_key] = False
        for part_key, (kind, _, _) in (kit.ce_armour or {}).items():
            if part_key not in kit.armour:
                problems.append(f'{kit.ce_source}: Combat Extended armour for {part_key}, which the node does not name; on a vehicle '
                                f'without that part Vehicle Framework\'s unlock throws')
            elif kind != 'Add':
                problems.append(f'{kit.ce_source}: Combat Extended armour on {part_key} must be of type Add')
    for part_key, ok in usable.items():
        if not ok:
            continue
        part = parts[part_key]
        for group in fittable_sets(kits, part_key):
            chosen = [kits[k] for k in sorted(group)]
            where = f'{chosen[0].source}: {vehicle.def_name} kits {" + ".join(k.key for k in chosen)}'
            sharp = part.sharp + sum(k.armour[part_key][1] or 0 for k in chosen)
            blunt = part.blunt + sum(k.armour[part_key][2] or 0 for k in chosen)
            game = class_of(classes, sharp, blunt, 'rating')
            if game is None:
                problems.append(f'{where}: bring {part_key} to {sharp:g}/{blunt:g}, which is no class\'s rating')
                continue
            ce_sharp = (part.ce_sharp or 0) + sum(k.ce_armour[part_key][1] or 0 for k in chosen)
            ce_blunt = (part.ce_blunt or 0) + sum(k.ce_armour[part_key][2] or 0 for k in chosen)
            ce = class_of(classes, ce_sharp, ce_blunt, 'ce')
            if ce is None or ce.name != game.name:
                problems.append(f'{where}: bring {part_key} to {game.name} in the Def but {ce_sharp:g} mm / {ce_blunt:g} MPa with Combat '
                                f'Extended; {game.name} is {game.sharp_mm:g} / {game.blunt_mpa:g}')
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
    kits, stray = load_kits(root, {v.tree for v in vehicles.values() if v.tree})
    problems += stray
    for vehicle in vehicles.values():
        problems += check_vehicle(vehicle, classes)
        if vehicle.tree and all(p.sharp is not None and p.blunt is not None for p in vehicle.parts):
            problems += check_kits(vehicle, kits.get(vehicle.tree, {}), classes)
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
