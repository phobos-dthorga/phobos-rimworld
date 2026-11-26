# Working trailers: the shared base and a catalogue (13 Nov 2026)

**Status (22 Nov 2026):** built in D'thorga Framework 0.111.0, 0.121.0, 0.133.0 and 0.216.0 (and Vehicles 0.54.0 to 0.56.0, 0.59.0 and 0.97.0): set up and pack up by hand (B0.1), the hitch that respects it (B0.2), the power lead (B0.3), set-out pieces and canvas rooms (B0.4), the Pintle flatbed at 3 x 7, the water and fuel bowsers (Q3, Q4), the Billy field kitchen (Q1), the shower trailer (Q7) a trailer's own systems (tyres, our Health tab), water on the road ([what was built](#water-on-the-road-22-nov-2026)) and the flatbed as a camper trailer, and (Framework 0.283.0, Vehicles 0.118.0) the tank trailers' electric pump and the shower trailer's waste tank ([what was built](#tank-trailers-pumps-and-the-shower-trailers-waste-framework-02830-vehicles-01180-25-nov-2026)); not built: the dry-goods locker and every other trailer in the catalogue. Not yet tested in game.

How camping and life aboard work now: [systems/camping-and-life-aboard.md](systems/camping-and-life-aboard.md); this record keeps its history.

How hitches and trailers work now: [systems/hitches-and-trailers.md](systems/hitches-and-trailers.md); this record keeps its history.

**Vehicle Framework, 17 Nov 2026:** VF's own 1.7 build shipped on 14 Nov 2026 (`dependency-audit-2026-11-16.md`); the VF line
references below were read from its 1.6 build, as written at the time.

**A research and design record.** "What was built" below has the first batch. The rest is designed, not built. The owner, 13 November 2026: "I want to do
a research and record purview on utilitarian trailers", with two ideas of their own, **a military-style field kitchen on wheels
powered by the Busbar as required** ([vehicle-field-kitchen.md](vehicle-field-kitchen.md)) and **something to transport prisoners
with** ([vehicle-prisoner-transport.md](vehicle-prisoner-transport.md)), and asked for the agent's ideas too. This record holds
the **shared base** every working trailer needs and the **catalogue** of the rest.

Facts marked **[code]** were read on 13 Nov 2026 with `scripts/query-code.py`, `scripts/query-defs.py` and our own source and are
summarised in our own words. **[A]** is an authored starting value; **[S]** is a real-world figure with its source below;
**[I]** is our inference. Vehicle Framework references are from its **1.6** decompile, the only one we hold.

## Read this first

- **No active mod on the owner's list has a working trailer of any kind** (kitchen, prisoner, water, fuel, cold store, medical,
  workshop, livestock, boat). The only trailer anyone can build is our own Pintle flatbed. Everything here is new ground.
- **One shared base does most of the work.** A trailer today has none of our vehicle systems and cannot be "set up". Five
  pieces, built once, turn the Pintle's chassis into any working trailer: **set up and pack up**, **a hitch that respects it**,
  **a power lead**, **set-out pieces** (real buildings placed beside it, a canvas room among them) and **stores**.
- **Trailers do their work through the game's own buildings.** A kitchen trailer sets out a real cooking range; an aid post sets
  out real hospital beds. The game's jobs, recipes and other mods' content then work unchanged. This is the main design choice:
  a vehicle cannot sensibly take cooking bills or be a bed [code], but a building beside it can.
- **The owner's chosen families**: Supply (water bowser, fuel bowser, cold store), Care (aid post, shower and latrine, crib
  room), Work (field workshop, field laboratory, light tower, stone saw, bridge layer), and the livestock trailer.
- **The owner's rules apply**: every trailer uses up something real; stores run down and are refilled by the player's people;
  passive gear costs materials and weight only; no drag; every vehicle of ours is upgradeable from its first Def; every
  mechanic works for other factions' vehicles; no stand-in body art.

## What the research found

### Our hitch and trailer today [code]

- **The Pintle flatbed** (`mods/PhobosVehicles/Defs/Phobos/Vehicles/FlatbedTrailer/Vehicle_FlatbedTrailer.xml`), as read on 13 Nov
  2026 (**since Vehicles 0.54.0 it is 3 x 7 cells, 2,800 kg empty, 5,000 kg of cargo**): 2 x 5 cells,
  2,000 kg empty, 3,000 kg of cargo, no seats; parts chassis, deck, drawbar, axles, lights; one upgrade (greedy boards).
- **The hitch** (`src/PhobosFramework.Vehicles/Hitch/`, Framework 0.101.0 to 0.104.0, untested): any land vehicle with a
  loadout can tow; the trailer stays on the map and can be shot; a colonist couples it in about a third of an hour; the towed
  weight is felt by the drivetrain; on the world map the trailer rides in the hitch's own holder and is set down behind the
  tractor on arrival. Its record already foresaw this: "tank, generator, tipping and camp trailers are the same trailer with a
  different deck" (`vehicle-hitch-design.md:114-122`).
- **What a trailer lacks:** a trailer gets no loadout, so no `CompPhobosVehicleSystems`: no Dashboard, fittings, electrics,
  conduits beneath it or shore power (`src/PhobosFramework.Vehicles/Service/VehicleSystems.cs:54-55`). It has no fuel comp, so it
  cannot be drafted. **Changed in Framework 0.133.0:** a trailer now has a loadout of its own, tyres only
  ([A trailer's own systems](#a-trailers-own-systems-18-nov-2026)).
- **Set-up today needs crew and a running engine:** `CompPhobosSetUp` refuses otherwise
  (`src/PhobosFramework.Vehicles/Systems/CompPhobosSetUp.cs:320-325`), which a trailer can never satisfy.

### Pieces we can reuse [code]

| Piece | Where | What a trailer gets from it |
| --- | --- | --- |
| **Power point** | `CompPhobosRigWork`, `RigWork.xml:246-286` | A 1 x 1 consumer set beside the vehicle that wires to any conduit or generator within 6 cells, the Busbar's cable end included; its draw is set each rare tick |
| **Rig work** | `WorkGiver_PhobosRigWork`, `CompPhobosRigWork.cs:330-401` | A colonist works at a parked vehicle from a cell beside it, trains a skill, sees a progress bar |
| **Tank** | `CompPhobosTank` | Any fluid by Def; **already works without vehicle systems**, reading the cell's temperature instead (`CompPhobosTank.cs:175-182`); ports to plumbing, pipes, items, a drill rig, and **refuelling any vehicle within hose reach** (`TankPorts.cs:79-101`) |
| **Holder** | `CompPhobosLitter` | People held in our own holder, loaded by a carry job, tended, set down when unloaded |
| **Set-out building** | The Busbar's cable end, the rig's power point | A building spawned beside a set-up vehicle and removed when packed |
| **Canvas roof** | Field Tents (another author's mod, read only) | Proof that a packable building can roof cells and make a real room |

### The Busbar, for every trailer that takes power

The Busbar gives **2,000 W** (2,650 W uprated) at 15 chemfuel a day at full output, with a 12-cell cable reel; its cable end is a
building on the game's power net (`Vehicle_GeneratorSet.xml`, `CompPhobosGeneratorSet.cs:355-432`). A trailer's power point within
6 cells of that cable end, or of any conduit, draws from it. **Nothing new is needed for "powered by the Busbar".** The owner
turned a towed generator down (`vehicle-generator-set.md:194-197`), so no trailer here makes power except the light tower's own
small engine.

## The shared base (B0)

Five pieces of new code, written once in the Framework, used by every trailer below.

### B0.1 Set up and pack up, with no crew and no engine

- **Order:** a **Set up here** button on a parked, unhitched trailer of the player's. A colonist (Construction) lowers the legs
  and unpacks in a set time per trailer [A: 1,500 to 4,000 work], like hitching. **Pack up** reverses it.
- **Refusals, each with its reason:** still hitched; ground blocked on the side the pieces go; a set-out piece in use (a bed
  occupied, a bill in progress) or holding things.
- **Rule:** a subclass of today's set-up comp that waives its crew-and-engine test for trailers; everything else (the saved flag,
  refusing to move while set up, placement helpers) is reused.

### B0.2 A hitch that respects set-up

- **Today the hitch moves a towed vehicle without asking whether it is set up** (`CompPhobosHitch.cs:272-299`; found on the way,
  listed under Faults). The base adds the check: coupling a set-up trailer is refused ("Pack it up first"), and a tractor that
  tries to drive off with one still coupled is stopped with the same words.

### B0.3 A power lead

- A trailer that takes power sets out the rig's **power point** on its side when set up. Its draw is the sum of what is
  switched on. **Without power, each trailer says plainly what stops** ("No power: the cold store is warming").
- **The Busbar link is free**: a Busbar parked within reach with its cable end near, or a grid conduit, powers it.

### B0.4 Set-out pieces

- Each trailer Def lists the **buildings it sets out** and where, relative to its body (left, right, rear; in cells). Packing
  removes them; each is held by the trailer while packed, so a save holds it once. This is the same service the accessories
  record's camp order needs (`vehicle-accessories.md`, Y3): build it once.
- **A canvas room** is one kind of piece: walls and a canvas roof set out together, making **a real room** in the game's eyes
  (the way Field Tents does it). A real room is what lifts the outdoor penalties: work tables outdoors work at x0.8 and outdoor
  cooking rolls a flat 2% food poisoning [code, `full:all.cs:715046-715084`, `91079-91135`]. **Agent recommendation:** the
  kitchen, aid post, crib room and laboratory each set out a canvas room; the rest set out open pieces.
- **Engineering rules carried over:** pieces are placed on the tick after the order, never during spawn set-up (the sling's
  landing fault); nothing of the player's is ever deleted (an item left inside a packed room is set down beside it).
- **If the trailer is destroyed while set up**, its pieces stay as ordinary buildings, now unowned by any trailer.

### B0.5 Stores

- A store is a tank or a locker on the trailer with a **size**, a **rate it empties at**, **how it is refilled** and a **plain
  warning when low** (the accessories rule). Fluids use the tank system as it is; dry goods use a locker that takes chosen items
  from the cargo.

### B0.6 Trailer sizes

**Owner, 13 Nov 2026: "3 x 7 is the bare minimum, and at least vehicle sized or near for the other trailers."** There is no
small chassis: the 2 x 5 proposed here is dropped.

- **The floor is 3 x 7**, the Pintle flatbed's size since Vehicles 0.54.0 and the survey truck's. No trailer of ours is smaller,
  the livestock and prisoner trailers included.
- **Every other trailer is the size of a truck or near it.** Our trucks are 3 x 7 (the survey truck), 3 x 8 (the Busbar, the Talus
  tipper) and 3 x 10 (the Ullage tanker, the drill rig) [code, their Defs]. **Agent's reading, owner to revise:** the working
  trailers are 3 wide and 7 to 8 long, 3 x 8 (the Busbar's and the tipper's length) for the big ones (the kitchen, the aid post,
  the bowsers, the crib room); each trailer's length is fixed in its own Def before its picture.
- **Figures to revisit at that size:** the bowsers' 1,500 L and 1,200 L, the prisoner trailer's six cells and the livestock
  trailer's stalls were set for the smaller chassis [A]; each is set again from its trailer's size when it is built.
- Before any trailer's picture is made, its size is **measured against the biggest comparable vehicle on the owner's list and
  generated at 1.5 times its drawn size or more** (owner's rule). Each body has its own upgrade tree and parts bill from its first
  Def.

```mermaid
flowchart LR
  tractor[Any tractor with our hitch] --> trailer[Trailer]
  trailer --> setup["B0.1 Set up and pack up"]
  setup --> pieces["B0.4 Set-out pieces: a range, beds, a canvas room"]
  setup --> lead["B0.3 Power lead"]
  busbar["Busbar cable end or a grid conduit"] --> lead
  lead --> pieces
  stores["B0.5 Stores: water, fuel, food, parts"] --> pieces
  refill["Refilled by the player's people"] --> stores
```

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Set up a working site anywhere a truck can tow to | Moving a set-up trailer without packing it | A camp becomes a convoy of purpose: one tractor, then the trailers the job needs |

## The catalogue

Every idea, with its ID for the outstanding-work list. "Beyond the base" says what each needs that B0 does not give. **Bold** IDs
are the owner's two and the families the owner chose to design in depth.

| ID | Trailer | What it does | Uses up | Beyond the base |
| --- | --- | --- | --- | --- |
| **Q1** | **Field kitchen** | Cooks for a camp; burners always, more with power | Fuel, water, food | Its own record |
| **Q2** | **Prisoner transport** | Holds prisoners or captives securely | Rations, water, fuel for a heater | Its own record |
| **Q3** | **Water bowser** | 1,500 L of water to where it is needed | Water | None: the tank as it is |
| **Q4** | **Fuel bowser** | Chemfuel to vehicles and the Busbar in the field | Chemfuel | None: the tank and its refuel port as they are |
| **Q5** | **Cold store** | Keeps cargo cold with power | Watts or its own fuel | A cold answer for its cargo |
| **Q6** | **Aid post** | Hospital beds in a canvas room, lit and powered | Medicine, watts | None beyond B0 |
| **Q7** | **Shower and latrine** | Washing and toilets in the field (Dubs Bad Hygiene) | Water, fuel to heat it; waste to empty | Dubs' water storage, unread |
| **Q8** | **Crib room** | A warm room with a table and a tea urn | Fuel or watts; tea, food | None beyond B0 |
| **Q9** | **Field workshop** | Faster repairs and services nearby; a spares shelf | Parts, watts | A repair factor near it |
| **Q10** | **Field laboratory** | Research away from home | Watts | None beyond B0 |
| **Q11** | **Light tower** | Lights a work site or a defence at night | Its own chemfuel, or watts | None beyond B0 |
| **Q12** | **Stone saw** | Cuts stone chunks into blocks at the quarry | Watts | None beyond B0 |
| **Q13** | **Bridge layer** | Lays a crossing over water and takes it up | Work; the bridge itself | Placing and lifting a bridge |
| **Q14** | **Livestock trailer** | Carries animals at vehicle speed | Feed and water | Animals' food aboard |
| Q15 | Boat trailer | Carries a small boat overland and launches it | — | Loading a vehicle onto a deck; launch at the shore |
| Q16 | Low-loader | Carries a broken or slow vehicle | — | The same deck as Q15 |
| Q17 | Drawbar tipper and tank trailers | A second body behind our own Talus tipper and Ullage tanker | — | Our tipper's and tanker's comps on a trailer |
| Q18 | Battery trailer | Batteries charged at base and drained at camp | Watts | A store of watts |
| Q19 | Ordnance trailer | Ammunition and shells for a gun line (Combat Extended) | — | None: cargo with explosive parts |

**Left out, with reasons:** a towed generator (the owner kept the Busbar self-propelled); a radio or command trailer (radio is on
the back burner); a camp trailer (the accessories record's camp order and swags cover it); anything that only changes speed or
drag (the owner: no drag).

## The families in depth

### Supply

#### Q3. Water bowser

- **What it is:** a tank trailer of water. The US Army's M149 "water buffalo" carries 400 US gallons (about 1,514 L); it weighs
  about 3,000 lb empty and carries another 3,300 lb of water when full, and was designed for 2.5-ton trucks [S]. Ours: **1,500 L**
  [A from the M149], about 1,300 kg empty [A], on the tandem chassis.
- **How:** the tank system as it is, with the water contents Def; filled from plumbing, our tanker, or a pump into open water [A];
  it remembers the worst water it was filled with (built). Feeds the field kitchen, the shower trailer and, where Dubs Bad
  Hygiene is active, plumbing beside it. **It never fills itself.**
- **Set-up:** none needed to pump; parked is enough.
- **Without Dubs Bad Hygiene:** it still feeds the kitchen's water (Q1) and the tanker's existing water uses.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Run a camp, a kitchen and showers far from any well | Nothing | Water is planned and hauled, as it is in real field work |

#### Q4. Fuel bowser

- **What it is:** a tank trailer of chemfuel, the field refuelling trailer armies call a bowser (the word comes from Sylvanus
  Bowser, credited with the fuel pump; in the UK a "bowser" is also an emergency water tank [S]).
- **How:** the tank system and its **existing refuel port**, which fills any vehicle within hose reach (`Port_VehicleFuel`). 1,200 L
  [A]. Refilled at base from items, a pipe net or our tanker.
- **Why it earns a trailer when the Ullage tanker exists:** the tanker is a truck that takes a driver and a vehicle slot; a bowser
  rides behind the Busbar or any truck already going, and stays at a camp while the truck leaves.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Keep a convoy and a Busbar running far from base | Nothing | Fuel becomes a supply line rather than a range limit |

#### Q5. Cold store

- **What it is:** an insulated box trailer with a refrigeration unit. A real shipping reefer runs from shore power or a diesel
  generator set clipped to it, and holds anything from −65 °C to 40 °C [S]. Ours keeps its cargo at **−5 °C** (frozen) or
  **4 °C** (chilled), the player's switch [A].
- **How:** the game asks a held thing's holders for a temperature before the weather [code, `RimWorld-1.7:Verse.Thing.cs:506-534`];
  we already answer for riders (`HeldTemperaturePatch`). The cold store answers for **its own cargo**. With power from its lead it
  holds the set point; **its own small diesel unit** burns chemfuel when no power is near [A: 3 chemfuel a day]; with neither, it
  warms towards the weather, slowly at first [A], and says so.
- **Uses:** fresh meat and vegetables for the field kitchen; a hunting or butchering camp; and, darkly, **a field mortuary**:
  corpses kept fresh for a funeral at home or for an ideology's rites. **Whether Vehicle Framework's cargo takes corpses was not
  read**; to check before building.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Bring a hunt home fresh; feed a kitchen fresh food for weeks | Forgetting the power: a flat cold store spoils the load | Cold is paid for in watts or chemfuel every hour |

### Care

#### Q6. Aid post

- **What it is:** a medical trailer that sets out a **canvas room with two hospital beds** [A], a lamp, and the game's vitals
  monitor beside them, linked to the beds as the game links it (powered through the lead).
- **How:** the beds are the game's own medical beds, so doctoring, tending, operations, rest and infection all run as they do at
  home, with the canvas room giving them real room cleanliness. Power for the lamp and monitor comes through its lead. Medicine
  comes from a locker stocked from the cargo.
- **Real-world sense:** a regimental aid post is a medic team's first stop under canvas behind the line; ours is that, not a
  field hospital.
- **Why not our litter kit:** the litter kit is an aircraft fitting that holds patients aboard; the aid post is where they are
  treated properly.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Treat and operate in the field in a clean room | Nothing | Expeditions can survive a bad fight without carrying the wounded home first |

#### Q7. Shower and latrine trailer (with Dubs Bad Hygiene only)

- **What it is:** a trailer that sets out showers and toilets under canvas, as the military's shower trailers do
  (`military-vehicles-research.md:110-124`, the DDA-66).
- **How:** Dubs Bad Hygiene's own fixtures as set-out pieces, fed from the trailer's own tank or a water bowser beside it, with a
  heater burning chemfuel for hot water [A]. Its waste tank fills and is emptied at a septic point (our tanker already has a
  septic port).
- **The open question:** how Dubs' fixtures would draw from a tank on a vehicle. Its water storage code was **not read**; that is
  the first check before building (also owed to the accessories record, row Y22).

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Keep hygiene up on long operations | Nothing | Water and waste become part of the camp's plan |

#### Q8. Crib room

- **What it is:** the Australian crib room or smoko shed on wheels: **a canvas room with a table, chairs, a heater and a tea urn**,
  somewhere warm to sit, eat and talk at a work site.
- **How:** set-out pieces only. The table is a gathering spot and a dining table, so people who eat there avoid the game's "ate
  without a table"; the room is a real room for recreation and warmth. The **urn** is a small work table that makes hot tea or
  coffee from leaves or beans in its locker [A]; with Vanilla Brewing Expanded active it takes their hot drinks' recipes. The
  heater burns chemfuel, or uses watts from its lead.
- **Not a kitchen:** it serves, it does not cook meals.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Keep a work crew's mood up far from home | Nothing | Long jobs (a quarry, a mine, a siege) get a proper break room |

### Work

#### Q9. Field workshop

- **What it is:** a workshop trailer with a bench, tools and a spares shelf, as recovery and maintenance units carry.
- **How:** set up, it sets out a **spares shelf** (as the depot bay has, built) and powers a **tool rack**. Repairs and services
  on vehicles within **8 cells** [A] go **1.5 times faster** [A] while it has power, and draw parts from its shelf. Our own
  service kits can be made at its bench (the parts bench's simpler recipes) [A].

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Keep a convoy running far from the motor pool | Nothing | Breakdowns in the field are an afternoon, not a tow home |

#### Q10. Field laboratory

- **What it is:** a canvas-room laboratory: the game's simple research bench and, with power, the multi-analyser link [A].
- **How:** set-out pieces only; the game's research runs as at home. **Value, honestly:** research is a home activity in most
  colonies; this trailer suits outposts, long expeditions and the survey truck's field research
  (`FieldResearchExtension`). The record lists it for completeness; the owner may judge it low.

#### Q11. Light tower

- **What it is:** a mast of lamps on a trailer. Real ones carry metal halide lamps on a mast, powered by their own diesel
  generator set; battery, solar and hydrogen sets also exist [S].
- **How:** set up, the mast rises (a moving part, like the survey truck's boom) and lights a wide circle [A: radius 14 cells,
  brighter than our work lamps]. **Its own small engine** burns chemfuel [A: 4 a day]; with a power lead it uses watts instead
  [A: 600 W].
- **Value:** night work and night defence (Combat Extended's darkness penalties make light matter more; unverified for CE's
  current rules).

#### Q12. Stone saw

- **What it is:** a powered stone saw for the quarry, so blocks are cut where the stone is.
- **How:** set out, it is a work table taking the game's stonecutting recipes (copied from any table that cuts stone, found by what
  the recipes make, never by name), faster than the stonecutter's table while powered [A: x1.5], at the hand rate without.
- **Value:** the Talus tipper then hauls blocks instead of chunks; how much lighter a load of blocks is than its chunks was **not
  read**, so the gain is unmeasured.

#### Q13. Bridge layer

- **What it is:** a trailer carrying a folding bridge, laid over water and lifted again. Real armoured bridge layers carry spans of
  19 m or more; the US Wolverine's 26 m bridge goes down in about four minutes [S].
- **How:** set up at a water's edge, a colonist lays the bridge over up to **8 cells** of shallow or deep water [A]; the cells
  become bridge for as long as it is down; packing lifts it, refusing while anything stands on it.
- **What must be checked before building:** that Vehicle Framework's own path grid treats our bridge as passable for vehicles, and
  what the game does with bridges over deep water. Neither was read.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Take vehicles across a river where there is no ford or bridge | Nothing | Rivers stop being walls for a convoy |

### Q14. Livestock trailer

- **What it is:** a stock crate on the Pintle chassis. Road livestock trailers are the ordinary way animals are moved today [S].
- **How:** Vehicle Framework already lets animals ride in any seat with no driving or gunner duty [code,
  `VehicleFramework-1.6:Vehicles.decompiled.cs:17131`], so the trailer has **stalls**: seats that take animals only (a small patch
  of ours on the seat check, shared with the prisoner trailer's cells). Stalls by body size: one large animal (a horse, a cow), or
  three small ones [A].
- **Feeding:** nobody feeds an animal aboard on a map; our rider check (the accessories record's "life aboard", Y4) feeds them
  hay or kibble from the trailer's feed locker, and water where Dubs Bad Hygiene gives animals thirst.
- **What it lets the player do:** move breeding stock to an outpost, take animals to market at vehicle speed, bring a tamed animal
  home from far away without a walk.
- **To check:** whether a vehicle caravan is slowed by animals walking beside it, which decides how much the trailer gains.

### Briefly: Q15 to Q19

- **Q15 Boat trailer and Q16 low-loader** share one need: carrying a whole vehicle on a deck and setting it down again (the
  HEMTT's deck in RimThunder does this; nothing of ours does). The boat trailer adds launching at the shore. Small boats on the
  owner's list: Vanilla Vehicles Expanded's dinghy (2 x 4) and trawler (2 x 5).
- **Q17 drawbar trailers:** a second tipping or tank body behind our own Talus and Ullage, as road trains do.
- **Q18 battery trailer:** in the utility vehicles catalogue already; overlaps the accessories record's house battery.
- **Q19 ordnance trailer:** a cargo trailer whose explosive parts make it dangerous when hit (Combat Extended's ammunition).

## Other factions' vehicles

| Where | What happens | Lets the player | Stops the player |
| --- | --- | --- | --- |
| **A raid on us** | Raiders bring no working trailer except the prisoner transport ([its record](vehicle-prisoner-transport.md)). Nothing else would change a raid | — | — |
| **A base we attack** | A base may have **parked, set-up trailers** placed when its map is made: a kitchen with its canvas room, a bowser, a light tower lighting the yard at night, a cold store [A: by the faction's tech level] | Loot them, capture them whole, or shoot the bowser | Easy night raids where a light tower stands |
| **A vehicle we capture** | A trailer works for us at once, its stores as found | Use it | Nothing |

The base and every trailer read only what any vehicle has. A trailer parked at a base costs nothing per tick until a player is
on the map, and then only what a set-out building costs.

## Cost to run

- **Set-out pieces** are ordinary buildings; they cost what a stove, a bed or a lamp costs.
- **The power lead** reads its draw on the rare tick, as the rig's does.
- **Stores** use the tank's existing rare update.
- **The cold store** adds one lookup for things in its own cargo when the game asks their temperature.
- **Nothing runs per tick.**

## Optional tie-ins

| Mod | What changes |
| --- | --- |
| Dubs Bad Hygiene | The shower and latrine trailer exists only with it; the water bowser feeds its plumbing; animals' thirst |
| Vanilla Brewing Expanded and its Coffees and Teas | The crib room's urn takes their hot drinks |
| Vanilla Cooking Expanded, Tavern | The field kitchen takes their stove recipes ([its record](vehicle-field-kitchen.md)) |
| Combat Extended | Light towers matter more at night (unverified); the ordnance trailer |
| Field Tents | Nothing; its canvas-room approach is the precedent for ours, nothing of it is used |
| Vanilla Vehicles Expanded | Its dinghy and trawler for the boat trailer |

## Art and sound

- **Truck-sized bodies, 3 x 7 at least.** Each trailer is its own picture, drawn bare, with its set-out pieces as separate things beside it
  (never drawn on the trailer). Until its picture exists a trailer borrows one of ours (the Pintle's, the tanker's), as the rule
  allows; no stand-in body art is drawn.
- **Set-out pieces need their own pictures**: the canvas room's walls and roof, the range, the urn, the stalls, the mast.
- **Moving parts:** the light tower's mast and the bridge layer's span, planned like the survey truck's boom.
- **Sounds, each its own:** legs going down, canvas, the reefer's compressor, the light tower's engine, the bridge's hydraulics.

## A suggested order

1. **The base (B0)** with the **water bowser (Q3)** and **fuel bowser (Q4)**: the bowsers need almost nothing beyond the base and
   prove it.
2. **The owner's two**: the field kitchen (Q1) and the prisoner transport (Q2).
3. **Care**: the aid post (Q6) and crib room (Q8); the shower and latrine (Q7) after Dubs' storage is read.
4. **The cold store (Q5)** and the **livestock trailer (Q14)**, which share the rider check with the accessories record.
5. **Work**: the field workshop (Q9), light tower (Q11), stone saw (Q12); the bridge layer (Q13) after its two checks; the field
   laboratory (Q10) if wanted.

## Faults and stale lines found on the way (corrected 17 Nov 2026, but the last)

| What | Where |
| --- | --- |
| ~~The hitch moves a towed vehicle without checking whether it is set up~~ **Fixed in Framework 0.111.0** (B0.2) | `src/PhobosFramework.Vehicles/Hitch/CompPhobosHitch.cs` |
| ~~"Recovery-tow caravans are not built" is stale: the code takes any towed vehicle along~~ **Corrected** (now in `vehicle-upgrades-round-two.md`) | `vehicle-upgrade-families.md:285-286` |
| The hitch record calls the Pintle's 2 x 5 "the generator set's width"; the Busbar is now 3 x 8. **Left as written:** it is a dated record of 12 Nov 2026, true on its day | `assets/phobos-vehicles/requests/round-three-record.md` |
| ~~The Busbar's Def comment is dated 14 Nov 2026; the work was 13 Nov~~ **Corrected** in three places | `Vehicle_GeneratorSet.xml`, near its fuel figures |
| ~~"A vehicle is not an IBillGiver" is not quite right: every pawn is one, with surgery-shaped rules~~ **Corrected** | `vehicle-framework-mechanics.md:159` |
| Whether people riding a towed vehicle count on the colonist bar while travelling is still unchecked | `vehicle-hitch-design.md:271` |

## What was built (13 Nov 2026)

The owner's first batch: "the base with the water and fuel bowsers". Framework 0.111.0 and Vehicles 0.55.0, **not yet tested in game**.

| Piece | Built | Where |
| --- | --- | --- |
| B0.1 Set up and pack up by hand | **Built.** A set-up comp on a trailer waives crew and engine; **Set up** and **Pack up** become orders a colonist with construction work carries out (work set in the trailer's Def: 1,500 and 1,000 on the bowsers [A]); the order is saved, shown in the inspect pane and cancellable with the game's own cancel picture. A towed vehicle cannot be set up | `CompPhobosSetUp` (hand set-up), `Hitch/TrailerSetUp.cs` |
| B0.2 A hitch that respects set-up | **Built**, and it fixes the fault listed below: the hitch refuses a vehicle set up for work or awaiting a colonist to set it up, on the recovery tow too, and a tractor finding what it tows set up stops and says so | `CompPhobosHitch.SetUpRefusal` |
| B0.3 A power lead | **Not built**: nothing in this batch takes power. With the field kitchen | |
| B0.4 Set-out pieces and canvas rooms | **Not built**: the bowsers set out nothing. With the field kitchen | |
| B0.5 Stores | **The fluid half is the tank as it is**, now with a tank built into a vehicle (a fitting with no node) and limited to one fluid (onlyContents). The dry-goods locker and a low-store warning come with the kitchen | `TankDefs.cs` |
| Q3 Water bowser | **Built**, with Dubs Bad Hygiene only (water gear only with Dubs, owner); 3 x 8, 2,500 L, 400 L an hour [A]; fills from plumbing or a tanker, gives to plumbing or a tanker | `mods/PhobosVehicles/Mods/DubsBadHygiene/Defs/WaterBowser` |
| Q4 Fuel bowser | **Built**; 3 x 8, 2,500 L, 400 L an hour [A]; fills from stacks, a chemfuel line or a tanker, gives to vehicles, fuelled buildings or a tanker | `mods/PhobosVehicles/Defs/Phobos/Vehicles/FuelBowser` |
| Tank to tank | **Built** (agent's addition, so "filled from our tanker" in Q3 and Q4 works): a tank on **Load** draws from one beside it on **Unload** holding the same | `CompPhobosTank` |
| Pipes beneath a trailer | **Built** (owner's rule: anything that takes water or piping has its own pipes beneath it): a trailer holds its own pipes, laid while it stands unhitched | `CompPhobosTrailer`, `VehiclePipes.IHost` |

**Changed from the design while building, agent's choices for the owner to revise:**

- **The bowsers are set up** by a colonist, where Q3 said "parked is enough": the tank's pump runs only while set up, and setting up a
  bowser (chocks, legs, hoses) is what B0.1 is for.
- **Sizes and figures re-set from the owner's sizes**: 3 x 8, 2,500 L each (was 1,500 and 1,200 L on the small chassis), 2,000 kg empty,
  400 L an hour.
- **The water bowser exists only with Dubs Bad Hygiene**: without it there is no water for a tank, and the kitchen it would also feed is
  not built.
- **No pump sound** (working machinery is never shared; sound-catalogue.md S129), **a borrowed picture** (the Pintle's, scaled evenly; **the bowsers have their own picture since Vehicles 0.56.0**),
  and **names**: Pintle P2 water bowser and Pintle P3 fuel bowser, the agent's choice.
- **Other factions**: raiders bring none, and none stand at enemy bases yet (question 4 was never put).

## What was built: the field kitchen batch (17 Nov 2026)

Framework 0.121.0 and Vehicles 0.59.0, **not yet tested in game** (checks 152 to 155). The kitchen's own record has its part
([vehicle-field-kitchen.md](vehicle-field-kitchen.md#what-was-built-17-nov-2026)).

| Piece | Built | Where |
| --- | --- | --- |
| B0.3 A power lead | **Built** as the rig's power point, set out with the pieces; pieces read whether it has power, and its draw follows what works | `CompPhobosSetOut` |
| B0.4 Set-out pieces and canvas rooms | **Built.** Pieces and rooms named in the trailer's Def, on whichever side is clear, with refusals saying what is in the way; walls, a flap door and a canvas roof (our own Defs, the game's pictures by path); the roof comes off before its walls; pieces are held inside the trailer while packed. A part of the trailer's set-up, so it follows a tank's set-up or a plain one | `Framework/SetOut/` |
| Q7 Shower and latrine | **Built**, with Dubs Bad Hygiene only (owner, 17 Nov 2026: flushing toilets with a waste tank; a chemfuel heater, power a bonus). The reading of 17 Nov 2026 in [vehicle-accessories.md](archive/vehicle-accessories.md#how-dubs-bad-hygiene-sees-water-and-what-a-vehicles-tank-needs-17-nov-2026) built as proposed: a hidden stand-in tower topped up from the tank, and a hidden stand-in septic tank as the waste tank | `CompPhobosTrailerPlumbing` |

**Changed from the design, agent's choices for the owner to revise:** the shower trailer is the **Pintle P4**, a 2,000 L tank on the bowsers'
chassis, set up through its tank like the bowsers; its tent holds two of Dubs' own showers and two flushing toilets (a third shower by upgrade),
with Dubs' own chemfuel boiler and hot-water store as its water heater outside the tent. **Power's bonus is half the chemfuel**, not faster
heating: Dubs' boiler cannot heat faster than its store fills. The waste tank holds 1,000 of Dubs' sewage units (about 70 flushes) and treats
nothing; Dubs' colonists empty it into sludge drums at half full, as Dubs does a septic tank whose draining is allowed, and our tanker can pump it
from beside the trailer's rear. Its upgrades are the water bowser's two and the third shower. The dry-goods locker is not built.

## A trailer's own systems (18 Nov 2026)

**Built in D'thorga Framework 0.133.0. Not yet tested in game.** The owner's screenshot of the Billy field kitchen showed Vehicle
Framework's plain Health tab (a part list, a blank Mass line) where our vehicles show ours, and asked whether that should be. It was
the design as built on 13 Nov 2026 ("a trailer gets no loadout"), but our Health tab, wear, repairs and fittings had since come to hang
off the same systems comp, so trailers missed more than intended. The agent recommended a slim trailer loadout; **the owner said "You
should go ahead, please"** (18 Nov 2026).

**What it lets the player do:** see a trailer's state on our Health tab (parts grouped with pips, wear lines) and keep its tyres up with
the same tyre service as any vehicle. **What it stops them doing:** towing a trailer forever with no upkeep; worn-through tyres can burst
and hurt its axles. **How it changes play:** trailers join the vehicle maintenance round, and a long tow off the roads shows on them.

| Piece | What it does | Where |
| --- | --- | --- |
| **The trailer loadout** | Tyres only (`PhobosVehicleSystem_RunningGear`) on the trailer's wheeled part; a land vehicle's figures (authored). No engine, fuel, electrics, cab, driver or brakes. Its kind, **Trailers**, has its own switch in the settings window, on | `Loadouts_Trailer.xml` |
| **The selector's trailer switch** | `trailer` is `No` unless given, so no other loadout claims a trailer | `LoadoutSelector` |
| **Moving while towed** | A trailer counts as moving while its tractor's pather moves (the hitch sets the trailer's cell, so its own pather never moves) | `CompPhobosVehicleSystems.TowMoving` |
| **On the world map** | A trailer in the hitch's holder has no ticks: the tractor's rare tick catches it up, moving as the tractor moves | `CatchUpTowedBy` |
| **What it does not take** | No hitch (whatever the loadout says), no Dashboard tab, no pipes from the systems comp (the trailer's own comp lays them), no engine demand (its pumps and set-up never "run the engine"), no dark-driving slowdown (the tractor's driver sees the way) | `VehicleSystems.Apply`, `ParkedForPipes`, `RefreshCaches`, `Input_DarkDriving` |

**Other factions' trailers.** None arrive today (raiders bring no trailers). The detail tiers apply as to any vehicle: another
faction's trailer on a map counts its hits and keeps the light tier's quantities; nothing evolves off the map. A captured trailer starts
its clock on capture, as a captured vehicle does.

**Cost to run.** One more vehicle on the full tier per player trailer: one system with four quantities on the rare tick, and one
dictionary look-up for the tow. A held trailer is caught up once per tractor rare tick.

**Limits and open questions.** The trailer's worn stages lower its own move speed, which nothing reads while it is towed: the real
cost is a burst tyre damaging the axles. **Nothing yet stops a trailer with wrecked axles from being towed**: the hitch never read the
axles, though the systems' "cannot move" message now names them (`Mobility.StoppedByDamage`). Whether a tow should refuse is the
owner's call. **The owner's answer (26 Nov 2026, by card): warn, then allow**, as the unwise orders do (EM29): the tow goes ahead
with a plain warning, and the trailer crawls and wears further. Not built; it is row EM42 of
[emergent play](vehicle-emergent-play.md#h-work-on-the-road-26-nov-2026). The settings window gains a **Trailers** checkbox among the loadout kinds (agent default: on).

## Water on the road (22 Nov 2026)

**Built in D'thorga Framework 0.216.0, with Dubs Bad Hygiene only. Not yet tested in game** (row 325 of
[the in-game test pass](in-game-test-pass.md)). The agent found the gap on 22 Nov 2026, checking how the camps, berths and trailers
meet: a towed water bowser (2,500 L), the shower trailer (2,000 L) and the Billy's own tank gave a caravan nothing. Dubs Bad Hygiene
gives a caravan water only from a packed water butt, bottles, rivers and rain (`DBH-1.7:DubsBadHygiene.Patches/HarmonyPatches.cs:138-213`),
and caravan cooking took no water for its kettle, though on a map each batch takes 10 L. **The owner said build it** (22 Nov 2026:
"Water in tow is used").

| Question | Answer |
| --- | --- |
| What it lets the player do | Cross dry country on the water they carry; wash, and shower, at the night's stop |
| What it stops them doing | Cooking at the kettle on the road with no water aboard (with Dubs Bad Hygiene) |
| How it changes play | Water is planned by the litre for a trip: people, days, meals, and where the next fill is. The bowser earns its tow on a long run |
| What outside the trailers notices | The colonists (thirst, hygiene, Dubs' own dirty-water check), the kitchen's cooks (water as a third limit beside food and fuel), the message log (tanks empty, no water for the kettle) |

**Risk: low** (agent's grade, for the owner to confirm): water is used up, never lost at once, and a caravan without tanks behaves as it
did before. **One thing does get harder, by the owner's choice:** with Dubs Bad Hygiene a caravan's kitchen now needs water. No setting
of its own (agent's choice, for the owner to revise).

| Piece | Where | Notes |
| --- | --- | --- |
| The caravan's water | `CaravanWater` (core), filled by `WaterAboard` in the Dubs tie-in | Every tank compartment holding a fluid that plumbs to Dubs' pipes, on any vehicle driven or in tow, and a kitchen's own tank (`CompPhobosTrailerPlumbing.TakeOwnWater`). Without Dubs, or in its lite mode, there is no source and nothing asks for water |
| Drinking | `CaravanDrinking`, a prefix ahead of Dubs' own on the caravan's needs look (it never skips) | A thirsty member takes **1.5 L** [A] and is refilled, with Dubs' own dirty-water check at the tank's remembered quality. Dubs' look then finds nobody thirsty and leaves bottles alone. Tanks before the kitchen's own water |
| Washing | The same look, only while the caravan stands still | Below 0.3 hygiene [A]: **5 L** to 0.7, the level Dubs' own caravan washing gives; with the shower trailer along, a **20 L** shower to full [A: five minutes at 4 L a minute] |
| Hot water (Framework 0.228.0; row 348) | `WaterAboard.HeatShower`, called from the same look for a shower | **0.25 chemfuel a shower** [A: 20 L warmed by 25 degrees is about 2.1 MJ; what a unit of chemfuel holds is the game's to say], from a packed piece with Dubs' boiler and a fuel tank (the shower trailer's field water heater), the fullest first. Then Dubs' own `WaterTempCheck`: hot eases hypothermia, cold gives its cold-water memory. A message once a caravan when the heater runs dry. A loose end the owner asked to have tidied (22 Nov 2026); before it, road showers were hot for nothing |
| The kettle | `CaravanCookRule.Meals`, a fourth limit | **1 L a meal** [A: the kettle's 10 L for a batch of ten], the kitchen's own tank first. No water, no meals, and a message says so |

**Simplifications, labelled:** people drink on the move (the water is aboard) but wash only at a stop. A shower on the road fills no waste
tank (Dubs' showers make no sewage at home either; only toilets and washing machines do); Dubs keeps a caravan's bladders full itself. A kitchen's own water does not keep its
quality while packed, so it is drunk as clean. Animals with thirst drink the same 1.5 L. The figures are in the vehicle rules Def.

**Other factions:** their caravans are not simulated person by person; nothing applies. **Cost to run:** per caravan member per needs
look, two need reads; a caravan with no water aboard adds one dictionary read, and its tanks are counted at most every 250 ticks.

**Who chose what:** the owner chose that water in tow is used and that the kettle's water is charged; the agent chose every figure,
washing only at a stop, tanks before the kitchen's own water for drinking and the reverse for the kettle.

## Pipes and lines beneath trailers and camps: an audit (22 Nov 2026)

**The owner's report** (22 Nov 2026): "the Billy Field Kitchen is exempt from the Dub's Hygiene plumbing placement and perhaps the
chemfuel lines too ... I'm not sure how many other vehicles/trailers might be affected", and then "nutrient paste lines!". The rule
being checked is the root's: **anything that takes water or piping has its own pipes beneath it**. The agent read every place a
vehicle of ours lays a pipe (`VehiclePipes` and its four links: Dubs' plumbing, Vanilla Expanded Framework's nets, Rimefeller's pipes,
power conduits) against what each vehicle and set-out piece uses.

| Vehicle or piece | Uses | Before 0.219.0 | Since |
| --- | --- | --- | --- |
| Driven vehicles | Water for their own systems' services; their own fuel; shore power | Laid while parked, engine off | Unchanged |
| Bulk tanker | Whatever its compartments hold: chemfuel, **nutrient paste**, water, oil | The contents' own line, while parked | Unchanged: **a tanker of paste already lays the paste line** |
| Drill rig | Its outlets' nets | Laid while parked | Unchanged |
| Water and fuel bowsers | Their tank's water or chemfuel | Laid while unhitched | Unchanged |
| Shower trailer | Its tank's water; its boiler's chemfuel | Plumbing while unhitched; **no chemfuel pipe** under the boiler | The boiler's chemfuel line is laid beneath the set-out area |
| **Billy field kitchen** | Its own water; its range's, kettle's and heater's chemfuel or wood | Plumbing and water tank **only while set up**; **no chemfuel pipe** | Plumbing and water tank whenever parked; chemfuel line beneath the whole set-out area |
| Pintle flatbed | Nothing piped | Nothing | Unchanged |
| A camp (any vehicle) | The camp heater's chemfuel; the kitchen box's, once built | **No chemfuel pipe** | The line is laid beneath the whole camp, tents' floors and walls included |
| Nutrient paste | Nothing of ours but the tanker uses it | The tanker lays it | A paste tap or feeder set out by a trailer or camp would get its line by the same rule; none is set out today |

**How the fix works** (Framework 0.219.0, row 336, not yet tested in game):

- **The Billy's water:** `PlumbingLink` counts a trailer with its own water as taking water, and tells it at each look whether it is
  parked; `CompPhobosTrailerPlumbing.SettleOwnWater` keeps its hidden tower beneath it while parked, and takes it up when hitched,
  the water going back aboard. Packing up keeps the tower while the trailer stays parked.
- **Lines beneath what is set out:** `PipeNetLink.SetOutNets` reads every piece a trailer or camp has standing. A piece whose fuel a
  net refills (Vanilla Expanded Framework already gives every such refuelable its own refill link, `AutoLinkRefuelables`) or that holds
  a resource of a net wants that net; its hidden pipe is then laid under every cell of the vehicle, its set-out pieces and walls, and
  its rooms' floors. Nothing is matched by name.
- **Risk: low** (agent's grade): hidden pipes only where none is, never touching another pipe, and taken up with the pieces. No setting.

## Food in tow (Framework 0.219.0, 22 Nov 2026)

**The gap:** a towed trailer rides in its hitch, outside the caravan, so the game's caravan feeding never looked in its cargo. Found
by the agent on 21 Nov 2026 and listed as open; the owner said proceed on 22 Nov 2026. Row 334 of
[the in-game test pass](in-game-test-pass.md). Not yet tested in game.

- **What it lets the player do:** carry a caravan's food in a trailer. **What it stops:** nothing. **How it changes play:** a flatbed
  of food is a larder for a long trip. **What outside notices:** the colonists (fed), the trailer's cargo (it empties).
- **How:** a postfix on the game's `CaravanInventoryUtility.TryGetBestFood`, which only acts when the packs gave nothing: it searches
  every towed vehicle's cargo (`Hitches.InTow`) with the game's own `CanEatForNutritionNow` and `GetFoodScore`, and names the trailer
  as the food's owner, so the game takes an eaten stack out of that cargo itself. **Risk: low** (agent's grade); no setting.
- **Limits:** drugs, medicine and Dubs Bad Hygiene's water bottles in a trailer are not reached; the tanks' water is (`CaravanWater`).
- **The days of food count it too (Framework 0.228.0, 22 Nov 2026; row 346, not yet tested in game).** The first build left the
  caravan's days-of-food figure, its days until rot and so its low-food warning reading the packs only: a loose end the owner asked to
  have tidied. Postfixes on the game's `DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(Caravan)` and
  `DaysUntilRotCalculator.ApproxDaysUntilRot(Caravan)` run the game's own sums again with the towed cargo's food added
  (`TowedLarderDays`, `TowedLarderRot`); a caravan that tows no food keeps the game's figures untouched. The caravan keeps its figure
  for 3,000 ticks, as the game does, so it follows the cargo within an hour or so. **The forming window too (Framework 0.236.0; row 365):** when its figure
  is worked out again, the game's sum is run with and without the food in the chosen vehicles' trailers and the difference added to Vehicle
  Framework's figure, so its route and speed stay in it (`TowedLarderForming`); a trailer chosen to go by itself is counted already.

## Tank trailers' pumps and the shower trailer's waste (Framework 0.283.0, Vehicles 0.118.0, 25 Nov 2026)

The owner asked (25 Nov 2026) for the Billy and the shower trailer to take water without setting up, for showers to add nothing to the
waste tank, and for ways to empty the waste tank set up or not; then, mid-work, "What if we introduce an electrical pump upgrade?".
Their cards of the same day chose everything below marked **owner**.

**What was found first** (read in Dubs Bad Hygiene 1.7 with `query-code`):

- **Showers add no waste**: only toilets, latrines and washing machines call `PlumbingNet.PushSewage` (`Building_toilet.TryUseFlush`,
  `Building_Latrine.TryUseFlush`, `Building_WashingMachine.Tick`); a basin's and a bath's `sewageUsedPerTick` are never read. Our road
  washing adds nothing either. Nothing to change.
- **The Billy already fills while packed** (Framework 0.219.0, row 336): its own water's hidden tank lies beneath it whenever it is
  parked. Nothing to change; row 336 waits on the owner's look.
- **The shower trailer's pump ran only while set up**, and its set-up is the tent.
- **Fault: the waste tank outranked Dubs' septic tanks.** Its stand-in had priority 2 (Dubs' septic tank 1, treatment plant 2, outlet
  0), and `PushSewage` sends all waste to the highest unblocked priority on a net, so a set-up shower trailer joined to the colony's
  plumbing took the colony's toilets' waste. The owner had noticed it too.
- **A trailer's pump needed no power at all**: no engine, yet 400 L an hour for free.

### What was built

| Piece | Choice | Where |
| --- | --- | --- |
| The shower trailer pumps while packed: Load, Unload and Hold, parked or on a hitch whose vehicle has stopped | owner | `CompPhobosTank.PumpsPackedNow`, `pumpsPacked` |
| An **Electric pump** upgrade on all four Pintle tank trailers (water, fuel and paste bowsers, shower trailer) | owner | the four trees and bills |
| Without it, or without power, a tank trailer works by gravity: it can be filled, and emptied by a pump beside it, but pushes nothing out | owner | `TankRule.Pushes`, `TankRule.Transfers` |
| Its power: the set-out power point, the grid through the hook-up conduit beneath it when parked, or the towing vehicle's battery | owner (point and tow battery); grid hook-up for "power point when parked" is the agent's reading | `PumpPowerSource`, `VehicleConduits`, `TrailerPumpInTow` |
| The waste tank stays beneath the trailer whenever it is parked, packed or not | owner | `CompPhobosTrailerPlumbing.SettleOwnWater` |
| Its priority drops to -1, below everything of Dubs' | owner (fault fix) | `Phobos_TrailerSepticStandIn` |
| Sewer hookup: plumbed to a colony septic tank, treatment plant or outlet, its waste drains into them by gravity at 300 units an hour | owner; the rate is authored | `DrainToSewer` |
| "Empty at" levels for Dubs' haulers: 10, 25, 50 (Dubs' default), 75 % or never | owner; the levels are the agent's | `TrailerWasteGizmos` |
| A dump valve onto the ground, as Dubs' own sewage, 300 units an hour, closing itself when empty; its own setting, on | owner; the setting and its default follow the shared risk rule | `Dump`, `trailerWasteDump` |

**Agent choices, for the owner to revise:**

- **"Power point when parked" is the grid hook-up.** A parked trailer with the electric pump gets the same hidden hook-up conduit a
  parked vehicle with a battery has (Framework 0.108.0, 0.120.0): a conduit touching any side joins it to the grid, which pays the
  pump's 750 W while it runs. No second building beside a packed trailer. The shower trailer's set-out power point still powers it
  while the tent is up.
- **The bowsers keep their set-up as their pumping state** (it is their hoses out). Only the shower trailer pumps packed, because its
  set-up is a tent. So the towing vehicle's battery serves only the shower trailer; a bowser can't be set up while hitched.
- **Loading needs no pump.** Plumbing's towers, a fuel line and poured stacks all push or fall into the tank. A simplification: a real
  tank fed from a vehicle's fuel tank would need a pump.
- **Road top-ups are unchanged.** On the road a towed bowser's tap fills cans by gravity, which the stop time already counts.
- **The dump valve is a button, not a colonist's job**, as the tank's pump modes are buttons. The valve closes by itself when the
  tank is empty, and on hitching.
- **750 W** for the pump (a one-horsepower transfer pump; about 60 A from a 12-volt battery), **300 units an hour** for the drain, and
  a dump radius of 3.9 cells: authored.

### The four questions

- **What it lets the player do**: fill the shower trailer without pitching its tent; empty its waste tank without setting up, into
  the colony's sewers, by hand into drums at a chosen level, by a sludge tanker, or onto the ground; run a trailer's pump off the grid
  or the towing vehicle.
- **What it stops them doing**: pumping a tank trailer out for free. Without the electric pump fitted and powered, a bowser can no
  longer deliver by itself: **a bowser already set up on Unload in a save stops delivering until the upgrade is fitted**. The colony's
  toilets no longer fill a shower trailer.
- **How it changes the way they play**: the electric pump becomes the first upgrade for a working bowser, and its power a planning
  question (a conduit to the parking spot, or a tractor left standing with a charged battery). The waste tank becomes something to
  plumb or service rather than a dead end once full.
- **What outside the trailer notices**: the colony's grid pays for the pump, and the towing vehicle's battery runs down (shed with
  the camp lights, so the starter outlasts it). Dubs' own colonists empty the waste tank and its sewers take the waste; dumped waste
  is Dubs' sewage on the map, which fouls the ground, smells and can make colonists who walk through it ill.

### Risk

- **The dump valve: low.** The player orders it and sees it: the waste runs out at a fixed rate beside the trailer, the button and
  the inspect line say so, and closing the valve stops it. What is at risk is a fouled patch of ground and colonists who walk through
  it. Its own setting, **"Trailers can dump waste on the ground"**, starts on (the shared rule: on unless high).
- **Gravity only: not a risk to things or people**; it can halt a delivery, which the inspect line names.

### Other factions' vehicles

The pump rules work on any faction's tank trailer; only the player's are parked for pipes, so another faction's trailer never lays a
hook-up or waste tank. A captured trailer keeps its upgrades and follows the player's rules at once.

### Not yet tested in game

Rows 501 to 504.

## Upgrade trees as they stand

**The tree as it stands**, drawn from its Defs by `python scripts/check-upgrade-trees.py --write` and kept current by the gate (never edit it by hand): a box holds alternatives, an arrow is what an upgrade needs, an "any one" joint is one of our any-of links, a crossed line joins two that cannot both be fitted, and a dashed node comes only with a tie-in.

### The Pintle flatbed

Since Framework 0.216.0 and Vehicles 0.97.0 it is also a **camper trailer**: its deck takes a roof tent and an awning, and it pitches its
own camp ([vehicle-accessories.md](archive/vehicle-accessories.md#built-the-camper-trailer-22-nov-2026)). Fittings, not upgrades, so the tree is unchanged.

<!-- upgrade-tree: Phobos_FlatbedTrailer_UpgradeTree -->
```mermaid
flowchart TB
  PhobosTrailerHighSides["Greedy boards"]
```

### The fuel bowser

<!-- upgrade-tree: Phobos_FuelBowser_UpgradeTree -->
```mermaid
flowchart TB
  PhobosBowserLagging["Lagged tank"]
  PhobosBowserSecondHose["Second hose"]
  PhobosTrailerElectricPump["Electric pump"]
```

### The water bowser (Dubs Bad Hygiene)

<!-- upgrade-tree: Phobos_WaterBowser_UpgradeTree -->
```mermaid
flowchart TB
  PhobosBowserLagging["Lagged tank"]
  PhobosBowserSecondHose["Second hose"]
  PhobosTrailerElectricPump["Electric pump"]
  PhobosBowserReverseOsmosis["Reverse-osmosis kit"]
```

### The shower trailer (Dubs Bad Hygiene)

<!-- upgrade-tree: Phobos_ShowerTrailer_UpgradeTree -->
```mermaid
flowchart TB
  PhobosBowserLagging["Lagged tank"]
  PhobosBowserSecondHose["Second hose"]
  PhobosShowerThirdShower["Third shower"]
  PhobosTrailerElectricPump["Electric pump"]
```

## Questions for the owner

**Answered by the owner, 13 Nov 2026:** (1) **the first batch is the base with the water and fuel bowsers** (Q0, Q3, Q4), the
field kitchen and the prisoner transport next; (2) **canvas rooms, yes**, for the kitchen, aid post, crib room and laboratory;
(3) **3 x 7 is the bare minimum, and the other trailers are at least vehicle-sized or near** (later the same day; see B0.6).
Questions 4 and 5 were not put yet; their agent defaults stand until the owner says otherwise. Nothing was built then: each batch
waits on the owner's go (the first batch has since been built; see "What was built").

The questions as they were written:

1. **Build order:** the suggested order above, or another?
2. **Canvas rooms:** the kitchen, aid post, crib room and laboratory set out a real canvas room (agent default), or open pieces only?
3. **Chassis:** two chassis, the Pintle's 2 x 5 and a 3 x 7 tandem (agent default), or one size for all?
4. **Bases with trailers:** enemy bases may have parked, set-up trailers (agent default: yes), or never?
5. **The field laboratory and bridge layer:** keep both, or drop either?

## Who chose what

- **Owner (13 Nov 2026):** the round; the field kitchen powered by the Busbar as required, cooking on burners with power as a
  bonus; the prisoner transport with capture into it, raiders using it, captives at bases and harsh conditions; the Supply, Care
  and Work families and the livestock trailer in depth.
- **Owner (13 Nov 2026, later):** the base with the two bowsers first; canvas rooms; 3 x 7 the bare minimum, the other trailers
  at least vehicle-sized or near.
- **Agent defaults, owner to revise:** the shared base and its five pieces; 3 x 8 for the big working trailers; every figure marked [A];
  the suggested order; the catalogue entries and what was left out.

## Sources

**Pages read in full on 13 Nov 2026** unless marked otherwise:

- Wikipedia, [Bowser (tanker)](https://en.wikipedia.org/wiki/Bowser_(tanker)): the name, emergency water in the UK, fuel at
  airfields.
- US Army, [Attack of the Water Buffaloes](https://www.army.mil/article/188614/attack_of_the_water_buffaloes) (Maj. Jason
  Spencer, 2017): the M149's 400 gallons, 3,000 lb empty and 3,300 lb of water, designed for 2.5-ton trucks.
- Wikipedia, [Refrigerated container](https://en.wikipedia.org/wiki/Refrigerated_container): shore power or a clip-on diesel
  generator set; −65 °C to 40 °C.
- Wikipedia, [Light tower (equipment)](https://en.wikipedia.org/wiki/Light_tower_(equipment)): metal halide lamps on a mast,
  diesel generator sets; battery, solar and hydrogen sets.
- Wikipedia, [Armoured vehicle-launched bridge](https://en.wikipedia.org/wiki/Armoured_vehicle-launched_bridge): spans of 19 m
  or more; the Wolverine's 26 m bridge in about four minutes.
- Wikipedia, [Livestock transportation](https://en.wikipedia.org/wiki/Livestock_transportation): road trailers as the ordinary
  way; no figures used.
- RimWorld 1.7 (Ludeon Studios), Vehicle Framework 1.6 build (Smash Phil), Field Tents (lordfelix): read from our decompiles,
  summarised in our own words.
- Our own records: `vehicle-hitch-design.md`, `vehicle-hitch-design.md`, `vehicle-generator-set.md`, `vehicle-accessories.md`,
  `utility-vehicles-design.md`, `military-vehicles-research.md`.
