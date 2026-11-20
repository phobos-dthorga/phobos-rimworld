# Close Air Support's A-10 and Su-25 in the fold (21 Nov 2026)

**Status (21 Nov 2026):** built, not yet tested in game: Framework 0.203.0 (the catch-all flight reading and our own dodge), Vehicles
0.91.0 (each jet's engines, avionics, cruise and cabin), Patches 0.29.0 (parts lists, parts bills, fabricator recipes, the shared rearm
fix and the realistic handling setting). Test row 299. Real bomb weights, a gentler loss of control for the jets' manual back-up, and the
Harrier's own cost as a parts bill are not built (see "Not built"). **Since Vehicles 0.93.0 both jets start as their early versions**
(the A-10A, the first Su-25), which changes some starting figures below: see "Starting fits since Vehicles 0.93.0".

## Read this first

- **What it is:** Close Air Support: A-10 Warthog & Su-25 Frogfoot (`potemkinvillager.CloseAirSupport`, Workshop 3842292130, released
  19 Nov 2026) by potemkinvillager, the Harrier's author, gets the treatment the Harrier got. The owner added it on 21 Nov 2026.
- **The owner's choices (21 Nov 2026, question cards):** the full treatment for both jets; realistic handling **as a setting, off by
  default**; **build a dodge** for jets whose own mod has none; and for vehicles not yet taken in, **recognise the framework** (keep the
  generic systems for cars, trucks and trains; read the flight frameworks' aircraft correctly).
- **Its author is active** (replies on its page on 19 and 20 Nov 2026; seven comments, none reporting a fault, read with
  `workshop-research.py report`). They plan to port the jets' cannons to the Harrier ("Harrier GAU-12, here we come"), and their source
  holds Maverick and air-to-air work not yet in any shipped Def. **When either lands, look again**: a new turret on the Harrier may need
  its Combat Extended ammunition at the fabricator, and new loads may need the parts lists.
- **It ships the same fixed-wing framework as the Harrier** (the identical `FixedWingAircraftFramework.dll`), loaded once whichever
  of the two is active, so our fixed-wing flight profile, the rearm fix and the on-map flight reading serve both.
- **Not yet tested in game.**

## What it does for the player (the four questions)

| Question | Answer |
| --- | --- |
| What it lets the player do | Fly the A-10 and Su-25 with real engines that run hot on one engine, their real avionics (flares, warning receivers, oxygen, night sight on the A-10), and a dodge in the air that flares improve; build them from real parts; make their bombs at the ammunition fabricator; and, by a setting, fly them at their real relative speeds |
| What it stops them doing | Flying an A-10 at nine-tenths of a Harrier's speed (with the setting); losing nothing when an engine goes; building a jet from car wheels and suspension |
| How it changes play | A lost engine becomes a reason to go home, not a five per cent slowdown; flares are a resource to spend over a defended target; the A-10 is the tough, slow one and the Su-25 the quicker one that dodges more; the bill asks for turbines and landing gear |
| What outside the jet notices | Raiders' and enemy bases' jets of these types dodge the player's fire the same way and carry the same flares; another mod's air defence (The Dead Man's Switch - Expedition) sees each jet's real size, speed and height and is fooled by its flares; the colony's haulers keep its dispensers loaded; the fleet board and the test list record it |

## Before: what the generic set did wrong

Every fuelled aircraft with no loadout of its own got the generic aircraft loadout, whose stock flight profile reads Vehicle Framework's
`inFlight` flag. That flag is set on a map only while an aircraft leaves it; the fixed-wing and VTOL frameworks keep their aircraft on the
map while they fly. So the two jets read as parked for a whole sortie (count by `scratchpad` script over `query-defs.py`, 21 Nov 2026: of
the owner's 100 vehicles, 40 had only the generic set, and the two jets were the only aircraft among them).

**Fixed (Framework 0.203.0):** a loadout selector can name code classes (`thingClasses`, by full name, matching any class a design is
built on). Two catch-alls at rank 1 recognise `FixedWingAircraftFramework.VtolVehiclePawn` (fixed-wing jet profile, 6,000 m cruise,
authored) and `VTOLVehicle.VtolVehiclePawn` (helicopter profile, two turboshafts through a gearbox, as the Hind's). Named loadouts still
win.

## The jets' figures

Researched 21 Nov 2026. **[P]** primary, **[S]** secondary, **[A]** authored. The A-10's primary source is its flight manual, *T.O.
1A-10A-1* (USAF, 15 Mar 1988), read from a public scan hosted by the
[Internet Archive](https://archive.org/details/T.O.1A10A1FlightManualA10ASerno7500258AndSubsequent15031988) (the host, not the author);
the USAF's [A-10 fact sheet](https://www.af.mil/About-Us/Fact-Sheets/Display/Article/104490/a-10c-thunderbolt-ii/) refused automated
reads, so figures from it came through search extracts and are marked so. No primary source was found for any Su-25 figure.

| Figure | A-10 | Su-25 |
| --- | --- | --- |
| Engines | 2 × GE TF34-GE-100, 40.3 kN each **[P, [GE Aerospace datasheet](https://www.geaerospace.com/sites/default/files/datasheet-TF34.pdf)]** | 2 × R-195, 44.2 kN **[S, [Wikipedia](https://en.wikipedia.org/wiki/Sukhoi_Su-25); Airforce Technology]**; 42.2 kN **[S, [Russian Wikipedia](https://ru.wikipedia.org/wiki/Су-25)]** |
| Maximum continuous | Not found: 0.9 of take-off **[A]** | Not found: 0.9 **[A]** |
| Emergency rating | Fuel-flow override, "15-20 %" more, to 980 °C, not over 15 min **[P, T.O. 1A-10A-1]**; we take 15 % | None found |
| Turbine temperature | Normal band to 830 °C (-100), hot light 835 **[P]**; maximum continuous 800 **[A]** | Not found: the Framework's generic turbofan's **[A]** |
| Idle core speed | 56 % **[P]** | 60 % **[A]** |
| Empty mass | 11,321 kg **[S, [Wikipedia](https://en.wikipedia.org/wiki/Fairchild_Republic_A-10_Thunderbolt_II)]** | 9,315 kg **[S, Russian Wikipedia]**; 9,800 **[S, Wikipedia]** |
| Internal fuel | 4,997 kg: 1,644 US gal of JP-8 at 6.7 lb/gal **[P, fig. 1-8]** | 3,000 kg, 3,660 L **[S, Russian Wikipedia]** |
| Top speed, sea level | 706 km/h **[S]** (the 450 kn often quoted is its airspeed limit **[P]**) | 950 km/h **[S]**; 975 **[S, Wikipedia]** |
| Cruise | 560 km/h **[S]** | 750 km/h **[S, Russian Wikipedia]** |
| Ceiling | 13,716 m **[P via search; S]** | 7,000 m, 5,000 at full load **[S]** |
| Cruising height | Not found: 5,000 m **[A]** | Not found: 4,000 m **[A]** |
| One engine | +200 ft/min, gear down, 40,000 lb, 15 °C **[P, appendix A2]** | Not found |
| Hydraulics | Two independent systems; pitch and yaw revert to cables, roll by pilot selection **[P]** | Two independent systems **[S]**; manual reversion unverified |
| Radar | None **[S]** | None (base aircraft) |
| Warning receiver | AN/ALR-69 **[P]** | SPO-15 "Beryoza" **[S]** |
| Countermeasures | 16 × AN/ALE-40 **[P]** × 15 flares **[S, [Military Periscope](https://www.militaryperiscope.com/weapons/sensorselectronics/electronic-support-measureselectronic-warfare/anale-40/)]** = **240**; "480 flares and 480 chaff" is not supported | "Up to 250" **[S, both Wikipedias]**; 128 raised to **256** after Afghanistan **[S, uncited: Weapons and Warfare]**, plausible, unverified |
| Navigation | INS, GPS from 1999 **[S]**: Full | Doppler and radio beacons **[S]**: Basic (GPS only on the SM) |
| Sight, night | HUD **[P]**; targeting pod on the A-10C **[S]**; goggle-compatible cockpit **[P via search]** | Sight with Klen-PS laser rangefinder **[S]**; no night sensor found |
| Autopilot | Stability augmentation, counted as one **[A, not checked against the manual]** | None found |
| Oxygen | 5 L liquid oxygen converter **[P]** | Demand regulator from 2 km **[S]** |

**Our dodge** (`mapEvasion`, Framework 0.203.0): the Harrier author's 40 % for the AV-8B scaled by top speed against its 1,083 km/h
**[S]**: 0.26 for the A-10, 0.35 for the Su-25 **[A]**, then scaled by flight controls and the warning receiver as the Harrier's is.
**Air defence track** (DMS Expedition's scales): speed 12 and 16 against the Harrier's 18, size 1.8 and 1.6 by span **[A]**.

## Starting fits since Vehicles 0.93.0

The owner's choice of 21 Nov 2026 ([aircraft service-life upgrades](aircraft-service-life-upgrades.md)): each jet starts as its early
version and is brought up with fittings. Where the table above gives the later fit, the jet now starts with the earlier:

| | A-10 (as the A-10A) | Su-25 (as first built) |
| --- | --- | --- |
| Navigation | Basic; full with the navigation set (GPS, 1999) | Basic, as before; full with the navigation set (the Su-25SM) |
| Autopilot | None; the autopilot kit (LASTE, from memory) | None, as before |
| Night sight | None; the night sight kit (the A-10C's targeting pod) | None, as before |
| Engines | TF34s, as before; the uprated engines, offered but never fielded | The R-95Sh, 40.2 kN; the R-195 as a fitting |
| Flare cartridges | 240, as before | 128; 256 with the extra dispensers |

## Realistic handling (the setting)

**D'thorga Patches → Close Air Support → Realistic handling**, off by default (owner's choice), at the next start. It puts each jet's
speeds on the Harrier's scale (the author's 0.30 for the AV-8B's 1,083 km/h) by its real ones, and the A-10 on one engine at the
framework's default. Figures and sources are beside each value in `Handling_CloseAirSupport.xml`.

| Figure | Author | Realistic | From |
| --- | --- | --- | --- |
| A-10 top speed | 0.27 | 0.20 | 706 / 1,083 × 0.30 |
| A-10 cruise | 0.21 | 0.16 | 560 km/h gives 0.155, raised to its attack-run (orbit) speed |
| A-10 on one engine | 0.95 | 0.80 | the framework's default, as the Su-25 has; the manual's marginal climb |
| Su-25 top speed | 0.30 | 0.26 | 950 / 1,083 × 0.30 |
| Su-25 cruise | 0.24 | 0.21 | 750 km/h |

**Risk: low** (agent's grade, for the owner to confirm): a slower jet is over a defended target longer and is hit more, a loss the player
sees coming and chose by turning it on. Take-offs, landings, orbits and attack runs are left alone, since the author's bomb release and
strafe timing are worked out from them. Fuel capacities are left alone too: the Su-25's is generous against the Harrier's (350 against
250, where real internal fuel is 3,000 kg against 3,500), but a smaller tank would strand saved jets above their new capacity; raise it
with the owner if wanted.

## Our dodge

**Risk: none to the player's things**: it only makes shots miss an aircraft in the air, ours or a raider's. While a jet with
`mapEvasion` is airborne over a map (the frameworks' own `IsAirborne`, through `OnMapFlight`), a shot at it misses by that chance as
scaled; a vanilla shot is re-aimed at a cell two beyond its footprint on the far side, a Combat Extended shot passes through, rolled once
per shot. Shots fired over the top and its own are never dodged. An aircraft whose mod has a dodge (Harrier, Hind, Mongoose) carries no
`mapEvasion`, so nothing dodges twice. The salvo button appears on any aircraft with dispensers and `mapEvasion`.

## Other factions' vehicles

- **In a raid on us:** an enemy A-10 or Su-25 gets the same systems and dodge, so the colony's guns miss it in the air as often as the
  player's jets are missed; its flares answer our air defence. Cost: one check when a shot is launched at a vehicle, nothing per tick.
- **Parked at a base we attack:** on the ground, no dodge; its parts can be shot out as any vehicle's.
- **Captured:** keeps its parts' health and its flare magazine (the Framework's countermeasures store: a captured aircraft has some
  cartridges, never a full or empty magazine).

## Parts, bills and bombs (D'thorga Patches 0.29.0)

- **Parts lists:** six parts each (avionics, hydraulics, wiring, landing gear, wing pylons, the gun), fewer than the Harrier's ten on
  purpose: the jets' own hit rule spreads a share of hits evenly over every part with cells, so every added part takes hits from the
  cockpit and engines. No radar part: neither jet has one.
- **Parts bills:** two engines (turbines), four or three fuel tanks, three legs of landing gear, two generators, a battery, wiring, two
  hydraulic pumps and lines, avionics; the blueprints' costs cut to a shell of steel, plasteel and a few components. Construction skill
  10, as the Harrier, Hind and Mongoose.
- **Bombs:** the cluster bombs and FAB-250 at the ammunition fabricator. The A-10's 500 lb bomb is the framework's shared item, which
  the Harrier's patch already handles.
- **Rearming one kind at a time** (temporary patch row 6) now also covers the A-10 and Su-25, which use the same framework code; with
  the Harrier active too, the one patch serves both.

## Not built

- **Real bomb weights under Combat Extended.** The author's cluster bombs weigh 40 and 60 kg there; a real RBK-250 is about 275 kg
  (unverified) and a CBU-87 about 430 kg (202 BLU-97/B bomblets **[S, [Wikipedia](https://en.wikipedia.org/wiki/CBU-87_Combined_Effects_Munition)]**).
  Real weights need recipes that cost that much, from a sourced split between casing and fill, as the Harrier's bomb cases have.
  Proposed for a later batch.
- **A gentler loss of control for the manual back-up.** Both jets keep flying on cables with both hydraulic systems gone (the A-10's
  "adequate for … safe return to base and landing" **[P]**); our flight hydraulics halve flight control whatever the aircraft. A
  parameter on that system would let these jets lose less.
- **The Harrier's own cost as a parts bill.** It still swaps in car parts (D'thorga Patches' older patch); it should move to a bill as
  these jets have.
- **Tools for other players to bring a vehicle in** (owner, 21 Nov 2026: "Later, we can perhaps design tools or something to make it
  easier for other players"): a helper that drafts a loadout and a parts list from another author's VehicleDef, and flags what the
  generic set cannot read. A wish, not designed.
