"""Offline checks for scripts/deploy.py. Nothing here touches the RimWorld installation."""
import importlib.util
import json
import os
import sys
from pathlib import Path
import tempfile
import re
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


deploy = load('scripts/deploy.py', 'deploy')


def fake_repo(root):
    """A repository with one mod, including files that must not ship."""
    mod = Path(root) / 'mods' / 'Example'
    (mod / 'About').mkdir(parents=True)
    (mod / 'About' / 'About.xml').write_text('<ModMetaData />', encoding='utf-8')
    (mod / 'README.md').write_text('# Example\n', encoding='utf-8')
    (mod / '1.6' / 'Assemblies').mkdir(parents=True)
    (mod / '1.6' / 'Assemblies' / 'Example.dll').write_bytes(b'\0')
    (mod / '1.6' / 'Assemblies' / 'Example.pdb').write_bytes(b'\0')
    (mod / 'Common' / 'Defs').mkdir(parents=True)
    (mod / 'Common' / 'Defs' / 'Things.xml').write_text('<Defs />', encoding='utf-8')
    (mod / 'Common' / '__pycache__').mkdir()
    (mod / 'Common' / '__pycache__' / 'x.pyc').write_bytes(b'\0')
    return mod


class PackageTests(unittest.TestCase):
    def test_ships_everything_except_build_clutter(self):
        with tempfile.TemporaryDirectory() as root:
            fake_repo(root)
            files = deploy.package('Example', root)
        self.assertEqual(sorted(files), ['1.6/Assemblies/Example.dll', 'About/About.xml', 'Common/Defs/Things.xml', 'README.md'])

    def test_unknown_mod_is_an_error(self):
        with tempfile.TemporaryDirectory() as root:
            with self.assertRaises(FileNotFoundError):
                deploy.package('Missing', root)

    def test_the_real_mod_packages(self):
        files = deploy.package('PhobosRadioFramework')
        self.assertIn('About/About.xml', files)
        self.assertIn('LoadFolders.xml', files)


class AllModsTests(unittest.TestCase):
    def test_every_folder_with_an_about_file_counts(self):
        with tempfile.TemporaryDirectory() as root:
            fake_repo(root)
            (Path(root) / 'mods' / 'NotAMod').mkdir()
            (Path(root) / 'mods' / 'README.md').write_text('# Mods\n', encoding='utf-8')
            self.assertEqual(['Example'], deploy.all_mods(root))

    def test_the_real_repository_lists_every_mod(self):
        mods = deploy.all_mods()
        for expected in ('PhobosFramework', 'PhobosBurnsAndSmoke', 'PhobosRadioFramework', 'PhobosFactoryAdditions',
                         'PhobosMedicalSaveBridge', 'PhobosVehiclePatches', 'PhobosWaterTap'):
            self.assertIn(expected, mods)
        # Every project builds into a real mod, and every mod's code is built; tie-in projects share their mod's folder.
        folders = {re.search(r'<ModFolder>\s*([^<\s]+)', project.read_text(encoding='utf-8')).group(1) for project in deploy.projects()}
        self.assertEqual(sorted(folders), sorted(mods))

    def test_build_runs_dotnet_on_every_project_and_stops_at_the_first_failure(self):
        calls = []

        class Result:
            def __init__(self, code):
                self.returncode, self.stdout, self.stderr = code, 'out', 'err'

        def run(cmd, **kwargs):
            calls.append(cmd)
            return Result(1 if len(calls) == 2 else 0)

        with self.assertRaises(RuntimeError):
            deploy.build(run=run, log=lambda s: None)
        self.assertEqual(2, len(calls))
        self.assertEqual(['dotnet', 'build'], calls[0][:2])
        self.assertTrue(calls[0][2].endswith('.csproj'))

    def test_all_flag_previews_every_mod_without_a_name(self):
        seen = []
        original = deploy.deploy
        deploy.deploy = lambda mod, game, apply, **kw: seen.append((mod, apply)) or 0
        try:
            deploy.main(['--all', '--game', 'nowhere'])
        finally:
            deploy.deploy = original
        self.assertEqual(sorted(deploy.all_mods()), [mod for mod, _ in seen])
        self.assertTrue(all(not apply for _, apply in seen))


class GamePathTests(unittest.TestCase):
    def test_precedence(self):
        with tempfile.TemporaryDirectory() as folder:
            settings = Path(folder) / 'install-settings.json'
            settings.write_text(json.dumps({'rimworldPath': 'C:/FromSettings'}), encoding='utf-8')
            self.assertEqual(Path('C:/Explicit'), deploy.game_path('C:/Explicit', {'RIMWORLD_PATH': 'C:/FromEnv'}, settings))
            self.assertEqual(Path('C:/FromEnv'), deploy.game_path(None, {'RIMWORLD_PATH': 'C:/FromEnv'}, settings))
            self.assertEqual(Path('C:/FromSettings'), deploy.game_path(None, {}, settings))
            self.assertEqual(deploy.DEFAULT_GAME_PATH, deploy.game_path(None, {}, Path(folder) / 'absent.json'))


class DeployTests(unittest.TestCase):
    def test_preview_copies_nothing(self):
        with tempfile.TemporaryDirectory() as root, tempfile.TemporaryDirectory() as game:
            fake_repo(root)
            (Path(game) / 'Mods').mkdir()
            count = deploy.deploy('Example', game, apply=False, root=root, running=lambda: True, log=lambda s: None)
            self.assertEqual(4, count)
            self.assertFalse((Path(game) / 'Mods' / 'Example').exists())

    def test_refuses_while_the_game_runs(self):
        with tempfile.TemporaryDirectory() as root, tempfile.TemporaryDirectory() as game:
            fake_repo(root)
            (Path(game) / 'Mods').mkdir()
            with self.assertRaises(RuntimeError):
                deploy.deploy('Example', game, apply=True, root=root, running=lambda: True, log=lambda s: None)

    def test_apply_copies_and_backs_up_the_previous_install(self):
        with tempfile.TemporaryDirectory() as root, tempfile.TemporaryDirectory() as game, tempfile.TemporaryDirectory() as backups:
            fake_repo(root)
            target = Path(game) / 'Mods' / 'Example'
            (target / 'Common').mkdir(parents=True)
            (target / 'Common' / 'Old.xml').write_text('<Defs />', encoding='utf-8')
            (Path(game) / 'Mods' / 'ModsConfig.xml').write_text('untouched', encoding='utf-8')
            deploy.deploy('Example', game, apply=True, root=root, backups=backups, running=lambda: False, log=lambda s: None)
            self.assertTrue((target / 'About' / 'About.xml').is_file())
            self.assertTrue((target / '1.6' / 'Assemblies' / 'Example.dll').is_file())
            self.assertFalse((target / 'Common' / 'Old.xml').exists())
            self.assertEqual(1, len(list(Path(backups).glob('Example-*/Common/Old.xml'))))
            self.assertEqual('untouched', (Path(game) / 'Mods' / 'ModsConfig.xml').read_text(encoding='utf-8'))
            self.assertEqual([], list(Path(game).glob('Example-deploying-*')))

    @unittest.skipUnless(sys.platform == 'win32', 'Windows refuses to rename a folder holding an open file')
    def test_an_open_file_stops_it_before_anything_changes(self):
        with tempfile.TemporaryDirectory() as root, tempfile.TemporaryDirectory() as game, tempfile.TemporaryDirectory() as backups:
            fake_repo(root)
            target = Path(game) / 'Mods' / 'Example'
            (target / 'Common').mkdir(parents=True)
            old = target / 'Common' / 'Old.xml'
            old.write_text('<Defs />', encoding='utf-8')
            with open(old, 'rb'), self.assertRaises(RuntimeError):
                deploy.deploy('Example', game, apply=True, root=root, backups=backups, running=lambda: False, log=lambda s: None)
            self.assertEqual(['Common/Old.xml'], [p.relative_to(target).as_posix() for p in target.rglob('*') if p.is_file()])
            self.assertEqual([], list(Path(backups).glob('Example-*')))

    def test_apply_needs_a_mods_folder(self):
        with tempfile.TemporaryDirectory() as root, tempfile.TemporaryDirectory() as game:
            fake_repo(root)
            with self.assertRaises(FileNotFoundError):
                deploy.deploy('Example', game, apply=True, root=root, running=lambda: False, log=lambda s: None)


class GameRunningTests(unittest.TestCase):
    def test_tasklist_finds_rimworld_1_6_and_1_7(self):
        for image in ('RimWorld.exe', 'RimWorldWin64.exe', 'rimworld.EXE'):
            out = f'"svchost.exe","1","Services","0","1 K"\n"{image}","8404","Console","1","10,734,748 K"\n'
            self.assertTrue(deploy.listed_in_tasklist(out), image)

    def test_tasklist_ignores_other_programs(self):
        self.assertFalse(deploy.listed_in_tasklist('"RimSort.exe","27984","Console","1","1 K"\n'))


if __name__ == '__main__':
    unittest.main()
