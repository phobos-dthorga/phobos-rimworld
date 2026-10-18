"""Copy a mod from mods/ into RimWorld's Mods folder.

Previews by default; --apply copies. Refuses while RimWorld is running, backs up what it replaces to
.local/deploy-backups/, and never touches ModsConfig.xml or any mod manager's lists. The game path comes
from --game, the RIMWORLD_PATH environment variable, .local/install-settings.json ("rimworldPath"), or
the default Steam location, in that order.

    python scripts/deploy.py PhobosRadioFramework            preview
    python scripts/deploy.py --apply PhobosRadioFramework    copy
"""
import argparse
import datetime as _dt
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_GAME_PATH = Path(r'C:\Program Files (x86)\Steam\steamapps\common\RimWorld')
SETTINGS_FILE = ROOT / '.local' / 'install-settings.json'
BACKUPS = ROOT / '.local' / 'deploy-backups'
GAME_PROCESSES = ('RimWorldWin64.exe', 'RimWorldWin.exe', 'RimWorldLinux', 'RimWorldLinux.x86_64', 'RimWorldMac')
SKIPPED_DIRS = ('__pycache__', 'obj', 'bin')
SKIPPED_SUFFIXES = ('.pyc', '.pdb')


def game_path(explicit=None, environ=None, settings_file=SETTINGS_FILE):
    """The RimWorld folder, by precedence: argument, RIMWORLD_PATH, install-settings.json, default."""
    environ = os.environ if environ is None else environ
    if explicit:
        return Path(explicit)
    if environ.get('RIMWORLD_PATH'):
        return Path(environ['RIMWORLD_PATH'])
    settings_file = Path(settings_file)
    if settings_file.is_file():
        with open(settings_file, encoding='utf-8') as fh:
            configured = json.load(fh).get('rimworldPath')
        if configured:
            return Path(configured)
    return DEFAULT_GAME_PATH


def package(mod, root=ROOT):
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
            path = Path(folder) / name
            files[path.relative_to(source).as_posix()] = path
    return files


def game_running(process_names=GAME_PROCESSES):
    """True if a RimWorld process is running (Windows tasklist; pgrep elsewhere)."""
    if sys.platform == 'win32':
        out = subprocess.run(['tasklist', '/FO', 'CSV', '/NH'], capture_output=True, text=True, check=False).stdout
        return any(f'"{name}"' in out for name in process_names)
    for name in process_names:
        if subprocess.run(['pgrep', '-x', name], capture_output=True, check=False).returncode == 0:
            return True
    return False


def deploy(mod, game, apply, root=ROOT, backups=BACKUPS, running=game_running, log=print):
    """Preview or copy one mod. Returns the number of files that would be, or were, copied."""
    files = package(mod, root)
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
        shutil.move(str(target), str(backup))
        log(f'  previous install moved to {backup}')
    for rel, source in files.items():
        destination = target / rel
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)
    log(f'  copied {len(files)} files')
    return len(files)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('mods', nargs='+', help='mod folder names under mods/')
    parser.add_argument('--apply', action='store_true', help='copy; without it, only list what would be copied')
    parser.add_argument('--game', help='RimWorld folder (default: RIMWORLD_PATH, .local/install-settings.json, or Steam)')
    args = parser.parse_args(argv)
    game = game_path(args.game)
    for mod in args.mods:
        deploy(mod, game, args.apply)
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (RuntimeError, FileNotFoundError) as error:
        print(error, file=sys.stderr)
        sys.exit(1)
