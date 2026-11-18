"""Whether any active mod would show an old picture at the next launch, and encoding the textures that need it, as RimSort does.

A thin caller for phobos-modkit's rimworld-textures (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-textures.md there). The game loads a mod's .dds instead of the .png beside it without comparing dates, so a .dds older than
its picture shows the old picture. encode writes .dds files into the named mods' folders, Workshop ones included (owner, 18 Nov 2026),
with RimSort's own todds preset, and refuses while RimWorld runs. Paths come from .local/install-settings.json, the environment or the
defaults. D'thorga Companion's Mods page runs the same tool.

    python scripts/textures.py                          active mods with stale, never-encoded or left-over textures
    python scripts/textures.py status --all             every active mod
    python scripts/textures.py encode --mod PACKAGE.ID  encode one mod's textures (repeatable)
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

REPORTS = ('status', 'encode')


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.textures_cli import main as textures
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv) or ['status']
    if argv[0] in REPORTS and '--settings' not in argv and deploy.SETTINGS_FILE.is_file():
        argv = argv[:1] + ['--settings', str(deploy.SETTINGS_FILE)] + argv[1:]
    return textures(argv, out=out)


if __name__ == '__main__':
    sys.exit(main())
