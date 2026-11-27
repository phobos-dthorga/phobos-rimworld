# Vehicle features for modders

How to give your own Vehicle Framework vehicles D'thorga Framework's vehicle features (RimWorld 1.7): walk-in cabins, camps, hitches
and trailers, sling loads, litters, field kitchens, work rigs, aircraft over the map, remote control, fire-fighting and the rest. Most
need little or no XML: the Framework adds them by what your vehicle has. **The systems schema** (batteries, coolant, fittings, tunes,
inputs, conditions and effects) **has its own page**: [vehicle systems for modders](vehicle-systems-modding.md). The services outside
vehicles are on [D'thorga Framework for modders](framework-modding.md).

**Status (28 Nov 2026):** new, written from the code of D'thorga Framework 0.365.0 and the shipped XML; every comp and Def extension of
the vehicle features has a section. **Not yet tested in game** as another modder's vehicle would use it, and names may still change
before the first public release. For every field of every class, see the reference site's
[XML types](https://phobos-dthorga.github.io/phobos-rimworld/xml-types.html) page, rebuilt from the code on every change.

## Read this first (gotchas)

- **Not yet tested in game.** These features are built and shipped, but nothing here has been played by the owner as a modder's
  vehicle would use it. The schema may change before the first public release.
- **Several of these comps are added for you.** D'thorga Framework adds the hitch, the camp, the sling, the litters and the deck tent
  to a vehicle while Defs load, **only when the vehicle has a loadout** (see [How a vehicle gets its systems](vehicle-systems-modding.md#how-a-vehicle-gets-its-systems)).
  Every Vehicle Framework vehicle gets a default loadout by what it does, so usually you write nothing. The player can switch a mod's
  vehicles off in the settings, and then none of these comps is added.
- **Most features wait for a fitting or an upgrade node.** A comp on the Def gives the vehicle the button; the player still fits the
  awning, the cargo hook, the litter kit or the deck tent from the fittings window, or buys the node in Vehicle Framework's upgrade
  tree. A "node" in the tables below is always the `key` of a node in the vehicle's own upgrade tree (`Vehicles.CompProperties_UpgradeTree`).
- **Classes are named in full**: `Class="Phobos.VehicleSystems.CompProperties_PhobosHold"`. A Def name (a coupling, a fitting, a
  sound) is written bare.
- **Load after D'thorga Framework.** Its vehicle Defs (couplings, fittings, canvas walls, parachutes) live in its Vehicle Framework
  folder, so they exist only while Vehicle Framework is active. Put XML that names another mod's vehicles in an `IfModActive` folder.
- **Figures marked "authored" are balance choices**, not measurements. Use your own.
- **A work stat that starts at 0 must be given a value in `vehicleStats`**, or the work never happens: `Phobos_DrillRate`,
  `Phobos_DrillReach`, `Phobos_TipRate` and `Phobos_GeneratorOutput` all start at 0. List each stat in the `categories` of the parts
  that feed it, as the shipped trucks do.
- **Upgrades turn most features on**: a fire tank, a grab, a laboratory, a sensor are 0 until a node's `StatUpgrade` raises their stat.
  Node keys are saved in the game: never rename or remove one once players have it.
- **The table below lists every comp the Framework adds by itself**, and when to write one yourself.

## What D'thorga Framework adds by itself

Each of these is put on the Def while Defs load. **Leave them out of your XML** unless the last column says otherwise; a Def that
already has one keeps its own and gets no second.

| Comp | Given to | Write it yourself? |
| --- | --- | --- |
| `CompProperties_PhobosVehicleSystems` | Every vehicle a loadout claims | **Never**: it is built from the loadout. |
| `CompProperties_PhobosWorkStore` | Every vehicle with a loadout | No. |
| `CompProperties_PhobosDispensers` | A vehicle whose loadout's `avionics` has `countermeasures` true | No. |
| `CompProperties_PhobosRemoteControl` | A vehicle with the `RemoteKitMount` or `RouteKitMount` part role | No. |
| `CompProperties_PhobosMechLink` | A vehicle with the `RemoteKitMount` part role | No. |
| `CompProperties_PhobosRoute` | A vehicle with the `RouteKitMount` part role | No. |
| `CompProperties_PhobosSupplyRun` | A land vehicle with the `RouteKitMount` part role | Only for a vehicle that has no such role. |
| `CompProperties_PhobosRound` | A vehicle with `CompProperties_PhobosFlight` whose `onlyInDrops` is false | No. |
| `CompProperties_PhobosSmoke` | A vehicle whose turrets, or whose upgrade tree's turrets, carry `PhobosSmokeDischargerExtension` | No. |
| `CompProperties_PhobosBayTrip` | Every land vehicle with a loadout, trailers apart | No. |
| `CompProperties_PhobosFireFighting` | Every vehicle with a loadout, trailers apart | No. |
| `CompProperties_PhobosHull` | Every `VehicleBuildDef` (the frame), loadout or not | No. |
| `CompProperties_PhobosVehicleLoadOut` | Every storage building (`Building_Storage` or a subclass), from every mod | No. |
| `CompProperties_PhobosHitch` | Every land vehicle with a loadout, except a trailer and a loadout with `<noHitch>true</noHitch>` | **On a trailer** that should tow in turn (see [Hitches](#hitches-couplings-and-trailers)). |
| `CompProperties_PhobosCamp` | Every land vehicle with a loadout, and a trailer whose `CompProperties_PhobosTrailer` has `campDeck` or `campSide` on | No. |
| `CompProperties_PhobosCrewWater` (Dubs Bad Hygiene) | Every vehicle that has `CompProperties_PhobosCamp` and is not a trailer, while Dubs Bad Hygiene is active and not in its lite mode | No. |
| `CompProperties_PhobosSling` | A vehicle whose loadout has a `<sling>` block | No. |
| `CompProperties_PhobosLitter` | A vehicle whose loadout binds the `Cabin` part role | No. |
| `CompProperties_PhobosVehicleCabin` (a deck tent) | A Def with `PhobosFlatDeckExtension` and no cabin of its own | **Yes, for a walk-in cabin** (see [Walk-in cabins](#walk-in-cabins-and-deck-tents)). |

Every land loadout that inherits `PhobosVehicleLoadoutLandBase` (all the shipped land defaults do) appends the parts behind the
`RemoteKitMount` (a control radio bay) and `RouteKitMount` (a sensor bar) roles, so every land vehicle with a default loadout gets the
remote control, mech link, route and supply run comps. They stay idle until the player fits a kit.

## Walk-in cabins and deck tents

A walk-in cabin is a small room inside the vehicle that colonists walk into by right-clicking the vehicle. It is a pocket map made when
the first person goes in and removed once nobody is inside. A deck tent is the same kind of room, standing on a flat load bed while the
deck tent fitting is fitted. Types: `CompProperties_PhobosVehicleCabin` (the cabin) and `PhobosFlatDeckExtension` (a flat deck).

**Gotchas first:**

- **The cabin is behind the player's "Walk-in cabins" setting** (on by default), and only the player's own vehicles offer it.
- **Rows are written nose first**: the first row is the front of the cabin, the top of its map. Every row must be the same width.
- **Exactly one `E`** (the way out). A litter or a bed is two cells: the head (`L`, `B`) directly above its foot (`l`, `b`).
- **Most particular layout first.** The first layout whose `whenFitted` fittings are all fitted and whose `whenGranted` abilities are
  all granted is used. **The last layout must name no fittings**: it is the cabin as built (the game reports a config error otherwise).
- **A cabin shares the passenger seats' limit.** People seated and people in the cabin are counted together against the vehicle's
  passenger seats (seats that neither drive nor man a weapon); the cabin also holds no more than its own seats, litters and beds. With no
  passenger seats, only the cabin's places count. A deck tent does not share the seats' limit.
- **Nobody goes in while the vehicle is towed, off a map, or in the air on the map.** People inside count as aboard for its weight.
- **A flat deck only becomes a deck tent if nothing else gave the vehicle a cabin.** If your Def has its own cabin comp, the flat deck
  extension adds no tent.
- **A deck tent needs a roof mount.** The deck tent fittings (`PhobosFitting_DeckTent`, `PhobosFitting_DeckTentRoomy`) go on the
  `RoofMount` part role and are offered only on a Def carrying `PhobosFlatDeckExtension`. Every land vehicle's default loadout appends a
  roof mount; a trailer gets one only with `campDeck` on its trailer comp.

### A cabin with layouts

From the V-22 Osprey (`mods/PhobosVehicles/Defs/Phobos/Vehicles/Osprey/Vehicle_Osprey.xml`), trimmed: litters while a litter kit is
fitted, a ferry tank's cells blocked while that is fitted, and seats as built.

```xml
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosVehicleCabin">
    <layouts>
      <li>
        <!-- Litters by ability: the plain litter kit or the one with monitors both grant "Litter". -->
        <whenGranted>
          <li>Litter</li>
        </whenGranted>
        <rows>
          <li>S.S</li>
          <li>L.L</li>
          <li>l.l</li>
          <li>...</li>
          <li>.E.</li>
        </rows>
      </li>
      <li>
        <whenFitted>
          <li>PhobosFitting_FerryTank</li>
        </whenFitted>
        <rows>
          <li>TTT</li>
          <li>S.S</li>
          <li>.E.</li>
        </rows>
      </li>
      <li>
        <!-- As built: names no fitting, so it is the fallback. -->
        <rows>
          <li>S.S</li>
          <li>S.S</li>
          <li>.E.</li>
        </rows>
      </li>
    </layouts>
  </li>
</comps>
```

| Field | What it does (default) |
| --- | --- |
| `layouts` | The layouts, most particular first (empty; at least one is required). |
| `deckTent` | The cabin is a tent on the deck: it exists only while a layout's conditions hold (there is no "as built"), its air follows the outside, it does not share the passenger seats' limit, and danger outside wakes and empties it (false). You rarely set this: `PhobosFlatDeckExtension` makes one for you. |

Each layout (`Phobos.VehicleSystems.CabinLayoutSpec`):

| Field | What it does (default) |
| --- | --- |
| `whenFitted` | Fittings (`PhobosVehicleFittingDef` names) that must all be fitted (empty). |
| `whenGranted` | Abilities the vehicle's fittings must grant, by name: `Litter`, `DeckTent` (empty). |
| `rows` | The cabin's cells, nose first: `S` seat, `L` over `l` a litter, `B` over `b` a bed, `T` a tank (blocks the cell), `E` the way out, `.` floor (empty). |

**Layouts keyed by fittings need a loadout.** Fittings exist only on a vehicle with our systems. A cabin with only an as-built layout
works without them.

### A flat deck for a deck tent

Mark an open load bed and the Framework does the rest: it adds a deck-tent cabin whose one layout needs the `DeckTent` ability. From
`mods/PhobosVehicles/Mods/VanillaVehiclesExpanded/Patches/FlatDecks_VanillaVehiclesExpanded.xml` (another author's truck, marked by a
patch; nothing else of it changes):

```xml
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/Vehicles.VehicleDef[defName="VVE_Mule"]</xpath>
  <value>
    <li Class="Phobos.VehicleSystems.PhobosFlatDeckExtension">
      <rows>
        <li>B.B</li>
        <li>b.b</li>
        <li>.E.</li>
      </rows>
    </li>
  </value>
</Operation>
```

On your own Def, `<li Class="Phobos.VehicleSystems.PhobosFlatDeckExtension" />` in `modExtensions` takes the default two beds, as
D'thorga Vehicles' flatbed trailer does (`Vehicle_FlatbedTrailer.xml`).

| Field | What it does (default) |
| --- | --- |
| `rows` | The tent's inside, as a cabin layout: `B` over `b` a bed, `E` the flap, `.` floor (`B.B`, `b.b`, `.E.`: two beds). |

**In play**: the player fits the deck tent kit, chooses who sleeps there on the vehicle, and they walk in at bedtime. Its warmth follows
the air where the vehicle stands, a little warmer (authored figures in the rules Def). **The vehicle does not drive off while anyone
is inside.**

---

## Camps

A parked land vehicle can set out a camp beside it: the awning and roof tent it has fitted, and the swags, bedrolls, tables, stools,
lanterns and other camp gear packed in its cargo. A colonist sets it out and packs it away again. Types: `CompProperties_PhobosCamp`
(the camp order), `PhobosCampGearExtension` (a building the camp sets out) and `PhobosBedMemoryExtension` (a bed's waking memory).

**Gotchas first:**

- **You do not write `CompProperties_PhobosCamp`.** It is added to every land vehicle with a loadout, and to a trailer whose
  `CompProperties_PhobosTrailer` has `campDeck` or `campSide` on. It has no fields. Its buttons show only when there is something to set
  out.
- **Camp gear must be a minifiable building.** The camp looks for packed buildings (`MinifiedThing`) in the vehicle's cargo whose inner
  Def carries `PhobosCampGearExtension`. Give your building a `minifiedDef`, as the game's bedroll has.
- **The vehicle will not drive off with its camp out.** Packing never deletes anything: a bed in use waits for its sleeper.
- **A bed memory works on any bed anywhere**, not only at a camp. It is given after the game's own bed thoughts, so it can answer the
  game's "slept outside". Wild people never get it.

### Your own camp gear

From the camp stool and the swag (`mods/PhobosFramework/Mods/VehicleFramework/Common/Defs/Phobos/Framework/VehicleSystems/Accessories_Camp.xml`),
trimmed to the extensions:

```xml
<!-- The camp stool: set beside a camp table, facing it. -->
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosCampGearExtension">
    <work>30</work>
    <besideTable>true</besideTable>
  </li>
</modExtensions>

<!-- The swag: camp gear, and a mood memory on a fair, mild night under an open sky. -->
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosCampGearExtension">
    <work>150</work>
  </li>
  <li Class="Phobos.VehicleSystems.PhobosBedMemoryExtension">
    <memory>PhobosSleptUnderTheStars</memory>
    <openSkyOnly>true</openSkyOnly>
    <fairWeatherOnly>true</fairWeatherOnly>
    <comfortableOnly>true</comfortableOnly>
  </li>
</modExtensions>
```

Another author's building is marked by a patch, as the Framework marks the game's bedrolls
(`mods/PhobosFramework/Mods/VehicleFramework/Common/Patches/CampGear.xml`):

```xml
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/ThingDef[defName="Bedroll" or defName="BedrollDouble"]</xpath>
  <value>
    <li Class="Phobos.VehicleSystems.PhobosCampGearExtension">
      <work>100</work>
    </li>
  </value>
</Operation>
```

`PhobosCampGearExtension`:

| Field | What it does (default) |
| --- | --- |
| `work` | The work of laying one out or rolling it up, at construction speed (150, authored). |
| `besideTable` | A seat: set down beside a camp table where one stands, facing it, so people eat there (false). |
| `besideBedHead` | A bed's machine (a sleep accelerator): set against the head of a camp bed, facing it. One to a bed; with no bed free it stays in the cargo (false). |

`PhobosBedMemoryExtension` (on a bed's Def):

| Field | What it does (default) |
| --- | --- |
| `memory` | The `ThoughtDef` the sleeper wakes with (none: nothing happens). Use a memory thought, as the shipped `PhobosSleptUnderTheStars` (+5, one day) is. |
| `openSkyOnly` | Only with no roof over any cell of the bed: the stars, not an awning (false). |
| `fairWeatherOnly` | Only with no rain or snow falling on waking (false). |
| `comfortableOnly` | Only when the night was neither too cold nor too hot (the game's own "slept in the cold" and "slept in the heat") (false). |

**What else goes into a camp:** the awning, side fly, rear tent, roof tent and fold-out camper are fittings (`Accessories_Camp.xml`)
on the `SideRail` and `RoofMount` part roles, which every land vehicle's default loadout appends. A vehicle whose loadout you write
yourself keeps them only if it inherits those part roles.

---

## Hitches, couplings and trailers

Any land vehicle with a loadout can tow a trailer on its hitch: a colonist couples it, and the trailer stays a vehicle on the map,
following behind. Couplings decide which hitch takes which trailer; an articulated vehicle is two vehicles joined for good at a pivot;
and the recovery winch drags in things marked as winchable. Types: `CompProperties_PhobosHitch`, `PhobosHitchPointExtension`,
`PhobosCouplingsExtension`, `PhobosCouplingDef`, `CompProperties_PhobosArticulated` and `PhobosWinchableExtension`.

**Gotchas first:**

- **You do not write `CompProperties_PhobosHitch` on an ordinary vehicle.** It is added to every land vehicle with a loadout, except a
  trailer and a vehicle whose loadout says `<noHitch>true</noHitch>`. It has no fields.
- **A trailer never gets a hitch by itself. Write `CompProperties_PhobosHitch` on a trailer** that should tow in turn (a line of tows):
  D'thorga Farming's implements and the Thane T400's rear half do.
- **Towing needs our drivetrain.** A vehicle whose loadout gives it no ground drivetrain is refused, unless the player turns on
  **Vehicles without the drivetrain model can tow** (off by default), which tows on a plain rule.
- **A trailer is any vehicle with `Phobos.VehicleSystems.CompProperties_PhobosTrailer`** (see [A trailer](#a-trailer)). Its
  `hitchToAxle` and `axleToCentre` say where its drawbar's eye and axle sit; measure them from the picture so the eye sits on the hitch.
- **Couplings are enforced, but a side that names none joins anything.** Every hitch and trailer built before couplings stays universal.
- **A permanent coupling joins only a hitch that names it**, never a universal hitch, never by an adapter. A hitch that names only
  permanent couplings takes nothing else.

### A trailer

`CompProperties_PhobosTrailer` makes a vehicle a trailer: no engine and no seats of its own, towed by a vehicle with a hitch. **Write it in
your trailer's own Def**; nothing adds it. From D'thorga Vehicles' flatbed trailer
(`mods/PhobosVehicles/Defs/Phobos/Vehicles/FlatbedTrailer/Vehicle_FlatbedTrailer.xml`):

```xml
<li Class="Phobos.VehicleSystems.CompProperties_PhobosTrailer">
  <hitchToAxle>4.48</hitchToAxle>
  <axleToCentre>1.23</axleToCentre>
  <jackKnifeDegrees>70</jackKnifeDegrees>
  <drawbarPart>Drawbar</drawbarPart>
  <campDeck>true</campDeck>   <!-- a camper trailer: a roof tent and an awning on its deck -->
</li>
```

| Field | What it does (default) |
| --- | --- |
| `hitchToAxle` | From the hitch to the trailer's axle, in cells: the drawbar and the front of the deck (3.2). **Above 0**, or a config error |
| `axleToCentre` | From the axle forward to the middle of the trailer's picture, in cells; the middle of a tandem pair for two axles (0.2). The trailer's middle stands `hitchToAxle - axleToCentre` behind the hitch |
| `jackKnifeDegrees` | The most it may stand turned from the vehicle towing it, in degrees; 0 holds it straight behind, as an implement (70). Authored, not a force. 0 to 180 |
| `drivenAxle` | An articulated vehicle's rear half: its wheels are driven, so its weight counts as grip, not as weight towed (false). See [Articulated vehicles](#articulated-vehicles) |
| `drawbarPart` | The part whose loss leaves nothing to hitch to; empty for none (`Drawbar`) |
| `setUpWork`, `packUpWork` | The work for a colonist to set a working trailer up and pack it up, at their construction speed (1500 and 1000). Authored. **Above 0** |
| `campDeck` | Its deck takes camp gear: a roof mount and a side rail for a roof tent and an awning, and the camp order once it stands unhitched. For a plain deck (false) |
| `campSide` | Its side takes camp gear: a side rail for an awning or a tent, and the camp order; nothing on top. For a tank, a kitchen or a shower trailer (false) |

- **Measure `hitchToAxle` and `axleToCentre` from the picture**, so the drawbar's eye sits on the hitch.
- A trailer has none of our vehicle systems, but a parked one still lays its own pipes beneath it, and takes them up when hitched.
- **A loadout claims a trailer only when its selector says `<trailer>Yes</trailer>` or `Any`** (see
  [Loadout fields](vehicle-systems-modding.md#loadout-fields)).

### Where the hitch sits

```xml
<!-- The Thane T400's front half (mods/PhobosFarming/Common/Defs/Phobos/Farming/Vehicle_TractorT400.xml). -->
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosHitchPointExtension">
    <behindCentre>2.31</behindCentre>
  </li>
</modExtensions>
```

| Field | What it does (default) |
| --- | --- |
| `behindCentre` | From the middle of the vehicle's footprint back to the hitch, in cells (-1: just behind its rear edge, half the footprint's length plus the rules Def's `hitchGapCells`, 0.3). |

Use it only when the picture's hitch is somewhere other than just behind the rear edge.

### Couplings

A coupling kind is a Def of its own. The Framework ships `PhobosCoupling_Drawbar`, `PhobosCoupling_Pintle`,
`PhobosCoupling_PickUpHitch`, `PhobosCoupling_Ball`, `PhobosCoupling_HeavyBall`, `PhobosCoupling_ThreePoint` and the permanent
`PhobosCoupling_ArticulatedPivot` (`Couplings.xml`). Its `label` and `description` are the player's words; its `defName` is what is matched.

```xml
<Phobos.VehicleSystems.PhobosCouplingDef>
  <defName>MyMod_FifthWheel</defName>
  <label>fifth wheel</label>
  <description>A turntable plate on the tractor that takes a semi-trailer's kingpin.</description>
</Phobos.VehicleSystems.PhobosCouplingDef>
```

`PhobosCouplingDef`:

| Field | What it does (default) |
| --- | --- |
| `permanent` | Never let go by hand: an articulated vehicle's pivot. Neither side offers Unhitch; only the loss of one half parts them (false). |

A vehicle names its couplings in `PhobosCouplingsExtension`. From D'thorga Farming (`Vehicle_Tractors.xml`, `Vehicle_ChaserBin.xml`):

```xml
<!-- A tractor: its hitch takes a drawbar and the three-point linkage. -->
<li Class="Phobos.VehicleSystems.PhobosCouplingsExtension">
  <hitch>
    <li>PhobosCoupling_Drawbar</li>
    <li>PhobosCoupling_ThreePoint</li>
  </hitch>
</li>

<!-- A chaser bin: towed by a drawbar. -->
<li Class="Phobos.VehicleSystems.PhobosCouplingsExtension">
  <towedBy>PhobosCoupling_Drawbar</towedBy>
</li>
```

| Field | What it does (default) |
| --- | --- |
| `hitch` | The couplings its hitch has (empty: it takes any trailer). |
| `towedBy` | The coupling it is towed by, as a trailer (none: any hitch takes it). |
| `adapters` | Adapters fitted to its hitch by upgrades (empty). Each is a `node` (the upgrade node that fits it) and `takes` (the coupling it then takes). Both are required. No shipped vehicle uses one yet. |

### Articulated vehicles

An articulated vehicle is two vehicles: a front half that drives, and a rear half that is a trailer of ours on a permanent coupling. When
the front half is first on a map it sets its rear half down behind it and couples it, once. With no room behind, it says so and tries
again every 250 ticks. From the Thane T400 (`Vehicle_TractorT400.xml`), trimmed:

```xml
<!-- The front half: its hitch is the pivot, and holds only its own rear half. -->
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosCouplingsExtension">
    <hitch>
      <li>PhobosCoupling_ArticulatedPivot</li>
    </hitch>
  </li>
</modExtensions>
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosArticulated">
    <rearHalf>Phobos_TractorT400StandardRear</rearHalf>
  </li>
</comps>

<!-- The rear half: a trailer whose wheels drive, towed only by the pivot, with a hitch of its own for implements. -->
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosCouplingsExtension">
    <towedBy>PhobosCoupling_ArticulatedPivot</towedBy>
    <hitch>
      <li>PhobosCoupling_Drawbar</li>
      <li>PhobosCoupling_PickUpHitch</li>
      <li>PhobosCoupling_ThreePoint</li>
    </hitch>
  </li>
</modExtensions>
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosTrailer">
    <hitchToAxle>1.6</hitchToAxle>
    <axleToCentre>-0.09</axleToCentre>
    <jackKnifeDegrees>40</jackKnifeDegrees>
    <drivenAxle>true</drivenAxle>
  </li>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosHitch" />
</comps>
```

`CompProperties_PhobosArticulated`:

| Field | What it does (default) |
| --- | --- |
| `rearHalf` | The rear half's `VehicleDef`: a trailer of ours, towed by a permanent coupling this vehicle's hitch has (none; required). |

**Needs:** the front half's own hitch (from its loadout) naming the permanent coupling; the rear half's `CompProperties_PhobosTrailer`
(with `drivenAxle` on, so its weight counts as grip, not towed weight) and `towedBy` naming the same coupling. **If the front half has
no hitch, no rear half is made, and nothing says so**: the player switching the front half's vehicles off in the
settings, or a loadout with `noHitch`, does it. Check the pair in dev mode after any change to the loadout.

### Things the recovery winch can drag

A land vehicle with the recovery winch fitted (`PhobosFitting_RecoveryWinch`, on the `RecoveryMount` part role) drags in vehicles,
wrecks, chunks of rock and carcasses already. Mark any other building it may drag, with what it weighs. From
`mods/PhobosFramework/Mods/VehicleFramework/Common/Patches/Phobos/Framework/Winchable.xml`:

```xml
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/ThingDef[defName="ShipChunk" or defName="ShipChunk_Mech"]</xpath>
  <value>
    <li Class="Phobos.VehicleSystems.PhobosWinchableExtension">
      <kilograms>3000</kilograms>
    </li>
  </value>
</Operation>
```

| Field | What it does (default) |
| --- | --- |
| `kilograms` | Its weight on the winch, in kilograms (1000). The shipped 3,000 kg for a ship chunk is the agent's figure, for the owner to revise. |

**Use it for things the game gives no useful mass.** Anything not marked is weighed by the game's own mass for the whole stack. Nothing
alive can be winched.

---

## Sling loads

A helicopter with a cargo hook fitted and a sling set in its cargo can lift a vehicle (or a hull, a packed building, a stack) parked
nearby, carry it in flight and set it down where it lands. Type: `CompProperties_PhobosSling`.

**Gotchas first:**

- **You do not write `CompProperties_PhobosSling`.** It has no fields. It is added to any vehicle whose loadout has a `<sling>` block.
  Rate the hook in the loadout instead.
- **The hook needs a `HookMount` part role** in the loadout: the cargo hook fitting (`PhobosFitting_CargoHook`) goes there.
- **A sling set** (`PhobosVehicleSystems_SlingSet`) **must be in the helicopter's cargo**, or it refuses.
- **It lifts only the player's vehicles, never an aircraft**, nothing under a roof, moving, hitched or under a lord's orders.
- **Another mod's own airlift wins.** Name its package ID in `standDownIfActive`, and while it is active our hook mount and sling stand
  down on that aircraft.

From the Mi-24 Hind's loadout (`mods/PhobosVehicles/Mods/HindHelicopter/Defs/Loadouts_Hind.xml`), trimmed to the sling:

```xml
<Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
  <!-- ... the rest of the loadout ... -->
  <sling>
    <ratedKilograms>2400</ratedKilograms>
  </sling>
  <partRoles>
    <li>
      <!-- Appended, so a hook is lost with its mount. -->
      <role>HookMount</role>
      <add>
        <key>PhobosHookMount</key>
        <countsTowardDestruction>false</countsTowardDestruction>
        <label>hook mount</label>
        <health>40</health>
        <cellsOf>RotorDrive</cellsOf>
      </add>
    </li>
  </partRoles>
</Phobos.VehicleSystems.PhobosVehicleLoadoutDef>
```

The loadout's `sling` block:

| Field | What it does (default) |
| --- | --- |
| `ratedKilograms` | The hook's rated load in kilograms (0). Name your source in a comment. An upgrade's `Phobos_SlingKilograms` stat raises it (the larger wins). On a hot day it lifts less, by the player's setting. |
| `standDownIfActive` | Package IDs of mods that bring their own airlift for this aircraft: while one is active, ours stands down here (empty). |

---

## Litters

A vehicle with the litter kit fitted to its cabin carries downed people lying on litters, not in seats, and anyone in the crew who can
doctor tends them while it stands. Type: `CompProperties_PhobosLitter`.

**Gotchas first:**

- **You do not write `CompProperties_PhobosLitter`.** It has no fields. It is added to any vehicle whose loadout binds the `Cabin` part
  role. The default land loadout finds a part named `Cab`, or whose key holds `cabin`, `upholstery` or `roof`, and never appends one.
- **The litters come from a fitting or a node.** The litter kit (`PhobosFitting_LitterKit`, or the monitored kit) goes on the `Cabin`
  role and grants `Litter`, bringing the rules Def's `litterSlots` (4, authored). An upgrade node can bring its own with the
  `Phobos_Litters` vehicle stat (the C-27J's 36 stretchers). The larger count is used.
- **A walk-in cabin's `L`/`l` cells are separate.** If your vehicle also has a walk-in cabin, give its litter cells to a layout with
  `<whenGranted><li>Litter</li></whenGranted>`, as the Osprey does, so they appear when the kit is fitted.

Binding the cabin in a loadout, from the Hind (`Loadouts_Hind.xml`), where the troop cabin is the fuselage:

```xml
<li>
  <role>Cabin</role>
  <find>
    <li>
      <key>Body</key>
    </li>
  </find>
</li>
```

A node that brings litters, from the C-27J (`mods/PhobosVehicles/Defs/Phobos/Vehicles/C27J/UpgradeTree_C27J.xml`), trimmed:

```xml
<upgrades>
  <li Class="Vehicles.StatUpgrade">
    <vehicleStats>
      <li>
        <def>Phobos_Litters</def>
        <value>36</value>
      </li>
    </vehicleStats>
  </li>
</upgrades>
```

---

## A hold for vehicles

A transport aircraft can take small vehicles aboard over its ramp. They are held off the map and weighed as its load; their crew rides
in its seats. They leave by the ramp, or by parachute (see Airdrops). Type: `CompProperties_PhobosHold`.

**Gotchas first:**

- **Only the player's vehicles, never an aircraft, and nothing nested**: a vehicle carrying a sling load, holding vehicles itself, or
  towing is refused.
- **The vehicles' crews must fit in the carrier's passenger seats** (seats that neither drive nor man a weapon).
- **A vehicle is taken aboard only from behind the ramp**: within `rampReachCells` of the cell just past the carrier's tail.
- **Destroyed, the carrier puts its held vehicles out beside it.**

From the Osprey (`mods/PhobosVehicles/Patches/Osprey/Airdrop_Osprey.xml`):

```xml
<Operation Class="PatchOperationAdd">
  <xpath>Defs/Vehicles.VehicleDef[defName="Phobos_Osprey"]/comps</xpath>
  <value>
    <li Class="Phobos.VehicleSystems.CompProperties_PhobosHold">
      <maxSize>(2, 4)</maxSize>
      <maxKg>3500</maxKg>
      <slots>1</slots>
      <rampReachCells>6</rampReachCells>
    </li>
  </value>
</Operation>
```

| Field | What it does (default) |
| --- | --- |
| `maxSize` | The largest footprint it takes, in cells, either way round ((2, 3)). |
| `maxKg` | The most it carries in all, kilograms (2500). |
| `slots` | How many vehicles at once (1). |
| `rampReachCells` | How far behind its ramp a vehicle may be taken aboard from, in cells (6, authored). |

`maxSize`, `maxKg` and `slots` must be positive (a config error otherwise).

---

## Field runs and bulk holds

A field run drives a vehicle across an area row by row by itself while an implement works the strip it covers; when the hold is full it
drives to the nearest bin or cart to pour, then carries on. A bulk hold carries loose goods (grain) by product rather than as cargo,
pours them into another hold in reach or onto the ground, and takes loose heaps in. Types: `CompProperties_PhobosFieldRun` and
`CompProperties_PhobosBulkHold`.

**Gotchas first:**

- **A field run needs a worker, and the worker is C#.** It is a comp implementing `Phobos.VehicleSystems.IVehicleFieldWorker`, on the
  vehicle itself (a combine's harvester) or on an implement behind it on the hitch. D'thorga Farming's harvester and implements are the
  shipped workers. With none, the vehicle offers no field order.
- **The worker decides which areas it works** (a growing zone marked for machines, in D'thorga Farming) and what a row's work is.
- **A bulk hold pours only while it, and whatever tows it, stands still**, and only within its auger's reach.
- **Loose goods are what a crop yields as its harvest**, plus anything whose Def carries `Phobos.Framework.Bulk.PhobosBulkGoodsExtension`
  (seed grain). Nothing else ever goes into a hold.
- **Its load weighs on the engine and the tow as cargo would**, and is spilled where it stood if the vehicle is destroyed.
- The run drives as a gathering run does; see [Gathering runs](vehicle-systems-modding.md#gathering-runs).

From D'thorga Farming's combine (`mods/PhobosFarming/Common/Defs/Phobos/Farming/Vehicle_Combine.xml`), trimmed:

```xml
<comps>
  <!-- Drives a paddock row by row; the harvester (D'thorga Farming's worker) cuts what its strip covers. -->
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosFieldRun" />
  <!-- The grain tank: one product at a time, 9 tonnes, a third more with the tank extension (authored). -->
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosBulkHold">
    <capacityKg>9000</capacityKg>
    <mixesProducts>false</mixesProducts>
    <pourKgPerSecond>100</pourKgPerSecond>
    <reachCells>2</reachCells>
    <capacityUpgrades>
      <li>
        <node>PhobosCombineTankExtension</node>
        <factor>1.33</factor>
      </li>
    </capacityUpgrades>
  </li>
</comps>
```

A towed chaser bin (`Vehicle_ChaserBin.xml`) mixes products and has a faster auger by upgrade:

```xml
<li Class="Phobos.VehicleSystems.CompProperties_PhobosBulkHold">
  <capacityKg>12000</capacityKg>
  <mixesProducts>true</mixesProducts>
  <pourKgPerSecond>120</pourKgPerSecond>
  <reachCells>2</reachCells>
  <pourUpgrades>
    <li>
      <node>PhobosChaserBinFastAuger</node>
      <factor>1.5</factor>
    </li>
  </pourUpgrades>
</li>
```

`CompProperties_PhobosFieldRun`:

| Field | What it does (default) |
| --- | --- |
| `stuckTicks` | How long a drive may stand still before it counts as stuck, in ticks (600, authored). |
| `failedRowsToGiveUp` | How many rows in a row it may fail to reach before it gives the run up (4, authored). |
| `stopForEnemies` | Whether enemies threatening the map end the run (true). |
| `holdsToTry` | How many bins it tries, nearest first, when its hold is full (4, authored). |

`CompProperties_PhobosBulkHold`:

| Field | What it does (default) |
| --- | --- |
| `capacityKg` | How much it holds, kilograms, before upgrades (9000, authored). |
| `mixesProducts` | Whether it takes several products at once: a combine's tank keeps to one, a chaser bin need not (false). |
| `pourKgPerSecond` | How fast its auger pours, kilograms a second (100, authored). |
| `reachCells` | How far its auger reaches beyond its footprint, in cells (2). |
| `capacityUpgrades` | Upgrades that enlarge it: each a `node` and a `factor` (1); the capacity is multiplied by each one unlocked (empty). |
| `pourUpgrades` | Upgrades that make its auger faster, written the same way (empty). |

---

## Setting out a trailer

A working trailer is set up by a colonist, and sets real buildings out beside itself: a canvas room with a range, a kettle, a cold box,
a shelf and a lamp, for the Billy field kitchen. The game's own cooking, washing and storage then work on them unchanged; packing up lifts
them back aboard. Types: `CompProperties_PhobosSetOut` (what it sets out), `CompProperties_PhobosTrailerSetUp` (a plain set-up),
`PhobosSetOutPowerExtension` (what power does for a piece) and `CompProperties_PhobosSetOutLight` (a lamp lit by the trailer's power).

**Gotchas first:**

- **A set-out needs a set-up to follow.** It is one part of the trailer's set-up: either its tank's (`CompProperties_PhobosTank`, as the
  shower trailer does) or, for a trailer with no tank or rig, `CompProperties_PhobosTrailerSetUp` (as the Billy does). With neither,
  nothing ever sets it out.
- **It is for trailers.** A colonist sets a trailer up by hand, at the trailer comp's `setUpWork` and `packUpWork` (1500 and 1000,
  authored). Nothing is set up while the trailer is towed.
- **Cells are in the trailer's own frame.** `x` counts out from the side the pieces go (0 is the column touching the trailer); `z` runs
  along the trailer towards its front, in the trailer's own cells (a 3 by 8 trailer's run from -3 to 4). It tries the driver's left first,
  and the right if the left is blocked.
- **A piece's `from` and `to` must cover exactly its Def's footprint** at the rotation its facing gives, or it is left out with a warning
  in the log.
- **Every piece must be a building.** A room needs `wallDef`, `doorDef` and `roofDef`; `setOutLabel` is required (config errors
  otherwise). The Framework ships `Phobos_CanvasWall`, `Phobos_CanvasDoor`, `Phobos_CanvasRoof` and `Phobos_PowerPoint`.
  **`PhobosVehicles_TrailerTentPitch` is D'thorga Vehicles' sound**, not the Framework's: name your own unless you depend on it.
- **Make pieces that cannot be built or taken down by hand** (`<deconstructible>false</deconstructible>`), as the Billy's do: the
  trailer pitches them and holds them while packed, so a range keeps its fuel and bills and a cold box its contents.
- **Nothing of the player's is deleted.** What lies inside a room stays on the ground; a trailer destroyed while set up leaves its pieces
  standing as ordinary buildings.

From the Billy field kitchen (`mods/PhobosVehicles/Defs/Phobos/Vehicles/FieldKitchen/Vehicle_FieldKitchen.xml`), trimmed:

```xml
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosTrailer">
    <hitchToAxle>4.88</hitchToAxle>
    <axleToCentre>1.17</axleToCentre>
    <campSide>true</campSide>
    <setUpWork>3000</setUpWork>
    <packUpWork>2000</packUpWork>
  </li>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosTrailerSetUp" />
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosSetOut">
    <setOutLabel>the kitchen tent</setOutLabel>
    <pitchSound>PhobosVehicles_TrailerTentPitch</pitchSound>
    <wallDef>Phobos_CanvasWall</wallDef>
    <doorDef>Phobos_CanvasDoor</doorDef>
    <roofDef>Phobos_CanvasRoof</roofDef>
    <powerPointDef>Phobos_PowerPoint</powerPointDef>
    <powerPointCell>(6, 0)</powerPointCell>
    <idleWatts>50</idleWatts>
    <rooms>
      <li>
        <from>(0, -3)</from>
        <to>(5, 4)</to>
        <door>(2, 4)</door>
      </li>
      <!-- An annex pitched against the kitchen's front once its upgrade is bought; the shared wall's door joins them. -->
      <li>
        <from>(0, 4)</from>
        <to>(5, 9)</to>
        <door>(2, 9)</door>
        <node>PhobosBillyDiningAnnex</node>
      </li>
    </rooms>
    <pieces>
      <li>
        <def>Phobos_BillyRange</def>
        <from>(4, -2)</from>
        <to>(4, -1)</to>
        <facing>Inward</facing>
      </li>
      <li>
        <def>Shelf</def>
        <stuff>Steel</stuff>
        <from>(1, -2)</from>
        <to>(1, -1)</to>
        <facing>Outward</facing>
        <storeOnly>FoodMeals</storeOnly>
      </li>
      <li>
        <def>Phobos_BillyLamp</def>
        <from>(4, 3)</from>
        <to>(4, 3)</to>
      </li>
    </pieces>
  </li>
</comps>
```

`CompProperties_PhobosSetOut`:

| Field | What it does (default) |
| --- | --- |
| `pieces` | The buildings it sets out (empty). It must have pieces or rooms. |
| `rooms` | Canvas rooms: walls on the edge of each rectangle, one door, a roof over the inside (empty). Rooms may share a wall; a door in either wins there. |
| `wallDef`, `doorDef`, `roofDef` | The walls, door and roof of its rooms (none; all three needed with any room). Made new each time. |
| `powerPointDef` | A power point set out with the pieces, for pieces that take power (none). |
| `powerPointCell` | Where the power point goes, in the trailer's frame ((1, 5)). |
| `idleWatts` | What the pieces draw with nothing working, in watts: lights, a cold box (0, authored). |
| `pitchSound` | Heard as the tent is pitched and struck (none). |
| `setOutLabel` | The words for what it sets out, as "the kitchen tent", in refusals and messages (empty; required, translated). |

Each piece (`Phobos.VehicleSystems.SetOutPiece`):

| Field | What it does (default) |
| --- | --- |
| `def` | The building (none; required). |
| `stuff` | Its material, for a building made from stuff (the game's default stuff). |
| `from`, `to` | The cells it must cover, in the trailer's frame. |
| `facing` | Which way its working side looks: `Inward` (towards the trailer), `Outward`, `Front` or `Back` (`Inward`). A chair's rotation is the way its sitter faces, so a chair that faces outward sits looking in. |
| `node` | Set out only while this upgrade node is unlocked (empty: always). |
| `storeOnly` | A storage piece takes only this `ThingCategoryDef` at first; the player may change it (none). |

Each room (`Phobos.VehicleSystems.SetOutRoom`): `from`, `to`, `door` (cells, as a piece's) and `node` (as a piece's).

`CompProperties_PhobosTrailerSetUp` has no fields: it gives a trailer with no tank or rig its **Set up** and **Pack up** buttons, and its
set-out does the rest.

### Power for set-out pieces

Plugged into a powered grid through its power point, the trailer draws its `idleWatts` plus what each working piece draws. From the
Billy's range (`mods/PhobosVehicles/Defs/Phobos/Vehicles/FieldKitchen/Buildings_FieldKitchen.xml`):

```xml
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosSetOutPowerExtension">
    <speedWhenPowered>1.25</speedWhenPowered>
    <wattsWorking>450</wattsWorking>
  </li>
</modExtensions>
```

| Field | What it does (default) |
| --- | --- |
| `speedWhenPowered` | The piece's work-table speed is multiplied by this while its trailer is plugged in (1; the Billy's 1.25 is authored). |
| `wattsWorking` | What it draws while working, in watts (0, authored). |

- **`speedWhenPowered` works on any work table** set out by a trailer: the Framework adds it to the game's work-table speed stat, so the
  bill's speed and its explanation agree.
- **`wattsWorking` counts only for a piece that reports its draw**: the Framework's `Building_PhobosFieldStove` (only while it cooks) and
  `Building_PhobosColdBox` (always), or a piece carrying `Phobos.VehicleSystems.Hygiene.CompProperties_PhobosHeaterPowerSaving` (always,
  while set out; see [Water and waste with Dubs Bad Hygiene](#water-and-waste-with-dubs-bad-hygiene)). Anything else needs C#
  (`ISetOutLoad`, on the building class or on one of its comps).

### A lamp lit by the trailer's power

```xml
<!-- The Billy's tent lamp (Buildings_FieldKitchen.xml), trimmed. -->
<tickerType>Rare</tickerType>
<comps>
  <li Class="CompProperties_Glower">
    <glowRadius>9</glowRadius>
    <glowColor>(217,217,208,0)</glowColor>
  </li>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosSetOutLight" />
</comps>
```

`CompProperties_PhobosSetOutLight` has no fields. **The lamp needs a `CompProperties_Glower` and a rare ticker**: it checks on each rare
tick whether its trailer is plugged in, and lights only then.

---

## Field kitchens and caravan cooking

A field stove cooks everything a stove at home cooks; a recipe can wait for a trailer's upgrade; and a field kitchen carried in a caravan
cooks raw food from its packs into meals once a night. Types: `PhobosCopyStoveRecipesExtension`, `PhobosFieldStoveSoundsExtension`,
`PhobosRecipeNodeExtension`, `PhobosKeepsIngredientsExtension` and `PhobosCaravanCookingExtension`.

**Gotchas first:**

- **`PhobosFieldStoveSoundsExtension` needs `<thingClass>Phobos.VehicleSystems.Building_PhobosFieldStove</thingClass>`**: only that class
  plays the sounds. The same class gives the trailer's power point its working draw.
- **`PhobosRecipeNodeExtension` needs `<workerClass>Phobos.VehicleSystems.RecipeWorker_PhobosNeedsNode</workerClass>`** on the same
  recipe. Without it the recipe is always offered.
- **Recipe copying runs once at start-up**, from the tables the game's cooking work giver lists (`DoBillsCook`), found by that list,
  never by name. A recipe a table lists for itself stays its own; the stove is only added to who may cook it.
- **Caravan cooking needs both `meal` and `recipe`**: their defaults are none, and with either missing nothing is cooked. It is behind
  the player's **Field kitchens cook in a caravan** setting (on by default).

### A stove that cooks what a stove cooks

From the Billy's range (`Buildings_FieldKitchen.xml`) and the kitchen box (`Accessories_Camp.xml`):

```xml
<!-- The range: every cooking table's recipes, and its burners' loop while cooking. -->
<thingClass>Phobos.VehicleSystems.Building_PhobosFieldStove</thingClass>
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosCopyStoveRecipesExtension" />
  <li Class="Phobos.VehicleSystems.PhobosFieldStoveSoundsExtension">
    <workingSound>PhobosVehicles_FieldRangeBurner</workingSound>
  </li>
</modExtensions>

<!-- The kitchen box: and Vanilla Brewing Expanded's teas and coffees, where that mod is loaded. -->
<li Class="Phobos.VehicleSystems.PhobosCopyStoveRecipesExtension">
  <alsoRecipesOf>
    <li MayRequire="vanillaexpanded.vbrewecandt">VBE_EspressoMachineBuilding</li>
  </alsoRecipesOf>
</li>
```

`PhobosCopyStoveRecipesExtension`:

| Field | What it does (default) |
| --- | --- |
| `alsoRecipesOf` | Benches whose recipes it takes as well (empty). Name them with `MayRequire`, so a bench that is not loaded is left out. |

`PhobosFieldStoveSoundsExtension`:

| Field | What it does (default) |
| --- | --- |
| `workingSound` | The burners' loop, from a batch's start until the cook stops (none). |
| `startSound` | A one-off as a batch starts: the kettle's lid (none). |

### A recipe that waits for an upgrade

From the Billy's twenty-meal batch (`Recipes_FieldKitchen.xml`) and the fine paste bake (`Meals_PasteBake.xml`), trimmed:

```xml
<RecipeDef>
  <defName>Phobos_CookMealSimpleTwenty</defName>
  <workerClass>Phobos.VehicleSystems.RecipeWorker_PhobosNeedsNode</workerClass>
  <!-- ... -->
  <modExtensions>
    <li Class="Phobos.VehicleSystems.PhobosRecipeNodeExtension">
      <node>PhobosBillyBigKettle</node>
    </li>
  </modExtensions>
</RecipeDef>

<RecipeDef>
  <defName>PhobosCook_PasteBakeFine</defName>
  <workerClass>Phobos.VehicleSystems.RecipeWorker_PhobosNeedsNode</workerClass>
  <!-- ... -->
  <modExtensions>
    <li Class="Phobos.VehicleSystems.PhobosKeepsIngredientsExtension" />
    <li Class="Phobos.VehicleSystems.PhobosRecipeNodeExtension">
      <node>PhobosBillyPasteKitchen</node>
      <anywhereElse>true</anywhereElse>
    </li>
  </modExtensions>
</RecipeDef>
```

`PhobosRecipeNodeExtension`:

| Field | What it does (default) |
| --- | --- |
| `node` | The upgrade node the trailer that set out this table must have unlocked (empty: no gate). |
| `anywhereElse` | Also offered at any table no trailer set out, such as a stove at home: the node then gates it on a trailer only (false). |

The bill menu says which upgrade is missing.

### Meals made from meals

`PhobosKeepsIngredientsExtension` has no fields. On a recipe whose ingredients are meals, it passes what those meals were made from into
the product, as well as the meals themselves. Without it, paste made from human meat would come out of the oven clean. It acts only on
recipes that carry it, at any table.

### Cooking in a caravan

Put `PhobosCaravanCookingExtension` on a field kitchen's `VehicleDef` (driven, or towed anywhere along a line of tows), or on a
minifiable building carried packed in the cargo (the kitchen box). A field kitchen on wheels is used before a packed box. From the Billy
(`Vehicle_FieldKitchen.xml`) and the kitchen box (`Accessories_Camp.xml`):

```xml
<!-- The Billy: up to 20 simple meals a cook a night, half a unit of fuel a meal (authored). -->
<li Class="Phobos.VehicleSystems.PhobosCaravanCookingExtension">
  <mealsPerCook>20</mealsPerCook>
  <fuelPerMeal>0.5</fuelPerMeal>
  <meal>MealSimple</meal>
  <recipe>CookMealSimple</recipe>
</li>

<!-- The kitchen box: 8 meals a cook, and no water drawn. -->
<li Class="Phobos.VehicleSystems.PhobosCaravanCookingExtension">
  <mealsPerCook>8</mealsPerCook>
  <fuelPerMeal>0.5</fuelPerMeal>
  <waterPerMeal>0</waterPerMeal>
  <meal>MealSimple</meal>
  <recipe>CookMealSimple</recipe>
</li>
```

| Field | What it does (default) |
| --- | --- |
| `mealsPerCook` | The most meals each cook makes in a night (20). |
| `fuelPerMeal` | Fuel a meal burns: from the kitchen's own burners (a field kitchen's packed refuelable pieces, or the box's own), else chemfuel or wood from the packs (0.5). |
| `meal` | The meal made (none; required). |
| `recipe` | The recipe whose ingredients and filters a meal takes, and whose work type and speed stat choose the cooks (none; required). |
| `outdoorPoisonChance` | The flat chance a meal is spoilt when cooked under the sky with no closed tent in the caravan (0.02, the game's own figure for cooking outdoors). |
| `experiencePerMeal` | Cooking experience a meal gives its cook (20, authored). |
| `onTheMoveShare` | The share of a cook's meals made on the move, when the caravan drives through the night (0.5, authored). |
| `mealHaltTicks` | How long a meal halt holds a caravan that drives through the night, in ticks (2500, an hour; authored). |
| `waterPerMeal` | Litres a meal takes from the caravan's tanks (1, authored). Asked only where water means something (with Dubs Bad Hygiene). |

**The cooks reach** the caravan's packs, the cargo of every vehicle it tows, and what a packed cold box holds. Nothing patches the game's
caravan food: meals join the packs.

---

## Water and waste with Dubs Bad Hygiene

With Dubs Bad Hygiene, water on a vehicle is real: a trailer's showers and toilets run on its own tank and fill its own waste tank, a
field kettle takes water for each batch, and a vehicle's crew water tank is drunk from on the road. All four types live in a tie-in
assembly that loads only while both Vehicle Framework and Dubs Bad Hygiene are active (`Mods/VehiclesHygiene/`). Types:
`CompProperties_PhobosTrailerPlumbing` (a trailer's water and waste tanks), `CompProperties_PhobosHeaterPowerSaving` (a chemfuel water
heater that burns less on power), `CompProperties_PhobosKettleWater` (water for each batch at a field stove) and
`CompProperties_PhobosCrewWater` (the crew water tank, added for you).

**Gotchas first:**

- **Put these in a folder that loads only with Dubs Bad Hygiene** (`IfModActive="Dubwise.DubsBadHygiene"` in your `LoadFolders.xml`, or a
  `MayRequire` patch). The classes do not exist without it, and the game reports a config error for an unknown class.
- **Dubs' lite mode has no plumbing, so all four stand down there**: nothing is laid, no water is asked for, no line is shown. Mark Defs
  that make sense only with plumbing `DBHLite="true"`, as the shower trailer is, so Dubs drops them in lite mode.
- **Water is never made.** Every litre a shower, a flush, a kettle or a drink uses comes off a tank, and only the player's people fill
  the tanks (plumbing or a water bowser beside the vehicle). The one exception is Dubs' own "unlimited pumping" option: its hidden towers
  are then filled from nothing, and that water is never given back to the trailer's tank.
- **The stand-in towers and waste tanks are your own Defs.** Dubs' fixtures draw only from water towers on their pipes, so the trailer
  lays hidden buildings of Dubs' own kinds beneath itself. The Framework ships only the crew tank's stand-in (`Phobos_CrewWaterStandIn`);
  the shower trailer's and the Billy's stand-ins, their abstract base `PhobosTrailerStandInBase` and the water load `PhobosTank_Water` are
  **D'thorga Vehicles'**. Copy them into your own mod unless you depend on it.
- **A waste tank's `DubsBadHygiene.CompProperties_SepticTank` must rank below all of Dubs' own** (`<priority>-1</priority>`, as shipped).
  Dubs sends a pipe net's sewage to its highest priority, so a waste tank level with Dubs' septic tank takes the whole colony's waste once
  the trailer stands on the colony's pipes. Give it `<TreatedPerDay>0</TreatedPerDay>` for a holding tank.
- **A field stove with `CompProperties_PhobosKettleWater` also needs Dubs' pipe comp** (`DubsBadHygiene.CompProperties_Pipe`, mode
  `Sewage`) and the class `Phobos.VehicleSystems.Building_PhobosFieldStove`. Without the pipe it finds no plumbing, and its bills wait for
  ever.
- **A patch that adds `<comps>` to a Def whose comps come only from its parent must add the whole `<comps>` node**, not a `li` into a node
  that is not there: patches run before inheritance. The Billy's kettle patch (`FieldKitchen_Water.xml`) shows how.

### A trailer's water and waste tanks

`CompProperties_PhobosTrailerPlumbing` goes on a trailer's `VehicleDef`. While the trailer is set up, it lays a hidden water tower and a
hidden waste tank beneath it, and hidden plumbing under everything it set out, so Dubs' showers, basins, toilets and water heaters
among its pieces join its pipes. It works in one of two ways:

- **Fed from the trailer's tank** (the shower trailer): the trailer has a `CompProperties_PhobosTank` holding the `water` load, and the
  hidden tower is kept topped up to `workingLitres` from it on every rare tick. Packing up puts the tower's water back into the tank.
- **Its own water** (`ownWater` true, the Billy field kitchen): the trailer has no tank comp. Its water lives in the hidden tower whenever
  it stands parked (unhitched, the player's), set up or not, and aboard the trailer while it is towed. A water bowser or plumbing beside it
  fills the tower as it fills any tower. On the road it counts as the caravan's water.

The waste tank (when `septicDef` is given) stays beneath the trailer whenever it is parked, set up or not, so it can be emptied without
setting up. It is emptied four ways: Dubs' own colonists empty it into sludge drums at the player's level (buttons for 10, 25, 50 and 75 %,
or never; Dubs' 50 % until the player chooses); a sludge tanker pumps it out from beside it; while its pipes join a colony septic tank,
treatment plant or outlet, it drains into them by gravity at `drainPerHour`; and the player may open its dump valve, which runs the waste
onto the ground beside it as Dubs' sewage, within `dumpRadius`. **The dump valve is behind the player's setting "Trailers can dump waste
on the ground"** (on by default). Hitched, the valve closes and the waste rides along; destroyed while laid, the waste spills as Dubs
spills any broken septic tank.

**Gotchas:**

- **It needs a set-up.** The tower and the pipes are laid when a colonist finishes setting the trailer up by hand: through its tank
  (`CompProperties_PhobosTank`) or, for a trailer with none, `CompProperties_PhobosTrailerSetUp` (see [Setting out a trailer](#setting-out-a-trailer)).
  A trailer with its own water also lays its tower whenever it is parked.
- **Cells are in the trailer's own frame, facing north**, as a part's hitbox is: `towerCell` and `septicCell` default to the rear row
  of a 3-wide trailer, `(-1, -3)` and `(1, -3)`.
- **Keep `workingLitres` within the tower Def's `WaterStorageCap`**: the tower is a working amount, never the whole tank (the shower
  trailer keeps 150 L in a 200 L tower and carries 2,000 L in its tank).
- **The bigger tower takes effect the next time it is laid.** With `towerNode` and `towerDefWithNode`, the bigger tower is used while
  that upgrade node is unlocked; what a smaller tower cannot hold stays aboard.
- **A caravan with a shower trailer along washes under its showers** while it stands still; without one, from a basin's worth of water.

From the shower trailer (`mods/PhobosVehicles/Mods/DubsBadHygiene/Defs/ShowerTrailer/Vehicle_ShowerTrailer.xml` and
`Buildings_ShowerTrailer.xml`), trimmed. Figures authored:

```xml
<!-- On the trailer's VehicleDef, beside its CompProperties_PhobosTrailer, CompProperties_PhobosTank and CompProperties_PhobosSetOut. -->
<li Class="Phobos.VehicleSystems.Hygiene.CompProperties_PhobosTrailerPlumbing">
  <towerDef>Phobos_TrailerWaterStandIn</towerDef>
  <septicDef>Phobos_TrailerSepticStandIn</septicDef>
  <water>PhobosTank_Water</water>
  <workingLitres>150</workingLitres>
  <towerCell>(-1, -3)</towerCell>
  <septicCell>(1, -3)</septicCell>
</li>

<!-- The hidden waste tank (its parent is not drawn, not selectable, not built or taken down by hand). -->
<ThingDef ParentName="PhobosTrailerStandInBase" DBHLite="true">
  <defName>Phobos_TrailerSepticStandIn</defName>
  <label>trailer waste tank</label>
  <description>The trailer's waste tank, where its toilets drain.</description>
  <comps>
    <li Class="DubsBadHygiene.CompProperties_Pipe">
      <mode>Sewage</mode>
    </li>
    <li Class="DubsBadHygiene.CompProperties_SepticTank">
      <capacity>1000</capacity>
      <priority>-1</priority>   <!-- below every sewage handler of Dubs' -->
      <TreatedPerDay>0</TreatedPerDay>
    </li>
  </comps>
</ThingDef>
```

The water tower stand-in is the same, with `DubsBadHygiene.CompProperties_WaterStorage` (`<WaterStorageCap>200</WaterStorageCap>`) in
place of the septic tank. A trailer with its own water, from the Billy (`mods/PhobosVehicles/Mods/DubsBadHygiene/Patches/FieldKitchen_Water.xml`):

```xml
<li Class="Phobos.VehicleSystems.Hygiene.CompProperties_PhobosTrailerPlumbing">
  <ownWater>true</ownWater>
  <towerDef>Phobos_BillyWaterTank</towerDef>               <!-- 200 L -->
  <towerDefWithNode>Phobos_BillyWaterTankLarge</towerDefWithNode>   <!-- 600 L -->
  <towerNode>PhobosBillyBiggerTank</towerNode>
  <towerCell>(0, -3)</towerCell>
</li>
```

`CompProperties_PhobosTrailerPlumbing`:

| Field | What it does (default) |
| --- | --- |
| `towerDef` | The hidden water tower laid beneath the trailer: a building with Dubs' `CompProperties_WaterStorage` and `CompProperties_Pipe` (none: no water side). |
| `septicDef` | The hidden waste tank: a building with Dubs' `CompProperties_SepticTank` and `CompProperties_Pipe`. Its `capacity` is the waste tank's size (none: no waste side, and no waste buttons). |
| `water` | The tank load the tower is topped up from, a `PhobosTankContentsDef` in the trailer's `CompProperties_PhobosTank` (none). Not used with `ownWater`. |
| `workingLitres` | What the tower is kept topped up to from the tank, in litres (150, authored). Not used with `ownWater`. |
| `towerCell` | Where the tower lies, in the trailer's frame facing north (`(-1, -3)`). |
| `ownWater` | The trailer has no tank: its water lives in the tower while parked and aboard while towed (false). |
| `towerDefWithNode`, `towerNode` | A bigger tower used while that upgrade node of the trailer is unlocked (none). |
| `septicCell` | Where the waste tank lies, in the same frame (`(1, -3)`). |
| `drainPerHour` | How fast the waste tank empties by gravity, into a colony sewer or through the dump valve, in Dubs' sewage units an hour (300, authored: a full 1,000 in a little over three hours). |
| `dumpRadius` | How far from the waste tank the dump valve's waste may run, in cells (3.9, authored). |

### A water heater that burns less on power

`CompProperties_PhobosHeaterPowerSaving` goes on a set-out water heater built from Dubs' own boiler. While the trailer that set it out is
plugged in, it puts back `savedShare` of the fuel the heater burned since the last rare tick: the electric element doing part of the work,
in the one form Dubs' boiler allows (it cannot heat faster than its store fills). It also plays `workingSound` while the boiler is heating.

**Gotchas:**

- **It needs a `CompProperties_Refuelable`** (the fuel it refunds) **and Dubs' `DubsBadHygiene.CompProperties_CompBoiler`** (when it is
  heard). Without the refuelable it does nothing; without the boiler it is silent.
- **It saves fuel only on a piece a trailer set out**, and only while that trailer's power point is powered. Standing alone, it burns
  the full rate.
- **It draws its `PhobosSetOutPowerExtension` `wattsWorking` from the power point the whole time it stands set out**, burning or not
  (the shipped heater: 300 W, authored).

From the shower trailer's heater (`Buildings_ShowerTrailer.xml`), trimmed:

```xml
<comps>
  <li Class="DubsBadHygiene.CompProperties_Pipe">
    <mode>Sewage</mode>
  </li>
  <li Class="DubsBadHygiene.CompProperties_CompBoiler">
    <BaseCapacity>100</BaseCapacity>
    <ThermostatControl>true</ThermostatControl>
    <ShouldUsePowerModes>false</ShouldUsePowerModes>
  </li>
  <li Class="CompProperties_Refuelable">
    <consumeFuelOnlyWhenUsed>true</consumeFuelOnlyWhenUsed>
    <fuelConsumptionRate>4.0</fuelConsumptionRate>
    <fuelCapacity>25.0</fuelCapacity>
    <fuelFilter><thingDefs><li>Chemfuel</li></thingDefs></fuelFilter>
  </li>
  <li Class="Phobos.VehicleSystems.Hygiene.CompProperties_PhobosHeaterPowerSaving">
    <savedShare>0.5</savedShare>
    <workingSound>PhobosVehicles_ShowerWaterHeater</workingSound>
  </li>
</comps>
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosSetOutPowerExtension">
    <wattsWorking>300</wattsWorking>
  </li>
</modExtensions>
```

The shipped heater also carries Dubs' `DubCore.CompProperties_HeatStore` (its hot-water store); see the file.
**`PhobosVehicles_ShowerWaterHeater` is D'thorga Vehicles' sound**: name your own unless you depend on it.

| Field | What it does (default) |
| --- | --- |
| `savedShare` | The share of the fuel burned that is put back while the trailer is plugged in (0.5: half, the owner's "power a bonus"). |
| `workingSound` | The burner's loop, while Dubs' boiler is heating (none). |

### Water for a field stove's batch

`CompProperties_PhobosKettleWater` makes a field stove take water for each batch from the plumbing it stands on: a trailer's own tank
through the pipes it lays under its pieces, a water bowser or shower trailer beside it, or a colony's plumbing. Without enough water on
its pipes, its bills wait, and its inspect pane says why. It is not tied to a trailer: any `Building_PhobosFieldStove` with Dubs' pipe comp
can carry it.

From the Billy's kettle (`FieldKitchen_Water.xml`), added whole because the kettle's comps come from its parent:

```xml
<Operation Class="PatchOperationAdd">
  <xpath>Defs/ThingDef[defName="Phobos_BillyKettle"]</xpath>
  <value>
    <comps>
      <li Class="DubsBadHygiene.CompProperties_Pipe">
        <mode>Sewage</mode>
      </li>
      <li Class="Phobos.VehicleSystems.Hygiene.CompProperties_PhobosKettleWater">
        <litresPerBatch>10</litresPerBatch>
      </li>
    </comps>
  </value>
</Operation>
```

| Field | What it does (default) |
| --- | --- |
| `litresPerBatch` | Litres each batch takes when it starts (10, the owner's authored figure). A bill waits while the stove's pipes have no water tower, or hold less than this. |

### The crew water tank

`CompProperties_PhobosCrewWater` is **added for you** to every vehicle with a camp that is not a trailer (see the table above), and does
nothing until the player fits the crew water tank (`PhobosFitting_CrewWaterTank`, made from a kit, granting `CrewWater`). The tank goes
on the `WaterTankBay` part role, which every land loadout inheriting `PhobosVehicleLoadoutLandBase` appends. Fitted, it holds water as
cargo weight (a kilogram a litre); riders drink from it aboard a vehicle on a map, and the caravan drinks and washes from it on the road.
Parked with its engine off, it stands in a hidden tower (`Phobos_CrewWaterStandIn`) beneath the vehicle, so plumbing or a water bowser
beside it fills it, and with its camp pitched the vehicle lays pipes under the camp too, so a basin and a shower set out with the camp
draw from it. A message says when it is down to a quarter. Taking the tank off lets its water out.

**You write nothing for it.** To change its figures, patch the vehicle rules Def (`PhobosVehicleRulesDef`): `crewTankLitres` (120, authored:
one of Dubs' showers takes 65 L), `crewTankLowShare` (0.25) and `caravanDrinkLitres` (1.5, a drink's worth). The stand-in tower's own
`WaterStorageCap` (120) also caps it: raise both together. Its one field, `towerDef`, is filled in by the code.

---

## Reservoirs

A small store of one tank load inside a building: the Billy's paste tap keeps its own paste in one. A tank of ours set to pump out fills
it while it stands within the tank's hose reach, carrying what the load was made from; the store spoils by the load's own days. Types:
`CompProperties_PhobosReservoir` and `PhobosNodeGraphicExtension`.

**Gotchas first:**

- **It fills only from a load whose contents Def lists the reservoir port**: `<li Class="Phobos.VehicleSystems.Port_Reservoirs" />` in
  the `PhobosTankContentsDef`'s `ports` (see [Bulk tanks](vehicle-systems-modding.md#bulk-tanks)). A tank pumps into the nearest
  reservoir with room that belongs to the tank's own faction.
- **Something has to draw on it.** The Framework's `Phobos.VehicleSystems.Building_PhobosPasteTap` (a paste dispenser served from its
  reservoir, with no hoppers) is the only shipped building that does, besides a caravan eating from a packed paste tap. Another use needs
  C#.
- **`node`, `capacityWithNode` and `PhobosNodeGraphicExtension` work only on a set-out piece**: the node is an upgrade of the trailer
  that set the piece out, or holds it packed.
- **On a set-out piece it also fills from a pipe line beneath the tent or the packed trailer**, through a `Port_PipeNet` on the same
  contents Def (needs Vanilla Expanded Framework).

From the Billy's paste tap (`mods/PhobosVehicles/Mods/VanillaNutrientPasteExpanded/Defs/FieldKitchen_PasteTap.xml`), trimmed:

```xml
<ThingDef ParentName="PhobosBillyPieceBase">
  <defName>Phobos_BillyPasteTap</defName>
  <thingClass>Phobos.VehicleSystems.Building_PhobosPasteTap</thingClass>
  <tickerType>Rare</tickerType>
  <comps>
    <li Class="Phobos.VehicleSystems.CompProperties_PhobosReservoir">
      <contents>PhobosTank_NutrientPaste</contents>
      <capacity>150</capacity>
      <node>PhobosBillyBiggerPasteTank</node>
      <capacityWithNode>400</capacityWithNode>
      <!-- An insulated store: half the plain tank's rate. -->
      <spoilRate>0.5</spoilRate>
    </li>
  </comps>
  <modExtensions>
    <li Class="Phobos.VehicleSystems.PhobosNodeGraphicExtension">
      <node>PhobosBillyBiggerPasteTank</node>
      <graphicData>
        <texPath>Phobos/Vehicles/FieldKitchen/BillyPasteTapLarge</texPath>
        <graphicClass>Graphic_Single</graphicClass>
        <drawSize>(1, 1)</drawSize>
      </graphicData>
    </li>
  </modExtensions>
</ThingDef>
```

`CompProperties_PhobosReservoir`:

| Field | What it does (default) |
| --- | --- |
| `contents` | The only load it holds, a `PhobosTankContentsDef` (none: it holds nothing). |
| `capacity` | How many units it holds (100, authored). |
| `node` | A trailer upgrade that makes it bigger, on a set-out piece (none). |
| `capacityWithNode` | How many units it holds while that node is unlocked (0: the node changes nothing). |
| `spoilRate` | How fast its spoilage clock runs; 1 is a plain tank (1). It spoils only when its contents have `spoilsAfterDays`, at the building's temperature, packed or set out. |

`PhobosNodeGraphicExtension`:

| Field | What it does (default) |
| --- | --- |
| `node` | The trailer upgrade that swaps the picture (empty). |
| `graphicData` | The picture shown while it is unlocked (none). |

**`PhobosNodeGraphicExtension` is read only by `Building_PhobosPasteTap`** in this version; on another building class it does nothing.

---

## Airdrops

An aircraft marked for airdrops can drop its passengers, its cargo in bundles and the vehicles in its hold by parachute, over a map, onto
a world tile, as a gift to a friendly settlement, or to a convoy in radio reach. Type: `PhobosAirdropExtension`, on the aircraft's
`VehicleDef`.

**Gotchas first:**

- **Parachutes come from the aircraft's cargo**: a personnel parachute (`Phobos_PersonnelParachute`) for each jumper, a cargo parachute
  for each bundle, and a heavy drop rig (`Phobos_HeavyDropRig`) for each vehicle. With none, the drop is refused.
- **A drop onto a map needs our flight on the aircraft** (`Phobos.VehicleSystems.CompProperties_PhobosFlight`; see the flight section).
  Drops onto a tile, a settlement or a convoy go through Vehicle Framework's own launch.
- **Jumpers are everyone awake in a seat that does nothing** (passengers, a crew chief); drivers and gunners stay aboard.
- **Vehicles drop from its hold**: our `CompProperties_PhobosHold` (see A hold for vehicles), or another mod's holder named in
  `holdComps`. Only vehicles up to `heaviestPlatformKg` drop.
- **Another author's aircraft drops on ours only with the player's setting** "Other authors' aircraft use D'thorga airdrops (the C-17)"
  (off by default), when its extension sets `onlyWithStockSetting`.

From the Osprey (`mods/PhobosVehicles/Patches/Osprey/Airdrop_Osprey.xml`), which also gives it a hold:

```xml
<Operation Class="PatchOperationAdd">
  <xpath>Defs/Vehicles.VehicleDef[defName="Phobos_Osprey"]/modExtensions</xpath>
  <value>
    <li Class="Phobos.VehicleSystems.PhobosAirdropExtension">
      <jumpDoors>2</jumpDoors>
      <bundlesPerSecond>2</bundlesPerSecond>
      <platforms>1</platforms>
      <heaviestPlatformKg>3500</heaviestPlatformKg>
    </li>
  </value>
</Operation>
```

Another author's aircraft with its own airdrop mode, from `mods/PhobosVehicles/Mods/C17Globemaster/Patches/Airdrop_C17.xml`, trimmed:

```xml
<li Class="Phobos.VehicleSystems.PhobosAirdropExtension">
  <jumpDoors>2</jumpDoors>
  <bundlesPerSecond>4</bundlesPerSecond>
  <platforms>4</platforms>
  <heaviestPlatformKg>27216</heaviestPlatformKg>
  <holdComps>
    <li>C17Globemaster.CompC17Airlifter</li>
  </holdComps>
  <onlyWithStockSetting>true</onlyWithStockSetting>
  <stockModeComp>C17Globemaster.CompC17Airlifter</stockModeComp>
  <stockModeField>mode</stockModeField>
  <stockModeValue>Airdrop</stockModeValue>
</li>
```

| Field | What it does (default) |
| --- | --- |
| `jumpDoors` | Its jump doors: people leave about one a second from each (2). |
| `bundlesPerSecond` | Bundles of cargo down its ramp a second (2, authored). |
| `platforms` | Vehicles it drops on one pass, one after another (1, authored). |
| `heaviestPlatformKg` | The heaviest vehicle it may drop on a platform, kilograms (9000). |
| `holdComps` | Other mods' comps, by full type name, whose held vehicles are its hold; read by name, so no reference to that mod is needed (empty). |
| `onlyWithStockSetting` | Another author's aircraft: it drops on ours only with the player's setting on (false). |
| `stockModeComp` | Another author's own airdrop mode, replaced by ours while the setting is on: the comp's full type name (none: ours is offered beside the aircraft's other arrivals). |
| `stockModeField` | That comp's field holding the mode (`mode`). |
| `stockModeValue` | The value of that field meaning "airdrop" (`Airdrop`). |
| `raiderSpareChutes` | A raid's aircraft carries this many spare personnel parachutes beyond one each (0, authored). |
| `lapesKg` | The heaviest vehicle it may pull out low on a platform over a strip (LAPES), kilograms (0: it cannot). Only an aeroplane with a rear ramp that flies steadily a few feet up; never a helicopter or tiltrotor. The C-27J's is 5000. |

`jumpDoors` and `bundlesPerSecond` must be positive, `platforms`, `heaviestPlatformKg` and `lapesKg` not negative (a config error otherwise).

---

## The systems comp

`CompProperties_PhobosVehicleSystems` is the comp that carries a vehicle's systems. **It is built in code from the loadout, never written
in XML**: [How a vehicle gets its systems](vehicle-systems-modding.md#how-a-vehicle-gets-its-systems) says how a vehicle gets one, and
[A loadout for your vehicle](vehicle-systems-modding.md#a-loadout-for-your-vehicle) how to change what it holds. Its figures
(`batteryAmpHours`, `alternatorAmps`, `engineHeatAtFullLoad` and the rest) are what the player sees in Vehicle Framework's per-vehicle
settings window: see [Tuning in Vehicle Framework's window](vehicle-systems-modding.md#tuning-in-vehicle-frameworks-window). The other
fields (`loadout`, `systems`, `roleKeys`, `addedKeys` and so on) are filled in by the service and mean nothing in XML.

## Work vehicles

Trucks that stop, set up and work: the survey truck, the drill rig, the generator set and the tipper. In play, the player presses **Set
up**, the outriggers go down (with any moving parts swinging out), and a colonist works it or it works by itself; **Pack up** ends it. How
each plays is in the player guide's [Working vehicles](player-guide/working-vehicles.md).

### Shared fields of the set-up rigs

`CompProperties_PhobosRigWork` holds the fields the survey truck and the drill rig share. **Never put it on a vehicle by itself**: it
names no comp class, and its comp is abstract. Use `CompProperties_PhobosSurveyTruck` or `CompProperties_PhobosDrillRig`, which inherit
every field below.

- **Give the vehicle `Phobos_RigStability` and `Phobos_RigControl`** in `vehicleStats` (both start at 1) and list them in the
  `categories` of the outriggers and the valves or wiring, as the shipped rigs do. Below `stabilityFloor` it will not set up; below
  `controlFloor` it will not set up; below `workControlFloor` it will not work.
- **`powerPointDef` makes it a real load on a grid.** The game's power code only takes buildings, so setting up places a small building
  of ours under it. Use the Framework's `Phobos_PowerPoint` and `Phobos_PowerPointLarge`; left out, it never joins a grid.
- **Overdrive** is a player switch: the work runs `overdriveFactor` times as fast, burns `overdriveFuelFactor` times the fuel and wears
  the working parts.

| Field | What it does (default) |
| --- | --- |
| `powerPointDef` | The small building that plugs the set-up vehicle into a grid (none). |
| `powerPointLargeDef` | The same with the larger socket, used when the player's setting asks for it (none). |
| `setUpOverlay` | A picture drawn over the vehicle while it is set up: outriggers down (none). |
| `movingParts` | Parts that swing out on set-up, each its own picture: `key`, `label`, `look`, the hinge `pivotX` and `pivotZ` (cells from the picture's centre, facing north), `reach` (3 cells, must be clear), `angle` (90), `seconds` (2), and `extras`, pictures riding on it while a node is fitted (empty). |
| `stabilityFloor` | Below this `Phobos_RigStability`, it will not set up (0.5, authored). |
| `controlFloor` | Below this `Phobos_RigControl`, it will not set up (0.5, authored). |
| `workControlFloor` | Below this `Phobos_RigControl`, it will not work (0.35, authored). |
| `vitalParts` | Part keys without which the work stops, whatever the averages say (empty). |
| `overdriveFactor` | Work rate under overdrive (2, authored). |
| `overdriveFuelFactor` | Fuel multiplier under overdrive (2.5, authored). |
| `overdrivePartDamagePerHour` | Health the working parts lose an hour under overdrive (2, authored). |
| `overdriveOffBelow` | Overdrive cuts out when the working parts fall below this share of health (0.5, authored). |
| `overdriveHotAbove` | Overdrive stops above this coolant temperature, °C (120; the cooling system's "overheating" stage starts there). |

### Drill rig

A truck that sets up over the ground and drills deep resources as the game's deep drill does: the same work a portion, the same portion
sizes and yield, the same infestation chance, worked by a colonist (Mining). Piped resources go into a buffer for a tanker or a pipe net.

- **Needs** `Phobos_DrillRate` and `Phobos_DrillReach` in `vehicleStats` (both start at 0) and in the categories of the drill parts.
  Upgrades raise `Phobos_RigBuffer` (the buffer, 0 to start), `Phobos_ElectricDrill` (works from the grid, not the engine),
  `Phobos_BedrockFactor` and the work lights.
- Uses the Framework's work job `PhobosVehicleSystems_Drill`; nothing to add.
- **`sideJobs` and `bufferOutlets` take classes**, `Class="Namespace.ClassName"`, subclassing `RigSideJob` and `RigBufferOutlet`; the
  shipped ones are tie-ins: `Phobos.VehicleSystems.Oil.RigJob_OilWell` (Rimefeller) and `Phobos.VehicleSystems.Pipes.Outlet_PipeNet`
  (Vanilla Expanded Framework's pipe nets).

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/DrillRig/Vehicle_DrillRig.xml` and the Vanilla Chemfuel Expanded patch, trimmed:

```xml
<vehicleStats>
  <Phobos_DrillRate>10</Phobos_DrillRate>
  <Phobos_DrillReach>2.6</Phobos_DrillReach>
  <Phobos_RigControl>1</Phobos_RigControl>
  <Phobos_RigStability>1</Phobos_RigStability>
</vehicleStats>
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosDrillRig">
    <powerPointDef>Phobos_PowerPoint</powerPointDef>
    <powerPointLargeDef>Phobos_PowerPointLarge</powerPointLargeDef>
    <vitalParts>
      <li>Mast</li>
      <li>RotaryHead</li>
      <li>DrillBit</li>
    </vitalParts>
    <pipedResources>
      <li MayRequire="vanillaexpanded.vchemfuele">VCHE_Deepchem</li>
    </pipedResources>
    <bufferOutlets>
      <li Class="Phobos.VehicleSystems.Pipes.Outlet_PipeNet">
        <net>VCHE_DeepchemNet</net>
        <thing>VCHE_Deepchem</thing>
      </li>
    </bufferOutlets>
  </li>
</comps>
```

Every field of [the shared rig fields](#shared-fields-of-the-set-up-rigs), and:

| Field | What it does (default) |
| --- | --- |
| `drillFuelPerDay` | Chemfuel a day while drilling on the engine (40, authored); with fuel following engine load, `engineShare` sets it instead. |
| `engineShare` | Share of the engine's rated power drilling takes: compressor and mast hydraulics (0.5, authored). |
| `electricWatts` | Watts drawn from the grid with the electric drill fitted (300, authored). |
| `unlitFactor` | Work rate in the dark with no work lights fitted (0.8). |
| `pipedResources` | Deep resources that are piped, never items: skipped until a head is fitted, then raised into the buffer (empty). |
| `pipedWorkPerUnit` | Work to raise one unit of a piped resource (6, a tenth of the deepchem pumpjack's). |
| `storageReach` | How far from the hole, in cells, a storage building may stand and be filled straight from the rig (3, authored). |
| `bufferOutlets` | Where the buffer can empty itself, besides a tanker drawing from it (empty). |
| `sideJobs` | Other work at the hole, offered by tie-ins, tried when no ore is in reach (empty). |

### Survey truck

A truck that sets up and finds deep resources as the game's ground-penetrating and long-range scanners do, on a colonist's Intellectual
work, ten times as fast (the owner's order of magnitude). Each find on a map slows the next.

- **Needs** `Phobos_ScanRate` (starts at 1). Upgrades set `Phobos_SurveyLongRange`, `Phobos_SurveyFastFactor` (the faster array) and
  `Phobos_BatteryCapacity` (runs the console on its battery with the engine off; 0 to start).
- `sensorWeights` name a stat an upgrade raises, an ore and a factor: with the stat above 0, that ore is more likely.
- `extras` take classes subclassing `SurveyExtra`; the shipped one is `Phobos.VehicleSystems.Hygiene.SurveyExtra_Groundwater` (Dubs Bad
  Hygiene).

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/SurveyTruck/Vehicle_SurveyTruck.xml`, trimmed:

```xml
<li Class="Phobos.VehicleSystems.CompProperties_PhobosSurveyTruck">
  <powerPointDef>Phobos_PowerPoint</powerPointDef>
  <movingParts>
    <li>
      <key>boom</key>
      <label>sensor boom</label>
      <pivotX>0</pivotX>
      <pivotZ>-0.5</pivotZ>
      <reach>2.8</reach>
      <look>
        <graphicData>
          <texPath>Phobos/Vehicles/SurveyTruck/SurveyTruck_Boom</texPath>
          <graphicClass>Vehicles.Graphic_Vehicle</graphicClass>
          <drawSize>(4, 8)</drawSize>
        </graphicData>
      </look>
    </li>
  </movingParts>
  <sensorWeights>
    <li><stat>Phobos_SensorGeophone</stat><ore>Steel</ore><factor>1.5</factor></li>
    <li><stat>Phobos_SensorRadiometric</stat><ore>Uranium</ore><factor>3</factor></li>
  </sensorWeights>
</li>
```

Every field of [the shared rig fields](#shared-fields-of-the-set-up-rigs), and:

| Field | What it does (default) |
| --- | --- |
| `mtbDays`, `guaranteedDays` | Mean days between finds, and the days after which one is certain (0.3 and 0.6: a tenth of the game's scanners). |
| `longRangeMtbDays`, `longRangeGuaranteedDays` | The same with the long-range sensor (0.4 and 0.8). |
| `curveStep`, `curveCap` | How much each find on the map slows the next, and the most it can (1.5 and 8; the owner's curve). |
| `consoleWatts` | Watts from a grid for the console (700, authored). |
| `arrayWatts` | Watts with the fast array fitted and switched on (1000, authored). |
| `idleFuelPerDay` | Fuel a day with the engine idling for it (8, authored). |
| `highIdleShare` | Engine speed while recharging, as a share from idle to rated (0.33, about 1,200 rpm). |
| `sensorWeights` | Sensor stats and the ore each favours: `stat`, `ore`, `factor` (1) (empty). |
| `extras` | What tie-ins show while it is set up (empty). |

### Generator set

`CompProperties_PhobosGeneratorSet` makes a generator truck: set up beside a grid (or over power conduits) and it feeds the grid from its engine, with a cable end, a battery
buffer, recovered heat and a service lead to another vehicle.

- **Needs `Phobos_GeneratorOutput` in `vehicleStats`** (starts at 0): that, not `baseWatts`, is the power it makes. `baseWatts` only
  scales the fuel: full-output fuel follows the watts on offer. Upgrades raise `Phobos_CableReach` (0 to start), `Phobos_GeneratorBuffer`,
  `Phobos_HeatRecovery` and `Phobos_ServiceLeadReach`.
- **Needs `cableEndDef`**: use the Framework's `Phobos_GeneratorStub`, the building that joins the grid.
- It uses `Phobos_RigStability` and `Phobos_RigControl` as the rigs do, with its own `stabilityFloor` and `controlFloor`.
- It keeps its own fuel curve: the engine model never burns fuel for it.

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/GeneratorSet/Vehicle_GeneratorSet.xml`, trimmed:

```xml
<vehicleStats>
  <Phobos_GeneratorOutput>2000</Phobos_GeneratorOutput>
  <Phobos_RigControl>1</Phobos_RigControl>
  <Phobos_RigStability>1</Phobos_RigStability>
</vehicleStats>
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosGeneratorSet">
    <cableEndDef>Phobos_GeneratorStub</cableEndDef>
    <baseWatts>2000</baseWatts>
    <outputFloorWatts>400</outputFloorWatts>
    <idleFuelPerDay>2</idleFuelPerDay>
    <fullFuelPerDay>15</fullFuelPerDay>
    <runningSound>PhobosVehicles_GeneratorSetRunning</runningSound>
    <vitalParts>
      <li>Engine</li>
      <li>TractionGenerator</li>
    </vitalParts>
  </li>
</comps>
```

| Field | What it does (default) |
| --- | --- |
| `cableEndDef` | The building placed on the grid when it sets up (none; **required**). |
| `baseWatts` | The output `fullFuelPerDay` is written for (1200, authored). |
| `outputFloorWatts` | Below this output (a damaged set) it makes nothing (240, authored). |
| `idleFuelPerDay` | Fuel a day with nothing drawn (1.5, authored). |
| `fullFuelPerDay` | Fuel a day at `baseWatts` (9, authored). |
| `heatPerSecondPerWatt` | Recovered heat a second per watt (0.12: the game's heater's 21 for 175 W). |
| `heatTargetCelsius` | Recovered heat stops when the room reaches this (21). |
| `stabilityFloor`, `controlFloor` | As the rigs' (0.5 and 0.5). |
| `vitalParts` | Parts without which no power is made (empty). |
| `reelPart` | Part key without which the cable cannot be run out (`CableReel`). |
| `setUpOverlay` | Picture drawn while set up (none). |
| `runningSound` | Loop while set up with the engine running (none: silent). |

### Tipper

`CompProperties_PhobosTipper` makes a truck that tips its whole load in a fan behind it, or into storage it backs up to, and with its loading grab fitted lifts loose things
or goods from storage into its cargo. The grab is also the worker of a [gathering run](vehicle-systems-modding.md#gathering-runs).

- **Needs `Phobos_TipRate` in `vehicleStats`** (things a second; starts at 0) and `Phobos_RigControl`. The grab needs `Phobos_GrabLift`
  from an upgrade (kilograms; 0 to start, so no grab).
- With the hydraulics' system in the loadout, the tip and the grab run at what the hydraulic fluid gives.

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/Tipper/Vehicle_Tipper.xml`:

```xml
<vehicleStats>
  <Phobos_TipRate>2</Phobos_TipRate>
  <Phobos_RigControl>1</Phobos_RigControl>
</vehicleStats>
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosTipper" />
</comps>
```

| Field | What it does (default) |
| --- | --- |
| `fanRadius` | Radius in cells of the half-disc the load lands on (8, authored: about 100 cells). |
| `controlFloor` | Below this `Phobos_RigControl`, it will not tip or grab (0.5). |
| `hoistPowerShare` | Share of the engine's rated power the hoist takes while raising a load (0.12, authored). |
| `grabPowerShare` | The same for the grab lifting (0.08, authored). |
| `idlePumpShare` | The same while the pump waits for a gathering run's next job (0: the pump's drag is on the engine's ledger since 0.249.0). |
| `engineDemand` | **No longer read** (0.5); kept so old patches load. |

### Research aboard

`CompProperties_PhobosFieldLab` gives a vehicle a laboratory: each able, skilled person in its scientist seats adds to research as at a bench, at half rate while
it moves. Shipped only on the Seabed Crawler, a submarine (submarine work is on hold by the owner's choice).

- **The seats come from an upgrade** that adds the `roleKey` role; the same upgrade sets `Phobos_FieldLabFactor` (the research multiplier)
  and `Phobos_FieldLabGrade`, which decides which projects it can take on (both 0 to start).
- With full detail on, it also needs a `fieldLab` electrical load in the loadout's systems, as the shipped one has.
- **`FieldResearchExtension`** on a `ResearchProjectDef` or a `ResearchTabDef` marks research only a laboratory aboard can do: choosing it
  puts it in a slot of its own, and benches carry on with theirs. It takes no fields.

```xml
<li Class="Phobos.VehicleSystems.CompProperties_PhobosFieldLab" />
```

| Field | What it does (default) |
| --- | --- |
| `roleKey` | The role whose seated people research (`scientist`). |
| `minimumIntellectual` | Intellectual skill a person needs to contribute (7). |
| `underWayFactor` | Share of the research rate kept while the vehicle moves (0.5). |

### The work store

`CompProperties_PhobosWorkStore` is where haulers lay out the supplies for a vehicle's services, repairs and fittings, apart from its cargo:
it takes no cargo room and adds no weight. **The Framework adds it to every vehicle with a loadout; never write it.** It has no fields.

### Load-out switches on storage

`CompProperties_PhobosVehicleLoadOut` gives a storage building its two switches, **Vehicles may load from here** (for a grab) and
**Vehicles may unload here** (for a tipper or a run unloading by itself). **The Framework adds it to every storage building type, from every
mod**; never write it.

| Field | What it does (default) |
| --- | --- |
| `unloadStartsOn` | Whether "Vehicles may unload here" starts on (false). Set by the Framework: true where the building carries `PhobosBackUpSideExtension`. |

To have it start on for your bunker, give the building `PhobosBackUpSideExtension` (see [Gathering runs](vehicle-systems-modding.md#gathering-runs)).

## Buildings for vehicles

These comps go on **buildings** (a `ThingDef`'s `comps`), not on vehicles.

### Loading chute

`CompProperties_PhobosLoadingChute` makes a one-cell storage building whose goods drop into the cargo of a vehicle of its side parked across its spout, the cell it faces. Belts and
haulers fill it as any storage.

- **The building must be a `Building_Storage` with `tickerType` Rare** and `rotatable` true: the comp works on the rare tick, and the spout is
  the facing cell.

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/BulkBunker/LoadingChute.xml`, trimmed:

```xml
<ThingDef ParentName="BuildingBase">
  <defName>PhobosVehicles_LoadingChute</defName>
  <thingClass>Building_Storage</thingClass>
  <size>(1,1)</size>
  <rotatable>true</rotatable>
  <tickerType>Rare</tickerType>
  <comps>
    <li Class="Phobos.VehicleSystems.CompProperties_PhobosLoadingChute" />
  </comps>
  <building>
    <maxItemsInCell>4</maxItemsInCell>
  </building>
</ThingDef>
```

| Field | What it does (default) |
| --- | --- |
| `stacksPerLook` | Stacks passed down the chute each rare tick, about four seconds (4, authored). |

### Depot bay

`CompProperties_PhobosDepotBay` makes a building a vehicle's home: one vehicle is assigned to it; parked on the pad in front, it is refuelled with no one attending and mechanics work on it
faster.

- **The building needs a `CompProperties_Refuelable`** (the tank the pump draws from; it fills only vehicles burning the fuel its filter
  allows) and, for power, a `CompPowerTrader`; with power off, the pump stops. A `Phobos.Framework.Upgrades.CompProperties_PhobosBuildingUpgrades`
  lets the faster-pump upgrade raise `pumpPerHour`. The shipped bay is also a `Building_Storage` with `tickerType` Rare.
- The shipped `PhobosVehicles_DepotBay` in `mods/PhobosFramework/Mods/VehicleFramework/Common/Defs/Phobos/Framework/VehicleSystems/Stations.xml`
  is the worked example; it writes `<li Class="Phobos.VehicleSystems.CompProperties_PhobosDepotBay" />` and keeps the defaults.

| Field | What it does (default) |
| --- | --- |
| `padWidth`, `padLength` | The parking pad in front of the bay, cells: a vehicle whose centre is on it is in the bay (3 and 6, authored). |
| `pumpPerHour` | Fuel the pump moves an hour (30, authored). |
| `workFactor` | Share of the usual work a job takes on a vehicle in its bay (0.7, authored). |

### Dyno bay

`CompProperties_PhobosTuningRig` makes a building a dyno bay: engines are tuned, and parts changed for another grade, only on a vehicle in
its reach while it is powered. It is also where [bay trips](#bay-trips) take vehicles, and what makes a service yard a yard.

- **Give the building a `CompPowerTrader`** (as the shipped one, 150 W). Add `PhobosYardEquipmentExtension` with `required` true to count
  as a service yard's dyno.

From `Stations.xml`, trimmed:

```xml
<comps>
  <li Class="CompProperties_Power">
    <compClass>CompPowerTrader</compClass>
    <basePowerConsumption>150</basePowerConsumption>
  </li>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosTuningRig">
    <reach>5</reach>
  </li>
</comps>
```

| Field | What it does (default) |
| --- | --- |
| `reach` | Cells from the rig's edge a vehicle may stand, measured to the vehicle's drawn size (3; the shipped bay sets 5). |

### Service yard fuel pump

`CompProperties_PhobosFuelPump` makes a pump with its own tank that fills the vehicles standing in its service yard, emptiest first; with its charging cable fitted (a building
upgrade), it also charges electric vehicles from the grid.

- **It works only as yard equipment**: the building needs `PhobosYardEquipmentExtension`, and must stand in or touch a service yard zone
  that has a dyno. It needs a `CompProperties_Refuelable` for its tank and `tickerType` Rare; a `CompPowerTrader` is optional (without one
  it never stops for power). The charging cable needs `CompProperties_PhobosBuildingUpgrades`.
- Worked example: `PhobosVehicles_FuelPump` in `ServiceYard.xml` beside `Stations.xml`.

| Field | What it does (default) |
| --- | --- |
| `pumpPerHour` | Fuel moved an hour (30, authored: the depot bay's). |
| `chargeWattsPerRate` | Watts the cable draws for each unit of a vehicle's charge rate (500, derived from Vehicle Framework's own charging). |

## Aircraft

### Flight over the map

`CompProperties_PhobosFlight` gives a Vehicle Framework aircraft our own flight over a map: it takes off, flies, circles, hovers and lands
there, rolls along a strip when too heavy to lift straight up, and (if it hovers) works fast ropes, a rescue hoist and a sling. It stays an
ordinary Vehicle Framework vehicle on the cell beneath it. In play: [Aircraft and sling loads](player-guide/aircraft-and-sling-loads.md).

- **Engine loads come from its flight profile.** For a hover check and fuel that follow our flight, replace the profile's `reader` with
  `Phobos.VehicleSystems.PhaseReader_OwnFlight` and give it the phases the Osprey's patch lists (`Hover`, `Forward`, `TakeoffRoll`,
  `RunwayLanding` and the rest): see [Flight profiles](vehicle-systems-modding.md#flight-profiles).
- **Add `<seeThroughFog>true</seeThroughFog>` to the VehicleDef**, or the game hides it while it flies over unseen ground.
- **An aeroplane** sets `canHover` false and `maxTakeoffKilograms`: it rolls to take off and land, circles where a rotorcraft would hold,
  and never slings, hoists or ropes.
- **Another author's helicopter**: set `onlyWithStockSetting` true, so it flies as its author made it unless the player turns on
  "Stock helicopters fly over the map". `onlyInDrops` is for another author's aeroplane given our airdrop (see [Airdrops](#airdrops)).
- **The game refuses bad figures at load**: speeds and times must be positive, `fullRollCells` no shorter than `shortestRollCells`,
  `workHeightMetres` no higher than `flightHeightMetres`, and `orbitRadiusCells` no tighter than it can turn at its circling speed.
- It also gets [rounds between places](#rounds-between-places) by itself.

From `mods/PhobosVehicles/Patches/C27J/Flight_C27J.xml` (an aeroplane), trimmed:

```xml
<Operation Class="PatchOperationAdd">
  <xpath>Defs/Vehicles.VehicleDef[defName="Phobos_C27J"]/comps</xpath>
  <value>
    <li Class="Phobos.VehicleSystems.CompProperties_PhobosFlight">
      <speedCellsPerSecond>9</speedCellsPerSecond>
      <accelerationCellsPerSecond>1.2</accelerationCellsPerSecond>
      <turnDegreesPerSecond>24</turnDegreesPerSecond>
      <climbSeconds>10</climbSeconds>
      <descentSeconds>12</descentSeconds>
      <canHover>false</canHover>
      <maxTakeoffKilograms>32500</maxTakeoffKilograms>
      <shortestRollCells>12</shortestRollCells>
      <fullRollCells>35</fullRollCells>
      <landingRollCells>25</landingRollCells>
      <orbitSpeedCellsPerSecond>7</orbitSpeedCellsPerSecond>
      <orbitRadiusCells>18</orbitRadiusCells>
      <cabinSound>PhobosVehicles_C27JCabin</cabinSound>
    </li>
  </value>
</Operation>
```

The Osprey's (`Patches/Osprey/Flight_Osprey.xml`) is the rotorcraft example, with ropes and a hoist; the Vanilla Vehicles Expanded patch
(`Mods/VanillaVehiclesExpanded/Patches/Flight_VanillaVehiclesExpanded.xml`) gives another author's helicopter our flight.

| Field | What it does (default) |
| --- | --- |
| `speedCellsPerSecond` | Top speed over a map, cells a second (6). |
| `accelerationCellsPerSecond` | How fast it speeds up and brakes (1.5). |
| `turnDegreesPerSecond` | How fast it turns (45). |
| `climbSeconds` | Seconds from the ground to flight height (6). |
| `descentSeconds` | Seconds from flight height to a landing spot (8). |
| `fallSeconds` | Seconds to the ground when it cannot stay up (3). |
| `flightHeightMetres` | The one height it flies a map at: what ropes, hoist and shadow read (60). |
| `shortestRollCells` | Shortest take-off roll, just too heavy to lift straight up (5). |
| `fullRollCells` | Full roll at its rolling share, and a rolling landing's strip (20). |
| `needsBuiltFloor` | Its strip must be a built floor (false). |
| `canHover` | Whether it can hover at all; false for an aeroplane (true). |
| `maxTakeoffKilograms` | An aeroplane's maximum take-off weight; its full roll at this weight, and it may not exceed it (0: no limit). |
| `landingRollCells` | Strip a rolling landing needs (-1: its full roll). |
| `liftOffSpeedShare` | Speed leaving the ground at the end of a roll, as a share of top speed (0.6, authored). |
| `orbitSpeedCellsPerSecond` | Speed circling (left out: seven tenths of top speed, authored). |
| `orbitRadiusCells` | Tightest circle's radius, never tighter than its turn allows (12). |
| `workHeightMetres` | Height it comes down to for ropes and hoist (20, authored). |
| `ropePoints` | Fast-rope points: people on the ropes at once (2). |
| `ropeSlideMetresPerSecond` | How fast a person slides down (4, authored). |
| `hoistRatedKilograms` | Hoist's rated load (272: the V-22's 600 lb). |
| `hoistLoadedMetresPerSecond` | Hoist speed at rated load (0.7). |
| `hoistEmptyMetresPerSecond` | Hoist speed with the hook empty (1.2). |
| `hoistHookSeconds` | Seconds to strap a person in (10, authored). |
| `hoistReachCells` | How far from the hoist's foot it reaches someone on the ground (3, authored). |
| `onlyWithStockSetting` | Idle unless the player's "Stock helicopters fly over the map" is on: for another author's helicopter (false). |
| `onlyInDrops` | Flies on our flight only on a parachute drop pass: for another author's aeroplane given our airdrop (false). |
| `flightSound` | Its sound in the air (left out: its Def's Vehicle Framework launch sound, if any). |
| `cabinSound` | Sound inside it while the player follows it on the world map (none). |
| `rotorDegreesPerTick` | How fast its drawn rotors turn at full speed (left out: its Vehicle Framework take-off animation's rate; 0 for none). |
| `rotorSpinUpSeconds` | Seconds for rotors to spin up once the engines run (6, authored). |
| `rotorRunDownSeconds` | Seconds for rotors to run down once the engines stop (10, authored). |

### Rounds between places

`CompProperties_PhobosRound` lets an aircraft on our flight fly a round of up to eight places on the world map by itself, with its pilot
aboard (or with nobody, once the optionally piloted kit is fitted), waiting at each stop to be unloaded, loaded and refuelled. **Added by
the Framework** to every aircraft with `CompProperties_PhobosFlight` whose `onlyInDrops` is false, when the aircraft has a loadout. No
fields.

### Propellers

`CompProperties_PhobosPropellers` draws an aeroplane's propellers from straight above, as thin bars across the nacelles' noses: still while they stand, a see-through blur
while they turn, after our flight's spin-up and run-down. Vehicle Framework's own propeller overlay turns a disc, which suits rotors only.

- Needs `CompProperties_PhobosFlight` for the spin; without it they stay still.

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/C27J/Vehicle_C27J.xml`:

```xml
<li Class="Phobos.VehicleSystems.CompProperties_PhobosPropellers">
  <stillPath>Phobos/Vehicles/C27J/C27J_Propeller</stillPath>
  <spinPath>Phobos/Vehicles/C27J/C27J_PropellerSpin</spinPath>
  <drawSize>(5, 2.5)</drawSize>
  <hubs>
    <li>(-5.26, 7.12)</li>
    <li>(5.26, 7.12)</li>
  </hubs>
</li>
```

| Field | What it does (default) |
| --- | --- |
| `stillPath` | The still propeller seen edge-on, centred on its hub (empty). |
| `spinPath` | The turning propeller's blur, centred on its hub, drawn see-through (empty). |
| `drawSize` | Drawn size in cells: across the blades, then fore and aft ((4.8, 0.6)). |
| `hubs` | Each hub with the vehicle facing north, cells from its centre: x right, y forward (empty). |
| `blurFromShare` | Share of full speed from which it draws the blur (0.3, authored). |

### Tiltrotor nacelles

`CompProperties_PhobosTiltrotor` draws a tiltrotor's nacelles and rotors: the nacelles tip from upright to forward with our flight's speed (and through Vehicle
Framework's own take-off and landing), and each rotor is still blades with the engines off and a spinning disc with them on.
Presentation only.

- **Pictures are found by name**: `<nacellePath><angle><suffix>` for each angle (`Osprey_Nacelle0`, `Osprey_Nacelle30`...), each with
  its paint mask beside it (Vehicle Framework's `m` suffix), and `<nacellePath><angle>Damage<Left|Right>` for the damage marks.
- **The game reports a missing `nacellePath`, `rotorPath`, `rotorSpinPath`, angle or side** at load.

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/Osprey/Vehicle_Osprey.xml`, trimmed:

```xml
<li Class="Phobos.VehicleSystems.CompProperties_PhobosTiltrotor">
  <nacellePath>Phobos/Vehicles/Osprey/Osprey_Nacelle</nacellePath>
  <variants>
    <li><node>InletBarrier</node><suffix>Barrier</suffix></li>
  </variants>
  <rotorPath>Phobos/Vehicles/Osprey/Osprey_Rotor</rotorPath>
  <rotorSpinPath>Phobos/Vehicles/Osprey/Osprey_RotorSpin</rotorSpinPath>
  <rotorDrawCells>14</rotorDrawCells>
  <sides>
    <li>
      <part>LeftNacelle</part>
      <hub>(-8.277, 2.211)</hub>
      <pivot>(-8.255, -0.509)</pivot>
      <damageSide>Left</damageSide>
    </li>
    <li>
      <part>RightNacelle</part>
      <hub>(8.254, 2.157)</hub>
      <pivot>(8.257, -0.509)</pivot>
      <mirrored>true</mirrored>
      <damageSide>Right</damageSide>
      <bladeStart>60</bladeStart>
    </li>
  </sides>
</li>
```

| Field | What it does (default) |
| --- | --- |
| `nacellePath` | The nacelle pictures' path before the angle (empty; **required**). |
| `angles` | The angles a picture exists for; the nearest is drawn (0, 30, 60, 90). |
| `variants` | A picture set shown while an upgrade is fitted: `node` and the `suffix` after the angle (empty). |
| `damageBelowHealth` | Damage marks over a nacelle below this share of its health (0.6). |
| `rotorPath`, `rotorSpinPath` | Still blades and spinning disc (empty; **required**). |
| `rotorDrawCells` | Rotor picture's width in cells (14). |
| `pixelsPerCell` | The pictures' scale (128). |
| `spinDegreesPerTick` | How fast the disc turns as drawn: a shimmer, not the real rpm (23, authored). |
| `sides` | Each nacelle: `part` (its key, whose health shows damage), `hub` (rotor hub with the nacelle upright), `pivot` (the nacelle's rear end), `mirrored` (false), `damageSide` (`Left`), `bladeStart` (degrees, 0) (empty; **required**). |

### Fire-fighting tank

`CompProperties_PhobosFireTank` gives an aircraft a roll-on tank: on the ground, a colonist pumps it full from open water within reach; in the air, a water drop pass
empties it along a line and puts out the fires beneath. In play: [Fighting a fire from the air](player-guide/aircraft-and-sling-loads.md#fighting-a-fire-from-the-air).

- **Its size is the stat `Phobos_FireTankLitres`**, which an upgrade node raises; at 0 (the start) the comp does nothing.
- **The drop pass is our flight's**: the aircraft needs `CompProperties_PhobosFlight`.

From `Vehicle_C27J.xml`; its `UpgradeTree_C27J.xml` node `FireTank` sets `Phobos_FireTankLitres` to 7950:

```xml
<li Class="Phobos.VehicleSystems.CompProperties_PhobosFireTank">
  <pumpReachCells>6</pumpReachCells>
  <litresPerMinute>400</litresPerMinute>
  <dropWidthCells>3</dropWidthCells>
</li>
```

| Field | What it does (default) |
| --- | --- |
| `pumpReachCells` | How far from the vehicle's edge open water may be for the hose (6, authored). |
| `litresPerMinute` | The pump's rate, litres a game minute (400, authored). |
| `dropWidthCells` | Width of the strip a pass wets (3, authored). |

### Chaff and flare magazine

`CompProperties_PhobosDispensers` is the magazine of an aircraft's chaff and flare dispensers: haulers top it up from the stockpile, it takes
no cargo room, and the air combat service spends it. **The Framework adds it** when the loadout's `avionics` has `countermeasures` true,
with `capacity` set from the avionics' `countermeasureCartridges` (30). With `countermeasuresByUpgrade` true, it holds nothing until an
upgrade raises `Phobos_Countermeasures`, as on the C-27J. **Write the avionics, not the comp.**

| Field | What it does (default) |
| --- | --- |
| `capacity` | Cartridges the magazine holds: the loadout's avionics figure (30). Fitted dispensers can raise it. |

### Raiders by air

`PhobosRaiderAircraftExtension` on a VehicleDef lets raiders arrive aboard it, with the player's "Raiders arrive by air" setting on: it
comes in over the edge, puts them down and leaves the way it came. Up to three aircraft a raid; whoever does not fit walks in.

- **The aircraft needs `CompProperties_PhobosFlight`.** One that cannot hover also needs `PhobosAirdropExtension` (not marked
  `onlyWithStockSetting`), so it can drop its raiders by parachute.

From `mods/PhobosVehicles/Patches/Osprey/Flight_Osprey.xml`:

```xml
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/Vehicles.VehicleDef[defName="Phobos_Osprey"]</xpath>
  <value>
    <li Class="Phobos.VehicleSystems.PhobosRaiderAircraftExtension">
      <minTechLevel>Industrial</minTechLevel>
      <weight>1</weight>
    </li>
  </value>
</Operation>
```

| Field | What it does (default) |
| --- | --- |
| `minTechLevel` | Least tech level a faction needs to field it (Industrial). |
| `weight` | How often it is picked among the others that fit (1). |

### Side guns

`PhobosSideGunExtension` on a `VehicleTurretDef` marks a side-firing gun: an aircraft with one fitted circles toward the gun's side, as a
gunship flies its pylon turn, so the gun faces the circle's centre. Its arc is the turret's own (`angleRestricted`). Shipped on the
C-27J's gun kit (`Turrets_C27J.xml`).

```xml
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosSideGunExtension">
    <left>true</left>
  </li>
</modExtensions>
```

| Field | What it does (default) |
| --- | --- |
| `left` | The gun fires out of the left side; false for the right (true). |

## Remote control and routes

None of these comps take figures, and **the Framework gives them out by itself** (see the table at the top): a modder's work is to keep
the land loadout's `RemoteKitMount` and `RouteKitMount` parts, which the default land loadouts already append. The figures live in
`PhobosRemoteControlRulesDef`. How it plays: [Vehicles on the road](player-guide/vehicles-on-the-road.md#driven-from-a-station).

### Remote control

`CompProperties_PhobosRemoteControl` lets a vehicle be driven from afar: its **Drive from a station** button, the lost-link choice (hold,
come back, carry on) and the letters. **It needs a source**: D'thorga Radio Framework's stations and its remote control kit fitting, which
goes on the `RemoteKitMount` part. The route kit uses its lost-link choice too. No fields.

### Mech link

`CompProperties_PhobosMechLink` lets a mechanitor drive the vehicle through the mech control link fitting, taking bandwidth by size. **The
fitting exists only with Biotech**, and goes on the same `RemoteKitMount` part as the radio's kit, so a vehicle takes one or the other. No
fields.

### Route kit

`CompProperties_PhobosRoute` lets a vehicle follow a leader in a convoy or run a loop between a pick-up and a drop-off by itself, once the
route kit fitting is on its `RouteKitMount` part. No fields.

### Supply runs

`CompProperties_PhobosSupplyRun` gives a land vehicle a supply run: a round of stops on the world map (depots, open ground, the player's
colonies), driven again and again with a driver, or with nobody by its route kit. With D'thorga Radio Framework a crewless run answers to
radio reach. **Added by the Framework** to every land vehicle that can take the route kit; another mod's vehicle without that part may name
it in its comps. No fields.

```xml
<li Class="Phobos.VehicleSystems.CompProperties_PhobosSupplyRun" />
```

### Remote guns

`PhobosRemoteGunExtension` on a `VehicleTurretDef` marks a remote weapon station: on any vehicle, kit or not, it fires only while a radio
station's operator has the vehicle's picture, or with the player's setting "Remote guns fire on their own" on. **Without D'thorga Radio
Framework there is no picture**, so it fires only with that setting. A turret without the marker keeps its own way. No fields.

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/Turrets_RemoteWeaponStation.xml`:

```xml
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosRemoteGunExtension" />
</modExtensions>
```

## Service and defence

### Bay trips

`CompProperties_PhobosBayTrip` lets a land vehicle be taken to a [dyno bay](#dyno-bay) for repair without being told, towed by a winch
truck or pushed if it cannot drive, and put away afterwards. The player's settings and the vehicle's own switch on the Fleet board decide
whether it goes. **Added by the Framework** to every land vehicle with a loadout, trailers apart. No fields. In play:
[Trips to the dyno bay](player-guide/vehicles-on-the-road.md#trips-to-the-dyno-bay).

### Fire-fighting

`CompProperties_PhobosFireFighting` holds a vehicle's fire zones (engine bay, crew compartment, running gear), its hand extinguisher's
charges and its spent suppression systems. **Added by the Framework** to every vehicle with a loadout, trailers apart, with an extinguisher
bracket appended. No fields. In play: [Fire](player-guide/vehicle-damage-and-repairs.md#fire).

- **Automatic suppression on your own vehicle**: put `Phobos.VehicleSystems.PhobosSuppressionUpgrade` in an upgrade node's `upgrades`,
  with `zone` `EngineBay` or `CrewCompartment`. The Framework then appends that zone's system part.
- **Without such nodes**, a vehicle that counts as armoured takes the suppression systems as fittings. Mark it with
  `PhobosFittedEquipmentExtension`'s `armoured` true or false; left unset, its outer plates' sharp armour decides.

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/BulkTanker/UpgradeTree_BulkTanker.xml`, trimmed:

```xml
<upgrades>
  <li Class="Phobos.VehicleSystems.PhobosSuppressionUpgrade">
    <zone>EngineBay</zone>
  </li>
</upgrades>
```

### Hulls

`CompProperties_PhobosHull` makes a vehicle's frame (its `VehicleBuildDef`) a hull: with the setting "Destroyed vehicles leave a hull" on,
a destroyed vehicle leaves its frame where it fell, and taking it apart gives salvaged parts, not materials. **Added by the Framework** to
every `VehicleBuildDef`. No fields.

- **A vehicle with no `buildDef` leaves no hull.** A raiders' aircraft brought down leaves none either.

### Smoke dischargers

Smoke dischargers are Vehicle Framework fixed (`Static`) turrets, one bank a side under one `groupKey`, so one button fires both.
`PhobosSmokeDischargerExtension` on the turret Def marks them as ours: the vehicle then gets `CompProperties_PhobosSmoke` by itself, which
gives the **Pop smoke when hit** switch and lets other factions' trucks pop smoke too (Vehicle Framework never fires a fixed turret for
anyone but the player).

- The marker counts on the vehicle's own turrets or on any turret its upgrade tree fits (`TurretUpgrade`).
- **`PhobosTurret_SmokeDischarger` is D'thorga Vehicles'**, not the Framework's: to reuse it, depend on D'thorga Vehicles; otherwise write
  your own turret with the marker.
- `CompProperties_PhobosSmoke` has no fields; never write it.

From `mods/PhobosVehicles/Defs/Phobos/Vehicles/Turrets_SmokeDischargers.xml`, trimmed:

```xml
<Vehicles.VehicleTurretDef ParentName="VehicleTurretBase">
  <defName>PhobosTurret_SmokeDischarger</defName>
  <turretType>Static</turretType>
  <maxRange>9</maxRange>
  <projectile>Bullet_SmokeLauncher</projectile>
  <modExtensions>
    <li Class="Phobos.VehicleSystems.PhobosSmokeDischargerExtension">
      <switchIcon>Phobos/VehicleSystems/PopSmoke</switchIcon>
    </li>
  </modExtensions>
</Vehicles.VehicleTurretDef>
```

| Field | What it does (default) |
| --- | --- |
| `switchIcon` | The switch's button picture (none: ours). |

### Upgrade research (retired)

`PhobosEnforceUpgradeResearch` **does nothing** since Framework 0.137.2: Vehicle Framework enforces the research on every upgrade tree's
nodes itself (since 1.7.2537). It is kept so older trees that name it still load. **Leave it out.** See
[the authors' override contract](development/vehicle-author-overrides.md).

## Sounds

### Engine sound

`PhobosEngineSoundExtension` on a VehicleDef gives it an engine sound that follows the engine: two steady loops, `idleSound` and
`loadSound` (the same engine labouring), pitched up with engine speed and blended by load. **It is played by the systems comp**, so the
vehicle needs a loadout.

- **Each loop's SoundDef must be `sustain` and map two external parameters**: `PhobosEnginePitch` (a pitch multiplier) onto pitch, and
  `PhobosEngineLoad` (0 to 1) onto volume, so the idle loop fades as the load loop rises. `mods/PhobosVehicles/Defs/Phobos/Vehicles/Sounds/Sounds_Vehicles.xml`
  (`PhobosVehicles_DrillRigEngineIdle`) is the worked example.
- Engine speeds come from the drivetrain model; with none (or the model off in the settings), from the three `fallback` figures.

From `Vehicle_C27J.xml`:

```xml
<modExtensions>
  <li Class="Phobos.VehicleSystems.PhobosEngineSoundExtension">
    <idleSound>PhobosVehicles_C27JEngineIdle</idleSound>
    <loadSound>PhobosVehicles_C27JEngineLoad</loadSound>
    <aloftSound>PhobosVehicles_C27JAloft</aloftSound>
    <taxiSound>PhobosVehicles_C27JTaxi</taxiSound>
    <idlePitch>1</idlePitch>
    <ratedPitch>1</ratedPitch>
  </li>
</modExtensions>
```

| Field | What it does (default) |
| --- | --- |
| `idleSound` | The loop recorded near idle (none). |
| `loadSound` | The same engine labouring (none). |
| `idlePitch`, `ratedPitch` | Pitch multiplier at idle and at rated speed (0.9 and 1.35, authored; keep within about 0.8 to 1.4). |
| `fallbackIdleRpm`, `fallbackCruiseRpm`, `fallbackRatedRpm` | Engine speeds without a drivetrain model (800, 1600, 2400). |
| `silentWhileWorking` | Silent while the engine turns for work that has its own running sound, as the generator set (false). |
| `aloftSound` | Played instead of the two loops once an aircraft on our flight is off the ground (none: the loops play in the air too). |
| `taxiSound` | Played instead of the two loops while it moves on the ground under its own power (none: the loops play). |

### Work sounds

`PhobosVehicleSoundsExtension` on a VehicleDef names the sounds of its work by key. Our comps ask for a key when the moment comes; a key
with no sound stays silent. An entry with `node` plays only while that upgrade is fitted: **list such entries before the plain one**, as
the first that applies is played.

- **Keys our comps use**: `SetUp`, `PackUp` (falls back to `SetUp`), `Work`, `WorkElectric`, `Portion`, `Spent`, `Console`, `Array`,
  `Dish`, `Find`, `Hoses`, `Pump`, `Relief`, `Leak`, `Purge`, `Rams`, `Slide`, `Tailgate`, `Dive`, `Surface`, `Settle`, `LiftOff` (falls
  back to `Settle`), `Creak`, `Scrubber`, `Lab`, `Contact`, and from our flight `Landing` and `FastRope`.

From `Vehicle_DrillRig.xml`:

```xml
<li Class="Phobos.VehicleSystems.PhobosVehicleSoundsExtension">
  <sounds>
    <li><key>SetUp</key><sound>PhobosVehicles_DrillRigSetUp</sound></li>
    <li><key>Work</key><sound>PhobosVehicles_DrillRigHammer</sound><node>RcHammer</node></li>
    <li><key>Work</key><sound>PhobosVehicles_DrillRigDrill</sound></li>
    <li><key>WorkElectric</key><sound>PhobosVehicles_DrillRigElectric</sound></li>
    <li><key>Portion</key><sound>PhobosVehicles_DrillRigChute</sound></li>
    <li><key>Spent</key><sound>PhobosVehicles_DrillRigWindDown</sound></li>
  </sounds>
</li>
```

| Field | What it does (default) |
| --- | --- |
| `sounds` | Each entry's `key`, `sound` (a SoundDef) and optional `node` (an upgrade key) (empty). |

## Listening and sonar

Submarine work is on hold by the owner's choice; these are shipped only on the Seabed Crawler. In play:
[Listening and sonar](player-guide/submarines.md#listening-and-sonar); the boat itself is under
[Submersibles](vehicle-systems-modding.md#submersibles).

### Listening array

`CompProperties_PhobosListening` lets a vehicle in the water hear movement within `Phobos_ListeningRange` (a stat, 0 until an upgrade
raises it).

| Field | What it does (default) |
| --- | --- |
| `movingFactor` | Share of its range kept while the vehicle moves (0.5). |
| `landShare` | Share of its range at which it hears things moving on land (0.5). |
| `stillBoatShare` | Share of its range at which it hears a boat under and keeping still (0.5). |
| `quietLooksToReport` | Quiet looks in a row, about four seconds each, before new movement is reported again (3). |

### Sonar

`CompProperties_PhobosSonar` gives an active sonar set with a ping button, within `Phobos_SonarRange` (a stat, 0 until an upgrade raises
it). A ping gives the boat away while it is under.

| Field | What it does (default) |
| --- | --- |
| `cooldownSeconds` | Time between pings (30). |
| `settledShare` | Share of its range at which a ping finds a boat resting on the bottom (0.5). |
| `heardFactor` | How many times its own range the ping is heard at (2). |
| `givenAwaySeconds` | How long a ping gives the boat away (60). |
| `showSeconds` | How long what a ping found stays drawn (10). |
| `crewPingChance` | Chance per look that a crew not the player's pings while hunting (0.15). |
| `pingSound` | Played at the boat with every ping (none: silent). |
| `pingIcon` | The ping button's picture (`Phobos/VehicleSystems/SonarPing`). |
