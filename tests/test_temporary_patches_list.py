"""The temporary patches register and the list update-check.py watches agree.

docs/development/temporary-patches.md says which of our patches exist for another author's fault; config/temporary-patches.json
names the methods each fault lives in, so phobos-modkit's rimworld-update-check can say when an update touched one (owner, 20 Nov
2026: the check after an update, in D'thorga Companion). A row added to the register without its line in the list would never be
watched, so this test holds the two together: every temporary row is listed or under notWatched with its reason, every listed row
is in the register, and each listed patch is named in its row.
"""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REGISTER = ROOT / 'docs' / 'development' / 'temporary-patches.md'
LIST = ROOT / 'config' / 'temporary-patches.json'
SECTION = '## Temporary: comes out when the author fixes it'


def register_rows(text):
    """{row number: the row's text} from the register's temporary table."""
    start = text.index(SECTION)
    end = text.find('\n## ', start + len(SECTION))
    rows = {}
    for line in text[start:end if end > 0 else None].splitlines():
        match = re.match(r'^\|\s*(\d+)\s*\|', line)
        if match:
            rows[int(match.group(1))] = line
    return rows


def row_number(value):
    return int(re.match(r'\s*(\d+)', str(value)).group(1))


class TemporaryPatchesListTests(unittest.TestCase):
    def setUp(self):
        self.rows = register_rows(REGISTER.read_text(encoding='utf-8'))
        self.listed = json.loads(LIST.read_text(encoding='utf-8'))

    def test_every_temporary_row_is_watched_or_says_why_not(self):
        watched = {row_number(entry['row']) for entry in self.listed['patches']}
        excused = {int(row) for row in self.listed.get('notWatched', {})}
        self.assertEqual([], sorted(set(self.rows) - watched - excused),
                         'add these rows to config/temporary-patches.json, or to its notWatched with the reason')

    def test_every_listed_row_is_in_the_register_and_names_its_patch(self):
        for entry in self.listed['patches']:
            with self.subTest(row=entry['row']):
                number = row_number(entry['row'])
                self.assertIn(number, self.rows, 'a row the register no longer has: remove it from the list')
                self.assertIn(entry['patch'], self.rows[number])
                self.assertTrue(entry['members'] and entry['file'].endswith('.dll'))

    def test_the_parser_reads_the_table(self):
        rows = register_rows('x\n' + SECTION + '\n\n| # | Patch |\n| --- | --- |\n| 2 | A |\n| 12 | B |\n\n## Next\n| 30 | C |\n')
        self.assertEqual({2, 12}, set(rows))


if __name__ == '__main__':
    unittest.main()
