"""A recipe worker's AvailableOnNow must not ask AvailableReport.

In RimWorld 1.7 the game's ``RecipeWorker.AvailableReport`` returns ``AvailableOnNow(thing, part)``. An ``AvailableOnNow`` of ours
that asks ``AvailableReport``, whose answer reaches the base class, calls itself until the stack runs out and the game closes with
nothing in its log: Framework's ``RecipeWorker_PhobosNeedsNode`` did, from 0.121.0, and once 0.273.0 let its recipe run at home
stoves too, a cook checking bills mid-raid closed the owner's game twice on 23 Nov 2026 (fixed in 0.273.2). This test reads our C#
as text and refuses an ``AvailableOnNow`` override whose body calls ``AvailableReport``. Answer both from one private check instead.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OVERRIDE = re.compile(r'override\s+bool\s+AvailableOnNow\s*\([^)]*\)\s*(=>|\{)')
ASKS = re.compile(r'\bAvailableReport\s*\(')


def strip_comments(text):
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    return re.sub(r'//[^\n]*', '', text)


def body_after(text, match):
    start = match.end() - 1
    if match.group(1) == '=>':
        end = text.find(';', start)
        return text[start:end if end >= 0 else len(text)]
    depth = 0
    for i in range(start, len(text)):
        if text[i] == '{':
            depth += 1
        elif text[i] == '}':
            depth -= 1
            if depth == 0:
                return text[start:i + 1]
    return text[start:]


def offences(text):
    text = strip_comments(text)
    return ['AvailableOnNow asks AvailableReport' for match in OVERRIDE.finditer(text) if ASKS.search(body_after(text, match))]


class NoRecipeAvailabilityLoop(unittest.TestCase):
    def test_no_available_on_now_asks_available_report(self):
        problems = []
        for path in sorted((ROOT / 'src').rglob('*.cs')):
            relative = path.relative_to(ROOT).as_posix()
            if '/obj/' in relative or '/bin/' in relative:
                continue
            problems += [f'{relative}: {line}' for line in offences(path.read_text(encoding='utf-8'))]
        self.assertEqual([], problems, 'Answer both from one check (see RecipeWorker_PhobosNeedsNode.Check):\n' + '\n'.join(problems))

    def test_the_check_sees_the_fault_it_was_written_for(self):
        # The shape of RecipeWorker_PhobosNeedsNode from Framework 0.121.0 to 0.273.1.
        sample = '''
        public class W : RecipeWorker
        {
            public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord? part = null)
            {
                return base.AvailableReport(thing, part);
            }

            public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null) => AvailableReport(thing, part).Accepted;
        }'''
        self.assertEqual(['AvailableOnNow asks AvailableReport'], offences(sample))

    def test_a_block_body_is_read_too(self):
        sample = '''
            public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
            {
                var report = AvailableReport(thing, part);
                return report.Accepted;
            }'''
        self.assertEqual(['AvailableOnNow asks AvailableReport'], offences(sample))

    def test_one_shared_check_passes(self):
        sample = '''
        public class W : RecipeWorker
        {
            public override AcceptanceReport AvailableReport(Thing thing, BodyPartRecord? part = null) => Check(thing);
            public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null) => Check(thing).Accepted;
            AcceptanceReport Check(Thing thing) => AcceptanceReport.WasAccepted;
        }'''
        self.assertEqual([], offences(sample))

    def test_asking_the_base_on_now_passes(self):
        # Medical's fluids bill: the base AvailableOnNow answers without asking AvailableReport.
        sample = '''
            public override bool AvailableOnNow(Thing thing, BodyPartRecord? part = null) =>
                thing is Pawn pawn && FluidsService.NeedsFluids(pawn) && base.AvailableOnNow(thing, part);'''
        self.assertEqual([], offences(sample))


if __name__ == '__main__':
    unittest.main()
