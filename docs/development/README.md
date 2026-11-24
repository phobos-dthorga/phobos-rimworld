# Development and research

Contributor instructions, research, designs and records. Dated records keep their original scope; they don't promise current
behaviour, and each opens with its status. For using the mods, start at the [player guide](../player-guide/README.md).

**Every entry is one line** (`scripts/check-doc-index.py` refuses more): the record, its date, what it is, and its status.
**Built** or **partly built** means in a mod, not tested in game unless the record says the owner tested it; **record only** means
research or design with nothing built; **living** means kept current by every change; **archived** means resolved or overtaken.

## Start here

- [Project direction](project-direction.md): intent, the owner's game, decisions with who made them, and open checks. Living.
- [Building, testing and deploying](building.md): every C# project, the gate, what runs on GitHub, deploying to the game. Living.
- [How the code is built](architecture.md) (23 Nov 2026): layers, ticks, tie-ins, saves and removal, settings, tests. Living.
- [How each system works now](systems/README.md) (23 Nov 2026): living pages written from the code, and their template. Living.
- [Decision logs](decisions/README.md) (23 Nov 2026): every decision of a system, one row each, with who made it. Living.
- [Every setting of every mod](settings-reference.md) (23 Nov 2026): generated from the code and language files; for contributors. Living.
- [The modders' reference site](reference-site.md) (23 Nov 2026): the DocFX site of the Framework's types, with XML examples; published; built by the gate.
- [Analysis tooling](analysis-tooling.md) (7 Nov 2026): the tools for logs, saves, Defs, assemblies and the Workshop. Living.
- [Writing READMEs and in-game text](player-language.md): plain words for players with no radio, engineering or coding knowledge. Living.
- [Documentation audit](docs-audit-2026-11-17.md) (17 Nov 2026): the checklist for cleaning up these documents, in the owner's order. Living.

## Status and trackers

- [Outstanding work](outstanding-work.md) (from 7 Nov 2026): every open item, the owner's decision and its status; done rows at the end. Living.
- [In-game test pass](in-game-test-pass.md) (from 8 Nov 2026): the checks for the owner's next session, and what passes. Living.
- [Seen-in-play checks](seen-in-play-checks.md) (18 Nov 2026): figures, order, parts and staleness for test notes. Built, untested in game.
- [Test automation](test-automation.md) (25 Nov 2026): save and live checks, saved histories as test evidence. Partly built, untested in game.
- [Patches for other authors' faults](temporary-patches.md) (10 Nov 2026): each such patch, temporary or permanent, and how to retire it. Living.
- [Tie-ins with other mods](tie-in-register.md) (13 Nov 2026): every tie-in folder and C# tie-in, by our mod. Living.
- [Dependency audit](dependency-audit-2026-11-16.md) (16 Nov 2026): what the mods we build on changed during the owner's break. Decisions answered.
- [Dependency audit, VF 1.7.2537](dependency-audit-2026-11-18.md) (18 Nov 2026): VF now enforces upgrade research; nothing of ours breaks. One proposal open.
- [Fault-read of the untested vehicle work](fault-read-2026-11-17.md) (17 Nov 2026): 36 faults in untested vehicle code. Fixed in Framework 0.120.5, untested.
- [Fault-read of the Osprey's first batch](fault-read-2026-11-19.md) (19 Nov 2026): 30 faults in the Osprey, flight and armour work. Part fixed, untested.
- [Fault-read of the untested work since 21 Nov](fault-read-2026-11-25.md) (25 Nov 2026): 51 faults in six areas. Fixed, untested.
- [Fault-read of D'thorga Farming](fault-read-2026-11-21.md) (21 Nov 2026): 66 faults in farming batches one and two. Fixed, untested; five left open.
- [Mod consolidation](mod-consolidation.md) (7 Nov 2026): eleven mods into six, how identities and settings were kept. Done.

## D'thorga Framework (core)

- [Gases and air](systems/gases-and-air.md) (23 Nov 2026): how room gases, masks, supplied oxygen and gear lines work now. Living.
- [Power, top-up and machines](systems/power-and-machines.md) (23 Nov 2026): how standby power, topping up, bills, research and pipes beneath work now. Living.
- [Framework services](systems/framework-services.md) (23 Nov 2026): how settings, logging, the loop watch, sub-tabs, recall and the rest work now. Living.
- [The health layer and care](systems/health-layer.md) (23 Nov 2026): how patients, condition danger, care choices and field care work now. Living.
- [Research gates audit](research-gates-audit-2026-11-17.md) (17 Nov 2026): what is gated and what is not; proposals R1 to R9. Built, bar R5 and R7.
- [A setup wizard on first load](first-load-setup-wizard.md) (9 Nov 2026): the owner's idea, kept for later. Record only.
- [Alternate looks](alternate-looks.md) (19 Nov 2026): good unused pictures as choosable looks; skins and customisation. Record only.

## Vehicles

### Systems and health

- [Vehicle systems](vehicle-systems-design.md) (8 Nov 2026): the schema-driven service for electrical, coolant, oil and more. Built.
- [The vehicle tabs](vehicle-tabs-design.md) (8 Nov 2026): the Dashboard, Systems and Health tabs and their history graphs. Built.
- [How our vehicle features look](vehicle-presentation-design.md) (11 Nov 2026): the diagnosis report, fleet board, icons and graphs. Partly built.
- [Other factions' vehicles: an audit](vehicle-other-factions-audit.md) (11 Nov 2026): every player-faction check against the owner's rule. Built.
- [Headlights that light the ground](vehicle-headlights.md) (12 Nov 2026): a beam of the game's own light ahead of every vehicle. Built.
- [Vehicle damage](systems/vehicle-damage.md) (23 Nov 2026): how vehicles are hurt, knocked out, burned and mended now. Living.

### Power, drivetrain and aircraft

- [Engines and drivetrains](systems/engines-and-drivetrain.md) (23 Nov 2026): how engines, gears, loads and fuel work now. Living.
- [Flight](systems/flight.md) (23 Nov 2026): how aircraft fly, lift, refuel, see, dodge and jam now. Living.
- [Parachute airdrops](airdrop.md) (25 Nov 2026): people, cargo and vehicles by parachute on the wind; the vehicle hold; the C-17's study. Built, untested.
- [The world map and the cab](vehicle-world-map-design.md) (8 Nov 2026): caravans, cab climate, booster packs, ground and cab moods. Built.
- [Real figures for named vehicles](vehicle-real-figures-design.md) (8 Nov 2026): power, weights, gearboxes; kerb mass; class by empty mass. Built.

### Upgrades and cross-links

- [Upgrades and fittings](systems/upgrades-and-fittings.md) (23 Nov 2026): how fittings, tunes, grades, trees and parts work now. Living.
- [Vehicle upgrades, round two](vehicle-upgrades-round-two.md) (11 Nov 2026): every vehicle's tree, signature features, shared families. Partly built.
- [Vehicle cross-links](vehicle-crosslinks.md) (9 to 13 Nov 2026): three rounds of links between systems, crew, weather and damage. Built, bar engine braking.
- [Refuelling from cargo](vehicle-cargo-refuel.md) (18 Nov 2026): caravans top up from their cans; aircraft set down on the way. Built, untested.
- [Thin air, turbines, aircraft fuel](vehicle-altitude-and-turbines.md) (18 Nov 2026): altitude, turbine gaps, fuel by flight phase. Built, untested.

### Utility vehicles, stations and trailers

- [Utility vehicles](utility-vehicles-design.md) (8 Nov 2026): the family's hub: principles, parts model, catalogue, art plan, a link to each vehicle. Built.
- [The bulk tanker](vehicle-bulk-tanker.md) (8 Nov 2026): the Ullage tanker's design, fluids, compartments and bulk buying. Built.
- [The survey truck](vehicle-survey-truck.md) (9 Nov 2026): a ground-penetrating scanner on wheels. Built; its boom owner-tested.
- [The mobile drill rig](vehicle-drill-rig.md) (9 Nov 2026): drills the game's deep resource grid at its own rates. Built.
- [The tipper hauler](vehicle-tipper-hauler.md) (9 Nov 2026): a high-capacity body and one tip job. Built.
- [Gathering runs](vehicle-gathering-runs.md) (19 Nov 2026): a vehicle fetching loose things by itself, by filter; the Talus first. Built.
- [Militarised service vehicles](vehicle-militarised-service-vehicles.md) (19 Nov 2026): armour kits, guns and smoke for the five trucks. Design only.
- [The mobile generator set](vehicle-generator-set.md) (9 Nov 2026): the Busbar, feeding a camp's grid when parked. Built; size and look owner-tested.
- [Stations, yard and depots](systems/stations-yard-and-depots.md) (23 Nov 2026): how stations, the yard, bay trips, depots and the garage work now. Living.
- [Working trailers](vehicle-trailers.md) (13 Nov 2026): the shared base, the bowsers and a catalogue of nineteen trailers. Partly built.
- [Hitches and trailers](systems/hitches-and-trailers.md) (23 Nov 2026): how hitching, couplings, trailers, towing and recovery work now. Living.
- [The field kitchen trailer](vehicle-field-kitchen.md) (13 Nov 2026): Billy, the owner's kitchen on wheels. Built; enemy bases and locker not.
- [The prisoner transport trailer](vehicle-prisoner-transport.md) (13 Nov 2026): Stockade, the owner's cage trailer. Record only.
- [Camping and life aboard](systems/camping-and-life-aboard.md) (23 Nov 2026): how camps, berths, cabins, the cab and life aboard work now. Living.
- [Crew machines](vehicle-crew-machines.md) (23 Nov 2026): sleep and medical machines on vehicles, CM1 to CM5. Partly built.
- [Nutrient paste on vehicles](vehicle-nutrient-paste.md) (23 Nov 2026): paste in tanks, on pipes, on the road, NP1 to NP5. Record only.
- [People use parked vehicles](vehicle-parked-use.md) (25 Nov 2026): colonists use a parked vehicle by themselves. Built, untested.

### Farming

- [Farming](systems/farming.md) (24 Nov 2026): how paddocks, the combine, bins, tractors, implements and seed work now. Living.
- [Farm machines](farming-machines.md) (19 Nov 2026): D'thorga Farming: paddocks, tractors, drills, harvesters, sprayers. Record only.
- [Farms on other tiles](farming-remote-farms.md) (19 Nov 2026): a ledger farm the player can open and watch; 29 hazards. Record only.
- [Soil rules](farming-soil.md) (19 Nov 2026): three presets, moisture as a bonus, a punishing Realistic mode, seed. Record only.
- [Farming batch one](farming-batch-one.md) (19 Nov 2026): build design for paddocks, combine, chaser bin, bins; art plan. Built: Farming 0.1.0 to 0.3.3.
- [Farming batch two](farming-batch-two.md) (20 Nov 2026): build design for tractors, the linkage, plough, cultivator, drill and seed. Record only.
- [Tractor recordings](farming-tractor-recordings.md) (20 Nov 2026): openly licensed tractor recordings by size. Research only.

### Submarines and long range

- [The submersibles framework](submarine-framework.md) (9 Nov 2026): diving, the reserve, and what still reaches a hull. Built.
- [The Seabed Crawler](vehicle-seabed-crawler.md) (10 Nov 2026): Fathom, the research submarine: upgrades, sonar, labs and its picture. Built.
- [Striking another map](cross-map-strikes.md) (11 Nov 2026): DMS Expedition's lost shells and rules for our cross-map weapons. Record only.
- [Air-defence tie-ins](air-defence-tie-ins.md) (13 Nov 2026): IRBM's and The Dead Man's Switch's radars against our aircraft. Partly built.
- [Unique military vehicles and submarines](military-vehicles-research.md) (9 Nov 2026): real vehicles no mod covers yet. Record only.
- [Per-engine realism](per-engine-realism.md) (23 Nov 2026): power by engine health, yaw, matched axles, wear per engine. Step 1 built, untested.
- [Railcars](vehicle-railcars.md) (22 Nov 2026): a loadout for Trains of the Rim's wagons, so they can take fittings. Set aside by the owner; nothing built.
- [Modern military aircraft and vehicles](modern-military-vehicles.md) (18 Nov 2026): the Osprey, armed candidates, crewless fits. Record only.
- [The V-22 Osprey](vehicle-osprey.md) (18 Nov 2026): our first aircraft: figures, parts, guns, upgrades, art plan. Record only.

### Tie-ins and contracts

- [Soft-dependency cross-links](soft-dependency-crosslinks.md) (13 Nov 2026): new tie-ins with mods on the owner's list, SD1 to SD47. Partly built.
- [Vehicles at Work](vehicles-at-work-tie-ins.md) (19 Nov 2026): what we gain from it and learn from it, VW1 to VW7. VW1 to VW3 built; two faults fixed.
- [The authors' override contract](vehicle-author-overrides.md) (13 Nov 2026): what another author can set for their vehicles. Record only.
- [What four new mods teach us](four-mods-study-2026-11-21.md) (21 Nov 2026): Farm and Ranch, Moving Parts, Realistic Rain; LM1 to LM11. LM1, LM2 building.

### Research and figures

- [Vehicle Framework mechanics](vehicle-framework-mechanics.md) (8 Nov 2026): its parts, ticking, fuel, settings and saves, from source. Research.
- [Vehicle figures](vehicle-figures-research.md) (8 Nov 2026): sourced engine, gearbox and weight figures for named vehicles. Research.
- [Steam and diesel-electric](steam-and-diesel-electric-research.md) (8 Nov 2026): boilers, shunters' ratings, engine-out precedents. Research.
- [Vehicle simulation references](vehicle-references.md) (8 Nov 2026): the study library behind the vehicle designs. Research.
- [World elevation and descents](world-elevation-and-descents.md) (18 Nov 2026): tile heights measured; descents, altitude, radio line of sight. Research.
- [Animating our vehicles](vehicle-animation-research.md) (9 Nov 2026): what can move, and where motion helps. Partly built; the boom owner-tested.
- [Emergent play around the vehicles](vehicle-emergent-play.md) (20 Nov 2026): where unplanned stories could come from; three audits, EM1 to EM29. Partly built.
- [Traps and vehicles](vehicle-traps.md) (21 Nov 2026): your vehicles pass over your traps when you did not send them; a setting. Built 0.213.0, untested.

## D'thorga Medical

Five mods until 7 November 2026; each record keeps its own mod's name and scope.

- [Injuries and care](systems/medical-injuries-and-care.md) (23 Nov 2026): how burns, smoke, caustic gas, dosing and triage work now. Living.
- [Medical equipment](systems/medical-equipment.md) (23 Nov 2026): how the oxygen, beds, devices, alarm and showers work now. Living.
- [Radiation](systems/medical-radiation.md) (23 Nov 2026): how the dose, radiation sickness, care, late effects, dust and Rimatomics' doses work now. Living.
- [Medical save bridge, how it works](systems/medical-save-bridge.md) (24 Nov 2026): the bridge for colonies saved before More Injuries, now. Living.
- [Medical save bridge](archive/medical-save-bridge.md) (3 Nov 2026): why adding More Injuries beside MSE2 wiped wounds, and the bridge. Archived.
- [Ventilation and hydrogen](ventilation-and-hydrogen.md) (7 Nov 2026): the electrolyser's hydrogen, vents, and workstation gases. Record only.
- [Vaccines and prevention](vaccines-and-prevention.md) (7 Nov 2026): doctors preventing disease in healthy pawns. Record only.

## D'thorga Patches

- [Patches](systems/patches.md) (24 Nov 2026): how each tie-in patch works now, temporary and permanent, by kind. Living.
- [Nuclear strikes, vehicles and people](nuclear-strike-effects.md) (22 Nov 2026): vehicles in a strike; the real effects; NK1 to NK27. Partly built, untested.
- [Soldiers keep their guns](soldiers-keep-their-guns.md) (19 Nov 2026): why soldiers fought with grenades under CE, and the fix. Built, untested.

## Factory Additions and Water Tap

- [Factory Additions](systems/factory.md) (24 Nov 2026): how belts, the output guard, bill repair, generated bills and piped water work now. Living.
- [Universal hoppers](universal-hoppers.md) (17 Nov 2026): who reads a hopper, the bulk bunker, belts to vehicles, UH1 to UH16. Partly built.
- [Water tap](water-tap.md) (4 Nov 2026): Dubs Bad Hygiene's bottles and water quality, and the split-stack fix. Built.

## Radio

- [D'thorga Radio Framework: design](radio-framework-design.md) (18 Oct 2026): the owner's design, a snapshot of the living doc. Phase 1 built.
- [Radio reference checks](radio-reference-checks.md) (18 Oct 2026): each reference figure checked against its source. Research.
- [Map scale research](map-scale-research.md) (19 Oct 2026): what a cell and a world tile measure, and why 1 m per cell. Research.
- [Autonomous vehicles by radio](autonomous-vehicles.md) (24 Nov 2026): trucks driven and fought from a radio console. Design only.
- [Radio visuals](radio-visuals.md) (23 Nov 2026): twenty ways to show the link budget as pictures; coverage, ray line and side view built. Partly built.
- [Radio stations](radio-stations.md) (24 Nov 2026): the radio desk, a radio terminal and amplifier, and upgrades. Partly built.

## Art and sound

- [Art pipeline](art-pipeline.md) (17 Nov 2026): how a picture reaches a mod today, its routes and steps; the first art design as history. Living.
- [Image routes research](image-routes-research.md) (19 Oct to 8 Nov 2026): first routes, view-angle models, paid services compared. Research.
- [Art backlog](art-backlog.md) (6 Nov 2026): every texture and preview the mods ship or still need. Living.
- [Art backups hand-off](art-backups-handoff.md) (21 Nov 2026): "safe copies" of training sets and style models. Built: kit 1.66.0, Companion 0.42.0.
- [Art automation and medical equipment](art-automation-and-medical-equipment.md) (4 Nov 2026): split on 17 Nov; links to both halves. Stub.
- [Background clean-up](background-cleanup.md) (12 Nov 2026): the background AI that clears white from our pictures. Built.
- [Vehicle paint masks](vehicle-paint-masks.md) (16 Nov 2026): paint by region, repaired by parts, the fleck checker; overlay fit idea. Built.
- [Paid image generation](paid-image-generation-rules.md) (8 Nov 2026): the rules for every picture bought from the OpenAI API. Living.
- [Sound generation](sound-generation.md) (10 Nov 2026): local sounds with Stable Audio 3, ffmpeg and the listening page. Built.
- [Sound catalogue](sound-catalogue.md) (10 Nov 2026): every sound we need, with IDs; 117 ship. Partly built.

## The owner's game and tools

- [The owner's mod list](owner-mod-list.md): every mod the owner has on, with suggestions; written by `refresh-mod-list.py`. Living.
- [RimWorld modding reference](rimworld-modding-reference.md) (18 Oct 2026): versions, About.xml, load folders, Ludeon's EULA. Reference.
- [Mod installs](mod-installs.md) (18 Nov 2026): updates, going back, backups, builds and textures; Companion's Mods page. Built, untested.
- [The AGENTS.md split](agents-split-2026-11-21.md) (21 Nov 2026): where each of the 99 rules went; the root 56 to 14 KB. Built.
- [Smoke run on a lean profile](smoke-run-design.md) (18 Nov 2026): our mods alone on the quick-test map, spawned and ticked before an install. Design.
- [Finding: patch XML kept in memory](patch-xml-kept-alive-2026-11-21.md) (21 Nov 2026): 453 MB held via VEF's settings code. Found; nothing changed.
- [Desktop front end for the Phobos tools](tools-desktop-research.md) (9 Nov 2026): the research behind D'thorga Companion. Research.

## Archive

- [The archive](archive/README.md) (23 Nov 2026): records moved unchanged, by system; old paths in changelogs still name them.
- [Vehicle drivetrain and engine speed](archive/vehicle-drivetrain-design.md) (8 Nov 2026): rpm and gears worked back from Vehicle Framework's speed. Archived.
- [Gas turbines](archive/vehicle-turbine-design.md) (8 Nov 2026): turboshaft helicopters, the Harrier's engine and the tank turbine. Archived.
- [Steam boilers](archive/vehicle-steam-design.md) (8 Nov 2026): raising steam, safety valves and the fusible plug for Trains of the Rim. Archived.
- [Electric vehicles](archive/vehicle-electric-design.md) (8 Nov 2026): traction batteries, the electric drive and charging. Archived.
- [Engine loads](archive/vehicle-engine-loads.md) (18 Nov 2026): a top-down review, and one ledger of what the engine drives. Archived.
- [Power and lift](archive/vehicle-power-and-lift-design.md) (8 Nov 2026): many engines, rotor drives, thrust pods and diesel-electric. Archived.
- [Avionics for every aircraft](archive/vehicle-avionics.md) (13 Nov 2026): instruments, navigation, sensors and the avionics fit. Archived.
- [Sling loads](archive/vehicle-sling-design.md) (11 Nov 2026): a helicopter carries a vehicle under it. Archived.
- [Our own flight over the map](archive/vehicle-flight-over-the-map.md) (18 Nov 2026): hover, circling, rolling take-off, cruising height. Archived.
- [UH-60M Black Hawk](archive/uh60-blackhawk.md) (22 Nov 2026): z3nt3's Black Hawk in the fold; a judgement of the mod. Archived.
- [C-17 Globemaster III](archive/c17-globemaster.md) (23 Nov 2026): z3nt3's C-17 in the fold; four engines by wing; its hold's weight. Archived.
- [Close Air Support's A-10 and Su-25](archive/close-air-support-jets.md) (21 Nov 2026): both jets in the fold, our own dodge. Archived.
- [EA-6B Prowler](archive/ea6b-prowler.md) (22 Nov 2026): the jamming jet in the fold, ICAP upgrades; ECM ideas, steps 2 to 4. Archived.
- [A pawn-like health model for vehicles](archive/vehicle-health-design.md) (8 Nov 2026): conditions on Vehicle Framework's parts, slices 0 to 3. Archived.
- [Knocked out, not destroyed](archive/vehicle-knocked-out-systems.md) (16 Nov 2026): weapons, tracks, working gear and crew lost before a wreck. Archived.
- [Armour for our armed vehicles](archive/vehicle-armour.md) (18 Nov 2026): classes by the threat stopped; Combat Extended first. Archived.
- [Spall inside our vehicles](archive/vehicle-spall.md) (21 Nov 2026): debris from a beaten plate, and spall liners; Combat Extended only. Archived.
- [Glass on our vehicles](archive/vehicle-glass.md) (22 Nov 2026): glass as a tracked part, and what follows when it breaks; VG1 to VG14. Archived.
- [Fire on vehicles](archive/vehicle-fire.md) (22 Nov 2026): what burns a vehicle today, a fault in our fire checks; FR1 to FR14. Archived.
- [Fires on vehicles that never go out](archive/vehicle-framework-fires.md) (10 Nov 2026): Vehicle Framework's stuck fires and the fix. Archived.
- [Vehicle tuning and upgrades](archive/vehicle-tuning-and-upgrades.md) (9 Nov 2026): fittings, engine tunes and part grades for any vehicle. Archived.
- [Aircraft service-life upgrades](archive/aircraft-service-life-upgrades.md) (21 Nov 2026): engines, avionics kits, early fits. Archived.
- [Vehicle accessories](archive/vehicle-accessories.md) (13 Nov 2026): a parked vehicle as the heart of a camp, Y1 to Y23, berths. Archived.
- [Hotboxing](archive/vehicle-hotboxing.md) (12 Nov 2026): smoking in a vehicle's cab. Archived.
- [Walk-in cabins](archive/vehicle-cabins.md) (22 Nov 2026): a vehicle's cabin as a small pocket map; the Osprey first. Archived.
- [The hitch and the generic trailer](archive/vehicle-hitch-design.md) (11 Nov 2026): our own hitch, a trailer any vehicle tows, RimThunder's way. Archived.
- [Vehicle stations](archive/vehicle-stations.md) (8 to 9 Nov 2026): the parts bench, tuning rig, motor pool tab, depot bays and the garage. Archived.
- [Service yard](archive/vehicle-service-yard.md) (20 Nov 2026): servicing, dyno, fuel and parking in one capped zone; building upgrades. Archived.
- [Bay trips](archive/vehicle-bay-trips.md) (20 Nov 2026): vehicles driven, towed or pushed to a dyno bay for repair by themselves. Archived.
- [Depots and forward fuel points](archive/vehicle-depots.md) (20 Nov 2026): fuel left on the world map for later trips; DP1 to DP10. Archived.
- [Radiation control line](archive/radiation-control-line.md) (10 Nov 2026): where colonists suit up for Rimatomics' radiation. Archived.
- [D'thorga Framework: research and decisions](archive/phobos-framework.md) (4 Nov 2026): room gases, supplied oxygen and the health layer. Archived.
- [Standby power](archive/standby-power.md) (22 Nov 2026): idle buildings draw 10%, empty rooms go dark; in place of LightsOut and Turn It On and Off. Archived.
- [Topping up machines](archive/top-up-design.md) (18 Nov 2026): haulers refill machines that hold a material, named in data. Archived.
- [Parts bills](archive/parts-bills.md) (11 Nov 2026): what a built thing is made of, by role, filled by installed mods' parts or ours. Archived.
- [Pipes and conduits beneath our things](archive/network-underlays.md) (13 Nov 2026): hidden pipes under our buildings and parked vehicles. Archived.
- [The D'thorga research tree](archive/research-tree.md) (10 Nov 2026): the research tab, its projects and the grant for older colonies. Archived.
- [Our Architect tab and its sub-tabs](archive/architect-tabs.md) (21 Nov 2026): one D'thorga tab with sub-tabs. Archived.
- [Pick-up and put-down loops](archive/pick-up-loops.md) (21 Nov 2026): the loop watch names a colonist caught in one. Archived.
- [Drones called home, wrecks taken home](archive/drone-recall-design.md) (23 Nov 2026): recall by the walk home; wrecks back to a platform. Archived.
- [A shared logging framework](archive/logging-framework-design.md) (8 Nov 2026): the 10,000-message cap, phobos-log and verbose logging. Archived.
- [Colony mood file](archive/mood-snapshot.md) (10 Nov 2026): every colonist's thoughts written at each save for the mood report. Archived.
- [Settings pages](archive/settings-pages.md) (20 Nov 2026): every settings window as pages with search, changed only and reset; the settings audit. Archived.
- [Burns and smoke](archive/burns-and-smoke.md) (4 Nov 2026): burns, burn shock, airway burns, carbon monoxide and fire heat. Archived.
- [Caustic gas](archive/caustic-gas.md) (5 Nov 2026): the game's tox gas on eyes, lungs and skin, slow by default. Archived.
- [Emergency dosing](archive/emergency-dosing.md) (5 Nov 2026): doctors giving painkillers and sedatives by themselves, by effect. Archived.
- [Triage](archive/triage.md) (6 Nov 2026): queueing life-saving surgery, classing conditions by effect. Archived.
- [Medical equipment](archive/medical-equipment.md) (5 Nov 2026): bedside oxygen and the burn unit bed. Archived.
- [Medical equipment, second set](archive/medical-equipment-2.md) (7 Nov 2026): the ventilator, steriliser and six more. Archived.
- [A radiation model of our own](archive/radiation-model.md) (22 Nov 2026): dose, radiation sickness, care, late effects; RM1 to RM16. Archived.
- [VFE Factory additions](archive/vfe-factory-additions.md) (28 Oct 2026): factory bills, the output guard, piped water, endless mince. Archived.
- [VFE Factory conveyors and hoppers](archive/vfe-factory-conveyors.md) (28 Oct 2026): what belts pull from and push to; switches, stuck belts. Archived.
- [Vehicle patches](archive/vehicle-patches.md) (2 Nov 2026): the Harrier's costs, rearming and disassembly; bombs, castings, Spaceship Turret Hull. Archived.
- [Aircraft munitions](archive/aircraft-munitions.md) (23 Nov 2026): the aircraft's bombs, missiles and pod at real weight, priced like the Mk. 82. Archived.
- [Missile patches](archive/missile-patches.md) (7 Nov 2026): IRBM's strategic warhead hang and our strike model. Archived.
- [RimPacts patches](archive/rimpacts-patches.md) (2 Nov 2026): the Harrier's load fix, sorties over battles, struck towns' assaults, empty vehicles. Archived.
- [Visit Settlements patches](archive/visit-settlements-patches.md) (8 Nov 2026): a battle map closed under the player, food drops every tick. Archived.
- [Mech rechargers that draw real power](archive/mech-recharging-real-power.md) (20 Nov 2026): rechargers draw by the mech's size; off by default. Archived.
- [Code audit: saves and cost per tick](code-audit-2026-11-11.md) (11 Nov 2026): one save fault, fixed in Framework 0.96.1. Archived.
- [Crash record: stack overflow](crash-2026-11-09-stack-overflow.md) (9 Nov 2026): Prison Labor's loop, fixed by its author. Archived.
- [RimWorld 1.7 readiness](rimworld-1.7-readiness.md) (24 Oct 2026): the mod list before the move to 1.7. Archived.
