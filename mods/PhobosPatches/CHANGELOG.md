# D'thorga Patches changelog

Maintained from 2 November 2026, as D'thorga Vehicle Patches until 7 November 2026; D'thorga Missile Patches' history is at the
end. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet tested in game unless an entry
says the owner tested it.

## [Unreleased]

### Changed

- **The README's links to the design records point to their new place** in the development archive (the docs
  rewrite, 24 Nov 2026); how the mod works now is on a living page beside them. Nothing a player sees changes.
- **The art and sound records no longer ship in the mod's folder** (the owner's choice of 23 Nov 2026, in the docs rewrite).
  The notes saying how each picture and sound was made moved to the repository's assets folder, so installing the mod no longer
  copies them into the game's Mods folder; the next install leaves the old copies in its backup. Nothing a player sees changes.
- **The exporters' records no longer ship in the mod's folder either** (the owner's word of 23 Nov 2026). The notes saying
  what each picture was cut from moved beside the art records, in the repository's assets folder; nothing a player sees
  changes.
- **The README is slimmer**: real gotchas, one line per patched mod, the settings and links; the settings table gains the
  sidearm fetch guard and the two Realistic handling settings it lacked. The record links moved to the repository's modders'
  page for this mod. The README and the Workshop draft now name the owner's later passes and the C-17. Nothing in play changes.

## [0.40.0] - 2026-11-23 - Draft

The C-17 Globemaster III built from real parts

### Added

- **The C-17 Globemaster III is built from real parts** (the owner's request of 23 November 2026; docs/development/c17-globemaster.md): its blueprint keeps its airframe (2,000 steel, 500 plasteel, 300 silver and 40 components, from 2,250, 600, 300, 120 and 450 chemfuel) and a parts bill adds four engines and their generators, four hydraulic pumps and actuators, three landing gear, two fuel tanks, a battery, wiring and avionics, each from an installed mod where it suits, else ours. All authored, for the owner's review. Not yet tested in game: row 420 of the test pass.

### Save compatibility

- Safe. A C-17 already built keeps working. A C-17 frame under construction keeps what was delivered and then asks for the new list: finish or cancel it before installing.

## [0.39.0] - 2026-11-23 - Draft

The Dead Man's Switch's drones come home

### Added

- **The Dead Man's Switch's drones are called home while they can still get there, and their wrecks are carried back to a platform** (the owner's report of 23 November 2026; docs/development/drone-recall-design.md). A new Fortified Features Framework part names its drone comp, so D'thorga Framework 0.240.0 calls each drone home once its power would barely cover the walk, and haulers carry wrecks to a platform, which keeps their steel. Fortified's own call at two hours of power still stands. Loads only with Fortified Features Framework, adds no setting of its own (D'thorga Framework has two) and saves nothing, so it is safe to add to and remove from a running game. Not yet tested in game: rows 384 and 385 of the test pass.

## [0.38.0] - 2026-11-23 - Draft

The research tab opens every time

### Fixed

- **With Nice Research Tab, a blank entry can no longer stop the research tab opening** (temporary; the owner's report of 23 November 2026, and their choice of this guard). Now and then, opening the tab failed, its window showed errors every frame, and it stayed broken until the game was restarted: a research project's list of what it unlocks had a blank in it. The likely cause, not proven: Nice Research Tab reads those lists from a background task while the game reads them too, and Vanilla Expanded Framework's patches change the lists on every read, so two changes could meet. A new Nice Research Tab part lets only one reader at a time into those lists and takes out any blank that is there anyway, saying once in the log which project held it. It loads only while Nice Research Tab is active, adds no setting and saves nothing, so it is safe to add to and remove from a running game. Not yet tested in game: row 382 of the test pass.

## [0.37.1] - 2026-11-23 - Draft

The new munitions at the automated benches

### Fixed

- **The Shturm, Hellfire and HARM are made at The Dead Man's Switch's ammunition stamping machine too, as the bombs are**, and the missile seeker head and the jamming pod at The Dead Man's Switch - Expedition's precise fabrication center, as electronics (the owner's report of 23 November 2026: not every new munition was at the automated benches). The A-10's Mk. 82 is now at the stamping machine without the Harrier too. Bills already made stay where they are. Not yet tested in game: row 381 of the test pass.

## [0.37.0] - 2026-11-23 - Draft

The new aircraft's weapons in line with the Mk. 82

### Balance

- **The A-10's, Su-25's, Hind's, Mongoose's and Prowler's bombs, missiles and jamming pods now cost what the Mk. 82 does for their size** (the owner's request of 23 November 2026: they were far too cheap; docs/development/aircraft-munitions.md). Under Combat Extended each has its real weight and is made from what it is made of, its value worked out from the ingredients: the FAB-250 about 1,630 silver (was 280), the RBK-250 1,060 (190), the CBU-87 2,000 (380), the Shturm 520 (310), the Hellfire 790 (410), the HARM 1,580 (400) and the jamming pod 1,100 (340). Real weights: the RBK-250 273 kg (was 40), the CBU-87 430 kg (60), the HARM 360 kg (80), the pod 431 kg (40), the Hellfire 45 kg and the Shturm 46.5 kg. Fills follow the Mk. 82's rule, by kg of real explosive, so the cluster bombs take little FSX and more steel. Weapons already made keep their old cost. Without Combat Extended nothing changes. Not yet tested in game: row 380 of the test pass.

### Added

- **The missile seeker head**: the guidance section inside each Hellfire and HARM (the owner's card: intermediate parts), 10 steel, 2 components and an advanced component, at the machining table or fabrication bench after Microelectronics basics. Its picture is the owner's pick of two tries.
- **The FAB-250 and RBK-250 are built on the Mk. 82 bomb case**, as all three are 250 kg-class bombs.

### Fixed

- **With Close Air Support and without the Harrier, the A-10's Mk. 82 now costs what it does with the Harrier**: the Mk. 82 bomb case and the Mk. 82's recipe now load with either jet. Before, the A-10 alone kept its author's 60 steel, 45 chemfuel and two components.

### Save compatibility

- Safe to add to a running save. Before removing the mod, use up or sell missile seeker heads, as bomb cases.

## [0.36.0] - 2026-11-23 - Draft

Shadow and shelter from a strike

### Added

- **What stood over people when the flash came now counts** (the owner's choice of 22 November 2026: with our own radiation model, a setting; docs/development/nuclear-strike-effects.md, NK12, NK17, NK18). The light, heat and radiation arrive before the blast, so each person's cover is judged before any roof falls. **Anyone under any roof is spared burns and flash blindness**, though they still take the blast. **The dose is cut by mass**, as Glasstone and Dolan give for buildings: almost all of it in a frame house, about a sixth behind stone walls, a twentieth under a thin rock roof, almost none deep in rock, which until now kept people from any dose at all. **Flash blindness is likelier at night and lasts longer**: about two minutes by day, up to half an hour by night (US Army FM 8-9). A setting, Shadow and shelter from the strike, on by default; off, the strike treats people as before. Not yet tested in game: row 375 of the test pass.
- **The doses are told** (NK13): a letter lists each of your people the strike dosed, in words and grays, largest first, as where they stood would tell a doctor.

## [0.35.0] - 2026-11-22 - Draft

The strike's radiation, in grays

### Changed

- **With D'thorga Medical's radiation model on, the strike gives its radiation in grays to it**: 10 Gy at the fireball's edge, falling with the strike's own curve, to people in the open, and to the people aboard vehicles cut by the hull. Before, it gave Dubs Rimatomics a dose too small ever to show. Without the model, Rimatomics' dose as before. It also works without Rimatomics now, with the model. Not yet tested in game: row 362 of the test pass.

## [0.34.0] - 2026-11-22 - Draft

The Prowler's jamming blinds other factions' radar

### Added

- **The EA-6B Prowler's jamming fields blind other factions' radar and warning receivers** (with D'thorga Framework 0.225.0; the owner's choice of 22 November 2026): wherever the Prowler says it is jamming, with the radius it works out (its ICAP suite included), other factions' vehicles inside lose their radar and warning receiver as well as the turrets the Prowler already shuts down. Read through the Prowler's own public calls, never by changing it. Not yet tested in game: row 343 of the test pass.

## [0.33.0] - 2026-11-22 - Draft

The EA-6B Prowler in the fold

### Added

- **The EA-6B Prowler's fuller parts list and parts bill** (needs EA-6B Prowler Electronic Attack Aircraft; the owner's choice of 22 November 2026, the A-10's treatment): avionics, hydraulics, wiring, landing gear and fin-top receivers join its own six parts, and it is built from two turbines, four tanks, three gear legs and two generators on a shell of 600 steel, 180 plasteel, 15 components and 10 advanced components, at construction skill 10. Not yet tested in game: row 342 of the test pass.
- **Its dodge follows its flight controls and warning receiver**, and a chaff and flares button adds to it for a few seconds, as the Harrier's does.
- **The ICAP upgrades widen its jamming field** (D'thorga Vehicles' fittings): its own field, worked out by its own mod, is multiplied by the suite's figure; a jet with no suite is untouched.
- **Rearming the Prowler keeps to one kind of ammunition**, as for the Harrier and the A-10, through the same fixed fault in the fixed-wing framework.
- **Realistic handling**, a new setting on the EA-6B Prowler page, off by default (the owner's choice): the Prowler's speeds set by the real EA-6B's. Today its author already gives it the real jet's top speed with its pods and its real cruise, so it changes nothing yet; it holds those figures if a later update changes his.

## [0.32.0] - 2026-11-22 - Draft

Vehicles and the people aboard them in a nuclear strike

### Added

- **Vehicles take their own route through D'thorga's strategic strike** (the owner's report and choices of 22 November 2026: vehicles came through a strike untouched; a bus 41 cells from ground zero kept every part). The strike treated a vehicle as a body: one blow to a single spot, which armour shrugs off when the blow carries no penetration, as the strike's blast did. Now, with D'thorga Framework's vehicles (0.218.0): in the fireball a vehicle is gone with everyone aboard; in the blast zone and beyond the blast throws and wrecks vehicles by their weight and distance, so a car or a bus is scrap where a tank is only battered; heat burns tyres, belts, hoses and hydraulic lines; a running engine takes the pulse. Any faction's vehicle. A new setting, **Vehicles in the strike**, on by default. Not yet tested in game: row 332 of the test pass.
- **The people aboard share their vehicle's fate.** They are thrown about inside and bruised as their vehicle is hit, and killed in the fireball. With Dubs Rimatomics they take the radiation dose people in the open take, cut by what the hull lets through: all of it in an open vehicle, most of it in a car, about a third in a tank (the owner's choice until a radiation model of our own replaces it). Not yet tested in game: row 333 of the test pass.
- **A vehicle counts as sheltered under rock only when all of it is**, not by the one cell it stands on.

### Changed

- **The strike's log line counts vehicles**: gone, wrecked, battered, untouched and pulsed, and the people aboard killed, hurt and dosed. **"Irradiated" now counts doses given**, not attempts: before, it counted a vehicle, which can take no dose.

### Compatibility and limits

- **Needs D'thorga Framework 0.218.0 for the vehicle route**; with an older Framework, or the setting off, vehicles are struck as before.
- **Only D'thorga's own blast model** (the extinction-class warhead). IRBM's other warheads, Dubs Rimatomics' own nukes and The Dead Man's Switch's still treat vehicles their own way.
- **Shadow and the graded dose for people on foot wait for a radiation model of our own** (the owner's choice), with settings for how harsh it is.
- The figures are authored, shaped by the real tests and sources in docs/development/nuclear-strike-effects.md.

## [0.31.1] - 2026-11-22 - Draft

A vehicle saved mid-burst no longer vanishes with its crew

### Fixed

- **A vehicle saved while one of its weapons is firing a burst keeps itself and everyone aboard in the save** (the owner's Mongoose, 22 November 2026: saved mid-strike, it and its pilot and gunner were gone on loading). Vehicle Framework saves the bursts under way in a way they cannot be saved, so the save stopped part-way through that vehicle: its health, cargo, crew and passengers were never written, and on loading the game dropped the vehicle and everyone in it. Now the burst under way is left out of the save and kept in play, so the save is whole; after loading, the burst has ended and the weapon aims again at its target. Any vehicle with weapons, any faction's. A temporary patch for Vehicle Framework's fault: it switches itself off once Vehicle Framework saves them properly, and says so in the log. Not yet tested in game: row 331 of the test pass.

### Save compatibility

- **A save already cut short this way cannot be mended**: the vehicle and its crew were never written into it. Go back to an earlier save; this fix only keeps it from happening again. The load line of D'thorga Patches names the fix while it is on.

## [0.31.0] - 2026-11-22 - Draft

The UH-60M Black Hawk built from real parts

### Changed

- **The UH-60M Black Hawk is built from real parts by role** (the owner's request of 22 November 2026, with z3nt3's UH-60M Black Hawk): two turbines (RimThunder's where installed, D'thorga Framework's own otherwise), its transmission and tail drive, main and tail rotors, three servos, two hydraulic pumps, three landing wheels, two fuel cells, two generators, a battery, wiring and avionics, on a shell of 350 steel, 120 plasteel and 10 components. Before, it asked for 400 steel, 150 plasteel and 40 components, with no engine or other part. Not yet tested in game: row 313 of the test pass.

### Save compatibility

- **A Black Hawk already built keeps working**; only new builds pay the new cost. **Finish or cancel a Black Hawk frame before installing**: a frame keeps what was delivered and then asks for the new list.

## [0.30.0] - 2026-11-21 - Draft

The Harrier built from real parts, as Close Air Support's jets are

### Changed

- **The Harrier is built from real parts by role** (the owner's yes of 21 November 2026): a turbine (RimThunder's where installed, D'thorga Framework's own gas turbine assembly otherwise), four nozzles, landing gear, fuel tanks, a generator, battery and wiring, hydraulics and avionics, on a shell of 400 steel, 120 plasteel and 10 components. Before, its cost asked for car exhausts, wheels, suspension, a transmission and six wirings, and RimThunder's turbine only with RimThunder's research. The Drop Tanks upgrade takes two fuel tanks from the same table in place of car fuel tanks. Construction 10 as before. Not yet tested in game.

### Save compatibility

- **A Harrier already built keeps working**; only new builds pay the new cost. **Finish or cancel a Harrier frame before installing**: a frame keeps what was delivered and then asks for the new list.

## [0.29.1] - 2026-11-21 - Draft

Sidearms that loop: a colonist no longer fetches a weapon their loadout would only drop

### Fixed

- **A colonist no longer fetches, again and again, a sidearm their Combat Extended loadout would only put down** (the owner's report of 21 November 2026: Carlotta, then Yunxing, picked up and dropped a gun over and over; the owner's choice, "the loadout wins"). Simple Sidearms fetched a second copy of the gun in their hands that it remembered, and the loadout, which counts the gun in hand as one, dropped the copy and hauled it away. Now the copy is not fetched, and you are told once which colonist and weapon, to take it off their sidearms or allow it in their loadout. Every other sidearm is fetched as before.

### Added

- **A new setting, A colonist never fetches a sidearm their loadout would only put down again, on by default** (Mod settings, D'thorga Patches, Combat Extended page, Soldiers keep their guns). Needs Combat Extended and Simple Sidearms.

### Compatibility and limits

- Combat Extended's and Simple Sidearms' code is read by name, as before: if either changes these names, the guard stands down and the load line says so.
- Simple Sidearms looks at a colonist's remembered weapons one at a time: while the guard turns down an extra copy, a remembered weapon listed after it waits until the extra copy is taken off the list.
- Safe to add to and remove from a running save. Not yet tested in game: row 302 of the test pass.

## [0.29.0] - 2026-11-21 - Draft

Close Air Support's A-10 and Su-25: real parts, real costs, and a realistic handling setting

### Added

- **Close Air Support's A-10 and Su-25 get fuller parts lists** (the owner's choice of 21 November 2026: the Harrier's treatment): avionics, hydraulics, wiring, landing gear, wing pylons and the gun, six each beside the jets' own five. Shot-up avionics, hydraulics or wiring slow the jet and, below about 35%, keep it on the ground until repaired, as on the Harrier. Fewer than the Harrier's ten on purpose: the jets' own rule spreads hits evenly over every part, so each part added takes hits from the cockpit and engines. Neither jet has a radar, so none is added.
- **The jets are built from real parts by role**: two turbines (RimThunder's where installed, ours otherwise), fuel tanks, three legs of landing gear, two generators, a battery, wiring, two hydraulic pumps and avionics, on a shell of steel, plasteel and a few components. Before, they took Vanilla Vehicles Expanded's engine blocks, car wheels and suspension. Construction 10, as the Harrier.
- **Their bombs can be made at The Dead Man's Switch's ammunition fabricator** as well as the machining table: the light and heavy cluster bombs and the 250 kg bomb.
- **Realistic handling, a setting, off by default** (the owner's choice), on the new **Close Air Support** page: the A-10's and Su-25's top and cruising speeds set by the real aircraft's against the Harrier's, and the A-10 keeping less of its speed on one engine. Take-offs, landings, turns and attack runs are unchanged. It takes effect at the next start.

### Fixed

- **Rearming the A-10 and Su-25 keeps to one kind of ammunition**, as the Harrier's does: they rearm through the same fixed-wing framework code, which fetched any kind in stock and threw out the magazine when another arrived. With the Harrier active too, the one fix serves both.

### Dependencies

- **D'thorga Framework 0.203.0 and D'thorga Vehicles 0.91.0** for the jets' systems and dodge.

### Save compatibility

- **Safe to add.** A jet you already built gains its new parts at full health when the save loads; only new builds pay the new cost.

### Compatibility and limits

- The cluster bombs keep their author's weights under Combat Extended, lighter than real ones; real weights wait for bomb cases of their own.
- Not yet tested in game: row 299 of the test pass.

## [0.28.0] - 2026-11-20 - Draft

Mech rechargers can draw power by the size of the mech on them (off by default)

### Added

- **With Biotech, mech rechargers can draw power by the size of the mech on them: a new setting, off by default** (owner's request, 20 Nov 2026). As the game comes, a recharger draws one figure whichever mech stands on it, so a war queen costs no more to charge than a mid-sized mech, and on a compact gravship recharger almost nothing. With the setting on, a recharger draws about 285 W for each point of the mech's body size while it charges: most light mechs 200 W as now, a cleansweeper about 85 W, a tunneler about 1,000 W, a war queen or diabolus about 1,150 W. This holds on every recharger, whatever power it lists, and for other mods' mechs and rechargers too. Charging to 80% takes as long as before.
- **Charging slows over the last fifth of the battery, and the draw falls with it.** From 80% the rate falls steadily to a quarter at full, so a full charge takes about 2.3 days in place of 2, and topping up to 80% is the quick part. A mech group's recharge range, the game's own, decides where its mechs stop. Wastepacks follow the energy put in, so a full charge makes the same waste as before.
- **A charging recharger warms its room.** About a sixth of the power it draws becomes heat: a tunneler's recharger gives about as much as one heater. It stops warming a room hotter than 80 C.
- **A recharger with no mech on it draws a twentieth of its listed power**, where the game drew all of it.
- **The recharger says what it is doing**: who is charging, the watts, and about how long is left; that charging is slowing as the battery fills; or that it waits for power and how much the mech needs.
- **A mech's information card shows its recharger draw** (owner's request, 20 Nov 2026), under its mechanoid figures beside wastepacks per recharge, so you can see what a mech will cost to charge before you make one. Click it for the battery's size, the power into it, the heat, the draw when nearly full and the time from empty to full. It shows for a living mech and for a mech's kind (a gestator's recipe), and links to the two mech rechargers. Shown only while the setting is on.
- **A Mechanoids page in the settings**, shown while Biotech is active, with two settings. Mech rechargers draw power by the size of the mech: off by default. Mech battery size: Standard (the default), Large (three times the draw) or Huge (ten times); charging takes the same time at each.

### Balance

- Every figure is authored, and chosen so the common light mechs draw the 200 W the game's own recharger lists. They sit in one rules Def (PhobosMechChargingRules) that another mod can patch.

### Dependencies

- This part needs Biotech and D'thorga Framework 0.198.0 or later.

### Save compatibility

- Safe to add to a save and to remove. It saves nothing; turning the setting off puts every recharger back to the game's figure at once.

### Compatibility and limits

- **A large mech can ask more than a small grid has.** The game then switches things off at random, as it does in any power shortage: it may be the recharger, it may be a freezer. Compact gravship rechargers lose their low-power advantage while the setting is on.
- Written to work under power-saving mods. LightsOut holds an unused recharger on standby and scales what a power user reports; read from its source, it works on top of our figures and neither undoes the other. Not yet seen in game.
- Not built: mechs feeding power back to the grid.
- Not yet tested in game.

## [0.27.0] - 2026-11-20 - Draft

One settings entry for every patched mod, with search

### Changed

- **One settings entry, D'thorga Patches, with a page for each patched mod**, in place of four entries (owner's choice, 20 Nov 2026): Combat Extended, Harrier jump jet, IRBM: strikes, IRBM: after-effects, and Outposts. A page shows only while its mod is active. The missile settings, the longest list, are split into the strike itself and what it leaves behind. With D'thorga Framework 0.184.0's search box, Changed only tick and a reset for each page.
- **The Harrier disassembly trace is off unless you ask for it** (owner's choice, 20 Nov 2026: detail for chasing a fault belongs behind Detailed logs). Its own tick, Log each step when a vehicle is taken apart, on by default, is gone: the trace is now D'thorga Patches: Harrier disassembly on D'thorga Framework's Detailed logs page. **Tick it before you next try to take a Harrier apart.**

### Dependencies

- Needs D'thorga Framework 0.184.0 or later.

### Save compatibility

- Safe to add to a save. Every remaining setting keeps its saved value; the retired trace tick's is ignored.

### Compatibility and limits

- Not yet tested in game.

### Changed

- **The README now says haulers load the Harrier's chaff and flare cartridges into its dispensers**, as D'thorga Framework 0.180.0 does, not into its cargo. Text only; nothing in this mod changed.

## [0.26.0] - 2026-11-19 - Draft

Soldiers go back to their own guns instead of fighting with grenades

### Added

- **With Combat Extended, colonists left holding a grenade, a satchel charge or nothing go back to their own gun.** A colonist downed in a fight drops their gun; when they got up, a grenade from their pack went into their empty hands first, the game forgot the gun it would have sent them back for, and from then on they fought with grenades and never looked for a gun again (the owner's colony, 19 November 2026: nine soldiers holding a frag grenade or a satchel charge, eight with no gun anywhere, since at least 6 November). Now they take up a gun from their pack that can fire, else the gun they dropped when they went down, wherever it lies, even if forbidden. Drafted colonists and melee weapons are left alone.
- **When their own gun is gone, they take another of the same kind**: the nearest unforbidden one on the map with rounds in it, never a different kind. A setting.
- **With Simple sidearms, the gun in a colonist's hands takes back its place as the weapon Simple Sidearms keeps handing them**, where Simple Sidearms had recorded a grenade, or nothing. A different gun you chose there, or a weapon you forced, is left alone. A setting.
- Three settings, all on by default, under **D'thorga Patches: Combat Extended**: Colonists go back to their own guns; When their own gun is gone, take another of the same kind; Simple Sidearms keeps a colonist's gun, not a grenade, as the weapon it hands back.

### Dependencies

- This part needs D'thorga Framework 0.174.0 or later. Simple sidearms is optional.

### Save compatibility

- Safe to add to and remove from a running save. What it remembers about each colonist's gun is kept by D'thorga Framework, so removing it leaves no error.

### Compatibility and limits

- **Not yet tested in game.**
- **A colonist who already has no gun anywhere needs one handed to them once**: nothing remembers which gun was theirs.
- A colonist whose gun has no ammunition left keeps the grenade until ammunition for the gun comes back into their pack.
- If something keeps taking the gun away again, they stop trying for a day, and the log says so.

## [0.25.0] - 2026-11-19 - Draft

A save made during a scorpion raid loads with the raid working

### Fixed

- **A save made during a Vanilla Factions Expanded - Classical scorpion raid loads with the raid still working.** The game could not rebuild that raid's orders from a save, so its raiders loaded with none: they could not act or leave, pathfinding on that map stopped for everyone, and the game reported thousands of errors a minute until the log stopped recording anything (the owner's game, 19 November 2026: 15,000 errors in two and a half minutes). The raid's orders are now rebuilt as the save loads, and the raid carries on from where it was. A save that already shows the fault loads cleanly with this version. Temporary: it does nothing once the game can load Classical's raid by itself.

### Dependencies

- This part needs D'thorga Framework 0.168.0 or later.

### Compatibility and limits

- **Not yet tested in game.** Safe with a running save; nothing is saved. A new part, loaded only while Vanilla Factions Expanded - Classical is active.
- The game still reports one error for each such raid while loading, its own failed first try; a line from this mod after it says the raid was rebuilt.
- A game saved again after it loaded with the fault, before this version, has lost that raid's orders for good and is not repaired: load the save made before that.

## [0.24.0] - 2026-11-19 - Draft

A VTOL's turret saved mid-fight is drawn again after loading

### Fixed

- **A VTOL aircraft saved while one of its turrets had a target loads with that turret drawn.** While a save loads, the turret's picture was asked for too early, could not be loaded at that moment, and was then never tried again: the turret had no picture and the game reported an error every frame, thousands a minute, until the log stopped recording anything. Its picture is now loaded once the game has finished loading. Seen with the Mongoose. Temporary: it does nothing once VTOL Vehicle or Vehicle Framework stops asking for pictures while a save loads.

### Compatibility and limits

- **Not yet tested in game.** Safe with a running save; nothing is saved. A save that already shows the fault loads cleanly with this version.

## [0.23.0] - 2026-11-18 - Draft

Own pictures for the Mk. 84 fills; drone platforms say how much they hold

### Changed

- **The Mk. 84's incendiary, smoke and tox gas canisters and its EMP coil pack have their own pictures** (the owner's picks of 18 November 2026), replacing ones drawn in code.
- With D'thorga Framework 0.135.0, the Hermit Crab and the Cluster Weaver are topped up too: a mechanitor brings them steel, as
  they do to repair them. The drone platform Def is the same; only its notes changed.
- The drone platform Def names the platform's capacity and holdings, so D'thorga Framework 0.136.0's setting How full to fill
  them can fill platforms past their starting store, up to full.
- The README says which part is tested in game (the stuck vehicle fires, by the owner on 10 November 2026) and that Vehicle Framework now ships its own 1.7 build. No change in play.
- The README is cut to today's gotchas, what each part does linking the player guide's Patches chapter, the settings and links for modders. How to play, with the cost, bomb, turret hull and blast tables, moved to the guide; research, the README's modder notes and its dated checks moved to the vehicle and missile patches design records; version notes live in this changelog. No change in play.
- A code comment and the README name the design records they now live in, after the merges of 17 November 2026. No change in play.
- The comment on the Harrier's fuller parts list no longer says the airframe's weight decides which part a hit lands on: since the Harrier's build of 16 November 2026 its own rule does, and that rule shares its hits evenly among every part, ours included. No change in play; what it means for the jet is in the avionics record (L6).

### Compatibility and limits

- The new pictures change nothing in play. Drone platforms fill past their starting store only with D'thorga Framework 0.136.0 or later, by its setting How full to fill them. Safe with a running save.
- Not yet tested in game.

## [0.22.0] - 2026-11-18 - Draft

Drone platforms are kept topped up with steel

### Added

- **Colonists refill drone platforms by themselves.** The engineering drone bay, the trashpan and hound stations and the FPV nest
  of The Dead Man's Switch hold steel for their drones and only offered a right-click order to fill them. Colonists doing hauling
  work now bring the steel whenever a platform is short by one drone's worth or more. The order by hand still works. It needs
  D'thorga Framework 0.134.0, whose setting Haulers keep machines topped up switches it off.

### Dependencies

- D'thorga Framework 0.134.0 or later.

### Compatibility and limits

- Loaded only with Fortified Features Framework. It names three members of that mod's platform comp; if an update renames one,
  D'thorga Framework's load line says the top-up stands down and the platforms go back to being filled by hand.
- A platform is filled to the level its own fill order reaches (its starting store), not to its full capacity.
- A platform carried by a mechanoid is not topped up, only platforms that are buildings.
- Not yet tested in game.

## [0.21.2] - 2026-11-17 - Draft

The missile patches no longer depend on which mod is timing the load

### Fixed

- **The InterRim Ballistic Missile part finds its own patches whatever else is installed.** It asked the game which code was running to
  find them. On RimWorld 1.7 with Harmony 3.0.3.0 (17 November 2026) that answer is wrong inside a mod's start-up code that another mod
  has patched, and Loading Progress patches every mod's start-up code to time it. With both, the part could start with none of its patches
  applied and no error: the fast blast sweep, shelter and rubble among them. It now names itself instead, which cannot be answered wrongly.
  Every other part already did.

### Compatibility and limits

- Read from the code and from Harmony's own issue 52 (pardeike/HarmonyRimWorld), not seen in play: whether the part's patches were in fact
  missing in the owner's game that evening was not checked. A test now refuses the same call anywhere in our code. No settings were added
  or changed. Not yet tested in game.

### Save compatibility

- Safe to add to a running game. Nothing new is saved.

## [0.21.1] - 2026-11-17 - Draft

Asks to load after the Hind, the Mongoose and Visit Settlements

### Fixed

- The mod now asks to load after the Hind, the Mongoose and Visit Settlements, as it already did for the other mods its parts work
  with, so a mod sorter keeps its parts' assemblies after theirs. Found by a new check after D'thorga Framework's air-defence tie-in
  was rejected for loading too early. No change in play if your list was already in that order.

### Save compatibility

- Safe to add to a running game.

## [0.21.0] - 2026-11-17 - Draft

The Harrier's dodge follows its controls and warning receiver, and it can fire chaff and flares

### Added

- **The Harrier's dodge chance follows its flight controls and warning receiver** (owner, 17 Nov 2026; avionics A12, with D'thorga
  Framework 0.119.0). Sound, with a working receiver, it dodges exactly as its author set. Flight controls shot up or heavy for want of
  hydraulics cut it in proportion; with the receiver out the crew sees shots late and it dodges 0.75 as well. One postfix on the Harrier's
  own dodge sum, which both its Combat Extended path and its plain one ask; nothing of the author's is skipped.
- **A Chaff and flares button for the Harrier in the air** (owner, 17 Nov 2026; avionics A16). With countermeasure cartridges in its cargo
  and its dispensers working, a salvo uses two and adds 0.25 to its dodge chance for ten seconds, never above 0.85. The button says why
  when it cannot fire.

### Compatibility and limits

- Needs D'thorga Framework 0.119.0. Reached by name: this part does not reference the Harrier's assembly. No settings changed. Not yet
  tested in game (row 143).

### Save compatibility

- Safe to add to a running game. A salvo's few seconds are not saved.

## [0.20.0] - 2026-11-17 - Draft

Mk. 84 bombs made from fill canisters, so the ammunition fabricator finishes them

### Fixed

- **The ammunition fabricator never finished a Mk. 84 bomb** (the owner's report, 17 Nov 2026: its loads kept being dropped, and no
  incendiary bomb had been made). Under Combat Extended a Mk. 84 took 14 to 20 hauling trips in one bill, and The Dead Man's Switch's
  fabricator drops everything it has loaded whenever a haul is interrupted, which in a busy colony always happened. The owner's save
  showed about 690 chemfuel and 50 prometheum dropped beside the fabricators and one incendiary bomb in the whole colony.

### Added

- **Bomb fills** (the owner's choice): incendiary bomb fill (5 a bomb, each 20 prometheum and 300 chemfuel), EMP bomb coil pack (8 a
  bomb, each 105 steel), smoke bomb fill (8 a bomb, each 95 steel and 125 chemfuel) and tox bomb fill (8 a bomb, each 9 wastepacks, with
  Biotech). Each is a short bill at the machining table or the ammunition fabricator; the bomb is then a case, its fills and the
  components, three trips for a hauler. Incendiary and smoke fills burn, and a store of them can go up in a fire.
- Stand-in pictures for the four, drawn in code; generated art waits for the owner's go.

### Balance

- The materials in each bomb are the same as before. Each fill adds 500 work, Crafting 5, to the bomb's own 6,000 (authored).

### Compatibility and limits

- Combat Extended only, as the bomb costs are. A fill exists only where its bomb does. No settings changed. Not yet tested in game.

### Save compatibility

- Safe to add to a running game. Bills for the bombs stay; they now ask for fills, so add bills for the fills too. Chemfuel and
  prometheum the fabricators dropped are ordinary items and can be used.

## [0.19.3] - 2026-11-16 - Draft

RimPacts' allies leave empty vehicles alone

### Fixed

- **With RimPacts, an ally's assault no longer spends hours beating on an empty vehicle.** RimPacts counted a vehicle with nobody aboard
  as a standing defender, so allies fought it by hand (the owner's save PG8: 11 of 19 soldiers on one empty Highwayman, kicking and
  clubbing it for over two hours) while its general assault waited. They now go for the people and turrets, and the empty vehicle is
  left standing to claim after the battle. A crewed vehicle is still attacked. Temporary: it uses RimPacts' own test for an unmanned
  vehicle, and changes nothing once RimPacts leaves empty vehicles out itself.

### Compatibility and limits

- One Harmony postfix on RimPacts, by name; if RimPacts' code changes, it stays off and the load line says so. No settings were added
  or changed. Not yet tested in game.

### Save compatibility

- Safe to add to and remove from a running game. Nothing is saved.

## [0.19.2] - 2026-11-16 - Draft

The Prison Labor guard comes out: Prison Labor fixed the crash itself

### Fixed

- **Removed the temporary Prison Labor guard** (from 0.16.5). Prison Labor 1.7.1 (13 November 2026) no longer calls back into the method it patches when a warden considers feeding a motivated prisoner, which is what closed the game, so the guard had nothing left to do. Keep Prison Labor up to date: an older Prison Labor build without the guard can close the game again.

### Compatibility and limits

- No settings were added or changed.

### Save compatibility

- Safe to install on a running game. The guard saved nothing.

## [0.19.1] - 2026-11-13 - Draft

The missile mod's close-in weapon systems see Combat Extended's shells

### Fixed

- **InterRim Ballistic Missile's close-in weapon systems now shoot down Combat Extended's mortar and artillery shells.** CE's shells fly as a different kind of projectile, which the missile mod's systems never looked at, so they did nothing against them. They now take aim at incoming enemy shells headed for your things by the missile mod's own rules for any shell, and an interception destroys the shell. A new setting turns it off. Not yet tested in game.

### Compatibility and limits

- Only with both InterRim Ballistic Missile and Combat Extended active; Combat Extended is read by name. Temporary: it can go if the missile mod learns Combat Extended's shells itself.

## [0.19.0] - 2026-11-13 - Draft

The Harrier's flight-control rule moves into D'thorga Framework, for every aircraft

### Changed

- **The Harrier's "no take-off with damaged flight controls" rule is now D'thorga Framework's** (0.106.0), one rule for every aircraft our systems run, at the same 35 %. This mod's own copy is removed: it reached any vehicle with a flight-control part while the Harrier mod was active, and so could ground the Frog, Toad, Smuggler, Warbird, Cherokee and Goliath. The Harrier still cannot take off with its avionics, hydraulics or wiring below 35 %, and now also flies slower while they are damaged. Not yet tested in game.

### Fixed

- The README's health figures for the Harrier's own parts are corrected from the jet's current definition: cockpit 170, engine 200, wing fuel tanks 120 (the airframe 470 was right).

### Compatibility and limits

- **With D'thorga Framework's vehicle systems switched off for the Harrier, nothing grounds it for damaged flight controls any more**: the rule lives with the systems.

## [0.18.0] - 2026-11-12 - Draft

A temporary trace to find why the Harrier cannot be taken apart. **Not yet tested in game.**

### Added

- **A trace of each step when a vehicle is taken apart** (owner's choice, 12 November 2026). The Harrier cannot be disassembled: the work stops near the end and the jet stays. While the new setting is on, a colonist's disassembly (or god mode's) writes one log line per step of the removal: the work finishing, the fixed-wing framework's clean-up, Vehicle Framework's removal, each part of the vehicle that cleans itself up, the vehicle leaving the map, and how the job ended. Where a step fails, the full error is logged with the last step reached. It does nothing at other times, changes nothing in play, and never hides an error. A temporary part: it comes out once the cause is found.
- **New setting, Finding faults (temporary): Log each step when a vehicle is taken apart.** On by default, in D'thorga Patches: Harrier's settings.

### Changed

- The README no longer calls the bomb cases' picture a stand-in. Documentation only; nothing changes in play.

### Compatibility and limits

- What the code suggests, not yet seen in play: Vehicle Framework clears the vehicle's picture before its parts clean themselves up, so a failure in between leaves the jet on the map with no picture, and the game's background drawing then fails to rebuild it. That matches the error the owner saw, and the trace will say which step fails.
- **Try once, on a copy of the save.** Each attempt runs the first half of the removal again.

### Save compatibility

- Safe to add or remove mid-game. Only the setting is saved.

## [0.17.0] - 2026-11-12 - Draft

Progress bars on Vehicle Framework's own vehicle work. **Not yet tested in game.**

### Added

- **Vehicle Framework's repairs and upgrades show the game's progress bar.** Vehicle Framework draws the bar only for work with no sparks, and every vehicle's work has sparks, so it never showed; the sparks stay and the bar now shows beside them (owner, 12 November 2026). A temporary patch: it comes out when Vehicle Framework shows the bar itself.

### Compatibility and limits

- D'thorga Framework's own vehicle work draws its own bar and is left alone, so no job shows two.

### Save compatibility

- Safe to add or remove mid-game. Nothing is saved.

## [0.16.9] - 2026-11-12 - Draft

The Harrier's empty Mk. 82 and Mk. 84 bomb cases have their own pictures, chosen by the owner on 12 November 2026. **Not yet tested in game.**

### Changed

- **New pictures for the Mk. 82 and Mk. 84 bomb cases**: empty olive-drab bomb bodies with their lugs, the Mk. 84 visibly fatter. They replace drawings made in code. Nothing changes in play.
- The Workshop preview is a new picture, a jump jet on a frontier airstrip at dusk, with the name added by script, from the owner's picks of 10 November 2026. No change in play.
- The mod's description and README now carry the spelling dthorga beside D'thorga, so a search without the apostrophe finds it. The description says it is community-created by House D'thorga.

## [0.16.8] - 2026-11-11 - Draft

From the code audit of 11 November 2026. **Not yet tested in game.**

### Fixed

- **Removing IRBM or RimPacts later no longer leaves an error in the save.** The IRBM part's memory of struck towns and the RimPacts part's quest check were game components in assemblies that load only with those mods, so a save that outlived either mod would log Could not find class on every load. Both now live in D'thorga Framework's part store. On the first load after this update the old components hand over what they held and retire, so the next save no longer names them.

### Dependencies

- Needs D'thorga Framework 0.97.0 or later.

### Save compatibility

- Safe to update mid-game. A strike remembered before the update is carried over.

## [0.16.7] - 2026-11-10 - Draft

Temporary, at the owner's request. **Not yet tested in game.**

### Fixed

- With Combat Extended, an ammo set entry naming ammo that does not exist no longer stops the game's whole ammo setup at startup, which left ammo recipes missing from machines. Such entries are dropped first, and the log says how many and from where. The Dead Man's Switch - Synthriada's Combat Extended patch adds some.

### Compatibility and limits

- With no such entries the guard does nothing, so it stands down by itself. Synthriada's Embryte ammo stays unavailable until that mod defines it.

## [0.16.6] - 2026-11-10 - Draft

From the owner's Harrier, listed as burning 37 times over for three days after a sortie (10 November 2026). Vehicle Framework's own bug, patched at the owner's request until its author handles it. **Tested in game by the owner, 10 November 2026: the stuck fires went out.**

### Fixed

- With Vehicle Framework, a vehicle no longer keeps fires it could never put out: a vehicle leaving a map takes no fire with it, and a fire on another map than its vehicle, or any fire after the first, is put out within a few seconds, with a message. A burning vehicle could never be auto-refuelled before.

### Save compatibility

- Safe on an existing save: the stuck fires go at the first check after loading.

## [0.16.5] - 2026-11-10 - Draft

Temporary, at the owner's request, until the author of Prison Labor ships a fix. **Tested in game by the owner, 10 November 2026: PG5 ran past the point where it had crashed every time.**

### Fixed

- With Prison Labor's first 1.7 build, the game could close without warning when a warden considered feeding a motivated prisoner, because one of its food patches called itself for ever. A guard now stops that patch after one pass and changes nothing else.

### Compatibility and limits

- The guard reads Prison Labor's code at each start and stands down by itself once the fault is gone, saying so on the load line. It holds no settings and nothing in a save, so it is safe to remove with the mod or on its own.

## [0.16.4] - 2026-11-09 - Draft

### Fixed

- Rearming the Hind and the Mongoose now keeps to one kind of ammunition, as the Harrier does since 0.16.3: they have the same fault, where different kinds brought by different colonists threw out one another's magazines. The fix now loads whenever any of the three is active. Not yet tested in game.

## [0.16.3] - 2026-11-09 - Draft

### Fixed

- Rearming the Harrier no longer calls out every colonist with the ammunition counter going up and down. With more than one kind of rocket in store, colonists brought different kinds and each kind threw out the one before. A magazine is now topped up only with what is already in it, or with the first kind on its way when it is empty. Choosing a kind on the weapon still swaps it. Not yet tested in game.

### Dependencies

- Needs D'thorga Framework 0.46.1.

## [0.16.2] - 2026-11-09 - Draft

### Changed

- Someone asleep in a casket at ground zero now dies with the casket when rubble is on, as they already did without it, and as everyone else at ground zero does (owner's decision). What else the building held is treated as before. **Not yet tested in game.**

## [0.16.1] - 2026-11-09 - Draft

### Fixed

- A strike reaching several maps could hold the white screen for as long as the other maps took: the target's own work now runs first, and it is what lifts the screen. **Not yet tested in game.**
- The RimPacts assault note no longer keeps a town from the previous game in memory after a load. **Not yet tested in game.**

### Compatibility and limits

- With rubble on, someone asleep in a casket at ground zero survives the strike, downed beside the crater. Left as it is until the owner decides.

### Changed

- Dates in this mod's records that were written a day ahead now read 8 November 2026. Text only; nothing in game changes.

## [0.16.0] - 2026-11-08 - Draft

The shared logger (owner, 8 November 2026). **Not yet tested in game.**

### Changed

- The Harrier, IRBM, RimPacts and Visit Settlements parts write under their own categories in Player.log, within one budget of lines for the mod, so a repeating fault of ours can never fill the game's log. What they say is unchanged.

### Dependencies

- The Harrier part needs D'thorga Framework 0.33.0.

## [0.15.0] - 2026-11-08 - Draft

Outposts and caravans with vehicles, at the owner's request (8 November 2026). **Not yet tested in game.**

### Added

- A setting, D'thorga Patches: outposts, on by default, with the reason for it in the settings window. Turning it off puts the original behaviour back, with no restart.

### Fixed

- With Vanilla Expanded Framework and Vehicle Framework, the create outpost window refuses a caravan that holds a vehicle and says why. Read from the outposts code and not reproduced in game: founding that way can throw an error part way through, leaving an empty outpost that is abandoned at once, and can lose the vehicles and anyone riding in them.

### Compatibility and limits

- Loads only while Vanilla Expanded Framework and Vehicle Framework are both active. The window's other reasons for refusing are unchanged.

### Save compatibility

- Safe to add to and remove from a save: it saves nothing in the game, and only changes what the window allows.

## [0.14.2] - 2026-11-08 - Draft

The Harrier's bomb cases had names their IDs could not survive, found in the owner's log (8 November 2026). **Not yet tested in game.**

### Fixed

- With the Harrier and Combat Extended, every save and load made a bomb case's ID longer, until it overflowed and two cases could share an ID, with errors at each load. The game reads an item's ID from the digits at the end of its name and number, and the cases' names ended in a digit (Mk. 82, Mk. 84). The cases have new internal names and their IDs now stay as they are. What players see is unchanged.

### Dependencies

- The Harrier part needs D'thorga Framework 0.30.0.

### Save compatibility

- Older saves carry over on load: the cases themselves, their bills, storage filters, factory hoppers, and jobs and haul orders that refer to them. The internal names change at the next save, so a save written after this version loads only with it or later.
- A case whose ID had already overflowed in an older save gives one last error on loading that save. Two such cases then keep the same ID until one is used up; the newest saves have none.

## [0.14.1] - 2026-11-08 - Draft

A fix for Visit Settlements' settlement events, found in the owner's log (8 November 2026). **Not yet tested in game.**

### Fixed

- With Visit Settlements, a friendly settlement holding a pawn that does not eat (a mechanoid, a robot, an android) no longer makes its food drops throw an error on every tick. The food sum counted every friendly pawn's food need, and such a pawn has none; the error came before the day counter was reset, so it repeated every tick: 6,846 errors in six minutes on the owner's colony, then 8,247 the next session, each time enough to reach the game's 10,000-message limit and stop every mod's logging for the rest of the session. A pawn that does not eat now needs no food, and the drops go ahead for the others.
- If anything else ever fails in those events, they now wait their usual number of days before trying again instead of failing on every tick, and the log says so at most once an in-game day.

### Compatibility and limits

- Found by name and shape in Visit Settlements' code (version of 22 October 2026); if that code changes, the load line says which part is off and why. Nothing is saved; safe to add and remove.

### Dependencies

- Needs D'thorga Framework 0.28.0 or later (its once-a-day throttle).

## [0.14.0] - 2026-11-08 - Draft

The owner's choice of 8 November 2026 for a jump jet too heavy or too hot to hover: a setting, defaulting to the runway. **Not yet
tested in game.**

### Added

- The Harrier's pad landings follow D'thorga Framework's hover check: when its engine cannot hold the jet in a hover at its weight in the day's heat, and the player chose to refuse the pad, the vertical-landing button is disabled with the reason, the order is refused with it, an automatic return uses a runway, and a landing whose hover fails part-way is called off with the reason instead of the Harrier's damaged-engine text.

### Dependencies

- The Harrier part needs D'thorga Framework 0.11.0 or later. D'thorga Vehicles 0.2.0 gives the Harrier its real engine figures.

### Compatibility and limits

- Patches four of the Harrier's own methods by name (its hover check, the vertical-landing order, its buttons and its cancel). The order's patch skips the original only when it would refuse with the wrong reason.

## [0.13.1] - 2026-11-08 - Draft

No change in play on 1.7. Not yet tested in game.

### Changed

- A saved Harrier now gains its new parts through D'thorga Framework's vehicle systems, which do the same for every vehicle, instead of a copy of its own. The parts and their health are as before.

### Dependencies

- The Harrier part needs D'thorga Framework 0.10.0 or later.

### Compatibility and limits

- RimWorld 1.7 only: the 1.6 build, its load folder and 1.6 in the About file are gone (owner, 8 November 2026: 1.6 is a dying version). Players on 1.6 keep the last version that had it.

## [0.13.0] - 2026-11-08 - Draft

The owner's report of 8 November 2026: a RimPacts defence of Barracuda's Stream was lost every time, about a quarter of an hour into the fight, and the tank, its four colonists and three mechs never came home. Visit Settlements had closed the battle map under them. Not yet tested in game.

### Fixed

- Visit Settlements: a visited settlement's map no longer closes when a pawn goes down while your people can still act there. Visit Settlements closed it if no colonist of yours stood on the map, so colonists inside a vehicle were lost with it, and with RimPacts the defence or battle there was lost too. The map now stays open while a colonist of yours there is conscious (on foot or aboard a vehicle, shuttle or pod), while a mechanoid of yours there is on its feet, or while a RimPacts defence or battle is fought there. Otherwise Visit Settlements' rule stands. Each save of a map is noted in the log once an in-game hour.

### Save compatibility

- Saves nothing; safe to add to and remove from a save. Pawns already lost this way are not brought back; load a save from before the map closed.

### Compatibility and limits

- Visit Settlements, RimPacts and Vehicle Framework are read by name; no assembly of theirs is referenced. If Visit Settlements changes the method this patches, the part stays off and the load line says why. The patch skips Visit Settlements' map closing only in the cases above; the reason is recorded in the code.

## [0.12.0] - 2026-11-07 - Draft

The owner's request of 7 November 2026: RimPacts' world war quest gave no sign of its deadline ("bring it in line with the game's vanilla style"). Not yet tested in game.

### Added

- RimPacts: a world war quest shows its countdown the way the game's own quests do, a red time left in the quest list and "Expires in ..." on the quest, with the date on hover, in the game's own words for every language. It counts to RimPacts' daily check that actually fails the war, which can be up to a day after the quest's 3 days. A quest already running picks it up within a few seconds of loading. Nothing RimPacts decides changes.

### Save compatibility

- The countdown is a standard game quest part added to RimPacts' quest and saved with it; removing this mod leaves it to finish harmlessly. A new game component holds nothing; removing the mod leaves one harmless error line about it on the next load.

### Compatibility and limits

- RimPacts is read by name. If a RimPacts update renames what is read, the countdown stays off and the load line says why.

## [0.11.1] - 2026-11-07 - Draft

The owner's report of 7 November 2026 (late evening): after a nuke on Lance Village the ally's assault still arrived with two soldiers, and about twelve towns changed hands. Read from the owner's two saves, their log and RimPacts 1.7's code; not yet tested in game.

### Fixed

- InterRim Ballistic Missile, with RimPacts: the 0.11.0 change that should have let an ally's assault on a struck town face its pre-strike garrison did not work. It raised only RimPacts' final trim, but RimPacts had already capped the squad's generation, and its top-up, to the defenders the strike left standing (two at Lance Village, against 39 before the strike). RimPacts' defender count itself is now raised to the pre-strike garrison while it raises its assault, so the generation cap, the top-up and the trim all use it, tech factor included. Only that assault, on that battle's map and for its attacker; defend waves, support forces and RimPacts' arrival check keep its own count, and a town with nobody standing still falls to the attack.

### Changed

- A strike made before 0.11.0 no longer leaves the next assault untrimmed: it has no garrison count, so RimPacts' own squad stands. A strike made with 0.11.0 or later keeps its count.

### Compatibility and limits

- The twelve towns: RimPacts' world war attack has a deadline of 3 days from accepting it, checked once a day. The Lance Village battle opened after the deadline had passed, and the next daily check failed the war and moved control of 11 border towns to the enemy. This is RimPacts' rule; nothing in this mod caused it, and nothing changes it. Strike and attack early.
- RimPacts counts only the attacking faction and, when the player joins the attack, the player's pawns as attackers. Allied factions and mercenaries help fight but do not keep a battle going.

## [0.11.0] - 2026-11-07 - Draft

The owner's reports of 7 November 2026 (evening): the Harrier could not fly sorties while a RimPacts client state was attacking, and the owner asked for a far more detailed parts list. Plus a fault found in the owner's log. Read from the code; not yet tested in game.

### Added

- Harrier: ten more parts beside its own four, fourteen in all: canopy, avionics, nose radar, intakes and compressor, thrust nozzles and exhaust, hydraulics, wiring and electrics, alternator, landing gear and wing pylons, each with its own health, cells and armour. The engine now overrides the other parts when it fails. Every figure authored. Owner's request.
- Harrier: flight controls that matter. Avionics, hydraulics and wiring carry the framework's flight-control figure, which nothing read before; below 35% the jet cannot take off, and the launch and sortie buttons say which parts to repair. The floor is authored.
- Harrier: the plasteel armour upgrade covers the nozzles, landing gear, pylons and intakes too, at a quarter of their armour, under Combat Extended at its scale.
- Harrier, with RimPacts: a sortie clicked on a battle the player has not joined now says why it is refused (RimPacts' own landing gate, which the fixed-wing framework turned into a silent rejection) instead of doing nothing.
- InterRim Ballistic Missile, with RimPacts: an ally's assault on a town hit by a strategic strike faces the garrison the town had before the strike, not the few defenders the missile left (owner's report and request: after a nuke, the ally sent about two soldiers and the battle was declared lost). The strike counts the town's standing defenders before doing any harm and the count is saved; RimPacts' assault trim is raised to it, tech factor included. A town struck before this version, found from the strike letter within 15 days, is not trimmed at all. Only RimPacts' assault squad changes, never its defend waves or support forces, and a squad is never bigger than RimPacts raised it.
- A setting, off by default, that lets aircraft fly a sortie over a battle not yet joined, with a warning that RimPacts then counts the crew as the player's forces there. Owner's report; the rule itself is RimPacts'.

### Fixed

- InterRim Ballistic Missile: after a strike, loading another game could leave a finished strike job throwing an error on every frame (7,662 lines in the owner's log of 7 November 2026): the job named its map in a log line after that map was gone. The place is now read once while the map is alive, a job whose map is gone is dropped, and D'thorga Framework 0.8.1 empties its queue even when a drop fails.

### Save compatibility

- With InterRim Ballistic Missile, the world keeps a small record of struck towns and their garrisons (saved, stable keys). Removing the mod leaves one harmless error line about it on the next load. (0.11.1 no longer saves the one-off letter-reading flag; a save that has it loads without complaint.)
- A Harrier saved with four parts gains the ten new ones at full health when the save loads; its own four keep their health. Parts are only ever appended to the list, never moved or removed, which is what keeps old saves right.

### Dependencies

- D'thorga Framework 0.8.1 or later.

### Compatibility and limits

- The flight-control rule applies to any vehicle whose design gives a part the flight-control figure; in the owner's list that is the Harrier alone.
- RimPacts' gate is read by name at runtime; without RimPacts nothing changes.

## [0.10.0] - 2026-11-07 - Draft

Owner's request: a strategic strike left about eighty "Area revealed" letters that could not be cleared quickly. Read from the game's code; not yet tested in game.

### Added

- A strategic blast sends no "Area revealed" letters while it runs, on the map it is striking. The game sent one for every sealed cave, ruin and vault the blast broke open; the blast leaves nothing in them. The fog clears as before, and reveals at other times send their letters as before. A setting, on by default.
- Dev action: dismiss every "Area revealed" letter, for those a strike sent before this version.

### Compatibility and limits

- The warning about mechanoids found inside an opened pocket shares the letter and is silenced with it during a blast. Covers both of this mod's blast paths and the missile mod's own one-moment pass when spreading is switched off.

## [0.9.0] - 2026-11-07 - Draft

Stage 2 of the owner-approved plan: a strike model of our own for the extinction-class warhead, after Dubs Rimatomics' nuke. Read from the mods' code; not yet tested in game.

### Added

- The extinction-class warhead strikes with a fireball, a blast zone and a heat zone around the centre of the struck map. In the fireball everything is gone and the ground is scoured, ashen and polluted; in the blast zone buildings, items and plants are gone and people take the blast and burns by distance; in the heat zone walls may stand, plants wilt, people burn and fires start, fewer towards the edge.
- Burns are spread over the skin in several hits, so D'thorga Medical's burn load and shock see a burned body.
- An electromagnetic pulse stops powered buildings and mechanoids still standing in the open for a few hours.
- With Dubs Rimatomics: its radioactive fallout on each struck map for 10 to 30 days, up to six of its fallout clouds drifting across the world, radiation sickness for survivors in the open by distance, and flash blindness by chance. Without it, the missile mod's nuclear winter as before. No fallout in space.
- Other settlements within the warhead's reach get the heat, fires and fallout, not the fireball and blast, unless the player chooses a full strike everywhere.
- The screen stays white until the fireball and blast are done, at most 8 seconds.
- Settings: the blast model, a full strike everywhere, fires, fallout, fallout clouds, radiation, ground pollution and the pulse, each with its cost shown when it has one.
- Dev action: strike the current map with the blast model.

### Changed

- For the extinction-class warhead, the missile mod's own fires on a fifth of every cell in one moment are replaced by the fires of the heat zone, spread over moments. Its letter, flash, sound, cloud and world effects are unchanged. The other strategic warheads keep the missile mod's effects, run by the fast sweep.

### Balance

- Zones of 25 and 110 cells and a reach of 250, with Dubs Rimatomics' falloff curve: about 576 damage at the fireball's edge and 205 at the blast zone's edge. Burns a quarter of the falloff, heat-zone blast 0.3 of it, fire chances 0.02, 0.08 and 0.3 by setting, pulse 400 EMP. All authored, tunable in XML, for the owner's review.

### Compatibility and limits

- Dubs Rimatomics is optional and read by name only; Rimatomics++ keeps the names used. Ground zero is the map's centre, since the missile mod has no impact point on the map. A strike in progress is not saved.

## [0.8.0] - 2026-11-07 - Draft

Owner's request: a strategic strike still took far too long after 0.7.0 (in the owner's game, 18 minutes and unfinished). Stage 1 of the owner-approved plan; read from the mods' code, not yet tested in game.

### Changed

- A strategic blast now ends in seconds rather than minutes. What the warhead's damage would destroy vanishes, as in Dubs Rimatomics' nuke, instead of going through the full damage path with its rubble, filth, effects and every mod's damage handling. People still take the damage and die properly; what the damage could not destroy is left to the missile mod's rule.
- Built roofs are torn off in one pass before any wall falls, so falling walls no longer check the whole connected roof for collapse.
- Overhead mountain shelters what is under it, thin or thick, and those cells are never swept. The missile mod sheltered only under thick overhead mountain; that rule is one of the setting's choices.
- While the screen is white or fading back, a blast may take up to 40 ms a frame.
- A container's contents vanish with it; a living sleeper inside dies first.
- Repeat strikes of the same warhead queued behind one another fold into passes of one sweep.
- The blast runs through D'thorga Framework's shared work queue instead of a patch of its own on the game's frame update.
- The finished-blast log line counts what was vaporised, wrecked, damaged, sheltered and already gone, and the frames and time taken.

### Added

- Settings: what shelters (any overhead mountain, only thick, nothing), rubble and salvage, the dead (vaporised or left), the larger budget while the screen is white, and the messages. Each choice that costs speed shows its cost in amber.
- With Combat Extended: a setting for wall fragments during a strategic blast, none by default, a tenth, or as in a fight. Combat Extended's own setting is changed only in memory for the moments a blast runs.
- Dev action: count Combat Extended fragments on the map.

### Fixed

- The likely cause of the crawl: Combat Extended threw about a hundred wall fragments for every building, and while the game is paused they never flew away, so each slowed every later removal. Read in Combat Extended's code with the owner's settings, not measured.

### Dependencies

- D'thorga Framework 0.7.0, for its work queue.

### Compatibility and limits

- Not yet tested in game. A blast in progress is not saved. Without the rubble setting, buildings leave nothing behind.

## [0.7.0] - 2026-11-07 - Draft

Owner's decision (7 November 2026): fewer, broader mods before anything is published. D'thorga Vehicle Patches and D'thorga Missile Patches become one mod, D'thorga Patches. The version continues from Vehicle Patches, with a middle-number step because the missile fix is new to it. Not yet tested in game as D'thorga Patches.

### Changed

- One mod, D'thorga Patches (package ID PhobosDthorga.Patches), holds every patch for another author's mod. Each part still loads only while its mod is active.
- The Harrier part now logs a load line of its own, naming what is on.
- The missile part's settings window has a heading for InterRim Ballistic Missile, and both parts log under one name.

### Dependencies

- D'thorga Framework is now required, as for every D'thorga mod with code. Harmony was already needed by both parts.

### Save compatibility

- Neither old mod saved anything, so a save made with them loads with this one. Switch both old mods off when switching this on: the About file lists them as incompatible, so RimSort warns if one is left on.
- The missile part's settings are read once from D'thorga Missile Patches' settings file.

### Compatibility and limits

- The missile fix is unchanged in behaviour: in the owner's first run it kept the game from freezing, but a blast on an enemy town was still running after 14 minutes. Work on it waits for the owner's proposals.
- The Workshop preview keeps the owner's picked background, with the new name added by script.

## [0.6.0] - 2026-11-07 - Draft

Owner's request: the Harrier's bombs no longer take RimThunder's heavy castings, which clogged the automated machining bays. The owner chose a stackable bomb case of our own, with a stand-in picture until generated art is picked. Not yet tested in game.

### Added

- Two new items under Combat Extended: the Mk. 82 bomb case and the Mk. 84 bomb case, the empty steel bodies of the bombs. Each bomb now takes one. They are made at the machining table, at the rotary forger from The Dead Man's Switch - Expedition, and at Vanilla Furniture Expanded - Factory's automated machining bay, which gives itself a bill for each.
- Their pictures are stand-ins, drawn by hand in code (agent-made, held for the owner's review); generated art is to follow on the owner's go.
- A Workshop preview picture: a generated background the owner picked (7 November 2026), with the name added by script. No change in play.

### Changed

- Every Mk. 82 and Mk. 84 takes one bomb case in place of 2 or 6 heavy castings. The bombs no longer depend on RimThunder: with or without it, the cost is the same.

### Fixed

- The automated machining bays no longer clog on bomb ingredients. Heavy castings don't stack, so each took a whole hopper slot: a deep hopper held only enough for two Mk. 82s, and haulers carried them one at a time. The cases stack to 4.

### Balance

- Mk. 82 case: 280 steel, 140 kg, 1,000 work, Crafting 6. Mk. 84 case: 956 steel, 478 kg, 3,000 work, Crafting 7. Real case weights, so the steel balances; the same work as the castings they replace. No research of their own. Stack size 4, the skill levels and the work are agent choices for the owner to review.
- Materials now weigh 166 kg for the Mk. 82 and 604 to 911 kg for the Mk. 84s. A Mk. 82 case costs 280 steel, against 200 steel for its two castings before.

### Save compatibility

- Before updating, delete every Mk. 82 and Mk. 84 bill on the automated machining bays, then add them again afterwards. A bay keeps its bill's old ingredient list in the save, and once steel or a case reaches its hoppers it would throw an error every few seconds (read in Pipe System's code, not seen in game). Deleting a half-filled bill drops what it had taken beside the bay.
- Bills at the machining table and the ammunition fabricator change in place. Heavy castings already made still serve RimThunder's vehicles, or smelt back to steel.
- Before removing this mod with Combat Extended active, use up or sell the bomb cases and delete their bills: they are this mod's items.

### Compatibility and limits

- Hoppers set to take heavy castings for the bays must be set to take the two cases instead.
- README: supported game versions, where its Harmony comes from, and the turret hull step before removal, in the gotchas.

## [0.5.0] - 2026-11-06 - Draft

Owner's request: heavy castings at the rotary forger in batches, so loading the machine is worth the trip. Not yet tested in game.

### Changed

- The rotary forger now makes RimThunder's heavy casting 12 at a time, in place of one. Its bill is called make 12 heavy castings. The electric smelter and assembly platform still make one at a time.
- The assembly now carries the mod's version number. It had said 0.3.2 since that release. No behaviour change.

### Balance

- 1,200 steel makes 12 castings and 12 lumps of steel slag, RimThunder's own recipe times 12, with 12 times its work: about 3,000 ticks on the machine. The batch size is the owner's choice.
- The colonist's fixed load and collect waits, about 600 ticks, are now shared by 12 castings, so the forger saves colonist time over the smelter. Before, it cost slightly more.

### Save compatibility

- Safe to add to a running save. A one-at-a-time heavy casting bill set up at a forger under 0.3.0 or 0.4.0 may keep running: delete it and add the new one.
- Before removing this mod, delete any heavy casting bills at a forger, as before.

### Compatibility and limits

- The 12 castings and 12 slag lumps drop as 24 separate things beside the forger, since neither stacks. Keep the floor around it clear.
- The bill waits until 1,200 steel can be fetched; loading it takes about 16 trips, with steel in stacks of 75.

## [0.4.0] - 2026-11-05 - Draft

Combat Extended support for Spaceship Turret Hull by MK. Not yet tested in game.

### Added

- With Spaceship Turret Hull and Combat Extended both active, all seven armed hulls load and fire Combat Extended ammunition like its own turrets. Three of the guns (shotgun, sniper, incendiary) gain Combat Extended versions; the calibres and magazines are agent-chosen values for the owner to review.

### Changed

- Under Combat Extended the hulls no longer explode when destroyed, and use its aim and cooldown figures for the nearest turret of each kind. Their fixed range ring is gone, since the turret now draws its real range.
- Under Combat Extended the uranium slug hull is labelled the 40mm flak hull, because that is the gun it carries there.
- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

### Balance

- Build costs, work, skill, hit points and power stay as the hull mod sets them, so these hulls cost far less than Combat Extended's own turrets. Left for the owner to decide.

### Save compatibility

- Safe to add to a running save. Hulls already built still can't load ammo, because a built thing loads as the type written in the save. Deconstruct and rebuild them, while landed. Nothing is moved or deleted for you.
- Before removing this mod with Combat Extended still active, deconstruct the hulls built under it. Not yet tested either way.

## [0.3.2] - 2026-11-04 - Draft

A native RimWorld 1.7 build, which now leads (owner, 4 November 2026); the 1.6 build stays as the minimum backwards support
until 1.6 is retired. No behaviour change. Not yet tested in game on either version since this build.

### Changed

- The assembly is built twice, against the 1.7.4998 reference on .NET 10 with Harmony 3 and against the 1.6 reference as before;
  the game loads its own version's folder (Mods/HarrierVtol/1.7 or Mods/HarrierVtol/1.6). Until now 1.7 loaded the 1.6 build.

### Compatibility and limits

- Vehicle Framework ships only 1.6, so both builds reference that assembly, as the game itself loads it on 1.7. Harmony 3 accepted the patches at build time; whether they still apply in play is unchecked.

## [0.3.1] - 2026-11-03 - Draft

Owner's request: a review of the Harrier's code for sortie, RimWorld 1.7 and Combat Extended faults. Not yet tested in game.

### Fixed

- A sortie's map could close under the jet. The game removes a site's map once no colonist is left on it, and its check can't see a
  pilot inside a vehicle. Vehicle Framework and the Harrier's flight framework each cover part of that gap, but the framework covers
  only the game's own kinds of site. Over a Real Ruins site, a RimMarket convoy battle or any other mod's site with its own rule, the map
  could be removed with the jet still in the air. Now any vehicle of yours with a colonist aboard keeps the map open, in the air or on
  the ground, whichever mod made the map.

### Compatibility and limits

- The fix lives in the Harrier's folder, so it runs only while the Harrier is active, but it covers every Vehicle Framework vehicle of
  yours, the Hind and Mongoose included.
- Checked but not changed: every game, Vehicle Framework, Harmony and Unity member the Harrier's three assemblies call exists in
  RimWorld 1.7 (by name and parameter count), and the Combat Extended members its code reaches by name exist in the current CE build.
  The review's record is in docs/development/vehicle-patches.md.

## [0.3.0] - 2026-11-03 - Draft

Owner's request. Not yet tested in game.

### Added

- RimThunder's heavy casting (its in-game name is heavy compoment) can now be made at the rotary forger from The Dead Man's Switch - Expedition, as
  well as at the electric smelter and assembly platform. It costs the same 100 steel and still gives a lump of steel slag.

### Dependencies

- None added. It switches on only while RimThunder is active and the forger exists.

### Save compatibility

- Safe to add. Before removing this mod, delete any heavy casting bills you added at a forger, because the forger may no longer list that recipe.

### Compatibility and limits

- The forger saves no colonist time on this recipe. Loading and collecting take a colonist about 600 ticks in all, against about 500 at the smelter, because the recipe is so short.
- Without RimThunder's own parts module, which Aoba.Motorization.Engine replaces, the recipe isn't there and this does nothing.

## [0.2.0] - 2026-11-03 - Draft

Owner's request. Not yet tested in game.

### Added

- The Harrier's five bombs (Mk. 82 and the four Mk. 84s) and its three 57 mm rockets (HE, HEAT and AP) can now be made at The Dead Man's
  Switch's ammunition fabricator, as well as at the machining table. A colonist loads the machine and later collects the result; the machine
  works alone in between, at twice the speed of a colonist at the table. Costs and research are unchanged.
- The Hind and Mongoose use the same three rocket recipes, so their rockets gain this too.

### Dependencies

- None added. It switches on only while the fabricator exists, which The Dead Man's Switch provides only with Combat Extended.

### Save compatibility

- Safe to add. Before removing this mod, delete any bomb or rocket bills you added at a fabricator, because the fabricator may no longer list those recipes.

### Compatibility and limits

- The fabricator drops what it makes beside itself, whatever the bill's storage setting says, and haulers take it from there.
- If a colonist is interrupted while loading, the machine drops what it was given, and the job starts again. A chemfuel-heavy bomb takes
  many trips, so this is more likely than with ordinary ammo. Nothing is lost.

## [0.1.0] - 2026-11-02 - Draft

First version, for Harrier VTOL Attack Aircraft by potemkinvillager. Owner's request. Not yet tested in game.

### Fixed

- The Harrier's flight framework failing to load fully while RimPacts is active. RimPacts patches a Vehicle Framework method in a way
  that breaks the next patch of it, so more than 50 framework patches never loaded, among them boarding, refuelling and landing. An
  ordinary patch now takes the place of RimPacts' one and calls its filter, so RimPacts' landing rule still works.
- The log warning Parsed 2.5 as int from the jet's fuel tank. The game already used 2, so only the warning goes.

### Balance

- The jet's build cost: 400 steel, 120 plasteel, 30 components, 1 RimThunder turbine or 3 engine blocks, 4 exhausts, 4 suspensions,
  4 wheels, 3 fuel tanks, 1 transmission, 2 alternators, 2 batteries and 6 wiring. It was 450 steel, 150 plasteel, 25 components,
  6 advanced components and 1 engine block. Authored; the design record compares value and mass with the Hind and Mongoose.
- With RimThunder the blueprint also needs turbocharged research, so it can't appear before its engine can be made.
- Construction skill 10, up from 8, as for the Hind and Mongoose.
- Drop Tanks upgrade: 2 fuel tanks, 20 steel and 2 components, in place of 60 steel and 2 components.
- Under Combat Extended, bomb ingredients now come much closer to the bombs' real weights. The Mk. 82 and incendiary Mk. 84 stop short on
  purpose (owner's choice), at one shared ratio of about 0.23 FSX or prometheum and 3.5 chemfuel per kg of real fill. Without RimThunder,
  cases cost steel in place of heavy castings. Selling prices now follow from the ingredients.
- The texts and figures are agent-written, held for the owner's review.

### Dependencies

- None required. It does nothing without the Harrier. One small assembly, built for RimWorld 1.6 and loaded on 1.7 too, loads only while
  the Harrier is active. It references the installed Vehicle Framework and copies nothing.

### Save compatibility

- Safe to add. A jet already built keeps working, and only new builds pay the new cost. Bombs already made are unchanged.
- Safe to remove. Costs return to the Harrier's own, and with RimPacts active its framework fails to load fully again.

### Compatibility and limits

- A second fault in the framework could have been hidden behind this one, because it stopped loading at the first. Only running the game
  will show it.
- Not checked: whether a shot-down Harrier raider from VTOL Raids leaves a share of the new, richer cost.
- If RimPacts stops using a generic patch, the fix finds nothing to do. If RimPacts renames what it reads, it logs a warning and changes nothing.

## D'thorga Missile Patches history

Merged into this mod on 7 November 2026.

### Missile Patches [0.1.0] - 2026-11-07 - Draft

Owner's request: a strategic warhead from InterRim Ballistic Missile hung the game at detonation until Windows closed it. Read from the owner's log and the mods' code; not yet tested in game.

#### Added

- The mod: fixes for InterRim Ballistic Missile, loaded only while it is active, built on D'thorga Framework.
- A strategic warhead's blast is spread over frames: the struck map is crossed a slice per frame from the centre outward, within a time budget, so the game keeps responding. Everything the warhead would have destroyed is still destroyed, by the missile mod's own rules.
- During each slice the game's room and region rebuild is held and run once at the slice's end, instead of after every fallen wall. That rebuild, triggered by placing each building's leavings, is what made the one-frame blast grow slower with every building.
- A message when a blast runs longer than two seconds, and one when it has spent itself.
- Settings: the time per frame (2 to 50 ms, default 12), a switch for the whole fix, and a switch for holding the room rebuild.
- Dev actions under the D'thorga category: strike the current map as an extinction-class warhead (destroys everything there), log the blast queue, drop queued blasts. Each finished blast logs one line with counts, seconds, frames and swallowed errors.

#### Fixed

- The game no longer stops responding for minutes when a strategic missile lands on a large map. In the owner's game of 7 November 2026 the one-frame blast was still running after two minutes when Windows closed the game as hung.
- Things already destroyed when the blast reaches them are skipped instead of logging a red error each.

#### Save compatibility

- Safe to add and to remove; nothing is saved. A blast still in progress when the game is saved is forgotten on load, so what it had not reached survives.

#### Compatibility and limits

- The fires, terrain changes and cloud the warhead makes still happen in the frame the screen reaches white, as the missile mod does them.
- Needs Harmony and D'thorga Framework. The missile mod ships only a 1.6 build; this mod is built for 1.7 and 1.6.
- The missile mod's Workshop description could not be read (region-blocked), so its licence is not recorded; nothing of it ships here.
- Owner's first run, 7 November 2026, 13:18: the game no longer froze and Windows never closed it, but the blast on an enemy town map was still crossing it 14 minutes later (read from the owner's log).
