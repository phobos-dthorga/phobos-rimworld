"""Which of the owner's active mods patch which methods with Harmony, read from their assemblies' metadata.

A thin caller for phobos-modkit's rimworld-patches (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-patches.md there). Assemblies are read as files through their metadata and never loaded or run (AGENTS.md:
agents never execute other authors' assemblies). The first run builds the kit's C# reader (needs the .NET SDK, which the
mods' builds need anyway) and reads every active mod's DLLs; later runs read only the ones that changed. Paths come from
the options, then the environment, then .local/install-settings.json, as for deploy.py.

    python scripts/query-patches.py who "VehicleStatHandler.ApplyDamage"   every patch on a method, in load order
    python scripts/query-patches.py mod "RimPacts*"                          what one mod patches
    python scripts/query-patches.py hotspots --top 40                        methods several mods patch
    python scripts/query-patches.py                                          counts and the most-patched methods
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
    from phobos_modkit.games.rimworld.patches_cli import main as rimworld_patches
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_patches(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
