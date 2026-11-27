# QSL cards in play (28 Nov 2026)

**Status (28 Nov 2026):** designed on the owner's cards; nothing built yet. Builds on [QSL cards, worked out](radio-qsl-cards.md) (parts A
and B, Radio 0.58.0 and 0.59.0; band and mode in 0.62.0), its leftover group C.

## Read this first

- **Game systems first** (the repository's rule, vanilla precedence): a received card is a **book**, so colonists read it with the
  game's own reading job, a card wall is a **bookcase**, and an award's certificate is **art**, admired with the game's own art viewing.
- **Nothing is lost by the bureau**: the logbook shows a card at once, as today; only the card you can hold comes later.
- **Performance first** (the radio's rule): every cost is below, before it is built.
- Marks: **[O]** the owner's, **[A]** the agent's, for the owner to revise.

## The owner's answers (28 Nov 2026)

Each card as put, the chosen option first and marked.

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 1 | Which parts go into the first slice (several could be picked) | **All four**: cards by bureau, the QSL wall and ham shack, more awards, rare stations and listener cards; and the owner's words: "With things like a QSL wall, make it like how the bookshelf operates in my current game, where every stored record is a truly realized book (or in this case a QSL card) that pawns can interact with, read, etc. for mood benefits and so forth. The same can be applied to rewards but they are observed from a distance instead, I suppose? I'll leave that one up to you as well." | (The agent leaned to the first two) |
| 2 | Cards by bureau: the logbook at once? | **The logbook at once, the card later** (the agent's lean): the contact and its card in the logbook as today, marked awaited; the card item comes later with a trader | All by bureau (awards wait too). The bureau as a setting, off by default |
| 3 | How much the ham shack does | **A room role with a work bonus**: the ham shack's mood and beauty, and radio work a little faster in one | A small mood and beauty only (the agent's lean). Just the wall |

## The design [A]

### 1. Card items and the bureau

- **A QSL card item** for each card earned: a book (the game's `Book` class, as its novels and textbooks are), one card a thing, carrying
  which logbook card it is. Its picture is the card's own front (part B's painting), drawn when it is looked at; on the ground, a small
  card picture.
- **Reading a card** (the game's reading job, from a shelf, a wall or the floor): recreation, and a memory, "Read a QSL card from
  {place}": a small mood, more for a far contact or a rare station (authored figures).
- **The bureau**: a card earned is "awaited" in the logbook. When the next friendly trader caravan or visitor group reaches a home map,
  it brings every awaited card (the bureau posts everyone's cards together), dropped beside it, with a message ("Post from the QSL
  bureau: three cards"). Orbital traders bring none (agent's choice).

### 2. The QSL wall, certificates and the ham shack

- **The QSL wall**: a wall-mounted frame that holds cards as a bookcase holds books, shows the fronts of the first few, and adds beauty
  for each card on it (authored, capped). Colonists take a card down to read it, as from a bookcase.
- **Award certificates**: each award also brings a framed **certificate**, drawn as the cards are (part D's idea), an art piece
  colonists admire from a distance with the game's art viewing (its beauty authored), placeable on a wall.
- **The ham shack**: a room with a radio station and a QSL wall takes the room role "ham shack". Radio operators get a small mood from
  a good one (by its impressiveness, as the game's rooms do), and radio work done in it (advice, faction calls, lessons) goes a little
  faster (authored, about 10%).

### 3. More awards

Every biome worked; 10 and 25 grid squares; a contact of 10,000 km or more (HF's scale); every landmass of the planet (found once a
session by a fill over the world's tiles). Each with its letter and its place in the awards' mood, as today.

### 4. Rare stations and listener cards

- **A rare station**: now and then (authored, about once a season), a station comes on the air for a few hours from somewhere unusual
  (an expedition on the ice, a lone outpost, a ship at sea), shown on the world map. Working it needs an HF band open to it; its card is
  marked rare, reads for more mood, and counts toward an award.
- **Listener cards (SWL)**: a settlement heard but not worked (its signal reaches the colony, the colony's does not reach it: the link
  budget's one direction), noted once; a listener's card is posted back by bureau, marked as heard only.

## Cost [A]

| Piece | What it costs, and when |
| --- | --- |
| Card items | One item a card earned; reading uses the game's job. A card's front picture is painted when looked at, kept as part B's are (48 at most) |
| The bureau | A look every game hour (2,500 ticks) for a friendly group newly arrived on a home map; nothing per tick |
| The wall | Its cards drawn with the map's mesh; repainted when a card is added or taken |
| The ham shack | The room role is scored when the game scores rooms; the work bonus is one look at the room when a radio job starts |
| Awards | Counted when a card is earned, as today; the landmass fill once a session |
| Rare stations | One roll a day; a world object while on the air |
| Listener cards | Noted while the existing searches run; nothing more |

## Sources

- Our records: [QSL cards, worked out](radio-qsl-cards.md), [HF](radio-hf.md) (for far contacts and rare stations).
