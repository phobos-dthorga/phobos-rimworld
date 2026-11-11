# Home depot bays: design (9 Nov 2026)

**Status: built in D'thorga Framework 0.45.0. Not yet tested in game.** Standing crews and convoys are not built, by the owner's decision: **Crew Presets** (Luke, `luke.crewpresets`, on the owner's list, updated 27 Oct 2026; 4 comments read) already saves groups of colonists in the form-caravan window and others, and "that kind of thing should be responsible for this, not us" (owner, 9 Nov 2026). It picks people, not exact vehicle seats.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| One vehicle, one bay | Give each vehicle a home | Two vehicles sharing a bay | Vehicle parking becomes part of base layout |
| Go to bay | Send a vehicle home in one click | Nothing; it refuses with a reason without a driver, a route or a clear need | Vehicles come home instead of stopping wherever they were last used |
| The bay refuels what is parked on it | Top up with no colonist | Nothing, but only its own vehicle, and only while it has power and fuel | Fuel goes to the bay once rather than to each vehicle |
| Faster service in the bay | Get the same job done in 70% of the work | Nothing | Home is the sensible place to do repairs |
| A shelf for spare parts | Keep the parts next to the vehicle, so haulers have a short walk | Nothing | Parts are stocked where they are used |

## How it works

- **The bay is a 2 x 1 building, the pad an outline** 3 cells wide and 10 long in front of it, shown while placing or selecting. The pad blocks nothing, and a vehicle is "in its bay" when its centre is on the pad. It covers our largest truck; a smaller one sits inside it.
- **The vehicle goes when you say so, and only then** (owner: auto-return was not chosen). The order drafts it and starts Vehicle Framework's own path; on arrival it switches off.
- **Parking direction is not controlled:** a truck may arrive sideways across the pad. Nothing depends on it.
- **The pump** moves 30 chemfuel an hour from the bay's own tank (200, filled by colonists the usual way). Electric vehicles and vehicles on another fuel are not refuelled, and the inspect pane says so.
- **Faster work** applies to our service, repair and fitting jobs by shortening the work total. Vehicle Framework's own free repair of damaged parts is untouched.
- **The shelf** is an ordinary storage building set to Important and to our vehicle parts and components. It changes nothing about how parts reach a vehicle; it only keeps them close.

## Cost and risk

- A vehicle looks up its bay on the rare tick, reading the map's few bays. "Not yet tested in game" applies to all of it.
- Saves: safe to add. The assignment is held by the bay, so removing a bay simply leaves the vehicle without one.

## Who chose what

| Choice | Who |
| --- | --- |
| A "Go to bay" button, self-refuelling, faster service; no auto-return; crews and convoys left to Crew Presets | Owner, 9 Nov 2026 |
| A shelf for the parts, the 2 x 1 building with an outlined pad, one vehicle to a bay | Agent, from the owner's choice of "holds its spare parts" |
| Pad 3 by 10, pump 30 an hour, tank 200, work at 70%, 100 W, 80 steel and 3 components | Agent, authored |
| A fuel tank and a fuel pump as parts beside that cost (Framework 0.95.0, [parts bills](parts-bills.md)) | Agent's proposal, approved by the owner with the plan, 11 Nov 2026 |
| Pad 3 by 6 against the bay, vehicles parked nose to the bay (Framework 0.94.2) | Owner asked for the pad "brought back a little", 11 Nov 2026; the length and parking spot are the agent's |
