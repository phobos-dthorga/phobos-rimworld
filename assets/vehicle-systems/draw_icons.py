"""Draw D'thorga Framework's vehicle systems button icons with Pillow, in the game's look: a thick near-black outline, pale fills
and flat cel shading. No image generator and no game or other author's art is involved.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/vehicle-systems/draw_icons.py [name ...]

Name icons to draw only those. Each icon goes into the mod with a record saying it is a stand-in drawn in code; generated art can
replace it on the owner's go, and then its entry here is removed so this can't overwrite the pick.

- Switch: a rocker switch, the fallback for any load a loadout gives no icon of its own. Kept by the owner on 10 Nov 2026, when every
  generated version came out as a medical cross or a blank plate.

- Flight/TakeOff, Flight/FlyTo, Flight/Hold, Flight/Land: our own flight's four buttons (Framework 0.147.0, 19 Nov 2026), drawn while
  the game was running, so the graphics card was not free; generated pictures can replace them on the owner's go.
- Flight/Circle, Flight/CircleSize: circling's two buttons (Framework 0.153.0, 19 Nov 2026), stand-ins likewise.
The other buttons this once drew have generated pictures the owner picked (9 and 10 Nov 2026); the two line fittings (FastRope, RescueHoist)
and the three fire modes (FireMode/Single, Burst, Auto) theirs on 19 Nov 2026 (assets/phobos-vehicles/requests/osprey-night7-picks-record.md).
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
STEEL_SHADE = (118, 122, 128, 255)
AMBER = (240, 196, 92, 255)
AMBER_SHADE = (206, 156, 60, 255)


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


def switch():
    image, draw = canvas()
    plate = [(250, 170), (774, 170), (774, 854), (250, 854)]
    outlined_polygon(draw, plate, PALE, PALE_SHADE, [(700, 220), (730, 220), (730, 810), (700, 810)])
    rocker_top = [(370, 260), (654, 330), (654, 540), (370, 540)]
    rocker_bottom = [(370, 540), (654, 540), (654, 750), (370, 750)]
    outlined_polygon(draw, rocker_bottom, STEEL_SHADE)
    outlined_polygon(draw, rocker_top, AMBER, AMBER_SHADE, [(560, 340), (630, 356), (630, 520), (560, 520)])
    return image


def outlined_ellipse(draw, box, fill):
    draw.ellipse(box, fill=OUTLINE)
    x0, y0, x1, y1 = box
    inset = LINE * 0.75
    draw.ellipse((x0 + inset, y0 + inset, x1 - inset, y1 - inset), fill=fill)


def pad(draw, cy=820):
    """A flat landing pad: a pale disc seen at a low angle."""
    outlined_ellipse(draw, (190, cy - 90, 834, cy + 90), PALE)
    draw.ellipse((330, cy - 40, 694, cy + 40), fill=PALE_SHADE)


# Our own flight's four buttons (Framework 0.147.0): stand-ins until generated art is picked on the owner's go.
def take_off():
    image, draw = canvas()
    pad(draw)
    outlined_polygon(draw, [(512, 110), (780, 420), (620, 420), (620, 700), (404, 700), (404, 420), (244, 420)], AMBER, AMBER_SHADE,
                     [(560, 420), (620, 420), (620, 680), (560, 680)])
    return image


def fly_to():
    image, draw = canvas()
    outlined_ellipse(draw, (560, 150, 900, 490), PALE)
    outlined_ellipse(draw, (650, 240, 810, 400), AMBER)
    outlined_polygon(draw, [(130, 880), (520, 470), (440, 400), (700, 330), (630, 590), (560, 520), (190, 930)], STEEL_SHADE)
    return image


def hold():
    image, draw = canvas()
    outlined_ellipse(draw, (150, 150, 874, 874), PALE)
    outlined_polygon(draw, [(360, 320), (470, 320), (470, 704), (360, 704)], AMBER, AMBER_SHADE, [(430, 340), (450, 340), (450, 684), (430, 684)])
    outlined_polygon(draw, [(554, 320), (664, 320), (664, 704), (554, 704)], AMBER, AMBER_SHADE, [(624, 340), (644, 340), (644, 684), (624, 684)])
    return image


def land():
    image, draw = canvas()
    pad(draw)
    outlined_polygon(draw, [(512, 700), (780, 390), (620, 390), (620, 110), (404, 110), (404, 390), (244, 390)], AMBER, AMBER_SHADE,
                     [(560, 130), (620, 130), (620, 390), (560, 390)])
    return image


# Circling (Framework 0.153.0): stand-ins too. A steel arrow sweeping round an amber centre; and three rings, small to large, for the
# size and way round.
def circle():
    image, draw = canvas()
    outlined_ellipse(draw, (402, 402, 622, 622), AMBER)
    # The sweep: a thick ring open at the top right, ending in an arrowhead.
    draw.arc((150, 150, 874, 874), start=-40, end=250, fill=OUTLINE, width=int(LINE * 3.2))
    draw.arc((150 + LINE * 0.6, 150 + LINE * 0.6, 874 - LINE * 0.6, 874 - LINE * 0.6), start=-38, end=248, fill=STEEL_SHADE, width=int(LINE * 1.7))
    outlined_polygon(draw, [(560, 60), (840, 200), (600, 330)], STEEL_SHADE)
    return image


def circle_size():
    image, draw = canvas()
    # Three rings standing on one point, small to large, so they read as sizes rather than a target.
    for box in ((120, 120, 904, 904), (270, 390, 754, 874), (400, 620, 624, 844)):
        draw.ellipse(box, outline=OUTLINE, width=int(LINE * 2.2))
        inset = LINE * 0.45
        draw.ellipse((box[0] + inset, box[1] + inset, box[2] - inset, box[3] - inset), outline=AMBER, width=int(LINE * 1.25))
    return image


DRAWINGS = {
    'Switch': (switch, 'a rocker switch on a pale plate, the fallback icon for any switchable load'),
    'Flight/TakeOff': (take_off, 'an amber arrow rising from a pale landing pad, for our own flight\'s "Take off" button'),
    'Flight/FlyTo': (fly_to, 'a steel arrow flying toward an amber target ring, for "Fly to"'),
    'Flight/Hold': (hold, 'two amber bars, the sign for a pause, in a pale ring, for "Hold here"'),
    'Flight/Land': (land, 'an amber arrow coming down onto a pale landing pad, for "Land here"'),
    'Flight/Circle': (circle, 'a steel arrow sweeping round an amber centre, for "Circle here"'),
    'Flight/CircleSize': (circle_size, 'three amber rings, small to large, for the circle size and way round'),
}


def write(name):
    draw_fn, description = DRAWINGS[name]
    image = draw_fn().resize((FINAL, FINAL), Image.LANCZOS)
    path = TEXTURES / (name + '.png')
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    today = dt.date.today().strftime('%d %B %Y')
    short = name.split('/')[-1]
    record = (
        f'# {short}\n\n'
        f'**A stand-in, drawn in code** by `assets/vehicle-systems/draw_icons.py` (Pillow) on {today}: {description}. '
        'No image generator, no reference images and no game or other author\'s art was used. '
        f'{FINAL} x {FINAL} px, transparent background.\n\n'
        '**Outstanding:** generated art can replace it on the owner\'s go (docs/development/art-backlog.md).\n\n'
        f'- `{short}.png`: SHA-256 `{digest}`\n'
    )
    path.with_name(short + '.record.md').write_text(record, encoding='utf-8', newline='\n')
    print(name, digest)


if __name__ == '__main__':
    for name in sys.argv[1:] or DRAWINGS:
        write(name)
