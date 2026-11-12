"""Draw the upgrade families' pictures (D'thorga Framework 0.103.0) with Pillow, in the look of the batch 3 buttons: the all-terrain tyres,
work lamps, auxiliary fuel tank and ferry tank fittings (the tyres, lamps and auxiliary tank also serve as their families' buttons) and the
cold-weather pack's button; and the recovery family's winch, with its Winch and Stop winching buttons. No image generator and no game or other author's art is involved; each has a record beside it.

    python assets/phobos-vehicles/draw_family_icons.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
RUBBER = (54, 56, 60, 255)
STEEL = (150, 156, 162, 255)
DARK = (92, 100, 110, 255)
ORANGE = (214, 124, 44, 255)
MUD = (122, 92, 60, 255)
ICE = (176, 214, 236, 255)
LAMP = (250, 238, 170, 255)
BEAM = (250, 238, 170, 110)
RED = (190, 60, 50, 255)
LINE = 18


def all_terrain_tyres(draw):
    # A tyre side-on with deep square lugs round its rim, standing in a smear of mud.
    draw.ellipse((40, 360, 470, 490), fill=MUD, outline=OUTLINE, width=LINE)
    centre, outer, inner = (256, 250), 205, 95
    for i in range(12):
        # Twelve blocky lugs, alternate ones longer, round the tread.
        import math
        angle = i * math.tau / 12
        reach = outer + (26 if i % 2 == 0 else 14)
        cx, cy = centre
        a0, a1 = angle - 0.17, angle + 0.17
        points = [
            (cx + math.cos(a0) * (outer - 20), cy + math.sin(a0) * (outer - 20)),
            (cx + math.cos(a0) * reach, cy + math.sin(a0) * reach),
            (cx + math.cos(a1) * reach, cy + math.sin(a1) * reach),
            (cx + math.cos(a1) * (outer - 20), cy + math.sin(a1) * (outer - 20)),
        ]
        draw.polygon(points, fill=RUBBER, outline=OUTLINE, width=LINE // 2)
    draw.ellipse((centre[0] - outer, centre[1] - outer, centre[0] + outer, centre[1] + outer), fill=RUBBER, outline=OUTLINE, width=LINE)
    draw.ellipse((centre[0] - inner - 30, centre[1] - inner - 30, centre[0] + inner + 30, centre[1] + inner + 30), outline=DARK, width=12)
    draw.ellipse((centre[0] - inner, centre[1] - inner, centre[0] + inner, centre[1] + inner), fill=STEEL, outline=OUTLINE, width=LINE)
    draw.ellipse((centre[0] - 30, centre[1] - 30, centre[0] + 30, centre[1] + 30), fill=DARK, outline=OUTLINE, width=LINE // 2)


def work_lamps(draw):
    # Two square floodlights on a bracket bar, their beams spreading down onto the ground.
    draw.polygon([(70, 250), (230, 250), (300, 470), (10, 470)], fill=BEAM)
    draw.polygon([(282, 250), (442, 250), (502, 470), (212, 470)], fill=BEAM)
    draw.rounded_rectangle((60, 120, 452, 160), radius=10, fill=DARK, outline=OUTLINE, width=LINE)
    draw.rectangle((236, 40, 276, 125), fill=DARK, outline=OUTLINE, width=LINE)
    for left in (60, 272):
        draw.rounded_rectangle((left, 150, left + 180, 270), radius=14, fill=ORANGE, outline=OUTLINE, width=LINE)
        draw.rounded_rectangle((left + 22, 172, left + 158, 250), radius=8, fill=LAMP, outline=OUTLINE, width=LINE // 2)


def cold_weather(draw):
    # An engine block with a plug-in heater lead on the left, a battery on the right, and a snowflake over both.
    draw.rounded_rectangle((30, 250, 280, 450), radius=18, fill=STEEL, outline=OUTLINE, width=LINE)
    for x in (70, 130, 190):
        draw.rectangle((x, 205, x + 40, 255), fill=DARK, outline=OUTLINE, width=LINE // 2)
    draw.line([(30, 350), (10, 420), (60, 480)], fill=OUTLINE, width=34, joint='curve')
    draw.line([(30, 350), (10, 420), (60, 480)], fill=ORANGE, width=16, joint='curve')
    draw.rounded_rectangle((310, 290, 490, 450), radius=14, fill=DARK, outline=OUTLINE, width=LINE)
    draw.rectangle((335, 258, 380, 292), fill=RED, outline=OUTLINE, width=LINE // 2)
    draw.rectangle((420, 258, 465, 292), fill=STEEL, outline=OUTLINE, width=LINE // 2)
    centre = (256, 120)
    for dx, dy in ((0, 1), (0.87, 0.5), (0.87, -0.5)):
        a = (centre[0] - dx * 95, centre[1] - dy * 95)
        b = (centre[0] + dx * 95, centre[1] + dy * 95)
        draw.line([a, b], fill=OUTLINE, width=40)
        draw.line([a, b], fill=ICE, width=20)
    draw.ellipse((centre[0] - 26, centre[1] - 26, centre[0] + 26, centre[1] + 26), fill=ICE, outline=OUTLINE, width=LINE // 2)


FUEL = (226, 190, 70, 255)


def aux_fuel_tank(draw):
    # A squat steel tank on two cradle straps, an orange filler cap on top and a fuel drop beside it: extra fuel carried aboard.
    draw.rounded_rectangle((40, 170, 380, 430), radius=60, fill=STEEL, outline=OUTLINE, width=LINE)
    for x in (110, 290):
        draw.rectangle((x, 150, x + 30, 450), fill=DARK, outline=OUTLINE, width=LINE // 2)
    draw.rounded_rectangle((180, 110, 250, 175), radius=10, fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.line([(60, 300), (360, 300)], fill=DARK, width=10)
    # The drop: a point at the top and a round foot.
    draw.polygon([(440, 180), (395, 300), (485, 300)], fill=FUEL, outline=OUTLINE, width=LINE // 2)
    draw.ellipse((392, 255, 488, 350), fill=FUEL, outline=OUTLINE, width=LINE)


def ferry_tank(draw):
    # A long cylindrical tank laid along an aircraft's cabin floor, under a swept wing's outline: a ferry tank for a long flight.
    draw.polygon([(60, 150), (300, 150), (470, 60), (500, 90), (360, 200), (60, 200)], fill=DARK, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((40, 260, 470, 400), radius=70, fill=STEEL, outline=OUTLINE, width=LINE)
    for x in (140, 360):
        draw.line([(x, 262), (x, 398)], fill=DARK, width=12)
    draw.rounded_rectangle((235, 225, 290, 268), radius=8, fill=ORANGE, outline=OUTLINE, width=LINE // 2)
    draw.line([(20, 440), (492, 440)], fill=OUTLINE, width=LINE)


def _winch_drum(draw, cx, cy):
    # A winch drum seen from the front: two dark flanges either side of a grey drum wrapped in rope, on an orange frame.
    draw.rounded_rectangle((cx - 170, cy + 60, cx + 170, cy + 110), radius=10, fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((cx - 120, cy - 70, cx + 120, cy + 70), radius=18, fill=STEEL, outline=OUTLINE, width=LINE)
    for y in range(cy - 50, cy + 60, 26):
        draw.line((cx - 112, y, cx + 112, y + 10), fill=DARK, width=10)
    for left in (cx - 160, cx + 120):
        draw.rounded_rectangle((left, cy - 100, left + 40, cy + 100), radius=12, fill=DARK, outline=OUTLINE, width=LINE)


def recovery_winch(draw):
    # The winch drum, its rope running off to a hook.
    _winch_drum(draw, 220, 230)
    draw.line((330, 200, 440, 390), fill=OUTLINE, width=22)
    draw.line((330, 200, 440, 390), fill=STEEL, width=10)
    draw.arc((395, 370, 485, 470), 300, 200, fill=OUTLINE, width=34)
    draw.arc((395, 370, 485, 470), 300, 200, fill=STEEL, width=18)


def winch_button(draw):
    # The rope pulling a small truck's outline towards the drum: the order to winch a vehicle in.
    _winch_drum(draw, 170, 160)
    draw.line((290, 160, 380, 330), fill=OUTLINE, width=22)
    draw.line((290, 160, 380, 330), fill=STEEL, width=10)
    draw.rounded_rectangle((300, 320, 490, 440), radius=16, fill=MUD, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((390, 270, 480, 330), radius=10, fill=MUD, outline=OUTLINE, width=LINE)
    for x in (340, 450):
        draw.ellipse((x - 34, 410, x + 34, 478), fill=RUBBER, outline=OUTLINE, width=LINE)
    draw.polygon([(60, 400), (190, 330), (190, 470)], fill=ORANGE, outline=OUTLINE)


def stop_winch(draw):
    # The winch drum under a red bar: the order to stop winching.
    _winch_drum(draw, 256, 240)
    draw.line((90, 430, 422, 70), fill=OUTLINE, width=70)
    draw.line((90, 430, 422, 70), fill=RED, width=44)


ICONS = [
    ('Fittings/AllTerrainTyres', all_terrain_tyres, 'a dark tyre side-on with deep square lugs round its tread and a grey rim, standing in brown mud'),
    ('Fittings/WorkLamps', work_lamps, 'two orange floodlights with pale yellow lenses on a dark bracket bar, their pale beams spreading downward'),
    ('Families/ColdWeather', cold_weather, 'a grey engine block with an orange heater lead, a dark battery with red and grey terminals, and a pale blue snowflake above'),
    ('Fittings/AuxFuelTank', aux_fuel_tank, 'a squat grey tank on two dark cradle straps with an orange filler cap, and a yellow fuel drop beside it'),
    ('Fittings/FerryTank', ferry_tank, 'a long grey cylindrical tank with an orange filler cap lying on a floor line, under the dark outline of a swept wing'),
    ('Fittings/RecoveryWinch', recovery_winch, 'a grey winch drum wrapped in dark rope between two dark flanges on an orange frame, its steel rope running down to a hook'),
    ('Hitch/Winch', winch_button, 'a grey winch drum on an orange frame, its steel rope pulling a brown truck outline towards it, with an orange arrow'),
    ('Hitch/StopWinch', stop_winch, 'a grey winch drum on an orange frame, crossed by a red bar'),
]


def main():
    for name, drawing, what in ICONS:
        path = FOLDER / f'{name}.png'
        path.parent.mkdir(parents=True, exist_ok=True)
        image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        layer = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        drawing(ImageDraw.Draw(layer, 'RGBA'))
        image.alpha_composite(layer)
        image.resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {path.stem}\n\n**Drawn in code** by `assets/phobos-vehicles/draw_family_icons.py` (Pillow): {what}. No image generator, no '
            f'reference images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.relative_to(ROOT))


if __name__ == '__main__':
    main()
