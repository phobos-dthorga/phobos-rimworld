"""Clear the white our textures kept where background should be, with an AI that tells background from white paint (owner, 12 Nov 2026:
"a lot of literal white-space in our assets ... either on the borders, or inside the picture, like with the hook lift").

    python scripts/art/clean_backgrounds.py plan                          which originals the shipped textures came from, and what is left
    python scripts/art/clean_backgrounds.py masks --cpu --stop-at 23:30  the AI looks at each original, on the processor beside the game
    python scripts/art/clean_backgrounds.py masks --gpu --stop-at 07:00  the same on the graphics card, only while the game is closed

Our cut-out (export_sprite.white_to_alpha) clears white it can reach from the picture's edge, so white the object encloses stayed: the
cargo hook's opening, the sling set's triangle between its strops, the trailer's drawbar. Whether an enclosed white patch is background
or white paint (a bed sheet, a cream tank) takes recognition, which BiRefNet gives: measured 12 Nov 2026 on full-size originals, every
leftover patch scored 0.00 to 0.06 and the hook's white highlight 1.00. On 128 px icons it was unreliable, so it always looks at the
original, never the shipped texture. Its masks are kept in .local/art/bgmasks, named by each original's SHA-256; the exporters read them
from there (export_sprite.ai_background), so every export from then on clears what the AI calls background.
"""
import argparse
import datetime as _dt
import importlib.util
import json
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import safeguards  # noqa: E402
from export_sprite import AI_MASKS, file_sha256  # noqa: E402

GENERATED = 'assets/*/generated'
PICK_CALL = re.compile(r"pick\('([a-z0-9-]+)'")
PNG_NAME = re.compile(r'[A-Za-z0-9_.-]+\.png')
SECONDS_EACH = {'cpu': 15.0, 'gpu': 1.0}  # Measured on the owner's PC, 12 Nov 2026 (8 processor threads); the card figure is an estimate.


def _art_run():
    spec = importlib.util.spec_from_file_location('art_run_for_backgrounds', ROOT / 'scripts' / 'art-run.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def resolve(path):
    """A path from a record, relative to the repository or (in older vehicle records) to assets/phobos-vehicles."""
    for candidate in (ROOT / path, ROOT / 'assets' / 'phobos-vehicles' / path):
        if candidate.is_file():
            return candidate
    return None


def originals():
    """Every original a shipped texture was cut from: the sources named by the sprite export records, the masters named by the vehicle
    export records, the derived files the vehicle scripts place, and any generated picture a texture record or a vehicle script names
    (the bodies whose own export record was not kept, and the picks the placement scripts take by name)."""
    found = set()
    for record in (ROOT / 'mods').rglob('*.export.json'):
        source = resolve(json.loads(record.read_text(encoding='utf-8')).get('source', ''))
        if source:
            found.add(source)
    for record in (ROOT / 'assets').glob('*/generated/export/*.export.json'):
        master = resolve(json.loads(record.read_text(encoding='utf-8')).get('master', ''))
        if master:
            found.add(master)
    found.update((ROOT / 'assets').glob('*/generated/derived-*/*.png'))
    by_name = {}
    for picture in (ROOT / 'assets').glob('*/generated/*/*.png'):
        by_name.setdefault(picture.name, []).append(picture)
    texts = [p.read_text(encoding='utf-8', errors='replace') for p in (ROOT / 'mods').rglob('*.record.md')]
    scripts = [p.read_text(encoding='utf-8') for p in (ROOT / 'assets').glob('*/export_*.py')]
    for text in texts + scripts:
        for name in PNG_NAME.findall(text):
            found.update(by_name.get(name, []))
    for text in scripts:
        for folder in PICK_CALL.findall(text):
            found.update(p for p in (ROOT / 'assets').glob(f'*/generated/{folder}/{folder}-*.png'))
    return sorted(p for p in found if not p.name.endswith('-sheet.png'))


def plan(device='cpu'):
    pictures = originals()
    todo = [p for p in pictures if not (AI_MASKS / f'{file_sha256(p)}.png').is_file()]
    return pictures, todo, len(todo) * SECONDS_EACH[device] / 60


def masks(args):
    device = 'gpu' if args.gpu else 'cpu'
    now = _dt.datetime.now()
    stop_at = safeguards.parse_stop_at(args.stop_at, now)
    pictures, todo, minutes = plan(device)
    print(f'{len(pictures)} originals, {len(todo)} still to look at: about {minutes:.0f} minutes on the {"graphics card" if args.gpu else "processor"}')
    if not todo:
        return 0
    art_run = _art_run()
    args.comfy = args.comfy or str(art_run.art.DEFAULT_COMFY)
    if args.gpu and safeguards.game_running(art_run.read_processes() or ('rimworld.exe',)):
        print('refused: the game is running, and this run would use the graphics card (owner rule: no graphics card work while playing)')
        return 3
    AI_MASKS.mkdir(parents=True, exist_ok=True)
    listing = AI_MASKS / 'todo.json'
    listing.write_text(json.dumps([p.relative_to(ROOT).as_posix() for p in todo], indent=1), encoding='utf-8')
    python = Path(args.comfy) / 'python_embeded' / 'python.exe'
    command = [str(python), '-I', str(ROOT / 'scripts' / 'art' / 'background_ai_worker.py'), '--comfy', args.comfy, '--list', str(listing),
               '--cache', str(AI_MASKS), '--device', device, '--stop-at', stop_at.isoformat(timespec='seconds')]
    env, flags = dict(os.environ), 0
    if not args.gpu:
        mask, threads = art_run.cpu_plan(args.cpu_cores)
        command += ['--threads', str(threads), '--cores', safeguards.describe_mask(mask)]
        env = art_run.cpu_env(threads)
        flags = art_run.BELOW_NORMAL_PRIORITY_CLASS if os.name == 'nt' else 0
        print(f'processors {safeguards.describe_mask(mask)}, {threads} threads, below-normal priority, graphics card hidden')
    if not args.gpu:
        art_run.stay_awake(True)
    try:
        return subprocess.run(command, cwd=ROOT, env=env, creationflags=flags).returncode
    finally:
        art_run.stay_awake(False)


# ---- Re-export into a staging folder, review, apply ----

VEHICLES = ROOT / 'mods' / 'PhobosVehicles' / 'Textures' / 'Phobos' / 'Vehicles'
VEHICLE_EXPORT = ROOT / 'assets' / 'phobos-vehicles' / 'generated' / 'export'
STAGE = ROOT / '.local' / 'art' / 'clean-stage'
REVIEW = ROOT / '.local' / 'art' / 'clean-review'
# Paint hue ranges that were not the exporter's defaults, from the bodies' texture records (the Crawler's grey-blue hull).
BODY_HUES = {'SeabedCrawler': ((185, 240), (8, 32))}
# Bodies whose export record was not kept: their master, from the texture record; the rest is read back from the transform.
BODIES_WITHOUT_RECORDS = {'Tipper': 'assets/phobos-vehicles/generated/tipper-body-north/tipper-body-north-3.png',
                          'DrillRig': 'assets/phobos-vehicles/generated/drill-rig-body-north/drill-rig-body-north-2.png',
                          'GeneratorSet': 'assets/phobos-vehicles/generated/generator-set-body-north/generator-set-body-north-3.png'}
CLEARED_ALPHA = 64  # A pixel counts as cleared when it lost this much opacity (authored).
PAINT_CHANGE = 1.0  # A body's paint mask that moved more than this on average (0 to 255) is not applied: its paint settings were guessed.
# A clearing this large a share of the picture is held for the owner's eye (authored, 12 Nov 2026): the AI took the gas alarm's white
# ring, white paint between two dark outlines, for background, and cleared 26% of it.
LARGE_CLEARING = 0.12


def _vehicle_folder(name):
    return name.split('_')[0]


def stage():
    """Re-export every texture the records can rebuild into STAGE, never into the mods. Returns what could not be rebuilt."""
    import math
    import shutil
    from export_sprite import export
    import export_vehicle
    from PIL import Image
    if STAGE.exists():
        shutil.rmtree(STAGE)
    skipped = []
    for record_path in sorted((ROOT / 'mods').rglob('*.export.json')):
        record = json.loads(record_path.read_text(encoding='utf-8'))
        source = resolve(record.get('source', ''))
        if not source:
            skipped.append(f'{record.get("target")}: its original {record.get("source")} is not on this PC')
            continue
        width, height = record['size']
        divisor = math.gcd(width, height)
        export(source, STAGE / 'textures' / record['target'], min(width, height), f'{width // divisor}:{height // divisor}',
               record['white_threshold'], record['soft_edge'], record.get('all_white', False), record.get('silhouette', False))
    bodies = {}
    for record_path in VEHICLE_EXPORT.glob('*.export.json'):
        record = json.loads(record_path.read_text(encoding='utf-8'))
        name = record_path.name[:-len('.export.json')]
        if record.get('kind') == 'body' and (VEHICLES / _vehicle_folder(name) / f'{name}_north.png').is_file():
            bodies[name] = (resolve(record['master']), record['cells_long'], record['draw_size'], record.get('min_saturation', 0.16),
                            record.get('holes', []))
    for name, master in BODIES_WITHOUT_RECORDS.items():
        transform = json.loads((VEHICLE_EXPORT / f'{name}.transform.json').read_text(encoding='utf-8'))
        cut = export_vehicle.white_to_alpha(Image.open(ROOT / master))
        top, bottom = cut.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox()[1::2]
        cells = round((bottom - top) * transform['scale'] / export_vehicle.PIXELS_PER_CELL)
        draw = [value // export_vehicle.PIXELS_PER_CELL for value in transform['canvas']]
        bodies[name] = (ROOT / master, cells, draw, export_vehicle.MIN_SATURATION, [])
    for name, (master, cells, draw, saturation, holes) in sorted(bodies.items()):
        if not master:
            skipped.append(f'{name}: its body master is not on this PC')
            continue
        main_hue, accent_hue = BODY_HUES.get(_vehicle_folder(name), ((38, 95), (8, 34)))
        export_vehicle.export_body(master, STAGE / 'vehicles' / _vehicle_folder(name), name, cells, draw, main_hue, accent_hue, saturation,
                                   [tuple(point) for point in holes])
    for record_path in VEHICLE_EXPORT.glob('*.export.json'):
        record = json.loads(record_path.read_text(encoding='utf-8'))
        name = record_path.name[:-len('.export.json')]
        if record.get('kind') == 'overlay' and (VEHICLES / _vehicle_folder(name) / f'{name}_north.png').is_file():
            export_vehicle.export_overlay(resolve(record['master']), VEHICLE_EXPORT / record['like'], STAGE / 'vehicles' / _vehicle_folder(name),
                                          name, record.get('fit'), False, [tuple(point) for point in record.get('holes', [])])
    for script in ('export_batch_b.py', 'export_round_three.py'):
        done = subprocess.run([sys.executable, f'assets/phobos-vehicles/{script}', '--out', str(STAGE / 'vehicles')], cwd=ROOT,
                              capture_output=True, text=True)
        if done.returncode:
            skipped.append(f'{script} failed: {done.stderr.strip()[-300:]}')
    return skipped


def _shipped_for(staged):
    relative = staged.relative_to(STAGE)
    if relative.parts[0] == 'textures':
        return ROOT.joinpath(*relative.parts[1:])
    return VEHICLES.joinpath(*relative.parts[1:])


def _measure(before, after):
    """(pixels cleared, pixels made more solid, mean change on dark ground 0 to 255) between two textures of one size."""
    from PIL import Image, ImageChops, ImageStat
    lost = ImageChops.subtract(before.getchannel('A'), after.getchannel('A')).point(lambda v: 255 if v > CLEARED_ALPHA else 0)
    gained = ImageChops.subtract(after.getchannel('A'), before.getchannel('A')).point(lambda v: 255 if v > CLEARED_ALPHA else 0)
    ground = Image.new('RGBA', before.size, (46, 52, 40, 255))
    shown = [Image.alpha_composite(ground, image).convert('RGB') for image in (before, after)]
    change = sum(ImageStat.Stat(ImageChops.difference(*shown)).mean) / 3
    return lost.histogram()[255], gained.histogram()[255], change


def review():
    """Compare every staged texture with the shipped one; write a dark review page of what changed and review.json for apply."""
    import html
    import shutil
    from PIL import Image, ImageChops, ImageStat
    if REVIEW.exists():
        shutil.rmtree(REVIEW)
    (REVIEW / 'pairs').mkdir(parents=True)
    entries, paint_moved = [], set()
    staged_files = sorted(p for p in STAGE.rglob('*.png'))
    for staged in staged_files:
        shipped = _shipped_for(staged)
        if not shipped.is_file() or not staged.stem.endswith('m') or not (staged.parent / f'{staged.stem[:-1]}.png').is_file():
            continue
        before, after = Image.open(shipped).convert('RGB'), Image.open(staged).convert('RGB')
        if before.size != after.size or sum(ImageStat.Stat(ImageChops.difference(before, after)).mean) / 3 > PAINT_CHANGE:
            paint_moved.add(staged.stem[:-1].rsplit('_', 1)[0])
    for staged in staged_files:
        shipped = _shipped_for(staged)
        if not shipped.is_file() or (staged.stem.endswith('m') and (staged.parent / f'{staged.stem[:-1]}.png').is_file()):
            continue
        before, after = Image.open(shipped).convert('RGBA'), Image.open(staged).convert('RGBA')
        entry = {'staged': staged.relative_to(ROOT).as_posix(), 'shipped': shipped.relative_to(ROOT).as_posix(), 'flags': []}
        if before.size != after.size:
            entry['flags'].append(f'size changed from {before.size} to {after.size}')
            entries.append({**entry, 'cleared': 0, 'gained': 0, 'change': 255.0})
            continue
        cleared, gained, change = _measure(before, after)
        if cleared == 0 and gained == 0 and change < 0.05:
            continue
        body = staged.stem.rsplit('_', 1)[0]
        if body in paint_moved:
            entry['flags'].append('its paint settings were guessed and the paint mask moved: not applied')
        if gained:
            entry['flags'].append(f'{gained} pixels became more solid')
        visible = before.getchannel('A').point(lambda v: 255 if v > CLEARED_ALPHA else 0).histogram()[255]
        if visible and cleared / visible > LARGE_CLEARING:
            entry['flags'].append(f'a large white area was cleared ({cleared / visible:.0%} of the picture): check it was background, '
                                  'not white paint')
        entries.append({**entry, 'cleared': cleared, 'gained': gained, 'change': round(change, 2)})
    entries.sort(key=lambda e: (-len(e['flags']), -e['cleared'], -e['change']))
    rows = []
    for index, entry in enumerate(entries):
        pair = []
        for side in ('shipped', 'staged'):
            image = Image.open(ROOT / entry[side]).convert('RGBA')
            ground = Image.new('RGBA', image.size, (46, 52, 40, 255))
            shown = Image.alpha_composite(ground, image)
            scale = min(4, max(1, 256 // max(image.size)))
            shown = shown.resize((image.width * scale, image.height * scale), Image.NEAREST)
            shown.thumbnail((360, 360))
            pair.append(shown)
        name = f'{index:03d}.png'
        sheet = Image.new('RGB', (pair[0].width + pair[1].width + 12, max(p.height for p in pair)), (24, 25, 28))
        sheet.paste(pair[0].convert('RGB'), (0, 0))
        sheet.paste(pair[1].convert('RGB'), (pair[0].width + 12, 0))
        sheet.save(REVIEW / 'pairs' / name)
        flags = ''.join(f'<li class="flag">{html.escape(flag)}</li>' for flag in entry['flags'])
        rows.append(f'<section><h2>{html.escape(Path(entry["shipped"]).name)}</h2><p class="path">{html.escape(entry["shipped"])}</p>'
                    f'<p>{entry["cleared"]} pixels cleared; change on dark ground {entry["change"]}</p><ul>{flags}</ul>'
                    f'<img src="pairs/{name}" alt="before and after"></section>')
    (REVIEW / 'review.json').write_text(json.dumps(entries, indent=1), encoding='utf-8')
    flagged = sum(1 for e in entries if e['flags'])
    page = f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Background clean-up review</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>:root{{color-scheme:dark}}body{{background:#16171a;color:#e6e6e6;font:16px/1.5 system-ui,sans-serif;margin:0 auto;max-width:1100px;padding:16px}}
h1{{font-size:1.5rem}}h2{{font-size:1.05rem;margin:0}}section{{background:#1f2125;border-radius:8px;padding:12px;margin:12px 0}}
.path{{color:#9aa0a6;font-size:.85rem;margin:0}}.flag{{color:#ffb4a8}}img{{max-width:100%;image-rendering:pixelated}}
.key{{background:#2a2d33;padding:10px;border-radius:8px}}</style></head><body>
<h1>Background clean-up: before (left) and after (right), on dark ground</h1>
<div class="key"><p><b>{len(entries)} textures changed</b>; <b>{flagged} flagged</b> (shown first, and not applied unless you say so).
Nothing in the mods has changed yet.</p>
<p>To apply all the unflagged ones: <code>python scripts/art/clean_backgrounds.py apply</code>.
To leave some out: add <code>--skip NAME</code> for each (the file name, such as CargoHook.png). To apply a flagged one after
looking: add <code>--include NAME</code>.</p></div>
{''.join(rows)}</body></html>'''
    (REVIEW / 'index.html').write_text(page, encoding='utf-8')
    return entries


def apply(skip=(), include=()):
    """Copy the reviewed, unflagged textures over the shipped ones, carry each sprite's export record across, and update every texture
    record that names a replaced file's old SHA-256."""
    import shutil
    entries = json.loads((REVIEW / 'review.json').read_text(encoding='utf-8'))
    replaced, hashes = [], {}
    for entry in entries:
        shipped, staged = ROOT / entry['shipped'], ROOT / entry['staged']
        if (entry['flags'] and shipped.name not in include) or shipped.name in skip:
            continue
        old = file_sha256(shipped)
        shutil.copyfile(staged, shipped)
        hashes[old] = file_sha256(shipped)
        record = staged.with_suffix('.export.json')
        if record.is_file():
            data = json.loads(record.read_text(encoding='utf-8'))
            data['target'] = entry['shipped']
            shipped.with_suffix('.export.json').write_text(json.dumps(data, indent=2), encoding='utf-8')
        replaced.append(entry['shipped'])
    for record in (ROOT / 'mods').rglob('*.record.md'):
        text = record.read_text(encoding='utf-8')
        updated = text
        for old, new in hashes.items():
            updated = updated.replace(old, new)
        if updated != text:
            record.write_text(updated, encoding='utf-8')
    return replaced


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    commands = parser.add_subparsers(dest='command', required=True)
    commands.add_parser('plan', help='list the originals and what is left to look at')
    commands.add_parser('stage', help='re-export every texture the records can rebuild into .local/art/clean-stage, then write the review')
    applying = commands.add_parser('apply', help='copy the reviewed, unflagged textures into the mods')
    applying.add_argument('--skip', action='append', default=[], help='a texture file name to leave as it is; repeatable')
    applying.add_argument('--include', action='append', default=[], help='a flagged texture to apply after all, once looked at; repeatable')
    run = commands.add_parser('masks', help='let the AI look at every original not yet done')
    where = run.add_mutually_exclusive_group(required=True)
    where.add_argument('--cpu', action='store_true', help='on the processor, below-normal priority, away from the game')
    where.add_argument('--gpu', action='store_true', help='on the graphics card; refused while the game runs')
    run.add_argument('--stop-at', required=True, help='HH:MM (its next occurrence) or a full ISO date and time')
    run.add_argument('--cpu-cores', help='the logical processors to use, such as 16-31 (default: those away from the game)')
    run.add_argument('--comfy', help="ComfyUI portable folder (default: the art tools' own, which COMFYUI_PATH can change)")
    args = parser.parse_args(argv)
    if args.command == 'plan':
        pictures, todo, minutes = plan()
        print(f'{len(pictures)} originals behind the shipped textures; {len(todo)} not yet looked at by the AI '
              f'(about {minutes:.0f} minutes on the processor, a few on the graphics card)')
        return 0
    if args.command == 'stage':
        for line in stage():
            print('not rebuilt:', line)
        entries = review()
        print(f'{len(entries)} textures changed, {sum(1 for e in entries if e["flags"])} flagged; review: {REVIEW / "index.html"}')
        return 0
    if args.command == 'apply':
        replaced = apply(set(args.skip), set(args.include))
        print(f'{len(replaced)} textures replaced; texture records updated to their new hashes')
        return 0
    return masks(args)


if __name__ == '__main__':
    sys.exit(main())
