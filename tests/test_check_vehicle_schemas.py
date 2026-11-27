"""Offline checks for scripts/check-vehicle-schemas.py: the shipped schemas pass, and each kind of mistake is caught.

Every mod below is synthetic; nothing reads or writes the RimWorld installation.
"""
import contextlib
import importlib.util
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
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

    def test_an_optional_or_per_engine_role_left_unbound_is_no_warning(self):
        # Framework 0.368.0 (VS11): a role a vehicle may rightly lack, and a role a system adds a part for beside each engine.
        unbound = SYSTEM.replace('<partRoles><li><role>Engine</role><add><key>TestEngine</key><health>10</health></add></li></partRoles>', '')
        result = self.run_with(unbound.replace('<roles><li>Engine</li></roles>', '<roles><li>Engine</li></roles><optionalRoles><li>Engine</li></optionalRoles>'))
        self.assertEqual(result['errors'], [])
        self.assertFalse(any('binds to nothing' in w for w in result['warnings']))
        result = self.run_with(unbound.replace('<roles><li>Engine</li></roles>', '<roles><li>Engine</li></roles><partPerEngine><role>Engine</role></partPerEngine>'))
        self.assertFalse(any('binds to nothing' in w for w in result['warnings']))
        result = self.run_with(SYSTEM.replace('<roles><li>Engine</li></roles>', '<roles><li>Engine</li></roles><optionalRoles><li>Radiator</li></optionalRoles>'))
        self.assertTrue(any('optionalRoles names Radiator' in e for e in result['errors']))

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


    def test_a_good_condition_with_a_hit_rule_and_a_cause_passes(self):
        result = self.run_with(CONDITIONS)
        self.assertEqual(result['errors'], [])
        self.assertIn('PhobosVehicleCondition_TestHoled', result['conditions'])

    def test_a_condition_stage_outside_zero_to_one_is_caught(self):
        result = self.run_with(CONDITIONS.replace('<from>0.5</from>', '<from>1.5</from>'))
        self.assertTrue(any('outside a severity of 0 to 1' in e for e in result['errors']))

    def test_a_rule_or_cause_naming_a_missing_condition_of_ours_is_caught(self):
        result = self.run_with(CONDITIONS.replace('<condition>PhobosVehicleCondition_TestHoled</condition>',
                                                  '<condition>PhobosVehicleCondition_Nowhere</condition>'))
        self.assertTrue(any('does not exist (PhobosVehicleCondition_Nowhere)' in e for e in result['errors']))

    def test_another_mods_condition_is_trusted(self):
        result = self.run_with(CONDITIONS.replace('<condition>PhobosVehicleCondition_TestHoled</condition>',
                                                  '<condition>OtherMod_Cracked</condition>'))
        self.assertFalse(any('does not exist' in e for e in result['errors']))

    def test_a_condition_effect_naming_no_quantity_is_caught(self):
        result = self.run_with(CONDITIONS.replace('<quantity>heat</quantity><perHour>-1</perHour>', '<quantity>coolant</quantity><perHour>-1</perHour>'))
        self.assertTrue(any('names a quantity no system has (coolant)' in e for e in result['errors']))

    def test_a_hit_rule_with_neither_quantity_nor_condition_is_caught(self):
        result = self.run_with(CONDITIONS.replace('<condition>PhobosVehicleCondition_TestHoled</condition>', ''))
        self.assertTrue(any('moves no quantity and gives no condition' in e for e in result['errors']))

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


CONDITIONS = SYSTEM.replace('<flows>', """<onPartHit>
      <li><role>Engine</role><condition>PhobosVehicleCondition_TestHoled</condition><perHealthLost>0.03</perHealthLost>
        <armorCategories><li>Sharp</li></armorCategories><exceptDamageDefs><li>Scratch</li></exceptDamageDefs></li>
    </onPartHit>
    <flows>""").replace('<effects><li Class="Phobos.VehicleSystems.Effect_Message">', """<effects><li Class="Phobos.VehicleSystems.Effect_AddCondition">
              <condition>PhobosVehicleCondition_TestHoled</condition><role>Engine</role><perHour>0.2</perHour></li>
              <li Class="Phobos.VehicleSystems.Effect_Message">""").replace('</Defs>', """
  <Phobos.VehicleSystems.PhobosVehicleConditionDef>
    <defName>PhobosVehicleCondition_TestHoled</defName>
    <label>holed</label>
    <stages>
      <li><to>0.5</to><label>seeping</label>
        <effects><li Class="Phobos.VehicleSystems.Effect_RateQuantity"><quantity>heat</quantity><perHour>-1</perHour></li></effects></li>
      <li><from>0.5</from><label>leaking</label></li>
    </stages>
  </Phobos.VehicleSystems.PhobosVehicleConditionDef>
</Defs>""")

OUR_DEFS = """<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Phobos.VehicleSystems.PhobosVehicleSystemDef>
    <defName>Test_System</defName>
    <roles><li>Engine</li><li>Radiator</li></roles>
    <parameters><rate>2</rate></parameters>
    <quantities><li><name>heat</name></li></quantities>
  </Phobos.VehicleSystems.PhobosVehicleSystemDef>
  <Phobos.VehicleSystems.PhobosVehicleLoadoutDef Name="Test_Base" Abstract="True">
    <systems><li><def>Test_System</def></li></systems>
    <partRoles><li><role>Engine</role><add><key>TestEngine</key><health>10</health></add></li></partRoles>
  </Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
  <Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
    <defName>Test_OurLoadout</defName>
    <systems><li><def>Test_System</def></li></systems>
  </Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
</Defs>
"""

THEIR_LOADOUT = """<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <Phobos.VehicleSystems.PhobosVehicleLoadoutDef ParentName="Test_Base">
    <defName>Their_Loadout</defName>
    <systems Inherit="False"><li><def>Test_System</def><set><rate>3</rate></set></li></systems>
    <partRoles><li><role>Radiator</role><add><key>TheirRadiator</key></add></li></partRoles>
  </Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
</Defs>
"""

PATCH = """<?xml version="1.0" encoding="utf-8"?>
<Patch>
  <Operation Class="PatchOperationAdd">
    <xpath>Defs/Phobos.VehicleSystems.PhobosVehicleLoadoutDef[defName="Test_OurLoadout"]</xpath>
    <value>
      <partRoles><li><role>Engine</role></li><li><role>Engine</role></li></partRoles>
    </value>
  </Operation>
  <Operation Class="PatchOperationAdd">
    <xpath>Defs/Phobos.VehicleSystems.PhobosVehicleLoadoutDef[defName="Test_NoSuchLoadout"]/partRoles</xpath>
    <value><li><role>Engine</role></li></value>
  </Operation>
  <Operation Class="PatchOperationAdd">
    <xpath>Defs/Phobos.VehicleSystems.PhobosVehicleLoadoutDef[contains(defName, "Test")]/partRoles</xpath>
    <value><li><role>Engine</role></li></value>
  </Operation>
</Patch>
"""


def write(path, content):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding='utf-8')


def about(package):
    return f'<?xml version="1.0" encoding="utf-8"?>\n<ModMetaData><packageId>{package}</packageId></ModMetaData>\n'


class AnotherModTests(unittest.TestCase):
    """--mod: another author's folder, checked against ours, each finding with its file, line, Def and what to do."""

    def setUp(self):
        self.temp = Path(tempfile.mkdtemp())
        self.ours = self.temp / 'Mods' / 'OurFramework'
        write(self.ours / 'About/About.xml', about('PhobosDthorga.Framework'))
        write(self.ours / 'Common/Defs/Systems.xml', OUR_DEFS)
        write(self.temp / 'Mods' / 'SomeoneElse/About/About.xml', about('Someone.Else'))
        self.mod = self.temp / 'MyMod'
        write(self.mod / 'About/About.xml', about('Someone.MyMod'))

    def tearDown(self):
        shutil.rmtree(self.temp, ignore_errors=True)

    def run_mod(self, *mods):
        report = schemas.check_mods([str(m) for m in (mods or [self.mod])], [str(self.temp / 'Mods')])
        return report, report['mods'][0]['findings']

    def test_a_good_loadout_inheriting_ours_passes(self):
        write(self.mod / '1.7/Defs/Loadouts.xml', THEIR_LOADOUT)
        report, findings = self.run_mod()
        self.assertEqual(report['ours'], [str(self.ours)])  # only our mods, by package ID
        self.assertEqual([f for f in findings if f['severity'] == 'error'], [])
        self.assertEqual(report['mods'][0]['read'], {'loadout': 1})

    def test_a_mistake_names_file_line_def_and_a_fix(self):
        write(self.mod / '1.7/Defs/Loadouts.xml', THEIR_LOADOUT.replace('<set><rate>', '<set><rates>').replace('</rate></set>', '</rates></set>'))
        _, findings = self.run_mod()
        found = next(f for f in findings if 'no parameter rates' in f['problem'])
        self.assertEqual((found['severity'], found['file'], found['line'], found['def'], found['kind']),
                         ('error', 'MyMod/1.7/Defs/Loadouts.xml', 3, 'Their_Loadout', 'loadout'))
        self.assertIn('<parameters>', found['fix'])

    def test_a_missing_parent_is_one_error_and_holds_back_the_rest(self):
        write(self.mod / '1.7/Defs/Loadouts.xml', THEIR_LOADOUT.replace('ParentName="Test_Base"', 'ParentName="Test_Bas"'))
        _, findings = self.run_mod()
        self.assertEqual([f['problem'].split(' names')[0] for f in findings], ['ParentName Test_Bas'])

    def test_each_version_folder_is_read_and_a_finding_says_where_it_holds(self):
        write(self.mod / '1.6/Defs/Loadouts.xml', THEIR_LOADOUT.replace('<def>Test_System</def><set>', '<def>Test_Sistem</def><set>'))
        write(self.mod / '1.7/Defs/Loadouts.xml', THEIR_LOADOUT)
        report, findings = self.run_mod()
        self.assertEqual(report['mods'][0]['versions'], ['1.6', '1.7'])
        found = next(f for f in findings if 'no system Test_Sistem' in f['problem'])
        self.assertEqual((found['file'], found['versions']), ('MyMod/1.6/Defs/Loadouts.xml', ['1.6']))

    def test_load_folders_are_followed(self):
        write(self.mod / 'LoadFolders.xml', '<loadFolders><v1.7><li>/</li><li IfModActive="Other.Mod">Mods/Other</li></v1.7></loadFolders>')
        write(self.mod / 'Mods/Other/Defs/Loadouts.xml', THEIR_LOADOUT.replace('<role>Radiator</role>', '<role>Engine</role>'))
        write(self.mod / 'Unlisted/Defs/Loadouts.xml', THEIR_LOADOUT.replace('<def>Test_System</def><set>', '<def>Test_Sistem</def><set>'))
        _, findings = self.run_mod()
        self.assertTrue(any('bound twice' in f['problem'] and f['file'] == 'MyMod/Mods/Other/Defs/Loadouts.xml' for f in findings))
        self.assertFalse(any('Test_Sistem' in f['problem'] for f in findings))

    def test_broken_xml_is_an_error_at_its_line(self):
        write(self.mod / 'Defs/Broken.xml', '<?xml version="1.0"?>\n<Defs>\n  <ThingDef>\n</Defs>\n')
        _, findings = self.run_mod()
        self.assertEqual([(f['file'], f['line'], f['problem'][:31]) for f in findings],
                         [('MyMod/Defs/Broken.xml', 4, 'the file is not well-formed XML')])

    def test_patches_change_ours_and_are_blamed_for_what_they_break(self):
        write(self.mod / 'Patches/Loadouts.xml', PATCH)
        _, findings = self.run_mod()
        twice = next(f for f in findings if 'bound twice' in f['problem'])
        self.assertEqual((twice['file'], twice['line'], twice['def']), ('MyMod/Patches/Loadouts.xml', 3, 'Test_OurLoadout'))
        self.assertTrue(twice['problem'].startswith("D'thorga's loadout Test_OurLoadout, as your patch changes it"))
        nothing = next(f for f in findings if 'changes nothing' in f['problem'])
        self.assertEqual((nothing['severity'], nothing['line']), ('error', 9))
        note = next(f for f in findings if f['severity'] == 'note')
        self.assertEqual(note['line'], 13)
        # Our own Defs' findings with no patch of theirs involved are never shown (our loadout's Radiator is unbound either way).
        self.assertFalse(any('Radiator' in f['problem'] for f in findings))

    def test_a_def_with_our_defname_replaces_ours_with_a_warning(self):
        write(self.mod / 'Defs/Systems.xml', OUR_DEFS.split('<Phobos.VehicleSystems.PhobosVehicleLoadoutDef Name')[0] + '</Defs>\n')
        _, findings = self.run_mod()
        self.assertEqual([(f['severity'], f['def']) for f in findings], [('warning', 'Test_System')])
        self.assertIn('replaces', findings[0]['problem'])

    def test_the_command_line_exits_1_on_an_error_and_2_without_ours(self):
        write(self.mod / '1.7/Defs/Loadouts.xml', THEIR_LOADOUT.replace('<def>Test_System</def><set>', '<def>Test_Sistem</def><set>'))
        argv = sys.argv
        try:
            with contextlib.redirect_stdout(io.StringIO()) as out:
                sys.argv = ['check', '--mod', str(self.mod), '--ours', str(self.temp / 'Mods')]
                self.assertEqual(schemas.main(), 1)
                sys.argv = ['check', '--mod', str(self.mod), '--ours', str(self.mod)]
                self.assertEqual(schemas.main(), 2)
        finally:
            sys.argv = argv
        self.assertIn('MyMod/1.7/Defs/Loadouts.xml, line 3', out.getvalue())
        self.assertIn('To fix:', out.getvalue())


def fake_index():
    """A type index as our assemblies would give it, written by hand."""
    def kind(t):
        return {'base': 'System.Object', 'fields': {}, 'members': [], 'custom': False, 'enum': False, 'abstract': False, **t}
    return schemas.TypeIndex({
        'Phobos.VehicleSystems.PhobosVehicleLoadoutDef': kind({'base': 'Verse.Def', 'fields': {
            'rank': ('prim', 'int'), 'cabinOpen': ('prim', 'bool'), 'priority': ('type', 'Phobos.VehicleSystems.LoadoutPriority'),
            'partRoles': ('list', ('type', 'Phobos.VehicleSystems.PartRoleSpec')), 'set': ('type', 'Phobos.VehicleSystems.ParameterSet'),
            'effect': ('type', 'Phobos.VehicleSystems.EffectBase'), 'external': ('type', 'Verse.IntRange')}}),
        'Phobos.VehicleSystems.PartRoleSpec': kind({'fields': {'role': ('prim', 'string'), 'health': ('prim', 'float')}}),
        'Phobos.VehicleSystems.LoadoutPriority': kind({'base': 'System.Enum', 'enum': True, 'members': ['Default', 'Vehicle']}),
        'Phobos.VehicleSystems.ParameterSet': kind({'custom': True}),
        'Phobos.VehicleSystems.EffectBase': kind({'abstract': True}),
        'Phobos.VehicleSystems.Effect_Message': kind({'base': 'Phobos.VehicleSystems.EffectBase', 'fields': {'message': ('prim', 'string')}}),
        'Phobos.VehicleSystems.OnOtherBase': kind({'base': 'Verse.DefModExtension', 'fields': {'a': ('prim', 'int')}}),
    })


class FieldCheckTests(unittest.TestCase):
    """Every element inside one of our classes names a field of it and holds what the field takes."""

    def findings(self, xml):
        found = []
        schemas.check_fields(ET.fromstring(xml), fake_index(), lambda element, problem: found.append(problem))
        return found

    def test_good_fields_pass(self):
        self.assertEqual(self.findings(
            '<Phobos.VehicleSystems.PhobosVehicleLoadoutDef><defName>A</defName><rank>2</rank><cabinOpen>True</cabinOpen>'
            '<priority>Vehicle</priority><partRoles><li><role>Engine</role><health>2.5</health></li></partRoles>'
            '<set><anything>1</anything></set><external>1~2</external>'
            '<effect Class="Phobos.VehicleSystems.Effect_Message"><message>Key</message></effect>'
            '</Phobos.VehicleSystems.PhobosVehicleLoadoutDef>'), [])

    def test_an_unknown_field_is_caught_with_a_suggestion(self):
        self.assertEqual(self.findings('<Phobos.VehicleSystems.PhobosVehicleLoadoutDef><rnak>2</rnak>'
                                       '<partRoles><li><rol>Engine</rol></li></partRoles></Phobos.VehicleSystems.PhobosVehicleLoadoutDef>'),
                         ['<rnak> is not a field of PhobosVehicleLoadoutDef (did you mean <rank>?)',
                          '<rol> is not a field of PartRoleSpec (did you mean <role>?)'])

    def test_bad_values_are_caught(self):
        self.assertEqual(self.findings(
            '<Phobos.VehicleSystems.PhobosVehicleLoadoutDef><rank>2.5</rank><cabinOpen>yes</cabinOpen><priority>Vehicles</priority>'
            '<partRoles><li><health>ten</health></li></partRoles></Phobos.VehicleSystems.PhobosVehicleLoadoutDef>'),
            ['<rank> is not a whole number (2.5)', '<cabinOpen> is not true or false (yes)',
             '<priority> Vehicles is not one of: Default, Vehicle', '<health> is not a number (ten)'])

    def test_classes_ours_abstract_and_another_mods(self):
        self.assertEqual(self.findings(
            '<Phobos.VehicleSystems.PhobosVehicleLoadoutDef><effect><message>K</message></effect></Phobos.VehicleSystems.PhobosVehicleLoadoutDef>'),
            ['<effect> needs a Class: EffectBase is only a base for other classes'])
        self.assertEqual(self.findings('<ThingDef><comps><li Class="Phobos.VehicleSystems.Effect_Massage"/>'
                                       '<li Class="OtherMod.Comp"><whatever/></li></comps></ThingDef>'),
                         ['no class Phobos.VehicleSystems.Effect_Massage'])
        self.assertEqual(self.findings('<Phobos.VehicleSystems.PhobosVehicleLodoutDef/>'),
                         ['no Def type Phobos.VehicleSystems.PhobosVehicleLodoutDef'])

    def test_a_class_built_on_one_we_cannot_read_is_trusted(self):
        self.assertEqual(self.findings('<ThingDef><modExtensions><li Class="Phobos.VehicleSystems.OnOtherBase"><b>1</b><a>x</a>'
                                       '</li></modExtensions></ThingDef>'), ['<a> is not a whole number (x)'])


BUILT = Path(ROOT, 'mods/PhobosFramework/Mods/VehicleFramework/1.7/Assemblies/PhobosFramework.Vehicles.dll')


@unittest.skipUnless(BUILT.exists(), 'D\'thorga Framework is not built here')
class BuiltAssemblyTests(unittest.TestCase):
    """The metadata reader against our real assembly: it finds every schema class the source has, and our own shipped Defs
    raise no field finding at all (so a finding on another author's Def is not the reader's mistake)."""

    def test_the_reader_finds_every_schema_class_and_our_defs_pass(self):
        ours = schemas.Ours(schemas.find_our_mods([str(Path(ROOT, 'mods'))]))
        self.assertEqual(schemas.our_classes(ROOT) - ours.index.schema_classes(), set())
        loadout = ours.index.types['Phobos.VehicleSystems.PhobosVehicleLoadoutDef']
        self.assertEqual(loadout['base'], 'Verse.Def')
        self.assertEqual(loadout['fields']['systems'], ('list', ('type', 'Phobos.VehicleSystems.LoadoutSystem')))
        found = []
        for our_mod in ours.mods:
            for node, source in our_mod.defs:
                schemas.check_fields(node, ours.index, lambda element, problem, s=source: found.append((s, problem)))
        self.assertEqual(found, [])


class OneFileTests(unittest.TestCase):
    """The one file for modders: the checker bundled with the kit modules it imports (the kit's bundle) runs alone, isolated from
    this repository and the kit, and finds what the checker beside the kit finds."""

    def test_the_bundled_checker_runs_alone_and_agrees(self):
        from phobos_modkit.bundle_cli import main as bundle
        temp = Path(tempfile.mkdtemp())
        argv = sys.argv
        try:
            one = temp / 'check-vehicle-schemas.py'
            self.assertEqual(bundle([os.path.join(ROOT, 'scripts/check-vehicle-schemas.py'), '--python', '3.9', '--out', str(one)],
                                    out=io.StringIO()), 0)
            args = ['--root', ROOT, '--mod', str(Path(ROOT, 'examples/DthorgaExampleMod')), '--ours', str(Path(ROOT, 'mods')), '--json']
            alone = subprocess.run([sys.executable, '-I', str(one)] + args, cwd=str(temp), capture_output=True, text=True)
            self.assertEqual(alone.stderr, '')
            with contextlib.redirect_stdout(io.StringIO()) as here:
                sys.argv = ['check'] + args
                code = schemas.main()
            self.assertEqual(alone.returncode, code)
            self.assertEqual(json.loads(alone.stdout), json.loads(here.getvalue()))
        finally:
            sys.argv = argv
            shutil.rmtree(temp, ignore_errors=True)


if __name__ == '__main__':
    unittest.main()
