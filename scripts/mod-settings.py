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
"""
from pathlib import Path
import json
import os
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _csharp  # noqa: E402  (our settings classes' defaults)
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]
OUR_MODS = 'phobosdthorga.*'
CHOOSERS = ('--mod', '--all', '--list', 'snapshot', 'compare')


def defaults_document(root=ROOT):
    """{schemaVersion, classes: {settings class: {key: {default, initial, field}}}} from every ModSettings class in src/."""
    sources = {p.relative_to(root).as_posix(): p.read_text(encoding='utf-8')
               for p in Path(root, 'src').rglob('*.cs') if 'obj' not in p.parts and 'bin' not in p.parts}
    classes = _csharp.read_settings_classes(sources)
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
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.settings_cli import main as rimworld_settings
    argv = list(sys.argv[1:] if argv is None else argv)
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
