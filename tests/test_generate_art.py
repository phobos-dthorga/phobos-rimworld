"""Offline checks of the ComfyUI graphs scripts/generate-art.py builds. No server, no GPU, no model."""
import importlib.util
import os
import unittest

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def load(relative, name):
    spec = importlib.util.spec_from_file_location(name, os.path.join(ROOT, relative))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


art = load('scripts/generate-art.py', 'generate_art')


def links_resolve(graph):
    """Every [node, slot] reference in the graph names a node that exists."""
    for node in graph.values():
        for value in node['inputs'].values():
            if isinstance(value, list) and len(value) == 2 and isinstance(value[0], str):
                if value[0] not in graph:
                    return False
    return True


class SchnellGraphTests(unittest.TestCase):
    def test_text_to_image_graph(self):
        g = art.workflow('a radio', 1024, 768, 7, 4, 'model.safetensors', 'phobos/test')
        self.assertTrue(links_resolve(g))
        self.assertEqual(g['5']['inputs']['latent_image'], ['4', 0])
        self.assertEqual(g['5']['inputs']['denoise'], 1.0)
        self.assertEqual(g['5']['inputs']['seed'], 7)
        self.assertEqual(g['4']['inputs'], {'width': 1024, 'height': 768, 'batch_size': 1})

    def test_init_image_graph_starts_from_the_sketch(self):
        g = art.workflow('a radio', 512, 512, 1, 4, 'model.safetensors', 'phobos/test', init_name='sketch.png', denoise=0.6)
        self.assertTrue(links_resolve(g))
        self.assertEqual(g['8']['class_type'], 'LoadImage')
        self.assertEqual(g['9']['inputs']['width'], 512)
        self.assertEqual(g['5']['inputs']['latent_image'], ['10', 0])
        self.assertEqual(g['5']['inputs']['denoise'], 0.6)


class KleinGraphTests(unittest.TestCase):
    def test_text_to_image_matches_the_template_shape(self):
        g = art.klein_workflow('a mast', 1024, 1024, 3, 4, 'phobos/test')
        self.assertTrue(links_resolve(g))
        types = {n['class_type'] for n in g.values()}
        for required in ('UNETLoader', 'CLIPLoader', 'VAELoader', 'EmptyFlux2LatentImage', 'Flux2Scheduler', 'KSamplerSelect',
                         'RandomNoise', 'CFGGuider', 'SamplerCustomAdvanced', 'ConditioningZeroOut', 'VAEDecode', 'SaveImage'):
            self.assertIn(required, types)
        self.assertEqual(g['2']['inputs']['type'], 'flux2')
        self.assertEqual(g['10']['inputs']['cfg'], 1.0)
        self.assertEqual(g['7']['inputs']['steps'], 4)
        self.assertEqual(g['9']['inputs']['noise_seed'], 3)
        self.assertEqual(g['10']['inputs']['positive'], ['4', 0])
        self.assertEqual(g['10']['inputs']['negative'], ['5', 0])
        self.assertNotIn('LoadImage', types)

    def test_references_chain_on_both_conditionings(self):
        g = art.klein_workflow('image 1 in the style of image 2', 1024, 1024, 1, 4, 'phobos/test', references=['a.png', 'b.png'])
        self.assertTrue(links_resolve(g))
        loads = [n for n in g.values() if n['class_type'] == 'LoadImage']
        self.assertEqual([n['inputs']['image'] for n in loads], ['a.png', 'b.png'])
        refs = [k for k, n in g.items() if n['class_type'] == 'ReferenceLatent']
        self.assertEqual(len(refs), 4)
        positive = g['10']['inputs']['positive'][0]
        negative = g['10']['inputs']['negative'][0]
        self.assertEqual(g[positive]['class_type'], 'ReferenceLatent')
        self.assertEqual(g[negative]['class_type'], 'ReferenceLatent')
        # The last positive ReferenceLatent chains back to the first, which chains to the text encode.
        first_positive = g[positive]['inputs']['conditioning'][0]
        self.assertEqual(g[first_positive]['class_type'], 'ReferenceLatent')
        self.assertEqual(g[first_positive]['inputs']['conditioning'], ['4', 0])
        self.assertEqual(g[negative]['inputs']['latent'], g[positive]['inputs']['latent'])

    def test_lora_sits_between_the_model_and_the_guider(self):
        g = art.klein_workflow('2x2 sprite sheet of a radio mast', 1024, 1024, 1, 4, 'phobos/test', lora='sheet.safetensors', lora_strength=1.1)
        self.assertTrue(links_resolve(g))
        self.assertEqual(g['14']['class_type'], 'LoraLoaderModelOnly')
        self.assertEqual(g['14']['inputs']['strength_model'], 1.1)
        self.assertEqual(g['10']['inputs']['model'], ['14', 0])


class KleinBaseGraphTests(unittest.TestCase):
    REQUEST = {'name': 'tank', 'engine': 'klein-base', 'prompt': 'a tank', 'width': 1024, 'height': 1024, 'seeds': [1]}

    def test_uses_the_full_model_with_real_guidance_and_full_steps(self):
        g = art.build_graph(self.REQUEST, 1)
        self.assertTrue(links_resolve(g))
        self.assertEqual(g['1']['inputs']['unet_name'], art.KLEIN_BASE_UNET)
        self.assertEqual(g['7']['inputs']['steps'], 50)
        self.assertEqual(g['10']['inputs']['cfg'], 4.0)

    def test_the_negative_is_a_real_empty_prompt_not_zeroed(self):
        g = art.build_graph(self.REQUEST, 1)
        self.assertEqual(g['5']['class_type'], 'CLIPTextEncode')
        self.assertEqual(g['5']['inputs']['text'], '')

    def test_a_request_may_override_steps_and_guidance(self):
        g = art.build_graph({**self.REQUEST, 'steps': 20, 'cfg': 5}, 1)
        self.assertEqual(g['7']['inputs']['steps'], 20)
        self.assertEqual(g['10']['inputs']['cfg'], 5.0)

    def test_small_references_scale_smoothly(self):
        g = art.build_graph(self.REQUEST, 1, ['ref.png'])
        scale = next(n for n in g.values() if n['class_type'] == 'ImageScaleToTotalPixels')
        self.assertEqual(scale['inputs']['upscale_method'], 'lanczos')


class ZImageGraphTests(unittest.TestCase):
    REQUEST = {'name': 'hero', 'engine': 'zimage', 'prompt': 'a ward', 'width': 1024, 'height': 576, 'seeds': [1]}

    def test_matches_the_template_shape(self):
        g = art.build_graph(self.REQUEST, 9)
        self.assertTrue(links_resolve(g))
        self.assertEqual(g['3']['inputs']['type'], 'lumina2')
        self.assertEqual(g['3']['inputs']['clip_name'], art.KLEIN_CLIP)
        self.assertEqual(g['2']['inputs']['shift'], 3)
        self.assertEqual(g['8']['inputs']['sampler_name'], 'res_multistep')
        self.assertEqual(g['8']['inputs']['steps'], 25)
        self.assertEqual(g['8']['inputs']['cfg'], 4.0)
        self.assertEqual(g['8']['inputs']['seed'], 9)
        self.assertEqual(g['8']['inputs']['model'], ['2', 0])

    def test_a_lora_sits_before_the_sampling_shift(self):
        g = art.build_graph({**self.REQUEST, 'lora': 'style.safetensors'}, 1)
        self.assertTrue(links_resolve(g))
        self.assertEqual(g['2']['inputs']['model'], ['11', 0])

    def test_references_are_refused_because_it_cannot_use_them(self):
        with self.assertRaises(ValueError):
            art.build_graph(self.REQUEST, 1, ['ref.png'])


class Flux3GraphTests(unittest.TestCase):
    REQUEST = {'name': 'tank', 'engine': 'flux3-base', 'prompt': 'a tank like <ref_image_0>', 'width': 1024, 'height': 1024, 'seeds': [1]}

    def test_text_to_image_samples_from_the_prompt_directly(self):
        g = art.build_graph(self.REQUEST, 1)
        self.assertTrue(links_resolve(g))
        self.assertEqual(g['2']['inputs']['type'], 'flux3')
        self.assertEqual(g['7']['inputs']['positive'], ['4', 0])
        self.assertEqual(g['7']['inputs']['steps'], 50)
        self.assertEqual(g['7']['inputs']['cfg'], 3.5)

    def test_references_go_through_one_multi_latent_node(self):
        g = art.build_graph(self.REQUEST, 1, ['a.png', 'b.png'])
        self.assertTrue(links_resolve(g))
        multi = g['11']['inputs']
        self.assertEqual(g['11']['class_type'], 'ReferenceMultiLatent')
        self.assertEqual(multi['resolution'], 'resize')
        self.assertEqual(multi['images.image0'], ['20', 0])
        self.assertEqual(multi['images.image1'], ['21', 0])
        self.assertEqual(g['7']['inputs']['positive'], ['11', 0])
        self.assertEqual(g['7']['inputs']['negative'], ['11', 1])


class UpscaleGraphTests(unittest.TestCase):
    def test_upscales_then_fits_the_exact_size(self):
        g = art.build_graph({'name': 'bed', 'engine': 'upscale', 'width': 1024, 'height': 1024, 'seeds': [0]}, 0, init_name='bed.png')
        self.assertTrue(links_resolve(g))
        self.assertEqual(g['1']['inputs']['model_name'], art.UPSCALE_MODEL)
        self.assertEqual(g['4']['inputs']['width'], 1024)

    def test_it_needs_an_image(self):
        with self.assertRaises(ValueError):
            art.build_graph({'name': 'bed', 'engine': 'upscale', 'width': 1024, 'height': 1024, 'seeds': [0]}, 0)


class BuildGraphTests(unittest.TestCase):
    def test_an_unknown_engine_says_which_exist(self):
        with self.assertRaisesRegex(ValueError, 'klein-base'):
            art.build_graph({'name': 'x', 'engine': 'dalle', 'prompt': 'y', 'width': 1, 'height': 1}, 1)

    def test_a_request_with_no_engine_stays_on_schnell(self):
        g = art.build_graph({'name': 'x', 'prompt': 'y', 'width': 512, 'height': 512}, 1)
        self.assertEqual(g['1']['class_type'], 'CheckpointLoaderSimple')


class ValidateGraphTests(unittest.TestCase):
    INFO = {
        'UNETLoader': {'input': {'required': {'unet_name': [['present.safetensors'], {}], 'weight_dtype': [['default'], {}]}}},
        'SaveImage': {'input': {'required': {'images': ['IMAGE'], 'filename_prefix': ['STRING', {}]}}},
        'NewStyle': {'input': {'required': {'mode': ['COMBO', {'options': ['a', 'b']}]}}},
        'LoadImage': {'input': {'required': {'image': [['other.png'], {}]}}},
        'Multi': {'input': {'required': {'positive': ['CONDITIONING'], 'resolution': ['COMFY_DYNAMICCOMBO_V3', {}]}}},
    }

    def graph(self, kind, **inputs):
        return {'1': {'class_type': kind, 'inputs': inputs}}

    def test_a_good_graph_has_no_problems(self):
        self.assertEqual(art.validate_graph(self.graph('UNETLoader', unet_name='present.safetensors', weight_dtype='default'), self.INFO), [])

    def test_a_missing_model_file_is_caught_before_any_gpu_time(self):
        problems = art.validate_graph(self.graph('UNETLoader', unet_name='gone.safetensors', weight_dtype='default'), self.INFO)
        self.assertEqual(len(problems), 1)
        self.assertIn('gone.safetensors', problems[0])

    def test_an_unknown_node_is_caught(self):
        self.assertIn('Nope', art.validate_graph(self.graph('Nope'), self.INFO)[0])

    def test_a_missing_required_input_is_caught(self):
        self.assertIn('filename_prefix', art.validate_graph(self.graph('SaveImage', images=['2', 0]), self.INFO)[0])

    def test_the_newer_combo_spelling_is_read(self):
        self.assertEqual(len(art.validate_graph(self.graph('NewStyle', mode='c'), self.INFO)), 1)
        self.assertEqual(art.validate_graph(self.graph('NewStyle', mode='a'), self.INFO), [])

    def test_image_names_are_not_checked_because_they_arrive_later(self):
        self.assertEqual(art.validate_graph(self.graph('LoadImage', image='not-yet.png'), self.INFO), [])

    def test_dynamic_inputs_are_not_demanded_by_bare_name(self):
        self.assertEqual(art.validate_graph(self.graph('Multi', positive=['2', 0]), self.INFO), [])


class DisplayPathTests(unittest.TestCase):
    def test_inside_the_repository_is_relative(self):
        self.assertEqual(art.display_path(os.path.join(ROOT, 'scripts', 'x.png')), 'scripts/x.png')

    def test_outside_the_repository_stays_whole_instead_of_failing(self):
        outside = os.path.join(os.path.dirname(ROOT), 'elsewhere', 'x.png')
        self.assertTrue(art.display_path(outside).endswith('elsewhere/x.png'))


if __name__ == '__main__':
    unittest.main()
