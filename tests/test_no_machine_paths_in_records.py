"""No art record the mods ship holds a path from the owner's PC (20 Nov 2026: 43 export records held the full path of the repository,
written by the exporters before export_sprite.repo_path). Reads the JSON records under mods/ and assets/ as git tracks them."""
import os
import re
import subprocess
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
# A drive letter followed by a slash inside a JSON string: "D:/..." or "C:\\...".
DRIVE_PATH = re.compile(r'"[A-Za-z]:(/|\\\\)')


class NoMachinePathsTests(unittest.TestCase):
    def test_no_record_holds_a_drive_path(self):
        files = subprocess.run(['git', 'ls-files', '--', 'mods/*.json', 'assets/*.json'], cwd=ROOT, capture_output=True, text=True,
                               check=True).stdout.splitlines()
        found = []
        for name in files:
            path = os.path.join(ROOT, name)
            if not os.path.isfile(path):
                continue
            with open(path, encoding='utf-8', errors='replace') as handle:
                if DRIVE_PATH.search(handle.read()):
                    found.append(name)
        self.assertEqual(found, [], 'records holding a machine path (write them with export_sprite.repo_path): ' + ', '.join(found[:10]))


if __name__ == '__main__':
    unittest.main()
