"""The vehicle art export: the master is never changed, the facings are the north turned, and an overlay registers with its body."""
import hashlib
import json
from pathlib import Path
import sys
import tempfile
import unittest

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'scripts' / 'art'))
import export_vehicle as ev  # noqa: E402

OLIVE = (110, 110, 60)
ORANGE = (200, 110, 40)
GREY_DECK = (70, 70, 72)


def truck(path):
    """A 100 x 300 'truck' on a 200 x 400 white canvas: olive cab on top with an orange stripe, grey deck below."""
    image = Image.new('RGB', (200, 400), (255, 255, 255))
    image.paste(OLIVE, (50, 50, 150, 150))
    image.paste(ORANGE, (50, 90, 150, 110))
    image.paste(GREY_DECK, (50, 150, 150, 350))
    image.save(path)


class ExportCase(unittest.TestCase):
    def setUp(self):
        folder = tempfile.TemporaryDirectory()
        self.addCleanup(folder.cleanup)
        self.root = Path(folder.name)
        self.master = self.root / 'master.png'
        truck(self.master)
        self.out = self.root / 'out'

    def body(self):
        return ev.export_body(self.master, self.out, 'Truck', 3, (2, 4), (38, 95), (8, 34))

    def opened(self, name):
        return Image.open(self.out / name).convert('RGBA')


class BodyTests(ExportCase):
    def test_the_master_is_left_exactly_as_it_was(self):
        before = hashlib.sha256(self.master.read_bytes()).hexdigest()
        self.body()
        self.assertEqual(before, hashlib.sha256(self.master.read_bytes()).hexdigest())

    def test_the_vehicle_is_as_many_cells_long_as_asked_on_a_draw_size_canvas(self):
        self.body()
        north = self.opened('Truck_north.png')
        self.assertEqual((256, 512), north.size)
        left, top, right, bottom = north.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox()
        self.assertAlmostEqual(3 * 128, bottom - top, delta=1)
        self.assertAlmostEqual(256, (top + bottom) / 2, delta=1)
        self.assertAlmostEqual(128, (left + right) / 2, delta=1)

    def test_east_and_south_are_the_north_turned_clockwise(self):
        self.body()
        north, east, south = (self.opened(f'Truck_{facing}.png') for facing in ('north', 'east', 'south'))
        self.assertEqual((512, 256), east.size)
        self.assertEqual(list(north.transpose(Image.ROTATE_270).getdata()), list(east.getdata()))
        self.assertEqual(list(north.transpose(Image.ROTATE_180).getdata()), list(south.getdata()))

    def test_paint_is_grey_under_the_mask_and_the_colour_gives_the_tone_back(self):
        result = self.body()
        north, mask = self.opened('Truck_north.png'), self.opened('Truck_northm.png')
        cab = (128, 150)  # inside the olive cab, clear of the stripe
        red, green, blue, _ = north.getpixel(cab)
        self.assertEqual(red, green)
        self.assertEqual(green, blue)
        self.assertEqual((255, 0, 0), mask.getpixel(cab)[:3])
        for wanted, colour in zip(OLIVE, result['colours']['color']):
            self.assertAlmostEqual(wanted, colour * red / 255, delta=2)

    def test_the_accent_has_its_own_channel_and_the_bare_deck_none(self):
        self.body()
        mask = self.opened('Truck_northm.png')
        self.assertEqual((0, 255, 0), mask.getpixel((128, 128))[:3])  # the orange stripe
        self.assertEqual((0, 0, 0), mask.getpixel((128, 400))[:3])  # the grey deck

    def test_a_greyish_paint_is_painted_only_when_asked(self):
        faint = (80, 84, 88)  # a grey-blue of saturation 0.09, as the Seabed Crawler's hull measured (0.06 to 0.12)
        image = Image.new('RGB', (200, 400), (255, 255, 255))
        image.paste(faint, (50, 50, 150, 350))
        image.save(self.master)
        ev.export_body(self.master, self.out, 'Truck', 3, (2, 4), (185, 240), (8, 34))
        self.assertEqual((0, 0, 0), self.opened('Truck_northm.png').getpixel((128, 256))[:3])
        ev.export_body(self.master, self.out, 'Truck', 3, (2, 4), (185, 240), (8, 34), min_saturation=0.05)
        self.assertEqual((255, 0, 0), self.opened('Truck_northm.png').getpixel((128, 256))[:3])

    def test_an_icon_is_written_at_64_pixels(self):
        self.body()
        self.assertEqual((64, 64), self.opened('Truck_icon.png').size)


class OverlayTests(ExportCase):
    def overlay_master(self, box):
        path = self.root / 'tank.png'
        image = Image.new('RGB', (200, 400), (255, 255, 255))
        image.paste(GREY_DECK, box)
        image.save(path)
        return path

    def content(self, name):
        return self.opened(name).getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox()

    def test_an_overlay_drawn_in_place_lands_where_the_same_pixels_of_the_body_do(self):
        self.body()
        ev.export_overlay(self.overlay_master((50, 150, 150, 350)), self.out / 'Truck.transform.json', self.out, 'Tank')
        transform = json.loads((self.out / 'Truck.transform.json').read_text(encoding='utf-8'))
        scale, (x, y) = transform['scale'], transform['offset']
        left, top, right, bottom = self.content('Tank_north.png')
        self.assertAlmostEqual(50 * scale + x, left, delta=2)
        self.assertAlmostEqual(350 * scale + y, bottom, delta=2)

    def test_fit_moves_a_misplaced_overlay_into_the_box(self):
        self.body()
        placed = ev.export_overlay(self.overlay_master((10, 10, 110, 210)), self.out / 'Truck.transform.json', self.out, 'Tank', fit=[50, 150, 150, 350])
        self.assertTrue(placed['files'])
        transform = json.loads((self.out / 'Truck.transform.json').read_text(encoding='utf-8'))
        left, top, _, _ = self.content('Tank_north.png')
        self.assertAlmostEqual(50 * transform['scale'] + transform['offset'][0], left, delta=2)
        self.assertAlmostEqual(150 * transform['scale'] + transform['offset'][1], top, delta=2)

    def test_damage_marks_keep_only_their_darkness(self):
        self.body()
        path = self.root / 'marks.png'
        image = Image.new('RGB', (200, 400), (255, 255, 255))
        image.paste((0, 0, 0), (60, 200, 90, 230))
        image.paste((200, 200, 200), (100, 200, 130, 230))  # a pale edge of soot
        image.save(path)
        transform = ev.export_overlay(path, self.out / 'Truck.transform.json', self.out, 'Marks', marks=True)
        self.assertTrue(transform['files'])
        scale = json.loads((self.out / 'Truck.transform.json').read_text(encoding='utf-8'))
        place = lambda x, y: (round(x * scale['scale'] + scale['offset'][0]), round(y * scale['scale'] + scale['offset'][1]))
        north = self.opened('Marks_north.png')
        dark, pale, clear = north.getpixel(place(75, 215)), north.getpixel(place(115, 215)), north.getpixel(place(30, 30))
        self.assertGreater(dark[3], 240)
        self.assertLess(pale[3], 80)
        self.assertLess(pale[0], 40)  # dark, not a white halo
        self.assertEqual(0, clear[3])

    def test_an_overlay_on_another_canvas_size_is_refused(self):
        self.body()
        path = self.root / 'small.png'
        Image.new('RGB', (100, 100), (255, 255, 255)).save(path)
        with self.assertRaises(ValueError):
            ev.export_overlay(path, self.out / 'Truck.transform.json', self.out, 'Tank')


if __name__ == '__main__':
    unittest.main()


class GreyPaintTests(unittest.TestCase):
    """The Osprey's grey paint (19 Nov 2026): a saturation ceiling keeps glass of the paint's hue out, and a colourless pixel is the
    grey paint whatever its hue, so seam slivers are not left as pale flecks. Other vehicles, with no ceiling, are unchanged."""
    GREY = dict(main_hue=(200, 280), accent_hue=(0, 34), min_saturation=0.0, main_max_saturation=0.15)

    def test_the_airframe_grey_is_the_main_paint(self):
        self.assertEqual(ev.classify(204, 205, 209, **self.GREY), 'main')

    def test_glass_of_the_same_hue_is_not_paint(self):
        self.assertIsNone(ev.classify(80, 90, 110, **self.GREY))

    def test_a_colourless_pixel_is_the_grey_paint_whatever_its_hue(self):
        self.assertEqual(ev.classify(150, 150, 150, **self.GREY), 'main')

    def test_the_orange_band_is_the_accent(self):
        self.assertEqual(ev.classify(202, 126, 76, **self.GREY), 'accent')

    def test_without_a_ceiling_a_colourless_pixel_stays_unpainted(self):
        self.assertIsNone(ev.classify(150, 150, 150, (38, 95), (8, 34)))


class ThinLineTests(unittest.TestCase):
    """The Osprey's seams (19 Nov 2026): a thin dark mark is found, a wide dark panel and the silhouette's outline are not, and a
    seam takes the paint of the panel it crosses, darkest pixels too."""

    @staticmethod
    def panel():
        image = Image.new('RGBA', (60, 60), (0, 0, 0, 0))
        for x in range(5, 55):
            for y in range(5, 55):
                image.putpixel((x, y), (204, 205, 209, 255))      # the airframe's grey
        for y in range(10, 50):
            for x in (28, 29, 30):
                image.putpixel((x, y), (50, 47, 55, 255))         # a seam, 3 px wide, its middle darker than the outline value
            image.putpixel((29, y), (40, 38, 44, 255))
        for x in range(38, 52):
            for y in range(20, 40):
                image.putpixel((x, y), (58, 56, 64, 255))         # a wide dark panel (a sponson side)
        return image

    def test_a_seam_is_thin_and_a_wide_dark_panel_is_not(self):
        thin = ev.thin_lines(self.panel(), (0.45, 9))
        self.assertEqual(thin.getpixel((29, 30)), 255)
        self.assertEqual(thin.getpixel((45, 30)), 0)

    def test_the_silhouette_outline_is_never_thin(self):
        thin = ev.thin_lines(self.panel(), (0.45, 9))
        self.assertEqual(thin.getpixel((29, 6)), 0)

    def test_the_whole_seam_takes_the_panel_paint(self):
        grey = dict(min_saturation=0.0, main_max_saturation=0.15)
        kinds = ev.region_kinds(self.panel(), (200, 280), (0, 34), line=(0.45, 9), **grey)
        self.assertEqual(kinds[30 * 60 + 29], 'main')
        self.assertEqual(kinds[30 * 60 + 28], 'main')


class PaintEdgeTests(unittest.TestCase):
    """The repaint's rust (owner's review, 23 Nov 2026): the paint's own darker lines and rims join the paint, so a red truck has no olive
    lines left; a lighter pixel of the same hue, the outline, and anything across the outline stay as drawn."""

    PANEL, LINE, CREAM, OUTLINE, GREY = (120, 118, 80, 255), (90, 90, 72, 255), (214, 206, 170, 255), (20, 20, 18, 255), (130, 130, 128, 255)
    OLIVE = (38, 95)

    def joined(self, columns, holes=()):
        """A 16 x 10 picture: a painted olive panel in columns 0 to 7, then the given columns of colour; returns the kinds afterwards."""
        image = Image.new('RGBA', (16, 10), (0, 0, 0, 0))
        for y in range(10):
            for x in range(8):
                image.putpixel((x, y), self.PANEL)
            for offset, colour in enumerate(columns):
                image.putpixel((8 + offset, y), colour)
        for point in holes:
            image.putpixel(point, self.GREY)
        kinds = [('main' if index % 16 < 8 else None) for index in range(160)]
        for x, y in holes:
            if x < 8:
                kinds[y * 16 + x] = None
        ev.absorb_paint_coloured_edges(image, kinds, self.OLIVE, (8, 34), 0.16, 1.0)
        return kinds

    def test_a_darker_line_of_the_paint_joins_it(self):
        kinds = self.joined([self.LINE, self.LINE])
        self.assertEqual(kinds[5 * 16 + 8], 'main')
        self.assertEqual(kinds[5 * 16 + 9], 'main')

    def test_a_lighter_cream_of_the_same_hue_and_the_outline_do_not(self):
        kinds = self.joined([self.CREAM, self.OUTLINE])
        self.assertIsNone(kinds[5 * 16 + 8])
        self.assertIsNone(self.joined([self.OUTLINE])[5 * 16 + 8])

    def test_nothing_joins_across_the_outline(self):
        kinds = self.joined([self.OUTLINE, self.LINE])
        self.assertIsNone(kinds[5 * 16 + 9])

    def test_a_gap_in_a_joined_line_fills_and_a_mark_inside_the_panel_stays(self):
        kinds = self.joined([self.LINE, self.LINE], holes=[(8, 5), (3, 5)])
        self.assertEqual(kinds[5 * 16 + 8], 'main')
        self.assertIsNone(kinds[5 * 16 + 3])
