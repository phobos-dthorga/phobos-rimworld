# Analysis tooling: proposals (7 Nov 2026)

**Answer first.** Agent sessions spend most of their reading on three things: the game's log, the save, and
the Defs of about 340 installed mods. Eight deterministic tools would answer the usual questions about them
in a few hundred words of summary instead. They are built in a new shared repository,
[phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit), because most of the code is not
specific to RimWorld and the owner's other game repositories carry drifting copies of the same scripts.

**Gotchas**

- **All eight tools are built (17 Nov 2026), and a ninth.** On 7 Nov 2026 log triage (tool 1) shipped as `scripts/triage-log.py`,
  the save index (tool 2) as `scripts/query-save.py` (map grids and save comparison came later), the mod and Def index (tool 3) as
  `scripts/query-defs.py`, and Workshop research (tool 5) as `scripts/workshop-research.py`. On 8 Nov 2026 followed the assembly
  reader's Harmony part (tool 4) as `scripts/query-patches.py`, StartupImpact sessions (tool 6) as `scripts/startup-impact.py`, the
  version readiness scan (tool 7) as `scripts/version-readiness.py`, and release tooling (tool 8) as `scripts/release.py`. Crash dumps
  (tool 9) came on 9 Nov 2026 as `scripts/crash-dump.py`. Each section below says what was built and what was left out.
- **Beyond this record:** `python scripts/check-targets.py` checks every Harmony patch and reflected lookup named in a string against
  the game's and the active mods' assemblies (part of the gate), and `python scripts/cleanup.py` lists what the tools, builds and
  tests left behind, for the owner to remove with `--apply`; `python scripts/art-audit.py` (section 11) says which shipped art is drawn
  in code, borrowed or unrecorded. Also `scripts/query-code.py` (the game's and other mods' code) and
  `scripts/mod-settings.py` (the owner's settings); AGENTS.md says which tool answers which question.
- **A tool saves nothing until an instruction points at it.** Each tool's first change here adds a one-line
  rule to `AGENTS.md` ("before reading a log, run ..."), in the same commit.
- **Save tools always read a frozen copy** in the session's scratchpad and delete it afterwards, as
  `AGENTS.md` requires. They never write to the Saves folder.
- **Fixtures are written by hand** from what we know of the formats, never cut from the owner's saves or logs.
- Timings below come from the owner's high-end PC: treat them as a best case.

**Who decided what.** Owner choices (7 Nov 2026): build tools to save LLM work; put truly generic code in
its own GitHub repository; name it `phobos-modkit`; make it public under MIT. Agent choices, open to
revision: which tools, their order, the language of each, and that game adapters live in phobos-modkit.

## Why: what the sessions keep doing by hand

Surveyed on 7 Nov 2026 across `docs/development/`, `scripts/` and the ignored `.local/tools/`.

| Job | How often | Tooling today |
| --- | --- | --- |
| Reading `Player.log` for a reported fault | Almost every report ([save bridge](medical-save-bridge.md), [missile patches](missile-patches.md), the 1.7 readiness check) | None. A `grep -c` loop pasted into the save bridge record |
| Reading a save for a diagnosis (injuries by load ID, burn fractions, gas cells, belts, building positions, research) | Most design records since late October | Five throwaway readers in `.local/tools/`, each reading the save differently |
| Finding which mod defines or patches a Def | Every compatibility question | `scripts/check-factory-patches.py` for one case; otherwise searching by hand |
| Finding which mods patch a game method, or a constant in a mod's DLL | Several records | Decompiling, or `.local/tools/us_strings.py` |
| Reading a mod's latest Workshop comments before suggesting it | Every suggestion (`AGENTS.md` rule) | None; Steam rate-limits quick reads |
| Load-time history from Loading Progress's sessions | Twice | None |
| Checking which mods declare the new game version | Once per version | None; the 1.7 scan was one-off |

What the inputs look like on the owner's machine (7 Nov 2026):

| Input | Size |
| --- | --- |
| A live save (PG1, PG2) | 293 MB each; autosaves 290 to 345 MB, about three times October's size |
| One full streaming read of a save in Python (`ElementTree.iterparse`) | 7.6 s: 8.18 million elements, 78,237 things, 230 pawns |
| `Player.log`, `Player-prev.log` | 1.6 MB, 4.7 MB; one kept broken-session log is 37 MB |
| Loading Progress's StartupImpact sessions | 11 files, 6.3 MB |

Duplicated logic in this repository alone: mod and `About.xml` scanning (twice), `LoadFolders.xml` handling
(twice, and each copy misses a condition the other handles: `IfModNotActive` in one, `IfModActiveAll` in the
other), and `ParentName` inheritance (twice).

## The tools, in order of expected saving

| # | Tool | Replaces | Language |
| --- | --- | --- | --- |
| 1 | Log triage | Reading 2 to 40 MB logs | Python |
| 2 | Save index and query | Five throwaway save readers; by-hand save comparisons | Rust |
| 3 | Mod and Def index | Searching 340 mods; three duplicated loaders | Python |
| 4 | Assembly metadata reader | Decompiling to find patches and constants | C# |
| 5 | Workshop research | Reading comment pages by hand | Python |
| 6 | StartupImpact sessions | Hand-tallied load times and mod-list history | Python |
| 7 | Version readiness scan | The one-off 1.7 scan | Python |
| 8 | Release tooling (changelogs, release notes, doc links, layout rules) | Copies of the same scripts in several repositories | Python |

### Why these languages

- **Python by default.** The inputs are small, this repository's tools and tests are already standard-library
  Python, and the owner's other repositories can use them without a new toolchain.
- **Rust for the save indexer only.** The 7.6 s Python read is paid on every question, and saves tripled in a
  month. Reading a save once into a SQLite index and then asking many questions needs a fast reader; one
  self-contained program suits a Windows PC with no runtime to install. C# would be a close second, but the
  tool never touches game assemblies, so .NET brings nothing. Phobos Scope already has the Rust conventions.
- **C# for assembly metadata only.** .NET's own `System.Reflection.Metadata` reads an assembly's tables
  without loading it, which keeps the rule against running other authors' code outside the game. Ostranauts'
  `apicheck.ps1` already uses it. A Rust version would need a hand-written metadata parser for no gain.

### 1. Log triage

- **Reads** `Player.log`, `Player-prev.log` or any kept log, opened for shared reading (the file lists as
  0 bytes while the game runs).
- **Groups** messages into signatures after stripping numbers, load IDs, coordinates and hashes, with count,
  first and last time, level and category, and compares them with the previous run: new, gone, grown.
- **RimWorld knowledge:** 1.7 lines carry a timestamp, a padded level and a `[Category]`; 1.6 lines carry
  none. It maps the first stack frame's namespace to a mod (through tool 3), notes where Verse stopped
  logging ("Reached max messages limit") and counts the engine's `[NativeAllocationRuntime]` warnings after
  it, reads "Loading game from file ... with mods", and measures load time. It flags known traps, such as
  Search and Rescue's finalizer rethrow that hides an exception's real origin.
- **Writes** a Markdown summary (top 20 signatures and the comparison) and full JSON.
- The owner's FS25 repositories already have a working log triage; it becomes the second game adapter.

### 2. Save index and query

- **`build`** reads the save once and writes a SQLite index, driven by a schema file that says which
  elements become which tables. RimWorld's schema covers things (load ID, def, class, map, position, stack,
  faction, holder), pawns, hediffs (pawn, def, body part, severity), building comps, storage groups,
  research and study state, world objects, factions and goodwill, the save's mod list, the game version and
  ticks, and each map's grids (terrain, roof, gas, pollution) decoded from their packed form.
- **`query`** ships ready-made reports: `schema` (where things are, with counts, so a session needn't read
  the file to find out), `things`, `pawns --hediffs`, `buildings`, `storage`, `research`, a grid count with
  an ASCII or PNG picture, and `diff` between two saves by load ID (the hediff-by-hediff comparison the
  [save bridge](medical-save-bridge.md) did by hand).
- **Format facts it needs,** already checked against real saves: packed grids are base64 of raw deflate
  holding little-endian 16-bit values; short Def hashes are the game's stable string hash modulo 65,535.
- **Working slice first:** things, pawns, hediffs, the game header, the mod list, `schema` and `things`.
  Grids and `diff` come second.
- **Grids and `diff` built 7 Nov 2026** (phobos-modkit 0.6.0). Read from the 1.7 game code first: grids are base64
  of raw deflate (plain when not smaller), cells row by row; terrain, roofs and deep resources hold 16-bit Def numbers;
  gas holds four 8-bit densities per cell; fog and pollution are bits. Naming Defs needed the game's short-hash rule
  and, for terrain, the Defs the game makes in code: with rough, hewn and smooth stone and carpets added to the Def
  index, terrain on the owner's maps went from 70 to 98 or 99 percent of cells named. `diff` matches things by ID and
  hediffs by load ID; PG2 against the 18:50 autosave took about 4 s.

### 3. Mod and Def index

- One library for the active mod list and order, each mod's loaded files (lifting
  `files_the_game_loads` from `scripts/check-mod-layout.py`), `LoadFolders.xml` with all three conditions and
  the 1.7-then-1.6 fallback, `About.xml`, assemblies, and `ParentName` inheritance.
- Commands: `who <defName>` (owning mod and file, and every patch whose path names it), `list --type`,
  `show <defName>` (patches not applied, and labelled so), `patches --target`.
- Written as SQLite with a provenance table in the kit's cache, like the save index (not `.local/cache/`, which is
  cloud-synced). The save index's mod list and this index's Defs can then be joined in one query.
- **Built 7 Nov 2026** (phobos-modkit 0.4.0, `scripts/query-defs.py`). Two choices changed while building, both
  toward the game's own behaviour, read from the 1.7 game code: the index marks which definition the game keeps
  (a later mod's Def replaces an earlier mod's of the same type and name; a second copy inside one mod is skipped),
  and `show` prints each parent the game would use (the same Name from the latest mod at or before the child's)
  rather than merging them into one Def. Merging was left out: the chain is exact, a merge would be our model.
- On the owner's install: 336 mods, 42,418 Defs and 15,208 patch operations index in about 8 s; 165 names are
  defined by more than one mod. Rebuilds on a change to ModsConfig.xml, Steam's Workshop manifest, the game's
  version file or any XML in the game's Mods folder.
- Its biggest limit, unapplied patches, has a proposed cure: the resolved-Defs dump below.
- `scripts/check-factory-patches.py` and `scripts/refresh-mod-list.py` move onto it when next touched.

### 4. Assembly metadata reader

- `harmony` lists every `[HarmonyPatch]` attribute's target, so "which active mods patch this method" is one
  command over every `Assemblies` folder.
- `types`, `members`, `strings` (each string literal and the instructions after it, replacing
  `us_strings.py`), and `apicheck` (does a consumer still match its provider).
- **Limit:** patches applied in code rather than by attribute need a decompile, and the tool says so.

**Built 8 Nov 2026, the `harmony` part** (owner's go-ahead): phobos-modkit 0.14.0's `rimworld-patches`, called through
`scripts/query-patches.py`. Better than this plan's limit: `harmony.Patch` calls are read from the IL too (the original
looked up through `AccessTools`, the patch methods passed to `HarmonyMethod`), and a `TargetMethod`'s target from its code.
On the owner's 254 active mods: 5,356 patches in 333 assemblies, read in 6 s. `types`, `members` and `strings` are left to
the code index for decompiled C#, since REA was evaluated on 9 Nov 2026 and cannot read the game's assembly (see the REA row
under Shared frameworks); `apicheck` is not built.

### 5. Workshop research

- Details through Steam's keyless `GetPublishedFileDetails`; the latest 50 comments from the comments page;
  about 8 s between requests, a cache with fetch dates, and a pause on HTTP 429.
- Per mod: last update, supported versions, the author's last reply, comments in the last 90 days, and lines
  that mention crashes, errors, breakage or the current game version.
- **Limit:** Workshop item pages are region-blocked here, so required items come from the description and
  comments. Ostranauts' `pull-workshop.py` is the first code to lift.
- **Built 7 Nov 2026** (phobos-modkit 0.5.0, `scripts/workshop-research.py`). Found while building: the item's
  comments page was rate-limited first thing, but the comment-render endpoint the page itself loads from worked,
  and gives the same markup with each commenter's account number, so the uploader's replies can be told apart.
  Items whose uploader's profile is private give no comments to signed-out visitors (Vehicle Framework's do not);
  the report names them. After a 429, 90 s of waiting was not enough and 150 s was, so the tool waits 150 s once.
  Its `versions` report found 209 of the owner's 323 active Workshop mods declaring 1.7 in their Steam tags.
- **Collections and descriptions, 9 Nov 2026** (phobos-modkit 1.2.0, `collection <ID or URL>`, `--grep`): a Steam
  collection's members in its order, each with its versions and the opening of its description, from the keyless
  `GetCollectionDetails` and `GetPublishedFileDetails` APIs; the cache now keeps descriptions (`item.description`) and
  members (table `collection`), so `sql` can ask across them. Found while building: the item pages at
  `steamcommunity.com/sharedfiles/filedetails` answer curl and the web-fetch agent with an error page or HTTP 429
  whatever the headers, while both APIs answer freely, so the gap check for the military vehicles record
  (`military-vehicles-research.md`) read the three Vehicle Framework collections (277 items) through the APIs. Still
  missing: Workshop text search, which has no keyless API (the search page returns HTTP 500 with the results in its body;
  not built).

### 6. StartupImpact sessions

Every session's time, mod list and hash, load time and per-mod cost: the trend, the costliest mods, and what
changed between any two launches. Built: phobos-modkit 0.17.0, `python scripts/startup-impact.py` (8 Nov 2026).

### 7. Version readiness scan

Per active mod: declared versions, Steam tags, and a byte scan for transpilers, Prepatcher and asset
bundles, compared with the last run. Built on tools 3 and 5. Built: phobos-modkit 0.17.0, `python scripts/version-readiness.py`
(8 Nov 2026). It says what a mod declares and how closely it depends on the game, never whether it works.

### 8. Release tooling

Ostranauts' changelog grammar, release-note export (with the Workshop page limit) and constants updater, a
shared link checker, and a layout checker whose rules (the EULA disclaimer, package ID pattern, placeholder
style) stay in each game repository as data.

**Adopted 8 Nov 2026** (owner's go-ahead the same day): phobos-modkit 0.10.0 brought the changelog check and the
constants updater from Ostranauts; 0.10.1 fixed its refusal of "C#" in a changelog line. `scripts/release.py` calls them;
`config/maintained-constants.json` registers every copy of each mod's version (42 copies across 7 mods: up to 11 for
D'thorga Patches), and `scripts/check-mod-layout.py` refuses a copy it does not list. Before this, each bump was a
hand-written script over eight copies.

### 9. Crash dumps

**Built, 9 Nov 2026 (phobos-modkit 1.4.0), at the owner's suggestion after a stack overflow left nothing in the log.** `python scripts/crash-dump.py` reads the newest RimWorld minidump, with no debugger or symbols: the exception, the thread, a call loop that ran away (how big one level is, how many times it repeats, whether every level held the same objects) and which modules the stack points at. A small dump cannot name .NET methods, so the report says how to get them. The first case is [the crash of 9 Nov](crash-2026-11-09-stack-overflow.md).

**Full dumps name the methods (phobos-modkit 1.22.0, 18 Nov 2026, owner's go-ahead).** When a dump holds the game's full memory,
crash-dump asks Microsoft's dotnet-dump which method and assembly each code address on the failing stack falls in: the loop's
return addresses, the calls beyond the loop that led into it, and those where the stack ran out. `python scripts/crash-dump.py setup`
says whether Windows keeps full dumps of RimWorld (on the owner's PC on 18 Nov it kept small ones only, the newest 10, in
`%LOCALAPPDATA%\CrashDumps`) and prints the administrator lines that ask it to; only the owner runs them. Checked on a stack overflow
of a small test program of ours: it named the same method and caller as .NET's own error output, in about 3 s. On the way, a fault
was found and fixed: in a dump .NET writes itself, the reader had taken the file's header for the stack. **Not yet run on a dump
of the game.**

### 9b. A frozen game, and memory

**Built 18 Nov 2026 (phobos-modkit 1.22.0), owner's go-ahead.** `python scripts/perf.py stack` takes three snapshots of every
thread's stack two seconds apart with Microsoft's dotnet-stack, finds the thread running the game's loop, and says whether it moved,
which frames never did, the part of the loop and the innermost mod on its stack. `heap` writes the managed heap with dotnet-gcdump
(a full garbage collection first, so the game pauses) and reports it by type and by mod; a .NET collection of a mod's type counts for
that mod; `heap-compare` shows who grew. Checked on a small test program of ours (a spinning thread was found unmoved in three
snapshots; a 21.7 MB heap was read in half a second). Limits: memory counts under the type that is it, so arrays a mod holds count as
.NET's; dotnet-gcdump warns that on .NET 10 it can drop events on a large heap.

**First run on the game, 19 Nov 2026: the heap snapshot sees only part of a large heap.** On the owner's PG15 (26 GB claimed)
dotnet-gcdump listed exactly 10,000,003 objects and 667 MB, while the runtime's own counters in earlier recordings put the heap at
6 to 11.5 GB: it stops near ten million objects. The game paused 20 to 30 s (the owner's watch). Since phobos-modkit 1.42.0 heap
reads the runtime's own count straight after and says in bold when the snapshot is partial; shares are of what it listed.

**The whole heap, values and why: `python scripts/perf.py look`, `look-kind` and `look-why` (phobos-modkit 1.65.0, 21 Nov 2026,
the owner's ask for debugging tools in D'thorga Companion).** The kit's heap reader takes a Windows snapshot (431 to 470 ms
pause on the owner's colony) and reads it with Microsoft's ClrMD and the game's own memory-reading library: all 27 to 28 million
objects in 12 to 18 s, no cap, any object's values, what holds it and paths from a root; no collection forced, so it counts garbage
too, and `look-why` tells the two apart. Its first use found 453 MB of patch XML kept alive
([patch-xml-kept-alive-2026-11-21.md](patch-xml-kept-alive-2026-11-21.md)).

**Memory over time: `python scripts/perf.py watch` (phobos-modkit 1.42.0, 19 Nov 2026, the owner's request).** Follows the
running game for minutes or hours **without pausing it**: what it claimed and what is in RAM (Windows' own figures, every few
seconds), .NET's heap and collector (dotnet-counters, attached once; `--no-counters` attaches nothing), and every minute the memory
map by kind, block size and file. Its report says whether memory held steady or climbed in jumps, splits .NET's heap from the rest
(the engine, drivers, native code), and names the saves loaded and errors logged around each jump. First readings (owner's game,
19 Nov, not tests): 26 GB claimed; one 135 GB reservation with 12.9 GB in use, most likely .NET's heap (unconfirmed without the
counters), and about 12 GB in some 2,400 native blocks of 1 to 64 MB; reloading PG15 dropped the claim 1.6 GB, then raised it
3.4 GB within a minute, mostly in 390 new 1 to 16 MB blocks. Still to build (owner agreed 19 Nov, TraceEvent as a dependency):
a reader of the `.gcdump` file for what holds what, and allocation recording by mod and method.

### 9c. The upstream watch

**Built 18 Nov 2026 (phobos-modkit 1.23.0), owner's go-ahead.** `python scripts/upstream.py` takes every member our C# names on
another author's type (184 that day, the scan check-targets uses) and looks for changes to them in two places: the archive's newest
two versions of each file updated in the last 30 days, compared member by member, and the source clones' commits since. Its first run
found 13 commits in Vanilla Expanded Framework and Vehicle Framework, among them VF-412, the upgrade research check found by hand on
15 Nov; a spot check of another ("Progress bar tweaks") was a real change inside `CompAdvancedResourceProcessor.PostSpawnSetup`,
which a patch of ours targets. About 35 s the first time, 4 s once the decompiles are cached. Limits: it matches by name, so read each
commit before acting; its note on the "oldest" clone names the repository with the oldest newest commit, which for an abandoned
mod (Vanilla Cooking Expanded, 2022) is its age, not a stale clone.

**The check after an update (20 Nov 2026, phobos-modkit 1.61.0), owner's request.** `python scripts/update-check.py` runs four
checks an agent had run by hand that day with five tools: check-targets' names, the temporary patches' faults (from
`config/temporary-patches.json`), each kept update's size and new types, and our XML patches against the Defs as written and then
the patched-Defs file. Its first run on the owner's game took 1 min 46 s: 622 of 622 targets found, VFE Factory's belt tick flagged
for row 12, the Harrier's 246 new declarations listed, and all 371 Def names our patches use found (7 only once patched). Our own
patches that add Defs whole, as the Harrier bombs do, are read too; before that fix they read as missing. D'thorga Companion's Since
you last played shows its answers.

### 10. The test pass

**Built, 17 Nov 2026 (phobos-modkit 1.9.0, D'thorga Framework 0.118.0), at the owner's request:** passes seen in play rarely
reached the test list. `python scripts/test-pass.py report` reads `docs/development/in-game-test-pass.md`, D'thorga Framework's
seen-in-play notes (with its setting on) and the owner's verdicts from D'thorga Companion's Tests page; `apply --apply` writes them in.
`config/test-evidence.json` ties each note to its rows. A note says a feature ran without an error, never that it looked right.

**Judged, 18 Nov 2026 (phobos-modkit 1.19.1, D'thorga Framework 0.138.0; [seen-in-play checks](seen-in-play-checks.md)):** notes carry
what they were seen on, their case and their figures; the map splits rows into parts, narrows notes and sets expected figures and order
rules; the report lists figures off, notes out of order, sightings older than their row, how widely a row was seen and notes no row names,
and apply takes back a seen sentence no note stands for any more. The owner's "make a row" and "no row needed" come from Companion.

### 11. Art audit

**Built, 17 Nov 2026 (phobos-modkit 1.12.0), at the owner's request after a one-off audit of stand-in art.** `python scripts/art-audit.py`
pairs every shipped texture with its record (its own, a body's shared by the name before an underscore, or its folder's), classes it by
the record's first statement of how it was made (so a generated picture made from a stand-in drawing stays generated), and lists every
picture a Def, patch or class uses that belongs to another of our things or is not ours. `config/art-audit.json` holds the patterns and
the accepted borrowings and drawn-in-code pictures with their reasons; `check` fails on a texture without a record. It reads names and
records, never the images, and caches nothing. `check` joined the gate the same day, once that day's missing records were written:
it enforces the standing rule that every texture has an asset record beside it.

### 12. Performance in play

**Built, 17 Nov 2026 (phobos-modkit 1.13.0), at the owner's request** ("BUILD THE TOOL"), after a slow game with many wounded
pawns could only be answered by reading code (D'thorga Medical 0.17.1). This line used to say performance in play had no file we
could read. Then RimWorld 1.7 turned out to run on Microsoft's CoreCLR with the standard .NET diagnostics pipe open, so
Microsoft's [dotnet-trace and dotnet-counters](https://learn.microsoft.com/dotnet/core/diagnostics/) record it from outside.
`python scripts/perf.py record` samples every thread's call stack and the runtime's counters for 30 seconds. The report then
gives the main thread by part of the game loop, the mod that caused each share of ticking (the innermost mod on the stack),
the busiest methods, patched game methods and garbage collection; `compare` sets two recordings side by side as shares.
Recording needs the owner's go-ahead each time, because attaching can hitch the game. Dubs Performance Analyzer stays useful
for watching a figure live. Inside the game, the owner's pick (17 Nov 2026) is keyz's
[Frame Profiler Plus](https://github.com/keyz182/FrameProfilerPlus) on Ludeon's 1.7 Frame Profiler: each mod's Harmony patch
time and tick time by ThingDef, comp and job driver, captured while the slow moment is on screen. [Phobos Scope](https://github.com/phobos-dthorga/phobos-scope)'s recorder times sections a mod marks
inside the game, which this does not. **Not yet run against the game**: checked on a small program of our own and on
hand-written fixtures.

### Not proposed

- **The world-grid rebuild** moved from `.local/tools/planet.py` into phobos-modkit 0.19.0 (9 Nov 2026) when a second use
  appeared: `python scripts/query-save.py planet` reports pollution on the world map from it, and since phobos-modkit 1.14.0
  (18 Nov 2026) `terrain` reports heights, hilliness and the steps between neighbouring tiles, which a scratch script had measured.

## Shared frameworks (7 Nov 2026)

Asked by the owner once two tools existed: could phobos-modkit benefit from shared frameworks? The agent's
answer, agreed by the owner the same day: yes, inside the kit; no outside ones.

| What | Where | Status |
| --- | --- | --- |
| **Report model**: one way for every tool to build and render a report as Markdown or JSON, with redaction everywhere | phobos-modkit 0.3.0, `phobos_modkit.report` | Built. Both tools use it; their Markdown came out byte-identical on the owner's live log and save |
| **Derived-data cache**: cache folder, provenance stamps, staleness, frozen copies | phobos-modkit 0.3.0, `phobos_modkit.cache` | Built. The save index uses it |
| **SQLite substrate**: every derived file is SQLite with a `meta` provenance table | phobos-modkit's AGENTS.md | Adopted. Tool 3 is designed to it |
| **Code index**: find, outline, show and refs over the decompiles in `.local/decompiled` and our `src/` | phobos-modkit 0.13.1, called through `scripts/query-code.py` (owner's go-ahead, 8 Nov 2026) | Built. 54 trees, about 330,000 declarations, indexed in 34 s; a query takes under a second. That day's vehicle health research had sent three agents to read the same code by hand |
| **REA** ([morluto/rea](https://github.com/morluto/rea), MIT): a reverse-engineering MCP server and command line; for .NET it reads an assembly's metadata, IL, call edges, field accesses, literals and constants, and compares builds | Installed 9 Nov 2026 at the owner's go-ahead: 6.8.0, pinned, command line only, not registered as an MCP server, so nothing runs unless called | Evaluated 9 Nov 2026; not adopted (agent's recommendation). Safe and light: no network use, launched programs or leftovers in seven monitored .NET runs, about 3 s and 300 to 400 MB each. But its fixed 64 MB limit on decoded metadata (`ManagedDecodeBudget`, no setting) left `Assembly-CSharp.dll` unread even for one small type, and `VEF.dll` readable only one type at a time; **an unread file exits as a success with zeros**. Comparing two builds of our Framework, about 360 of 365 "changed" methods were false alarms, and it does not compare literals. Use it for one type's call edges, field accesses or constants where it can read them; `.local/tools/us_strings.py` stays. The Ghidra route is untested. What changed between builds is `query-code.py compare` |
| **Build comparison**: what changed between two builds, member by member, from decompiled source | phobos-modkit 1.3.0, `code compare`, called through `scripts/query-code.py` (owner's go-ahead, 9 Nov 2026) | Built. Two Framework builds: 9 added, 2 changed, all confirmed against the source. `VEF.dll` 1.6 to 1.7 in 6 s. `Assembly-CSharp.dll` read whole, 96,913 declarations, about 5 minutes on first use with the game running; no two game builds were available to compare, and the run against a single-file decompile showed that the two sides must be decompiled the same way |
| **Assembly archive**: every version of the game's assembly and of the mods ours tie into, so an update can be compared after Steam replaces the older build | phobos-modkit 1.15.0, `rimworld-archive`, called through `scripts/assembly-archive.py`; a snapshot runs with every `refresh-mod-list.py` (owner's request, 18 Nov 2026) | Built. First snapshot 18 Nov 2026: 89 files from the game and 43 active tied mods, 13.7 MB compressed in `%LOCALAPPDATA%\phobos-modkit\assembly-archive`. `compare <file>` runs `code compare` on the last two versions. A game build no snapshot saw: DepotDownloader, run by the owner (the kit's guide). That day's 1.7.5011 to 1.7.5013 update could not be compared, as no copy survived |
| **Public source first** (owner's rule, 18 Nov 2026): a mod's own repository, read at the matching tag, before any decompile | AGENTS.md; the register `config/source-repos.json`, kept by `scripts/source-repos.py --write` (phobos-modkit 1.16.0, `rimworld-sources`: About.xml, the Workshop page, then a GitHub search, keyless and paced; each repository checked by an About.xml naming the mod's packageId); lean clones (`source-repos.py --clone`, phobos-modkit 1.17.0 to 1.18.0: whole history, source files only) go to `sourceClonesPath`, `K:\phobos-modkit\sources` on the owner's PC (drive D: was at 2% free), indexed by `query-code.py`; every `refresh-mod-list.py` run prints their size and the drive's free space | Built. First run 18 Nov 2026, 54 tied mods: 19 confirmed (among them Vehicle Framework, Vanilla Expanded Framework, Combat Extended, Dubs Bad Hygiene, Rimatomics, Rimefeller, Search and Rescue, Hospitality, More Injuries, Harmony), 22 with only unconfirmed candidates (mostly the Harmony mod's link, which the check rules out), 13 with none (the Harrier, Hind and Mongoose, RimPacts, InterRim Ballistic Missile among them). The 19 clones took 59 MB; 14 hold C# (Combat Extended with 19,582 commits, Vehicle Framework with 1,266 files), and five hold built files only (Dubs' three, Medical System Expansion 2, Frontline Workshop). Vehicle Framework's history named today's change itself: "VF-412: Add upgrade research prerequisite check" (15 Nov 2026) |
| **Mod settings**: the owner's saved settings for every mod, ours beside defaults read from our C# source, files left by removed mods, snapshots to compare | phobos-modkit 0.12.0, called through `scripts/mod-settings.py` (owner's request, 8 Nov 2026) | Built. On the owner's Config folder: 154 files, 118 for active mods, five left by our merged mods; `check-mod-layout.py` now also refuses a settings field whose starting value differs from its saved default (none found). **`changes --since REV`** (10 Nov 2026, for the owner's rule that every new or changed setting is reported to them) lists settings our code added, removed or gave a new default since a commit, from git's history alone |
| **The game's log kept alive, and a shared logger**: the game stops every mod's logging at 10,000 messages a session; a bounded revive in the Framework, our own log categories with budgets, a shared logger in a sibling repository, a schema and a modkit reader across games | Proposal recorded 8 Nov 2026 ([record](logging-framework-design.md)); owner chose the revive, no Rust in-game, a new sibling repository; record only so far | The reader's JSON-lines adapter waits for the file (slice L4). The engine shims `UnityStubs` and `Waylith.UnityCompat` are now decompiled in `.local/decompiled` for the code index |
| **Ghidra** (`C:\ghidra`, installed by the owner 8 Nov 2026) | The owner's install | Not used yet: it decompiles native code, not .NET IL (that is `ilspycmd`, local at `.local/tools/ilspy`, run as its win-x64 `ilspycmd.exe`; the `.cmd` wrapper fails on the path's space). Ghidra is the tool for the engine's native libraries (`Brutal.*`, Waylith's native side) should a question ever need them |
| **The gate**: every build, test and check before a commit from `config/gate.json`, printing only what failed | phobos-modkit 0.11.0, called through `scripts/check-all.py` (8 Nov 2026, owner's go-ahead) | Built. 34 steps in 52 s on the owner's PC; it found the four rule-test projects (112 tests) that sessions' hand-typed loops had skipped |
| **Game adapter contract**: generic `log` and `save` commands with a game option, instead of commands named by game | Proposal (agent) | Waits for a second game adapter; FS25 log triage is the likely first. Built for one game, its shape would be a guess |
| **Desktop front end**: a program for the person at the screen over every tool, Tauri 2 and Svelte over a Rust host, the kit called through `--json`; agents keep the command line | D'thorga Companion, a new sibling repository (owner, 9 Nov 2026); [research record](tools-desktop-research.md) | The window, theme and checks are built (0.1.0, not yet judged on screen by the owner). The kit's part is half done: phobos-modkit 1.0.0 has one JSON envelope for every command and a tool manifest; typed table cells and a progress stream wait for the screens that need them |
| **Resolved-Defs dump**: D'thorga Framework writes every Def with all patches applied (before inheritance) to the RimWorld user folder on a developer's one-shot request; the kit indexes it and diffs each Def against its written form | Framework 0.27.0 and phobos-modkit 0.15.0's `rimworld-defs patched`, through `python scripts/query-defs.py patched <defName>` (owner's go-ahead, 8 Nov 2026) | Built and **the owner wrote one in their game on 8 November 2026** (72.8 MB, 335 mods); the tool indexed it in 6.8 s and showed Combat Extended's rewrite of Gun_Revolver as a diff. Refresh the file (the setting, then a restart) whenever the mod list or a mod's patches change; the index notices the file's date itself. No Harmony: a `PatchOperation` of ours keeps the game's combined XML document, which later mods go on patching, and a `StaticConstructorOnStartup` class writes it. The file holds other authors' Defs, so it stays in the player's RimWorld folder and the kit's cache, never committed |

Declined, with reasons: outside Python frameworks (click, pandas, rich), because the kit runs on the standard
library alone; clap for the Rust program, which has one command; and sharing code with Phobos Scope, whose HTML
reports for people differ from the kit's Markdown for agents.

**Sound tools (10 Nov 2026, this repository's `scripts/art/`, ffmpeg underneath; [sound generation](sound-generation.md)):**
`game_sounds.py` finds and measures RimWorld's own sounds where they lie in its `Assets.zip` archives and reads its SoundDefs, so a
question such as "how loud is the game's generator?" is one command, not an extraction (no game file is kept). `sound_review.py`
measures and flags generated candidates for a listening page; `export_sound.py` levels the owner's picks against the game's sounds
and writes them into the mods with records. They are RimWorld-specific, so they live here rather than in phobos-modkit.

## Where the code lives

The full rules are in phobos-modkit's own documents (`docs/project-direction.md` and `docs/migration.md`
there). In short:

- **Core** (no game in it) and **game adapters** (public file-format knowledge for one game) live in
  phobos-modkit. **Project policy** (which Defs matter, the factory-patch check, the hand-written mod
  suggestions, the EULA disclaimer) stays here.
- This repository pins phobos-modkit as a git submodule at `external/phobos-modkit`, the way
  phobos-ostranauts pins Phobos Scope. Python scripts reach it through a small path shim; the Rust and C#
  tools are built from it and their programs are ignored.
- A fix to a shared tool goes to phobos-modkit first, then the pin moves. Existing scripts move over when
  next touched, not in a sweep.
- Two path keys join `.local/install-settings.json`, resolved the way `scripts/deploy.py` resolves the others:
  `rimworldLogsPath` (in use by `scripts/triage-log.py`) and `rimworldSavesPath` (with the save indexer).

## Order of work

1. Create phobos-modkit with its documents. **Done 7 Nov 2026**, documents only.
2. Log triage, consumed here first: the largest saving and no new toolchain. **Done 7 Nov 2026**: phobos-modkit 0.1.1,
   called through `scripts/triage-log.py`. On the owner's logs a 2.1 MB log became an 8 KB summary in under 4 s, and a 38 MB
   broken-session log one of under 5 KB.
3. The save indexer's working slice with the RimWorld schema. **Done 7 Nov 2026**: phobos-modkit 0.2.2, called through
   `scripts/query-save.py`. PG2 (308 MB, 8.2 million elements) indexes in 1.4 s, against 7.6 s for one pass of Python's own
   reader; reports then take about 0.6 s. Agent choice made while building: the Rust program only builds the index, and
   the reports are Python, sharing the log tool's command line and Markdown and reading the index with Python's own SQLite.
   Map grids and comparing two saves followed the same day (backlog T2b, phobos-modkit 0.6.0).
4. The shared report model and derived-data cache. **Done 7 Nov 2026**: phobos-modkit 0.3.0 (see Shared frameworks).
5. The mod and Def index, which tool 1's mod attribution and tool 7 build on. **Done 7 Nov 2026**: phobos-modkit 0.4.0,
   called through `scripts/query-defs.py`.
6. Workshop research. **Done 7 Nov 2026**: phobos-modkit 0.5.0, called through `scripts/workshop-research.py`.
7. Tools 4, 6, 7 and 8 as the need arises, each its own change with its own tests.
