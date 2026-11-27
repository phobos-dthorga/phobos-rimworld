# Radio cover on the world map, and high ground between tiles (27 Nov 2026)

**Status (27 Nov 2026):** all three pieces are built and none is tested in game: high ground between tiles (Radio Framework 0.41.0,
row 699), the cover picture for one site (0.42.0, row 700) and the picture of the whole network (0.43.0, row 701).

The owner asked on 27 Nov 2026 whether radio cover can be seen on the world map as it can inside a colony, what it would take and
what could go wrong, then answered three cards. This record holds the answers, the design of each piece and what is built.

## Read this first

- **Three pieces, all built**: high ground between tiles blocks signal; a painted picture of one site's cover; one picture of the
  whole network through every relay.
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

**Limits and agent choices [A]:**

- The walk over the ground runs from each tile toward the place, as a call from that tile does. A call made the other way (from a
  selected convoy out to a relay) may cross a neighbouring tile, so an edge tile can differ.
- Impassable tiles are not painted: nothing stands there. Water is painted, for boats.
- The picture is of the planet's surface; a place in orbit shows none.
- With nothing with a radio selected the last picture stays up, so a look elsewhere does not lose it.
- The brightest colour is 60 dB to spare (`worldCoverFullScaleDb`), not the colony overlay's 130 dB: tiles are far apart. A shade
  means the same share of each picture's own scale.
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

## Sources

- International Telecommunication Union, [Recommendation ITU-R P.526-16, "Propagation by diffraction" (11/2025)](https://www.itu.int/dms_pubrec/itu-r/rec/p/R-REC-P.526-16-202511-I!!PDF-E.pdf),
  section 4.1, "Single knife-edge obstacle": equation 26 for the edge's parameter and equation 31 for the loss, stated for a
  parameter greater than -0.78. Read from the recommendation's own text on 27 Nov 2026
  ([radio reference checks](radio-reference-checks.md)).
- The four-thirds radius is the radio horizon's own, checked by calculation in the same record.
- The ground heights, the choice of one edge and everything about the pictures are our own authored design. No endorsement by the
  ITU is implied.
