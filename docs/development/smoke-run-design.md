# Smoke run on a lean profile (design, 18 Nov 2026)

**Status (18 Nov 2026):** design only; nothing is built. Proposed by the agent on 18 Nov 2026 when asked which tools the kit lacks;
the owner asked for it to be documented the same day, without a go-ahead to build it yet.

**What it is, in one line:** a script starts RimWorld on its own small profile with only our mods and what they need, lets a
dev routine of D'thorga Framework spawn and tick every thing of ours on the game's own quick-test map, then quits and reports every
error and warning our things caused. It answers "does it load, spawn and tick without errors?" before the owner meets a change.

## Read this first (gotchas)

- **A smoke run is never an in-game test.** It proves the code ran without an error on a small mod list, never that a feature works
  or looks right. Nothing it sees goes into the seen-in-play notes or the test list (`config/test-evidence.json`), and nothing it
  reports may be written up as "tested in game". The owner's word stays the only pass.
- **It cannot catch what needs the owner's other ~340 mods**: Combat Extended's damage rewrite, Harmony collisions with other
  authors, load-order faults. Our optional tie-ins can be smoke-run only by adding their mod to the profile (a "with" list).
- **It launches the game, so it needs the owner's go-ahead each time** while that rule stands, and never runs while their game is
  open: both would share the Mods folder, the GPU, and the log file.
- **RimWorld keeps one `Player.log`.** A smoke run's launch turns the owner's last log into `Player-prev.log` and pushes out the one
  before. The script must copy both aside first and put them back afterwards, keeping the smoke run's own log beside its report.
- Assumed, not checked: that `RimWorldWin64.exe` (or `RimWorld.exe`) starts directly without Steam relaunching it, and that
  the quick-test world generates in reasonable time with our mods alone.

## Why (what it changes for the owner)

| | |
| --- | --- |
| **Lets** | Agents see a load error, a red error on spawn or a fault in the first few thousand ticks before an install, the way the sling's landing fault (12 Nov) and the Mongoose crash (17 Nov) would have shown. The owner's first look at a change can start from something that at least starts. |
| **Stops** | Installs that break the map or the load on first contact; test asks that die on an exception rather than on the thing being tested. |
| **Changes** | Before an install, the agent runs (with the owner's go) one smoke run and reads its report. A fault found there is fixed before the owner sees the build. |

## What the game already offers (read from RimWorld 1.7's code, 18 Nov 2026)

| Piece | Where | What it does |
| --- | --- | --- |
| `-savedatafolder=PATH` | `Verse.GenFilePaths.SaveDataFolderPath` (full:all.cs:210907) | Everything the game keeps in the user folder (Config with ModsConfig.xml and Prefs.xml, Saves, mod settings) comes from PATH instead. The game logs "Save data folder overridden to …", which proves the run used it. The value is split on `=`, so **a path holding `=` breaks it**. |
| `-quicktest` | `Verse.QuickStarter.CheckQuickStart` (full:all.cs:53158) | From the entry scene, loads the play scene directly. With no game set up, `Verse.Root_Play.Start` then calls `SetupForQuickTestPlay` (RimWorld-1.7:Verse.Root_Play.cs:85-98): Crashlanded, Cassandra on Rough, a new world at 30% coverage, a random starting tile, a 250-by-250 map. |
| The autostart save | `Verse.SaveGameFilesUtility.GetAutostartSaveFile` (full:all.cs:145547-145554) | With dev mode on, a save named `autostart` in the profile's Saves loads by itself instead of the quick test. Useful later for a fixed test colony (a "fixture save" built by us, never one of the owner's). |
| Switches | `Verse.GenCommandLine.CommandLineArgPassed` (full:all.cs:209705) | Any `-name` on the command line can be read, so our routine can switch on with its own (`-phobossmoke`). |

## The parts

**1. The profile** (`python scripts/smoke-run.py`, agent's design): a folder under `.local/smoke-profile/`, rebuilt each run, outside
the owner's user folder. It holds `Config/ModsConfig.xml` naming Harmony, the game's DLCs the owner has, Vanilla Expanded Framework,
our mods and their hard dependencies (read from our About.xml files, in load order), plus any `--with <mod>` for a tie-in;
`Config/Prefs.xml` with dev mode on and the log's message cap left alone; and nothing else, so every mod setting is at its default.
**Agent choice:** the defaults, not the owner's settings, because a smoke run asks whether the mod works out of the box. A
`--owner-settings` option could copy the owner's settings for our mods from the Config folder (read-only).

**2. The launch:** the script refuses while RimWorld runs, copies `Player.log` and `Player-prev.log` aside, starts the game with
`-savedatafolder=<profile> -quicktest -phobossmoke`, waits with a time limit (agent default: 15 minutes, as world generation is
the slow part), kills it if the limit passes, then puts the owner's logs back and keeps the run's log in the report folder.

**3. The routine** (D'thorga Framework, a `GameComponent` that does nothing unless `-phobossmoke` was passed): once the map is
ready, it spawns one of every ThingDef our mods define (buildings, items, plants; vehicles through Vehicle Framework's own spawner;
apparel worn by a colonist; pawns and mechs by their kinds), each in its own clear spot, then lets the game tick at full speed for
a set number of ticks (agent default: 30,000, half an in-game day), records how long the ticks took, saves the game once and loads
it again to exercise our saved state, ticks again, and quits with the game's own shutdown. It writes a JSON report: what it
spawned, what it could not and why, and the time ticking took. It never presses a gizmo or gives an order in this first slice.

**4. The report:** the script runs `triage-log` on the run's log and prints, beside the routine's report: every error and warning,
grouped as the triage groups them, with the mod named; what failed to spawn; whether the save and reload completed; and the
ticking time against the last run. Exit 1 when an error names one of our mods. The generic parts (launch with a log set aside, wait,
collect) belong in phobos-modkit as a RimWorld adapter command; the list of our things and the routine stay here.

## Order of work, when the owner gives the go-ahead

1. The launch and the profile alone (no routine): a quick-test game with our mods starts, the script waits, quits it and reads
   the log. This proves the switches, the Steam question and the time world generation takes.
2. The routine: spawn, tick, save and load, report.
3. Optional tie-ins, one `--with` at a time, starting with Vehicle Framework, whose vehicles are where most faults have been.
4. Later, if worth it: a fixture save for the autostart path, so a run can start from a colony with a garage, a hospital and
   vehicles already in place.

## Open questions for the owner

- Should a smoke run before each install become a rule, or stay something agents offer?
- Is launching the game for a smoke run fine while they are away from the PC, given the GPU rule for art (no GPU work while they
  play)? A smoke run uses the GPU only while it runs.
