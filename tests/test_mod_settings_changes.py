"""Offline checks for `mod-settings.py changes`: settings added, removed or given a new default between two versions of our source.

Every settings class below is synthetic; nothing reads the game's Config folder.
"""
import importlib.util
import io
import os
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
spec = importlib.util.spec_from_file_location('mod_settings', os.path.join(ROOT, 'scripts', 'mod-settings.py'))
settings = importlib.util.module_from_spec(spec)
spec.loader.exec_module(settings)


def source(*looks):
    body = '\n'.join(looks)
    return {'src/Test/TestSettings.cs': ('namespace Phobos.Test;\npublic class TestSettings : ModSettings\n{\n    public bool a = true;\n'
                                         '    public bool b;\n    public int c = 3;\n'
                                         f'    public override void ExposeData()\n    {{\n{body}\n    }}\n}}\n')}


A = '        Scribe_Values.Look(ref a, "PhobosTest_A", true);'
B = '        Scribe_Values.Look(ref b, "PhobosTest_B", false);'
C3 = '        Scribe_Values.Look(ref c, "PhobosTest_C", 3);'
C5 = '        Scribe_Values.Look(ref c, "PhobosTest_C", 5);'


class SettingChangesTests(unittest.TestCase):
    def found(self, old, new):
        return settings.changes(settings.defaults_document(sources=old), settings.defaults_document(sources=new))

    def test_nothing_changed_reports_nothing(self):
        self.assertEqual([], self.found(source(A, C3), source(A, C3)))

    def test_an_added_setting_is_named_with_its_default(self):
        self.assertEqual([('Phobos.Test.TestSettings', 'PhobosTest_B', 'added', None, 'False')], self.found(source(A), source(A, B)))

    def test_a_removed_setting_is_named(self):
        self.assertEqual([('Phobos.Test.TestSettings', 'PhobosTest_B', 'removed', 'False', None)], self.found(source(A, B), source(A)))

    def test_a_new_default_is_named_with_both_values(self):
        self.assertEqual([('Phobos.Test.TestSettings', 'PhobosTest_C', 'default', '3', '5')], self.found(source(C3), source(C5)))

    def test_a_new_settings_class_counts_every_key(self):
        self.assertEqual(2, len(self.found({}, source(A, B))))

    def test_the_report_reads_the_repository_history(self):
        out = io.StringIO()
        self.assertEqual(0, settings.report_changes(['--since', 'HEAD'], out))
        self.assertIn('HEAD', out.getvalue())

    def test_a_bad_revision_is_refused(self):
        self.assertEqual(2, settings.report_changes(['--since', 'no-such-revision-anywhere'], io.StringIO()))


if __name__ == '__main__':
    unittest.main()
