"""Draw the liquid oxygen store's frost from the store's own picture, in code (Pillow), and write its record. Free, reproducible.

    python assets/phobos-medical/draw_frost.py

The owner asked for the store to frost while it works, after DMS2's frosting Pressure Chamber (27 Oct and 12 Nov 2026). DMS2 does it with
a frost picture under the game's Mote shader, faded in and out; we do the same with our own picture, drawn here, and D'thorga Framework's
FadingOverlay. Where the frost goes follows the real thing: on a liquid oxygen store the ambient-air vaporiser fins and the pipework ice
up as the cold gas warms through them, while the vacuum-insulated tank stays mostly clear apart from a little rime at its foot (common
engineering knowledge, no figure claimed). The regions are boxes read off the shipped store picture (agent placement).
"""
import hashlib
import random
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

STORE = Path('mods/PhobosMedical/Common/Textures/Phobos/Medical/Things/LiquidOxygenStore.png')
OUT = STORE.with_name('LiquidOxygenStore_Frost.png')
SEED = 12112026
ICE = (232, 245, 255)
# (left, top, right, bottom, strength): boxes in the 256 px store picture.
REGIONS = [
    (122, 108, 210, 236, 1.0),  # the finned ambient vaporiser
    (60, 170, 112, 214, 0.85),  # the pipework and gauges at the tank's foot
    (40, 180, 160, 230, 0.35),  # a little rime round the tank's base
]


def weight_map(size):
    """Strength of frost per pixel, the strongest region winning, softened at the edges."""
    weights = Image.new('L', size, 0)
    draw = ImageDraw.Draw(weights)
    for left, top, right, bottom, strength in sorted(REGIONS, key=lambda r: r[4]):
        draw.rectangle((left, top, right, bottom), fill=int(255 * strength))
    return weights.filter(ImageFilter.GaussianBlur(6))


def main():
    store = Image.open(STORE).convert('RGBA')
    size = store.size
    rng = random.Random(SEED)
    silhouette = store.getchannel('A').point(lambda a: 255 if a > 128 else 0)
    # Rime along every inner edge of the drawn shapes: the outline and the fins' own lines, where the metal is thinnest and coldest.
    inner = silhouette.filter(ImageFilter.MinFilter(7))
    edge_band = ImageChops.subtract(silhouette, inner)
    lines = store.convert('L').point(lambda v: 255 if v < 90 else 0)  # dark outlines inside the picture: fin edges, pipe outlines
    lines = ImageChops.multiply(lines, silhouette).filter(ImageFilter.MaxFilter(3))

    frost = Image.new('L', size, 0)
    draw = ImageDraw.Draw(frost)
    frost.paste(edge_band, (0, 0), edge_band)
    frost = ImageChops.lighter(frost, lines.point(lambda v: v * 0.8))
    draw = ImageDraw.Draw(frost)
    # Crystal fingers growing inward from the edges, short and branching, as hoar frost does.
    seeds = [(x, y) for y in range(size[1]) for x in range(size[0]) if edge_band.getpixel((x, y)) and rng.random() < 0.06]
    for x, y in seeds:
        for _ in range(rng.randint(1, 3)):
            length = rng.randint(3, 9)
            dx, dy = rng.uniform(-1, 1), rng.uniform(-1, 1)
            px, py = x, y
            for _ in range(length):
                nx, ny = px + dx * 1.6, py + dy * 1.6
                if not (0 <= nx < size[0] and 0 <= ny < size[1]) or not silhouette.getpixel((int(nx), int(ny))):
                    break
                draw.line((px, py, nx, ny), fill=230, width=1)
                px, py = nx, ny
                dx += rng.uniform(-0.5, 0.5)
                dy += rng.uniform(-0.5, 0.5)
    # A sugaring of ice crystals over the frosted parts.
    for _ in range(2600):
        x, y = rng.randrange(size[0]), rng.randrange(size[1])
        if silhouette.getpixel((x, y)):
            draw.point((x, y), fill=rng.randint(150, 255))
    frost = frost.filter(ImageFilter.GaussianBlur(0.6))
    alpha = ImageChops.multiply(ImageChops.multiply(frost, weight_map(size)), silhouette)
    alpha = alpha.point(lambda v: min(225, int(v * 1.15)))
    picture = Image.new('RGBA', size, ICE + (0,))
    picture.putalpha(alpha)
    picture.save(OUT)

    digest = hashlib.sha256(OUT.read_bytes()).hexdigest()
    store_digest = hashlib.sha256(STORE.read_bytes()).hexdigest()
    OUT.with_suffix('.record.md').write_text(
        "# LiquidOxygenStore_Frost\n\n"
        "**Drawn in code** by `assets/phobos-medical/draw_frost.py` (Pillow, seed 12112026) on 12 November 2026, from the outline of the "
        "shipped store picture, so the two register exactly. No image generator, no reference images and no game or other author's art: "
        "the idea follows DMS2's frosting Pressure Chamber (a frost picture under the game's Mote shader), and none of its files were copied.\n\n"
        "Heaviest on the ambient-air vaporiser fins and the pipework, a little rime at the tank's foot, the vacuum-insulated tank itself "
        "clear: where a real liquid oxygen store ices up. Shown and faded by D'thorga Framework's FadingOverlay while the store holds liquid "
        "and its cooler runs.\n\n"
        f"- Drawn from `LiquidOxygenStore.png`, SHA-256 `{store_digest}`\n"
        f"- `LiquidOxygenStore_Frost.png`: {size[0]} x {size[1]} px, SHA-256 `{digest}`\n", encoding='utf-8')
    print('wrote', OUT)


if __name__ == '__main__':
    main()
