# Knocked out, not destroyed: what a vehicle loses before it is a wreck (16 Nov 2026)

A research and record round the owner asked for on 16 Nov 2026, after the destruction rule of Framework 0.114.5 ("parts doing real
work stay in the sum and pure mounting points come out"). The owner's thought: if a part matters because of its job, **losing the
part, or what feeds it, should cost the vehicle that job**: "turrets that become disabled through the ill of battle ... or simply
because they lose hydraulic pressure, or electrical power". Scope chosen by the owner: **turrets and weapons, mobility, working
equipment, crew and cab**. **Record only: nothing here is built, and nothing is tested in game.** The owner picks.

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
  of [round three](vehicle-crosslinks-round-three.md), marked "read first" there.**
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
