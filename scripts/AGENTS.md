# Tools, scripts and tests

Which script answers which question, and the rules for using tools, writing scripts and tests, installing and committing. A
session reads it before using a tool, writing a script or a test, installing, or committing.

Split from the root `AGENTS.md` on 21 Nov 2026 (the owner's yes: "keep a slimmed-down AGENTS at the root"); the root's rules
apply here too. **How each tool works (its reports, flags and limits) is written once, in the kit's docs**, in
`external/phobos-modkit/docs/`: read the doc named below before a tool's first use in a session. This file keeps which script
to run, and the rules that are this repository's or the owner's own (owner's docs rewrite, 23 Nov 2026: one home per topic).

## Which script answers which question

Every script is run as `python scripts/<script>`. The third column is the kit doc in `external/phobos-modkit/docs/`.

| The question | Script | How it works |
| --- | --- | --- |
| Anything in a save: pawns, things, map grids, world, mood, what changed | `query-save.py` | `rimworld-save.md` |
| A state our rules say should never be: in a save (`query-save.py checks`) or the running game (`perf.py look-check`, ask first) | `query-save.py`, `perf.py` | `checks.md` |
| A fault in the log | `triage-log.py` | `rimworld-log.md` |
| The modders' reference site (DocFX), and which modder-facing types have no summary | `reference-site.py` | `docs/development/reference-site.md` (this repository) |
| The game closed by itself; asking Windows for full dumps | `crash-dump.py` (`setup`) | `crash-dump.md` |
| Which mod defines or patches a Def, and the Def as patched | `query-defs.py` | `rimworld-defs.md` |
| The game's or another mod's code; what changed between builds | `query-code.py` (`compare`) | `rimworld-code.md`, `code.md` |
| Where another author's public source lives; cloning it | `source-repos.py` | `rimworld-sources.md` |
| What an update of the game or a tied mod changed | `assembly-archive.py compare` | `rimworld-archive.md` |
| Whether an update or an author's source touched what we patch | `upstream.py` | `rimworld-upstream.md` |
| After an update, everything at once | `update-check.py` | `rimworld-update-check.md` |
| Known problems between other authors' mods | the kit's `feeds` | `feeds.md` |
| Which mods patch a method | `query-patches.py` | `rimworld-patches.md` |
| Where the running game spends its time; a frozen game; memory over time | `perf.py` | `rimworld-perf.md` |
| What is in memory and why it is still there | `perf.py look`, `look-kind`, `look-why` | `heap-reader.md` |
| How long the game takes to start, and which mods cost it | `startup-impact.py` | `rimworld-startup.md` |
| Which active mods are ready for a game version | `version-readiness.py` | `rimworld-readiness.md` |
| The settings the owner plays with | `mod-settings.py` | `rimworld-settings.md` |
| The owner's mod list | `refresh-mod-list.py` | this file, below |
| A mod's Workshop page and comments; a Steam collection | `workshop-research.py` | `workshop.md` |
| What updating or going back would change | `installs.py` | `installs.md` |
| The code behind a changelog entry | `changes.py` | `changes.md` |
| What happened to our mods over time | `history.py` | `history.md` |
| What is safely copied; copying what cannot be made again | `safe-copies.py` | `safe-copies.md` |
| The paths and settings a script reads | `settings.py` | `settings.md` |
| Whether textures need encoding | `textures.py` | `rimworld-textures.md` |
| Everything a commit needs checked (the gate) | `check-all.py` | `gate.md` |
| The radio's HF propagation tables, from VOACAP in WSL (`run`, `pack`, `show`) | `hf-tables.py` | `docs/development/radio-hf.md` (this repository) |
| Whether a fitting, tune or upgrade node declares a grant, figure, mount or key nothing reads | the kit's `effects` (gate step "declared effects") | `effects.md` |
| Harmony targets and by-name lookups | `check-targets.py` | `rimworld-targets.md` |
| The generated tables in our docs: projects, Harmony patches, what each mod needs, each mod's settings | `doc-tables.py` | `doc-tables.md` |
| What the tools, builds and tests left behind | `cleanup.py` | `cleanup.md` |

## Working with the owner

- **Tools before LLM reading, even when the tool needs work first** (owner, 7 November 2026). When an analysis (of logs, saves,
  mod Defs, assemblies, Workshop pages or anything else) can be done by a tool we already have, in phobos-modkit or `scripts/`,
  use the tool. When it could be done by one only after a fix, an update or a new feature, **make that change first and then
  run the tool**, rather than spend more tokens having an agent read the raw material for the same or much the same answer.
  Shared changes go to phobos-modkit under its own rules, then the pin moves here. Read raw material by hand only for what no
  reasonable tool change would answer, and say so in the reply. The tools, their order and their gaps are in
  `docs/development/analysis-tooling.md`.

### Saves, logs and crashes

- **Answer questions about a save with `python scripts/query-save.py`** (`thing <ID>` for everything one thing saved, such as
  a vehicle's systems and fault history), and read the raw save only for what the index does not hold.
- **Run `python scripts/query-save.py checks` over a reported save before reading it by hand** (owner's choice, 25 Nov 2026,
  after the HEMTT's 975 steel sat in four saves unnoticed): its rules are `config/checks.json`, and **a new rule that would have
  caught a fault goes there in the change that fixes it**. Live checks (`perf.py look-check`) take a snapshot: **ask first**; on the
  owner's game a snapshot has no room (error 1450), so add `--freeze` (the game stands still while it is read, 120 s at most). A
  finding is something to look at, never a test (`docs/development/test-automation.md`).
- **test-pass reads the saves' histories too** (`saved` in `config/test-evidence.json`): a key under `saved.` is the kit's, from a
  vehicle's event log or logbook, never a `Seen.Note`; its version comes from Framework's version ledger (0.287.0).
- **A figure a save holds is evidence through a save check's `note`** (owner's choice, 26 Nov 2026; kit 1.129.0): a key under
  `check.` is the kit's, one note per thing per save, judged by the row's `expect`. Give the rule `row`, map the key in the same
  change, and add a `fault` for what the design says is impossible. Select `{"class": "*VehiclePawn"}`, or a `def`: jets save their own vehicle class. Vehicle
  Framework saves its own parts by place (`statHandler/components/li[N]`), so say in `why` which part and as of when. **A graph
  history is read with `samples(path, min, max)`** (kit 1.130.0): take the range from the quantity's Def and name it in `why`, and
  **begin every save figure rule's `when` with `ofPlayer`** (kit 1.131.0): another faction's vehicle off a map is never updated, so its
  history can be months old. **A live check may note figures too** (kit 1.134.0), for a value only the running game holds (a derived
  field never saved); the kit keeps its notes, and a live check cannot tell whose a thing is yet, so say in `why` why only the player's
  things meet its `when`. D'thorga
  Companion 0.101.0 runs the save checks by itself on the newest save: **a finding on Home is not the owner's error to explain**.
- Read the owner's `Player.log` before assuming a reported fault is the owner's error, and **start with
  `python scripts/triage-log.py`**. Open the raw log only for what the summary cannot answer.
- **When the game closes by itself with little in its log, run `python scripts/crash-dump.py`** (owner, 9 Nov 2026). For a
  stack overflow the game's error output can name the methods a small dump cannot
  (`docs/development/crash-2026-11-09-stack-overflow.md`). **A full dump names them** (owner's go-ahead, 18 Nov 2026):
  **`python scripts/crash-dump.py setup`** says whether Windows keeps full dumps of RimWorld and gives the PowerShell lines that
  ask it to; the owner runs them (or `setup --apply`) in an administrator window. **Agents never run `--apply` or `--off`.**

### Defs, code and other authors' source

- **Answer which mod defines or patches a Def with `python scripts/query-defs.py`** before searching mod folders by hand. The
  patched view comes from the file D'thorga Framework writes on request (dev mode: its settings or the dev action "write patched
  Defs at the next start", then a restart).
- **Read another mod's Defs as patched by default** (owner, 8 Nov 2026). `python scripts/query-defs.py patched <defName>`, from
  `PatchedDefs.xml`, is the first source for what a Def really is in the owner's game; `who` and `show` then say who defines and
  patches it and give the parent chain. Fall back to the as-written view only when the file has a blind spot, and say which: it
  is missing or older than the change in question (the report says when it may be); the Def is made in code; C# changes it at
  startup (Vanilla Expanded Framework sets its delivery spot's build category that way); inherited values are needed (the file
  is written before inheritance); or the mod was not active when it was written.
- **Answer questions about the game's or another mod's code with `python scripts/query-code.py`** (owner, 8 Nov 2026), before
  grepping a decompile or sending an agent to read one, and cite what it shows as `file:line`. It reads every tree in
  `.local/decompiled`; `--in ours` narrows to our `src/`, and builds only the indexes it queries. **A `--corpus NAME=PATH` given
  replaces those trees** (kit 1.141.1), so a question about one tree builds one index. A tree not yet decompiled needs decompiling first (into
  `.local/decompiled/<name>`). **`compare OLD.dll NEW.dll` says what changed between two builds** (owner, 9 Nov 2026); its
  decompiles stay in the kit's cache, outside every repository. A single assembly's metadata, call graph or constants are REA's
  job only where it can read them (evaluated 9 Nov 2026: not the game's assembly, and whole-file only for small mods; an unread
  file exits as a success with zeros); the owner has it set up (`docs/development/analysis-tooling.md`).
- **Public source before a decompile** (owner, 18 Nov 2026: the original source "will be so much more rich in what it can
  offer"). Before decompiling another author's assembly, look for its public repository, GitHub first: **read
  `config/source-repos.json`, and run `python scripts/source-repos.py --write`** when the mod is missing or unconfirmed there.
  An unconfirmed candidate may still be its source (a rename, or source with no About.xml): judge it and say so. Where one
  exists, read it at the tag or commit matching the build in play, with its commit history for what changed and why; **clone it
  with `python scripts/source-repos.py --clone`** (`--update-clones` to bring clones up to date) into `sourceClonesPath` from
  `.local/install-settings.json` (the owner chose `K:\phobos-modkit\sources`, 18 Nov 2026, as drive D: was at 2% free), where
  `query-code.py` indexes it; `git log -- <file>` there gives the author's own reason for a change. A repository that holds
  built files only (the report's "C# files" is 0: Dubs' three, Medical System Expansion 2): decompile for code, read the history
  for what changed. Decompile only when there is no public source, when it does not match the installed build, or for a quick
  check of the file the game actually loads, and say which you read.
- **Watch what the clones cost** (owner, 18 Nov 2026): every `refresh-mod-list.py` run prints their size and the drive's free
  space, and the tool refuses new clones on a short drive. **Never delete a clone, or anything else to free space, without
  first telling the owner** what is short and what their choices are, and waiting for their decision.
- **New local data goes on the drive the owner chose, never the repository's drive** (owner, 19 Nov 2026: "Just use K: from now
  on instead of D:"; D: was at 2% free). Caches, work files, clones and staging that are neither shipped nor committed take
  their folder from a key in `.local/install-settings.json` (`sourceClonesPath`; `artWorkPath` for the art tools' masks, parts
  and staging, read through `scripts/art/art_paths.py`, which still finds files made earlier in `.local/art`). A new tool that
  writes much gives itself such a key. The repository, shipped textures and committed files stay where they are.

### Updates of the game and of tied mods

- **Answer what an update of the game or of a mod we tie into changed with `python scripts/assembly-archive.py compare
  <file>`** (owner, 18 Nov 2026: Steam replaces the older build, so it could not be compared). Every `refresh-mod-list.py` run
  keeps new versions of `Assembly-CSharp.dll` and of every active mod our About.xml and LoadFolders.xml name. For a game build no
  snapshot saw, the owner can fetch it with DepotDownloader; **agents never log in to Steam**.
- **Whether an update or an author's source touched what we patch: `python scripts/upstream.py`** (owner's go-ahead, 18 Nov
  2026): `updates` against the archive, `pending` against the source clones' commits. Run it after `refresh-mod-list.py` and
  before an install; it matches by name, so **read each listed commit or diff before acting on it**.
- **It also runs each night on GitHub's own machine** (`.github/workflows/upstream-watch.yml`, the owner's go-ahead of 23 Nov
  2026, D'thorga Companion's `docs/online.md` O3): `pending` for the last 30 days, from clones made straight from
  `config/source-repos.json`, with its findings left on the `companion-results` branch, committed only when they change.
  **Never commit to that branch by hand**; the run alone writes it. Its kit version is the `kit-ref` the workflow names, not this
  repository's pin.
- **Known problems between other authors' mods are published in `feeds/known-problems.json`** (the owner's choice, 23 Nov 2026:
  "public, facts and links"; D'thorga Companion's `docs/online.md` O2), which every Companion's Mod list check warns of. **Check
  each entry as a report is checked before adding or changing it** (phobos-modkit `rules/reports.md`): against the author's
  newest public source or Workshop build, saying in `versions` what it was checked against and what was not; plain facts and
  what to do, no blame. Mark one `fixed` with the day once the author fixes it, rather than deleting it. Write it through the
  kit (`feeds index --folder feeds --name phobos-rimworld --title "Phobos RimWorld" --write`, then `feeds check --folder
  feeds`).
- **After an update, start with `python scripts/update-check.py`** (owner, 20 Nov 2026: the checks run by hand after that day's
  updates, "implemented into Companion"; D'thorga Companion's Since you last played runs it). What is ours is
  `config/update-check.json`, which check-targets.py reads too.
- **A row added to `docs/development/temporary-patches.md` gets its line in `config/temporary-patches.json`** (the members its
  fault lives in) or under its `notWatched` with the reason, in the same change; `tests/test_temporary_patches_list.py` holds
  them together.
- **Answer which mods patch a method with `python scripts/query-patches.py`** (owner, 8 Nov 2026), before decompiling a mod to
  look for patches, and before adding a patch of ours, to see who else is there.

### The running game

- **Answer where the running game spends its time, and which mods cause it, with `python scripts/perf.py`** (owner, 17 Nov
  2026: "build the tool"), never by reading code alone: `record --label` while the slow moment is on screen, then `report`,
  `mod <name>` and `compare`. **Ask the owner before recording their running game, and to save first**: attaching can hitch it.
  A performance fix is checked with a recording before and after; a recording measures, never tests.
- **A frozen or stuck game: `python scripts/perf.py stack`** (owner's go-ahead, 18 Nov 2026), before guessing from code.
- **Memory: start with `watch`** (owner, 19 Nov 2026), which follows the game without pausing it (`--no-counters` attaches
  nothing). Then `heap`, and **`heap-compare`** for what grew: **take three over minutes before calling it a leak**. **Ask the
  owner before each heap**, as for a recording: it pauses the game until it is done.
- **What is in memory and why it is still there: `look`, `look-kind TYPE --open` and `look-why TYPE`** (owner, 20 Nov 2026, for
  D'thorga Companion's Explorer; phobos-modkit's heap reader). Each takes a Windows snapshot that pauses the game briefly (how
  long: `external/phobos-modkit/docs/heap-reader.md`). **Ask the owner before each snapshot.** Counts include garbage not yet
  collected, so **answer "is it a leak?" with `look-why`**, never from counts alone.
- **Look before guessing** (owner, 21 Nov 2026): when a fault is about what the running game holds (a value, a cache, a thing
  that will not act) and no save or log shows it, ask the owner for a snapshot while it is happening, before reasoning from code
  alone. A snapshot shows one instant: that a value is wrong, never how it came to be.
- **Answer how long the game takes to start, and which mods cost it, with `python scripts/startup-impact.py`** (owner, 8 Nov
  2026). Start-up time varies between identical launches, so **trust only a difference that repeats**.
- **Answer which active mods are ready for a game version with `python scripts/version-readiness.py`** (`python
  scripts/workshop-research.py versions` first for the Steam tags): what each declares, never whether it works.
- **Answer what settings the owner plays with using `python scripts/mod-settings.py`** (owner, 8 Nov 2026). **Run it with a
  reported fault before asking the owner which options are on, and never read the Config folder by hand.**

### The owner's mod list and the Workshop

- **Keep `docs/development/owner-mod-list.md` up to date** (owner, 22 Oct 2026): run `python scripts/refresh-mod-list.py` (it
  only reads the game's files) before checking advice or compatibility claims against the list, and whenever the owner mentions
  changing mods; add `--online` now and then to re-read the Vanilla Expanded collection. Its suggestions are written by hand:
  when a refresh shows one taken up or stale, update them from the mods' Workshop pages and label your own reasoning.
- **Read a mod's latest player comments before suggesting it** (owner, 22 Oct 2026: a Workshop description and a recent update
  date don't show unfixed bugs), and say which picks are comment-checked. **Read them with `python scripts/workshop-research.py
  report <mod>`** (names, package IDs or Workshop IDs; its first line says what came from the cache).
- **Answer what a Steam collection holds with `python scripts/workshop-research.py collection <ID or URL>`**, the first check
  of whether another mod already does a thing. Steam's item pages refuse automated fetches, so **never curl a Workshop page**.
  `docs/development/project-direction.md` names the mods that matter to these designs.

## Builds, installation and checks

- **Answer what updating or going back would change with `python scripts/installs.py`** (owner, 18 Nov 2026: a part of
  D'thorga Companion for "updating, reversing changes, making backups", showing what each change gains or loses; the
  Companion's Mods page runs it; `docs/development/mod-installs.md`). So **a changelog entry is what the owner reads before
  updating**: keep its bold opening when rewording it, and put save effects under "Save compatibility". Status also says which
  mods need a build (`python scripts/deploy.py --build-only` builds without installing, fine while the game runs).
- **Builds take the repository's build lock** (kit 1.103.0; the owner's report of 23 Nov 2026: an install failed while another
  session's gate built the same projects): deploy's build and the gate's build lane each wait while the other builds. A build run
  by hand or by an editor is not covered.
- **Answer what code is behind a changelog entry with `python scripts/changes.py`** (owner, 19 Nov 2026: a code viewer in
  D'thorga Companion "to show off all the new and shiny code associated with any particular CHANGELOG item"). Nothing new is
  asked of a changelog; it rests on the shared rule that a change and its entry go in one commit.
- **Answer what happened to our mods over time with `python scripts/history.py`** (owner, 19 Nov 2026: a timeline of "how
  updates were applied, changed, removed", to read a changelog "from a particular update-in-moment"); the Companion's Timeline
  page runs it. **A test that installs, backs up or encodes points `PHOBOS_HISTORY` and `PHOBOS_SETTINGS` into its own folder**
  (`tests/test_deploy.py` does), or it writes into the owner's real log.
- **Answer what is safely copied, and copy what cannot be made again, with `python scripts/safe-copies.py`** (owner, 21 Nov
  2026: "backup functionality for our training sets, models, etc."). What it copies is `config/safe-copies.json`; **a new set
  of art work that cannot be made again gets an entry there in the same change**. The owner's choices:
  - Copies go to K: (`safeCopiesPath`, marked private and local) and the private repository dthorga-safe-copies
    (`safeCopiesRepositoryPath`), pushed after each run.
  - **Anything made from the game's art goes to K: only**; a training counts as ours only when its manifest's hashes prove
    every picture.
  - **RimWorld's Config folder, RimSort's own files and Claude's notes about the owner are personal: K: only too** (owner, 22
    Nov 2026); the notes are the shared tools' own list (`--root external/phobos-modkit`).
  - **The saves: the newest pair of the owner's rotation, autosaves left out, five earlier copies of each on K:** (owner, 22 Nov
    2026); older copies of a save are the one thing it lets go by itself.
  - **The saves, the Config folder and RimSort's files are the game's, so they live in the shared tools' list for RimWorld**
    (kit 1.71.0, the shared rule `rules/safe-copies.md`: `--root external/phobos-modkit --game rimworld`); this repository's
    list keeps its own work.
  - **`run-all` runs every list on the owner's games list**; `art-run.py night` runs it after the last step with `--verify`, and
    the daily run (`safe-copies.py schedule`, set from D'thorga Companion) runs it too. It uses no graphics card, so it may run
    while the owner plays.
  - `let-go` removes a copy the list no longer takes only on the owner's word. **Never delete or prune any other copy without
    the owner's word**, and never touch the saves folder itself: the tool only reads it.
- **Every path a script reads goes through the shared order** (owner, 19 Nov 2026: "a central settings page where everything
  or at least, most, of these sorts of things are managed"): argument, environment, `.local/install-settings.json`, the central
  settings file, default, through `deploy.local_path` (phobos-modkit's `paths.resolve`). **A new setting of a script of ours is
  declared in `config/settings.json`**, so `python scripts/settings.py` and the Companion's Settings page list it.
- **A script the owner may wait on says how far along it is** (owner, 19 Nov 2026: progress bars with status updates): count
  its steps through phobos-modkit's `Progress` (`scripts/deploy.py`'s `Steps`), which writes only when a program asks, so a
  console sees no change. The Companion draws the bar.
- **Answer whether textures need encoding before a launch with `python scripts/textures.py`** (owner, 18 Nov 2026: "it's
  always a wild guess"). `encode --mod` writes `.dds` files into the named mods' folders, **Workshop ones included, by the
  owner's choice**, and refuses while RimWorld runs. **It and deploy then shorten the mip chains Waylith would unpack** (owner's
  choice, 20 Nov 2026: the owner's game runs on Waylith). **New art avoids it with power-of-two canvas heights, or heights with
  many factors of two** (1408 = 11 × 128 keeps six levels).
- Scale checks to the change; test rules and persistence, not static documentation. **Before committing, run `python
  scripts/check-all.py` and chain the commit on its exit code** (`python scripts/check-all.py && git commit ...`): the gate
  builds every project, runs every rule-test project and the Python tests, and checks links, mod layout, text keys, vehicle
  schemas, version copies and changelogs (owner's go-ahead, 8 Nov 2026). Its steps are in `config/gate.json`, found by glob, so
  a new project is never left out. `--only` and `--skip` narrow it while iterating; **never hand-type a loop of builds and
  tests** (one missed four of seven test projects). Read a failed step's full output, whose path the report gives, only when
  its summary is not enough.
- **The commit guard enforces it** (owner's go-ahead, 18 Nov 2026): `.githooks/pre-commit` refuses a commit whose staged files
  no full, un-narrowed passing gate run saw exactly as staged. **After cloning, run `git config core.hooksPath .githooks`**;
  never pass `--no-verify`. It cannot pass a commit that stages only part of a file: when another session has uncommitted
  changes in a file you need, wait for its commit (or agree with it to commit the file together).
- **The projects table and the Harmony patch list in `docs/development/building.md`, and the player guide's table of what each
  mod needs, are generated by `python scripts/doc-tables.py --write`** (kit 1.107.0, the owner's docs rewrite step 3, 23 Nov
  2026), from the project files, the C# source and the mods' About.xml and LoadFolders.xml, as `config/doc-tables.json` says.
  **Run it after changing a project file, a Harmony patch or a mod's About.xml or LoadFolders.xml**, and commit the docs with the
  change: the gate's "doc tables" step fails while one is out of date. Never edit a table by hand; answer "which methods does
  this project patch?" from the list before grepping the source. Another author's mod named only by package ID gets its name
  in the config's `names`. **Every mod's settings are generated into `docs/development/settings-reference.md`** (kit 1.108.0): label, what it
  does, default, page and section, read from the settings classes, the window's drawing code and the English Keyed files. **Run it
  after adding, removing or relabelling a setting**; a fix to a label or tooltip goes in the Keyed file. The READMEs' settings
  tables for players stay hand-written (agent choice of 23 Nov 2026, owner to revise: the generated text is not yet plain enough
  for a player's page).
- **Every grant, figure, mount, carried store and upgrade-node key our fittings, tunes and upgrade trees declare must be read by
  something** (the owner's choice of 27 Nov 2026, after the fittings audit: "yes, as a gate check"): the gate's "declared effects" step,
  phobos-modkit's `effects check`, by this repository's `config/effects.json`. **A new fitting or node that fails it is finished, not
  excused**: add what reads it, or, only where the name is a deliberate marker, an `accepted` entry with why. A new kind of reader (a
  Def field naming a grant or a figure) goes in the table's `readers`. Run `effects list` for the whole picture.
- **`python scripts/check-targets.py` checks what the compiler cannot** (owner, 9 Nov 2026; part of the gate): it says the
  Harmony and by-name targets still exist, never that the code still behaves. **Write a by-name tie-in so it can be read** (17
  Nov 2026, after the Mongoose crash fix had to be checked by hand): the full `"Namespace.Type:Member"` or a fixed type name
  (`OptionalMods.TypeNamed`, `TypeByName`, a string constant) in the `TargetMethod` or lookup itself, never a name put together
  from a parameter; its "worked out at run time" table lists what it still cannot read.
- **Tests leave nothing behind.** A test that makes a git repository sets `core.fsmonitor false` in it and deletes it with
  read-only handling (git's object files are read-only; `ignore_errors=True` hides the failure).
- **`python scripts/cleanup.py` lists what the tools, builds and tests left behind, with sizes and why** (owner, 11 Nov 2026:
  phobos-modkit's `cleanup`); **the owner runs `--apply`**. **Something new that a script or test of ours leaves behind gets a
  rule in `config/cleanup.json`** in the same change, never a one-off deletion.
- **The shared tools come from the pinned phobos-modkit submodule** (`external/phobos-modkit`, owner's request of 7 Nov 2026 for
  a shared repository): **after cloning, or in a new worktree, run `git submodule update --init`** before the scripts or tests.
  A fix to a shared tool goes to phobos-modkit first, under its own AGENTS.md; then move the pin here with `git -C
  external/phobos-modkit checkout <tag>` and a commit. **Never edit the submodule in place.**
