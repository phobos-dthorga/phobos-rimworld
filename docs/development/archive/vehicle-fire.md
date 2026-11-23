# Fire on vehicles: what burns today, what does not, and what a vehicle fire should do (22 Nov 2026)

**Status (23 Nov 2026):** partly built, at the owner's word: FR1 (seeing a fire on a vehicle) in D'thorga Framework 0.220.0; FR2,
FR3, FR5, FR6 and FR9 in 0.221.0; FR10 (the extinguisher, automatic suppression and fire zones) in Framework 0.241.0 and Vehicles
0.107.0; FR4, FR8, FR11, FR12 and FR13 in Framework 0.245.0 (the owner's card of 23 Nov 2026); FR14 corrected the fires record. FR7
is unbuilt. Not yet tested in game: rows 339, 340, 386 to 388 and 395 to 399.

The owner, 22 Nov 2026, on the [nuclear strike record](../nuclear-strike-effects.md)'s row NK20 (fire that harms a vehicle): "Write its
record next", the strike research having found that Combat Extended discards the game's fire damage against every vehicle. It began
as research, as the strike record did; the owner's answers below chose what to build.

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

**Found on the way, corrected 23 Nov 2026 (FR14):** [the Vehicle Framework fires record](vehicle-framework-fires.md) said the Harrier
"reads as burning, so Vehicle Framework never auto-refuels it". It cannot: Vehicle Framework's refuel test is `IsBurning()` (VFsrc
`CompFueledTravel.cs:122-128`), and the game's `IsBurning()` counts only loose fires on a multi-cell thing, never attached ones [code].
The record now says the cause is unknown, with a fuel leak as a candidate (Vehicle Framework refuses to refuel a leaking tank), and its
draft report no longer makes the claim. The save that showed it was written again on 12 Nov, so it cannot be re-checked.

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
| **FR4** | **A tank that leaks in a fire**: heat holes a fuel tank, and the leak feeds the fire | Vehicle Framework's leak already drops a flammable puddle under Combat Extended | A fire that grows from a burning vehicle | — | Fuel tanks are what makes a vehicle fire dangerous | **Built: Framework 0.245.0**, a setting, off |
| **FR5** | **Ground fire burns a vehicle that cannot catch**: a vehicle at flammability 0 standing in fire still has its tyres and soft parts worn | A quarter of the list never catches; its tyres would still burn | Fire matters to tanks and trains too | Driving a tank through a burning field unharmed | Tracks and tyres need watching near fire | Small. **Idea**: the owner's call, since it changes other authors' vehicles |
| **FR6** | **Other factions' vehicles burn too**: fire wear in the lite tier | The lite tier runs only cook-off and bail-out | Burn out a raider's truck | Raiders' vehicles being fireproof | Incendiaries are a real tool against vehicles | Small. **Take**, cheap: one wear step on the rare tick |
| **FR7** | **Smoke and heat in the cab**: the people aboard choke and overheat in a burning vehicle | Knocked-out row C3 and hotboxing's cab air; D'thorga Medical's smoke needs people on the map | Lose a crew to a fire | Sitting in a burning cab | Getting out of a burning vehicle matters | **Waiting** on the cab-air model (the owner's answer of 20 Nov 2026) |
| **FR8** | **Getting out**: the player's crew bail out of a burning vehicle by themselves, as other factions' do, behind a setting | Bail-out exists for other factions, with an alert for the player | Crews that save themselves | Losing a crew to an order they never got | Less micromanagement in a fire | **Built: Framework 0.245.0** (see below) |
| **FR9** | **Extinguishing reaches a vehicle under Combat Extended**: firefoam and extinguish damage that hits a vehicle shrinks its fire | Combat Extended skips Vehicle Framework's step | Foam that works | — | Firefoam poppers near the motor pool | Small; a temporary-patches row (Combat Extended's gap). **Take** |
| **FR10** | **A vehicle extinguisher**, an accessory that uses up its charges, and **automatic suppression**, an upgrade for armoured and military vehicles | Accessories use up something real (the owner's rule); real armoured vehicles carry it | Put a fire out from inside | — | Suppression is a reason to upgrade | Medium; needs art. **Idea** |
| **FR11** | **Tyres that reignite and burst**: a burning tyre is hard to put out and may burst, hurting people near | NTSB and MSHA sources | — | — | Keep clear of a burning vehicle | **Built: Framework 0.245.0**, two settings |
| **FR12** | **Glass lets fire in**: a vehicle's glass (the [glass record](vehicle-glass.md)) cracks in a fire, and a cab with glass gone takes the fire's heat faster | Real fires enter through the windscreen | — | — | Ties the two records | **Built: Framework 0.245.0** (the cracking came with the glass, 0.238.0) |
| **FR13** | **A burnt-out hull**: a vehicle destroyed by fire leaves a blackened hull worth less salvage | Hulls exist | — | — | — | **Built: Framework 0.245.0**, without the blackening |
| **FR14** | **Correct the fires record's refuel cause** | See "Found on the way" | — | — | — | **Done, 23 Nov 2026** (records only) |

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
| FR4 (a leaking tank feeds the fire) | A vehicle, faster; people and vehicles near, by a spreading ground fire | High, the agent's grade (23 Nov 2026) | Its own setting, off (built 0.245.0) |
| FR5, FR6 (fireproof vehicles and raiders' vehicles burn) | Parts of tanks and trains | Moderate | On, each with a setting |
| FR8 (crews get out by themselves) | Nothing; it saves people. A crew may leave a gun it was firing | Low | On (built 0.245.0) |
| FR11 (tyres relight) | The vehicle burns again, in sight | Moderate, the agent's grade | On (built 0.245.0) |
| FR11 (tyres burst) | People near a burning vehicle | High | Off by the rule (built 0.245.0) |

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
5. ~~The fires record's refuel cause, against the save it came from (FR14).~~ Done 23 Nov 2026: the save was rewritten, so the code settled it.
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

## FR10: an extinguisher and automatic suppression (design, 22 Nov 2026) [A]

The owner's "design FR10". Nothing is built. Every figure is authored; the real figures behind it are in
[what the real thing does](#what-the-real-thing-does).

### FR10(a): the vehicle extinguisher, an accessory

- **What it is:** a hand extinguisher clipped in the cab, fitted from the fittings window like our other accessories. **It uses up
  something real** (the owner's rule for accessories): its charges.
- **What it does:** a button on a burning vehicle, **Use the extinguisher**: someone aboard, or a colonist beside it, puts out or
  shrinks the fire on the vehicle, through `AttachedFires`. Each use spends one charge. **Three charges** [A] for a dry-powder unit.
- **The real limit, kept:** a hand extinguisher knocks a fire down but may not put it out, and plain water lets a fire flash back
  within a second (McCormick and Clauson, 2006). So a use **shrinks the fire by a set amount** [A: as much as one firefoam burst],
  enough to put out a small fire and to buy time against a big one.
- **Refilling:** at the vehicle parts bench, from a charge of chemicals (the game's firefoam popper's ingredients as the stand-in,
  to be read before building), by the player's people. Nothing refills itself.
- **Weight:** about 9 kg [A], as carried mass under the accessories' rule.
- **Other factions:** their crews do not use one (they bail out); a captured vehicle keeps its extinguisher and its charges.
- **Art:** one icon for the fitting and its button, made with the next art batch.

### FR10(b): automatic suppression, an upgrade

- **What it is:** an upgrade node for armoured and military vehicles (our armour-kit columns), fitted with the armoured cab as
  real crew-compartment systems are.
- **What it does:** when a fire takes hold on the vehicle, **it fires by itself, once**, and puts the fire out at once, as real
  systems do in a fraction of a second (161 to 384 ms with Halon, McCormick and Clauson). Then it is **spent** until recharged.
- **What it costs:** its parts (a bottle, nozzles, sensors) through the parts bill, weight as carried mass, and a **recharge** at
  the parts bench after each discharge [A]. A spent system shows on the Health tab and in an alert, as a spent spall liner does.
- **The crew:** a real Halon discharge is safe for crews to breathe at the levels used; no harm is modelled [A].
- **Which vehicles:** ours with the armoured cab; other authors' armoured vehicles may get it through the extension the spall liner
  and the glass also want (one "fitted equipment" extension for all three, built once).

### The four questions

| Behaviour | Lets | Stops | Changes |
| --- | --- | --- | --- |
| The extinguisher | Put out a small fire on the spot | Losing a vehicle to a fire nobody could fight from inside it | Charges are carried and refilled; a fire is fought before it grows |
| Automatic suppression | Shrug off one fire in a fight | Armoured vehicles burning out from a single incendiary | A suppressed vehicle needs a recharge before the next fight |

**What outside the vehicle notices:** the colony, through the alert for a spent system; the map, where a fire goes out; raiders,
whose incendiaries meet a vehicle that may put itself out once.

**Risk:** both only protect the player's things: **none** to grade. No setting is needed for either, beyond fitting it or not.

**To read before building:** the firefoam popper's recipe, for the refill's stand-in; how our accessories' charges are saved (the
water tank's store is the pattern); whether the automatic system should also put out a fire on the ground under the vehicle (a real
system protects only the compartment it is in: proposed no).

**Questions for the owner, as cards when the owner wants it built:** three charges or another figure; whether the automatic system
fires only for the crew compartment or the engine too (real vehicles have both, separately); and whether other authors' armoured
vehicles may be fitted with it.

**The owner's answers (cards, 23 Nov 2026):** **three charges**; **crew and engine, as two upgrade nodes**, each firing once and
recharging, as real vehicles have; and **yes, other authors' armoured vehicles may be fitted**, through the extension the liner and
the glass share.

**More answers (cards, 23 Nov 2026):** the extinguisher's charges stay with it, and **a mechanic refills it at the vehicle**, as every
repair of ours is done (the owner's worry: "that it won't be refilled like the flares", whose hauler top-up is an open fault); **fire
zones with spread**, "as long as it's not too much of a performance concern"; a **suppressant bottle** to recharge; other authors'
vehicles count as armoured **by armour or by a mark** ("Both?"). Then, by the owner's request, **the button sends someone**: with
nobody aboard, the nearest free colonist runs to use it, the right-click kept for choosing who.

### What was built (Framework 0.241.0, Vehicles 0.107.0)

- **Fire zones** (`FireFightingRule`, `FireFighting`; rules in `FireFighting.xml`): a fire takes the zone of the part hit within three
  game minutes before it caught (roles named in the rules: the engine and what sits by it, the cab and glass, the running gear), else the
  engine bay [A]. It spreads from the running gear into the engine bay after 0.4 game hours and from the engine bay into the crew
  compartment after 0.25 [A]. The game has one fire on a vehicle; the zones are ours, saved on `CompPhobosFireFighting`. Cost: a postfix
  on `FireUtility.TryAttachFire` only notes the vehicle, handled on the next tick by a game component; the rest is read on the rare tick,
  only while the vehicle burns.
- **The extinguisher** (FR10(a)): a fitting on a bracket every vehicle with a loadout gets (trailers apart), three charges, each use
  knocking the fire down by 0.75 of the game's fire sizes [A] (a small fire goes out). Its charges stay with the vehicle when it is
  taken off, so refitting is no free refill. **Refilled by the repairs** (a refill item, made at the parts bench, hauled in and put in
  by a mechanic), as relining is: listed by itself, never a hauler's top-up. The button sends the nearest free colonist with nobody
  aboard (`Work/SendNearest`, in the Framework core for any button whose work needs a person).
- **Automatic suppression** (FR10(b)): two nodes on our five armoured-cab trucks, `PhobosSuppressionEngine` and `PhobosSuppressionCrew`
  (`PhobosSuppressionUpgrade`, a zone each; after the armoured cab and Firefoam; a bottle, 15 steel, 2 components, 30 kg carried). Each
  discharges when its own zone burns, puts that zone out (the fire shrinks by the share of zones put out, or goes out), and is spent; the
  repairs recharge it with a bottle. Its part on the Health tab reads ready or spent; the **Fire protection spent** alert, a message and
  the vehicle's logbook say when it fires.
- **Other authors' armoured vehicles**: both systems as fittings on mounts appended at start-up, when the vehicle's outer plates reach
  0.8 sharp armour (8 under Combat Extended) [A], or `PhobosFittedEquipmentExtension.armoured` marks it in or out. The spall liner
  (`lined`) and the glass (`glass`, `noGlass`) share the extension since Framework 0.244.0.
- **Other factions:** any faction's vehicle with a system fitted puts its fire out; their crews do not use extinguishers. Zones are
  kept for every burning vehicle.
- **Known limit:** a fire in the running gear is covered by no system until it spreads, as with real systems.

## What was built in the second round (Framework 0.245.0) [A]

The owner's card of 23 Nov 2026: "FR4 + FR11 + FR13, FR8: crews bail out, FR12: glass lets fire in, FR14: correct the record". The
grades and defaults below are the agent's, by the designs rule (on, unless high risk), for the owner to revise.

- **FR8, your crews get out** (`CrewBailOut.PlayerRareTick`, on the full tier's rare tick after the suppression look): a burning vehicle
  of the player's, with people aboard, empties once it has stopped (Vehicle Framework's pather not moving) and is not airborne. The
  fire is read from the fires themselves, so a fire a system has just put out empties nothing. Only fire: a dead hull that does not
  burn stays the player's call, with the Crew in danger alert, as the owner chose on 16 Nov 2026. The knock-out rules' `crewsBailOut`
  still switches every faction's bail-out off. Setting **Your crews get out of a burning vehicle**, on (risk low: it saves people; a
  crew may leave a gun it was firing). **Outside the vehicle:** a message, and the crew on the map, in the fire's way.
- **FR12, glass lets fire in** (XML only, the cab's `fireThroughGlass` flow): the glass already cracked and broke in a fire (Framework
  0.238.0, VG3, through the fire wear table), but a cab with its glass gone then met the outside air nine times as fast, so it ran
  cooler in a fire than a sealed cab, the wrong way round. The fire now comes in as freely as the air: the same eightfold figure on the
  fire's heat, so the open cab reaches the sealed cab's heat (about 40 degrees C above the air at full fire) nine times as fast [A]. No
  setting of its own: it follows **Vehicles have glass**, which already switches every effect of the glass, and it only hastens a heat
  the cab already took [A]. **Outside the vehicle:** the crew's heat, read by the game for everyone aboard.
- **FR4, a leaking tank feeds the fire** (`LeakFire`, on both tiers' rare tick; figures on the fire rules): the fire already wears the
  fuel tank role (FR3), and Vehicle Framework already leaks from a tank part worn below its own threshold (`Reactor_FuelLeak`). While a
  vehicle burns and that leak runs with fuel in the tank, the leak catches beneath it about three times an hour at full fire, scaled by
  the fire's size: a ground fire of 0.6 on a random cell under the vehicle, taking 2 units of fuel [A]. The game's own fire rules then
  spread it and let it relight the vehicle. Any faction's vehicle. Setting **Leaking fuel feeds a vehicle fire**, **off**: risk high
  [A], since a spreading ground fire can take people and other vehicles near, and the player cannot stop the leak once it runs.
  **Not read:** whether every loadout's fuel tank role is the part carrying Vehicle Framework's leak reactor; where it is not, the tank
  wears in a fire but never leaks, and nothing catches. **Outside the vehicle:** a message, the ground fire, and anyone in its way.
- **FR11, burning tyres** (`TyreFire`, asked by `FireFighting.Look` on the rare tick; figures on the fire rules): only a wheeled vehicle,
  its tyres the parts of the `Wheels` role; a design with a part keyed "track" has none (the one check, `VehicleFittings.TrackedDesign`,
  now shared with the fittings). **Relighting:** a fire that had reached the running gear zone leaves the tyres smouldering for two hours
  once it goes out (saved, `PhobosFireFighting_SmoulderUntil`), lighting again about once an hour (a fire of 0.3, starting in the running
  gear) unless rain heavier than 0.25 falls on a vehicle under open sky [A]. Setting **Burnt tyres can light again**, on (risk moderate
  [A]: the vehicle burns again, in sight). **Bursting:** while the running gear burns, each sound tyre bursts about 0.6 times an hour at
  full fire, scaled by the fire's size, at most one a rare tick: the part goes to nothing and a blast of 8 Bomb damage reaches 1.9 cells
  from that tyre's own cell, sparing the vehicle [A]. Setting **Burning tyres can burst**, off (risk high: people near). Any faction's
  vehicle. **Outside the vehicle:** messages, the fire again, the blast and anyone in it.
- **FR13, a burnt-out hull** (`VehicleHulls`, `SalvageRule.BurntShare`): a vehicle that dies with a fire on it or under it leaves a hull
  marked burnt out (saved on the hull, `PhobosHull_BurntOut`): named "burnt-out", and giving half the salvaged parts, an eighth of the
  vehicle's worth instead of a quarter [A]: a fire takes the rubber, wiring, seals, glass and plastics, and softens springs and castings.
  **Not built: the blackening.** The hull is drawn as its vehicle by Vehicle Framework; a darker picture needs a draw patch or art of
  its own, for the owner to decide. No setting of its own: it lessens a reward and puts nothing of the player's at risk [A]; it follows
  **Destroyed vehicles leave a hull**. **Outside the vehicle:** the hull's name and text, and the parts the player gets.

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
