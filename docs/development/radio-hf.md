# HF: the radio's Phase 2 (27 Nov 2026)

**Status (28 Nov 2026):** a design record; nothing is built. The owner chose by card on 27 Nov 2026 to write it ("HF, Phase 2") and
answered its questions on 28 Nov ([first round](#the-owners-answers-28-nov-2026)): HF at the world's scale, VOACAP's predictions
shipped as tables, 40 m and 20 m first, the radio's own flare, no licence tiers. In a [second round](#the-owners-second-round-28-nov-2026)
the same day the owner opened **building for everything but HF's reach in play**: the tables (voacapl, under WSL, which the owner
installs), the kit and its Defs, the band panel and band rose against test fixtures, the ground wave from ITU-R P.368, and the kit's
art. HF's skywave in play waits for the owner's first look at the radio's rows (699 to 731).

## Read this first

- **HF stands on our own stations and kit** (the owner, 28 Nov 2026: "it doesn't have to RELY on the comms console"): the console
  needing reach is one optional tie (S3, a setting off by default), never what HF is for.
- **Three things wait on HF**: the comms console needing reach (RT6, the owner's "later, with HF"), factions beyond about 100 tiles,
  and the skip zones and grey line on the world map (RV9, RV10, RV13).
- **The scale must be settled first.** For radio, a world tile counts as 1 km (the owner's gameplay choice of 26 Nov 2026, for VHF), so
  the whole planet is about 1,271 km round for radio. A single real skywave hop is 500 to 3,000 km. At 1 km a tile, every HF link on the
  planet would be one hop or less, and skip zones could not exist. **HF needs its own scale** (question 1).
- **The radio's gates have not passed in order**: Phase 1's gate ("the 6 dB rule feels right in play") is not ticked, and Phase 2's gate
  (a public beta) has not happened, while Phase 3 is open by the owner's word. The design's "each gate before the next" is the owner's to
  waive; this record does not assume it.
- **Performance is the radio's first concern**: nothing here runs every tick or frame for a whole map (cost below).
- Marks: **[O]** the owner's, **[A]** the agent's, **[design]** the radio design's own words (`radio-framework-design.md`, 18 Oct 2026).
  Figures not checked against a source say **unverified**.

## What HF is, in the game

**[design]** HF (3 to 30 MHz) "reaches the world but needs a backpack": worldwide by skywave, regional by NVIS; heavy kit, big
antennas, high draw, noisy, "conditions change hourly". A simple maximum-usable-frequency (MUF) curve by local time and season is
enough; low bands work at night and over short ranges, high bands open by day over long ranges; grey-line openings near dawn and dusk.
"A visible band conditions readout beats hidden ionospheric maths." A full ionospheric simulation is out of scope.

## What exists to build on

| Piece | State | Where |
| --- | --- | --- |
| The link budget between tiles | VHF/UHF only: free space or plane earth, the horizon, high ground | `LinkBudgetService.WorldLosses` |
| Bands | 2 m, 70 cm, the air band, military UHF; no HF band | `Bands.xml` |
| Modes | FM voice, control data, CW (500 Hz, 3 dB, an agent default), AM voice, picture | `Modes.xml` |
| A per-band path model | **Missing**: `BandDef` says the model is chosen per band, but has no field for it; every band takes the VHF path | `RadioDefs.cs` |
| Time of day, season, latitude | **Nothing in the radio reads them** | |
| Noise | Thermal, colony sources, jamming; no atmospheric HF noise | `NoiseFloor`, `NoiseService` |
| Solar flares | Only through power: a station without power reaches nobody | `RadioNet.cs` |
| Weak-signal digital | FT8 decodes at about −21 dB in 2.5 kHz, **sourced** (Franke, Somerville and Taylor, QEX 2020) | `radio-reference-checks.md` |
| Licence limits | Australian classes, **sourced**: Foundation 10 W on 80, 40, 15 and 10 m; Standard 100 W SSB, adds 20 m; Advanced 400 W, adds 160 m and the WARC bands | `radio-reference-checks.md` |
| Relays, the ledger, the logbook, QSL cards, the world pictures, faction calls | Built for VHF; each takes a new band as data | Radio 0.21.0 to 0.54.0 |

## The model [A]

The simplest model that gives the right behaviour, every rule visible to a player who wants to see it.

### Where a signal goes

| Path | When | What it does |
| --- | --- | --- |
| **Ground wave** | Any HF band, short range | Out to some tens of km on the low bands, less on the high: the colony and its neighbours |
| **NVIS** | 80 m and 40 m, a low antenna, day or night | Up and straight back down: a circle of a few hundred km with **no skip zone, and no care for mountains** |
| **Skywave** | Any band below the MUF for the path | One hop of 500 to 3,000 km, more hops for further; a **skip zone** between the ground wave's end and the first hop's landing |

### The MUF, by time and place

One number for each band each game hour: whether the band is **open** to a given distance. Its inputs are the game's own: **local time
at the mid-point of the path** (from the tiles' longitude), **season** (from latitude and the date) and the **sun** (flares). A
table, not a simulation:

| Band | By day | By night |
| --- | --- | --- |
| 80 m | NVIS and short skip; absorbed at noon | Regional and long |
| 40 m | NVIS by day; regional | Long |
| 20 m | Long; the day band | Closes after dark |
| 15 m, 10 m | Long, by day, in good conditions | Closed |

**Unverified**: the table is the general behaviour every operator knows, not read from a source; ITU-R P.533 (HF prediction) and
VOACAP are the references to check it against before it goes into a Def.

### The link budget on HF

The same one link budget, with an HF path term in place of free space or plane earth: spreading over the hop's length, plus an
absorption loss by time of day and band, plus a loss per hop. **Atmospheric noise** replaces the quiet rural floor: far higher on HF
than thermal, higher on the low bands, higher at night and in storms (**unverified**: ITU-R P.372 is the reference to check).

### Kit: physics sets the kit

| Kit | Bands | Why it is the size it is |
| --- | --- | --- |
| **Manpack** [design] | HF and high-power VHF | A backpack with a whip; deploys a field antenna as a temporary building when stopped |
| **Dipole** | One band each | Half a wavelength: about 40 m long on 80 m, 20 m on 40 m, 10 m on 20 m. Two masts and a span of ground |
| **Vertical** | Several | A quarter wave tall with radials; low angle, long distance, noisy |
| **Long wire** | Many, with a tuner | The field antenna: strung from a vehicle or a tree |
| **Beam (Yagi)** | One or two high bands | Directional, on a rotator gizmo [design]: gain where it points |
| **A compact multiband** | 20 m to 10 m | The kind of small high-band antenna the owner once had: easy to place, gives up some gain |

### Modes

| Mode | Needs | What it is for |
| --- | --- | --- |
| **SSB voice** | About +10 dB in 2.4 kHz [design], **unverified** | Talk, advice, faction calls |
| **CW** | Built (Morse key, 500 Hz) | Further than voice; the Morse key's real home |
| **Weak-signal digital** | About −21 dB, **sourced** | Short structured messages at the edge of reach: a convoy's position, "all well" |

### The sun

**[design]** A solar flare blacks out HF on the **sunlit side** of the planet (the Dellinger effect); a geomagnetic storm a day later
brings aurora. The owner chose that hand-helds keep working in a flare (RT18). **Open**: today the game's flare cuts every powered
station's power, so stations go dark everywhere; whether the radio's realistic flare replaces that for stations is question 5.

## What it would change in play

- **The world opens**: every faction in HF reach can be called, at the price of a big antenna and the right band at the right hour.
- **The console needs reach (S3)**, the owner's "later, with HF": a setting, off by default (RT6 graded it high).
- **Convoys far from home stay in touch** by HF, with a manpack or a vehicle's long wire at a halt.
- **Timing matters**: a call to the far side of the planet waits for the band to open; the waterfall and a band rose show when.
- **Pictures** [radio visuals]: skip-zone rings (RV9), the grey line (RV10), contact arcs by band (RV11), the band rose (RV12), the
  flare's wash over the sunlit half (RV13).

## The four questions

- **What it lets the player do:** reach anywhere on the planet, with skill and timing; plan antennas by band.
- **What it stops:** reaching the far side at the wrong hour; casual worldwide calls without the kit.
- **How it changes play:** the radio room gains a timetable; antennas need ground; a convoy carries a manpack.
- **What outside notices:** every faction (calls, trade, help), raiders who listen (EW15), the logbook (DX contacts, awards).

## Risk and settings

The model is a benefit: **low**. S3, the console needing reach, is **high** (RT6): a setting, off by default. A flare blacking out HF is
**moderate**, warned of by a letter.

## Cost [A]

- **Band conditions**: one small table a band each game hour, for the planet: tens of numbers. Nothing per tile, nothing per tick.
- **A link**: as today, worked out when asked; the HF term is a few sums. Searches stay within the rules' bounds.
- **Pictures**: as the world cover pictures, off until picked.
- **To be measured** before and after, as every radio piece.

## Questions for the owner

To be put as cards; each with the agent's lean.

1. **HF's scale**: HF at the world's own scale (about 30 km a tile, so the planet is about 40,000 km round and skywave behaves as on
   Earth) while VHF keeps its 1 km gameplay scale (the agent's lean); or one scale for both, at some middle figure; or HF at 1 km too,
   with skywave hops shrunk to fit (no real skip zones).
2. **How deep the model goes**: a table by band, hour and season (the agent's lean); a simple MUF curve worked out per path; or a full
   prediction from a published method.
3. **The first slice**: 40 m and 20 m with SSB and CW, a dipole, a manpack, and the band conditions readout (the agent's lean); or
   every band at once; or NVIS alone first.
4. **How often conditions change**: each game hour (the agent's lean), or in a few daily phases.
5. **Flares**: the radio's own flare (HF out on the sunlit side, stations otherwise working) in place of the game's power cut for
   stations (the agent's lean), or keep the game's power cut and add the HF blackout on top.
6. **Licence tiers**: research steps that raise the power cap (Foundation, Standard, Advanced), off by default (the agent's lean); on;
   or none.

## The owner's answers (28 Nov 2026)

Each card as put, the chosen option first and marked.

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 1 | HF's scale | **The world's scale for HF**: about 30 km a tile, so skywave, skip zones and the grey line behave as on Earth; VHF keeps its 1 km gameplay scale (the agent's lean) | One middle scale for both. HF at 1 km too, hops shrunk to fit |
| 2 | How deep the model goes | **A published method**, and the owner's words: "maybe we can make performance gains by having some data calculated at the time when you generate the planet, i.e. when you are starting a new game? For players who are mid-game, they are simply met with an introductory screen and told plainly before continuing, their game might pause for sometime as calculations are need to be made as a one-off. I'm just hypothesizing here." | A table by band, hour and season (the agent's lean). A MUF worked out per path |
| 3 | The first slice | **40 m and 20 m first**: SSB voice and CW, a dipole, a manpack, the band-conditions readout (the agent's lean) | Every band at once. NVIS first |
| 4 | Flares | **The radio's own flare**: HF out on the sunlit side, stations otherwise working (the agent's lean) | Both: the game's power cut for stations, and the HF blackout on top |
| 5 | When the heavy calculation happens | **Offline, shipped as tables**, "and also at world generation if the need comes to fruition" | Offline only (the agent's lean). At world generation only. Both, by a setting |
| 6 | Which published method | **VOACAP** (the agent's lean) | ITU-R P.533 with the ITU's ITURHFProp. Both, one checked against the other |
| 7 | Licence tiers | **None**: power is set by the kit alone | Research steps, off by default (the agent's lean). Research steps, on |

### What follows from them [A]

- **Two scales for radio between tiles**: VHF and UHF keep `metresPerWorldTile` (1 km); HF reads its own, the world's 30 km, as a rule
  figure of its own. A path's length in km is then real for HF: a planet about 40,000 km round.
- **Why tables, and why no pause** (the agent's reasoning, with which the owner agreed by card): VOACAP's prediction depends on the path's
  latitudes, local time, month, the solar cycle and its length, not on a particular planet's terrain or the colony. Run once before
  release, its results fit in small tables (in the order of a megabyte: latitude bands × 24 hours × 12 months × a few solar levels ×
  distance bands × bands), which the game only looks values up in. World generation is the fallback should anything come to depend on
  the planet itself (the owner's words).
- **VOACAP's terms**: "developed by an agency of the U.S. Government. NTIA/ITS has no objection to the use of this software for any
  purpose since it is not subject to copyright protection in the U.S."
  ([NTIA/ITS](https://its.ntia.gov/software/high-frequency/voacap-propagation-model/)); the Linux port voacapl's own changes are CC0
  ([its licence](https://github.com/jawatson/voacapl/blob/master/LICENSE)). Checked 28 Nov 2026. The tables we ship are our derived
  data; VOACAP itself is not shipped, and runs only on the machine that makes the tables.
- **The ITU's method is free too, kept for reference**: the reference software for P.533, ITURHFProp
  ([ITU-R Study Group 3](https://github.com/ITU-R-Study-Group-3/ITU-R-HF)), "may be used by implementers in their implementation of the
  Recommendation ... free from any copyright assertions" (its readme, 28 Nov 2026).
- **The pace** [A]: the tables are hourly, so conditions change each game hour.
- **No licence tiers** [O]: the design's open question is answered; the kit sets the power.
- **The tool step**: making the tables needs voacapl installed on the owner's machine, under the shared rule that the owner installs
  tools; the scripts that drive it and the tables' record of how they were made live in this repository.

## The owner's second round (28 Nov 2026)

Put as cards after the first round, for what was still open before building; the chosen option first and marked.

| # | Card | Chosen | Not chosen |
| --- | --- | --- | --- |
| 8 | When building starts (the radio's Phase 1 gate, "the 6 dB rule feels right in play", is not ticked, and rows 699 to 731 are untested) | **Tables now, game later**: the scripts that run VOACAP and the tables, and the game-free rule with its tests; HF's reach in play waits for the owner's first look at the radio rows (the agent's lean) | Build slice 1 now, waiving the gate. Wait for the Phase 1 gate |
| 9 | The sun's cycle | **A cycle a few game years long**, speeded up so a colony sees highs and lows (about one cycle in 3 game years), starting at a random point on a new world, shown on the band readout (the agent's lean) | A fixed middle level. The real 11-year length. A setting, the cycle by default |
| 10 | How VOACAP reaches the owner's machine | **voacapl under WSL** (the Linux port, its changes CC0), the owner installing WSL, a Linux distribution and voacapl (the agent's lean) | NTIA's Windows VOACAP. Neither yet, a small hand-picked test table meanwhile |
| 11 | The comms console needing reach (S3; RT6 graded it high) | **With slice 1, a setting off by default** (the agent's lean) | After slice 1. With slice 1, on by default |
| 12 | How often conditions change | **Each game hour**, as the tables are hourly (the agent's lean) | A few phases a day |
| 13 | Where slice 1 shows band conditions | **The station and the world map**: the station's band panel and the world map's band rose | The station first, the world map later (the agent's lean). The world map first |
| 14 | The grey line and skip-zone pictures (RV9, RV10, RV13) | **A later slice, off until picked**, each measured before and after (the agent's lean) | In slice 1, off until picked. Not at all |
| 15 | What goes ahead beside the tables (several could be picked) | **All four**: the kit and its Defs; the band panel and band rose against test fixtures; the ground wave from a published curve (ITU-R P.368); the kit's art | (The agent leaned to the first two) |

The owner's words with the cards: "We can still implement HF in other ways", and "it doesn't have to RELY on the comms console".

### What follows from them [A]

- **What is built now, and what waits**: the tables, the kit (40 m and 20 m as bands, SSB voice as a mode, the dipole and the manpack,
  sized by wavelength), the band panel on the station and the band rose on the world map, the ground wave, and the art. **HF's
  skywave reach in play** waits for the owner's first look at the radio's rows; until then the panel and the rose run on test fixtures
  in the tests only, never in a Def, as the radio's rules forbid a figure with no source.
- **The ground wave** follows ITU-R P.368 (ground-wave propagation curves), tabulated as VOACAP's predictions are; **not yet read**:
  its figures are checked against the recommendation before they go into a Def.
- **The solar cycle's tables**: VOACAP is run at a few sunspot levels (about four, from a quiet sun to a busy one) and the game
  looks between them; the cycle's length in game years is a rule figure, authored.
- **The pictures that wait**: the grey line, skip-zone rings and the flare's wash (RV9, RV10, RV13), each in a later slice, off until
  picked and measured, as the radio's performance rules ask. The band rose (RV12) comes with slice 1.
- **The art**: by the usual route, the owner choosing paid or a night run each time.

## Sources

- The radio design, `radio-framework-design.md` (18 Oct 2026): the Phase 2 roadmap, the HF and NVIS rows, the sun.
- Franke, Somerville and Taylor, "The FT4 and FT8 Communication Protocols", QEX July/August 2020: FT8's threshold, **checked**
  (`radio-reference-checks.md`).
- The Radiocommunications (Amateur Stations) Class Licence 2023 (Australia): bands and power, **checked**.
- VOACAP: [NTIA/ITS](https://its.ntia.gov/software/high-frequency/voacap-propagation-model/) and the voacapl port
  ([licence](https://github.com/jawatson/voacapl/blob/master/LICENSE)), the method the owner chose; its terms checked 28 Nov 2026, its
  predictions not yet run.
- ITU-R [P.368](https://www.itu.int/rec/R-REC-P.368/en) (ground-wave propagation curves, 10 kHz to 30 MHz): the ground wave's
  source, the owner's pick of 28 Nov 2026, **not yet read**.
- ITU-R [P.533](https://www.itu.int/rec/R-REC-P.533/en) (HF propagation prediction) with its reference software
  ([ITURHFProp](https://github.com/ITU-R-Study-Group-3/ITU-R-HF)), and ITU-R P.372 (radio noise): references, **not yet read**.
- Our records: [radio ties](radio-ties-with-other-mods.md) (RT6, RT18), [the comms console](radio-comms-console.md) (S3),
  [radio visuals](radio-visuals.md) (RV9 to RV13), [world elevation](world-elevation-and-descents.md) ("HF does not care" about
  elevation), [project direction](project-direction.md) (the tile scales).
