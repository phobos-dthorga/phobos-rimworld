# Parachute airdrops: people, cargo and vehicles from the air (25 Nov 2026)

**Status (26 Nov 2026):** built in D'thorga Framework 0.288.0 and D'thorga Vehicles 0.120.0, with the owner's ten answers: the drop
pass and hover drop, the world-map arrival, the parachutes and rigger's table, the hold, the wind, the raids and the C-17's tie-in; round
three (drop zone markers with smoke and lights, allied supply drops, the sounds) in Framework 0.306.0, art and sounds the owner's picks.
Not yet tested in game (test rows 514 to 517, 542 to 544).

The owner asked on 25 Nov 2026: "The C-17 Globetrotter has the ability to eject cargo and vehicles out of its backside and allow them to
drop to the ground safely via parachute. I wish for you to study this code and [add] it as an ability to our own Framework and where ever
else it would be appropriate. It's something I very much want to make use of with our own vehicles, both current and planned." Then: "If
you find anything else of interest, let me know and study it too", and "We'll probably need paid art for the parachutes, pallets, etc. too".
The aircraft is the C-17 **Globemaster** III; its mod is z3nt3's ([our earlier record](archive/c17-globemaster.md)).

How to read it: **[code]** read in code on 25 Nov 2026 with `scripts/query-code.py` (decompiles stay in `.local/`, never committed, and
are described in our own words); **[P]** a primary source; **[S]** a secondary one; **[I]** our inference; **[A]** an authored gameplay
figure. Owner choices, agent defaults and agent choices are labelled as such.

## Read this first

- **One drop, many aircraft.** Any aircraft whose Def carries `Phobos.VehicleSystems.PhobosAirdropExtension` can drop: people from its
  passenger seats, its cargo in bundles, and vehicles from its hold. The Osprey carries it now; the C-17 does, behind a setting that
  starts **off**; the planned C-27J will.
- **Two ways to drop.** A **drop pass** (any aircraft, fixed-wing too): it crosses the map in a straight line, releases upwind of the
  target so the wind carries everything onto it, and flies out the far edge and home. A **hover drop** (aircraft that hover): it climbs to
  its drop height where it hovers, drops, and comes back down.
- **From the world map**: "Airdrop on" a map flies there and makes the pass; on a tile with no map, people (and vehicles) form a caravan,
  cargo alone becomes a depot site, and cargo dropped on a friendly faction's settlement is a gift.
- **Real parachutes**, packed at a **rigger's table**: a personnel parachute per jumper, a cargo parachute per bundle, a heavy-drop rig per
  vehicle. Each canopy lands as a spent canopy to carry home and repack. No parachute, no drop of that load.
- **Wind has a direction**: Combat Extended's when it is on, else ours (the same rule). Loads drift by the Army's own drift formula,
  compressed to the map's scale (**[A]**, below).
- **The risk is real and on by default** (owner's card): landing injuries at the T-11's measured rate, raised at night, in wind and with
  heavy kit; damage to a vehicle on a hard landing. **A vehicle may drop with its crew inside only with a setting that starts off.**
- **Enemies drop on us too**, under "Raiders arrive by air": their aircraft is a real target on its pass, and with Combat Extended its
  CIWS shoots at their parachutes (and the enemy's at ours). A shot-down load loses what it carried (CE's own handling).
- **Gotchas found in others' code**: Vehicle Framework's own paratrooper raids lose their paratroopers if saved mid-drop (its drop classes
  cannot be rebuilt from a save; left alone, owner's card); a plain skyfaller destroys whatever it cannot place, so our vehicles land by
  their own footprint check; the game's wind has no direction without Combat Extended.

## The owner's answers (25 Nov 2026, question cards)

| # | Question | Answer |
| --- | --- | --- |
| 1 | Scope of the first build | **Everything, hold included**: drop, chutes, wind, a drop pass for any aircraft, people + bundles + vehicles, a general vehicle hold |
| 2 | The C-17's own airdrop | **Ours replaces its drop, behind a setting** that starts off; off, the C-17 is exactly as its author made it |
| 3 | Parachutes | **Real, recovered and repacked** |
| 4 | Risk | **Real figures on; crew-inside off**: drift, scatter, injuries and damage on (with a setting to switch off); over the wind limit a warning, not a refusal; crew inside behind its own setting, off |
| 5 | Wind | "It does have direction? Or at least my game does": **Combat Extended's**, confirmed in code; ours where CE is off |
| 6 | A tile with no map | **Caravan, depot or gift** |
| 7 | Enemy paratroopers | **Yes, as a raid arrival**, "and make sure it works with (or rather against) anti-aircraft guns, CIWS, etc. as necessary" |
| 8 | Hover drops | **Yes, from a hover too** |
| 9 | Art | **Paid, US$1.50 cap**; a **rigger's table** of our own; the buttons' icons on the free night run |
| 10 | Report Vehicle Framework's paratrooper save fault | **No, leave it** (noted here only) |

## What was studied

### The C-17's own airdrop [code]

Read from the decompile `.local/decompiled/C17Globemaster-1.7-2026-11-22` (the installed build of 22 Nov 2026).

- A comp on the aircraft switches it between landing ("PTP") and "Airdrop". In Airdrop mode a postfix on Vehicle Framework's
  `LaunchProtocol.GetArrivalOptions` swaps the arrival options for "Airdrop on" a map, for maps that exist only, and asks for a drop cell
  (the game's good-drop-spot test).
- Arriving, it hides the aircraft inside a stand-in object that always crosses the map west to east at a fixed speed, drawn with a
  painted shadow by a map component. A postfix keeps the map from being removed while it is over it.
- Over the drop cell, it drops **all the cargo in one crate** under one canopy (a plain skyfaller with a parachute picture), **each
  passenger** as a plain skyfaller two cells apart, and **each vehicle as a pale ghost** that becomes the vehicle five seconds later,
  facing east: vehicles never fall.
- Then it flies home if its fuel allows (1.15 times the need), else lands at the drop zone; home on the same map, it lands where it took off.
- Its hold is a holder on its comp (`storedVehicles`), up to 77,000 kg, loaded from a zone behind its tail.
- **What it leaves out**: wind, scatter and height; any injury, damage or failure; any cost for a parachute; a drop onto a tile with no
  map. Its messages are hard-coded English. The aircraft cannot be shot at on its pass, as it is not on the map.

### The other studies

- **Vehicle Framework 1.7.2537** (its public source, commit of 17 Nov 2026) [code]: an `AirdropSkyfaller` and `AirdropDef` that draw a canopy
  with rope lines, and a crate that opens. Usable from outside, but its flyover (`DropShip`) and `Paratrooper` classes have no
  parameterless constructor, so a save made mid-drop cannot rebuild them and the paratroopers are lost (the fault in answer 10). Nothing
  of it drops vehicles. Its only user is a raid arrival.
- **The game's drop pods** [code]: an arrival on a tile with no map forms a caravan (`TransportersArrivalAction_FormCaravan`), and world
  pawns are taken out of the world before they land (`DropPodUtility.MakeDropPodAt`). A plain skyfaller destroys what it cannot place
  (`Skyfaller.Impact`), and hits roofs fifteen ticks before landing unless its Def says not.
- **Combat Extended** [code]: a map component (`WeatherTracker`) keeps a wind direction that wanders up to five degrees each rare tick toward
  a random target, and a strength on the Beaufort scale of three times the game's wind speed, at most nine. Its vector points downwind (its
  fire spreads along it). Its **CIWS** shoots at any skyfaller whose class is also an active transporter, leads it by the skyfaller's own
  draw formula, and only when what it carries is hostile; when one is shot down, CE spills what it held where it was hit, killing people and
  damaging items. CE gives every skyfaller built on `SkyfallerBase` that handling by its own patch.
- **The UH-60 Black Hawk** [code]: its "airdrop" lowers people on ropes from a hover, as our own ropes already do; nothing to take. It does
  fly the helicopter home before a map is removed under it, a safeguard worth having.
- **VTOL Vehicle and the fixed-wing framework** [code]: each holds an empty `CanAirDropNonPilotCrew` returning false: room left, never built.
- **Atmosphere Overhaul** (off in the owner's game) [code]: the only wind drift found: a saved path, landing damage by energy with a cap,
  and things blown off the map kept and handed back later.
- **Real practice**: the sources are in [the figures](#the-figures).

## The four questions

| Question | Answer |
| --- | --- |
| What it lets the player do | Put people, supplies and vehicles anywhere an aircraft can reach, with no strip and without landing: reinforce a fight, supply a camp, start a caravan far away, or gift supplies to a friendly settlement |
| What it stops them doing | Dropping without packed parachutes; getting anything back from the drop zone by air; dropping exactly on a spot in strong wind; keeping people safe from a bad landing, a night jump or air defence |
| How it changes play | Parachutes become a stock to make, recover and repack; the wind decides where a drop goes; a drop zone is chosen for open ground, distance from enemies and the wind; an aircraft is in danger on its pass |
| What outside it notices | **Enemies**: they see the aircraft on its pass and shoot at it, and their CIWS shoots at descending loads; enemy raids arrive the same way. **Factions**: a gift drop raises goodwill. **The map**: spent canopies lie where they landed; roofs and water are avoided. **People**: a jump makes a record, a thought, and sometimes an injury. **Letters**: a raid by parachute, a gift received |

## The design

### Wind (Framework core)

- `PhobosWind.Of(map)` gives a **downwind unit vector** and a **speed in knots** (agent choice). With Combat Extended active it reads CE's
  `WeatherTracker` by name (its `WindDirection` vector and its Beaufort strength); without CE, a map component of ours keeps a direction by
  CE's own rule (up to five degrees each rare tick toward a random target), and the strength comes from the game's wind speed by CE's
  mapping, so both read the same way.
- Beaufort to knots: the midpoint of each Beaufort band (0: 0; 1: 2; 2: 5; 3: 8.5; 4: 13.5; 5: 19; 6: 24.5; 7: 30.5; 8: 37; 9: 44) **[S]**
  (the bands are the World Meteorological Organization's; the midpoint is our choice **[A]**).
- Indoors (under a roof) there is no wind, as CE says.
- Gases and fire may read the same wind later; nothing else changes now.

### What drops, and how

| Load | Real basis | Parachute | Planned height | Minimum | Descent | Drift constant K | Wind limit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A person | Static-line jump, T-11 | Personnel parachute | 1,000 ft **[P]** | 800 ft (tactical training) **[P]** | 19 ft/s **[S]** | 3.0 **[P]** | 13 kt **[P]** |
| A bundle of cargo | Container Delivery System, A-22 bag, G-12 | Cargo parachute | 600 ft **[P]** | 400 ft **[A]** (the G-14's 300 to 400 ft is the nearest found **[P]**) | 27.6 ft/s at 2,200 lb **[P]** | 1.5 **[P]** | 13 kt **[P]** |
| A vehicle | Heavy drop, Type V platform, G-11 cluster | Heavy-drop rig | 1,100 ft **[P]** | 700 ft (G-11's lowest) **[P]** | 27.6 ft/s **[A]** (G-11's rate not found; the G-12's used) | 1.5 **[P]** | 13 kt **[P]** |

- **A bundle** takes up to 1,000 kg of cargo (the A-22's 2,200 lb **[S]**, rounded **[A]**); stacks are packed into bundles in the order they
  lie in the cargo, each needing a cargo parachute. A heavy-drop vehicle needs one rig, whatever its size; its weight must be within the
  aircraft's `heaviestPlatformKg`.
- **Drift**: D metres = K × A × V, with A the height in hundreds of feet and V the wind in knots, the US Army pathfinder handbook's
  computed-air-release-point formula **[P]**. The forward throw from the aircraft is left out (**simplification**). **On the map the drift is
  scaled by 0.2** (`airdropDriftShare`, **[A]**): our flight draws 1.5 m a cell and real drift is too long for a map (240 m at 8 kt from
  1,000 ft would be 160 cells). The targeter shows both the real metres and the cells.
- **Scatter** around the drifted point: a random spread of a tenth of the drift plus a cell and a half (**[A]**; no unguided miss figure
  was found that fits; the NRC's model of 413 m for a 2.6 t load from a C-130 **[P]** is for a far higher release).
- **Descent time on the map**: the real seconds at the drop height, times 0.5 (`airdropDescentTimeShare`, **[A]**), as RimWorld runs time
  faster than real (a 1,000 ft jump: 52 s real, about 26 s on the map at normal speed).
- **People leave one a second from each jump door** (`jumpDoors`; about one a second per door is quoted by secondary sources only **[S]**);
  bundles one every half second down the ramp (**[A]**); one vehicle per platform position (`platforms`, the C-17's one-pass sequence
  **[P]** taken as one at a time **[A]**). So a stick spreads along the line of flight, as a real one does.

### Landing

- **People**: the T-11's 5.3 injuries per 1,000 jumps **[P]**, times 1.76 at night **[P]**, 1.55 in 6 to 11 kt and 1.88 at 11 kt or
  more **[P]**, 2.6 with heavy kit (gear and inventory over 25 kg, **[A]** threshold) **[P]**, and twice that for someone's first five jumps
  (**[A]**: no sourced figure for novices). An injury is a blow to an ankle, a foot or a leg (most of them) or the head (**[A]** split;
  "the head and the ankles get the most injuries" **[P]**), of authored size. Over the wind limit the wind factor goes on rising (**[A]**).
- **Too low** (below the minimum): the canopy has less time to slow them; below 150 ft (**[A]**, about a static line's opening) it does not
  open in time and they fall as from that height (the ropes' fall rule, `LineRule.FallHarm`).
- **Cargo**: bundles land and spill their contents beside a spent canopy; a hard landing (2 %, **[A]**: the one malfunction figure found was
  garbled) damages what was inside.
- **Vehicles**: each lands on its platform, wearing its wheels or landing gear lightly (Framework's `WearLanding`); a hard landing (2 %,
  **[A]**) does the sling's fall damage for 10 m (**[A]**). It lands on a clear footprint near its point (never on another thing, never under
  a roof), facing the way it was loaded. **With the crew-inside setting on** (Kentavr, 5 Jan 1973; Reaktavr, 23 Jan 1976 **[S]**), its crew
  stays aboard and each takes a person's landing chance three times over (**[A]**), and the sling's harm to people aboard on a hard landing.
- **Roofs, water and edges**: a load never punches a roof. Drifting over a roof, deep water or off the map, it comes down on the nearest
  open, standable cell (**[A]**), with the injury chance doubled for people (**[A]**).
- **What is left behind**: every canopy lands as a spent parachute item; a vehicle's platform as a spent platform. Carried home and
  repacked at the rigger's table, they become parachutes and rigs again, for less than new.
- **Records and thoughts**: each person landing notes a jump (`Phobos_Jumps`, a record), and gets one thought: "Jumped from an aircraft"
  (+4 mood, a day; **[A]**), or "Hard landing" (-4, a day) after an injury instead.
- **A vehicle's spent platform stands for its whole rig**, canopies included: refurbished, it is a whole rig again, so no spent canopies
  are left beside it.

### Dropping from our flight

- **A drop pass** (Framework, `CompPhobosFlight.Drop.cs`): the player picks the drop point; the aircraft works out the release line from
  the wind (release point = target less the drift), flies in over the edge on the side it came from, levels at the pass, drops people,
  bundles and vehicles in that order along the line, and flies on out of the far edge, then home (or to where it was going) through
  Vehicle Framework's world flight. Over the map it is drawn at its usual flight height; its loads fall from their drop heights (**honest
  simplification**: our flight draws one height).
- **A hover drop** (aircraft that hover, owner's card): a "Drop here" button. It climbs to its drop height (the climb taking the time it
  would, not drawn higher), drops in order through its jump doors and ramp, and comes back down. **"Drop low"** drops from where it hovers,
  with a warning that it is below the minimum.
- **Who goes**: everyone in a passenger or crew chief's seat who is not downed, each needing a personnel parachute from the aircraft's
  cargo; the pilots stay. **What goes**: the cargo except parachutes, rigs and people. **Which vehicles**: those in the hold, each with a
  rig. Missing parachutes hold that load back, and the drop says what stayed.
- **What it refuses, and why**: no parachutes; nothing to drop; the drop point under a thick roof or off the map; not flying; busy at its
  lines. **What it warns of**: wind over the limit, night, below the minimum.

### From the world map

- **"Airdrop on" a place** appears among the arrival options of an aircraft with the extension and parachutes aboard (a postfix on
  Vehicle Framework's `GetArrivalOptions`, as the C-17 and our sortie already do). On a map: the targeter opens on it for the drop point,
  showing the wind arrow and where the loads will drift; the aircraft arrives and makes the pass, then flies home.
- **A tile with no map**: the drop happens over the tile. People with or without vehicles form a caravan there (Vehicle Framework's
  vehicle caravan), with the cargo; cargo alone becomes a depot site to collect (the sling's wreck site, which holds items since
  Framework 0.199.0); **onto a friendly faction's settlement**, cargo is given as a gift for goodwill (the game's own gift rule), like the
  Berlin Airlift's "Little Vittles" (over 23 tons of sweets on 250,000 handkerchief parachutes, 1948 to 1949 **[S]**). People are not
  dropped on another faction's settlement (agent choice: the game has no way for them to arrive there unasked).
- The aircraft then flies home, or lands at the drop zone if it is short of fuel, as the C-17 does.

### The hold (MV5)

- A comp of ours, `CompPhobosHold`, on any vehicle Def: it holds vehicles off the map in a holder of its own, saved, found by the game
  through our `GetChildHolders` postfix, weighed as carried load (the C-17's hold read the same way since 0.254.0).
- **Loading**: a "Load into hold" order on the carrier, then click a vehicle of ours beside its ramp (within a few cells of the back
  of the carrier, **[A]**); it is taken aboard by its driver, who joins the carrier's seats. **Unloading**: "Unload hold" puts each vehicle
  out behind the ramp, facing away.
- The limits proposed by the read of 18 Nov 2026 (agent defaults then, kept): nobody aboard a held vehicle (crew in the carrier's seats);
  the player's vehicles only; no nesting (nothing slinging, towing, holding or being towed, and no aircraft); a size limit in cells
  (`maxSize`) and a weight limit (`maxKg`).
- **The Osprey** carries small vehicles internally in reality (the Internally Transportable Vehicle was made to fit its cabin **[S]**): its
  hold takes one vehicle up to 2 by 4 cells and 3,500 kg (the owner's answer on the Decisions page, 26 Nov 2026; it was 2 by 3 and
  2,500 kg), and it drops one of up to 3,500 kg on a platform (the owner, the same day).

### Parachutes and the rigger's table

- Items (all in the Framework's vehicle part): **personnel parachute**, **cargo parachute**, **heavy-drop rig**; **spent parachute**,
  **spent cargo parachute** and **spent drop platform** after landing.
- **The rigger's table** (a long packing table) packs new parachutes and repacks spent ones, and rigs and refurbishes platforms. Real
  riggers pack at least two T-11s an hour **[P]** (Army news, from a search summary): the repack's work is set from that (**[A]** in
  game time).
- Costs and research are authored and listed in the Defs, for the owner's review.

### Raiders by parachute

- A raid arrival of ours, under the existing "Raiders arrive by air" setting: an enemy aircraft with the extension crosses the map on a pass
  upwind of our base and its raiders jump, landing scattered as ours do. Its aircraft is an ordinary target on its pass, as on every pass.
- **Air defence**: every gun that can hit a vehicle can hit the aircraft on its pass (our flight keeps it on the map). With Combat Extended,
  its CIWS shoots at the descending loads that carry enemies, through our faller being both a skyfaller and an active transporter; shot down,
  CE spills and kills as it does for drop pods (**owner's answer 7**). The Dead Man's Switch's tracks already see our aircraft between
  places; whether its on-map turrets, and InterRim Ballistic Missile's CIWS, engage the aircraft on its pass or our falling loads is read
  during the build and recorded below.

### The C-17 (Vehicles, behind a setting that starts off)

- **"C-17 drops on D'thorga's airdrop"** (owner's answer 2). On: the C-17's own "Airdrop on" option is replaced by ours; its hold
  (`storedVehicles`, read by name) is dropped as ours is, each vehicle on a rig; it flies our pass only while it makes one (its take-off,
  landing and world flight stay its author's). Off: nothing of ours touches its airdrop. As the rule for other authors' aircraft on our
  flight asks (MV12, owner, 19 Nov 2026), its setting text and log note say that faults while it flies on ours are D'thorga's.

## Risk (agent's grades, for the owner)

| Behaviour | What the player can lose | Grade | Setting |
| --- | --- | --- | --- |
| Landing injuries, drift and vehicle damage | A person hurt (rarely), cargo damaged, a vehicle worn; all visible ahead in the wind and the warnings | Moderate | "Airdrop landings can hurt and damage", **on** (owner's answer 4) |
| A vehicle dropped with its crew inside | The crew hurt, rarely killed, on a hard landing | High | "Vehicles may drop with their crew inside", **off** (owner's answer 4) |
| Enemy paratroopers | Raids arriving anywhere upwind of the base | As raids by air | "Raiders arrive by air" (existing) |
| The C-17 on our drop | The C-17's behaviour on its pass | Moderate | "C-17 drops on D'thorga's airdrop", **off** (owner's answer 2) |

## Other factions' vehicles

- **In a raid on us**: an enemy aircraft with the extension may drop its raiders on a pass (above); its loads drift by the same wind.
- **At a base we attack**: aircraft standing there are ordinary; nothing drops unless one is ours.
- **One we capture**: it drops as ours do once it has parachutes.

## Save safety and cost

- Everything falling is a spawned thing of ours with its contents in its own holder, saved; a load always lands, never vanishes: what
  cannot be placed is put on the nearest standable cell, and a vehicle that finds no clear footprint within 25 cells goes on the nearest
  footprint without one, wiping only plants and filth (**[A]**).
- A map removed under an aircraft on its pass: as any aircraft of ours on the map (fault-read F2), it comes back grounded; falling loads
  land at once where they are.
- Cost: nothing runs unless an aircraft is dropping or a load is falling; each falling load does a few sums a tick. The wind is a rare tick.

## Art (paid, US$1.50 cap)

Nine in-world pictures, two tries each: an open canopy seen falling (one picture, scaled, for people, bundles and a three-canopy
cluster); a cargo bundle; a drop platform beneath a vehicle; a spent canopy; a packed personnel parachute; a packed cargo parachute; a
heavy-drop rig; a spent drop platform; the rigger's table. The buttons' icons (drop, drop low, load into hold, unload hold, airdrop on)
go in the local night run, with confirmed existing icons of ours until then.

## Follow-ups from the study (25 Nov 2026; Framework 0.293.0)

The owner asked "Was there anything else interesting in the source(s)?" and chose all four follow-ups offered, with one condition: the packer's
skill rule "gated by a setting, disabled by default".

| Follow-up | What was built | Real basis or reason | Who chose |
| --- | --- | --- | --- |
| A hold destroyed with no room for its vehicles | They wait at a depot on the world map beside the place, as a lost convoy does; before, they stayed in a destroyed holder | The C-17 kills and destroys in the same case | Owner's card; depot is the agent's choice |
| A map removed under an aircraft in the air | It hands over on its journey home if it has one; a raid's aircraft goes home; otherwise it comes down on the world map as a caravan at that place | The UH-60's own safeguard (a prefix on Game.DeinitAndRemoveMap that flies it home) | Owner's card; caravan is the agent's choice |
| The packer's skill | Packed parachutes and rigs carry the game's quality from their maker's Crafting skill; with **"Packers' skill decides whether canopies fail"** on (off by default), a canopy fails at 3 % (awful) to none (legendary): a jumper lands on the reserve at ten times the risk; cargo and vehicles land hard (a vehicle as from 20 m) | The rigger's pledge: a rigger must be ready to jump any parachute they packed. Rates **[A]**: no sourced malfunction rate fits | Owner's card, setting off |
| Ankle braces | Worn on the feet under anything else, made at a tailoring bench (15 cloth, 4 steel, no research); a jumper wearing them has the risk divided by 1.84 | Knapik and colleagues (2016): the risk without a brace is 1.84 times **[P]** | Owner's card; recipe **[A]** |
| A GPS-guided cargo parachute | Made or repacked at the rigger's table after microelectronics (3 or 2 components); used before plain cargo parachutes; steers its bundle onto the point whatever the wind, within 2 cells | JPADS 2K: up to 2,200 lb, within about 150 m **[P, US Marine Corps]** | Owner's card; figures **[A]** |
| The ramp's loading area | Drawn in cyan behind the ramp while a carrier with room is selected on the ground | The C-17's own zone | Owner's card |
| Bundles de-rigged | A bundle lands as a landed cargo bundle (the bundle's own picture), marked for the game's "Open" order when it is the player's; a colonist de-rigs it in about 15 game minutes and the cargo comes out beside it | Real A-22 bundles are de-rigged by hand | Owner's card; time **[A]** |

**Air defence against drops** (the read owed since 25 Nov 2026): see [the next section](#air-defence-against-drops).

## Air defence against drops

Read in code on 25 Nov 2026 **[code]** (the read the design owed: "make sure it works against anti-aircraft guns, CIWS, etc.").
Two things are in the air during a drop: **the aircraft on its pass** (a vehicle spawned on the map) and **each falling load** (a
skyfaller and active transporter). Nothing below is tested in game.

| System | The aircraft on its pass | Falling loads | Done (Framework 0.293.0, Patches 0.43.0) |
| --- | --- | --- | --- |
| **Combat Extended** CIWS (its KPV; under CE, DMS's GateKeeper and DMSE's Vulcan and Dawnblade) | As an ordinary ground target only: no CIWS verb aims at a vehicle | **Yes**: loads carrying enemies (people or vehicles); bundles of cargo are never shot. Our three loads are listed, ticked, in each CIWS's target dialog | A vehicle's load takes 80 hits to bring down (CE's skyfaller default 22 for a jumper), by an XML patch only with CE; the drawn height is kept inside the map, since CE spills a downed load at its drawn point |
| **InterRim Ballistic Missile**'s CIWS | Only in its ground mode, as any hostile target | **Marked "intercepted" but nothing happened**: its effect runs in the game's skyfaller impact, which our loads never call | A hook in the Framework (`SkyfallerInterception`), filled by the IRBM tie-in with IRBM's public check: an intercepted load's canopy is shredded half the time (its people come down downed, as IRBM leaves drop-pod raiders) or holed (the reserve's risk; a vehicle lands as from 20 m) |
| **The Dead Man's Switch - Expedition** and Fortified | As an ordinary ground target (its flak and field air defence) | Only through the CE verbs above; its own long-range system takes skyfallers only from the game's skyfaller maker | Left alone. **Keep spawning loads with GenSpawn**: through the game's skyfaller maker DMSE would take hostile loads off the map while its radar is up |
| **Vehicle Framework** | No on-map air defence (its flak is on the world map) | No | Nothing to do |

Your own CIWS also shoots at your own drop if it carries a hostile person (a prisoner, say): CE's rule.

**The two extras the owner asked for (26 Nov 2026; Framework 0.297.0, Patches 0.44.0):**

- **A softer outcome when Combat Extended's CIWS brings a load down.** CE's own kills everyone inside, a vehicle too. Ours brings the
  load down where it is, its canopy shredded: its people downed (as InterRim Ballistic Missile leaves drop-pod raiders), a vehicle
  landing as from 20 m, cargo damaged. Done without a reference to CE's assembly: a prefix, by name, on CE's
  `CompCIWSImpactHandler_Skyfaller.OnDestroying` in D'thorga Patches' CE tie-in, which hands our loads to the Framework
  (`ShotDownFallers`) and leaves every other skyfaller to CE.
- **InterRim Ballistic Missile's CIWS fires at enemy aircraft** in the air over the map, behind **CIWS fires at enemy aircraft**
  (D'thorga Patches, on: it never aims at the player's aircraft). With nothing else to shoot, it takes the first enemy aircraft the
  Framework names (`AirTargets`); each hit IRBM rolls does 4 points of bullet damage through the aircraft's own parts (**[A]**: IRBM
  rolls a hit most ticks it fires, so a burst's worth each).


## Round three (26 Nov 2026; Framework 0.306.0)

The owner asked "Anything more we should continue with, other than testing?" and chose four: the C-27J (its own record,
[vehicle-c27j.md](vehicle-c27j.md)), and the three below, by cards the same day.

### Drop zone markers

A pathfinder's signal panel (the US Army's VS-17, about 1.8 by 0.6 m **[S]**), built in the Motor pool tab after vehicle parts (15 cloth,
2 steel **[A]**). **Owner's card: "Aim point and less scatter"**: a drop aimed within 15 cells of one scatters half as much **[A]**; the
drop targeter opens on the first marker laid; an ally's supply drop aims at it. Its picture is paid: by the owner's card after smoke was
added, the panel with a violet smoke pot and a beacon lamp staked beside it, a thin wisp only (the game draws the moving smoke); the
owner picked try 4 of four (tries 1 and 2, panel only, superseded).

**Owner's card, the same day: "Panel plus smoke"** (the owner asked whether a marker should not be a smoke signal: real drop zones use
both, panels as the standing marker and coloured smoke popped for the run-in, which confirms the spot and shows the crew the wind at the
ground **[S]**). By day, while we or an ally drop on a point near it, the marker puts up violet smoke (one of the M18 grenade's colours
**[S]**) carried downwind: looks only, never the game's blinding smoke. Our crews already let go upwind of the point by the drift worked
out from the wind, as a real crew does, so the smoke's gain is a truer wind: by day the scatter near a marker is cut to 0.7 of the half
again (0.35 of the usual) **[A]**. By night its small lights show the spot (no power needed, battery beacons **[A]**) and the scatter stays
at half. A raid's crew gains nothing from our markers.

### Allied supply drops

**Owner's cards: a comms console request, and the player chooses the kind.** An industrial-or-better ally (the game's own "must be an
ally") offers **Request a supply drop** for 15 goodwill (military aid costs 25), once a day per faction **[A]**: food (survival packs, or
pemmican from a lower-tech ally), medicine (industrial) or materials (steel and components), about 800 silver's worth **[A]**. Two to four
hours later **[A]** its aircraft (an aircraft of ours fit for the faction, the Osprey today) comes in over an edge, makes a pass over the
first drop zone marker (else the game's trade drop spot), and leaves by the far edge; the bundles land strapped up for colonists to
de-rig. With no aircraft to send, they come down from one unseen overhead. A letter says what is coming. **Allies' fighters** already
come by air through the game's own "request military aid", which our air assault serves.

### Sounds and the review list

Three sounds, made on the processor beside the game (`assets/phobos-sounds/requests/night-15-airdrop.json`): a canopy opening (S150), a
bundle landing (S151) and a platform landing (S152), shipped with the owner's picks of 26 Nov 2026 (151, 152 and 152). The authored figures the
owner may want to revise (the drift share, the fall-time share, the Osprey's hold, the CIWS hit) are on D'thorga Companion's Decisions
page (`config/decisions.json`).

## What a next game would need

The wind (a direction and a speed in knots) and the parachute rule (`ParachuteRule`: drift, descent, injury and minimum height) are
game-free and live in the Framework core; a next game needs its own falling thing, its own aircraft and its own wind source.

## The figures

- US Army, Sabalauski Air Assault School, *Pathfinder student handbook* (Oct 2022), reproducing the USAF airdrop tables: wind limits,
  drop heights, airspeeds, the drift formula and constants **[P]**:
  <https://home.army.mil/campbell/9016/6999/4960/PFDR_DAPS_01_october_2022.pdf>
- Knapik and colleagues, *Journal of Athletic Training* (2016), injuries with the T-10 and T-11 and their risk factors **[P]**:
  <https://pmc.ncbi.nlm.nih.gov/articles/PMC5224738>
- US Army, on the T-11's injury rate **[P]**: <https://www.army.mil/article/120614/injury_experts_evaluate_safety_of_new_army_parachutes>
- Mills Manufacturing, G-11 and G-12 parachutes **[P]**: <https://www.millsmanufacturing.com/products/g-11-parachute/> and
  <https://www.millsmanufacturing.com/products/g-12-parachute/>
- US Army TM 10-1670-268-20P, Type V platforms **[P]**:
  <https://parachutemanuals.tpub.com/TM-10-1670-268-20P/css/TM-10-1670-268-20P_31.htm>
- US Air Force fact sheets, C-17 and C-130 **[P]**: <https://www.af.mil/About-Us/Fact-Sheets/Display/Article/1529726/> and
  <https://www.af.mil/About-Us/Fact-Sheets/Display/Article/1555054/c-130-hercules/>
- Wikipedia, T-11 parachute, BMD-1 (the 1976 Reaktavr drop) and Vasily Margelov (the 1973 Kentavr drop) **[S]**
- Wikipedia, Gail Halvorsen (Little Vittles) **[S]**
- NRC, airdrop accident model (ML20065F294) **[P]**: <https://www.nrc.gov/docs/ML2006/ML20065F294.pdf>
- Combat Extended (the CE Team), `WeatherTracker`, the CIWS verbs and `Skyfaller_Utility`, read from its public source **[code]**
