"""Offering the owner's picks to D'thorga Companion's Picks page: the review cards' pictures and the listening page's sounds, written
into a made-up repository's config/picks.json through the shared tools. No art and no sound of the game's."""
import json
import os
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))

import art_picks  # noqa: E402
import sound_review  # noqa: E402


class ArtPicksTests(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.addCleanup(self.dir.cleanup)
        self.root = Path(self.dir.name) / 'repo'
        self.root.mkdir()

    def read(self):
        return json.loads((self.root / 'config' / 'picks.json').read_text(encoding='utf-8'))

    def test_a_name_becomes_a_pick_id(self):
        self.assertEqual(art_picks.pick_id('Billy Kettle (redraw)'), 'billy-kettle-redraw')
        self.assertEqual(art_picks.pick_id('!!'), 'pick')

    def test_a_picture_pick_carries_the_cards_grounds_and_each_try(self):
        try_file = Path(self.dir.name) / 'kettle-3.png'
        try_file.write_bytes(b'png')
        art_picks.offer('billy-kettle', 'The billy kettle', 'picture', [('3', 'Try 3', try_file, '0 loose specks')],
                        source='assets/thing/requests/redraw.json', root=self.root)
        data = self.read()
        self.assertEqual([g['name'] for g in data['grounds']], ['Soil', 'Dark stone'])
        pick = data['picks'][0]
        self.assertEqual((pick['id'], pick['kind'], pick['tries'][0]['note']), ('billy-kettle', 'picture', '0 loose specks'))

    def test_sounds_not_yet_picked_are_offered_with_their_review_copies(self):
        generated = Path(self.dir.name) / 'phobos-sounds' / 'generated'
        for folder, seeds in (('tent-heater', (11, 12)), ('kettle-boil', (3,))):
            for seed in seeds:
                master = generated / folder / f'{folder}-{seed}.flac'
                master.parent.mkdir(parents=True, exist_ok=True)
                master.write_bytes(b'flac')
                review = sound_review.outputs_for(master)['review']
                review.parent.mkdir(parents=True, exist_ok=True)
                review.write_bytes(b'ogg')
        (generated.parent / 'picks.json').write_text(json.dumps({'picks': [{'candidate': 'kettle-boil/kettle-boil-3.flac'}]}),
                                                     encoding='utf-8')
        sound_review.offer_picks(generated, sorted(p for p in generated.iterdir() if p.is_dir()), root=self.root)
        picks = self.read()['picks']
        self.assertEqual([p['id'] for p in picks], ['tent-heater'])
        self.assertEqual([(t['id'], t['note']) for t in picks[0]['tries']], [('11', 'no flags'), ('12', 'no flags')])
        self.assertTrue(picks[0]['tries'][0]['path'].endswith('tent-heater-11.review.ogg'))


if __name__ == '__main__':
    unittest.main()
