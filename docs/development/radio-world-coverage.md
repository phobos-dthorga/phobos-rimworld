# Radio cover on the world map, and high ground between tiles (27 Nov 2026)

**Status (27 Nov 2026):** high ground between tiles is built (Radio Framework 0.41.0), not yet tested in game (row 699). The cover
picture for one site and the picture of the whole network are designed here and not built.

The owner asked on 27 Nov 2026 whether radio cover can be seen on the world map as it can inside a colony, what it would take and
what could go wrong, then answered three cards. This record holds the answers, the design of each piece and what is built.

## Read this first

- **Three pieces, in this order**: high ground between tiles blocks signal (built); a painted picture of one site's cover (next);
  one picture of the whole network through every relay (after that).
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

## The cover picture for one site [O], designed

Select a place of the player's on the world map that has a radio (a colony with a powered station, a relay mast, a relay post, a
convoy with a radio), switch the picture on, and every tile in reach is coloured by signal to spare, in the colony overlay's
colours. Hill and mountain tiles light up beyond the flat-ground edge; ground behind a range stays dark.

### The far-end radio [O]

The owner chose buttons to switch. The card offered a hand-held, a vehicle radio, a vehicle with its mast raised and Morse.
**A hand-held is left out** [A]: no contact between tiles uses a hand-held today (a hand-held reaches a vehicle on its own map, and
the vehicle reaches home), and the radio's rule is that a figure the interface shows must change an outcome somewhere. **A relay
mast is offered in its place** [A], as "where would a mast reach this site from" is a real contact and a real decision.

| Button [A] | The far end it stands for |
| --- | --- |
| Vehicle radio | A convoy's fitted radio on its own whip |
| Vehicle radio, mast up | The same radio on a raised mast |
| Relay mast | A relay mast put up on the tile |
| Morse key | A vehicle's radio worked by Morse; only where the site has a key, as relays pass on speech alone |

### The four questions

- **What it lets the player do:** see where a convoy will be in touch before sending it; pick the tile for a mast or a post.
- **What it stops:** nothing.
- **How it changes play:** routes and relay sites are planned from a picture instead of found by trying.
- **What outside notices:** nothing, by choice: it is a picture of rules that already decide contacts.

### Cost, as designed

- **Nothing while the picture is off, or while nothing with a radio is selected on the world map.**
- **While on:** the tiles within the furthest horizon are worked out once, a few milliseconds of real time a frame until done:
  about 1,000 tiles flat to flat, about 22,000 where mountain tiles could be reached. Each tile costs one walk back to the site
  (as a contact's does) and a few sums a band.
- **Worked out again only when** the selection, the button, a station, mast, post or convoy changes.
- **The picture is rebuilt once**, at the end of the working out.
- To be measured on the owner's colony before and after, with the owner's go-ahead.

### Not yet checked

How a mod adds a world draw layer in RimWorld 1.7, and how it behaves on Odyssey's orbit layer. The picture is for the planet's
surface only.

## The whole network [O], designed

One picture of everywhere a convoy could reach home from, directly or through relays. It needs a look-only form of the chain
search first: today's search (`RelayNodes.BestChain`) books channels at each relay, plays the repeater's tone and writes seen-in-play
notes, none of which a picture may do. **Signal is not capacity**: a tile shown as covered can still be refused by a full relay,
and the picture's line will say so.

## Sources

- International Telecommunication Union, [Recommendation ITU-R P.526-16, "Propagation by diffraction" (11/2025)](https://www.itu.int/dms_pubrec/itu-r/rec/p/R-REC-P.526-16-202511-I!!PDF-E.pdf),
  section 4.1, "Single knife-edge obstacle": equation 26 for the edge's parameter and equation 31 for the loss, stated for a
  parameter greater than -0.78. Read from the recommendation's own text on 27 Nov 2026
  ([radio reference checks](radio-reference-checks.md)).
- The four-thirds radius is the radio horizon's own, checked by calculation in the same record.
- The ground heights, the choice of one edge and everything about the pictures are our own authored design. No endorsement by the
  ITU is implied.
