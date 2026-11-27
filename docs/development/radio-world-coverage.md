# Radio cover on the world map, and high ground between tiles (27 Nov 2026)

**Status (27 Nov 2026):** all three pieces are built and none is tested in game: high ground between tiles (Radio Framework 0.41.0,
row 699), the cover picture for one site (0.42.0, row 700) and the picture of the whole network (0.43.0, row 701). So are caravans on
foot calling home by hand-held, from the owner's second round of cards (0.44.0 with Framework 0.353.0, row 702), and the third
round's four pieces (0.48.0, rows 712 to 714): the path home seen from the side, the ground in the way named, a relay planner and
full relays marked. The airborne relay the owner also picked is not here: it is row EW7 of
[electronic warfare](electronic-warfare.md), already chosen and being built by that record's session.

The owner asked on 27 Nov 2026 whether radio cover can be seen on the world map as it can inside a colony, what it would take and
what could go wrong, then answered three cards. This record holds the answers, the design of each piece and what is built.

## Read this first

- **Three pieces, all built**: high ground between tiles blocks signal; a painted picture of one site's cover; one picture of the
  whole network through every relay. A fourth followed: caravans on foot call home by hand-held, so the hand-held picture is real.
- **The picture starts off in every game.** Its button is on a colony with a station, a relay mast, a relay post and a convoy with a
  radio, on the world map.
- **High ground between tiles now costs every contact between tiles**, convoys, relays and Morse alike. A link that closed before
  0.41.0 over hills may no longer close. This is the owner's choice.
- **For radio a world tile counts as 1 km** (the owner's gameplay choice of 26 Nov 2026, `metresPerWorldTile`). Every figure here uses it.
- **Performance is the radio's first concern.** Each piece states its cost below; the pictures start switched off.
- Marks: **[O]** the owner's choice, **[A]** the agent's choice or figure, for the owner to revise.

## The cards as put

Each card as the owner saw it on 27 Nov 2026, the chosen option first and marked.

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 1 | Which size of world-map cover picture to design and build | **B, then C**: painted tiles for one site first, then one picture of the whole network through every relay | B alone: painted tiles for one site, with the ring's fix (the agent's lean). A alone: a ring for the player's own colony too, and the drawing moved into the radio's core; horizon only. Nothing for now: record the idea and stop |
| 2 | How to handle that a mountain range between two tiles blocks nothing | **Add terrain blocking**: hills and mountains on the line between two tiles cost signal, for every contact between tiles | Label it: keep the physics and say in the tooltip and guide that ground between tiles casts no shadow (the agent's lean). Decide after seeing it: build the picture first, then choose |
| 3 | Which far-end radio a cover picture is drawn for | **Buttons to switch** between presets, each worked out only when picked (the agent's lean) | Vehicle radio only: simplest, but wrong for a raised mast, Morse or a hand-held. Best case only: one picture for the best radio the game has |

## High ground between tiles [O]

A link between two tiles used to see only distance, the two aerials' heights and the horizon. Now the hills of the tiles on the
way count too.

### The rule

1. **Walk the tiles on the straight way** between the two ends (D'thorga Framework's `TileAir.WalkLeg`, the walk a flight uses).
   The two ends' own tiles are left out: an aerial stands on its own hill.
2. **Each tile's ground stands as high as it would lift an aerial** [A]: flat 0 m, small hills 30 m, large hills 100 m,
   mountainous 300 m (the site heights of 26 Nov 2026), and **impassable mountains 500 m** [A], which nothing can stand on.
3. **The line between the two aerials** runs from one aerial's height (aerial and site together) to the other's, less the planet's
   bulge between them, with the same four-thirds radius the radio horizon uses.
4. **The one edge that stands furthest into the line decides.** Its cost is the single knife-edge of Recommendation ITU-R P.526:
   nothing well under the line, 6 dB when it grazes it, more the deeper the shadow.
5. **A shorter wave bends less**, so the same hill costs UHF more than VHF.
6. **Past the horizon nothing changes**: the link is lost as before.
7. **Flat ground costs nothing here** [A]: it is already counted by plane earth and the horizon.

| Example (146 MHz, our arithmetic) | Cost |
| --- | --- |
| Small hills half way between a 3 m and a 10 m aerial 10 km apart | about 10 dB |
| The same hills, both aerials on 100 m hilltops | nothing: the line clears them |
| A mountain 1 km from a 10 m mast, the far end on a mountain 20 km off | about 32 dB (36 dB on 435 MHz) |

### Simplifications, labelled

- **One edge, never several.** Real paths over two ridges lose more; the sums for several edges are left out.
- **A ridge as sharp as a knife.** Rounded hills lose more than a knife edge; left out.
- **By hilliness, never elevation** [A], to agree with the site heights the owner chose on 26 Nov 2026. A high plateau is not a
  peak. Tile elevation above the sea was the option not taken then, and is not taken here.
- **Tiles on the way are taken as evenly spaced** along the distance.
- **Nothing is kept between askings**: another mod (DMS Expedition's craters) may change a tile's hills in play.

### The four questions

- **What it lets the player do:** find the hilltop that sees over a range; see why two places cannot hear each other; give a relay
  on high ground a real purpose.
- **What it stops:** reaching home from behind hills on a low aerial, where before only distance counted.
- **How it changes play:** where a convoy halts, and which tile gets a mast or a relay post, now depends on the ground between, not
  only the distance. A way round a range can beat a shorter way through it.
- **What outside notices:** a convoy's line names the ground in the way; the station logbook and every feature that asks a convoy's
  reach (ambush warnings, advice, medevac, airdrops, supply runs) follow the same answer. Other factions' radios are not modelled,
  so nothing of theirs changes.

### Risk and settings

**Low** [A]: a convoy can lose touch where it had it, which it can see in its line before it matters, and loses nothing by it at
once. Under the shared rule a low risk needs no switch, and physics is a rule, never a player's setting. The rules Def keeps a
switch for Def packs (`terrainBlocksBetweenTiles`, on).

### Cost

Asked only when a link between tiles is worked out: one walk between the two tiles (a look at each step's neighbours, about seven
small sums a tile of distance) and one sum a tile on the way. A convoy's reach is asked at most once a tenth of a game hour, over a
handful of nodes. **Not measured.**

### As built (Radio Framework 0.41.0)

| Piece | Where |
| --- | --- |
| The rule, game-free, 13 tests | `Physics/TerrainRule.cs`, `tests/PhobosRadio.Physics.Tests/TerrainRuleTests.cs` |
| The ground between two tiles | `Services/WorldTerrain.cs` |
| Every contact between tiles reads it | `LinkBudgetService.EvaluateWorld`, the overload that takes the tiles; relay hops, a convoy's reach and Morse call it |
| The convoy's line when ground is the reason | `PhobosRadioReach_Blocked`: used when the call is inside the horizon and would have reached without the ground |
| Seen in play | `radio.terrain-blocked`, row 699 |

**Limits:** the link sheet shows the cost as its "in the way" term but does not name the tile; a call lost past the horizon still
says so, even with a range in between.

## The cover picture for one site [O]

Select a place of the player's on the world map that has a radio (a colony with a powered station, a relay mast, a relay post, a
convoy with a radio), switch the picture on, and every tile in reach is coloured by signal to spare, in the colony overlay's
colours. Hill and mountain tiles light up beyond the flat-ground edge; ground behind a range stays dark.

### The far-end radio [O]

The owner chose buttons to switch. The card offered a hand-held, a vehicle radio, a vehicle with its mast raised and Morse.
**A hand-held is left out** [A]: no contact between tiles uses a hand-held today (a hand-held reaches a vehicle on its own map, and
the vehicle reaches home), and the radio's rule is that a figure the interface shows must change an outcome somewhere. **A relay
mast is offered in its place** [A], as "where would a mast reach this site from" is a real contact and a real decision.

| Button [A] | The far end it stands for | Needs |
| --- | --- | --- |
| Vehicle radio | A convoy's fitted radio on its own whip (the vehicle control radio) | Vehicle Framework |
| Vehicle radio, mast up | The same radio on a raised mast, at the Datum's own height | Vehicle Framework |
| Relay mast | A relay mast put up on the tile | Vehicle Framework |
| Relay post [A] | A radio relay post founded on the tile | Nothing more |
| Vehicle radio, by Morse | A vehicle's radio worked by Morse; only where a station at the place has a key, as relays pass on speech alone | Vehicle Framework |

Each is a `RadioCoverKindDef` naming the fitting or thing whose radio it stands for, so its figures are the ones a contact uses
and another modder adds a button in XML. **The mode is the caller's**: the far radio's own, or a selected convoy's when the far end
is a relay it calls through.

### The four questions

- **What it lets the player do:** see where a convoy will be in touch before sending it; pick the tile for a mast or a post.
- **What it stops:** nothing.
- **How it changes play:** routes and relay sites are planned from a picture instead of found by trying.
- **What outside notices:** nothing, by choice: it is a picture of rules that already decide contacts.

### Cost

- **With the picture off:** one check a frame while the world map is hidden. On the world map with a radio place selected, its
  radios are read once every 60 frames, for the ring. One comp on every place that can hold a map, which does nothing each tick.
- **While on:** tiles are worked out outward from the place for at most 1.5 ms of real time a frame (`worldCoverBudgetMsPerFrame`)
  until done: about 1,400 tiles for a 10 m mast and a whip on flat ground, about 22,000 where a whip on a mountain could still be in
  reach, never further than 200 tiles (`worldCoverMaxTiles`).
- **A tile costs one look at its neighbours and a few sums a tile of distance.** Each tile's step toward the place is found once and
  kept, so its way back is read, never searched: the steps are the very ones a contact's walk takes.
- **Worked out again only when** the place selected, its radios or the far-end radio change.
- **The picture's mesh is rebuilt** once a second while it is being worked out, so it is seen to grow, and once at its end.
- **Memory:** four arrays the size of the planet's surface while the picture is on (about 9 MB on the owner's 590,492-tile
  world), let go when it is switched off.
- **Not measured.** To be recorded on the owner's colony before and after with `python scripts/perf.py`, on the owner's go-ahead.

### As built (Radio Framework 0.42.0, Framework 0.352.0)

| Piece | Where |
| --- | --- |
| The working out: places with a radio, the spread from the place, each tile's signal to spare | `Services/WorldCover.cs` |
| The losses a contact and the picture share | `LinkBudgetService.WorldLosses` |
| One step of a leg's walk, shared with flights | D'thorga Framework's `TileAir.StepToward` |
| The colour ramp's rule and how far a picture looks, game-free, 11 tests | `Physics/CoverRule.cs` |
| The picture: a draw layer of the planet's surface, added to its Def by patch as four other mods in the owner's list add theirs | `UI/WorldCoverView.cs`, `Common/Patches/WorldCover.xml` |
| The button, its menu and its line in the pane | `WorldCoverGizmo`; a comp on every place that can hold a map, on the relay mast by its Def, on a convoy through its own buttons |
| The ring for a colony, a mast and a post, moved from the vehicles tie-in | `WorldComponent_PhobosWorldCover` |
| The far-end radios | `RadioCoverKindDef`; `Common/Defs/Radio/CoverKinds.xml` and the Vehicle Framework folder's |
| Seen in play | `radio.world-cover`, by far-end radio, row 700 |

**Measuring** [O]: the owner's answer of 27 Nov 2026 was **not now**: the pictures are tried by feel first, and stay "not measured"
until a recording is asked for. The other option offered: record it when first tried, with the picture off, being worked out, and shown
(the agent's lean).

**Limits and agent choices [A]:**

- The walk over the ground runs from each tile toward the place, as a call from that tile does. A call made the other way (from a
  selected convoy out to a relay) may cross a neighbouring tile, so an edge tile can differ.
- Impassable tiles are not painted: nothing stands there. Water is painted, for boats.
- The picture is of the planet's surface; a place in orbit shows none.
- With nothing with a radio selected the last picture stays up, so a look elsewhere does not lose it.
- The brightest colour is 60 dB to spare (`worldCoverFullScaleDb`), not the colony overlay's 130 dB: tiles are far apart. A shade
  means the same share of each picture's own scale.
- **The hand-held, first left out, is now a far-end radio** (Radio 0.44.0): see [Caravans on foot](#caravans-on-foot-by-hand-held-o).
- The button borrows the signal picture; its own icon is requested for a night run
  (`assets/phobos-radio/requests/night-17-cover-icon.json`).
- A colony's ring measures to a vehicle's whip on level ground; a mast's and a post's to a home station's 10 m mast, as before.

## The whole network [O]

One picture of everywhere a call could get home from, straight to a colony's station or passed on by relays. The button's menu
offers it as **Home, through relays** for each far-end radio that calls (a vehicle radio, the same with its mast up, and Morse); it
does not depend on what is selected.

### The rule

1. **Each colony with a powered station is a place**: a call that reaches it is home.
2. **Each relay that reaches home is a place too**, with what it has to spare onward: the best chain from it to any colony's
   station through the other relays, in one hop fewer than a call may take (`maxRelayHops` less one), every hop in touch.
3. **A tile's call through a relay is as good as the worse** of its hop to the relay and what the relay has onward. A chain is as
   good as its worst hop, as in the relay rules.
4. **A tile keeps the best of its ways home.**
5. **Morse goes straight home only**: relays pass on speech.

The onward figure comes from the relay search itself (`RelayRule.Best`, through `RelayNodes.OnwardMargin`) over the same hop the
booking search asks, so it only looks: no channel is booked, no repeater tone plays and nothing is noted. **Signal is not
capacity**: a tile shown as in reach can still be refused by a relay with every channel in use, and the picture's line says so.

### The four questions

- **What it lets the player do:** see the network as a whole, and where its gaps are, before a convoy finds them.
- **What it stops:** nothing.
- **How it changes play:** a repeater vehicle parked, a mast put up or a post founded can be judged by what it adds to the picture.
- **What outside notices:** nothing, by choice: it is a picture of rules that already decide contacts.

### Cost

- **Nothing while it is off.** While on, the colonies' stations and the relays are read once every 60 frames on the world map.
- **On a change only:** the relays' onward figures (one small chain search a relay, each hop one link with its walk over the
  ground), then one spread a place, as one place's picture costs, within the same 1.5 ms a frame.
- **At most 24 places** (`worldCoverMaxPlaces`), colonies first, then relays in the order the game lists them.
- **Not measured.**

### As built (Radio Framework 0.43.0)

| Piece | Where |
| --- | --- |
| The plan of places, the spread over each in turn, a tile keeping its best | `Services/WorldCover.cs` |
| The relay search that only looks | `RelayNodes.OnwardMargin`; the hop shared with the booking search is `RelayNodes.HopMargin` |
| The worse of a hop and the way onward, game-free, tested | `CoverRule.Through` |
| Seen in play | `radio.world-cover-network`, by far-end radio, row 701 |

**Limits and agent choices [A]:**

- Offered only for a far-end radio that calls. A relay mast or post as the far end ("where would a new relay join the network")
  is one place's picture for now.
- A relay on a colony's own tile is left out: a call that reaches that tile is home through the colony's own stations.
- All places must be on the planet's surface.
- Past 24 places the later relays are left out, without a line saying so.

## Caravans on foot by hand-held [O]

The cover picture left out the hand-held the first card offered, as no call between tiles used one. The owner's answer, 27 Nov 2026:
**make it real**. A caravan on foot now calls home by hand-held, and the picture offers one.

### The cards as put (second round)

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 4 | The hand-held left out of the far-end radios | **Make it real**: let a caravan on foot with hand-helds call home between tiles, then add the hand-held picture | Keep it out (the agent's lean): every picture shows reach a real call uses. Add the picture anyway, labelled as showing reach no call uses yet |
| 5 | Record the pictures' cost on the owner's colony | **Not now**: try them by feel first | Yes, record it (the agent's lean): off, being worked out, shown |
| 6 | What reach gives a caravan on foot (several could be chosen) | **All three: airdrops to it, medevac, the in-touch mood** (each the agent's lean) | None left out |
| 7 | Whether relays pass a hand-held call on | **Yes, like a convoy** (the agent's lean) | Straight home only |
| 8 | Which hand-helds count | **Worn or packed**: also a hand-held packed in the caravan's goods | Worn, best one (the agent's lean): only worn, as on a map. Foot caravans only: a vehicle convoy without a vehicle radio stays out of touch |

### The rule

1. **A caravan's radio is its best vehicle radio**, as before. **With none**, its best hand-held: worn by anyone in it or packed in its
   goods [O], the most powerful, then the one held highest [A].
2. **Someone must be able to use it** [A]: a person of the player's in the caravan, not downed, able to talk. A caravan of only the
   downed, or of animals, has no reach.
3. **A hand-held is held in the hand** [A]: its own 1.5 m whip, lifted by its tile's hills; a Datum's mast does not lift it.
4. **Relays pass it on as a convoy's call** [O], taking a channel at each.
5. **What reach gives** [O]: the caravan's line, airdrops to it (which already asked only for reach), a medevac call, and the in-touch
   mood. A caravan on foot with a radio can also put up a relay mast at a halt, as a convoy can: it follows from the convoy's buttons [A].
6. **The in-touch mood on the road** [O] is for any caravan of the player's, on foot or in vehicles. It reads the kept reach and never
   works it out; with the mood setting on, each caravan's reach is kept fresh once a tenth of a game hour.

| Example, our arithmetic (5 W VHF hand-held at 1.5 m, a home mast at 10 m) | Signal to spare |
| --- | --- |
| 10 km over flat ground | about 11 dB: workable |
| 15 km over flat ground | about 4 dB: faint |
| Past about 18 km on flat ground | none: past the horizon |

### The four questions

- **What it lets the player do:** send colonists out on foot and stay in touch; call a medevac or a drop to a walking caravan.
- **What it stops:** nothing that was possible before.
- **How it changes play:** a hand-held packed for a trip becomes worth its weight; a walking caravan picks its route and halts by the
  ground and the relays, as a convoy does.
- **What outside notices:** the caravan's colonists (their mood), home's aircraft (drops, medevac), the relays (channels taken), the
  station logbook (the medevac's calls).

### Risk and settings

**Low** [A]: nothing is lost. A caravan in reach holds places at relays while the mood keeps its reach fresh, which may leave a convoy
calling through a full relay; the convoy's line names the full relay, as before. No new setting; the in-touch mood's own setting stops
the keeping fresh.

### Cost

- **Off the road:** nothing.
- **With the mood setting on:** once a tenth of a game hour, one reach check a caravan of the player's with people in it: a look over
  its people's clothes and its goods for a radio, then, with one, the relay search over a handful of nodes (each hop one link and its
  walk over the ground). **Not measured.**

### As built (Radio Framework 0.44.0, Framework 0.353.0)

| Piece | Where |
| --- | --- |
| The best hand-held a caravan can use | `Services/CaravanRadios.cs` |
| A caravan's end falls back to it | `ConvoyReachSource.ConvoyEnd` (vehicles tie-in) |
| The pane line, the radio's buttons and the medevac on a caravan on foot | Framework's `WorldObjectComp_PhobosCaravanRadio`, on the game's Caravan Def by patch |
| A caravan on foot counts as moving while it walks | Framework's `RoadWork.Moving` |
| The mood on the road | `ThoughtWorker_RadioInTouch`, `AwayReach.KeptCaravanOutcome`, `CaravanTouch`, `WorldComponent_PhobosCaravanTouch`; Framework's `ConvoyRadio.Kept` |
| The hand-held picture | `PhobosRadioCover_Handheld` (the VHF hand-held) |
| Seen in play | `radio.caravan-handheld`, by worn or packed, row 702 |

**Limits [A]:** needs Vehicle Framework, as all reach between tiles does today. The hand-held picture is the VHF hand-held's; a UHF
hand-held reaches a little less over hills. Another faction's caravans are not modelled.

## The third round: four finishing pieces [O]

The owner, 27 Nov 2026, asked where to go next and picked by card.

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 9 | Which small finishing pieces to build (several could be chosen) | **All four: path profile (the agent's lean), relay planner (lean), full relays marked, name the blocking tile** | None left out |
| 10 | Which bigger step to write a design record for | **Airborne relay** (the agent's lean) | Manpack radio: a backpack set for caravans on foot, more power and a taller whip at real weight. HF, Phase 2: skywave, skip zones, day and night bands. None for now |

**Card 10 needed no new record.** An aircraft as a flying relay is row EW7 of [electronic warfare](electronic-warfare.md), which the
owner had opened for building the same day. This session handed that record's session what it knew of the radio side (heights over
the plain in `SiteMetres`, a surface tile under the aircraft, a stable owner for the ledger, and that a moving relay would restart
the network picture) and added the `Airborne` field to `RelayNode` for it, which only the side view reads.

### The path home, from the side (rows 712)

- **A Radio button on a caravan with a radio**, the hand-held's own: its bars by the caravan's kept reach home.
- **The sheet lists each hop** of the chain the reach check last found; out of reach, the one hop straight to the nearest colony, so
  it can say why; by Morse, the one hop in that mode.
- **The side view** draws each tile on the way as a column: the planet's bulge there, then its hills, so the line between the two
  aerials is straight (the usual way a path profile is drawn). Each end stands on its own tile's high ground. The column that
  decides is orange where it costs signal; Engineer adds its cost.
- **The words** give, in Plain, why a hop fails or what the ground costs it: past the horizon, high ground (named by the game's word
  for it and its distance from the hop's first end), or too weak. Operator adds band, mode, margin, distance and horizon, and the
  ground's height over or under the line; Engineer adds both directions' sheets.
- **Show that ground** selects the tile on the globe: tiles have no names, so this is how it is named.
- **Cost:** nothing while the sheet is shut. Open, one look a frame at a kept list; the hops are laid out again only when the reach
  check has run again (at most once a tenth of a game hour): a link and two walks a hop, three hops at most.
- **The four questions:** it lets the player see why a call fails and what would fix it; stops nothing; siting a halt or a relay is
  read off a picture; nothing outside notices, by choice.

### The relay planner (row 713)

The network's picture for a far end that is itself a relay: every tile a new relay mast or post would reach home from, straight
or through the relays there are. A call has taken one hop to reach the new relay, so what lies onward from an existing relay has one
hop fewer again (`maxRelayHops` less two: with three hops, relays that reach a colony directly).

**Not built [A]: how much new ground a relay would add.** That is a whole network picture for every candidate tile. The planner
says where a relay would work; the vehicle picture beside it shows where one is needed.

### Full relays marked (row 714)

The real search leaves out a relay with no room for the caller's kind of call. The network's picture now does the same, asking the
ledger as a caller who holds no place there (`RelayNodes.HasRoomFor`, which books nothing): such a relay is left out of the plan
and of the other relays' ways onward, its tile is painted pink over the picture and it is named in the pane. A relay filling or
freeing a channel starts the picture again, checked every 60 frames. One place's picture is not changed: a full relay's own pane
already says so.

### As built (Radio Framework 0.48.0)

| Piece | Where |
| --- | --- |
| A hop laid out for the sheet | `Services/WorldLinkPath.cs`; the ground as a list is `WorldTerrain.Profile` |
| The side view, the words, the window and the Radio button | `UI/WorldLinkSheet.cs` |
| A caravan's hops kept as nodes | `ConvoyLinks` (vehicles tie-in), kept by `ConvoyReachSource.Reach` |
| The planet's bulge, game-free, tested | `TerrainRule.BulgeMetres` |
| The planner and full relays | `WorldCover.BuildNetworkPlan`, `WorldCover.FullRelays`, `RelayNodes.HasRoomFor`; the mark is the draw layer's |
| Seen in play | `radio.world-link-sheet` (712), `radio.world-cover-network` by far-end radio (713), `radio.world-cover-full-relay` (714) |

## Sources

- International Telecommunication Union, [Recommendation ITU-R P.526-16, "Propagation by diffraction" (11/2025)](https://www.itu.int/dms_pubrec/itu-r/rec/p/R-REC-P.526-16-202511-I!!PDF-E.pdf),
  section 4.1, "Single knife-edge obstacle": equation 26 for the edge's parameter and equation 31 for the loss, stated for a
  parameter greater than -0.78. Read from the recommendation's own text on 27 Nov 2026
  ([radio reference checks](radio-reference-checks.md)).
- The four-thirds radius is the radio horizon's own, checked by calculation in the same record.
- The ground heights, the choice of one edge and everything about the pictures are our own authored design. No endorsement by the
  ITU is implied.
