"""Offline checks of the shipped parts bills (docs/development/parts-bills.md).

The game reports a bill that names a role nothing supplies, or a node its tree does not have, only as a config error in its log.
These read the mods' own XML and say so before a commit. Nothing reads or writes the RimWorld installation.
"""
import os
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MODS = ROOT / 'mods'

SUPPLY_TYPES = ('PhobosRoleSupplyDef', 'PhobosVehiclePartSupplyDef')
BILL_TYPES = ('PhobosBillDef', 'PhobosVehicleBillDef')


def _kind(tag, kinds):
    return tag.split('.')[-1] in kinds


class Shipped:
    """Every Def name, role supply, bill and upgrade-node key in the mods' XML."""

    def __init__(self):
        self.def_names = set()
        self.supplies = []      # (file, element)
        self.bills = []         # (file, element)
        self.node_keys = set()
        for path in sorted(MODS.rglob('*.xml')):
            if 'Languages' in path.parts or 'About' in path.parts:
                continue
            try:
                root = ET.parse(path).getroot()
            except ET.ParseError:
                continue    # another check reports XML that does not parse
            for element in root.iter():
                name = element.findtext('defName')
                if name:
                    self.def_names.add(name.strip())
                if _kind(element.tag, SUPPLY_TYPES):
                    self.supplies.append((path, element))
                elif _kind(element.tag, BILL_TYPES):
                    self.bills.append((path, element))
                if element.tag == 'nodes':
                    for node in element:
                        key = node.findtext('key')
                        if key:
                            self.node_keys.add(key.strip())

    def roles(self):
        found = set()
        for _, supply in self.supplies:
            for role in supply.findall('roles/li'):
                found.add((role.text or '').strip())
        return found


class PartsBillsTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.shipped = Shipped()

    def where(self, path, element):
        return '%s in %s' % (element.findtext('defName'), path.relative_to(ROOT).as_posix())

    def test_there_are_bills_and_supplies(self):
        self.assertTrue(self.shipped.bills, 'no parts bill was found: has the Def type been renamed?')
        self.assertTrue(self.shipped.supplies, 'no role supply was found: has the Def type been renamed?')

    def test_every_role_a_bill_asks_for_is_supplied(self):
        roles = self.shipped.roles()
        for path, bill in self.shipped.bills:
            lists = [bill.find('parts')] + [node.find('parts') for node in bill.findall('nodes/li')]
            for parts in lists:
                if parts is None:
                    continue
                for part in parts:
                    self.assertIn(part.tag, roles, '%s asks for the role %s, which no role supply fills' % (self.where(path, bill), part.tag))
                    self.assertGreater(int((part.text or '0').strip()), 0, '%s asks for none of the role %s' % (self.where(path, bill), part.tag))

    def test_every_bill_has_a_target_that_exists(self):
        for path, bill in self.shipped.bills:
            target = (bill.findtext('target') or '').strip()
            self.assertTrue(target, '%s names no target' % self.where(path, bill))
            self.assertIn(target, self.shipped.def_names, '%s: its target %s is not a Def of ours' % (self.where(path, bill), target))

    def test_no_two_bills_share_a_target(self):
        seen = {}
        for path, bill in self.shipped.bills:
            target = (bill.findtext('target') or '').strip()
            self.assertNotIn(target, seen, '%s and %s both bill %s' % (self.where(path, bill), seen.get(target), target))
            seen[target] = self.where(path, bill)

    def test_every_node_a_bill_names_exists(self):
        for path, bill in self.shipped.bills:
            for node in bill.findall('nodes/li'):
                key = (node.findtext('key') or '').strip()
                self.assertIn(key, self.shipped.node_keys, '%s lists the node %s, which no upgrade tree of ours has' % (self.where(path, bill), key))

    def test_our_own_items_exist(self):
        for path, supply in self.shipped.supplies:
            item = (supply.findtext('item') or '').strip()
            if item:
                self.assertIn(item, self.shipped.def_names, '%s: its item %s is not a Def of ours' % (self.where(path, supply), item))

    def test_another_mods_part_is_always_behind_mayrequire(self):
        """A substitute without MayRequire is loaded for every player; if it names another mod's item, a player without that mod gets an error."""
        for path, supply in self.shipped.supplies:
            for substitute in supply.findall('substitutes/li'):
                thing = (substitute.findtext('thing') or '').strip()
                self.assertTrue(thing, '%s has a substitute with no thing' % self.where(path, supply))
                if substitute.get('MayRequire') or substitute.get('MayRequireAnyOf'):
                    continue
                self.assertIn(thing, self.shipped.def_names,
                              '%s offers %s with no MayRequire, and it is not a Def of ours' % (self.where(path, supply), thing))


if __name__ == '__main__':
    unittest.main()
