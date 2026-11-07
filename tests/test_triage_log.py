"""scripts/triage-log.py reaches the pinned phobos-modkit and runs on a synthetic log. The tool itself is tested in
phobos-modkit; nothing here reads the real installation."""
import importlib.util
import io
import os
from pathlib import Path
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

LOG = """=== Waylith log ===
Version:   1.7.0.1

2026-01-02 10:00:01.000 ERROR [Verse] Exception ticking Thing_Example123456: System.NullReferenceException: x
   at ExampleMod.Comp.Tick() in ExampleMod.dll:token 0x6000001+0x1
    at .Verse.Log.Error_Patch3(String text) [0xFFFFFFFF]
2026-01-02 10:00:02.000 ERROR [Verse] Exception ticking Thing_Example654321: System.NullReferenceException: x
   at ExampleMod.Comp.Tick() in ExampleMod.dll:token 0x6000001+0x1
    at .Verse.Log.Error_Patch3(String text) [0xFFFFFFFF]
"""


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


modkit = load('scripts/_modkit.py', '_modkit_under_test')


class Shim(unittest.TestCase):
    def test_the_submodule_is_present(self):
        self.assertTrue((Path(ROOT) / 'external' / 'phobos-modkit' / 'python' / 'phobos_modkit').is_dir(),
                        'run: git submodule update --init')

    def test_a_missing_submodule_says_how_to_fetch_it(self):
        with tempfile.TemporaryDirectory() as empty:
            with self.assertRaisesRegex(modkit.MissingModKit, 'git submodule update --init'):
                modkit.ensure(empty)


class Script(unittest.TestCase):
    def test_summarises_a_log_and_groups_the_two_messages(self):
        triage = load('scripts/triage-log.py', 'triage_log')
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 'Player.log'
            path.write_text(LOG, encoding='utf-8')
            out = io.StringIO()
            code = triage.main([str(path), '--no-mods', '--no-previous'], out=out)
        self.assertEqual(code, 0)
        text = out.getvalue()
        self.assertIn('# Log triage: Player.log', text)
        self.assertIn('| 2 | ERROR |', text)
        self.assertIn('ExampleMod.dll (assembly)', text)


if __name__ == '__main__':
    unittest.main()
