# D'thorga Medical changelog

Maintained from 7 November 2026, when five mods were merged into this one; their histories follow the new entries, each under its
former name. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet tested in game unless an
entry says the owner tested it.

## [Unreleased]

## [0.7.0] - 2026-11-07 - Draft

The owner's answers of 7 November 2026 to every open question (docs/development/outstanding-work.md). Not yet tested in game.

### Balance

- Emergency dosing: doctors give pain relief from 40% pain, where the game's severe label starts, instead of 60%. A patient in bed between 40% and 60% no longer goes without. Owner's choice.
- Emergency dosing: a disease counts as winning the race when immunity would arrive within two days of the lethal mark, instead of one, so doctors give a disease-fighting drug sooner. A patient had been lost by under one point of immunity. Owner's choice.

### Added

- Triage: each patient's triage tag shows as a small icon by their name on the map, coloured as in field triage: red immediate, yellow urgent, green delayed, white minor, black expectant. With Useful Marks active it is one of Useful Marks' marks, through its own way for other mods to add marks; without it, the icon sits just right of the name. The icon is a stand-in drawn in code. Owner's choice.
- Triage: the choice of how far the doctors may go is also a small button on the health tab, beside the game's medicine setting, with the same menu as the button on the person. Owner's choice.
- Triage, with More Injuries: its mechanite therapy for brain damage is found for the five conditions it treats. It uses glitterworld medicine, so a colonist set to any repair may have it queued.

- Burns: fires heat the cells around them in large rooms too, on top of the room's own temperature, with its own setting: nothing extra in a room of 30 cells or fewer, which the game already heats through, then the share the game's even spread misses (a quarter in a 40-cell room, nine tenths in a 300-cell hangar). Figures authored. Owner's choice.
- Burns: people and animals not drafted sprint out of air near a fire that is hot enough to burn them, to the nearest cooler spot within 12 cells, reading moving away from the heat. Firefighters beating a fire stay at it, as does anyone ordered there (agent choice). Uses the game's own burn threshold, 150 degrees over the top of a person's comfortable range, so clothing counts. Its own setting. Owner's choice.
- Burns: an arm or leg whose open burns reach 60% of its health grows a constricting eschar that tightens over about half a day, with its own setting. An escharotomy on that limb releases it; its incisions heal in about four days. Left tight, the limb is starved: with More Injuries it can turn gangrenous through More Injuries' own handlers, otherwise it takes ischaemic damage until it is lost (about three days for a human leg). The eschar goes once the limb's burns heal below the mark. The 60% is the owner's figure; the rest is authored. Burns phase 2, owner's go.
- Burns, with More Injuries: a bill to give fluids for burn shock with one of its saline bags, offered only during burn shock. It counts as tending the shock at the best quality while it lasts. Burns phase 2, owner's go.
- Equipment: a ventilator, a bedside machine (200 W) that breathes for one patient whose breathing has failed, holding it up to a safe level (0.6, authored) but only through a breathing tube. The first piece of the second set, owner's order.
- Equipment: an intubation kit. A doctor with Medicine 4 or better uses it by themselves, as a quick job, on a patient whose airway is swelling shut or who lies at a working ventilator waiting for a tube (owner's choice: a doctor job). The tube takes the place of a swollen, burned airway, which goes, comes out after two days, and stops the patient talking meanwhile. A doctor without the skill is told why when you right-click.
- Burns: an acid burn leaves acid on the skin for about three hours, deepening the acid burns by 2 an hour at first (authored), unless it is rinsed off. Matched by a Def extension on the game's acid burn, so other mods' chemical burns can join. Its own setting.
- Equipment: a safety shower and eyewash, the second piece of the second set. Anyone who can walk runs to the nearest one by themselves and rinses for a quarter of an hour; doctors draw water there and rinse patients who can't walk. Rinsing stops the acid and the cool water eases burn pain for a few hours. Each rinse uses the ANSI Z358.1 figure, about 1,100 litres, or 23 for eyes alone, from Dubs Bad Hygiene's plumbing when it is active; without it water is assumed.
- Equipment: a carbon monoxide and smoke alarm, the third piece of the second set: a 5 W ceiling fixture that warns once, naming the room, when smoke or carbon monoxide passes its level, wakes anyone asleep in the room (owner's choice), and stays on until the gas falls to half its level. Separate from the hydrogen detector planned with the vents (owner's choice). Levels authored.
- Equipment, with Vanilla Gravship Expanded: an oxygen cylinder trolley, the fourth piece of the second set. Unlike the bedside stand it needs no bed: it serves one patient lying in any cell beside it, in a bed or on the floor, holds 40 canisters and is quick to pick up and set down, so it goes to a casualty (agent choice). For modders, any oxygen machine can do this with one comp setting.
- Equipment: a nebuliser, the fifth piece of the second set: a 60 W bedside machine that gives a bronchodilator mist (breathing up 0.15 to at most normal, authored) to one patient whose airways tightened after smoke, our acute smoke inhalation or Combat Extended's. It burns bronchodilator vials, a new drug-lab item, 4 from 1 neutroamine, about one per four hours of treatment. For modders, any bedside machine can treat conditions by a tag in XML.
- Equipment: an anaesthesia machine, the sixth piece: a 150 W bedside facility that adds 8% to surgery success in its bed through the game's own surgery factor (authored; the vitals monitor adds 5%).
- Equipment, with More Injuries: a needle decompression kit, the seventh piece. A doctor with Medicine 5 or better uses it by themselves on a collapsed lung under pressure, taking 0.5 off it (about a day), never clearing it, so the operation is still needed.
- Equipment: a steriliser, the last piece: an unpowered autoclave whose cleanliness counts toward its room's, lowering infection and helping surgery the game's way (cleanliness 4, about seven sterile tiles, authored).
- Burns: tox gas is a caustic gas, the owner's design of 5 November 2026 (docs/development/caustic-gas.md). On top of the game's own stinging and toxic buildup, three doses (eyes, lungs, skin) grow in the gas and drive delayed harm: eye injury, a roll for an airway burn, chemical lung injury, the game's chemical burns on the skin, weakened immunity after a heavy dose, and irritable airways after severe lung injury (lasting, by chance, after critical). Slow-acting by default (harm from about 4 hours, over two or three days); a setting makes it fast-acting. Gas masks protect eyes and lungs, not skin; only a sealed suit protects everything; a safety shower washes the skin's dose off. A toggle per track. Every figure authored.
- Dosing: fussing. An awake patient in extreme pain (from 80%, the game's label) is tended 10% worse, falling to 25% worse at full pain (authored), through one Harmony postfix on the game's tend quality. Pain relief keeps patients below it; anyone asleep or anaesthetised doesn't fuss. Its own setting, on by default. Owner's go.
- Equipment: a fluids kit for burn shock, made at a drug lab. Doctors use it by themselves on burn shock with no fluids yet or poorer ones, as care of quality 0.9 (authored), through the same fluids care as More Injuries' saline bill, which counts as 1. Owner's choice: More Injuries' saline when active, ours otherwise; both work.
- Equipment: placeholder art for every new piece of the second set and the fluids kit, drawn in code, with records.
- Equipment: the one-time letter about Vanilla Gravship Expanded names the new pieces, and calls the mod D'thorga Medical rather than its name before the merge.
- Equipment, with More Injuries: supplied oxygen helps its hypoxia heal, on top of its own healing: organ hypoxia by 4 a day, cerebral hypoxia by 2 (authored; the benefit to the brain is less certain), twice that in the hyperbaric chamber. Permanent damage never heals. Owner's choice.

### Changed

- Burns: smoke-damaged lungs are now called chemical lung injury, and acid on the skin chemical on the skin, since caustic gas causes both. Names only; nothing in play changes.
- Burns, with More Injuries: its own fire inhalation setting decides again, rather than this mod removing that feature (owner's choice). With it on, this mod's smoke damage to the lungs stands down while a person is on fire, so the lungs are never damaged twice; the settings window shows which way it is set.
- The tracheotomy's surgery class now checks the operated part, so the escharotomy can tell an eschar on one limb from one on another. No change for the tracheotomy.
- Triage keeps queuing fracture repairs under save life or a limb, rather than leaving fractures to More Injuries' splints. No change in play: the owner chose to keep it.
- Emergency dosing: a masochist, or anyone else for whom pain is no hardship (the game's own pain mood is cancelled for them, as by a belief in idealised pain), no longer fetches their own painkillers. Doctors still treat them in bed. Read from the game's pain thought, not from the trait's name, so a modded trait that does the same counts too.

### Dependencies

- D'thorga Framework 0.8.0 or later, for the shared menu of a person's care choices, healing on oxygen and reading More Injuries' setting. Useful Marks is optional; with it, triage tags join its marks.


### Compatibility and limits

- People move away from heat through a patch to the game's constant behaviour for people and animals, next to its own fleeing from explosions and danger. A mod that rebuilt those leaves this out without an error. Mechanoids never move away, as heat does not burn them.
- Fire heat in rooms changes what heaters and coolers near a fire read for their own cell, as fire heat in the open already did. Not yet tested in game.

## [0.6.0] - 2026-11-07 - Draft

Owner's decision (7 November 2026): fewer, broader mods before anything is published. D'thorga Burns and Smoke, Emergency Dosing, Triage, Medical Equipment and Medical Save Bridge become one mod, D'thorga Medical; the owner chose to include the save bridge, behind a question on load. The version continues above the highest of the five (Medical Equipment and Burns and Smoke, 0.5.0) with a middle-number step, because the question is new. Not yet tested in game.

### Added

- Loading a colony saved before More Injuries, or before another mod that adds body parts, asks first: Convert now, Not now, or Never for this save. It asks only when converting would change something. Escape goes back without loading. Saves marked never can be asked about again from the mod settings.
- The settings of the five old mods are read once, on the first start, so every choice carries over.
- An error in the log, at load, names any of the five old mods still switched on beside this one.

### Changed

- One mod, D'thorga Medical (package ID PhobosDthorga.Medical), with one settings window in sections, one load line per part and one button that removes every condition the mod adds.
- One display mode for every part, carried over from Medical Equipment's setting; the other four parts' display mode settings are gone.
- The save bridge's log lines start with D'thorga Medical: save bridge, and say HELD when the player chose not to convert.
- The one-time letter recommending Vanilla Gravship Expanded records that it was shown through the merged settings.
- The burns dev action that removed burn conditions now removes every condition this mod adds.

### Fixed

- No copy of D'thorga Framework's assembly ships in the Dubs Bad Hygiene folder any more. Medical Equipment's build had placed one there since 0.5.0; the shared build rules now stop every such copy.

### Dependencies

- Harmony, D'thorga Framework and Vanilla Expanded Framework, as Medical Equipment needed. The save bridge no longer needs a mod of its own.

### Save compatibility

- Every class, condition, building, item, job and saved key keeps its name, so a save made with the five mods loads with this one. Switch the five off when switching this on; the About file lists them as incompatible.
- Safe to add to a running colony. Before removing it, use the remove button in its settings, deconstruct the machines and burn unit beds, then save.

### Compatibility and limits

- Not yet tested in game as one mod. The parts' own in-game reports, from before the merge, are in their histories below.
- A load started by another mod without the load menu is not asked about, and converts as the save bridge always did.
- The Workshop preview keeps the owner's picked background for Medical Equipment, with the new name added by script.

## D'thorga Medical Equipment history

### [0.5.0] - 2026-11-07 - Draft

The owner's choice (7 November 2026): with Dubs Bad Hygiene, the electrolyser needs real water. Not yet tested in game.

#### Added

- With Dubs Bad Hygiene active, the oxygen electrolyser draws its water from the plumbing (a pipe under it or against its side),
  about half a litre an in-game hour. Without water it stops making oxygen, drops to its idle power draw and says why: no pipe, or
  the water towers are empty. Without Dubs Bad Hygiene, or in its lite mode, water stays assumed as before.

#### Balance

- Half a litre an hour is our arithmetic from the reaction: one canister of oxygen an hour is about 0.43 kg of oxygen, which takes
  about 0.48 kg of water to split. Water quality is ignored (a real electrolyser needs pure water; a simplification).

#### Dependencies

- D'thorga Framework 0.5.0 or later, for its shared plumbing reader. The water part loads only while Dubs Bad Hygiene is active.

### [0.4.3] - 2026-11-07 - Draft

No change in play. Not yet tested in game.

#### Changed

- The breathing devices and the chamber transfer use D'thorga Framework's shared check of who counts as a patient, instead of their
  own.

#### Dependencies

- D'thorga Framework 0.4.2 or later.

### [0.4.2] - 2026-11-07 - Draft

A fix from the owner's game. Not yet tested in game.

#### Fixed

- Right-clicking a vehicle with a doctor selected offered a line saying the doctor couldn't use a breathing device on it because
  it was "breathing well enough" (seen in the owner's game on 0.4.1, on a tank, 7 November 2026). Vehicles are a kind of pawn to
  the game. The line is now shown only for a real patient, lying down, and only when the reason is worth knowing: the airway is
  shut, no device is within reach, or the patient is already on oxygen. A healthy colonist gets no line either.

### [0.4.1] - 2026-11-07 - Draft

Not yet tested in game.

#### Fixed

- The hyperbaric chamber tells other mods why its patient can't be operated on, through D'thorga Framework's new surgery check, so
  D'thorga Triage can hold the operation and say why instead of reporting it queued (once Triage reads it).

#### Dependencies

- D'thorga Framework 0.4.0 or later.

### [0.4.0] - 2026-11-07 - Draft

The owner's answers of 7 November 2026 for the pieces added in 0.3.0. Harmony comes into this mod, for three small postfixes. Not
yet tested in game.

#### Added

- Doctors carry a downed patient who needs oxygen to a free, ready hyperbaric chamber instead of the nearest bed. With none free,
  the game's own rescue goes on as before. Patients already in a bed stay where they are.
- A one-time letter, the first time a colony starts or loads without Vanilla Gravship Expanded, says which five pieces need it and
  what works without it. It is shown once per install. The settings window says the same.

#### Changed

- No operation is done on a patient lying in a hyperbaric chamber; the bill waits, and the chamber says which patient to move.
- A liquid oxygen store without power takes in no more oxygen. It still gives out what it holds, boils off as before, and says so.
- The Workshop preview's lettering has a thin dark edge, so it reads on light backgrounds too.

#### Dependencies

- Harmony, for the three postfixes: the pipe network's storage check is not overridable, a medical bill's readiness has no
  extension point, and the letter needs the game's start-up step without saving anything in the game.

#### Save compatibility

- Safe on a save from 0.3.0: nothing new is saved in the game. The letter's "shown" flag is in the mod settings.

#### Compatibility and limits

- With D'thorga Triage, a patient lying in a chamber may still have an operation queued, and Triage still says it is queued while
  no doctor starts it; the chamber's inspect line says to move them. Fixed in 0.4.1 with the Framework's shared check.

### [0.3.0] - 2026-11-07 - Draft

The rest of the oxygen kit and the field kit the owner asked for (7 November 2026): a pressure chamber, two devices doctors use by
themselves, a burn dressing, and an electrolyser and a liquid store to make and keep oxygen. Not yet tested in game.

#### Added

- Hyperbaric chamber: a bed for one patient, 1 by 3, that runs on oxygen canisters or an oxygen pipe. A patient who needs oxygen
  gets it under pressure: breathing eased and carbon monoxide cleared with a half-life of about 22 minutes, against about 75 on a mask.
- Field oxygen kit: a small cylinder, regulator and mask made from an oxygen canister. Doctors fit it by themselves to a patient
  lying down who needs oxygen and isn't on it; it lasts about an hour and is used up.
- Manual resuscitator: a bag and mask that doctors pump by themselves for a patient lying down whose breathing is under half. It
  holds breathing up while they pump, for up to an hour at a time, and is kept. It can't help an airway that is shut, and says so.
- Silver burn dressing: a medicine made at a drug lab from cloth, silver and neutroamine, between herbal and industrial medicine.
  Doctors use it for any tend; a better tend lowers infection through the game's own rule.
- Oxygen electrolyser: splits water into oxygen and hydrogen on 3000 W and puts one canister's worth of oxygen an hour into an
  oxygen pipe. Water is assumed. Meant for vacuum and thin air, where the oxygen harvester can't run.
- Liquid oxygen store: holds 2000 units of oxygen on an oxygen pipe, four times a gas tank of the same size. Without power for its
  cooler about 2% boils off each day. A badly damaged store bursts into a fireball. It can be drained into canisters like a tank.
- Doctors right-clicking a patient are told why no device is used: the airway is shut, none is within reach, the patient is already
  on oxygen, or breathes well enough.

#### Changed

- An oxygen machine that is itself a bed serves whoever lies in it, and a machine can give its own, stronger treatment condition.
- The oxygen conditions now check the patient's breathing by themselves, so the kit and the hand pump never lower breathing that is
  already above normal, as the machines never did.
- The oxygen tooltip works out how many times faster carbon monoxide clears from each treatment's own half-life, instead of a fixed
  four times.
- The hyperbaric chamber is 1 by 3, not the 2 by 3 first proposed: a bed holds as many patients as it is wide.

#### Balance

- All new costs, power draws, work, the 6 canisters an hour in the chamber, the boil-off and the breathing figures are authored. The
  electrolyser's 3000 W follows real electrolysis (about 3 to 3.6 kW for one patient's oxygen, our arithmetic from a published
  range), which is far hungrier than Vanilla Gravship Expanded's harvester by that mod's own balance. The half-lives are sourced.

#### Dependencies

- The chamber, the field kit, the electrolyser and the store need Vanilla Gravship Expanded, like the bedside stand, and load only
  with it. They join its oxygen network instead of a second network of our own.
- References Vanilla Expanded Framework's PipeSystem assembly from the installed mod, for the store's boil-off; nothing of it is
  copied.

#### Save compatibility

- Safe to add to a running colony. The new treatment conditions live with the core Defs, so a save still loads cleanly if Vanilla
  Gravship Expanded is taken out while a patient has one; each lapses within the hour. Its buildings and kits would be dropped.

#### Compatibility and limits

- Art: the store and the three items are the owner's picks (references only, no private model). The electrolyser and the chamber
  are scripted placeholders until generated art is picked, and the store's frost is not drawn yet.
- A chamber does not stop surgery on its patient, and patients are not sent to it in preference to other medical beds.

### [0.2.3] - 2026-11-07 - Draft

The owner's art picks, and the README brought up to date. No change in play. Not yet tested in game.

#### Changed

- The oxygen concentrator has generated art in place of its hand-drawn placeholder, redrawn from that placeholder's layout (the owner's
  pick, 7 November 2026). No private model was used for it: only the placeholder and the game's vitals monitor as references.
- The bedside oxygen stand has new generated art, a slim pole with a flowmeter and a coil of tubing (the owner's pick, 7 November
  2026). No private model either: the game's vitals monitor and oxygen pump were its references.
- The mod has a Workshop preview picture: a generated hospital ward, with the name added by script.
- Both textures are now 128 px (the concentrator was 64), for detail when zoomed in. Each passed the near-copy check; the record beside each
  texture says how it was made.

#### Fixed

- The uninstall button's description now names the burn unit beds as well as the machines. README brought up to date: the burn
  unit bed in the opening line, Framework 0.3.0 or later, the owner's bed report, the burn-care extension for modders.

### [0.2.2] - 2026-11-06 - Draft

No change in play. Not yet tested in game.

#### Changed

- The lapsing treatment condition and the "who was treated, who was left alone" pass now live in D'thorga Framework's health
  layer, shared with D'thorga Triage. The time a treatment lingers after the machine's last pass (600 ticks, unchanged) moved
  from this mod's rules Def onto the condition's comp in the hediff Defs.

#### Dependencies

- D'thorga Framework 0.3.0 or later.

#### Save compatibility

- The treatment comp keeps its saved key, and the game saves no class name for a condition's comps, so saves made on 0.2.1 load
  unchanged (read from the code, not yet tested).

### [0.2.1] - 2026-11-06 - Draft

Fixes after the owner's first test of the burn unit bed: what the machines say, and oxygen that cut above-normal breathing. Not yet tested in game.

#### Fixed

- An oxygen machine with a patient who breathes well enough now names them and says why it gives them nothing, instead of waiting in
  silence. Oxygen still goes only to a patient with weak breathing, or with smoke, fumes or carbon monoxide in the lungs.
- A burn unit bed now names an occupant with no open burns, and says it gives them no air-cell care.
- Oxygen no longer lowers a patient whose breathing is above normal, such as a pawn with bionic lungs treated for carbon monoxide. Its
  ceiling at normal used to cut them down to it; now oxygen raises only breathing below normal, and leaves the rest alone.

#### Save compatibility

- An oxygen condition saved by an earlier version is replaced with the new kind on the machine's next pass. Nothing else changes.

#### Compatibility and limits

- Tested in game by the owner on 0.2.0: the burn unit bed looks right and gives a burned patient air-cell care. The owner's report that
  oxygen did not start was a patient breathing at full strength, so the machine was right to hold back; it now says so.

### [0.2.0] - 2026-11-05 - Draft

A burn unit bed, the owner's go after their Burns and Smoke results. Not yet tested in game.

#### Added

- Burn unit bed: a hospital bed with an air-cell mattress and a blower. While it has power, a patient with an open burn gets the on air
  cells condition: wounds heal 25% faster and pain falls by 15%. It works by itself and lapses soon after the patient leaves or the power
  stops. Its better tend quality lowers infection through the game's own rule, powered or not.
- Burns from the game's four kinds and Combat Extended's incendiary burn count. Other mods can mark their own in XML.
- The art is the owner's pick. It is AI-generated, by a model trained in private on 60 of the game's sprites; the record beside it has the
  details. North and east are the south view turned, as it is drawn from directly above.

#### Changed

- The uninstall button now removes every treatment condition, and its wording says so.

#### Fixed

- The load line said 0.1.0 in version 0.1.1. It now matches.

#### Balance

- All the bed's figures are authored: healing, pain, tend quality, comfort, power and cost. None has a clinical source.

#### Compatibility and limits

- The healing speeds all of the patient's wounds, not only the burns.

#### Save compatibility

- Safe to add to a running colony. Before removing the mod, deconstruct the beds as well as the machines.

### [0.1.1] - 2026-11-05 - Draft

New art for the bedside oxygen stand, picked by the owner. The new art is not yet tested in game. The owner reports that a patient's breathing improves on the oxygen concentrator (tested on 0.1.0); the bedside stand is not yet tested in game.

#### Changed

- The bedside oxygen stand has generated art in place of its placeholder. It is AI-generated, by a model trained in private on 60 of the
  game's sprites, and the owner chose to ship it knowing that Ludeon's EULA does not settle this. The record beside the texture has the
  details. The sprite passed the near-copy check. The oxygen concentrator keeps its hand-drawn placeholder.

#### Save compatibility

- Art only. Safe in a running colony.

### [0.1.0] - 2026-11-05 - Draft

The first pieces of the owner's medical equipment idea: oxygen at the bedside. Not yet tested in game. Labels, descriptions and the
placeholder art are agent-authored and held for the owner's review.

#### Added

- Oxygen concentrator: a one-tile bedside facility that needs only power, for up to two patients.
- Bedside oxygen stand, only while Vanilla Gravship Expanded is active: burns its oxygen canisters, and fills from its oxygen pipes
  through Vanilla Expanded Framework's own linking. One canister per patient per in-game hour.
- Both link to any bed that accepts a vitals monitor, found by that and not by name, and are built against the bed in the same way.
- Treatment is automatic (owner's choice): a patient in a linked bed with a condition oxygen helps, or with breathing under 80%, gets
  the on oxygen condition with no job. The worst breathing is served first. It lapses soon after the supply stops.
- On oxygen raises breathing by 20 points up to normal. With D'thorga Burns and Smoke, carbon monoxide's half-life falls to 75 minutes.
- Each machine's inspect text says what it is doing or waiting for. Machines have the game's power switch.
- Settings: the display mode and an uninstall button. A dev action logs a pawn's oxygen need.

#### Balance

- Power, cost, work, the patient limit, the canister rate and the breathing figures are authored. The 75 minutes is sourced.

#### Dependencies

- D'thorga Framework 0.2.0 or later and Vanilla Expanded Framework (required; the latter is the owner's choice, for the pipes
  this mod will need, though this version uses its linking only through Vanilla Gravship Expanded). Burns and Smoke 0.4.0, Vanilla
  Gravship Expanded, Combat Extended and More Injuries are optional and used when present.

#### Save compatibility

- Safe to add mid-game. To remove: deconstruct the machines, press the settings button, save, then switch the mod off.

#### Compatibility and limits

- Oxygen does not repair lungs, does not treat More Injuries' hypoxia directly beyond easing breathing, and cannot help a closed airway.
- A patient on oxygen in a room full of carbon monoxide still breathes it in, a simplification. Move them.
- The art is a scripted placeholder with one view. Only beds are served: no field use yet.
- Game-free rules pass their unit tests and it builds for 1.7 and 1.6. Nothing is tested in game, including the pipe link.

## D'thorga Burns and Smoke history

### [Unreleased at the merge] - Draft

Not yet in a version of its own; carried into D'thorga Medical 0.6.0.

#### Added

- A Workshop preview picture: a generated background the owner picked (7 November 2026), with the name added by script. No change in play. A stand-in: the owner is not yet satisfied with either background.

#### Compatibility and limits

- Correction to 0.5.0: the fire heat's map component is saved by name, so removing the mod prints one harmless line per map on
  the next load. The README now says so, names the Framework versions it needs, and how the other medical mods work beside it.
- The owner's report in game (on 0.4.2, before 0.5.0), 5 November 2026: burn numbers, how conditions progress, and the More Injuries and Organ Failure tie-ins work.
  Faster carbon monoxide clearing on oxygen is the owner's inference, not an observation. The tracheotomy is unproven.

### [0.5.0] - 2026-11-06 - Draft

The owner's observation of 6 November 2026: fires in the open were never hot, the air beside a blaze read as the weather. Not yet
tested in game.

#### Added

- Fire heat in the open. The game gives a fire's heat to its room and throws it away outdoors; now each fire in the open heats the
  cells around it, hottest at the flame and fading within five cells, and the game's own heatstroke, hot-air burns, hot thoughts, plant
  growth, food spoiling and the Alt cell inspector respond to it. A full-size fire adds about 600 degrees at its own cell, 150 one
  cell away, 67 at two, 24 at four; smaller fires less; several fires add up to a cap of 900. The peak is authored and approximate,
  from the 600 to 1000 degree range of wood flames; the falloff, radius and cap are authored. Setting: Fires heat the air around
  them in the open, on by default.
- Dev action: Burns: log fire heat at the mouse cell.

#### Dependencies

- Harmony, loaded before this mod. It carries the one patch, a postfix on the game's cell-temperature lookup; every other mod's
  answer comes first and ours is added to it.

#### Compatibility and limits

- Rooms keep the game's own heating; the zones apply only to cells whose air is the outdoor air. Fires spread exactly as before;
  the zones never start a fire.
- With Atmosphere Overhaul or Celsius active the fire heat stands down by itself, so heat is never counted twice; the load line
  says so. The list of such mods lives in the rules Def for others to add to.
- Vanilla Temperature Expanded, Dubs Central Heating, Dubs Bad Hygiene and Combat Extended were read and leave the outdoor
  answer of that lookup alone; VTE's own proximity heat adds on top where its heaters stand.
- The zones are rebuilt every 150 ticks, the game's own fire cadence, so a cell can stay hot for up to two and a half seconds after
  its fire goes out.
- Combat Extended's loose fire and the game's own fire are the same Thing, so both count.

#### Save compatibility

- Nothing saved. Safe to add to a running colony; removing the mod leaves nothing behind from this feature.

### [0.4.2] - 2026-11-05 - Draft

No change in play. Not yet tested in game.

#### Changed

- Tooltips now read their figures from the Defs: the shock threshold, the score marks and treatment terms in Engineer mode, the carbon
  monoxide half-lives and the lung recovery rates. Tuning those in XML now changes the text too.
- Messages use named placeholders, so translators can see what each one means.
- Moved into the rules Def, at the same values: burn shock's starting and leaving severities, and the airway burn's chance at the
  reference damage and its lightest peak.

#### Save compatibility

- Nothing saved changes.

### [0.4.1] - 2026-11-05 - Draft

The owner's rule of 5 November 2026: text a player reads in game stays in the game world. Wording agreed with the owner. Not yet
tested in game.

#### Changed

- Combat Extended's smoke inhalation now says in its tooltip what follows from it: scarred lungs, and carbon monoxide from fires in
  closed rooms. Each sentence shows only while its effect is switched on. Combat Extended's own description is untouched.
- Our own acute condition is now labelled acute smoke inhalation, so two conditions never share a name when both are switched on.
- The smoke-damaged lungs tooltip opens with what caused them, in Operator and Engineer modes.

#### Fixed

- The carbon monoxide tooltip named the mod's internals as its source. That line now appears only in the log.

#### Save compatibility

- Text only, plus a tooltip comp that saves nothing. Safe to add and remove as before.

### [0.4.0] - 2026-11-05 - Draft

#### Added

- Supplied oxygen clears carbon monoxide faster: a half-life of about 75 minutes against about 320 on room air, from any mod's
  treatment that the framework reports (D'thorga Medical Equipment's first). Carbon monoxide, smoke-damaged lungs, pneumonia and
  smoke inhalation now count as conditions oxygen helps. Not yet tested in game.

#### Dependencies

- D'thorga Framework 0.2.0 or later.

#### Changed

- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

### [0.3.0] - 2026-11-04 - Draft

Two owner decisions of 4 November 2026, after the first results from play. Not yet tested in game.

#### Added

- Burn area by kind. A burn's severity is its depth, as before (pain, healing, losing the part); its area now comes from the
  kind of burn, so fire and an armour-piercing incendiary round come out differently (owner's choice). Fire, chemical and
  acid burns cover a whole part at 12 points of damage; an electrical burn or Combat Extended's incendiary and laser burn is
  deep but covers at most a third of a part, 40 points for the whole. Other mods set both numbers in the same extension.
- Fluids by medicine. Tending the shock lowers its target in proportion to the tend quality the game records, so the medicine
  used matters: with burns tended and bed rest, industrial medicine holds the worst scores at the shock stage, out of the
  death clocks, and glitterworld medicine brings them to compensated; herbal or none leaves them life-threatening (owner's
  choice: playable without a burn ward). Fluids wear off after 12 hours as before.

#### Changed

- The modder extension's single weight is replaced by areaPerDamage and maxArea. No saves hold it.
- Tended burns and bed rest now take 20% and 15% off the untreated target instead of 60% and 30%; fluids take up to 45.5%.
- The Engineer tooltip shows the fluids quality; the Plain tooltip says better medicine helps more.

#### Balance

- Ten seconds on fire (four hits) is about a fifth of the body burned: serious, and over the shock threshold. A brief catch
  on one arm is under 2% and shows nothing. Burns from fire now count about three times what they did in 0.2.1, because area
  is no longer read against the part's full health. All authored.

#### Compatibility and limits

- Game-free rules pass 19 unit tests. The fluids quality is read from the shock condition's own tend comp; untested in game.

### [0.2.1] - 2026-11-04 - Draft

#### Fixed

- Old burn scars counted as burned skin. With the 0.2.0 skin shares a scar on the torso or a leg could hold a healed pawn in
  burn shock for life. Scars no longer count, and a pawn with only scars or healed stumps is no longer recomputed.
- Corrected the 0.2.0 entry, the README and the design record, which said burn amputations do not bleed. They do: the game
  bleeds any fresh lost part until it is tended or 1.5 days old, whatever destroyed it. No behaviour changes with this.

#### Compatibility and limits

- A stump 1.5 days old counts as tended even if nobody tended it, because the game cannot tell the two apart once it stops
  being fresh. A labelled simplification.
- Game-free rules pass 18 unit tests; the scar check lives in the game-facing service and has no unit test. Not yet tested
  in game. The owner's save holds 0.2.0 results (see the design record); that is not a declared test.

### [0.2.0] - 2026-11-04 - Draft

The owner's first look at 0.1.0 in game (an incendiary raid on 7 September 5512: pawns with limbs, a jaw and a head burned
off, burns on head, neck, torso and shoulders) found every one of them at "burns (minor)" or with no burns condition at all,
and never in burn shock. The mod ran without errors; it was counting almost nothing. Not yet tested in game.

#### Fixed

- Skin shares: the mod subtracted child parts from a game figure that already leaves them out, so the torso, neck, head and
  both shoulders counted as zero skin and only arms, legs and feet ever added up. Shares now come from the including-children
  coverage and sum to 1 (torso 0.405, each leg 0.126, each arm 0.080, head 0.028), as the design record intended.
- Parts burned off counted for nothing, and the burns on them vanished with them, so the worst-burned pawns showed the least.
  A part burned off now counts as its whole skin burned while the stump is fresh.
- The load line threw at startup through the framework (D'thorga Framework 0.1.1 fixes it).
- The burns condition said burn shock starts past a fifth of the body; the rule is 15%. The wording now says about a sixth.

#### Changed

- Burn shock always shows its compensated stage once 15% of the body is burned, even for a young pawn whose Baux score is
  below the no-death mark, so the fluids job is visible; the death clocks are still Baux-driven.

#### Balance

- A tended stump fades out of the burned fraction over 15 days (authored).

#### Dependencies

- D'thorga Framework 0.1.1 or later.

#### Compatibility and limits

- Burn amputations bleed by the game's own rule until tended or 1.5 days old. Dead pawns are not recomputed; a corpse keeps
  the condition it died with.
- Game-free rules pass 18 unit tests. Nothing is tested in game.

### [0.1.0] - 2026-11-04 - Draft

The owner's request after an incendiary raid showed burns and smoke doing almost nothing beside More Injuries' wounds. Not
yet tested in game. Every condition's labels and descriptions are agent-authored and held for the owner's review.

#### Added

- Burned body fraction: every burn on the skin, weighted by the part's share of the skin from the game's body coverage,
  recomputed every 300 ticks. Stages bring pain, cold sensitivity, hunger, rest loss and strain on the heart.
- Burn shock from 15% burned, heading for a target set by the revised Baux score (age plus percent burned plus 17 with an
  inhalation injury), lowered by tended burns and bed rest; tendable, so tending it stands in for fluids. Death clocks at the
  severe and terminal stages.
- Airway burns after burning or burns to the head and neck: swelling rises to a peak at 8 hours, holds to 24 and resolves by
  48; the top stage stops breathing. A tracheotomy surgery removes it and leaves a tracheostomy for five days.
- Smoke read from D'thorga Framework (Combat Extended's own inhalation while its smoke runs, otherwise the game's smoke):
  lasting lung damage that invites pneumonia, and an acute smoke condition when Combat Extended's is not running.
- Carbon monoxide from the framework's room gas, which fires make in closed rooms; it clears with a half-life of 5.3 hours
  under a roof and 3 in the open, and ignores filter masks. Without the framework's gas it is inferred from smoke in an
  enclosed space.
- Settings: a toggle per condition, the display mode, the Combat Extended acute-smoke option, and an uninstall button.
- Dev actions: log a pawn's burn load and shock readout, start an airway burn, remove every condition.

#### Dependencies

- D'thorga Framework 0.1.0 (required). More Injuries, More Injuries: Organ Failure and Combat Extended optional.
- Built twice: against the 1.7.4998 reference on .NET 10, which leads, and against the 1.6 reference; the game loads its own
  version's folder (1.7/Assemblies or 1.6/Assemblies).

#### Save compatibility

- Safe to add mid-game: the conditions are derived from the burns a pawn already has. To remove, press the settings button,
  save, then switch the mod off.

#### Compatibility and limits

- With More Injuries: its fire-inhalation worker is removed from its worker list (owner's choice), carbon monoxide and a
  closing airway feed its hypoxia and brain damage through its own XML handlers, and the tracheotomy waits for Emergency
  Medicine. With Organ Failure, a closed airway runs into its respiratory-failure chain instead of killing outright.
- Humanlike pawns only in this version. Combat Extended's burn from incendiary ammunition counts as a burn.
- Game-free rules pass 14 unit tests. Nothing is tested in game.

## D'thorga Emergency Dosing history

### [Unreleased at the merge] - Draft

Not yet in a version of its own; carried into D'thorga Medical 0.6.0.

#### Added

- A Workshop preview picture: a generated background the owner picked (7 November 2026), with the name added by script. No change in play.

#### Compatibility and limits

- README brought up to date: partly tested in game, Framework 0.3.0 or later, slaves are dosed too, the setting's current name,
  and how it works beside D'thorga Triage.
- The owner reports doctors' dosing working in game on 0.3.0, from one case with penicillin and ketamine. Self-medication and the bleeding hold are not yet tested in game.
- A drug that gives its condition through its own mod's custom ingestion code is not read by the index. Vanilla Races Expanded's antibiotics are one, so doctors do not give them
  on their own.

### [0.3.1] - 2026-11-06 - Draft

No change in play. Not yet tested in game.

#### Changed

- Who counts as the player's patient, the care-level test, the severity-rate sum and the danger reader now come from D'thorga
  Framework's health layer, which D'thorga Triage shares; the patient cache is the framework's. Same answers as before.

#### Dependencies

- D'thorga Framework 0.3.0 or later.

### [0.3.0] - 2026-11-05 - Draft

Two additions the owner approved on 5 November 2026. Not yet tested in game. New text is agent-authored and held for the owner's review.

#### Added

- Doctors give drugs that help fight disease: a patient down or in bed with a disease that can kill them, when it is winning the race against their immunity, gets a medical drug
  that adds at least 10% to immunity gain speed. Drugs are found by their effects, as painkillers are. The race is read from the game's own severity and immunity rates, and a
  patient already winning gets nothing, so stock lasts. Only doctors give these. A setting switches it off.
- A bleeding hold: a patient bleeding at 30% a day or more waits for a painkiller until that is under control, because some painkillers raise bleeding in ways this mod cannot
  see. A sedative in extreme pain and a drug for fighting disease do not wait. It applies to self-medication too.
- A refusal message for the hold, and a dev action listing the drugs for fighting disease.

#### Changed

- The doctor's job now reads giving medication rather than pain relief, in the work giver, the job report and the main setting.

#### Balance

- The 30% hold, the 10% least gain and the one day of margin in the race are authored, all in the rules Def.

#### Compatibility and limits

- Medical drugs only: no teas or other drinks, and nothing that gives a disease, such as Biological Warfare's malaria pills. Drugs that prevent a disease, such as vaccines and
  penoxycyline, are not included: those are for the healthy, and penoxycyline can already be scheduled in a drug policy.
- Real antibiotics kill bacteria and do nothing for a virus. The game's immunity stat is a stand-in, so these drugs help against any disease, which is a gameplay simplification.
- Effects a mod applies through other means, such as stats by severity, are not read.
- Game-free rules pass 23 unit tests. The reading of a patient's disease and rates is game-facing and has no unit test. Not yet tested in game.

### [0.2.0] - 2026-11-05 - Draft

The owner's idea: someone above a pain threshold would seek out a painkiller on their own. Not yet tested in game. Settings text is
agent-authored and held for the owner's review.

#### Added

- Self-medication: an adult colonist who is up and about at 40% pain or more goes and takes the best painkiller their drug policy allows, for
  joy or on a schedule, through the game's own ingest job. Medical drugs come before recreational ones. The doctors' safety rules apply,
  with a higher floor so a dose cannot drop a pawn on their feet (consciousness and breathing stay at 40% or more), and any painkiller still
  working blocks the next.
- Chemical interest and fascination lower the threshold to 30% and 20% and ignore the policy, as they do for joy in the game. Teetotalers and
  children never self-medicate, and sedatives are never taken by choice.
- A setting to switch it off, on by default, and a setting to let guests do the same, off by default, for mods such as Hospitality. The game gives
  guests no drug policy, so they take medical painkillers only.
- A dev action that logs what a selected pawn would take and why.

#### Balance

- 40% pain is the owner's choice; the two trait offsets and the 40% capacity floor are authored.

#### Compatibility and limits

- A pawn in bed or under medical rest, which includes anyone with a tended wound still healing, is left to the doctors, who step in from 60%.
- Addiction and overdose follow from the drug taken, by design. Check drug policies before enabling this on an existing colony.
- Uses a think-tree hook, no Harmony. Nothing is saved. Removing the mod while a pawn is fetching a drug is not tested.
- Game-free rules pass 17 unit tests. The eligibility checks and the item search are game-facing and have no unit test. Not yet tested in game.

### [0.1.3] - 2026-11-05 - Draft

No change in play. Not yet tested in game.

#### Changed

- Refusal messages use a named placeholder for the drug, so translators can see what it means.
- The share of the bleed weight that counts while a patient is not bleeding moved into the rules Def, at the same value.

### [0.1.2] - 2026-11-05 - Draft

#### Changed

- Two refusal messages spoke of settings rather than the world, under the owner's rule of 5 November 2026 that text a player reads
  in game stays in the game world. No behaviour change. The new wording is agent-authored and held for the owner's review. Not yet
  tested in game.
- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

### [0.1.1] - 2026-11-05 - Draft

Found by reading the load line 0.1.0 printed in the owner's game, then auditing the mod against the game's code. Not yet tested
in game. New refusal messages are agent-authored and held for the owner's review.

#### Fixed

- Epinephrine would have been given for pain: its small relief was enough to index it, and doctors carry it for cardiac arrest.
  A drug must now take away at least 30% of the pain and score above zero to count at all, read from its effects, so relief as a
  side effect no longer qualifies. The index on the owner's mod list falls from 40 items.
- Recreational drugs outranked medical ones once their setting was on: go-juice and heroin score higher than morphine because
  they cost no consciousness. They are now a last resort, after medical painkillers and sedatives.
- Two different painkillers could stack, because only the item's own last dose was checked. A medical painkiller still working
  now blocks every other painkiller. A sedative may still follow in extreme pain.
- The load line listed recreational drugs as if they might be given. It now names medical and recreational relief apart and
  says the second is never given while the setting is off.
- The right-click gave no reason when the care setting or a mental state blocked a patient, and offered nothing at all when the
  cached patient list was empty or up to 250 ticks old. It now checks directly and names the reason, for patients in need only.
- A doctor with no working hands could take the job; it now needs Manipulation, as tending and feeding do. Babies are no longer
  dosed. An ingestible that is not a drug was always refused by the game's drug check; it is now simply taken.
- Path searches ran for every indexed item on every check. The rules now run first and reach is tested only for an item the rules
  allow.
- Wording: 60% is not the game's severe band, which starts at 40%; and 80% is described as extreme pain.

#### Compatibility and limits

- Game-free rules pass 12 unit tests; the stacking check, the right-click reasons and the reach order live in game-facing code
  and have no unit test. Not yet tested in game.

### [0.1.0] - 2026-11-05 - Draft

The owner's request after finding that no doctor ever gives a painkiller or sedative on their own, Search and Rescue or not.
Not yet tested in game. Settings text and refusal messages are agent-authored and held for the owner's review.

#### Added

- Doctor work "give pain relief": a resting patient (down or in bed) in severe pain gets the painkiller with the strongest
  relief net of side effects, from the doctor's inventory first, then the nearest stack; past the pain-shock band a sedative
  when nothing lighter can be given. The patient takes it through the game's own toils, so the item's effects apply exactly
  as its mod wrote them.
- An index of every pain-relieving ingestible, built from the Defs at startup (owner's rule: effects, not names): pain,
  consciousness, breathing and bleeding from the outcome condition's stage at the dose, addictiveness and category from the
  drug comp. Any mod's drugs qualify; a mod can opt an item out with PhobosDosingExtension.
- Safety: no second dose while the first still works, none during an overdose, none that reaches a dangerous stage of its
  own condition, none whose offsets leave consciousness or breathing under 15%, none to a patient under "no medicine",
  recreational drugs only by setting, and the game's own willingness check (teetotalers, precepts) unless overridden for
  medical drugs.
- Settings: on or off, the minimum medical care level, recreational drugs for pain, drugs for patients who refuse them, the
  display mode. Two dev actions: log the index, log the decision for selected pawns.
- Every refusal says why, in the right-click menu.

#### Dependencies

- D'thorga Framework 0.1.1 or later (required). No other mod. Built twice: against the 1.7.4998 reference on .NET 10, which
  leads, and against the 1.6 reference.

#### Save compatibility

- Nothing is saved. Safe to add or remove at any time.

#### Compatibility and limits

- Effects a mod applies from its own code rather than its XML are invisible: the bleed increase of More Injuries' morphine (a
  linked condition) is not seen.
- A pawn up and working in pain is not dosed. Mechs do not dose. Search and Rescue's and More Injuries' doctors are not
  interrupted: a reserved patient is skipped.
- Game-free rules pass unit tests. Nothing is tested in game.

## D'thorga Triage history

### [0.2.0] - 2026-11-07 - Draft

Operations wait while something holds the patient from surgery. Not yet tested in game.

#### Added

- A patient that another mod says can't be operated on for now (D'thorga Medical Equipment 0.4.1's hyperbaric chamber is the
  first) is held: nothing new is queued for them, a bill already there stays, and doctors taking the worst case first skip them.
  Their inspect line and one message per condition say the operation must wait and why, in the words of whatever holds them,
  such as moving them to a bed. A held patient is tagged by their clock, never expectant (agent choice, 7 November 2026).
- A Workshop preview picture: a generated background the owner picked (7 November 2026), with the name added by script. No change in play.

#### Dependencies

- D'thorga Framework 0.4.0 or later, for its surgery holds.

#### Compatibility and limits

- Closes the limit D'thorga Medical Equipment 0.4.0 recorded: Triage queued an operation for a patient in the chamber and said it
  was queued while no doctor could start it. The game's own operate giver may still look at a held patient; Medical Equipment's
  rule refuses the job.

### [0.1.2] - 2026-11-07 - Draft

The care-choice button gets its own icon. Not yet tested in game.

#### Changed

- The "Doctors may operate" button on a patient shows a scalpel across a red cross, in place of the game's industrial medicine
  icon (generated art, the owner's pick, 7 November 2026; it passed the near-copy check, and the record beside the texture says how
  it was made). If the texture is ever missing, the button falls back to the medicine icon, never the red error cross.

#### Dependencies

- D'thorga Framework 0.3.1 or later, unchanged: the icon goes through its existing icon setting.

### [0.1.1] - 2026-11-06 - Draft

Fixes found in a documentation review the same day, before any play. Not yet tested in game.

#### Fixed

- A dying patient whose condition no operation removes (carbon monoxide poisoning, burn shock, a widespread burn) was tagged
  expectant, and a message said nobody could operate. Such a condition is now tagged by its clock like any other, nothing is
  queued and no message is sent: oxygen, tending and time are its treatment, not a bill.
- "For any repair" did the same as "to save life or a limb". It now also queues an operation for a condition that neither kills
  nor cripples, when one removes it.
- Drug addictions and gene-driven chemical dependencies are left out entirely: never operated on, tagged or named in a message
  (owner, 6 November 2026, after first-start messages about psychite dependency on 0.1.0). Found by what the game says the
  condition is, an addiction or a chemical dependency, never by name; a rules Def flag turns it off.
- The rules Def's urgentHours was never read. It is now the score's time scale: within a tag, a patient with that many hours
  left gets half the urgency of one with none (24 hours, as before).

#### Save compatibility

- Nothing saved changes. The map component the mod adds is saved by name, so removing the mod prints one harmless line per map
  on the next load, besides the one about its game component; 0.1.0's notes said only the latter.

### [0.1.0] - 2026-11-06 - Draft

The owner's ask of 6 November 2026: doctors should take over life-saving surgery on their own when given the agency, with
settings, and a triage score should decide which bills are done first. Not yet tested in game.

#### Added

- Doctors queue the operation a killing condition needs, by what the condition does and what each recipe removes, never by
  name: the operation most likely to succeed among those the colony can do, with a skilled doctor free, the ingredients in
  stock, the care level allowing the medicine, and a chance of death on the table within the setting. Bills the doctors queue
  are withdrawn once the condition is gone; the player's bills are never touched.
- A button on each colonist, slave, prisoner and guest: how far the doctors may go without orders (never, to save life, to save
  life or a limb, for any repair). Defaults by group in the settings: life-saving for colonists, slaves and prisoners, never for
  guests.
- Triage tags (immediate, urgent, delayed, minor, expectant) on the inspect line, from how soon each condition kills and how
  far the vital functions have fallen, and a copy of the game's operate work giver that takes the worst case first.
- Messages when an operation is queued and, once per person and condition, when nobody can do a needed one and why.
- Settings: on or off, the defaults by group, the highest chance of death accepted, rare tools, violations on other factions'
  people, messages, the order of work, the display mode.
- Dev actions: log the operations indexed, log the triage of selected pawns, run the planner now.

#### Dependencies

- D'thorga Framework 0.3.0 or later (its health layer and patient comp). No Harmony.

#### Save compatibility

- Saves only the list of bills it queued, by reference. Safe to add to a running colony; safe to remove, the bills it queued
  staying as ordinary bills, with one line about a missing game component on the next load.

#### Compatibility and limits

- Operations are found through recipes that set removesHediff or carry this mod's extension; a recipe whose worker removes a
  condition another way is not seen until its author or a patch adds the extension.
- Field care (tending, tourniquets, transfusions, CPR, airways) is left to Search and Rescue and More Injuries; this mod only
  queues operations and orders surgical work. Search and Rescue's rescue lanes and reservations are respected through the
  game's own reservation checks.
- A condition's hours to death are an estimate: a death clock's mean, the climb to a lethal severity at today's rate, or the
  game's bleed-out figure, whichever is soonest.
- The doctors' order of work is read from the 1.7 code; the 1.6 job giver's body could not be read and is assumed the same.

## D'thorga Medical Save Bridge history

### [Unreleased at the merge] - Draft

Not yet in a version of its own; carried into D'thorga Medical 0.6.0.

#### Changed

- The description now carries the disclaimer in the wording Ludeon's EULA requires. It said only that the mod is not official and not endorsed. No behaviour change.

#### Compatibility and limits

- README: says plainly that the 0.1.2 build is not yet tested in game, and gains a Checks done section and a diagram of one load.

### [0.1.2] - 2026-11-04 - Draft

A native RimWorld 1.7 build, which now leads (owner, 4 November 2026); the 1.6 build stays as the minimum backwards support
until 1.6 is retired. No behaviour change. Not yet tested in game on either version since this build.

#### Changed

- The assembly is built twice, against the 1.7.4998 reference on .NET 10 with Harmony 3 and against the 1.6 reference as before;
  the game loads its own version's folder (1.7/Assemblies or 1.6/Assemblies). Until now 1.7 loaded the 1.6 build.

#### Compatibility and limits

- Harmony 3 on 1.7 accepted every patch at build time; whether each still applies in play is unchecked.

### [0.1.1] - 2026-11-03 - Draft

Follows the owner's first load in game, which showed the one gap below. **Tested in game by the owner on 3 November 2026**:
the same copy of the live colony loaded again with 0.1.1, and the load report added 92 hediffs upgraded from the game's Hediff
to HediffWithComps, MSE2 re-installed nothing, and the error list was identical to the 0.1.0 load. The owner then saved and reloaded: the bridge logged INERT, every failure
pattern stayed at zero and no new error group appeared. A new colony is not yet tested in game.

#### Fixed

- Blood loss and hypothermia carried over from the save keep their severity. More Injuries re-classes those two vanilla hediff
  defs to the game's HediffWithComps so they can carry its comps, and 0.1.0 only upgraded classes that came from a newly added
  mod, so it left them to Medical System Expansion 2, which re-makes such hediffs from scratch: in the owner's load 79 blood-loss
  and 8 hypothermia hediffs came back fresh, their severity gone. The bridge now upgrades any carried-over hediff whose def's class
  has since become a subclass of the saved one, whichever mod the class belongs to.

#### Compatibility and limits

- Without MSE2, 0.1.0 left those hediffs on the plain class, without More Injuries' comps, until they healed. Now they load as
  More Injuries expects on either setup.

### [0.1.0] - 2026-11-03 - Draft

Owner's request, from a handoff investigating why More Injuries (Continued) broke the PG17 colony when added to a save that
runs Medical System Expansion 2.

**Tested in game by the owner on 3 November 2026**, first load only: a copy of the live colony (an autosave of PG17, RimWorld
1.7.4995, about 330 mods, Combat Extended, MSE2) loaded with More Injuries, both its add-ons and No More Lethal Damage Threshold
switched on in the same step. The log said ACTIVE with Human (8 new parts), 3,811 parts re-numbered, 1,687 injuries and 1,071
missing parts upgraded, 517 wipes prevented, no hediff on a part newer than the save, no added part off its recipe's parts, and
none of the failure patterns; the error list matched the control run without More Injuries. The owner reported the colony
playing well, with More Injuries' own migration dialogs and new surgeries in place. Save-and-reload and the new-colony check
are still to run.

#### Added

- The bridge itself: on the first load of a save written without More Injuries (or any other mod that adds body parts), every
  saved body part reference is re-numbered from the old part list to the new one, so wounds, missing parts, prosthetics, bionics,
  MSE2 modules and surgery bills keep the part they had. Nothing is written to disk until the player saves.
- Old wounds and missing parts are created as More Injuries' own classes on that load, carrying the bleeding state a live wound
  would, so Medical System Expansion 2 has nothing to re-install and no lost limb throws an error.
- More Injuries' first-load clean-up, which removes every wound and missing part from every pawn and sets dead pawns back to
  Mobile, is held off on that load by clearing its flag as it is set.
- A load report in the log: the ACTIVE or INERT decision and why, counts of what was done, then a check of every pawn, dead or
  alive, for hediffs sitting on parts the save never had and for added parts sitting off the parts their recipes allow.
- Two dev-mode actions under D'thorga: log body part indices (each part's old number, or the mod it is new from), and check every
  pawn's parts.

#### Dependencies

- Harmony. Load after More Injuries and Medical System Expansion 2; neither is required, and the bridge references neither
  assembly. Without More Injuries it only re-numbers parts for whatever other part-adding mod the save lacked.

#### Save compatibility

- Safe to add; it saves nothing. Safe to remove at any time. After one save with More Injuries it is inert for that colony.

#### Compatibility and limits

- A mod that moves or deletes an existing body part, rather than adding one, is beyond the re-numbering rule.
- A newly added mod that inserts records of vanilla part defs (a second kidney, say) is not seen as new, because the bridge tells
  new parts by the mod that defines the part def.
- More Injuries' add-ons (Organ Failure, Medieval Solutions) were not studied. Keep them off for the first load.
- The bleeding-state defaults written for upgraded wounds were read from More Injuries 1.6.0.2. A later version that renames them
  loads such wounds with its own defaults instead, as it would without the bridge.
- Built for RimWorld 1.6, loaded on 1.7 too. The hooks it uses exist in both; a missing one switches that part off with a log line.
