# Radio visuals: showing the link budget as pictures (23 Nov 2026)

**Status (24 Nov 2026):** partly built. RV5 to RV8 and RV3 are built in Radio Framework 0.6.0
([round two](#round-two-rv5-to-rv8-and-rv3-radio-060-24-nov-2026)); not yet tested in game: rows 468 to 472. RV1, the coverage
overlay, and the noise fade (F2) are built in Radio Framework 0.2.0
([as built](#as-built-radio-020-24-nov-2026)); RV2, the ray line, and RV4, the side view, in 0.3.0
([as built](#as-built-radio-030-24-nov-2026)). Not yet tested in game: rows 456, 457, 459 and 460. Every other idea is unbuilt. The
owner reopened the radio on seeing the mock-up ([below](#reopened-23-nov-2026)).

The owner, 23 Nov 2026: "If I were to reopen the Radio mod for Rimworld, what are some strikingly visual ways I could show its
systems?" Then, of the three ideas the agent put first: "draw the dark mock-up page of the top three, PLEASE <3 But make sure to
document everything still~". This record is that documentation. The design it serves is
[radio-framework-design.md](radio-framework-design.md); the radio's rules are in `src/PhobosRadio/AGENTS.md`.

## Read this first (gotchas)

- **This record was written while the radio was on the back burner**, as an answer to what could be done if it were reopened. The
  owner then reopened it the same day ([below](#reopened-23-nov-2026)); the rest of the record reads as written before that.
- **Every idea is an agent's proposal** for the owner to revise. None is tested, in game or on screen.
- **The mock-up is a drawing in a browser, not the game.** Its map, colonists and colours are invented. Its numbers are the mod's
  own rules and figures, ported to JavaScript by the agent and checked on a handful of cells.
- **Radio Framework 0.1.6 is itself not yet tested in game**, so the figures the mock-up runs on are untested too.
- **Of the three, the waterfall has the least to show while radio stays on one map** ([finding F4](#what-running-the-figures-showed)).
  The agent's recommendation is the coverage overlay and the ray line first.

**How to read the marks.** **[code]** was read in our code or Defs on 23 Nov 2026, never run in the game. **[run]** was computed by
the mock-up's port of the rules, on its invented map. **[game]** was found in the indexed decompile with `scripts/query-code.py`
and not checked against the installed 1.7 build. **[A]** is an agent's proposal, choice or authored figure, for the owner to revise.
**[O]** is the owner's word.

## What the mod shows today

All **[code]**. The radio draws nothing on the map.

| What | Where | What the player sees |
| --- | --- | --- |
| The Radio button | `src/PhobosRadio/UI/SignalGizmo.cs` | Signal bars, and the link sheet on a click |
| The Net tab | `src/PhobosRadio/UI/ITab_RadioNet.cs` | Who is in touch and which antenna the station uses |
| The link sheet | `src/PhobosRadio/UI/Dialog_LinkBudget.cs` | Every gain and loss as a column of numbers |
| Siting text | `src/PhobosRadio/UI/PlaceWorker_AntennaSiting.cs` | Height, roof and noise at the cursor while placing |

The design's placement preview also promises "expected coverage" while placing an antenna. That part is not built.

## The mock-up page

- **Where:** <https://claude.ai/artifact/N2Jd8RDdjY83bNbxSCCx5V>, a private page only the owner can open. Its source is not in this
  repository **[A]**: the repository holds no web pages, and the page can be read back from that address.
- **What it holds:** one invented map (a stone house, a wooden barn, a generator, two batteries, a mountain with rooms cut into it,
  two rock outcrops), drawn three times: with the coverage overlay, with the ray line and its side-on profile, and as a waterfall.
- **What can be changed on it:** the antenna (built-in whip, whip, mast) and where it stands, the hand-helds' band, the generator
  on or off, the overlay's colouring, the display level (Plain, Operator, Engineer), and where the selected colonist stands. Every
  control is a button; the two maps also take a click or the arrow keys.
- **What is real in it:** the rules and figures in the table below, each read from the file named.
- **What is invented for it [A]:** the map; the colonists' names; one channel per colonist on the waterfall (the mod has one
  channel per band); every door counted as closed; the colours; which of four reasons the Plain line gives (the largest of
  distance, obstruction, cable loss and noise rise, which may differ from `LinkText`'s choice).

| Rule | Figure | From |
| --- | --- | --- |
| Distance | 20 log d(km) + 20 log f(MHz) + 32.45 dB, at 146 MHz or 440 MHz | `Physics/Propagation.cs`, `Defs/Radio/Bands.xml` |
| Walls and doors below 2.5 m | wood 6 dB, stone 15 dB; UHF counts 0.75 of it | `Patches/Obstruction.xml`, `Rules.xml`, `Bands.xml` |
| Natural rock | 30 dB a cell crossed, at any height | `Rules.xml` |
| Roof over each end | built 6 dB, thin rock 20 dB, overhead mountain 60 dB | `Rules.xml` |
| Hand-held | 5 W, 0 dBi, 1.5 m, noise figure 8 dB | `ThingDefs/Handhelds.xml` |
| Base station | 25 W, noise figure 6 dB; built-in whip 0 dBi at 2 m | `ThingDefs/Stations.xml` |
| Antennas | whip 0 dBi at 3 m; mast 3 dBi at 10 m | `ThingDefs/Antennas.xml` |
| Coax | 8 dB per 100 m at 146 MHz, rising with the square root of frequency | `ThingDefs/Feedline.xml`, `Physics/Coax.cs` |
| Noise | generator −60 dBm at 1 m to 12 cells; battery −85 dBm to 6 cells; inverse-square | `Patches/Noise.xml`, `NoiseFloor.cs` |
| FM voice | 12.5 kHz wide, needs 12 dB above the noise, keyed a fifth of the time | `Defs/Radio/Modes.xml` |
| Outcomes | marginal from 0 dB to spare, workable from 6, clean from 15 | `Rules.xml` |
| Scale | 1 m a cell, the mod's default | mod setting |

**How it was checked:** the page's model was run outside a browser on nine cells, for each of the three antennas with the generator
on and off. Its drawing code has not been looked at by anyone but the owner.

## What running the figures showed

All **[run]**: the mock-up's map, VHF, the mast seven cells from the generator unless said. These are the reason to draw before
building: each one changes a design below.

| | Finding | The figures | What it changes |
| --- | --- | --- | --- |
| F1 | **Coloured by outcome, the map is flat.** | With nothing in the way a hand-held has 89 to 138 dB to spare with the generator off; "clean" starts at 15. | The overlay is coloured by signal to spare, on a scale of 0 to 130 dB. The four outcomes are a second view. |
| F2 | **A generator's noise ends at a hard edge.** | With the antenna moved out of the generator's range: 12 cells from the generator a hand-held has 70 dB to spare; at 13 cells, 108. The noise at 12 cells is still 43 dB over the quiet floor. | The overlay draws the rule's cut-off as a sharp ring. Players would see it. With the antenna inside the range the ring does not show, because F3 already limits every contact. |
| F3 | **Noise at the antenna dims every contact at once.** | The station's floor rises from −127 to −77 dBm, and every cell loses 50 dB. Down the mountain corridor goes from 48 dB to spare to no contact. | This is the mod's strongest siting lesson, and bars cannot show it. The overlay does in one look. |
| F4 | **On one map, signals are huge or gone.** | At the antenna: +2 dBm from the yard, −23 at the mountain doorway, −66 down the corridor, nothing from the deep room. | A waterfall has little between bright and dark to show in Phase 1. It suits Phase 2's weak signals better. |
| F5 | **A mast's advantage over walls is visible.** | Behind the stone house the mast's line passes one wall below 2.5 m and clears two; the 3 m whip's passes two and clears one. | The ray line marks cleared walls as well as counted ones, so the difference can be seen. |

**F2 is an observation, not a proposal.** Whether the noise rule should fade out instead of stopping is a physics question for the
owner when the radio is reopened.

## The three in the mock-up

Each answers the four questions the shared rules ask of a new behaviour. **None puts the player's things or people at risk**, so the
rule on risk settings asks for no off-switch **[A]**; each is a toggle or shows only on selection anyway.

### RV1: Coverage overlay

A toggle, like the game's roof and fertility overlays. Every cell a colonist could stand on is coloured by how much signal a
hand-held there would have to spare. Brighter is better; hatched black is no contact.

| The question | The answer |
| --- | --- |
| What it lets the player do | See where radio reaches before sending anyone, and see what moving an antenna or a generator would change. |
| What it stops them doing | Nothing. It shows; it decides nothing. |
| How it changes the way they play | Antennas get sited by looking, not by walking a colonist about and reading bars. |
| What outside it notices | Nothing, by choice **[A]**: it is a view of rules that already act on colonists' mood. |

- **It completes a promise.** The same drawing, shown while placing an antenna, is the design's "expected coverage" preview.
- **How the game could draw it [game]:** `Verse.CellBoolDrawer` draws a coloured cell layer with a colour per cell and redraws only
  when told (`SetDirty`); the game's own overlays use it. It is drawn each frame from `MapComponent.MapComponentUpdate`.
- **Where its switch goes:** the game's row of overlay switches is `RimWorld.PlaySettings.DoPlaySettingsGlobalControls` **[game]**.
  Adding a switch there needs a Harmony patch, and the radio mod has none today. **[A]** A button on the station and on each
  antenna needs no patch; start there.
- **What it costs.** A 250 by 250 map is 62,500 cells, each needing a line to the antenna checked cell by cell. **[A]** Work it
  out only while the overlay is on, in coarse blocks first and finer after, spread over many ticks, and again only when a wall, a
  roof, an antenna or a noise source changes.
- **Choices it forces [A]:** one band at a time (a VHF view and a UHF view); doors counted as closed, since they open and close
  too often to redraw for; with several antennas, each cell shows the best one, as the station already picks.

**As built, resumable (Radio 0.44.1; the owner's report of 27 Nov 2026: it "restarts everytime you click away").** The cause: the
picture was worked out only while its station was selected, and any change on the map meanwhile (an edifice built or gone, a roof, fog,
a noise source switching) made a finished picture start again from 0%; selecting an antenna threw the station's picture away. Now
**each station and antenna keeps its own picture, the last three [A]**, and **the last one shown finishes its pass in the background**
at `coverageBackgroundBudgetMsPerFrame` (0.5 ms a frame [A], against 1.5 while shown), never starting a new one unseen. **A change
updates a finished picture over the old one**, said as "updating" and painted until the new pass ends. Cost: one float a map cell for
each picture kept (about 0.25 MB each on a 250 by 250 map); nothing runs once the last picture is done. Not measured; row 704.

### RV2: Ray line with the walls marked

Select a colonist who wears a radio and a line runs to the antenna in use, coloured by the result. Each thing in the way gets a
mark. The mock-up adds the same path seen from the side (RV4), which belongs in the link sheet window.

| The question | The answer |
| --- | --- |
| What it lets the player do | See why one colonist's contact is weak: which wall, which rock, which roof. |
| What it stops them doing | Nothing. |
| How it changes the way they play | A weak link becomes a thing to fix (knock out a wall, raise the antenna), not a number to accept. |
| What outside it notices | Nothing, by choice **[A]**. |

- **The marks:** a filled dot for a wall the line passes below wall height, a hollow dot for a wall it clears, a square for rock. The
  stretch of the line above wall height is dotted, so a mast's line visibly lifts over the base.
- **Display levels change only what is drawn**, as the radio's rules require: Plain shows marks and one reason in words and never a
  decibel; Operator adds the margin; Engineer puts a figure on every mark.
- **How the game could draw it [game]:** `Verse.GenDraw.DrawLineBetween` for the line, from the selected thing's own draw-extra
  hook; the marks are small quads. It draws only for what is selected, so it costs almost nothing.
- **It needs one thing the service does not return today [code]:** `ObstructionService.LossDb` returns a total. The ray needs
  the list of cells that counted. **[A]** Return that list from the service, so the picture can never disagree with the result.

### RV3: Waterfall on the base station

A picture in the station's tab, in the manner of a software radio's display such as
[SDR++ (Alexandre Rouma)](https://github.com/AlexandreRouma/SDRPlusPlus): frequency across, time down, brightness for strength at
the antenna, speckle for the noise the station hears. A dashed line marks what FM voice needs above the noise.

| The question | The answer |
| --- | --- |
| What it lets the player do | See the noise floor rise and fall, and see who is heard but not readable. |
| What it stops them doing | Nothing. |
| How it changes the way they play | Little in Phase 1 (F4). In Phase 2 it becomes how band conditions are read. |
| What outside it notices | Nothing, by choice **[A]**. |

- **It must stay honest.** The radio's rule is that a figure the UI shows must affect an outcome somewhere. Brightness comes from
  the link budget's received level and the speckle from the noise service; nothing is decoration.
- **It suits the owner.** The owner reviews sound by spectrogram, so this is radio that can be seen instead of heard.
- **Untested assumption:** a picture written a row at a time means using a Unity texture directly, and 1.7 runs Unity calls
  through a compatibility layer. Check that before designing further.
- **The mod has no channels [code]**, so today every hand-held would sit on one streak. Separate streaks need either channels (a
  new rule) or a drawing that fans them out for show, which would break the honesty rule. **[A]** That is a second reason to wait.

## Every idea, in one table

All **[A]**. The phases are the design's own; an idea needs its phase to exist first.

| | Idea | Phase | What the player sees |
| --- | --- | --- | --- |
| RV1 | Coverage overlay | 1 | Cells coloured by signal to spare; rock and walls cast shadows (above) |
| RV2 | Ray line with wall marks | 1 | A line from a selected colonist to the antenna, with a mark on each thing in the way (above) |
| RV3 | Waterfall on the station | 1, better in 2 | A scrolling picture of signals and noise (above) |
| RV4 | Side-on path profile | 1 | In the link sheet: mast, walls at 2.5 m and the line between, seen from the side. In the mock-up |
| RV5 | Step chart of the budget | 1 | In the link sheet: each loss a step down, each gain a step up, the margin as the gap; both directions |
| RV6 | Transmit lamp and meter needle | 1 | Marks laid over the base station's art while it transmits |
| RV7 | Warning light on the mast | 1 | A red light that blinks at night |
| RV8 | Coax tinted by loss | 1 | A selected run, bright at the station and dimmer along its length; water damage in green later |
| RV1b | World-map cover | 2 | Tiles coloured by signal to spare with a selected site; built in Radio 0.42.0 ([radio cover on the world map](radio-world-coverage.md)) |
| RV9 | Skip-zone rings | 2 | On the world map, a dead ring between ground wave and the first hop, moving with the time of day |
| RV10 | The grey line | 2 | The dawn and dusk band glowing on the globe, with the paths that ride it lit |
| RV11 | Contact arcs | 2 | Arcs to each faction worked, coloured by signal, in the manner of [PSKReporter (Philip Gladstone)](https://pskreporter.info/) |
| RV12 | Band rose | 2 | A compass with a petal per band, longer where that band is open |
| RV13 | Solar flare wash | 2 | HF washed out across the sunlit half of the planet |
| RV14 | Direction-finding bearings | 3 | Bearing lines from two stations crossing in a diamond that shrinks with more bearings |
| RV15 | Exposure ring | 3 | How far the player's own transmission can be traced, growing with power |
| RV16 | Microwave beams | 3 | A thin beam between dishes that dims in rain |
| RV17 | Antenna lobes | 3 | A directional antenna's pattern as a petal that turns with its rotator |
| RV18 | QSL card wall | 4 | Cards as art items, and a world map that fills in as each biome is worked |
| RV19 | Moonbounce echo | 4 | A pulse leaving and returning about 2.5 seconds later |
| RV20 | Hydrogen-line strip chart | 4 | A bump at 1420 MHz built up over several nights |

## Cautions

- **Keep movement to what is selected or switched on [A].** Rings pulsing from every transmission would clutter a large colony and
  cost frame time across the owner's mod list; the owner's high-end PC is a best case for players.
- **Marks over art, never replacement art.** RV6 to RV8 lay marks over what is drawn, as the vehicle damage overlays do.
- **Interfaces follow the game's own patterns.** An overlay that looks unlike the game's overlays will be noticed at once.
- **A setting added for any of these is reported to the owner** when it is built: its label, what it does, its default and where.

## For the owner, if the radio is reopened

All **[A]** recommendations; each is the owner's to decide.

1. **Build RV1 and RV2 first**, with RV4 inside the link sheet. They need only what Phase 1 has, and they would help judge Phase
   1's gate ("the 6 dB rule feels right in play") by letting the rule be seen.
2. **Leave RV3 for Phase 2**, when weak signals and band conditions give it something to show.
3. **Decide F2**: keep the noise rule's hard edge and let the overlay show it, or fade the rule out.
4. **Decide where the overlay's switch lives**: a button on the station (no patch), or the game's overlay row (the mod's first
   Harmony patch).

## Reopened (23 Nov 2026)

The owner, on seeing the mock-up: "HOLY SHIT I AM PLAYING THE HELL OUT OF THIS WHEN DEVELOPED." Then: "Reopen the radio: button
first, and fade the noise out. I'm unsure how to answer the question about the button without more context first and before I
answer #2, I can see these kinds of features becoming a bit of a performance hog if we don't treat them with due care. As such, put
in the AGENTS for this mod that performance is our outmost concern for this mod."

| What | Who | Where it stands |
| --- | --- | --- |
| The radio is reopened | **[O]** | The root `AGENTS.md` and `src/PhobosRadio/AGENTS.md` say so |
| Performance is the mod's first concern | **[O]**; its four working points are **[A]** | `src/PhobosRadio/AGENTS.md`, "Performance first" |
| Where the overlay's switch lives (item 4 above) | **[O]** | **Shown while a station or an antenna is selected**: no button, no patch |
| The noise rule's hard edge (item 3 above, F2) | **[O]** | **A taper to the edge** for now; ground-wave physics may be tried later |

**Context the owner was given for the switch [game]:** ten active mods in the owner's list already add to the game's overlay row
(`python scripts/query-patches.py who "PlaySettings.DoPlaySettingsGlobalControls"`, 23 Nov 2026), one of which also removes
switches it judges unused. Where the switch lives changes nothing about cost: the cost is in working the overlay out.

**The owner's words on the noise fade (question card, 23 Nov 2026):** "Taper to the edge as our choice to develop for now while in
the future, we can possibly experiment with ground-wave physics once more features are developed and we can do a benchmark against
everything else to get a true feeling for its effects." The ground-wave option offered was the plane-earth model: beyond a crossover
distance of about 4π·h₁·h₂/λ (roughly 9 m for a hand-held and 60 m for a 10 m mast on 2 m, by that textbook form) noise falls 12 dB
per doubling instead of 6. It would raise using the same model for the signals, which changes every link figure. **[A]** Left as a
later experiment, measured against the rest of the mod as the owner asked.

## As built: Radio 0.2.0 (24 Nov 2026)

What differs from, or settles, the design above. All **[code]**; nothing is tested in game.

**Its cost, stated before building** (the radio's performance rule):

| What | How much | How often | What starts it again |
| --- | --- | --- | --- |
| Nothing selected | One field read a frame | Every frame | — |
| A station or antenna selected, picture current | A few comparisons a frame | Every frame while selected | — |
| Working cells out | At most 1.5 ms of real time a frame (`coverageBudgetMsPerFrame`) | Only while selected and not finished | Selecting another station or antenna; a building that is an edifice, coax or an antenna port spawning or going; a roof changing; the noise sources changing; the cell scale changing |
| One cell | The game's straight line to each antenna, cell by cell, stopping early once the link is lost or cannot beat the best so far; a few logarithms per band | Once per pass | — |
| The quick pass | One cell per 4 by 4 block (`coverageCoarseBlockCells`): about 3,900 cells on a 250 by 250 map | Only when a new station or antenna is selected | — |
| The full pass | Every standing, unfogged cell: up to 62,500 on a 250 by 250 map | After the quick pass, and once per change | — |
| Redrawing the picture | The game's cell drawer rebuilds its mesh | Only at the end of each pass, never mid-pass | — |
| Memory | One float per map cell (250 KB on a 250 by 250 map), kept while the map is loaded | — | — |

**How it is measured:** the dev action **Time coverage of selected station or antenna** times one whole pass and logs milliseconds,
microseconds a cell and how many frames that is at the budget. Then `python scripts/perf.py` on the owner's colony, with the station
selected and not, **asked for first**. Until both are done, the 1.5 ms budget is an authored guess. **[A]** The estimate before
measuring was a few microseconds a cell, so a full pass of several seconds at the budget; the quick pass in a fraction of a second.

**What was built:**

- **Shown while selected [O].** `CompRadio` (a station) and `CompAntenna` ask for the picture from their selection-overlay hook,
  each frame they are selected; `RadioCoverage`, a map component, does the work in its update and draws through the game's
  `CellBoolDrawer`. A station shows the best of all its antennas; an antenna shows its own reach through its station; an antenna
  with no station on its coax says so in its inspect pane.
- **The picture cannot disagree with a contact.** Every cell goes through the same walk (`ObstructionService.Walk`), the same
  inputs (`LinkBudgetService.Inputs`) and the same arithmetic (`LinkMath`, game-free and tested) as `LinkBudgetService.Evaluate`.
  Two choices are the agent's **[A]**: every door counts as closed, so the picture does not redraw when one swings; and each cell
  shows the better of the hand-held kinds the game has (VHF or UHF).
- **Colouring [A]:** the mock-up's ramp by signal to spare, 0 to 130 dB (`coverageFullScaleDb`), at 45% opacity; dark red for no
  contact. Fogged cells, walls and rock are left unpainted.
- **The noise fade [O]:** `NoiseFloor.FadeDb`, a smoothstep power weight from two thirds of each source's radius
  (`noiseFadeStartFraction`, 0.67) to nothing at the radius; 1 keeps the old hard edge. It changes every contact near the edge of
  a noise source, not just the picture.
- **Cheaper on the way:** summing noise and two levels no longer allocates, and a contact walks its line once for both bands
  instead of once per band.
- **Not built:** nothing on the game's overlay row. RV2 and RV4 followed in 0.3.0 (below).

## As built: Radio 0.3.0 (24 Nov 2026)

The owner, 24 Nov 2026: "you can go ahead with RV2 and RV4 now". All **[code]**; nothing is tested in game.

**Their cost, stated before building** (the radio's performance rule):

| What | How much | How often |
| --- | --- | --- |
| Nothing selected | A look at an empty selection list | Every frame |
| Colonists selected | For each selected colonist (pawns only), a look at their worn apparel for a radio | Every frame |
| A colonist with a radio selected | One line (two where it passes wall height), and one quad or ring per mark, at most 120 marks; at most 6 colonists drawn at once | Every frame while selected |
| Working out the path | One walk of the line with a list of what it met: the same walk as the contact | Once per contact the net hands out, about once a second per selected colonist, and only for those drawn |
| Labels (Operator and Engineer) | One label (Operator) or one per run of the same thing, plus roofs (Engineer), made once per contact and display level, drawn once per frame | While drawn |
| The side view | A few boxes and lines per mark, in a window | Only while the link-budget window is open |
| Memory | The path and its labels are kept with the contact and dropped with it | — |

Measured: not yet; it is small by construction. `python scripts/perf.py` with a colonist selected, asked for first, would show it.

**What was built:**

- **RV2, the ray line [O].** `RadioPathView`, a map component, looks at the selection each frame; for each selected colonist wearing
  a hand-held with a contact, it draws a line from where the net last saw them to the antenna that contact uses, in the contact's
  colour (clean, workable, marginal, no contact). Thick where the line is below wall height, so walls count; thin where it is above
  them. Marks: a filled orange square on a wall or door that cost signal, a hollow ring on one it passed over, a red diamond on rock.
  Operator adds "*clean, 43 dB to spare*" at the middle; Engineer adds what each run of the same thing cost and each roof. Plain
  draws no numbers.
- **RV4, the side view [O].** `PathProfile`, drawn at the top of the link-budget window in every mode: the station's antenna on the
  left, the colonist on the right, at their heights; wall height dashed; walls the line met (filled below it, outlined when cleared),
  rock full height, a roof over either end; the line coloured by the contact. Engineer labels each run's cost, skipping any that
  would overlap.
- **One walk, three uses.** `ObstructionService.Walk` gained an optional list of what it met (`CrossingMark`); `LinkPath` runs it
  once per contact for the line and the side view, with the same settings as the contact. A contact and the coverage pass no list
  and pay nothing for it (a cleared wall's loss is looked up only when a list is kept).
- **Choices that are the agent's [A]:** the line starts where the net last saw the colonist, so it can trail a walking colonist by up
  to a second rather than disagree with its marks; at most 6 colonists and 120 marks drawn; no setting to hide the line, as it costs
  little and shows only on selection.

## Round two: RV5 to RV8 and RV3 (Radio 0.6.0, 24 Nov 2026)

The owner, 24 Nov 2026: "begin RV5 through RV8, including RV3, all step-wise if necessary". Each is its own commit under Radio 0.6.0.
**Their cost, stated before building** (the radio's performance rule):

| Idea | What draws | How often | What it costs |
| --- | --- | --- | --- |
| RV5 step chart | Boxes and lines in the link-budget window | Only while the window is open | A dozen boxes per direction per frame; no work outside the window |
| RV6 lamp and needle | A small quad and a thin quad over the desk and each amplifier | Each frame each one is on screen (the game culls those off screen) | Two meshes a building; the state is read from fields the rare tick already keeps |
| RV7 mast light | One small quad above each mast | Each frame a mast is on screen, at night only | One mesh; the sky's glow is one field read |
| RV8 coax tint | One quad per cell of each cable run | Only while its station or an antenna is selected | The run's cells are kept with its route (the route cache already exists), so nothing is walked per frame |
| RV3 waterfall | A 256 by 128 picture in a tab on the radio terminal | Only while the tab is open: one new row every tenth of a second | 32,768 pixels copied and sent to the graphics card ten times a second; the signals are the net's, read, never worked out |

**Making buildings draw each frame:** RV6 and RV7 need the desk, the amplifiers and the mast drawn each frame as well as in the map's
baked mesh (`MapMeshAndRealTime`), as the game's own lamps are. That is a few more draw calls per such building on screen.

**Honesty [A]:** the lamp lights and the needle rises only while the station is in use, for the mode's duty cycle (a fifth of the
time); the amplifier's needle shows its output against its rating. The waterfall's brightness is each hand-held's real received
level and its speckle the station's real noise floor, **each radio was drawn on its own spot of the band to tell them apart**
(0.6.0). **Since Radio 0.7.0 each sits on the channel the ledger gives it** ([channels](radio-stations.md#channels-radio-070)), so
that fiction is gone.

**As built (all [code], none tested in game):**

- **RV5**, `BudgetSteps` in the link-budget window: Operator the deciding direction, Engineer both; the window is now 640 wide.
- **RV6 and RV7**, `CompRadioMarks`: marks as data on a building (`TransmitLamp`, `Needle`, `WarningLight`, each at an offset in cells,
  measured from the shipped pictures on 24 Nov 2026). The lamp and needle follow the station's in-use state for the mode's duty cycle,
  phased per station; an amplifier's needle shows output against rating; the light flashes about 40 times a minute below a sky glow
  of 0.35. Agent choices **[A]**: the timings, the colours and the night threshold.
- **RV8**, `RadioCoaxView`: the route cache now keeps each run's cells in order (`FeedPath.Cells`); the tint is rounded to eighths, as
  the game keeps a material per colour asked for. Water damage is not modelled, so there is no green.
- **RV3**, `ITab_RadioWaterfall`, always on the radio terminal (`waterfallBuiltIn`), and on the desk only with the setting **Show the
  waterfall on the radio desk**, off by default (the owner, 24 Nov 2026: "Put the waterfall on the desk too but as a setting, disabled by
  default"): the colour
  ramp is shared with the coverage overlay (`RadioPalette`); the grain uses Unity's random numbers, never the game's, so the picture
  cannot change what happens in play; it stops while the game is paused.

## For another game

The link budget's arithmetic is already game-free (`src/PhobosRadio/Physics/`, tested without the game). The three pictures are
not: each rests on RimWorld's cell grid, walls and overlay drawing. A second game would need its own way to say what lies between
two points, and its own drawing; the colouring by signal to spare, the cleared-or-counted marks and the display levels would carry
over as designs.

## Sources

- Our own code and Defs, read 23 Nov 2026: the files named in the tables above, under `src/PhobosRadio/` and
  `mods/PhobosRadioFramework/Common/`.
- The game's drawing code: `Verse.CellBoolDrawer`, `Verse.GenDraw.DrawLineBetween`, `Verse.MapComponent.MapComponentUpdate` and
  `RimWorld.PlaySettings.DoPlaySettingsGlobalControls`, found by name with `python scripts/query-code.py find` in the indexed
  decompile. Their bodies were not read, and they were not checked against the installed 1.7 build. **Read each before building
  on it.**
- SDR++ and PSKReporter are named only as likenesses a radio operator would recognise. Nothing of theirs is used, and neither
  endorses this work.
