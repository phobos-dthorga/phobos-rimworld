# Compressor wash (26 Nov 2026)

**Status (26 Nov 2026):** built in D'thorga Framework 0.300.0. Not yet tested in game: row 539.

How engines work now: [systems/engines-and-drivetrain.md](systems/engines-and-drivetrain.md). This record keeps the design and its reasons.

## Read this first

- **The owner's question (25 Nov 2026):** the A-10, the Frogfoot, the Cherokee and other aircraft had no plumbing beneath them when parked.
  That was the rule working: a vehicle lays plumbing only when it takes water ([network-underlays.md](archive/network-underlays.md)), and
  a gas turbine has no water cooling. The owner asked whether turbines take water. Water injection is real on only a few engines (the
  Harrier's, already modelled), **but every turbine gets a compressor wash**. The owner chose to model it (question card, 25 Nov 2026).
- **What it changes for the player:** every turbine vehicle (jets, helicopters, the Osprey, the Abrams) now takes water, so it **lays
  plumbing beneath it when parked with its engine off**, like any vehicle that takes water. It also **asks for a wash about every 50 hours
  of running**.
- **Its setting:** **Turbine compressors get dirty**, on by default, in D'thorga Framework's settings, Flight section.

## The design

Dust, grit and salt drawn in with the air coat a gas turbine's compressor blades, so it moves less air. The turbine gives less power for
the same fuel, and runs hotter for the same work. Ground crews rinse the compressor with water to win the power back. Fouling is the commonest
cause of a gas turbine's lost performance, and washing recovers much of it ([Meher-Homji and Bromley, "Gas turbine axial compressor fouling
and washing", Proceedings of the 33rd Turbomachinery Symposium, Texas A&M, 2004](https://oaktrust.library.tamu.edu/bitstream/handle/1969.1/163249/t33-18.pdf),
an overview paper). The figures below are ours, not the paper's.

| What | How it works | Source |
| --- | --- | --- |
| Fouling | A share from clean (0) to fully fouled (1), kept per vehicle. It grows only while the engine runs | Owner's cards |
| Rate | 0.01 an hour on paving; times (1 + 2 × the rough share of the ground under it × the intake's exposure). The exposure is the loadout's `intakeDebris` figure that the grit-in-intakes rule already reads (0.5 for most aircraft) | Authored; the shape is the owner's card "hours, faster off paving" |
| When a wash is due | At half fouled: about 50 running hours on paving, about 25 on rough ground for an exposure of 0.5. A loadout with no exposure fouls at the paving rate anywhere | Owner's card "about 50 engine hours" |
| What fouling costs | Up to 5% of output when fully fouled, as a factor on the turbine's condition. The turbine model reads condition for its output, its temperature at a demand and its fuel. So the same work runs hotter, a helicopter's hover margin shrinks, and the existing wear-fuel rule burns 0.45 × 5% = **2.25% more fuel** when fully fouled | Owner's card "less power, runs hotter, plus more fuel"; 5% authored, inside the few percent published |
| Fouling never stops anything | It stops growing at fully fouled. A turbine never washed is weaker, never grounded | Agent's choice |
| The wash | A service, due above half fouled, that restores fouling to 0. It takes **about 20 litres of water for each engine** and 600 work, wherever the vehicle is parked. The water comes from Dubs Bad Hygiene's plumbing under or beside the vehicle, or is free without that mod, as the coolant top-up works | Owner's card "water per engine, anywhere"; 20 L and 600 work authored |
| On a world flight | Fouling grows at the paving rate (no ground under it) | Agent's choice |

**How it is built.**

- **`PhobosVehicleSystem_CompressorWash`** (`Systems_Turbine.xml`): the `compressorFouling` quantity, its "dirty" stage with a message at
  half, the fouling flow, and the wash service.
- **The gas turbine brings it** through a new `comesWith` list on a system Def. Every loadout that names the gas turbine gets the wash once,
  whether it inherits its system list or writes its own. Generic: any system may name the systems that always come with it.
- **`waterPerEngine`** on a service multiplies its litres by the vehicle's engines.
- **`CompressorFoulingRule`** (game-free, with tests) holds the rate and the output factor. **`Input_CompressorFouling`** feeds the flow.
  **`Powertrain.Condition`** multiplies in the factor. The three figures are in `Rules_VehicleSystems.xml`.
- **Plumbing beneath:** the wash's water makes the vehicle one that takes water (`VehiclePipes.SystemsTakeWater`), so Dubs' plumbing is
  laid beneath it whenever it stands with its engine off. Nothing new was needed for that.

## The four questions

- **What it lets the player do:** keep their turbines at full power by washing them, and plumb an airfield or a hangar so a wash needs
  nobody to carry water. Paving a hardstanding or an airstrip now pays off twice: less grit in the intakes, and less fouling.
- **What it stops them doing:** nothing. A dirty compressor costs a little power and fuel and never refuses a start or a flight.
- **How it changes the way they play:** a wash joins the coolant top-up and the hot-section overhaul as routine work. A helicopter that
  hovers near its limit hovers a little less well when dirty. The hot section wears faster when a weaker engine is pushed past its
  continuous limit, so washing also spares the overhaul.
- **What outside the vehicle notices:** the colony's water towers give the wash its water. The plumbing overlay shows pipes beneath every
  parked turbine vehicle. A colonist does the work, and the game's message and the vehicle's logbook record it.

## Risk and its setting

**Risk: low** (agent's grade, for the owner to confirm). The player can lose at most 5% of a turbine's output and about 2% more fuel, slowly,
and is warned at half fouled. Nothing is lost at once. So the setting **Turbine compressors get dirty** starts **on**. Off, nothing fouls,
and fouling already gained costs nothing.

## Other factions' vehicles

- **In a raid on us:** a raider's turbine vehicle fouls as it runs, like ours. It arrives clean unless it has a history, and nobody washes it.
- **Parked at a base we attack:** it lays no plumbing (other factions never do) and fouls only while its engine runs.
- **Captured:** it keeps the fouling it had, and our mechanics can wash it at once.
- **Cost:** one figure per turbine vehicle, read at each systems update. The rough-ground share counts the cells under the vehicle, as the
  grit rule already does.

## For a next game

`CompressorFoulingRule` is game-free and could serve any game's gas turbine. The rest is RimWorld's systems engine.
