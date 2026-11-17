"""A patch of ours that adds to a node of one of our own Defs must find that node in the Def as written.

The game applies patches before it merges a Def with its parent, so ``Defs/ThingDef[defName="X"]/comps`` finds nothing when X takes
its comps from a parent and writes no ``<comps>`` of its own. The patch then fails with an error at start-up and what it added is
missing: Billy's kettle took no water with Dubs Bad Hygiene for this reason (the owner's log, 18 Nov 2026; fixed in D'thorga
Vehicles 0.65.3 by adding the whole node to the Def). This test reads every patch file in ``mods/`` and, for each xpath of the plain
form ``Defs/Type[defName="X"]/child...`` whose X is a Def of ours, checks that X writes ``child`` itself, or that an earlier
operation in the same file adds it. Other authors' Defs are not checked: their files are not in this repository.
"""
import re
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
XPATH = re.compile(r'^/?Defs/([\w.]+)\[defName\s*=\s*"([^"]+)"\]/(\w+)')
WHOLE = re.compile(r'^/?Defs/([\w.]+)\[defName\s*=\s*"([^"]+)"\]$')


def own_defs():
    """defName -> the names of the child nodes each of our Defs writes itself (every definition of the name, merged)."""
    found = {}
    for path in (ROOT / 'mods').rglob('*.xml'):
        if 'Defs' not in path.parts:
            continue
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        if root.tag != 'Defs':
            continue
        for node in root:
            name = node.findtext('defName')
            if name:
                found.setdefault(name.strip(), set()).update(child.tag for child in node)
    return found


def problems():
    defs = own_defs()
    out = []
    for path in (ROOT / 'mods').rglob('*.xml'):
        if 'Patches' not in path.parts:
            continue
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        if root.tag != 'Patch':
            continue
        added = {}
        for operation in root.iter():
            xpath = (operation.findtext('xpath') or '').strip()
            if not xpath:
                continue
            whole = WHOLE.match(xpath)
            if whole:
                # An operation on the Def itself: what its value holds exists for the operations after it.
                value = operation.find('value')
                if value is not None:
                    added.setdefault(whole.group(2), set()).update(child.tag for child in value)
                continue
            match = XPATH.match(xpath)
            if not match:
                continue
            _, name, child = match.groups()
            if name not in defs:
                continue
            if child in defs[name] or child in added.get(name, set()):
                continue
            out.append(f'{path.relative_to(ROOT).as_posix()}: {xpath} names <{child}>, which {name} does not write itself')
    return out


class PatchesNameOwnNodes(unittest.TestCase):
    def test_every_patch_on_our_own_def_finds_its_node(self):
        self.assertEqual([], problems(),
                         'A patch adds to a node the Def takes from its parent: the game patches before it inherits, so the patch fails. '
                         'Add the whole node to the Def instead (xpath ending at the Def, value holding the node).')


if __name__ == '__main__':
    unittest.main()
