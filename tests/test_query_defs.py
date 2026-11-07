"""scripts/query-defs.py reaches the pinned phobos-modkit and indexes a tiny synthetic install. The Def index itself
is tested in phobos-modkit; nothing here reads the real installation."""
import importlib.util
import io
import os
from pathlib import Path
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8')


class Script(unittest.TestCase):
    def test_who_finds_a_def_in_a_synthetic_install(self):
        query = load('scripts/query-defs.py', 'query_defs')
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            game = root / 'steamapps' / 'common' / 'RimWorld'
            core = game / 'Data' / 'Core'
            write(core / 'About' / 'About.xml', '<ModMetaData><name>Core</name><packageId>Ludeon.RimWorld</packageId>'
                                                '</ModMetaData>')
            write(core / 'Defs' / 'Things.xml', '<Defs><ThingDef><defName>Steel</defName></ThingDef></Defs>')
            write(root / 'Config' / 'ModsConfig.xml', '<ModsConfigData><version>1.7.0 rev1</version><activeMods>'
                                                      '<li>ludeon.rimworld</li></activeMods></ModsConfigData>')
            out = io.StringIO()
            code = query.main(['who', 'Steel', '--game', str(game),
                               '--workshop', str(root / 'steamapps' / 'workshop' / 'content' / '294100'),
                               '--config', str(root / 'Config'), '--cache', str(root / 'cache')], out=out)
        self.assertEqual(code, 0)
        self.assertIn('| ThingDef | Steel | Core | 0 | kept |', out.getvalue())


if __name__ == '__main__':
    unittest.main()
