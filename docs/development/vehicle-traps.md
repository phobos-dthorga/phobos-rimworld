# Traps and vehicles: your vehicles pass over your traps when you did not send them (21 Nov 2026)

**Status (21 Nov 2026):** built in D'thorga Framework 0.213.0, with its setting on by default. Not yet tested in game (test pass row 318).

**The owner, 21 Nov 2026:** "It looks like we might have to add a setting, enabled by default, that when vehicles are being driven
automatically by pawns (i.e. outside of the player's direct control/orders), that they are not to activate traps and instead, just ignore
them completely. I have been having this problem quite a bit otherwise." And: "We might have similar code for this already for air-based
vehicles" (we did: the aircraft rule of Framework 0.171.1, now decided in the same place).

How to read it: **[O]** is the owner's choice; **[A]** is the agent's choice, for the owner to revise; **[G]** is what the game's or
Vehicle Framework's code does, read with `scripts/query-code.py`.

## What the game does, and why vehicles kept setting traps off

- **[G]** A trap rolls its chance once, when a pawn first comes onto its cell (`Building_Trap.Tick`, then `CheckSpring`). A vehicle is a
  pawn, so the cell its centre stands on is checked as it drives.
- **[G]** A pawn of the trap's own faction "knows of" it and springs it 0.5% of the time per crossing (`Building_Trap.SpringChance`,
  `KnowerSpringChanceFactorSameFaction`). Colonists also path round known traps, which makes that rare in practice.
- **[G]** Vehicle Framework neither touches traps nor gives its own path grid the trap's path cost (no use of `Building_Trap`,
  `PathFindCostFor` or `PawnTrapSpringChance` in its source), so vehicles drive straight over trap lines. With errands driving vehicles
  through the colony's gates many times a day, 0.5% a crossing adds up.

## The rule

- **[O]** A vehicle of the player's on a drive the player did not order passes over every trap: nothing is rolled. A setting, on by
  default: **Vehicles you did not send pass over your traps**, in the vehicle systems' **Ground, weather and towing** section.
- **[A]** "A drive the player ordered" is read as: the vehicle is driving to (or has just reached) a cell the player sent it to. Every move
  order the player gives a vehicle, one or a group, a drag to set its facing, or a shift-click queued after another, ends in Vehicle
  Framework's `FloatMenuOptionProvider_OrderVehicle.PawnGotoAction`, where it is remembered. The job alone could not tell them apart: our
  errands issue the same ordered Goto job a right-click does (`VehicleAutoDrive.DriveTo`).
- **[A]** Everything else is not the player's order: our errands (bay trips, gathering and delivery runs), a trailer being towed (judged by
  its tractor's drive), a vehicle pushed by hand, a caravan setting off, and a vehicle standing still somewhere the player did not send it.
- **[A]** A drive the player ordered keeps the game's own rule (0.5% a crossing for a vehicle of the trap's faction), as the owner asked
  only about drives outside the player's orders.
- **[A]** Where the player sent a vehicle is derived state and never saved: after a load, a drive ordered before it passes over traps until
  the player's next order to that vehicle. This errs towards the setting's purpose, never towards a trap going off.
- An aircraft of ours in the air still passes over every trap, whatever the setting (Framework 0.171.1).

The rule is `TrapRule` (game-free, with tests); the service is `VehicleTraps`; the one patch on `Building_Trap.CheckSpring` is a prefix
that skips the original in the two cases, as VTOL Vehicle's and the Harrier's frameworks do for their aircraft, so it covers every kind of
trap, other mods' included. It runs only when a pawn first comes onto a trap's cell, so it costs nothing per tick.

## The four questions

- **What it lets the player do:** keep trap lines across the paths their vehicles take on errands, without losing vehicles, tyres or
  letters to their own traps.
- **What it stops them doing:** nothing they could do on purpose before; a vehicle can no longer be used to set off the colony's own
  traps by sending it on an errand.
- **How it changes the way they do things:** errands, bay trips and caravans no longer need trap-free lanes kept for them. The player's
  own driving still meets traps as before, so they still drive round their trap lines when they take the wheel.
- **What outside the vehicle notices:** the trap, which stays armed; no "trap sprung" letter comes. Nothing else, by choice: no person,
  faction or record takes note of a trap not going off.

## Risk

**[A] Grade: none added; it takes risk away.** Off, the player's vehicles meet traps as the game decides, as before. So by the shared rule
on settings by risk it is on by default, which is also the owner's word.

## Other factions' vehicles

- **A raid on us:** unchanged. A raider's vehicle does not know of our traps and meets them as the game decides; our traps still work
  against it.
- **A vehicle parked at a base we attack:** unchanged; the rule is only for the player's own vehicles.
- **A vehicle we capture:** it becomes the player's, and follows the rule from then on.
