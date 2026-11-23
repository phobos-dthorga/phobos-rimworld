# Decisions: Framework services

**Status (23 Nov 2026):** living; harvested from the records' "Who chose what", owner's decisions and answered questions.
One row a decision. How it works now: [the system page](../systems/framework-services.md). Dates are 2026. **Who**:
owner; owner's card (a decision card answered); agent default or agent choice (the owner may revise either). A later row that
overturns an earlier one says so.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 4 Nov | One shared framework for code every mod repeats | owner | Docs, code and art unified now | [pf], The owner's rules (4 Nov 2026) |
| 4 Nov | Existing mods adopt its helpers when next touched | owner | No sweep of tested mods | [pf], The owner's rules (4 Nov 2026) |
| 4 Nov | Helpers: optional mods, load line, display mode, rules Defs, debug category, uninstall sweep | owner | Repeated in every mod | [pf], Decisions |
| 7 Nov | Work queue not built yet (overturned 7 Nov) | agent choice | Waited for the owner's proposals | [pf], Later services (moved out 17 Nov 2026) |
| 7 Nov | Work queue built, Framework 0.7.0 (overturns 7 Nov) | agent choice | From Patches' missile fix | [pf], Later services (moved out 17 Nov 2026) |
| 7 Nov | No base class for interval map components yet | agent choice | Would change tested timing code | [pf], Later services (moved out 17 Nov 2026) |
| 8 Nov | A shared logging framework, generic across mods and games | owner | Logs kept hitting the game's limit | [lg], Who chose what |
| 8 Nov | Keep the log alive by a bounded revive, no Harmony | owner | Another mod's storm kills the log | [lg], Read this first |
| 8 Nov | Revive on by default, off switch; README says so | owner | It changes Ludeon's anti-spam cap | [lg], Read this first |
| 8 Nov | No Rust inside the game; Rust stays in the reader | owner | Costs and no gain named | [lg], 5. Rust inside the game: no (owner agreed) |
| 8 Nov | Shared C# logger in a new sibling repository | owner | Pinned like Scope and modkit | [lg], Read this first |
| 8 Nov | First round a record only, no build | owner | Research before building | [lg], Read this first |
| 8 Nov | Leaving the game alone, triage only: rejected | owner | The log still dies at 10,000 | [lg], Recorded alternatives (not chosen) |
| 8 Nov | Revive checks every 250 ticks, calls the game's own reset | agent choice, owner | One bool read; no Harmony | [lg], Who chose what |
| 8 Nov | A storm counter names the loudest message; no mirror file | agent choice, owner | Names the culprit cheaply | [lg], Who chose what |
| 8 Nov | Our messages through a category of our own, not Verse.Log | agent choice, owner | Survives the cap in Player.log | [lg], Who chose what |
| 8 Nov | The logger's once and throttle gates, budgets, templates | agent choice, owner | So we are never the storm | [lg], Who chose what |
| 8 Nov | Debug on in dev mode (overturned 16 Nov) | agent choice | Reports | [lg], 2. Our own messages: a real category, and budgets (the shared logger) |
| 8 Nov | Writers per game host; schema and reader shared | agent choice, owner | C# and Lua cannot share a writer | [lg], Who chose what |
| 8 Nov | phobos-log layers: game-free rules, host sinks, schema; MIT | agent choice, owner | Rules tested apart from hosts | [lg], Who chose what |
| 8 Nov | Repository named phobos-log | agent default | Placeholder for the owner | [lg], Who chose what |
| 8 Nov | Revive bound: 25 a session | agent default | Authored; a disk guard | [lg], Who chose what |
| 8 Nov | Budgets: 200 Warn and 50 Error a category per ten minutes | agent default | Authored | [lg], Who chose what |
| 8 Nov | Storm counter 512 keys; a ring of 200 events | agent default | Authored | [lg], Who chose what |
| 8 Nov | A future log file capped at 8 MB a session, keep 5 | agent default | Authored | [lg], Who chose what |
| 8 Nov | Suppressed call under 50 ns; no logging per tick | agent choice, owner | Authored | [lg], Performance budget |
| 8 Nov | No dedup prefix on the game's Log.Error | agent choice | Most-patched logging method | [lg], Recorded alternatives (not chosen) |
| 8 Nov | No mirror of other mods' messages into our file | agent choice | Duplicates Player.log | [lg], Recorded alternatives (not chosen) |
| 8 Nov | Logger neither in Phobos Scope nor in modkit | agent choice | Other shapes; modkit has no runtimes | [lg], Recorded alternatives (not chosen) |
| 8 Nov | Framework and Patches adopt the logger; others when touched | agent choice | Four Patches prefixes one category | [lg], Roadmap |
| 10 Nov | One sounds volume setting, in D'thorga Framework | owner | The quiet rule | [sg], Read this first (gotchas) |
| 10 Nov | Scales each SoundDef from its authored volume | agent choice | One setting | [sg], Into the game (built 10 Nov 2026; not yet tested in game) |
| 10 Nov | Volume default 100%, 0 to 200% (150% before 0.108.1) | agent choice | Authored | [sg], Into the game (built 10 Nov 2026; not yet tested in game) |
| 10 Nov | Switch click on toggles; off at 0.9 pitch | agent choice | Like a switch | [sg], Into the game (built 10 Nov 2026; not yet tested in game) |
| 10 Nov | A colony mood file at each save, for the mood report | owner | A save holds half of a mood | [ms], Who chose what |
| 10 Nov | Mood report suggestions labelled as advice | owner | Advice, not fact | [ms], Who chose what |
| 10 Nov | Colonists, slaves and prisoners all in the file | owner | The report's three groups | [ms], Who chose what |
| 10 Nov | Mood file setting on by default | agent default | A moment at each save only | [ms], Who chose what |
| 10 Nov | One file, replaced at each save | agent choice | Not one per save | [ms], Who chose what |
| 10 Nov | File matched to a save by tick count and seed | agent choice | Exact for the newest save | [ms], Who chose what |
| 10 Nov | Folder named PhobosSnapshots | agent choice | Beside Config and Saves | [ms], Who chose what |
| 10 Nov | Written from a GameComponent as the game saves; no Harmony | agent choice | A failure never stops the save | [ms], How it works |
| 16 Nov | Verbose logs: master switch, tick per part, both off | owner | No dev mode needed | [lg], Verbose logging, built (Framework 0.114.0, 16 Nov 2026) |
| 16 Nov | Debug no longer on in dev mode (overturns 8 Nov) | agent choice | All at once | [lg], Verbose logging, built (Framework 0.114.0, 16 Nov 2026) |
| 16 Nov | Debug lines share the budget: 400 a mod per ten minutes | agent choice | Authored | [lg], Verbose logging, built (Framework 0.114.0, 16 Nov 2026) |
| 17 Nov | Seen-in-play notes written as the owner plays | owner | Fits their spontaneity | [si], Status |
| 18 Nov | Seen notes sharpened: design written, then built | owner | Hollow and stale sightings | [si], Status |
| 18 Nov | Once a session per key, case and thing | agent choice | Each vehicle | [si], N1. Notes say what they were seen on, which case, and their figures |
| 18 Nov | Thing: its own ID; kind: its def | agent default | Stable in a save | [si], N1. Notes say what they were seen on, which case, and their figures |
| 18 Nov | Test-moment message, dev mode only, off by default | agent choice | Names a test row | [si], E8. A test-moment message (dev mode, off by default) |
| 18 Nov | Test rows go in only by deploy | agent choice | Nothing ships on the Workshop | [si], E8. A test-moment message (dev mode, off by default) |
| 18 Nov | Test rows as a tab-separated file | agent choice | No JSON library needed | [si], As built (18 Nov 2026) |
| 19 Nov | A note after errors with our code | owner | Reports reach us | [lg], Saying when our code was on an error's path (Framework 0.170.0, 19 Nov 2026) |
| 19 Nov | For every mod of ours | agent choice | Not only the takeover | [lg], Saying when our code was on an error's path (Framework 0.170.0, 19 Nov 2026) |
| 19 Nov | Errors only, once each, 50 a session | agent default | Bounds | [lg], Saying when our code was on an error's path (Framework 0.170.0, 19 Nov 2026) |
| 19 Nov | "May have caused it"; no link | agent choice | EULA | [lg], Saying when our code was on an error's path (Framework 0.170.0, 19 Nov 2026) |
| 19 Nov | Its setting on by default | agent default | No change in play | [lg], Saying when our code was on an error's path (Framework 0.170.0, 19 Nov 2026) |
| 20 Nov | Settings as pages and search, like the game's Options | owner | Tabs cramped past eight | [sp], The owner's choices (20 Nov 2026) |
| 20 Nov | Changed only, reset per page, last page remembered | owner | Find what was changed | [sp], The owner's choices (20 Nov 2026) |
| 20 Nov | Search box takes typing at once | owner | Keyboard over mouse | [sp], The owner's choices (20 Nov 2026) |
| 20 Nov | Vehicle settings an entry of their own | owner | Over pages in Framework's entry | [sp], The owner's choices (20 Nov 2026) |
| 20 Nov | Harrier disassembly trace a detailed log, off | owner | Its tick wrote for every player | [sp], The owner's choices (20 Nov 2026) |
| 20 Nov | New: a vehicle message level and a restart reminder | owner | Gaps the audit found | [sp], The owner's choices (20 Nov 2026) |
| 20 Nov | Every setting keeps its saved key and value | agent choice | Nothing stopped or lost | [sp], What it lets the player do, stops, and changes |
| 20 Nov | Factory Additions keeps its own settings | agent choice | No dependency for two ticks | [sp], Entries before and after |
| 20 Nov | Radio settings unchanged | owner | On the back burner | [sp], Entries before and after |
| 20 Nov | Show gases at the mouse only in dev mode | agent choice | Does nothing outside it | [sp], The audit (agent's findings, 20 Nov 2026) |
| 20 Nov | Vehicle sounds setting drawn on the vehicles' page | agent choice | Far from the vehicles | [sp], The audit (agent's findings, 20 Nov 2026) |
| 20 Nov | Restart-needing rows marked by the page, one way | agent choice | Marked by hand, unevenly | [sp], The audit (agent's findings, 20 Nov 2026) |
| 20 Nov | Four Patches entries become one | agent choice | Named inconsistently | [sp], The audit (agent's findings, 20 Nov 2026) |
| 20 Nov | Three display modes kept apart | agent choice | Owner uses two different ones | [sp], The audit (agent's findings, 20 Nov 2026) |
| 20 Nov | Figures only kept beside the drivetrain's | agent choice | Different reach | [sp], The audit (agent's findings, 20 Nov 2026) |
| 20 Nov | Message level: All, Important only, Letters only | owner | Quiet unasked messages | [sp], The vehicle message level (built in Framework 0.186.0) |
| 20 Nov | Only messages nobody asked for are hidden | agent choice | Refusals always say why | [sp], The vehicle message level (built in Framework 0.186.0) |
| 20 Nov | One prefix on Messages.Message; free at All | agent choice | Skips only our own | [sp], The vehicle message level (built in Framework 0.186.0) |
| 21 Nov | A general loop watch: notice and tell, never step in | owner's card | Loops from any mods | [pl], Status |
| 21 Nov | A loop: same item, three turns in two in-game hours | agent choice | Authored | [pl], Read this first |
| 21 Nov | Told once, then quiet a day per colonist and thing | agent choice | Authored | [pl], Read this first |
| 21 Nov | Player's colonists only; nothing saved | agent choice | Small memory | [pl], Read this first |
| 21 Nov | Loop watch setting on by default | agent choice | A message can be unwanted | [pl], Read this first |
| 21 Nov | Postfix on StartJob; only jobs that became current | agent choice | Eight mods patch it | [pl], How it works |
| 21 Nov | One D'thorga Architect tab, in Motor pool's old place | owner's card | One place for our buildables | [at], Read this first |
| 21 Nov | Sub-tabs: Motor pool, Farming, Medical, Radio | owner's card | Sorted by purpose | [at], Read this first |
| 21 Nov | Tap, electrolyser, oxygen store stay in others' tabs | owner's card | Beside their kin | [at], Read this first |
| 21 Nov | VGE lists the sub-tabs while active | owner's card | One mod draws the Architect | [at], Read this first |
| 21 Nov | Without VGE, our own listing with game buttons | owner's card | "Build our own" | [at], Read this first |
| 21 Nov | Each sub-tab its own tab where a mod replaces Architect | agent choice | Better Architect, Mint Menus | [at], Read this first |
| 21 Nov | Motor pool's defName kept | agent choice | Existing Defs keep working | [at], How it works |
| 21 Nov | A search on the parent shows its sub-tabs' things | agent choice | Search still finds them | [at], How it works |
| 21 Nov | Sub-tab icons from pictures we already ship | agent choice | No new art | [at], How it works |
| 21 Nov | No setting; tab choice not saved | agent choice | Layout only, no risk | [at], What the behaviour answers for |
| 23 Nov | Fix the drone platforms' top-up faults | owner | Bays rarely topped up | [dr], Who chose what |
| 23 Nov | Call drones home by distance | owner | Drones died of flat cells | [dr], Who chose what |
| 23 Nov | Platforms take back their drones' wrecks | owner | Wrecks littered the map | [dr], Who chose what |
| 23 Nov | Wrecks vanishing into steel on death: left aside | owner | Not chosen | [dr], Who chose what |
| 23 Nov | Which drones named in a Def, never in code | agent choice | As the top-up does | [dr], Read this first |
| 23 Nov | Fortified's own two-hour call stays; not patched | agent choice | Ours only calls earlier | [dr], Read this first |
| 23 Nov | Top-up: buildings not held; loads from several stacks | agent choice | A drone walking home blocked it | [dr], How it works |
| 23 Nov | Both recall settings on | agent default | Low risk | [dr], Who chose what |
| 23 Nov | Walk padded by half, 600 ticks spare | agent default | Authored | [dr], Who chose what |
| 23 Nov | Straight line x4; path price kept 1,000 ticks; look each 250 | agent default | Authored; cheap | [dr], Who chose what |
| 23 Nov | Drafted drones left alone | agent default | Left to the player | [dr], Who chose what |
| 23 Nov | Top-up at 139 and wrecks at 95 in Hauling | agent default | Before most hauling | [dr], Who chose what |
| 23 Nov | Salvage: the whole butchering yield, no work | agent default | A shredder's steel, no bench | [dr], Who chose what |
| 23 Nov | A full home passed over for the nearest with room | agent default | Wrecks still go home | [dr], Who chose what |
| 23 Nov | No letter or alert for a drone called home | agent default | Not news | [dr], Who chose what |
| 23 Nov | Padding and spare constants, not settings | agent choice | A setting nobody can judge | [dr], Limits and open questions |

[pf]: ../archive/phobos-framework.md
[lg]: ../archive/logging-framework-design.md
[sg]: ../sound-generation.md
[ms]: ../archive/mood-snapshot.md
[si]: ../seen-in-play-checks.md
[sp]: ../archive/settings-pages.md
[pl]: ../archive/pick-up-loops.md
[at]: ../archive/architect-tabs.md
[dr]: ../archive/drone-recall-design.md
