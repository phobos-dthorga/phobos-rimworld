"""Draw the batch 3 signatures' buttons (the generator set's service lead, the tipper's loading grab) with Pillow, in the look of the hitch's
buttons. Each is also its upgrade node's picture. No image generator and no game or other author's art is involved; each has a record beside
it (D'thorga Framework 0.101.0).

    python assets/phobos-vehicles/draw_signature_icons.py
"""
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
FOLDER = ROOT / 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems'
WORK = 512
FINAL = 128
OUTLINE = (24, 24, 26, 255)
STEEL = (150, 156, 162, 255)
DARK = (92, 100, 110, 255)
ORANGE = (214, 124, 44, 255)
COPPER = (196, 120, 70, 255)
RED = (190, 60, 50, 255)
LINE = 18


def service_lead(draw):
    # A cable reel on the left, the heavy lead looping across, and a battery with a clamp on its terminal on the right.
    draw.ellipse((30, 150, 230, 350), fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.ellipse((95, 215, 165, 285), fill=DARK, outline=OUTLINE, width=LINE)
    draw.line([(200, 300), (260, 400), (330, 360), (360, 250)], fill=OUTLINE, width=46, joint='curve')
    draw.line([(200, 300), (260, 400), (330, 360), (360, 250)], fill=DARK, width=22, joint='curve')
    draw.rounded_rectangle((300, 260, 490, 450), radius=16, fill=STEEL, outline=OUTLINE, width=LINE)
    draw.rectangle((330, 225, 380, 262), fill=RED, outline=OUTLINE, width=LINE // 2)
    draw.rectangle((420, 225, 470, 262), fill=DARK, outline=OUTLINE, width=LINE // 2)
    # The clamp's jaws on the red terminal.
    draw.polygon([(320, 150), (400, 150), (380, 235), (340, 235)], fill=COPPER, outline=OUTLINE, width=LINE // 2)
    # A bolt sign on the battery: it charges.
    draw.polygon([(410, 290), (360, 370), (395, 370), (370, 430), (440, 340), (402, 340), (430, 290)], fill=ORANGE, outline=OUTLINE, width=LINE // 2)


def grab(draw):
    # A short crane on a truck deck: post, boom, a hanging grab with two jaws over a chunk.
    draw.rounded_rectangle((20, 400, 300, 470), radius=14, fill=DARK, outline=OUTLINE, width=LINE)
    draw.rectangle((90, 190, 150, 405), fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.polygon([(100, 170), (430, 110), (440, 160), (130, 235)], fill=ORANGE, outline=OUTLINE, width=LINE)
    draw.line([(410, 150), (410, 285)], fill=OUTLINE, width=14)
    draw.polygon([(380, 280), (440, 280), (470, 370), (430, 360), (410, 310), (390, 360), (350, 370)], fill=STEEL, outline=OUTLINE, width=LINE // 2)
    draw.polygon([(360, 395), (455, 380), (490, 450), (400, 480), (340, 450)], fill=(128, 116, 104, 255), outline=OUTLINE, width=LINE)


ICONS = [
    ('ServiceLead', service_lead, 'an orange cable reel with a dark lead running to a clamp on the red terminal of a grey battery marked with an orange bolt'),
    ('Stations/Grab', grab, 'a short orange crane on a dark truck deck, its grey two-jawed grab hanging over a brown rock chunk'),
]


def main():
    for name, drawing, what in ICONS:
        path = FOLDER / f'{name}.png'
        path.parent.mkdir(parents=True, exist_ok=True)
        image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
        drawing(ImageDraw.Draw(image))
        image.resize((FINAL, FINAL), Image.LANCZOS).save(path)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        path.with_suffix('.record.md').write_text(
            f'# {path.stem}\n\n**Drawn in code** by `assets/phobos-vehicles/draw_signature_icons.py` (Pillow): {what}. No image generator, no '
            f'reference images and no game or other author\'s art.\n\n- `{path.name}`: SHA-256 `{digest}`\n', encoding='utf-8', newline='\n')
        print(path.relative_to(ROOT))


if __name__ == '__main__':
    main()
