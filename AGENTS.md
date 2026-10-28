# Phobos RimWorld contributor instructions

These are the standing rules for work in this repository: owner directions still in force, stated once
and in their current form. What each version delivered lives in the owning mod's
`mods/<Mod>/CHANGELOG.md`; designs, research and records live in `docs/development/` (indexed in
`docs/development/README.md`). The project started on 18 October 2026 from the owner's radio framework
design (`docs/development/radio-framework-design.md`); its conventions follow Phobos Ostranauts
(`phobos-dthorga/phobos-ostranauts`), adapted to RimWorld. Keep this file to rules: when a direction is
delivered, record the delivery in the changelog or design record, and add or amend only the rule here.

## Working with the owner

- Practical, small-team project: prefer a working slice over speculative frameworks or process. Aim for
  maximum creative freedom (owner, carried over from Phobos Ostranauts): risks such as a mod becoming
  unsafe to remove from a save are engineering problems to report honestly and explore, never a reason
  to drop, defer or shrink a design.
- Candidly recommend simplifying, setting aside or changing direction when gameplay value, engine limits
  or maintenance cost make further work unconvincing. Never continue merely because effort was spent;
  never invent hour estimates.
- Explain what works, what was checked and what remains uncertain. Distinguish observed game behaviour
  from proposed designs and untested assumptions. A build, an offline check or a unit test is never an
  in-game test. **Nothing counts as tested in game until the owner says so**; write "not yet tested in
  game" in READMEs, changelogs and replies, and never claim untested compatibility.
- The owner runs gameplay tests. Do not control the owner's mouse or keyboard; give instructions for
  interactive steps. Never edit, replace or delete save files without explicit authorisation (read-only
  copies for diagnosis are fine; delete them afterwards). Read the owner's `Player.log` before assuming a
  reported fault is the owner's error.
- The owner plays RimWorld 1.6 on Steam's main branch with all five DLCs and a large mod list (about 280
  mods, managed with RimSort, including Combat Extended). **Keep `docs/development/owner-mod-list.md` up
  to date** (owner, 22 Oct 2026): run `python scripts/refresh-mod-list.py` (it only reads the game's files)
  before checking advice or compatibility claims against the list, and whenever the owner mentions changing
  mods; add `--online` now and then to re-read the Vanilla Expanded collection. Its suggestions are written by
  hand: when a refresh shows one taken up or stale, update them from the mods' Workshop pages and label your
  own reasoning. **Read a mod's latest player comments before suggesting it** (owner, 22 Oct 2026: a Workshop
  description and a recent update date don't show unfixed bugs), and say which picks are comment-checked. `docs/development/project-direction.md` names the mods that matter to these designs. The PC
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

## Phases and the design

- **Phase 1 of the radio framework is open** (owner, 19 October 2026): local VHF/UHF on one map, as the
  design's roadmap lists it. Each later phase waits for its gate and an explicit owner go-ahead; the
  design's "refined during play" still applies to everything beyond the open phase. Research,
  verification and design records are welcome at any time.
- The living design doc on claude.ai is the master copy; `docs/development/radio-framework-design.md` is
  a dated snapshot of it. Refresh the snapshot when the owner asks, keeping its text as written and its
  diagrams as Mermaid. Notes from play go in the living doc's "Notes from play" table. Decisions that
  answer the design's open questions are recorded in `docs/development/project-direction.md`.
- Reference figures the design marks as unverified stay unverified until a record shows a source
  (`docs/development/radio-reference-checks.md`); a Def may carry a figure with a secondary source if
  its description says "approximate", and never one with no source at all.

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
  inside a mod), clearly presented as community-created, not official and not endorsed by Ludeon, and
  never bundled with the game or its expansions (`docs/development/rimworld-modding-reference.md`).
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
- A mod's version lives in `About/About.xml` (`modVersion`), its `CHANGELOG.md` and its README; keep them
  in step in the same commit. When version copies multiply, adopt Ostranauts' constants updater rather than
  hand-editing copies.
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
  its reason); never ship a silent success or a silent refusal. A building's inspect text names the one
  thing it waits for.
- Player text lives in `Languages/English/Keyed` and `DefInjected`, never hard-coded in C#. Stable keys
  prefixed `Phobos`, complete messages with named placeholders; never translate defNames, saved keys or
  settings keys, and never drive gameplay from translated text.

## Documentation

- Player guides live in each mod's README and `docs/`; contributor instructions, research, designs and
  records in `docs/development/`, each with its index. Add every new document to its index. Guides lead
  with player actions and match the implemented behaviour; dated records keep their original scope.
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
- Mod display names follow the house pattern `Phobos <Name>` unless the owner chooses otherwise; the radio
  framework's public name is an open question in its design.

## RimWorld engineering

- **Shared code lives once.** A service another current or plausible Phobos mod would need goes in a
  shared library mod in the same change as its first consumer; policy, balance, art and content stay in
  the content mod. One provider per Def or service, no circular dependencies. Other authors must be able
  to use a framework without our content mods. Name meaningful constants; derive displayed figures from
  the authoritative rule; extract duplication when you meet it, not later.
- **UI presents, services decide.** Gizmos, inspect tabs, windows, designators and placement previews read
  state and delegate to one service call; they never mutate game state, roll chances or grant rewards.
- **XML first, C# where Defs cannot express it.** Hardware and tuning are Defs (new Def types such as
  `BandDef` and `ModeDef`; `DefModExtension`s and comps on `ThingDef`s), so other modders extend in XML.
  Prefer comps, `DefModExtension`s, `GameComponent`s and RimWorld's own extension points over Harmony.
- **Harmony, sparingly.** No Harmony reference until a feature needs a patch (Phase 1 needs none). Then
  patch only what has no extension point; prefer postfixes; a prefix that skips the original needs a
  recorded reason. Name overloads by parameter types. Each patch has a comment saying what it changes
  and why. Harmony 3 on RimWorld 1.7 rejects patches whose injected types don't match: use exact types
  for `__instance` and `__result`.
- **Optional tie-ins only** (owner rule for every Phobos mod): a feature that needs another mod switches on
  only when that mod is active, through `LoadFolders.xml` `IfModActive` folders, `MayRequire` and
  `PatchOperationFindMod` in XML, and `ModsConfig.IsActive` in C#. Never hard-reference an optional mod's
  assembly from a core assembly; put such code in a separate assembly loaded only from its `IfModActive`
  folder. A player running the mod alone sees no errors. **The one hard dependency is Vanilla Expanded
  Framework** (owner, 19 October 2026): the radio framework's feedline is a VEF pipe network, declared in
  `modDependencies`, and `PipeSystem.dll` is referenced from the installed mod with `Private=false`.
  VEF is CC BY-NC-ND 4.0 with a rider that permits depending on it: never copy its DLLs, source, art or
  XML into this repository or a build, and never adapt its code (`THIRD_PARTY_NOTICES.md`).
- **Worn apparel is never spawned.** A hand-held worn by a pawn has `Spawned == false`; find radios by
  enumerating `map.mapPawns` and `pawn.apparel.WornApparel` at recompute time, never through spawn hooks.
- **Derived state is never saved.** `RadioNet` and every link cache hold only what can be recomputed from
  Defs and spawned things; they save nothing, touch no VEF state during load, and recompute lazily on
  first use. Thought workers and gizmos read caches; they never compute.
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
- **Versions.** Build for RimWorld 1.6 first and keep 1.7 in view: per-version folders through
  `LoadFolders.xml`, shared XML in `Common/`, one assembly build per version (1.6: .NET Framework 4.7.2
  and Harmony 2, with PolySharp for newer C#; 1.7: .NET 10 and Harmony 3; `docs/development/building.md`).
  Never write a polyfill type by hand or make PolySharp's types public: they stay internal so mods can't
  clash. Prefer RimWorld's own
  wrappers (`Widgets`, `GenUI`, `ContentFinder`) over direct Unity calls; 1.7 runs Unity calls through a
  compatibility shim.
- Expose reasonable player preferences as documented mod settings; keep identities and physics rules
  stable rather than configurable. Multiplayer is out of scope until after the public beta.

## Radio framework rules

- **One link-budget service decides every contact.** Plain, Operator and Engineer modes change only what
  the UI shows, never the outcome.
- **Decibels everywhere.** Gains and losses add in dB; distance follows the inverse-square law (doubling
  it costs 6 dB). Every figure in a Def names its source or says it is authored. The rules Def
  (`RadioRulesDef`) holds the starting values the design says to tune in play; player preferences
  (display mode, `metresPerCell`, the thought) are mod settings, never physics.
- **A link is two-way.** The link budget evaluates both directions and the worse one decides; the sheet
  names the limiting direction. Antenna height matters through ray clearance over walls
  (`docs/development/project-direction.md`); a figure the UI shows must affect an outcome somewhere.
- **Realism is a feel, not a hidden calculation.** Use the simplest model that produces the right
  behaviour, and make every rule visible to a player who wants to see it.
- **Physics sets the kit, the kit sets the play**: wavelength drives antenna size, which drives
  portability, power draw and siting. Don't hand out kit that physics says is impossible.

## Materials, chemistry and balance

- **Honest physics and chemistry**: outputs are what the inputs can realistically give; give the real
  relationship, mass balance or source in the README or design record; label authored values and
  simplifications as gameplay choices.
- No loops that create value from nothing; building to sell must not beat the vanilla economy. Keep new
  items and research gates proportional to what they unlock.

## Artwork and sound

- Match RimWorld's look. Record how each texture or sound was made (tool, prompt, date, source hashes) in
  an asset record beside it; disclose AI assistance; never ship the game's or another mod's art. Check
  generator allowance and cost first; never silently buy credit or switch to a costlier generator.
- Sounds only where usefulness is high: quiet, never alarming, with a volume setting.

## Builds, installation and checks

- There is no C# project yet. The first one follows `docs/development/building.md`, and its build and
  local deploy scripts arrive in the same change. Install to the game's `Mods` folder only through that
  script, with RimWorld closed. Never edit `ModsConfig.xml` or the owner's RimSort lists unless the owner
  asks, and back them up first.
- Scale checks to the change; test rules and persistence, not static documentation. Before committing,
  run `python scripts/check-doc-links.py`, `python scripts/check-mod-layout.py` and
  `python -m unittest discover -s tests`. Gate commits on the exit codes, not on grep output.
