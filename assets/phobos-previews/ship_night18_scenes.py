"""Ship the owner's picks of night 18's scenes (28 Nov 2026, the Picks page): each titled by scripts/art/make_preview.py, put where its
request entry's art_plan.for says, with a record beside it (or under assets/records for a mod's file) read from the try's own record.

    python assets/phobos-previews/ship_night18_scenes.py

The head mark is shipped by ship_house_mark() as the reference site's logo and favicon, untitled.
"""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
GENERATED = HERE / 'generated'
sys.path.insert(0, str(ROOT / 'scripts' / 'art'))
import art_paths  # noqa: E402

BANNERS = 'docs/player-guide/banners'
# name: (the owner's pick, where it ships, its title, its line under the title). Titles are the chapters' own headings; the lines are
# agent's wording for the owner to revise.
PICKS = {
    'guide-front-door': ('zimage-316', f'{BANNERS}/front-door.png', 'Player guide', 'Every mod, and how to play it'),
    'guide-getting-started': ('2', f'{BANNERS}/getting-started.png', 'Getting started', 'Install, settings, and taking a mod out'),
    'guide-aircraft': ('3', f'{BANNERS}/aircraft-and-sling-loads.png', 'Aircraft and sling loads', 'Lifting, landing and flying'),
    'guide-camping': ('3', f'{BANNERS}/camping.png', 'Camping and life on the road', 'Making camp beside your vehicles'),
    'guide-supply-network': ('zimage-315', f'{BANNERS}/supply-network.png', 'Setting up a supply network', 'Depots, runs and relays'),
    'guide-damage-repairs': ('1', f'{BANNERS}/vehicle-damage-and-repairs.png', 'Vehicle damage and repairs', 'From a hit to a working truck'),
    'guide-vehicle-workshop': ('1', f'{BANNERS}/vehicle-workshop.png', 'The vehicle workshop', 'Fittings, upgrades and tuning'),
    'guide-on-the-road': ('zimage-316', f'{BANNERS}/vehicles-on-the-road.png', 'Vehicles on the road', 'Hills, towing and getting unstuck'),
    'guide-submarines': ('zimage-316', f'{BANNERS}/submarines.png', 'Submarines', 'Under the sea'),
    'guide-modding': ('3', 'docs/reference/images/modding.png', 'For modders', 'Building on the D\'thorga mods'),
    'hero-farming': ('1', 'mods/PhobosFarming/About/Preview.png', 'Farming', 'Machines that work your fields'),
}
MARK = ('house-crest-mark', 'zimage-315')


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def try_files(name: str, pick: str):
    folder = GENERATED / (f'{name}-zimage' if pick.startswith('zimage') else name)
    png = folder / f'{name}-{pick}.png'
    return png, json.loads(png.with_suffix('.json').read_text(encoding='utf-8'))


def made_line(rec: dict) -> str:
    return (f"Made on {rec['date'][:10]} with {rec['generator']}: {rec['steps']} steps, seed {rec['seed']}, at {rec['size'][0]} x "
            f"{rec['size'][1]}. No reference images and no LoRA. No game art is in this file.")


def write_record(target: Path, title: str, name: str, pick: str, src: Path, rec: dict, text_line: str) -> None:
    record = art_paths.record_path(target.with_suffix('.record.md'))
    record.write_text(
        f'# {target.stem}\n\n'
        f"**AI-generated, then picked by the owner on 28 November 2026** (night 18, the Picks page, try {pick} of `{name}`). "
        f"{made_line(rec)}\n\n{text_line}\n\n"
        f"**Prompt:** {rec['prompt']}\n\n"
        f"- Model SHA-256: `{rec['model_sha256']}`\n"
        f"- Background `{src.name}` SHA-256: `{sha(src)}`\n"
        f"- `{target.name}` SHA-256: `{sha(target)}`\n", encoding='utf-8', newline='\n')


def ship_scenes() -> None:
    for name, (pick, out, title, line) in PICKS.items():
        src, rec = try_files(name, pick)
        target = ROOT / out
        target.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(ROOT / 'scripts' / 'art' / 'make_preview.py'), str(src), title, line, str(target)], check=True)
        text = (f'The text ("D\'thorga", "{title}", "{line}") was added by `scripts/art/make_preview.py` in Arial, never generated. The '
                'background is cropped to 16:9 and scaled to 640 x 360. Shipped by `assets/phobos-previews/ship_night18_scenes.py`.')
        write_record(target, title, name, pick, src, rec, text)
        print('shipped', out)


def ship_house_mark() -> None:
    """The head mark as the reference site's logo (96 px) and favicon (32 and 48 px in one .ico), cut from its square."""
    from PIL import Image
    name, pick = MARK
    src, rec = try_files(name, pick)
    images = ROOT / 'docs' / 'reference' / 'images'
    images.mkdir(parents=True, exist_ok=True)
    im = Image.open(src).convert('RGBA')
    logo = images / 'dthorga-mark.png'
    im.resize((96, 96), Image.LANCZOS).save(logo)
    icon = images / 'favicon.ico'
    im.resize((48, 48), Image.LANCZOS).save(icon, sizes=[(32, 32), (48, 48)])
    text = ('Scaled down whole by `assets/phobos-previews/ship_night18_scenes.py`, with nothing added: the logo at 96 x 96, the favicon '
            'at 32 and 48 px. House D\'thorga\'s head mark, the third wording of the house crest (the owner\'s yes of 28 Nov 2026); '
            'the full crest is to be bought on the paid route.')
    for target in (logo, icon):
        write_record(target, 'mark', name, pick, src, rec, text)
    print('shipped the head mark')


if __name__ == '__main__':
    ship_scenes()
    ship_house_mark()
