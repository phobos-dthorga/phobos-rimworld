# The smoke discharger bank: run and review (26 Nov 2026)

**What was bought.** Three images from fal.ai (`fal-ai/nano-banana-pro/edit`, US$0.15 each) of one bank of four smoke grenade tubes,
drawn alone on white, on the owner's go of 26 Nov 2026 ("Let's do the smoke dischargers, ammunition locker, and extra gun mounts").
Dry runs first; caps US$0.35 and then US$0.16. Spent: about **US$0.45**. Request: [smoke-dischargers.json](smoke-dischargers.json);
design: [the militarised service trucks](../../../docs/development/vehicle-militarised-service-vehicles.md), "Smoke, the locker and
extra mounts, built".

**References, our own.** The Talus tipper's picked body, and for tries 1 and 2 the picked remote weapon station. No game or other
author's art was uploaded.

**What went wrong once.** With the station as a style reference, try 1 redrew the station with tubes on it, as the gun ring set's
tries did on 25 Nov 2026. Try 3 was drawn with the truck alone and a prompt naming what not to draw, the change that worked then; try 1
was not offered.

**Near-copy check** (`scripts/art/near_copy.py`, against the game's and our house datasets): no flags, no likeness to our own art.

**Decision (owner, 26 Nov 2026, in chat: "Try #3"):** try 3, chosen against try 2 on overlay cards of all five trucks
(`review_cards.py overlays`). Exported by `assets/phobos-vehicles/export_smoke_dischargers.py` (D'thorga Vehicles 0.125.0): the sprite
exporter's cut-out, the bank cropped to itself, 0.55 of a cell wide on a 0.75-cell canvas, laid twice on each truck by its node at the
cab's front corners turned 30 degrees out (agent placement read off each armoured cab's outline, to revise once seen in game); the
node's icon fitted from it. The pick is on `art-masters`, tries 1 and 2 on `art-references`.
