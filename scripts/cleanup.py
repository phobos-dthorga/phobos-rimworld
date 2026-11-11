"""Clear out what this repository's tools, builds and tests leave behind. Previews by default; --apply deletes.

    python scripts/cleanup.py                 list what would go, with sizes and why
    python scripts/cleanup.py --apply         delete it, and stop git's file-watcher daemons
    python scripts/cleanup.py --keep-build    leave the shared tools' development build alone
    python scripts/cleanup.py --help          every option

A thin caller for phobos-modkit's cleanup command (the pinned submodule at external/phobos-modkit; its guide is
docs/cleanup.md there). It clears:

- the shared tools' cache: save indexes whose save has been saved again or has gone, anything else unchanged for 14 days
  (--older-than), and frozen copies a stopped run left; the Workshop cache is always kept. The tools rebuild what they need.
- the shared tools' development build (the debug profile; never the programs the tools run, never while cargo runs).
- what config/cleanup.json names: old install backups (the newest of each current mod stays), the archive tests'
  throwaway repositories, .NET's per-build logs in the temporary folder, and all but the newest RimWorld crash dump.
- git's file-watcher daemons, stopped with --apply; git starts one again when a repository needs it.

It never touches saves, the game's Mods folder, ModsConfig.xml, RimSort's lists or .local's research material. A folder
left by a failed deploy in the game folder (<Mod>-deploying-* or <Mod>-deploy-check-*) is reported, never deleted: it may
be the only copy of an install.
"""
from pathlib import Path
import re
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]
DEPLOY_LEFTOVER = re.compile(r'-(deploying|deploy-check)-\d{8}-\d{6}$')


def deploy_leftovers(game):
    """Folders a failed deploy left in the game folder, to report (never delete)."""
    game = Path(game) if game else None
    if not game or not game.is_dir():
        return []
    return sorted(p for p in game.iterdir() if p.is_dir() and DEPLOY_LEFTOVER.search(p.name))


def main(argv=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit import cleanup_cli
    import deploy

    argv = list(sys.argv[1:] if argv is None else argv)
    extra = ['--root', str(ROOT)]
    if deploy.SETTINGS_FILE.is_file() and '--settings' not in argv:
        extra += ['--settings', str(deploy.SETTINGS_FILE)]
    code = cleanup_cli.main(extra + argv)
    if '--json' not in argv and '-h' not in argv and '--help' not in argv:
        try:
            game = deploy.game_path()
        except Exception:  # noqa: BLE001 - the report is optional
            game = None
        for leftover in deploy_leftovers(game):
            print(f'Left by a failed deploy, not deleted: {leftover}. Check it, then delete it by hand if the mod is installed.')
    return code


if __name__ == '__main__':
    sys.exit(main())
