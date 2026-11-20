"""Offline checks of scripts/art/recordings.py and the recordings manifests. No network, no ffmpeg, nothing written."""
import glob
import json
import os
import sys
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))
import recordings  # noqa: E402

MANIFESTS = sorted(glob.glob(os.path.join(ROOT, 'assets', '*', 'recordings', '*.json')))


class PageTests(unittest.TestCase):
    def test_a_page_marked_public_domain_passes(self):
        self.assertTrue(recordings.page_is_public_domain('<div class="usage">PUBLIC DOMAIN &nbsp;</div>'))

    def test_a_page_without_the_mark_is_refused(self):
        self.assertFalse(recordings.page_is_public_domain('<div class="usage">All rights reserved</div>'))


class CutTests(unittest.TestCase):
    MANIFEST = {'set': 'test', 'licence': 'Public domain',
                'sources': {'1': {'page': 'https://example.invalid/1', 'title': 'A test', 'credit': 'Someone', 'sha256': 'ab'}}}

    def test_names_are_stable_and_sort_by_start(self):
        early = recordings.cut_name({'role': 'idle', 'source': '1', 'start': 6})
        late = recordings.cut_name({'role': 'idle', 'source': '1', 'start': 68.5})
        self.assertEqual(early, 'idle-1-00060')
        self.assertEqual(late, 'idle-1-00685')
        self.assertLess(early, late)

    def test_a_record_carries_the_loop_and_the_credit(self):
        record = recordings.cut_record(self.MANIFEST, {'role': 'idle', 'source': '1', 'start': 2, 'seconds': 8, 'loop': True})
        self.assertTrue(record['loop'])
        self.assertEqual(record['credit'], 'Someone')
        self.assertEqual(record['source_page'], 'https://example.invalid/1')
        self.assertEqual(record['licence'], 'Public domain')


class LicenceTests(unittest.TestCase):
    def test_a_source_may_carry_its_own_licence(self):
        manifest = {'set': 'test', 'licence': 'CC0 or CC BY, per source',
                    'sources': {'1': {'page': 'p', 'title': 't', 'credit': 'Someone', 'licence': 'CC BY 4.0'}}}
        record = recordings.cut_record(manifest, {'role': 'idle', 'source': '1', 'start': 0, 'seconds': 4})
        self.assertEqual(record['licence'], 'CC BY 4.0')

    def test_without_one_the_sets_licence_holds(self):
        record = recordings.cut_record(CutTests.MANIFEST, {'role': 'idle', 'source': '1', 'start': 0, 'seconds': 4})
        self.assertEqual(record['licence'], 'Public domain')


class RepairTests(unittest.TestCase):
    def test_a_repaired_cut_says_so_in_its_name_and_record(self):
        item = {'role': 'start', 'source': '1', 'start': 0, 'seconds': 7, 'filter': 'adeclip'}
        self.assertEqual(recordings.cut_name(item), 'start-1-00000-repaired')
        self.assertEqual(recordings.cut_record(CutTests.MANIFEST, item)['filter'], 'adeclip')

    def test_an_untouched_cut_has_no_filter(self):
        item = {'role': 'start', 'source': '1', 'start': 0, 'seconds': 7}
        self.assertNotIn('filter', recordings.cut_record(CutTests.MANIFEST, item))


class BuildTests(unittest.TestCase):
    def test_one_layer_is_lowered_and_named_as_the_output(self):
        graph = recordings.build_graph([{'source': 'a', 'start': 0, 'seconds': 9, 'lower': 0.72}], 12, [48000])
        self.assertIn('asetrate=34560', graph)
        self.assertTrue(graph.endswith('[o]'))
        self.assertNotIn('amix', graph)

    def test_two_layers_are_mixed_without_normalising(self):
        layers = [{'source': 'a', 'start': 0, 'seconds': 12, 'lower': 0.7}, {'source': 'b', 'start': 5, 'seconds': 12, 'lower': 0.6, 'gain_db': -14}]
        graph = recordings.build_graph(layers, 15, [44100, 96000])
        self.assertIn('asetrate=30869', graph)  # 44100 x 0.7, truncated as the built files were
        self.assertIn('asetrate=57600', graph)
        self.assertIn('volume=-14.0dB', graph)
        self.assertIn('amix=inputs=2:normalize=0', graph)

    def test_a_build_credits_every_source_it_uses(self):
        manifest = {'licence': 'per source', 'sources': {
            'a': {'page': 'pa', 'title': 'A', 'credit': 'Ann', 'licence': 'CC0 1.0'},
            'b': {'page': 'pb', 'title': 'B', 'credit': 'Bob', 'licence': 'CC BY 3.0'}}}
        record = recordings.build_record(manifest, {'name': 'x', 'role': 'stop', 'seconds': 10, 'layers': [
            {'source': 'a', 'start': 0, 'seconds': 9, 'lower': 0.7}, {'source': 'b', 'start': 0, 'seconds': 9, 'lower': 0.7}]})
        self.assertEqual(record['kind'], 'recording-built')
        self.assertIn('Ann', record['credit'])
        self.assertIn('Bob', record['credit'])
        self.assertEqual(record['licence'], 'CC BY 3.0, CC0 1.0')


class CombRecordTests(unittest.TestCase):
    MANIFEST = dict(CutTests.MANIFEST, comb={'hz': 20.55})

    def test_a_cut_carries_the_sets_comb(self):
        record = recordings.cut_record(self.MANIFEST, {'role': 'idle', 'source': '1', 'start': 0, 'seconds': 8})
        self.assertEqual(record['comb'], {'hz': 20.55})

    def test_a_cut_can_leave_the_comb_out(self):
        record = recordings.cut_record(self.MANIFEST, {'role': 'shutdown', 'source': '1', 'start': 0, 'seconds': 8, 'comb': False})
        self.assertNotIn('comb', record)


class ManifestTests(unittest.TestCase):
    def test_there_is_a_manifest(self):
        self.assertTrue(MANIFESTS)

    def test_every_source_has_its_page_credit_and_full_hash(self):
        for path in MANIFESTS:
            manifest = json.loads(open(path, encoding='utf-8').read())
            self.assertTrue(manifest.get('licence'), path)
            for key, source in manifest['sources'].items():
                for field in ('page', 'title', 'credit'):
                    self.assertTrue(source.get(field), f'{path}: {key} has no {field}')
                # A source is fetched from its file_url or was downloaded by hand into the set's folder (local).
                self.assertTrue(source.get('file_url') or source.get('local'), f'{path}: {key} has neither file_url nor local')
                self.assertRegex(source.get('sha256', ''), r'^[0-9a-f]{64}$', f'{path}: {key} needs its full SHA-256')

    def test_every_cut_names_a_source_and_a_length(self):
        for path in MANIFESTS:
            manifest = json.loads(open(path, encoding='utf-8').read())
            for item in manifest.get('builds', []):
                for layer in item['layers']:
                    self.assertIn(layer['source'], manifest['sources'], path)
            for item in manifest['cuts']:
                self.assertIn(item['source'], manifest['sources'], path)
                self.assertGreater(float(item['seconds']), 0, path)
                self.assertGreaterEqual(float(item['start']), 0, path)


if __name__ == '__main__':
    unittest.main()
