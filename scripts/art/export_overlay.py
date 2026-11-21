"""An overlay that lines up with a shipped sprite, cut from an edit of that sprite's master.

    python scripts/art/export_overlay.py BASE_MASTER EDIT TARGET --aspect 1:2 [--size 128] [--shift DX,DY]

For a building upgrade's overlay (D'thorga Framework's `PhobosBuildingUpgradeDef.overlay`): the picked master is edited to add only
the upgrade (the fuel pump's charging cable, 21 Nov 2026), and this keeps only what the edit added, placed exactly as
`export_sprite.py` placed the base. The base's crop and padding are worked out again from its master, so the two share one
centre and one scale. Where the added part reaches past the base's own frame, the overlay's canvas grows evenly on both sides, so
the game, which centres both, still lines them up; the overlay's drawSize is then wider than the building's, and the record says
by how much.

What counts as added: a pixel the edit shows that differs from the base master by more than --difference (summed over red, green
and blue), with specks under --min-area pixels dropped. A small misregistration of the edit is undone first with --shift (the
edit's offset from the base, in master pixels; measure it before cutting). Both masters keep their background cleared by the
background AI's mask, as export_sprite does.
"""
import argparse
import datetime as _dt
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import export_sprite  # noqa: E402  (beside this script)


def frame(cut, aspect_w, aspect_h):
    """Where export_sprite puts the base: the crop box in master pixels, and the padded frame (left, top, width, height) around it."""
    box = cut.getchannel('A').getbbox()
    left, top, right, bottom = box
    margin = int(max(right - left, bottom - top) * 0.06)
    crop = (max(left - margin, 0), max(top - margin, 0), min(right + margin, cut.width), min(bottom + margin, cut.height))
    width, height = crop[2] - crop[0], crop[3] - crop[1]
    padded = export_sprite.pad_to_aspect(Image.new('RGBA', (width, height)), aspect_w, aspect_h)
    pad_left, pad_top = (padded.width - width) // 2, (padded.height - height) // 2
    return crop, (crop[0] - pad_left, crop[1] - pad_top, padded.width, padded.height)


def added(base, edit, difference=90, min_area=40):
    """The edit's pixels that the base does not have, as an RGBA image the size of the base."""
    diff = ImageChops.difference(base.convert('RGB'), edit.convert('RGB'))
    red, green, blue = diff.split()
    summed = ImageChops.add(ImageChops.add(red, green), blue)
    shown = edit.getchannel('A').point(lambda value: 255 if value > 0 else 0)
    strict = ImageChops.multiply(summed.point(lambda value: 255 if value > difference else 0), shown)
    # A near-perfect edit still redraws the base's outlines a hair apart, leaving long slivers a few pixels wide. Shrinking by two
    # pixels each way drops them; growing by four brings the part back with its outline, and the looser difference below keeps the
    # grown edge to pixels that really changed (21 Nov 2026: the fuel pump's cable came with lines down the pump's sides).
    core = strict.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(9))
    loose = ImageChops.multiply(summed.point(lambda value: 255 if value > difference // 3 else 0), shown).filter(ImageFilter.MaxFilter(3))
    mask = drop_specks(ImageChops.multiply(core, loose), min_area)
    out = Image.new('RGBA', edit.size, (0, 0, 0, 0))
    out.paste(edit, (0, 0), ImageChops.multiply(mask, edit.getchannel('A')))
    return out


def drop_specks(mask, min_area):
    """The mask without connected patches smaller than min_area pixels."""
    width, height = mask.size
    pixels = mask.load()
    seen = set()
    for y in range(height):
        for x in range(width):
            if pixels[x, y] == 0 or (x, y) in seen:
                continue
            patch, stack = [], [(x, y)]
            seen.add((x, y))
            while stack:
                px, py = stack.pop()
                patch.append((px, py))
                for nx, ny in ((px + 1, py), (px - 1, py), (px, py + 1), (px, py - 1)):
                    if 0 <= nx < width and 0 <= ny < height and pixels[nx, ny] and (nx, ny) not in seen:
                        seen.add((nx, ny))
                        stack.append((nx, ny))
            if len(patch) < min_area:
                for px, py in patch:
                    pixels[px, py] = 0
    return mask


def overlay_canvas(part, base_frame):
    """The base's frame, widened (or heightened) evenly about its centre until it holds every pixel of the part."""
    left, top, width, height = base_frame
    box = part.getchannel('A').getbbox()
    if box is None:
        raise SystemExit('the edit adds nothing the base lacks: nothing to cut')
    centre_x, centre_y = left + width / 2, top + height / 2
    half_w = max(width / 2, centre_x - box[0], box[2] - centre_x)
    half_h = max(height / 2, centre_y - box[1], box[3] - centre_y)
    return (round(centre_x - half_w), round(centre_y - half_h), round(2 * half_w), round(2 * half_h))


def export(base_master, edit_master, target, aspect, size=128, shift=(0, 0), difference=90, min_area=40):
    base_master, edit_master, target = Path(base_master), Path(edit_master), Path(target)
    aspect_w, aspect_h = (int(part) for part in aspect.split(':'))
    base_cut = export_sprite.white_to_alpha(Image.open(base_master), background=export_sprite.ai_background(base_master))
    edit_cut = export_sprite.white_to_alpha(Image.open(edit_master), background=export_sprite.ai_background(edit_master))
    if edit_cut.size != base_cut.size:
        raise SystemExit(f'the edit is {edit_cut.size}, the base {base_cut.size}: ask for the edit at the base master\'s size')
    if shift != (0, 0):
        moved = Image.new('RGBA', edit_cut.size, (0, 0, 0, 0))
        moved.paste(edit_cut, (-shift[0], -shift[1]))
        edit_cut = moved
    _, base_frame = frame(base_cut, aspect_w, aspect_h)
    part = added(base_cut, edit_cut, difference, min_area)
    left, top, width, height = overlay_canvas(part, base_frame)
    scale = (size * aspect_w // min(aspect_w, aspect_h)) / base_frame[2]
    # Compressed textures are cut into 4 x 4 blocks: grow the canvas evenly until each side is a multiple of 4 in game pixels.
    pixels = tuple(-(-round(side * scale) // 4) * 4 for side in (width, height))
    grow_w, grow_h = round(pixels[0] / scale) - width, round(pixels[1] / scale) - height
    left, top, width, height = left - grow_w // 2, top - grow_h // 2, width + grow_w, height + grow_h
    canvas = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    canvas.paste(part, (-left, -top), part)
    sprite = canvas.convert('RGBa').resize(pixels, Image.LANCZOS).convert('RGBA')
    target.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(target, optimize=True)
    base_cells = (aspect_w / min(aspect_w, aspect_h), aspect_h / min(aspect_w, aspect_h))
    # The base is drawn at `size` pixels a cell, so the overlay's drawSize is its own pixels at that rate.
    draw_size = (round(sprite.width / size, 3), round(sprite.height / size, 3))
    record = {
        'target': export_sprite.repo_path(target),
        'base_master': export_sprite.repo_path(base_master),
        'base_master_sha256': export_sprite.file_sha256(base_master),
        'edit_master': export_sprite.repo_path(edit_master),
        'edit_master_sha256': export_sprite.file_sha256(edit_master),
        'target_sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
        'size': list(sprite.size),
        'draw_size_for_base_aspect': list(draw_size),
        'shift': list(shift),
        'difference': difference,
        'min_area': min_area,
        'date': _dt.datetime.now().isoformat(timespec='seconds'),
        'tool': 'export_overlay.py: the edit\'s added pixels, framed and scaled as export_sprite framed the base',
    }
    target.with_suffix('.export.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'wrote {target} {sprite.size}; drawSize {draw_size} for a base drawn at {base_cells}')
    return record


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('base_master', help='the picked master the shipped sprite was exported from')
    parser.add_argument('edit_master', help='the same master, edited to add only the upgrade')
    parser.add_argument('target')
    parser.add_argument('--aspect', default='1:1', help='the base sprite\'s width:height, as given to export_sprite')
    parser.add_argument('--size', type=int, default=128, help='the base sprite\'s pixels along its shorter side')
    parser.add_argument('--shift', default='0,0', help='the edit\'s offset from the base in master pixels, DX,DY')
    parser.add_argument('--difference', type=int, default=90)
    parser.add_argument('--min-area', type=int, default=40)
    args = parser.parse_args()
    dx, dy = (int(part) for part in args.shift.split(','))
    export(args.base_master, args.edit_master, args.target, args.aspect, args.size, (dx, dy), args.difference, args.min_area)
