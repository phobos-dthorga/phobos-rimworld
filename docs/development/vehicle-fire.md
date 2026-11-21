# Fire on vehicles: what burns today, what does not, and what a vehicle fire should do (22 Nov 2026)

**Status (22 Nov 2026):** a research record. It changes no code, Def or setting, and every numbered idea in it (FR1 to FR14) is
unbuilt. Nothing here has been tested in game.

The owner, 22 Nov 2026, on the [nuclear strike record](nuclear-strike-effects.md)'s row NK20 (fire that harms a vehicle): "Write its
record next", the strike research having found that Combat Extended discards the game's fire damage against every vehicle. Research
and record only, as the strike record was.

**How to read the marks.** **[code]** read in code or Defs on 22 Nov 2026, never run. **[P]** a primary source; **[S]** a secondary
one. **[I]** our inference. **[A]** an agent's choice or authored figure, for the owner to revise. **[not read]** says so plainly.
`V/` is `src/PhobosFramework.Vehicles/`; **G:n** a line of the game's decompile; **VFsrc** Vehicle Framework's public source at
commit 026420b5; **CEsrc** Combat Extended's at d847807b.

**Who read what.** A research agent read the code with the repository's tools and the public source clones, and the web sources
through a further agent, on 22 Nov 2026. The agent writing this record re-read the lines marked as re-read under
[Sources](#sources); every other place is as the survey reported it. **Re-read a line before building on it.**

## Read this first

- **Fire does reach the player's own vehicles today, through our systems, not the game's damage.** A fire on one of the colony's
  vehicles wears its radiator by half its health an hour, its tyres by 0.4 and its battery by 0.3, and heats its coolant, oil and
  cab, by setting part health directly, outside any armour step **[code, re-read]**. Ammunition aboard can cook off and the crew of
  other factions' vehicles bail out. The strike record's "fire does nothing to any vehicle" was too strong and is corrected there.
- **The game's own fire damage does nothing to any vehicle part under Combat Extended**: it carries no armour penetration, and
  Combat Extended's vehicle armour step discards such a hit **[code]**. Other factions' vehicles run our systems' lite tier, so for
  them fire does nothing but cook-off and bail-out.
- **A fault of ours: six of our checks cannot see a fire on a vehicle.** The game's `IsBurning()` counts a fire attached to a thing
  only when the thing is one cell; for anything larger it looks only for fires on the ground under it **[code, re-read: G:595987-596009]**.
  Our checks use it, so a fire *on* a vehicle goes unseen: **the tanker's relief valve does not open in a fire on the tanker**
  (`V/Tanks/CompPhobosTank.cs:442`), and camp bunks, deck tents, service jobs and bay trips carry on beside it
  (`Camp/CompPhobosCamp.cs:250, 297`; `Cabins/DeckTents.cs:75`; `Jobs/ServiceJobs.cs:47`; `Service/BayTrips.cs:134`) **[code, re-read]**.
- **A quarter of the vehicles on the list can never catch fire**: about 27 of 83 vehicle Defs have flammability 0, every RimThunder
  vehicle (the M1A2 included), every Trains of the Rim vehicle and a few others **[code, surveyed, from the patched-Defs file of
  19 Nov]**. Our fire wear never starts on them.
- **Nobody aboard is touched by a fire**: no burns, smoke or carbon monoxide, because people aboard are not on the map **[code]**.
  Only our cab's heat reaches them.
- **Most ways of putting a fire out still work on a vehicle** (rain, colonists beating it out, sprinklers, our water drop). Only
  extinguishing that arrives *as damage to the vehicle* is lost under Combat Extended **[code]**.
- **The ideas are at [the short list](#the-short-list).**

## What the code does today

### How a fire gets on a vehicle [code, surveyed unless marked]

- **A fire attaches** to a pawn that can burn (flammability 0.01 or more) and carries the game's attachment part. The fire sits at
  the vehicle's position (G:595961-595976, G:595869-595888).
- **A ground fire under any cell catches it fast**: once the ground fire passes size 0.4 it may attach a fire to the vehicle every
  pass, at a chance that is near certain a second for flammability 0.3 or more. Most vehicles on the list are 0.5 (Vanilla Vehicles
  Expanded's), ours 0.2 to 0.8, the Harrier 0.6 (G:595712-595714, G:595859-595865).
- **An attached fire grows** and, above size 1, spreads to nearby cells. Under Combat Extended its growth follows the wind, and a fire
  on a pawn with no clothing, which every vehicle is, never burns out by itself [I, from CEsrc `Harmony_Fire.cs:152-179`]. Rain puts it
  out in the open.
- **Its damage** each pass is Flame at penetration 0 (G:595799-595822), raised by Combat Extended by the pawn's body size.

### Why that damage does nothing [code]

Combat Extended's armour step replaces Vehicle Framework's for every vehicle while "Use New Vehicle Armor" is on (its default, the
owner's setting). In it, a hit that is not sharp and whose penetration is under half the plate's armour does nothing; a part with no
armour is tested against its own bulk the same way (CEsrc `VehicleArmorPatch.cs:383-442`). Flame and Burn are heat hits at
penetration 0. **Fire's own damage never harms a vehicle part.** With that setting off, Vehicle Framework's own roll would let it
wear one part at one cell [I].

### What our systems already do [code, re-read]

`CompPhobosVehicleSystems.FireShare` (`V/Systems/CompPhobosVehicleSystems.cs:2277-2304`) reads the largest fire attached to the
vehicle, as a share of the largest a fire grows; `Input_Fire` feeds it to the systems' Defs, and part wear goes through `WearRole`
(`:2309-2319`), which sets health directly.

| System | What a fire does (authored) |
| --- | --- |
| Cooling | Coolant +180 °C an hour; the radiator loses half its health an hour |
| Chassis | The tyres lose 0.4 of their health an hour |
| Electrical | The battery loses 0.3 an hour |
| Lubrication | Oil +60 °C an hour where there is no coolant |
| Electric drive | The traction pack +30 °C an hour |
| Cabin | The cab +80 °C an hour, to at most 80 °C |

- **A mismatch to know** [code, surveyed]: the comments say "at full fire", but each wear stage starts at a share of 0.01 and wears at
  its flat rate, so any fire wears at the full rate; only the heating scales with the fire's size.
- **Full detail only.** Other factions' vehicles run the lite tier: cook-off and crew bail-out, nothing else.
- **Cook-off** (`V/Service/CookOff.cs`, on by default): ammunition aboard a burning vehicle may go off as a blast, any faction's.
  Combat Extended's own cook-off is only for ammunition lying on the map.
- **Crew bail-out** (`V/Service/CrewBailOut.cs`): other factions' crews leave a burning vehicle; the player gets an alert.
- **Fuel leaks**: Vehicle Framework's leak, from a fuel tank below 80 %, drops fuel puddles that Combat Extended makes very
  flammable, so a holed tank feeds a fire that is already there. Our own holed-tank condition burns fuel away without a puddle.

### What is missed

| Gap | Whose | What it means in play |
| --- | --- | --- |
| **`IsBurning()` cannot see a fire on a vehicle** (six of our checks) | **Ours**, using the game's helper | A tanker on fire keeps its relief valve shut; sleepers stay in a burning truck's bunk; service and bay trips go to it |
| Fire's damage, under Combat Extended | Combat Extended, meeting the game's figures | No part is harmed by the game's fire on any vehicle |
| Other factions' vehicles | Ours, by design (the lite tier) | A raider's truck burns without harm except cook-off |
| Flammability 0 on a quarter of the list | Those mods' authors | Those vehicles never catch fire, ground fire or not |
| People aboard | Vehicle Framework, by design | No burns, smoke or carbon monoxide aboard a burning vehicle |
| Extinguishing as damage, under Combat Extended | Combat Extended skips Vehicle Framework's step | Firefoam that hits only the vehicle, not the fire, puts nothing out |

**Found on the way:** [the Vehicle Framework fires record](vehicle-framework-fires.md) says the Harrier "reads as burning, so Vehicle
Framework never auto-refuels it". Vehicle Framework's refuel test is `IsBurning()` (VFsrc `CompFueledTravel.cs:122-128`), which for a
5 by 7 jet does not see attached fires [code, surveyed]. Something else may have held the refuelling; that record's cause is now in
question. Not re-checked against the save.

### Putting a fire out today [code, surveyed]

- **Works:** rain; colonists beating it out (the game lets them fight a fire on their own faction's pawn); Dubs Bad Hygiene's
  sprinklers; our water drop and `AttachedFires.PutOut`; firefoam whose blast reaches the fire's own cell, which Vehicle Framework
  allows only on a vehicle's edge cells (every cell of a vehicle two wide).
- **Lost under Combat Extended:** extinguishing that arrives as damage to the vehicle (Vehicle Framework's step, which Combat
  Extended's prefix skips).
- **No mod on the list** adds a vehicle extinguisher or automatic suppression.

## What the real thing does

Gathered by a research agent on 22 Nov 2026; "opened" means the agent opened and quoted the page. Links are under [Sources](#sources).

- **Where vehicle fires start** (US Fire Administration, *Highway Vehicle Fires 2014–2016*, 2018) **[P]**: 62 % in the engine, running
  gear or wheel area; the first thing alight was most often wiring insulation (29 %), then flammable liquids in the engine area (18 %),
  then tyres (6 %). Fuel tanks start 2 % of fires but 12 % of fatal ones; collisions 5 % of fires but 60 % of deaths.
- **How fast** **[P, abstracts]**: flames reached the cabin 10 to 24 minutes after an engine-compartment fire began, but 0.5 to 3
  minutes after a petrol pool fire under the car (Tewarson, Quintiere and Purser, SAE 2005-01-1555); "thermal hazard appears to be more
  critical than the toxic hazard". Fire enters through the windscreen, first where it is already damaged (Papageorge and Colwell, SAE
  2024-01-2502).
- **Tyres** **[P]**: rubber breaks down from about 350 to 400 °F and ignites near 800 °F; the US National Transportation Safety Board
  found no practical extinguisher puts a burning tyre fully out, and in its test the tyre fire restarted twice (NTSB HAR-07/01). A loader
  tyre exploded in an equipment fire and killed a firefighter (US Mine Safety and Health Administration, 2024).
- **Wiring** **[P, an approximation from power-plant cable guidance]**: thermoplastic insulation is damaged from about 205 °C,
  thermoset from about 330 °C (NEI and NRC FAQ 16-0011).
- **Fuel tanks** **[P]**: a plastic tank must survive a minute in direct flame without leaking (UN ECE Regulation 34's fire test); plastic
  tanks fail by leaking at low pressure, steel ones build vapour pressure and release it suddenly (SwRI, SAE 2005-01-1561).
- **People inside** **[P]**: in Australian bushfire tests, a car became untenable within 1.5 minutes when burning underneath, and on
  average after 10.2 minutes from its smoke (Bushfire CRC and AFAC, *Fire Note* 69, 2010).
- **Military vehicles** **[P]**: crew compartments carry automatic suppression that puts a fire out in 161 to 384 milliseconds with
  Halon 1301, and plain water sprays let a fire "typically reflash within one second" (McCormick and Clauson, US Army TARDEC, at the
  NIST Halon Options conference, 2006).
- **Not verified:** the DTIC reports on armoured-vehicle fires, hydrogen cyanide yields from car interiors, tyre heat-flux figures,
  hose burst times.

## The ideas

Every row is unbuilt. Verdicts are the agent's recommendations **[A]**: **Take**, **Idea**, **Hold**, **Waiting**.

| # | Idea | What the reads found | Lets | Stops | Changes | Size and verdict |
| --- | --- | --- | --- | --- | --- | --- |
| **FR1** | **See a fire on a vehicle.** One helper of ours, a vehicle burning when a fire is attached to it or under it, used by the six checks | The game's helper misses attached fires on anything larger than a cell | A tanker's valve opens in a fire; sleepers wake; service jobs keep clear | Sleeping through a truck fire | Nothing new to learn; things behave as they say | Small. **Take**: it is a fault in our code |
| **FR2** | **Fire wear by the fire's size**, as the comments always said | Wear starts at any fire and runs at its full rate | A small fire put out quickly costs little | Losing a radiator to a spark | Fighting a fire early pays | Small. **Take** |
| **FR3** | **More parts burn**: fuel tank, hydraulic lines, belts, brakes and the cab's fittings, by their roles; wiring through the shorted-wiring condition, since there is no wiring role | Our roles and conditions exist; the US statistics put wiring and fluids first | A long fire ruins a vehicle | A burnt-out truck needing only a radiator | Fire is costly enough to prevent | Small to medium. **Take** |
| **FR4** | **A tank that leaks in a fire**: heat holes a fuel tank, and the leak feeds the fire | Vehicle Framework's leak already drops a flammable puddle under Combat Extended | A fire that grows from a burning vehicle | — | Fuel tanks are what makes a vehicle fire dangerous | Small. **Idea** |
| **FR5** | **Ground fire burns a vehicle that cannot catch**: a vehicle at flammability 0 standing in fire still has its tyres and soft parts worn | A quarter of the list never catches; its tyres would still burn | Fire matters to tanks and trains too | Driving a tank through a burning field unharmed | Tracks and tyres need watching near fire | Small. **Idea**: the owner's call, since it changes other authors' vehicles |
| **FR6** | **Other factions' vehicles burn too**: fire wear in the lite tier | The lite tier runs only cook-off and bail-out | Burn out a raider's truck | Raiders' vehicles being fireproof | Incendiaries are a real tool against vehicles | Small. **Take**, cheap: one wear step on the rare tick |
| **FR7** | **Smoke and heat in the cab**: the people aboard choke and overheat in a burning vehicle | Knocked-out row C3 and hotboxing's cab air; D'thorga Medical's smoke needs people on the map | Lose a crew to a fire | Sitting in a burning cab | Getting out of a burning vehicle matters | **Waiting** on the cab-air model (the owner's answer of 20 Nov 2026) |
| **FR8** | **Getting out**: the player's crew bail out of a burning vehicle by themselves, as other factions' do, behind a setting | Bail-out exists for other factions, with an alert for the player | Crews that save themselves | Losing a crew to an order they never got | Less micromanagement in a fire | Small. **Idea** |
| **FR9** | **Extinguishing reaches a vehicle under Combat Extended**: firefoam and extinguish damage that hits a vehicle shrinks its fire | Combat Extended skips Vehicle Framework's step | Foam that works | — | Firefoam poppers near the motor pool | Small; a temporary-patches row (Combat Extended's gap). **Take** |
| **FR10** | **A vehicle extinguisher**, an accessory that uses up its charges, and **automatic suppression**, an upgrade for armoured and military vehicles | Accessories use up something real (the owner's rule); real armoured vehicles carry it | Put a fire out from inside | — | Suppression is a reason to upgrade | Medium; needs art. **Idea** |
| **FR11** | **Tyres that reignite and burst**: a burning tyre is hard to put out and may burst, hurting people near | NTSB and MSHA sources | — | — | Keep clear of a burning vehicle | Small. **Idea** |
| **FR12** | **Glass lets fire in**: a vehicle's glass (the [glass record](vehicle-glass.md)) cracks in a fire, and a cab with glass gone takes the fire's heat faster | Real fires enter through the windscreen | — | — | Ties the two records | **Waiting** on the glass build |
| **FR13** | **A burnt-out hull**: a vehicle destroyed by fire leaves a blackened hull worth less salvage | Hulls exist | — | — | — | Small. **Idea** |
| **FR14** | **Correct the fires record's refuel cause** | See "Found on the way" | — | — | — | A read of the save, then one line. **Take** |

### What outside the vehicle notices

- **The colony:** the alerts our systems already raise, an alert for a burning vehicle with people aboard (FR8), and the logbook.
- **The map:** fuel puddles and spreading fire (FR4), a burst tyre (FR11), hulls (FR13).
- **Other factions:** raiders' vehicles burn (FR6), so incendiaries become a way to stop them.
- **Doctors:** burns and smoke once the cab air exists (FR7).

## Risk and settings

Grades are the agent's proposals **[A]** until the owner confirms them.

| Behaviour | What the player can lose | Proposed grade | Default by the rule |
| --- | --- | --- | --- |
| FR1 (seeing the fire) | Nothing: it makes things behave as they claim | None: a fix | Always on |
| FR2, FR3 (wear by size, more parts) | Parts, over minutes, with the fire visible | Moderate | On, under the existing fire wear |
| FR4 (a leaking tank feeds the fire) | A vehicle, faster | Moderate to high | Its own setting; the owner to grade |
| FR5, FR6 (fireproof vehicles and raiders' vehicles burn) | Parts of tanks and trains | Moderate | On, each with a setting |
| FR8 (crews get out by themselves) | Nothing; it saves people | Low | On |
| FR11 (tyres burst) | People near a burning vehicle | High | Off by the rule |

## Other factions' vehicles

| Where | What happens |
| --- | --- |
| **A raid on us** | Today a raider's burning vehicle takes no harm but cook-off; its crew bail out. FR6 lets the fire wear it |
| **Parked at a base we attack** | The same, without a crew to bail out |
| **One we capture** | It keeps its fire damage and its flammability; a fireproof design stays fireproof unless FR5 is taken |

**Cost with several on a map:** FR1 is a lookup where a check already runs. FR6 adds one wear step on the rare tick, only for a
burning vehicle. Nothing runs for a vehicle that is not on fire.

## Saves

Nothing is built, so nothing changes in any save. Every idea writes only part health, which Vehicle Framework saves, or conditions
of ours that already exist; FR10 would add an accessory's charges, saved like our other accessories.

## To read or prove before building

1. Where vehicles get the game's attachment part, and whether every design has it.
2. Which figure Combat Extended's body-size multiplier scales in a fire's damage.
3. Whether a hot cab reaches the game's heatstroke for the people aboard.
4. Whether a destroyed vehicle's hull burns like a building.
5. The fires record's refuel cause, against the save it came from (FR14).
6. The patched-Defs file is from 19 Nov: flammability figures to be read again from a fresh one.

## The short list

1. **FR1, seeing a fire on a vehicle**: a fault in our code; small; nothing to decide but the word.
2. **FR2, FR3 and FR6**: fire wear that scales, reaches more parts, and reaches other factions' vehicles.
3. **FR9**: extinguishing that works on vehicles under Combat Extended.
4. **Then:** FR4, FR5, FR8 and FR14; FR7 and FR12 wait for the cab air and the glass.

## Questions for the owner

To be asked as cards once this record has been read; the agent's recommendation is the first option of each.

1. **FR1, the fault:** fix it now; or with the rest.
2. **FR2, FR3, FR6:** build them; or some of them.
3. **FR5, fireproof vehicles:** let ground fire wear their soft parts; or respect their authors' flammability.
4. **FR9 and FR10:** fix extinguishing under Combat Extended now, and design the extinguisher accessory; or either alone.

## The owner's answers (cards, 22 Nov 2026)

| # | Question | The owner's answer | The agent's recommendation |
| --- | --- | --- | --- |
| 1 | FR1, the fault | **Fix it now** | The same |
| 2 | FR2, FR3, FR6 | **All three** | The same |
| 3 | FR5, fireproof vehicles | **Yes, soft parts only**: their authors' flammability stays, but a ground fire under them wears tyres, tracks and hoses, behind a setting | The same |
| 4 | FR9, FR10 | **Fix FR9, design FR10** | The same |

**What follows:** FR1 is built first, on its own. Then FR2, FR3, FR5, FR6 and FR9 as one piece, each that risks the player's things
with its setting; FR10 gets a short design of its own (an extinguisher accessory that uses up its charges, and an automatic
suppression upgrade). FR4, FR7, FR8 and FR11 to FR14 stay ideas, FR7 and FR12 waiting on the cab air and the glass.

## Who chose what

- **Owner (22 Nov 2026):** the record (the strike record's NK20), research and record only; then the answers above.
- **Agent:** every row, verdict and grade; which lines were re-read.

## Sources

**Code.** Re-read by the agent writing this record: the game's `FireUtility.IsBurning` (G:595987-596009) through
`scripts/query-code.py`; our `CompPhobosVehicleSystems.FireShare` and `WearRole` (`V/Systems/CompPhobosVehicleSystems.cs:2277-2319`),
the tanker's vent (`V/Tanks/CompPhobosTank.cs:434-446`), the six `IsBurning()` call sites, and the start of `V/Service/CookOff.cs`.
Everything else is as the survey reported it, from the game's decompile, Combat Extended's and Vehicle Framework's public sources, our
`Systems_*.xml` and the patched-Defs file of 19 Nov 2026.

**The real thing,** gathered and opened by a research agent on 22 Nov 2026:

- [US Fire Administration, *Highway Vehicle Fires (2014-2016)*, Topical Fire Report Series 19(2), 2018](https://www.usfa.fema.gov/downloads/pdf/statistics/v19i2.pdf).
- Tewarson (FM Global), Quintiere (University of Maryland) and Purser (BRE), SAE Technical Paper 2005-01-1555: the abstract.
- Papageorge and Colwell, "Evaluating Fire Propagation into the Passenger Compartment via Full-Scale Burn Testing", SAE Technical
  Paper 2024-01-2502 ([DOI](https://doi.org/10.4271/2024-01-2502)): the abstract.
- US National Transportation Safety Board, Highway Accident Report NTSB/HAR-07/01 (the Wilmer, Texas, motorcoach fire).
- US Mine Safety and Health Administration, fatality alert of 2024 on a loader tyre explosion.
- NEI and US NRC, FAQ 16-0011, cable damage thresholds (power-plant guidance, used here as an approximation).
- UN ECE Regulation No. 34, the plastic fuel tank fire test; Southwest Research Institute, SAE Technical Paper 2005-01-1561.
- Bushfire CRC and AFAC, *Fire Note* 69 (2010), on cars in bushfire burnovers.
- McCormick and Clauson (US Army TARDEC), crew-compartment fire suppression, NIST Halon Options Technical Working Conference, 2006.

These were opened by the agent but their exact pages and links were not carried into this record; they are named so they can be found
again, and none is quoted beyond what is above. **Not verified, so not used:** the DTIC reports on armoured-vehicle fires, hydrogen
cyanide yields from car interiors (Lönnermark and Blomqvist, 2006), tyre critical heat-flux figures, hose burst times.
