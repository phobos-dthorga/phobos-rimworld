"""Answer questions about a RimWorld save from a fast index: pawns, hediffs, things, where a thing is, any SQL.

A thin caller for phobos-modkit's rimworld-save tool (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-save.md there). The first question about a save indexes a frozen copy of it, deleted straight
afterwards; the save itself is only ever read. Paths come from the options, the environment variables,
.local/install-settings.json (rimworldSavesPath, modkitCachePath) or the defaults. Indexes stay under the system's
temp folder by default, never in this cloud-synced repository.

The indexer is a Rust program, built once:  python scripts/query-save.py --build-indexer

    python scripts/query-save.py                           summary of the newest save
    python scripts/query-save.py pawns --faction player    the colony's pawns
    python scripts/query-save.py hediffs --pawn Abbeson    one pawn's hediffs
    python scripts/query-save.py find ChunkA8061017        a thing and what holds it
    python scripts/query-save.py things --help             every option of one report
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.save_cli import main as rimworld_save
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_save(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
