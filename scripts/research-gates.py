"""Report what research gates everything a player can build, craft or fit in our mods, and what has no gate at all.

Reads every XML file under mods/. Writes nothing. It answers "is this gated, and by what?" before a research audit, where
check-research.py answers "is the tree itself sound?".

Listed, each with the research it names (its own, or inherited from a parent Def in our mods):

- buildings and vehicles a player can place (a Def with a designationCategory, VehicleBuildDefs included);
- items crafted at a bench (a Def with a recipeMaker) and stand-alone recipes (RecipeDef), with the benches that use them;
- vehicle upgrades (each node of an upgrade tree), with the vehicle's own gate beside the node's;
- our own Def types that carry a researchPrerequisite field (fittings, tunes and the like).

Marks: "none" means nothing gates it; "bench" a recipe gated only through the bench it is made at; "vehicle" an upgrade gated
only by the research that builds its vehicle.

    python scripts/research-gates.py                 everything, grouped by mod
    python scripts/research-gates.py --ungated       only what has no gate of its own
    python scripts/research-gates.py --project X     everything a project (defName, or part of one) unlocks

Limits: research added by an XML patch (PatchOperation) shows only when the patch's value holds the Def's whole research list;
a patch that adds to another mod's Def is listed under "patches". Defs made in C# are not seen. An offline report is not an
in-game test.
"""
import argparse
from collections import defaultdict
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RESEARCH_FIELDS = ('researchPrerequisites', 'researchPrerequisite')
UPGRADE_TREE = 'Vehicles.UpgradeTreeDef'
SKIP_TAGS = {'ResearchProjectDef', 'ResearchTabDef'}


def text_of(element):
    return (element.text or '').strip() if element is not None else ''


def research_in(element):
    """The research named directly under an element: a list of names, or None when the element says nothing."""
    found = None
    for field in RESEARCH_FIELDS:
        node = element.find(field)
        if node is None:
            continue
        found = found or []
        items = node.findall('li')
        found.extend(text_of(i) for i in items) if items else found.append(text_of(node))
    return [name for name in found if name] if found is not None else None


class Def:
    def __init__(self, element, path, mod):
        self.element, self.path, self.mod = element, path, mod
        self.tag = element.tag
        self.name = text_of(element.find('defName')) or element.get('Name', '')
        self.label = text_of(element.find('label'))
        self.parent = element.get('ParentName')
        self.abstract = element.get('Abstract', '').lower() == 'true'


def read(mods_dir):
    defs, patches = [], []
    for path in sorted(mods_dir.rglob('*.xml')):
        parts = path.relative_to(mods_dir).parts
        if 'Languages' in parts or 'About' in parts:
            continue
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        if root.tag == 'Defs':
            defs.extend(Def(e, path, parts[0]) for e in root)
        elif root.tag == 'Patch':
            for operation in root.iter('Operation'):
                xpath = text_of(operation.find('xpath'))
                if 'esearch' in xpath or any(operation.iter(f) for f in RESEARCH_FIELDS if next(operation.iter(f), None) is not None):
                    names = sorted({text_of(li) for f in RESEARCH_FIELDS for node in operation.iter(f) for li in (node.findall('li') or [node]) if text_of(li)})
                    patches.append((parts[0], path, xpath, names))
    return defs, patches


def inherited(definition, by_name, field_reader, seen=None):
    """A value read from a Def, or from the nearest parent in our mods that has one."""
    seen = seen or set()
    value = field_reader(definition.element)
    if value is not None:
        return value
    parent = by_name.get(definition.parent)
    if parent is None or parent.name in seen:
        return None
    return inherited(parent, by_name, field_reader, seen | {definition.name})


def gate_text(names):
    return ', '.join(names) if names else 'none'


def collect(defs):
    by_name = {}
    for d in defs:
        by_name.setdefault(d.element.get('Name') or d.name, d)
        if d.name:
            by_name.setdefault(d.name, d)
    rows = []  # (mod, kind, defName, label, gate list, note)

    def gate_of(d):
        return inherited(d, by_name, research_in) or []

    # Buildings and vehicles a player places.
    for d in defs:
        if d.abstract or d.tag in SKIP_TAGS or d.tag == UPGRADE_TREE:
            continue
        category = inherited(d, by_name, lambda e: text_of(e.find('designationCategory')) or None if e.find('designationCategory') is not None and e.find('designationCategory').get('IsNull') is None else None)
        if category:
            rows.append((d.mod, 'build', d.name, d.label, gate_of(d), category))

    # Benches' gates, to say what gates a recipe through its bench.
    bench_gate = {d.name: gate_of(d) for d in defs if d.name}

    # Crafted items (recipeMaker) and stand-alone recipes.
    for d in defs:
        if d.abstract:
            continue
        maker = d.element.find('recipeMaker')
        if maker is not None:
            own = research_in(maker) or []
            users = [text_of(li) for li in maker.findall('recipeUsers/li')]
            rows.append((d.mod, 'craft', d.name, d.label, own, recipe_note(own, users, bench_gate)))
        elif d.tag == 'RecipeDef':
            own = gate_of(d)
            users = [text_of(li) for li in d.element.findall('recipeUsers/li')]
            rows.append((d.mod, 'recipe', d.name, d.label, own, recipe_note(own, users, bench_gate)))

    # Upgrade nodes, with their vehicle's gate.
    tree_vehicle = {}
    for d in defs:
        for comp in d.element.findall('comps/li'):
            if comp.get('Class', '').endswith('CompProperties_UpgradeTree'):
                tree_vehicle[text_of(comp.find('def'))] = d
    build_for = {text_of(d.element.find('thingToSpawn')): d for d in defs if d.element.find('thingToSpawn') is not None}
    for d in defs:
        if d.tag != UPGRADE_TREE:
            continue
        vehicle = tree_vehicle.get(d.name)
        build = build_for.get(vehicle.name) if vehicle else None
        vehicle_gate = gate_of(build) if build else []
        for node in d.element.findall('nodes/li'):
            own = research_in(node) or []
            label = text_of(node.find('label'))
            note = 'vehicle: ' + gate_text(vehicle_gate) if not own else ''
            rows.append((d.mod, 'upgrade', f"{d.name}/{text_of(node.find('key'))}", label, own, note))

    # Our own Def types with a research field (fittings, tunes), and anything else not yet listed that names research.
    listed = {r[2] for r in rows}
    for d in defs:
        if d.abstract or d.tag in SKIP_TAGS or d.tag == UPGRADE_TREE or d.name in listed or not d.tag.startswith('Phobos.'):
            continue
        own = research_in(d.element)
        if own is not None or d.tag.endswith(('FittingDef', 'TuneDef', 'FamilyDef')):
            rows.append((d.mod, d.tag.rsplit('.', 1)[-1], d.name, d.label, own or [], ''))
    return rows


def recipe_note(own, users, bench_gate):
    if own or not users:
        return ''
    gates = sorted({name for user in users for name in bench_gate.get(user, [])})
    return 'bench: ' + (gate_text(gates) if gates else 'none') + ' (' + ', '.join(users) + ')'


def effectively_ungated(row):
    _, kind, _, _, gate, note = row
    if gate:
        return False
    if kind in ('craft', 'recipe', 'upgrade') and note and not note.endswith('none') and 'none (' not in note:
        return False
    return True


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--mods', type=Path, default=ROOT / 'mods')
    parser.add_argument('--ungated', action='store_true', help='only what nothing gates, directly or through a bench or vehicle')
    parser.add_argument('--weak', action='store_true', help='also what is gated only through its bench or its vehicle')
    parser.add_argument('--project', help='only what this project (or part of its name) unlocks')
    parser.add_argument('--kind', help='only this kind: build, craft, recipe, upgrade, or a Def type')
    args = parser.parse_args(argv)

    defs, patches = read(args.mods)
    rows = collect(defs)
    if args.kind:
        rows = [r for r in rows if r[1].lower() == args.kind.lower()]
    if args.project:
        rows = [r for r in rows if any(args.project.lower() in name.lower() for name in r[4]) or args.project.lower() in r[5].lower()]
    if args.ungated:
        rows = [r for r in rows if effectively_ungated(r)]
    elif args.weak:
        rows = [r for r in rows if not r[4]]

    by_mod = defaultdict(list)
    for row in rows:
        by_mod[row[0]].append(row)
    total = 0
    for mod in sorted(by_mod):
        print(f'\n## {mod}\n')
        print('| Kind | Def | Label | Research | Note |')
        print('| --- | --- | --- | --- | --- |')
        for _, kind, name, label, gate, note in sorted(by_mod[mod], key=lambda r: (r[1], r[2])):
            print(f'| {kind} | {name} | {label} | {gate_text(gate)} | {note} |')
            total += 1
    print(f'\n{total} rows.')
    if patches and not (args.ungated or args.weak or args.kind):
        print('\n## Patches that name research (not folded into the rows above)\n')
        for mod, path, xpath, names in patches:
            if args.project and not any(args.project.lower() in n.lower() for n in names):
                continue
            print(f'- {mod}: {path.relative_to(args.mods)}: {xpath[:110]} -> {gate_text(names)}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
