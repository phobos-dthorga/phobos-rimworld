# Code audit: saves, cost per tick and missing things (11 Nov 2026)

**A code read, not a game test.** The owner asked on 11 Nov 2026 for a general audit beyond the other-factions rule, and chose three
areas: saves, cost per tick, and code that assumes something is there. The radio framework was left out (back burner). Read with
small scripts over `src/` and the decompiled game, commit d5120be onward; line numbers are of that day's source. Classifications are
agent-authored.

## Read this first

- **The code is in good shape on all three.** No crash path, lost data or per-tick cost of note was found.
- **One fault fixed:** a depot bay saved its assigned vehicle even after the vehicle was destroyed, so the next load looked for a
  thing that was gone. Fixed in D'thorga Framework 0.96.1, with pass 1's fixes. Not yet tested in game.
- **One proposal:** D'thorga Patches keeps two saved components in assemblies that load only with IRBM or RimPacts. Removing either
  of those mods later would cost a red "Could not find class" error each time that save loads. Nothing is lost but that part's memory.
- **What a read cannot say:** how much time any of this takes on the owner's list. The test list at the end asks for one measurement.

## Saves

| Check | How | Found |
| --- | --- | --- |
| Keys that could collide with another mod's | Every saved key (285) mapped to its class; those written straight onto a game object (a comp, a hediff, a building, a component) | **None unprefixed**, and no two of our classes share a key |
| Lists a save from an older version leaves empty | Every saved list and deep-saved object, checked for a guard after loading | All guarded. The one apparent miss is a list built only for saving (`MapComponent_PhobosRoomGases.cs:308`) |
| Saved Defs that another mod's removal takes away | All 15 saved Def references | All handled: dropped, replaced by a default, or checked where used. Tank fluids empty the tank (`CompPhobosTank.cs:55`); a diagnosis report for a vehicle that has gone opens as a plain letter (`Letter_PhobosReport.cs:34`) |
| Saved references to things that may be destroyed | All 9 | All cleared or filtered, **except the depot bay's**, which is now fixed (`DepotBays.cs:69`) |
| Saved gas by position in a list | Room gases | Saved by gas name (`MapComponent_PhobosRoomGases.cs:27-41`), so adding a gas is safe |
| Saved classes that vanish with an optional mod | Every game, world and map component (19) and every saved class in an optional assembly | See below |

**The optional components.** From the game's code, a saved object whose class cannot be found logs "Could not find class" as an
error and is dropped (`Verse.ScribeExtractor.cs:108-112` in our decompile of 1.7). Two of ours live in assemblies that load only
with another mod:

| Component | Assembly, loaded with | Removing that mod later |
| --- | --- | --- |
| `WorldComponent_PhobosStrikeMemory` | `PhobosPatches.Irbm`, IRBM | A red error on each load of that save; the strike memory is gone |
| `GameComponent_PhobosWarQuestCountdown` | `PhobosPatches.RimPacts`, RimPacts | The same; the countdown is gone |

D'thorga Framework's own vehicle components behave the same if Vehicle Framework is removed, and its README already says to press
the removal button first. D'thorga Patches' README says nothing for these two.

**Proposed (agent-authored):** a small saved store in D'thorga Framework's core, which a part loaded with another mod writes into
by key. The core always loads, so the save never names a class that may vanish, and a part that comes back finds its data again.
It is generic, so per the owner's rule it belongs in the core. Until then, a line in D'thorga Patches' README.

**Built, owner's choice of 11 Nov 2026:** `GameComponent_PhobosPartStore` in D'thorga Framework 0.97.0
(`src/PhobosFramework/Persistence/PartStore.cs`), with D'thorga Patches 0.16.8 moving both parts onto it. Not yet tested in game.
One finding changed the plan: when a save's mod list matches the game's, the game skips every old-save converter, so simply deleting
the two classes would have cost an error on the next load. Both classes stay instead: each reads what an older save holds, hands it
to the store and retires once loading ends, so the next save no longer names it.

## Cost per tick

| Check | Found |
| --- | --- |
| **Everything of ours that runs each tick** (12 methods) | Each returns at once unless its interval has come round or it has work: gases, fire smoke and heat, protection zones, the parked-vehicle warning, triage, the caustic plant pass, the RimPacts countdown. The medical boil-off check runs per storage each tick with an interval test, which is cheap |
| **Harmony patches on methods the game calls constantly** | The allowed-area check (`ProtectionPart.cs:70`), the step into each path cell (`:55`), the temperature at a cell (`FireHeatPatch.cs:16`), the vehicle draw and threat checks (`SubmersiblePatches.cs`), the vehicle path cost (`Patches.cs:128`), held temperature (`Patches.cs:252`) and the triage tag overlay all leave after a cached lookup or a flag |
| **Finding a map component** | The game's lookup walks every map component in turn (`Verse.Map.cs:1205-1215`), and a 340-mod list has many. The hot callers keep the last answer or a table (`ProtectionZones.For`, fire heat, triage). The room gas lookup does not, but is called only about once a second per pawn, and only when carbon monoxide is carried as our own gas |
| **Factory patches on every belt and process tick** | The stuck-belt check is two comparisons; the bill repair runs once per process (`BillRepairPatches.cs:21-23`) |

Nothing here is a measurement. With dev mode on, D'thorga Framework times its vehicle work, and the dev action "Vehicle systems:
timing report" writes the totals to the log.

## Missing things

| Check | How | Found |
| --- | --- | --- |
| Places the code claims something is never missing (`!`) | All 134 listed; the 20 or so on live game objects read | All follow a check: a pawn without a mood is filtered out before the mood file (`ColonyMoodFile.cs:46`), a jump start checks the donor first (`CompPhobosVehicleSystems.cs:1609-1614`), the water tap reads the pipe safely before filling. The rest are Defs cleaned at load, settings, or reflection resolved at start-up |
| A vehicle's map, used where it may have none | Every use in the vehicle code; a vehicle in a caravan runs the full model with no map | 8 uses without a check in the same method. Five run only while the vehicle is selected on a map. The drill rig's and survey truck's work paths are safe because leaving the map packs a rig up (`CompPhobosRigWork.cs:147-152`). The other two run only for another faction's boat in the water |
| Calls that throw on an empty list | `.First()`, `.Single()`, `.Last()` | Three, each on a list known not to be empty |

## Test list for the owner

Short, highest consequence first, for after the owner installs Framework 0.96.1 and the current Vehicles.

1. **Load the live save and run `python scripts/triage-log.py`.** Several drafts since the last test changed Defs and saved data (parts
   bills, research, fittings). A red line on load is the thing to catch first.
2. **Meet a raider's or visitor's aircraft, and if you can, capture any vehicle.** Expect no messages about their engines; a captured
   vehicle should show its systems at the next look.
3. **Turn dev mode on through a busy moment (a raid, or the big factory running), then run the dev action "Vehicle systems: timing
   report".** It writes what our vehicle code took to the log, the one measurement this audit could not make.

## Who chose what

- **Owner:** the audit and its three areas (11 Nov 2026).
- **Agent:** the methods, every judgement, the fix and the proposal.
