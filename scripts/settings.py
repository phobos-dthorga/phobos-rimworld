"""Every path the tools and our scripts read, its value, and where that value comes from; and setting one centrally.

A thin caller for phobos-modkit's settings (the pinned submodule at external/phobos-modkit; its guide is docs/settings.md there),
run with this repository named, so the settings our own scripts declare (config/settings.json) and our own values
(.local/install-settings.json) are shown too. Order: an argument, an environment variable, this repository's own file, the central
settings file (D'thorga Companion's settings.json, for every repository), the default. set writes only the central file.

    python scripts/settings.py                                         every setting and where its value comes from
    python scripts/settings.py set --key historyPath --value K:/phobos-modkit/history
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]
REPORTS = ('list', 'set')


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.settings_cli import main as settings
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv) or ['list']
    if argv[0] in REPORTS and '--root' not in argv:
        argv = argv[:1] + ['--root', str(ROOT)] + argv[1:]
    return settings(argv, out=out)


if __name__ == '__main__':
    sys.exit(main())
