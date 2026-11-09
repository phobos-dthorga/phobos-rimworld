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
    python scripts/query-code.py compare OLD.dll NEW.dll             what changed between two builds, member by member
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]
ILSPYCMD = '.store/ilspycmd/*/ilspycmd.win-x64/*/tools/any/win-x64/ilspycmd.exe'


def corpus_arguments(root=ROOT):
    """--each-in for the decompiled folder when it exists, and our own source as "ours"."""
    arguments = []
    decompiled = Path(root) / '.local' / 'decompiled'
    if decompiled.is_dir():
        arguments += ['--each-in', str(decompiled)]
    arguments += ['--corpus', f'ours={Path(root) / "src"}']
    return arguments


def compare_arguments(given=(), root=ROOT, game=None):
    """For compare: our local ilspycmd (its .cmd wrapper fails on this repository's path) and the game folder as references."""
    arguments = []
    if '--decompiler' not in given:
        found = sorted((Path(root) / '.local' / 'tools' / 'ilspy').glob(ILSPYCMD))
        if found:
            arguments += ['--decompiler', str(found[-1])]
    game = deploy.game_path() if game is None else Path(game)
    if '--reference' not in given and game.is_dir():
        arguments += ['--reference', str(game)]
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
        if argv[:1] == ['compare']:
            argv += compare_arguments(argv)
    return code(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
