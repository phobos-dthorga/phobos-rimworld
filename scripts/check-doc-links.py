"""Check relative Markdown file links (inline and reference-style) and their #anchors; no network or game installation.

Checks tracked and untracked (non-ignored) Markdown. Fenced examples are ignored. Remote URLs are not
validated. An anchor into a Markdown file must name one of its headings as GitHub spells them (or an
explicit <a id> / <a name>); two links to a heading renamed away survived until 17 Nov 2026 because
anchors went unchecked.
"""
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import unquote, urlsplit

# Link text may run onto the next line (wrapped prose); a link left pointing at a merged-away record hid that way.
LINK = re.compile(r"\[[^\]]{0,400}\]\((<[^>]+>|[^\s)]+)(?:\s+\"[^\"]*\")?\)")
# Reference-style definitions, `[label]: target`, as the decision logs use; one left pointing at an archived record hid until
# 23 Nov 2026 because only inline links were read.
REFERENCE = re.compile(r"^[ \t]{0,3}\[[^\]\n]+\]:[ \t]*(<[^>]+>|\S+)")


def references(content):
    """Reference definitions as Markdown reads them: one cannot continue a paragraph, so it counts only at the start, after a
    blank line or after another definition (a wrapped line of prose that starts "[A]: the ..." is not one)."""
    found, may_start = [], True
    for line in content.split("\n"):
        match = REFERENCE.match(line) if may_start else None
        if match:
            found.append(match)
        may_start = bool(match) or not line.strip()
    return found
FENCE = re.compile(r"(?ms)^```.*?^```[^\n]*")
HEADING = re.compile(r"^#{1,6}[ \t]+(.+?)[ \t#]*$", re.M)
HTML_ANCHOR = re.compile(r"<a\s+(?:id|name)=\"([^\"]+)\"", re.I)


def slug(heading):
    """GitHub's anchor for a heading: inline links reduced to their text, lower case, punctuation dropped, spaces to hyphens."""
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", heading)
    text = re.sub(r"<[^>]+>", "", text).lower()
    text = "".join(ch for ch in text if ch.isalnum() or ch in " -_")
    return text.replace(" ", "-")


def anchors(content):
    """Every anchor a Markdown file offers, with GitHub's -1, -2 suffixes for repeated headings."""
    found, seen = set(), {}
    for match in HEADING.finditer(content):
        base = slug(match.group(1))
        count = seen.get(base, 0)
        seen[base] = count + 1
        found.add(base if count == 0 else f"{base}-{count}")
    found.update(HTML_ANCHOR.findall(content))
    return found


def listed_files(root):
    """Tracked and untracked, non-ignored files, as POSIX paths relative to root."""
    out = subprocess.check_output(["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"], cwd=root)
    return [name for name in out.decode("utf-8").split("\0") if name]


def check(root, names=None):
    """Return (links checked, failures) for the Markdown files among names (default: git's file list)."""
    root = Path(root).resolve()
    published = set(listed_files(root) if names is None else names)
    failures = []
    checked = 0
    offered = {}

    def anchors_of(target):
        key = target.resolve()
        if key not in offered:
            offered[key] = anchors(FENCE.sub("", target.read_text(encoding="utf-8-sig")))
        return offered[key]

    for name in sorted(published):
        path = root / name
        if not name.endswith(".md") or not path.is_file():
            continue
        content = FENCE.sub("", path.read_text(encoding="utf-8-sig"))
        for match in [*LINK.finditer(content), *references(content)]:
            link = match.group(1).strip("<>")
            parsed = urlsplit(link)
            if parsed.scheme or parsed.netloc:
                continue
            if not parsed.path:
                if parsed.fragment:
                    checked += 1
                    if unquote(parsed.fragment).lower() not in anchors_of(path):
                        failures.append(f"{name}: no heading for {link}")
                continue
            if parsed.path.startswith("/"):
                target = root / unquote(parsed.path.lstrip("/"))
            else:
                target = path.parent / unquote(parsed.path)
            checked += 1
            try:
                relative = target.resolve().relative_to(root).as_posix()
            except ValueError:
                failures.append(f"{name}: {link} points outside the repository")
                continue
            available = (relative == "." or relative in published
                         or any(other.startswith(relative + "/") for other in published))
            if not available or not target.exists():
                failures.append(f"{name}: missing {link}")
            elif parsed.fragment and target.is_file() and target.suffix == ".md":
                if unquote(parsed.fragment).lower() not in anchors_of(target):
                    failures.append(f"{name}: no heading for {link}")
    return checked, failures


if __name__ == "__main__":
    checked, failures = check(Path(__file__).resolve().parents[1])
    for failure in failures:
        print(failure)
    print(f"Checked {checked} relative file links and anchors; {len(failures)} broken. External URLs not checked.")
    sys.exit(bool(failures))
