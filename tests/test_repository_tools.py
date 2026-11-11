"""Offline checks for the repository's own tooling: the mod layout check and the documentation link check.

Nothing here reads or writes the RimWorld installation; every mod below is synthetic.
"""
import importlib.util
import json
import os
import re
from pathlib import Path
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


layout = load('scripts/check-mod-layout.py', 'check_mod_layout')
doc_links = load('scripts/check-doc-links.py', 'check_doc_links')

GOOD_ABOUT = b"""<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <packageId>PhobosDthorga.Example</packageId>
  <name>Phobos Example</name>
  <author>Phobos A. D'thorga</author>
  <description>A synthetic mod for the tests, by House D'thorga (also spelt dthorga). Portions of the materials used to create this content/mod are trademarks and/or
  copyrighted works of Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.</description>
  <modVersion>0.1.0</modVersion>
  <supportedVersions>
    <li>1.6</li>
  </supportedVersions>
</ModMetaData>
"""

GOOD_LOAD_FOLDERS = b"""<loadFolders>
  <v1.6>
    <li>/</li>
    <li>Common</li>
    <li>1.6</li>
    <li IfModActive="CETeam.CombatExtended">Mods/CombatExtended</li>
  </v1.6>
</loadFolders>
"""


GOOD_README = b"# Example\n\nA synthetic mod. One of House D'thorga's mods (also spelt dthorga).\n\n## Read this first\n"


def mod(name, about=GOOD_ABOUT, extra=None):
    """Files and contents for one synthetic, well-formed mod folder, plus any extra files."""
    contents = {
        f'mods/{name}/About/About.xml': about,
        f'mods/{name}/README.md': GOOD_README,
        f'mods/{name}/CHANGELOG.md': b'# Example changelog\n',
    }
    for rel, data in (extra or {}).items():
        contents[f'mods/{name}/{rel}'] = data
    return contents


def run(contents):
    return layout.check(ROOT, list(contents), contents.__getitem__)


def about_with(**fields):
    """GOOD_ABOUT with fields replaced; a value of None removes the field."""
    text = GOOD_ABOUT.decode()
    for field, value in fields.items():
        start, end = text.index(f'<{field}>'), text.index(f'</{field}>') + len(f'</{field}>')
        text = text[:start] + ('' if value is None else f'<{field}>{value}</{field}>') + text[end:]
    return text.encode()


class LayoutCheckTests(unittest.TestCase):
    def test_repository_layout_is_valid(self):
        self.assertEqual(layout.check(ROOT)['errors'], [])

    def test_a_well_formed_mod_passes(self):
        contents = mod('Example', extra={
            'LoadFolders.xml': GOOD_LOAD_FOLDERS,
            'Common/Defs/Example.xml': b'<Defs />',
            'Mods/CombatExtended/Patches/Example.xml': b'<Patch />',
        })
        result = run(contents)
        self.assertEqual(result['errors'], [])
        self.assertEqual(result['mods'], ['Example'])

    def test_version_copies_in_src_must_match_the_about_file(self):
        version = re.search(rb'<modVersion>([^<]+)</modVersion>', GOOD_ABOUT).group(1).decode()
        contents = mod('Example')
        contents['src/Example/Example.csproj'] = f'<Project><ModFolder>Example</ModFolder><Version>{version}</Version></Project>'.encode()
        contents['src/Example/ExampleMod.cs'] = f'public const string ModVersion = "{version}";'.encode()
        self.assertEqual(run(contents)['errors'], [])
        contents['src/Example/Example.csproj'] = b'<Project><ModFolder>Example</ModFolder><Version>9.9.9</Version></Project>'
        contents['src/Example/ExampleMod.cs'] = b'public const string ModVersion = "9.9.8";'
        errors = '\n'.join(run(contents)['errors'])
        self.assertIn('<Version> 9.9.9 differs', errors)
        self.assertIn('ModVersion 9.9.8 differs', errors)

    def test_an_item_def_name_ending_in_a_digit_is_refused(self):
        def errors_for(xml):
            return '\n'.join(run(mod('Example', extra={'Common/Defs/Example.xml': xml}))['errors'])
        self.assertIn('ExampleCaseMk82 ends in a digit', errors_for(b'<Defs><ThingDef><defName>ExampleCaseMk82</defName></ThingDef></Defs>'))
        # Inside a patch's value, which is where the real ones were, and on a subclass of ThingDef.
        self.assertIn('ExampleCaseMk84 ends in a digit', errors_for(
            b'<Patch><Operation><value><ThingDef><defName>ExampleCaseMk84</defName></ThingDef></value></Operation></Patch>'))
        self.assertIn('ExampleTruck2 ends in a digit', errors_for(b'<Defs><Vehicles.VehicleDef><defName>ExampleTruck2</defName></Vehicles.VehicleDef></Defs>'))
        # Other kinds of Def are not read that way.
        self.assertEqual(errors_for(b'<Defs><ThingDef><defName>ExampleCase</defName></ThingDef><RecipeDef><defName>Make2</defName></RecipeDef></Defs>'), '')

    def test_a_bench_of_ours_needs_a_work_giver(self):
        def errors_for(xml):
            return '\n'.join(run(mod('Example', extra={'Common/Defs/Example.xml': xml}))['errors'])
        bench = b'<ThingDef><defName>ExampleBench</defName></ThingDef>'
        recipe = b'<RecipeDef><defName>MakeThing</defName><recipeUsers><li>ExampleBench</li></recipeUsers></RecipeDef>'
        self.assertIn('no WorkGiverDef lists it', errors_for(b'<Defs>' + bench + recipe + b'</Defs>'))
        giver = b'<WorkGiverDef><defName>DoExample</defName><fixedBillGiverDefs><li>ExampleBench</li></fixedBillGiverDefs></WorkGiverDef>'
        self.assertEqual(errors_for(b'<Defs>' + bench + recipe + giver + b'</Defs>'), '')
        # Added to another work giver by a patch counts too.
        patch = (b'<Patch><Operation Class="PatchOperationAdd"><xpath>Defs/WorkGiverDef[defName="DoBillsMachiningTable"]/fixedBillGiverDefs'
                 b'</xpath><value><li>ExampleBench</li></value></Operation></Patch>')
        contents = mod('Example', extra={'Common/Defs/Example.xml': b'<Defs>' + bench + recipe + b'</Defs>', 'Common/Patches/Givers.xml': patch})
        self.assertEqual(run(contents)['errors'], [])
        # A bench of the game's or another mod's is theirs to list.
        self.assertEqual(errors_for(b'<Defs><RecipeDef><defName>MakeThing</defName><recipeUsers><li>TableMachining</li></recipeUsers></RecipeDef></Defs>'), '')

    def test_every_version_copy_is_in_the_constants_catalogue(self):
        version = re.search(rb'<modVersion>([^<]+)</modVersion>', GOOD_ABOUT).group(1).decode()
        contents = mod('Example')
        contents['src/Example/Example.csproj'] = f'<Project><ModFolder>Example</ModFolder><Version>{version}</Version></Project>'.encode()
        contents['src/Example/ExampleMod.cs'] = f'public const string ModVersion = "{version}";'.encode()
        targets = [{'path': 'mods/Example/About/About.xml'}, {'path': 'src/Example/Example.csproj'}]
        contents['config/maintained-constants.json'] = json.dumps({'constants': {'Example.version': {'targets': targets}}}).encode()
        errors = '\n'.join(run(contents)['errors'])
        self.assertIn('src/Example/ExampleMod.cs carries mods/Example\'s version', errors)
        targets.append({'path': 'src/Example/ExampleMod.cs'})
        contents['config/maintained-constants.json'] = json.dumps({'constants': {'Example.version': {'targets': targets}}}).encode()
        self.assertEqual(run(contents)['errors'], [])
        contents['config/maintained-constants.json'] = b'{"constants": {}}'
        self.assertIn('no Example.version', '\n'.join(run(contents)['errors']))

    def test_translated_keys_and_their_placeholders_must_exist(self):
        keyed = (b'<LanguageData><PhobosEx_Hello>Hello {PAWN_labelShort}, {COUNT} left</PhobosEx_Hello>'
                 b'<PhobosEx_Tab>Tab</PhobosEx_Tab></LanguageData>')
        contents = mod('Example', extra={'Languages/English/Keyed/Example.xml': keyed})
        good = ('class A { void B() { labelKey = "PhobosEx_Tab"; '
                'Log("PhobosEx_Hello".Translate(pawn.Named("PAWN"), (n + 1).Named("COUNT"))); '
                'Log("PhobosEx_Hello".Translate(args, more)); } }')
        contents['src/Example/A.cs'] = good.encode()
        self.assertEqual(run(contents)['errors'], [])
        contents['src/Example/A.cs'] = ('class A { void B() { labelKey = "PhobosEx_NoTab";\n'
                                        'Log("PhobosEx_Missing".Translate());\n'
                                        'Log("PhobosEx_Hello".Translate(pawn.Named("PAWN"), Fmt("x,y").Named("COUNTS"))); } }').encode()
        errors = '\n'.join(run(contents)['errors'])
        self.assertIn('src/Example/A.cs:1: tab label "PhobosEx_NoTab" has no English Keyed entry', errors)
        self.assertIn('src/Example/A.cs:2: "PhobosEx_Missing" has no English Keyed entry', errors)
        self.assertIn('src/Example/A.cs:3: "PhobosEx_Hello" is translated without {COUNT}', errors)

    def test_the_real_listing_includes_the_projects_the_version_check_reads(self):
        # The version check once saw no projects at all, because the listing held only mods/ (found 7 Nov 2026).
        listing = layout.listed_files(ROOT)
        self.assertTrue(any(name.startswith('src/') and name.endswith('.csproj') for name in listing))
        self.assertIn(layout.CATALOGUE, listing)

    def test_missing_files_are_named(self):
        errors = '\n'.join(run({'mods/Bare/Defs/Thing.xml': b'<Defs />'})['errors'])
        for fragment in ('no About/About.xml', 'no README.md', 'no CHANGELOG.md'):
            self.assertIn(fragment, errors)

    def test_about_fields_are_checked(self):
        cases = {
            'no <name>': about_with(name=None),
            'no <description>': about_with(description=None),
            'lacks the disclaimer': about_with(description='A synthetic mod. Not official and not endorsed by Ludeon Studios.'),
            'no <author> or <authors>': about_with(author=None),
            'lists no <supportedVersions>': about_with(supportedVersions=None),
            'is not a game version': about_with(supportedVersions='<li>1.6.4871</li>'),
            'is not MAJOR.MINOR.PATCH': about_with(modVersion='1.0'),
            'letters and digits': about_with(packageId='PhobosDthorga.Radio-Framework'),
            'does not start with PhobosDthorga.': about_with(packageId='Someone.Radio'),
            'not <ModMetaData>': b'<Mod><name>x</name></Mod>',
            'not valid XML': b'<ModMetaData>',
        }
        for fragment, about in cases.items():
            with self.subTest(fragment=fragment):
                self.assertIn(fragment, '\n'.join(run(mod('Example', about=about))['errors']))

    def test_search_spelling_is_required_in_the_about_description(self):
        # A packageId-like word or a URL does not count: people and search engines do not read them as the name.
        for description in ('A mod by House D\'thorga.', 'See PhobosDthorga.Example.', 'See github.com/phobos-dthorga/x.'):
            with self.subTest(description=description):
                about = about_with(description=description + ' ' + ' '.join(layout.EULA_DISCLAIMER))
                self.assertIn('lacks the spelling "dthorga"', '\n'.join(run(mod('Example', about=about))['errors']))

    def test_search_spelling_is_required_in_the_readme_opening(self):
        cases = {
            'none at all': b"# Example\n\nOne of House D'thorga's mods.\n",
            'only after the first section': b"# Example\n\nA mod.\n\n## More\n\nAlso spelt dthorga.\n",
            'only in a URL': b"# Example\n\nSee https://github.com/phobos-dthorga/phobos-rimworld.\n",
        }
        for case, readme in cases.items():
            with self.subTest(case=case):
                errors = run(mod('Example', extra={'README.md': readme}))['errors']
                self.assertEqual(len(errors), 1)
                self.assertIn('README.md lacks the spelling "dthorga"', errors[0])
        self.assertEqual(run(mod('Example', extra={'README.md': b'# Example\n\nAlso spelt Dthorga.\n'}))['errors'], [])

    def test_authors_list_satisfies_author(self):
        about = about_with(author=None).replace(b'</ModMetaData>',
                                                b'<authors><li>Phobos A. D\'thorga</li></authors></ModMetaData>')
        self.assertEqual(run(mod('Example', about=about))['errors'], [])

    def test_package_prefix_ignores_case(self):
        self.assertEqual(run(mod('Example', about=about_with(packageId='phobosdthorga.example')))['errors'], [])

    def test_duplicate_package_ids_are_refused(self):
        contents = {**mod('One'), **mod('Two', about=about_with(packageId='phobosdthorga.EXAMPLE'))}
        errors = run(contents)['errors']
        self.assertEqual(len(errors), 1)
        self.assertIn('used by One, Two', errors[0])

    def test_load_folders_must_exist_except_version_folders(self):
        errors = '\n'.join(run(mod('Example', extra={'LoadFolders.xml': GOOD_LOAD_FOLDERS}))['errors'])
        self.assertIn('names Common under <v1.6>', errors)
        self.assertIn('names Mods/CombatExtended under <v1.6>', errors)
        self.assertNotIn('names 1.6', errors)

    def test_the_last_listed_folder_wins_a_shared_path(self):
        loaded, skipped = layout.files_the_game_loads([
            ('/', []), ('Common', ['Patches/A.xml', 'Defs/B.xml']), ('Mods/X', ['Patches/A.xml']), ('Mods/Y', ['Patches/A.xml'])])
        self.assertEqual(loaded, {'Patches/A.xml': 'Mods/Y', 'Defs/B.xml': 'Common'})
        self.assertEqual(skipped, [('Mods/X', 'Patches/A.xml', 'Mods/Y'), ('Common', 'Patches/A.xml', 'Mods/Y')])

    def test_shared_paths_across_load_folders_are_refused(self):
        contents = mod('Example', extra={
            'LoadFolders.xml': GOOD_LOAD_FOLDERS,
            'Common/Patches/Granulator.xml': b'<Patch />',
            'Common/Textures/Icon.png': b'',
            'Mods/CombatExtended/Patches/Granulator.xml': b'<Patch />',
            'Mods/CombatExtended/Textures/Icon.png': b'',
        })
        self.assertEqual(run(contents)['errors'], [
            'Example: under <v1.6>, Common/Patches/Granulator.xml is never loaded, because '
            'Mods/CombatExtended/Patches/Granulator.xml has the same path and its folder is listed later; '
            'give one of them another name'])

    def test_numbered_placeholders_are_refused(self):
        numbered = mod('Example', extra={'Common/Languages/English/Keyed/Example.xml': b'<LanguageData><Key>{0} of {1}</Key></LanguageData>'})
        self.assertIn('numbered placeholder', '\n'.join(run(numbered)['errors']))
        named = mod('Example', extra={'Common/Languages/English/Keyed/Example.xml': b'<LanguageData><Key>{COUNT} of {TOTAL}</Key></LanguageData>'})
        self.assertEqual(run(named)['errors'], [])

    def test_build_products_are_refused(self):
        errors = '\n'.join(run(mod('Example', extra={
            '1.6/Assemblies/PhobosExample.dll': b'',
            'Source/obj/project.assets.json': b'',
        }))['errors'])
        self.assertIn('PhobosExample.dll is a build product', errors)
        self.assertIn('obj/project.assets.json is a build product', errors)


class DocLinkCheckTests(unittest.TestCase):
    def test_repository_links_resolve(self):
        checked, failures = doc_links.check(ROOT)
        self.assertEqual(failures, [])
        self.assertGreater(checked, 0)

    def test_missing_targets_and_escapes_are_reported(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'docs').mkdir()
            (root / 'docs' / 'guide.md').write_text('# Guide\n', encoding='utf-8')
            (root / 'README.md').write_text(
                '[guide](docs/guide.md#top) [gone](docs/gone.md) [out](../elsewhere.md) '
                '[web](https://example.com/a.md)\n\n```text\n[fenced](docs/fenced.md)\n```\n', encoding='utf-8')
            checked, failures = doc_links.check(root, ['README.md', 'docs/guide.md'])
        self.assertEqual(checked, 3)
        self.assertEqual(failures, ['README.md: missing docs/gone.md',
                                    'README.md: ../elsewhere.md points outside the repository'])

    def test_unlisted_files_count_as_missing(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / 'secret.md').write_text('# Ignored locally\n', encoding='utf-8')
            (root / 'README.md').write_text('[secret](secret.md)\n', encoding='utf-8')
            self.assertEqual(doc_links.check(root, ['README.md'])[1], ['README.md: missing secret.md'])


if __name__ == '__main__':
    unittest.main()
