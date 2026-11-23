# Modern military aircraft and vehicles: candidates (18 Nov 2026)

**Status (18 Nov 2026):** research and record only; the owner answered its thirteen open questions the same day
([their decisions](#the-owners-decisions-on-the-open-questions-18-nov-2026)). No vehicle or mechanic here is built; six
faults its code reads found in shipped code were fixed in D'thorga Framework 0.140.1, not yet tested in game.

How flight works now: [systems/flight.md](systems/flight.md); this record keeps its history.

Researched on 18 November 2026 by an agent at the owner's request: "I really, really enjoy the military aircraft in my
Rimworld gaming and to a slightly lesser extent, just interesting military vehicles that can also serve autonomously
... modern concepts are more my thing. But the Osprey is definitely something I would like to implement!" This is the
modern companion to [Unique military vehicles and submarines](military-vehicles-research.md) (9 Nov 2026), which covers
odd and historical machines; nothing from that record is repeated here. Owner choices, agent defaults and agent
recommendations are labelled as such.

How to read this file:

- **[P]** primary source (a maker, a service, a government agency or its own document); **[S]** secondary (Wikipedia's
  editors, a magazine, a news site); **[code]** read in code; **[I]** our inference; **[A]** an authored gameplay choice.
  Where sources disagree, both are given. Metric figures marked "(converted)" are ours.
- **Many primary sites refuse automated reading** (the US Air Force, Navy and Marine Corps, NAVAIR, the FAA, the RAAF
  and Australia's Department of Defence), so several entries rest on makers' pages and secondary sources. Each entry
  says which.
- Every claim about Vehicle Framework or another mod's code was read from that code, never run. **Nothing in this
  record is tested in game.**

## Read this first

- **No tiltrotor exists** in the owner's game (83 vehicles, 14 mods) or in the three Vehicle Framework collections on
  the Workshop (292 items). The V-22 is open ground.
- **The owner's three choices (18 Nov 2026)** shape every entry: the Osprey gets the longest entry here and no design
  record of its own yet; vehicles are **armed as the real machine is**, with Combat Extended ammunition; working
  without a crew is designed **three ways** (a set route, a control station, a mechanitor), with radio tie-in ideas.
- **An armed aircraft needs to fly on the map, and Vehicle Framework's aircraft do not.** Two other mods on the owner's
  list do: VTOL Vehicle (the Hind and Mongoose fly on it) and the Harrier author's fixed-wing code. Neither states a
  licence. **The owner chose to build on neither: we learn from both and write our own flight over the map**
  (decision 2 below). Nothing of theirs is copied or adapted; what they do is described here in our own words.
- **Working without a crew is already in Vehicle Framework 1.7**: a vehicle with no driving seat moves, launches and
  travels with nobody aboard, and an upgrade can remove the seat. Following someone is the part we would have to write.
- **Armour is decided** (decision 1): Combat Extended's rules first, with Vehicle Framework's own armour as the
  fallback for players without Combat Extended. This replaces the 9 November record's "unarmoured by default" for
  the vehicles in this record.
- **The radio framework stays on the back burner.** The owner asked for radio tie-in ideas; they are recorded as ideas
  and no radio feature is designed here.
- **These vehicles carry their real names** (decision 12): V-22 Osprey, K-MAX, C-27J Spartan. The names belong to
  their makers, as the Harrier's and Hind's do. Our utility vehicles keep their house names.

## The owner's choices (18 Nov 2026)

| Question | Owner's answer | What it changes |
| --- | --- | --- |
| How much to write for the Osprey | The longest entry in this list | One record. Its own design record waits for a go-ahead to build |
| Should the new vehicles be armed | Armed as the real machine | Each carries what the real one carries from its first Def, with Combat Extended ammunition. A machine that is unarmed in reality stays unarmed |
| How working without a crew should behave | All three: a set route, a control station, a mechanitor; and "our Radio mod on the backbench could have a genuinely good tie-in with this" | The three become levels of one system; a radio section is added as ideas |

## What is already there

### In the owner's game

From `python scripts/query-defs.py list --type "*VehicleDef"` on 18 November 2026: 83 vehicles from 14 mods, 12 of
them ours.

| Category | In the game today | Verdict |
| --- | --- | --- |
| Tiltrotor | none | **empty** |
| Helicopter | Louie, Mosquito, Cherokee (Vanilla Vehicles Expanded); Hind; Mongoose; Ka-27PS and Mi-38 (Rimarrow) | well covered |
| Attack helicopter | Hind (Mi-24), Mongoose (after the A129) | covered |
| Jet | Harrier (AV-8B) | thin |
| Propeller aeroplane | Warbird (a piston fighter), Smuggler (a small cargo plane) | thin; no Hercules-class transport |
| Side-firing gunship | none | **empty** |
| Tanker or radar aircraft | none | **empty** |
| Crewless aircraft or ground vehicle | none as vehicles (The Dead Man's Switch has drone pawns; IRBM has missiles) | **empty** |
| Mobile air defence | none as vehicles (fixed guns and launchers from IRBM, The Dead Man's Switch and Combat Extended) | **empty** |
| Rocket artillery | Burya and Scytheman (Vanilla Vehicles Expanded) | covered on one map |
| Tanks and armoured carriers | sixteen across four mods | well covered |
| Protected truck | MaxxPro, L-ATV, Humvee | covered |
| Amphibious, engineering, mine clearing | none | empty; see the 9 November record |

Our own twelve vehicles are utility vehicles, trailers and submarines. **None is an aircraft and none is armed**
([knocked-out systems](archive/vehicle-knocked-out-systems.md): "Our own vehicles carry no weapons").

### On the Workshop

`python scripts/workshop-research.py collection 3497770083 3419713592 3185234947 --grep "*word*"` on 18 November 2026,
over titles and the openings of descriptions. The same limits apply as on 9 November: mods outside those collections
and mods named only in Chinese or Japanese are not covered, and a mod's presence says nothing about whether it works.

| Searched for | Found |
| --- | --- |
| osprey, tiltrotor, v-22, condor, valor | **none** |
| hercules, c-130, ac-130, spartan (as an aircraft) | none |
| uav, reaper, ghost bat, k-max | none |
| himars, c-ram, miclic, line charge, themis, bushmaster, boxer | none |
| a-10, thunderbolt | none |
| unmanned | GTek's Vehicles - Ripsaw M5 UGV (3323900516; declares 1.5 and 1.6, not 1.7): a crewless tank with a 30 mm gun |
| stryker | Stryker M1126 ICV (3840894231; declares 1.6) |
| gunship | the Hind and Mongoose, and a Half-Life helicopter; no fixed-wing gunship |
| tilt | Vehicles - Pelican (Helldivers 2) (3370607942; 1.5 and 1.6), a science-fiction tiltwing dropship |

**One possible tiltrotor, not confirmed.** RimField5042 (3031623119; declares 1.5 and 1.4; last updated 25 Feb 2025)
adds "vehicles and aircraft from BATTLEFIELD 2042". Its page names no vehicle; one player comment mentions "the
MV38", which in that game is a tiltrotor. Its aircraft need a separate flight mod, and players report them unusable.
It does not declare 1.6 or 1.7.

**One aircraft to leave alone.** The Harrier's shipped source has a comment that a rule was "ported from A-10 08j"
(`StrayHits.cs:10`), so its author has or had an A-10 in work. See [the ground-attack jet](#the-ground-attack-jet-a-10c).

## What the frameworks allow

### Vehicle Framework 1.7

Read from Vehicle Framework's public source at commit `026420b5` (17 Nov 2026), which matches the installed build
1.7.2537; paths are inside its `Source/Vehicles/` folder. **[code]** throughout.

| Idea | Verdict | Evidence |
| --- | --- | --- |
| A vehicle that moves, launches and travels with nobody aboard | **Free.** A vehicle none of whose seats handles movement is marked autonomous; `HasEnoughOperators` is then always true, and drafting, launching and move orders pass | `Components/Vehicles/VehiclePawn/VehiclePawn_Health.cs:74-91`, `VehiclePawn_Handlers.cs:74-90`, `Comps/Flyer/CompVehicleLauncher.cs:211` |
| Caravans of crewless vehicles | Free: they form with no colonist, never stop for the night and are not lost when "all owners are downed" | `CaravanFormation.cs:230-240`, `VehicleCaravanPathingHelper.cs:48-62`, `Patch_CaravanHandling.cs:700-745` |
| Removing the driver's seat by upgrade | Free: a `VehicleUpgrade` role with `remove`; permissions are worked out again when an upgrade completes | `CustomFeatures/Upgrades/VehicleUpgrade.cs:172-229`, `VehiclePawn.cs:170-173` |
| A gun that fires and reloads with nobody aboard | Free: a turret no seat names counts as manned and reloads from cargo. Our L-ATV power rule already relies on it | `Turrets/Turret/VehicleTurret.cs:517-535`, `:1084` |
| A runway | Free: `LaunchRestriction_Runway` checks a strip in front of the aircraft and previews it; landing reads the same kind of restriction | `Takeoff/Restrictions/LaunchRestriction_Runway.cs:213-339`, `Targeters/LandingTargeter.cs:159-174` |
| Redirecting a flight in mid-air | Free: `controlInFlight` per aircraft | `World/Shuttle/AerialVehicleInFlight.cs:178-194` |
| Setting up before firing | Free: `deployTime`; the vehicle cannot move while deployed | `Comps/CompVehicleTurrets.cs:313-326` |
| **Two ways of taking off on one aircraft** | Moderate code of ours: one `launchProtocol` per vehicle as written. A launch style of ours holding two property sets and a saved switch would do it, with no Harmony **[I]** | `Comps/Flyer/CompProperties_VehicleLauncher.cs:95`, `CompVehicleLauncher.cs:437-446` |
| Fuel passed between two aircraft in flight | Small: an order between two flights close together. Nothing a flight carries ticks, so it cannot be a process | `AerialVehicleInFlight.cs:55` |
| Dropping troops or cargo by parachute | Moderate: `DropShip`, `Paratrooper` and `AirdropSupplies` exist, but only the raid arrival mode uses them | `Raiders/PawnsArrivalModeWorker_Airdrop.cs` |
| A crewless vehicle that follows someone | Substantial: driverless vehicles move on orders only; there is no follow, patrol or autopilot | not found |
| Strafing, bombing, reconnaissance, loitering | **Not usable.** The strafe arrival is commented out as "proof of concepts not finished"; `BomberProperties` is empty; nothing sets the reconnaissance order; loitering is a dev-mode flag | `Takeoff/LaunchProtocol.cs:679-688`, `Bombing/BomberProperties.cs` |
| Shooting at things in the air | Not there: turrets cannot target skyfallers, and the world-map flak is not registered | `AerialVehicleInFlight.cs:399-401` |
| Firing across the world map | Not there: no world targeting for vehicle turrets | not found |
| Real height | Not there: `rateOfClimb` and `maxAltitude` exist but nothing reads them | `AerialVehicleInFlight.cs:59` |

Four things a crewless design of ours must handle **[code]**:

1. **Enemies ignore an empty vehicle.** It counts as a threat only with people aboard or when a comp says so
   (`VehiclePawn_AI.cs:92-106`). An armed crewless vehicle needs a comp that says it is a threat.
2. **An empty vehicle ticks slowly**: every 250 ticks with nobody aboard and no ticking comps
   (`VehiclePawn_Tickers.cs:38-46`).
3. **Two checks read the seats as the Def wrote them**, not as an upgrade changed them (`VehicleRole.cs:226-227`,
   `Utility/Extensions/Ext_Vehicles.cs:52-60`). Removing the seat outright avoids both.
4. **Its author marks the permissions enum "Subject for removal"** (`Misc/Enums/Vehicle/VehiclePermissions.cs:5-7`).
   One helper of ours should read "can this move with nobody aboard", so a change there is fixed in one place.

Our seatless trailers already count as crewless this way, an open check in
[the dependency audit](dependency-audit-2026-11-16.md).

### Flying on the map: two other frameworks

A Vehicle Framework aircraft leaves the map when it flies, and its turrets tick only while it is spawned
(`VehicleTurret.cs:654`), **so its guns work only on the ground**. Two mods on the owner's list keep the aircraft on
the map. Read from our decompiles of the installed builds (VTOL Vehicle of 15 Nov 2026; the fixed-wing framework of
2 Nov 2026) and from the Harrier's own C# source, which ships in its mod folder. **[code]**

| | VTOL Vehicle (rabiosus; the Hind and Mongoose use it) | Fixed-Wing Aircraft Framework (the Harrier's author; ships inside the Harrier mod) |
| --- | --- | --- |
| What a vehicle needs | Its own `thingClass` (`VTOLVehicle.VtolVehiclePawn`), its turret class, and an `AircraftProperties` extension of about 100 fields | The same shape under its own namespace, plus `FixedWingProperties`; it is a fork of VTOL Vehicle with real fixed-wing flight added |
| Hover | **Yes, the default**: it stops over its move target (`VtolFlightController.cs:1484-1499`) | Only without `FixedWingProperties`, as a helicopter |
| Circle a point | **No** | **Yes, the default**: any move target becomes an orbit centre, with a radius, speed and direction per Def |
| Minimum speed, turn limit | No: the "fixed wing" fields only blend the handling | Yes |
| Take-off and landing | On the spot, never under a roof; landing needs every footprint cell unroofed and clear | A runway roll between four beacons on built floor (24 by 5 cells for the Harrier), approach, go-around, rough landings |
| Guns in the air | Yes: Vehicle Framework turrets fire airborne, with Combat Extended's own path; "combat directional hover" turns the hull so a named turret bears | Yes in the same turret class; **no installed aircraft shows a free turret firing while circling** |
| Gun and bomb runs | No | `Strafe` and `Bomb` run types, repeat runs. Every Harrier run is `Bomb`; **nothing uses `Strafe`** |
| Being shot at | A spawned pawn on the cell beneath; shooters under a roof are refused; damage goes through Vehicle Framework's parts | The same, copied |
| Troops leaving in the air | No: `CanAirDropNonPilotCrew` returns false | No |
| Crewless | Take-off asks Vehicle Framework's `HasEnoughOperators`, so it should fly **[I]**; nothing does yet | The same **[I]** |
| Art | "designed around a single texture viewed directly from above" (author, Workshop comment, 25 Oct 2026) | The same |
| Cost | 67 Harmony patch classes, several on methods every pawn calls; already paid in the owner's game. A parked aircraft ticks nothing | 72 more of its own, on the same methods |
| Licence | **None stated**: not in About.xml, the folder or the Workshop page | **None stated** |
| Author | Active: updated 15 Nov 2026 | Very active: same-day fixes on 15 Nov 2026 |

**Decided (owner, 18 Nov 2026): we build on neither.** "We simply learn where we can from said, named, mods because I
wish to go our own, more improved, route." Our aircraft will fly over the map on flight code of our own, in D'thorga
Framework, **designed for both hovering and circling from the start, with the hover half built first**. Because
neither mod states a licence, we take what they show is possible and how players use it, never their code. What
follows is what was read, kept as the starting point for that design.

What the reading says about an aircraft **[I]**:

- **A tiltrotor that hovers and fires its ramp gun is what VTOL Vehicle already shows is possible.** Its combat
  directional hover turns the tail to the target, which is how a ramp gunner covers a landing.
- **A gunship, a gun-pass jet and a circling drone all point at the fixed-wing framework.** It is under a month old,
  names its assembly after each build ("19o, 19p, ...", `FrameworkResolver.cs:8-13`), and reaches the game only
  inside the Harrier. It is an excellent source of ideas and a risky thing to build on yet.
- **Joining either changes a vehicle's class**, and a saved aircraft is stored under that class. Flight code of our
  own avoids that: one Osprey Def for every player, whichever other mods they run.
- **On permission, for the record.** The VTOL Vehicle author answered a question about other aircraft with: "It's
  possible, but you'd have to write the defs from scratch ... I didn't originally design this framework for other
  modders to use" (Workshop comment, 25 Oct 2026). That is not a licence, and with our own flight code none is needed.

**What our own flight should do better, from this reading [I]:**

- **Cost.** VTOL Vehicle carries 67 Harmony patch classes and the fixed-wing framework 72 more, several on methods
  every pawn calls. Ours should patch far fewer and none of those, inside the 0.05 ms a tick the vehicle model is
  allowed ([vehicle health](archive/vehicle-health-design.md)).
- **People leaving and boarding in the air.** Neither lets anyone down a rope or up a hoist. The Osprey has both.
- **A circle the player sets.** The fixed-wing framework fixes one orbit radius per Def; a gunship wants the player
  to choose it.
- **Other mods' air defence.** Aircraft that fly on the map are invisible to The Dead Man's Switch's and IRBM's air
  defences today ([air-defence tie-ins](air-defence-tie-ins.md)); ours should be visible to them from the first design.
- **No change of class**, so a save never holds a vehicle whose class has left the game.

### Sizes for comparison

From `python scripts/query-defs.py show` (Defs as written). Mass, cargo and speed are their authors' game figures.

| Vehicle | Footprint | Drawn size | Mass | Cargo | Seats | Flight speed |
| --- | --- | --- | --- | --- | --- | --- |
| Cherokee (after the CH-47) | 3 x 10 | 5 x 12 | 1,200 | 4,600 | 2 pilots | 4.5 |
| Louie (after the UH-1) | 3 x 7 | 5 x 9 | 780 | 780 | 1 + 5 | 9.95 |
| Hind | 3 x 9 | 12 x 12 | 1,100 | 250 | 2 + 6 | 10 |
| Mongoose | 3 x 7 | 11 x 11 | 600 | 120 | 2 | 10 |
| Harrier | 5 x 7 | 8.4 x 8.4 | 700 | 75 | 1 | 16 |
| Smuggler | 3 x 6 | 7 x 7 | 400 | 460 | 1 + 3 | 22 |
| Mi-38 | 5 x 11 | 24.8 x 24.8 | 9,500 | 5,000 | 2 + 30 | 14 |
| M1075 (ground) | 3 x 10 | 4 x 11 | 2,000 | 15,000 | 2 | |
| L-ATV (ground) | 2 x 4 | 5 x 5 | 400 | 550 | 4 | |

**For the tiltrotor [I]:** between the Hind and the Mi-38, about a 5 x 9 to 5 x 11 footprint. Aircraft that fly on
the map draw as one square picture seen from above, much wider than their footprint (the Hind's is 12 x 12 on 3 x 9),
so the picture would be 14 to 18 cells square. By the paid-generation rule it is drawn at 1.5 times that or more.

## The tiltrotor (V-22 Osprey)

**Named the V-22 Osprey in game** (owner, 18 Nov 2026: real names). Based on the Bell Boeing V-22 Osprey.

### The real machine

Sources: Bell Boeing's 2007 [V-22 Osprey Pocket Guide](https://community.sketchucation.com/uploads/imported_attachments/K2W7_V-22_64214_pGuide.pdf)
("Approved for Public Release", read from a third-party host) **[P]**; a
[V-22 fact sheet of March 2002](https://www.popasmoke.com/misc/V22factsheet.pdf) naming the programme office, with no
publisher printed **[P, probable]**; [Bell's product page](https://www.bellflight.com/products/bell-boeing-v-22)
**[P]**; [Boeing's product page](https://www.boeing.com/defense/military-rotorcraft/v-22-osprey) **[P]**;
[Wikipedia](https://en.wikipedia.org/wiki/Bell_Boeing_V-22_Osprey) **[S]**.

| What | Figure | Source |
| --- | --- | --- |
| Greatest weight taking off vertically | 52,600 lb (23,859 kg) | [P] guide, Bell, Boeing |
| Greatest weight with a short rolling take-off | 57,000 lb (25,855 kg) | [P] guide, Bell |
| Greatest weight for a ferry flight | 60,500 lb (27,443 kg) | [P] guide, Bell |
| Empty weight | 33,140 lb (15,032 kg) [P] fact sheet; 33,835 lb (15,347 kg, converted) [P] guide; 31,818 lb (14,432 kg) [S] | sources disagree |
| Cruise | 250 kt (463 km/h) at sea level and 47,000 lb [P] guide; 266 kt (493 km/h) [P] Bell; 242 to 255 kt [P] fact sheet | sources disagree |
| Range | "resupply bases as far as 400 nm on one tank" (741 km, converted) [P] Bell; combat range 390 nmi (720 km) [S] | |
| Ferry range | 2,100 nm (3,892 km) [P] fact sheet; 2,230 nmi (4,130 km) [S] | |
| Ceiling | 25,000 ft (7,620 m) [P] guide, Bell | |
| Ceiling on one engine | 10,300 ft (3,139 m) guide; 9,500 ft Bell; 9,400 ft fact sheet | all [P] |
| Hover ceiling, out of ground effect | 5,400 ft (1,646 m) at 47,000 lb [P] guide; 6,000 ft (1,829 m) [P] fact sheet | |
| From helicopter to aeroplane flight | "in as little as 16 seconds" | [P] guide |
| Engines | two Rolls-Royce AE1107C, 6,150 shp (4,586 kW) each | [P] guide, Bell, Boeing |
| Seats | a crew chief and 24 troops; 9 stretchers (guide) or 12 (fact sheet) | [P] |
| Cabin | 5.7 ft wide, 5.5 ft high, 20.8 ft long (1.74 x 1.68 x 6.34 m, converted) [P] guide; 1.80 m wide, 1.83 m high, 7.37 m long [P] fact sheet | sources disagree |
| Load inside | up to 20,000 lb (9,072 kg, converted); a 2,000 lb winch | [P] guide |
| Hooks | two; 10,000 lb (4,536 kg) on one, 15,000 lb (6,804 kg) on both [P] guide, fact sheet. Bell's page says 12,500 lb on both | sources disagree |
| Rescue hoist | at the ramp, 600 lb, 250 ft of cable; two fast-rope points | [P] guide |
| Cabin fuel tanks | up to three, 430 gal (1,628 L) each | [P] guide |
| Width | 84 ft 7 in (25.8 m) with rotors turning; 18 ft 5 in (5.61 m) folded | [P] guide, fact sheet, Boeing |

**Five things about it that make mechanics:**

1. **One engine turns both rotors.** "The interconnect driveshaft provides safe one-engine-out flight in all modes of
   operation" **[P, guide]**, but "the V-22 is generally not capable of hovering on one engine" **[S, Wikipedia]**; no
   primary source was found that says the second part directly. Our
   [steam and diesel-electric research](steam-and-diesel-electric-research.md) already holds this precedent.
2. **Small vehicles ride inside; ordinary ones do not.** The guide lists the M151 Jeep and the M274 Mule as fitting
   **[P]**; the M1161 Growler, 1.5 m wide, was "designed specifically for use with the V-22"
   **[S, [Wikipedia](https://en.wikipedia.org/wiki/M1161_Growler)]**. A Humvee is 2.16 m wide
   **[S, [Wikipedia](https://en.wikipedia.org/wiki/Humvee)]**, wider than the cabin **[I]**.
3. **Its downwash and exhaust do damage.** "Maximum rotor downwash speed of over 80 knots" **[S, Wikipedia]**; its
   exhaust lit grass at a North Carolina range in June 2013
   **[S, [WTKR](https://www.wtkr.com/2013/06/21/report-v-22-osprey-sparks-grass-fire-at-dare-county-bombing-range)]**;
   in May 2015 at Bellows, Hawaii, dust raised by its own rotors stalled an engine during repeated landing attempts,
   and two Marines died
   **[S, [Military.com](https://www.military.com/daily-news/2015/11/23/ospreys-own-rotor-wash-led-to-deadly-crash.html)]**.
4. **It carries little armament.** A ramp gun is listed as mission equipment **[P, guide]**: a 7.62 mm M240 or a .50
   **[S, Wikipedia]**, and a GAU-21 .50 in a
   [Marine Corps photograph of 2018](https://www.dvidshub.net/image/4800142/mv-22b-tail-gunnery-certification) **[P]**.
   A belly turret with a 7.62 mm GAU-17 minigun, remotely worked, exists but "found limited use because of its 800 lb
   (360 kg) weight" **[S, Wikipedia]**. Rockets and small missiles were test-fired, not fielded **[S, Wikipedia]**.
5. **The tanker kit was built and then set aside.** A roll-on kit to refuel other aircraft was tested with an F/A-18
   in 2013, planned to give about 10,000 lb of fuel
   **[S, [Military.com](https://www.military.com/dodbuzz/2016/10/26/new-system-will-allow-ospreys-refuel-f-35s-flight)]**
   or carry up to 12,000 lb **[S, Wikipedia]**; in May 2025 the US Navy said it was not pursuing it **[S, Wikipedia]**.
   The aircraft does **take** fuel in flight, through a retractable probe **[P, guide]**.

Also real: it folds (blades in, nacelles down, the wing turned along the body) in about 90 seconds **[S]**, in winds
up to 45 knots **[P, guide]**; the Air Force version has terrain-following radar **[P, guide, Bell]**; its landing gear
is built for rocks, sand, grass and snow **[P, guide]**.

### What each behaviour does to the player

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **Two ways up** | Take off straight up from any open ground; or roll along a clear strip with about two tonnes more aboard (25,855 against 23,859 kg; the difference is our arithmetic) | Lifting the heavier load without a strip | A strip at home, however short, is worth laying; the far end of the trip decides the load |
| **Aeroplane flight between maps** | Reach a place a helicopter cannot, and land there without a runway | Nothing | Distant sites, rescues and outposts come inside one flight |
| **Hover is the thirsty part** | Nothing new; it is a cost | Loitering in the hover | Short hovers and quick turnarounds pay; fuel by flight phase is built |
| **Thin air** | Nothing new; it is a limit | Lifting the full vertical load on a hot day or from high ground (the real hover ceiling is 1,646 m at 21.3 t) | The rolling take-off is the answer to a high or hot base |
| **One engine out** | Fly home on one engine | Hovering: it must land rolling on a strip, or drop its load and fuel to come down vertically at all | Engine damage becomes "where is the nearest strip", not "it crashed" |
| **Hook** | Sling a vehicle or a load up to the real 4,536 kg on one hook | Slinging what only the Cherokee's 11,800 kg hook lifts | Two lifters with different jobs: fast and far, or heavy |
| **Cabin fits, one at a time** | Seats, stretchers, ferry tanks, or the tanker kit | Having two at once | The fit is chosen for the trip, at home |
| **What it lands on** | Land anywhere open | Landing freely on loose sand or dry growth without wear or a fire risk | Paved pads at the places it uses often |
| **Its guns** | Cover a landing from the ramp; a belly gun that needs no gunner | Fighting like a gunship: one rifle-calibre or .50 gun, and a belly gun that costs 360 kg of load | It defends a landing; it does not attack |
| **Crewless fit** | Send it on supply flights between the player's own places with nobody aboard | Sending it anywhere new, or into a fight, without a control station | Routine haulage stops costing a pilot |

### How each would work

**Two ways up.** One aircraft, a saved switch, two sets of take-off properties: vertical, as Vehicle Framework's
`VTOLTakeoff`; rolling, with a runway restriction on that set only. The load limit per way goes in our existing lift
check and launch refusal ([altitude and turbines](vehicle-altitude-and-turbines.md)). The refusal names the limit:
too heavy to lift straight up from here, and by how much. **[I]**; moderate code.

**Helicopter on the map, aeroplane between maps.** The real conversion takes sixteen seconds, so the honest split is
that the aircraft is a helicopter whenever it is near the ground and an aeroplane on the world map: world speed and
fuel follow the aeroplane, the map follows the helicopter. No visible tilt is needed in flight between maps, and on the
map the nacelles are upright. **[A]**

**Thin air and one engine.** Both come from systems already built: `AirRule` for height and heat, the turbine and
multi-engine models for one engine driving both rotors ([power and lift](archive/vehicle-power-and-lift-design.md),
[turbines](archive/vehicle-turbine-design.md)). What is new is the consequence: with one engine the hover margin is below zero
at any useful weight, so the aircraft may only take off or land rolling, and the refusal says so.

**Hook.** Our [sling](archive/vehicle-sling-design.md) as built, with a hook class of 4,536 kg between the medium class (4,080
kg) and the Cherokee (11,800 kg). The second hook is an upgrade that raises it to 6,804 kg. A loaded Pintle trailer is
a vehicle, so it could be slung as a cargo load with no new code **[I]**, subject to its weight.

**No vehicle inside.** Nothing on the owner's list is narrow enough for the real cabin **[I]**, so the tiltrotor
carries no vehicle inside, and the question of one vehicle inside another does not arise here. It stays open for the
transport aeroplane.

**Cabin fits.** Alternatives that fill the same place, so one "only one of these" row in the tree: troop seats (the
bare cabin), stretchers (our litter kit, built), ferry tanks (the long-range family, built; the real ones are 1,628 L
each, up to three), and the tanker kit. **The tanker kit is offered although its real programme stopped**, because the
hardware flew; the owner chose to keep it (18 Nov 2026). In play it is an order between two flights close
together on the world map, moving fuel from the kit's tank to the other aircraft, since nothing ticks in flight.

**What it lands on.** Three cases from the three real events, each a condition in our health layer rather than a new
system: loose sand or soil under the rotors adds wear to the engines for every landing and take-off (the Bellows
crash); dry growth under the exhaust has a chance of catching (the 2013 grass fire), highest in a heat wave; built
floor or rock costs nothing. This gives the player a reason to pave a pad, and it is the one place the downwash is
modelled. **[A]**; each figure would be authored and labelled so.

**Its guns.** Combat Extended already has the ammunition in the owner's game: `AmmoSet_762x51mmNATO` for the M240 and
the GAU-17, `AmmoSet_50BMG` for the GAU-21 (checked with `query-defs.py`, 18 Nov 2026). The ramp gun is a turret with
an arc to the rear, worked from a seat. The belly gun is a turret no seat names, so it fires with nobody at it under
our [weapon-station power rule](archive/vehicle-knocked-out-systems.md), and its node costs load. On Vehicle Framework alone
both fire only when landed; on VTOL Vehicle the ramp gun fires in the hover.

**Hoist and ropes.** The real aircraft has a rescue hoist and two fast-rope points. Neither flight framework lets
anyone leave or board in the air, so these would be our own code on top of on-map flight: an order that puts one
pawn down or takes one up while hovering. Recorded as an idea, not costed.

**Folding.** Probably not worth building. A vehicle's footprint cannot change in play as far as we have read **[I,
not checked in code]**, so folding would save no room and change no outcome, and a behaviour with no answer to the
three questions is not built.

**Crewless.** See [working without a crew](#working-without-a-crew-three-levels-of-one-system). The real basis: Bell's
V-280 "flew autonomously for the first time Dec. 18"
**[P, [Bell](https://news.bellflight.com/en-US/189270-bell-v-280-flies-autonomously-for-first-time)]** (2019 by the
page dates, our inference), doing a take-off, conversion, waypoints, a loiter and a landing with safety pilots aboard
**[S, [Defense News](https://www.defensenews.com/land/2020/01/09/bell-v-280-flies-autonomously-for-first-time/)]**;
and Bell's V-247 Vigilant, a crewless tiltrotor concept with a 9,000 lb sling load
**[P, [Bell](https://www.bellflight.com/products/bell-v-247)]**, shown as a model in 2016 **[S]**.

### Which framework it flies on

| | On Vehicle Framework alone | On VTOL Vehicle, as a Def that exists only with it |
| --- | --- | --- |
| Flight between maps | Yes | Yes (it hands over to Vehicle Framework at the map edge) |
| Hovering over the map, guns in the air | No | Yes |
| Two ways up | Our launch style | Our own strip check on top of its take-off; not read in detail |
| Sling, water bucket, litter | Built | Built: our `OnMapFlight` already reads both frameworks |
| Works without VTOL Vehicle | Yes | No |
| Permission | Not needed | None stated; ask first |

**Decided (owner, 18 Nov 2026): neither column.** The Osprey flies over the map on flight code of our own, so it
keeps the second column's hovering and guns in the air and the first column's "works for every player". The table
stays as the comparison that led there. The agent had recommended the second column if its author agreed.

### An upgrade tree, sketched

By the branching rules: branches only where one upgrade does nothing or makes no physical sense without another.
Agent-authored; no figure is set.

| Row | Nodes | Link |
| --- | --- | --- |
| Cabin fit | Stretchers; ferry tanks; tanker kit | Only one of these (the bare cabin is troop seats) |
| Hooks | Cargo hook; then second hook | The second needs the first |
| Guns | Ramp gun; belly gun | Free-standing; the belly gun costs load |
| Flying | Refuelling probe; terrain-following radar (night and weather, through the [avionics fit](archive/vehicle-avionics.md)) | Free-standing |
| Crew | Crewless fit | Free-standing |

The body is drawn bare, and each visible node is its own overlay: the hooks, the ramp gun, the belly turret, the probe,
the radar nose.

### Other factions' tiltrotors

| Where | What happens |
| --- | --- |
| A raid on us | Only if something brings one. VTOL Raids brings aircraft already flying at the map edge; a tiltrotor of ours there would be a troop carrier that lands and unloads, which that mod's "combat landing" behaviour already describes ([air-defence tie-ins](air-defence-tie-ins.md)) |
| A base we attack | Parked, on the lite tier: hits are counted, nothing else runs ([other factions audit](vehicle-other-factions-audit.md)) |
| One we capture | Claimed with the claim order; it moves to the full model and works for us at once |

### Art

One picture from above for the on-map shape, drawn bare with nacelles upright and rotors as separate turning
overlays; a second body for parked or between-maps use only if the Vehicle Framework shape is chosen. Overlays turn
flat, like a clock hand ([animation research](vehicle-animation-research.md)), so nacelles cannot visibly tilt:
upright and forward would be two pictures. One pilot picture first, inspected at full size.

## The other aircraft

Each is shorter than the tiltrotor's entry. Order within this section is not the recommended order, which is in
[its own table](#recommended-order).

### The cargo helicopter that is all hook (Kaman K-MAX)

**Real basis.** Two intermeshing rotors and no tail rotor, one seat, and a hook that lifts 6,000 lb (2,722 kg)
**[P, [Kaman](https://kaman.com/kaman-announces-first-flight-of-unmanned-k-max-titan/)]**, "greater than the
helicopter's empty weight" of 5,145 lb (2,334 kg) **[S, [Wikipedia](https://en.wikipedia.org/wiki/Kaman_K-MAX)]**.
Its greatest weight with a load is 12,000 lb, with one seat and a top speed of 80 knots under load
**[P, [FAA type certificate data sheet](https://kaman.com/wp-content/uploads/2025/09/FAA-K1200-Type-Cert-TR7BO.pdf)]**.
Its blades are pitched by small flaps, without hydraulics **[S, Wikipedia]**. A crewless version flew supply runs
for the US Marine Corps in Afghanistan from November 2011
**[P, [Lockheed Martin, 2012](https://news.lockheedmartin.com/2012-07-31-U-S-Marine-Corps-to-Keep-K-MAX-Unmanned-Cargo-Re-supply-Helicopter-in-Theater-for-Second-Deployment-Extension)]**
and delivered "more than 4.5 million pounds of cargo" over nearly three years
**[P, [Lockheed Martin, 2014](https://news.lockheedmartin.com/2014-07-24-Lockheed-Martins-Unmanned-K-MAX-Cargo-Helicopter-Team-Returns-From-Deployment-With-U-S-Marine-Corps-In-Afghanistan)]**,
mostly at night, with people on the ground hooking the loads on **[S, Wikipedia]**. A four-hook carousel let it make
several drops in one flight
**[P, [Lockheed Martin, 2010](https://news.lockheedmartin.com/2010-02-08-Team-K-MAX-Demonstrates-Successful-Unmanned-Helicopter-Cargo-Resupply-to-U-S-Marine-Corps)]**.
One was lost in June 2013 when a tailwind set its load swinging **[S, Wikipedia]**. It also fights fires with a
700-gallon bucket
**[S, [Fire Apparatus Magazine](https://www.fireapparatusmagazine.com/equipment/firefighter-drones/wildland-firefighting-aircraft-fixed-wing-rotors-drones/)]**.
Production ended in 2023 after 60 aircraft
**[S, [Vertical](https://verticalmag.com/news/kaman-to-discontinue-k-max-production/)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| All hook | Sling more than its own weight for little fuel | Carrying people or cargo inside: one seat, no cabin | A cheap lifter beside the Cherokee |
| Carousel (node) | Drop up to four loads at four places in one flight | Slinging a vehicle while it is fitted | Outposts are supplied in a round, not one flight each |
| Crewless fit (node) | Run that round with nobody aboard | Hooking on by itself: someone on the ground does it, as in Afghanistan | Supply becomes a schedule |
| Bucket | Fight fires from open water | Nothing | Shares our water bucket |

**Armed as the real machine:** unarmed. **Reuses:** the sling, the water bucket, cargo refuelling, the turbine model.
No flight hydraulics to leak, which is a real difference our fluid systems can show. **Feasibility [I]:** the
aircraft is ordinary; the carousel needs slung cargo loads rather than slung vehicles, which the sling does not do
yet; crewless rounds need a schedule that launches flights. **Other factions:** as a parked lifter at a base; it
brings nothing to a raid.

### The side-firing gunship (AC-130J; the smaller MC-27J)

**Real basis.** The AC-130J carries a 30 mm GAU-23/A and a 105 mm gun firing from the left side, with small bombs and
missiles on the wings and from tubes in the ramp
**[S, [Air and Space Forces](https://www.airandspaceforces.com/weapons-platforms/ac-130j/)]**. It attacks in "a
pylon turn, flying in a large circle around the target", so it can keep firing far longer than a passing aircraft,
and usually at night, because it is large, slow and low
**[S, [Wikipedia](https://en.wikipedia.org/wiki/Lockheed_AC-130)]**. It is 29.8 m long with a 40.4 m wingspan **[S]**.
The smaller relative is Leonardo's MC-27J: the C-27J transport with a 30 mm gun on a pallet, firing through the
left rear door, "installed in the aircraft within four hours"
**[S, [Airforce Technology](https://www.airforce-technology.com/projects/mc27j-multimission-aircraft-italy/)]**, a
roll-on fit that "retains all C-27J Spartan transport / airdrop capabilities"
**[P, [Leonardo presentation, 2021](https://www.smgconferences.com/documentportal/ComplementaryContent/4080.pdf)]**.
Italy planned to field it by 2030
**[S, [Janes, 2022](https://www.janes.com/defence-intelligence-insights/defence-news/air/italian-air-force-to-deploy-mc-27j-praetorian-by-2030)]**;
its status since is not found.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Circle and fire | Hold a gun on one place for as long as the fuel lasts | Hitting anything on its right side, under a mountain roof, or once ground fire drives it off | A fight gets a second, slower clock: the gunship's fuel |
| Big, slow and low | Nothing; it is the cost | Surviving real anti-aircraft fire | It is for enemies without air defence, as in reality |

**Armed as the real machine:** Combat Extended has `AmmoSet_30x173mmNATO` and `AmmoSet_105mmHowitzerShell` in the
owner's game. **Feasibility [I]:** it needs flight on the map, and circling exists only in the fixed-wing framework,
where no aircraft yet fires a free turret in orbit. **The full AC-130J is too large:** at the Harrier's scale (8.4
cells for a 14 m jet) it would be about 18 cells long and 24 across, with a runway far beyond the Harrier's 24 by 5.
**Agent recommendation: build it as the transport aeroplane's gun kit, not as its own aircraft.** That is what
Leonardo did, it shares one body and one runway, and it fits the "only one of these" rule for the hold.

### The rough-strip transport aeroplane (C-27J Spartan)

**Real basis.** Leonardo's twin-engine transport: 11,300 kg of load, 602 km/h, "short take-off and landing" on
"unprepared runways" **[P, [Leonardo](https://aeronautics.leonardo.com/en/products/c-27j-spartan-next-generation)]**;
a take-off run of 690 m at full weight and a landing run of 390 m on a strip at least 12.1 m wide; up to 60 troops,
46 paratroops or 36 stretchers; up to 9,000 kg dropped by parachute; a roll-on fire-fighting tank of 6,000 or
8,000 L **[P, Leonardo presentation, as above]**. Other sources give shorter runs (about 580 m; "under 500m")
**[S]**. It is 22.7 m long with a 28.7 m wingspan **[S, [Wikipedia](https://en.wikipedia.org/wiki/Alenia_C-27J_Spartan)]**.
A Humvee or an M113 drives on through the rear ramp
**[S, [Airforce Technology](https://www.airforce-technology.com/projects/spartan/)]**. **The Royal Australian Air
Force flies ten**, with No. 35 Squadron at Amberley, and they reach about 400 Australian airfields against about 200
for the Hercules
**[P, [Leonardo](https://leonardo.com/en/news-and-stories-detail/-/detail/c-27j-spartan-from-italy-to-the-us-destination-australia)]**;
Australia announced in April 2026 that it will replace them
**[S, [The Aviationist](https://theaviationist.com/2026/04/16/australia-announces-c-27j-spartan-retirement/)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Runway only | Move the biggest loads by air | Using any place without a strip at both ends, or dropping instead | An airstrip becomes a thing worth building and defending |
| Parachute drop | Deliver cargo or troops to a place with no strip, without landing | Bringing anything back from there | Supply and reinforcement of a camp, an outpost or a fight |
| Hold fits, one at a time | Stretchers, a fire-fighting tank, a tanker fit, or the gun kit | Having two at once | One airframe, several jobs, chosen at home |

**Armed as the real machine:** unarmed, except with the gun kit above. **The tanker fit's real basis is the KC-130J,
not the C-27J**: two wing pods passing up to 300 gallons a minute each
**[S, [Military.com](https://www.military.com/equipment/kc-130j-hercules)]**; and the fire-fighting tank's fullest
description is the Hercules' MAFFS: 3,000 gallons released "in less than five seconds" over a strip a quarter of a
mile long **[P, [US Forest Service](https://www.fs.usda.gov/managing-land/fire/planes/maffs)]**. **Reuses:** Vehicle
Framework's runway check; the tiltrotor's rolling take-off; our water drop; the litter kit. **Feasibility [I]:**
the aircraft and its runway are nearly free; the parachute drop is moderate; **a vehicle in the hold was the open
question** the 9 November record also met, and the read the owner asked for found it feasible with limits
([the code reads](#a-vehicle-in-a-hold-decision-5-feasible-with-limits)). **Size:** about 14 cells long and 17 across at the Harrier's scale, the
largest body we would have drawn.

### The crewless wingman (MQ-28 Ghost Bat)

**Real basis.** "Designed and developed by Boeing in Australia", to "team with existing military aircraft"; over
2,000 nautical miles of range; a nose built for a "quick swap out of payloads"
**[P, [Boeing](https://www.boeing.com/defense/autonomous-and-unmanned-systems/mq-28-ghost-bat)]**, 2.5 m long with
over 1.5 cubic metres inside
**[S, [Airforce Technology](https://www.airforce-technology.com/projects/loyal-wingman-unmanned-aircraft/)]**. It is
11.7 m long **[S]** and is built in Melbourne **[S, [Wikipedia](https://en.wikipedia.org/wiki/Boeing_MQ-28_Ghost_Bat)]**.
In December 2025 one shot down a target drone with an AIM-120 missile while flying beside an E-7A Wedgetail
**[S, [The War Zone](https://www.twz.com/air/mq-28-ghost-bat-has-fired-an-aim-120-amraam-missile)]**. Its newer
version can be controlled "from a crewed aircraft, a ground station or a naval vessel"
**[P, [Boeing Australia, 2026](https://www.boeing.com.au/news/2026/boeing-unveils-advanced-mq-28-capabilities)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Flies beside a crewed aircraft | Send an escort that is shot at first and carries the warning receiver or the decoys | Sending it alone without a control station | A crewed flight into defended air risks a machine before a pilot |
| Swappable nose | Fit one of a sensor, a jammer or decoys | Having two | The escort is chosen for the threat |

**Armed as the real machine:** sensors first; missiles only in its newest version. **Feasibility [I]:** aircraft
travel one to a world object, so the cheap shape is an escort that is a vehicle on the ground and a state on the
leader's flight in the air. **It only matters where air defence exists**: today that is The Dead Man's Switch -
Expedition's, through the tie-in we built ([air-defence tie-ins](air-defence-tie-ins.md)). No framework has
air-to-air combat. It needs a runway.

### The armed scout drone and its control van (MQ-9; Bayraktar TB2)

**Real basis.** The MQ-9A stays up "over 27 hours" with 3,850 lb of load
**[P, [General Atomics](https://www.ga-asi.com/remotely-piloted-aircraft/mq-9a)]**; its crew is a "pilot, sensor
operator (operating from GCS)", and a system is three aircraft, a ground control station and line-of-sight and
satellite links **[S, [Air and Space Forces](https://www.airandspaceforces.com/weapons-platforms/mq-9/)]**. The
Bayraktar TB2 is smaller: 150 kg of load, over 18 hours, and a control station on a truck with pilot, payload and
image consoles and an antenna "raised up to 12m"
**[P, [Baykar](https://baykartech.com/en/uav/bayraktar-tb2/)]**; it is controlled by line of sight out to under 300
km **[S, [Wikipedia](https://en.wikipedia.org/wiki/Baykar_Bayraktar_TB2)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Watch | Keep an aircraft over a place for a long time with no one at risk | Doing it without two colonists at the consoles for as long as it flies | Watching costs people at home, not people in danger |
| Strike | Fire a few small weapons on order | Carrying much: a TB2 lifts 150 kg | Precise and light, never a bombardment |
| The control van | Take the consoles to the field | Flying beyond the van's reach | The van's place and its mast height decide where the drone can go |

**Armed as the real machine:** yes. **Feasibility [I]:** circling with nobody aboard points at the fixed-wing
framework again, and at a runway; a drone launched from a rail has no framework. **This is the plainest use of the
control-station level and of the radio ideas**, and the control van is a truck we could build first as a thing in
itself.

### The ground-attack jet (A-10C)

**Real basis.** Built round a seven-barrel 30 mm gun firing up to 4,200 rounds a minute
**[P, [General Dynamics](https://www.gdots.com/armaments/aircraft-guns-gun-systems/gau8a/)]**, with 1,174 rounds
**[S, [Air and Space Forces](https://www.airandspaceforces.com/weapons-platforms/a-10/)]**; 540 kg of titanium round
the cockpit, doubled hydraulic controls with a mechanical back-up, and take-offs from "relatively short and unpaved
runways" **[S, [Wikipedia](https://en.wikipedia.org/wiki/Fairchild_Republic_A-10_Thunderbolt_II)]**. Its retirement
was paused until at least 2030 in April 2026
**[S, [Air and Space Forces](https://www.airandspaceforces.com/air-force-delays-a-10-retirement-until-2030/)]**.

**Agent recommendation: do not build it.** A gun pass needs the fixed-wing framework's `Strafe` run, which nothing
uses yet, and that framework's own author appears to have an A-10 in work (the comment in the Harrier's source,
above). It is theirs to make; if it appears, it would get our systems as the Harrier did.

## Ground vehicles and fits

### The crewless fit (an upgrade family, not a vehicle)

**Real basis.** Four programmes, three of them Australian:

- **Leader and followers.** The US Army's Leader-Follower puts one crewed truck in front of crewless ones: as fielded,
  "one leader PLS truck to control four follower PLS trucks", with nine as the goal
  **[P, [US Army, 2021](https://www.army.mil/article/249257/the_future_is_now_at_jrtc_modernizing_the_force_with_tactical_wheeled_vehicle_leader_follower_technology)]**.
  The trucks are the M1075 PLS
  **[S, [Breaking Defense](https://www.breakingdefense.com/2018/08/army-wants-70-self-driving-supply-trucks-by-2020/)]**,
  the truck RimThunder's Roaring Tiger puts on the owner's list.
- **In Victoria.** On 6 June 2023 "a convoy of four autonomous Army trucks followed a crewed 'leader' vehicle on a
  public road in Victoria", between Mangalore Airfield and Puckapunyal, with Deakin University and the National
  Transport Research Organisation
  **[S, [Contact](https://www.contactairlandandsea.com/2023/06/09/armys-autonomous-truck-convoy-a-first), reproducing Defence's release]**.
- **Optionally crewed.** BAE Systems Australia delivered twenty M113AS4 carriers that can "be operated autonomously or
  as a standard crewed vehicle"
  **[S, [Asia Pacific Defence Reporter](https://asiapacificdefencereporter.com/bae-systems-delivers-20-m113-as4-optionally-crewed-combat-vehicles/)]**;
  the work began in 2019 **[P, [BAE Systems](https://www.baesystems.com/en-aus/insight/trusted-autonomy)]**.
- **The Pilbara.** "Approximately 90% of our haul truck fleet across our Pilbara sites is also autonomous", run "from a
  single location 1,500 kilometres from the Pilbara" **[P, [Rio Tinto](https://www.riotinto.com/en/operations/australia/pilbara)]**;
  the trucks follow set courses under "a supervisory system and a central controller, rather than a driver", and
  each ran about 700 hours more than a crewed truck
  **[P, [Rio Tinto, 2018, as filed with the SEC](https://www.sec.gov/Archives/edgar/data/0000863064/000156459018001173/rio-ex9930_584.htm)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Follow the leader | Run a convoy in which only the first vehicle has a crew | Sending a follower anywhere alone; more than four followers to a leader (the fielded number) | A five-truck convoy costs two colonists, not ten |
| Run a loop | Let the tipper go between the drill rig and the bunker by itself, along a marked route | Leaving the route; working where a person must load it | Hauling becomes something set up once |
| Lose the leader | Nothing; it is the weakness | Carrying on: followers stop where they are | A convoy is protected by protecting one vehicle, and an enemy convoy is stopped by hitting one |

**Armed:** no. (**19 Nov 2026:** the owner chose that a crewless service truck of ours may carry a remote weapon station, a kit of its
own in [militarised service vehicles](vehicle-militarised-service-vehicles.md); the fit itself stays unarmed.) **What it uses up:** components to fit, research, and electrical power while it is on, through the
[power budget](vehicle-accessories.md) already built. **Reuses:** Vehicle Framework's crewless movement and caravans;
our hitch's follow code as the start of following; the tipper, the loading chute and the bunker for a loop with no
hands in it. **Feasibility [I]:** small to moderate. **Decided (owner, 18 Nov 2026): the vehicle stays optionally
crewed**, as the M113s are, and **a leader has at most four followers**. Keeping the seat needs our own answer to the
two Vehicle Framework checks that read the seat as the Def wrote it. **Other factions:** a raid convoy with followers stops when its
leader is destroyed, by the same rule; a captured follower works for us.

### The crewless ground carrier (Milrem THeMIS)

**Real basis.** A tracked carrier the size of a small car: 1,815 kg, 750 kg of load, 20 km/h, 2.50 by 2.10 by 1.16 m,
up to 15 hours on its diesel generator or 1.5 hours on batteries alone, controlled by line of sight "up to 1,5 km",
and "acquired by 21 nations". Fits include cargo, casualty evacuation, engineering and bomb disposal, and remote
weapon stations **[P, [Milrem Robotics](https://milremrobotics.com/themis-family)]**. In a 2021 demonstration one
"followed a human operator walking along a path" and another followed it between waypoints
**[S, [eeNews Europe](https://www.eenewseurope.com/en/first-demonstration-of-european-standard-unmanned-ground-vehicle/)]**;
one "with stretchers for rapid casualty evacuation" went to Ukraine in 2022
**[S, [eeNews Europe](https://www.eenewseurope.com/en/unmanned-vehicle-to-evacuate-casualties-in-ukraine)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Carry | Bring ammunition and supplies behind a squad | Carrying people; going faster than a walk | A squad on foot stays supplied without a truck in the fight |
| Stretcher fit | Bring a casualty back from a fight | Picking the casualty up: someone still loads the stretcher | One trip into danger instead of a carry all the way out |
| Weapon fit | Put a gun where no colonist stands | Having the stretcher or the cargo deck as well | Risk a machine first |
| Quiet running | Move on batteries alone for a short while | Doing it for long: the real figure is 1.5 hours against 15 | Our battery drive, already built, on a new body |

**Armed as the real machine:** by fit, with a .50 gun on `AmmoSet_50BMG`. **Feasibility [I]:** moving on orders is
free; following a pawn is the substantial part; an armed one needs a comp that marks it as a threat, or enemies ignore
it. **Neighbour:** GTek's Ripsaw M5 is the armed crewless tank, though not for 1.7; ours would be the small carrier.
**Other factions:** an armed carrier in a raid is a turret that walks; if it is run from a control station, killing
its operator stops it.

### Mobile air defence (Skyranger; C-RAM)

**Real basis.** Rheinmetall's Skyranger 30 is a turret for any carrier: a 30 mm revolver cannon at 1,250 rounds a
minute with 300 rounds ready, shells whose fuses are set at the muzzle to burst in front of the target, an effective
range of 3,000 m, two to four short-range missiles, its own search radar and two consoles; its "special focus" is
small crewless aircraft. **Rheinmetall claims rockets and mortar shells only for the 35 mm version**, which engages
air targets "while the vehicle is stopped"
**[P, [Rheinmetall brochure](https://www.rheinmetall.com/Rheinmetall%20Group/brochure-download/Air-Defence/B327us0725-Oerlikon-Skyranger.pdf)]**.
The machine built for shells is the US Army's C-RAM: a 20 mm six-barrel gun at 3,000 or 4,500 rounds a minute
"intercepting rockets, artillery and mortar rounds in the air", with rounds that destroy themselves
**[P, [Raytheon data sheet](https://www.mobileradar.org/Documents/Ray_Phalanx.pdf), third-party host]**; and it
"sounds an alarm to alert personnel, regardless if the Phalanx will fire or not"
**[P, [US Army](https://www.army.mil/article-amp/108158/us_soldiers_increase_base_defense_measures_on_fob_shank)]**.
Australia's own is a missile launcher on the Hawkei, a truck built in Bendigo
**[S, [Defence Technology Review, hosted by Thales](https://www.thalesgroup.com/sites/default/files/2025-05/Hawkei%20muscles%20up%20for%20GBAD%20role%20%28DTR%20Sept%202024%29.pdf)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Shoot at aircraft and drones | Defend a camp or a convoy against raiding aircraft and drone pawns | Doing it on the move | Air defence goes where the vehicles go |
| Shoot down shells | Sit under a siege's mortars | Doing it cheaply: hundreds of rounds a burst | A siege becomes a contest of ammunition |
| Warn | Give colonists a moment to reach cover when shells are in the air, whether or not it fires | Nothing | Even with an empty magazine it earns its place |

**Armed as the real machine:** Combat Extended has `AmmoSet_30x173mmNATO`, `AmmoSet_35x228mm` and
`AmmoSet_20x102mmNATO` in the owner's game. **Feasibility [I]:** in three parts. Aircraft that fly on the map and
drone pawns are ordinary targets, so shooting at them may need only a turret with the right figures (not checked).
Shells and drop pods are not targets a turret can take, so shooting them down is code of ours. The warning is small.
**It is the carrier our [air-defence record](air-defence-tie-ins.md) row AD12 waits for.** The gun itself needs no
gunner, which our weapon-station power rule already covers. **Other factions:** this one cuts both ways from its
first Def: an enemy base with one makes our own landings dangerous, and it must work the same for them.

### The rocket truck with swap-in pods (HIMARS)

**Real basis.** A crew of three
**[P, [DVIDS](https://www.dvidshub.net/news/237035/139th-airlift-wing-and-2nd-battalion-130th-field-artillery-lift-confidence-during-joint-training)]**
and one pod: "a single six-pack of rockets or one ATACMS missile"
**[P, [US Army](https://www.army.mil/article/70277/the_u_s_army_marked_the_delivery_of_the_400th_high_mobility_artillery_rocket_system_himars)]**,
or two of the newer missile. The rockets reach "70-plus km" and the missile about 300 km
**[P, [Congressional Research Service](https://www.congress.gov/crs_external_products/R/PDF/R46721/R46721.2.pdf)]**;
the newer missile exceeds 400 km
**[P, [US Army, 2024](https://www.army.mil/article/278296/then_now_atacms_to_prsm_out_with_the_old_in_with_the_new)]**.
A loaded pod weighs about 5,000 lb **[P, [US Army](https://www.army.mil/article/6260/army_fields_himars_to_fourth_battalion)]**
and is swapped with the truck's own boom in "roughly 5 to 10 minutes"
**[S, [The Defense Post](https://thedefensepost.com/2025/07/15/m142-himars-guide/)]**; no primary source gives the
time. Australia is buying 42 **[S, [AIDN, reproducing the ministerial release](https://aidn.org.au/government-delivers-first-australian-himars/)]**.
**Its crewless relatives:** the US Army's Autonomous Multi-domain Launcher fired six practice rockets in April 2024 on
commands "from a remote gunner position"
**[P, [US Army](https://www.army.mil/article/275666/autonomous_multi_domain_launcher_meets_another_program_milestone)]**;
and the US Marine Corps' ROGUE Fires is a crewless carrier on the L-ATV's own chassis, run in "teleoperator or
leader-follower modes", which hit a ship with a missile in 2021
**[P, [Oshkosh Defense](https://oshkoshdefense.com/oshkosh-defense-demonstrates-rogue-fires-as-part-of-u-s-navys-large-scale-exercise/)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Whole pods | Reload six rockets in one lift from a supply truck | Topping up single rockets: a pod is fired or swapped whole | Ammunition is planned by the pod, and a supply truck matters |
| Set up to fire | Fire a pod in moments | Firing on the move | Stop, fire, leave |
| Reach beyond the map | Strike another tile | Doing it under the rules still open in [striking another map](cross-map-strikes.md) | Artillery becomes a world-map matter |

**Armed as the real machine:** Combat Extended has no 227 mm rocket in the owner's game, so the ammunition would be
ours. **Feasibility [I]:** on one map it is a sibling of the Burya, which the owner already has, and adds little.
Its worth is the reach, which needs world targeting we do not have and must be designed for enemy launches from the
start. **Low priority until the cross-map rules are settled.** **Other factions:** the same weapon pointed at us
from another tile, which IRBM already does with its own missiles.

### The line-charge trailer (M58 MICLIC)

**Real basis.** "A rocket-propelled explosive line charge fired from a MK155 launcher mounted on a M200A1 trailer":
350 feet of charge pulled over a minefield by a rocket, "detonated by command wire", clearing "a vehicle wide lane
100 meters long"
**[P, [DVIDS, quoting the Army's fact sheet](https://www.dvidshub.net/news/545059/combat-engineers-conduct-2000-pound-mine-clearing-line-charge-op-fort-mccoy)]**.
It holds "1,750 pounds of C4"
**[P, [US Army](https://www.army.mil/article/116401/here_comes_the_boom_marines_demonstrate_mine_clearing_line_charge_during_live_fire)]**
and the lane is 8 m wide **[S, [FAS](https://man.fas.org/dod-101/sys/land/m58-miclic.htm)]**. It is fired from the
towing vehicle on a 60-foot cable, and the charges are known for "high misfire rates"
**[P authors, [Engineer Bulletin, 1996](https://man.fas.org/dod-101/sys/land/docs/960800-brchmir2.htm)]**. It is
"effective against conventionally fused mines" only **[S, FAS]**, and a charge that fails to go off has to be dealt
with by hand **[S, [We Are The Mighty](https://www.wearethemighty.com/mighty-culture/miclic-mine-clearance-military-engineers/)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| One lane | Blow a straight lane through traps, mines and light obstacles in one shot | Clearing everything: some kinds survive, as in reality | A trapped approach is breached, not walked |
| One shot | Nothing; it is the cost | Firing again without a fresh charge from a supply vehicle | Each breach is planned |
| Misfire | Nothing; it is the risk | Relying on it: a charge that lies there unexploded must be set off by a person who walks out to it | The best breach still sometimes needs a brave engineer |

**Armed as the real machine:** it is the weapon. **Reuses:** the [trailer base](vehicle-trailers.md) every Pintle
shares, set up by hand, behind any hitched vehicle. **Feasibility [I]:** small: a trailer and one order that lays a
line of explosions through the game's own explosion system. Every figure (lane size in cells, what survives, the
misfire chance, what the charge costs) would be authored and kept modest; 794 kg of explosive is a real figure,
not a recipe. **Other factions:** raiders with one would breach a trapped corridor the same way. That is fair and
costs nothing to run, but it changes how defences are built. **Decided (owner, 18 Nov 2026): yes, behind a mod
setting that starts off.**

### The protected truck (Thales Bushmaster)

**Real basis.** Built at "Finn Street, Bendigo, Victoria": a crew of one and nine passengers, 11,400 kg empty, a
"monocoque vee-shaped armoured hull", a 250-litre water tank as an option, and bodies for "troop movement, command and
control, electronic warfare, air defence, assault pioneer, direct fire support, route clearance and ambulance"
**[P, [Thales Australia brochure, 2012](https://warwheels.net/images/BushmasterTroop2012ThalesBrochure.pdf), third-party host]**.
The hull "redirects blasts out from under the vehicle"
**[P, [New Zealand Ministry of Defence](https://www.defence.govt.nz/news/new-bushmaster-armoured-vehicles-hit-the-production-line/)]**.
A cooled drinking-water system "was once planned ... but was omitted upon production due to cost constraints" and
later reconsidered **[S, [Wikipedia](https://en.wikipedia.org/wiki/Bushmaster_Protected_Mobility_Vehicle)]**. An
electric version was shown in 2022, with "reduced noise and a reduced heat signature"
**[S, [Defence Connect](https://www.defenceconnect.com.au/land/10488-army-unveils-electric-bushmaster)]**, and a
crewless cargo carrier based on it in October 2026
**[S, [Defence Connect](https://www.defenceconnect.com.au/land/18956-thales-australia-3me-technology-unveils-bladehaul-uncrewed-ground-vehicle-at-land-forces-2026)]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| The hull | Drive over a mine and lose a wheel, not the crew | Nothing | A mined road costs a repair |
| Water aboard | Carry the crew's drinking water in the vehicle | Nothing; the tank runs down and is refilled by hand | Long patrols with Dubs Bad Hygiene's thirst |
| Bodies, one at a time | Troop carrier, ambulance or command post on one hull | Having two | One hull, chosen for the job |

**Agent verdict: it waits on the armour question**, because a protected truck without working protection is only a
truck, and GTek's MaxxPro already fills the role on the owner's list. What it would add sits on systems we have: crew
water (a designed accessory), the ambulance body on D'thorga Medical, and battery drive for the electric version.

## Catalogue lines

| Machine | What it is | Why it is only a line | Source |
| --- | --- | --- | --- |
| AC-130J in full | The four-engine gunship | Too large to draw or land; its gun kit lives on the transport instead | above |
| Bell V-280 Valor (MV-75) | The Osprey's smaller successor: 14 troops, a wing that does not fold | A sibling for later; the basis of the crewless fit | [P] [Bell](https://www.bellflight.com/products/bell-v-280); [P] [US Army](https://www.army.mil/article/285828/army_designates_mv_75_as_mission_design_series_for_future_long_range_assault_aircraft) |
| Sikorsky U-Hawk | A Black Hawk with the cockpit removed for nose doors and a ramp; 25% more room; a crewless ground vehicle can drive aboard | A vehicle inside a vehicle, which is still open | [P] [Lockheed Martin, 2025](https://news.lockheedmartin.com/2025-10-13-Sikorsky-Converts-BLACK-HAWK-into-U-Hawk-A-Battle-Ready-Autonomous-UAS) |
| Black Hawk flown with nobody aboard | 30 minutes over Fort Campbell, 5 Feb 2022 | The real basis for a crewless fit on any helicopter, the Louie included | [P] [DARPA](https://www.darpa.mil/news/2022/alias-flight) |
| CH-53K King Stallion | Lifted 36,000 lb on one hook | The Cherokee already lifts heavy | [P] [Lockheed Martin, 2018](https://news.lockheedmartin.com/2018-03-07-Sikorsky-CH-53K-Completes-Critical-Flight-Envelope-Expansion-with-36-000-pound-External-Lift) |
| MQ-25 Stingray | A crewless tanker for aircraft carriers | Tanking is covered by the cabin and hold fits | [P] [Boeing](https://www.boeing.com/defense/autonomous-and-unmanned-systems/mq-25-stingray) |
| OA-1K Skyraider II | A crop-duster turned armed overwatch aircraft, for rough strips | The same on-map flight need as the gunship, with less to show for it | [P] [AFSOC release, hosted by DSIAC](https://dsiac.dtic.mil/articles/air-force-special-operations-command-accepts-the-first-missionized-oa-1k-skyraider-ii) |
| ScanEagle | A small scout thrown from a catapult and caught on a rope; 18 hours aloft; flown from Australian warships | A launcher-and-rope trailer is a charming Pintle; it needs a flight model no framework has | [P] [Insitu](https://www.insitu.com/products/scaneagle); [P] [Insitu Pacific](https://insitupacific.com.au/defence/) |
| E-7A Wedgetail | The RAAF's radar aircraft | Radar as a detection game was argued against in the avionics record; its use here is as an airborne control station | [P] [Boeing](https://www.boeing.com/defense/patrol-early-warning-and-battle-management/e-7-aewc) |
| XQ-58A Valkyrie | A crewless jet launched by rocket, no runway | The wingman without a runway; a launch no framework has | [P] [Kratos](https://www.kratosdefense.com/wp-content/uploads/2026/07/XQ-58-Valkyrie.pdf) |
| Boxer | One drive module, mission modules swapped "within < 30 minutes" | Our tanker already fits its tank as an upgrade; the idea is in use | [P] [Rheinmetall](https://www.rheinmetall.com/en/products/wheeled-armoured-vehicles/boxer) |
| Harvest HAWK | A missile kit for the KC-130J tanker | Retired; the gun kit above is the living version | [S] [The War Zone](https://www.twz.com/air/kc-130js-hellfire-missile-firing-harvest-hawk-kits-retired-by-marines) |

## Working without a crew: three levels of one system

The owner chose all three ways. They are written here as levels a vehicle can have, each a separate upgrade or
building, so that a vehicle with only the first is useful and nothing depends on the radio mod.

| Level | What controls it | What it can do | What it cannot do | What it uses up | Real basis |
| --- | --- | --- | --- | --- | --- |
| **Route** | A fitted kit, and nobody | Follow a crewed leader; run a marked loop; fly a round between the player's own places | Go anywhere new, fight wisely, load, hook on or repair itself, carry on without its leader | Components, research, electrical power while on | Leader-follower trucks; the K-MAX; the Pilbara's trucks |
| **Station** | A colonist at a console, in a building or a control van | Go where it is sent; aim its weapons | Work beyond the station's reach; work while the operator sleeps or is hurt | The operator's time; power at the station | The MQ-9's pilot and sensor operator; the TB2's truck with its 12 m mast; the US Army launcher's "remote gunner position" |
| **Mechanitor** | A mechanitor's bandwidth | As the station, with no console | Exceed the bandwidth the player's mechanoids also need | Bandwidth | None: this one is the game's own idea, used as the game uses it |

**Which vehicles take which [A]:**

| Vehicle | Route | Station | Mechanitor |
| --- | --- | --- | --- |
| Our trucks and trailers' tractors | follow, loop | | |
| Cargo helicopter, tiltrotor | rounds between our own places | redirect in flight; the tiltrotor's belly gun | yes |
| Crewless ground carrier | follow a squad | drive it, aim its gun | yes |
| Scout drone, wingman | | fly it, strike | yes |
| Rocket truck | follow a leader | fire | |

**What is built on what.** The route level is Vehicle Framework's own crewless movement plus rules of ours (who
leads, what the route is, what happens when the leader is lost). The station level adds a building and a vehicle
(the control van) and a check of reach. **The mechanitor level needed a code read**, because our vehicles are not
mechanoids; the owner asked for it (decision 10) and its findings are under
[the code reads](#code-reads-the-decisions-asked-for-18-nov-2026). The nearest thing we have is `PhobosTopUpDef`,
through which mechanitors already refill store-carrying mechanoids ([top-up design](top-up-design.md)).

**The weakness is the point.** Every level has one thing an enemy can hit to stop it: the leader, the operator, the
mechanitor. That is true of the real machines, it gives the player something to protect, and it gives them a way to
stop an enemy's crewless vehicles without destroying each one.

**Agent recommendation for order:** route first, on our own trucks; station second, with the control van; mechanitor
after the code read.

## Radio tie-in ideas

**Ideas only.** The radio framework is on the back burner (owner, 9 Nov 2026) and nothing here is designed to build.
The owner asked on 18 November 2026 for tie-in ideas between it and crewless vehicles, and these are recorded so
they are there when the radio is picked up again. Without the radio mod, a control station would use one plain
authored reach; with it, the reach would come from the link.

**What real control links are like:**

- **Two links, not one.** "The command and control (C2) link is the data link between the remotely piloted aircraft
  and the remote pilot station for the purpose of managing flight", kept separate from the payload's data
  **[P, [ICAO, 2017](https://www.icao.int/sites/default/files/sp-files/safety/UA/Documents/ICAO%20RPAS%20Concept%20of%20Operations.pdf)]**.
- **Orders are small and pictures are large.** The command link "may operate at data rates ranging from 2 kbps to 250
  kbps" **[P, [NASA Glenn](https://ntrs.nasa.gov/api/citations/20170005641/downloads/20170005641.pdf)]**. Global
  Hawk's link carried "a 274 Mbs downlink and a 200 Kbs uplink"; the Predator sent video at 1.544 Mb/s by one
  satellite link and had another of 16 kb/s
  **[P, [USAF Air Combat Command, 1996, hosted by FAS](https://irp.fas.org/doddir/usaf/conops_uav/part06.htm)]**. The
  figures are old; the proportion is the point.
- **Line of sight ends.** The Predator "operates within approximately 100 nautical mile LOS of the GCS" **[P, the same]**;
  the TB2 under 300 km **[S]**; the THeMIS 1.5 km on the ground **[P, Milrem]**. Beyond that it is a satellite, or
  "air-to-air relay of imagery using a UAV as a collector and another as the relay vehicle", which "will extend the
  range for LOS data collection by 400-500 NM" **[P, the same]**.
- **Losing the link is planned for.** "Lost C2 Link" is a defined state, and "any pre-programmed emergency or
  contingency procedure should ensure a safe response" **[P, ICAO]**. The aircraft may "return to base", "continue to
  destination" or "return to mission altitude", after a set delay of one to eight minutes in the cases NASA studied
  **[P, [NASA Ames](https://ntrs.nasa.gov/api/citations/20190001648/downloads/20190001648.pdf)]**.
- **The controller can be in the air.** The newer Ghost Bat is controlled "from a crewed aircraft, a ground station or
  a naval vessel" **[P, Boeing Australia]**.

**The ideas:**

1. **The control link is a real link.** A station drives a vehicle only while the link holds in both directions, and
   the worse direction decides, as the framework's one link-budget service already rules. Hills and walls block it,
   and **the height of the antenna on the control van matters**, as the TB2's twelve-metre mast says it should.
2. **Orders get through where pictures do not.** By bandwidth alone, a picture link at 274 Mb/s needs about 31 dB more
   than an order link at 200 kb/s, and the Predator's two satellite links differ by about 20 dB **[I: ten times the
   logarithm of the ratio of the rates; our arithmetic on the 1996 figures]**. So one link has two thresholds: above
   the lower, the vehicle takes orders (go there, come home); above the higher, the operator can also aim its
   weapons by hand. This is the weak-signal idea the owner knows from FT8, as a mechanic, and it gives a narrow, slow
   mode a use nothing else has. In Plain display mode it reads "orders only" or "orders and picture", never a figure.
3. **Lost-link behaviour, chosen per vehicle:** come home, hold, or carry on with the route. Each is a real procedure,
   and each has a real cost when chosen wrongly.
4. **An aircraft as a relay.** A repeater carried by the tiltrotor, the cargo helicopter or a drone reaches over the
   ridge the control van cannot see past. Height gives line of sight, which is a figure that changes an outcome.
5. **An aircraft as the station.** The transport or the tiltrotor carries the consoles, as the Wedgetail does for the
   Ghost Bat, so the reach moves with the flight.
6. **Redirecting a flight needs a link.** A route flown without one cannot be changed in the air. Vehicle Framework
   already has the switch (`controlInFlight`), so this is a rule on an existing setting.
7. **Jamming cuts both ways.** A jammer in reach drops a crewless vehicle to its lost-link behaviour, ours or theirs.
8. **Leader and followers share a short link.** Stretch a convoy too far over rough ground and a follower stops.

**An honest limit.** The radio framework's open phase is VHF and UHF on one map. On one map most links would close
easily, so ideas 1, 2 and 8 would bite only behind hills, walls and jammers. The larger stakes (reach between maps,
relays, satellites) belong to phases that are not open.

## Recommended order

Agent recommendation; the owner picks.

| # | What | Why here |
| --- | --- | --- |
| 1 | **The tiltrotor** | The owner's wish; most of it stands on built systems. It waits for the armour model and for our own flight over the map |
| 2 | **The crewless fit, route level**, on our own trucks | The smallest piece, and the base every other crewless thing needs |
| 3 | **The cargo helicopter** | Small art, built systems, and the best show of the crewless fit in the air |
| 4 | **The line-charge trailer** | Small, and unlike anything in the owner's game |
| 5 | **The crewless ground carrier** | Needs following, which the fit will have taught us |
| 6 | **The control van and the station level** | Opens the armed crewless vehicles and the radio ideas |
| 7 | **The transport aeroplane**, then its gun kit | The biggest picture we would draw; the gun kit waits for on-map flight to settle |
| 8 | **Mobile air defence** | Substantial code; wanted more once aircraft are armed on both sides |
| 9 | The scout drone, the wingman, the rocket truck, the protected truck | Each waits on something named in its entry |
| - | The ground-attack jet | Not recommended: another author appears to have it in work |

**Since the owner's decisions**, three things come before the first Def: **the design of our own flight over the map**
(hover and circling together, hover built first), which the Osprey, the cargo helicopter and every later aircraft
stand on; **the armour model** (Combat Extended first); and the two code reads the owner asked for, on a vehicle in a
hold and on mechanitor control. Each is its own design record when the owner gives the word.

## The owner's decisions on the open questions (18 Nov 2026)

The record first closed with thirteen open questions. The owner answered all of them the same day, asked in four
rounds. Quoted words are the owner's; the rest are the options they picked.

| # | Question | The owner's decision |
| --- | --- | --- |
| 1 | How armour works on armed vehicles | "Combat Extended is the priority, with Vehicle Framework a fallback for other users who go without CE for when we finally publish our mods, which is something I still feel like doing eventually." |
| 2 | Whether to ask the two flight frameworks' authors | Neither asked nor built on: "We simply learn where we can from said, named, mods because I wish to go our own, more improved, route." |
| 3 | The Osprey's shape | Follows from 2: one Osprey for every player, flying over the map on our own code |
| 4 | Which mod holds these | D'thorga Vehicles, as a part, with Combat Extended's ammunition in a folder that loads only with it |
| 5 | A vehicle carried in a hold | "Yes, read it, and only implement if it becomes a feasible reality. Otherwise, we fallback to a sling and you clearly let me know this is the direction where we are heading / give me a choice." |
| 6 | The Osprey's tanker kit | Kept, although the real programme stopped |
| 7 | Real figures or game figures | Real proportions among our own aircraft, anchored to a helicopter on the owner's list; other authors' aircraft are left as their authors set them |
| 8 | The crewless fit and the driver's seat | The seat stays: optionally crewed |
| 9 | Followers per leader | Four |
| 10 | The mechanitor way | Read the code first; say plainly if it is not feasible |
| 11 | Strikes on a map that is not loaded | Loaded maps only: a strike elsewhere is refused, with its reason |
| 12 | Names | Real names |
| 13 | The line charge in enemy hands | Yes, behind a mod setting |

A fourteenth question arose from decision 2, and the owner answered it too: **our own flight over the map is designed
for both hovering and circling from the start, and the hover half is built first.**

**What each decision sets in motion:**

- **1, armour.** A read of how Combat Extended resolves a hit on a Vehicle Framework vehicle, then one armour
  declaration per Def that serves both. **Designed 18 Nov 2026:** [armour for our armed vehicles](archive/vehicle-armour.md);
  not built. The Osprey's own design record followed the same day: [the V-22 Osprey](vehicle-osprey.md).
- **2, 3 and the fourteenth.** A design record for our own flight over the map, in D'thorga Framework's core because
  every aircraft of ours will use it. It comes before the Osprey's own design record. **Written 18 Nov 2026:**
  [our own flight over the map](archive/vehicle-flight-over-the-map.md); nothing in it is in code.
- **5.** A read of how one vehicle can be held inside another; the owner hears the verdict plainly either way.
- **7.** The anchor helicopter is the Hind (owner, 18 Nov 2026, [later choices](#the-owners-later-choices-18-nov-2026));
  the agent had first suggested the Louie, which would have put the Osprey above everything on the list.
- **10.** A read of how the game counts bandwidth.
- **11.** Answers, for these two weapons, the question [striking another map](cross-map-strikes.md) left open.
- **12.** Applies to machines based on one real machine. Our utility vehicles keep their house names, and the
  mods keep theirs (agent's reading of the decision).
- **13.** The setting starts off (owner, 18 Nov 2026).

## Code reads the decisions asked for (18 Nov 2026)

Three reads, done the same day by agents with our query tools. **All three came back feasible.** Each is read from
code and nothing was run; "not yet tested in game" applies to every line. Sources: Vehicle Framework's public source
(commit `026420b5`) and its 1.7 decompile of 18 Nov 2026; Combat Extended's public source of 15 Nov 2026, checked
against the installed build; RimWorld 1.7's decompile; our own `src/`. RimThunder - Core was read from a decompile,
as it has no public repository on record and states no licence, and is described in our own words.

### Armour (decision 1): one Def and a Combat Extended patch

**Verdict: the owner's choice works as chosen, and under Combat Extended armour has a real unit.**

| | With Combat Extended | With Vehicle Framework alone |
| --- | --- | --- |
| Who decides a hit | Combat Extended's own prefix on `VehicleStatHandler.ApplyDamage` replaces Vehicle Framework's step whenever its setting "Use New Vehicle Armor" is on, the default (`Source/VehiclesCompat/VehiclesCompat/VehicleArmorPatch.cs:17-34`) | `VehicleComponent.ReduceDamageFromArmor` (`Components/Vehicles/Health/VehicleComponent.cs:192-223`) |
| What the figures mean | **Millimetres of rolled steel against sharp hits, megapascals against blunt** (Combat Extended's own stat description) | A rating on the game's scale, where 1.0 is 100% |
| How armour acts | The round's penetration is subtracted, part by part along the shot's line; several outer parts on the same cells act as layers | Dice: the armour less the penetration is the chance to deflect or to halve |
| What else counts | A part's health adds about one fiftieth of itself as millimetres; a damaged part protects less | Nothing |
| Which part is hit first | The **smaller** `hitWeight` | The **larger** `hitWeight` |

- **No single number serves both scales.** 30 means 30 mm under Combat Extended and "deflects everything" without it;
  0.8 means 80% without it and 0.8 mm with it. So a Def of ours carries game-scale figures, and a patch that loads only
  with Combat Extended replaces each part's armour, the vehicle-wide figures, the hit weights and every upgrade's
  armour. Every author checked does this (RimThunder, GTek, the Hind, the Harrier; Combat Extended itself for the
  Cherokee), and `mods/PhobosVehicles` already has the folder.
- **Real figures, honestly.** Sharp armour can be sourced, but as an equivalent, not a plate thickness: source the
  vehicle's published protection level, then set the millimetres just above the Combat Extended round of that threat
  (its 7.62x51 ball penetrates 7 mm, its .50 armour-piercing 28 mm, its 30 mm armour-piercing 57 mm). The label is
  "sourced threat level, authored millimetres". Blunt figures have no real-world source and are authored.
- **Other authors' figures are ratings, not measurements [I]:** GTek gives the L-ATV and the much larger MaxxPro the
  same numbers.
- **An add-on armour kit is an upgrade node**: armour and health added per plate, weight added through the mass
  stat, with the Combat Extended patch replacing the kit's figures too.
- **Combat Extended treats every vehicle as one cell wide** and reads `fillPercent` as its height, which is why the
  Hind's and Harrier's authors add collision code of their own. Flight code of ours meets the same need.

### A vehicle in a hold (decision 5): feasible, with limits

**Verdict: feasible, about the size of one hitch slice. The owner does not need the fall-back to the sling.**

- **A hold is the sling's holder with a different way in.** The sling keeps its load despawned in a container on its
  own comp, found through our postfix on `Pawn.GetChildHolders` and saved deep
  (`src/PhobosFramework.Vehicles/Sling/CompPhobosSling.cs:38,53,531-544`). That holder carried a vehicle through
  take-off, the world map and landing in the owner's test of 18 Nov 2026 (test row 49), and the hitch uses the same
  kind for a towed vehicle on a caravan journey (`Hitch/TowTravel.cs:22-31`; not yet tested in game).
- **A hold is simpler than the sling where the sling went wrong.** It is loaded on the ground before departure, so
  nothing happens at the moment of launch; and it unloads only on an order, so nothing is spawned at landing. That
  second point is where the landing fault of 12 November came from.
- **RimThunder already carries vehicles in the owner's game** (Roaring Tiger's M1075 and M113 take one on deck). It
  puts the carried vehicle in the carrier's ordinary inventory, and that choice is why its companion library needs
  more than twenty patches on caravans, trade and weight rules. **We keep a holder of our own and do not use the
  inventory.**

The limits the read proposes (agent defaults):

1. The carried vehicle travels with nobody aboard; its crew ride in the carrier's seats.
2. "Drive on" ends at a ramp cell, where the vehicle is taken aboard. One vehicle cannot enter another's cells.
3. Only the player's vehicles are loaded, and the game's own factions never load one.
4. No nesting: nothing that is itself slinging, towing, holding or being towed; no aircraft as a load for now.
5. A size limit in cells as well as a weight limit.

**Before it is built:** a host aircraft with sourced hold sizes (the C-27J is still only a candidate), and owner
passes on test rows 57 and 70, which prove setting down by choice and holding through a caravan.

### Mechanitor control (decision 10): feasible, with limits

**Verdict: feasible and small, by counting a vehicle's cost against a mechanitor's bandwidth without making the
vehicle a mechanoid.**

- **How the game counts.** Used bandwidth is the sum of a `BandwidthCost` stat over the pawns a mechanitor oversees
  (`Pawn_MechanitorTracker.UsedBandwidthFromSubjects`); neither that nor the total is cached. No active mod patches
  the used figure.
- **A vehicle cannot become a mechanoid.** Vehicle Framework's vehicles have their own flesh type, and every part of
  the game's mechanoid handling (the Mechs tab, the right-click Control option, the "no overseer" alert, going
  feral) tests for a mechanoid. None of it will ever act on a vehicle, so the gizmo, the inspect line and the rule
  for stopping are ours.
- **The recommended shape:** one comp on the vehicle that remembers its mechanitor, and one small postfix that adds
  assigned vehicles' cost to the used figure. Nothing in the game's own saved data changes.

| Way | What the player sees | Who loses control first when bandwidth runs short |
| --- | --- | --- |
| **Add to the used figure** (the owner's choice, 18 Nov 2026) | The mechanitor's bandwidth fills up with the vehicle's cost | Vehicles, newest first; mechanoids keep priority |
| Lower the total, as the game's own band node raises it | The total shrinks; the Health tab names the link | Mechanoids, newest first; vehicles keep priority |
| Make the vehicle a truly overseen pawn | The vehicle sits in a control group whose work modes mean nothing to it | The game's own rule |
| A small control mechanoid fitted in the vehicle | The full game behaviour, on a new mechanoid | The game's own rule |

The last two work but are larger and more fragile, and are not recommended.

**The limits, plainly:**

- **Direct orders reach only as far as the mechanitor's own command range**: about 25 cells, on the mechanitor's map
  (`Pawn_MechanitorTracker.CanCommandTo`), as for mechanoids. Other mods' range changes to that method are inherited;
  the relay patches some mods put on a different method are not.
- **Nothing reaches another map.** A vehicle elsewhere keeps its assignment and takes no orders.
- **A downed mechanitor commands nothing; a dead one, or one who loses the mechlink, frees the vehicle, which
  stops.** A vehicle never goes feral.
- **Vehicle Framework's order for several selected vehicles skips the hook a range check would use**
  (`VehicleComp.CanMove`), so that case needs its own answer.

So the mechanitor way is the control station with no console and the game's own short reach. It suits a vehicle that
works near its mechanitor, and the route level still does the travelling.

### Faults the reads found in code we already ship

Read in code, none seen in game. **The owner chose to fix all six (18 Nov 2026), and they went into D'thorga Framework
0.140.1**, not yet tested in game. They are rows CR1 to CR6 in
[outstanding work](outstanding-work.md#done-modern-military-vehicles-18-nov-2026).

| Row | The fault as read | What was done |
| --- | --- | --- |
| CR1 | The sling checked room, and drew the outline, with the game's footprint in place of Vehicle Framework's. The same mistake in the hitch destroyed a vehicle set down at a map edge | Fixed: both use Vehicle Framework's footprint, with a test that refuses the game's one where vehicles are set down |
| CR2 | The sling's set-down caught a thrown error but not a spawn that fails silently | Fixed: the load goes back on the hook and the player is told, as the hitch does |
| CR3 | Our Health tab showed part armour as a percentage, so 30 mm under Combat Extended read "3,000%" | Fixed: it uses the armour stat's own format, which Combat Extended sets to "mm RHA" and "MPa" |
| CR4 | Nothing of ours reads Combat Extended's "Use New Vehicle Armor" setting | **Not a fault.** The armour figures are on Combat Extended's scale whenever it is active, whatever that setting says, so choosing our blast scale by "Combat Extended is active" is right. No change |
| CR5 | Cook-off always passed Combat Extended's scale | Harmless: it runs only with Combat Extended's ammunition system on. It now asks, like our other blasts |
| CR6 | The order of our submersible prefix and Combat Extended's on the same method was unverified | Fixed: ours runs before every other prefix there, with a test. Which of the two ran first before is still not known, so "a dived boat took full damage" was possible, not proven |

**The fixing found a seventh, not fixed (row CR7):** about forty other places in the vehicle code read a vehicle's
cells the game's way (`OccupiedRect()`), which has the same one-cell error for a vehicle of even length facing south or
west: the pipes and conduits laid beneath it, set-up, the tipper, the generator set's leads. Each needs reading before
any is changed.

## The owner's later choices (18 Nov 2026)

Asked after the code reads, the same day:

| Choice | The owner's decision |
| --- | --- |
| How a mechanitor's bandwidth is charged for a vehicle | Added to the used figure: vehicles stop first when it runs short, and mechanoids keep priority |
| Whether to fix the six faults | Fix all six now (Framework 0.140.1, above) |
| The anchor for real proportions | **The Hind**: 10 in game. At about 270 km/h in reality, the Osprey's sourced 463 to 493 km/h comes out near 17 to 18, above every helicopter and just above the Harrier's 16 **[I; the Hind's real speed is from memory and still to be sourced]** |
| Whether the line charge's setting starts on or off | Off |

## Still open

1. **Row CR7**: the forty other uses of the game's footprint on a vehicle. The owner has not been asked.
2. ~~The Hind's real cruising speed~~ **Sourced 18 Nov 2026:** "Cruise speed: 270 km/h" **[S, Wikipedia's Mi-24
   specifications, citing the OE Data Integration Network]**, the figure the anchor was worked from
   ([our own flight over the map](archive/vehicle-flight-over-the-map.md#sources)).
3. **Vehicle Framework's own armour figures** for players without Combat Extended: authored ratings, set when the
   first armoured Def is written.
4. **Everything in the radio section**, until the radio framework is picked up again.

## Who chose what

- **Owner (18 Nov 2026):** research and record only; modern machines, aircraft first; the Osprey for certain; the
  Osprey as the longest entry here, with no design record yet; armed as the real machine; all three ways of working
  without a crew; radio tie-in ideas asked for; and the fourteen decisions in
  [the table above](#the-owners-decisions-on-the-open-questions-18-nov-2026), our own flight over the map among them.
- **Agent recommendations:** every game idea in every table; the order; building the gunship as the transport's gun
  kit; not building the ground-attack jet; route level first. The agent had recommended asking the flight
  frameworks' authors and house names; the owner chose otherwise on both.
- **Agent defaults, owner to revise:** helicopter flight on the map and aeroplane flight between maps; no vehicle
  inside the Osprey; what it lands on as three conditions; the table of which vehicle takes which crewless level;
  the five limits proposed for a hold.
- **Owner, later the same day:** bandwidth added to the used figure; all six faults fixed now; the Hind as the anchor
  for real proportions; the line charge's setting starting off.
- **Authored, to be set if anything is built:** every game figure. No figure in this record is a Def value.

## Sources and what could not be confirmed

Sources are linked beside the claims they support. Organisations whose work this record leans on: Bell and Boeing
(the V-22 guide and product pages); Kaman, Lockheed Martin and the FAA (the K-MAX); Leonardo (the C-27J); Boeing
Australia (the Ghost Bat); General Atomics and Baykar (the drones); Milrem Robotics (the THeMIS); Rheinmetall and
Raytheon (air defence); the US Army, the Congressional Research Service and Oshkosh Defense (HIMARS and its crewless
relatives); Thales Australia (the Bushmaster); Rio Tinto (the Pilbara); ICAO and NASA's Glenn and Ames centres
(control links). Wikipedia's editors, Air and Space Forces Magazine, The War Zone, Defence Connect and the other
secondary sources are named where used. None of these endorses anything here.

**Not confirmed, and marked so above or left out:**

- A primary statement that the V-22 cannot hover on one engine; a current primary figure for its fold time; its ramp
  gun's arc; primary figures for the tanker kit and for the CMV-22B.
- That a Humvee does not fit a V-22: only the dimensions were found, and the comparison is ours.
- The year of the V-280's autonomous flight (2019 is read from the page dates).
- The K-MAX's empty weight from a primary source, and a final count of its Afghanistan flights.
- The AC-130J's crew (seven or nine) and endurance; the MC-27J's status after January 2022.
- The C-27J's take-off run: Leonardo's 690 m, or the shorter figures elsewhere.
- That the Skyranger 30 defeats shells: its maker claims that for the 35 mm version only.
- HIMARS's reload and firing times from a primary source, and one agreed weight.
- That the Bushmaster's water tank is cooled as built, and its size (250 or 270 litres).
- The Australian convoy's truck type beyond "Land 121 40M cargo trucks", and anything about the optionally crewed
  M113s after 2021.
- Current figures for video link rates: the primary ones are from 1996.

**Tools used:** `query-defs.py` (vehicles, sizes, ammunition sets), `workshop-research.py` (collections and mod
reports), `query-code.py` over Vehicle Framework's public source and our decompiles of VTOL Vehicle and the
fixed-wing framework. The Harrier's own source was read from its installed folder. No other author's code was run.
