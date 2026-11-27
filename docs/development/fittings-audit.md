# The fittings audit: what each fitting promises, and what is modelled

**Status (27 Nov 2026):** five gaps found; four fixed in D'thorga Framework 0.334.0 (rows 646 to 649), and the four smaller ones closed in
0.344.0 (row 671), none yet tested in game. The check that keeps it so is built: phobos-modkit 1.133.0's `effects`, the gate's "declared
effects" step, by `config/effects.json` (it also shows on D'thorga Companion's Reports page).

After the house battery ([its record](house-battery.md)), the owner asked on 27 Nov 2026 which other fittings and customisations were
"available but not actually modelled in any circumstance". Every fitting (65), tune (9) and upgrade node (159) of ours was read by a
one-off script, each declared effect (a grant, a figure, a load, a mount) checked for something in the Defs or the C# that reads it, then
each fitting's description read against what it moves.

**Nothing is wholly dead**: every declared effect has a reader. What was wrong was a promise only partly kept.

## Fixed in Framework 0.334.0 (the owner's cards of 27 Nov 2026)

| Fitting | What was missing | Now |
| --- | --- | --- |
| Vehicle extinguisher | Its charges lived on the vehicle: an empty kit moved to a vehicle that never had one came up full | The kit holds its charges (`CompPhobosCharges`), carried through the fitting's `kitCarries` and a comp's kit store (`IPhobosKitStore`) |
| House battery (both) | Its shore charger filled it from nowhere: only the starting battery's charger was billed | The hook-up bills what flowed into the house battery |
| Block heater, battery warmer | They warmed the vehicle from the grid with nothing billed | The fitting's `gridWatts` and `gridWhen`: 750 W while the coolant is below 40 °C, 80 W below 5 °C |
| Fuel heater | Did nothing unless the gelling setting was on, and drew nothing when it did | It stays offered and says so (owner's card: "leave it offered, say so"); a 12 A load keeps the fuel thin while it runs, going out to keep the start |
| ECM pod | Jammed on a flat battery or with its load shed, and drew 15 A even parked | Works only while its load runs, which draws only in the air; the Prowler's ICAP reach likewise needs its load |

**Agent's choices, for the owner to revise:** the wattages (750 W block heater, 80 W battery warmer, 150 W fuel heater, all authored within
secondary, unverified ranges); the fuel heater running from the battery parked off the grid until it reaches the start line, as the camp
lights do; another faction's vehicle, or one ticked at less than full detail, never refused for power, as the feeds rule already does.

## The four questions

- **What it lets the player do:** carry a part-used extinguisher to another vehicle as it is.
- **What it stops them doing:** getting a full extinguisher by moving a kit; warming engines and charging house batteries on the colony's
  grid for nothing; a pod that jams with no power.
- **How it changes the way they play:** a cold-weather vehicle park on the grid now costs the colony power, enough to plan for; a fuel heater
  wants a running engine, the grid or a charged battery.
- **What outside the vehicle notices:** the colony's power grid, which now carries the heaters and the house battery's charge. Nothing else.

**Risk:** none of these puts the player's things or people at new risk; a fuel heater that goes out leaves the fuel to gel, as before
fitting one. No new setting.

## The smaller gaps, closed in Framework 0.344.0 (the owner's card of 27 Nov 2026, "close all four")

| Gap | Now |
| --- | --- |
| A crew water tank taken off left its water on the vehicle, back on a refit | **A tank taken off is drained** at the next look at it, in a caravan too |
| Fitting a deep-cycle battery topped the battery up: its charge was kept as a share | **A store kept as a share of a capacity keeps its amount** when a fitting changes the capacity (`FittingRule.ShareAfterCapacityChange`), found from the flows that divide by it, so any such store gains it |
| Ground kits authored at 12 V drew their amps at any voltage, so twice the watts on a 24-volt truck, though their own comments said "halved" | **The route kit, the mech control link and the recovery beacons follow the voltage** (`Input_TwelveVoltShare`); the remote control kit, the repeater kit and the dual-band radio, which are the Radio Framework's, went to that mod's own next version |
| The Prowler's ICAP descriptions did not say the wider reach needs power | **They do** (D'thorga Vehicles 0.129.2) |

**Left as authored:** the aircraft kits (navigation, autopilot, night sight, radar, jamming pod, optionally piloted, rescue hoist, cabin
oxygen) keep their amps, which follow the avionics rule's 24-volt figures; our aircraft loadouts name no voltage and run at the default
12, a separate question for when aircraft electrics are next touched.

## For a next game

The check is game-free (phobos-modkit `effects`, docs/effects.md there): a next game needs only its own `config/effects.json`, naming
its Def types and where their effects are read, and a reader for its data's form if that is not XML.
