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


if __name__ == '__main__':
    unittest.main()
