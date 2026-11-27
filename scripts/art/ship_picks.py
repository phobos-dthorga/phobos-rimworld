"""Ship the owner's picked tries of a request into the mods, each with its record (27 Nov 2026: replaces a ship script per batch).

    python scripts/art/ship_picks.py REQUEST.json NAME=TRY [NAME=TRY ...] [--size 128] [--note NAME="extra sentence"]

Each pick ships where its request entry's art_plan.ships_as says, exported by scripts/art/export_sprite.py (the background AI looked
at every try when review_cards.py drew its card), with a record under assets/records that opens with how the picture was made, read
from the try's own record (its generator and engine), so a local, OpenAI or fal try each says what it is. A try the near-copy check
flagged against others' art is refused (sort_rules.flagged_in_record). Pictures only; sounds ship through export_sound.py.
"""
import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import art_paths  # noqa: E402
import sort_rules  # noqa: E402


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def made_with(try_record: dict) -> str:
    """The record's opening: how the try was made, from its own record."""
    generator = try_record.get('generator') or try_record.get('engine') or 'an unrecorded generator'
    paid = try_record.get('engine') in ('openai', 'fal')
    if 'through' in generator:  # the generator already says where it ran (fal's does)
        return f'Made with {generator}: **AI-generated**.'
    where = 'through a paid image service' if paid else "on the owner's own graphics card"
    return f'Made with {generator}, {where}: **AI-generated**.'


def record_text(name: str, pick: str, entry: dict, request: str, try_record: dict, master: Path, target: Path, size: int,
                note: str = '') -> str:
    plan = entry.get('art_plan') or {}
    what = plan.get('button') or entry.get('title') or name
    refs = entry.get('references') or []
    ref_line = (' Its references were our own approved art, for style only; no game or other author\'s art was uploaded or used.'
                if refs else '')
    lines = [f'# {target.stem}', '',
             f'{made_with(try_record)} {what}. Picked by the owner on the Picks page (try {pick}). The request is `{request}`.'
             f'{ref_line}{(" " + note) if note else ""} Exported by `scripts/art/export_sprite.py` at {size} px after the background AI had '
             'looked at the master, by `scripts/art/ship_picks.py`. The master and its record go to the `art-masters` branch, the other '
             'tries to `art-references`.',
             '',
             f'- Master: `{master.stem}`, SHA-256 `{sha(master)}`',
             f'- `{target.name}`: SHA-256 `{sha(target)}` (see its export.json)']
    return '\n'.join(lines) + '\n'


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('request')
    parser.add_argument('picks', nargs='+', metavar='NAME=TRY')
    parser.add_argument('--size', type=int, default=128)
    parser.add_argument('--note', action='append', default=[], metavar='NAME=TEXT', help='a sentence added to that pick\'s record')
    args = parser.parse_args(argv)
    request_file = Path(args.request).resolve()
    request = request_file.relative_to(ROOT).as_posix()
    entries = {e['name']: e for e in json.loads(request_file.read_text(encoding='utf-8'))}
    notes = dict(n.split('=', 1) for n in args.note)
    generated = request_file.parent.parent / 'generated'
    for pick_arg in args.picks:
        name, pick = pick_arg.split('=', 1)
        entry = entries[name]
        master = generated / name / f'{name}-{pick}.png'
        if flags := sort_rules.flagged_in_record(master):
            print(f'refused {master.name}: near-copy flag against {", ".join(f["source"] for f in flags)}')
            return 1
        target = ROOT / entry['art_plan']['ships_as']
        target.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([sys.executable, str(HERE / 'export_sprite.py'), str(master), str(target), '--size', str(args.size)],
                       check=True, stdout=subprocess.DEVNULL)
        try_record = json.loads(master.with_suffix('.json').read_text(encoding='utf-8'))
        text = record_text(name, pick, entry, request, try_record, master, target, args.size, notes.get(name, ''))
        base = target.with_suffix('')
        art_paths.record_path(base.with_name(base.name + '.record.md')).write_text(text, encoding='utf-8')
        print('shipped', target.relative_to(ROOT).as_posix())
    return 0


if __name__ == '__main__':
    sys.exit(main())
