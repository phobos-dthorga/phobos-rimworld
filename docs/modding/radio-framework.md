# D'thorga Radio Framework for modders

How your own mod can add a radio, a band, an antenna or a noisy machine to D'thorga Radio Framework, with no C#. Players start at
[the mod's README](../../mods/PhobosRadioFramework/README.md) and [the player guide](../player-guide/radio.md). The design, its
phases and its physics are in [the radio framework design](../development/radio-framework-design.md). Work on the radio
resumed on 23 Nov 2026, after its pause from 9 Nov. **Not yet tested in game.**

## Defs to extend

Everything is a Def:

- `Phobos.Radio.BandDef` and `Phobos.Radio.ModeDef`: bands and modes. Since 0.49.0 a band may name an `upgradeFigure`: a station
  or antenna then has it only while one of its fitted upgrades sets that figure to 1 (the aviation bands, given by the aviation
  transceiver and aviation antennas upgrades); a radio's built-in antenna works only its own Def's bands. Aircraft carry the
  `PhobosRadio_FittingAviation` radio, and every powered vehicle shows a `PhobosVehicleSystem_Comms` system (with Vehicle Framework).
- One `Phobos.Radio.RadioRulesDef`: the tuning values, including coverage's limits.
- `Phobos.Radio.ChannelKindDef` (0.8.0): a kind of channel use, with a `label`, a `mode` and an `order` (lower is served first
  when channels are short), and `preempts` (0.9.0: when every station it reaches is full, it takes channels from the kinds
  served after it that do not pre-empt, the weakest first and no more than it needs). Kinds fill blocks of channels of their
  own and are listed apart on the station. Ours: `PhobosRadio_KindVehicleControl` (pre-empts), `PhobosRadio_KindVehiclePicture`
  and `PhobosRadio_KindLocalVoice`. Add one for a new use of the radio rather than a second system of frequencies.
- `Phobos.Radio.RadioFittingDef` (0.10.0): a radio fitted to something that wears none, its `radio` the comp's figures as on any
  radio. Ours: `PhobosRadio_FittingVehicleControl`. With Vehicle Framework, a vehicle def names one with the tie-in's
  `Phobos.Radio.Vehicles.VehicleRadioFitting` extension (`fitting`, and `upgradeNode`, the key of the node that fits it; empty
  for always fitted), and every vehicle of it is built with that radio.
- `Phobos.Radio.Services.CarriedRadios` (0.10.0): the net counts a radio comp on any spawned pawn of the colony that is not a
  person. Register an `ICarriedRadioRule` to say what yours does: `Off`, `StandingBy` (links shown, no channel asked) or `InUse`;
  with no rule answering it is in use.
- `Phobos.Radio.CompProperties_Radio`, on any apparel or building; its `channelKind` (0.8.0) names its kind, local voice when
  empty; a station's `channels` (0.7.0) is how many 25 kHz channels it
  brings to the ledger (`RadioRulesDef.channelWidthHz`); a mode takes as many as its `bandwidthHz` needs, and radios share one by
  turns as its `dutyCycle` allows.
- `Phobos.Radio.CompProperties_Antenna` plus `Phobos.Radio.CompProperties_FeedlinePort`, on an antenna building.
- `Phobos.Radio.CompProperties_Amplifier` (`gainDb`, `ratedOutputWatts`, `lossDb`) plus `CompProperties_FeedlinePort` and a power
  comp, on an amplifier building (0.5.0): it counts for an antenna only when the cable path from the station passes through it.
- Three `DefModExtension`s:
  - `RadioObstructionExtension` (`lossDb` per crossing), on a stuff or building Def;
  - `RadioNoiseExtension` (`noiseDbmAt1m`, `radiusCells`, `noiseWhen` = PowerOn, Connected or Always, and `computerNoise`, true
    for a computer the setting *Radio terminals make radio noise* may silence), on a building Def. `noiseDbmAt1m` is the noise in
    **12.5 kHz**, one voice channel; since 0.45.0 each mode hears it scaled to its own bandwidth (Morse 14 dB less, a picture 6 dB more);
  - `RadioCoaxExtension` (`lossDbPer100m` at `referenceMHz`), on a coax Def listed in the `PhobosRadio_FeedlineNet` pipe net.

`Common/Patches` shows how the vanilla materials and generators get theirs.

**Jammers** (0.45.0) join in C#, not XML: register a source once at start-up with
`Phobos.Radio.Services.RadioJamming.Register(Func<Map, IEnumerable<RadioJammer>>)`. Each `RadioJammer` gives a cell, the watts it sends
out from an omnidirectional antenna, the `BandDef`s it covers (null for every band) and the thing it is. The radio asks it about once a
second, when it takes its noise snapshot, and spreads each jammer's power evenly over the bands it covers; its noise then falls off by
free space with no radius, and enters each receiver through its antenna and cable. A source that throws is left out until the next
start. D'thorga Framework's jam fields come in this way: a `JamField` with `radioWatts` above 0 is heard as a jammer (the radio's
Vehicle Framework tie-in). The player can switch it all off with *Jamming reaches radios*.

**A call sent off the map** (0.46.0) can ask whether it is jammed: `Phobos.Radio.Services.JammedCalls.Jammed(map, cell, out jammingDbm)`
says whether jamming at that cell drowns the far end's reply, by the rules Def's `callOutBand`, `callOutMode`, `callOutReplyDbm` and
`callOutNoiseFigureDb`. One lookup in the net's last noise snapshot. The Dead Man's Switch's uplinks use it (`Mods/DeadMansSwitch`).

**Upgrades** are D'thorga Framework's `Phobos.Framework.Upgrades.PhobosBuildingUpgradeDef` ([building
upgrades](../framework-modding.md#building-upgrades)): give the building `CompProperties_PhobosBuildingUpgrades` and set the
figures the radio reads, `heightMetres` and `gainDbi` on an antenna and `noiseFigureReductionDb` and `channels` on a station.
`Common/Defs/Radio/Upgrades.xml` has ours.

## Lamps, needles and warning lights on a building

`Phobos.Radio.CompProperties_RadioMarks` lays small marks over a building's picture, without replacing the art: a transmit lamp that
lights while the transmitter is keyed, a meter needle that swings with its output, and a red warning light that flashes at night.
Our radio desk, the two amplifiers and the mast carry them.

**Gotchas first:**

- **Set `drawerType` to `MapMeshAndRealTime`.** The marks are drawn each frame, as the game's lamps are. The comp's load check
  refuses `MapMeshOnly`, and the game's `BuildingBase` sets `MapMeshOnly`, so a building made from it must override it.
- **The lamp and the needle need a station or an amplifier.** They read a `CompProperties_Radio` with `isStation` true on the same
  building, or a `CompProperties_Amplifier`. On any other building the lamp never lights and the needle rests.
- **The warning light needs nothing else.** It shows whenever the sky's glow is below 0.35 (dusk to dawn, authored), flashing on for
  30 ticks in every 90 (about 40 flashes a minute, authored). It does not check power.
- **Offsets are in cells from the building's centre**, x to the right and z up the screen, as the building faces north. A building
  that can be turned turns its marks with it.
- Colours are fixed in code (an orange-red lamp, a near-black needle, a red light with a faint glow); the XML cannot change them.

From our radio desk (`mods/PhobosRadioFramework/Common/Defs/ThingDefs/Stations.xml`), trimmed; the offsets were measured from its
picture:

```xml
<ThingDef ParentName="BuildingBase">
  <defName>PhobosRadio_BaseStation</defName>
  <drawerType>MapMeshAndRealTime</drawerType>
  <!-- ... -->
  <comps>
    <li Class="Phobos.Radio.CompProperties_RadioMarks">
      <marks>
        <li><kind>TransmitLamp</kind><offset>(0.555,0,0.570)</offset><size>0.04</size></li>
        <li><kind>Needle</kind><offset>(0.621,0,0.445)</offset><length>0.13</length></li>
      </marks>
    </li>
  </comps>
</ThingDef>
```

And the mast's warning light (`Antennas.xml`, `PhobosRadio_Mast`), at its top:

```xml
<li Class="Phobos.Radio.CompProperties_RadioMarks">
  <marks>
    <li><kind>WarningLight</kind><offset>(0,0,3.16)</offset><size>0.06</size></li>
  </marks>
</li>
```

`CompProperties_RadioMarks`:

| Field | What it does (default) |
| --- | --- |
| `marks` | The marks, one `li` each (empty: nothing is drawn). |

Each mark:

| Field | What it does (default) |
| --- | --- |
| `kind` | `TransmitLamp`, `Needle` or `WarningLight` (`TransmitLamp`, the first). |
| `offset` | Where it sits, in cells from the building's centre, as `(x,0,z)` ((0,0,0)). |
| `size` | A lamp's or a light's size, in cells (0.05). The warning light's glow is drawn three times this size. |
| `length` | A needle's length from its pivot, in cells (0.1). The pivot is at `offset`. |

How the needle moves: at rest it shows 4 % of its sweep. On a keyed station it flutters about three quarters, as a voice signal
would (authored). On an amplifier it shows the amplifier's output against its `ratedOutputWatts`. The sweep is 100 degrees.

## A turret worked from a radio station

`Phobos.Radio.CompProperties_RemoteTurret` makes a turret that fires only while a colonist at a radio station watches it through
its camera, and that colonist may aim it by hand. Our remote gun turret (`PhobosRadio_RemoteTurret`) is the shipped one. The comp has
no fields: naming it is all the XML it takes.

**Gotchas first:**

- **The comp does not hold fire by itself. The building's `thingClass` must be `Phobos.Radio.Building_PhobosRemoteTurret`**, a
  subclass of the game's `Building_TurretGun` that asks the comp before it looks for a target, bursts, or takes a forced target.
  The comp alone only registers the turret with the radio net and writes its inspect line.
- **Under Combat Extended, use `Phobos.Radio.CombatExtended.Building_PhobosRemoteTurretCE`** instead. That class lives in the radio's
  Combat Extended tie-in, which loads only with Combat Extended, so swap the class from a patch that loads only then, as our
  `Mods/CombatExtended/Common/Patches/RemoteTurret.xml` does.
- **It needs a way to a station.** Give it a `Phobos.Radio.CompProperties_FeedlinePort` on the `PhobosRadio_FeedlineNet` so coax can
  join it to a powered station; coax always comes first. A turret no coax reaches can use a radio of its own: a
  `Phobos.Radio.CompProperties_Radio` that is not a station, on the vehicle control kind. Without either, it never fires (unless the
  setting below is on).
- **"Watched" means a colonist works that station's console, and the turret has one of that operator's hands.** An operator has the
  rules Def's `vehiclesPerOperator` hands (2, authored); the station's driven vehicles take theirs first, then its turrets in order.
  While enemies are about, a station with turrets and nobody at its console asks for an operator.
- **"Remote guns fire on their own"** is D'thorga Framework's setting, from its Vehicle Framework part. With it on, every such turret
  fires as an ordinary turret. Without Vehicle Framework there is no such setting, and the turret fires only while watched.

From `mods/PhobosRadioFramework/Common/Defs/ThingDefs/Turrets.xml`, trimmed. The radio's figures are authored [A], the control radio's:

```xml
<ThingDef ParentName="BuildingBase">
  <defName>PhobosRadio_RemoteTurret</defName>
  <thingClass>Phobos.Radio.Building_PhobosRemoteTurret</thingClass>
  <drawerType>MapMeshAndRealTime</drawerType>
  <!-- ... graphics, stats, cost ... -->
  <comps>
    <li Class="CompProperties_Power">
      <compClass>CompPowerTrader</compClass>
      <basePowerConsumption>150</basePowerConsumption>
    </li>
    <li Class="Phobos.Radio.CompProperties_FeedlinePort">
      <pipeNet>PhobosRadio_FeedlineNet</pipeNet>
    </li>
    <li Class="Phobos.Radio.CompProperties_RemoteTurret" />
    <!-- Its own radio, for a turret no coax reaches: a whip about two metres up on the mount. -->
    <li Class="Phobos.Radio.CompProperties_Radio">
      <bands>
        <li>PhobosRadio_VHF</li>
      </bands>
      <mode>PhobosRadio_ControlData</mode>
      <channelKind>PhobosRadio_KindVehicleControl</channelKind>
      <transmitPowerWatts>25</transmitPowerWatts>
      <noiseFigureDb>8</noiseFigureDb>
      <antennaGainDbi>0</antennaGainDbi>
      <antennaHeightMetres>2</antennaHeightMetres>
    </li>
  </comps>
  <building>
    <turretGunDef>PhobosRadio_RemoteTurretGun</turretGunDef>
    <turretBurstCooldownTime>2.5</turretBurstCooldownTime>
  </building>
</ThingDef>
```

`CompProperties_RemoteTurret` has no fields. The hands per operator are the rules Def's (`Phobos.Radio.RadioRulesDef`,
`vehiclesPerOperator`). Without enemies about, a turret's own radio stands by: its link is worked out and shown, but it holds no
channel. A remote gun on a vehicle is a different thing: see [Remote guns](../vehicle-features-modding.md#remote-guns).

## Advice by radio

`Phobos.Radio.RadioAdviceDef` is one kind of help a more skilled colonist can give over the radio: talking another through tending,
surgery, a vehicle repair or trade talks. The carer works at a lifted skill level while the call lasts, usually a little slower, and
learns the skill faster. Each kind is a Def, so a new one is data.

**Gotchas first:**

- **`skill`, `act` and `actLabel` are required**; the Def's load check names any that are missing.
- **The lift is D'thorga Framework's skill lift.** `liftedStats` and `slowedStats` are lifted for the pair of `skill` and `act`. A
  lifted stat counts its factors for `skill` at the lifted level, and only the game's own two kinds of skill factor are read (a base
  plus a bonus a level, and a value for each level); a stat whose skill factor is another mod's own kind is left unchanged.
- **A new `act` name works for jobs on a map.** A call opens when a colonist starts one of `jobs`, and the lift holds while that call
  is live. Lifts before a job starts (`gate`) and on the road (a convoy's tend or repair) come only where D'thorga Framework asks for
  that act by name: `Tend`, `Surgery`, `Trade` and `PhobosVehicleRepair`. A name of your own has no road or gate use unless C# asks
  for it.
- **Both people need radio touch.** On a map, the carer must be served by one of that map's stations (a worn hand-held in touch, for
  example), and the adviser must have a hand-held in touch or sit at a station's desk. The worse of the two links decides the share.
- **`fromFactions` only works for Medicine and Construction.** Another faction has a doctor and a mechanic and nobody else. It also
  needs the player's setting **Other factions' doctors and mechanics give advice by radio** to be on.
- **`aidShare` and `pictureShare` must be above 0** when `aid` or `pictureAid` is named; the load check says so.

How much it lifts: on a clean or workable link the carer closes half the gap to the adviser's level; on a marginal link a quarter
(the rules Def's `adviceShareSolid` 0.5 and `adviceShareMarginal` 0.25, the owner's figures). The aids add their shares to that, up to
the whole gap, rounded down to a whole level. Slowed stats are multiplied by `adviceWorkSpeedFactor`, 0.77 (authored). A call held
by Morse counts as marginal at best.

From `mods/PhobosRadioFramework/Common/Defs/Radio/Advice.xml`, trimmed:

```xml
<Phobos.Radio.RadioAdviceDef>
  <defName>PhobosRadio_AdviceTending</defName>
  <label>tending by radio</label>
  <actLabel>tending</actLabel>
  <skill>Medicine</skill>
  <act>Tend</act>
  <workType>Doctor</workType>
  <liftedStats>
    <li>MedicalTendQuality</li>
  </liftedStats>
  <slowedStats>
    <li>MedicalTendSpeed</li>
  </slowedStats>
  <jobs>
    <li>TendPatient</li>
  </jobs>
  <bookedTicks>1250</bookedTicks>
  <fromFactions>true</fromFactions>
  <aid>PhobosRadio_MedicalChest</aid>
  <aidShare>0.1667</aidShare>
  <pictureAid>PhobosRadio_SSTVKit</pictureAid>
  <pictureShare>0.1667</pictureShare>
</Phobos.Radio.RadioAdviceDef>
```

A riskier kind gets its own switch in the mod's settings. From the surgery Def in the same file:

```xml
<jobs>
  <li>DoBill</li>
</jobs>
<medicalBillsOnly>true</medicalBillsOnly>
<ownSetting>true</ownSetting>
<settingLabel>Radio advice during surgery</settingLabel>
<settingDescription>A more skilled colonist in radio touch can talk a surgeon through an operation, raising its chance of success. If the call drops part-way, the surgeon finishes at their own skill. On by default.</settingDescription>
```

`RadioAdviceDef`:

| Field | What it does (default) |
| --- | --- |
| `skill` | The skill talked through. Required (none). |
| `act` | The act lifted, by D'thorga Framework's name: `Tend`, `Surgery`, `Trade`, `PhobosVehicleRepair`, or a name of your own (see the gotchas). Required (empty). |
| `actLabel` | What the act is called in a sentence, as in "talking Mako through {0}". Required (empty). |
| `workType` | The work an adviser must have switched on in the Work tab (none: the adviser need only be capable of social work). |
| `liftedStats` | Stats whose factors for `skill` count at the lifted level while a call is live, or for the act on the road (empty). |
| `slowedStats` | Work speeds slowed while a call is live, because each step is spoken (empty). |
| `jobs` | A colonist on a map doing one of these jobs opens a call, when an adviser is free and in touch (empty). |
| `medicalBillsOnly` | For a job of bills: only bills on a person, an operation (false). |
| `trade` | The trade negotiator, while trade talks are open. The game stops while you trade, so only an adviser on a hand-held already in touch can take it, and their time is booked when the talks close (false). |
| `gate` | The lift is offered before the job starts, to a skill gate such as a repair's minimum skill, whenever an adviser could take the call (false). |
| `bookedTicks` | How long an adviser stays on the radio after helping with an act that has no job (a tend or repair on the road, the trade talks), in ticks (1250). |
| `aid` | A thing whose presence adds `aidShare` of the gap: carried by the carer, in their convoy, within 8 cells, or in the same indoor room (none). |
| `aidShare` | The share of the gap the aid adds (0). |
| `pictureAid` | A picture's aid, adding `pictureShare` only on a clean link, as noise spoils a picture long before speech (none). |
| `pictureShare` | The share of the gap the picture aid adds (0). |
| `fromFactions` | Another faction's doctor or mechanic may help by radio from one of its settlements in reach, when no colonist can (false). Medicine and Construction only. |
| `ownSetting` | The kind has its own switch in the mod's settings, on until the player turns it off (false). |
| `settingLabel` | That switch's label. Needed when `ownSetting` is true (none). |
| `settingDescription` | That switch's tooltip (none). |

Our four: tending, surgery and trade talks in `Common/Defs/Radio/Advice.xml`, and vehicle repairs (with Vehicle Framework) in
`Mods/VehicleFramework/Common/Defs/AdviceRepair.xml`. The 0.1667 shares (a sixth) are the owner's figures; booked times are
authored. The design is in [radio advice and relays](../development/radio-advice-and-relays.md#radio-advice).

## Far-end radios for the world map's cover picture

`Phobos.Radio.RadioCoverKindDef` is one choice in the menu of the world map's cover picture. Select a place with a radio, pick a kind,
and the picture shows where a radio of that kind could reach it from. Each kind stands for a radio a real contact between tiles uses,
so the picture runs on that radio's own figures. The Def only says which radio, and what differs from it.

**Gotchas first:**

- **Name a radio, through `fitting` or `radioThing`.** A Def with neither fails its load check. A `radioThing` must carry a
  `Phobos.Radio.CompProperties_Radio`, with at least one band and a mode (or give the Def a `mode`).
- **It is a picture only.** It changes no contact; real contacts are worked out by the link budget as always.
- **Our `PhobosRadioCover_VehicleMast` has its height set in code** at start-up, from D'thorga Framework's convoy mast figure. The 10
  in its XML is that figure written out, so a patch to it is overwritten.
- **Our vehicle kinds and the hand-held kind load only with Vehicle Framework** (`Mods/VehicleFramework/Common/Defs/CoverKinds.xml`).
  The relay post is in `Common`.
- The choice is the player's for the session, never saved.

From `mods/PhobosRadioFramework/Mods/VehicleFramework/Common/Defs/CoverKinds.xml`, two of the five:

```xml
<Phobos.Radio.RadioCoverKindDef>
  <defName>PhobosRadioCover_VehicleMast</defName>
  <label>vehicle radio, mast up</label>
  <description>A convoy's fitted radio on a raised mast.</description>
  <fitting>PhobosRadio_FittingVehicleControl</fitting>
  <antennaHeightMetres>10</antennaHeightMetres>
  <order>20</order>
</Phobos.Radio.RadioCoverKindDef>

<Phobos.Radio.RadioCoverKindDef>
  <defName>PhobosRadioCover_Morse</defName>
  <label>vehicle radio, by Morse</label>
  <description>A convoy's fitted radio worked with a Morse key, to a station that has one.</description>
  <fitting>PhobosRadio_FittingVehicleControl</fitting>
  <mode>PhobosRadio_CW</mode>
  <needsKey>true</needsKey>
  <order>50</order>
</Phobos.Radio.RadioCoverKindDef>
```

And a relay, which is called through rather than calling (`Common/Defs/Radio/CoverKinds.xml`):

```xml
<Phobos.Radio.RadioCoverKindDef>
  <defName>PhobosRadioCover_RelayPost</defName>
  <label>relay post</label>
  <description>A radio relay post founded on the tile, on its own mast.</description>
  <radioThing>PhobosRadio_RelayPostUnit</radioThing>
  <caller>false</caller>
  <order>40</order>
</Phobos.Radio.RadioCoverKindDef>
```

`RadioCoverKindDef`:

| Field | What it does (default) |
| --- | --- |
| `fitting` | The fitted radio it stands for, a `Phobos.Radio.RadioFittingDef` (a vehicle's) (none). |
| `radioThing` | Or the thing whose radio comp it stands for, such as a relay's radio unit (none). `fitting` wins if both are given. |
| `antennaHeightMetres` | Its aerial's height in metres where that is not the radio's own, such as a raised mast (0: the radio's own). |
| `mode` | The mode it is worked in where that is not the radio's own, such as Morse (none: the radio's own). |
| `needsKey` | The place selected counts only if one of its stations has a Morse key, as a real contact by Morse needs (false). A relay never has one. |
| `caller` | It is the one that calls (a vehicle), so the link is worked in its mode. False for a relay, which is called through and takes the mode of a convoy selected (true). |
| `order` | Its place in the menu, lowest first; ties go by defName (0). |

The `label` is what the menu and the button show. The design is in [the world coverage record](../development/radio-world-coverage.md).

## The dual-band radio on vehicles

With Vehicle Framework, the dual-band radio fitting (`PhobosFitting_DualBandRadio`) passes calls on: a colonist's hand-held on a map
where no station of theirs serves them (a camp, a ruin, a fight away from home) talks to the vehicle on the same map, and the vehicle
sends the call on at once over VHF between tiles, through any relay, toward home. This is cross-band repeat, as many real dual-band
sets do. Advice by radio uses it. `Phobos.Radio.Vehicles.CompProperties_PhobosCrossBand` is only the vehicle's inspect line that
names whose calls it is passing on.

**Gotchas first:**

- **You never need to write `CompProperties_PhobosCrossBand`.** At start-up it is added to every `VehicleDef` that has D'thorga
  Framework's systems comp, `CompProperties_PhobosVehicleSystems`. Writing it yourself does no harm: a Def that already has it is
  skipped. It has no fields.
- **What makes the vehicle pass calls on is the fitting, not the comp.** The vehicle counts only while a fitting that grants
  `RadioCrossBand` is fitted and its load named `dualBandRadio` has power.
- **Every land vehicle with our systems already has a bay for it.** D'thorga Framework's default loadouts append a `VoiceRadioMount`
  part, the voice radio bay, which the fitting names in its `roles`. Boats have one too; submarines do not.
- **The radio's figures are fixed.** The link is worked out with `PhobosRadio_DualBandUnit`'s radio comp, whichever fitting grants
  the role: a 25 W set on VHF and UHF, on a 3 m whip, 8 dB noise figure, 0 dBi (all authored, a mobile set's).
- **Not on a home map, and not where a station serves the colonist.** The hand-held's hop is that map's own link budget, walls and
  all, and must reach at least a marginal margin; the vehicle's hop is the world's.

The fitting, from `mods/PhobosRadioFramework/Mods/VehicleFramework/Common/Defs/CrossBand.xml`, trimmed. Cost, weight, work and the
load's draw are authored:

```xml
<Phobos.VehicleSystems.PhobosVehicleFittingDef>
  <defName>PhobosFitting_DualBandRadio</defName>
  <label>dual-band radio</label>
  <roles><li>VoiceRadioMount</li></roles>
  <materials><Steel>12</Steel><ComponentIndustrial>2</ComponentIndustrial></materials>
  <kilograms>5</kilograms>
  <work>1200</work>
  <minSkill>3</minSkill>
  <researchPrerequisite>PhobosRadio_LocalRadio</researchPrerequisite>
  <grants><li>RadioCrossBand</li></grants>
  <loads>
    <li>
      <!-- About 2 A at 12 V on average (authored), half that on a 24 V vehicle. -->
      <name>dualBandRadio</name>
      <label>dual-band radio</label>
      <amps>2</amps>
      <shedOrder>4</shedOrder>
      <ampsInputs>
        <li Class="Phobos.VehicleSystems.Input_TwelveVoltShare" />
      </ampsInputs>
    </li>
  </loads>
  <feed>
    <load>dualBandRadio</load>
  </feed>
</Phobos.VehicleSystems.PhobosVehicleFittingDef>
```

`CompProperties_PhobosCrossBand` has no fields. To give your own vehicle the bay, see [A fitting for your own
vehicle](../vehicle-systems-modding.md#a-fitting-for-your-own-vehicle) and [Part roles](../vehicle-systems-modding.md#part-roles).

---

## Dev actions

With dev mode on, under **Actions → Phobos Radio**: log the selected pawns' sheets, log the noise at a cell, recompute the net
now, and time one whole coverage pass of the selected station or antenna (it pauses the game while it runs).

## Rebuilding

```text
python scripts/deploy.py --build --apply PhobosRadioFramework   (with RimWorld closed)
```

See [building, testing and deploying](../development/building.md).

## Checks done

- 4 and 5 Nov 2026: a native 1.7 build beside the 1.6 one, against Vanilla Expanded Framework's PipeSystem for each version; named
  placeholders in messages. Not loaded in the game on either version. (1.6 was retired on 8 Nov 2026: the mod is 1.7 only.)
- 19 Oct 2026: built with 0 warnings against RimWorld 1.6.4871 and PipeSystem 1.0.1.0; the 25 physics unit tests passed; every XML
  file parsed, and every Def reference, texture path and string key resolved; the mod folder passed `scripts/check-mod-layout.py`.
  Deployed to the owner's Mods folder.
- **Not yet tested in game**: the Def loading, the coax net, the gizmos and the mood have not been seen running. Later versions'
  checks are in [the changelog](../../mods/PhobosRadioFramework/CHANGELOG.md).
