"""Every tie-in assembly's mods are in its own mod's load-after list (Framework 0.120.1; the owner's log of 17 Nov 2026).

A tie-in assembly lives in a folder LoadFolders.xml loads only with another mod, and may name that mod's types. The game checks an
assembly's types as it loads, so the other mod's assembly must be loaded first. D'thorga Framework's air-defence tie-in was built
against The Dead Man's Switch - Expedition with nothing telling a mod sorter to put the Framework after it; in the owner's list the
Framework came first, and the game rejected the tie-in with "Could not load type" for each type it named. This reads each project's
ModFolder and TieInFolder, finds the mods LoadFolders.xml asks for on that folder, and asks that About.xml lists each under loadAfter
(or as a dependency). It cannot see a load order; it keeps the hint that sets one.
"""
import re
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def tie_ins():
    """(project name, mod folder, the package IDs its tie-in folder needs)."""
    found = []
    for project in sorted((ROOT / 'src').glob('*/*.csproj')):
        text = project.read_text(encoding='utf-8')
        mod = re.search(r'<ModFolder>([^<]+)</ModFolder>', text)
        tie = re.search(r'<TieInFolder>([^<]+)</TieInFolder>', text)
        if not mod or not tie:
            continue
        folder = tie.group(1).strip('/')
        needs = set()
        for entry in ET.parse(ROOT / 'mods' / mod.group(1) / 'LoadFolders.xml').getroot().iter('li'):
            if not (entry.text or '').strip().strip('/').startswith(folder):
                continue
            for attribute in ('IfModActive', 'IfModActiveAll'):
                needs.update(part.strip() for part in (entry.get(attribute) or '').split(',') if part.strip())
        found.append((project.parent.name, mod.group(1), needs))
    return found


def loads_after(mod):
    about = ET.parse(ROOT / 'mods' / mod / 'About' / 'About.xml').getroot()
    listed = {entry.text.strip().lower() for entry in about.findall('loadAfter/li') if entry.text}
    listed |= {entry.text.strip().lower() for entry in about.findall('modDependencies/li/packageId') if entry.text}
    return listed


class TieInLoadOrderTests(unittest.TestCase):
    def test_there_are_tie_ins_to_check(self):
        self.assertTrue(any(needs for _, _, needs in tie_ins()), 'no tie-in project with an IfModActive folder was found')

    def test_every_tie_in_mod_is_loaded_after(self):
        missing = []
        for project, mod, needs in tie_ins():
            listed = loads_after(mod)
            for package in sorted(needs):
                if package.lower() not in listed:
                    missing.append(f'{project}: mods/{mod}/About/About.xml has no loadAfter entry for {package}')
        self.assertEqual([], missing, 'a tie-in assembly loaded before the mod whose types it names is rejected by the game')


if __name__ == '__main__':
    unittest.main()
