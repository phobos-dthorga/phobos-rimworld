"""Offline checks of the unattended art safeguards and the sorting rules. No server, no GPU, no game, no Pillow."""
import datetime as dt
import importlib.util
import os
import sys
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))
import lora_experiment  # noqa: E402
import safeguards  # noqa: E402
import sort_rules  # noqa: E402

NOON = dt.datetime(2026, 11, 5, 12, 0)
LATER = dt.datetime(2026, 11, 5, 18, 0)
PLENTY = {'C:\\': 150.0, 'D:\\': 29.0}


class StopTimeTests(unittest.TestCase):
    def test_a_time_later_today_is_today(self):
        self.assertEqual(safeguards.parse_stop_at('18:00', NOON), LATER)

    def test_a_time_already_past_means_tomorrow(self):
        self.assertEqual(safeguards.parse_stop_at('07:00', NOON), dt.datetime(2026, 11, 6, 7, 0))

    def test_exactly_now_means_tomorrow_not_an_instant_stop(self):
        self.assertEqual(safeguards.parse_stop_at('12:00', NOON), dt.datetime(2026, 11, 6, 12, 0))

    def test_a_full_date_is_taken_as_written(self):
        self.assertEqual(safeguards.parse_stop_at('2026-11-07T06:30', NOON), dt.datetime(2026, 11, 7, 6, 30))

    def test_a_full_date_in_the_past_is_refused(self):
        with self.assertRaises(ValueError):
            safeguards.parse_stop_at('2026-11-01T06:30', NOON)


class ProcessTests(unittest.TestCase):
    LISTING = '"System Idle Process","0","Services","0","8 K"\n"RimWorld.exe","60744","Console","1","12,883,600 K"\n'

    def test_tasklist_names_are_lower_case(self):
        self.assertEqual(safeguards.parse_tasklist(self.LISTING), {'system idle process', 'rimworld.exe'})

    def test_both_game_versions_are_watched(self):
        self.assertEqual(safeguards.game_running({'rimworld.exe'}), 'rimworld.exe')
        self.assertEqual(safeguards.game_running({'rimworldwin64.exe'}), 'rimworldwin64.exe')
        self.assertIsNone(safeguards.game_running({'explorer.exe'}))


class StopReasonTests(unittest.TestCase):
    def test_nothing_wrong_means_no_reason(self):
        self.assertIsNone(safeguards.stop_reason(NOON, LATER, {'explorer.exe'}, PLENTY))

    def test_the_stop_time_ends_the_run(self):
        self.assertIn('stop time', safeguards.stop_reason(LATER, LATER, {'explorer.exe'}, PLENTY))

    def test_the_game_ends_the_run(self):
        self.assertIn('RimWorld', safeguards.stop_reason(NOON, LATER, {'rimworld.exe'}, PLENTY, names=('RimWorld.exe',)))

    def test_the_game_is_named_first_when_both_apply(self):
        self.assertIn('rimworld', safeguards.stop_reason(LATER, LATER, {'rimworld.exe'}, {'C:\\': 1.0}).lower())

    def test_an_unreadable_process_list_stops_rather_than_assumes(self):
        self.assertIn('process list', safeguards.stop_reason(NOON, LATER, None, PLENTY))

    def test_a_drive_under_the_floor_ends_the_run_and_is_named(self):
        reason = safeguards.stop_reason(NOON, LATER, set(), {'C:\\': 150.0, 'D:\\': 4.9}, min_free_gb=5.0)
        self.assertIn('D:', reason)

    def test_a_drive_at_the_floor_is_fine(self):
        self.assertIsNone(safeguards.stop_reason(NOON, LATER, set(), {'D:\\': 5.0}, min_free_gb=5.0))


def measure(name, border=1.0, sharp=400.0, content=0.5, value=0):
    return {'file': name, 'border_white': border, 'sharpness': sharp, 'content': content, 'hash': value}


class SortingTests(unittest.TestCase):
    def test_a_perfect_candidate_scores_one(self):
        self.assertEqual(sort_rules.score(measure('a.png')), 1.0)

    def test_a_busy_background_costs_most(self):
        self.assertLess(sort_rules.score(measure('a.png', border=0.0)), sort_rules.score(measure('b.png', sharp=0.0)))

    def test_a_subject_filling_the_frame_loses_framing(self):
        self.assertEqual(sort_rules.score(measure('a.png', content=0.99)), 0.8)

    def test_sharpness_beyond_full_marks_is_capped(self):
        self.assertEqual(sort_rules.score(measure('a.png', sharp=9000.0)), 1.0)

    def test_rank_is_best_first_and_stable_on_ties(self):
        ranked = sort_rules.rank([measure('c.png', border=0.0, value=0xFFFF), measure('b.png', value=0xFF0000), measure('a.png', value=0xFF000000)])
        self.assertEqual([m['file'] for m in ranked], ['a.png', 'b.png', 'c.png'])

    def test_a_repeat_is_marked_but_kept(self):
        ranked = sort_rules.rank([measure('a.png', value=0b1111), measure('b.png', border=0.5, value=0b1110), measure('c.png', value=(1 << 40) - 1)])
        files = {m['file']: m for m in ranked}
        self.assertEqual(files['b.png']['duplicate_of'], 'a.png')
        self.assertNotIn('duplicate_of', files['c.png'])
        self.assertEqual(len(ranked), 3)

    def test_hamming_counts_differing_bits(self):
        self.assertEqual(sort_rules.hamming(0b1010, 0b0110), 2)

    def test_either_signal_flags_a_near_copy(self):
        self.assertTrue(sort_rules.is_near_copy(12, 0.1))
        self.assertTrue(sort_rules.is_near_copy(40, 0.60))
        self.assertFalse(sort_rules.is_near_copy(13, 0.59))


class LoraExperimentTests(unittest.TestCase):
    def test_a_square_sprite_fits_the_long_side(self):
        self.assertEqual(lora_experiment.fit_long_side(64, 64), (1024, 1024))

    def test_a_tall_sprite_keeps_its_shape_on_the_step(self):
        self.assertEqual(lora_experiment.fit_long_side(128, 192), (688, 1024))

    def test_a_thin_sprite_never_collapses_to_nothing(self):
        self.assertEqual(lora_experiment.fit_long_side(1, 1000), (16, 1024))

    def test_captions_describe_the_subject_after_the_trigger(self):
        self.assertEqual(lora_experiment.caption_for('HospitalBed_south', 'phobosvanilla'), 'phobosvanilla, hospital bed')
        self.assertEqual(lora_experiment.caption_for('MedicineUltratech_a', 'phobosvanilla'), 'phobosvanilla, medicine ultratech')

    def test_the_config_trains_klein_base_with_a_12_gb_card_in_mind(self):
        process = lora_experiment.training_config('v', 'C:/d', 'C:/o', 'x')['config']['process'][0]
        self.assertEqual(process['model']['arch'], 'flux2_klein_4b')
        self.assertTrue(process['model']['low_vram'])
        self.assertTrue(process['model']['quantize'])
        self.assertTrue(process['train']['cache_text_embeddings'])
        self.assertFalse(process['save']['push_to_hub'])
        self.assertEqual(process['trigger_word'], 'x')

    def test_the_comparison_runs_three_routes_on_the_same_seeds(self):
        requests = [{'name': 'tank', 'engine': 'klein-base', 'prompt': 'A tank. Style.', 'subject': 'a tank', 'references': ['r.png'], 'width': 1024, 'height': 1024},
                    {'name': 'hero', 'engine': 'zimage', 'prompt': 'A ward.', 'width': 1280, 'height': 720}]
        routes = lora_experiment.route_requests(requests, 'x', 'l.safetensors')
        by_name = {r['name']: r for r in routes}
        self.assertEqual(set(by_name), {'tank-references', 'tank-lora', 'tank-both'})
        self.assertEqual({tuple(r['seeds']) for r in routes}, {lora_experiment.ROUTE_SEEDS})
        self.assertNotIn('lora', by_name['tank-references'])
        self.assertNotIn('references', by_name['tank-lora'])
        self.assertEqual(by_name['tank-lora']['prompt'], 'x, a tank')
        self.assertEqual(by_name['tank-both']['lora'], 'l.safetensors')
        self.assertEqual(by_name['tank-both']['references'], ['r.png'])

    def test_a_subject_can_stay_out_of_the_lora_comparison(self):
        requests = [{'name': 'tank', 'engine': 'klein-base', 'prompt': 'A tank.', 'references': ['r.png'], 'lora_comparison': False}]
        self.assertEqual(lora_experiment.route_requests(requests, 'x', 'l.safetensors'), [])


class ExportBackgroundTests(unittest.TestCase):
    def setUp(self):
        try:
            from PIL import Image
        except ImportError:
            self.skipTest('needs Pillow')
        import export_sprite
        self.white_to_alpha = export_sprite.white_to_alpha
        # White around a dark ring, with white inside the ring.
        self.image = Image.new('RGB', (9, 9), (255, 255, 255))
        for x in range(2, 7):
            for y in range(2, 7):
                self.image.putpixel((x, y), (10, 10, 10))
        self.image.putpixel((4, 4), (255, 255, 255))

    def test_white_inside_the_object_stays_solid(self):
        result = self.white_to_alpha(self.image)
        self.assertEqual(result.getpixel((0, 0))[3], 0)
        self.assertEqual(result.getpixel((4, 4))[3], 255)
        self.assertEqual(result.getpixel((3, 3))[3], 255)

    def test_all_white_clears_it_too(self):
        result = self.white_to_alpha(self.image, all_white=True)
        self.assertEqual(result.getpixel((0, 0))[3], 0)
        self.assertEqual(result.getpixel((4, 4))[3], 0)
        self.assertEqual(result.getpixel((3, 3))[3], 255)


class RequestFileTests(unittest.TestCase):
    def test_every_baseline_image_exists(self):
        import pathlib
        import review_page
        generated = pathlib.Path(ROOT) / 'assets' / 'phobos-medical' / 'generated'
        baselines = review_page.baselines_for(generated)
        self.assertIn('oxygen-concentrator', baselines)
        for subject, entry in baselines.items():
            self.assertTrue((generated / entry['src']).is_file(), subject)

    def test_the_stand_in_reference_is_in_the_repository(self):
        import json
        with open(os.path.join(ROOT, 'assets', 'phobos-medical', 'requests', 'first-batch.json'), encoding='utf-8') as file:
            subject = next(r for r in json.load(file) if r['name'] == 'oxygen-concentrator-from-stand-in')
        self.assertTrue(os.path.isfile(os.path.join(ROOT, subject['references'][0])))
        self.assertIs(subject['lora_comparison'], False)

    def test_the_medical_requests_build_valid_graphs_with_no_server(self):
        spec = importlib.util.spec_from_file_location('generate_art_for_requests', os.path.join(ROOT, 'scripts', 'generate-art.py'))
        art = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(art)
        for name in ('first-batch.json', 'test-run.json'):
            graphs = art.preview_graphs(os.path.join(ROOT, 'assets', 'phobos-medical', 'requests', name))
            self.assertTrue(graphs)


class ArtRunLoadsTests(unittest.TestCase):
    def test_the_runner_imports_and_needs_a_stop_time(self):
        spec = importlib.util.spec_from_file_location('art_run', os.path.join(ROOT, 'scripts', 'art-run.py'))
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        with self.assertRaises(SystemExit):
            stderr, sys.stderr = sys.stderr, open(os.devnull, 'w')
            try:
                module.main(['generate', 'nothing.json'])
            finally:
                sys.stderr.close()
                sys.stderr = stderr


if __name__ == '__main__':
    unittest.main()


class PreviewTests(unittest.TestCase):
    def setUp(self):
        try:
            from PIL import Image
        except ImportError:
            self.skipTest('needs Pillow')
        import make_preview
        self.make_preview = make_preview
        self.Image = Image

    def test_any_background_becomes_a_640_by_360_preview(self):
        for size in ((1280, 720), (1024, 1536), (2000, 500)):
            preview = self.make_preview.make_preview(self.Image.new('RGB', size, (200, 200, 200)), 'Water Tap', 'A line')
            self.assertEqual(preview.size, (640, 360))

    def test_the_lower_part_is_darkened_for_the_text_and_the_top_is_not(self):
        preview = self.make_preview.make_preview(self.Image.new('RGB', (1280, 720), (200, 200, 200)), 'X')
        self.assertEqual(preview.getpixel((630, 10)), (200, 200, 200))
        self.assertLess(preview.getpixel((630, 355))[0], 60)


class RetrainTests(unittest.TestCase):
    def test_settings_come_only_from_the_options_given(self):
        args = lora_experiment.argparse.Namespace(steps=2500, lr=None, resolutions='768,1024')
        self.assertEqual(lora_experiment.settings_from(args), {'steps': 2500, 'resolutions': [768, 1024]})

    def test_the_new_settings_reach_the_training_config(self):
        config = lora_experiment.training_config('v2', 'd', 'o', 't', {'steps': 2500, 'resolutions': [768, 1024]})
        process = config['config']['process'][0]
        self.assertEqual(process['train']['steps'], 2500)
        self.assertEqual(process['datasets'][0]['resolution'], [768, 1024])
        self.assertEqual(process['train']['lr'], lora_experiment.TRAINING['lr'])
