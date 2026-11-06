"""Make a mod's Workshop preview from a generated background: the house layout, with the name added by script, never generated.

    python scripts/art/make_preview.py BACKGROUND "Medical Equipment" "Oxygen and burn care for your hospital" OUT.png

The background is cropped to 16:9 from its centre and scaled to 640 x 360, Steam's usual preview size. The lower third is darkened
with a gradient so the text reads on any picture; "D'thorga" sits small above the mod's name, and an optional line below it. The
fonts are Windows' Arial, rendered to pixels (agent default for the house layout, for the owner to change). Needs Pillow.
"""
import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

SIZE = (640, 360)
HOUSE = "D'thorga"
INK = (238, 236, 230, 255)
ACCENT = (214, 170, 92, 255)  # Brass, the house colour on the water tap and the hyperbaric door.
SUB_INK = (186, 196, 204, 255)
SHADE_FROM = 0.52  # Where the darkening starts, as a fraction of the height.
SHADE_MAX = 225


def font(names, size):
    for name in names:
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def cover(image, size=SIZE):
    """Crop to the target's shape from the centre, then scale: nothing is stretched."""
    width, height = image.size
    target = size[0] / size[1]
    if width / height > target:
        new_width = round(height * target)
        left = (width - new_width) // 2
        image = image.crop((left, 0, left + new_width, height))
    else:
        new_height = round(width / target)
        top = (height - new_height) // 2
        image = image.crop((0, top, width, top + new_height))
    return image.resize(size, Image.LANCZOS)


def shade(image):
    """Darken the lower part, from nothing at SHADE_FROM to SHADE_MAX at the bottom edge."""
    width, height = image.size
    overlay = Image.new('RGBA', image.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(overlay)
    start = int(height * SHADE_FROM)
    for y in range(start, height):
        draw.line((0, y, width, y), fill=(10, 12, 14, int(SHADE_MAX * (y - start) / max(1, height - start))))
    return Image.alpha_composite(image, overlay)


def make_preview(background, title, subtitle=''):
    image = shade(cover(background.convert('RGBA')))
    draw = ImageDraw.Draw(image)
    bold = ['arialbd.ttf', 'DejaVuSans-Bold.ttf']
    plain = ['arial.ttf', 'DejaVuSans.ttf']
    left, bottom = 28, SIZE[1] - 26
    if subtitle:
        draw.text((left, bottom), subtitle, font=font(plain, 20), fill=SUB_INK, anchor='ls')
        bottom -= 34
    draw.text((left, bottom), title, font=font(bold, 42), fill=INK, anchor='ls')
    draw.text((left + 2, bottom - 50), HOUSE, font=font(bold, 20), fill=ACCENT, anchor='ls')
    return image.convert('RGB')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('background')
    parser.add_argument('title')
    parser.add_argument('subtitle', nargs='?', default='')
    parser.add_argument('out')
    args = parser.parse_args(argv)
    preview = make_preview(Image.open(args.background), args.title, args.subtitle)
    Path(args.out).parent.mkdir(parents=True, exist_ok=True)
    preview.save(args.out, optimize=True)
    print(f'wrote {args.out}')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
