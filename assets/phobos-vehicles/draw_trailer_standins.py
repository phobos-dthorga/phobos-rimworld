"""Layout stand-ins for the working trailers' set-out pieces, drawn large as references for generated art.

Run with the ComfyUI Python, which has Pillow:
    C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I assets/phobos-vehicles/draw_trailer_standins.py [name ...]

Each drawing is a flat plan seen from directly above, south face hinted, so the image model redraws its layout instead of
inventing an angle (the route that worked for the hyperbaric chamber and the electrolyser). Nothing here ships: the drawings go to
`assets/phobos-vehicles/references/`, which is ignored on main. No image generator, game art or other author's art is used.
"""
import datetime as dt
import hashlib
import sys
from pathlib import Path

from PIL import Image, ImageDraw
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'scripts' / 'art'))
from art_paths import record_path  # noqa: E402  (records of shipped files are kept under assets/records/)

OUT = Path(__file__).resolve().parent / 'references'
INK = (30, 30, 30)
W = 14  # outline width at reference size
STEEL = (168, 172, 176)
STEEL_LIGHT = (205, 208, 211)
STEEL_DARK = (110, 114, 120)
IRON = (62, 62, 66)
OLIVE = (112, 118, 78)
OLIVE_LIGHT = (146, 152, 104)
ORANGE = (214, 128, 50)
PALE = (226, 228, 222)
BLUE = (96, 140, 186)
RED = (186, 70, 60)


def canvas(w, h):
    return Image.new('RGB', (w, h), (255, 255, 255))


def box(d, xy, fill, r=24):
    d.rounded_rectangle(xy, radius=r, fill=fill, outline=INK, width=W)


def ring(d, cx, cy, r, fill, width=W):
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=fill, outline=INK, width=width)


def burner(d, cx, cy, r):
    """A round burner with a cast-iron pot grate over it."""
    ring(d, cx, cy, r, IRON)
    ring(d, cx, cy, int(r * 0.55), (90, 60, 40))
    for k in range(4):
        if k % 2 == 0:
            d.line((cx - r + 10, cy, cx + r - 10, cy), fill=(20, 20, 20), width=12)
        else:
            d.line((cx, cy - r + 10, cx, cy + r - 10), fill=(20, 20, 20), width=12)


def field_range():
    """Billy's field range, 2 x 1: a long steel cooking cabinet, two burners and a griddle, a flue at the back."""
    im = canvas(1536, 768)
    d = ImageDraw.Draw(im)
    box(d, (90, 150, 1446, 650), OLIVE, 30)              # cabinet body
    box(d, (120, 180, 1416, 560), STEEL, 18)             # steel cook top
    burner(d, 350, 370, 140)
    burner(d, 720, 370, 140)
    box(d, (920, 230, 1370, 510), STEEL_DARK, 14)        # griddle plate
    for y in range(270, 500, 40):
        d.line((950, y, 1340, y), fill=(130, 134, 140), width=6)
    ring(d, 1330, 150, 60, IRON)                         # flue stub at the back right
    ring(d, 1330, 150, 30, (40, 40, 40))
    for x in range(200, 1400, 160):                      # control knobs along the south face
        ring(d, x, 605, 22, IRON, 8)
    box(d, (1446, 380, 1510, 470), ORANGE, 10)           # fuel line connector
    return im


def kettle():
    """Billy's kettle, 1 x 1: a big round double-walled cooking pot with a hinged lid on a square burner frame, a tap to the south."""
    im = canvas(1024, 1024)
    d = ImageDraw.Draw(im)
    box(d, (150, 150, 874, 874), OLIVE, 30)              # burner frame
    for cx, cy in ((190, 190), (834, 190), (190, 834), (834, 834)):
        ring(d, cx, cy, 34, IRON, 10)                    # frame legs at the corners
    ring(d, 512, 500, 310, STEEL_DARK)                   # outer wall
    ring(d, 512, 500, 265, STEEL)                        # lid
    ring(d, 512, 500, 60, STEEL_LIGHT, 10)               # lid boss
    d.rounded_rectangle((452, 470, 572, 530), radius=20, fill=IRON, outline=INK, width=10)  # lid handle
    d.rounded_rectangle((190, 470, 250, 530), radius=12, fill=IRON, outline=INK, width=10)  # hinge
    box(d, (470, 800, 554, 900), STEEL_DARK, 12)         # draw-off tap at the south
    ring(d, 512, 920, 26, ORANGE, 8)
    return im


def cold_box():
    """Billy's cold box, 1 x 1: an insulated pale chest with a lid, two latches and a compressor grille at one end."""
    im = canvas(1024, 1024)
    d = ImageDraw.Draw(im)
    box(d, (150, 230, 874, 820), PALE, 40)               # body
    box(d, (190, 270, 680, 760), (240, 242, 238), 28)    # lid
    for y in (360, 670):
        box(d, (150, y - 28, 200, y + 28), STEEL_DARK, 8)  # latches on the west face
    box(d, (700, 270, 834, 760), STEEL_DARK, 18)         # compressor end
    for y in range(310, 740, 44):
        d.line((725, y, 810, y), fill=(60, 64, 70), width=10)
    box(d, (380, 790, 490, 860), BLUE, 12)               # blue badge on the south face
    d.line((874, 700, 960, 760), fill=INK, width=16)     # power cable out
    return im


def water_heater():
    """The shower trailer's water heater, 1 x 1: an upright tank beside a burner box with a flue, pipes running south."""
    im = canvas(1024, 1024)
    d = ImageDraw.Draw(im)
    box(d, (130, 180, 894, 860), OLIVE, 30)              # skid
    ring(d, 380, 470, 220, PALE)                         # tank top
    ring(d, 380, 470, 70, STEEL, 10)                     # top fitting
    box(d, (640, 260, 850, 660), STEEL_DARK, 18)         # burner box
    ring(d, 745, 340, 50, IRON)                          # flue
    ring(d, 745, 340, 24, (40, 40, 40), 8)
    for y in range(440, 640, 40):
        d.line((670, y, 820, y), fill=(70, 74, 80), width=8)
    d.line((300, 690, 300, 900), fill=INK, width=34)     # pipes to the south
    d.line((300, 690, 300, 900), fill=RED, width=18)
    d.line((460, 690, 460, 900), fill=INK, width=34)
    d.line((460, 690, 460, 900), fill=BLUE, width=18)
    ring(d, 380, 470, 0, PALE, 1)
    return im


DRAWINGS = {
    'BillyRange': ('Billy field range, a 2 x 1 steel cooking cabinet with two burners and a griddle', field_range),
    'BillyKettle': ('Billy kettle, a big round lidded cooking pot on a square burner frame', kettle),
    'BillyColdBox': ('Billy cold box, an insulated chest with a compressor end', cold_box),
    'ShowerWaterHeater': ('the shower trailer\'s water heater, a tank beside a burner box', water_heater),
}


def write(name):
    what, draw = DRAWINGS[name]
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / f'{name}-large.png'
    draw().save(path, optimize=True)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    record_path(path.with_suffix('.record.md')).write_text(
        f'# {name}, large\n\nA layout reference of {what}, seen from directly above, drawn with Pillow by '
        f'`assets/phobos-vehicles/draw_trailer_standins.py` on {dt.date.today():%d %B %Y}. No image generator, game art or other '
        f'author\'s art was used. SHA-256 `{digest}`. It does not ship.\n', encoding='utf-8', newline='\n')
    print('wrote', path.name, digest[:12])


MODS = Path(__file__).resolve().parents[2] / 'mods' / 'PhobosVehicles' / 'Textures' / 'Phobos' / 'Vehicles'
TEXTURES = {
    'BillyRange': ('FieldKitchen/BillyRange', (512, 256)),
    'BillyKettle': ('FieldKitchen/BillyKettle', (256, 256)),
    'BillyColdBox': ('FieldKitchen/BillyColdBox', (256, 256)),
    'ShowerWaterHeater': ('ShowerTrailer/WaterHeater', (256, 256)),
}


def texture(name):
    """The drawing as a shipped stand-in texture: white made clear, at 256 px a cell, until the owner picks a generated picture."""
    what, draw = DRAWINGS[name]
    relative, size = TEXTURES[name]
    image = draw().convert('RGBA')
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            r, g, b, _ = pixels[x, y]
            if r > 245 and g > 245 and b > 245:
                pixels[x, y] = (255, 255, 255, 0)
    image = image.resize(size, Image.LANCZOS)
    path = MODS / (relative + '.png')
    path.parent.mkdir(parents=True, exist_ok=True)
    image.save(path, optimize=True)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    record_path(path.with_suffix('.record.md')).write_text(
        f'# {path.stem}\n\n**A stand-in**, drawn with Pillow by `assets/phobos-vehicles/draw_trailer_standins.py` on {dt.date.today():%d %B %Y}: '
        f'{what}, seen from directly above. No image generator, game art or other author\'s art was used. {size[0]} x {size[1]} px, '
        f'transparent background.\n\n**Outstanding:** generated art; candidates were made on the night of 17 Nov 2026 '
        f'(`assets/phobos-vehicles/requests/trailer-pieces.json`). This file is replaced when the owner picks one.\n\n'
        f'- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
    print('wrote', path.relative_to(MODS), digest[:12])


if __name__ == '__main__':
    arguments = sys.argv[1:]
    if arguments[:1] == ['--textures']:
        for chosen in arguments[1:] or TEXTURES:
            texture(chosen)
    else:
        for chosen in arguments or DRAWINGS:
            write(chosen)
