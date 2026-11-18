"""Lift an added part out of an edited picture: the difference finds where it is, Segment Anything decides its outline.

    <ComfyUI python> -I scripts/art/sam_lift.py SPEC.json

Learnt 18 Nov 2026 with the Osprey's overlays (assets/phobos-vehicles/requests/osprey-overlays-record.md): lifting by difference alone
(edit_difference.py) leaves a part see-through wherever it shares the body's colour, and doubles outlines where the redraw sits a
little off the original. The owner found four of them "very badly mangled". So the difference only says *where* the part is (each
patch of strong difference, its box and its deepest points); Segment Anything 2.1 (Meta, Apache 2.0) is prompted there on the edited
picture and outlines the part as one solid object.

SPEC.json:
  "body"     the picture the edits were made from, a path from the repository root
  "out"      the folder to write into, from the repository root
  "parts"    [{"name", "edit", "box": [x0, y0, x1, y1] shares of the picture to look in (optional), "patches": how many of the largest
             patches of difference are separate pieces of the part (two jammer turrets: 2; default 1), "pick": "best" | "smallest"
             (smallest: a cap inside a larger shape, such as a nose cap), "pad": the share of a patch's size added round its box (0.08),
             "darker_than": keep only pixels darker than this mean (0 to 255), for a dark part over the pale body (a gun),
             "negative": [[x, y], ...] shares of the picture that are not the part (the body beside it), "outline": true to give back
             a black outline Segment Anything left off (it also brings stubs of the body's own lines, so only where it is missing),
             "discs": [[cx, cy, r], ...] shares of the picture's width: a round part (a dome) clipped to its measured circle, where
             Segment Anything takes the panel under it too,
             "body" and "place": an edit made on a piece of the body (a crop, so the model had no whole aircraft to redraw): the
             piece's file, and [x0, y0, x1, y1] where it sits in the body's pixels; the part is shrunk to the piece and laid back there,
             and NAME-piece.png keeps it at the edit's size,
             "pale_is_ground": pixels paler than this in every channel are cleared, for a part reaching past the body's edge
             where the white ground shows between its pieces (a hoist's boom and pulley)}]
After Segment Anything: specks smoothed away, holes filled, stray pieces dropped.

--cpu runs on the processor: slower, but it leaves the graphics card alone, so it may run while the game does (as the background
AI's --cpu). Without it the tool refuses while RimWorld runs.

Writes, per part: NAME.png (the part on transparency, at the edit's size and place, ready to lay over the body), NAME-white.png (the
part cropped on white: a reference for a redraw, where the part alone must be in the references), NAME.json (its record); and
review.png. The edit and the body are never changed. Uses the graphics card and refuses while RimWorld runs; memory released at the end.
"""
import hashlib
import json
import sys
import time
from pathlib import Path

import numpy as np
import torch
from PIL import Image, ImageFilter
from scipy import ndimage
from transformers import Sam2Model, Sam2Processor

sys.path.insert(0, str(Path(__file__).resolve().parent))
from edit_difference import difference, part_mask  # noqa: E402
from name_parts import DARK, MODEL, WHITE, game_running  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SMOOTH_AT_4K = 9      # Pixels: closing then opening the mask by this much (at 4,096 px) drops specks and fills rivet-dot holes.
MIN_PATCH = 0.0005    # A patch of difference smaller than this share of the picture is the redraw's noise, not a part.
WHITE_MARGIN = 0.08   # Margin round the part's crop on white, as a share of its size.
OUTLINE_AT_4K = 15    # Pixels: how far past Segment Anything's edge the part's own black outline may reach (at 4,096 px).
OUTLINE_DARK = 110    # An outline pixel is darker than this in every channel (the style's near-black line).
SPECK = 0.05          # A piece smaller than this share of the part's largest piece is dropped.


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def odd(value):
    value = max(3, int(round(value)))
    return value if value % 2 else value + 1


def patches(where, count, box):
    """The largest `count` patches of difference, each as (its mask, its box)."""
    if box:
        h, w = where.shape
        keep = np.zeros_like(where)
        keep[int(box[1] * h):int(box[3] * h), int(box[0] * w):int(box[2] * w)] = True
        where = where & keep
    labels, found = ndimage.label(where)
    if not found:
        return []
    sizes = ndimage.sum(where, labels, range(1, found + 1))
    floor = MIN_PATCH * where.size
    order = [i + 1 for i in np.argsort(sizes)[::-1] if sizes[i] >= floor][:count]
    result = []
    for label in order:
        mask = labels == label
        rows, cols = np.where(mask)
        result.append((mask, (int(cols.min()), int(rows.min()), int(cols.max()), int(rows.max()))))
    return result


def deep_points(mask, bounds, count=4):
    """Points well inside a patch: its deepest point, and the deepest in each of `count - 1` bands along its longer side."""
    depth = ndimage.distance_transform_edt(mask)
    points = [np.unravel_index(np.argmax(depth), depth.shape)]
    x0, y0, x1, y1 = bounds
    along_x = (x1 - x0) >= (y1 - y0)
    edges = np.linspace(x0 if along_x else y0, (x1 if along_x else y1) + 1, count)
    for a, b in zip(edges[:-1], edges[1:]):
        band = np.zeros_like(depth)
        if along_x:
            band[:, int(a):int(b)] = depth[:, int(a):int(b)]
        else:
            band[int(a):int(b), :] = depth[int(a):int(b), :]
        if band.max() > 2:
            points.append(np.unravel_index(np.argmax(band), band.shape))
    return [[float(c), float(r)] for r, c in points]


def smooth(mask, size):
    picture = Image.fromarray((mask * 255).astype(np.uint8))
    closed = picture.filter(ImageFilter.MaxFilter(size)).filter(ImageFilter.MinFilter(size))
    opened = closed.filter(ImageFilter.MinFilter(size)).filter(ImageFilter.MaxFilter(size))
    return np.asarray(opened) > 127


def outline_ring(mask, pixels, size):
    """The part's own black outline: Segment Anything stops just inside it. Dark pixels within `size` of the part are given back;
    a stub of the body's own outline touching the part lies over that same outline once laid on the body."""
    near = np.asarray(Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(size))) > 127
    return near & ~mask & (pixels.max(axis=2) < OUTLINE_DARK)


def drop_specks(mask):
    """Pieces smaller than SPECK of the largest piece are stray bits of the redraw, not the part."""
    labels, found = ndimage.label(mask)
    if found <= 1:
        return mask
    sizes = ndimage.sum(mask, labels, range(1, found + 1))
    keep = [i + 1 for i, size in enumerate(sizes) if size >= SPECK * sizes.max()]
    return np.isin(labels, keep)


def segment(model, processor, device, image, box, points, pick, negative=()):
    labels = [1] * len(points) + [0] * len(negative)
    inputs = processor(images=image, input_boxes=[[box]], input_points=[[list(points) + list(negative)]], input_labels=[[labels]],
                       return_tensors='pt').to(device)
    with torch.no_grad():
        output = model(**inputs, multimask_output=True)
    masks = processor.post_process_masks(output.pred_masks.cpu(), inputs['original_sizes'])[0][0]
    scores = output.iou_scores.cpu()[0][0]
    choices = [(masks[i].numpy().astype(bool), float(scores[i])) for i in range(masks.shape[0])]
    choices = [choice for choice in choices if choice[0].any()]
    if not choices:
        return None, 0.0
    if pick == 'smallest':
        return min(choices, key=lambda choice: choice[0].sum())
    return max(choices, key=lambda choice: choice[1])


def on_white(part):
    bounds = part.getchannel('A').point(lambda v: 255 if v >= 128 else 0).getbbox()
    margin = int(max(bounds[2] - bounds[0], bounds[3] - bounds[1]) * WHITE_MARGIN)
    crop = part.crop((bounds[0] - margin, bounds[1] - margin, bounds[2] + margin, bounds[3] + margin))
    ground = Image.new('RGBA', crop.size, (255, 255, 255, 255))
    ground.alpha_composite(crop)
    return ground.convert('RGB')


def lift(model, processor, device, body_path, item, out):
    canvas = Image.open(body_path).size
    if item.get('body'):           # A piece of the body the edit was made on (a crop), laid back at item["place"].
        body_path = ROOT / item['body']
    edit_path = ROOT / item['edit']
    edit = Image.open(edit_path).convert('RGB')
    body = Image.open(body_path).convert('RGB')
    if body.size != edit.size:
        body = body.resize(edit.size, Image.LANCZOS)
    width, height = edit.size
    where = np.asarray(part_mask(difference(body, edit), 40, 9)) >= 128
    found = patches(where, item.get('patches', 1), item.get('box'))
    pixels = np.asarray(edit)
    inside = ~(pixels.min(axis=2) >= WHITE)
    whole = np.zeros((height, width), bool)
    prompts = []
    for mask, bounds in found:
        x0, y0, x1, y1 = bounds
        pad = item.get('pad', 0.08)
        px, py = max(12, (x1 - x0) * pad), max(12, (y1 - y0) * pad)
        box = [float(max(0.0, x0 - px)), float(max(0.0, y0 - py)), float(min(width - 1.0, x1 + px)), float(min(height - 1.0, y1 + py))]
        points = deep_points(mask, bounds)
        negative = [[x * width, y * height] for x, y in item.get('negative', [])
                    if box[0] <= x * width <= box[2] and box[1] <= y * height <= box[3]]
        chosen, score = segment(model, processor, device, edit, box, points, item.get('pick', 'best'), negative)
        if chosen is None:
            continue
        clip = np.zeros_like(chosen)
        clip[int(box[1]):int(box[3]) + 1, int(box[0]):int(box[2]) + 1] = True
        whole |= chosen & clip & inside
        prompts.append({'box': [round(v) for v in box], 'points': [[round(v) for v in p] for p in points], 'score': round(score, 3)})
    scale = width / 4096
    if item.get('discs'):          # A round part laid over a body panel of its own colour: clipped to its measured circle.
        rows, cols = np.ogrid[:height, :width]
        within = np.zeros_like(whole)
        for cx, cy, r in item['discs']:
            within |= (cols - cx * width) ** 2 + (rows - cy * height) ** 2 <= (r * width) ** 2
        whole &= within
    if 'darker_than' in item:      # A dark part over a pale body: only its own dark tones, its holes filled.
        whole &= pixels.mean(axis=2) < item['darker_than']
    whole = smooth(whole, odd(SMOOTH_AT_4K * scale)) & inside
    whole = ndimage.binary_fill_holes(whole)
    if item.get('outline'):
        whole |= outline_ring(whole, pixels, odd(OUTLINE_AT_4K * scale))
    if 'pale_is_ground' in item:   # A part reaching past the body: near-white caught between its pieces is the ground showing.
        whole &= pixels.min(axis=2) < item['pale_is_ground']
    whole = drop_specks(whole)
    alpha = Image.fromarray((whole * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))
    part = edit.convert('RGBA')
    part.putalpha(alpha)
    name = item['name']
    if alpha.getbbox():
        on_white(part).save(out / f'{name}-white.png')
    if item.get('place'):          # Made on a piece of the body: shrunk to the piece's size and laid back at its place.
        x0, y0, x1, y1 = item['place']
        placed = Image.new('RGBA', canvas, (0, 0, 0, 0))
        placed.alpha_composite(part.resize((x1 - x0, y1 - y0), Image.LANCZOS), (x0, y0))
        part.save(out / f'{name}-piece.png')
        part = placed
    part.save(out / f'{name}.png')
    bounds = part.getchannel('A').point(lambda v: 255 if v >= 128 else 0).getbbox()
    record = {'made_by': 'scripts/art/sam_lift.py (Segment Anything 2.1, facebook/sam2.1-hiera-large; no generator)',
              'body': str(Path(body_path).relative_to(ROOT)).replace('\\', '/'), 'body_sha256': sha(body_path),
              'edit': item['edit'], 'edit_sha256': sha(edit_path), 'box': item.get('box'), 'pick': item.get('pick', 'best'),
              'place': item.get('place'), 'device': device, 'prompts': prompts,
              'part_bounds': list(bounds) if bounds else None, 'sha256': sha(out / f'{name}.png')}
    (out / f'{name}.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'  {name}: {len(prompts)} piece(s), within {bounds}, scores {[p["score"] for p in prompts]}', flush=True)
    return part, bounds


def review(views, path):
    tiles = []
    for name, part, bounds in views:
        if not bounds:
            continue
        margin = 60
        crop = part.crop((bounds[0] - margin, bounds[1] - margin, bounds[2] + margin, bounds[3] + margin))
        ground = Image.new('RGBA', crop.size, DARK + (255,))
        ground.alpha_composite(crop)
        ground.thumbnail((600, 600), Image.LANCZOS)
        tiles.append(ground.convert('RGB'))
    if not tiles:
        return
    sheet = Image.new('RGB', (sum(t.width for t in tiles) + 20 * (len(tiles) + 1), max(t.height for t in tiles) + 40), DARK)
    x = 20
    for tile in tiles:
        sheet.paste(tile, (x, 20))
        x += tile.width + 20
    sheet.save(path)


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
    out = ROOT / spec['out']
    out.mkdir(parents=True, exist_ok=True)
    device = 'cuda' if torch.cuda.is_available() and not cpu else 'cpu'
    started = time.time()
    processor = Sam2Processor.from_pretrained(MODEL)
    model = Sam2Model.from_pretrained(MODEL).to(device).eval()
    print(f'model loaded on {device} in {time.time() - started:.0f} s', flush=True)
    views = []
    try:
        for item in spec['parts']:
            part, bounds = lift(model, processor, device, ROOT / spec['body'], item, out)
            views.append((item['name'], part, bounds))
    finally:
        del model
        if device == 'cuda':
            torch.cuda.empty_cache()
    review(views, out / 'review.png')
    print(f'done in {time.time() - started:.0f} s; graphics memory released; {out}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
