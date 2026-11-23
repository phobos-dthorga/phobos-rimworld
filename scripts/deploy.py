"""Copy mods from mods/ into RimWorld's Mods folder.

Previews by default; --apply copies. --all takes every mod under mods/; --build runs dotnet build on every
project under src/ first, so the assemblies are current. Refuses while RimWorld is running, or while anything
holds an installed mod's folder open (naming any File Explorer window open inside it), before changing any mod; backs up what
it replaces to .local/deploy-backups/, and never touches ModsConfig.xml or any mod manager's lists. The
game path comes from --game, the RIMWORLD_PATH environment variable, .local/install-settings.json
("rimworldPath"), or the default Steam location, in that order.

    python scripts/deploy.py PhobosRadioFramework            preview one mod
    python scripts/deploy.py --apply PhobosRadioFramework    copy one mod
    python scripts/deploy.py --all                           preview every mod
    python scripts/deploy.py --build --apply --all           build everything, then copy every mod
    python scripts/deploy.py --build-only                    build everything and copy nothing (fine while the game runs)

After copying, it encodes each installed mod's textures to DDS with RimSort's todds, using RimSort's own "optimized" preset, so
RimSort's texture step is not needed for our mods (owner, 13 Nov 2026: a fresh install had wiped the DDS files RimSort made, and
every update meant running todds over every mod again). todds comes from --todds, RIMSORT_TODDS, install-settings.json
("toddsPath") or RimSort's default install; without it the mods still work from their PNG files. --no-dds skips the step.

Every install writes InstallRecord.json into the installed copy (the commit, the time and any uncommitted files it came from), for
D'thorga Companion's Mods page and phobos-modkit's installs tool: what updating or going back would gain or lose
(docs/development/mod-installs.md). The same write adds "installed" to the history log, for the Companion's Timeline page.

Then it removes the installed mods' repeated backups (owner, 20 Nov 2026: removing them "should be part of the normal process of
install/updating mods"): a backup the same as the one before it, keeping the oldest of each run, as the Companion's Remove repeated
backups does (phobos-modkit's installs.tidy). A failure there is reported and the install still stands.

Installing D'thorga Framework also writes TestRows.tsv into the installed copy, never the repository's mod folder (seen-in-play
checks, E8, 18 Nov 2026): the test rows still waiting for the owner and the notes that stand for each, for the Framework's dev-mode
test-moment message. A Workshop copy has none and shows no message.
"""
import argparse
import datetime as _dt
import json
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys
import time
from urllib.parse import unquote
from urllib.request import url2pathname

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_GAME_PATH = Path(r'C:\Program Files (x86)\Steam\steamapps\common\RimWorld')
SETTINGS_FILE = ROOT / '.local' / 'install-settings.json'
sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

_modkit.ensure()
# RimSort's todds and its "optimized" preset live once, in phobos-modkit's RimWorld texture tool (kit 1.21.0), which the Companion's
# texture check also runs: BC1, BC7 where there is alpha, re-encode only what is newer than its DDS, flipped as the game reads DDS.
# Then the kit shortens the mip chains Waylith would unpack to plain pixels at four times the memory (kit 1.54.0, owner's choice of
# 20 Nov 2026, after D'thorga Vehicles showed 2.0 GiB of textures in Graphics Settings' profiler).
from phobos_modkit.games.rimworld.textures import DEFAULT_TODDS, RIMSORT_OPTIMIZED, trim_folder  # noqa: E402
from phobos_modkit import build_lock  # noqa: E402
from phobos_modkit.progress import Progress  # noqa: E402
BACKUPS = ROOT / '.local' / 'deploy-backups'
# RimWorld.exe is 1.7 on Windows (seen in tasklist, 30 Oct 2026); RimWorldWin64.exe is 1.6.
GAME_PROCESSES = ('RimWorld.exe', 'RimWorldWin64.exe', 'RimWorldWin.exe', 'RimWorldLinux', 'RimWorldLinux.x86_64',
                  'RimWorldMac')
SKIPPED_DIRS = ('__pycache__', 'obj', 'bin')
SKIPPED_SUFFIXES = ('.pyc', '.pdb')
# A file sync's copy of something it could not merge. Beside a mod's real DLL it can load instead of it: the game keeps the first of two assemblies with one name (10 Nov 2026).
SYNC_CONFLICT = re.compile('_(Download|Upload|Case)Conflict', re.IGNORECASE)


def local_path(key, env_var, default, explicit=None, environ=None, settings_file=SETTINGS_FILE):
    """
    A machine path, by precedence: argument, environment variable, install-settings.json key, the central settings file (D'thorga
    Companion's settings.json, for every repository; kit 1.30.0), default. The kit's paths.resolve owns the order, so every script
    and tool reads a setting the same way (python scripts/settings.py lists each one and where its value comes from).
    """
    from phobos_modkit.paths import resolve
    return resolve(key, env_var, Path(default), explicit, environ, settings_file)


def game_path(explicit=None, environ=None, settings_file=SETTINGS_FILE):
    """The RimWorld folder, by precedence: argument, RIMWORLD_PATH, install-settings.json, default."""
    return local_path('rimworldPath', 'RIMWORLD_PATH', DEFAULT_GAME_PATH, explicit, environ, settings_file)


def todds_path(explicit=None, environ=None, settings_file=SETTINGS_FILE):
    """RimSort's todds, by precedence: argument, RIMSORT_TODDS, install-settings.json ("toddsPath"), RimSort's default install; None if absent."""
    path = local_path('toddsPath', 'RIMSORT_TODDS', DEFAULT_TODDS, explicit, environ, settings_file)
    return path if path.is_file() else None


def texture_count(folder):
    """How many PNG files the todds preset would read in an installed mod: those with "Textures" in their path."""
    folder = Path(folder)
    if not folder.is_dir():
        return 0
    return sum(1 for png in folder.rglob('*.png') if 'Textures' in str(png))


def encode_textures(folder, todds, run=subprocess.run, log=print, clock=time.monotonic, trim=trim_folder):
    """
    Encodes one installed mod's textures to DDS beside its PNG files, exactly as RimSort's "optimized" preset would, then shortens the
    mip chains Waylith would unpack. Returns the number of PNG files it covered (0 when there were none or todds failed). A failure is
    reported, never raised: the PNG files still work.
    """
    count = texture_count(folder)
    if count == 0:
        return 0
    started = clock()
    result = run([str(todds), *RIMSORT_OPTIMIZED, str(Path(folder))], capture_output=True, text=True, check=False)
    if result.returncode != 0:
        log(f'  todds could not encode the textures of {Path(folder).name} (exit {result.returncode}); the game will read the PNG files.'
            f' {(result.stderr or result.stdout or "").strip()[:300]}')
        return 0
    log(f'  encoded {count} textures to DDS in {clock() - started:.1f} s')
    shortened = trim(folder)
    if shortened.trimmed or shortened.left_whole or shortened.failed:
        log(f'  shortened {len(shortened.trimmed)} mip chains so Waylith keeps them compressed'
            + (f'; {len(shortened.left_whole)} left whole (too few levels would remain)' if shortened.left_whole else '')
            + (f'; could not write {len(shortened.failed)}: {", ".join(shortened.failed[:3])}' if shortened.failed else ''))
    return count


def package(mod, root=ROOT, log=None):
    """Relative path -> absolute source path for every file of mods/<mod> that ships."""
    source = Path(root) / 'mods' / mod
    if not source.is_dir():
        raise FileNotFoundError(f'no such mod folder: {source}')
    files = {}
    for folder, dirnames, filenames in os.walk(source):
        dirnames[:] = sorted(d for d in dirnames if d not in SKIPPED_DIRS)
        for name in sorted(filenames):
            if name.endswith(SKIPPED_SUFFIXES):
                continue
            if SYNC_CONFLICT.search(name):
                if log:
                    log(f'Left out a file sync conflict copy: {mod}/{name}')
                continue
            path = Path(folder) / name
            files[path.relative_to(source).as_posix()] = path
    return files


def listed_in_tasklist(out, process_names=GAME_PROCESSES):
    """True if Windows tasklist CSV output names one of the processes (Windows image names ignore case)."""
    lowered = out.lower()
    return any(f'"{name.lower()}"' in lowered for name in process_names)


def game_running(process_names=GAME_PROCESSES):
    """True if a RimWorld process is running (Windows tasklist; pgrep elsewhere)."""
    if sys.platform == 'win32':
        out = subprocess.run(['tasklist', '/FO', 'CSV', '/NH'], capture_output=True, text=True, check=False).stdout
        return listed_in_tasklist(out, process_names)
    for name in process_names:
        if subprocess.run(['pgrep', '-x', name], capture_output=True, check=False).returncode == 0:
            return True
    return False


def explorer_windows():
    """The local folders open in File Explorer windows (Windows only; empty elsewhere or if the query fails)."""
    if sys.platform != 'win32':
        return []
    command = '(New-Object -ComObject Shell.Application).Windows() | ForEach-Object { $_.LocationURL }'
    try:
        out = subprocess.run(['powershell', '-NoProfile', '-NonInteractive', '-Command', command], capture_output=True, text=True,
                             check=False, timeout=20).stdout
    except (OSError, subprocess.SubprocessError):
        return []
    folders = []
    for line in out.splitlines():
        line = line.strip()
        if line.lower().startswith('file:///'):
            folders.append(Path(url2pathname(unquote(line[len('file:///'):]))))
    return folders


def inside(path, folder):
    """True if path is folder or anywhere below it (case-insensitive, as Windows paths are)."""
    path, folder = str(Path(path)).lower().rstrip('\\/'), str(Path(folder)).lower().rstrip('\\/')
    return path == folder or path.startswith(folder + os.sep) or path.startswith(folder + '/')


def held_message(target, error, windows):
    """Why a mod folder cannot be moved, and what to close: an Explorer window open inside it when there is one."""
    open_inside = [w for w in windows if inside(w, target)]
    if open_inside:
        hint = 'A File Explorer window is open inside it: ' + '; '.join(str(w) for w in open_inside) + '. Close that window'
    else:
        hint = ('Something holds a file or folder inside it open: close any File Explorer window, terminal or editor open in it, '
                'and RimSort if it is showing the mod')
    return f'{target} cannot be moved ({error}). {hint}, then deploy again. Nothing was changed.'


def check_movable(mods, game, windows=explorer_windows, log=print):
    """
    Before anything is copied: rename each installed mod folder aside and straight back, so a folder something holds open stops the
    whole run before any mod changes (on 8 Nov 2026 an Explorer window inside one mod left every later mod uninstalled while earlier
    ones updated). Raises RuntimeError naming every held folder.
    """
    stamp = _dt.datetime.now().strftime('%Y%m%d-%H%M%S')
    held = []
    for mod in mods:
        target = Path(game) / 'Mods' / mod
        if not target.exists():
            continue
        probe = Path(game) / f'{mod}-deploy-check-{stamp}'
        try:
            target.rename(probe)
        except OSError as error:
            held.append((target, error.strerror))
            continue
        try:
            probe.rename(target)
        except OSError as error:
            raise RuntimeError(f'{target} was moved to {probe} for a check and could not be moved back ({error.strerror}). '
                               f'Rename {probe.name} back to {mod} by hand.') from error
    if held:
        found = windows()
        raise RuntimeError('\n'.join(held_message(target, reason, found) for target, reason in held))


def deploy(mod, game, apply, root=ROOT, backups=BACKUPS, running=game_running, log=print):
    """Preview or copy one mod. Returns the number of files that would be, or were, copied."""
    files = package(mod, root, log=log)
    target = Path(game) / 'Mods' / mod
    assemblies = [rel for rel in files if '/Assemblies/' in rel and rel.endswith('.dll')]
    log(f'{mod}: {len(files)} files -> {target}' + ('' if assemblies else ' (no assembly built: XML only)'))
    if not apply:
        for rel in files:
            log(f'  {rel}')
        log('Preview only. Add --apply to copy.')
        return len(files)
    if running():
        raise RuntimeError('RimWorld is running. Close it, then deploy again.')
    if not (Path(game) / 'Mods').is_dir():
        raise FileNotFoundError(f'no Mods folder under {game}; is the game path right?')
    if target.exists():
        stamp = _dt.datetime.now().strftime('%Y%m%d-%H%M%S')
        backup = Path(backups) / f'{mod}-{stamp}'
        backup.parent.mkdir(parents=True, exist_ok=True)
        # Rename on the game's own drive first, outside Mods: Windows refuses while any file in the folder is
        # open, such as an assembly a running game has loaded, and nothing has changed yet. Moving straight to
        # another drive copies, then deletes file by file, and can stop halfway through the install.
        staged = Path(game) / f'{mod}-deploying-{stamp}'
        try:
            target.rename(staged)
        except OSError as error:
            raise RuntimeError(held_message(target, error.strerror, explorer_windows())) from error
        shutil.move(str(staged), str(backup))
        log(f'  previous install moved to {backup}')
    for rel, source in files.items():
        destination = target / rel
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
    log(f'  copied {len(files)} files')
    write_install_record(target, mod, root, log)
    if mod == TEST_ROWS_MOD:
        write_test_rows(target, root, log)
    return len(files)


def write_install_record(target, mod, root=ROOT, log=print, settings_file=SETTINGS_FILE):
    """
    Writes InstallRecord.json into the installed copy, never the repository's mod folder: the commit, the time and any uncommitted files
    it was installed from, for D'thorga Companion's Mods page and phobos-modkit's installs tool (docs/development/mod-installs.md).
    The kit owns the record's format. A failure is reported, never raised: only the record goes without. Returns the record or None.
    """
    try:
        from phobos_modkit import installs
        from phobos_modkit.games.rimworld.installs import identity
        record = installs.record_for(root, installs.load_config(root), mod, identity(target)[1])
        installs.write_record(target, record, settings_file)
    except Exception as error:  # noqa: BLE001 (the install itself has succeeded; the record only feeds the Companion)
        log(f'  the install record could not be written ({error})')
        return None
    commit = (record.get('commit') or 'no commit')[:8]
    extra = f', with {record["uncommittedCount"]} uncommitted files' if record['uncommittedCount'] else ''
    log(f'  wrote {INSTALL_RECORD}: {record["version"] or "no version"} from {commit}{extra}')
    return record


INSTALL_RECORD = 'InstallRecord.json'


# The test-moment message's rows (seen-in-play checks, E8): read by D'thorga Framework's Rules/TestRowsRule from its installed folder.
TEST_ROWS_MOD = 'PhobosFramework'
TEST_ROWS_FILE = 'TestRows.tsv'
WAITING = ('open', 'partial', 'seen')


def test_rows_text(root=ROOT):
    """The test rows still waiting for the owner, one line per piece of evidence: row, title, key, case, detail wildcard (tab-separated)."""
    from phobos_modkit import testpass
    config = testpass.load_config(root)
    rows = testpass.parse_rows((Path(root) / config['doc']).read_text(encoding='utf-8'))
    mapping = testpass.parse_mapping(config)
    tidy = lambda text: ' '.join(str(text).replace('\t', ' ').split())
    lines = ['# The test rows still waiting for the owner, and the seen-in-play notes that stand for each; written by scripts/deploy.py',
             '# at install, for the dev-mode test-moment message. Not part of the mod: a Workshop copy has none.']
    for row in rows:
        if row.status not in WAITING:
            continue
        for part in mapping.get(row.id, []):
            for item in part.items:
                lines.append('\t'.join([row.id, tidy(row.title), item.key, tidy(item.variant or ''), tidy(item.detail or '')]))
    return '\n'.join(lines) + '\n'


def write_test_rows(target, root=ROOT, log=print):
    """Writes TestRows.tsv into an installed D'thorga Framework. A failure is reported, never raised: only the message goes without."""
    try:
        text = test_rows_text(root)
        with open(Path(target) / TEST_ROWS_FILE, 'w', encoding='utf-8', newline='\n') as handle:
            handle.write(text)
    except Exception as error:  # noqa: BLE001 (the install itself has succeeded; this only feeds a dev-mode message)
        log(f'  the test rows could not be written ({error}); the test-moment message will say nothing')
        return 0
    count = sum(1 for line in text.splitlines() if line and not line.startswith('#'))
    log(f'  wrote {TEST_ROWS_FILE}: {count} pieces of evidence for the rows still waiting')
    return count


def all_mods(root=ROOT):
    """Every mod folder under mods/: the folders that hold an About/About.xml, in name order."""
    mods = Path(root) / 'mods'
    return sorted(p.name for p in mods.iterdir() if p.is_dir() and (p / 'About' / 'About.xml').is_file())


def projects(root=ROOT):
    """Every C# project under src/, in name order."""
    return sorted((Path(root) / 'src').glob('*/*.csproj'))


class Steps:
    """
    Counts a run's steps for a program showing progress (phobos-modkit's progress lines, written only when it asks for them): a project
    built, a mod copied, a mod's textures encoded. A person at a console sees deploy's ordinary lines and nothing more.
    """

    def __init__(self, total=0, progress=None):
        self.total, self.done = total, 0
        self.progress = progress if progress is not None else Progress()

    def __call__(self, label):
        self.done += 1
        self.progress(label, min(self.done, self.total) if self.total else None, self.total or None)


def build(root=ROOT, run=subprocess.run, log=print, steps=None, locked=None):
    """Build every project under src/ (each for both game versions). Raises RuntimeError on the first failure.

    Inside the repository's build lock (phobos-modkit 1.103.0; the owner's report of 23 Nov 2026: an install failed because another
    session's gate was building the same projects at the same moment): while another build holds it, this one says so and waits.
    """
    steps = steps or Steps(len(projects(root)))
    locked = locked or build_lock.held
    try:
        with locked(root, 'an install (deploy.py)', log=log):
            for project in projects(root):
                steps(f'Building {project.stem}')
                log(f'building {project.relative_to(root).as_posix()}')
                result = run(['dotnet', 'build', str(project), '--nologo', '-v', 'q'], capture_output=True, text=True, check=False)
                if result.returncode != 0:
                    raise RuntimeError(f'build failed for {project.name}:\n{result.stdout}{result.stderr}')
    except build_lock.BuildBusy as busy:
        raise RuntimeError(f'build not started: {busy}') from None


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('mods', nargs='*', help='mod folder names under mods/ (or --all)')
    parser.add_argument('--all', action='store_true', help='every mod under mods/')
    parser.add_argument('--build', action='store_true', help='run dotnet build on every project under src/ first')
    parser.add_argument('--apply', action='store_true', help='copy; without it, only list what would be copied')
    parser.add_argument('--game', help='RimWorld folder (default: RIMWORLD_PATH, .local/install-settings.json, or Steam)')
    parser.add_argument('--todds', help="RimSort's todds.exe (default: RIMSORT_TODDS, install-settings.json, or RimSort's install)")
    parser.add_argument('--no-dds', action='store_true', help="leave the textures as PNG; RimSort's texture step can make the DDS files")
    parser.add_argument('--build-only', action='store_true',
                        help='build every project under src/ and stop: nothing is copied, so it may run while the game does')
    args = parser.parse_args(argv)
    if args.build_only:
        build()
        print(f'built {len(projects())} projects; nothing was copied.')
        return 0
    steps = Steps()
    mods = all_mods() if args.all else args.mods
    if not mods:
        parser.error('name a mod, or pass --all')
    game = game_path(args.game)
    if args.apply and game_running():
        raise RuntimeError('RimWorld is running. Close it, then deploy again.')
    steps.total = (len(projects()) if args.build else 0) + (len(mods) * (1 if args.no_dds else 2) + 1 if args.apply else 0)
    if args.build:
        build(steps=steps)
    if args.apply:
        check_movable(mods, game)
    for mod in mods:
        if args.apply:
            steps(f'Installing {mod}; its installed copy becomes a backup')
        deploy(mod, game, args.apply)
    if args.apply:
        steps('Removing repeated backups')
        tidy_backups(mods)
    if args.apply and not args.no_dds:
        encode_installed(mods, game, args.todds, steps=steps)
    if args.apply:
        print(f'deployed {len(mods)} mod{"s" if len(mods) != 1 else ""}.')
    return 0


def tidy_backups(mods, root=ROOT, log=print):
    """
    Removes the installed mods' repeated backups, keeping the oldest of each run: phobos-modkit's installs.tidy, which Companion's
    go-back runs too, with the same kinds as its Remove repeated backups button. The install has already succeeded, so a failure is
    reported, never raised. Returns what was removed.
    """
    from phobos_modkit import installs
    try:
        where = installs.place(Path(root))
    except Exception as error:  # noqa: BLE001 (the install stands; only the tidying goes without)
        removed, problem = [], str(error)
    else:
        removed, problem = installs.tidy(where, mods)
    for line in installs.tidy_lines(removed, problem):
        log(f'  {line}')
    return removed


def encode_installed(mods, game, explicit=None, find=todds_path, encode=encode_textures, log=print, steps=None):
    """Encodes the textures of the mods just deployed, and only those. Without todds it says so once and carries on."""
    todds = find(explicit)
    if todds is None:
        log("todds was not found (RimSort's texture encoder): the mods will use their PNG files. Pass --todds, set RIMSORT_TODDS, "
            "or run RimSort's texture step.")
        return 0
    total = 0
    for mod in mods:
        folder = Path(game) / 'Mods' / mod
        if steps is not None:
            steps(f"Encoding {mod}'s textures")
        if texture_count(folder):
            log(f'{mod}: textures to DDS')
        total += encode(folder, todds, log=log)
    return total


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (RuntimeError, FileNotFoundError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
