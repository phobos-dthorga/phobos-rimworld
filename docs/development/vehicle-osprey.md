# The V-22 Osprey: design (18 Nov 2026)

**Status (18 Nov 2026):** a design record. No part of it is in code yet, no picture is made, and nothing here is tested in game.
The owner answered its eight questions the same day ([their answers](#the-owners-answers-18-nov-2026)).

The owner asked for the Osprey on 18 November 2026: "the Osprey is definitely something I would like to implement!" This is its
design record, row MV3 in [outstanding work](outstanding-work.md). It is the first aircraft of ours, the first vehicle on
[our own flight over the map](vehicle-flight-over-the-map.md) and the first with [armour by class](vehicle-armour.md). The owner
also asked that a good portion be built, with its proper art, before any first test; [the first batch](#the-first-batch) is that
portion. The research behind it, and the owner's earlier decisions, are in
[modern military vehicles](modern-military-vehicles.md#the-tiltrotor-v-22-osprey) and are not repeated here.

Labels: **[P]** a primary source; **[S]** a secondary one; **[code]** read in code on 18 Nov 2026, never run; **[I]** our
inference; **[A]** an authored gameplay choice. Metric figures marked "(converted)" are ours.

## Read this first

- **Named the V-22 Osprey in game** (owner: real names). Home: D'thorga Vehicles. Based on the Bell Boeing V-22.
- **Real kilograms throughout.** Its empty weight, fuel and cargo are the real machine's, so the engine model that already
  judges every hover judges this one with no fudge. Its published weights then fall out of the model: it lifts its vertical
  maximum straight up below about 26 °C at the sea, and wants a strip above that or with more aboard.
- **One figure is fitted, to one published fact:** hover power, set so the model gives the guide's hover ceiling of 1,646 m at
  21.3 t. Momentum theory alone would put it a little lower.
- **Published fuel, engine power and fuel consumption give a range of about 1,770 km** against the 1,628 km published. That is
  our arithmetic, not a test, and a good sign for the model.
- **It is unarmoured, as the real one is**, with an armour kit as an upgrade. Rifle fire goes through it and hurts what is inside.
- **It defends a landing; it does not attack.** One gun on the ramp, and a belly gun as an upgrade that costs load.
- **Its speed and weight are in proportion to the Hind's**, the owner's anchor. Its range is in proportion to the Mi-38's,
  the nearest transport, because the Hind's author gave it a range no real helicopter has.
- **It is drawn on the Mi-38's scale, about 30 cells across its rotors** (owner, 18 Nov 2026): "I wish for this to be our hero
  vehicle, essentially. Plus it's easier to scale down than scale up".
- **One picture from above**, bare, with everything a player can fit drawn as its own overlay.

## The owner's decisions it stands on

| Decision | Whose | Where |
| --- | --- | --- |
| The Osprey is wanted; real name; armed as the real machine, with Combat Extended ammunition | Owner, 18 Nov 2026 | Modern military vehicles |
| One Osprey for every player, on our own flight over the map | Owner | The same, decisions 2 and 3 |
| Real proportions among our own aircraft, anchored to the Hind; other authors' aircraft left as they are | Owner | Decision 7 and the later choices |
| The tanker kit is kept, though the real programme stopped | Owner | Decision 6 |
| A vehicle in a hold only if feasible; none fits the Osprey's real cabin | Owner; agent's reading | Decision 5 |
| Eight headings; people slowed beneath a hover; real consequences for running dry | Owner | Our own flight over the map |
| A good portion built, with proper art, before a first test | Owner, 18 Nov 2026 | In conversation |

## What each behaviour lets, stops and changes

| Behaviour | Lets the player | Stops them | Changes how they play |
| --- | --- | --- | --- |
| **Two ways up** | Lift 3.6 t of cargo straight up from open ground on a mild day, or 5.6 t along a strip | The heavier load with no strip; either on a hot day at height | A strip at home; the far end of the trip decides the load |
| **A long reach at speed** | Reach places no helicopter on the list reaches as fast, and land with no runway | Nothing | Distant rescues and outposts come inside one flight |
| **A thirsty hover** | Hold over a spot | Loitering there: the hover burns the most | Quick turnarounds |
| **One engine out** | Fly home on one | Hovering: it lands rolling, or comes down | Engine damage means "where is a strip" |
| **Troops and oxygen** | Carry 24 | A high cruise with troops and no cabin oxygen | Empty legs are fast and thrifty; a full cabin flies lower until cabin oxygen is fitted |
| **No armour** | Fit a kit for a hot landing | Shrugging off rifle fire without it | Landing zones are chosen, or cleared first |
| **Ramp gun, belly gun** | Cover a landing | Attacking like a gunship | It delivers and leaves |
| **Hooks** | Sling 4.5 t on one hook, 6.8 t on two | What only the Cherokee's 11.8 t hook lifts | Two lifters with different jobs |
| **What it lands on** | Land anywhere open | Using loose sand or dry growth for nothing | Paved pads where it lands often; an inlet barrier for the desert |

## The real machine

The fuller table is in [modern military vehicles](modern-military-vehicles.md#the-real-machine). Figures added on 18 Nov 2026:

| What | Figure | Source |
| --- | --- | --- |
| Crew | "3–4 (pilot, copilot and 1 or 2 flight engineers/crew chiefs/loadmasters/gunners)" | **[S]** Wikipedia's specifications |
| Troops | "24 troops (seated), 32 troops (floor loaded)" | **[S]** the same; 24 also **[P]** Bell |
| Length; width with rotors turning | 17.48 m; 25.78 m | **[S]**; 17.47 m and 25.8 m **[P]** Boeing |
| Rotors | two of 11.61 m, 210.7 m² together | **[S]** |
| Engines | 6,150 hp (4,586 kW) take-off, **5,890 hp (4,392 kW) maximum continuous**, each | **[S]** Wikipedia, on the engine and the aircraft; 6,150 shp **[P]** Bell, Boeing. Rolls-Royce's own page now says "7,000shp" for the AE 1107C and states no ratings **[P]** |
| Engine fuel consumption | 0.426 lb/shp/hr (0.259 kg/kWh) | **[S]** Wikipedia, "Rolls-Royce T406" |
| Fuel aboard | 1,721 US gal (6,513 L) | **[P]** Bell |
| Range | 879 nmi (1,628 km); combat range 390 nmi (720 km) | **[S]**; the article's own text also says "operational range is 1,100 nmi" |
| Glide | "Maximum glide ratio: 4.5:1" | **[S]** |
| If a proprotor gearbox fails | "that proprotor cannot be feathered, and both engines must be stopped before an emergency landing" | **[S]** |
| Autorotation | "technically capable of autorotation if both engines fail in helicopter mode, a safe landing is difficult" | **[S]** |
| Armour kit | "66 plates fitting along interior bulkheads and deck", "adding 800 lb (360 kg)" | **[S]** |
| Under fire | South Sudan, December 2013: three aircraft "hit 119 times, wounding four crew", "causing flight control failures and hydraulic and fuel leaks on all three aircraft" | **[S]** |
| Flight controls | "triple-redundant fly-by-wire flight control systems" | **[S]** |
| Sand | "an inlet barrier system to reduce the power loss from the engine ingesting dust and sand particles"; engines "failed after less than 400 hours of service, as compared to the estimated life of 500–600 hours" | **[S]** |
| Downwash | "over 80 knots"; "strong enough to destroy unstrengthened helipads" | **[S]** |
| Warning and jamming | "AN/AAR-47 Missile Approach Warning System"; on the Air Force version a terrain-following radar and "Directional Infrared Counter Measures" | **[S]** |
| Ramp gun | "M240 machine gun or .50 in (12.7 mm) M2 Browning machine gun on ramp, removable" | **[S]** |
| Belly gun | a 7.62 mm GAU-17 minigun, "belly-mounted, retractable, video remote control"; 800 lb (360 kg) | **[S]** |
| Cabin tanks | "three auxiliary tanks (200 or 430 gal; 760 or 1,630 L) can also be added in the cabin" | **[S]**; 430 gal each **[P]** guide |
| Tanker kit | "up to 12,000 lb (5,400 kg) of fuel"; hose out at 185 knots | **[S]** |
| Hoist | "a 600 lb hoist to the ramp" | **[S]**; 600 lb and 250 ft of cable **[P]** guide |

## The aircraft in game

### Size

**Decided (owner, 18 Nov 2026): the Mi-38's scale.** "I wish for this to be our hero vehicle, essentially. Plus it's easier to
scale down than scale up". The agent had recommended the Hind's scale, a picture of 16 x 11 cells.

| | Real | In game | How |
| --- | --- | --- | --- |
| Picture, across the rotors | 25.78 m | **about 30 cells** | The Mi-38's picture is 24.79 cells for a 21.1 m rotor: 1.175 cells a metre **[I]** |
| Picture, nose to tail | 17.48 m | **about 20.5 cells** | The same scale |
| The airframe without its rotors | wingspan 13.97 m, plus the nacelles | about 18 x 20.5 cells | The rotors are overlays of their own, each about 13.6 cells across |
| Footprint | fuselage 17.48 m | **5 x 9** cells | The Mi-38's footprint is 5 x 11 for a 20.28 m body; the same ratio gives 9 **[I]** |

- **The footprint stays 5 x 9** (agent default): it is what must be clear to land, what a strip is as wide as, and what is hit.
  The picture overhangs it widely, as the Mi-38's does. Both sides are odd, so the fault in reading an even-length vehicle's
  cells (row CR7) cannot touch it.
- **How large a picture the services make.** Our trucks ship at 128 px a cell. The airframe without its rotors is about 18 x
  20.5 cells, so it wants about 2,600 px on its long side to match them. Every paid route makes that in one picture: the
  OpenAI engine takes up to 3,840 x 2,160 px in area (OpenAI's API reference as read on 8 Nov 2026; never tried by us above
  768 x 1,536), and the models reached through fal go to 4K. **An earlier version of this record said the paid engine stopped
  at 1,536 px and that one picture was not enough. That was the agent's error**, made without checking the engine, and
  corrected the same day. No sections need joining.
- **Which service draws it** (owner, 18 Nov 2026): "I don't think ChatGPT would be a good fit visually for this", and another
  service is welcome "if the outlay is not much". So the body is tried on several services through fal, one picture each from
  the same brief and the same two references of our own, and the owner picks the look
  ([the fal engine](paid-image-generation-rules.md#running-the-fal-engine)). The owner's rule that art is only ever scaled
  down stands.

### Weights, speed and crew

| Figure | Value | How |
| --- | --- | --- |
| Empty weight | 15,032 kg | **[P]** fact sheet; sources give 14,432 to 15,347 kg |
| Fuel aboard, full | 5,210 kg | 6,513 L **[P]** at 0.80 kg a litre **[A: jet fuel's usual density]** |
| **Cargo capacity** | **5,600 kg** | The short take-off maximum, 25,855 kg **[P]**, less empty weight and full fuel **[I]** |
| Heaviest it flies | 25,855 kg | **[P]** |
| Vehicle Framework's mass stat | 1,950 | The Hind's 1,100 for 8,500 kg empty **[I]** |
| Flight speed | **17.5** | The Hind's 10 for 270 km/h **[S]**; the Osprey's 463 to 493 km/h **[P]** gives 17.1 to 18.3 |
| Pilots | 2 seats, **2 needed** | The real crew; the Cherokee and the Mi-38 also need two |
| Crew chief | 1 seat; works the ramp gun | **[S]** |
| Troops | 24 seats | **[P]**, **[S]** |

**The cargo figure is in real kilograms, where other authors' are not.** The Cherokee carries 4,600 for a Chinook's real 10,886 kg
and the Mi-38 5,000 for a real 6,000 kg, so in game the Osprey out-carries the Cherokee, which the real one does not. The
owner's rule leaves their figures as they are. **Decided: 5,600 kg, and two pilots needed** (owner, 18 Nov 2026).

### Engines and lift

A drivetrain Def of its own, two engines bound as `Engine1` and `Engine2`:

| Figure | Value | Standing |
| --- | --- | --- |
| Maximum continuous | 4,392 kW each | **[S]** |
| Take-off rating | 1.044 of maximum continuous (4,586 kW) | **[S]**; its time limit, 10 minutes, is **[A]** |
| Fuel at idle | 0.2 of full | The turboshaft class's figure, built |
| Hover power at its heaviest (25,855 kg), sea level, 15 °C | **1.055** of maximum continuous | **[I]: fitted** so the model gives the guide's hover ceiling, 1,646 m at 21,319 kg **[P]** |
| Rolling share | 0.886 | Worked from its two published take-off weights (our own flight's record) |
| Forward-flight share | 0.6 | **[A]** |
| Ceiling; design cruise height | 7,620 m **[P]**; 4,600 m **[A]**, where its top speed is published **[S]** |
| Drive limit | None set | No source gives the gearboxes' rating |

Momentum theory by itself gives the ideal hover power at 25,855 kg as 5,620 kW, 0.64 of maximum continuous; real rotors and the
wing in the downwash roughly half as much again **[I]**. The fitted 1.055 is somewhat above that, which a published ceiling with a
safety margin would explain.

**What the model then gives (our arithmetic, full fuel):**

| Weight | Where | Hover margin | What happens |
| --- | --- | --- | --- |
| 23,859 kg, the published vertical maximum | Sea level, 15 °C | 1.12 | Lifts straight up |
| The same | Sea level, 25 °C | 1.02 | Lifts straight up, just |
| The same | Sea level, 35 °C | 0.93 | Rolls: 14 cells |
| The same | 1,000 m, 8.5 °C | 0.94 | Rolls: 12 cells |
| The same | 2,000 m, 2 °C | 0.79 | Refused: lighten |
| 20,000 kg | 2,000 m, 2 °C | 1.04 | Lifts straight up |
| 25,855 kg, the published short take-off maximum | Sea level, 15 °C | 0.99 | Rolls: 6 cells |
| The same | Sea level, 35 °C | 0.82 | Refused: lighten |

Strips are for a full roll of 20 cells and a shortest of 5 **[A]**. **The published vertical maximum is where the model stops
lifting straight up on a warm day**, and the published short take-off maximum is where it just fails to on a standard one. The
figures were not fitted to either.

**One engine** (sea level, 15 °C): margin 0.93 at 17,000 kg, 0.73 at 20,000 kg, 0.63 at 22,000 kg, 0.56 at the vertical maximum.
It never hovers on one, as its sources say; below about 22.7 t it flies on and lands rolling; above that it comes down.

**The proprotor gearboxes** are two parts of the rotor drive. With one destroyed, both engines stop and the aircraft comes down,
as the real one must **[S]**. The shaft between them is why one engine turns both rotors, and is a third part: with it destroyed,
an engine lost afterwards takes its own rotor with it.

### Fuel and range

| Figure | Value | How |
| --- | --- | --- |
| Tank | **1,500** chemfuel | 4.3 L a unit of the real 6,513 L **[A]** |
| Burn | **200** a day at Vehicle Framework's own rate | **[A]**: between the Cherokee's 74.5 and the Mi-38's 140 scaled by engine power (93 and 290) |
| Days of fuel | 7.5 | |
| World range, by Vehicle Framework's formula | **about 790** | 1.9 times the Mi-38's 420; the real ranges are 1,628 and 880 km, 1.85 times **[I]** |

- **The cross-check:** at 0.6 of maximum continuous on both engines, the published consumption burns 1,365 kg an hour; the
  published fuel lasts 3.8 hours; at 463 km/h that is 1,770 km, against 1,628 km published **[I]**.
- **Not anchored to the Hind here:** its author's world multiplier gives it a range of 3,500 by the same formula, more than
  eight times the Mi-38's, where the real Hind's 450 km is half the Mi-38's.
- **A full tank is 1,500 chemfuel.** That is the price of the real machine's thirst, and it is the figure to tune first.
  **Decided: a 1,500 tank burning 200 a day** (owner, 18 Nov 2026).
- Fuel by flight phase, thin air and the cruising height apply as built or designed; nothing new is needed.

### Parts and armour

One outer part to a cell, by [the armour record's rule](vehicle-armour.md#one-outer-part-to-a-cell). Health is authored, in the
range of the transports on the list (the Cherokee 2,090 in all, the Mi-38 2,760).

| Part | Depth | Health | Class | Notes |
| --- | --- | --- | --- | --- |
| Cockpit | Outer | 180 | Skin | The pilots' cells |
| Forward cabin, aft cabin and ramp | Outer | 260 each | Skin | The troops' cells; the armour kit raises these and the cockpit |
| Left wing, right wing | Outer | 160 each | Skin | Flight control; wing fuel |
| Left nacelle, right nacelle | Outer | 150 each | Skin | |
| Tail | Outer | 140 | Skin | Flight control |
| Left engine, right engine | Inner | 130 each | Splinter | `Engine1`, `Engine2`; may explode |
| Left and right proprotor gearbox | Inner | 110 each | Splinter | The rotor drive |
| Interconnect shaft | Inner | 70 | Skin | The rotor drive |
| Forward tank, wing tanks | Inner | 120, 100 | Skin | Leak; may explode |
| Three flight-control channels | Inner | 50 each | Skin | "Triple-redundant" **[S]**; control is lost only with all three |
| Hydraulics | Inner | 80 | Skin | Leaks when hit, as in South Sudan **[S]** |
| Avionics bay | Inner | 60 | Skin | |
| Nose gear, left and right main gear | Outer, beneath | 70, 90, 90 | Skin | The first parts bound to the landing gear role, which a rolling landing wears |
| Hook mount | Inner | 60 | Skin | With the cargo hook |

About 2,900 health in all. Under Combat Extended the Skin class is 2 mm and 4 MPa, the Splinter class 5 mm and 20 MPa; without it,
ratings of 0.10 and 0.25. Its Combat Extended hit box is 5 cells wide and 1 high **[A]**, as the Harrier's is 5.

**The armour kit** (an upgrade): the cockpit and both cabin parts to the Rifle class, 360 kg **[S]** off the cargo. What the real
plates stop is not stated in what was read; Rifle is **[A]**.

### Guns

| Gun | Real | In game |
| --- | --- | --- |
| **Ramp gun, 7.62 mm** | M240, 7.62x51 mm, 650 to 950 rounds a minute **[S]** | A turret facing aft with an arc of 60 degrees each side **[A]**, worked by the crew chief. Combat Extended: `AmmoSet_762x51mmNATO` |
| **Ramp gun, .50** | The M2 on the ramp **[S]**; a GAU-21 in a Marine Corps photograph **[P]**, "1,100 rpm" **[S]** | The same mount, either this or the 7.62 mm. `AmmoSet_50BMG` |
| **Belly gun** | GAU-17 minigun, 7.62x51 mm, 2,000 or 4,000 rounds a minute selectable on other installations **[S]**; remotely worked; 360 kg | A turret no seat names, all round, firing by itself under our weapon-station power rule; 360 kg off the cargo |

Every turret Def sets its Combat Extended recoil outright (the armour record). Without Combat Extended each fires a base-game
projectile of its calibre's size, authored.

### Avionics, oxygen and defences

| Item | Fit | Source |
| --- | --- | --- |
| Instruments, navigation, autopilot | Full; the autopilot is its fly-by-wire | **[S]** |
| Missile warning | Yes | "AN/AAR-47" **[S]** |
| Crew oxygen | Yes | Masks are worn above 10,000 feet **[S]** |
| Cabin oxygen | **An upgrade.** Without it, a leg with anyone in the cabin is flown at 3,050 m or lower | The cabin "is not pressurized" **[S]** |
| Terrain-following radar | An upgrade: flies blind in weather and at night at full speed, through the avionics fit | The Air Force version **[S]** |
| Infrared jammer | An upgrade: countermeasures that do not run out, drawing power | "Directional Infrared Counter Measures" **[S]** |
| Flare and chaff dispensers | Not fitted | Not found in what was read |

### What it lands on

Three cases from three real events, each a small rule in the health layer **[A]**:

| Ground beneath it at take-off or landing | What happens | The real basis |
| --- | --- | --- |
| Loose sand or soil | Both engines' hot sections wear a little | Dust stalled an engine at Bellows, Hawaii, 2015; engines lasted under 400 hours in service **[S]** |
| Dry growth, most in a heat wave | A chance of fire beneath the exhaust | A grass fire at a North Carolina range, 2013 **[S]** |
| Built floor or rock | Nothing | |

**The inlet barrier** (an upgrade) takes most of the sand wear away, for a little engine power, as the real one is meant to **[S]**.
**Decided: all three cases, with the inlet barrier** (owner, 18 Nov 2026).

### The upgrade tree

By the branching rules: a link only where an upgrade does nothing or makes no sense without another. Every visible node is its own
overlay. Node keys are fixed here, before the first picture.

| Row | Node key | What it does | Link | Batch |
| --- | --- | --- | --- | --- |
| Cabin fit | `Stretchers` | 9 litters **[P]** and 6 seats in place of 24 seats | Either this, `FerryTanks` or `TankerKit` | 1 |
| | `FerryTanks` | Three cabin tanks of 375 chemfuel each (the real 1,628 L); 8 seats left | Either/or | 1 |
| | `TankerKit` | Fuel passed to another aircraft in flight | Either/or | 2 |
| Hooks | `CargoHook` | Sling loads to 4,536 kg | | 1 |
| | `SecondHook` | To 6,804 kg | Needs `CargoHook` | 1 |
| Ramp | `RampGun762`, `RampGun50` | The ramp gun | Either/or: one mount | 1 |
| | `RescueHoist` | One person up or down in a hover | | 2 |
| | `FastRopes` | People down ropes in a hover | | 2 |
| Belly | `BellyGun` | The remote minigun; 360 kg | | 1 |
| Protection | `ArmourKit` | Cockpit and cabin to the Rifle class; 360 kg | | 1 |
| | `InfraredJammer` | Countermeasures | | 1 |
| Flying | `CabinOxygen` | Troops at the design cruise height | | 1 |
| | `TerrainRadar` | Full speed at night and in weather | | 1 |
| | `InletBarrier` | Sand wear cut; a little power lost | | 1 |
| | `RefuellingProbe` | Takes fuel in flight | | 2 |
| Crew | `CrewlessFit` | The route level of working without a crew | | 2 |

The hook's two published limits disagree: 15,000 lb on both hooks **[P, guide and fact sheet]** against Bell's page, 12,500 lb.
The guide's is used.

### Cost and research

- **Shell** (its own cost): steel 900, plasteel 250, components 40, advanced components 6 **[A]**. Plasteel stands for its
  composite airframe; the advanced components for fly-by-wire.
- **By role, through the parts bill:** two turbine engines, three gearboxes, four fuel tanks, three wheels, two alternators, two
  batteries, wiring, hydraulics. Our own parts set the price; other mods' are accepted only where they suit, as for every vehicle.
- **Work:** 60,000 **[A]**; the Cherokee's is 52,000 and the Mi-38's 90,000.
- **Research:** "Tiltrotor aircraft", 6,000, after our turbines research **[A]**; the Cherokee's is 4,000. Each upgrade has its
  own small project on our research tab, as the Crawler's do. **Decided as written** (owner, 18 Nov 2026).

## Flying it

On [our own flight over the map](vehicle-flight-over-the-map.md), with these figures of its own **[A]**: forward speed over a map
0.35 cells a tick (the Hind's is 0.40), turning 1.8 degrees a tick, a flight height of 30 m, take-off and landing in 300 ticks, a
full roll of 20 cells and a shortest of 5. On the map its nacelles are upright and it flies as a helicopter; between tiles it
flies as an aeroplane, at 17.5.

## Other factions' Ospreys

| Where | What happens |
| --- | --- |
| **A raid on us** | Not in the first batch: a raider's aircraft needs the small flight AI our own flight's record leaves for later. Until then none arrives |
| **Parked at a base we attack** | It can stand there, on the lite tier: hits are counted, its guns work if crewed, nothing else runs |
| **One we capture** | Claimed with the claim order; it flies for us at once, with whatever is fitted |
| **Armour and guns** | In the Def, so the same on anyone's |
| **Cost with several on a map** | Nothing while parked; the flight step for each in the air |

## Art

- **One body from above**, nose up, nacelles upright, **bare**: no guns, hooks, probe, radar, people, damage or lettering. The
  east and south pictures are the same one turned by script; no further paid pictures.
- **Overlays, each its own picture:** two proprotor discs that spin and two sets of still blades; cargo hook, second hook; ramp
  gun (two); belly turret; refuelling probe; radar nose; rescue hoist; ropes; inlet barriers; armour kit (if anything of it
  shows); damage looks for the cockpit, cabin, wings and nacelles. The batch-2 overlays are drawn with the rest, so the body
  never needs redrawing.
- **Paint mask** by region, then repaired by parts; judged tinted on dark ground, with the fleck count, before the owner sees it.
- **Icons:** the vehicle's, and one for each of seventeen upgrades and each new button, on the owner's local model.
- **Before any paid picture:** the trial across services (under [size](#size)), a dry run with its cost, and a spending
  limit from the owner. One pilot picture from each, inspected at full size, before anything else.
- **Only our own art is ever uploaded** as a reference.

## Sound

Its own set, shared with nothing: engine start, ground idle, the hover's beat, forward flight, shut-down; the ramp; each gun; the
hook; the warning tone. Made locally under the sound rules, picked from a listening page with spectrograms and measurements, and
entered in [the sound catalogue](sound-catalogue.md) when built.

## The first batch

What is built before the owner's first test:

| Part | What |
| --- | --- |
| Flight | The cruising height, the hover half, leaving and arriving, leaving the ground rolling (our own flight's slices 1 to 4) |
| Armour | The classes, the Combat Extended patch file, the width extension, the gate check |
| The Osprey | Its Def, engines, parts, bill, research, the batch-1 upgrades, both ramp guns and the belly gun, its avionics fit |
| Art and sound | The body, every overlay, the mask, the icons; its sounds |
| Also | The stock-helicopters setting; a fault-read by a separate reader before the install; seen-in-play notes and verbose logging throughout |

**Second batch:** circling; ropes and the hoist; the tanker kit and the probe; the crewless fit; a raider's flight AI.

## The owner's answers (18 Nov 2026)

Asked as eight cards in two rounds after the record was written; the owner answered all eight the same day.

| # | Question | The agent recommended | The owner chose |
| --- | --- | --- | --- |
| 1 | Size | The Hind's scale: a 16 x 11 picture | **The Mi-38's scale**, about 30 cells across: "I wish for this to be our hero vehicle, essentially. Plus it's easier to scale down than scale up" |
| 2 | Cargo | 5,600 kg, in real kilograms | **5,600 kg** |
| 3 | Pilots | Two needed | **Two needed** |
| 4 | Fuel | A 1,500 tank burning 200 a day | **1,500 and 200** |
| 5 | In the air under Combat Extended | Shot at and shooting as from the ground in the first build; real height as a later slice | **As from the ground, first** |
| 6 | The first batch's upgrades | The twelve marked batch 1 | **The twelve** |
| 7 | What it lands on | Sand wear and a fire risk, with the inlet barrier | **Yes, with the inlet barrier** |
| 8 | Research | 6,000 after our turbines research, a small project per upgrade | **6,000, after turbines** |

## Who chose what

- **Owner (18 Nov 2026):** the Osprey; its real name; armed as the real machine; our own flight; the Hind as the anchor; the tanker
  kit kept; a good portion built with proper art before a first test. **Later the same day:** the Mi-38's scale, as the hero
  vehicle (against the agent's recommendation of the Hind's scale); 5,600 kg; two pilots; 1,500 and 200; ground-level hit boxes
  in the air first; the twelve upgrades; what it lands on, with the inlet barrier; research at 6,000 after turbines.
- **Agent recommendations, for the owner to accept or change:** the Mi-38 as the anchor for range; the trial of the body
  across services.
- **Agent defaults, owner to revise:** the 5 x 9 footprint under the larger picture; the parts list and every health figure; no drive limit; three flight-control channels as
  parts; the guns' arcs; the batch split; the shell's cost; the on-map flight figures; the cabin fits' seat counts.
- **Authored:** everything marked [A].

## Sources

Findings are the sources'. The game figures, every simplification and every authored value are ours; no source endorses them.
Web pages were read by a research helper on 18 Nov 2026.

| Source | What it supports | How read |
| --- | --- | --- |
| Bell, product page for the Bell Boeing V-22 ([page](https://www.bellflight.com/products/bell-boeing-v-22)) | Fuel aboard, take-off weights, hooks, 24 troops, cruise 266 kt, one-engine ceiling, "400 nm on one tank" | The page |
| Boeing, product page for the V-22 ([page](https://www.boeing.com/defense/military-rotorcraft/v-22-osprey)) | Dimensions, the engines' 6,150 shp, the vertical maximum | The page |
| Rolls-Royce, AE 1107C ([page](https://www.rolls-royce.com/products-and-services/defence/aerospace/transport-tanker-patrol-and-tactical/ae-1107c.aspx)) | "7,000shp"; no ratings stated | The page |
| Bell Boeing, *V-22 Osprey Pocket Guide*, 2007, and a 2002 fact sheet, as read for [modern military vehicles](modern-military-vehicles.md) | Empty weight, the hover ceiling, ceilings, cabin tanks, stretchers, hoist, landing gear | That record's reading |
| Wikipedia, "Bell Boeing V-22 Osprey" ([page](https://en.wikipedia.org/wiki/Bell_Boeing_V-22_Osprey)) | The specifications block; armour kit; South Sudan; redundancy; guns; gearbox failure; autorotation; downwash; cabin tanks; tanker kit | The page |
| Wikipedia, "Rolls-Royce T406" ([page](https://en.wikipedia.org/wiki/Rolls-Royce_T406)) | Ratings, fuel consumption, the inlet barrier, engine life | The page |
| Wikipedia, "Mil Mi-24", "Mil Mi-38", "Boeing CH-47 Chinook" ([Mi-24](https://en.wikipedia.org/wiki/Mil_Mi-24), [Mi-38](https://en.wikipedia.org/wiki/Mil_Mi-38), [CH-47](https://en.wikipedia.org/wiki/Boeing_CH-47_Chinook)) | The comparison aircraft's real sizes, weights, speeds and ranges | The pages |
| Wikipedia, "M134 Minigun", "M2 Browning", "M240 machine gun" ([M134](https://en.wikipedia.org/wiki/M134_Minigun), [M2](https://en.wikipedia.org/wiki/M2_Browning), [M240](https://en.wikipedia.org/wiki/M240_machine_gun)) | Cartridges and rates of fire | The pages |
| The aircraft Defs on the owner's list | Every game figure of another author's quoted here | `scripts/query-defs.py`, 18 Nov 2026 |
| [Our own flight over the map](vehicle-flight-over-the-map.md), [armour](vehicle-armour.md), [thin air and turbines](vehicle-altitude-and-turbines.md) | The rules these figures feed | Our own records |

**Unverified. No Def carries these as fact:**

- The US Naval Air Systems Command's and the US Air Force's own pages (both refused reading).
- What the real armour kit stops; the gearboxes' power rating; the take-off rating's time limit; the number of hydraulic systems.
- Whether the real aircraft carries flare and chaff dispensers.
- Jet fuel's density as 0.80 kg a litre for the fuel it actually uses.
- A fuel-flow or endurance figure for the aircraft itself: only Bell's "five hours or more" with extra tanks and refuelling.
