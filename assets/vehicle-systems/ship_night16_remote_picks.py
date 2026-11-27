"""Ship the owner's picks of 27 Nov 2026 from night 16's remote, route and round-trip icons (local), into D'thorga Framework and
D'thorga Radio.

    python assets/vehicle-systems/ship_night16_remote_picks.py

Each pick ships where its request's art_plan.ships_as says, exported by scripts/art/export_sprite.py at 128 px (the background AI
had looked at every try when its card was drawn), with its record under assets/records. Round waits is the one exception: the owner
picked try 162 and chose, by card, a pale clock face with dark hands, so the pin and the clock read apart at actual size. That
recolour is made here on a copy of the master (the original stays byte for byte), and the background AI looks at the copy before it
is exported. The six the owner sent to the paid route (mech link and unlink, round stop, the optionally piloted kit, pop smoke and
the ammo locker) are not here: they are drawn again on the paid route and shipped when picked.
"""
import hashlib
import json
import subprocess
import sys
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

GENERATED = ROOT / 'assets' / 'vehicle-systems' / 'generated'
REQUEST = 'assets/vehicle-systems/requests/night-16-remote-icons.json'

# request name, the owner's pick (Picks page, 27 Nov 2026), what it is
PICKS = [
    ('n16-route-follow', '162', "the route's Follow button"),
    ('n16-route-loop', '162', "the route's Loop button"),
    ('n16-route-waypoint', '161', "the route's Add a waypoint button"),
    ('n16-route-stop', '162', "the route's Stop button (and a running supply run's)"),
    ('n16-route-break-up', '161', "the route's Break up button"),
    ('n16-remote-station', '161', "the remote-control Drive from the station button"),
    ('n16-remote-lost-link', '162', "the remote-control If the link is lost button"),
    ('n16-round-plan', '162', "the round trip's Plan a round button"),
    ('n16-round-fly', '162', "the round trip's Fly the round button"),
    ('n16-round-waits', '162', "the round trip's Waits at each stop button"),
]
RECOLOURED = {'n16-round-waits'}
CREAM = np.array([238.0, 232.0, 214.0])


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def pale_clock(master: Path, out: Path):
    """Round waits 162 with its clock face cream and its hands dark (the owner's card, 27 Nov 2026). The face is flood-filled from a
    point inside it, stopping at the black outlines and the hands; its anti-aliased edge toward the outline is carried along the
    face-to-black line, so no yellow fringe is left; the hands' pale cores are darkened."""
    im = np.asarray(Image.open(master).convert('RGB')).astype(np.float64)
    h, w, _ = im.shape
    seed = (330, 720)  # y, x: inside the clock face of try 162
    face = im[seed]
    inside = np.sqrt(((im - face) ** 2).sum(axis=2)) < 40
    mask = np.zeros((h, w), bool)
    queue = deque([seed])
    mask[seed] = True
    while queue:
        y, x = queue.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and not mask[ny, nx] and inside[ny, nx]:
                mask[ny, nx] = True
                queue.append((ny, nx))
    grown = mask.copy()
    for _ in range(4):
        step = grown.copy()
        step[1:] |= grown[:-1]
        step[:-1] |= grown[1:]
        step[:, 1:] |= grown[:, :-1]
        step[:, :-1] |= grown[:, 1:]
        grown = step
    t = (im @ face) / (face @ face)
    off = np.sqrt(((im - t[..., None] * face) ** 2).sum(axis=2))
    fringe = grown & ~mask & (t > 0.05) & (t < 1.05) & (off < 30)
    out_im = im.copy()
    out_im[mask] = np.clip(CREAM * (im.mean(axis=2) / face.mean())[mask][:, None], 0, 255)
    out_im[fringe] = np.clip(t[fringe][:, None] * CREAM, 0, 255)
    yy, xx = np.mgrid[0:h, 0:w]
    disk = (yy - 430) ** 2 + (xx - 640) ** 2 < 175 ** 2
    core = disk & (im.min(axis=2) > 120) & (im.max(axis=2) - im.min(axis=2) < 45)
    out_im[core] = np.clip(im[core] * 0.15, 0, 255)
    Image.fromarray(out_im.astype(np.uint8)).save(out)


def main():
    entries = {e['name']: e for e in json.loads((ROOT / REQUEST).read_text(encoding='utf-8'))}
    masters = {}
    for name, pick, _ in PICKS:
        master = GENERATED / name / f'{name}-{pick}.png'
        if name in RECOLOURED:
            derived = master.with_name(f'{name}-{pick}-paleclock.png')
            pale_clock(master, derived)
            masters[name] = (master, derived)
        else:
            masters[name] = (master, master)
    derived = [str(d) for m, d in masters.values() if d != m]
    if derived:
        cmd = [sys.executable, str(ROOT / 'scripts' / 'art' / 'clean_backgrounds.py'), 'masks', '--cpu', '--stop-at', '23:59']
        for path in derived:
            cmd += ['--also', path]
        subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL)
    for name, pick, what in PICKS:
        original, source = masters[name]
        target = ROOT / entries[name]['art_plan']['ships_as']
        target.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'export_sprite.py'), str(source), str(target), '--size', '128'],
                       check=True, stdout=subprocess.DEVNULL)
        base = target.with_suffix('')
        made = ('Made on 26 to 27 Nov 2026 on the owner\'s own graphics card with FLUX.2 Klein 4B base (klein-base, Apache 2.0) '
                'through `scripts/art-run.py` (night 16), with our private style model (`phobos-private-vanilla`, trained on the '
                f'game\'s sprites; never published): **AI-generated**, {what}. Picked by the owner on the Picks page (seed {pick}). '
                f'The request is `{REQUEST}`; its references were our own approved icons, for style only.')
        if source != original:
            made += (' Recoloured by script at the owner\'s choice (27 Nov 2026): the clock face cream and its hands dark, so the pin '
                     'and the clock read apart; the recolour is a new file, the master is unchanged.')
        lines = [f'# {base.name}', '',
                 made + ' Exported by `scripts/art/export_sprite.py` at 128 px (1:1) after the background AI had looked at it, by '
                 '`assets/vehicle-systems/ship_night16_remote_picks.py`. The master and its record go to the `art-masters` branch, '
                 'the other attempts to `art-references`.',
                 '',
                 f'- Master: `{name}-{pick}`, SHA-256 `{sha(original)}`']
        if source != original:
            lines.append(f'- Recoloured copy: `{source.name}`, SHA-256 `{sha(source)}`')
        lines.append(f'- `{target.name}`: SHA-256 `{sha(target)}` (see its export.json)')
        art_paths.record_path(base.with_name(base.name + '.record.md')).write_text('\n'.join(lines) + '\n', encoding='utf-8')
        print('shipped', target.relative_to(ROOT).as_posix())


if __name__ == '__main__':
    main()
