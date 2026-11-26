"""Ship the owner's picks of the airdrop's paid pictures of 25 Nov 2026 (assets/vehicle-systems/requests/airdrop-paid.json), reproducibly.

    python assets/vehicle-systems/ship_airdrop.py                          the answers on D'thorga Companion's Picks page
    python assets/vehicle-systems/ship_airdrop.py --pick airdrop-canopy=2  a pick given by hand (repeatable; overrides the page)
    python assets/vehicle-systems/ship_airdrop.py --dry-run                say what would ship, writing nothing

The same day's second batch (the guided cargo parachute and the ankle braces, `airdrop-guided-chute` and `airdrop-ankle-braces`) is in
the same request file and ships the same way, as does the third, of 26 Nov 2026 (the drop zone marker, `airdrop-dz-marker`: the
panel with its smoke pot and lamp, tries 3 and 4, which replaced the panel-only tries 1 and 2 the same day).

Each pick is looked at by the background AI on the processor (safe beside the game), exported by scripts/art/export_sprite.py at its
game size, and given its record under assets/records. The rigger's table is drawn north (the rigger at the bottom edge, as the game
draws a bench whose interaction cell is in front) and turned by script for east (90 degrees clockwise), south and west (90 degrees
anticlockwise), so all four facings register; a 4 x 1 bench drawn at (4.5, 1.5) as our parts bench (3 x 1) is drawn at (3.5, 1.5).
Its Def uses CutoutComplex, so each facing also gets a colour mask (`_northm` and the rest, red where the stuff tints): the frame and the
wooden top only, never the canopy and gear laid on it (the coordinator's note of 25 Nov 2026), drawn by `table_mask` and turned alike.
`--mask-preview` writes each table try's mask and the try tinted as wood and as steel to the art working folder, writing no mod.
A pick answered "none" is skipped and named. Afterwards, by hand: archive (pick to art-masters, the other try to art-references, with
scripts/art/archive_branch.py), then `python -m phobos_modkit picks settle --root . --id <pick id>` in the same commit as the texture.
"""
import argparse
import hashlib
import subprocess
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402
import art_picks  # noqa: E402

REQUEST = 'assets/vehicle-systems/requests/airdrop-paid.json'
GENERATED = ROOT / 'assets' / 'vehicle-systems' / 'generated'
AIRDROP = 'mods/PhobosFramework/Mods/VehicleFramework/Common/Textures/Phobos/VehicleSystems/Airdrop'

# pick id: (target without extension, pixels on the shorter side, aspect, four facings, what it is)
PICKS = {
    'airdrop-canopy': (f'{AIRDROP}/CanopyOpen', 512, '1:1', False,
                       'the open canopy drawn over a falling person, bundle or vehicle (three for a heavy drop), scaled in code'),
    'airdrop-bundle': (f'{AIRDROP}/CargoBundle', 256, '1:1', False, 'a cargo bundle falling under its canopy, seen from above'),
    'airdrop-platform': (f'{AIRDROP}/DropPlatform', 256, '1:2', False,
                         'the heavy-drop platform drawn beneath a falling vehicle, scaled in code to the vehicle (front at the top)'),
    'airdrop-spent-canopy': (f'{AIRDROP}/SpentParachute', 128, '1:1', False, 'the spent parachute item left where a canopy landed'),
    'airdrop-personnel-chute': (f'{AIRDROP}/PersonnelParachute', 128, '1:1', False, 'the packed personnel parachute item'),
    'airdrop-cargo-chute': (f'{AIRDROP}/CargoParachute', 128, '1:1', False, 'the packed cargo parachute item'),
    'airdrop-heavy-rig': (f'{AIRDROP}/HeavyDropRig', 128, '1:1', False, 'the heavy-drop rig item, one per vehicle dropped'),
    'airdrop-spent-platform': (f'{AIRDROP}/SpentPlatform', 128, '1:1', False, 'the spent drop platform item left where a vehicle landed'),
    'airdrop-riggers-table': (f'{AIRDROP}/RiggersTable', 192, '3:1', True,
                              "the rigger's table, a 4 x 1 work table drawn at (4.5, 1.5), Graphic_Multi with four facings"),
    # The second batch of the same day (run 20261125-1034, the owner's card: two pictures, two tries each), same request file.
    'airdrop-guided-chute': (f'{AIRDROP}/GuidedCargoParachute', 128, '1:1', False,
                             'the packed guided cargo parachute item (a square canopy in its bag with a small guidance unit)'),
    'airdrop-ankle-braces': (f'{AIRDROP}/AnkleBraces', 128, '1:1', False, 'the parachute ankle braces item, as it lies in a stockpile'),
    # The third batch (26 Nov 2026, the owner's card "Aim point and less scatter": one picture, two tries, cap US$0.15), same file.
    # Its tries 1 and 2 (the panel alone) were replaced the same day by tries 3 and 4 (the owner's card "Panel + smoke pot + lamp"):
    # the waiting pick offers 3 and 4, so `--pick airdrop-dz-marker=3` or `=4`.
    'airdrop-dz-marker': (f'{AIRDROP}/DropZoneMarker', 256, '1:1', False,
                          "the drop zone marker, a pathfinder's signal panel staked flat on the ground with a violet smoke pot "
                          'and a battery beacon lamp beside it, a building drawn at (1.6, 1.6)'),
}
# The day each pick's tries were made, where it is not the first batch's 25 Nov 2026.
MADE = {'airdrop-dz-marker': '26 Nov 2026'}
# The table's facings from its north picture: (facing, turn). PIL's ROTATE_270 turns clockwise.
TURNS = (('east', Image.Transpose.ROTATE_270), ('south', Image.Transpose.ROTATE_180), ('west', Image.Transpose.ROTATE_90))

# The table's colour mask (CutoutComplex, the Def's stuff tints red): the frame and the wooden top only, never what lies on the top.
# Authored thresholds, read off both tries of 25 Nov 2026: steel and outlines are grey or dark (saturation under STEEL_SAT, or no
# channel over DARK); wood is within WOOD_TOL a channel of the top's own colour; a patch under SPECK pixels (grain, a dot) is wood.
STEEL_SAT = 0.14
DARK = 70
WOOD_TOL = 46
SPECK = 40
# How far each table try's things are closed, read off its mask preview (25 Nov 2026): try 1's middle bag leaks at 1; try 2's
# pack, canopy and tool roll sit so close that 2 walls in strips of the top.
CLOSE = {'1': 2, '2': 1}


def table_mask(sprite, close=2):
    """Red where the stuff tints: the steel frame and the wooden top. The frame is the band outside the top's inner rectangle, measured
    on each side from the silhouette's edge to the first warm pixel. Inside it, whatever is not the top's colour is a thing lying on the
    table (canopy, lines, pack, clamp, paddle, tools); a wood-coloured patch walled in by such a thing and thicker than a few pixels (the
    khaki shot bags on the canopy) goes with it, while the thin gaps between lines stay with the top. Returns an opaque RGBA mask the
    size of the sprite, black where nothing tints."""
    import numpy as np
    from scipy import ndimage

    rgba = np.asarray(sprite.convert('RGBA')).astype(np.int16)
    rgb, alpha = rgba[..., :3], rgba[..., 3] > 0
    top, bottom = rgb.max(-1), rgb.min(-1)
    sat = (top - bottom) / np.maximum(top, 1)
    warm = alpha & (sat >= STEEL_SAT) & (top >= DARK)
    ys, xs = np.nonzero(alpha)
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()

    def depth(lines):
        """The median distance from the edge to the first warm pixel, over the middle half of the side."""
        found = [int(np.argmax(line)) for line in lines if line.any()]
        if not found:
            raise SystemExit('no wooden top found inside the frame: look at the try before shipping')
        return int(np.median(found))

    mid_x = slice(x0 + (x1 - x0) // 4, x1 - (x1 - x0) // 4)
    mid_y = slice(y0 + (y1 - y0) // 4, y1 - (y1 - y0) // 4)
    t = depth(warm[y0:, mid_x].T)
    b = depth(warm[y1::-1, mid_x].T)
    left = depth(warm[mid_y, x0:])
    right = depth(warm[mid_y, x1::-1])
    inner = np.zeros_like(alpha)
    inner[y0 + t:y1 - b + 1, x0 + left:x1 - right + 1] = True
    inner &= alpha
    rim = inner & ~ndimage.binary_erosion(inner, iterations=3) & warm
    wood_ref = np.median(rgb[rim], axis=0)
    woodish = (np.abs(rgb - wood_ref) <= WOOD_TOL).all(-1) & (top >= DARK)
    things = inner & ~woodish
    parts, count = ndimage.label(things, structure=np.ones((3, 3)))
    if count:
        sizes = ndimage.sum(np.ones_like(parts), parts, index=range(1, count + 1))
        things = np.isin(parts, [i + 1 for i, s in enumerate(sizes) if s >= SPECK])
    # Each thing closed by `close` pixels and filled solid: the khaki bags are the wood's colour (try 1: 129,113,75 against 145,121,90),
    # so only their place on the canopy says they are not the top; a thin outline gap no longer lets the tint in.
    things = ndimage.binary_fill_holes(ndimage.binary_closing(things, structure=np.ones((3, 3)), iterations=close)) & inner
    # Only tint joined to the frame: an island left inside a thing is a speck, never shipped (the paint-mask rule).
    tint = alpha & ~things
    parts, _ = ndimage.label(tint)
    tint = np.isin(parts, np.unique(parts[alpha & ~inner])) & (parts > 0)
    out = np.zeros(rgba.shape, dtype=np.uint8)
    out[..., 3] = 255
    out[..., 0] = np.where(tint, 255, 0)
    return Image.fromarray(out, 'RGBA')


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def answers():
    """The owner's answers on the Picks page, pick id to the chosen try's id ('none' included)."""
    _, picks = art_picks._picks().listed(ROOT)
    return {p['id']: str(p['answer']['pick']) for p in picks if p['id'] in PICKS and p.get('answer')}


def export(master, target, size, aspect):
    target.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(master), str(target), '--size', str(size),
                    '--aspect', aspect], check=True, stdout=subprocess.DEVNULL)


def ship(pick_id, attempt):
    target, size, aspect, facings, what = PICKS[pick_id]
    master = GENERATED / pick_id / f'{pick_id}-{attempt}.png'
    base = ROOT / target
    if facings:
        north = base.with_name(f'{base.name}_north.png')
        export(master, north, size, aspect)
        picture = Image.open(north)
        mask = table_mask(picture, CLOSE.get(str(attempt), 2))
        mask.save(base.with_name(f'{base.name}_northm.png'))
        for facing, turn in TURNS:
            picture.transpose(turn).save(base.with_name(f'{base.name}_{facing}.png'))
            mask.transpose(turn).save(base.with_name(f'{base.name}_{facing}m.png'))
        files = [north] + [base.with_name(f'{base.name}_{facing}.png') for facing, _ in TURNS]
        files += [base.with_name(f'{base.name}_{facing}m.png') for facing in ('north', 'east', 'south', 'west')]
    else:
        files = [base.with_suffix('.png')]
        export(master, files[0], size, aspect)
    w, h = Image.open(files[0]).size
    lines = [f'# {base.name}', '',
             f'Made on {MADE.get(pick_id, "25 Nov 2026")} with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, {what}. '
             f'Picked by the owner from two attempts (attempt {attempt}) on the Picks page (pick `{pick_id}`). Exported by '
             f'`scripts/art/export_sprite.py` at {w} x {h} ({aspect}) after the background AI had looked at the master, by '
             f'`assets/vehicle-systems/ship_airdrop.py`. The prompt and its references are in `{REQUEST}`; the master and its record '
             'go to the `art-masters` branch, the other attempt to `art-references`. The only references uploaded were our own approved '
             "art, for style only. No game or other author's art was uploaded or used."]
    if facings:
        lines.append('The north facing is the master, the rigger standing at its bottom edge; east, south and west are it turned by '
                     'script (90 degrees clockwise, 180 degrees and 90 degrees anticlockwise), so the four facings register exactly. '
                     'The colour masks (`_northm` and the rest, red where the stuff tints, for CutoutComplex) are drawn by script '
                     '(`table_mask` in the ship script): the steel frame and the wooden top only, never the canopy, pack, paddle, '
                     'tools or bags on it, and turned with their facings.')
    lines += ['', f'- Master: `{master.name}`, SHA-256 `{sha(master)}`']
    lines += [f'- `{f.name}`: SHA-256 `{sha(f)}`' + (' (see its export.json)' if i == 0 else '') for i, f in enumerate(files)]
    record = art_paths.record_path(base.with_name(base.name + '.record.md'))
    record.write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print('shipped', pick_id, 'try', attempt, '->', target, f'({len(files)} file(s))')


# Stuff colours for the mask preview, approximately the game's wood and steel (authored, for a look only).
PREVIEW_TINTS = (('wood', (0.52, 0.38, 0.26)), ('steel', (0.62, 0.62, 0.64)))


def tinted(sprite, mask, colour):
    """The sprite as CutoutComplex draws it, roughly: multiplied by the stuff colour where the mask is red."""
    import numpy as np

    rgba = np.asarray(sprite.convert('RGBA')).astype(np.float32)
    weight = np.asarray(mask)[..., 0:1].astype(np.float32) / 255
    rgba[..., :3] *= (1 - weight) + weight * np.array(colour, dtype=np.float32)
    return Image.fromarray(rgba.clip(0, 255).astype(np.uint8), 'RGBA')


def mask_preview():
    """Every table try's export (from its review card), its mask, and it tinted as wood and as steel, stacked on soil at 2x."""
    folder = art_paths.work('cards', Path(REQUEST).stem, 'export')
    out = art_paths.work('cards', Path(REQUEST).stem)
    for path in sorted(folder.glob('airdrop-riggers-table-*.png')):
        sprite = Image.open(path).convert('RGBA')
        mask = table_mask(sprite, CLOSE.get(path.stem.rsplit('-', 1)[-1], 2))
        rows = [sprite, mask, *(tinted(sprite, mask, c) for _, c in PREVIEW_TINTS)]
        w, h = sprite.size
        card = Image.new('RGBA', (w, (h + 8) * len(rows)), (92, 76, 58, 255))
        for i, row in enumerate(rows):
            card.alpha_composite(row, (0, i * (h + 8)))
        card = card.resize((card.width * 2, card.height * 2), Image.Resampling.NEAREST)
        target = out / f'mask-{path.stem}.png'
        card.convert('RGB').save(target)
        print(target)


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--pick', action='append', default=[], metavar='ID=TRY', help='a pick by hand, e.g. airdrop-canopy=2')
    parser.add_argument('--dry-run', action='store_true', help='say what would ship, writing nothing')
    parser.add_argument('--mask-preview', action='store_true', help="the table tries' masks and tints, to look at; writes no mod")
    args = parser.parse_args()
    if args.mask_preview:
        mask_preview()
        return
    chosen = answers()
    for given in args.pick:
        pick_id, _, attempt = given.partition('=')
        if pick_id not in PICKS or not attempt:
            parser.error(f'unknown pick {given!r}: one of {", ".join(PICKS)}, as ID=TRY')
        chosen[pick_id] = attempt
    waiting = [p for p in PICKS if p not in chosen]
    nones = [p for p, a in chosen.items() if a == 'none']
    todo = {p: a for p, a in chosen.items() if a != 'none'}
    missing = [p for p, a in todo.items() if not (GENERATED / p / f'{p}-{a}.png').is_file()]
    if missing:
        sys.exit('no such try: ' + ', '.join(f'{p}-{todo[p]}' for p in missing))
    if todo and not args.dry_run:
        # The background AI looks at each master first, on the processor (beside the game): the exporter reads its masks.
        cmd = [sys.executable, str(ROOT / 'scripts' / 'art' / 'clean_backgrounds.py'), 'masks', '--cpu', '--stop-at', '23:59']
        for p, a in todo.items():
            cmd += ['--also', str(GENERATED / p / f'{p}-{a}.png')]
        subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)
    for pick_id, attempt in todo.items():
        if args.dry_run:
            print('would ship', pick_id, 'try', attempt, '->', PICKS[pick_id][0])
        else:
            ship(pick_id, attempt)
    if nones:
        print('answered none (a new route is owed, nothing shipped):', ', '.join(nones))
    if waiting:
        print('still waiting for the owner:', ', '.join(waiting))


if __name__ == '__main__':
    main()
