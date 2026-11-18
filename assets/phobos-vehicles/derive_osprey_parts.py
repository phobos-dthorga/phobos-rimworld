"""Finish the V-22 Osprey's parts by script from pictures we already have, with Pillow only: no generator is called.

    python assets/phobos-vehicles/derive_osprey_parts.py

Rule 11 of the paid rules (prefer a free derivation to a paid generation), and the round's record:
assets/phobos-vehicles/requests/osprey-overlays-record.md. Reads the owner's pick, its part masks from scripts/art/name_parts.py, the
nacelle's pointed cap lifted from an edit, and the clean rotor redraw; writes new files to
assets/phobos-vehicles/generated/osprey-derived/, each with a record. No input is changed.

  ramp-gun-50/762.png  both ramp guns at their real length, on the one ramp mount (owner's choice: the .50 "b", real size)
  radar-nose.png       the radar cap from the pick's own nose tip, a shade darker, with a seam line: it lines up exactly
  rotor.png            the clean rotor redraw (osprey-rotor-clean-4k) cut from its white ground, centred on its hub
  rotor-spin.png       the same rotor spinning: an average of it turned through a third of a turn
  nacelle-0.png        the upright nacelle, cut from the pick by its part mask, its hub cap taken off whole rows at a time
  nacelle-30/60/90.png the nacelle tilting forward: its front drawn longer by the sine of the angle, the kept pointed cap growing at
                       its front; the rear end stays put, as the nacelle pivots there [A: the shape and the 60 % are authored]
"""
import hashlib
import json
import math
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageMath

ROOT = Path(__file__).resolve().parents[2]
G = ROOT / 'assets/phobos-vehicles/generated'
OUT = G / 'osprey-derived'
PICK = G / 'osprey-trial-nano-banana-pro/osprey-trial-nano-banana-pro-1.png'
LIFTED = G / 'osprey-overlays-lifted'
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402  The art tools' working folder, where Segment Anything's part masks are
FORWARD_GROWTH = 0.6                               # Fully forward, the nacelle shows this share longer than upright [A]
SPIN_STEPS = 40                                    # Turns averaged over a third of a turn (the rotor repeats every 120 degrees)
SPIN_PEAK_ALPHA = 110                              # The spinning disc's densest ring, out of 255 [A]
HUB_SHADOW = 30                                    # Pixels below the pick's hub cap that its drop shadow darkens (measured)
ROTOR_DRAWN = G / 'osprey-rotor-clean-4k/osprey-rotor-clean-4k-1.png'
WHITE = 236                                        # Paler than this is the white ground round the rotor
OUTLINE_DARK = 90                                  # Darker than this in every channel is the drawing's near-black outline
HUB_RING = 1.12                                    # The hub cap's outer edge, as a share of its ring's inner radius (measured)
NOSE_BOX = (1800, 200, 2300, 414)                  # The pick's nose tip down to where the radar edit put its seam (measured)
NOSE_SEAM_WIDTH = 6                                # Pixels: as the edit drew the seam (rows 414 to 420)
NOSE_SHADE = 0.88                                  # The cap's panel against the pick's grey [A]
OUTLINE_COLOUR = (42, 33, 48)                      # The drawing's near-black line, read from the pick
SAM = G / 'osprey-overlays-sam'
METRE_PX = 208                                     # The pick's pixels a metre: 3,628 px nose to tail for 17.48 m (measured)
GUN_762_LENGTH_M, GUN_50_LENGTH_M = 1.25, 1.65     # About right for an M240 and an M2 [A: from general knowledge, not sourced]
BARRIER = 'inlet-barrier-a.png'                    # The owner's pick of 19 Nov 2026
MARKS = {'body': G / 'bulk-tanker-damage-body/bulk-tanker-damage-body-1.png',
         'tank': G / 'bulk-tanker-damage-tank/bulk-tanker-damage-tank-1.png'}
MARKS_GAME_PX = 10 * 128 / 1536                    # Game pixels per marks pixel: the master's height as the tanker's 10 cells [A]
GAME_PX_PER_PICK_PX = 20.5 * 128 / 3628            # export_osprey.py's scale: 20.5 cells over the pick's 3,628 px nose to tail
# (name, part masks, marks master, turned for a wide part, where in the marks to start): agent placements, 19 Nov 2026
DAMAGE_PARTS = [('Cockpit', ['Cockpit'], 'body', False, (0, 0)),
                ('ForwardCabin', ['ForwardCabin'], 'tank', False, (150, 300)),
                ('AftCabin', ['AftCabinAndRamp'], 'body', False, (200, 60)),
                ('LeftWing', ['LeftWing'], 'tank', True, (0, 200)),
                ('RightWing', ['RightWing'], 'body', True, (500, 0)),
                ('Tail', ['Tailplane', 'LeftFin', 'RightFin'], 'tank', True, (300, 380))]
NACELLE_DAMAGE = {'-damage-left': ('body', (300, 900)), '-damage-right': ('tank', (500, 200))}
SEAM_LINE = 4                                      # Pixels: the nacelle's seam line, kept below its rounded end
MOUNT_SHARE = 0.3                                 # The pintle, this share of the gun's length back from its grips [A]


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


def gun_mount(gun):
    """A gun drawn pointing aft (down the canvas): its length, its centre line (the barrel's middle in its last rows) and its
    mount, MOUNT_SHARE of its length back from its front end on that line."""
    x0, y0, x1, y1 = opaque_box(gun)
    alpha = gun.getchannel('A')
    xs = [x for y in range(y1 - 40, y1) for x in range(x0, x1) if alpha.getpixel((x, y)) >= 128]
    centre_x = sum(xs) / len(xs)
    return y1 - y0, (centre_x, y0 + (y1 - y0) * MOUNT_SHARE)


def guns_real_size():
    """Both ramp guns at their real length (owner, 19 Nov 2026), shrunk about their mount; the 7.62 mm gun moved onto the .50's
    mount on the ramp, as they are either/or on one mount (the design's upgrade tree)."""
    fifty, seven = (Image.open(SAM / name).convert('RGBA') for name in ('ramp-gun-50-b.png', 'ramp-gun-762.png'))
    _, mount = gun_mount(fifty)
    for gun, metres, name, source in ((fifty, GUN_50_LENGTH_M, 'ramp-gun-50.png', 'ramp-gun-50-b.png'),
                                      (seven, GUN_762_LENGTH_M, 'ramp-gun-762.png', 'ramp-gun-762.png')):
        length, own_mount = gun_mount(gun)
        scale = metres * METRE_PX / length
        box = opaque_box(gun)
        piece = gun.crop(box)
        piece = piece.resize((max(1, round(piece.width * scale)), max(1, round(piece.height * scale))), Image.LANCZOS)
        offset = ((own_mount[0] - box[0]) * scale, (own_mount[1] - box[1]) * scale)
        out = Image.new('RGBA', gun.size, (0, 0, 0, 0))
        out.alpha_composite(piece, (round(mount[0] - offset[0]), round(mount[1] - offset[1])))
        save(out, name, {'from': (SAM / source).relative_to(ROOT).as_posix(), 'scale': round(scale, 3), 'length_m': metres,
                         'mount_px': [round(mount[0], 1), round(mount[1], 1)],
                         'why': 'real size, the owner\'s choice of 19 Nov 2026; both on the ramp mount the .50 was drawn on'})


def radar_nose():
    """The radar nose cap from the pick's own nose tip: the same outline, a shade darker, one seam line across it. The model's
    redrawn nose was a slightly different shape from the pick's, so laid over it the pick's outline showed doubled beside it
    (seen at full size, 18 Nov 2026); made from the pick itself, it lines up exactly. What the request asked for, less "fuller"."""
    pick = Image.open(PICK).convert('RGB')
    w, h = pick.size
    x0, y0, x1, y1 = NOSE_BOX
    region = Image.new('L', (w, h), 0)
    ImageDraw.Draw(region).rectangle((x0, y0, x1, y1 + NOSE_SEAM_WIDTH), fill=255)
    not_white = pick.convert('L').point(lambda v: 255 if v < 236 else 0)
    mask = ImageChops.multiply(region, not_white)
    r, g, b = pick.split()
    outline = ImageChops.lighter(ImageChops.lighter(r, g), b).point(lambda v: 255 if v < OUTLINE_DARK else 0)
    darker = pick.point(lambda v: int(v * NOSE_SHADE))
    cap = Image.composite(pick, darker, outline)          # The outline stays as drawn; the panel inside goes a shade darker.
    # The seam: a dark line across the nose at the bottom of the cap, between its two outline edges.
    seam = Image.new('L', (w, h), 0)
    ImageDraw.Draw(seam).rectangle((x0, y1, x1, y1 + NOSE_SEAM_WIDTH), fill=255)
    seam = ImageChops.multiply(seam, mask)
    cap = Image.composite(Image.new('RGB', (w, h), OUTLINE_COLOUR), cap, seam)
    part = cap.convert('RGBA')
    part.putalpha(mask.filter(ImageFilter.GaussianBlur(0.8)))
    save(part, 'radar-nose.png', {'from': PICK.relative_to(ROOT).as_posix(), 'box': list(NOSE_BOX), 'shade': NOSE_SHADE,
                                  'seam_rows': [y1, y1 + NOSE_SEAM_WIDTH],
                                  'why': 'the model drew a nose of a slightly different shape; made from the pick, it lines up'})


def marks(name, rotate=False):
    """A damage marks master (the bulk tanker's, our art of 8 Nov 2026) as near-black with alpha, at the pick's scale so a bullet
    hole is the same size in game on every vehicle: the master's 1,536 px are taken as the tanker's 10 cells [A]."""
    sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
    from export_vehicle import marks_to_alpha
    picture = marks_to_alpha(Image.open(MARKS[name]).convert('RGB'))
    if rotate:
        picture = picture.transpose(Image.ROTATE_90)
    factor = MARKS_GAME_PX / GAME_PX_PER_PICK_PX
    return picture.resize((round(picture.width * factor), round(picture.height * factor)), Image.LANCZOS)


def damage_layer(part_mask, box, source, rotate, start):
    """Marks over a part's box, from a starting point of their own in the master, clipped to the part's outline."""
    picture = marks(source, rotate)
    width, height = box[2] - box[0], box[3] - box[1]
    x = min(max(0, start[0]), max(0, picture.width - width))
    y = min(max(0, start[1]), max(0, picture.height - height))
    crop = picture.crop((x, y, x + width, y + height))
    crop.putalpha(ImageChops.multiply(crop.getchannel('A'), part_mask.crop(box)))
    return crop


def damage():
    """Damage looks on the body's outer parts (vehicle-osprey.md, "Parts and armour"), each on the pick's canvas, clipped to the part's
    mask from Segment Anything. The nacelles' are made in nacelles(), so they tilt with them. Agent placements, for the owner to revise."""
    folder = art_paths.find('parts', sha(PICK))
    size = Image.open(PICK).size
    for name, parts, source, rotate, start in DAMAGE_PARTS:
        mask = Image.new('L', size, 0)
        for part in parts:
            mask = ImageChops.lighter(mask, Image.open(folder / f'{part}.png').convert('L'))
        box = mask.getbbox()
        layer = Image.new('RGBA', size, (0, 0, 0, 0))
        layer.alpha_composite(damage_layer(mask, box, source, rotate, start), (box[0], box[1]))
        save(layer, f'damage-{name}.png', {'from': [MARKS[source].relative_to(ROOT).as_posix(), f'part masks {parts}'],
                                            'start_in_marks': list(start), 'turned': rotate, 'game_px_per_marks_px': MARKS_GAME_PX})


def spin(whole, cap, size):
    """The rotor spinning: an even mix of it turned through a third of a turn (it repeats every 120 degrees)."""
    layers = [whole.rotate(120 * step / SPIN_STEPS, resample=Image.BICUBIC, center=(size / 2, size / 2)) for step in range(SPIN_STEPS)]
    # Summed in floating point (8-bit blends drift toward the last layers), with colour weighted by cover (premultiplied), or the
    # clear parts' black would darken the disc.
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
    disc = Image.merge('RGBA', colour + [mean_alpha])
    # The even mix is faint, as a real spinning rotor is; it is brought up so its densest ring is a little under half opaque [A].
    # The blade roots overlap under the hub, so the densest ring is found outside it.
    outside = Image.new('L', disc.size, 255)
    ImageDraw.Draw(outside).ellipse((size / 2 - cap.width * 0.6, size / 2 - cap.height * 0.6,
                                     size / 2 + cap.width * 0.6, size / 2 + cap.height * 0.6), fill=0)
    peak = max(1, ImageChops.multiply(disc.getchannel('A'), outside).getextrema()[1])
    disc.putalpha(disc.getchannel('A').point(lambda v: min(255, int(v * SPIN_PEAK_ALPHA / peak))))
    disc.alpha_composite(cap, (round(size / 2 - cap.width / 2), round(size / 2 - cap.height / 2)))
    return disc


def rotor():
    """The clean redraw (osprey-rotor-clean-4k, the owner's look of 18 Nov 2026 found the script-built one mangled), cut from its
    white ground and centred on its hub, then spun."""
    drawn = Image.open(ROTOR_DRAWN).convert('RGB')
    w, h = drawn.size
    # The ground is the white connected to the canvas's corner: anything pale inside a blade (a highlight) stays.
    pale = drawn.convert('L').point(lambda v: 255 if v >= WHITE else 0)
    ImageDraw.floodfill(pale, (0, 0), 128)
    ground = pale.point(lambda v: 255 if v == 128 else 0)
    alpha = ImageChops.invert(ground).filter(ImageFilter.GaussianBlur(0.8))
    rotor_image = drawn.convert('RGBA')
    rotor_image.putalpha(alpha)
    # The hub: the pale disc inside its dark ring, flooded from the canvas's centre (the request put the hub there).
    r, g, b = drawn.split()
    dark = ImageChops.lighter(ImageChops.lighter(r, g), b).point(lambda v: 255 if v < OUTLINE_DARK else 0)
    ImageDraw.floodfill(dark, (w // 2, h // 2), 128)
    inner = dark.point(lambda v: 255 if v == 128 else 0).getbbox()
    hub = ((inner[0] + inner[2]) / 2, (inner[1] + inner[3]) / 2)
    ring = (inner[2] - inner[0]) / 2 * HUB_RING
    # The farthest blade tip from the hub, read on a coarse grid (the box's corners are empty, between the blades).
    step = 8
    coarse = alpha.resize((w // step, h // step))
    reach = max(math.dist(hub, (x * step, y * step)) for y in range(coarse.height) for x in range(coarse.width)
                if coarse.getpixel((x, y)) >= 128) + 2 * step
    size = int(2 * reach)
    whole = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    whole.alpha_composite(rotor_image, (round(size / 2 - hub[0]), round(size / 2 - hub[1])))
    save(whole, 'rotor.png', {'from': ROTOR_DRAWN.relative_to(ROOT).as_posix(), 'hub_found_at': [round(hub[0], 1), round(hub[1], 1)],
                              'hub_radius_px': round(ring, 1), 'why': 'the clean redraw, centred on its hub for the renderer to turn'})
    cap_mask = Image.new('L', (size, size), 0)
    ImageDraw.Draw(cap_mask).ellipse((size / 2 - ring, size / 2 - ring, size / 2 + ring, size / 2 + ring), fill=255)
    cap = whole.copy()
    cap.putalpha(ImageChops.multiply(whole.getchannel('A'), cap_mask))
    cap = cap.crop(cap_mask.getbbox())
    save(spin(whole, cap, size), 'rotor-spin.png', {'from': 'rotor.png', 'turns_averaged': SPIN_STEPS, 'over_degrees': 120})


def nacelles():
    pick = Image.open(PICK).convert('RGBA')
    folder = art_paths.find('parts', sha(PICK))
    mask = Image.open(folder / 'LeftNacelle.png').convert('L')
    hub = Image.open(folder / 'LeftHub.png').convert('L')
    box = mask.getbbox()
    nacelle = pick.copy()
    nacelle.putalpha(mask)
    nacelle = nacelle.crop(box)
    hub = hub.crop(box)
    hx0, hy0, hx1, hy1 = hub.getbbox()
    # Take the hub cap off. The nacelle is a cylinder lying along its length, so its shading changes across its width only: every
    # row through the hub, its rim and its drop shadow is replaced whole by a plain row of the same kind (the orange band's where
    # the row is band beside the hub, the grey panel's otherwise). Patching only the hub's own pixels left a ghost of its rim and
    # shadow, which the owner saw (18 Nov 2026).
    def is_band(pixel):
        return pixel[3] > 250 and pixel[0] - pixel[2] > 80
    def is_grey(pixel):
        return pixel[3] > 250 and abs(pixel[0] - pixel[2]) < 15 and abs(pixel[0] - pixel[1]) < 10 and sum(pixel[:3]) / 3 > 140
    above, below = 14, HUB_SHADOW                  # Rows above the hub, and below it, where its drop shadow falls
    middle = nacelle.width // 2
    rows = range(nacelle.height)
    clear = [y for y in rows if not hy0 - above - 6 <= y <= hy1 + below + 6]
    grey_row = min((y for y in clear if is_grey(nacelle.getpixel((middle, y)))), key=lambda y: abs(y - hy0))
    band_row = min((y for y in clear if is_band(nacelle.getpixel((middle, y)))), key=lambda y: abs(y - hy0))
    sides = (max(2, hx0 - 16), min(nacelle.width - 3, hx1 + 16))
    for y in range(max(0, hy0 - above), min(nacelle.height, hy1 + below)):
        source = band_row if any(is_band(nacelle.getpixel((x, y))) for x in sides) else grey_row
        for x in range(nacelle.width):
            nacelle.putpixel((x, y), nacelle.getpixel((x, source)))
    width, length = nacelle.size
    # With the inlet barrier (the owner's pick (a), 19 Nov 2026): its overlay, lifted onto the pick's canvas, cropped to the nacelle's
    # box so the two register. It is drawn into the nacelle, not laid over the body, so it tilts with it.
    barrier = Image.open(SAM / BARRIER).convert('RGBA').crop(box)
    with_barrier = nacelle.copy()
    with_barrier.alpha_composite(barrier)
    caps = Image.open(LIFTED / 'nacelle-90-caps.png').convert('RGBA')
    cx0, cy0, cx1, cy1 = opaque_box(caps)
    left_cap = caps.crop((cx0, cy0, (cx0 + cx1) // 2, cy1))
    left_cap = left_cap.crop(opaque_box(left_cap))
    # The nacelle's front end is rounded down to its first seam line; a cap laid on top of it leaves a gap at the corners (seen at
    # full size, 18 Nov 2026). So the cap covers that rounded end and meets the nacelle at the seam: the first dark row down the
    # middle after the light panel below the top outline. Below the seam the nacelle stretches evenly as it tilts, so a grille
    # drawn there (the inlet barrier) keeps its shape.
    middle_x = width // 2
    head = range(int(length * 0.3))
    shade = [sum(nacelle.getpixel((middle_x, y))[:3]) / 3 if nacelle.getpixel((middle_x, y))[3] > 128 else None for y in head]
    top = next(y for y, v in enumerate(shade) if v is not None)
    light = next(y for y in range(top, len(shade)) if shade[y] is not None and shade[y] > 100)
    seam = next(y for y in range(light, len(shade)) if shade[y] is not None and shade[y] < 70)
    end = seam + SEAM_LINE                          # The rounded end and the seam line below it
    # Damage marks clipped to the nacelle (with its hub), one look for each side; the right is drawn as the left and mirrored.
    outline = ImageChops.lighter(mask, Image.open(folder / 'LeftHub.png').convert('L'))
    damaged = {suffix: damage_layer(outline, box, source, False, start) for suffix, (source, start) in NACELLE_DAMAGE.items()}
    # The barrier alone, turned the same way, so the export can keep its grille out of the paint (a filter mesh is bare metal).
    pictures = [('', nacelle), ('-barrier', with_barrier), ('-barrier-only', barrier)] + list(damaged.items())
    for suffix, picture in pictures:
        front_whole, rest = picture.crop((0, 0, width, end)), picture.crop((0, end, width, length))
        for angle in (0, 30, 60, 90):
            grow = FORWARD_GROWTH * math.sin(math.radians(angle))
            cap_height = round(left_cap.height * width / left_cap.width * math.sin(math.radians(angle)))
            overlap = min(seam, cap_height)
            front = front_whole
            if cap_height:   # Under the cap the nacelle's rounded end goes, or its corners peek out beside a short cap (30 degrees).
                front = front_whole.copy()
                ImageDraw.Draw(front).rectangle((0, 0, width, seam - 1), fill=(0, 0, 0, 0))
            rest_height = round(length * (1 + grow)) - end
            total = cap_height - overlap + end + rest_height
            frame = Image.new('RGBA', (width, total), (0, 0, 0, 0))
            frame.alpha_composite(rest.resize((width, rest_height), Image.LANCZOS), (0, total - rest_height))
            frame.alpha_composite(front, (0, cap_height - overlap))
            if cap_height and suffix in ('', '-barrier'):   # Laid last, over the nacelle's rounded end.
                frame.alpha_composite(left_cap.resize((width, cap_height), Image.LANCZOS), (0, 0))
            sources = [PICK.relative_to(ROOT).as_posix(), 'its LeftNacelle and LeftHub part masks',
                       (LIFTED / 'nacelle-90-caps.png').relative_to(ROOT).as_posix()]
            if suffix:
                sources.append((SAM / BARRIER).relative_to(ROOT).as_posix())
            save(frame, f'nacelle-{angle}{suffix}.png', {'from': sources, 'tilt_degrees_from_upright': angle,
                                                          'length_share': round(1 + grow, 3), 'nacelle_box_in_pick': list(box),
                                                          'pivot': 'the bottom edge, which stays where the upright nacelle\'s rear end is'})


if __name__ == '__main__':
    guns_real_size()
    radar_nose()
    damage()
    rotor()
    nacelles()
