"""How long RimWorld takes to start, the costliest mods and what changed between launches, from Loading Progress's sessions.

A thin caller for phobos-modkit's rimworld-startup (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-startup.md there). Paths come from the options, then the environment, then .local/install-settings.json. Reads only.

    python scripts/startup-impact.py                         every launch, the costliest mods, the slowest stages
    python scripts/startup-impact.py compare                 the last two launches
    python scripts/startup-impact.py mod "Combat Extended"   one mod across the launches
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
    from phobos_modkit.games.rimworld.startup_cli import main as rimworld_startup
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_startup(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
