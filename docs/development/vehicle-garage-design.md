# The vehicle garage and fleet tools: design (8 Nov 2026)

**Status: design record. Nothing here is tested in game.** Findings marked "from the code" were read from local decompiles
of Vanilla Expanded Framework's 1.7 build and Vehicle Framework's 1.6 build (the one the 1.7 game loads), and are summarised
in our own words. Where this record says something will happen in play, that is an inference until the owner has seen it.

The owner asked on 8 November 2026 for a garage built on the outposts system, because the player will now be dealing with a
lot more vehicles, and for other ways of handling a large fleet.

## Decisions and who made them

| Decision | Made by |
| --- | --- |
| A garage built on the outposts system | Owner |
| First version: parking and slow repairs | Owner |
| Also build: fleet board, parked-vehicle warning, standing crews and convoys, home depot bays | Owner |
| The garage needs Vanilla Expanded Framework and Vehicle Framework, not Vanilla Outposts Expanded itself | Agent default |
| Its code is a Framework tie-in; its Def and balance are in D'thorga Vehicles | Agent default |
| A garage parks a whole convoy at a time (split the convoy first to park some of it) | Agent default, from the code |
| Bays, repair rate and material costs | Agent-authored balance, in XML, for the owner to tune |
| Patch Vanilla Expanded Framework's convoy fault in D'thorga Patches as possible loss of colony data, with a setting for the original behaviour that explains why it is there | Owner, 8 Nov 2026 |

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Refusing convoys at the "create outpost" window | Keep the original behaviour through a setting, for anyone who wants it | Found an outpost from a caravan with vehicles (by default) | Vehicles are left somewhere first, and the outpost is founded on foot; the garage has its own button for convoys |
| Garage: parking | Keep vehicles off the map with no time limit, wherever they found a garage | Use a parked vehicle until a caravan goes to collect it. Spare a colonist: someone must stay. Pack up while vehicles are inside | Spare vehicles become a stock held at chosen places, so where to base them is a world-map decision, instead of clutter at home or a deadline in the wild |
| Garage: slow repairs | Leave damaged vehicles to be mended without tying up the home colony's builders | Repair without stores: no steel or components, no repairs. Fix the condition layer's conditions, which still need on-map service | A battered convoy can drop vehicles at a forward garage and carry on, and stocking that garage becomes a job of its own |
| Parked-vehicle warning | Go back for a vehicle before it is deleted, knowing which one and where | Nothing | Leaving a vehicle in the wild becomes a choice with a visible deadline, rather than a loss found out later |
| Fleet board | See every vehicle in one list wherever it is, sort it, and jump to one | Nothing | The question "what state is my fleet in?" is answered from one window, instead of by clicking through each convoy and map |
| Standing crews and convoys | Board a vehicle's regular crew with one button, and form a saved convoy again in one click | Nothing: a crew member stays free for other work until called | Vehicles get regular crews and convoys become named units, instead of being assembled by hand every time |
| Home depot bays | Assign a vehicle a bay where it is refuelled and serviced | Park two vehicles in one bay; a bay's service reaches only what is in it | Vehicle parking and supply become part of base layout, instead of vehicles stopping wherever they were last used |

## What the outposts system gives us

The outposts engine is `Outposts.dll`, which ships inside
[Vanilla Expanded Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2023507013) (Oskar Potocki and team). The
outposts players build come from content mods such as
[Vanilla Outposts Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2688941031) (legodude17, Oskar Potocki).

From the code:

- Any `WorldObjectDef` whose class derives from `Outposts.Outpost` is picked up automatically (`OutpostsMod.FindOutposts`).
- `OutpostBase` is an abstract parent Def in the framework, so a new outpost is one Def and one class.
- `Outpost` has virtual `GetCaravanGizmos`, `GetGizmos`, `Produce`, `ProductionString`, `PostRemove` and `ExposeData`. The garage needs no Harmony patch.
- `OutpostExtension` can be subclassed, as Vanilla Outposts Expanded's mining outpost does, so our own figures can live in XML.

## Hazards, from the code

| Hazard | Where | What we do |
| --- | --- | --- |
| Outposts refuse vehicles | `Outpost.AddPawn`, `Outpost.cs:326-333` | The garage holds vehicles itself and never passes them to `AddPawn` |
| That refusal calls `RemoveAllPawns` on the vehicle, which Vehicle Framework's vehicles do not have | `Outpost.cs:332` | Expected to throw if a convoy with someone walking uses the normal "create outpost" dialog. D'thorga Patches 0.15.0 refuses it, with a setting; report below |
| A caravan is destroyed once no human is left in its list, and crew aboard vehicles are not in that list | `Outpost.cs:359` | The same refusal and report. The garage's own founding route unloads the crew first |
| A convoy with everyone aboard counts as no colonists in the normal dialog | `Utils.HumanColonists` reads the caravan's list only | The garage has its own button on a convoy |
| An outpost with nobody in it deletes itself | `Outpost.Tick`, `Outpost.cs:214-218` | A garage must be staffed; vehicles are moved out before it goes |
| Vehicle Framework's parked vehicles are deleted after 15 to 30 days, by vehicle size | `StashedVehicle.Create`, `StashedVehicle.Destroy` | This is the garage's main value. Part 4 warns before it happens |

Vanilla Outposts Expanded's comments were read on 8 November 2026 (50 of 2,325): one player reported outposts abandoned
instantly on 30 October 2026, and one reported a save freeze from a body left in an outpost in June 2026. The uploader had not
replied in those 50. The first report fits the second hazard above, but nothing shows that player had vehicles; that link is a guess.

## The garage

**A staffed outpost that holds vehicles with no time limit, and slowly repairs them.**

- **Founding from a convoy:** a button on the convoy, with a window to choose who stays. It makes the same checks as the framework's own dialog.
- **Founding on foot:** the framework's normal dialog also lists the garage, and works as for any outpost.
- **Parking:** Vehicle Framework's `StashedVehicle.Create` unloads the crew and cargo rules safely. The vehicles then move from that stash into the garage and the empty stash is removed, so its timer never matters. The crew carries on as a caravan on foot.
- **Taking vehicles out:** a caravan at the garage gets a button and chooses which vehicles, following the steps of `StashedVehicle.Notify_CaravanArrived`.
- **Repairs:** every few days the stationed people's Construction skill gives repair points, spent on parts through Vehicle Framework's own `HealComponent`, so its part health stays the one authority. Each point costs steel and components from the garage's stores. Inspect text names the one thing it is waiting for.
- **Frozen while parked (gameplay simplification):** a parked vehicle is not ticked, so fuel, batteries and conditions hold still and it costs nothing per tick.
- **Closing:** pack-up is refused with a reason while vehicles are inside. If the last person dies or leaves, every vehicle goes into an ordinary Vehicle Framework parked stash and a letter says so. The player's vehicles are never deleted silently.

Limits known now: repairs do not clear the condition layer's conditions (that stays with on-map service), and aircraft and anything that cannot form a caravan are refused.

## As built (Framework 0.31.0, Vehicles 0.11.0; not yet tested in game)

- `Outpost_PhobosGarage` holds vehicles in a saved `ThingOwner` and `GarageService` holds every rule. The rules for bays and repair budgets are game-free, in `GarageRule`, with tests.
- The button icons are the outposts code's, found by path: its `TexOutposts` fields can't be read from our 1.7 build because their Unity type comes from another assembly.
- Stores are spent one stack at a time, because the outposts code's `TakeItems` goes wrong when a request spans several stacks (from the code, `Outpost.cs:153`).
- A convoy at a garage doesn't get the outposts code's "add a person" button, which lists vehicles and fails on them. Its crew can join once on foot.
- **Removal:** a garage and the vehicles inside it go with the mod, so the README says to empty them first.
- **Not settled by reading the code:** whether the convoy's button shows on the world map, and whether a vehicle saved inside a garage comes back whole. These are the owner's first checks.

## Fleet tools

| Tool | What it does | Uncertain |
| --- | --- | --- |
| Parked-vehicle warning | A letter a set number of days before Vehicle Framework deletes a parked vehicle | Little |
| Fleet board | One window of every vehicle wherever it is, with fuel, worst condition, service due and crew; sort, filter, jump | Cost while open, to be measured |
| Standing crews and convoys | A saved crew per vehicle and seat, a "Crew up" button, named convoys formed again in one click | Whether Vehicle Framework's form-caravan dialog can be pre-filled |
| Home depot bays | A building vehicles are assigned to; refuels and services what is parked in it | Making idle vehicles drive back unprompted |

## Ideas kept for later

- **Callsigns and House D'thorga liveries**, tied to the radio framework and Vehicle Framework's paint job.
- **Organised units**: lances of four, named in the owner's `D'thorga's <Outpost> Squad #1` pattern.
- **Garages that send a vehicle home** on request, by caravan.
- **The tanker as a mobile fuel depot** for a garage or convoy.
- **Mothballing for performance**: each vehicle on a map ticks its comps every tick (`VehiclePawn.Tick`), so parking the spares may help TPS. To be measured with the timing report, not assumed.
- **Surplus disposal**: selling or scrapping in bulk from the fleet board.

## A report for the owner to post (agents never post)

D'thorga Patches guards this for our players; the report would fix it for everyone. To Vanilla Expanded Framework, about its
outposts code. **Not reproduced in game; found by reading the code.**

> Founding an outpost from a Vehicle Framework vehicle caravan looks unsafe. In `Outpost.AddPawn`, a vehicle is handled by calling `RemoveAllPawns` on it through reflection, but Vehicle Framework's `VehiclePawn` has no method of that name in its current build, so the call would throw a null reference. The outpost has already been added to the world by then, so it could end up empty and be abandoned on the next tick. Separately, once no human is left in the caravan's own list, the caravan is destroyed; crew riding in vehicles are not in that list. It may be worth refusing the "create outpost" button for a caravan that holds vehicles.
