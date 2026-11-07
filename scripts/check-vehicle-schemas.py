"""Check every vehicle systems schema in the repository without the game: systems, loadouts and the keys and classes they name.

    python scripts/check-vehicle-schemas.py [--root DIR] [--json]

Reads each mods/*/ XML file for Phobos.VehicleSystems.PhobosVehicleSystemDef and PhobosVehicleLoadoutDef, resolves XML inheritance
the way the game does (a child's elements override, list items append unless Inherit="False"), and checks what the game would only
find at load, or never: quantities and flows that name each other, parameters declared before use, stages in order, roles a system
reads declared and bound, loadouts that name real systems and parameters, appended parts with unique keys and a role to take cells
from, the effect, input and condition classes the service ships, and the translation keys messages and refusals use. Another mod's
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
OUR_NAMESPACE = 'Phobos.VehicleSystems.'
SCHEMA_SOURCE = 'src/PhobosFramework.Vehicles/Schema'
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
    raw = {SYSTEM_TAG: [], LOADOUT_TAG: []}
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
    for path in Path(root, SCHEMA_SOURCE).glob('*.cs'):
        names.update(CLASS_LINE.findall(path.read_text(encoding='utf-8')))
    return names


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
            for restore in items(service, 'restores'):
                if text(restore, 'quantity') not in all_quantities:
                    errors.append(f'{where}: the service restores a quantity no system has ({text(restore, "quantity")})')
        jump = node.find('jumpStart')
        if jump is not None:
            if text(jump, 'quantity') not in system['quantities']:
                errors.append(f'{where}: the jump-start names an unknown quantity ({text(jump, "quantity")})')
            if text(jump, 'capacityParameter') not in system['parameters']:
                errors.append(f'{where}: the jump-start names the undeclared parameter {text(jump, "capacityParameter")}')

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
        names = [text(load, 'name') for load in items(node, 'loads')]
        if len(names) != len(set(names)) or None in names:
            errors.append(f'{where}: a load has no name or a repeated one')
        for element in walk_classes(node):
            cls = element.attrib['Class']
            if cls.startswith(OUR_NAMESPACE) and cls[len(OUR_NAMESPACE):] not in classes:
                errors.append(f'{where}: no class {cls}')
    return {'systems': sorted(systems), 'loadouts': sorted(loadouts), 'errors': errors, 'warnings': warnings}


def main():
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('--root', default=str(Path(__file__).resolve().parents[1]))
    parser.add_argument('--json', action='store_true')
    args = parser.parse_args()
    result = check(args.root)
    if args.json:
        print(json.dumps(result, indent=2))
    else:
        print(f'{len(result["systems"])} systems, {len(result["loadouts"])} loadouts; '
              f'{len(result["errors"])} errors, {len(result["warnings"])} warnings.')
        for line in result['errors']:
            print('ERROR   ' + line)
        for line in result['warnings']:
            print('warning ' + line)
    return 1 if result['errors'] else 0


if __name__ == '__main__':
    sys.exit(main())
