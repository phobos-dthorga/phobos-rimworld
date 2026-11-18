"""Did an update, or another author's public source, change a method our code patches or names?

A thin caller for phobos-modkit's rimworld-upstream (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-upstream.md there). Our side is every member src/ names on another author's type, as check-targets reads them.
updates compares the newest two versions the assembly archive kept of each file updated lately (refresh-mod-list.py keeps them);
pending reads the source clones (source-repos.py --clone, --update-clones) for commits since that touch those members, before
the Workshop build carries them. Reads only. The decompiler and the clones folder come from this repository's local settings.

    python scripts/upstream.py                         both, for the last 30 days
    python scripts/upstream.py updates --days 90       only the archive's updates
    python scripts/upstream.py pending --days 14       only the public source
"""
from pathlib import Path
import importlib.util
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]


def _query_code():
    """scripts/query-code.py, for its decompiler and clones lookups (its name has a hyphen, so it is loaded by path)."""
    spec = importlib.util.spec_from_file_location('query_code', Path(__file__).with_name('query-code.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def arguments(argv, root=ROOT):
    """The arguments to pass on: this repository, our ilspycmd and the clones folder, unless given."""
    argv = list(argv)
    if '--root' not in argv:
        argv += ['--root', str(root)]
    query_code = _query_code()
    if '--decompiler' not in argv:
        decompiler = [a for a in query_code.compare_arguments(argv, root, game=root / '.no-game') if a != '--decompiler']
        if decompiler:
            argv += ['--decompiler', decompiler[0]]
    if '--clones' not in argv:
        clones = deploy.local_path('sourceClonesPath', 'PHOBOS_SOURCE_CLONES', root / '.local' / 'decompiled')
        if Path(clones).is_dir():
            argv += ['--clones', str(clones)]
    return argv


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.upstream_cli import main as rimworld_upstream
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_upstream(arguments(sys.argv[1:] if argv is None else argv), settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
