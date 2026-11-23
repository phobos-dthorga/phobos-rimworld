# Aircraft service-life upgrades (21 Nov 2026)

**Status (21 Nov 2026):** built, not yet tested in game: D'thorga Framework 0.208.0 (engine and avionics fittings, the older-save fittings,
and the avionics' effects A3, A4, A10, A11) and D'thorga Vehicles 0.93.0 (the jets' early fits and their own upgrades). Test rows 306 and 307. Since Framework 0.246.0 and
Vehicles 0.107.3 the A-10's and Su-25's new engines are a matched pair (row 400).

## Read this first

- **The owner's request (21 Nov 2026):** upgrades of our own for the jets, "whether it's jet-by-jet or across the range, given that many
  of them have had many changes over the years throughout all of their service lives", without stepping on Close Air Support's author,
  who is changing the jets themselves. So **everything here is a fitting of ours, beside the authors' upgrade trees**, never in them.
- **The owner's choices (21 Nov 2026, question cards):** the first batch as proposed (engine upgrades, the Su-25's extra dispensers,
  three avionics kits for any aircraft, the jets starting at their early fits); **the A-10's never-fielded engine, included and
  labelled**; **the autopilot's and night sight's effects built with them** (avionics A3, A4, A10, A11), since a kit must change an
  outcome; **the Harrier starts early too**, with Harriers already built given its night sight and radar free when a save loads.
- **Not yet tested in game.**

## The four questions

| Question | Answer |
| --- | --- |
| What it lets the player do | Bring an aircraft through its service life: an early A-10A to an A-10C, a first Su-25 to its post-Afghanistan and SM refits, a day-attack AV-8B to a Night Attack and Harrier II Plus; re-engine the Harrier and the Su-25; give any aircraft of any mod navigation, an autopilot, a night sight or a radar it lacks |
| What it stops them doing | Flying a new-built jet with a fit it only gained decades later; night attacks at day accuracy; a free landing in fog with a crippled aircraft |
| How it changes play | A jet becomes an investment worked up over time, and the parts that carry its upgrades become worth protecting; night and weather become reasons to wait, divert or fit a sight |
| What outside the aircraft notices | Raiders' and enemy bases' aircraft arrive at different generations through Framework's enemy fitting sets, so an old Su-25 one raid and a refitted one the next; colonists do the fitting work; the weather and the time of day decide whether an attack lands; rough landings show in the fault history and the alerts |

## The pieces

### 1. Fittings for engines and avionics (Framework)

- **Fittings can be limited to some loadouts** (`loadouts`), so a Harrier engine is offered only on a Harrier, and to some kinds
  (`kinds`, such as Aircraft).
- **Engine upgrades** (`thrustFactor`, `hotDerateFactor`): a turbine's rated thrust and its output against demand grow by the factor,
  and its loss in hot air shrinks by the other. Shared across a twin's engines by Framework's share-per-part rule, so a re-engining
  is complete only with both engines done. **Since Framework 0.246.0 the two twins' engine fittings are matched sets** (`matchedSet`;
  the owner, 23 Nov 2026: fitted one engine at a time, the jet "wouldn't fly very stable"): ordered on both engines at once, one
  turbine and one job each, cancelled and taken off together, and moving no figure until both carry it. Before, one engine done gave
  half the gain. An aircraft that already had one engine done keeps it, and its gain waits for the other; the fittings window says so.
- **Avionics kits** (`parameterFloors`): while the fitting's part stands, a system's figure is at least the fitting's. The avionics
  system gains the figures autopilot, sight, nightSight and radar beside its existing ones, each a quantity read from its part, its
  power and its figure, as the warning receiver is. Every aircraft gets three small mount parts for them (an autopilot servo bay, a
  sensor station, a radar bay; the Harrier's own nose radar part where it has one). A kit is offered only where it adds something.
- **Dispensers** (`cartridgeFactor`): a fitting that adds dispensers raises the magazine.
- **Older saves** (`fittedOnOlderSaves` on a loadout): an aircraft saved before its loadout started early is given the listed
  fittings free, once, when the save loads, so no Harrier loses its night sight or radar.

### 2. What the avionics do (Framework; avionics A3, A4, A10, A11)

As the avionics record designed them, with risk settings by the shared rule. Detailed in `vehicle-avionics.md` when built.

### 3. The jets (Vehicles)

| Jet | Starts as | Upgrades (real, with the year they came) |
| --- | --- | --- |
| Harrier | Day-attack AV-8B: no night sight, no radar | Night sight (Night Attack, 1989), radar (Harrier II Plus, 1993), the Pegasus 11-61 (F402-RR-408) |
| A-10 | A-10A: basic navigation, no autopilot, no night sight | Navigation (GPS, 1999), autopilot (LASTE), night sight (the A-10C's targeting pod); **uprated engines, offered by General Electric and never fielded** |
| Su-25 | First Su-25: basic navigation, R-95Sh engines, 128 flare cartridges | Extra dispensers to 256 (after Afghanistan, unverified), the R-195 engines, navigation (Su-25SM) |

Figures and sources are written beside each value in the loadouts and fittings. Years (secondary sources): the Night Attack Harrier first
delivered in September 1989 (others give 1990 to 1991 for its fielding), the Harrier II Plus with its APG-65 radar in 1993, the A-10's
LASTE autopilot in service by 2004 (its first year not found).

## Risk

- **The fittings themselves: none.** They only add.
- **Starting early: low**, agent's grade: a new jet is weaker than today's; older saves keep what they had.
- **Rough landings (A4): moderate**, agent's grade: damage to the gear or airframe, never the crew, with a message, after a choice the
  player made (landing in fog or crippled). A setting, on by default, per the shared rule.

## Other factions' aircraft

A raider's or enemy base's aircraft gets fittings by Framework's enemy sets (by chance and tech level), so it may arrive at any
generation. A captured one keeps what it had. The effects (A3, A4, A10, A11) work for any faction's aircraft.
