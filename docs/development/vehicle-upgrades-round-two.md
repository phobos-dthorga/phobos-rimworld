# Vehicle upgrades, round two: what every vehicle has, and a signature feature for each (11 Nov 2026)

**A proposal with an inventory. Nothing here is built, and nothing is tested in game.** The owner asked on 11 Nov 2026 whether to
think about upgrades and unique features for vehicles that already have them and especially for those that do not, naming the
Cherokee, and approved the plan this record follows. The inventory is observed from the owner's mod list; every proposal is
agent-authored for the owner to pick from, and no figure is set yet.

## Read this first

- **The Cherokee already has an upgrade tree in the owner's game.** It comes from VVE - Upgrades Extended and holds five plain
  stat nodes. Its gap is that nothing in it is unique to a twin-rotor cargo helicopter, not that it has nothing.
- **21 of the 75 vehicles have no tree at all:** the 13 of Trains of the Rim, seven of RimThunder's, the bicycle among them, and
  our own submarine test hull.
- **Three layers of upgrade already exist**, so this round adds a fourth idea rather than more of the same: one **signature
  feature** per vehicle, something only that vehicle does.
- **Another author's vehicle keeps its picture and its own tree.** The owner's rule of 11 Nov 2026 rules out overlays on other
  authors' art; this record also proposes leaving their trees alone (agent default, below).
- **Two earlier decisions stand and are not reopened here:** the generator set's sound enclosure was cut by the owner (9 Nov
  2026), and the tipper's tailgate spreader was cut because no code reads what it would move
  ([the tipper record](vehicle-tipper-hauler.md)).
- **Four questions for the owner** are at the end. The first build waits for them.

## What exists today

### The three layers

| Layer | What it is | Reach | State |
| --- | --- | --- | --- |
| Vehicle Framework's upgrade tree | Nodes in an `UpgradeTreeDef`, one tree per vehicle Def | A vehicle has one only if someone wrote it | Ours: six trees. Others: 48 trees from their own mods and two VVE upgrade mods |
| Our fittings, tunes and part grades | Bolt-ons to a part by role, engine and gearing tunes, salvaged to heavy-duty parts ([tuning and upgrades](vehicle-tuning-and-upgrades.md)) | Any vehicle whose parts we have bound to roles | Built from Framework 0.40.0, with costs revised in 0.91.0; not yet tested in game |
| Per-vehicle systems | Loadouts, cabins, real figures, engines, the sling hook | Each vehicle we have patched by name | Built for our six and many of VVE's; mostly not yet tested in game |

From Vehicle Framework's code (our decompile of its 1.6 build, read with `scripts/query-code.py`, in our own words): a node may
hold several upgrades, and the kinds are `StatUpgrade` (a stat or vehicle stat), `VehicleUpgrade` (armour, health and seats),
`TurretUpgrade`, `CompUpgrade` (adds a comp), `SettingsUpgrade`, `SoundUpgrade` and `ActionUpgrade`
(`Vehicles.decompiled.cs:33674-34659`). Our own trees use the first two, behind `PhobosEnforceUpgradeResearch`.

### Every vehicle on the owner's list

Read on 11 Nov 2026 with `scripts/query-defs.py` from Defs as written: 75 `Vehicles.VehicleDef` and 54
`Vehicles.UpgradeTreeDef` among the active mods. **Checked:** the counts, the Def names, and for the Cherokee the patched Def
(the file written 8 Nov 2026), which carries `CompProperties_UpgradeTree` pointing at `VVE_Cherokee_UpgradeTree`.
**Not checked:** that each other tree is attached to its vehicle, and what its nodes move.

| Mod | Vehicles | Trees | Where the trees come from |
| --- | --- | --- | --- |
| Vanilla Vehicles Expanded (23) and its Tier 3 (13) | 36 | 36 | Vanilla Vehicles Expanded - Upgrades (7) and VVE - Upgrades Extended (29); matched by count, only the Cherokee's by name |
| Trains of the Rim | 13 | **0** | |
| D'thorga Vehicles | 7 | 6 | Ours; the submarine test hull has none |
| RimThunder - Roaring Tiger | 5 | **0** | HEMTT, M113, M113(M), M1A2, M48H |
| Frontline Workshop | 4 | 4 | Its own |
| RimThunder - Desert Sabre - Continued | 3 | 2 | Its own; the M10 Booker has none |
| RimThunder - Core | 1 | **0** | The bicycle |
| Mongoose, Hind, Harrier, HUMVEE, L-ATV, MaxxPro | 1 each | 1 each | Their own |

### Our own trees

| Vehicle | Nodes | What they are | Has a signature already? |
| --- | --- | --- | --- |
| Seabed crawler | 10 | Bottoming wheels, air scrubber, two laboratories, listening array, sonar set, two exclusive hull refits | Yes: the refits and the sonar |
| Bulk tanker | 6 | Three tank kinds, a second hose reel, two or three compartments | Yes: compartments |
| Survey truck | 6 | Geophone, radiometric, long-range and powered arrays, large battery, work lights | Yes: the arrays |
| Drill rig | 5 | Diamond bit, RC hammer, tall mast, electric power pack, work lights | Yes: the bits |
| Generator set | 3 | Uprated alternator, cable reel, battery buffer | **No: all three are "more of the same"** |
| Tipper | 3 | Greedy boards, rock body, bulk body | **No: all three change the body's size or speed** |

### The Cherokee

Vanilla Vehicles Expanded - Tier 3's twin-rotor cargo helicopter. What it has in the owner's game:

- **From VVE - Upgrades Extended:** a tree of five stat nodes: cargo capacity, armour, flight speed, fuel consumption, fuel
  capacity (read from the tree's Def as written).
- **From us:** two turboshaft engines with a rotor drive, a cabin, real figures, a loadout, and a sling hook rated from the
  CH-47F's centre hook ([the sling record](vehicle-sling-design.md)). Cherokee Sling, the mod ours stands down for, is not on
  the owner's list, so our sling is the one in play.

## The proposal

### One rule for other authors' vehicles (agent default)

**Leave another author's tree alone, and add our features beside it.** A node of ours inside their tree would sit on their
grid, could collide with their next update, and would put our keys into their saved tree. Instead:

- A vehicle **with** someone's tree gains our signature feature as a **fitting** or a loadout-level feature of ours, fitted
  the way our other fittings are.
- A vehicle with **no** tree may be given one by us, written in our mod's `IfModActive` folder for that mod, with node keys
  prefixed `Phobos`. Vehicle Framework's own design expects this: a tree belongs to one vehicle's Def.
- Either way the picture is theirs and stays as drawn.

### Signature features

Each answers the three questions. "Builds on" names what already exists, because a signature that reuses a built system
costs far less than a new one.

| Vehicle | Signature | Lets the player | Stops the player | Changes how they play | Builds on | To check first |
| --- | --- | --- | --- | --- | --- | --- |
| **Cherokee** | **Water bucket**, a slung load that fills at water and empties over a fire | Fight a fire from the air, far from the colony's hands | Sling anything else on that flight; work without open water in reach | A wildfire near a camp or an outpost becomes a job, not a loss | Our sling hook; fire in the game and in Burns and Smoke | How a slung flight is flown on a map today (sling slice 2 was "next"); what puts a fire out in code |
| Cherokee, second | **Litter kit**, seats swapped for stretchers | Fly a casualty home under care | Carry as many passengers or as much cargo | The helicopter becomes the colony's air ambulance | D'thorga Medical; `VehicleUpgrade` seat changes | Whether a patient can be tended in a vehicle seat at all |
| **Tipper** | **Loading grab**, a small crane behind the cab | Load chunks and stacks from the cells beside the truck with only the driver | Tip while the grab is out; reach past a few cells | One driver does a quarry run alone; the carries at the far end, which the tipper record says still cost, go away | The tip job's placement code; the work-rate pattern of the drill rig | Whether Vehicle Framework's cargo can be filled by our own job without its loading dialog |
| **Generator set** | **Service lead**, a heavy cable to another vehicle | Charge or jump-start a vehicle with a flat battery in the field | Feed the camp at full output meanwhile | A dead battery away from a depot bay is a visit from the set, not a tow | The battery and charge figures ([electric design](vehicle-electric-design.md)); the camp's power rule | What a flat battery does to a vehicle today |
| Generator set, second | **Exhaust heat recovery** | Warm one room or tent beside the parked set | Nothing; it costs a little output | A cold-biome camp runs on the set's waste heat | The game's room temperature system | Nothing new |
| **Trains of the Rim** (13) | A tree of ours for the steam locomotives: boiler and firebox nodes | Trade fuel for pull, and wood for chemfuel | To be set | To be set | [The steam design](vehicle-steam-design.md) | Whether we bind steam parts on them yet; the mod's state (below) |
| **RimThunder Roaring Tiger** (5) | Nothing proposed yet | | | | Our fittings already reach any vehicle with bound parts | Whether their parts are bound to roles |

**Not proposed:** more stat nodes on any vehicle. The owner's list already holds 54 trees of them, and our fittings cover the
same ground with trade-offs.

### Trailers (added later on 11 Nov 2026)

The owner then asked how another mod's trailers follow their tractor. [The trailer research](vehicle-trailer-research.md)
found the mechanism in RimThunder - Core, already on the owner's list, and adds five ideas to this round: a drawbar trailer
for the tipper, a tank trailer for the bulk tanker, the generator set as a trailer, a recovery tow and a camp trailer. A
trailer is a second way to give a thin vehicle something of its own, beside the signatures above; its three questions are
in that record.

### Order (agent default)

1. **Generator set: exhaust heat recovery.** The smallest slice, on the game's own temperature system, on our own vehicle.
2. **Tipper: the loading grab**, if the check on Vehicle Framework's cargo passes.
3. **Cherokee: the water bucket**, after the sling's flight on the map is settled, since the bucket rides on it.
4. The service lead, the litter kit and the trains, each after its own check.

One version and one test list per batch, as the owner prefers.

## About the other mods

Read with `scripts/workshop-research.py report` on 11 Nov 2026; a Workshop page shows activity, never whether a mod works.

| Mod | Updated | Declares | Uploader's last reply in the comments read | What it means here |
| --- | --- | --- | --- | --- |
| VVE - Upgrades Extended | 13 Oct 2026 | 1.7, 1.6, 1.5 | 20 Jul 2025 (27 of 27 comments read) | Active: its trees are its author's to change, so we add beside them |
| Trains of the Rim | 26 Aug 2025 | 1.6 only | None in the newest 50 of 432 | Looks dormant and does not declare 1.7. A tree of ours on its vehicles would rest on a mod that may not follow the game; weigh that before building |

## Cost and risk

- **Saves:** a node key or a fitting's defName is permanent once a save can hold it. Each is named before its first build.
- **The loading grab and the water bucket are new jobs**, the dearest kind of work here, and both lean on Vehicle Framework
  behaviour not yet read. Each has a check before any design.
- **Performance:** every proposal acts on an order or a parked state, never per tick; heat recovery uses the room's own
  temperature pass.
- **Enemy vehicles:** none of these is offered to raiders in this round. **Added later on 11 Nov 2026:** the owner asked that
  vehicle mechanics be designed with other factions' vehicles in mind (now a rule in AGENTS.md), so each signature gets that
  section when it is designed: what it does on a raider's vehicle, on one parked at a base we attack, and on one we capture.

## Questions for the owner

1. **Other authors' vehicles:** the Cherokee only as a pilot, or every vehicle we already patch by name?
2. **Signatures:** one per vehicle as above, or shared families (recovery, long range, cold weather) offered to many?
3. **Their trees:** leave them alone and add beside them (agent default), or add nodes of ours into them?
4. **The submarine test hull:** does it stay bare, as a test hull?

## Who chose what

- **Owner:** the question itself and the Cherokee as the example (11 Nov 2026); no overlays on other authors' art (11 Nov
  2026); the sound enclosure cut (9 Nov 2026).
- **Agent defaults:** leaving other authors' trees alone; one signature per vehicle; the order; every signature named here.

## Sources

- Vehicle Framework, by Smash Phil: its upgrade types, read from our decompile and summarised in our own words.
- Vanilla Vehicles Expanded and its Tier 3, by the Vanilla Expanded team; VVE - Upgrades Extended and Trains of the Rim, by
  their Workshop authors: Def names and counts read from the installed mods, nothing copied.
- The Cherokee's hook rating: the source is given in [the sling record](vehicle-sling-design.md).
