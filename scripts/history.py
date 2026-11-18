"""What happened to our mods over time, and any changelog as it stood at any moment.

A thin caller for phobos-modkit's history (the pinned submodule at external/phobos-modkit; its guide is docs/history.md there), run
on this repository with RimWorld's history log. Facts from git go back to the first commit: versions written, released and removed,
entries added, reworded and removed, mods renamed or merged into another. Facts from this PC (installed, went back, backed up,
built, textures encoded) come from the history log, kept from 19 Nov 2026 on. D'thorga Companion's Timeline page runs the same
tool. Reads only.

    python scripts/history.py                                         every fact, newest first
    python scripts/history.py timeline --since 2026-11-15             only facts from that day on
    python scripts/history.py at --changelog mods/PhobosFramework/CHANGELOG.md --when 2026-11-12T15:00
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]
GAME = 'rimworld'
REPORTS = ('timeline', 'at')


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.history_cli import main as history
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv) or ['timeline']
    if argv[0] in REPORTS and '--root' not in argv:
        argv = argv[:1] + ['--root', str(ROOT)] + argv[1:]
    if argv[0] == 'timeline' and '--game' not in argv:
        argv += ['--game', GAME]
    if '--settings' not in argv:
        argv += ['--settings', str(ROOT / '.local' / 'install-settings.json')]
    return history(argv, out=out)


if __name__ == '__main__':
    sys.exit(main())
