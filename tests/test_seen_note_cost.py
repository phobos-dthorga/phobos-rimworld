"""A seen-in-play note costs nothing with its setting off (Framework 0.138.1; owner, 18 Nov 2026: "go ahead with both").

``Seen.Note(...)`` returns at once when the setting is off, but C# works out its arguments first: a note whose key or detail is put
together at the call (``+``, ``.ToString(``, ``ToStringPercent``, ``string.Join``, ``$"..."``) made text, and garbage, for every player
on every call. Such a call must sit behind ``Seen.Wants(...)`` (the setting on and not yet seen this session, so a tester pays only for
the first sighting) or at least ``Seen.On`` / ``SeenInPlay.On``, on its own line or the line before. A call whose arguments are only
names and fields needs no guard: reading them makes nothing.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CALL = re.compile(r'\b(?:Medical\.)?Seen\.Note\(')
BUILDS = re.compile(r'\+|\.ToString\(|ToStringPercent\(|string\.Join\(|\$"')
GUARD = re.compile(r'\bSeen\.Wants\(|\bSeen\.On\b|\bSeenInPlay\.On\b')
STRING = re.compile(r'"(?:[^"\\]|\\.)*"')


def arguments(text, start):
    """The text of a call's arguments, from the character after its opening bracket to its closing one."""
    depth, i = 1, start
    while i < len(text) and depth:
        if text[i] == '"':
            i = text.index('"', i + 1) + 1 if '"' in text[i + 1:] else len(text)
            continue
        depth += {'(': 1, ')': -1}.get(text[i], 0)
        i += 1
    return text[start:i - 1]


def unguarded_calls():
    found = []
    for path in sorted((ROOT / 'src').rglob('*.cs')):
        if 'obj' in path.parts or 'bin' in path.parts or path.name == 'Seen.cs':
            continue
        text = path.read_text(encoding='utf-8')
        lines = text.splitlines()
        for match in CALL.finditer(text):
            line = text.count('\n', 0, match.start())
            if lines[line].lstrip().startswith(('//', '///', '*')):
                continue
            if not BUILDS.search(STRING.sub('""', arguments(text, match.end()))):
                continue
            back = line - 1
            while back > 0 and lines[back].strip() in ('{', ''):
                back -= 1
            before = lines[back] if back >= 0 else ''
            if GUARD.search(lines[line]) or GUARD.search(before):
                continue
            found.append(f'{path.relative_to(ROOT).as_posix()}:{line + 1}')
    return found


class SeenNoteCostTests(unittest.TestCase):
    def test_a_note_that_builds_text_is_behind_its_setting(self):
        self.assertEqual([], unguarded_calls(),
                         'Seen.Note calls that build their key or detail before the setting is checked: put them behind '
                         'Seen.Wants("key", thing) or Seen.On')

    def test_the_check_reads_what_it_should(self):
        text = 'x Seen.Note("k", v, a + "b"); y Seen.Note("k", v, turret.key); z Seen.Note("k", v, "a (b) + c");'
        builds = [bool(BUILDS.search(STRING.sub('""', arguments(text, m.end())))) for m in CALL.finditer(text)]
        self.assertEqual([True, False, False], builds)


if __name__ == '__main__':
    unittest.main()
