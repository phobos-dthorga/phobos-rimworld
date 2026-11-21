"""clean_backgrounds.py's review and apply, and export_sprite's named holes: offline, with synthetic pictures and a stage outside the
repository, as the art tools' working folder is on the owner's PC (K:, since 19 Nov 2026)."""
import json
import shutil
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import clean_backgrounds  # noqa: E402
import export_sprite  # noqa: E402

try:
    from PIL import Image, ImageDraw
except ImportError:  # pragma: no cover - the gate's Python has Pillow
    Image = None


def ring(path):
    """A dark ring on the generator's white, its hole left white: 40 x 40, the hole at (20, 20)."""
    image = Image.new('RGB', (40, 40), (254, 254, 254))
    draw = ImageDraw.Draw(image)
    draw.rectangle((8, 8, 31, 31), fill=(20, 20, 20))
    draw.rectangle((14, 14, 25, 25), fill=(254, 254, 254))
    image.save(path)


@unittest.skipIf(Image is None, 'needs Pillow')
class NamedHoleTests(unittest.TestCase):
    def setUp(self):
        self.work = Path(tempfile.mkdtemp())

    def tearDown(self):
        shutil.rmtree(self.work)

    def test_a_named_hole_clears_that_patch_and_is_kept_in_the_record(self):
        # 21 Nov 2026: the run-flat tyres, the service spanner and the long gearing kept their holes white.
        ring(self.work / 'master.png')
        with mock.patch.object(export_sprite, 'ai_background', lambda path: None):
            plain = export_sprite.export(self.work / 'master.png', self.work / 'plain.png', 32, '1:1', 235, 30)
            holed = export_sprite.export(self.work / 'master.png', self.work / 'holed.png', 32, '1:1', 235, 30, holes=[('20', '20')])
        self.assertTrue(plain['white_left'], 'the white hole is warned of')
        self.assertEqual(holed['white_left'], [])
        self.assertEqual(holed['holes'], [[20, 20]])
        sprite = Image.open(self.work / 'holed.png').convert('RGBA')
        self.assertEqual(sprite.getpixel((16, 16))[3], 0, 'the middle shows the ground')
        self.assertEqual(Image.open(self.work / 'plain.png').convert('RGBA').getpixel((16, 16))[3], 255)


@unittest.skipIf(Image is None, 'needs Pillow')
class ReviewAndApplyTests(unittest.TestCase):
    def setUp(self):
        self.repo = Path(tempfile.mkdtemp())
        self.outside = Path(tempfile.mkdtemp())  # The working folder, on another drive on the owner's PC.
        self.patches = [mock.patch.object(clean_backgrounds, 'ROOT', self.repo),
                        mock.patch.object(clean_backgrounds, 'STAGE', self.outside / 'clean-stage'),
                        mock.patch.object(clean_backgrounds, 'REVIEW', self.outside / 'clean-review')]
        for patch in self.patches:
            patch.start()
        for name, colour in (('Tyre', (254, 254, 254)), ('Gauge', (120, 120, 120))):
            shipped = self.repo / 'mods' / 'M' / 'Textures' / f'{name}.png'
            shipped.parent.mkdir(parents=True, exist_ok=True)
            Image.new('RGBA', (8, 8), (20, 20, 20, 255)).save(shipped)
            staged = clean_backgrounds.STAGE / 'textures' / 'mods' / 'M' / 'Textures' / f'{name}.png'
            staged.parent.mkdir(parents=True, exist_ok=True)
            changed = Image.new('RGBA', (8, 8), (20, 20, 20, 255))
            changed.putpixel((4, 4), (*colour, 0))  # One pixel cleared in each.
            changed.save(staged)

    def tearDown(self):
        for patch in self.patches:
            patch.stop()
        shutil.rmtree(self.repo)
        shutil.rmtree(self.outside)

    def test_review_reads_a_stage_outside_the_repository(self):
        # 21 Nov 2026: review failed on every staged file once the stage moved to K: (19 Nov 2026).
        entries = clean_backgrounds.review()
        self.assertEqual(sorted(Path(entry['shipped']).name for entry in entries), ['Gauge.png', 'Tyre.png'])
        self.assertTrue(all(Path(entry['staged']).is_absolute() for entry in entries))

    def test_apply_only_leaves_every_other_change_staged(self):
        # 21 Nov 2026: a fix to three icons staged 21 changes, among them a gauge the AI would have bitten a blotch out of.
        clean_backgrounds.review()
        before = (self.repo / 'mods' / 'M' / 'Textures' / 'Gauge.png').read_bytes()
        replaced = clean_backgrounds.apply(only={'Tyre.png'})
        self.assertEqual(replaced, ['mods/M/Textures/Tyre.png'])
        self.assertEqual((self.repo / 'mods' / 'M' / 'Textures' / 'Gauge.png').read_bytes(), before)
        self.assertEqual(Image.open(self.repo / 'mods' / 'M' / 'Textures' / 'Tyre.png').getpixel((4, 4))[3], 0)


if __name__ == '__main__':
    unittest.main()
