# QSL cards, worked out (28 Nov 2026)

**Status (28 Nov 2026):** part A (what the card carries) is built in Radio 0.58.0, part B (how it looks) in 0.59.0, and the band, mode,
relays and the player's own callsign in 0.62.0. None is tested in game (test rows 749, 750 and 764). The owner asked on 28 Nov 2026 where procedural design could go with QSL cards, and chose
"A and then B" of the agent's four groups; C (cards as things in play) and D's remaining parts are [leftover work](#leftover-work).

## Read this first

- **Nothing here is drawn by hand or stored as a picture**: every callsign, locator and picture is worked out from what the game
  already knows (the tile's latitude, longitude, biome and hills, the faction, the contact), from a seed, so the same contact always
  gives the same card.
- **Performance first** (the radio's rule): nothing runs per tick or per frame for a map. Part A is a few string sums when a card is
  earned. Part B draws each card's picture once, when it is first shown, and keeps it for the session; it is never saved.
- Marks: **[O]** the owner's, **[A]** the agent's, for the owner to revise.

## What a QSL card is

Amateurs confirm a contact by exchanging a card: the station's callsign large on the front, often a picture of its home; on the back the
contact's date and time in UTC, the frequency and mode, the signal report, the station's grid locator, a few words, and "73" (best
regards). The radio has given a card for each first contact with a settlement since 0.38.0, drawn plainly: the faction's colour and
icon, the place, the operator, the date, the report and the distance.

## A: what the card carries (Radio 0.58.0)

| On the card | Worked out from | Notes |
| --- | --- | --- |
| **The sender's callsign**, heading the card | The faction's two-letter prefix, a call-area digit from the station's latitude (0 far south to 9 far north), two or three letters from the settlement's ID | Prefix: the faction name's first letter, then a seeded letter; factions in load order step past a prefix already taken [A] |
| **Our station's callsign** ("To RV5KQX, operator Combarro") | The same scheme from the player's faction and the colony's tile | |
| **The Maidenhead grid locator** | The tile's latitude and longitude, by the published algorithm (six characters; Munich is JN58td) | Real, not a likeness |
| **The time in UTC** beside the local date | The contact's tick at longitude 0 | Operators log in UTC; the local date is the sender's |
| **The report** | As before: readability and strength (RS) from the link's margin | Morse's RST (a tone figure) is in the rule, unused until a card knows its mode |
| **A note**, then "73" | What the contact was: first contact, their help, our call, a radio friend, our help to them; three authored lines each | Agent-authored, in the language files, for the owner's review |

Callsign, locator and our call are worked out when the card is earned and kept on it, so a card stays whole when its settlement is
gone; a card from before 0.58.0 fills them in the first time it is drawn.

## B: how it looks (Radio 0.59.0)

As built: the rules in `QslArtRule` (game-free, tested), the painting in `QslCardArt`, the card in the station's Log tab. The front is
256 by 176 pixels (about 180 KB), painted the first time a card is shown; at most 48 are kept, the oldest let go and painted again when
next shown [A]. The ground's kind comes from the biome's name (desert, ice, swamp, forest; grass otherwise), so other mods' biomes
take the nearest look [A]. With Ideology, the stamp has the faction's primary ideology's symbol beside it.

- **A front**: the station's home as a layered picture: the sky by the hour of the contact, ridgelines by the tile's hills, the ground by
  its biome (trees, dunes, ice, grass), the sea if it is coastal; the callsign large across it and the faction's icon as a stamp.
- **A back**: what A carries, with the operator's signature, a pen squiggle seeded by the name.
- **Card stock by the faction's tech level**: hide for neolithic factions, parchment for medieval, printed card for industrial,
  dark glossy for spacer; a gilt border for a faction with royal titles (the Empire); with Ideology, its primary ideology's symbol.
- **A click turns the card over.**
- **Cost**: one picture a card, 256 by 176 pixels (about 180 KB), drawn when the card is first shown; ten cards are about 1.8 MB,
  and the 48 kept at most about 8.7 MB. Nothing is saved: after a load the same seed draws the same picture. The tab draws each card
  as one picture.

## The four questions

- **What it lets the player do**: collect cards worth looking at, each telling where a contact was and what it was for.
- **What it stops**: nothing.
- **How it changes play**: the logbook becomes a collection; contacts with far or rare places feel like trophies.
- **What outside notices**: nothing yet, a choice: the cards stay in the logbook. C (below) would bring them into the colony.

**Risk**: none to the player's things.

## Leftover work

- **C, cards as things in play**: cards arriving later by trader (the bureau's delay), a wall of cards as art and a mood, a ham shack
  room, more awards (every biome, grid squares, the far side of the planet with HF), rare stations, listener (SWL) cards.
- **D's rest**: built, the album in Radio 0.71.0 (sorted by date, faction or distance; not linked to the contact arcs) and the award
  certificates in 0.68.0 (as framed art, docs/development/radio-qsl-in-play.md).

## Callsigns from the world's own places (Radio 0.65.0)

The owner's words of 28 Nov 2026: "we need to implement callsigns into Rimworld where the prefixes are unique to the game and generated
at the start of the game with each roll of the world, themselves. I think having the prefix based on an areas unique name/geography
would be a great way to help the player associate themselves with a local area and make themselves feel more at home." Then three
cards, the chosen option first and marked:

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 1 | Where a prefix comes from | **The nearest named place** (the agent's lean): the game names each world's geography when it is rolled (mountain ranges, seas, lakes, bays); a station takes the prefix of the place its tile lies in, or the nearest | Our own named regions (by landmass and biome, named from the world's seed). Both: the place, then a letter for the faction |
| 2 | The callsigns' shape | **Three letters, the digit, letters** (the agent's lean): KSM4QX; Earth's amateur calls essentially never put three letters before the digit | Two letters, a mark, the digit, letters (KS·4QX). Two letters no Earth country holds |
| 3 | Where the place shows | **On the card and in the logbook** (the agent's lean) | Also as faint labels on the world map (a picture, off until picked). The callsign only |

**As built** [A]: `QslRule.PlacePrefix` takes the first letter of the name's distinctive words (the generic ones, "Ocean", "Mountains",
"Bay of", left out) and its next two consonants (vowels if it runs short, then letters from the seed); the world's places in the order the
game made them each step past a prefix already taken (the last letter moves on), so a world's prefixes are unique and the same each time it
loads. A tile's place is its own (`Tile.feature`) or the nearest by a search over its neighbours (at most 4,000 tiles, kept); a tile with
none near takes three letters from its own number. The card keeps its area (`PhobosQslArea`); a card from before fills its area and new
callsigns in the first time it is shown while its settlement stands (the faction-based prefix of 0.58.0 is retired). The owner's own
callsign setting still wins. Not tested in game (row 784).

## The band, mode and own callsign (Radio 0.62.0)

Built on the owner's choice of 28 Nov 2026, together with HF's first slice. A link to a settlement now carries the first hop's band and
mode (from our end) and the relays in its chain, worked out once a search (kept the rules' while, as the search is); the five places a
card is earned pass the link whole. The card keeps them (`PhobosQslBand`, `PhobosQslMode`, `PhobosQslRelays`); a mode Def's `keyed` gives
Morse its three-figure report. The colony's own callsign is a mod setting, cleaned to letters, digits and a stroke (at most ten), and
addresses every card, old ones too, while it is set [A: a setting for every game rather than one per colony, as the leftover asked].
