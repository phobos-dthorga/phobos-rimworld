"""scripts/query-code.py: which source trees it names, and a query through it on a made-up tree."""
import importlib.util
import io
import os
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


def load():
    spec = importlib.util.spec_from_file_location('query_code', ROOT / 'scripts' / 'query-code.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class Script(unittest.TestCase):
    def test_corpora_are_the_decompiled_folder_when_present_and_our_source(self):
        module = load()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.assertEqual(module.corpus_arguments(root), ['--corpus', f'ours={root / "src"}'])
            (root / '.local' / 'decompiled').mkdir(parents=True)
            self.assertEqual(module.corpus_arguments(root)[:2], ['--each-in', str(root / '.local' / 'decompiled')])

    def test_a_query_finds_our_own_code(self):
        module = load()
        with tempfile.TemporaryDirectory() as tmp:
            out = io.StringIO()
            os.environ['PHOBOS_MODKIT_CACHE'] = tmp
            try:
                code = module.main(['find', 'CompPhobosVehicleSystems', '--in', 'ours', '--kind', 'class'], out=out)
            finally:
                del os.environ['PHOBOS_MODKIT_CACHE']
        self.assertEqual(code, 0)
        self.assertIn('Phobos.VehicleSystems.CompPhobosVehicleSystems', out.getvalue())

    def test_compare_finds_the_local_decompiler_and_the_game_unless_named(self):
        module = load()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            game = root / 'game'
            game.mkdir()
            tool = root / '.local/tools/ilspy/.store/ilspycmd/1.0/ilspycmd.win-x64/1.0/tools/any/win-x64/ilspycmd.exe'
            tool.parent.mkdir(parents=True)
            tool.write_bytes(b'')
            self.assertEqual(module.compare_arguments((), root, game),
                             ['--decompiler', str(tool), '--reference', str(game)])
            self.assertEqual(module.compare_arguments(['--decompiler', 'x', '--reference', 'y'], root, game), [])
            self.assertEqual(module.compare_arguments((), root / 'nothing', root / 'absent'), [])

    def test_compare_through_the_script_on_made_up_trees(self):
        module = load()
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for name, value in (('old', 1), ('new', 2)):
                (root / name).mkdir()
                (root / name / 'Thing.cs').write_text(
                    f'namespace N {{ public class Thing {{ public int Size() {{ return {value}; }} }} }}\n', encoding='utf-8')
            out = io.StringIO()
            os.environ['PHOBOS_MODKIT_CACHE'] = str(root / 'cache')
            try:
                code = module.main(['compare', str(root / 'old'), str(root / 'new')], out=out)
            finally:
                del os.environ['PHOBOS_MODKIT_CACHE']
        self.assertEqual(code, 0)
        self.assertIn('N.Thing.Size | values only', out.getvalue())


if __name__ == '__main__':
    unittest.main()
