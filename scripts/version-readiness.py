"""Which active mods are ready for a game version, and which would be likeliest to break if they are not.

A thin caller for phobos-modkit's rimworld-readiness (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-readiness.md there). It says what a mod declares and how closely it depends on the game, never whether it works. Run
`python scripts/workshop-research.py versions` first to refresh the Steam tags. Paths come from the options, then the environment,
then .local/install-settings.json. Reads only.

    python scripts/version-readiness.py                      against the version the game is on now
    python scripts/version-readiness.py --game-version 1.8   against another version
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
    from phobos_modkit.games.rimworld.readiness_cli import main as rimworld_readiness
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_readiness(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
