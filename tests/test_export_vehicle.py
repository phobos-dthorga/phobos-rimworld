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
