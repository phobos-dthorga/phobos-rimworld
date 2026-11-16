# The bulk tanker: design (8 Nov 2026)

**Status (17 Nov 2026):** built in D'thorga Framework 0.34.0 and D'thorga Vehicles 0.12.0 (8 Nov 2026), the first vehicle of ours,
and grown since: the second wave of fluids (Framework 0.35.0, Vehicles 0.13.0), compartments (0.37.0, 0.15.0), bulk buying at a
settlement (0.38.0, 0.16.0), chemfuel into generators and crude through Rimefeller's pipeline (Framework 0.109.0, Vehicles 0.52.0), and
an electric pump that runs on the battery with the engine off (Framework 0.116.0, Vehicles 0.57.0). Not built: quests, RimThunder
engine-class nodes, the fire monitor port, helixien gas and liquid oxygen. **Not yet tested in game:** no changelog records an owner
test of the tanker.

Its public name is the **Ullage bulk tanker** (Ullage U1; [principle 10](utility-vehicles-design.md#principles-for-every-utility-vehicle-agent-proposal)
of the utility vehicles record); the player guide's chapter is [working vehicles](../player-guide/working-vehicles.md#ullage-bulk-tanker). It is one of the
five vehicles of [the first family](utility-vehicles-design.md#the-first-family-in-full-agent-default-the-extraction-chain-owner-to-confirm);
the sections below are the shape the other four records follow ([survey truck](vehicle-survey-truck.md), [drill rig](vehicle-drill-rig.md),
[tipper hauler](vehicle-tipper-hauler.md), [generator set](vehicle-generator-set.md)).

*Moved here on 17 Nov 2026 from `utility-vehicles-design.md`; kept as written.* Where the text below says "the table above", "principle
8" or "principle 9", "the heavy-truck set" or "the rig", it means [utility vehicles](utility-vehicles-design.md): its principles, its
parts model (the stats table and the parts by role, the latter since reversed by [parts bills](parts-bills.md)) and its family. The
owner's decisions on the tanker and its open questions stay there too, under "Who chose what" and "Open questions for the owner".

## Bulk tanker (owner's addition, 8 Nov 2026; the design is the agent's, owner to confirm)

A heavy truck whose tank is an upgrade: a **liquid tanker** or a **gas tanker** by the player's choice, carrying whatever the mods
present give it to carry. One chassis, one `VehicleDef` (`Phobos_Tanker`). Vehicle Framework's upgrade tree does the switching:
each tank node names the other in `disableIfUpgradeNodeEnabled`, so one tank must come off before the other can go on
(`CompUpgradeTree.Disabled`, `:21690`; `replaces` would swap them in one step and refund the old tank, `FinishUnlock`, `:21777`, with
no chance to drain it; corrected 8 Nov 2026), a node carries its own `graphicOverlays` (the tank body is drawn by the
node, not the base texture), a `StatUpgrade` sets the capacity, and the unlocked set is saved and re-applied on load
(`CompUpgradeTree.ReloadUnlocks`, VF 1.6 decompile `Vehicles.decompiled.cs:21875`); our comp asks `CompUpgradeTree.NodeUnlocked`
(`:21659`) which tank is fitted. Prior art: none carries fluid on Vehicle Framework; Trains of the Rim's tanker car is cargo only
(`TOTR_Railcars_Tanker`: CargoCapacity 30,000, a Tank part, no fluid comp; patched Def read 8 Nov 2026).

| Item | Value |
| --- | --- |
| Footprint, drawSize | (3, 10), (4, 11) (9 Nov 2026, after the owner's first look: 2 x 6 was two thirds the width of the other heavy trucks. This matches RimThunder's PLS, the biggest truck on the list; the picked art is 3.27 cells wide at 10 long) |
| Mass | 340 empty; contents add their own mass by density (below) |
| Fuel | Chemfuel, 28 a day driving, 6 a day pumping (the engine at idle drives the pump through a power take-off) |
| Crew | Driver; the pump runs unattended once the hoses are connected |
| Research | Chassis and liquid tank: the Vehicles tier (VVE's hauler research where present). Pressure vessel: the research of the first gas building present (Helixien gas; Odyssey's vacuum technology for the oxygen net), else Fabrication |
| Cost (authored) | The heavy-truck set by role (table above), plus 280 steel and 8 components. Liquid tank node: 120 steel. Pressure vessel node: 160 steel, 20 plasteel, 2 components. A refit refunds half (VF's default `refundFraction`) |
| Upgrades | `LiquidTank` or `PressureVessel` (below); `HoseReel` (a second hose, PumpRate x1.5); `InsulatedLiquidTank` (see Temperature, below). Overlays: the liquid tank, the pressure vessel, the hoses out, damage |
| Tie-ins | Every fluid mod below, each in its own `IfModActive` folder; none required |

**The two tank nodes:**

| Node | Capacity (`Phobos_TankCapacity`) | Why that figure | Look |
| --- | --- | --- | --- |
| `LiquidTank` | 3,000 units | Two thirds of Vanilla Chemfuel Expanded's fixed chemfuel tank (4,500 units for 140 steel, patched Def read 8 Nov 2026), so a tanker never beats a built tank per steel; it buys mobility | A horizontal elliptical tank with a manway and a ladder |
| `PressureVessel` | 1,200 units | A pressure vessel holds far less per tonne of steel than an open tank; authored ratio | A single cylinder with hemispherical ends, a relief valve and a gauge cabinet |

**Variants from the accepted art (owner, 8 Nov 2026: all twelve overlay attempts are accepted, for upgrades, extensions and storage types;
the owner approved the mapping below, 8 Nov 2026; the figures are authored).** Under principle 8 each variant that becomes a node must move a figure.

| Accepted art | Proposed node | What it would change (authored) |
| --- | --- | --- |
| Liquid tank 1, default | `LiquidTank` | The general tank above: 3,000 units |
| Liquid tank 2, wider with slim hatches | `InsulatedLiquidTank` | 2,700 units; contents change temperature slowly (owner, 8 Nov 2026; it replaces the separate insulation node first sketched) |
| Liquid tank 3, squarer ends | `HighVolumeLiquidTank` | 3,300 units; a full load slows the truck more |
| Pressure vessel 2, default | `PressureVessel` | The vessel above: 1,200 units |
| Pressure vessel 1, slim | `LightPressureVessel` | 800 units; cheaper and lighter |
| Pressure vessel 3, wide band | `HeavyWallVessel` | 1,000 units; a tougher shell (`Phobos_TankIntegrity`) |
| Hose reel 1 and hose 2 | Held for a later node | Alternates, such as a second hose type |

With more than two tank nodes, each names all the others in `disableIfUpgradeNodesEnabled` (the list form of the field read at `:21696`).

**Contents are a Def anyone can write.** `PhobosTankContentsDef` (a new Def type in D'thorga Framework's Vehicle-Framework part, XML):
label, `phase` (Liquid or Gas, which decides the node it needs), unit label, `kilogramsPerUnit` (the mass balance), `flammable`, its temperature behaviour (below), and a
list of **ports**: the classes that move contents between the tank and the world. Each contents Def ships in the folder of the mod
that gives it meaning, so a player with no fluid mod still has a chemfuel tanker that refuels vehicles and loads from barrels.

| Contents | Phase | Ports | Folder | Lean |
| --- | --- | --- | --- | --- |
| Chemfuel | Liquid | `Port_VehicleFuel`: refuels any VF vehicle alongside (`CompFueledTravel.Refuel(float)`, `:19335`). `Port_Items`: chemfuel items in and out, the game's own stack. `Port_PipeNet` on `VCHE_ChemfuelNet` where Vanilla Chemfuel Expanded is active | Common; the pipe port under VCHE | First |
| Deepchem | Liquid | `Port_PipeNet` on `VCHE_DeepchemNet`: the tanker is the pipeline from a pumpjack field you never built | VCHE | First |
| Water | Liquid | `Port_DubsPlumbing`: pull through the Framework's `PlumbingFeed` (already used for coolant), push through `PlumbingNet.PushWater` (DBH 1.7 `PlumbingNet.cs:314`), carrying DBH's contamination level. `Port_FireMonitor` later: the catalogue's fire engine becomes this port | Dubs Bad Hygiene | First |
| Nutrient paste | Liquid | `Port_PipeNet` on `VNPE_NutrientPasteNet` | Vanilla Nutrient Paste Expanded | Second |
| Astrofuel | Liquid | `Port_PipeNet` on `VGE_AstrofuelNet` | Vanilla Gravship Expanded, Chapter 1 (Odyssey) | Second |
| Oxygen | Gas | `Port_PipeNet` on `VGE_OxygenNet` (its unit is u³; its Def links oxygen canisters at 1:1) | Vanilla Gravship Expanded, Chapter 1 | Second: the only gas net on the owner's list today |
| Helixien gas | Gas | `Port_PipeNet` on Vanilla Helixien Gas Expanded's net | VHGE (not on the owner's list) | When the owner adds the mod |
| Crude oil | Liquid | `Port_Rimefeller`: its own pipe model (`CompStorageTank.Storage`, `BasePipeNet`), a second model to read | Rimefeller | Later |
| Hemogen | Liquid | `Port_PipeNet` on `VRE_HemogenNet` | Vanilla Races Expanded - Sanguophage | Second (owner, 8 Nov 2026) |

**How transfer works (the honest model).** The tanker never registers as a pipe connector: VEF registers connectors at spawn from
their neighbours (`PipeNetManager.RegisterConnector`, PipeSystem 1.7 `PipeNetManager.cs:93`), and a thing that moves would leave a
stale one behind. Instead, **deployed** (hoses out, wheel chocks down: it cannot move, through the same deploy comp on VF's move and
draft hooks that the rig uses), the pump moves `Phobos_PumpRate` units an hour between the tank and the first counterpart a port
finds in the cardinal neighbours of the vehicle's cells: a pipe net through `PipeNetManager.GetPipeNetAt(cell, def)` and then
`PipeNet.DistributeAmongStorage` or `DrawAmongStorage` (public, `PipeNet.cs:664` and `:743`); Dubs' plumbing through `PlumbingFeed`;
a vehicle's `CompFueledTravel`. Three gizmos, Load, Unload and Hold; each says what it is doing and names what stops it (no
counterpart beside the tanker, the counterpart full or empty, the pump below its floor). Rare tick; nothing per tick.

**Temperature (owner approved, 8 Nov 2026; the agent's proposal).** The contents have their own temperature, which moves slowly towards the outside
air; the Dashboard shows it. It slows the player or puts them on a clock, and stops them only where physics would. Each contents Def
carries its own limits, so a modder's fluid can freeze, thicken, spoil or vent without code.

| Contents | Trigger | What it stops | How it changes play | What it opens up |
| --- | --- | --- | --- | --- |
| Water | Tank below 0 °C | **Pumping**, until it thaws; the truck can still drive | Winter runs need timing, or a heated garage overnight | The insulated tank makes winter deliveries possible |
| Chemfuel, deepchem | Below about −8 °C, worst at −20 °C, only with the existing fuel-thickening setting on (off by default: chemfuel is not diesel) | Nothing | Pumping slows to a floor of 15%, as a vehicle without grip crawls and never stops | Refuelling in deep cold is possible but slow |
| Nutrient paste, hemogen | Above 0 °C | **The whole load spoils** after 0.75 days, about 18 in-game hours; below 0 °C the clock pauses | Long summer hauls become a race | The insulated tank buys time, so it matters in hot climates too |
| Compressed gas | Fire or extreme heat | The relief valve vents the load fast | Move the truck away or lose the cargo | A damaged valve (the tank fittings part) is worth repairing before a dangerous run |

**Sources and simplifications.** Water's 0 °C is physics. The −8 and −20 °C curve is the one the fuel system already uses (No. 2 diesel's gel point, secondary source, in `Systems_Lubrication.xml`); applying it to chemfuel is a gameplay choice. The 0.75 days and the pause below 0 °C are Vanilla Expanded Framework's own rule for unpowered stores (`CompResourceStorage.Tick`, PipeSystem 1.7 `CompResourceStorage.cs:110`, default `daysToRotStart`), and a tanker has no power, so the clock always runs. Gas pressure follows absolute temperature: 0 to 40 °C is only about 15% (273 K to 313 K), so ordinary weather has no effect, while a 300 °C fire about doubles it. The insulation time (about a day to reach the outside temperature, against six hours for a plain tank) is authored. Insulation slows the change and never prevents it.

**Left out, because they would not change a decision:** ordinary hot days on gas; cold gas, where the pressure falls but the amount carried does not; and liquid expansion in a full tank, which would only be a fill limit. **Known:** Dubs Bad Hygiene's water towers were not found to freeze (no freezing in `CompWaterStorage`), so a tanker's water is more exposed than a fixed tower: the owner accepts this for a thin mobile tank. On a temperate map, most of this rarely comes up.

**Contents as a systems-service quantity.** The loadout adds `PhobosVehicleSystem_Tank`: quantity `contents` from 0 to the
capacity, on the Dashboard; `kilogramsPerUnit x contents` joins the vehicle's mass through the kerb-mass extension (whether VF's
Mass stat then slows the vehicle is verified at build). Stages: **residue** below 2 % (the tank is not clean); **heavy** above 60 %
(`Effect_StatFactor` 0.85 on MoveSpeed, authored). **The chemist's rule:** changing contents or tank type needs an empty tank and a
purge job (4 hours, a colonist; water where Dubs Bad Hygiene is present), and water drawn from a tank that last held chemfuel is
contaminated at Dubs' Untreated level. Refusals name the step: "drain the tank", "purge the tank". **Open at build:** Vehicle
Framework has no Def-level way found so far to refuse taking a node off a loaded tank (`RemoveUnlock`, `:21827`, only queues the work),
so the drain-first rule needs either one Harmony patch there or a removal that spills what is left. **Prerequisites are all-of**
(`PrerequisitesMet`, `:21674`), so "either tank" cannot be a prerequisite: the hose reel takes none and does its
work once a tank is fitted.

| Part | Health | Depth | Cells | Feeds | Job |
| --- | --- | --- | --- | --- | --- |
| Chassis | 170 | External | all, weight 3 | BodyIntegrity | |
| Cab | 50 | External | (0, 2) (1, 2) | | Crew |
| Engine | 60 | Internal, priority | (0, 3) (1, 3) | MoveSpeed, Phobos_PumpRate | Drives the road and the pump |
| Transmission | 40 | Internal | (0, 1) (1, 1) | MoveSpeed | |
| Front axle and wheels | 35 | External | (0, 3) (1, 3) | MoveSpeed | |
| Rear axles and wheels | 50 | External | (0, −1) (1, −1) (0, −2) (1, −2) | MoveSpeed | Tandem axles |
| Truck fuel tank | 35 | Internal | (0, 1) | | VF's fuel-leak reactor |
| Tank shell | 90 | External | (0, 1) (1, 1) (0, 0) (1, 0) (0, −1) (1, −1) (0, −2) (1, −2) | Phobos_TankIntegrity | Our reactor: a hit drains liquid as a puddle (chemfuel: filth that burns) or vents gas fast; flammable contents at zero integrity explode through VF's `Reactor_Explosive`, scaled to the load, as VCHE's fixed tank already does (`CompProperties_ExplosiveContent`, flame, radius up to 12 at 4,500 units; patched Def read 8 Nov 2026); on by default, with a setting to turn it off (owner, 8 Nov 2026) |
| Tank fittings | 40 | External | (0, −2) (1, −2) | Phobos_TankIntegrity | Baffles on the liquid tank; the relief valve and gauge on the pressure vessel (the node sets the label; the part key never changes) |
| Pump and power take-off | 40 | Internal | (1, 2) | Phobos_PumpRate | Below the floor nothing moves |
| Hose reel and couplings | 30 | External | (0, −2) (1, −2) | Phobos_PumpRate | Needed to deploy |
| Manifold and valves | 30 | Internal | (0, 0) | Phobos_RigControl | Below the floor the pump will not run |
| Wiring | 25 | Internal | (1, 0) | Phobos_RigControl | |
| Lights | 15 | External | (0, 3) (1, 3) (0, −2) (1, −2) | | Load at night |

Cells run x from −1 to 1 and z from −4 to 5 on a 3 x 10 footprint, and are read off the picked art: bonnet at z 5 and 4 with the front wheels at z 4, cab and pump at z 3, the deck from z 1 back, the rear axles at z −2 and −3. Colours for the Def, which give the picked body its tone while leaving it repaintable: `color` (162, 156, 123), `colorTwo` (195, 110, 36).

**Argued against here:** registering the tank as a VEF connector (stale the moment the vehicle moves); two VehicleDefs for the two
tanks (a refit would be a new vehicle and lose the parts' saved health); adding parts by upgrade (VF parts are fixed per Def and saved
by position); a fire engine of its own (it is a water tanker with a monitor port). **Diesel-electric:** the tanker stays diesel; a
power take-off pump is the real machine.

## As built: the bulk tanker (8 Nov 2026, D'thorga Framework 0.34.0 and D'thorga Vehicles 0.12.0, Drafts)

**Not yet tested in game.** Built on the owner's go. Comps and rules are in D'thorga Framework (`src/PhobosFramework.Vehicles/Tanks`, `TankRule` in the core, a new tie-in assembly for pipe nets); the vehicle, tree, contents, textures and parts are in D'thorga Vehicles.

| What the player does | What it lets them do | What it stops | How it changes play |
| --- | --- | --- | --- |
| Fits a tank | Carry a liquid or a gas | Only one tank at a time | The tree decides what the truck can haul |
| Sets up beside something | Pump to and from pipes, plumbing, stacks and other vehicles | The truck cannot move or be drafted | A delivery is a place and a stop, not a route |
| Lets it get cold, hot or shot | Insulation and repair buy time | Frozen water will not pump; a holed tank will not load | Temperature and damage are planned around |

**Differences from the design above, each the agent's choice for the owner to revise:**

- **Removing a tank with a load in it spills the load.** Vehicle Framework gives no way to refuse the job, so this needs no Harmony patch.
- **The insulated tank halves the spoilage clock** (`spoilRate` 0.5). The approved table said it buys time on hot hauls, but a clock that only pauses below 0 °C would give it nothing to do above freezing.
- **The contamination clause is dropped for fuel:** purging is required before a change of contents, so water cannot follow chemfuel unpurged. Dubs Bad Hygiene's own quality is tracked instead: contaminated water pumped into a tower contaminates it.
- **A fresh load of something that can freeze arrives 5 °C above its freezing point** and cools in the tank, otherwise an empty tank could never load water in the cold.
- **A fire vents gas by the outside temperature or the vehicle burning**, not the contents' temperature, which lags by hours.
- **Gas vessels are patched in with Vanilla Gravship Expanded**, since nothing else carries a gas yet. Water needs Dubs Bad Hygiene.

**Size, from the owner's first look (9 Nov 2026):** the owner finds even Vanilla Vehicles Expanded's 3 x 6 Big Rig small, so heavy trucks of ours are sized against RimThunder's 3 x 10 PLS. The footprints of the other four designs above predate this and need revisiting before their art. **Damage marks** are drawn by the tank's comp: Vehicle Framework never reads a part requirement on a graphic overlay.

**Not built:** RimThunder engine-class upgrade nodes (principle 9), `Port_FireMonitor`, crude oil and helixien gas ports, masks for overlays.

## As built: the second wave of fluids (9 Nov 2026, D'thorga Framework 0.35.0 and D'thorga Vehicles 0.13.0, Drafts)

**Not yet tested in game.** The owner asked for items that are really fluids: FSX and prometheum, and the agent's list, all but liquid oxygen. Each is a contents Def using the item port, so stacks on the ground pump straight in.

| Fluid | What it lets the player do | What it stops | How it changes play |
| --- | --- | --- | --- |
| FSX | Move it in bulk, or park a truck bomb | Nothing | A hit may set off the load (25%, authored) |
| Prometheum | Move it in bulk | Pressure vessel only (agent's reading of "ignites on contact with oxygen") | Any spill catches fire |
| Toxic sludge | Haul waste off your land in one trip | Nothing | 8 days to get rid of it unless frozen (the game's own figure); a spill or dissolved load pollutes, 6 cells a pack as the game does |
| Fecal sludge | Empty septic tanks without barrels | Nothing | One trip replaces dozens |
| Napalm, crude, beer, wort, insect jelly | Bulk haulage | Nothing | Wort runs on the game's 5-day clock; napalm's blast reaches further |

**Agent defaults, owner to revise:** ten units to an FSX or prometheum, thirty to a wastepack; a blast cap of 15 cells; blast factors of 1.4 (FSX) and 1.25 (napalm). The owner did not answer these, so they are the agent's stated leans.

**Deferred, on the owner's word (9 Nov 2026): liquid oxygen.** D'thorga Medical's liquid oxygen store (`docs/development/medical-equipment.md`) would be a real cryogenic cargo: insulated tank only, boiling off slowly. It waits on functionality not built yet: the store's own pipe connection needs reading, and a boil-off behaviour for tanks.

## As built: compartments (9 Nov 2026, D'thorga Framework 0.37.0 and D'thorga Vehicles 0.15.0, Drafts)

**Not yet tested in game.** Owner's decisions, 9 Nov 2026: a tank holds one fill type by default, with a setting on by default; compartment upgrades divide its capacity; filled alike they still hold the whole capacity; food cannot share with anything else, and the player is told why; what counts as food is anything with a nutrition value, with a whitelist and blacklist.

| | |
| --- | --- |
| Lets the player | Carry up to three fluids on one trip, a delivery round rather than a single drop |
| Stops them | Pumping two compartments at once; mixing food with anything else; dividing a pressure vessel |
| Changes | A burst tank loses every compartment at once, and only what burns feeds the blast |

**Agent's choices:** three compartments at most, one to each hatch on the liquid tanks; one tank temperature; water, nutrient paste, hemogen and wort whitelisted as food, because no item of theirs carries a nutrition value (the owner may blacklist any of them). The rule is food against non-food only, so sludge may share with fuel.

## Bulk trading and quests (owner's decisions, 9 Nov 2026; not built)

**Bulk buying only at a settlement the player has travelled to**, with the tanker in the caravan: "the player should have reasons to go out and explore, not an easy way to sit at home". No buying from visiting traders or orbital traders. **As built, 9 Nov 2026 (Framework 0.38.0, Vehicles 0.16.0, Drafts; not yet tested in game):** a Bulk order button on the caravan, priced by the game's own `TradeUtility.GetPricePlayerBuy` with the caravan's best negotiator, buying only, leaving `RimWorld.TradeDeal` alone: at least seven mods on the owner's list patch its `TryExecute`. What a settlement offers is its trader's own `WillTrade`. It lets the player fill up far from home; it stops them buying from home, or buying the same fluid at one settlement twice in 15 days (authored); it changes the tanker into a reason to travel. The amount is set in a small window with the game's own count control, `TransferableUIUtility.DoCountAdjustInterface` (Framework 0.39.0, owner's choice: interfaces follow the game's patterns), which needs no patch. Agent's choice: fluids that exist only in pipes are not sold. **Quests come later**, generic and in the Framework, so other vehicles of ours can use them too.
