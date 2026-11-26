"""Cut a vehicle master into its named parts with Segment Anything 2.1 (Meta, Apache 2.0), prompted at the places its plan gives.

    <ComfyUI python> -I scripts/art/name_parts.py [--cpu] PARTS.json

The owner's rule of 18 Nov 2026 (AGENTS.md): every vehicle master is cut into its named parts wherever a part earns it, for a damage
look on that part, the anchor of anything that turns or sits on the body, a paint region, or the part as its own overlay. The grid
tool (paint_segments.py) finds every part with no names, for paint; this one finds the parts a design names.

PARTS.json says, in the plan's metres (across from the centre line, back from the nose):
  "master"   the picture, a path from the repository root
  "frame"    the plan's extent: left_m, right_m, front_m, rear_m. The picture's own extent (everything not white) is matched to it,
             so a picture that follows the plan's proportions closely needs nothing else
  "objects"  [{"name", "box_m": [x0, y0, x1, y1], "pick": "best" | "smallest", "points_m": [[x, y], ...], "negative_m": [...],
             "priority": n}]: each prompted with its box and points (the box's centre unless points_m are given; negative_m are places
             that are not the part); "smallest" takes the smallest of the model's three answers (a hub cap inside a nacelle), "best"
             its best-scored. A part never reaches past its box (with the margin); where two parts claim a pixel the higher priority
             keeps it, then the smaller part
  "sections" [{"name", "of", "from_m", "to_m"}]: a band of an object, cut along the plan's length where no line in the picture
             divides the parts (a cockpit, a cabin, a ramp along one fuselage)

Writes to parts/<the master's SHA-256>/ in the art tools' working folder (art_paths.py; formerly .local/art): one mask per part (white is the part), parts.json with each part's area, box, centre
and score in the master's pixels, and review.png: the picture on dark ground with every part outlined and named. Uses the graphics
card and refuses while RimWorld runs (the owner's rule: no GPU work while gaming); graphics memory is released at the end.

--cpu runs on the processor (26 Nov 2026, the owner's request to work without the graphics card): slower, but it leaves the graphics
card alone, so it may run while the game does, as sam_lift.py's --cpu. Without it the tool refuses while RimWorld runs.
"""
import hashlib
import json
import subprocess
import sys
import time
from pathlib import Path

import numpy as np
import torch
from PIL import Image, ImageDraw, ImageFont
from transformers import Sam2Model, Sam2Processor

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402

OUT = art_paths.work('parts')   # The art tools' working folder (art_paths.py): K: on the owner's PC since 19 Nov 2026
MODEL = art_paths.comfy_root() / 'ComfyUI' / 'models' / 'segmentation' / 'sam2.1-hiera-large'
WHITE = 235           # Paler than this in every channel is the background around the vehicle (as paint_segments.py).
BOX_MARGIN = 0.12     # The plan's box is widened by this share each way: a picture follows its plan closely, not exactly.
SMOOTH = 17           # Pixels at 4,096 px: closing then opening a mask by about this much fills rivet-dot holes and drops edge speckle.
DARK = (38, 40, 34)
COLOURS = [(230, 90, 70), (90, 170, 230), (240, 200, 70), (120, 210, 120), (200, 120, 220), (240, 150, 60), (80, 210, 200),
           (255, 240, 120), (170, 140, 100), (250, 120, 170), (150, 170, 250), (190, 230, 90)]


def game_running():
    """Through safeguards.py's list of the game's process names: 1.7 runs as RimWorld.exe, and a check for the 1.6 name alone let a
    graphics card job run beside the game (18 Nov 2026)."""
    sys.path.insert(0, str(Path(__file__).resolve().parent))   # -I leaves the script's folder off the path.
    from safeguards import game_running as watched, parse_tasklist
    try:
        listing = subprocess.run(['tasklist', '/FO', 'CSV', '/NH'], capture_output=True, text=True, timeout=20).stdout
    except (OSError, subprocess.SubprocessError):
        return False
    return bool(watched(parse_tasklist(listing)))


def mapping(inside, frame):
    """Plan metres to master pixels, by matching the picture's extent to the plan's."""
    rows, cols = np.where(inside)
    top, bottom, left, right = rows.min(), rows.max(), cols.min(), cols.max()
    sx = (right - left) / (frame['right_m'] - frame['left_m'])
    sy = (bottom - top) / (frame['rear_m'] - frame['front_m'])

    def to_px(x_m, y_m):
        return left + (x_m - frame['left_m']) * sx, top + (y_m - frame['front_m']) * sy
    return to_px, (int(left), int(top), int(right), int(bottom))


def box_px(to_px, box_m, width, height):
    (x0, y0), (x1, y1) = to_px(box_m[0], box_m[1]), to_px(box_m[2], box_m[3])
    mx, my = (x1 - x0) * BOX_MARGIN, (y1 - y0) * BOX_MARGIN
    return [max(0.0, x0 - mx), max(0.0, y0 - my), min(width - 1.0, x1 + mx), min(height - 1.0, y1 + my)]


def smooth(mask, inside):
    """Close, then open, a mask with PIL's rank filters: holes the size of a rivet row fill, specks at the edge go."""
    from PIL import ImageFilter
    picture = Image.fromarray((mask * 255).astype(np.uint8))
    closed = picture.filter(ImageFilter.MaxFilter(SMOOTH)).filter(ImageFilter.MinFilter(SMOOTH))
    opened = closed.filter(ImageFilter.MinFilter(SMOOTH)).filter(ImageFilter.MaxFilter(SMOOTH))
    return (np.asarray(opened) > 127) & inside


def describe(name, mask, score=None):
    rows, cols = np.where(mask)
    if not len(rows):
        return {'name': name, 'area': 0}
    entry = {'name': name, 'area': int(mask.sum()), 'box': [int(cols.min()), int(rows.min()), int(cols.max()), int(rows.max())],
             'centre': [round(float(cols.mean()), 1), round(float(rows.mean()), 1)]}
    if score is not None:
        entry['score'] = round(score, 3)
    return entry


def review(image, parts, path):
    view = image.convert('RGBA')
    pixels = np.asarray(view.convert('RGB'))
    alpha = np.where(pixels.min(axis=2) >= WHITE, 0, 255).astype(np.uint8)
    view.putalpha(Image.fromarray(alpha))
    ground = Image.new('RGBA', view.size, DARK + (255,))
    ground.alpha_composite(view)
    tint = Image.new('RGBA', view.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(tint)
    try:
        font = ImageFont.truetype('arial.ttf', max(18, view.width // 70))
    except OSError:
        font = ImageFont.load_default()
    for number, (name, mask, info) in enumerate(parts):
        if not info.get('area'):
            continue
        colour = COLOURS[number % len(COLOURS)]
        layer = np.zeros((view.height, view.width, 4), dtype=np.uint8)
        layer[mask] = colour + (90,)
        edge = mask & ~(np.roll(mask, 1, 0) & np.roll(mask, -1, 0) & np.roll(mask, 1, 1) & np.roll(mask, -1, 1))
        for shift in range(3):   # A thick outline, readable at a small size.
            layer[np.roll(edge, shift, 0)] = colour + (255,)
            layer[np.roll(edge, shift, 1)] = colour + (255,)
        tint.alpha_composite(Image.fromarray(layer))
        x, y = info['centre']
        draw.text((x, y), name, fill=colour + (255,), font=font, anchor='mm', stroke_width=3, stroke_fill=(0, 0, 0, 255))
    ground.alpha_composite(tint)
    ground.convert('RGB').save(path)


def main(argv):
    cpu = '--cpu' in argv
    argv = [arg for arg in argv if arg != '--cpu']
    if len(argv) != 1:
        print(__doc__)
        return 2
    if not cpu and game_running():
        print('Refused: RimWorld is running, and this uses the graphics card. --cpu runs on the processor instead.')
        return 1
    spec = json.loads(Path(argv[0]).read_text(encoding='utf-8'))
    master = ROOT / spec['master']
    image = Image.open(master).convert('RGB')
    pixels = np.asarray(image)
    height, width = pixels.shape[:2]
    inside = ~(pixels.min(axis=2) >= WHITE)
    to_px, extent = mapping(inside, spec['frame'])
    device = 'cpu' if cpu or not torch.cuda.is_available() else 'cuda'
    started = time.time()
    processor = Sam2Processor.from_pretrained(MODEL)
    model = Sam2Model.from_pretrained(MODEL).to(device).eval()
    print(f'model loaded on {device} in {time.time() - started:.0f} s', flush=True)
    digest = hashlib.sha256(master.read_bytes()).hexdigest()
    folder = OUT / digest
    folder.mkdir(parents=True, exist_ok=True)
    found = {}
    parts = []
    try:
        inputs = processor(images=image, return_tensors='pt').to(device)
        with torch.no_grad():
            embeddings = model.get_image_embeddings(inputs['pixel_values'])
        for item in spec['objects']:
            box = box_px(to_px, item['box_m'], width, height)
            positive = [list(to_px(*point)) for point in item.get('points_m', [])] or [[(box[0] + box[2]) / 2, (box[1] + box[3]) / 2]]
            negative = [list(to_px(*point)) for point in item.get('negative_m', [])]
            prompts = processor(images=image, input_boxes=[[box]], input_points=[[positive + negative]],
                                input_labels=[[[1] * len(positive) + [0] * len(negative)]], return_tensors='pt').to(device)
            with torch.no_grad():
                output = model(input_boxes=prompts['input_boxes'], input_points=prompts['input_points'],
                               input_labels=prompts['input_labels'], image_embeddings=embeddings, multimask_output=True)
            masks = processor.post_process_masks(output.pred_masks.cpu(), prompts['original_sizes'])[0][0]  # (3, H, W)
            scores = output.iou_scores.cpu()[0][0]
            choices = [(masks[i].numpy().astype(bool) & inside, float(scores[i])) for i in range(masks.shape[0])]
            choices = [(mask, score) for mask, score in choices if mask.any()]
            if not choices:
                print(f'  {item["name"]}: nothing found in its box', flush=True)
                parts.append((item['name'], np.zeros((height, width), bool), {'name': item['name'], 'area': 0}))
                continue
            if item.get('pick') == 'smallest':
                mask, score = min(choices, key=lambda choice: choice[0].sum())
            else:
                mask, score = max(choices, key=lambda choice: choice[1])
            clip = np.zeros_like(mask)
            clip[int(box[1]):int(box[3]) + 1, int(box[0]):int(box[2]) + 1] = True
            found[item['name']] = (smooth(mask & clip, inside), score, item.get('priority', 0))
        # Where two parts claim a pixel, the higher priority keeps it, then the smaller part.
        order = sorted(found, key=lambda name: (found[name][2], -int(found[name][0].sum())))
        owner = np.full((height, width), -1, dtype=np.int16)
        for index, name in enumerate(order):
            owner[found[name][0]] = index
        for index, name in enumerate(order):
            mask = owner == index
            found[name] = (mask, found[name][1], found[name][2])
        for item in spec['objects']:
            if item['name'] not in found:
                continue
            mask, score, _ = found[item['name']]
            info = describe(item['name'], mask, score)
            parts.append((item['name'], mask, info))
            print(f'  {item["name"]}: {info.get("area", 0)} px, score {score:.2f}', flush=True)
        for section in spec.get('sections', []):
            if section['of'] not in found:
                continue
            whole = found[section['of']][0]
            _, y0 = to_px(0, section['from_m'])
            _, y1 = to_px(0, section['to_m'])
            band = np.zeros_like(whole)
            band[int(round(y0)):int(round(y1)), :] = True
            mask = whole & band
            parts.append((section['name'], mask, describe(section['name'], mask)))
    finally:
        del model
        if device == 'cuda':
            torch.cuda.empty_cache()
    for name, mask, _ in parts:
        Image.fromarray((mask * 255).astype(np.uint8)).save(folder / f'{name}.png')
    review(image, [p for p in parts if p[0] not in {s['of'] for s in spec.get('sections', [])}], folder / 'review.png')
    record = {'master': spec['master'], 'master_sha256': digest, 'model': 'facebook/sam2.1-hiera-large', 'spec': argv[0].replace('\\', '/'),
              'picture_extent': list(extent), 'box_margin': BOX_MARGIN, 'parts': [info for _, _, info in parts]}
    (folder / 'parts.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'done in {time.time() - started:.0f} s; graphics memory released; {folder}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
