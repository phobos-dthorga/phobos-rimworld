"""The "seen in play" notes and the map that ties them to test rows agree (Framework 0.118.0; owner, 17 Nov 2026).

The game writes a note the first time a feature is seen working (``Seen.Note("key", ...)`` in our C#, with the setting on), and
``config/test-evidence.json`` says which rows of ``docs/development/in-game-test-pass.md`` each key stands for. A key in the map that
no code notes would never flag its row; a note no row names would be lost. This reads our C# as text, so a key built at run time is
listed under the map's ``prefixes`` instead.

Since the seen-in-play checks (18 Nov 2026, docs/development/seen-in-play-checks.md) the map may split a row into parts, narrow its
evidence and set rules on the order of notes; it is read through phobos-modkit's test-pass, which refuses a badly written map. A key
with nothing narrowing it that stands for more than one row must be listed under ``shared`` with why (E4): a generic note standing for
two claims is how rows 121 and 124 were once flagged on nothing.
"""
import json
import re
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts'))
import _modkit  # noqa: E402  (the shared tools' import path)

_modkit.ensure()
from phobos_modkit import testpass  # noqa: E402

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
        self.mapped = testpass.mapped_keys(self.map)
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
        named = list(self.map['rows']) + [r['row'] for r in self.map.get('order', []) if r.get('row')]
        self.assertEqual([], sorted(r for r in named if r not in rows), 'rows the test pass does not have')

    def test_a_generic_key_stands_for_one_row_unless_shared_on_purpose(self):
        self.assertEqual([], testpass.map_problems(self.map))

    def test_every_shared_key_is_still_shared(self):
        rows_of = {}
        for row, parts in testpass.parse_mapping(self.map).items():
            for part in parts:
                for item in part.items:
                    if not item.narrowed:
                        rows_of.setdefault(item.key, set()).add(row)
        stale = sorted(k for k in self.map.get('shared', {}) if len(rows_of.get(k, ())) < 2)
        self.assertEqual([], stale, 'keys listed under "shared" that no longer stand for more than one row')

    def test_every_mod_in_version_names_writes_notes(self):
        mods = set(re.findall(r'const string Mod = "([^"]+)"', ''.join(
            p.read_text(encoding='utf-8') for p in (ROOT / 'src').rglob('Seen.cs'))))
        mods.add("D'thorga Framework")
        self.assertEqual([], sorted(m for m in self.map.get('versionNames', {}) if m not in mods | {"D'thorga Factory Additions"}))


if __name__ == '__main__':
    unittest.main()
