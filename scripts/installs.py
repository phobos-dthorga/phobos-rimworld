"""What updating or going back would change in the installed mods, by their changelogs; and backing up, going back and installing.

A thin caller for phobos-modkit's installs (the pinned submodule at external/phobos-modkit; its guide is docs/installs.md there),
run on this repository. config/installs.json says where the mods are installed and backed up, and that an install is
scripts/deploy.py. D'thorga Companion's Mods page runs the same tool. Going back and installing refuse while RimWorld runs.

    python scripts/installs.py                                  every mod: installed, repository, backups, gains and losses
    python scripts/installs.py status --mod PhobosFramework     one mod
    python scripts/installs.py backup --mod PhobosFramework     copy the installed mod into .local/deploy-backups
    python scripts/installs.py restore --mod PhobosFramework --backup PhobosFramework-20261118-200751
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]
REPORTS = ('status', 'backup', 'restore', 'install')


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.installs_cli import main as installs
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv) or ['status']
    if argv[0] in REPORTS and '--root' not in argv:
        argv = argv[:1] + ['--root', str(ROOT)] + argv[1:]
    return installs(argv, out=out)


if __name__ == '__main__':
    sys.exit(main())
