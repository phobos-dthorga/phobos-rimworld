"""scripts/workshop-research.py reaches the pinned phobos-modkit; Steam is faked, so nothing leaves the computer.
The research itself is tested in phobos-modkit."""
import importlib.util
import io
import json
import os
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def fake_steam(url, data):
    if 'GetPublishedFileDetails' in url:
        return 200, json.dumps({'response': {'publishedfiledetails': [
            {'publishedfileid': '1111111111', 'result': 1, 'creator': '76561197960265829', 'title': 'Example Mod',
             'time_updated': 1790000000, 'subscriptions': 5, 'tags': [{'tag': '1.7'}]}]}}).encode()
    raise AssertionError('details only: no comments were asked for')


class Script(unittest.TestCase):
    def test_details_through_the_script(self):
        research = load('scripts/workshop-research.py', 'workshop_research')
        with tempfile.TemporaryDirectory() as tmp:
            out = io.StringIO()
            code = research.main(['details', '1111111111', '--cache', tmp, '--config', tmp, '--game', tmp,
                                  '--workshop', tmp], out=out, post=fake_steam, clock=lambda: 1790600000)
        self.assertEqual(code, 0)
        self.assertIn('| 1111111111 | Example Mod | 2026-09-21 | 1.7 |', out.getvalue())


if __name__ == '__main__':
    unittest.main()
