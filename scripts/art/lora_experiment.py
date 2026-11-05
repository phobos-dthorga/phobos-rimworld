"""Prepare a private LoRA experiment: upscale a folder of images, caption them, write the training config, then compare routes.

Everything goes in a work folder outside the repository and outside the synced drive (default C:/AI/lora-experiments/<name>), so
a dataset drawn from game art is never committed or shared. Generic: it knows nothing about where the images came from. Run the
GPU steps through scripts/art-run.py, which holds the safeguards.

    python scripts/art/lora_experiment.py requests .local/references/vanilla-training --name vanilla
    python scripts/art-run.py generate C:/AI/lora-experiments/vanilla/upscale/requests/upscale.json --stop-at 07:00
    python scripts/art/lora_experiment.py assemble .local/references/vanilla-training --name vanilla --trigger phobosvanilla
    python scripts/art-run.py train C:/AI/lora-experiments/vanilla/config.yaml --stop-at 07:00
    python scripts/art/lora_experiment.py compare assets/phobos-medical/requests/first-batch.json --name vanilla --trigger phobosvanilla
    python scripts/art-run.py generate C:/AI/lora-experiments/vanilla/requests/compare.json --stop-at 07:00 --near-copy-against C:/AI/lora-experiments/vanilla/dataset

The upscales have their own folder because they are copies on purpose: beside the candidates they would trip the near-copy check.
"""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import struct

WORK_ROOT = Path(r'C:\AI\lora-experiments')
COMFY = Path(os.environ.get('COMFYUI_PATH', r'C:\AI\ComfyUI\ComfyUI_windows_portable'))
LONG_SIDE = 1024
LATENT_STEP = 16  # Image sides must be whole multiples of this for these models.
# Starting values for a 12 GB card. Black Forest Labs' training guide gives 8e-5 to 1e-4 for the learning rate; the rest are
# ai-toolkit's example values, with sampling off and the text encoder's output cached to keep within the card. All authored.
TRAINING = {'steps': 2000, 'lr': 1e-4, 'linear': 16, 'save_every': 250, 'keep': 4, 'resolutions': [512, 768]}
ROUTE_SEEDS = (11, 12)


def fit_long_side(width, height, long_side=LONG_SIDE, step=LATENT_STEP):
    """Scale so the long side is long_side, keeping the shape, with both sides on the step and never below one step."""
    scale = long_side / max(width, height)
    return (max(step, round(width * scale / step) * step), max(step, round(height * scale / step) * step))


def caption_for(stem, trigger):
    """The trigger word, then the subject in plain words from the file name. The style is left out on purpose: the model
    should attach what the captions do not describe to the trigger word."""
    words = re.sub(r'[_\-]+', ' ', stem)
    words = re.sub(r'(?<=[a-z0-9])(?=[A-Z])', ' ', words)
    words = re.sub(r'\b[a-e]\b$', '', words.strip()).strip()
    words = re.sub(r'\b(south|north|east|west)\b', '', words).strip()
    return f'{trigger}, {re.sub(r" +", " ", words).lower()}'


def png_size(path):
    with open(path, 'rb') as fh:
        header = fh.read(24)
    if header[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError(f'{path} is not a PNG')
    return struct.unpack('>II', header[16:24])


def training_config(name, dataset, output, trigger, settings=None):
    """An ai-toolkit job, as a dict. It is written out as JSON, which YAML readers accept, so no YAML library is needed."""
    values = {**TRAINING, **(settings or {})}
    return {
        'job': 'extension',
        'config': {
            'name': f'phobos_{name}',
            'process': [{
                'type': 'sd_trainer',
                'training_folder': str(output).replace('\\', '/'),
                'device': 'cuda:0',
                'trigger_word': trigger,
                'network': {'type': 'lora', 'linear': values['linear'], 'linear_alpha': values['linear']},
                'save': {'dtype': 'float16', 'save_every': values['save_every'], 'max_step_saves_to_keep': values['keep'], 'push_to_hub': False},
                'datasets': [{
                    'folder_path': str(dataset).replace('\\', '/'), 'caption_ext': 'txt', 'caption_dropout_rate': 0.05,
                    'shuffle_tokens': False, 'cache_latents_to_disk': True, 'resolution': values['resolutions']}],
                'train': {
                    'batch_size': 1, 'steps': values['steps'], 'gradient_accumulation_steps': 1, 'train_unet': True, 'train_text_encoder': False,
                    'gradient_checkpointing': True, 'noise_scheduler': 'flowmatch', 'optimizer': 'adamw8bit', 'lr': values['lr'], 'dtype': 'bf16',
                    'cache_text_embeddings': True, 'unload_text_encoder': False, 'disable_sampling': True},
                'model': {
                    'name_or_path': 'black-forest-labs/FLUX.2-klein-base-4B', 'arch': 'flux2_klein_4b',
                    'quantize': True, 'quantize_te': True, 'low_vram': True,
                    # float8, because moving the default qfloat8 text encoder to the card a second time crashes.
                    'qtype': 'float8', 'qtype_te': 'float8'},
                'sample': {'sampler': 'flowmatch', 'sample_every': 100000, 'width': 1024, 'height': 1024, 'prompts': [f'{trigger}, a small machine'],
                           'neg': '', 'seed': 42, 'guidance_scale': 4, 'sample_steps': 30},
            }],
        },
        'meta': {'name': f'phobos_{name}', 'version': '1.0'},
    }


def route_requests(requests, trigger, lora, seeds=ROUTE_SEEDS):
    """The same subjects through three routes, for a fair comparison: references only, LoRA only, both."""
    out = []
    for req in requests:
        if req.get('engine') != 'klein-base' or not req.get('references') or req.get('lora_comparison') is False:
            continue
        base = {key: req[key] for key in ('engine', 'width', 'height', 'prompt', 'references') if key in req}
        no_references = {key: value for key, value in base.items() if key != 'references'}
        out.append({**base, 'name': f'{req["name"]}-references', 'seeds': list(seeds)})
        out.append({**no_references, 'name': f'{req["name"]}-lora', 'seeds': list(seeds),
                    'prompt': f'{trigger}, {req.get("subject", req["prompt"])}', 'lora': lora})
        out.append({**base, 'name': f'{req["name"]}-both', 'seeds': list(seeds), 'prompt': f'{trigger}, {req["prompt"]}', 'lora': lora})
    return out


def write_requests(source, work):
    requests = []
    for path in sorted(Path(source).glob('*.png')):
        width, height = fit_long_side(*png_size(path))
        requests.append({'name': path.stem, 'engine': 'upscale', 'init': str(path.resolve()), 'width': width, 'height': height, 'seeds': [0]})
    target = work / 'upscale' / 'requests' / 'upscale.json'
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(requests, indent=2), encoding='utf-8')
    print(f'{len(requests)} upscale requests in {target}')


def assemble(source, work, name, trigger):
    dataset = work / 'dataset'
    dataset.mkdir(parents=True, exist_ok=True)
    count = 0
    for path in sorted(Path(source).glob('*.png')):
        upscaled = work / 'upscale' / 'generated' / path.stem / f'{path.stem}-0.png'
        if not upscaled.is_file():
            print(f'not upscaled yet, left out: {path.stem}')
            continue
        shutil.copyfile(upscaled, dataset / f'{path.stem}.png')
        (dataset / f'{path.stem}.txt').write_text(caption_for(path.stem, trigger), encoding='utf-8')
        count += 1
    config = training_config(name, dataset, work / 'output', trigger)
    (work / 'config.yaml').write_text(json.dumps(config, indent=2), encoding='utf-8')
    print(f'{count} images and captions in {dataset}; config in {work / "config.yaml"}')


def compare(request_file, work, name, trigger):
    """Copy the newest LoRA where ComfyUI finds it and write the three-route comparison."""
    outputs = sorted((work / 'output').rglob('*.safetensors'), key=lambda p: p.stat().st_mtime)
    if not outputs:
        raise SystemExit(f'no trained LoRA under {work / "output"} yet')
    lora = f'phobos-private-{name}.safetensors'
    shutil.copyfile(outputs[-1], COMFY / 'ComfyUI' / 'models' / 'loras' / lora)
    requests = route_requests(json.loads(Path(request_file).read_text(encoding='utf-8')), trigger, lora)
    target = work / 'requests' / 'compare.json'
    target.write_text(json.dumps(requests, indent=2), encoding='utf-8')
    print(f'{len(requests)} comparison requests in {target}, using {outputs[-1].name}')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('step', choices=('requests', 'assemble', 'compare'))
    parser.add_argument('source', help='a folder of PNGs; for compare, a request file')
    parser.add_argument('--name', required=True)
    parser.add_argument('--trigger', default='phobosstyle')
    args = parser.parse_args(argv)
    work = WORK_ROOT / args.name
    if args.step == 'requests':
        write_requests(args.source, work)
    elif args.step == 'assemble':
        assemble(args.source, work, args.name, args.trigger)
    else:
        compare(args.source, work, args.name, args.trigger)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
