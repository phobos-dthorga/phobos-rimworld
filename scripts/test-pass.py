"""What the in-game test list is waiting for: rows the game has seen working, and the owner's verdicts not yet written in.

A thin caller for phobos-modkit's test-pass (the pinned submodule at external/phobos-modkit; its guide is docs/test-pass.md
there), run on this repository. config/test-evidence.json names the test list and maps rows to D'thorga Framework's seen-in-play
notes (its setting "Note features as they are seen working", off by default). Verdicts are the owner's clicks on D'thorga
Companion's Tests page. A note never passes a row; only the owner's verdict does. Paths come from the options, the environment,
.local/install-settings.json or the defaults.

    python scripts/test-pass.py report               rows waiting for the owner, rows seen working, verdicts not yet written
    python scripts/test-pass.py apply                what would be written into the test list (a preview)
    python scripts/test-pass.py apply --apply        write it
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.testpass_cli import main as test_pass
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv) or ['report']
    extra = []
    if argv[0] in ('report', 'apply'):
        if '--root' not in argv:
            extra += ['--root', str(ROOT)]
        if '--settings' not in argv:
            extra += ['--settings', str(deploy.SETTINGS_FILE)]
    return test_pass(argv[:1] + extra + argv[1:], out=out)


if __name__ == '__main__':
    sys.exit(main())
