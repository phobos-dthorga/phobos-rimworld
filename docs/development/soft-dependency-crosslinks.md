# Soft-dependency cross-links: new tie-ins with other mods (13 Nov 2026)

**A research record. Nothing is built, and nothing here is tested in game.** The owner, 13 November 2026, asked for "an exploration
into the kind of cross-links we can make regarding soft-dependencies with other mods", and chose:

| Question | Owner's answer |
| --- | --- |
| Which of our mods | All of them: Vehicles and Framework, Medical, Factory Additions and Water Tap, plus InterRim Ballistic Missile and The Dead Man's Switch "for their anti-air, RADAR, FCS, etc." |
| Candidates | The owner's mod list first, plus a short list of strong mods not on it |
| Outcome | Record only; the owner picks |
| Direction | Both ways: their things feeding our systems, and our systems giving their things a role |

It is one of three records:

1. [The tie-in register](tie-in-register.md): every tie-in that exists today (63 folders across six mods).
2. This record: new links for our mods with the mods on the owner's list, and every unbuilt proposal from earlier records in one
   table.
3. [Air-defence tie-ins](air-defence-tie-ins.md): InterRim Ballistic Missile and The Dead Man's Switch against our aircraft, strikes and
   submarines. **Its rows are not repeated here**; the short list below ranks across both.

How to read it:

- Facts marked "from the code" were read on 13 Nov 2026 with `scripts/query-code.py` and `scripts/query-defs.py`; other mods' Defs named
  in a row are confirmed present in the owner's game unless the row says "not checked". Our files are named in full.
- **Author** comes from `python scripts/workshop-research.py report` on 13 Nov 2026: the last update, the versions the mod declares, and
  the uploader's last reply in its recent comments ("none seen" means none in the newest comments, not that the author is gone).
- **Kind**: **XML** (a tie-in folder or `MayRequire` entries only), **C#** (a small change in our code that reads the other mod by name),
  **assembly** (a tie-in assembly that names their types, loaded only from its `IfModActive` folder), **hook** (a Harmony patch into
  their code, named). No row hard-references another author's assembly from a core assembly.
- Every new row says what it **lets** the player do, what it **stops**, and how it **changes** play (owner rule, 8 Nov 2026). Rows that
  cannot answer are in "argued against".

## Read this first

- **Found on the way, likely a live fault in the owner's game: Vehicles at Work drives vehicles our systems refuse to move.** It is on
  the owner's list (row 228, updated 11 Nov 2026). From its code (`.local/decompiled/VehiclesAtWork-1.7/WOR/VF.cs`): it switches the
  ignition through Vehicle Framework's setter, which asks `CanDraft` (`VF:10694`), so our refusals to start (a flat battery, a cold
  boiler) hold; then it gives the vehicle a plain `Goto` job and calls `StartPath` itself (`VF.cs:670-700`). Our refusals to *move* on a
  map live in `CompPhobosVehicleSystems.CanMove(FloatMenuContext)`, which only the player's float menu asks; `CaravanRefusalPatch`
  (`src/PhobosFramework.Vehicles/Service/Patches.cs:316`) refuses `CanMoveFinal` off the map only. So a colonist commuting with Vehicles
  at Work can drive a vehicle with worn-out brakes, a set-up Busbar or a settled submarine. **Not reproduced in game.** Row SD1 is the
  fix; it is ours to make, since the refusal is ours.
- **The strongest new links**, all on the owner's list: Vehicles at Work (SD1, SD2), the tanker on Rimefeller's own pipes (SD4), the
  tanker refuelling any fuelled building (SD5), EMP on vehicle electrics in XML only (SD9), bench-bill templates for the assembly
  benches (SD30), Search and Rescue's medics with our field devices (SD22), the VFE Medical IV drip as continuous fluids (SD23),
  Rimatomics' potassium iodide as prevention (SD21) and Vanilla Temperature Expanded's ducts carrying our gases (SD8).
- **About 50 earlier proposals** that name another author's mod are listed once, with a fresh verdict, under "Carried forward". Their
  designs stay in their home records.
- **Authors matter** (owner, 22 Oct 2026: mods need active authors). Biological Warfare (last update 30 Dec 2025, declares 1.6 only,
  no reply seen), Ion Weaponry (24 Jul 2025, 1.6 only), Luciferium Expansion and InterRim Ballistic Missile (30 Jun 2026, 1.6 only, no
  reply seen) are quiet; rows that lean on them say so.

## New links: Vehicles and Framework

| # | Link, and the other mod | Reads → changes | Lets · Stops · Changes | Kind | Author | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| SD1 | **Vehicles at Work obeys our refusals to move.** Vehicles at Work (`shengchipangxieyaotuke.vehiclesatwork`) | Our `MoveRefusal` → its vehicle checks (`WOR.VF.Healthy` reads Vehicle Framework's `CanMove`, `VF.cs:433`; `Wheels.cs:147` picks free vehicles); its "why stuck" line (`VF.cs:577`) gives our reason | **Lets** commuting by vehicle with our systems on. **Stops** colonists driving a vehicle with worn brakes, a set-up Busbar or a settled submarine. **Changes** upkeep from a gizmo chore to something that keeps the commute running | Assembly, one postfix on their public static, or a wider answer on our side (`VehiclePawn.CanMove`); read which is cleaner first | Updated 11 Nov 2026, 1.7; uploader replied 11 Nov; new and rough by its author's own account | **Take, first**: a fault between our mod and one on the owner's list |
| SD2 | **Lots on depot bays.** Vehicles at Work's parking lots (`Zone_ParkingLot`) | A vehicle parked in a lot that holds one of our depot bays → the bay's refuel, charge and faster service run as for any parked vehicle; its `ParkVehicle` job prefers a lot with a free bay | **Lets** a motor pool that refuels itself overnight. **Stops** commuters leaving vehicles to run dry in a far lot. **Changes** where bays are built: by the lots | Assembly | As SD1 | Take after SD1 |
| SD3 | **Hauls by tipper and trailer.** Vehicles at Work's `WOR_HaulByVehicle` | Its haul trip → our Talus tip and Pintle trailer unload at the stockpile | **Lets** bulk hauls without micromanaging. **Stops** nothing. **Changes** little: its haul already works | Assembly | As SD1 | Set aside: little gain over its own haul |
| SD4 | **The tanker on Rimefeller's pipes.** Rimefeller (`dubwise.rimefeller`): `OilPipeline`, `OilStorage`, `FuelStorage` | A parked tanker of crude or chemfuel → a new tank port on Rimefeller's own pipe net (not a Vanilla Expanded net, so `PipeNetLink` cannot do it); pipes laid beneath it the hidden way, as plumbing is | **Lets** drill rig → tanker → refinery with no barrels. **Stops** the barrel shuffle. **Changes** where oil fields and refineries sit: apart, joined by road | Assembly, in `Mods/VehiclesRimefeller` | 22 Oct 2026, 1.7; no reply seen | **Take** (`Tanker_Rimefeller.xml:3` already says it is missing) |
| SD5 | **The tanker refuels fuelled buildings.** VFE Power's chemfuel generators (`VFE_IndustrialChemfuelPoweredGenerator`, `VFE_PortableChemfuelPoweredGenerator`), and the game's own | A tanker of the building's fuel parked beside it → a port for `CompRefuelable` buildings, fed like the vehicle-fuel port (`src/PhobosFramework.Vehicles/Tanks/TankPorts.cs:79`) | **Lets** an outlying generator kept running by a fuel run. **Stops** hauling chemfuel by hand. **Changes** a remote outpost from "carry stacks" to "send the tanker" | C# in the core (it needs no other mod) | 14 Oct 2026, 1.7 | **Take**; a core feature that VFE Power gives more targets |
| SD6 | **A radiation line round VFE Power's reactor.** `VPE_NuclearGenerator` gives "local radiation" when overloaded (its description) | Our radiation control line (`docs/development/radiation-control-line.md`) → loaded with VFE Power too, not only Rimatomics | **Lets** a reactor hall run with suits. **Stops** walking into an overloaded core unprotected. **Changes** little unless overloading is used | XML, if its radiation is cut by toxic resistance | 14 Oct 2026, 1.7 | **Read first**: what its overload applies, and whether toxic resistance cuts it, are not checked (no decompile) |
| SD7 | **Central heating for parked vehicles.** Dubs Central Heating (`dubwise.dubscentralheating`): `HeatingPipe`, `HeatingPipeHidden`, boilers, radiators | A heating pipe under a parked vehicle → its engine block heater draws from the net (the underlay pattern of `src/PhobosFramework/Persistence/PipeUnderlay.cs`); the Busbar's exhaust heat exchanger feeds the net as a boiler | **Lets** cold-weather starts in a heated motor pool, and waste heat into a base. **Stops** wasting the Busbar's heat. **Changes** cold-map garages | Assembly | 16 Oct 2026, 1.7; last reply seen 2025 | Medium; after SD4 |
| SD8 | **Our gases through Vanilla Temperature Expanded's ducts.** `VTE_AirDuct`, `VTE_UndergroundAirDuct`, `VTE_VentValve`, `VTE_WallMountedVent` | Our room-gas pass reads only the game's `Building_Vent` (`src/PhobosFramework/Gases/MapComponent_PhobosRoomGases.cs:147`) → its ducts become openings between the rooms they join | **Lets** ducts clear carbon monoxide and smoke, and a planned extractor fan reuse its vents. **Stops** smoke staying in a room its duct really serves. **Changes** how bases are aired | C# by name, or assembly | 6 Nov 2026, 1.7; no reply seen | **Take**: the gas layer should match how the owner's bases are built |
| SD9 | **EMP on electrics.** The game's EMP, Combat Extended's electrical damage, Ion Weaponry's ion bullet (`IW_IonBullet`) | The part-hit rule's unused damage filter (`damageDefs`, `src/PhobosFramework.Vehicles/Defs/SystemDefs.cs:162`) → EMP drains the battery and harms electrics, wiring and avionics parts | **Lets** EMP grenades and mortars stop a vehicle for real. **Stops** EMP being a stun alone. **Changes** what to bring against armour | **XML only**; Ion Weaponry by `MayRequire` | Ion Weaponry: 24 Jul 2025, 1.6 only | **Take**, with the game's EMP; Ion Weaponry only as an extra entry |
| SD10 | **Rimkit's repair kit for field repairs.** Dubs Rimkit (`dubwise.dubsrimkit`): `DubsRepairKit`, `SalvagedComponent` | Our part-supply table (`PartSupplies.xml`) → its kit stands in for a service's consumables, its salvaged component matches our salvaged grade | **Lets** field service with what the player already makes. **Stops** nothing. **Changes** little | XML | 13 Oct 2026, 1.7 | Small; take when parts are next touched |
| SD11 | **Roads by kind.** Rails and Roads of the Rim (`mlie.railsandroadsoftherim`): asphalt, glitter road, railroad | World-map ground treats any road alike (`WorldGroundRules.PathCost(..., ground.Road, ...)`, `src/PhobosFramework.Vehicles/Systems/Powertrain.cs:582`) → firmness and grade by road kind; rail counts only for Trains of the Rim engines | **Lets** paved roads matter to tyres and fuel. **Stops** a locomotive treating a dirt road as rail. **Changes** world-map routes | C# by name | 14 Sep 2026; declares up to 1.6; uploader replied 15 Sep | Medium; check it runs on 1.7 first |
| SD12 | **Repair expertise.** Vanilla Skills Expanded (`vanillaexpanded.skills`): `Repairman` (`VSE_RepairSpeed`) | Services and fittings read construction speed only (`src/PhobosFramework.Vehicles/Jobs/ServiceJobs.cs:187`) → also its repair speed | **Lets** a specialist mechanic be faster. **Stops** nothing. **Changes** who services | C# by stat name | 3 Nov 2026, 1.7 | Small; take when service work is next touched |
| SD13 | **A life-support pack is its own air.** Life Support Pack (`kilo.lifesuppack`) | "Sealed" is judged by CE smoke sensitivity or toxic resistance 0.999+ (`src/PhobosFramework/Gases/MaskFactor.cs`) → its pack plus a sealed helmet counts | **Lets** rescue in smoke with the gear the player owns. **Stops** a vacsuit wearer choking on carbon monoxide. **Changes** fire and rescue kit | C# | 15 Oct 2026, 1.7 | Low-medium |
| SD14 | **Worn parts recycle.** Vanilla Recycling Expanded (`vanillaexpanded.recycling`) | Destroyed parts and worn kits → bills at its recycling bench | **Lets** a little steel back. **Stops** nothing. **Changes** little | XML | 14 Oct 2026, 1.7 | Low |
| SD15 | **Engines in a closed garage make carbon monoxide.** Ours, through `PhobosGases`; TSIND Garage Doors QA (`tsind.garagedoors.qa`) as the vent | An idling engine indoors → carbon monoxide in its room; an open garage door clears it | **Lets** a garage feel like one. **Stops** idling inside a sealed room without harm. **Changes** where vehicles warm up | C# (ours); the door by name | 13 Oct 2026 | Medium; the gas part needs no other mod |

## New links: Medical

| # | Link, and the other mod | Reads → changes | Lets · Stops · Changes | Kind | Author | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| SD20 | **Biological Warfare's vaccines and antiserums in our care.** `USH_*Vaccine`, `*_Antiserum`, `*_Gas` | Designed already: [vaccines and prevention](vaccines-and-prevention.md) proposals 1 and 4 | See its record | C# | 30 Dec 2025, 1.6 only, no reply seen | **Wait**: quiet author; Factory Additions already makes its items |
| SD21 | **Potassium iodide before exposure.** Dubs Rimatomics: `PotassiumIodide`, `RimatomicsRadiation` | Our dosing (`docs/development/emergency-dosing.md`) → gives iodide as prevention to colonists behind the radiation line or in fallout, including fallout from the strike model (`src/PhobosPatches.Irbm/Strike/RimatomicsTieIn.cs`) | **Lets** a doctor protect the hall crew. **Stops** iodide sitting unused in storage. **Changes** reactor and strike aftermath into a medical routine | C# | 23 Oct 2026, 1.7 | **Take** |
| SD22 | **Field medics with our devices.** Search and Rescue (`ch4acko3.searchandrescue`): `SAR_FieldRescue`, `SAR_EmergencyMedicalCare` | Its field medics → use our oxygen kit, resuscitator, intubation, decompression and fluids; a rescue can end at a vehicle with a litter kit. Our dosing already waits for patients it has not reserved (Medical README) | **Lets** a combat medic do what our doctors do at base. **Stops** a downed colonist dying beside a medic who has the kit. **Changes** CE battles | Assembly; a decompile exists (`.local/decompiled/SearchAndRescue`) | 6 Nov 2026, 1.7; uploader replied 10 Nov | **Take** |
| SD23 | **The IV drip gives fluids.** VFE Medical (`vanillaexpanded.vfemedical`): `Facility_IVDrip` ("delivering lost fluids") | A bed linked to its drip → burn-shock fluids run continuously, as our fluids kit does; its operating table takes our anaesthesia machine | **Lets** the hospital the player already built treat burn shock. **Stops** a second, parallel drip of ours. **Changes** burns care to bed-side | XML and small C# | 14 Oct 2026, 1.7 | **Take** |
| SD24 | **Transfusion for blood loss.** Hemogen Pack - Emergency transfusion (`aoba.hemogendirect`) | Our device needs are fixed in code (`src/PhobosMedical/Equipment/Rules/DeviceRule.cs`) → a blood-loss need its job answers; the air ambulance carries packs | **Lets** doctors transfuse unasked. **Stops** bleed-outs with packs in the freezer. **Changes** what an ambulance carries | C# | 15 Oct 2026, 1.7 | Medium |
| SD25 | **Suits from the stand.** Outfit Stands Plus (`khamenman.outfitstandsplus`) and the game's outfit stand | The protection line looks for loose apparel only (`src/PhobosFramework/Protection/ProtectionLine.cs:191`) → also on stands; a mending stand keeps suits repaired | **Lets** a real change room by the door. **Stops** colonists turning back while suits hang beside them. **Changes** hall layout | C# | 20 Oct 2026, 1.7; replied 27 Oct | Medium; the game's own stand is the first half |
| SD26 | **Paramedic mechs use field kits.** Biotech `Mech_Paramedic` | Our field oxygen kit and hand pump → usable by mechs | **Lets** a mechanitor's medics help. **Stops** nothing. **Changes** little | C# | Ludeon | Low |
| SD27 | **Saltwater draught as weak fluids.** Medieval Solutions for More Injuries (`endoxis.medievalsolutionsformoreinjuries`) | Its `SaltwaterDraught` → a weaker fluids source for burn shock | **Lets** low-tech burn care. **Stops** nothing. **Changes** little | XML | 12 Nov 2026, 1.6 only | Low |
| SD28 | **Stocked field kits.** Smart Medicine (`memegoddess.smartmedicine`) | Its stock-up jobs → also our field kits | **Lets** doctors carry kits. **Stops** nothing. **Changes** little | C# | 2 Nov 2026, 1.7 | Low |
| SD29 | **Hospital visitors get triage.** Colony Hospital (`jianyuan.colonyhospital`) | Its patients → "save life" authority by default (guests start at "never") | **Lets** its hospital play use triage. **Stops** nothing. **Changes** little | C# | 15 Oct 2026, 1.6 only | Low |

## New links: Factory Additions and Water Tap

| # | Link, and the other mod | Reads → changes | Lets · Stops · Changes | Kind | Author | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| SD30 | **Bench-bill templates for the assembly benches.** VFE Production's `VFE_ComponentFabricationBench`; RimThunder's `RT_AssemblyBench` (engines, wheels); our vehicle parts bench | Copy the fabrication bay's templates (`mods/PhobosFactoryAdditions/Common/Defs/BenchBillTemplateDefs/BenchBills_FabricationBay.xml`, which already take `MayRequire`) | **Lets** components and engines made on a line. **Stops** hand-made components gating every service and fitting. **Changes** the vehicle economy | **XML** | VFE Production 14 Oct 2026; RimThunder Core 4 Jul 2026, 1.6 only | **Take** |
| SD31 | **Hydroponics on piped water.** Automatic Hydroponics (`poncho.automatichydroponics`) | Under Dubs Bad Hygiene, as the oven rule | **Lets** a consistent water rule. **Stops** free water. **Changes** a farm's plumbing | C# | 20 Jul 2026, 1.6 only | Low |
| SD32 | **Brewing on piped water.** Vanilla Brewing Expanded (`vanillaexpanded.vbrewe`) | Same rule; the tanker carries its spirits | As SD31 | C# and XML | 14 Oct 2026, 1.7 | Low |
| SD33 | **Hoppers that take the tanker's fuels.** Munitions Industries for VFE Factory (`turkler.leammomod`) | FSX and prometheum from the tanker → its hoppers | **Lets** a fuel line into munitions. **Stops** nothing. **Changes** little | C# | 24 May 2026, 1.6 only | Low |

## New links: other

| # | Link, and the other mod | Lets · Stops · Changes | Kind | Verdict |
| --- | --- | --- | --- | --- |
| SD40 | Better Exploration Loot (`frozensnowfox.betterexplorationloot`): our kits and field oxygen kits in ruins | Lets a find; little else | XML | Low |
| SD41 | Tech Level Progression (`mrhydralisk.techlevelprogression`): every D'thorga research project carries a tech level | A correctness check more than a link | XML | **Check** when research is next touched |
| SD42 | Deep And Deeper (`shashlichnik.deepanddeeper`): the survey truck marks cave entrances | Lets a find | C# | Low |
| SD43 | Exosuit Framework (`aoba.exosuit.framework`): fuel cells filled from the tanker or a chemfuel line | Lets refuelling in the field | C# | Low |
| SD44 | Turn It On and Off - RePowered (`mlie.turnonoffrepowered`): our medical machines draw little while idle | Lets the electrolyser's 3,000 W sit idle cheaply | C# | Low |

## Not on the owner's list

| Mod | Why the link would be strong | Verdict |
| --- | --- | --- |
| Vanilla Helixien Gas Expanded | A Vanilla Expanded pipe net: the tanker's pressure vessel already plans for it (`utility-vehicles-design.md`), a gas generator set, fuel for Central Heating's gas boiler, leaks as a room gas | Strongest of these; only if the owner adds it |
| VFE Spacer Module | Its air purifiers clear our gases room by room | Only if added |
| Atmosphere Overhaul (subscribed, switched off) | Real gas per cell; we already stand down for it on fire heat | High clash risk; only if switched on |
| Celsius | Per-cell temperature; already detected for fire heat | Only if added |
| Vanilla Trading Expanded (subscribed, switched off) | Tanker bulk orders follow its markets | Only if switched on |
| Pharmacist | Triage tags pick medicine quality | Not checked for 1.7 |
| VFE Insectoids 2 | The drill rig's infestation risk joins its hives | Only if added |

## Carried forward from earlier records

One row per unbuilt proposal that names another author's mod. The design stays in its home record; the verdict here is fresh.

| Home record (row) | Other mod | Idea | Status there | Fresh verdict |
| --- | --- | --- | --- | --- |
| [Avionics](vehicle-avionics.md) A7 | Vanilla Vehicles Expanded, Harrier | Flight hydraulics on their parts | Agreed | Unchanged: first batch |
| Avionics A10, A11, A12 | Harrier, fixed-wing framework, VTOL Vehicle | Sight, night and evasion scale their figures | Agreed, hooks | Unchanged; A12 now also reads DMS fire control ([air defence](air-defence-tie-ins.md)) |
| Avionics A13, A16 | InterRim Ballistic Missile, The Dead Man's Switch | Radar strike warning; countermeasures | Deferred; later | **Revisited** in the air-defence record: DMS Expedition's chaff and seekers change A16's premise |
| Avionics L5 | Vanilla Vehicles Expanded Warbird and Smuggler | Piston loadout | Agreed | Unchanged |
| [Round three](vehicle-crosslinks-round-three.md) G13 | Combat Extended | Ammunition aboard a burning vehicle cooks off | Agreed | Unchanged |
| [Outstanding work](outstanding-work.md) V6 | RimThunder | APU upgrade node | Deferred | Unchanged: RimThunder Core declares 1.6 only |
| Outstanding work W8 | Trains of the Rim | Steam upgrade tree; safety-valve sound | Deferred, mod dormant | Unchanged |
| Outstanding work G5 | Crew Presets | Standing crews and convoys | Set aside | Unchanged |
| Outstanding work G7 | Vanilla Expanded Framework outposts | Report to its authors | Owner's option | Unchanged |
| Outstanding work (deferred) | InterRim Ballistic Missile | Our treatment of its other strategic warheads | After the owner tests the nuke | Unchanged |
| [Vehicle patches](vehicle-patches.md) | Harrier under Combat Extended; Vehicle Framework | Cheaper armour upgrade; a kill-leavings report | Proposed | Unchanged |
| [Seabed crawler](vehicle-seabed-crawler.md) soft tie-ins | Rimatomics; Rimefeller; InterRim Ballistic Missile; Atmosphere Overhaul; research mods; Vanilla Fishing Expanded | Its reactor; subsea oil; the radar mast as anti-missile radar; as weather radar; research; a catch | Not built | The radar rows **move to the air-defence record**; the rest unchanged |
| [Utility vehicles](utility-vehicles-design.md) | RimThunder; Rimefeller; Vanilla Helixien Gas Expanded; Rails and Roads; Vanilla Gravship Expanded | Engine-class nodes; crude through its pipes; gas port; road paver; crane | Ideas | Crude through pipes **becomes SD4**; road kinds join SD11; the rest unchanged |
| [Drill rig](vehicle-drill-rig.md), [survey truck](vehicle-survey-truck.md) | Dubs Bad Hygiene; Helixien | Water-well kit; a gas grid | Rejected or low | Unchanged |
| [Generator set](vehicle-generator-set.md) | VOE Power Grid | Connect the set to its outlet | Noted | Unchanged |
| [Vehicle accessories](vehicle-accessories.md) Y1-Y4, Y22 | Dubs Bad Hygiene; Vanilla Brewing Expanded; Field Tents; Use Bedrolls; VFE radios; Combat Extended spotlight; Tier 3 Hermano | Crew water and shower; hot drinks; swags and tents; radios from the outlet; fridge stands down | Not built | Unchanged; **Field Tents is active** (updated 9 Nov, replied 12 Nov), so the camp rows are on firmer ground |
| [Berths](vehicle-berths.md) | Field Tents | Bunks in the caravan bed list | Answered | Unchanged |
| [Hotboxing](vehicle-hotboxing.md) | Drug mods, Ideology | Their smoked drugs through the smoke test | Not built | Unchanged |
| [Parts bills](parts-bills.md) | Rimefeller synthamide and composite | Faired or insulated upgrade | Candidate | Unchanged |
| [Factory additions](vfe-factory-additions.md) | Biological Warfare samples; Vanilla Pirates rumsuit; VAE medic bag; Dubs Bad Hygiene; Anomaly; Biotech; Smithy and DMS printers | Ports and ideas | Not done | Unchanged; Luciferium stays out (owner's choice) |
| [Vaccines and prevention](vaccines-and-prevention.md) | Biological Warfare; Dubs Bad Hygiene; Combat Extended masks | Vaccines; hygiene against disease; masks against airborne disease | Record only | See SD20 |
| [Workstation effects](workstation-environmental-effects.md) | Rimefeller, VFE Factory refiners, Combat Extended smoke | Workstation gases | Record only | Unchanged; SD8 makes any gas from them reach the right rooms |
| [Cross-map strikes](cross-map-strikes.md) | DMS Expedition; InterRim Ballistic Missile | Rules for our cross-map weapons | Proposals | **Joined by** the air-defence record |
| [Radio design](radio-framework-design.md) | Many | Radio tie-ins | Unbuilt | Unchanged: the radio is on the back burner |

## Argued against

| Idea | Why not |
| --- | --- |
| Luciferium Expansion's scarlet sludge as tanker contents | The owner chose to leave Luciferium out of Factory Additions; the mod is dormant (21 Jul 2025, uploader last seen Aug 2025) |
| Vehicles at Work's hauls through our tipper and trailer (SD3) | Its own haul works; the gain is presentation |
| Our own copy of Vehicles at Work's commuting | It exists and is active; SD1 and SD2 make it work with ours instead |

## Other factions' vehicles

- SD1 and SD2 touch only the player's vehicles: Vehicles at Work drives colonists' vehicles.
- SD9 (EMP on electrics) works on any faction's vehicle under the part-hit rule, and is the one row here that changes a raid: EMP against
  raiders' vehicles drains their batteries too. Other factions' vehicles run the lite tier
  ([audit](vehicle-other-factions-audit.md)), so the part rule must be confirmed to run there before this is claimed.
- SD4, SD5 and SD7 need a parked, player-owned vehicle; a captured tanker joins them as any of ours.

## Cost per tick

- XML rows (SD9, SD10, SD14, SD30, SD40, SD41) cost nothing per tick beyond the rules they join.
- SD1 is one check per Vehicles at Work decision, reading our cached refusal; never a recompute.
- SD4, SD5 and SD7 run at the pipe link's existing rare look, as `PipeNetLink` does.
- SD8 adds the duct openings to the room-gas pass, which already runs on rooms changing, not every tick.

## Ranked short list (across this record and the air-defence record)

The ten cheapest links with the clearest play value, XML first where the value is close.

| Rank | Row | Why |
| --- | --- | --- |
| 1 | SD1 Vehicles at Work obeys our refusals | A likely live fault in the owner's game; one postfix |
| 2 | SD9 EMP on electrics | XML only; a real counter to vehicles |
| 3 | SD30 Bench-bill templates | XML only; eases the vehicle economy everywhere |
| 4 | AD1 DMS fire control blunts the Harrier ([air defence](air-defence-tie-ins.md)) | One postfix on the A12 hook that is already planned |
| 5 | SD4 The tanker on Rimefeller's pipes | Closes the oil chain the drill rig started |
| 6 | SD5 The tanker refuels buildings | A core feature; VFE Power gives it targets |
| 7 | SD23 The IV drip gives fluids | Uses the hospital the player already built |
| 8 | SD21 Potassium iodide before exposure | Joins two of our features to a mod we already tie in |
| 9 | SD22 Search and Rescue's medics with our devices | High value in CE battles; larger |
| 10 | AD3 Our aircraft seen by DMS world-map defence ([air defence](air-defence-tie-ins.md)) | Gives the whole aircraft programme an opponent |

## Questions for the owner

Answered by the owner, 13 Nov 2026:

- **SD1: fix it now**, ahead of everything else. **Built: Framework 0.108.4**, not yet tested in game; the depot bay's Go to bay had the
  same gap and takes the same shared refusal.
- **The next batch**: the vehicle picks (SD4 the tanker on Rimefeller's pipes, SD5 the tanker refuels fuelled buildings, SD9 EMP on
  electrics), the factory pick (SD30 bench-bill templates), the medical picks (SD21 potassium iodide, SD22 Search and Rescue's medics,
  SD23 the IV drip) and the gas pick (SD8 Vanilla Temperature Expanded's ducts).

**Built on 13 Nov 2026 (one batched release; not yet tested in game):**

- **SD9 EMP on vehicles**, Framework 0.109.0 and Vehicles 0.52.0. **Not XML only, as recorded**: Vehicle Framework's own EMP model acts only
  on parts their authors rated, no vehicle mod on the owner's list rates any, so EMP did nothing to any vehicle. The owner chose (13 Nov
  2026) hardening by class (armed is military, steam is mechanical), Vehicle Framework's stall scaled by it, and RimThunder's vehicles never
  stalled but hardened, as its authors switched stalls off. Ion Weaponry was left out: its ion bullet acts on pawns only.
- **SD5 the tanker fills generators**, Framework 0.109.0 and Vehicles 0.52.0.
- **SD4 the tanker on Rimefeller's pipeline**, Framework 0.109.0 and Vehicles 0.52.0: Rimefeller's hidden pipeline laid beneath the parked
  tanker, crude and chemfuel pumped through it. One unit of crude to one stored unit is authored.
- **SD30, narrowed to our vehicle parts bench**, Factory Additions 0.13.0. VFE Production's assembly bench adds nothing (VFE Factory's own
  assembler already makes components), and RimThunder's assembly platform makes its parts by separate recipes, which the bill maker does
  not read; teaching it those was set aside (RimThunder Core is quiet, and its parts are only stand-ins for ours).
- **SD23 the drip stand**, Medical 0.16.0, at quality 0.7 (authored, below the fluids kit's 0.9).
- **SD21 iodide at the radiation line**, Framework 0.109.0 (a preventive dose any protection line can ask for) and Medical 0.16.0. Read from
  Rimatomics 1.7: its iodide does not block a dose (that stat is commented out); it takes 0.1 a day off radiation sickness for about five
  days. Treatment of a resting patient and fallout over a whole map are left to the game's drug policies.
- **SD8 set aside: nothing to build.** Vanilla Temperature Expanded's wall-mounted vent is the game's own vent class, so our gases already
  pass through it; its ducts are an air-conditioning network between AC units and compressors and join no rooms' air.

Still open:

1. **SD20**: wait on Biological Warfare's quiet author, or build the vaccine work anyway since Factory Additions already makes its items?
2. **SD6**: worth a decompile of VFE Power to see what its overloaded reactor does to people?
3. Any of the mods not on your list worth adding to your game (Helixien gas above all)? Their player comments would be read first.

## Who chose what

- **Owner (13 Nov 2026)**: the scope, candidates, record-only outcome and both directions, as above.
- **Agent choices**: the row numbers (SD1 onward), every verdict, the ranked short list, and the reading of Vehicles at Work's code as a
  likely fault. All are for the owner to revise.

## Sources

- Other mods' code: decompiles in `.local/decompiled` (never committed), read with `scripts/query-code.py` on 13 Nov 2026: Vehicles at Work
  (installed 11 Nov 2026 build), Vehicle Framework 1.6 (`VF`).
- Other mods' Defs: `scripts/query-defs.py` over the owner's installed mods.
- Author activity: `scripts/workshop-research.py report`, 13 Nov 2026, from Steam's public item and comment data.
- Surveys of our own records and folders: [the tie-in register](tie-in-register.md).
