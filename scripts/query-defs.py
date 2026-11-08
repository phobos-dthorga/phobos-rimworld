"""Which active mods define and patch each Def: answered from an index of the installed game, not by searching mods.

A thin caller for phobos-modkit's rimworld-defs tool (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-defs.md there). The first question indexes every active mod's Defs and patches (about 8 s for 340
mods); later ones reuse the index until the mod list, a Workshop item, the game or a mod in the game's Mods folder
changes. Reads the game's files only. Paths come from the options, the environment variables,
.local/install-settings.json (rimworldPath, workshopPath, rimworldConfigPath, modkitCachePath) or the defaults.
Defs are shown as written: other mods' patches are not applied. `patched <defName>` shows one with every patch applied and
what changed, from the file D'thorga Framework writes on a developer's request (dev mode, then a restart).

    python scripts/query-defs.py                       summary: types, names several mods define, problems
    python scripts/query-defs.py who Gun_Revolver      every definition, which one the game keeps, every patch
    python scripts/query-defs.py show Gun_Revolver     its XML, then each parent the game would use
    python scripts/query-defs.py list --type ThingDef --mod "Vanilla*"
    python scripts/query-defs.py patches --target "Meal*"
    python scripts/query-defs.py patched Gun_Revolver  with every patch applied, and the diff from its written form
    python scripts/query-defs.py who --help            every option of one report
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)


PATCHED_DEFS = ('PhobosPatchedDefs', 'PatchedDefs.xml')   # where D'thorga Framework writes it (Debug/PatchedDefsDump.cs)


def patched_defs_path(settings_file=None, environ=None):
    """The patched-Defs file D'thorga Framework writes: beside the game's Config folder, in the RimWorld user folder."""
    from phobos_modkit.games.rimworld import paths
    return Path(paths.config_path(None, environ, settings_file)).parent.joinpath(*PATCHED_DEFS)


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.defs_cli import main as rimworld_defs
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv)
    if argv and argv[0] in ('patched', 'who', 'show') and '--dump' not in argv and '-h' not in argv and '--help' not in argv:
        argv += ['--dump', str(patched_defs_path(deploy.SETTINGS_FILE))]
    return rimworld_defs(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
