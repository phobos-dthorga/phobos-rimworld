"""Check relative inline Markdown file links; no network or game installation.

Checks tracked and untracked (non-ignored) Markdown. Fenced examples are ignored. Remote URLs and
fragments are not validated by this check.
"""
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import unquote, urlsplit

LINK = re.compile(r"\[[^\]\n]*\]\((<[^>]+>|[^\s)]+)(?:\s+\"[^\"]*\")?\)")
FENCE = re.compile(r"(?ms)^```.*?^```[^\n]*")


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
    for name in sorted(published):
        path = root / name
        if not name.endswith(".md") or not path.is_file():
            continue
        content = FENCE.sub("", path.read_text(encoding="utf-8-sig"))
        for match in LINK.finditer(content):
            link = match.group(1).strip("<>")
            parsed = urlsplit(link)
            if parsed.scheme or parsed.netloc or not parsed.path:
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
    return checked, failures


if __name__ == "__main__":
    checked, failures = check(Path(__file__).resolve().parents[1])
    for failure in failures:
        print(failure)
    print(f"Checked {checked} relative file links; {len(failures)} missing targets. "
          "Fragments/external URLs not checked.")
    sys.exit(bool(failures))
