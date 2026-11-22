"""Draw the owner's review cards from the real export: one card per picture to pick, every attempt side by side.

    python scripts/art/review_cards.py sprites REQUEST.json [--only NAME] [--size 128] [--aspect 1:1]
    python scripts/art/review_cards.py overlays --vehicle FOLDER/PREFIX --candidate LABEL=PATH_north.png [--candidate ...]

**sprites**: every attempt of each request in a request file (an item, a kit, a building, an icon), each exported exactly as it would ship:
the background AI looks at the original first (on the processor, beside the game), then `export_sprite.py` cuts it out. Each attempt is
shown enlarged three times without smoothing and at actual size, on soil and on dark stone, with the edge check's figures under it.
**overlays**: candidate pictures drawn on one of our vehicles (already exported, `..._north.png`): the vehicle as the game draws it, tinted
in its own colours and in a repainted set, whole at half size and the gear enlarged at game pixels without smoothing.

Learnt with the owner on 22 Nov 2026: a card drawn from a plain white cut-off showed a white fringe the export does not have, and cards
looked at only smoothed hid ragged, "scrubbed out" edges and stray crumbs. So a card is always the real export, at game pixels, in more than
one setting. Cards go to the art tools' working folder (`art_paths.work('cards', ...)`), one file per picture, named in order; the tool
prints their paths for the reply. Game-free apart from the vehicle tinting (RimWorld's paint mask, `..._northm.png`, red the main colour,
green the second); a next game gives its own grounds and tinting.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import art_paths  # noqa: E402
import art_picks  # noqa: E402
import edge_check  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SOIL = (92, 76, 58, 255)
STONE = (48, 48, 52, 255)
BACK = (30, 30, 32, 255)
TEXT = (235, 235, 235, 255)
EXPORT = ROOT / 'assets' / 'phobos-vehicles' / 'generated' / 'export'
VEHICLES = ROOT / 'mods' / 'PhobosVehicles' / 'Textures' / 'Phobos' / 'Vehicles'


def font(size=24):
    try:
        return ImageFont.truetype('arial.ttf', size)
    except OSError:
        return ImageFont.load_default()


def on(ground, picture: Image.Image) -> Image.Image:
    g = Image.new('RGBA', picture.size, ground)
    g.alpha_composite(picture)
    return g


# ---- Sprites ----

def attempts(request_file: Path, entry: dict) -> list[Path]:
    """The request's own tries, named by seed value: a correction request's seeds [3, 4] are tries 3 and 4, never 1 and 2."""
    folder = request_file.parent.parent / 'generated' / entry['name']
    return [p for seed in entry.get('seeds', [1]) if (p := folder / f"{entry['name']}-{seed}.png").is_file()]


def export(original: Path, out: Path, size: int, aspect: str) -> Path:
    out.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(original), str(out), '--size', str(size),
                    '--aspect', aspect], check=True, stdout=subprocess.DEVNULL)
    return out


def edge_note(path: Path) -> str:
    """The edge check's figures for one export, in words: on its card, and as its try's note on the Companion's Picks page."""
    m = edge_check.measure(path)
    return f"{m['specks']} loose specks" + (' (flagged)' if edge_check.flagged(m) else '') + (', haze worth a look' if edge_check.hazy(m) else '')


def sprite_card(title: str, exports: list[tuple[str, Path]], out: Path) -> None:
    f, small = font(26), font(18)
    columns = []
    for label, path in exports:
        pic = Image.open(path).convert('RGBA')
        big = pic.resize((pic.width * 3, pic.height * 3), Image.NEAREST)
        note = edge_note(path)
        width = big.width * 2 + 30
        col = Image.new('RGBA', (width, big.height + pic.height + 90), BACK)
        d = ImageDraw.Draw(col)
        d.text((0, 0), label, fill=TEXT, font=f)
        col.alpha_composite(on(SOIL, big), (0, 36))
        col.alpha_composite(on(STONE, big), (big.width + 30, 36))
        col.alpha_composite(on(SOIL, pic), (0, 46 + big.height))
        col.alpha_composite(on(STONE, pic), (big.width + 30, 46 + big.height))
        d.text((0, 56 + big.height + pic.height), note, fill=TEXT, font=small)
        columns.append(col)
    width = sum(c.width for c in columns) + 40 * (len(columns) + 1)
    card = Image.new('RGBA', (width, max(c.height for c in columns) + 70), BACK)
    ImageDraw.Draw(card).text((40, 14), title, fill=TEXT, font=f)
    x = 40
    for c in columns:
        card.alpha_composite(c, (x, 60))
        x += c.width + 40
    out.parent.mkdir(parents=True, exist_ok=True)
    card.convert('RGB').save(out)


def sprites(args) -> int:
    request_file = Path(args.request).resolve()
    entries = json.loads(request_file.read_text(encoding='utf-8'))
    entries = [e for e in entries if not args.only or e['name'] == args.only]
    originals = [p for e in entries for p in attempts(request_file, e)]
    if not originals:
        print('no attempts made yet for', request_file.name)
        return 1
    # The background AI looks at each original first, on the processor: the exporter reads its masks.
    cmd = [sys.executable, str(ROOT / 'scripts' / 'art' / 'clean_backgrounds.py'), 'masks', '--cpu', '--stop-at', '23:59']
    for p in originals:
        cmd += ['--also', str(p)]
    subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)
    folder = art_paths.work('cards', request_file.stem)
    for n, entry in enumerate(entries, 1):
        made = attempts(request_file, entry)
        if not made:
            continue
        exports = [(p.stem.rsplit('-', 1)[-1], export(p, folder / 'export' / p.name, args.size, args.aspect)) for p in made]
        out = folder / f"{n:02d}-{entry['name']}.png"
        sprite_card(f"{entry['name']}: {' or '.join(l for l, _ in exports)} (x3 above, actual size below; soil and stone)", exports, out)
        print(out)
        if not args.no_offer:
            # The same tries, offered on the Companion's Picks page (the owner's choice of 23 Nov 2026): the exports, as shipped.
            art_picks.offer(entry['name'], entry.get('title') or entry['name'], 'picture',
                            [(label, f'Try {label}', path, edge_note(path)) for label, path in exports],
                            in_use=entry.get('target'), source=request_file.relative_to(ROOT).as_posix()
                            if request_file.is_relative_to(ROOT) else str(request_file))
    return 0


# ---- Overlays on our vehicles ----

def tinted(folder: str, prefix: str, colours: tuple | None) -> Image.Image:
    body = Image.open(VEHICLES / folder / f'{prefix}_north.png').convert('RGBA')
    mask_path = VEHICLES / folder / f'{prefix}_northm.png'
    if not mask_path.is_file():
        return body
    if colours is None:
        transform = EXPORT / f'{prefix}.transform.json'
        saved = json.loads(transform.read_text(encoding='utf-8'))['colours'] if transform.is_file() else {}
        main = saved.get('color') or [200, 200, 200]
        colours = (main, saved.get('colorTwo') or main)
    main, second = colours
    mask = Image.open(mask_path).convert('RGBA')
    px, mp = body.load(), mask.load()
    out = body.copy()
    op = out.load()
    for y in range(body.height):
        for x in range(body.width):
            r, g, b, a = px[x, y]
            if not a:
                continue
            fr, fg = mp[x, y][0] / 255, mp[x, y][1] / 255
            k = [(1 - fr - fg) + fr * main[i] / 255 + fg * second[i] / 255 for i in range(3)]
            op[x, y] = (int(r * k[0]), int(g * k[1]), int(b * k[2]), a)
    return out


def overlays(args) -> int:
    folder, prefix = args.vehicle.split('/')
    bodies = [('own colours', tinted(folder, prefix, None)), ('repainted', tinted(folder, prefix, ((70, 110, 140), (200, 60, 60))))]
    f = font(24)
    out_folder = art_paths.work('cards', f'overlays-{prefix}')
    for n, spec in enumerate(args.candidate, 1):
        label, path = spec.split('=', 1)
        over = Image.open(path).convert('RGBA')
        box = over.getbbox()
        if box is None:
            print(f'{label}: the picture is empty')
            continue
        pad = 16
        crop = (max(0, box[0] - pad), max(0, box[1] - pad), min(over.width, box[2] + pad), min(over.height, box[3] + pad))
        zoom = max(1, min(3, 900 // (crop[3] - crop[1])))
        panels = []
        for name, body in bodies:
            g = on(SOIL, body)
            g.alpha_composite(over)
            whole = g.resize((g.width // 2, g.height // 2), Image.LANCZOS)
            near = g.crop(crop)
            near = near.resize((near.width * zoom, near.height * zoom), Image.NEAREST)
            panels.append((name, whole, near))
        width = sum(w.width + n.width + 60 for _, w, n in panels) + 40
        height = max(max(w.height, n.height) for _, w, n in panels) + 110
        card = Image.new('RGBA', (width, height), BACK)
        d = ImageDraw.Draw(card)
        m = edge_check.measure(Path(path))
        d.text((30, 12), f"{label} on {prefix}: whole at half size, then the gear at game pixels x{zoom}; {m['specks']} loose specks", fill=TEXT, font=f)
        x = 30
        for name, whole, near in panels:
            d.text((x, 50), name, fill=TEXT, font=f)
            card.alpha_composite(whole, (x, 84))
            card.alpha_composite(near, (x + whole.width + 20, 84))
            x += whole.width + near.width + 60
        out = out_folder / f'{n:02d}-{label}.png'
        out.parent.mkdir(parents=True, exist_ok=True)
        card.convert('RGB').save(out)
        print(out)
    return 0


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    sub = parser.add_subparsers(dest='command', required=True)
    s = sub.add_parser('sprites', help="a request file's attempts, exported as they would ship")
    s.add_argument('request')
    s.add_argument('--only', help='one request by name')
    s.add_argument('--size', type=int, default=128, help='pixels along the shorter side, as shipped')
    s.add_argument('--aspect', default='1:1')
    s.add_argument('--no-offer', action='store_true', help="draw the cards only, offering no pick on the Companion's Picks page")
    o = sub.add_parser('overlays', help='candidate pictures drawn on one of our vehicles')
    o.add_argument('--vehicle', required=True, help='texture folder and name, e.g. Tipper/Tipper or FieldKitchen/BillyFieldKitchen')
    o.add_argument('--candidate', action='append', required=True, help='LABEL=path to the exported ..._north.png')
    args = parser.parse_args(argv)
    return sprites(args) if args.command == 'sprites' else overlays(args)


if __name__ == '__main__':
    sys.exit(main())
