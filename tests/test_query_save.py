"""scripts/query-save.py reaches the pinned phobos-modkit. The save index itself is tested in phobos-modkit (it needs
its Rust program built); nothing here reads the real installation."""
import importlib.util
import io
import os
import tempfile
import unittest
from contextlib import redirect_stderr

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class Script(unittest.TestCase):
    def test_an_empty_saves_folder_is_reported_without_touching_anything(self):
        query = load('scripts/query-save.py', 'query_save')
        with tempfile.TemporaryDirectory() as tmp:
            errors = io.StringIO()
            with redirect_stderr(errors):
                code = query.main(['summary', '--saves', tmp, '--cache', os.path.join(tmp, 'cache')],
                                  out=io.StringIO())
            self.assertFalse(os.path.exists(os.path.join(tmp, 'cache')), 'nothing is created before a save is found')
        self.assertEqual(code, 2)
        self.assertIn('No saves', errors.getvalue())


if __name__ == '__main__':
    unittest.main()
