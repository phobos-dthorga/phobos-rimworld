# Knocked out, not destroyed: what a vehicle loses before it is a wreck (16 Nov 2026)

**Status (17 Nov 2026):** built in D'thorga Framework 0.115.0 (T1, T9, T10, M1, M2, C2, T2, C4, T3, T4, W1, W4), 0.116.0 (and
Vehicles 0.57.0: T8, M3, W2, W3) and 0.119.0 (T5 and T6, with the aircraft batch); not built: C3 (with hotboxing's cab air), and T7,
T11, M4, W5, W6 and C6. Not yet tested in game. M4 was read on 17 Nov 2026: in a raid a vehicle that cannot move is emptied by Vehicle
Framework's own orders, so it is a pillbox only at a base ([the reading](#m4-read-what-its-orders-do-with-a-vehicle-that-cannot-move-17-nov-2026)).

A research and record round the owner asked for on 16 Nov 2026, after the destruction rule of Framework 0.114.5 ("parts doing real
work stay in the sum and pure mounting points come out"). The owner's thought: if a part matters because of its job, **losing the
part, or what feeds it, should cost the vehicle that job**: "turrets that become disabled through the ill of battle ... or simply
because they lose hydraulic pressure, or electrical power". Scope chosen by the owner: **turrets and weapons, mobility, working
equipment, crew and cab**. It began as a record for the owner to pick from; what was built is in the status line above and the two
sections at the end.

## Read this first

- **Vehicle Framework already has the hooks for a weapon that stops, and almost nobody uses them.** Of the 60 weapons on the 27 armed
  vehicles in the owner's game, **2** are tied to a part and **none** carries a restriction. So today a tank fires at full pace until
  the moment the whole vehicle is destroyed.
- **Nothing ties a weapon to power, pressure or its own damage.** A turret's traverse, aim time and reload are read from its Def,
  shared by every vehicle that mounts it. No part's health changes them.
- **Part damage can reach every faction's vehicles; power and pressure cannot.** Other factions run our lite tier, which counts hits
  but runs no stages (owner's decision, 11 Nov 2026, no setting). A row built on Vehicle Framework's own hooks works for raiders; a row
  built on our battery or hydraulics works on the player's vehicles only. Each row says which.
- **Our own vehicles carry no weapons**, so the turret rows act on other authors' vehicles (owner, 13 Nov 2026: our conditions may
  scale other authors' figures through hooks that never skip their code). The working-equipment rows are ours.
  **19 Nov 2026:** the owner chose a choice of gun mounts by upgrade for our service trucks
  ([militarised service vehicles](../vehicle-militarised-service-vehicles.md), designed, not in code); once built, the turret rows act
  on ours as on any other.
- **One finding on the way** (not fixed, below): nothing makes a crew leave a vehicle that can neither move nor shoot.
- **A correction, made the same day:** the first version of this record said the people aboard our own vehicles could never be hit.
  **That was the agent's error** (its inventory script did not read seats' hit areas). Every seat on our vehicles has one; row C1 and
  the faults list are corrected below.
- **The short list** is at the end, with the owner's answers to its six questions.

## The principle

A vehicle can be lost in steps. Armoured-warfare writing names them; we use plain words in game:

| In game | The term | Meaning | Source |
| --- | --- | --- | --- |
| Cannot move | Mobility kill | "a vehicle that is immobilised due to damage or anti-tank obstacles" | [Wikipedia: Mobility kill](https://en.wikipedia.org/wiki/Mobility_kill), citing K. Mizokami, "How to Kill Tanks", Popular Mechanics, 5 Sep 2023 |
| Cannot shoot | Firepower kill | Loss of the ability to fire its weapons | Common usage; **unverified**: the US Army laboratory reports that define it could not be read (the archive refused the fetch) |
| Destroyed | Catastrophic kill | Beyond repair | The same Wikipedia page, in passing; otherwise **unverified** |

The destruction rule of 0.114.5 answers the last row. This record is about the first two, plus a third of our own: **cannot work**
(a tipper that cannot tip, a rig that cannot drill). **Our inference, not the sources':** each is the loss of one job, so each should
come from the parts and feeds that do that job, never from a hidden roll.

## What exists today

Read on 16 Nov 2026 with `scripts/query-defs.py` (every active vehicle Def, as written, parents resolved; other mods' patches
not applied), `scripts/query-code.py` and `scripts/query-patches.py`. "VF:n" is Vehicle Framework 1.7
(`.local/decompiled/VehicleFramework-1.7/Vehicles.decompiled.cs`), "CE:n" Combat Extended's vehicle compatibility
(`.local/decompiled/CE-VehiclesCompat/VehiclesCompat.decompiled.cs`). SmashPhil's and the CE team's code, summarised in our words.

### Weapons in the owner's game

| Mod | Armed vehicles | Weapons |
| --- | --- | --- |
| RimThunder - Roaring Tiger | 5 | 16 |
| RimThunder - Desert Sabre | 3 | 15 |
| Vanilla Vehicles Expanded - Tier 3 | 6 | 11 |
| Vanilla Vehicles Expanded | 7 | 7 |
| Frontline Workshop | 3 | 5 |
| Harrier, Hind, Mongoose | 3 | 6 |
| **All** | **27 of 78** | **60** (10 of them fixed smoke launchers) |

- **Tied to a part: 2** (the Roaring Tiger M1A2's and M48H's main guns need their `MainTurret` part above 1 %). Five armed vehicles
  have a part called `MainTurret` at all; the rest have nothing a gun could depend on.
- **Restrictions: 0. Must be deployed: 1** (the Burya). Nobody uses Vehicle Framework's `Reactor_TurretPopper`.
- The MaxxPro's gun arrives by an upgrade (its turret list is empty as written), so anything we do must also reach weapons added later.
- 16 of the 27 are tracked. The only parts any author marks as able to cap a stat alone are engines (57) and boilers (4).

### How Vehicle Framework stops a weapon

- A weapon is **disabled** when it is restricted, unmanned, not deployed as it needs, or its part requirement fails
  (`VehicleTurret.TurretDisabled`, VF:67432). `IsDisabled(out reason)` gives the player the reason (VF:68067).
- **A part requirement** names a part and a health share (`ComponentRequirement`, VF:9837); it is re-checked only when the vehicle's
  health changes, so it costs nothing between hits. A weapon mounted on another inherits its parent's (VF:67444). When it fails, the
  turret also swings back to rest (VF:68269).
- **A restriction** is one class per weapon with a `Disabled` test and a reason (`TurretRestrictions`, VF:66642). There is **one slot**:
  it comes from the turret Def's `restrictionType` or an upgrade (VF:68019, 36520), and `SetTurretRestriction` replaces whatever is
  there (VF:68051). Vehicle Framework ships one, "stationary only" (VF:66658).
- **Traverse, aim time and reload come from the Def**: `def.rotationSpeed` in `TurretRotationTick` (VF:68296), `WarmupTicks` from
  `def.warmUpTimer` (VF:67484), `MaxTicks` from `def.reloadTimer`, already scaled by how many gunners are aboard where the author gave
  a curve (VF:67563; RimThunder - Roaring Tiger's M1A2, M48H and M113(M) files give one). A Def is shared, so one vehicle's turret cannot be slowed by editing it.
- **No stat for weapons.** Parts feed `MoveSpeed` (500 parts in the owner's game), flight speed and control, cargo and our own work
  stats through their `categories`; there is no category a gun's parts could feed.
- One other mod patches turret targeting: the Hind's own steering-only rule (`query-patches`).

### How a vehicle stops moving

- A vehicle can move while `MoveSpeed` is above 0.1 (`VehiclePawn.CanMove`, VF:16256). Damage that takes it to 0.1 or under undrafts it
  on the spot (VF:10828).
- A vehicle with nobody aboard is no threat to anyone (`IsThreatToAttackTargetSearcher`, VF:17052): nobody shoots at it, which is how
  RimPacts' allies came to kick one for hours ([the finding](vehicle-health-design.md#our-parts-and-a-vehicles-destruction-16-nov-2026)).
- Ours already: stages that refuse to move or draft with a reason (worn-out brakes, no air, a dry tank), tread and brake wear,
  conditions such as the holed radiator, field fixes, the recovery winch and tow (Framework 0.103.0), and the claim order for an
  unattended vehicle (0.97.0).

### People aboard

- **A hit can already reach someone inside** (`VehicleStatHandler.HitPawn`, VF:12537; Combat Extended calls the same, CE:372). A hit
  landing on a seat's cells strikes one occupant with the seat's chance to hit (0.3 unless its author says otherwise, VF:20094), times
  1.25 on the seat's own cell, 1 or 0.75 on a neighbouring one. An outside hit reaches only seats marked exposed. **This answers G14
  of [round three](../vehicle-crosslinks.md#vehicle-cross-links-round-three-the-crew-weather-and-ground-combat-damage-electrics-and-hydraulics-13-nov-2026), marked "read first" there.**
- A seat is chosen when its hit area holds the struck cell (`TrySelectHandler`, VF:12610). A seat whose author gives no hit area gets
  the vehicle's centre cell (`ComponentHitbox.Initialize`, VF:9735). **Every seat on our own vehicles gives its cab cells** (read from
  `mods/PhobosVehicles/Defs/Phobos/Vehicles/*/Vehicle_*.xml`, 16 Nov 2026), enclosed, at the default chance.
- A seat counts as filled only by someone of the vehicle's faction who can do its job (`RoleFulfilled`, VF:20426); a weapon whose
  seat is not filled is unmanned, so disabled.
- **Nobody is ever told to get out.** People leave when the vehicle is destroyed (VF:18104) and after five idle hours on a map
  (12,500 ticks, VF:19791; our `CrewOnStation` holds that off while a station works). There is no rule for a burning vehicle or one
  that can no longer fight.

### Our systems, as feeds

- **Electrical:** a battery, an alternator, switched loads with amps and a shed order when the charging fails (cross-links G16).
- **Hydraulics:** fluid, pump and lines, on the working vehicles and (as flight hydraulics) on aircraft. The tipper's body and grab,
  the drill mast and the survey boom are paced by it and stop below a fifth (`HydraulicSupply`, `HydraulicRefusal`; Framework 0.73.0).
- **Air:** the compressor and brake pressure.
- **Work stats from parts:** tip rate, drill rate, pump rate, generator output, scan rate, sonar and listening range already fall with
  the health of the parts in their categories.
- **What reads nothing from any feed today:** the recovery winch and the hitch (`CompPhobosHitch`), the cargo hook
  (`CompPhobosSling`), and, as far as this round read, the Crawler's sonar, listening array and laboratories (**read first**).
- **Tiers** (`CompPhobosVehicleSystems.CompTickRare`): the player's vehicles run everything; other factions' count hits
  (`SweepPartHealth`), light the ground and cook off, and nothing else.

### Real turret drives (what was read)

| Vehicle | What a source says | Source | Standing |
| --- | --- | --- | --- |
| M2/M3 Bradley | "An electric motor drives a gearbox" for each of the traverse, gun elevation and TOW drives, and "all drives can be manually operated by the rotation of the handwheels at the gunner's station"; a lever selects power or manual | The US Army ordnance subcourse OD1609, lesson 1, as hosted by Integrated Publishing: [principles of operation](https://armyordnance.tpub.com/Od1609/Turret-Technical-Principles-Of-Operation-19.htm), [handwheel controls](https://armyordnance.tpub.com/Od1609/Gunners-Handwheel-Controls-27.htm) | Read. The page names no Army author; it does not say whether the guns fire in manual mode |
| A US tank to MIL-T-62222(AT) | A hand traverse crank whose effort "shall not exceed 17 lb", and a hand elevation crank at no more than 46 inch-pounds | [The specification's page 17](https://mil-spec.tpub.com/MIL-T/MIL-T-62222/MIL-T-6222200017.htm), same host | Read. **The page does not name the tank**; a search summary called it the M1A1's, which is unverified |
| M1 Abrams | Hydraulic, with two pumps and an accumulator | Forum posts only | **Unverified** |
| T-54/55 | Electro-hydraulic stabiliser, traverse about 10 to 19 degrees a second | A hobbyist page and game wikis that disagree with each other | **Unverified; not to be used** |
| M48, M10 Booker, M113, Wiesel, BMP-1, the Hind | Not searched to a readable source | None | **Unverified** |

**Our inference:** powered turrets on real fighting vehicles keep a hand-cranked backup, so a turret that loses its drive is slowed,
not dead. Two sources support it for two vehicles; it is a pattern we are assuming for the rest.

## Proposals

Every figure marked [A] is authored, for tuning. "Hook" means a Harmony postfix or a prefix and postfix pair that never skips the
original. Verdicts are the agent's, for the owner to overrule.

### T: turrets and weapons

| # | Idea | How | Lets | Stops | Changes | Built with | Other factions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| T1 | **A weapon needs its mount** | Every main weapon without a part requirement of its author's gets one: on the vehicle's own turret part where there is one, else on a **turret ring** part our loadout appends at the weapon's cells (it does a job, so it counts toward destruction). The weapon stops when the part is under 10 % (owner, 16 Nov 2026) | Silence a tank's gun without destroying the tank; repair the ring to get it back | Firing a gun whose mount is shot to pieces | A firepower kill exists, for and against the player | XML in loadouts, a little C# to set Vehicle Framework's own requirement at start-up; no hook | **Works for everyone**: it is Vehicle Framework's check, run on a health change | **Take, first** |
| T2 | **Damage slows a weapon before it stops** | Traverse falls with the ring's health, aim time rises with the gun's, reload with the loader's or breech's [A curves]; the factor is worked out when health changes | See a battered tank fight worse; choose to pull it back | A gun at 11 % health firing as if new | Damage is felt before the end | Hooks on `TurretRotationTick`, `WarmupTicks`, `MaxTicks` | **Works for everyone** (it reads part health, not our stages) | **Take, after T1** |
| T3 | **A powered turret needs power** | A turret drive is a switched electrical load (or draws hydraulic pressure); with the engine off it runs the battery down; with the battery flat, the load shed or the pressure gone, the turret is **hand-cranked**: traverse x0.2 [A], aim time x2 [A], never dead where the real vehicle has handwheels | Sit on silent watch and mind the battery; keep fighting, slowly, with a dead engine | Full-speed traverse from a vehicle with no power | The engine, alternator and battery matter in a fight | Our electrical loads and hydraulics, T2's hooks, one restriction class for a weapon with no hand backup | **Player's vehicles only**: stages do not run on the lite tier | **Take** (owner, 16 Nov 2026: hand-cranked; the player's vehicles only) |
| T4 | **The right drive for each vehicle** | Loadouts name the drive: electric (the Bradley, read), hydraulic, or **hand-swung** (a pintle or ring-mount machine gun, which needs no power and loses nothing). Fictional vehicles by class [A]: heavy guns powered, light mounts hand-swung | Know which of your vehicles can fight on a flat battery | A hand-swung machine gun going dead with the battery | T3 is honest per vehicle | XML in D'thorga Vehicles' loadouts | As T3 | **Take with T3; the real drives need sources first** |
| T5 | **A stabiliser to lose** | Without drive power the gun is not steadied: accuracy on the move falls [A] | Stop to shoot when the power is out | Accurate fire on the move from a dead turret | Halting to fire means something | The shot-report hook the avionics record lists for A12 | Player's only | **Hold: one accuracy rollout with avionics** |
| T6 | **Sights need power** | No power: night and poor-visibility accuracy fall to a plain sight's [A] | Fight by day when crippled | Night-sight accuracy with nothing powering it | Night fighting depends on a healthy vehicle | As T5 | Player's only | **Hold with T5** |
| T7 | **A wounded or missing loader** | Vehicle Framework already scales reload by gunner count where the author gives a curve; RimThunder - Roaring Tiger's M1A2, M48H and M113(M) do | — | — | — | Nothing | Everyone, already | **Nothing to build** |
| T8 | **The turret blown off** | Vehicle Framework's own `Reactor_TurretPopper` on the main turret part of tanks that carry their ammunition there, joined to our cook-off (G13) | A spectacular, deadly end for a burning tank | A tank shrugging off an ammunition fire | Fires in tanks are feared | XML | Everyone | **Taken** (owner, 16 Nov 2026: yes, with cook-off); it kills crews |
| T9 | **Aircraft stores** | A pylon or gun part requirement (T1) for the Harrier's, Hind's and Mongoose's weapons; release power belongs to the avionics record | Lose a rocket pod to flak and fly home | Firing from a shot-off pylon | As T1 | As T1 | Everyone | **Take with T1** |
| T10 | **An author's own rule always stands** | We never replace a part requirement or a restriction an author set; ours is added only where the slot is empty, and a weapon added by an upgrade is looked at when it arrives | — | Our patch silencing an author's design | — | A rule in the code | — | **A rule, with T1** |
| T11 | Random jams and stoppages | — | — | — | — | — | — | **Argued against**: a roll with nothing the player did or can do, as the fuses of G19 were |

### M: mobility

| # | Idea | How | Lets | Stops | Changes | Built with | Other factions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| M1 | **Say why it cannot move** | When damage takes a vehicle's speed to nothing, its inspect line and Health tab name the state and the one part that did it ("Cannot move: left track destroyed"). Vehicle Framework undrafts it without a word | Know what to repair, or what to shoot at | A vehicle that just stops | Immobilised becomes a state the player reads | Presentation only; the cause is read from the stat's parts | Everyone's vehicle shows it when selected | **Take, first** (cheap) |
| M2 | **A thrown track stops a tracked vehicle** | Track parts are marked able to cap `MoveSpeed` alone (Vehicle Framework's own `priorityStatEfficiency`), so one destroyed track means no movement, not a slower average. Wheels stay averaged: a truck limps on three | Stop a tank by its tracks | A tank driving on at three-quarter speed with a track gone | Tracks become the thing to hit and the thing to guard | A flag set at start-up from the loadout; no hook | **Works for everyone** | **Take** |
| M3 | **Fixed enough to move** | A field fix that brings a destroyed track or wheel back to a low health [A], good for the drive home | Limp out under your own power | Waiting for the depot with a whole part | Recovery has a cheap, slow option | The field fixes of Framework 0.36.0; read first what they cover | Player's only | **Read first** |
| M4 | **An immobilised vehicle still fights** | Nothing to build: a manned vehicle that cannot move keeps its weapons (subject to T1 to T3). What its lord then does with it was not read | — | — | A stuck tank is a pillbox | — | — | **Read first**: Vehicle Framework's and Custom Base Framework's vehicle lords |
| M5 | **Tow it home** | Already built: the recovery winch and tow take a disabled land vehicle, and the claim order takes an unattended one. With M2 and C2 an enemy tank stopped by its tracks and left by its crew can be claimed and towed | Capture what you immobilised | — | Mobility kills pay | Nothing new | — | **Check in play** |

### W: working equipment

| # | Idea | How | Lets | Stops | Changes | Built with | Other factions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| W1 | **The winch draws power** | The recovery winch is a heavy electrical load while it pulls [A amps; a real figure needs a source]; a flat battery refuses with a reason; with the engine running the alternator carries most of it | Plan a recovery; run the engine while winching | Pulling a 10-tonne truck on a flat battery | The winch joins the electrics | Our loads; `CompPhobosHitch` asks before a pull | A raider's winch is never used | **Take** |
| W2 | **The Crawler's sonar, listening array and laboratories as loads** | Each draws amps and has a shed order: laboratories first, sonar next, the listening array last [A] | Choose what to keep running on a weak battery | Every instrument at full power on a dying boat | Power is a submarine's budget | Our loads | Hostile boats run the lite tier: no effect | **Read first**, then take |
| W3 | **The tanker's pump** | Whether pumping needs the engine or the battery was not read | — | — | — | — | — | **Read first** |
| W4 | **Every powered fitting names its feed** | One small schema: a fitting or station says what it draws (amps, hydraulic pressure, air, engine power), so the same rule covers the winch, the future field kitchen's power lead and the accessories' fridge | One way to answer "why did it stop?" | Fittings that run on nothing | The owner's rule of 13 Nov 2026 ("an accessory uses up something real") gets one mechanism | A Def field and one service | Player's only | **Take, as the design rule for W1, the accessories and the trailers** |
| W5 | **The cargo hook's release** | Real hooks release electrically with a manual backup; nothing to gain in play | — | — | — | — | — | **Argued against** |
| W6 | The tipper, grab, drill mast and survey boom | Already paced by hydraulic fluid and stopped below a fifth; their work stats already fall with part health | — | — | — | — | — | **Done (0.73.0)** |

### C: crew and cab

| # | Idea | How | Lets | Stops | Changes | Built with | Other factions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| C1 | **Seats on our vehicles that can be hit** | Already so: every seat on our vehicles has its cab cells as a hit area, enclosed, at Vehicle Framework's default chance of 0.3. (The first version of this row called it a fault; that was the agent's error) | — | — | — | Nothing | Everyone who crews one | **Nothing to build** |
| C2 | **A crew leaves a vehicle that can neither move nor shoot** | At a rare tick, an AI crew gets out when the vehicle cannot move and has no working weapon, or is burning; then fights or flees on foot as its lord says. The player's crews get an alert, not an order | Finish a fight by disabling, not only by destroying; pick up the vehicle afterwards | Crews sitting in a dead, burning hull | Raids with vehicles end sooner and leave salvage | A cheap check on the lite tier (it reads two answers Vehicle Framework already has), `DisembarkAll` | **This row is for them** | **Take** (owner, 16 Nov 2026: AI crews leave, the player's get an alert) |
| C3 | **Smoke and heat in the cab** | A burning vehicle's cab fills: the people inside take smoke as in a burning room | A reason to get out | Sitting out a vehicle fire unharmed | Fire is the crew's problem too | Needs a cab-air model; the hotboxing design (I7) plans one | Everyone | **Hold: build with hotboxing's cab air** |
| C4 | **A hurt gunner aims and loads slower** | The gunner's sight and manipulation scale aim time and reload, as the driver system (G1) does for driving | Rotate a wounded gunner out | A half-blind gunner shooting like a fresh one | Crew health matters to firepower | T2's hooks, the pattern of `Input_Driver` | Everyone, cheaply, if read when the crew changes | **Take with T2** |
| C5 | **What a left vehicle is** | Already built: nobody aboard means no threat, and the claim order takes it. With C2, raiders' vehicles become loot that needs repair | — | — | Salvage is earned in parts and work, no value from nothing | Nothing new | — | **Check in play** |
| C6 | Crew panic and morale | — | — | — | — | — | — | **Argued against**: the game's mental breaks already cover people |

## Other factions' vehicles

| | What these rows do |
| --- | --- |
| **In a raid on us** | T1, T2, T9, M2 and C4 act on raiders' vehicles exactly as on ours, because they ride on part health and Vehicle Framework's own checks: shoot the ring to silence the gun, the track to stop the tank. C2 then gets the crew out. T3 to T6 and the W rows do **not** act on them: their battery and hydraulics never evolve on the lite tier. So a raider's turret is never slowed for want of power, only by damage |
| **Parked at a base we attack** | Nothing runs on an empty vehicle. Parts shot during the attack stay shot: a captured tank may need its ring or a track before it is any use |
| **Captured** | From the claim it runs on the player's tier and settings, with no catch-up for the hours before. Its weapons then feel T3 like any of ours |

**Cost with several on a map:** T1 and M2 cost nothing between hits. T2 and C4 keep one factor per weapon, refreshed on a health or
crew change, and add a multiplication where Vehicle Framework already ticks the turret. C2 is two reads per vehicle per rare tick.
T3 rides on stages the player's vehicles already run. None adds a per-tick scan.

## Saves

Nothing here needs a new saved field: a part's health is Vehicle Framework's, a requirement and a priority flag are set again at each
start, a factor is derived. **A turret ring appended to an existing vehicle** follows the rule for every appended part: appended at
the end of the list, healths kept by key (Framework 0.83.1). **Removing the mod** cuts appended parts back as the uninstall sweep
does today.

## Faults found on the way (listed, not fixed)

1. **Nothing makes a crew leave a vehicle that can no longer fight or is burning.** Row C2.
2. **A vehicle stopped by damage is undrafted without a word** (VF:10828). Row M1.
3. **Withdrawn:** "people aboard our own vehicles can never be hit". Wrong: the agent's inventory script did not read seats' hit
   areas, and the vehicles' XML shows every seat has one. Found when the owner chose to have it fixed.
4. Round three's G14 ("read first": do cab hits hurt the crew?) is answered above: yes, Vehicle Framework and Combat Extended both do.

## The short list

| Order | Rows | Why first |
| --- | --- | --- |
| 1 | **T1 with T9 and T10** | The owner's example, on Vehicle Framework's own hook, for every faction, with no patch |
| 2 | **M1 and M2** | Mobility kills that say what they are; both small |
| 3 | **C2** | Ends the dead-hull problem from both sides and makes salvage |
| 4 | **T2 with C4** | Damage and a hurt gunner felt before the end; the first hooks |
| 5 | **T3 with T4** | Power and pressure, the other half of the owner's example; needs real drive sources per vehicle |
| 6 | **W4, then W1** | One feed rule for fittings, with the winch as its first user |

## Questions for the owner

1. **A powered turret with no power:** slowed to hand-cranking (recommended, as the Bradley's manual describes) or stopped dead?
2. **Raiders and power (T3):** leave them out, since damage already reaches their weapons through T1 and T2 (recommended), or make an
   exception to the lite tier for weapon power?
3. **The turret blown off (T8):** yes, with our cook-off, or no?
4. **Crews bailing out (C2):** AI crews leave a vehicle that cannot move or shoot, or is burning, and the player's get an alert
   (recommended); or the player's leave by themselves too; or nobody?
5. **Where a weapon stops (T1):** when its mount is under 10 % (recommended, so a repair is needed but the part is not gone), or only
   when the mount is destroyed?
6. **What to build first**, if anything: the short list in order, or another pick?

**Answered by the owner, 16 Nov 2026:**

1. **Hand-cranked.** A powered turret with no power is slowed, never dead; a hand-swung mount loses nothing.
2. **Damage only for raiders.** Power and pressure act on the player's vehicles; the lite tier stays as it is.
3. **The turret blown off: yes, with cook-off** (T8 is taken; it follows the cook-off setting).
4. **AI crews bail out; the player's get an alert** (C2).
5. **A weapon stops when its mount is under 10 %** (T1).
6. **Fix the seats now, queue the rest.** The seats needed no fix (the correction above), so **nothing is built now**, and the short
   list waits behind the owner's standing order: the aircraft batch, the vaccines tie-in, then the Billy field kitchen.

## As built (17 Nov 2026, Framework 0.115.0)

The owner asked for steps 1 to 6 of the short list ("proceed with steps #1 through #6"). Built in six commits, one version; not yet
tested in game (in-game test pass, checks 120 to 125). Agent choices made while building, for the owner to revise:

- **One rules Def, `PhobosKnockOutRules`**, holds every figure and on/off; no player settings.
- **T1:** the mount is the vehicle's own part named in the rules (MainTurret, our Harrier's PhobosPylons), else an appended turret ring
  (60 health) or weapon pylons (40), inside, at the cells of the seat that works the weapon. Set on each weapon's template, the design's
  and its upgrades', through Vehicle Framework's own `ComponentRequirement`; not saved.
- **M2:** Vehicle Framework's own priority flag could not be used: it averages the priority parts too, so a dead engine beside two good
  tracks would have given a third of the speed. Our speed stat part returns nothing while a track is destroyed instead.
- **C2:** a crew counts as in danger when the vehicle burns, or cannot move (a vehicle whose design moves) and has no weapon whose mount
  works; fixed smoke launchers do not count as weapons.
- **T2, C4:** a mount at the stop line gives 0.55 of the weapon's pace, linear to full; the gunner's sight paces the aim, their
  manipulation the traverse and reload, never below half. Worked out once a second per weapon; three hooks that never skip the original.
- **T3, T4:** one electrical load, **turret drive** (40 A, shed last at rank 7), in the four base loadouts; hand-swung weapons by key
  patterns. Hand cranking: traverse 0.2, aim 0.5. A turret on a vehicle without the load (none in the owner's game) is never cranked.
- **W4, W1:** `FeedSpec` on a fitting (load, quantity, engine); the winch's load is 150 A, never shed, fitted with the winch.

## The owner's answers on the open rows, and what was built (17 Nov 2026)

| Row | The owner's answer | Status |
| --- | --- | --- |
| T5, T6 stabiliser and sights | Yes, with the aircraft air-defence batch (one aiming hook) | **Built: Framework 0.119.0** (not yet tested in game). The shared hook was wrong: the Harrier's evasion and a turret's aim are separate. Without Combat Extended, a postfix on Vehicle Framework's `TurretShotReport.HitReportFor` scales the turret's hit factor; under it, a prefix on Combat Extended's own shot-angle function widens the sway (Framework 0.120.2: the wrapper 0.119.0 put round `VehicleTurret.ProjectileAngleCE` looped with the Mongoose's own wrapper and closed the game, 17 Nov 2026). 0.6 on the move, 0.7 in poor visibility, authored |
| T8 turret blown off | Now, a small change | **Built: Framework 0.116.0.** Vehicle Framework's own reaction, subclassed to follow the cook-off setting and to be set off by a cook-off blast; only tanks with an ammunition rack part (RimThunder's) |
| M3 field re-fit | Yes, to a quarter | **Built: Framework 0.116.0.** The field-fix button (now Field repairs), twice a patch's work; a re-fitted part stays on the replacement list and out of free repair until replaced (agent's choice: so one component never stands in for a track) |
| W2 the Crawler's instruments | Laboratories first, then sonar, listening last | **Built: Framework 0.116.0.** Loads in the boat loadout at shed ranks 1, 2 and 4, fitted by stat, drawing only while each works |
| W3 the tanker's pump | Either: engine by default, an electric upgrade | **Built: Framework 0.116.0, Vehicles 0.57.0.** The pump was already engine-driven (since 0.106.1, which this record had missed); the new node lets it set up and pump on the battery |
| C3 cab smoke | Yes, with hotboxing | Waits for hotboxing's cab air |

All built rows are not yet tested in game (checks 126 to 129). **Correction:** this record (W3) and the agent's card said the tanker's
pump "runs on nothing"; it already needed the engine, through the shared set-up code.

## M4 read: what its orders do with a vehicle that cannot move (17 Nov 2026)

The owner asked for the outstanding reading (17 Nov 2026). Read from Vehicle Framework's 1.7 build (its own, of 14 Nov 2026) and Custom
Base Framework's Defs and code with `scripts/query-code.py` and `scripts/query-defs.py`. **Nothing is built from this, and none of it is
observed in game.**

- **In a raid, a vehicle that cannot move is abandoned at once; it is not a pillbox.** Vehicle Framework's raid (`LordJob_ArmoredAssault`,
  `VF:5806`) gives every vehicle the duty `VF_RangedAggressive` (`VF:6042`). Its think tree, in order: take up a firing position if it has
  a weapon; **if it cannot move and has people aboard, put them all out** (`VehicleActions.DisembarkAll`); drive at the nearest enemy;
  wait. "Cannot move" is Vehicle Framework's own `CanMove` (`VF:16256`): speed above 0.1, allowed to move, and its movement online. So a
  thrown track (M2), a dead engine or any refusal of ours that takes the speed to nothing empties a raider's vehicle at its next think,
  **whether or not its guns still work**.
- **The firing-position step never gives a job in this build.** `CombatPositionFinder.TryFindShootPosition` is an empty stub that always
  answers no (`VF:72814-72820`), and `JobGiver_RangedSupport` gives a job only when it finds one (`VF:3746-3780`). So a raid's vehicles
  never hold a firing position: they drive at the nearest enemy at a walk, their weapons firing by themselves on the way. No active mod
  patches either (`scripts/query-patches.py`).
- **At a base the player attacks, a stopped vehicle does stay a pillbox.** Custom Base Framework's duties send a vehicle that cannot move
  straight to waiting, crew aboard (`CBF_VehicleAssault`, `CBF_VehiclePatrol`), and `CBF_VehicleHold` only ever waits. Its weapons keep
  firing by themselves.
- **What the crew does on foot was not read to the end.** The raid's duty for people is the game's `Follow` (`VF:6046`), given with
  nobody to follow; that tree then wanders near where they stand. Whether they fight from there is not checked.

**Decided by the owner on 19 Nov 2026** (the open choice KO4, with [militarised service vehicles](../vehicle-militarised-service-vehicles.md)):
**any faction's crew stays aboard a vehicle that cannot move while a weapon of its still works.** Not built. It needs the raid's
"put them all out" step to wait for the weapons, by a think-tree step of ours ahead of it or a patch, and our own bail-out rule (C2)
read again against it.

**What this changes for our rows:**

- **M4's "a stuck tank is a pillbox" holds at bases and for the player, not in raids.**
- **C2 is narrower than its record says.** Its reason was that "Vehicle Framework only empties a vehicle when it is destroyed or has stood
  idle five hours"; in a raid Vehicle Framework already empties one that cannot move. C2 still adds the burning hull, and the vehicle that
  can still move but has no working weapon. Its "cannot move and cannot shoot" case is one the raid had already emptied.
- **Check 122 cannot tell the two apart**: destroying a raider's tracks alone should already bring the crew out, before the gun's mount
  is touched. Worth knowing when the row is tested.
- **A choice for the owner, not made:** leave it (a raider's crew abandons a tank that has thrown a track, guns working or not), or
  hold a crew aboard while a weapon still works, which our own condition could do as a think node added to that duty by XML patch,
  with no Harmony. It changes how another author's raids behave, so it is the owner's call.

## Weapon mounts audit (17 Nov 2026, Framework 0.117.1 and 0.117.2)

The owner's screenshot of a parked M2A3 Bradley at a full battery showed every weapon as "cranked by hand, no power", and the owner
asked for every vehicle's turret setup to be checked. Read with `scripts/query-defs.py` (as written, parents resolved) over all 60
weapons on 27 armed vehicles; the classification below is what the code now does, checked by a scratch copy of the rule.

**What was wrong:**

1. **Not drawing read as no power** (0.117.1). The turret drive draws only while someone mans the gun, so every unmanned weapon read as
   cranked by hand. Now only a drive that is cut, switched off or on a flat battery cranks.
2. **Every weapon riding on a turret took the turret's drive** (0.117.2). Right for a coaxial gun, which is fixed beside the main gun,
   and for the Bradley's TOW launcher on the turret's side; wrong for a commander's or loader's machine gun on its own ring or pintle
   on top of the turret, swung by hand. Seven weapons: the M10's and Desert Sabre M1A2's commander's M2HB, the Roaring Tiger M1A2's
   top M240 and top M2HB, the M48H's top M240, the T-54's DShK and the Marshal's mounted gun.
3. **A machine gun standing alone was matched by its key only** (0.117.2). The M113's M2HB has the key "mainTurret", so it read as a
   powered turret; its turret Def's name is matched now too. The MaxxPro's upgrade guns (M2HB and Mk 19, both on a gunner's ring) are
   matched by their turret Defs.
4. **Aircraft had a turret drive** (0.117.2; the agent's proposal, confirmed by the owner, 17 Nov 2026: "nobody hand-cranks a Hind"). Rocket pods and bombs hang fixed on pylons, and the
   Hind's and Mongoose's guns run off generators that turn with the engines; the load is gone from the aircraft loadout.
5. **Fixed smoke launchers took the drive's pace and read as ready while unmanned** (0.117.1).

**The rule now** (`TurretPowerRule.Classify`, with tests; hints in `KnockOut.xml`):

| Weapon | Drive | Found by |
| --- | --- | --- |
| Fixed (Static) | None | Vehicle Framework's turret type |
| Rides a turret, coaxial | The turret's | "coax" in its key or turret Def |
| Rides a turret, own mount | Hand-swung | a mount word in its key (topMG, topTurret, topMounted, MountedTurret, pintle, cupola), or worked by a different crew member from the turret |
| Rides a turret, same crew | The turret's | the gunner works both (the Bradley's and M10's coaxial M240, the TOW launcher) |
| Stands alone, machine gun or grenade launcher | Hand-swung | M2HB, M240, M249, Mk19, supportMG and the mount words, in its key or turret Def |
| Stands alone, otherwise | Powered, cranked by hand without power | everything else: main guns, autocannons, artillery, rocket launchers |

**What it gives, by vehicle:** powered: every tank's and IFV's main turret (M1A2 both, M10, M48H, Bradley, T-54, Wiesel, M113(M)'s
mortar turret) and Vanilla Vehicles Expanded's turrets (Bulldog, Bunsen, Highwayman, Roadkill, Scytheman, Tango, Toad, Badger, Burya,
Goliath's five, Marshal, Paladin, Udar). Hand-swung: the seven riding guns in fault 2, the M113's M2HB, the HEMTT's and M113(M)'s
support M240. With the turret: four coaxial guns and the TOW launcher. Fixed: ten smoke launchers. No drive: the six aircraft weapons.
Frontline Workshop's towed 105 mm howitzer has no engine, so it gets none of our systems.

**Limits:** the words are hints from the weapons in the owner's game; a mod whose keys say nothing falls back on the crew test, and a
weapon standing alone on another author's vehicle with no matching word reads as powered. The Roaring Tiger M1A2's top M2HB has no crew
role that works it (the author's Def), so it never fires; not ours to fix, and nothing dire, so no report drafted. Not yet tested in game.

## Their 20 Nov 2026 updates: the Harrier, Hind and Mongoose now crash by themselves

Read on 20 Nov 2026 from the builds the assembly archive kept that day (`python scripts/assembly-archive.py compare <file> --show
<Type.Member>` on `AV8BHarrier.dll`, `Mi24HindHelicopter.dll` and `MongooseAttackHelicopter.dll`), their Defs in the Workshop folder,
and `python scripts/query-patches.py who` on every method their new patches name. Nothing of ours was changed; nothing here is built.

**What the author added** (their code; figures are their defaults, authored):

- **The Harrier's engine or cockpit lost** (`EngineLoss.CheckGoingDown`, a postfix on `VtolFlightController.Tick`): with the part named
  by `engineComponentKey` (`Engine` in `Harrier_Jet.xml`) at 0 health in flight, the jet makes an emergency landing where it can, or
  crashes: a blast of radius 2.9, damage 30 and fire chance 0.8 (`crashRadius`, `crashDamage`, `crashFireChance`). A vertical landing
  under way is cancelled. `cockpitComponentKey` is not set in the Def, so the cockpit check does nothing as shipped.
- **Pilot loss** on all three (`PilotLoss` on the Harrier, `MapComponent_PilotLoss` on the helicopters): with no pilot alive and
  conscious in the air, a message and a crash slide or impact.
- **The helicopters' crash on a lost part** (`CrashOnComponentLossExtension`, critical parts Body and Cockpit) gains its own blast
  (`explosionRadius` 2.9, `explosionDamage` 30, `explosionFireChance` 0.8).
- **Cook-off of loose munitions**, patched in only under Combat Extended: the Harrier's 500 lb bomb item (`FWA_Bomb500lb`) and the
  Mongoose's Hellfire item wick and blow when burnt or badly hurt while lying on the map.
- **Low-tech armour** (all three; [vehicle armour](vehicle-armour.md) has it) and **airborne self-damage** filters: prefixes that skip
  `Thing.TakeDamage` and the vehicles' own `TakeDamage`.

**How ours meets it** (our reading of our code; not seen in play):

| Theirs | Ours | Verdict |
| --- | --- | --- |
| The Harrier crashes when its engine part reaches 0 in flight | Our breakdowns take engine health with no floor: the turbine system's "hot section worn" stage (`Systems_Turbine.xml`, `Effect_Breakdown` on the Engine role, mean 150 hours, 30 % of full health a time while running) and the service stages (`Systems_Service.xml`, 25-30 %, mean 3,000, 800 or 250 hours), through `CompPhobosVehicleSystems.BreakRole` (`src/PhobosFramework.Vehicles/Systems/CompPhobosVehicleSystems.cs:2283`) | **A chain, not a double.** A fourth breakdown from full, on a Harrier in the air on a map, now ends in the author's forced landing or crash: our breakdown message, then theirs, and one blast, theirs. Engine failure forcing a landing is the real outcome. **To watch**; if the owner finds it harsh, a floor of 1 health while airborne in `BreakRole`, as `WearLanding` already has |
| The same, from our other wear | Over-limit hovers use hot-section life on these aircraft, not engine health (`HoverLimits.Strain`); landing wear never goes below 1 health (`WearLanding`); dusty landings stop at `engineWearFloor` (`LandingSurface`) | Fine |
| The helicopters crash when Body or Cockpit is lost | Nothing of ours takes Body or Cockpit to 0 (`WearLanding` floors at 1); our air-combat hits are real damage, meant to bring aircraft down | Fine |
| Pilot loss | Nothing of ours downs crew in flight yet. C3 (smoke and heat in the cab, not built) would: then a downed pilot crashes these three, which is the right outcome. Say so in C3's design when it is built | Fine; a note for C3 |
| Loose-munition cook-off | Our cook-off (`src/PhobosFramework.Vehicles/Service/CookOff.cs`) is for ammunition stowed in a burning vehicle's cargo; theirs is the item on the map. A stowed item takes no damage, so it never wicks there | Fine: they meet at no point |
| Low-tech and self-damage filters | Both are off under Combat Extended, which the owner plays (the Harrier's through `Prepare`, the helicopters' inside `LowTechArmour.Ignore`); our air-combat damage goes through `VehicleStatHandler.TakeDamage` (`src/PhobosFramework.Vehicles/Service/AirCombat.cs:209`), beneath both, so our interceptors are never filtered | Fine |
| T9, aircraft stores (not built) | The Harrier's new `Patch_HarrierAirborneTurretButton` and `Patch_HarrierAirborneTurretTargets` change its turret button and targets in the air | **Read them before building T9** |
| Research: `HarrierNoVVEResearchPlacement`, `NodeResearchNote` | Placement acts on `Prowler_JetAircraft`, defined only in the Harrier's `NoVVE` folder; the owner plays with Vanilla Vehicles Expanded, so the project does not exist in their game (`query-defs.py` finds none) and nothing moves. The note adds research names to the Harrier's upgrade descriptions; we add no research to those nodes, and our `Cost_HarrierVtol.xml` still finds the blueprint's `researchPrerequisites` (`Harrier_Jet.xml:419`) | Fine |
| Their new Harmony patches | `query-patches.py who` on every target (`VtolFlightController` `ConsumeFuel`, `Tick`, `ResolveEmergencyLanding`; `Thing.TakeDamage`; the VTOL vehicle's `TakeDamage`; `VtolWorldFlightUtility.CanStartLocalFlight`; `Pawn_MindState.StartFleeingBecauseOfPawnAction`; parking spots; take-off points; `JobDriver_IdleWhileDespawned.GetReport`; `VehiclePawn.SpawnSetup`; `JobDriver.GetReport`; `CompGlower.SetGlowColorInternal`): none is a method we patch. The three mods each add the same prefix on fleeing and on the idle report, harmless and theirs. None fixes a fault in [our register](../temporary-patches.md) or the fault-reads | Fine |

The one real mismatch found is fuel, not a crash: the Harrier's plasteel armour costs 15 % more fuel, and our on-map fuel correction
hands that back ([thin air, turbines and aircraft fuel](../vehicle-altitude-and-turbines.md#their-20-nov-2026-updates-the-harriers-armour-fuel-is-handed-back-on-a-map)).

## Who chose what

- **Owner (16 Nov 2026):** the idea, the round, record only, all four areas, and the six answers above.
- **Agent choices, for the owner to revise:** every row, verdict, figure marked [A] and the short list; the readings of other mods'
  code; the inference that turret drives keep a hand backup.

## Sources

- Vehicle Framework 1.7 by SmashPhil, and Combat Extended's vehicle compatibility by the CE team: our decompiles, kept in `.local/`,
  cited by line above and summarised in our own words. Neither author endorses this record.
- US Army ordnance subcourse OD1609, "M2/M3 Bradley Fighting Vehicle: Turret", lesson 1, as hosted by Integrated Publishing:
  [principles of operation](https://armyordnance.tpub.com/Od1609/Turret-Technical-Principles-Of-Operation-19.htm) and
  [gunner's handwheel controls](https://armyordnance.tpub.com/Od1609/Gunners-Handwheel-Controls-27.htm), read 16 Nov 2026.
- MIL-T-62222(AT), [page 17](https://mil-spec.tpub.com/MIL-T/MIL-T-62222/MIL-T-6222200017.htm), same host, read 16 Nov 2026.
- [Wikipedia: Mobility kill](https://en.wikipedia.org/wiki/Mobility_kill), read 16 Nov 2026, itself citing Popular Mechanics.
- **Not read:** the US Army laboratory reports on vulnerability terms (the Defense Technical Information Center refused the fetch).
  Nothing here rests on them.
