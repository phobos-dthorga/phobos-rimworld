"""The tables in our docs that are made from the repository's own files: its C# projects and the methods they patch
(docs/development/building.md), and what each mod needs (docs/player-guide/getting-started.md).

A thin caller for phobos-modkit's doc-tables (the pinned submodule at external/phobos-modkit; its guide is docs/doc-tables.md
there), run on this repository. config/doc-tables.json says which table goes in which doc and what it reads. It reads project
files, C# source and the mods' About.xml and LoadFolders.xml; it writes only the generated tables, and only with --write.

    python scripts/doc-tables.py            check: exit 1 when a table is missing or out of date (a gate step)
    python scripts/doc-tables.py --write    write the tables after changing a project, a patch or a mod's About.xml
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.doc_tables import main as doc_tables
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv)
    if '--root' not in argv:
        argv = ['--root', str(ROOT)] + argv
    return doc_tables(argv, out=out)


if __name__ == '__main__':
    sys.exit(main())
