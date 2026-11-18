"""Offline checks for scripts/check-vehicle-armour.py: the shipped vehicles pass, and each kind of mistake is caught. Every mod below is
synthetic, written to a temporary folder; nothing reads or writes the RimWorld installation.
"""
import importlib.util
import os
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
spec = importlib.util.spec_from_file_location('check_vehicle_armour', os.path.join(ROOT, 'scripts/check-vehicle-armour.py'))
armour = importlib.util.module_from_spec(spec)
spec.loader.exec_module(armour)

CLASSES = """<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Phobos.VehicleSystems.PhobosArmourClassDef>
    <defName>PhobosArmour_Skin</defName><sharpMillimetres>2</sharpMillimetres><bluntMegapascals>4</bluntMegapascals>
    <sharpRating>0.10</sharpRating><bluntRating>0.08</bluntRating>
  </Phobos.VehicleSystems.PhobosArmourClassDef>
  <Phobos.VehicleSystems.PhobosArmourClassDef>
    <defName>PhobosArmour_Rifle</defName><sharpMillimetres>8</sharpMillimetres><bluntMegapascals>40</bluntMegapascals>
    <sharpRating>0.45</sharpRating><bluntRating>0.35</bluntRating>
  </Phobos.VehicleSystems.PhobosArmourClassDef>
</Defs>
"""

SKIN = '<armor><ArmorRating_Sharp>0.10</ArmorRating_Sharp><ArmorRating_Blunt>0.08</ArmorRating_Blunt></armor>'


def vehicle(parts, size='(1, 2)', marked=True):
    extension = '<modExtensions><li Class="Phobos.VehicleSystems.PhobosArmourExtension" /></modExtensions>' if marked else ''
    return f"""<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Vehicles.VehicleDef>
    <defName>Test_Truck</defName>
    <size>{size}</size>
    {extension}
    <components>{parts}</components>
  </Vehicles.VehicleDef>
</Defs>
"""


def part(key, depth, cells, armor=SKIN):
    cell_xml = ''.join(f'<li>{c}</li>' for c in cells)
    return f'<li><key>{key}</key><depth>{depth}</depth>{armor}<hitbox><cells>{cell_xml}</cells></hitbox></li>'


GOOD_PARTS = part('Cab', 'External', ['(0, 1)']) + part('Body', 'External', ['(0, 0)']) + part('Engine', 'Internal', ['(0, 1)'])

CE_PATCH = """<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="PatchOperationReplace">
    <xpath>Defs/Vehicles.VehicleDef[defName="Test_Truck"]/components/li/armor</xpath>
    <value><armor><ArmorRating_Sharp>2</ArmorRating_Sharp><ArmorRating_Blunt>4</ArmorRating_Blunt></armor></value>
  </Operation>
</Patch>
"""


class VehicleArmourTests(unittest.TestCase):
    def setUp(self):
        self.root = Path(tempfile.mkdtemp(prefix='phobos-armour-'))
        classes = self.root / armour.CLASSES_FILE
        classes.parent.mkdir(parents=True)
        classes.write_text(CLASSES, encoding='utf-8')
        (self.root / 'mods/TestMod/Defs').mkdir(parents=True)
        (self.root / 'mods/TestMod/Mods/CombatExtended/Patches').mkdir(parents=True)

    def tearDown(self):
        shutil.rmtree(self.root, ignore_errors=True)

    def write(self, defs, patch=CE_PATCH, tree=None):
        (self.root / 'mods/TestMod/Defs/Truck.xml').write_text(defs, encoding='utf-8')
        if patch is not None:
            (self.root / 'mods/TestMod/Mods/CombatExtended/Patches/Truck_Armour.xml').write_text(patch, encoding='utf-8')
        if tree is not None:
            (self.root / 'mods/TestMod/Defs/Tree.xml').write_text(tree, encoding='utf-8')
        problems, _ = armour.run(self.root)
        return problems

    def test_the_shipped_vehicles_pass(self):
        problems, vehicles = armour.run(Path(ROOT))
        self.assertEqual([], problems)
        self.assertGreaterEqual(len(vehicles), 5)
        # The Osprey's armour kit is read, with its Combat Extended twin, so its check is real.
        kits = armour.load_kits(Path(ROOT), {'Phobos_Osprey_UpgradeTree'})['Phobos_Osprey_UpgradeTree']
        self.assertIn('ArmourKit', kits)
        self.assertIsNotNone(kits['ArmourKit'].ce_armour)

    def test_a_good_truck_passes(self):
        self.assertEqual([], self.write(vehicle(GOOD_PARTS)))

    def test_an_unmarked_vehicle_is_not_checked(self):
        unarmoured = vehicle(part('Cab', 'External', ['(0, 1)'], armor=''), marked=False)
        self.assertEqual([], self.write(unarmoured, patch=None))

    def test_a_part_with_no_armour_fails(self):
        parts = part('Cab', 'External', ['(0, 1)'], armor='') + part('Body', 'External', ['(0, 0)'])
        self.assertTrue(any('no armour of its own' in p for p in self.write(vehicle(parts))))

    def test_a_rating_that_is_no_class_fails(self):
        odd = '<armor><ArmorRating_Sharp>0.40</ArmorRating_Sharp><ArmorRating_Blunt>0.26</ArmorRating_Blunt></armor>'
        parts = part('Cab', 'External', ['(0, 1)'], armor=odd) + part('Body', 'External', ['(0, 0)'])
        self.assertTrue(any("no class's rating" in p for p in self.write(vehicle(parts))))

    def test_no_combat_extended_patch_fails(self):
        self.assertTrue(any('would read 0.1 mm' in p for p in self.write(vehicle(GOOD_PARTS), patch=None)))

    def test_a_patch_of_another_class_fails(self):
        rifle = CE_PATCH.replace('>2<', '>8<').replace('>4<', '>40<')
        self.assertTrue(any('Skin in the Def but 8.0 mm' in p for p in self.write(vehicle(GOOD_PARTS), patch=rifle)))

    def test_a_patch_for_one_part_counts_for_that_part_only(self):
        one = CE_PATCH.replace('components/li/armor', 'components/li[key="Cab"]/armor')
        problems = self.write(vehicle(GOOD_PARTS), patch=one)
        self.assertTrue(any('part Body has no Combat Extended armour' in p for p in problems))
        self.assertFalse(any('part Cab' in p for p in problems))

    def test_two_outer_parts_on_a_cell_fail(self):
        parts = GOOD_PARTS + part('Lights', 'External', ['(0, 1)'])
        self.assertTrue(any('cell (0, 1) has 2 outer parts (Cab, Lights)' in p for p in self.write(vehicle(parts))))

    def test_a_cell_with_no_outer_part_fails(self):
        parts = part('Cab', 'External', ['(0, 1)']) + part('Body', 'Internal', ['(0, 0)'])
        self.assertTrue(any('cell (0, 0) has no outer part' in p for p in self.write(vehicle(parts))))

    def test_a_part_with_no_hitbox_covers_the_whole_footprint(self):
        whole = f'<li><key>Chassis</key><depth>External</depth>{SKIN}</li>'
        problems = self.write(vehicle(GOOD_PARTS + whole))
        self.assertTrue(any('(Cab, Chassis)' in p for p in problems))

    def test_a_cell_outside_the_footprint_fails(self):
        parts = GOOD_PARTS + part('Mirror', 'Internal', ['(3, 0)'])
        self.assertTrue(any('outside its 1 x 2 footprint' in p for p in self.write(vehicle(parts))))

    def tree(self, upgrade, explained=False):
        explanation = '<upgradeExplanation>Fitted later.</upgradeExplanation>' if explained else ''
        return f"""<?xml version="1.0" encoding="utf-8"?>
<Defs><Vehicles.UpgradeTreeDef><defName>Test_Tree</defName><nodes>
  <li><key>Kit</key>{explanation}<upgrades>{upgrade}</upgrades></li>
</nodes></Vehicles.UpgradeTreeDef></Defs>
"""

    ARMOUR_SET = ('<li Class="Vehicles.VehicleUpgrade"><armor><li><key>Cab</key><type>Set</type>'
                  '<statModifiers><ArmorRating_Sharp>0.45</ArmorRating_Sharp></statModifiers></li></armor></li>')

    def test_armour_of_type_set_with_no_explanation_fails(self):
        problems = self.write(vehicle(GOOD_PARTS), tree=self.tree(self.ARMOUR_SET))
        self.assertTrue(any('node Kit: armour of type Set' in p for p in problems))

    def test_armour_of_type_set_with_an_explanation_passes(self):
        self.assertEqual([], self.write(vehicle(GOOD_PARTS), tree=self.tree(self.ARMOUR_SET, explained=True)))

    def test_a_stat_of_type_set_fails(self):
        stat = ('<li Class="Vehicles.StatUpgrade"><vehicleStats><li><def>CargoCapacity</def><value>3600</value><type>Set</type></li>'
                '</vehicleStats></li>')
        self.assertTrue(any('a stat of type Set does nothing' in p for p in self.write(vehicle(GOOD_PARTS), tree=self.tree(stat))))

    # Kits on an armoured vehicle: the truck carries a tree, and each kit must add by class on both scales.
    KIT_TREE = """<?xml version="1.0" encoding="utf-8"?>
<Defs><Vehicles.UpgradeTreeDef><defName>Test_TruckTree</defName><nodes>
  <li><key>Cab</key><upgrades><li Class="Vehicles.VehicleUpgrade"><armor><li><key>Cab</key><type>{kind}</type>
    <statModifiers><ArmorRating_Sharp>{sharp}</ArmorRating_Sharp><ArmorRating_Blunt>{blunt}</ArmorRating_Blunt></statModifiers></li></armor></li></upgrades></li>
</nodes></Vehicles.UpgradeTreeDef></Defs>
"""
    KIT_PATCH = """
  <Operation Class="PatchOperationReplace">
    <xpath>Defs/Vehicles.UpgradeTreeDef[defName="Test_TruckTree"]/nodes/li[key="Cab"]/upgrades/li[@Class="Vehicles.VehicleUpgrade"]/armor</xpath>
    <value><armor><li><key>Cab</key><type>Add</type><statModifiers><ArmorRating_Sharp>{sharp}</ArmorRating_Sharp>
      <ArmorRating_Blunt>{blunt}</ArmorRating_Blunt></statModifiers></li></armor></value>
  </Operation>
</Patch>
"""

    def kit(self, kind='Add', sharp='0.35', blunt='0.27', ce=('6', '36')):
        defs = vehicle(GOOD_PARTS).replace('<components>', '<comps><li Class="Vehicles.CompProperties_UpgradeTree"><def>Test_TruckTree</def></li>'
                                            '</comps><components>')
        patch = CE_PATCH if ce is None else CE_PATCH.replace('</Patch>\n', self.KIT_PATCH.format(sharp=ce[0], blunt=ce[1]))
        return self.write(defs, patch=patch, tree=self.KIT_TREE.format(kind=kind, sharp=sharp, blunt=blunt))

    def test_a_kit_from_skin_to_rifle_passes(self):
        self.assertEqual([], self.kit())

    def test_a_kit_that_lands_on_no_class_fails(self):
        self.assertTrue(any("is no class's rating" in p for p in self.kit(sharp='0.30')))

    def test_a_kit_with_no_combat_extended_twin_fails(self):
        self.assertTrue(any('no Combat Extended figures for Cab' in p for p in self.kit(ce=None)))

    def test_a_kit_whose_twin_is_another_class_fails(self):
        self.assertTrue(any('rifle' not in p and 'Combat Extended; PhobosArmour_Rifle is 8 / 40' in p for p in self.kit(ce=('3', '36'))))

    def test_a_kit_that_sets_on_an_armoured_vehicle_fails(self):
        problems = self.kit(kind='Set')
        self.assertTrue(any('must be of type Add' in p for p in problems))

    def test_an_add_passes(self):
        add = ('<li Class="Vehicles.VehicleUpgrade"><armor><li><key>Cab</key><type>Add</type>'
               '<statModifiers><ArmorRating_Sharp>0.35</ArmorRating_Sharp></statModifiers></li></armor></li>')
        self.assertEqual([], self.write(vehicle(GOOD_PARTS), tree=self.tree(add)))


if __name__ == '__main__':
    unittest.main()
