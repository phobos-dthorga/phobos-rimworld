"""The Osprey's part-only references for the third round of overlays: pieces of the owner's pick, never the whole aircraft.

    python assets/phobos-vehicles/cut_osprey_references.py

Learnt 18 Nov 2026 (requests/osprey-overlays-record.md): with the whole aircraft in its references, the editing model redraws the
whole aircraft, and lifting a part back out of that redraw mangles it. Given only a piece of the body (the ramp, one nacelle), it
has no aircraft to redraw: it adds the part to that piece, and the piece is laid back at its known place. Writes into references/
(ignored on main; this script draws them again), each with its place in the pick's pixels in osprey-crops.json.
"""
import hashlib
import json
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
PICK = HERE / 'generated/osprey-trial-nano-banana-pro/osprey-trial-nano-banana-pro-1.png'
ROTOR = HERE / 'generated/osprey-derived/rotor.png'
OUT = HERE / 'references'
# Places in the pick's 4,096 px, 2:3 so the service's 2:3 shape fits them exactly (it has no 3:5).
CROPS = {
    'osprey-crop-ramp': (1800, 2900, 2400, 3800),      # The rear fuselage, ramp hatch and tailplane: the ramp gun and the hoist.
    'osprey-crop-nacelle': (183, 1060, 983, 2260),     # The left nacelle and the wing root beside it: the inlet barrier.
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    OUT.mkdir(exist_ok=True)
    pick = Image.open(PICK).convert('RGB')
    record = {'pick': PICK.relative_to(HERE.parents[1]).as_posix(), 'pick_sha256': sha(PICK), 'crops': {}}
    for name, box in CROPS.items():
        pick.crop(box).save(OUT / f'{name}.png')
        record['crops'][name] = {'box': list(box), 'sha256': sha(OUT / f'{name}.png')}
    # The rotor rebuilt by script: its shape and layout are right, its outlines broken. On white, for the model to draw cleanly.
    rotor = Image.open(ROTOR).convert('RGBA')
    ground = Image.new('RGBA', rotor.size, (255, 255, 255, 255))
    ground.alpha_composite(rotor)
    ground.convert('RGB').save(OUT / 'osprey-rotor-white.png')
    record['rotor'] = {'from': ROTOR.relative_to(HERE.parents[1]).as_posix(), 'sha256': sha(OUT / 'osprey-rotor-white.png')}
    (OUT / 'osprey-crops.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
    for name in [*CROPS, 'osprey-rotor-white']:
        print(OUT / f'{name}.png')


if __name__ == '__main__':
    main()
