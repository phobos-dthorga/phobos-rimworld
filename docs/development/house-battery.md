# The house battery, made honest

**Status (27 Nov 2026):** built in D'thorga Framework 0.333.0 (rows 639 and 640), with wear and a solar panel added in 0.344.0 (rows 672
and 673), none yet tested in game: the charge travels with the battery, 24-volt vehicles take two, lead-acid stops at half, freezes run
down in deep cold and wears with use, lithium is a second fitting, and a solar panel charges a parked vehicle by day.

The owner asked on 27 Nov 2026 whether the house battery fitting was "appropriately modelled". It was not, in four ways, and the owner
chose the fixes by card the same day. How the house battery works now is in
[camping and life aboard](systems/camping-and-life-aboard.md); this record keeps what was wrong, what was chosen and why.

## What was wrong

| # | Fault | What a player saw |
| --- | --- | --- |
| 1 | **The charge belonged to the vehicle's tray, not the battery.** Every system's figures are made when a vehicle first spawns, fitted or not, and stood still while nothing was fitted | A flat battery taken off one truck and fitted to another came up full; a new battery fitted where a flat one had been came up flat |
| 2 | **One 25 kg battery gave a 24-volt vehicle twice the energy.** 100 Ah at 24 V is 2.4 kWh, at 12 V 1.2 kWh, while the camp loads draw by the watt | A Humvee, MaxxPro, heavy truck or train camped twice as long on one battery |
| 3 | **It weighed like lead-acid and drained like lithium**, to a tenth, with no harm | Nothing to weigh against: one battery did everything |
| 4 | **Cold never touched it**, though the starting battery freezes run down in deep cold | Winter camping cost nothing extra |

A fifth, the same spare alternator output counted for both batteries while both charge, stays as a labelled simplification: at most
about a tenth more than the alternator gives, and only while the starting battery is above 90 % and the house battery below 80 %.

## What was chosen

| Question | Choice | Whose |
| --- | --- | --- |
| A 24-volt vehicle | **Two batteries in series**: twice the cost, the weight and what comes back, 100 Ah at 24 V | Owner's card |
| The chemistry | **Both, as two fittings**: lead-acid early, lithium behind Microelectronics | Owner's card |
| Wear from deep drains | **None for now** at first, then **built** in 0.344.0 (the owner's later card of the same day, "Deep-discharge wear"): lead-acid only, behind the setting **House batteries wear**, on by default | Owner's cards |
| The cut-off, freezing's grade, the fuel heater parked | **As built**: half; moderate, so on; from the battery down to the start line | Owner's cards of 27 Nov 2026 |
| A solar panel | **Built** in 0.344.0: 200 W on a mount of its own, charging the house battery, or the starting battery without one, by daylight with the engine off | Owner's card ("Solar panel"); the size, mount and efficiency are the agent's defaults |
| Lead-acid's cut-off | **Half its charge** (50 Ah usable), its low warning at 60 % | Agent's default, on common practice |
| Lithium's cut-off | **A tenth** (90 Ah usable), its low warning at 20 % | Agent's default, on manufacturers' figures |
| Lithium in the cold | **No charge below 0 °C**, full from 2 °C; it never freezes and still gives power | Agent's default, as its management board does |
| Lithium's kit | 12 kg, 10 steel and 4 industrial components, Crafting 6, worth 380 | Agent's default |
| Which battery a fitting uses | **The fullest aboard**, from the work store first | Agent's choice |
| A loose battery | **Keeps its charge**, with no self-discharge while it lies in storage | Agent's simplification |

## The four questions

- **What it lets the player do:** carry charged batteries between vehicles and swap a flat one for a full one, as people really do;
  choose lithium for more usable charge at half the weight, once Microelectronics is researched.
- **What it stops them doing:** getting a free full battery by moving one kit between vehicles; camping twice as long on a 24-volt
  truck for one battery; draining a lead-acid battery to nothing without cost in deep cold.
- **How it changes the way they play:** a winter camp wants the engine run or lithium; a big truck's house bank is a real expense; a
  spare charged battery in the hold is worth something.
- **What outside the vehicle notices:** the battery item itself (its charge shows on its inspect line, and it can be stored, carried
  and traded), the weather (deep cold freezes a run-down lead-acid battery), and a letter-free warning message when it starts to
  freeze. Nothing else: no person or faction reacts to a battery, a choice.

## Risk: a freezing lead-acid battery

**Graded moderate** (agent's grade, for the owner to confirm): one battery kit is lost, slowly (about half a day once it starts to
freeze: 2 a hour on a 25-health tray), and the player is warned when it starts. Running the engine, shore power or a warmer place
saves it. So it has its own setting, **"Run-down house batteries freeze"** (Vehicle Systems, Camps), **on by default** under the
shared rule for a slow loss with a warning. The starting battery's own frost, older, has no setting yet; it follows the rule when next
touched.

## Other factions' vehicles

- **In a raid on us:** a raider's vehicle with a house battery behaves as ours. Its kit, taken off after capture, holds whatever charge
  it had.
- **Parked at a base we attack:** the same; a run-down lead-acid one in deep cold may already be cracked and gone.
- **Captured:** its battery's charge stays with the battery; a 24-volt vehicle's two come back as two.
- **Cost with several on a map:** nothing new each tick. The cut-off is read from the fitted system's figures, and the charge is read
  only when a battery is fitted or taken off.

## How it is built

- **The charge travels with the kit** through two generic pieces any fitting may use: the fitting's `kitCarries` names a system quantity,
  and its kit holds D'thorga Framework's `CompPhobosCharges` (here 100 uses, so its inspect line reads as a percentage). Fitting reads
  the kits just used (`PackedThings.Take` shows each thing taken; the vehicle keeps their shares for that tick); taking it off writes
  the quantity back into the kits given back. With no kit read (a dev action's fitting) it is full.
- **Units in series** come from the fitting's `unitVolts` and the vehicle's electrical voltage (`FittingRule.UnitsInSeries`): materials,
  kilograms and what comes back are multiplied, and the fittings window says why.
- **Two systems, one quantity:** `PhobosVehicleSystem_HouseBattery` (lead-acid, granted by `HouseBatteryLeadAcid`) and
  `PhobosVehicleSystem_HouseBatteryLithium` (`HouseBatteryLithium`) share the quantity `houseCharge` and the tray, so the camp loads, the
  dashboard and the charge carried in the kit work for either. Each names its own cut-off, `houseCutoff`; the rules Def's
  `houseBatteryCutoff` is only the fallback.

## Wear (Framework 0.344.0)

- **What wears:** a lead-acid house battery's **capacity** (`houseCapacity`, nothing to whole, at least a fifth), which every charge and
  drain flow divides by, so a worn battery fills and empties faster for the same amps; the amps billed and loaded on the alternator are
  corrected for it.
- **Use:** about 0.0008 of capacity per whole capacity drawn while above 60 %, rising to x1.5 at half, x3 at 30 % and x6 flat, after the
  usual depth-of-discharge curves and roughly 500 cycles to 80 % at half depth (secondary, common makers' figures; unverified).
- **Sitting run down:** about 2 % of capacity a week at half charge, 5 % a week flat, nothing above 55 % (authored; the direction is real).
- **It travels as the battery's hit points** (the fitting's `kitHealthCarries`): a worn battery moved stays worn, and its trade value falls
  with it. A new battery is whole; nothing else restores it. Hit points lost to weather in storage count as wear too, a simplification.
- **Risk graded low** (agent's grade): a slow loss of capacity, shown by a message below four-fifths and on the battery, never a battery
  lost. Its setting starts on. Lithium does not wear (its cycle life is several times longer; a simplification).

## The solar panel (Framework 0.344.0)

- **A fitting on its own mount** (`SolarMount`, on every land vehicle, outside the body), so it never takes the roof mount from a tent;
  200 W (`solarWatts`), 12 kg, behind the game's solar panel research.
- **What it gives** (`Input_SolarAmps`): its watts times the daylight times the charge controller's 0.85 (authored), over the vehicle's
  volts. On a map the daylight is the map's sky glow, as the game's own solar generator reads it, times the share of the vehicle's cells
  under open sky; on the world map, the sun's glow at its tile. About 14 A at noon on 12 V, 7 A on 24 V: the same watts.
- **Where it goes:** with the engine off, into the house battery if one is fitted (lithium only above freezing), else into the starting
  battery; with the engine running, nowhere (the alternator charges then; a simplification).
- **Other factions:** works the same on any vehicle that has one; none comes with one.

## Sources and labels

- A dual battery system's isolator or DC-DC charger: REDARC's explanation (secondary; unverified, as the accessories record gives it).
- Lead-acid deep-cycle batteries drawn to about half for a long life, and freezing points rising as they empty (about -8 °C flat, -24
  °C at half): common practice and commonly published tables, secondary and unverified. The freezing curve is authored on that shape.
- LiFePO4: about 11 to 13 kg for 100 Ah, usable to about a tenth, no charging below about 0 °C: manufacturers' figures, secondary
  and unverified.
- Every other figure (costs, work, skills, self-discharge, tapers) is authored.

## For a next game

The two generic pieces need nothing game-specific beyond RimWorld's item comps: a game with items that hold a charge and fittings
made from them would need its own form of `CompPhobosCharges` and of the taking hook, and the same two fields on its fittings.
