"""Summarise RimWorld's Player.log: messages grouped, compared with the run before, and the mods involved named.

A thin caller for phobos-modkit's rimworld-log tool (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-log.md there). Paths come from the options, then the environment variables, then
.local/install-settings.json (rimworldLogsPath, rimworldPath, workshopPath), then the defaults, as for deploy.py.
Reads only, and works while the game runs; writes a file only when --json names one.

    python scripts/triage-log.py                  Player.log against Player-prev.log
    python scripts/triage-log.py --show 1a2b3c4d  one signature in full
    python scripts/triage-log.py --help           every option
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
    from phobos_modkit.games.rimworld.cli import main as rimworld_log
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_log(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
