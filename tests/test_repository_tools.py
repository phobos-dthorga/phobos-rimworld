"""Offline checks for the repository's own tooling: the mod layout check and the documentation link check.

Nothing here reads or writes the RimWorld installation; every mod below is synthetic.
"""
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


layout = load('scripts/check-mod-layout.py', 'check_mod_layout')
doc_links = load('scripts/check-doc-links.py', 'check_doc_links')

GOOD_ABOUT = b"""<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <packageId>PhobosDthorga.Example</packageId>
  <name>Phobos Example</name>
  <author>Phobos A. D'thorga</author>
  <description>A synthetic mod for the tests.</description>
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


def mod(name, about=GOOD_ABOUT, extra=None):
    """Files and contents for one synthetic, well-formed mod folder, plus any extra files."""
    contents = {
        f'mods/{name}/About/About.xml': about,
        f'mods/{name}/README.md': b'# Example\n',
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

    def test_missing_files_are_named(self):
        errors = '\n'.join(run({'mods/Bare/Defs/Thing.xml': b'<Defs />'})['errors'])
        for fragment in ('no About/About.xml', 'no README.md', 'no CHANGELOG.md'):
            self.assertIn(fragment, errors)

    def test_about_fields_are_checked(self):
        cases = {
            'no <name>': about_with(name=None),
            'no <description>': about_with(description=None),
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
