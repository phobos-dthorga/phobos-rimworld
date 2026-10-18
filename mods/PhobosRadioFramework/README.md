# D'thorga Radio Framework

Radio that works like radio: every contact is decided by a real link budget, and doubling the distance
costs 6 dB. This is **Phase 1**: local VHF and UHF on one map.

Version 0.1.0, built 19 October 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Requires Vanilla Expanded Framework** (its pipe network carries the coax feedline). Load this mod
  after Harmony, Core, the DLCs and Vanilla Expanded Framework.
- **Built for RimWorld 1.6.** On 1.7 it loads the same build and is untested there.
- **Decide before you start a colony:** the mod setting *Metres per cell* (default 1) changes every path
  and feedline loss. Changing it mid-save changes which links work.
- **Safe to add to a running save.** Removing it is safe once you have deconstructed every radio building
  and dropped every hand-held; the first load afterwards shows one harmless "could not find class" error.
- **The hand-held is worn on the belt slot**, so a colonist cannot wear a shield belt and a hand-held at
  the same time.
- **Nothing is published on the Steam Workshop yet.** Install from source with `scripts/deploy.py`.

## What it does

Research **Local radio** (after Microelectronics Basics). Then:

| Thing | Where | What it is |
| --- | --- | --- |
| Hand-held radio (VHF) and (UHF) | Machining table, 20 steel + 1 component | 5 W, worn on the belt. VHF reaches further outdoors; UHF copes better with walls. |
| Radio base station | Architect → Misc, 100 steel + 3 components | 2 × 2, needs power (60 W idle, more while in use), works both bands, built-in whip. |
| Coax feedline | Architect → Misc, 2 steel per cell | Laid like power conduit, from the station to an antenna. Loses signal per metre; can run under walls. |
| Whip antenna | Architect → Misc, 15 steel | 1 × 1, 3 m high, no gain. Cheap way to get the antenna outside. |
| Mast antenna | Architect → Misc, 60 steel + 1 component | 1 × 1, 10 m high, 3 dBi. Sees over walls. |

A colonist wearing a hand-held shows a **Radio** gizmo with signal bars. Click it for the full
link-budget sheet. The station's gizmo and inspect text say how many hand-helds are in touch, and which
antenna it is using. A free colonist whose link is workable or clean gets the small **in touch with
home** mood; a marginal link gives a fainter one.

**What decides a link** (both directions; the worse one counts):

- distance, by the inverse-square law (doubling it costs 6 dB);
- walls and closed doors on the straight line, by material (wood least, plasteel most), but only where
  that line is below 2.5 m: a tall antenna sees over walls, the last few cells near the hand-held bite;
- natural rock, 30 dB per cell, whatever the heights;
- the roof over each antenna: a built roof costs a little, thin mountain roof a lot, overhead mountain
  almost everything;
- coax length: 8 dB per 100 m on VHF, more on UHF;
- your own generators and batteries, which raise the noise floor near them.

The station uses whichever antenna on its coax gives the best link, or its built-in whip if that is
better or nothing is connected. A mountain base needs coax out to an antenna on the surface.

**Display modes** (mod settings): *Plain* shows bars and one reason; *Operator* adds band, SNR, margin
and the limiting direction; *Engineer* shows every term on the sheet. Click a radio's gizmo for the
link-budget window, which can show one level more than your setting. The station has a **Net** tab.

**While placing** an antenna or the station, the cursor shows its height, the roof over that cell and
the colony noise there. Red means overhead mountain: nothing gets through it.

**Dev mode** (Actions → Phobos Radio): log the selected pawns' sheets, log the noise at a cell,
recompute the net now.

## What to try first (the owner's play test)

1. Research Local radio; build the station indoors under a built roof; craft a VHF hand-held and wear it.
2. Walk the wearer outside, then behind a stone wall, then into a mountain: watch the bars and the reason.
3. Lay coax from the station out of the building to a mast antenna; see the station switch to it.
4. Stand near a running generator: the noise line should appear and the margin fall.
5. Open the Engineer sheet: every term should add up to the margin, and the limiting direction should
   make sense (the 5 W hand-held usually limits, not the 25 W station).
6. Check the needs tab for "in touch with home".

## For modders

Everything is a Def: `Phobos.Radio.BandDef`, `Phobos.Radio.ModeDef`, one `Phobos.Radio.RadioRulesDef`
(the tuning values), `Phobos.Radio.CompProperties_Radio` on any apparel or building,
`Phobos.Radio.CompProperties_Antenna` plus `Phobos.Radio.CompProperties_FeedlinePort` on an antenna
building, and three `DefModExtension`s: `RadioObstructionExtension` (`lossDb` per crossing, on a stuff or
building Def), `RadioNoiseExtension` (`noiseDbmAt1m`, `radiusCells`, `noiseWhen` = PowerOn, Connected or
Always, on a building Def) and `RadioCoaxExtension` (`lossDbPer100m` at `referenceMHz`, on a coax Def
listed in the `PhobosRadio_FeedlineNet` pipe net). `Common/Patches` shows how the vanilla materials and
generators get theirs. No C# needed to add a radio, a band, an antenna or a noisy machine.

## Rebuilding

```text
dotnet build src/PhobosRadio/PhobosRadio.csproj
python scripts/deploy.py --apply PhobosRadioFramework
```

See [building, testing and deploying](../../docs/development/building.md).

## Checks done

- 19 Oct 2026: builds with 0 warnings against RimWorld 1.6.4871 and VEF's PipeSystem 1.0.1.0; the 25
  physics unit tests pass; every XML file parses, every Def reference, texture path and string key
  resolves; the mod folder passes `scripts/check-mod-layout.py`. Deployed to the owner's Mods folder.
  **Not yet loaded in the game**: the Def loading, the VEF net, the gizmos and the thought are untested.
