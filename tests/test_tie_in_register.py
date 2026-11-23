"""The tie-in register and the mods' LoadFolders.xml agree.

docs/development/tie-in-register.md lists every place one of our mods works with another author's mod (owner's question card, 24 Nov
2026: a gate check, in the style of tests/test_temporary_patches_list.py). Its rule, "a change that adds, removes or widens a tie-in
adds or edits its row in the same commit", was held by hand until this test. It reads each mod's conditional load folders (IfModActive,
IfModActiveAll, IfModNotActive) through phobos-modkit's RimWorld adapter (loading.conditional_folders), and checks that:

- each mod's section of the register names every one of them as `Mods/<Folder>` (a tie-in's 1.7 assembly folder or Common folder is
  part of its Mods/<Folder>, so naming that, or any path under it, is enough);
- every `Mods/...` path a section names is a conditional folder of that mod and exists on disk;
- the counts in "Read this first" (folders, entries and the folders of each mod) match what LoadFolders.xml lists.

C# tie-ins that look for a mod by name, and MayRequire entries, are not checked: nothing lists them but the code.

For a next game: the comparison (a register's sections against the folders a mod loads only with another mod) is game-free; what is
RimWorld's is the reading of LoadFolders.xml and About.xml's name. Another game would need its own reader of conditional content in
the kit's adapter for it, and this test's two readers moved into the kit with the game as a parameter.
"""
import re
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts'))
import _modkit  # noqa: E402  (the shared tools' import path)

_modkit.ensure()
from phobos_modkit.games.rimworld.loading import conditional_folders  # noqa: E402

REGISTER = ROOT / 'docs' / 'development' / 'tie-in-register.md'
MODS = ROOT / 'mods'
NUMBER_WORDS = {'one': 1, 'two': 2, 'three': 3, 'four': 4, 'five': 5, 'six': 6, 'seven': 7, 'eight': 8, 'nine': 9, 'ten': 10,
                'eleven': 11, 'twelve': 12}
COUNTS = re.compile(r'\*\*(\d+) tie-in folders across (\w+) mods\*\* \(recounted [^:]*: ([^;]+); (\d+) entries')


def top(folder):
    """The tie-in a load folder belongs to: Mods/<Folder> for anything under it, else the folder itself."""
    parts = folder.strip('/').split('/')
    return '/'.join(parts[:2]) if parts[0] == 'Mods' and len(parts) > 1 else '/'.join(parts)


def mod_names():
    """{the name in About.xml: the mod's folder}."""
    names = {}
    for about in sorted(MODS.glob('*/About/About.xml')):
        match = re.search(r'<name>([^<]*)</name>', about.read_text(encoding='utf-8'))
        if match:
            names[match.group(1).replace('&apos;', "'").strip()] = about.parents[1]
    return names


def register_sections(text):
    """{section title: [`Mods/...` paths it names, trailing slash removed]} for every '## ' section."""
    sections = {}
    for chunk in re.split(r'^## ', text, flags=re.M)[1:]:
        title, _, body = chunk.partition('\n')
        sections[title.strip()] = [p.rstrip('/') for p in re.findall(r'`(Mods/[^`]+)`', body)]
    return sections


def loaded_conditionally(mod_folder):
    """[folder as LoadFolders.xml writes it] for each conditional entry of the mod's load folders."""
    _, found = conditional_folders(mod_folder)
    return [folder for folder, _ in found]


class TieInRegisterTests(unittest.TestCase):
    def setUp(self):
        self.text = REGISTER.read_text(encoding='utf-8')
        self.sections = register_sections(self.text)
        self.mods = mod_names()
        self.entries = {folder.name: loaded_conditionally(folder) for folder in self.mods.values()}

    def test_every_conditional_folder_is_in_its_mods_section(self):
        for title, folder in self.mods.items():
            tops = sorted({top(entry) for entry in self.entries[folder.name]})
            if not tops:
                continue
            with self.subTest(mod=title):
                self.assertIn(title, self.sections, 'the register has no "## ' + title + '" section for this mod\'s tie-in folders')
                named = {top(path) for path in self.sections[title]}
                self.assertEqual([], [t for t in tops if t not in named],
                                 'name these LoadFolders.xml folders in the register\'s row for the tie-in (its Where column)')

    def test_every_folder_the_register_names_still_loads(self):
        for title, folder in self.mods.items():
            if title not in self.sections:
                continue
            tops = {top(entry) for entry in self.entries[folder.name]}
            for path in self.sections[title]:
                with self.subTest(mod=title, path=path):
                    self.assertIn(top(path), tops, 'not a conditional folder in this mod\'s LoadFolders.xml: remove or correct it')
                    self.assertTrue((folder / path).is_dir(), 'no such folder in mods/' + folder.name)

    def test_the_counts_match(self):
        match = COUNTS.search(self.text)
        self.assertIsNotNone(match, '"Read this first" lost its count line')
        folders, mods_word, per_mod, entries = match.groups()
        tops = {name: {top(e) for e in found} for name, found in self.entries.items() if found}
        self.assertEqual(sum(len(t) for t in tops.values()), int(folders), 'tie-in folders in all')
        self.assertEqual(sum(len(found) for found in self.entries.values()), int(entries), 'LoadFolders.xml entries in all')
        self.assertEqual(len(tops), NUMBER_WORDS.get(mods_word, -1), 'mods with tie-in folders')
        for item in per_mod.split(','):
            short, count = item.strip().rsplit(' ', 1)
            folder = self.mods.get("D'thorga " + short)
            with self.subTest(mod=short):
                self.assertIsNotNone(folder, 'no mod named D\'thorga ' + short)
                self.assertEqual(len(tops.get(folder.name, ())), int(count))

    def test_the_readers(self):
        self.assertEqual('Mods/IRBM', top('Mods/IRBM/1.7'))
        self.assertEqual('Mods/IRBM', top('Mods/IRBM/'))
        sections = register_sections('# T\n\n## A\n| x | `Mods/One`, `Mods/Two/1.7` |\n\n## B\n`Mods/<Other>/` and `src/X`\n')
        self.assertEqual({'A': ['Mods/One', 'Mods/Two/1.7'], 'B': ['Mods/<Other>']}, sections)
        match = COUNTS.search('- **84 tie-in folders across seven mods** (recounted 24 Nov 2026: Vehicles 29, Water Tap 1; 98 entries')
        self.assertEqual(('84', 'seven', 'Vehicles 29, Water Tap 1', '98'), match.groups())


if __name__ == '__main__':
    unittest.main()
