"""Draw D'thorga Framework's vehicle systems button icons with Pillow, in the game's look: a thick near-black outline, pale fills
and flat cel shading. No image generator and no game or other author's art is involved.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/vehicle-systems/draw_icons.py [name ...]

Name icons to draw only those. Each icon goes into the mod with a record saying it is a stand-in drawn in code; generated art can
replace it on the owner's go, and then its entry here is removed so this can't overwrite the pick.

- Service: a spanner, for "service when possible".
- Switch: a rocker switch, the fallback for any load a loadout gives no icon of its own.
- Headlights: a headlamp with three beams.
- CabHeater: three rising heat waves over a fan's grille.
"""
import datetime as dt
import hashlib
import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
TEXTURES = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
WORK = 1024  # Drawn large and scaled down, which smooths the edges.
FINAL = 128
LINE = 44
OUTLINE = (24, 24, 26, 255)
PALE = (226, 222, 210, 255)
PALE_SHADE = (184, 180, 168, 255)
STEEL = (160, 164, 168, 255)
STEEL_SHADE = (118, 122, 128, 255)
AMBER = (240, 196, 92, 255)
AMBER_SHADE = (206, 156, 60, 255)
RED = (196, 84, 64, 255)
RED_SHADE = (150, 60, 46, 255)
BLUE = (124, 176, 220, 255)
OUTLINE_LEAD = (44, 44, 48, 255)


def canvas():
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def outlined_polygon(draw, points, fill, shade=None, shade_points=None):
    draw.polygon(points, fill=OUTLINE)
    inner = shrink(points, LINE * 0.75)
    draw.polygon(inner, fill=fill)
    if shade and shade_points:
        draw.polygon(shade_points, fill=shade)


def shrink(points, amount):
    cx = sum(p[0] for p in points) / len(points)
    cy = sum(p[1] for p in points) / len(points)
    result = []
    for x, y in points:
        dx, dy = x - cx, y - cy
        length = math.hypot(dx, dy) or 1
        result.append((x - dx / length * amount, y - dy / length * amount))
    return result


def thick_line(draw, points, width, fill):
    draw.line(points, fill=OUTLINE, width=width + LINE, joint='curve')
    for x, y in (points[0], points[-1]):
        r = (width + LINE) / 2
        draw.ellipse((x - r, y - r, x + r, y + r), fill=OUTLINE)
    draw.line(points, fill=fill, width=width, joint='curve')
    for x, y in (points[0], points[-1]):
        r = width / 2
        draw.ellipse((x - r, y - r, x + r, y + r), fill=fill)


def service():
    image, draw = canvas()
    # Spanner: a straight handle laid diagonally, an open jaw at the top left and a ring at the bottom right.
    jaw, ring = (300, 300), (740, 740)
    for centre, radius in ((jaw, 175), (ring, 125)):
        x, y = centre
        draw.ellipse((x - radius - LINE, y - radius - LINE, x + radius + LINE, y + radius + LINE), fill=OUTLINE)
    thick_line(draw, [jaw, ring], 120, STEEL)
    for centre, radius in ((jaw, 175), (ring, 125)):
        x, y = centre
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=STEEL)
    # The jaw's mouth opens to the top left; the ring has a hole.
    mouth = [(jaw[0] + 10, jaw[1] + 10), (jaw[0] - 260, jaw[1] - 70), (jaw[0] - 70, jaw[1] - 260)]
    draw.polygon([(mouth[0][0] + LINE, mouth[0][1] + LINE), (mouth[1][0], mouth[1][1] + LINE), (mouth[2][0] + LINE, mouth[2][1])], fill=OUTLINE)
    draw.polygon(mouth, fill=(0, 0, 0, 0))
    x, y = ring
    draw.ellipse((x - 70, y - 70, x + 70, y + 70), fill=OUTLINE)
    draw.ellipse((x - 42, y - 42, x + 42, y + 42), fill=(0, 0, 0, 0))
    thick_line(draw, [(jaw[0] + 120, jaw[1] + 170), (ring[0] - 170, ring[1] - 80)], 22, STEEL_SHADE)
    return image


def switch():
    image, draw = canvas()
    plate = [(250, 170), (774, 170), (774, 854), (250, 854)]
    outlined_polygon(draw, plate, PALE, PALE_SHADE, [(700, 220), (730, 220), (730, 810), (700, 810)])
    rocker_top = [(370, 260), (654, 330), (654, 540), (370, 540)]
    rocker_bottom = [(370, 540), (654, 540), (654, 750), (370, 750)]
    outlined_polygon(draw, rocker_bottom, STEEL_SHADE)
    outlined_polygon(draw, rocker_top, AMBER, AMBER_SHADE, [(560, 340), (630, 356), (630, 520), (560, 520)])
    return image


def headlights():
    image, draw = canvas()
    # Lamp: a disc with a chrome rim, beams to the right.
    cx, cy, r = 340, 512, 230
    draw.ellipse((cx - r - LINE, cy - r - LINE, cx + r + LINE, cy + r + LINE), fill=OUTLINE)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=STEEL)
    inner = r - 60
    draw.ellipse((cx - inner, cy - inner, cx + inner, cy + inner), fill=AMBER)
    draw.pieslice((cx - inner, cy - inner, cx + inner, cy + inner), 200, 340, fill=(250, 232, 160, 255))
    for dy in (-200, 0, 200):
        thick_line(draw, [(640, cy + dy * 0.6), (900, cy + dy)], 54, AMBER)
    return image


def cab_heater():
    image, draw = canvas()
    # Grille: a rounded box with slats.
    grille = [(220, 600), (804, 600), (804, 880), (220, 880)]
    outlined_polygon(draw, grille, STEEL, STEEL_SHADE, [(740, 640), (770, 640), (770, 846), (740, 846)])
    for x in (330, 450, 570, 690):
        draw.line([(x, 650), (x, 830)], fill=OUTLINE, width=34)
    # Heat waves rising.
    for x in (330, 512, 694):
        points = []
        for i in range(41):
            t = i / 40
            y = 540 - t * 400
            points.append((x + math.sin(t * math.pi * 2.2) * 50, y))
        thick_line(draw, points, 56, RED)
    return image


def air_conditioner():
    image, draw = canvas()
    # A vent box with slats, and three pale-blue cold waves falling from it.
    box = [(220, 140), (804, 140), (804, 420), (220, 420)]
    outlined_polygon(draw, box, STEEL, STEEL_SHADE, [(740, 180), (770, 180), (770, 386), (740, 386)])
    for x in (330, 450, 570, 690):
        draw.line([(x, 190), (x, 370)], fill=OUTLINE, width=34)
    for x in (330, 512, 694):
        points = []
        for i in range(41):
            t = i / 40
            y = 480 + t * 400
            points.append((x + math.sin(t * math.pi * 2.2) * 50, y))
        thick_line(draw, points, 56, BLUE)
    return image


def night_heater():
    image, draw = canvas()
    # A squat heater on legs with a flame in its window, and a crescent moon above it.
    body = [(200, 420), (824, 420), (824, 860), (200, 860)]
    outlined_polygon(draw, body, STEEL_SHADE, STEEL, [(240, 460), (784, 460), (784, 500), (240, 500)])
    for x in (260, 764):
        draw.line([(x, 860), (x, 960)], fill=OUTLINE, width=50)
    window = [(360, 520), (664, 520), (664, 800), (360, 800)]
    draw.polygon(window, fill=OUTLINE)
    draw.polygon(shrink(window, 24), fill=(70, 40, 30, 255))
    flame = [(512, 560), (600, 660), (560, 700), (620, 780), (512, 790), (404, 780), (464, 700), (424, 660)]
    draw.polygon(flame, fill=AMBER)
    draw.polygon([(512, 640), (560, 710), (512, 775), (464, 710)], fill=RED)
    cx, cy, r = 760, 230, 130
    draw.ellipse((cx - r - LINE, cy - r - LINE, cx + r + LINE, cy + r + LINE), fill=OUTLINE)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=PALE)
    draw.ellipse((cx - r + 70, cy - r - 10, cx + r + 70, cy + r - 10), fill=(0, 0, 0, 0))
    return image


def jump_start_kit():
    image, draw = canvas()
    # Two coiled leads, red and black, each ending in a sprung clamp.
    for colour, offset in ((RED, 0), (OUTLINE_LEAD, 220)):
        points = []
        for i in range(61):
            t = i / 60
            x = 150 + t * 560
            y = 300 + offset + math.sin(t * math.pi * 3) * 70
            points.append((x, y))
        thick_line(draw, points, 40, colour)
        jaw_x, jaw_y = 710, 300 + offset
        for dy in (-60, 60):
            thick_line(draw, [(jaw_x, jaw_y), (jaw_x + 190, jaw_y + dy)], 44, STEEL)
        draw.ellipse((jaw_x - 60, jaw_y - 60, jaw_x + 60, jaw_y + 60), fill=OUTLINE)
        draw.ellipse((jaw_x - 36, jaw_y - 36, jaw_x + 36, jaw_y + 36), fill=colour)
    return image


def booster_pack():
    image, draw = canvas()
    # A squat battery case with a carrying handle, three amber charge bars, and two short leads with clamps out to the right.
    handle = [(360, 170), (560, 170), (560, 300), (360, 300)]
    draw.rounded_rectangle((330 - LINE, 150 - LINE, 590 + LINE, 330), radius=70, fill=OUTLINE)
    draw.rounded_rectangle((330, 150, 590, 330), radius=50, fill=STEEL_SHADE)
    draw.rectangle(handle[0] + handle[2], fill=(0, 0, 0, 0))
    case = [(160, 300), (760, 300), (760, 860), (160, 860)]
    outlined_polygon(draw, case, PALE_SHADE, STEEL_SHADE, [(690, 340), (724, 340), (724, 826), (690, 826)])
    panel = [(240, 400), (600, 400), (600, 560), (240, 560)]
    draw.polygon(panel, fill=OUTLINE)
    for i in range(3):
        x0 = 268 + i * 112
        draw.rectangle((x0, 428, x0 + 84, 532), fill=AMBER if i < 2 else (70, 64, 52, 255))
    for colour, y in ((RED, 660), (OUTLINE_LEAD, 780)):
        thick_line(draw, [(700, y), (800, y - 20), (860, y - 60)], 36, colour)
        for dy in (-40, 40):
            thick_line(draw, [(860, y - 60), (960, y - 60 + dy)], 34, STEEL)
    return image


def climate_control():
    image, draw = canvas()
    # A thermometer in the middle, a red flame on the left and a pale-blue snowflake on the right: heating and cooling by itself.
    stem = [(452, 160), (572, 160), (572, 640), (452, 640)]
    draw.rounded_rectangle((452 - LINE, 140 - LINE, 572 + LINE, 660), radius=80, fill=OUTLINE)
    draw.rounded_rectangle((452, 140, 572, 660), radius=60, fill=PALE)
    cx, cy, r = 512, 760, 130
    draw.ellipse((cx - r - LINE, cy - r - LINE, cx + r + LINE, cy + r + LINE), fill=OUTLINE)
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=RED)
    draw.rectangle((482, 380, 542, 700), fill=RED)
    for y in (240, 330, 420, 510):
        draw.line([(452, y), (500, y)], fill=OUTLINE, width=20)
    flame = [(200, 760), (250, 600), (300, 680), (330, 520), (380, 640), (400, 760), (300, 840)]
    draw.polygon(flame, fill=OUTLINE)
    draw.polygon(shrink(flame, 26), fill=AMBER)
    sx, sy = 780, 520
    for angle in (0, 60, 120):
        dx = math.cos(math.radians(angle)) * 150
        dy = math.sin(math.radians(angle)) * 150
        thick_line(draw, [(sx - dx, sy - dy), (sx + dx, sy + dy)], 34, BLUE)
    return image


DRAWINGS = {
    'Service': (service, 'a steel spanner laid diagonally, open jaw and ring end, for "service when possible"'),
    'Switch': (switch, 'a rocker switch on a pale plate, the fallback icon for any switchable load'),
    'Headlights': (headlights, 'a headlamp with a chrome rim and three amber beams'),
    'CabHeater': (cab_heater, "three red heat waves rising from a steel fan grille"),
    'AirConditioner': (air_conditioner, 'three pale-blue cold waves falling from a slatted steel vent'),
    'NightHeater': (night_heater, 'a squat parking heater on legs with a flame in its window and a crescent moon above'),
    'JumpStartKit': (jump_start_kit, 'two coiled jump leads, red and black, each ending in a sprung steel clamp'),
    'ClimateControl': (climate_control, 'a pale thermometer with a red bulb between an amber flame and a pale-blue snowflake'),
    'BoosterPack': (booster_pack, 'a squat pale battery case with a carrying handle, three charge bars (two lit amber) and two short leads with steel clamps'),
}

# Textures that live in a subfolder (an item's texture under Things/).
SUBFOLDERS = {'JumpStartKit': 'Things', 'BoosterPack': 'Things'}


def write(name):
    draw_fn, description = DRAWINGS[name]
    image = draw_fn().resize((FINAL, FINAL), Image.LANCZOS)
    folder = TEXTURES / SUBFOLDERS.get(name, '')
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / (name + '.png')
    image.save(path)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    today = dt.date.today().strftime('%d %B %Y')
    record = (
        f'# {name}\n\n'
        f'**A stand-in, drawn in code** by `assets/vehicle-systems/draw_icons.py` (Pillow) on {today}: {description}. '
        'No image generator, no reference images and no game or other author\'s art was used. '
        f'{FINAL} x {FINAL} px, transparent background.\n\n'
        '**Outstanding:** generated art can replace it on the owner\'s go (docs/development/art-backlog.md).\n\n'
        f'- `{name}.png`: SHA-256 `{digest}`\n'
    )
    (folder / (name + '.record.md')).write_text(record, encoding='utf-8', newline='\n')
    print(name, digest)


if __name__ == '__main__':
    for name in sys.argv[1:] or DRAWINGS:
        write(name)
