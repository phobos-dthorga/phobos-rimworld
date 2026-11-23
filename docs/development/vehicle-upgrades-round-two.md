# Vehicle upgrades, round two: what every vehicle has, and a signature feature for each (11 Nov 2026)

**Status (17 Nov 2026):** built in D'thorga Framework 0.97.0 (and Vehicles 0.42.0: batch 1, the generator set's heat exchanger, the
claim order and the core part store), 0.101.0 (and Vehicles 0.45.0: batch 2, the hitch and the Pintle flatbed trailer, and batch 3,
the four signatures: the service lead, the loading grab, the water bucket and the litter kit; its Take casualties aboard switch in
0.126.0) and 0.103.0 (and Vehicles 0.47.0: the
shared [families](#vehicle-upgrade-families-upgrades-shared-across-many-vehicles-12-nov-2026)); not built: the Trains of the Rim tree (skipped while that mod looks dormant) and
anything for the Roaring Tiger; the submarine test hull stays bare by the owner's choice. Not yet tested in game, apart from the
families' check of 13 Nov 2026 (the families record says what passed).

**Changed 18 Nov 2026:** the owner removed VVE - Upgrades Extended from the mod list, as our own upgrades now cover it. What this
record says about its trees, the Cherokee's five stat nodes among them, describes the list as it stood on 11 Nov 2026.

**Merged here on 17 Nov 2026**, each kept as written: [Vehicle upgrade families: upgrades shared across many vehicles](#vehicle-upgrade-families-upgrades-shared-across-many-vehicles-12-nov-2026).

A proposal with an inventory. The owner asked on 11 Nov 2026 whether to
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
- **Four questions for the owner** are at the end, answered on 11 and 12 Nov 2026; the builds that followed are in the status line.

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
  CH-47F's centre hook ([the sling record](archive/vehicle-sling-design.md)). Cherokee Sling, the mod ours stands down for, is not on
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
| **Generator set** | **Service lead**, a heavy cable to another vehicle | Charge or jump-start a vehicle with a flat battery in the field | Feed the camp at full output meanwhile | A dead battery away from a depot bay is a visit from the set, not a tow | The battery and charge figures ([electric design](archive/vehicle-electric-design.md)); the camp's power rule | What a flat battery does to a vehicle today |
| Generator set, second | **Exhaust heat recovery** | Warm one room or tent beside the parked set | Nothing; it costs a little output | A cold-biome camp runs on the set's waste heat | The game's room temperature system | Nothing new |
| **Trains of the Rim** (13) | A tree of ours for the steam locomotives: boiler and firebox nodes | Trade fuel for pull, and wood for chemfuel | To be set | To be set | [The steam design](archive/vehicle-steam-design.md) | Whether we bind steam parts on them yet; the mod's state (below) |
| **RimThunder Roaring Tiger** (5) | Nothing proposed yet | | | | Our fittings already reach any vehicle with bound parts | Whether their parts are bound to roles |

**Not proposed:** more stat nodes on any vehicle. The owner's list already holds 54 trees of them, and our fittings cover the
same ground with trade-offs.

### Trailers (added later on 11 Nov 2026)

The owner then asked how another mod's trailers follow their tractor. [The trailer research](vehicle-hitch-design.md#trailers-that-follow-their-tractor-how-rimthunder-does-it-and-what-it-opens-for-us-11-nov-2026)
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

**Its picture (Vehicles 0.49.0, 12 Nov 2026):** the crane folded behind the cab, shown once the node is fitted (owner's pick from
[round three](../../assets/phobos-vehicles/requests/round-three-record.md)). It stays folded while it works; the work shows through the work bar and its sounds.

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
- **How a slung flight works on a map today** (part 2, d8ae738, recorded in [the sling design](archive/vehicle-sling-design.md)): only Vehicle
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

**Take casualties aboard (Framework 0.126.0, 17 Nov 2026; the owner's idea, built the same day).** A switch on any vehicle with the kit,
off to start and saved per vehicle. While it is on, Doctor work (priority 66) and, with Search and Rescue, its field rescue work (161,
above its evacuation to a bed at 160) carry downed people to the litters (`LitterService`, `WorkGiver_PhobosCarryToLitter`, the existing
carry job). **Who:** the colony's people, prisoners and guests on the ground whose care is not "no care". **Where:** the nearest switched-on
vehicle when it is nearer than the patient's bed or they have none (`LitterRule.PrefersLitter`, straight-line distances, no radius);
litters promised to carriers on the way count as taken. **When:** with Search and Rescue, a casualty the game would tend urgently is treated
first; without it the carry is at once, as the game's own rescue is, and a crew doctor tends aboard. All agent choices, owner to revise.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Make a parked helicopter the collection point for casualties far from home, with no right-click per patient | Nothing while off; while on, carriers walking a casualty across the map to a bed when a litter is nearer | Rescue away from base becomes: land, switch on, fly home, unload |

**Other factions' vehicles:** the switch is the player's order, so a raider's or a base's parked vehicle never takes casualties aboard. A
captured vehicle with the kit gains the switch. **Cost:** with every switch off the work giver is skipped by one check of a short list;
switched on, a bed search per casualty at most every 250 ticks.

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

**Decided by the owner, 12 Nov 2026:** batch 3 builds **all four signatures** (the service lead, the loading grab, the water bucket
and the litter kit; built in Framework 0.101.0 and Vehicles 0.45.0, not yet tested in game, sections above); the **shared families
get a design record first**, with nothing built: [vehicle upgrade families](#vehicle-upgrade-families-upgrades-shared-across-many-vehicles-12-nov-2026) (all five built since, in
Framework 0.103.0 and Vehicles 0.47.0); **Trains of the Rim is
skipped** while it looks dormant (last updated 26 Aug 2025, declares 1.6 only), to be raised again if it is updated. Batch 2, the
hitch and the Pintle flatbed trailer, is built in the same versions ([the hitch design](vehicle-hitch-design.md)).

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
- The Cherokee's hook rating: the source is given in [the sling record](archive/vehicle-sling-design.md).

## Vehicle upgrade families: upgrades shared across many vehicles (12 Nov 2026)

*Merged into this record on 17 Nov 2026 from `vehicle-upgrade-families.md`; kept as written.*


**Status (17 Nov 2026):** built in D'thorga Framework 0.103.0 (and Vehicles 0.47.0; parts 1 to 3, 12 Nov 2026): the order for a
whole family, the cold-weather pack, all-terrain tyres and work lights, then long range, then recovery with the hitch's recovery tow;
not built: nothing of the five families is left. Tested in game by the owner (13 Nov 2026, on Framework 0.105.1): work lamps work, and
families and tunes ordered several at once queue as they should; that test also led to two changes in 0.105.2 (new tyres renew the
tread, and work lamps start switched off). Everything else is not yet tested in game. **19 Nov 2026 (Framework 0.171.0, not yet tested in
game):** lights that switch themselves, and work lights from a design's own upgrade; see
[Lights that switch themselves](#lights-that-switch-themselves-19-nov-2026).

The sections "Built in part 1", "Built in part 2" and "Built in part 3" say what was built and where it departs from the proposal; the text before it is the proposal as the owner answered it. On 11 Nov 2026 the owner chose both a signature feature per
vehicle and "shared families" for the rest ([round two](vehicle-upgrades-round-two.md)); on 12 Nov 2026 they asked for this record
first, before any building. **Every family, figure and fit below is agent-proposed** for the owner to pick from. Facts marked "from
the code" were read on 12 Nov 2026 from our source and Vehicle Framework's 1.6 decompile with `scripts/query-code.py`.

### Read this first

- **Five families proposed**: recovery, long range, cold weather, work lights and all-terrain tyres. Each is a set of fittings that
  any vehicle can take where it has the right part, so one family serves dozens of vehicles.
- **They are fittings, beside other authors' trees** (owner, 11 Nov 2026). Nothing goes into another author's upgrade tree, and nothing
  changes another author's picture.
- **Who gets one is decided by effects, never by a list of vehicle names.** A fitting names a loadout role (`Wheels`, `Engine`, a
  cabin) and can match on what the vehicle is (piston or turbine, tracked or wheeled, its mass), as the fittings already do
  (`PhobosVehicleFittingDef`: `roles`, `piston`, `tracked`, `minKilograms`, `maxKilograms`).
- **Three of the five are mostly XML** on systems that already exist. Two need a small piece of code each (said per family).
- **Not proposed:** dust filters (nothing models dust or intake wear today, so it would be a new system, not a family) and armour
  (Vehicle Framework's own upgrade trees and Combat Extended already own a vehicle's armour; a second armour path would fight them).
- **Four questions for the owner** are at the end, answered on 12 Nov 2026; all five families are built (tested in game only in part; see the status line).

### What already exists to build on

From the code:

| What | Where | What a family can do with it |
| --- | --- | --- |
| Fittings by role, with stat and parameter factors, abilities (`grants`) and matches | `src/PhobosFramework.Vehicles/Defs/FittingDefs.cs`; `Fittings.xml` | Most families are new Defs of this type |
| Fit sets (a named group of fittings and tunes) | `PhobosFitSet_*` in `Fittings.xml`, used today for other factions' vehicles | A family could be ordered in one go as a set |
| The hitch and the towed mass | `src/PhobosFramework.Vehicles/Hitch/`, `HitchRule`, `TowedMassKg` (Framework 0.101.0) | Recovery tows through the same holder and drivetrain |
| The crawl floor: a vehicle without grip still crawls at 15% of its speed | `DrivetrainModel.CrawlFloor` | Recovery has something to rescue: a crawling vehicle |
| Tyre grip from tread wear | `TreadRules.GripFactor`, multiplied into the ground's grip by the drivetrain | All-terrain tyres move the same grip |
| Cold starts, block heater, night heater, cab heater | `Systems_Cooling.xml`, `Systems_Cabin.xml`, `PhobosFitting_BlockHeater` | Cold weather bundles what exists |
| Electrical loads cost engine power | `AccessoryRules.EngineShare` | Work lights have a real cost |
| The fuel range on the world map | `VehicleRange` | Long range shows its effect at once |
| Fuel capacity | Vehicle Framework's fuel comp holds it as a number, not a stat (`VehicleStatDefOf` has no fuel capacity) | Long range needs a small code hook |

### The families

#### 1. Recovery

A winch and recovery straps: one vehicle pulls a bogged, crawling or broken one out and tows it home.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Get a vehicle out of mud or home after a breakdown without a helicopter | Nothing new; a recovery tow is slow and heats the tractor | A trip into soft ground takes a recovery vehicle along, as real convoys do |

- **Suits:** land vehicles with our drivetrain (the tow goes through it), heavier ones best; the drivetrain decides whether the pull is
  possible, so no vehicle list is needed.
- **Builds on:** the hitch (a recovery tow is the hitch's slice 4 item, held by the same comp) and the crawl floor.
- **Cost and risk:** the winch pull on a crawling vehicle is new code (a short job and a rule); the tow itself reuses the hitch. Medium.
- **Other factions:** a raider's vehicle runs the lite tier, so it never bogs in our model; at a base we attack, a broken vehicle of
  theirs can be towed off once claimed (the claim order, Framework 0.97.0); a captured recovery vehicle works as ours.

#### 2. Long range

Auxiliary tanks: more fuel aboard for long trips, paid for in cargo space and weight. On a helicopter, ferry tanks.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Reach a far site or a second colony without refuelling on the way | Carrying as much cargo (the tank takes space) | A long trip is planned as fuel against load |

- **Suits:** any vehicle with a fuel tank; aircraft too, as ferry tanks that take cabin space.
- **Builds on:** the range outline on the world map (Framework 0.85.0), which shows the gain without new interface.
- **Cost and risk:** the fitting needs a fuel capacity factor, which today's fitting Def does not have, and a hook that raises Vehicle
  Framework's fuel capacity number while the fitting is on. Small code; the risk is a vehicle saved with more fuel than its capacity once
  the fitting comes off (to be capped on removal, never deleted silently).
- **Other factions:** nothing: other factions' vehicles do not travel the world map on our fuel rules. A captured vehicle keeps its tanks.

#### 3. Cold weather

One order fits a cold-climate pack: the block heater, a deep-cycle battery and a night heater switched on by default.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Run a winter camp or an ice-sheet colony without cold-start wear and flat batteries | Nothing; it costs materials and a little fuel overnight | Cold maps stop punishing vehicles that were prepared for them |

- **Suits:** any vehicle with an engine role (where the block heater fits today) and a starting battery.
- **Builds on:** fittings that exist (`PhobosFitting_BlockHeater`, `PhobosFitting_DeepCycleBattery`) and the fit set type. **Pure XML**,
  plus one player-facing order to fit a set (the sets are only given to other factions today).
- **Cost and risk:** low. The only new thing is letting the player order a set.
- **Other factions:** a raider's vehicle on the lite tier has no cold model, so the pack means nothing there; this is said, not hidden.

#### 4. Work lights

Lamps on a mast or roof bar that light the ground round a parked, working vehicle.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Keep a drill rig, survey truck, tipper grab or generator set working at night at full pace | Leaving the engine off while they burn: the lamps draw on the battery | Night shifts at a remote site become possible, at a fuel cost |

- **Suits:** vehicles with a set-up job (the set-up comp) and an electrical system; the survey truck and drill rig already carry work
  light nodes in their own trees, and this extends the idea to any such vehicle as a fitting.
- **Builds on:** the electrical load rule (lamps cost engine power through `AccessoryRules`); the game's own light (a glow while set up).
- **Cost and risk:** small code: a glow that follows the vehicle while set up, and its draw added to the electrical load. Check first
  whether the existing work light nodes already light the ground or only change a figure.
- **Other factions:** a vehicle parked at a base we attack could show its lamps at night, which makes it a visible target; cheap, since
  the glow is the game's own and only on while set up.

#### 5. All-terrain tyres

A deeper, blockier tread: more grip on soft ground, slower and louder on a road. The opposite of the road tyres that exist.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Cross mud and sand without bogging | Running as fast on a road; the tread also wears faster on hard ground | Tyres become a choice made for the map, not a default |

- **Suits:** wheeled vehicles (the `tracked` match excludes tracks); fit every axle for the full effect, as road tyres say.
- **Builds on:** `TreadRules.GripFactor`, which the drivetrain already multiplies into the ground's grip; road tyres as the pattern.
- **Cost and risk:** the fitting needs a grip parameter for the tyres, which the tread rule does not take yet: a small code change and a
  test. Otherwise XML.
- **Other factions:** the lite tier ignores grip, so nothing changes for a raider; an enemy fit set could list the tyres for flavour only.

### Order (agent default)

1. **Cold weather**, pure XML and a player order for fit sets that every later family can use.
2. **All-terrain tyres**, one small rule change.
3. **Long range**, one small hook, with the save case handled.
4. **Work lights**, after checking what the existing nodes do.
5. **Recovery**, together with the hitch's slice 4, since it is the same comp.

One version and one test list for the batch, as the owner prefers.

### Questions for the owner

**Answered by the owner, 12 Nov 2026:** (1) **all five**, work lights and all-terrain tyres included ("Both work lights and all-terrain
tires sound good"), in the order above; (2) recovery and the hitch's slice 4 **as one piece**; (3) **both**: a whole family in one order, or
each part on its own; (4) long range **on aircraft too**, as ferry tanks. Built in Framework 0.103.0 and Vehicles 0.47.0, in three parts the same day.

The questions as they were put:

1. **Which families**, of the five, and in what order? (Agent default: all five, in the order above.)
2. **Recovery and the hitch's slice 4:** build them as one piece, or the winch alone first? (Agent default: as one.)
3. **Fit sets for the player:** may the player order a whole family in one go, or fit each part on its own? (Agent default: both.)
4. **Long range on aircraft:** ferry tanks in the cabin too, or land vehicles only? (Agent default: aircraft too.)

### Not proposed, with the reason

- **Dust and sand filters:** nothing models dust or intake wear; it would be a new system.
- **Armour:** owned by Vehicle Framework's trees and Combat Extended.
- **Trains of the Rim's steam tree:** skipped by the owner on 12 Nov 2026 while that mod looks dormant (last updated 26 Aug 2025, declares
  1.6 only). Raise it again if the mod is updated.

### Built in part 1 (Framework 0.103.0, Vehicles 0.47.0, 12 Nov 2026)

**Not yet tested in game.** Built by the agent from the owner's answers; every figure is authored.

#### The order for a whole family

- **A Def of its own, `PhobosVehicleFamilyDef`** (`src/PhobosFramework.Vehicles/Defs/FittingDefs.cs`), rather than the enemy fit sets: a
  family names fittings and the switches it turns on (`switchOn`), and nothing else. Parts 2 and 3 add families as Defs in
  `Families.xml`, with any new fittings beside them; they need no new code for the order.
- **Which vehicles it suits** follows from its fittings: it is offered where any of them suits a part (`VehicleFittings.Families`).
- **The order** (`VehicleFittings.FamilyPlan` and `OrderFamily`): each fitting, in the family's order, on every part of the vehicle it suits
  that does not carry it and is not already waiting for it, one fitting to a part. A part another fitting holds is fitted over, and the
  details strip says which. Refused parts (destroyed, a salvaged grade, research unfinished) are left out and the first reason is given.
  Each part is its own job for a mechanic, as before; the family's switches are turned on at once.
- **The window**: a Families section above the parts, one row a family, its picture button **Fit all** in the look of the other choices
  (lit green when every part it suits has it, amber while its work waits, dimmed with its reason). The details strip lists the work,
  materials, hours and skill.

#### Cold weather

- Built as proposed: the block heater and deep-cycle battery fittings, and the night heater switched on. **The night heater still needs its
  setting** (vehicle systems settings, off by default); the pack switches the vehicle's own switch, not the setting.

#### All-terrain tyres

- **`TreadRules.SoftGroundFactor`** (game-free, five tests): the tyre's factor applies in full on the softest going (the drivetrain's ground
  grip 0.3) and fades to none on firm ground (0.8), in step with the ground's grip. The tyres give 1.35 (authored).
- Read through a `softGroundGrip` parameter on the running gear, which the fitting's parameter factor moves, so the window's figures show it.
- **Costs**: 0.95 of the speed and 1.15 times the wear, everywhere (a simplification: the proposal said "on a road").

#### Work lights

- **Checked first, as proposed:** the survey truck's and drill rig's own work light nodes set the `Phobos_WorkLights` stat, which shows their
  lamps' switch and spares the drill rig its night penalty. **Neither lit the ground.**
- **Agent choice: a lamp bracket on every land vehicle**, not only those with a set-up job. The fittings model puts one fitting on one
  part, and the roles a land vehicle has already take fittings (the battery its deep-cycle battery, the alternator its heavy one), so the
  lamps needed a part of their own. It is appended in the land base loadout, as the radiator, water pump, belt, brakes and clutch already
  are, so another author's vehicle gains a part on its Health tab and nothing in its picture.
- **The lamps** (`PhobosFitting_WorkLamps`) grant `WorkLights`. Their switch (`workLamps`, in the light and heavy land loadouts) shows once
  they are fitted (`When_Granted`) and runs at night while the vehicle stands still: 10 A at 12 V or 5 A at 24 V (two 60 W LED floods,
  authored).
- **The light is the game's own** (`WorkLamps`): a glower registered with the map's light grid while the lamps burn and taken off when they
  stop, when the vehicle moves and when it leaves the map. It is never added to the vehicle's Def and never saved. Anyone working inside it
  works at the game's own lit pace, and the drill rig counts the lamps as its work lights (`WorkLamps.Fitted`).
- **Limits:** the generator set lists its own parts (`Inherit="False"`), so it has no bracket. The survey truck and drill rig can carry both
  their own lights and the lamps, and then draw both. The light is checked at the vehicle's rare tick, so it can lag a stop or a start by
  a few seconds.

#### Other factions' vehicles

| Where | Cold weather | All-terrain tyres | Work lights |
| --- | --- | --- | --- |
| **A raid on us** | Nothing: the lite tier has no cold model | Nothing: the lite tier ignores grip | Nothing: the lamps are the player's to fit, and the lite tier lights nothing |
| **Parked at a base we attack** | Nothing | Nothing | Nothing yet: no enemy fit set lists the lamps |
| **One we capture** | Fits as ours | Fits as ours | Fits as ours; it has the bracket like every land vehicle |

Cost with several on a map: a family is worked out only while its window is open (every half second). The lamps add one check to a
vehicle's rare tick, and their light is the game's own glower, registered only while they burn.

### Built in part 2 (Framework 0.103.0, 12 Nov 2026)

**Not yet tested in game.** Built by the agent from the owner's answers (long range on aircraft too, as ferry tanks); every figure is
authored.

#### Long range

- **Two fittings, one family.** The **auxiliary fuel tank** (`PhobosFitting_AuxFuelTank`: capacity ×1.5, cargo ×0.85) goes on a new
  **auxiliary tank cradle**, appended in the combustion base loadout, so every fuelled land vehicle and boat has one. The **ferry tank**
  (`PhobosFitting_FerryTank`: capacity ×1.75, cargo ×0.7) goes on new **ferry tank tie-downs**, appended in the aircraft base loadout, so
  every fuelled aircraft has them. Each vehicle has only the mount for its own, so the family `PhobosFamily_LongRange` names both and
  fits whichever suits. Electric vehicles have neither: they carry no liquid fuel.
- **The capacity hook, from the code.** Vehicle Framework's `CompFueledTravel.FuelCapacity` is its Def's figure passed through
  `VehicleStatHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.FuelCapacity, ...)`, its own extension point for upgrades that add to a
  tank; `AddStatOffset` adds to it, and the offsets are not saved. So a fitting gained `fuelCapacityFactor`, and
  `VehicleFittings.SyncFuelCapacity` adds the difference between what the tanks should add (`FittingRule.FuelCapacityOffset`: the
  capacity without them times the factor less one) and what it last added, held in a field that is never saved. **No Harmony.**
- **Save and load.** Because neither our figure nor Vehicle Framework's offset is saved, both start from nothing after a load. The sync
  runs in the existing post-load hook on `VehicleStatHandler.ExposeData` (where part grades are put back), which reaches a vehicle in a
  caravan too, and again in the systems comp's `PostSpawnSetup`; it is idempotent. Vehicle Framework saves the fuel itself without
  trimming it, and trims to capacity only when it burns fuel on a map (`ConsumeFuel`), which comes after both.
- **Removal (agent choice).** Taking a tank off with more fuel aboard than the vehicle's own tank takes **drains the whole units above
  into containers of the vehicle's fuel beside it**, with a message; the fraction is lost in the draining (the game counts fuel in whole
  items). A tank destroyed with its mount **loses** that fuel, with a message saying how much. Removal is never refused: the player can
  always undo a fitting. A vehicle off the map loses the excess, since there is nowhere to put containers.
- **What follows the bigger tank without new code:** the refuel gizmo and refuelling jobs (Vehicle Framework fills to a share of
  `FuelCapacity`), our depot bay and the tanker's ports (both read `FuelCapacity`), and the range outline on the world map, which reads
  the fuel aboard.
- **Which aircraft.** Every fuelled aircraft on the aircraft loadout, helicopters and jets alike: the Cherokee, the Louie, the Hind (VTOL
  Vehicle) and the Harrier (the fixed-wing framework) all use Vehicle Framework's own fuel comp, and no mod on the owner's list overrides
  its capacity (read 12 Nov 2026), so the offset reaches them all.
- **Limits, said plainly.** The tank's weight and its fuel's are counted only as the cargo space it takes, not as mass on the drivetrain.
  A design whose capacity Vehicle Framework sets outright (an override in its settings) ignores offsets, so a tank adds nothing there.
  Removing the mod with a tank fitted keeps the fuel until it is next burnt, when it is trimmed to the vehicle's own capacity.
- **Checked:** `FittingRule.FuelCapacityOffset`, `FuelAbove` and `WholeUnits` have unit tests; the self-test fits a tank to its test
  vehicle, checks the capacity rises by the factor, that a second sync adds nothing, and that it goes back when the tank comes off.

#### Other factions' vehicles (long range)

| Where | Long range |
| --- | --- |
| **A raid on us** | Nothing: no enemy fit set lists a tank, and other factions' vehicles do not travel the world map on our fuel rules |
| **Parked at a base we attack** | Nothing yet |
| **One we capture** | Fits as ours; it has the cradle or tie-downs like every vehicle of its kind |

Cost with several on a map: the sync runs only when a fitting changes, a vehicle loads or one spawns; nothing per tick.

### Built in part 3 (Framework 0.103.0, 12 Nov 2026)

**Not yet tested in game.** Built by the agent from the owner's answer (recovery as one piece with the hitch's recovery tow,
[vehicle-hitch-design.md](vehicle-hitch-design.md) slice 4); every figure is authored.

#### Recovery

- **One fitting, one family.** The **recovery winch** (`PhobosFitting_RecoveryWinch`) goes on a new **winch mount**
  (`PhobosRecoveryMount`), appended in the land base loadout beside the lamp bracket, so every land vehicle with our systems has one
  (the generator set excepted, as for the lamps). It grants `RecoveryWinch`, which brings both the winch and the tow bar, so the family
  `PhobosFamily_Recovery` names this one fitting.
- **The winch** (`src/PhobosFramework.Vehicles/Hitch/Recovery.cs`, part of `CompPhobosHitch`). **Winch a vehicle** picks one of ours within
  12 cells (the empty cells between the two footprints). While **someone is aboard** the winching vehicle and neither vehicle drives off,
  the stuck vehicle is dragged one whole cell at a time along the line between them (`RecoveryRule.Step`; if the straight step is blocked,
  either axis alone), as Vehicle Framework moves a vehicle (its `Position`), until at most one cell lies between them. **Stop winching**
  ends it; a stall, a block, either vehicle moving or nobody aboard ends it with the reason. It is saved and carries on after a load.
- **How the pull rule decides** (`RecoveryRule`, game-free, with tests). It takes the stuck vehicle's weight (its kerb mass, cargo and
  people, as the sling counts it) times a resistance: its ground's **rolling resistance** if its wheels turn, or its ground's **grip** if
  they do not (Vehicle Framework gives a vehicle whose running gear is wrecked no speed, so it is dragged and slides against the ground's
  friction), plus **suction** on soft ground, up to 0.3 of the weight on the softest going. The ground is read from the same path-cost curves
  the drivetrain uses (`Powertrain.PathCostAt`, shared now). The winch can give its rating, **5,443 kgf (12,000 lb)**, but never more than the
  winching vehicle holds on its brakes: its own weight times its ground's grip. A pull that needs more is refused with both figures, and a
  winch that finds the next cell needs more stalls. The pace is 180 ticks a cell for a light pull, up to twice that at the winch's limit.
  The 12,000 lb rating is a common one for vehicle recovery winches; no one product is claimed, and the figure is authored.
- **The recovery tow.** With the winch fitted, the hitch takes an **ordinary land vehicle** as well as a trailer. It follows by the same
  follow rule on a tow bar: the hitch to its rear axle is 1.5 cells plus three quarters of its length, its axle a quarter of its length
  behind its middle, and it may swing 55 degrees (all authored). Its weight and everything in it reach the drivetrain as the towed mass,
  exactly as a trailer's. Refused, with the reason: aircraft and boats; a vehicle that is drafted, ready to drive (undraft it so it rolls
  free); one towing or winching something itself; one more than **1.5 times** the tractor's weight (authored); another faction's until
  claimed (the claim order). The towed vehicle shows "Hitched to" and its tractor's **Unhitch** button.
- **Crew (from Vehicle Framework's code).** People aboard a vehicle are held in its handlers, not on the map, so moving the vehicle by its
  `Position` carries them. **They stay aboard**; if anyone tries to drive it while it is towed, its pather is stopped on the spot, each tick
  while hitched.
- **Agent choices and limits:** no ground anchors or snatch blocks; the winch pulls in a straight line by whole cells and stops a cell
  short; a vehicle on the tow is taken as rolling freely even if its wheels are wrecked; the tow limit is a weight ratio rather than a
  force balance (the drivetrain already slows the pair); the winch needs only someone aboard, not a colonist's job. Towing on the world map
  and in caravans was not built here; it came with hitch slice 3 (Framework 0.104.0): a towed vehicle rides along in the hitch's own holder
  and is set down on arrival (corrected 17 Nov 2026).
- **Checked:** `RecoveryRule` has eight unit tests; the build passes the gate. Nothing about the winch or the tow is tested in game.

#### Other factions' vehicles (recovery)

| Where | Recovery |
| --- | --- |
| **A raid on us** | Nothing: no AI orders a winch or a tow, and raiders' vehicles run the lite tier, so they never bog in our model |
| **Parked at a base we attack** | Claim a wrecked or empty one (the claim order), then winch it out and tow it home: loot as content |
| **One we capture** | Winches and tows as ours, if a winch is fitted |

Cost with several on a map: a winch ticks only while it works, and a tow only while its tractor moves (the hitch's own rule).

### Who chose what

- **Owner:** families as well as signatures (11 Nov 2026); our features beside other authors' trees, never inside (11 Nov 2026); a design
  record first, nothing built (12 Nov 2026); Trains of the Rim skipped (12 Nov 2026); all five built, recovery as one piece with the
  hitch's recovery tow, whole sets or part by part, ferry tanks on aircraft (12 Nov 2026).
- **Agent-proposed:** every family, its fit, its order, and what is left out.

### Sources

- Vehicle Framework, by Smash Phil: its stats and fuel comp, read from our decompile and summarised in our own words.
- Our own code and Defs, cited by path above. No figure is set here, so none needs a source yet.
- The tanks' factors (1.5 and 1.75) and cargo costs are authored. That real ferry tanks can more than double an aircraft's fuel is general
  knowledge, not a cited figure, and no figure here rests on it.

## Lights that switch themselves (19 Nov 2026)

**Status (19 Nov 2026):** built in D'thorga Framework 0.171.0. Not yet tested in game.

The owner's report (19 Nov 2026): work lamps on vehicles had not worked "for quite some time" and drew no power, and they asked for a
setting, on by default, that switches lights on and off by itself.

### What was found

- **No fault in the fitted lamps' code was found.** Their switch, light and draw follow the same rules as when the owner passed them on
  13 Nov 2026 (check 66), and the owner's log (PG15, 19 Nov 2026) has no error near them. In that save all five sets of fitted lamps (an
  M2A3, a HEMTT and three JLTVs) had their switch off. A switched-on lamp lights only **at night** and only while the vehicle **stands
  still**; by day or on the move it drew nothing and the Systems tab said only "standing by". That reads as broken, and is the likeliest
  explanation (agent's inference, not confirmed by the owner).
- **A real fault:** the drill rig's and the survey truck's own **Work lights** upgrade raised a figure only (the drill's pace at night).
  It gave no switch, no light and no draw, because the lamps' switch showed only for the fitted lamps. Fixed in XML: the lamps' load is
  now there for either.
- **A small one:** the lamps followed their switch and a vehicle setting off only at the next slow update (up to about four seconds).
  They now follow at once.

### What it does

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Never think about lights: headlights come on to drive at night, work lamps when people work round a parked vehicle at night | Flattening a parked vehicle's battery by forgetting a switch: headlights go out when it is parked with the engine off, and work lamps on the battery stop at half charge | Lights become a per-vehicle "may switch themselves on" choice, not a chore |

- **Setting:** "Lights switch themselves", in the vehicle systems settings, **on by default** (owner's direction).
- **Headlights** (agent's choice): lit at night while the vehicle is driven or its engine runs; out once parked with the engine off.
- **Work lamps** (agent's choices, all in the loadout Defs, `autoOn`): lit at night while the vehicle stands still and someone of its own
  faction (people or mechanoids, not animals) stands within 8 cells, the lamps' own pool; they stay lit for **1 in-game hour** after the
  last one leaves, so a colonist walking past does not flick them. With the engine off and no grid, only while the battery is **more than
  half full**, so it can still start the engine.
- **The buttons stay.** With the setting on, each vehicle's headlights and work lamps buttons mean "may switch themselves on" (on by
  default) and say so in their description; switch one off to keep that vehicle dark. This is saved under its own key, apart from the hand
  switch, so lamps switched off by hand before the setting came are not kept dark by it. With the setting off, the buttons switch the
  lights by hand, as before.
- **The Systems tab says what a switched-on light waits for** ("waiting for dark and a stop", and with the setting on "waiting for dark, a
  stop and people near"; headlights "waiting for dark", or "waiting for dark and a driver"), from each load's Def.
- **Cost:** the decision is made once per look of each vehicle's systems (every rare tick, and at its events), cached, and read by the
  buttons and tabs. The people check runs only at night on a parked vehicle, over the pawns of its own faction on the map.

### Other factions

- **Raiders and vehicles at a base we attack** have no electrics or switches (the light tier): nothing changes. Their headlights burn at
  night while they drive, as before (`HeadlightRule.LiteBurns`); their work lamps never light.
- **A captured vehicle** becomes the player's and follows the setting like any other.

### Who chose what

- Owner: the setting, on by default.
- Agent (for the owner to revise): every rule and figure above; keeping the buttons as a per-vehicle "may switch themselves on"; the
  standby wording.
