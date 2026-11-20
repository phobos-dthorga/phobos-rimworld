# Where each rule of AGENTS.md went: the split of 21 Nov 2026

**Status (21 Nov 2026):** built, as drafted and approved (Do it as drafted), with one change: D'thorga Patches' one rule went to `src/AGENTS.md`, because `deploy.py` ships everything inside `mods/<Mod>/`. The files as written: the root 13.6 KB, `scripts/` 24.0 KB, `src/` 15.4 KB, `mods/` 7.1 KB, `docs/` 3.0 KB; `tests/test_rule_files.py` holds the root to the files it names.

**The owner's idea (21 Nov 2026):** "keep a slimmed-down AGENTS at the root and save the more specific details, as appropriate, for each mod's own root". The agent's reading of all 99 rules: only a few are one mod's alone, so the map splits **by the kind of work** as well, into the folders that work happens in.

## Read this first (gotchas)

- **A nested file loads by itself only when a session opens a file in its folder.** Running a script does not load it. So the root keeps a short table, one line a tool, of which script answers which question; the detail moves.
- **Anything that stops work starting stays at the root**: what is on the back burner, whose bug is whose, what never to touch (saves, the owner's game, Steam).
- **No rule is reworded in the move**, except where the root keeps a shortened line. A check would hold every rule's bold opening to exist somewhere afterwards.
- **Sizes are of the text before the move**; the files as written are a little larger, for their headers.

## What it comes to

| File | Size | Read by |
| --- | --- | --- |
| `AGENTS.md` today | 54.7 KB | Every session |
| **`AGENTS.md` after** | **about 13.3 KB** | Every session |
| `scripts/AGENTS.md` (new) | 21.7 KB | A session about to answer a question with a tool, or writing a script or a test |
| `src/AGENTS.md` (new) | 14.4 KB | A session writing C# or designing how a mod works |
| `mods/AGENTS.md` (new) | 6.2 KB | A session editing a mod's shipped files: Defs, text, About, changelog, README |
| `docs/AGENTS.md` (new) | 2.5 KB | A session writing a guide or a record |
| `src/PhobosRadio/AGENTS.md` (there already) | +0.2 KB | Radio work |
| `src/PhobosFramework.Vehicles/AGENTS.md` (there already) | +0.6 KB | Vehicle and submarine work |

| A session that... | Reads today | Would read |
| --- | --- | --- |
| only talks, plans, or works on another repository from here | 54.7 KB | about 13.3 KB |
| diagnoses a fault with the tools | 54.7 KB | about 35.0 KB |
| writes C# for a mod | 54.7 KB | about 27.7 KB |
| edits a mod's Defs, text or changelog | 54.7 KB | about 19.5 KB |
| writes C# and the mod's changelog and README (most feature work) | 54.7 KB | about 33.9 KB |
| writes a guide or a record | 54.7 KB | about 15.8 KB |

**Said plainly:** feature work saves the least, because it truly needs the code rules and the mod rules. The saving there is the tool catalogue and the document rules it no longer reads.

## The map, rule by rule

Each rule by its opening words. **Root keeps** says what stays in the root file when the rule itself moves.

### Rules shared by every game

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 1 | A rule that would hold in another game's repository too does not go in this... | 370 | stays | all of it |  |
| 2 | A rule here that the shared rules now state is dropped from this file | 197 | stays | all of it |  |
| 3 | `python scripts/check-all.py` checks this repository still reads them (its... | 100 | stays | all of it |  |

### Rules kept beside their work

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 4 | Art and sound | 202 | stays | all of it | The table of files to read gains the new ones |
| 5 | Vehicles and submarines | 226 | stays | all of it |  |
| 6 | Radio framework | 115 | stays | all of it |  |
| 7 | The radio framework is on the back burner | 262 | stays | all of it | It stops work starting, so every session needs it |
| 8 | Submarine work is on the back burner too, and out of view | 582 | `src/PhobosFramework.Vehicles/AGENTS.md` | a line | The root keeps: no submarine work until the owner says; the detail moves |

### Working with the owner

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 9 | Practical, small-team project: prefer a working slice over speculative fram... | 346 | stays | all of it |  |
| 10 | Candidly recommend simplifying, setting aside or changing direction when ga... | 238 | stays | all of it |  |
| 11 | Explain what works, what was checked and what remains uncertain. Distinguis... | 393 | stays | all of it |  |
| 12 | A feature with a test row notes itself when seen working | 1,080 | `src/AGENTS.md` | 1 table row | Needed when building a feature with a test row; test-pass gets a table row |
| 13 | A note carries what makes it evidence | 1,072 | `src/AGENTS.md` | nothing |  |
| 14 | The owner runs gameplay tests. Do not control the owner's mouse or keyboard... | 1,585 | `scripts/AGENTS.md` | a line and 2 table rows | The root keeps: the owner runs tests, never their mouse or keyboard, never edit saves, read the log before blaming the owner |
| 15 | Ordinary causes before a bug hunt | 373 | stays | all of it |  |
| 16 | When the game closes by itself with little in its log, run `python scripts/... | 905 | `scripts/AGENTS.md` | 1 table row |  |
| 17 | Answer which mod defines or patches a Def with `python scripts/query-defs.py` | 623 | `scripts/AGENTS.md` | 1 table row |  |
| 18 | Read another mod's Defs as patched by default | 719 | `scripts/AGENTS.md` | nothing | Shares query-defs' row |
| 19 | Answer questions about the game's or another mod's code with `python script... | 1,343 | `scripts/AGENTS.md` | 1 table row |  |
| 20 | Public source before a decompile | 1,845 | `scripts/AGENTS.md` | 1 table row |  |
| 21 | New local data goes on the drive the owner chose, never the repository's drive | 619 | `scripts/AGENTS.md` | a line | The root keeps one line: new local data goes on K:, never D: |
| 22 | Answer what an update of the game or of a mod we tie into changed with `pyt... | 1,913 | `scripts/AGENTS.md` | 3 table rows | Three rows: assembly-archive, upstream, update-check |
| 23 | Answer which mods patch a method with `python scripts/query-patches.py` | 504 | `scripts/AGENTS.md` | 1 table row |  |
| 24 | Answer where the running game spends its time, and which mods cause it, wit... | 2,374 | `scripts/AGENTS.md` | a line and 1 table row | The root keeps: ask before attaching to the owner's game, save first, and look before guessing |
| 25 | Answer how long the game takes to start, and which mods cost it, with `pyth... | 632 | `scripts/AGENTS.md` | 2 table rows |  |
| 26 | Tell the owner whenever a setting of ours is added, removed or given a new... | 497 | stays | all of it | Every reply that delivers a change needs it |
| 27 | Answer what settings the owner plays with using `python scripts/mod-setting... | 505 | `scripts/AGENTS.md` | 1 table row |  |
| 28 | Tools before LLM reading, even when the tool needs work first | 784 | stays | a shorter line | Shortened; it introduces the question-to-script table |
| 29 | The owner plays RimWorld 1.7 (since 26 October 2026; the last 1.6 save is a... | 1,672 | `scripts/AGENTS.md` | a line and 2 table rows | The root keeps: RimWorld 1.7, every DLC, a large mod list, a high-end PC |
| 30 | The owner is a licensed amateur radio operator and an applied chemist. Radi... | 196 | stays | all of it |  |
| 31 | Owner choices, agent defaults and agent choices made while the owner is awa... | 151 | stays | all of it |  |
| 32 | Who does what | 511 | `mods/AGENTS.md` | nothing | About content in Defs and language files |

### Git and publication boundaries

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 33 | The repository is public, and its mods are for public release | 160 | stays | all of it |  |
| 34 | Commit and push directly to `main`; no pull requests unless the owner asks... | 262 | stays | all of it |  |
| 35 | Several agent sessions may share this tree: commit explicit paths, and leav... | 220 | stays | all of it |  |
| 36 | Steam Workshop. | 471 | `mods/AGENTS.md` | a line | The root keeps: agents never upload, handle credentials or change visibility |
| 37 | Ludeon's EULA | 614 | `mods/AGENTS.md` | nothing |  |
| 38 | Never commit saves, game assemblies, decompiled game or mod source, extract... | 371 | stays | all of it |  |
| 39 | Agents never execute other authors' mod assemblies or scripts outside the g... | 258 | stays | all of it |  |

### Versions, changelogs and READMEs

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 40 | Version numbers (owner rule from Phobos Ostranauts): a fix or patch raises... | 289 | `mods/AGENTS.md` | nothing |  |
| 41 | A mod's version lives in `About/About.xml` (`modVersion`), every project of... | 739 | `mods/AGENTS.md` | 1 table row | release.py gets a table row |
| 42 | Every change to a mod updates its `CHANGELOG.md` in the same commit: an Unr... | 521 | `mods/AGENTS.md` | nothing |  |
| 43 | Every mod has a README that opens with **Read this first (gotchas)**: requi... | 284 | `mods/AGENTS.md` | nothing |  |

### Player language and localisation

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 44 | All player-facing English follows `docs/development/player-language.md`: in... | 295 | `mods/AGENTS.md` | nothing |  |
| 45 | Every gizmo, command and dialog says what it did, and every refusal says wh... | 457 | `src/AGENTS.md` | nothing | About gizmos and buttons, which are code |
| 46 | Text a player reads in play stays in the game world | 614 | `mods/AGENTS.md` | nothing |  |
| 47 | Player text is never hard-coded in C#: what code shows lives in `Languages/... | 803 | `src/AGENTS.md` | nothing | About text in C# |

### Documentation

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 48 | Player guides live in each mod's README and `docs/`; contributor instructio... | 406 | `docs/AGENTS.md` | nothing |  |
| 49 | The player guide (`docs/player-guide/`) is the players' front door | 547 | `docs/AGENTS.md` | nothing |  |
| 50 | A design record's status is one line at its top | 582 | `docs/AGENTS.md` | nothing |  |
| 51 | Charts and diagrams are GitHub's native Mermaid, in the Markdown itself. Ne... | 189 | `docs/AGENTS.md` | nothing |  |
| 52 | Write plainly: answer first, short sentences, **bold** for must-dos, tables... | 190 | `docs/AGENTS.md` | nothing |  |

### Research attribution

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 53 | Name the organisation or researchers whose work supports a claim, calculati... | 322 | `docs/AGENTS.md` | nothing |  |
| 54 | Separate the source's findings from our inference, simplified models, ficti... | 373 | `docs/AGENTS.md` | nothing |  |

### Identifiers and branding

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 55 | `packageId` is `PhobosDthorga.<ModName>` (agent default, owner to confirm b... | 235 | `mods/AGENTS.md` | nothing |  |
| 56 | Prefix new defNames, C# namespaces, translation keys and saved (Scribe) key... | 207 | `src/AGENTS.md` | nothing | Namespaces and saved keys are code |
| 57 | An item Def's `defName` never ends in a digit | 264 | `mods/AGENTS.md` | nothing |  |
| 58 | Names players see are plain and unassuming | 452 | `mods/AGENTS.md` | nothing |  |
| 59 | D'thorga is the one brand for everything new, in every game | 312 | `mods/AGENTS.md` | nothing |  |
| 60 | Every public page also carries the spelling "dthorga" | 387 | `mods/AGENTS.md` | nothing |  |

### RimWorld engineering

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 61 | Shared code lives once. | 489 | `src/AGENTS.md` | nothing |  |
| 62 | Generic functions go to the Framework core | 622 | `src/AGENTS.md` | nothing |  |
| 63 | D'thorga Framework is that library | 484 | `src/AGENTS.md` | nothing |  |
| 64 | Fewer, broader mods | 767 | `src/AGENTS.md` | nothing |  |
| 65 | Another author's bug is theirs to fix | 626 | stays | all of it | It decides whether patch work starts at all, often in the middle of a diagnosis |
| 66 | Every patch for another author's fault has a row in `docs/development/tempo... | 407 | `src/AGENTS.md` | nothing | The one rule that is one mod's alone; in src/, not mods/PhobosPatches/, because deploy ships everything in a mod's folder |
| 67 | Existing effects first, own gases when they earn it | 523 | `src/AGENTS.md` | nothing |  |
| 68 | Anything that takes water or piping has its own pipes beneath it | 610 | `src/AGENTS.md` | nothing |  |
| 69 | What a thing of ours is built from is a parts bill, by role | 501 | `src/AGENTS.md` | nothing |  |
| 70 | UI presents, services decide. | 210 | `src/AGENTS.md` | nothing |  |
| 71 | XML first, C# where Defs cannot express it. | 310 | `src/AGENTS.md` | nothing |  |
| 72 | Harmony, sparingly. | 697 | `src/AGENTS.md` | nothing |  |
| 73 | Optional tie-ins only | 989 | `src/AGENTS.md` | nothing |  |
| 74 | Other Vanilla Expanded mods may become hard dependencies where one earns it... | 358 | `src/AGENTS.md` | nothing |  |
| 75 | Worn apparel is never spawned. | 209 | `src/PhobosRadio/AGENTS.md` | nothing | About hand-held radios only |
| 76 | Never spawn a thing from `PostSpawnSetup` | 396 | `src/AGENTS.md` | nothing |  |
| 77 | Never put a function of ours into another mod's shared hook | 486 | `src/AGENTS.md` | nothing |  |
| 78 | Derived state is never saved. | 281 | `src/AGENTS.md` | nothing |  |
| 79 | Interfaces follow the game's own patterns | 328 | `src/AGENTS.md` | nothing |  |
| 80 | Vanilla precedence. | 275 | `src/AGENTS.md` | nothing |  |
| 81 | Cheap to run | 290 | `src/AGENTS.md` | nothing |  |
| 82 | Saves. | 328 | `src/AGENTS.md` | nothing |  |
| 83 | Versions: RimWorld 1.7 only | 876 | `src/AGENTS.md` | a line | The root keeps one line: RimWorld 1.7 only, never 1.6 |
| 84 | Expose reasonable player preferences as documented mod settings; keep ident... | 194 | `src/AGENTS.md` | nothing |  |

### Materials, chemistry and balance

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 85 | Honest physics and chemistry | 238 | `mods/AGENTS.md` | nothing |  |
| 86 | No loops that create value from nothing; building to sell must not beat the... | 166 | `mods/AGENTS.md` | nothing |  |

### Builds, installation and checks

| # | Rule | Size | Goes to | Root keeps | Why, or what else |
| --- | --- | --- | --- | --- | --- |
| 87 | Install to the game's `Mods` folder only through `scripts/deploy.py`, with... | 479 | stays | all of it | Safety: installs only through deploy.py, with the game closed |
| 88 | Answer what updating or going back would change with `python scripts/instal... | 914 | `scripts/AGENTS.md` | 1 table row | Its last sentence repeats a shared rule (keep an entry's opening) and can be dropped |
| 89 | Answer what code is behind a changelog entry with `python scripts/changes.py` | 651 | `scripts/AGENTS.md` | 1 table row |  |
| 90 | Answer what happened to our mods over time with `python scripts/history.py` | 706 | `scripts/AGENTS.md` | 1 table row |  |
| 91 | Every path a script reads goes through the shared order | 496 | `scripts/AGENTS.md` | nothing |  |
| 92 | A script the owner may wait on says how far along it is | 304 | `scripts/AGENTS.md` | nothing |  |
| 93 | Answer whether textures need encoding before a launch with `python scripts/... | 1,039 | `scripts/AGENTS.md` | 1 table row | Its sentence on new art's canvas heights moves to the art rules |
| 94 | Scale checks to the change; test rules and persistence, not static document... | 1,341 | `scripts/AGENTS.md` | a line | The root keeps: run check-all.py and chain the commit on it, the commit guard, never --no-verify |
| 95 | `python scripts/check-targets.py` checks what the compiler cannot | 786 | `scripts/AGENTS.md` | 1 table row | Its half on how to write a by-name tie-in moves to the code rules |
| 96 | Tests leave nothing behind. | 758 | `scripts/AGENTS.md` | nothing |  |
| 97 | Detail for chasing a fault is a Debug line | 288 | `src/AGENTS.md` | nothing |  |
| 98 | Log through the shared logger | 396 | `src/AGENTS.md` | nothing |  |
| 99 | The shared tools come from the pinned phobos-modkit submodule | 464 | stays | a shorter line | Shortened: the submodule, and never edit it in place |

## How it was done

1. One commit: the new files, each with a `CLAUDE.md` beside it that imports it (as `assets/`, the vehicles and the radio have), `tests/CLAUDE.md` importing the scripts' rules, and the root slimmed; a script moved each rule and checked all 99 were found, word for word, in exactly one file.
2. `tests/test_rule_files.py`: every rule file the root names exists and has a `CLAUDE.md` loading it, none sits inside a mod's shipped folder, and every script in the root's table exists; the gate runs it.
3. This map is the record of the split.

## Agent choices, owner to revise

Every row's home; which rules keep a line at the root; one file for scripts and tests together; `mods/PhobosPatches/` as no file at all: its one rule is in `src/`, since everything in `mods/<Mod>/` ships.
