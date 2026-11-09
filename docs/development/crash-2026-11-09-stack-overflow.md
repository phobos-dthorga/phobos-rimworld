# Crash record: stack overflow on 9 Nov 2026

The owner's game closed suddenly at 23:33 on 9 November 2026, about five minutes after loading PG5. This records what the evidence shows, what it cannot show, and how to find the cause.

## Answer (10 Nov 2026): it is Prison Labor, and not our code

**Named by the launch option's error output, the fourth time it crashed.** The trace repeats one pair of frames 6,845 times:

```text
PrisonLabor.HarmonyPatches.Patches_Food.GetFoodRestrictionPatch.Postfix
RimWorld.Pawn_FoodRestrictionTracker.GetCurrentRespectedRestriction (patched)
```

**The bug, read from Prison Labor's 1.7 build and the game's code:** the postfix runs on `GetCurrentRespectedRestriction`. When the result is null, the pawn is a prisoner of the colony who is not in a mental state, and their motivation is above 0.7 or they are being watched, it returns `__instance.GetCurrentRespectedRestriction(null)`, which is the method it is patching. For a prisoner the game answers a null asker with null, because the pawn is not the player's, so the postfix runs again with the same arguments, for ever.

**What sets it off:** a warden looking for a job. The bottom of the trace is `WorkGiver_Warden_DeliverFood`, then Prison Labor's `FoodAvailableForPrisoner`, then `FoodUtility.WillEat`. That is why it came a couple of minutes after each load: it needs a warden to consider feeding a motivated prisoner.

**Prisoner Realism is not at fault.** Its postfix is at the very top of the trace only because it was running when the stack ran out. Everything earlier in this record is kept as the path that led here: Synthriada, the save bridge, REA, our own code and the pipes are all cleared.

### The temporary guard (Patches 0.16.5, owner, 10 Nov 2026: "strictly temporary")

`src/PhobosPatches.PrisonLabor`, a separate assembly loaded only with Prison Labor. A prefix on Prison Labor's postfix lets it run once and hands back the game's answer from any nested call, so a prisoner in that case ends up with no food policy, which is the game's own answer. The rule that another author's bug is theirs applies, and this is its exception for extreme errors: Prison Labor's project is active (updated 9 Nov 2026), and a crash to desktop with no log entry is extreme. The owner has reported it.

**Holding, 10 Nov 2026:** the owner reports it holds. The load line shows the guard on with Framework 0.58.0, no dump has been written since, and PG5 ran for 15 minutes after loading where it had crashed within five every time.

**Lets the player:** keep Prison Labor and keep playing. **Stops:** only the endless call. **Changes:** nothing else.

**How it comes out.** At each start the guard reads Prison Labor's code. When the load line says `temporary guard against Prison Labor's food policy loop` is off because Prison Labor no longer calls itself or has that patch, the fix has shipped. Check with `python scripts/workshop-research.py report avius.prisonlabor` for a newer update, then:

1. Delete `src/PhobosPatches.PrisonLabor/` and `mods/PhobosPatches/Mods/PrisonLabor/`.
2. Remove its line and comment from `mods/PhobosPatches/LoadFolders.xml`.
3. Remove its two entries from `config/maintained-constants.json` and its row from `docs/development/building.md`.
4. Remove the README's gotcha line, add a changelog entry, close item J2 in `outstanding-work.md`, and take a patch-number version.

### Report for the author

The owner posted a bug report on the mod's Workshop page on 10 Nov 2026. The agent's draft, which may differ from what was posted:

> **Prison Labor 1.7: stack overflow crash from GetFoodRestrictionPatch.** `GetFoodRestrictionPatch.Postfix` calls `__instance.GetCurrentRespectedRestriction(null)`, the method it patches. For a prisoner the game returns null to a null getter, so the postfix runs again with the same arguments until the stack overflows and the game closes with no log entry. It happens when a warden checks food for a prisoner with motivation above 0.7, or one being watched. `Patch_EnableRespectingFoodPolicies` beside it returns `__instance.CurrentFoodPolicy`, which may be what this one meant to do. Captured with `RimWorld.exe 2> errors.txt`: 6,845 repeats of that postfix and the patched method.

## Correction (10 Nov 2026): the "ghost bills" were not ghosts

Below, this record says Munitions Industries no longer defines nine recipes. **That is wrong.** Munitions Industries builds those recipes at startup, inside a postfix on Combat Extended's `AmmoInjector.Inject`, and the postfix never ran because `Inject` threw. With The Dead Man's Switch - Synthriada on, its `1.7/CE/Patches/AmmoSets.xml` adds ammo links to `Ammo_556x45mmNATO_Embryte` and `Bullet_556x45mmNATO_Embryte`, which nothing defines. Those links load as null, Combat Extended's vehicle compatibility passes one to `ThingFilter.SetAllow`, and the whole ammo injection dies.

| Session | Synthriada | `SetAllow` exception | Factory recipes added |
| --- | --- | --- | --- |
| 9 Nov 19:41 | Off | 0 | 495 |
| 9 Nov 23:46 | On | 1 | 0 |
| 9 Nov 23:52 | Off | 0 | 495 |
| 10 Nov 01:41 | On | 1 | 0 |

So Factory Additions 0.12.1 removed nine bills that were valid but for this fault. **The owner's saves of 10 Nov 02:02 (PG3, PG4 and PG5) no longer hold them.** `Autosave-16` (9 Nov 21:40) still does, which is where this list comes from. They were all on the automated ammunition press, and must be added again by hand once the recipes return:

| Makes | Repeat | Target |
| --- | --- | --- |
| 105mm howitzer shell, HE (H fuzed) | Do X times | 1 |
| High-explosive shell | Do until X | 96 |
| 7.62x51mm NATO, HE | Do until X | 2,500 |
| 30x165mm, HE | Do until X | 2,500 |
| 57mm rocket, HEAT | Do until X | 250 |
| 20x102mm NATO, HE | Do until X | 2,500 |
| 12.7x108mm, HE | Do until X | 2,500 |
| 30x165mm, AP | Do until X | 2,500 |
| .50 BMG, HE | Do until X | 2,500 |

**Fixed the same day:** Patches 0.16.7 drops the empty entries so the ammo setup finishes with Synthriada on, and Factory Additions 0.12.2 sets such bills aside instead of removing them.

## Second crash, 23:51 the same night

The game crashed again about two minutes after loading the same save, with **the same fault: a 224-byte level repeating 6,841 times, every level holding the same objects** (`crash-dump` on `RimWorld.exe.63208.dmp`). The first was 6,843 times, about five minutes after loading. So it is one repeatable bug, not chance. The owner suspects The Dead Man's Switch - Synthriada, which remains the leading suspect but is not proven.

- **The save bridge's Convert did nothing to the colony** in the session logged: "Body parts re-numbered: none. Hediff classes upgraded: none", and it found no hediffs on wrongly numbered parts. It asks because Synthriada changes the numbering of some bodies, and PG5 was saved without it. **No save has been written since 22:13**, so PG4 and PG5 are intact.
- **The ghost bills also throw while drawing:** 679 `Exception drawing` errors on the same press in the second session, from Vanilla Expanded's bar drawing. Not the crash, but real noise.
- Synthriada's two assemblies were decompiled and scanned for members calling themselves. The matches are overloads passing on to one another; no loop was found. A scan of this kind cannot rule one out, and Synthriada adds no Harmony patches.
- **The decisive test:** switch Synthriada off and load PG5 for ten minutes. If the crash stops, it is the cause or part of it. Its author is active and asks for a log and mod list in the bug reports section.

## Third crash, 23:56, with Synthriada switched off

The owner switched The Dead Man's Switch - Synthriada off, and the game crashed again about two minutes after loading PG5: **the same 224-byte level, 6,845 times, the same objects at every level** (`RimWorld.exe.48584.dmp`). **Synthriada is cleared.** The save bridge was inert in this session, so it is cleared too.

What is left is **Prison Labor's new 1.7 build** and **our Framework 0.52.0 to 0.57.1**, both first run in the session of the first crash. The owner believes it is ours.

- Our changed C# was reviewed for call loops by reasoning, and Dubs Bad Hygiene's plumbing net was read for a loop through our hidden pipes: none found. That is not proof.
- **Inference:** each level is only 224 bytes with three or four calls, so these are very small methods, such as properties calling each other. Our new pipe code goes through the game's spawn and destroy paths, whose frames are far larger, which counts against it but does not exclude it.
- **Inference:** the crash comes a short, similar time after each load of the same save, so something scheduled at a fixed moment of game time is a likely trigger.
- **Next:** the launch option below names the methods in one two-minute reproduction, without guessing or switching anything off.

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

## Checked and set aside: REA running alongside

The owner's theory: the newly installed REA, run by another session at about the same time, took a huge share of the machine's resources. Checked on 9 Nov 2026, and it does not fit, with one small doubt left.

| Test | Result |
| --- | --- |
| Was REA loaded in the game? | No. The dump lists 203 modules, all from the game, Windows, Steam, Defender and OBS's capture hook |
| Did the machine run short? | No sign of it: no resource-exhaustion or paging events in Windows' System log from 22:30 to 23:50, on a machine with 63 GB of RAM. The page file's peak use is 252 MB |
| Did the stack run out for lack of memory? | No. `RimWorld.exe` reserves 1,572,864 bytes for its main thread and 1,548,320 were in use, so the thread used essentially its whole allowance and hit its real limit |
| Can a shortage make code call itself? | Not by itself: 6,843 levels with identical objects is program logic. A shortage produces failed allocations, stutter or an out-of-memory error |

**The doubt (inference):** another program's load could change timing, and timing could steer a mod's logic into a loop that already exists. REA would then be the trigger at most, with the loop still in a mod or the game. Nothing here shows that happened. How much memory REA used at that moment was not recorded; another session measured 300 to 400 MB and about 3 seconds for each of seven .NET runs.

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

Windows Application log; both dumps and the second crash's log, kept in `.local/owner-test-logs/2026-11-09-stack-overflow/` (the first crash's `Player.log` was lost when the game rotated its logs: only the triage quoted here remains); `python scripts/triage-log.py`; `python scripts/query-save.py` on PG5 for the pipe cells; `python scripts/query-patches.py`, `python scripts/workshop-research.py report` and `python scripts/query-defs.py` for the mods; our own source for the review of new code paths. Microsoft's published minidump structures for the format.
