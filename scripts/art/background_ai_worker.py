"""Find what is background in each picture with BiRefNet (Zheng Peng and colleagues, MIT licence; ComfyUI's single-file copy), and keep
the answer as a mask: white where the picture's object is, black where it is background. Runs inside ComfyUI's bundled Python, which has
torch; scripts/art/clean_backgrounds.py starts it with the safeguards and never needs torch itself.

    <ComfyUI python> -I scripts/art/background_ai_worker.py --comfy PORTABLE --list LIST.json --cache DIR --device cpu --threads 8 --cores 16-31 --stop-at ISO

On the processor the graphics card is hidden before torch loads, and the run refuses to start if it can still see one (12 Nov 2026: an
empty CUDA_VISIBLE_DEVICES does not hide it on Windows; -1 does). On the graphics card it refuses to start, and stops between pictures,
while the game runs. Each mask is named by the SHA-256 of the picture's file, so a picture already done is never done again.
"""
import argparse
import datetime as _dt
import hashlib
import json
import os
import subprocess
import sys
import time
from pathlib import Path

MODEL = 'birefnet.safetensors'
GAME_PROCESSES = ('rimworld.exe', 'rimworldwin64.exe')


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def game_running():
    try:
        listing = subprocess.run(['tasklist', '/FO', 'CSV', '/NH'], capture_output=True, text=True, timeout=30, check=True).stdout.lower()
    except (OSError, subprocess.SubprocessError):
        return True  # Unreadable: assume the game may be running, which stops a graphics card run.
    return any(f'"{name}"' in listing for name in GAME_PROCESSES)


def parse_cores(text):
    cores = []
    for part in text.split(','):
        low, _, high = part.partition('-')
        cores += list(range(int(low), int(high or low) + 1))
    return cores


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--comfy', required=True, help="the ComfyUI portable folder (clean_backgrounds.py passes the art tools' own)")
    parser.add_argument('--list', required=True, help='a JSON list of picture paths')
    parser.add_argument('--cache', required=True)
    parser.add_argument('--device', choices=('cpu', 'gpu'), required=True)
    parser.add_argument('--threads', type=int, default=8)
    parser.add_argument('--cores', help='processor run: the logical processors to keep to, such as 16-31')
    parser.add_argument('--stop-at', required=True, help='ISO date and time')
    args = parser.parse_args()
    stop_at = _dt.datetime.fromisoformat(args.stop_at)
    comfy_root = Path(args.comfy) / 'ComfyUI'

    if args.device == 'cpu':
        os.environ['CUDA_VISIBLE_DEVICES'] = '-1'
        os.environ['OMP_NUM_THREADS'] = os.environ['MKL_NUM_THREADS'] = str(args.threads)
    elif game_running():
        print('refused: the game is running, and this run would use the graphics card', flush=True)
        return 3

    import psutil
    process = psutil.Process()
    if args.device == 'cpu':
        process.nice(psutil.BELOW_NORMAL_PRIORITY_CLASS)
        if args.cores:
            process.cpu_affinity(parse_cores(args.cores))

    sys.argv = [sys.argv[0]] + (['--cpu'] if args.device == 'cpu' else [])
    sys.path.insert(0, str(comfy_root))
    import comfy.options
    comfy.options.enable_args_parsing()
    import numpy as np
    import torch
    from PIL import Image
    if args.device == 'cpu':
        if torch.cuda.is_available():
            print('refused: a graphics card is still visible to a processor run', flush=True)
            return 3
        torch.set_num_threads(args.threads)
    import comfy.bg_removal_model as bg
    import comfy.model_management as management

    pictures = [Path(p) for p in json.loads(Path(args.list).read_text(encoding='utf-8'))]
    cache = Path(args.cache)
    cache.mkdir(parents=True, exist_ok=True)
    todo = [(p, sha256(p)) for p in pictures if p.is_file()]
    todo = [(p, h) for p, h in todo if not (cache / f'{h}.png').is_file()]
    print(f'{len(todo)} pictures to look at, {len(pictures) - len(todo)} already done or missing', flush=True)
    if not todo:
        return 0

    started = time.time()
    model = bg.load(str(comfy_root / 'models' / 'background_removal' / MODEL))
    if args.device == 'cpu' and str(model.load_device) != 'cpu':
        print(f'refused: the model loaded on {model.load_device}, not the processor', flush=True)
        return 3
    print(f'model loaded on {model.load_device} in {time.time() - started:.0f} s', flush=True)
    model_sha = sha256(comfy_root / 'models' / 'background_removal' / MODEL)

    done = 0
    try:
        for path, digest in todo:
            if _dt.datetime.now() >= stop_at:
                print('stopped: the stop time was reached', flush=True)
                break
            if args.device == 'gpu' and game_running():
                print('stopped: the game started', flush=True)
                break
            began = time.time()
            image = Image.open(path).convert('RGBA')
            flat = Image.new('RGBA', image.size, (255, 255, 255, 255))
            flat.alpha_composite(image)
            pixels = torch.from_numpy(np.asarray(flat.convert('RGB')).astype('float32') / 255.0)[None]
            with torch.no_grad():
                mask = model.encode_image(pixels)[0].float().cpu().numpy()
            Image.fromarray((mask * 255).clip(0, 255).round().astype('uint8'), 'L').save(cache / f'{digest}.png')
            (cache / f'{digest}.json').write_text(json.dumps({
                'picture': path.as_posix(), 'picture_sha256': digest, 'model': MODEL, 'model_sha256': model_sha,
                'device': args.device, 'seconds': round(time.time() - began, 1), 'date': _dt.datetime.now().isoformat(timespec='seconds'),
                'meaning': 'white where the picture shows its object, black where it shows background'}, indent=2), encoding='utf-8')
            done += 1
            print(f'{done}/{len(todo)} {path.name} {time.time() - began:.1f} s', flush=True)
    finally:
        del model
        if args.device == 'gpu':
            management.unload_all_models()
            management.soft_empty_cache()
    print(f'finished: {done} masks in {(time.time() - started) / 60:.1f} min', flush=True)
    return 0


if __name__ == '__main__':
    sys.exit(main())
