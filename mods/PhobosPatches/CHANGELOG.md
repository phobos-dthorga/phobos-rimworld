# D'thorga Patches changelog

Maintained from 2 November 2026, as D'thorga Vehicle Patches until 7 November 2026; D'thorga Missile Patches' history is at the
end. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet tested in game unless an entry
says the owner tested it.

## [Unreleased]

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
