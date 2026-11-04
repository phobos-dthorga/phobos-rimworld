"""Generate artwork candidates through a local ComfyUI server and record how each was made.

Reads a request file (JSON: a list of requests with a name, a prompt, sizes and seeds), submits one graph for every seed
through the request's engine (see ENGINES), waits for the images and saves them with a record beside each one: prompt, seed,
steps, size, model file and its SHA-256, server version and date. Nothing is sent anywhere but the local server. Needs ComfyUI
running (python scripts/generate-art.py --start starts it from COMFYUI_PATH or C:/AI/ComfyUI/ComfyUI_windows_portable).
For anything unattended use scripts/art-run.py, which adds the stop time, the game check and the safe finish.

    python scripts/generate-art.py assets/phobos-radio/requests/phase1-sprites.json
    python scripts/generate-art.py --start --only base-station assets/phobos-radio/requests/phase1-sprites.json
"""
import argparse
import datetime as _dt
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_COMFY = Path(os.environ.get('COMFYUI_PATH', r'C:\AI\ComfyUI\ComfyUI_windows_portable'))
SERVER = os.environ.get('COMFYUI_URL', 'http://127.0.0.1:8188')
DEFAULT_MODEL = 'flux1-schnell-fp8.safetensors'
NEGATIVE = ''  # Flux schnell ignores the negative prompt; it is kept empty on purpose.
CONSECUTIVE_FAILURES_TO_GIVE_UP = 3  # Three in a row, restarts included, means trying again will not help.
MAX_SERVER_RESTARTS = 5  # In one run, so a server that keeps dying cannot use up a whole night (authored).

KLEIN_UNET = 'flux-2-klein-4b.safetensors'
KLEIN_BASE_UNET = 'flux-2-klein-base-4b-fp8.safetensors'
KLEIN_CLIP = 'qwen_3_4b.safetensors'
KLEIN_VAE = 'flux2-vae.safetensors'
ZIMAGE_UNET = 'z_image_int8_convrot.safetensors'
ZIMAGE_VAE = 'ae.safetensors'
ZIMAGE_SHIFT = 3  # ModelSamplingAuraFlow value in ComfyUI's Z-Image template.
FLUX3_UNET = 'flux3_image_klein_int8_convrot.safetensors'
FLUX3_CLIP = 'qwen3vl_4b_int8_convrot.safetensors'
UPSCALE_MODEL = 'RealESRGAN_x4plus.pth'

# Steps and guidance are each publisher's or template's own, and a request may override them. Every timeout is generous:
# the owner prefers slow, full models over fast ones that need rerunning (5 Nov 2026).
ENGINES = {
    'schnell': {
        'label': 'Flux.1-schnell fp8 (Comfy-Org packaging, Apache 2.0)',
        'model': DEFAULT_MODEL, 'folder': 'checkpoints', 'steps': 4, 'cfg': 1.0, 'sampler': 'euler/simple', 'timeout': 600},
    'klein': {
        'label': 'FLUX.2 Klein 4B distilled (Black Forest Labs, Apache 2.0; Comfy-Org packaging)',
        'model': KLEIN_UNET, 'folder': 'diffusion_models', 'steps': 4, 'cfg': 1.0, 'sampler': 'euler/flux2', 'timeout': 600},
    # 50 steps at guidance 4 is the model card's own example (ComfyUI's template shortens it to 20 at guidance 5).
    'klein-base': {
        'label': 'FLUX.2 Klein 4B base fp8 (Black Forest Labs, Apache 2.0)',
        'model': KLEIN_BASE_UNET, 'folder': 'diffusion_models', 'steps': 50, 'cfg': 4.0, 'sampler': 'euler/flux2', 'timeout': 3600},
    # ComfyUI's Z-Image base template. No reference input: this model only makes images from text.
    'zimage': {
        'label': 'Z-Image base int8 (Alibaba Tongyi lab, Apache 2.0; Comfy-Org packaging)',
        'model': ZIMAGE_UNET, 'folder': 'diffusion_models', 'steps': 25, 'cfg': 4.0, 'sampler': 'res_multistep/simple', 'timeout': 3600},
    # Gated, FLUX Kommunity License: only runs once the owner has accepted the terms and saved the files. ComfyUI's template
    # note gives 50 steps at guidance 3.5.
    'flux3-base': {
        'label': 'FLUX 3 Image Klein base int8 (Black Forest Labs, FLUX Kommunity License v1.0)',
        'model': FLUX3_UNET, 'folder': 'diffusion_models', 'steps': 50, 'cfg': 3.5, 'sampler': 'euler/simple', 'timeout': 3600},
    'upscale': {
        'label': 'Real-ESRGAN x4plus (Xintao Wang and colleagues, BSD 3-Clause)',
        'model': UPSCALE_MODEL, 'folder': 'upscale_models', 'steps': 0, 'cfg': 0.0, 'sampler': 'none', 'timeout': 600},
}


class Stopped(Exception):
    """A safeguard ended the run. Not a failure: the report says why."""


def workflow(prompt, width, height, seed, steps, model, prefix, init_name=None, denoise=1.0):
    """ComfyUI API-format graph for Flux.1-schnell packaged as a single checkpoint.

    With init_name (a file in ComfyUI's input folder) the image is scaled to the target size and used
    as the starting latent, and denoise says how much of it the model may repaint (1.0 = ignore it).
    """
    graph = {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': model}},
        '2': {'class_type': 'CLIPTextEncode', 'inputs': {'text': prompt, 'clip': ['1', 1]}},
        '3': {'class_type': 'CLIPTextEncode', 'inputs': {'text': NEGATIVE, 'clip': ['1', 1]}},
        '4': {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': width, 'height': height, 'batch_size': 1}},
        '5': {'class_type': 'KSampler', 'inputs': {
            'seed': seed, 'steps': steps, 'cfg': 1.0, 'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': denoise,
            'model': ['1', 0], 'positive': ['2', 0], 'negative': ['3', 0], 'latent_image': ['4', 0]}},
        '6': {'class_type': 'VAEDecode', 'inputs': {'samples': ['5', 0], 'vae': ['1', 2]}},
        '7': {'class_type': 'SaveImage', 'inputs': {'images': ['6', 0], 'filename_prefix': prefix}},
    }
    if init_name:
        graph['8'] = {'class_type': 'LoadImage', 'inputs': {'image': init_name}}
        graph['9'] = {'class_type': 'ImageScale', 'inputs': {
            'image': ['8', 0], 'upscale_method': 'lanczos', 'width': width, 'height': height, 'crop': 'disabled'}}
        graph['10'] = {'class_type': 'VAEEncode', 'inputs': {'pixels': ['9', 0], 'vae': ['1', 2]}}
        graph['5']['inputs']['latent_image'] = ['10', 0]
    return graph


def klein_workflow(prompt, width, height, seed, steps, prefix, references=(), lora=None, lora_strength=1.0,
                   unet=KLEIN_UNET, clip=KLEIN_CLIP, vae=KLEIN_VAE, cfg=1.0):
    """ComfyUI API-format graph for FLUX.2 Klein 4B, after ComfyUI's own templates.

    references: names of images already in ComfyUI's input folder. Each becomes a ReferenceLatent on both
    the positive and the negative conditioning, chained in order, so the prompt can say "image 1", "image 2".
    No references = plain text-to-image. At cfg 1 (distilled) the negative is the prompt zeroed out, because it is never
    used; above 1 (base) it is a real empty prompt, as in ComfyUI's base template. Small sprites are scaled up with Lanczos:
    the template's nearest-exact would hand the model hard blocks to copy.
    """
    negative_node = ({'class_type': 'ConditioningZeroOut', 'inputs': {'conditioning': ['4', 0]}} if cfg == 1.0
                     else {'class_type': 'CLIPTextEncode', 'inputs': {'text': '', 'clip': ['2', 0]}})
    graph = {
        '1': {'class_type': 'UNETLoader', 'inputs': {'unet_name': unet, 'weight_dtype': 'default'}},
        '2': {'class_type': 'CLIPLoader', 'inputs': {'clip_name': clip, 'type': 'flux2', 'device': 'default'}},
        '3': {'class_type': 'VAELoader', 'inputs': {'vae_name': vae}},
        '4': {'class_type': 'CLIPTextEncode', 'inputs': {'text': prompt, 'clip': ['2', 0]}},
        '5': negative_node,
        '6': {'class_type': 'EmptyFlux2LatentImage', 'inputs': {'width': width, 'height': height, 'batch_size': 1}},
        '7': {'class_type': 'Flux2Scheduler', 'inputs': {'steps': steps, 'width': width, 'height': height}},
        '8': {'class_type': 'KSamplerSelect', 'inputs': {'sampler_name': 'euler'}},
        '9': {'class_type': 'RandomNoise', 'inputs': {'noise_seed': seed}},
        '10': {'class_type': 'CFGGuider', 'inputs': {'model': ['1', 0], 'positive': ['4', 0], 'negative': ['5', 0], 'cfg': cfg}},
        '11': {'class_type': 'SamplerCustomAdvanced', 'inputs': {
            'noise': ['9', 0], 'guider': ['10', 0], 'sampler': ['8', 0], 'sigmas': ['7', 0], 'latent_image': ['6', 0]}},
        '12': {'class_type': 'VAEDecode', 'inputs': {'samples': ['11', 0], 'vae': ['3', 0]}},
        '13': {'class_type': 'SaveImage', 'inputs': {'images': ['12', 0], 'filename_prefix': prefix}},
    }
    if lora:
        graph['14'] = {'class_type': 'LoraLoaderModelOnly', 'inputs': {'model': ['1', 0], 'lora_name': lora, 'strength_model': lora_strength}}
        graph['10']['inputs']['model'] = ['14', 0]
    positive, negative = ['4', 0], ['5', 0]
    next_id = 20
    for name in references:
        load, scale, encode, pos, neg = (str(next_id + i) for i in range(5))
        graph[load] = {'class_type': 'LoadImage', 'inputs': {'image': name}}
        graph[scale] = {'class_type': 'ImageScaleToTotalPixels', 'inputs': {
            'image': [load, 0], 'upscale_method': 'lanczos', 'megapixels': 1.0, 'resolution_steps': 1}}
        graph[encode] = {'class_type': 'VAEEncode', 'inputs': {'pixels': [scale, 0], 'vae': ['3', 0]}}
        graph[pos] = {'class_type': 'ReferenceLatent', 'inputs': {'conditioning': positive, 'latent': [encode, 0]}}
        graph[neg] = {'class_type': 'ReferenceLatent', 'inputs': {'conditioning': negative, 'latent': [encode, 0]}}
        positive, negative = [pos, 0], [neg, 0]
        next_id += 5
    graph['10']['inputs']['positive'] = positive
    graph['10']['inputs']['negative'] = negative
    return graph


def zimage_workflow(prompt, width, height, seed, steps, cfg, prefix, lora=None, lora_strength=1.0):
    """ComfyUI API-format graph for Z-Image base, after ComfyUI's image_z_image_int8 template."""
    graph = {
        '1': {'class_type': 'UNETLoader', 'inputs': {'unet_name': ZIMAGE_UNET, 'weight_dtype': 'default'}},
        '2': {'class_type': 'ModelSamplingAuraFlow', 'inputs': {'model': ['1', 0], 'shift': ZIMAGE_SHIFT}},
        '3': {'class_type': 'CLIPLoader', 'inputs': {'clip_name': KLEIN_CLIP, 'type': 'lumina2', 'device': 'default'}},
        '4': {'class_type': 'VAELoader', 'inputs': {'vae_name': ZIMAGE_VAE}},
        '5': {'class_type': 'CLIPTextEncode', 'inputs': {'text': prompt, 'clip': ['3', 0]}},
        '6': {'class_type': 'CLIPTextEncode', 'inputs': {'text': '', 'clip': ['3', 0]}},
        '7': {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': width, 'height': height, 'batch_size': 1}},
        '8': {'class_type': 'KSampler', 'inputs': {
            'seed': seed, 'steps': steps, 'cfg': cfg, 'sampler_name': 'res_multistep', 'scheduler': 'simple', 'denoise': 1.0,
            'model': ['2', 0], 'positive': ['5', 0], 'negative': ['6', 0], 'latent_image': ['7', 0]}},
        '9': {'class_type': 'VAEDecode', 'inputs': {'samples': ['8', 0], 'vae': ['4', 0]}},
        '10': {'class_type': 'SaveImage', 'inputs': {'images': ['9', 0], 'filename_prefix': prefix}},
    }
    if lora:
        graph['11'] = {'class_type': 'LoraLoaderModelOnly', 'inputs': {'model': ['1', 0], 'lora_name': lora, 'strength_model': lora_strength}}
        graph['2']['inputs']['model'] = ['11', 0]
    return graph


def flux3_workflow(prompt, width, height, seed, steps, cfg, prefix, references=(), lora=None, lora_strength=1.0):
    """ComfyUI API-format graph for FLUX 3 Image Klein base, after ComfyUI's image_flux3_klein_image_edit template.

    References go through one ReferenceMultiLatent node; the prompt names them <ref_image_0>, <ref_image_1> and so on. The
    template's prompt upsampler is left out on purpose (9.7 GB more): the requests carry full captions instead.
    """
    graph = {
        '1': {'class_type': 'UNETLoader', 'inputs': {'unet_name': FLUX3_UNET, 'weight_dtype': 'default'}},
        '2': {'class_type': 'CLIPLoader', 'inputs': {'clip_name': FLUX3_CLIP, 'type': 'flux3', 'device': 'default'}},
        '3': {'class_type': 'VAELoader', 'inputs': {'vae_name': KLEIN_VAE}},
        '4': {'class_type': 'CLIPTextEncode', 'inputs': {'text': prompt, 'clip': ['2', 0]}},
        '5': {'class_type': 'CLIPTextEncode', 'inputs': {'text': '', 'clip': ['2', 0]}},
        '6': {'class_type': 'EmptyFlux2LatentImage', 'inputs': {'width': width, 'height': height, 'batch_size': 1}},
        '7': {'class_type': 'KSampler', 'inputs': {
            'seed': seed, 'steps': steps, 'cfg': cfg, 'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': 1.0,
            'model': ['1', 0], 'positive': ['4', 0], 'negative': ['5', 0], 'latent_image': ['6', 0]}},
        '8': {'class_type': 'VAEDecode', 'inputs': {'samples': ['7', 0], 'vae': ['3', 0]}},
        '9': {'class_type': 'SaveImage', 'inputs': {'images': ['8', 0], 'filename_prefix': prefix}},
    }
    if lora:
        graph['10'] = {'class_type': 'LoraLoaderModelOnly', 'inputs': {'model': ['1', 0], 'lora_name': lora, 'strength_model': lora_strength}}
        graph['7']['inputs']['model'] = ['10', 0]
    if references:
        multi = {'positive': ['4', 0], 'negative': ['5', 0], 'vae': ['3', 0], 'resolution': 'resize', 'resolution.megapixels': 1.0}
        for i, name in enumerate(references):
            graph[str(20 + i)] = {'class_type': 'LoadImage', 'inputs': {'image': name}}
            multi[f'images.image{i}'] = [str(20 + i), 0]
        graph['11'] = {'class_type': 'ReferenceMultiLatent', 'inputs': multi}
        graph['7']['inputs']['positive'] = ['11', 0]
        graph['7']['inputs']['negative'] = ['11', 1]
    return graph


def upscale_workflow(init_name, width, height, prefix):
    """Model upscale (four times), then Lanczos to the exact size. For the private LoRA dataset, not for shipped art."""
    return {
        '1': {'class_type': 'UpscaleModelLoader', 'inputs': {'model_name': UPSCALE_MODEL}},
        '2': {'class_type': 'LoadImage', 'inputs': {'image': init_name}},
        '3': {'class_type': 'ImageUpscaleWithModel', 'inputs': {'upscale_model': ['1', 0], 'image': ['2', 0]}},
        '4': {'class_type': 'ImageScale', 'inputs': {
            'image': ['3', 0], 'upscale_method': 'lanczos', 'width': width, 'height': height, 'crop': 'disabled'}},
        '5': {'class_type': 'SaveImage', 'inputs': {'images': ['4', 0], 'filename_prefix': prefix}},
    }


def build_graph(req, seed, reference_names=(), init_name=None, denoise=1.0):
    """The one place a request becomes a graph. Reference and LoRA support is by engine; asking the wrong one is an error."""
    engine_name = req.get('engine', 'schnell')
    if engine_name not in ENGINES:
        raise ValueError(f'{req["name"]}: unknown engine {engine_name!r}; the engines are {", ".join(ENGINES)}')
    engine = ENGINES[engine_name]
    prefix = f'phobos/{req["name"]}'
    steps = req.get('steps', engine['steps'])
    cfg = float(req.get('cfg', engine['cfg']))
    lora, strength = req.get('lora'), float(req.get('lora_strength', 1.0))
    if engine_name == 'klein':
        return klein_workflow(req['prompt'], req['width'], req['height'], seed, steps, prefix, reference_names, lora, strength)
    if engine_name == 'klein-base':
        return klein_workflow(req['prompt'], req['width'], req['height'], seed, steps, prefix, reference_names, lora, strength,
                              unet=KLEIN_BASE_UNET, cfg=cfg)
    if engine_name == 'zimage':
        if reference_names:
            raise ValueError(f'{req["name"]}: Z-Image takes no reference images; use klein-base')
        return zimage_workflow(req['prompt'], req['width'], req['height'], seed, steps, cfg, prefix, lora, strength)
    if engine_name == 'flux3-base':
        return flux3_workflow(req['prompt'], req['width'], req['height'], seed, steps, cfg, prefix, reference_names, lora, strength)
    if engine_name == 'upscale':
        if not init_name:
            raise ValueError(f'{req["name"]}: the upscale engine needs an init image')
        return upscale_workflow(init_name, req['width'], req['height'], prefix)
    return workflow(req['prompt'], req['width'], req['height'], seed, steps, req.get('model', DEFAULT_MODEL), prefix, init_name, denoise)


def validate_graph(graph, object_info):
    """Problems a graph would hit, from the server's own node list, before any GPU time is spent: unknown nodes, missing
    required inputs, and fixed choices (a model file, sampler or type) the server does not offer. Images are left out,
    because reference files are copied in only when their batch starts."""
    problems = []
    for node_id, node in graph.items():
        kind = node['class_type']
        info = object_info.get(kind)
        if info is None:
            problems.append(f'node {node_id}: ComfyUI has no {kind}')
            continue
        declared = info.get('input', {})
        for name, spec in declared.get('required', {}).items():
            if name not in node['inputs'] and not _is_dynamic(spec):
                problems.append(f'node {node_id} ({kind}): missing input {name}')
        for section in ('required', 'optional'):
            for name, spec in declared.get(section, {}).items():
                value = node['inputs'].get(name)
                options = _options_of(spec)
                if value is None or isinstance(value, list) or options is None or name == 'image':
                    continue
                if value not in options:
                    problems.append(f'node {node_id} ({kind}): {name} = {value!r} is not offered (is the file installed?)')
    return problems


def _options_of(spec):
    """The list of fixed choices in an object_info input, in either of the server's two spellings; None if it has none."""
    if not spec:
        return None
    if isinstance(spec[0], list):
        return spec[0]
    if spec[0] == 'COMBO' and len(spec) > 1 and isinstance(spec[1], dict):
        return spec[1].get('options')
    return None


def _is_dynamic(spec):
    """Dynamic inputs (ReferenceMultiLatent's images) are spelled out as name.part keys, so the bare name is never present."""
    return bool(spec) and isinstance(spec[0], str) and spec[0].startswith('COMFY_')


def display_path(path):
    """Relative to the repository when inside it, so records carry no machine paths; private experiment files are outside."""
    path = Path(path).resolve()
    return str(path.relative_to(ROOT)).replace('\\', '/') if path.is_relative_to(ROOT) else str(path).replace('\\', '/')


def stage_init(comfy_path, init_path, name):
    """Copy a starting image into ComfyUI's input folder; returns the name ComfyUI knows it by."""
    source = Path(init_path)
    if not source.is_absolute():
        source = ROOT / source
    target = Path(comfy_path) / 'ComfyUI' / 'input' / f'phobos-init-{name}.png'
    target.parent.mkdir(parents=True, exist_ok=True)
    # Transparent sprites must be flattened onto white first, or the model paints the alpha as black.
    flatten = (
        "import sys; from PIL import Image; im = Image.open(sys.argv[1]).convert('RGBA'); "
        "bg = Image.new('RGBA', im.size, (255, 255, 255, 255)); bg.alpha_composite(im); bg.convert('RGB').save(sys.argv[2])")
    python = Path(comfy_path) / 'python_embeded' / 'python.exe'
    subprocess.run([str(python), '-c', flatten, str(source), str(target)], check=True)
    return target.name, source


def get_json(path):
    with urllib.request.urlopen(SERVER + path, timeout=30) as response:
        return json.load(response)


def post_json(path, payload):
    data = json.dumps(payload).encode('utf-8')
    request = urllib.request.Request(SERVER + path, data=data, headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=30) as response:
        body = response.read()
    return json.loads(body) if body else {}


def server_up():
    try:
        return 'comfyui_version' in get_json('/system_stats').get('system', {})
    except (urllib.error.URLError, OSError, ValueError):
        return False


def start_server(comfy_path, extra_args=()):
    """Start ComfyUI and wait for it; returns the process, so a run that started it can also stop it."""
    exe = Path(comfy_path) / 'python_embeded' / 'python.exe'
    main_py = Path(comfy_path) / 'ComfyUI' / 'main.py'
    if not exe.is_file() or not main_py.is_file():
        raise FileNotFoundError(f'ComfyUI portable not found under {comfy_path}')
    log = Path(comfy_path) / 'comfyui-server.log'
    print(f'starting ComfyUI; log in {log}')
    with open(log, 'ab') as fh:
        process = subprocess.Popen([str(exe), '-s', str(main_py), '--windows-standalone-build', '--listen', '127.0.0.1', '--port', '8188', *extra_args],
                                   cwd=str(Path(comfy_path) / 'ComfyUI'), stdout=fh, stderr=subprocess.STDOUT)
    for _ in range(120):
        if server_up():
            return process
        time.sleep(2)
    raise RuntimeError('ComfyUI did not come up within four minutes; read the log')


def sha256_of(path, cache):
    path = Path(path)
    key = str(path)
    if key in cache:
        return cache[key]
    digest = hashlib.sha256()
    with open(path, 'rb') as fh:
        for block in iter(lambda: fh.read(1 << 20), b''):
            digest.update(block)
    cache[key] = digest.hexdigest()
    return cache[key]


def interrupt_server():
    """Stop the image in progress and empty the queue."""
    post_json('/queue', {'clear': True})
    post_json('/interrupt', {})


def _cancel_stuck_image():
    """An image past its timeout is still running, and would hold up every later one. A dead server needs no cancelling."""
    try:
        interrupt_server()
    except (urllib.error.URLError, OSError, ValueError):
        pass


def wait_for(prompt_id, timeout=600, should_stop=None):
    deadline = time.time() + timeout
    while time.time() < deadline:
        if should_stop:
            reason = should_stop()
            if reason:
                interrupt_server()
                raise Stopped(reason)
        history = get_json(f'/history/{prompt_id}')
        if prompt_id in history:
            entry = history[prompt_id]
            status = entry.get('status', {})
            if status.get('status_str') == 'error':
                raise RuntimeError(f'ComfyUI reported an error: {json.dumps(status)[:500]}')
            return entry['outputs']
        time.sleep(2)
    raise TimeoutError('image did not finish in time')


def fetch_image(info):
    query = urllib.parse.urlencode({'filename': info['filename'], 'subfolder': info.get('subfolder', ''), 'type': info.get('type', 'output')})
    with urllib.request.urlopen(f'{SERVER}/view?{query}', timeout=60) as response:
        return response.read()


def output_root(request_file):
    return Path(request_file).resolve().parent.parent / 'generated'


def plan(request_file, only=None):
    """Every image a request file asks for, as (request, seed, target path); touches neither the server nor the GPU."""
    requests = json.loads(Path(request_file).read_text(encoding='utf-8'))
    out_root = output_root(request_file)
    return [(req, seed, out_root / req['name'] / f'{req["name"]}-{seed}.png')
            for req in requests if not only or req['name'] == only for seed in req['seeds']]


def preview_graphs(request_file, only=None):
    """One graph per request, built with the names the references will have, for validate_graph."""
    graphs = {}
    for req, seed, _ in plan(request_file, only):
        if req['name'] in graphs:
            continue
        references = [f'phobos-init-{req["name"]}-ref{i + 1}.png' for i in range(len(req.get('references', [])))]
        init = f'phobos-init-{req["name"]}.png' if req.get('init') else None
        graphs[req['name']] = build_graph(req, seed, references, init, float(req.get('denoise', 0.7)))
    return graphs


def should_restart(server_alive, timed_out, consecutive):
    """After a failed image, is a fresh server worth trying? Yes if it died or hung, or on the second failure in a row, which
    is how a card left short of memory looks. One failure with a healthy server is just that image, so the run moves on."""
    return not server_alive or timed_out or consecutive >= 2


def make_record(req, engine_name, target, data, version, model, model_hash, seed, request_file, init_source, denoise, reference_sources, seconds=None):
    engine = ENGINES[engine_name]
    record = {
        'file': target.name,
        'sha256': hashlib.sha256(data).hexdigest(),
        'date': _dt.datetime.now().isoformat(timespec='seconds'),
        'generator': f'ComfyUI {version}, local, {engine["label"]}',
        'engine': engine_name,
        'model': model,
        'model_sha256': model_hash,
        'prompt': req.get('prompt', ''),
        'negative_prompt': NEGATIVE,
        'seed': seed,
        'steps': req.get('steps', engine['steps']),
        'cfg': float(req.get('cfg', engine['cfg'])),
        'sampler': engine['sampler'],
        'size': [req['width'], req['height']],
        'request_file': display_path(request_file),
    }
    if seconds is not None:
        record['seconds'] = seconds
    if init_source is not None:
        record['init_image'] = display_path(init_source)
        record['init_sha256'] = hashlib.sha256(init_source.read_bytes()).hexdigest()
        record['denoise'] = denoise
    if reference_sources:
        record['references'] = [{'image': display_path(s), 'sha256': hashlib.sha256(s.read_bytes()).hexdigest()} for s in reference_sources]
    if req.get('lora'):
        record['lora'] = req['lora']
        record['lora_strength'] = float(req.get('lora_strength', 1.0))
    return record


def run(request_file, only=None, comfy_path=DEFAULT_COMFY, start=False, should_stop=None, keep_going=False, restart_server=None):
    """Make every missing image. should_stop returns a reason to end the run, checked before and during each image.
    keep_going records a failed image and moves on, for unattended runs; several failures in a row still end the run.
    restart_server, if given, returns whether it brought up a fresh server, and the image that failed is then tried again.
    Returns made, failures, restarts, timings, the folders touched and the stop reason, if any."""
    requests = json.loads(Path(request_file).read_text(encoding='utf-8'))
    out_root = output_root(request_file)
    if not server_up():
        if not start:
            raise RuntimeError(f'ComfyUI is not running at {SERVER}; pass --start or start it yourself')
        start_server(comfy_path)
    version = get_json('/system_stats')['system'].get('comfyui_version', 'unknown')
    models_dir = Path(comfy_path) / 'ComfyUI' / 'models'
    hashes = {}
    result = {'made': 0, 'skipped': 0, 'failures': [], 'restarts': [], 'timings': [], 'folders': [], 'stopped': None}
    consecutive = 0
    try:
        for req in requests:
            if only and req['name'] != only:
                continue
            engine_name = req.get('engine', 'schnell')
            engine = ENGINES[engine_name]
            model = req.get('model', engine['model']) if engine_name == 'schnell' else engine['model']
            model_path = models_dir / engine['folder'] / model
            model_hash = sha256_of(model_path, hashes) if model_path.is_file() else 'unknown (model not in the local folder)'
            folder = out_root / req['name']
            folder.mkdir(parents=True, exist_ok=True)
            result['folders'].append(req['name'])
            init_name, init_source, denoise = None, None, 1.0
            if req.get('init'):
                init_name, init_source = stage_init(comfy_path, req['init'], req['name'])
                denoise = float(req.get('denoise', 0.7))
            reference_names, reference_sources = [], []
            for i, ref in enumerate(req.get('references', [])):
                name, source = stage_init(comfy_path, ref, f'{req["name"]}-ref{i + 1}')
                reference_names.append(name)
                reference_sources.append(source)
            for seed in req['seeds']:
                target = folder / f'{req["name"]}-{seed}.png'
                if target.exists():
                    print(f'  {target.name} exists, skipped')
                    result['skipped'] += 1
                    continue
                data = None
                while data is None:
                    if should_stop:
                        reason = should_stop()
                        if reason:
                            raise Stopped(reason)
                    began = time.monotonic()
                    try:
                        graph = build_graph(req, seed, reference_names, init_name, denoise)
                        queued = post_json('/prompt', {'prompt': graph, 'client_id': str(uuid.uuid4())})
                        print(f'  {req["name"]} seed {seed}: queued {queued["prompt_id"]}', flush=True)
                        outputs = wait_for(queued['prompt_id'], req.get('timeout', engine['timeout']), should_stop)
                        images = [img for node in outputs.values() for img in node.get('images', [])]
                        if not images:
                            raise RuntimeError('no image came back')
                        data = fetch_image(images[0])
                    except (RuntimeError, TimeoutError, ValueError, urllib.error.URLError, OSError) as error:
                        timed_out = isinstance(error, TimeoutError)
                        if timed_out:
                            _cancel_stuck_image()
                        if not keep_going:
                            raise
                        consecutive += 1
                        print(f'  {target.name} FAILED: {error}', flush=True)
                        if consecutive >= CONSECUTIVE_FAILURES_TO_GIVE_UP:
                            result['failures'].append({'image': target.name, 'error': str(error)[:500]})
                            raise Stopped(f'{consecutive} images failed in a row; the last error was: {str(error)[:300]}') from error
                        if (restart_server and len(result['restarts']) < MAX_SERVER_RESTARTS
                                and should_restart(server_up(), timed_out, consecutive) and restart_server()):
                            result['restarts'].append(f'at {target.name}, after: {str(error)[:200]}')
                            print('  started a fresh ComfyUI; trying that image again', flush=True)
                            continue
                        result['failures'].append({'image': target.name, 'error': str(error)[:500]})
                        break
                if data is None:
                    continue
                consecutive = 0
                seconds = round(time.monotonic() - began, 1)
                target.write_bytes(data)
                record = make_record(req, engine_name, target, data, version, model, model_hash, seed, request_file,
                                     init_source, denoise, reference_sources, seconds)
                target.with_suffix('.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
                result['timings'].append({'request': req['name'], 'engine': engine_name, 'steps': req.get('steps', engine['steps']), 'seconds': seconds})
                result['made'] += 1
                print(f'  wrote {display_path(target)}', flush=True)
    except Stopped as stop:
        result['stopped'] = str(stop)
        print(f'stopped: {stop}', flush=True)
    print(f'{result["made"]} images made')
    return result


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('request_file')
    parser.add_argument('--only', help='run only the request with this name')
    parser.add_argument('--start', action='store_true', help='start ComfyUI if it is not running')
    parser.add_argument('--comfy', default=str(DEFAULT_COMFY), help='ComfyUI portable folder')
    args = parser.parse_args(argv)
    run(args.request_file, args.only, Path(args.comfy), args.start)
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (RuntimeError, FileNotFoundError, TimeoutError, ValueError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
