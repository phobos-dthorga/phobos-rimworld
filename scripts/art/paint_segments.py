"""Cut a vehicle master into its parts with Segment Anything 2.1 (Meta, Apache 2.0), for paint masks painted a whole part at a time.
Run with ComfyUI's bundled Python, which carries torch and transformers; it reads the model from ComfyUI's models folder, fetched by
scripts/art/fetch_models.py --only sam2 (and its three config files).

    <ComfyUI python> -I scripts/art/paint_segments.py [--cpu] MASTER [MASTER ...]

Owner, 16 Nov 2026: paint masks made one pixel at a time came out speckled; the owner asked for this method and the outlined-region method
(export_vehicle.py) side by side. For each master it prompts the model with a grid of points over the vehicle (never the white around it),
keeps each point's best-scoring mask, and writes a label map: every pixel names the smallest kept part covering it, or none. The map is
a 16-bit greyscale PNG in .local/art/segments, named by the master's SHA-256, with a small JSON beside it; the exporter reads it with
--paint parts. Uses the graphics card and refuses while RimWorld runs (the owner's rule: no GPU work while gaming).

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
from PIL import Image
from transformers import Sam2Model, Sam2Processor

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402

OUT = art_paths.work('segments')   # The art tools' working folder (art_paths.py): K: on the owner's PC since 19 Nov 2026
MODEL = art_paths.comfy_root() / 'ComfyUI' / 'models' / 'segmentation' / 'sam2.1-hiera-large'
GRID_STEP = 24        # master pixels between prompt points (authored: a panel is wider than this in every master we have)
POINTS_PER_BATCH = 48
POINTS_PER_BATCH_CPU = 8   # on the processor, beside the game: a batch's full-size masks are held at once (48 wanted 2.3 GB, 26 Nov 2026)
MIN_SCORE = 0.80      # a mask the model scores lower is not kept
MIN_AREA = 40         # master pixels; smaller masks are noise
WHITE = 235           # paler than this in every channel is the background around the vehicle


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


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def segment(master, model, processor, device, per_batch=POINTS_PER_BATCH):
    image = Image.open(master).convert('RGB')
    pixels = np.asarray(image)
    height, width = pixels.shape[:2]
    inside = ~(pixels.min(axis=2) >= WHITE)
    points = [(x, y) for y in range(GRID_STEP // 2, height, GRID_STEP) for x in range(GRID_STEP // 2, width, GRID_STEP) if inside[y, x]]
    inputs = processor(images=image, return_tensors='pt').to(device)
    with torch.no_grad():
        embeddings = model.get_image_embeddings(inputs['pixel_values'])
    kept = []
    for start in range(0, len(points), per_batch):
        batch = points[start:start + per_batch]
        prompts = processor(images=image, input_points=[[[[x, y]] for x, y in batch]], input_labels=[[[1] for _ in batch]], return_tensors='pt').to(device)
        with torch.no_grad():
            output = model(input_points=prompts['input_points'], input_labels=prompts['input_labels'], image_embeddings=embeddings, multimask_output=True)
        masks = processor.post_process_masks(output.pred_masks.cpu(), prompts['original_sizes'])[0]  # (points, 3, H, W)
        scores = output.iou_scores.cpu()[0]  # (points, 3)
        for index in range(len(batch)):
            best = int(scores[index].argmax())
            if float(scores[index, best]) < MIN_SCORE:
                continue
            mask = masks[index, best].numpy().astype(bool) & inside
            area = int(mask.sum())
            if area >= MIN_AREA:
                # Kept cropped to its own box (26 Nov 2026): thousands of full-size masks filled the memory beside the game.
                rows, cols = np.any(mask, axis=1), np.any(mask, axis=0)
                y0, y1 = np.argmax(rows), len(rows) - np.argmax(rows[::-1])
                x0, x1 = np.argmax(cols), len(cols) - np.argmax(cols[::-1])
                kept.append((area, y0, x0, mask[y0:y1, x0:x1].copy()))
    # Smallest wins: paint the biggest first and let smaller parts overwrite them.
    labels = np.zeros((height, width), dtype=np.uint16)
    kept.sort(key=lambda item: -item[0])
    for number, (_, y0, x0, part) in enumerate(kept, start=1):
        labels[y0:y0 + part.shape[0], x0:x0 + part.shape[1]][part] = min(number, 65535)
    return labels, len(points), len(kept)


def main(argv):
    cpu = '--cpu' in argv
    argv = [arg for arg in argv if arg != '--cpu']
    if not argv:
        print(__doc__)
        return 2
    if not cpu and game_running():
        print('Refused: RimWorld is running, and this uses the graphics card. --cpu runs on the processor instead.')
        return 1
    device = 'cpu' if cpu or not torch.cuda.is_available() else 'cuda'
    started = time.time()
    processor = Sam2Processor.from_pretrained(MODEL)
    model = Sam2Model.from_pretrained(MODEL).to(device).eval()
    print(f'model loaded on {device} in {time.time() - started:.0f} s', flush=True)
    OUT.mkdir(parents=True, exist_ok=True)
    try:
        for master in argv:
            began = time.time()
            labels, prompted, parts = segment(master, model, processor, device, POINTS_PER_BATCH_CPU if device == 'cpu' else POINTS_PER_BATCH)
            digest = sha(master)
            Image.fromarray(labels).save(OUT / f'{digest}.png')
            (OUT / f'{digest}.json').write_text(json.dumps({'master': str(master).replace('\\', '/'), 'points': prompted, 'parts': parts,
                                                            'model': 'facebook/sam2.1-hiera-large', 'grid_step': GRID_STEP,
                                                            'min_score': MIN_SCORE}, indent=2), encoding='utf-8')
            print(f'{Path(master).name}: {prompted} points, {parts} parts kept, {time.time() - began:.0f} s', flush=True)
    finally:
        del model
        if device == 'cuda':
            torch.cuda.empty_cache()
    print(f'done in {time.time() - started:.0f} s; graphics memory released')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
