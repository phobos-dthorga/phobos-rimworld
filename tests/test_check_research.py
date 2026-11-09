"""Offline checks for scripts/check-research.py: a clean tree passes, and each kind of mistake is caught.

Every mod below is synthetic; nothing reads or writes the RimWorld installation.
"""
import importlib.util
import os
import tempfile
import unittest
from pathlib import Path

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
spec = importlib.util.spec_from_file_location('check_research', os.path.join(ROOT, 'scripts/check-research.py'))
research = importlib.util.module_from_spec(spec)
spec.loader.exec_module(research)

GAME = {'Machining', 'Electricity'}
TAB = '<ResearchTabDef><defName>TestTab</defName></ResearchTabDef>'
REPLACES = '<modExtensions><li Class="Phobos.Framework.Research.ResearchReplacesExtension"><replaces><li>Machining</li></replaces></li></modExtensions>'
MARKER = '<modExtensions><li Class="Phobos.VehicleSystems.PhobosEnforceUpgradeResearch" /></modExtensions>'


def project(name='PhobosResearch_One', x=0, y=0, needs='<li>Machining</li>', tab='TestTab', replaces=REPLACES):
    return (f'<ResearchProjectDef><defName>{name}</defName><tab>{tab}</tab><researchViewX>{x}</researchViewX>'
            f'<researchViewY>{y}</researchViewY><prerequisites>{needs}</prerequisites>{replaces}</ResearchProjectDef>')


def tree(marker=MARKER, research='Machining'):
    return (f'<Vehicles.UpgradeTreeDef><defName>Test_Tree</defName>{marker}<nodes><li><key>Node</key>'
            f'<researchPrerequisites><li>{research}</li></researchPrerequisites></li></nodes></Vehicles.UpgradeTreeDef>')


class CheckResearchTests(unittest.TestCase):
    def errors(self, defs, extra=None):
        with tempfile.TemporaryDirectory() as folder:
            mods = Path(folder)
            path = mods / 'TestMod' / 'Defs' / 'Test.xml'
            path.parent.mkdir(parents=True)
            path.write_text(f'<Defs>{defs}</Defs>', encoding='utf-8')
            for relative, content in (extra or {}).items():
                other = mods / relative
                other.parent.mkdir(parents=True, exist_ok=True)
                other.write_text(content, encoding='utf-8')
            return research.check(mods, GAME)

    def assert_caught(self, defs, phrase, extra=None):
        found = self.errors(defs, extra)
        self.assertTrue(any(phrase in error for error in found), found)

    def test_a_clean_tree_passes(self):
        self.assertEqual([], self.errors(TAB + project() + tree()))

    def test_a_soft_requirement_is_allowed_beside_a_hard_one(self):
        needs = '<li>Machining</li><li MayRequire="some.mod">TheirResearch</li>'
        self.assertEqual([], self.errors(TAB + project(needs=needs)))

    def test_a_tab_inherited_from_a_parent_counts(self):
        parent = '<ResearchProjectDef Abstract="True" Name="Base"><tab>TestTab</tab></ResearchProjectDef>'
        child = project().replace('<ResearchProjectDef>', '<ResearchProjectDef ParentName="Base">').replace('<tab>TestTab</tab>', '')
        self.assertEqual([], self.errors(TAB + parent + child))

    def test_the_prefix_is_required_on_our_tab(self):
        self.assert_caught(TAB + project(name='Other_One'), 'must start')

    def test_only_soft_requirements_are_refused(self):
        self.assert_caught(TAB + project(needs='<li MayRequire="some.mod">Theirs</li>'), 'needs a hard requirement')

    def test_a_loop_is_refused(self):
        one = project('PhobosResearch_One', 0, 0, '<li>PhobosResearch_Two</li>')
        two = project('PhobosResearch_Two', 1, 0, '<li>PhobosResearch_One</li>')
        self.assert_caught(TAB + one + two, 'loop')

    def test_an_unknown_hard_requirement_is_refused(self):
        self.assert_caught(TAB + project(needs='<li>Nonsense</li>'), 'neither ours')

    def test_an_unknown_tab_is_refused(self):
        self.assert_caught(TAB + project(tab='Elsewhere', name='PhobosResearch_One'), 'not one we define')

    def test_projects_too_close_are_refused(self):
        self.assert_caught(TAB + project('PhobosResearch_One', 0, 0) + project('PhobosResearch_Two', 0.2, 0.1), 'too close')

    def test_a_position_off_the_screen_is_refused(self):
        self.assert_caught(TAB + project(y=7), 'off the screen')

    def test_a_project_must_list_what_it_replaces(self):
        self.assert_caught(TAB + project(replaces=''), 'lists nothing it replaces')

    def test_an_upgrade_with_unknown_research_is_refused(self):
        self.assert_caught(TAB + project() + tree(research='Nonsense'), 'upgrade Node')

    def test_a_tree_without_the_marker_is_refused(self):
        self.assert_caught(TAB + project() + tree(marker=''), 'lacks')

    def test_a_misspelt_project_name_in_a_gate_is_refused(self):
        thing = '<ThingDef><researchPrerequisites><li>PhobosResearch_Typo</li></researchPrerequisites></ThingDef>'
        self.assert_caught(TAB + project() + thing, 'PhobosResearch_Typo is not a project')

    def test_a_requirement_in_a_tie_in_folder_is_conditional(self):
        tie_in = f'<Defs>{project("PhobosResearch_Tie", 2, 0, "<li>ModOnlyResearch</li>")}</Defs>'
        self.assertEqual([], self.errors(TAB + project(), {'TestMod/Mods/Other/Defs/Tie.xml': tie_in}))

    def test_the_shipped_tree_passes_without_a_game(self):
        self.assertEqual([], research.check(Path(ROOT) / 'mods', None))


if __name__ == '__main__':
    unittest.main()
