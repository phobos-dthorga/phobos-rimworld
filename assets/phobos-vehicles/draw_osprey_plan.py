"""Draw the V-22 Osprey's plan from above with Pillow, as a layout reference of our own for the paid picture.

No image generator, photograph, drawing or other author's art is involved: every shape is placed from a published dimension
or, where none was read, from an approximate figure marked as such below. The plan is a reference only and is never shipped.

    python assets/phobos-vehicles/draw_osprey_plan.py

It writes two files to assets/phobos-vehicles/references/ (kept off main; drawn again by this script):
  osprey-plan.png        the airframe alone, nacelles upright, no rotors: the reference a service is given
  osprey-plan-check.png  the same with the two rotor discs and a scale bar, for our own check; never sent anywhere

Published (docs/development/vehicle-osprey.md): fuselage 17.48 m long; 25.78 m across the turning rotors; rotors 11.61 m
across, so their centres are 14.17 m apart; folded width 5.61 m, which the tail sets. Everything else is approximate, by
eye from general knowledge of the aircraft, and is for layout only: fuselage and sponson widths, the wing's chord and where
it sits, the nacelles' and the tail's sizes.
"""
import hashlib
import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'assets/phobos-vehicles/references'
CANVAS = 2048
FILL = 0.88                       # The airframe's length as a share of the canvas height.
LENGTH = 17.48                    # Published.
ROTOR = 11.61                     # Published.
ROTOR_CENTRES = 25.78 - ROTOR     # Published width across the rotors, less one rotor.
TAIL_SPAN = 5.61                  # Published folded width.
SCALE = FILL * CANVAS / LENGTH    # Pixels a metre.

# Approximate, for layout only.
FUSELAGE_WIDTH = 2.3
SPONSON_WIDTH = 4.4
SPONSON_FROM, SPONSON_TO = 4.6, 11.6     # Metres back from the nose.
WING_AT = 6.9                            # The wing's mid-chord, metres back from the nose.
WING_CHORD = 2.5
NACELLE_WIDTH, NACELLE_LENGTH = 1.5, 2.6
TAIL_CHORD = 1.9
FIN_THICKNESS = 0.3

OUTLINE = (24, 24, 26, 255)
PANEL = (190, 194, 198, 255)
DARKER = (150, 154, 160, 255)
GLASS = (70, 82, 96, 255)
WHITE = (255, 255, 255, 255)
LINE = 8


def at(across, back):
    """Canvas pixels for a point `across` metres right of the centre line and `back` metres behind the nose."""
    top = (CANVAS - LENGTH * SCALE) / 2
    return CANVAS / 2 + across * SCALE, top + back * SCALE


def box(draw, left, front, right, rear, fill, radius=0.25):
    (x0, y0), (x1, y1) = at(left, front), at(right, rear)
    draw.rounded_rectangle((x0, y0, x1, y1), radius=int(radius * SCALE), fill=fill, outline=OUTLINE, width=LINE)


def airframe(draw):
    half = FUSELAGE_WIDTH / 2
    # Tail first, so the fuselage overlaps its root: the stabiliser across the very rear, a fin at each tip.
    box(draw, -TAIL_SPAN / 2, LENGTH - TAIL_CHORD - 0.1, TAIL_SPAN / 2, LENGTH - 0.1, PANEL)
    for side in (-1, 1):
        tip = side * TAIL_SPAN / 2
        box(draw, tip - FIN_THICKNESS / 2, LENGTH - TAIL_CHORD - 0.5, tip + FIN_THICKNESS / 2, LENGTH + 0.05, DARKER, radius=0.1)
    # The wing, straight across, with a nacelle at each tip.
    wing_half = ROTOR_CENTRES / 2
    box(draw, -wing_half, WING_AT - WING_CHORD / 2, wing_half, WING_AT + WING_CHORD / 2, PANEL)
    for side in (-1, 1):
        box(draw, side * wing_half - NACELLE_WIDTH / 2, WING_AT - NACELLE_LENGTH / 2,
            side * wing_half + NACELLE_WIDTH / 2, WING_AT + NACELLE_LENGTH / 2, DARKER, radius=0.5)
        x, y = at(side * wing_half, WING_AT)
        hub = 0.45 * SCALE
        draw.ellipse((x - hub, y - hub, x + hub, y + hub), fill=PANEL, outline=OUTLINE, width=LINE)
    # Sponsons, then the fuselage over them: a rounded nose, a straight cabin, a rear that narrows to the tail.
    box(draw, -SPONSON_WIDTH / 2, SPONSON_FROM, SPONSON_WIDTH / 2, SPONSON_TO, DARKER, radius=0.7)
    taper_from, tail_half = 12.6, 0.75
    nose = [at(0, 0)]
    for step in range(1, 13):                 # A rounded nose over the first 2.4 metres.
        back = 2.4 * step / 12
        nose.append(at(half * (1 - (1 - back / 2.4) ** 2) ** 0.5, back))
    right = nose + [at(half, taper_from), at(tail_half, LENGTH - 0.4), at(tail_half * 0.6, LENGTH)]
    left = [(CANVAS - x, y) for x, y in reversed(right)]
    draw.polygon(right + left, fill=PANEL, outline=OUTLINE, width=LINE)
    # The fairing over the wing's centre, and the cockpit glazing.
    box(draw, -half * 0.75, WING_AT - WING_CHORD / 2 - 0.5, half * 0.75, WING_AT + WING_CHORD / 2 + 0.9, DARKER, radius=0.5)
    box(draw, -half * 0.72, 1.1, half * 0.72, 2.7, GLASS, radius=0.45)


def draw_plan(check):
    image = Image.new('RGBA', (CANVAS, CANVAS), WHITE)
    draw = ImageDraw.Draw(image)
    airframe(draw)
    if check:
        for side in (-1, 1):
            x, y = at(side * ROTOR_CENTRES / 2, WING_AT)
            radius = ROTOR / 2 * SCALE
            draw.ellipse((x - radius, y - radius, x + radius, y + radius), outline=(214, 124, 44, 255), width=4)
        x0, y0 = at(-LENGTH / 2 + 1, LENGTH + 0.7)
        draw.line((x0, y0, x0 + 5 * SCALE, y0), fill=OUTLINE, width=6)   # Five metres.
    return image


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    record = {'made_by': 'assets/phobos-vehicles/draw_osprey_plan.py (Pillow shapes; no generator, no photograph, no other art)',
              'pixels_a_metre': round(SCALE, 2), 'published': {'length_m': LENGTH, 'rotor_m': ROTOR, 'rotor_centres_m': round(ROTOR_CENTRES, 2),
                                                                'tail_span_m': TAIL_SPAN},
              'approximate_for_layout_only': {'fuselage_width_m': FUSELAGE_WIDTH, 'sponson_width_m': SPONSON_WIDTH, 'wing_at_m': WING_AT,
                                              'wing_chord_m': WING_CHORD, 'nacelle_m': [NACELLE_WIDTH, NACELLE_LENGTH],
                                              'tail_chord_m': TAIL_CHORD},
              'files': {}}
    for name, check in (('osprey-plan.png', False), ('osprey-plan-check.png', True)):
        target = OUT / name
        draw_plan(check).save(target)
        record['files'][name] = hashlib.sha256(target.read_bytes()).hexdigest()
        print(f'wrote {target.relative_to(ROOT).as_posix()}')
    (OUT / 'osprey-plan.record.json').write_text(json.dumps(record, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
