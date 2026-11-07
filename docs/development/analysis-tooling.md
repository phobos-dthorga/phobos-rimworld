# Analysis tooling: proposals (7 Nov 2026)

**Answer first.** Agent sessions spend most of their reading on three things: the game's log, the save, and
the Defs of about 340 installed mods. Eight deterministic tools would answer the usual questions about them
in a few hundred words of summary instead. They are built in a new shared repository,
[phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit), because most of the code is not
specific to RimWorld and the owner's other game repositories carry drifting copies of the same scripts.

**Gotchas**

- **This is a record; one tool is built.** Log triage (tool 1) shipped on 7 Nov 2026 as `scripts/triage-log.py`; the
  others are proposals.
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

### 3. Mod and Def index

- One library for the active mod list and order, each mod's loaded files (lifting
  `files_the_game_loads` from `scripts/check-mod-layout.py`), `LoadFolders.xml` with all three conditions and
  the 1.7-then-1.6 fallback, `About.xml`, assemblies, and `ParentName` inheritance.
- Commands: `who <defName>` (owning mod and file, and every patch whose path names it), `list --type`,
  `show <defName>` (inheritance applied, patches not applied, and labelled so), `patches --target`.
- Cached under `.local/cache/`, keyed by a hash of the mod list.
- `scripts/check-factory-patches.py` and `scripts/refresh-mod-list.py` move onto it when next touched.

### 4. Assembly metadata reader

- `harmony` lists every `[HarmonyPatch]` attribute's target, so "which active mods patch this method" is one
  command over every `Assemblies` folder.
- `types`, `members`, `strings` (each string literal and the instructions after it, replacing
  `us_strings.py`), and `apicheck` (does a consumer still match its provider).
- **Limit:** patches applied in code rather than by attribute need a decompile, and the tool says so.

### 5. Workshop research

- Details through Steam's keyless `GetPublishedFileDetails`; the latest 50 comments from the comments page;
  about 8 s between requests, a cache with fetch dates, and a pause on HTTP 429.
- Per mod: last update, supported versions, the author's last reply, comments in the last 90 days, and lines
  that mention crashes, errors, breakage or the current game version.
- **Limit:** Workshop item pages are region-blocked here, so required items come from the description and
  comments. Ostranauts' `pull-workshop.py` is the first code to lift.

### 6. StartupImpact sessions

Every session's time, mod list and hash, load time and per-mod cost: the trend, the costliest mods, and what
changed between any two launches.

### 7. Version readiness scan

Per active mod: declared versions, Steam tags, and a byte scan for transpilers, Prepatcher and asset
bundles, compared with the last run. Built on tools 3 and 5.

### 8. Release tooling

Ostranauts' changelog grammar, release-note export (with the Workshop page limit) and constants updater, a
shared link checker, and a layout checker whose rules (the EULA disclaimer, package ID pattern, placeholder
style) stay in each game repository as data.

### Not proposed

- **Performance in play** (milliseconds per tick) has no file we can read; the owner uses Dubs Performance
  Analyzer. The long-term answer is a RimWorld adapter for
  [Phobos Scope](https://github.com/phobos-dthorga/phobos-scope)'s recorder, which belongs to that repository.
- **The world-grid rebuild** in `.local/tools/planet.py` stays there until a second use appears.

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
3. The save indexer's working slice with the RimWorld schema.
4. The mod and Def index, which tool 1's mod attribution and tool 7 build on.
5. Tools 4 to 8 as the need arises, each its own change with its own tests.
