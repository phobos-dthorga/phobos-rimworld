# Radio stations: the radio desk and the radio terminal (24 Nov 2026)

**Status (24 Nov 2026):** record only. Nothing is built. The design waits on the owner's answers to the questions
[below](#questions-for-the-owner); the art for both stations is in a paid batch whose dry run is recorded in
`assets/phobos-radio/requests/paid-1-record.md`.

The owner, 24 Nov 2026, of the base station's picture: "they come in so many varieties these days ^ ^ A computer terminal can
effectively be a base station with an SDR and hefty amplifier." Then, by card: the two paid tries for the base station's picture
are "One of each, they could each have wildly different stats to each of them :)", and the SDR terminal is to be designed now, as
its own kit. The radio's rules apply: **performance first**, and **physics sets the kit, the kit sets the play**
(`src/PhobosRadio/AGENTS.md`).

**How to read the marks.** **[O]** the owner's word. **[code]** read in our code or Defs on 24 Nov 2026. **[S]** a source named
beside it. **[A]** an agent's proposal or authored figure, for the owner to revise. Every figure not marked **[S]** is authored.

## Read this first

- **Two stations, each its own kit [O].** The radio desk is today's base station with a new picture; the radio terminal is new: a
  computer with a software-defined radio (SDR) and a linear amplifier.
- **On one map the terminal's edge is modest, and that is said plainly.** The hand-held's 5 W usually decides a link, so the
  terminal's power helps the weaker direction only rarely; its quieter receiver gains about 4 dB where the colony is quiet. Its
  real identity (more bands, digital modes, the waterfall) arrives with Phase 2 **[A]**. In Phase 1 it is a better receiver that
  needs an outside antenna and makes its own noise.
- **No risk to the player's things or people**, so no risk setting is needed (the shared design rules).

## The two stations

| | Radio desk (today's base station) | Radio terminal (new) |
| --- | --- | --- |
| What it is | A desk transceiver with a power supply, desk microphone, power meter and logbook | A computer driving a software-defined radio, with a linear amplifier in a cabinet beside it |
| defName | `PhobosRadio_BaseStation` (kept; a label may change, never a defName) **[code]** | `PhobosRadio_RadioTerminal` **[A]** |
| Footprint | 2 × 2 **[code]** | 3 × 2, the vanilla comms console's footprint **[A]** |
| Power, idle | 60 W **[code]** | 250 W, more than the comms console's 200 W for the amplifier on standby **[A]** |
| Transmit power | 25 W **[code]** | 100 W, the Australian Standard licence's limit **[S]** ([reference checks](radio-reference-checks.md#australian-licence-limits)) |
| Receiver noise figure | 6 dB **[code]** | 2 dB, a quiet front end with a preamplifier **[A]**, unverified against a source |
| Antenna | A built-in whip at 2 m, or the best antenna on its coax **[code]** | **None of its own**: it reaches nobody until coax joins it to a whip or mast **[A]** |
| Its own noise | None | **A noise source itself while powered**: −75 dBm at 1 m, out to 8 cells, fading like every source **[A]**. Computers and switch-mode power supplies are well-known radio noise makers; the figure is authored |
| Bands | VHF and UHF **[code]** | VHF and UHF now; the wider bands an SDR covers join in Phase 2 **[A]** |
| Heat | Waste heat while in use **[code]** | The same rule, four times the power: more heat in the shack **[A]** |
| Research | Local radio **[code]** | A new project, **Software radio**, after Local radio and the game's Microelectronics **[A]** |
| Cost | 100 steel, 3 components **[code]** | 120 steel, 6 components, 1 advanced component **[A]** |

**What the figures do on one map** (the link-budget arithmetic of `LinkMath`, worked by hand **[A]**): the forward direction, hand-held
to station, usually decides. The terminal's receiver is 4 dB quieter than the desk's, so where nothing noisy is near its antenna a
hand-held gains about 4 dB of signal to spare. Its 100 W adds 6 dB only to the station-to-hand-held direction, which rarely decides.
**Its own noise can undo all of it**: an antenna within a few cells of the terminal hears the computer, so the terminal wants its
antenna well away, which is the siting lesson the radio already teaches with generators.

## The four questions

| Question | Answer **[A]** |
| --- | --- |
| What it lets the player do | Run a quieter, stronger station; later (Phase 2) reach further bands, use digital modes, and see the waterfall on its screen (RV3). |
| What it stops them doing | Running it on its own: it has no antenna, so it needs coax to an outside whip or mast. Siting an antenna near it, which its own noise spoils. |
| How it changes the way they play | The shack goes indoors and the antenna goes out and away; the power grid feels a 250 W machine; the room warms. |
| What outside it notices | The power grid and the room's temperature, through the game's own systems. Nothing else in Phase 1, by choice; Phase 3's direction finding would make a 100 W station easier to trace. |

## Its cost (the radio's performance rule)

| What | How much | How often |
| --- | --- | --- |
| A second station in the net | One more station in the loop that already pairs every worn hand-held with every station's antennas | Every recompute, about once a second **[code]** |
| Its own noise | One more source in the noise snapshot, summed like the others, no allocation | Each noise lookup **[code]** |
| Its coverage picture | The same overlay as the desk's, while it is selected | Only while selected |

Nothing new runs per tick or per frame. Measuring: the coverage dev action and, asked for first, `perf.py`.

## What it needs in code [A]

- `CompProperties_Radio` gains `hasBuiltInAntenna` (default true), so a station can have none; `RadioNet.StationEndpoints` then offers
  only the antennas on its coax, and its inspect text says it needs one.
- The terminal carries a `RadioNoiseExtension` in its own Def (`noiseWhen` PowerOn), so it joins the noise snapshot with no new code.
- One `ThingDef`, one `ResearchProjectDef`, the Keyed text; the Architect's Radio tab.

## Questions for the owner

1. The amplifier: **built into the terminal [A, recommended]**, or a separate building on the coax that either station can use?
2. The terminal's own noise: **on [A, recommended]**, or left out?
3. The figures above: as proposed, or changed?

## The art

Both stations' pictures are in one paid batch with the new whip and mast ([radio visuals](radio-visuals.md)): two tries each, the
owner picking one card each. The radio desk keeps the 2 × 2 picture; the terminal is drawn at 3 × 2 with its amplifier.
