"""Find, outline and show C# declarations, and every line that uses a name, in the game's and other mods' decompiled
code (.local/decompiled) and our own source (src/, as the corpus "ours").

A thin caller for phobos-modkit's code tool (the pinned submodule at external/phobos-modkit; its guide is docs/code.md
there). Every folder and .cs file in .local/decompiled is a corpus named after it; indexes live in the kit's cache,
never in this repository, and rebuild when the files change. Decompiled output stays in .local/ (AGENTS.md); this
prints lines to the terminal only.

    python scripts/query-code.py show VehicleStatHandler.ApplyDamage --in "VehicleFramework*"
    python scripts/query-code.py refs HighlightedComponent           every use, by the member it is in
    python scripts/query-code.py outline VehicleComponent            a type's members with their lines
    python scripts/query-code.py find "*ApplyDamage*" --kind method  declarations by name
    python scripts/query-code.py summary                             the corpora and their sizes
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]


def corpus_arguments(root=ROOT):
    """--each-in for the decompiled folder when it exists, and our own source as "ours"."""
    arguments = []
    decompiled = Path(root) / '.local' / 'decompiled'
    if decompiled.is_dir():
        arguments += ['--each-in', str(decompiled)]
    arguments += ['--corpus', f'ours={Path(root) / "src"}']
    return arguments


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.code.cli import main as code
    argv = list(sys.argv[1:] if argv is None else argv)
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    if not any(a in ('-h', '--help') for a in argv):
        argv += corpus_arguments()
    return code(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
