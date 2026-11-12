"""Turn an approved vehicle master (seen from above, north facing, on white) into Vehicle Framework's textures. Free, and never touches
the master: every file it writes is new.

    python scripts/art/export_vehicle.py body MASTER --out DIR --name BulkTanker --cells-long 6 --draw 3 7
    python scripts/art/export_vehicle.py overlay MASTER --like DIR/BulkTanker.transform.json --out DIR --name BulkTanker_LiquidTank \
        --fit 225 560 540 1440

"body" cuts the white away, scales so the vehicle is exactly --cells-long cells long at 128 px a cell, centres it on a drawSize canvas and
writes the north, east and south facings with colour masks (suffix m), a 64 px icon and a transform file. East and south are the north
turned, so they can never drift from it; Vehicle Framework mirrors east for west. "overlay" applies a body's transform to an overlay
drawn on the same canvas, so the two register; --fit first moves and scales the overlay's content into a box given in master pixels,
because a generator's placement is only approximate.

Paint: Vehicle Framework's pattern shader multiplies the texture by the player's colour where the mask is set, so painted areas are made
grey in the texture, as Vanilla Vehicles Expanded's are (measured 8 Nov 2026: saturation about 0 under the mask), and the colour that
reproduces the master is written to the transform file for the Def. Mask red is the main paint, green the accent.
"""
import argparse
import colorsys
import datetime as _dt
import hashlib
import json
from pathlib import Path
import sys

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from export_sprite import white_to_alpha  # noqa: E402

PIXELS_PER_CELL = 128
ICON_SIZE = 64
FACINGS = {'north': None, 'east': Image.ROTATE_270, 'south': Image.ROTATE_180}
BRIGHTEST_GREY = 235  # The brightest paint in the texture, leaving headroom for the shader.
MARK_COLOUR = (22, 19, 17)  # Damage marks keep only their darkness: soot and holes in near-black, however pale their edges were drawn.
MARK_FLOOR = 14  # Paler than this much darkness is the white background, not a mark.
OUTLINE_VALUE = 0.24  # Darker than this is outline or shadow, never paint.
MIN_SATURATION = 0.16  # Greyer than this is bare metal, glass or rubber, never paint.


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def classify(red, green, blue, main_hue, accent_hue, min_saturation=MIN_SATURATION):
    """'main', 'accent' or None for one opaque pixel; hues are in degrees. A greyish paint, such as a grey-blue hull, needs a lower
    min_saturation than the default, which keeps bare metal unpainted."""
    hue, saturation, value = colorsys.rgb_to_hsv(red / 255, green / 255, blue / 255)
    if value < OUTLINE_VALUE or saturation < min_saturation:
        return None
    degrees = hue * 360
    if main_hue[0] <= degrees <= main_hue[1]:
        return 'main'
    if accent_hue[0] <= degrees <= accent_hue[1]:
        return 'accent'
    return None


def luminance(red, green, blue):
    return 0.299 * red + 0.587 * green + 0.114 * blue


def separate_paint(image, main_hue, accent_hue, min_saturation=MIN_SATURATION):
    """(grey-painted texture, mask, default colours). Each paint is scaled on its own, so its grey fills the shader's range and its
    default colour gives the master's tone back."""
    source = image.load()
    width, height = image.size
    groups = {'main': [], 'accent': []}
    for y in range(height):
        for x in range(width):
            red, green, blue, alpha = source[x, y]
            if alpha >= 128:
                kind = classify(red, green, blue, main_hue, accent_hue, min_saturation)
                if kind:
                    groups[kind].append((red, green, blue))
    gains, defaults = {}, {}
    for kind, pixels in groups.items():
        if not pixels:
            gains[kind], defaults[kind] = 1.0, None
            continue
        lights = sorted(luminance(*pixel) for pixel in pixels)
        gain = BRIGHTEST_GREY / max(lights[int(len(lights) * 0.98)], 1)
        mean = [sum(pixel[channel] for pixel in pixels) / len(pixels) for channel in range(3)]
        mean_grey = sum(lights) / len(lights) * gain
        gains[kind] = gain
        defaults[kind] = [min(255, round(channel / mean_grey * 255)) for channel in mean]
    texture = image.copy()
    mask = Image.new('RGBA', image.size, (0, 0, 0, 255))
    written, marks = texture.load(), mask.load()
    channel_mark = {'main': (255, 0, 0, 255), 'accent': (0, 255, 0, 255)}
    for y in range(height):
        for x in range(width):
            red, green, blue, alpha = source[x, y]
            if alpha < 128:
                continue
            kind = classify(red, green, blue, main_hue, accent_hue, min_saturation)
            if kind:
                grey = min(255, round(luminance(red, green, blue) * gains[kind]))
                written[x, y] = (grey, grey, grey, alpha)
                marks[x, y] = channel_mark[kind]
    return texture, mask, {'color': defaults['main'], 'colorTwo': defaults['accent']}


def marks_to_alpha(image):
    """Damage marks on white as near-black with alpha from how dark each pixel is, so soot fades into whatever is under it instead of
    keeping a pale halo."""
    grey = image.convert('L')
    alpha = grey.point(lambda value: 0 if 255 - value < MARK_FLOOR else 255 - value)
    result = Image.new('RGBA', image.size, MARK_COLOUR + (0,))
    result.putalpha(alpha)
    return result


def place(image, scale, offset, canvas):
    """The image scaled (premultiplied, so no white fringe) and pasted at offset on a clear canvas."""
    size = (round(image.width * scale), round(image.height * scale))
    scaled = image.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')
    result = Image.new('RGBA', canvas, (0, 0, 0, 0))
    result.alpha_composite(scaled, tuple(max(0, value) for value in offset), tuple(max(0, -value) for value in offset))
    return result


def place_mask(mask, scale, offset, canvas):
    size = (round(mask.width * scale), round(mask.height * scale))
    scaled = mask.resize(size, Image.LANCZOS)
    result = Image.new('RGBA', canvas, (0, 0, 0, 255))
    result.paste(scaled, offset)
    return result


def write_facings(image, out, name, suffix=''):
    written = []
    for facing, turn in FACINGS.items():
        turned = image if turn is None else image.transpose(turn)
        path = out / f'{name}_{facing}{suffix}.png'
        turned.save(path, optimize=True)
        written.append(path)
    return written


def record(out, name, master, written, details):
    data = {
        'master': str(master).replace('\\', '/'), 'master_sha256': sha(master),
        'files': {path.name: sha(path) for path in written},
        'date': _dt.date.today().isoformat(), 'tool': 'scripts/art/export_vehicle.py', **details,
    }
    (out / f'{name}.export.json').write_text(json.dumps(data, indent=2), encoding='utf-8')


def export_body(master, out, name, cells_long, draw, main_hue, accent_hue, min_saturation=MIN_SATURATION, holes=()):
    out.mkdir(parents=True, exist_ok=True)
    cut = white_to_alpha(Image.open(master), holes=holes)
    left, top, right, bottom = cut.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox()
    scale = cells_long * PIXELS_PER_CELL / (bottom - top)
    canvas = (draw[0] * PIXELS_PER_CELL, draw[1] * PIXELS_PER_CELL)
    offset = (round(canvas[0] / 2 - (left + right) / 2 * scale), round(canvas[1] / 2 - (top + bottom) / 2 * scale))
    texture, mask, colours = separate_paint(cut, main_hue, accent_hue, min_saturation)
    written = write_facings(place(texture, scale, offset, canvas), out, name)
    written += write_facings(place_mask(mask, scale, offset, canvas), out, name, 'm')
    natural = place(cut, scale, offset, canvas)
    icon = natural.crop(natural.getbbox())
    longest = max(icon.size)
    icon = icon.convert('RGBa').resize((round(icon.width * ICON_SIZE / longest), round(icon.height * ICON_SIZE / longest)), Image.LANCZOS).convert('RGBA')
    square = Image.new('RGBA', (ICON_SIZE, ICON_SIZE), (0, 0, 0, 0))
    square.alpha_composite(icon, ((ICON_SIZE - icon.width) // 2, (ICON_SIZE - icon.height) // 2))
    icon_path = out / f'{name}_icon.png'
    square.save(icon_path, optimize=True)
    written.append(icon_path)
    transform = {'scale': scale, 'offset': list(offset), 'canvas': list(canvas), 'master_size': list(cut.size), 'colours': colours}
    (out / f'{name}.transform.json').write_text(json.dumps(transform, indent=2), encoding='utf-8')
    width_cells = (right - left) * scale / PIXELS_PER_CELL
    record(out, name, master, written, {'kind': 'body', 'cells_long': cells_long, 'draw_size': list(draw), 'width_cells': round(width_cells, 2), 'colours': colours,
                                        'min_saturation': min_saturation, 'holes': [list(point) for point in holes]})
    return {'width_cells': width_cells, 'colours': colours, 'files': written}


def export_overlay(master, like, out, name, fit=None, marks=False, holes=()):
    out.mkdir(parents=True, exist_ok=True)
    transform = json.loads(Path(like).read_text(encoding='utf-8'))
    cut = marks_to_alpha(Image.open(master).convert('RGB')) if marks else white_to_alpha(Image.open(master), holes=holes)
    if cut.size != tuple(transform['master_size']):
        raise ValueError(f'{master} is {cut.size}, but the body was made from {transform["master_size"]}: the two would not register')
    if fit:
        content = cut.crop(cut.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox())
        box_width, box_height = fit[2] - fit[0], fit[3] - fit[1]
        factor = min(box_width / content.width, box_height / content.height)
        size = (round(content.width * factor), round(content.height * factor))
        content = content.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')
        cut = Image.new('RGBA', cut.size, (0, 0, 0, 0))
        cut.alpha_composite(content, (fit[0] + (box_width - size[0]) // 2, fit[1] + (box_height - size[1]) // 2))
    written = write_facings(place(cut, transform['scale'], tuple(transform['offset']), tuple(transform['canvas'])), out, name)
    record(out, name, master, written, {'kind': 'marks' if marks else 'overlay', 'like': Path(like).name, 'fit': fit,
                                        'holes': [list(point) for point in holes]})
    return {'files': written}


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest='command', required=True)
    body = commands.add_parser('body')
    body.add_argument('master', type=Path)
    body.add_argument('--out', type=Path, required=True)
    body.add_argument('--name', required=True)
    body.add_argument('--cells-long', type=int, required=True)
    body.add_argument('--draw', type=int, nargs=2, required=True, metavar=('X', 'Z'))
    body.add_argument('--main-hue', type=float, nargs=2, default=(38, 95), help='hue range of the main paint, in degrees')
    body.add_argument('--accent-hue', type=float, nargs=2, default=(8, 34), help='hue range of the accent paint, in degrees')
    body.add_argument('--min-saturation', type=float, default=MIN_SATURATION, help='greyer than this is never paint; lower it for a greyish paint')
    body.add_argument('--hole', type=int, nargs=2, action='append', default=[], metavar=('X', 'Y'),
                      help='a point of background enclosed by the vehicle, in master pixels, cleared as the edge is; repeatable')
    overlay = commands.add_parser('overlay')
    overlay.add_argument('master', type=Path)
    overlay.add_argument('--like', type=Path, required=True)
    overlay.add_argument('--out', type=Path, required=True)
    overlay.add_argument('--name', required=True)
    overlay.add_argument('--fit', type=int, nargs=4, metavar=('LEFT', 'TOP', 'RIGHT', 'BOTTOM'))
    overlay.add_argument('--marks', action='store_true', help='damage marks: keep only their darkness, as near-black with alpha')
    overlay.add_argument('--hole', type=int, nargs=2, action='append', default=[], metavar=('X', 'Y'),
                         help='a point of background enclosed by the part, in master pixels, cleared as the edge is; repeatable')
    arguments = parser.parse_args()
    if arguments.command == 'body':
        result = export_body(arguments.master, arguments.out, arguments.name, arguments.cells_long, arguments.draw, arguments.main_hue, arguments.accent_hue,
                             arguments.min_saturation, [tuple(point) for point in arguments.hole])
        print(f'{arguments.name}: {result["width_cells"]:.2f} cells wide; Def colours {result["colours"]}')
    else:
        result = export_overlay(arguments.master, arguments.like, arguments.out, arguments.name, arguments.fit, arguments.marks,
                                [tuple(point) for point in arguments.hole])
    for path in result['files']:
        print(f'  {path}')


if __name__ == '__main__':
    main()
