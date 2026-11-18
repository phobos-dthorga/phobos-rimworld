# Phobos RimWorld contributor instructions

These are the standing rules for work in this repository: owner directions still in force, stated once
and in their current form. What each version delivered lives in the owning mod's
`mods/<Mod>/CHANGELOG.md`; designs, research and records live in `docs/development/` (indexed in
`docs/development/README.md`). The project started on 18 October 2026 from the owner's radio framework
design (`docs/development/radio-framework-design.md`); its conventions follow Phobos Ostranauts
(`phobos-dthorga/phobos-ostranauts`), adapted to RimWorld. Keep this file to rules: when a direction is
delivered, record the delivery in the changelog or design record, and add or amend only the rule here.

## Rules kept beside their work

Three sets of rules live in the folders they govern, so a session loads them only when it works there (owner's go-ahead,
18 Nov 2026, to cut what every session reads). **Read the file before that work, even if no file in its folder is open yet**:
running a script does not load them.

- **Art and sound**: [`assets/AGENTS.md`](assets/AGENTS.md). Before generating, picking, cutting, exporting, recording or
  shipping any picture or sound, and before touching a texture or a paint mask.
- **Vehicles and submarines**: [`src/PhobosFramework.Vehicles/AGENTS.md`](src/PhobosFramework.Vehicles/AGENTS.md). Before
  designing or changing any vehicle, upgrade tree, accessory, part or submarine mechanic, in XML or C#.
- **Radio framework**: [`src/PhobosRadio/AGENTS.md`](src/PhobosRadio/AGENTS.md): its phases and its physics rules.
- **The radio framework is on the back burner** (owner, 9 Nov 2026): no new radio features or phases, and no reviews of it, until the
  owner picks it up again. The mod stays as it is, and a fix to something broken in it is still fine when the owner reports it.

## Working with the owner

- Practical, small-team project: prefer a working slice over speculative frameworks or process. Aim for
  maximum creative freedom (owner, carried over from Phobos Ostranauts): risks such as a mod becoming
  unsafe to remove from a save are engineering problems to report honestly and explore, never a reason
  to drop, defer or shrink a design.
- Candidly recommend simplifying, setting aside or changing direction when gameplay value, engine limits
  or maintenance cost make further work unconvincing. Never continue merely because effort was spent;
  never invent hour estimates.
- **Every new behaviour answers for itself before it is built** (owner, 8 Nov 2026): its design record says what it lets the player
  do, what it stops them doing, and how it changes the way they do things. Ask this when a behaviour is introduced, not for every
  figure or Def. A behaviour with no answer to any of the three is not built.
- Explain what works, what was checked and what remains uncertain. Distinguish observed game behaviour
  from proposed designs and untested assumptions. A build, an offline check or a unit test is never an
  in-game test. **Nothing counts as tested in game until the owner says so**; write "not yet tested in
  game" in READMEs, changelogs and replies, and never claim untested compatibility.
- **A feature with a test row notes itself when seen working** (owner, 17 Nov 2026: passes seen in play rarely reached the test list):
  a `Seen.Note("area.what", thing, detail)` where the behaviour happens (D'thorga Framework's `SeenInPlay`, behind its setting, off by
  default), and the key against its row in `config/test-evidence.json`, in the same change; `tests/test_test_evidence.py` holds the two
  together. A note says the code ran without an error, never that the result looked right: **a row it flags is "seen working, awaiting
  the owner's word"** until the owner passes or fails it (D'thorga Companion's Tests page). **Answer what the test list is waiting for
  with `python scripts/test-pass.py report`**, and write the owner's verdicts and new sightings into it with `apply --apply` (phobos-modkit's
  test-pass; a preview without the flag), never by hand-copying them. **A verdict the owner gives in conversation** goes in the same way:
  `apply --verdict "ROW:WORD:NOTE" --apply`, WORD `passed`, `partial` (part of the row, the note saying which) or `failed`.
- **A note carries what makes it evidence** (owner, 18 Nov 2026: `docs/development/seen-in-play-checks.md`): pass the thing it was seen on,
  the case (`variant`) where one feature has several (a stage, a warning level), and its figures (`values`) where the row expects an amount.
  **A note that builds its text or key at the call sits behind `Seen.Wants("key", thing)`** (or `Seen.On` for a built key), so it costs
  nothing with the setting off and only its first sighting with it on; `tests/test_seen_note_cost.py` refuses one that does not. In `config/test-evidence.json` split a row of several claims into `parts`, narrow a generic note with a `detail`
  wildcard or a case, give `expect` figures where a hollow note could pass for a real one, and add an `order` rule where the design promises
  one thing before another (a warning before a refusal). A note with nothing narrowing it stands for one row unless `shared` says why. The
  report's "figures off", "out of order" and "seen only before the row last changed" sections are faults to look into, never passes.
- The owner runs gameplay tests. Do not control the owner's mouse or keyboard; give instructions for
  interactive steps. Never edit, replace or delete save files without explicit authorisation (read-only
  copies for diagnosis are fine; delete them afterwards). **Answer questions about a save with
  `python scripts/query-save.py`** (pawns, hediffs, things, what holds a thing, map grids such as gas, roofs and
  terrain with `grid`, pollution on the world map with `planet` (`--near` a place, `--compare` a save, `--history` for when it arrived), settlements by faction with `world`, mods' saved state such as RimPacts' wars with `components`,
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
- **Answer what an update of the game or of a mod we tie into changed with `python scripts/assembly-archive.py compare <file>`**
  (owner, 18 Nov 2026: Steam replaces the older build, so it could not be compared). Every `refresh-mod-list.py` run keeps new
  versions of `Assembly-CSharp.dll` and of every active mod our About.xml and LoadFolders.xml name (phobos-modkit's rimworld-archive,
  outside this repository); `list` shows what is kept, `--show Type.Member` one member's diff. For a game build no snapshot saw,
  the owner can fetch it with DepotDownloader (the kit's `docs/rimworld-archive.md`); agents never log in to Steam.
  **Whether an update or an author's source touched what we patch: `python scripts/upstream.py`** (owner's go-ahead, 18 Nov 2026):
  every member `src/` names on another author's type, against the archive's last update of each file (`updates`) and the source
  clones' commits since (`pending`, a warning before the Workshop build carries them). Run it after `refresh-mod-list.py` and before
  an install; it matches by name, so read each listed commit or diff before acting on it.
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
  **Memory: `heap`**, by type and by mod, and **`heap-compare`** for what grew (take three over minutes before calling it a leak).
  **Ask the owner before each**, as for a recording; heap forces a full garbage collection, so the game pauses until it is done.
- **Answer how long the game takes to start, and which mods cost it, with `python scripts/startup-impact.py`** (owner, 8 Nov 2026):
  every launch, the costliest mods, `compare` for what changed between two launches, `mod <name>` for one mod. Start-up time varies
  between identical launches, so trust only a difference that repeats. **Answer which active mods are ready for a game version
  with `python scripts/version-readiness.py`** (`--game-version 1.8` for another; `python scripts/workshop-research.py versions` first
  for the Steam tags): what each declares and how closely it depends on the game, never whether it works.
- **Tell the owner whenever a setting of ours is added, removed or given a new default** (owner, 10 Nov 2026: "so I can take a look at
  them or modify them as needed"), in the reply that delivers the change: the mod, the setting's label as the settings window shows it,
  what it does, its default and where to find it. `python scripts/mod-settings.py changes --since REV` lists every such change since a
  commit; run it against the commit a piece of work started from before reporting it done.
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
- The owner is a licensed amateur radio operator and an applied chemist. Radio and chemistry realism
  matter; still label every approximation, authored value and gameplay simplification as such.
- Owner choices, agent defaults and agent choices made while the owner is away are labelled as such in
  design records, so the owner can revise them.
- **Who does what** (owner, carried over from Phobos Ostranauts): creative and world content (factions'
  radio voices, contact stories, letters, flavour, QSL card art text) lives in XML Defs and language files,
  never in C#, so other writers and players can add to it. Claude builds the logic, Def types, validators
  and the hooks between data and code, and may write creative content following the same rules as any
  writer: it is labelled agent-authored in the changelog and held for the owner's review.

## Git and publication boundaries

- **The repository is public, and its mods are for public release** on the Steam Workshop (owner,
  18 October 2026). Never change the repository's visibility.
- Commit and push directly to `main`; no pull requests unless the owner asks (external contributors use
  forks, see `CONTRIBUTING.md`). Never force-push or rewrite history. Edit in the base checkout, not
  `.claude/worktrees`, so the owner can follow the work.
- Several agent sessions may share this tree. Claim a shared version number with the other sessions before
  using it, commit explicit paths, and leave others' uncommitted work alone.
- **Steam Workshop.** Publication tooling (page drafts, generated release notes, an owner-run uploader)
  is adopted from Phobos Ostranauts in the change that prepares the first mod for its public beta gate,
  not before. Agents prepare and test it but never run a real upload, handle credentials, subscribe,
  announce or change an item's visibility. The game writes `About/PublishedFileId.txt` at a mod's first
  upload: commit that file then; never invent an item ID.
- **Ludeon's EULA** governs every mod: free, with no ads or calls to action of any kind (no donation links
  inside a mod), clearly presented as community-created, and never bundled with the game or its expansions. Every
  mod's description carries the EULA's disclaimer word for word, which `scripts/check-mod-layout.py` enforces:
  "Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon
  Studios Inc." and "All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon."
  (`docs/development/rimworld-modding-reference.md`).
- Never commit saves, game assemblies, decompiled game or mod source, extracted game textures or sounds,
  other authors' mod files, credentials or personal machine paths. Resolve game paths from local settings
  (`.local/`) or arguments. Game-derived research material and the owner's handoff originals stay in the
  ignored `.local/`. Synthetic fixtures only in tests.
- Agents never execute other authors' mod assemblies or scripts outside the game. Read their XML, About
  files and published source; decompiling for research is fine, but the output stays in `.local/` and is
  summarised in our own words, never committed.

## Versions, changelogs and READMEs

- Version numbers (owner rule from Phobos Ostranauts): a fix or patch raises the last number (0.3.0 to
  0.3.1), new functionality the middle number, a total rework or API overhaul the first. A release mixing a
  fix and a feature takes the middle bump. Classify before setting a version.
- A mod's version lives in `About/About.xml` (`modVersion`), every project of the mod, its `ModVersion` constants, its
  README and `docs/README.md`, all registered in `config/maintained-constants.json`. **Set it with
  `python scripts/release.py constants --set <Mod>.version=X.Y.Z --apply`** (phobos-modkit's updater checks every copy
  before writing any), never by hand; then write the changelog entry and the README's built date in the same commit,
  **and stage `config/maintained-constants.json` with them**: the tool records the new value there too.
  `scripts/check-mod-layout.py` refuses a copy the catalogue does not list, and `python scripts/release.py changelog
  mods/*/CHANGELOG.md` checks every entry fits a Workshop page.
- Every change to a mod updates its `CHANGELOG.md` in the same commit: an Unreleased section, then dated
  entries marked **Draft** until publication is confirmed, then **Released** with the real date. Sections:
  Added, Changed, Fixed, Balance, Dependencies, Save compatibility, Compatibility and limits. Record
  player-visible features, fixes, balance, dependencies, save effects, known limits and whether it is
  tested in game. No backticks or `>` in changelog lines (Ostranauts' release-note tooling rejects them).
- Every mod has a README that opens with **Read this first (gotchas)**: requirements and load order,
  whether it is safe to add to and remove from a save, settings to decide before starting a colony, and
  whether it is tested in game. Follow `docs/development/player-language.md`.

## Player language and localisation

- All player-facing English follows `docs/development/player-language.md`: in-game text, settings,
  READMEs, guides and Workshop drafts. Write for players with no radio, engineering or coding knowledge;
  the Engineer display mode may show the full maths, the Plain mode never shows a decibel.
- Every gizmo, command and dialog says what it did, and every refusal says why (a disabled gizmo carries
  its reason); never ship a silent success or a silent refusal. **Every new button gets its own icon in the same change**, or
  reuses a game or required-mod icon confirmed to exist; a lookup that can fail falls back to a visible icon of ours, never the
  game's red X (owner, 7 Nov 2026). A building's inspect text names the one
  thing it waits for.
- **Text a player reads in play stays in the game world** (owner, 5 November 2026): condition descriptions, tooltips, inspect text,
  job reports, refusals and messages never name a mod, a signal or a setting, and describe the body and what to do about it. Mod
  names and sources belong in READMEs, changelogs, the mod settings window, dev actions and the load line. **One exception**
  (owner, 7 Nov 2026): a one-time letter, shown once per install when a colony first loads, may recommend an optional mod by name
  where much of a mod depends on it (Vanilla Gravship Expanded for D'thorga Medical's equipment).
- Player text is never hard-coded in C#: what code shows lives in `Languages/English/Keyed`, and a Def's labels and descriptions stay
  in its XML in English, as the game's own do, for translators to override with `DefInjected`. Stable keys prefixed `Phobos`, complete
  messages with named placeholders (`scripts/check-mod-layout.py` refuses numbered ones, a key translated in C# with no English
  entry, and a `{NAME}` a fully named call does not supply). Translate a key by its literal name (`"PhobosX_Y".Translate(...)`) so the
  check can read it; a key chosen at run time goes unchecked. A figure in a message comes from the rule or
  Def that owns it, as a placeholder, never as fixed text; never translate defNames, saved keys or
  settings keys, and never drive gameplay from translated text.

## Documentation

- Player guides live in each mod's README and `docs/`; contributor instructions, research, designs and
  records in `docs/development/`, each with its index. Add every new document to its index, **in one line**: the record, its date,
  what it is and its status (`scripts/check-doc-index.py`). Guides lead
  with player actions and match the implemented behaviour; dated records keep their original scope.
- **The player guide (`docs/player-guide/`) is the players' front door** (owner's request, 12 Nov 2026): a chapter per mod or
  area, saying what it does today, without version history, illustrated with the mods' own art. A change that alters what a
  player builds, presses or sees updates its chapter in the same commit. Its figures are drawn from shipped textures by
  `python scripts/art/guide_figures.py` (listed in `docs/player-guide/images/figures.json`); redraw them when a texture they use
  changes. The gate checks their sources exist.
- **A design record's status is one line at its top** (owner, 17 Nov 2026, after the documentation audit found about thirty records
  opening with "nothing is built" over a build): `**Status (date):**`, saying what is built and what is not, updated in the change that
  builds it. What a version delivered goes in its changelog; the record keeps only what differs from its design.
  `scripts/check-doc-status.py` refuses an opening that says nothing is built while the record records a build, and
  `scripts/check-doc-links.py` checks every `#anchor` against the headings it names.
- Charts and diagrams are GitHub's native Mermaid, in the Markdown itself. Never use Figma (owner rule):
  not for charts, diagrams or designs, and do not suggest or mention its connector.
- Write plainly: answer first, short sentences, **bold** for must-dos, tables for comparisons, gotchas at
  the top, and restate a key detail where it matters again instead of "see above".

## Research attribution

- Name the organisation or researchers whose work supports a claim, calculation or design decision, with
  a labelled primary-source link beside the claim: ITU-R recommendations, the ACMA's instruments, the
  WSJT-X authors' papers, Ludeon's announcements, other mod authors. Distinguish authorship from a hosting
  site.
- Separate the source's findings from our inference, simplified models, fictional equipment and authored
  balance. Never imply endorsement. Mark unverified references as unverified; never invent citations;
  never cite figures a design record lists as unverified. Keep citations in text and documentation, never
  in artwork. Repair missing attribution when you meet it.

## Identifiers and branding

- `packageId` is `PhobosDthorga.<ModName>` (agent default, owner to confirm before the first public build;
  `scripts/check-mod-layout.py` enforces it). The Harmony ID is the `packageId`. Never change either once
  a mod is published.
- Prefix new defNames, C# namespaces, translation keys and saved (Scribe) keys with `Phobos`, and keep
  them stable once saves can hold them. Renaming is presentation only: labels change, identities don't.
- **An item Def's `defName` never ends in a digit** (the game reads a thing's ID from the digits at the end of name + number, so its
  IDs grow at every load and overflow; `scripts/check-mod-layout.py` enforces it): `PhobosBombMk82Case`, not `PhobosBombCaseMk82`.
- **Names players see are plain and unassuming**, so people can look them up and remember them (owner, 9 Nov 2026: "It's all
  about brand awareness"): `D'thorga <Plain Word>`, as in D'thorga Framework and D'thorga Medical, however lovely a fancier name
  is. The word after D'thorga says what the thing does. Check a name is not already taken in that game's mods before using it.
  The radio framework's public name is an open question in its design.
- **D'thorga is the one brand for everything new, in every game** (owner, 9 Nov 2026). Older mods named Phobos take the new display
  name when the owner next returns to that game, never in a sweep, and display names only: `packageId`, defNames, repository names
  and anything a save can hold stay as they are.
- **Every public page also carries the spelling "dthorga"** (owner, 9 Nov 2026), without the apostrophe, in the Workshop description
  and the README's opening, so a search spelled either way finds us. Searching GitHub or the web for "dthorga" found none of our work.
  `scripts/check-mod-layout.py` enforces it in every mod's About description and its README before the first section.

## RimWorld engineering

- **Shared code lives once.** A service another current or plausible Phobos mod would need goes in a
  shared library mod in the same change as its first consumer; policy, balance, art and content stay in
  the content mod. One provider per Def or service, no circular dependencies. Other authors must be able
  to use a framework without our content mods. Name meaningful constants; derive displayed figures from
  the authoritative rule; extract duplication when you meet it, not later.
- **Generic functions go to the Framework core** (owner, 8 November 2026). Any function that can reasonably be made
  generic, or that more than one Phobos mod or part uses, goes into D'thorga Framework's core (`src/PhobosFramework`,
  `mods/PhobosFramework`'s root folders) in the same change as its first consumer, even while only one consumer exists:
  game-free rules in its `Rules/` with tests in `tests/PhobosFramework.Rules.Tests`, UI helpers in `UI/`, persistence
  helpers in `Persistence/`. A mod or part keeps only what is specific to it: its Defs, its policy and its text. When in
  doubt, put it in the core.
- **D'thorga Framework is that library** (owner, 4 November 2026; `mods/PhobosFramework`, `src/PhobosFramework`,
  `docs/development/phobos-framework.md`). Every Phobos RimWorld mod with C# depends on it for the shared
  helpers (optional mods by name, the load line, the display mode, rules Defs, the debug category, the
  uninstall sweep) and goes through its `PhobosGases` for any gas; never a second gas model. Existing mods
  adopt the helpers when next touched, not in a sweep.
- **Fewer, broader mods** (owner, 7 November 2026: "an absolute metric tonne of disparate mods" had become hard to maintain;
  `docs/development/mod-consolidation.md`). A new feature joins the existing mod for its audience as a part, with its own section in
  that mod's settings and README, rather than becoming a mod: medical work goes in D'thorga Medical, a fix or patch for another
  author's mod goes in D'thorga Patches under that mod's `IfModActive` folder. A new mod needs a different audience or different hard
  dependencies, and says why in its design record. A merge keeps every namespace, class name, defName, language key and saved
  key, carries the player's settings over once (`SettingsImport`), and lists the replaced mods under `incompatibleWith`.
- **Another author's bug is theirs to fix** (owner, 8 Nov 2026). Patch an active project only when its errors are rather extreme in
  nature (a storm that kills the log, lost colony data); otherwise leave it to the author and, where possible, draft a report for the
  owner to post (agents never post). **If the project is abandoned, this reverses**: patch it. Judge that with
  `python scripts/workshop-research.py report <mod>` (update date, the uploader's last reply) and say what it showed.
- **Every patch for another author's fault has a row in `docs/development/temporary-patches.md`** (owner, 10 Nov 2026), added in the same change: whose fault it is, what would make the patch unnecessary, how
  to check, whether the author has been told, and whether it is temporary or permanent. Temporary patches say so in their code comment and, where the fault can be detected, stand down by themselves.
- **Existing effects first, own gases when they earn it** (owner, 4 November 2026). Infer an effect from
  gases and functionality that already exist (the game's, another mod's) where they are present; add a
  gas of our own only where its value is greater than inference could give, where nothing exists to infer
  from, or on the owner's call-out, and say which in the design record. Gases are additive and per room:
  no air, oxygen, pressure or vacuum model. The framework's source-mode setting is the manual override.
- **Anything that takes water or piping has its own pipes beneath it** (owner, 9 Nov 2026: "like how the vanilla game and pretty much most other mods do"). Every cell of its footprint joins the
  network, so a pipe touching any side connects and a run passes through: a building through `CompPhobosPipeUnderlay` or the piping mod's own comp; a vehicle, like a gravship, while it is parked, taking them
  with it when it moves (`VehiclePipes`). `PipeUnderlay` lays the piping mod's own hidden pipe only where none is, never touches another pipe, and is applied at the next look, so saves convert by themselves.
- **What a thing of ours is built from is a parts bill, by role** (owner, 11 Nov 2026; `docs/development/parts-bills.md`): a vehicle or
  a placed building lists its roles in a `PhobosBillDef`, keeps only its shell in its own `costList`, and each role is filled at start-up
  by an installed mod's part where it suits and ours otherwise, from the same table repairs use. Never swap a cost with patch folders per
  mix of mods. A new soft dependency is a substitute added to a role, with `MayRequire`.
- **UI presents, services decide.** Gizmos, inspect tabs, windows, designators and placement previews read
  state and delegate to one service call; they never mutate game state, roll chances or grant rewards.
- **XML first, C# where Defs cannot express it.** Hardware and tuning are Defs (new Def types such as
  `BandDef` and `ModeDef`; `DefModExtension`s and comps on `ThingDef`s), so other modders extend in XML.
  Prefer comps, `DefModExtension`s, `GameComponent`s and RimWorld's own extension points over Harmony.
- **Harmony, sparingly.** No Harmony reference until a feature needs a patch (Phase 1 needs none). Then
  patch only what has no extension point; prefer postfixes; a prefix that skips the original needs a
  recorded reason. Name overloads by parameter types. Each patch has a comment saying what it changes
  and why. Harmony 3 on RimWorld 1.7 rejects patches whose injected types don't match: use exact types
  for `__instance` and `__result`. **Find our patches with `typeof(OurType).Assembly`, never `Assembly.GetExecutingAssembly()`**
  (17 Nov 2026: in a constructor another mod has patched, Harmony 3.0.3.0 answers with its own assembly;
  `tests/test_no_executing_assembly.py` refuses it).
- **Optional tie-ins only** (owner rule for every Phobos mod): a feature that needs another mod switches on
  only when that mod is active, through `LoadFolders.xml` `IfModActive` folders, `MayRequire` and
  `PatchOperationFindMod` in XML, and `ModsConfig.IsActive` in C#. Never hard-reference an optional mod's
  assembly from a core assembly; put such code in a separate assembly loaded only from its `IfModActive`
  folder. A player running the mod alone sees no errors. **The one hard dependency is Vanilla Expanded
  Framework** (owner, 19 October 2026; extended to the planned medical equipment mod, 4 November 2026): the
  radio framework's feedline is a VEF pipe network, declared in `modDependencies`, and `PipeSystem.dll` is
  referenced from the installed mod with `Private=false`.
  VEF is CC BY-NC-ND 4.0 with a rider that permits depending on it: never copy its DLLs, source, art or
  XML into this repository or a build, and never adapt its code (`THIRD_PARTY_NOTICES.md`).
- **Other Vanilla Expanded mods may become hard dependencies where one earns its place** (owner, 7 Nov 2026: they are
  stable, long-lived and of high quality), not by default, and never as a habit of adding mods. Say when one brings a DLC with it:
  Vanilla Gravship Expanded needs Odyssey, so D'thorga Medical keeps it optional and strongly recommends it.
- **Worn apparel is never spawned.** A hand-held worn by a pawn has `Spawned == false`; find radios by
  enumerating `map.mapPawns` and `pawn.apparel.WornApparel` at recompute time, never through spawn hooks.
- **Never spawn a thing from `PostSpawnSetup`** (the sling's landing fault, 12 Nov 2026). The game runs a pawn's comps before it gives the
  pawn its movement parts, and a pawn spawned beside one in that state throws, leaving both half on the map and the map undrawn. Note what
  happened and act at the next tick (`CompPhobosSling`); `tests/test_no_spawn_in_spawn_setup.py` refuses the pattern.
- **Never put a function of ours into another mod's shared hook** (the Mongoose crash, 17 Nov 2026), such as Vehicle Framework's
  `VehicleTurret.ProjectileAngleCE`. Other mods swap their own in for one shot and put back what they found; with ours left there, each
  took the other for the original and they called each other until the game closed. Patch the function the hook ends in instead
  (`TurretAimSwayCombatExtended`); `tests/test_no_shared_hook_swaps.py` refuses the pattern.
- **Derived state is never saved.** `RadioNet` and every link cache hold only what can be recomputed from
  Defs and spawned things; they save nothing, touch no VEF state during load, and recompute lazily on
  first use. Thought workers and gizmos read caches; they never compute.
- **Interfaces follow the game's own patterns** (owner, 9 Nov 2026: people spot anything out of the ordinary at once). Windows, menus and quantity controls use the game's own widgets and conventions, such as the trade window's number box and arrows for an amount, and values are not rounded where the game would not round them.
- **Vanilla precedence.** Use the game's own systems instead of parallel ones: power through power comps,
  heat into rooms through the game's temperature system, weather, research, letters, quests, factions and
  goodwill. Hook or extend them; never replace them wholesale.
- **Cheap to run** (design principle 8). Recalculate when something changes, not every tick; cache and
  invalidate on events (hardware built or destroyed, weather, time-of-day band); use rare or long ticks.
  The owner's mod list is large, so a fraction of a millisecond per tick matters.
- **Saves.** Safe to add mid-game. Saved state lives in `GameComponent`, `WorldComponent` or
  `MapComponent` with stable Scribe keys; load older saves through idempotent conversions; never silently
  delete or relocate the player's things to enforce a new rule. Each README says whether removal is safe
  and what to do first.
- **Versions: RimWorld 1.7 only** (owner, 8 November 2026; 1.7 had led since 4 November 2026). 1.6 is a dying
  version and gets no resources: every C# project builds one target, `net10.0` against the 1.7 reference
  (Harmony 3), into `mods/<Mod>/1.7/Assemblies/`; no mod has a `1.6` folder or declares 1.6 in `supportedVersions`
  (every mod dropped it on 8 November 2026, at the owner's request); never fix a 1.6-only failure. Per-version folders through `LoadFolders.xml` (`1.7`, and `Mods/<Other>/1.7`
  for a tie-in assembly), shared XML in `Common/`; `Directory.Build.targets` turns the target into the game
  version, output folder and references (`docs/development/building.md`). Never write a polyfill type by hand.
  Prefer RimWorld's own
  wrappers (`Widgets`, `GenUI`, `ContentFinder`) over direct Unity calls; 1.7 runs Unity calls through a
  compatibility shim.
- Expose reasonable player preferences as documented mod settings; keep identities and physics rules
  stable rather than configurable. Multiplayer is out of scope until after the public beta.

## Materials, chemistry and balance

- **Honest physics and chemistry**: outputs are what the inputs can realistically give; give the real
  relationship, mass balance or source in the README or design record; label authored values and
  simplifications as gameplay choices.
- No loops that create value from nothing; building to sell must not beat the vanilla economy. Keep new
  items and research gates proportional to what they unlock.

## Builds, installation and checks

- Install to the game's `Mods` folder only through `scripts/deploy.py`, with RimWorld closed:
  `python scripts/deploy.py --build --apply --all` builds every project and installs every mod (owner, 4
  November 2026); name mods instead of `--all` for one; deploy also encodes our textures to DDS with RimSort's todds, so RimSort's
  texture step is not needed for our mods. Never edit `ModsConfig.xml` or the owner's RimSort
  lists unless the owner asks, and back them up first.
- **Answer what updating or going back would change with `python scripts/installs.py`** (owner, 18 Nov 2026: a part of D'thorga
  Companion for "updating, reversing changes, making backups", showing what each change gains or loses): each mod's installed copy,
  the repository's and every backup, compared by the changelogs each carries (phobos-modkit's installs; `docs/development/mod-installs.md`).
  The Companion's Mods page runs it. So **a changelog entry is what the owner reads before updating**: keep its bold opening when
  rewording it, and save effects under "Save compatibility". deploy writes `InstallRecord.json` into every installed copy.
  Status also says which mods need a build (`python scripts/deploy.py --build-only` builds without installing, fine while the game
  runs), the files an install would change, its settings changes (`mod-settings.py changes --json`) and the test rows it brings.
- **Answer whether textures need encoding before a launch with `python scripts/textures.py`** (owner, 18 Nov 2026: "it's always a
  wild guess"): every active mod, ours and others', stale (the game would show the old picture: it loads a `.dds` instead of the
  `.png` beside it without comparing dates), never encoded, or left over. `encode --mod` writes `.dds` files with RimSort's own
  todds preset into the named mods' folders, **Workshop ones included, by the owner's choice**, and refuses while RimWorld runs.
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
- **Detail for chasing a fault is a Debug line** (owner, 16 Nov 2026: verbose logging): `Logger.Debug(...)`, behind `Logger.DebugEnabled` where
  the line costs anything to build. A mod's logs are static fields of its Mod class, so D'thorga Framework's settings can list a tick for each.
- **Log through the shared logger** (phobos-log, public, pinned at `external/phobos-log` and compiled into D'thorga Framework; owner, 8
  Nov 2026): `Phobos.Framework.Logging.Logs.For(mod, part)` in a static field named `Logger`, never `Log`, with its gates before a line
  that can repeat. A dev action's own answer still goes through the game's `Log`. Existing mods adopt it when next touched.
- **The shared tools come from the pinned phobos-modkit submodule** (`external/phobos-modkit`, owner's request of
  7 Nov 2026 for a shared repository): **after cloning, or in a new worktree, run `git submodule update --init`**
  before the scripts or tests. A fix to a shared tool goes to phobos-modkit first, under its own AGENTS.md; then
  move the pin here with `git -C external/phobos-modkit checkout <tag>` and a commit. Never edit the submodule in
  place.
