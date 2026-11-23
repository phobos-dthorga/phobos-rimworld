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


if __name__ == '__main__':
    unittest.main()
