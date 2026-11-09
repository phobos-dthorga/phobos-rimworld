# D'thorga Framework changelog

Maintained from 4 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.60.0] - 2026-11-10 - Draft

From the owner's decision on staying down (10 November 2026). **Not yet tested in game.**

### Added

- A boat ordered to stay down can now be lost with all hands. Once nobody aboard can work it, the order cannot be changed, the boat will not come up by itself, and a letter says so.

### Changed

- Stay down no longer lets a crew that passes out bring the boat up: they die there. A boat without the order still comes up by itself when it loses everyone who can work it.

### Save compatibility

- Safe to add. The new saved value has a default.

### Compatibility and limits

- Nothing can raise a lost boat yet. Equipment for recovering one is planned.

## [0.59.0] - 2026-11-10 - Draft

From the owner's answers on submarines (10 November 2026). **Not yet tested in game.**

### Added

- Being found: a boat under the water can now be spotted by anyone hostile who is near, awake and has a clear line to it. Moving makes that easier; fog, rain and darkness make it harder. A boat seen going down is followed under, and a blast that hurts it gives it away.
- A spotted boat shows as a dark shape and can be aimed at by anyone whose weapon can reach it. Unspotted, it shows nothing, except a faint mark at the middle of a boat of yours so that you can find it. Watchers lose it about 30 seconds after they last spotted it, and you are told both ways.
- Stay down: a button that keeps a boat under after its air and charge run out, off by default. The crew then breathes stale air, a new condition that gets steadily worse and clears in fresh air. If nobody aboard can work the boat, it comes up by itself.
- A design can now choose to dive in chest-deep water and, on the world map, in rivers, as a midget boat would.
- For testing, in dev mode: an action that has a selected submersible spotted at once.

### Changed

- A boat now comes up by itself if it loses everyone who can work it, whatever else is happening.

### Save compatibility

- Safe to add. The new saved values have defaults, and no existing vehicle has the comp except the test hull.

### Compatibility and limits

- Watching applies only on a map; a caravan under the water is still simply out of reach. Combat Extended's guns are judged by their default shell, not the one loaded. A hostile boat, when there is one, could still be clicked while unseen.

## [0.58.0] - 2026-11-09 - Draft

From the owner's request for a framework for submarines (9 November 2026). **Not yet tested in game.**

### Added

- Submersibles: any boat can be made to dive with one comp and two stats in its XML. A boat over deep water can dive and surface with a short delay, and says why when it will not.
- Under the water a boat is drawn as a dark shape, nothing picks it as a target, only explosions reach it and at half strength by default, most of its guns hold fire, and nobody can get out.
- A reserve of air and charge runs down while under, three times as fast on the move by default, and refills on the surface. A boat that runs out, reaches shallow water or has its ballast or pressure hull badly damaged comes up and says why.
- A caravan made up wholly of submersibles can dive as one over water tiles, and is then no target for ambushes, demands or meetings.
- For testing, in dev mode: an action that places a crewed submersible on the map and one that nearly empties its reserve.

### Save compatibility

- Safe to add to an existing save. Nothing changes for a vehicle without the new comp, and no existing vehicle has it.

### Compatibility and limits

- A boat under the water still claims its cells and leaves a wake, fuel burns as usual, and a submerged caravan is not stopped from visiting a settlement. Combat Extended's targeting of a hidden boat is untested.

## [0.57.1] - 2026-11-09 - Draft

From the owner's Harrier, which came down on fire short of home. **Not yet tested in game.**

### Fixed

- A holed fuel tank goes on leaking for the whole of a flight, at the rate its damage sets, so it can run an aircraft dry before it gets home. Before, the leak and the vehicle's systems stopped partway through a flight, up to six in-game hours after take-off.

### Save compatibility

- Safe on an existing save. An aircraft already asleep in flight when you load stays that way until it lands.

## [0.57.0] - 2026-11-09 - Draft

From the owner's rule of 9 November 2026: anything that takes water or piping has its own pipes beneath it. **Not yet tested in game.**

### Added

- A parked vehicle that takes on water joins any plumbing it stands on or against, across its whole footprint, so a pipe touching any side connects and a run passes under it. It takes those pipes with it when it drives off, as a gravship does. A tanker or a rig does the same with the pipes of a fluid it carries or pumps.
- For modders: a comp that gives any building its own plumbing beneath it, and a shared piece that keeps track of what a thing has laid.

### Save compatibility

- Safe on an existing save, which converts by itself at the next load. Pipes already there are left alone. If this mod is removed later, what it laid stays as ordinary hidden pipe.

## [0.56.0] - 2026-11-09 - Draft

From the owner's idea of a dashboard to interact with (9 November 2026). **Not yet tested in game.**

### Added

- The Dashboard has an engine key that starts and stops the engine, and carries the vehicle's own working buttons (set up, pack up, overdrive and the like).
- A check-engine warning under half engine health, red under a quarter.
- Any warning can show as a tinted picture and open the Systems tab when clicked. The pictures are not made yet, so warnings still show in words and the key uses the plain switch picture.

### Save compatibility

- Safe on an existing save.

## [0.55.0] - 2026-11-09 - Draft

From the owner's request to refresh every vehicle's parts (9 November 2026). The figures are authored. **Not yet tested in game.**

### Added

- Every combustion land vehicle gets a water pump, whose damage leaves the radiator less coolant to cool and spills some.
- Every combustion vehicle gets a fuel pump. A damaged one costs a quarter of its speed, and a ruined one will not let the engine start until it is repaired.

### Save compatibility

- Safe on an existing save: vehicles gain the two parts undamaged at the next load.

## [0.54.0] - 2026-11-09 - Draft

From the owner's question about the survey truck (9 November 2026). **Not yet tested in game.**

### Changed

- A vehicle that is set up can have its engine started. It still cannot be driven until it packs up, and Go to bay says so. A vehicle no longer has to be switched off to set up or tip, only stopped.

### Fixed

- Working a vehicle hard while it stands now heats its engine as it should when the drivetrain model is on. Overdrive made no heat there, so its overheating cut-out could not trip.

### Balance

- With a driver holding the engine on, set-up work is charged only the fuel it burns above idling, so nothing pays twice.

### Save compatibility

- Safe on an existing save.

## [0.53.0] - 2026-11-09 - Draft

From the owner's screenshots of two shot-up raider vehicles (9 November 2026). The figures are authored, the owner agreed them. **Not yet tested in game.**

### Added

- A cracked sump: a hit on an engine can leave it leaking oil, even parked, at one of three rates. A field patch slows it and a repair stops it.
- A new oil pressure stage: with nothing reaching the bearings a running engine seizes in about five hours, and says so.

### Fixed

- Six Vanilla Vehicles Expanded designs (Traveller, Bang Bus, Highwayman, Bunsen, Roadkill, Scytheman) treated a suspension or a gearbox as their engine, so hits on the real engine went unnoticed and overheating wore the wrong part. Parts we add around the engine now sit where the engine is.
- Another faction's vehicle no longer tells you to park it beside water for a top-up.

### Balance

- Hits on a radiator, an oil pump or an engine now cost coolant and oil by the share of the part's health lost, not by points, so big and small vehicles agree. A wrecked radiator empties the system; a wrecked engine and pump leave under a third of the oil.
- Boiling over now kills any engine in about seven hours, where a big one used to last days.

### Save compatibility

- Safe on an existing save. The six vehicles above find their real engine at the next load, and damage already taken stays as it is.

### Compatibility and limits

- Other factions' vehicles still do not evolve after a hit, to save time each tick: a raider's holed radiator shows the hit but does not go on draining, unless the full model is set for every faction.

## [0.52.0] - 2026-11-09 - Draft

### Added

- A setting for which picture a working truck's power point uses, the smaller socket by default. A matter of looks only. Not yet tested in game.

### Changed

- The vehicle parts bench, tuning rig, depot bay, cable end, power point, vehicle parts, jump-start kit and booster pack have proper pictures, replacing the stand-ins drawn in code. Generated with OpenAI's image API, recorded beside the textures. Not yet tested in game.

## [0.51.0] - 2026-11-09 - Draft

### Added

- A vehicle's parts can now swing out flat about a hinge when it sets up, each as its own picture, in step with the facing. Before it sets up it checks both sides for walls, buildings and other vehicles, uses the clearer one, and refuses with a reason if neither is clear, so a part does not draw through them. D'thorga Vehicles' survey truck is the first to use it. **Tested in game by the owner, 9 November 2026, on this version: the survey truck's boom works well.**

## [0.50.0] - 2026-11-09 - Draft

### Added

- Overdrive for working vehicles: a toggle, with its own icon, that runs the work faster for more fuel, full engine load and wear on the working parts, and stops itself before it wrecks one. D'thorga Vehicles' survey truck and drill rig are the first to use it. Not yet tested in game.

## [0.49.1] - 2026-11-09 - Draft

### Balance

- A drill rig that drills faster than a deep drill now meets insects sooner in the same measure, so the risk for each portion of ore is the game's own (owner's decision). The deepchem head raises deepchem at ten times a pumpjack's pace. Not yet tested in game.

## [0.49.0] - 2026-11-09 - Draft

### Added

- What a tipping truck needs: a button that tips a vehicle's whole cargo into a half-circle behind it. D'thorga Vehicles' tipper truck is the first to use it. Not yet tested in game.

## [0.48.0] - 2026-11-09 - Draft

### Added

- Ways for other mods' tie-ins to add work to a drill rig and readouts to a survey truck, and the first two of them: Rimefeller oil wells and Dubs Bad Hygiene groundwater. Not yet tested in game.

## [0.47.0] - 2026-11-09 - Draft

### Added

- What a drill rig needs to raise a liquid: a buffer on the rig, a way for a tanker to load from it and a way for a pipe to take from it. D'thorga Vehicles' deepchem head is the first to use them. Not yet tested in game.

## [0.46.1] - 2026-11-09 - Draft

### Added

- A shared rule for which kind of ammunition a magazine may take, used by D'thorga Patches for the Harrier. Nothing changes in play from this alone.

## [0.46.0] - 2026-11-09 - Draft

### Added

- What working vehicles need: a shared job for a colonist to work a set-up vehicle, a power point that makes one a real load on a grid, and a medium diesel drivetrain. D'thorga Vehicles' survey truck and drill rig are the first to use them. Not yet tested in game.
- While a survey truck is set up, placing a deep drill shows the ore it found, as with a ground-penetrating scanner. Not yet tested in game.

### Changed

- A vehicle turning its engine for work with nobody driving now runs its alternator at the right speed. The generator set benefits too. Not yet tested in game.
- The set-up code shared by the tanker and the generator set now also draws their set-up look and finds their fittings a clear cell. They should behave as before. Not yet tested in game.

## [0.45.0] - 2026-11-09 - Draft

### Added

- A depot bay, on the motor pool tab: a home for one vehicle with a fuel tank and pump, a shelf for its parts and an outlined parking pad. A button on the vehicle sends it home, and it switches off when it arrives. On the pad it refuels with nobody attending, on chemfuel and while the bay has power, and mechanics need 70 percent of the usual work. It only moves when you tell it to. Not yet tested in game.

### Save compatibility

- Safe to add to a running colony.

## [0.44.0] - 2026-11-09 - Draft

### Added

- What a generator vehicle needs: a cable end that puts its power on your grid, figures for output, reach and a battery buffer, and a road diesel-electric drivetrain. D'thorga Vehicles' generator set is the first to use them. Not yet tested in game.

### Changed

- The bulk tanker's set-up code is now shared with other vehicles that set up. It should behave exactly as before, and its saved state is unchanged. Not yet tested in game.

## [0.43.0] - 2026-11-09 - Draft

### Added

- A new Architect tab, motor pool, beside the Vehicles tab, holding our stations and our vehicles. Not yet tested in game.
- A vehicle parts bench (research Machining). Every vehicle part, jump-start kit and booster pack is now made there and only there, so they no longer lengthen the machining table's list. A bill already on a machining table goes on working until you remove it. Not yet tested in game.
- A tuning rig (research Machining). A tune, or a change of grade on a working part, now waits until the vehicle is parked within 3 cells of a powered rig, and says so. The reach is outlined while you place or select it. Fittings and replacing a destroyed part need no rig. A setting, on by default, turns the rule off. The self-test checks that a tune waits. Not yet tested in game.

### Save compatibility

- Safe to add to a running colony. A tune or refit already ordered waits for a rig until you build one or turn the setting off.

### Compatibility and limits

- The bench and rig pictures are stand-ins drawn in code.

## [0.42.1] - 2026-11-09 - Draft

### Changed

- The vehicle self-test, the cost measurement and the check after a load write to the log only when something is wrong. A passing result stays in its window, because the game's log puts a stack trace under every line and a routine result looked like a fault. **Not yet tested in game.**

## [0.42.0] - 2026-11-09 - Draft

### Added

- A vehicle self-test for dev mode: reads every vehicle you have, then exercises fittings, tunes, part replacement and grades on a throwaway vehicle, and opens a pass or fail report. Every load in dev mode also checks your vehicles by itself. **Tested in game by the owner, 9 November 2026: 76 passed, 0 failed, 0 skipped.** That covers the service calls on a throwaway vehicle; the haulers, the mechanic's job and the window are not yet tested in game.
- A measurement for dev mode of what the vehicle model costs over an in-game hour or day, with a verdict against its budget. **Tested in game by the owner, 9 November 2026: within budget.**

## [0.41.1] - 2026-11-09 - Draft

### Fixed

- A room that merged with another or was split while gas was being added could keep its gas undiluted, or leave the cut-off half with none. **Not yet tested in game.**
- A vent standing beside a room, but not opening into it, no longer drains that room as if open to the outdoors. **Not yet tested in game.**
- After a large job finishes, the shared work queue no longer holds on to the game that queued it. **Not yet tested in game.**

## [0.41.0] - 2026-11-09 - Draft

### Added

- Part grades. Salvaged parts are cheap, frail and fault-prone and cannot hold a fitting; heavy-duty parts cost more, take more punishment and shrug off faults. Choose one from the new Grade button in the Fittings and tunes window, for a destroyed part before it is replaced or as a refit of a working part, which uses up the old one. **Not yet tested in game.**

### Balance

- Salvaged: 70 percent of the cost and health, faults from hits 1.5 times as bad. Heavy-duty: 175 percent of the cost, 130 percent of the health, faults from hits 0.6 times as bad, needs Machining and Construction 6. All authored, to be tuned in play.

### Save compatibility

- Safe to add to a running game. A part's grade is saved by part, and its health is put back after every load.

### Compatibility and limits

- A substitute part from another mod only ever stands in for a standard part.

## [0.40.5] - 2026-11-09 - Draft

### Fixed

- Loading a game within a session no longer keeps the previous game's vehicles in memory. **Not yet tested in game.**

## [0.40.4] - 2026-11-09 - Draft

### Fixed

- A fitting or tune whose mod was removed could stop that vehicle loading, the same way the black map did: it is now dropped early enough. **Not yet tested in game.**
- Cancelling an order, or losing its part, while a mechanic was working stood down every repair on that vehicle for a day, or finished another job on the first one's time. A job now does the work it set out to do, and stops quietly if that work has gone. **Not yet tested in game.**
- Mechanoids were refused every fitting and tune: they now count at their fixed skill, as the game counts them. **Not yet tested in game.**
- Engine heat from a turbocharger or a tune also heated the gearbox. **Not yet tested in game.**
- Tunes and the turbocharger are no longer offered where they could do nothing or only cost: boats, aircraft, and gearing with no gears for it to change. **Not yet tested in game.**
- Fittings are not offered for a system the vehicle does not have, such as a radiator fitting on a tank with no coolant. **Not yet tested in game.**
- Long gearing now saves fuel and short gearing costs a little, where it had no effect on fuel by default. **Not yet tested in game.**
- A fitting is lost with a part destroyed in a garage, and work on a part destroyed moments before it finished no longer uses up the materials. **Not yet tested in game.**

## [0.40.3] - 2026-11-09 - Draft

### Fixed

- Taking more vehicles out of a garage than the caravan has free people to drive is refused and says why. Before, the whole convoy could not move until they were parked again. **Not yet tested in game.**
- A tanker left set up with no tank by an earlier version is freed when the game loads. **Not yet tested in game.**
- A damaged pump can move large containers again: a step smaller than one item moves one, where it used to report a full tank or nothing at all. **Not yet tested in game.**
- The last of the room in a water tower now fills, where it used to read as full. **Not yet tested in game.**
- Founding a garage checks that the convoy paying for it carries the cost. Another caravan on the tile could satisfy the check, and the convoy then paid only what it had. **Not yet tested in game.**

### Balance

- A pump too slow to move one whole item in a step runs a little ahead of its rate: an authored simplification.

## [0.40.2] - 2026-11-09 - Draft

### Fixed

- Loading a save made before 0.40.0 left every vehicle unable to spawn, and the map black behind the interface: the fittings lists were null on an older save at the moment Vehicle Framework first asked for the vehicle's stats. Found by the owner in play, 9 November 2026. Do not save a game loaded on 0.40.0 or 0.40.1; load it again on 0.40.2 and it is whole. **Tested in game by the owner, 9 November 2026: the save loads and works again.**

## [0.40.1] - 2026-11-09 - Draft

### Fixed

- A garage could lose up to its whole founding cost from its repair stock when a lone caravan joined it: it is paid for once now, garages already built included. **Not yet tested in game.**
- A tanker pumping into a pipe network lost its load when the only tanks with room were marked for transfer, and could gain a little when pumping from a net others were drawing on: it now counts what really moved. **Not yet tested in game.**
- Taking the tank off a tanker that was set up left it unable to move, with no way to pack up. **Not yet tested in game.**
- With two or more tankers in a caravan, only the first could be given a bulk order: each tanker now has its own buttons, named. **Not yet tested in game.**
- A bulk order could fill a holed or burst tank and lose the load the moment it was paid for: it is refused now, as the pump refuses. **Not yet tested in game.**
- A tank holding a fluid whose mod has been removed no longer breaks its menus, and a pump set going on an empty compartment says why it is not running. **Not yet tested in game.**

## [0.40.0] - 2026-11-09 - Draft

### Added

- Fittings and tunes for vehicles, from any mod, through a new button on the Dashboard. A fitting goes on one part, one to a part, and is lost if that part is destroyed. The first set: uprated radiator, oil cooler, turbocharger, heavy-duty alternator, deep-cycle battery, heavy-duty springs and road tyres. **Not yet tested in game.**
- Engine tunes (economy, balanced, power) and gearing (long, standard, short), set by a mechanic. Tunes apply to piston engines; gearing needs a gearbox. **Not yet tested in game.**
- Demanding work names a minimum Construction skill: a mechanic below it is refused and told why, and one at or above it always succeeds. **Not yet tested in game.**
- Vehicles of other factions may be found with a set of fittings and tunes suited to their tech level, and keep them if captured. A setting turns this off. **Not yet tested in game.**

### Changed

- A caravan's repair button uses the best mechanic aboard.

### Fixed

- A tank's damage marks follow the health of their part. Vehicle Framework never reads a part requirement written on an overlay, so the tank now draws them itself. **Not yet tested in game.**

### Balance

- Every figure for fittings and tunes is authored and is to be tuned in play. A 25 percent chance of a set on another faction's vehicle.

### Save compatibility

- Safe to add to a running game. Fittings and tunes are saved by part, and the uninstall sweep clears them.

### Compatibility and limits

- Part grades are designed and not built: they wait for a destroyed part to be seen being replaced correctly in game.
- Other factions' vehicles run the light model, so fittings that change stats act at once, and engine power and tunes only matter once a vehicle is captured.
- Long gearing cannot make a vehicle faster than it was built to go.

## [0.39.1] - 2026-11-09 - Draft

A watchdog on vehicle repairs, at the owner's request (9 November 2026): a long repair and a stuck one look alike. **Not yet tested in game.**

### Added

- A mechanic whose work on a vehicle has got nowhere for an in-game hour now stops, and a message says so. Repairs to that vehicle wait a day before being tried again, and its inspect line says that is why. This covers services, fitting parts, field fixes and the ordinary free repair.

### Compatibility and limits

- A long repair that is making progress is never stopped, however long it takes. Only work that has stopped moving counts.
- The wait is not saved, so loading a game lets repairs try again at once.

## [0.39.0] - 2026-11-09 - Draft

The bulk order gets the game's own controls (owner, 9 November 2026). **Not yet tested in game.**

### Changed

- A bulk order now opens a small window with the number box and arrows of the game's trade window, the live price and a Buy button, in place of three preset amounts.

## [0.38.1] - 2026-11-09 - Draft

Bugs found by reviewing the vehicle repair code before it was tested. **Not yet tested in game.**

### Fixed

- A service or repair that needed more than about 2,000 ticks of a mechanic's time could never finish: the mechanic worked, stopped and started again for ever, and no supplies were used. This also affected existing services, such as a hot-section overhaul.
- Saving and loading while such a job was under way would have finished it at once.
- A vehicle worn only by overheating, low oil or a breakdown was not picked up for repair until something actually shot it.
- One part nobody had made stopped everything else a vehicle was waiting for from being hauled to it.
- A newly fitted part kept the old part's conditions. It now starts clean.
- A radiator repaired to full in a garage could keep leaking after a save and load.
- The request to patch leaks stayed on after the leaks had gone, so the next leak was patched unasked.
- Another faction's vehicle ran a full update when hit. It now counts the hit and nothing more, as the lighter model is meant to.
- A part that plays two roles counted a hit for only the first.
- A vehicle in a caravan or garage no longer says it is waiting for a patch nobody asked for.
- Shared logging core 1.0.1: a rare odd line could make the count of repeating messages throw.

### Compatibility and limits

- Our errors now go to the log under our own name, so the game's setting that pauses or opens the log on an error does not react to them. The line is still written.

## [0.38.0] - 2026-11-09 - Draft

Bulk buying, for D'thorga Vehicles 0.16.0 (owner, 9 November 2026). **Not yet tested in game.** Design: docs/development/utility-vehicles-design.md.

### Added

- A tanker in a caravan at a settlement can buy a fluid in bulk straight into its tank, a quarter, half or all of the room left, paying with the caravan's silver. The price is the game's own, with the caravan's best negotiator; what a settlement sells follows what its traders deal in.
- A settlement cannot sell the same fluid in bulk again for 15 days.

### Compatibility and limits

- It works beside the trade window and does not change it. It only buys, and only fluids that exist as items, so nothing is sold that travels only by pipe.

## [0.37.0] - 2026-11-09 - Draft

Tank compartments, for D'thorga Vehicles 0.15.0 (owner, 9 November 2026). **Not yet tested in game.** Design: docs/development/utility-vehicles-design.md.

### Added

- A tank can be divided into compartments, each with its own fluid. They share the tank's capacity equally and share its temperature. The pump serves one compartment at a time, chosen with a button.
- A new setting, on by default: a tank holds one kind of fluid until its compartment upgrades are fitted. Off, every liquid tank has its compartments free.
- Food is kept apart from everything else. A fluid counts as food if its item has a nutrition value; a modder can whitelist or blacklist any fluid in XML. The contents menu says why a choice is refused.

### Fixed

- A tank that leaked or vented dry no longer forgets what it held, so it still asks to be purged.
- A tanker leaving the map no longer keeps its hoses drawn.

## [0.36.0] - 2026-11-09 - Draft

What a badly damaged vehicle costs, and a parts framework of our own (owner, 9 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-health-consequences.md.

### Added

- A destroyed part now needs replacing before ordinary free repair can continue. The supplies are hauled to the vehicle's cargo and a mechanic fits them; the part comes back at a quarter of its health. Damaged parts are still mended free. A setting turns this off.
- Settings for the price: replacing an engine or transmission costs 100% of the whole part by default, any other part 50% of its materials. Either can be anywhere from nothing upward.
- Our own parts, which always exist: an engine assembly, a gas turbine assembly and a transmission, made at the machining table. Other mods' matching parts are accepted instead only where they make sense: a RimThunder engine by the vehicle's weight, Vanilla Vehicles Expanded's car parts only on car-sized vehicles.
- A danger clock on the Health tab: how long a leak or a short has before the level reaches the next bad stage, worked out from what the level is really doing.
- A badly warped cylinder head now needs its own job, with steel and components; a slightly warped one is still cleared by repairing the engine.
- Field fixes: a colonist can patch a leak or a short with a component, away from any base. The patch halves it, once, and never repairs the part. A button on the vehicle asks for it.
- In a caravan at a halt, a button per vehicle does these repairs, at once and from its own cargo.
- For modders, in XML: what each part role costs and which substitutes it takes, and conditions that need a job of their own or can be patched. The guide is docs/vehicle-systems-modding.md, Parts and repairs.
- Shared, in the core: Rules/CostShareRule and Rules/DangerClockRule (tested).

### Compatibility and limits

- Every player's vehicle from any mod is affected, and a vehicle whose engine is destroyed cannot move until it has a new one. Have parts made before you need them, or turn the setting off.
- The garage skips destroyed parts and charges nothing for them. It cannot fit new ones, so that vehicle has to go home first.
- The danger clock is an estimate, and says about.
- All the labels and every figure are agent-authored, held for the owner's review.

### Save compatibility

- Safe on a running colony. A part already destroyed in a saved game will now wait for supplies.

## [0.35.0] - 2026-11-09 - Draft

More that a tank's contents can do, for D'thorga Vehicles 0.13.0 (owner, 9 November 2026). **Not yet tested in game.**

### Added

- A fluid can now detonate when its tank is hit, explode as something other than flame, reach further when it does, light itself when spilled, pollute the ground, and say what happened to it when its clock runs out. A blast has a cap, so one truck cannot erase the map.
- A tanker can pump out a Dubs Bad Hygiene septic tank.

### Fixed

- A tank of something moved in whole items could never be emptied once a leak left less than one item in it.

## [0.34.0] - 2026-11-08 - Draft

Bulk tanks on vehicles, for D'thorga Vehicles' bulk tanker (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/utility-vehicles-design.md.

### Added

- A bulk tank any vehicle can carry, with the tank fitted through the vehicle's upgrade tree. Anyone can add a fluid in XML: what tank it needs, its weight, whether it freezes, thickens, spoils or vents, and where it can be pumped.
- Set up a tanker, then load or unload it: from pipes of Vanilla Expanded Framework's networks, from Dubs Bad Hygiene's plumbing, from stacks on the ground, or into another vehicle's fuel tank. It cannot move while set up.
- A tank's contents warm and cool slowly with the air. Water freezes and cannot be pumped; fuel can thicken and pump slowly; food spoils completely on a clock; gas vents in a fire or great heat, and a vessel whose relief valve is too damaged bursts.
- A damaged tank leaks, and a destroyed one bursts. An empty tank must be purged, by a colonist, before it can carry something else.
- A new setting, on by default: a flammable load explodes when its tank is destroyed, more violently the fuller it is.

### Compatibility and limits

- Taking a tank off with a load in it spills the load: Vehicle Framework gives no way to refuse that job.
- Only a Vehicle Framework vehicle can have a tank. The pipe port needs Vanilla Expanded Framework, the plumbing port Dubs Bad Hygiene; neither is required.

## [0.33.0] - 2026-11-08 - Draft

The shared logger, and what coolant is made of (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/logging-framework-design.md.

### Added

- Shared, in the core: the shared logging core (phobos-log 1.0.0, compiled in) and Logging/Logs, a log per mod or part under the mod's own category in Player.log, with lines that can repeat written once or at most so often, and one budget of lines per mod.

### Changed

- This mod's lines, and the load line of every D'thorga mod, are written under the mod's own category in Player.log instead of the game's. Quote them the same way.
- With Dubs Bad Hygiene, a coolant top-up is 10 litres of water from plumbing beside the vehicle and no chemfuel. Without it, the top-up takes 3 chemfuel in place of the water, as oil does; water is no longer free for it. The Harrier's injection water and a boiler's feed water are unchanged.

### Fixed

- A line written very early in loading, before the engine's log is ready, now goes through the game's own log instead of failing.

### Compatibility and limits

- A dev action's own answer still goes through the game's log, so it shows in the usual place.

## [0.32.0] - 2026-11-08 - Draft

Two ways of handling a large fleet, at the owner's request (8 November 2026). **Not yet tested in game.**

### Added

- With Vehicle Framework: the fleet board, a button on the bottom bar listing every vehicle you own wherever it is (on a map, in a convoy, in flight, in a garage or left parked), with fuel, worst part, condition and crew. Sort by any column, search by name or place, click a vehicle to go to it. A setting hides the button.
- A letter before a vehicle left parked in the wild is lost, naming the vehicles and how long is left, once per parked group. Three days ahead by default; a setting changes that or turns it off.

### Compatibility and limits

- The button's icon reuses the vehicle service picture until art is picked. The board costs nothing while closed and rebuilds its list a few times a game hour while open.

### Save compatibility

- Safe to add to and remove from a save. It saves only which parked groups have been warned about.

## [0.31.0] - 2026-11-08 - Draft

The vehicle garage's rules and code, at the owner's request (8 November 2026). The garage itself is a Def in D'thorga Vehicles. **Not yet tested in game.**

### Added

- With Vehicle Framework and Vanilla Expanded Framework: a garage class for the outposts system, holding vehicles with no time limit, and repairing them every few days from steel and components in its stores.
- A button on every vehicle convoy to set up a garage where it stands, and buttons at a garage to park a convoy and to take vehicles out.
- The world-map Health, Dashboard and Systems tabs show the vehicles a garage holds. Other mods' world objects can join in by implementing the new vehicle holder interface.
- A game-free rule for bays and for what one repair cycle can spend, with tests.

### Compatibility and limits

- Loads only while Vehicle Framework and Vanilla Expanded Framework are both active. A garage parks a whole convoy at a time; aircraft are refused. Parked vehicles are frozen, so fuel, batteries and conditions hold still; repairs mend parts only, and conditions still need on-map service.

### Save compatibility

- Safe to add to a save. **Before removing it, take every vehicle out of every garage**: a garage and the vehicles in it go with the mod.

## [0.30.0] - 2026-11-08 - Draft

For D'thorga Patches 0.14.2. No change in play. **Not yet tested in game.**

### Added

- Shared, in the core: Rules/DefRenameRule, a table of renamed item Defs that gives their current names, their recipes' and the load IDs a save refers to them by (tested).

### Changed

- Dates in this mod's records that were written a day ahead now read 8 November 2026. Text only; nothing in game changes.

## [0.29.1] - 2026-11-08 - Draft

Temperatures that stayed at the air's, from the owner's test of the Cherokee and the M1A2 Abrams (8 November 2026). **Not yet tested in game.**

### Fixed

- Oil in an engine with no coolant (every gas turbine, so the Abrams and every aircraft) followed the air even with the engine running. It now runs from 55 degrees C idling to 95 at full load, and falls back to the air once the engine is off. The figures are authored.
- A gearbox stayed near the air's temperature however far it was driven: its heat was tuned for the old load figure, and the drivetrain model's real loads are far lower on a light cruise. It now warms on any drive and most at full load, and a little while standing with the engine running.
- A turbine's temperature dropped to the air's the moment the engine stopped. It now cools with the metal, about half the gap in a quarter of an hour and near the air in two hours. Its limits and wear are unchanged, and still follow the engine's work.

### Save compatibility

- Safe on a running colony. A turbine's temperature begins at the air's on the first load, then is saved.

### Compatibility and limits

- Vehicle Framework does not tick a vehicle during a world-map flight, so temperatures are caught up on landing, as if the time had passed at what the engine is doing then.

## [0.29.0] - 2026-11-08 - Draft

Part conditions: the third slice of the vehicle health work (owner, 8 November 2026), after a read of Vehicle Framework's development branch for 1.7. **Not yet tested in game.** Design: docs/development/vehicle-health-design.md.

### Added

- Vehicle parts can have conditions, as a body part has an injury: a severity with stages, under the part on the Health tab. A vehicle part's health is still the one health figure; a condition says what it cannot, such as a radiator that goes on leaking after the shot.
- Three to begin with (labels and figures agent-authored, held for the owner's review): a holed radiator, which leaks coolant faster the worse it is, from shot and shrapnel; a shorted battery, which drains itself, from the same; and a warped cylinder head, from boiling over, which makes the engine run hotter and, when bad, slower. All three go when the part is repaired to full health.
- For modders, in XML: a condition Def; part-hit rules that give a condition and that name the kinds of damage they count; an effect that gives a condition from a system's stage; services that clear conditions. The guide is docs/vehicle-systems-modding.md, Part conditions.
- A dev action that gives the selected vehicle's part any condition at any severity.
- Shared, in the core: Rules/SeverityRule, a severity that hits add to, that fades, and that a repair to full clears (tested).

### Changed

- The systems' refusals, stat factors and their explanations now read one list of the stages in force, so a condition's stage acts exactly as a system's does.

### Save compatibility

- Safe to add to a running colony. Conditions are saved by part key; one whose part has gone is dropped with a line in the log. The uninstall sweep clears them with everything else.

### Compatibility and limits

- Vehicle Framework's development branch for 1.7 adds hand brakes that scratch every wheel when a vehicle brakes (on by default in its settings); the shipped conditions ignore scratches, and none of them is on wheels. Its own wheel wear will come on top of the tread wear here when that version ships: not yet decided, recorded in the design.
- Another faction's vehicles get conditions from hits too, but on the light tier they only advance while one of their parts is being hit.

## [0.28.0] - 2026-11-08 - Draft

The game's log kept going past its message limit (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/logging-framework-design.md.

### Added

- A setting, Keep the game's log going after its message limit (on). RimWorld stops all logging, Player.log included, once a session has written 10,000 messages, and only loading a save turns it back on; one other mod's repeating error filled it within minutes in two of the owner's sessions. With the setting on, the log is turned back on through the game's own call when that happens, at most 25 times a session, and one line names the message that filled it and how often it came. It changes nothing until the game has stopped its log; no Harmony.
- Dev actions for the probes in the design record: game log status (the loudest messages so far), one line through the framework's own log category (T2), a flood past the limit (T1, and T4 with the setting on), a minute's count of the log stream by category (T3), and a timing of 1,000 messages each way (T5).
- Shared, in the core: the game's log categories through the engine (Logging/HostLog, falling back to the game's own log if a later build moves them), message signatures and a bounded tally (Rules/GameLogRules), and Rules/TickThrottle, "at most once per so many ticks", which D'thorga Patches 0.14.1 uses (tested).

### Compatibility and limits

- Turning the log back on replaces Ludeon's anti-spam limit with a bounded one; the setting turns it off. Player.log can grow larger in a session with a repeating error; phobos-modkit's log triage reads a large log into a short summary.
- Lines under the framework's own category still reach Player.log after the game's limit (read from the 1.7 engine); they also count toward it.
- Nothing saved changes; the counting lives in memory only.

## [0.27.0] - 2026-11-08 - Draft

A file of every Def with all patches applied, for mod developers (owner's go-ahead, 8 November 2026). **Tested in game:** the owner wrote one on 8 November 2026 (72.8 MB, 335 mods) and phobos-modkit's Def tool read it.

### Added

- A dev-mode setting, Write every Def with all patches applied at the next start, and a dev action that asks for the same. At the next start, once the game has loaded, every Def is written as it stands after every mod's XML patches (before inheritance) to PhobosPatchedDefs/PatchedDefs.xml in the RimWorld user folder, and the request turns itself off. phobos-modkit's Def tool reads the file to show a Def as patched and what the patches changed.

### Compatibility and limits

- It does nothing unless asked: a patch operation of this mod's hands the game's combined XML to the writer only while the request is on, and changes nothing. With the request on, the game's XML is kept in memory a little longer than usual and writing takes a few seconds; the file runs to a few hundred megabytes with a large mod list.
- The file holds other authors' Defs: it stays in the player's own RimWorld folder.
- A mod that applies patches its own way, outside the game's patch list, may change Defs after the file's snapshot; the log line says when the patched XML was not seen at all.

## [0.26.0] - 2026-11-08 - Draft

The Health tab: the second slice of the vehicle health work (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-health-design.md.

### Added

- A Health tab of this mod's own on every vehicle with systems, in Vehicle Framework's place, laid out like a colonist's: on the left the vehicle's speed, strength and the rest, each with a tooltip naming the parts it is made from and what this mod's systems do to it; what the vehicle waits for; the crew with the game's own word for each. On the right every part, grouped outside and inside and by what it does, with its health, how well it works and (in Operator and Engineer mode) its armour, and under each part what already ails it: the stages of the systems that read it (coolant running hot under the engine, worn tread under a wheel, a flat battery that will not start), a fuel leak, or destroyed. Hovering a part lights it on the vehicle; clicking a condition opens the Systems tab at it. On the world map too, on caravans and aircraft in flight.
- A setting, Keep Vehicle Framework's own Health tab (off; needs a restart).
- Dev actions: open the Health tab, rebuild the health view; the tab-state log counts its views.

### Changed

- The Systems and Health tabs share one way of lighting a part on the vehicle.

### Compatibility and limits

- Vehicle Framework's own health panel still appears in its caravan manifest's details window; the two lay the parts out differently.
- No part icons until Vehicle Framework ships for 1.7 (their drawing helpers pass Unity types a 1.7 build cannot use), and the stat rows are drawn by this mod from Vehicle Framework's figures for the same reason.
- The tab rebuilds its rows only when a hit, a repair or a systems update says something changed, and draws them without building text in between.

## [0.25.0] - 2026-11-08 - Draft

Measurement and the lite tier for other factions' vehicles: the first slice of the vehicle health work (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-health-design.md.

### Added

- A setting, Every faction's vehicles run the full model (off by default).
- Dev actions: a timing report (how long the vehicle systems took, by kind of work, with how many vehicles run full, lite or off), reset timing, and log the tier of the selected vehicles. Dev mode must be on while the work runs for it to be timed.
- Shared, in the core: the detail tier rule (tested) and a timing accumulator any Phobos mod can wrap work in, free outside dev mode.

### Changed

- Other factions' vehicles on your map no longer run the full systems model every rare tick: they take hits and keep what they are given, but nothing in them heats, wears or runs down on its own. Until now every raider's vehicle cost what yours did. Off the map they run nothing. A captured vehicle wakes to the full model on its next update from its saved values.

### Compatibility and limits

- Nothing saved changes; the tier is worked out afresh each time. Hits on another faction's vehicle are still counted, so a captured vehicle's systems remember them.

## [0.24.0] - 2026-11-08 - Draft

Bleed air (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-turbine-design.md.

### Added

- Bleed air from gas turbines. In a hover the Harrier's puffer jets take about 3% more of its engine's work, up to half as much again in strong wind; cockpit heat or climate control takes half a per cent on every turbine aircraft. The engine holds its thrust by working harder, so it runs hotter, wears its hot section a little faster and has a little less hover margin; only past its limits does the hover suffer. NASA measured the Harrier's reaction control bleed in hover (Borchers and colleagues, 1994).
- Engineer mode shows the bleed on the Systems tab's turbine line.
- For modders: reactionControlBleed, gustBleedFactor and cabinBleed on a flight profile.

### Balance

- A heavy Harrier on a hot, windy day has slightly less margin to hover than before.

## [0.23.0] - 2026-11-08 - Draft

The vehicle tabs on the world map (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-tabs-design.md.

### Added

- The Dashboard and Systems tabs on a vehicle caravan and on an aircraft in flight, beside Vehicle Framework's own: the systems keep running off the map, and now you can see them there. With several vehicles, a list down the left chooses which one the tab shows, each with a light (green; amber when something is notable; red when it will not move) and what it waits for in its tooltip. The switches work from there, and a service asked for there is done once the vehicle is on a map.
- Shared, in the core: opening a world inspector tab from code.

### Changed

- The map tabs and the world tabs draw through the same code, so they always show the same thing.

## [0.22.0] - 2026-11-08 - Draft

Automatic climate control (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-world-map-design.md.

### Added

- Automatic climate control on vehicles built with it: a switch, on by default, that holds the cab near 21 degrees by heating below 20 and cooling above 22 while the engine runs, so a mild day no longer leaves the cab at 10 degrees. The manual heater and air conditioner stand down while it runs. Fitted by the vehicle (D'thorga Vehicles marks modern ones), its loadout, or any spacer-tech design.
- Every closed cab has climate control (setting, off by default): a blanket option for players who want it on every vehicle.
- On electric vehicles it draws on the traction battery, as the heater does.
- For modders: available on an electrical load (whether the vehicle has it fitted, so its switch shows), climateControl on a loadout's cabin block and on PhobosVehicleCabinExtension, and When_ClimateControl.

## [0.21.2] - 2026-11-08 - Draft

From the owner's test of the Harrier (8 November 2026). **Not yet tested in game.**

### Fixed

- Aircraft and boats had no cab heater, so a closed cockpit or cabin could only cool and froze in the cold even in flight. Aircraft now heat it with air bled from the engines, boats with the engine's heat, below 5 degrees while the engine runs, with a switch as land vehicles have. A parked vehicle with its engine off still follows the weather, as before.

## [0.21.1] - 2026-11-08 - Draft

From the owner's test of the Harrier (8 November 2026). **Not yet tested in game.**

### Fixed

- A vehicle with two services due at once (an engine service and a hot-section overhaul) asked only for the first service's first missing supply, so the second waited unseen behind it. Haulers now fetch everything every due service lacks, the inspect line and the Dashboard list it all (Waiting for supplies for an engine service and a hot-section overhaul: chemfuel x9, component x4, plasteel x10), and a mechanic starts whichever service already has its supplies aboard.
- Shared, in the core: the rule that adds several jobs' needs against what is in stock, with tests.

## [0.21.0] - 2026-11-08 - Draft

For D'thorga Medical 0.8.0's caustic gas. **Not yet tested in game.**

### Added

- Shared, in the core: a condition component that leaves a lasting condition on the same body part when it heals, by the worst it reached (D'thorga Medical's scarred cornea), with its rule and tests. It leaves nothing on a part that has gone or been replaced, after death, or during the uninstall sweep.

## [0.20.0] - 2026-11-08 - Draft

Electric vehicles (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-electric-design.md.

### Added

- Systems for every electric land vehicle Vehicle Framework runs: a traction battery whose temperature sets its power and how fast it charges (slow when cold or hot, none when overheating), which wears with age and heat and holds less charge as it does, until new battery modules are fitted; an electric drive whose motor heats with work, more when damaged; 12 V electrics charged by a converter whenever the vehicle is on, so jump-starts still work; brakes that wear a third as fast; and a cab whose heater and air conditioner cost range.
- Battery fires (setting, off by default, with a warning): a traction battery in thermal runaway bursts into flame.
- The Lightning runs on batteries (setting, off by default, needs a restart): with D'thorga Vehicles, Vanilla Vehicles Expanded's Lightning becomes an electric van to try these systems.
- Dev action: log charging.
- For modders: Effect_ChargeFactor, Effect_LimitCharge, When_Charging, and ofCapacity on Effect_ConsumeFuel. The brakes' wear figures are parameters. Shared, in the core: PatchOperationPhobosFlag, an XML patch that applies only while a registered setting is on.

### Compatibility and limits

- Charging speed follows the battery through a postfix on Vehicle Framework's charge rate, which has no extension point.
- Electric vehicles get no night heater yet; its fuel is counted in chemfuel.
- None of the owner's vehicles is electric without the Lightning setting.

### Save compatibility

- Safe to add. Electric vehicles already built gain the new parts on load, as every retrofit does.

## [0.19.0] - 2026-11-08 - Draft

Kerb masses and the tabs after the owner's first look in game (8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-kerb-mass-check.md.

### Added

- Empty mass (kg) in Vehicle Framework's settings window for every vehicle with systems: the weight cargo, passengers, hills and soft ground are worked against, filled with the vehicle's own figure.
- For modders: PhobosVehicleFiguresExtension gives one vehicle its own empty mass and, where a class rule would be far off, its engine power; it wins over loadouts and the 12 kg per Mass rule. remainingLabel on a quantity makes the tabs' gauges show what is left.

### Changed

- The tabs' tread and brake gauges show what is left (Tread 100% when new), as the battery and oil gauges do (owner's choice). The inspect line still gives the wear.
- The Systems tab no longer repeats a part's name after its role (Battery, not Battery: Battery), and leaves out a system with nothing to show, such as fuel in the cold while its setting is off.

### Compatibility and limits

- Loadouts still pick a vehicle's class (light, medium, heavy) by Vehicle Framework's Mass, not by the empty mass.

### Save compatibility

- Safe to add. The tuning field is saved in Vehicle Framework's own settings file, not in saves.

## [0.18.0] - 2026-11-08 - Draft

The follow-ups to the world map and the tabs (owner's go, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-crew-and-ground-design.md.

### Added

- Booster packs: craft one at a machining table or fabrication bench after researching batteries. A colonist carries it to a vehicle whose battery is too flat to start and jump-starts it with no second vehicle and no jump-start kit. It holds 4 starts, shown in its inspect pane, gains a start back every 2 hours in the cargo of a vehicle whose engine runs, and a bill at a machining table recharges it fully. Caravans with a charged pack aboard get the same jump-start.
- The ground on the world map: a caravan's engines now work against the tile's ground as well as its hills. Each biome's own typical terrain (sand in a desert, soil in a forest), its difficulty (swamps and jungle) and the game's winter snow set how hard the going is; a road is firm whatever the land around it. Laden or underpowered vehicles slow on heavy ground and the caravan's explanation names them.
- Cab moods: people aboard a closed cab are glad of a warm cab in the cold or a cool one in the heat, and unhappy in a cab too cold or too hot for them, by their own comfortable range. A setting, Passengers feel the cab (on), turns them off.
- With RimHUD: a Vehicle value for RimHUD's pawn display, added in its layout editor, showing which vehicle a person is aboard and in what role; the tooltip gives the fuel, the cab and what the vehicle waits for, and a click selects the vehicle.
- Dev actions: give a booster pack (full, one start left, flat), log cab comfort; the caravan log now shows the tile's ground and each passenger's cab comfort.
- Shared, in the Framework's core: a component for things that hold a few uses (with a recharging recipe filter), the shelter comfort rule, the biome ground reader and item finders that take a condition, for every Phobos mod.

### Changed

- A jump-start from a booster pack, crafted or assumed by the setting, no longer needs a jump-start kit: the pack has its own clamps. The kit is still needed for leads from another vehicle.
- Refusals now name both ways out: another vehicle with the leads, or a booster pack with a start left.

### Fixed

- A jump-start that fetched a jump-start kit ended the moment the colonist picked it up, and fetching it raised an error (both from 0.16.0, found reading the code; never seen in game).

### Compatibility and limits

- The world-map ground adds to Vehicle Framework's own travel times per biome rather than replacing them: the biome's difficulty slows the whole caravan there, and the ground here works the engines. Ice is treated by its path cost, so it reads as heavy going rather than slippery and easy rolling.
- The recharge bill makes a fresh pack from the part-used one, so its hit points come back full: a small repair, judged not worth code.
- RimHUD's value is read by RimHUD through its published custom widget interface (api version 1); nothing here references RimHUD's code.
- Cab moods are small beside the game's own (3 for a warm or cool cab, minus 2 or 5 for a cold or hot one), authored.

### Save compatibility

- Safe to add to a running colony. Booster packs save their starts left. Removing the Framework drops kits and packs with an error line each.

## [0.17.0] - 2026-11-08 - Draft

The vehicle tabs (owner, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-tabs-design.md.

### Added

- Two inspector tabs on every vehicle with systems, beside Vehicle Framework's own. **Dashboard**: does anything need me now? A gauge for each quantity that matters (battery, coolant, oil, tyres, brakes, steam, the turbine's hot section, the cab), a warning light for every notable stage (red when it refuses or damages), the one thing the vehicle waits for, the switches and the service button; in Operator and Engineer mode the tachometer and gear, or the turbine's speed. A gauge click opens the Systems tab at that system.
- **Systems**: what exactly is going on. Every quantity with its figure, stage and bar; each role's parts with their health, lighting the part up on the vehicle when hovered; the drivetrain's gear, speed, load and what holds it back; the turbine's rating and engines. In Engineer mode the last day's graph of each quantity, the flows an hour, kilowatts, mass and the hover margin. Hidden in Plain mode.
- The last day of each quantity is kept and saved for the graphs (the owner's exception to "nothing derived is saved": a few kilobytes per vehicle).
- Dev actions: fill history with a test curve, open the Dashboard tab, open the Systems tab, log tab state.
- Shared, in the Framework's core: a history ring and its save codec, the gauge, warning light, graph, section and tachometer widgets, and per-thing tab state, for every Phobos mod's tabs.
- For modders: showOnDashboard on a quantity. The schema check reads it.

### Compatibility and limits

- The tabs are ordinary inspector tabs: RimHUD draws them in its own tab strip unchanged (Vehicle Framework already hands the vehicle pane back from RimHUD). Vehicle Framework 1.7 may order its Def resolution differently; the tabs are added where its Upgrades tab is.
- The Systems tab shows each part's name and health in its own green, amber and red, without Vehicle Framework's part icons: Vehicle Framework ships only a 1.6 build, whose drawing helpers can't be called from a 1.7 build. To revisit when it ships for 1.7.
- For modders: the core's UI helpers are in the Phobos.Framework.Interface namespace (folder UI), because a namespace named UI would hide the game's own UI class.

### Save compatibility

- Safe to add to a running colony: graphs start empty and fill over the first day. Each vehicle's save gains the history strings.

## [0.16.0] - 2026-11-08 - Draft

The world map: caravans, the cab's climate, the night heater and jump-starts, from the owner's decisions of 8 November 2026. **Not yet tested in game.** Design: docs/development/vehicle-world-map-design.md.

### Added

- A closed cab has its own temperature, and everyone aboard feels it instead of the weather, on maps and in caravans: the game's own cold, heat and comfort rules apply to it. The cab drifts toward the outside and toward what the heater or the new air conditioner holds while the engine runs. Open vehicles (motorcycles, an open boat) shelter nobody.
- A night heater (a setting, off by default): with the engine off at a caravan's night camp, a vehicle whose night heater is switched on burns a little fuel to keep its cab warm. Switch it on before leaving.
- Caravans move as their slowest vehicle: a laden truck held back by its engine or the ground slows the whole caravan, and the caravan's explanation says which vehicle.
- A vehicle that will not move (a flat battery, worn brakes, a cold boiler) stops its caravan, and the caravan's inspect text says why.
- A jump-start kit, the leads a jump-start needs: crafted at a machining table or a fabrication bench, never used up. With the setting Jump-start leads must be crafted (on by default) a jump-start on the map fetches one from stock and a caravan needs one aboard, each refusal saying so.
- The setting Every vehicle carries a booster pack (off by default): a jump-start then needs no other vehicle, on the map or in a caravan.
- Caravan jump-starts: a button on the caravan jump-starts a vehicle of it from another vehicle in the caravan (or its booster pack), with the reason when it cannot.
- Dev actions: toggle a load, give a jump-start kit, flatten the battery, log caravan state and set caravan cap (both on the world map).
- Shared, in the Framework's core: the held-temperature registry any mod can give its holders a temperature through, and a finder for a tool in stock or aboard a caravan.
- For modders: a loadout's cabin block and a vehicle's cabin extension; the conditions When_CaravanResting and When_LoadActive; a fuel effect's rate from a parameter; the caravan comp. The schema check reads them.

### Changed

- A jump-start on the map now needs a jump-start kit unless the setting says the leads are free (in 0.10.0 they were).
- With the load-dependent fuel setting on, a caravan's fuel correction follows Vehicle Framework's own world-map rate (with each vehicle's world multiplier) instead of the map rate; it was wrong by that multiplier.

### Compatibility and limits

- Patches the game's held-temperature lookup (a postfix: the game's own answers stand), Vehicle Framework's caravan ticks-per-move and its vehicles' world-map movement check, and adds a comp to its vehicle caravan world object by XML.
- The cab's figures are authored; the night heater's fuel is approximate. Biome is not yet read for a caravan's drivetrain.

### Save compatibility

- Safe to add to a running colony: cabs start at the outside temperature; the new settings start at their defaults. The caravan comp joins saved caravans on load.

## [0.15.0] - 2026-11-08 - Draft

Combat Extended corrections for the vehicle systems, from a reading of CE's vehicle code on 8 November 2026. **Not yet tested in game.**

### Fixed

- Under Combat Extended the parts the systems add were hit far more often than meant: CE sorts parts the opposite way to Vehicle Framework, so a low hit weight meant hit more. Each added part's weight is now inverted under CE, or set by its own CE weight.
- A part hit now counts the health the part lost, not the shot's damage. Under CE a rifle round the armour stopped used to drain coolant, oil or charge as if it had gone through, sometimes twice.
- A boiler explosion now carries an armour penetration on the scale in use (about 24 MPa under CE for its 60 damage, as CE's own blasts of that size); before, armoured things took nothing from it under CE.
- A loaded turret's ammunition mass now counts the shots per item, as Vehicle Framework does.

### Added

- For modders: hitWeightCE on an added part; armorPenetration on the explosion effect; dev actions to hit a selected part through the damage model in use, to log the systems' memory of part health, and to explode a boiler now (destructive, with a confirmation). The schema check reads hitWeightCE.
- Shared, in the Framework's core: the Combat Extended rules (hit weights, blast penetration) and dev-selection helpers, for every Phobos mod.

### Compatibility and limits

- Read from CE's vehicle compatibility code and Defs; whether Vehicle Framework fires a part's reactor before or after CE writes its health is unverified, and the systems tolerate both.

## [0.14.0] - 2026-11-08 - Draft

The owner's answers on steam locomotives, 8 November 2026. **Not yet tested in game.**

### Added

- Setting: **boiler explosions**, off by default, with a warning in the settings window. On, a boiler without a fusible plug whose water falls below the firebox crown with the fire lit explodes, wrecking the boiler and hurting what stands near, with a letter. Boilers with a plug still only vent.
- Setting: **realistic feed water**, off by default. Off, a water stop draws a tenth of a locomotive's real tender or tank capacity; on, the real figure.
- A damaged boiler raises steam more slowly.
- For modders: an explosion effect, a condition on a system parameter, and a service's scaled water figure with the setting that restores the real one. The schema check reads them.

### Save compatibility

- Safe to add to a running colony. The two settings are new and start off.

## [0.13.0] - 2026-11-08 - Draft

Steam boilers: part 3 of the owner's approved proposals of 8 November 2026. **Not yet tested in game.** Design: docs/development/vehicle-steam-design.md.

### Added

- A boiler system for steam locomotives: hours to raise steam from cold (a cold engine refuses to move and says why), safety valves blowing off at the top, a water glass the injector keeps up from the tender or tanks, and a water stop to refill them, drawing only the shortfall (from Dubs Bad Hygiene's water towers when that mod is active, otherwise free).
- A fusible plug: let the water fall below the firebox crown with the fire lit and it melts, venting the boiler, with a letter; the engine cannot steam until a mechanic fits a new one.
- For modders: the boiler and fusible plug systems; a service's water from a system parameter and for the shortfall only; the Steam loadout kind. The schema check reads them.

### Compatibility and limits

- Boiler explosions, priming and water treatment are not modelled. The rates are authored inside the researched ranges and labelled.

### Save compatibility

- Safe to add to a running colony: steam locomotives start cold with full feed water.

## [0.12.0] - 2026-11-08 - Draft

Power and lift, part 1 of the owner's approved proposals of 8 November 2026. **Not yet tested in game.** Design:
docs/development/vehicle-power-and-lift-design.md.

### Added

- Any number of engines (up to eight): each one out takes its share of output.
- Helicopters' rotor drives: the main transmission and rotor masts take only so much power. Lift is the smaller of what the engines give and what the drive takes; asked for more, the drive is over-torqued and wears, and a damaged drive takes less.
- Thrust-lifted craft: lift as a multiple of the craft's full weight on every thruster, so each thruster lost takes lift away.
- A lift-off check for aircraft that launch through Vehicle Framework itself (its helicopters and pods): too heavy for the day's heat or with engines out, they follow the hover-limit setting, now worded to cover lift-off. Refused, the launch button says why: unload, repair the engines, or wait for cooler air. Allowed at a cost, the engines wear at lift-off.
- Diesel-electric locomotives: no gearbox, the engine runs at the power asked of it, the traction motors pull hardest at a standstill and heat when pulled past their continuous rating, running hot and then burning their windings until they cool.
- Steel wheels on rails roll and grip as rails do, whatever the ground beneath.
- For modders: Engine1 to Engine8 roles; the RotorDrive and TractionMotor roles with their systems; the turbine block's transmission limit and lift-to-weight; the Thruster engine kind; a real engine's rated power in kW; the DieselElectric transmission with its tractive efforts; fixed rolling resistance and grip; the lift check on flight profiles and two new profiles for Vehicle Framework's own helicopters and pods; a part finder taking several key hints; new inputs for over-torque and traction load. The schema check reads them all.

### Compatibility and limits

- Patches Vehicle Framework's launch check (the same method its launch button asks) to add the lift-off refusal, and listens for its launch event.
- The pods' lift figures and the traction motors' heating are authored. The Huey's and the Mangusta's drive limits come from published figures; where none was found, the limit is authored and labelled.

### Save compatibility

- Safe to add to a running colony. Vehicles named by D'thorga Vehicles gain the rotor drive or traction motor systems; nothing the model works out is saved.

## [0.11.0] - 2026-11-08 - Draft

The drivetrain and gas-turbine designs (records of 8 November 2026), built with the owner's four answers of 8 November 2026.
**Not yet tested in game.** Designs: docs/development/vehicle-drivetrain-design.md and docs/development/vehicle-turbine-design.md.

### Added

- Vehicle systems: an engine and drivetrain for every land vehicle and boat. Vehicles pick a gear, run at an engine speed and work harder with cargo, passengers, ammunition, hills, sand, mud and snow. A light vehicle gets a petrol engine; medium and heavy ones a diesel; tracked ones (found by their track parts) a tracked diesel; boats a propeller.
- A vehicle slows where its engine runs out of power or its wheels run out of grip, and crawls on at no less than 15% of its speed with "Wheels spinning" rather than stopping (owner's choice). Vehicle Framework's own speeds and path planning are untouched.
- The engine's heat into its coolant follows the fuel it burns; the alternator gives nothing below its cut-in speed and about half at idle, so lights left on at idle slowly drain the battery; oil pressure and the radiator's fan follow engine speed.
- Gas turbines: aircraft and the M1A2 tanks of D'thorga Vehicles run a turbine model with spool speed, turbine temperature, time-limited ratings and an allowance for hot days. Run past its limits a turbine wears its hot section and then takes damage; a worn hot section breaks down until a mechanic overhauls it. A weak battery gives a hot start (wear and a message) or none at all (the draft is refused with the reason); a jump-start gives a clean start.
- Aircraft phases (idling, taxiing, take-off, attack run, vertical landing, cruising) set the engines' work, read from the flight frameworks by name: the Harrier's fixed-wing framework, the VTOL Vehicle framework of the Hind and the Mongoose, and Vehicle Framework's own launch state for any other aircraft.
- Twin engines: a loadout can bind two engines; with one out, the other carries the load on its emergency rating and runs hot.
- Water injection (the Harrier): a tank that lets the engine run its wet rating for a short, hard hover, refilled by a mechanic.
- A coolant top-up is its own service now, due once the coolant is down to 60%: 3 chemfuel for the glycol, and water. With Dubs Bad Hygiene, vehicles take water from a plumbing pipe beside or under the parked vehicle with a water tower on it, and wait for one; without it, water is free (owner's choice).
- Inspect text: Plain shows only "Engine labouring" or "Wheels spinning"; Operator adds the gear and engine speed, or a turbine's speed and temperature and the flight phase; Engineer adds load, engine, fuel and heat in kW and how far the vehicle is held back.
- Settings: the drivetrain off, shown only, or full (default full); fuel use that follows engine load (off by default: authors' fuel figures stand, owner's choice); and what happens when a jump jet is too heavy or the day too hot to hover onto its pad: use the runway (default), allow it with the engine run past its limits, or warn only (owner's choice).
- For modders: drivetrain and flight profile Defs, loadout fields for both (with per-loadout overrides), a rank that breaks ties between equal loadouts, a part-key suffix selector, a service's water in litres, new inputs (engine speed, engine heat, drive power, turbine readings), a water source any mod can register, and a hover check a flight mod can ask. The schema check reads all of them.

### Changed

- The engine service no longer tops up the coolant (the coolant top-up does), and its chemfuel drops from 12 to 9.
- The default loadouts gain a medium land vehicle (Vehicle Framework mass 120 to 350, 12 V electrics with a diesel), a tracked vehicle and a clutch part on land vehicles.

### Compatibility and limits

- Patches Vehicle Framework's path follower to apply the speed cap, before Vanilla Vehicles Expanded's own patch there; nothing else about movement changes.
- The figures are authored or secondary and labelled in the Defs: kerb mass is Vehicle Framework's mass times 12 kg unless a loadout gives a real one; turbine temperature limits are placeholders. Engine sound, an APU for the M1A2 and a skid-steer turning cost wait (Vehicle Framework's turning events are not yet released).
- The Dubs Bad Hygiene water part is its own assembly, loaded only with both mods active.

### Save compatibility

- Safe to add to a running colony. Nothing the drivetrain or turbine works out is saved; vehicles gain a clutch part, and the turbine and water systems where their loadouts name them, as in 0.9.0. The removal button still applies before removing the mod.

## [0.10.0] - 2026-11-08 - Draft

The owner's follow-ups of 8 November 2026. Not yet tested in game.

### Added

- Vehicle systems: jump-starting. Select a colonist and right-click a vehicle whose battery is too low to start: another of your vehicles within 8 cells, with a charged battery or its engine running, lends it enough to start. The donor loses the same charge unless its engine runs. Every refusal says why (no donor near, nowhere to stand).
- Vehicle systems: per-vehicle tuning in Vehicle Framework's own settings window: battery capacity, alternator output, starter draw, engine heat, radiator cooling, thermostat, oil use, tyre wear, gearbox heat and the air brakes' compressor and leak. Each starts at the vehicle's own figure.
- For modders: a system may declare a jump-start (which quantity, raised to what, from donors within what reach); the schema check reads it.

### Balance

- New tyres or track pads take 10 chemfuel as well, a stand-in for the rubber (owner, 8 November 2026).
- The flat-battery refusal now names the jump-start first.

### Compatibility and limits

- RimWorld 1.7 only: the 1.6 build, its load folder and 1.6 in the About file are gone (owner, 8 November 2026: 1.6 is a dying version). Players on 1.6 keep the last version that had it.

## [0.9.0] - 2026-11-08 - Draft

The owner's vehicle systems (8 November 2026): real systems for every Vehicle Framework vehicle, as a generic service any mod can
extend in XML. Built overnight while the owner slept, phases A to C of the design. **Not yet tested in game.** Design:
docs/development/vehicle-systems-design.md.

### Added

- With Vehicle Framework active, on RimWorld 1.7: a battery and alternator, coolant and oil, the engine's hours and a service, tyres or tracks, brakes and a gearbox on every vehicle that fits, found by what the vehicle runs on and whether it drives, floats or flies. Air brakes and hydraulics are ready for named vehicles.
- A flat or frozen battery, a broken starter, gelled fuel (a setting), worn-through brakes or air brakes still held on stop a vehicle starting or driving, and the button or order says why and what to do.
- Overheating, low oil, worn tyres and overdue services damage parts, slow the vehicle or make parts fail; steam rises from a hot engine; a letter tells you when an engine boils over.
- Mechanics service vehicles: haulers load the supplies into the vehicle's cargo, then a constructor does the work. A spanner button asks for a service now.
- Switches for headlights and the cab heater; a parked vehicle beside a powered conduit or battery trickle-charges.
- An idling engine warms the room it stands in.
- Each vehicle's inspect text shows its systems by display mode: Plain names only what needs a look.
- Settings: which vehicles (all or only D'thorga ones, per mod, per kind), each system on or off, figures only, cold fuel, display mode, and a button that removes the added parts from a colony before the mod is removed.
- For modders: system, loadout and rules Defs, with input, condition and effect classes any mod can add to; a schema check that runs without the game (scripts/check-vehicle-schemas.py); a guide (docs/vehicle-systems-modding.md); dev actions to log a vehicle's systems, set a quantity, ask for a service and remove the parts.

### Compatibility and limits

- RimWorld 1.7 only (owner, 8 November 2026). Vehicle Framework's Steam build is for 1.6 and the 1.7 game loads it; the part is built against that and may need a rebuild when Vehicle Framework releases 1.7.
- Under Combat Extended, the added parts take the engine's cells and armour so shots can reach them.
- Aircraft get the electrical system and the service only; electric vehicles get nothing yet.
- Patches one Vehicle Framework method to add the parts and comp before Vehicle Framework resolves each design, and its save code, to drop or add parts a design has lost or gained.

### Save compatibility

- Safe to add to a running colony: vehicles gain the parts at full health and the systems start from the vehicle's surroundings.
- Switching vehicles or systems off in the settings is safe: saved vehicles drop the parts their design no longer has.
- **Removing D'thorga Framework or Vehicle Framework from such a colony needs the removal button first**, then a save. Without it, saved vehicles may fail to load (read from Vehicle Framework's code, not seen in game).

## [0.8.1] - 2026-11-07 - Draft

Found in the owner's log of 7 November 2026: 7,662 errors in one session. Not yet tested in game.

### Fixed

- The frame-work queue: when a loaded game empties the queue, a job whose map has gone could throw while being named in the log, which left the queue full and threw again on every frame. Every log line now names a job through a label that cannot throw, and the queue is emptied even when a drop fails.

## [0.8.0] - 2026-11-07 - Draft

Made with D'thorga Medical 0.7.0, its first user. No change in play on its own. Not yet tested in game.

### Added

- For modders: a per-person choice's icon, its menu and its label with the current option, from the choices registry, so every place that offers the choice draws the same thing. The pawn's own button now uses them.
- For modders: a condition oxygen helps can say how much faster it heals on supplied oxygen, and a treatment how much better than a mask it is; the treatment calls one shared method to heal. Permanent conditions never heal.
- For modders: reading another mod's setting by name, from its static settings holder, even when a source generator hides the field inside a longer name.

## [0.7.1] - 2026-11-07 - Draft

Owner's report: many settings seemed to have gone missing since the merge. **Tested in game: the owner reports every setting back (7 November 2026).**

### Fixed

- Long settings windows (D'thorga Medical's, and D'thorga Patches' since 0.8.0) lost their later settings off-screen. Past its first height the game's settings list starts a new column to the right, which in a scrolling page sits out of sight and makes the page measure itself shorter, so more settings disappeared each moment. The page now keeps one column and scrolls. No setting was lost: every choice was saved and carried over, and shows again with this fix.

## [0.7.0] - 2026-11-07 - Draft

Made with D'thorga Patches 0.8.0, its first user. No change in play on its own. Not yet tested in game.

### Added

- For modders: a work queue that runs a long job a slice per drawn frame, paused or not, within a time budget, so the game keeps responding. A job says how to start, how to take one step and when it is done; identical jobs queued behind one another can fold into one.
- For modders: a hold on a map's room and region rebuild while a slice destroys many things, released and rebuilt once at the end of the slice.
- For modders: a cost warning under a setting, in amber, and a labelled button that opens a menu of choices.

### Save compatibility

- Adds one game component that saves nothing. A save made with it logs one line about a missing class if the framework is later removed.

## [0.6.0] - 2026-11-07 - Draft

Made with the merge of seven D'thorga mods into two (owner's decision, 7 November 2026), as the pieces both merged mods needed. No change in play. Not yet tested in game.

### Added

- For modders: a one-time settings import. A mod that changes its folder or its Mod class reads the settings file its old self left, once, so a player's choices carry over. D'thorga Medical and D'thorga Patches use it.
- For modders: section headings and a scrolling page for settings windows longer than the screen. D'thorga Medical's one window, with a section per part, uses them.
- For modders: a load line can name the part of a mod it reports, for a mod made of several parts that logs one line per part.

### Compatibility and limits

- The shared build rules now stop any project's assembly being copied beside another mod's. A copy of this framework's assembly had shipped inside D'thorga Medical Equipment since 0.5.0.

## [0.5.0] - 2026-11-07 - Draft

Not yet tested in game.

### Added

- A Dubs Bad Hygiene tie-in, loaded only while that mod is active: the one shared way the D'thorga mods draw water from its plumbing,
  from a pipe under a building or against its side. Moved from D'thorga Factory Additions, whose machines and D'thorga Medical
  Equipment's oxygen electrolyser now both use it.

### Compatibility and limits

- Loads after Dubs Bad Hygiene when both are present. Without it nothing changes and nothing is logged.

## [0.4.2] - 2026-11-07 - Draft

No change in play. Not yet tested in game.

### Added

- For modders: one shared check of who counts as a patient (one of the colony's people with a flesh body, and lying down: downed or
  in bed), so each treatment mod stops writing its own. A mod's own version had let a vehicle be checked as a patient.

## [0.4.1] - 2026-11-07 - Draft

A fix from the owner's game. **The owner reports the error gone in game (7 November 2026, on 0.4.1).** The surgery bars from 0.4.0
are not yet tested in game.

### Fixed

- Selecting a dead colonist's corpse threw an error and showed it in the inspect pane instead of the corpse's details (seen in the
  owner's game on 0.3.1, 7 November 2026). The per-person care comp had also reached the corpse; it now shows nothing there, and is
  added only to things that are pawns. The error was in display code only: it could not hurt or kill anyone.

## [0.4.0] - 2026-11-07 - Draft

Not yet tested in game.

### Added

- Surgery bars for modders: a mod registers what stops surgeons operating on a patient for now, with the reason in the game's words,
  and a mod that queues operations asks whether a patient is barred and why. D'thorga Medical Equipment registers its hyperbaric
  chamber; D'thorga Triage is to read it. Nothing is saved.
- A Workshop preview picture: a generated background the owner picked (7 November 2026), with the name added by script. No change in play.

## [0.3.1] - 2026-11-06 - Draft

### Fixed

- The care-choice button (D'thorga Triage's "Doctors may operate") showed the game's red error cross: it asked for a texture the
  game does not have. It now shows the game's industrial medicine icon, as the medical care menu does. Seen in game by the owner,
  6 November 2026; the fix is not yet tested in game.

- The load line said "0 care choices registered": it is written before other mods register theirs. It now names only the comp.

### Added

- A care choice can name its own icon (iconPath on PatientPreference); a missing one falls back to the medicine icon.

### Compatibility and limits

- The owner reports the gas layer and supplied oxygen working in game through Burns and Smoke and Medical Equipment, 5 November
  2026. The health layer is not yet tested in game.
- README corrected: removal prints a line per missing map component (up to three per map), and the care choices are saved too.

## [0.3.0] - 2026-11-06 - Draft

The health layer the medical mods share, built for D'thorga Triage and adopted by Emergency Dosing 0.3.1 and Medical Equipment
0.2.2. Not yet tested in game.

### Added

- A health layer for mods whose doctors act on their own: who the doctors look after and under what care (colonist, slave,
  prisoner, guest), how dangerous a condition is and how long a patient has (from the game's own life-threatening stages, death
  clocks, lethal severities and bleeding), which doctors could operate and how well, a per-map patient cache, and a lapsing
  treatment condition. Emergency Dosing and Medical Equipment now read these from here instead of their own copies.
- Per-person care choices: every humanlike pawn carries a small comp that saves the choices D'thorga medical mods register, and
  shows a button per choice on colonists, slaves, prisoners and guests. On its own the framework registers none.
- Dev action: Framework: log care choices of selected pawns.

### Save compatibility

- The comp saves only the choices a player sets, as plain fields on the pawn; a save loads without the framework with no line
  about them. Nothing else new is saved.

## [0.2.2] - 2026-11-05 - Draft

No change in play. Not yet tested in game.

### Changed

- Moved into the gas rules Def, at the same values: the resistance that counts as sealed air without Combat Extended, and how enclosed
  a roofed cell is in a room that counts as outdoors.

## [0.2.1] - 2026-11-05 - Draft

### Fixed

- With Combat Extended, a gas mask stopped carbon monoxide. The sealed-headgear test read the smoke sensitivity stat, and
  Combat Extended's gas masks lower it to 0 with the same offset its power and recon helmets use. The figure is now rebuilt
  from the worn items that carry no gas-mask tag, so a filter mask passes carbon monoxide as the README always said and a
  sealed helmet still stops it. Found by reading the owner's save PG19 of 5 November 2026 against the code; not yet tested in game.

## [0.2.0] - 2026-11-05 - Draft

### Added

- Supplied oxygen, for medical mods: one condition marks a pawn as breathing supplied oxygen, and another marks a condition as one
  oxygen helps, so a mod can ask about either without depending on the mod behind it. It is a treatment, not a gas in the room.
  Not yet tested in game.

### Changed

- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

## [0.1.1] - 2026-11-04 - Draft

### Fixed

- The load report threw at startup ("Could not execute post-long-event action") because the Combat Extended smoke check read
  the tick manager before any game existed, and took D'thorga Burns and Smoke's load line down with it. The check now reads
  the setting directly outside a game. Seen in the owner's Player.log on 4 November 2026; the fix is not yet tested in game.

## [0.1.0] - 2026-11-04 - Draft

The owner's shared framework, started for D'thorga Burns and Smoke and recorded as a future idea on 22 October 2026. Not yet
tested in game.

### Added

- The gas layer. One way for a mod to read what a pawn breathes: smoke from Combat Extended's own smoke inhalation condition
  while that runs (its masks, sleep and breathing capacity included), otherwise the game's own smoke at the cell through the
  pawn's toxic environment resistance or a gas-mask tag; tox gas, rot stink and deadlife dust from the game's grid.
- Fires fill roofed rooms with the game's own smoke whenever Combat Extended's smoke is not in use, so burning buildings are
  smoky without it, with the game's own spread through open doors, slower fading under a roof, and its aim and sight penalties.
- The framework's own additive room gases: one mixture per room that exists only while something adds it, exchanged through
  open and closed doors and switched-on vents at the game's own door and vent rates, vented through roof gaps and doors to
  the outdoors, fading at each gas's half-life, emptied by vacuum, kept across wall changes through the cells it covered, and
  saved as cells. No air, oxygen or pressure model.
- Carbon monoxide as the first own gas: fires add it to their roofed room; it ignores filter masks and lingers for hours.
- The owner's rule as a setting, "Where gases come from": Automatic reuses existing gases and effects and uses the
  framework's own where nothing else carries one; Always own and Never override it. A gas Def can pin itself to own.
- An XML-only emitter comp, a dev-mode overlay and two dev actions.
- Shared helpers: optional mods and Defs by name, the one-line load report, the Plain, Operator and Engineer display mode and
  its settings radio, a rules-Def base whose first instance wins, the D'thorga debug category, and a cleanup service that
  removes a mod's conditions from every pawn before the mod is switched off.

### Dependencies

- None. Combat Extended is reached by name only (its setting, condition and stat names); no other mod's assembly is referenced.
- Built twice: against the 1.7.4998 reference on .NET 10, which leads, and against the 1.6 reference; the game loads its own
  version's folder (1.7/Assemblies or 1.6/Assemblies).

### Save compatibility

- Safe to add mid-game. Saves hold the room gas as cells; a save without the framework loads with no gas. Safe to remove once
  the mods that depend on it are gone; the next load prints one harmless line about a missing map component.

### Compatibility and limits

- Gas does not travel with a gravship. Rooms are found through regions and vents through a room's adjacent things on 1.7;
  not yet confirmed in play. Humanoid Alien Races and other flesh races use the same giver sets and get the same gases.
- Game-free rules pass 17 unit tests. Nothing is tested in game.
