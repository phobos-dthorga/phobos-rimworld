# Sling loads: a helicopter carries a vehicle under it (11 Nov 2026)

Design record. **Nothing here is built or tested in game yet.** The owner asked for a vehicle airlift of our own, "with our own flavour on
it", after seeing Yaroslav's [Cherokee Sling — Vehicle Airlift](https://steamcommunity.com/sharedfiles/filedetails/?id=3836765380)
(Workshop, updated 9 Nov 2026). That mod's published source was read on 11 Nov 2026 only to learn how such a mod meets Vehicle Framework's
flight; nothing of it is copied or adapted, and ours is designed from the questions below.

**Status (11 Nov 2026): slice 1 is built** in D'thorga Framework 0.92.0 and Vehicles 0.40.0, not yet tested in game: the cargo hook (a
fitting granting "CargoHook" on an appended hook mount), the sling set, the weight rule with hot-day derating (`SlingRule`), Lift a vehicle
with its reasons, the hook-up job, the load carried through Vehicle Framework's own flight (held in `CompPhobosSling`, found through a
`Pawn.GetChildHolders` postfix) and set down beside the helicopter where it lands, the launch refused while the load is not ready, and the
stand-down on the Cherokee for Cherokee Sling. **Slice 2 is next:** the selectable slung flight drawn on the map, choosing the set-down spot,
weather limits, the slower and thirstier slung flight, emergency release, and their settings. Agent choices in slice 1: a 25-cell reach,
about half an hour's hook-up for anyone who hauls, and the hook's cost (120 steel, 8 components, about 8 hours, Construction 7).

**Update (12 Nov 2026): the load can be seen**, in D'thorga Framework 0.99.0. The owner lifted a vehicle with the
Cherokee and reported that it "simply disappears into the aether": not under the Cherokee on the map, not listed on the world map. The
section "The owner's report of 12 Nov 2026" below says what was found and what changed.

**Tested in game by the owner, later on 12 Nov 2026 (take-off and world map only):** with the Cherokee lifting the L-ATV, "It's working
beautifully and the animation at launch looks wonderfully realistic". Their screenshot shows the world-map line "Cherokee carries L-ATV on
its sling: 7,103 kg." and the L-ATV listed after the Cherokee in our world-map tabs. **The landing and set-down are not yet tested in game.**

**Slice 2 built (12 Nov 2026), D'thorga Framework 0.101.0, not yet tested in game.** What it does, in short: the weather limit; the
slower, thirstier slung flight and the shorter range drawn for it; on landing, the game pauses and asks where to set the load down;
emergency release on the world map, the load becoming a **wreck** a caravan recovers (owner's decision, 12 Nov 2026); and a crash drops
the load, which lands hard. **Not built, by the agent's choice** (below, "Slice 2 as built"): our own selectable stand-in that hovers
over the load and takes orders in mid-air. Slice 3's hook latch sound is in too, as the agent's provisional pick.

## Read this first

- **What it is:** any helicopter given a **cargo hook** (a fitting, in the fittings window) can lift one vehicle on a sling, fly it on the
  world map and set it down at the far end, with the vehicle's passengers, cargo and fuel aboard.
- **Our flavour (owner's answers, 11 Nov 2026):** real weights against the aircraft's rated hook load, less lift on hot days, a colonist on
  the ground hooks the load up, weather can ground a lift, a slung load flies slower and burns more, and an emergency release drops it.
  **Each of these is a setting the player can switch off** (owner: "configurable settings ... so the player can turn on/off").
- **Pictures:** about five small icons from the OpenAI image API, in the same family as today's (owner's choice); the slung vehicle is drawn
  from its own picture with the strops drawn in code, so it needs no art.
- **Another author's mod:** when Cherokee Sling is active, ours **stands down on the Cherokee** (no cargo hook offered there), so the two
  never offer two airlifts on one aircraft and that author's vision for the Cherokee is left alone (agent default, from the owner's rule of
  11 Nov 2026 about other authors' art and vision). Our hook still works on every other helicopter.

## What the player does

1. **Fit a cargo hook** to a helicopter in the fittings window (materials and a mechanic, as any fitting).
2. **Make a sling set** at the vehicle parts bench and load it aboard the helicopter: strops, a swivel and a spreader, kept, never used up
   (agent default; it is what a lift needs and what a caravan must carry, like the jump-start kit).
3. With the helicopter ready to take off, press **Lift a vehicle** and pick a vehicle parked outdoors on the map. The button says why not
   when it cannot: no hook, no sling set, too heavy today, too windy, or no one to hook up.
4. **A colonist hooks up**: they walk to the vehicle, and the helicopter hovers over it while they fit the strops (a short job; the lift
   waits for it). With the ground crew setting off, the helicopter hooks up by itself.
5. **Choose a destination** on the world map, as for any flight; the planned range accounts for the load.
6. **At the destination**, place where the load is set down, then where the helicopter lands. The crew aboard release the hook.
7. **Emergency release** drops the load where it hangs. On a map it lands hard there and is damaged; an engine failure in the air drops it
   by itself.

## Lets, stops, changes

- **Lets the player:** move a vehicle where no road goes, over water and mountains; reach a second colony with a truck; get a broken vehicle
  home from far away; drop an armoured car into a fight.
- **Stops them:** lifting more than the aircraft can carry, lifting in storms, lifting without a sling set or a hook-up crew (each a setting).
- **Changes how they play:** heavy vehicles stay land vehicles; a light truck or a buggy becomes air-mobile; weather and the time of day
  (heat) become part of planning a lift; a helicopter fleet gains a real job.

## The rules

| Rule | What we do | Source or label |
| --- | --- | --- |
| **The load's weight** | The vehicle's real kerb mass (our drivetrain model's figure, the same the drive and the repair costs use) plus what it carries (its cargo and passengers, as Vehicle Framework's cargo tab counts them) | Kerb masses as recorded in `utility-vehicles-design.md` |
| **What a hook takes** | Each helicopter's **rated hook load**, in its flight profile or loadout, in kilograms | Named per aircraft below |
| **The Cherokee** | 11,800 kg, the CH-47F Chinook's centre hook (26,000 lb), the Cherokee being VVE's twin-rotor cargo helicopter | US Army CH-47 fact sheet, republished by the [127th Wing, Air National Guard](https://www.127wg.ang.af.mil/About-127th-Wing/Fact-Sheets/Article/3694458/ch-47-chinook/) |
| **A medium helicopter** (for other mods' helicopters of that class) | 4,080 kg, the UH-60M Black Hawk's external load (9,000 lb) | [Lockheed Martin, UH-60M](https://lockheedmartin.com/en-us/products/sikorsky-uh-60m-black-hawk-helicopter.html) |
| **A hot day** | The hook load falls 1% for each °C of outdoor temperature above 15 °C, to at most 30% less (**authored**). Real helicopters lose lift and engine power as heat thins the air: the FAA defines density altitude and says air density falls with rising temperature ([FAA Helicopter Flying Handbook, glossary](https://www.faa.gov/sites/faa.gov/files/regulations_policies/handbooks_manuals/aviation/helicopter_flying_handbook/hfh_gloss_index.pdf); [chapter 7](https://www.faa.gov/sites/faa.gov/files/regulations_policies/handbooks_manuals/aviation/helicopter_flying_handbook/hfh_ch07.pdf) names density altitude as a major factor). The CH-47F fact sheet quotes 16,000 lb of payload in high and hot conditions against its 26,000 lb hook, which our cap is gentler than. | Authored, from the direction those sources give |
| **Weather** | No lift while the map's wind is strong or a storm is on (thresholds **authored**, named in the rules Def) | Authored |
| **The slung flight** | Cruise speed ×0.7 and fuel per tile ×1.3 while a load hangs (**authored**: an external load's drag slows a helicopter and costs fuel; real figures vary with the load's shape) | Authored |
| **A dropped load** | Damage on landing from the height it fell, spread over its parts as a heavy impact (**authored** figures in the rules Def) | Authored |

Figures the table calls authored live in a rules Def (`PhobosSlingRulesDef`), so they can be tuned in play without code.

## Settings (all on by default; the owner's request)

| Setting | Off means |
| --- | --- |
| Hot days lift less | The rated hook load whatever the weather's temperature |
| A ground crew hooks up | The helicopter hooks up by itself |
| Weather can ground a lift | Lifts in any weather |
| Slung loads fly slower and burn more | A slung flight costs what an empty one does |
| Emergency release and drops | No release button, and an engine failure keeps the load |

A sling set aboard is part of the rules, not a setting (agent default); the owner may ask for a setting for it too.

## Selecting the helicopter while it flies (owner's question, 11 Nov 2026)

**The problem:** while a Vehicle Framework aircraft takes off or lands on a map, the vehicle is held inside a stand-in that draws its flight,
and that stand-in cannot be selected, so nothing can be asked of the helicopter mid-air. A sling needs exactly that: set down here, release
now.

**How Cherokee Sling answers it** (read from its source, in our words): its lift and set-down run as a stand-in of its own on the map that
*can* be selected and carries its own buttons (one to choose where the load and the helicopter come down); its inbound flight on the world
map is a world object of its own with a "continue" button; and while it waits for the player to choose landing spots it pauses the game,
so nobody has to catch a moving aircraft with the mouse.

**Ours (agent's proposal):**

- **On a map, a slung flight is its own selectable thing**: our stand-in for the hover, hook-up, climb, descent and set-down, which draws
  the helicopter, the strops and the load, can be selected, shows an inspect line (the load, its share of the hook's rating today, the
  wind) and carries the buttons that matter in the air: **Set down here**, **Emergency release**, and Cancel before the hook takes the weight.
- **On the world map**, a helicopter in flight is already a selectable world object (our Dashboard and Systems tabs sit on it); the sling
  adds an inspect line and **Emergency release** there.
- **Choosing where to come down** follows the game's own habit: chosen before take-off when the destination map is open; otherwise, on
  arrival, the game pauses and asks, with the load's and the helicopter's outlines to place.
- **Not our fault to fix:** an ordinary flight's stand-in staying unselectable is Vehicle Framework's design. We leave it, and only our
  slung flight gets its own.

## How it meets Vehicle Framework (to read before building)

The other mod showed that a sling can ride on Vehicle Framework's own launch: the load leaves the map held inside the aircraft, and an
arrival action of the aircraft's places it again. We will read Vehicle Framework's launch, flight and arrival code with
`scripts/query-code.py` before building and record the members we use; any reflected name goes through `scripts/check-targets.py`. Our comps on
the carried vehicle already catch up the hours they were away when it is spawned again (the engine has cooled, the battery has rested).

**Read so far (11 Nov 2026):**

- **Holding the load needs one Harmony patch.** The slung vehicle (and anyone riding in it) is held in a container of the helicopter's sling
  comp. The game finds held things through `Pawn.GetChildHolders`, which lists a pawn's inventory, carried thing, equipment and apparel
  but never its comps (`RimWorld-1.7:Verse.Pawn.cs:4736`), and Vehicle Framework adds no way in. Without it the game would not know the
  passengers in a slung vehicle are aboard. So a **postfix on `Pawn.GetChildHolders`** adds the sling's container, only for a pawn with a
  sling comp; it never skips the original. Cherokee Sling makes the same kind of postfix; each adds only its own comp, so both can be active.
  The patch gets its row in no temporary-patch list: it is how our feature works, not a fix for another author.
- **Where Vehicle Framework holds flying things:** its own stand-in on a map (`VehicleSkyfaller`, `Vehicles.decompiled.cs:24417`) and its
  world object in flight (`AerialVehicleInFlight`, `:90772`) both pass their holders on, so a helicopter's sling container rides along
  through the whole flight once the postfix lists it.

- **Correction (12 Nov 2026):** the two stand-ins do **not** pass holders on. `VehicleSkyfaller` and `AerialVehicleInFlight` both answer
  `GetChildHolders` with nothing (`VehicleFramework-1.6:Vehicles.decompiled.cs`, `:24417` and `:90772`), so a search that starts at
  the stand-in never reaches the helicopter's contents, ours or Vehicle Framework's own. Our postfix serves only a search that starts at the
  helicopter. Going upward the chain is whole: a person in a slung vehicle is held by its seat, the vehicle by the sling, and the sling
  answers with the helicopter's own holder. **Unchecked:** whether colonists riding in a slung vehicle are counted while it flies (the
  colonist bar, the game's count of colonists away). Vehicle Framework counts the people aboard the aircraft itself, not ours. Until the
  owner has flown one, a lift with people aboard the load is the riskier test.
- **People and things in a slung vehicle do not tick in flight**: nothing ticks the sling's contents, and Vehicle Framework's world object
  does not tick its own either. They come back as they left.

## The owner's report of 12 Nov 2026

**Observed by the owner:** with a vehicle chosen and hooked up by a colonist, the Cherokee lifted off and the vehicle "simply disappears
into the aether. It doesn't appear beneath the Cherokee itself on your colony map nor does it appear on the World map as a vehicle that's
selectable in the Pawns tab."

**Read from the save and the log (not a test in game):** the newest save was made before the lift, with the vehicle parked and chosen as
the Cherokee's load, and the log holds no error between that save and the reload two minutes later. Slice 1's code takes the vehicle off
the map into the sling at lift-off and sets it down when the helicopter next appears on a map. The owner confirmed the same day that the
flight was never allowed to reach a landing, so nothing shows the vehicle was lost: it was aboard and unseen. Whether it is set down at a
landing in game is still **untested**. What slice 1 certainly did was show the load nowhere and say
nothing: no message at lift-off (a silent success, against our own rule), no picture, and no line or list on the world map.

**Changed in Framework 0.99.0:**

| What | How | Lets, stops, changes |
| --- | --- | --- |
| The load is drawn under the helicopter in take-off and landing | A postfix on each of Vehicle Framework's two stand-ins' `DrawAt` draws the held vehicle one layer below the helicopter, with two strops, placed by the game-free `SlingRule.Hang` | Lets the player see the lift happen. Changes nothing a lift does |
| Lift-off says so | A message with the helicopter, the load and its weight | Ends the silent success |
| The world map shows it | A world-object comp adds a line to an aircraft in flight and to a convoy; a postfix on `AerialVehicleInFlight.Vehicles` lists the load after the helicopter, which is the list Vehicle Framework's passengers tab draws; our own world tabs and the fleet board list it through `SlingLoads` | Lets the player find the load and the people in it while it flies |
| A load is never lost to the hook | If the sling cannot take the vehicle at lift-off, it is put back where it stood, with a message | Stops a silent loss |
| **Set the load down** | A button on a landed helicopter still holding its load because no clear ground was found | Lets the player set it down when there is room, where it used to wait for the next flight |

**The picture's rule (authored):** the map shows height as distance up the screen, so the load hangs 3 cells below the helicopter's
picture and is on the ground while the helicopter is nearer its spot than that. On a take-off the load is drawn where it stood during the
spool-up, and drawn in under the helicopter as it climbs the first 3 cells. **This is a simplification:** a real crew hovers over the load
to hook up, and ours does not yet, so a load parked 20 cells off slides in. The hover is slice 2's.

**Vehicle Framework's members that name Unity's types** (`VehiclePawn.DrawAt`, `VehiclePawn.TrueCenter`) are called by name, as
`OverlayPosition` and `VehiclePicture` already do, because Vehicle Framework's build names another Unity version's types and the compiler
refuses them. `scripts/check-targets.py` checks both names.

**How Cherokee Sling shows its load** (its published source, installed but not active on the owner's list, read again 12 Nov 2026 at the
owner's suggestion; in our words, nothing copied): it replaces Vehicle Framework's stand-in with one of its own that draws both vehicles
through every draw phase and flies the helicopter over to the load first. It does not list the load on the world map either. Ours keeps
Vehicle Framework's stand-in and draws beside it, which is less to maintain but cannot change the helicopter's path; that is why the
hover waits for slice 2's own stand-in.

## Slice 2 as built (12 Nov 2026)

| What | How | Lets, stops, changes |
| --- | --- | --- |
| **Weather grounds a lift** | No lift is chosen or flown while the map's wind is above 150% on the game's own readout, or the weather brings lightning (read from what the weather does, never its name). The Lift button and the launch say which and wait | Stops a lift in a gale or a storm; changes a lift into something planned around the weather |
| **The slung flight** | Vehicle Framework's flight speed and fuel use are multiplied while a load is ready on the hook or hanging: 0.7 of the speed and 1.3 times the fuel per tile. Vehicle Framework spends fuel by the tick, so the per-tick factor is 1.3 x 0.7 (`SlingRule.SlungFlight`). Its range drawn when choosing a destination reads the same figures, so it shows the load. The world map's line says so | Stops a slung flight costing what an empty one does; changes how far a lift can go |
| **Choosing where the load comes down** | When a helicopter with a load lands on the map the player is looking at, the game pauses and asks; the load's outline follows the mouse, white where it fits within reach, red where it does not. Right-click, and it comes down on the nearest clear ground as before (the 0.99.0 behaviour, kept). On a map nobody is looking at, it is set down at once as before | Lets the player put the load where it is wanted, not where it happens to fit |
| **Emergency release** (world map) | A button on our helicopter in flight carrying a load, with a confirmation. The load falls 30 m and lies on the tile below (or the nearest free one) as a **wreck** | Lets the crew save a helicopter short of fuel; stops the load reaching its destination |
| **The wreck** | A world object of ours holding the vehicle and its riders. A caravan sent to **Recover** it reaches a small map (100 cells across) with the wreck on it; the fall's damage is dealt then. When nobody of ours is left there, the map goes; a wreck still lying on it is taken back to wait for the next visit, and one driven, towed or lifted away leaves nothing | Lets a loss become a recovery mission (owner: "a lost thing is future content") |
| **A crash drops the load** | Vehicle Framework's crash event (out of fuel, shot down) is noted; when the helicopter reaches the crash site's map, the load falls 15 m beside it instead of being set down | Changes a crash into a harder recovery |
| **A drop's damage** | Each part loses a share of its health: none from 2 m or less, rising evenly to all of it from 40 m (`SlingRule.DropShare`), varied 40% either way part by part. **Every part keeps a sliver of health**, so a fall never destroys the vehicle outright and there is always a wreck to recover. Riders take blunt damage, 6 for each tenth of the share, in three hits | Authored |
| **The latch sound** (slice 3) | A steel hook snapping shut, played where the load stands when a colonist hooks it up, or when the helicopter takes it up itself with the ground crew setting off | Agent's provisional pick, for the owner to confirm |

**Not built, the agent's choice (for the owner to revise):** our own selectable stand-in for the take-off and landing, with **Set down
here**, **Emergency release** and **Cancel** in mid-air, and the hover over the load before the hook-up. Read from Vehicle Framework's
code: its take-off and landing stand-ins are ordinary things whose picture moves while their cell stays on the ground, they last a few
seconds, and the launch is shared with every aircraft and with VTOL Vehicle's and the Harrier framework's prefixes that may skip it.
Replacing the stand-in (as Cherokee Sling does) means owning that whole launch for helicopters, and making Vehicle Framework's stand-in
selectable would change every aircraft's take-off. The orders the stand-in was for are instead given where the player can reach them:
**Leave the load** before take-off (slice 1), the **set-down choice** on landing, and **Emergency release** on the world map. A load still
slides in under the helicopter during take-off, as in 0.99.0. Settings for all three new behaviours are below.

**Read from code for this slice** (`scripts/query-code.py`): `CompVehicleLauncher.FlightSpeed` and `FuelConsumptionWorldMultiplier`
(`VehicleFramework-1.6:Vehicles.decompiled.cs:18422-18424`), spent by `AerialVehicleInFlight.SpendFuel` and read by `FuelNeededToLaunchAtDist`
(`:90429`, `:18597`); `AerialVehicleInFlight.InitiateCrashEvent` (`:90445`), which makes Vehicle Framework's crash site and its map at
once through `IncidentWorker_ShuttleDowned` (`:77856`). That crash site makes its map the moment it is created, and two places that can
hold a map must not share a tile, so the wreck is a place of our own that makes its map only when a caravan arrives; a released load
is put on the nearest tile with nothing else on it. The game's wind (`WindManager`: a base of 0.04 to 2 times the weather's factor).

## Other factions' vehicles

- **In a raid on us:** no raider orders a lift, so a raider's helicopter never carries a load. The drawing checks for a load first and
  does nothing more for an aircraft without one.
- **Parked at a base we attack:** a vehicle that is not ours is refused as a load, with the reason. Claimed first (the claim order,
  Framework 0.97.0), it lifts as ours.
- **One we capture:** a captured helicopter with a hook mount lifts as ours does, and a captured vehicle is a load like any other.
- **Cost with several on a map:** nothing each tick. The picture is drawn each frame only for a flight that carries a load.
- **Slice 2:** the weather limit and the slower flight apply to any helicopter with a load, but no raider lifts one. The release button is
  shown only on our own helicopters. A wreck is nobody's: a caravan of ours recovers it, and it waits however long that takes.

## Slices

1. **Lift and carry**: the hook fitting, the sling set, the weight rule with the hot-day derating, the Lift button with its reasons, the
   hook-up job, the load carried through the flight, and set-down at the destination. Settings for derating and the ground crew.
2. **Weather, flight and release**: our own stand-in that hovers over the load to hook up and lets the player choose where it comes
   down (the first picture of the load is in 0.99.0), the weather limit, the slower and thirstier slung flight (and the range drawn for it), emergency
   release on a map, the drop on engine failure. Their settings.
3. **Pictures and sounds**: the five icons (OpenAI, the owner's choice), and a hook latch sound through the sound pipeline.

**Also on the hook (Framework 0.101.0, batch 3's signatures):** the **water bucket** hangs from the same cargo hook and keeps to the
sling's weather rule and hook capacity, but flies its runs as a stand-in of our own, not through Vehicle Framework's flight; the
**litter kit** rides in the cabin and through a flight like the sling's load, held by a comp of its own. Both are designed in
[vehicle upgrades, round two](vehicle-upgrades-round-two.md).

## Open questions

- **Decided (owner, 12 Nov 2026): a load dropped on the world map becomes a wreck site** a caravan can recover. Built in Framework
  0.101.0 as a place of our own rather than Vehicle Framework's crash site (see "Slice 2 as built").
- **A sling set's cost**: the agent will author it from real strops (polyester round slings and a steel swivel) and say so.
- **Other mods' helicopters**: each needs a flight profile and a hook rating. The Cherokee comes first; others as the owner names them.

## Who chose what

- **Owner (11 Nov 2026):** our own airlift with our own flavour; any helicopter, by fitting; real masses with heat derating; ground crew,
  weather limits, slower and thirstier slung flight, emergency release; all of them as settings; pictures from the OpenAI API.
- **Owner (12 Nov 2026):** a load dropped on the world map becomes a wreck a caravan can recover; the latch sound made on the processor.
- **Agent choices in slice 2 (12 Nov 2026):** no selectable stand-in or hover (above); the thresholds and figures in the table; a drop
  leaves every part a sliver of health; the wreck's map size; release only on the world map; a crash dropping the load from 15 m; the
  latch sound's pick.
- **Agent defaults (for the owner to revise):** the sling set as an item kept aboard; the authored figures in the table; standing down on
  the Cherokee when Cherokee Sling is active; the order of the slices; a wreck site for a load dropped in flight (open).
- **Owner's picks (11 Nov 2026):** lift #1, set down #2, emergency release #1, cargo hook #2, sling set #2 (US$0.54 for ten attempts; masters
  on `art-masters` under `vehicles/sling/`, the rest on `art-references`).
