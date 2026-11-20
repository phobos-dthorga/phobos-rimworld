# Radio framework rules

Moved from the repository's `AGENTS.md` on 18 Nov 2026 (owner's go-ahead), text unchanged; that file points here and keeps
the back-burner rule. The design is `docs/development/radio-framework-design.md`.

## Phases and the design

- **Phase 1 of the radio framework is open** (owner, 19 October 2026): local VHF/UHF on one map, as the
  design's roadmap lists it. Each later phase waits for its gate and an explicit owner go-ahead; the
  design's "refined during play" still applies to everything beyond the open phase. Research,
  verification and design records are welcome at any time.
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
