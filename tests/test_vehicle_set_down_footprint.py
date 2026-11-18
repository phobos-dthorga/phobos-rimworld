"""Code that sets a vehicle down must ask Vehicle Framework where the vehicle will stand, not the game.

Vehicle Framework reads a vehicle facing south as facing north, and west as east, before it works out the cells the vehicle
covers (``Ext_Vehicles.AdjustForVehicleOccupiedRect``). The game's own ``GenAdj.OccupiedRect(centre, rotation, size)`` does
not, so for a vehicle of even length the two answers are a cell apart. A room check made with the game's answer passed a spot
at the north or east map edge that the vehicle did not fit, and the vehicle was destroyed as it spawned: the hitch's trailer
(fault-read H1, fixed in Framework 0.120.5) and then, by the same line copied, the sling's load (fixed in 0.140.1).

This test reads our C# as text and refuses the game's three-argument footprint in the folders that put vehicles on the map.
Use ``vehicle.VehicleRect(cell, rotation)`` or ``vehicleDef.VehicleRect(cell, rotation)`` there.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FOLDERS = ('src/PhobosFramework.Vehicles/Sling', 'src/PhobosFramework.Vehicles/Hitch')
GAME_FOOTPRINT = re.compile(r'\bGenAdj\s*\.\s*OccupiedRect\s*\(')


def strip_comments(text):
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    return re.sub(r'//[^\n]*', '', text)


def offences(text):
    return [text.count('\n', 0, match.start()) + 1 for match in GAME_FOOTPRINT.finditer(strip_comments(text))]


class VehicleSetDownFootprint(unittest.TestCase):
    def test_the_check_sees_the_fault_it_was_written_for(self):
        self.assertEqual(offences('CellRect rect = GenAdj.OccupiedRect(cell, rotation, design.Size);\n'), [1])

    def test_a_comment_naming_it_is_not_an_offence(self):
        self.assertEqual(offences('// the game\'s GenAdj.OccupiedRect(cell, rot, size) was a cell out\nvar r = load.VehicleRect(cell, rot);\n'), [])

    def test_vehicle_framework_footprint_passes(self):
        self.assertEqual(offences('CellRect rect = design.VehicleRect(cell, rotation);\n'), [])

    def test_no_set_down_code_uses_the_game_footprint(self):
        found = []
        for folder in FOLDERS:
            base = ROOT / folder
            self.assertTrue(base.is_dir(), f'{folder} is missing: update FOLDERS in this test')
            for path in sorted(base.rglob('*.cs')):
                for line in offences(path.read_text(encoding='utf-8-sig')):
                    found.append(f'{path.relative_to(ROOT).as_posix()}:{line}')
        self.assertEqual(found, [], 'the game\'s footprint is used where a vehicle is set down; use VehicleRect: ' + ', '.join(found))


if __name__ == '__main__':
    unittest.main()
