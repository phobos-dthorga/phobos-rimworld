# The gun ring set: run and review (25 Nov 2026)

**What was bought.** Thirteen images from fal.ai (`fal-ai/nano-banana-pro/edit`, US$0.15 each) for five parts: the gun ring, a 7.62 mm
machine gun, a .50 heavy machine gun, a 40 mm grenade launcher and a gun shield, on the owner's card "Gun ring first", with a US$1.55
cap and then a US$0.50 cap. Dry runs first. Spent: about **US$1.95** (US$1.50, then US$0.45). Request: [gun-ring-set.json](gun-ring-set.json);
design: [the militarised service trucks](../../../docs/development/vehicle-militarised-service-vehicles.md), "The gun ring and its guns" (MV8).

**References, our own.** The Talus tipper's picked body, and for the first ten the picked remote weapon station. No game or other
author's art was uploaded.

**What went wrong once.** With the station as a style reference, try 1 of the .50, the grenade launcher and the shield redrew the station
instead of the part. The owner chose to redraw those three with the truck alone and a prompt naming what not to draw (try 3 of each);
fal.ai's balance ran out on the first attempt, and the owner topped it up. The first tries are kept on `art-references` as reference.

**Near-copy check** (`scripts/art/near_copy.py`): one flag, the ring's try 1 against the game's "deactivate mechanoid" icon by picture
hash (distance 11) with a structure score of 0.15: both are round, and side by side they are plainly unlike. By the rule it stays off
the public branches; it was not picked. Nothing else flagged, and no likeness to our own art.

**The ring's white.** The owner noted white left in the middle of the ring's try 2 on its card. The quarters a ring encloses are now
named in the request (`holes`) and cleared by the card tool and the exporter; the card was drawn again.

**Decision (owner, 25 Nov 2026, on D'thorga Companion's Picks page and by question cards in chat):** ring try 2, 7.62 mm try 1, .50
try 2, 40 mm try 3, shield try 2. Exported by `assets/phobos-vehicles/export_gun_ring.py` (D'thorga Vehicles 0.121.0): the sprite
exporter's cut-out, each picture whole about the pivot it was drawn on, the guns at two cells and the ring and shield at one and a half
(laid on the four trucks by the agent: at two cells the ring filled the roof and the shield stood wider than the cab). Agent placement
and scale, to revise once seen in game. The picks go to `art-masters`, the rest to `art-references`, the ring's try 1 kept local.
