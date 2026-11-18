"""Finish the V-22 Osprey's parts by script from pictures we already have, with Pillow only: no generator is called.

    python assets/phobos-vehicles/derive_osprey_parts.py

Rule 11 of the paid rules (prefer a free derivation to a paid generation), and the round's record:
assets/phobos-vehicles/requests/osprey-overlays-record.md. Reads the owner's pick, its part masks from scripts/art/name_parts.py, and
the parts lifted out by scripts/art/edit_difference.py; writes new files to assets/phobos-vehicles/generated/osprey-derived/, each
with a record. No input is changed.

  ramp-gun-50.png      the .50 gun the model drew far too large, shrunk about its mount to its real size beside the 7.62 mm gun
  rotor.png            the still rotor rebuilt from its one whole blade, turned by 120 degrees twice, with the pick's own hub cap
  rotor-spin.png       the same rotor spinning: an average of it turned through a third of a turn
  nacelle-0.png        the upright nacelle, cut from the pick by its part mask, its hub cap taken off (the rotor carries it)
  nacelle-30/60/90.png the nacelle tilting forward: its front drawn longer by the sine of the angle, the kept pointed cap growing at
                       its front; the rear end stays put, as the nacelle pivots there [A: the shape and the 60 % are authored]
"""
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageMath

ROOT = Path(__file__).resolve().parents[2]
G = ROOT / 'assets/phobos-vehicles/generated'
OUT = G / 'osprey-derived'
PICK = G / 'osprey-trial-nano-banana-pro/osprey-trial-nano-banana-pro-1.png'
LIFTED = G / 'osprey-overlays-lifted'
PARTS = ROOT / '.local/art/parts'
GUN_762_LENGTH_M, GUN_50_LENGTH_M = 1.25, 1.65   # About right for an M240 and an M2 [A: from general knowledge, not sourced]
FORWARD_GROWTH = 0.6                               # Fully forward, the nacelle shows this share longer than upright [A]
SPIN_STEPS = 40                                    # Turns averaged over a third of a turn (the rotor repeats every 120 degrees)
SPIN_PEAK_ALPHA = 110                              # The spinning disc's densest ring, out of 255 [A]
ROOT_CLEAN_SHARE = 0.72                            # Out along the blade, the share of its length where it lifted out clean


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def opaque_box(image, cut=128):
    return image.getchannel('A').point(lambda v: 255 if v >= cut else 0).getbbox()


def save(image, name, record):
    OUT.mkdir(parents=True, exist_ok=True)
    target = OUT / name
    image.save(target)
    record = dict(record, file=name, sha256=sha(target), made_by='assets/phobos-vehicles/derive_osprey_parts.py (Pillow)')
    target.with_suffix('.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    print(f'wrote {target.relative_to(ROOT).as_posix()}')


def gun_50():
    small, big = Image.open(LIFTED / 'ramp-gun-762.png'), Image.open(LIFTED / 'ramp-gun-50.png')
    (sx0, sy0, sx1, sy1), (bx0, by0, bx1, by1) = opaque_box(small), opaque_box(big)
    scale = (sy1 - sy0) * GUN_50_LENGTH_M / GUN_762_LENGTH_M / (by1 - by0)
    # The barrel is all there is in the gun's last rows: its middle is the line the mount sits on.
    alpha = big.getchannel('A')
    xs = [x for y in range(by1 - 40, by1) for x in range(bx0, bx1) if alpha.getpixel((x, y)) >= 128]
    barrel_x = sum(xs) / len(xs)
    part = big.crop((bx0, by0, bx1, by1))
    part = part.resize((max(1, round(part.width * scale)), max(1, round(part.height * scale))), Image.LANCZOS)
    out = Image.new('RGBA', big.size, (0, 0, 0, 0))
    out.alpha_composite(part, (round(barrel_x - (barrel_x - bx0) * scale), by0))   # The mount (the top) stays where it was drawn.
    save(out, 'ramp-gun-50.png', {'from': (LIFTED / 'ramp-gun-50.png').relative_to(ROOT).as_posix(), 'scale': round(scale, 3),
                                  'why': 'drawn about three times too large; shrunk about its mount to its length beside the 7.62 mm gun'})


def column_middle(alpha, x, y0, y1):
    ys = [y for y in range(y0, y1) if alpha.getpixel((x, y)) >= 128]
    return sum(ys) / len(ys) if ys else None


def pick_hub():
    """The pick's left hub cap, cut by its part mask, and its radius in the pick's pixels."""
    folder = PARTS / sha(PICK)
    mask = Image.open(folder / 'LeftHub.png').convert('L').point(lambda v: 255 if v >= 128 else 0)
    # The mask also holds a stray speck by the band (the parts record notes it): keep only the disc, flooded from its centre.
    record = json.loads((folder / 'parts.json').read_text(encoding='utf-8'))
    centre = next(part['centre'] for part in record['parts'] if part['name'] == 'LeftHub')
    ImageDraw.floodfill(mask, (int(centre[0]), int(centre[1])), 128)
    mask = mask.point(lambda v: 255 if v == 128 else 0)
    box = mask.getbbox()
    cap = Image.open(PICK).convert('RGBA')
    cap.putalpha(mask)
    return cap.crop(box), (box[2] - box[0]) / 2


def rotor():
    lifted = Image.open(LIFTED / 'rotor.png').convert('RGBA')
    alpha = lifted.getchannel('A')
    w, h = lifted.size
    centre_x = w / 2                               # The request put the hub at the canvas's centre; the rotor is drawn symmetric.
    x0, y0, x1, y1 = opaque_box(lifted)
    # The left blade's centre line, from its middle at two columns, carried on to the centre line: that is the hub.
    a_x, b_x = int(x0 + (centre_x - x0) * 0.25), int(x0 + (centre_x - x0) * 0.6)
    a_y, b_y = column_middle(alpha, a_x, y0, y1), column_middle(alpha, b_x, y0, y1)
    hub_y = b_y + (b_y - a_y) / (b_x - a_x) * (centre_x - b_x)
    hub = (centre_x, hub_y)
    # One whole blade: everything left of the centre line, below the hub (the blade runs down and to the left), clear of the hub.
    blade = lifted.copy()
    keep = Image.new('L', lifted.size, 0)
    ImageDraw.Draw(keep).rectangle((0, int(hub_y), int(centre_x) - 1, h), fill=255)
    clear = Image.new('L', lifted.size, 255)
    hub_clear = 0.12 * w                          # Clear of the ragged roots and the scraps the lifting left near the hub
    ImageDraw.Draw(clear).ellipse((hub[0] - hub_clear, hub[1] - hub_clear, hub[0] + hub_clear, hub[1] + hub_clear), fill=0)
    blade_alpha = ImageChops.multiply(ImageChops.multiply(alpha, keep), clear)
    # Only the blade itself: flood the shape that holds the blade's own centre line, a quarter size for speed, and keep that.
    quarter = blade_alpha.resize((w // 4, h // 4)).point(lambda v: 255 if v >= 128 else 0)
    ImageDraw.floodfill(quarter, (b_x // 4, int(b_y) // 4), 128)
    blob = quarter.point(lambda v: 255 if v == 128 else 0).resize((w, h)).filter(ImageFilter.MaxFilter(9))
    blade.putalpha(ImageChops.multiply(blade_alpha, blob))
    # Three blades about the hub, on a square canvas centred on it.
    reach = max(math.dist(hub, (x0, y1)), math.dist(hub, (x1, y1))) + 20
    size = int(2 * reach)
    whole = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    placed = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    placed.alpha_composite(blade, (round(size / 2 - hub[0]), round(size / 2 - hub[1])))
    # The inner part of the blade lifted out ragged (it crossed the fuselage's grey). Turn the blade to lie along +x, then rebuild
    # it from the hub out to a clean cross-section, repeated: a straight root, as the outer blade is straight [A].
    lean = math.degrees(math.atan2(b_y - hub_y, b_x - centre_x))
    level = placed.rotate(lean, resample=Image.BICUBIC, center=(size / 2, size / 2))
    tip = opaque_box(level)[2] - size / 2
    clean_at = int(size / 2 + tip * ROOT_CLEAN_SHARE)
    strip = level.crop((clean_at, 0, clean_at + 3, size))
    sx0, sy0, sx1, sy1 = opaque_box(strip, 64) or (0, 0, 3, size)
    band = (int(sy0) - 6, int(sy1) + 6)
    ImageDraw.Draw(level).rectangle((int(size / 2), band[0], clean_at, band[1]), fill=(0, 0, 0, 0))
    for x in range(int(size / 2), clean_at, 3):
        level.alpha_composite(strip, (x, 0))
    placed = level.rotate(-lean, resample=Image.BICUBIC, center=(size / 2, size / 2))
    for turn in (0, 120, 240):
        whole.alpha_composite(placed.rotate(turn, resample=Image.BICUBIC, center=(size / 2, size / 2)))
    cap, cap_radius = pick_hub()
    # The cap covers the ragged blade roots: the real V-22's spinner is large beside its blades, so this suits it [A].
    cap_scale = (hub_clear + 0.02 * w) / cap_radius
    cap = cap.resize((max(1, round(cap.width * cap_scale)), max(1, round(cap.height * cap_scale))), Image.LANCZOS)
    with_cap = whole.copy()
    with_cap.alpha_composite(cap, (round(size / 2 - cap.width / 2), round(size / 2 - cap.height / 2)))
    save(with_cap, 'rotor.png', {'from': [(LIFTED / 'rotor.png').relative_to(ROOT).as_posix(), PICK.relative_to(ROOT).as_posix()],
                                 'hub_found_at': [round(hub[0], 1), round(hub[1], 1)], 'hub_is_canvas_centre': True,
                                 'why': 'the blade over the fuselage lifted out incomplete; rebuilt from the one whole blade'})
    layers = [whole.rotate(120 * step / SPIN_STEPS, resample=Image.BICUBIC, center=(size / 2, size / 2)) for step in range(SPIN_STEPS)]
    # An even mix of every turn, summed in floating point (8-bit blends drift toward the last layers), with colour weighted by
    # cover (premultiplied), or the clear parts' black would darken the disc.
    add = lambda e: e['x'] + e['y']
    sums = [Image.new('F', (size, size), 0.0) for _ in range(4)]
    for layer in layers:
        r, g, b, a = (channel.convert('F') for channel in layer.split())
        for index, channel in enumerate((r, g, b)):
            sums[index] = ImageMath.lambda_eval(lambda e: e['s'] + e['c'] * e['a'] / 255.0, s=sums[index], c=channel, a=a)
        sums[3] = ImageMath.lambda_eval(add, x=sums[3], y=a)
    alpha_sum = sums[3]
    colour = [ImageMath.lambda_eval(lambda e: e['convert'](e['min'](e['c'] * 255.0 / e['max'](e['a'], 1.0), 255.0), 'L'),
                                    c=sums[i], a=alpha_sum) for i in range(3)]
    mean_alpha = ImageMath.lambda_eval(lambda e: e['convert'](e['a'] / float(SPIN_STEPS), 'L'), a=alpha_sum)
    spin = Image.merge('RGBA', colour + [mean_alpha])
    # The even mix is faint, as a real spinning rotor is; it is brought up so its densest ring is a little under half opaque [A].
    # The blade roots overlap under the cap, so the brightest ring is found outside it.
    outside = Image.new('L', spin.size, 255)
    ImageDraw.Draw(outside).ellipse((size / 2 - cap.width * 0.6, size / 2 - cap.height * 0.6,
                                     size / 2 + cap.width * 0.6, size / 2 + cap.height * 0.6), fill=0)
    peak = max(1, ImageChops.multiply(spin.getchannel('A'), outside).getextrema()[1])
    spin.putalpha(spin.getchannel('A').point(lambda v: min(255, int(v * SPIN_PEAK_ALPHA / peak))))
    spin.alpha_composite(cap, (round(size / 2 - cap.width / 2), round(size / 2 - cap.height / 2)))
    save(spin, 'rotor-spin.png', {'from': 'rotor.png', 'turns_averaged': SPIN_STEPS, 'over_degrees': 120})


def nacelles():
    pick = Image.open(PICK).convert('RGBA')
    folder = PARTS / sha(PICK)
    mask = Image.open(folder / 'LeftNacelle.png').convert('L')
    hub = Image.open(folder / 'LeftHub.png').convert('L')
    box = mask.getbbox()
    nacelle = pick.copy()
    nacelle.putalpha(mask)
    nacelle = nacelle.crop(box)
    hub = hub.crop(box)
    hx0, hy0, hx1, hy1 = hub.getbbox()
    # Take the hub cap off: each of its pixels takes the colour the same column has in a plain row of the same kind, the orange
    # band's where the row outside the hub is band, the grey panel's otherwise. Only the hub (a little grown) is touched.
    def is_band(pixel):
        return pixel[3] > 250 and pixel[0] - pixel[2] > 80
    def is_grey(pixel):
        return pixel[3] > 250 and abs(pixel[0] - pixel[2]) < 15 and abs(pixel[0] - pixel[1]) < 10 and sum(pixel[:3]) / 3 > 140
    middle = nacelle.width // 2
    rows = range(nacelle.height)
    grey_row = min((y for y in rows if is_grey(nacelle.getpixel((middle, y))) and not hy0 - 20 <= y <= hy1 + 20),
                   key=lambda y: abs(y - hy0))
    band_row = min((y for y in rows if is_band(nacelle.getpixel((middle, y))) and not hy0 - 20 <= y <= hy1 + 20),
                   key=lambda y: abs(y - hy0))
    grown = hub.filter(ImageFilter.MaxFilter(25))   # Over the hub's dark rim as well
    sides = (max(2, hx0 - 16), min(nacelle.width - 3, hx1 + 16))
    for y in range(max(0, hy0 - 14), min(nacelle.height, hy1 + 14)):
        source = band_row if any(is_band(nacelle.getpixel((x, y))) for x in sides) else grey_row
        for x in range(nacelle.width):
            if grown.getpixel((x, y)) > 0:      # The part cut gave the hub's pixels to the hub, so here they are made solid again.
                r, g, b, a = nacelle.getpixel((x, source))
                nacelle.putpixel((x, y), (r, g, b, a))
    width, length = nacelle.size
    caps = Image.open(LIFTED / 'nacelle-90-caps.png').convert('RGBA')
    cx0, cy0, cx1, cy1 = opaque_box(caps)
    left_cap = caps.crop((cx0, cy0, (cx0 + cx1) // 2, cy1))
    left_cap = left_cap.crop(opaque_box(left_cap))
    nose = 0.18                                     # The front share of the nacelle drawn as its rounded end, kept unstretched.
    front, rest = nacelle.crop((0, 0, width, int(length * nose))), nacelle.crop((0, int(length * nose), width, length))
    for angle in (0, 30, 60, 90):
        grow = FORWARD_GROWTH * math.sin(math.radians(angle))
        cap_height = round(left_cap.height * width / left_cap.width * math.sin(math.radians(angle)))
        total = round(length * (1 + grow)) + cap_height
        frame = Image.new('RGBA', (width, total), (0, 0, 0, 0))
        body_rest = rest.resize((width, total - cap_height - front.height), Image.LANCZOS)
        frame.alpha_composite(body_rest, (0, total - body_rest.height))
        frame.alpha_composite(front, (0, cap_height))
        if cap_height:
            frame.alpha_composite(left_cap.resize((width, cap_height), Image.LANCZOS), (0, 0))
        save(frame, f'nacelle-{angle}.png', {'from': [PICK.relative_to(ROOT).as_posix(), 'its LeftNacelle and LeftHub part masks',
                                                       (LIFTED / 'nacelle-90-caps.png').relative_to(ROOT).as_posix()],
                                              'tilt_degrees_from_upright': angle, 'length_share': round(1 + grow, 3),
                                              'pivot': 'the bottom edge, which stays where the upright nacelle\'s rear end is'})


if __name__ == '__main__':
    gun_50()
    rotor()
    nacelles()
