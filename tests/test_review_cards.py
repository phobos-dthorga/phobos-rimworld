"""Review cards drawn from pictures made here: a sprite card's layout and figures, and a vehicle tinted by its paint mask. No game art."""
import os
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))
from PIL import Image, ImageDraw  # noqa: E402

import review_cards  # noqa: E402


def sprite(path, crumbs=0):
    im = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.ellipse((12, 12, 51, 51), fill=(90, 110, 70, 255), outline=(20, 20, 20, 255), width=2)
    for i in range(crumbs):
        d.point((2 + i * 6, 2), fill=(200, 200, 200, 255))
    im.save(path)
    return path


class ReviewCardTests(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.addCleanup(self.dir.cleanup)
        self.folder = Path(self.dir.name)

    def test_a_sprite_card_shows_each_attempt_enlarged_and_at_size(self):
        exports = [('1', sprite(self.folder / 'a.png')), ('2', sprite(self.folder / 'b.png', crumbs=8))]
        out = self.folder / 'card.png'
        review_cards.sprite_card('test', exports, out)
        card = Image.open(out)
        # Two columns, each the x3 picture on two grounds side by side.
        self.assertGreaterEqual(card.width, 2 * (64 * 3 * 2 + 30))
        self.assertGreaterEqual(card.height, 64 * 3 + 64)

    def test_attempts_are_found_by_seed_value(self):
        requests, generated = self.folder / 'requests', self.folder / 'generated' / 'pane'
        requests.mkdir()
        generated.mkdir(parents=True)
        for seed in (1, 2, 3, 4):
            sprite(generated / f'pane-{seed}.png')
        found = review_cards.attempts(requests / 'retry.json', {'name': 'pane', 'seeds': [3, 4]})
        self.assertEqual([p.name for p in found], ['pane-3.png', 'pane-4.png'])

    def test_tinting_paints_the_masked_part_and_leaves_the_rest(self):
        vehicles = self.folder / 'Vehicles'
        (vehicles / 'Van').mkdir(parents=True)
        body = Image.new('RGBA', (4, 1), (200, 200, 200, 255))
        body.save(vehicles / 'Van' / 'Van_north.png')
        mask = Image.new('RGBA', (4, 1), (0, 0, 0, 255))
        mask.putpixel((0, 0), (255, 0, 0, 255))      # main colour
        mask.putpixel((1, 0), (0, 255, 0, 255))      # second colour
        mask.save(vehicles / 'Van' / 'Van_northm.png')
        with mock.patch.object(review_cards, 'VEHICLES', vehicles):
            out = review_cards.tinted('Van', 'Van', ((255, 0, 0), (0, 0, 255)))
        self.assertEqual(out.getpixel((0, 0))[:3], (200, 0, 0))
        self.assertEqual(out.getpixel((1, 0))[:3], (0, 0, 200))
        self.assertEqual(out.getpixel((2, 0))[:3], (200, 200, 200))

    def test_a_vehicle_with_no_paint_mask_is_drawn_as_it_is(self):
        vehicles = self.folder / 'Vehicles'
        (vehicles / 'Cart').mkdir(parents=True)
        Image.new('RGBA', (2, 2), (10, 20, 30, 255)).save(vehicles / 'Cart' / 'Cart_north.png')
        with mock.patch.object(review_cards, 'VEHICLES', vehicles):
            self.assertEqual(review_cards.tinted('Cart', 'Cart', None).getpixel((0, 0)), (10, 20, 30, 255))


if __name__ == '__main__':
    unittest.main()
