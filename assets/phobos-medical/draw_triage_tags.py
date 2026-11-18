"""Draw the triage tag icon and its outline with Pillow: a stand-in, drawn in code, no image generator and no game art.

    python assets/phobos-medical/draw_triage_tags.py

The icon is white so the game can tint it with the tag's colour (red immediate, yellow urgent, green delayed, white minor, black
expectant: the owner's choice, 7 Nov 2026); the outline is drawn over it in white, so a black tag reads on a dark map. A disc with the
medical cross cut out of it. Each texture gets a record beside it with its hash. Generated art can replace it on the owner's go.
"""
import datetime as dt
import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'mods/PhobosMedical/Common/Textures/Phobos/Triage/UI'
SIZE = 64
WORK = 512  # Drawn large and scaled down, which smooths the edges.
RING = 34   # Outline width at the working size.


def disc_with_cross():
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    margin = RING // 2 + 6
    draw.ellipse([margin, margin, WORK - margin, WORK - margin], fill=(255, 255, 255, 255))
    arm = WORK // 9
    centre = WORK // 2
    length = WORK // 3
    draw.rectangle([centre - arm, centre - length, centre + arm, centre + length], fill=(0, 0, 0, 0))
    draw.rectangle([centre - length, centre - arm, centre + length, centre + arm], fill=(0, 0, 0, 0))
    return image


def outline():
    image = Image.new('RGBA', (WORK, WORK), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    margin = RING // 2 + 6
    draw.ellipse([margin, margin, WORK - margin, WORK - margin], outline=(255, 255, 255, 255), width=RING)
    arm = WORK // 9
    centre = WORK // 2
    length = WORK // 3
    thin = RING // 3
    # The cross's edge, so the cut-out shape stays readable on a dark tag.
    points = [(centre - arm, centre - length), (centre + arm, centre - length), (centre + arm, centre - arm), (centre + length, centre - arm),
              (centre + length, centre + arm), (centre + arm, centre + arm), (centre + arm, centre + length), (centre - arm, centre + length),
              (centre - arm, centre + arm), (centre - length, centre + arm), (centre - length, centre - arm), (centre - arm, centre - arm)]
    draw.line(points + [points[0]], fill=(255, 255, 255, 255), width=thin, joint='curve')
    return image


def save(image, name, what):
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / f'{name}.png'
    image.resize((SIZE, SIZE), Image.LANCZOS).save(path)
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    (OUT / f'{name}.record.md').write_text(
        f'# {name}\n\n**A stand-in, drawn in code on {dt.date.today().isoformat()}** by `assets/phobos-medical/draw_triage_tags.py` '
        f'(Pillow): {what} No image generator, no reference images and no game art. Generated art can replace it on the owner\'s go.\n\n'
        f'- This file SHA-256: `{digest}`\n', encoding='utf-8')
    print(path.relative_to(ROOT), digest)


def outline_from_pick(icon_path):
    """The outline of a picked tag (18 Nov 2026: the owner picked generated art for the tag): the band just inside the edge of the
    shipped silhouette, around the disc and around the cross cut out of it, so the outline always matches the picture it sits on."""
    from PIL import ImageChops, ImageFilter
    alpha = Image.open(icon_path).convert('RGBA').split()[3].resize((WORK, WORK), Image.LANCZOS).point(lambda v: 255 if v >= 128 else 0)
    eroded = alpha
    for _ in range(RING // 2 // 2):  # MinFilter(5) eats two pixels a pass: about half the stand-in's ring, as the cross shares it.
        eroded = eroded.filter(ImageFilter.MinFilter(5))
    band = ImageChops.subtract(alpha, eroded)
    image = Image.new('RGBA', (WORK, WORK), (255, 255, 255, 0))
    image.putalpha(band)
    return image


if __name__ == '__main__':
    import sys
    if len(sys.argv) > 2 and sys.argv[1] == '--outline-from':
        # python assets/phobos-medical/draw_triage_tags.py --outline-from <shipped TriageTag.png>: only the outline, from the pick.
        path = OUT / 'TriageTagOutline.png'
        outline_from_pick(sys.argv[2]).resize((SIZE, SIZE), Image.LANCZOS).save(path)
        print(path.relative_to(ROOT), hashlib.sha256(path.read_bytes()).hexdigest())
    else:
        save(disc_with_cross(), 'TriageTag', 'a white disc with the medical cross cut out, tinted in game by the triage tag\'s colour.')
        save(outline(), 'TriageTagOutline', 'the white outline of the disc and the cross, drawn over the tinted tag so a black tag reads on a dark map.')
