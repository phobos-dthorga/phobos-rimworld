"""Check every vehicle systems schema in the repository without the game: systems, loadouts and the keys and classes they name.

    python scripts/check-vehicle-schemas.py [--root DIR] [--json]

Reads each mods/*/ XML file for Phobos.VehicleSystems.PhobosVehicleSystemDef and PhobosVehicleLoadoutDef, resolves XML inheritance
the way the game does (a child's elements override, list items append unless Inherit="False"), and checks what the game would only
find at load, or never: quantities and flows that name each other, parameters declared before use, stages in order, roles a system
reads declared and bound, loadouts that name real systems and parameters, appended parts with unique keys and a role to take cells
from, the effect, input and condition classes the service ships, and the translation keys messages and refusals use. Drivetrains
(gears, shift points, torque curve, turbine ratings and water injection), flight profiles (a reader for every phase they use) and
the loadouts that name them are checked too (Framework 0.11.0). Another mod's
own classes (outside Phobos.VehicleSystems) are taken on trust. Exit code 1 when anything is wrong; warnings alone exit 0.
The schema itself is documented in docs/vehicle-systems-modding.md.
"""
import argparse
import copy
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

SYSTEM_TAG = 'Phobos.VehicleSystems.PhobosVehicleSystemDef'
LOADOUT_TAG = 'Phobos.VehicleSystems.PhobosVehicleLoadoutDef'
DRIVETRAIN_TAG = 'Phobos.VehicleSystems.PhobosVehicleDrivetrainDef'
PROFILE_TAG = 'Phobos.VehicleSystems.PhobosVehicleFlightProfileDef'
OUR_NAMESPACE = 'Phobos.VehicleSystems.'
SCHEMA_SOURCE = 'src/PhobosFramework.Vehicles/Schema'
EXTRA_CLASS_SOURCES = ('src/PhobosFramework.Vehicles/Defs/PowertrainDefs.cs',)  # the flight profile's phase readers
CLASS_LINE = re.compile(r'^public (?:abstract )?class (\w+)', re.M)
KEY_FIELDS = {'message', 'reason', 'label', 'text'}  # fields of effects that hold translation keys
PARAMETER_FIELDS = ('factorParameter', 'divideByParameter', 'openAboveParameter', 'openBelowParameter', 'scaleByParameter',
                    'supplyParameter')


def text(element, tag, default=None):
    found = element.find(tag)
    return default if found is None or found.text is None else found.text.strip()


def items(element, tag):
    found = element.find(tag)
    return [] if found is None else [li for li in found.findall('li')]


def merge(parent, child):
    """XML inheritance as Verse.XmlInheritance does it, enough for these Defs."""
    result = copy.deepcopy(parent)
    result.attrib.update({k: v for k, v in child.attrib.items() if k not in ('Name', 'Abstract')})
    result.attrib.pop('Abstract', None)
    if child.attrib.get('Abstract') == 'True':
        result.attrib['Abstract'] = 'True'
    result.text = child.text
    for node in child:
        existing = result.find(node.tag)
        if node.attrib.get('Inherit') == 'False' or existing is None or node.tag == 'li':
            if existing is not None and node.tag != 'li':
                result.remove(existing)
            result.append(copy.deepcopy(node))
        elif len(node) and all(grand.tag == 'li' for grand in node):
            for grand in node:
                existing.append(copy.deepcopy(grand))
        elif len(node):
            replaced = merge(existing, node)
            index = list(result).index(existing)
            result.remove(existing)
            result.insert(index, replaced)
        else:
            existing.text = node.text
            existing.attrib.update(node.attrib)
    return result


def gather(root):
    """Every system and loadout Def under mods/, inheritance resolved, with the file each came from."""
    raw = {SYSTEM_TAG: [], LOADOUT_TAG: [], DRIVETRAIN_TAG: [], PROFILE_TAG: []}
    keys = set()
    for path in sorted(Path(root, 'mods').rglob('*.xml')):
        relative = path.relative_to(root).as_posix()
        try:
            tree = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        if '/Languages/' in relative and '/Keyed/' in relative:
            keys.update(child.tag for child in tree)
            continue
        if tree.tag != 'Defs':
            continue
        for node in tree:
            if node.tag in raw:
                raw[node.tag].append((node, relative))
    resolved = {}
    for tag, nodes in raw.items():
        named = {node.attrib['Name']: node for node, _ in nodes if 'Name' in node.attrib}

        def resolve(node, seen=()):
            parent_name = node.attrib.get('ParentName')
            if not parent_name or parent_name not in named or parent_name in seen:
                return node
            return merge(resolve(named[parent_name], seen + (parent_name,)), node)

        resolved[tag] = [(resolve(node), source) for node, source in nodes if node.attrib.get('Abstract') != 'True']
    return resolved, keys


def our_classes(root):
    names = set()
    paths = list(Path(root, SCHEMA_SOURCE).glob('*.cs')) + [Path(root, extra) for extra in EXTRA_CLASS_SOURCES]
    for path in paths:
        if path.exists():
            names.update(CLASS_LINE.findall(path.read_text(encoding='utf-8')))
    return names


def number(element, tag, default):
    value = text(element, tag)
    try:
        return float(value) if value is not None else default
    except ValueError:
        return None


def check_drivetrain(node, where, errors):
    # What the Def's own ConfigErrors says, before the game does, plus the turbine's ladder and water.
    engine = node.find('engine') if node.find('engine') is not None else ET.Element('engine')
    transmission = node.find('transmission') if node.find('transmission') is not None else ET.Element('transmission')
    kind = text(engine, 'kind', 'Piston')
    if text(transmission, 'kind', 'Automatic') == 'Automatic':
        gears = [float(li.text) for li in items(transmission, 'gears')]
        if not gears:
            errors.append(f'{where}: an automatic transmission has no gears')
        if any(b >= a for a, b in zip(gears, gears[1:])):
            errors.append(f'{where}: gears must fall from first to top')
        if number(transmission, 'upshiftRpm', 2400) <= number(transmission, 'downshiftRpm', 1300):
            errors.append(f'{where}: the upshift rpm must be above the downshift rpm')
    if kind == 'Piston' and not (number(engine, 'idleRpm', 700) <= number(engine, 'launchRpm', 1100) <= number(engine, 'ratedRpm', 3000)):
        errors.append(f'{where}: the launch rpm must lie between idle and rated')
    curve = items(engine.find('torqueCurve'), 'points') if engine.find('torqueCurve') is not None else []
    if curve and float(curve[-1].text.strip().strip('()').split(',')[1]) != 0:
        errors.append(f'{where}: the torque curve must end at zero')
    if text(transmission, 'kind', 'Automatic') == 'DieselElectric':
        most = number(transmission, 'maxTractiveEffortKilonewtons', 0)
        held = number(transmission, 'continuousTractiveEffortKilonewtons', 0)
        if most is None or held is None or most < 0 or held < 0 or (most and held > most):
            errors.append(f'{where}: the continuous tractive effort must be a positive figure no greater than the maximum')
    turbine = engine.find('turbine')
    if kind in ('Turboshaft', 'Turbofan', 'Thruster') and turbine is None:
        errors.append(f'{where}: a {kind} engine has no turbine block')
    if turbine is None:
        return
    limit = number(turbine, 'transmissionLimit', float('nan'))
    if limit is None or limit <= 0:
        errors.append(f'{where}: transmissionLimit must be a positive share of the engines\' output')
    ratings = items(turbine, 'ratings')
    if not ratings:
        errors.append(f'{where}: a turbine has no ratings')
        return
    for below, above in zip(ratings, ratings[1:]):
        if number(above, 'share', 1) <= number(below, 'share', 1) or number(above, 'temperatureLimit', 775) < number(below, 'temperatureLimit', 775):
            errors.append(f'{where}: turbine ratings must rise in share and temperature ({text(below, "name")} to {text(above, "name")})')
        if number(above, 'minutes', 0) <= 0:
            errors.append(f'{where}: the rating {text(above, "name")} above maximum continuous needs a time limit in minutes')
    wet = [r for r in ratings if text(r, 'wet') == 'true']
    if len(wet) > 1:
        errors.append(f'{where}: more than one wet rating')
    if bool(wet) != (turbine.find('waterInjection') is not None):
        errors.append(f'{where}: a wet rating and a water injection block go together')
    thrust = [number(r, 'thrustKilonewtons', 0) > 0 for r in ratings]
    if any(thrust) and not all(thrust):
        errors.append(f'{where}: give every rating a thrust, or none')


def walk_classes(element):
    for node in element.iter():
        if 'Class' in node.attrib:
            yield node


def check(root):
    resolved, keys = gather(root)
    classes = our_classes(root)
    errors, warnings = [], []
    systems = {}
    for node, source in resolved[SYSTEM_TAG]:
        name = text(node, 'defName')
        where = f'{source}: {name}'
        if not name:
            errors.append(f'{source}: a system has no defName')
            continue
        if name in systems:
            errors.append(f'{where}: defined twice')
        quantities = {}
        for quantity in items(node, 'quantities'):
            qname = text(quantity, 'name')
            if not qname:
                errors.append(f'{where}: a quantity has no name')
                continue
            if qname in quantities:
                errors.append(f'{where}: two quantities are called {qname}')
            quantities[qname] = quantity.find('derivedFrom') is not None
            previous_top = float('-inf')
            for stage in items(quantity, 'stages'):
                bottom = float(text(stage, 'from', '-inf'))
                top = float(text(stage, 'to', 'inf'))
                if top <= bottom or bottom < previous_top:
                    errors.append(f'{where}: {qname}\'s stages overlap or are out of order')
                previous_top = top
                if not text(stage, 'label'):
                    errors.append(f'{where}: a stage of {qname} has no label')
        parameters = {child.tag for child in (node.find('parameters') if node.find('parameters') is not None else [])}
        roles = {(li.text or '').strip() for li in items(node, 'roles')}
        systems[name] = {'quantities': quantities, 'parameters': parameters, 'roles': roles, 'node': node, 'where': where}
    all_quantities = {q for system in systems.values() for q in system['quantities']}

    for name, system in systems.items():
        node, where = system['node'], system['where']
        for flow in items(node, 'flows'):
            quantity = text(flow, 'quantity')
            if quantity not in system['quantities']:
                errors.append(f'{where}: flow {text(flow, "name")} names no quantity of this system ({quantity})')
            elif system['quantities'][quantity]:
                errors.append(f'{where}: flow {text(flow, "name")} moves the derived quantity {quantity}')
            if text(flow, 'kind') == 'Toward' and flow.find('target') is None:
                errors.append(f'{where}: flow {text(flow, "name")} moves toward nothing')
        for element in node.iter():
            if element.tag in PARAMETER_FIELDS and element.text and element.text.strip() not in system['parameters'] \
                    and element.tag != 'supplyParameter':
                errors.append(f'{where}: {element.tag} names the undeclared parameter {element.text.strip()}')
        for element in walk_classes(node):
            cls = element.attrib['Class']
            if cls.startswith(OUR_NAMESPACE) and cls[len(OUR_NAMESPACE):] not in classes:
                errors.append(f'{where}: no class {cls}')
            if cls.endswith('Input_Parameter') and text(element, 'parameter') not in system['parameters']:
                errors.append(f'{where}: Input_Parameter names the undeclared parameter {text(element, "parameter")}')
            quantity = text(element, 'quantity')
            if quantity is not None and quantity not in all_quantities:
                errors.append(f'{where}: {cls.split(".")[-1]} names a quantity no system has ({quantity})')
            role = text(element, 'role')
            if role is not None and role not in system['roles']:
                warnings.append(f'{where}: {cls.split(".")[-1]} reads the role {role}, which <roles> does not list')
            for child in element:
                if child.tag in KEY_FIELDS and cls.split('.')[-1].startswith('Effect_') and child.text and child.text.strip() not in keys:
                    errors.append(f'{where}: the translation key {child.text.strip()} is missing')
        for rule in items(node, 'onPartHit'):
            if text(rule, 'quantity') not in all_quantities:
                errors.append(f'{where}: a part-hit rule names a quantity no system has ({text(rule, "quantity")})')
            if text(rule, 'role') not in system['roles']:
                warnings.append(f'{where}: a part-hit rule reads the role {text(rule, "role")}, which <roles> does not list')
        service = node.find('service')
        if service is not None:
            if text(service, 'dueQuantity') not in system['quantities']:
                errors.append(f'{where}: the service is due on an unknown quantity')
            if text(service, 'dueAbove') is None and text(service, 'dueBelow') is None:
                errors.append(f'{where}: the service never falls due')
            water = number(service, 'waterLitres', 0)
            if water is None or water < 0:
                errors.append(f"{where}: the service's waterLitres must be a number of litres, 0 or more")
            for restore in items(service, 'restores'):
                if text(restore, 'quantity') not in all_quantities:
                    errors.append(f'{where}: the service restores a quantity no system has ({text(restore, "quantity")})')
        jump = node.find('jumpStart')
        if jump is not None:
            if text(jump, 'quantity') not in system['quantities']:
                errors.append(f'{where}: the jump-start names an unknown quantity ({text(jump, "quantity")})')
            if text(jump, 'capacityParameter') not in system['parameters']:
                errors.append(f'{where}: the jump-start names the undeclared parameter {text(jump, "capacityParameter")}')

    drivetrains = {}
    for node, source in resolved[DRIVETRAIN_TAG]:
        name = text(node, 'defName')
        where = f'{source}: {name}'
        if name in drivetrains:
            errors.append(f'{where}: defined twice')
        drivetrains[name] = node
        check_drivetrain(node, where, errors)
    profiles = {}
    for node, source in resolved[PROFILE_TAG]:
        name = text(node, 'defName')
        where = f'{source}: {name}'
        profiles[name] = node
        phases = [text(li, 'phase') for li in items(node, 'phases')]
        if len(phases) != len(set(phases)) or None in phases:
            errors.append(f'{where}: a phase has no name or a repeated one')
        reader = node.find('reader')
        if phases and reader is None:
            errors.append(f'{where}: phases but no reader to tell them apart')
        if reader is not None:
            cls = reader.attrib.get('Class', '')
            if cls.startswith(OUR_NAMESPACE) and cls[len(OUR_NAMESPACE):] not in classes:
                errors.append(f'{where}: no class {cls}')
            for rule in items(reader, 'rules'):
                if text(rule, 'phase') not in phases:
                    errors.append(f'{where}: the reader names the phase {text(rule, "phase")}, which <phases> does not list')
                if not text(rule, 'member') or (text(rule, 'equals') is None and text(rule, 'atLeast') is None):
                    errors.append(f'{where}: a reader rule needs a member and an equals or atLeast')
    water_systems = {name for name, system in systems.items() if 'injectionWater' in system['quantities']}
    loadouts = {}
    for node, source in resolved[LOADOUT_TAG]:
        name = text(node, 'defName')
        where = f'{source}: {name}'
        if not name:
            errors.append(f'{source}: a loadout has no defName')
            continue
        loadouts[name] = node
        used = []
        for entry in items(node, 'systems'):
            system_name = text(entry, 'def')
            if system_name not in systems:
                errors.append(f'{where}: no system {system_name}')
                continue
            used.append(system_name)
            overrides = entry.find('set')
            for child in (overrides if overrides is not None else []):
                if child.tag not in systems[system_name]['parameters']:
                    errors.append(f'{where}: {system_name} has no parameter {child.tag}')
        bound, added = set(), set()
        for role in items(node, 'partRoles'):
            role_name = text(role, 'role')
            if role_name in bound:
                errors.append(f'{where}: the role {role_name} is bound twice')
            bound.add(role_name)
            add = role.find('add')
            if add is not None:
                key = text(add, 'key')
                if not key or key in added:
                    errors.append(f'{where}: the part added for {role_name} has no key or a repeated one')
                added.add(key)
                cells_of = text(add, 'cellsOf')
                if cells_of is not None and cells_of not in {text(r, 'role') for r in items(node, 'partRoles')}:
                    errors.append(f'{where}: the part added for {role_name} takes cells from the unbound role {cells_of}')
                if int(text(add, 'health', '20')) <= 0:
                    errors.append(f'{where}: the part added for {role_name} has no health')
        for system_name in used:
            for role in sorted(systems[system_name]['roles'] - bound):
                warnings.append(f'{where}: {system_name} reads the role {role}, which this loadout binds to nothing (it reads as sound)')
        drivetrain = node.find('drivetrain')
        train = None
        if drivetrain is not None:
            train = drivetrains.get(text(drivetrain, 'def'))
            if train is None:
                errors.append(f'{where}: no drivetrain {text(drivetrain, "def")}')
        profile = text(node, 'flightProfile')
        if profile is not None and profile not in profiles:
            errors.append(f'{where}: no flight profile {profile}')
        engine = None if train is None else train.find('engine')
        turbine = None if engine is None else engine.find('turbine')
        if profile is not None and turbine is None:
            warnings.append(f'{where}: a flight profile with no turbine engine: its phases drive nothing but the inspect line')
        kind = text(engine, 'kind', 'Piston') if engine is not None else None
        if turbine is not None and kind != 'Thruster' and 'PhobosVehicleSystem_Turbine' not in used:
            warnings.append(f'{where}: a turbine engine without the turbine system: no temperature, hot-section wear or hung starts')
        if turbine is not None and turbine.find('waterInjection') is not None and not water_systems.intersection(used):
            errors.append(f'{where}: the engine injects water but no system here holds the injectionWater quantity')
        if kind == 'Thruster' and turbine is not None:
            overrides = drivetrain.find('set') if drivetrain is not None else None
            lift = text(overrides, 'liftToWeightAtMaxGross') if overrides is not None else None
            if lift is None and text(turbine, 'liftToWeightAtMaxGross') is None and not any(
                    number(r, 'thrustKilonewtons', 0) for r in items(turbine, 'ratings')):
                errors.append(f'{where}: thrusters need a liftToWeightAtMaxGross or ratings with thrust')
        if text(node, 'rank') is not None and number(node, 'rank', 0) is None:
            errors.append(f'{where}: rank must be a whole number')
        names = [text(load, 'name') for load in items(node, 'loads')]
        if len(names) != len(set(names)) or None in names:
            errors.append(f'{where}: a load has no name or a repeated one')
        for element in walk_classes(node):
            cls = element.attrib['Class']
            if cls.startswith(OUR_NAMESPACE) and cls[len(OUR_NAMESPACE):] not in classes:
                errors.append(f'{where}: no class {cls}')
    return {'systems': sorted(systems), 'loadouts': sorted(loadouts), 'drivetrains': sorted(drivetrains), 'profiles': sorted(profiles),
            'errors': errors, 'warnings': warnings}


def main():
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('--root', default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument('--json', action='store_true')
    args = parser.parse_args()
    result = check(args.root)
    if args.json:
        print(json.dumps(result, indent=2))
    else:
        print(f'{len(result["systems"])} systems, {len(result["loadouts"])} loadouts, {len(result["drivetrains"])} drivetrains, '
              f'{len(result["profiles"])} flight profiles; '
              f'{len(result["errors"])} errors, {len(result["warnings"])} warnings.')
        for line in result['errors']:
            print('ERROR   ' + line)
        for line in result['warnings']:
            print('warning ' + line)
    return 1 if result['errors'] else 0


if __name__ == '__main__':
    sys.exit(main())
