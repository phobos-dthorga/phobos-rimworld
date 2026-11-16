# Documentation audit (17 Nov 2026)

**A read of the documents, not of the game.** The owner asked on 17 Nov 2026 for an audit of the documentation, "particularly in
areas that direly need a cleanup and what can be merged into elsewhere". Five read-only agent passes covered every Markdown file in
the repository (about 37,000 lines): vehicle systems records, utility vehicles and trailers, process and per-mod records, art and
sound, and the player-facing pages. The agent spot-checked a sample of their findings against the files; every claim checked held.
Line numbers are of commit `bf8e3985`. Verdicts are agent-authored; the order of work is the owner's choice.

## Read this first

- **The worst fault is wrong status, not size.** About thirty documents, or their index entries, say "nothing built" (or "research
  only") for work the changelogs record as built. Each opens with the wrong answer and records the build further down.
- **Version facts went stale** after 8 Nov (RimWorld 1.6 dropped) and 14 Nov (Vehicle Framework 1.7 shipped), and several reversed
  decisions are still written as the rule (parts from other mods first, visible pipes, posterised paint masks, Gemini for bodies).
- **The mod READMEs have become changelogs.** D'thorga Framework's "Read this first" is about 4,300 words of "New in 0.x" notes;
  D'thorga Medical's README is the five former mods' READMEs pasted together. Every how-to is written twice, README and guide chapter,
  and the two copies drift.
- **Design records have become build logs**: about 2,000 lines of "what 0.x built" narration that the changelogs already hold.
- **The owner's choices (17 Nov 2026):** fix the wrong facts first, then rebuild the index; slim the READMEs, with version history
  only in the changelogs; add a gate check so wrong status cannot creep back; keep this record as the checklist. Merges, splits and
  archiving wait for the owner's word, batch by batch.

## Batch 1: wrong facts

### Status lines that contradict their own documents

Each opens with "nothing built" (or similar) and records a build further down. Fix: one dated status line at the top, saying what is
built and what is not.

- [ ] `vehicle-avionics.md` (:3 against "Built", :328)
- [ ] `vehicle-presentation-design.md` (:6 against :10)
- [ ] `vehicle-knocked-out-systems.md` (:7 against "As built", :247)
- [ ] `vehicle-system-crosslinks.md` (:4 against :154), `vehicle-crosslinks-round-two.md` (:6 against :225),
  `vehicle-crosslinks-round-three.md` (:3 and :304 against :271 and :291)
- [ ] `vehicle-upgrades-round-two.md` (:3 and :21 against :325)
- [ ] `vehicle-other-factions-audit.md` ("nothing was changed", :6, against :104)
- [ ] `vehicle-health-design.md` (slice 3 "Open", :315, built in 0.36.0; performance table "empty until the owner runs the
  protocol", :7, though Framework CHANGELOG records it measured within budget on 9 Nov)
- [ ] `vehicle-sling-design.md` (:3 against :7, :15 and :22; six stacked status paragraphs at :3-31)
- [ ] `vehicle-animation-research.md` (:3 against :18)
- [ ] `soft-dependency-crosslinks.md` (:3 against :194; SD1, 4, 5, 9, 21, 22, 23 and 30 built)
- [ ] `air-defence-tie-ins.md` (:3 against AD14, :172)
- [ ] `logging-framework-design.md` (:6 against :185)
- [ ] `sound-catalogue.md` (:3-5 against :268 onward)
- [ ] `art-backlog.md` (:17 "nothing here has been generated"; electrolyser :30, night 3 queue :209 all done)
- [ ] `view-angle-models.md` (:4-7 against the trial it reports; :145 "not judged yet")
- [ ] `utility-vehicles-design.md` (:4 and :187 call the four extraction vehicles unbuilt; all built 9 Nov; :214 build order)
- [ ] `vehicle-seabed-crawler.md` (:199 overlays "wait for their paid batch", :232 "building waits": both done)
- [ ] `vehicle-trailers.md` (:35 Pintle 2 x 5 "today", now 3 x 7; borrowed picture, own art since Vehicles 0.56.0)
- [ ] `vehicle-generator-set.md` (:361 still argues for 2 x 6, answered by the 3 x 8 resize at :300)
- [ ] `outstanding-work.md` (I1 :249 "not built" against V3 :62; IDs I1 to I3 used twice, :33-35 and :249-251)
- [ ] `docs/medical-mods.md` (:62-65 ventilator and needle decompression kit "not built"; both shipped in Medical 0.7.0)
- [ ] Tested-in-game lists behind the changelogs: `player-guide/vehicle-care.md` (:371-377; headlights and work lamps, fittings and
  tunes queueing, the diagnosis letter, oil pump and gearbox, research refusal, the self-test are recorded as owner-tested in
  Framework's CHANGELOG), `vehicle-headlights.md` (:3), `player-guide/working-vehicles.md` (:282; the Busbar's look, the sling's
  take-off), Vehicles README (:10 and :73 against :180), Patches README (:17 against :30)
- [ ] Open questions already answered: drivetrain (:292), turbine (:291), systems design (:388-398), survey truck (shore load),
  `sound-generation.md` question 2; health design :136 says the hand-brake question "is now live", answered "Leave both" on 16 Nov
  (outstanding-work V9a)

### RimWorld 1.6 and Vehicle Framework 1.7

Dated records keep their wording; add a one-line note where a reader would act on it. Living documents are corrected.

- [ ] `building.md`: "fourteen C# projects" (:3; `src/` holds 22, five missing from the table), Vehicle Framework "ships only 1.6"
  (:4-6, :174-175), the required checks (:66-79 never name `check-all.py`), the scripts table (:103-114, four of about fourteen tools)
- [ ] `rimworld-modding-reference.md` (:5-9 build for 1.6; :87 links a heading that no longer exists)
- [ ] `project-direction.md` (:62 the same dead link; :7-9 the radio "being built" against the back-burner row :110; superseded rows
  :61, :62 and :78 unmarked; rows out of date order; two empty "By" cells)
- [ ] `player-language.md` (:23 "1.7 leads, 1.6 kept")
- [ ] `SUPPORT.md` (:15 "the others declare 1.6 and 1.7": every mod declares 1.7 only; :28)
- [ ] `CONTRIBUTING.md` (:4 "Ten mods"; :53 should name the player-guide chapter too)
- [ ] Mod READMEs: Framework :547, Medical :205, :337, :474, :677, :825, Factory Additions :762-804, Patches :50 and checks,
  Water Tap :123, Radio :112 and :115
- [ ] Vehicle Framework 1.7 as unreleased: `vehicle-systems-design.md` (:26-29), `vehicle-framework-mechanics.md` (:12-16, :31-34;
  add a pointer to the dependency audit), `vehicle-health-design.md` (:122, :341), `vehicle-health-consequences.md` (:4, :13),
  `utility-vehicles-design.md` (:26, :171, :434-446), `submarine-framework.md` (:14), `vehicle-generator-set.md` (:196)
- [ ] `in-game-test-pass.md`: the Vehicle Framework development-build section (:224-232) is obsolete; rows 4 to 6 are numbered twice
  (:80-92); the dependency audit's and the code audit's in-game checks are missing

### Reversed decisions still written as the rule

- [ ] Parts from other mods first (reversed 9 Nov): `utility-vehicles-design.md` :8-9 and :139-175; point to `parts-bills.md`
- [ ] Visible pipes (reversed to hidden 13 Nov): index entry, `network-underlays.md` audit table :161-169, Framework README :182-187
- [ ] Posterised masks and flood-fill cut-outs: `paid-image-generation-rules.md` :169 and :189, `vehicle-art-services.md` :69-70
- [ ] Gemini for vehicle bodies: `vehicle-art-services.md` :59-77 (the owner chose OpenAI)
- [ ] Region paint masks superseded by "repaired": `background-cleanup.md` :64 and :68
- [ ] "Sounds only where usefulness is high": `sound-generation.md` :16 (our vehicles get a full set)
- [ ] The lighter health model's setting (removed in 0.96.1): `vehicle-health-design.md` :16-18 and :312

### Smaller facts

- [ ] Old mod names as current: Framework README (:11, :245-250, :549), `caustic-gas.md` :3, `ventilation-and-hydrogen.md` :53,
  `art-automation-and-medical-equipment.md` :11-22, `art-backlog.md`, `docs/README.md` :19 ("seven mods became two"), :24, :27
- [ ] `mods/README.md` :13 (Vehicles "a stand-in"), curly apostrophes
- [ ] Root README: :35 lists the crash guard removed in Patches 0.19.2 as tested; :37 and the Radio README omit that the radio is
  on the back burner
- [ ] `tie-in-register.md` :10 says 63 folders; a count of `IfModActive` folders on 17 Nov finds 71: recount
- [ ] The Crawler's public name: "D'thorga Seabed Crawler" (`vehicle-seabed-crawler.md` :8) against "Fathom" (Vehicles README :121,
  `player-guide/submarines.md` :3): check the shipped label
- [ ] The M48's weight: 45 t (`vehicle-kerb-mass-check.md` :24) against 50 t elsewhere: check the source
- [ ] `player-guide/working-vehicles.md`: crude oil listed twice in the tanker table (:197, :211); the tanker's electric pump and the
  Crawler's instruments on the battery reach only `vehicle-care.md`
- [ ] `docs/vehicle-systems-modding.md` pins Framework 0.10.0 (:3) and lacks seven inputs shipped Defs use: `Input_DarkDriving`,
  `Input_Driver`, `Input_Fording`, `Input_GeneratorDrive`, `Input_LoadShedding`, `Input_TankLoad`, `Input_VehicleStat`
- [ ] Asset READMEs: `assets/phobos-radio` (:10 base station "placeholder", picked 12 Nov), `assets/phobos-sounds` (request table
  lists one of eight), `assets/phobos-medical` (:1-4 "planned" mod; three of seven requests), `assets/phobos-vehicles` (stops 9 Nov)
- [ ] `analysis-tooling.md` :10-13 "four tools are built" (all eight are; `check-targets.py` and `cleanup.py` missing)
- [ ] `owner-mod-list.md`: hand suggestions dated 22-25 Oct say "about 290 mods" (:165) against 346 (:17); "a new-colony list" (:22).
  A layout change goes through `scripts/refresh-mod-list.py` first

## Batch 2: the development index

The index has 110 entries, 39 of them over 350 characters. The upgrade-families entry (:122) is cut off and its tail is glued to the
avionics entry (:150-152); the generator set is listed twice with opposite status (:198, :240). The "D'thorga Vehicles" section holds
Framework and process records (logging, the crash record, the code audit, the research tree, the radiation line, the temporary-patches
register, the setup wizard); five Medical sub-headings hold one entry each.

- [ ] One line per entry: title, date, status (built, record only, archived)
- [ ] Sections: Start here; Status and trackers; D'thorga Framework (core); Vehicles (systems and health; power, drivetrain and
  aircraft; utility vehicles and trailers; submarines; cross-links and tie-ins; research and figures; ideas not built); D'thorga
  Medical; D'thorga Patches (one entry per other author's mod); Factory Additions and Water Tap; Radio (on the back burner); Art and
  sound; The owner's game; Archive
- [ ] "Start here" links the player guide, not `medical-mods.md`

## Batch 3: the gate check

- [ ] In phobos-modkit, under its own rules, then the pin moved here: refuse a document whose opening says nothing is built while it
  holds a "Built in" or "As built" section; refuse an index entry longer than one line; check link anchors (`check-doc-links.py`
  skips them today, which is how two dead anchors survived)
- [ ] AGENTS.md: a build is recorded in the changelog; the design record updates its status line and keeps only what differs from
  the design

## Batch 4: slimmer READMEs

Each mod README to about 100-200 lines: title and the dthorga spelling, version and built date, today's test status in one line;
**Read this first** with today's gotchas only (no "New in"); a short feature list linking to its guide chapter; a figures and settings
table; one link for modders. The player guide becomes the authority for how to play; `player-guide/README.md` :6-7 (the README wins
a disagreement) is reversed. Version history lives in the changelogs.

| README | Words | Main fault |
| --- | --- | --- |
| D'thorga Medical | about 12,900 | Five former mods' READMEs "kept as they were written" (:45); five gotchas and five checks sections |
| D'thorga Framework | about 10,900 | Gotchas :18-212 are version notes in no order; three pipe bullets contradict |
| D'thorga Factory Additions | about 10,400 | Version-headed sections; fault diagnosis :312-366 belongs in a design record |
| D'thorga Patches | about 8,100 | Sortie research :271-294; a Factory item in its checks (:494) |
| D'thorga Vehicles | about 6,400 | Version-log gotchas :12-73; per-vehicle sections repeat `working-vehicles.md` |

- [ ] Framework, then Vehicles, Medical, Patches, Factory Additions; Water Tap and Radio need light fixes only
- [ ] Modder material: vehicle API to `docs/vehicle-systems-modding.md`; other Framework API to a core API page; per-mod "For
  modders" and diagnoses to their design records
- [ ] One mod table and one dependency diagram (the guide README's); the root README, `docs/README.md` and `mods/README.md` link to
  it (`docs/README.md`'s versions are registered in `config/maintained-constants.json`: change the catalogue with it)
- [ ] `docs/installing-mods.md` into `player-guide/getting-started.md`; `docs/medical-mods.md` into `player-guide/medical.md`
  (nine inbound links to repoint)

## Later, on the owner's word: merges, splits and archive

Each merge copies every record in as its own dated section, unchanged, under one current status line, so no record loses its
original scope.

| Survives | Takes in |
| --- | --- |
| `vehicle-crosslinks.md` (new) | `vehicle-system-crosslinks.md`, `vehicle-crosslinks-round-two.md`, `vehicle-crosslinks-round-three.md` |
| `vehicle-health-design.md` | `vehicle-health-consequences.md` (as slice 3) |
| `vehicle-upgrades-round-two.md` | `vehicle-upgrade-families.md` |
| `vehicle-real-figures-design.md` | `vehicle-kerb-mass-check.md`, `vehicle-class-by-empty-mass.md` |
| `vehicle-world-map-design.md` | `vehicle-crew-and-ground-design.md` |
| `vehicle-stations.md` | `vehicle-depot-bays.md`, `vehicle-garage-design.md` |
| `vehicle-accessories.md` | `vehicle-berths.md` (row Y21) |
| `vehicle-hitch-design.md` | `vehicle-trailer-research.md` |
| `vehicle-seabed-crawler.md` | `vehicle-seabed-crawler-art.md` |
| `missile-patches.md` | `strategic-strike.md`; its RimPacts and Visit Settlements material moves out to records of their own |
| `ventilation-and-hydrogen.md` | `workstation-environmental-effects.md` |
| `vfe-factory-conveyors.md` | the belt and hopper sections of `vfe-factory-additions.md` |

Splits:

- `art-automation-and-medical-equipment.md`: the art half to a new short `art-pipeline.md` (the current pipeline end to end, linking
  AGENTS.md for the rules), the medical half to `medical-equipment.md`; a stub stays at the old path for about 30 texture records
- `background-cleanup.md`: paint masks, the fleck checker and the overlay fit (:53-104) to their own record; AGENTS.md's pointer moves
- `utility-vehicles-design.md`: a hub table of links; the bulk tanker (:221-392) to its own record, like every other vehicle
- `phobos-framework.md`: keeps the gas research and health layer; the API log goes to the Framework README or a core API page
- `artwork-direction.md`, `view-angle-models.md` and `vehicle-art-services.md` into one dated image-routes record; the OpenAI engine
  manual (`vehicle-art-services.md` :79-115) into `paid-image-generation-rules.md`
- `analysis-tooling.md` becomes the one tool reference; AGENTS.md keeps one-line rules and points to it
- `outstanding-work.md` keeps open rows; done rows move to an archive section or file

Archive (resolved): `code-audit-2026-11-11.md`, `crash-2026-11-09-stack-overflow.md`, `rimworld-1.7-readiness.md`
(`version-readiness.py` replaces it). Not merged: the one-record-per-vehicle documents (survey truck, drill rig, tipper hauler,
generator set, field kitchen, prisoner transport).

## What this audit cannot say

- Whether every "as built" section repeats its changelog line for line: sampled, not read in full.
- Whether line numbers cited into the 1.6 decompile of Vehicle Framework still hold on its 1.7 build.
- Whether the game behaves as any document says: nothing here is tested in game.
