# Headlights that light the ground (12 Nov 2026)

**Built in D'thorga Framework 0.104.0. Not yet tested in game.**

The owner asked on 12 November 2026 for "functional headlights by default for all vehicles that are under our Vehicle mod's wing. We
have a gizmo for headlights so why not make it properly functional?" Until 0.104.0 the headlights were only an electrical load: their
switch, the night and a running engine made them draw current from the battery, and they gave no light.

## Read this first

- **Which vehicles:** every vehicle whose loadout carries a headlights load, which is every land vehicle and boat our vehicle systems
  reach through the light, heavy and electric loadouts, **ours and other authors' alike**. That is what "under our wing" means here: the
  vehicles D'thorga Framework's systems run, not only D'thorga Vehicles' own. Aircraft keep their navigation lights, which cast no beam.
- **When (owner, 12 Nov 2026: "make this a rule"):** at night, with the headlights switch on, **whether or not the engine runs**. They
  draw their current either way, so with the engine off they run the battery down, and left on overnight they can flatten it. The switch
  is the existing button; its description says so.
- **The switch is now off by default** (agent's choice, for the owner to revise). With it on by default, as before, the rule above would
  flatten every parked vehicle's battery every night: a light vehicle's 45 Ah battery feeds 9 A of headlights for about five hours. So
  the lights burn only where someone switched them on, as in a real vehicle. **A vehicle whose switch was never pressed now has its
  headlights off**; one switched by hand keeps its choice.
- **Aircraft navigation lights are unchanged** (agent's choice): they still burn only while the engines run. They are flight lights, cast
  no beam, and default on, so the new rule would flatten every parked aircraft overnight for nothing.
- **A warning before a vehicle is stranded:** see "Starting power" below.
- **The battery in words on the vehicle's inspect panel**, in every display mode: see "The battery's line" below.
- **A setting turns the beam off:** "Headlights light the ground", in the vehicle systems settings, on by default. Off, headlights draw
  on the battery as before and give no light.
- **Nothing is saved.** The light is worked out again after a load.

## What it does to the player

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| See and work where a vehicle stands or drives at night: the ground ahead is lit by the game's own light, so the darkness penalties to work and to shooting lift there | Leaving the lights on in a parked vehicle all night: the battery goes flat and the engine will not start | Light at a work site costs battery or fuel: park with the engine off and the battery runs down, leave it running and it burns fuel; a convoy driving at night shows itself |

## How it works

- **Data, not code:** a load in a loadout may name a `beam` (`BeamSpec`: radius, cells ahead of the nose, colour, brightness). The three
  headlights loads carry one: a warm halogen white for diesel and petrol vehicles, a cool LED white for electrics. Another modder gives
  any load a beam in XML.
- **Where the light falls:** the game's light is a round pool around one cell, so a beam is one pool whose centre stands 5 cells past
  the vehicle's nose along its heading, with a 6-cell radius: from the nose to about 11 cells out is lit (`HeadlightRule.BeamCell`, with
  tests). **A simplification, labelled:** a real low beam is a cone, not a pool, and the distances are authored, roughly a low beam's reach on
  the map's scale.
- **The light itself** is the game's own custom glower (`GlowGrid.RegisterCustomGlower`): a light at any cell with no thing behind it.
  `VehicleLight` is the one place our vehicles register, move and clear light; the work lamps (Framework 0.103.0) were moved onto it.

## Starting power: a warning before the battery is too weak

Owner, 12 Nov 2026: a warning in the top left before a vehicle cannot start, "around 33%", and a better figure welcome.

- **The figure chosen: starting power below 0.2**, twice the line at which the engine will not start (0.1). The stage is labelled
  "barely enough to start" and sends a message once (the systems' usual cooldown applies; only for the player's vehicles).
- **Why starting power and not charge:** starting power is the charge times the battery's cold factor times the cold oil's factor. At
  -18 degrees C with cold oil an engine already fails near 33% charge (the owner's figure), while in mild weather 30% still starts. A
  warning on starting power therefore always comes before being stranded, whatever the weather. The 30% "battery is running low"
  message stays as it was.
- **A limit, labelled:** the message is sent when the stage is entered from either side, so a flat battery being charged back up also
  passes through it and sends it once.

## The battery's line

Owner, 12 Nov 2026: "Yes, in words", in every display mode. On the inspect panel of the player's vehicles with an electrical system:

| Mode | Line |
| --- | --- |
| Plain | "Headlights on: the battery is running down." (the switched loads that run, named from the loadout) or "The battery is running down."; "The alternator is charging the battery."; "The battery is charging from the power nearby." (a parked vehicle on shore power or a service lead); nothing when the battery is steady or nearly full |
| Operator and Engineer | The same line with the current in amperes: "... (9 A)". Engineer's own line of figures stays |

The line reads a status the systems work out at each update, from the battery's own flows; the inspect panel computes nothing.

## Cost

The owner's colony runs at 36 to 50 ticks a second, and every move of a light makes the game work out the light around it again.

- A dark vehicle costs one check a tick.
- Whether a beam should burn is decided on the vehicle's rare tick and at its ignition and move events, and when the switch is pressed.
- A lit beam is looked at no more than every 20 ticks (authored, about three times a second), and moved only when the vehicle has reached
  another cell or turned. A parked vehicle with its lights on costs nothing after the first placing.
- The setting turns it off entirely.

## Other factions' vehicles

- **In a raid on us:** another faction's vehicles run on the light tier, with no electrics, so the headlights rule cannot be read for them.
  **Agent's decision:** their headlights burn at night **only while they move**, whatever their switch says (their crews switch them on
  to drive). A raid
  that drives in at night lights itself up: a real tell for the defenders, and a real cost to the raiders, as on a dark road.
- **Parked at a base we attack:** dark. Vehicle Framework keeps another faction's vehicles "drafted" from the moment they spawn, so a rule
  reading the engine would light every parked vehicle at an enemy base all night. Lighting them only while they move keeps a base of them
  free.
- **One we capture:** it becomes ours and follows our rule (switch, night, engine).

## Not done

- **No headlight part.** No loadout names a part that carries the headlights, so a vehicle's lights do not go out when its front is
  shot up. A loadout role for them would let them.
- **No cone.** One pool stands for the beam. Two or three smaller pools along the heading would look more like a beam and cost two or
  three times the work: worth trying only if the owner finds one pool too round.
- **No dazzle or detection.** The light lifts the game's darkness penalties where it falls, and nothing more: a lit raider is easier to
  see only because the player can see it.

## Who chose what

- **Owner (12 Nov 2026):** headlights that light the ground, by default, on every vehicle our systems run.
- **Owner (12 Nov 2026, later):** headlights work with the engine off as a rule; a low-starting-power warning; the battery's state in words.
- **Agent:** the single pool and its distances and colours; the setting (on); the 20-tick look; raiders lit only while moving, whatever
  the switch; the headlights switch off by default; aircraft navigation lights left on the engine; the warning at starting power 0.2.
