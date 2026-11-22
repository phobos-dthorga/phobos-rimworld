# Vehicle paint masks: by region, repaired by parts (16 Nov 2026)

**Status (17 Nov 2026):** built: every vehicle body's paint mask is made by region and repaired with Segment Anything's parts map (`repaired`, Vehicles 0.56.1, 16 Nov 2026), and the fleck checker counts what is left; the overlay fit is an experiment, not built. Not yet tested in game.

How our vehicles' paint masks (the `m` files, which tell the game where to tint) are made and checked. The rules are in [the art and sound rules](../../assets/AGENTS.md); the background AI, which looks at every picture before this step, is in [background clean-up](background-cleanup.md), and the whole pipeline in the [art pipeline](art-pipeline.md).

*Moved here on 17 Nov 2026 from `background-cleanup.md`; kept as written.*

**The paint's own lines join it (23 Nov 2026, D'thorga Vehicles 0.106.1):** the owner's review found repainted trucks looked "rusty":
regions leave a panel's thin lines and rims out of the paint, so they kept the master's olive under any colour. `absorb_paint_coloured_edges`
(in `scripts/art/export_vehicle.py`) grows each paint outward, a touching pixel at a time and three master pixels at most, into pixels of
its own hue that are darker than the paint (value 0.12 to 0.55) and at least half its saturation floor, inside a Segment Anything part that
mostly carries that paint; a small gap left in a joined line (40 pixels or fewer, ringed by that paint) fills. A cream roof of the same
hue is lighter and stays, the outline stays, and nothing joins across an outline (each tried and seen: smeared roofs, then loose islands,
then dotted lines, before these limits; all figures authored). Then `repaint_vehicles.py stage --paint repaired` mends the flecks
`paint_check.py` still finds at game size with its `fill_flecks` (`mend_flecks`), so a staged body reaches the owner at 0. Shipped for the
tipper, bulk tanker, drill rig, generator set and survey truck; **the owner held back the Pintle bowser, field kitchen and flatbed
trailer** (23 Nov 2026), and said that if they cannot be put right they become non-recolourable. **The Pintle bowser followed in D'thorga Vehicles 0.106.2**
(the owner's pick, 23 Nov 2026, after a second try): its red dashes along the tank had two causes. Its stripe is drawn a strong orange
(the Def tints it blue) at hues up to 42, where the dull olive body starts, so `repaint_vehicles.py` gives it the hues 8 to 42 for the
second paint and `MAIN_CEILING` 0.35 (no main paint more saturated). And the tank's warm shaded edge (saturation 0.11 to 0.13) sat just
above its floor of 0.12; no single floor parts it from the walkway's pale rails, so `settle_faint` (`SETTLE_FAINT`, per body) drops paint
within `PAINT_DOUBT` (0.03) of the floor inside a Segment Anything part carrying under `PAINT_DOUBT_SHARE` (12 %) of that paint: the
tank's sections carry 0 to 2 % and the slivers of its edge 6 to 10 %, the hoops with the walkway plates crossing them 16 to 25 %. A
small dropped patch ringed by one paint keeps it. Tried and set aside: floors of 0.14 (patchy walkway rails) and 0.16 (a broken walkway),
a pixel-by-pixel part rule (a speckled crossing plate) and a 5 % line (red specks the owner found on the tank's right side). Per body
because on the generator set it broke up a painted rim that was right as shipped; on the survey truck it would clear a few red dashes
from its cream panel, the owner's call. **The field kitchen and flatbed trailer are not offered paint** (the owner's choice of
23 Nov 2026, D'thorga Vehicles 0.106.3): `repaint_vehicles.UNPAINTED` stages them in their own colours with no mask (`repaint_body`'s
`natural`), `apply` removes their masks, and their Defs use plain Cutout, so Vehicle Framework shows no paint button
(`VehiclePawn.DoInspectPaneButtons` asks whether the shader takes a colour mask). Tried first, and set aside with the paint: the
kitchen's khaki shutter frames (saturation 0.17 to 0.25 about its 0.21 floor) settled by part with a 0.05 band, which left dashes where
Segment Anything gave a frame's edge to the body; the flatbed's rails, whose lighter edge (0.15 to 0.17 about a 0.16 floor) and outer line
were joined to the paint inside a mostly painted part (counted without its outline: 43 % with it), which left pale dots at its edge. D'thorga
Farming's bodies use the same exporter and are checked next. Not yet tested in game.

**D'thorga Farming's machines are not offered paint either** (the owner's choice of 23 Nov 2026,
Farming 0.7.7), after its bodies were checked: repainted red, the combine showed green lines round its panels and the T60 green blotches
in its recesses, as the trucks had. A re-export was set aside: the exporter has moved on since Farming was made (new transforms, new
outlines), so it would have shifted every picture and its fitted parts. Laying only the line rule's additions over the shipped pictures
cured the lines, but the owner chose no paint. `scripts/art/bake_paint.py` makes a painted picture unpainted from the shipped files alone:
the grey texture times its vehicle's Def colours under the mask (what the game drew), the mask removed, and the pale rim cleaned (below),
so nothing moves; `assets/phobos-vehicles/farming-unpainted.json` names every picture and its colours, and `export_farming.py` bakes after
every export. **The rim** (the owner's "white dithering" at every border): exports before the soft-rim fix (5c6d04e8) left a half-transparent
pale grey pixel outside each outline; every partly transparent outer pixel paler than the pixel inside it (by 24) now takes that pixel's
colour, keeping its transparency. **Open, next by the owner's card:** white background left inside some Farming pictures (the cultivator's
roller gaps and a tine wedge, the T400B's lower-link eyes); shiny parts are as white and flat (the plough shares, the combine's cab roof),
so the background AI on each master is to judge them.

**For a new body:** run `scripts/art/paint_segments.py` on its master, then `python scripts/art/repaint_vehicles.py stage --paint repaired`, `review` and `apply --paint repaired`, and `python scripts/art/paint_check.py` before the owner sees it.

## Paint masks, by region (16 Nov 2026)

**Owner's report (16 Nov 2026):** the Pintle bowser's texture and paint mask "look atrocious", and the owner asked that the AI be used for
everything. The background AI had been used (its answers match every shipped texture but the three held back on 12 Nov); what looked bad
was the **paint mask**, made a pixel at a time by hue and saturation, so texture noise and soft edges flipped single pixels: orange flecks
along the stripe, half-painted hoops. BiRefNet knows object from background, not one panel from another, so it cannot make a paint mask.

- **Two methods, compared side by side on all ten bodies (owner's choice):** by **regions** (`region_kinds`: alike neighbours with the same
  paint grow into a region inside the outlines, never across a change of paint; a region of fewer than 24 pixels takes the paint of the
  large region it touches most) and by **Segment Anything 2.1's parts** (Meta, Apache 2.0, `scripts/art/paint_segments.py` on the
  graphics card: 2 min 14 s for all ten masters, about 1,000 to 2,400 prompt points each). Segment Anything cut panels well but swallowed
  thin details (the bowser's stripe) and left flecks on the Crawler. **The owner picked regions for all ten.** **Later the same day the
  `repaired` paint replaced them** (regions repaired with the parts map, Vehicles 0.56.1; see the fleck checker below), and it is the
  one to use now. The parts maps stay in
  `.local/art/segments` for any later comparison.
- **Never judge a vehicle texture raw:** painted parts are stored grey for the game to tint. The review page tints them roughly as the game
  does (the texture multiplied by the Def's colours under the mask) on dark ground.
- The commands as first written: `python scripts/art/repaint_vehicles.py stage --paint regions`, `review`, `apply --paint regions`.
  **Use `--paint repaired` now** (16 Nov 2026), after `paint_segments.py` on each master; `parts` needs
  `paint_segments.py` first (`masters` lists them). Each body reuses its own saved transform, so overlays still register.
- Not yet tested in game.

## The fleck checker (16 Nov 2026)

The owner's go ("Absolutely") for an automatic inspector after the bowser's mask reached them speckled. `python scripts/art/paint_check.py`
reads each shipped body's north mask, maps it onto Segment Anything's parts map of its master, and counts **flecks**: an island of one
paint, 12 texture pixels or fewer, **inside** a part that is at least 80 % another paint, ringed on every side by that paint (so a bolt
head in its own outline, a seam at a part's edge and a long thin stripe are never flecks), **and visible**: painting it like its panel
would move its drawn colour by 24 or more in some channel, with the vehicle's own Def colours (so near-dark weathering, which looks the
same painted or not, is left out). All figures authored. It draws each body's flecks in `.local/art/paint-check`, prints a count, and
exits 1 past `--allow`; the review page shows each version's count. **It is a fair judge of the region masks, not of the parts masks**: those were painted by the same parts the checker judges by, so they score better partly by construction (on the review page, the bowser 15 by regions, 1 by parts). Its core (`find_flecks`) is tested on made-up pictures
(`tests/test_paint_check.py`). It needs the parts maps, which live only on the owner's PC, so it is an art step, not a gate step.

**What it found, 16 Nov 2026** (north facings; old per-pixel masks, then today's region masks): tanker 63 then 18, survey truck 76 then 5,
drill rig 23 then 6, generator set 57 then 29, tipper 0 then 0, flatbed 25 then 3, bowser 86 then 15, Crawler 108 then 87, its research
refit 425 then 36, its sensor refit 294 then 272. **The region masks are much cleaner but not clean.** The Crawler's dark blue-grey hull
shows the weathering specks drawn into its art that the regions leave unpainted (chains of them grow past the 24-pixel small-region
limit). **Repaired and shipped the same day (Vehicles 0.56.1; owner: "Yes, repair and show me", then "Yes, all ten").** The `repaired` paint
(`region_kinds` with the parts map) repaints two kinds of island inside a part that is at least 80 % one paint, each ringed on every side
by that paint: a region of up to 400 master pixels, and a **dark speck** of up to 40, weathering drawn darker than the outline threshold
and so never painted, which shrinking blended into half-painted pixels. The first kind alone barely helped; the dark specks were the
cause. After: tanker 0, survey truck 1, drill rig 5, generator set 15, tipper 0, flatbed 2, bowser 9, Crawler 22, research refit 3,
sensor refit 24, mostly blends at seams. **For a new body: run `paint_segments.py` on its master, then repaint with `repaired`.**

## Experiment for later: overlay fit (owner, 16 Nov 2026: "documenting the overlay fit as an experiment for later")

Not built. The idea: Segment Anything's parts map of a body says where each of its parts sits; an upgrade overlay drawn for one of them
(the bowser's lagged jacket over its tank, a tanker's liquid tank on its deck) should cover that part and little else. For each overlay
with a named target part (picked by a point in the body master, beside its `--fit` box), measure on the north facing: the share of the
target part the overlay covers (a jacket that leaves the tank's ends bare), the overlay's pixels beyond the part (an overhang onto the
mudguards), and the gap between the overlay's outline and the part's (a reel floating clear of its bracket). Report each, with a picture
of the three in colour, and flag past authored limits (agent's first guess: under 95 % covered, over 3 % overhang). What it would catch:
a `--fit` box read off by eye a few pixels wrong, or an overlay drawn a little narrower than its part (the bowser's jacket is about 6 %
narrower than its tank). What it would not: whether an overlay looks right, which stays the owner's eye. Cost: processor only, once the
body's parts map exists (about 10 s on the graphics card per body).
