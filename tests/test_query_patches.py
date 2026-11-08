"""scripts/query-patches.py: it reaches the kit's command, and a missing ModsConfig.xml is a plain error."""
import importlib.util
import io
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


def load():
    spec = importlib.util.spec_from_file_location('query_patches', ROOT / 'scripts' / 'query-patches.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class Script(unittest.TestCase):
    def test_help_and_a_plain_error_without_a_config_folder(self):
        module = load()
        self.assertEqual(module.main(['--help'], out=io.StringIO()), 0)
        with tempfile.TemporaryDirectory() as tmp:
            out = io.StringIO()
            code = module.main(['who', 'Pawn.Kill', '--config', tmp, '--game', tmp, '--workshop', tmp, '--cache', tmp], out=out)
        self.assertEqual(code, 1)
        self.assertIn('No ModsConfig.xml', out.getvalue())


if __name__ == '__main__':
    unittest.main()
