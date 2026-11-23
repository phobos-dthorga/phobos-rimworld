"""Records of shipped files are kept under assets/records/ (the owner's choice of 23 Nov 2026), and the art tools write and find
them there. Every file is written in a temporary folder."""
import json
from pathlib import Path
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402


class RecordPaths(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.root = Path(self.folder.name)
        (self.root / 'config').mkdir()
        (self.root / 'config' / 'art-audit.json').write_text(
            json.dumps({'records': {'mirrors': {'mods': 'assets/records'}}}), encoding='utf-8')

    def tearDown(self):
        self.folder.cleanup()

    def test_a_shipped_file_s_record_goes_under_assets_records(self):
        beside = self.root / 'mods' / 'ModA' / 'Textures' / 'Lamp.record.md'
        target = art_paths.record_path(beside, root=self.root)
        self.assertEqual(target, self.root / 'assets' / 'records' / 'ModA' / 'Textures' / 'Lamp.record.md')
        self.assertTrue(target.parent.is_dir())

    def test_a_file_outside_the_mods_keeps_its_record_beside_it(self):
        beside = self.root / 'assets' / 'x' / 'Draft.record.md'
        self.assertEqual(art_paths.record_path(beside, root=self.root), beside.resolve())

    def test_records_are_found_beside_and_under_the_mirror(self):
        textures = self.root / 'mods' / 'ModA' / 'Textures'
        textures.mkdir(parents=True)
        (textures / 'Old.record.md').write_text('old', encoding='utf-8')
        art_paths.record_path(textures / 'New.record.md', root=self.root).write_text('new', encoding='utf-8')
        names = [p.name for p in art_paths.records_under(textures, root=self.root)]
        self.assertEqual(names, ['Old.record.md', 'New.record.md'])
        self.assertEqual([p.name for p in art_paths.shipped_records(root=self.root)], ['New.record.md', 'Old.record.md'])

    def test_no_record_is_left_in_the_mods(self):
        left = sorted(p.relative_to(ROOT).as_posix() for pattern in ('*.record.md', '*export.json')
                      for p in (ROOT / 'mods').rglob(pattern))
        self.assertEqual(left, [], 'records belong under assets/records/ (art_paths.record_path)')


if __name__ == '__main__':
    unittest.main()
