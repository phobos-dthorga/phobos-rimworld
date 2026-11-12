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

## Batch 3: the signatures in detail (12 Nov 2026)

The owner chose all four signatures on 12 Nov 2026, in one version with the hitch and sling slice 2 (Framework 0.101.0, Vehicles
0.45.0). Each is designed here before it is built. Facts marked "from the code" were read that day with `scripts/query-code.py`;
every figure is authored unless a source is named. **Not yet tested in game.**

### The generator set's service lead

**Built** in D'thorga Framework 0.101.0 and D'thorga Vehicles 0.45.0 (12 Nov 2026), as below; **not yet tested in game**. Agent
choices made while building: the button shows only once the lead is fitted (a set without it has nothing to offer); the node key is
`PhobosServiceLead`, Phobos-prefixed although the tree's older keys are not; the lead is drawn as a line while the set is selected.

**From the code, what a flat battery does today:** each electrical system keeps a `charge` quantity. Below its jump-start level
(`jumpStart.raiseTo`) the engine will not start, and the vehicle is refused a draft. Today it can be helped three ways, all through
one service, `CompPhobosVehicleSystems.ReceiveJumpStart`: leads from a donor vehicle of the same electrical system (with a crafted
jump-start kit when the setting asks for one), a crafted booster pack, or the booster pack every vehicle is assumed to carry (a
setting). Separately, a parked vehicle beside a building on a grid with power to give trickle-charges at its system's
`shoreChargeAmps` (`ShorePowered`, the `shoreCharging` flow in `Systems_Electrical.xml`). The generator set's cable end is such a
building, but the set makes only what its grid draws, so a vehicle beside it rarely sees spare power.

**What the lead is:** an upgrade node on our generator set (`PhobosServiceLead`), giving the stat **service lead reach** (6 cells,
authored). With the set **set up and running**, the player selects it, presses **Run the service lead** and picks one of our
vehicles within reach. The lead is plugged in at once, as the set's own cable end is.

- **Charging:** while the lead is in and the set is running, the vehicle counts as on shore power, so the existing shore-charging
  flow charges it at its own system's rate. No second charging model.
- **A start:** if the vehicle's battery is below its start level, the lead brings it up through the same jump-start service as a
  donor would, with its own message. No kit or booster pack is spent: the set is the charger.
- **The cost to the camp:** the lead takes **300 W** (authored: a heavy battery charger and its losses) off the top of what the set
  makes. The camp gets the rest, and the set's engine works harder for the lead (load and fuel follow).
- **It comes out by itself**, with a message, when the set packs up, stops running, or either vehicle moves out of reach.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Charge or jump-start a vehicle with a flat battery in the field, with no donor, kit or booster pack | Feed the camp at the set's full output meanwhile; run the lead to more than one vehicle, or beyond 6 cells | A dead battery away from a depot bay is a visit from the set, not a tow |

**Other factions' vehicles:** a vehicle that is not ours is refused ("not ours"; claim it first, with the claim order of Framework
0.97.0). Another faction's generator set never sets up, so it never runs a lead. A captured generator set runs a lead as ours does,
and a captured vehicle can be charged once it is ours. Cost with several on a map: nothing per tick; the set's rare tick checks its
one lead.

### The tipper's loading grab

**Built** in D'thorga Framework 0.101.0 and D'thorga Vehicles 0.45.0 (12 Nov 2026), as below; **not yet tested in game**. Agent
choices made while building: what one lift carries is a stat (`Phobos_GrabLift`, 500 kg from the node) so a later upgrade can raise
it, while reach and pace stay in the rules Def; the button shows only once the grab is fitted; the reach shows as a blue outline
while the tipper is selected; node key `PhobosLoadingGrab`.

**From the code, the check:** Vehicle Framework's cargo is the vehicle's own inventory, and `VehiclePawn.AddOrTransfer(Thing, int)`
(`VehicleFramework-1.6:Vehicles.decompiled.cs:14646`) is public: it moves a thing into the cargo, fires Vehicle Framework's own
"cargo added" event and takes the thing off any load list the cargo window made. Its capacity is the stat `CargoCapacity`, against
the mass already aboard (`MassUtility.InventoryMass`), as its own loading reads it (`:1471`). So **the check passes**: our own work
can fill the cargo without the loading window, through Vehicle Framework's own call.

**What the grab is:** an upgrade node on our tipper (`PhobosLoadingGrab`), a small hydraulic crane behind the cab. With a driver
aboard and the tipper stopped, the player presses **Work the grab**. Each lift takes things lying loose within **4 cells** of the
truck (authored) into the cargo: one stack, or as much of it as the lift and the room aboard allow. A lift takes **20 seconds** of
game time (authored) and up to **500 kg** (authored, about a small loader crane's rated load at short reach). It stops by itself,
with a message saying how much came aboard, when nothing is left in reach or the cargo is full.

- **What it takes:** loose items on the ground that are not forbidden and not in a stockpile or on a shelf (agent choice: what the
  player stored, they meant to keep there), nearest first. Pawns, corpses and minified buildings are left alone.
- **Hydraulics:** like the body's rams, the grab is paced by the hydraulic fluid and stops when there is too little.
- **Holds the truck still**, as tipping does, and **cannot tip while the grab is out**, nor be worked while tipping.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Load chunks and stacks lying near the truck with only the driver, no hauler | Tip while the grab is out; reach past 4 cells; lift more than 500 kg at once; drive while it works | One driver can do a quarry run alone: the carries at the loading end go away |

**Other factions' vehicles:** another faction's tipper is never ordered to grab. Parked at a base we attack, it is a truck to claim;
once claimed, its grab works as ours does. Captured: the same. Cost with several on a map: nothing unless a grab is working, and a
working grab scans a few dozen cells once each rare tick.

**No picture yet:** the node has no overlay, since no picture of a crane exists for the tipper and this round makes no new art
(owner's rules on paid art). The grab's work shows through the work bar and its sounds.

### The water bucket (any helicopter with a cargo hook; the Cherokee today)

**Built** in D'thorga Framework 0.101.0 and D'thorga Vehicles 0.45.0 (12 Nov 2026), as below; **not yet tested in game**. Everything here
beyond the owner's choice of the signature is an agent choice, labelled for the owner to revise.

**From the code, the checks:**

- **How a fire goes out.** A fire is a thing of its own on a cell (`Fire`, with a `fireSize`); a burning pawn or vehicle carries one as an
  attachment. The game's own extinguishing damage shrinks a fire and destroys it below a tenth of its size
  (`DamageWorker_Extinguish.Apply`, `RimWorld-1.7` decompile), and also gives a pawn it hits a condition, which is the firefoam popper's
  foam. Water from a bucket is not foam, so the drop destroys the fires on the cells it covers, and the fires attached to things on
  them, and gives nobody a condition. Heat from fire (D'thorga Medical's hot cells) is worked out from the fires there are, so it
  follows by itself: no tie-in needed.
- **How a slung flight works on a map today** (part 2, d8ae738, recorded in [the sling design](vehicle-sling-design.md)): only Vehicle
  Framework's take-off and landing, through its own stand-in; no hover and no orders in mid-air, because that stand-in is shared with
  other aircraft mods' patches. A bucket that must fly to water and on to a fire cannot ride on it.
- **So the bucket run is a stand-in of our own**, the shape Vehicle Framework itself uses: the helicopter leaves the map into a thing of
  ours that flies the circuit and draws it, then sets it back down where it took off. Nothing of Vehicle Framework's launch is touched,
  and no Harmony is needed. The run can be selected, and its inspect text says what it is doing.

**What the player does:** fit a cargo hook (as for a sling load), make a **fire bucket** at the vehicle parts bench and load it aboard.
With the helicopter crewed, fuelled, under open sky and the weather fit to fly, press **Water drop** and click a fire within
**50 cells** (authored). The helicopter lifts off, flies to the open water within reach that makes the shortest circuit, dips the
bucket, flies to the fire, empties the bucket over it and comes back to where it stood.

| Rule | What we do | Source or label |
| --- | --- | --- |
| **Water carried** | The bucket's 3,000 litres, or less if the hook cannot lift that much today (a hot day's derating applies; a litre of water taken as a kilogram, the bucket's own weight left out) | Authored: a medium helicopter bucket's size; the hook rating and derating are the sling's |
| **Area put out** | Every cell within a radius that gives 100 litres a cell (about 3 cells across for a full bucket), under open sky; roofed cells are missed | Authored |
| **The circuit** | Climb, fly to the water, a dip of 6 seconds, fly to the fire, a pour of 2 seconds, fly back, come down; at 0.15 cells a tick, with 3 seconds to climb and to come down | Authored |
| **Fuel** | The helicopter's own fuel use per tick for the length of the run, times the slung flight's 1.3 when that setting is on | Vehicle Framework's rate; the factor is the sling's |
| **Weather** | The sling's own rule: no run in a storm or in wind above the sling's limit, with the sling's setting | The sling's |

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Put out a fire from the air, far from the colony's hands, with nobody walking into the heat | Sling a vehicle on that flight; drop with no open water within reach; drop under a roof or through it; fly in a storm or high wind | A wildfire near a camp or outpost becomes a job for the helicopter, not a loss; a pond near the base becomes part of fire planning |

**Cost:** nothing unless a run is in the air: then one thing ticks and draws for about half a minute. Finding the water reads the
terrain within 50 cells once, when the order is given.

**Other factions' vehicles:** no raider is ever given a bucket or the order, so a raider's helicopter never flies one. Parked at a base
we attack, a helicopter is claimed first (the claim order); once ours, with a hook fitted and a bucket aboard, it flies runs as ours do.
Captured: the same. A run puts out every fire in its area, whoever's it is.

**Left out, said plainly:** the run is flown at a fixed height and speed, with no wind drift; the helicopter cannot be ordered mid-run
(it finishes its circuit); a map closed while a run is in the air loses the helicopter with it; Cherokee Sling's own airlift, when
active, takes the Cherokee's hook mount, so no hook and no bucket there (the hook's own stand-down).

### The litter kit (any vehicle whose loadout names a cabin; the Cherokee today)

**Built** in D'thorga Framework 0.101.0 and D'thorga Vehicles 0.45.0 (12 Nov 2026), as below; **not yet tested in game**. Agent choices
throughout, for the owner to revise.

**From the code, the check: can a patient be carried and tended in a vehicle seat?**

- **The Cherokee has no passenger seats.** Its only role is the pilots' (two seats, both needed to fly; `VVE_Cherokee`'s Def). So
  "seats swapped for stretchers" has nothing to swap on it, and the kit has to bring its own places.
- **Vehicle Framework's own seats** take a downed pawn only in a seat with no duties, and only when a caravan is formed: its job that
  carries a downed pawn aboard (`JobDriver_CarryPawnToVehicle`) is given by the caravan-forming duty alone, never as an order on the map.
- **A seat's people are carried along and age as the vehicle ticks** (`VehicleRoleHandler.DoTick`), but nobody tends them, and a seat
  added by code is lost at the next load unless the vehicle's Def or upgrade tree names it (`VehicleRoleHandler.ExposeData` rebuilds the
  role from its key). Adding a role to another author's Def would also open it to every colonist who boards, not only patients.
- **Tending out of reach of the map works:** the game tends caravan patients through `TendUtility.DoTend`, which needs neither
  pawn on a map, one doctor every 1,250 ticks (`CaravanTendUtility`).
- **In flight nothing ages:** Vehicle Framework's flying world object does not tick what it carries
  (`AerialVehicleInFlight.ShouldTickContents` is false), so a casualty flown home arrives as they left.

**So the litters are ours:** a holder on the vehicle's own comp, like the sling's, that takes downed pawns only. The game finds them
through the same `Pawn.GetChildHolders` postfix the sling uses. While anyone lies on a litter, the comp ticks them, and a crew member
aboard who can do doctoring tends them, as a caravan's doctor would.

**What the player does:** fit the **litter kit** to the vehicle's cabin (the Cherokee's upholstery) in the fittings window. Right-click a
downed person with a colonist selected: **Carry to a litter aboard...** names each vehicle with a free litter. The colonist carries
them over and straps them in. **Unload casualties** on the vehicle sets everyone on its litters down beside it, for a doctor to take
to bed.

| Rule | What we do | Source or label |
| --- | --- | --- |
| **Litters** | 4 per kit | Authored |
| **Who goes on one** | A downed person who is not hostile to the colony | Agent choice: a hostile is a capture, not a patient |
| **Tending aboard** | Each crew member aboard who can do doctoring, is not downed and is not in a mental state tends one patient every 1,250 ticks, the game's own caravan interval, with the best medicine the vehicle's cargo holds that the patient's medical care allows, or none | The game's caravan rule and `TendUtility.DoTend` |
| **The cost** | The kit's frames and stretchers take a fifth of the cargo space (cargo capacity x0.8) | Authored |
| **Leaving the map** | In flight, the litters ride along and nothing ages. For anything else that takes the vehicle off the map (a caravan), the patients are set down where it stood first, with a message | Agent choice: a caravan has its own tending, and a vehicle caravan does not know our holder |

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Fly a casualty home from a battle or a crash site, kept tended on the ground by a crew medic, without a seat to spare | Carry as much cargo (a fifth less); load anyone who is not down, or a hostile; take patients on a caravan | The helicopter becomes the colony's air ambulance: a doctor flying as the second pilot matters |

**Cost:** nothing unless someone lies on a litter; then the vehicle ticks them, as Vehicle Framework ticks its passengers, and checks for
a tend once every 250 ticks.

**Other factions' vehicles:** no raider is given the kit, and another faction's vehicle never offers its litters to our colonists. Parked
at a base we attack, it is claimed first; once ours, a fitted kit works as ours does. Captured: the same. Who lies on a litter can be a
guest or a prisoner of ours (anyone downed and not hostile).

**Left out, said plainly:** patients on litters are not fed while they wait; their weight does not count against the cargo capacity;
whether the colonist bar counts them in flight is unchecked (as for the sling's passengers); a vehicle destroyed in flight loses them
with it; a doctor on the ground cannot tend them aboard (unload them first). D'thorga Medical has no tie-in: none was needed.

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

**Decided by the owner, 11 Nov 2026:** (1) signatures for **every vehicle we patch**, not the Cherokee alone; (2) **both** a signature
per vehicle and shared families for the rest; (3) our features sit **beside** other authors' trees as fittings, never inside them;
(4) the submarine test hull **stays bare**. Order: batch 1 (the generator set's heat exchanger, with the claim order and the core
store), batch 2 (the hitch and trailer), batch 3 (signatures and families for every vehicle we patch). **Batch 1 is built** in
D'thorga Framework 0.97.0 and D'thorga Vehicles 0.42.0, not yet tested in game.

The questions as they were put:

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
