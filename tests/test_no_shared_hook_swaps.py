"""Our code never puts a function of its own into one of Vehicle Framework's shared hooks.

``VehicleTurret.ProjectileAngleCE`` and its neighbours are single static functions that Combat Extended's vehicle compatibility
fills in. Other mods wrap them for the length of one shot: they keep what they found, put their own in, and put the old one back
afterwards (the Mongoose's and the Mi-24 Hind's ``Patch_FireTurret_Converge``). Framework 0.119.0 put a wrapper of ours into
``ProjectileAngleCE`` and left it there. The next time a Mongoose fired its rocket pods, its wrapper kept ours as "the original"
while ours took its wrapper as "Combat Extended's", each called the other, and the game closed with a stack overflow (17 Nov 2026;
fixed in 0.120.2 by a prefix on Combat Extended's own function instead). Two wrappers that each believe the other is the original
cannot be made safe from one side, so this test reads our C# as text and refuses any assignment to those hooks.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HOOKS = ('LaunchProjectileCE', 'ProjectileAngleCE', 'LookupAmmosetCE', 'NotifyShotFiredCE', 'LookupProjectileCountAndSpreadCE')
ASSIGNS = re.compile(r'\bVehicleTurret\s*\.\s*(' + '|'.join(HOOKS) + r')\s*(?:[-+]|\?\?)?=(?!=)')


def strip_comments(text):
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    return re.sub(r'//[^\n]*', '', text)


def offences(text):
    return sorted({match.group(1) for match in ASSIGNS.finditer(strip_comments(text))})


class NoSharedHookSwaps(unittest.TestCase):
    def test_no_code_of_ours_assigns_a_shared_hook(self):
        problems = []
        for path in sorted((ROOT / 'src').rglob('*.cs')):
            relative = path.relative_to(ROOT).as_posix()
            if '/obj/' in relative or '/bin/' in relative:
                continue
            problems += [f'{relative}: assigns VehicleTurret.{hook}' for hook in offences(path.read_text(encoding='utf-8'))]
        self.assertEqual([], problems, 'Patch the function the hook ends in instead (see TurretAimSwayCombatExtended):\n'
                         + '\n'.join(problems))

    def test_the_check_sees_the_fault_it_was_written_for(self):
        # The shape of the sway wrapper in Framework 0.119.0.
        sample = '''
        public static void Prefix(VehicleTurret __instance)
        {
            firing = __instance;
            var current = VehicleTurret.ProjectileAngleCE;
            if (current != null && current != (System.Func<float, Vector2>)Wrapped)
            {
                combatExtended = current;
                VehicleTurret.ProjectileAngleCE = Wrapped;
            }
        }'''
        self.assertEqual(['ProjectileAngleCE'], offences(sample))

    def test_reading_or_comparing_a_hook_passes(self):
        sample = '''
        // VehicleTurret.ProjectileAngleCE = Wrapped; in a comment is not code
        if (VehicleTurret.LaunchProjectileCE == null) return;
        bool combatExtended = VehicleTurret.ProjectileAngleCE != null;
        var angle = VehicleTurret.ProjectileAngleCE(speed, range);'''
        self.assertEqual([], offences(sample))


if __name__ == '__main__':
    unittest.main()
