"""Draw D'thorga Framework's vehicle systems button icons with Pillow, in the game's look: a thick near-black outline, pale fills
and flat cel shading. No image generator and no game or other author's art is involved.

    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/vehicle-systems/draw_icons.py [name ...]

Name icons to draw only those. Each icon goes into the mod with a record saying it is a stand-in drawn in code; generated art can
replace it on the owner's go, and then its entry here is removed so this can't overwrite the pick.

- Switch: a rocker switch, the fallback for any load a loadout gives no icon of its own. Kept by the owner on 10 Nov 2026, when every
  generated version came out as a medical cross or a blank plate.

- Flight/TakeOff, Flight/FlyTo, Flight/Hold, Flight/Land: our own flight's four buttons (Framework 0.147.0, 19 Nov 2026), drawn while
  the game was running, so the graphics card was not free; generated pictures can replace them on the owner's go.

The other buttons this once drew have generated pictures the owner picked (9 and 10 Nov 2026).
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


# A gun's three fire modes (Framework 0.152.0): Vehicle Framework names its own fire-mode pictures, but on RimWorld 1.7 it no longer
# loads them, so a turret of ours showed the red cross. Brass cartridges: one, three, and a belt.
BRASS = (222, 176, 82, 255)
BRASS_SHADE = (178, 132, 50, 255)
COPPER = (196, 112, 70, 255)


def cartridge(draw, cx, bottom, width, height):
    """One upright cartridge: a brass case under a copper bullet."""
    case_top = bottom - height * 0.62
    half = width / 2
    outlined_polygon(draw, [(cx - half, case_top), (cx + half, case_top), (cx + half, bottom), (cx - half, bottom)], BRASS, BRASS_SHADE,
                     [(cx + half * 0.35, case_top + LINE * 0.6), (cx + half - LINE * 0.6, case_top + LINE * 0.6),
                      (cx + half - LINE * 0.6, bottom - LINE * 0.6), (cx + half * 0.35, bottom - LINE * 0.6)])
    tip = bottom - height
    bullet = [(cx - half * 0.86, case_top + LINE * 0.3), (cx - half * 0.86, case_top - height * 0.12), (cx - half * 0.45, tip + height * 0.08),
              (cx, tip), (cx + half * 0.45, tip + height * 0.08), (cx + half * 0.86, case_top - height * 0.12), (cx + half * 0.86, case_top + LINE * 0.3)]
    outlined_polygon(draw, bullet, COPPER)


def fire_single():
    image, draw = canvas()
    cartridge(draw, 512, 900, 230, 780)
    return image


def fire_burst():
    image, draw = canvas()
    for cx in (232, 512, 792):
        cartridge(draw, cx, 860, 190, 640)
    return image


def fire_auto():
    image, draw = canvas()
    for cx in (152, 332, 512, 692, 872):
        cartridge(draw, cx, 720, 150, 560)
    # The belt's links, joining the cartridges.
    outlined_polygon(draw, [(70, 690), (954, 690), (954, 840), (70, 840)], STEEL_SHADE)
    return image


DRAWINGS = {
    'Switch': (switch, 'a rocker switch on a pale plate, the fallback icon for any switchable load'),
    'FireMode/Single': (fire_single, 'one brass cartridge, for a gun\'s "single shot" fire mode'),
    'FireMode/Burst': (fire_burst, 'three brass cartridges, for a gun\'s "burst" fire mode'),
    'FireMode/Auto': (fire_auto, 'five brass cartridges on a steel belt, for a gun\'s "automatic" fire mode'),
    'Flight/TakeOff': (take_off, 'an amber arrow rising from a pale landing pad, for our own flight\'s "Take off" button'),
    'Flight/FlyTo': (fly_to, 'a steel arrow flying toward an amber target ring, for "Fly to"'),
    'Flight/Hold': (hold, 'two amber bars, the sign for a pause, in a pale ring, for "Hold here"'),
    'Flight/Land': (land, 'an amber arrow coming down onto a pale landing pad, for "Land here"'),
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
