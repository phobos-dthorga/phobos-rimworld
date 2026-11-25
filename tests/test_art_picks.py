"""Offering the owner's picks to D'thorga Companion's Picks page: the review cards' pictures and the listening page's sounds, written
into a made-up repository's config/picks.json through the shared tools. No art and no sound of the game's."""
import contextlib
import io
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

    def test_cards_that_ship_as_one_file_carry_its_slot(self):
        try_file = Path(self.dir.name) / 'purge-1.png'
        try_file.write_bytes(b'png')
        for name in ('purge-a', 'purge-b'):
            art_picks.offer(name, name, 'picture', [('1', 'Try 1', try_file, '')], root=self.root,
                            slot='mods/Sample/Textures/Purge.png')
        art_picks.offer('kettle', 'kettle', 'picture', [('1', 'Try 1', try_file, '')], root=self.root)
        slots = {p['id']: p.get('slot') for p in self.read()['picks']}
        self.assertEqual(slots, {'purge-a': 'mods/Sample/Textures/Purge.png', 'purge-b': 'mods/Sample/Textures/Purge.png',
                                 'kettle': None})

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

    def sounds(self, folders):
        """A made-up generated folder: each sound folder's masters and review copies."""
        generated = Path(self.dir.name) / 'phobos-sounds' / 'generated'
        for folder, seeds in folders:
            for seed in seeds:
                master = generated / folder / f'{folder}-{seed}.flac'
                master.parent.mkdir(parents=True, exist_ok=True)
                master.write_bytes(b'flac')
                review = sound_review.outputs_for(master)['review']
                review.parent.mkdir(parents=True, exist_ok=True)
                review.write_bytes(b'ogg')
        return generated, sorted(p for p in generated.iterdir() if p.is_dir())

    def offered(self):
        path = self.root / 'config' / 'picks.json'
        return [p['id'] for p in self.read()['picks']] if path.is_file() else []

    def test_named_sounds_not_yet_picked_are_offered_with_their_review_copies(self):
        generated, folders = self.sounds((('tent-heater', (11, 12)), ('kettle-boil', (3,))))
        (generated.parent / 'picks.json').write_text(json.dumps({'picks': [{'candidate': 'kettle-boil/kettle-boil-3.flac'}]}),
                                                     encoding='utf-8')
        self.assertEqual(sound_review.offer_picks(generated, folders, ['tent-heater', 'kettle-boil'], root=self.root), [])
        picks = self.read()['picks']
        self.assertEqual([p['id'] for p in picks], ['sound-tent-heater'])
        self.assertEqual([(t['id'], t['note']) for t in picks[0]['tries']], [('11', 'no flags'), ('12', 'no flags')])
        self.assertTrue(picks[0]['tries'][0]['path'].endswith('tent-heater-11.review.ogg'))

    def test_a_sound_not_named_is_never_offered_though_it_has_no_pick(self):
        """23 Nov 2026: a bare --offer swept up 25 settled sounds whose rounds were picked under another folder's name."""
        generated, folders = self.sounds((('n11-purge', (81,)), ('n11-window', (81,)), ('concentrator-hum', (12,))))
        self.assertEqual(sound_review.offer_picks(generated, folders, ['n11-*'], root=self.root), [])
        self.assertEqual(self.offered(), ['sound-n11-purge', 'sound-n11-window'])

    def test_a_request_file_names_its_sounds_and_a_name_matching_nothing_is_returned(self):
        generated, folders = self.sounds((('n11-purge', (81,)), ('old-hum', (1,))))
        request = Path(self.dir.name) / 'night-11.json'
        request.write_text(json.dumps([{'name': 'n11-purge', 'engine': 'sa3-sfx'}]), encoding='utf-8')
        self.assertEqual(sound_review.offer_picks(generated, folders, [str(request), 'n11-typo'], root=self.root), ['n11-typo'])
        self.assertEqual(self.offered(), ['sound-n11-purge'])

    def test_a_picture_and_a_sound_of_one_name_are_both_offered(self):
        """23 Nov 2026: night 12's picture and sound "n12-exhaust-brake" shared an id, and the sound replaced the picture."""
        try_file = Path(self.dir.name) / 'try.png'
        try_file.write_bytes(b'png')
        art_picks.offer('n12-exhaust-brake', 'Exhaust brake', 'picture', [('11', 'Try 11', try_file, '')], root=self.root)
        generated, folders = self.sounds((('n12-exhaust-brake', (121,)),))
        self.assertEqual(sound_review.offer_picks(generated, folders, ['n12-exhaust-brake'], root=self.root), [])
        self.assertEqual(self.offered(), ['n12-exhaust-brake', 'sound-n12-exhaust-brake'])

    def test_offer_needs_a_name(self):
        with self.assertRaises(SystemExit), contextlib.redirect_stderr(io.StringIO()):
            sound_review.main(['generated', '--offer'])


if __name__ == '__main__':
    unittest.main()
