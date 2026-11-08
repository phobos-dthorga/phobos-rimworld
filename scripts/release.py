"""Release chores: set a mod's version in every copy at once, check the copies agree, check the changelogs.

A thin caller for phobos-modkit's release tool (the pinned submodule at external/phobos-modkit). The copies of each
mod's version (About.xml, every project's Version, the ModVersion constants, the docs/README.md table and the mod's
README) are registered in config/maintained-constants.json; every copy is checked before any is written.

    python scripts/release.py constants --check                                   do all copies agree?
    python scripts/release.py constants --set PhobosFramework.version=0.27.0      preview a bump
    python scripts/release.py constants --set PhobosFramework.version=0.27.0 --apply
    python scripts/release.py changelog mods/PhobosFramework/CHANGELOG.md --version 0.27.0
    python scripts/release.py --help                                              every command
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]


def main(argv=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.release.cli import main as release
    argv = list(sys.argv[1:] if argv is None else argv)
    if argv and argv[0] in ('constants', 'changelog', 'history') and '--root' not in argv:
        argv[1:1] = ['--root', str(ROOT)]
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return release(argv)


if __name__ == '__main__':
    sys.exit(main())
