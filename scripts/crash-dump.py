"""Say what a Windows crash dump shows: the exception, and whether a call loop ran away.

A thin caller for phobos-modkit's crash-dump tool (the pinned submodule at external/phobos-modkit; its guide docs/crash-dump.md
there). With no file named it reads the newest RimWorld dump in Windows' CrashDumps folder. Reads only.

    python scripts/crash-dump.py                  the newest RimWorld dump
    python scripts/crash-dump.py FILE.dmp         one dump
    python scripts/crash-dump.py --help           every option
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

PROGRAM = 'RimWorld'


def arguments(argv):
    """The arguments to pass on: RimWorld's dumps unless a file or another program is named."""
    argv = list(argv)
    named = '--program' in argv or any(not a.startswith('-') for a in argv)
    return argv if named or '-h' in argv or '--help' in argv else ['--program', PROGRAM, *argv]


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.crashdump_cli import main as crash_dump
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return crash_dump(arguments(sys.argv[1:] if argv is None else argv), out=out)


if __name__ == '__main__':
    raise SystemExit(main())
