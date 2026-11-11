"""Damage pictures for the generator set, drill rig, tipper, survey truck and Seabed Crawler, made free from the bulk tanker's own damage
masters (our art, generated 8 Nov 2026): a different crop of the marks for each place, fitted there with export_vehicle.py overlay
--marks (darkness only), then clipped to the vehicle's own outline so no mark hangs over empty ground. Crops are new files; the masters
are never changed.

    python assets/phobos-vehicles/export_damage.py
"""
import json
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageChops

GEN = Path('assets/phobos-vehicles/generated')
DERIVED = GEN / 'derived-2026-11-12'
EXPORT = GEN / 'export'
MOD = Path('mods/PhobosVehicles/Textures/Phobos/Vehicles')
BODY_MARKS = GEN / 'bulk-tanker-damage-body/bulk-tanker-damage-body-1.png'
TANK_MARKS = GEN / 'bulk-tanker-damage-tank/bulk-tanker-damage-tank-1.png'
FACINGS = ('north', 'east', 'south')

# (vehicle, texture, marks master, crop of it (left, top, right, bottom), fit box in the body master, textures whose outline clips it)
# Agent placements, 12 Nov 2026, for the owner to revise.
DAMAGE = [
    ('GeneratorSet', 'GeneratorSet_DamageCab', BODY_MARKS, (0, 0, 768, 512), (150, 40, 600, 420), ['GeneratorSet']),
    ('GeneratorSet', 'GeneratorSet_DamageHousing', TANK_MARKS, (0, 512, 768, 1100), (210, 540, 560, 1110), ['GeneratorSet']),
    ('DrillRig', 'DrillRig_DamageCab', BODY_MARKS, (0, 512, 768, 1024), (300, 40, 720, 560), ['DrillRig']),
    ('DrillRig', 'DrillRig_DamagePowerUnit', TANK_MARKS, (0, 0, 768, 512), (340, 570, 690, 900), ['DrillRig']),
    ('DrillRig', 'DrillRig_DamageDeck', BODY_MARKS, (0, 0, 768, 1536), (250, 900, 760, 2000), ['DrillRig']),
    ('Tipper', 'Tipper_DamageCab', TANK_MARKS, (0, 1024, 768, 1536), (260, 30, 770, 700), ['Tipper']),
    ('Tipper', 'Tipper_DamageBody', TANK_MARKS, (0, 0, 768, 1536), (262, 712, 762, 2010), ['Tipper_Body', 'Tipper_RockBody', 'Tipper_BulkBody']),
    ('SurveyTruck', 'SurveyTruck_DamageCab', BODY_MARKS, (0, 1024, 768, 1536), (250, 120, 780, 720), ['SurveyTruck']),
    ('SurveyTruck', 'SurveyTruck_DamageCabin', TANK_MARKS, (0, 600, 768, 1100), (250, 730, 780, 1100), ['SurveyTruck']),
    ('SurveyTruck', 'SurveyTruck_DamageDeck', BODY_MARKS, (0, 400, 768, 1300), (240, 1100, 790, 1910), ['SurveyTruck']),
    ('SeabedCrawler', 'SeabedCrawler_DamageHull', TANK_MARKS, (0, 0, 768, 1536), (120, 60, 900, 1900),
     ['SeabedCrawler', 'SeabedCrawler_Research', 'SeabedCrawler_Sensor']),
]


def cropped(master, box):
    """A crop of a marks master, as a new file."""
    DERIVED.mkdir(parents=True, exist_ok=True)
    target = DERIVED / f'{master.stem}-crop-{"-".join(map(str, box))}.png'
    if not target.exists():
        Image.open(master).convert('RGB').crop(box).save(target)
    return target


def on_canvas(part, like):
    size = tuple(json.loads(Path(like).read_text(encoding='utf-8'))['master_size'])
    target = DERIVED / f'{part.stem}-on-{size[0]}x{size[1]}.png'
    if not target.exists():
        image = Image.open(part).convert('RGB')
        if image.width > size[0] or image.height > size[1]:
            image.thumbnail(size, Image.LANCZOS)
        canvas = Image.new('RGB', size, (255, 255, 255))
        canvas.paste(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
        canvas.save(target)
    return target


def clip(path, outlines):
    """Keeps only what falls inside every outline (the body, or each body a slot can show)."""
    image = Image.open(path).convert('RGBA')
    alpha = image.getchannel('A')
    for outline in outlines:
        alpha = ImageChops.multiply(alpha, Image.open(outline).convert('RGBA').getchannel('A'))
    image.putalpha(alpha)
    image.save(path)


def main():
    for vehicle, name, master, crop, box, clips in DAMAGE:
        like = EXPORT / f'{vehicle}.transform.json'
        out = MOD / vehicle
        part = on_canvas(cropped(master, crop), like)
        result = subprocess.run([sys.executable, 'scripts/art/export_vehicle.py', 'overlay', str(part), '--like', str(like), '--out', str(out),
                                 '--name', name, '--fit', *map(str, box), '--marks'], capture_output=True, text=True)
        if result.returncode:
            raise SystemExit(f'{name}: {result.stderr.strip().splitlines()[-1]}')
        for facing in FACINGS:
            clip(out / f'{name}_{facing}.png', [out / f'{c}_{facing}.png' for c in clips])
        for extra in out.glob(f'{name}.*'):
            if extra.suffix == '.json':
                extra.unlink()
        print('wrote', name)


if __name__ == '__main__':
    main()
