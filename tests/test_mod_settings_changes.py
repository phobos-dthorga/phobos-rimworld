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



class SettingLabelTests(unittest.TestCase):
    def test_the_settings_window_label_is_found_beside_the_saved_key(self):
        texts = {'PhobosFramework_SettingTopUpLevel': 'How full to fill them', 'PhobosTest_BLabel': 'Bees'}
        self.assertEqual('How full to fill them', settings.setting_label('PhobosFramework_TopUpLevel', texts))
        self.assertEqual('Bees', settings.setting_label('PhobosTest_B', texts))
        self.assertIsNone(settings.setting_label('PhobosTest_C', texts))
        self.assertIsNone(settings.setting_label('NoUnderscore', {'NoUnderscore': 'x'}))

    def test_each_class_is_given_the_mod_its_project_builds_into(self):
        found = settings.class_mods([source(A)], root=ROOT)
        self.assertEqual({}, {k: v for k, v in found.items() if k.endswith('TestSettings')})  # src/Test is no real project
        real = settings.class_mods([settings.sources_now()])
        self.assertEqual('PhobosFramework', real.get('Phobos.Framework.FrameworkSettings'))

if __name__ == '__main__':
    unittest.main()


class SettingsHistoryTests(unittest.TestCase):
    """The history of our settings is put together from each commit's changes, kept per commit; the commits are synthetic here."""

    def setUp(self):
        import tempfile
        from unittest import mock
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.read = []

        def changed_in(commit, root=None):
            self.read.append(commit)
            return [{'mod': 'PhobosTest', 'class': 'Phobos.Test.TestSettings', 'key': f'PhobosTest_{commit}', 'what': 'added',
                     'before': None, 'after': 'True'}]

        store = os.path.join(self.tmp.name, 'history.json')
        for name, value in (('history_commits', lambda root=None: [('aaa', '2026-11-01T10:00:00+11:00', 'first'),
                                                                     ('bbb', '2026-11-02T10:00:00+11:00', 'second')]),
                            ('_changed_in', changed_in),
                            ('_history_store', lambda root=None: settings.Path(store)),
                            ('sources_at', lambda revision, root=None: source(A)),
                            ('sources_now', lambda root=None: source(A, B)),
                            ('keyed_texts', lambda root=None: {'PhobosTest_SettingB': 'Bees'})):
            patcher = mock.patch.object(settings, name, value)
            patcher.start()
            self.addCleanup(patcher.stop)

    def test_each_commit_once_oldest_first_then_what_is_not_committed(self):
        found = settings.settings_history()
        self.assertEqual([c['key'] for c in found], ['PhobosTest_aaa', 'PhobosTest_bbb', 'PhobosTest_B'])
        self.assertEqual([c['commit'] for c in found], ['aaa', 'bbb', None])
        self.assertEqual(found[-1]['label'], 'Bees')
        settings.settings_history()
        self.assertEqual(self.read, ['aaa', 'bbb'], 'a commit already read comes from the cache')
