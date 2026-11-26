# The house battery, made honest

**Status (27 Nov 2026):** built in D'thorga Framework 0.333.0, not yet tested in game (rows 639 and 640): the charge travels with the
battery, 24-volt vehicles take two, lead-acid stops at half and freezes run down in deep cold, and lithium is a second fitting. The
lithium battery's picture waits for the owner's pick.

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
| Wear from deep drains | **None for now**: the cut-off alone protects it | Owner's card |
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
