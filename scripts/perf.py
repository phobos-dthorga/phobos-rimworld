"""Where the running game spends its time: by part of the game loop, by the mod that caused it, by method, with garbage collection.

A thin caller for phobos-modkit's rimworld-perf (the pinned submodule at external/phobos-modkit; its guide is docs/rimworld-perf.md
there). It records RimWorld 1.7 from outside with Microsoft's dotnet-trace and dotnet-counters, which the first recording installs
into the kit's build folder (needs the .NET SDK). Nothing in the game is changed, but attaching can hitch it for a moment: **ask the
owner before recording their running game**, and ask them to save first. Paths come from the options, the environment, then
.local/install-settings.json.

    python scripts/perf.py record --label "many wounded"   30 seconds of the running game, then its report
    python scripts/perf.py report                          the newest recording again
    python scripts/perf.py mod "D'thorga Medical"          one mod: its busiest methods and where its time went
    python scripts/perf.py compare                         the last two recordings, as shares
    python scripts/perf.py list                            every recording and snapshot kept
    python scripts/perf.py stack --label "frozen"          a frozen or stuck game: every thread's stack, 3 times, 2 s apart
    python scripts/perf.py heap --label "day 40"           what holds the game's memory, by type and by mod (pauses the game)
    python scripts/perf.py heap-compare                    the last two heaps: who grew
    python scripts/perf.py watch --minutes 120             the game's memory over time, without pausing it; Ctrl+C ends it
    python scripts/perf.py watch-report                    the newest watch again

Ask the owner before stack and heap too: heap forces a full garbage collection, so the game pauses until it is done.
"""
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import _modkit  # noqa: E402  (the shared tools' import path)
import deploy  # noqa: E402  (shared path settings)


def main(argv=None, out=None):
    try:
        _modkit.ensure()
    except _modkit.MissingModKit as missing:
        print(missing, file=sys.stderr)
        return 2
    from phobos_modkit.games.rimworld.perf_cli import main as rimworld_perf
    if out is None and hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    return rimworld_perf(argv, settings_file=deploy.SETTINGS_FILE, out=out)


if __name__ == '__main__':
    sys.exit(main())
