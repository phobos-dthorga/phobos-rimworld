# The station logbook, QSL cards, a picture for the doctor and the Morse key

**Status (27 Nov 2026):** partly built. The logbook, QSL cards and awards are built in Radio Framework 0.38.0 (with D'thorga Framework
0.348.0 for the medevac's lines), not yet tested in game: rows 681 and 682. The picture for the doctor is built in 0.39.0 (row 683; its kit's
picture the owner's pick of two paid tries, US$0.11). Being built next: the Morse key (0.40.0, with a later D'thorga Framework for the
vehicle's key).

The owner chose all four on 27 Nov 2026, after the radio friends, School of the Air, neighbours' calls and the medevac
([radio friends, school, medevac](radio-friends-school-medevac.md)). Seven radio features were then built and untested; the logbook makes
them visible in one place, which suits the owner's way of testing what is seen in play. QSL cards and awards were in the owner's October
radio design ([radio framework design](radio-framework-design.md), Phase 4: "QSL cards, awards"); slow-scan television (SSTV) is a mode the
owner ran on VHF; and a Morse key honours the owner's bug.

Every figure marked [A] is an agent default, authored, for the owner to revise; [O] marks the owner's choices.

## The owner's cards (27 Nov 2026)

| # | Question | Chosen | Not chosen |
| --- | --- | --- | --- |
| 1 | Which ideas to build next (several) | **All four: the station logbook (the agent's lean), QSL cards, a picture for the doctor, a Morse key** | None left out |
| 2 | What form a QSL card takes | **Pictures in the logbook, "and perhaps these can be made procedurally to some extent?"** (the agent's lean, with the owner's addition) | Real cards to hang (paid art). Both |
| 3 | What an award gives | **A lasting colony mood** (the agent's lean) | Goodwill with everyone worked. The certificate only |
| 4 | How much the picture adds to advice | **A sixth, on a clean link** (the agent's lean) | A sixth, on any working link. A third, on a clean link |
| 5 | What Morse carries | **In touch, and advice at a quarter** (the agent's lean) | In touch only. Everything, slower |

## The station logbook

A **Log** tab on the radio station and the radio terminal, as every operator keeps one.

- **What it shows** [A]: the colony's radio friends (who, of which faction, how close, how many hours); the settlements in reach now, by
  faction (worked out when the tab opens, kept an hour); the QSL cards and awards; and the log itself, newest first: each advice call
  (who, whom, what, at what level), a faction's helper and its terms, a friendship made or lost, a radio lesson, a neighbour's call, and a
  medevac called, taken and home.
- **How much it keeps** [A]: the last 200 entries, saved with the game.
- **The four questions.** It lets the player see what the radio has done and why a call did not happen (nobody in reach, an ally already
  asked today); it stops nothing; it changes play by making the radio's quiet work visible; what outside notices is nothing new: it is a
  record, stated as a choice.
- **Risk: none.** **Cost** [A]: an entry is added when a call happens, a list append; the tab costs nothing until it is open, and then
  draws only what fits; the settlements in reach are worked out once an hour while it is open.

## QSL cards

**The first time the colony works a settlement by radio, it earns that settlement's QSL card**, as operators send cards to confirm a
contact. Working means a call with one of its people: a faction's helper, a radio friend's chat, a neighbour's call.

- **Drawn in code** [O, "made procedurally"]: no art. A card is a small rectangle in the faction's own colour, its icon, the settlement's
  name, the colonist who made the contact, the date, the band, the signal report and the distance, with the operators' customary
  "thanks for the contact". The signal report is the readability-and-strength pair operators send (RS, from 31 to 59), worked out from
  the link's margin [A].
- **Awards** [O, "A lasting colony mood"]: each award shows as a certificate in the logbook and gives colonists who work the radio a
  lasting +2 mood, "our station is known far and wide", up to +6 for three awards or more [A]. **Who works the radio** [A]: a colonist
  wearing a hand-held, or with a radio friend. The awards [A]:
  - **Worked three factions**: cards from three different factions.
  - **Worked all**: cards from every faction on the planet that has radio and is not hidden.
  - **A long way**: a contact with a settlement 50 tiles or more away.
  - **Ten settlements**: ten cards.
- **The four questions.** It lets the player collect proof of the radio's reach; it stops nothing; it changes play by rewarding masts,
  relays and good relations that reach further; what outside notices is the colonists who work the radio (their mood).
- **Risk: none.** **Cost** [A]: a card is checked for when a call with a settlement happens (a dictionary lookup); the awards are counted
  when a card is earned, and the mood's worker reads a count.

## A picture for the doctor (SSTV)

**A slow-scan camera kit at hand lets the adviser see the wound** (or the engine).

- **The kit** [A]: an item, made at the machining table after local radio: a small camera and a scan converter. In the world, so its
  picture is paid, on the owner's go.
- **What it adds** [O, "A sixth, on a clean link"]: with the kit at hand (as the medical chest is: carried, in the convoy, or near) and
  the call's link **clean**, advice closes a sixth more of the skill gap, on top of the chest. A worse link sends no usable picture.
  Real basis: SSTV sends a still picture in about a minute through a voice channel, and noise ruins it well before speech is lost.
- **Which advice** [A]: tending, surgery and vehicle repairs; not the trade talks.
- **Risk: none.** **Cost** [A]: one more look for an item near the carer when an offer is made, as for the chest.

## The Morse key

**A narrow CW signal gets through where voice cannot.** Morse needs about 500 Hz where FM voice needs 12.5 kHz, so about 14 dB less noise
under it, and a trained ear copies it a few decibels above the noise where voice wants 12 [A, approximate]: about 23 dB in all, three to
four times the reach between tiles where the ground's curve and plane-earth fading rule.

- **The keys** [A]: a **Morse key** upgrade on the base station's desk, and a **Morse key** fitting for a vehicle's radio. Both ends need
  one; hand-helds are FM only and never send Morse. No relays carry Morse [A]: they are voice repeaters.
- **What it carries** [O, "In touch, and advice at a quarter"]: a convoy reached only by Morse counts as in touch with home (its mood, its
  reach home), and advice by radio closes a quarter of the gap, as on a marginal link. No radio chats, lessons or neighbours' calls.
- **Who keys it** [A]: anyone; Morse is slow, and advice by Morse slows the work as advice does.
- **The four questions.** It lets a far convoy stay in touch beyond voice range; it stops nothing; it changes play by giving a station's
  key and a vehicle's key a reason to be fitted together; what outside notices is the convoy's in-touch mood and the advice it can get.
- **Risk: none.** **Cost** [A]: one more link budget, a direct one, only when a convoy's voice search finds no way home and both ends have
  a key, kept as the convoy's reach is kept.
