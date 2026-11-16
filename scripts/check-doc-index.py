"""Check the development index: every record listed exactly once, each entry one short line.

By 17 Nov 2026 the index's entries had grown to paragraphs (39 of 110 over 350 characters), one was cut off mid-sentence and
glued onto another, and one record was listed twice with opposite status. Each entry is now one line: the record, its date,
what it is and its status, with the detail in the record itself. No network or game installation.
"""
from pathlib import Path
import re
import sys

INDEX = "docs/development/README.md"
MAX_LENGTH = 160
ENTRY = re.compile(r"^- \[[^\]]+\]\(([^)#]+)\)")


def check(root):
    """Return the index's faults."""
    root = Path(root).resolve()
    folder = root / "docs" / "development"
    lines = (root / INDEX).read_text(encoding="utf-8-sig").splitlines()
    faults = []
    listed = {}
    for number, line in enumerate(lines, 1):
        match = ENTRY.match(line)
        if not match:
            if line.startswith("  ") and number > 1 and lines[number - 2].startswith("- ["):
                faults.append(f"{INDEX}:{number}: an entry runs onto a second line; keep it to one")
            continue
        if len(line) > MAX_LENGTH:
            faults.append(f"{INDEX}:{number}: entry is {len(line)} characters (most {MAX_LENGTH}); the detail belongs in the record")
        target = match.group(1)
        if "/" not in target:
            listed.setdefault(target, []).append(number)
    for name, numbers in sorted(listed.items()):
        if len(numbers) > 1:
            faults.append(f"{INDEX}: {name} is listed {len(numbers)} times (lines {', '.join(map(str, numbers))})")
    for record in sorted(p.name for p in folder.glob("*.md") if p.name != "README.md"):
        if record not in listed:
            faults.append(f"{INDEX}: {record} is not listed")
    return faults


if __name__ == "__main__":
    faults = check(Path(__file__).resolve().parents[1])
    for fault in faults:
        print(fault)
    print(f"Development index: {len(faults)} faults.")
    sys.exit(1 if faults else 0)
