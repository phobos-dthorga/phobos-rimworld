"""Keep every version of the game's assembly and of the mods ours tie into, and compare any two of them.

A thin caller for phobos-modkit's rimworld-archive (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-archive.md there). Steam replaces files in place, so the build before an update is gone unless a snapshot
kept it; refresh-mod-list.py takes one every run. What is watched: Assembly-CSharp.dll, and every active mod named by our
mods' About.xml (dependencies, load order) or LoadFolders.xml (IfModActive). The archive lives in
%LOCALAPPDATA%\\phobos-modkit\\assembly-archive (assemblyArchivePath in .local/install-settings.json moves it), never in
this repository: it holds the game's and other authors' files (AGENTS.md).

    python scripts/assembly-archive.py snapshot                 keep what changed since the last snapshot
    python scripts/assembly-archive.py list                     every file kept; --file NAME for one file's versions
    python scripts/assembly-archive.py compare Vehicles.dll     what changed in the last update, member by member
    python scripts/assembly-archive.py compare Vehicles.dll --show CompUpgradeTree.PrerequisitesMet   one member's diff
    python scripts/assembly-archive.py get Assembly-CSharp.dll --back 1   unpack an older build; prints its path
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
    from phobos_modkit.games.rimworld.archive_cli import main as rimworld_archive
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    arguments = list(sys.argv[1:] if argv is None else argv) or ['list']
    if arguments[0] == 'snapshot' and '--root' not in arguments:
        arguments += ['--root', str(ROOT)]
    return rimworld_archive(arguments, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
