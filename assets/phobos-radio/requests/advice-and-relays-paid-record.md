# Advice and relay pictures: paid request record

**Decision (owner, 27 Nov 2026, in the conversation):** the numbered medical chest, attempt **2**; the repeater kit icon,
attempt **1**; the relay mast, attempt **2**. Picked from the review cards drawn by `scripts/art/review_cards.py sprites` at
128 px, offered on the Companion's Picks page too.

- **Request:** `advice-and-relays-paid.json`, three pictures, OpenAI gpt-image-2 at medium quality, two attempts each (the chest
  and the kit icon at 1024 x 1024, the mast portrait at 1024 x 1536).
- **Why paid:** the chest is an item in the game world, under `assets/AGENTS.md`; the owner asked for the kit and mast pictures
  to be paid as well (26 Nov 2026: the local model was not up to the stereo kit).
- **Cap:** US$0.90, the owner's card of 26 Nov 2026 ("Buy all three, cap US$0.90"), after a dry run. **Spent:** about US$0.32.
- **References:** our own approved radio and medical art (the hand-held, the linear amplifier, the mast and whip antennas, the
  fluids kit), style only. Nothing else was uploaded.
- **Shipped by** `assets/phobos-radio/ship_advice_art.py` at 128 px, square, after the background AI had looked at every try;
  0 loose specks or fringe on each.

| Picture | Attempt | Review at native and actual size | Outcome |
| --- | --- | --- | --- |
| Medical chest | 1 | One large body chart on the lid, shallower box | Not picked. Archived on `art-references` |
| Medical chest | 2 | Two body figures on the lid chart, deeper box, coloured dots on each compartment | **Picked** |
| Repeater kit icon | 1 | Straight-on radio box with green and amber lights, six-can duplexer, whip in front | **Picked** |
| Repeater kit icon | 2 | The same parts turned slightly | Not picked. Archived on `art-references` |
| Relay mast | 1 | Guyed lattice mast, solar panel, radio box at its foot | Not picked. Archived on `art-references` |
| Relay mast | 2 | Guyed lattice mast, solar panel, a larger base plate | **Picked** |

Masters of the picks are on `art-masters` (a62fe8c), the other tries on `art-references` (0790b65).
