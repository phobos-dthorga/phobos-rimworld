"""A comp must not put a thing on the map from inside its parent's own spawning.

The game calls ``PostSpawnSetup`` from ``ThingWithComps.SpawnSetup``, which a pawn runs *before* it has its movement parts
(``Pawn.SpawnSetup`` adds them afterwards). A pawn spawned next to a pawn in that state makes the game's standing-room check throw,
which leaves both half on the map and breaks the map's drawing: the sling's set-down did exactly this on 12 Nov 2026 (Framework
0.100.0; fixed in 0.101.1 by waiting for the next tick). This test reads our C# as text and refuses a spawn written in a
``PostSpawnSetup`` body, or in a method of the same file that the body calls directly. It cannot follow calls any further.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SPAWNS = re.compile(r'\b(GenSpawn\.Spawn|GenPlace\.TryPlaceThing|GenDrop\.TryDropSpawn)\s*\(')
METHOD = re.compile(
    r'^[ \t]*(?:(?:public|internal|private|protected|static|override|virtual|sealed|async)\s+)*[\w<>\[\]?,. ]+?\s+(\w+)\s*\([^;{}]*\)\s*'
    r'(?:where[^{;]*)?\{', re.M)
CALL = re.compile(r'(?<![.\w])(\w+)\s*\(')
NOT_METHODS = {'if', 'for', 'foreach', 'while', 'switch', 'using', 'lock', 'catch'}

# Files allowed to spawn from PostSpawnSetup, each with its reason. The fault needs a pawn on both sides, so a building that lays
# another building while it is placed may be listed here once it has been read and found safe.
ALLOWED = {}


def body_at(text, open_brace):
    depth = 0
    for i in range(open_brace, len(text)):
        if text[i] == '{':
            depth += 1
        elif text[i] == '}':
            depth -= 1
            if depth == 0:
                return text[open_brace:i + 1]
    return text[open_brace:]


def strip_comments(text):
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    return re.sub(r'//[^\n]*', '', text)


def methods(text):
    found = {}
    for match in METHOD.finditer(text):
        name = match.group(1)
        if name in NOT_METHODS:
            continue
        found.setdefault(name, []).append(body_at(text, match.end() - 1))
    return found


def offences(text):
    found = methods(strip_comments(text))
    out = []
    for body in found.get('PostSpawnSetup', []):
        if SPAWNS.search(body):
            out.append('PostSpawnSetup spawns a thing itself')
        for called in sorted(set(CALL.findall(body)) - {'PostSpawnSetup'}):
            if any(SPAWNS.search(b) for b in found.get(called, [])):
                out.append(f'PostSpawnSetup calls {called}, which spawns a thing')
    return out


class NoSpawnInSpawnSetup(unittest.TestCase):
    def test_no_comp_spawns_a_thing_from_post_spawn_setup(self):
        problems = []
        for path in sorted((ROOT / 'src').rglob('*.cs')):
            relative = path.relative_to(ROOT).as_posix()
            if '/obj/' in relative or '/bin/' in relative or relative in ALLOWED:
                continue
            problems += [f'{relative}: {line}' for line in offences(path.read_text(encoding='utf-8'))]
        self.assertEqual([], problems, 'Spawn at the next tick instead (see CompPhobosSling.PostSpawnSetup):\n' + '\n'.join(problems))

    def test_the_check_sees_the_fault_it_was_written_for(self):
        # The shape of the sling's set-down in Framework 0.100.0.
        sample = '''
        public sealed class C : VehicleComp
        {
            public override void PostSpawnSetup(bool respawningAfterLoad)
            {
                base.PostSpawnSetup(respawningAfterLoad);
                if (!respawningAfterLoad && Carried != null) SetDown();
            }

            internal void SetDown()
            {
                GenSpawn.Spawn(load, cell, map, Vehicle.Rotation);
            }
        }'''
        self.assertEqual(['PostSpawnSetup calls SetDown, which spawns a thing'], offences(sample))

    def test_a_spawn_left_for_the_next_tick_passes(self):
        sample = '''
        public sealed class C : VehicleComp
        {
            public override void PostSpawnSetup(bool respawningAfterLoad)
            {
                base.PostSpawnSetup(respawningAfterLoad);
                if (!respawningAfterLoad) landed = true;
                if (landed) Vehicle.RequestTickStart(this);
            }

            public override void CompTick()
            {
                GenSpawn.Spawn(load, cell, map, Vehicle.Rotation);
            }
        }'''
        self.assertEqual([], offences(sample))


if __name__ == '__main__':
    unittest.main()
