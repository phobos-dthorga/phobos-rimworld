# Unique military vehicles and submarines: research (9 Nov 2026)

**Status: research and record only. Nothing here is built, and nothing is a commitment to build.** Researched 9 November
2026 by an agent at the owner's request ("truly unique in their design and perhaps not provided for in other mods, whether
flying, VTOL or ground-based"), with submarines added as their own category on the owner's review of the plan. Owner
choices, agent defaults and agent recommendations are labelled as such. Names players would see follow the brand rule,
`D'thorga <Plain Word>`; none is checked against the Workshop yet beyond the gap table below.

How to read this file:

- **[P]** primary source (a navy, a maker, an archive); **[S]** secondary (Wikipedia, GlobalSecurity, a magazine);
  **[I]** our inference about the game or the framework; **[A]** an authored gameplay choice. "(snippet)" marks a figure
  seen only in a search excerpt. Where sources disagree, both are given.
- Every Vehicle Framework claim comes from its Defs, its decompiled code as summarised in
  [Vehicle Framework mechanics](vehicle-framework-mechanics.md), and our past records, never from running the idea.

## Read this first

- **Why these ten.** Each fills a gap no Vehicle Framework mod on RimWorld 1.6 or 1.7 fills, and each drives a system we
  already have: the vehicle health and parts layer, PhobosGases, D'thorga Medical, the generator set, the depot bays, the
  tanker, or Dubs Bad Hygiene through its tie-in.
- **What the gap check covered.** The three Steam collections of Vehicle Framework mods ("1.6 List of Mods using vehicle
  Framework", 3497770083, 164 items; "[1.5] Mods using vehicle framework", 3419713592, 178; "Rimworld Vehicles and
  Aerocrafts", 3185234947, 72): 277 unique items, 17 of them no longer public. Read through Steam's keyless
  `GetCollectionDetails` and `GetPublishedFileDetails` APIs, by title, version tags and the opening of each description,
  and re-checked with `python scripts/workshop-research.py collection <ID> --grep "*word*"` (phobos-modkit 1.2.0, built
  for this; its run on 9 Nov 2026 found no item for recovery, decontam, shower or ekranoplan, and the only match for
  "submarine" is the text of a VTOL transport's description). **Not covered:** mods outside those collections, mods named only in Chinese or Japanese (the descriptions
  were read in English), and quality: a mod's presence says nothing about whether it works. Version tags are the
  authors' statements.
- **Feasibility is untested.** The one engine fact that matters most: Vehicle Framework's `vehicleType` is Land, Sea,
  Air or Universal ([mechanics](vehicle-framework-mechanics.md), design fields), and it has no depth. An amphibious
  vehicle is therefore plausible as `Universal` **[I]**, and a submarine's dive can only be **a state of ours** **[A]**.
  Both need a `query-code.py` reading of VF's path grids and world-map hooks before any Def.
- **Radio stays on the back burner** (owner, 9 Nov 2026): the radio van and the kite balloon are catalogue lines only.
- **Armour is set aside** as the tuning record left it: armour goes through Combat Extended's rules or not at all, so
  every vehicle below is written as an unarmoured utility vehicle **[agent default]**.

## Workshop gap check

| Concept | What exists on Vehicle Framework (version tags as declared) | Verdict |
| --- | --- | --- |
| Armoured recovery vehicle: winch, crane, tow | none; R3 Capraia (1.6) mentions a crane add-on | open |
| Jet-blower decontamination or smoke truck | none | open |
| Shower, laundry or bath truck | none; interiors exist (Vehicle Interior Framework 1.7, RV with built-in PD 1.6) | open |
| NBC reconnaissance vehicle | none | open |
| Mobile operating theatre | HE-Ford F350 Ambulance '90 (1.6) and Gehenna Emergency Vehicle (1.6), transport only | partly |
| Air-cushion landing craft carrying vehicles | Edge of Descension Hovercrafts (ultratech transport, 1.7), Hovercraft (Blue Archive, 1.5); neither carries vehicles. Vehicles - partridge (Helldivers 2, 1.6) says it transports vehicles | open |
| Screw-propelled vehicle | none | open |
| Articulated amphibious tracked carrier | Snow Crawler (Continued) (1.6) is a single-body snowcat | partly |
| Polar cruiser with an interior | Nomadic Carts, Technical map Vehicles, Vehicle Map Framework (1.6) | partly |
| Ekranoplan | none | open |
| Amphibious bridging and ferry rig | none; Trains of the Rim (1.6) is the only track-laying precedent | open |
| Mine clearer: jet, flail or roller | none | open |
| Thermobaric launcher | Burya MRL (VVE Tier 3), Breacher Missile Artillery (1.5), Viperidae plasma artillery (MTG, 1.6); none thermobaric | crowded by type |
| Mobile nuclear power station | our generator set is diesel; nothing nuclear | open |
| Armoured train | Trains of the Rim (civil, 1.6); A27M Cromwell + CromRail (1.4); RimThunder Aurora of Liberation is a light cruiser (1.5) | partly |
| Flying platform or tailsitter | none, but VTOL transports are crowded: Skyranger (three uploads), three Pelicans, UH-144 Falcon, Harrier, Hind, Mongoose, Local Flight Systems | open, crowded family |
| Tethered kite balloon | hot-air balloons only (Alpha Vehicles Neolithic, Medieval Overhaul Air Vehicles, Airships Fleet) | open |
| Walking engineering vehicle | Starcraft Goliath (1.6), Vehicle Framework Expanded Classic Mechs (1.5) | partly |
| Submarine or submersible of any kind | none; on water: VVE's boats, Boats (Continued), Alpha Vehicles Age of Sail, Fishing Boats (Continued), RX97 Tradeship and Simple Raft, Titan Vehicles' frigate, CVN Mechanoid Naval Warfare (1.5) | **open, whole category** |
| Electronic attack aircraft | EA-6B Prowler (1.7) jams turrets, electrics and mechanoids | taken |
| Object 279, Maus, Landkreuzer P1000, T-35, Saint-Chamond | all exist (Object 279 at 1.5 only; Ratte 1.6; two Maus mods 1.6 and 1.7) | taken |

## The shortlist: five land and water vehicles

Agent recommendation order; the owner picks. Each has the three questions every new behaviour must answer (AGENTS.md).

### 1. D'thorga Recovery Vehicle

**Real basis.** The Bergepanzer line: Bergepanzer 2 on the Leopard 1 hull, in service from 1966 **[S]**; Bergepanzer 3
Büffel on the Leopard 2, with a crane that lifts, lowers and swivels loads of up to 30 tonnes, a dozer blade and a main
winch, for recovering tracked vehicles, maintenance and clearing obstacles **[P, Rheinmetall]**. The wartime ancestor is
the Bergepanther **[S]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Tow | Bring a vehicle home that has no fuel, a dead battery or a destroyed part | Towing a crewed or hostile vehicle; a tow needs a driver and a clear route | A breakdown far from home is a job, not a loss |
| Field crane | Swap a heavy part where the vehicle stands, with the parts bench's work total | Nothing; the parts still have to be carried there | Spares travel to the breakdown instead of the wreck travelling home |
| Winch and blade | Pull a bogged vehicle out of mud or snow, push rubble clear | Nothing | Terrain traps cost time, not vehicles |

**On what we have.** The vehicle health layer's parts and conditions, the parts bench, the depot bays' service work, and
the catalogue's "recovery truck" line (lean High) in [utility vehicles](utility-vehicles-design.md). **Feasibility [I]:**
VF has no tow; the honest routes are a "follower" that paths behind the recovery vehicle, or a carried-vehicle mechanic
(what Partridge claims). Size against RimThunder's 3x10 PLS: a 3x8 tracked hull with a crane overlay.

### 2. D'thorga Decontamination Truck

**Real basis.** The Soviet TMS-65 (TMS-65U in Russian service today): a VK-1 turbojet, the MiG-15's engine and a
Rolls-Royce Nene derivative, in a turret on the rear of a Ural-375 6x6 truck; decontamination fluid is fed into the
exhaust to wash vehicles and buildings fast, and the same tank filled with fuel oil lays a smoke screen **[S, The War
Zone; S, Armorama]**. Hungary's Big Wind, a T-34 hull with two MiG-21 R-25 engines, put out oil-well fires in Kuwait in
1991 **[S, The Drive; S, IFLScience; S, Wikipedia oil well fire]**. The Soviet jet mine clearer Progvev-T (a MiG-15 engine
on a T-54 or T-55 hull; sources disagree) is the same idea aimed at the ground **[S]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Blow | Clear a gas (PhobosGases, VEF gases, Combat Extended smoke) out of a room or a yard by driving at it | Nothing; the gas goes somewhere, and the jet is loud and thirsty | Gas becomes a thing a crew fights, not a thing they wait out |
| Rinse | Wash exposed pawns and vehicles: D'thorga Medical's strip-and-rinse after caustic exposure, done by a vehicle | Nothing | A camp can decontaminate without a bath |
| Smoke | Lay a screen from the same tank filled with fuel | Seeing through it themselves | Movement under fire changes |
| Fire (upgrade node, after Big Wind) | Blast and water on a burning building | Nothing | The tanker's fire monitor gets a heavier sibling |

**Honest chemistry note for the owner.** Real decontaminants are chlorine-releasing (calcium hypochlorite slurries) or
alkaline solutions; the game's caustic model in D'thorga Medical would treat a spray of them as caustic in its own right,
so the rinse should be water or a named "decontaminant" item, with the owner to say what it is. The jet is a 1947 design:
its fuel use should hurt **[A]**, or the owner says otherwise.

### 3. D'thorga Shower Truck

**Real basis.** The Soviet DDA-66 on a GAZ-66: a boiler, showers and a steam disinfection chamber, a chemical-defence
asset widely used for other purposes such as field laundry; the DDA-2 shower unit serves sanitary-epidemiological units
**[S, snippet]**. The CIA's reading room holds a recognition card for the earlier DDA-53C decontamination truck on the
same chassis **[P, CIA FOIA]**. **Figures for the DDA-66 are thinly sourced**; nothing here depends on them.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Shower | Wash a caravan or a camp (Dubs Bad Hygiene's hygiene need) from a truck | Nothing; it needs water from the tanker, a river or DBH's supply | A camp stays out longer (RimExodus camping; the owner camps in real life) |
| Disinfect | Strip-and-rinse after caustic exposure (D'thorga Medical's setting), steam apparel | Nothing | Field care gains a step between the fight and the hospital |
| Laundry | Clean apparel's hygiene state where DBH tracks it | Nothing | Hygiene stops being a reason to go home |

**On what we have.** DBH is optional in every Phobos mod, so the truck's washing lives in an `IfModActive` folder and the
truck itself ships a plain water tank and boiler without it **[I]**.

### 4. D'thorga Landing Craft

**Real basis.** The Zubr class, the largest hovercraft in service: 555 tonnes full load, a 400 m² cargo deck, three main
battle tanks or ten armoured vehicles, 56 tonnes of fuel; seven to ten in service depending on the source **[S,
Wikipedia; S, GlobalSecurity]**. Scaled down for RimWorld to a craft that carries two of our trucks or one tank-sized
vehicle **[A]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Hover | Cross water, marsh, shallows and mud, and run up the beach | Hills, trees, walls: flat terrain only | Rivers and coasts become routes rather than walls |
| Carry | Take vehicles and cargo across in one trip | Carrying while hovering over nothing flat; loading needs a ramp and a shore | A coastal colony's vehicles reach the other bank |

**Feasibility [I].** VF's `vehicleType` has a `Universal` value; what it does to the path grid and the world-map costs
must be read in VF's code before a Def. Vehicle-in-vehicle: read how Vehicles - partridge does it. The owner's love of
boats is the reason this ranks above the snow carrier despite the harder engineering.

### 5. D'thorga Snow Carrier

**Real basis.** Hägglunds' Bv 206: an articulated two-car tracked carrier developed for the Swedish Army from 1974,
fully amphibious by its tracks at about 4.7 km/h, with over 11,000 built and users including the American and
Australian Antarctic programmes **[S, GlobalSecurity; S, Silodrome]**. Ground pressure figures differ by source (about
1.3 N/cm² per car, or 11.6 to 13.6 kPa). A real ambulance body exists. For comparison the Soviet ZIL-2906 screw-propelled
vehicle of the Blue Bird cosmonaut-recovery complex ran at about 65 km/h on roads and 16 km/h in water, was superb in
marsh and snow, hopeless on hard ground, and was dropped after a few of its ZIL-4904 successors were built **[S,
Sariel.pl; S, The Drive]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Low ground pressure | Cross snow, bog and sand that stops trucks | Nothing; it is slow and small | Ice sheet and tundra maps have a vehicle |
| Swim | Cross a river at walking pace | Deep water in weather | The first small test of `Universal` before the landing craft |
| Ambulance body (node) | Carry a patient with D'thorga Medical's kit aboard | Cargo in the rear car | Field care reaches where trucks cannot |

**Why fifth and not a screw drive.** The Bv 206 is the proven machine and a small amphibious test case; the screw drive
is a possible later vehicle if VF can be taught "fast in marsh, slow on roads" through its terrain costs **[I]**.

## Submarines: their own category, five in full

**The owner began the framework for these on 9 November 2026**, inside D'thorga Framework: see [the submersibles framework](submarine-framework.md). It takes the dive-as-a-state model below. **The owner chose the Seabed Crawler as the first boat on 10 November 2026**: see [its design](vehicle-seabed-crawler.md).

**What bounds every submarine idea [I] and [A].** Vehicle Framework has no depth. A Sea vehicle already crosses ocean
tiles on the world map and deep water on a map. A dive is therefore a state of ours: on the world map it hides the
caravan from the ambush and raid hooks VF exposes; on a map the boat draws as a shadow, is untargetable and may not fire
or load. Surfacing is where play happens: coasts and river mouths are the ports. Endurance submerged is the natural
resource, battery or air-independent fuel per hour, which the systems service already models. **No Vehicle Framework
mod has a submarine of any kind**, so the whole category is open. The owner's love of boats ("you never know what
adventure awaits") is the reason the category exists.

### 1. D'thorga Cargo Submarine

**Real basis.** Deutschland, 1916: an unarmed merchant submarine with a wide beam for 700 tonnes of cargo (750 in one
account), which left Bremen with dyestuffs and chemicals in June 1916 and later discharged ten million dollars of cargo
at New London **[S, Wikipedia; S, USNI Proceedings 1965; P, Scientific American archive]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Dive on the world map | Move goods and people along a hostile coast and into a besieged colony unseen | Fighting, carrying vehicles, leaving deep water | The sea is a safe road when the land is not |
| Port | Load and unload at any coast or river mouth | Trading while submerged | Coastal settlements become trade partners by sea |

The plainest use, so first **[agent recommendation]**.

### 2. D'thorga Chemical Submarine

**Real basis.** Narcís Monturiol's Ictíneo II, launched 1864, first under human power in 1865, under steam on the surface
in 1867, and then with the first air-independent engine: a reaction of zinc, manganese dioxide and potassium chlorate
that gave heat for the boiler and released oxygen for the crew, with a submerged endurance of seven and a half hours
reported **[S, Amusing Planet; S, Wikipedia]**. Wikipedia's article instead says peroxide, so **the fuel chemistry is
disputed** and any Def would say "approximate" with the source named. George Garrett's steam Resurgam (1879, sank 1880)
is the comparison **[P, Ships Project survey PDFs]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Dive on chemistry | Build a submarine at industrial tech, before electrics, its endurance bought with an oxidiser stock | Long dives, speed | Chemistry sits before electricity on the research path |
| Surface under steam | Run on chemfuel or wood on the surface | Nothing | The same boat is a slow steamer between dives |

Reaction chemistry is the owner's own field, so this record asks the owner for the mass balance before a Def: how many
kilograms of chlorate per hour submerged, and what the game item is **[open]**.

### 3. D'thorga Seabed Crawler

**Real basis.** NR-1, the US Navy's nuclear research submarine (launched 25 January 1969, in service 27 October 1969):
two retractable rubber-tyred bottoming wheels that hold a fixed distance between the keel and the seabed, a manipulator,
and the independence from surface ships that a reactor gives **[P, NAVSEA inactivation report; P, DANFS; S, Wikipedia;
S, GlobalSecurity]**. The Ben Franklin PX-15 drifted 1,444 nautical miles in the Gulf Stream for 30 days in 1969 with six
aboard, a research habitat rather than a vehicle **[S, NASA Ocean Planet; S, Guinness]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Crawl | Survey and deep-drill under water cells with the survey truck's and drill rig's stats | Combat, cargo | Lakes and coasts become a mineable map |
| Lift | Salvage debris that lands in water, lay and lift things on the bottom | Nothing | Lost things in the water stop being lost |
| Nuclear node | Stay down for a season | Nothing, but needs Rimatomics or a reactor part of ours | Endurance becomes a research prize |

### 4. D'thorga Carrier Submarine

**Real basis.** The I-400 class carried three Aichi M6A1 Seiran floatplanes in a watertight hangar with a catapult, a
5.5-inch gun and ten anti-aircraft guns; two entered service and I-402 was completed five weeks before the war's end
**[S, Wikipedia I-400 class, I-401; S, Air and Space Forces]**. France's Surcouf carried two 203 mm guns in a
pressure-tight turret and a Besson MB.411 floatplane, an "underwater heavy cruiser", the largest submarine until the
I-400 **[S, Wikipedia; S, HistoryNet]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Hangar | Bring a light aircraft (ours, or a listed one) to a coast unseen | Flying while submerged; the aircraft needs calm water to launch | A coastal raid or rescue becomes a naval operation |
| Deck gun | Bombard from the surface | Firing submerged | A siege has a sea side |

Vehicle-in-vehicle is the feasibility question, shared with the landing craft **[I]**.

### 5. D'thorga Polar Submarine

**Real basis.** USS Nautilus crossed the North Pole submerged on 3 August 1958 and surfaced after 96 hours and 1,590
nautical miles under the ice **[S, Woods Hole; S, American Philosophical Society]**; USS Skate was the first to surface
through the ice at the Pole, on 17 March 1959, after ice too heavy to break in August 1958 **[S, USNI Proceedings 1959;
S, Wikipedia]**. The Typhoon class (Project 941) gave crews of about 160 on patrols of 120 days a small heated pool and a
sauna, the habitat angle for long voyages **[S, National Interest; S, Naval Encyclopedia]**.

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Under the ice | Travel along an ice-sheet coast and surface where no boat can | Nothing new | Polar play has a vehicle (the owner's Antarctica wish; House D'thorga's gravship Audeamus) |
| Long patrol | Live aboard for a season with beds, a galley and a bath | Nothing | A submarine is a mobile home at sea |

**RimWorld has no sea ice tiles**, so "under the ice" means pathing across ice sheet tiles that border ocean as if
beneath them: a gameplay simplification, labelled **[A]**.

### Submarine catalogue

| Boat | What it was | Game idea | Source |
| --- | --- | --- | --- |
| Type XIV Milchkuh | Supply U-boat: about 430 t of fuel, torpedoes and fresh food, refuelled other boats on the surface by hose from 7 Dec 1942; none survived | A tanker-at-sea node for our tanker's contents | [S] Wikipedia, Naval Encyclopedia |
| Type XB minelayer; narco semi-submersibles | | A minelayer; a cheap half-submerged smuggler | not yet sourced |
| X-craft and Seehund | Midgets: six X-craft attacked Tirpitz on 22 Sep 1943, two laid charges, Tirpitz out until May 1944; 285 Seehunds built, 142 sorties Jan to Apr 1945 | A sabotage boat that plants charges at a shore | [S] Wikipedia X-class, Seehund; warhistory.org |
| SEAL Delivery Vehicle | | A wet submersible for a squad | not yet sourced |
| Project 1231 | Khrushchev's diving missile boat; cancelled 1964, no prototype | A gunboat that hides | [S] Wikipedia mirror |
| Ushakov's LPL and Reid RFS-1 | Flying submarines: a 1930s Soviet student project; a US craft that flew in 1962 or 1964 and failed its immersion tests | An owner's-delight oddity | [S] Wikipedia Flying submarine; Today I Found Out |
| Project 705 Lira (Alfa) | Titanium hull, lead-bismuth reactor whose coolant froze if let cool, a crew of about 16, seven built 1968 to 1981 | A reactor that must never stop | [S] Naval Encyclopedia; National Interest |
| Ben Franklin PX-15 | 30-day Gulf Stream drift, 1969 | A drifting research habitat | [S] NASA; Guinness |
| Resurgam | Steam submarine, 1879, sank 1880, found 1995 | The chemical boat's surface mode | [P] Ships Project survey |
| H. L. Hunley | Hand-cranked; sank USS Housatonic on 17 Feb 1864, then was lost | A neolithic or medieval boat for Alpha Vehicles' Age of Sail audience | [P] Naval History and Heritage Command |
| DSRV Mystic | Rescue submersible rated to 5,000 ft, launched 1970, never used in a real rescue | Little meaning without sunk boats to reach | [S] Wikipedia; FAS |
| Bushnell's Turtle; Losharik AS-31 | | | not yet sourced |

## The catalogue: land, air and odd

| Vehicle | What it was | Game idea | Gap | Source |
| --- | --- | --- | --- | --- |
| M3 Amphibious Rig | GDELS' bridging and ferry vehicle: one two-bay rig ferries a Class 70 tank; eight rigs make a 100 m bridge in about 15 minutes; in service from the mid-1990s | A vehicle that becomes a bridge or ferry | open; hard in VF (a bridge is terrain) | [S] Wikipedia, Army Technology |
| Progvev-T, Krupp Räumer S, Sherman Crab | Jet blast, 130 t of wheels, and a flail against mines | A mine clearer for Combat Extended's and the game's traps | open | [S] Jalopnik, Tankhistoria |
| Hobart's Funnies | Churchill AVRE petard and fascine, Bobbin carpet layer, ARK ramp tank, Crocodile flamethrower, Sherman Crab | An engineering tank family: the Bobbin as a road layer, the ARK as a ramp | open | [S] IWM, Wikipedia |
| TOS-1 Buratino | 220 mm thermobaric rockets, 30 tubes (24 on the TOS-1A), on a T-72 | The owner wants thermobarics paired with Atmosphere Overhaul | crowded by type | [S] Wikipedia, GlobalSecurity |
| RKhM-4 | NBC reconnaissance on a BTR-80, radiation metering outside the vehicle | A survey-truck node that reads gases ahead | open | [S] army-guide.com |
| Pamir-630D and TES-3 | A nuclear power station on a MAZ-7960 tractor: 630 kWe, 45% enriched uranium, nitrogen tetroxide coolant, one unit fielded in spring 1986; TES-3 was tracked | A nuclear node for the generator set with Rimatomics | open | [S] Bellona, Sovietologist, RERTR |
| Kharkovchanka | Antarctic cruiser on the AT-T tractor: eight beds, a galley; two reached the South Pole on 26 Dec 1959 | A polar cruiser with an interior | partly (Nomadic Carts, Snow Crawler) | [S] Wikipedia AT-T, Antarctica.gov.au |
| Lun ekranoplan | 73.8 m, 137 t, six Moskit launchers; in service 1987, beached at Kaspiysk from 2020 | Flight over water and flats only | open; VF has no "flat only" air type | [S] Wikipedia |
| Landwasserschlepper, LARC-LX | Amphibious tractors: a river tug that works on land (about 100 built); 60 t of cargo ship-to-shore, 1952 to 2001 | Amphibious haulers | open | [S] Wikipedia, Tanks Encyclopedia |
| Antarctic Snow Cruiser | 1939, abandoned 1940 after spinning its wheels | A cautionary tale for the polar cruiser | | [S] Hagerty |
| Zaamurets, BP-42 | Armoured trains, 1916 and the 1940s | Armoured cars on Trains of the Rim's rails | partly | [S] Wikipedia, Task and Purpose |
| Hiller VZ-1, Williams X-Jet | One-person flying platforms, 1950s and 1980s; judged inferior to helicopters | A scout for one | open, crowded family | [S] Wikipedia |
| Convair XFY Pogo | Tailsitter VTOL fighter, flown 1954 to 1956, grounded as uncontrollable | A fighter that lands in a yard | open | [S] USNI, Wikipedia |
| Fairey Rotodyne, Kamov Ka-22 | Compound rotorcraft: 70 seats planned, cancelled 1962; a missile carrier that set a record in 1961 and killed nine | Heavy VTOL transport | crowded | [S] Wikipedia, migflug |
| Kite balloon | Tethered observation from 1893 (Parseval-Sigsfeld, then Caquot); a radar balloon gave the first warning of Iraq's advance in 1990 | A winch truck that lifts a spotter or a relay | open; radio on the back burner | [S] Wikipedia |
| Menzi Muck | Walking excavator since 1966; legs with wheels | A mountain engineering vehicle | partly (walkers exist) | [S] Wikipedia, Menzi Muck |
| Mobile operating theatre | | D'thorga Medical's equipment on Vehicle Interior Framework | partly | |

## Open questions for the owner

1. **Which first, and in which mod.** By the consolidation rule every vehicle here joins D'thorga Vehicles; the shower
   truck's dependence on Dubs Bad Hygiene argues for an `IfModActive` folder, not a mod. Submarines may be a different
   audience (boat players who want no trucks): a sibling mod, or a part of D'thorga Vehicles?
2. **Amphibious pathing.** Allow a research slice on VF's `Universal` type and path grids before any amphibious Def?
3. **Dive as a state.** Is a dive that hides the caravan and the boat honest enough, or does the category wait for a
   framework feature that does not exist?
4. **Armour.** The recovery vehicle and the landing craft are armoured in reality. Keep them unarmoured utility vehicles
   until armour goes through Combat Extended's rules (agent default)?
5. **Jet fuel.** The decontamination truck's 1947 turbojet: chemfuel at a punishing rate, or a gameplay figure?
6. **The chemical submarine's mass balance.** Kilograms of chlorate per hour submerged, and the game item that is.

## Who chose what

- **Owner (9 Nov 2026):** research and record only; a shortlist of five in full; the themes chemical and medical support,
  amphibious and polar, engineering and recovery; submarines as their own category with a top five; the Workshop tool
  gains collections in the same work.
- **Agent recommendations:** the order within each shortlist; the game ideas in every table; the "dive as a state"
  model; unarmoured by default; the plain names.
- **Agent defaults, owner to revise:** that every vehicle here belongs in D'thorga Vehicles; the landing craft above the
  snow carrier.

## Sources

Secondary sources are Wikipedia's editors unless named. Mirrors and aggregator pages seen in search results are not
listed; where only such a page held a fact, the entry above says "mirror" or "snippet".

- Recovery: [Bergepanzer 2](https://en.wikipedia.org/wiki/Bergepanzer_2) [S]; [Bergepanther](https://en.wikipedia.org/wiki/Bergepanther) [S]; Rheinmetall, [30 years Bergepanzer 3 Büffel](https://www.rheinmetall.com/en/media/stories/2022/30-years-bergepanzer-3-bueffel) [P].
- Decontamination: The War Zone, [Russia uses these crazy antique jet engine-equipped trucks](https://twz.com/24906/russia-uses-these-crazy-antique-jet-engine-equipped-trucks-to-blast-away-chemical-agents) [S]; Armorama, [TMS-65 feature](https://armorama.kitmaker.net/features/6324) [S]; CIA FOIA reading room, [DDA-53C recognition features](https://www.cia.gov/readingroom/document/cia-rdp78t05698a000200040022-8) [P]; [Oil well fire](https://en.wikipedia.org/wiki/Oil_well_fire) [S]; The Drive, [Big Wind](https://www.thedrive.com/news/43217/this-apocalyptic-tank-fights-giant-fires-with-twin-jet-engines) [S]; IFLScience, [Hungary's firefighting tank](https://www.iflscience.com/editors-blog/hungary-once-strapped-two-fighter-jet-engines-onto-a-tank-to-put-out-fiery-infernos/) [S]; Jalopnik, [The ten strangest tanks](https://jalopnik.com/5978658/the-ten-strangest-tanks-ever-built) (Progvev-T) [S].
- Shower truck: [Chemical Troops of the USSR](https://www.wikipedia.com/wiki/Chemical_Troops_of_the_USSR) [S]; army-guide.com, [RKhM-4-01](https://army-guide.com/eng/product1993.html) [S]; the DDA-66 lines are from a Scribd document list seen only as a snippet [S, snippet].
- Landing craft: [Zubr-class LCAC](https://en.wikipedia.org/wiki/Zubr-class_LCAC) [S]; GlobalSecurity, [Zubr program](https://www.globalsecurity.org/military/world/china/zubr-program.htm) [S].
- Snow carrier: GlobalSecurity, [Bv 206](https://www.globalsecurity.org/military/world/europe/bv206.htm) and [specifications](https://www.globalsecurity.org/military/world/europe/bv206-specs.htm) [S]; Silodrome, [Hägglunds Bv 206](https://silodrome.com/hagglunds-bandvagn-bv206-an-amphibious-all-terrain-legend) [S]; Sariel.pl, [ZIL-2906](https://sariel.pl/2022/01/zil-2906) [S]; The Drive, [screw-driven amphibious truck](https://www.thedrive.com/watch-this/14987/this-fully-restored-screw-driven-amphibious-truck-is-practically-unstoppable) [S].
- Cargo submarine: [German submarine Deutschland](https://en.wikipedia.org/wiki/German_submarine_Deutschland) and [Merchant submarine](https://en.wikipedia.org/wiki/Merchant_submarine) [S]; USNI Proceedings, [Deutschland: merchant submarine](https://www.usni.org/magazines/proceedings/1965/april/deutschland-merchant-submarine) (1965) [S]; Scientific American, [A political submarine, 1916](https://www.scientificamerican.com/blog/anecdotes-from-the-archive/a-political-submarine-1916/) [P].
- Chemical submarine: [Ictíneo II](https://en.wikipedia.org/wiki/Ict%C3%ADneo_II) [S]; Amusing Planet, [the world's first powered submarine that ran on chemicals](https://www.amusingplanet.com/2025/07/ictineo-ii-worlds-first-powered.html) [S]; Ships Project, [Resurgam survey 2017](https://shipsproject.org/downloads/2017NASHoltSUBMAPSubmarineResurgam.pdf) and [2018](https://shipsproject.org/downloads/2018SHAHoltResurgam.pdf) [P].
- Seabed crawler: [NR-1](https://en.wikipedia.org/wiki/American_submarine_NR-1) [S]; GlobalSecurity, [NR-1](https://www.globalsecurity.org/military/systems/ship/nr-1.htm) [S]; NAVSEA, [NR-1 inactivation report](https://www.navsea.navy.mil/Portals/103/Documents/TeamShips/SEA21/InactiveShips/Historic/2015/NR1-DOE-Final(2)11-3-15.pdf) [P]; Naval History and Heritage Command, [DANFS NR-1](https://history.navy.mil/research/histories/ship-histories/danfs/n/nr-1.html) [P]; NASA Ocean Planet, [How deep can they go](https://seawifs.gsfc.nasa.gov/OCEAN_PLANET/HTML/ben_franklin.html) [S]; Guinness, [largest deep sea research submersible](https://www.guinnessworldrecords.com/world-records/81085-largest-deep-sea-research-submersible) [S].
- Carrier submarine: [I-400-class submarine](https://en.wikipedia.org/wiki/I-400-class_submarine), [I-401](https://en.wikipedia.org/wiki/Japanese_submarine_I-401), [French submarine Surcouf](https://en.wikipedia.org/wiki/French_submarine_Surcouf) [S]; Air and Space Forces, [Flights from the deep](https://www.airandspaceforces.com/article/0304sub/) [S]; HistoryNet, [The Surcouf](https://historynet.com/?p=13779571) [S].
- Polar submarine: Woods Hole Oceanographic Institution, [Submarines under ice, 1950s to 1960s](https://www2.whoi.edu/site/beaufortgyre/history/submarines-under-ice-1950s-1960s/) [S]; American Philosophical Society, [Nautilus at the Pole](https://amphilsoc.org/exhibits/nautilus/pole.htm) [S]; USNI Proceedings, [Skate breakthrough: North Pole](https://www.usni.org/magazines/proceedings/1959/september/skate-breakthrough-north-pole) (1959) [S]; [USS Skate (SSN-578)](https://en.wikipedia.org/wiki/USS_Skate_(SSN-578)) [S]; Naval Encyclopedia, [Project 941 Akula](https://naval-encyclopedia.com/cold-war/ussr/typhoon-class-submarines.php) [S]; National Interest, [Typhoon-class](https://nationalinterest.org/blog/buzz/typhoon-class-russia-built-biggest-submarine-ever-it-had-swimming-pool-208883) [S].
- Submarine catalogue: [Type XIV submarine](https://en.wikipedia.org/wiki/Type_XIV_submarine) and Naval Encyclopedia, [Type XIV Milchkühe](https://naval-encyclopedia.com/ww2/germany/type-xiv-u-boats.php) [S]; [X-class submarine](https://en.wikipedia.org/wiki/X-class_submarine), [Seehund](https://en.wikipedia.com/wiki/Seehund), warhistory.org, [Operation Source part I](https://warhistory.org/article/operation-source-midget-submarine-attack-on-the-tirpitz-22-september-1943-part-i) [S]; [Flying submarine](https://en.wikipedia.org/wiki/Flying_submarine) and Today I Found Out, [the effort to create a flying submarine](https://www.todayifoundout.com/?p=59682) [S]; Naval Encyclopedia, [Project 705 Lira](https://naval-encyclopedia.com/cold-war/ussr/alfa-class-submarine.php) [S]; [DSRV-1 Mystic](https://en.wikipedia.org/wiki/DSRV-1_Mystic) and FAS, [DSRV](https://man.fas.org/dod-101/sys/ship/dsrv.htm) [S]; Naval History and Heritage Command, [H. L. Hunley](https://www.history.navy.mil/browse-by-topic/ships/submarines/h--l--hunley.html) [P]; Project 1231 from a Wikipedia mirror only [S, mirror].
- Land and air catalogue: [M3 Amphibious Rig](https://en.wikipedia.org/wiki/M3_Amphibious_Rig) and Army Technology, [M3 amphibious bridging](https://www.army-technology.com/projects/m3amphibiousbridging/) [S]; Tankhistoria, [Krupp Räumer S](https://tankhistoria.com/?p=4710) [S]; Imperial War Museums, [The funny tanks of D-Day](https://www.iwm.org.uk/history/the-funny-tanks-of-d-day) and [Hobart's Funnies](https://www.wikipedia.org/wiki/Hobarts_Funnies) [S]; [TOS-1](https://en.wikipedia.org/wiki/TOS-1) and GlobalSecurity, [TOS-1](https://www.globalsecurity.org/military/world/russia/tos-1.htm) [S]; Bellona, [Pamir](https://bellona.org/?p=7213), Sovietologist, [Pamir nuclear power goes on road](https://sovietologist.blogspot.com/2008/08/pamir-nuclear-power-goes-on-road.html?m=1), and RERTR, [Sikorin](https://www.rertr.anl.gov/RERTR33/pdfs/S2-P1_Sikorin.pdf) [S]; [AT-T](https://en.wikipedia.org/wiki/AT-T) and Australian Antarctic Program, [Arctic juggernauts](https://www.antarctica.gov.au/news/stations/davis/2024/26-january/) (Kharkovchanka) [S]; [Lun-class ekranoplan](https://en.wikipedia.org/wiki/Lun-class_ekranoplan) [S]; [Landwasserschlepper](https://en.wikipedia.org/wiki/Landwasserschlepper), Tanks Encyclopedia, [Landwasserschlepper](https://tanks-encyclopedia.com/ww2/germany/Landwasserschlepper.php), [LARC-LX](https://en.wikipedia.org/wiki/LARC-LX) [S]; Hagerty, [The lost 1939 Antarctic Snow Cruiser](https://hagerty.com/media/?p=7814) [S]; [Zaamurets](https://en.wikipedia.org/wiki/Zaamurets) and Task and Purpose, [the story of Zaamurets](https://taskandpurpose.com/news/the-story-of-zaamurets-an-armored-train-that-fought-its-way-across-eurasia/) [S]; [Hiller VZ-1 Pawnee](https://en.wikipedia.org/wiki/Hiller_VZ-1_Pawnee), [Williams X-Jet](https://en.wikipedia.org/wiki/Williams_X-Jet), [Tail-sitter](https://en.wikipedia.org/wiki/Tail-sitter), USNI, [Historic aircraft: tail sitters part II](https://www.usni.org/magazines/naval-history-magazine/2013/july/historic-aircraft-tail-sitters-part-ii-convair) [S]; [Fairey Rotodyne](https://en.wikipedia.org/wiki/Fairey_Rotodyne) and migflug, [Kamov Ka-22](https://migflug.com/afterburner/?p=20037914) [S]; [Kite balloon](https://en.wikipedia.org/wiki/Kite_balloon) [S]; [Walking excavator](https://en.wikipedia.org/wiki/Walking_excavator) and Menzi Muck, [walking excavator](https://menzimuck.com/en/product/walking-excavator/) [S].
- Workshop: the three collections [3497770083](https://steamcommunity.com/sharedfiles/filedetails/?id=3497770083), [3419713592](https://steamcommunity.com/sharedfiles/filedetails/?id=3419713592) and [3185234947](https://steamcommunity.com/sharedfiles/filedetails/?id=3185234947), read through Steam's keyless APIs on 9 November 2026.
