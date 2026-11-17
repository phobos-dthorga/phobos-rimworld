"""Every upgrade node of ours names itself with a capital letter.

Vehicle Framework's upgrade tab shows a node's label exactly as written, where the game itself capitalises a Def's label
before showing it, and other vehicle mods (Vanilla Vehicles Expanded, GTek's) write their node labels capitalised. Ours
were all lower case until the owner spotted "bigger water tank" heading its tooltip (17 Nov 2026, D'thorga Vehicles
0.61.1). This reads every upgrade tree and every patch that adds nodes, and refuses a node label that starts lower case.
A crew role inside a node (a key and label with no description) stays lower case, as the game's roles do.
"""
import re
import unittest
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def node_lists(root):
    """The <nodes> lists in a Defs file's upgrade trees, or the <value> of a patch that adds to one."""
    if root.tag == 'Defs':
        for tree in root:
            if tree.tag.endswith('UpgradeTreeDef'):
                nodes = tree.find('nodes')
                if nodes is not None:
                    yield nodes
    elif root.tag == 'Patch':
        for operation in root.iter('Operation'):
            xpath = (operation.findtext('xpath') or '').strip()
            value = operation.find('value')
            if value is not None and 'UpgradeTreeDef' in xpath and xpath.endswith('/nodes'):
                yield value


def offences(root):
    found = []
    for nodes in node_lists(root):
        for node in nodes.findall('li'):
            label = (node.findtext('label') or '').strip()
            if label and label[0].islower():
                found.append(f"{(node.findtext('key') or '?').strip()}: {label}")
    return found


class UpgradeLabelsCapitalised(unittest.TestCase):
    def test_every_upgrade_node_label_starts_with_a_capital(self):
        problems = []
        for path in sorted((ROOT / 'mods').rglob('*.xml')):
            if 'Languages' in path.parts:
                continue
            try:
                root = ET.parse(path).getroot()
            except ET.ParseError:
                continue
            problems += [f'{path.relative_to(ROOT).as_posix()}: {o}' for o in offences(root)]
        self.assertEqual([], problems, 'Vehicle Framework shows an upgrade name as written; capitalise it:\n' + '\n'.join(problems))

    def test_the_check_sees_the_fault_it_was_written_for(self):
        tree = ET.fromstring('<Defs><Vehicles.UpgradeTreeDef><nodes><li><key>K</key><label>bigger water tank</label>'
                             '<description>d</description><upgrades><li><roles><li><key>r</key><label>scientist</label>'
                             '</li></roles></li></upgrades></li></nodes></Vehicles.UpgradeTreeDef></Defs>')
        self.assertEqual(['K: bigger water tank'], offences(tree))
        patch = ET.fromstring('<Patch><Operation Class="PatchOperationAdd"><xpath>Defs/Vehicles.UpgradeTreeDef[defName="X"]/nodes'
                              '</xpath><value><li><key>W</key><label>wash-up basin</label></li></value></Operation></Patch>')
        self.assertEqual(['W: wash-up basin'], offences(patch))


if __name__ == '__main__':
    unittest.main()
