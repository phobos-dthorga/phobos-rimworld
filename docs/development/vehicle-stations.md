# Vehicle stations and the motor pool tab: design (9 Nov 2026)

**Status: built in D'thorga Framework 0.43.0. Not yet tested in game.**

## Read this first

- **The owner's reason:** "with a list of mods like I have which is not at all uncommon, you have lists into the hundreds that make finding parts just
  disastrous." The machining table's bill list, and the shared Architect tabs, are where our things get lost.
- **Vehicle parts, jump-start kits and booster packs are now made only at the vehicle parts bench.** You need one before you can make any more of them.
- **Tunes and grade refits wait for a tuning rig** (a setting, on by default). Fittings and replacing a destroyed part do not.
- **Our things are under a new Architect tab, motor pool**, beside Vehicle Framework's Vehicles tab. Other authors' vehicles stay where they were.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| The motor pool tab | Find our benches and our vehicles in one short list | Nothing | One more tab on a long Architect menu, in return for short lists inside it |
| The vehicle parts bench | Open a bill list holding only vehicle parts | Making our parts and kits at a machining table or fabrication bench | A vehicle workshop becomes a place in the base, needing a build, power and a worker's walk to it |
| The tuning rig | Do tunes and refits with a visible reach, shown while placing and selecting it | Tuning or refitting a part anywhere, such as in a caravan or away from power | Vehicles come home to be tuned; where the rig stands decides the layout of the vehicle yard |
| The setting that drops the rig rule | Play with tunes anywhere | Nothing | Restores how 0.42 worked |

## How it works

- **Which work needs the rig** (agent's reading of the owner's "Required"): a tune, or a refit of a working part to another grade. A destroyed part's replacement,
  even with a grade chosen, is a repair and needs none, so a vehicle is never stranded for want of a rig. Fittings need none.
- **Orders can be placed anywhere and wait.** The inspect line says what they wait for, and so does the fittings window. Haulers still bring the materials ahead of the
  vehicle reaching a rig.
- **Reach:** any cell of the vehicle within 3 cells of the rig's edge (authored), read from the vehicle's real footprint, which Vehicle Framework patches `OccupiedRect` to
  give (from the code: `Patch_VehiclePathing.OccupiedRectVehicles`). The rig must have power: flicked off, unpowered or broken down all count as not working.
- **Moving the recipes:** only the recipe's list of tables changes. From the code, the game reads a table's recipe list for the Add Bill menu and for pasting a bill
  (`ITab_Bills.FillTab`), and not when a colonist does a bill, so **a bill already on a machining table goes on working** until it is removed.
- **The tab** is a new `DesignationCategoryDef`, order 23, just under Vehicle Framework's 24. Only the bulk tanker's blueprint moves, by naming the category in its own Def.

## Cost and risk

- `AtRig` scans the player's rigs (normally one or two) only when a tune or refit is pending, or the fittings window is open.
- Saves: safe to add. A tune already ordered simply waits for a rig until one is built or the setting is turned off.

## Who chose what

| Choice | Who |
| --- | --- |
| Own stations; a parts bench and a tuning rig; parts only at our bench; the rig required | Owner, 9 Nov 2026 |
| Our own build category | Owner, 9 Nov 2026 |
| The name "motor pool", its place beside the Vehicles tab, and moving the tanker into it | Agent; one line each to change |
| Which work needs the rig; the setting to turn it off | Agent |
| Reach of 3 cells; sizes, costs and power of both stations | Agent, authored, a little under the machining table's 150 steel, 5 components and 350 W |
| Bench and rig pictures | Agent: stand-ins drawn in code, for proper art when the owner wants it |
