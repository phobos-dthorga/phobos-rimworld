# Radio framework rules

Moved from the repository's `AGENTS.md` on 18 Nov 2026 (owner's go-ahead), text unchanged; that file points here. **The radio is
open again** (owner, 23 Nov 2026), after its time on the back burner from 9 Nov 2026. The design is
`docs/development/radio-framework-design.md`; the pictures it was reopened for are `docs/development/radio-visuals.md`.

## Performance first

- **Performance is this mod's first concern** (owner, 23 Nov 2026, reopening the radio for its map pictures: "I can see these kinds
  of features becoming a bit of a performance hog if we don't treat them with due care", and "performance is our outmost concern
  for this mod"). Where a feature's look, detail or realism would cost frame time, the cheaper form wins, and one that cannot be
  made cheap is not built. **The four points below are the agent's reading of that, for the owner to revise.**
- **A design states its cost before it is built**: what it touches (cells, things, links), how often, and what makes it run
  again. A feature with no cost written in its design record is not built.
- **Nothing runs every tick or every frame for a whole map.** Work happens when something changes (a wall, a roof, an antenna, a
  noise source, a radio put on or taken off), is spread over ticks where it is large, and is kept until the next change. A picture
  that is switched off, or whose thing is not selected, costs nothing.
- **Measured, never assumed**: anything that draws or recomputes is recorded with `python scripts/perf.py` before and after, on
  the owner's colony (ask first, as for every recording), and its design record keeps the figures. The owner's PC is a best case
  for players.
- **A costly picture is the player's choice**: it starts switched off, and a coarser, cheaper form is offered where one exists.

## Phases and the design

- **Phase 1 of the radio framework is open** (owner, 19 October 2026): local VHF/UHF on one map, as the
  design's roadmap lists it. Each later phase waits for its gate and an explicit owner go-ahead; the
  design's "refined during play" still applies to everything beyond the open phase. Research,
  verification and design records are welcome at any time.
- **Phase 3 is open** (owner, 27 Nov 2026: "Open Phase 3 and start the jamming-as-noise slice"): links, security, microwave and
  electronic warfare. Its work follows `docs/development/electronic-warfare.md`, a slice at a time in that record's order, each slice
  on the owner's word; the record's radar simulation stays a sketch until the owner asks for its own record.
- The living design doc on claude.ai is the master copy; `docs/development/radio-framework-design.md` is
  a dated snapshot of it. Refresh the snapshot when the owner asks, keeping its text as written and its
  diagrams as Mermaid. Notes from play go in the living doc's "Notes from play" table. Decisions that
  answer the design's open questions are recorded in `docs/development/project-direction.md`.
- Reference figures the design marks as unverified stay unverified until a record shows a source
  (`docs/development/radio-reference-checks.md`); a Def may carry a figure with a secondary source if
  its description says "approximate", and never one with no source at all.

## Radio framework rules

- **One link-budget service decides every contact.** Plain, Operator and Engineer modes change only what
  the UI shows, never the outcome.
- **Decibels everywhere.** Gains and losses add in dB; distance follows the inverse-square law (doubling
  it costs 6 dB). Every figure in a Def names its source or says it is authored. The rules Def
  (`RadioRulesDef`) holds the starting values the design says to tune in play; player preferences
  (display mode, `metresPerCell`, the thought) are mod settings, never physics.
- **Channels are capacity, never physics** (owner, 24 Nov 2026: the channel ledger, after the BVR system's). The link budget
  decides every contact; a station's channels (25 kHz each, shared by radios taking turns) only decide which of those it can hear
  it serves. A new use of the radio (a caravan call, a data link) is a kind in the ledger with its own mode, never a second system
  of frequencies.
- **A link is two-way.** The link budget evaluates both directions and the worse one decides; the sheet
  names the limiting direction. Antenna height matters through ray clearance over walls
  (`docs/development/project-direction.md`); a figure the UI shows must affect an outcome somewhere.
- **Realism is a feel, not a hidden calculation.** Use the simplest model that produces the right
  behaviour, and make every rule visible to a player who wants to see it.
- **Physics sets the kit, the kit sets the play**: wavelength drives antenna size, which drives
  portability, power draw and siting. Don't hand out kit that physics says is impossible.

## Moved here from the root AGENTS.md (21 Nov 2026)

- **Worn apparel is never spawned.** A hand-held worn by a pawn has `Spawned == false`; find radios by
  enumerating `map.mapPawns` and `pawn.apparel.WornApparel` at recompute time, never through spawn hooks.
