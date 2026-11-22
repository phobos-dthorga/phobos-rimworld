"""The edge check on pictures drawn here: a clean shape, the same with crumbs beside it, and with haze. No game art."""
import os
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))
from PIL import Image, ImageDraw  # noqa: E402

import edge_check  # noqa: E402


def picture(folder, name, crumbs=0, haze=False, outline=True):
    im = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((30, 30, 97, 97), fill=(120, 110, 90, 255), outline=(20, 20, 20, 255) if outline else None, width=3)
    for i in range(crumbs):
        x = 8 + (i % 4) * 30
        y = 4 if i < 4 else 118
        d.rectangle((x, y, x + 1, y + 1), fill=(200, 190, 160, 255))
    if haze:
        d.rectangle((105, 30, 120, 97), fill=(200, 190, 160, 40))
    path = Path(folder) / name
    im.save(path)
    return path


class EdgeCheckTests(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.TemporaryDirectory()
        self.addCleanup(self.dir.cleanup)

    def test_a_clean_shape_has_no_specks(self):
        m = edge_check.measure(picture(self.dir.name, 'clean.png'))
        self.assertEqual(m['specks'], 0)
        self.assertFalse(edge_check.flagged(m))

    def test_crumbs_beside_a_shape_are_specks_and_flag_it(self):
        m = edge_check.measure(picture(self.dir.name, 'crumbs.png', crumbs=8))
        self.assertEqual(m['specks'], 8)
        self.assertTrue(edge_check.flagged(m))

    def test_one_or_two_crumbs_do_not_flag(self):
        m = edge_check.measure(picture(self.dir.name, 'two.png', crumbs=2))
        self.assertEqual(m['specks'], 2)
        self.assertFalse(edge_check.flagged(m))

    def test_a_loose_thin_stroke_flags_but_a_thick_part_does_not(self):
        im = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.rectangle((30, 30, 97, 97), fill=(120, 110, 90, 255))
        d.line((10, 110, 40, 110), fill=(20, 20, 20, 255), width=2)  # a ghost outline, apart from the shape
        stroke = Path(self.dir.name) / 'stroke.png'
        im.save(stroke)
        m = edge_check.measure(stroke)
        self.assertEqual((m['strokes'], m['specks']), (1, 0))
        self.assertTrue(edge_check.flagged(m))
        part = Path(self.dir.name) / 'part.png'
        im2 = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
        ImageDraw.Draw(im2).rectangle((30, 30, 97, 97), fill=(120, 110, 90, 255))
        ImageDraw.Draw(im2).rectangle((5, 105, 25, 120), fill=(120, 110, 90, 255))  # a second part with a body of its own
        im2.save(part)
        self.assertEqual(edge_check.measure(part)['strokes'], 0)

    def test_haze_is_counted_and_shown_but_never_flags(self):
        m = edge_check.measure(picture(self.dir.name, 'haze.png', haze=True))
        self.assertGreater(m['haze'], 0)
        self.assertTrue(edge_check.hazy(m))
        self.assertFalse(edge_check.flagged(m))

    def test_a_picture_with_no_transparency_is_left_alone(self):
        path = Path(self.dir.name) / 'opaque.png'
        Image.new('RGB', (16, 16), (90, 90, 90)).save(path)
        self.assertTrue(edge_check.measure(path)['opaque'])

    def test_wildcards_match_the_accepted_list(self):
        entries = {'mods/*/Textures/*_Damage*.png': 'damage marks'}
        self.assertEqual(edge_check.listed('mods/X/Textures/Truck_DamageCab_north.png', entries), 'damage marks')
        self.assertIsNone(edge_check.listed('mods/X/Textures/Truck_north.png', entries))


if __name__ == '__main__':
    unittest.main()
