"""Offline checks for scripts/cleanup.py and config/cleanup.json. Nothing here touches the real backups, the temporary
folder, the crash dumps or any process: the rules run against made-up folders."""
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts'))
import _modkit  # noqa: E402

_modkit.ensure()
from phobos_modkit import cleanup as kit  # noqa: E402

spec = importlib.util.spec_from_file_location('cleanup', ROOT / 'scripts' / 'cleanup.py')
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)

RULES = {rule['name']: rule for rule in kit.load_rules(ROOT)}


def make(path, age_days=0.0, folder=False):
    path = Path(path)
    if folder:
        path.mkdir(parents=True, exist_ok=True)
        (path / 'inside.bin').write_bytes(b'x')
    else:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b'x')
    stamp = time.time() - age_days * kit.DAY
    os.utime(path, (stamp, stamp))


class Rules(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def chosen(self, name, folder):
        """What one of our rules picks when its folder is this made-up one."""
        rule = {**RULES[name], 'folder': str(folder)}
        items, _ = kit.rule_items(rule, self.root, {}, time.time())
        return sorted(i.path.name for i in items)

    def test_keeps_the_newest_backup_of_each_current_mod_and_none_of_a_merged_mod(self):
        backups = self.root / 'backups'
        for name in ('PhobosFramework-20261107-130954', 'PhobosFramework-20261108-145928', 'PhobosFramework-20261101-090000',
                     'PhobosTriage-20261107-131000', 'notes', 'My-Mod-20261108-100000'):
            make(backups / name, folder=True)
        for mod in ('PhobosFramework', 'My-Mod'):
            make(self.root / 'mods' / mod / 'About' / 'About.xml')
        self.assertEqual(['PhobosFramework-20261101-090000', 'PhobosFramework-20261107-130954', 'PhobosTriage-20261107-131000'],
                         self.chosen('Install backups', backups))

    def test_finds_only_the_tests_repositories(self):
        for name in ('archive-test-abc123', 'phobos-artwork-archive-test-x', 'someone-elses', 'tmp.ABCDEFGHIJ'):
            make(self.root / name, folder=True)
        make(self.root / 'archive-test-file.txt')
        self.assertEqual(['archive-test-abc123', 'phobos-artwork-archive-test-x'], self.chosen('Test repositories', self.root))

    def test_build_logs_wait_a_day(self):
        make(self.root / 'Microsoft.NET.Workload_1_20261101_000000_1.log', age_days=3)
        make(self.root / 'Microsoft.NET.Workload_2_20261111_000000_1.log', age_days=0.1)
        make(self.root / 'SomethingElse.log', age_days=30)
        self.assertEqual(['Microsoft.NET.Workload_1_20261101_000000_1.log'], self.chosen('.NET build logs', self.root))

    def test_keeps_the_newest_crash_dump_and_other_programs_dumps(self):
        make(self.root / 'RimWorld.exe.1.dmp', age_days=5)
        make(self.root / 'RimWorld.exe.2.dmp', age_days=2)
        make(self.root / 'Other.exe.3.dmp', age_days=9)
        self.assertEqual(['RimWorld.exe.1.dmp'], self.chosen('RimWorld crash dumps', self.root))


class Leftovers(unittest.TestCase):
    def test_reports_failed_deploy_folders(self):
        with tempfile.TemporaryDirectory() as game:
            for name in ('Mods', 'PhobosVehicles-deploying-20261108-100723', 'PhobosVehicles-deploy-check-20261109-010203', 'Data'):
                (Path(game) / name).mkdir()
            self.assertEqual(['PhobosVehicles-deploy-check-20261109-010203', 'PhobosVehicles-deploying-20261108-100723'],
                             [p.name for p in cleanup.deploy_leftovers(game)])
            self.assertEqual([], cleanup.deploy_leftovers(None))

    def test_the_rules_file_is_valid(self):
        data = json.loads((ROOT / 'config' / 'cleanup.json').read_text(encoding='utf-8'))
        self.assertEqual(set(RULES), {rule['name'] for rule in data['rules']})


if __name__ == '__main__':
    unittest.main()
