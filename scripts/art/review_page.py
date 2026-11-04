"""Write a dark review page that shows every generated candidate with its seed and record, so the owner
can judge a whole round in one scroll. Where scripts/art/sort_candidates.py has run, the promising images come first and repeats
are marked; nothing is hidden or deleted. Pure Python; opens from disk with relative image paths.

    python scripts/art/review_page.py assets/phobos-medical/generated        writes review.html in that folder
"""
import argparse
import html
import json
from pathlib import Path

STYLE = """
:root { --bg:#171814; --panel:#20221c; --line:#3a3d33; --fg:#e6e1d3; --muted:#a8a392; --accent:#d6b25e; }
body { background:var(--bg); color:var(--fg); font-family: "Atkinson Hyperlegible","Segoe UI",Verdana,sans-serif; margin:0; padding:24px; line-height:1.5; }
h1 { font-size:1.8rem; margin:0 0 4px; } h2 { font-size:1.3rem; margin:36px 0 8px; color:var(--accent); }
.meta { color:var(--muted); font-size:0.95rem; }
.grid { display:grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap:14px; }
.card { background:var(--panel); border:1px solid var(--line); padding:10px; }
.card img { width:100%; height:auto; display:block; background:#fff; }
.card .name { font-weight:bold; margin:8px 0 2px; }
.badge { color:var(--accent); font-size:0.85rem; }
.warn { color:#ff8a7a; font-weight:bold; font-size:0.95rem; }
.card .rec { color:var(--muted); font-size:0.85rem; white-space:pre-wrap; max-height:7.5em; overflow:auto; }
details { margin-top:6px; } summary { cursor:pointer; color:var(--muted); }
nav a { color:var(--accent); margin-right:14px; }
"""


def ordered_candidates(folder):
    """The sorted order and its notes where _sort.json exists, plus anything it does not list, by name."""
    sort_path = folder / '_sort.json'
    measures = json.loads(sort_path.read_text(encoding='utf-8')) if sort_path.is_file() else []
    notes = {m['file']: m for m in measures}
    listed = [folder / m['file'] for m in measures if (folder / m['file']).is_file()]
    rest = [p for p in sorted(folder.glob('*.png')) if p.name not in notes and not p.name.endswith('-sheet.png')]
    return [(p, notes.get(p.name, {})) for p in listed + rest]


def near_copy_flags(generated):
    """Output file name to the source images it came too close to, from scripts/art/near_copy.py."""
    path = generated / 'near-copy.json'
    flags = {}
    if path.is_file():
        for entry in json.loads(path.read_text(encoding='utf-8')).get('flagged', []):
            flags.setdefault(entry['output'], []).append(entry['source'])
    return flags


def build(generated, title=None):
    generated = Path(generated).resolve()
    flags = near_copy_flags(generated)
    out = generated / 'review.html'
    title = title or generated.parent.name
    sections = []
    folders = sorted(p for p in generated.iterdir() if p.is_dir())
    nav = ' '.join(f'<a href="#{html.escape(f.name)}">{html.escape(f.name)}</a>' for f in folders)
    for folder in folders:
        cards = []
        for png, note in ordered_candidates(folder):
            record_path = png.with_suffix('.json')
            record = json.loads(record_path.read_text(encoding='utf-8')) if record_path.is_file() else {}
            summary = ', '.join(f'{k}: {record[k]}' for k in ('engine', 'seed', 'steps', 'seconds', 'size', 'denoise', 'lora') if k in record)
            prompt = record.get('prompt', '')
            refs = record.get('references', [])
            init = record.get('init_image')
            extra = ''
            if png.name in flags:
                extra += f'<div class="warn">Too close to {html.escape(", ".join(flags[png.name]))}: do not pick</div>'
            if 'score' in note:
                extra += f'<div class="badge">sort score {note["score"]}</div>'
            if 'duplicate_of' in note:
                extra += f'<div class="badge">repeats {html.escape(note["duplicate_of"])}</div>'
            if init:
                extra += f'<div class="meta">sketch: {html.escape(init)}</div>'
            if refs:
                extra += '<div class="meta">references: ' + ', '.join(html.escape(r['image']) for r in refs) + '</div>'
            cards.append(
                f'<div class="card"><img loading="lazy" src="{html.escape(folder.name)}/{html.escape(png.name)}" alt="{html.escape(png.stem)}">'
                f'<div class="name">{html.escape(png.stem)}</div><div class="meta">{html.escape(summary)}</div>{extra}'
                f'<details><summary>prompt</summary><div class="rec">{html.escape(prompt)}</div></details></div>')
        if cards:
            sections.append(f'<h2 id="{html.escape(folder.name)}">{html.escape(folder.name)} <span class="meta">({len(cards)})</span></h2><div class="grid">{"".join(cards)}</div>')
    page = (f'<!doctype html><html><head><meta charset="utf-8"><title>{html.escape(title)} art review</title><style>{STYLE}</style></head><body>'
            f'<h1>{html.escape(title)}: generated candidates</h1><div class="meta">Local, from the records beside each image. '
            f'Nothing here is in the mod until approved.</div><nav>{nav}</nav>{"".join(sections)}</body></html>')
    out.write_text(page, encoding='utf-8')
    print(f'wrote {out} ({len(sections)} sections)')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('generated', help='the generated folder of one asset set')
    parser.add_argument('--title')
    arguments = parser.parse_args()
    build(arguments.generated, arguments.title)
