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
                for field in ('page', 'title', 'credit', 'file_url'):
                    self.assertTrue(source.get(field), f'{path}: {key} has no {field}')
                self.assertRegex(source.get('sha256', ''), r'^[0-9a-f]{64}$', f'{path}: {key} needs its full SHA-256')

    def test_every_cut_names_a_source_and_a_length(self):
        for path in MANIFESTS:
            manifest = json.loads(open(path, encoding='utf-8').read())
            for item in manifest['cuts']:
                self.assertIn(item['source'], manifest['sources'], path)
                self.assertGreater(float(item['seconds']), 0, path)
                self.assertGreaterEqual(float(item['start']), 0, path)


if __name__ == '__main__':
    unittest.main()
