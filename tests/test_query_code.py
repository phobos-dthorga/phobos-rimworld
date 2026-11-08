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


if __name__ == '__main__':
    unittest.main()
