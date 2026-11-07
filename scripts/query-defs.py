"""Which active mods define and patch each Def: answered from an index of the installed game, not by searching mods.

A thin caller for phobos-modkit's rimworld-defs tool (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-defs.md there). The first question indexes every active mod's Defs and patches (about 8 s for 340
mods); later ones reuse the index until the mod list, a Workshop item, the game or a mod in the game's Mods folder
changes. Reads the game's files only. Paths come from the options, the environment variables,
.local/install-settings.json (rimworldPath, workshopPath, rimworldConfigPath, modkitCachePath) or the defaults.
Defs are shown as written: other mods' patches are not applied.

    python scripts/query-defs.py                       summary: types, names several mods define, problems
    python scripts/query-defs.py who Gun_Revolver      every definition, which one the game keeps, every patch
    python scripts/query-defs.py show Gun_Revolver     its XML, then each parent the game would use
    python scripts/query-defs.py list --type ThingDef --mod "Vanilla*"
    python scripts/query-defs.py patches --target "Meal*"
    python scripts/query-defs.py who --help            every option of one report
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
    from phobos_modkit.games.rimworld.defs_cli import main as rimworld_defs
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_defs(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
