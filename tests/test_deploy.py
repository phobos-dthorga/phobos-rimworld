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

    def test_a_sync_conflict_copy_never_ships(self):
        with tempfile.TemporaryDirectory() as root:
            fake_repo(root)
            folder = Path(root) / 'mods' / 'Example' / '1.6' / 'Assemblies'
            (folder / 'Example_DiskStation_Nov-09-2352-2026_DownloadConflict_1.dll').write_bytes(b'old')
            said = []
            files = deploy.package('Example', root, log=said.append)
            self.assertFalse(any('Conflict' in name for name in files))
            self.assertEqual(1, len(said))

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
        for expected in ('PhobosFramework', 'PhobosMedical', 'PhobosRadioFramework', 'PhobosFactoryAdditions',
                         'PhobosPatches', 'PhobosWaterTap'):
            self.assertIn(expected, mods)
        # Every project builds into a real mod; tie-in projects share their mod's folder. A mod with no project is XML only
        # (D'thorga Vehicles 0.1.0), so it must load no version folder, where an assembly would be.
        folders = {re.search(r'<ModFolder>\s*([^<\s]+)', project.read_text(encoding='utf-8')).group(1) for project in deploy.projects()}
        self.assertLessEqual(folders, set(mods))
        for mod in set(mods) - folders:
            load_folders = Path(ROOT, 'mods', mod, 'LoadFolders.xml')
            text = load_folders.read_text(encoding='utf-8') if load_folders.exists() else ''
            self.assertNotRegex(text, r'<li[^>]*>\s*(Mods/[^<]*/)?1\.\d\s*</li>', mod + ' has no code but loads a version folder')

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


class HeldFolderTests(unittest.TestCase):
    def test_a_free_install_passes_the_check_and_stays_where_it_was(self):
        with tempfile.TemporaryDirectory() as game:
            target = Path(game) / 'Mods' / 'Example'
            (target / 'About').mkdir(parents=True)
            deploy.check_movable(['Example', 'NotInstalled'], game, windows=lambda: [], log=lambda s: None)
            self.assertTrue((target / 'About').is_dir())
            self.assertEqual(['Mods'], [p.name for p in Path(game).iterdir()])

    @unittest.skipUnless(sys.platform == 'win32', 'Windows refuses to rename a folder holding an open file')
    def test_a_held_install_stops_every_mod_and_names_the_explorer_window(self):
        with tempfile.TemporaryDirectory() as game:
            free = Path(game) / 'Mods' / 'Free'
            free.mkdir(parents=True)
            held = Path(game) / 'Mods' / 'Held'
            (held / 'Defs').mkdir(parents=True)
            open_file = held / 'Defs' / 'Things.xml'
            open_file.write_text('<Defs />', encoding='utf-8')
            with open(open_file, 'rb'), self.assertRaises(RuntimeError) as caught:
                deploy.check_movable(['Free', 'Held'], game, windows=lambda: [held / 'Defs'], log=lambda s: None)
            message = str(caught.exception)
            self.assertIn('File Explorer window is open inside it', message)
            self.assertIn(str(held / 'Defs'), message)
            self.assertIn('Nothing was changed', message)
            self.assertTrue(free.is_dir())

    def test_the_message_without_a_window_names_the_usual_holders(self):
        message = deploy.held_message(Path('C:/Game/Mods/Example'), 'Access is denied', [Path('C:/Elsewhere')])
        self.assertIn('terminal or editor', message)
        self.assertIn('RimSort', message)
        self.assertNotIn('Close RimWorld', message)

    def test_inside_matches_the_folder_and_below_only(self):
        self.assertTrue(deploy.inside(Path('C:/Game/Mods/Example/Defs'), Path('C:/game/mods/example')))
        self.assertTrue(deploy.inside(Path('C:/Game/Mods/Example'), Path('C:/Game/Mods/Example')))
        self.assertFalse(deploy.inside(Path('C:/Game/Mods/ExampleTwo'), Path('C:/Game/Mods/Example')))

    def test_apply_checks_every_mod_before_deploying_any(self):
        calls = []
        originals = deploy.deploy, deploy.check_movable, deploy.game_running
        deploy.deploy = lambda mod, game, apply, **kw: calls.append(('deploy', mod)) or 0
        deploy.check_movable = lambda mods, game, **kw: calls.append(('check', tuple(mods)))
        deploy.game_running = lambda *a, **kw: False
        try:
            deploy.main(['--apply', '--game', 'nowhere', 'A', 'B'])
        finally:
            deploy.deploy, deploy.check_movable, deploy.game_running = originals
        self.assertEqual([('check', ('A', 'B')), ('deploy', 'A'), ('deploy', 'B')], calls)


def installed_mod_with_textures(game, mod='Example', pngs=('Textures/A.png', 'Mods/Other/Common/Textures/B.png')):
    folder = Path(game) / 'Mods' / mod
    for rel in pngs:
        path = folder / rel
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b'\x89PNG')
    (folder / 'About').mkdir(parents=True, exist_ok=True)
    (folder / 'About' / 'Preview.png').write_bytes(b'\x89PNG')
    return folder


class ToddsTests(unittest.TestCase):
    """deploy encodes the installed mods' textures with RimSort's todds and RimSort's own preset (owner, 13 Nov 2026)."""

    def test_the_preset_is_rimsorts_optimized_one(self):
        # RimSort/RimSort app/utils/todds/wrapper.py, "optimized", with RimSort's overwrite off (-on): the flip matters.
        self.assertEqual(('-f', 'BC1', '-af', 'BC7', '-on', '-vf', '-fs', '-ss', 'Textures', '-t', '-p'), deploy.RIMSORT_OPTIMIZED)

    def test_todds_is_called_with_the_preset_and_the_installed_folder(self):
        calls = []

        class Result:
            returncode = 0
            stdout = stderr = ''

        with tempfile.TemporaryDirectory() as game:
            folder = installed_mod_with_textures(game)
            count = deploy.encode_textures(folder, Path('C:/fake/todds.exe'), run=lambda cmd, **kw: calls.append(cmd) or Result(),
                                           log=lambda s: None, clock=iter([0.0, 1.5]).__next__)
        self.assertEqual(2, count, 'only PNG files under a Textures path count, not About/Preview.png')
        self.assertEqual([[str(Path('C:/fake/todds.exe')), *deploy.RIMSORT_OPTIMIZED, str(folder)]], calls)

    def test_a_mod_without_textures_never_calls_todds(self):
        with tempfile.TemporaryDirectory() as game:
            folder = Path(game) / 'Mods' / 'XmlOnly'
            folder.mkdir(parents=True)
            self.assertEqual(0, deploy.encode_textures(folder, Path('todds.exe'), run=lambda *a, **kw: self.fail('called todds'),
                                                       log=lambda s: None))

    def test_a_todds_failure_is_reported_not_raised(self):
        class Result:
            returncode = 3
            stdout = ''
            stderr = 'bad file'

        lines = []
        with tempfile.TemporaryDirectory() as game:
            folder = installed_mod_with_textures(game)
            count = deploy.encode_textures(folder, Path('todds.exe'), run=lambda *a, **kw: Result(), log=lines.append)
        self.assertEqual(0, count)
        self.assertIn('PNG files', lines[0])

    def test_missing_todds_is_a_note_not_a_failure(self):
        lines = []
        self.assertEqual(0, deploy.encode_installed(['Example'], 'nowhere', find=lambda explicit: None,
                                                    encode=lambda *a, **kw: self.fail('encoded'), log=lines.append))
        self.assertIn('todds was not found', lines[0])

    def test_only_the_deployed_mods_are_encoded(self):
        seen = []
        with tempfile.TemporaryDirectory() as game:
            installed_mod_with_textures(game, 'Ours')
            installed_mod_with_textures(game, 'SomeoneElses')
            deploy.encode_installed(['Ours'], game, find=lambda explicit: Path('todds.exe'),
                                    encode=lambda folder, todds, **kw: seen.append(Path(folder).name) or 1, log=lambda s: None)
        self.assertEqual(['Ours'], seen)

    def test_todds_path_precedence(self):
        with tempfile.TemporaryDirectory() as folder:
            explicit, env, configured = (Path(folder) / name for name in ('a.exe', 'b.exe', 'c.exe'))
            for path in (explicit, env, configured):
                path.write_bytes(b'')
            settings = Path(folder) / 'install-settings.json'
            settings.write_text(json.dumps({'toddsPath': str(configured)}), encoding='utf-8')
            self.assertEqual(explicit, deploy.todds_path(str(explicit), {'RIMSORT_TODDS': str(env)}, settings))
            self.assertEqual(env, deploy.todds_path(None, {'RIMSORT_TODDS': str(env)}, settings))
            self.assertEqual(configured, deploy.todds_path(None, {}, settings))
            self.assertIsNone(deploy.todds_path(str(Path(folder) / 'absent.exe'), {}, settings))

    def test_no_dds_skips_the_step(self):
        calls = []
        originals = deploy.deploy, deploy.check_movable, deploy.game_running, deploy.encode_installed
        deploy.deploy = lambda mod, game, apply, **kw: 0
        deploy.check_movable = lambda mods, game, **kw: None
        deploy.game_running = lambda *a, **kw: False
        deploy.encode_installed = lambda mods, game, explicit=None, **kw: calls.append((tuple(mods), explicit)) or 0
        try:
            deploy.main(['--apply', '--no-dds', '--game', 'nowhere', 'A'])
            deploy.main(['--apply', '--todds', 'X:/todds.exe', '--game', 'nowhere', 'A'])
        finally:
            deploy.deploy, deploy.check_movable, deploy.game_running, deploy.encode_installed = originals
        self.assertEqual([(('A',), 'X:/todds.exe')], calls)


class GameRunningTests(unittest.TestCase):
    def test_tasklist_finds_rimworld_1_6_and_1_7(self):
        for image in ('RimWorld.exe', 'RimWorldWin64.exe', 'rimworld.EXE'):
            out = f'"svchost.exe","1","Services","0","1 K"\n"{image}","8404","Console","1","10,734,748 K"\n'
            self.assertTrue(deploy.listed_in_tasklist(out), image)

    def test_tasklist_ignores_other_programs(self):
        self.assertFalse(deploy.listed_in_tasklist('"RimSort.exe","27984","Console","1","1 K"\n'))


if __name__ == '__main__':
    unittest.main()
