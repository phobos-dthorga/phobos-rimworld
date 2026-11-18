"""Where the public source of each mod ours tie into lives, checked against the mod's package ID.

A thin caller for phobos-modkit's rimworld-sources (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-sources.md there). The answers are kept in config/source-repos.json: read that first, and run this when a
mod is not in it, is unconfirmed, or a new tie-in has been added. Keyless and paced: GitHub allows 60 calls an hour without
logging in, answers are cached for a week, and a run that meets the limit says when to run again.

    python scripts/source-repos.py               every tied mod; confirmed ones come from the register
    python scripts/source-repos.py --write       look up and record the answers in config/source-repos.json
    python scripts/source-repos.py --mod some.packageid --only-named   one installed mod
    python scripts/source-repos.py --recheck --write   look again at mods already confirmed
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.sources_cli import main as rimworld_sources
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    arguments = list(sys.argv[1:] if argv is None else argv)
    if '--root' not in arguments:
        arguments += ['--root', str(ROOT)]
    return rimworld_sources(arguments, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
