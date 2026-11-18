"""Say what a Windows crash dump shows: the exception, whether a call loop ran away, and in a full dump the methods.

A thin caller for phobos-modkit's crash-dump tool (the pinned submodule at external/phobos-modkit; its guide docs/crash-dump.md
there). With no file named it reads the newest RimWorld dump in Windows' CrashDumps folder. In a dump holding the game's full
memory it names the methods on the failing stack with Microsoft's dotnet-dump (installed on first use; needs the .NET SDK).

    python scripts/crash-dump.py                  the newest RimWorld dump
    python scripts/crash-dump.py FILE.dmp         one dump
    python scripts/crash-dump.py setup            whether Windows keeps full dumps of RimWorld, and the lines that ask it to
    python scripts/crash-dump.py setup --apply    ask it to (the owner, in an administrator window; agents never run this)
    python scripts/crash-dump.py setup --off      back to Windows' default small dumps for RimWorld
    python scripts/crash-dump.py --help           every option
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

PROGRAM = 'RimWorld'
SETUP = 'setup'


def arguments(argv):
    """The arguments to pass on: RimWorld's dumps unless a file or another program is named."""
    argv = list(argv)
    named = '--program' in argv or any(not a.startswith('-') for a in argv)
    return argv if named or '-h' in argv or '--help' in argv else ['--program', PROGRAM, *argv]


def setup_arguments(argv):
    """windows-dumps' arguments for `setup`: RimWorld unless another program is named."""
    argv = list(argv)
    return argv if '--program' in argv or '-h' in argv or '--help' in argv else ['--program', PROGRAM, *argv]


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv)
    if argv[:1] == [SETUP]:
        from phobos_modkit.windows_dumps import main as windows_dumps
        return windows_dumps(setup_arguments(argv[1:]), out=out)
    from phobos_modkit.crashdump_cli import main as crash_dump
    return crash_dump(arguments(argv), out=out)


if __name__ == '__main__':
    raise SystemExit(main())
