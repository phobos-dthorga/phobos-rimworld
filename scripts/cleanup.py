"""Clear out what this repository's tools and tests leave behind. Previews by default; --apply deletes.

    python scripts/cleanup.py            list what would go, with sizes
    python scripts/cleanup.py --apply    delete it

What it clears, and nothing else:

- Old install backups in .local/deploy-backups/: every backup but the newest of each mod still under mods/ (--keep sets how many),
  and every backup of a mod no longer under mods/ (the merged mods of 7 Nov 2026; their files are in git history).
- Throwaway test repositories in the system temporary folder: archive-test-* and phobos-artwork-archive-test-*, from
  tests/test_archive_branch.py (before 9 Nov 2026 its tidy-up could not delete git's read-only files, so each run left some).
- Git's file-watcher daemons (git fsmonitor--daemon), when --apply is given: a global core.fsmonitor starts one per repository, and
  the test repositories left hundreds running. They are only a cache: git starts one again the next time a repository needs it.

It never touches saves, the game's Mods folder, ModsConfig.xml, RimSort's lists, .local's research material, or other programs'
temporary files. A folder left by a failed deploy in the game folder (<Mod>-deploying-* or <Mod>-deploy-check-*) is reported,
never deleted: it may be the only copy of an install.
"""
import argparse
import os
from pathlib import Path
import re
import shutil
import stat
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
BACKUPS = ROOT / '.local' / 'deploy-backups'
STAMP = re.compile(r'^(?P<mod>.+)-(?P<stamp>\d{8}-\d{6})$')
TEST_REPO_PREFIXES = ('archive-test-', 'phobos-artwork-archive-test-')
DEPLOY_LEFTOVER = re.compile(r'-(deploying|deploy-check)-\d{8}-\d{6}$')


def current_mods(root=ROOT):
    """The mods under mods/: the folders holding an About/About.xml."""
    mods = Path(root) / 'mods'
    return {p.name for p in mods.iterdir() if p.is_dir() and (p / 'About' / 'About.xml').is_file()} if mods.is_dir() else set()


def backups_to_remove(names, mods, keep=1):
    """
    From backup folder names (<Mod>-<yyyymmdd>-<hhmmss>): the ones to delete. The newest `keep` of each current mod stay; every
    backup of a mod no longer in the repository goes. Names that do not look like backups are never chosen.
    """
    by_mod = {}
    for name in names:
        match = STAMP.match(name)
        if match:
            by_mod.setdefault(match.group('mod'), []).append((match.group('stamp'), name))
    doomed = []
    for mod, entries in by_mod.items():
        entries.sort()
        kept = keep if mod in mods else 0
        doomed.extend(name for _, name in entries[:max(0, len(entries) - kept)])
    return sorted(doomed)


def test_repositories(temp=None):
    """Throwaway repositories the tests left in the temporary folder."""
    temp = Path(temp or tempfile.gettempdir())
    if not temp.is_dir():
        return []
    return sorted(p for p in temp.iterdir() if p.is_dir() and p.name.startswith(TEST_REPO_PREFIXES))


def deploy_leftovers(game):
    """Folders a failed deploy left in the game folder, to report (never delete)."""
    game = Path(game) if game else None
    if not game or not game.is_dir():
        return []
    return sorted(p for p in game.iterdir() if p.is_dir() and DEPLOY_LEFTOVER.search(p.name))


def size(path):
    total = 0
    for folder, _, files in os.walk(path):
        for name in files:
            try:
                total += os.path.getsize(os.path.join(folder, name))
            except OSError:
                pass
    return total


def remove_tree(path):
    """Deletes a folder, clearing the read-only flag git puts on its object files (Windows will not delete those otherwise)."""
    def writable_then_retry(function, failed, *_):
        os.chmod(failed, stat.S_IWRITE)
        function(failed)
    if sys.version_info >= (3, 12):
        shutil.rmtree(path, onexc=writable_then_retry)
    else:
        shutil.rmtree(path, onerror=writable_then_retry)


def watcher_daemons():
    """Process IDs of running git fsmonitor daemons (Windows only; empty elsewhere)."""
    if sys.platform != 'win32':
        return []
    command = ("Get-CimInstance Win32_Process -Filter \"Name='git.exe'\" | "
               "Where-Object { $_.CommandLine -match 'fsmonitor--daemon' } | ForEach-Object { $_.ProcessId }")
    out = subprocess.run(['powershell', '-NoProfile', '-NonInteractive', '-Command', command], capture_output=True, text=True,
                         check=False).stdout
    return [int(line) for line in out.split() if line.strip().isdigit()]


def stop_daemons(pids):
    stopped = 0
    for pid in pids:
        if subprocess.run(['taskkill', '/PID', str(pid), '/F'], capture_output=True, check=False).returncode == 0:
            stopped += 1
    return stopped


def megabytes(count):
    return f'{count / 1024 / 1024:.1f} MB'


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--apply', action='store_true', help='delete; without it, only list what would go')
    parser.add_argument('--keep', type=int, default=1, help='install backups to keep for each current mod (default 1)')
    parser.add_argument('--game', help='RimWorld folder, to report failed-deploy leftovers (default: as deploy.py finds it)')
    args = parser.parse_args(argv)

    names = [p.name for p in BACKUPS.iterdir() if p.is_dir()] if BACKUPS.is_dir() else []
    backups = [BACKUPS / name for name in backups_to_remove(names, current_mods(), args.keep)]
    repos = test_repositories()
    daemons = watcher_daemons()
    try:
        sys.path.insert(0, str(Path(__file__).parent))
        from deploy import game_path  # noqa: E402
        game = game_path(args.game)
    except Exception:  # noqa: BLE001 - the report is optional
        game = None
    leftovers = deploy_leftovers(game)

    backup_size = sum(size(p) for p in backups)
    repo_size = sum(size(p) for p in repos)
    print(f'Install backups to delete: {len(backups)} of {len(names)} ({megabytes(backup_size)}), keeping the newest {args.keep} of each current mod')
    print(f'Test repositories to delete: {len(repos)} ({megabytes(repo_size)}) in {tempfile.gettempdir()}')
    print(f'Git file-watcher daemons running: {len(daemons)}' + (' (stopped with --apply; git restarts one when a repository needs it)' if daemons else ''))
    for leftover in leftovers:
        print(f'Left by a failed deploy, not deleted: {leftover}. Check it, then delete it by hand if the mod is installed.')
    if not args.apply:
        print('Preview only. Add --apply to delete.')
        return 0

    stopped = stop_daemons(daemons)
    failed = []
    for path in backups + repos:
        try:
            remove_tree(path)
        except OSError as error:
            failed.append(f'{path} ({error.strerror})')
    print(f'Stopped {stopped} daemons; deleted {len(backups) + len(repos) - len(failed)} folders.')
    for line in failed:
        print(f'Could not delete {line}: something holds it open; run again later.')
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
