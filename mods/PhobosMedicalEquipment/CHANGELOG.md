# D'thorga Medical Equipment changelog

Maintained from 5 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.4.2] - 2026-11-07 - Draft

A fix from the owner's game. Not yet tested in game.

### Fixed

- Right-clicking a vehicle with a doctor selected offered a line saying the doctor couldn't use a breathing device on it because
  it was "breathing well enough" (seen in the owner's game on 0.4.1, on a tank, 7 November 2026). Vehicles are a kind of pawn to
  the game. The line is now shown only for a real patient, lying down, and only when the reason is worth knowing: the airway is
  shut, no device is within reach, or the patient is already on oxygen. A healthy colonist gets no line either.

## [0.4.1] - 2026-11-07 - Draft

Not yet tested in game.

### Fixed

- The hyperbaric chamber tells other mods why its patient can't be operated on, through D'thorga Framework's new surgery check, so
  D'thorga Triage can hold the operation and say why instead of reporting it queued (once Triage reads it).

### Dependencies

- D'thorga Framework 0.4.0 or later.

## [0.4.0] - 2026-11-07 - Draft

The owner's answers of 7 November 2026 for the pieces added in 0.3.0. Harmony comes into this mod, for three small postfixes. Not
yet tested in game.

### Added

- Doctors carry a downed patient who needs oxygen to a free, ready hyperbaric chamber instead of the nearest bed. With none free,
  the game's own rescue goes on as before. Patients already in a bed stay where they are.
- A one-time letter, the first time a colony starts or loads without Vanilla Gravship Expanded, says which five pieces need it and
  what works without it. It is shown once per install. The settings window says the same.

### Changed

- No operation is done on a patient lying in a hyperbaric chamber; the bill waits, and the chamber says which patient to move.
- A liquid oxygen store without power takes in no more oxygen. It still gives out what it holds, boils off as before, and says so.
- The Workshop preview's lettering has a thin dark edge, so it reads on light backgrounds too.

### Dependencies

- Harmony, for the three postfixes: the pipe network's storage check is not overridable, a medical bill's readiness has no
  extension point, and the letter needs the game's start-up step without saving anything in the game.

### Save compatibility

- Safe on a save from 0.3.0: nothing new is saved in the game. The letter's "shown" flag is in the mod settings.

### Compatibility and limits

- With D'thorga Triage, a patient lying in a chamber may still have an operation queued, and Triage still says it is queued while
  no doctor starts it; the chamber's inspect line says to move them. Fixed in 0.4.1 with the Framework's shared check.

## [0.3.0] - 2026-11-07 - Draft

The rest of the oxygen kit and the field kit the owner asked for (7 November 2026): a pressure chamber, two devices doctors use by
themselves, a burn dressing, and an electrolyser and a liquid store to make and keep oxygen. Not yet tested in game.

### Added

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

### Changed

- An oxygen machine that is itself a bed serves whoever lies in it, and a machine can give its own, stronger treatment condition.
- The oxygen conditions now check the patient's breathing by themselves, so the kit and the hand pump never lower breathing that is
  already above normal, as the machines never did.
- The oxygen tooltip works out how many times faster carbon monoxide clears from each treatment's own half-life, instead of a fixed
  four times.
- The hyperbaric chamber is 1 by 3, not the 2 by 3 first proposed: a bed holds as many patients as it is wide.

### Balance

- All new costs, power draws, work, the 6 canisters an hour in the chamber, the boil-off and the breathing figures are authored. The
  electrolyser's 3000 W follows real electrolysis (about 3 to 3.6 kW for one patient's oxygen, our arithmetic from a published
  range), which is far hungrier than Vanilla Gravship Expanded's harvester by that mod's own balance. The half-lives are sourced.

### Dependencies

- The chamber, the field kit, the electrolyser and the store need Vanilla Gravship Expanded, like the bedside stand, and load only
  with it. They join its oxygen network instead of a second network of our own.
- References Vanilla Expanded Framework's PipeSystem assembly from the installed mod, for the store's boil-off; nothing of it is
  copied.

### Save compatibility

- Safe to add to a running colony. The new treatment conditions live with the core Defs, so a save still loads cleanly if Vanilla
  Gravship Expanded is taken out while a patient has one; each lapses within the hour. Its buildings and kits would be dropped.

### Compatibility and limits

- Art: the store and the three items are the owner's picks (references only, no private model). The electrolyser and the chamber
  are scripted placeholders until generated art is picked, and the store's frost is not drawn yet.
- A chamber does not stop surgery on its patient, and patients are not sent to it in preference to other medical beds.

## [0.2.3] - 2026-11-07 - Draft

The owner's art picks, and the README brought up to date. No change in play. Not yet tested in game.

### Changed

- The oxygen concentrator has generated art in place of its hand-drawn placeholder, redrawn from that placeholder's layout (the owner's
  pick, 7 November 2026). No private model was used for it: only the placeholder and the game's vitals monitor as references.
- The bedside oxygen stand has new generated art, a slim pole with a flowmeter and a coil of tubing (the owner's pick, 7 November
  2026). No private model either: the game's vitals monitor and oxygen pump were its references.
- The mod has a Workshop preview picture: a generated hospital ward, with the name added by script.
- Both textures are now 128 px (the concentrator was 64), for detail when zoomed in. Each passed the near-copy check; the record beside each
  texture says how it was made.

### Fixed

- The uninstall button's description now names the burn unit beds as well as the machines. README brought up to date: the burn
  unit bed in the opening line, Framework 0.3.0 or later, the owner's bed report, the burn-care extension for modders.

## [0.2.2] - 2026-11-06 - Draft

No change in play. Not yet tested in game.

### Changed

- The lapsing treatment condition and the "who was treated, who was left alone" pass now live in D'thorga Framework's health
  layer, shared with D'thorga Triage. The time a treatment lingers after the machine's last pass (600 ticks, unchanged) moved
  from this mod's rules Def onto the condition's comp in the hediff Defs.

### Dependencies

- D'thorga Framework 0.3.0 or later.

### Save compatibility

- The treatment comp keeps its saved key, and the game saves no class name for a condition's comps, so saves made on 0.2.1 load
  unchanged (read from the code, not yet tested).

## [0.2.1] - 2026-11-06 - Draft

Fixes after the owner's first test of the burn unit bed: what the machines say, and oxygen that cut above-normal breathing. Not yet tested in game.

### Fixed

- An oxygen machine with a patient who breathes well enough now names them and says why it gives them nothing, instead of waiting in
  silence. Oxygen still goes only to a patient with weak breathing, or with smoke, fumes or carbon monoxide in the lungs.
- A burn unit bed now names an occupant with no open burns, and says it gives them no air-cell care.
- Oxygen no longer lowers a patient whose breathing is above normal, such as a pawn with bionic lungs treated for carbon monoxide. Its
  ceiling at normal used to cut them down to it; now oxygen raises only breathing below normal, and leaves the rest alone.

### Save compatibility

- An oxygen condition saved by an earlier version is replaced with the new kind on the machine's next pass. Nothing else changes.

### Compatibility and limits

- Tested in game by the owner on 0.2.0: the burn unit bed looks right and gives a burned patient air-cell care. The owner's report that
  oxygen did not start was a patient breathing at full strength, so the machine was right to hold back; it now says so.

## [0.2.0] - 2026-11-05 - Draft

A burn unit bed, the owner's go after their Burns and Smoke results. Not yet tested in game.

### Added

- Burn unit bed: a hospital bed with an air-cell mattress and a blower. While it has power, a patient with an open burn gets the on air
  cells condition: wounds heal 25% faster and pain falls by 15%. It works by itself and lapses soon after the patient leaves or the power
  stops. Its better tend quality lowers infection through the game's own rule, powered or not.
- Burns from the game's four kinds and Combat Extended's incendiary burn count. Other mods can mark their own in XML.
- The art is the owner's pick. It is AI-generated, by a model trained in private on 60 of the game's sprites; the record beside it has the
  details. North and east are the south view turned, as it is drawn from directly above.

### Changed

- The uninstall button now removes every treatment condition, and its wording says so.

### Fixed

- The load line said 0.1.0 in version 0.1.1. It now matches.

### Balance

- All the bed's figures are authored: healing, pain, tend quality, comfort, power and cost. None has a clinical source.

### Compatibility and limits

- The healing speeds all of the patient's wounds, not only the burns.

### Save compatibility

- Safe to add to a running colony. Before removing the mod, deconstruct the beds as well as the machines.

## [0.1.1] - 2026-11-05 - Draft

New art for the bedside oxygen stand, picked by the owner. The new art is not yet tested in game. The owner reports that a patient's breathing improves on the oxygen concentrator (tested on 0.1.0); the bedside stand is not yet tested in game.

### Changed

- The bedside oxygen stand has generated art in place of its placeholder. It is AI-generated, by a model trained in private on 60 of the
  game's sprites, and the owner chose to ship it knowing that Ludeon's EULA does not settle this. The record beside the texture has the
  details. The sprite passed the near-copy check. The oxygen concentrator keeps its hand-drawn placeholder.

### Save compatibility

- Art only. Safe in a running colony.

## [0.1.0] - 2026-11-05 - Draft

The first pieces of the owner's medical equipment idea: oxygen at the bedside. Not yet tested in game. Labels, descriptions and the
placeholder art are agent-authored and held for the owner's review.

### Added

- Oxygen concentrator: a one-tile bedside facility that needs only power, for up to two patients.
- Bedside oxygen stand, only while Vanilla Gravship Expanded is active: burns its oxygen canisters, and fills from its oxygen pipes
  through Vanilla Expanded Framework's own linking. One canister per patient per in-game hour.
- Both link to any bed that accepts a vitals monitor, found by that and not by name, and are built against the bed in the same way.
- Treatment is automatic (owner's choice): a patient in a linked bed with a condition oxygen helps, or with breathing under 80%, gets
  the on oxygen condition with no job. The worst breathing is served first. It lapses soon after the supply stops.
- On oxygen raises breathing by 20 points up to normal. With D'thorga Burns and Smoke, carbon monoxide's half-life falls to 75 minutes.
- Each machine's inspect text says what it is doing or waiting for. Machines have the game's power switch.
- Settings: the display mode and an uninstall button. A dev action logs a pawn's oxygen need.

### Balance

- Power, cost, work, the patient limit, the canister rate and the breathing figures are authored. The 75 minutes is sourced.

### Dependencies

- D'thorga Framework 0.2.0 or later and Vanilla Expanded Framework (required; the latter is the owner's choice, for the pipes
  this mod will need, though this version uses its linking only through Vanilla Gravship Expanded). Burns and Smoke 0.4.0, Vanilla
  Gravship Expanded, Combat Extended and More Injuries are optional and used when present.

### Save compatibility

- Safe to add mid-game. To remove: deconstruct the machines, press the settings button, save, then switch the mod off.

### Compatibility and limits

- Oxygen does not repair lungs, does not treat More Injuries' hypoxia directly beyond easing breathing, and cannot help a closed airway.
- A patient on oxygen in a room full of carbon monoxide still breathes it in, a simplification. Move them.
- The art is a scripted placeholder with one view. Only beds are served: no field use yet.
- Game-free rules pass their unit tests and it builds for 1.7 and 1.6. Nothing is tested in game, including the pipe link.
