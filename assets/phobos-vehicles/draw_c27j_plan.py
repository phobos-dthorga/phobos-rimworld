"""Draw the C-27J Spartan's plan from above, and one six-blade propeller, with Pillow, as layout references of our own for the paid
pictures (docs/development/vehicle-c27j.md, "Art"). Modelled on draw_osprey_plan.py.

No image generator, photograph, drawing or other author's art is involved: every shape is placed from a published dimension or,
where none was read, from an approximate figure marked as such below. The plans are references only and are never shipped.

    python assets/phobos-vehicles/draw_c27j_plan.py

It writes to assets/phobos-vehicles/references/ (kept off main; drawn again by this script):
  c27j-plan.png            the airframe alone, nacelles ending in bare spinners, no propellers: the reference a service is given
  c27j-plan-check.png      the same with the propeller planes (edge on, as seen from above) and a five-metre bar; never sent anywhere
  c27j-propeller-plan.png  one six-blade propeller laid flat (seen along its shaft), alone on white: the layout reference of tries
                           1 and 2, superseded (the owner rejected them, 26 Nov 2026)
  c27j-propeller-edge-plan.png  the same propeller as it ships, from straight above and edge on, black: tries 3 and 4's reference
  c27j-plan.record.json    the figures used and each file's SHA-256

Published (sources in docs/development/vehicle-c27j.md, read 26 Nov 2026): length 22.7 m and span 28.7 m [S]; wing area 82 m2 [S]
(the wing below comes to about 82 m2); six-blade propellers 4.115 m across (EASA type certificate data, [P]); fuselage 3.70 m across
(AeroCorner, [S]). Everything else is approximate, by eye from general knowledge of the aircraft, and is for layout only: where the
wing sits and its chords, the engines' spacing, the nacelles, the gear fairings, the tail's sizes and the propeller blade's shape.
"""
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/phobos-vehicles/references'
WIDTH, HEIGHT = 2560, 2048        # 5:4, the shape Nano Banana Pro offers nearest the aircraft's span to length (1.264).
FILL = 0.92                       # The span as a share of the canvas width.
LENGTH = 22.7                     # Published.
SPAN = 28.7                       # Published.
PROPELLER = 4.115                 # Published (EASA): six blades.
FUSELAGE_WIDTH = 3.70             # Published (secondary).
SCALE = FILL * WIDTH / SPAN       # Pixels a metre.

# Approximate, for layout only (metres; "back" is behind the nose).
NOSE_ROUND = 3.0                  # The nose rounds over its first three metres.
GLAZING = (1.15, 2.75)            # The cockpit glazing, front and rear.
WING_LE = 8.3                     # The wing's leading edge at the root.
ROOT_CHORD, TIP_CHORD = 3.4, 1.8  # A constant-chord centre section out to the engines, then tapered outer panels.
TIP_SWEEP = 0.5                   # How far back the tip's leading edge sits from the root's.
ENGINE_AT = 4.5                   # Each engine's centre line from the aircraft's.
NACELLE_WIDTH = 1.3
NACELLE_FRONT, NACELLE_REAR = 4.3, 12.4
SPINNER = 0.8                     # The spinner cone's length; the propeller plane is at its base.
GEAR_FAIRING = (8.6, 13.6, 0.55)  # The main gear fairings: front, rear, and how far each bulges out past the fuselage side.
TAPER_FROM = 13.8                 # The rear fuselage narrows from here, over the loading ramp.
TAIL_END_WIDTH = 0.9
RAMP = (14.2, 19.6)               # The ramp's outline across the rear fuselage.
FIN = (15.4, LENGTH - 0.15, 0.45)  # The fin seen from above: front, rear and thickness.
TAILPLANE_SPAN = 11.0
TAILPLANE_LE, TAILPLANE_ROOT_CHORD, TAILPLANE_TIP_CHORD, TAILPLANE_SWEEP = 19.75, 2.8, 1.5, 0.9

# The propeller, laid flat: six blades 60 degrees apart, each a slender scimitar sweeping back toward its tip (approximate).
HUB = 0.80                        # The spinner's diameter.
BLADE_CHORD = 0.34                # The widest chord, near mid-blade.
BLADE_SWEEP = 0.22                # How far the tip curves back from the blade's straight line, in metres.

OUTLINE = (24, 24, 26, 255)
PANEL = (190, 194, 198, 255)
DARKER = (150, 154, 160, 255)
GLASS = (70, 82, 96, 255)
WHITE = (255, 255, 255, 255)
LINE = 8


def at(across, back):
    """Canvas pixels for a point `across` metres right of the centre line and `back` metres behind the nose."""
    top = (HEIGHT - LENGTH * SCALE) / 2
    return WIDTH / 2 + across * SCALE, top + back * SCALE


def box(draw, left, front, right, rear, fill, radius=0.25):
    (x0, y0), (x1, y1) = at(left, front), at(right, rear)
    draw.rounded_rectangle((x0, y0, x1, y1), radius=int(radius * SCALE), fill=fill, outline=OUTLINE, width=LINE)


def mirrored(draw, right_side, fill):
    """A shape symmetric about the centre line, from its right half (points from front to rear)."""
    left = [(WIDTH - x, y) for x, y in reversed(right_side)]
    draw.polygon(right_side + left, fill=fill, outline=OUTLINE, width=LINE)


def airframe(draw):
    half = FUSELAGE_WIDTH / 2
    # The wing, one piece across: constant chord out to the engines, then tapering to the tips.
    tip = SPAN / 2
    wing = [at(0, WING_LE), at(ENGINE_AT, WING_LE), at(tip, WING_LE + TIP_SWEEP), at(tip, WING_LE + TIP_SWEEP + TIP_CHORD),
            at(ENGINE_AT, WING_LE + ROOT_CHORD), at(0, WING_LE + ROOT_CHORD)]
    mirrored(draw, wing, PANEL)
    # The main gear fairings bulge out from under the fuselage's sides, behind and below the wing.
    front, rear, bulge = GEAR_FAIRING
    box(draw, -half - bulge, front, half + bulge, rear, DARKER, radius=0.9)
    # The fuselage: a rounded nose, a straight cabin, a rear narrowing over the ramp to the tail.
    right = [at(0, 0)]
    for step in range(1, 13):
        back = NOSE_ROUND * step / 12
        right.append(at(half * (1 - (1 - back / NOSE_ROUND) ** 2) ** 0.5, back))
    right += [at(half, TAPER_FROM), at(TAIL_END_WIDTH / 2, LENGTH - 0.6), at(TAIL_END_WIDTH * 0.3, LENGTH - 0.3)]
    mirrored(draw, right, PANEL)
    # The ramp's outline, the wing's centre section over the roof, the cockpit glazing.
    ramp_half = half * 0.78
    ramp = [at(ramp_half, RAMP[0]), at(TAIL_END_WIDTH * 0.75, RAMP[1])]
    left_ramp = [(WIDTH - x, y) for x, y in reversed(ramp)]
    draw.line(ramp + left_ramp + ramp[:1], fill=OUTLINE, width=LINE // 2)
    box(draw, -half * 0.92, WING_LE + 0.05, half * 0.92, WING_LE + ROOT_CHORD - 0.05, PANEL, radius=0.3)
    box(draw, -half * 0.7, GLAZING[0], half * 0.7, GLAZING[1], GLASS, radius=0.5)
    # The engines: a long nacelle over the wing, a bare spinner cone at its front.
    for side in (-1, 1):
        x = side * ENGINE_AT
        box(draw, x - NACELLE_WIDTH / 2, NACELLE_FRONT + SPINNER, x + NACELLE_WIDTH / 2, NACELLE_REAR, DARKER, radius=0.6)
        draw.polygon([at(x - HUB / 2, NACELLE_FRONT + SPINNER), at(x, NACELLE_FRONT), at(x + HUB / 2, NACELLE_FRONT + SPINNER)],
                     fill=PANEL, outline=OUTLINE, width=LINE)
    # The T-tail: the fin along the centre line, the tailplane across its top.
    fin_front, fin_rear, fin_thick = FIN
    box(draw, -fin_thick / 2, fin_front, fin_thick / 2, fin_rear, DARKER, radius=0.2)
    t = TAILPLANE_SPAN / 2
    tailplane = [at(0, TAILPLANE_LE), at(t, TAILPLANE_LE + TAILPLANE_SWEEP), at(t, TAILPLANE_LE + TAILPLANE_SWEEP + TAILPLANE_TIP_CHORD),
                 at(0, TAILPLANE_LE + TAILPLANE_ROOT_CHORD)]
    mirrored(draw, tailplane, PANEL)
    box(draw, -fin_thick / 2, TAILPLANE_LE - 0.3, fin_thick / 2, LENGTH, DARKER, radius=0.2)   # The fin's top over the tailplane.


def draw_plan(check):
    image = Image.new('RGBA', (WIDTH, HEIGHT), WHITE)
    draw = ImageDraw.Draw(image)
    airframe(draw)
    if check:
        for side in (-1, 1):
            # From straight above, a turboprop's propeller turns in an upright plane, so it shows edge on, as a line.
            (x0, y), (x1, _) = at(side * ENGINE_AT - PROPELLER / 2, NACELLE_FRONT + SPINNER), at(side * ENGINE_AT + PROPELLER / 2, 0)
            draw.line((x0, y, x1, y), fill=(214, 124, 44, 255), width=6)
        x0, y0 = at(-SPAN / 2 + 1, LENGTH + 0.5)
        draw.line((x0, y0, x0 + 5 * SCALE, y0), fill=OUTLINE, width=6)   # Five metres.
    return image


def draw_propeller():
    """One six-blade propeller laid flat, filling a square canvas with a small margin: its centre at the canvas's centre."""
    side = 2048
    scale = 0.9 * side / PROPELLER
    image = Image.new('RGBA', (side, side), WHITE)
    draw = ImageDraw.Draw(image)
    c = side / 2
    radius = PROPELLER / 2
    for n in range(6):
        angle = math.radians(90 + 60 * n)
        along, across = (math.cos(angle), -math.sin(angle)), (math.sin(angle), math.cos(angle))
        leading, trailing = [], []
        steps = 16
        for i in range(steps + 1):
            r = HUB / 2 * 0.8 + (radius - HUB / 2 * 0.8) * i / steps
            u = (r - HUB / 2 * 0.8) / (radius - HUB / 2 * 0.8)
            chord = BLADE_CHORD * (0.65 + 0.7 * u - 0.75 * u * u) if i < steps else BLADE_CHORD * 0.35
            sweep = BLADE_SWEEP * u ** 2.5      # The tip curves back against the turn, as a scimitar.
            mid = r * along[0] + sweep * across[0], r * along[1] + sweep * across[1]
            for side_list, k in ((leading, 1), (trailing, -1)):
                side_list.append((c + (mid[0] + k * chord / 2 * across[0]) * scale, c + (mid[1] + k * chord / 2 * across[1]) * scale))
        draw.polygon(leading + list(reversed(trailing)), fill=DARKER, outline=OUTLINE, width=LINE)
    hub = HUB / 2 * scale
    draw.ellipse((c - hub, c - hub, c + hub, c + hub), fill=PANEL, outline=OUTLINE, width=LINE)
    return image


# The propeller as it ships (owner's card, 26 Nov 2026): from straight above, edge on, a tractor propeller turning in an upright
# plane. Approximate, for layout only: the spinner's length and base, the blades' chord seen from above and their sweep.
EDGE_WIDTH, EDGE_HEIGHT = 2520, 1080          # 21:9, the shape nearest a 4.1 m bar with its spinner and a margin.
EDGE_SCALE = 0.88 * EDGE_WIDTH / PROPELLER    # Pixels a metre.
SPINNER_LENGTH, SPINNER_BASE = 1.05, 0.78     # A pointed bullet spinner, nose forward (up the canvas).
EDGE_CHORD = 0.22                             # The deepest a blade shows from above, in metres.
EDGE_SWEEP = 0.24                             # How far the tip curves back, in metres.
BLACK, BLACK_EDGE = (44, 44, 48, 255), (8, 8, 10, 255)


def draw_propeller_edge():
    """One six-blade propeller seen from straight above, edge on: the spinner's point up the canvas, the blades a thin bar across it.

    With a blade straight out to each side, the other four point 60 degrees up or down from the plane of the picture and so show at
    half their length (cos 60), as shorter slivers over the inner half of each long one, a little ahead or behind it."""
    image = Image.new('RGBA', (EDGE_WIDTH, EDGE_HEIGHT), WHITE)
    draw = ImageDraw.Draw(image)
    s = EDGE_SCALE
    plane_y = EDGE_HEIGHT / 2 + 0.30 * s      # The propeller plane, so the spinner and blades sit centred together.
    cx = EDGE_WIDTH / 2
    root = SPINNER_BASE / 2 * 0.9

    def sliver(side, reach, offset, depth):
        """A blade seen edge on: `reach` metres out from the centre line, `offset` metres behind the plane, `depth` its deepest."""
        front, back = [], []
        steps = 18
        for i in range(steps + 1):
            u = i / steps
            r = root + (reach - root) * u
            d = depth * (0.55 + 0.9 * u - 1.1 * u * u) if i < steps else depth * 0.25
            y = plane_y + (offset + EDGE_SWEEP * u ** 2.2 * reach / (PROPELLER / 2)) * s
            front.append((cx + side * r * s, y - d / 2 * s))
            back.append((cx + side * r * s, y + d / 2 * s))
        draw.polygon(front + list(reversed(back)), fill=BLACK, outline=BLACK_EDGE, width=LINE)

    half = PROPELLER / 2
    for side in (-1, 1):
        sliver(side, half * 0.5, -0.07, EDGE_CHORD * 0.8)    # A blade 60 degrees toward the camera, a little ahead.
        sliver(side, half * 0.5, 0.10, EDGE_CHORD * 0.8)     # A blade 60 degrees away from it, a little behind.
        sliver(side, half, 0.0, EDGE_CHORD)                  # The blade straight out to the side, full length.
    # The spinner: a pointed bullet from its base on the plane forward to its tip.
    outline = []
    steps = 24
    for i in range(steps + 1):
        t = i / steps                          # 0 at the base, 1 at the tip.
        w = SPINNER_BASE / 2 * (1 - t ** 1.8) ** 0.75
        outline.append((cx + w * s, plane_y - (t * SPINNER_LENGTH - 0.12) * s))
    left = [(2 * cx - x, y) for x, y in reversed(outline)]
    draw.polygon(outline + left, fill=BLACK, outline=BLACK_EDGE, width=LINE)
    return image


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    record = {'made_by': 'assets/phobos-vehicles/draw_c27j_plan.py (Pillow shapes; no generator, no photograph, no other art)',
              'pixels_a_metre': round(SCALE, 2),
              'published': {'length_m': LENGTH, 'span_m': SPAN, 'propeller_m': PROPELLER, 'fuselage_width_m': FUSELAGE_WIDTH,
                            'wing_area_m2': 82},
              'approximate_for_layout_only': {'wing_le_m': WING_LE, 'chords_m': [ROOT_CHORD, TIP_CHORD], 'engine_at_m': ENGINE_AT,
                                              'nacelle_m': [NACELLE_WIDTH, NACELLE_FRONT, NACELLE_REAR], 'gear_fairing_m': GEAR_FAIRING,
                                              'tailplane_span_m': TAILPLANE_SPAN, 'hub_m': HUB, 'blade_chord_m': BLADE_CHORD},
              'wing_area_drawn_m2': round(2 * ENGINE_AT * ROOT_CHORD + 2 * (SPAN / 2 - ENGINE_AT) * (ROOT_CHORD + TIP_CHORD) / 2, 1),
              'files': {}}
    for name, image in (('c27j-plan.png', draw_plan(False)), ('c27j-plan-check.png', draw_plan(True)),
                        ('c27j-propeller-plan.png', draw_propeller()), ('c27j-propeller-edge-plan.png', draw_propeller_edge())):
        target = OUT / name
        image.save(target)
        record['files'][name] = hashlib.sha256(target.read_bytes()).hexdigest()
        print(f'wrote {target.relative_to(ROOT).as_posix()}')
    (OUT / 'c27j-plan.record.json').write_text(json.dumps(record, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
