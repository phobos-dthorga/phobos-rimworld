# Night 7 picks: the Osprey's icons and sounds (19 Nov 2026)

**Status (19 Nov 2026):** the owner's picks, made by question card from the review pages, the paid redraw included. **Shipped in
D'thorga Vehicles 0.85.0 with Framework 0.169.0:** fourteen icons (the Osprey's eight upgrades, the fast-rope, hoist and cabin oxygen
fittings, the three fire modes) and the eight sounds. **Held back:** the cargo hook, stretchers and ferry tank picks (those fittings keep
the pictures the owner picked earlier, 11 and 18 Nov 2026) and the tanker kit, refuelling probe and crewless fit picks (their upgrades are
not built yet). Not yet tested in game.

Night 7 (`.local/art/night-7.json`, run by the armour session) made two local pictures of each Osprey upgrade icon
(`osprey-upgrade-icons.json`) and of each fire-mode button (`assets/vehicle-systems/requests/flight-and-fire-buttons.json`), and
generated candidate Osprey sounds (`assets/phobos-sounds/requests/night-7.json`). Seventeen real recordings were cut from public-domain
DVIDS footage beside them (`assets/phobos-sounds/recordings/osprey.json`, the owner's rule of 19 Nov 2026 for real machines).

## Icons

The agent's note before the picks: both 7.62 mm ramp guns came out as assault rifles, both .50s as a post holding two boxes, both
rescue hoists as a valve wheel on a box.

| Icon | Pick | File |
| --- | --- | --- |
| Armour kit | B | `osprey-upgrade-armourkit-42.png` |
| Second hook | A | `osprey-upgrade-secondhook-29.png` |
| Cargo hook | B | `osprey-upgrade-cargohook-28.png` |
| Tanker kit | B | `osprey-upgrade-tankerkit-26.png` |
| Crewless fit | A | `osprey-upgrade-crewlessfit-53.png` |
| Fire mode: single | A | `firemode-single-11.png` |
| Fire mode: burst | A | `firemode-burst-11.png` |
| Ramp gun, 7.62 mm; ramp gun, .50; belly gun; infrared jammer; terrain radar; inlet barrier; rescue hoist; fast ropes; stretchers; ferry tanks; cabin oxygen; refuelling probe; fire mode: automatic | **Paid redraw** (owner, 19 Nov 2026) | Below |

## The paid redraw (19 Nov 2026)

Requests: `osprey-paid-icons.json` (OpenAI gpt-image, medium quality, 1024 px, two attempts each) and `osprey-paid-guns.json` (fal,
Nano Banana Pro edit, 1K). Style references, both ours: the drill rig's approved Work Lights icon and the owner's armour kit pick (B);
the guns used the infrared jammer pick in place of the armour kit, and the automatic button the single and burst picks. Dry runs first;
the owner's go with a US$4 cap for the whole batch.

- **OpenAI refused every firearm** (`moderation_blocked`, "violence" and "illicit"): both ramp guns' first attempts and one of the
  automatic button's two. Nothing is billed for a refusal. The guns went to fal at the owner's choice, which drew them.
- **Spend:** about US$1.20 at OpenAI (19 pictures) and US$0.90 at fal (6 pictures): **about US$2.10** of the US$4 approved.

| Icon | Pick | File |
| --- | --- | --- |
| Ramp gun, 7.62 mm | A | `osprey-fal-rampgun762-101.png` |
| Ramp gun, .50 | B | `osprey-fal-rampgun50-104.png` |
| Belly gun | A | `osprey-fal-bellygun-105.png` |
| Infrared jammer | A | `osprey-paid-infraredjammer-67.png` |
| Terrain radar | B | `osprey-paid-terrainradar-70.png` |
| Inlet barrier | A | `osprey-paid-inletbarrier-71.png` |
| Rescue hoist | B | `osprey-paid-rescuehoist-74.png` |
| Fast ropes | A | `osprey-paid-fastropes-75.png` |
| Stretchers | A | `osprey-paid-stretchers-77.png` |
| Ferry tanks | A | `osprey-paid-ferrytanks-79.png` |
| Cabin oxygen | B | `osprey-paid-cabinoxygen-82.png` |
| Refuelling probe | A | `osprey-paid-refuellingprobe-83.png` |
| Fire mode: automatic | The one made | `firemode-paid-auto-85.png` |

## Sounds

Judged on the listening pages with their measurements: the rotor comb check (lines every 20.55 Hz, the real proprotors' blade
passes, 411 rpm with three blades, measured from the recordings by the armour session) and the measured flags.

| Sound | Pick | Source | Measured |
| --- | --- | --- | --- |
| Idle | `osprey-idle-791592-00800` | DVIDS recording | Comb 20.49 Hz, 32.6 dB, 6 of 6 lines; seam 0.7 dB; flag: strong highs (the turbines' whine) |
| Working hard | `osprey-labour-978528-00005` | DVIDS recording | Comb 21.9 Hz (heard passing by, so shifted), 33.6 dB, 6 of 6; seam 0.9 dB |
| Landing | `osprey-landing-791592-00400` | DVIDS recording | 22 s; comb 20.56 Hz, 30.8 dB, 6 of 6; flag: strong highs, true peak -0.5 dBTP |
| Shutdown | `n7-osprey-shutdown-72` | Generated | No flags; 2.1 s |
| Engine start | `n7-osprey-start-73` | Generated | No flags; 2.0 s |
| Ramp | `n7-osprey-ramp-73` | Generated | No flags; 2.1 s |
| Crew door | `n7-osprey-door-72` | Generated | Flag: strong highs (9 dB below), the mildest of three |
| Fast rope | `n7-fast-rope-73` | Generated | Flag: sharp attack (21 dB), the gentlest of three |
| Rescue hoist | **None** | | The owner: "Not needed amongst all the other sounds" |
| Forward flight | **None** | | Idle and working-hard loops only; the flyby recording is not used |

Credit for every recording: U.S. Department of Defense footage via DVIDS, public domain as a U.S. federal work; its use does not imply
endorsement. Each sound's own record names its video and URL when it is exported (`scripts/art/export_sound.py`).
