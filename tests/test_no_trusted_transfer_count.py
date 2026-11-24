"""Our code never takes the game's TryTransferToContainer answer as the number of things moved.

``ThingOwner.TryTransferToContainer`` answers with the size of the stack that arrived as a thing of its own, so a stack that merges into
one already in the other container counts as nought, or as only its unmerged rest. The work store's loop that took the answer as the
amount moved kept moving: the owner's HEMTT (25 Nov 2026) laid out 975 steel for fittings needing about a hundred. D'thorga Framework's
``ThingMoves.Transfer`` counts what left the first container instead; this test reads our C# as text and refuses the answer used anywhere
else. A call whose answer is ignored, or whose bool overload is tested, is fine.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
# The answer assigned, added up or compared: "x = a.TryTransferToContainer(", "x += ...", "return ...", "> 0".
USED = re.compile(r'(=|\+=|-=|\breturn)\s*[\w.?\[\]()]*\bTryTransferToContainer\s*\(|\bTryTransferToContainer\s*\([^;]*\)\s*(==|!=|>|<)')
HOME = 'src/PhobosFramework/Items/ThingMoves.cs'


def strip_comments(text):
    text = re.sub(r'/\*.*?\*/', '', text, flags=re.S)
    return re.sub(r'//[^\n]*', '', text)


def offences(text):
    code = strip_comments(text)
    return [code.count('\n', 0, match.start()) + 1 for match in USED.finditer(code)]


class NoTrustedTransferCount(unittest.TestCase):
    def test_no_code_of_ours_counts_by_the_games_answer(self):
        problems = []
        for path in sorted((ROOT / 'src').rglob('*.cs')):
            relative = path.relative_to(ROOT).as_posix()
            if '/obj/' in relative or '/bin/' in relative or relative == HOME:
                continue
            problems += [f'{relative}:{line}' for line in offences(path.read_text(encoding='utf-8'))]
        self.assertEqual([], problems, 'Count with Phobos.Framework.Items.ThingMoves.Transfer instead:\n' + '\n'.join(problems))

    def test_the_check_sees_the_fault_it_was_written_for(self):
        sample = 'int done = cargo.TryTransferToContainer(thing, held, System.Math.Min(left, thing.stackCount));'
        self.assertEqual([1], offences(sample))
        self.assertEqual([1], offences('moved += cargo.TryTransferToContainer(thing, magazine, 3);'))

    def test_an_ignored_answer_and_a_comment_are_not_offences(self):
        sample = '// never x = a.TryTransferToContainer(b, c, 1)\npawn.carryTracker.innerContainer.TryTransferToContainer(carried, store, n);'
        self.assertEqual([], offences(sample))


if __name__ == '__main__':
    unittest.main()
