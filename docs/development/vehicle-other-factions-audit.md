# Other factions' vehicles: an audit of the vehicle code (11 Nov 2026)

**A code read, not a game test.** The owner asked on 11 Nov 2026 for a retroactive pass over the vehicle code against their new rule:
design every vehicle mechanic with other factions' vehicles in mind, raiders and at least the vehicles at a base the player attacks
(AGENTS.md). Every check of the player's faction in `src/PhobosFramework.Vehicles*` was read: 66 lines. Line numbers are of the source
on 11 Nov 2026, commit a077974. Classifications and proposals are agent-authored; nothing was changed.

## Read this first

- **Three faults, each a small fix:** an AI aircraft can be refused lift-off by the player's own setting; a raider's aircraft sends the
  player lift messages and has its engines worn; deployed kit on a vehicle that is not the player's would place buildings that are.
- **The biggest gap is a design choice, not a fault:** other factions' vehicles run the lite tier (owner, 8 Nov 2026), so the
  drivetrain never holds them back. A raider's heavy truck does not slow in mud, climb hills slower or run hot. Every mechanic built
  on the drivetrain (the hitch included) does nothing to them.
- **Most checks are right:** 49 of the 66 keep buttons, menus, messages, letters, sounds, the fleet board and the debug tools to the
  player's own vehicles.
- **Already designed for other factions:** enemy fitting sets (Framework 0.41.0, owner's setting); part hits counted on every
  vehicle; another faction's vehicle never refuses to start, so a raid is never stranded (`CompPhobosVehicleSystems.cs:1350`); a
  captured vehicle wakes to the full model at its next rare tick (`:244`); and the submarine mechanics, which their own rule covers
  ([submarine framework](submarine-framework.md)).
- **27 of the 39 vehicle design records say nothing about other factions.** This record stands in for that section until each is next
  touched.
- **Decisions for the owner** are at the end.

## Faults

| # | What | Where | What happens | Proposed fix |
| --- | --- | --- | --- | --- |
| F1 | **The lift-off refusal applies to every faction** | `Service/HoverLimits.cs` `RefusesLaunch`, through `Service/Patches.cs:238-245` | With "refuse the pad, use the runway" chosen, an AI aircraft too heavy, too hot or with an engine out is refused lift-off by Vehicle Framework's own launch. That contradicts the rule the code already keeps for starting (`:1350`): another faction must never be stranded | Refuse only the player's aircraft |
| F2 | **The lift-off strain and its message apply to every faction** | `HoverLimits.OnLaunch`, registered on every vehicle with a turbine (`CompPhobosVehicleSystems.cs:459`) | A raider's overloaded aircraft lifting off tells the player it is beyond its lift, and wears its engines' hot section | Message only for the player's; keep the wear for every faction, so a hard-used raider aircraft is worn by it |
| F3 | **Deployed kit belongs to the player whoever deploys it** | `CompPhobosGeneratorSet.cs:146`, `CompPhobosRigWork.cs:185` | The cable end and the rig's power point are given the player's faction. Today only the player's buttons deploy, so it cannot happen; a captured-then-lost vehicle, a dev action or a later AI use would make it happen | Give them the vehicle's faction |

One more of the same kind, lower risk: `Powertrain.cs:410-414` sends the hover-beyond message with no faction check. It is reached
only on the full tier, so only with the "full model for everyone" setting on, when it would speak of raiders' aircraft. Same fix as F2.

## The lite tier and the drivetrain

From the code: other factions' vehicles on a map run the lite tier, where part hits are counted and nothing else evolves
(`CompPhobosVehicleSystems.cs:493-506`); the drivetrain samples only on the full tier (`:466`). So for a raider's vehicle:

| Mechanic | Player's vehicle | Raider's vehicle, or one at a base we attack |
| --- | --- | --- |
| Speed held back by power, gear and grip (mud, grade, load) | Yes | **No** |
| Engine heat, cooling, oil, wear, battery | Yes | **No** |
| Fuel used by load | Yes | **No** (Vehicle Framework's own fuel only) |
| Part damage from hits | Yes | Yes, counted, without consequences |
| Fittings and tunes | Yes | A fitting set by chance, which changes stats |
| Breakdowns and refusals to start | Yes | No, by design |

**Why it is so:** the owner chose the lite tier for cost on 8 Nov 2026 ([vehicle health](vehicle-health-design.md)), with the setting
"full model for everyone" for players who want it.

**What it costs the new rule:** a convoy raid moves as fast through a swamp as on a road, and the hitch design's "a raider's vehicle
towing is held back by the same model" is false while this stands.

**Proposed: a drive-only tier** between lite and full, agent-authored. Other factions' moving vehicles would take the drivetrain
sample, once a second while moving, so the speed cap and the mass reach them. Heat, wear and breakdowns would stay off, so a raid
is still never stranded. Nothing runs while they stand. **To measure first:** the sample's cost with several hostile vehicles moving,
with the dev-mode timing readout against the 0.05 ms per tick budget.

## Mechanics that skip other factions, judged

| Mechanic | Today | Raid on us | Base we attack | Captured | Judgement |
| --- | --- | --- | --- | --- | --- |
| **Sling** (`CompPhobosSling.cs:91`) | Lifts only the player's vehicles | n/a | Cannot lift a wrecked or abandoned vehicle off a base we took | Lifts as ours | **Design question:** allow an unattended vehicle of another faction with no crew aboard? It would make a raid's wreck or a base's truck salvage. Depends on how Vehicle Framework claims a vehicle (not yet read) |
| **Repairs, field fixes, servicing** (`VehicleRepairs.cs:62`) | Player's only | n/a | n/a | Yes | Right: nobody repairs another faction's vehicle for them |
| **Field laboratory** (`CompPhobosFieldLab.cs:74`) | Player's only | n/a | n/a | Yes | Right: research is the player's |
| **Listening array, sonar reports** (`CompPhobosListening.cs:144`, `CompPhobosSonar.cs`) | Reports to the player only | A raider's array hears nothing for anyone | Same | Yes | Right for reports. A raider's sonar ping is heard by the player's crew (`CompPhobosSonar.cs:217-221`), as the submarine rule asks |
| **Depot bays** (`CompPhobosVehicleSystems.cs:207`, `DepotBays.cs:137`) | Player's vehicles in player's bays | n/a | A bay at a base we attack does nothing | Yes | Right |
| **Pipes under a parked vehicle** (`:1312`) | Player's only | n/a | A parked tanker at a base does not join pipes | Yes | Right: nothing of ours pipes into another faction's network |
| **Drill rig's insect check, survey truck's deep scan** (`CompPhobosDrillRig.cs:298`, `CompPhobosSurveyTruck.cs:247`) | Look at the player's vehicles | n/a | n/a | Yes | Right |
| **Flying aircraft kept awake on the world map** (`Patches.cs:342`) | Player's only | An AI aircraft in flight on the world map may be put to sleep by the game | n/a | Yes | Probably right: VTOL Raids flies its own aircraft. Not checked further |
| **Fleet board, garage outposts** | Player's only | n/a | n/a | Yes | Right |

## Records without the section

These say nothing about other factions. Until each is next touched, this record is their section. The vehicle ones that matter most:
[drivetrain](vehicle-drivetrain-design.md) (the lite tier, above), [sling](vehicle-sling-design.md) (salvage, above),
[tipper](vehicle-tipper-hauler.md), [drill rig](vehicle-drill-rig.md), [generator set](vehicle-generator-set.md) (F3),
[survey truck](vehicle-survey-truck.md), [turbine](vehicle-turbine-design.md) (F1, F2), [electric](vehicle-electric-design.md),
[world map](vehicle-world-map-design.md), [depot bays](vehicle-depot-bays.md), [stations](vehicle-stations.md),
[utility vehicles](utility-vehicles-design.md). The rest are research, art and reference records, where the rule does not apply.

## Decisions for the owner

1. **The three faults:** fix them in one small Framework release?
2. **A drive-only tier** for other factions' moving vehicles, measured first?
3. **Sling salvage:** may a helicopter lift an unattended, uncrewed vehicle of another faction?

**Decided by the owner, 11 Nov 2026:**

1. **Fixed** in D'thorga Framework 0.96.1, with the hover-beyond message. Not yet tested in game.
2. **Keep the lite tier as it is, with no setting to change it,** and a vehicle the player takes from another faction moves to the
   player's own settings. Done in 0.96.1: the setting "Every faction's vehicles run the full model" is removed, and a captured
   vehicle's clock starts when it becomes the player's, so the time it spent as another faction's is not caught up at once. The
   drive-only tier is not built. The hitch design's line about a raider's towing vehicle being held back is therefore false.
3. **Salvage needs a claim first.** From the code, Vehicle Framework offers no claim order for another faction's vehicle: the only
   route found that makes one the player's is repairing a wrecked vehicle's building (`Vehicles.decompiled.cs:45539-45541`). The
   sling already lifts any vehicle that is the player's, so a claim order of our own is the missing piece.

**The claim order, built in D'thorga Framework 0.97.0 (owner, 11 Nov 2026: unattended, nobody aboard), not yet tested in game.**
A further read found Vehicle Framework does have a claim rule (`VehiclePawn.ClaimableBy`, `Vehicles.decompiled.cs:14472-14533`), but
the game's Claim order only takes Defs with claimable building settings (`ThingDef.Claimable`), which no vehicle Def has, so it never
runs.

| Question | Answer |
| --- | --- |
| What it lets the player do | Take a vehicle left at a base they attacked, a raider's abandoned truck, or a visitor's, and use it: drive it, sling it, strip it |
| What it stops them doing | Claiming one with anyone aboard, one whose owners' armed people stand within 12 cells, or a machine faction's |
| How it changes how they play | After a fight, clearing the guards is the way to the vehicles; a friendly faction's vehicle is a temptation with a goodwill price |
| Figures (authored, in the rules Def) | Half an hour plus 0.08 h a cell, at most six hours; 30 goodwill with a faction that was not hostile |
| Where | A right-click order for a selected colonist; `VehicleClaims` decides, the menu and the job delegate (`src/PhobosFramework.Vehicles/Jobs/ClaimVehicle.cs`) |

## Who chose what

- **Owner:** the rule (11 Nov 2026); the lite tier and its setting (8 Nov 2026); enemy fitting sets (9 Nov 2026).
- **Agent:** every classification and proposal here.
