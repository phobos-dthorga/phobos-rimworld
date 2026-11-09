# Crash record: stack overflow on 9 Nov 2026

The owner's game closed suddenly at 23:33 on 9 November 2026, about five minutes after loading PG5. This records what the evidence shows, what it cannot show, and how to find the cause.

## Read this first

- **It was a stack overflow** (Windows exception `0xC00000FD`) on the game's main thread: code called itself until the thread ran out of stack. Windows' Application log and a crash dump both say so.
- **The culprit is not named yet.** The dump Windows made is a small one, so it has the shape of the failure and not the method names.
- **To name it: launch the game with its error output sent to a file, as below, and load PG5 again.** .NET writes the repeating methods there on a stack overflow. This was not known to be needed until now, and it has not been tried on this game.
- **Three things changed since the last session that ended normally**, and any of them could hold the loop (below). Nothing yet points at one more than the others.

## The evidence

| What | Found | Source |
| --- | --- | --- |
| Exception | `0xC00000FD` stack overflow, faulting module unknown, 23:33:41 | Windows Application log, events 1000 and 1001 |
| Dump | `RimWorld.exe.3964.dmp`, 72.8 MB, small (no full memory) | `%LOCALAPPDATA%\CrashDumps`, read with `python scripts/crash-dump.py` |
| **The loop** | **One level of 224 bytes repeated 6,843 times.** Each level holds three or four code addresses | `crash-dump` |
| **Same objects at every level** | Every object pointer in a level was identical from level to level, so the arguments never changed and it could not end by itself | `crash-dump` |
| Where the code is | Almost every address is code compiled at run time, so it is managed code, a mod's or the game's, and not a native library | `crash-dump` |
| Log | Nothing about it. The last line, a Combat Extended error at 23:33:27, is 14 seconds before Windows' report and shows no recursion | `Player.log` |

**Inference, not observation:** the 14-second gap may be the time Windows took to write the dump, or the game may have been stuck for part of it. A loop of three calls with unchanging arguments is the typical shape of two patches or properties calling each other.

## What changed since the last session that ended normally

The session of 19:41 to 22:15 ran for over two hours without this crash.

| Change | Detail | Why it is a suspect |
| --- | --- | --- |
| **The Dead Man's Switch - Synthriada** newly switched on | Updated 09 Nov 23:14, minutes before this session. Logs 165 lines here, none before, including types it cannot find (`DMSSR.DamageWorkers.DamageWorker_EmbryteResonance`) and failed patches | New code with visible load problems. Its author is active on the Workshop |
| **Prison Labor** updated | Updated 09 Nov 22:20, after the last session closed: its first 1.7 build. Its author wrote on 6 Nov that general tests were still needed. 85 Harmony patches | New code that patches many pawn and job methods |
| **D'thorga Framework 0.51.0 to 0.57.1** | Our six versions, first played in this session | Our new code paths were read for call loops and none was found. That is reasoning, not proof |

Other Workshop items changed the same evening (Turbo's Vehicle Features Library, RimArrow: Mechanized Vol.1) are subscribed but switched off, so they are ruled out.

## Other errors in this log, none of which is the crash

| Error | Count | Whose | What it means |
| --- | --- | --- | --- |
| **`FactoryWater.MayTick` null reference on the automated ammunition press at (215, 133)** | 1, then suppressed | **Ours** (Factory Additions) | The press holds a bill whose recipe no longer exists, and our water check reads it without looking. Nine saved bills name recipes that **Munitions Industries for VFE - Factory** no longer defines (`SEX_MakeInFactory_...`); it defines 3 such recipes now, and the log of the session before had none of these errors. A null guard would stop our error but the bill would stay broken: clearing it is the real fix |
| `Building_FactoryHopper.CheckForFactories` null reference on four hoppers around the press | 4 | Vanilla Furniture Expanded - Factory | Same place as the press; most likely the same broken bills (inference) |
| `More than 1 pipe comp in the same cell` | 6, at six cells | Dubs Bad Hygiene | **Checked against the save: not our hidden pipes.** Those cells hold the player's own plumbing (`sewagePipeStuff`), one with a water treatment building on it. No `sewagePipeHidden` is on them. The same cell (119, 213) gave this error before the pipes feature existed |
| `Notify_NewLink was called but the link is already here` | 43, at load only | Reported through the game's facility linking | At load, not at the crash. Not traced further |
| `RandomElementByWeight with totalWeight=0`, `StatRequest for null def` | 37 and 10 | Combat Extended's job givers | Noise that repeats; the last lines of the log are these. Not the crash, because an error here is caught and logged |

## How to name the culprit

1. In Steam, open **RimWorld, Properties, Launch Options**, and set: `cmd /c "%command% 2> "%LOCALAPPDATA%\rimworld-errors.txt""`
2. Start the game and **load PG5**, doing what you were doing when it crashed.
3. If it crashes again, say so. `%LOCALAPPDATA%\rimworld-errors.txt` should then hold `Stack overflow` and the repeating methods by name.
4. **Clear the launch option afterwards.**

Not yet tried on this game: the game's own build may change where that output goes. If the file comes out empty, the fallback is switching the two new or updated mods off one at a time.

## Who chose what

- **Owner (9 Nov 2026):** analyse the log, record it, and that crash dumps were an area worth covering in the toolkit.
- **Agent:** the analysis, the `crash-dump` tool (phobos-modkit 1.4.0, in `scripts/crash-dump.py`), and the suspects and their order.

## Sources

Windows Application log; `RimWorld.exe.3964.dmp` and `Player.log`, kept with this date in `.local/owner-test-logs/2026-11-09-stack-overflow/`; `python scripts/triage-log.py`; `python scripts/query-save.py` on PG5 for the pipe cells; `python scripts/query-patches.py`, `python scripts/workshop-research.py report` and `python scripts/query-defs.py` for the mods; our own source for the review of new code paths. Microsoft's published minidump structures for the format.
