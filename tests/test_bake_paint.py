"""scripts/art/bake_paint.py: a painted picture becomes what the game drew in its own colours, and its pale rim takes the outline's colour."""
import json
from pathlib import Path
import sys
import tempfile
import unittest

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'scripts' / 'art'))
import bake_paint  # noqa: E402


class BakeTests(unittest.TestCase):
    def test_paint_is_the_grey_times_the_colour_and_bare_parts_stay(self):
        texture = Image.new('RGBA', (3, 1), (200, 200, 200, 255))
        mask = Image.new('RGB', (3, 1), (0, 0, 0))
        mask.putpixel((0, 0), (255, 0, 0))
        mask.putpixel((1, 0), (0, 255, 0))
        result = bake_paint.baked(texture, mask, (128, 255, 64), (255, 0, 0))
        self.assertEqual(result.getpixel((0, 0)), (100, 200, 50, 255))
        self.assertEqual(result.getpixel((1, 0)), (200, 0, 0, 255))
        self.assertEqual(result.getpixel((2, 0)), (200, 200, 200, 255))

    def test_alpha_is_kept(self):
        texture = Image.new('RGBA', (1, 1), (200, 200, 200, 90))
        result = bake_paint.baked(texture, Image.new('RGB', (1, 1), (255, 0, 0)), (255, 255, 255), (255, 255, 255))
        self.assertEqual(result.getpixel((0, 0))[3], 90)


class RimTests(unittest.TestCase):
    @staticmethod
    def outline_with_pale_rim():
        image = Image.new('RGBA', (8, 5), (0, 0, 0, 0))
        for y in range(5):
            image.putpixel((1, y), (170, 170, 170, 140))   # the pale half-transparent rim
            image.putpixel((2, y), (20, 20, 20, 255))      # the outline
            for x in range(3, 8):
                image.putpixel((x, y), (90, 120, 60, 255))  # the picture
        return image

    def test_a_pale_rim_takes_the_outline_colour_and_keeps_its_transparency(self):
        cleaned, changed = bake_paint.clean_rim(self.outline_with_pale_rim())
        self.assertEqual(cleaned.getpixel((1, 2)), (20, 20, 20, 140))
        self.assertGreater(changed, 0)

    def test_opaque_pixels_and_a_rim_no_paler_than_inside_stay(self):
        image = self.outline_with_pale_rim()
        for y in range(5):
            image.putpixel((1, y), (25, 25, 25, 140))
        cleaned, changed = bake_paint.clean_rim(image)
        self.assertEqual(changed, 0)
        self.assertEqual(cleaned.getpixel((3, 2)), (90, 120, 60, 255))


class HoleTests(unittest.TestCase):
    """The cultivator's roller gaps (owner's review, 23 Nov 2026): white clears only where the background AI calls it background."""

    def test_white_the_ai_calls_background_clears_and_shiny_white_stays(self):
        picture = Image.new('RGBA', (10, 4), (60, 60, 60, 255))
        for x in range(2, 5):
            picture.putpixel((x, 1), (255, 255, 255, 255))   # a hole
        for x in range(6, 9):
            picture.putpixel((x, 1), (255, 255, 255, 255))   # a shiny part, as white
        verdict = Image.new('L', (10, 4), 255)
        for x in range(2, 5):
            verdict.putpixel((x, 1), 0)
        original = bake_paint.ai_background
        bake_paint.ai_background = lambda master: verdict
        try:
            with tempfile.TemporaryDirectory() as root:
                master = Path(root) / 'master.png'
                Image.new('RGB', (10, 4)).save(master)
                result, cleared = bake_paint.clear_ai_holes(picture, master, {'scale': 1.0, 'offset': [0, 0]})
        finally:
            bake_paint.ai_background = original
        self.assertEqual(result.getpixel((3, 1))[3], 0)
        self.assertEqual(result.getpixel((7, 1)), (255, 255, 255, 255))
        self.assertEqual(result.getpixel((0, 0)), (60, 60, 60, 255))
        self.assertEqual(cleared, 3)


class PlanTests(unittest.TestCase):
    def test_apply_writes_turned_facings_and_drops_the_mask(self):
        with tempfile.TemporaryDirectory() as root:
            folder = Path(root) / 'Machine'
            folder.mkdir()
            north = Image.new('RGBA', (4, 6), (100, 100, 100, 255))
            for facing, turn in bake_paint.FACINGS.items():
                (north if turn is None else north.transpose(turn)).save(folder / f'Thing_{facing}.png')
                Image.new('RGB', (4, 6) if turn != Image.ROTATE_270 else (6, 4), (255, 0, 0)).save(folder / f'Thing_{facing}m.png')
            plan = {'textures': root, 'out': str(Path(root) / 'out'),
                    'pictures': [{'name': 'Machine/Thing', 'color': [255, 128, 0], 'colorTwo': [0, 0, 255]}]}
            bake_paint.run(plan, apply=True)
            self.assertFalse((folder / 'Thing_northm.png').exists())
            self.assertEqual(Image.open(folder / 'Thing_north.png').getpixel((0, 0)), (100, 50, 0, 255))
            self.assertEqual(Image.open(folder / 'Thing_east.png').size, (6, 4))
            json.dumps(plan)


if __name__ == '__main__':
    unittest.main()
