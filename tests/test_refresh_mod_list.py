"""Offline checks for scripts/refresh-mod-list.py, on a synthetic game. Nothing here reads the real installation."""
import importlib.util
import json
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


refresh = load('scripts/refresh-mod-list.py', 'refresh_mod_list')

HARMONY = '''<ModMetaData><name>Harmony</name><author>Brrainz</author><packageId>brrainz.harmony</packageId>
<supportedVersions><li>1.6</li><li>1.7</li></supportedVersions></ModMetaData>'''
CORE = '<ModMetaData><name>Core</name><packageId>Ludeon.RimWorld</packageId></ModMetaData>'
EXAMPLE = '''<?xml version="1.0" encoding="utf-8"?>
<ModMetaData><name>Example & Co</name><authors><li>Ann</li><li>Bo</li><li>Cy</li></authors>
<PackageId>Example.A</PackageId><supportedVersions><li>1.6</li></supportedVersions>
<modDependencies>
  <li><packageId>brrainz.harmony</packageId><displayName>Harmony</displayName></li>
  <li><packageId>old.lib</packageId><alternativePackageIds><li>Example.B</li></alternativePackageIds></li>
</modDependencies>
<modDependenciesByVersion><v1.6><li><packageId>missing.lib</packageId><displayName>Missing Lib</displayName></li></v1.6></modDependenciesByVersion>
<loadAfter><li>Example.B</li></loadAfter>
<incompatibleWith><li>example.c</li></incompatibleWith>
</ModMetaData>'''
B_MOD = '''<ModMetaData><name>B</name><packageId>example.b</packageId>
<supportedVersions><li>1.5</li></supportedVersions></ModMetaData>'''
C_MOD = '''<ModMetaData><name>C [sbz]</name><packageId>example.c</packageId>
<supportedVersions><li>1.6</li></supportedVersions></ModMetaData>'''
OFF_MOD = '''<ModMetaData><name>Off | Mod</name><packageId>example.off</packageId>
<supportedVersions><li>1.6</li></supportedVersions></ModMetaData>'''
CE_MOD = '''<ModMetaData><name>Combat Extended</name><packageId>CETeam.CombatExtended</packageId>
<supportedVersions><li>1.6</li></supportedVersions></ModMetaData>'''
MODS_CONFIG = '''<?xml version="1.0"?><ModsConfigData><version>1.6.4871 rev590</version><activeMods>
<li>brrainz.harmony</li><li>ludeon.rimworld</li><li>example.a</li><li>example.b</li><li>example.c</li>
<li>ceteam.combatextended</li><li>ghost.mod</li></activeMods></ModsConfigData>'''
MANIFEST = '''"AppWorkshop"
{
	"WorkshopItemsInstalled"
	{
		"200"
		{
			"size"		"1"
			"timeupdated"		"1760400000"
			"manifest"		"1"
		}
	}
}'''
DOCUMENT = '''# List

<!-- BEGIN GENERATED: summary -->
old
<!-- END GENERATED: summary -->

<!-- BEGIN GENERATED: checks -->
<!-- END GENERATED: checks -->

<!-- BEGIN SUGGESTIONS -->
[Off](https://steamcommunity.com/sharedfiles/filedetails/?id=500) and [New](https://steamcommunity.com/sharedfiles/filedetails/?id=999)
<!-- END SUGGESTIONS -->

<!-- BEGIN GENERATED: vanilla-expanded -->
<!-- END GENERATED: vanilla-expanded -->

<!-- BEGIN GENERATED: active -->
<!-- END GENERATED: active -->

<!-- BEGIN GENERATED: inactive -->
<!-- END GENERATED: inactive -->
'''


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8')


def fake_install(root):
    """(game, workshop, config) folders laid out as Steam lays them out."""
    root = Path(root)
    game = root / 'steamapps' / 'common' / 'RimWorld'
    workshop = root / 'steamapps' / 'workshop' / 'content' / '294100'
    config = root / 'Config'
    write(game / 'Data' / 'Core' / 'About' / 'About.xml', CORE)
    (game / 'Mods').mkdir(parents=True)
    write(workshop / '100' / 'About' / 'About.xml', HARMONY)
    write(workshop / '200' / 'about' / 'about.xml', EXAMPLE)
    write(workshop / '300' / 'About' / 'About.xml', B_MOD)
    write(workshop / '400' / 'About' / 'About.xml', C_MOD)
    write(workshop / '500' / 'About' / 'About.xml', OFF_MOD)
    write(workshop / '600' / 'About' / 'About.xml', CE_MOD)
    (workshop / '600' / 'ModPatches' / 'Vanilla Weapons Expanded').mkdir(parents=True)
    (workshop / '700').mkdir()
    write(root / 'steamapps' / 'workshop' / 'appworkshop_294100.acf', MANIFEST)
    write(config / 'ModsConfig.xml', MODS_CONFIG)
    return game, workshop, config


COLLECTION = {'collection': '42', 'checked': '2026-10-22', 'items': [
    {'id': '700', 'title': 'Vanilla Weapons Expanded', 'versions': ['1.5', '1.6'], 'updated': '2026-10-14'},
    {'id': '500', 'title': 'Vanilla Animals Expanded \u2014 Endangered', 'versions': ['1.6'], 'updated': '2026-10-14'},
    {'id': '800', 'title': 'Vanilla Old Expanded', 'versions': ['1.5'], 'updated': '2025-01-01'},
]}


class ReadingTests(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.game, self.workshop, self.config = fake_install(self.folder.name)
        self.collection_file = Path(self.folder.name) / 'collection.json'
        self.collection_file.write_text(json.dumps(COLLECTION), encoding='utf-8')
        self.mod_list = refresh.load(self.game, self.workshop, self.config, self.collection_file)

    def tearDown(self):
        self.folder.cleanup()

    def test_mods_config(self):
        self.assertEqual('1.6', self.mod_list.game_version)
        self.assertEqual('1.6.4871 rev590', self.mod_list.full_version)
        self.assertEqual('brrainz.harmony', self.mod_list.active[0])

    def test_about_files_are_read_forgivingly(self):
        mods = self.mod_list.mods
        example = mods['example.a']
        self.assertEqual('Example & Co', example.name)  # bare ampersand repaired
        self.assertEqual('200', example.workshop_id)
        self.assertEqual('Ann, Bo, Cy', example.authors)
        self.assertEqual('Ludeon', mods['ludeon.rimworld'].source)
        self.assertNotIn('', mods)  # the folder without an About.xml is skipped

    def test_checks(self):
        found = refresh.check(self.mod_list)
        self.assertEqual([('Example & Co', 'Missing Lib')], found['missing'])  # old.lib is satisfied by Example.B
        self.assertEqual(['ghost.mod'], found['not_installed'])
        self.assertEqual([('B', '1.5')], found['version'])
        self.assertEqual([('Example & Co', 'after', 'B')], found['order'])
        self.assertEqual([('Example & Co', 'C [sbz]')], found['incompatible'])

    def test_workshop_times_and_ce_patches(self):
        self.assertEqual({'200': 1760400000}, self.mod_list.times)
        self.assertEqual({'vanillaweaponsexpanded'}, self.mod_list.ce_patches)

    def test_render_fills_every_block_and_keeps_the_rest(self):
        text = refresh.render(DOCUMENT, self.mod_list)
        self.assertIn('**7 mods switched on**', text)
        self.assertIn('**1 more subscribed but switched off**', text)
        self.assertIn('Example & Co needs **Missing Lib**', text)
        self.assertIn('[C \\[sbz\\]](https://steamcommunity.com/sharedfiles/filedetails/?id=400)', text)
        self.assertIn('Off \\| Mod', text)  # the switched-off mod, with its pipe escaped
        self.assertIn('2025-10-14', text)  # Workshop update date from the manifest
        self.assertIn('Off \\| Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=500): **subscribed, off**', text)
        self.assertNotIn('id=999): ', text)  # a suggestion you don't have is not reported
        self.assertIn('[New](https://steamcommunity.com/sharedfiles/filedetails/?id=999)', text)  # hand-written text kept
        self.assertNotIn('\nold\n', text)
        self.assertEqual(text, refresh.render(text, self.mod_list))  # stable on a second run

    def test_collection_table(self):
        body = refresh.render_collection(self.mod_list)
        self.assertIn('3 mods', body)
        self.assertIn('| CE patch |', body)
        rows = [line for line in body.splitlines() if line.startswith('| [')]
        self.assertTrue(rows[0].startswith('| [Vanilla Animals Expanded'))  # sorted by series, em dash or not
        self.assertIn('| Subscribed, off | — |', rows[0])
        self.assertIn('| No | — | **no** |', rows[1])
        self.assertIn('| No | yes | yes |', rows[2])  # CE patch matched by name; the empty folder 700 isn't a mod


class BlockTests(unittest.TestCase):
    def test_replaces_only_the_named_block(self):
        text = '<!-- BEGIN GENERATED: a -->\nx\n<!-- END GENERATED: a -->\nkeep\n<!-- BEGIN GENERATED: b -->\ny\n<!-- END GENERATED: b -->'
        result = refresh.replace_block(text, 'a', 'new')
        self.assertIn('<!-- BEGIN GENERATED: a -->\nnew\n<!-- END GENERATED: a -->', result)
        self.assertIn('keep\n<!-- BEGIN GENERATED: b -->\ny\n', result)

    def test_missing_block_is_an_error(self):
        with self.assertRaises(ValueError):
            refresh.replace_block('no blocks here', 'a', 'new')

    def test_the_real_document_has_every_block(self):
        text = refresh.DOC.read_text(encoding='utf-8')
        for name in refresh.BLOCKS:
            self.assertIn(f'<!-- BEGIN GENERATED: {name} -->', text)
            self.assertIn(f'<!-- END GENERATED: {name} -->', text)
        self.assertRegex(text, r'<!-- BEGIN SUGGESTIONS -->(.|\n)*<!-- END SUGGESTIONS -->')


class CollectionTests(unittest.TestCase):
    def test_fetch_collection_without_network(self):
        calls = []

        def post(method, data):
            calls.append(method)
            if method.startswith('GetCollectionDetails'):
                return {'collectiondetails': [{'children': [
                    {'publishedfileid': '2', 'sortorder': 1}, {'publishedfileid': '1', 'sortorder': 0},
                    {'publishedfileid': '3', 'sortorder': 2}]}]}
            return {'publishedfiledetails': [
                {'publishedfileid': '1', 'title': 'One', 'time_updated': 1760400000,
                 'tags': [{'tag': 'Mod'}, {'tag': '1.6'}, {'tag': '1.10'}, {'tag': '1.5'}]},
                {'publishedfileid': '2', 'title': 'Two', 'tags': []},
                {'publishedfileid': '3', 'result': 9}]}  # removed from the Workshop

        import datetime
        result = refresh.fetch_collection('42', post=post, today=datetime.date(2026, 10, 22))
        self.assertEqual(['GetCollectionDetails/v1/', 'GetPublishedFileDetails/v1/'], calls)
        self.assertEqual('2026-10-22', result['checked'])
        self.assertEqual(['One', 'Two'], [item['title'] for item in result['items']])
        self.assertEqual(['1.5', '1.6', '1.10'], result['items'][0]['versions'])
        self.assertEqual('2025-10-14', result['items'][0]['updated'])


class PathTests(unittest.TestCase):
    def test_workshop_sits_beside_the_game(self):
        game = Path('C:/Steam/steamapps/common/RimWorld')
        self.assertEqual(Path('C:/Steam/steamapps/workshop/content/294100'), refresh.default_workshop(game))


if __name__ == '__main__':
    unittest.main()
