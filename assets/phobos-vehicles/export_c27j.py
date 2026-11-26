"""Export the C-27J Spartan's art at the game's scale: body, paint masks, icon, and the two propeller pictures Framework's
CompPhobosPropellers draws. Free, processor only, Pillow and numpy; never changes a master. Modelled on export_osprey.py. Stages into
the art tools' working folder (K: on the owner's PC) with a review card, or ships with --out.

    python assets/phobos-vehicles/export_c27j.py                                                  stage, with the review card
    python assets/phobos-vehicles/export_c27j.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles/C27J   ship (after review)

What it makes, from the design (docs/development/vehicle-c27j.md, "Art") and the owner's picks of 26 Nov 2026 (body try 1, the
propeller's try 4 with its outline redrawn by script; requests/c27j-paid-record.md):

  C27J_north/east/south (+m)   the body, 22.7 m nose to tail [S] at 1.175 cells a metre (owner's card: the Osprey's scale), on a 34 x 27
                               cell canvas at 120 px a cell (at 128 the canvas would pass 4,096 px; the design's "about 120"); east and
                               south are the north turned (Vehicle Framework mirrors east for west); paint mask, icon, transform
  C27J_Propeller               the still propeller from straight above, edge on, centred on its hub (the blade plane at the spinner's
                               base), 4.115 m across [P, EASA]
  C27J_PropellerSpin           the same turning: a see-through blur of the blades by script, the spinner kept crisp
  C27J_Damage<Part>            each part's damage look (27 Nov 2026): the bulk tanker's marks clipped to the part's mask from Segment
                               Anything (requests/c27j-parts.json), shown by Framework's damage looks below 60% health
  C27J_DamageGlass             cracks over the cockpit's glass, by assets/vehicle-systems/lay_cracked_glass.py, as every vehicle's
  C27J.layout.json             the hubs, the propellers' drawSize and the footprint's reach, in cells from the body's centre (x right, y
                               forward, facing north): the figures the Def's CompProperties_PhobosPropellers takes

The body's two spinners are darkened to black by script before export (the owner's description of the real one, 26 Nov 2026: "ALL
BLACK" spinner), so the propeller's own black spinner, narrower than the one the body drew, leaves no grey sliver beside it. The
grey airframe is the main paint; it has no second colour (no band, no stripe), so the masks are main paint only. Paint is decided by
region. The Segment Anything repair was tried and is not used (26 Nov 2026): its parts map of c27j-body-at-scale.png, made by
`paint_segments.py --cpu` beside the game in 13 minutes, moved 1,624 pixels of the north mask, and `paint_check.py`'s check found 0 visible
flecks both ways: one paint and no accent leave regions nothing to get wrong. The map is kept for the damage looks.
"""
import argparse
import hashlib
import json
import math
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402
import export_vehicle as ev  # noqa: E402
from export_sprite import ai_background, file_sha256, white_to_alpha  # noqa: E402

G = HERE / 'generated'
PICK = G / 'c27j-body/c27j-body-1.png'
PROPELLER = G / 'c27j-propeller/c27j-propeller-4-outlined.png'
NAME = 'C27J'
PIXELS_PER_CELL = 120             # The design's "about 120 px a cell": 34 cells at 128 would be 4,352 px, past the 4K texture limit
CELLS_A_METRE = 1.175             # The Osprey's scale (owner's card, 26 Nov 2026)
LENGTH_M, PROPELLER_M = 22.7, 4.115   # [S] length; [P] EASA type certificate data, six blades
CELLS_LONG = LENGTH_M * CELLS_A_METRE
DRAW = (34, 27)                   # The body's canvas in cells; the airframe is about 33.5 x 26.7
FOOTPRINT = (5, 11)               # The Def's size, for the layout's check of what stands over it
# Measured 26 Nov 2026 on the pick: the airframe's grey at hue 230-260, saturation 0.02-0.09; the cockpit glazing darker and bluer.
# No second colour anywhere, so no accent range (an empty one).
PAINT = {'main_hue': (200, 280), 'accent_hue': (999, 999), 'min_saturation': 0.0, 'main_max_saturation': 0.15}
LINES = (0.45, 9)                 # As the Osprey's: seams and rivets taken whole with the paint around them (export_osprey.py)
HEADROOM = 1.03                   # The body is pre-shrunk to this much above its drawn size, so the exporter still only shrinks
# The defensive aids pods (Vehicles 0.127.x; requests/c27j-overlays-record.md): the owner's pick of the redraw, try 4, drawn alone on white
# and laid here, one on each main-gear fairing, the right the left turned over. Agent placements, measured on the pick: the left fairing's
# middle, and a pod about 1.4 m long at the pick's 148.7 px a metre; the fuselage's centre line is midway between the spinners.
POD = G / 'c27j-defence-pod-redo/c27j-defence-pod-redo-4.png'
POD_AT = (2017, 2045)
POD_LENGTH_PX = 210
# The MC-27J gun kit's barrel (Vehicles 0.128.0): the owner's pick, try 1 ("Use the slim barrel on the gun kit"), drawn alone on white,
# pointing up. Vehicle Framework draws a turret's picture about its centre, so the collar, where the gun turns, is put at the centre of a
# 4-cell square, the barrel reaching 1.6 m to its muzzle (agent's figure for the part beyond the door). Measured on the pick: the collar's
# middle at (848, 2230), the muzzle's tip at y 173.
GUN = G / 'c27j-gun-barrel/c27j-gun-barrel-1.png'
GUN_PIVOT = (848, 2230)
GUN_TIP_Y = 173
GUN_LENGTH_M = 1.6
TURRET_CELLS = 4
# The fire-fighting tank's discharge pipe (Vehicles 0.128.0): the owner's pick, try 2, drawn rising from its flange and bending left; turned
# a quarter anticlockwise and laid with its flange (measured at (1147, 2240)) on the left jump door, so it reaches out and bends aft, about
# 1.4 m long (agent's figure). The door, measured on the body pick: (2068, 2410).
PIPE = G / 'c27j-spray-boom/c27j-spray-boom-2.png'
PIPE_FLANGE = (1147, 2240)
PIPE_LENGTH_PX = 208
DOOR_AT = (2068, 2410)
# The spinners in the pick's pixels, measured 26 Nov 2026: each a cone from its tip (y 819) to its base line (y 968), 142 px across at
# the base, centred on x 1635 and 2974.5 (the nacelles' own centres).
SPINNERS = {'Left': (1635.0, 819, 968), 'Right': (2974.5, 819, 968)}
SPINNER_HALF_WIDTH = 80           # px either side of a spinner's centre that may be darkened (its base is 71 px either side)
# The propeller picture: the blade plane is taken at the spinner's base, where the blades root (measured: y 955 on the centre line).
PROPELLER_PLANE_Y = 955
PROPELLER_DRAW = (5.0, 2.5)       # cells: across the blades (4.84 with the outline) and fore and aft, centred on the hub
SPIN_ALPHA = (0.22, 0.62)         # The blur's opacity at the hub and at most toward the tips [A]
PREVIEW = {'soil': (92, 76, 58), 'stone': (48, 48, 52)}
# Damage looks (27 Nov 2026; the owner saw the parts cut: "the damage looks appears to be okay"): the bulk tanker's marks, as the
# Osprey's, clipped to each part's mask from Segment Anything (requests/c27j-parts.json), one picture per part of the Def.
MARKS = {'body': G / 'bulk-tanker-damage-body/bulk-tanker-damage-body-1.png',
         'tank': G / 'bulk-tanker-damage-tank/bulk-tanker-damage-tank-1.png'}
# (the Def's part, its masks, marks master, turned for a wide part, where in the marks to start): agent placements, each starting
# somewhere of its own so neighbouring parts never repeat the same marks
DAMAGE_PARTS = [('Cockpit', ['Cockpit'], 'body', True, (0, 0)),
                ('ForwardHold', ['ForwardHold'], 'tank', True, (300, 0)),
                ('CentreHold', ['CentreHold'], 'body', True, (600, 0)),
                ('AftHold', ['AftHold'], 'tank', False, (0, 0)),
                ('LeftWing', ['LeftWing'], 'tank', True, (0, 0)),
                ('RightWing', ['RightWing'], 'body', True, (200, 0)),
                ('LeftNacelle', ['LeftNacelle'], 'body', False, (100, 300)),
                ('RightNacelle', ['RightNacelle'], 'tank', False, (300, 500)),
                ('LeftMainGear', ['LeftMainGear'], 'tank', False, (200, 700)),
                ('RightMainGear', ['RightMainGear'], 'body', False, (100, 900)),
                ('Tail', ['Tail'], 'body', True, (400, 300))]
CRACKS = ROOT / 'assets/vehicle-systems/generated/cracked-windscreen/cracked-windscreen-1.png'   # One master for every vehicle's glass


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def white_mask(rgb):
    r, g, b = rgb.split()
    return ImageChops.multiply(ImageChops.multiply(r.point(lambda v: 255 if v >= 235 else 0), g.point(lambda v: 255 if v >= 235 else 0)),
                               b.point(lambda v: 255 if v >= 235 else 0))


def darken_spinners(pick):
    """The body's spinners in black: every non-white pixel of each cone, its shading kept as a dark ramp."""
    a = np.asarray(pick).astype(np.float32)
    for x, top, base in SPINNERS.values():
        x0, x1 = int(x - SPINNER_HALF_WIDTH), int(x + SPINNER_HALF_WIDTH) + 1
        block = a[top - 4:base, x0:x1]
        lum = block @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
        inside = lum < 235
        dark = (14 + lum * 0.22)[..., None]
        block[inside] = np.minimum(block[inside], np.repeat(dark, 3, axis=-1)[inside])
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def prepare_body(work):
    """The pick with black spinners, shrunk to just above its drawn size, with the background AI's mask shrunk alike and filed under it."""
    pick = darken_spinners(Image.open(PICK).convert('RGB'))
    box = ImageChops.invert(white_mask(pick)).getbbox()
    prescale = CELLS_LONG * PIXELS_PER_CELL * HEADROOM / (box[3] - box[1])
    size = (round(pick.width * prescale), round(pick.height * prescale))
    path = work / 'c27j-body-at-scale.png'
    pick.resize(size, Image.LANCZOS).save(path)
    mask = art_paths.find('bgmasks', f'{file_sha256(PICK)}.png')
    if mask is None:
        raise SystemExit('the background AI has not looked at the pick: run clean_backgrounds.py masks --cpu --also <pick> first')
    target = art_paths.work('bgmasks', f'{file_sha256(path)}.png')
    target.parent.mkdir(parents=True, exist_ok=True)
    Image.open(mask).convert('L').resize(size, Image.LANCZOS).save(target)
    return path, prescale


class Mapping:
    """The pick's pixels to the body texture's pixels and to cells from its centre (x right, y forward)."""

    def __init__(self, prescale, transform):
        self.total = prescale * transform['scale']
        self.offset, self.canvas = transform['offset'], transform['canvas']

    def cells(self, x, y):
        tx, ty = x * self.total + self.offset[0], y * self.total + self.offset[1]
        return round((tx - self.canvas[0] / 2) / PIXELS_PER_CELL, 3), round(-(ty - self.canvas[1] / 2) / PIXELS_PER_CELL, 3)


def pod_overlay(mapping):
    """The two defensive aids pods on the body's canvas: cut from their white ground, the soft edge taken in a pixel as the Osprey's
    overlays are, shrunk premultiplied, the right pod the left mirrored about the centre line."""
    rgb = Image.open(POD).convert('RGB')
    cut = white_to_alpha(rgb, background=ai_background(POD))
    part = cut.crop(cut.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox())
    part.putalpha(part.getchannel('A').filter(ImageFilter.MinFilter(3)))
    factor = POD_LENGTH_PX / part.height * mapping.total
    size = (max(1, round(part.width * factor)), max(1, round(part.height * factor)))
    small = part.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')
    centre_line = (SPINNERS['Left'][0] + SPINNERS['Right'][0]) / 2
    canvas = Image.new('RGBA', tuple(mapping.canvas), (0, 0, 0, 0))
    for x, picture in ((POD_AT[0], small), (2 * centre_line - POD_AT[0], small.transpose(Image.FLIP_LEFT_RIGHT))):
        tx, ty = x * mapping.total + mapping.offset[0], POD_AT[1] * mapping.total + mapping.offset[1]
        canvas.alpha_composite(picture, (round(tx - picture.width / 2), round(ty - picture.height / 2)))
    return canvas


def cut_part(path):
    """A part drawn alone on white, cut out with the background AI's mask, its soft edge taken in a pixel; with its crop's offset."""
    cut = white_to_alpha(Image.open(path).convert('RGB'), background=ai_background(path))
    box = cut.getchannel('A').point(lambda alpha: 255 if alpha >= 128 else 0).getbbox()
    part = cut.crop(box)
    part.putalpha(part.getchannel('A').filter(ImageFilter.MinFilter(3)))
    return part, box


def premultiplied(picture, factor):
    size = (max(1, round(picture.width * factor)), max(1, round(picture.height * factor)))
    return picture.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')


def gun_turret():
    """The gun kit's barrel as Vehicle Framework's turret picture: barrel up, the collar at the centre of a TURRET_CELLS square."""
    part, box = cut_part(GUN)
    side = TURRET_CELLS * PIXELS_PER_CELL
    factor = GUN_LENGTH_M * CELLS_A_METRE * PIXELS_PER_CELL / (GUN_PIVOT[1] - GUN_TIP_Y)
    small = premultiplied(part, factor)
    canvas = Image.new('RGBA', (side, side), (0, 0, 0, 0))
    px, py = (GUN_PIVOT[0] - box[0]) * factor, (GUN_PIVOT[1] - box[1]) * factor
    canvas.alpha_composite(small, (round(side / 2 - px), round(side / 2 - py)))
    return canvas


def pipe_overlay(mapping):
    """The tank's pipe on the body's canvas: turned a quarter anticlockwise, its flange on the left jump door."""
    part, box = cut_part(PIPE)
    turned = part.rotate(90, expand=True)
    # A quarter turn anticlockwise sends a point (x, y) of the part to (y, width - 1 - x).
    fx, fy = PIPE_FLANGE[1] - box[1], part.width - 1 - (PIPE_FLANGE[0] - box[0])
    factor = PIPE_LENGTH_PX / turned.width * mapping.total
    small = premultiplied(turned, factor)
    canvas = Image.new('RGBA', tuple(mapping.canvas), (0, 0, 0, 0))
    tx, ty = DOOR_AT[0] * mapping.total + mapping.offset[0], DOOR_AT[1] * mapping.total + mapping.offset[1]
    canvas.alpha_composite(small, (round(tx - fx * factor), round(ty - fy * factor)))
    return canvas


def damage_look(mapping, folder, parts, source, rotate, start):
    """A part's damage on the body's canvas: marks clipped to its mask on the pick, shrunk premultiplied by the pick's own mapping,
    so a hole is the tanker's size in cells (export_vehicle.damage_marks)."""
    mask = Image.new('L', Image.open(PICK).size, 0)
    for part in parts:
        mask = ImageChops.lighter(mask, Image.open(folder / f'{part}.png').convert('L'))
    box = mask.getbbox()
    marks = ev.clip_marks(ev.damage_marks(MARKS[source], mapping.total, PIXELS_PER_CELL, rotate), mask, box, start)
    small = premultiplied(marks, mapping.total)
    canvas = Image.new('RGBA', tuple(mapping.canvas), (0, 0, 0, 0))
    tx, ty = box[0] * mapping.total + mapping.offset[0], box[1] * mapping.total + mapping.offset[1]
    canvas.alpha_composite(small, (max(0, round(tx)), max(0, round(ty))), (max(0, -round(tx)), max(0, -round(ty))))
    return canvas


def cracked_glass(out):
    """Cracks over the cockpit's own glass, by the one tool every vehicle's glass uses (lay_cracked_glass.py), from the shipped north."""
    tool = ROOT / 'assets/vehicle-systems/lay_cracked_glass.py'
    subprocess.run([sys.executable, str(tool), str(CRACKS), str(out / f'{NAME}_north.png'), str(out / f'{NAME}_DamageGlass')], check=True)
    return [out / f'{NAME}_DamageGlass_{facing}.png' for facing in ev.FACINGS]


def damage_card(out, colour, path):
    """Every damage look at once over the body, tinted, on dark ground; in game each shows only as its own part wears."""
    body = tinted(Image.open(out / f'{NAME}_north.png').convert('RGBA'), Image.open(out / f'{NAME}_northm.png'), colour)
    canvas = Image.new('RGBA', body.size, PREVIEW['stone'] + (255,))
    canvas.alpha_composite(body)
    plain = canvas.copy()
    for name, *_ in DAMAGE_PARTS:
        canvas.alpha_composite(Image.open(out / f'{NAME}_Damage{name}_north.png'))
    canvas.alpha_composite(Image.open(out / f'{NAME}_DamageGlass_north.png'))
    half = (body.width // 2, body.height // 2)
    card = Image.new('RGB', (half[0] * 2 + 30, half[1]), (30, 30, 32))
    card.paste(plain.resize(half, Image.LANCZOS).convert('RGB'), (0, 0))
    card.paste(canvas.resize(half, Image.LANCZOS).convert('RGB'), (half[0] + 30, 0))
    path.parent.mkdir(parents=True, exist_ok=True)
    card.save(path)
    return path


def propeller_cut():
    """The picked propeller cut out of its white ground, with the spinner's own mask (the cone, continued under the blades' roots)."""
    rgb = Image.open(PROPELLER).convert('RGB')
    cut = white_to_alpha(rgb, background=ai_background(PROPELLER))
    alpha = np.asarray(cut.getchannel('A')) >= 128
    rows, cols = np.nonzero(alpha)
    left, right, top = cols.min(), cols.max(), rows.min()
    centre = (left + right) / 2
    # The cone's half width on each row down to where the blades begin, then carried on at the same taper to the plane.
    widths = {}
    for y in range(top, PROPELLER_PLANE_Y + 1):
        run = alpha[y]
        c = int(round(centre))
        if not run[c]:
            continue
        a = c
        while a > 0 and run[a - 1]:
            a -= 1
        b = c
        while b < run.size - 1 and run[b + 1]:
            b += 1
        widths[y] = (b - a) / 2
    rows_sorted = sorted(widths)
    # The blades begin where the run on the centre line jumps wider from one row to the next (the cone widens a pixel or so a row).
    blades_from = next((y for y in rows_sorted[1:] if widths[y] - widths.get(y - 1, widths[y]) > 20), PROPELLER_PLANE_Y)
    before = [y for y in rows_sorted if y < blades_from - 5]
    y0, y1 = before[-30], before[-1]
    slope = (widths[y1] - widths[y0]) / max(1, y1 - y0)
    spinner = np.zeros_like(alpha)
    for y in range(top, PROPELLER_PLANE_Y + 1):
        half = widths[y] if y <= y1 else widths[y1] + slope * (y - y1)
        spinner[y, max(0, int(centre - half)):int(centre + half) + 1] = True
    spinner &= alpha
    return cut, spinner, {'centre_x': float(centre), 'span_px': int(right - left), 'tip_y': int(top), 'blades_from_y': int(blades_from)}


def propeller_spin(cut, spinner, centre_x):
    """A turning propeller seen edge on: each blade sweeps the width of the disc. At a distance x from the hub a blade spends time in
    proportion to 1 / sqrt(r^2 - x^2) (plain geometry of a point on a turning circle), so the blur is fainter by the hub and densest
    toward the tips; fore and aft it covers the blades' own depth at that distance. The spinner stays crisp on top."""
    a = np.asarray(cut).astype(np.float32)
    alpha = a[..., 3] >= 128
    blades = alpha & ~spinner
    height, width = alpha.shape
    radius = max(centre_x - np.nonzero(blades.any(0))[0].min(), np.nonzero(blades.any(0))[0].max() - centre_x)
    band = np.zeros((height, width), np.float32)
    for x in range(width):
        d = abs(x - centre_x)
        if d > radius:
            continue
        mirror = int(round(2 * centre_x - x))
        rows = np.nonzero(blades[:, x] | (blades[:, mirror] if 0 <= mirror < width else False))[0]
        if rows.size == 0:
            continue
        u = min(d / radius, 0.985)
        density = 1 / math.sqrt(1 - u * u)
        band[rows.min():rows.max() + 1, x] = min(SPIN_ALPHA[1], SPIN_ALPHA[0] * density)
    band = np.asarray(Image.fromarray((band * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6))).astype(np.float32) / 255
    out = np.zeros((height, width, 4), np.uint8)
    out[..., :3] = (18, 18, 20)
    out[..., 3] = (band * 255).astype(np.uint8)
    blur = Image.fromarray(out, 'RGBA')
    crisp = cut.copy()
    crisp.putalpha(Image.fromarray(np.where(spinner, a[..., 3], 0).astype(np.uint8)))
    blur.alpha_composite(crisp)
    return blur


def to_game(picture, centre_x, factor):
    """Shrunk to the game's scale and placed on a PROPELLER_DRAW canvas with the hub (centre x, the blade plane) at its centre."""
    side_x, side_y = round(PROPELLER_DRAW[0] * PIXELS_PER_CELL), round(PROPELLER_DRAW[1] * PIXELS_PER_CELL)
    half_x, half_y = side_x / 2 / factor, side_y / 2 / factor
    box = (round(centre_x - half_x), round(PROPELLER_PLANE_Y - half_y), round(centre_x + half_x), round(PROPELLER_PLANE_Y + half_y))
    crop = Image.new('RGBA', (box[2] - box[0], box[3] - box[1]), (0, 0, 0, 0))
    crop.alpha_composite(picture.crop((max(0, box[0]), max(0, box[1]), min(picture.width, box[2]), min(picture.height, box[3]))),
                         (max(0, -box[0]), max(0, -box[1])))
    reach = picture.getchannel('A').point(lambda v: 255 if v >= 16 else 0).getbbox()
    if reach[0] < box[0] or reach[1] < box[1] or reach[2] > box[2] or reach[3] > box[3]:
        raise SystemExit(f'the propeller {reach} reaches past its canvas {box}: widen PROPELLER_DRAW')
    return crop.convert('RGBa').resize((side_x, side_y), Image.LANCZOS).convert('RGBA')


def tinted(texture, mask, colour):
    t = np.asarray(texture).astype(np.float32)
    painted = np.asarray(mask.convert('RGB'))[..., 0] > 127
    t[painted, :3] *= np.array(colour, np.float32) / 255
    return Image.fromarray(t.clip(0, 255).astype(np.uint8), 'RGBA')


def review_card(out, layout, colour, path):
    """The body tinted in its own colour, with both propellers on their hubs, still and turning, on soil and stone; and the nacelles
    enlarged three times without smoothing."""
    body = tinted(Image.open(out / f'{NAME}_north.png').convert('RGBA'), Image.open(out / f'{NAME}_northm.png'), colour)
    still = Image.open(out / f'{NAME}_Propeller.png').convert('RGBA')
    spin = Image.open(out / f'{NAME}_PropellerSpin.png').convert('RGBA')
    panels = []
    for label, prop in (('still', still), ('turning', spin)):
        for ground in PREVIEW.values():
            canvas = Image.new('RGBA', body.size, ground + (255,))
            canvas.alpha_composite(body)
            for hx, hy in layout['propellers']['hubs']:
                x = round(body.width / 2 + hx * PIXELS_PER_CELL - prop.width / 2)
                y = round(body.height / 2 - hy * PIXELS_PER_CELL - prop.height / 2)
                canvas.alpha_composite(prop, (x, y))
            hx, hy = layout['propellers']['hubs'][0]
            cx, cy = body.width / 2 + hx * PIXELS_PER_CELL, body.height / 2 - hy * PIXELS_PER_CELL
            near = canvas.crop((round(cx - 380), round(cy - 260), round(cx + 380), round(cy + 340)))
            panels.append((label, canvas.resize((canvas.width // 4, canvas.height // 4), Image.LANCZOS),
                           near.resize((near.width, near.height), Image.NEAREST)))
    pad = 30
    row_w = max(w.width + n.width for _, w, n in panels) + 3 * pad
    row_h = max(max(w.height, n.height) for _, w, n in panels) + pad
    card = Image.new('RGB', (row_w * 2, row_h * 2 + pad), (30, 30, 32))
    for i, (label, whole, near) in enumerate(panels):
        x0, y0 = (i % 2) * row_w + pad, (i // 2) * row_h + pad
        card.paste(whole.convert('RGB'), (x0, y0))
        card.paste(near.convert('RGB'), (x0 + whole.width + pad, y0))
    path.parent.mkdir(parents=True, exist_ok=True)
    card.save(path)
    return path


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', type=Path, help='where to write the textures (default: the art working folder, staged)')
    args = parser.parse_args(argv)
    ev.PIXELS_PER_CELL = PIXELS_PER_CELL       # export_body reads it at call time
    work = art_paths.work('c27j-export')
    out = (ROOT / args.out) if args.out and not args.out.is_absolute() else (args.out or work / 'textures')
    work.mkdir(parents=True, exist_ok=True)
    out.mkdir(parents=True, exist_ok=True)

    body_master, prescale = prepare_body(work)
    result = ev.export_body(body_master, out, NAME, CELLS_LONG, DRAW, PAINT['main_hue'], PAINT['accent_hue'], PAINT['min_saturation'],
                            main_max_saturation=PAINT['main_max_saturation'], trim=1, paint='regions', line=LINES)
    files = list(result['files'])
    mapping = Mapping(prescale, result['transform'])
    print(f'body: {result["width_cells"]:.2f} cells wide; Def colours {result["colours"]}', flush=True)

    cut, spinner, measured = propeller_cut()
    factor = PROPELLER_M * CELLS_A_METRE * PIXELS_PER_CELL / measured['span_px']
    for name, picture in (('Propeller', cut), ('PropellerSpin', propeller_spin(cut, spinner, measured['centre_x']))):
        path = out / f'{NAME}_{name}.png'
        to_game(picture, measured['centre_x'], factor).save(path, optimize=True)
        files.append(path)

    files += ev.write_facings(pod_overlay(mapping), out, f'{NAME}_DefensiveAids')
    files += ev.write_facings(pipe_overlay(mapping), out, f'{NAME}_FireTank')
    gun_path = out / f'{NAME}_GunKit.png'
    gun_turret().save(gun_path, optimize=True)
    files.append(gun_path)
    parts = art_paths.find('parts', sha(PICK))
    for name, masks, source, rotate, start in DAMAGE_PARTS:
        files += ev.write_facings(damage_look(mapping, parts, masks, source, rotate, start), out, f'{NAME}_Damage{name}')
    files += cracked_glass(out)
    hubs = {side: mapping.cells(x, base) for side, (x, _, base) in SPINNERS.items()}
    across = round((abs(hubs['Left'][0]) + abs(hubs['Right'][0])) / 2, 2)
    forward = round((hubs['Left'][1] + hubs['Right'][1]) / 2, 2)
    nose = mapping.cells(0, ImageChops.invert(white_mask(Image.open(PICK).convert('RGB'))).getbbox()[1])[1]
    layout = {'body': {'cells_long': round(CELLS_LONG, 3), 'draw_size': list(DRAW), 'pixels_a_cell': PIXELS_PER_CELL,
                       'width_cells': round(result['width_cells'], 2), 'colours': result['colours'], 'pick_to_texture': mapping.total},
              'propellers': {'draw_size': list(PROPELLER_DRAW), 'hubs': [[-across, forward], [across, forward]],
                             'hubs_as_measured': hubs, 'hub_is': "the blade plane at the spinner's base, on the nacelle's centre line",
                             'picture': measured},
              'footprint': {'size': list(FOOTPRINT), 'reaches_forward_cells': FOOTPRINT[1] / 2, 'nose_at_cells': nose,
                            'half_width_cells': FOOTPRINT[0] / 2, 'fuselage_half_width_cells': round(3.70 / 2 * CELLS_A_METRE, 2)}}
    (out / f'{NAME}.layout.json').write_text(json.dumps(layout, indent=2), encoding='utf-8')
    record = {'tool': 'assets/phobos-vehicles/export_c27j.py', 'pick': PICK.relative_to(ROOT).as_posix(), 'pick_sha256': sha(PICK),
              'propeller': PROPELLER.relative_to(ROOT).as_posix(), 'propeller_sha256': sha(PROPELLER),
              'made': 'Made with generative AI (Google Nano Banana Pro through fal.ai, the owner\'s picks of 26 Nov 2026), then exported by '
                      'script: cut out, black spinners, paint by region, the propeller blur by script',
              'prescale': prescale, 'paint': PAINT, 'pixels_a_cell': PIXELS_PER_CELL, 'files': {p.name: sha(p) for p in files}}
    art_paths.record_path(out / f'{NAME}.c27j-export.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    card = review_card(out, layout, tuple(result['colours']['color']), art_paths.work('cards', 'c27j-export') / '01-c27j-body-propellers.png')
    damage_card(out, tuple(result['colours']['color']), art_paths.work('cards', 'c27j-export') / '02-c27j-damage.png')
    print(f'hubs {layout["propellers"]["hubs"]} (measured {hubs}); propeller drawSize {list(PROPELLER_DRAW)}')
    print(f'{len(files)} textures in {out}; card: {card}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
