# Patches

**D'thorga Patches** fixes and adjusts other authors' mods. Each part switches on only while that mod is active and copies nothing
from it; without the mods it patches, it does nothing. [Back to the guide](README.md).

![A pilot walking towards a Harrier jet parked by a hangar at dusk](../../mods/PhobosPatches/About/Preview.png)

## Read this first (gotchas)

- **Needs Harmony and D'thorga Framework. Load it after the mods it patches**; RimSort does this from its About file. **Switch it on
  yourself**: nothing else requires it.
- **Switching from D'thorga Vehicle Patches or D'thorga Missile Patches**: both off, this one on, in the same change.
- **Mostly not yet tested in game**: see [the last section](#what-has-been-tested-in-game).
- **After a missile strike, wait for the message that the blast has spent itself before saving**: a blast still running when you
  save is forgotten on load.
- **Before removing it**: delete bomb, rocket and heavy-casting bills it added; with Combat Extended, use up or sell bomb cases and bomb
  fills, and deconstruct turret hulls built under it.

## What it changes, mod by mod

| Mod (author) | What changes | What you do |
| --- | --- | --- |
| **Harrier VTOL Attack Aircraft** (potemkinvillager) | A realistic build cost (with RimThunder: its turbine, landing gear, tanks, wiring and nozzles; Construction 10). Fourteen parts in place of four. Loads fully beside RimPacts. A sortie's map stays open under the jet. Rearming keeps to one kind of ammunition | Research engine making, Otto Cycle and turbocharged for the turbine. **Avionics, hydraulics or wiring below about 35% ground the jet**: repair them. To change ammunition, choose it on the weapon |
| Harrier, with **Combat Extended** | Bombs cost what they weigh: each starts as a **bomb case** made from steel, then is filled. With The Dead Man's Switch, bombs and rockets are bills at its ammunition fabricator too | Make bomb cases at the machining table, rotary forger or automated machining bay. **Allow the cases in bay hoppers** that used to take heavy castings |
| Harrier, in the air | Its dodge chance follows its flight controls and warning receiver; a **Chaff and flares** button makes it harder to hit for ten seconds | Load **countermeasure cartridges** (D'thorga Framework's parts bench) into its cargo. Repair shot-up controls and avionics |
| Harrier, Hind, Mongoose | Rearming tops up the kind already loaded | A part-full magazine waits if that kind runs out |
| Harrier, being taken apart | A temporary trace writes each step of a vehicle's disassembly to the game's log, to find why the Harrier can't be taken apart | **Try one disassembly, on a copy of your save, and don't retry.** The setting **Log each step when a vehicle is taken apart** turns it off |
| **RimThunder** with The Dead Man's Switch - Expedition | The rotary forger makes heavy castings 12 at a time from 1,200 steel | Keep the floor around the forger clear: 24 items drop beside it |
| **Spaceship Turret Hull** (MK), with **Combat Extended** | All seven hulls load ammo like CE's own turrets, and no longer explode when destroyed | **Rebuild hulls built before**, while the ship is landed |
| **InterRim Ballistic Missile** (Psionic Zephyr) | A strategic warhead's blast is spread over moments instead of freezing the game; overhead mountain shelters what is under it; the extinction-class warhead strikes with a fireball, blast and heat zone; no flood of "Area revealed" letters | **Wait for the message that a blast has spent itself before saving.** Settings under D'thorga Patches |
| IRBM with **Combat Extended** | Its close-in weapon systems shoot down CE's mortar and artillery shells headed for your things, as they always did vanilla shells | **Unroofed and powered**, as before. The setting **Close-in weapon systems shoot down Combat Extended shells** turns it off |
| **RimPacts** (temporary) | An ally's assault leaves vehicles with nobody aboard alone and goes for the defenders, instead of beating on an empty vehicle for hours | Claim the empty vehicles after the battle. Not yet tested in game |
| IRBM with **RimPacts** | An ally's assault after your strike faces the garrison the town had before it. A world war attack shows its deadline | **Strike and attack early**: RimPacts gives a war attack 3 days |
| **Visit Settlements** (Nina.Goblin) | A visited settlement's map stays open while any colonist of yours there is conscious, on foot or in a vehicle. Its food drops no longer fail every tick | Pawns already lost this way stay lost: load an earlier save |
| **Vanilla Expanded Framework** outposts, with **Vehicle Framework** | A caravan with vehicles can't found an outpost through "create outpost", which could lose them | Leave the vehicles first and come on foot. The setting **Refuse to found an outpost from a caravan with vehicles** turns this off |
| **Vehicle Framework** | Fires a vehicle could never put out are put out; a vehicle leaving a map takes no fire with it. Repairs and upgrades show the game's progress bar | Nothing |
| **The Dead Man's Switch** drone platforms (Fortified Features Framework) | Colonists refill the engineering drone bay, the trashpan and hound stations and the FPV nest with steel by themselves | Nothing: keep steel in reach. [Drone platforms](#drone-platforms) |
| **Combat Extended** ammo sets | A temporary guard drops ammo set entries that would stop CE's whole ammo setup (The Dead Man's Switch - Synthriada adds such entries) | Nothing |

![The Mk. 82 and Mk. 84 bomb cases](images/patches-items.png)

## The Harrier

### Building it

The jet's blueprint is under **Architect, Vehicles**. It needs **Construction 10**, as the Hind and Mongoose do. Its cost is a
gameplay choice, one part for each real system:

| Part | The Harrier mod's own | Now | Why |
| --- | --- | --- | --- |
| Steel | 450 | **400** | Airframe |
| Plasteel | 150 | **120** | Stands in for the composites, about a quarter of the real airframe's weight |
| Components | 25 | **30** | Avionics; the same as the Hind |
| Advanced components | 6 | **0** | The turbine's own recipe still uses 1 |
| Engine | 1 engine block | **1 RimThunder turbine** | One real engine. Without RimThunder: 3 engine blocks, as on the Hind |
| Car exhausts | 0 | **4** | The real engine's four swivelling nozzles |
| Suspension and wheels | 0 | **4 and 4** | Landing gear |
| Fuel tanks | 0 | **3** | Fuselage and wings |
| Transmission | 0 | **1** | Accessory gearbox |
| Alternators and batteries | 0 | **2 and 2** | Electrics |
| Wiring | 0 | **6** | The same as the Hind |

**With RimThunder, the turbine is a long job:**

- **Research:** engine making, Otto Cycle and turbocharged. The jet's blueprint stays hidden until they're done.
- **An assembly platform** to make it on (RimThunder, 200 steel, 15 components).
- **Each turbine:** 3 heavy castings (made at a smelter, 100 steel each), 15 components, 1 advanced component and 180,000 ticks of
  work.

The **Drop Tanks** upgrade costs **2 car fuel tanks, 20 steel and 2 components**, in place of 60 steel and 2 components. A jet you
already built keeps working; only new builds pay the new cost.

### Its parts and flight controls

The Health tab lists fourteen parts, each taking hits and needing repair. Every figure is authored.

| Part | Where | Health | What its damage does |
| --- | --- | --- | --- |
| Airframe, cockpit, engine, wing fuel tanks | The jet's own four | 470, 170, 200, 120 | The engine drives move and flight speed, and overrides the other parts when it fails; the tanks leak and burn |
| Canopy | Outside, over the cockpit | 40 | Takes hits that would have hit the cockpit |
| Avionics, nose radar | Inside the nose | 40, 30 | Avionics are a flight control; the radar only needs repair |
| Intakes and compressor | Inside, either side of the engine | 50 | Flight speed |
| Thrust nozzles and exhaust | Outside, either side of the engine | 50 | Move and flight speed |
| Hydraulics, wiring and electrics | Inside the tail | 40, 30 | Flight controls |
| Alternator | Inside, beside the engine | 30 | Repair only |
| Landing gear | Outside, nose and belly | 60 | Move speed on the ground |
| Wing pylons | Outside, under the wing tips | 50 | Repair only |

- **Avionics, hydraulics and wiring are the flight controls. With any of them below about 35% the jet cannot take off**, and the
  launch and sortie buttons say which to repair. Damaged, it flies slower. This is D'thorga Framework's rule for every aircraft;
  with the Framework's vehicle systems switched off for the Harrier, nothing grounds it.
- **A Harrier you already have gains the new parts at full health** when the save loads.
- The **plasteel armour upgrade** covers the nozzles, landing gear, pylons and intakes too. Under Combat Extended the new parts take
  the jet's own light armour, which rifle fire goes through.

### Rearming

Rearming keeps to **one kind of ammunition**: it tops up with what is already loaded, for the Harrier, the Hind and the Mongoose.
**To change kind, choose it on the weapon**: that still swaps the magazine. If the loaded kind runs out of stock, a part-full magazine
waits rather than being swapped by itself.

### Hovering onto its pad

A Harrier too heavy for its engine to hover in the day's heat will not hover onto its pad. The vertical-landing button and order say
why, an automatic return home uses a runway instead, and a landing that runs out of injection water part-way is called off with the
reason. **This follows D'thorga Framework's setting "When an aircraft is too heavy or too hot to hover onto its pad"**: use the runway
(the default), allow it at a cost to the engine, or warn only. D'thorga Vehicles gives the Harrier its real engine figures; without
it the Framework's general jet engine judges the hover ([aircraft](vehicle-care.md#aircraft-and-sling-loads)).

### Sorties

Read from the Harrier's own code, not seen in play. The Harrier mod does all of this; this mod keeps the map open under the jet and
explains refusals.

- **Sortie** (the button on the jet) opens the world map. Pick a site or settlement: an open map gives "fly to" and "land" choices; a
  closed one is entered from the map edge. **Your own home map is refused**; use Launch to fly home.
- **Fuel decides time on station, not distance.** World flight costs about 0.16 fuel and a minute a tile, so a 30-tile sortie costs
  about 5 fuel each way; over a map the jet burns 250 fuel in about 6 hours. A sortie is refused unless the tank holds the trip there
  and back, an hour over the target and a 10% reserve.
- **It comes home by itself** when its rockets and bombs are spent or fuel runs low, to the map it took off from if that has a
  runway, else the nearest home map with one.
- **Home needs a full beacon runway**, 24 cells by 5 for the Harrier: four beacons on the corners of a rectangle, 23 cells apart
  along it and at least 4 across. With no runway at home, the jet circles and says so.
- **Don't land on a sortie map.** The Harrier lands vertically anywhere but takes off only from a beacon runway, so a jet landed away
  from one stays there. A rough landing also wrecks its engine.
- **No sortie from under a roof**: taxi out first.
- **No sortie onto a RimPacts battle you have not joined**: join it from the town's button on the world map first; the targeter says
  so. The setting **Sorties may fly over a battle you have not joined** lifts this for aircraft only, and RimPacts then counts the
  crew as your forces in that battle.
- **A sortie's map stays open while the jet is over it**, whichever mod made the map (Real Ruins and RimMarket maps could close
  before). Any vehicle of yours with a colonist aboard keeps its map open, the Hind and Mongoose included.

### Taking it apart (temporary)

The Harrier cannot be disassembled yet: the work stops near the end and the jet stays. While the setting **Log each step when a
vehicle is taken apart** is on (the default), each disassembly writes one log line per step, to find the cause. It changes nothing in
play. **Try one disassembly, on a copy of your save, and stop there:** each attempt runs the first half of the removal again.

## Bombs under Combat Extended

Under Combat Extended bombs are items with their real weight, so their ingredients now weigh close to what the bomb does. **The fuel
and explosive fills stop short of their real weight on purpose**: matching it would take far too long to make. Their selling price
follows from their ingredients. Without Combat Extended nothing here changes.

| Bomb | The Harrier mod's own | Now | Materials weigh |
| --- | --- | --- | --- |
| Mk. 82 (227 kg) | 60 steel, 45 chemfuel, 2 components | 1 Mk. 82 bomb case, 20 FSX, 300 chemfuel, 2 components | 166 kg |
| Mk. 84 incendiary (907 kg) | 120 steel, 120 chemfuel, 2 components | 1 Mk. 84 bomb case, 5 incendiary bomb fills, 2 components | 604 kg |
| Mk. 84 EMP | 120 steel, 6 components | 1 Mk. 84 bomb case, 8 EMP bomb coil packs, 12 components | 905 kg |
| Mk. 84 smoke | 120 steel, 50 chemfuel, 1 component | 1 Mk. 84 bomb case, 8 smoke bomb fills, 1 component | 909 kg |
| Mk. 84 poison gas (Biotech) | 120 steel, 4 wastepacks, 1 component | 1 Mk. 84 bomb case, 8 tox bomb fills, 1 component | 911 kg |

So four Mk. 82s take 4 bomb cases (1,120 steel), 80 FSX, 1,200 chemfuel and 8 components. Bombs already made stay as they are.

### Bomb fills (Mk. 84s)

**A Mk. 84's fill comes in canisters, each made in a short bill**, then the bomb is a case, its canisters and the components: three
trips for a hauler. Making it all in one bill took 14 to 20 trips, and The Dead Man's Switch's ammunition fabricator **drops everything
it has loaded when a haul is interrupted**, so its Mk. 84s were never finished. The materials in all are the same as before.

| Fill | Per bomb | Each made from | Weighs | Research |
| --- | --- | --- | --- | --- |
| Incendiary bomb fill | 5 | 20 prometheum, 300 chemfuel | 25 kg | IEDs |
| EMP bomb coil pack | 8 | 105 steel | 53 kg | Microelectronics basics |
| Smoke bomb fill | 8 | 95 steel, 125 chemfuel | 54 kg | Smokepop belt |
| Tox bomb fill (Biotech) | 8 | 9 wastepacks | 54 kg | Tox gas |

- **Where:** the machining table, and the ammunition fabricator with The Dead Man's Switch. 500 work each, Crafting 5. They stack to 10.
- **Incendiary and smoke fills burn, and a store of them can go up in a fire**, as chemfuel does. Keep them away from heat.

### Bomb cases

Each bomb starts as an empty steel case, made first and then filled.

| Case | Steel | Weighs | Work | Crafting skill | Stack |
| --- | --- | --- | --- | --- | --- |
| Mk. 82 bomb case | 280 | 140 kg | 1,000 | 6 | 4 |
| Mk. 84 bomb case (all four Mk. 84s) | 956 | 478 kg | 3,000 | 7 | 4 |

- **Where:** the machining table; the rotary forger, with The Dead Man's Switch - Expedition; and the automated machining bay, with
  Vanilla Furniture Expanded - Factory, which gives the bay a bill for each case by itself.
- **They stack to 4**, so a hopper slot holds four. **If your bays' hoppers were set to take heavy castings, allow the two bomb cases
  there instead.**
- **No research of their own**: each bomb keeps its own. They can be smelted back to steel.
- They are this mod's items: **use them up or sell them before removing the mod**.

## Bombs and rockets at the ammunition fabricator

With The Dead Man's Switch and Combat Extended, all five bombs and the jet's three kinds of 57 mm rocket are also bills at its
**ammunition fabricator**. The machining table still makes them, with the same cost and research. The Hind and Mongoose use the same
rocket recipes, so their rockets can be made there too.

| Item | Machine time for one bill | Colonist's Crafting skill |
| --- | --- | --- |
| Mk. 82 | 2,250 ticks (0.9 in-game hours) | 6 |
| Any Mk. 84 | 3,000 ticks (1.2 in-game hours) | 7 |
| 5 HE rockets | 6,500 ticks (2.6 in-game hours) | 4 |
| 5 HEAT rockets | 4,300 ticks (1.7 in-game hours) | 4 |
| 5 AP rockets | 2,750 ticks (1.1 in-game hours) | 4 |

That is half the machining table's work, and no colonist stands at the machine while it runs. It draws 1,000 W running, 50 W idle.

- **A colonist still loads it and collects the result**: the Crafting job "Operate Machine", with the skill above.
- **Finished items drop beside the machine**, whatever the bill's storage setting says; haulers take them from there.
- **Interrupting the colonist while loading drops what they had brought** beside the machine, and the job starts again. Nothing is
  lost, but the chemfuel-heavy bombs take many trips (1,500 chemfuel for an incendiary), so keep your chemfuel close.

## Heavy castings at the rotary forger

With RimThunder and The Dead Man's Switch - Expedition, the **rotary forger** makes heavy castings **12 at a time**: its bill is "make
12 heavy castings" (RimThunder spells the item "heavy compoment"). The electric smelter and assembly platform still make one at a time.

- **Cost:** 1,200 steel makes 12 castings and 12 lumps of steel slag, RimThunder's own recipe times 12. No research, no skill.
- **A colonist with Crafting enabled loads it** ("Operate Machine"): about 16 trips, as steel comes in stacks of 75. **The bill waits
  until 1,200 steel can be fetched**; below that, use the smelter.
- **It saves colonist time**: the fixed load and collect waits are shared by all 12. The machine then runs just over an in-game hour
  on its own, drawing 800 W (50 W idle).
- **Finished items drop beside the machine**: 24 separate things, since castings and slag don't stack. **Keep the floor around the
  forger clear.**
- **Interrupting the colonist while loading drops what they had brought** beside the machine. Nothing is lost.

## Turret hulls under Combat Extended

With Spaceship Turret Hull and Combat Extended both active, all seven armed hulls load like Combat Extended's own turrets: colonists
haul the ammo over, and the turret gets the usual reload and ammo buttons. Colonists reach a hull from any open cell beside it, so
**keep one clear on the inside of the ship**.

| Hull | Fires | Magazine | Range |
| --- | --- | --- | --- |
| Spaceship turret hull | 5.56x45mm NATO | 100 | 48 |
| Autocannon hull and heavy autocannon hull | 20x102mm NATO | 100 | 78 |
| 40mm flak hull (was the uranium slug hull) | 40x311mmR | 20 | 86 |
| Shotgun hull | 12 gauge | 20 | 19 |
| Sniper hull | .50 BMG | 10 | 84 |
| Incendiary hull | 30x64mm fuel cells | 10 | 45 |

- **Hulls built before must be rebuilt** to take ammo: deconstruct and rebuild each, **while the ship is landed** (in space the gap
  lets the air out).
- **They no longer explode when destroyed**, and aim and cooldown follow Combat Extended's nearest turret of each kind. The autocannon
  and heavy autocannon fire the same gun; the heavy one has more hit points.
- **Build cost, work, skill, hit points and power are the hull mod's own**, so these hulls are far cheaper than Combat Extended's
  turrets: a known gap.
- **Before removing this mod with Combat Extended active, deconstruct them.**

## InterRim Ballistic Missile

### A strategic blast

A strategic warhead landing on an open map used to damage everything in one frame, which on a big modded map froze the game for
minutes. Now:

- **The blast is spread over moments**, from the centre outward, and a strike ends in seconds. It takes up to 40 ms a frame while the
  screen is still white, and your setting (12 ms) afterwards.
- **What the warhead would destroy simply vanishes**: no rubble, filth or fragments (each is a setting). People still take the damage
  and die properly.
- **Built roofs are torn off first**, and **overhead mountain shelters what is under it**, thin or thick (the missile mod sheltered
  only under thick).
- **A container's contents go with it**; someone asleep in a casket dies first.
- **With Combat Extended, wall fragments are off during a blast** by default: they were what made blasts crawl.
- **No "Area revealed" letters** while a blast runs. Letters sent by an older version stay: right-click them away, or use the dev
  action below.

### The extinction-class warhead

The missile mod's one true blast warhead strikes with a shape, after Dubs Rimatomics' own nuke. **Ground zero is the centre of the
struck map**:

| Zone | Distance from the centre | What happens |
| --- | --- | --- |
| Fireball | Within 25 cells | Everything is gone, overhead mountain too; the ground is scoured to bare earth, ashen and polluted |
| Blast | 25 to 110 cells | Buildings, items and plants are gone; people take the blast and burns, worse nearer the centre |
| Heat | 110 cells to the edge | Walls may stand; plants wilt; people burn and, with Rimatomics, may be flash-blinded and irradiated; fires start, fewer towards the edge |
| Other settlements in range | | The heat, fires and fallout only, unless you choose a full strike everywhere |

- **Burns are spread over the skin**, so D'thorga Medical sees a burned body.
- **The pulse** stops powered buildings and mechanoids standing in the open for a few hours.
- **Fallout:** with Dubs Rimatomics, its radioactive fallout settles on every struck map for 10 to 30 days, and up to six clouds of
  it drift across the world. Without Rimatomics, the missile mod's nuclear winter. No fallout in space.
- **The screen stays white** until the fireball and blast are done (at most 8 seconds).
- The missile mod's letter, flash, sound, cloud and world effects are its own, unchanged. Other strategic warheads keep its effects,
  with the fast blast.

The zone sizes and damage are gameplay choices; every option is in [the settings](../../mods/PhobosPatches/README.md#settings).

### Messages, limits and dev actions

- A blast that runs longer than two seconds says it is still tearing through the place, and says when it has spent itself.
- **A blast in progress is not saved**: what it had not reached survives a reload. **Wait for the message before saving.**
- The warhead's fires, terrain changes and cloud still happen in the frame the screen turns white, as the missile mod does them.
- Combat Extended's fragment setting is changed only in memory while a blast runs; its settings file is never touched.
- **Dev mode**, D'thorga category: **strike current map** (destroys everything there: use a throwaway save), **strike current map with
  D'thorga's blast model**, **log blast queue**, **drop queued blasts**, **dismiss every "Area revealed" letter** and **count Combat
  Extended fragments on map**. Each finished blast logs one line with its counts and time.
- **Removing IRBM or RimPacts later is clean**: load the save once with this version first.

## RimPacts

- **An ally's assault leaves empty vehicles alone** (temporary). RimPacts counted a vehicle with nobody aboard as a standing defender,
  so allies spent hours beating on it by hand. They now go for the people and turrets, and the empty vehicle is left for you to claim.
  A crewed vehicle is still attacked.
- **After your missile strike, an ally's assault faces the town's garrison from before it**, not the few survivors, so your ally
  arrives with the squad it would have had. A town with nobody left standing still falls to the attack.
- **A world war attack must win before its deadline.** RimPacts gives it 3 days from accepting, checked once a day; if the target town
  has not fallen by then the war is lost, whatever the battlefield shows, and **control of about a dozen border towns passes to the
  enemy**. This is RimPacts' rule. The quest shows its countdown the way the game's own quests do. **Strike and attack early.**
- **The Harrier loads fully beside RimPacts**, and RimPacts' rule against landing on some maps keeps working.

## Visit Settlements

Visit Settlements closed a visited settlement's map the moment anyone there went down if it saw no colonist of yours standing, so
colonists inside a vehicle were lost with the map, and with RimPacts the defence too. **The map now stays open while:**

- a colonist of yours there is conscious, on foot or aboard a vehicle, shuttle or pod;
- a mechanoid of yours there is on its feet;
- or a RimPacts defence or battle is fought there.

Otherwise Visit Settlements' rule stands. **Pawns already lost this way stay lost**: load a save from before. When it keeps a map open
the log says "Kept the map of ... open", once an in-game hour.

Its food drops for friendly settlements also no longer fail on every tick when a settlement holds a pawn that does not eat (a
mechanoid, a robot, an android). That fault filled the game's log in minutes.

## Outposts and caravans with vehicles

With Vanilla Expanded Framework and Vehicle Framework, the **"create outpost" window refuses a caravan that holds a vehicle**, and
says why: founding one that way looks able to fail part-way and lose the vehicles and their riders (read from the code, not seen in a
game). **Leave the vehicles somewhere first and come on foot.** The setting **Refuse to found an outpost from a caravan with vehicles**
puts the original behaviour back, with no restart.

## Vehicle Framework and Combat Extended

- **Fires a vehicle could never put out are put out**: a second fire on one vehicle, or one left on a map the vehicle has left, goes
  within a few seconds, with a message. A vehicle leaving a map takes no fire with it. A burning vehicle could never be refuelled.
- **Repairs and upgrades show the game's progress bar** beside Vehicle Framework's sparks (temporary).
- **With Combat Extended, one bad entry in an ammo set no longer stops the whole ammo setup** at startup (temporary). The Dead Man's
  Switch - Synthriada adds such entries; they are dropped and the log says how many.

## Drone platforms

Not yet tested in game. The Dead Man's Switch's drone platforms (the **engineering drone bay**, the **trashpan** and **hound stations**
and the **FPV nest**) hold steel that each drone they send out uses up. On their own they only offer a right-click order, **Fill**.

- **Colonists doing hauling work now refill them by themselves**, once a platform is short by one drone's worth of steel or more.
- **Keep steel where haulers can reach it**, and leave the platform unforbidden.
- **A platform fills to the level its own Fill order reaches**, which is its starting store, not its full capacity.
- **The order by hand still works**, for less than a drone's worth too.
- **To switch it off**, untick **Haulers keep machines topped up** in D'thorga Framework's settings.

## Where to find things

- The Harrier's blueprint: Architect, Vehicles. Its turbine: a bill at RimThunder's assembly platform.
- Bombs and rockets: bills at the machining table, and at The Dead Man's Switch's ammunition fabricator.
- Bomb cases (Combat Extended only): bills at the machining table, the rotary forger and the automated machining bay.
- Heavy castings: the electric smelter and assembly platform (one at a time), and the rotary forger (12 at a time).
- Settings: **Mod settings, D'thorga Patches** (the missile and air-defence options), **D'thorga Patches: Harrier** and **D'thorga
  Patches: outposts**.

## What has been tested in game

- **Tested by the owner:** Vehicle Framework's stuck fires went out (10 November 2026); one missile strike on an earlier version.
- **Not yet tested in game:** everything else.

The settings and the links for modders are in [D'thorga Patches' README](../../mods/PhobosPatches/README.md).
