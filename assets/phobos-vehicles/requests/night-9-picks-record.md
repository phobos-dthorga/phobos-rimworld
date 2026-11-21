# Night 9 picks and the paid redraws: pump, cable, paddock, brakes, clutch kit, standard front (21 Nov 2026)

**Status (21 Nov 2026):** the owner's picks, made by question card from one picture per item on 21 Nov 2026. Six pictures ship:
the fuel pump (Framework 0.210.1) and the paddock button (Farming 0.7.3) from night 9's local run; then, from OpenAI's image API (the
owner's choice, to match the parts pictures OpenAI drew, once the owner had topped up the account): the heavy-duty brakes, the
clutch kit and the fuel pump's charging cable overlay (Framework 0.210.2) and the standard grain front (Farming 0.7.4). Eight paid
pictures, two tries each, about US$0.42. Not yet tested in game.

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
| Heavy-duty brakes | 1 | `fitting-heavy-brakes-paid-1.png` (paid) | Framework `Fittings/HeavyBrakes.png` |
| Clutch kit | 1 | `part-clutch-kit-paid-1.png` (paid) | Framework `Parts/ClutchKit.png` |
| Standard grain front | 2 | `farming-front-standard-paid-2.png` (paid) | Farming `Upgrades/FrontStandard.png` |
| Charging cable overlay | 1 | `station-fuel-pump-cable-paid-1.png` (paid edit of the pump pick) | Framework `Stations/FuelPumpCable.png` |

The two local picks came from the new house-v1 model with references. Every pick but the standard front (the agent leaned to try 1)
was the agent's lean. The cable's try 1 kept the pump within one pixel of the pick (try 2 moved and shrank it), so it could be cut
by the new `scripts/art/export_overlay.py`; its coil reaches past the pump's right edge, so the overlay is drawn at (1.188, 2).

## Routes tried before the paid pictures

| Item | Local tries | Why they were not used |
| --- | --- | --- |
| Heavy-duty brakes | Night 8 (two), night 9 (twelve) | Read as a turbocharger, then a bearing; the owner chose the paid service |
| Clutch kit | Night 8 (two), night 9 (twelve) | A blank disc, an odd ring, turbine fans; the owner chose the paid service |
| Standard grain front | Night 9 (twelve) | The right-looking tries were the wide front again (near-copy 0.97 against its pick) |

## Near-copy flags

14 pictures were flagged and stay local: nearly all against our own pictures in house-v1's training set, three against the game's
round `IconMorbidA` at very low structure scores (0.07 to 0.11). The reports beside each batch list them.
