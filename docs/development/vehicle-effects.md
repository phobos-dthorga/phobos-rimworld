# Vehicles' visual effects

**Status (26 Nov 2026):** slice 1 (the faults' signs) built in D'thorga Framework 0.321.0, with the shared emitter and the Vehicle effects
setting. Slice 2 (what moving vehicles and aircraft throw up) is next. Not yet tested in game (row 578).

The owner, 26 Nov 2026: "Is there any other FX we can apply to our vehicles and aircraft?" Before this, every vehicle system shared one
grey smoke puff (overheating, the boiler, the turbo), and most faults showed only by sound: a diesel knock, a brake squeal, a clutch
slip, a tyre burst, a failed start. The owner is hearing impaired, so a fault that is only heard is a fault they may miss.

## The owner's choices (question cards, 26 Nov 2026)

| Question | The owner's choice |
| --- | --- |
| Slice 1, the faults' signs | **All four**: coloured smoke; engine faults seen; brakes, clutch and tyres; sparks and drips |
| Slice 2, moving vehicles | **All four**: exhaust by load; wheel dust and spray; downwash and touchdowns; fading wheel tracks |
| The setting's start | **Full** (presets Off, Light, Full) |
| Slice 3 (beacons and lights, heat shimmer, flares) | **Later**, once slices 1 and 2 have been seen |

## How it works

- **One emitter for every map** (`MapComponent_PhobosFx`, the Framework core, so any of our mods can use it): every 15 ticks, on the map
  being looked at only, it makes the flecks of each thing on screen. A stage asks for a *rate* (puffs a game hour) and the emitter spreads
  it evenly (`FxRule.Count`), so a puff a minute never comes in a clump at each four-second systems update. Smoke and steam drift on the
  map's wind (`PhobosWind`). Nothing is saved; one pass makes at most 120 flecks.
- **The game's own flecks**, tinted (`instanceColor`) and placed on the vehicle along its heading: no new art. A vehicle's places come from
  its size: the exhaust at the back, the engine and the radiator toward the front, the wheels, underneath.
- **The setting, Vehicle effects** (Effects section): **Full** (the default), **Light** (faults in full; what moving vehicles throw up
  thinned to 40% and ruts fade sooner), **Off** (nothing from vehicles, the faults' signs included).

## Slice 1: the faults' signs

Smoke's colour says what burns (secondary: engine makers' and mechanics' diagnostic guides as commonly cited; no figure checked).

| Fault | What is seen | Where |
| --- | --- | --- |
| Running hot, overheating, boiling over | White steam (was grey smoke) | Radiator |
| The boiler blowing off | White steam | Body |
| A worn turbo (worn, failing, failed) | Blue-grey oil smoke (was grey) | Exhaust |
| High boost, on boost | A little black smoke | Exhaust |
| A clogged air filter, engine running | Black smoke | Exhaust |
| An engine worn below 40%, running | Blue-grey smoke, and a **burning oil** line on the Health tab | Exhaust |
| No oil pressure (the knock) | Dark puffs; sparks once the engine is below a quarter | Exhaust; engine |
| A cold start (below 5 C; below freezing) | A puff of black; white condensation too | Exhaust |
| Gelled fuel cranked at the key | A white cough (fuel through unburnt) | Exhaust |
| A turbine's hot start | A flash of flame and black smoke | Exhaust |
| Brakes hot, fading, faded | Pale smoke, more as they fade | Wheels |
| A slipping clutch taking up | A wisp of acrid smoke | Underneath |
| A tyre bursting | Grit and shredded rubber | Wheels |
| Shorted wiring | Sparks while it lasts | Engine |
| A fuel leak (Vehicle Framework's) | Drips | Underneath |

**A choice of the agent's, for the owner to revise**: the owner's card said "a black cough on a failed start". Fuel cranked through without
firing comes out white, so a gelled-fuel start coughs white; a flat battery only clicks, and an empty fuel feed shows nothing.

## Slice 2: what moving vehicles throw up (next)

Exhaust that follows the engine's load (a diesel's darker, a petrol's faint, white from a cold engine in cold air); dust, sand, mud, snow
and water spray thrown up by the wheels, by the ground and the place's dust; ruts on soft ground and snow that fade; rotor downwash under
a helicopter near the ground (dust, spray over water, nothing on paving); tyre smoke and dust at a rough landing. The figures are in
`VehicleEffects.xml`. **Close Air Support's jets draw their own wash and touchdowns**, so ours stands aside for any aircraft another flight
mod flies.

## The four questions

- **What it lets the player do**: see a vehicle's faults on the map at a glance, without the Health tab and without sound; see where
  vehicles are and how hard they work.
- **What it stops them doing**: nothing; it is looks only, and the setting switches it off.
- **How it changes the way they play**: a smoking truck is worth a look, and its smoke's colour says which tab to open (blue: oil; black:
  air filter or boost; white: coolant).
- **What outside the vehicle notices**: the map shows it, and the player's eye; other factions' vehicles show the same signs, so a raider's
  truck smoking blue is a worn one. Nothing in the game's rules notices, by choice: looks only.

## Risk

None: nothing a player owns is touched. The Vehicle effects setting is for performance and taste, not for risk.

## Other factions' vehicles

The same signs on any faction's vehicle while its systems run; another faction's vehicle runs its systems at the lite tier or not at all, so
most of its fault signs show only once it is captured. The fuel drips and slice 2's effects need no systems and show on any vehicle. The
cost is the emitter's, which is on screen only.

## Performance

One pass every 15 ticks, on the map being looked at, over the things on screen: a dictionary walk and a few flecks each. The game draws
flecks instanced. Not yet measured in play: a big convoy on screen is the case to record with `perf.py`, with the owner's go-ahead.

## Saves

Nothing is saved; safe to add and to remove. What a next game would need: the emitter's arithmetic (`FxRule`) is game-free; the emitter
itself is RimWorld's.
