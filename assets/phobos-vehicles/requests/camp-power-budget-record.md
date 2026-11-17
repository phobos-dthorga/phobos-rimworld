# Vehicle accessories, the power budget: run and review (17 Nov 2026)

**What was bought.** Five pictures for D'thorga Framework 0.129.0's power budget batch ([vehicle accessories](../../../docs/development/vehicle-accessories.md)),
two attempts each, from the OpenAI image API (gpt-image-2, edits endpoint, medium quality, 1024 x 1024), at the owner's go with a US$1.10 cap
(17 Nov 2026). Dry run first: at most US$1.01. Spent: about **US$0.545** (US$0.054 an image, from the returned token counts). Request:
[camp-power-budget.json](camp-power-budget.json).

**References, our own only.** The three kits used our approved jump-start kit item; the two buttons used our approved night heater switch
button. Each for style only. No game or other author's art was uploaded.

**Review (agent, on dark ground at 128 px and at 64 px).** All ten were clean, straight on, empty of text and marks, and in the family's
style. The agent suggested fridge kit 1, battery 1, inverter 1, fridge button 2 and outlet button 1.

**Decision (owner, 17 Nov 2026):** fridge kit **2**, house battery **1**, power inverter **1**, fridge button **2**, outlet button **1**.
Exported with `scripts/art/export_sprite.py --size 128 --aspect 1:1` after the background AI had looked at each master (on the processor);
the fridge button's pale interior came through whole. The picks are on `art-masters` (`vehicles/accessories/`), the other attempts on
`art-references`.
