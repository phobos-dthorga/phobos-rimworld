# The remote weapon station: run and review (25 Nov 2026)

**What was bought.** Two images, two tries of one turret, from fal.ai (`fal-ai/nano-banana-pro/edit`, US$0.15 each), on the owner's
"Let's work on our own remote weapon station :)", with a US$0.35 cap. Dry run first. Spent: about **US$0.30**. Request:
[remote-weapon-station-fal.json](remote-weapon-station-fal.json); design: [the militarised service trucks](../../../docs/development/vehicle-militarised-service-vehicles.md),
"The gun ring and its guns" (MV8), and [autonomous vehicles by radio](../../../docs/development/autonomous-vehicles.md), step 6.

**OpenAI first, refused.** The same request to the OpenAI image API ([remote-weapon-station.json](remote-weapon-station.json)) was
refused by its moderation as a weapon, before any image was made; nothing was spent. The owner chose fal by card ("fal, as the Osprey's
guns"), the service the Osprey's ramp guns were drawn with.

**Reference, our own.** The Talus tipper's picked body (`tipper-body-north-3.png`), for style and scale only. No game or other
author's art was uploaded.

**The idea [A].** Seen from straight above, barrel up, its turning centre at the canvas centre: a low round turret ring, a squat
cradle with a .50 heavy machine gun, a sight box with a camera lens on its left and an ammunition box with a short belt feed on its
right, olive drab and gunmetal, our trucks' outline and shading.

**Review (agent, each at full size and on the card at 256 game pixels, on soil and stone):** both drawn alone on white as asked, plan
view, no truck, text or damage; no loose specks after export.

- **Try 1:** an open olive cradle on a ring with a bright steel rim; the sight box's lens faces forward and stands out past the ring; a
  long ammunition box with a clear belt feed. Reads clearly at actual size.
- **Try 2:** a darker, heavier drum with spokes and an armoured hood over the gun; the lens at the sight box's lower corner. Goes
  muddy at actual size.

**Near-copy check** (`scripts/art/near_copy.py`, 25 Nov 2026): 2 outputs compared, no flags, no likeness to our own art.

**Card:** `review_cards.py sprites ... --size 256 --aspect 1:1`, offered on D'thorga Companion's Picks page.

**Decision (owner, 25 Nov 2026, on the Picks page):** try 1. Exported by `assets/phobos-vehicles/export_remote_weapon_station.py`
(D'thorga Vehicles 0.119.0): the sprite exporter's cut-out (the background AI's mask), framed on the turret ring's centre rather than
the drawing's bounds, because the long barrel had drawn the ring below the middle and Vehicle Framework turns a turret about its
picture's centre. The ring is 1.15 cells across and the barrel reaches 1.33 cells forward, on a 3-cell square; the node's icon is
fitted from the same picture until a drawn icon is picked. Placed on the Talus and the Busbar left of the remote kit's antenna,
checked by the agent tinted at game pixels, facing forward and turned 60 degrees; agent placement, to revise once seen in game.
Try 1 goes to `art-masters` and try 2 to `art-references`.
