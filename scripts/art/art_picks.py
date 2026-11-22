"""Offering the owner's picks to D'thorga Companion's Picks page (the owner's choices of 23 Nov 2026: picks come from a file the
repository keeps, `config/picks.json`; a pick is kept as an answer, for the next session to apply).

The review tools call `offer` whenever they make tries for the owner to choose from: review_cards.py for pictures (every card), and
sound_review.py for sounds (with --offer). The file's format and the answers are phobos-modkit's `picks` (docs/picks.md); this only
names our grounds and tidies our names into its ids. Applying a pick stays a session's job: export the try, record it as the art
records say, then `python -m phobos_modkit picks settle --root . --id ID` in the same commit.
"""
from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]

# The grounds the review cards draw on (review_cards.SOIL and STONE), so the Companion shows the same ones.
GROUNDS = [{"name": "Soil", "colour": "#5C4C3A"}, {"name": "Dark stone", "colour": "#303034"}]


def pick_id(name: str) -> str:
    """A request's or a sound's name as a pick's id: lower case, and anything but letters, digits, dots and hyphens made hyphens."""
    tidy = re.sub(r"[^a-z0-9._-]+", "-", name.lower()).strip("-._")
    return (tidy or "pick")[:80]


def _picks():
    sys.path.insert(0, str(ROOT / "scripts"))
    import _modkit  # noqa: PLC0415  (the shared tools, from the pinned submodule)

    _modkit.ensure()
    from phobos_modkit import picks  # noqa: PLC0415

    return picks


def offer(name: str, title: str, kind: str, tries: list[tuple[str, str, Path, str]], in_use: str | None = None,
          context: str = "", source: str = "", root: Path = ROOT) -> dict:
    """Offers one pick: tries as (id, label, file, note). Offering the same name again replaces it, for tries made again."""
    item = {"id": pick_id(name), "title": title, "kind": kind, "inUse": in_use, "context": context, "from": source,
            "tries": [{"id": pick_id(try_id), "label": label, "path": str(path), "note": note} for try_id, label, path, note in tries]}
    return _picks().offer(root, item, grounds=GROUNDS if kind == "picture" else None)
