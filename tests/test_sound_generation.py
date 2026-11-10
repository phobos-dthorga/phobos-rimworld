"""Offline checks of sound generation: the Stable Audio 3 graph and records in scripts/generate-art.py, the model fetcher's
filter, and scripts/art/sound_review.py's parsers, flags and filters. No server, no GPU, no model. One end-to-end test runs
ffmpeg on a made-up tone when ffmpeg is installed, and is skipped otherwise."""
import importlib.util
import json
import math
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, 'scripts', 'art'))
import fetch_models  # noqa: E402
import sound_review  # noqa: E402


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


art = load('scripts/generate-art.py', 'generate_art_for_sound')

SOUND = {'name': 'hum', 'engine': 'sa3-sfx-base', 'prompt': 'a quiet hum. Length: 8 seconds', 'seconds': 8, 'seeds': [11], 'loop': True}


class AudioGraphTests(unittest.TestCase):
    def test_the_graph_follows_the_template_core(self):
        graph = art.build_graph(SOUND, 11)
        types = {node['class_type'] for node in graph.values()}
        self.assertEqual(types, {'CheckpointLoaderSimple', 'CLIPLoader', 'CLIPTextEncode', 'EmptyLatentAudio', 'KSampler',
                                 'VAEDecodeAudio', 'SaveAudioAdvanced'})
        self.assertEqual(graph['1']['inputs']['ckpt_name'], 'stable_audio_3_small_sfx_base.safetensors')
        self.assertEqual(graph['2']['inputs'], {'clip_name': art.SA3_CLIP, 'type': 'stable_audio', 'device': 'default'})
        self.assertEqual(graph['5']['inputs'], {'seconds': 8.0, 'batch_size': 1})
        sampler = graph['6']['inputs']
        self.assertEqual((sampler['seed'], sampler['steps'], sampler['cfg'], sampler['sampler_name']), (11, 50, 7.0, 'lcm'))
        self.assertEqual(graph['7']['inputs']['vae'], ['1', 2])
        self.assertEqual(graph['8']['inputs']['format'], 'flac')

    def test_the_distilled_engine_takes_few_steps(self):
        graph = art.build_graph({**SOUND, 'engine': 'sa3-sfx'}, 1)
        self.assertEqual((graph['6']['inputs']['steps'], graph['6']['inputs']['cfg']), (8, 1.0))

    def test_a_sound_needs_seconds_in_range_and_no_pictures(self):
        for bad in ({**SOUND, 'seconds': 0}, {**SOUND, 'seconds': art.MAX_AUDIO_SECONDS + 1}, {k: v for k, v in SOUND.items() if k != 'seconds'}):
            with self.assertRaises(ValueError):
                art.build_graph(bad, 1)
        with self.assertRaises(ValueError):
            art.build_graph(SOUND, 1, reference_names=['ref.png'])

    def test_sounds_are_saved_as_flac_and_images_as_png(self):
        self.assertEqual(art.target_for(Path('out'), SOUND, 11).name, 'hum-11.flac')
        self.assertEqual(art.target_for(Path('out'), {'name': 'icon', 'engine': 'klein-base'}, 3).name, 'icon-3.png')
        self.assertEqual(art.describe_size(SOUND), '8 s')
        self.assertEqual(art.describe_size({'name': 'icon', 'width': 64, 'height': 32}), '64x32')

    def test_the_server_saves_sounds_under_audio(self):
        outputs = {'8': {'audio': [{'filename': 'hum_00001_.flac', 'subfolder': 'phobos', 'type': 'output'}]}}
        self.assertEqual(art.saved_files(outputs)[0]['filename'], 'hum_00001_.flac')

    def test_a_sound_record_says_how_long_whether_it_loops_and_that_ai_made_it(self):
        with tempfile.TemporaryDirectory() as folder:
            target = Path(folder) / 'hum-11.flac'
            record = art.make_record(SOUND, 'sa3-sfx-base', target, b'fLaC', '0.41.0', SOUND['engine'], 'abc', 11,
                                     Path(ROOT) / 'assets' / 'x.json', None, 1.0, [])
        self.assertEqual((record['seconds_asked'], record['loop'], record['text_encoder']), (8.0, True, art.SA3_CLIP))
        self.assertIn('generative AI', record['ai_disclosure'])
        self.assertNotIn('size', record)

    def test_every_sound_engine_names_a_model_in_the_manifest(self):
        manifest = {item['file'] for item in json.loads(Path(ROOT, 'scripts', 'art', 'models.json').read_text(encoding='utf-8'))['models']}
        for name, engine in art.ENGINES.items():
            if engine.get('kind') == 'audio':
                self.assertIn(engine['model'], manifest, name)
        self.assertIn(art.SA3_CLIP, manifest)


class FetchFilterTests(unittest.TestCase):
    def test_optional_models_wait_to_be_asked_for(self):
        big = {'file': 'stable_audio_3_medium.safetensors', 'optional': True}
        small = {'file': 'stable_audio_3_small_sfx.safetensors'}
        self.assertFalse(fetch_models.wanted(big))
        self.assertTrue(fetch_models.wanted(big, optional=True))
        self.assertTrue(fetch_models.wanted(big, only='MEDIUM'))
        self.assertTrue(fetch_models.wanted(small))
        self.assertFalse(fetch_models.wanted(small, only='t5gemma'))


EBUR128_TAIL = """[Parsed_ebur128_0 @ 0x1] Summary:

  Integrated loudness:
    I:         -18.5 LUFS
    Threshold: -28.6 LUFS

  True peak:
    Peak:        1.0 dBFS
[Parsed_volumedetect_1 @ 0x2] n_samples: 132300
[Parsed_volumedetect_1 @ 0x2] mean_volume: -30.3 dB
[Parsed_volumedetect_1 @ 0x2] max_volume: -4.0 dB
"""


class ReviewParserTests(unittest.TestCase):
    def test_measurements_are_read_from_ffmpeg_output(self):
        self.assertEqual(sound_review.parse_ebur128(EBUR128_TAIL), (-18.5, 1.0))
        self.assertEqual(sound_review.parse_volumedetect(EBUR128_TAIL), (-30.3, -4.0))
        self.assertAlmostEqual(sound_review.parse_duration('  Duration: 00:01:02.50, start: 0'), 62.5)
        self.assertIsNone(sound_review.parse_duration('nothing'))

    def test_silence_reads_as_minus_infinity(self):
        mean, _ = sound_review.parse_volumedetect('mean_volume: -inf dB\nmax_volume: -inf dB')
        self.assertEqual(mean, -math.inf)


def calm(**changes):
    """A measurement that earns no flag, with some values changed."""
    return {'lufs': -24.0, 'true_peak': -10.0, 'mean_db': -28.0, 'max_db': -10.0, 'high_mean_db': -50.0, **changes}


class ReviewFlagTests(unittest.TestCase):
    def codes(self, measure):
        return [code for code, _ in sound_review.flags_for(measure)]

    def test_a_calm_sound_earns_nothing(self):
        self.assertEqual(self.codes(calm()), [])

    def test_each_flag_has_its_cause(self):
        self.assertEqual(self.codes(calm(max_db=0.0)), ['clipped'])
        self.assertEqual(self.codes(calm(true_peak=0.5, max_db=-3.0, lufs=-12.0)), ['clipped'])
        self.assertEqual(self.codes(calm(high_mean_db=-31.0)), ['highs'])
        self.assertEqual(self.codes(calm(true_peak=-1.0, lufs=-25.0)), ['spiky'])
        self.assertEqual(self.codes(calm(seam_jump_db=4.0)), ['seam'])

    def test_a_silent_sound_is_only_called_silent(self):
        self.assertEqual(self.codes(calm(mean_db=-math.inf, high_mean_db=-math.inf, lufs=-math.inf)), ['silent'])

    def test_the_gain_levels_without_passing_the_peak_ceiling(self):
        self.assertEqual(sound_review.gain_db({'lufs': -30.0, 'true_peak': -20.0}), 10.0)
        self.assertEqual(sound_review.gain_db({'lufs': -30.0, 'true_peak': -4.0}), 3.0)
        self.assertIsNone(sound_review.gain_db({'lufs': -math.inf, 'true_peak': -math.inf}))


class ReviewFilterTests(unittest.TestCase):
    def test_the_lowered_copy_moves_only_the_top_band(self):
        graph = sound_review.lowered_graph()
        self.assertIn(f'acrossover=split={sound_review.SPLIT_HZ}', graph)
        self.assertIn('rubberband=pitch=0.5', graph)
        self.assertIn('rubberband=pitch=0.25', sound_review.lowered_graph(2))
        self.assertIn('[lo][down]amix', graph)

    def test_a_loop_keeps_its_steady_span_and_crossfades_its_ends(self):
        self.assertNotIn('silenceremove', sound_review.review_filter(2.0, trim=False))
        self.assertIn('silenceremove', sound_review.review_filter(2.0))
        self.assertIn(f'acrossfade=d={sound_review.LOOP_CROSSFADE_S}', sound_review.loop_graph(1.5))
        self.assertNotIn('atrim=start=0.5', sound_review.loop_graph(1.5))
        self.assertIn('atrim=start=0.5000:end=6.3000', sound_review.loop_graph(1.5, span=(0.5, 6.3)))


def frames(levels, step=0.1):
    """Momentary loudness frames at 100 ms, from a list of levels."""
    return [(round((i + 1) * step, 2), level) for i, level in enumerate(levels)]


class LoopLoudnessTests(unittest.TestCase):
    """The pilot's first loop (10 Nov 2026) faded out early under a steady rumble: a silence test missed it."""

    def test_momentary_readings_are_read_from_ffmpeg(self):
        text = ('[Parsed_ebur128_0 @ 1] t: 0.0999773  TARGET:-23 LUFS    M:-120.7 S:-120.7     I: -70.0 LUFS\n'
                '[Parsed_ebur128_0 @ 1] t: 0.899977   TARGET:-23 LUFS    M: -12.7 S:-120.7     I: -15.5 LUFS\n')
        self.assertEqual(sound_review.parse_momentary(text), [(0.0999773, -120.7), (0.899977, -12.7)])

    def test_the_steady_span_leaves_out_a_late_start_and_an_early_fade(self):
        levels = [-60] * 5 + [-10] * 50 + [-30] * 15  # Quiet for 0.5 s, steady for 5 s, faded for 1.5 s.
        start, end = sound_review.steady_span(frames(levels), -11.0)
        self.assertAlmostEqual(start, 0.4, places=2)
        self.assertAlmostEqual(end, 5.3, places=2)
        self.assertIsNone(sound_review.steady_span(frames([-60] * 10), -11.0))
        self.assertIsNone(sound_review.steady_span(frames([-10] * 3), -11.0))

    def test_a_dip_inside_or_at_the_seam_is_measured(self):
        steady = [-10] * 40
        self.assertEqual(sound_review.longest_dip(frames(steady), -10.0), 0.0)
        self.assertEqual(sound_review.longest_dip(frames(steady[:20] + [-25] * 6 + steady[:20]), -10.0), 0.6)
        self.assertEqual(sound_review.longest_dip(frames([-120] * 4 + steady), -10.0), 0.0)  # The first window is skipped.

    def test_a_narrow_tone_is_a_whine_but_a_falling_slope_is_not(self):
        bands = list(sound_review.WHINE_BANDS_HZ)
        # Band levels measured from the pilot (10 Nov 2026): the concentrator hum's 15 kHz whine, and a sonar ping's plain slope.
        hum = [-73, -74, -70, -74, -73, -75, -74, -69, -73, -73, -73, -71, -68, -70, -69, -68, -69, -67, -67, -59, -59, -51, -44,
               -52, -53, -69, -72, -75, -76]
        ping = [-66, -68, -70, -72, -74, -75, -77, -78, -80, -81, -82, -83, -84, -86, -87, -87, -90, -90, -90, -90, -91, -91, -91,
                -91, -91, -91, -91, -91, -91]
        whine = sound_review.strongest_whine(dict(zip(bands, hum)))
        self.assertEqual(whine['hz'], 15000)
        self.assertGreater(whine['prominence_db'], sound_review.WHINE_PROMINENCE_DB)
        self.assertLess(sound_review.strongest_whine(dict(zip(bands, ping)))['prominence_db'], sound_review.WHINE_PROMINENCE_DB)
        self.assertIn('whine', [code for code, _ in sound_review.flags_for(calm(whine=whine))])
        self.assertIsNone(sound_review.strongest_whine({4000: -60.0}))

    def test_a_whine_alone_is_moved_to_the_midrange_and_levelled(self):
        solo = sound_review.whine_solo_filter(15000)
        self.assertIn('bandpass=f=15000', solo)
        self.assertIn(f'rubberband=pitch={1000 / 15000:.4f}', solo)
        self.assertIn(f'loudnorm=I={sound_review.WHINE_SOLO_LUFS:g}', solo)

    def test_band_reports_are_matched_in_order(self):
        text = ''.join(f'[Parsed_volumedetect_{2 * i + 2} @ 0x1] mean_volume: -{50 + i}.0 dB\n' for i in range(len(sound_review.WHINE_BANDS_HZ)))
        levels = sound_review.parse_band_levels(text)
        self.assertEqual(levels[4000], -50.0)
        self.assertEqual(levels[4500], -51.0)
        self.assertEqual(sound_review.parse_band_levels('nothing'), {})

    def test_a_hum_is_joined_where_its_ends_are_in_step(self):
        from array import array
        rate, hz = 8000, 100.0  # A pure 100 Hz hum: in step every 10 ms.
        samples = array('f', [math.sin(2 * math.pi * hz * i / rate) for i in range(rate * 3)])
        end, match = sound_review.best_loop_end(samples, rate, 0.0, 2.995, crossfade=0.05, search=0.02)
        self.assertGreater(match, 0.99)
        self.assertAlmostEqual((end * hz) % 1.0, 0.0, places=2)  # A whole number of cycles, so the join is seamless.

    def test_a_short_dip_at_the_join_is_measured(self):
        from array import array
        rate = 8000
        steady = [0.5 * (1 if i % 2 else -1) for i in range(rate * 2)]
        dipped = list(steady)
        for i in range(rate - 160, rate + 160):  # 40 ms at a fifth of the level, across the join at 1 s.
            dipped[i] *= 0.2
        self.assertEqual(sound_review.seam_dip_db(array('f', steady), rate, 1.0), 0.0)
        self.assertGreater(sound_review.seam_dip_db(array('f', dipped), rate, 1.0), sound_review.SEAM_DIP_DB)
        self.assertIn('seamdip', [code for code, _ in sound_review.flags_for(calm(seam_dip_db=5.2))])

    def test_matching_ends_fade_at_equal_gain(self):
        self.assertIn('c1=tri', sound_review.loop_graph(0, span=(0.2, 6.5), coherent=True))
        self.assertIn('c1=qsin', sound_review.loop_graph(0, span=(0.2, 6.5)))

    def test_clean_candidates_come_first_on_the_page(self):
        clean = sound_review.review_order({'flags': []})
        advisory = sound_review.review_order({'flags': [{'code': 'highs'}, {'code': 'spiky'}]})
        serious = sound_review.review_order({'flags': [{'code': 'whine'}]})
        self.assertLess(clean, advisory)
        self.assertLess(advisory, serious)
        self.assertEqual(set(sound_review.SERIOUS_FLAGS), set(export_sound.EXPORT_FLAGS_TO_REFUSE))

    def test_a_long_dip_earns_the_gap_flag(self):
        self.assertIn(('gap'), [code for code, _ in sound_review.flags_for(calm(loop_gap_s=0.6))])
        self.assertNotIn('gap', [code for code, _ in sound_review.flags_for(calm(loop_gap_s=0.1))])


import datetime as _dt  # noqa: E402
import contextlib  # noqa: E402
import io  # noqa: E402
import safeguards  # noqa: E402

art_run = load('scripts/art-run.py', 'art_run_for_sound')


class ProcessorRunTests(unittest.TestCase):
    """art-run.py --cpu: small models on the processor beside the game (owner's request, 10 Nov 2026)."""

    def test_processor_lists_read_and_print_as_ranges(self):
        self.assertEqual(safeguards.parse_cpu_list('16-31'), 0xFFFF0000)
        self.assertEqual(safeguards.parse_cpu_list('0, 2-3'), 0b1101)
        self.assertEqual(safeguards.describe_mask(0xFFFF0000), '16-31')
        self.assertEqual(safeguards.describe_mask(0b1101), '0,2-3')
        with self.assertRaises(ValueError):
            safeguards.parse_cpu_list('')

    def test_an_x3d_keeps_the_run_off_the_v_cache(self):
        groups = [(96 << 20, 0xFFFF), (32 << 20, 0xFFFF0000)]  # A Ryzen 9 7950X3D, as Windows reported it on 10 Nov 2026.
        self.assertEqual(safeguards.pick_cpu_mask(groups, 32), 0xFFFF0000)

    def test_equal_caches_take_the_upper_half(self):
        self.assertEqual(safeguards.pick_cpu_mask([(32 << 20, 0xFFFF), (32 << 20, 0xFFFF0000)], 32), 0xFFFF0000)
        self.assertEqual(safeguards.pick_cpu_mask([], 8), 0xF0)
        self.assertEqual(safeguards.pick_cpu_mask([], 1), 1)

    def test_one_thread_per_core(self):
        self.assertEqual(art_run.cpu_plan('16-31'), (0xFFFF0000, 8))
        self.assertEqual(art_run.cpu_plan('5'), (0b100000, 1))

    def test_the_memory_floor_stops_a_processor_run_but_not_the_game(self):
        now = _dt.datetime(2026, 11, 10, 20, 0)
        later = now + _dt.timedelta(hours=1)
        game = {'rimworld.exe'}
        self.assertIsNone(safeguards.stop_reason(now, later, game, {'C:\\': 100}, names=(), free_ram_gb=10.0))
        self.assertIn('memory', safeguards.stop_reason(now, later, game, {'C:\\': 100}, names=(), free_ram_gb=4.0))
        self.assertIn('rimworld', safeguards.stop_reason(now, later, game, {'C:\\': 100}))

    def test_a_night_step_may_run_on_the_processor_only_when_it_generates(self):
        self.assertEqual(art_run.night_problems([{'generate': 'a.json', 'cpu': True}]), [])
        self.assertTrue(art_run.night_problems([{'train': 'c.yaml', 'cpu': True}]))
        args = art_run.argparse.Namespace(comfy='C:/AI', min_free_gb=5.0, dry_run=True, also_stop_on=[], cpu=False,
                                          cpu_cores=None, min_free_ram_gb=6.0)
        command = art_run.step_command({'generate': 'a.json', 'cpu': True}, _dt.datetime(2026, 11, 10, 23, 0), args)
        self.assertIn('--cpu', command)
        self.assertNotIn('--cpu', art_run.step_command({'generate': 'a.json'}, _dt.datetime(2026, 11, 10, 23, 0), args))

    def test_a_dry_run_says_where_it_would_run(self):
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            code = art_run.main(['generate', os.path.join(ROOT, 'assets', 'phobos-sounds', 'requests', 'pilot.json'), '--dry-run',
                                 '--cpu', '--cpu-cores', '16-31'])
        art.use_port(8188)
        art_run.art.use_port(8188)
        self.assertEqual(code, 0)
        self.assertIn('the processor only: logical processors 16-31, below-normal priority, 8 threads', out.getvalue())

    @unittest.skipUnless(os.name == 'nt', 'Windows only')
    def test_windows_reports_caches_and_memory(self):
        groups = art_run.read_l3_groups()
        self.assertTrue(groups)
        self.assertTrue(all(size > 0 and mask > 0 for size, mask in groups))
        self.assertGreater(art_run.free_ram_gb(), 0)


import export_sound  # noqa: E402
import game_sounds  # noqa: E402


class GameSoundTests(unittest.TestCase):
    """scripts/art/game_sounds.py: reading the game's SoundDefs and working out how loud they play."""

    def test_ranges_read_as_the_game_writes_them(self):
        self.assertEqual(game_sounds.parse_range('13'), (13.0, 13.0))
        self.assertEqual(game_sounds.parse_range('10~20'), (10.0, 20.0))
        self.assertEqual(game_sounds.parse_range('', (50.0, 50.0)), (50.0, 50.0))
        self.assertIsNone(game_sounds.parse_range(None))

    def test_effective_loudness_adds_the_sounddef_gain(self):
        # The game's chemfuel generator, measured 10 Nov 2026: -18.8 LUFS through volume 13.
        self.assertEqual(game_sounds.effective_lufs(-18.8, 13), -36.5)
        self.assertIsNone(game_sounds.effective_lufs(-math.inf, 13))

    def test_a_folder_grain_plays_every_clip_in_it(self):
        entries = {'Misc/MoisturePump/a': 1, 'Misc/MoisturePump/b': 1, 'Misc/Other': 1}
        self.assertEqual(game_sounds.clips_for({'class': 'AudioGrain_Folder', 'path': 'Misc/MoisturePump'}, entries),
                         ['Misc/MoisturePump/a', 'Misc/MoisturePump/b'])
        self.assertEqual(game_sounds.clips_for({'class': 'AudioGrain_Clip', 'path': 'Misc/Other'}, entries), ['Misc/Other'])
        self.assertEqual(game_sounds.clips_for({'class': 'AudioGrain_Clip', 'path': 'Missing'}, entries), [])


class ExportLevelTests(unittest.TestCase):
    """scripts/art/export_sound.py: levelling a picked sound to sit beside a game sound."""

    def test_the_file_level_undoes_our_sounddef_gain(self):
        # Beside the hemopump (-37.1), 3 dB quieter, through volume 50: the file itself sits at about -34.1.
        self.assertAlmostEqual(export_sound.file_level_for(-37.1, -3, 50), -34.08, places=2)
        self.assertAlmostEqual(export_sound.file_level_for(-36.5, 0, 13), -18.78, places=2)

    def test_the_gain_never_pushes_the_peak_past_the_ceiling(self):
        self.assertEqual(export_sound.gain_for({'lufs': -30.0, 'true_peak': -20.0}, target_lufs=-20.0), 10.0)
        self.assertEqual(export_sound.gain_for({'lufs': -30.0, 'true_peak': -5.0}, target_lufs=-20.0), 4.0)
        self.assertEqual(export_sound.gain_for({'lufs': -70.0, 'true_peak': -10.0}, target_peak=-4.0), 6.0)
        with self.assertRaises(ValueError):
            export_sound.gain_for({'lufs': -math.inf, 'true_peak': -math.inf}, target_lufs=-20.0)

    def test_a_oneshot_is_cut_mixed_to_mono_and_trimmed(self):
        chain = export_sound.oneshot_filter(cut=[0.98, 1.3], filter_chain='lowpass=f=11000')
        self.assertTrue(chain.startswith('lowpass=f=11000,pan=mono'))
        self.assertIn('atrim=start=0.98:end=1.3', chain)
        self.assertIn('silenceremove', chain)
        self.assertTrue(chain.endswith(f'aresample={export_sound.RATE}'))

    def test_a_short_loop_is_refused_but_a_short_oneshot_is_not(self):
        self.assertIn('give its spec a span', export_sound.too_short({'kind': 'loop'}, 0.91))
        self.assertIsNone(export_sound.too_short({'kind': 'loop'}, export_sound.MIN_LOOP_S))
        self.assertIsNone(export_sound.too_short({'kind': 'oneshot'}, 0.3))

    def test_a_spec_span_replaces_the_steady_span(self):
        # No ffmpeg is needed: a span in the spec is used as it stands, without measuring the master.
        self.assertEqual(export_sound.loop_span(None, None, {'span': [1.5, 6.2]}), (1.5, 6.2))

    def test_a_rerun_leaves_unchanged_sounds_alone(self):
        exports = Path(ROOT, 'assets', 'phobos-sounds', 'exports.json')
        spec = json.loads(exports.read_text(encoding='utf-8'))['exports'][0]
        records = json.loads(exports.with_name('exported.json').read_text(encoding='utf-8'))
        master = exports.parent / 'generated' / spec['candidate']
        if not master.is_file():
            self.skipTest('the generated masters are local, not in the repository')
        self.assertTrue(export_sound.unchanged(spec, records[spec['name']], master))
        self.assertFalse(export_sound.unchanged({**spec, 'offset_db': spec.get('offset_db', 0) - 1}, records[spec['name']], master))
        self.assertFalse(export_sound.unchanged(spec, None, master))

    def test_the_shipped_sounds_match_their_records(self):
        exports = Path(ROOT, 'assets', 'phobos-sounds', 'exports.json')
        specs = json.loads(exports.read_text(encoding='utf-8'))['exports']
        records = json.loads(exports.with_name('exported.json').read_text(encoding='utf-8'))
        self.assertEqual(export_sound.check(specs, records), [])


@unittest.skipUnless(sound_review.find_ffmpeg(), 'ffmpeg is not installed')
class ReviewEndToEndTests(unittest.TestCase):
    def test_a_made_up_hum_gets_every_review_file_and_a_page(self):
        exe = sound_review.find_ffmpeg()
        with tempfile.TemporaryDirectory() as folder:
            generated = Path(folder) / 'generated'
            (generated / 'hum').mkdir(parents=True)
            master = generated / 'hum' / 'hum-1.flac'
            subprocess.run([exe, '-hide_banner', '-loglevel', 'error', '-f', 'lavfi', '-i', 'sine=f=220:d=2', '-ar', '44100', str(master)],
                           check=True)
            (generated / 'hum' / 'hum-1.json').write_text(json.dumps({'seed': 1, 'loop': True, 'prompt': 'a hum'}), encoding='utf-8')
            self.assertEqual(sound_review.build(generated), 0)
            names = {path.name for path in (generated / 'hum' / 'review').iterdir()}
            for suffix in ('.review.ogg', '.lowered.ogg', '.loop.ogg', '.spectrogram.png', '.seam.png', '.measure.json'):
                self.assertIn('hum-1' + suffix, names)
            page = (generated / 'sound-review.html').read_text(encoding='utf-8')
            self.assertIn('hum/review/hum-1.lowered.ogg', page)
            self.assertIn(' loop ', page)
            measure = json.loads((generated / 'hum' / 'review' / 'hum-1.measure.json').read_text(encoding='utf-8'))
            self.assertAlmostEqual(measure['seconds'], 2.0, places=1)


if __name__ == '__main__':
    unittest.main()
