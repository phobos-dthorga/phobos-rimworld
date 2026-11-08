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
- **A future framework idea** (owner, 22 Oct 2026): true gas mixing for rooms and ships, as Ostranauts has,
  built on Vanilla Expanded Framework with an extendable Phobos framework of its own. It goes ahead if other
  modders don't get there first, or fast enough. Not started.

## The owner's game (18 Oct 2026)

The living, refreshed list is [the owner's mod list](owner-mod-list.md); this section keeps its 18 Oct scope.
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
| 7 Nov 2026 | **Fewer, broader mods, merged before anything is published**: Burns and Smoke, Emergency Dosing, Triage, Medical Equipment and Medical Save Bridge become **D'thorga Medical**; Vehicle Patches and Missile Patches become **D'thorga Patches** ([mod consolidation](mod-consolidation.md)) | owner (on the agent's recommendation) |
| 7 Nov 2026 | **The save bridge joins D'thorga Medical** and asks on load: convert now, not now, or never for this save | owner (the agent had recommended keeping it separate) |
| 7 Nov 2026 | Factory Additions, Water Tap, Radio Framework and D'thorga Framework stay separate mods | owner (agreed with the agent's recommendation) |
| 7 Nov 2026 | Identities and versions of the merged mods: `PhobosDthorga.Medical` 0.6.0 and `PhobosDthorga.Patches` 0.7.0, continuing above the highest version merged in; every namespace, class, defName and saved key kept | agent default (version argued to the owner, who agreed with the plan as a whole) |
| 8 Nov 2026 | **RimWorld 1.7 only going forward**: no resources on 1.6 compatibility; new projects single-target, existing ones drop 1.6 when next touched (AGENTS.md) | owner (the "when next touched" part is an agent default) |
| 8 Nov 2026 | **Real vehicle systems as a generic, schema-driven service** (electrical, coolant and oil first; "this is just a start"), retrofitted to other authors' Vehicle Framework vehicles as settings, extensible by anyone through XML loadouts, documented ([design](vehicle-systems-design.md)) | owner (placement in D'thorga Framework's Vehicle-Framework-optional part is an agent default) |
| 8 Nov 2026 | **A new mod, D'thorga Vehicles** (`PhobosDthorga.Vehicles`), for utility vehicles on Vehicle Framework; created as a stand-in carrying loadouts for other mods' vehicles until the framework is built ([design](utility-vehicles-design.md)) | owner (on the agent's recommendation) |
| 8 Nov 2026 | The framework before any custom vehicle; the records before any code | owner |
| 8 Nov 2026 | Vehicle art through a pay-per-image API rather than a subscription or PixelLab for bodies; the Gemini image API first, the OpenAI image API second ([research](vehicle-art-services.md)) | agent recommendation; **owner chose the OpenAI image API** for vehicle bodies, with local generation for icons (9 Nov 2026); engine built 9 Nov 2026 ([how to run it](vehicle-art-services.md#running-the-openai-engine)); the rules for using it, learnt in Phobos Ostranauts, are in [paid image generation rules](paid-image-generation-rules.md) |
| 9 Nov 2026 | **The OpenAI image API for vehicle bodies; the owner's local generation for icons** ([research](vehicle-art-services.md)); PixelLab not used, its pixel art does not suit RimWorld | owner |
| 9 Nov 2026 | **Electric vehicles: framework systems plus an optional test vehicle** (Vanilla Vehicles Expanded's Lightning made electric); **one or more of our own utility vehicles will be diesel-electric**, for mechanics of their own | owner |
| 9 Nov 2026 | **Every vehicle of ours is fully upgradeable and extendable** by its own character, and its art is planned around its upgrade tree (bare body, an overlay per visible node); **parts come from providers by role**: Vehicle Framework's mechanics by default, RimThunder's and Vanilla Vehicles Expanded's part items as soft dependencies (together or as replacements), our own parts only where neither is present; **a fifth vehicle designed in full, the bulk tanker**, liquid or gas by upgrade and extensible by the fluid mods it finds ([design](utility-vehicles-design.md), principles 8 and 9) | owner (the trees, the role split, the fallback parts and the tanker's figures are the agent's) |
| 9 Nov 2026 | **Kerb mass: authored masses for Vanilla Vehicles Expanded** by real class, and empty mass tunable per vehicle ([record](vehicle-kerb-mass-check.md)); the tabs' wear gauges show what is left | owner |
| 9 Nov 2026 | **Caustic gas:** lasting **corneal scarring** of our own; **plants wilt** in dense gas, corpses unaffected; **supplied oxygen helps chemical lung injury**; contaminated clothing: **the shower is enough by default**, extra decontamination with Dubs Bad Hygiene, and **strip and rinse as a main-menu setting** ([record](caustic-gas.md)) | owner |
| 9 Nov 2026 | **Logging:** the game's 10,000-message cap is answered by **a bounded revive in D'thorga Framework, no Harmony** (on by default, a setting); our mods log through **their own categories with budgets**; the shared logger goes to **a new sibling repository** (proposed phobos-log); **no Rust inside the game**, Rust stays in the reader; the cross-game part is a schema and a reader ([record](logging-framework-design.md)) | owner (revive, no Rust, sibling repository, record only); agent (shapes, budgets, defaults) |
| 9 Nov 2026 | **Vehicle health: a condition layer on Vehicle Framework's parts, not a revived pawn health stack**; VF's part health stays the one authority; our Health tab replaces VF's; **NPC vehicles run a lite tier by default** (a setting runs the full model for all); later, **destroyed parts and serious conditions need supplies** behind a setting; performance budget 0.05 ms a tick ([record](vehicle-health-design.md)) | owner (architecture, scope, tiers, repair); agent (budget, roadmap) |
| 9 Nov 2026 | Vehicle follow-ups: a crafted **booster pack** with starts (no kit needed), **the world map's ground** from each biome's Defs, **cab moods** by each person's comfort range (a setting, on), a **RimHUD** value ([record](vehicle-crew-and-ground-design.md)) | owner's go; figures and defaults agent choices |
| 9 Nov 2026 | **Drop RimWorld 1.6 from every mod now**, not when each is next touched | owner |
| 9 Nov 2026 | Vehicle systems: jump-starting from another vehicle, and per-vehicle tuning in Vehicle Framework's own settings window | owner |
| 9 Nov 2026 | Tyres and track pads take chemfuel as a stand-in for rubber, beside steel and components | owner |
| 9 Nov 2026 | Drivetrain: a vehicle that cannot grip **crawls** (15% floor, "Wheels spinning"), never stops ([design](vehicle-drivetrain-design.md)) | owner |
| 9 Nov 2026 | Fuel use: **keep other authors' fuel figures**; a load-dependent fuel setting, off by default | owner (agent's recommendation) |
| 9 Nov 2026 | A jump jet too heavy or too hot to hover onto its pad: **a player setting** (use the runway, the default; allow at a cost to the engine; warn only) ([design](vehicle-turbine-design.md)) | owner (the three choices and the default are the agent's) |
| 9 Nov 2026 | Vehicles' water (coolant top-ups, the Harrier's injection water): **Dubs Bad Hygiene plumbing beside the parked vehicle when it is active, otherwise free**; coolant's glycol share is a little chemfuel | owner (agent's recommendation) |
| 9 Nov 2026 | **More engines and related systems where real counterparts have them**: thrust-lifted pods (Frog, Toad, Goliath), rotor drives for every helicopter, a diesel-electric for the Goat DE2, a boiler system for the steam locomotives, and real figures for the single-engine military vehicles, **the L-ATV first**; the boats, the Warbird and the Smuggler left out; built in parts ([power and lift](vehicle-power-and-lift-design.md)) | owner (on the agent's proposals) |
| 9 Nov 2026 | Steam: **boiler explosions allowed, off by default**, behind a main-menu setting with a warning; **the tender scaled down by default** (a tenth) with a realistic setting ([steam](vehicle-steam-design.md)) | owner (the tenth and the plugless 4-8-4 are the agent's) |
| 9 Nov 2026 | The L-ATV's engine at **the middle of the published figures**: 370 hp (276 kW) ([real figures](vehicle-real-figures-design.md)) | owner |
| 9 Nov 2026 | The world map: **a closed cab with its own temperature** (the game's cold, heat and comfort rules apply); **engine off at a caravan's night camp, with an optional fuel-burning night heater as a setting**; **the drivetrain's speed cap and the "will not move" refusals apply to caravans**, with a caravan jump-start; **a craftable jump-start kit** behind the setting "Jump-start leads must be crafted" (on) and "Every vehicle carries a booster pack" (off) ([world map](vehicle-world-map-design.md)) | owner (the cab's figures and the open VVE vehicles are the agent's) |
| 9 Nov 2026 | **Generic functions go to the Framework core**, in the same change as their first consumer (AGENTS.md) | owner |
| 9 Nov 2026 | **Two vehicle inspector tabs**: "Dashboard" (glanceable) and "Systems" (in depth, hidden in Plain mode); **a day of graph history, saved** (an exception to "nothing derived is saved"); RimHUD needs no hook ([tabs](vehicle-tabs-design.md)) | owner (the layouts are the agent's) |

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
- [x] Read Ludeon's official 1.7 Modder Primer (7 Nov 2026; findings in [the modding reference](rimworld-modding-reference.md#building-c-mods)).
- [ ] **The 6 dB gate**: 0.1.0 Draft was built and deployed on 19 Oct 2026; the owner's play test (the
      README's "What to try first") decides whether the rule feels right before Phase 2 opens. Every
      tuning value is in `Common/Defs/Radio/Rules.xml` and the patches.
