"""The "seen in play" notes and the map that ties them to test rows agree (Framework 0.118.0; owner, 17 Nov 2026).

The game writes a note the first time a feature is seen working (``Seen.Note("key", ...)`` in our C#, with the setting on), and
``config/test-evidence.json`` says which rows of ``docs/development/in-game-test-pass.md`` each key stands for. A key in the map that
no code notes would never flag its row; a note no row names would be lost. This reads our C# as text, so a key built at run time is
listed under the map's ``prefixes`` instead.
"""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NOTE = re.compile(r'\bSeen\.Note\(\s*"([^"]+)"')
ROW = re.compile(r'^\| (\w+) \|', re.M)


def noted_keys():
    keys = set()
    for path in (ROOT / 'src').rglob('*.cs'):
        if 'obj' in path.parts or 'bin' in path.parts:
            continue
        keys.update(NOTE.findall(path.read_text(encoding='utf-8')))
    return keys


class TestEvidenceMapTests(unittest.TestCase):
    def setUp(self):
        self.map = json.loads((ROOT / 'config' / 'test-evidence.json').read_text(encoding='utf-8'))
        self.mapped = {key for keys in self.map['rows'].values() for key in keys}
        self.prefixes = tuple(self.map.get('prefixes', {}))
        self.noted = noted_keys()

    def test_every_mapped_key_is_noted_somewhere(self):
        missing = sorted(k for k in self.mapped if k not in self.noted and not any(k.startswith(p) for p in self.prefixes))
        self.assertEqual([], missing, 'keys in config/test-evidence.json that no Seen.Note writes')

    def test_every_noted_key_has_a_row(self):
        unmapped = sorted(k for k in self.noted if k not in self.mapped and k not in self.prefixes)
        self.assertEqual([], unmapped, 'Seen.Note keys that no row of config/test-evidence.json names')

    def test_every_prefix_is_used_by_a_row(self):
        for prefix in self.prefixes:
            self.assertTrue(any(k.startswith(prefix) for k in self.mapped), prefix)

    def test_every_row_exists_in_the_test_pass(self):
        rows = set(ROW.findall((ROOT / 'docs' / 'development' / 'in-game-test-pass.md').read_text(encoding='utf-8')))
        self.assertEqual([], sorted(r for r in self.map['rows'] if r not in rows), 'rows the test pass does not have')


if __name__ == '__main__':
    unittest.main()
