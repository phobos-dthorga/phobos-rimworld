"""Draw the four signal-level icons by script: four bars rising left to right, the lit ones filled, one matched set.

    python scripts/art/draw_signal_bars.py                 draw both styles to the art working folder and draw their card
    python scripts/art/draw_signal_bars.py --offer         and offer the two styles on D'thorga Companion's Picks page
    python scripts/art/draw_signal_bars.py --style level --ship FOLDER   write the picked style's four icons into FOLDER

Why by script (owner, 24 Nov 2026: "draw the signal levels by script"): night 13's generated tries could not hold the four as a set
(equal bars in one seed, rising in the other, stray extra bars in a third), and a set must differ only in what is lit. Drawing them
gives the same bars every time, and a change of colour or count is one line here.

The look follows the game's icons as recorded in the art pipeline: a thick near-black outline, flat cel shading (a lit band on the
left, a shade band on the right, hard edges), rounded corners and a muted palette. Each icon is drawn at eight times its size and
shrunk, so its edges are smooth with no stray pixels. Levels follow the words the game shows today (src/PhobosRadio/UI/LinkText.cs:
clean [||||], workable [||| ], marginal [||  ], none [    ]), so the icon and its tooltip agree.

Two styles for the owner to pick from (agent's choice of what to offer): "level" colours the lit bars by level (orange, yellow-green,
green), "single" lights every bar the same green, as a phone does. No contact is four unlit bars under a red slash in both.
Game-free: a next game passes its own size, outline and colours.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402

SIZE = 128          # as shipped, pixels
SCALE = 8           # drawn at this many times, then shrunk
OUTLINE = (24, 24, 26, 255)
OUTLINE_PX = 5      # at shipped size
RADIUS_PX = 5
UNLIT = (82, 84, 88)
SLASH = (176, 52, 40)

# Bars at shipped size: left edge, top; all share the bottom and the width.
BAR_WIDTH = 24     # widened from 21 after the first card: thin at the 75 px a button draws
BAR_BOTTOM = 116
BARS = [(7, 86), (37, 64), (67, 40), (97, 14)]

# (name, bars lit, colour of the lit bars in each style)
LEVELS = [
    ('None', 0, {'level': None, 'single': None}),
    ('Marginal', 2, {'level': (204, 124, 58), 'single': (112, 164, 84)}),
    ('Workable', 3, {'level': (184, 180, 86), 'single': (112, 164, 84)}),
    ('Clean', 4, {'level': (112, 164, 84), 'single': (112, 164, 84)}),
]
STYLES = ('level', 'single')


def shade(colour, amount):
    return tuple(max(0, min(255, c + amount)) for c in colour[:3]) + (255,)


def bar(d: ImageDraw.ImageDraw, x, top, fill):
    """One bar: the outline as a rounded block, then the fill inset by the outline, with a lit band left and a shade band right."""
    s = SCALE
    box = (x * s, top * s, (x + BAR_WIDTH) * s, BAR_BOTTOM * s)
    d.rounded_rectangle(box, radius=RADIUS_PX * s, fill=OUTLINE)
    o = OUTLINE_PX * s
    inner = (box[0] + o, box[1] + o, box[2] - o, box[3] - o)
    r = max(1, (RADIUS_PX - OUTLINE_PX // 2) * s)
    d.rounded_rectangle(inner, radius=r, fill=shade(fill, 0))
    width = inner[2] - inner[0]
    # Cel bands: hard-edged, kept inside the rounded fill by insetting them from its corners.
    d.rectangle((inner[0], inner[1] + r, inner[0] + int(width * 0.28), inner[3] - r), fill=shade(fill, 34))
    d.rectangle((inner[2] - int(width * 0.22), inner[1] + r, inner[2], inner[3] - r), fill=shade(fill, -26))


def slash(d: ImageDraw.ImageDraw):
    """No contact: a red stroke with the same outline, corner to corner across the bars."""
    s = SCALE
    start, end = (14 * s, 112 * s), (114 * s, 22 * s)
    for width, colour in ((15, OUTLINE), (7, SLASH + (255,))):
        d.line((start, end), fill=colour, width=width * s)
        for x, y in (start, end):
            rad = width * s // 2
            d.ellipse((x - rad, y - rad, x + rad, y + rad), fill=colour)


def draw(lit: int, colour) -> Image.Image:
    big = Image.new('RGBA', (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))
    d = ImageDraw.Draw(big)
    for n, (x, top) in enumerate(BARS):
        bar(d, x, top, colour if n < lit and colour else UNLIT)
    if lit == 0:
        slash(d)
    return big


def draw_style(style: str, folder: Path) -> list[Path]:
    """The four icons of one style: masters at eight times, and the shipped size beside them."""
    folder.mkdir(parents=True, exist_ok=True)
    out = []
    for name, lit, colours in LEVELS:
        big = draw(lit, colours[style])
        big.save(folder / f'Signal{name}-master.png')
        path = folder / f'Signal{name}.png'
        big.resize((SIZE, SIZE), Image.LANCZOS).save(path)
        out.append(path)
    return out


def strip(paths: list[Path], gap: int = 16) -> Image.Image:
    pics = [Image.open(p).convert('RGBA') for p in paths]
    s = Image.new('RGBA', (sum(p.width for p in pics) + gap * (len(pics) - 1), max(p.height for p in pics)), (0, 0, 0, 0))
    x = 0
    for p in pics:
        s.alpha_composite(p, (x, 0))
        x += p.width + gap
    return s


def card(sets: dict[str, list[Path]], out: Path) -> None:
    """Each style as a row: three times without smoothing on soil and on stone, then as shipped and at the 75 px a button draws."""
    import review_cards  # noqa: PLC0415  (its grounds and font, so every card looks alike)

    f = review_cards.font(26)
    rows = []
    for style, paths in sets.items():
        s = strip(paths)
        big = s.resize((s.width * 3, s.height * 3), Image.NEAREST)
        button = strip(paths, gap=10)
        button = button.resize((button.width * 75 // SIZE, 75), Image.LANCZOS)
        row = Image.new('RGBA', (big.width, 40 + big.height * 2 + 20 + s.height + 20 + 75), review_cards.BACK)
        ImageDraw.Draw(row).text((0, 0), f'{style}: none, marginal, workable, clean', fill=review_cards.TEXT, font=f)
        y = 40
        for ground in (review_cards.SOIL, review_cards.STONE):
            row.alpha_composite(review_cards.on(ground, big), (0, y))
            y += big.height
        y += 20
        row.alpha_composite(review_cards.on(review_cards.SOIL, s), (0, y))
        row.alpha_composite(review_cards.on(review_cards.STONE, s), (s.width + 30, y))
        y += s.height + 20
        row.alpha_composite(review_cards.on(review_cards.STONE, button), (0, y))
        rows.append(row)
    c = Image.new('RGBA', (max(r.width for r in rows) + 80, sum(r.height + 40 for r in rows) + 70), review_cards.BACK)
    ImageDraw.Draw(c).text((40, 14), 'Signal levels drawn by script: x3 above, then actual size, then at button size',
                           fill=review_cards.TEXT, font=f)
    y = 70
    for r in rows:
        c.alpha_composite(r, (40, y))
        y += r.height + 40
    out.parent.mkdir(parents=True, exist_ok=True)
    c.convert('RGB').save(out)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--style', choices=STYLES, help='one style only')
    parser.add_argument('--ship', help="write the style's four icons into this folder (needs --style)")
    parser.add_argument('--offer', action='store_true', help="offer the styles on the Companion's Picks page")
    args = parser.parse_args(argv)
    if args.ship:
        if not args.style:
            parser.error('--ship needs --style')
        for p in draw_style(args.style, art_paths.work('signal-bars', args.style)):
            target = Path(args.ship) / p.name
            target.write_bytes(p.read_bytes())
            print(target)
        return 0
    folder = art_paths.work('signal-bars')
    styles = [args.style] if args.style else list(STYLES)
    sets = {style: draw_style(style, folder / style) for style in styles}
    out = art_paths.work('cards', 'signal-bars', 'signal-bars.png')
    card(sets, out)
    print(out)
    if args.offer:
        import art_picks  # noqa: PLC0415
        import edge_check  # noqa: PLC0415

        tries = []
        for style, paths in sets.items():
            preview = folder / style / 'set.png'
            strip(paths).save(preview)
            specks = sum(edge_check.measure(p)['specks'] for p in paths)
            tries.append((style, f'{style.capitalize()} colours', preview, f'{specks} loose specks across the four'))
        art_picks.offer('signal-bars', 'Signal levels (drawn by script)', 'picture', tries,
                        in_use='mods/PhobosRadioFramework/Common/Textures/Phobos/Radio/UI/Signal*.png',
                        context='One set of four for the Radio button: none, marginal, workable, clean. Replaces night 13 tries.',
                        source='scripts/art/draw_signal_bars.py')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
