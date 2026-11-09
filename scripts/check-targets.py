"""Whether every member our C# names by string still exists in the game and the active mods.

A thin caller for phobos-modkit's rimworld-targets (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-targets.md there). The compiler checks typeof and nameof; this checks Harmony patches and reflected lookups
named in strings, the overloads they ask for, and the parameters Harmony fills at run time. Assemblies are read as files
through their metadata and never loaded or run (AGENTS.md). Our own fresh build is read first, so a stale installed copy
never answers for it. With no game on the machine, as on CI, it says so and passes: other mods' assemblies may not be
copied there.

    python scripts/check-targets.py          what no longer matches, notes, and what could not be checked
    python scripts/check-targets.py --all    what passed too
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]
OWN_ASSEMBLIES = ('mods/*/1.7/Assemblies/*.dll', 'mods/*/Mods/*/1.7/Assemblies/*.dll')


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.targets_cli import main as rimworld_targets
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    arguments = ['--root', str(ROOT), '--if-installed']
    for pattern in OWN_ASSEMBLIES:
        arguments += ['--assemblies', pattern]
    return rimworld_targets(arguments + list(sys.argv[1:] if argv is None else argv), settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
