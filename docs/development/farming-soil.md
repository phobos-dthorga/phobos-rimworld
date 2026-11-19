# Soil rules for D'thorga Farming: research and design (19 Nov 2026)

**Status (19 Nov 2026):** research and record only. No code, Def or setting exists for any of it, and nothing here is tested in game. The owner set its direction the same day; the open questions are at the end.

The owner's words, 19 Nov 2026, asked how deep the soil and crop rules should go: "We offer configurable settings for the player to
choose from. I'm honest in saying that I tend to be weird one out in enjoying soil chemistry, realistic farming, etc." On moisture:
"Dub's Hygiene implements/adjusts soil moisture in a way that I find quite good in a games mechanics-to-realistic ratio, and even
refreshing, so that's something I would find a definite and worthwhile study." On seed: "seeds too would be an excellent tradeoff
for the ability to mass-plant and farm at an industrial scale." And, correcting the agent's draft, which had softened the deep
rules: "there should be a more punishing and more realistic mode available for players like me. Soil hydration is the only one I
agree on with you, especially in terms of weather which is sort of outside of the players control. We should be giving the player
choices, not denying or negating them", clarified as "shouldn't", not "never".

This record is the soil side of [farm machines](farming-machines.md) and [farms on other tiles](farming-remote-farms.md).

**How to read the marks:** **[code]** was read in code on 19 Nov 2026, never run; **[code, checked]** was read by the agent writing
this; **[Defs]** comes from the Def tools; **[S]** a secondary source, and **[S, summary]** one read only as a search summary, not
the page; **[C]** textbook chemistry stated from the agent's own knowledge, for the owner, a chemist, to check; **[I]** our
inference; **[A]** an authored figure or choice for the owner to revise. **None of it is tested in game.**

## Read this first

- **Three presets, and a switch for each rule beneath them**: **Off**, **Field care** and **Realistic**.
- **Realistic is punishing on purpose** (owner): harvests take nutrients away, yields fall below the plain game's, and mistakes
  last for seasons. It is off by default and exists for players who want it.
- **Field care never punishes**: care steps add yield, in Farming Simulator 25's manner; neglect only forgoes a bonus.
- **Moisture never punishes, in any preset** (owner): it follows Dubs Bad Hygiene's shape, a bonus that fades.
- **With Dubs Bad Hygiene active there is one soil model, never two**: our machines write into its wetness and fertiliser.
- **Every figure is kept per paddock and changed at events**, never per cell or per tick, and each follows an equation that can be
  caught up over any stretch of time in one step. That is what lets a farm run off the map.
- **Machine sowing uses up seed** (owner).
- **Each preset needs its own figures and its own checks**, and Realistic needs careful tuning: hard, never unfair.

## The owner's decisions (19 Nov 2026)

| Question | The owner's answer |
| --- | --- |
| How deep the rules go | **Configurable settings** for the player to choose from |
| A harsher mode | **Yes: "a more punishing and more realistic mode available for players like me"**, off by default |
| Moisture | **A bonus only**, in Dubs Bad Hygiene's manner: weather is outside the player's control |
| Choices | **We shouldn't deny or negate the player's choices** ("shouldn't", not "never": a strong lean, to be weighed) |
| Seed | **Machine sowing uses up seed** |

## What each behaviour lets, stops and changes

| Behaviour | What it lets the player do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **The presets and switches** | Choose how much farming there is to think about, rule by rule | Nothing: each is the player's choice | A colony's farming is as light or as deep as its player wants |
| **Moisture** (a bonus that fades) | Raise yield by watering, or gain from rain | Nothing: dry soil is the plain game | Watering is worth doing in dry spells, never a duty |
| **Care steps** (Field care) | Add yield by ploughing, fertilising, liming and weeding | Getting the top yield with no care | Each paddock has a short list of steps worth doing each season |
| **Nutrients run down** (Realistic) | Farm a paddock hard for a few seasons, knowingly | Cropping without return for ever: yields fall below the plain game's | Fertiliser, legumes and fallow are planned ahead |
| **The scarcest nutrient sets the yield** (Realistic) | Fix exactly what is short, once tested | Curing everything with one fertiliser | A soil test comes before spending |
| **Acidity and lime** (Realistic) | Keep a paddock in its best range with lime | Using nitrogen fertiliser for years with no thought; over-liming | Lime is a slow, occasional job with long effects |
| **Weeds and disease** (Realistic) | Break them with ploughing, spraying, rotation or fallow | Growing one crop for ever in one paddock | Crops rotate; ploughing and spraying become choices |
| **The soil test and warnings** | Know what is wrong and how badly | Guessing | Decisions rest on figures the player can read |
| **Seed** | Sow at industrial scale | Doing so for nothing | Seed is stored, carted and planned |

## The presets

| Preset | What it does | Can it hurt the player? |
| --- | --- | --- |
| **Off** | Machines only; the game's own fertility | No |
| **Field care** (agent lean: the default) | Visible paddock states; ploughing, fertilising, liming and weeding each add yield; moisture | No: neglect only forgoes a bonus |
| **Realistic** (off by default) | Nitrogen, phosphorus, potassium, acidity, weeds and crop history for each paddock, with real consequences; moisture | **Yes** |

- **Presets are buttons**, not a slider (the owner's standing preference).
- **Beneath them, each rule has its own switch** (SL1): nutrients run down; acidity and lime; weeds; rotation and disease; seed
  use. Changing one shows the preset as "Custom".
- **A switch that makes no sense without another is greyed out and says why**: acidity acts through nutrients, so it needs them on.
  This is where "shouldn't", not "never", applies.
- **A second setting says where the rules apply**: paddocks only (agent lean: the default), or every growing zone. The second
  makes hand farming harder too, and only by the player's choice.
- **Every new setting is reported to the owner by name when built**, as the standing rule asks.

## Moisture: Dubs Bad Hygiene's way, studied

Read from the decompile of its 1.7 build; its public repository holds built files only [code, checked].

- **Two figures for each cell, each 0 to 100**: wetness and fertiliser. Both are saved with the map and drawn as overlays
  (`MapComponent_Hygiene.cs:79-83`, `:359-360`).
- **Wetness only ever helps.** The game's fertility is multiplied by a factor that rises in a straight line from 1 when dry to the
  player's chosen strength when soaked; fertilised soil is multiplied by its own strength (`:997-1009`). Dry soil is never worse
  than the plain game.
- **Water in**: a sprinkler adds 3 to each cell it reaches at each spray, capped at 100, drawing 0.16 units of stored water for
  each cell (`CompSprinkler.cs:113-191`, `CompProperties_Sprinkler.cs:7`); the irrigation sprinkler sprays only during the hour
  from 05:00 (`CompIrrigationSprinkler.cs:21-33`). Rain adds up to 3 times the rain rate at each visit to an unroofed fertile cell,
  evened out by a noise pattern, when the "rain irrigation" option is on (`:1028-1054`). **The owner plays with it on.**
- **Water out**: when it is not raining, each visit takes 2.7 at 0 °C and up to 5 at 40 °C (`:1056-1068`).
- **How often**: the mod visits 0.0006 of the map's cells each tick in random order (`:574-590`), so each cell is visited about 36
  times a day [I]. Soaked soil therefore dries in about a day when cold and about half a day when hot [I].
- **Fertiliser is on or off**: a colonist spreads it, setting the cell to 100; any amount above zero gives the one fixed boost; it
  wears off evenly over the number of days the player set (`:968-986`).

**Why it plays well** (agent's reading): there is no penalty, so no punishing swings; one morning watering gives a boost that
fades through the day, a rhythm the player can see; the strengths are the player's to set. The one RimWorld mod found with a full
moisture model, Terra Project (2019, abandoned), was found punishing by its players [tool].

**What we take from it** (SL2):

- **Our moisture has the same shape**: a bonus only, capped, gained by watering and rain, lost faster in heat.
- **With Dubs Bad Hygiene active, our machines write into its figures**: a sprayer carrying water adds wetness to its strip, and the
  spreader lays its fertiliser through the mod's own `AddFertilizer` (`:968-972`), from a tie-in assembly loaded only when the mod
  is. One soil model in the game, never two.
- **Without it, the same rule runs for each paddock** in our ledger.
- **Off the map**, the ledger mirrors the same rates and reads the player's own Dubs Bad Hygiene settings. Straight-line gain and
  loss can be worked out for any stretch of time in one step.
- **Cheaper than its method**: it sweeps every cell of every map; ours keeps one figure for each paddock.

## Field care (SL3)

The model is Farming Simulator 25 (GIANTS Software): a field has states the player can see, and each care step adds to the yield.

- **What players report of that game** [S, summary: community guides, not the developer's figures, and they disagree in places]:
  fertilising twice adds about 45%; removing weeds early about 20%; lime about 15%, fading over three harvests; ploughing about
  15%; rolling and mulching 2.5% each; together about double the bare yield.
- **Ours** [A]: a paddock shows its states (ploughed, limed, fertilised once or twice, weeds low or high), each step adds a share
  of yield for the coming crop, and the shares add up to a ceiling. The figures are authored for play and set at the first build.
- **Nothing here runs down the soil**: skip every step and the paddock yields what the plain game gives.
- **Weeds return each season** and are cleared by ploughing, cultivating or spraying; left alone they only remove the weeding share.

## Realistic (SL4)

**What it means, written so it is not softened again.**

- **Every harvest takes nutrients away**, in proportion to what is carted off. A paddock cropped without return yields less each
  season and **falls below the plain game's yield**, with no safety net.
- **The scarcest nutrient sets the yield** (Liebig's law of the minimum [C]): plenty of nitrogen does not make up for missing
  phosphorus.
- **Legumes put nitrogen back** [C]; **fallow restores slowly**; fertiliser restores fast and costs goods. Peas, beans and chickpeas
  exist in Vanilla Plants Expanded [Defs]; which crops count as legumes is named in XML for each crop, never worked out from a
  name.
- **Acidity is real chemistry** [C]. Ammonium turned to nitrate in soil releases acid: NH4+ + 2 O2 -> NO3- + 2 H+ + H2O. So
  nitrogen fertilisers sour a paddock over years, ammonium sulphate most. Acid soil locks up phosphorus and stunts legumes. Lime
  takes the acid up, slowly: CaCO3 + 2 H+ -> Ca2+ + H2O + CO2. Too much lime is its own problem: at high pH other nutrients are
  locked up instead.
- **Weeds build up** when nothing checks them and take a share of the crop; **the same crop year after year builds disease**, which
  rotation and fallow break.
- **Mistakes last**: a mined or soured paddock takes seasons to restore.
- **The player is told, never left guessing**: a **soil test** gives the figures (Farming Simulator 25's Precision Farming add-on
  works the same way: sample, then treat [S]); without one, the crop itself gives a plain hint ("pale leaves"); and a warning comes
  before a paddock is badly run down.
- **The same rules run in the ledger**, so a neglected remote farm declines while nobody watches.

**Real proportions, to scale the authored figures** [S, summary]: wheat grain carries off roughly 22 kg of nitrogen, 3 kg of
phosphorus and 8 kg of potassium for each tonne, the last including an allowance for burnt stubble (Incitec Pivot Fertilisers'
trials); other tests give 2.3 to 4 kg of phosphorus and 3.7 to 5 kg of potassium a tonne (Nutrient Advantage). **None was read in
full; no Def may carry one until its page is read** (SL7).

## The equations

- **Each figure moves towards a balance point at a fixed rate**: `X(t) = X_eq + (X_0 - X_eq) x exp(-k t)`. Cropping sets a low
  balance point, fallow a middle one, fertiliser lifts the figure directly. This is the shape Captain of Industry uses for field
  fertility [S: its wiki], and it can be solved for any stretch of time in one step.
- **Moisture and Dubs Bad Hygiene's fertiliser are straight lines**, solvable the same way.
- **So a paddock's soil is updated only at events** (a pass, a harvest, a daily step off the map), and the map and the ledger agree
  because they call the same rule.
- **Yield is changed through the game's own `Plant.YieldNow`**, by one postfix that looks up the paddock under the plant. Every
  harvest, by hand or machine, and every other mod's yield patch then sees the same figure [code].
- **The factor's range is authored** [A]: about 0.3 at the worst to about 1.3 at the best under Realistic; 1.0 to the ceiling under
  Field care.

## Seed (SL5, owner's decision)

- **A seed drill uses up seed as it sows.** Its amount for each cell is authored [A]; real sowing rates are a few per cent of a
  harvest [I, unverified], which is the proportion to aim at.
- **Sowing by hand stays free** (agent's reading, to confirm), unless the player turns the "seed use" switch to cover hands too.
- **Where seed comes from is open** (question 2).

## Fertiliser and the chemistry (SL6)

- **D'thorga Farming ships plain goods** [A]: nitrogen fertiliser, phosphate, potash, lime and a weed killer, with simple recipes
  and trader stock, and takes Dubs Bad Hygiene's biosolids and compost where those mods are active.
- **The real routes belong to the planned chemical industry mod** and replace the simple recipes when it is present [C]: ammonia
  from nitrogen and hydrogen (N2 + 3 H2 -> 2 NH3); nitric acid from ammonia; ammonium nitrate from the two; superphosphate from
  phosphate rock and sulphuric acid (Ca3(PO4)2 + 2 H2SO4 -> Ca(H2PO4)2 + 2 CaSO4).
- **Ammonium nitrate is also an explosive's feedstock**, a tie-in for that mod, noted and not designed here.
- **Crops as fuel already exist** in the owner's game: chemfuel from organics, from wood and from canola [Defs]. A farm at scale
  feeds them; the economy check in [farm machines](farming-machines.md) covers it.

## Mods and games we learn from

| Source | State | The lesson |
| --- | --- | --- |
| Dubs Bad Hygiene (Dubwise; active) | 1.7 | The moisture and fertiliser model above |
| [Crop Rotation](https://github.com/emipa606/CropRotation) (Mlie) | 1.7, updated 12 Nov 2026; MIT | Each cell's crop history and time empty, turned into a yield multiplier through `YieldNow`. It acts only inside growing zones, so it would work on paddocks unchanged |
| [Farming Expansion](https://github.com/emipa606/FarmingExpansion) (Mlie) | 1.7, updated 9 Nov 2026; MIT | Weeds and pests as incidents built on blight; chemicals applied as a work task |
| Fertile Fields (Greysuki's 1.6 continuation) | 1.6; being rewritten | Soil that runs down at harvest by terrain tier, with a "return to soil" switch for each zone. Its only public source is an unofficial fork with no licence: read, never copied |
| Vanilla Plants Expanded (active) | 1.7 | Tilled soil, fertility 1.4, used up at each harvest |
| Terra Project (Lanilor) | 1.0; abandoned 2019 | The warning: a full moisture model with flooding was found punishing |
| Farming Simulator 25 and its Precision Farming add-on (GIANTS Software) [S] | — | Care steps that add yield; soil types with their own acidity and nitrogen, shown by sampling; lime towards a target; spot spraying by weed sensor |
| Captain of Industry [S] | — | One equation for each field: each crop draws fertility to its own balance point; fertiliser raises the ceiling |
| Farthest Frontier [S] | — | Three-year rotations with nitrogen fixers; weeds and rocks that return; disease that builds in the soil |
| Manor Lords [S] | — | Fertility kept for each crop in each field; fallow restores it |

## Cost to run

- **One small record for each paddock**, changed at events. No sweep of cells, no per-tick work.
- **One lookup at each harvested plant** for the yield factor.
- **With Dubs Bad Hygiene active**, its own cell sweep runs as it does today; we add nothing to it.

## Saves

- **Safe to add mid-game**: a paddock starts at a neutral soil, and nothing changes for growing zones that are not paddocks unless
  the player says so.
- **Changing preset mid-game is allowed**: figures the new preset does not use are kept, not thrown away, so changing back loses
  nothing.

## Questions for the owner

| # | Question | Agent lean |
| --- | --- | --- |
| 1 | The default preset | Field care |
| 2 | Where seed comes from: one generic seed, seed kept back from each crop's harvest, or both | Kept back from the harvest, with traders selling seed for a first sowing |
| 3 | Do the rules apply to paddocks only by default, or to every growing zone? | Paddocks only |
| 4 | Under Realistic, how far may a yield fall? | To about a third of the plain game's, never to nothing |
| 5 | Is a soil test a kit a colonist uses, a tractor-mounted sampler, or both? | Both: the kit first |
| 6 | Should Realistic add a moisture penalty as a separate switch, off even there? | No: the owner agreed moisture never punishes |

## Still unknown

- **Whether one postfix on `Plant.YieldNow` is cheap enough at a large harvest**: it should be, at one lookup a plant, but it is
  unmeasured.
- **How Dubs Bad Hygiene's settings are best read from outside it**: its options were read in code, not through an interface.
- **Every real figure above**, read as a search summary only.
- **The textbook chemistry marked [C]** is the agent's own and unsourced: the owner is asked to correct it.

## Sources

- **Dubs Bad Hygiene** (Dubwise): its 1.7 build, decompiled into the ignored `.local/` and summarised in our own words.
- **Public mod source**: Crop Rotation and Farming Expansion (Mlie, MIT).
- **Farming Simulator 25** (GIANTS Software): the Precision Farming add-on's
  [changelog](https://www.farming-simulator.com/changelogs/fs25-precisionFarming.php) and
  [the developer's post](https://www.farming-simulator.com/newsArticle.php?news_id=194); the community
  [wiki page on Precision Farming](https://farmingsimulator.wiki.gg/wiki/Precision_Farming/Farming_Simulator_25). The care-step
  percentages are from community guides read as search summaries, among them
  [itemlevel.net's yield guide](https://itemlevel.net/how-to-maximize-yield-in-farming-simulator-25/); they are not the
  developer's figures.
- **Other games** [S]: the [Captain of Industry wiki on fertility](https://wiki.coigame.com/Fertility); Farthest Frontier's
  [official farming guide](https://www.farthestfrontier.com/guide/gameplay/farming); Gamer Guides on
  [Manor Lords' fertility](https://gamerguides.com/manor-lords/guide/resources/farming/farming-fertility-explained).
- **Nutrients carried off in grain** [S, summary]: Incitec Pivot Fertilisers,
  [nutrient removal after hay or grain harvesting](https://incitecpivotfertilisers.com.au/news-and-insights/agronomic-insights/winter-crop/nutrient-removal-after-hay-or-grain-harvesting);
  Nutrient Advantage's [winter crop insights](https://nutrientadvantage.com.au/latest-publications/agronomic-insights/winter-crop).
  Which page carries which figure is unconfirmed.
