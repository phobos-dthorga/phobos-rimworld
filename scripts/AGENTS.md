# Tools, scripts and tests

How to answer a question with a tool, and how the scripts and tests are written and run: the detail behind the root's table of
which tool answers which question. A session reads it before using a tool, writing a script or a test, installing, or committing.

Split from the root `AGENTS.md` on 21 Nov 2026 (the owner's yes: "keep a slimmed-down AGENTS at the root"), every rule moved word
for word; the root's rules apply here too.

## Working with the owner

- The owner runs gameplay tests. Do not control the owner's mouse or keyboard; give instructions for
  interactive steps. Never edit, replace or delete save files without explicit authorisation (read-only
  copies for diagnosis are fine; delete them afterwards). **Answer questions about a save with
  `python scripts/query-save.py`** (pawns, hediffs, things, what holds a thing, map grids such as gas, roofs and
  terrain with `grid`, **everything one thing saved with `thing <ID>`** (a vehicle's systems and fault history, a comp's values;
  `--path "*Phobos*"` to narrow it), pollution on the world map with `planet` (`--near` a place, `--compare` a save, `--history` for when it arrived), settlements by faction with `world`, mods' saved state such as RimPacts' wars with `components`,
  how high places stand and how rough the land is with `terrain` (`--near` a place), where the colony's mood suffers, who may break and where to start with `mood` (`--pawn` for one; a save holds remembered
  thoughts only, and Framework's colony mood file from the same save adds every situational one), what changed since another save with `diff`, any read-only SQL; it indexes a frozen copy and
  deletes it) and read the raw save only for what the index does not hold. Read the owner's `Player.log` before assuming a
  reported fault is the owner's error, and **start with `python scripts/triage-log.py`**: it summarises the log
  against `Player-prev.log` and names the mods involved, and `--show <ID>` prints one message in full. Open the
  raw log only for what the summary cannot answer.
- **When the game closes by itself with little in its log, run `python scripts/crash-dump.py`** (owner, 9 Nov 2026): it reads the newest RimWorld dump in Windows' CrashDumps folder without a debugger, and names
  the exception and any call loop that ran away. A small dump cannot name .NET methods; for a stack overflow the game's error output can (`docs/development/crash-2026-11-09-stack-overflow.md`).
  **A full dump names them** (owner's go-ahead, 18 Nov 2026): crash-dump then asks Microsoft's dotnet-dump which method and assembly each
  code address on the failing stack falls in. **`python scripts/crash-dump.py setup`** says whether Windows keeps full dumps of RimWorld and
  gives the PowerShell lines that ask it to; the owner runs them (or `setup --apply`) in an administrator window. **Agents never run
  `--apply` or `--off`.** A full dump is as large as the game's memory when it crashed.
- **Answer which mod defines or patches a Def with `python scripts/query-defs.py`** (`who <defName>` for every
  definition, the one the game keeps and every patch naming it; `show` for its XML and parents, `--lines 0` for a long Def in
  full; `list`, `patches`,
  `sql`) before searching mod folders by hand. It shows Defs as written, without other mods' patches applied; **`patched
  <defName>` shows a Def with every patch applied and a diff from its written form**, from the file D'thorga Framework writes
  on request (dev mode: its settings or the dev action "write patched Defs at the next start", then a restart).
- **Read another mod's Defs as patched by default** (owner, 8 Nov 2026). `python scripts/query-defs.py patched <defName>`, from
  `PatchedDefs.xml`, is the first source for what a Def really is in the owner's game; `who` and `show` then say who defines and
  patches it and give the parent chain. Fall back to the as-written view only when the file has a blind spot, and say which: it is
  missing or older than the change in question (the report says when it may be); the Def is made in code; C# changes it at startup
  (Vanilla Expanded Framework sets its delivery spot's build category that way); inherited values are needed (the file is written
  before inheritance); or the mod was not active when it was written.
- **Answer questions about the game's or another mod's code with `python scripts/query-code.py`** (owner, 8 Nov 2026):
  `show Type.Member` for its source, `outline Type` for what a type holds, `refs Name` for every line using a name grouped
  by the member it is in, `find "*Pattern*"`; `--in "VehicleFramework*"` narrows to one decompile, `--in ours` to our
  `src/`. It reads every tree in `.local/decompiled` and indexes them in the kit's cache. Use it before grepping a decompile
  or sending an agent to read one, and cite what it shows as `file:line`. A tree not yet decompiled needs decompiling
  first (into `.local/decompiled/<name>`). **`compare OLD.dll NEW.dll` says what changed between two builds** (owner, 9 Nov 2026),
  member by member: added, removed, new signatures, new bodies, and "values only" for a changed number or string; `--show Type.Member`
  for one diff, `--only "*Pattern*"` to narrow it. Its decompiles stay in the kit's cache, outside every repository. Compare two `.dll`s or
  two trees decompiled the same way. A single assembly's metadata, call graph or constants are REA's job only where it can read them (evaluated
  9 Nov 2026: not the game's assembly, and whole-file only for small mods; an unread file exits as a success with zeros); the
  owner has it set up (`docs/development/analysis-tooling.md`).
- **Public source before a decompile** (owner, 18 Nov 2026: the original source "will be so much more rich in what it can offer").
  Before decompiling another author's assembly, look for its public repository, GitHub first: **read `config/source-repos.json`,
  and run `python scripts/source-repos.py --write`** when the mod is missing or unconfirmed there (phobos-modkit's rimworld-sources:
  About.xml, the Workshop page, then a GitHub search, each repository checked against the mod's packageId). An unconfirmed
  candidate may still be its source (a rename, or source with no About.xml): judge it and say so. Where one exists, read it at the tag or
  commit matching the build in play, with its commit history for what changed and why; **clone it with `python scripts/source-repos.py
  --clone`** (lean clones: whole history, source files only; `--update-clones` to bring them up to date) into `sourceClonesPath` from
  `.local/install-settings.json` (the owner chose `K:\phobos-modkit\sources`, 18 Nov 2026, as drive D: was at 2% free), where
  `query-code.py` indexes it; `git log -- <file>` there gives the author's own reason for a change. Some repositories hold built files
  only (the report's "C# files" is 0: Dubs' three, Medical System Expansion 2): decompile for code, read the history for what changed.
  **Watch what the clones cost** (owner, 18 Nov 2026): every `refresh-mod-list.py` run prints their size and
  the drive's free space, and the tool refuses new clones on a short drive. **Never delete a clone, or anything else to free space,
  without first telling the owner** what is short and what their choices are, and waiting for their decision. Decompile only when there is no public source, when it does not match the installed build, or
  for a quick check of the file the game actually loads, and say which you read.
- **New local data goes on the drive the owner chose, never the repository's drive** (owner, 19 Nov 2026: "Just use K: from now on
  instead of D:"; D: was at 2% free). Caches, work files, clones and staging that are neither shipped nor committed take their folder
  from a key in `.local/install-settings.json` (`sourceClonesPath`; `artWorkPath` for the art tools' masks, parts and staging, read
  through `scripts/art/art_paths.py`, which still finds files made earlier in `.local/art`). A new tool that writes much gives itself
  such a key. The repository, shipped textures and committed files stay where they are.
- **Answer what an update of the game or of a mod we tie into changed with `python scripts/assembly-archive.py compare <file>`**
  (owner, 18 Nov 2026: Steam replaces the older build, so it could not be compared). Every `refresh-mod-list.py` run keeps new
  versions of `Assembly-CSharp.dll` and of every active mod our About.xml and LoadFolders.xml name (phobos-modkit's rimworld-archive,
  outside this repository); `list` shows what is kept, `--show Type.Member` one member's diff. For a game build no snapshot saw,
  the owner can fetch it with DepotDownloader (the kit's `docs/rimworld-archive.md`); agents never log in to Steam.
  **Whether an update or an author's source touched what we patch: `python scripts/upstream.py`** (owner's go-ahead, 18 Nov 2026):
  every member `src/` names on another author's type, against the archive's last update of each file (`updates`) and the source
  clones' commits since (`pending`, a warning before the Workshop build carries them). Run it after `refresh-mod-list.py` and before
  an install; it matches by name, so read each listed commit or diff before acting on it.
  **After an update, start with `python scripts/update-check.py`** (owner, 20 Nov 2026: the checks run by hand after that day's
  updates, "implemented into Companion"; D'thorga Companion's Since you last played runs it): our patches' targets, whether an update
  touched the method a temporary patch's fault lives in, each update's size and new types, and whether our XML patches still find
  their Defs (as written, then in the patched-Defs file). What is ours is `config/update-check.json`, which check-targets.py reads
  too. **A row added to `docs/development/temporary-patches.md` gets its line in `config/temporary-patches.json`** (the members its
  fault lives in) or under its `notWatched` with the reason, in the same change; `tests/test_temporary_patches_list.py` holds them together.
- **Answer which mods patch a method with `python scripts/query-patches.py`** (owner, 8 Nov 2026): `who "Type.Method"` for
  every Harmony patch on it in load order (wildcards), `mod <name>` for what a mod patches, `hotspots` for methods several mods
  patch; "may skip the original" marks a bool prefix. It reads every active mod's assemblies through their metadata, never
  running them. Use it before decompiling a mod to look for patches, and before adding a patch of ours, to see who else is there.
- **Answer where the running game spends its time, and which mods cause it, with `python scripts/perf.py`** (owner, 17 Nov 2026:
  "build the tool"), never by reading code alone: `record --label` while the slow moment is on screen, then `report`, `mod <name>`
  and `compare` (shares of ticking, so a faster game cannot hide a gain). It attaches Microsoft's dotnet-trace and dotnet-counters
  to RimWorld 1.7 from outside and changes nothing in it, but **ask the owner before recording their running game, and to save
  first**: attaching can hitch it. A performance fix is checked with a recording before and after; a recording measures, never tests.
  **A frozen or stuck game: `python scripts/perf.py stack`** (owner's go-ahead, 18 Nov 2026), before guessing from code: snapshots of
  every thread's stack a moment apart, whether the main thread moved, the frames that never did and the innermost mod on them.
  **Memory: start with `watch`** (owner, 19 Nov 2026): the game's memory over minutes or hours without pausing it, whether it climbs
  steadily or in jumps, .NET's heap against the rest, and the saves and errors around each jump; `--no-counters` attaches nothing.
  Then `heap`, by type and by mod, and **`heap-compare`** for what grew (take three over minutes before calling it a leak); **on the
  owner's game a heap snapshot holds only part of the heap** (it stops near ten million objects), and its report says so.
  **Ask the owner before each**, as for a recording; heap forces a full garbage collection, so the game pauses until it is done.
  **What is in memory and why it is still there: `look`, `look-kind TYPE --open` and `look-why TYPE`** (owner, 20 Nov 2026, for
  D'thorga Companion's Explorer; phobos-modkit's heap reader): a Windows snapshot, a pause of about half a second, then every
  object with no cap, any object's values, and paths from a root. It counts garbage not yet collected too, so **answer "is it a
  leak?" with `look-why`**, never from counts alone. Ask before each snapshot, as above.
  **Look before guessing** (owner, 21 Nov 2026): when a fault is about what the running game holds (a value, a cache, a thing that
  will not act) and no save or log shows it, ask the owner for a snapshot while it is happening, before reasoning from code alone.
  A snapshot shows one instant: that a value is wrong, never how it came to be.
- **Answer how long the game takes to start, and which mods cost it, with `python scripts/startup-impact.py`** (owner, 8 Nov 2026):
  every launch, the costliest mods, `compare` for what changed between two launches, `mod <name>` for one mod. Start-up time varies
  between identical launches, so trust only a difference that repeats. **Answer which active mods are ready for a game version
  with `python scripts/version-readiness.py`** (`--game-version 1.8` for another; `python scripts/workshop-research.py versions` first
  for the Steam tags): what each declares and how closely it depends on the game, never whether it works.
- **Answer what settings the owner plays with using `python scripts/mod-settings.py`** (owner, 8 Nov 2026): our mods' saved
  values beside their defaults, read from our C# source; `--mod <pattern>` for any other mod (Combat Extended, Vehicle Framework);
  `snapshot FILE`, then `compare FILE`, for what changed between two sessions. Run it with a reported fault before asking the
  owner which options are on, and never read the Config folder by hand. RimWorld writes nothing for a value at its default.
- **Tools before LLM reading, even when the tool needs work first** (owner, 7 November 2026). When an analysis
  (of logs, saves, mod Defs, assemblies, Workshop pages or anything else) can be done by a tool we already have,
  in phobos-modkit or `scripts/`, use the tool. When it could be done by one only after a fix, an update or a new
  feature, **make that change first and then run the tool**, rather than spend more tokens having an agent read
  the raw material for the same or much the same answer. Shared changes go to phobos-modkit under its own
  rules, then the pin moves here. Read raw material by hand only for what no reasonable tool change would
  answer, and say so in the reply. The tools, their order and their gaps are in
  `docs/development/analysis-tooling.md`.
- The owner plays RimWorld 1.7 (since 26 October 2026; the last 1.6 save is archived) with all five DLCs and a large mod list
  (about 340 mods, managed with RimSort, including Combat Extended). **Keep `docs/development/owner-mod-list.md` up
  to date** (owner, 22 Oct 2026): run `python scripts/refresh-mod-list.py` (it only reads the game's files)
  before checking advice or compatibility claims against the list, and whenever the owner mentions changing
  mods; add `--online` now and then to re-read the Vanilla Expanded collection. Its suggestions are written by
  hand: when a refresh shows one taken up or stale, update them from the mods' Workshop pages and label your
  own reasoning. **Read a mod's latest player comments before suggesting it** (owner, 22 Oct 2026: a Workshop
  description and a recent update date don't show unfixed bugs), and say which picks are comment-checked. **Read
  them with `python scripts/workshop-research.py report <mod>`** (names, package IDs or Workshop IDs; paced and
  cached: details for an hour, comments for a day, and its first line says what came from the cache), and use its `versions` report for which active mods declare the game's version. **Answer what a Steam collection
  holds with `python scripts/workshop-research.py collection <ID or URL>`** (`--grep "*tank*"` for a vehicle type), the first
  check of whether another mod already does a thing: Steam's item pages refuse automated fetches, its keyless APIs do not, so
  never curl a Workshop page. `docs/development/project-direction.md` names the mods that matter to these designs. The PC
  is high-end: treat measured performance as a best case for players.

## Builds, installation and checks

- **Answer what updating or going back would change with `python scripts/installs.py`** (owner, 18 Nov 2026: a part of D'thorga
  Companion for "updating, reversing changes, making backups", showing what each change gains or loses): each mod's installed copy,
  the repository's and every backup, compared by the changelogs each carries (phobos-modkit's installs; `docs/development/mod-installs.md`).
  The Companion's Mods page runs it. So **a changelog entry is what the owner reads before updating**: keep its bold opening when
  rewording it, and save effects under "Save compatibility". deploy writes `InstallRecord.json` into every installed copy.
  Status also says which mods need a build (`python scripts/deploy.py --build-only` builds without installing, fine while the game
  runs), the files an install would change, its settings changes (`mod-settings.py changes --json`) and the test rows it brings.
- **Answer what code is behind a changelog entry with `python scripts/changes.py`** (owner, 19 Nov 2026: a code viewer in D'thorga
  Companion "to show off all the new and shiny code associated with any particular CHANGELOG item"): `entry --changelog PATH --text
  "its opening"` gives the commit that first wrote the entry and its diff, code first. Nothing new is asked of a changelog; it rests
  on the shared rule that a change and its entry go in one commit. **A commit that writes several entries is shown whole**, the
  others named; how commits are cut is in the shared rules (commit separate changes separately, one finished before the next).
- **Answer what happened to our mods over time with `python scripts/history.py`** (owner, 19 Nov 2026: a timeline of "how updates
  were applied, changed, removed", to read a changelog "from a particular update-in-moment"): every fact from git back to the first
  commit, and installs, go-backs, backups, builds and encoded textures from the history log in D'thorga Companion's folder (kept from
  19 Nov 2026 on); `at --changelog PATH --when TIME` gives a changelog as it stood. The Companion's Timeline page runs it. **A test
  that installs, backs up or encodes points `PHOBOS_HISTORY` and `PHOBOS_SETTINGS` into its own folder** (`tests/test_deploy.py`
  does), or it writes into the owner's real log.
- **Answer what is safely copied, and copy what cannot be made again, with `python scripts/safe-copies.py`** (owner, 21 Nov 2026:
  "backup functionality for our training sets, models, etc."; phobos-modkit's safe-copies): `status` (the default) says each training,
  model and note set's kind and whether it is copied, changed since or never copied; `plan`, `run`, `verify`, `restore`. What it
  copies is `config/safe-copies.json`; **a new set of art work that cannot be made again gets an entry there in the same change**.
  The owner's choices: copies go to K: (`safeCopiesPath`, marked private and local) and the private repository dthorga-safe-copies
  (`safeCopiesRepositoryPath`), pushed after each run. **Anything made from the game's art goes to K: only**; a training counts as
  ours only when its manifest's hashes prove every picture. **RimWorld's Config folder, RimSort's own files and Claude's notes about
  the owner are personal: K: only too** (owner, 22 Nov 2026); the notes are the shared tools' own list (`--root
  external/phobos-modkit`). **The saves: the newest pair of the owner's rotation, autosaves left out, five earlier copies of each
  on K:** (owner, 22 Nov 2026); older copies of a save are the one thing it lets go. `art-run.py night` runs both lists after the
  last step; it uses no graphics card, so it may run while the owner plays. **Never delete or prune any other copy without the
  owner's word**, and never touch the saves folder itself: the tool only reads it.
- **Every path a script reads goes through the shared order** (owner, 19 Nov 2026: "a central settings page where everything or at
  least, most, of these sorts of things are managed"): argument, environment, `.local/install-settings.json`, the central settings
  file, default, through `deploy.local_path` (phobos-modkit's `paths.resolve`). **A new setting of a script of ours is declared in
  `config/settings.json`**, so `python scripts/settings.py` and the Companion's Settings page list it.
- **A script the owner may wait on says how far along it is** (owner, 19 Nov 2026: progress bars with status updates): count its
  steps through phobos-modkit's `Progress` (`scripts/deploy.py`'s `Steps`), which writes only when a program asks, so a console
  sees no change. The Companion draws the bar.
- **Answer whether textures need encoding before a launch with `python scripts/textures.py`** (owner, 18 Nov 2026: "it's always a
  wild guess"): every active mod, ours and others', stale (the game would show the old picture: it loads a `.dds` instead of the
  `.png` beside it without comparing dates), never encoded, or left over. `encode --mod` writes `.dds` files with RimSort's own
  todds preset into the named mods' folders, **Workshop ones included, by the owner's choice**, and refuses while RimWorld runs.
  **It and deploy then shorten the mip chains Waylith would unpack** (owner's choice, 20 Nov 2026: the owner's game runs on Waylith,
  which unpacks a compressed texture at four times the memory when a mip height four or more pixels tall is not a multiple of four),
  and status counts the textures it would unpack ("Unpacked by Waylith"; D'thorga Companion's Mods page offers encoding for them).
  New art avoids it with power-of-two canvas heights, or heights with many factors of two (1408 = 11 × 128 keeps six levels).
- Scale checks to the change; test rules and persistence, not static documentation. **Before committing, run
  `python scripts/check-all.py` and chain the commit on its exit code** (`python scripts/check-all.py && git commit ...`),
  never on reading output: phobos-modkit's gate builds every project, runs every rule-test project and the Python tests,
  and checks links, mod layout, text keys, vehicle schemas, version copies and changelogs, printing only what failed
  (owner's go-ahead, 8 Nov 2026; the steps are in `config/gate.json`, found by glob, so a new project is never left out).
  `--only` and `--skip` narrow it while iterating; never hand-type a loop of builds and tests (one missed four of seven
  test projects). Read a failed step's full output, whose path the report gives, only when its summary is not enough.
  **The commit guard enforces it** (owner's go-ahead, 18 Nov 2026): `.githooks/pre-commit` refuses a commit whose staged files no full,
  un-narrowed passing gate run saw exactly as staged. **After cloning, run `git config core.hooksPath .githooks`**; never pass
  `--no-verify`. It cannot pass a commit that stages only part of a file, since the gate checked the whole file: when another
  session has uncommitted changes in a file you need, wait for its commit (or agree with it to commit the file together).
- **`python scripts/check-targets.py` checks what the compiler cannot** (owner, 9 Nov 2026; part of the gate): every Harmony patch and reflected
  lookup named in a string, the overload it asks for, and the parameters Harmony fills at run time or matches by name, against the game's and the
  active mods' assemblies as metadata. It says the names still exist, never that the code still behaves. **Write a by-name tie-in so it can be
  read** (17 Nov 2026, after the Mongoose crash fix had to be checked by hand): the full `"Namespace.Type:Member"` or a fixed type name (`OptionalMods.TypeNamed`,
  `TypeByName`, a string constant) in the `TargetMethod` or lookup itself, never a name put together from a parameter; its "worked out at run time"
  table lists what it still cannot read.
- **Tests leave nothing behind.** A test that makes a git repository sets `core.fsmonitor false` in it and deletes it with
  read-only handling (git's object files are read-only; `ignore_errors=True` hides the failure). **`python scripts/cleanup.py`
  lists what the tools, builds and tests left behind, with sizes and why** (owner, 11 Nov 2026: phobos-modkit's `cleanup`): stale and
  unused save indexes and other cache entries, the shared tools' development build, old install backups, test repositories, .NET's
  per-build logs, older RimWorld crash dumps and git watcher daemons; the owner runs `--apply`. **Something new that a script or test
  of ours leaves behind gets a rule in `config/cleanup.json`** in the same change, never a one-off deletion.
- **The shared tools come from the pinned phobos-modkit submodule** (`external/phobos-modkit`, owner's request of
  7 Nov 2026 for a shared repository): **after cloning, or in a new worktree, run `git submodule update --init`**
  before the scripts or tests. A fix to a shared tool goes to phobos-modkit first, under its own AGENTS.md; then
  move the pin here with `git -C external/phobos-modkit checkout <tag>` and a commit. Never edit the submodule in
  place.
