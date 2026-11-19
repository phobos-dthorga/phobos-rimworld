# Night 7 picks: the Osprey's icons and sounds (19 Nov 2026)

**Status (19 Nov 2026):** the owner's picks, made by question card from the review pages; none is exported or wired into a mod yet.
The icons marked "paid" wait for a redraw through the paid service, with a dry run and a spending cap first.

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
| Ramp gun, 7.62 mm; ramp gun, .50; belly gun; infrared jammer; terrain radar; inlet barrier; rescue hoist; fast ropes; stretchers; ferry tanks; cabin oxygen; refuelling probe; fire mode: automatic | **Paid redraw** (owner, 19 Nov 2026) | Interim icons and stand-ins stay until then |

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
