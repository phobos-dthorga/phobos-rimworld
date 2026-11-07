# Caustic gas: research and decisions (5 Nov 2026)

A record for a future feature of [D'thorga Burns and Smoke](../../mods/PhobosMedical/README.md): what the game's tox gas does
now, and the lingering injuries a caustic gas could add. **Nothing is built and nothing is tested in game.**

## The problem

The owner asked for a more down-to-earth tox gas, working from the game's own word for it, "caustic". In the game, a pawn in tox gas
loses sight and breath while standing in it and gains toxic buildup. The moment the cell clears, the irritation is gone. Nothing
touches the skin, nothing comes on later, and nothing is left behind except the buildup.

**This is a game design, not a medical reference** (owner agreed, 5 Nov 2026). Real caustic gases inspired the two models below, but
this record carries no data on real agents. Every figure is an authored gameplay value, chosen for how it plays. As the owner put
it, a mod always carries its makers' flavour and preferences, and players accept that when they download it.

## The chain, as read from the code (5 Nov 2026)

Read from the 1.7.4998 assembly decompiled to the console with ilspycmd, the Biotech and Core Defs, Combat Extended 16.7.4 (its
decompile in `.local`), and the Defs of the owner's active mods. Worked figures are our arithmetic from the read constants and are
marked inferred.

- **Tox gas has no Def.** `Verse.GasType` has four values, `BlindSmoke`, `ToxGas`, `RotStink` and `DeadlifeDust`, packed into one
  `uint` per cell by `GasGrid`. Each is capped at 255; a cell over the cap floods its neighbours up to radius 40. Tox gas fades 3 per
  visit outdoors and 2 under a roof (`DissipationAmount_ToxGas = 3`, halved by the roof factor), each cell visited about every 64
  ticks, so a full cell clears in about 2.2 game hours outdoors and 3.3 under a roof (inferred). It spreads to a cardinal neighbour
  only when it holds 17 or more units. A full-fillage building blocks it, a door passes it only while open, and spawning a building
  clears its cells. `AddGas` refuses tox gas without Biotech.
- **"Caustic" appears in two places only:** the research project `ToxGas` ("Produce weapons that utilize caustic tox gas",
  `Biotech/Defs/ResearchProjectDefs/ResearchProjects_Misc.xml`) and Combat Extended's ammo category `Toxic` ("Filled with tox gas, a
  caustic agent that burns the lungs and eyes"). The items share one line: "Tox gas burns the lungs and eyes, causing a temporary
  shortness of breath and reduction in sight. Continued exposure to tox gas results in toxic buildup which can be lethal." The gas
  mask: "It prevents tox gas irritants from entering through the eyes and mouth."
- **Sources of the gas** (all Biotech): the mortar `Shell_Toxic` (`CompProperties_ReleaseGas`, 20 cells over 10 s), the toxbomb
  launcher (radius 1.9), tox grenades (1.9), the IED tox trap (8.9), the tox pack (45 cells over 12.75 s), wastepacks when damaged by
  violence (`CompProperties_GasOnDamage`; dissolving pollutes and releases nothing) and a dying toxalope. An explosion writes 255 into
  every cell of its radius. The `ToxGas` damage Def itself harms nothing. Polluted terrain, toxic fallout, the Royalty spewer, Odyssey's
  ancient vents, tox rain and toxic water all add buildup directly and emit no gas.
- **Two effects, no HediffGiver.** `GasUtility.PawnGasEffectsTickInterval` runs every 50 ticks from `Pawn.Tick`. For a spawned pawn in
  tox gas it takes density over 255 (a quarter of that once buildup is at its last stage), gives humanlikes `ToxGasExposure`, then
  calls `ToxicUtility.DoPawnToxicDamage`.

| Effect | What it is | Figures (Defs) |
| --- | --- | --- |
| `ToxGasExposure` (`Biotech/Defs/HediffDefs/Hediffs_Local_Misc.xml`) | Whole body; severity set straight from the cell's density every 60 ticks; removed as soon as the cell is clear | mild: pain +0.10, Sight −0.10, Breathing −0.15; moderate: +0.15, −0.25, −0.30; severe: +0.25, −0.40, −0.60. Skipped for any worn item or gene with `immuneToToxGasExposure`; humanlikes only |
| `ToxicBuildup` (`Core/Defs/HediffDefs/Hediffs_Global_Misc.xml`) | Systemic poison, lethal at 1, falls 0.08 a day | 0.023 × (1 − ToxicResistance) × (1 − ToxicEnvironmentResistance) × density × 0.85 to 1.15 per 50 ticks: about 1.15 a game hour at full density bare-faced, lethal in about 1.4 game hours (inferred). Vomiting from 0.40, dementia and carcinoma clocks from 0.40, consciousness capped at 0.10 from 0.80 |

- **Protection.** `ToxicResistance` is the systemic stat (the detoxifier kidney, venom). `ToxicEnvironmentResistance` (TER) "protects
  against tox gas, rot stink, toxic fallout, and polluted terrain". The gas mask has TER 0.8 and `immuneToToxGasExposure`; the cloth
  mask TER 0.5 and no immunity; the detoxifier lung TER 0.6 and "does not prevent the irritating effect of tox gas"; Odyssey's vacsuit
  helmet TER 0.8 and immune. Animals have ToxicResistance 0.5 and never get the exposure hediff; insects and mechs resist fully.
- **Nothing else.** Tox gas does not cause lung rot (`LungRotExposure` reads rot stink only), does not hurt plants, corpses or items
  (toxic fallout does), does not block sight or aim (`ShotReport` and `AttackTargetFinder` read blinding smoke only), and no vanilla AI
  avoids it.
- **Combat Extended** keeps the mechanics and changes the kit: it tags the gas mask `GasMask`, adds its own gas mask and an improvised
  one (TER 0.5, not immune), gives power and recon helmets TER 0.5 and immunity, replaces the toxbomb launcher with 40 mm tox grenades
  and its own 81 mm tox shell, and makes non-player pawns put on a mask from their inventory (`CompGasMask`).
- **The owner's mods emit it widely:** the IRBM tactical tox warhead (radius 36), the Harrier's Mk84 tox bomb (7.9), Apex Mechanoids'
  Haze mist, Dead Man's Switch gas vents and ruin traps, VFE Deserters' remote traps, Vanilla Recycling's packs and trash, Alpha Genes'
  gas-producer gene, VPE's toxpuff mushroom. Protection comes from DMS hazard and bionic suits (TER 1 and 0.8), Biological Warfare's
  respirator (0.8, immune), vacsuits and astrorigs (0.8), Rimatomics' rad suit (0.4), Medicines+'s antitox (0.6) and genes.
- **Nothing to reuse for the new injuries.** A search of the 331 active mods' condition, damage and recipe Defs, by name, label and
  description, found no eye injury, immune-weakening or blistering condition from a gas. The game's `ChemicalBurn` was the one useful hit.
- **The framework already reads it.** `GasKind.Toxic` maps to the grid under Biotech (`VanillaGasCarrier`).
  `PhobosGases.Breathed(pawn, GasKind.Toxic)` is density × `MaskRule.Filtered`: 1 − TER, or **0 under a `GasMask`-tagged item**. So a
  vanilla gas mask breathes 0.2 without Combat Extended and 0 with it, because CE adds the tag. It leaves out ToxicResistance and does
  not read `immuneToToxGasExposure` (Framework 0.2.0, read 5 Nov 2026).
- **Burns and Smoke (0.4.0) reads only smoke and carbon monoxide.** It owns four conditions a caustic gas could reuse: the airway burn
  (`PhobosBurns_AirwayBurn`, rises to a peak at 8 hours, holds to 24, gone by 48, can close the throat), "smoke-damaged lungs"
  (`PhobosBurns_LungInjury`, tendable, pneumonia from its severe stage), pneumonia and carbon monoxide. Its burn load already counts
  the game's `ChemicalBurn` at 1/12 of a part per point.

## The two models

The game's gas is already a blend of an immediate irritant and a slow systemic poison. The setting chooses the **delayed** tracks;
the game's own immediate effect and its buildup stay in both, by vanilla precedence.

| Track | Slow-acting (default) | Fast-acting (setting: closer to vanilla) |
| --- | --- | --- |
| How it feels | The gas stings, then seems to pass. The real harm arrives hours later | Hurts at once, and you know who was hit |
| Eyes | From 4 hours, worst at 12 | At once, from density 0.2 |
| Throat | A roll for an airway burn from 4 hours, by dose | A roll for an airway burn at once, by density |
| Lungs | Rise from 4 hours, worst on days 2 to 3 | Rise after 4 to 36 hours, sooner with a bigger dose |
| Skin | Chemical burns from the onset hour, bare skin most, covered skin a quarter | Chemical burns on bare skin only, in the top density band only |
| Immunity | Weakened from day 3 after a heavy dose, for about two weeks | No effect |
| Aftermath | Irritable airways after severe lung injury | The same |

## Decisions

- **Two models behind one setting** (owner's choice, 5 Nov 2026): slow-acting by default, fast-acting for players who want something
  closer to the game's own feel. The choice is a player preference, so it is a mod setting; the clocks and rates are rules in the Def.
- **The effects live in Burns and Smoke** (owner's choice, 5 Nov 2026): it already owns the airway burn, the lung injury and pneumonia,
  and its giver already runs every 300 ticks for pawns in smoke.
- **A game design, with no real-agent data** (owner agreed, 5 Nov 2026): every figure below is authored for play.
- **Existing effects first** (owner's rule, argued per condition):
  - `ToxicBuildup` and `ToxGasExposure` stay as they are, in both models.
  - The **airway burn** is reused for the throat: same shape, and a tracheotomy relieves it.
  - The **lung injury** is reused, its label widened from "smoke-damaged lungs" to "chemical lung injury" (presentation only; the
    defName stays). Pneumonia follows from it as now.
  - **Skin** uses the game's `ChemicalBurn`, so a gassed pawn's burned fraction and burn shock follow with no new code there.
  - **Own conditions only where nothing exists** (the search above): the dose, an eye injury, weakened immunity and irritable airways.
- **Three exposures, not one dose** (agent proposal; a single dose was drafted and fails). Under a `GasMask` tag the framework's breathed
  figure is 0, so one shared dose would make a masked pawn immune to skin burns too, and a mask should not protect skin. So
  `PhobosBurns_CausticExposure` keeps three figures:

| Exposure | Grows with | What stops it |
| --- | --- | --- |
| Eyes | Cell density | Any worn item or gene with `immuneToToxGasExposure`, as the game does for its own irritation |
| Lungs and throat | `PhobosGases.Breathed(pawn, GasKind.Toxic)` | TER and gas masks, through the framework |
| Skin | Cell density × the skin's cover: bare 1, covered 0.25 | A sealed suit only: any worn item with TER 0.999 or more |

- **Why not the game's buildup formula for the local tracks** (agent proposal): ToxicResistance is systemic, and a detoxifier kidney should
  not protect eyes or lungs. Buildup keeps the game's formula; the framework's figure drives the lungs.
- **The sealed test reads worn items, not `MaskRule.IsSealed`** (agent proposal): `IsSealed` is about headgear with its own air, and
  under Combat Extended a power helmet passes it. A helmet does not seal the skin.
- **Irritable airways over lung rot** (agent proposal): the game's `LungRot` is a progressive disease, too lethal for the aftermath of an
  injury someone survived, and tied to rot stink in the game's text. Recorded as the alternative for the owner.
- **Not touched:** the gas grid, spread, fade, sight lines, AI avoidance, plants and corpses.

## Proposed conditions and rules (nothing built; every figure authored)

| defName | Where | Stages | How it moves |
| --- | --- | --- | --- |
| `PhobosBurns_CausticExposure` ("caustic gas dose") | whole body | trace 0.05, light 0.2, heavy 0.5, massive 0.8, by its largest exposure; no capacity effects of its own | each exposure gains 0.6 per game hour at full density; fades at 0.5 a day once the delayed tracks have fired |
| `PhobosBurns_CausticEye` ("caustic eye injury") | each eye | irritated 0.1 (part efficiency −0.2), inflamed 0.3 (−0.5, pain), burned 0.6 (−0.9, pain), and at 0.9 a roll for lasting scarring | heals 0.15 a day untended, 0.4 tended (tending stands in for rinsing) |
| `PhobosBurns_AirwayBurn` (reused) | neck | as now | rolled per the model |
| `PhobosBurns_LungInjury` (reused, relabelled) | whole body | as now | raised per the model, scaled by the lung exposure |
| `ChemicalBurn` (the game's) | coverage-weighted outside parts | the game's | 1 to 3 points per game hour at a heavy skin exposure |
| `PhobosBurns_CausticWeakness` ("weakened immunity") | whole body | mild 0.3 (immunity gain −0.3), severe 0.6 (−0.6) | slow-acting only, from a heavy dose, from day 3, fading over about 14 days |
| `PhobosBurns_IrritableAirways` ("irritable airways") | whole body | mild (Breathing −0.1), moderate (−0.2) | after severe lung injury: fades over 60 days; after critical, a chance of staying |

**What the dose rate means in play, at full density and bare-faced (inferred):** one game hour is 2,500 ticks, about 42 seconds at speed 1.

| Time in the gas | Dose | Toxic buildup, by the game's rate |
| --- | --- | --- |
| 20 game minutes (about 14 s) | 0.2, light | about 0.38 |
| 50 game minutes (about 35 s) | 0.5, heavy | about 0.84, extreme |
| 80 game minutes (about 56 s) | 0.8, massive | about 0.98, close to lethal |

So a bare-faced pawn gets a massive dose only if they nearly die of buildup first. **The heavy skin track mostly falls on the masked:** a
mask cuts buildup to a fifth and lungs to a fifth or nothing, while the skin keeps dosing. That is a good story, but it needs watching in play.

Rules-Def fields: the dose rate and bands, each model's onset and scaling, the eye peak hour, the lung onset and peak, the skin burn rate
and the covered fraction, the immunity dose threshold and the airways' permanence chance. Settings: the model, a toggle per track, the display
mode. Plain mode names the hazard and the care without figures; Operator shows the dose and the hours to onset.

**Performance:** no new tick. `HediffGiver_PhobosBurns.Interesting` gains "in tox gas or carrying the dose", the giver reads density once per
pass, and the delayed tracks act from the dose condition's rare comp ticks.

**Framework follow-up when built:** `MaskFactor` or a new helper exposes `immuneToToxGasExposure` for the eye track, and Burns and Smoke's
minimum Framework version rises with it.

## Test protocol (owner runs it, once built)

Times are game time, and the gas thins as it spreads, so treat them as rough.

1. Load: the Burns and Smoke load line names the model; no error naming the mod.
2. Throw a tox grenade at a bare-faced colonist for about 20 game minutes, then clear the gas: the game's exposure effect goes, a light dose
   stays and its tooltip gives the hours to onset. Slow-acting: nothing else for hours. Fast-acting: the eyes hurt at once.
3. The same colonist over the next three days at speed 3. Slow-acting: eye injury by hour 12, chemical burns, lung injury rising into day 2.
   Fast-acting: lung injury by hour 36, no immunity track.
4. A colonist in a gas mask for about 50 game minutes: eyes and throat spared, lungs none with Combat Extended or a fifth without, **skin
   burns as if unmasked**, weakened immunity from day 3. In a DMS hazard suit: nothing.
5. Tend the eye and lungs: faster healing. After severe lung injury, irritable airways appear and fade.
6. Save, reload, press "Remove every burn condition", switch the mod off: the new conditions go; the game's burns and buildup stay.

## Results from play

None yet. Nothing is built.

## Open points and upstream

- **For the owner:** should lasting eye scarring be our own stage or the game's blindness?
- **For the owner:** plants and corpses. The game's fallout kills plants and its gas does not. Under the gas rule this is a call-out, not a default.
- **Stripping contaminated clothing** (agent proposal, phase 2): under the slow-acting model, clothes keep dosing the skin for hours after the
  gas clears unless removed. A cheap, strong hook, held until the core is tested.
- **Lung rot** as the aftermath instead of irritable airways: recorded as the alternative.
- **Oxygen:** [D'thorga Medical Equipment](../../mods/PhobosMedical/README.md) provides bedside oxygen and does not repair lungs. Whether
  chemical lung injury should count as a condition oxygen helps is a question for when this is built.
- **Next gases under the rule** ([framework record](phobos-framework.md)): Biological Warfare's clouds, chemfuel vapour and Rimatomics, each argued
  in its own record.
- **Order of work** (owner, 5 Nov 2026): the dosing and burns testing comes first.
