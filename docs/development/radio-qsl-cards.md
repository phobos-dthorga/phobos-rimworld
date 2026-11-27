# QSL cards, worked out (28 Nov 2026)

**Status (28 Nov 2026):** part A (what the card carries) is built in Radio 0.58.0 and part B (how it looks) in 0.59.0. Neither is
tested in game (test rows 749 and 750). The owner asked on 28 Nov 2026 where procedural design could go with QSL cards, and chose
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
- **Cost**: one picture a card, 256 by 128 pixels (128 KB), drawn when the card is first shown; ten cards are about 1.3 MB. Nothing is
  saved: after a load the same seed draws the same picture. The tab draws each card as one picture.

## The four questions

- **What it lets the player do**: collect cards worth looking at, each telling where a contact was and what it was for.
- **What it stops**: nothing.
- **How it changes play**: the logbook becomes a collection; contacts with far or rare places feel like trophies.
- **What outside notices**: nothing yet, a choice: the cards stay in the logbook. C (below) would bring them into the colony.

**Risk**: none to the player's things.

## Leftover work

- **The band and mode on the card**: a link records only its margin, not the band or mode it was worked on; the five places a card is
  earned would need to pass them. Then RST for Morse contacts, and "via" a relay.
- **C, cards as things in play**: cards arriving later by trader (the bureau's delay), a wall of cards as art and a mood, a ham shack
  room, more awards (every biome, grid squares, the far side of the planet with HF), rare stations, listener (SWL) cards.
- **D's rest**: an album view (sort by faction, distance, date; linked to the contact arcs), award certificates drawn in the same
  way as the cards.
- **A callsign of the owner's own choosing** for the colony, as a setting.
