"""The owner's settings for our mods, each value beside its default, read from the game's Config folder; any other mod's too.

A thin caller for phobos-modkit's rimworld-settings (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-settings.md there). RimWorld writes a value only when it differs from the default, so this script reads
every ModSettings class in src/ (scripts/_csharp.py) and passes the defaults along: the report then shows each saved
value beside its default, the keys left at default, keys our code no longer reads, and defaults that disagree. Paths
come from the options, then the environment, then .local/install-settings.json, as for deploy.py. Reads only.

    python scripts/mod-settings.py                       our mods' settings (package IDs phobosdthorga.*)
    python scripts/mod-settings.py --mod "ceteam.*"      another mod's (name, package ID or Workshop folder; * and ?)
    python scripts/mod-settings.py --list                every mod with settings, and files left by removed mods
    python scripts/mod-settings.py snapshot FILE         every value now; then compare FILE later to see what changed
    python scripts/mod-settings.py changes --since REV   settings our code added, removed or gave a new default since a
                                                         commit (REV is anything git understands: a hash, HEAD~5, a tag)

The owner asked on 10 Nov 2026 to be told whenever a setting of ours is added or changed, so they can look at it or set it;
`changes` is how a session finds out, reading only the repository's history, never the game.
"""
from pathlib import Path
import json
import os
import subprocess
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _csharp  # noqa: E402  (our settings classes' defaults)
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]
OUR_MODS = 'phobosdthorga.*'
CHOOSERS = ('--mod', '--all', '--list', 'snapshot', 'compare')


def is_source(path):
    parts = Path(path).parts
    return path.endswith('.cs') and 'obj' not in parts and 'bin' not in parts


def sources_now(root=ROOT):
    return {p.relative_to(root).as_posix(): p.read_text(encoding='utf-8') for p in Path(root, 'src').rglob('*.cs') if is_source(p.as_posix())}


def sources_at(revision, root=ROOT):
    """{path: text} of every C# file under src/ as it was at a commit, read from git's history in one batch."""
    names = subprocess.run(['git', '-c', 'core.fsmonitor=false', 'ls-tree', '-r', '--name-only', revision, '--', 'src'], cwd=root,
                           capture_output=True, text=True, encoding='utf-8', check=True).stdout.split('\n')
    names = [n for n in names if n and is_source(n)]
    batch = subprocess.run(['git', '-c', 'core.fsmonitor=false', 'cat-file', '--batch'], cwd=root, check=True, capture_output=True,
                           input=''.join(f'{revision}:{n}\n' for n in names).encode('utf-8')).stdout
    sources, at = {}, 0
    for name in names:
        header_end = batch.index(b'\n', at)
        size = int(batch[at:header_end].split()[2])
        sources[name] = batch[header_end + 1:header_end + 1 + size].decode('utf-8', 'replace')
        at = header_end + 1 + size + 1
    return sources


def changes(old, new):
    """What differs between two defaults documents: [(class, key, what, old default, new default)], what being added, removed or
    default. A class added or removed whole counts each of its keys."""
    found = []
    for cls in sorted(set(old['classes']) | set(new['classes'])):
        before, after = old['classes'].get(cls, {}), new['classes'].get(cls, {})
        for key in after:
            if key not in before:
                found.append((cls, key, 'added', None, after[key]['default']))
            elif before[key]['default'] != after[key]['default']:
                found.append((cls, key, 'default', before[key]['default'], after[key]['default']))
        found += [(cls, key, 'removed', before[key]['default'], None) for key in before if key not in after]
    return found


def report_changes(argv, out=None):
    out = out or sys.stdout
    if len(argv) != 2 or argv[0] != '--since':
        print('usage: mod-settings.py changes --since REV', file=sys.stderr)
        return 2
    try:
        old = defaults_document(sources=sources_at(argv[1]))
    except subprocess.CalledProcessError as error:
        print(f'git could not read {argv[1]}: {(error.stderr or "").strip()}', file=sys.stderr)
        return 2
    found = changes(old, defaults_document())
    if not found:
        print(f'No setting of ours was added, removed or given a new default since {argv[1]}.', file=out)
        return 0
    print(f'{len(found)} setting change(s) since {argv[1]}:', file=out)
    for cls, key, what, before, after in found:
        text = {'added': f'added, default {after!r}', 'removed': f'removed (was {before!r})',
                'default': f'default changed from {before!r} to {after!r}'}[what]
        print(f'  {cls}: {key} {text}', file=out)
    return 0


def defaults_document(root=ROOT, sources=None):
    """{schemaVersion, classes: {settings class: {key: {default, initial, field}}}} from every ModSettings class in src/, or in the
    sources given."""
    classes = _csharp.read_settings_classes(sources if sources is not None else sources_now(root))
    document = {'schemaVersion': 1, 'classes': {}}
    for name, cls in sorted(classes.items()):
        entries = {}
        for key in _csharp.all_keys(cls, classes):
            entry = {'default': key.default, 'field': key.field}
            if key.initial is not None:
                entry['initial'] = key.initial
            entries[key.key] = entry
        document['classes'][name] = entries
    return document


def main(argv=None, out=None):
    argv = list(sys.argv[1:] if argv is None else argv)
    if argv[:1] == ['changes']:
        return report_changes(argv[1:], out)
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.settings_cli import main as rimworld_settings
    if not any(arg in CHOOSERS for arg in argv) and not any(a in ('-h', '--help') for a in argv):
        argv += ['--mod', OUR_MODS]
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    handle, defaults = tempfile.mkstemp(prefix='phobos-settings-defaults-', suffix='.json')
    try:
        with os.fdopen(handle, 'w', encoding='utf-8') as file:
            json.dump(defaults_document(), file)
        if not any(a in ('-h', '--help') for a in argv) and '--defaults' not in argv:
            argv += ['--defaults', defaults]
        return rimworld_settings(argv, settings_file=deploy.SETTINGS_FILE, out=out)
    finally:
        os.remove(defaults)


if __name__ == '__main__':
    sys.exit(main())
