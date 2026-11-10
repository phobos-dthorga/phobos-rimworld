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

    def test_a_loop_trims_nothing_and_crossfades_its_ends(self):
        self.assertNotIn('silenceremove', sound_review.review_filter(2.0, trim=False))
        self.assertIn('silenceremove', sound_review.review_filter(2.0))
        self.assertIn(f'acrossfade=d={sound_review.LOOP_CROSSFADE_S}', sound_review.loop_graph(1.5))


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
