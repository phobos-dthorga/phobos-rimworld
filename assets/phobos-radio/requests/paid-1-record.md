# Radio paid batch 1: the radio desk, the radio terminal, the whip and the mast (24 Nov 2026)

**Status (24 Nov 2026):** dry run only. Nothing has been generated or bought. The real run waits for the owner's go.

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
