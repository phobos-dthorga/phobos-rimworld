"""ship_picks.py: a picked try's record opens with how it was made, read from the try's own record. No game art."""
import os
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))
import ship_picks  # noqa: E402


class ShipPicksTests(unittest.TestCase):
    def test_each_engine_says_where_it_ran(self):
        local = ship_picks.made_with({'generator': 'ComfyUI 0.41.0, local, FLUX.2 Klein 4B base', 'engine': 'klein-base'})
        self.assertIn("on the owner's own graphics card", local)
        paid = ship_picks.made_with({'generator': 'OpenAI gpt-image-2', 'engine': 'openai'})
        self.assertIn('through a paid image service', paid)
        fal = ship_picks.made_with({'generator': 'Nano Banana Pro, through fal.ai (paid per image)', 'engine': 'fal'})
        self.assertEqual(fal.count('through'), 1)
        self.assertTrue(all(text.endswith('**AI-generated**.') for text in (local, paid, fal)))

    def test_a_record_names_the_pick_the_request_and_both_hashes(self):
        with tempfile.TemporaryDirectory() as work:
            master, target = Path(work) / 'icon-2.png', Path(work) / 'Icon.png'
            master.write_bytes(b'master')
            target.write_bytes(b'target')
            text = ship_picks.record_text('icon', '2', {'art_plan': {'button': 'Do the thing'}, 'references': [{'image': 'x'}]},
                                          'assets/x/requests/r.json', {'engine': 'openai', 'generator': 'OpenAI'}, master, target, 128)
        self.assertTrue(text.startswith('# Icon\n\nMade with OpenAI'))
        self.assertIn('Do the thing. Picked by the owner on the Picks page (try 2)', text)
        self.assertIn('`assets/x/requests/r.json`', text)
        self.assertIn('our own approved art, for style only', text)
        self.assertEqual(text.count('SHA-256'), 2)


if __name__ == '__main__':
    unittest.main()
