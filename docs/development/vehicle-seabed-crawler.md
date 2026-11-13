# The Seabed Crawler: design (10 Nov 2026)

**First slice built in D'thorga Vehicles 0.28.0 and D'thorga Framework 0.62.0: the research tab and three projects, the bare hull, and the bottoming wheels and air scrubber, each with its part. The wet laboratory followed in Vehicles 0.29.0 and Framework 0.65.0 (see [what was built](#the-wet-laboratory-as-built)), then aboard-only research and the advanced laboratory in Vehicles 0.30.0 and Framework 0.66.0 (see [aboard-only research as built](#aboard-only-research-as-built)), then the listening array in Vehicles 0.32.0 and Framework 0.72.0 (see [the listening array as built](#the-listening-array-as-built)), then the sonar set and two hull refits in Vehicles 0.38.0 and Framework 0.81.0 (see [the sonar set as built](#the-sonar-set-as-built) and [the hull refits as built](#the-hull-refits-as-built)). Everything else here is still design. The boat became 4 x 9 cells in Vehicles 0.36.0 (owner, 11 Nov 2026), and has its own picture since Vehicles 0.38.0 ([the art record](vehicle-seabed-crawler-art.md)). Not yet tested in game.** The owner chose this as the first real submarine on 10 November 2026, for its unique mechanics, and asked that it also be a research vessel, carry sonar and perhaps radar, and take tie-ins to other mods as soft dependencies. Agent-authored for the owner's review: every figure is **authored** unless it says otherwise. It builds on [the submersibles framework](submarine-framework.md), and the real boat behind it is in [the research record](military-vehicles-research.md).

## Read this first

- **The bare boat does very little (owner, 10 Nov 2026).** It can dive, hide and come home. **Each special ability is its own upgrade, with its own research and its own crafted part.** Nothing below the base hull comes free.
- **Public name:** D'thorga Seabed Crawler. Defs use `Phobos_SeabedCrawler`.
- **Goes in D'thorga Vehicles**, as a part of the existing mod and not a mod of its own. Anything another boat could use goes in D'thorga Framework, per the rule on generic functions.
- **Parts cannot be added by an upgrade.** Vehicle Framework saves parts by position, so a part any upgrade needs is on the Def before that upgrade ships, appended at the end, and the upgrade switches on what it does.
- **Radar and sonar here are not the radio framework**, which stays on the back burner (owner, 9 Nov 2026). This design touches none of it.
- **Paid art waits for the owner's go.** See [the art plan](#the-art-plan).

## What the real boat gives us

NR-1 (US Navy, launched 25 January 1969): a research submarine with two retractable rubber-tyred bottoming wheels that held it at a fixed height over the seabed, a manipulator, and the endurance of a reactor **[P, NAVSEA inactivation report; P, DANFS; S, Wikipedia; S, GlobalSecurity; links in the research record]**. The Ben Franklin PX-15 adds the idea of a drifting laboratory **[S, in the research record]**.

## The upgrades

Every row is one Vehicle Framework `UpgradeNode`, with `ingredients` holding its own crafted part (`Vehicles.decompiled.cs:34859`). **Its own research has to be enforced by us.** The node has a `researchPrerequisites` field (`:34855`), but Vehicle Framework never reads it: `CompUpgradeTree.PrerequisitesMet` (`:21674`) checks only `prerequisiteNodes`, so listing a project there would gate nothing. Another session plans an opt-in marker for this, `PhobosEnforceUpgradeResearch` on the `UpgradeTreeDef` with a postfix on `PrerequisitesMet` (not built). Node keys are saved in the game: never rename or remove one. Each node moves a figure that changes an outcome.

| Node key | Upgrade | Own research | Own crafted part | Needs first | Figure it moves |
| --- | --- | --- | --- | --- | --- |
| `BottomingWheels` | Bottoming wheels | Seabed running gear | Bottoming wheel set | None | Reserve burned at rest, noise while resting |
| `Manipulator` | Manipulator arm | Remote manipulators | Manipulator arm | Wheels | Salvage rate |
| `Scrubber` | Air scrubber | Air purification | Scrubber canister rack | None | Hours under, stale air per hour |
| `OxygenCandles` | Oxygen candle rack | Chemical oxygen | Candle rack | Scrubber | Emergency hours under, used up and restocked |
| `Hydrophones` | Listening array | Hydrophones | Hydrophone array | None | How far it hears, how soon it knows it is being followed |
| `ActiveSonar` | Sonar set | Active sonar | Sonar transducer | Hydrophones | Range and detail of a ping |
| `RadarMast` | Radar mast | Mast radar | Radar set and mast | None | How far it sees while up |
| `WetLab` | Wet laboratory | Marine science | Laboratory fittings | None | Research seats, research speed |
| `DeepLab` | Advanced laboratory | Advanced marine science | Analyser bench | Wet laboratory | Which projects it can work on |
| `SeabedDrill` | Seabed drill | Subsea drilling | Drill string | Wheels and arm | Drilling rate |
| `SensorRefit` | Sensor refit (built 0.38.0) | Faired sensor sail | Faired sensor sail | Sonar set; not with the laboratory refit | Listening and sonar range, deck cargo; the body picture |
| `LaboratoryRefit` | Laboratory refit (built 0.38.0) | Laboratory hull section | Laboratory hull section | Advanced laboratory; not with the sensor refit | Scientist seats, research speed, speed; the body picture |
| `Reactor` | Compact reactor: **ours by default**, Rimatomics' when it is active (owner, 10 Nov 2026) | Compact reactors | Reactor core | None | Hours under, heat, what happens if it is hit |

**New to our vehicles:** the vehicle trees so far all point at the game's research, and only the radio framework has a project of ours. The owner's rule against huge scarce-ingredient costs applies to every part, and the research gates stay proportional to what they unlock.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Bottoming wheels | Settle on the bottom and wait, using less reserve and making less noise | Moving while settled: it must lift off first | Patience becomes a tactic: sit out a raid under the shelling |
| Manipulator and salvage | Bring up finds located by sonar | Salvaging from the surface, or from a boat that is not settled | Water becomes somewhere worth going, with a place to work |
| Air scrubber | Stay down longer, and suffer more slowly past empty | Nothing | Staying down past empty becomes a planned risk |
| Oxygen candles | A one-off extra stretch when caught short | Using it twice without restocking | Something to carry and spend, and a reason to come home |
| Listening array | Hear what is moving nearby and find enemy boats | Nothing | The player stops diving blind |
| Sonar set | Ping a wide area for finds and contacts | Staying secret: every ping gets the boat spotted | A choice each time between knowing and being known |
| Radar mast | See a long way while up | Working under the water at all; a raised mast is easier to spot | The moment on the surface buys something |
| Research vessel | Do real research at sea with the right crew | Researching with the wrong crew, or at a hi-tech level without the advanced lab | Clever colonists have a reason to go to sea |
| Sensor refit | Hear further and ping further | Carrying as much, and the laboratory refit | One hull per boat: a hunter or a laboratory, chosen once and undone only by refitting back |
| Laboratory refit | Seat a third scientist and research faster at sea | Moving as fast, and the sensor refit | As above, the other way |
| Seabed drill | Drill under water | Working a site without settling and sonar first | Lakes and coasts become a mineable map, if the ore is there (see below) |
| Compact reactor | Weeks under | Being careless: a hit on it is a disaster | Endurance becomes a late prize |

## The research vessel

- **Who can contribute** (owner, 10 Nov 2026): **a person seated in a scientist's seat, who is able to do research and has an Intellectual skill of at least 7**, which the owner notes is where the game calls someone a weak professional. The rate is the game's own `ResearchSpeed` for that person, which already follows skill and health, times the vessel's factor. They gain Intellectual experience at the rate a bench gives.
- **How it reaches the game:** the same call a research bench makes, `ResearchManager.ResearchPerformed(amount, researcher)` (`JobDriver_Research`, decompile `all.cs:288371`). **It logs an error if no project is chosen** (`:379075`), so the service checks that first.
- **When it counts** (owner, 10 Nov 2026): at the full rate at rest, and more slowly under way, because the crew is busy with more urgent tasks. **They still learn:** under way the research runs at half the rate **[authored]**, and the Intellectual experience stays at the full rate **[agent reading of "still valuable for learning"]**.
- **What it can work on:** the game limits projects by bench (`requiredResearchBuilding`, `requiredResearchFacilities`; `CanBeResearchedAt`, `all.cs:42924`). A vessel is not a bench, so the wet laboratory takes projects that need no special bench, and the advanced laboratory takes those that need a hi-tech bench and its analyser.
- **Some projects can be done only aboard** (owner, 10 Nov 2026), such as oceanography that opens the later upgrades. See [below](#aboard-only-research).
- **Generic:** the comp goes in D'thorga Framework, so a survey truck or a land laboratory could use it.

### The wet laboratory as built

Built 10 Nov 2026 (Framework 0.65.0, Vehicles 0.29.0). Not yet tested in game.

- **Seats through Vehicle Framework's own role upgrade.** The `WetLab` node's `Vehicles.VehicleUpgrade` adds a `scientist` role with two slots (`UpgradeRole`, `Vehicles.decompiled.cs:34596`). Vehicle Framework saves the role's key with its handler and rebuilds the role from the upgrade tree on load (`VehicleDef.CreateRole`, `:12027`), so the seats survive a save and nothing of ours is saved. **Refunding the node puts everyone aboard ashore** (`RemoveRole` calls `DisembarkAll`, `:15030`).
- **Two stats in the Framework:** `Phobos_FieldLabFactor` (the node adds 0.75, the game's simple research bench's `ResearchSpeedFactor`, read as patched in the owner's game) and `Phobos_FieldLabGrade` (the node adds 1). A `Laboratory` part, appended to the hull's parts, feeds the factor, so damage there slows research. A stat with no part in its category counts at full (`StatEfficiency`, `:9903`).
- **The comp** is `CompPhobosFieldLab` in D'thorga Framework, usable by any vehicle, with game-free rules in `FieldResearchRule` (core `Rules/ResearchRules.cs`, tested). Each rare tick, on a map or through a caravan comp on the world map, every seated person who can research, has Intellectual 7 or more, is awake, sane, not downed and on a Work or Anything shift adds research speed × laboratory factor × ticks (× 0.5 under way) through `ResearchPerformed`, and learns 0.1 Intellectual per tick, as at a bench. The project is checked again before each person's share, because a finished project leaves none current.
- **Projects:** grade 1 takes projects with no `requiredResearchBuilding` and no `requiredResearchFacilities`; grade 2, for the advanced laboratory, would also take those needing the hi-tech bench and its facilities. A project needing any other bench is never taken.
- **The inspect line** names how many are at work and on what, or the first reason nobody is (no project, wrong bench, nobody seated, a named person unskilled, unfit or off shift). It is worked out at the rare tick and read by the panel.
- **Agent choices, owner to revise:** the shift check (the game's own rule for benches); scientists' seats are not needed to work the boat; the part's cost; the research's cost and place on the tab (2, 3.5).

## Aboard-only research

**The owner remembered a gravship mod doing this: it is Vanilla Gravship Expanded, Chapter 1** (Oskar Potocki, Sarg Bjornson and others; decompile in `.local/decompiled/VGE1`, read 10 Nov 2026 and summarised in our own words). What it does:

- **It marks research with an empty `DefModExtension`**, on a tab or a single project (`GravshipResearchUtility.IsGravshipResearch`).
- **A marked project is kept out of the normal queue.** Prefixes on `ResearchManager.SetCurrentProject` and `MainTabWindow_Research.DoBeginResearch` send it to a current-project slot of its own, saved with the world, and one on `IsCurrentProject` answers for that slot. Benches go on working on the ordinary project at the same time.
- **Only its own activity makes progress**, through the game's `ResearchManager.AddProgress`.

**Ours would follow the same shape, written fresh** and never copied: a `DefModExtension` marking a project as field research, a saved slot in a `GameComponent` of ours, three small patches, and the vessel's research service calling `AddProgress`. This has its costs: the patches sit on the research window, where Vanilla Gravship Expanded already patches, so **the two must be checked together**. Ordinary research keeps going at home while a vessel works on its own project, which suits a boat away at sea.

**The tab** (owner, 10 Nov 2026): our projects get their own, titled to match the main one for our vehicle mods. **That main tab is approved by the owner and being built by another session:** `PhobosDthorga`, labelled "D'thorga", in the Framework's always-loaded folder. Ours sits beside it as `PhobosDthorgaSubmarines`, labelled **"D'thorga: Submarines"** (owner, 10 Nov 2026). Project defNames take the prefix `PhobosResearch_`. **Every project keeps one prerequisite that always exists**: a project whose prerequisites all drop out through `MayRequire` opens at once, and `ResearchManager.FinishProject` finishes unfinished prerequisites too (`all.cs:379167`).

### Aboard-only research as built

Built 10 Nov 2026 (Framework 0.66.0, Vehicles 0.30.0). Not yet tested in game. Written fresh after reading Vanilla Gravship Expanded's approach; nothing copied.

- **The marker** is `Phobos.VehicleSystems.FieldResearchExtension`, an empty `DefModExtension` on a project or a whole tab. **The slot** is `GameComponent_PhobosFieldResearch`, saved as `PhobosFieldResearchProject`, which empties itself once its project is finished. `FieldResearch` is the one service that starts, stops and advances it.
- **Three patches** on `ResearchManager`: a prefix on `SetCurrentProject` that sends a marked project to the slot and skips the original only for it (whatever chooses a project ends there, and a bench must never be handed one); postfixes on `IsCurrentProject` (the window shows it in progress and offers Stop) and `StopProject` (empties the slot). No patch on the research window itself.
- **Points:** `AddProgress` takes points, not research speed, so `FieldResearchRule.Points` repeats `ResearchPerformed`'s arithmetic (0.00825 points per speed-tick, the storyteller's research speed, the project's cost factor, the dev fast-research setting), and the scientist's research record is kept.
- **The laboratory's choice:** the aboard-only project when this laboratory can take it on; otherwise the colony's project, so a laboratory too basic for it helps at home.
- **Checked against the owner's list** (`query-patches.py`, 10 Nov 2026): Vanilla Gravship Expanded Chapter 1 patches the same three methods with prefixes that act only on its own marked projects; Research Commission and Nice Research Tab add postfixes. **Nice Research Tab** (decompiled into `.local/decompiled/NiceResearchTab-1.7` for this) touches `SetCurrentProject`'s result only for zero-cost projects, so ours pass through it; but its queue assigns its head with `SetCurrentProject` and waits while `IsCurrentProject` is true, so **an aboard-only project at the head of its main queue holds that queue** until it is finished. Recorded as a limit; the README tells players to choose such projects directly.
- **The first project** is `PhobosResearch_AdvancedMarineScience` (2,500, at (3, 3.5), after marine science and the game's multi-analyzer). It opens the `DeepLab` node (after `WetLab`; +0.25 factor to the hi-tech bench's 1.0, +1 grade; one analyser bench, Crafting 8).
- **Limits:** a colony with no research bench at all cannot choose any project, since the window's `CanStartNow` asks for one; one aboard-only project at a time for the whole colony.

## Sonar and radar

- **Both feed the framework's detection.** `CompPhobosSubmersible.Spot()` is the one way a hull becomes spotted, and it is public so that sonar can call it (Framework 0.59.0).
- **Passive listening stays quiet; a ping gives you away.** That is how sonar works in general **[not yet sourced]**, and it is the whole of the choice. A ping spots the pinging boat for longer than an ordinary sighting.
- **Radar works only with the mast above the water.** Radio waves die within a short distance in sea water **[not yet sourced; the owner will know this better than the record does]**.
- **Sonar sees through fog of war only on water cells.** Radar sees what is above the water and in the open **[agent default]**.

### The listening array as built

Built 10 Nov 2026 (Framework 0.72.0, Vehicles 0.32.0). Not yet tested in game. The owner's go-ahead was "You may proceed" (10 Nov 2026), after the record's row: hear what is moving nearby and find enemy boats; stop nothing; the player stops diving blind.

**What it gives the player in a game where they already see the whole map:** news when they are looking elsewhere (a boat sitting out a raid on another map), a bearing into the fog, a warning that the nearest hostile is close enough to spot a boat that is under, and the only way to find another boat that is under. That last has no use yet, since nobody else has submarines; it is there for the first hostile boat, ours or another author's.

- **The comp** is `CompPhobosListening` in D'thorga Framework, usable by any vehicle in the water. Game-free rules are in `ListeningRule` (`src/PhobosFramework.Vehicles/Rules/ListeningRules.cs`, tested). It saves nothing.
- **Range:** a new stat, `Phobos_ListeningRange`. The node adds 40 cells. A `Hydrophones` part at the bow, appended last to the hull's parts, feeds it, so damage there shortens it. The range is then multiplied by the best **hearing** among those aboard who are awake and up. That is the game's own hearing capacity, so deafness, ear injuries and bionic ears all count. **On the move the range halves**, because a boat's own machinery and the water rushing past mask what it listens for. That is the general principle **[not yet sourced]**; the half is **[authored]**.
- **What it hears, every rare tick (about four seconds):**
  - Any hostile pawn or vehicle **moving in the water**, across the whole range.
  - Anything **moving on land**, within half the range, because sound carries poorly from land into water. This is a **gameplay simplification**: real hydrophones hear little from land.
  - **Something standing still makes no sound to hear.**
  - **A boat that is under** is heard by its noise figure, so a quiet or settled boat is harder to hear, and only half as far when it keeps still. **Hearing it spots it**, through the same `Spot()` the watchers use, so it shows as a dark shape and can be fired on.
- **What the player sees:**
  - An inspect line, either "all quiet within 40 cells" or how many are moving, the nearest one's distance and compass bearing, and how many of them are boats under the water.
  - On a boat that is under, the line adds "close enough to spot the boat" once the nearest contact is within its spotting distance.
  - **One message when movement is first heard**, and again only after three quiet looks in a row, so one contact stopping and starting does not repeat the news.
  - While the boat is selected, a ring shows the hearing range, and a red line runs to each place movement was heard. A line into the fog gives a bearing and lifts no fog.
- **It makes no sound and gives nothing away.** That is the difference from the sonar set, still to come, whose ping will spot the boat that sends it.
- **Limits:**
  - It hears nothing on the world map.
  - It works on the surface as well as under, so the array is no reason to dive.
  - Mechanoid crews, who have no hearing, cannot listen.
  - It does not hear neutral or friendly movement.
- **Agent choices, owner to revise:** 40 cells; half on the move; land at half the range; a still boat at half; hearing from anyone aboard rather than a seat of its own; three quiet looks before the news repeats; the research's cost (1,500) and place (2, 0.5); the part's cost.

### The sonar set as built

Built 11 Nov 2026 (Framework 0.81.0, Vehicles 0.38.0), on the owner's go ("your suggestions are fine", 11 Nov 2026, to the design offered as: your own ping finds everything in range but gives your boat away; an enemy's ping is heard, with its bearing, even without sonar of your own). Not yet tested in game.

- **The comp** is `CompPhobosSonar` in D'thorga Framework, usable by any vehicle in the water. Game-free rules are in `SonarRule` (`src/PhobosFramework.Vehicles/Rules/SonarRules.cs`, tested). It saves nothing: after a load the set is ready again and the boat is no longer given away.
- **Range:** a new stat, `Phobos_SonarRange`; the `ActiveSonar` node adds 60 cells. A `SonarTransducer` part under the bow at (0..1, 3), appended last to the hull's parts, feeds it, so damage there shortens it. Someone aboard must be awake and up to work it; unlike the listening array, hearing does not matter (it is a screen), so a mechanoid crew can ping.
- **A ping, from the "Sonar ping" button:**
  - **Finds** everything hostile in the water within range, **moving or still**, and every boat that is under **however quiet**: what the passive array cannot. A boat resting on the bottom is found only within half the range, lost among the seabed's own echoes beyond that **[the principle is general and not yet sourced; the half is authored]**. Nothing on land.
  - **Spots** each hidden boat it finds, through the same `Spot()` as the watchers and the array.
  - **Gives the pinging boat away:** a boat that is under is followed for 60 seconds as if spotted, so it shows as a dark shape and can be fired on. On the surface it is in plain sight anyway.
  - **Is heard twice as far away as it reaches** (120 cells), by anyone in the water with ears, set or no set: the ping travels one way to a listener but out and back to find anything **[the principle is general and not yet sourced; the factor 2 is authored]**. The player's boats are told the **bearing only**, never the distance, which one ping cannot give a listener.
  - Needs **30 seconds** between pings.
- **What the player sees:** a message with how many were found, the nearest one's distance and bearing, and how many boats among them; for ten seconds, while the boat is selected, the ring the ping reached and a yellow line to each thing found (a line into the fog gives a bearing and lifts no fog); an inspect line, ready or ready again in so many seconds; the button's reason when it cannot ping (out of the water, nobody awake, cooling down). Its sound is the ping already made for being spotted, `PhobosVehicles_SonarPing`, played at the boat with every ping, whoever sends it.
- **Another faction's boat** with a set pings by itself, now and then (15 per cent a look, about every four seconds) while it is under, ready and something hostile to it is within reach. So a hunting enemy boat gives itself away from time to time, and the player's boats hear it, with or without sonar.
- **Limits:** sonar does not lift fog of war (the design's "sees through fog on water cells" is not built); a ping finds only what is hostile to the boat; nothing on the world map; the ping's sound is the same as being spotted's; when a boat that was not yet followed pings while under, both ask for it at once, and **only one plays**, since the SoundDef allows one at a time and the game refuses a second start within 0.05 seconds (`SampleOneShotManager.CanAddPlayingOneShot`, `all.cs:241117`; found by the sound session, Framework 0.82.0).
- **Agent choices, owner to revise:** 60 cells; twice as far heard; 60 seconds given away; 30 seconds between pings; half the range for a settled boat; 15 per cent for another crew; the research's cost (1,500) and place (3, 0.5); the part's cost (30 steel, 5 components, Crafting 7).

### The hull refits as built

Built 11 Nov 2026 (Vehicles 0.38.0). **The owner's idea** (11 Nov 2026): the pilot's attempts 1, 3 and 4 are close enough to be variants reached through upgrade pathways. Mapped as the agent proposed, which the owner accepted. Not yet tested in game.

- **How the picture changes:** Vehicle Framework's own `RetextureDef` on a `VehicleUpgrade` swaps the boat's whole body picture when the node is fitted and puts the base hull back when it is refunded; the vehicle saves which one it wears (`VehiclePawn.retextureDef`, `Vehicles.decompiled.cs:14419`). Nothing of ours is saved. Two `RetextureDef`s, `Phobos_SeabedCrawler_LaboratoryRefit` and `Phobos_SeabedCrawler_SensorRefit`, in `RetextureDefs_SeabedCrawler.xml`.
- **They exclude each other.** Vehicle Framework holds one body picture at a time, and refunding any picture-swapping node puts the base hull back even if another is fitted, so each refit names the other in `disableIfUpgradeNodeEnabled` (`CompUpgradeTree.Disabled`, `:21690`). A boat is either a hunter or a laboratory; changing means refunding one refit and fitting the other.
- **Sensor refit** (attempt 4's neat faired sail), after the sonar set: listening range +20 cells, sonar range +30, cargo capacity -200 (the narrower deck).
- **Laboratory refit** (attempt 1's long panelled casing), after the advanced laboratory: a third scientist's seat (it edits the wet laboratory's `scientist` role to three slots through Vehicle Framework's `editKey`, `VehicleUpgrade.UpgradeRole`, `:34596`), laboratory factor +0.15, move speed -0.5 (the longer hull). Vehicle Framework offers a refund only on the last fitted node of a chain (`LastNodeUnlocked`, `:21703`), so the advanced laboratory and the wet laboratory, whose role it edits, cannot be refunded while it is fitted.
- **Each has its own research and its own part** (owner, 10 Nov 2026): faired sensor sail (2,000, after active sonar) and laboratory hull section (2,500, after advanced marine science), each made at the parts bench at Crafting 8.
- **Limits:** with Vehicle Framework's custom shaders turned off in its settings, it drops the refit picture on load (`VehiclePawn.ExposeData`, `:14458`) and the boat shows the base hull, though the refit's figures stay; whether the role edit is restored on load before anyone is seated has not been traced; the overlays still to come sit on the base hull's canvas, and a sail-top overlay will need a version per hull ([the art record](vehicle-seabed-crawler-art.md)).
- **Agent choices, owner to revise:** which hull goes with which pathway; every figure above; that the two exclude each other rather than one following the other.

**Diving puts out fires** (owner's go-ahead, 10 Nov 2026, offered as: lets the player dive a burning boat to put the fire out; stops a fire burning on a hull under the water; diving becomes an emergency drill as well as a way to hide). When the tanks finish flooding and the boat is fully under, every fire attached to it goes out and the player is told. Before that it can still burn. The helper, `AttachedFires.PutOut`, is in the Framework core for any mod's use. Fire already does no harm to a boat that is under (Framework 0.69.0).

## Chemistry the owner can judge

Arithmetic from atomic masses, assuming the reactions run to completion. **Real use and yields are not yet sourced.** Every in-game figure built on these is authored.

| Upgrade | Reaction | Mass balance |
| --- | --- | --- |
| Air scrubber, lithium hydroxide | 2 LiOH + CO2 → Li2CO3 + H2O | 47.9 g of LiOH per 44.0 g of CO2: about 1.09 kg per kg |
| Oxygen candle, sodium chlorate | 2 NaClO3 → 2 NaCl + 3 O2 | 212.9 g per 96.0 g of O2: about 2.2 kg per kg, before the fuel a real candle burns to keep it going |

## Soft tie-ins

Each loads only when the other mod is active. **Read each mod's code before building its row**; the "what was read" column says how far that has gone.

The radar rows are taken further in [air-defence tie-ins](air-defence-tie-ins.md) (13 Nov 2026): the mast as an IRBM radar (AD9) and as a
search radar for The Dead Man's Switch (AD8). Every other tie-in of ours is in the [tie-in register](tie-in-register.md).

| Mod | The tie-in | What was read |
| --- | --- | --- |
| Rimatomics | **Overrides our default reactor** (owner, 10 Nov 2026: "maybe even overrides"; the agent's pick) with its richer fuel and rules, under the same node key so a save is unaffected | Decompile on hand, not read for this by this record. It has its own research tab. Read by the radiation control work (Framework 0.63.0, Medical 0.11.0), not yet checked here: Rimatomics measures protection as the game's `ToxicEnvironmentResistance`, and `DubUtils.applyRads` scales a dose down to nothing at 1.0; Medical's radiation-controlled zone asks for 0.75. The crew's exposure can be judged with the game-free `Phobos.Framework.Rules.ProtectionRule` (`IsProtected(value, required)`, `Choose(...)`), which knows nothing of cells, without the cell-based `Phobos.Framework.Protection` parts |
| Rimefeller | Subsea oil, as the survey truck already finds oil on land | Decompile on hand, not read for this |
| IRBM | The radar mast counts as an anti-missile radar | `Building_AntiMissileRadar` is a `Building` with a `RadarExtension` (`detectionRange`, `detectionLevel`), so a vehicle would need a bridge of ours |
| Atmosphere Overhaul | The radar mast counts as a weather radar | `WeatherRadarAccess.Working` takes a `Building` too, and the mod is off in the owner's game |
| Research Papers, Research Commission, Semi Random Research | Papers written aboard; a project chosen for the vessel | Not read |
| Vanilla Fishing Expanded | A catch from the boat | Not read |
| D'thorga Medical | Better treatment for stale air | Ours; not designed |

## What is still uncertain

- **Whether deep ore exists under water cells.** The drill and salvage depend on it, and it has not been checked. If it does not, the seabed gets finds of its own, in the way the survey truck's are generated.
- **Whether a pawn in a role with no handling type counts for anything but a seat.** A scientist's seat should be such a role, and that is inferred.
- **How the upgrade tree window handles this many nodes.** Our largest tree so far is much smaller.

## The art plan

**The boat is 4 x 9 cells, drawn 5 x 10 (owner, 11 Nov 2026: "I want the players to feel the weight and size of it"; it was 3 x 8 drawn 4 x 9), in Vehicles 0.36.0.** The game lays a 4-wide hull out on columns x -1 to 2, so the centreline runs between columns 0 and 1; every part kept its place in the list and took new cells (the Def says which). The biggest other boat on the owner's list is Vanilla Vehicles Expanded's Trawler, 2 x 5 drawn 3 x 6, so the Crawler sets its own mark. The picture, the six visible nodes placed on its canvas, the prompt, the costs and the review are in [the art record](vehicle-seabed-crawler-art.md); **nothing is bought before the owner sees the dry run's ceiling and says go.** **Its own picture is in since Vehicles 0.38.0**: attempt 3 of the pilot as the base hull, attempts 1 and 4 as the two refits' hulls, each widened by script to fill the footprint; the test hull wears the base hull too. The six overlays wait for their own paid batch and the owner's go.

## How to test it (the owner's)

Short list, highest consequence first. On a map with deep water and dev mode on.

1. Debug actions, "Vehicle systems: spawn a submersible", pick the Seabed Crawler and click deep water. Check the **D'thorga: Submarines** research tab shows three projects.
2. Try the Upgrades tab without research: the wheels should refuse and name the project. With the research finished and two wheel sets on hand, fit the wheels.
3. Dive, stop, and press **Settle on bottom**. The inspect line should say it is resting, an order to move should be refused, and **Lift off** should free it.
4. Place a blueprint in shallow water and check it can be built there.
5. **Wet laboratory:** with marine science researched and two laboratory fittings on hand, fit it, then board a colonist with Intellectual 7 or more into a scientist's seat during a work shift, with a project chosen that needs no particular bench. The inspect panel should say they are at work and the project's progress should rise; it should rise more slowly while the boat moves. Try someone with Intellectual 6, and a project that needs the hi-tech bench, to see the refusal lines.
6. **Aboard-only research:** with a colony project running at a bench, choose **advanced marine science**. Both should show as in progress; the bench project should keep rising at the bench, and advanced marine science only while a scientist sits in the Crawler's laboratory. Stop it from the research window. Save and load with it chosen, to check the choice is kept.
7. **Listening array:** with hydrophones researched and one hydrophone array on hand, fit it. Select the Crawler: a ring should show the range and the inspect line should say all is quiet. Spawn a hostile raid in dev mode and dive. A message should come when they start moving within range. Red lines should point at them, and the line should say "close enough to spot the boat" as they come near. Set the surfaced Crawler alight with the dev tools' incendiary explosion, then dive: the fire should go out once it is fully under.
8. **The 4 x 9 boat (0.36.0):** spawn a fresh Crawler (one placed by an earlier version keeps its old shape). Its "Flash OccupiedRect" gizmo in dev mode should light 4 cells across and 9 along; a blueprint should want the same; the crew role should seat four; a damage flash on the Health tab should land on parts along the whole hull. Once its own picture is in: it shows in all four facings, the repaint tool recolours the hull and the orange band separately, and fitting the wheels and the listening array shows each overlay. Dive: the usual shadow, now the bigger rectangle.
9. **Sonar set (0.38.0):** with active sonar researched and a sonar transducer on hand, fit it. Select the Crawler: the **Sonar ping** button and an inspect line saying it is ready should show. Spawn a hostile raid, dive, and ping: a message should say how many were found and where, a ring and yellow lines should show for a few seconds, and the boat should be told it has been spotted. The button should then refuse for 30 seconds.
10. **Refits (0.38.0):** with the research and parts on hand, fit the sensor refit. The hull picture should change to the one with the neat sail, the laboratory refit's node should be greyed out, and the ranges should rise in the stats. Refund it: the base hull should come back. Fit the laboratory refit (it needs the advanced laboratory): the long-casing hull should show, three scientists should be able to sit, and a save and load should keep the picture and the third seat.

## Proposed order of building

1. The enforcing check for upgrade research and the main D'thorga tab, in step with the session planning them; then the research projects, the craft recipes and the bare hull.
2. Bottoming wheels and the scrubber, which use what the framework already has.
3. The research vessel comp and the wet laboratory; then aboard-only research and the advanced laboratory (both built).
4. Listening array, then the sonar set (both built), then the hull refits (built).
5. Radar mast, then the salvage and the drill, once the ore question is answered.
6. Reactor and the tie-ins.

## Who chose what

- **Owner (10 Nov 2026):** this is the first real boat; it is also a research vessel whose crew contribute according to intelligence; sonar and perhaps radar; tie-ins as soft dependencies; many of its mechanics are unique upgrades, each needing its own research and crafted parts; Intellectual 7 to contribute; research at rest and more slowly under way; some projects only aboard; their own tab, "D'thorga: Submarines"; a default reactor of ours, with Rimatomics providing a richer one.
- **Agent choices, owner to revise:** the eleven nodes and their order; which need which; half rate under way, with full learning; pings spotting the boat; radar on the surface only; the build order; Rimatomics replacing ours, not sitting beside it.

## Open questions for the owner

All are answered, under [Who chose what](#who-chose-what). **Building waits for the owner's word that the research tree work is finished** (10 Nov 2026), so that upgrade research is enforced from the first node.
