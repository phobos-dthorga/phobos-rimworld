"""Offline checks for scripts/check-upgrade-trees.py: the shipped trees pass, each kind of mistake is caught, and diagrams are written
and then held current. Every mod and record below is synthetic; nothing reads or writes the RimWorld installation.
"""
import importlib.util
import os
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
spec = importlib.util.spec_from_file_location('check_upgrade_trees', os.path.join(ROOT, 'scripts/check-upgrade-trees.py'))
trees = importlib.util.module_from_spec(spec)
spec.loader.exec_module(trees)

TREE = """<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Vehicles.UpgradeTreeDef>
    <defName>Test_Tree</defName>
    <modExtensions>
      <li Class="Phobos.VehicleSystems.PhobosUpgradeAnyOf">
        <links>
          <li><node>Bulkhead</node><holdsParent>true</holdsParent><anyOf><li>TankA</li><li>TankB</li></anyOf></li>
        </links>
      </li>
    </modExtensions>
    <nodes>
      <li><key>TankA</key><label>Tank A</label><gridCoordinate>(2, 6)</gridCoordinate>
        <disableIfUpgradeNodesEnabled><li>TankB</li></disableIfUpgradeNodesEnabled></li>
      <li><key>TankB</key><label>Tank B</label><gridCoordinate>(9, 6)</gridCoordinate>
        <disableIfUpgradeNodesEnabled><li>TankA</li></disableIfUpgradeNodesEnabled></li>
      <li><key>Bulkhead</key><label>Bulkhead</label><gridCoordinate>(5, 15)</gridCoordinate></li>
      <li><key>Second</key><label>Second bulkhead</label><gridCoordinate>(12, 15)</gridCoordinate>
        <prerequisiteNodes><li>Bulkhead</li></prerequisiteNodes></li>
    </nodes>
  </Vehicles.UpgradeTreeDef>
</Defs>
"""

TIE_IN = """<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="PatchOperationAdd">
    <xpath>Defs/Vehicles.UpgradeTreeDef[defName="Test_Tree"]/nodes</xpath>
    <value>
      <li><key>Extra</key><label>Extra</label><gridCoordinate>(20, 6)</gridCoordinate>
        <prerequisiteNodes><li>TankA</li></prerequisiteNodes></li>
    </value>
  </Operation>
</Patch>
"""


class CheckUpgradeTreesTests(unittest.TestCase):
    def setUp(self):
        self.temp = Path(tempfile.mkdtemp())
        self.mods = self.temp / 'mods'
        self.docs = self.temp / 'docs'
        self.docs.mkdir()
        self.write('TestMod/Defs/Tree.xml', TREE)
        self.write('TestMod/Mods/Other/Patches/Extra.xml', TIE_IN)
        (self.docs / 'record.md').write_text('# Record\n\n<!-- upgrade-tree: Test_Tree -->\n', encoding='utf-8')

    def tearDown(self):
        shutil.rmtree(self.temp, ignore_errors=True)

    def write(self, rel, text):
        path = self.mods / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding='utf-8')

    def problems(self, write=False):
        found = trees.gather(self.mods)
        out = [p for tree in found.values() for p in trees.check_tree(tree)]
        return out + trees.check_diagrams(found, self.docs, write)

    def test_a_sound_tree_passes_once_its_diagram_is_written(self):
        self.assertTrue(any('missing under its marker' in p for p in self.problems()))
        self.problems(write=True)
        self.assertEqual([], self.problems())
        text = (self.docs / 'record.md').read_text(encoding='utf-8')
        self.assertIn('subgraph alt1 ["Only one of these"]', text)
        self.assertIn('==>|holds it on| Bulkhead', text)
        self.assertIn('Extra["Extra (tie-in: Other)"]', text)

    def test_a_changed_tree_makes_its_diagram_stale(self):
        self.problems(write=True)
        self.write('TestMod/Defs/Tree.xml', TREE.replace('Second bulkhead', 'Third bulkhead'))
        self.assertTrue(any('is stale' in p for p in self.problems()))

    def test_a_tree_without_a_diagram_fails(self):
        (self.docs / 'record.md').write_text('# Record\n', encoding='utf-8')
        self.assertTrue(any('no diagram' in p for p in self.problems()))

    def test_a_one_sided_exclusion_fails(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace(
            '<disableIfUpgradeNodesEnabled><li>TankA</li></disableIfUpgradeNodesEnabled>', ''))
        self.assertTrue(any('write the exclusion on both' in p for p in self.problems()))

    def test_a_node_of_ours_needing_a_tie_in_node_fails(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace('<prerequisiteNodes><li>Bulkhead</li>', '<prerequisiteNodes><li>Extra</li>'))
        self.assertTrue(any('a tie-in node' in p for p in self.problems()))

    def test_a_missing_key_fails(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace('<li>TankB</li></anyOf>', '<li>TankC</li></anyOf>'))
        self.assertTrue(any('names TankC' in p for p in self.problems()))

    def test_a_loop_of_prerequisites_fails(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace(
            '<li><key>Bulkhead</key><label>Bulkhead</label><gridCoordinate>(5, 15)</gridCoordinate></li>',
            '<li><key>Bulkhead</key><label>Bulkhead</label><gridCoordinate>(5, 15)</gridCoordinate>'
            '<prerequisiteNodes><li>Second</li></prerequisiteNodes></li>'))
        self.assertTrue(any('loop' in p for p in self.problems()))

    def test_two_nodes_on_one_cell_fail(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace('(12, 15)', '(5, 15)'))
        self.assertTrue(any('share the grid cell' in p for p in self.problems()))

    def test_the_shipped_trees_pass(self):
        found = trees.gather(Path(ROOT) / 'mods')
        self.assertGreater(len(found), 0)
        self.assertEqual([], [p for tree in found.values() for p in trees.check_tree(tree)])


if __name__ == '__main__':
    unittest.main()
