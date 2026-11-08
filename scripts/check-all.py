"""Every check before a commit, printing only what failed: builds, rule tests, Python tests, links, layout, text keys,
vehicle schemas, version copies and changelogs.

A thin caller for phobos-modkit's gate (the pinned submodule at external/phobos-modkit; its guide is docs/gate.md
there). The steps are in config/gate.json: every project under src/ and every test project under tests/ is found by
glob, so a new one is checked without editing the list. Exit 0 only when everything passed, so a commit can be chained
on it. A failed step's full output is kept in the kit's cache; the report says where.

    python scripts/check-all.py                         everything
    python scripts/check-all.py --only "test *"         the rule tests only (wildcards * and ?)
    python scripts/check-all.py --skip "build *" --skip "test *"   the Python checks only
    python scripts/check-all.py --list                  the steps, without running them
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]


def main(argv=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.gate.cli import main as gate
    argv = list(sys.argv[1:] if argv is None else argv)
    if '--root' not in argv:
        argv = ['--root', str(ROOT)] + argv
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return gate(argv)


if __name__ == '__main__':
    sys.exit(main())
