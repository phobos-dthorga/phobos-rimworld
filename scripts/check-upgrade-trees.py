"""Check our vehicles' upgrade trees without the game, print them, and keep their diagrams in the design records current.

    python scripts/check-upgrade-trees.py              check every tree and its diagram (part of the gate)
    python scripts/check-upgrade-trees.py show [TREE]  every tree (or those matching TREE) as text: links, exclusions, tie-ins
    python scripts/check-upgrade-trees.py --write      rewrite every diagram from its tree

Owner, 17 Nov 2026 (AGENTS.md, "Upgrade trees"): two upgrades are linked only when one does nothing, or makes no physical sense,
without the other. Reads every Vehicles.UpgradeTreeDef in mods/ and every PatchOperationAdd into one's nodes (a tie-in node, from a
Mods/<Other>/ folder), with our "any one of" links (Phobos.VehicleSystems.PhobosUpgradeAnyOf). Fails on what the game would accept
silently or find only in play: a key used twice, an exclusion written on one side only or naming no node, a prerequisite or any-of
parent that does not exist, a node of ours that needs a tie-in node (it would be unfittable without that mod), a node that both needs
and excludes another, a loop of prerequisites, two nodes on one grid cell, and a tree with no diagram, or a stale one, in
docs/development. A diagram sits under a marker line, <!-- upgrade-tree: DEFNAME -->, followed by its ```mermaid block.
Exit code 1 when anything is wrong.
"""
import argparse
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TREE_TAG = 'Vehicles.UpgradeTreeDef'
ANY_OF_CLASS = 'Phobos.VehicleSystems.PhobosUpgradeAnyOf'
NODES_XPATH = re.compile(r'UpgradeTreeDef\[defName="([^"]+)"\]/nodes\s*$')
TIE_IN_FOLDER = re.compile(r'[\\/]Mods[\\/]([^\\/]+)[\\/]')
MARKER = re.compile(r'^<!-- upgrade-tree: (\S+) -->\s*$')


@dataclass
class Node:
    key: str
    label: str
    coord: str
    prereqs: list
    excludes: list
    source: str
    tie_in: str | None  # the Mods/<Other> folder that adds it, or None for a node of the tree itself


@dataclass
class AnyOf:
    node: str
    parents: list
    holds: bool


@dataclass
class Tree:
    name: str
    source: str
    nodes: dict = field(default_factory=dict)
    duplicates: list = field(default_factory=list)
    any_of: list = field(default_factory=list)


def _items(element, tag):
    return [li.text.strip() for li in element.findall(f'{tag}/li') if li.text and li.text.strip()]


def _node(element, source, tie_in):
    excludes = _items(element, 'disableIfUpgradeNodesEnabled')
    single = (element.findtext('disableIfUpgradeNodeEnabled') or '').strip()
    if single:
        excludes.append(single)
    return Node(key=(element.findtext('key') or '').strip(), label=(element.findtext('label') or '').strip(),
                coord=re.sub(r'\s', '', element.findtext('gridCoordinate') or ''), prereqs=_items(element, 'prerequisiteNodes'),
                excludes=excludes, source=source, tie_in=tie_in)


def gather(mods):
    """Every tree of ours with its tie-in nodes, keyed by defName. Files that are not XML Defs or patches are skipped."""
    trees, patches = {}, []
    for path in sorted(mods.rglob('*.xml')):
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        rel = path.relative_to(mods.parent).as_posix()
        for element in root.findall(TREE_TAG):
            tree = Tree(name=(element.findtext('defName') or '').strip(), source=rel)
            for li in element.findall('nodes/li'):
                _add(tree, _node(li, rel, None))
            for ext in element.findall('modExtensions/li'):
                if ext.get('Class') == ANY_OF_CLASS:
                    for link in ext.findall('links/li'):
                        tree.any_of.append(AnyOf(node=(link.findtext('node') or '').strip(), parents=_items(link, 'anyOf'),
                                                 holds=(link.findtext('holdsParent') or '').strip().lower() == 'true'))
            trees[tree.name] = tree
        for operation in root.iter('Operation'):
            match = NODES_XPATH.search(operation.findtext('xpath') or '')
            if match:
                folder = TIE_IN_FOLDER.search(rel)
                patches.append((match.group(1), rel, folder.group(1) if folder else 'a patch', operation.findall('value/li')))
    for name, rel, tie_in, items in patches:
        if name in trees:  # a patch into another author's tree is not ours to check
            for li in items:
                _add(trees[name], _node(li, rel, tie_in))
    return trees


def _add(tree, node):
    if node.key in tree.nodes:
        tree.duplicates.append(node.key)
    tree.nodes[node.key] = node


def check_tree(tree):
    problems = []
    nodes = tree.nodes

    def say(text):
        problems.append(f'{tree.name}: {text}')

    for key in tree.duplicates:
        say(f'the key {key} is used twice')
    cells = {}
    for node in nodes.values():
        if node.coord:
            if node.coord in cells:
                say(f'{node.key} and {cells[node.coord]} share the grid cell {node.coord}')
            cells.setdefault(node.coord, node.key)
        for other in node.excludes:
            if other not in nodes:
                say(f'{node.key} excludes {other}, which no tree or tie-in defines')
            elif node.key not in nodes[other].excludes:
                say(f'{node.key} excludes {other}, but {other} does not exclude {node.key}: write the exclusion on both')
        for other in node.prereqs:
            if other not in nodes:
                say(f'{node.key} needs {other}, which no tree or tie-in defines')
            elif node.tie_in is None and nodes[other].tie_in is not None:
                say(f'{node.key} needs {other}, a tie-in node: without that mod it could never be fitted')
            if other in node.excludes:
                say(f'{node.key} both needs and excludes {other}')
    for link in tree.any_of:
        if link.node not in nodes:
            say(f'an any-of link names {link.node}, which no tree or tie-in defines')
            continue
        if not link.parents:
            say(f'the any-of link for {link.node} names no parents')
            continue
        missing = [p for p in link.parents if p not in nodes]
        for parent in missing:
            say(f'the any-of link for {link.node} names {parent}, which no tree or tie-in defines')
        present = [p for p in link.parents if p in nodes]
        if nodes[link.node].tie_in is None and present and all(nodes[p].tie_in is not None for p in present):
            say(f'{link.node} needs any one of tie-in nodes only: without those mods it could never be fitted')
        for parent in present:
            if parent in nodes[link.node].excludes:
                say(f'{link.node} both needs any one of, and excludes, {parent}')
    problems.extend(f'{tree.name}: {loop}' for loop in _loops(nodes))
    return problems


def _loops(nodes):
    found, state = [], {}

    def visit(key, path):
        state[key] = 1
        for other in nodes[key].prereqs:
            if other not in nodes:
                continue
            if state.get(other) == 1:
                found.append('the prerequisites loop: ' + ' -> '.join(path[path.index(other):] + [other]))
            elif other not in state:
                visit(other, path + [other])
        state[key] = 2

    for key in nodes:
        if key not in state:
            visit(key, [key])
    return found


def _implied(tree, a, b):
    """True when a node's exclusion of another follows from what it needs (compartments need a liquid tank, and every liquid tank
    already excludes the gas vessels), so a diagram need not draw it."""
    nodes = tree.nodes
    for prereq in nodes[a].prereqs:
        if prereq in nodes and b in nodes[prereq].excludes:
            return True
    for link in tree.any_of:
        present = [p for p in link.parents if p in nodes]
        if link.node == a and present and all(b in nodes[p].excludes for p in present):
            return True
    return False


def _cliques(tree):
    """Groups of nodes that all exclude one another (alternatives), largest first; each node in one group at most."""
    nodes, groups, used = tree.nodes, [], set()
    for key in nodes:
        if key in used:
            continue
        group = [key]
        for other in nodes[key].excludes:
            if other in nodes and other not in used and all(other in nodes[g].excludes and g in nodes[other].excludes for g in group):
                group.append(other)
        if len(group) > 1:
            groups.append(group)
            used.update(group)
    return groups


def _label(node):
    text = node.label or node.key
    if node.tie_in:
        text += f' (tie-in: {node.tie_in})'
    return text.replace('"', '#quot;')


def mermaid(tree):
    """The tree as a Mermaid flowchart: alternatives boxed together, prerequisites as arrows, any-of links through a small joint (one
    joint for every link with the same parents and the same hold), holds on in bold, exclusions not implied by those as crossed lines,
    tie-in nodes dashed."""
    nodes = tree.nodes
    lines = ['flowchart TB']
    grouped = set()
    for index, group in enumerate(_cliques(tree), start=1):
        lines.append(f'  subgraph alt{index} ["Only one of these"]')
        lines.extend(f'    {key}["{_label(nodes[key])}"]' for key in group)
        lines.append('  end')
        grouped.update(group)
    lines.extend(f'  {key}["{_label(node)}"]' for key, node in nodes.items() if key not in grouped)
    for key, node in nodes.items():
        lines.extend(f'  {p} --> {key}' for p in node.prereqs if p in nodes)
    joints = {}
    for link in tree.any_of:
        parents = tuple(p for p in link.parents if p in nodes)
        if link.node in nodes and parents:
            joints.setdefault((parents, link.holds), []).append(link.node)
    for index, ((parents, holds), children) in enumerate(joints.items(), start=1):
        joint = f'any{index}'
        lines.append(f'  {joint}(("any one"))')
        lines.extend(f'  {p} -.- {joint}' for p in parents)
        lines.extend(f'  {joint} {"==>|holds it on|" if holds else "-->"} {child}' for child in children)
    drawn = set()
    in_group = {key: i for i, group in enumerate(_cliques(tree)) for key in group}
    for key, node in nodes.items():
        for other in node.excludes:
            pair = tuple(sorted((key, other)))
            if other not in nodes or pair in drawn:
                continue
            drawn.add(pair)
            if key in in_group and in_group.get(other) == in_group[key]:
                continue
            if _implied(tree, key, other) or _implied(tree, other, key):
                continue
            lines.append(f'  {key} x--x {other}')
    tie_ins = [key for key, node in nodes.items() if node.tie_in]
    if tie_ins:
        lines.append('  classDef tiein stroke-dasharray: 5 5')
        lines.append(f'  class {",".join(tie_ins)} tiein')
    return '\n'.join(lines)


def _rel(path):
    try:
        return path.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return path.as_posix()


def diagrams(docs):
    """Every diagram marker in docs/development: (path, defName, line index of the marker)."""
    found = []
    for path in sorted(docs.glob('*.md')):
        for index, line in enumerate(path.read_text(encoding='utf-8').split('\n')):
            match = MARKER.match(line)
            if match:
                found.append((path, match.group(1), index))
    return found


def _block(lines, marker_index):
    """The (start, end) line indices of the ```mermaid block after a marker, end exclusive, or None."""
    start = marker_index + 1
    if start >= len(lines) or lines[start].strip() != '```mermaid':
        return None
    for end in range(start + 1, len(lines)):
        if lines[end].strip() == '```':
            return start, end + 1
    return None


def check_diagrams(trees, docs, write):
    problems = []
    found = diagrams(docs)
    for name in sorted(set(trees) - {n for _, n, _ in found}):
        problems.append(f'{name}: no diagram in {_rel(docs)} (add "<!-- upgrade-tree: {name} -->" to its '
                        'design record and run with --write)')
    by_path = {}
    for path, name, index in found:
        by_path.setdefault(path, []).append((name, index))
    for path, entries in by_path.items():
        lines = path.read_text(encoding='utf-8').split('\n')
        changed = False
        for name, index in sorted(entries, key=lambda e: -e[1]):  # from the bottom, so earlier indices stay right
            rel = _rel(path)
            if name not in trees:
                problems.append(f'{rel}: a diagram names {name}, which is not one of our upgrade trees')
                continue
            wanted = ['```mermaid', *mermaid(trees[name]).split('\n'), '```']
            block = _block(lines, index)
            current = lines[block[0]:block[1]] if block else None
            if current == wanted:
                continue
            if write:
                start, end = block if block else (index + 1, index + 1)
                lines[start:end] = wanted
                changed = True
            else:
                problems.append(f'{rel}: the diagram of {name} is {"stale" if block else "missing under its marker"} '
                                '(run python scripts/check-upgrade-trees.py --write)')
        if changed:
            path.write_text('\n'.join(lines), encoding='utf-8', newline='')
            print(f'Wrote {_rel(path)}')
    return problems


def show(trees, pattern):
    for name in sorted(trees):
        if pattern and pattern.lower() not in name.lower():
            continue
        tree = trees[name]
        print(f'## {name}  ({tree.source})')
        for key, node in tree.nodes.items():
            parts = [f'{key} "{node.label}" at {node.coord or "?"}']
            if node.tie_in:
                parts.append(f'with {node.tie_in}')
            if node.prereqs:
                parts.append('needs all of ' + ', '.join(node.prereqs))
            for link in tree.any_of:
                if link.node == key:
                    parts.append('needs any one of ' + ', '.join(link.parents) + (' (holds it on)' if link.holds else ''))
            if node.excludes:
                parts.append('not with ' + ', '.join(node.excludes))
            print('- ' + '; '.join(parts))
        print()


def main(argv=None):
    parser = argparse.ArgumentParser(description='Check, print and draw our vehicles\' upgrade trees.')
    parser.add_argument('command', nargs='?', default='check', choices=['check', 'show'])
    parser.add_argument('pattern', nargs='?', help='show only trees whose defName contains this')
    parser.add_argument('--write', action='store_true', help='rewrite every diagram from its tree')
    parser.add_argument('--mods', default=str(ROOT / 'mods'))
    parser.add_argument('--docs', default=str(ROOT / 'docs' / 'development'))
    args = parser.parse_args(argv)
    trees = gather(Path(args.mods))
    if args.command == 'show':
        show(trees, args.pattern)
        return 0
    problems = [p for tree in trees.values() for p in check_tree(tree)]
    problems += check_diagrams(trees, Path(args.docs), args.write)
    for problem in problems:
        print(problem)
    print(f'{len(trees)} upgrade tree(s), {len(problems)} problem(s).')
    return 1 if problems else 0


if __name__ == '__main__':
    sys.exit(main())
