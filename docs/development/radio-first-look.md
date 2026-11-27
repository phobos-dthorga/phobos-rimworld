# Radio: what to look at first (27 Nov 2026)

**Status (27 Nov 2026):** a test guide for the owner, not a design. The radio's rows 699 to 731 of the
[in-game test pass](in-game-test-pass.md) are all untested; this page puts the ones worth a first look in the order you would meet
them in play. The test pass keeps the full steps; this page says where to start and what would be wrong.

## Read this first

- **Five checks first**, then the rest when they come up in play. Each is a glance, not a session.
- **Save first**, and try the riskier ones (raids, ambushes) on a copy.
- **Turn on the seen-in-play setting** (D'thorga Framework's settings), so each feature notes itself when it runs; `python
  scripts/test-pass.py report` then lists what was seen.
- **After a session, run `python scripts/triage-log.py`** on the log: an error from any of these shows there first.
- The jamming session's radio rows (706 to 711) and the other sessions' rows in this range are not on this page.

## The five first

| # | Row | Where | Look for | Wrong if |
| --- | --- | --- | --- | --- |
| 1 | **720, a seat at the radio** | Your existing radio desk | Its pane: does it say "Nobody can sit at it"? If so, clear the spot in front of the microphone edge, or move it. Put a chair there; watch someone use the radio | A colonist stands inside the desk, or walks to it and gives up |
| 2 | **725, calling a faction** | Select a colonist, right-click the station | A "Call ... on the radio" line for each faction in reach; the negotiation window opens with the line's quality | No line though the logbook lists a settlement in reach; an error when the window opens |
| 3 | **704, the map's coverage carries on** | Select the station, then something else, then the station again | The percentage carries on, or the picture is done | It starts again from 0% |
| 4 | **728, an ally warns of a raid** | Wait for an ordinary raid, with an ally's settlement in reach | A letter from the ally, then the raid about three hours later | The raid comes at once with no letter, or twice, or never |
| 5 | **699, high ground blocks the signal** | A convoy with hills between it and home | Its line names the ground in the way when it is out of reach | Every convoy that reached home before is suddenly out of reach |

## Then, as they come up

**At home, the desk**

| Row | One line |
| --- | --- |
| 705 | Pressing Fit in a station's Upgrades: a tick, the card lights up as ordered |
| 721 | A call at the desk takes a channel; with none free, the desk is passed over |
| 722 | A teacher sits beside a child at the terminal (Biotech) |
| 730 | A child with no comms console radio-talks at our station (Biotech) |

**The world map's pictures**

| Row | One line |
| --- | --- |
| 700 | Radio coverage for one place: pick a far-end radio from the button |
| 701 | Home, through relays: the whole network |
| 713 | The planner: where a new relay mast or post would reach home |
| 714 | A full relay is pink |
| 729 | Contact arcs to each settlement in reach, when the colony is selected |

**Convoys and caravans**

| Row | One line |
| --- | --- |
| 702 | A caravan on foot reaches home by a hand-held, worn or packed |
| 712 | A convoy's Radio button shows the path home from the side |

**Calling factions, the rest**

| Row | One line |
| --- | --- |
| 726 | On a marginal line, now and then a request is misheard |
| 727 | A hostile listener ambushes a trader you asked for (on a copy of the save) |
| 731 | An allied supply drop asked for on the radio call |

## What would matter most if it went wrong

- **A raid held twice or lost** (728): the warning queues the raid in the game's own incident queue; a raid that never comes, or comes
  twice, is worth a log and a save at once. Switch it off with **Allies warn of raids by radio** meanwhile.
- **A station nobody can use** (720): a station built before 0.51.0 may have something standing on its new seat; its pane says so.
- **Frame time on the world map** (700, 701, 713): the pictures start off; if one makes the world map stutter, say so and it can be
  measured.
