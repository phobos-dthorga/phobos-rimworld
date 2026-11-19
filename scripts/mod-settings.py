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
    python scripts/mod-settings.py changes --since REV --json
                                                         the same as JSON, each with its mod and, where one is found, the label
                                                         the settings window shows (phobos-modkit's installs reads it)
    python scripts/mod-settings.py history --json        every setting of ours ever added, removed or given a new default, with
                                                         the commit and time of each, oldest first, and what is not committed yet
                                                         last (D'thorga Companion's Mods page and Timeline read it; each commit is
                                                         read once and kept in the kit's cache)

The owner asked on 10 Nov 2026 to be told whenever a setting of ours is added or changed, so they can look at it or set it;
`changes` is how a session finds out, reading only the repository's history, never the game.
"""
from pathlib import Path
import json
import os
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET

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


def project_mods(root=ROOT):
    """{project folder name: mod folder} from each C# project's ModFolder property."""
    found = {}
    for csproj in Path(root, 'src').glob('*/*.csproj'):
        match = re.search(r'<ModFolder>\s*([^<]+?)\s*</ModFolder>', csproj.read_text(encoding='utf-8', errors='replace'))
        if match:
            found[csproj.parent.name] = match.group(1)
    return found


def class_mods(sources_list, root=ROOT):
    """{settings class name: mod folder}, from the project folder each class's file sits in."""
    projects = project_mods(root)
    found = {}
    for sources in sources_list:
        for name, cls in _csharp.read_settings_classes(sources).items():
            parts = Path(cls.path).parts
            if len(parts) > 1 and parts[0] == 'src' and parts[1] in projects:
                found.setdefault(name, projects[parts[1]])
    return found


def keyed_texts(root=ROOT):
    """{key: English text} from every mod's Keyed language files."""
    texts = {}
    for file in Path(root, 'mods').glob('*/**/Languages/English/Keyed/*.xml'):
        try:
            for element in ET.parse(file).getroot():
                if isinstance(element.tag, str) and element.text:
                    texts.setdefault(element.tag, element.text.strip())
        except (ET.ParseError, OSError):
            continue
    return texts


def setting_label(key, texts):
    """The settings window's label for a saved key, where the mod follows its own naming (Prefix_Thing beside
    Prefix_SettingThing); None when no label is found."""
    prefix, _, rest = key.partition('_')
    for candidate in (f'{prefix}_Setting{rest}', f'{key}Label', f'{key}_Label', key):
        if rest and candidate in texts:
            return texts[candidate]
    return None


def report_changes(argv, out=None):
    out = out or sys.stdout
    as_json = '--json' in argv
    argv = [a for a in argv if a != '--json']
    if len(argv) != 2 or argv[0] != '--since':
        print('usage: mod-settings.py changes --since REV [--json]', file=sys.stderr)
        return 2
    try:
        old_sources = sources_at(argv[1])
        old = defaults_document(sources=old_sources)
    except subprocess.CalledProcessError as error:
        print(f'git could not read {argv[1]}: {(error.stderr or "").strip()}', file=sys.stderr)
        return 2
    new_sources = sources_now()
    found = changes(old, defaults_document(sources=new_sources))
    if as_json:
        mods, texts = class_mods([new_sources, old_sources]), keyed_texts()
        json.dump([{'mod': mods.get(cls), 'class': cls, 'key': key, 'label': setting_label(key, texts), 'what': what,
                    'before': before, 'after': after} for cls, key, what, before, after in found], out)
        out.write('\n')
        return 0
    if not found:
        print(f'No setting of ours was added, removed or given a new default since {argv[1]}.', file=out)
        return 0
    print(f'{len(found)} setting change(s) since {argv[1]}:', file=out)
    for cls, key, what, before, after in found:
        text = {'added': f'added, default {after!r}', 'removed': f'removed (was {before!r})',
                'default': f'default changed from {before!r} to {after!r}'}[what]
        print(f'  {cls}: {key} {text}', file=out)
    return 0


# Commits whose changes touch a settings class: a saved key, its default, or the class itself.
SETTINGS_TOUCHED = r'Scribe_Values\.Look|:\s*ModSettings'
HISTORY_CACHE = 'mod-settings-history'
HISTORY_FORMAT = 1


def history_commits(root=ROOT):
    """[(commit, ISO time, subject)] of every commit whose diff touches a settings class under src/, oldest first."""
    out = subprocess.run(['git', '-c', 'core.fsmonitor=false', 'log', '--reverse', '--format=%H%x1f%cI%x1f%s', '-G', SETTINGS_TOUCHED,
                          '--', 'src'], cwd=root, capture_output=True, text=True, encoding='utf-8', check=True).stdout
    return [tuple(line.split('\x1f', 2)) for line in out.splitlines() if line.count('\x1f') == 2]


def _history_store(root=ROOT):
    from phobos_modkit import cache as derived
    return derived.folder(HISTORY_CACHE) / f'{Path(root).resolve().name}.json'


def _changed_in(commit, root=ROOT):
    """What one commit changed in our settings: [{mod, class, key, what, before, after}] against its first parent."""
    try:
        old_sources = sources_at(f'{commit}^', root)
    except subprocess.CalledProcessError:
        old_sources = {}    # the repository's first commit
    new_sources = sources_at(commit, root)
    mods = class_mods([new_sources, old_sources], root)
    return [{'mod': mods.get(cls), 'class': cls, 'key': key, 'what': what, 'before': before, 'after': after}
            for cls, key, what, before, after in changes(defaults_document(sources=old_sources),
                                                         defaults_document(sources=new_sources))]


def settings_history(root=ROOT, progress=None):
    """Every setting of ours ever added, removed or given a new default, oldest first: [{time, commit, subject, mod, class, key,
    label, what, before, after}], with the working tree's changes not yet committed last (commit None). A commit never changes, so
    what each one changed is kept in the kit's cache and only new commits are read."""
    store = _history_store(root)
    try:
        kept = json.loads(store.read_text(encoding='utf-8'))
        if kept.get('format') != HISTORY_FORMAT:
            kept = {}
    except (OSError, ValueError, AttributeError):
        kept = {}
    commits = kept.setdefault('commits', {})
    found, fresh = [], False
    listed = history_commits(root)
    for position, (commit, when, subject) in enumerate(listed, 1):
        if commit not in commits:
            if progress:
                progress(f'Reading what {commit[:8]} changed in our settings', position, len(listed))
            commits[commit] = _changed_in(commit, root)
            fresh = True
        found += [{'time': when, 'commit': commit, 'subject': subject, **change} for change in commits[commit]]
    head = sources_at('HEAD', root)
    now = sources_now(root)
    mods = class_mods([now, head], root)
    from datetime import datetime, timezone
    when = datetime.now(timezone.utc).isoformat(timespec='seconds')
    found += [{'time': when, 'commit': None, 'subject': None, 'mod': mods.get(cls), 'class': cls, 'key': key, 'what': what,
               'before': before, 'after': after}
              for cls, key, what, before, after in changes(defaults_document(sources=head), defaults_document(sources=now))]
    if fresh:
        kept['format'] = HISTORY_FORMAT
        store.parent.mkdir(parents=True, exist_ok=True)
        store.write_text(json.dumps(kept), encoding='utf-8')
    texts = keyed_texts(root)
    for change in found:
        change['label'] = setting_label(change['key'], texts)
    return found


def report_history(argv, out=None):
    out = out or sys.stdout
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    try:
        found = settings_history()
    except subprocess.CalledProcessError as error:
        print(f'git could not read the history: {(error.stderr or "").strip()}', file=sys.stderr)
        return 2
    if '--json' in argv:
        json.dump(found, out)
        out.write('\n')
        return 0
    print(f'{len(found)} setting change(s) in the history of our settings:', file=out)
    for change in found:
        print(f"  {change['time'][:10]} {(change['commit'] or 'not committed')[:8]} {change['key']} {change['what']}", file=out)
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
    if argv[:1] == ['history']:
        return report_history(argv[1:], out)
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
