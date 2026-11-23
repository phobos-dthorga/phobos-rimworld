"""Steam Workshop publishing for this repository's mods: release notes, checks, staging, the owner's uploader, records.

A thin caller for phobos-modkit's steam-publish (the pinned submodule at external/phobos-modkit; its guide is
external/phobos-modkit/docs/workshop-publishing.md). The catalogue is config/workshop-publishing.json; each mod's page is
workshop/<Mod>/page.bbcode and its generated change notes workshop/<Mod>/releases/<version>.bbcode. D'thorga Companion's
Publish page runs the same commands.

    python scripts/workshop.py notes --check               release notes current? pages valid?
    python scripts/workshop.py notes --write               write the generated change notes
    python scripts/workshop.py status                      every mod: package, blockers, sizes, publication order
    python scripts/workshop.py show Framework              one mod's card: the change note Steam would get, tags, receipts
    python scripts/workshop.py prepare Framework --build   stage a checked candidate, offline
    python scripts/workshop.py upload Framework --create   OWNER ONLY: through the running Steam client (never an agent)

Agents never run upload or update (mods/AGENTS.md): --what-if at most.
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
    from phobos_modkit.steam.publish.cli import main as publish
    argv = list(sys.argv[1:] if argv is None else argv)
    if argv and not argv[0].startswith('-') and '--root' not in argv:
        argv[1:1] = ['--root', str(ROOT)]
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return publish(argv, root=ROOT)


if __name__ == '__main__':
    sys.exit(main())
