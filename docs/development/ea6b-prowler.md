# The EA-6B Prowler in the fold (22 Nov 2026)

**Status (22 Nov 2026):** step 1 built: D'thorga Vehicles 0.99.0 (engines, systems, ICAP upgrades, enemy fits), D'thorga Patches 0.33.0
(parts list, parts bill, rearming, dodge, salvo button, the ICAP hook, realistic handling) and D'thorga Framework 0.224.0 (the jamming
reach figure on fittings). Step 2 built: Framework 0.225.0 and Patches 0.34.0 (jamming fields blind other factions' radar and warning
receivers). Not yet tested in game: rows 342 and 343 of the [test pass](in-game-test-pass.md). Steps 3 and 4 are designs below.

## Read this first

- **The mod:** EA-6B Prowler Electronic Attack Aircraft (`potemkinvillager.ea6bprowler`, Workshop 3835738691), by the Harrier's, Hind's,
  Mongoose's and Close Air Support's author, on the same fixed-wing framework as the A-10. It ships its own source (`Source/`), read on
  22 Nov 2026; no decompile was needed.
- **The owner's choices (22 Nov 2026, question cards):** the full treatment **with the ICAP upgrades**; all three of our own ECM ideas
  (jamming fields that jam our systems, a self-protection pod, a jamming truck); **realistic handling as a setting, off by default**, kept
  even though it changes nothing today ("players might wonder there's something literally missing otherwise").
- **Its jamming:** sent to circle a spot, it jams inside up to 44 cells (wider with its operators' Intellectual): automated turrets shut
  down or fire wildly, shields drop, mechanoids slow and are stunned, powered buildings flicker. The field shrinks as the airframe or pods are
  damaged. It has its own dodge (40 % of hits miss in the air), its own cook-off, crash slide and pilot loss.
- **Figures:** the flight manual (NATOPS NAVAIR 01-85ADC-1) is not freely available, so no J52 engine limits were found; every authored
  figure says so beside it in the XML.

## The four questions

| Question | Answer |
| --- | --- |
| What it lets the player do | Run the Prowler on our engine, electrics and hydraulics model; build it from real parts; refit it to ICAP II or III for a wider jamming field; fire chaff and flares to raise its dodge |
| What it stops them doing | Flying it with its hydraulics or controls shot out (our flight-control rule); keeping its full dodge with a dead warning receiver |
| How it changes play | Its upkeep and refits matter as for our other jets; enemy Prowlers may arrive as later versions with wider fields |
| What outside the vehicle notices | Other factions (their Prowlers carry ICAP suites by tech level), the air defence of another mod (its air track), the colony's workshops (its parts bill, its refits) |

## What was built (step 1)

| Piece | Where | Note |
| --- | --- | --- |
| Engines | `PhobosDrivetrain_J52` (Vehicles, `Mods/EA6BProwler`) | J52-P-408, 49.8 kN take-off (secondary: Wikipedia, "Pratt & Whitney J52", from Flight International 1973; the Navy's fact file gives 46.3 kN). Maximum continuous and limits authored |
| Systems | `PhobosVehicleLoadout_Prowler` | Starts as the ICAP of 1976: basic navigation (ASN-130 inertial), navigation radar, no head-up display, no autopilot, a warning receiver by inference, 60 chaff and flare cartridges (authored), oxygen by inference |
| ICAP upgrades | `Fittings_EA6BProwler.xml` | **ICAP II** (1982): field +10 %, full navigation. **ICAP III** (2005): field +25 %, full navigation, warning receiver. One replaces the other on the fin-top receivers. Factors authored |
| Enemy fits | Same file | ICAP II for industrial to spacer factions, ICAP III for spacer and above |
| Parts list | Patches, `Parts_EA6BProwler.xml` | Avionics, hydraulics, wiring, landing gear, fin-top receivers; its own six parts untouched |
| Parts bill, cost | Patches | Two turbines, four tanks, three gear legs, two generators; shell 600 steel, 180 plasteel, 15 components, 10 advanced components |
| Rearming | Patches, `RearmOneKind` | The same fixed-wing framework code as the Harrier and A-10 (temporary patch row 6) |
| Dodge and salvo | Patches, `PhobosPatches.Prowler` | Its own dodge follows its flight controls and warning receiver; the chaff and flares button adds to it, as the Harrier's |
| ICAP hook | Same | A postfix on its `Jammer.EffectiveRadius` multiplies by the fittings' `jamRadiusFactor` (Framework 0.224.0); a stock jet is untouched |
| Realistic handling | Patches setting, off | Top speed 0.27 (980 km/h with pods, secondary: aerospaceweb.org) and cruise 0.21 (774 km/h, secondary: Wikipedia) on the Harrier's scale: **both equal the author's figures today**; it holds them if his change |

**Risk: low** (agent's grade, for the owner to confirm): nothing here can lose a thing or a person; it only reshapes the jet's systems.

**Other factions' vehicles:** a raiding Prowler runs our systems on the lite tier and may carry an ICAP suite; one at a base is parked with
its fittings; one captured keeps them.

## Our own ideas on its jamming (owner's choices, 22 Nov 2026), steps 2 to 4

- **Step 2, fields jam our systems (built, Framework 0.225.0, Patches 0.34.0):** `JamFields` in the Framework holds every registered jammer's
  fields (read at most once a second per map); `AirCombat.Works` answers "not working" inside a hostile field for the capabilities the rules
  Def lists, **radar and warning receiver** (agent's realism default: a radar jammer floods radar frequencies; an infrared night sight and an
  autopilot's gyros are untouched; the owner can add them in `Rules_VehicleSystems.xml`). Differs from the card's wording, which named the
  night sight and autopilot too; said to the owner in the reply. Fields never jam their own faction, nor an ally. The design as first
  written: any faction's jamming field (the Prowler's, read through its own public `Jammer.TryGetJamCenter` and
  `Jammer.EffectiveRadius`, and later ours) answers "not working" for the radar, night sight, autopilot, warning receiver and the aim
  assist of every vehicle of another faction inside it. All our aircraft ask one place (`AirCombat.Works`), so one check covers them.
- **Step 3, a self-protection pod:** a fitting of ours for other aircraft that makes enemy automated turrets slower to lock on and less
  accurate against the aircraft carrying it. We already have an infrared jammer upgrade for helicopters (`Phobos_InfraredJammer`); the pod
  is its radar-band partner.
- **Step 4, a jamming truck:** an upgrade for one of our trucks: a smaller field of our own while parked and running, feeding step 2's
  field service. Which truck is the owner's call; it needs its own overlay art.

## Not built

- The cancelled ADVCAP (J52-P-409 engines, +2,000 lbf, wing changes; cancelled 1994): never fielded, not offered.
- The ICAP III's communications jammer: our radio work is set aside.
- Icons for the two ICAP fittings: still to draw, with the other aircraft fittings' icons.

## Sources

- The Prowler mod's own source and Defs (`potemkinvillager.ea6bprowler`, read 22 Nov 2026), summarised in our own words.
- US Navy fact file, EA-6B, archived 2004 (primary): webharvest.gov/peth04/20041018022103/http:/www.chinfo.navy.mil/navpalib/factfile/aircraft/air-ea6b.html
- Wikipedia, "Northrop Grumman EA-6B Prowler" and "Pratt & Whitney J52" (secondary).
- FAS, EA-6B Prowler: irp.fas.org/program/collect/ea-6b_prowler.htm (secondary, reproducing Navy data).
- aerospaceweb.org, EA-6B (secondary).
- Congressional Research Service, report RL30639 (2001), on the EA-6B's upgrades (secondary for our purpose).
- VAQ-139's history, airpac.navy.mil (primary), for ICAP III's first deployment.
- Research gathered by an agent on 22 Nov 2026 from the pages above; the Navy's current pages and GlobalSecurity refused automated reads.
