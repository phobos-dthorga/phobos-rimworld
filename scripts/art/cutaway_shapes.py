"""Shared drawing for the systems schematic's cutaways: region masks from lists of shapes, white shaded layers, an outline worked out
from the regions, a tinted preview, and shipping with a record for each layer.

Used by draw_diesel_cutaway.py and draw_turboshaft_cutaway.py. A shape is ('rect', box), ('poly', points), ('ellipse', box) or
('line', points, width), in shipped pixels; regions are drawn in order and a later one covers an earlier one where they meet, so no two
layers overlap. Game-free: a next game passes its own regions and canvas.
"""
from __future__ import annotations

import hashlib
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

SCALE = 4
OUTLINE = (24, 24, 26)
OUTLINE_PX = 2.5


def draw_shape(d: ImageDraw.ImageDraw, shape, scale: int = SCALE) -> None:
    s = scale
    kind = shape[0]
    if kind == 'rect':
        d.rectangle(tuple(v * s for v in shape[1]), fill=255)
    elif kind == 'ellipse':
        d.ellipse(tuple(v * s for v in shape[1]), fill=255)
    elif kind == 'poly':
        d.polygon([(x * s, y * s) for x, y in shape[1]], fill=255)
    elif kind == 'line':
        points = [(x * s, y * s) for x, y in shape[1]]
        width = int(shape[2] * s)
        d.line(points, fill=255, width=width, joint='curve')
        for x, y in points:
            r = width // 2
            d.ellipse((x - r, y - r, x + r, y + r), fill=255)
    else:
        raise ValueError(kind)


def masks(regions: dict, width: int, height: int, scale: int = SCALE) -> dict[str, Image.Image]:
    """Each region's mask at <scale> times, with every later region taken out of it, so no two layers overlap."""
    drawn = {}
    for name, shapes in regions.items():
        mask = Image.new('L', (width * scale, height * scale), 0)
        d = ImageDraw.Draw(mask)
        for shape in shapes:
            draw_shape(d, shape, scale)
        drawn[name] = mask
    names = list(regions)
    for i, name in enumerate(names):
        for later in names[i + 1:]:
            drawn[name] = ImageChops.subtract(drawn[name], drawn[later])
    return drawn


def layer(mask: Image.Image) -> Image.Image:
    """A white layer over its mask, shaded light grey toward the bottom so a flat tint still reads as a solid."""
    w, h = mask.size
    shading = Image.linear_gradient('L').resize((w, h)).point(lambda v: 255 - int(v * 0.22))
    return Image.merge('RGBA', (shading, shading, shading, mask))


def outline(drawn: dict[str, Image.Image], scale: int = SCALE) -> Image.Image:
    """A near-black ring round every region, from the regions themselves."""
    size = int(OUTLINE_PX * scale) * 2 + 1
    first = next(iter(drawn.values()))
    ring = Image.new('L', first.size, 0)
    for mask in drawn.values():
        grown = mask.filter(ImageFilter.MaxFilter(size))
        ring = ImageChops.lighter(ring, ImageChops.subtract(grown, mask.filter(ImageFilter.MinFilter(3))))
    edge = Image.new('RGBA', ring.size, OUTLINE + (0,))
    edge.putalpha(ring)
    return edge


def draw(regions: dict, width: int, height: int, scale: int = SCALE, outline_as_one: tuple = ()) -> dict[str, Image.Image]:
    """Every region's layer and the outline, at their shipped size. The regions named in <outline_as_one> are outlined as one, so
    bands cut from one passage (a gas path's heat bands) show no line between them."""
    drawn = masks(regions, width, height, scale)
    out = {name: layer(mask).resize((width, height), Image.LANCZOS) for name, mask in drawn.items()}
    edges = {name: mask for name, mask in drawn.items() if name not in outline_as_one}
    if outline_as_one:
        merged = Image.new('L', next(iter(drawn.values())).size, 0)
        for name in outline_as_one:
            merged = ImageChops.lighter(merged, drawn[name])
        edges['+'.join(outline_as_one)] = merged
    out['Outline'] = outline(edges, scale).resize((width, height), Image.LANCZOS)
    return out


def preview(layers: dict[str, Image.Image], tints: dict, width: int, height: int) -> Image.Image:
    """The layers tinted with a preview's colours on dark ground, the outline over them."""
    ground = Image.new('RGBA', (width, height), (20, 23, 26, 255))
    for name, img in layers.items():
        if name == 'Outline':
            continue
        tint = Image.new('RGBA', (width, height), tints[name] + (255,))
        tinted = ImageChops.multiply(img, tint)
        tinted.putalpha(img.getchannel('A'))
        ground.alpha_composite(tinted)
    ground.alpha_composite(layers['Outline'])
    return ground


def ship(layers: dict[str, Image.Image], folder: Path, record_path, record: str) -> list[Path]:
    """Writes every layer that has anything in it into <folder>, and a record for each (record.format(name=, sha=)) at record_path(file)."""
    folder.mkdir(parents=True, exist_ok=True)
    written = []
    for name, img in layers.items():
        if img.getchannel('A').getbbox() is None:
            continue
        target = folder / f'{name}.png'
        img.save(target)
        sha = hashlib.sha256(target.read_bytes()).hexdigest()
        path = record_path(target.with_name(f'{name}.record.md'))
        path.write_text(record.format(name=name, sha=sha), encoding='utf-8', newline='\n')
        written.append(target)
    return written
