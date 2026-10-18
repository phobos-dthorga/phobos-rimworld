# Project direction

## Intent

- **Public RimWorld mods by Phobos A. D'thorga** (House D'thorga), made for the owner's own colonies and
  published free on the Steam Workshop for anyone to use and build on.
- **The first project is the [D'thorga Radio Framework](radio-framework-design.md)** (working title).
  It is an XML-extensible framework where a real link budget decides every contact. Phase 1 (local
  VHF/UHF on one map) is being built; later phases wait for their gates and the owner's go-ahead.
- **Frameworks that others can extend.** Hardware and tuning are Defs, so other modders add bands, radios
  and antennas in XML. Tie-ins with other mods are optional and switch on only when those mods are active.
- **Keep it a working slice.** Build the smallest version that proves the idea in play, then grow it. Drop
  an idea that isn't worth its upkeep.

## The owner's game (18 Oct 2026)

RimWorld 1.6.4871 on Steam's main branch, with all five DLCs and about 280 Workshop mods, managed with
RimSort. Combat Extended is in the list, so the owner is starting a new colony rather than continuing an
older save. 1.7 stays on the beta branch for now.

The design's optional tie-ins, checked against that list:

| Design tie-in | In the owner's list | `packageId` (as the list shows it) |
| --- | --- | --- |
| Combat Extended | Yes, with Armory, Guns, Armors and Charged Ammo | `CETeam.CombatExtended` |
| Vehicle Framework | Yes | `smashphil.vehicleframework` |
| Vanilla Outposts Expanded | Yes, with Additional Outposts and Power Grid | `vanillaexpanded.outposts` |
| Deep And Deeper | Yes; its last update (Sep 2025) declares 1.5 and 1.6 only | `shashlichnik.deepanddeeper` |
| RimPacts | Yes | `wowgag.rimpacts` |
| Empire Refactored | Yes | `matathias.empire` |
| IRBM (InterRim Ballistic Missile) | Yes | `kazepsi.irbm` |
| Vanilla Gravship Expanded | Yes, chapters 1 and 2 | `vanillaexpanded.gravship`, `vanillaexpanded.gravship2` |
| Rimatomics | Yes, with Rimatomics++ | `dubwise.rimatomics` |
| Other Dubs mods | Yes: Bad Hygiene, Central Heating, Rimkit and others | `dubwise.*` |
| Royalty, Biotech, Odyssey | Yes (DLCs) | `Ludeon.RimWorld.<DLC>` |
| **Surveillance Shift** | **No** | `ferny.SurveillanceShift` (from its GitHub `About.xml`) |

**Surveillance Shift** is on GitHub, but no Workshop page turned up in a search on 18 Oct 2026. Its
repository has **no licence file**, so a tie-in may call its public types at runtime but must never copy
its code or art. It declares 1.6 and 1.7 and needs Vanilla Expanded Framework.

Package IDs are case-insensitive. Check each one against the mod's own `About.xml` before using it in a
load folder or patch.

## Decisions

| Date | Decision | By |
| --- | --- | --- |
| 18 Oct 2026 | Repository `phobos-rimworld`, set up like Phobos Ostranauts: rules in `AGENTS.md`, docs, checks, changelogs | owner |
| 18 Oct 2026 | **Public repository and public mods**, published free on the Steam Workshop | owner |
| 18 Oct 2026 | The radio framework stays **design only** until the owner opens Phase 1; the living doc on claude.ai is the master copy, and this repository holds a dated snapshot | owner (in the design) |
| 18 Oct 2026 | The owner's handoff originals (the design export and its two PNG diagrams) are kept in ignored `.local/handoff/`; the repository copy uses Mermaid | agent default, following the Mermaid rule |
| 18 Oct 2026 | The owner's mod triage page is kept in ignored `.local/handoff/` and not published: it holds machine paths and personal settings. The tie-in table above carries what the designs need from it | agent default |
| 18 Oct 2026 | MIT for original work, as Phobos Ostranauts; Ludeon's content and other authors' material are excluded (`LICENSE`) | agent default, following Ostranauts |
| 18 Oct 2026 | `packageId` form `PhobosDthorga.<ModName>`; the Harmony ID is the `packageId` | **agent default, owner to confirm before the first public build** |
| 18 Oct 2026 | Layout: `mods/<Mod>/` is the shipped folder, `src/<Project>/` holds C#, `workshop/<Mod>/` holds page drafts once publishing starts | agent default, following Ostranauts |
| 18 Oct 2026 | Build for 1.6 first (net472, Harmony 2). Ship the 1.6 build to 1.7 through `LoadFolders.xml` until a native net10.0 build is needed | agent default, following the design's Versions decision |
| 18 Oct 2026 | **PolySharp** for every 1.6 (net472) project, set once in `Directory.Build.props`, so newer C# works on Unity's 2018-era libraries; proven by a throwaway probe build ([building](building.md#newer-c-on-rimworld-16)) | owner (agent proposed) |
| 18 Oct 2026 | Workshop publication tooling comes from Ostranauts in the change that prepares the first public beta, not before | agent default |
| 18 Oct 2026 | Direct pushes to `main`; no pull requests unless the owner asks | agent default, following Ostranauts |
| 19 Oct 2026 | **Phase 1 of the radio framework is open**: local VHF/UHF on one map (link-budget service, hand-helds, base station, antennas, feedline, walls and mountain, three display modes) | owner |
| 19 Oct 2026 | Identity: `packageId` **PhobosDthorga.RadioFramework**, assembly `PhobosRadio`, namespace `Phobos.Radio`, defName prefix `PhobosRadio_`, folder `mods/PhobosRadioFramework/`; the display name stays "D'thorga Radio Framework" as a working title | owner |
| 19 Oct 2026 | Phase 1 payoff: a testbed plus **"In touch"**, a small mood thought for a free colonist whose worn hand-held has a working two-way link to a base station; the station's Net tab lists who is in touch. Slaves are excluded and downed colonists included | owner (the payoff); agent default (slaves, downed) |
| 19 Oct 2026 | **Feedline is a Vanilla Expanded Framework pipe network**; VEF becomes the framework's one hard dependency (licence and limits in `THIRD_PARTY_NOTICES.md`) | owner |
| 19 Oct 2026 | **1 m per cell** as the `metresPerCell` setting, after the research in [map-scale-research.md](map-scale-research.md); the world tile scale waits for Phase 2 (starting point about 6.6 km per tile) | agent default after owner-requested research; owner to confirm |
| 19 Oct 2026 | **Antenna height works through ray clearance**: a wall crossed by the straight line between two antennas counts only where that line is below the rules Def's wall height (2.5 m), so a mast sees over walls and the last cells near a hand-held bite | agent default, owner to confirm |
| 19 Oct 2026 | Phase 1 ships one mode (FM voice at 12 dB) and one coax type; the weak-signal digital mode, a second coax and water-ingress degradation wait for later phases | agent default |
| 19 Oct 2026 | No Harmony reference until a feature needs a patch | agent default |
| 19 Oct 2026 | GitHub Actions builds and runs only the game-free physics tests, because VEF cannot be present there; the mod builds on the owner's machine | agent default, forced by the VEF licence |

## Open checks

- [x] The `packageId` form: confirmed as `PhobosDthorga.RadioFramework` (owner, 19 Oct 2026).
- [x] Map cell scale: 1 m per cell as a setting ([research](map-scale-research.md)); owner to confirm.
- [x] First bands: VHF and UHF, one hand-held each (19 Oct 2026).
- [x] Feedline: VEF pipes (owner, 19 Oct 2026).
- [x] Sources for voice SNR and amplifier efficiency: found, secondary ([reference checks](radio-reference-checks.md)).
- [ ] **Confirm the agent defaults of 19 Oct 2026**: ray clearance at 2.5 m, slaves excluded from "in touch",
      `metresPerCell` as a setting at all.
- [ ] **Settle the rest of the design's open questions** during play: world tile distance (Phase 2), console
      replace or upgrade (Phase 2), HF pace, direction-finding harshness, licence tiers, public name.
- [ ] **Decide whether licence tiers cap power by mode**, as the real Australian classes do. The
      [reference checks](radio-reference-checks.md) suggest it.
- [ ] Read Ludeon's official 1.7 Modder Primer before the 1.7 build.
- [ ] **The 6 dB gate**: 0.1.0 Draft was built and deployed on 19 Oct 2026; the owner's play test (the
      README's "What to try first") decides whether the rule feels right before Phase 2 opens. Every
      tuning value is in `Common/Defs/Radio/Rules.xml` and the patches.
