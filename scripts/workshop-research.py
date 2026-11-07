"""What players report about RimWorld mods on the Steam Workshop lately, and which active mods do not declare 1.7.

A thin caller for phobos-modkit's rimworld-workshop tool (the pinned submodule at external/phobos-modkit; its guide
is docs/workshop.md there). Reads Steam's public pages only, paced about 8 s apart for comments, and caches
everything for 24 hours in the kit's cache, outside this cloud-synced folder. Mods are named by their name (with *
and ?), package ID, Workshop ID or URL. Paths come from the options, the environment variables,
.local/install-settings.json (rimworldPath, workshopPath, rimworldConfigPath, modkitCachePath) or the defaults.

    python scripts/workshop-research.py report "Useful Marks" smashphil.vehicleframework
    python scripts/workshop-research.py versions             active Workshop mods not declaring the game's version
    python scripts/workshop-research.py comments "Field Tents"
    python scripts/workshop-research.py report --help        every option
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)


def main(argv=None, out=None, **fakes):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.workshop_cli import main as rimworld_workshop
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_workshop(argv, settings_file=deploy.SETTINGS_FILE, out=out, **fakes)


if __name__ == '__main__':
    sys.exit(main())
