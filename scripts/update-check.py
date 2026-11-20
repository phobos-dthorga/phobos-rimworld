"""The game or mods updated: what does it mean for ours?

A thin caller for phobos-modkit's rimworld-update-check (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-update-check.md there). Four answers from one run: whether every member our C# names still exists (as
check-targets.py), whether an update touched the method a temporary patch's fault lives in (config/temporary-patches.json,
beside docs/development/temporary-patches.md), how big each kept update was and the new types it brought, and whether our XML
patches still find their Defs as the active mods write them. What counts as ours is config/update-check.json. Reads only.

    python scripts/update-check.py                          all four, for updates kept in the last 7 days
    python scripts/update-check.py --since 2026-11-19T10:00Z   for updates kept since a moment
    python scripts/update-check.py --only temporary,xml     some of them

Run refresh-mod-list.py first, so the archive has kept the new builds.
"""
from pathlib import Path
import importlib.util
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]


def _upstream():
    """scripts/upstream.py, for its decompiler lookup."""
    spec = importlib.util.spec_from_file_location('upstream', Path(__file__).with_name('upstream.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def arguments(argv, root=ROOT):
    """The arguments to pass on: this repository and our ilspycmd, unless given."""
    argv = list(argv)
    if '--root' not in argv:
        argv += ['--root', str(root)]
    if '--decompiler' not in argv:
        passed = _upstream().arguments([], root)
        if '--decompiler' in passed:
            argv += ['--decompiler', passed[passed.index('--decompiler') + 1]]
    return argv


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.update_check_cli import main as rimworld_update_check
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_update_check(arguments(sys.argv[1:] if argv is None else argv), settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
