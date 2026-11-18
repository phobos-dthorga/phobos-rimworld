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
reproduces the master is written to the transform file for the Def. Mask red is the main paint, green the accent. Paint is decided a
region at a time, never a pixel at a time (owner, 16 Nov 2026: the Pintle bowser's per-pixel mask was speckled): see region_kinds.
"repaint" makes a body's facings and masks again on its saved transform (scripts/art/repaint_vehicles.py drives it for every body).
"""
import argparse
import colorsys
import datetime as _dt
from fractions import Fraction
import hashlib
import json
from pathlib import Path
import sys

from PIL import Image, ImageChops, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
from export_sprite import ai_background, file_sha256, white_to_alpha  # noqa: E402

PIXELS_PER_CELL = 128
ICON_SIZE = 64
FACINGS = {'north': None, 'east': Image.ROTATE_270, 'south': Image.ROTATE_180}
BRIGHTEST_GREY = 235  # The brightest paint in the texture, leaving headroom for the shader.
MARK_COLOUR = (22, 19, 17)  # Damage marks keep only their darkness: soot and holes in near-black, however pale their edges were drawn.
MARK_FLOOR = 14  # Paler than this much darkness is the white background, not a mark.
OUTLINE_VALUE = 0.24  # Darker than this is outline or shadow, never paint.
MIN_SATURATION = 0.16  # Greyer than this is bare metal, glass or rubber, never paint.
GREY_ANY_HUE = 0.04  # For a grey paint: this colourless or less is the paint whatever its hue (authored, 19 Nov 2026).


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def classify(red, green, blue, main_hue, accent_hue, min_saturation=MIN_SATURATION, main_max_saturation=1.0):
    """'main', 'accent' or None for one opaque pixel; hues are in degrees. A greyish paint, such as a grey-blue hull, needs a lower
    min_saturation than the default, which keeps bare metal unpainted. main_max_saturation keeps glass of the paint's own hue out of a
    grey paint (the Osprey, 19 Nov 2026: its airframe at saturation 0.02 to 0.07, its windows of the same hue at 0.27)."""
    hue, saturation, value = colorsys.rgb_to_hsv(red / 255, green / 255, blue / 255)
    if value < OUTLINE_VALUE or saturation < min_saturation:
        return None
    degrees = hue * 360
    # A grey paint (main_max_saturation set): a near-colourless pixel has no hue worth reading, so it is the paint whatever its hue;
    # otherwise the slivers of neutral grey beside seam lines stay unpainted and show as pale flecks (the Osprey, 19 Nov 2026).
    grey = main_max_saturation < 1.0 and saturation <= GREY_ANY_HUE
    if (grey or main_hue[0] <= degrees <= main_hue[1]) and saturation <= main_max_saturation:
        return 'main'
    if accent_hue[0] <= degrees <= accent_hue[1]:
        return 'accent'
    return None


def luminance(red, green, blue):
    return 0.299 * red + 0.587 * green + 0.114 * blue


REGION_STEP = 24  # Neighbours closer than this in every channel are one region: cel shading steps within a panel, not a new panel.
SMALL_REGION = 24  # A region of fewer pixels is a blend at a seam: it is judged by its own mean colour, not by a vote.
REPAIR_MAX = 400   # master pixels: the largest island a parts map may repaint (authored; a fleck in the shipped texture is 12 pixels or fewer)
REPAIR_SURE = 0.8  # the share of a part that must have one paint before an island in it is repainted (as paint_check.PART_SURE)
DARK_SPECK_MAX = 40  # master pixels: a dark speck no larger, wholly inside a sure part, is weathering, not outline (authored)


def thin_lines(image, line):
    """Dark marks too thin to be a panel: seam lines, hatch rims, rivets. line is (value, width): darker than value (0 to 1) and gone
    under an opening of width pixels. A wide dark panel survives the opening, so it is not caught (the Osprey, 19 Nov 2026: its seam
    lines are a lighter, purple-tinted dark than its outline, so some of their pixels were painted and a seam came out speckled).
    region_kinds gives each such mark, its darkest pixels too, the paint of the panels it touches most: left half outline and half
    paint, a hatch rim showed as grey bars among dark red ones."""
    value, width = line
    r, g, b, a = image.convert('RGBA').split()
    brightest = ImageChops.lighter(ImageChops.lighter(r, g), b)
    dark = ImageChops.multiply(brightest.point(lambda v: 255 if v < value * 255 else 0), a.point(lambda v: 255 if v >= 128 else 0))
    wide = dark.filter(ImageFilter.MinFilter(width)).filter(ImageFilter.MaxFilter(width))
    # The silhouette's own outline is as thin as a seam: anything within reach of the clear ground stays outline, as drawn.
    edge = a.point(lambda v: 255 if v < 128 else 0).filter(ImageFilter.MaxFilter(2 * width + 1))
    return ImageChops.subtract(ImageChops.subtract(dark, wide), edge)


def region_kinds(image, main_hue, accent_hue, min_saturation=MIN_SATURATION, parts=None, main_max_saturation=1.0, line=None):
    """The paint of every opaque pixel, decided a region at a time (owner, 16 Nov 2026: the Pintle bowser's per-pixel mask was speckled).
    Our art is cel shaded inside thick dark outlines, so a painted panel is a run of near-alike pixels the outline closes off. Regions
    grow between neighbours closer than REGION_STEP in every channel, never through outline; each region takes the paint most of its
    pixels have, so texture noise and a stray highlight can no longer flip single pixels. A small region (a blend where two colours meet)
    takes the paint of its own mean colour. Outline and clear pixels are never paint. Returns a list of 'main', 'accent' or None, row by row.

    With parts (Segment Anything's map of the master, flattened), a repair follows (owner, 16 Nov 2026: "Yes, repair and show me"): an
    island of up to REPAIR_MAX pixels, ringed on every side by another paint and touching no outline, inside a part at least REPAIR_SURE of
    that paint, takes the part's paint. That is the fleck checker's own test (paint_check.py) at full size, so weathering left unpainted on
    a painted hull is painted, and a bolt head drawn in its own outline is left as drawn."""
    width, height = image.size
    pixels = list(image.get_flattened_data() if hasattr(image, 'get_flattened_data') else image.getdata())
    count = width * height
    label = [-1] * count
    pixel_kind = [None] * count
    candidate = [False] * count
    # With line set, a thin dark mark (a seam, a hatch rim, a rivet) is one region of its own, whatever paint its pixels would get one
    # by one, and takes the paint of the panels it touches most: a seam reads as a darker groove in the paint along its whole length,
    # and a frame between two windows, touching only glass, stays bare (the Osprey, 19 Nov 2026: its seams came out speckled).
    lines = thin_lines(image, line) if line else None
    thin = list(lines.get_flattened_data() if hasattr(lines, 'get_flattened_data') else lines.getdata()) if line else [0] * count
    for index, (red, green, blue, alpha) in enumerate(pixels):
        if alpha >= 128 and (max(red, green, blue) / 255 >= OUTLINE_VALUE or thin[index]):
            candidate[index] = True
            pixel_kind[index] = None if thin[index] else classify(red, green, blue, main_hue, accent_hue, min_saturation,
                                                                    main_max_saturation)

    def neighbours(here):
        x, y = here % width, here // width
        if x > 0:
            yield here - 1
        if x < width - 1:
            yield here + 1
        if y > 0:
            yield here - width
        if y < height - 1:
            yield here + width

    # Regions: alike neighbours with the same paint, never across outline nor across a change of paint (so a stripe's soft end cannot
    # leak into the panel beside it and be outvoted).
    region_kind, region_size, members_of = [], [], []
    for start in range(count):
        if not candidate[start] or label[start] >= 0:
            continue
        region = len(region_kind)
        kind = pixel_kind[start]
        label[start] = region
        members = [start]
        head = 0
        while head < len(members):
            here = members[head]
            head += 1
            red, green, blue, _ = pixels[here]
            for there in neighbours(here):
                if not candidate[there] or label[there] >= 0 or pixel_kind[there] != kind or bool(thin[there]) != bool(thin[here]):
                    continue
                other = pixels[there]
                if abs(other[0] - red) <= REGION_STEP and abs(other[1] - green) <= REGION_STEP and abs(other[2] - blue) <= REGION_STEP:
                    label[there] = region
                    members.append(there)
        region_kind.append(kind)
        region_size.append(len(members))
        members_of.append(members)
    # A small region is a fleck of noise or a blend at a seam: it takes the paint of the large region it touches most. One alone in
    # outline keeps its own.
    final = list(region_kind)
    for region, members in enumerate(members_of):
        if region_size[region] >= SMALL_REGION and not thin[members[0]]:
            continue
        touches = {}
        for member in members:
            for there in neighbours(member):
                other = label[there]
                if other >= 0 and other != region and region_size[other] >= SMALL_REGION and not thin[members_of[other][0]]:
                    touches[region_kind[other]] = touches.get(region_kind[other], 0) + 1
        if touches and thin[members[0]] and None in touches:
            final[region] = None   # A thin mark touching glass or bare metal is its frame: it stays bare, as drawn
        elif touches:
            final[region] = max(touches, key=lambda key: (touches[key], key is None))
    if parts is not None:
        tallies = {}
        for index in range(count):
            if label[index] >= 0 and parts[index]:
                tally = tallies.setdefault(parts[index], {})
                kind = final[label[index]]
                tally[kind] = tally.get(kind, 0) + 1
        sure = {}
        for part, tally in tallies.items():
            kind, most = max(tally.items(), key=lambda item: item[1])
            if most / sum(tally.values()) >= REPAIR_SURE:
                sure[part] = kind
        for region, members in enumerate(members_of):
            if region_size[region] > REPAIR_MAX:
                continue
            votes = {}
            for member in members:
                if parts[member]:
                    votes[parts[member]] = votes.get(parts[member], 0) + 1
            if not votes:
                continue
            part = max(votes, key=votes.get)
            if part not in sure or final[region] == sure[part]:
                continue
            ringed = True
            for member in members:
                for there in neighbours(member):
                    other = label[there]
                    if other == region:
                        continue
                    if other < 0 or parts[there] != part or final[other] != sure[part]:
                        ringed = False
                        break
                if not ringed:
                    break
            if ringed:
                final[region] = sure[part]
    result = [final[label[index]] if label[index] >= 0 else None for index in range(count)]
    if parts is not None:
        # Dark specks: weathering drawn darker than the outline line, so never painted, and blended into half-painted pixels when the
        # picture is shrunk (the fleck checker's finding on the Crawler, 16 Nov 2026). A speck of DARK_SPECK_MAX pixels or fewer, every
        # pixel in one sure part and ringed only by that part's paint, takes it. The outline itself is long and joined up, so it never is.
        seen = [False] * count
        for start in range(count):
            if seen[start] or candidate[start] or pixels[start][3] < 128:
                continue
            seen[start] = True
            speck = [start]
            head = 0
            while head < len(speck) and len(speck) <= DARK_SPECK_MAX:
                here = speck[head]
                head += 1
                for there in neighbours(here):
                    if not seen[there] and not candidate[there] and pixels[there][3] >= 128:
                        seen[there] = True
                        speck.append(there)
            if len(speck) > DARK_SPECK_MAX:
                while head < len(speck):
                    here = speck[head]
                    head += 1
                    for there in neighbours(here):
                        if not seen[there] and not candidate[there] and pixels[there][3] >= 128:
                            seen[there] = True
                            speck.append(there)
                continue
            part = parts[speck[0]]
            if part not in sure or sure[part] is None or any(parts[member] != part for member in speck):
                continue
            members = set(speck)
            ringed = all(there in members or (candidate[there] and parts[there] == part and result[there] == sure[part])
                         for member in speck for there in neighbours(member))
            if ringed:
                for member in speck:
                    result[member] = sure[part]
    return result


import art_paths  # noqa: E402  The art tools' working folder, where Segment Anything's parts maps are (K: on the owner's PC)


def parts_map(master):
    """Segment Anything's parts map of a master (scripts/art/paint_segments.py), or FileNotFoundError."""
    found = art_paths.find('segments', f'{file_sha256(master)}.png')
    if found is None:
        raise FileNotFoundError(f'no parts map for {master}: run scripts/art/paint_segments.py on it first')
    return Image.open(found)


def part_kinds(image, labels, main_hue, accent_hue, min_saturation=MIN_SATURATION):
    """The paint of every opaque pixel, decided a part at a time from Segment Anything's label map (scripts/art/paint_segments.py): a
    part takes the paint most of its pixels have. Pixels in no part, and the outline, fall back to region_kinds. For comparison with the
    region method (owner, 16 Nov 2026: both, side by side)."""
    width, height = image.size
    fallback = region_kinds(image, main_hue, accent_hue, min_saturation)
    pixels = list(image.get_flattened_data() if hasattr(image, 'get_flattened_data') else image.getdata())
    parts = list(labels.get_flattened_data() if hasattr(labels, 'get_flattened_data') else labels.getdata())
    votes = {}
    for index, (red, green, blue, alpha) in enumerate(pixels):
        part = parts[index]
        if part == 0 or alpha < 128 or max(red, green, blue) / 255 < OUTLINE_VALUE:
            continue
        tally = votes.setdefault(part, {'main': 0, 'accent': 0, None: 0})
        tally[classify(red, green, blue, main_hue, accent_hue, min_saturation)] += 1
    decided = {part: max(tally, key=lambda key: (tally[key], key is None)) for part, tally in votes.items()}
    return [decided[parts[index]] if parts[index] in decided and pixels[index][3] >= 128 and max(pixels[index][:3]) / 255 >= OUTLINE_VALUE
            else fallback[index] for index in range(width * height)]


def separate_paint(image, main_hue, accent_hue, min_saturation=MIN_SATURATION, per_pixel=False, labels=None, repair=False,
                   main_max_saturation=1.0, line=None):
    """(grey-painted texture, mask, default colours). Each paint is scaled on its own, so its grey fills the shader's range and its
    default colour gives the master's tone back. Paint is decided by region (region_kinds) unless per_pixel asks for the old rule, kept
    for comparison only."""
    source = image.load()
    width, height = image.size
    if per_pixel:
        kinds = [None] * (width * height)
        for y in range(height):
            for x in range(width):
                red, green, blue, alpha = source[x, y]
                if alpha >= 128:
                    kinds[y * width + x] = classify(red, green, blue, main_hue, accent_hue, min_saturation, main_max_saturation)
    elif labels is not None and repair:
        flat = list(labels.get_flattened_data() if hasattr(labels, 'get_flattened_data') else labels.getdata())
        kinds = region_kinds(image, main_hue, accent_hue, min_saturation, parts=flat, main_max_saturation=main_max_saturation, line=line)
    elif labels is not None:
        kinds = part_kinds(image, labels, main_hue, accent_hue, min_saturation)
    else:
        kinds = region_kinds(image, main_hue, accent_hue, min_saturation, main_max_saturation=main_max_saturation, line=line)
    groups = {'main': [], 'accent': []}
    for y in range(height):
        for x in range(width):
            kind = kinds[y * width + x]
            if kind:
                red, green, blue, _ = source[x, y]
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
            kind = kinds[y * width + x]
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


def export_body(master, out, name, cells_long, draw, main_hue, accent_hue, min_saturation=MIN_SATURATION, holes=(),
                main_max_saturation=1.0, trim=0, paint='regions', line=None):
    """trim takes this many pixels off the silhouette's outer edge, for a master whose outline fades into the white through a ring
    of grey that shows as a pale dotted rim on dark ground (the Osprey, 19 Nov 2026: its outline is about 7 px thick at game size).
    paint 'repaired' repairs the regions' flecks with Segment Anything's parts map of the master (paint_segments.py first)."""
    out.mkdir(parents=True, exist_ok=True)
    background = ai_background(master)
    cut = white_to_alpha(Image.open(master), holes=holes, background=background)
    if trim:
        cut.putalpha(cut.getchannel('A').filter(ImageFilter.MinFilter(2 * trim + 1)))
    left, top, right, bottom = cut.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox()
    scale = cells_long * PIXELS_PER_CELL / (bottom - top)
    canvas = (draw[0] * PIXELS_PER_CELL, draw[1] * PIXELS_PER_CELL)
    offset = (round(canvas[0] / 2 - (left + right) / 2 * scale), round(canvas[1] / 2 - (top + bottom) / 2 * scale))
    labels = parts_map(master) if paint == 'repaired' else None
    texture, mask, colours = separate_paint(cut, main_hue, accent_hue, min_saturation, labels=labels, repair=labels is not None,
                                            main_max_saturation=main_max_saturation, line=line)
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
                                        'min_saturation': min_saturation, 'main_max_saturation': main_max_saturation, 'trim': trim,
                                        'line': list(line) if line else None,
                                        'main_hue': list(main_hue), 'accent_hue': list(accent_hue),
                                        'holes': [list(point) for point in holes],
                                        'ai_mask': file_sha256(master) if background is not None else None})
    return {'width_cells': width_cells, 'colours': colours, 'files': written, 'transform': transform}


def repaint_body(master, like, out, name, main_hue, accent_hue, min_saturation=MIN_SATURATION, holes=(), paint='regions'):
    """A body's texture and paint mask made again from its master on its own saved transform (scale, offset, canvas), so it registers
    with every overlay already fitted to it. paint is 'regions' (region_kinds), 'repaired' (regions, then flecks repaired by Segment
    Anything's map), 'parts' (that map alone; both need the map made first) or 'pixels' (the old per-pixel rule, for comparison). Writes the facings and masks only; the icon and transform stay."""
    out.mkdir(parents=True, exist_ok=True)
    transform = json.loads(Path(like).read_text(encoding='utf-8'))
    background = ai_background(master)
    cut = white_to_alpha(Image.open(master), holes=holes, background=background)
    if cut.size != tuple(transform['master_size']):
        raise ValueError(f'{master} is {cut.size}, but its transform was made from {transform["master_size"]}')
    labels = None
    if paint in ('parts', 'repaired'):
        labels = parts_map(master)
    texture, mask, colours = separate_paint(cut, main_hue, accent_hue, min_saturation, per_pixel=paint == 'pixels', labels=labels, repair=paint == 'repaired')
    scale, offset, canvas = transform['scale'], tuple(transform['offset']), tuple(transform['canvas'])
    written = write_facings(place(texture, scale, offset, canvas), out, name)
    written += write_facings(place_mask(mask, scale, offset, canvas), out, name, 'm')
    record(out, name, master, written, {'kind': 'body-repaint', 'paint': paint, 'like': Path(like).name, 'colours': colours,
                                        'main_hue': list(main_hue), 'accent_hue': list(accent_hue), 'min_saturation': min_saturation,
                                        'holes': [list(point) for point in holes], 'ai_mask': file_sha256(master) if background is not None else None})
    return {'colours': colours, 'files': written}


def export_overlay(master, like, out, name, fit=None, marks=False, holes=()):
    out.mkdir(parents=True, exist_ok=True)
    transform = json.loads(Path(like).read_text(encoding='utf-8'))
    background = None if marks else ai_background(master)
    cut = marks_to_alpha(Image.open(master).convert('RGB')) if marks else white_to_alpha(Image.open(master), holes=holes, background=background)
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
                                        'holes': [list(point) for point in holes],
                                        'ai_mask': file_sha256(master) if background is not None else None})
    return {'files': written}


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest='command', required=True)
    body = commands.add_parser('body')
    body.add_argument('master', type=Path)
    body.add_argument('--out', type=Path, required=True)
    body.add_argument('--name', required=True)
    # A fraction such as 20/3 keeps a body's look when only its drawSize grows (the Pintle trailer, 3 x 6 to 4 x 8, 13 Nov 2026).
    body.add_argument('--cells-long', type=lambda text: float(Fraction(text)), required=True, help='a number or a fraction, such as 5 or 20/3')
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
