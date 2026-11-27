"""scripts/xml-schema.py: the XSD of our XML, generated from made-up C#: its Defs, types, field types, enums and summaries; that
every name it uses is defined; and, where a Java runtime is present, that Xerces (as the editors use it) accepts valid RimWorld XML
it does not fully describe and flags wrong values. No game, no DocFX."""
import importlib.util
from pathlib import Path
import re
import shutil
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XS = '{http://www.w3.org/2001/XMLSchema}'

CATEGORIES = [
    {"name": "Rules Defs", "bases": ["PhobosRulesDef"], "form": "def"},
    {"name": "Inputs", "bases": ["VehicleSystemInput"], "form": "li", "container": "inputs"},
    {"name": "Comps", "bases": ["CompProperties", "CompProperties_*"], "form": "li", "container": "comps",
     "where": "In a ThingDef's comps."},
    {"name": "Def extensions", "bases": ["DefModExtension"], "form": "li", "container": "modExtensions"},
    {"name": "Defs", "bases": ["Def", "*Def"], "form": "def"},
]

SOURCE = '''using System;
using System.Xml;
using Verse;

namespace My.Mod;

public enum Mode { Off, On }

[Flags]
public enum Zones { None = 0, Cab = 1, Bay = 2 }

/// <summary>A number read from the vehicle.</summary>
public abstract class VehicleSystemInput
{
    public float factor = 1f;
}

/// <summary>A fixed number.</summary>
public class Input_Constant : VehicleSystemInput
{
    /// <summary>The number. It never changes.</summary>
    public float value = 2.5f;
}

/// <summary>A rule's part.</summary>
public class Stage
{
    public int order;
    public VehicleSystemInput? input;
}

/// <summary>Read by its own code.</summary>
public class ParameterSet
{
    public int ignored;
    public void LoadDataFromXmlCustom(XmlNode xmlRoot) { }
}

/// <summary>The comp's figures.</summary>
public class CompProperties_Thing : CompProperties
{
    /// <summary>How it runs.</summary>
    public Mode mode = Mode.On;
    public Zones zones;
    public bool loud;
    public int count = 3;
    public List<string> tags = new();
    public ThingDef? fuel;
    public IntRange span = new(1, 3);
    public GraphicData? graphic;
}

/// <summary>An extension.</summary>
public class MyExtension : DefModExtension
{
    public float weight;
}

/// <summary>Rules.</summary>
public abstract class PhobosRulesDef : Def { }

/// <summary>The game's figures.</summary>
public class MyRulesDef : PhobosRulesDef
{
    public float speed = 0.5f;
}

/// <summary>A system.</summary>
public class MySystemDef : Def
{
    public List<Stage> stages = new();
    public ParameterSet parameters = new();
    public Stage? first;
    public List<MySystemDef> comesWith = new();
}
'''

GOOD = '''<?xml version="1.0" encoding="utf-8"?>
<Defs xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <My.Mod.MySystemDef ParentName="Base" MayRequire="some.mod">
    <defName>A</defName>
    <ignoreConfigErrors>True</ignoreConfigErrors>
    <stages Inherit="False">
      <li><order>2</order><input Class="My.Mod.Input_Constant" xsi:type="My.Mod.Input_Constant"><value>4</value></input></li>
      <li><order>3</order><input Class="My.Mod.Input_Constant"><value>anything goes without xsi:type</value></input></li>
    </stages>
    <parameters><fuel>3</fuel><anything><li>x</li></anything></parameters>
    <comesWith><li>B</li></comesWith>
    <privateOrAliasField>1</privateOrAliasField>
    <modExtensions>
      <li Class="My.Mod.MyExtension" xsi:type="My.Mod.MyExtension"><weight>2</weight></li>
    </modExtensions>
  </My.Mod.MySystemDef>
  <My.Mod.MyRulesDef><defName>R</defName><speed> 0.75 </speed></My.Mod.MyRulesDef>
  <ThingDef ParentName="BuildingBase">
    <defName>T</defName>
    <techLevel>Industrial</techLevel>
    <comps>
      <li Class="My.Mod.CompProperties_Thing" xsi:type="My.Mod.CompProperties_Thing">
        <mode>Off</mode><zones>Cab, Bay</zones><loud>TRUE</loud><count>+4</count><fuel>Steel</fuel><span>1~4</span>
        <tags><li>a</li></tags><graphic><texPath>x</texPath></graphic><compClass>X</compClass>
      </li>
      <li Class="CompProperties_Flickable"/>
    </comps>
  </ThingDef>
  <RecipeDef><defName>Q</defName><whatever>1</whatever></RecipeDef>
</Defs>
'''

BAD = '''<?xml version="1.0" encoding="utf-8"?>
<Defs xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <My.Mod.MyRulesDef><speed>fast</speed></My.Mod.MyRulesDef>
  <ThingDef>
    <comps>
      <li Class="My.Mod.CompProperties_Thing" xsi:type="My.Mod.CompProperties_Thing">
        <mode>Sideways</mode>
        <loud>yes</loud>
      </li>
    </comps>
  </ThingDef>
</Defs>
'''


def load():
    spec = importlib.util.spec_from_file_location('xml_schema', ROOT / 'scripts' / 'xml-schema.py')
    module = importlib.util.module_from_spec(spec)
    sys.modules['xml_schema'] = module      # dataclasses look their module up while the class is made
    spec.loader.exec_module(module)
    return module


xsd = load()


def generated(folder):
    project = Path(folder) / 'src' / 'My.Mod'
    project.mkdir(parents=True)
    (project / 'Things.cs').write_text(SOURCE, encoding='utf-8')
    return xsd.generate(folder, {'projects': ['src/My.Mod'], 'categories': CATEGORIES})


class Generating(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.tmp = tempfile.TemporaryDirectory()
        cls.text, cls.schema = generated(cls.tmp.name)
        cls.root = ET.fromstring(cls.text)
        cls.types = {e.get('name'): e for e in cls.root if e.tag in (XS + 'complexType', XS + 'simpleType')}
        cls.elements = {e.get('name'): e for e in cls.root if e.tag == XS + 'element'}

    @classmethod
    def tearDownClass(cls):
        cls.tmp.cleanup()

    def fields(self, type_name):
        restriction = self.types[type_name].find(f'{XS}complexContent/{XS}restriction')
        return {e.get('name'): e for e in restriction.iter(XS + 'element')}

    def test_concrete_defs_are_global_elements_offered_in_defs(self):
        self.assertIn('My.Mod.MySystemDef', self.elements)
        self.assertIn('My.Mod.MyRulesDef', self.elements)
        self.assertNotIn('My.Mod.PhobosRulesDef', self.elements)          # abstract in C#
        refs = [e.get('ref') for e in self.elements['Defs'].iter(XS + 'element')]
        self.assertIn('My.Mod.MySystemDef', refs)
        self.assertIn('ThingDef', refs)                                    # a skeleton of the game's Def our comps go in

    def test_every_type_derives_from_its_csharp_base_or_its_kind(self):
        base = lambda name: self.types[name].find(f'{XS}complexContent/{XS}restriction').get('base')
        self.assertEqual(base('My.Mod.Input_Constant'), 'My.Mod.VehicleSystemInput')
        self.assertEqual(base('My.Mod.CompProperties_Thing'), 'rw.kind.Comps')
        self.assertEqual(base('My.Mod.MyRulesDef'), 'My.Mod.PhobosRulesDef')
        self.assertEqual(base('rw.kind.Comps'), 'rw.Open')
        self.assertEqual(base('My.Mod.MySystemDef'), 'rw.kind.Defs')
        self.assertEqual(base('rw.kind.Defs'), 'rw.Def')

    def test_fields_are_typed(self):
        fields = self.fields('My.Mod.CompProperties_Thing')
        types = {name: e.get('type') for name, e in fields.items()}
        self.assertEqual(types['mode'], 'enum.Mode')
        self.assertEqual(types['loud'], 'rw.bool')
        self.assertEqual(types['count'], 'rw.int')
        self.assertEqual(types['tags'], 'rw.List.rw.string')
        self.assertEqual(types['fuel'], 'rw.string')                     # a Def is named by its defName
        self.assertEqual(types['span'], 'rw.string')
        self.assertEqual(types['graphic'], 'rw.Open')                    # the game's class: anything
        self.assertEqual(types['compClass'], 'rw.string')                # inherited from the game's CompProperties
        system = {name: e.get('type') for name, e in self.fields('My.Mod.MySystemDef').items()}
        self.assertEqual(system['stages'], 'rw.List.My.Mod.Stage')
        self.assertEqual(system['first'], 'My.Mod.Stage')
        self.assertEqual(system['comesWith'], 'rw.List.rw.string')
        self.assertEqual(system['parameters'], 'My.Mod.ParameterSet')
        self.assertIn('defName', system)                                 # Verse.Def's, inherited

    def test_inherited_fields_come_first_and_are_kept(self):
        names = list(self.fields('My.Mod.Input_Constant'))
        self.assertEqual(names, ['factor', 'value'])

    def test_a_type_read_by_its_own_code_takes_anything(self):
        self.assertEqual(self.fields('My.Mod.ParameterSet'), {})
        self.assertIn('own code', self.types['My.Mod.ParameterSet'].find(f'{XS}annotation/{XS}documentation').text)

    def test_enums_are_enumerations_and_flags_take_any_text(self):
        values = [e.get('value') for e in self.types['enum.Mode.values'].iter(XS + 'enumeration')]
        self.assertEqual(values, ['Off', 'On'])
        self.assertNotIn('xs:string', self.types['enum.Mode.text'].find(XS + 'union').get('memberTypes'))
        self.assertIn('xs:string', self.types['enum.Zones.text'].find(XS + 'union').get('memberTypes'))

    def test_summaries_and_defaults_are_documentation(self):
        doc = self.fields('My.Mod.CompProperties_Thing')['mode'].find(f'{XS}annotation/{XS}documentation').text
        self.assertIn('How it runs.', doc)
        self.assertIn('default On', doc)
        type_doc = self.types['My.Mod.CompProperties_Thing'].find(f'{XS}annotation/{XS}documentation').text
        self.assertIn("The comp's figures.", type_doc)
        self.assertIn('xsi:type="My.Mod.CompProperties_Thing"', type_doc)

    def test_every_type_is_open_and_mixed(self):
        for name, element in self.types.items():
            if element.tag != XS + 'complexType' or element.find(XS + 'simpleContent') is not None:
                continue
            self.assertEqual(element.get('mixed'), 'true', name)
            wildcards = list(element.iter(XS + 'any'))
            self.assertEqual([w.get('namespace') for w in wildcards], ['##local'], name)
            self.assertEqual(wildcards[0].get('processContents'), 'skip', name)
            self.assertIsNotNone(element.find(f'.//{XS}anyAttribute'), name)

    def test_every_name_used_is_defined(self):
        defined = set(self.types) | set(self.elements)
        used = set()
        for element in self.root.iter():
            for attribute in ('type', 'base', 'ref'):
                if element.get(attribute):
                    used.add(element.get(attribute))
            used.update((element.get('memberTypes') or '').split())
        missing = {name for name in used if not name.startswith('xs:') and name not in defined}
        self.assertEqual(missing, set())

    def test_names_are_xml_names(self):
        for name in list(self.types) + list(self.elements):
            self.assertRegex(name, r'^[A-Za-z_][\w.\-]*$')


class Validating(unittest.TestCase):
    """Xerces in the JDK, as the editors use it: 'schema full checking' off (VS Code's XML extension) and on (Rider)."""

    @classmethod
    def setUpClass(cls):
        if not shutil.which('java'):
            raise unittest.SkipTest('no Java runtime on PATH')
        cls.tmp = tempfile.TemporaryDirectory()
        folder = Path(cls.tmp.name)
        text, _schema = generated(folder)
        (folder / 'my.xsd').write_text(text, encoding='utf-8')
        (folder / 'good.xml').write_text(GOOD, encoding='utf-8')
        (folder / 'bad.xml').write_text(BAD, encoding='utf-8')
        files = [folder / 'good.xml', folder / 'bad.xml']
        cls.lax = xsd.java_validate(folder / 'my.xsd', files)
        cls.full = xsd.java_validate(folder / 'my.xsd', files, full=True)

    @classmethod
    def tearDownClass(cls):
        cls.tmp.cleanup()

    def problems(self, found, name):
        return next(v for k, v in found.items() if k.endswith(name))

    def test_the_schema_loads_as_the_editors_load_it(self):
        self.assertEqual(self.lax['schema'], [])

    def test_valid_xml_it_does_not_fully_describe_has_no_problems(self):
        self.assertEqual(self.problems(self.lax, 'good.xml'), [])
        self.assertEqual(self.problems(self.full, 'good.xml'), [])

    def test_wrong_values_are_flagged(self):
        for found in (self.lax, self.full):
            text = ' '.join(message for _kind, _line, message in self.problems(found, 'bad.xml'))
            self.assertIn("'fast'", text)
            self.assertIn("'Sideways'", text)
            self.assertIn("'yes'", text)

    def test_full_checking_reports_only_the_ambiguity_xsd_1_0_forbids(self):
        kinds = {re.match(r'[\w.\-]+', message).group(0) for _kind, _line, message in self.full['schema']}
        self.assertTrue(kinds)
        self.assertLessEqual(kinds, {'cos-nonambig', 'rcase-RecurseLax.2', 'derivation-ok-restriction.5.4.2'})


class OtherValidators(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.text, _schema = generated(self.tmp.name)

    def tearDown(self):
        self.tmp.cleanup()

    def test_libxml2_refuses_it_as_xsd_1_0_says(self):
        try:
            from lxml import etree
        except ImportError:
            self.skipTest('lxml is not installed')
        with self.assertRaises(etree.XMLSchemaParseError) as caught:
            etree.XMLSchema(etree.fromstring(self.text.encode('utf-8')))
        self.assertIn('not determinist', str(caught.exception))

    def test_an_xsd_1_1_validator_accepts_it(self):
        try:
            import xmlschema
        except ImportError:
            self.skipTest('xmlschema is not installed')
        schema = xmlschema.XMLSchema11(self.text)
        self.assertTrue(schema.is_valid(GOOD))
        self.assertFalse(schema.is_valid(BAD))


if __name__ == '__main__':
    unittest.main()
