"""What our changelogs say changed lately, and the code behind any one entry: the commit that wrote it and its diff.

A thin caller for phobos-modkit's changes (the pinned submodule at external/phobos-modkit; its guide is docs/changes.md there), run
on this repository. An entry's code is the commit that first wrote the entry into its changelog, which is the change itself because
every change here updates its changelog in the same commit. A commit that wrote several entries is shown whole, with the others
named. D'thorga Companion's Changes page and code viewer run the same tool. Reads only.

    python scripts/changes.py                        every changelog, its newest versions and their entries
    python scripts/changes.py entry --changelog mods/PhobosFramework/CHANGELOG.md --text "A slung vehicle could be set down"
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]
REPORTS = ('list', 'entry')


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.changes_cli import main as changes
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv) or ['list']
    if argv[0] in REPORTS and '--root' not in argv:
        argv = argv[:1] + ['--root', str(ROOT)] + argv[1:]
    return changes(argv, out=out)


if __name__ == '__main__':
    sys.exit(main())
