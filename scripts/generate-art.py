"""Generate artwork candidates through a local ComfyUI server and record how each was made.

Reads a request file (JSON: a list of requests with a name, a prompt, sizes and seeds), submits a
Flux.1-schnell workflow for every seed, waits for the images and saves them with a record beside each
one: prompt, seed, steps, size, model file and its SHA-256, server version and date. Nothing is sent
anywhere but the local server. Needs ComfyUI running (python scripts/generate-art.py --start starts it
from COMFYUI_PATH or C:/AI/ComfyUI/ComfyUI_windows_portable).

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


def workflow(prompt, width, height, seed, steps, model, prefix):
    """ComfyUI API-format graph for Flux.1-schnell packaged as a single checkpoint."""
    return {
        '1': {'class_type': 'CheckpointLoaderSimple', 'inputs': {'ckpt_name': model}},
        '2': {'class_type': 'CLIPTextEncode', 'inputs': {'text': prompt, 'clip': ['1', 1]}},
        '3': {'class_type': 'CLIPTextEncode', 'inputs': {'text': NEGATIVE, 'clip': ['1', 1]}},
        '4': {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': width, 'height': height, 'batch_size': 1}},
        '5': {'class_type': 'KSampler', 'inputs': {
            'seed': seed, 'steps': steps, 'cfg': 1.0, 'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': 1.0,
            'model': ['1', 0], 'positive': ['2', 0], 'negative': ['3', 0], 'latent_image': ['4', 0]}},
        '6': {'class_type': 'VAEDecode', 'inputs': {'samples': ['5', 0], 'vae': ['1', 2]}},
        '7': {'class_type': 'SaveImage', 'inputs': {'images': ['6', 0], 'filename_prefix': prefix}},
    }


def get_json(path):
    with urllib.request.urlopen(SERVER + path, timeout=30) as response:
        return json.load(response)


def post_json(path, payload):
    data = json.dumps(payload).encode('utf-8')
    request = urllib.request.Request(SERVER + path, data=data, headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def server_up():
    try:
        return 'comfyui_version' in get_json('/system_stats').get('system', {})
    except (urllib.error.URLError, OSError, ValueError):
        return False


def start_server(comfy_path):
    exe = Path(comfy_path) / 'python_embeded' / 'python.exe'
    main = Path(comfy_path) / 'ComfyUI' / 'main.py'
    if not exe.is_file() or not main.is_file():
        raise FileNotFoundError(f'ComfyUI portable not found under {comfy_path}')
    log = Path(comfy_path) / 'comfyui-server.log'
    print(f'starting ComfyUI; log in {log}')
    with open(log, 'ab') as fh:
        subprocess.Popen([str(exe), '-s', str(main), '--windows-standalone-build', '--listen', '127.0.0.1', '--port', '8188'],
                         cwd=str(Path(comfy_path) / 'ComfyUI'), stdout=fh, stderr=subprocess.STDOUT)
    for _ in range(120):
        if server_up():
            return
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


def wait_for(prompt_id, timeout=600):
    deadline = time.time() + timeout
    while time.time() < deadline:
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


def run(request_file, only=None, comfy_path=DEFAULT_COMFY, start=False):
    requests = json.loads(Path(request_file).read_text(encoding='utf-8'))
    out_root = Path(request_file).resolve().parent.parent / 'generated'
    if not server_up():
        if not start:
            raise RuntimeError(f'ComfyUI is not running at {SERVER}; pass --start or start it yourself')
        start_server(comfy_path)
    stats = get_json('/system_stats')
    version = stats['system'].get('comfyui_version', 'unknown')
    model_dir = Path(comfy_path) / 'ComfyUI' / 'models' / 'checkpoints'
    hashes = {}
    made = 0
    for req in requests:
        if only and req['name'] != only:
            continue
        model = req.get('model', DEFAULT_MODEL)
        model_path = model_dir / model
        model_hash = sha256_of(model_path, hashes) if model_path.is_file() else 'unknown (model not under the local checkpoints folder)'
        folder = out_root / req['name']
        folder.mkdir(parents=True, exist_ok=True)
        for seed in req['seeds']:
            target = folder / f'{req["name"]}-{seed}.png'
            if target.exists():
                print(f'  {target.name} exists, skipped')
                continue
            graph = workflow(req['prompt'], req['width'], req['height'], seed, req.get('steps', 4), model, f'phobos/{req["name"]}')
            queued = post_json('/prompt', {'prompt': graph, 'client_id': str(uuid.uuid4())})
            print(f'  {req["name"]} seed {seed}: queued {queued["prompt_id"]}')
            outputs = wait_for(queued['prompt_id'])
            images = [img for node in outputs.values() for img in node.get('images', [])]
            if not images:
                raise RuntimeError('no image came back')
            data = fetch_image(images[0])
            target.write_bytes(data)
            record = {
                'file': target.name,
                'sha256': hashlib.sha256(data).hexdigest(),
                'date': _dt.datetime.now().isoformat(timespec='seconds'),
                'generator': f'ComfyUI {version}, local, Flux.1-schnell fp8 (Comfy-Org packaging, Apache 2.0)',
                'model': model,
                'model_sha256': model_hash,
                'prompt': req['prompt'],
                'negative_prompt': NEGATIVE,
                'seed': seed,
                'steps': req.get('steps', 4),
                'cfg': 1.0,
                'sampler': 'euler/simple',
                'size': [req['width'], req['height']],
                'request_file': str(Path(request_file).resolve().relative_to(ROOT)).replace('\\', '/'),
            }
            target.with_suffix('.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
            made += 1
            print(f'  wrote {target.relative_to(ROOT)}')
    print(f'{made} images made')
    return made


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
    except (RuntimeError, FileNotFoundError, TimeoutError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
