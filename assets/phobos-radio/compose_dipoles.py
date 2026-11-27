"""The six HF dipoles' textures (Radio 0.62.0; docs/development/radio-hf.md, slice 1), laid out from the owner's picks of the paid wire mast
and dipole parts sheet (assets/phobos-radio/requests/hf-kit-paid.json), with the wires drawn by script.

Each dipole is a strip one cell deep and as long as its band's antenna at 1 m a cell. Its texture is Graphic_Multi: a north face (the strip
across the canvas) and an east face (the strip up it), drawn at (length + 6, 6) cells with the canvas lifted two cells, so the masts,
drawn upright as the game draws trees and at the VHF mast's scale (the owner's word of 28 Nov 2026: "several tiles big, quite like how the
real life versions can become"), have room above the strip at either end. Drawn at twice the size and scaled down, at 128 px a cell.

    python assets/phobos-radio/compose_dipoles.py --mast 1 --parts 2
"""
import argparse
import hashlib
import math
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
OUT = ROOT / "mods" / "PhobosRadioFramework" / "Common" / "Textures" / "Phobos" / "Radio" / "Things"
CELL = 48          # the whole-dipole pictures are only the placement ghost and menu icon since Radio 0.64.0: kept small
SCALE = 2           # drawn at twice the size, then scaled down
C = CELL * SCALE
MAST_CELLS = 4.5    # a 10 m mast's picture, cells tall: the VHF mast's own scale (its 10 m drawn 4.5 cells tall)
MARGIN = 3          # cells of canvas beyond each end of the strip, for the masts' guys and tops
HEIGHT = 6          # cells of canvas across the strip; the strip sits one cell up from its foot (drawOffset (0,0,2))
FOOT = 0.75         # the footing's centre, down the mast picture's height (measured from both tries)
WIRE_DARK = (24, 24, 26, 255)
WIRE_CORE = (128, 132, 138, 255)
COAX = (16, 16, 18, 255)

# (defName, cells long, layout, mast scale)
DIPOLES = [
    ("Dipole40Flat", 21, "flat", 1.0),
    ("Dipole20Flat", 11, "flat", 1.0),
    ("Dipole40Vee", 14, "vee", 1.0),
    ("Dipole20Vee", 7, "vee", 1.0),
    ("Dipole40Short", 6, "short", 0.8),
    ("Dipole20Short", 4, "short", 0.8),
]


def cut_out(source, size, aspect):
    """The picture with its white taken out, by the shared sprite exporter, cropped to what is left."""
    with tempfile.TemporaryDirectory() as folder:
        target = Path(folder) / "cut.png"
        subprocess.run([sys.executable, str(ROOT / "scripts" / "art" / "export_sprite.py"), str(source), str(target), "--size", str(size),
                        "--aspect", aspect], check=True, capture_output=True)
        im = Image.open(target).convert("RGBA")
        im.load()
    return im.crop(im.getbbox())


def split_parts(sheet):
    """The parts sheet's four objects, left to right: centre feed, loading coil, egg insulator, ground peg."""
    alpha = sheet.getchannel("A")
    w, h = sheet.size
    filled = [any(alpha.getpixel((x, y)) > 16 for y in range(0, h, 2)) for x in range(w)]
    runs, start = [], None
    for x, on in enumerate(filled + [False]):
        if on and start is None:
            start = x
        elif not on and start is not None:
            runs.append((start, x))
            start = None
    runs = sorted(sorted(runs, key=lambda r: r[1] - r[0], reverse=True)[:4])
    if len(runs) != 4:
        raise SystemExit(f"the parts sheet split into {len(runs)} objects, not 4")
    parts = []
    for a, b in runs:
        piece = sheet.crop((a, 0, b, h))
        parts.append(piece.crop(piece.getbbox()))
    return parts


def fit(im, width_cells=None, height_cells=None):
    w, h = im.size
    k = (width_cells * C / w) if width_cells else (height_cells * C / h)
    return im.resize((max(1, round(w * k)), max(1, round(h * k))), Image.LANCZOS)


def paste_centred(canvas, im, x, y):
    canvas.alpha_composite(im, (round(x - im.width / 2), round(y - im.height / 2)))


def wire(draw, points):
    draw.line(points, fill=WIRE_DARK, width=round(0.05 * C), joint="curve")
    draw.line(points, fill=WIRE_CORE, width=max(1, round(0.018 * C)), joint="curve")


def sagged(a, b, sag, steps=24):
    """A wire from a to b hanging by sag pixels at its middle (a parabola, near enough to a catenary at this size)."""
    return [(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t + sag * 4 * t * (1 - t)) for t in (i / steps for i in range(steps + 1))]


def along(points, t):
    """The point a share t of the way along a polyline, and the line's angle there in degrees."""
    lengths = [math.dist(points[i], points[i + 1]) for i in range(len(points) - 1)]
    goal, run = t * sum(lengths), 0.0
    for i, d in enumerate(lengths):
        if run + d >= goal or i == len(lengths) - 1:
            s = (goal - run) / d if d else 0
            (x0, y0), (x1, y1) = points[i], points[i + 1]
            return (x0 + (x1 - x0) * s, y0 + (y1 - y0) * s), math.degrees(math.atan2(y1 - y0, x1 - x0))
        run += d


def on_wire(canvas, part, points, t):
    (x, y), angle = along(points, t)
    paste_centred(canvas, part.rotate(-angle, expand=True, resample=Image.BICUBIC), x, y)


def compose(length, layout, mast_scale, mast, parts, east):
    feed, coil, egg, peg = parts
    cells_w, cells_h = (HEIGHT, length + 2 * MARGIN) if east else (length + 2 * MARGIN, HEIGHT)
    canvas = Image.new("RGBA", (cells_w * C, cells_h * C), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    m = fit(mast, height_cells=MAST_CELLS * mast_scale)
    rise = m.height * FOOT  # from the footing up to the mast's top

    def ground(u):
        """The strip's centre line at u cells from its start (west, or south on the east face), on the canvas."""
        if east:
            return HEIGHT / 2 * C, (length + 5 - u) * C
        return (MARGIN + u) * C, (HEIGHT - 1) * C

    def top(u):
        x, y = ground(u)
        return x, y - rise + m.height * 0.03

    def put_mast(u):
        x, y = ground(u)
        canvas.alpha_composite(m, (round(x - m.width / 2), round(y - m.height * FOOT)))

    feed_p, coil_p, egg_p = fit(feed, width_cells=0.32), fit(coil, width_cells=0.42), fit(egg, width_cells=0.26)
    peg_p = fit(peg, height_cells=0.32)
    if layout in ("flat", "short"):
        a, b = 0.5, length - 0.5
        line = sagged(top(a), top(b), 0.12 * C if layout == "flat" else 0.08 * C)
        mid = along(line, 0.5)[0]
        # Seen along the strip (the east face) the wire runs over the far mast, so the masts go on top of it there.
        if not east:
            put_mast(a)
            put_mast(b)
        draw.line([mid, ground(length / 2)], fill=COAX, width=round(0.04 * C))
        wire(draw, line)
        if east:
            put_mast(b)
            put_mast(a)
        for t in (0.04, 0.96):
            on_wire(canvas, egg_p, line, t)
        if layout == "short":
            for t in (0.3, 0.7):
                on_wire(canvas, coil_p, line, t)
        paste_centred(canvas, feed_p, mid[0], mid[1] + feed_p.height * 0.2)
    else:
        centre = length / 2
        put_mast(centre)
        apex = top(centre)
        x0, y0 = ground(centre)
        draw.line([apex, (x0, y0 - m.height * 0.05)], fill=COAX, width=round(0.035 * C))
        for end in (0.35, length - 0.35):
            gx, gy = ground(end)
            line = sagged(apex, (gx, gy - 0.06 * C), 0.04 * C)
            wire(draw, line)
            on_wire(canvas, egg_p, line, 0.9)
            canvas.alpha_composite(peg_p, (round(gx - peg_p.width / 2), round(gy - peg_p.height * 0.75)))
        paste_centred(canvas, feed_p, apex[0], apex[1] + feed_p.height * 0.4)
    return canvas.resize((cells_w * CELL, cells_h * CELL), Image.LANCZOS)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def record(texture, mast_try, parts_try, mast_src, parts_src):
    """The texture's record, where the art audit looks for it (scripts/art/art_paths.record_path)."""
    sys.path.insert(0, str(ROOT / "scripts" / "art"))
    import art_paths
    target = art_paths.record_path(Path(texture).with_suffix(".record.md"))
    target.write_text(
        f"# {Path(texture).stem}\n\n"
        "Made with OpenAI gpt-image-2 through the OpenAI image API (paid per image): **AI-generated** parts, laid out by script. "
        "The owner's picks of 28 Nov 2026: the wire mast (drawn at the VHF mast's scale, the owner's word: \"several tiles big, quite like "
        "how the real life versions can become\") and the dipole parts sheet. The request is `assets/phobos-radio/requests/hf-kit-paid.json`; "
        "its references were our own approved art, for style only; no game or other author's art was uploaded or used. Cut out by "
        "`scripts/art/export_sprite.py` and laid out by `assets/phobos-radio/compose_dipoles.py`, which draws the wires and the coax.\n\n"
        f"- Mast: `hf-wire-mast-{mast_try}`, SHA-256 `{sha(mast_src)}`\n"
        f"- Parts: `hf-dipole-parts-{parts_try}`, SHA-256 `{sha(parts_src)}`\n"
        f"- `{Path(texture).name}`: SHA-256 `{sha(texture)}`\n", encoding="utf-8", newline="\n")


PARTS_OUT = OUT / "HfParts"
SPRITE_CELL = 128
# Each part as the game draws it (Radio 0.64.0, the dipoles drawn from parts into the map's mesh): its name, and its height (masts) or
# width (the small parts) in cells. The masts' heights are the game's drawing of 10, 15 and 25 m, foreshortened (agent's figures).
MASTS = {"HfMastPole": 4.5, "HfMastTelescopic": 6.0, "HfMastLattice": 8.0}
SMALL = {"HfFeed": 0.32, "HfCoil": 0.42, "HfInsulator": 0.26, "HfPeg": 0.32}


def part_record(texture, source_name, source, request):
    sys.path.insert(0, str(ROOT / "scripts" / "art"))
    import art_paths
    target = art_paths.record_path(Path(texture).with_suffix(".record.md"))
    target.write_text(
        f"# {Path(texture).stem}\n\n"
        "Made with OpenAI gpt-image-2 through the OpenAI image API (paid per image): **AI-generated**, a part the six HF dipoles are drawn "
        "from in the map's mesh (Radio 0.64.0). The owner's picks of 28 Nov 2026 (all the masts, each a real kind). The request is "
        f"`assets/phobos-radio/requests/{request}`; its references were our own approved art, for style only; no game or other author's "
        "art was uploaded or used. Cut out by `scripts/art/export_sprite.py` in `assets/phobos-radio/compose_dipoles.py`.\n\n"
        f"- Source: `{source_name}`, SHA-256 `{sha(source)}`\n"
        f"- `{Path(texture).name}`: SHA-256 `{sha(texture)}`\n", encoding="utf-8", newline="\n")


def export_parts(pole_try, telescopic_try, lattice_try, parts_try):
    """The parts the game draws the dipoles from, at 128 px a cell, each with its record; and the wire, a white strip the game tints."""
    generated = HERE / "generated"
    PARTS_OUT.mkdir(parents=True, exist_ok=True)
    sources = {"HfMastPole": ("hf-wire-mast", pole_try, "hf-kit-paid.json"),
               "HfMastTelescopic": ("hf-wire-mast", telescopic_try, "hf-kit-paid.json"),
               "HfMastLattice": ("hf-lattice-tower", lattice_try, "hf-lattice-paid.json")}
    for name, cells in MASTS.items():
        folder, n, request = sources[name]
        src = generated / folder / f"{folder}-{n}.png"
        im = cut_out(src, 1024, "2:3")
        im = im.resize((max(1, round(im.width * cells * SPRITE_CELL / im.height)), round(cells * SPRITE_CELL)), Image.LANCZOS)
        im.save(PARTS_OUT / f"{name}.png")
        part_record(PARTS_OUT / f"{name}.png", f"{folder}-{n}", src, request)
    src = generated / "hf-dipole-parts" / f"hf-dipole-parts-{parts_try}.png"
    for (name, cells), im in zip(SMALL.items(), split_parts(cut_out(src, 1024, "3:2"))):
        k = cells * SPRITE_CELL / (im.width if name != "HfPeg" else im.height)
        im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
        im.save(PARTS_OUT / f"{name}.png")
        part_record(PARTS_OUT / f"{name}.png", f"hf-dipole-parts-{parts_try}", src, "hf-kit-paid.json")
    wire = Image.new("RGBA", (8, 8), (255, 255, 255, 255))
    wire.save(PARTS_OUT / "HfWire.png")
    sys.path.insert(0, str(ROOT / "scripts" / "art"))
    import art_paths
    art_paths.record_path(PARTS_OUT / "HfWire.record.md").write_text(
        "# HfWire\n\nDrawn by script: an 8 by 8 white square, which the game tints and stretches into the dipoles' wires (Radio 0.64.0). Made "
        "by `assets/phobos-radio/compose_dipoles.py`; no AI and no other author's art.\n", encoding="utf-8", newline="\n")
    print("exported the parts: " + ", ".join(list(MASTS) + list(SMALL) + ["HfWire"]))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--mast", type=int, required=True, help="the picked wire-mast try for the pole")
    parser.add_argument("--parts", type=int, required=True, help="the picked parts-sheet try")
    parser.add_argument("--telescopic", type=int, help="the wire-mast try for the telescopic mast (Radio 0.64.0)")
    parser.add_argument("--lattice", type=int, help="the picked lattice-tower try (Radio 0.64.0)")
    parser.add_argument("--out", default=str(OUT))
    args = parser.parse_args()
    if args.telescopic and args.lattice:
        export_parts(args.mast, args.telescopic, args.lattice, args.parts)
    generated = HERE / "generated"
    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    mast_src = generated / "hf-wire-mast" / f"hf-wire-mast-{args.mast}.png"
    parts_src = generated / "hf-dipole-parts" / f"hf-dipole-parts-{args.parts}.png"
    mast = cut_out(mast_src, 1024, "2:3")
    parts = split_parts(cut_out(parts_src, 1024, "3:2"))
    shipping = out.resolve() == OUT.resolve()
    for name, length, layout, scale in DIPOLES:
        north = compose(length, layout, scale, mast, parts, east=False)
        north.save(out / f"{name}_north.png")
        north.save(out / f"{name}_south.png")
        compose(length, layout, scale, mast, parts, east=True).save(out / f"{name}_east.png")
        if shipping:
            for face in ("north", "south", "east"):
                record(out / f"{name}_{face}.png", args.mast, args.parts, mast_src, parts_src)
        print(f"{name}: {north.size[0]}x{north.size[1]} north and south, east turned")


if __name__ == "__main__":
    main()
