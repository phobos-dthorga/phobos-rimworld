# D'thorga Medical Equipment changelog

Maintained from 5 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

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
