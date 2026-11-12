# The hitch and the generic trailer: design (11 Nov 2026)

**Status (12 Nov 2026): slices 1 to 3 are built**, slices 1 and 2 in D'thorga Framework 0.101.0 and D'thorga Vehicles 0.45.0, slice 3 in
Framework 0.104.0, and slice 4's recovery tow in Framework 0.103.0, **none yet tested in game**: see "Slice 2 as built" and "Slice 3 as built"
below for what was built and where it differs from the plan. It follows
[the trailer research](vehicle-trailer-research.md), where the owner answered on 11 Nov 2026: a hitch of our own, improved
and smoother; the smoother turn is worth a check; and "a genericized trailer that can be towed by, theoretically, any
vehicle", provided the vehicle has the torque, the engine power and the cooling for it. Facts marked "from the code" were
read on 11 Nov 2026 with `scripts/query-code.py`; everything else is agent-authored for the owner to confirm, and no
figure is set yet.

## Read this first

- **Yes to the generic trailer, and most of it already exists.** Our drivetrain model works out speed, gear, load, heat and
  fuel from the mass it is given. Give it the trailer's mass and every consequence the owner asked for follows without a
  new rule: a small truck with a heavy trailer drops gears, runs hot, burns more and crawls on soft ground.
- **The check passed for what a trailer needs.** Vehicle Framework draws a vehicle's body and its overlays at any angle.
  It does not turn the shadow or seated crew, so the first trailer has neither.
- **Any vehicle that has our systems can tow.** The hitch is added the way the sling is: in code at start-up, from a
  vehicle's loadout, with no change to another author's Def or picture.
- **The turn is a real trailer's**, from one rule: the trailer's axle stays a fixed length behind the hitch and is dragged
  toward it. Two lengths per trailer replace RimThunder's eight hand-written offsets.
- **Four decisions for the owner** are at the end. The first build waits for them.

## The check: does Vehicle Framework honour a free angle?

From the code of Vehicle Framework's 1.6 build, `VF` being `.local/decompiled/VehicleFramework-1.6/Vehicles.decompiled.cs`:

| Drawn thing | Honours the angle? | Where |
| --- | --- | --- |
| The body | **Yes.** The picture is chosen by facing, then turned by the facing's own angle plus the free angle; its offset turns too | `VF:38735-38759` |
| Overlays | **Yes.** They go through the same routine as the body | `VF:6411-6417` |
| The shadow | **No.** It is drawn at the vehicle's own facing with no angle | `VF:75257-75261` |
| Crew drawn in seats | **No.** Their places come from the facing alone | `VF:17961-17966` |
| Turrets | Not read | |

**What it means:** a trailer with no shadow, no seated crew and no turret can swing smoothly. We draw it at the nearest
of the eight facings plus the remainder, at most 22.5 degrees either way. **Still to see in game:** whether the picture
visibly jumps where one facing's drawing hands over to the next.

## What each behaviour does to the player

| Behaviour | Lets them | Stops them | Changes how they play |
| --- | --- | --- | --- |
| **Hitching a trailer to any vehicle** | Take a second load on one trip, behind whatever vehicle is to hand | Hitching to an aircraft, a boat, a rail vehicle or another trailer; two trailers on one vehicle | A trip is planned around what tows what |
| **The tow costs what physics says** | Pull a heavy trailer with a strong vehicle at nearly full speed | Pulling the same trailer briskly with a small one: it is held to a lower speed, in a lower gear, hotter and thirstier | The player picks the tractor for the load, and watches the temperature on a long pull |
| **Grip comes from the tractor alone** | Tow on firm ground with little loss | Getting a heavy trailer through mud or up a grade without weight on the driven wheels: it crawls | Soft ground and hills are a reason to send a heavier or all-wheel-drive tractor |
| **Dropping the trailer** | Leave the load at a camp or a site and drive away | Nothing | A trailer becomes a store that stays behind |
| **The swing** | See where the trailer is and which way it lies | Nothing in the first build: it is a picture | None yet; it is what makes the rest legible |

## How it works

### Hitching and holding

- **A hitch comp, `CompPhobosHitch`, beside the sling's.** The sling already carries one vehicle in a holder of its own,
  takes it off the map, puts it back, and saves it (`src/PhobosFramework.Vehicles/Sling/CompPhobosSling.cs`). The hitch
  is the same shape with a different rule for where the load is drawn and what it weighs on.
- **Added at start-up from the loadout**, as the systems comp and the sling are
  (`src/PhobosFramework.Vehicles/Service/VehicleSystems.cs:92-163`). So every vehicle the loadout selectors reach gets a
  hitch, ours or another author's, and a loadout can say "no hitch".
- **The trailer is held by the hitch, not in the cargo bay.** RimThunder puts it in the tractor's inventory, and Turbo's
  library then needs a long list of patches to keep cargo capacity, the cargo window and caravans straight
  ([the research](vehicle-trailer-research.md)). A holder of our own avoids all of it and needs no Harmony.
- **The order:** select a vehicle, right-click a trailer, "hitch". The vehicle backs up to it, a short wait, the trailer
  leaves the map. A gizmo with its own icon unhitches, and the trailer is set down behind the vehicle. Every refusal says
  why.
- **The hitch point** defaults to the middle of the vehicle's rear edge, from its footprint. A vehicle whose picture
  needs a different point names one in XML.

### The turn

One number is kept and saved: the trailer's heading. It moves by one rule, each tick the tractor moves:

```mermaid
flowchart TD
    A[Where is the hitch now?] --> B[Draw a line from the trailer's axle to the hitch]
    B --> C[Slide the axle along that line until it is one drawbar length from the hitch]
    C --> D[The trailer's heading is the direction of that line]
    D --> E{More than the jack-knife angle from the tractor?}
    E -- yes --> F[Hold it at that angle]
    E -- no --> G[Draw the trailer at its nearest facing plus the remainder]
    F --> G
```

- **What comes out of it without more rules:** a long trailer straightens slowly and a short one quickly; the trailer
  cuts the inside of a corner; it swings when the tractor's rear swings.
- **Two lengths per trailer:** hitch to axle, and axle to the middle of its picture.
- **Game-free:** the rule is a few lines with one square root, in the Framework core's `Rules/` with its own tests.
- **Simplifications, labelled:** the same rule is used in reverse, where it is stable and a real trailer is not; and the
  jack-knife angle is an authored limit, not a force.

### The load on the tractor

From our code: the model's road load is rolling resistance times weight, plus air drag, plus mass times acceleration and
grade (`src/PhobosFramework.Vehicles/Rules/DrivetrainModel.cs:146-147`), and grip is the driven wheels' share of the
weight times the ground's friction (`DrivetrainModel.cs:203`). Both take one mass
(`src/PhobosFramework.Vehicles/Systems/Powertrain.cs:303-322`).

**The change:** a second mass, the towed one.

| Term | Mass used | Why |
| --- | --- | --- |
| Rolling resistance, acceleration, grade | Tractor plus trailer | The whole train must be rolled, speeded up and lifted |
| Air drag | Unchanged | A simplification: the trailer runs in the tractor's wake |
| Grip | Tractor, plus an authored share of the trailer's weight carried on the hitch (none for a trailer on its own axles) | Only weight on the driven wheels pushes |

- **The trailer's mass** is its empty mass plus everything in it, counted as the sling already counts a slung vehicle
  (`CompPhobosSling.LoadKilograms`).
- **Everything downstream is the existing model:** the speed it can hold, the gear, the engine load, the heat into the
  cooling system, the fuel, clutch slip on a launch, and the crawl floor (a vehicle that cannot pull still crawls,
  owner, 8 Nov 2026).
- **A vehicle without a drivetrain of ours** is decision 3 below.

### The generic trailer

- **One Def, a flatbed with a cargo deck:** a vehicle with no engine, no seats, no turret and no shadow, loaded through
  Vehicle Framework's own cargo window while it stands unhitched.
- **Its picture:** our own, drawn empty. Until a picture exists it borrows one of ours so the game shows no error (owner's
  rule of 9 Nov 2026 against stand-in vehicle art).
- **It is the base for the special ones later:** tank, generator, tipping and camp trailers are the same trailer with a
  different deck.

## What it leaves out, said plainly

- **A towed trailer is not on the map.** It blocks nothing, cannot be shot and can swing through a wall (decision 1).
- **No loading while hitched** in the first build. (Slice 3: loading while hitched was never stopped and works; see below.)
- **The world map and caravans** are not designed here: whether a hitched trailer's mass reaches our world rules needs a
  read first. (Slice 3 builds it; see "Slice 3 as built".)
- **No road trains:** one trailer per vehicle.
- **No raid is given a trailer yet** (the section below says what works for other factions from the first build).

## Other factions' vehicles

The owner asked on 11 Nov 2026 that vehicle mechanics be designed with enemy raids in mind, or at the very least for the
vehicles at a base the player attacks. How those vehicles come to exist is in
[the Vehicle Framework record](vehicle-framework-mechanics.md). Agent-authored:

| Where | What the hitch does there | Lets the player | Stops the player | From which slice |
| --- | --- | --- | --- | --- |
| **A base we attack** | A trailer is a vehicle, so whatever puts vehicles at a base can put a parked trailer there, loaded | Hitch it to a vehicle of ours and drive off with the load | Taking it without bringing something that can pull it, and standing still to hitch under fire | 2, once a hostile faction's unattended trailer can be taken (to check: how Vehicle Framework treats claiming a vehicle) |
| **A raid on us** | A raider's vehicle may arrive towing. Other factions' vehicles run the lite tier (owner, 11 Nov 2026: kept, with no setting), so the tow does not slow it | Stop the tractor and keep what it was pulling: a trailer whose tractor is destroyed is set down where it stood, as the sling sets down its load | Nothing | The comp works for any faction from slice 2; a raid that brings a trailer waits for slice 4 and a hook to add one |
| **A vehicle we capture** | It tows as ours do, by its own drivetrain | Use a captured truck as a tractor at once | Nothing | 2 |

- **Cost with several on a map:** the follow rule runs only for a vehicle that is towing and moving, and a trailer set
  down runs nothing.
- **Decision 1 matters more here.** Off the map while towed, a raider's trailer cannot be shot until its tractor falls.
  That is acceptable for a first build and a reason to make towed trailers hittable soon after.

## Slices

1. **Rules, game-free and tested:** the trailer's follow rule; the towed mass in the drivetrain model.
2. **The first working trailer:** the hitch comp, the orders and their icons, the drawing, the saved heading, the generic
   flatbed, the mass reaching the model. One Framework version and one Vehicles version, one install, one short test list.
3. **Reading it:** towed mass and what limits the pull on the Dashboard; "can this vehicle pull that?" before hitching;
   loading while hitched; the world map. **Built** (Framework 0.104.0, not yet tested in game): "Slice 3 as built" below.
4. **The special trailers** from [the research](vehicle-trailer-research.md), the recovery tow, and road trains. **The recovery tow is
   built** (Framework 0.103.0, not yet tested in game) as one piece with the recovery family's winch (owner, 12 Nov 2026):
   [vehicle-upgrade-families.md](vehicle-upgrade-families.md), "Built in part 3". The special trailers and road trains are still open.

Slices 1 and 2 are built together: rules nobody uses are not a working slice.

## Decisions for the owner

**Decided by the owner, 11 Nov 2026:**

1. **A towed trailer is on the map and can be hit, from the first build.** This replaces "off the map while towed" throughout this
   record: the trailer is a spawned vehicle that follows the hitch, so it blocks and is blocked, can be shot, and a raider's towed
   trailer is a target from the start. The hitch therefore moves a real vehicle each tick instead of drawing a held one, and needs
   a design pass of its own before slice 2: how it is moved without Vehicle Framework's pathing fighting it, what it does at a wall,
   and what it costs per tick with several on a map.
2. **Borrow one of our pictures for the first build**, then a paid picture on the owner's go.
3. **Only vehicles with our drivetrain tow by default.** A mod setting lets other vehicles tow too, with a warning in the setting's
   description that their pull is a plain rule (speed falls with the trailer's share of the total mass), not the full model.
4. **One medium flatbed first.**

### The design pass for an on-map trailer (11 Nov 2026)

From Vehicle Framework's code (our 1.6 decompile, `VF` below), read after the owner's decision 1:

| Question | Finding | Plan |
| --- | --- | --- |
| How does Vehicle Framework move a vehicle? | It sets the vehicle's cell (`Position`), works out its angle, and refreshes path costs on the cells it left and entered (`VF:12960-12986`) | The hitch moves the trailer the same way, only when the follow rule puts its axle in a new cell |
| How is the smooth swing drawn? | Every vehicle has a public `Transform`, whose offset and angle its renderer adds when drawing (`VF:16047`, `VF:38738-38759`); the repair job already resets it (`VF:2339`) | The trailer's cell carries its facing, and the remainder of its heading and the sub-cell offset go in its `Transform`. **No Harmony patch** |
| What follows the trailer's cells? | Pawns, fire and bullets treat a spawned vehicle as on the map | It blocks, is blocked and can be shot, as decided |
| What happens at a wall? | Nothing in Vehicle Framework stops a vehicle because of another one's cells | Before the trailer moves, its footprint at the new pose is checked; if any cell is impassable for it, **the tractor is stopped** and told why ("the trailer cannot follow"). The tractor's path does not plan for the trailer in the first build |
| What does it cost? | | The follow rule runs only while a towing vehicle moves; a cell change touches the trailer's few cells; a parked train costs nothing |
| What carries the mass? | `Powertrain.MassKg` feeds the model | The hitch adds the trailer's mass as `TowedMassKg` (slice 1, built) |

**Slice 1 is built** (Framework, unreleased): `HitchRule` (`src/PhobosFramework.Vehicles/Rules/HitchRule.cs`) with its tests, and a
towed mass in the drivetrain model that is rolled, speeded up and lifted with the vehicle but adds no grip. Nothing uses them yet.

The questions as they were put:

1. **Off the map while towed?** Simple and cheap, as RimThunder does it, with the limits above. Or must a towed trailer
   be hittable from the first build? (Agent default: off the map first, hits later. A raider's towed trailer is the case
   that suffers most; see "Other factions' vehicles".)
2. **The trailer's picture.** Borrow one of ours for the first build, then a paid picture on your go. (Agent default.)
3. **A vehicle with no D'thorga drivetrain:** tow with a plain rule (speed falls with the trailer's share of the total
   mass, a gameplay simplification), or not tow at all? (Agent default: the plain rule, so "any vehicle" stays true.)
4. **How many sizes?** One medium flatbed first, or small, medium and large from the start? (Agent default: one.)

## Slice 2 as built (12 Nov 2026)

**Built, not yet tested in game.** The owner started it on 12 Nov 2026 as part of one batch with one version.

| Piece | Where | What it does |
| --- | --- | --- |
| `CompPhobosHitch` | `src/PhobosFramework.Vehicles/Hitch/CompPhobosHitch.cs` | Added at start-up to every land vehicle with a loadout (`VehicleSystems.Apply`; a loadout's `noHitch` leaves it out). Holds the trailer, its saved axle and heading, the order and its refusals, the follow step, the drawbar picture and the towed weight. Ticks only while hitched, through Vehicle Framework's tick on request, and works only while the vehicle moves or for 90 ticks after it stops |
| `CompPhobosTrailer` | `src/PhobosFramework.Vehicles/Hitch/CompPhobosTrailer.cs` | Makes any vehicle Def a trailer in XML: hitch to axle, axle to centre, jack-knife angle, the drawbar part. The trailer's inspect line and its own Unhitch button. A trailer gets no loadout |
| `Hitches` | same file as the hitch | Which hitch tows which trailer, and which wait for a colonist, kept by the hitches |
| The hitch-up job | `src/PhobosFramework.Vehicles/Hitch/HitchJobs.cs`, `Rules_VehicleSystems.xml` | A colonist who hauls walks to the trailer, works about a third of an hour, and the trailer is set behind the vehicle |
| `HitchRule` additions | `src/PhobosFramework.Vehicles/Rules/HitchRule.cs`, tested | The footprint's middle as Vehicle Framework works it out, the cell that puts it nearest a point, a heading split into a facing and an extra turn, and the plain rule |
| The tow in the model | `Powertrain.SampleGround`, `CompPhobosVehicleSystems.SpeedCap` | The towed weight reaches `DriveConditions.TowedMassKg`; the plain rule multiplies the speed cap where the model does not work the tow out |
| The Pintle flatbed trailer | `mods/PhobosVehicles/Defs/Phobos/Vehicles/FlatbedTrailer/` | 2 x 5 cells, 2,000 kg empty, 3,000 kg of cargo, a parts bill and one upgrade (greedy boards) |
| Icons | `Textures/Phobos/VehicleSystems/Hitch/`, drawn by `assets/phobos-vehicles/draw_hitch_icons.py` | Hitch a trailer, and Unhitch (also on the cancel button) |

**Where it differs from the plan, and why (agent choices, for the owner to revise):**

- **The trailer's place snaps to whole cells; there is no sub-cell offset.** The plan put the remainder of the axle's place in the
  trailer's `Transform`. Vehicle Framework adds `Transform.position` twice when it draws a vehicle: its `DrawPos` includes it
  (`VehicleFramework-1.6:Vehicles.decompiled.cs:13843-13852`) and `DynamicDrawPhaseAt` adds it again to the place it is given
  (`:16047`), which the game fills from `DrawPos` (`RimWorld-1.7:Verse.Thing.cs:1308-1314`). An offset there would be drawn double. So
  only the turn goes through `Transform.rotation`, and Vehicle Framework's own glide (`VehicleTweener`, a spring toward the cell's
  middle) smooths each step. The gap at the hitch varies by up to half a cell; **a drawbar is drawn from the hitch to the trailer's front**
  each frame, so the eye sees the bar stretch rather than the trailer jump.
- **Four facings, not eight.** The trailer Def turns off diagonal facings and is drawn at the nearest of four plus up to 45 degrees.
  Our textures' east and south are the north turned by the export script, so the hand-over between facings should not show. Its
  footprint is the nearest facing's, a simplification at 45 degrees.
- **Coupling sets the trailer behind the vehicle.** The plan had the vehicle back up to it. Backing a vehicle onto a point needs path
  work of its own; in this slice the colonist couples it and the trailer is placed straight behind the vehicle, if the ground there is
  clear (otherwise it refuses and says so). The trailer must stand within 8 cells.
- **Blocked means stopped.** Before the trailer moves, its footprint at the new cells is checked: on the map, walkable, and clear of every
  vehicle but the one towing it. If not, the vehicle's path is stopped and a message says why, at most once in ten seconds.
- **Leaving the map leaves the trailer.** A vehicle that despawns (a caravan, a launch) unhitches and says the trailer stays behind.
- **Loading while hitched is not stopped**, though the plan said "no loading while hitched": refusing it would need a patch to Vehicle
  Framework's cargo window. Haulers may follow a moving trailer.
- **With the drivetrain model on display only**, our vehicles tow on the plain rule too, so a tow is never free.
- **The trailer's name**: the Pintle flatbed trailer, Pintle P1 (a pintle is the hook-and-ring coupling a drawbar trailer hangs from).
  Bolster was the first choice, but Busbar already holds the B1 designation.
- **Authored figures**: 2,000 kg empty and 3,000 kg of cargo, hitch to axle 3.2 cells, axle to centre 0.2, jack-knife 70 degrees, the
  hitch 0.3 cells behind a vehicle's rear edge, 8 cells' reach, 900 ticks of work to couple, the greedy boards' +600 kg.

**Other factions, as built:** any faction's land vehicle with a loadout carries a hitch; nothing orders one to hitch, so no raid brings a
trailer yet (slice 4). A trailer of another faction is refused until claimed ("Claim it first"; the claim order, Framework 0.97.0). A
trailer whose vehicle is destroyed or leaves stays where it stood. Cost with several on a map: nothing for a vehicle without a trailer;
a parked train costs one comparison a tick.

**Still to see in game:** whether the picture jumps at the hand-over between facings; whether the tweener's glide reads as smooth at
speed; how often a tight turn stops the vehicle.

## Slice 3 as built (12 Nov 2026)

Not yet tested in game. What each part lets the player do, stops and changes, and how it was built:

| Part | Lets, stops, changes | Built as |
| --- | --- | --- |
| **What holds a tow back** | Lets the player see why a train is slow (the engine's power in a gear, the wheels' grip, a crawl) and pick a stronger or all-wheel-drive tractor | `TowText.Towing` (presentation) on the inspect panel, the Dashboard under its first line and the Systems tab's drivetrain card. It reads the hitch's cached towed weight and the drivetrain's last sample (`Powertrain.Result`, `Cap`), computing nothing. Plain: words; Operator adds the share of the speed kept; Engineer adds the train's weight |
| **"Can this pull that?"** | Lets the player know before the colonist walks over; stops a hitch that will only crawl being a surprise | `TowRule` (game-free, 7 tests): the drivetrain model's own `Evaluate` at the tractor's design speed, level and steady, on the ground it stands on (`Powertrain.ForecastTow`, the same ground, tread, wheel and gearbox reads the drive uses), so the forecast and the drive agree on the same ground; the plain rule without the full model; "too heavy" over the recovery tow's weight ratio. `CompPhobosHitch.Outlook` keeps a forecast a second (the targeter asks every frame). Shown beside the mouse while choosing (the game's own `Targeter` with an `onGuiAction` and `Widgets.MouseAttachedLabel`), with the refusal instead where there is one, and in the order's message |
| **Loading while hitched** | Lets the player load a trailer where it stands, hitched or not | **Nothing to build.** Vehicle Framework's loading jobs (`JobDriverLoadVehicleBase`) walk to the vehicle and touch it, with no condition a hitch could break (read 12 Nov 2026); while the train moves, the hauler follows it. The towed weight is counted again the next time the tractor moves (it is refreshed at most every 250 ticks while moving) |
| **The world map and caravans** | Lets a trailer and its cargo, or a recovered vehicle, travel with its tractor; changes caravan speed and fuel by its weight | The hitch is now an `IThingHolder`, as the sling is. When the tractor leaves the map not destroyed (`OnDeSpawn`; Vehicle Framework runs a destroyed vehicle's comps' `OnDestroy` first, which lets go), the towed vehicle is despawned into the hitch's own holder, saved with it. `TowedKilograms` keeps its weight for the drivetrain model, which the caravan's speed and fuel already read. When the tractor is on a map again, the hitch's next tick (never its spawning) sets the trailer down: straight behind if clear, otherwise the nearest clear cell within the hitch's reach, with its pose taken from where it stands so the follow rule draws it into line as the tractor moves off (at a map's edge, where caravans arrive, the cells behind are off the map). No room at all: it stays in the holder and is tried again every 250 ticks, with a message. The sling's `GetChildHolders` postfix lists the hitch while it holds, so the people in a recovered vehicle are found; the world-map lists and the caravan's line (`WorldObjectComp_PhobosSling`) name what a vehicle tows |

**Agent choices in slice 3:**

- The forecast is for the design speed on the ground under the tractor, level: it does not look along the way. Labelled a forecast in the
  text.
- Vehicle Framework's own caravan carrying capacity does not count the trailer: the trailer carries its own cargo, not the caravan's.
- A vehicle that tows or is towed is refused as a sling load ("unhitch first"): otherwise the partner would be left on the map or carried
  unseen.
- A vehicle destroyed while away with something in tow loses it with it, as a sling loses its load.
- **Unchecked:** whether colonists riding in a recovered vehicle are counted while the caravan travels (the colonist bar), as for a slung
  load.

**Other factions, slice 3:** a raider's vehicle that flees the map towing takes its trailer with it (the same rule as ours); the forecast and
the panels are the player's only; a captured vehicle tows and travels as ours do. Cost: nothing while away; the set-down is one tick's work.

## Who chose what

- **Owner (11 Nov 2026):** our own hitch, improved and smoother; the check; a generic trailer any vehicle can tow, limited
  by torque, power and cooling.
- **Owner (11 Nov 2026, the four decisions):** on the map and hittable; borrow our picture first; only our drivetrain tows by default, with
  a setting; one medium flatbed.
- **Agent choices in slice 2 (12 Nov 2026):** everything in "Where it differs from the plan" above.
- **Agent choices in slice 3 (12 Nov 2026):** the list in "Slice 3 as built" above.
- **Agent defaults:** the holder beside the sling's; the follow rule and its two simplifications; grip from the tractor
  alone; drag unchanged; the hitch point from the footprint; the slices; the four defaults above.

## Sources

- Vehicle Framework, by Smash Phil: the drawing routines, read from our decompile and summarised in our own words.
- RimThunder - Core, by RimThunderTeams, and Turbo's Vehicle Features Library, by Turbo: the approach we chose to differ
  from, described in [the research](vehicle-trailer-research.md). No code, Def or art of theirs is used.
- The drivetrain model's method and its sources are in [the drivetrain design](vehicle-drivetrain-design.md).
- The follow rule is standard geometry (the curve is a tractrix); our form of it has not been checked against a reference.
