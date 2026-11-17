"""Our code never asks which assembly is running: it names a type of its own and takes that type's assembly.

On RimWorld 1.7 with Harmony 3.0.3.0 (17 Nov 2026), ``Assembly.GetExecutingAssembly()`` inside a constructor that any mod has patched
returns Harmony's own assembly (pardeike/HarmonyRimWorld issue 52). Loading Progress patches every mod's constructor to time it, so a
mod that applied its patches with ``PatchAll(Assembly.GetExecutingAssembly())`` from its constructor silently applied none: in the
owner's game that day, 32 mods, Combat Extended and Simple Sidearms among them. Our missile patches read their assembly the same
way. ``typeof(OurType).Assembly`` cannot be answered wrongly, so this test reads our C# as text and refuses the call anywhere.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CALL = re.compile(r'\bAssembly\s*\.\s*Get(Executing|Calling|Entry)Assembly\s*\(')


def strip_comments(text):
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    return re.sub(r'//[^\n]*', '', text)


def offences(text):
    return sorted({'Get' + match.group(1) + 'Assembly' for match in CALL.finditer(strip_comments(text))})


class NoExecutingAssembly(unittest.TestCase):
    def test_no_code_of_ours_asks_which_assembly_is_running(self):
        problems = []
        for path in sorted((ROOT / 'src').rglob('*.cs')):
            relative = path.relative_to(ROOT).as_posix()
            if '/obj/' in relative or '/bin/' in relative:
                continue
            problems += [f'{relative}: Assembly.{call}()' for call in offences(path.read_text(encoding='utf-8'))]
        self.assertEqual([], problems, 'Use typeof(a type of ours).Assembly instead:\n' + '\n'.join(problems))

    def test_the_check_sees_the_fault_it_was_written_for(self):
        sample = 'HarmonyPatchApplier.ApplyEach(harmony, Assembly.GetExecutingAssembly(), LogPrefix);'
        self.assertEqual(['GetExecutingAssembly'], offences(sample))

    def test_a_comment_naming_the_call_is_not_an_offence(self):
        sample = '// By type, never Assembly.GetExecutingAssembly()\nApplyEach(harmony, typeof(MissilePatchesMod).Assembly, LogPrefix);'
        self.assertEqual([], offences(sample))


if __name__ == '__main__':
    unittest.main()
