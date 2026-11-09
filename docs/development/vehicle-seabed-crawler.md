# The Seabed Crawler: design (10 Nov 2026)

**Design only. Nothing here is built, and no picture exists.** The owner chose this as the first real submarine on 10 November 2026, for its unique mechanics, and asked that it also be a research vessel, carry sonar and perhaps radar, and take tie-ins to other mods as soft dependencies. Agent-authored for the owner's review: every figure is **authored** unless it says otherwise. It builds on [the submersibles framework](submarine-framework.md), and the real boat behind it is in [the research record](military-vehicles-research.md).

## Read this first

- **The bare boat does very little (owner, 10 Nov 2026).** It can dive, hide and come home. **Each special ability is its own upgrade, with its own research and its own crafted part.** Nothing below the base hull comes free.
- **Public name:** D'thorga Seabed Crawler. Defs use `Phobos_SeabedCrawler`.
- **Goes in D'thorga Vehicles**, as a part of the existing mod and not a mod of its own. Anything another boat could use goes in D'thorga Framework, per the rule on generic functions.
- **Parts cannot be added by an upgrade.** Vehicle Framework saves parts by position, so every part any upgrade needs exists on the Def from the first day, and an upgrade switches on what it does.
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
| `Reactor` | Compact reactor: **ours by default**, Rimatomics' when it is active (owner, 10 Nov 2026) | Compact reactors | Reactor core | None | Hours under, heat, what happens if it is hit |

**New to our vehicles:** the vehicle trees so far all point at the game's research, and only the radio framework has a project of ours. The owner's rule against huge scarce-ingredient costs applies to every part, and the research gates stay proportional to what they unlock.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Bottoming wheels | Settle on the bottom and wait, using less reserve and making less noise | Moving while settled; only a short crawl | Patience becomes a tactic: sit out a raid under the shelling |
| Manipulator and salvage | Bring up finds located by sonar | Salvaging from the surface, or from a boat that is not settled | Water becomes somewhere worth going, with a place to work |
| Air scrubber | Stay down longer, and suffer more slowly past empty | Nothing | Staying down past empty becomes a planned risk |
| Oxygen candles | A one-off extra stretch when caught short | Using it twice without restocking | Something to carry and spend, and a reason to come home |
| Listening array | Hear what is moving nearby and find enemy boats | Nothing | The player stops diving blind |
| Sonar set | Ping a wide area for finds and contacts | Staying secret: every ping gets the boat spotted | A choice each time between knowing and being known |
| Radar mast | See a long way while up | Working under the water at all; a raised mast is easier to spot | The moment on the surface buys something |
| Research vessel | Do real research at sea with the right crew | Researching with the wrong crew, or at a hi-tech level without the advanced lab | Clever colonists have a reason to go to sea |
| Seabed drill | Drill under water | Working a site without settling and sonar first | Lakes and coasts become a mineable map, if the ore is there (see below) |
| Compact reactor | Weeks under | Being careless: a hit on it is a disaster | Endurance becomes a late prize |

## The research vessel

- **Who can contribute** (owner, 10 Nov 2026): **a person seated in a scientist's seat, who is able to do research and has an Intellectual skill of at least 7**, which the owner notes is where the game calls someone a weak professional. The rate is the game's own `ResearchSpeed` for that person, which already follows skill and health, times the vessel's factor. They gain Intellectual experience at the rate a bench gives.
- **How it reaches the game:** the same call a research bench makes, `ResearchManager.ResearchPerformed(amount, researcher)` (`JobDriver_Research`, decompile `all.cs:288371`). **It logs an error if no project is chosen** (`:379075`), so the service checks that first.
- **When it counts** (owner, 10 Nov 2026): at the full rate at rest, and more slowly under way, because the crew is busy with more urgent tasks. **They still learn:** under way the research runs at half the rate **[authored]**, and the Intellectual experience stays at the full rate **[agent reading of "still valuable for learning"]**.
- **What it can work on:** the game limits projects by bench (`requiredResearchBuilding`, `requiredResearchFacilities`; `CanBeResearchedAt`, `all.cs:42924`). A vessel is not a bench, so the wet laboratory takes projects that need no special bench, and the advanced laboratory takes those that need a hi-tech bench and its analyser.
- **Some projects can be done only aboard** (owner, 10 Nov 2026), such as oceanography that opens the later upgrades. See [below](#aboard-only-research).
- **Generic:** the comp goes in D'thorga Framework, so a survey truck or a land laboratory could use it.

## Aboard-only research

**The owner remembered a gravship mod doing this: it is Vanilla Gravship Expanded, Chapter 1** (Oskar Potocki, Sarg Bjornson and others; decompile in `.local/decompiled/VGE1`, read 10 Nov 2026 and summarised in our own words). What it does:

- **It marks research with an empty `DefModExtension`**, on a tab or a single project (`GravshipResearchUtility.IsGravshipResearch`).
- **A marked project is kept out of the normal queue.** Prefixes on `ResearchManager.SetCurrentProject` and `MainTabWindow_Research.DoBeginResearch` send it to a current-project slot of its own, saved with the world, and one on `IsCurrentProject` answers for that slot. Benches go on working on the ordinary project at the same time.
- **Only its own activity makes progress**, through the game's `ResearchManager.AddProgress`.

**Ours would follow the same shape, written fresh** and never copied: a `DefModExtension` marking a project as field research, a saved slot in a `GameComponent` of ours, three small patches, and the vessel's research service calling `AddProgress`. This has its costs: the patches sit on the research window, where Vanilla Gravship Expanded already patches, so **the two must be checked together**. Ordinary research keeps going at home while a vessel works on its own project, which suits a boat away at sea.

**The tab** (owner, 10 Nov 2026): our projects get their own, titled to match the main one for our vehicle mods. **That main tab is approved by the owner and being built by another session:** `PhobosDthorga`, labelled "D'thorga", in the Framework's always-loaded folder. Ours sits beside it as `PhobosDthorgaSubmarines`, labelled **"D'thorga: Submarines"** (owner, 10 Nov 2026). Project defNames take the prefix `PhobosResearch_`. **Every project keeps one prerequisite that always exists**: a project whose prerequisites all drop out through `MayRequire` opens at once, and `ResearchManager.FinishProject` finishes unfinished prerequisites too (`all.cs:379167`).

## Sonar and radar

- **Both feed the framework's detection.** `CompPhobosSubmersible.Spot()` is the one way a hull becomes spotted, and it is public so that sonar can call it (Framework 0.59.0).
- **Passive listening stays quiet; a ping gives you away.** That is how sonar works in general **[not yet sourced]**, and it is the whole of the choice. A ping spots the pinging boat for longer than an ordinary sighting.
- **Radar works only with the mast above the water.** Radio waves die within a short distance in sea water **[not yet sourced; the owner will know this better than the record does]**.
- **Sonar sees through fog of war only on water cells.** Radar sees what is above the water and in the open **[agent default]**.

## Chemistry the owner can judge

Arithmetic from atomic masses, assuming the reactions run to completion. **Real use and yields are not yet sourced.** Every in-game figure built on these is authored.

| Upgrade | Reaction | Mass balance |
| --- | --- | --- |
| Air scrubber, lithium hydroxide | 2 LiOH + CO2 → Li2CO3 + H2O | 47.9 g of LiOH per 44.0 g of CO2: about 1.09 kg per kg |
| Oxygen candle, sodium chlorate | 2 NaClO3 → 2 NaCl + 3 O2 | 212.9 g per 96.0 g of O2: about 2.2 kg per kg, before the fuel a real candle burns to keep it going |

## Soft tie-ins

Each loads only when the other mod is active. **Read each mod's code before building its row**; the "what was read" column says how far that has gone.

| Mod | The tie-in | What was read |
| --- | --- | --- |
| Rimatomics | **Overrides our default reactor** (owner, 10 Nov 2026: "maybe even overrides"; the agent's pick) with its richer fuel and rules, under the same node key so a save is unaffected | Decompile on hand, not read for this. It has its own research tab |
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

**Not started.** The bare hull and one overlay set for each visible node (wheels, arm, mast, sonar dome, drill) are planned together before the first picture, under the rule that every visible node is an overlay.

1. **Measure the planned size against the biggest comparable boat on the owner's list**, then generate at 1.5 times the in-game drawn size or more.
2. `--dry-run` first, `--max-usd` on the real run, and one pilot picture inspected at native size, under the paid generation rules.
3. **Until then it borrows an existing picture**, as the test hull does.

## Proposed order of building

1. The enforcing check for upgrade research and the main D'thorga tab, in step with the session planning them; then the research projects, the craft recipes and the bare hull.
2. Bottoming wheels and the scrubber, which use what the framework already has.
3. The research vessel comp and the wet laboratory.
4. Listening array, then the sonar set.
5. Radar mast, then the salvage and the drill, once the ore question is answered.
6. Reactor and the tie-ins.

## Who chose what

- **Owner (10 Nov 2026):** this is the first real boat; it is also a research vessel whose crew contribute according to intelligence; sonar and perhaps radar; tie-ins as soft dependencies; many of its mechanics are unique upgrades, each needing its own research and crafted parts; Intellectual 7 to contribute; research at rest and more slowly under way; some projects only aboard; their own tab, "D'thorga: Submarines"; a default reactor of ours, with Rimatomics providing a richer one.
- **Agent choices, owner to revise:** the eleven nodes and their order; which need which; half rate under way, with full learning; pings spotting the boat; radar on the surface only; the build order; Rimatomics replacing ours, not sitting beside it.

## Open questions for the owner

All are answered, under [Who chose what](#who-chose-what). **Building waits for the owner's word that the research tree work is finished** (10 Nov 2026), so that upgrade research is enforced from the first node.
