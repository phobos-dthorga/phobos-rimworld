"""export_overlay.py: an overlay keeps only what an edit added, framed and scaled exactly as export_sprite framed the base."""
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import export_overlay  # noqa: E402
import export_sprite  # noqa: E402


def base_and_edit(folder, shift=(0, 0)):
    """A synthetic 1:2 building (a grey box with an outline) and an edit adding a dark bar past its right edge."""
    base = Image.new('RGB', (200, 400), 'white')
    draw = ImageDraw.Draw(base)
    draw.rectangle((50, 100, 130, 300), fill=(120, 120, 120), outline=(10, 10, 10), width=4)
    edit = Image.new('RGB', (200, 400), 'white')
    edit.paste(base.crop((0, 0, 200 - shift[0], 400 - shift[1])), shift)
    ImageDraw.Draw(edit).rectangle((126 + shift[0], 180 + shift[1], 170 + shift[0], 200 + shift[1]), fill=(30, 30, 30))
    base_path, edit_path = folder / 'base.png', folder / 'edit.png'
    base.save(base_path)
    edit.save(edit_path)
    return base_path, edit_path


class ExportOverlayTest(unittest.TestCase):
    def setUp(self):
        self.folder = Path(tempfile.mkdtemp())
        self.no_mask = export_sprite.ai_background
        export_sprite.ai_background = lambda path: None  # No background AI in a test: plain white-to-alpha, as on a fresh master.

    def tearDown(self):
        export_sprite.ai_background = self.no_mask
        for path in self.folder.iterdir():
            path.unlink()
        self.folder.rmdir()

    def test_keeps_only_the_added_part_and_widens_evenly(self):
        base, edit = base_and_edit(self.folder)
        record = export_overlay.export(base, edit, self.folder / 'overlay.png', '1:2', size=32)
        overlay = Image.open(self.folder / 'overlay.png')
        self.assertEqual(overlay.height, 64)
        self.assertGreater(overlay.width, 32, 'the bar reaches past the base, so the canvas is wider')
        self.assertEqual(overlay.width % 4, 0)
        self.assertGreater(record['draw_size_for_base_aspect'][0], 1.0)
        self.assertEqual(record['draw_size_for_base_aspect'][1], 2.0)
        box = overlay.getchannel('A').getbbox()
        self.assertIsNotNone(box)
        self.assertGreater(box[0], overlay.width // 2, 'only the bar on the right is kept, not the box')

    def test_shift_undoes_a_misregistered_edit(self):
        base, edit = base_and_edit(self.folder, shift=(2, 2))
        export_overlay.export(base, edit, self.folder / 'overlay.png', '1:2', size=32, shift=(2, 2))
        overlay = Image.open(self.folder / 'overlay.png').getchannel('A')
        left_half = overlay.crop((0, 0, overlay.width // 2, overlay.height))
        self.assertIsNone(left_half.getbbox(), 'no outline slivers from the box once the shift is undone')

    def test_refuses_an_edit_that_adds_nothing(self):
        base, _ = base_and_edit(self.folder)
        with self.assertRaises(SystemExit):
            export_overlay.export(base, base, self.folder / 'overlay.png', '1:2', size=32)


if __name__ == '__main__':
    unittest.main()
