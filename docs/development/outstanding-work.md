# Outstanding work (from 7 Nov 2026)

Everything still open across the D'thorga mods, as the owner decided it on 7 November 2026, when they asked for every open item as a
question. **Keep this file current:** every change that starts, finishes or defers an item updates it in the same commit. In-game
testing is the owner's and is not listed here; each mod's changelog says what is tested, and the next session's short list is
[the in-game test pass](in-game-test-pass.md).

**Status key:** open, in progress, done (with the version), deferred (with the reason).

**Done rows move to [Done](#done)** at the end of this file, unchanged, in a table for their area (17 Nov 2026: 100 rows moved, 50
left open above, 7 deferred). A row moves when its status says done, built or fixed with nothing left open.

## This round, in order

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| J | Bug reports finished for the owner to post: RimPacts, Harrier (two), More Injuries and MSE2 (two), IRBM (after the owner's test confirms the cause) | docs | Finish all | Done except IRBM: RimPacts and Harrier (two) in [RimPacts patches](rimpacts-patches.md#sorties-blocked-by-rimpacts-battles-and-the-parts-list-7-nov-2026-evening), More Injuries and MSE2 in [the save bridge](medical-save-bridge.md); IRBM held for the owner's test |
| J3 | **Remove the Vehicle Framework fires part** once the framework puts attached fires out on despawn and keeps one per vehicle; post the draft report first | Patches | Owner, 10 Nov 2026 | Waiting on the author: Patches 0.16.6; removal steps and the draft report in [the record](vehicle-framework-fires.md) |
| J4 | **Remove the empty ammo entries guard** once The Dead Man's Switch - Synthriada defines its Embryte ammo or drops the entries; it stands down by itself until then | Patches | Owner, 10 Nov 2026 | Waiting on the author: Patches 0.16.7; see row 13 of [the register](temporary-patches.md) |
| J5 | **Owner to judge: a recipe picker for machines with very long bill lists.** The automated ammunition press lists about 500 recipes in the game's plain pop-up menu. Proposed: from about 40 recipes up, the game's own search box and one column folded by calibre, grouped from what each recipe makes; clicking runs Pipe System's own add-bill action. Would be Factory Additions 0.13.0, design record first | Factory Additions | Agent's proposal, 10 Nov 2026; **not now** (owner, 12 Nov 2026) | Deferred: not built; raise again only if the owner mentions long bill menus. Check 25 still waits on a look at a Dead Man's Switch press |
| J7 | **Fix the faults the fault-read of 17 Nov 2026 found** in the untested vehicle work: 36 findings, among them the Harrier never brought down by air defence under Combat Extended, a bowser destroyed at a north or east edge, a stopped autonomous turret that never fires again ([record](fault-read-2026-11-17.md)) | Framework | All now, one version (owner, 17 Nov 2026) | **Fixed: Framework 0.120.5**, not yet tested in game; a report for Vehicle Framework's author drafted (K7) |
| J6 | **Owner to choose: the Harrier cannot be disassembled.** With a colonist the work stops near the end and the jet stays; in god mode each try logs a graphic error from the main-thread check in `VehiclePawn.GenerateGraphic`. Reading the code suggests the destroy step stops partway; the cause is not found. Choice: a temporary diagnostic in D'thorga Patches that logs each stage of the destroy step (recommended), or only a report for the owner to post. Re-decompile the installed Harrier framework first: our copy is from 3 Nov, the mod updated 9 Nov | Patches | Diagnostic first (owner, 12 Nov 2026) | Diagnostic built: Patches 0.18.0, waiting on one try in game (check 62 in the test pass; [record](vehicle-patches.md#the-harrier-cannot-be-taken-apart-a-trace-0180-12-nov-2026)). Avoid repeat attempts on the jet: each runs the first half of the destroy step |

## The owner's reports of 7 November 2026 (evening)

All its rows are done: see [Done: The owner's reports of 7 November 2026 (evening)](#done-the-owners-reports-of-7-november-2026-evening) at the end of this file.

## Vehicles (from 8 Nov 2026)

Added 8 Nov 2026: the vehicle work of 8 November did not update this file as it went. Records:
[vehicle systems](vehicle-systems-design.md), [utility vehicles](utility-vehicles-design.md), and the dated records they link.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| V4 | Vehicle body art through OpenAI's image API (the `openai` engine of `scripts/art-run.py`, built 8 Nov 2026 under [the paid rules](paid-image-generation-rules.md)); icons on the owner's local model | art | OpenAI for bodies, local for icons (8 Nov 2026); **the stand-in icons are good enough: replace them only when little else is left** (8 Nov 2026) | Open: the engine is built and the key accepted; the tanker's pilot is made (four north bodies, US$0.118, [record](../../assets/phobos-vehicles/README.md)); the owner picked attempt 2 and a 2 x 6 footprint (8 Nov 2026); the body's facings, masks and icon are exported by script; the owner accepted all twelve overlays as defaults and variants (8 Nov 2026); icons low priority. Round three (12 Nov 2026): the trailer's own body, its boards, the tipper's grab and the winch and long-range tank, US$0.62 ([round three](../../assets/phobos-vehicles/requests/round-three-record.md)) |
| V6 | The M1A2's APU upgrade node; "drop the stores" for a jet too heavy to land vertically | Vehicles, Patches | Proposed | Deferred: the APU needs a patch to RimThunder's upgrade trees. The landing check at a journey's end is built (Framework 0.140.0, a rolling landing where it cannot hover); dropping stores waits for an aircraft with external stores |
| V10 | The boats, the Warbird and the Smuggler keep one engine | Vehicles | Agreed to leave out | Deferred |
| V15 | Every vehicle of ours upgradeable and extendable from its first Def, with the art planned around its upgrade tree (bare body, one overlay set per visible node); parts by role from Vehicle Framework's mechanics, RimThunder's and Vanilla Vehicles Expanded's items as soft dependencies, our own fallback parts only where neither is present; the tanker's contents Def and ports ([design](utility-vehicles-design.md), principles 8 and 9) | Vehicles, Framework | Directions given (8 Nov 2026); the trees, the role split and the fallback parts are agent proposals | Built in parts, not yet tested in game: every vehicle of ours has an upgrade tree (the submarine test hull stays bare, owner, 11 Nov 2026); fittings, tunes and part grades (Framework 0.40.0 onward); our own parts with other mods' as substitutes; parts bills by role (Framework 0.95.0, Vehicles 0.41.0, [record](parts-bills.md)); overlays per visible node for our bodies, picked 11 to 12 Nov 2026 |

## Vehicles, 11 to 12 Nov 2026

Added 12 Nov 2026: the vehicle work of 10 to 12 November had not updated this file as it went. Nothing in this section is tested in
game unless the row says so; the test rows are in [the in-game test pass](in-game-test-pass.md), and the round of 12 Nov 2026 is listed
at its top.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| W2 | Sling slice 2: weather grounds a lift, a slower and thirstier slung flight, choosing where the load comes down, emergency release on the world map, a load dropped in a crash, and **wrecks a caravan recovers** | Framework 0.101.0 | Wreck site for a load dropped on the world map (owner, 12 Nov 2026) | Built, not yet tested in game. **Set aside as an agent choice:** our own selectable stand-in with the hover over the load and mid-air orders, too risky against Vehicle Framework's shared launch; raise it if Vehicle Framework ships a selectable stand-in or the owner asks |
| W5 | The hitch, slices 3 and 4: towed mass on the Dashboard, "can this pull that?", loading while hitched, the world map and caravans; then the special trailers, the recovery tow and road trains | Framework, Vehicles | Planned in the design | Slice 4's recovery tow **built** with the recovery family (Framework 0.103.0, not yet tested in game). Slice 3 **built** (Framework 0.104.0, not yet tested in game): what holds a tow back on the panels, "can this pull that?" beside the mouse, loading while hitched (already worked), trailers on journeys. Open: the special trailers and road trains |
| W8 | Trains of the Rim: a steam tree of ours | Vehicles | Skipped while the mod looks dormant (owner, 12 Nov 2026) | Deferred: raise if the mod is updated for 1.7 |

## Cross-links round three (13 Nov 2026)

The owner asked for a research and record review of deeper cross-links for every vehicle, with aircraft avionics as the gap seen
most, and for a contract other mods' authors can use in XML. Three records, written before anything was built; what has been built
since is in each row.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| X1 | [The authors' override contract](vehicle-author-overrides.md) | Framework | One override Def, layered, a tidy of names before the freeze, a tick per mod (owner, 13 Nov 2026) | Record written; not built |
| X2 | [Avionics for every aircraft](vehicle-avionics.md): A1 to A17, L1 to L6 | Framework, Patches | All four areas; our conditions may scale other authors' figures through non-skipping hooks | Record written; the owner's five answers recorded (13 Nov 2026). Built from it: A5, A6, A8, A9 (Framework 0.106.0, not yet tested in game). **Built 13 Nov 2026 (Framework 0.110.0, Vehicles 0.53.0, not yet tested in game):** the avionics system and fit, the appended avionics bay, A1 with its warn-only setting, A2, A7 on the servo aircraft and the Harrier, L1; checks 99 to 103. **L5 set aside**: a piston aircraft needs its own engine model first. **Built 17 Nov 2026, the aircraft batch (Framework 0.119.0, Vehicles 0.58.0, Patches 0.21.0, not yet tested in game; checks 139 to 146):** A14 with L3 and the owner's hypoxia setting (off by default, a short condition of our own), A16 countermeasures (automatic on the world map, plus a Harrier button), A12 the Harrier's evasion |
| X3 | [Cross-links, round three](vehicle-crosslinks.md#vehicle-cross-links-round-three-the-crew-weather-and-ground-combat-damage-electrics-and-hydraulics-13-nov-2026): G1 to G20, round two's rows carried forward, a short list of ten | Framework, Vehicles | The four ground areas (the crew, weather and ground, combat damage, electrics and hydraulics) | Record written; the owner's answers recorded (13 Nov 2026). Built from it: T6, G9, G15, G16, G18 (Framework 0.106.0, not yet tested in game). **Built 16 Nov 2026 (Framework 0.113.0, Vehicles 0.56.0, not yet tested in game):** G1 with G2 (the driver system), G5 (weather on grip, a setting), G7 (fording and the snorkel fitting), G13 (cook-off where Combat Extended's ammunition is on, a setting). The snorkel's icon made and picked the same day. **Built 17 Nov 2026 at the owner's picks (Framework 0.123.0, Vehicles 0.62.0, not yet tested in game; rows 158 to 164):** G6, G8 with F8, G11, G12, G17 (power steering), round two's T4 with F3, T8, F2, F4, F5, F7 and the boost axis, and G1 and G9 for raiders; G3 found to be Vehicle Framework's already. **Held by the owner:** engine braking (T5 with F6). **S3 built (Framework 0.124.0, not yet tested in game; row 165):** head machining and the hot-section overhaul wait for a tuning rig. The new fittings' pictures are stand-ins until the next local art run |
| X5 | Fault: the Warbird and Smuggler get the generic turbofan loadout | Vehicles | — | Open: avionics L5 |
| X6 | Fault: stale Harrier part figures in the Patches README and a probably stale hit-share note | Patches | — | Part fixed: the Patches README's Harrier part figures corrected in Patches 0.19.0. **The hit-share note read and corrected 17 Nov 2026** (it was stale: the Harrier's build of 16 Nov decides hits itself). **Open, the owner's choice:** that rule shares hits evenly among parts, so our ten added parts leave the jet's own engine and wing tanks struck about a quarter as often as its author intended ([avionics L6](vehicle-avionics.md#l6-read-our-ten-parts-thin-out-hits-on-the-harriers-own-17-nov-2026)) |

## The owner's report of 16 Nov 2026: allies slow to destroy vehicles

All its rows are done: see [Done: The owner's report of 16 Nov 2026: allies slow to destroy vehicles](#done-the-owners-report-of-16-nov-2026-allies-slow-to-destroy-vehicles) at the end of this file.

## Knocked out, not destroyed (16 Nov 2026)

The owner asked for a research and record round on vehicles losing a job before they are wrecks: turrets and weapons, mobility,
working equipment, crew and cab ([record](vehicle-knocked-out-systems.md)). Written before anything was built; what has been built
since is in each row.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| KO1 | The record: rows T1 to T11, M1 to M5, W1 to W6, C1 to C6, with a short list | docs | All four areas, record only; then (16 Nov 2026) hand-cranked turrets, damage only for raiders, the turret blown off with cook-off, AI crews bail out and the player's get an alert, a weapon stops with its mount under 10 %, and the short list queued after the aircraft batch, the vaccines tie-in and Billy | Written and answered. **Built 17 Nov 2026 at the owner's word ("proceed with steps 1 through 6"): Framework 0.115.0**, the whole short list: T1, T9, T10; M1, M2; C2; T2, C4; T3, T4; W4, W1. Not yet tested in game (checks 120 to 125). **Then (17 Nov 2026), Framework 0.116.0 and Vehicles 0.57.0:** T8, M3, W2, W3 (not yet tested in game, checks 126 to 129). **T5 and T6 built with the aircraft batch (Framework 0.119.0, check 146).** Waiting: C3 with hotboxing; M4 read 17 Nov 2026 (row KO4) |
| KO2 | ~~Fault: the people aboard our own vehicles can never be hit~~ | Vehicles | — | **Withdrawn, the agent's error** (16 Nov 2026): its script did not read seats' hit areas; every seat has one. Nothing to fix |
| KO4 | To read before building: field fixes for tracks and wheels (M3), vehicle lords with an immobilised vehicle (M4), the Crawler's instruments as loads (W2), the tanker's pump (W3), real turret drives beyond the Bradley (T4) | docs | — | M3, W2 and W3 read when built. **M4 read 17 Nov 2026**: in a raid Vehicle Framework's own orders empty a vehicle that cannot move, guns working or not, so it is a pillbox only at a base; C2 is narrower than recorded and check 122 cannot tell the two apart ([the reading](vehicle-knocked-out-systems.md#m4-read-what-its-orders-do-with-a-vehicle-that-cannot-move-17-nov-2026)). **Decided (owner, 19 Nov 2026): any faction's crew stays aboard a vehicle that cannot move while a weapon of its still works** ([militarised service vehicles](vehicle-militarised-service-vehicles.md)); not built: it needs a step in Vehicle Framework's raid orders or a patch, and our own bail-out rule (C2) read again against it. Still to read: real turret drives beyond the Bradley (T4) |

## Tie-ins with other mods (13 Nov 2026)

The owner asked for an exploration of cross-links through soft dependencies with other mods: all our mods, the owner's mod list
first, both directions, InterRim Ballistic Missile and The Dead Man's Switch for their air defence; **record only**, the owner picks.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| TI1 | [The tie-in register](tie-in-register.md) | All | — | Written; kept current by every tie-in change |
| TI2 | [Soft-dependency cross-links](soft-dependency-crosslinks.md): SD1 to SD44, earlier proposals carried forward, a ranked short list | Framework, Vehicles, Medical, Factory Additions | Scope as above (13 Nov 2026). **The next batch** (owner, 13 Nov 2026): SD4, SD5, SD9 (vehicles); SD30 (factory); SD21, SD22, SD23 (medical); SD8 (gases) | Record written. **Built 13 Nov 2026** (not yet tested in game): SD9 (Framework 0.109.0, Vehicles 0.52.0), SD5, SD4, SD21 (Framework 0.109.0, Medical 0.16.0), SD23 (Medical 0.16.0), SD30 narrowed to our parts bench (Factory Additions 0.13.0). SD8 set aside: nothing to build. Checks 91 to 97. **SD22 was left out without a note** (found 16 Nov 2026). **Owner, 16 Nov 2026:** build SD22 next, then the aircraft batch (TI3); build SD20 anyway; decompile and record SD6; check the mods not on the list. **SD22 built: Medical 0.17.0** (not yet tested in game; check 117): Search and Rescue's responders use our kits as two lanes in its field rescue work type, no Harmony; the litter-vehicle half set aside (its evacuation list is internal), with a draft request for its author. **Later the same day:** SD6 read, then **not now** (owner); the mods not on the list checked, Celsius and Pharmacist included (found by name; [record](soft-dependency-crosslinks.md)) |
| TI3 | [Air-defence tie-ins](air-defence-tie-ins.md): AD1 to AD14; avionics A13 and A16 revisited | Framework, Patches | IRBM and DMS in scope (13 Nov 2026). **Our aircraft become DMS targets, with countermeasures** (owner, 13 Nov 2026): AD3 with AD2 and avionics A16. Cards of 17 Nov 2026: down only if unflyable; automatic countermeasures plus a Harrier button; A12, the launch warning and AD7 join | **Built 17 Nov 2026: Framework 0.119.0, Vehicles 0.58.0, Patches 0.21.0**, not yet tested in game (checks 139 to 146): AD3, AD2, AD7 and the launch warning. The cartridge's picture picked by the owner the same day. **Read 17 Nov 2026:** VTOL Raids spawns its aircraft at the map's edge already flying, never on the world map, so neither DMS's nor IRBM's air defences can ever engage them, and a closed airspace does not stop them; only unroofed ordinary guns can. **Still open, the owner's choice:** AD5, with its facts read (today a heat seeker with no hot room to chase goes for the biggest vehicle, cold or not: usually one of our work trucks) and two shapes offered ([the reading](air-defence-tie-ins.md#read-17-nov-2026-vtol-raids-and-the-heat-seeker)) |

## Engine loads (18 Nov 2026)

The owner asked for a research and record round on a deeper simulation layer, where what the engine drives (the alternator, the
water pump and more) takes power from it and so chemfuel, with a top-down look at the vehicle simulation first
([record](vehicle-engine-loads.md)). **Nothing built**; the owner picks.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| EL1 | The record: where the simulation stands, the seams, one ledger of engine loads, the chains, sizes worked from the model | docs | The whole fuel model on by default; drag kept as one fixed figure; work loads on the ledger with re-derived figures (owner, 18 Nov 2026) | Written; not built |
| EL2 | The step back, S1 to S8: one engine state, one fuel service, one unit, lost power as power, figures to the rules Def, displays reading one fuel figure, bleed air in the same shape, the performance overrun closed | Framework | **Go** (owner, 18 Nov 2026); lost power as power; record before installing | **Built: Framework 0.130.0**, not yet tested in game (checks 182, 183): S1 to S6, and S8's three hot spots from the owner's recording; S7 in 0.132.0. **Measured after (18 Nov 2026):** D'thorga Framework's share of ticking fell from 1.3 % to 0.3 % |
| EL10 | The profiler could not split the game loop in the owner's recording of 18 Nov 2026 (every frame under `RimWorldWaylith.Program.Main`, "the simulation hardly ran"), though it matched 99.2 % on the same build the day before | phobos-modkit | — | Fixed in phobos-modkit 1.14.0 (18 Nov 2026): Harmony 3 names a patched method Type.Method_PatchN, which the phases never matched; the same recording now names all but 2 % of the main thread |
| EL3 | The loads: the ledger with the alternator and the fuel default; the fan and air conditioner; the water pump and belt slip; compressor and steering pump; work loads | Framework | Everything at once (owner, 18 Nov 2026) | **Built: Framework 0.131.0**, not yet tested in game (checks 184 to 186). Open: turbine bleed air (S7). **To watch in play:** the drill rig near 100 chemfuel a day drilling (40 before), and the air conditioner's cost on small cars |
| EL11 | **Altitude**: thin air taking engine power, turbine output, rotor lift and radiator cooling, from each tile's elevation ([record](vehicle-altitude-and-turbines.md)) | Framework | Built with the turbine slice (owner, 18 Nov 2026) | Built in Framework 0.132.0; not yet tested in game (row 188). A flight's cruising height **built in Framework 0.141.0** ([record](vehicle-flight-over-the-map.md#built-18-nov-2026-the-cruising-height-and-phases-that-follow-the-hover)), not yet tested in game (row 199). Not built: other factions and the light model, water boiling lower, brake cooling |
| EL12 | **Turbines catch up with piston engines** ([record](vehicle-altitude-and-turbines.md)): T-1, T-2 with S7, T-4, and T-3, aircraft fuel following the engines between tiles and on a map | Framework | Built now, not after testing; fuel on a map as well as between tiles (owner, 18 Nov 2026) | Built in Framework 0.132.0, Vehicles 0.65.2; not yet tested in game (row 187). To watch: how thirsty idling on the ground feels |
| EL4 | The generator set's fuel: re-derived from a real generator head it would burn 72 a day at full output against 15 today | Vehicles | **Keep today's figure and record the real one** (owner, 18 Nov 2026) | Done: recorded |
| EL5 | Idle fuel halves with the fuel model on (about a quarter of the cruise rate, against Vehicle Framework's half) | Framework | **Accepted** (owner, 18 Nov 2026) | Done: recorded |
| EL6 | To confirm before building, C1 to C7 | Framework | Confirm first (owner, 18 Nov 2026) | **Done 18 Nov 2026** ([the results](vehicle-engine-loads.md#what-the-checks-found-18-nov-2026)): three faults (C1 lamps and the night heater run on a flat battery with the engine off; C4 the diesel-electric's alternator gives nothing at idle; C6 the tanker's pump costs nothing above idling), two missing links, one simplification, one not a fault. Read from code, not seen in game |
| EL7 | To source before any Def carries them: a water pump's shaft power, the alternator's efficiency, real loads for the tanker's pump and the drill rig | docs | — | **Part done 18 Nov 2026:** alternator sourced (Remy: 55 % typical, 54 to 60.5 % measured; the agent recommends 0.55 over today's 0.6); accessory loads measured on a city bus (University of Minnesota); the drill rig's load taken from its existing engine demand. **Still open:** a water pump figure from a page actually read, and a litres figure for the tanker's pump rate |
| EL9 | Fix the three faults the checks found | Framework, Vehicles | C1 and C4 now, C6 with the ledger's work loads; the ledger's alternator at 0.55 falling with speed; the step back next (owner, 18 Nov 2026) | **C1 and C4 fixed: Framework 0.129.1, Vehicles 0.65.1**, not yet tested in game (check 181). C6 waits for EL3 |
| EL8 | Records to bring up to date: [Vehicle Framework mechanics](vehicle-framework-mechanics.md) against its 1.7 build (five points), a design record for the grid hook-up of Framework 0.120.0, one set of battery and alternator figures across records | docs | — | Open |

## World elevation and descents (18 Nov 2026)

The owner asked how slopes could be simulated, after engine braking came up beside the engine loads, and for research and a record
before anything is decided; the answer also bears on the radio mod ([record](world-elevation-and-descents.md)). **Nothing built or
decided**; the owner picks.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| WE1 | The record: what a tile holds, slopes measured on the owner's world, real descents and engine brakes, altitude, radio line of sight | docs | Research and record first (owner, 18 Nov 2026) | Written |
| WE2 | **How big a world tile is: about 30 km** (owner, 18 Nov 2026, the agent's recommendation). Three anchors: 6.6 to 9 km (a walker), 29 km (our vehicles' own speed), 31.5 km (an Earth-sized planet, as Atmosphere Overhaul assumes). It sets radio's reach and the vehicles' net slope | Radio, Framework | — | Decided; nothing reads it in code yet |
| WE3 | Descents (**held until after the ledger**, owner, 18 Nov 2026). Whether they are wanted; hills up and down, net slope from elevation, or both; brake wear only or a brake temperature with fade. Engine braking (cross-links T5 with F6) stays held until then | Framework | — | Open, the owner's choice |
| WE4 | Altitude: thinner air taking engine power, rotor lift and cooling | Framework | Built with the turbine slice (owner, 18 Nov 2026) | Built as EL11 (Framework 0.132.0); not yet tested in game |
| WE5 | Gaps D1 to D5, read from code: no wear per stop on the world map; the heavy-duty brakes' descent term never built; rails get the hill grade; Vehicle Framework counts no hills on a road and our grade does; a leg reads the tile being left | Framework | The safe four with the ledger (owner, 18 Nov 2026) | **D2 to D5 fixed: Framework 0.131.0**, not yet tested in game. **Open, the owner's choice:** D1, wear per leg on the world map, which would wear caravans' brakes and clutches faster than today |
| WE6 | Radio: line of sight from elevation, hilltop sites and repeaters, the tile's size | Radio | On the back burner | Recorded for when the owner picks radio up again; nothing to do now |
| WE7 | A tool for it: the slope measurement was a scratch script over the save index. Worth adding to phobos-modkit's save tool (elevation, hilliness and neighbour differences) if elevation is taken up | phobos-modkit | — | Built in phobos-modkit 1.14.0 (18 Nov 2026): `python scripts/query-save.py terrain`, with `--near` a place |

## Modern military vehicles (18 Nov 2026)

The owner asked for modern military aircraft and vehicles to build, the V-22 Osprey for certain, and answered the record's
questions the same day ([record](modern-military-vehicles.md)). **Nothing built**; each row waits for the owner's word.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| MV1 | Our own flight over the map: one design for hovering and circling, the hover half built first; every aircraft of ours stands on it. With it, at the owner's word: a rule for leaving the ground rolling, flight phases from our own flight, and a cruising height between tiles | Framework | Our own route, learning from VTOL Vehicle and the Harrier's code without building on either (owner, 18 Nov 2026) | **Record written** (18 Nov 2026): [our own flight over the map](vehicle-flight-over-the-map.md). Not built. **The owner answered its four questions the same day:** stock helicopters fly on it behind a setting that starts off; eight headings; people slowed beneath a hover, with a setting; real consequences for running dry. The dev-only proof was dropped at the owner's word (a good portion built, with proper art, before any test), and **its questions were read from code instead (18 Nov 2026)**: a postfix on Vehicle Framework's draw position replaces the height offset, which cannot work; the launch button, right-click orders and arrival each need a patch the first design lacked; one guard for boarding, loading and work is placed. About twelve new patches where the first count was five, still none on a method every pawn calls **Built 18 Nov 2026 at the owner's word ("proceed with both cruising height and engine phases"): Framework 0.141.0 and Vehicles 0.66.0**, not yet tested in game (rows 199, 200): the cruising height between tiles, whole, and phases that follow the hover figure (the Hind's and Mongoose's on station and climbing away). The phase reader for our own flight, the forward-flight and rolling shares and the three thresholds wait for the hover half, since only our flight reads them **The owner's answers of 19 Nov 2026 built in Framework 0.142.0** (not yet tested in game): a cabin oxygen fitting for passengers, a twin that loses an engine in flight flies on, turns back or struggles across, the oxygen line kept at 3,810 m and the no-drag rule's second exception **The hover half built 19 Nov 2026 at the owner's word: Framework 0.147.0, Vehicles 0.71.0** (not yet tested in game, rows 204 and 205): take off, fly to, hold, land here, the three thresholds, forced landings, the guards, the phase reader; the Osprey flies on it. **Slice 3, leaving and arriving in flight, built 19 Nov 2026: Framework 0.149.0** (not yet tested in game, row 206). **Slice 4, leaving the ground rolling, built 19 Nov 2026: Framework 0.151.0, Vehicles 0.73.0** (not yet tested in game, row 207). **Slice 5, circling, built 19 Nov 2026: Framework 0.153.0, Vehicles 0.75.0** (not yet tested in game, row 208); aeroplanes, side-firing arcs and runs wait for an aircraft that needs them. **Slice 6's ropes and hoist built 19 Nov 2026: Framework 0.155.0, Vehicles 0.77.0** (not yet tested in game, row 209). Next: a raider's flight (slice 6's other half) |
| MV2 | Armour for armed vehicles: game-scale figures in the Def, millimetres and megapascals in a patch that loads only with Combat Extended | Vehicles | Combat Extended first, Vehicle Framework the fallback (owner, 18 Nov 2026) | **Record written** (18 Nov 2026): [armour for our armed vehicles](vehicle-armour.md): classes named by the threat stopped, one outer part to a cell, a wider hit box under Combat Extended, a gate check. Not built. **Decided (owner, 18 Nov 2026):** an aircraft in the air is shot at and shoots as from the ground in the first build; real height under Combat Extended may follow as its own slice **Built for the five service trucks (owner, 19 Nov 2026): Framework 0.154.0, Vehicles 0.76.0** (not yet tested in game, row 222): every part Skin, one outer part to a cell, a Combat Extended patch per truck, the wider target through Combat Extended's own list, the class on the Health tab and `scripts/check-vehicle-armour.py`. The Osprey's classes are its session's next slice; the trailers and the Crawler are not built |
| MV3 | The V-22 Osprey, under its real name: two ways up, one engine out, hook, cabin fits with the tanker kit, its real guns | Vehicles | Wanted; real proportions anchored to the Hind's speed (owner, 18 Nov 2026) | **Record written** (18 Nov 2026): [the V-22 Osprey](vehicle-osprey.md): real kilograms, a 5 x 9 footprint, engines and lift worked from published figures, parts and armour, guns, a seventeen-node upgrade tree, the art and sound plan, and the first batch built before any test (owner, 18 Nov 2026). Not built; no picture made. **The owner answered its eight questions the same day:** the Mi-38's scale, about 30 cells across, as the hero vehicle (against the agent's recommendation of the Hind's scale); 5,600 kg of cargo; two pilots needed; a 1,500 tank burning 200 a day; the twelve first-batch upgrades; what it lands on matters, with the inlet barrier; research at 6,000 after turbines. **The body is picked (owner, 18 Nov 2026): Nano Banana Pro through fal**, from a four-service trial ([record](../../assets/phobos-vehicles/requests/osprey-trial-record.md)); tilting nacelles wanted, with conversion taking real time in play. Before that: the body was tried on several services through the new fal engine (owner, 18 Nov 2026: another service than ChatGPT for the look), one picture each, with a dry run and a spending limit first. (An earlier note here said one paid picture was too small at this size; that was the agent's error. The OpenAI engine makes one large enough.) The Hind's real cruising speed is sourced (270 km/h), so its published 463 to 493 km/h comes to about 17 to 18 in game |
| MV4 | The crewless fit: follow a leader (four followers at most) or run a loop, the vehicle staying optionally crewed | Framework | All three ways of working without a crew; seat kept; four followers (owner, 18 Nov 2026) | Open |
| MV5 | A vehicle in a hold: the sling's holder with a ramp and a size limit | Framework | Only if feasible (owner, 18 Nov 2026) | Read done: feasible with limits; waits for a host aircraft and passes on test rows 57 and 70 |
| MV6 | Mechanitor control of a crewless vehicle through the game's bandwidth | Framework | Read first; then charged to the used figure, so mechanoids keep priority (owner, 18 Nov 2026) | Open: read done, feasible with limits; nothing designed |
| MV7 | The other candidates: cargo helicopter, line-charge trailer (raiders may use it behind a setting that starts off), crewless ground carrier, control van, transport aeroplane and its gun kit, mobile air defence, rocket truck (loaded maps only), scout drone, wingman, protected truck | Vehicles | Real names (owner, 18 Nov 2026) | Record only; the order is the agent's recommendation |
| MV8 | Militarised service vehicles: armour kits, a gun ring with a choice of three guns, smoke dischargers and a ram bar for the five service trucks, the Talus first | Framework, Vehicles | A choice of mounts; weight counts like cargo; an armoured truck may keep working while enemies are about, behind a switch; our own guns plus optional extras; no hard dependency (owner, 19 Nov 2026) | **Record written** (19 Nov 2026): [militarised service vehicles](vehicle-militarised-service-vehicles.md). Nothing in code and no picture drawn. It waits on MV2, the armour base work for the trucks. **The owner answered its eight questions the same day:** weight on the engine everywhere and cargo room on the Talus only; real kilograms; other factions later, behind a setting that starts off; EMP hardening as its own kit; a remote weapon station that fires with nobody aboard (so MV4's crewless trucks may be armed); guns by role, confirmed truck by truck (Talus all four mounts; Kelly the three hand-swung guns; Ullage the machine gun; Busbar the machine gun or the remote station; Datum none); any faction's stranded crew stays and fights; protection first |
| MV9 | Turbine engine upgrades for our vehicles where a real machine has one, the Busbar generator set first | Framework, Vehicles | Wanted, down the line (owner, 19 Nov 2026: "It'd be great for our generator vehicle") | Noted, not designed. Turbine drivetrains, the turbine system, its part and its research already exist; the one missing capability is a drivetrain chosen by an upgrade node. What it changes for MV8 is in [that record](vehicle-militarised-service-vehicles.md#beside-this-turbine-engines-owner-19-nov-2026) |

### Faults found by the code reads of 18 Nov 2026

Read in code while answering the owner's questions; none seen in game. The owner chose to fix all six (18 Nov 2026); five are in [Done](#done-modern-military-vehicles-18-nov-2026) below, a sixth needed no change, and the fixing found a seventh.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| CR7 | **About forty other places read a vehicle's cells the game's way** (`OccupiedRect()` on a vehicle). Found while fixing CR1 | Framework | — | **Read 18 Nov 2026, one by one: the premise was mostly wrong.** Vehicle Framework itself patches `GenAdj.OccupiedRect(Thing)` to answer with its own footprint, so forty of the forty-six sites that can receive a vehicle are already right on the four main facings. **Six are not, and one related fault was found:** a load dropped on a wreck map is checked for room facing north and then spawned at a random facing, which can put it off the map and destroy it (`SlingWreck.PlaceOn`); a trailer's hidden water tower and septic tank are placed by turning an offset about the centre (safe for the shipped cells, a latent wipe for others); the ring where a vehicle looks for plumbing is a cell out on one side (`PlumbingFeed.FeedCells`); set-out pieces stand a cell forward facing south or west; four "is a vehicle on this cell" checks miss a vehicle parked at 45 degrees; depot parking and the headlight cell are a cell out (cosmetic). Both an even length and an even width show it. **Proposed, not built:** one helper, `VehicleCells`, with two game-free rules and a wider test; a self-test step that proves in game that the three footprints agree. **For flight over the map:** a flying aircraft must not count as standing on the cells beneath it for the loading chute, depot bays and set-up, and the pipes laid beneath a parked vehicle must not trail behind one in the air. The owner has not been asked which to fix now |
| CR8 | **A submarine destroyed while it is under takes its crew with it** (read in code 18 Nov 2026, inferred from the order of calls; not seen in game): Vehicle Framework puts everyone out before removing a destroyed vehicle, and our rule that nobody steps out of a hull that is under refuses them while that flag is still set (`SubmersiblePatches`, `CompPhobosSubmersible`). Found while reading the guard our own flight needs | Framework | — | **Fixed: Framework 0.147.0** (not yet tested in game): a vehicle being destroyed lets its people out through the shared guard |

## Vehicle accessories (13 Nov 2026)

The owner asked for a research and record round on accessories a player can install on vehicles, for entertainment, camping or
anything else ([record](vehicle-accessories.md)). Written before anything was built; what has been built since is in each row.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| Y0 | The rules: every accessory uses up something real; stores run down and are refilled; passive gear costs materials and weight only; no drag; camp first, some aboard | Framework | Owner, 13 Nov 2026 | Recorded |
| Y1 to Y4 | The four links: power outlet, crew water tank, camp order, life aboard | Framework | One button for the camp; water gear only with Dubs Bad Hygiene; life aboard and hotboxing as one piece; a removed accessory gives its kit back (owner, 13 Nov 2026) | Record written; not built |
| Y5 to Y13 | The short list: house battery, solar panel, fridge, swags, awning, kitchen box, folding table and chairs, shower and basin, stereo | Framework | **The power budget first** (loads and weight on fittings, house battery, outlet, fridge); "slept under the stars" at +5; other factions' camps with the camp batch (owner, 13 Nov 2026) | **Power budget built 17 Nov 2026** (Framework 0.129.0; not yet tested in game, rows 178 to 180): the house battery (Y5) with the outlet (Y1) and fridge (Y7), as crafted kits, automatic fridge, OpenAI art. Next in the owner's order: the camp (Y3, Y8, Y9, Y10, Y11) |
| Y14 to Y20 | The rest: reading light, better seats, roof rack, tool locker, tyre compressor, spotlight, bull bar | Framework | — | Record written; not built |
| Y21 | [Sleeping berths](vehicle-accessories.md#sleeping-berths-roof-tents-cab-bunks-and-driving-through-the-night-13-nov-2026): B1 roof tent at camp, B2 bunks on the road, B3 driving through the night, B4 sleeping inside a parked vehicle | Framework | Designed properly; all four parts in that order, B4 included; night driving a setting, off by default; roof tent memory +4; bunks on heavy vehicles only (owner, 13 Nov 2026) | Record written and answered; not built |
| Y22 | To read before the water work: how Dubs Bad Hygiene's water storage would see a tank on a vehicle | Framework | — | **Read 17 Nov 2026:** it cannot, as it stands: fixtures draw only from towers on their pipe network. Proposed, not built: a hidden stand-in tower beneath the parked vehicle, topped up from our tank, no Harmony ([the reading](vehicle-accessories.md#how-dubs-bad-hygiene-sees-water-and-what-a-vehicles-tank-needs-17-nov-2026)) |
| Y23 | To verify before any Def carries them: the record's web figures (fridge draw, water per person, shower flow, solar output, burner gas use), read only as search summaries | docs | — | **Part done, 17 Nov 2026** ([the table](vehicle-accessories.md#sources)): fridge draw, water per person and the dual-battery rule confirmed from the pages; the burner figure is not what its page says; shower flow and solar output still unverified (the pages refused); the Flying Doctor's 10 L has lost its source |

## Universal hoppers (17 Nov 2026)

The owner asked for a research and record round on near-universal hoppers, with chunks from the Talus carried by belt as the first
example ([record](universal-hoppers.md)). Written before anything was built; what has been built since is in the row.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| UH | [Universal hoppers](universal-hoppers.md): UH1 to UH16 in three families (vehicles into hoppers, hoppers into other mods, belts out to vehicles), and six questions for the owner | Framework, Vehicles, Factory Additions | Scope: the three families, active mods only, chunks not a new material (17 Nov 2026). **Build orders 1 to 3** (owner, 17 Nov 2026): UH1 with UH6, then UH2, then UH12; stand-in art; four cards answered as recommended | **Built 17 Nov 2026** (not yet tested in game, rows 167 to 169): UH1 and UH12 in Framework 0.125.0, UH2 in Vehicles 0.63.0, UH6 in Factory Additions 0.14.0. **Second round built 17 Nov 2026** (owner's cards; rows 172 to 175): UH3, UH5 and UH13 in Framework 0.127.0 and Vehicles 0.64.0, UH9 in the guide; UH16 researched and parked; UH11 is test row 175. The bunker's own picture bought and picked (Vehicles 0.64.1, about US$0.155); attempt 2 kept for other uses |

## Working trailers (13 Nov 2026)

The owner asked for a research and record round on working trailers, with a field kitchen powered by the Busbar and a prisoner
transport of their own, and the agent's ideas ([base and catalogue](vehicle-trailers.md), [field kitchen](vehicle-field-kitchen.md),
[prisoner transport](vehicle-prisoner-transport.md)). Written before anything was built; what has been built since is in each row.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| Q0 | The shared base: set up and pack up with no crew, a hitch that respects it, a power lead, set-out pieces and canvas rooms, stores | Framework | **First batch: the base with the water and fuel bowsers (Q3, Q4)**; canvas rooms yes; **3 x 7 the bare minimum, the other trailers at least vehicle-sized or near**, no 2 x 5 chassis (owner, 13 Nov 2026); "Build" (owner, 13 Nov 2026) | **Built in part: Framework 0.111.0, not yet tested in game** (checks 105 to 107): set up and pack up by hand, the hitch that respects it, tanks built in and limited to one fluid, tank to tank, pipes beneath trailers. **Built 17 Nov 2026 with the field kitchen (Framework 0.121.0), not yet tested in game** (checks 152 to 155): the power lead (a power point set out with the pieces), set-out pieces and canvas rooms. **Still open:** the dry-goods locker and a low-store warning (the kitchen's cold box and the trailer's cargo serve meanwhile) |
| Q1 | Billy field kitchen | Vehicles | Burners always, the Busbar's power as a bonus; the name Billy; batches of ten; caravan cooking as a setting, on; no siege kitchens (owner, 13 Nov 2026). **The first answered design to build after SD22, the aircraft batch and SD20** (owner, 16 Nov 2026) | **Built: Framework 0.121.0, Vehicles 0.59.0, not yet tested in game** (checks 152 to 154), at the owner's word of 17 Nov 2026: the tent, range, kettle (x10, x20 with its upgrade), meals shelf, cold box, lamp, power point, caravan cooking and three upgrades. **Follow-up batch built (Framework 0.122.0, Vehicles 0.60.0, not yet tested in game; check 156):** the kettle's water with Dubs Bad Hygiene (10 L a batch) from Billy's own 200 L tank, the dining annex, the wash-up basin and the bigger water tank (600 L), the tent heater burning only below 15 °C; the dry-goods locker later; no Field Tents tie-in. **Not built:** enemy bases with a set-up kitchen (owner: a later batch) and the dry-goods locker. Its own body picture, the pieces' pictures and its sounds are in since 17 Nov 2026 (owner's picks; body US$0.166), with six small paint flecks left on the body |
| Q2 | Stockade prisoner trailer | Vehicles | Capture into it, raiders' cages (1 raid in 5 with vehicles), captives at bases, harsh conditions; the name Stockade; memories as proposed; **the guard's tending a setting, "Wounds a guard tends a day", default 1** (owner, 13 Nov 2026) | Record answered; next after the first batch |
| Q3, Q4 | Water bowser (with Dubs Bad Hygiene) and fuel bowser | Vehicles | First batch (owner, 13 Nov 2026) | **Built: Vehicles 0.55.0, not yet tested in game.** 3 x 8, 2,500 L each; their own picture and overlays in Vehicles 0.56.0 (16 Nov 2026); the pump and legs sounds (S129) in Vehicles 0.59.0 (17 Nov 2026) |
| Q5, Q6, Q8 to Q14 | Cold store, aid post, crib room, field workshop, field laboratory, light tower, stone saw, bridge layer, livestock trailer | Vehicles | The supply, care and work families and the livestock trailer, in depth (owner, 13 Nov 2026) | Record written; not built |
| Q7 | Pintle P4 shower trailer (with Dubs Bad Hygiene) | Vehicles, Framework | **Flushing toilets with a waste tank; a chemfuel water heater, power a bonus; borrowed body picture; enemy bases later** (owner, 17 Nov 2026) | **Built: Framework 0.121.0, Vehicles 0.59.0, not yet tested in game** (check 155): 2,000 L tank, tent with two showers (a third by upgrade) and two flushing toilets, chemfuel water heater (half the chemfuel plugged in: the form "power a bonus" could take, agent's choice), hidden stand-in tower and septic (the waste tank) beneath it |
| Q15 to Q19 | Boat trailer, low-loader, drawbar tipper and tank trailers, battery trailer, ordnance trailer | Vehicles | Listed briefly only | Record written |
| Q20 | Requirement for any trailer carrying people or animals: a towed vehicle's riders join the caravan while it travels | Framework | — | Open; not checked in code |
| Q21 | Faults found on the way (the hitch moves a set-up vehicle; stale lines in four records) | Framework, docs | — | The hitch fault **fixed in Framework 0.111.0** (not yet tested in game); the stale lines **corrected 17 Nov 2026**, one dated record left as written |

## Pipes and conduits beneath our things (13 Nov 2026)

The owner asked for visible pipes and conduits, as the game and other mods show them, and an audit ([record](network-underlays.md)).

All its rows are done: see [Done: Pipes and conduits beneath our things (13 Nov 2026)](#done-pipes-and-conduits-beneath-our-things-13-nov-2026) at the end of this file.

## Garage and fleet tools (from 8 Nov 2026)

The design is [the vehicle garage and fleet tools](vehicle-stations.md#the-vehicle-garage-and-fleet-tools-design-8-nov-2026).

| # | What | Where | Owner | Status |
| --- | --- | --- | --- | --- |
| G5 | Standing crews and convoys | Framework | Yes (8 Nov 2026) | Set aside (owner, 9 Nov 2026): Crew Presets, on the owner's list, does this; it picks people, not exact seats |
| G7 | Post the outposts report to Vanilla Expanded Framework | Owner | Optional | Text in the design record |

## Logging (from 8 Nov 2026)

The game stops every mod's logging at 10,000 messages a session; one other mod's error storm killed the owner's log on 8 November
([record](logging-framework-design.md)). Owner's decisions, 8 Nov 2026: a bounded revive without Harmony; no Rust in-game; a new
sibling repository for the shared logger; record only this round.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| L4 | The JSON-lines sink, the schema document and the modkit reader shared by every game | phobos-log, phobos-modkit | | Open: when Ostranauts adopts or a need is named |
| L5 | The Ostranauts host sink (BepInEx) | phobos-log, phobos-ostranauts | | Open |

## Vehicle health (from 8 Nov 2026)

The owner's primary aim for the vehicles: pawn-like health and injury detail for player vehicles, diffuse for NPCs, performance
first ([record](vehicle-health-design.md)). Slices in order; each needs the one before.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| H4 | The performance table in the record, filled from the owner's runs (Dubs Performance Analyzer, the 1.7 Frame Profiler, our readout) | docs | First reading in the record (owner, 9 Nov 2026, Framework 0.42.0): within the overall budget at about 15% of it; one full-tier vehicle's rare tick is about a third over its own line | Open: further readings from the owner's runs |

## Caustic gas, the rest (8 Nov 2026)

All its rows are done: see [Done: Caustic gas, the rest (8 Nov 2026)](#done-caustic-gas-the-rest-8-nov-2026) at the end of this file.

## Ideas, not decided (8 Nov 2026)

Agent proposals made after the electric vehicles and caustic gas builds, kept here at the owner's request so they are not lost. None is
decided or started; each needs the owner's go.

| # | Idea | Mod | Why | Status |
| --- | --- | --- | --- | --- |
| I3 | Choose a vehicle's class (light, medium, heavy) by its empty mass instead of Vehicle Framework's Mass | Framework | Loadouts still use VF's Mass, which is not comparable between mods | **Record written** (owner's word, 16 Nov 2026): [vehicle class by empty mass](vehicle-real-figures-design.md#vehicle-class-by-empty-mass-16-nov-2026). Five Vanilla Vehicles Expanded vehicles would move (the Traveller to light; the Hermano, Highwayman, Roadrunner and Nomad to heavy); real, tracked, boat and electric vehicles none, as their own loadouts decide first (the tanks were already heavy by the tracked loadout). Two questions open; not built |
| I4 | A field decontamination kit for caravans: strip and rinse away from any shower | Medical | Strip and rinse (C8) needs a shower; there is none in the wild | Idea; left as one (owner, 16 Nov 2026) |
| I6 | Liquid oxygen in the bulk tanker, insulated tank only, boiling off slowly | Vehicles, Medical | D'thorga Medical's liquid oxygen store would feed it; the owner asked for it to wait for other functionality (9 Nov 2026) | Idea, documented; left as one (owner, 16 Nov 2026) |
| I7 | Hotboxing: passengers smoke aboard, a closed cab shares the high with everyone in it, smoke drifts from the windows, a stoned driver drives slower (owner, 12 Nov 2026) | Framework, Vehicles | The owner's idea; a stoned driver drives worse with real consequences, no random crashes | Design record written and the four questions answered (owner, 12 Nov 2026: all smoked drugs, a windows switch, raiders' rare roll, the starting figures); not built, waiting on the owner's go: [record](vehicle-hotboxing.md) |

## Housekeeping (8 Nov 2026)

All its rows are done: see [Done: Housekeeping (8 Nov 2026)](#done-housekeeping-8-nov-2026) at the end of this file.

## Analysis tooling (from 7 Nov 2026)

Built in the shared [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit) repository and consumed here; the order and
reasons are in [analysis tooling](analysis-tooling.md). Each tool's first change here adds its rule to `AGENTS.md`.

| # | Item | Language | Status |
| --- | --- | --- | --- |
| T10 | Game adapter contract (generic log and save commands) | Python | Waits for a second game adapter |
| T16 | **The near-copy check scores flat patches as alike** (found 12 Nov 2026, [round three](../../assets/phobos-vehicles/requests/round-three-record.md)): the game's flat grey button squares match blank white at about 0.94 a patch, so three shipped picks (the generator set's cable out 1, the drill rig's diamond bit 1 and hammer 1) are flagged against the larger set of game sprites though nothing like them. Counting only patches with detail clears every false flag but loses a recoloured copy, so it was not adopted. Next ideas: compare in colour rather than grey, or judge flat patches by their difference in brightness. The same night's fix (the hash counts only between subjects of like proportions) is done | Python | Open: agent proposal; the shipped picks stand, judged by eye |
| T15 | Desktop front end over the tools, for the person at the screen: a new sibling repository on Tauri 2 and Svelte over a Rust host, calling the kit through `--json` | Rust and TypeScript | Steps 1 to 4 and the report viewer's first stage done 9 Nov 2026; charts and the colony mood report 10 Nov 2026; the atlas 10 Nov 2026; none yet judged on screen by the owner: [D'thorga Companion](https://github.com/phobos-dthorga/phobos-companion) 0.6.0 (the window, theme, checks, the "where was I?" home page, 21 RimWorld reports chosen by question, charts on ECharts, the atlas as a three.js hex globe); phobos-modkit 1.7.0 (one JSON envelope, a tool manifest, sortable values, questions, chart blocks, `rimworld-save mood` with advice and the [colony mood file](mood-snapshot.md) from Framework 0.64.0, `rimworld-save atlas` and globe blocks, roads and rivers in the game's neighbour order). Next: the owner's cross-links between reports and the atlas, saves stepped through on the atlas, then input forms ([research](tools-desktop-research.md)) |
| T17 | **Smoke run on a lean profile**: the game started on its own small profile with only our mods, a dev routine spawning and ticking every thing of ours on the quick-test map, then a report of every error ([design](smoke-run-design.md)) | Python and C# | Open: design only (18 Nov 2026); waits for the owner's go-ahead |
| T18 | A frozen game's stacks, the heap by mod, and methods named in full crash dumps | Python | Done, not yet run on the game: phobos-modkit 1.22.0, `scripts/perf.py stack` and `heap`, `scripts/crash-dump.py setup`, AGENTS.md rules (18 Nov 2026) |
| T19 | Commit guard: a commit only of files a full passing gate run checked | Python | Done: phobos-modkit 1.22.0, `.githooks/pre-commit`, AGENTS.md rule (18 Nov 2026) |
| T20 | Upstream watch: updates and authors' commits that change members we patch or name | Python | Done: phobos-modkit 1.23.0, `scripts/upstream.py`, AGENTS.md rule (18 Nov 2026). First run: 13 commits, VF-412 among them |

## Deferred

| Item | Reason |
| --- | --- |
| Hydrogen as a room gas, and the four vents (extractor fan, passive vent, fume hood, hydrogen detector) | The owner left it out of this round (7 Nov 2026); [ventilation and hydrogen](ventilation-and-hydrogen.md) |
| Radio Framework: the three defaults of 19 Oct, licence tiers, Phase 2, the public name | "Later, when I have a better idea of everything" (owner, 7 Nov 2026) |
| D'thorga treatments for IRBM's other strategic warheads | After the owner has tested the nuke |
| Art still a stand-in or borrowed ([stand-in art audit, 17 Nov 2026](art-backlog.md#stand-in-art-audit-17-nov-2026)). **Next local run, the owner's pick (17 Nov 2026):** the four Mk. 84 fills, the vehicle buttons and fittings drawn in code (work lamps as a redraw, auxiliary and ferry tanks, litter kit, cold-weather family, hitch, unhitch, winch, stop winch, litter unload, service lead, water drop, sonar ping, grab, fire bucket), the Crawler's five upgrade icons and the triage tags. **A later batch:** Billy's tent pieces (canvas wall atlas, door, lamp, heater; the game's pictures, tinted, until then). **Held:** the whip antenna (until the radio resumes, owner, 12 Nov 2026), the drill rig's well-pipe rack (refused twice by the paid API; waits for the owner's GPU) | Only on the owner's word: a go and a stop time, with the game closed. Kept as they are by the owner's choice: the load switch (10 Nov 2026), the shower trailer's borrowed body (17 Nov 2026); the coax atlases stay drawn by script. Done since: the dashboard pictures (picked 10 Nov 2026), the bowsers' own body and overlays (Vehicles 0.56.0, 16 Nov 2026), the Pintle trailer's own picture and greedy boards, the tipper's loading grab, and the winch and long-range tank on our trucks (Framework 0.105.0, Vehicles 0.49.0, [round three](../../assets/phobos-vehicles/requests/round-three-record.md)); the store's frost (drawn in code, Medical 0.15.0), the electrolyser, the chamber redraw and the new equipment (Medical 0.14.1), the radio base station (Radio 0.1.7) |
| A base class for interval map components in the Framework | Would change the timing code of parts the owner has tested; waits until one is changed for its own sake |
| [A setup wizard on first load](first-load-setup-wizard.md): how the player wants our mods to play, above all how they affect third-party mods | Owner, 9 Nov 2026: when the time is right; the agent may raise it (agent's view: once the settings have settled, at the public beta gate at the latest) |
| Workshop publication tooling | At the first public beta, per AGENTS.md |

## Done

Rows whose work is finished, moved here unchanged on 17 Nov 2026 from the tables above, one table per area, so their IDs stay
searchable in this file (other records cite them, as in "outstanding-work V9a"). A row is here when its status says done, built or
fixed with nothing left open; in-game testing, which this file does not track, may still be waiting: [the in-game test pass](in-game-test-pass.md)
lists it. A row that is only partly done stays in its table above.

### Done: This round, in order

*Moved here on 17 Nov 2026 from [This round, in order](#this-round-in-order); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| A1 | This checklist | docs | Wanted | Done |
| A2 | Stale records after the merge (Medical Equipment's "next", the art backlog's pre-merge previews, Triage's open points) | docs | Fix | Done |
| A3 | Water Tap draws through the Framework's shared water reader | Water Tap 0.2.2 | Do it | Done |
| A4 | Read Ludeon's 1.7 Modder Primer | docs | Do it | Done: [modding reference](rimworld-modding-reference.md#building-c-mods) |
| B1 | Immunity race: dose when losing by under **two days** (was one) | Medical (dosing) | Two days | Done: Medical 0.7.0 |
| B2 | Doctors give pain relief from **40%** pain (was 60%) | Medical (dosing) | From 40% | Done: Medical 0.7.0 |
| B3 | Masochists skip self-medication | Medical (dosing) | Yes | Done: Medical 0.7.0 |
| C1 | A colour-coded icon for every triage tag, in the game's row of small status icons by a pawn's name | Medical (triage) | Every tag: red immediate, yellow urgent, green delayed, white minor, black expectant | Done: Medical 0.7.0 (Useful Marks row, or beside the name without it) |
| C2 | "How far the doctors may go" as a dropdown on the health tab, beside medical care (the button stays) | Medical (triage) | Add it | Done: Medical 0.7.0 (a button beside the medicine setting) |
| C3 | Triage queues fracture repairs under "save life or a limb" | Medical (triage) | Triage queues them | Done: no change needed, triage already queued them |
| C4 | More Injuries' brain-damage therapy and custom-worker recipes found by triage | Medical (triage) | With C1 to C3 | Done: Medical 0.7.0 |
| D1 | Respect More Injuries' own fire-inhalation setting | Medical (burns) | Respect it | Done: Medical 0.7.0 (its setting decides; ours stands down while a pawn burns) |
| D2 | Fire heat indoors: hot cells near a fire in a large room | Medical (burns) | Yes | Done: Medical 0.7.0 (its own setting) |
| D3 | Pawns and animals flee cells hot enough to burn | Medical (burns) | Yes | Done: Medical 0.7.0 (its own setting; firefighters stay) |
| D4 | Supplied oxygen treats More Injuries' hypoxia | Medical (equipment) | Yes | Done: Medical 0.7.0 (faster healing; the chamber twice as fast) |
| E | Burns phase 2: eschar, escharotomy, ischaemia handed to More Injuries' gangrene, a fluid bill using its saline bag | Medical (burns) | This round, first build | Done: Medical 0.7.0 (fluids bill with More Injuries only; our kit comes in G) |
| F | Second equipment set, in the owner's order: ventilator (intubation a quick doctor job), safety shower and eyewash, carbon monoxide and smoke alarm (wakes sleepers; separate from the hydrogen detector, Ostranauts-like), cylinder trolley, nebuliser, anaesthesia machine, needle decompression kit, steriliser | Medical (equipment) | This round, second build | Done: Medical 0.7.0, all eight with placeholder art |
| G | Fussing (lower tend quality in extreme pain) and a fluids kit for burn shock | Medical | This round, third build | Done: Medical 0.7.0 (fussing setting on; fluids kit beside the saline bill) |
| H | Caustic (tox) gas model: slow-acting by default, fast-acting setting | Medical (burns) | This round, after the builds | Done: Medical 0.7.0 (slow-acting default; eye scarring, plants and clothing still open) |
| Ia | Workstation environmental effects: a design record | docs | Record now, nothing built | Done: [record](ventilation-and-hydrogen.md#workstation-environmental-effects-design-7-nov-2026) |
| Ib | Vaccines and prevention for healthy pawns: a design record | docs | Record now, nothing built | Done: [record](vaccines-and-prevention.md) |
| Ic | Harrier: upgrade costs and CE armour against the Hind, and what a shot-down Harrier leaves; proposals | Patches | Check both | Done: findings and proposals in [vehicle patches](vehicle-patches.md); the cheaper CE armour upgrade **closed: leave it alone** (owner, 16 Nov 2026) |
| J2 | **Remove the temporary Prison Labor guard** once its author ships a fix | Patches | Yes: owner, 10 Nov 2026, "strictly temporary" | **Done**: Prison Labor 1.7.1 (13 Nov 2026) fixed the fault, and the guard came out in Patches 0.19.2 (16 Nov 2026) at the owner's word ([dependency audit](dependency-audit-2026-11-16.md)) |

### Done: The owner's reports of 7 November 2026 (evening)

*Moved here on 17 Nov 2026 from [The owner's reports of 7 November 2026 (evening)](#the-owners-reports-of-7-november-2026-evening); kept as written.*

| # | Report | Finding | Status |
| --- | --- | --- | --- |
| R1 | Machining bays read only the first stack in a deep factory hopper | The scan reads every stack; a bill saved before a recipe change kept old owners, and an item it could not take threw and ended the scan. Two bays also waited on ingredients no input hopper held ([record](vfe-factory-conveyors.md)) | Done: Factory Additions 0.11.4 rebuilds stale bills, guards the scan; README states the input-cell rule. Not yet tested in game |
| R2 | The Harrier cannot fly sorties while a RimPacts client state attacks | RimPacts' spectate landing gate empties the arrival list until the battle is joined; the jet rejected the click in silence ([record](rimpacts-patches.md#sorties-blocked-by-rimpacts-battles-and-the-parts-list-7-nov-2026-evening)) | Done: Patches 0.11.0 shows the reason; a setting lets sorties fly over an unjoined battle. The owner should try Join first. Not yet tested in game |
| R3 | A far more detailed Harrier parts list, upgrades following, existing jets updated | Vehicle Framework saves parts by position; parts may only be appended, and code must add them to saved jets ([record](rimpacts-patches.md#sorties-blocked-by-rimpacts-battles-and-the-parts-list-7-nov-2026-evening)) | Done: Patches 0.11.0, fourteen parts, flight-control rule, saved jets gain the parts on load. Not yet tested in game |
| R5 | After a nuke, an ally's RimPacts assault "never eventuated" and the battle was lost; about twelve towns were lost later | RimPacts trims the assault to the defenders left standing, about two after the strike ([record](rimpacts-patches.md#rimpacts-assaults-on-a-struck-town-7-nov-2026-evening)) | Done: Patches 0.11.1 (0.11.0's version did not work: RimPacts capped the squad before our trim). The towns: RimPacts' world war quest deadline (3 days from accepting) passed before the battle; its daily check failed the war and moved control of 11 towns. RimPacts' rule, recorded in the README; Patches 0.12.0 gives the quest the game's own countdown (owner's request); Patches 0.13.0 stops Visit Settlements closing a battle map under the owner's force (bug report drafted in the [Visit Settlements record](visit-settlements-patches.md)) |
| R4 | (Found in the log) 7,662 errors from the IRBM strike job after loading another game | The job named a closed map in a log line, and the Framework's queue could not empty itself | Done: Framework 0.8.1 and Patches 0.11.0 |

### Done: Vehicles (from 8 Nov 2026)

*Moved here on 17 Nov 2026 from [Vehicles (from 8 Nov 2026)](#vehicles-from-8-nov-2026); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| V1 | Vehicle systems: battery, coolant, oil, wear and services, tyres, brakes, drivetrains, gas turbines, power and lift, steam, Combat Extended corrections, the world map and the cab, the Dashboard and Systems tabs, booster packs, world-map ground, cab moods, RimHUD | Framework 0.9.0 to 0.18.0, Vehicles 0.1.0 to 0.7.0 | Approved in parts | Done; not yet tested in game |
| V2 | What a service uses for oil and coolant (chemfuel stands in; with Dubs Bad Hygiene coolant could be its water) | Framework | Unsure (8 Nov 2026) | Done: Framework 0.33.0. Coolant is Dubs Bad Hygiene's water; without that mod, chemfuel for both oil and coolant (owner, 8 Nov 2026). Not yet tested in game |
| V3 | The utility vehicles, five designed in full: survey truck, drill rig, tipper hauler, generator set and, since 8 Nov 2026, the bulk tanker (liquid or gas by upgrade). **The tanker is built first** (owner, 8 Nov 2026) ([design](utility-vehicles-design.md)) | Vehicles | Yes, after the framework; the five and the tanker's shape (8 Nov 2026); tanker first (8 Nov 2026) | The tanker is built (Framework 0.34.0 onward, Vehicles 0.12.0 onward; not yet tested in game). The other four are built too (generator set Framework 0.44.0, survey truck and drill rig 0.46.0, tipper 0.49.0): [survey truck](vehicle-survey-truck.md), [drill rig](vehicle-drill-rig.md), [tipper hauler](vehicle-tipper-hauler.md), [generator set](vehicle-generator-set.md). The survey truck passed the owner's test (9 Nov 2026); the others are not yet tested in game; their bodies got picked pictures on 12 Nov 2026 |
| V5 | Systems for electric vehicles, with an optional patch making Vanilla Vehicles Expanded's Lightning electric to test them; our own diesel-electric utility vehicles later (V3) | Framework, Vehicles | Framework and a test vehicle (8 Nov 2026) | Done: Framework 0.20.0, Vehicles 0.9.0 ([record](vehicle-electric-design.md)); a night heater for electric vehicles left for later |
| V7 | Engine sound pitched by engine speed | Framework | Done | **Built in 0.76.0** (11 Nov 2026): our systems play the engine loops themselves, pitched by rpm and blended by load (sound-catalogue.md); not yet tested in game |
| V7a | Vehicle sounds too quiet against the game's mix (owner's listening report on reference speakers, 13 Nov 2026) | Framework, Vehicles | Fix it | **Built: Framework 0.108.1, Vehicles 0.51.1** (engines 4 dB up, doors 3 dB up, reach widened to the game's own; sound-catalogue.md); not yet tested in game (check 89) |
| V8 | Kerb mass of 12 kg per Vehicle Framework mass unit for retrofitted vehicles, checked for outliers (dirt bikes, tanks) | Framework, Vehicles | Authored masses for Vanilla Vehicles Expanded; tunable (8 Nov 2026) | Done: Framework 0.19.0, Vehicles 0.8.0 ([record](vehicle-real-figures-design.md#kerb-mass-check-8-nov-2026)) |
| V9a | Vehicle Framework 1.7's hand brakes wear wheel health beside our own tread wear | Framework | **Leave both** (owner, 16 Nov 2026): they measure different things | Done: nothing to build ([dependency audit](dependency-audit-2026-11-16.md), decision 1) |
| V9 | When Vehicle Framework ships for 1.7: rebuild, and re-check the tab hook's order, the caravan patches' method names, and the part icons and colours the Systems tab leaves out | Framework | Expected | **Done** (16 Nov 2026): rebuilt in Framework 0.111.1; hook order and caravan patches unchanged ([dependency audit](dependency-audit-2026-11-16.md)); health colours now the game's own in Framework 0.112.0; VF has no part icons to borrow |
| V14 | Bleed air: the Harrier's puffer jets in a hover (more in wind) and cockpit heat raise the engine's work | Framework | Yes, if it stays cheap (8 Nov 2026) | Done: Framework 0.24.0 |
| V13 | The Dashboard and Systems tabs on the world map: vehicle caravans and aircraft in flight, with a vehicle list | Framework | Yes (8 Nov 2026) | Done: Framework 0.23.0 |
| V12 | Cab heaters for aircraft, boats and steam cabs (a gap found from the owner's Harrier); automatic climate control by era with a blanket setting | Framework, Vehicles | Climate control with a main-menu option (8 Nov 2026) | Done: Framework 0.21.2 and 0.22.0, Vehicles 0.9.1 and 0.10.0 |
| V11 | The tabs after the owner's first look (8 Nov 2026): wear gauges show what is left, no repeated part names, no empty sections | Framework | Show what is left | Done: Framework 0.19.0 |

### Done: Vehicles, 11 to 12 Nov 2026

*Moved here on 17 Nov 2026 from [Vehicles, 11 to 12 Nov 2026](#vehicles-11-to-12-nov-2026); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| W1 | Sling slice 1: the cargo hook, the sling set, lift and carry a vehicle, set-down on landing ([design](vehicle-sling-design.md)) | Framework 0.92.0, Vehicles 0.40.0; the picture of the load, 0.99.0 | Our own airlift, every flavour a setting (11 Nov 2026) | Built. **Take-off and the world map passed** (owner, 12 Nov 2026); the landing and set-down are not yet tested in game. **The set-down on landing failed in the owner's test of 12 Nov 2026 on 0.100.0 (the map went grey); fixed in Framework 0.101.1, not yet tested in game**. **The Louie and the Hind take a cargo hook too (owner, 12 Nov 2026): Vehicles 0.46.0, with Framework 0.102.0 for the Hind's own on-map flight; not yet tested in game** |
| W3 | Sling slice 3: the five icons and the hook latch sound | Framework 0.92.0 to 0.101.0 | Icons picked (11 Nov 2026); sound made on the processor (owner, 12 Nov 2026); **a fresh set for the latch** (owner, 16 Nov 2026) | Built; the latch sound is the agent's provisional pick. **Fresh set made 16 Nov 2026** (`assets/phobos-sounds/requests/sling-latch-2.json`: eight clean takes on the distilled model; eight at full steps rejected as clipped). **Done: the owner picked take r2d-94, Framework 0.114.4**, not yet tested in game |
| W4 | The hitch, slices 1 and 2: the follow rule and towed mass, then the hitch on the map and the Pintle flatbed trailer ([design](vehicle-hitch-design.md)) | Framework 0.101.0, Vehicles 0.45.0 | On the map and hittable, a borrowed picture, our drivetrain only by default, one medium flatbed (11 Nov 2026) | Built, not yet tested in game. One setting added (Towing), default off |
| W6 | Vehicle upgrades, round two ([record](vehicle-upgrades-round-two.md)): batch 1 (the generator set's heat exchanger, the claim order, the core part store), batch 2 (the hitch, W4), batch 3 (the service lead, the loading grab, the water bucket, the litter kit) | Framework 0.97.0 and 0.101.0, Vehicles 0.42.0 and 0.45.0 | All four signatures (owner, 12 Nov 2026) | Built, not yet tested in game |
| W7 | Shared upgrade families: recovery, long range, cold weather, work lights, all-terrain tyres ([record](vehicle-upgrades-round-two.md#vehicle-upgrade-families-upgrades-shared-across-many-vehicles-12-nov-2026)) | Framework, Vehicles | Design record first, nothing built (owner, 12 Nov 2026); all five, recovery as one piece with hitch slice 4, whole set or part by part, ferry tanks on aircraft too (owner, 12 Nov 2026) | **All five built** in Framework 0.103.0 and Vehicles 0.47.0 (parts 1 to 3, 12 Nov 2026), not yet tested in game: the order for a whole family, the cold-weather pack, all-terrain tyres, work lights, long range (auxiliary and ferry tanks) and recovery (the winch and the recovery tow). Checks 64 to 68 |
| W9 | The Seabed Crawler: hull, research tab, wet and advanced laboratories, listening array, sonar set, two hull refits, its picture and overlays ([design](vehicle-seabed-crawler.md), [framework](submarine-framework.md)) | Framework 0.62.0 to 0.81.0, Vehicles 0.28.0 to 0.38.0 | A flagship of D'thorga Vehicles; enemy submarines "very soon" (owner, 10 Nov 2026) | Built, not yet tested in game |
| W10 | Parts bills: a thing's build cost listed by role, filled by installed mods' parts or ours ([record](parts-bills.md)) | Framework 0.95.0, Vehicles 0.41.0 | Costs in line with similar trucks, widest reach (11 Nov 2026) | Built, not yet tested in game (check 53) |
| W11 | The 15 dashboard warning pictures (`assets/phobos-vehicles/requests/dash-telltales.json`, 60 images on the local model) | art | GPU only when the owner says so (owner, 12 Nov 2026) | **Done**: made and picked by the owner on 10 Nov 2026 (53af902); the row was stale until 13 Nov 2026 |
| W12 | Headlights that light the ground, on every vehicle our systems run; with the engine off they run the battery down; a warning at low starting power; the battery in words ([record](vehicle-headlights.md)) | Framework 0.104.0 | Owner, 12 Nov 2026 | Built, not yet tested in game. One setting added (Headlights light the ground), default on; the headlights switch now defaults off |

### Done: Cross-links round three (13 Nov 2026)

*Moved here on 17 Nov 2026 from [Cross-links round three (13 Nov 2026)](#cross-links-round-three-13-nov-2026); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| X8 | **The first batch, when the owner says build** (owner, 13 Nov 2026): T6, G18, G15; A5, A9, A6 with G16, A8; G9. Agreed for later: G1 with G2, G5, G7 (fording and snorkel), G13 (cook-off, on where Combat Extended allows), the avionics answers | Framework, Vehicles, Patches | Chosen | **Built: Framework 0.106.0, Vehicles 0.50.0, Patches 0.19.0 (c4fa18b, a0c431a and part 3), not yet tested in game.** Built with it at the owner's word: our own vehicles' upgrades priced like tunes |
| X4 | Fault: our Harrier flight-control rule can refuse launches for other aircraft while the Harrier mod is active | Patches | — | **Fixed: Framework 0.106.0 and Patches 0.19.0 (A8), not yet tested in game** |
| X7 | Fault: round one's record miscounted its links | docs | — | Done: corrected 13 Nov 2026 |

### Done: The owner's report of 16 Nov 2026: allies slow to destroy vehicles

*Moved here on 17 Nov 2026 from [The owner's report of 16 Nov 2026: allies slow to destroy vehicles](#the-owners-report-of-16-nov-2026-allies-slow-to-destroy-vehicles); kept as written.*

| # | Report | Finding | Status |
| --- | --- | --- | --- |
| R6 | Allies capturing a town spend an inordinate time attacking its vehicles (save PG8, Framework 0.114.2) | RimPacts counts an empty vehicle as a live defender, and its allies trash it by hand (all 57 logged attacks melee or fire, 11 of 19 soldiers on one Highwayman); our appended parts also made every vehicle about 17 % harder to destroy ([record](vehicle-health-design.md#our-parts-and-a-vehicles-destruction-16-nov-2026)) | **Patched: Patches 0.19.3** (temporary, register row 17, no report at the owner's word); **our empty mounting points no longer count: Framework 0.114.5, Vehicles 0.56.2** (the owner's audit rule). Not yet tested in game (checks 118, 119) |

### Done: Knocked out, not destroyed (16 Nov 2026)

*Moved here on 17 Nov 2026 from [Knocked out, not destroyed (16 Nov 2026)](#knocked-out-not-destroyed-16-nov-2026); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| KO3 | Gap: nothing makes a crew leave a vehicle that can neither move nor shoot, or is burning (row C2) | Framework | — | **Fixed: Framework 0.115.0** (not yet tested in game) |

### Done: Tie-ins with other mods (13 Nov 2026)

*Moved here on 17 Nov 2026 from [Tie-ins with other mods (13 Nov 2026)](#tie-ins-with-other-mods-13-nov-2026); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| TI4 | Fault: Vehicles at Work drives vehicles our systems refuse to move on a map (worn brakes, a set-up Busbar, a settled submarine) | Framework | Fix it now, ahead of everything else (owner, 13 Nov 2026) | **Fixed: Framework 0.108.4, not yet tested in game** (SD1; the depot bay's Go to bay had the same gap and is fixed too) |
| TI5 | Gap: IRBM's CIWS cannot see Combat Extended's shells in the owner's game | Patches | Patch it (owner, 13 Nov 2026) | **Built: Patches 0.19.1**, not yet tested in game; temporary-patches.md row 16; check 98 |

### Done: Pipes and conduits beneath our things (13 Nov 2026)

*Moved here on 17 Nov 2026 from [Pipes and conduits beneath our things (13 Nov 2026)](#pipes-and-conduits-beneath-our-things-13-nov-2026); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| P1 | Visible plumbing and visible Vanilla Expanded Framework pipes beneath our buildings and parked vehicles, older hidden ones swapped by themselves | Framework | Visible, as the game does it (13 Nov 2026); reversed after testing (P5) | **Built: Framework 0.107.0; hidden again from 0.108.1** |
| P2 | The generator set joins the grid through power conduits beneath it, beside the cable end | Framework | An option beside the cable (13 Nov 2026) | **Built: Framework 0.107.0, not yet tested in game** |
| P3 | The audit's proposals: the bottle tap lays plumbing (N4), vehicles refuel from a chemfuel pipe (N10), the depot bay fills from one (N7) | Water Tap, Framework | Audit and propose (13 Nov 2026); all three yes, and the removal rule kept (owner, 13 Nov 2026) | N10 **built** (Framework 0.107.0, not yet tested in game); N7 needs nothing: Vanilla Expanded Framework already fills the bay from a chemfuel pipe; N4 not built: the tap already joins plumbing as Dubs' own fixtures do, and a pipe beneath it would trip Dubs' overlap error |
| P4 | Pipes and conduits beneath every vehicle parked with its engine off (plumbing, power conduit, any line it uses), gone when it starts; the old hidden pipes of earlier versions found and made visible once ([record](network-underlays.md)) | Framework | Engine off, always; conduits for every vehicle with a battery; the generator set keeps its conduits while set up (owner, 13 Nov 2026) | **Built: Framework 0.108.0, not yet tested in game** |
| P5 | **Hidden again** (reverses P1's visible choice): Dubs' hidden plumbing, the game's hidden conduit and a line's transparent pipe, seen in the overlays; our visible pieces of 0.107.0 and 0.108.0 swapped by themselves; a pipe of ours deconstructed refunds nothing ([record](network-underlays.md)) | Framework | "You need to use the Hidden versions of them, mate" (owner, 13 Nov 2026, after testing 0.108.0) | **Built: Framework 0.108.1, not yet tested in game** (check 88) |

### Done: Garage and fleet tools (from 8 Nov 2026)

*Moved here on 17 Nov 2026 from [Garage and fleet tools (from 8 Nov 2026)](#garage-and-fleet-tools-from-8-nov-2026); kept as written.*

| # | What | Where | Owner | Status |
| --- | --- | --- | --- | --- |
| G1 | The garage: parking and slow repairs | Framework, Vehicles | Yes (8 Nov 2026) | Built: Framework 0.31.0, Vehicles 0.11.0. Not yet tested in game |
| G2 | Refuse convoys at the normal "create outpost" window, with a setting | Patches | Yes (8 Nov 2026) | Built: Patches 0.15.0. Not yet tested in game |
| G3 | Parked-vehicle warning | Framework | Yes (8 Nov 2026) | Built: Framework 0.32.0. Not yet tested in game |
| G4 | Fleet board | Framework | Yes (8 Nov 2026) | Built: Framework 0.32.0. Not yet tested in game |
| G6 | Home depot bays | Framework, Vehicles | Yes (8 Nov 2026) | Built in Framework 0.45.0 ([record](vehicle-stations.md#home-depot-bays-design-9-nov-2026)), not yet tested in game |

### Done: Logging (from 8 Nov 2026)

*Moved here on 17 Nov 2026 from [Logging (from 8 Nov 2026)](#logging-from-8-nov-2026); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| L1 | The research record; the engine shims decompiled for the code index | docs | Record only (8 Nov 2026) | Done |
| L0 | Visit Settlements' food drops throw every tick when a friendly settlement holds a pawn that does not eat (the storm that killed both logs) | Patches | Fix at the source (8 Nov 2026) | Done: Patches 0.14.1 (not yet tested in game); bug report drafted for the owner to post |
| L2 | GameLogKeeper: revive the game's log when it caps itself (bounded, a setting, on by default), the storm counter naming the culprit, the probe dev actions T1 to T5 and the benchmark | Framework | Revive, no Harmony (8 Nov 2026) | Done: Framework 0.28.0 (not yet tested in game) |
| L3 | Create the sibling repository (owner names it; proposed phobos-log), the game-free rules (once, throttle, budget, signature) and the RimWorld category sink; the Framework and D'thorga Patches adopt it, other mods on touch | new repo, Framework, Patches | Sibling repository (8 Nov 2026) | Done: phobos-log 1.0.0 (public), pinned at external/phobos-log; Framework 0.33.0 and Patches 0.16.0 adopt it (not yet tested in game); other mods when next touched |

### Done: Vehicle health (from 8 Nov 2026)

*Moved here on 17 Nov 2026 from [Vehicle health (from 8 Nov 2026)](#vehicle-health-from-8-nov-2026); kept as written.*

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| H0 | Measurement (a dev timing readout) and the NPC lite tier, with the setting to run the full model for everyone | Framework | Lite by default, setting (8 Nov 2026) | Done: Framework 0.25.0 |
| H1 | The Health tab replacing Vehicle Framework's: VF's stats with impactor tooltips, parts grouped pawn-style with the stages and refusals under them, a crew row, hover highlight, on the world map too; a setting keeps VF's tab | Framework | First deliverable (8 Nov 2026) | Done: Framework 0.26.0 |
| H2 | Conditions proper: per-part severities with stages, from hits, stages and wear; resolved by repair, fading or a service | Framework | Architecture chosen (8 Nov 2026); Def shape after a read of Vehicle Framework's develop branch | Done: Framework 0.29.0 (not yet tested in game), three agent-authored conditions for the owner's review |
| H3 | Consequences and repair depth: leaks with the danger clock, destroyed parts needing supplies (setting, on), field fixes, permanent conditions; wreck-on-death as a separate question | Framework, Vehicles | Supplies for destroyed parts and serious conditions (8 Nov 2026) | Done except permanent damage and wrecks, left out on the agent's recommendation: Framework 0.36.0, Vehicles 0.14.0 ([record](vehicle-health-design.md#vehicle-health-slice-3-what-it-does-to-the-player-8-nov-2026-built-9-nov-2026)); not yet tested in game |
| H5 | Our parts framework grown into tuning and upgrades | Framework, Vehicles | Direction (owner, 9 Nov 2026); wants its own record of what each behaviour does before it is built | Built: fittings, tunes and enemy sets (Framework 0.40.0), part grades (0.41.0); not yet tested in game ([record](vehicle-tuning-and-upgrades.md)) |

### Done: Caustic gas, the rest (8 Nov 2026)

*Moved here on 17 Nov 2026 from [Caustic gas, the rest (8 Nov 2026)](#caustic-gas-the-rest-8-nov-2026); kept as written.*

| # | Item | Owner's decision | Status |
| --- | --- | --- | --- |
| C5 | Lasting eye damage after a severe caustic eye injury | Our own corneal scarring | Done: Medical 0.8.0 |
| C6 | Plants and corpses in dense gas | Plants wilt, corpses unaffected | Done: Medical 0.8.0 |
| C7 | Supplied oxygen and chemical lung injury | Oxygen eases and speeds recovery | Done: Medical 0.8.0 |
| C8 | Contaminated clothing | The shower is enough by default; extra decontamination with Dubs Bad Hygiene; strip and rinse as a main-menu setting | Done: Medical 0.8.0 |

### Done: Ideas, not decided (8 Nov 2026)

*Moved here on 17 Nov 2026 from [Ideas, not decided (8 Nov 2026)](#ideas-not-decided-8-nov-2026); kept as written.*

| # | Idea | Mod | Why | Status |
| --- | --- | --- | --- | --- |
| I1 | A diesel-electric utility vehicle that powers a base: parked, its generator feeds the power grid (a mobile generator set) | Vehicles, Framework | The owner wants diesel-electric utility vehicles with mechanics of their own (V3); the engine model, traction motor heat and the electrical system already exist; the generator set's sketched upgrade tree carries a diesel-electric drive node ([design](utility-vehicles-design.md)) | Decided (owner, 9 Nov 2026): the generator set is the self-propelled diesel-electric vehicle and the camp's power for every vehicle of ours; designed in [its record](vehicle-generator-set.md). **Built**: Framework 0.44.0 and D'thorga Vehicles' generator set (see V3); not yet tested in game |
| I2 | A small battery buffer on a diesel-electric (a hybrid), so it can creep quietly a short way on electricity alone | Framework | Builds on the traction battery of the electric vehicles (V5) | Offered as a node and an open question in [the generator set's record](vehicle-generator-set.md) (9 Nov 2026). **Built (owner, 12 Nov 2026): Framework 0.104.0, the Battery drive and the creep when out of fuel; not yet tested in game** |
| I5 | A night heater for electric vehicles, drawing a share of the traction battery an hour | Framework | Left out of V5: the night heater's fuel is counted in chemfuel units | **Built (owner, 12 Nov 2026): Framework 0.104.0, 2 % of the pack an hour at a night camp; not yet tested in game** ([record](vehicle-electric-design.md)) |

### Done: Housekeeping (8 Nov 2026)

*Moved here on 17 Nov 2026 from [Housekeeping (8 Nov 2026)](#housekeeping-8-nov-2026); kept as written.*

| # | Item | Status |
| --- | --- | --- |
| K1 | `python scripts/cleanup.py --apply`: on 11 Nov 2026 the preview listed 15.9 GB (the shared tools' 12.2 GB development build, 724 old install backups, 17 stale save indexes, 3 old crash dumps, 8,357 .NET build logs, 401 test repositories, 6 git watcher daemons) | Done, 11 Nov 2026: most had already been cleared; the run on the owner's go removed 21 backups and 391 build logs |
| K2 | Another session's uncommitted edits in the tree since 8 Nov 2026 (`docs/development/README.md`, `project-direction.md`, the new `rimworld-1.7-readiness.md`) | Done: the owner confirmed every session finished (8 Nov 2026); committed as that session's dated records |

### Done: Analysis tooling (from 7 Nov 2026)

*Moved here on 17 Nov 2026 from [Analysis tooling (from 7 Nov 2026)](#analysis-tooling-from-7-nov-2026); kept as written.*

| # | Item | Language | Status |
| --- | --- | --- | --- |
| T0 | Create phobos-modkit (public, MIT) with its documents | docs | Done: documents only |
| T1 | Log triage, with the RimWorld adapter | Python | Done: phobos-modkit 0.1.1, `scripts/triage-log.py`, AGENTS.md rule |
| T2 | Save index and query, working slice | Rust | Done: phobos-modkit 0.2.2, `scripts/query-save.py`, AGENTS.md rule |
| T2b | Save index: map grids (terrain, roofs, gas) and comparing two saves by load ID | Rust | Done: phobos-modkit 0.6.0, `grid` and `diff` in `scripts/query-save.py` |
| T3 | Mod and Def index (SQLite in the kit's cache) | Python | Done: phobos-modkit 0.4.0, `scripts/query-defs.py`, AGENTS.md rule |
| T4 | Assembly metadata reader | C# | Done for Harmony patches: phobos-modkit 0.14.0, `scripts/query-patches.py`, AGENTS.md rule (8 Nov 2026). Constants, strings and API checks left to REA once the owner has it |
| T5 | Workshop research | Python | Done: phobos-modkit 0.5.0, `scripts/workshop-research.py`, AGENTS.md rule |
| T6 | StartupImpact sessions | Python | Done: phobos-modkit 0.17.0, `scripts/startup-impact.py` (8 Nov 2026) |
| T7 | Version readiness scan | Python | Done: phobos-modkit 0.17.0, `scripts/version-readiness.py` (8 Nov 2026) |
| T8 | Release tooling | Python | Done for version bumps and changelog checks: phobos-modkit 0.10.1, `scripts/release.py`, `config/maintained-constants.json` (8 Nov 2026). Workshop publishing waits for the first public beta gate |
| T9 | Shared report model and derived-data cache | Python | Done: phobos-modkit 0.3.0 |
| T12 | The check gate: every build, test and check in one command | Python | Done: phobos-modkit 0.11.0, `scripts/check-all.py`, AGENTS.md rule (8 Nov 2026) |
| T13 | Mod settings snapshot | Python | Done: phobos-modkit 0.12.0, `scripts/mod-settings.py`, AGENTS.md rule (8 Nov 2026) |
| T14 | Decompiled-code index | Python | Done: phobos-modkit 0.13.1, `scripts/query-code.py`, AGENTS.md rule (8 Nov 2026) |
| T11 | Resolved-Defs dump: a D'thorga Framework dev action the kit indexes, so Defs show with patches applied | C# and Python | Done, not yet tested in game: Framework 0.27.0 and phobos-modkit 0.15.0, `scripts/query-defs.py patched` (8 Nov 2026) |

### Done: Modern military vehicles (18 Nov 2026)

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| CR1 | The sling checked room for a set-down, and drew the outline, with the game's footprint in place of Vehicle Framework's; the same fault in the hitch (H1) destroyed a vehicle at a map edge | Framework | Fix all six now (owner, 18 Nov 2026) | **Fixed: Framework 0.140.1**, not yet tested in game |
| CR2 | The sling's set-down caught a thrown error but not a spawn that fails silently; the hitch checks both (H10) | Framework | Fix all six now (owner, 18 Nov 2026) | **Fixed: Framework 0.140.1**, not yet tested in game |
| CR3 | The Health tab showed part armour as a percentage; under Combat Extended the figures are millimetres and megapascals | Framework | Fix all six now (owner, 18 Nov 2026) | **Fixed: Framework 0.140.1**, not yet tested in game: it uses the armour stat's own format |
| CR4 | Nothing of ours reads Combat Extended's "Use New Vehicle Armor" setting: air-combat and effect damage pick their scale by whether Combat Extended is active | Framework | Fix all six now (owner, 18 Nov 2026) | Done, no change needed: the armour figures are on Combat Extended's scale whenever it is active, whatever that setting, so our test is the right one |
| CR5 | Cook-off always passed Combat Extended's blast scale | Framework | Fix all six now (owner, 18 Nov 2026) | Done: harmless, since cook-off runs only with Combat Extended's ammunition system on; it asks like our other blasts from Framework 0.140.1 |
| CR6 | The order of our submersible damage prefix and Combat Extended's prefix on the same method was unverified; at equal priority the earlier-applied one ran first and could apply a hit the dive should refuse | Framework | Fix all six now (owner, 18 Nov 2026) | **Fixed: Framework 0.140.1**, not yet tested in game: ours now runs before every other prefix there |
