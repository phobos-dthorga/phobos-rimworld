# Vehicle paint masks: by region, repaired by parts (16 Nov 2026)

**Status (17 Nov 2026):** built: every vehicle body's paint mask is made by region and repaired with Segment Anything's parts map (`repaired`, Vehicles 0.56.1, 16 Nov 2026), and the fleck checker counts what is left; the overlay fit is an experiment, not built. Not yet tested in game.

How our vehicles' paint masks (the `m` files, which tell the game where to tint) are made and checked. The rules are in [AGENTS.md, "Artwork and sound"](../../AGENTS.md#artwork-and-sound); the background AI, which looks at every picture before this step, is in [background clean-up](background-cleanup.md), and the whole pipeline in the [art pipeline](art-pipeline.md).

*Moved here on 17 Nov 2026 from `background-cleanup.md`; kept as written.*

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
