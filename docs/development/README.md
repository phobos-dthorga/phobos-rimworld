# Development and research

Contributor instructions, research, designs and records. Dated records keep their original scope; they don't
promise current behaviour. For using the mods, start at the [documentation index](../README.md).

## Start here

- [Project direction](project-direction.md): intent, the owner's game, decisions with who made them, and open
  checks.
- [Building, testing and deploying](building.md): the checks that run today, where things go, and the C#
  toolchain for RimWorld 1.6 and 1.7.
- [Writing READMEs and in-game text](player-language.md)

## The radio framework

- [D'thorga Radio Framework: design (snapshot, 18 Oct 2026)](radio-framework-design.md): the owner's design,
  from the living doc on claude.ai, which stays the master copy.
- [Radio reference checks (18 Oct 2026)](radio-reference-checks.md): each figure in the design's Reference
  table, checked by calculation or against its primary source, including the Australian licence limits by mode.
- [Map scale research (19 Oct 2026)](map-scale-research.md): what a cell and a world tile measure, from the
  game's own constants, and why Phase 1 uses 1 m per cell.

## Factory additions

- [VFE Factory additions: research and decisions (28 Oct 2026)](vfe-factory-additions.md): how a factory bill
  works, why the stock neutroamine bill already took human meat, the values in play, decisions and ideas;
  the drug-lab recipe scan (86 recipes, 44 ported in 0.3.0), the output-cap rule and the offline checker;
  why bionics had no factory bills, the fabrication bay's bill generator and the machining bay's lost-item bug;
  the output guard for every machine (0.5.1); and the storage switch that lets belts take from any storage
  (0.6.0), with a survey of the owner's storage buildings.
- [VFE Factory conveyors and hoppers (28 Oct 2026)](vfe-factory-conveyors.md): what a belt may pull from (only
  factory hoppers, or storage flagged with Pipe System's extension; since 0.6.0 also switched-on storage), where
  it pushes, what the flag does not change, and the recipe for flagging another mod's storage.

## Artwork

- [Artwork direction (19 Oct 2026)](artwork-direction.md): why PixelLab does not suit RimWorld, the three
  routes (code-drawn, local Flux.1-schnell through ComfyUI, a paid plan last), licences and records.

## RimWorld

- [The owner's mod list](owner-mod-list.md): every mod the owner has on, checked against each mod's own rules,
  with the official Vanilla Expanded collection and suggestions for what to add next. Kept current by
  `scripts/refresh-mod-list.py`.
- [RimWorld modding reference (18 Oct 2026)](rimworld-modding-reference.md): versions, the Waylith engine and
  1.7, package IDs, `About.xml`, load folders, C# build targets, paths, and Ludeon's EULA on mods, with sources.
