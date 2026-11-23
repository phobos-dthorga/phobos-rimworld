# Radio paid batch 1: the radio desk, the radio terminal, the amplifier, the whip and the mast (24 Nov 2026)

**Status (24 Nov 2026):** bought, ten pictures for about US$0.50 of the owner's US$1.00 cap, and picked. The desk, whip and mast
are exported and ship in Radio 0.4.0; the terminal, both amplifiers and the second whip in Radio 0.5.0. All ten are applied. [The run, as it went](#the-run-24-nov-2026) is below the dry run.

## The owner's direction

- 24 Nov 2026, asked for thoughts on redoing the radio's art "especially under the new rules": the agent proposed redoing the base
  station, the whip (a stand-in) and the mast on the paid route, the coax icon on the local night run, and keeping the coax atlases,
  the signal icon and the hand-held. The owner: "The ones you recommend keeping are genuinely good, yes, so I agree with you there."
- By card, the same day: the base station's two tries are "One of each, they could each have wildly different stats to each of
  them :)", so a radio desk and a radio terminal, each its own station ([radio stations](../../../docs/development/radio-stations.md));
  the hand-held is left out; go for the dry run.
- The rule this batch follows: what appears in the game world is made on the paid route, two tries each, the owner picking one card
  each (`assets/AGENTS.md`), under [paid image generation rules](../../../docs/development/paid-image-generation-rules.md).

## The requests ([paid-1.json](paid-1.json))

| Request | Ships as | In game | Asked for | Camera |
| --- | --- | --- | --- | --- |
| `radio-desk` | `Things/BaseStation.png` | 2 × 2, 256 × 256 px | 1024 × 1024 | Straight overhead, as our stations are |
| `radio-terminal` | `Things/RadioTerminal.png` (planned) | 3 × 2, 384 × 256 px | 1536 × 1024 | Straight overhead |
| `whip-ground-plane` | `Things/Whip.png` | drawSize (1, 1.5), 128 × 192 px | 1024 × 1536 | Front view, standing upright **[A]** |
| `mast-collinear` | `Things/Mast.png` | drawSize (1, 2.5), 128 × 320 px | 768 × 1920 | Front view, standing upright **[A]** |

- **Every size is at least 1.5 times the drawn size** (the rule of 9 Nov 2026); in fact four to eight times.
- **One reference, our own art**: the approved vehicle parts bench (`PartsBench.png`), for its outline weight, shading, palette
  and finish only, its role named in the request and repeated in the prompt.
- **The camera for the antennas is the agent's choice [A]**: our paid buildings so far are drawn straight from above, but a tall thin
  antenna seen from above is a dot, and the game draws trees and lamps standing up. The owner may prefer otherwise.
- **Physically right [A]**: the whip is a dual-band ground-plane (a short upright rod, about a quarter wave at VHF, with four drooping
  radials on a 3 m pole); the mast carries a white fibreglass collinear, for its 3 dBi, rather than a broadcast tower's tip.
- **Nothing that will be an overlay is painted**: the desk's meter has no needle and its lamp is unlit (RV6), the terminal's monitor
  has no picture (RV3), the mast has no warning light (RV7).

## Dry run (24 Nov 2026)

```text
8 images asked for, 8 still to make
  radio-desk: openai, medium quality, up to US$0.101 an image, 2 seeds, 1 references, 1024x1024
  radio-terminal: openai, medium quality, up to US$0.101 an image, 2 seeds, 1 references, 1536x1024
  whip-ground-plane: openai, medium quality, up to US$0.101 an image, 2 seeds, 1 references, 1024x1536
  mast-collinear: openai, medium quality, up to US$0.101 an image, 2 seeds, 1 references, 768x1920
paid images still to make should cost at most about US$0.81
```

The figure is the engine's ceiling, not a price: the real cost is worked out from the tokens OpenAI returns. The real run would use
`--max-usd 1.00`.

## After the run

Review each try at native size and half size, the background AI before export, the review cards (`review_cards.py`) on the Picks
page, the edge check; then the owner's picks, the unpicked tries to `art-references` and the picks' masters to `art-masters`.

## The run (24 Nov 2026)

**The owner's go, by card:** "Go, cap US$1.00". The same cards changed the batch:

- **The antennas' camera waited for the owner's screenshot of the trees**, which showed they are "not quite upright at all": the game
  draws standing things steeply from above and a little in front, the height shortened to a stub. The whip and mast moved to
  [paid-2-antennas.json](paid-2-antennas.json) with that camera ("STEEP HIGH-ANGLE VIEW FROM ABOVE AND SLIGHTLY IN FRONT, exactly as
  the game RimWorld draws its trees and standing lamps"), both at 1024 x 1536.
- **The amplifier became its own building on the coax** (the owner's card), so the terminal's prompt lost its amplifier cabinet and a
  `linear-amplifier` request (1024 x 1024, overhead) joined [paid-1.json](paid-1.json). The agent swapped it in within the same cap,
  having told the owner a separate amplifier meant another picture.

| Run | Pictures | Spent | Cap given |
| --- | --- | --- | --- |
| `paid-1.json`: desk, terminal, amplifier | 6 | about US$0.321 (US$0.057, 0.057, 0.046, 0.046, 0.057, 0.057) | US$0.62 |
| `paid-2-antennas.json`: whip, mast | 4 | about US$0.183 (US$0.046 each) | US$0.41 |
| **Together** | **10** | **about US$0.504** | the owner's US$1.00 |

Costs are the engine's, from OpenAI's returned token counts. Every size came back as asked. The background AI looked at all ten
before the cards were drawn; every card shows 0 loose specks.

**The agent's review at native and three times size (for the owner to overrule):**

| Picture | Try 1 | Try 2 | Agent's suggestion |
| --- | --- | --- | --- |
| Radio desk | Clean layout; transceiver with a small display panel, mic, logbook, unlit meter | Fuller transceiver face with knobs; meter with three buttons; a lip on the front edge | Either |
| Radio terminal | **The radio box has a stub antenna**, which the design says the terminal has none of | No antenna; radio box with two cables; monitor, keyboard, tower, headset | **2** |
| Linear amplifier | Fan grille, two handles, unlit meter, cable in at the top and out at the bottom | The same, bolder | Either |
| Whip | X of radials; the radiator much shorter than the radials | **Radiator about as long as the radials**, as a quarter-wave ground-plane's are | **2** |
| Mast | Lattice, white collinear, three guy anchors in a Y | The same, cleaner lattice, cable inside it | Either |

**For the export:** the mast's guy anchors spread well beyond one cell, so its drawn size (today 1 x 2.5) will need widening, to be
judged on the map at export. Cards: `K:\phobos-rimworld\art\cards\paid-1\` and `paid-2-antennas\`.

## The owner's picks (24 Nov 2026, on the Picks page)

| Picture | Pick | The owner's words | What happens |
| --- | --- | --- | --- |
| Radio desk | **2** | | Exported as `BaseStation.png`, Radio 0.4.0 |
| Radio terminal | **1** (the agent had suggested 2: try 1's radio box has a stub antenna) | | Waits for the terminal; whether it keeps an antenna is asked |
| Linear amplifier | **1** | "Offer both as alternate models with varying specs to each ^ ^" | Two amplifier models, each its own figures; both wait for the amplifier |
| Whip | **1** | "Both with varying specs, just like real life! ^ ^" | Try 1 exported as `Whip.png`; try 2 becomes a second ground-plane with its own figures |
| Mast | **1** | | Exported as `Mast.png`, Radio 0.4.0 |

**A third comment was lost** by the Companion's Picks page before it reached the answers file (the owner: "there were three comments
in total"): its keyboard pick sent no words, and moving between picks emptied the box. Fixed in Companion 0.76.1. The lost words are
asked again in chat.

**Exported (agent's sizes, for the owner to revise):** the whip at drawSize (1.5, 2.25), offset (0, 0, 0.49); the mast at (3, 4.5),
offset (0, 0, 1.07): each footing, measured at about 28% and 26% up its picture, sits on its one cell. Masters to `art-masters`
(23beffb), the unpicked desk 1, mast 2 and terminal 2 to `art-references` (714a5de); whip 2 and amplifier 2 are with the masters, as
the owner keeps both.
