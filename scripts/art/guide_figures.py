"""Draw the player guide's figures from the mods' shipped textures.

    python scripts/art/guide_figures.py                  draw every figure in docs/player-guide/images/figures.json
    python scripts/art/guide_figures.py --only vehicles  draw the figures whose name contains "vehicles"
    python scripts/art/guide_figures.py --check          check every source exists and every figure is drawn; draws nothing

Each figure is a dark panel built from textures the mods already ship, so the guide never holds art of its own:

- "layers": vehicles or buildings, each one or more pictures laid on top of each other (a vehicle and its upgrades), drawn
  to one scale (pixels per game cell) so sizes compare truly, in a grid, each with a label above it;
- "icons": buttons and items in a grid, each with a label under it, optionally tinted (the game tints some icons at run time).

Run it again when a texture changes. Needs Pillow; reads nothing outside the repository and writes only the figures.
"""
import argparse
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / 'docs' / 'player-guide' / 'images' / 'figures.json'

BACKGROUND = (0x1E, 0x20, 0x1C)   # close to the game's window colour
CARD = (0x2A, 0x2D, 0x27)
TEXT = (0xE6, 0xE1, 0xD3)
MUTED = (0xA8, 0xA3, 0x96)
GAP = 16
LABEL_SIZE = 18
NOTE_SIZE = 14


def font(size):
    return ImageFont.load_default(size=size)


ROOTS = {}


def resolve(path):
    """A path in the manifest, with an optional "alias:" prefix from its "roots" table, as a repository path."""
    alias, sep, rest = path.partition(':')
    return ROOTS[alias] + rest if sep and alias in ROOTS else path


def source(path):
    full = ROOT / resolve(path)
    if not full.is_file():
        raise FileNotFoundError(f'missing source {path}')
    return full


def icon_layers(item):
    """An icon is one path (optionally with the item's tint) or a list of {path, tint} drawn in order."""
    if isinstance(item['icon'], str):
        return [{'path': item['icon'], 'tint': item.get('tint')}]
    return item['icon']


def sources(figure):
    if figure['kind'] == 'layers':
        return [layer for item in figure['items'] for layer in item['layers']]
    return [layer['path'] for item in figure['items'] for layer in icon_layers(item)]


def wrap(draw, text, face, width):
    """Split text into lines no wider than width."""
    lines, line = [], ''
    for word in text.split():
        trial = f'{line} {word}'.strip()
        if line and draw.textlength(trial, font=face) > width:
            lines.append(line)
            line = word
        else:
            line = trial
    if line:
        lines.append(line)
    return lines


def tinted(image, colour):
    """Multiply an icon by a colour, as the game's shader tints a white icon."""
    r, g, b = (int(colour[i:i + 2], 16) for i in (1, 3, 5))
    bands = image.split()
    rgb = [band.point(lambda v, k=k: v * k // 255) for band, k in zip(bands[:3], (r, g, b))]
    return Image.merge('RGBA', (*rgb, bands[3]))


def draw_layers(figure):
    """Pictures stacked into one per item, scaled by cells so every item shares one scale."""
    cell = figure.get('pixelsPerCell', 48)
    columns = figure.get('columns', 1)
    label_face, note_face = font(LABEL_SIZE), font(NOTE_SIZE)
    tiles = []
    for item in figure['items']:
        base = None
        for layer in item['layers']:
            picture = Image.open(source(layer)).convert('RGBA')
            if base is None:
                base = Image.new('RGBA', picture.size, (0, 0, 0, 0))
            if picture.size != base.size:
                picture = picture.resize(base.size, Image.LANCZOS)
            base = Image.alpha_composite(base, picture)
        w_cells, h_cells = item['cells']
        tiles.append((item, base.resize((round(w_cells * cell), round(h_cells * cell)), Image.LANCZOS)))

    rows = [tiles[i:i + columns] for i in range(0, len(tiles), columns)]
    column_widths = [max((row[c][1].width for row in rows if c < len(row)), default=0) for c in range(columns)]
    probe = ImageDraw.Draw(Image.new('RGB', (1, 1)))
    label_h = LABEL_SIZE + 8

    def note_lines(item, width):
        return wrap(probe, item['note'], note_face, width) if item.get('note') else []

    def label_lines(item, width):
        return wrap(probe, item['label'], label_face, width)

    row_heights = []
    for row in rows:
        row_heights.append(max(len(label_lines(item, column_widths[c])) * label_h
                               + len(note_lines(item, column_widths[c])) * (NOTE_SIZE + 4) + picture.height
                               for c, (item, picture) in enumerate(row)))
    width = sum(column_widths) + GAP * (columns + 1)
    height = sum(row_heights) + GAP * (len(rows) + 1)
    canvas = Image.new('RGBA', (width, height), BACKGROUND + (255,))
    draw = ImageDraw.Draw(canvas)
    y = GAP
    for row, row_h in zip(rows, row_heights):
        x = GAP
        for c, (item, picture) in enumerate(row):
            ny = y
            for line in label_lines(item, column_widths[c]):
                draw.text((x, ny), line, font=label_face, fill=TEXT)
                ny += label_h
            for line in note_lines(item, column_widths[c]):
                draw.text((x, ny), line, font=note_face, fill=MUTED)
                ny += NOTE_SIZE + 4
            canvas.alpha_composite(picture, (x, ny))
            x += column_widths[c] + GAP
        y += row_h + GAP
    return canvas


def draw_icons(figure):
    """Icons on cards in a grid, each with a wrapped label under it."""
    size = figure.get('iconSize', 64)
    columns = figure.get('columns', 6)
    card_w = max(size + 24, figure.get('cardWidth', 128))
    face = font(NOTE_SIZE + 1)
    probe = ImageDraw.Draw(Image.new('RGB', (1, 1)))
    items = figure['items']
    labels = [wrap(probe, item['label'], face, card_w - 12) for item in items]
    line_h = NOTE_SIZE + 5
    card_h = 12 + size + 10 + max(len(lines) for lines in labels) * line_h + 8
    rows = (len(items) + columns - 1) // columns
    used_columns = min(columns, len(items))
    width = used_columns * card_w + (used_columns + 1) * GAP // 2
    height = rows * card_h + (rows + 1) * GAP // 2
    canvas = Image.new('RGBA', (width, height), BACKGROUND + (255,))
    draw = ImageDraw.Draw(canvas)
    for i, (item, lines) in enumerate(zip(items, labels)):
        x = GAP // 2 + (i % columns) * (card_w + GAP // 2)
        y = GAP // 2 + (i // columns) * (card_h + GAP // 2)
        draw.rounded_rectangle((x, y, x + card_w, y + card_h), radius=6, fill=CARD)
        icon = None
        for layer in icon_layers(item):
            picture = Image.open(source(layer['path'])).convert('RGBA')
            if layer.get('tint'):
                picture = tinted(picture, layer['tint'])
            if icon is None:
                icon = Image.new('RGBA', picture.size, (0, 0, 0, 0))
            icon = Image.alpha_composite(icon, picture.resize(icon.size, Image.LANCZOS))
        icon.thumbnail((size, size), Image.LANCZOS)
        canvas.alpha_composite(icon, (x + (card_w - icon.width) // 2, y + 12 + (size - icon.height) // 2))
        ty = y + 12 + size + 10
        for line in lines:
            draw.text((x + (card_w - draw.textlength(line, font=face)) // 2, ty), line, font=face, fill=TEXT)
            ty += line_h
    return canvas


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--only', help='draw only figures whose name contains this text')
    parser.add_argument('--check', action='store_true', help='check sources and figures exist; draw nothing')
    args = parser.parse_args(argv)
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
    ROOTS.update(manifest.get('roots', {}))
    out_dir = MANIFEST.parent
    failures = []
    for figure in manifest['figures']:
        if args.only and args.only not in figure['name']:
            continue
        target = out_dir / f"{figure['name']}.png"
        missing = [path for path in sources(figure) if not (ROOT / resolve(path)).is_file()]
        failures += [f"{figure['name']}: missing source {path}" for path in missing]
        if args.check:
            if not target.is_file():
                failures.append(f"{figure['name']}: not drawn yet ({target.relative_to(ROOT).as_posix()})")
            continue
        if missing:
            continue
        image = draw_layers(figure) if figure['kind'] == 'layers' else draw_icons(figure)
        image.convert('RGB').save(target, optimize=True)
        print(f'wrote {target.relative_to(ROOT).as_posix()} ({image.width} x {image.height})')
    for failure in failures:
        print(failure)
    return 1 if failures else 0


if __name__ == '__main__':
    sys.exit(main())
