# The vehicle tabs: Dashboard and Systems (9 Nov 2026)

A design record, built the same day in D'thorga Framework 0.17.0 (Draft). **Not yet tested in game.** The owner asked for "our
own, unique, vehicle statistics" in a tab of their own beside Vehicle Framework's Customize, Cargo, Pawns and Health, "with all
kinds of statistics, graphs, health-bars, other little displays", named it **Dashboard**, and kept a second tab, **Systems**, "for
the more in-depth stuff" (9 Nov 2026).

## Read this first

- **Two tabs, two questions.** Dashboard: *does anything need me right now?* Systems: *what exactly is going on, and why?* The
  reason for anything is the same in both; only the depth differs (`docs/development/player-language.md`).
- **Systems is hidden in Plain mode**: it is nothing but figures. Dashboard shows its figures only in Operator and Engineer.
- **A Dashboard gauge opens Systems at that system.**
- **The graphs keep a day of history, saved with the vehicle** (owner, 9 Nov 2026): one sample per rare tick of every shown
  quantity, 240 samples, as one compact string per quantity (about 480 bytes). History cannot be recomputed, so this is the one
  exception to "derived state is never saved", recorded here, in the changelog and in the code.
- **RimHUD needs no hook.** Vehicle Framework ships patches that hand the vehicle pane back from RimHUD, and RimHUD draws the
  game's inspector tabs itself (85 px buttons, right to left) and calls each tab's own drawing unchanged (read in RimHUD's source,
  9 Nov 2026). The tab labels are one short word each, and nothing hard-codes the pane's size.

## How they hook in

Vehicle Framework adds its own Upgrades tab inside `VehicleDef.ResolveReferences`, before the game's `ThingDef.ResolveReferences`
builds the resolved tab list; our prefix there (`Service/Patches.cs`, which calls `VehicleSystems.Apply`) runs earlier still, so
`Apply` appends the two tab types to `def.inspectorTabs` for every fitted design and the game resolves them. The game makes one
shared instance of each tab type, so per-vehicle UI state (scroll, a pending scroll target) lives in the core's `TabState<T>`, keyed
by the vehicle's ID and cleared when the tab closes. **To re-check when VF 1.7 ships:** that `ResolveReferences` keeps the order.

## What each tab shows

| | Dashboard (460×300) | Systems (560×450, scrolls) |
| --- | --- | --- |
| Always | The waiting line (a refusal, or the due service and what it lacks, or "Nothing needs you."); a gauge per quantity marked `showOnDashboard` (battery charge, coolant temperature, oil level, tread wear, brake wear, steam pressure, hot-section life, cab temperature), coloured by stage; a warning light per notable stage, red when the stage refuses, damages or explodes; the switches (each toggle load, lit green when on and active, amber when on but idle) and the service button | A section per running system: each quantity's figure and stage with a bar for fractions and temperatures; each role's parts with their health in green, amber or red (the core's `HealthColour`), the part lit on the vehicle when hovered; the engine (gear, rpm, load; what holds the vehicle back); the turbine (speed, temperature, phase, rating, engines working) |
| Operator | Figures on the gauges; the tachometer and gear, or the turbine's speed and temperature | as above |
| Engineer | the same | a day's graph under each quantity (the game's curve drawer, scaled to the quantity's range); the flows an hour; kilowatts; mass; the hover margin |

**No part icons, and no Vehicle Framework colour helper.** Vehicle Framework ships only a 1.6 assembly, whose methods name Unity's
`UnityEngine.CoreModule`; a 1.7 build compiles against the Waylith engine's stubs, so a call that passes a `Rect` or returns a `Color`
through Vehicle Framework's assembly does not compile (found at build, 9 Nov 2026). The tab shows each part's name and health
instead. **To revisit when VF 1.7 ships.**

Presentation only: every click delegates to the comp (`SetSwitch`, `SetServiceRequested`) or opens the other tab. The widgets
(`Gauge`, `WarningLight`, `HistoryGraph`, `Section`, `Tachometer`) (with `HealthColour`) went to the Framework core's `UI/PhobosWidgets.cs` (namespace `Phobos.Framework.Interface`), as the owner's
rule asks, with the history ring and its codec in the core's `Rules/History.cs` (tested) and the Scribe wrapper in
`Persistence/ScribeHistory.cs`.

## To test in game (owner)

1. Select a vehicle: both tabs appear beside Vehicle Framework's; Plain mode hides Systems.
2. Click a Dashboard gauge: Systems opens scrolled to that system.
3. "Fill history with a test curve" (dev), then Engineer mode: a graph under each quantity; save and reload: the graphs survive.
4. Hover a part row in Systems: the part lights up on the vehicle; it stops when the tab closes ("log tab state": none held).
5. With RimHUD, docked and floating: the tabs sit in RimHUD's strip and draw as on the plain pane.

## Who chose what

- **Owner (9 Nov 2026):** the two tabs and their names; a day of saved history.
- **Agent choices, owner to revise:** the layouts and sizes; which quantities get gauges; the stage colours; Systems hidden in
  Plain; the per-quantity history as 16-bit samples.
