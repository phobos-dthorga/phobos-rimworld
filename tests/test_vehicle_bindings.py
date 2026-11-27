"""The vehicle bindings audit (scripts/vehicle-bindings.py), on a synthetic report."""
import importlib.util
import io
import json
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / 'scripts' / 'vehicle-bindings.py'
spec = importlib.util.spec_from_file_location('vehicle_bindings', SCRIPT)
vehicle_bindings = importlib.util.module_from_spec(spec)
spec.loader.exec_module(vehicle_bindings)

REPORT = {
    'written': '2026-11-23T00:00:00Z',
    'vehicles': [{
        'def': 'Test_Jet', 'label': 'test jet', 'mod': 'someone.testjet', 'loadout': 'PhobosVehicleLoadout_Test',
        'systems': [
            {'def': 'PhobosVehicleSystem_Turbine', 'roles': ['Engine']},
            {'def': 'PhobosVehicleSystem_Fuel', 'roles': ['FuelPump', 'FuelTank']},
            {'def': 'PhobosVehicleSystem_Cabin', 'roles': []},
        ],
        'roles': {'Engine': ['EngineCover'], 'Engine1': ['LeftEngine']},
        'added': [],
        'parts': [
            {'key': 'LeftEngine', 'label': 'left engine', 'depth': 'Internal', 'categories': ['FlightSpeed']},
            {'key': 'EngineCover', 'label': 'engine cover', 'depth': 'External', 'categories': []},
            {'key': 'Gyro', 'label': 'gyroscope', 'depth': 'Internal', 'categories': ['FlightControl']},
        ],
    }],
}


class VehicleBindingsAuditTests(unittest.TestCase):
    def test_finds_each_kind(self):
        found = vehicle_bindings.audit(REPORT, rules={})
        self.assertEqual(['PhobosVehicleSystem_Fuel'], [f['system'] for f in found['unboundSystems']])
        self.assertEqual(['EngineCover'], [f['part'] for f in found['coverLikeParts']])
        self.assertEqual(['Gyro'], [f['part'] for f in found['unwatchedParts']])

    def test_a_system_with_no_roles_is_not_unbound(self):
        found = vehicle_bindings.audit(REPORT, rules={})
        self.assertNotIn('PhobosVehicleSystem_Cabin', [f['system'] for f in found['unboundSystems']])

    def test_expected_findings_and_our_own_parts_are_left_out(self):
        report = json.loads(json.dumps(REPORT))
        report['vehicles'][0]['roles']['RecoveryMount'] = ['PhobosRecoveryMount']
        report['vehicles'][0]['added'] = ['PhobosRecoveryMount']
        report['vehicles'][0]['parts'].append({'key': 'PhobosRecoveryMount', 'label': 'recovery mount', 'depth': 'Internal', 'categories': []})
        rules = {'coverLike': [{'role': 'Engine', 'partContains': 'cover'}], 'unwatched': [{'partContains': 'Gyro'}]}
        found = vehicle_bindings.audit(report, rules=rules)
        self.assertEqual([], found['coverLikeParts'])
        self.assertEqual([], found['unwatchedParts'])

    def test_runs_from_a_file_and_shows_one_vehicle(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'VehicleBindings.json'
            path.write_text(json.dumps(REPORT), encoding='utf-8')
            out = io.StringIO()
            self.assertEqual(0, vehicle_bindings.main(['--file', str(path)], out=out))
            self.assertIn('Cover-like parts: 1', out.getvalue())
            out = io.StringIO()
            self.assertEqual(0, vehicle_bindings.main(['show', 'Test_Jet', '--file', str(path)], out=out))
            self.assertIn('whole vehicle', out.getvalue())
            self.assertIn('Parts no role binds: 1', out.getvalue())


def _vehicle(def_name, loadout, systems, roles):
    return {'def': def_name, 'label': def_name, 'mod': 'someone.test', 'loadout': loadout,
            'systems': [{'def': s, 'roles': []} for s in systems], 'roles': {r: ['P' + r] for r in roles}, 'added': [], 'parts': []}


STANDARD = {
    'kinds': [
        {'kind': 'trailer', 'match': {'loadoutContains': 'Trailer'}},
        {'kind': 'submarine', 'match': {'vehicles': ['Test_Sub']}},
        {'kind': 'rotor', 'match': {'hasRole': 'RotorDrive'}},
        {'kind': 'fixed-wing', 'match': {'hasSystem': 'Flight'}},
        {'kind': 'land', 'match': {}},
    ],
    'powered': ['Electrical'],
    'lines': [
        {'line': 'VS1', 'label': 'comms', 'status': 'built', 'kinds': ['land', 'fixed-wing', 'rotor'], 'poweredOnly': True, 'systems': ['Comms']},
        {'line': 'VS2', 'label': 'aviation bay', 'status': 'built', 'kinds': ['fixed-wing', 'rotor'], 'roles': ['AviationRadioMount']},
        {'line': 'VS6', 'label': 'fuel', 'status': 'planned', 'batch': 3, 'kinds': ['fixed-wing'], 'systems': ['Fuel']},
        {'line': 'VS10', 'label': 'galley', 'status': 'planned', 'batch': 4, 'vehicles': ['Test_Truck'], 'roles': ['FridgeBay']},
    ],
    'except': [{'vehicle': 'Test_Heli', 'line': 'VS2', 'why': 'a test'}],
}


class VehicleStandardTests(unittest.TestCase):
    """The audit's "below the standard" kind (config/vehicle-standard.json; docs/development/vehicle-standard.md)."""

    def report(self):
        return {'vehicles': [
            _vehicle('Test_Truck', 'PhobosVehicleLoadout_Truck', ['Electrical'], []),
            _vehicle('Test_Loco', 'PhobosVehicleLoadout_Steam', ['Boiler'], []),
            _vehicle('Test_Jet', 'PhobosVehicleLoadout_Jet', ['Electrical', 'Flight', 'Comms'], []),
            _vehicle('Test_Heli', 'PhobosVehicleLoadout_Heli', ['Electrical', 'Flight', 'Comms'], ['RotorDrive']),
            _vehicle('Test_Sub', 'PhobosVehicleLoadout_Boat', ['Electrical'], []),
            _vehicle('Test_Cart', 'PhobosVehicleLoadout_Trailer', [], []),
        ]}

    def test_sorts_each_vehicle_into_its_kind(self):
        found = vehicle_bindings.below_standard(self.report(), STANDARD)
        self.assertEqual({'fixed-wing': 1, 'land': 2, 'rotor': 1, 'submarine': 1, 'trailer': 1}, found['kinds'])

    def test_a_built_line_missed_is_a_finding_and_only_where_it_applies(self):
        found = vehicle_bindings.below_standard(self.report(), STANDARD)
        missing = {(f['vehicle'], f['line']) for f in found['belowStandard']}
        # The truck lacks comms; the steam engine has no power, so VS1 does not ask it; the jet lacks its aviation bay; the helicopter is
        # excused from VS2; the submarine's kind has no lines.
        self.assertEqual({('Test_Truck', 'VS1'), ('Test_Jet', 'VS2')}, missing)

    def test_planned_lines_are_listed_apart_including_one_naming_its_vehicles(self):
        found = vehicle_bindings.below_standard(self.report(), STANDARD)
        self.assertEqual({('Test_Jet', 'VS6'), ('Test_Truck', 'VS10')}, {(f['vehicle'], f['line']) for f in found['stillToCome']})
        self.assertEqual({3, 4}, {f['batch'] for f in found['stillToCome']})

    def test_the_command_prints_both_sections(self):
        out = io.StringIO()
        found = vehicle_bindings.full_audit(self.report(), rules={}, std=STANDARD)
        vehicle_bindings.print_audit(found, out)
        self.assertIn('Below the standard: 2', out.getvalue())
        self.assertIn('Still to come: 2', out.getvalue())

    def test_a_military_vehicle_showing_a_civilian_bay_is_a_finding(self):
        # Framework 0.368.0 (VS11): a line for military vehicles only, naming roles that should not be there.
        std = {**STANDARD, 'lines': [{'line': 'VS11', 'label': 'no civilian bays', 'status': 'built', 'kinds': ['land'], 'militaryOnly': True,
                                      'absentRoles': ['StereoSlot']}]}
        tank = _vehicle('Test_Tank', 'PhobosVehicleLoadout_Tracked', ['Electrical'], ['StereoSlot'])
        tank['military'] = True
        truck = _vehicle('Test_Truck', 'PhobosVehicleLoadout_Truck', ['Electrical'], ['StereoSlot'])
        clean = _vehicle('Test_Tank2', 'PhobosVehicleLoadout_Tracked', ['Electrical'], [])
        clean['military'] = True
        found = vehicle_bindings.below_standard({'vehicles': [tank, truck, clean]}, std)
        self.assertEqual([('Test_Tank', ['StereoSlot (should not be there)'])], [(f['vehicle'], f['lacks']) for f in found['belowStandard']])

    def test_the_shipped_standard_reads(self):
        std = vehicle_bindings.standard()
        self.assertTrue(std['lines'])
        self.assertEqual('land', std['kinds'][-1]['kind'])


if __name__ == '__main__':
    unittest.main()
