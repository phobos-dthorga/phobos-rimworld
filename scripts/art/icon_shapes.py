"""Shapes for icons drawn by script in the game's look: a thick near-black outline, flat cel shading and rounded corners.

Shared by draw_schematic_icon.py and draw_separator_icon.py (and any later button drawn the same way): each draws at SCALE times its
shipped size and is shrunk with Lanczos, so its edges are smooth with no stray pixels. Coordinates are in shipped pixels. Game-free: a next
game passes its own outline and colours.
"""
from __future__ import annotations

import hashlib
from pathlib import Path

from PIL import Image, ImageDraw

SIZE = 128
SCALE = 8
OUTLINE = (24, 24, 26, 255)
OUTLINE_PX = 5
RADIUS_PX = 6


def shade(colour, amount):
    """A colour made lighter (positive) or darker (negative), opaque."""
    return tuple(max(0, min(255, c + amount)) for c in colour[:3]) + (255,)


def canvas() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    big = Image.new('RGBA', (SIZE * SCALE, SIZE * SCALE), (0, 0, 0, 0))
    return big, ImageDraw.Draw(big)


def block(d: ImageDraw.ImageDraw, box, fill):
    """A rounded block with its outline, a lit band on the left and a shade band on the right."""
    s = SCALE
    outer = tuple(v * s for v in box)
    d.rounded_rectangle(outer, radius=RADIUS_PX * s, fill=OUTLINE)
    o = OUTLINE_PX * s
    inner = (outer[0] + o, outer[1] + o, outer[2] - o, outer[3] - o)
    r = max(1, (RADIUS_PX - OUTLINE_PX // 2) * s)
    d.rounded_rectangle(inner, radius=r, fill=shade(fill, 0))
    width = inner[2] - inner[0]
    d.rectangle((inner[0], inner[1] + r, inner[0] + int(width * 0.24), inner[3] - r), fill=shade(fill, 30))
    d.rectangle((inner[2] - int(width * 0.2), inner[1] + r, inner[2], inner[3] - r), fill=shade(fill, -26))


def pipe(d: ImageDraw.ImageDraw, path, fill, width=10):
    """A pipe along straight runs: the outline drawn wide first, then the fill inside it, with round joints."""
    s = SCALE
    points = [(x * s, y * s) for x, y in path]
    for w, colour in ((width + OUTLINE_PX * 2, OUTLINE), (width, shade(fill, 0))):
        d.line(points, fill=colour, width=w * s, joint='curve')
        for x, y in points:
            rad = w * s // 2
            d.ellipse((x - rad, y - rad, x + rad, y + rad), fill=colour)


def disc(d: ImageDraw.ImageDraw, centre, radius, fill, lit=True):
    """A round part with its outline, lit on its upper left."""
    s = SCALE
    cx, cy = centre[0] * s, centre[1] * s
    r = radius * s
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=OUTLINE)
    ri = max(1, (radius - OUTLINE_PX + 1)) * s
    d.ellipse((cx - ri, cy - ri, cx + ri, cy + ri), fill=shade(fill, 0))
    if lit:
        d.pieslice((cx - ri, cy - ri, cx + ri, cy + ri), 110, 250, fill=shade(fill, 30))


def arrowhead(d: ImageDraw.ImageDraw, tip, direction, size, fill):
    """A filled arrowhead with its outline, its tip at <tip>, pointing along <direction> (dx, dy), <size> shipped pixels long."""
    s = SCALE
    dx, dy = direction
    length = (dx * dx + dy * dy) ** 0.5 or 1
    ux, uy = dx / length, dy / length
    px, py = -uy, ux
    tx, ty = tip[0] * s, tip[1] * s
    base = (tx - ux * size * s, ty - uy * size * s)
    half = size * 0.7 * s
    points = [(tx, ty), (base[0] + px * half, base[1] + py * half), (base[0] - px * half, base[1] - py * half)]
    grow = OUTLINE_PX * s * 0.9
    outline = [(tx + ux * grow, ty + uy * grow),
               (base[0] + px * (half + grow) - ux * grow * 0.6, base[1] + py * (half + grow) - uy * grow * 0.6),
               (base[0] - px * (half + grow) - ux * grow * 0.6, base[1] - py * (half + grow) - uy * grow * 0.6)]
    d.polygon(outline, fill=OUTLINE)
    d.polygon(points, fill=shade(fill, 0))


def ship(big: Image.Image, folder: Path, name: str, art_paths, record: str, target_folder: str | None) -> Path:
    """Saves the master and the shipped size to the art working folder, and, given a folder, the icon there with its record."""
    folder.mkdir(parents=True, exist_ok=True)
    big.save(folder / f'{name}-master.png')
    small = big.resize((SIZE, SIZE), Image.LANCZOS)
    small.save(folder / f'{name}.png')
    print(folder / f'{name}.png')
    if not target_folder:
        return folder / f'{name}.png'
    target = Path(target_folder) / f'{name}.png'
    small.save(target)
    sha = hashlib.sha256(target.read_bytes()).hexdigest()
    path = art_paths.record_path(target.with_name(f'{name}.record.md'))
    path.write_text(record.format(sha=sha), encoding='utf-8', newline='\n')
    print(target)
    print(path)
    return target
