"""Safe copies of what cannot be made again: the style-model trainings, the models made from them, and the art notes.

A thin caller for phobos-modkit's safe-copies (the pinned submodule at external/phobos-modkit; its guide is docs/safe-copies.md
there), run with this repository named, so it reads config/safe-copies.json and this repository's own settings. The owner's
choices (21 Nov 2026): copies go to K: (safeCopiesPath, marked private and local) and to the private repository
dthorga-safe-copies (safeCopiesRepositoryPath), pushed after every run. A training made from the game's art goes to K: only.
art-run.py runs it after every night; D'thorga Companion's Copy now runs it at any time. No graphics card is used.

    python scripts/safe-copies.py                    status: each set, what is copied, changed since, or never copied
    python scripts/safe-copies.py plan               what a run would copy, and what each destination refuses and why
    python scripts/safe-copies.py run                copy what changed; commit and push the repository's copy
    python scripts/safe-copies.py verify             hash every copy again against its record
    python scripts/safe-copies.py restore --set lora-trainings --unit house-v1 --to D:/restored/house-v1
    python scripts/safe-copies.py run-all --verify    every list on the owner's games list, then every copy checked (kit 1.71.0)
    python scripts/safe-copies.py schedule           the daily run: how it stands (--at HH:MM sets it, --off removes it)

RimWorld's own files (the saves, the Config folder, RimSort's lists) are in the shared tools' list for the game since kit 1.71.0:
add --root external/phobos-modkit --game rimworld to read it.
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)

ROOT = Path(__file__).resolve().parents[1]
REPORTS = ('plan', 'run', 'status', 'verify', 'restore')


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.safe_copies import main as safe_copies
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    argv = list(sys.argv[1:] if argv is None else argv) or ['status']
    if argv[0] in REPORTS and '--root' not in argv:
        argv = argv[:1] + ['--root', str(ROOT)] + argv[1:]
    return safe_copies(argv, out=out)


if __name__ == '__main__':
    sys.exit(main())
