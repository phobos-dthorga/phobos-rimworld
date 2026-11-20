"""The rules a session reads are where the root AGENTS.md says they are.

On 21 Nov 2026 the owner had the root file slimmed ("keep a slimmed-down AGENTS at the root"): what every session needs stays in it,
and the rest lives beside its work, in rule files a session's tools load only when it works in their folder. This test holds the
root's promises: every rule file it names exists and has a CLAUDE.md beside it that loads it, no rule file sits inside a mod's own
folder (deploy ships everything there), and every script in its table of which tool answers which question exists.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ROOT_RULES = ROOT / "AGENTS.md"
LINK = re.compile(r"\]\(([\w./-]*AGENTS\.md)\)")
SCRIPT = re.compile(r"`python (scripts/[\w./-]+\.py)")


class RuleFiles(unittest.TestCase):
    def setUp(self):
        self.text = ROOT_RULES.read_text(encoding="utf-8")
        self.named = sorted(set(LINK.findall(self.text)))

    def test_the_root_names_its_rule_files(self):
        for wanted in ("scripts/AGENTS.md", "src/AGENTS.md", "mods/AGENTS.md", "docs/AGENTS.md"):
            self.assertIn(wanted, self.named)

    def test_every_rule_file_named_exists_and_is_loaded_beside_it(self):
        for name in self.named:
            rules = ROOT / name
            self.assertTrue(rules.is_file(), f"the root names {name}, which is missing")
            loader = rules.with_name("CLAUDE.md")
            self.assertTrue(loader.is_file(), f"{name} has no CLAUDE.md beside it to load it")
            self.assertIn("@AGENTS.md", loader.read_text(encoding="utf-8"), f"{loader.relative_to(ROOT)} does not load {name}")

    def test_tests_load_the_scripts_rules(self):
        loader = ROOT / "tests" / "CLAUDE.md"
        self.assertIn("@../scripts/AGENTS.md", loader.read_text(encoding="utf-8"))

    def test_no_rule_file_is_shipped_inside_a_mod(self):
        shipped = [p.relative_to(ROOT).as_posix() for p in (ROOT / "mods").glob("*/**/*") if p.name in ("AGENTS.md", "CLAUDE.md")]
        self.assertEqual(shipped, [], "everything in mods/<Mod>/ is installed and published; put a mod's rules in mods/AGENTS.md")

    def test_every_tool_in_the_root_table_exists(self):
        tools = sorted(set(SCRIPT.findall(self.text)))
        self.assertGreater(len(tools), 15)
        for tool in tools:
            self.assertTrue((ROOT / tool).is_file(), f"the root's table names {tool}, which is missing")


if __name__ == "__main__":
    unittest.main()
