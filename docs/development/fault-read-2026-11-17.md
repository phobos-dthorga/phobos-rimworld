# Fault-read of the untested vehicle work (17 Nov 2026)

**Status (17 Nov 2026):** a reading only; nothing is fixed yet. 36 findings listed below (13 aircraft, 6 grid, 7 knocked-out, 10 trailers), each with its place and a fix to make;
the fixes wait for the owner's go.

The owner asked for analysis while quota allowed (17 Nov 2026) and chose this as the first job: read the vehicle code built since
12 November, none of it tested in game, for faults before the owner meets them in play. Four independent reads, one per area:
the aircraft batch (Framework 0.119.0 to 0.120.2), the knocked-out systems (0.115.0 to 0.117.2), the trailers, hitch and winch (0.101.0 to
0.111.0), and the grid hook-up, turret power and lights (0.119.1 to 0.120.4). Each read the game's, Vehicle Framework's and Combat
Extended's code with `scripts/query-code.py` and `scripts/query-patches.py`; nothing was built, run or edited.

**How sure:** every finding marked **[checked]** was confirmed again by the session that commissioned the reads, by reading both sides
(ours and the other code) on 17 Nov 2026. The rest are the readers' own confirmations by reading. **None of this is observed in game.**
Line numbers are from the day.

## Read this first

- **Combat Extended changes the picture most.** It replaces Vehicle Framework's damage step outright (its `VehicleArmorPatch`, on by
  default and at its default in the owner's settings). That alone makes A2 (the Harrier cannot be brought down by air defence) and K1 (a
  weapon never stops when its mount is shot out) true in the owner's game.
- **The worst for a save:** H1 (a bowser destroyed as it arrives from the north or east edge), H2 (parked trailers turn on every load),
  G1 (a stopped autonomous turret never fires again until the save is reloaded).
- **Test rows these findings change:** 122 (crews bail out), 139 to 146 (the aircraft batch), 149 and 150 (grid and autonomous turret),
  105 to 107 (trailers), 64 and 68 (recovery tow). Testing them before the fixes would mostly find these faults again.

## A: the aircraft batch

| # | Fault | Where | Fix |
| --- | --- | --- | --- |
| A1 | **[checked]** DMS sees our aircraft at the start of its current leg, not where it is: `AerialVehicleInFlight.position` is the leg's start; the true place is `DrawPos`, a slerp along the leg (`VF:96620-96632`, `:96876-96881`). A radar at the destination never sees it; one near the take-off point sees it all flight | `AircraftTrack.PositionAt` (`Vehicles.Dmse/AircraftTrack.cs:79`) | Start from `DrawPos` |
| A2 | **[checked]** Under Combat Extended an interceptor's hit does no damage: our penetration 0.5 is on the vanilla scale, CE's `DamageArmor` deflects blunt damage when penetration is under half the armour (`CE-VehiclesCompat:621-643`), and the Harrier's parts are Blunt 4 to 8 under CE. Every hit says "still flying"; hypoxia never comes | `AirCombat.cs:169`, `AirCombat.xml:16-19` | A CE figure beside the vanilla one, chosen when CE's armour patch is on; authored, from CE's own warheads |
| A3 | **[checked]** Height is a tenth of cruise for a direct flight: Vehicle Framework's `TotalDistance` leaves out the leg from the aircraft to its first node (`VF:84500-84516`), so it is 0 on a one-node route | `AircraftTrack.AltitudeAt` (`:99`) | Measure flown distance ourselves (take-off tile to `DrawPos`) |
| A4 | **[checked]** Passing a waypoint voids every interceptor already in the air: the route hash counts the nodes left, and Vehicle Framework pops one at each waypoint; DMS then reports "evaded" without a roll | `AircraftTrack.RouteRevision` (`:62-72`) | Hash the destination and the order's identity, not the nodes left |
| A5 | The Harrier's chaff and flares button is refused in an attack run: `AttackRun` and `RunwayLanding` are not in the airborne phases; the phase is also sampled only on the rare tick | `Conditions.cs:268-272`, `Rules_VehicleSystems.xml:155-161` | Add both phases; read the phase live for the button |
| A6 | Another faction's warning receiver counts as broken until its systems are first built (a hit or the player selecting it), so its dodge is cut to 0.75 until then | `AirCombat.Works` (`:37-38`) | Build the states on first read, or treat unbuilt as working |
| A7 | The track cache is static and outlives a load; a flight re-launched after a reload may get a known ID and wake DMS up to 2,000 ticks late | `AircraftTrack.cs:132-151` | Clear on game load |
| A8 | Hand-cranked sway under CE does nothing for a weapon whose sway is 0 (multiplicative) | `AirCombatRule.cs:119-120` | An added minimum as well as the factor |
| A9 | "On the move" for aim lags up to a rare tick | `TurretPace.cs:103` | Read the pather live |
| A10 | A second hit in one tick reads stale oxygen | `AirCombat.cs:174` | Read after forcing an update |
| A11 | No launch warning when an aircraft takes off from a caravan (`VehicleCaravan.Launch`, `VF:93442`); "from" is the last waypoint, not the position | `LaunchChecks` prefixes | Add the caravan path; use `DrawPos` |
| A12 | Recon flights fly at a fixed 5 (`VF:96908-96926`); our track uses the launcher's speed | `AircraftTrack.FlightSpeed` | Follow Vehicle Framework's recon speed |
| A13 | "ten seconds" hard-coded in `PhobosAirCombat_SalvoGizmoDesc` while the figure is a Def's; cartridges spent silently when the receiver is out | language file; `AirCombat.cs:124-125` | A placeholder from the Def; a message |

## G: the grid hook-up, turret power and lights

| # | Fault | Where | Fix |
| --- | --- | --- | --- |
| G1 | **[checked]** A stopped autonomous turret keeps its target, so its drive keeps drawing and, once power is back, it never fires again: Vehicle Framework clears a target only while the turret works (`TurretTargetValid`, `VF:67456-67469`) and scans only when it has none (`VF:68261`) | `TurretPace.Crewed` (`:287`), `TurretPowerStop` | Clear the target when we stop it; count an autonomous turret as manned only while it is not stopped |
| G2 | **[checked]** Every hidden hook-up piece is a power consumer that starts off, so parking shows the "needs power" bolt on every cell and plays a click as each comes on (the game's `showPowerNeededIfOff` defaults to true) | `PowerHookUp.xml:34-41` | `showPowerNeededIfOff` false |
| G3 | **[checked]** The battery charges from "shore power" whether or not the meter is drawing: `ShorePowered` asks only for live power within a cell, not the meter's net or the meter's state. Free charge with a battery-less grid short of surplus, a brown-out, a vehicle beside an appliance, or no piece laid; the block heater likewise | `CompPhobosVehicleSystems.ShorePowered` (`:1623-1650`) | Shore power from the hook-up means the meter is on and its net has power; the service lead stays as it is |
| G4 | A set-up vehicle with its engine running is billed by the grid for loads its alternator carries, and for charge it does not take | `UpdateHookUp`, `GridCarriesLoads` | Neither while the engine runs |
| G5 | A flat battery on a live grid still stops the autonomous turret, against the changelog | `TurretPace.cs:211-217` | Powered when the grid carries the loads |
| G6 | `GridWatts` is read by nothing; a headlight whose beam cell is off the map stays out until the next update | `CompPhobosVehicleSystems.cs:1593`, `VehicleLight.cs:53-55` | Show it on the inspect line or remove it; retry the light |

## K: knocked out, not destroyed

| # | Fault | Where | Fix |
| --- | --- | --- | --- |
| K1 | **[checked]** Under Combat Extended a weapon never stops when its mount is shot out: the mount requirement re-checks only on `HealthChanged`, and CE's damage raises only `DamageTaken` (`CE-VehiclesCompat:553-583`). Raiders' crews then never bail out of a dead hull either, and the seen-in-play note fires though the weapon did not stop | `WeaponMounts.cs:17-20, 51-57` | Re-check on `DamageTaken` too |
| K2 | A thrown track does not stop a tracked vehicle with a speed upgrade: Vehicle Framework adds upgrade offsets after the stat parts (`VF:10330-10348`), so 0 + 1.0 drives on (Vanilla Vehicles Expanded's Bulldog) | `Patches.cs:152` | Stop it after offsets (a postfix on the final value, or the stopped test reading the part) |
| K3 | An overloaded truck (over 124 % cargo) reads as knocked out: "Crew in danger" alert and "has stopped" message | `Mobility.cs:48, 52-77`; `CrewBailOut.cs:25-26` | "Stopped by damage" asks for a damaged part that does it |
| K4 | The electric tanker pump stalls on an empty fuel tank, against its own description | `CompPhobosSetUp.cs:378` | Ask `NeedsEngineNow` before the fuel check |
| K5 | The listening array's "manned" flag survives leaving the map (5 A counted off-map) | `CompPhobosListening.cs` | Clear it on despawn |
| K6 | A field re-fit flag is cleared only by a replacement, so a part healed by free repair can later be listed for replacement | `VehicleRepairs.cs` | Clear it when the part reaches full health |
| K7 | Latent: the traverse finalizer's "nothing to restore" marker is 0, not NaN, if our prefix never ran; and Vehicle Framework's `IsDisabled` loops forever on a turret three deep (`VF:68091`), which our mount requirement makes reachable. Neither is reachable on the owner's list | `TurretPacePatches.cs:46-60`; `ITab_VehicleSystems.Knocked.cs:147` | Guard both; a report for Vehicle Framework's author |

## H: trailers, hitch and winch

| # | Fault | Where | Fix |
| --- | --- | --- | --- |
| H1 | **[checked]** Even-length vehicles facing south or west are checked against the wrong footprint: Vehicle Framework treats south as north and west as east (`VF:71592-71614`), ours uses the game's `GenAdj.OccupiedRect`. A 3 x 8 bowser arriving from the north or east edge can be set down where the game then destroys it during spawn | `TowTravel.cs:160`, `CompPhobosHitch.cs:328`, `Recovery.cs:258` | Vehicle Framework's own `VehicleRect` in all three |
| H2 | A vehicle moved by the hitch or winch keeps its old pather cell, so Vehicle Framework turns it toward its old place on every load (`VF:16891`, `:18711-18755`) | `CompPhobosHitch.cs:332-333`, `Recovery.cs:262` | Reset the pather after each move (`ResetToCurrentPosition`) |
| H3 | After an edge arrival, the trailer set down beside the tractor is snapped to a pose off the map, and the tractor is stopped dead each time it moves | `TowTravel.cs:113-120`, `CompPhobosHitch.cs:288-296` | Set down only where the follow rule's pose fits; otherwise unhitch with a message |
| H4 | A hitched trailer also ticked in the caravan dialog ends up in the hitch's holder and among the world's pawns, saved twice; after a load mid-journey the hitch may let go and the held copy is lost | `CompPhobosHitch.OnDeSpawn`, `TowTravel.TakeAlong` | Leave a hitched trailer out of the dialog, or unhitch it when chosen |
| H5 | A vehicle moved by the winch or tow still counts as parked: its pipes and conduits stay behind and new ones are laid on the way (can join separate grids) | `CompPhobosVehicleSystems.cs:1697-1698`, `CompPhobosTrailer.cs:71` | Not parked while towed or dragged; take pieces up on each move |
| H6 | The towed weight is 0 after loading a save made mid-journey | `CompPhobosHitch.cs:47, 226-240` | Recompute on load |
| H7 | The winch drags a vehicle set up for work | `Recovery.WinchRefusalFor` | The same set-up refusal as the hitch |
| H8 | A recovery-towed vehicle parked on a diagonal is drawn 45 degrees off when the tow faces east or west | `CompPhobosHitch.cs:332-339` | Clear its angle |
| H9 | Position is set before rotation, so the in-between footprint can leave the map near an edge | `CompPhobosHitch.cs:332-333` | Set rotation first, or check both |
| H10 | Small: a pending couple survives leaving the map; a failed spawn would drop the trailer; no retry gate while there is no room | `TowTravel.cs` | Clear on despawn; check the spawn; a gate |

## Tool gap found on the way

`scripts/query-patches.py who Thing.set_Position` reports no patches, but Vehicle Framework patches both setters through its own
`HarmonyPatcher.Patch` calls (`VF:51845-51846`). The tool reads attributes only, as its record says; patches made in code are a known
limit worth closing in phobos-modkit.
