# Vehicle systems: design (8 Nov 2026, built the same night)

**Built in D'thorga Framework 0.9.0 (8 Nov 2026), phases A to C, while the owner slept, on the owner's go. Not yet tested in game.**
What differs from the design below is in [As built](#as-built-framework-090-8-nov-2026), with the owner's test list. The rest of
this record is the design as it was reviewed.

A design record. The owner asked on 8 Nov 2026 for real vehicle
systems in RimWorld: electrical (battery, alternator), coolant and oil temperature, "and others, this is just a start",
each with per-part health; built as a **generic, schema-driven service** on D'thorga Framework; applied to our own
vehicles and **retrofitted to other authors' Vehicle Framework vehicles as configurable settings**; and extensible by
anyone through XML files, with the process documented. The owner reads and corrects this record before any code is
written; the build is the next plan. Code readings behind it are in
[Vehicle Framework mechanics](vehicle-framework-mechanics.md).

## Read this first

- **The owner's rules (8 Nov 2026):** a generic service, not a feature of one vehicle; per-vehicle customisation and
  whole loadouts come from schema files (Defs in XML) that we or any other modder write; the process is documented so it
  is transparent; the system is highly extensible. The framework comes **before** any custom vehicle of ours. Everything
  new is **RimWorld 1.7 only**.
- **Removal from a save is unsafe once the service has run** (inferred from Vehicle Framework's code, not observed): the
  service appends parts to vehicles, saves hold part healths **by position**, and a design with fewer parts than its
  saved vehicle fails on load. The Framework's uninstall sweep must run first: it cuts every saved vehicle's part list
  back to the design's original count and drops our state. The README of every mod carrying the service says so.
- **Changing which vehicles get the service needs a restart.** Parts and the comp are added while Defs load.
- **Vehicle Framework on 1.7 is its 1.6 build.** The Workshop copy (3014915404) declares 1.4 to 1.6; the game loads it on
  1.7 and the owner plays that way. SmashPhil is active and has said a 1.7 release "with more changes" is near (GitHub
  issue 379, 1 Nov 2026). We compile against the installed DLL and expect a rebuild when 1.7 ships.
- **Combat Extended hits only parts with real cells** (its own damage model replaces Vehicle Framework's). Every part
  the service adds carries cells next to the engine's and a low hit weight under Vehicle Framework, which the service
  inverts under Combat Extended, where a smaller weight is hit more (corrected 8 Nov 2026; each part may give its own
  `hitWeightCE`). Part hits count the health a part lost, not the shot's damage, so a round the armour stopped drains nothing.
- **Text stays in the world.** Inspect lines, refusals and messages describe the vehicle, never a mod, a schema or a
  setting. The Framework's display mode decides how much a player sees: Plain shows no figure the player did not ask for.
- **Every figure is labelled**: primary source, secondary source, our inference or authored. The real-world figures
  below are secondary unless stated, and every starting value is "a proposal, not tuned".

## Where it lives (agent default, owner to confirm)

**D'thorga Framework, as a Vehicle-Framework-optional part.** The same pattern as the Framework's Dubs Bad Hygiene
tie-in: a folder `mods/PhobosFramework/Mods/VehicleFramework/{Common,1.7}` that `LoadFolders.xml` loads only with
`IfModActive="SmashPhil.VehicleFramework"`, and an assembly `PhobosFramework.Vehicles` (namespace
`Phobos.Framework.Vehicles`) that references the installed `Vehicles.dll` with `Private=false` and is never copied. The
Framework's core never references Vehicle Framework. The Def types, the default loadouts, the rules Def, the settings
section, the dev actions and the modder guide ship with that part, so another author's vehicle mod can use the service
with none of our content installed.

| Considered | Why not |
| --- | --- |
| Inside D'thorga Vehicles (the content mod) | Other authors would have to depend on our content to use the service, against "other authors must be able to use a framework without our content mods" |
| A third mod, "D'thorga Vehicle Systems" | Fewer, broader mods (owner, 7 Nov 2026); the Framework already carries optional parts |
| A part of D'thorga Patches | New behaviour, not a fix for another author's mod |

D'thorga Vehicles (`PhobosDthorga.Vehicles`) is created as a **stand-in** content mod: it holds the specific loadouts for
other mods' vehicles (Vanilla Vehicles Expanded, the Harrier, the HUMVEE) under `IfModActive` folders now, and our own
vehicles later ([utility vehicles](utility-vehicles-design.md)).

## The shape of the schema (agent proposal)

The schema mirrors the game's own health conditions: a `HediffDef` has a severity with **stages**, each stage a threshold
with effects. RimWorld modders already know that shape. Here a vehicle has systems, a system has **quantities** (charge,
coolant temperature, oil pressure), quantities move through **flows**, and each quantity has stages whose **effects** are
pluggable classes, the way Vehicle Framework's reactors are. Three Def types and one rules Def, all XML, all under the
`Phobos` prefix and never renamed once saves can hold them.

### `PhobosVehicleSystemDef`: one kind of system

| Field | What it holds |
| --- | --- |
| `workerClass` | Default `VehicleSystemWorker`, the generic stock-and-flow worker. A C# subclass only for what XML cannot say. |
| `quantities` | State variables. Each: `name`, `unit` (degrees Celsius, fraction, ampere-hours, kilopascals), `min`, `max`, `startsAt` (Ambient, Full, Zero, or a rule), `stages`. |
| `flows` | Rate rules. Each: `name`, `into` or `from` a quantity, `toward` a target (a fixed value or an input such as ambient), `inputs` (multiplied together), a `curve` or `factor`, and `openAbove` or `openBelow` thresholds (a thermostat). |
| `stages` (per quantity) | Ordered thresholds: `from`, `to`, an in-world `label`, and `effects`. |
| `effects` (per stage) | A list of effect classes, each with its own fields. |
| `parameters` | Named figures a loadout may override (battery capacity, alternator output, thermostat temperature, radiator capacity), with defaults and units. |

**Inputs** (pluggable, `Class=`): `Input_FuelBurn` (this vehicle's fuel use this interval, from Vehicle Framework's own
fuel comp: idling is half rate), `Input_EngineRunning` (drafted), `Input_Moving`, `Input_Ambient` (the vehicle's own
ambient temperature, which works on the world map too), `Input_PartEfficiency` (by **role**, see loadouts),
`Input_TimeOfDay` (lights), `Input_Quantity` (another quantity, for cross-system rules), `Input_Constant`.

**Effects** (pluggable, `Class=`): `Effect_DamagePart` (by role, per hour), `Effect_PartWear` (slow damage, for cold
running), `Effect_RefuseDraft` (with the reason key; the gizmo shows the reason), `Effect_ForceUndraft`,
`Effect_StatFactor` (a Vehicle Framework stat part: dim lights, slow a vehicle), `Effect_PushHeat` (into the vehicle's
room through the game's own temperature system), `Effect_Fleck` and `Effect_Mote` (steam from the bonnet, the
frost-effect pattern in our own art), `Effect_Message` (once per stage entry), `Effect_Letter` (rare: a seized engine, a
cracked battery), `Effect_ConsumeFuel`, `Effect_SetQuantity` and `Effect_RateQuantity` (cross-system: a boil-over drains
the coolant level). A modder adds an input or an effect by writing one class and naming it in XML; nothing of ours
changes.

### `PhobosVehicleLoadoutDef`: which systems a vehicle gets, and how its parts map

| Field | What it holds |
| --- | --- |
| `selector` | Any of: `vehicleDefs` (names), `packageIds` (every vehicle of a mod), or **effects**: `vehicleTypes` (Land, Sea, Air, Universal), `fuelled`, `electricPowered`, `fuelTypes`, `hasStat` (FlightSpeed marks an aircraft), `massRange`, `partTags`. All given conditions must hold. |
| `priority` | `Vehicle` (named) beats `Mod` beats `Effect` beats `Default`. Ties: the last loaded wins, with a load-line warning. |
| `systems` | The `PhobosVehicleSystemDef`s to run, each with `set` overrides for that system's parameters. |
| `partRoles` | For each role a system needs (Engine, Alternator, Battery, Radiator, CoolantPump, OilPump, Starter, Wiring): `find` rules tried in order (by `key`, by `categories` plus `depth` plus `priority`, by `tag`), then `add` (a `VehicleComponentProperties` to append if nothing matched, with `cellsNear` a found role or explicit `cells`, a low `hitWeight`, and depth). |
| `loads` | Named electrical loads: `amps`, `when` (a pluggable condition: night, below a temperature, a gizmo toggle, another mod's registration). |
| `variants` | Optional named kits (an "arctic" kit: bigger battery, block heater, winter fuel). **Agent idea for the owner:** a variant could be offered as a Vehicle Framework upgrade node through its `CompUpgrade`, so the player buys it in the upgrade tree. |

A loadout never renames or reorders a vehicle's existing parts. Added parts are **appended only**, keys prefixed with
the loadout's own prefix, so a saved vehicle keeps its positions (the rule D'thorga Patches already relies on for the
Harrier). Matching by `key` is a hint for named loadouts; the default loadouts match by effect (category, depth, tag),
never by name (owner's standing rule, 5 Nov 2026).

### `PhobosVehicleRulesDef`: the physics constants

A `PhobosRulesDef` subclass (first loaded instance wins; patchable in XML): the battery capacity curve against
temperature, the thermostat band, the overheating floors, the cold-running wear floor, the fuel gel point, the heat
released per unit of fuel, the rare-tick length as hours. Player preferences are mod settings, never rules.

### Worked examples (draft; field names settle at build)

The default combustion loadout, shipped by the Framework, matched by effect:

```xml
<Phobos.Framework.Vehicles.PhobosVehicleLoadoutDef>
  <defName>PhobosVehicleLoadout_CombustionLand</defName>
  <priority>Default</priority>
  <selector>
    <vehicleTypes><li>Land</li></vehicleTypes>
    <fuelled>true</fuelled>
    <electricPowered>false</electricPowered>
  </selector>
  <systems>
    <li><def>PhobosVehicleSystem_Electrical</def></li>
    <li><def>PhobosVehicleSystem_Cooling</def></li>
    <li><def>PhobosVehicleSystem_Lubrication</def></li>
  </systems>
  <partRoles>
    <li>
      <role>Engine</role>
      <find><categories><li>MoveSpeed</li></categories><depth>Internal</depth><priority>true</priority></find>
      <find><categories><li>MoveSpeed</li></categories><depth>Internal</depth></find>
    </li>
    <li>
      <role>Radiator</role>
      <add>
        <key>PhobosRadiator</key><label>radiator</label><health>30</health><depth>Internal</depth>
        <hitWeight>0.5</hitWeight><cellsNear>Engine</cellsNear>
      </add>
    </li>
    <li>
      <role>Battery</role>
      <find><tag>Battery</tag></find>
      <add>
        <key>PhobosBattery</key><label>battery</label><health>20</health><depth>Internal</depth>
        <hitWeight>0.3</hitWeight><cellsNear>Engine</cellsNear>
      </add>
    </li>
    <li>
      <role>Alternator</role>
      <find><tag>Alternator</tag></find>
      <add>
        <key>PhobosAlternator</key><label>alternator</label><health>20</health><depth>Internal</depth>
        <hitWeight>0.3</hitWeight><cellsNear>Engine</cellsNear>
      </add>
    </li>
  </partRoles>
  <loads>
    <li><name>lights</name><amps>9</amps><when Class="When_Night" /></li>
    <li><name>cabHeater</name><amps>20</amps><when Class="When_AmbientBelow"><celsius>5</celsius></when></li>
  </loads>
</Phobos.Framework.Vehicles.PhobosVehicleLoadoutDef>
```

A named loadout in D'thorga Vehicles for a Vanilla Vehicles Expanded truck, which already has `Engine`, `Gearbox` and
`Alternator` parts (its alternator is health only today):

```xml
<Phobos.Framework.Vehicles.PhobosVehicleLoadoutDef ParentName="PhobosVehicleLoadout_CombustionLand">
  <defName>PhobosVehicleLoadout_VVE_Mule</defName>
  <priority>Vehicle</priority>
  <selector><vehicleDefs><li>VVE_Mule</li></vehicleDefs></selector>
  <systems Inherit="False">
    <li>
      <def>PhobosVehicleSystem_Electrical</def>
      <set><batteryAmpHours>90</batteryAmpHours><alternatorAmps>90</alternatorAmps></set>
    </li>
    <li><def>PhobosVehicleSystem_Cooling</def><set><radiatorKilowatts>60</radiatorKilowatts></set></li>
    <li><def>PhobosVehicleSystem_Lubrication</def></li>
  </systems>
  <partRoles Inherit="False">
    <li><role>Engine</role><find><key>Engine</key></find></li>
    <li><role>Alternator</role><find><key>Alternator</key></find></li>
    <li><role>Radiator</role><add><key>PhobosRadiator</key><label>radiator</label><health>30</health><depth>Internal</depth><hitWeight>0.5</hitWeight><cells><li>(0, 2)</li><li>(1, 2)</li></cells></add></li>
    <li><role>Battery</role><add><key>PhobosBattery</key><label>battery</label><health>20</health><depth>Internal</depth><hitWeight>0.3</hitWeight><cells><li>(0, 1)</li></cells></add></li>
  </partRoles>
</Phobos.Framework.Vehicles.PhobosVehicleLoadoutDef>
```

What another modder writes to give their own truck a bigger alternator and nothing else:

```xml
<Phobos.Framework.Vehicles.PhobosVehicleLoadoutDef ParentName="PhobosVehicleLoadout_CombustionLand">
  <defName>MyMod_BigTruck_Loadout</defName>
  <priority>Vehicle</priority>
  <selector><vehicleDefs><li>MyMod_BigTruck</li></vehicleDefs></selector>
  <systems Inherit="False">
    <li><def>PhobosVehicleSystem_Electrical</def><set><alternatorAmps>140</alternatorAmps></set></li>
    <li><def>PhobosVehicleSystem_Cooling</def></li>
    <li><def>PhobosVehicleSystem_Lubrication</def></li>
  </systems>
</Phobos.Framework.Vehicles.PhobosVehicleLoadoutDef>
```

A cooling system, abridged, showing quantities, a thermostat flow and stages:

```xml
<Phobos.Framework.Vehicles.PhobosVehicleSystemDef>
  <defName>PhobosVehicleSystem_Cooling</defName>
  <label>cooling</label>
  <parameters>
    <li><name>radiatorKilowatts</name><default>40</default></li>
    <li><name>thermostatOpensCelsius</name><default>88</default></li>
  </parameters>
  <quantities>
    <li>
      <name>coolantTemperature</name><unit>Celsius</unit><startsAt>Ambient</startsAt><min>-60</min><max>200</max>
      <stages>
        <li><label>cold</label><to>40</to>
          <effects><li Class="Effect_PartWear"><role>Engine</role><perHour>0.5</perHour><onlyWhile Class="When_EngineRunning" /></li></effects></li>
        <li><label>warm</label><from>40</from><to>110</to></li>
        <li><label>hot</label><from>110</from><to>120</to>
          <effects>
            <li Class="Effect_Fleck"><fleck>PhobosSteam</fleck><perHour>12</perHour></li>
            <li Class="Effect_Message"><key>PhobosVehicles_EngineRunningHot</key></li>
          </effects></li>
        <li><label>overheating</label><from>120</from><to>125</to>
          <effects><li Class="Effect_DamagePart"><role>Engine</role><perHour>4</perHour></li></effects></li>
        <li><label>boiling over</label><from>125</from>
          <effects><li Class="Effect_RateQuantity"><quantity>coolantLevel</quantity><perHour>-1</perHour></li></effects></li>
      </stages>
    </li>
    <li><name>coolantLevel</name><unit>Fraction</unit><startsAt>Full</startsAt><min>0</min><max>1</max></li>
  </quantities>
  <flows>
    <li><name>engineHeat</name><into>coolantTemperature</into>
      <inputs><li Class="Input_FuelBurn" /><li Class="Input_Quantity"><quantity>coolantLevel</quantity><curve>(0,4)(0.5,1.5)(1,1)</curve></li></inputs>
      <factor>HeatPerFuelUnit</factor></li>
    <li><name>radiator</name><from>coolantTemperature</from><toward Class="Input_Ambient" /><openAbove>thermostatOpensCelsius</openAbove>
      <inputs><li Class="Input_PartEfficiency"><role>Radiator</role></li><li Class="Input_PartEfficiency"><role>CoolantPump</role></li>
        <li Class="Input_Moving"><stopped>0.6</stopped><moving>1</moving></li></inputs>
      <factor>radiatorKilowatts</factor></li>
    <li><name>soak</name><from>coolantTemperature</from><toward Class="Input_Ambient" /><factor>Soak</factor></li>
  </flows>
</Phobos.Framework.Vehicles.PhobosVehicleSystemDef>
```

A new effect class, in a modder's own assembly:

```csharp
public class Effect_FlashBeacon : VehicleSystemEffect
{
    public ThingDef beacon;
    public override void Apply(VehicleSystemContext context, float hours)
    {
        // context.Vehicle, context.Quantity(name), context.Part(role), context.Hours
    }
}
```

## The service (agent proposal)

- **`VehicleSystems`**, a static, presentation-free service: resolves a `VehicleDef` to its loadout by the priority
  rule; injects the added parts and our comp **before** Vehicle Framework resolves references (a postfix on the game's
  `DefGenerator.GenerateImpliedDefs_PreResolve`, or Vehicle Framework's own implied-Defs event), so Vehicle Framework
  turns our cells into hitboxes and its settings window sees our comp; validates every schema at load (unknown roles,
  duplicate keys, cells outside the vehicle, stages out of order, a parameter no system declares) and reports once in
  the Framework's load line; refuses a loadout with errors rather than half-applying it.
- **`CompPhobosVehicleSystems : VehicleComp`**, one per vehicle: one `SystemState` per system, quantities saved in
  `PostExposeData` with stable keys; derived values never saved. It integrates on the rare tick (every 250 ticks, a hook
  Vehicle Framework already calls on every comp) with the real elapsed hours, and switches state on Vehicle Framework's
  events (ignition on and off, move start and stop, damage taken, repaired, out of fuel, refuelled). No per-tick work.
- **`VehicleSystemModel`**, the game-free core: given the schema, the state, the inputs and the hours, it returns the new
  state and the stage transitions. Unit-tested in `tests/` like the Framework's other rules; the comp is a thin wrapper.
- **Tuning in Vehicle Framework's own window**: our `VehicleCompProperties` subclass carries the loadout's parameters as
  settable fields, so a player tunes a vehicle's battery or radiator beside its fuel settings. Which vehicles get the
  service is our own setting (below), because that decides whether the comp exists at all.
- **Dev actions** in the Framework's debug category: set a quantity, flatten the battery, overheat the engine, dump the
  state of the selected vehicle.
- **The uninstall sweep**: the Framework's cleanup service gains a vehicle step that truncates each saved vehicle's parts
  to its design's original count and removes our comp state, so the player can remove the service before saving.
- **An offline validator**, `scripts/check-vehicle-schemas.py`, reads our and other mods' schema files without the game
  (tools before reading by hand); built with phase A.

## Settings (our `ModSettings`, drawn with the Framework's section helpers)

| Setting | Choices | Default (agent) | Needs restart |
| --- | --- | --- | --- |
| Scope | Phobos vehicles only / every vehicle with a loadout | Every vehicle | Yes |
| Per source mod | On or off for each mod whose vehicles matched | On | Yes |
| Per loadout kind | Combustion land, electric, aircraft, boat | Combustion land on; aircraft off in phase A | Yes |
| Per system | Electrical, cooling, lubrication, later wear | On | No |
| Level | Show only (figures, no consequences) / Full | Full | No |
| Fuel gelling | Off / On (chemfuel behaves like diesel below the gel point) | Off | No |

## The first two systems, as schemas (agent proposal; every figure labelled)

### Electrical

- **Quantities:** battery charge (ampere-hours, starts Full, capacity scaled by the vehicle's mass: 40 Ah at 12 V for a
  light vehicle, two in series for 24 V on heavy ones, secondary: Wikipedia, automotive battery); the usable capacity
  follows a temperature curve, 100% at 27 °C, about 80% at 0 °C, about 50% at −18 °C (secondary: Battery University
  BU-502, Midtronics; our interpolation between them is authored).
- **Flows in:** the alternator while the engine runs, scaled by the alternator part's efficiency; 50 to 70 A light
  vehicles, about 140 A trucks, up to 300 A heavy plant (secondary: Wikipedia, automotive alternator; Delco Remy 24SI
  24 V 70 A and 28SI 24 V 100 A as product data).
- **Flows out:** registered loads (lights at night, cab heater below a floor, a winch while it works, our rigs'
  consoles, the radio framework's vehicle radio once that tie-in exists), the starter's draw on each ignition, and cold
  cranking: below 0 °C the draw rises and a weak battery fails to start (authored from the capacity curve).
- **Stages:** good; low (lights dim, an `Effect_StatFactor` on move speed at night, authored); flat (**cannot start: the
  draft gizmo refuses and names the flat battery**; a jump-start job from another vehicle alongside, or a charger
  building on the power net, restores a starting charge); frozen (a discharged battery below −10 °C cracks: part
  damage; secondary: Battery University).
- **Electric vehicles:** Vehicle Framework's fuel tank already is their traction pack; we add only the auxiliary battery
  and the motor's temperature. **Boats and aircraft** get the electrical system unchanged.

### Cooling and lubrication

- **Quantities:** coolant temperature (starts at ambient), coolant level (Full), oil temperature (ambient), oil pressure
  (derived each interval from the oil pump's efficiency and the oil level, shown as a quantity).
- **Flows:** engine heat into the coolant in proportion to fuel burned (Vehicle Framework's own consumption; idling is
  half rate; the heat per fuel unit is authored), rising steeply as the coolant level falls; radiator cooling toward
  ambient once the thermostat opens, 82 to 90 °C (secondary: engine handbooks quoted on forums; a Detroit Diesel unit at
  87 °C), scaled by the radiator and coolant-pump parts' efficiency and by airflow (moving beats stopped); a slow soak
  toward ambient always; steady running lands at 85 to 105 °C (secondary: Wikipedia, engine cooling). Oil temperature
  lags the coolant. Room heat: the engine's waste heat goes into the room the vehicle stands in through the game's own
  temperature system, in proportion to burn, instead of Vehicle Framework's flat ambient-heat figure (vanilla
  precedence; a vehicle idling in a closed garage warms it).
- **Stages, coolant:** cold (slow engine wear while running below 40 °C: cold starts cause a large share of total wear
  and more the colder it is, secondary: a Szczecin Maritime University study and SAE 2006-01-3413 as cited second-hand);
  warm; hot above 110 °C (steam from the bonnet, a message); overheating above 120 °C (engine damage per hour);
  boiling over above 125 °C (coolant drains, which drives the heat up faster: the feedback loop head-gasket failures
  show, secondary: Wikipedia). The three floors are authored for a pressurised system, which runs above 100 °C.
- **Stages, oil:** cold; normal 85 to 105 °C; hot above 110 °C (oil breaks down faster, secondary and low quality: a
  forum; replace with an engine manual before the build); low pressure (engine wear) when the pump is damaged or the
  level is low.
- **Fuel gelling (setting, off by default):** chemfuel is a generic fuel, so treating it as diesel is a gameplay choice;
  when on, below −8 °C (No. 2 diesel gel point, secondary: Wikipedia, gel point) a cold vehicle will not start until
  warmed by a heater load or a warmer day.
- **Aircraft are deferred** from phase A: jets and rotorcraft have different thermal behaviour and Vehicle Framework's
  flight model gives no load signal. **Boats** run raw-water cooling: no radiator part, a pump only.

## What the player sees (presentation only; services decide)

- An inspect line per display mode. Plain: "Engine warm, battery good." Operator: coolant and oil temperatures, charge
  as a percentage. Engineer: each flow this interval and the stage thresholds.
- Gizmos: lights, cab heater (toggles that register loads); jump-start as a float-menu option on another vehicle.
- Every refusal names its cause and the fix ("The battery is flat. Jump-start it from another vehicle or a charger.").
- Letters only for the rare and costly: a seized engine, a cracked battery.
- On the vehicle: steam flecks when hot, exhaust when cold-running (our own art, the frost-effect pattern).

## Roadmap (this is just a start)

| Phase | What | Where |
| --- | --- | --- |
| A | Electrical, cooling and lubrication; the default combustion loadout; named loadouts for Vanilla Vehicles Expanded, the Harrier and the HUMVEE in the stand-in mod; settings; the uninstall sweep; dev actions; the validator; the modder guide `docs/vehicle-systems-modding.md` | Framework vehicle part, D'thorga Vehicles |
| B | Wear and service: a breakdown roll mirrored from the game's own (mean time between failures scaled by part health, hours above the oil floor and cold starts); a service job (oil change, coolant top-up, battery swap) needing components and chemfuel; the service interval in Engineer mode | Framework vehicle part |
| C | Fuel quality, tyres and tracks, brakes, transmission; hydraulics and pneumatics for our rigs | Framework vehicle part, D'thorga Vehicles |
| D | Tie-ins: the radio framework's vehicle radio as a load; D'thorga Medical's ambulance power; Dubs Bad Hygiene water for coolant | Each mod's `IfModActive` folder |

## Who chose what

- **Owner (8 Nov 2026):** real electrical, battery, alternator, coolant and oil systems; retrofitted to other authors'
  vehicles as settings; a generic service using our Framework; schemas in XML for us and other modders, documented;
  highly extensible; the framework before any custom vehicle; 1.7 only; this record before any code.
- **Agent defaults, owner to confirm:** the Framework placement; the stages-and-effects shape; parts appended by role
  and found by effect; the rare-tick loop; the settings table; aircraft deferred; the three temperature floors; the
  variants-as-upgrades idea.

## What was argued against, and why

- **A free scripting layer** (expressions in XML) instead of stages and pluggable classes: harder to validate, easy to
  make slow, and foreign to RimWorld modders. Stages plus `Class=` extension points cover the same ground and the game
  already teaches the pattern.
- **Reusing the game's breakdown manager**: it assumes buildings and needs map registration; mirroring its roll in our
  rare tick is simpler and keeps vehicles out of its sweep.
- **Reusing Vehicle Framework's turret deploy state** for anything of ours: it belongs to turrets; our comps use the
  framework's own move and draft hooks.
- **Simulating per tick**: the rare tick is what Vehicle Framework already calls on every comp; a fraction of a
  millisecond per tick matters with the owner's mod list.

## Open questions for the owner

| Question | Agent lean |
| --- | --- |
| Framework placement, or inside D'thorga Vehicles? | Framework (other authors use it without our content) |
| Stages and effects, as proposed, or something freer? | Stages (RimWorld modders know it; validators can check it) |
| Should a flat battery strand a caravan on the world map, or only refuse on a map? | Refuse on a map only in phase A; the world map later, with a jump-start from another caravan vehicle |
| Aircraft in phase A? | No: deferred until a load signal exists |
| Variants as Vehicle Framework upgrade nodes? | Yes, as an option a loadout may declare |
| Figures: Celsius everywhere, and ampere-hours for charge? | Celsius (the game's unit); charge shown as a percentage in Plain and Operator modes |
| The display name of the stand-in content mod | "D'thorga Vehicles" |

## As built (Framework 0.9.0, 8 Nov 2026)

The owner said "proceed with Phase A through to Phase C" and to save questions for the morning, so every open question above was
answered with the agent's lean and is listed again at the end of this section. **Not yet tested in game.** Built and checked offline:
the assembly compiles against the installed Vehicle Framework (1.6 build) and RimWorld 1.7; 24 rule tests pass, including the shipped
cooling figures; the schema check passes every system and loadout; the repository's checks pass.

**Where things are:** the code in `src/PhobosFramework.Vehicles/` (namespace `Phobos.VehicleSystems`), the Defs, language file and
icons in `mods/PhobosFramework/Mods/VehicleFramework/Common/`, the offline check in `scripts/check-vehicle-schemas.py`, the guide in
[vehicle systems for modders](../vehicle-systems-modding.md). D'thorga Vehicles 0.1.0 (`mods/PhobosVehicles/`) is the stand-in mod.

| Phase | Built |
| --- | --- |
| A | The service (loadouts, part roles, injection, the comp, the rare-tick loop, events, part hits, stat factors, draft and move refusals, inspect text by display mode, switches), the electrical, cooling and lubrication systems, cold fuel (a setting), the default loadouts (light and heavy land, boats, aircraft), the settings, the removal sweep, dev actions, the schema check, the modder guide, D'thorga Vehicles' named loadouts |
| B | Wear and service: the hour meter, breakdowns by mean time between failures (more once due, far more once overdue), the service with supplies hauled into the cargo and Construction work at the vehicle, a "service when possible" button |
| C | Tyres and tracks, brakes, the gearbox's temperature, air brakes (named heavy trucks), hydraulics (ready, unused) |

**Changed from the design, and why (agent choices, for the owner):**

- **Namespace `Phobos.VehicleSystems`**, not `Phobos.Framework.Vehicles`: it is what every schema file names in `Class=`, so it is
  short and says what it is.
- **The parts go in from a prefix on `VehicleDef.ResolveReferences`**, not from the game's implied-Defs step: by then every
  cross-reference in the loadouts (stats, systems) is resolved, and Vehicle Framework still resolves the appended parts' hitboxes
  itself straight after.
- **Saved vehicles are kept in step both ways.** A prefix on Vehicle Framework's save code drops saved parts beyond the design's list,
  so switching vehicles or systems off in the settings is safe; a postfix creates parts a design gained, at full health (the
  D'thorga Patches rule, written again here because Patches' copy builds for 1.6 too and cannot reference this 1.7-only part; Patches
  adopts this one when it drops 1.6). The appended parts' healths are also saved by key, in case their order changes.
- **Removing the whole service still needs the sweep** (settings button or dev action) before saving, because without the service
  loaded nothing trims the saved parts.
- **`cellsOf`** names the role whose part lends its cells and armour (the design said `cellsNear`); finders' `priority` is
  `Yes`, `No` or `Any`, because the game's XML loader cannot be trusted with a nullable bool.
- **Flat battery fix:** a parked vehicle beside a powered building trickle-charges. The grid gives the charge free, a labelled
  simplification. **A jump-start from another vehicle is not built**: it needs a job across two vehicles; it is the first follow-up.
- **The service carries its supplies in the vehicle's cargo**: haulers load them (a hauling job), then a constructor services the
  vehicle through Vehicle Framework's own vehicle work job. One job per system that is due; a request services every system once.
- **Not built:** tuning in Vehicle Framework's own per-vehicle window (its window shows fields, and the parameters live in
  loadouts, so it would need one field per parameter; a question for the owner), loadout variants as upgrade nodes, aircraft cooling,
  electric vehicles' motor temperature, fuel quality beyond gelling, a boiler system for steam locomotives.
- **Supplies are stand-ins**: chemfuel for oil, coolant and hydraulic fluid; steel and components for tyres and track pads;
  components for brake pads and filters. Labelled in the system files and the README.
- **The added parts on a light land vehicle:** battery, starter, radiator, oil pump, brakes, and an alternator or gearbox only where
  the vehicle has none. Vanilla Vehicles Expanded's land vehicles keep their own engine, gearbox and alternator.
- **Cost:** one comp per fitted vehicle, its systems integrated every 250 ticks in up to four steps each, a few flows per system;
  nothing per tick. Not measured.

**Open questions answered by the agent's lean (the owner may change any):** the Framework placement; the stages-and-effects shape;
a flat battery refuses only on a map (caravans on the world map are not stopped); aircraft get only the electrical system and the
service; Celsius, with charge as a percentage; the display name D'thorga Vehicles.

**Test list for the owner** (short, the consequential checks first):

1. Load a colony with Vehicle Framework and the new Framework. The log's line "(vehicle systems)" says how many designs were fitted;
   any warning names `Vehicle systems:`. Report both.
2. Select a Vanilla Vehicles Expanded truck, switch the display mode to Operator in the settings: battery, coolant, oil, wear and the
   rest show in the inspect pane. The health tab lists the new parts.
3. Draft it and drive: the coolant warms to about 90 °C within a few in-game minutes and stays there.
4. Dev mode, D'thorga, "Vehicle systems: set a quantity": set its coolant temperature to the top. Steam rises, a message and then a
   letter come, and the engine takes damage. Set the battery charge to 0, undraft and draft: the draft is refused with the reason.
   Build a powered conduit beside it: the charge rises.
5. Press the spanner (service when possible) with chemfuel and a component in storage: a hauler loads them into the cargo, then a
   constructor services it and a message says so.
6. Save, reload: the figures are kept and no error mentions `Phobos.VehicleSystems`. In the settings, switch the truck's mod off,
   restart and load: the truck loads without the added parts and without errors.

## Follow-ups (Framework 0.10.0, 8 Nov 2026)

The owner's answers to the morning's questions. **Not yet tested in game.**

| Question | Owner's answer (8 Nov 2026) | Built |
| --- | --- | --- |
| Placement in the Framework and the stages shape | Kept (the owner was happy with the build) | |
| Jump-start from another vehicle | Yes | A system's `jumpStart` block; a right-click order on the flat vehicle for a colonist, with a donor within 8 cells; the donor loses the same ampere-hours unless its engine runs; every refusal says why |
| Tuning in Vehicle Framework's window | Yes | One field per tunable shipped parameter on the comp's properties, filled with each design's figure while Defs load, read back through Vehicle Framework's settings cache. A modder's own parameters stay in loadouts |
| Drop 1.6 from every mod | Yes | Every project single-target, PolySharp gone, `1.6` load folders and declarations removed (AGENTS.md, building.md) |
| Supplies for tyres | Steel and components, plus chemfuel for the rubber (owner) | 15 steel, 2 components, 10 chemfuel (counts authored) |
| Supplies for oil and coolant | Open: the owner is unsure; coolant with Dubs Bad Hygiene could be its water | Still chemfuel for both; the agent's proposal is in the reply of 8 Nov 2026 |

D'thorga Patches' Harrier part no longer carries its own copy of the saved-parts patch: the Framework's covers every vehicle.

## Sources

- Vehicle Framework (SmashPhil, MIT): code readings in [Vehicle Framework mechanics](vehicle-framework-mechanics.md),
  from the GitHub `develop` branch (head b557926d, 31 Oct 2026) and the 1.6 build's decompile, read 8 Nov 2026.
- The game's temperature, heat-pusher and breakdown code: RimWorld 1.7 assembly, read 8 Nov 2026.
- Real-world figures (secondary unless stated): [Internal combustion engine cooling](https://en.wikipedia.org/wiki/Internal_combustion_engine_cooling)
  and [Head gasket](https://en.wikipedia.org/wiki/Head_gasket) (Wikipedia); thermostat opening temperatures from engine
  handbooks quoted on owner forums and a [Detroit Diesel thermostat listing](https://www.duallane.com/detroit-coolant-thermostat-a4712007515);
  [Automotive battery](https://en.wikipedia.org/wiki/Automotive_battery) and
  [Alternator (automotive)](https://en.wikipedia.org/wiki/Alternator_(automotive)) (Wikipedia);
  [Battery University BU-502](https://batteryuniversity.com/article/bu-502-discharging-at-high-and-low-temperatures) and
  [Midtronics on cold weather](https://www.midtronics.com/blog/what-happened-to-a-car-battery-in-cold-weather/);
  [Discover Battery on cold cranking amps](https://discoverbattery.com/support/learning-center/application-101/starter-battery-specifications-cold-cranking-amps-cca);
  Delco Remy [24SI](https://www.finditparts.com/products/1881565/delco-remy-8600388) and
  [28SI](https://www.newindo.com/products/delco-remy/8600764) product listings; a
  [Szczecin Maritime University study of start-up wear](https://repository.am.szczecin.pl/handle/123456789/237); SAE
  2006-01-3413 as cited second-hand on an enthusiast forum (unverified); [Gel point (petroleum)](https://en.wikipedia.org/wiki/Gel_point_(petroleum))
  (Wikipedia; ASTM D975 is the authority). No primary Battery Council International figure was found; no source was found
  for winch and heater currents, so those loads are authored.
