"""Refuse a design record whose opening says nothing is built while the record itself records a build.

The documentation audit of 17 Nov 2026 found about thirty records that opened with "nothing is built" (or "research
only") and recorded the build further down, so a reader who trusts the opening, as the records ask, got the wrong
answer. A record's status lives in one bold line at the top:

    **Status (17 Nov 2026):** built in D'thorga Framework 0.110.0: the fit; not built: the override Def.

The opening is everything before the record's first "## " heading. This check reads only the Markdown under
docs/development; no network or game installation.
"""
from pathlib import Path
import re
import sys

FENCE = re.compile(r"(?ms)^```.*?^```[^\n]*")
STATUS_LINE = re.compile(r"^\*\*Status \([^)]*\):\*\*")
# What a record says when it claims nothing is built.
NOTHING_BUILT = re.compile(
    r"\b[Nn]othing (?:here |in this record |of (?:it|them) )?(?:is |has been |was )?(?:yet )?built\b"
    r"|\b[Rr]esearch and record only\b|\b(?:[Rr]esearch|[Rr]ecord|[Dd]esign) only\b")
# What a record says when it records a build of ours.
# A heading about a build ("## Built", "## Slice 2 as built"), not about one to come ("## Planned, not built",
# "## How it would be built").
BUILT_HEADING = re.compile(r"^#{2,4} (?![^\n]*\b(?:not|be) built\b)[^\n]*\b[Bb]uilt\b", re.M)
# "built in Framework 0.67.0", "**Built: Patches 0.19.1**", "**Shipped** (Framework 0.71.0)", "built (Framework 0.114.0)"
BUILT_IN = re.compile(
    r"\b(?:[Bb]uilt|[Ss]hipped)\b\**:?\**\s*(?:in |\()?(?:D'thorga )?"
    r"(?:Framework|Vehicles|Medical|Patches|Factory Additions|Farming|Water Tap|Radio Framework)(?: Framework)? \d+\.\d+")
SKIP = {"README.md"}


def opening_and_body(text):
    """Split a record at its first second-level heading."""
    text = FENCE.sub("", text)
    match = re.search(r"^## ", text, re.M)
    if not match:
        return text, ""
    return text[:match.start()], text[match.start():]


def problems(name, text):
    """Return the faults in one record's text."""
    opening, body = opening_and_body(text)
    claims = [(line, NOTHING_BUILT.search(line)) for line in opening.splitlines()]
    claims = [(line, claim) for line, claim in claims if claim]
    if not claims:
        return []
    # A build may be recorded anywhere, the opening included, except on the line that claims nothing is built.
    claimed = {line for line, _ in claims}
    rest = "\n".join(line for line in FENCE.sub("", text).splitlines() if line not in claimed)
    built = BUILT_HEADING.search(rest) or BUILT_IN.search(rest)
    if not built:
        return []
    evidence = built.group(0).strip()
    found = []
    for line, claim in claims:
        where = "its status line" if STATUS_LINE.match(line) else "its opening"
        found.append(f'{name}: {where} says "{claim.group(0)}" but the record says "{evidence}"; '
                     "give its status in one **Status (date):** line, saying what is built and what is not")
    return found


def check(root, names=None):
    """Return (records checked, faults) for the records under docs/development (default: every one)."""
    root = Path(root).resolve()
    folder = root / "docs" / "development"
    if names is None:
        names = sorted(p.relative_to(root).as_posix() for p in folder.glob("*.md"))
    faults = []
    checked = 0
    for name in names:
        if Path(name).name in SKIP:
            continue
        checked += 1
        faults.extend(problems(name, (root / name).read_text(encoding="utf-8-sig")))
    return checked, faults


if __name__ == "__main__":
    checked, faults = check(Path(__file__).resolve().parents[1])
    for fault in faults:
        print(fault)
    print(f"Checked {checked} records; {len(faults)} openings contradict their own record.")
    sys.exit(1 if faults else 0)
