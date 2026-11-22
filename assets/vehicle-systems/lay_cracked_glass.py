"""Cracks over a vehicle's own glass (D'thorga Framework 0.238.0; docs/development/vehicle-glass.md, VG11).

The owner's choice of 23 Nov 2026, after pasting whole cracked panes over the glass left ragged edges: as the damage pictures are made,
marks only. One master serves every vehicle (the owner's pick of the tipper pilot, try 1). Its cracks are kept as light marks
(export_vehicle.light_marks_to_alpha: near-white lines and stars, the master's own glass falling away), fitted to the vehicle's glass and
clipped to its panes, so they lie over the vehicle's own glass, which stays exactly as drawn: its colour, shading, pillars and edges. A
clip a pixel off only ends a crack a pixel early. A windscreen strip takes the whole master; a squarer box (an aircraft's cockpit) takes
the master's cracks in rows at a windscreen's crack size. East is north turned 90 degrees clockwise and south 180 degrees, as our vehicle
pictures are. Masters are never changed; the outputs are new files.

    python assets/vehicle-systems/lay_cracked_glass.py MASTER.png BODY_north.png OUT_PREFIX

writes OUT_PREFIX_north.png, _east.png and _south.png on the body's canvas, transparent but for the cracks.
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'scripts' / 'art'))
from export_vehicle import light_marks_to_alpha  # noqa: E402

OUTLINE_BELOW = 50    # a pixel whose brightest channel is under this is the drawing's outline, a pillar or a stroke
SMALLEST_PANE = 40    # pixels; anything smaller is a crumb between outline strokes, never a pane by itself
FRONT_SHARE = 0.16    # the glass is found within this share of the vehicle's length from its front
BOX_PADDING = 14      # pixels round the found panes, so corner panes at a windscreen's ends are inside the box
SMALLEST_CRACK = 4     # pixels; a smaller piece of crack is a crumb left by the clip or the scaling
CRACK_SCALE = 0.32    # a squarer box's cracks: twice a windscreen's size, so a tall pane carries one or two impacts, not five


def is_glass_colour(mean: np.ndarray) -> bool:
    """A pane's mean colour: dark and clearly blue-leaning, rim highlights included; a tyre's neutral dark grey is not glass."""
    return mean.max() < 130 and mean[2] - mean[0] >= 4


def panes(rgb: np.ndarray, alpha: np.ndarray) -> tuple[np.ndarray, int]:
    inside = (rgb.max(axis=2) >= OUTLINE_BELOW) & (alpha > 200)
    return ndimage.label(inside)


def find_box(body: np.ndarray) -> tuple[int, int, int, int]:
    """The glass's box: the glass-coloured panes within the front of the vehicle, padded."""
    rgb, alpha = body[..., :3].astype(int), body[..., 3]
    ys, _ = np.where(alpha > 200)
    front, back = ys.min(), ys.max()
    limit = front + (back - front) * FRONT_SHARE
    labels, count = panes(rgb, alpha)
    boxes = []
    for label in range(1, count + 1):
        pane = labels == label
        if pane.sum() < SMALLEST_PANE * 4 or not is_glass_colour(rgb[pane].mean(axis=0)):
            continue
        py, px = np.where(pane)
        if py.mean() <= limit:
            boxes.append((px.min(), py.min(), px.max() + 1, py.max() + 1))
    if not boxes:
        raise SystemExit('no glass found at the front of the picture')
    h, w = alpha.shape
    return (max(0, int(min(b[0] for b in boxes)) - BOX_PADDING), max(0, int(min(b[1] for b in boxes)) - BOX_PADDING),
            min(w, int(max(b[2] for b in boxes)) + BOX_PADDING), min(h, int(max(b[3] for b in boxes)) + BOX_PADDING))


def crop_to_glass(master: Image.Image) -> Image.Image:
    """The master's glass: the rows and columns that are not the generator's near-black border, trimmed inside its own dark rim."""
    a = np.array(master.convert('RGB')).astype(int)
    bright = a.max(axis=2) > 40
    rows = np.where(bright.mean(axis=1) > 0.5)[0]
    cols = np.where(bright[rows.min():rows.max() + 1].mean(axis=0) > 0.5)[0]
    top, bottom, left, right = int(rows.min()), int(rows.max()) + 1, int(cols.min()), int(cols.max()) + 1
    inset_y, inset_x = max(2, (bottom - top) // 12), max(2, (right - left) // 60)
    return master.crop((left + inset_x, top + inset_y, right - inset_x, bottom - inset_y))


def glass_mask(body: np.ndarray, box: tuple[int, int, int, int]) -> np.ndarray:
    """Where cracks may show: the panes the outline encloses whose colour is the glass's, with the glass's own neutral shadow band. Never the
    outline, the pillars or the paint: a crack stops at them, as it would."""
    x0, y0, x1, y1 = box
    region = body[y0:y1, x0:x1].astype(int)
    rgb, alpha = region[..., :3], region[..., 3]
    labels, count = panes(rgb, alpha)
    mask = np.zeros(alpha.shape, dtype=bool)
    for label in range(1, count + 1):
        pane = labels == label
        if pane.sum() >= SMALLEST_PANE and is_glass_colour(rgb[pane].mean(axis=0)):
            mask |= pane
    if not mask.any():
        return mask
    touching = ndimage.binary_dilation(mask, iterations=2)
    for label in range(1, count + 1):
        pane = labels == label
        if (pane & mask).any() or not (pane & touching).any():
            continue
        if pane[0, :].any() or pane[-1, :].any() or pane[:, 0].any() or pane[:, -1].any():
            continue
        if rgb[pane].mean(axis=0).max() < 130:
            mask |= pane
    return mask


def premultiplied_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    """Resized without a dark or pale fringe: alpha premultiplied while scaling."""
    return image.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')


def cracks_for(marks: Image.Image, width: int, height: int) -> Image.Image:
    """The master's cracks fitted to the box: stretched along a windscreen strip, as the owner's pilot; in rows, each shifted along, at a
    windscreen's crack size over a squarer box."""
    if width / height >= (marks.width / marks.height) / 2:
        return premultiplied_resize(marks, (width, height))
    row = premultiplied_resize(marks, (max(1, round(marks.width * CRACK_SCALE)), max(1, round(marks.height * CRACK_SCALE))))
    canvas = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    for i, top in enumerate(range(0, height, row.height)):
        shift = ((i * row.width * 3) // 7) % row.width
        for left in range(-shift, width, row.width):
            piece = row.crop((max(0, -left), 0, min(row.width, width - left), min(row.height, height - top)))
            canvas.alpha_composite(piece, (max(0, left), top))
    return canvas


def main() -> int:
    if len(sys.argv) != 4:
        print(__doc__)
        return 2
    master_path, body_path, out = sys.argv[1], sys.argv[2], sys.argv[3]
    body = np.array(Image.open(body_path).convert('RGBA'))
    x0, y0, x1, y1 = box = find_box(body)
    marks = light_marks_to_alpha(crop_to_glass(Image.open(master_path)).convert('RGB'))
    piece = np.array(cracks_for(marks, x1 - x0, y1 - y0))
    mask = glass_mask(body, box)
    piece[..., 3] = np.where(mask, piece[..., 3], 0).astype(np.uint8)
    # Crumbs of a crack left where the clip or the scaling cut a line short are dropped: under SMALLEST_CRACK pixels, not a crack.
    marked = piece[..., 3] > 0
    labels, count = ndimage.label(marked, structure=np.ones((3, 3)))
    if count:
        sizes = ndimage.sum(marked, labels, range(1, count + 1))
        crumbs = np.isin(labels, np.where(sizes < SMALLEST_CRACK)[0] + 1)
        piece[..., 3][crumbs] = 0
    canvas = np.zeros_like(body)
    canvas[y0:y1, x0:x1] = piece
    north = Image.fromarray(canvas, 'RGBA')
    north.save(out + '_north.png')
    north.rotate(-90, expand=True).save(out + '_east.png')
    north.rotate(180, expand=True).save(out + '_south.png')
    print(f'wrote {out}_north/_east/_south.png: box {box}, cracks on {int((piece[..., 3] > 0).sum())} of {int(mask.sum())} glass pixels')
    return 0


if __name__ == '__main__':
    sys.exit(main())
