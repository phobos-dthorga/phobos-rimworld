# Night 9 picks: the fuel pump and the paddock button (21 Nov 2026)

**Status (21 Nov 2026):** the owner's picks, made by question card from one picture per item on 21 Nov 2026. Two pictures ship:
the fuel pump in D'thorga Framework 0.210.1 and the paddock button in D'thorga Farming 0.7.3. **The heavy-duty brakes and the clutch
kit go to the paid image service** (the owner's choice, so they match the other parts pictures, which OpenAI drew); the first try
was refused because the OpenAI account had no credit left, so nothing was made or spent. **The standard grain front and the fuel
pump's charging cable overlay are still to make.** Not yet tested in game.

Night 9 (`.local/art/night-9.json`, 21 Nov 2026, 06:02 to 11:01) ran on the owner's go before bed: the fuel pump on the old style
model (v1) with our depot bay and parts bench as style references, then the training of the new house style model (house-v1, our own
151 picks, 3,000 steps in 158 minutes), then the four redraws drawn three ways (v1 with references, house-v1 with references, house-v1
alone), then the fuel pump on house-v1. Local, FLUX.2 Klein 4B base, about 2.5 minutes an image; 58 pictures. The full record is in
`docs/development/art-backlog.md`.

## Picks

| Item | Pick | File | Ships as |
| --- | --- | --- | --- |
| Fuel pump | 1 | `station-fuel-pump-b-house-11.png` | Framework `Stations/FuelPump.png` |
| Paddock (button) | 1 | `redraw-paddock-c-house-11.png` | Farming `UI/Paddock.png` |

Both picks came from the new house-v1 model with references, and both were the agent's lean.

## Not picked

| Item | The owner's choice | Why |
| --- | --- | --- |
| Heavy-duty brakes | The paid service | Keeps the parts' art style; the local tries read as a turbocharger (night 8) and a bearing (night 9) |
| Clutch kit | The paid service | Keeps the parts' art style; the local tries were a blank disc, an odd ring and turbine fans |
| Standard grain front | Not yet asked | Its right-looking tries were flagged at 0.97 against the wide front's pick: they are the wide front again |

## Near-copy flags

14 pictures were flagged and stay local: nearly all against our own pictures in house-v1's training set, three against the game's
round `IconMorbidA` at very low structure scores (0.07 to 0.11). The reports beside each batch list them.
