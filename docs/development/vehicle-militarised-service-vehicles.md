# Militarised service vehicles: research and design (19 Nov 2026)

**Status (19 Nov 2026):** a design record, partly built: every armour kit is built (Vehicles 0.80.0 and 0.86.0); the guns, smoke and remote station are not. Its prerequisite, the trucks' armour by class (MV2), is in Framework 0.154.0 and Vehicles 0.76.0; the kits with no picture (engine and tank guards, run-flat tyres, EMP hardening) and the truck armour research are in Vehicles 0.80.0 and Framework 0.158.0. The drawn kits, the guns and the under-fire switch are not built, no picture is drawn, and nothing here is tested in game. The owner answered every question it raised the same day (below), the guns each truck may mount among them.

How vehicle damage works now: [systems/vehicle-damage.md](systems/vehicle-damage.md); this record keeps its history.

The owner's request, 19 Nov 2026, with the answer that enemies end a [gathering run](vehicle-gathering-runs.md): "perhaps we could
offer upgrades where we militarize some or all of our service vehicles? Either with advantages only or some disadvantages as a result
to boot?" Then: "Go ahead and research the militarised service vehicles." This record is that research and the design it leads to.

**How to read the marks:** **[code]** was read in code or Defs on 19 Nov 2026 by research helpers, never run; **[code, checked]** was
also read by the agent writing this; **[S]** a secondary source; **[I]** our inference; **[A]** an authored figure or choice for the
owner to revise. Vehicle Framework was read in its public source (`SmashPhil/Vehicle-Framework`, MIT) and checked against the installed
build of 18 Nov 2026; Combat Extended in its public source and its vehicle compatibility assembly as installed. **None of it is tested
in game.**

## Read this first

- **What it is:** upgrade kits for our five service trucks (Talus, Ullage, Kelly, Datum, Busbar): an **armoured cab**, protection for
  the engine, tank and tyres, body plates, a **gun ring with a choice of three guns**, **smoke dischargers** and a ram. Each is a node
  in the truck's own upgrade tree.
- **Every kit costs something real** (owner): its weight loads the engine as cargo would; a gun needs a gunner, ammunition and
  research; an armed truck is worth shooting.
- **The trucks now have armour by part** (Framework 0.154.0, Vehicles 0.76.0; [armour for our armed vehicles](archive/vehicle-armour.md),
  row MV2): every part Skin, one outer part to a cell, a Combat Extended patch per truck and a wider target. Kits raise named parts
  from there.
- **No patch is needed for any of it.** Vehicle Framework's own upgrade nodes add armour to named parts, add a gun and its gunner's
  seat, and change how exposed a seat is.
- **No other mod on the owner's list militarises a cargo or service truck.** Only protected trucks and fighting vehicles have such
  upgrades, and most attach no disadvantage at all.
- **No hard dependency** (owner). The most useful things to borrow are already in Vehicle Framework and Combat Extended; three mods'
  guns are offered as optional extra mounts.
- **An armoured truck may keep working while enemies are about** (owner), behind a switch that starts off.
- **Four of the five trucks have almost no cargo room** (30 to 60 kg), so a kit's weight loads the engine on every truck and takes
  cargo room on the Talus only (owner). Kit weights are **real kilograms** (owner).
- **A remote station may fire with nobody aboard** (owner), EMP hardening is a kit of its own (owner), and **any faction's crew stays
  aboard a stranded vehicle while a weapon still works** (owner). Each needs code that does not exist yet.
- **Turbine engines are noted beside this** (owner's wish of the same day), with what they change here; not designed.

## The owner's answers (cards, 19 Nov 2026)

The first six shaped the record; the last eight answered its questions.

| Question | The owner's answer |
| --- | --- |
| Weapons | **A choice of mounts**: several exclusive weapons, as the HUMVEE has. This replaces the 13 Nov statement "our own vehicles carry no weapons" for the service trucks |
| What armour costs | **Weight counts like cargo**: a kit's kilograms take cargo room and load the engine as the same weight of cargo would |
| Scope | **All five trucks, the Talus first**; trailers only where it makes sense; the Crawler left out |
| Under fire | **Yes, with a switch**: a truck with cab armour may keep working while enemies are on the map; the switch starts off |
| Where the guns come from | **Ours, plus optional extras**: we draw our own machine gun, heavy machine gun and grenade launcher; an installed mod's gun is one more mount |
| Hard dependencies | **None**: Vehicle Framework stays the one requirement; Combat Extended stays first choice but optional (as on 18 Nov 2026); every other mod is an extra |
| Where a kit's weight counts | **The engine on every truck; cargo room on the Talus only.** The gun ring adds a small ammunition locker to the other four |
| Real kilograms or cut like cargo | **Real kilograms** |
| Other factions' trucks | **Later, behind a setting that starts off**, after the player's side is tested |
| EMP | **Hardening is its own kit**, with its own cost and weight, whether or not the truck carries a gun |
| A gun on a crewless truck | **Yes, a remote station**: a powered mount that fires with nobody aboard |
| Which guns on which truck | **By role**, then each truck confirmed on its own card: the table below |
| A stranded vehicle whose gun works | **Its crew stays and fights, raiders' too** |
| The order to build | **Protection first** |

## What each behaviour lets, stops and changes

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **Armoured cab** | Keep the crew alive under rifle fire, and so keep a truck working or get it home | Having it for nothing: it weighs about a tonne, and that loads the engine | A truck sent near danger is fitted for it; a truck on a long haul is left light |
| **Engine, tank and tyre protection** | Stop one lucky shot burning or stranding the truck | Protecting everything cheaply: each is its own kit and weight | The truck's worst weakness is chosen first: the tanker's shell, the tipper's engine |
| **Body plates** | Protect what the truck carries or is (the tanker's load) | Carrying as much, or going as fast uphill | A tanker of explosives is escorted or plated, not sent bare |
| **Gun ring and a choice of guns** | Give a convoy or a work site a gun of its own | Firing it alone: it needs a second person as gunner, ammunition from the cargo, and research; one gun at a time | One truck in a group becomes the gun truck, as real convoys run one to every ten |
| **The gunner's seat and shield** | Seat the gunner; the shield protects them where the game's own rules expose them | A free seat: without Combat Extended the gunner is the easiest person on the truck to hit | The shield is worth fitting without Combat Extended; with it, the cab's armour is what counts |
| **Remote weapon station** (owner) | Arm a truck that drives itself, or keep a gun firing with the crew out | Having it cheaply: it draws real power, stops without it, and makes even an empty truck a target | A crewless convoy can defend itself; a parked gun truck is never "empty" to an enemy |
| **EMP hardening kit** (owner) | Keep a truck running through an EMP burst | Getting it free with a gun: it is its own kit and weight | Trucks sent against mechanoids are hardened; others are not |
| **A stranded crew stays and fights** (owner) | Hold a position from a truck that can no longer move; face raiders who do the same | Emptying a raiders' vehicle just by stopping it | A stopped gun truck is a strongpoint until its gun is silenced or its crew is hit |
| **Smoke dischargers** | Break the enemy's aim for long enough to leave | Using them for ever: each firing uses up canisters | Smoke is saved for the moment a truck must get away |
| **Ram bar** | Push through light obstacles and animals with less harm to the truck | Nothing but its weight | A truck that must drive through a fight is fitted with one |
| **Weight as carried mass** | See honestly what protection costs | Armouring every truck with no thought | Kits are a decision, not a checklist |
| **Keep working when enemies are about** | Leave an armoured truck gathering or drilling through a raid elsewhere on the map | Doing so unarmoured, or by default: the switch starts off | Armour buys working time, not only survival |
| **Other factions' militarised trucks** (question 3) | Meet a raiders' gun truck, and capture one with its kits | Nothing; it starts off | Raids that bring vehicles are less predictable |

## What the research found

### Vehicle Framework's upgrade nodes can do all of it

- **The classes** [code]: `Upgrade` is public and abstract (`CustomFeatures/Upgrades/Node/Upgrade.cs:7-43`), so a subclass of ours may
  be used in XML. `VehicleUpgrade` carries `armor`, `health`, `roles` and `retextureDef` and re-applies on every load. `TurretUpgrade`
  carries `turrets` and `removeTurrets`. `StatUpgrade` carries `stats`, `vehicleStats` and `statCategories`. `CompUpgrade` activates or
  deactivates comps. There is no armour-only class.
- **Armour on a part** [code, checked] (`VehicleComponent.cs:229-270`): a part's rating starts from the vehicle-wide stat; a part with
  its own `armor` replaces that and **ignores vehicle-wide armour upgrades** ("Part-specific armor does not apply vehicle-wide armor
  upgrades"); `AddArmorModifiers` are added last. So **every kit is a per-part `VehicleUpgrade` armour entry with `Add`**, never a
  vehicle-wide stat.
- **A gunner's seat** [code] (`VehicleUpgrade.cs:179-226, 284-317`): a `RoleUpgrade` with a new `key` adds a role; one naming an
  existing role edits it; fields are `handlingTypes`, `slots`, `slotsToOperate`, `turretIds`, `hitbox`, `exposed`, `chanceToHit`. Saved
  seats rebuild from the tree on load. VVE's Roadkill and the HUMVEE both add a gunner this way.
- **A gun** [code] (`TurretUpgrade.cs:28-80`): the node adds a full `VehicleTurret`. **The vehicle's Def must carry
  `CompProperties_VehicleTurrets`**, even with an empty list, because there is no null check; the MaxxPro ships exactly that.
- **Exclusive choices** are `disableIfUpgradeNodesEnabled`, as the HUMVEE's and MaxxPro's mounts use.

**The traps** [code]:

| Trap | What happens | What we do |
| --- | --- | --- |
| `type=Set` on a stat or vehicle stat | Does nothing: the override is only read for stat categories (`VehicleStatHandler.cs:190-211`) | `Add` only |
| `type=Set` on armour | Applies itself when the Upgrades tab merely shows the node (`VehicleUpgrade.cs:40-42`) | `Add` only |
| A `health` upgrade | Fully repairs the part on unlock and on every load (`VehicleUpgrade.cs:102`) | No `health` upgrades; protection is armour. Our fittings write `AddHealthModifiers` directly where health must change (`VehicleFittings.cs:974-975`) |
| A wrong part key | Throws; the unlock is caught and logged (`CompUpgradeTree.cs:253-261`) | The planned gate check reads every key |
| A turret added outside the tree | Deleted on load (`CompVehicleTurrets.cs:792-864`) | Mounts are tree nodes; a hidden node unlocked by code where code must add one |
| A Turret role with no `turretIds` | May throw (`VehicleTurret.cs:527`; **unconfirmed**) | Every gunner role names its gun |
| Parts | No upgrade can add a part, change a part's hit box, add a damage reactor or change `damageDefMultipliers` | Kits act on parts the Def already has |

### How armour really works

- **Without Combat Extended** [code]: the hit cell is the one nearest the shooter; an outer part there takes it; the armour roll is
  rating minus penetration, on the game's own scale; a shot falls through to inner parts only when the outer part is at or below half
  health, and then only the overflow (`VehicleComponent.cs:134-223`).
- **With Combat Extended** [code]: its prefix replaces the whole step (`VehicleArmorPatch.cs:17-34`). The same stats are read in
  millimetres and megapascals. Armour is the part's rating times its condition, a second layer is the part's health divided by 50,
  and **every outer part on the hit cell must be beaten**, so our Defs keep one outer part to a cell ([the armour design](archive/vehicle-armour.md)).
  Its path skips `damageDefMultipliers`, Vehicle Framework's EMP handling and fire extinguishing.
- **A vehicle is hit through a box one cell wide under Combat Extended** unless its Def widens it (`PhobosCollisionExtension`, designed
  in the armour record, not built).
- **Collision recoil** uses the vehicle's base blunt armour with a `(2 - armour)` term (`VehiclePawn_Combat.cs:100`), so our Defs must
  not carry a Combat Extended-scale figure vehicle-wide **[I]**.
- **Our trucks today** [code]: vehicle-wide `statBases` only (the Talus, Ullage and Kelly 0.40 sharp and 0.26 blunt; the Datum and
  Busbar 0.30 and 0.20), no per-part armour and **no Combat Extended patch**, although [the utility vehicles design](utility-vehicles-design.md)
  says their armour "follows the Harrier part's scale". That is a discrepancy to repair with the base work.
- **The Health tab already shows armour in Combat Extended's units** (Framework 0.140.1), so a fitted kit is visible with no new screen.

### How people aboard get hit [code, checked]

- **Without Combat Extended** (`VehicleStatHandler.cs:814-865`): on an outer hit, only an **exposed** seat at that cell can be hit, at
  its chance times 1.25, and that shot does the vehicle no damage. Enclosed seats are hit only when a shot reaches an inner part.
  Defaults are enclosed and a chance of 0.3; all our seats are enclosed today.
- **With Combat Extended** [code]: people are hit only after the shot has reached an inner layer, with the damage the armour left
  (`VehicleArmorPatch.cs:197-221`). **An exposed seat makes no difference there.**
- **So:** a gun shield is a real figure only without Combat Extended; with it, the cab kit's class is what protects everyone.
- A pawn in a mental state is put out; a destroyed vehicle puts everyone out; a raiders' vehicle that cannot move is emptied
  ([knocked-out systems](archive/vehicle-knocked-out-systems.md), M4). Its open choice KO4, whether a crew stays aboard while a weapon still
  works, now touches our own trucks.

### Guns [code]

- **Who fires**: a turret is manned unless a Turret role that names it lacks capable crew; a turret no role names fires with nobody
  (`VehicleTurret.cs:517-536`). **An enabled turret makes even an empty truck a target**; any truck with people aboard already is one
  (`VehiclePawn_AI.cs:92-108`).
- **Ammunition**: without Combat Extended a gun with no ammunition filter fires for ever, so every mount of ours carries one and draws
  rounds from the cargo. With Combat Extended's ammunition on, the filter is rewritten from the gun's ammo set
  (`VehiclesCompat.cs:94-128`). Ammunition weighs against cargo, and our engine model already counts loaded rounds
  (`Powertrain.cs:361-382`) **[code, checked]**.
- **What a new gun gets from our code with nothing added** [code, checked]: `WeaponMounts.cs` reads weapons added by upgrade nodes too
  and gives a ground vehicle's gun a turret ring (60 health; the gun stops at a tenth of it). The knock-out rules then apply: a damaged
  weapon slows to 0.55 of its pace, a hurt gunner aims and loads slower, a powered turret without power is cranked by hand at a fifth
  of its traverse, and guns named as hand-swung draw no power (`KnockOut.xml`). A powered turret draws 40 A and sheds last; the trucks
  have 24 V, a 100 Ah battery and a 100 A alternator (`Loadouts_Default.xml:540-600`).
- **EMP** [code, checked]: a vehicle counts as armed, and so military, only when its **design's own** turret list has turrets
  (`VehicleEmp.cs:30`). A gun added by upgrade does not change its class today. Question 4.
- **Unconfirmed**: whether a player's gun fires on a vehicle whose engine is off. The grab and the tip run with the engine on, so a
  working truck should be able to fire; it is the first thing to test.

### Weight [code, checked]

- A fitting's `kilograms` come off `CargoCapacity` and nothing else (`Patches.cs:154-159`).
- Our engine model's mass is kerb mass, cargo, the tank's load, people, loaded ammunition and fuel (`Powertrain.cs:361-382`). Kerb mass
  comes from the Def (`Powertrain.cs:183-193`). **A Vehicle Framework `Mass` upgrade reaches neither**; in Vehicle Framework itself mass
  matters only for collisions.
- **Vehicle Framework's own speed penalty is cargo weight over cargo capacity** (`VehicleStatPart_WeightUsage.cs:15-37`). So taking
  cargo room slows a laden truck even where our engine model does not run: on other factions' vehicles, which run our lighter model.
- [The armour design](archive/vehicle-armour.md) says a kit "adds its weight to the mass stat". That would slow nothing; it is corrected there.

### What our trucks already risk [code]

| Truck | Size | Cargo, kg | Empty kg / kW | Seats | Its worst weakness |
| --- | --- | --- | --- | --- | --- |
| Talus tipper | 3 x 8 | 2,400 (to 4,200) | 10,000 / 220 | driver | An engine that explodes; a fuel tank that leaks |
| Ullage tanker | 3 x 10 | 60 | 9,000 / 224 | driver, passenger | Its shell: with Combat Extended it can carry FSX, which detonates on one hit in four |
| Kelly drill rig | 3 x 10 | 40 | 20,000 / 300 | driver, passenger | An engine that explodes; a tall mast |
| Datum survey truck | 3 x 7 | 30 | 4,500 / 130 | driver, passenger | Thin skin (0.30) and delicate gear |
| Busbar generator set | 3 x 8 | 40 | 8,000 / 130 | driver | An engine that explodes; the camp's power goes with it |

- The one part-protection upgrade we ship is the tanker's heavy-wall vessel (+60 health, only with the Tanker Vessels tie-in). It is a
  `health` upgrade, so it meets the full-repair trap above.
- Trailers have tyres only: no cab, no seats, no electrics and no fitting mounts.
- Passive gear costs its materials and its weight, with no wear or upkeep (owner, [vehicle accessories](vehicle-accessories.md)).

### Prior art on the owner's list [code]

**No loaded mod gives a cargo or service truck an armour or weapon node.** Figures as written; "CE" is Combat Extended's scale.

| Mod and vehicle | Armour upgrade | Weapons by upgrade | Disadvantage attached |
| --- | --- | --- | --- |
| HUMVEE (`HMV_Upgrade`) | Roof (seats no longer exposed); kit on three plates +1.3 sharp, CE +7 mm to 10 mm; internals; tyres | LMG, HMG, grenade launcher or flame launcher, exclusive; each adds a gunner | Cargo only: -25 to -150 |
| GTek L-ATV | Plasteel kit, plates +0.8 to +1.2, CE to 40 to 60 mm | Three remote stations, exclusive, no gunner needed | **None: the armour makes it 160 lighter and faster** |
| GTek MaxxPro | None (built protected) | M2HB or Mk 19 on a cupola ring, exclusive | None |
| RimThunder M1A2SEP, M2A3 | Reactive armour on skirts +0.5, CE +50 mm | Swapped turret set with smoke launchers | Speed -1 or -2; slower repair |
| VVE - Upgrades (seven Tier 1 vehicles) | `Armor` +0.04; `ReinforcedArmor` +0.10 to 0.18, CE +0.85 to 2.5 mm; engine cover; bumper | Mini-turret on the Prowler; gunner seat on the Roadkill | Reinforced: mass +40 to +110, slower acceleration |
| RimThunder HEMTT (a cargo truck) | None | A fixed 7.62 mm self-defence gun, not an upgrade | — |
| Frontline Workshop Land Cruiser | Reinforced armour, mass +300, cargo -50 | Thirteen exclusive mounts, some indirect fire | Mass +20 to +1,500 per mount. **Does not load in the owner's game today** |

Mount costs elsewhere run from 75 steel and 3 components at 7,200 work (the HUMVEE's LMG) to 195 steel and 10 components at 57,000 work
(the L-ATV's remote station). Our own trees already charge more than any of them: work x5 and bulk materials x2.5, scaled by the
vehicle's weight (`PhobosUpgradeCostsLikeTunes`, [tuning and upgrades](vehicle-tuning-and-upgrades.md)). Kit prices go through it.

### Smaller findings the build must know [code]

- **Role edits**: for most fields the last edit of a role wins; `SlotsToOperate` and `Comfort` take the first. Removing a role puts
  everyone in it out first (`VehicleRole.cs:54-224`; `VehiclePawn_Handlers.cs:223-236`).
- **Taking a gun off**: a `TurretUpgrade`'s refund restores a turret it removed only if that turret is in the Def's own list
  (`TurretUpgrade.cs:66-80`). Our mounts only add, so a refund simply removes the gun.
- **Hidden nodes** can be unlocked only from code (`UpgradeNode.cs:22-23`): the route for other factions' kits, if the owner allows them.
- **Fittings cannot add parts** and each sits on one part, lost with it (`FittingDefs.cs:31-105`). Kits are tree nodes for that reason,
  and because the armour design already made them so.
- **Fuel follows engine load by default** (`loadDependentFuel`, on since Framework 0.131.0) and accessories load the engine
  (`accessoriesLoadEngine`, on), so a kit's weight costs fuel with no new setting.
- **Research**: other authors gate almost none of their armour or gun nodes. Combat Extended adds its `CE_TurretHeavyWeapons` project to
  the MaxxPro itself. Our nodes take research from our own tree; a heavy gun may also ask for Combat Extended's project where it is active.
- **Combat power**: no author sets it; Vehicle Framework gives every vehicle 100 (`Vehicles.decompiled.cs:13766-13768`). There is no
  prior art for what an armed truck is worth in a raid.

### The real machines

- **Gun trucks were service trucks.** In Vietnam the US Army's transport battalions armed their own five-ton cargo trucks: quarter-inch
  steel plates on the cab doors and windscreen, a gun box on the bed, an M60 forward and an M2 .50 aft, more "on ring and pintle
  mountings", a crew of five to seven, and "typically, one gun truck was assigned for every 10 transport trucks" (Carl O. Schuster,
  ["M54 Gun Truck: The Savior of the Support Convoy"](https://historynet.com/m54-gun-truck-the-savior-of-the-support-convoy/),
  *Vietnam* magazine, Autumn 2022; read 19 Nov 2026).
- **The weight cost them.** "The added weight of armour, weapons and ammunition increased fuel consumption, as well as creating
  maintenance problems and reducing the durability of the truck frames", and the lighter two-and-a-half-ton trucks "were underpowered,
  and the addition of armor and weapons slowed them down" **[S]** ([Wikipedia, "Gun truck"](https://en.wikipedia.org/wiki/Gun_truck),
  citing Paul S. Gardiner, "Gun Trucks: Genuine Examples of American Ingenuity", *Army Logistician*, July-August 2003; the article
  itself was not read).
- **So the owner's 18 Nov rule, "armed as the real machine; a machine that is unarmed in reality stays unarmed", is kept**: it was
  decided for the real machines of [modern military vehicles](modern-military-vehicles.md), and real service trucks were armed in
  exactly this way, by their own crews, with kits.
- **Modern kit weights were not verified.** Two pages giving figures would not load on 19 Nov 2026: a copy of a US Army technical
  bulletin for the HEMTT's crew protection kit (a search summary gave "+2,490.79 lb", about 1,130 kg) and a Defense Update article on
  an MTVR kit (cab 2,200 lb, about 1,000 kg; cargo bed 4,500 lb). **Both are unverified** and no figure of ours rests on them.

## The design

### How a kit is built

- **A node in the truck's own upgrade tree**, as [the armour design](archive/vehicle-armour.md) decided, written with `Add` only.
- **Links** follow the owner's branching rules (`src/PhobosFramework.Vehicles/AGENTS.md`): the three guns are either/or; each needs
  the gun ring; the shield needs the ring; a heavier cab class needs the cab kit. Nothing else is gated: research paces the rest.
- **Each truck's tree keeps one free row for an engine choice** (below, "Beside this: turbine engines").
- **The prerequisite**: the armour base work, MV2. Each truck's parts get a class of their own in the Def and a Combat Extended patch
  file in D'thorga Vehicles' `Mods/CombatExtended` folder; the planned gate check `scripts/check-vehicle-armour.py` keeps the two scales
  together and reads kit figures too.

### The kits

Classes are those of [the armour design](archive/vehicle-armour.md): Splinter 5 mm, Rifle 8 mm, Armour-piercing rifle 15 mm.

| Kit | What it raises | Class reached [A] | Weight, kg [A, sum below] | Trucks |
| --- | --- | --- | --- | --- |
| **Armoured cab** | The cab part | Rifle | 1,000 | All five |
| **Heavy cab plates** (needs the cab kit) | The cab part, one class more | Armour-piercing rifle | +850 | All five |
| **Engine and tank guards** | The engine and fuel tank parts (inner) | Rifle | 300 | All five |
| **Run-flat tyres** | The wheel parts | Splinter | 120 | All five |
| **Body plates** | The outer body part: the tanker's shell, the tipper's body, the rig's deck | Rifle | 900 to 1,400 by truck | Ullage first; Talus; Kelly |
| **Ram bar** | Vehicle Framework's four collision categories | — | 150 | All five |
| **EMP hardening** (owner) | The truck's EMP class, to the hardened one armed vehicles have | — | 60 | All five |

- **The weight sum** **[A]**: steel is about 7,850 kg a cubic metre, so a square metre of plate weighs 7.85 kg for each millimetre:
  62.8 kg at the Rifle class's 8 mm, 117.8 kg at 15 mm. A truck cab's doors, sides, back and roof come to about 14 square metres
  **[A]**, so about 880 kg of plate at 8 mm; with heavier glass, 1,000 kg. The unverified real kits above are of the same order.
- **A guard cannot remove a hazard.** Reactors are fixed to parts, so the fuel tank still leaks and the engine still explodes when
  they are finally holed. A guard makes that take a heavier round. The record does not call it self-sealing.
- **Tyres**: armour on the wheel parts only. Our own tread and wear model is untouched.
- **Body plates wait on art.** The Talus has three bodies, so plates on it are three overlay sets; the tanker's shell is one.
- **EMP hardening needs a code change** **[I]**: today a vehicle's EMP class is rated once per Def at start-up, from the loadout's choice
  or from whether the design has guns (`VehicleEmp.cs:27-33`, `EmpRule.ClassFor`). A kit must be read per vehicle, at the time of the
  burst. Its 60 kg (shielded looms, filters, a spare control unit) is authored. A gun by upgrade still does not harden a truck: the
  owner chose the kit instead.
- **Built (Vehicles 0.80.0, Framework 0.158.0; not yet tested in game):** engine and tank guards, run-flat tyres and EMP hardening on all
  five trucks, after the first-tier research **truck armour** (after flak armour; agent's choice of parent, 1,800 points). Each tree gives
  them their own row at the foot. EMP hardening is a vehicle stat (`Phobos_EmpHardening`): while it is above zero, `EmpHardeningPatch`
  lowers a part's rating to the rules' military one for that one call of Vehicle Framework's `ApplyEMPDamage`, and puts it back in a
  finalizer (`EmpRule.HardenedSeverity`, tested). Kit weights go in `Phobos_KitKilograms`, which the engine model already weighs. The
  second research tier and the under-fire switch come with the cab, in step 3: neither has anything to unlock or switch before it.
- **Built (Vehicles 0.86.0, Framework 0.172.0; not yet tested in game):** the armoured cab (Rifle, 1,000 kg) and heavy cab plates
  (Armour-piercing rifle, +850 kg, needs the cab, second research tier **heavy truck armour**) on all five trucks; the ram bar (150 kg;
  Vehicle Framework's collision categories: harm taken from people, animals and buildings halved, harm dealt 0.50 to 0.75 and 2.5 to
  3.0, agent's figures); body plates on the Talus (tipping body, 1,100 kg), Ullage (tank shell, 1,400 kg) and Kelly (chassis, 900 kg).
  Pictures: the cab kits by paid editing of each picked body's cab (fal, US$1.50, `assets/phobos-vehicles/requests/truck-cab-kits-paid-record.md`),
  lifted out and fitted by `assets/phobos-vehicles/export_armour_kits.py`; the ram bar and skirts drawn alone on the local model and
  fitted by the same script (agent placement). The under-fire switch is a choice in Gather what, shown once the armoured cab is
  fitted (`CompProperties_PhobosGatherRun.keepWorkingNode`, `GatherRule.StopsForEnemies`, tested).
- **Under Combat Extended an EMP did nothing to any vehicle** (found by the fault-read of 19 Nov 2026): its armour setting replaces
  Vehicle Framework's damage method with one that has no EMP step, so the kit cost 60 kg for nothing there. Framework 0.162.0 runs
  Vehicle Framework's own EMP step when Combat Extended skipped it (`EmpUnderCombatExtendedPatch`; temporary-patches.md row 18).
  `scripts/check-vehicle-armour.py` now also sums kits that can be fitted together on one part, and refuses a Combat Extended kit
  patch naming a part its node does not.

### The gun ring and its guns (owner: a choice of mounts, by role; ours, plus optional extras)

| Node | What it adds | Needs | Costs beyond materials [A] |
| --- | --- | --- | --- |
| **Gun ring** | A ring on the cab roof and a **gunner's seat** | A truck with a cab | 120 kg; a second person aboard to fire anything on it |
| **Machine gun** (7.62 mm) | Our own gun, swung by hand | The ring | 40 kg; ammunition from cargo |
| **Heavy machine gun** (.50) | Our own gun, swung by hand | The ring; either/or with the others | 90 kg; heavier ammunition |
| **Grenade launcher** (40 mm) | Our own launcher, swung by hand; with Combat Extended its ammo set includes a smoke round | The ring; either/or | 110 kg; the heaviest ammunition |
| **Gun shield** | The gunner's seat stops being exposed | The ring | 180 kg |
| **Remote weapon station** (owner) | Our own powered mount with a heavy machine gun, fired with nobody aboard | The ring; either/or with the guns above | 250 kg; the 40 A turret drive while it works |
| **Ammunition locker** | Cargo room for rounds, on the four trucks with almost none | The ring; not on the Talus | Its own weight, 40 kg, for 150 kg of room **[A]** |
| **Extra mounts** | An installed mod's gun, as one more either/or choice | The ring, and that mod | As that node says |

- **The seat**: a `RoleUpgrade` adds `gunner` with Turret handling, naming the gun's key, on the cab's cells, exposed until the shield
  is fitted. The driver's role is untouched, so a truck still drives with one person and fires with two.
- **Swung by hand**: the three guns draw no turret-drive load, as the knock-out rules already treat machine guns and grenade launchers.
- **The remote station** (owner: "yes, a remote station"): no Turret role names its gun, so Vehicle Framework fires it with nobody
  aboard; it draws the 40 A turret drive, and our knock-out rules already stop such a station without power and crank nothing by hand.
  **It makes an empty truck a target**, which the owner accepted with it. With the crewless fit (MV4) it gives a truck that drives and
  fires alone; that record's "Armed: no" is noted as changed for our service trucks. The L-ATV's own station stays an optional extra
  once that mod declares 1.7.
- **Our gun Defs carry Combat Extended's data themselves**: `Vehicles.CETurretDataDefModExtension` is a Vehicle Framework class, so no
  Combat Extended folder is needed for the guns, only the usual patch for projectiles where the armour design asks for one.
- **Rearming**: Vehicle Framework has haulers bring ammunition to a turret up to a quota (`autoRefuelProportion`). Our top-up Defs
  (`PhobosTopUpDef`) are the other route; which is better is for the build to read.
- **The Kelly's mast and the Talus's grab** share the roof line with the ring; the art plan must place them before a picture is made.

**Guns by role** (owner, 19 Nov 2026: by role, then confirmed truck by truck on cards, as the agent proposed; the reasons are the agent's):

| Truck | Machine gun | Heavy machine gun | Grenade launcher | Remote station | Why [A] |
| --- | --- | --- | --- | --- | --- |
| Talus tipper | Yes | Yes | Yes | Yes | The gun truck of the five: the only one with room for ammunition, and the one that runs by itself |
| Kelly drill rig | Yes | Yes | Yes | No | Twenty tonnes and parked for days at a site: a strongpoint that needs a crew |
| Ullage tanker | Yes | No | No | No | Self-defence only: nobody wants a tanker of fuel in a firefight |
| Busbar generator set | Yes | No | No | Yes | Self-defence, and a station it can power itself while it runs the camp |
| Datum survey truck | No | No | No | No | 4.5 tonnes of delicate gear: cab armour and smoke only |

### Smoke dischargers

- **The lead route needs no C#** [code]: a Vehicle Framework `Static` turret (one click, fires along its own facing, no targeting)
  firing the game's own `Bullet_SmokeLauncher`, as RimThunder's `RT_GrenadeLauncher` does. Our code already treats such fixed launchers
  as weapons with no drive that do not count for crews bailing out.
- **It uses up something real**: canisters, or chemfuel as other authors charge (5 to 15 a shot) **[A]**.
- **The fallback** is our own button calling the game's `GenExplosion.DoExplosion` with blinding smoke at the truck's edge cells:
  explosions skip a vehicle's inner cells (`Patch_Combat.cs:207-216`). The game's own smoke-pop verb needs a worn belt and cannot be used.
- **What smoke does**: a shot along a smoked line hits at 0.7, for the game, Combat Extended and Vehicle Framework's turrets alike.
- **"Pops smoke when hit"** is possible later without a patch: `DamageTaken` fires in both damage paths and a comp may listen.
- **A caution**: Vehicle Framework's own "sight blocked by gas" test for turrets looks inverted against the game's (`TargetingHelper.cs:59-62`).
  It applies only to turrets that ask for it; untested.

### Weight as carried mass (owner)

- **One new, general capability, described and not built**: a weight in kilograms for any upgrade node, listed in a tree mod extension
  as `PhobosUpgradeCostsLikeTunes` and `PhobosUpgradeAnyOf` are. Our engine model adds the fitted nodes' weights to the mass it moves
  (`Powertrain.MassKg`), and the cargo stat part takes them off cargo room where fittings' kilograms already come off.
- **It is not armour-only, and a weight may be negative**: a lighter engine, a stripped body.
- **An `Upgrade` subclass of ours** in XML would carry the weight on the node itself. The extension is the house pattern and is the lean.
- **Still no drag**: only the mass changes. The no-drag rule stands.
- **Where it counts** (owner): a kit's weight joins the engine's mass on every truck, and comes off cargo room **on the Talus only**. The
  extension says per tree whether weights take cargo room, so the other four keep their 30 to 60 kg of stowage.
- **Real kilograms** (owner): no cut to match the game's cargo figures. On a laden Talus (10,000 kg empty, 2,400 kg of cargo) the cab
  kit is 8 % more mass to move, a little slower on grades and a little more fuel, and 42 % of a base load, less once the bulk body
  (3,600 kg) and greedy boards (+600 kg) are fitted.

### Keep working when enemies are about (owner)

- A per-truck switch, **off to start**, shown once the armoured cab is fitted: "Keep working when enemies are about".
- Today enemies end a gathering run through a Def-wide `stopForEnemies` and a map-wide test (`CompPhobosGatherRun.cs`). The switch
  moves that into a rule in `GatherRule`: stop unless the cab kit is fitted and the switch is on.
- The same switch can serve other unattended work later (a drill rig left running), read from one place.

### Trailers and the Crawler

- **Bowsers** (fuel, water): body plates on the tank only. No cab, no gun, no smoke: they have no seats or electrics.
- **The flatbed, Billy and the shower trailer**: nothing.
- **The Crawler**: left out (owner). It is a boat, and the submarine rules govern it.

## What to take from other mods (the owner's question)

**Nothing on the owner's list earns a hard dependency** (owner agreed). The most useful things are already in Vehicle Framework, which
we require, and Combat Extended, which we already reach by name.

### Taken at no new dependency

| What | Where it lives | What it gives the kits |
| --- | --- | --- |
| A `Static` turret firing the game's smoke shell | Vehicle Framework and the base game | The smoke discharger with no C# |
| `Vehicles.CETurretDataDefModExtension` | Vehicle Framework | Our own guns carry Combat Extended data without a Combat Extended folder |
| `AmmoSet_40x53mmGrenade` and the other sets | Combat Extended | Our grenade launcher fires its smoke round as well as high explosive |
| `PawnCollisionMultiplier` and three more collision categories | Vehicle Framework (VVE's reinforced bumper uses them) | A ram bar that moves a real figure (accessory row Y20) |
| A `RoleUpgrade` that removes the driver's role | Vehicle Framework; the L-ATV's "autonomous mobility" is only this | The crewless fit (MV4) needs no code for driving |
| `autoRefuelProportion` | Vehicle Framework | Haulers rearm a gun truck |

### Offered as optional extra mounts

Each is a tie-in node patched into our tree from that mod's folder, as the tanker's pressure vessels and the drill rig's well kit are;
either/or with our own guns; hanging from our gun ring (a tie-in node may hang from ours, never ours from it, which
`scripts/check-upgrade-trees.py` enforces). The mod goes in our `loadAfter` (`tests/test_tie_in_load_order.py`). Activity was read on
19 Nov 2026 with `scripts/workshop-research.py`.

| Mod | What would be offered | Its standing | Verdict |
| --- | --- | --- | --- |
| GTek MaxxPro (`killerbot.navistarmaxxpro`) | `HID_Navistar_M2HB` and `HID_Navistar_Mk19GL` on its cupola ring: the closest match to this design | Declares 1.7; needs only Vehicle Framework; Combat Extended built in; updated 15 Nov 2026 but the author's last reply was Sep 2025; no stated permission | **First** |
| HUMVEE (`hmv.lunartika`) | Its ring and shield, sized for a 3-wide hull, with `HMV_GUN_A` and `HMV_TURRET_A` | Declares 1.7; updated Oct 2026, author replying; no Combat Extended support of its own (the owner's local patch gives it); two of its turrets carry duplicate graphics elements and players report start-up errors | **Second, after reading how its graphics load** |
| GTek L-ATV (`killerbot.jltv`) | `HID_JLTV_M2HB`, a remote station that fires with no gunner | Does not declare 1.7 | **Waits**; it also brings the autonomous-gun question |
| RimThunder's HEMTT | `RT_M249_MGTurret_HEMTT`, a shielded gun on a cab the tanker's size | Core is built for 1.6, updated Jul 2026, author not replying; earlier records set its tie-ins aside for this | **Not offered** unless the owner asks |
| Frontline Workshop (`serinairo.frontlineworkshop`) | The best standalone guns: M1919, PKM, MG3, M2HB, DShK, Mk 19 | **Loads nothing in the owner's game today** (its 1.7 Combat Extended folder is commented out); author active, says support is coming | **Revisit when it loads** |
| Vanilla Vehicles Expanded family | Its smoke-pop comp | Very active; CC BY-NC-ND 4.0; its guns are housings for its own vehicles; the smoke-pop code is not decompiled | **Not needed**: the static launcher does the job |
| The Dead Man's Switch family | Nothing: it adds no vehicle turrets | — | — |

### Doing it properly

- A tie-in mount shows another author's picture on our truck, by Def reference only, for a player who has that mod. **Nothing of
  theirs is copied.** `THIRD_PARTY_NOTICES.md` gets an entry per mod used when one is built. **None of these mods states a permission
  either way**, and nothing here implies their authors endorse it.
- **The parts-bill mechanism fills costs only** (`BuildBillRule.Fill` writes cost lists and node ingredients); it cannot choose which
  gun a node installs. So extras are separate nodes, not substitutes. A mount's ingredients may still go through a bill.
- **Lessons already paid for**: never put a function of ours into another mod's shared hook (the Mongoose crash); a tie-in assembly
  needs its mod in `loadAfter`; a by-name target is written in full so `check-targets.py` can read it.
- **Not taken**: RimThunder's unused projectile interceptor and active protection (quiet core, no Def uses them); the helicopters'
  ground rearm (aircraft code that checks flight state); VVE's Paladin "smoke launcher" (Combat Extended turns it into a
  high-explosive launcher).
- **No earlier record proposes any of this.** The nearest are AD12 in [air-defence tie-ins](air-defence-tie-ins.md) (a flak or missile
  vehicle of ours registering with The Dead Man's Switch, which "needs a vehicle to carry it") and the "cheaper armour upgrade" patch
  idea the owner closed on 16 Nov 2026.

## Other factions' vehicles

- **In a raid on us**: Vehicle Framework puts no vehicles into raids in the installed build (the patch method is empty), and its rules
  would want category Combat where ours are Transport [code]. Where another mod brings one of our trucks, our enemy fit sets (a chance
  of 0.25, by tech level) are the one way it gets kit, and those are fittings. **No code of ours unlocks tree nodes for other
  factions' vehicles today.** It could, through the public `CompUpgradeTree.FinishUnlock`, as VTOL Raids does for hostile aircraft.
  **The owner chose: later, behind a setting that starts off**, as the line-charge trailer's raider use is, after the player's side is tested.
- **A stranded crew stays and fights** (owner, deciding the open choice KO4): any faction's crew stays aboard a vehicle that cannot move
  while a weapon of its still works. Today Vehicle Framework's raid orders put everyone out of a vehicle that cannot move, guns working
  or not ([the reading](archive/vehicle-knocked-out-systems.md#m4-read-what-its-orders-do-with-a-vehicle-that-cannot-move-17-nov-2026)), and our
  own bail-out rule (C2) must be read again against it. Changing the first needs a think-tree step or a patch; not designed here.
- **A raiders' gun truck would work as built**: other factions' vehicles spawn with their engines on and guns set to fire by
  themselves; armour sits in the tree and the Def, so it needs none of our player-only systems; taking cargo room slows it through
  Vehicle Framework's own rule even on our lighter model.
- **Parked at a base we attack**: a kitted truck there keeps its armour; its gun fires only if crewed.
- **Captured**: it keeps its kits and its gun. The under-fire switch and gathering are the player's only.
- **Cost with several on a map**: none while idle. Armour and guns are Vehicle Framework's own; the weight sum is read when mass is.

## Art

- **Every visible node is its own overlay set, and there is no stand-in vehicle art** (owner). So the build waits on pictures.
- Needed for the Talus alone: the armoured cab (and heavier plates, if they show), the ring, three guns as turning turret pictures,
  the shield, the discharger tubes, the ram bar; body plates on each of its three bodies if the Talus gets them.
- Guns and the ring are drawn once and shared by all five trucks; cab kits are per truck.
- An extra mount from another mod brings its own picture and needs none of ours.

## Cost to run and saves

- **Nothing new ticks.** Armour, seats and guns are Vehicle Framework's; the smoke discharger is a turret; the weight is summed when
  the engine model already recomputes mass (at most every 250 ticks).
- **Saved**: fitted nodes and added guns are saved by Vehicle Framework with the vehicle; the under-fire switch is one saved flag on
  the truck; nothing else.
- **Safe to add mid-game. Removing the mod** with kits fitted is as removing any vehicle mod: the trucks go with it.

## Argued against

- **A `health` upgrade for protection**: it fully repairs the part at every load.
- **Camouflage**: a vehicle's visibility is fixed on its Def ([vehicle accessories](vehicle-accessories.md)).
- **An autonomous gun** was argued against here (it makes an empty parked truck a target). **The owner chose it** as the remote
  station, knowing that; it stays its own node, so a truck without one is still "empty" when nobody is aboard.
- **A turret added by our own comp outside the tree**: deleted on load.
- **Substituting another mod's gun for ours by parts bill**: the mechanism fills costs only, and the kit would look different in every game.
- **Advantages only**: the L-ATV's weightless armour is why every L-ATV ends up armoured. A kit with no cost is not a choice.

## Still unknown

- **Unconfirmed in code**: whether a player's gun fires with the truck's engine off; the Turret-role error; the inverted gas test in
  Vehicle Framework's targeting; how `AccelerationRate` is used beyond the file that reads it; whether any Combat Extended stat part
  moves a vehicle's base armour.
- **Not read**: VVE's smoke-pop code; Frontline Workshop's Combat Extended folder in play; the trees of the removed VVE - Upgrades
  Extended; how the HUMVEE's duplicate graphics elements load.
- **Not sourced**: any modern add-on armour kit's weight (above).
- **Nothing here was run in game.**

## Beside this: turbine engines (owner, 19 Nov 2026)

The owner, on reading this record's plan: "I wish to offer turbine engine upgrades of the sort to our vehicles, where it would be sort
of realistic to do so, down the line. It'd be great for our generator vehicle." **Not designed here.** Row MV9 in
[outstanding work](outstanding-work.md).

- **What already exists** [code, checked]: an engine kind and a turbine block on drivetrain Defs (`PowertrainDefs.cs`); a tank's
  turbine drivetrain, which burns nearly as much standing as moving (`PhobosDrivetrain_TrackedTurbine`, idle fuel share 0.6,
  `Drivetrains.xml:105-127`); the turbine system and its start stage; a gas turbine assembly part "for an aircraft or a heavy vehicle";
  the turbines research project; records [the turbine design](archive/vehicle-turbine-design.md) and [altitude and turbines](vehicle-altitude-and-turbines.md).
- **What it would need** **[I]**: a drivetrain chosen by an upgrade node. Today a truck's drivetrain is named by its loadout, per Def.

**What it changes here:**

1. **The per-node weight is general and may be negative**, as designed above: a turbine is much lighter than a diesel of its power.
2. **A turbine is the honest counterpart to armour's cost**: armour loads the engine; a turbine buys the power back and pays in fuel,
   most of all standing still. A gathering run idles at every pile, so the pairing is a real choice.
3. **Kits name the engine part, never the engine kind**, so the engine guard survives a swap. Fittings that match on engine kind (the
   turbocharger, the boost tunes) would need exclusions against a turbine node.
4. **Heat**: AD5 in [air-defence tie-ins](air-defence-tie-ins.md), hot engines drawing heat-seeking missiles, is an open question to
   the owner. A militarised turbine truck is its hottest case.
5. **Each truck's tree leaves a free row for an engine choice**, the Busbar's first: its exhaust heat exchanger is the node a
   turbine's hotter exhaust would feed.

## Questions for the owner

**All eight were answered by the owner on 19 Nov 2026 (cards), and the guns-by-role table was then confirmed truck by truck.**
Nothing is open; the figures marked [A] remain the owner's to revise.

| # | Question | Answer |
| --- | --- | --- |
| 1 | Four of five trucks have 30 to 60 kg of cargo room. Should a kit's weight take cargo room on every truck (zeroing it there), or load the engine everywhere and take cargo room only on the Talus? | **Decided: the engine everywhere, cargo room on the Talus only**, with an ammunition locker on the others |
| 2 | The cab kit's tonne is a real figure against a cargo figure the game cut to a sixth of the real truck's. Real kilograms, or cut as cargo was? | **Decided: real kilograms** |
| 3 | May other factions' trucks carry these kits and guns, behind a setting that starts off? | **Decided: later, behind a setting that starts off** |
| 4 | A gun added by upgrade does not make a truck "military" for EMP today. Should it? | **Decided: no; hardening is its own kit** (against the agent's lean) |
| 5 | The crewless fit (MV4) is unarmed. May a crewless truck carry a gun? | **Decided: yes, a remote station** (against the agent's lean) |
| 6 | Which mounts on which truck? All three on all five, or lighter guns only on the Datum (4.5 t)? | **Decided: by role**, each truck confirmed: the table above |
| 7 | KO4: does a crew stay aboard a truck that cannot move while its gun still works? | **Decided: any faction's crew stays and fights** (beyond the agent's lean) |
| 8 | The order to build | **Decided: protection first**, as below |

**The order** (owner: protection first): (0) the armour base work on the five trucks, MV2; (1) the per-node weight; (2) the armoured cab
and the under-fire switch on the Talus; (3) the gun ring, our three guns, the shield and the ammunition locker, with their pictures;
(4) smoke dischargers, the ram bar and the EMP hardening kit; (5) the remote station; (6) the MaxxPro's extra mounts; (7) the other
four trucks, by role; (8) the stranded-crew rule; (9) other factions, behind their setting.

## Who chose what

- **Owner (19 Nov 2026):** the idea; a choice of mounts; weight counting like cargo; all five trucks with the Talus first; the
  under-fire switch; our own guns plus optional extras; no hard dependency; turbine engines noted beside it. Then: weight on the
  engine everywhere and cargo room on the Talus only; real kilograms; other factions later behind a setting; EMP hardening as its own
  kit; a remote station that fires with nobody aboard; guns by role; any faction's stranded crew staying to fight; protection first.
- **Owner (earlier, kept):** Combat Extended first with Vehicle Framework the fallback (18 Nov); armed as the real machine (18 Nov);
  no drag (13 Nov); passive gear costs materials and weight only (13 Nov); mods need active authors (22 Oct); no stand-in vehicle art.
- **Agent:** every figure in the kit and gun tables; the classes each kit reaches; the weight sum and the cab's 14 square metres;
  guns swung by hand; the static smoke launcher as the lead route; the mount verdicts by mod; the reasons in the guns-by-role table
  (the table itself is the owner's); the remote station's and the hardening kit's figures; the ammunition locker.

## Sources

- **Vehicle Framework**, `SmashPhil/Vehicle-Framework` (MIT): `VehicleComponent.cs`, `VehicleStatHandler.cs`, `VehicleUpgrade.cs`,
  `TurretUpgrade.cs`, `StatUpgrade.cs`, `CompUpgradeTree.cs`, `VehicleTurret.cs`, `CompVehicleTurrets.cs`,
  `VehicleStatPart_WeightUsage.cs`, `VehiclePawn_AI.cs`, `VehiclePawn_Combat.cs`, `TargetingHelper.cs`, `Patch_Combat.cs`,
  `Patch_NpcAi.cs`; checked against the installed build's decompile.
- **Combat Extended**, its public source and vehicle compatibility assembly: `VehicleArmorPatch.cs`, `VehiclesCompat.cs`, its `ModPatches` folders.
- **Other mods' Defs**, read with `scripts/query-defs.py` and from their Workshop folders, summarised in our words: the HUMVEE, GTek's
  L-ATV and MaxxPro, RimThunder Core, Roaring Tiger and Desert Sabre, Vanilla Vehicles Expanded with Tier 3 and Upgrades, Frontline
  Workshop, the Hind and the Mongoose. Activity and permissions with `scripts/workshop-research.py` and `config/source-repos.json`.
- **Our own**: `Powertrain.cs`, `Patches.cs`, `WeaponMounts.cs`, `VehicleEmp.cs`, `KnockOut.xml`, `Loadouts_Default.xml`,
  `Drivetrains.xml`, `CompPhobosGatherRun.cs`, the five trucks' Defs and trees, and the records linked above.
- **Real machines**: Carl O. Schuster, ["M54 Gun Truck: The Savior of the Support Convoy"](https://historynet.com/m54-gun-truck-the-savior-of-the-support-convoy/),
  *Vietnam* magazine (HistoryNet), Autumn 2022, read 19 Nov 2026. [Wikipedia, "Gun truck"](https://en.wikipedia.org/wiki/Gun_truck),
  read 19 Nov 2026, a secondary source citing Gardiner (2003) and Richard E. Killblane's *Circle the Wagons* (Combat Studies
  Institute, 2006), neither read. **Unverified, pages not readable on 19 Nov 2026**: a copy of US Army TB 9-2320-279-13&P on the
  HEMTT's crew protection armour kit; Defense Update's article on MTVR armour protection kits.
- **Not read**: VVE's smoke-pop code; Frontline Workshop's Combat Extended folder in play; the removed VVE - Upgrades Extended's trees.
  The density of steel is the standard figure.
