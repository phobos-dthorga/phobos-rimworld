# The C-27J Spartan's fit overlays and icons: paid pictures, stage 2 (26 Nov 2026)

**Status (26 Nov 2026):** fourteen pictures made, none picked yet, every one offered on D'thorga Companion's Picks page with its card.
Made without the owner's graphics card, at the owner's request ("Can you do all of these without accessing my GPU?", then "Go").
Nothing exported or shipped. Not yet tested in game.

- **The three fit overlays** ([c27j-overlays.json](c27j-overlays.json)): the MC-27J gun kit's barrel, the fire-fighting tank's
  discharge pipe and one defensive aids pod, on Google Nano Banana Pro through fal, two tries each. Under the owner's US$3.60 cap for the
  C-27J's art ([stage 1](c27j-paid-record.md)), of which US$1.80 remained: **US$0.90 spent** by the table's ceilings, US$0.90 left.
- **Four icons** ([c27j-icons-paid.json](c27j-icons-paid.json), [c27j-icons-paid-fal.json](c27j-icons-paid-fal.json)): D'thorga
  Framework's Low extraction (LAPES) button, and the C-27J's defensive aids, gun kit and fire-fighting tank upgrades. **The owner's card of
  26 Nov 2026: "Paid, US$1.19 cap"** (the airdrop buttons were made paid after the owner rejected their local tries). The stretchers
  reuse the Osprey's stretcher icon, being the same litter kit. **Spent about US$0.65** by the ceilings: six on OpenAI (US$0.35, the run's
  figure from OpenAI's token counts), two on fal (US$0.30).

How each picture was made is written per image beside it (`assets/phobos-vehicles/generated/<request>/<request>-N.json`: prompt,
references with roles and SHA-256, the provider's request ID, the size that came back, the AI disclosure and the terms note).

## The route: each part drawn alone, laid on by script

The design planned the overlays "as edits of the picked body". The agent first cut two pieces of the body for them (rule 13 of the paid
rules, the Osprey's lesson), then followed the newer rule (assets/AGENTS.md, owner's review of 22 Nov 2026): **a part that sits on a
vehicle and stands alone is drawn alone on white and laid on by script**, since parts lifted out of an edited vehicle came out "scrubbed
out". The body pick is each request's one reference, for finish only. The crop script was deleted unused.

| Part | Drawn | Laid on by script (to come, after the pick) |
| --- | --- | --- |
| Gun barrel | Pointing to the top, its collar at the bottom | At the left jump door (about x 2,067 to 2,113, y 2,353 to 2,467 in the body pick), turned to point out to the left, about 1.3 m beyond the fuselage side **[A]** |
| Discharge pipe | Its flange at the bottom, rising, bending 60 degrees to the left | At the left jump door, turned a quarter turn anticlockwise so it reaches out and bends toward the tail, about 1 m out **[A]** |
| Defensive aids pod | Long side top to bottom: jammer dome above, flare dispenser below | On the left main-gear fairing (about x 1,947 to 2,087, y 1,900 to 2,180), mirrored onto the right. Its look is the agent's design, not a drawing of the RAAF's fit |

The pick measures about 149 px a metre; the shipped body 120 px a cell at 1.175 cells a metre, about 141 px a metre. **Measured against the
1.5x rule**: the barrel's shipped length is about 225 px against about 2,300 px drawn (10x); the pipe about 200 px against 2,240 (11x); the
pod about 140 px against 770 (5.5x).

## What each made (agent's review at full size and on its card)

| Try | Cost ceiling | Review |
| --- | --- | --- |
| Gun barrel 1 | US$0.15 | Straight, symmetrical, dark gunmetal; a slim barrel with a slotted muzzle brake and a stepped collar. Clean outline at full size, 0 loose specks |
| Gun barrel 2 | US$0.15 | The same, chunkier: a wider collar and a heavier slotted brake; reads better at the shipped size. Clean, 0 specks |
| Pipe 1 | US$0.15 | Pale blue-grey pipe, a bolted flange, a clamp band, a flared nozzle with a dark mouth. The flange and nozzle mouth are drawn face on, a little off the plan view; the flange sits at the door, so mostly hidden. Clean, 0 specks |
| Pipe 2 | US$0.15 | The same in a warmer grey, the flange drawn larger and face on. Clean, 0 specks |
| Pod 1 | US$0.15 | A riveted grey plate, a glossy dark dome in a ring, a dispenser grid of 5 by 4 caps (the prompt asked 3 by 5). Clean, 0 specks |
| Pod 2 | US$0.15 | Slimmer, as asked: a 3 by 5 grid, a larger dome. Clean, 0 specks |
| LAPES button 1 | about US$0.055 | Platform with a strapped load sliding right, dust behind, an open olive chute ahead pulling it, a short amber arrow. 2 loose specks: the drawn dust, on purpose |
| LAPES button 2 | about US$0.055 | The same with a longer amber arrow beneath. 2 specks, the dust |
| Defensive aids 1, 2 | about US$0.055 each | A pod plate with a dark dome and a flare dispenser box, two amber flares arcing away |
| Gun kit (OpenAI) 1, 2 | none made | **Refused by OpenAI's filter both times** ("moderation_blocked", "illicit"): a gun. A refusal is a route change (rule 7), not a retry |
| Gun kit (fal) 1, 2 | US$0.15 each | A 30 mm gun on an olive mount on a steel roller pallet, an ammunition box beside it, pointing right |
| Fire tank 1, 2 | about US$0.055 each | A pale grey tank with a muted red band, a filler cap and a bent discharge pipe, on an olive pallet |

**Near-copy**: every reference was our own art, so the run's own check had nothing to compare against, by design.

## Owed

- The owner's picks (Picks page), then the parts laid on the body by script (east and south facings turned from north), paint kept off
  them, and the icons exported at 128 px; the overlays ship with the fit nodes that show them (slice 3's second part).
- Every try to the art branches after the picks: picks to `art-masters`, the rest to `art-references`.
