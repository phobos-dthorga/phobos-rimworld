"""Where the public source of each mod ours tie into lives, checked against the mod's package ID.

A thin caller for phobos-modkit's rimworld-sources (the pinned submodule at external/phobos-modkit; its guide is
docs/rimworld-sources.md there). The answers are kept in config/source-repos.json: read that first, and run this when a
mod is not in it, is unconfirmed, or a new tie-in has been added. Keyless and paced: GitHub allows 60 calls an hour without
logging in, answers are cached for a week, and a run that meets the limit says when to run again.

    python scripts/source-repos.py               every tied mod; confirmed ones come from the register
    python scripts/source-repos.py --write       look up and record the answers in config/source-repos.json
    python scripts/source-repos.py --mod some.packageid --only-named   one installed mod
    python scripts/source-repos.py --recheck --write   look again at mods already confirmed
    python scripts/source-repos.py --clone       lean clones of confirmed repositories, and what they cost
    python scripts/source-repos.py --clone --update-clones   bring them up to date too

Clones go to sourceClonesPath in .local/install-settings.json (PHOBOS_SOURCE_CLONES overrides it; default .local/decompiled).
On the owner's PC that is K:\\phobos-modkit\\sources (owner's choice, 18 Nov 2026: drive D: was at 2% free); query-code.py
indexes it beside .local/decompiled.

The clones are checked on every refresh-mod-list.py run (watch_line): their total size and the drive's free space. The tool
never deletes a clone; when space runs short it says so and lists the choices, and the owner decides (AGENTS.md).
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)

ROOT = Path(__file__).resolve().parents[1]
CLONES = deploy.local_path('sourceClonesPath', 'PHOBOS_SOURCE_CLONES', ROOT / '.local' / 'decompiled')


def watch_line():
    """One line on the source clones and their drive, with a call for a decision when space runs short or they pass
    their budget. Reads sizes only; never deletes."""
    _modkit.ensure()
    from phobos_modkit import source_clones as sc
    clones = sc.existing(CLONES)
    total = sum(size for _, size in clones)
    free, size = sc.drive(CLONES)
    line = (f'Source clones: {len(clones)}, {total / 1e9:.2f} GB; their drive has {free / 1e9:.1f} GB free '
            f'({free / size:.0%}).')
    if sc.short_of_space(free, size) or total > sc.BUDGET_BYTES:
        line += ' Storage needs a decision (nothing deleted): run python scripts/source-repos.py --clone for the choices.'
    return line


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.sources_cli import main as rimworld_sources
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    arguments = list(sys.argv[1:] if argv is None else argv)
    if ('--clone' in arguments or '--update-clones' in arguments) and '--clone-into' not in arguments:
        arguments = [a for a in arguments if a != '--clone'] + ['--clone-into', str(CLONES)]
    if '--root' not in arguments:
        arguments += ['--root', str(ROOT)]
    return rimworld_sources(arguments, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
