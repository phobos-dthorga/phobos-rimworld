# Vehicle and submarine rules

Moved from the repository's `AGENTS.md`, "RimWorld engineering", on 18 Nov 2026 (owner's go-ahead), text unchanged; that
file points here. They govern every vehicle, upgrade tree, accessory, part and submarine mechanic of ours, in XML and C#;
the general engineering rules there (parts bills, pipes beneath, optional tie-ins, Harmony) still apply.

- **Every kind of vehicle meets the vehicle standard** (owner, 27 Nov 2026: "We need to bring everything in-line to some sort of
  standard so players aren't left bewildered and confused"): `docs/development/vehicle-standard.md` says what every land vehicle,
  aircraft, helicopter, boat and trailer shows on its Systems, Fittings and Health tabs, and what does not belong. A new loadout, system,
  bay or fitting is checked against it, and a line it adds or changes goes into the standard in the same change. The audit that keeps
  it ("below the standard", the owner's card of 27 Nov 2026) is its batch 2.
- **Every vehicle of ours is upgradeable and extendable from its first Def** (owner, 8 Nov 2026): a Vehicle Framework upgrade
  tree shaped by the vehicle, every node moving a figure that changes an outcome, node keys stable once a save can hold them; the
  base body is drawn bare and every visible node is its own overlay set, planned before the first picture
  (`docs/development/utility-vehicles-design.md`, principle 8).
- **Upgrade trees branch only where one upgrade does nothing, or makes no physical sense, without another** (owner, 17 Nov 2026,
  after the tanker's flat tree let compartments go on a bare deck). Research paces progress; a tree never adds gating of its own, and
  "better with" is no link. Four links, each for its own reason: **either/or** for things that fill the same place (Vehicle Framework's
  exclusion list, on both nodes); **needs all of** for building on one earlier upgrade (`prerequisiteNodes`); **needs any one of**
  for alternatives (`PhobosUpgradeAnyOf`); and **holds it on** only for what sits inside or on its parent. Alternatives share a row,
  dependants sit below, and free-standing upgrades take their own row. **Every tree fits Vehicle Framework's tab** (owner's choice,
  20 Nov 2026, after the armour kits stacked under five trucks' trees ran off it): the first row at row 2, one grid step between nodes,
  tie-in nodes after our rows; a truck's own upgrades on the left and its armour and weapon kits in three columns on the right (columns
  29, 34 and 39), the armoured cab in the far one. A tie-in node may hang from ours, never ours from a tie-in's;
  other authors' trees are left alone. **`python scripts/check-upgrade-trees.py`** (part of the gate) checks every tree, `show` prints
  them, and `--write` redraws the Mermaid diagram each tree's design record carries under its `<!-- upgrade-tree: DEFNAME -->` line.
- **A vehicle accessory uses up something real** (owner, 13 Nov 2026): battery and alternator power, fuel, water or goods from the game
  world. A store such as a water tank runs down as it is used and is refilled by the player's people, never by itself. Gear with nothing
  to plug in (an awning, a roof rack) costs its materials and its weight, with no wear or upkeep (`docs/development/vehicle-accessories.md`).
- **No drag or aerodynamics** (owner, 13 Nov 2026: "not something that can be accurately modelled in Rimworld without large assumptions"):
  nothing of ours changes a vehicle's speed or fuel use through air resistance. **One exception** (owner, 18 Nov 2026): the drivetrain
  model keeps its one fixed drag figure per vehicle class; no fitting, accessory, load or upgrade ever changes it. **A second exception**
  (owner, 19 Nov 2026): an aircraft's cruising height between tiles sets its speed and fuel by the air's density at the height flown
  (`CruiseRule`); height alone moves them, never a fitting, accessory, load or upgrade. **A third exception** (owner's card, 23 Nov
  2026): a twin jet held straight against lopsided thrust burns more fuel (`AirCombatRule.LopsidedThrustFuel`), moved only by its
  engines' outputs and its loadout's yaw figure, never a fitting, accessory, load or upgrade.
- **An armour or weapon kit's weight is carried mass** (owner, 19 Nov 2026: "weight counts like cargo"): its kilograms take cargo room
  and load the engine as the same weight of cargo would. Never through Vehicle Framework's mass stat, which slows nothing, and still
  no drag. Plain accessories keep their rule: their weight takes cargo room only (`docs/development/vehicle-militarised-service-vehicles.md`).
- **Our service trucks may be armed by upgrade, by a choice of mounts** (owner, 19 Nov 2026, replacing "our own vehicles carry no
  weapons" for them): our own guns first, an installed mod's gun as an optional extra mount, and no hard dependency for any of it.
  A machine that is unarmed in reality still stays unarmed (owner, 18 Nov 2026); real service trucks were armed with such kits.
- **Design every vehicle mechanic with other factions' vehicles in mind** (owner, 11 Nov 2026): raiders who arrive in vehicles and, at the very
  least, the vehicles standing at a base the player attacks. Each vehicle design record has a section saying what the mechanic does on a vehicle
  in a raid on us, on one parked at a base we attack, and on one we capture; it works on any faction's vehicle unless the record says why not,
  and costs little with several on a map. How other factions come to have vehicles is in `docs/development/vehicle-framework-mechanics.md`;
  `docs/development/vehicle-other-factions-audit.md` stands in for the section in older records until each is next touched.
- **Design every submarine mechanic for enemy submarines** (owner, 10 Nov 2026: coming "not later, but very soon"). Each mechanic works for any
  faction's boat, costs little per hull with several hostile ones on a map, and never shows the player what their own crew could not know.
  Submarines get no walkable gravship-style interior, because of its performance cost; our own interfaces and mechanics stand in for it.
  `docs/development/submarine-framework.md` lists what already works for a hostile boat and what is missing.
- **Vehicle parts are ours, and other mods' are substitutes** (owner, 9 Nov 2026, reversing "providers first"): our part items always
  exist in D'thorga Framework and set what a repair costs, whichever mods are installed; RimThunder's and Vanilla Vehicles Expanded's
  are accepted only where they make sense, named per role in `PhobosVehiclePartSupplyDef` with `MayRequire`, never in C#. The framework
  is meant to grow into tuning and upgrades (`docs/development/vehicle-health-design.md`).

## Moved here from the root AGENTS.md (21 Nov 2026)

- **Submarine work is on the back burner too, and out of view** (owner, 20 Nov 2026: "put our submarine development on the backbench
  and even partially retire it out of view for now"): no new submarine features or reviews until the owner picks it up again. The
  Seabed Crawler is off the build menu and its parts off the bench, and its research tab is out of view in a colony that never started
  it ("visible for colonies with pre-existing data"). Nothing is removed: a boat already built keeps working, and a fix to something
  broken is still fine when the owner reports it.
