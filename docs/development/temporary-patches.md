# Patches for other authors' faults (register, 10 Nov 2026)

Every Phobos patch that exists because of a fault in someone else's mod: whose fault it is, what would make the patch unnecessary, how to check, and whether the author knows. The owner asked for this on 10 Nov 2026 so that nothing temporary quietly becomes permanent.

## Read this first

- **Twelve entries are marked temporary, four permanent.** Every marking is the agent's, for the owner to overrule. Only the first two were called temporary by the owner.
- **"Temporary" means it should come out when the author fixes the fault.** "Permanent" means our own design needs it, whatever the author does.
- **Before removing one, check the fault is really gone** in the mod's new build (`python scripts/query-code.py compare OLD.dll NEW.dll`, or read the method named below). A new update date alone proves nothing.
- **"Drafted" means the author does not have it.** The owner confirmed on 10 Nov 2026 that only the Prison Labor report has been posted, and that they post reports only for dire faults. So these patches may be needed for a long time unless an author finds the fault on their own.
- **Author activity**, from `python scripts/workshop-research.py report` on 10 Nov 2026, with Vehicle Framework from its GitHub. Re-run it before relying on these.

| Mod | Last updated | Declares 1.7 | Uploader's last reply seen |
| --- | --- | --- | --- |
| Prison Labor | 9 Nov 2026 | Yes (first 1.7 build) | None in the comments read; the author commented 6 Nov |
| Vehicle Framework | `develop` pushed 31 Oct 2026; Workshop 21 Oct 2025 | `develop` yes, Workshop no | Comments closed |
| Visit Settlements | 22 Oct 2026 | Yes | 1 Nov 2026 |
| RimPacts | 7 Nov 2026 | Yes | 8 Nov 2026 |
| Harrier VTOL Attack Aircraft | 9 Nov 2026 | Yes | 1 Nov 2026 |
| InterRim Ballistic Missile | 30 Jun 2026 | **No** | None in the comments read |
| Vanilla Expanded Framework (Pipe System) | 6 Nov 2026 | Yes | None in the comments read |
| Vanilla Furniture Expanded - Factory | 14 Oct 2026 | Yes | 6 Nov 2026 |

More Injuries and Medical Surgery Expanded 2 were not re-checked for this register.

## Temporary: comes out when the author fixes it

| # | Patch (mod, class) | Whose fault, and what | It can go when | Stands down by itself? | Report |
| --- | --- | --- | --- | --- | --- |
| 1 | Patches 0.16.5, `PrisonLaborFoodGuard` | Prison Labor: a food postfix calls the method it patches, and the game closes | `GetFoodRestrictionPatch.Postfix` no longer calls `GetCurrentRespectedRestriction` | **Yes**, and the load line says so | **Posted by the owner**, 10 Nov 2026. [Steps](crash-2026-11-09-stack-overflow.md) |
| 2 | Patches 0.16.6, `VehicleFires` | Vehicle Framework: attached fires are left behind on leaving a map, and only the first is ever put out | `VehiclePawn.DeSpawn` puts fires out and a vehicle keeps one | No | Drafted. [Steps](vehicle-framework-fires.md) |
| 3 | Patches, `VisitMapStaysOpen` | Visit Settlements: its downing postfix calls `DeinitializeMap` directly, skipping every rule that keeps a map open | That path goes through the game's map removal rules | Partly: off if its code has changed | Drafted, [missile patches](missile-patches.md) |
| 4 | Patches, `SettlementEventsGuard` | Visit Settlements: the food sum reads the food need of pawns that do not eat, and throws every tick | The sum skips pawns with no food need | Partly: off if its code has changed | Drafted, [missile patches](missile-patches.md) |
| 5 | Patches, `RimPactsArrivalFilter` | RimPacts: a postfix in a generic class on `LaunchProtocol.GetArrivalOptions`, which breaks when the next mod patches that method | RimPacts patches it from a non-generic class | No | Drafted, [vehicle patches](vehicle-patches.md) |
| 6 | Patches, `RearmOneKind` | Harrier, Hind and Mongoose (one author's shared rearm code): with no kind chosen, a colonist fetches any kind, and a different kind arriving throws the load out again | Rearming holds one kind per magazine | Partly: per aircraft, if its code is found | None found |
| 7 | Patches, `Patch_ApplyStrategicDamage_Spread`, `MarkBlast` | InterRim Ballistic Missile: the strategic pass runs over a whole map in one frame | The mod paces it. **Its project looks dormant and has no 1.7 build, so this may be permanent in practice** | No; a setting turns it off | **Held**: the cause was revised, see [missile patches](missile-patches.md) |
| 8 | Factory Additions, `BillRepair` and its tick and reset patches | Pipe System: a bill's ingredient owners are built once and never compared with the recipe again | A saved bill is rebuilt when its recipe changes | No | Drafted, [factory additions](vfe-factory-additions.md) |
| 9 | Factory Additions, `AddIngredientGuardPatch` | Pipe System: an item no owner can take throws and ends the hopper scan | `AddIngredient` skips such an item | No | Drafted, same record |
| 10 | Factory Additions, `BeingFilledClearPatch` | Pipe System: the hopper scan leaves an owner marked as being filled for good | The scan unmarks the owner it marked | No | Drafted, same record |
| 11 | Factory Additions 0.12.1, `GhostBillsPatch` | Pipe System: a bill whose recipe no longer exists loads with no recipe, and everything reading it throws | Such a bill is dropped on load | No | None yet |
| 12 | Factory Additions, `ConveyorTickPatch` | Vanilla Furniture Expanded - Factory: a belt that is empty and Waiting returns at once and never moves again | A belt's tick wakes itself from that state | No | None found |

## Permanent: our own design needs it

| Patch | Why it stays |
| --- | --- |
| Framework, `PartsOnLoad` | Vehicle Framework saves parts by position, and we append parts to its vehicles. We need this for as long as we append |
| Framework, `FlyingAircraftStayAwakePatch` | The game's six-hourly sweep is working as designed; our simulation needs aircraft kept awake in flight |
| Patches, `AirborneCrewKeepsMap` | Sorties need a map kept open around crew inside a vehicle. **Could become temporary** if Vehicle Framework's own check starts covering crew in the air |
| Patches, `ConvoyOutpostRefusal` | Has a player setting, and refuses founding that can lose vehicles (read from the code, not reproduced). **Could become temporary** if Vehicle Expanded's outposts learn to handle vehicles |

Medical's save bridge is mixed: re-numbering body parts is permanent, because mods really do change bodies, while the More Injuries first-load wipe guard is a candidate for this list. Its reports are drafted in [the save bridge record](medical-save-bridge.md).

## Left out on purpose

Patches that add features or integrate mods, not fix faults: the factory belts' storage access and bench bills, the Harrier's hover and flight gates, the survey truck counting as a scanner, Rimefeller and Dubs tie-ins, IRBM's strike model and quiet reveals, balance changes such as the Harrier's costs, and compatibility patches for Combat Extended and RimThunder. Each of these does something the other mod never set out to do, so there is nothing for its author to fix.

## How to use this

- **A new patch for another author's fault gets a row here in the same change** (`AGENTS.md`).
- **When a listed mod updates**, check its row. The outstanding-work items J2 and J3 track the two the owner called temporary.
- **To remove one:** confirm the fault is gone, delete the patch, take a patch-number version with a changelog entry, and delete the row.

## Who chose what

- **Owner (10 Nov 2026):** that this register, the standing rule and the sweep should exist; that the Prison Labor guard is strictly temporary; that the Vehicle Framework fires are patched until its author handles them.
- **Agent:** every temporary or permanent marking, the removal conditions, and what was left out. The list comes from `python scripts/query-patches.py mod` for each of our mods and the opening comment of each patch class; patches were not all read in full.
