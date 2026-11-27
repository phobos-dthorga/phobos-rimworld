"""The HF table maker (scripts/radio/hf_tables.py): VOACAP's fixed-column input deck, reading its output, local hours, combining a
cell's paths and packing. No VOACAP here: the output below is written by hand in VOACAP's layout, with made-up figures."""
import os
import struct
import sys
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'radio'))

import hf_tables as t  # noqa: E402

TWO = {"40m": 7.10, "20m": 14.20}

# Two hours in VOACAP's method 30 layout: the first column after the hour is the MUF's own; a dash is no figure.
OUTPUT = """
   1.0 12.0  7.1 14.2  0.0  0.0  0.0  0.0  0.0  0.0  0.0  0.0  0.0 FREQ
        1F2  1F2  1F2   -    -    -    -    -    -    -    -    -  MODE
       0.50 0.90 0.20   -    -    -    -    -    -    -    -    -  MUFday
        140  125  150   -    -    -    -    -    -    -    -    -  LOSS
       -170 -160 -175   -    -    -    -    -    -    -    -    -  N DBW
       25.0  9.0 12.0   -    -    -    -    -    -    -    -    -  SIG LW
       11.0  5.0  6.0   -    -    -    -    -    -    -    -    -  SIG UP
   2.0 11.0  7.1 14.2  0.0  0.0  0.0  0.0  0.0  0.0  0.0  0.0  0.0 FREQ
       0.50 0.80    -   -    -    -    -    -    -    -    -    -  MUFday
        139  126    -   -    -    -    -    -    -    -    -    -  LOSS
"""


class DeckTests(unittest.TestCase):
    def card(self, name, deck):
        return next(line for line in deck.splitlines() if line.startswith(name))

    def test_each_frequency_sits_in_its_own_five_wide_field_from_column_11(self):
        line = self.card("FREQUENCY", t.deck(6, 100, 30, 0, 0, 1000))
        for i, mhz in enumerate(t.BANDS.values()):
            self.assertAlmostEqual(float(line[10 + 5 * i:15 + 5 * i]), mhz, places=2)

    def test_the_circuit_card_keeps_voacaps_columns(self):
        line = self.card("CIRCUIT", t.deck(6, 100, 30, 0, 0, 1000))
        self.assertEqual(line[15], "N")
        self.assertEqual(line[25], "E")
        self.assertEqual(line[35], "N")
        self.assertEqual(line[45], "E")
        self.assertTrue(line.endswith("  S     0"))

    def test_the_sunspot_card_reads_the_number(self):
        self.assertEqual(self.card("SUNSPOT", t.deck(6, 150, 0, 0, 0, 100)), "SUNSPOT    150.")


class GeometryTests(unittest.TestCase):
    def test_a_north_south_path_straddles_its_midpoint(self):
        (lat1, lon1), (lat2, lon2) = t.endpoints(0, 0, 0, 1000)
        self.assertAlmostEqual(lat1, 4.5, delta=0.05)
        self.assertAlmostEqual(lat2, -4.5, delta=0.05)
        self.assertAlmostEqual(lon1, 0, delta=1e-6)

    def test_an_east_west_path_at_the_equator_stays_on_it(self):
        (lat1, lon1), (lat2, lon2) = t.endpoints(0, 170, 90, 2000)
        self.assertAlmostEqual(lat1, 0, delta=1e-6)
        self.assertAlmostEqual(lon1, 179.0, delta=0.05)
        self.assertAlmostEqual(lon2, 161.0, delta=0.05)

    def test_longitude_wraps_past_180(self):
        (_, lon1), _ = t.endpoints(0, 175, 90, 2000)
        self.assertAlmostEqual(lon1, -176.0, delta=0.05)


class ReadingTests(unittest.TestCase):
    def test_bands_follow_the_mufs_own_column(self):
        hours = t.parse(OUTPUT, TWO)
        self.assertEqual(hours[1]["muf"], 12.0)
        self.assertEqual(hours[1]["bands"]["40m"], {"mufday": 0.9, "loss": 125.0, "noise": -160.0, "sigLow": 9.0, "sigHigh": 5.0})
        self.assertEqual(hours[1]["bands"]["20m"]["loss"], 150.0)

    def test_a_dash_is_left_out(self):
        self.assertNotIn("20m", t.parse(OUTPUT, TWO)[2]["bands"])

    def test_hour_24_is_midnight(self):
        hours = t.parse(OUTPUT.replace("   1.0 12.0", "  24.0 12.0"), TWO)
        self.assertIn(0, hours)

    def test_the_frequencies_read_are_checked(self):
        self.assertTrue(t.frequencies_read(OUTPUT, TWO))
        self.assertFalse(t.frequencies_read(OUTPUT.replace(" 14.2  0.0", "214.2  0.0", 1), TWO))

    def test_universal_hours_become_the_midpoints_local_hours(self):
        self.assertEqual(sorted(t.to_local({0: "a", 23: "b"}, 90)), [5, 6])


class CombineTests(unittest.TestCase):
    def sample(self, opened, loss):
        return {h: {"muf": 10.0, "bands": {"40m": {"mufday": opened, "loss": loss, "noise": -160.0, "sigLow": 9.0, "sigHigh": 5.0}}}
                for h in range(24)}

    def test_open_is_a_mean_and_loss_a_median(self):
        cell = t.combine([self.sample(1.0, 120), self.sample(0.0, 300), self.sample(0.5, 130)], {"40m": 7.1})
        self.assertAlmostEqual(cell[12]["bands"]["40m"]["mufday"], 0.5)
        self.assertEqual(cell[12]["bands"]["40m"]["loss"], 130)


class PackTests(unittest.TestCase):
    def test_the_header_names_every_axis_and_the_size_adds_up(self):
        cells = {(6, 100, 30, 1000): {h: {"muf": 12.0, "bands": {"40m": {"mufday": 0.9, "loss": 125, "noise": -160, "sigLow": 9}}}
                                      for h in range(24)}}
        data = t.pack(cells, ["40m", "20m"])
        head = t.unpack_header(data)
        self.assertEqual(head["sunspots"], t.SUNSPOTS)
        self.assertEqual(head["latitudes"], t.LATITUDES)
        self.assertEqual(head["kHz"], [7100, 14200])
        self.assertEqual(head["size"], len(data))

    def test_a_cell_packs_to_its_bytes(self):
        cells = {(1, 0, -80, 50): {0: {"muf": 12.0, "bands": {"40m": {"mufday": 0.9, "loss": 125, "noise": -160, "sigLow": 9}}}}}
        data = t.pack(cells, ["40m"])
        at = t.unpack_header(data)["paths"]
        self.assertEqual(list(data[at:at + 4]), [48, 225, 45, 90])  # 12 MHz / 0.25; 0.9 x 250; 125 - 80; 9 dB / 0.1
        self.assertEqual(data[t.unpack_header(data)["noise"]], 60)  # -160 dBW/Hz

    def test_no_path_and_no_figure_pack_as_255(self):
        cells = {(1, 0, -80, 50): {0: {"muf": 12.0, "bands": {"40m": {"mufday": 0.0, "loss": 400}}}}}
        data = t.pack(cells, ["40m"])
        at = t.unpack_header(data)["paths"]
        self.assertEqual(data[at + 2], 255)

    def test_the_format_is_marked(self):
        data = t.pack({}, ["40m"])
        self.assertEqual(data[:4], b"PHFT")
        self.assertEqual(struct.unpack_from("<H", data, 4)[0], t.FORMAT)


if __name__ == '__main__':
    unittest.main()
