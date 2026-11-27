"""Ship the owner's pick of the toxic controlled area's paid icon (27 Nov 2026), and derive its pair by script, reproducibly.

    python assets/phobos-medical/ship_toxic_area_icons.py

The owner picked try 1 of `toxic-area-expand-paid` on the Picks page. It is exported as the expand icon. The clear icon is made from
the same master (paid rule 11: a free derivation over a paid one, so the pair matches exactly): the corner badge's green becomes the red
of the radiation pair's clear badge, and the plus sign's upright bar is filled with that red, leaving its crossbar as a minus. Both
are exported by scripts/art/export_sprite.py at 128 px, as the radiation pair is, and each gets its record under assets/records.
"""
import colorsys
import hashlib
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
REQUEST = 'assets/phobos-medical/requests/toxic-area-icons-paid.json'
GENERATED = ROOT / 'assets' / 'phobos-medical' / 'generated' / 'toxic-area-expand-paid'
PICK = 1
MASTER = GENERATED / f'toxic-area-expand-paid-{PICK}.png'
DERIVED = GENERATED / 'toxic-area-clear-derived.png'
TARGETS = 'mods/PhobosMedical/Common/Textures/Phobos/Medical/Protection'
RECORDS = ROOT / 'assets' / 'records' / 'PhobosMedical' / 'Common' / 'Textures' / 'Phobos' / 'Medical' / 'Protection'
RADIATION_CLEAR = ROOT / 'mods/PhobosMedical/Mods/Rimatomics/Textures/Phobos/Medical/Protection/RadiationAreaClear.png'
SIZE = 128


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def hsv(pixel):
    return colorsys.rgb_to_hsv(pixel[0] / 255, pixel[1] / 255, pixel[2] / 255)


def is_green(pixel):
    h, s, v = hsv(pixel)
    return 0.25 <= h <= 0.45 and s > 0.35 and v > 0.25


def is_white(pixel):
    h, s, v = hsv(pixel)
    return s < 0.18 and v > 0.85


def median(values):
    values = sorted(values)
    return values[len(values) // 2]


def badge_red():
    """The fill of the radiation clear icon's minus badge: its strongly red, opaque pixels, by median."""
    picture = Image.open(RADIATION_CLEAR).convert('RGBA')
    reds = [p for p in picture.get_flattened_data() if p[3] > 250 and hsv(p)[1] > 0.5 and (hsv(p)[0] < 0.04 or hsv(p)[0] > 0.96) and hsv(p)[2] > 0.4]
    return tuple(median([p[i] for p in reds]) for i in range(3))


def derive_clear():
    picture = Image.open(MASTER).convert('RGB')
    width, height = picture.size
    pixels = picture.load()
    green = [(x, y) for y in range(height) for x in range(width) if is_green(pixels[x, y])]
    gx0, gx1 = min(x for x, _ in green), max(x for x, _ in green)
    gy0, gy1 = min(y for _, y in green), max(y for _, y in green)
    cx, cy = (gx0 + gx1) / 2, (gy0 + gy1) / 2
    radius = max(gx1 - gx0, gy1 - gy0) / 2
    inside = lambda x, y: (x - cx) ** 2 + (y - cy) ** 2 <= (radius + 1) ** 2
    # The plus sign: the white inside the green disc. Its crossbar is the rows where white runs widest, its upright the columns.
    white = [(x, y) for y in range(gy0, gy1 + 1) for x in range(gx0, gx1 + 1) if inside(x, y) and is_white(pixels[x, y])]
    rows, columns = {}, {}
    for x, y in white:
        rows[y] = rows.get(y, 0) + 1
        columns[x] = columns.get(x, 0) + 1
    bar_rows = [y for y, n in rows.items() if n > 0.6 * max(rows.values())]
    upright = [x for x, n in columns.items() if n > 0.6 * max(columns.values())]
    by0, by1 = min(bar_rows), max(bar_rows)
    ux0, ux1 = min(upright), max(upright)
    px0, px1 = min(x for x, _ in white), max(x for x, _ in white)
    py0, py1 = min(y for _, y in white), max(y for _, y in white)
    margin = 4  # the plus sign's soft edge, in master pixels
    fill_green = tuple(median([pixels[x, y][i] for x, y in green]) for i in range(3))
    red = badge_red()
    fh, fs, fv = hsv(fill_green)
    rh, rs, rv = hsv(red)
    for y in range(gy0 - 2, gy1 + 3):
        for x in range(gx0 - 2, gx1 + 3):
            if not inside(x, y):
                continue
            h, s, v = hsv(pixels[x, y])
            if 0.2 <= h <= 0.5 and s > 0.15:
                # Green, or green blended into the outline or the white: the same blend of the red, by how far it is from the fill.
                r, g, b = colorsys.hsv_to_rgb(rh, min(1.0, s * rs / fs), min(1.0, v * rv / fv))
                pixels[x, y] = (round(r * 255), round(g * 255), round(b * 255))
    # The upright, above and below the crossbar's own rows: each row filled by blending the recoloured fill just left and right of
    # it, so no seam shows and no stub of the upright is left on the crossbar.
    left, right = ux0 - margin - 1, ux1 + margin + 1
    for y in range(py0 - margin, py1 + margin + 1):
        if by0 <= y <= by1:
            continue
        a, b = pixels[left, y], pixels[right, y]
        for x in range(left + 1, right):
            t = (x - left) / (right - left)
            pixels[x, y] = tuple(round(a[i] * (1 - t) + b[i] * t) for i in range(3))
    picture.save(DERIVED)
    return (px0, px1, py0, py1), (ux0, ux1), (by0, by1), fill_green, red


def export(master, target):
    target.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(target), '--size', str(SIZE)],
                   check=True, stdout=subprocess.DEVNULL)


def record(name, master, what, how):
    target = ROOT / TARGETS / f'{name}.png'
    lines = [f'# {name}', '',
             f'**AI-generated, picked by the owner on 27 November 2026.** Made on 27 Nov 2026 with OpenAI gpt-image-2 through the image API, '
             f'at medium quality: {what}. {how} Exported by `scripts/art/export_sprite.py` (white background to transparency, {SIZE} x {SIZE} '
             f'px) by `assets/phobos-medical/ship_toxic_area_icons.py`. The prompt and its references are in `{REQUEST}`; the master and its '
             'record go to the `art-masters` branch, the other attempt to `art-references`. The only references uploaded were our own '
             'approved radiation area icons (the layout and finish of the set). The symbol is the chemical hazard pictogram for a toxic '
             'substance (a skull and crossbones, GHS06), drawn fresh. No game or other author\'s art is in this file.', '',
             f'- Master: `{master.name}`, SHA-256 `{sha(master)}`',
             f'- `{name}.png`: SHA-256 `{sha(target)}`', '']
    RECORDS.mkdir(parents=True, exist_ok=True)
    (RECORDS / f'{name}.record.md').write_text('\n'.join(lines), encoding='utf-8', newline='\n')


def main():
    plus, upright, bar, fill_green, red = derive_clear()
    print(f'plus {plus}, upright columns {upright}, crossbar rows {bar}, green {fill_green} to red {red}')
    export(MASTER, ROOT / TARGETS / 'ToxicAreaExpand.png')
    export(DERIVED, ROOT / TARGETS / 'ToxicAreaClear.png')
    record('ToxicAreaExpand', MASTER, 'the Zone menu tool that paints the toxic controlled area, replacing the game\'s home-area icon '
           'it borrowed since Medical 0.30.0', f'Picked by the owner from two attempts (attempt {PICK}) on the Picks page.')
    record('ToxicAreaClear', DERIVED, 'the Zone menu tool that clears the toxic controlled area, replacing the game\'s home-area icon '
           'it borrowed since Medical 0.30.0', f'Derived by script from the owner\'s pick for the expand icon (attempt {PICK}): its corner '
           'badge recoloured to the radiation clear icon\'s red, and the plus sign\'s upright filled in, leaving a minus. No picture was '
           'paid for it.')


if __name__ == '__main__':
    main()
