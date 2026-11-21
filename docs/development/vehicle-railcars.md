# Railcars: a loadout for Trains of the Rim's wagons (22 Nov 2026)

**Status (22 Nov 2026):** a design record with questions for the owner. Nothing here is designed in full or decided yet.

## Read this first

- **Why this exists.** On 22 Nov 2026 the owner picked Trains of the Rim's **Flatbed**, **Flatbed with Stakes** and **Gondola** to take
  a deck tent ([vehicle-accessories.md](vehicle-accessories.md#built-tents-for-every-vehicle-22-nov-2026), step 4). They could not be done:
  **our vehicle systems do not fit a vehicle with no engine**, so a railcar has no Health tab of ours, no fittings window and no part
  for any fitting to go on. The owner chose a design record first.
- **The mod looks unmaintained, and that comes first** (owner's rule: say an author's activity up front). Read with
  `python scripts/workshop-research.py report "Trains of the Rim"` on 22 Nov 2026: last updated **26 Aug 2025**; it declares
  **RimWorld 1.6 only**; 81,172 subscribers; of the 50 newest comments, 14 are from the last 90 days and 12 of those mention a
  problem, **most of them asking for 1.7**; **no reply from the uploader** among those 50. Its public source
  ([PrinceOfPluto/Trains-of-the-Rim](https://github.com/PrinceOfPluto/Trains-of-the-Rim), MIT) was last pushed the same day.
- **It is on the owner's active list** and our steam and diesel-electric loadouts already cover its locomotives
  ([vehicle-steam-design.md](vehicle-steam-design.md)). **Whether it works in the owner's 1.7 game is not known here**: nothing in this
  record was tested in game.
- **Marks:** **[Def]** read from the mod's Defs with `scripts/query-defs.py`; **[code]** read from its public source with
  `scripts/query-code.py`, outlines and one method only, summarised in our own words; **[A]** authored; **[I]** our inference.

## What a railcar is

| Question | Finding |
| --- | --- |
| Which vehicles? | Eight wagons [Def]: Boxcar, Caboose, Flatbed, Flatbed with Stakes, Gondola, Hopper, Passenger Car, Tanker. Five locomotives besides, which already have loadouts of ours |
| What kind of vehicle? | A land vehicle of Vehicle Framework's own class, on the mod's `VehiclePawn_Railroad` parent, 3 x 10 cells [Def] |
| Its parts | Front and rear **bogie** (they carry its move speed), front and rear **coupler**, a **frame** [Def, the Flatbed]. No engine, no fuel, no brakes of its own, no cab |
| Its seats | One passenger seat [Def, the Flatbed] |
| Its load | The Flatbed carries 15,000 by Vehicle Framework's cargo figure; the Gondola 30,000 [Def; the tipper record read it] |
| How does it move? | By itself. A train is a list of member vehicles with a place each relative to the locomotive (`TrainConsist`); ordering the train gives **every member its own job to go to its place** (`TrainVehicleComp.OrderToPosition`) [code]. Nothing tows a wagon: there is no hitch of ours or theirs between cars [I] |
| So what would our systems see? | A vehicle that moves under its own pather. Distance, speed and "moving" would read as for any vehicle; no trailer catch-up is needed [I] |

## What our systems need

- **A loadout with at least one system.** The service runs only for a vehicle whose loadout has systems
  (`CompPhobosVehicleSystems.Usable`). With it come our Health tab, wear, repairs and the fittings window; without it, nothing.
- **A part for each fitting.** The deck tent goes on a roof mount; an awning or a tent on a side rail. Loadouts append those parts.
- **A flat-deck mark** on the three wagons the owner picked (`PhobosFlatDeckExtension`), as on the trucks.

## Three ways to do it

| | What it is | What it costs |
| --- | --- | --- |
| **R1. A slim wagon loadout** | One wear system on the two bogies, and the two camp mounts. As the trailer loadout is: enough for our Health tab and fittings, no more | Wagons join the maintenance round. A new small system, since a wagon's wheels are steel, not tyres: our tyre system's words ("tread", "tyres burning") would be wrong on a railcar |
| **R2. A wagon as a railway vehicle** | R1, plus what a real wagon has: **axle boxes** that run hot when their oil is gone, **couplers** that wear and can part, and **brakes** worked from the locomotive's brake pipe | A real design, with cross-links to the locomotives' brake pipes and our lubrication service. Much more to build and to test, on a mod that may never see 1.7 |
| **R3. Leave railcars alone** | Nothing built. The note in the accessories record stands | The owner's pick of three wagons stays undone |

**The agent's recommendation: R3 for now, R1 only if the owner plays with trains in the 1.7 game.** A mod not updated in fifteen
months, declaring a version the owner no longer plays, is a poor foundation for new work, and R2 is the kind of depth worth spending
on a mod that is alive. R1 is small and harms nothing if the mod works for the owner today.

## R1 in a little more detail

| Question | Answer |
| --- | --- |
| What it lets the player do | Fit a deck tent on a flatcar or gondola, an awning on any wagon, and see a wagon's state on our Health tab |
| What it stops them doing | Running a wagon for ever with no upkeep: worn wheels slow it |
| How it changes play | A train can be a camp: a flatcar with a tent, a wagon with an awning. Wagons need a wheel service now and then |
| What outside the wagon notices | The colonists (beds on a flatcar), the mechanic (a new service), the train's pace (a worn wagon is slower, and a train keeps together [I: not read]) |

- **The system [A]:** "wheels": one quantity, wheel wear, growing with distance at a small fraction of a tyre's rate (steel on steel
  lasts far longer than rubber; no figure was read), with two stages, worn and worn out, that slow the wagon. Renewed by a fitting
  made of steel. No fire stage, no ground roughness (a wagon stays on rails).
- **Mounts:** a side rail on every wagon; a roof mount, labelled a deck mount, only on the three flat ones.
- **Risk: low** (agent's grade): slow wear the Health tab shows. No setting beyond the loadout kind's own switch, as trailers have.
- **Other factions:** no faction arrives by train, and none keeps wagons at a base; nothing to design.
- **Cost to run:** one system with one quantity per wagon on the rare tick. A long train is many vehicles: ten wagons are ten such looks.
- **Not checked:** whether a train keeps its wagons together when one is slowed by wear; whether a wagon with one passenger seat is
  treated by Vehicle Framework as driving itself (it has no driver's seat [Def]).

## If R2 were ever wanted

Ideas only, none designed. Each would get its own four questions and risk grade first.

- **Hot boxes.** A wagon's axle bearings ran in oil-soaked packing; run dry, they overheated and could set the wagon alight. Here: a
  quantity of axle oil that a mechanic tops up, a hot stage that shows smoke, and a fire stage. That last would be **high risk** (a
  wagon and its load lost), so by the shared rule it would have its own setting, off by default. **From memory; no source was read.**
- **Couplers.** Wear with distance and with the weight behind them; worn out, the train parts and the rear wagons stop.
- **Brakes.** Worked from the locomotive's brake pipe, which our steam loadouts already model as the locomotive's brakes.
- **Sleeping cars.** The Caboose and the Passenger Car as berths, on the cab bunk's rules.

## Questions for the owner

1. **Go ahead at all?** R3, leave railcars alone (agent's recommendation unless trains are in use); R1, the slim loadout; or R2 designed
   properly first.
2. **Do you use Trains of the Rim in the current 1.7 game, and does it work there?** This decides most of question 1.
3. **If R1: which wagons get the deck mount?** The three picked (agent default), or every open wagon including the Hopper.

## Who chose what

- **Owner (22 Nov 2026):** the three wagons for a deck tent; a design record before anything is built.
- **Agent, for the owner to revise:** the three ways and the recommendation; every detail of R1; the ideas under R2.

## Sources

- [Trains of the Rim on the Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3530446424), by PrinceOfPluto: its
  update date, declared version and comments, read through Steam's public APIs on 22 Nov 2026.
- [PrinceOfPluto/Trains-of-the-Rim](https://github.com/PrinceOfPluto/Trains-of-the-Rim) (MIT): `TrainVehicleComp` and `TrainConsist`,
  outlines, and `OrderToPosition` in full.
- The mod's Defs as installed: `TOTR_Trains_Railcar_Flatbed` and its parent.
- Our own: [vehicle-real-figures-design.md](vehicle-real-figures-design.md) ("vehicles with no engine ... railcars, which the systems
  do not fit"), [vehicle-trailers.md](vehicle-trailers.md) (the slim trailer loadout this would follow).
