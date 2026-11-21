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

A second training from the same images, with other settings, then installing its newest save where ComfyUI finds it:

    python scripts/art/lora_experiment.py retrain vanilla --name vanilla-v2 --trigger phobosvanilla --steps 3000 --resolutions 768,1024
    python scripts/art-run.py train C:/AI/lora-experiments/vanilla-v2/config.yaml --stop-at 07:00
    python scripts/art/lora_experiment.py install - --name vanilla-v2
A sharper dataset (v3): enlarge pixel-exact instead of the AI upscale, add the fixed view phrase to every caption, then set the new
model beside the earlier ones' images on the same subjects and seeds:
    python scripts/art/lora_experiment.py assemble .local/references/vanilla-training-v3 --name vanilla-v3 --trigger phobosvanilla --enlarge nearest --caption-suffix "seen from directly above, south face hinted" --steps 3000 --resolutions 768,1024
    python scripts/art/lora_experiment.py compare-models assets/phobos-medical/requests/first-batch.json --name vanilla-v3 --trigger phobosvanilla --loras phobos-private-vanilla-v3.safetensors

A style model of our own art (owner's choice, 20 Nov 2026): every picture the owner picked from the local model, read from the
art-masters branch with its generation record and captioned with its request's subject, never from game art:

    python scripts/art/lora_experiment.py picks origin/art-masters --name house-v1 --trigger phobosdthorga --steps 3000 --resolutions 768,1024 --low-vram off --tag "vehicles/dashboard/*=flat solid black silhouette, no shading"

With --low-vram off it also writes config-lowvram.yaml, the same training with the text encoder squeezed for a small card, as the
fallback step of a night plan: ai-toolkit resumes from the first one's saves.

The upscales have their own folder because they are copies on purpose: beside the candidates they would trip the near-copy check.
"""
import argparse
import fnmatch
import json
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess

WORK_ROOT = Path(r'C:\AI\lora-experiments')
REPO = Path(__file__).resolve().parents[2]
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
                    'quantize': True, 'quantize_te': True, 'low_vram': values.get('low_vram', True),
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


ENLARGE = """
import math, sys
from pathlib import Path
from PIL import Image
for line in Path(sys.argv[1]).read_text(encoding='utf-8').splitlines():
    source, target, width, height = line.split('|')
    image = Image.open(source).convert('RGBA')
    factor = max(1, math.ceil(max(int(width), int(height)) / max(image.size)))
    image = image.resize((image.width * factor, image.height * factor), Image.NEAREST)
    image.resize((int(width), int(height)), Image.LANCZOS).save(target)
"""


def enlarge_nearest(sources, dataset):
    """The sharper upscale for v3 (owner, 7 Nov 2026): each sprite enlarged pixel-exact by a whole factor, then brought to the training
    size with one smoothing step, so the outlines stay crisp instead of the AI upscaler's softness. Runs under ComfyUI's Python, which
    has Pillow. Returns the stems written."""
    import subprocess
    import tempfile
    lines, stems = [], []
    for path in sources:
        width, height = fit_long_side(*png_size(path))
        lines.append(f'{path.resolve()}|{(dataset / f"{path.stem}.png").resolve()}|{width}|{height}')
        stems.append(path.stem)
    with tempfile.TemporaryDirectory() as temp:
        listing = Path(temp) / 'list.txt'
        listing.write_text('\n'.join(lines), encoding='utf-8')
        subprocess.run([str(COMFY / 'python_embeded' / 'python.exe'), '-c', ENLARGE, str(listing)], check=True)
    return stems


def assemble(source, work, name, trigger, enlarge='ai', caption_suffix='', settings=None):
    dataset = work / 'dataset'
    dataset.mkdir(parents=True, exist_ok=True)
    count = 0
    sources = sorted(Path(source).glob('*.png'))
    if enlarge == 'nearest':
        for stem in enlarge_nearest(sources, dataset):
            caption = caption_for(stem, trigger) + (f', {caption_suffix}' if caption_suffix else '')
            (dataset / f'{stem}.txt').write_text(caption, encoding='utf-8')
            count += 1
    for path in sources if enlarge != 'nearest' else []:
        upscaled = work / 'upscale' / 'generated' / path.stem / f'{path.stem}-0.png'
        if not upscaled.is_file():
            print(f'not upscaled yet, left out: {path.stem}')
            continue
        shutil.copyfile(upscaled, dataset / f'{path.stem}.png')
        caption = caption_for(path.stem, trigger) + (f', {caption_suffix}' if caption_suffix else '')
        (dataset / f'{path.stem}.txt').write_text(caption, encoding='utf-8')
        count += 1
    config = training_config(name, dataset, work / 'output', trigger, settings)
    (work / 'config.yaml').write_text(json.dumps(config, indent=2), encoding='utf-8')
    print(f'{count} images and captions in {dataset}; config in {work / "config.yaml"}')


def retrain(source_name, work, name, trigger, settings):
    """A new work folder trained on an earlier experiment's captioned images, with changed settings. The images are copied, so
    the earlier experiment stays exactly as it was."""
    source = WORK_ROOT / source_name / 'dataset'
    if not source.is_dir():
        raise SystemExit(f'no dataset at {source}')
    dataset = work / 'dataset'
    dataset.mkdir(parents=True, exist_ok=True)
    for path in sorted(source.iterdir()):
        if path.suffix in ('.png', '.txt'):
            shutil.copyfile(path, dataset / path.name)
    config = training_config(name, dataset, work / 'output', trigger, settings)
    (work / 'config.yaml').write_text(json.dumps(config, indent=2), encoding='utf-8')
    print(f'{len(list(dataset.glob("*.png")))} images from {source_name}; config in {work / "config.yaml"} with {settings}')


def install(work, name):
    """Copy the newest LoRA where ComfyUI finds it, as phobos-private-<name>. Returns that file name and the source."""
    outputs = sorted((work / 'output').rglob('*.safetensors'), key=lambda p: p.stat().st_mtime)
    if not outputs:
        raise SystemExit(f'no trained LoRA under {work / "output"} yet')
    lora = f'phobos-private-{name}.safetensors'
    shutil.copyfile(outputs[-1], COMFY / 'ComfyUI' / 'models' / 'loras' / lora)
    print(f'installed {outputs[-1].name} as {lora}')
    return lora, outputs


def settings_from(args):
    settings = {}
    if args.steps:
        settings['steps'] = args.steps
    if args.lr:
        settings['lr'] = args.lr
    if args.resolutions:
        settings['resolutions'] = [int(size) for size in args.resolutions.split(',')]
    if getattr(args, 'low_vram', None):
        settings['low_vram'] = args.low_vram == 'on'
    return settings


LOCAL_ENGINES = ('klein-base', 'klein')


def git_lines(*args):
    done = subprocess.run(['git', *args], capture_output=True, text=True, encoding='utf-8', cwd=REPO)
    if done.returncode:
        raise SystemExit(f'git {" ".join(args)}: {done.stderr.strip()}')
    return done.stdout


def subject_for(record, name):
    """The subject in plain words: the request's own subject where the request file still names it, else the prompt's first sentence
    without the trigger word and the style instructions."""
    request = REPO / (record.get('request_file') or '-')
    if request.is_file():
        for entry in json.loads(request.read_text(encoding='utf-8')):
            if entry.get('name') == name and entry.get('subject'):
                return entry['subject']
    prompt = re.sub(r'^\w+, ', '', record.get('prompt', '')).strip()
    return re.split(r'(?<=[a-z0-9)])\. ', prompt, maxsplit=1)[0].rstrip('.')


def branch_picks(branch, engines=LOCAL_ENGINES):
    """Every picture on an art branch whose generation record names a local engine: (path, record, request name)."""
    files = set(git_lines('ls-tree', '-r', '--name-only', branch).split())
    found = []
    for path in sorted(files):
        if not path.endswith('.json') or path[:-5] + '.png' not in files:
            continue
        try:
            record = json.loads(git_lines('show', f'{branch}:{path}'))
        except (ValueError, SystemExit):
            continue
        if isinstance(record, dict) and record.get('engine') in engines:
            found.append((path[:-5] + '.png', record, re.sub(r'-\d+$', '', Path(path).stem)))
    return found


def tag_words(path, tags):
    """The words every tag whose pattern matches this branch path adds to its caption, such as a look the subject does not name
    (the dashboard's flat glyphs), so the model ties that look to words instead of learning it as the style."""
    return [words for pattern, words in tags if fnmatch.fnmatch(path, pattern)]


def mark_own_art(dataset, branch):
    """Marks a dataset of our own picks as our own art, so the near-copy check reports a likeness to it apart from one to the
    game's art (sort_rules.OWN_ART_MARKER): no one else's art is in it."""
    import sort_rules  # Beside this script; imported here so the rest of this tool needs nothing more.
    marker = {'own_art': True, 'from': branch,
              'why': 'every picture is a pick of ours from an art branch, copied byte for byte; none is the game\'s or another author\'s'}
    (Path(dataset) / sort_rules.OWN_ART_MARKER).write_text(json.dumps(marker, indent=1), encoding='utf-8')


def picks(branch, work, name, trigger, settings, tags=()):
    """A dataset of the owner's own picks from an art branch, captioned with their subjects, and its training config. The pictures
    are copied byte for byte (ai-toolkit buckets them by size), with a manifest of where each came from."""
    dataset = work / 'dataset'
    dataset.mkdir(parents=True, exist_ok=True)
    manifest = []
    for path, record, request_name in branch_picks(branch):
        data = subprocess.run(['git', 'show', f'{branch}:{path}'], capture_output=True, cwd=REPO, check=True).stdout
        stem = Path(path).stem
        (dataset / f'{stem}.png').write_bytes(data)
        caption = ', '.join([f'{trigger}, {subject_for(record, request_name)}', *tag_words(path, tags)])
        (dataset / f'{stem}.txt').write_text(caption, encoding='utf-8')
        manifest.append({'image': path, 'sha256': record.get('sha256'), 'caption': caption})
    (work / 'manifest.json').write_text(json.dumps(manifest, indent=1), encoding='utf-8')
    mark_own_art(dataset, branch)
    config = training_config(name, dataset, work / 'output', trigger, settings)
    (work / 'config.yaml').write_text(json.dumps(config, indent=2), encoding='utf-8')
    if settings.get('low_vram') is False:
        fallback = training_config(name, dataset, work / 'output', trigger, {**settings, 'low_vram': True})
        (work / 'config-lowvram.yaml').write_text(json.dumps(fallback, indent=2), encoding='utf-8')
    print(f'{len(manifest)} pictures and captions from {branch} in {dataset}; config in {work / "config.yaml"}')


def compare_models(request_file, work, loras, trigger, seeds=ROUTE_SEEDS):
    """The same subjects and seeds as earlier comparisons, through named style models only (LoRA alone, and LoRA with references),
    so a new model can be set beside the images the earlier ones already made."""
    out = []
    for req in json.loads(Path(request_file).read_text(encoding='utf-8')):
        if req.get('engine') != 'klein-base' or not req.get('references') or req.get('lora_comparison') is False:
            continue
        base = {key: req[key] for key in ('engine', 'width', 'height', 'prompt', 'references') if key in req}
        for lora in loras:
            tag = Path(lora).stem.replace('phobos-private-', '')
            out.append({key: value for key, value in base.items() if key != 'references'}
                       | {'name': f'{req["name"]}-lora-{tag}', 'seeds': list(seeds), 'prompt': f'{trigger}, {req.get("subject", req["prompt"])}', 'lora': lora})
            out.append(base | {'name': f'{req["name"]}-both-{tag}', 'seeds': list(seeds), 'prompt': f'{trigger}, {req["prompt"]}', 'lora': lora})
    target = work / 'requests' / 'compare-models.json'
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(out, indent=2), encoding='utf-8')
    print(f'{len(out)} requests in {target}, for {", ".join(loras)}')


def compare(request_file, work, name, trigger):
    """Copy the newest LoRA where ComfyUI finds it and write the three-route comparison."""
    lora, outputs = install(work, name)
    requests = route_requests(json.loads(Path(request_file).read_text(encoding='utf-8')), trigger, lora)
    target = work / 'requests' / 'compare.json'
    target.write_text(json.dumps(requests, indent=2), encoding='utf-8')
    print(f'{len(requests)} comparison requests in {target}, using {outputs[-1].name}')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('step', choices=('requests', 'assemble', 'compare', 'compare-models', 'retrain', 'install', 'picks'))
    parser.add_argument('source', help='a folder of PNGs; for compare, a request file; for retrain, the earlier experiment name; '
                        'for picks, an art branch such as origin/art-masters; for install, anything (-)')
    parser.add_argument('--low-vram', choices=('on', 'off'), help='the text encoder squeezed for a small card (default on); '
                        'picks with off also writes config-lowvram.yaml as a fallback')
    parser.add_argument('--tag', action='append', default=[], help='picks: PATTERN=WORDS, words added to the caption of every '
                        'branch path the glob pattern matches; repeatable')
    parser.add_argument('--name', required=True)
    parser.add_argument('--trigger', default='phobosstyle')
    parser.add_argument('--steps', type=int)
    parser.add_argument('--lr', type=float)
    parser.add_argument('--resolutions', help='comma-separated, such as 768,1024')
    parser.add_argument('--enlarge', choices=('ai', 'nearest'), default='ai', help='assemble: the AI upscales, or a pixel-exact enlarge')
    parser.add_argument('--caption-suffix', default='', help='assemble: words added to every caption, such as the fixed view phrase')
    parser.add_argument('--loras', default='', help='compare-models: comma-separated LoRA file names')
    args = parser.parse_args(argv)
    work = WORK_ROOT / args.name
    if args.step == 'requests':
        write_requests(args.source, work)
    elif args.step == 'assemble':
        assemble(args.source, work, args.name, args.trigger, args.enlarge, args.caption_suffix, settings_from(args))
    elif args.step == 'compare-models':
        loras = [name.strip() for name in args.loras.split(',') if name.strip()]
        if not loras:
            raise SystemExit('compare-models needs --loras')
        compare_models(args.source, work, loras, args.trigger)
    elif args.step == 'retrain':
        retrain(args.source, work, args.name, args.trigger, settings_from(args))
    elif args.step == 'install':
        install(work, args.name)
    elif args.step == 'picks':
        tags = [tuple(tag.split('=', 1)) for tag in args.tag]
        if any(len(tag) != 2 for tag in tags):
            raise SystemExit('--tag takes PATTERN=WORDS')
        picks(args.source, work, args.name, args.trigger, settings_from(args), tags)
    else:
        compare(args.source, work, args.name, args.trigger)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
