# The trucks' cab armour kits by paid editing (19 Nov 2026)

**Status (19 Nov 2026):** run at 15:31, one try each under the owner's US$1.50 cap: 10 pictures, every one keeping its cab.
Nothing is picked or shipped.

## The owner's direction

- 19 Nov 2026, question card on the cab pictures after night 7: **"Straight to paid editing."**
- Earlier the same day: the armoured cab, heavy cab plates, body plates and ram bar are drawn kits, "code first, they wait for
  pictures" ([the record](../../../docs/development/vehicle-militarised-service-vehicles.md), "The kits").

## Why paid

Night 7 tried the cab kits on the local model (FLUX.2 Klein 4B base) as edits of each picked body's cab: 54 tries, about 75 seconds
each. **The model redraws the cab rather than editing it**: shapes drift, some turn end to end, and both ram bars landed on the roof.
At most two of the 54 kept the cab's layout ([art backlog](../../../docs/development/art-backlog.md), night 7). An overlay has to sit
exactly on the body, so the kits need an editing model that keeps what it is not asked to change. fal's Nano Banana Pro edit did that
for the Osprey's pieces (18 Nov 2026, [Osprey overlays](osprey-overlays-record.md)).

## Scope

| Request | Pictures | Ceiling |
| --- | --- | --- |
| `truck-cab-kits-paid.json`: armoured cab and heavy cab plates on the Talus, Ullage, Kelly, Datum and Busbar cabs | 10 requests x 2 seeds = 20 | US$3.00 (US$0.15 each, the table's figure; fal's answers carry no price) |

- **Inputs:** each truck's cab cut from the owner's picked body by `assets/phobos-vehicles/draw_kit_crops.py`, at an exact 4:3,
  16:9 or 3:2 shape (fal takes only standard shapes). Only our own art is uploaded.
- **After the run:** the kit is lifted out with `scripts/art/edit_difference.py` (or `sam_lift.py`), laid back at the crop's box from
  `references/kit-crops.json`, and shown to the owner on the trucks, tinted, before anything is exported.
- **Run:** `python scripts/art-run.py generate assets/phobos-vehicles/requests/truck-cab-kits-paid.json --max-usd <the owner's cap>`.

## Costs and results

**Run 19 Nov 2026, 15:31** (`generated/run-20261119-1531.md`), under the owner's cap of **US$1.50, one try each** (card, same day):
10 pictures, US$1.50 recorded (the table's US$0.15 each; fal's answers carry no price). Nothing refused or failed.

- **Every picture kept its cab** (agent's look, beside the cut cab): outline, wheels, stripe and layout unchanged, the armour added on
  top. The armoured cabs are bolted plates with a framed windscreen; the heavy plates are thick slabs with a slit visor.
- **Sizes came back larger than asked**, nearly the same shape: 1200 x 896 for 1024 x 768 (4:3), 1376 x 768 for 1024 x 576 (16:9),
  1264 x 848 for 768 x 512 (3:2). Each is scaled back to its cut's size before the kit is lifted out.
- On the owner's picking page (`<artWorkPath>/picks/night-7-picks.html`), beside each cab as it is. **Nothing picked yet.**
