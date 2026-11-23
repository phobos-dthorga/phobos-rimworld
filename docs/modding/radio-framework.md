# D'thorga Radio Framework for modders

How your own mod can add a radio, a band, an antenna or a noisy machine to D'thorga Radio Framework, with no C#. Players start at
[the mod's README](../../mods/PhobosRadioFramework/README.md) and [the player guide](../player-guide/radio.md). The design, its
phases and its physics are in [the radio framework design](../development/radio-framework-design.md). Work on the radio
resumed on 23 Nov 2026, after its pause from 9 Nov. **Not yet tested in game.**

## Defs to extend

Everything is a Def:

- `Phobos.Radio.BandDef` and `Phobos.Radio.ModeDef`: bands and modes.
- One `Phobos.Radio.RadioRulesDef`: the tuning values, including coverage's limits.
- `Phobos.Radio.ChannelKindDef` (0.8.0): a kind of channel use, with a `label`, a `mode` and an `order` (lower is served first
  when channels are short), and `preempts` (0.9.0: when every station it reaches is full, it takes channels from the kinds
  served after it that do not pre-empt, the weakest first and no more than it needs). Kinds fill blocks of channels of their
  own and are listed apart on the station. Ours: `PhobosRadio_KindVehicleControl` (pre-empts), `PhobosRadio_KindVehiclePicture`
  and `PhobosRadio_KindLocalVoice`. Add one for a new use of the radio rather than a second system of frequencies.
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
    for a computer the setting *Radio terminals make radio noise* may silence), on a building Def;
  - `RadioCoaxExtension` (`lossDbPer100m` at `referenceMHz`), on a coax Def listed in the `PhobosRadio_FeedlineNet` pipe net.

`Common/Patches` shows how the vanilla materials and generators get theirs.

**Upgrades** are D'thorga Framework's `Phobos.Framework.Upgrades.PhobosBuildingUpgradeDef` ([building
upgrades](../framework-modding.md#building-upgrades)): give the building `CompProperties_PhobosBuildingUpgrades` and set the
figures the radio reads, `heightMetres` and `gainDbi` on an antenna and `noiseFigureReductionDb` and `channels` on a station.
`Common/Defs/Radio/Upgrades.xml` has ours.

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
