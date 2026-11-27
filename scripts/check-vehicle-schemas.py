"""Check every vehicle systems schema in the repository without the game: systems, loadouts and the keys and classes they name.

    python scripts/check-vehicle-schemas.py [--root DIR] [--json]

Reads each mods/*/ XML file for Phobos.VehicleSystems.PhobosVehicleSystemDef and PhobosVehicleLoadoutDef, resolves XML inheritance
the way the game does (a child's elements override, list items append unless Inherit="False"), and checks what the game would only
find at load, or never: quantities and flows that name each other, parameters declared before use, stages in order, roles a system
reads declared and bound, loadouts that name real systems and parameters, appended parts with unique keys and a role to take cells
from, the effect, input and condition classes the service ships, and the translation keys messages and refusals use. Drivetrains
(gears, shift points, torque curve, turbine ratings and water injection), flight profiles (a reader for every phase they use) and
the loadouts that name them are checked too (Framework 0.11.0), and part conditions (Framework 0.29.0): stages in order within a
severity of 0 to 1, their effects as a system's, and every rule, cause and service that names one. Another mod's
own classes (outside Phobos.VehicleSystems) are taken on trust. Exit code 1 when anything is wrong; warnings alone exit 0.
The schema itself is documented in docs/vehicle-systems-modding.md.

Another author's mod, from a copy of this one file (Python 3.9 or newer, nothing else to install):

    python check-vehicle-schemas.py --mod FOLDER [--mod FOLDER ...] [--ours FOLDER ...] [--json]

checks that mod's Defs and patches (every game version and load folder it has) against our Defs, as read from --ours: a mod
folder of ours, or a folder holding them (the game's Mods folder, or Steam's workshop/content/294100); by default this
repository's mods/, else Steam's usual places. Its loadouts may inherit ours, and its patches are applied to ours first. On top
of the checks above, every element inside one of our classes, anywhere in its Defs, is checked against the class's fields as our
assemblies declare them (names, numbers, true or false, enum values). Each finding names the file, the line, the Def and what to
do. The run over mods/ is skipped then, and its findings about our own files are left out unless the mod changes them.
"""
import argparse
import copy
import difflib
import json
import re
import sys
from collections import Counter
import xml.etree.ElementTree as ET
import xml.parsers.expat as expat
from pathlib import Path

SYSTEM_TAG = 'Phobos.VehicleSystems.PhobosVehicleSystemDef'
LOADOUT_TAG = 'Phobos.VehicleSystems.PhobosVehicleLoadoutDef'
DRIVETRAIN_TAG = 'Phobos.VehicleSystems.PhobosVehicleDrivetrainDef'
PROFILE_TAG = 'Phobos.VehicleSystems.PhobosVehicleFlightProfileDef'
CONDITION_TAG = 'Phobos.VehicleSystems.PhobosVehicleConditionDef'
OUR_CONDITION_PREFIX = 'PhobosVehicleCondition_'  # a condition of another mod's is taken on trust
OUR_NAMESPACE = 'Phobos.VehicleSystems.'
SCHEMA_SOURCE = 'src/PhobosFramework.Vehicles/Schema'
EXTRA_CLASS_SOURCES = ('src/PhobosFramework.Vehicles/Defs/PowertrainDefs.cs',)  # the flight profile's phase readers
CLASS_LINE = re.compile(r'^public (?:abstract )?class (\w+)', re.M)
KEY_FIELDS = {'message', 'reason', 'label', 'text', 'hotAirMessage', 'hotAirText'}  # fields of effects that hold translation keys
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
    raw = {SYSTEM_TAG: [], LOADOUT_TAG: [], DRIVETRAIN_TAG: [], PROFILE_TAG: [], CONDITION_TAG: []}
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


def figure(element, tag, default, where, errors):
    """A number from the Def, or the default and an error when its text is no number (rather than stopping the whole check)."""
    value = number(element, tag, default)
    if value is None:
        errors.append(f'{where}: <{tag}> is not a number ({text(element, tag)})')
        return default
    return value


def check_drivetrain(node, where, errors):
    # What the Def's own ConfigErrors says, before the game does, plus the turbine's ladder and water.
    engine = node.find('engine') if node.find('engine') is not None else ET.Element('engine')
    transmission = node.find('transmission') if node.find('transmission') is not None else ET.Element('transmission')
    kind = text(engine, 'kind', 'Piston')
    if text(transmission, 'kind', 'Automatic') == 'Automatic':
        gears = []
        for li in items(transmission, 'gears'):
            try:
                gears.append(float(li.text))
            except (TypeError, ValueError):
                errors.append(f'{where}: a gear ratio is not a number ({li.text})')
        if not gears:
            errors.append(f'{where}: an automatic transmission has no gears')
        if any(b >= a for a, b in zip(gears, gears[1:])):
            errors.append(f'{where}: gears must fall from first to top')
        if figure(transmission, 'upshiftRpm', 2400, where, errors) <= figure(transmission, 'downshiftRpm', 1300, where, errors):
            errors.append(f'{where}: the upshift rpm must be above the downshift rpm')
    if kind == 'Piston' and not (figure(engine, 'idleRpm', 700, where, errors) <= figure(engine, 'launchRpm', 1100, where, errors)
                                 <= figure(engine, 'ratedRpm', 3000, where, errors)):
        errors.append(f'{where}: the launch rpm must lie between idle and rated')
    curve = items(engine.find('torqueCurve'), 'points') if engine.find('torqueCurve') is not None else []
    if curve:
        try:
            if float(curve[-1].text.strip().strip('()').split(',')[1]) != 0:
                errors.append(f'{where}: the torque curve must end at zero')
        except (AttributeError, IndexError, ValueError):
            errors.append(f'{where}: a torque curve point is not written (rpm, torque) ({curve[-1].text})')
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
        if figure(above, 'share', 1, where, errors) <= figure(below, 'share', 1, where, errors)                 or figure(above, 'temperatureLimit', 775, where, errors) < figure(below, 'temperatureLimit', 775, where, errors):
            errors.append(f'{where}: turbine ratings must rise in share and temperature ({text(below, "name")} to {text(above, "name")})')
        if figure(above, 'minutes', 0, where, errors) <= 0:
            errors.append(f'{where}: the rating {text(above, "name")} above maximum continuous needs a time limit in minutes')
    wet = [r for r in ratings if text(r, 'wet') == 'true']
    if len(wet) > 1:
        errors.append(f'{where}: more than one wet rating')
    if bool(wet) != (turbine.find('waterInjection') is not None):
        errors.append(f'{where}: a wet rating and a water injection block go together')
    thrust = [figure(r, 'thrustKilonewtons', 0, where, errors) > 0 for r in ratings]
    if any(thrust) and not all(thrust):
        errors.append(f'{where}: give every rating a thrust, or none')


def walk_classes(element):
    for node in element.iter():
        if 'Class' in node.attrib:
            yield node


def check_effects(element, where, classes, keys, all_quantities, roles, errors, warnings):
    """The classes, quantities, roles and translation keys named inside a system's or a condition's stages."""
    for node in walk_classes(element):
        cls = node.attrib['Class']
        if classes is not None and cls.startswith(OUR_NAMESPACE) and cls[len(OUR_NAMESPACE):] not in classes:
            errors.append(f'{where}: no class {cls}')
        quantity = text(node, 'quantity')
        if quantity is not None and quantity not in all_quantities:
            errors.append(f'{where}: {cls.split(".")[-1]} names a quantity no system has ({quantity})')
        role = text(node, 'role')
        if roles is not None and role is not None and role not in roles:
            warnings.append(f'{where}: {cls.split(".")[-1]} reads the role {role}, which <roles> does not list')
        for child in node:
            if child.tag in KEY_FIELDS and cls.split('.')[-1].startswith('Effect_') and child.text and child.text.strip() not in keys:
                errors.append(f'{where}: the translation key {child.text.strip()} is missing')


def check_condition_name(name, conditions, where, what, errors):
    if name is None:
        errors.append(f'{where}: {what} names no condition')
    elif name.startswith(OUR_CONDITION_PREFIX) and name not in conditions:
        errors.append(f'{where}: {what} names a condition that does not exist ({name})')


def check(root):
    resolved, keys = gather(root)
    return check_defs(resolved, keys, our_classes(root))


def check_defs(resolved, keys, classes):
    """The checks themselves, over Defs already gathered and resolved: classes is the set of our class names (after
    Phobos.VehicleSystems.), or None when they are unknown and every class is taken on trust."""
    errors, warnings = [], []
    systems = {}
    conditions = {}
    for node, source in resolved[CONDITION_TAG]:
        name = text(node, 'defName')
        where = f'{source}: {name}'
        if not name:
            errors.append(f'{source}: a condition has no defName')
            continue
        if name in conditions:
            errors.append(f'{where}: defined twice')
        conditions[name] = (node, where)
        stages = items(node, 'stages')
        if not stages:
            errors.append(f'{where}: a condition needs at least one stage')
        previous_top = float('-inf')
        for stage in stages:
            bottom = figure(stage, 'from', 0.0, where, errors)
            top = figure(stage, 'to', 1.0, where, errors)
            if top <= bottom or bottom < previous_top:
                errors.append(f'{where}: stages overlap or are out of order')
            if not (0 <= bottom <= 1 and 0 <= top <= 1):
                errors.append(f'{where}: the stage {text(stage, "label")} lies outside a severity of 0 to 1')
            previous_top = top
            if not text(stage, 'label'):
                errors.append(f'{where}: a stage has no label')
        fade = number(node, 'fadePerHour', 0)
        if fade is None or fade < 0:
            errors.append(f'{where}: fadePerHour must be 0 or more')
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
                bottom = figure(stage, 'from', float('-inf'), where, errors)
                top = figure(stage, 'to', float('inf'), where, errors)
                if top <= bottom or bottom < previous_top:
                    errors.append(f'{where}: {qname}\'s stages overlap or are out of order')
                previous_top = top
                if not text(stage, 'label'):
                    errors.append(f'{where}: a stage of {qname} has no label')
            if text(quantity, 'showOnDashboard', 'false') not in ('true', 'false'):
                errors.append(f'{where}: {qname} showOnDashboard must be true or false')
            if text(quantity, 'showOnDashboard') == 'true' and text(quantity, 'hidden') == 'true':
                errors.append(f'{where}: {qname} is hidden and on the dashboard at once')
            if quantity.find('remainingLabel') is not None and not text(quantity, 'remainingLabel'):
                errors.append(f'{where}: {qname} has an empty remainingLabel')
        parameters = {child.tag for child in (node.find('parameters') if node.find('parameters') is not None else [])}
        roles = {(li.text or '').strip() for li in items(node, 'roles')}
        # Framework 0.368.0 (the vehicle standard's VS11): roles a vehicle may rightly lack (a turbine's radiator and belt), and the role a
        # system adds a part for beside each engine (partPerEngine, Framework 0.360.0), are never "bound to nothing" on a loadout.
        optional = {(li.text or '').strip() for li in items(node, 'optionalRoles')}
        per_engine = node.find('partPerEngine')
        if per_engine is not None and text(per_engine, 'role'):
            optional.add(text(per_engine, 'role'))
        unknown_optional = optional - roles
        if unknown_optional:
            errors.append(f'{where}: optionalRoles names {", ".join(sorted(unknown_optional))}, which <roles> does not list')
        systems[name] = {'quantities': quantities, 'parameters': parameters, 'roles': roles, 'optional': optional, 'node': node, 'where': where}
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
        check_effects(node, where, classes, keys, all_quantities, system['roles'], errors, warnings)
        for element in walk_classes(node):
            cls = element.attrib['Class']
            if cls.endswith(('Input_Parameter', 'When_ParameterBelow')) and text(element, 'parameter') not in system['parameters']:
                errors.append(f'{where}: Input_Parameter names the undeclared parameter {text(element, "parameter")}')
            if cls.endswith('Effect_AddCondition'):
                check_condition_name(text(element, 'condition'), conditions, where, 'Effect_AddCondition', errors)
                if not (number(element, 'severity', 0) or 0) > 0 and not (number(element, 'perHour', 0) or 0) > 0:
                    errors.append(f'{where}: Effect_AddCondition adds no severity')
        for rule in items(node, 'onPartHit'):
            quantity, condition = text(rule, 'quantity'), text(rule, 'condition')
            if quantity is None and condition is None:
                errors.append(f'{where}: a part-hit rule on {text(rule, "role")} moves no quantity and gives no condition')
            if quantity is not None and quantity not in all_quantities:
                errors.append(f'{where}: a part-hit rule names a quantity no system has ({quantity})')
            if condition is not None:
                check_condition_name(condition, conditions, where, 'a part-hit rule', errors)
                if not ((number(rule, 'perHealthLost', 0) or 0) > 0 or (number(rule, 'perShareLost', 0) or 0) > 0):
                    errors.append(f'{where}: the part-hit rule giving {condition} adds no severity')
            if text(rule, 'role') not in system['roles']:
                warnings.append(f'{where}: a part-hit rule reads the role {text(rule, "role")}, which <roles> does not list')
        service = node.find('service')
        if service is not None:
            if text(service, 'dueQuantity') not in system['quantities']:
                errors.append(f'{where}: the service is due on an unknown quantity')
            if text(service, 'dueAbove') is None and text(service, 'dueBelow') is None:
                errors.append(f'{where}: the service never falls due')
            for field in ('waterLitresParameter', 'scaledWaterLitresParameter'):
                water_parameter = text(service, field)
                if water_parameter is not None and water_parameter not in system['parameters']:
                    errors.append(f'{where}: the service draws water from the undeclared parameter {water_parameter}')
            water = number(service, 'waterLitres', 0)
            if water is None or water < 0:
                errors.append(f"{where}: the service's waterLitres must be a number of litres, 0 or more")
            for restore in items(service, 'restores'):
                if text(restore, 'quantity') not in all_quantities:
                    errors.append(f'{where}: the service restores a quantity no system has ({text(restore, "quantity")})')
            for cleared in items(service, 'clearsConditions'):
                check_condition_name((cleared.text or '').strip() or None, conditions, where, 'the service', errors)
        jump = node.find('jumpStart')
        if jump is not None:
            if text(jump, 'quantity') not in system['quantities']:
                errors.append(f'{where}: the jump-start names an unknown quantity ({text(jump, "quantity")})')
            if text(jump, 'capacityParameter') not in system['parameters']:
                errors.append(f'{where}: the jump-start names the undeclared parameter {text(jump, "capacityParameter")}')

    for name, (node, where) in conditions.items():
        check_effects(node, where, classes, keys, all_quantities, None, errors, warnings)

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
            if classes is not None and cls.startswith(OUR_NAMESPACE) and cls[len(OUR_NAMESPACE):] not in classes:
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
                if figure(add, 'health', 20, where, errors) <= 0:
                    errors.append(f'{where}: the part added for {role_name} has no health')
                ce_weight = number(add, 'hitWeightCE', 1)
                if ce_weight is None or ce_weight <= 0:
                    errors.append(f'{where}: the part added for {role_name} needs a positive hitWeightCE')
        for system_name in used:
            for role in sorted(systems[system_name]['roles'] - bound - systems[system_name]['optional']):
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
        cabin = node.find('cabin')
        if cabin is not None and text(cabin, 'closed', 'true') not in ('true', 'false'):
            errors.append(f'{where}: cabin closed must be true or false')
        for element in walk_classes(node):
            if element.attrib['Class'].endswith('When_LoadActive') and text(element, 'load') not in names:
                errors.append(f'{where}: When_LoadActive names a load this loadout does not have ({text(element, "load")})')
        for element in walk_classes(node):
            cls = element.attrib['Class']
            if classes is not None and cls.startswith(OUR_NAMESPACE) and cls[len(OUR_NAMESPACE):] not in classes:
                errors.append(f'{where}: no class {cls}')
    return {'systems': sorted(systems), 'loadouts': sorted(loadouts), 'drivetrains': sorted(drivetrains), 'profiles': sorted(profiles),
            'conditions': sorted(conditions), 'errors': errors, 'warnings': warnings}


# --- Another author's mod (--mod), checked against D'thorga's own Defs and assemblies (--ours), without the game -------------
#
# A modder runs this one file with Python 3.9 or newer and nothing else installed. D'thorga's Defs come from our mods' folders
# (this repository's mods/, or an installed copy: the game's Mods folder or Steam's Workshop folder), each read through its
# LoadFolders.xml as the game does. Our classes and their fields come from the assemblies those folders ship, read straight from
# their .NET metadata, so no source and no .NET are needed. The modder's mod is read once per game version it has a folder for.

LINE = '{urn:dthorga-check}line'  # the line an element starts on, kept through inheritance and patches
FROM = '{urn:dthorga-check}from'  # the patch file an element was copied in from
OUR_PACKAGE_PREFIX = 'phobosdthorga.'
SCHEMA_TAGS = (SYSTEM_TAG, LOADOUT_TAG, DRIVETRAIN_TAG, PROFILE_TAG, CONDITION_TAG)
KINDS = {SYSTEM_TAG: 'system', LOADOUT_TAG: 'loadout', DRIVETRAIN_TAG: 'drivetrain', PROFILE_TAG: 'flight profile',
         CONDITION_TAG: 'condition'}
# Where Steam puts the game and its Workshop items by default (a mod in the game's own Mods folder first, as one copied there
# by hand is usually the newer); --ours names any other place.
DEFAULT_OUR_PLACES = ('C:/Program Files (x86)/Steam/steamapps/common/RimWorld/Mods',
                      'C:/Program Files (x86)/Steam/steamapps/workshop/content/294100')
# Verse.Def's fields (RimWorld 1.7), the base of every Def; its own assembly is the game's, which this check never reads.
STRING, BOOL = ('prim', 'string'), ('prim', 'bool')
DEF_FIELDS = {'defName': STRING, 'label': STRING, 'description': STRING, 'descriptionHyperlinks': ('other', None),
              'ignoreConfigErrors': BOOL, 'ignoreIllegalLabelCharacterConfigError': BOOL,
              'modExtensions': ('list', ('type', 'Verse.DefModExtension')), 'shortHash': ('prim', 'int'), 'index': ('prim', 'int'),
              'modContentPack': ('other', None), 'fileName': STRING, 'defNameHash': ('prim', 'int'),
              'cachedLabelCap': ('other', None), 'generated': BOOL, 'debugRandomId': ('prim', 'int')}
KNOWN_BASES = {'System.Object': {}, 'System.ValueType': {}, 'Verse.Def': DEF_FIELDS, 'Verse.Editable': {}}

# What to do about each finding, matched by the words it contains; the first match wins.
FIXES = (
    ('not well-formed XML', 'Fix the XML at that line: a tag left open or misspelt at one end, or a bare < or & (write &lt; or &amp;).'),
    ('is not a field of', 'Check the spelling against the reference site\'s XML types page; a field the class does not have is ignored with an error at load.'),
    ('needs a Class', 'Name the class to make, in full: Class="Phobos.VehicleSystems.<the class>" (the guide\'s tables list them).'),
    ('no class ', 'Check the class name against the guide\'s tables. A class of your own goes in your own namespace, never Phobos.VehicleSystems.'),
    ('no Def type', 'Check the Def type against the guide: it is named in full, e.g. Phobos.VehicleSystems.PhobosVehicleLoadoutDef.'),
    ('is not a number', 'Write a plain number with a dot for decimals (1500 or 0.25), with no units or commas.'),
    ('is not a whole number', 'Write a whole number with no decimals, units or commas.'),
    ('is not true or false', 'Write true or false.'),
    ('is not one of', 'Use one of the values listed, spelt exactly.'),
    ('ParentName', 'Check the spelling of ParentName against the Name="..." of the Def you mean to inherit; D\'thorga\'s abstract Defs are listed in the guide.'),
    ('replaces D\'thorga', 'If you meant to change our Def, a patch changes only what you name and keeps up with our updates; if not, give yours a defName with your own prefix.'),
    ('changes nothing', 'Check the xpath\'s Def type and defName against the Def you mean; a patch that finds nothing is an error at load. Add <success>Always</success> only if it is meant to be optional.'),
    ('not followed', 'Nothing to fix: this check cannot follow that patch, so it did not check what the patch adds. The game\'s log will show any error.'),
    ('defined twice', 'Give each Def its own defName: within one mod the game keeps the first and skips the rest.'),
    ('no defName', 'Give the Def a <defName>, beginning with your mod\'s own prefix.'),
    ('bound twice', 'Bind each role once in <partRoles>. If you inherit a loadout that binds it already, write <partRoles Inherit="False"> and list them all, or leave yours out.'),
    ('binds to nothing', 'Bind the role in <partRoles> to one of the vehicle\'s parts, or add a part for it; if the vehicle really has no such part, leave it: the system treats it as sound.'),
    ('which <roles> does not list', 'Add the role to the system\'s <roles>, or check its spelling.'),
    ('injectionWater', 'Add a system holding an injectionWater quantity (D\'thorga\'s turbine water system does), or drop the water injection.'),
    ('quantity', 'Use the name of a quantity a system declares in its <quantities>; check the spelling.'),
    ('no system ', 'Use the defName of a system Def, yours or D\'thorga\'s; check the spelling.'),
    ('has no parameter', 'A loadout\'s <set> can change only the parameters the system declares in its <parameters>; check the spelling.'),
    ('undeclared parameter', 'Declare the parameter in the system\'s <parameters> with a default, or check its spelling.'),
    ('no drivetrain', 'Use the defName of a drivetrain Def, yours or D\'thorga\'s.'),
    ('no flight profile', 'Use the defName of a flight profile Def, yours or D\'thorga\'s.'),
    ('translation key', 'Add the key to your mod\'s Languages/English/Keyed files, or use one D\'thorga ships.'),
    ('condition', 'Use the defName of a condition Def, and give the rule a severity above 0.'),
    ('out of order', 'List stages from lowest to highest, each <from> no lower than the <to> before it.'),
    ('has no label', 'Give every stage a <label>.'),
    ('key or a repeated one', 'Give each appended part its own <key>.'),
    ('cells from the unbound role', 'Name a role this loadout binds in <cellsOf>.'),
    ('without the turbine system', 'Add PhobosVehicleSystem_Turbine to the loadout\'s <systems>.'),
    ('', 'Change the Def as the message says.'),
)


def fix_for(problem):
    return next(advice for words, advice in FIXES if words in problem)


def parse_lines(path):
    """An XML file as ElementTree reads it, each element also holding the line it starts on (under LINE)."""
    builder = ET.TreeBuilder()
    parser = expat.ParserCreate()

    def start(tag, attributes):
        attributes = dict(attributes)
        attributes[LINE] = str(parser.CurrentLineNumber)
        builder.start(tag, attributes)

    parser.StartElementHandler = start
    parser.EndElementHandler = builder.end
    parser.CharacterDataHandler = builder.data
    with open(path, 'rb') as stream:
        parser.ParseFile(stream)
    return builder.close()


def line_of(element):
    value = element.attrib.get(LINE)
    return int(value) if value else None


# --- .NET metadata: our assemblies' types, fields and enums (ECMA-335 partition II, sections 22 to 24) ---------------------

PRIMITIVES = {0x02: 'bool', 0x03: 'char', 0x04: 'int', 0x05: 'int', 0x06: 'int', 0x07: 'int', 0x08: 'int', 0x09: 'int', 0x0A: 'int',
              0x0B: 'int', 0x0C: 'float', 0x0D: 'float', 0x0E: 'string', 0x18: 'int', 0x19: 'int', 0x1C: 'object'}


def _compressed(data, at):
    first = data[at]
    if first & 0x80 == 0:
        return first, at + 1
    if first & 0xC0 == 0x80:
        return ((first & 0x3F) << 8) | data[at + 1], at + 2
    return ((first & 0x1F) << 24) | (data[at + 1] << 16) | (data[at + 2] << 8) | data[at + 3], at + 4


def read_assembly_types(path):
    """The types an assembly defines, from its metadata tables: full name, base type, instance fields with their types, enum
    members, and whether the type reads its own XML (LoadDataFromXmlCustom). An empty dict for a file that is no .NET assembly."""
    data = Path(path).read_bytes()

    def u16(at):
        return int.from_bytes(data[at:at + 2], 'little')

    def u32(at):
        return int.from_bytes(data[at:at + 4], 'little')

    try:
        pe = u32(0x3C)
        if data[:2] != b'MZ' or data[pe:pe + 4] != b'PE\0\0':
            return {}
        optional, optional_size = pe + 24, u16(pe + 20)
        directories = optional + (96 if u16(optional) == 0x10B else 112)
        cli = u32(directories + 14 * 8)
        if not cli:
            return {}
        table = optional + optional_size
        sections = [(u32(at + 12), max(u32(at + 8), u32(at + 16)), u32(at + 20)) for at in range(table, table + 40 * u16(pe + 6), 40)]

        def offset(rva):
            return next(rva - start + raw for start, size, raw in sections if start <= rva < start + size)

        meta = offset(u32(offset(cli) + 8))
        if data[meta:meta + 4] != b'BSJB':
            return {}
        length = u32(meta + 12)
        streams, at = {}, meta + 20 + length
        for _ in range(u16(meta + 18 + length)):
            end = data.index(b'\0', at + 8)
            streams[data[at + 8:end].decode('ascii')] = meta + u32(at)
            at += 8 + ((end - at - 8) // 4 + 1) * 4
        tables = streams.get('#~', streams.get('#-'))
        heaps, valid = data[tables + 6], int.from_bytes(data[tables + 8:tables + 16], 'little')
        rows, at = [0] * 64, tables + 24
        for number_ in range(64):
            if valid >> number_ & 1:
                rows[number_], at = u32(at), at + 4
        string_size, guid_size, blob_size = (4 if heaps & bit else 2 for bit in (1, 2, 4))

        def index_size(of):
            return 4 if rows[of] > 0xFFFF else 2

        def coded_size(of, bits):
            return 4 if max(rows[t] for t in of) >= 1 << (16 - bits) else 2

        type_ref = coded_size((2, 1, 0x1B), 2)
        widths = [[2, string_size, guid_size, guid_size, guid_size],                     # Module
                  [coded_size((0, 0x1A, 0x23, 1), 2), string_size, string_size],          # TypeRef
                  [4, string_size, string_size, type_ref, index_size(4), index_size(6)],  # TypeDef
                  [index_size(4)],                                                        # FieldPtr
                  [2, string_size, blob_size],                                            # Field
                  [index_size(6)],                                                        # MethodPtr
                  [4, 2, 2, string_size, blob_size, index_size(8)]]                       # MethodDef
        starts = []
        for number_, width in enumerate(widths):
            starts.append(at)
            at += rows[number_] * sum(width)

        def row(of, index):
            at_ = starts[of] + (index - 1) * sum(widths[of])
            values = []
            for width in widths[of]:
                values.append(u16(at_) if width == 2 else u32(at_))
                at_ += width
            return values

        def string(index):
            start = streams['#Strings'] + index
            return data[start:data.index(b'\0', start)].decode('utf-8', 'replace')

        def blob(index):
            size, start = _compressed(data, streams['#Blob'] + index)
            return data[start:start + size]

        definitions = [row(2, i) for i in range(1, rows[2] + 1)]

        def full_name(name, namespace):
            return f'{string(namespace)}.{string(name)}' if string(namespace) else string(name)

        def definition_name(index):
            # A nested type has no namespace of its own and may share its name with another: it gets a key of its own,
            # after '#', which no Class attribute can name.
            flags, name, namespace = definitions[index - 1][:3]
            return f'{string(name)}#{Path(path).stem}:{index}' if flags & 7 >= 2 else full_name(name, namespace)

        def coded(value):
            tag, index = value & 3, value >> 2
            if index == 0:
                return None
            if tag == 0:
                return definition_name(index)
            if tag == 1:
                return full_name(*row(1, index)[1:3])
            return '<generic>'

        def signature_type(signature, at_):
            kind = signature[at_]
            at_ += 1
            while kind in (0x1F, 0x20):  # custom modifiers
                at_ = _compressed(signature, at_)[1]
                kind, at_ = signature[at_], at_ + 1
            if kind in PRIMITIVES:
                return ('prim', PRIMITIVES[kind]), at_
            if kind in (0x11, 0x12):
                value, at_ = _compressed(signature, at_)
                return ('type', coded(value)), at_
            if kind == 0x15:
                value, at_ = _compressed(signature, at_ + 1)
                generic = coded(value)
                count, at_ = _compressed(signature, at_)
                arguments = []
                for _ in range(count):
                    argument, at_ = signature_type(signature, at_)
                    arguments.append(argument)
                if generic == 'System.Collections.Generic.List`1':
                    return ('list', arguments[0]), at_
                return ('other', generic), at_
            if kind == 0x1D:
                inner, at_ = signature_type(signature, at_)
                return ('list', inner), at_
            return ('other', None), len(signature)

        def field_row(index):
            return row(4, row(3, index)[0] if rows[3] else index)

        def method_row(index):
            return row(6, row(5, index)[0] if rows[5] else index)

        types = {}
        field_count, method_count = rows[3] or rows[4], rows[5] or rows[6]
        for number_, (flags, name, namespace, extends, first_field, first_method) in enumerate(definitions):
            following = definitions[number_ + 1] if number_ + 1 < len(definitions) else None
            last_field = following[4] if following else field_count + 1
            last_method = following[5] if following else method_count + 1
            fields, members = {}, []
            for index in range(first_field, last_field):
                field_flags, field_name, field_signature = field_row(index)
                label = string(field_name)
                if field_flags & 0x10:  # static: an enum's members are its static literal fields
                    if field_flags & 0x40:
                        members.append(label)
                    continue
                if '<' not in label:
                    fields[label] = signature_type(blob(field_signature), 1)[0]
            methods = {string(method_row(index)[3]) for index in range(first_method, last_method)}
            base = coded(extends)
            types[definition_name(number_ + 1)] = {
                'base': base, 'fields': fields, 'members': members, 'custom': 'LoadDataFromXmlCustom' in methods,
                'enum': base == 'System.Enum', 'abstract': bool(flags & 0xA0)}  # abstract or an interface
        return types
    except (IndexError, KeyError, StopIteration, TypeError, ValueError):
        return {}


class TypeIndex:
    """Our classes, from our assemblies: which exist, what fields the game would fill from XML, and what each field holds."""

    def __init__(self, types, assemblies=()):
        self.types = types
        self.assemblies = list(assemblies)
        self.namespaces = {name.rsplit('.', 1)[0] for name in types if '.' in name and name.startswith('Phobos')}

    @classmethod
    def from_assemblies(cls, paths):
        types, read = {}, []
        for path in paths:
            found = read_assembly_types(path)
            if found:
                read.append(path)
                types.update(found)
        return cls(types, read) if types else None

    def ours(self, class_name):
        return '+' not in class_name and class_name.rsplit('.', 1)[0] in self.namespaces

    def fields(self, name):
        """Every field of the type and the types it is built on, and whether that list is known to be complete."""
        fields, seen = {}, set()
        while name is not None and name not in seen:
            seen.add(name)
            if name in KNOWN_BASES:
                for field, kind in KNOWN_BASES[name].items():
                    fields.setdefault(field, kind)
                return fields, True
            info = self.types.get(name)
            if info is None or info['custom']:
                return fields, False
            for field, kind in info['fields'].items():
                fields.setdefault(field, kind)
            name = info['base']
        return fields, name is None

    def schema_classes(self):
        return {name[len(OUR_NAMESPACE):] for name in self.types if name.startswith(OUR_NAMESPACE)}


def short(type_name):
    return (type_name or '').split('#')[0].rsplit('.', 1)[-1]


def check_value(element, kind, index, report):
    """One element's value against the field it fills: a number, a true or false, an enum's value, or an object of ours whose
    own fields are then checked in turn."""
    if element.attrib.get('IsNull') == 'True':
        return
    class_name = element.attrib.get('Class')
    if class_name is not None:
        if class_name in index.types:
            check_object(element, class_name, index, report)
        elif index.ours(class_name):
            report(element, f'no class {class_name}')
        return
    what, of = kind
    value = (element.text or '').strip()
    if what == 'prim':
        if len(element) or not value:
            return
        if of == 'float':
            try:
                float(value)
            except ValueError:
                report(element, f'<{element.tag}> is not a number ({value})')
        elif of == 'int' and not re.fullmatch(r'[+-]?\d+', value):
            report(element, f'<{element.tag}> is not a whole number ({value})')
        elif of == 'bool' and value.lower() not in ('true', 'false'):
            report(element, f'<{element.tag}> is not true or false ({value})')
    elif what == 'list':
        for li in element:
            if li.tag == 'li':
                check_value(li, of, index, report)
    elif what == 'type' and of in index.types:
        info = index.types[of]
        if info['enum']:
            members = info['members']
            wrong = [part.strip() for part in value.split(',') if part.strip() and part.strip() not in members
                     and not re.fullmatch(r'-?\d+', part.strip())]
            if value and not len(element) and wrong:
                report(element, f'<{element.tag}> {", ".join(wrong)} is not one of: {", ".join(members)}')
        elif info['abstract']:
            report(element, f'<{element.tag}> needs a Class: {short(of)} is only a base for other classes')
        else:
            check_object(element, of, index, report)


def check_object(element, type_name, index, report):
    """Every child element names a field of the class, and holds what that field can take."""
    info = index.types.get(type_name)
    if info is None or info['custom']:
        return
    fields, complete = index.fields(type_name)
    for child in element:
        if not isinstance(child.tag, str) or child.tag == 'li':
            continue
        kind = fields.get(child.tag)
        if kind is None:
            if complete:
                near = difflib.get_close_matches(child.tag, list(fields), n=1) or \
                    [name for name in fields if name.lower() == child.tag.lower()]
                hint = f' (did you mean <{near[0]}>?)' if near else ''
                report(child, f'<{child.tag}> is not a field of {short(type_name)}{hint}')
            continue
        check_value(child, kind, index, report)


def check_fields(node, index, report):
    """A whole Def: its own fields when it is one of our Def types, else every object of ours placed inside it."""
    if node.tag in index.types:
        check_object(node, node.tag, index, report)
        return
    if index.ours(node.tag):
        report(node, f'no Def type {node.tag}')
        return
    for element in node.iter():
        class_name = element.attrib.get('Class')
        if class_name and (class_name in index.types or index.ours(class_name)):
            check_value(element, ('other', None), index, report)


# --- Reading mods ----------------------------------------------------------------------------------------------------------

def package_id(folder):
    try:
        return (ET.parse(Path(folder, 'About', 'About.xml')).getroot().findtext('packageId') or '').strip()
    except (OSError, ET.ParseError):
        return ''


def version_key(version):
    return tuple(int(part) for part in re.findall(r'\d+', version))


def load_sets(folder):
    """Each game version's load folders, as the game picks them: LoadFolders.xml when the mod has one (every folder it lists,
    whatever mods it waits for), else the mod's root, Common and that version's folder."""
    folder, sets = Path(folder), {}
    listing = folder / 'LoadFolders.xml'
    if listing.exists():
        try:
            for version in ET.parse(listing).getroot():
                paths = [(li.text or '').strip().strip('/\\') for li in version.findall('li')]
                sets[version.tag.lstrip('vV')] = [folder / path if path else folder for path in paths]
        except ET.ParseError:
            sets = {}
    if not sets:
        base = [folder] + ([folder / 'Common'] if (folder / 'Common').is_dir() else [])
        versions = sorted((d.name for d in folder.iterdir() if d.is_dir() and re.fullmatch(r'\d+\.\d+', d.name)), key=version_key)
        sets = {version: base + [folder / version] for version in versions} or {'': base}
    return sets


def folder_files(folders):
    found = []
    for folder in folders:
        for sub in ('Defs', 'Patches'):
            found += sorted(Path(folder, sub).rglob('*.xml'))
        found += sorted(Path(folder, 'Languages').glob('*/Keyed/**/*.xml'))
    return list(dict.fromkeys(found))


class ModFiles:
    """One mod's Def nodes, patches and translation keys for one game version, each Def with the file it came from."""

    def __init__(self, folder, label, folders):
        self.folder, self.label = Path(folder), label
        self.defs, self.patches, self.keys, self.problems = [], [], set(), []
        for path in folder_files(folders):
            source = f'{label}/{path.relative_to(self.folder).as_posix()}'
            try:
                tree = parse_lines(path)
            except expat.ExpatError as error:
                self.problems.append(finding('error', source, error.lineno, None, None,
                                             f'the file is not well-formed XML ({expat.ErrorString(error.code)})'))
                continue
            if 'Keyed' in path.relative_to(self.folder).parts:
                self.keys.update(child.tag for child in tree)
            elif tree.tag == 'Defs':
                self.defs += [(node, source) for node in tree if isinstance(node.tag, str)]
            elif tree.tag == 'Patch':
                self.patches.append((tree, source))


def finding(severity, file, line, def_name, kind, problem):
    return {'severity': severity, 'file': file, 'line': line, 'def': def_name, 'kind': kind, 'problem': problem,
            'fix': fix_for(problem)}


def find_our_mods(places, exclude=()):
    """Our mods among these folders: a mod folder of ours, or every mod of ours in a folder of mods (the game's Mods folder,
    Steam's Workshop folder or this repository's mods/)."""
    exclude = {Path(p).resolve() for p in exclude}
    found, packages = [], set()
    for place in map(Path, places):
        candidates = [place] if (place / 'About' / 'About.xml').exists() else \
            sorted(child for child in place.iterdir() if child.is_dir()) if place.is_dir() else []
        for folder in candidates:
            package = package_id(folder).lower()
            if package.startswith(OUR_PACKAGE_PREFIX) and package not in packages and folder.resolve() not in exclude:
                found.append(folder)
                packages.add(package)  # one copy of each mod, the first found, as the game loads one
    return found


class Ours:
    """D'thorga's Defs, keys and assemblies, from our mods' newest version folders."""

    def __init__(self, folders, root=None):
        self.folders = list(folders)
        self.mods, dlls = [], []
        for folder in self.folders:
            sets = load_sets(folder)
            newest = max(sets, key=version_key)
            label = folder.name if not folder.name.isdigit() else (package_id(folder) or folder.name)
            self.mods.append(ModFiles(folder, label, sets[newest]))
            for load_folder in sets[newest]:
                dlls += sorted(Path(load_folder, 'Assemblies').glob('*.dll'))
        self.index = TypeIndex.from_assemblies(list(dict.fromkeys(dlls)))
        self.keys = set().union(*(mod.keys for mod in self.mods)) if self.mods else set()
        if self.index is not None:
            self.classes, self.classes_from = self.index.schema_classes(), f'{len(self.index.assemblies)} assemblies'
        elif root is not None and Path(root, SCHEMA_SOURCE).is_dir():
            self.classes, self.classes_from = our_classes(root), 'the source'
        else:
            self.classes, self.classes_from = None, None


def xpath_targets(document, xpath):
    """What an xpath selects in the combined Defs, or None when it uses XPath this check cannot follow."""
    path = (xpath or '').strip()
    if not path or '|' in path or re.search(r'\w\(', path):
        return None
    if path.startswith('//'):
        path = './/' + path[2:]
    else:
        path = path.lstrip('/')
        if path == 'Defs':
            return [document]
        if not path.startswith('Defs/'):
            return None
        path = './' + path[5:]
    try:
        return document.findall(path)
    except (SyntaxError, KeyError, TypeError):
        return None


class Patcher:
    """The mod's patch operations applied to the combined Defs as the game applies them, so far as ElementTree's XPath can
    follow them; anything that names our Defs and cannot be followed becomes a note."""

    SIMPLE = {'PatchOperationAdd', 'PatchOperationInsert', 'PatchOperationRemove', 'PatchOperationReplace',
              'PatchOperationAttributeSet', 'PatchOperationAttributeAdd', 'PatchOperationAttributeRemove'}

    def __init__(self, document, names):
        self.document, self.names = document, names
        self.touched, self.findings = {}, []

    def mentions_ours(self, xpath):
        return bool(xpath) and ('Phobos.VehicleSystems' in xpath or any(f'"{name}"' in xpath or f"'{name}'" in xpath for name in self.names))

    def apply(self, patch, source):
        for operation in patch:
            if isinstance(operation.tag, str) and operation.tag == 'Operation':
                self.run(operation, source)

    def run(self, operation, source):
        kind = operation.attrib.get('Class', '').rsplit('.', 1)[-1]
        xpath = text(operation, 'xpath')
        if kind == 'PatchOperationSequence':
            results = [self.run(li, source) for li in items(operation, 'operations')]
            return all(results)
        if kind == 'PatchOperationFindMod':  # taken as if the mods it looks for are active
            match = operation.find('match')
            return True if match is None else self.run(match, source)
        if kind == 'PatchOperationConditional':
            targets = xpath_targets(self.document, xpath)
            branch = operation.find('match' if targets or targets is None else 'nomatch')
            return True if branch is None else self.run(branch, source)
        if kind not in self.SIMPLE:
            if self.mentions_ours(xpath):
                self.note(operation, source, f'a {kind or "patch"} on {xpath} was not followed')
            return True
        targets = xpath_targets(self.document, xpath)
        if targets is None:
            if self.mentions_ours(xpath):
                self.note(operation, source, f'the patch on {xpath} was not followed: its XPath is beyond this check')
            return True
        if not targets:
            if self.mentions_ours(xpath) and text(operation, 'success', 'Normal') not in ('Always', 'Invert'):
                self.findings.append(finding('error', source, line_of(operation), None, 'patch',
                                             f'the patch on {xpath} changes nothing: no Def matches it'))
            return False
        parents = {child: parent for parent in self.document.iter() for child in parent}
        for target in targets:
            self.change(kind, operation, target, parents.get(target), source)
            top = target
            while parents.get(top) is not None and parents[top] is not self.document:
                top = parents[top]
            if top is not self.document:
                self.touched.setdefault(id(top), (source, line_of(operation)))
        return True

    def values(self, operation, source):
        copies = [copy.deepcopy(node) for node in (operation.find('value') if operation.find('value') is not None else [])]
        for node in copies:
            for element in node.iter():
                element.attrib[FROM] = source
        return copies

    def change(self, kind, operation, target, parent, source):
        order = text(operation, 'order')
        if kind == 'PatchOperationAdd':
            values = self.values(operation, source)
            for value in (reversed(values) if order == 'Prepend' else values):
                target.insert(0, value) if order == 'Prepend' else target.append(value)
        elif kind in ('PatchOperationInsert', 'PatchOperationReplace') and parent is not None:
            at = list(parent).index(target) + (1 if kind == 'PatchOperationInsert' and order == 'Append' else 0)
            for offset_, value in enumerate(self.values(operation, source)):
                parent.insert(at + offset_, value)
            if kind == 'PatchOperationReplace':
                parent.remove(target)
        elif kind == 'PatchOperationRemove' and parent is not None:
            parent.remove(target)
        elif kind in ('PatchOperationAttributeSet', 'PatchOperationAttributeAdd'):
            if kind == 'PatchOperationAttributeSet' or text(operation, 'attribute') not in target.attrib:
                target.attrib[text(operation, 'attribute')] = text(operation, 'value', '')
        elif kind == 'PatchOperationAttributeRemove':
            target.attrib.pop(text(operation, 'attribute'), None)

    def note(self, operation, source, problem):
        self.findings.append(finding('note', source, line_of(operation), None, 'patch', problem))


def resolve_defs(nodes, owner_of):
    """XML inheritance over our Defs and the mod's together: a parent is looked for in the child's own mod first, as the game
    does. Returns the resolved Defs by tag, the Defs whose parent was found nowhere, and each resolved Def's own node."""
    by_owner = {}
    for node, source in nodes:
        if 'Name' in node.attrib:
            by_owner.setdefault((node.tag, owner_of[id(node)]), {})[node.attrib['Name']] = node
    missing = []

    def parent_of(node):
        name, owner = node.attrib.get('ParentName'), owner_of[id(node)]
        own = by_owner.get((node.tag, owner), {})
        if name in own:
            return own[name]
        for (tag, other), named in by_owner.items():
            if tag == node.tag and name in named:
                return named[name]
        return None

    def resolve(node, seen=()):
        name = node.attrib.get('ParentName')
        if not name or name in seen:
            return node
        parent = parent_of(node)
        if parent is None:
            missing.append(node)
            return node
        return merge(resolve(parent, seen + (name,)), node)

    resolved, original_of = {tag: [] for tag in SCHEMA_TAGS}, {}
    for node, source in nodes:
        if node.attrib.get('Abstract') != 'True':
            result = resolve(node)
            owner_of[id(result)], original_of[id(result)] = owner_of[id(node)], node
            resolved[node.tag].append((result, source))
        elif node.attrib.get('ParentName'):
            resolve(node)  # an abstract Def's parent is looked for too
    return resolved, missing, original_of


def check_mod_version(ours, ours_baseline, mod, index):
    """One mod's files for one game version, against ours: every finding about the mod's own Defs and patches, and about our
    Defs where the mod changed what we would find."""
    findings = list(mod.problems)
    document = ET.Element('Defs')
    owner_of, source_of = {}, {}
    for files, owner in [(our_mod, 'ours') for our_mod in ours.mods] + [(mod, 'theirs')]:
        for node, source in files.defs:
            if node.tag in SCHEMA_TAGS:
                node = copy.deepcopy(node)
                document.append(node)
                owner_of[id(node)], source_of[id(node)] = owner, source
    names = {text(node, 'defName') for node in document if text(node, 'defName')}
    patcher = Patcher(document, names)
    for patch, source in mod.patches:
        patcher.apply(patch, source)
    findings += patcher.findings
    nodes = []
    for node in document:
        if not isinstance(node.tag, str) or node.tag not in SCHEMA_TAGS:
            continue
        if id(node) not in owner_of:  # a whole Def a patch added
            owner_of[id(node)], source_of[id(node)] = 'theirs', node.attrib.get(FROM, mod.label)
        nodes.append((node, source_of[id(node)]))
    resolved, missing, original_of = resolve_defs(nodes, owner_of)
    for node in missing:
        if owner_of[id(node)] == 'theirs':
            findings.append(finding('error', source_of[id(node)], line_of(node), text(node, 'defName') or node.attrib.get('Name'),
                                    KINDS[node.tag], f'ParentName {node.attrib["ParentName"]} names no {KINDS[node.tag]} Def '
                                                     f'with that Name, in your mod or D\'thorga\'s; its other checks wait '
                                                     f'until its parent is found'))
    # A Def of the mod's with the defName of one of ours replaces ours, as the game does between mods.
    for tag in SCHEMA_TAGS:
        theirs = {text(node, 'defName'): (node, source) for node, source in resolved[tag] if owner_of[id(node)] == 'theirs'}
        kept = []
        for node, source in resolved[tag]:
            name = text(node, 'defName')
            if owner_of[id(node)] == 'ours' and name in theirs:
                their_node, their_source = theirs[name]
                findings.append(finding('warning', their_source, line_of(their_node), name, KINDS[tag],
                                        f'replaces D\'thorga\'s {KINDS[tag]} of the same defName ({source})'))
                continue
            kept.append((node, source))
        resolved[tag] = kept
    result = check_defs(resolved, ours.keys | mod.keys, ours.classes)
    places, orphans = {}, {id(node) for node in missing}
    for tag in SCHEMA_TAGS:
        for node, source in resolved[tag]:
            name = text(node, 'defName')
            place = (source, line_of(node), name, KINDS[tag], owner_of[id(node)],
                     patcher.touched.get(id(original_of[id(node)])))
            if id(original_of[id(node)]) in orphans and owner_of[id(node)] == 'theirs':
                place = None  # without its parent the Def is not what the game would see; the ParentName error says so
            places.setdefault(f'{source}: {name}' if name else source, place)
    keys = sorted(places, key=len, reverse=True)
    for severity, messages in (('error', result['errors']), ('warning', result['warnings'])):
        for message in messages:
            key = next((k for k in keys if message.startswith(k + ': ')), None)
            if key is None or places[key] is None:
                continue
            source, line, name, kind, owner, touched = places[key]
            problem = message[len(key) + 2:]
            if index is not None and problem.startswith('no class '):
                continue  # the field check names the same class, at its own line
            if owner == 'theirs':
                findings.append(finding(severity, source, line, name, kind, problem))
            elif message not in ours_baseline:
                if touched:
                    findings.append(finding(severity, touched[0], touched[1], name, kind,
                                            f'D\'thorga\'s {kind} {name}, as your patch changes it: {problem}'))
                else:
                    findings.append(finding(severity, source, line, name, kind,
                                            f'D\'thorga\'s {kind} {name}, with your mod loaded: {problem}'))
    if index is not None:
        for node, source in mod.defs:
            name = text(node, 'defName') or node.attrib.get('Name')

            def report(element, problem, node=node, source=source, name=name):
                findings.append(finding('error', source, line_of(element), name, KINDS.get(node.tag, short(node.tag)), problem))
            check_fields(node, index, report)
        for node in document:  # what the mod's patches put into Defs: ours, or a whole new one
            if node.tag in index.types and (id(node) in patcher.touched or node.attrib.get(FROM)):
                whose = 'D\'thorga\'s ' if owner_of.get(id(node)) == 'ours' else 'your '

                def report(element, problem, node=node, whose=whose):
                    if element.attrib.get(FROM):
                        findings.append(finding('error', element.attrib[FROM], line_of(element), text(node, 'defName'),
                                                KINDS.get(node.tag), f'what your patch adds to {whose}{KINDS.get(node.tag)} '
                                                                     f'{text(node, "defName")}: {problem}'))
                check_object(node, node.tag, index, report)
    return findings


def check_mods(mod_folders, our_places, root=None):
    """Every given mod, each of its game versions, against our mods found in our_places."""
    our_folders = find_our_mods(our_places, exclude=mod_folders)
    ours = Ours(our_folders, root)
    baseline = check_defs(resolve_defs(
        [(node, source) for our_mod in ours.mods for node, source in our_mod.defs if node.tag in SCHEMA_TAGS],
        {id(node): 'ours' for our_mod in ours.mods for node, _ in our_mod.defs})[0], ours.keys, ours.classes)
    report = {'ours': [str(folder) for folder in our_folders], 'classesFrom': ours.classes_from,
              'fieldsChecked': ours.index is not None,
              'ourCounts': {k: len(baseline[k]) for k in ('systems', 'loadouts', 'drivetrains', 'profiles', 'conditions')},
              'mods': []}
    for folder in map(Path, mod_folders):
        sets = load_sets(folder)
        merged, read = {}, {}
        for version, folders in sets.items():
            files = ModFiles(folder, folder.name, folders)
            for node, source in files.defs:
                read[(source, line_of(node))] = KINDS.get(node.tag, short(node.tag))
            for _, source in files.patches:
                read[(source, None)] = 'patch file'
            for item in check_mod_version(ours, set(baseline['errors'] + baseline['warnings']), files, ours.index):
                key = tuple(item[k] for k in ('severity', 'file', 'line', 'def', 'problem'))
                merged.setdefault(key, dict(item, versions=[]))['versions'].append(version)
        order = {'error': 0, 'warning': 1, 'note': 2}
        findings = sorted(merged.values(), key=lambda f: (order[f['severity']], f['file'], f['line'] or 0, f['problem']))
        for item in findings:
            if len(item['versions']) == len(sets):
                item['versions'] = []
        report['mods'].append({'mod': str(folder), 'name': folder.name, 'packageId': package_id(folder),
                               'versions': [v for v in sets if v], 'read': dict(Counter(read.values())), 'findings': findings})
    return report


def print_mod_report(report):
    if not report['ours']:
        print('D\'thorga Framework was not found. Point --ours at its folder, or at the folder that holds it: your game\'s Mods '
              'folder, or Steam\'s steamapps/workshop/content/294100.')
        return
    counts = report['ourCounts']
    places = sorted({str(Path(folder).parent) for folder in report['ours']})
    print(f'D\'thorga\'s Defs read from {len(report["ours"])} mod folders in {", ".join(places)} ({counts["systems"]} systems, {counts["loadouts"]} '
          f'loadouts, {counts["drivetrains"]} drivetrains, {counts["profiles"]} flight profiles, {counts["conditions"]} conditions).')
    if not report['fieldsChecked']:
        print('Field names and values were not checked: no D\'thorga assemblies were found in those folders (an installed copy has '
              'them in its Assemblies folders).' + ('' if report['classesFrom'] else ' Class names were taken on trust too.'))
    for mod in report['mods']:
        findings = mod['findings']
        errors = sum(1 for f in findings if f['severity'] == 'error')
        warnings = sum(1 for f in findings if f['severity'] == 'warning')
        versions = f' (game versions {", ".join(mod["versions"])})' if mod['versions'] else ''
        read = ', '.join(f'{count} {kind}' for kind, count in sorted(mod['read'].items())) or 'no Defs or patches'
        print(f'\n{mod["name"]}{versions}: read {read}.\n{errors} errors, {warnings} warnings.')
        for f in findings:
            place = f['file'] + (f', line {f["line"]}' if f['line'] else '')
            what = f' - {f["kind"]} {f["def"]}' if f['def'] else ''
            only = f' [only in {", ".join(f["versions"])}]' if f['versions'] else ''
            print(f'\n{f["severity"].upper():8}{place}{what}{only}\n        {f["problem"]}\n        To fix: {f["fix"]}')
    print('\nAn error is something the game will refuse or get wrong; a warning is worth a look; a note says what was not checked.')


def main():
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('--root', default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument('--json', action='store_true')
    parser.add_argument('--mod', action='append', default=[], metavar='FOLDER',
                        help='check another mod\'s folder against our Defs (repeatable); the run over mods/ is then skipped')
    parser.add_argument('--ours', action='append', default=[], metavar='FOLDER',
                        help='where our mods are: a mod folder of ours, or a folder holding them (the game\'s Mods folder, '
                             'Steam\'s workshop/content/294100); by default this repository\'s mods/, else Steam\'s usual places')
    args = parser.parse_args()
    if args.mod:
        missing = [folder for folder in args.mod if not Path(folder).is_dir()]
        if missing:
            print('No such folder: ' + ', '.join(missing))
            return 2
        places = args.ours
        if not places:
            repository = Path(args.root, 'mods')
            places = [str(repository)] if Path(repository, 'PhobosFramework', 'About', 'About.xml').exists() else \
                [place for place in DEFAULT_OUR_PLACES if Path(place).is_dir()]
        report = check_mods(args.mod, places, args.root)
        if args.json:
            print(json.dumps(report, indent=2))
        else:
            print_mod_report(report)
        if not report['ours']:
            return 2
        return 1 if any(f['severity'] == 'error' for mod in report['mods'] for f in mod['findings']) else 0
    result = check(args.root)
    if args.json:
        print(json.dumps(result, indent=2))
    else:
        print(f'{len(result["systems"])} systems, {len(result["loadouts"])} loadouts, {len(result["drivetrains"])} drivetrains, '
              f'{len(result["profiles"])} flight profiles, {len(result["conditions"])} conditions; '
              f'{len(result["errors"])} errors, {len(result["warnings"])} warnings.')
        for line in result['errors']:
            print('ERROR   ' + line)
        for line in result['warnings']:
            print('warning ' + line)
    return 1 if result['errors'] else 0


if __name__ == '__main__':
    sys.exit(main())
