"""Offline checks for scripts/cleanup.py. Nothing here touches the real backups, the temporary folder or any process."""
import importlib.util
import os
from pathlib import Path
import stat
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
spec = importlib.util.spec_from_file_location('cleanup', os.path.join(ROOT, 'scripts', 'cleanup.py'))
cleanup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cleanup)


class BackupTests(unittest.TestCase):
    NAMES = [
        'PhobosFramework-20261107-130954', 'PhobosFramework-20261108-145928', 'PhobosFramework-20261101-090000',
        'PhobosTriage-20261107-131000',  # merged away: no longer a mod
        'notes',  # not a backup
    ]

    def test_keeps_the_newest_of_each_current_mod_and_drops_merged_mods(self):
        doomed = cleanup.backups_to_remove(self.NAMES, {'PhobosFramework'})
        self.assertEqual(['PhobosFramework-20261101-090000', 'PhobosFramework-20261107-130954', 'PhobosTriage-20261107-131000'], doomed)

    def test_keep_two(self):
        doomed = cleanup.backups_to_remove(self.NAMES, {'PhobosFramework'}, keep=2)
        self.assertEqual(['PhobosFramework-20261101-090000', 'PhobosTriage-20261107-131000'], doomed)

    def test_a_mod_name_with_dashes_is_read_whole(self):
        self.assertEqual([], cleanup.backups_to_remove(['My-Mod-20261108-100000'], {'My-Mod'}))


class FolderTests(unittest.TestCase):
    def test_finds_only_the_tests_repositories(self):
        with tempfile.TemporaryDirectory() as temp:
            for name in ('archive-test-abc123', 'phobos-artwork-archive-test-x', 'someone-elses', 'tmp.ABCDEFGHIJ'):
                (Path(temp) / name).mkdir()
            (Path(temp) / 'archive-test-file.txt').write_text('x', encoding='utf-8')
            self.assertEqual(['archive-test-abc123', 'phobos-artwork-archive-test-x'], [p.name for p in cleanup.test_repositories(temp)])

    def test_reports_failed_deploy_folders(self):
        with tempfile.TemporaryDirectory() as game:
            for name in ('Mods', 'PhobosVehicles-deploying-20261108-100723', 'PhobosVehicles-deploy-check-20261109-010203', 'Data'):
                (Path(game) / name).mkdir()
            self.assertEqual(['PhobosVehicles-deploy-check-20261109-010203', 'PhobosVehicles-deploying-20261108-100723'],
                             [p.name for p in cleanup.deploy_leftovers(game)])
            self.assertEqual([], cleanup.deploy_leftovers(None))

    def test_remove_tree_deletes_read_only_files(self):
        with tempfile.TemporaryDirectory() as temp:
            objects = Path(temp) / 'repo' / '.git' / 'objects' / 'ab'
            objects.mkdir(parents=True)
            locked = objects / 'cdef'
            locked.write_bytes(b'\0')
            os.chmod(locked, stat.S_IREAD)
            cleanup.remove_tree(Path(temp) / 'repo')
            self.assertFalse((Path(temp) / 'repo').exists())


if __name__ == '__main__':
    unittest.main()
