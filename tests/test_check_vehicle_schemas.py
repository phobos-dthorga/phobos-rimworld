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

    def test_a_good_drivetrain_and_flight_profile_pass(self):
        result = self.run_with(POWER)
        self.assertEqual(result['errors'], [])
        self.assertEqual(result['drivetrains'], ['Test_Diesel', 'Test_Jet'])
        self.assertEqual(result['profiles'], ['Test_Profile'])

    def test_gears_that_do_not_fall_and_crossed_shift_points_are_caught(self):
        result = self.run_with(POWER.replace('<li>3.5</li><li>2.0</li>', '<li>2.0</li><li>3.5</li>')
                               .replace('<upshiftRpm>2400</upshiftRpm>', '<upshiftRpm>1000</upshiftRpm>'))
        self.assertTrue(any('gears must fall' in e for e in result['errors']))
        self.assertTrue(any('upshift rpm' in e for e in result['errors']))

    def test_a_turbine_ladder_out_of_order_or_without_time_limits_is_caught(self):
        result = self.run_with(POWER.replace('<share>1.3</share>', '<share>0.9</share>').replace('<minutes>5</minutes>', ''))
        self.assertTrue(any('must rise in share' in e for e in result['errors']))
        self.assertTrue(any('needs a time limit' in e for e in result['errors']))

    def test_a_wet_rating_without_water_injection_is_caught(self):
        result = self.run_with(POWER.replace('<waterInjection><tankKilograms>100</tankKilograms></waterInjection>', ''))
        self.assertTrue(any('go together' in e for e in result['errors']))

    def test_a_reader_naming_an_unlisted_phase_is_caught(self):
        result = self.run_with(POWER.replace('<rules><li><phase>Cruise</phase>', '<rules><li><phase>Hover</phase>'))
        self.assertTrue(any('names the phase Hover' in e for e in result['errors']))

    def test_a_loadout_naming_no_drivetrain_or_profile_is_caught(self):
        result = self.run_with(POWER.replace('<def>Test_Jet</def>', '<def>Test_Jit</def>').replace(
            '<flightProfile>Test_Profile</flightProfile>', '<flightProfile>Test_Profil</flightProfile>'))
        self.assertTrue(any('no drivetrain Test_Jit' in e for e in result['errors']))
        self.assertTrue(any('no flight profile Test_Profil' in e for e in result['errors']))

    def test_water_injection_needs_a_system_holding_the_water(self):
        result = self.run_with(POWER.replace('<name>injectionWater</name>', '<name>tankWater</name>'))
        self.assertTrue(any('injectionWater quantity' in e for e in result['errors']))

    def test_thrusters_without_lift_are_caught_and_need_no_turbine_system(self):
        pod = POWER.replace('<defName>Test_Jet</defName>\n    <engine><kind>Turbofan</kind>', '<defName>Test_Jet</defName>\n    <engine><kind>Thruster</kind>')
        result = self.run_with(pod)
        self.assertTrue(any('thrusters need a liftToWeightAtMaxGross' in e for e in result['errors']))
        lifted = pod.replace('<waterInjection>', '<liftToWeightAtMaxGross>1.4</liftToWeightAtMaxGross><waterInjection>')
        result = self.run_with(lifted.replace('<li><def>PhobosVehicleSystem_Turbine</def></li>', ''))
        self.assertEqual(result['errors'], [])
        self.assertFalse(any('without the turbine system' in w for w in result['warnings']))

    def test_a_diesel_electric_holding_more_than_its_most_pull_is_caught(self):
        result = self.run_with(POWER.replace(
            '<transmission><kind>Automatic</kind><gears><li>3.5</li><li>2.0</li></gears><upshiftRpm>2400</upshiftRpm><downshiftRpm>1300</downshiftRpm></transmission>',
            '<transmission><kind>DieselElectric</kind><maxTractiveEffortKilonewtons>100</maxTractiveEffortKilonewtons>'
            '<continuousTractiveEffortKilonewtons>150</continuousTractiveEffortKilonewtons></transmission>'))
        self.assertTrue(any('continuous tractive effort' in e for e in result['errors']))

    def test_a_service_drawing_water_from_an_undeclared_parameter_is_caught(self):
        result = self.run_with(POWER.replace('<waterLitres>10</waterLitres>', '<waterLitresParameter>tankLitres</waterLitresParameter>'))
        self.assertTrue(any('undeclared parameter tankLitres' in e for e in result['errors']))

    def test_a_part_with_a_bad_combat_extended_hit_weight_is_caught(self):
        result = self.run_with(SYSTEM.replace('<add><key>TestEngine</key><health>10</health></add>',
                                              '<add><key>TestEngine</key><health>10</health><hitWeightCE>0</hitWeightCE></add>'))
        self.assertTrue(any('positive hitWeightCE' in e for e in result['errors']))

    def test_a_load_condition_naming_no_load_and_a_bad_cabin_are_caught(self):
        result = self.run_with(SYSTEM.replace('<defName>Test_Loadout</defName>', '<defName>Test_Loadout</defName><cabin><closed>maybe</closed></cabin>'
                                              '<loads><li><name>lamp</name><amps>1</amps><when Class="Phobos.VehicleSystems.When_LoadActive"><load>fan</load></when></li></loads>'))
        self.assertTrue(any('cabin closed' in e for e in result['errors']))
        self.assertTrue(any('When_LoadActive names a load' in e for e in result['errors']))

    def test_a_hidden_dashboard_quantity_is_caught(self):
        result = self.run_with(SYSTEM.replace('<name>heat</name>', '<name>heat</name><hidden>true</hidden><showOnDashboard>true</showOnDashboard>'))
        self.assertTrue(any('hidden and on the dashboard' in e for e in result['errors']))

    def test_a_service_with_negative_water_is_caught(self):
        result = self.run_with(POWER.replace('<waterLitres>10</waterLitres>', '<waterLitres>-1</waterLitres>'))
        self.assertTrue(any('waterLitres' in e for e in result['errors']))


POWER = SYSTEM.replace('</Defs>', """
  <Phobos.VehicleSystems.PhobosVehicleSystemDef>
    <defName>Test_Water</defName>
    <quantities><li><name>injectionWater</name></li></quantities>
    <service><dueQuantity>injectionWater</dueQuantity><dueBelow>0.5</dueBelow><waterLitres>10</waterLitres></service>
  </Phobos.VehicleSystems.PhobosVehicleSystemDef>
  <Phobos.VehicleSystems.PhobosVehicleSystemDef>
    <defName>PhobosVehicleSystem_Turbine</defName>
  </Phobos.VehicleSystems.PhobosVehicleSystemDef>
  <Phobos.VehicleSystems.PhobosVehicleDrivetrainDef>
    <defName>Test_Diesel</defName>
    <engine><kind>Piston</kind><idleRpm>700</idleRpm><launchRpm>1100</launchRpm><ratedRpm>3000</ratedRpm></engine>
    <transmission><kind>Automatic</kind><gears><li>3.5</li><li>2.0</li></gears><upshiftRpm>2400</upshiftRpm><downshiftRpm>1300</downshiftRpm></transmission>
  </Phobos.VehicleSystems.PhobosVehicleDrivetrainDef>
  <Phobos.VehicleSystems.PhobosVehicleDrivetrainDef>
    <defName>Test_Jet</defName>
    <engine><kind>Turbofan</kind>
      <turbine>
        <ratings>
          <li><name>continuous</name><share>1</share><temperatureLimit>800</temperatureLimit></li>
          <li><name>wet</name><share>1.3</share><temperatureLimit>900</temperatureLimit><minutes>5</minutes><wet>true</wet></li>
        </ratings>
        <waterInjection><tankKilograms>100</tankKilograms></waterInjection>
      </turbine>
    </engine>
    <transmission><kind>None</kind></transmission>
  </Phobos.VehicleSystems.PhobosVehicleDrivetrainDef>
  <Phobos.VehicleSystems.PhobosVehicleFlightProfileDef>
    <defName>Test_Profile</defName>
    <reader Class="OtherMod.Reader"><rules><li><phase>Cruise</phase><member>FlightState</member><equals>Flying</equals></li></rules></reader>
    <phases><li><phase>Cruise</phase><label>cruising</label><demand>0.6</demand></li></phases>
  </Phobos.VehicleSystems.PhobosVehicleFlightProfileDef>
  <Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
    <defName>Test_Aircraft</defName>
    <systems><li><def>Test_Water</def></li><li><def>PhobosVehicleSystem_Turbine</def></li></systems>
    <drivetrain><def>Test_Jet</def></drivetrain>
    <flightProfile>Test_Profile</flightProfile>
  </Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
</Defs>
""")


if __name__ == '__main__':
    unittest.main()
