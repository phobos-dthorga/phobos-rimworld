"""Export the V-22 Osprey's art at the game's scale: body, overlays, turrets and the pieces our own renderer draws. Free, processor
only, Pillow only; never changes a master. Stages into the art tools' working folder (K: on the owner's PC) with a review page.

    python assets/phobos-vehicles/export_osprey.py                       stage, with the review page
    python assets/phobos-vehicles/export_osprey.py --out mods/PhobosVehicles/Textures/Phobos/Vehicles/Osprey   ship (after review)

What it makes, from the design (docs/development/vehicle-osprey.md, "Art") and the owner's picks of 19 Nov 2026
(requests/osprey-overlays-record.md):

  Osprey_north/east/south (+m)    the body at 20.5 cells nose to tail on a 19 x 21 cell canvas, 128 px a cell; east and south are
                                  the north turned (Vehicle Framework mirrors east for west); paint mask, icon, transform
  Osprey_<Overlay>_north/...      upgrade overlays on the body's canvas: probe, radar nose, hoist, jammer
  Osprey_RampGun50, _RampGun762   turrets: each centred on its mount, barrel up (north), as Vehicle Framework draws a turret; the
                                  Def turns them to face aft
  Osprey_Rotor, _RotorSpin        our renderer's rotor, centred on its hub, 11.61 m across [S] at the body's scale
  Osprey_Nacelle<angle>[Barrier]  our renderer's left nacelle at 0, 30, 60 and 90 degrees, with paint masks; the right is the left
                                  mirrored; the inlet barrier drawn into its own set so it tilts with the nacelle
  Osprey.layout.json              where the renderer and the Def put things, in cells from the body's centre (x east, z north)

The body is shrunk once to just above its drawn size before the paint is worked out (a 4K master walked pixel by pixel in Python
needs gigabytes beside the game); the background AI's mask is shrunk with it. Paint: the airframe's grey is the main paint, the
orange bands the accent, and glass is kept out of the grey by a saturation ceiling (export_vehicle.classify).
"""
import argparse
import hashlib
import html
import json
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageFilter

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402
import export_vehicle as ev  # noqa: E402
from export_sprite import file_sha256  # noqa: E402

G = HERE / 'generated'
PICK = G / 'osprey-trial-nano-banana-pro/osprey-trial-nano-banana-pro-1.png'
SAM, DERIVED = G / 'osprey-overlays-sam', G / 'osprey-derived'
NAME = 'Osprey'
CELLS_LONG = 20.5                 # Nose to tail, on the Mi-38's scale (owner, 18 Nov 2026; vehicle-osprey.md, "Size")
DRAW = (19, 21)                   # The body's canvas in cells: the airframe is about 18.3 x 20.5
METRES_PER_CELL = 1 / 1.175       # The Mi-38's scale (vehicle-osprey.md)
ROTOR_METRES = 11.61              # [S] Boeing / Wikipedia
# Measured 19 Nov 2026: the airframe's grey at hue 220-256, saturation 0.02-0.07; windows the same hue at 0.27; the bands' orange at
# hue 24, and their one-pixel blend into the grey at hue 0-7, so the accent starts at 0.
PAINT = {'main_hue': (200, 280), 'accent_hue': (0, 34), 'min_saturation': 0.0, 'main_max_saturation': 0.15}
# Regions, then flecks repaired with Segment Anything's parts map of the shrunk body (owner's go, 19 Nov 2026: made by
# paint_segments.py on osprey-body-at-scale.png, 2 min 42 s on the graphics card); 'regions' if no map has been made.
BODY_PAINT = 'repaired'
# Thin dark marks (seams, hatch rims, rivets) taken whole, each with the paint of the panels around it; any touching glass stays bare.
# The Osprey's seams are a lighter dark than its outline, so they came out half painted and half bare: speckled at full zoom, with grey
# bars on the hatch rims (the owner's look, 19 Nov 2026). Darker than 0.45 and under 9 px wide [A, tried on this body].
LINES = (0.45, 9)
HEADROOM = 1.03                  # The body is pre-shrunk to this much above its drawn size, so the exporter still only shrinks
OVERLAYS = {'Probe': SAM / 'probe.png', 'RadarNose': DERIVED / 'radar-nose.png', 'Hoist': SAM / 'hoist-b.png', 'Jammer': SAM / 'jammer.png'}
DAMAGE = ('Cockpit', 'ForwardCabin', 'AftCabin', 'LeftWing', 'RightWing', 'Tail')   # The body's outer parts (vehicle-osprey.md)
TURRETS = {'RampGun50': DERIVED / 'ramp-gun-50.png', 'RampGun762': DERIVED / 'ramp-gun-762.png'}
TURRET_CELLS = 3                  # A turret's square canvas: the .50 reaches 1.4 cells aft of its mount
PREVIEW_PAINT = ((150, 40, 40), (230, 190, 60))  # A red and yellow livery for the review page only


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def solid_box(image):
    return image.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()


class Mapping:
    """The pick's pixels to the body texture's pixels and to cells from its centre."""

    def __init__(self, prescale, transform):
        self.prescale, self.scale = prescale, transform['scale']
        self.offset, self.canvas = transform['offset'], transform['canvas']

    @property
    def total(self):
        return self.prescale * self.scale

    def texture(self, x, y):
        return x * self.total + self.offset[0], y * self.total + self.offset[1]

    def cells(self, x, y):
        tx, ty = self.texture(x, y)
        return round((tx - self.canvas[0] / 2) / ev.PIXELS_PER_CELL, 3), round(-(ty - self.canvas[1] / 2) / ev.PIXELS_PER_CELL, 3)


def shrink(image, factor):
    """Premultiplied, so a clear edge carries no colour into its neighbours."""
    size = (max(1, round(image.width * factor)), max(1, round(image.height * factor)))
    return image.convert('RGBa').resize(size, Image.LANCZOS).convert('RGBA')


def prepare_body(work):
    """The pick shrunk to just above its drawn size, on white, with the background AI's mask shrunk alike and filed under it."""
    pick = Image.open(PICK).convert('RGB')
    r, g, b = pick.split()
    not_white = ImageChops.invert(ImageChops.multiply(ImageChops.multiply(r.point(lambda v: 255 if v >= 235 else 0),
                                                                          g.point(lambda v: 255 if v >= 235 else 0)),
                                                      b.point(lambda v: 255 if v >= 235 else 0)))
    top, bottom = not_white.getbbox()[1], not_white.getbbox()[3]
    prescale = CELLS_LONG * ev.PIXELS_PER_CELL * HEADROOM / (bottom - top)
    size = (round(pick.width * prescale), round(pick.height * prescale))
    small = pick.resize(size, Image.LANCZOS)
    path = work / 'osprey-body-at-scale.png'
    small.save(path)
    mask = art_paths.find('bgmasks', f'{file_sha256(PICK)}.png')
    if mask is None:
        raise SystemExit('the background AI has not looked at the pick: run clean_backgrounds.py masks --cpu --also <pick> first')
    target = art_paths.work('bgmasks', f'{file_sha256(path)}.png')
    target.parent.mkdir(parents=True, exist_ok=True)
    Image.open(mask).convert('L').resize(size, Image.LANCZOS).save(target)
    return path, prescale


def overlay_on_body(source, mapping, trim=True):
    """An overlay lifted onto the pick's canvas, placed on the body's canvas. Its edge is taken in one pixel, so the soft edge is
    the part's own dark outline and not the white it was lifted from; damage marks keep their soft soot (trim=False)."""
    part = Image.open(source).convert('RGBA')
    box = part.getchannel('A').getbbox() if not trim else solid_box(part)
    margin = 8
    crop = part.crop((box[0] - margin, box[1] - margin, box[2] + margin, box[3] + margin))
    if trim:
        crop.putalpha(crop.getchannel('A').filter(ImageFilter.MinFilter(3)))
    small = shrink(crop, mapping.total)
    canvas = Image.new('RGBA', tuple(mapping.canvas), (0, 0, 0, 0))
    x, y = mapping.texture(box[0] - margin, box[1] - margin)
    canvas.alpha_composite(small, (round(x), round(y)))
    return canvas


def turret(source, mapping):
    """A gun centred on its mount, barrel up: Vehicle Framework draws a turret's texture about its centre, facing north."""
    gun = Image.open(source).convert('RGBA')
    mount = json.loads(source.with_suffix('.json').read_text(encoding='utf-8'))['mount_px']
    side = TURRET_CELLS * ev.PIXELS_PER_CELL
    half = side / 2 / mapping.total
    crop = gun.crop((round(mount[0] - half), round(mount[1] - half), round(mount[0] + half), round(mount[1] + half)))
    crop.putalpha(crop.getchannel('A').filter(ImageFilter.MinFilter(3)))
    picture = shrink(crop, side / crop.width).resize((side, side), Image.LANCZOS).transpose(Image.ROTATE_180)
    return picture, mount


def renderer_piece(source, factor, canvas_cells=None):
    piece = shrink(Image.open(source).convert('RGBA'), factor)
    if canvas_cells:
        side = canvas_cells * ev.PIXELS_PER_CELL
        square = Image.new('RGBA', (side, side), (0, 0, 0, 0))
        square.alpha_composite(piece, ((side - piece.width) // 2, (side - piece.height) // 2))
        return square
    return piece


def tinted(texture, mask):
    """What the game shows with the review livery: the paint shader multiplies the texture by a colour where the mask is set."""
    out = texture.copy()
    t, m = out.load(), mask.convert('RGBA').load()
    for y in range(out.height):
        for x in range(out.width):
            red, green, blue, alpha = t[x, y]
            if not alpha:
                continue
            mr, mg = m[x, y][0], m[x, y][1]
            colour = PREVIEW_PAINT[0] if mr > 127 else PREVIEW_PAINT[1] if mg > 127 else None
            if colour:
                t[x, y] = (red * colour[0] // 255, green * colour[1] // 255, blue * colour[2] // 255, alpha)
    return out


def on_dark(image):
    ground = Image.new('RGBA', image.size, (38, 40, 34, 255))
    ground.alpha_composite(image)
    return ground.convert('RGB')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', type=Path, help='where to write the textures (default: the art working folder, staged)')
    args = parser.parse_args(argv)
    work = art_paths.work('osprey-export')
    out = (ROOT / args.out) if args.out and not args.out.is_absolute() else (args.out or work / 'textures')
    work.mkdir(parents=True, exist_ok=True)
    out.mkdir(parents=True, exist_ok=True)
    files = []

    body_master, prescale = prepare_body(work)
    result = ev.export_body(body_master, out, NAME, CELLS_LONG, DRAW, PAINT['main_hue'], PAINT['accent_hue'], PAINT['min_saturation'],
                            main_max_saturation=PAINT['main_max_saturation'], trim=1, paint=BODY_PAINT, line=LINES)
    files += result['files']
    mapping = Mapping(prescale, result['transform'])
    print(f'body: {result["width_cells"]:.2f} cells wide; Def colours {result["colours"]}', flush=True)

    for name, source in OVERLAYS.items():
        files += ev.write_facings(overlay_on_body(source, mapping), out, f'{NAME}_{name}')
    for name in DAMAGE:   # Damage looks: near-black marks with alpha, shown by the Def's damageOverlays as a part wears
        files += ev.write_facings(overlay_on_body(DERIVED / f'damage-{name}.png', mapping, trim=False), out, f'{NAME}_Damage{name}')
    layout = {'body': {'cells_long': CELLS_LONG, 'draw_size': list(DRAW), 'colours': result['colours'], 'pick_to_texture': mapping.total},
              'turrets': {}, 'hubs': {}, 'nacelles': {}}
    for name, source in TURRETS.items():
        picture, mount = turret(source, mapping)
        path = out / f'{NAME}_{name}.png'
        picture.save(path, optimize=True)
        files.append(path)
        drawn_at = mapping.cells(*mount)
        # On the centre line, as the real ramp gun is: the model drew the .50 a little right of it on the hatch.
        layout['turrets'][name] = {'mount_cells': [0.0, drawn_at[1]], 'drawn_at_cells': drawn_at, 'draw_size': [TURRET_CELLS, TURRET_CELLS],
                                   'drawn_facing': 'north (barrel up); the Def turns it to face aft'}

    rotor = Image.open(DERIVED / 'rotor.png')
    rotor_reach = json.loads((DERIVED / 'rotor.json').read_text(encoding='utf-8'))
    tip_px = rotor.width / 2 - 16                  # derive_osprey_parts sizes the canvas to the farthest tip plus two grid steps
    rotor_factor = ROTOR_METRES / 2 / METRES_PER_CELL * ev.PIXELS_PER_CELL / tip_px
    rotor_cells = int(ROTOR_METRES / METRES_PER_CELL) + 1
    for name, source in (('Rotor', DERIVED / 'rotor.png'), ('RotorSpin', DERIVED / 'rotor-spin.png')):
        path = out / f'{NAME}_{name}.png'
        renderer_piece(source, rotor_factor, rotor_cells).save(path, optimize=True)
        files.append(path)
    layout['rotor'] = {'diameter_cells': round(ROTOR_METRES / METRES_PER_CELL, 2), 'draw_size': [rotor_cells, rotor_cells],
                       'hub_found_at_in_its_picture': rotor_reach['hub_found_at']}

    parts = json.loads((art_paths.find('parts', sha(PICK)) / 'parts.json').read_text(encoding='utf-8'))
    by_name = {part['name']: part for part in parts['parts']}
    for side in ('Left', 'Right'):
        hub = by_name[f'{side}Hub']['centre']
        nacelle = by_name[f'{side}Nacelle']['box']
        layout['hubs'][side] = mapping.cells(*hub)
        layout['nacelles'][side] = {'rear_pivot_cells': mapping.cells((nacelle[0] + nacelle[2]) / 2, nacelle[3]),
                                    'drawn': 'the left nacelle' + (' mirrored' if side == 'Right' else '')}
    for angle in (0, 30, 60, 90):
        for suffix in ('', '-barrier'):
            source = DERIVED / f'nacelle-{angle}{suffix}.png'
            piece = renderer_piece(source, mapping.total)
            texture, mask, _ = ev.separate_paint(piece, PAINT['main_hue'], PAINT['accent_hue'], PAINT['min_saturation'],
                                                 main_max_saturation=PAINT['main_max_saturation'], line=LINES)
            if suffix:   # The grille is bare metal: no paint where the barrier is (it took the body colour, seen 19 Nov 2026).
                grille = renderer_piece(DERIVED / f'nacelle-{angle}-barrier-only.png', mapping.total).getchannel('A')
                grille = grille.point(lambda a: 255 if a >= 32 else 0).filter(ImageFilter.MaxFilter(3))
                mask.paste((0, 0, 0, 255), (0, 0), grille)
                texture.paste(piece, (0, 0), grille)   # Its drawn colours back: the paint step had lightened them for the shader
            stem = f'{NAME}_Nacelle{angle}{"Barrier" if suffix else ""}'
            texture.save(out / f'{stem}.png', optimize=True)
            mask.save(out / f'{stem}m.png', optimize=True)
            files += [out / f'{stem}.png', out / f'{stem}m.png']
        for side in ('Left', 'Right'):   # Damage over the nacelle, tilting with it; the right is drawn as the left, then mirrored
            path = out / f'{NAME}_Nacelle{angle}Damage{side}.png'
            renderer_piece(DERIVED / f'nacelle-{angle}-damage-{side.lower()}.png', mapping.total).save(path, optimize=True)
            files.append(path)
    layout['nacelles']['draw_px_per_cell'] = ev.PIXELS_PER_CELL
    (out / f'{NAME}.layout.json').write_text(json.dumps(layout, indent=2), encoding='utf-8')
    record = {'tool': 'assets/phobos-vehicles/export_osprey.py', 'pick': PICK.relative_to(ROOT).as_posix(), 'pick_sha256': sha(PICK),
              'prescale': prescale, 'paint': PAINT, 'files': {p.name: sha(p) for p in files}}
    (out / f'{NAME}.osprey-export.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    review(out, work)
    print(f'{len(files)} textures in {out}; review: {work / "review" / "index.html"}')
    return 0


def review(out, work):
    """Tinted with a sample livery on dark ground, every facing, every overlay on the body, the turrets on their mounts."""
    page = work / 'review'
    if page.exists():
        shutil.rmtree(page)
    page.mkdir(parents=True)
    cards = []

    def card(title, note, image, name):
        image.save(page / name)
        cards.append(f'<div class="card"><div class="name">{html.escape(title)}</div><p class="meta">{html.escape(note)}</p>'
                     f'<a href="{name}"><img src="{name}"></a></div>')

    for facing in ('north', 'east', 'south'):
        texture = Image.open(out / f'{NAME}_{facing}.png')
        mask = Image.open(out / f'{NAME}_{facing}m.png')
        card(f'Body, {facing}', 'Tinted with a sample red and yellow livery, as the paint shader would', on_dark(tinted(texture, mask)),
             f'body-{facing}.png')
        if facing == 'north':
            card('Body, as drawn', 'The texture before paint: the painted areas are grey for the shader', on_dark(texture), 'body-plain.png')
            card('Paint mask', 'Red: the main paint. Green: the accent. Black: never painted (glass, outline, dark metal)',
                 mask.convert('RGB'), 'mask.png')
    north = tinted(Image.open(out / f'{NAME}_north.png'), Image.open(out / f'{NAME}_northm.png'))
    for name in OVERLAYS:
        stacked = north.copy()
        stacked.alpha_composite(Image.open(out / f'{NAME}_{name}_north.png'))
        card(f'{name} on the body', 'North facing, at game size', on_dark(stacked), f'overlay-{name}.png')
    damaged = north.copy()
    for name in DAMAGE:
        damaged.alpha_composite(Image.open(out / f'{NAME}_Damage{name}_north.png'))
    card('Damage looks, all at once', 'Cockpit, both cabins, both wings and the tail; in game each shows as its own part wears',
         on_dark(damaged), 'damage-all.png')
    for name in TURRETS:
        card(f'Turret {name}', 'Centred on its mount, barrel up; the Def turns it aft', on_dark(Image.open(out / f'{NAME}_{name}.png')),
             f'turret-{name}.png')
    for name in ('Rotor', 'RotorSpin'):
        card(name, 'Centred on its hub; our renderer spins and tilts it', on_dark(Image.open(out / f'{NAME}_{name}.png')), f'{name}.png')
    row = []
    for angle in (0, 30, 60, 90):
        for suffix in ('', 'Barrier'):
            stem = f'{NAME}_Nacelle{angle}{suffix}'
            row.append(tinted(Image.open(out / f'{stem}.png'), Image.open(out / f'{stem}m.png')))
        damaged_nacelle = row[-2].copy()
        damaged_nacelle.alpha_composite(Image.open(out / f'{NAME}_Nacelle{angle}DamageLeft.png'))
        row.append(damaged_nacelle)
    sheet = Image.new('RGBA', (sum(i.width for i in row) + 20 * (len(row) + 1), max(i.height for i in row) + 40), (38, 40, 34, 255))
    x = 20
    for image in row:
        sheet.alpha_composite(image, (x, 20))
        x += image.width + 20
    card('Nacelles, tinted', '0, 30, 60 and 90 degrees: plain, with the inlet barrier, and damaged', sheet.convert('RGB'), 'nacelles.png')
    (page / 'index.html').write_text(f'''<!doctype html><html><head><meta charset="utf-8"><title>Osprey export</title><style>
:root {{ --bg:#171814; --panel:#20221c; --line:#3a3d33; --fg:#e6e1d3; --muted:#a8a392; --accent:#d6b25e; }}
body {{ background:var(--bg); color:var(--fg); font-family:"Atkinson Hyperlegible","Segoe UI",Verdana,sans-serif; margin:0; padding:24px; line-height:1.5; }}
h1 {{ font-size:1.8rem; margin:0 0 4px; }} .meta {{ color:var(--muted); font-size:0.95rem; margin:6px 0; }}
.grid {{ display:grid; grid-template-columns:repeat(auto-fill, minmax(420px, 1fr)); gap:16px; }}
.card {{ background:var(--panel); border:1px solid var(--line); padding:12px; }} .card img {{ width:100%; height:auto; display:block; }}
.name {{ font-weight:bold; font-size:1.2rem; color:var(--accent); }}
</style></head><body><h1>V-22 Osprey: exported at game size</h1>
<p class="meta">128 px a cell. The livery is a sample to show where paint goes; in game it is the player's. Click any picture for full size.
Nothing is in the mod yet.</p><div class="grid">{"".join(cards)}</div></body></html>''', encoding='utf-8')


if __name__ == '__main__':
    sys.exit(main())
