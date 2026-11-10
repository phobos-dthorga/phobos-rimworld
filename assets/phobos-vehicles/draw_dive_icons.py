"""Draw the Seabed Crawler's upgrade icons, and the sonar ping button, with Pillow, in the game's look. No image generator and no game or other author's art is
involved; each has a record beside it, as the vehicle systems icons do.

    python assets/phobos-vehicles/draw_dive_icons.py

The Dive, Surface, Stay down and Settle buttons have generated pictures the owner picked (10 Nov 2026), so this no longer draws them.
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
CRAWLER = ROOT / 'mods/PhobosVehicles/Textures/Phobos/Vehicles/SeabedCrawler/Upgrades'
SYSTEMS = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
RUBBER = (56, 58, 62, 255)
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
HULL = (92, 100, 110, 255)
WATER = (84, 140, 190, 255)
ORANGE = (214, 124, 44, 255)
LINE = 18


def wheel(draw, x, y, radius):
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=RUBBER, outline=OUTLINE, width=LINE)
    draw.ellipse((x - radius // 3, y - radius // 3, x + radius // 3, y + radius // 3), fill=ORANGE, outline=OUTLINE, width=LINE // 2)


def bottoming_wheels(draw):
    draw.rounded_rectangle((90, 110, 422, 170), radius=16, fill=HULL, outline=OUTLINE, width=LINE)
    for x in (150, 362):
        draw.rectangle((x - 22, 160, x + 22, 300), fill=HULL, outline=OUTLINE, width=LINE)
        wheel(draw, x, 350, 92)


def scrubber(draw):
    draw.rounded_rectangle((60, 70, 452, 442), radius=26, fill=HULL, outline=OUTLINE, width=LINE)
    for x in (146, 256, 366):
        draw.rounded_rectangle((x - 40, 120, x + 40, 392), radius=26, fill=(214, 216, 212, 255), outline=OUTLINE, width=LINE)
        draw.rectangle((x - 31, 220, x + 31, 276), fill=WATER)


GLASS = (214, 216, 212, 255)


def laboratory(draw):
    # A conical flask of sea water on a grey bench, with a test tube beside it.
    draw.rounded_rectangle((50, 392, 462, 446), radius=12, fill=HULL, outline=OUTLINE, width=LINE)
    flask = [(206, 92), (290, 92), (290, 196), (392, 392), (104, 392), (206, 196)]
    draw.polygon(flask, fill=GLASS, outline=OUTLINE, width=LINE)
    draw.polygon([(160, 290), (336, 290), (380, 380), (116, 380)], fill=WATER)
    draw.rectangle((190, 74, 306, 104), fill=GLASS, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((398, 170, 446, 392), radius=22, fill=GLASS, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((407, 300, 437, 383), radius=14, fill=ORANGE)


def advanced_laboratory(draw):
    # An analyser: a grey cabinet with a dark screen showing a blue trace, and a sample tray of orange tubes.
    draw.rounded_rectangle((70, 90, 442, 420), radius=24, fill=HULL, outline=OUTLINE, width=LINE)
    draw.rectangle((112, 128, 400, 268), fill=(30, 36, 44, 255), outline=OUTLINE, width=LINE // 2)
    draw.line([(130, 230), (180, 230), (210, 160), (250, 250), (290, 190), (330, 230), (384, 230)], fill=WATER, width=LINE)
    draw.rectangle((112, 304, 400, 386), fill=GLASS, outline=OUTLINE, width=LINE // 2)
    for x in (152, 210, 268, 326):
        draw.rounded_rectangle((x - 16, 290, x + 16, 372), radius=14, fill=ORANGE, outline=OUTLINE, width=LINE // 2)


def hydrophones(draw):
    # A listener's headphones under the blue rings of a sound heard through the water.
    for radius in (60, 115, 170):
        draw.arc((256 - radius, 200 - radius, 256 + radius, 200 + radius), start=210, end=330, fill=WATER, width=LINE)
    draw.arc((96, 196, 416, 516), start=180, end=360, fill=OUTLINE, width=LINE * 2)
    draw.arc((106, 206, 406, 506), start=180, end=360, fill=HULL, width=LINE)
    for x in (110, 402):
        draw.rounded_rectangle((x - 50, 300, x + 50, 450), radius=30, fill=HULL, outline=OUTLINE, width=LINE)
        draw.rounded_rectangle((x - 24, 330, x + 24, 420), radius=16, fill=ORANGE)


def sonar(draw):
    # A sonar transducer: a pale dome on a grey base, sending out three orange rings.
    for radius in (90, 150, 210):
        draw.arc((256 - radius, 330 - radius, 256 + radius, 330 + radius), start=215, end=325, fill=ORANGE, width=LINE)
    draw.chord((136, 250, 376, 470), start=180, end=360, fill=GLASS, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((110, 350, 402, 420), radius=14, fill=HULL, outline=OUTLINE, width=LINE)


def sonar_ping(draw):
    # The ping button: a dark hull seen from above at the centre of three widening blue rings, the outermost broken.
    for radius, start, end in ((110, 0, 360), (170, 0, 360), (230, 20, 340)):
        draw.arc((256 - radius, 256 - radius, 256 + radius, 256 + radius), start=start, end=end, fill=WATER, width=LINE)
    draw.ellipse((206, 150, 306, 362), fill=HULL, outline=OUTLINE, width=LINE)
    draw.rounded_rectangle((232, 220, 280, 268), radius=8, fill=ORANGE, outline=OUTLINE, width=LINE // 2)


def hull_section(draw):
    # The laboratory refit: a ring of hull plating seen at an angle, its open end showing a conical flask inside.
    draw.rounded_rectangle((70, 130, 360, 400), radius=40, fill=HULL, outline=OUTLINE, width=LINE)
    for x in (150, 230, 300):
        draw.line([(x, 140), (x, 390)], fill=OUTLINE, width=LINE // 2)
    draw.ellipse((290, 120, 450, 410), fill=(40, 44, 50, 255), outline=OUTLINE, width=LINE)
    draw.polygon([(352, 200), (388, 200), (388, 250), (420, 350), (320, 350), (352, 250)], fill=GLASS, outline=OUTLINE, width=LINE // 2)
    draw.polygon([(334, 310), (406, 310), (418, 344), (322, 344)], fill=WATER)


def sensor_sail(draw):
    # The sensor refit: a smooth faired sail seen from the side, a short mast on top sending out orange rings.
    for radius in (60, 110):
        draw.arc((300 - radius, 120 - radius, 300 + radius, 120 + radius), start=200, end=340, fill=ORANGE, width=LINE)
    draw.rectangle((288, 120, 312, 230), fill=HULL, outline=OUTLINE, width=LINE // 2)
    draw.polygon([(120, 440), (170, 230), (380, 230), (430, 440)], fill=HULL, outline=OUTLINE, width=LINE)
    draw.rectangle((150, 330, 400, 360), fill=ORANGE)
    draw.rounded_rectangle((40, 430, 472, 470), radius=14, fill=(40, 44, 50, 255), outline=OUTLINE, width=LINE)


ICONS = [
    ('BottomingWheels', bottoming_wheels, 'two rubber-tyred wheels on struts from a bar', CRAWLER),
    ('Scrubber', scrubber, 'three pale canisters with blue bands in a grey rack', CRAWLER),
    ('Laboratory', laboratory, 'a conical flask of blue water and an orange test tube on a grey bench', CRAWLER),
    ('AdvancedLaboratory', advanced_laboratory, 'a grey analyser cabinet with a blue trace on a dark screen and a tray of orange sample tubes', CRAWLER),
    ('Hydrophones', hydrophones, 'grey headphones with orange ear pads under three blue rings of sound', CRAWLER),
    ('Sonar', sonar, 'a pale sonar dome on a grey base sending out three orange rings', CRAWLER),
    ('LaboratoryRefit', hull_section, 'a ring of grey hull plating with a conical flask of blue water inside its open end', CRAWLER),
    ('SensorRefit', sensor_sail, 'a smooth grey sail with an orange band and a short mast sending out orange rings', CRAWLER),
    ('SonarPing', sonar_ping, 'a grey hull seen from above with an orange hatch, at the centre of three widening blue rings', SYSTEMS),
]


def main():
    for name, drawing, what, folder in ICONS:
        folder.mkdir(parents=True, exist_ok=True)
        image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        drawing(ImageDraw.Draw(image))
        path = folder / f'{name}.png'
        image.resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {path.stem}\n\n**Drawn in code** by `assets/phobos-vehicles/draw_dive_icons.py` (Pillow): {what}. No image generator, no reference '
            f'images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.name)


if __name__ == '__main__':
    main()
