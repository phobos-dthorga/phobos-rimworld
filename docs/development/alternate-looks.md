# Alternate looks: our good pictures as choices, not leftovers (19 Nov 2026)

**Status (19 Nov 2026):** the owner's idea, recorded with what exists to build it on, and **held for later** at the owner's word: pursued when they have the tokens to do it properly, and only for the pictures it suits best. Nothing is built; nothing is tested in game.

The owner, later the same day: "when I have the tokens and such to pursue it properly, I guess we'll reserve it for the assets and the
like it would work most appropriately for". So it is not a rule for every picture: when it is taken up, each candidate is judged on
whether a choice of look fits it (agent's reading: buildings and items through a Customise option first, where nothing hangs off the
picture; vehicles only where the default's overlays fit the alternate body).

The owner's words, 19 Nov 2026, picking the combine harvester's first body from three good attempts: "#1 by default because I feel
that we use all of these. There was a mod or something that I used a long time ago, but it allowed you to change the style of
items/objects if there were other assets available for them. It might've been available as part of VFE or something. I think we
implement a feature likewise for our own perusal where we have similar but otherwise, nice looking, assets that would typically go
unused still."

**How to read the marks:** **[code]** was read in code on 19 Nov 2026, never run; **[I]** our inference; **[A]** an agent's proposal
for the owner to revise.

## Read this first

- **The idea:** a vehicle, building or item of ours that has more than one good picture lets the player choose which it wears. The
  first pick stays the default.
- **Every generated picture is already kept** (the owner's rule of 8 Nov 2026), so the pictures exist; what is missing is a way to
  ship and choose them.
- **Two mechanisms already exist** [code]: Vehicle Framework's **skins**, chosen in its paint window, and Vanilla Expanded
  Framework's **graphic customisation**, a "Customise" option on a building or item. The second is very likely what the owner
  remembers.
- **The catch for vehicles: overlays.** Each upgrade's overlay is drawn to fit one body. A second body with its auger or cab in a
  different place needs its own overlays, or its overlays will float off the body.
- **First candidates:** the combine's attempts 2 and 3 (19 Nov 2026), and the bulk bunker's attempt 2 (17 Nov 2026), which the owner
  already kept "for other uses".

## What it lets, stops and changes

| Lets the player | Stops them | Changes how they play |
| --- | --- | --- |
| Give a machine or building the look they like, and tell two of the same kind apart at a glance | Nothing: a look changes no figure | A fleet or a farm can look like the player's own; a captured enemy machine can be told from the player's own |

## What exists

- **Vehicle Framework's skins** [code]: `SkinDef`, a kind of `PatternDef` drawn with its own shader (`CutoutComplexSkin`), with a
  texture path, per-facing pictures (turned by code where a facing is missing), and `exclusiveFor`, the vehicles it belongs to
  (`Source/Vehicles/Graphics/Patterns/PatternDef.cs:10-116`, `SkinDef.cs:8-11`). Skins are chosen where paint patterns are, in Vehicle
  Framework's own paint window, so **no interface of ours would be needed for vehicles** [I]. How the skin shader treats our paint
  masks is not read yet.
- **Vanilla Expanded Framework's graphic customisation** [code]: `VEF.Graphics.CompGraphicCustomization`, which adds a "Customise"
  option to a thing's menu and opens a window of the looks it has (`CompGraphicCustomization.cs:140-170`). For our buildings and
  items, where Vanilla Expanded Framework is present.
- **The game's own styles** (Ideology's `ThingStyleDef`) change a building's look by ideoligion. They need the Ideology DLC and follow
  the ideoligion, not the player's choice of one building, so they fit less well [I].

## The proposal [A]

1. **Vehicles**: each alternate body ships as a skin for its vehicle alone, chosen in Vehicle Framework's paint window. An alternate
   ships only with its own overlays, or once the record shows the default's overlays fit it; until then it waits.
2. **Buildings and items**: Vanilla Expanded Framework's customisation where that framework is present (it is a hard dependency of
   D'thorga Medical and Factory Additions already); elsewhere, a small comp of our own in the Framework core with the same idea.
3. **Records**: every shipped look has its texture record, as every texture does; `config/art-audit.json` names which look is the
   default.
4. **Paint masks**: an alternate body gets its own masks by script, and the fleck count before the owner sees it, as any body does.

## Questions for later

| Question | Agent lean |
| --- | --- |
| Do alternates need their own overlays, or only ship where the default's fit? | Only where they fit, at first |
| Can raiders' and other factions' vehicles wear an alternate at random? | Yes: it makes enemy fleets less uniform, at no cost |
| Which mod holds the shared comp for buildings? | D'thorga Framework's core |
