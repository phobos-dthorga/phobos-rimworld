# A chip detector: wear seen before it breaks (28 Nov 2026)

**Status (28 Nov 2026):** a design record; nothing is built. The owner chose by card to build all three of the schematic's missing parts,
"each with its own record first" ([the schematic record](vehicle-schematic.md), M3). Building waits for the owner's answers below.

## Read this first

- **What it is**: a warning lamp that lights when metal from a wearing engine or gearbox reaches its oil. It reports wear the model
  already does; **by the agent's lean it adds no harm of its own**.
- **Why it earns a place**: today an engine wears quietly (low oil pressure, a worn hot section, an overdue service) until it breaks
  down. A chip light is the warning between the two, a decision the player can make in time.
- **Nothing here is tested in game.** Marks: **[O]** the owner's, **[A]** the agent's, **[code]** read from our source or Defs.
  Real-world statements from general knowledge, not looked up, say **unverified**.

## What a chip detector does

A magnetic plug in an engine's or gearbox's oil catches iron and steel particles shed by wearing bearings and gears. The electrical
kind closes a circuit when enough metal bridges its gap and lights a caution in the cockpit; in helicopters a chip light on a gearbox
usually means landing soon. Land machines more often use magnetic drain plugs checked at oil changes, and fleets send oil samples for
analysis (general knowledge, **unverified**: no source was looked up).

## What exists to build on [code]

| Piece | State | Where |
| --- | --- | --- |
| Wear on an engine | Low oil pressure wears it (2 health an hour), none seizes it; a worn hot section and an overdue service raise breakdowns | `EngineFeeds.Harm`, `Systems_Lubrication.xml`, `Systems_Turbine.xml`, `Systems_Service.xml` |
| Breakdowns | `Effect_Breakdown`: a chance in mean hours between, on the most worn part where kept per part | `Effects.cs` |
| Oil per engine on aircraft | One sump each | `EngineFeeds` |
| A helicopter's gearbox | The rotor drive, with over-torque | `PhobosVehicleSystem_RotorDrive` |
| Warning lamps | Every notable stage lights one; the Dashboard's pictures are tinted white shapes | `ITab_VehicleDashboard` |

## The model [A]

| Row | What |
| --- | --- |
| CD1 | **A quantity `oilMetal`**, 0 to 1, per engine (and per rotor gearbox), kept by part as hot-section life is |
| CD2 | **It rises only with wear the model already does**: each share of an engine's health lost to wear (not to hits) adds metal; running in a "worn" or "overdue" stage adds a little an hour |
| CD3 | **The lamp lights at 0.5**: a notable amber stage, "chip light", with a message naming the engine. At 0.8 it turns red, still with no effect of its own |
| CD4 | **An oil change clears it** (the engine service), but the cause stays: an engine still wearing brings it back |
| CD5 | **The diagnosis report names the engine and the likely cause** (low oil, the hot section, the gearbox) |
| CD6 | **The schematic's chip box is real**, one per engine on the aircraft pages |
| CD7 | **A dashboard picture** for the lamp, made locally with the night run's icons |

CD2's rates and CD3's lines are authored, set so the lamp lights some hours before a likely breakdown.

## What it changes in play

- **A warning between quiet wear and a breakdown.** The player can bring a vehicle in, ease off, or press on knowing the risk.
- **A twin's pilot knows which engine** to nurse, since the lamp names it.

## Other factions' vehicles

- **In a raid on us, or parked at a base we attack**: nothing; the lite tier keeps no wear. The player's crew could not see another
  faction's lamps anyway.
- **Captured**: clean, as its oil is unknown (agent default).

## The four questions

- **What it lets the player do**: see internal wear before it breaks something, and choose when to act.
- **What it stops them doing**: nothing.
- **How it changes the way they do things**: a light to weigh mid-trip: turn back, or carry on.
- **What outside the vehicle notices**: **the vehicle's logbook and event log** keep the lamp's lighting, so a vehicle's history shows
  its warnings before its breakdowns. Nothing else, as a choice.

## Risk and settings

**None** by the lean (question 1): it reports, and harms nothing. **No setting of its own**: switching off the lubrication system
already switches it off. If the owner chooses that a lit lamp raises breakdowns, the risk becomes **low** and it gets its own setting,
on by default.

## Cost

One figure per engine, raised where wear is already applied, and one stage on the 250-tick update. Nothing per tick.

## What a next game would need

The rule (metal from wear, the lamp's lines) is game-free. A next game needs its own wear events and its own lamp.

## Questions for the owner

On the Decisions page (`config/decisions.json`), each with the agent's lean.

1. **Should a lit chip lamp make a breakdown likelier?** Lean: no, it reports wear that already causes breakdowns.
2. **Helicopter gearboxes too?** Lean: yes, a rotor gearbox's chip light is the classic one.

## Sources

- How chip detectors work: general knowledge, **unverified** here.
- Our own records: [the schematic](vehicle-schematic.md), [per-engine realism](per-engine-realism.md), [engines and the drivetrain](systems/engines-and-drivetrain.md).
