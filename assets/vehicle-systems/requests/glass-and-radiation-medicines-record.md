# Glass chain items and radiation medicines: run and review (22 Nov 2026)

**What was bought.** Twenty-four pictures from the OpenAI image API (gpt-image-2, edits endpoint, medium quality, 1024 x 1024), at
the owner's word (22 Nov 2026: what appears in the game world is made paid; "Go ahead with the paid art run"). Dry runs first.

| Batch | Request | Pictures | Cap | Spent (from returned token counts) | Run report |
| --- | --- | --- | --- | --- | --- |
| Radiation medicines | [radiation-medicines.json](radiation-medicines.json) | 6 | US$0.90 | about US$0.33 | `generated/run-20261122-0847.md` |
| Glass chain items | [glass-chain-items.json](glass-chain-items.json) | 16 | US$2.40 | about US$0.88 | `generated/run-20261122-0850.md` |
| Laminated pane, redrawn whole | [laminated-pane-retry.json](laminated-pane-retry.json) | 2 | US$0.30 | about US$0.11 | the same folder |

**In all about US$1.32.** The items are for [vehicle glass](../../../docs/development/archive/vehicle-glass.md) (the glass chain) and the
[radiation model](../../../docs/development/radiation-model.md) (RM5's three medicines). Nothing is shipped yet: each waits for its
build.

**References, our own only, for style.** The glass items: the approved tyre set and electrical kit icons. The medicines: the
bronchodilator vial and burn dressing. Each with the role "match its outline weight, shading, palette and finish only; do not copy its
shape". No game or other author's art was uploaded, so the near-copy check had no source to compare against.

**Review (agent).** The background AI looked at every original (on the processor, beside the game); each attempt was cut out with
`scripts/art/export_sprite.py` at 128 px, as shipped, and shown enlarged three times without smoothing and at actual size on
soil-coloured ground. No white fringe, holes or loose specks in any. The armoured pane's bright back edge is drawn on purpose, with
its own outline (a clear inside layer, as real armoured glass has). **The laminated pane's tries 1 and 2 each showed a star crack**,
which the agent's prompt had asked for (laminated glass cracks but holds); the owner asked for it redrawn whole, the one correction,
and tries 3 and 4 came back whole. Their card was drawn by hand from the real export, because `scripts/art/review_cards.py` read tries
1 and 2 for a request numbered 3 and 4 (reported to the session that owns it, which fixed it).

**Decision (owner, 22 Nov 2026, one card each):**

| Item | Pick | The agent's suggestion |
| --- | --- | --- |
| Anti-sickness tablets (antiemetic) | **2** | 2 |
| Marrow stimulant | **2** | 2 |
| Prussian blue capsules | **2** | 2 |
| Glass sand | **2** | 1 |
| Potash | **2** | 2 |
| Soda ash | **2** | 1 |
| Glass pane | **1** | 1 |
| Laminated pane | **3** (redrawn whole) | 3 |
| Armoured glass pane | **2** | 2 |
| Vision block | **2** | 2 |
| Plastic film | **2** | 2 |

**Kept.** The eleven picks, with their records, on `art-masters` (`vehicles/glass/`, `medical/radiation/`); the thirteen unpicked
tries, with theirs, on `art-references`, the reason being the owner chose the other try (and, for laminated pane 1 and 2, the crack).
Nothing was left out as fugly.
