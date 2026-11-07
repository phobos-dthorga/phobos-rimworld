"""Offline checks for scripts/check-vehicle-schemas.py: the shipped schemas pass, and each kind of mistake is caught.

Every mod below is synthetic; nothing reads or writes the RimWorld installation.
"""
import importlib.util
import os
import shutil
import tempfile
import unittest
from pathlib import Path

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
spec = importlib.util.spec_from_file_location('check_vehicle_schemas', os.path.join(ROOT, 'scripts/check-vehicle-schemas.py'))
schemas = importlib.util.module_from_spec(spec)
spec.loader.exec_module(schemas)

SYSTEM = """<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Phobos.VehicleSystems.PhobosVehicleSystemDef>
    <defName>Test_System</defName>
    <roles><li>Engine</li></roles>
    <parameters><rate>2</rate></parameters>
    <quantities>
      <li>
        <name>heat</name>
        <stages>
          <li><to>10</to><label>cool</label></li>
          <li><from>10</from><label>hot</label>
            <effects><li Class="Phobos.VehicleSystems.Effect_Message"><message>Test_Hot</message></li></effects></li>
        </stages>
      </li>
    </quantities>
    <flows>
      <li><name>warm</name><quantity>heat</quantity><kind>Into</kind><factorParameter>rate</factorParameter>
        <inputs><li Class="Phobos.VehicleSystems.Input_PartEfficiency"><role>Engine</role></li></inputs></li>
    </flows>
  </Phobos.VehicleSystems.PhobosVehicleSystemDef>
  <Phobos.VehicleSystems.PhobosVehicleLoadoutDef Name="Test_Base" Abstract="True">
    <systems><li><def>Test_System</def></li></systems>
    <partRoles><li><role>Engine</role><add><key>TestEngine</key><health>10</health></add></li></partRoles>
  </Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
  <Phobos.VehicleSystems.PhobosVehicleLoadoutDef ParentName="Test_Base">
    <defName>Test_Loadout</defName>
    <systems Inherit="False"><li><def>Test_System</def><set><rate>3</rate></set></li></systems>
  </Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
</Defs>
"""

KEYED = """<?xml version="1.0" encoding="utf-8"?>
<LanguageData><Test_Hot>Hot.</Test_Hot></LanguageData>
"""


class VehicleSchemaCheckTests(unittest.TestCase):
    def setUp(self):
        self.root = Path(tempfile.mkdtemp())
        schema_source = self.root / schemas.SCHEMA_SOURCE
        schema_source.mkdir(parents=True)
        for name in ('Inputs.cs', 'Effects.cs', 'Conditions.cs'):
            shutil.copy(Path(ROOT, schemas.SCHEMA_SOURCE, name), schema_source / name)
        self.defs = self.root / 'mods/Test/Common/Defs/Test.xml'
        self.defs.parent.mkdir(parents=True)
        keyed = self.root / 'mods/Test/Common/Languages/English/Keyed/Test.xml'
        keyed.parent.mkdir(parents=True)
        keyed.write_text(KEYED, encoding='utf-8')

    def tearDown(self):
        shutil.rmtree(self.root, ignore_errors=True)

    def run_with(self, xml):
        self.defs.write_text(xml, encoding='utf-8')
        return schemas.check(self.root)

    def test_the_shipped_schemas_pass(self):
        result = schemas.check(ROOT)
        self.assertEqual(result['errors'], [])
        self.assertIn('PhobosVehicleSystem_Electrical', result['systems'])
        self.assertIn('PhobosVehicleLoadout_LandLight', result['loadouts'])

    def test_a_good_synthetic_schema_passes_and_inherits(self):
        result = self.run_with(SYSTEM)
        self.assertEqual(result['errors'], [])
        self.assertEqual(result['loadouts'], ['Test_Loadout'])

    def test_a_flow_naming_no_quantity_is_caught(self):
        result = self.run_with(SYSTEM.replace('<quantity>heat</quantity><kind>Into</kind>', '<quantity>heet</quantity><kind>Into</kind>'))
        self.assertTrue(any('names no quantity' in e for e in result['errors']))

    def test_an_undeclared_parameter_is_caught(self):
        result = self.run_with(SYSTEM.replace('<factorParameter>rate</factorParameter>', '<factorParameter>speed</factorParameter>'))
        self.assertTrue(any('undeclared parameter speed' in e for e in result['errors']))

    def test_stages_out_of_order_are_caught(self):
        result = self.run_with(SYSTEM.replace('<li><to>10</to>', '<li><to>20</to>'))
        self.assertTrue(any('out of order' in e for e in result['errors']))

    def test_a_missing_translation_key_is_caught(self):
        result = self.run_with(SYSTEM.replace('<message>Test_Hot</message>', '<message>Test_Cold</message>'))
        self.assertTrue(any('Test_Cold is missing' in e for e in result['errors']))

    def test_an_unknown_class_of_ours_is_caught_but_another_mods_is_trusted(self):
        result = self.run_with(SYSTEM.replace('Effect_Message', 'Effect_Massage'))
        self.assertTrue(any('no class Phobos.VehicleSystems.Effect_Massage' in e for e in result['errors']))
        result = self.run_with(SYSTEM.replace('Phobos.VehicleSystems.Effect_Message', 'OtherMod.Effect_Beacon'))
        self.assertFalse(any('Beacon' in e for e in result['errors']))

    def test_a_loadout_overriding_an_unknown_parameter_is_caught(self):
        result = self.run_with(SYSTEM.replace('<set><rate>3</rate></set>', '<set><rates>3</rates></set>'))
        self.assertTrue(any('has no parameter rates' in e for e in result['errors']))

    def test_a_loadout_naming_no_system_is_caught(self):
        result = self.run_with(SYSTEM.replace('<li><def>Test_System</def><set>', '<li><def>Test_Sistem</def><set>'))
        self.assertTrue(any('no system Test_Sistem' in e for e in result['errors']))

    def test_an_unbound_role_is_a_warning(self):
        result = self.run_with(SYSTEM.replace('<partRoles><li><role>Engine</role><add><key>TestEngine</key><health>10</health></add></li></partRoles>', ''))
        self.assertEqual(result['errors'], [])
        self.assertTrue(any('binds to nothing' in w for w in result['warnings']))


if __name__ == '__main__':
    unittest.main()
