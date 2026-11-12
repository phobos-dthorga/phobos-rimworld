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

    def test_the_hash_counts_only_between_subjects_of_like_proportions(self):
        # The owner's fuel tank (0.19 wide to tall) hashed 7 bits from the game's wide 1 by 3 altar (3.1): not a copy.
        self.assertFalse(sort_rules.same_proportions(0.193, 3.097))
        self.assertFalse(sort_rules.is_near_copy(7, 0.34, proportions_agree=False))
        # A blurred or tilted copy stays within 1.13 times of its source, and still flags on the hash.
        self.assertTrue(sort_rules.same_proportions(0.50, 0.565))
        self.assertTrue(sort_rules.is_near_copy(7, 0.34, proportions_agree=True))
        # The structural score flags whatever the proportions.
        self.assertTrue(sort_rules.is_near_copy(40, 0.60, proportions_agree=False))
        self.assertFalse(sort_rules.same_proportions(0.0, 1.0))

    def test_blank_on_blank_windows_do_not_count_as_shared_structure(self):
        self.assertFalse(sort_rules.compares_structure(255, 240))
        self.assertTrue(sort_rules.compares_structure(255, 40))
        self.assertTrue(sort_rules.compares_structure(30, 30))

    def test_nothing_drawn_in_either_picture_shares_no_structure(self):
        self.assertEqual(sort_rules.mean_or_zero([]), 0.0)
        self.assertEqual(sort_rules.mean_or_zero([1.0, 0.5]), 0.75)


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

    def test_a_named_hole_clears_background_the_object_encloses(self):
        # A trailer's drawbar frame, 12 Nov 2026: background inside a closed shape, cleared only where named.
        result = self.white_to_alpha(self.image, holes=[(4, 4)])
        self.assertEqual(result.getpixel((4, 4))[3], 0)
        self.assertEqual(result.getpixel((3, 3))[3], 255)
        self.assertEqual(result.getpixel((0, 0))[3], 0)

    def ai_case(self):
        """A dark frame enclosing two white rooms side by side: the left is background showing through (the cargo hook's opening, owner,
        12 Nov 2026), the right is white paint (a bed sheet). The AI's mask calls the left background and the right object."""
        from PIL import Image
        image = Image.new('RGB', (13, 7), (255, 255, 255))
        for x in range(1, 12):
            for y in range(1, 6):
                image.putpixel((x, y), (10, 10, 10))
        for y in range(2, 5):
            for x in (2, 3, 4):
                image.putpixel((x, y), (255, 255, 255))
            for x in (8, 9, 10):
                image.putpixel((x, y), (250, 250, 250))
        mask = Image.new('L', (13, 7), 255)
        for y in range(2, 5):
            for x in (2, 3, 4):
                mask.putpixel((x, y), 0)
        return image, mask

    def test_the_ai_clears_enclosed_background_and_keeps_white_paint(self):
        image, mask = self.ai_case()
        result = self.white_to_alpha(image, background=mask)
        self.assertEqual(result.getpixel((3, 3))[3], 0, 'the hook opening clears')
        self.assertEqual(result.getpixel((9, 3))[3], 255, 'the sheet stays')
        self.assertEqual(result.getpixel((6, 3))[3], 255, 'the dark frame stays')
        self.assertEqual(result.getpixel((0, 0))[3], 0, 'the outside still clears')

    def test_without_the_ai_enclosed_white_stays_as_before(self):
        image, _ = self.ai_case()
        result = self.white_to_alpha(image)
        self.assertEqual(result.getpixel((3, 3))[3], 255)

    def test_white_joined_to_white_paint_is_one_patch_and_stays(self):
        # The two rooms joined by a white corridor are one white patch the AI is sure of only in part: when unsure, the white stays,
        # and the sheet is never cleared.
        image, mask = self.ai_case()
        for x in (5, 6, 7):
            image.putpixel((x, 3), (252, 252, 252))
        result = self.white_to_alpha(image, background=mask)
        self.assertEqual(result.getpixel((9, 3))[3], 255)
        self.assertEqual(result.getpixel((3, 3))[3], 255)

    def test_pale_shading_does_not_join_an_opening_to_the_object(self):
        # Pale metal (whiteness 220, under the 235 threshold) between the opening and the sheet does not make them one patch.
        image, mask = self.ai_case()
        for x in (5, 6, 7):
            image.putpixel((x, 3), (220, 220, 220))
        result = self.white_to_alpha(image, background=mask)
        self.assertEqual(result.getpixel((3, 3))[3], 0)
        self.assertEqual(result.getpixel((9, 3))[3], 255)

    def test_a_patch_the_ai_is_unsure_of_is_left_whole(self):
        # The Diagnose clipboard's paper, 12 Nov 2026: the AI was sure of only part of a white patch and cleared blotches. A patch is
        # cleared whole or not at all.
        image, mask = self.ai_case()
        mask.putpixel((2, 2), 255)
        mask.putpixel((3, 2), 255)
        result = self.white_to_alpha(image, background=mask)
        self.assertEqual(result.getpixel((3, 3))[3], 255, 'two of nine pixels unsure: the whole patch stays')
        self.assertEqual(result.getpixel((4, 4))[3], 255)
        self.assertEqual(result.getpixel((0, 0))[3], 0, 'the outside still clears')

    def test_a_soft_edge_loses_the_white_it_was_mixed_with(self):
        import export_sprite
        # A fifth of a dark red object (40, 0, 0) over white shows as (212, 204, 204); with alpha 51 the red comes back exactly.
        self.assertEqual(export_sprite.unmixed(212, 51), 40)
        self.assertEqual(export_sprite.unmixed(204, 51), 0)
        self.assertEqual(export_sprite.unmixed(200, 255), 200, 'an opaque pixel is untouched')
        self.assertEqual(export_sprite.unmixed(255, 0), 255, 'a clear pixel is left alone')

    def test_a_silhouette_is_white_with_opacity_from_darkness(self):
        import export_sprite
        self.image.putpixel((2, 2), (135, 135, 135))  # A grey edge pixel, half way between solid and clear.
        result = export_sprite.silhouette(self.image)
        self.assertEqual(result.getpixel((3, 3)), (255, 255, 255, 255))
        self.assertEqual(result.getpixel((0, 0))[3], 0)
        self.assertEqual(result.getpixel((4, 4))[3], 0, 'white inside the shape becomes a hole')
        self.assertTrue(0 < result.getpixel((2, 2))[3] < 255, 'an edge stays soft')


class RequestFileTests(unittest.TestCase):
    def test_every_baseline_image_exists(self):
        import pathlib
        import review_page
        generated = pathlib.Path(ROOT) / 'assets' / 'phobos-medical' / 'generated'
        baselines = review_page.baselines_for(generated)
        self.assertIn('oxygen-concentrator', baselines)
        for subject, entry in baselines.items():
            self.assertTrue((generated / entry['src']).is_file(), subject)

    def test_the_stand_in_reference_is_kept_off_main_and_archived(self):
        # Our reference drawings are not on main (owner, 7 Nov 2026): assets/*/references/ is ignored, and the art-references
        # branch keeps each one under drawings/. A clean checkout of main (CI) has neither the file nor that branch; the test
        # once asked for the file itself and failed there on every commit, skipping every .NET step after it.
        import json
        import subprocess
        with open(os.path.join(ROOT, 'assets', 'phobos-medical', 'requests', 'first-batch.json'), encoding='utf-8') as file:
            subject = next(r for r in json.load(file) if r['name'] == 'oxygen-concentrator-from-stand-in')
        reference = subject['references'][0]
        self.assertRegex(reference, r'^assets/[^/]+/references/')
        self.assertIs(subject['lora_comparison'], False)
        git = ['git', '-c', 'core.fsmonitor=false']
        branch = subprocess.run(git + ['rev-parse', '--verify', '--quiet', 'origin/art-references'], cwd=ROOT,
                                capture_output=True, check=False)
        if branch.returncode != 0:
            self.skipTest('this checkout has no art-references branch (a clean checkout of main)')
        archived = subprocess.run(git + ['cat-file', '-e', 'origin/art-references:drawings/' + os.path.basename(reference)],
                                  cwd=ROOT, capture_output=True, check=False)
        self.assertEqual(archived.returncode, 0, 'the art-references branch should keep ' + os.path.basename(reference))

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
