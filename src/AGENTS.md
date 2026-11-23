# C# and how a mod works

The engineering rules for every mod's code and design: shared code, Harmony, tie-ins, saves, performance, text in code, logging
and seen-in-play notes. A session reads it before writing C# or designing how a mod behaves; the vehicles and radio keep their own
rules beside their folders as well. **How the code is built** (layers, ticks, tie-ins, saves and removal):
[`docs/development/architecture.md`](../docs/development/architecture.md).

Split from the root `AGENTS.md` on 21 Nov 2026 (the owner's yes: "keep a slimmed-down AGENTS at the root"), every rule moved word
for word; the root's rules apply here too.

## Working with the owner

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

## Player language and localisation

- Every gizmo, command and dialog says what it did, and every refusal says why (a disabled gizmo carries
  its reason); never ship a silent success or a silent refusal. **Every new button gets its own icon in the same change**, or
  reuses a game or required-mod icon confirmed to exist; a lookup that can fail falls back to a visible icon of ours, never the
  game's red X (owner, 7 Nov 2026). A building's inspect text names the one
  thing it waits for.
- Player text is never hard-coded in C#: what code shows lives in `Languages/English/Keyed`, and a Def's labels and descriptions stay
  in its XML in English, as the game's own do, for translators to override with `DefInjected`. Stable keys prefixed `Phobos`, complete
  messages with named placeholders (`scripts/check-mod-layout.py` refuses numbered ones, a key translated in C# with no English
  entry, and a `{NAME}` a fully named call does not supply). Translate a key by its literal name (`"PhobosX_Y".Translate(...)`) so the
  check can read it; a key chosen at run time goes unchecked. A figure in a message comes from the rule or
  Def that owns it, as a placeholder, never as fixed text; never translate defNames, saved keys or
  settings keys, and never drive gameplay from translated text.

## Identifiers and branding

- Prefix new defNames, C# namespaces, translation keys and saved (Scribe) keys with `Phobos`, and keep
  them stable once saves can hold them. Renaming is presentation only: labels change, identities don't.

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

## Builds, installation and checks

- **Detail for chasing a fault is a Debug line** (owner, 16 Nov 2026: verbose logging): `Logger.Debug(...)`, behind `Logger.DebugEnabled` where
  the line costs anything to build. A mod's logs are static fields of its Mod class, so D'thorga Framework's settings can list a tick for each.
- **Log through the shared logger** (phobos-log, public, pinned at `external/phobos-log` and compiled into D'thorga Framework; owner, 8
  Nov 2026): `Phobos.Framework.Logging.Logs.For(mod, part)` in a static field named `Logger`, never `Log`, with its gates before a line
  that can repeat. A dev action's own answer still goes through the game's `Log`. Existing mods adopt it when next touched.
