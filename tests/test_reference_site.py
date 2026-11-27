"""scripts/reference-site.py: reading XML-facing types from made-up C#, their XML examples and defaults, the summary check against
its known gaps, the guides' links, and the DocFX project made runnable. No DocFX, no dotnet, no game."""
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]

CATEGORIES = [
    {"name": "Rules Defs", "bases": ["PhobosRulesDef"], "form": "def"},
    {"name": "Inputs", "bases": ["VehicleSystemInput"], "form": "li", "container": "inputs"},
    {"name": "Conditions", "bases": ["VehicleSystemCondition"], "form": "li", "element": "when", "container": ""},
    {"name": "Comps", "bases": ["CompProperties", "CompProperties_*"], "form": "li", "container": "comps"},
    {"name": "Defs", "bases": ["Def", "*Def"], "form": "def"},
]

SOURCE = '''using Verse;

namespace My.Mod;

public enum Mode { Off, On }

/// <summary>A number read from the vehicle.</summary>
public abstract class VehicleSystemInput
{
    public SimpleCurve? curve;
    public float factor = 1f;
    protected abstract float Raw();
}

/// <summary>A fixed number.</summary>
public class Input_Constant : VehicleSystemInput
{
    /// <summary>The number. It never changes.</summary>
    public float value = 2.5f;
    public static float Shared = 3f;
    public readonly int fixedOne = 1;
    private int hidden = 4;
    protected override float Raw() => value;
}

public class When_Moving : VehicleSystemCondition
{
    public bool invert;
}

/// <summary>The comp's figures.</summary>
[StaticConstructorOnStartup]
public class CompProperties_Thing : CompProperties
{
    public Mode mode;
    public int count = Limits.Most, other;
    public IntRange span = new(1, 3);
    public List<string> tags = new() { "a", "b" };
    public List<ThingDef> things = new();
    public ThingDef? fuel;
    public string label = "tank";
    public float share = 21f / 175f;
    public SimpleCurve output = new() { new CurvePoint(0f, 0f), new CurvePoint(10f, 1f) };
    public CompProperties_Thing() => compClass = typeof(CompThing);
}

internal class CompProperties_Hidden : CompProperties
{
    public int x;
}

public class Outer
{
    public class InnerExtension : CompProperties { public int y; }
}

/// <summary>Rules.</summary>
public abstract class PhobosRulesDef : Def { }

/// <summary>The game's figures.</summary>
public class MyRulesDef : PhobosRulesDef
{
    public float speed = 0.5f;
}

public static class Limits
{
    public const int Most = 12;
}
'''


def load():
    spec = importlib.util.spec_from_file_location('reference_site', ROOT / 'scripts' / 'reference-site.py')
    module = importlib.util.module_from_spec(spec)
    sys.modules['reference_site'] = module      # dataclasses look their module up while the class is made
    spec.loader.exec_module(module)
    return module


site = load()


def source_in(folder):
    project = Path(folder) / 'src' / 'My.Mod'
    project.mkdir(parents=True)
    (project / 'Things.cs').write_text(SOURCE, encoding='utf-8')
    (project / 'obj').mkdir()
    (project / 'obj' / 'Generated.cs').write_text('public class CompProperties_Generated : CompProperties { }', encoding='utf-8')
    return site.read_source(folder, ['src/My.Mod'])


class Reading(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.source = source_in(self.tmp.name)
        self.chosen = {t.full: t for t in site.modder_types(self.source, CATEGORIES)}

    def tearDown(self):
        self.tmp.cleanup()

    def test_xml_facing_types_are_the_public_ones_with_a_matching_base(self):
        self.assertEqual(sorted(self.chosen), ['My.Mod.CompProperties_Thing', 'My.Mod.Input_Constant', 'My.Mod.MyRulesDef',
                                               'My.Mod.Outer.InnerExtension', 'My.Mod.PhobosRulesDef', 'My.Mod.When_Moving'])

    def test_the_nearest_base_decides_the_kind(self):
        self.assertEqual(self.chosen['My.Mod.MyRulesDef'].category, 'Rules Defs')
        self.assertEqual(self.chosen['My.Mod.PhobosRulesDef'].category, 'Defs')
        self.assertEqual(self.chosen['My.Mod.Input_Constant'].category, 'Inputs')

    def test_only_public_instance_fields_are_read(self):
        names = [f.name for f in self.chosen['My.Mod.Input_Constant'].fields]
        self.assertEqual(names, ['value'])
        names = [f.name for f in self.chosen['My.Mod.CompProperties_Thing'].fields]
        self.assertEqual(names, ['mode', 'count', 'other', 'span', 'tags', 'things', 'fuel', 'label', 'share', 'output'])

    def test_summaries_and_gaps(self):
        self.assertEqual(self.chosen['My.Mod.Input_Constant'].summary, 'A fixed number.')
        self.assertTrue(self.chosen['My.Mod.CompProperties_Thing'].documented)   # an attribute between it and its comment
        self.assertEqual(site.gaps(self.chosen.values()), ['My.Mod.Outer.InnerExtension', 'My.Mod.When_Moving'])

    def test_nested_types_are_named_with_a_plus_in_xml(self):
        self.assertEqual(self.chosen['My.Mod.Outer.InnerExtension'].xml_class, 'My.Mod.Outer+InnerExtension')

    def test_an_example_lists_inherited_fields_and_defaults(self):
        category = CATEGORIES[1]
        snippet, rows = site.xml_example(self.chosen['My.Mod.Input_Constant'], category, self.source.enums, self.source.constants)
        text = '\n'.join(snippet)
        self.assertTrue(text.startswith('<inputs>\n  <li Class="My.Mod.Input_Constant">'))
        self.assertIn('<value>2.5</value>  <!-- float: The number -->', text)
        self.assertIn('<!-- from VehicleSystemInput -->', text)
        self.assertIn('<factor>1</factor>', text)
        self.assertIn('<!-- <curve>…</curve>', text)
        self.assertEqual([r[0] for r in rows], ['value', 'curve', 'factor'])

    def test_a_type_added_in_code_says_so(self):
        info = self.chosen['My.Mod.Input_Constant']
        never = site.overwrite_page(info, CATEGORIES[1], self.source.enums, self.source.constants,
                                    {'type': info.full, 'never': True, 'note': 'added to every vehicle.'})
        self.assertIn('**Added for you:** added to every vehicle.', never)
        self.assertIn('**Never write it in XML.**', never)
        self.assertNotIn('```xml', never)                                   # nothing to copy
        self.assertIn('| `value` |', never)                                 # its fields still listed
        sometimes = site.overwrite_page(info, CATEGORIES[1], self.source.enums, self.source.constants,
                                        {'type': info.full, 'never': False, 'note': 'write it only on a trailer.'})
        self.assertIn('```xml', sometimes)
        catalogue = site.catalogue_page([info], CATEGORIES, '', {info.full: {}})
        self.assertIn('*(added for you)*', catalogue)

    def test_a_def_example_is_its_own_element(self):
        snippet, _ = site.xml_example(self.chosen['My.Mod.MyRulesDef'], CATEGORIES[0], self.source.enums, self.source.constants)
        self.assertEqual(snippet[:3], ['<Defs>', '  <My.Mod.MyRulesDef>', '    <defName>MyMod_Example</defName>'])
        self.assertIn('    <speed>0.5</speed>  <!-- float -->', snippet)

    def test_a_condition_without_fields_of_its_own_is_one_element(self):
        page = site.overwrite_page(self.chosen['My.Mod.When_Moving'], CATEGORIES[2], self.source.enums, self.source.constants)
        self.assertIn('uid: My.Mod.When_Moving', page)
        self.assertIn('<when Class="My.Mod.When_Moving">', page)
        self.assertIn('<invert>false</invert>', page)

    def test_the_catalogue_links_every_type(self):
        page = site.catalogue_page(list(self.chosen.values()), CATEGORIES, 'https://example.org/repo')
        self.assertIn('<xref:My.Mod.Input_Constant?text=Input_Constant>', page)
        self.assertIn('https://example.org/repo/blob/main/src/My.Mod/Things.cs#L', page)
        self.assertIn('## Rules Defs (1)', page)


class Defaults(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.source = source_in(self.tmp.name)
        self.fields = {f.name: f for f in self.source.types['My.Mod.CompProperties_Thing'].fields}

    def tearDown(self):
        self.tmp.cleanup()

    def shown(self, name):
        item = self.fields[name]
        return site.default_value(item.type, item.initial, self.source.enums, self.source.constants)

    def test_values(self):
        self.assertEqual(self.shown('mode').xml, 'Off')                 # an enum's zero member
        self.assertEqual(self.shown('count').xml, '12')                 # a constant's value
        self.assertEqual(self.shown('other').xml, '0')
        self.assertEqual(self.shown('span').xml, '1~3')
        self.assertEqual(self.shown('label').xml, 'tank')
        self.assertEqual(self.shown('share').xml, '0.12')
        self.assertIsNone(self.shown('fuel').xml)
        self.assertEqual(self.shown('fuel').text, 'none')

    def test_lists_and_curves(self):
        self.assertEqual(self.shown('tags').items, ['a', 'b'])
        self.assertEqual(self.shown('things').text, 'empty')
        curve = self.shown('output')
        self.assertEqual((curve.wrapper, curve.items), ('points', ['(0, 0)', '(10, 1)']))

    def test_literals(self):
        none = {}
        self.assertEqual(site.default_value('float', '1e3f', none, none).xml, '1e3')
        self.assertEqual(site.default_value('Color', 'new Color(1f, 0.5f, 0f)', none, none).xml, '(1, 0.5, 0)')
        self.assertEqual(site.default_value('int', 'GenDate.TicksPerHour', none, none).xml, '2500')
        self.assertEqual(site.default_value('Type', 'typeof(MyWorker)', none, none).xml, 'MyWorker')
        self.assertTrue(site.default_value('float', 'Mathf.Sqrt(2f)', none, none).text.startswith('set in code'))
        self.assertEqual(site.default_value('Settings', 'new()', none, none).text, 'a new Settings with its own defaults')


class Parsing(unittest.TestCase):
    def test_base_classes(self):
        self.assertEqual(site.base_of('public class A : B<C, D>, IThing where C : new()'), 'B<C, D>')
        self.assertEqual(site.base_of('public sealed class A : Verse.CompProperties'), 'Verse.CompProperties')
        self.assertIsNone(site.base_of('public class A'))

    def test_fields(self):
        self.assertEqual(site.parse_field('public Dictionary<string, float> map = new();'),
                         [('Dictionary<string, float>', 'map', 'new()')])
        self.assertEqual(site.parse_field('[Unsaved] public int a = 1, b;'), [('int', 'a', '1'), ('int', 'b', None)])
        self.assertEqual(site.parse_field('public const int A = 1;'), [])
        self.assertEqual(site.parse_field('protected int a;'), [])

    def test_summary_text(self):
        doc = '<summary>Reads <see cref="Foo.Bar"/> and <c>baz</c>\n &amp; more.</summary>\n<remarks>x</remarks>'
        self.assertEqual(site.summary_text(doc), 'Reads Bar and baz & more.')
        self.assertTrue(site.documented('<inheritdoc/>'))


class Checking(unittest.TestCase):
    def test_only_new_gaps_fail(self):
        with tempfile.TemporaryDirectory() as tmp:
            source_in(tmp)
            settings = {'projects': ['src/My.Mod'], 'categories': CATEGORIES,
                        'knownGaps': ['My.Mod.When_Moving', 'My.Mod.Gone']}
            lines = []
            self.assertEqual(site.check(tmp, settings, log=lines.append), 1)
            text = '\n'.join(lines)
            self.assertIn('My.Mod.Outer.InnerExtension (Comps) has no <summary>', text)
            self.assertIn('My.Mod.Gone', text)            # a known gap now fixed is named, so the list shrinks
            settings['knownGaps'].append('My.Mod.Outer.InnerExtension')
            self.assertEqual(site.check(tmp, settings, log=lines.append), 0)

    def test_the_baseline_is_written_into_the_config(self):
        with tempfile.TemporaryDirectory() as tmp:
            source_in(tmp)
            (Path(tmp) / 'config').mkdir()
            settings = {'projects': ['src/My.Mod'], 'categories': CATEGORIES, 'knownGaps': []}
            (Path(tmp) / site.CONFIG).write_text(json.dumps(settings), encoding='utf-8')
            site.write_baseline(tmp, settings, log=lambda _: None)
            written = json.loads((Path(tmp) / site.CONFIG).read_text(encoding='utf-8'))
            self.assertEqual(written['knownGaps'], ['My.Mod.Outer.InnerExtension', 'My.Mod.When_Moving'])


class Site(unittest.TestCase):
    def test_guide_links(self):
        text = ('[a](vehicle-systems-modding.md#pipes) [b](development/x.md#y) ![c](player-guide/images/p.png) '
                '[d](https://e.org) [e](#here) [f](../mods/PhobosFramework/)')
        out = site.rewrite_links(text, 'docs/framework-modding.md',
                                 ['docs/framework-modding.md', 'docs/vehicle-systems-modding.md'], 'https://g/r')
        self.assertIn('[a](vehicle-systems-modding.md#pipes)', out)
        self.assertIn('[b](https://g/r/blob/main/docs/development/x.md#y)', out)
        self.assertIn('![c](https://g/r/raw/main/docs/player-guide/images/p.png)', out)
        self.assertIn('[d](https://e.org)', out)
        self.assertIn('[e](#here)', out)
        self.assertIn('[f](https://g/r/tree/main/mods/PhobosFramework)', out)

    def test_pages_get_a_uid(self):
        self.assertTrue(site.with_uid('# Title', 'xml-types').startswith('---\nuid: xml-types\n---\n\n# Title'))
        self.assertEqual(site.guide_uid('docs/framework-modding.md'), 'guide-framework-modding')

    def test_the_project_is_made_runnable_from_elsewhere(self):
        project = {'metadata': [{'src': [{'src': '../../src/A', 'files': ['**/*.cs']}], 'references': []}], 'build': {}}
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp) / 'docs' / 'reference'
            refs = [(Path(tmp) / 'refs', {'exclude': ['Microsoft.*.dll']})]
            config = site.effective_config(project, tmp, folder, refs)
            self.assertEqual(config['metadata'][0]['src'][0]['src'], (Path(tmp) / 'src' / 'A').resolve().as_posix())
            self.assertEqual(config['metadata'][0]['references'],
                             [{'src': (Path(tmp) / 'refs').as_posix(), 'files': ['*.dll'], 'exclude': ['Microsoft.*.dll']}])
            self.assertEqual(project['metadata'][0]['references'], [])      # the committed project is left as it was

    def test_the_footer_says_when_and_from_what_it_was_built(self):
        from datetime import datetime, timezone
        when = datetime(2026, 11, 28, 4, 5, tzinfo=timezone.utc)
        answers = {'rev-parse': 'abc1234', 'status': ''}
        git = lambda _root, command, *_args: answers[command]                        # noqa: E731
        with tempfile.TemporaryDirectory() as tmp:
            about = Path(tmp) / 'About.xml'
            about.write_text('<ModMetaData>\n  <modVersion>0.365.0</modVersion>\n</ModMetaData>', encoding='utf-8')
            settings = {'stamp': {'name': "D'thorga Framework", 'about': 'About.xml'}}
            self.assertEqual(site.build_stamp(tmp, settings, when, git),
                             "Built 28 Nov 2026, 04:05 UTC, from D'thorga Framework 0.365.0 (commit abc1234).")
            answers['status'] = ' M src/A.cs'
            self.assertIn('(commit abc1234, with uncommitted changes)', site.build_stamp(tmp, settings, when, git))
            self.assertEqual(site.build_stamp(tmp, {}, when, lambda *_: None), 'Built 28 Nov 2026, 04:05 UTC.')
        project = {'build': {'globalMetadata': {'_appFooter': 'Our footer.'}}}
        config = site.stamp_footer(json.loads(json.dumps(project)), 'Built today <now>.')
        self.assertEqual(config['build']['globalMetadata']['_appFooter'], 'Our footer. Built today &lt;now&gt;.')
        self.assertEqual(project['build']['globalMetadata']['_appFooter'], 'Our footer.')

    def test_reference_folders_come_from_the_targets_and_the_nuget_cache(self):
        with tempfile.TemporaryDirectory() as tmp:
            (Path(tmp) / 'Directory.Build.targets').write_text(
                '<PackageReference Include="Krafs.Rimworld.Ref" Version="1.7.1-x" />', encoding='utf-8')
            packages = Path(tmp) / 'nuget'
            (packages / 'krafs.rimworld.ref' / '1.7.1-x' / 'ref' / 'net10.0').mkdir(parents=True)
            settings = {'references': [{'package': 'Krafs.Rimworld.Ref', 'folder': 'ref/net10.0'}]}
            found, missing = site.reference_folders(tmp, settings, environ={'NUGET_PACKAGES': str(packages)})
            self.assertEqual((len(found), missing), (1, []))
            settings['references'].append({'package': 'Lib.Harmony', 'folder': 'lib/net10.0'})
            with self.assertRaises(site.SiteError):
                site.reference_folders(tmp, settings, environ={'NUGET_PACKAGES': str(packages)})

    def test_a_folder_it_did_not_make_is_never_emptied(self):
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp) / 'out'
            folder.mkdir()
            (folder / 'precious.txt').write_text('keep', encoding='utf-8')
            with self.assertRaises(site.SiteError):
                site.prepare_folder(folder)
            self.assertTrue((folder / 'precious.txt').is_file())
            (folder / 'precious.txt').unlink()
            site.prepare_folder(folder)                    # empty: taken, and marked
            (folder / 'old.html').write_text('x', encoding='utf-8')
            site.prepare_folder(folder)                    # marked: emptied
            self.assertEqual(sorted(p.name for p in folder.iterdir()), [site.MARKER])

    def test_the_committed_project_and_config_agree(self):
        project = json.loads((ROOT / site.PROJECT / 'docfx.json').read_text(encoding='utf-8'))
        settings = site.load_settings(ROOT)
        sources = {Path(e['src']).name for e in project['metadata'][0]['src']}
        for folder in settings['projects']:
            self.assertIn(Path(folder).name, sources, f'{folder} has XML examples but DocFX does not read it')
        for guide in settings['guides']:
            self.assertTrue((ROOT / guide).is_file(), guide)
        names = [Path(guide).name for guide in settings['guides']]
        self.assertEqual(len(names), len(set(names)), 'the guides share one folder on the site, so their file names must differ')
        with_types = [p for p in sorted((ROOT / 'src').iterdir()) if p.is_dir() and p.name != 'Shared'
                      and not p.name.startswith('PhobosPatches') and f'src/{p.name}' not in settings['projects']
                      and site.modder_types(site.read_source(ROOT, [f'src/{p.name}']), settings['categories'])]
        self.assertEqual(with_types, [], 'a project with XML-facing types is missing from the site (config "projects")')
        facing = {t.full for t in site.modder_types(site.read_source(ROOT, settings['projects']), settings['categories'])}
        unknown = sorted(set(site.added_in_code(settings)) - facing)
        self.assertEqual(unknown, [], 'config "addedInCode" names a type that is not XML-facing (renamed or gone?)')
        self.assertIsNotNone(site.mod_version(ROOT, settings['stamp']['about']), 'the stamp\'s About.xml gives a version')


class Verifying(unittest.TestCase):
    """The gate's step builds into a throwaway folder, fails on a site warning, and always removes the folder."""

    def run_verify(self, outcome):
        folders = []

        def fake_build(root, settings, work, site_folder, docfx, log, restore):
            folders.append(Path(work).parent)
            (Path(work) / 'docfx-build.log').write_text('Build succeeded with warning.\n  warning InvalidCrossReference: x\n',
                                                         encoding='utf-8')
            if outcome == 'error':
                raise site.SiteError('DocFX could not build the site (1 errors)')
            return 10, 8, 3, (2 if outcome == 'warnings' else 0)

        lines = []
        original = site.build
        site.build = fake_build
        try:
            code = site.verify(ROOT, {}, log=lines.append)
        finally:
            site.build = original
        self.assertFalse(folders[0].exists(), 'the throwaway folder is removed')
        return code, lines

    def test_a_clean_site_passes(self):
        self.assertEqual(self.run_verify('clean')[0], 0)

    def test_a_site_warning_fails_and_is_shown(self):
        code, lines = self.run_verify('warnings')
        self.assertEqual(code, 1)
        self.assertTrue(any('InvalidCrossReference' in line for line in lines))

    def test_a_docfx_error_fails(self):
        self.assertEqual(self.run_verify('error')[0], 1)


if __name__ == '__main__':
    unittest.main()
