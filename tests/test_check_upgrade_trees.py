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
      <li><key>TankA</key><label>Tank A</label><gridCoordinate>(2, 2)</gridCoordinate>
        <disableIfUpgradeNodesEnabled><li>TankB</li></disableIfUpgradeNodesEnabled></li>
      <li><key>TankB</key><label>Tank B</label><gridCoordinate>(9, 2)</gridCoordinate>
        <disableIfUpgradeNodesEnabled><li>TankA</li></disableIfUpgradeNodesEnabled></li>
      <li><key>Bulkhead</key><label>Bulkhead</label><gridCoordinate>(5, 9)</gridCoordinate></li>
      <li><key>Second</key><label>Second bulkhead</label><gridCoordinate>(12, 9)</gridCoordinate>
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
      <li><key>Extra</key><label>Extra</label><gridCoordinate>(20, 2)</gridCoordinate>
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
            '<li><key>Bulkhead</key><label>Bulkhead</label><gridCoordinate>(5, 9)</gridCoordinate></li>',
            '<li><key>Bulkhead</key><label>Bulkhead</label><gridCoordinate>(5, 9)</gridCoordinate>'
            '<prerequisiteNodes><li>Second</li></prerequisiteNodes></li>'))
        self.assertTrue(any('loop' in p for p in self.problems()))

    def test_two_nodes_on_one_cell_fail(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace('(12, 9)', '(5, 9)'))
        self.assertTrue(any('share the grid cell' in p for p in self.problems()))

    def layout(self):
        return [p for tree in trees.gather(self.mods).values() for p in trees.check_layout(tree)]

    def test_a_sound_layout_passes(self):
        self.assertEqual([], self.layout())

    def test_a_node_below_the_tab_fails(self):
        # Row 24: centre 40 + 20 * 24 = 520, past the tab's 515 even for a 40-pixel node.
        self.write('TestMod/Defs/Tree.xml', TREE.replace('(12, 9)', '(12, 24)').replace('(5, 9)', '(5, 18)'))
        self.assertTrue(any('Second at (12,24) is drawn past the bottom' in p for p in self.layout()))

    def test_a_tree_named_in_may_scroll_may_run_past_the_tab(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace('(12, 9)', '(12, 24)').replace('(5, 9)', '(5, 18)'))
        trees.MAY_SCROLL['Test_Tree'] = 'a test'
        try:
            self.assertFalse(any('drawn past' in p for p in self.layout()))
        finally:
            del trees.MAY_SCROLL['Test_Tree']

    def test_a_first_row_below_the_top_fails(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace('(2, 2)', '(2, 6)').replace('(9, 2)', '(9, 6)'))
        self.assertTrue(any('its first row is row 6' in p for p in self.layout()))

    def test_a_tie_in_row_alone_is_not_the_first_row(self):
        # The tie-in node sits at row 2 with ours; moved below, our own first row still counts.
        self.write('TestMod/Mods/Other/Patches/Extra.xml', TIE_IN.replace('(20, 2)', '(20, 14)'))
        self.assertEqual([], self.layout())

    def test_nodes_with_no_gap_between_them_fail(self):
        # 40-pixel nodes two cells apart are 40 pixels between centres: touching, with no clear grid step.
        self.write('TestMod/Defs/Tree.xml', TREE.replace('(9, 2)', '(4, 2)'))
        self.assertTrue(any('TankA at (2,2) and TankB at (4,2) touch' in p for p in self.layout()))

    def test_a_drawn_size_counts(self):
        # Three cells apart, two 40-pixel nodes clear one grid step; with TankB drawn at 80 pixels they touch.
        self.write('TestMod/Defs/Tree.xml', TREE.replace('(9, 2)', '(5, 2)'))
        self.assertEqual([], self.layout())
        self.write('TestMod/Defs/Tree.xml', TREE.replace('<label>Tank B</label>', '<label>Tank B</label><drawSize>(80, 80)</drawSize>')
                   .replace('(9, 2)', '(5, 2)'))
        self.assertTrue(any('TankA at (2,2) and TankB at (5,2) touch' in p for p in self.layout()))

    def test_an_empty_band_of_rows_fails(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace('(5, 9)', '(5, 17)').replace('(12, 9)', '(12, 17)'))
        self.assertTrue(any('rows 2 and 17 have empty rows between them' in p for p in self.layout()))

    def pictures(self):
        known = trees.textures(self.mods)
        return [p for tree in trees.gather(self.mods).values() for p in trees.check_pictures(tree, known)]

    def picture(self, rel):
        path = self.mods / 'TestMod/Textures' / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b'')

    def test_an_icon_no_mod_ships_fails(self):
        # Vehicle Framework draws a missing icon as nothing and throws, so every node after it vanishes from the tab.
        icon = '<label>Tank A</label><icon>Phobos/Test/TankA</icon>'
        self.write('TestMod/Defs/Tree.xml', TREE.replace('<label>Tank A</label>', icon))
        self.assertTrue(any("TankA's icon Phobos/Test/TankA is not a picture" in p for p in self.pictures()))
        self.picture('Phobos/Test/TankA.png')
        self.assertEqual([], self.pictures())

    def test_an_overlay_needs_its_facings_or_a_plain_picture(self):
        overlay = ('<label>Tank B</label><graphicOverlays><li><graphicData><texPath>Phobos/Test/Body_Tank</texPath>'
                   '</graphicData></li></graphicOverlays>')
        self.write('TestMod/Defs/Tree.xml', TREE.replace('<label>Tank B</label>', overlay))
        self.assertTrue(any("TankB's overlay Phobos/Test/Body_Tank is not a picture" in p for p in self.pictures()))
        self.picture('Phobos/Test/Body_Tank_north.png')
        self.assertEqual([], self.pictures())

    def test_pictures_not_ours_are_not_checked(self):
        self.write('TestMod/Defs/Tree.xml', TREE.replace('<label>Tank A</label>', '<label>Tank A</label><icon>UI/Icons/Other</icon>'))
        self.assertEqual([], self.pictures())

    def test_the_shipped_trees_pass(self):
        mods = Path(ROOT) / 'mods'
        found = trees.gather(mods)
        known = trees.textures(mods)
        self.assertGreater(len(found), 0)
        self.assertEqual([], [p for tree in found.values()
                              for p in trees.check_tree(tree) + trees.check_layout(tree) + trees.check_pictures(tree, known)])


if __name__ == '__main__':
    unittest.main()
