"""Offline checks for scripts/check-factory-patches.py on a synthetic game. Nothing here reads the real installation."""
import importlib.util
import os
from pathlib import Path
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


checker = load('scripts/check-factory-patches.py', 'check_factory_patches')

CORE_ABOUT = '<ModMetaData><name>Core</name><packageId>Ludeon.RimWorld</packageId></ModMetaData>'
CORE_DEFS = '''<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <ThingDef Name="DrugBase" Abstract="True"><stackLimit>150</stackLimit></ThingDef>
  <ThingDef ParentName="DrugBase"><defName>Flake</defName></ThingDef>
  <ThingDef><defName>PsychoidLeaves</defName></ThingDef>
  <ThingDef><defName>Human</defName><race /></ThingDef>
  <ThingDef><defName>Neutroamine</defName><stackLimit>150</stackLimit></ThingDef>
  <ThingDef><defName>MedicineHerbal</defName><stackLimit>25</stackLimit></ThingDef>
  <ResearchProjectDef><defName>PsychiteRefining</defName></ResearchProjectDef>
  <SoundDef><defName>VFEFactory_AutomatedDrugsSustainer</defName></SoundDef>
  <EffecterDef><defName>VFEFactory_Cook</defName></EffecterDef>
</Defs>'''
FACTORY_ABOUT = '<ModMetaData><name>Factory</name><packageId>Example.Factory</packageId></ModMetaData>'
FACTORY_LOAD_FOLDERS = '<loadFolders><v1.6><li>/</li><li>1.6</li></v1.6></loadFolders>'
FACTORY_DEFS = '''<Defs>
  <ThingDef><defName>VFEFactory_MedicineGranulator</defName><comps>
    <li Class="PipeSystem.CompProperties_AdvancedResourceProcessor"><processes><li>Stock</li></processes></li>
  </comps></ThingDef>
  <ResearchProjectDef><defName>VFE_ComplexFactories</defName></ResearchProjectDef>
</Defs>'''
OPTIONAL_ABOUT = '<ModMetaData><name>Optional Drugs</name><packageId>Example.Optional</packageId></ModMetaData>'
OPTIONAL_DEFS = '<Defs><ThingDef ParentName="DrugBase"><defName>OptionalPill</defName></ThingDef></Defs>'
OUR_ABOUT = '<ModMetaData><name>Ours</name><packageId>Phobos.Test</packageId></ModMetaData>'
OUR_LOAD_FOLDERS = '''<loadFolders>
  <v1.6><li>/</li><li>Common</li><li IfModActive="example.optional">Mods/Optional</li><li IfModActive="example.absent">Mods/Absent</li></v1.6>
  <v1.7><li>/</li><li>Common</li><li IfModActive="example.optional">Mods/Optional</li><li IfModActive="example.absent">Mods/Absent</li></v1.7>
</loadFolders>'''


def process(def_name, ingredient, result, count, cap, research='PsychiteRefining', may_require=''):
    attr = f' MayRequire="{may_require}"' if may_require else ''
    cap_xml = f'<maxOutputCount>{cap}</maxOutputCount>' if cap is not None else ''
    return f'''<PipeSystem.ProcessDef{attr}>
    <defName>{def_name}</defName>
    <ingredients><li><thing>{ingredient}</thing><countNeeded>16</countNeeded></li></ingredients>
    <results><li><thing>{result}</thing><count>{count}</count></li></results>
    <researchPrerequisites><li>{research}</li><li>VFE_ComplexFactories</li></researchPrerequisites>
    <onlyGrabAndOutputToFactoryHoppers>true</onlyGrabAndOutputToFactoryHoppers>
    <sustainerDef>VFEFactory_AutomatedDrugsSustainer</sustainerDef>
    <effecterDef>VFEFactory_Cook</effecterDef>
    {cap_xml}
  </PipeSystem.ProcessDef>'''


def patch(names):
    lis = ''.join(f'<li>{n}</li>' for n in names)
    return f'''<Patch><Operation Class="PatchOperationAdd">
  <xpath>Defs/ThingDef[defName="VFEFactory_MedicineGranulator"]/comps/li[@Class="PipeSystem.CompProperties_AdvancedResourceProcessor"]/processes</xpath>
  <value>{lis}</value></Operation></Patch>'''


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8')


class Fixture:
    def __init__(self, root, version='1.7'):
        root = Path(root)
        self.game = root / 'RimWorld'
        self.workshop = root / 'workshop'
        self.config = root / 'Config'
        self.mod = root / 'repo' / 'mods' / 'Ours'
        write(self.game / 'Data' / 'Core' / 'About' / 'About.xml', CORE_ABOUT)
        write(self.game / 'Data' / 'Core' / 'Defs' / 'Things.xml', CORE_DEFS)
        write(self.workshop / '1' / 'About' / 'About.xml', FACTORY_ABOUT)
        write(self.workshop / '1' / 'LoadFolders.xml', FACTORY_LOAD_FOLDERS)
        write(self.workshop / '1' / '1.6' / 'Defs' / 'Buildings.xml', FACTORY_DEFS)
        write(self.workshop / '2' / 'About' / 'About.xml', OPTIONAL_ABOUT)
        write(self.workshop / '2' / 'Defs' / 'Pills.xml', OPTIONAL_DEFS)
        # an installed copy of our own mod must not be loaded a second time
        write(self.game / 'Mods' / 'Ours' / 'About' / 'About.xml', OUR_ABOUT)
        write(self.game / 'Mods' / 'Ours' / 'Common' / 'Defs' / 'P.xml', '<Defs>' + process('Phobos_Flake', 'PsychoidLeaves', 'Flake', 4, 75) + '</Defs>')
        write(self.config / 'ModsConfig.xml', f'<ModsConfigData><version>{version}.1</version><activeMods>'
              '<li>ludeon.rimworld</li><li>example.factory</li><li>example.optional</li><li>phobos.test</li></activeMods></ModsConfigData>')
        write(self.mod / 'About' / 'About.xml', OUR_ABOUT)
        write(self.mod / 'LoadFolders.xml', OUR_LOAD_FOLDERS)

    def check(self):
        return checker.check(self.mod, self.game, self.workshop, self.config)


class CheckerTests(unittest.TestCase):
    def test_a_sound_mod_passes(self):
        with tempfile.TemporaryDirectory() as tmp:
            f = Fixture(tmp)
            write(f.mod / 'Common' / 'Defs' / 'ProcessDefs' / 'P.xml',
                  '<Defs>' + process('Phobos_Flake', 'PsychoidLeaves', 'Flake', 4, 75)
                  + process('Phobos_Meat', 'Meat_Human', 'Neutroamine', 6, 75) + '</Defs>')
            write(f.mod / 'Common' / 'Patches' / 'Granulator.xml', patch(['Phobos_Flake', 'Phobos_Meat']))
            write(f.mod / 'Mods' / 'Optional' / 'Defs' / 'P.xml',
                  '<Defs>' + process('Phobos_Pill', 'PsychoidLeaves', 'OptionalPill', 4, 75)
                  + process('Phobos_Skipped', 'Nothing', 'Nothing', 4, 0, may_require='example.absent') + '</Defs>')
            write(f.mod / 'Mods' / 'Optional' / 'Patches' / 'Granulator_Optional.xml',
                  '''<Patch><Operation Class="PatchOperationFindMod"><mods><li>Optional Drugs</li></mods>
                  <match Class="PatchOperationSequence"><operations>
                  <li Class="PatchOperationAdd"><xpath>Defs/PipeSystem.ProcessDef[defName="Phobos_Pill"]</xpath><value><x>1</x></value></li>
                  </operations></match></Operation>''' + patch(['Phobos_Pill'])[7:] )
            write(f.mod / 'Mods' / 'Absent' / 'Defs' / 'P.xml', '<Defs>' + process('Phobos_Bad', 'Nothing', 'Nothing', 4, 0) + '</Defs>')
            errors, summary = f.check()
            self.assertEqual(errors, [])
            self.assertIn('3 ProcessDefs', summary)
            self.assertIn('3 bills added', summary)

    def test_faults_are_reported(self):
        with tempfile.TemporaryDirectory() as tmp:
            f = Fixture(tmp)
            write(f.mod / 'Common' / 'Defs' / 'ProcessDefs' / 'P.xml',
                  '<Defs>' + process('Phobos_NoCap', 'PsychoidLeaves', 'Flake', 4, None)
                  + process('Phobos_HighCap', 'PsychoidLeaves', 'Flake', 4, 147)
                  + process('Phobos_Herbal', 'PsychoidLeaves', 'MedicineHerbal', 4, 21)
                  + process('Phobos_Missing', 'NoSuchLeaf', 'Flake', 4, 75, research='NoSuchResearch')
                  + process('Phobos_Orphan', 'PsychoidLeaves', 'Flake', 4, 75) + '</Defs>')
            write(f.mod / 'Common' / 'Patches' / 'Granulator.xml',
                  patch(['Phobos_NoCap', 'Phobos_HighCap', 'Phobos_Herbal', 'Phobos_Missing', 'Phobos_Ghost'])
                  .replace('</Patch>', '<Operation Class="PatchOperationAdd"><xpath>Defs/ThingDef[defName="Nobody"]</xpath>'
                           '<value><x/></value></Operation></Patch>'))
            errors, _ = f.check()
            text = '\n'.join(errors)
            self.assertIn('Phobos_NoCap: hopper-only process without maxOutputCount', text)
            self.assertIn('Phobos_HighCap: maxOutputCount 147 is above the stack limit of Flake (150) minus the batch of 4', text)
            self.assertNotIn('Phobos_Herbal', text)
            self.assertIn('ingredient NoSuchLeaf is not a loaded Def', text)
            self.assertIn('research NoSuchResearch is not a loaded Def', text)
            self.assertIn('Phobos_Orphan is defined but no patch adds it', text)
            self.assertIn('adds Phobos_Ghost to a machine, but no loaded Def', text)
            self.assertIn('xpath matches 0 nodes, not 1: Defs/ThingDef[defName="Nobody"]', text)
            self.assertEqual(len(errors), 7)

    def test_a_file_hidden_by_a_later_folder_is_reported(self):
        # The 0.3.0 fault: two load folders with Patches/Granulator.xml. The game loads only the later folder's.
        with tempfile.TemporaryDirectory() as tmp:
            f = Fixture(tmp)
            write(f.mod / 'Common' / 'Defs' / 'P.xml', '<Defs>' + process('Phobos_Flake', 'PsychoidLeaves', 'Flake', 4, 75) + '</Defs>')
            write(f.mod / 'Common' / 'Patches' / 'Granulator.xml', patch(['Phobos_Flake']))
            write(f.mod / 'Mods' / 'Optional' / 'Defs' / 'P_Optional.xml',
                  '<Defs>' + process('Phobos_Pill', 'PsychoidLeaves', 'OptionalPill', 4, 75) + '</Defs>')
            write(f.mod / 'Mods' / 'Optional' / 'Patches' / 'Granulator.xml', patch(['Phobos_Pill']))
            errors, summary = f.check()
            self.assertEqual(errors, [
                'Common/Patches/Granulator.xml is never loaded: the game loads Mods/Optional/Patches/Granulator.xml '
                'instead (same path inside a later-listed folder)',
                'Common/Defs/P.xml: Phobos_Flake is defined but no patch adds it to a machine'])
            self.assertIn('1 patch files', summary)

    def test_parent_names_must_exist_or_be_added_by_a_patch(self):
        with tempfile.TemporaryDirectory() as tmp:
            f = Fixture(tmp)
            name_patch = ('<Operation Class="PatchOperationAttributeAdd"><xpath>Defs/ThingDef[defName="{0}"]</xpath>'
                          '<attribute>Name</attribute><value>{1}</value></Operation>')
            write(f.mod / 'Common' / 'Patches' / 'Names.xml',
                  '<Patch>' + name_patch.format('VFEFactory_MedicineGranulator', 'Phobos_GranulatorParent') + '</Patch>')
            write(f.mod / 'Common' / 'Defs' / 'Things.xml', '<Defs>'
                  '<ThingDef ParentName="Phobos_GranulatorParent"><defName>Phobos_Good</defName></ThingDef>'
                  '<ThingDef ParentName="DrugBase"><defName>Phobos_VanillaParent</defName></ThingDef>'
                  '<ThingDef ParentName="Phobos_Nowhere"><defName>Phobos_Orphan</defName></ThingDef></Defs>')
            errors, _ = f.check()
            self.assertEqual(errors, ['Common/Defs/Things.xml: Phobos_Orphan: ParentName Phobos_Nowhere is not a Name '
                                      "in the loaded Defs or one this mod's patches add"])
            # If the target already has a Name, the game keeps it, skips the patch and the ParentName breaks.
            write(f.workshop / '1' / '1.6' / 'Defs' / 'Named.xml', '<Defs><ThingDef Name="TheirName"><defName>Named</defName></ThingDef></Defs>')
            write(f.mod / 'Common' / 'Patches' / 'Names.xml',
                  '<Patch>' + name_patch.format('Named', 'Phobos_GranulatorParent') + '</Patch>')
            text = '\n'.join(f.check()[0])
            self.assertIn('already has Name="TheirName", so the game keeps it and skips this PatchOperationAttributeAdd', text)
            self.assertIn('Phobos_Good: ParentName Phobos_GranulatorParent is not a Name', text)

    def test_load_folders_fall_back_to_the_newest_lower_version(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write(root / 'LoadFolders.xml', '<loadFolders><v1.5><li>/</li><li>1.5</li></v1.5><v1.6><li>1.6</li><li IfModActive="a.b,c.d">Mods/X</li></v1.6></loadFolders>')
            for rel in ('1.5', '1.6', 'Mods/X'):
                (root / rel).mkdir(parents=True)
            folders = checker.load_folders_for(root, '1.7', {'c.d'})
            self.assertEqual([p.relative_to(root).as_posix() for p in folders], ['1.6', 'Mods/X'])
            self.assertEqual([p.relative_to(root).as_posix() for p in checker.load_folders_for(root, '1.5', set())], ['.', '1.5'])
            (root / 'LoadFolders.xml').unlink()
            (root / 'Common').mkdir()
            self.assertEqual([p.relative_to(root).as_posix() for p in checker.load_folders_for(root, '1.7', set())], ['.', 'Common', '1.6'])


if __name__ == '__main__':
    unittest.main()
