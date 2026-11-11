# The player guide's figures

Every figure here is drawn by `scripts/art/guide_figures.py` from textures the mods already ship. **No art of its own is made for
the guide**, so nothing here is generated, and nothing is copied from the game or another author. Each texture's own record, beside
it in its mod's `Textures` folder, says how it was made: generative AI, picked by the owner.

## How they are made

- [figures.json](figures.json) lists each figure: its kind, its labels, and the textures it uses (paths under the repository,
  shortened by the `roots` table at its top).
- **Layers** figures lay a vehicle's or building's pictures on top of each other (a vehicle and its upgrades) and scale each to the
  same number of pixels per game cell, taken from the Def's drawn size, so sizes compare truly.
- **Icons** figures put buttons and items on cards with a label under each. Icons the game tints at run time (the dashboard lamps,
  the triage tags) are tinted here: amber for a lamp, the field triage colours for a tag. Those colours are the guide's choice.
- The panel colours are close to the game's own windows, and dark, so the white button icons show.

```text
python scripts/art/guide_figures.py                  draw every figure
python scripts/art/guide_figures.py --only crawler   draw the figures whose name contains "crawler"
python scripts/art/guide_figures.py --check          check every source exists and every figure is drawn (part of check-all)
```

**Run it again whenever a texture it uses changes**, and commit the redrawn figures with that change.

## Limits

- **A layered vehicle is the guide's view.** Upgrades are drawn where their texture places them; in the game a part may move (the
  survey truck's boom swings out to the clear side when it sets up) or sit slightly differently once seen in play.
- Vehicles are tinted by their paint in the game; the figures show the unpainted texture.
