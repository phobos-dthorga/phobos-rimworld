"""What art is still drawn in code, borrowed from another thing or another mod, or shipped without a record.

A thin caller for phobos-modkit's art-audit (the pinned submodule at external/phobos-modkit; its guide is docs/art-audit.md
there), run on this repository. config/art-audit.json says where the shipped textures are, how a record says a picture was made,
and which borrowings and drawn-in-code pictures are accepted, with their reasons. It reads file names, records, Defs and C#; it
never opens an image and writes nothing.

    python scripts/art-audit.py                          drawn in code, no record, borrowed pictures still open
    python scripts/art-audit.py report --all             accepted images and borrowings too, with their reasons
    python scripts/art-audit.py report --mod PhobosVehicles --kind code-drawn
    python scripts/art-audit.py check                    exit 1 when a shipped texture has no record
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
    from phobos_modkit.artaudit_cli import main as art_audit
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv)
    if not argv or argv[0] not in ('report', 'check', '-h', '--help'):
        argv = ['report'] + argv
    if argv[0] in ('report', 'check') and '--root' not in argv:
        argv = argv[:1] + ['--root', str(ROOT)] + argv[1:]
    return art_audit(argv, out=out)


if __name__ == '__main__':
    sys.exit(main())
