"""Write a dark review page that shows every generated candidate with its seed and record, so the owner
can judge a whole round in one scroll. Pure Python; opens from disk with relative image paths.

    python assets/phobos-radio/review_page.py            writes assets/phobos-radio/generated/review.html
"""
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GENERATED = ROOT / 'assets' / 'phobos-radio' / 'generated'
OUT = GENERATED / 'review.html'

STYLE = """
:root { --bg:#171814; --panel:#20221c; --line:#3a3d33; --fg:#e6e1d3; --muted:#a8a392; --accent:#d6b25e; }
body { background:var(--bg); color:var(--fg); font-family: "Atkinson Hyperlegible","Segoe UI",Verdana,sans-serif; margin:0; padding:24px; line-height:1.5; }
h1 { font-size:1.8rem; margin:0 0 4px; } h2 { font-size:1.3rem; margin:36px 0 8px; color:var(--accent); }
.meta { color:var(--muted); font-size:0.95rem; }
.grid { display:grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap:14px; }
.card { background:var(--panel); border:1px solid var(--line); padding:10px; }
.card img { width:100%; height:auto; display:block; background:#fff; }
.card .name { font-weight:bold; margin:8px 0 2px; }
.card .rec { color:var(--muted); font-size:0.85rem; white-space:pre-wrap; max-height:7.5em; overflow:auto; }
details { margin-top:6px; } summary { cursor:pointer; color:var(--muted); }
nav a { color:var(--accent); margin-right:14px; }
"""


def build():
    sections = []
    folders = sorted(p for p in GENERATED.iterdir() if p.is_dir())
    nav = ' '.join(f'<a href="#{html.escape(f.name)}">{html.escape(f.name)}</a>' for f in folders)
    for folder in folders:
        cards = []
        for png in sorted(folder.glob('*.png')):
            if png.name.endswith('-sheet.png'):
                continue
            record_path = png.with_suffix('.json')
            record = json.loads(record_path.read_text(encoding='utf-8')) if record_path.is_file() else {}
            summary = ', '.join(f'{k}: {record[k]}' for k in ('engine', 'seed', 'steps', 'size', 'denoise', 'lora') if k in record)
            prompt = record.get('prompt', '')
            refs = record.get('references', [])
            init = record.get('init_image')
            extra = ''
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
    page = (f'<!doctype html><html><head><meta charset="utf-8"><title>Radio art review</title><style>{STYLE}</style></head><body>'
            f'<h1>Radio framework: generated candidates</h1><div class="meta">Local, from the records beside each image. '
            f'Nothing here is in the mod until approved.</div><nav>{nav}</nav>{"".join(sections)}</body></html>')
    OUT.write_text(page, encoding='utf-8')
    print(f'wrote {OUT} ({len(sections)} sections)')


if __name__ == '__main__':
    build()
