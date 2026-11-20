"""The fleck checker's core (scripts/art/paint_check.py find_flecks), on made-up pictures. No GPU, no game, no parts maps."""
import os
import sys
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))
import paint_check  # noqa: E402

W, H = 12, 12


def picture(painted_part=1):
    """A 12 x 12 picture: one part filling it, every pixel considered, all painted 'main'."""
    kinds = ['main'] * (W * H)
    parts = [painted_part] * (W * H)
    considered = [True] * (W * H)
    return kinds, parts, considered


def at(x, y):
    return y * W + x


class FleckTests(unittest.TestCase):
    def test_a_clean_panel_has_none(self):
        self.assertEqual(paint_check.find_flecks(*picture(), W, H), [])

    def test_one_unpainted_pixel_inside_a_panel_is_a_fleck(self):
        kinds, parts, considered = picture()
        kinds[at(5, 5)] = None
        flecks = paint_check.find_flecks(kinds, parts, considered, W, H)
        self.assertEqual(flecks, [[at(5, 5)]])

    def test_a_long_stripe_is_not_a_fleck(self):
        kinds, parts, considered = picture()
        for x in range(1, 11):
            for y in (5, 6):
                kinds[at(x, y)] = 'accent'
        self.assertEqual(paint_check.find_flecks(kinds, parts, considered, W, H), [])

    def test_a_spot_ringed_by_outline_is_a_detail_not_a_fleck(self):
        kinds, parts, considered = picture()
        kinds[at(5, 5)] = None
        for x, y in ((4, 5), (6, 5), (5, 4), (5, 6)):
            considered[at(x, y)] = False
        self.assertEqual(paint_check.find_flecks(kinds, parts, considered, W, H), [])

    def test_a_spot_at_the_edge_of_its_part_is_a_seam_not_a_fleck(self):
        kinds, parts, considered = picture()
        for y in range(H):
            parts[at(11, y)] = 2
        kinds[at(10, 5)] = None
        self.assertEqual(paint_check.find_flecks(kinds, parts, considered, W, H), [])

    def test_a_fleck_is_filled_with_the_paint_that_rings_it(self):
        from PIL import Image
        mask = Image.new('RGB', (W, H), (255, 0, 0))
        mask.putpixel((5, 5), (0, 0, 0))
        mask.putpixel((6, 5), (0, 0, 0))
        changed = paint_check.fill_flecks(mask, [[at(5, 5), at(6, 5)]])
        self.assertEqual(changed, 2)
        self.assertEqual(mask.getpixel((5, 5)), (255, 0, 0))
        self.assertEqual(mask.getpixel((6, 5)), (255, 0, 0))

    def test_filling_leaves_the_rest_of_the_mask_alone(self):
        from PIL import Image
        mask = Image.new('RGB', (W, H), (255, 0, 0))
        mask.putpixel((0, 0), (0, 255, 0))
        mask.putpixel((5, 5), (0, 0, 0))
        paint_check.fill_flecks(mask, [[at(5, 5)]])
        self.assertEqual(mask.getpixel((0, 0)), (0, 255, 0))

    def test_a_part_without_a_clear_paint_has_no_flecks(self):
        kinds, parts, considered = picture()
        for index in range(0, W * H, 3):
            kinds[index] = None
        self.assertEqual(paint_check.find_flecks(kinds, parts, considered, W, H), [])

    def test_an_island_the_eye_cannot_see_is_left_out(self):
        kinds, parts, considered = picture()
        kinds[at(5, 5)] = None
        flecks = paint_check.find_flecks(kinds, parts, considered, W, H, visible=lambda island, paint: False)
        self.assertEqual(flecks, [])


if __name__ == '__main__':
    unittest.main()
