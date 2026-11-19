"""Cut the pieces of each service truck's picked body that its drawn armour kits change, for the local editing model.

    <ComfyUI's python> assets/phobos-vehicles/draw_kit_crops.py

The drawn kits (docs/development/vehicle-militarised-service-vehicles.md, "The kits"; step 3, 19 Nov 2026) sit on the cab and along
the cargo bed. Each is made by editing a piece of the owner's picked body and lifting the added part out with
scripts/art/edit_difference.py, as the Osprey's overlays were (18 Nov 2026), so it lines up with the body by construction. This
script cuts those pieces: the cab with the front bumper (armoured cab, heavy cab plates, ram bar) and, where a truck has body plates,
one flank of the bed. Pieces go to assets/phobos-vehicles/references/, which is ignored on main; run this again to redraw them. Each
box is in shares of the body (0 to 1), widened to whole multiples of 16 pixels, as the image model needs. Needs Pillow.
"""
import hashlib
import json
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
GENERATED = HERE / 'generated'
OUT = HERE / 'references'

# truck: (picked body, its pieces as name: (x0, y0, x1, y1) in shares of the body). Boxes read off the bodies by the agent, 19 Nov 2026;
# each cab is an exact 4:3, 16:9 or 3:2 picture, the shapes the paid editing models take (1024x768, 768x576, 1024x576, 768x512).
BODIES = {
    'tipper': ('tipper-body-north/tipper-body-north-3.png', {
        'cab': (0.0, 0.0, 1.0, 0.375),
        'bed': (0.0, 0.30, 1.0, 1.0),
    }),
    'tanker': ('bulk-tanker-body-north/bulk-tanker-body-north-2.png', {
        'cab': (0.0, 0.0, 1.0, 0.375),
        'bed': (0.0, 0.32, 1.0, 1.0),
    }),
    'drill-rig': ('drill-rig-body-north/drill-rig-body-north-2.png', {
        'cab': (0.0, 0.0, 1.0, 0.25),
        'bed': (0.0, 0.22, 1.0, 1.0),
    }),
    'survey-truck': ('survey-truck-body-north/survey-truck-body-north-1.png', {
        'cab': (0.0, 0.0, 1.0, 0.375),
    }),
    'generator-set': ('generator-set-body-north/generator-set-body-north-3.png', {
        'cab': (0.0, 0.0, 1.0, 1 / 3),
    }),
}


def snap(value, limit):
    """Down to a multiple of 16, within the picture."""
    return max(0, min(limit, int(value) // 16 * 16))


def main():
    OUT.mkdir(exist_ok=True)
    manifest = {}
    for truck, (source, pieces) in BODIES.items():
        path = GENERATED / source
        body = Image.open(path).convert('RGB')
        w, h = body.size
        for piece, (x0, y0, x1, y1) in pieces.items():
            box = (snap(x0 * w, w), snap(y0 * h, h), snap(x1 * w + 15, w), snap(y1 * h + 15, h))
            crop = body.crop(box)
            name = f'kit-crop-{truck}-{piece}.png'
            crop.save(OUT / name)
            manifest[name] = {'body': f'assets/phobos-vehicles/generated/{source}',
                              'body_sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                              'box_pixels': list(box), 'size': list(crop.size)}
            print(name, crop.size, box)
    (OUT / 'kit-crops.json').write_text(json.dumps(manifest, indent=1), encoding='utf-8')


if __name__ == '__main__':
    main()
