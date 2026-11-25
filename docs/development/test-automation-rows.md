# Test automation: the rows still without evidence (25 Nov 2026)

**Status (25 Nov 2026):** A list for the next batches of [test automation](test-automation.md) part 1. Built from an agent's
reading of every waiting row with no evidence (188 rows, 263 claims), reviewed by the session that mapped 26 of them into
`config/test-evidence.json` the same day. Nothing here is built; a row leaves this list in the change that gives it evidence.

**How to read it:** each claim of a row is sorted by what would stand for it. **A new note** names the place in the code where
the behaviour happens and a key in our style. **Held back** are mappings to keys another row already stands on, left out until
someone checks each pair tests one thing (then narrow them, or list them under `shared` with why). **The owner's eyes** are
claims about how something looks, sounds or reads: no note can stand for them. **Replaced** rows test something a later
version took away. The sites are an agent's reading of the code on 25 Nov 2026: check each before writing a note there.

**Done since:** rows 27, 71 and 31 by stages marked `logged` (Framework 0.290.0, the owner's card: "logged only, no lamps"),
13 rows by the first batch of notes (Framework 0.291.0), 12 by the second (0.295.0), 7 by the third (Framework 0.298.0,
Medical 0.27.0, Patches 0.45.0) and 21 by the fourth (Framework 0.301.0, Radio Framework 0.17.0), which also narrowed the
held-back pairs by case or listed them under `shared`. A stage the event log should keep without showing anything can be
marked `logged` the same way, with no C#.

## Need a new note (2)

| # | Row | Claim | Key | Where | Why |
| --- | --- | --- | --- | --- | --- |
| 28 | A worn engine | a worn engine burns oil faster and calls a service | vehicles.oil-burn-worn | mods/PhobosFramework/Mods/VehicleFramework/Common/Defs/Phobos/Framework/VehicleSystems/Systems_Lubrication.xml (oil burn; code not pinned) | Oil burn by engine health is a model figure; no note says the rate rose. |
| 169 | Hoppers shared with The Dead Man's Switch | the auto mill cuts from a linked hopper | factory.hopper-shared | src/PhobosFactory (hopper link to a DMS mill; not pinned) | Factory link use has no note. |

## Held back: an existing note another row shares (2)

| # | Row | Claim | Key | Where | Why |
| --- | --- | --- | --- | --- | --- |
| 87 | Old hidden pipes found, and pipes and conduits beneath every parked ve | a truck against a powered run charges | vehicles.grid-hook-up (charging only) |  | Noted for a real grid draw with no loads carried (CompPhobosVehicleSystems.cs:2781). |
| 511 | Under Combat Extended, blasts crack glass; the radio overlay | a near-miss blast cracks the glass | vehicles.glass (cracked) |  | Glass.Damage notes 'cracked' with cause 'a blast' from the explosion path (Glass.cs:256 to 143); the variant alone does not say blast. |

## Replaced (2)

| # | Row | Claim | Key | Where | Why |
| --- | --- | --- | --- | --- | --- |
| 82 | Pipes you can see, with Dubs Bad Hygiene | visible pipes laid |  |  | Visible pipes of 0.107.0 were replaced by hidden ones in Framework 0.108.1 (CHANGELOG 0.108.1, 'hidden again'; README note 'replaced by the hidden pipes of 0.108.1'); row 88 tests the replacement. |
| 87 | Old hidden pipes found, and pipes and conduits beneath every parked ve | old hidden pipe made visible |  |  | 0.108.0's visible plumbing was replaced by hidden pipe in Framework 0.108.1 (CHANGELOG 0.108.1; README marks the 0.108.0 notes replaced). |

## The owner's eyes only (162)

| # | Row | Claim | Why |
| --- | --- | --- | --- |
| 1 | A destroyed part needs replacing. This changes repairs on every vehicl | returns at 25%, finished free, no idle mechanic | Health figures and a mechanic's pacing are read on screen. |
| 2 | Part conditions: a hit radiator stays holed | clock line, slower leak, cleared by full repair | Wording and rate are read on the Health tab. |
| 3 | Temperatures | whole row | Temperatures are figures on the Systems tab; no notable stage marks 55 C or gearbox warmth. |
| 4 | Repairs in a caravan | button says why it cannot | Refusal wording on the button. |
| 5 | The game's log comes back after its limit | T2 line shows in the log window | The owner's look at the in-game log window. |
| 6 | The Health tab on the world map and the shared logger | whole row | Tab drawing and log prefixes are read by eye. |
| 9 | Part grades | +/-30% health, same after load | Health figures on the tab. |
| 10 | Stations | bench lists, machining table lists none | Bill lists on screen. |
| 11 | Generator set | cable end, lamp, pack up | Overlay and the lamp are seen on screen. |
| 12 | Depot bay | drives to the pad, quicker repair | Go to bay logs nothing (DepotBays.GoTo); repair speed is a figure. |
| 16 | Oil and groundwater | groundwater shown and deep well allowed | Shown on the ground by the survey (SurveyExtra_Groundwater). |
| 18 | Overdrive | pace, fuel, wear | Figures on the tabs. |
| 20 | Engine while set up | engine runs, overdrive heats | Read on the Dashboard. |
| 21 | Pipes beneath a parked vehicle | whole row | Overlay joins and the vehicle moving freely are seen in the overlay; the pipes are now hidden (row 88), so only the overlay shows them. |
| 25 | Ammo setup with Synthriada on | whole row | Bills listed again and a startup log line: read by eye. |
| 26 | Research tree | a new colony starts with them unresearched | The research screen in a new colony. |
| 27 | Heaters and the engine | cab warms after the coolant passes 30 C | Figures on the Systems tab. |
| 28 | A worn engine | running hot on a hot day | saved coolantTemperature 2 would fire for any heat, not this cause. |
| 29 | A burning vehicle | coolant limits, stifling cab, boil-over timing, jet's oil | Figures and timings on the tabs; coolantTemperature 4 is not fire-specific. |
| 30 | Cold starts and the block heater | cold start drains more; heated coolant near 35-40 C | Figure comparisons on the Systems tab. |
| 32 | The radiation-line alert | click jumps, clears on leaving | Alert behaviour on screen. |
| 34 | The drive belt | coolant climbs and charging stops together | Read on the Systems tab. |
| 36 | Diagnose, better fits and the pit | pit raises the bench's speed | A stat figure and the link line. |
| 37 | Service kits | settings box state in PG5 and a new colony | A settings checkbox. |
| 38 | The diagnosis report | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 39 | At a glance | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 40 | New pictures | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 41 | Graph marks and the picture | whole row | Pictures, sounds, names and tooltips; row 249 says it notes nothing. |
| 41 | Names | whole row | Pictures, sounds, names and tooltips; row 249 says it notes nothing. |
| 42 | The range on the world map | whole row | Pictures, sounds, names and tooltips; row 249 says it notes nothing. |
| 42 | A tune's figures | whole row | Pictures, sounds, names and tooltips; row 249 says it notes nothing. |
| 43 | Wear trends | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 44 | Fitting pictures | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 45 | Choices together | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 46 | Axles | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 47 | Finding tuning | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 48 | Earned fittings | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 50 | Aircraft readings | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 51 | The compact Systems tab | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 52 | The four passes | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 53 | Parts bills | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 54 | Tooltips that explain | whole row | Screen layout, pictures, tooltips or figures read by eye. |
| 58 | Service lead | charge climbs, 300 W, lead taken in | Figures and messages on screen. |
| 59 | Loading grab | stockpiled stack stays, Tip refused while out | Seen on the map and the button. |
| 60 | Water bucket | refusals, drawing | Wording and pictures. |
| 61 | Litter kit | tended, kept through load, unloaded, flight | Watched on screen. |
| 62 | Why the Harrier cannot be taken apart | whole row | The owner sends the trace lines from the log; the trace stays behind Detailed logs (Patches changelog). |
| 63 | The Louie and the Hind lift a vehicle | whole row | The hanging load's drawing, where it comes down and the grey-map watch are by eye; no note covers the Hind's carry to another map. |
| 65 | All-terrain tyres | less bogging, slower on road, tread renewed | Comparisons and Health readings. |
| 67 | Long range: extra fuel tanks | capacity, cargo and range | Figures on screen. |
| 68 | Recovery: the winch and the tow | a heavy vehicle refused with two figures | Refusal wording. |
| 70 | Hitch slice 3: reading a tow, and trailers on journeys | labels, Dashboard, world line | Wording on screen. |
| 71 | The electric night heater | charge and pack temperature figures | Figures on the Systems tab. |
| 72 | The generator set as a hybrid | speed, silence, fuel unchanged | Watched on screen. |
| 73 | A tank run dry | the parked drain says nothing | An absence. |
| 75 | A burning truck's tyres and battery | whole row | Wheel and battery health on the Health tab; bodyFire's stage does not show which parts lost health. |
| 77 | Loads switched off when the charging fails | order of shedding, no flicker | Watched on screen. |
| 78 | An engine out | 0.8 speed, half generator, no repeat | Figures and message count. |
| 79 | One flight-controls rule | other aircraft launch as normal | An absence. |
| 80 | Our vehicles' upgrades priced like tunes | whole row | Prices, refunds and the log list are figures. |
| 83 | The generator set through conduits beneath it | whole row | Conduits, the lamp and the greyed choice are seen on screen. |
| 84 | The Busbar at 3 x 8 | whole row | Look passed; output, footprint and the resized older Busbar are figures and looks. |
| 86 | The depot bay fills from a chemfuel pipe | whole row | Vanilla Expanded Framework's own refill; not our code. |
| 87 | Old hidden pipes found, and pipes and conduits beneath every parked ve | Busbar shows only its own conduits; no slowdown | Overlay and feel. |
| 88 | Hidden again, with Dubs Bad Hygiene | whole row | Hidden pipes, overlays and leavings are looks. |
| 89 | Vehicle sounds in the game's mix, D'thorga sounds volume at 100% | whole row | Loudness on the owner's speakers. |
| 90 | Plumbing joins up in the overlay | whole row | How the overlay joins. |
| 92 | EMP on vehicles | battery drop figures | Figures. |
| 94 | The tanker on Rimefeller's pipeline | hidden pipeline in overlay, nothing left behind | Overlay look. |
| 97 | Vehicle parts on the fabrication bay | whole row | Startup log line and the bay's bill list. |
| 100 | Navigation on the world map | whole row | Range and speed comparisons on the world map. |
| 101 | Heavy controls | halved flight control, slower; restored by a top-up | Figures; which service restores it is not pinned. |
| 102 | The avionics bay | whole row | Part listing and picture. |
| 104 | The Pintle at 3 x 7 | whole row | Size, picture and tow drawing. |
| 105 | The fuel bowser | contents chemfuel, no errors | Read on screen. |
| 106 | The water bowser and tank to tank (with Dubs Bad Hygiene) | hidden plumbing joins; fills from the tower | Overlay look. |
| 108 | The driver | healthy driver not slowed; nothing crashed | Absences. |
| 110 | Rivers and the snorkel | no damage with the snorkel or for a heavy truck | Absences. |
| 112 | The Health tab's bars and folded rows | whole row | Tab drawing, circles, log lines or paint: read by eye. |
| 113 | An aircraft's range | whole row | Tab drawing, circles, log lines or paint: read by eye. |
| 114 | Verbose logging | whole row | Tab drawing, circles, log lines or paint: read by eye. |
| 115 | The bowsers' picture and every vehicle's paint | whole row | Tab drawing, circles, log lines or paint: read by eye. |
| 117 | Search and Rescue's medics with our kits | whole row | Search and Rescue's responder order and log silence; no code of ours decides the responder's kit use that we could find. |
| 118 | RimPacts' allies and empty vehicles | whole row | Allied behaviour watched over an hour; the load line is a log read. |
| 132 | The reach ring with Vehicle Framework 1.7) | whole row | Size of a ring, whether the game allows a lone trailer, red errors: by eye. |
| 133 | A lone trailer) | whole row | Size of a ring, whether the game allows a lone trailer, red errors: by eye. |
| 134 | The Harrier on Vehicle Framework 1.7) | whole row | Size of a ring, whether the game allows a lone trailer, red errors: by eye. |
| 135 | Other factions' aircraft and a captured vehicle) | no messages about their engines | An absence. |
| 136 | What our vehicle code costs in a busy moment) | whole row | The owner sends the timing report. |
| 138 | Mk. 84 bombs from fills | whole row | Bills and loads at the bench. |
| 144 | Oxygen and the low cruise | shorter range and speed | Figures on the world map. |
| 148 | An empty vehicle on the grid charges; an autonomous turret draws only  | flat battery wording | Wording. |
| 151 | A Mongoose's rocket pods fire and the game carries on | whole row | The absence of a crash; vehicles.weapon-fired variants are turret keys whose Mongoose pod names were not verified. |
| 157 | The trailers' research | whole row | Research tab layout and refusals' wording. |
| 169 | Hoppers shared with The Dead Man's Switch | belt leaves the corpse | Watched on the map. |
| 175 | A belt's end and the grab load a truck, with nothing built | whole row | The owner's judgement whether this is good enough. |
| 180 | Accessories weigh what they weigh, and come back off | capacity falls by 49 kg | Stat figure and explanation. |
| 185 | Working vehicles burn by what the engine does | whole row | Figures, and the owner's judgement on thirst. |
| 191 | Billy's kettle takes water | no start-up error | Log read. |
| 196 | A message when a listed test happens | whole row | The test-row message itself is the Seen system; a note cannot stand for it. |
| 249 | A stockpile's unloading mark flies with the gravship | whole row | Pictures, sounds, names and tooltips; row 249 says it notes nothing. |
| 291 | The Thane tractors | whole row | Pictures, sounds, names and tooltips; row 249 says it notes nothing. |
| 292 | Upgrades drawn and working | whole row | Pictures, sounds, names and tooltips; row 249 says it notes nothing. |
| 307 | The jets' early fits and own upgrades | what each kit changes; older Harrier keeps its kit | Hover, magazine and temperature figures. |
| 313 | The UH-60M Black Hawk in the fold | whole row | Costs, the Systems tab, its own modes and interior; aircraft.engine-out-rating would fire for any twin, not the Black Hawk. |
| 330 | Other mods' open-bed trucks take a deck tent | whole row | Fittings offered and bed counts on other mods' trucks; the deck tent notes do not name the vehicle kind. |
| 342 | The EA-6B Prowler in the fold | whole row | Parts, jam ring width, dodging, rearming and speeds; aircraft.jammer-cover and aircraft.own-evasion do not show the ICAP widening. |
| 357 | The deck tent drawn on the Pintle flatbed | whole row | Pictures, comfort and weight figures. |
| 358 | The deck tent, roomy or light | whole row | Pictures, comfort and weight figures. |
| 359 | Camp gear drawn on our vehicles | whole row | Pictures, comfort and weight figures. |
| 360 | The fold-out camper, roomy or compact; clean roof tents | whole row | Pictures, comfort and weight figures. |
| 366 | The rolled bag on either side; the rear tent drawn | whole row | Pictures, comfort and weight figures. |
| 367 | The Busbar's Customize tab shows every upgrade again | whole row | Pictures, comfort and weight figures. |
| 368 | The trucks' armour kits drawn clean | whole row | Pictures, comfort and weight figures. |
| 371 | The radiation dev actions | whole row | Dev-action doses shown in the log and on the condition; removal staying removed is watched. |
| 375 | Shadow and shelter from a strike | whole row | Burns, blindness and the dose letter read by eye; radiation.shielded (row 372) is not the IRBM blast model. |
| 378 | The tanker's tanks and the Osprey's rotor drawn clean | whole row | Pictures, icons and bill lists. |
| 379 | The liquid tyre ballast's own icon | whole row | Pictures, icons and bill lists. |
| 380 | The new aircraft's weapons in line with the Mk. 82 | whole row | Pictures, icons and bill lists. |
| 381 | The new munitions at the automated benches | whole row | Pictures, icons and bill lists. |
| 396 | A cab with its glass gone heats faster in a fire | whole row | A comparison of two trucks' cab heat; PhobosLog_GlassGone shows only that the glass went. |
| 420 | The C-17 Globemaster III runs our vehicle systems, built from real par | whole row | Blueprint cost, Systems tab and its own modes. |
| 422 | Four engines by wing | whole row | Fuel and dodge cost per engine and button wording; vehicles.engine-starved fires for any twin, not the C-17. |
| 423 | The fleet brought in line: grit at a stock launch, and three more twin | never while standing; cross-feed buttons; ratings | Absences and buttons. |
| 424 | The Ka-27PS and Mi-38 fly their own engines' ratings | whole row | Tab listings, ratings and ranges read by eye, or the bindings tool's output. |
| 425 | The thruster craft are pulled by a thruster out and keep a tank each s | whole row | Tab listings, ratings and ranges read by eye, or the bindings tool's output. |
| 426 | The Systems and Health tabs agree | whole row | Tab listings, ratings and ranges read by eye, or the bindings tool's output. |
| 427 | A report of what every vehicle was bound to | whole row | Tab listings, ratings and ranges read by eye, or the bindings tool's output. |
| 429 | Every aircraft's fuel tanks and landing gear watched, and a vehicle's  | whole row | Tab listings, ratings and ranges read by eye, or the bindings tool's output. |
| 430 | The diesel shunter's fuel system has its parts | whole row | Tab listings, ratings and ranges read by eye, or the bindings tool's output. |
| 431 | The Cherokee's front and rear engines are part of its rotor drive | whole row | Tab listings, ratings and ranges read by eye, or the bindings tool's output. |
| 452 | The paste bowser | green stripe | Picture. |
| 456 | Radio coverage on the map | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 457 | Electrical noise fades at the edge of its reach | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 459 | The line from a colonist's radio to its antenna | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 460 | The side view in the link-budget window | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 462 | What the radio upgrades change | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 463 | The radio's new pictures | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 464 | The radio terminal | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 465 | Linear amplifiers | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 466 | The full-size ground-plane | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 467 | Setting: Radio terminals make radio noise | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 468 | The link budget as steps | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 469 | Lamps and needles | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 470 | The mast's warning light | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 471 | The coax glows with its signal | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 472 | The waterfall | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 473 | Setting: Show the waterfall on the radio desk | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 474 | Channels | counts and Net tab wording | Wording. |
| 476 | The desk's second transceiver | counts change at once | Wording on the station. |
| 477 | The waterfall on channels | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 478 | Channels by kind | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 484 | Vehicle kinds load quietly | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 486 | A vehicle's radio | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 489 | The remote control kit | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 491 | Signal bars on the Radio button | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 493 | The kit as a fitting | drops off the net when flat | Watched on screen. |
| 495 | The paid pictures of 25 Nov | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 504 | The colony's toilets never drain into a shower trailer | colony toilets never drain into the trailer | An absence. |
| 505 | The radio's sounds | whole row | Radio coverage pictures, sheets, sounds, tabs and wording read by eye. |
| 511 | Under Combat Extended, blasts crack glass; the radio overlay | radio overlay reach | Coverage picture. |
| 514 | Parachutes and the rigger's table | whole row | Repacking and refurbishing are bills at the rigger's table (Airdrop_RiggersTable.xml, uncommitted airdrop work); outputs, work time and the canopy's tint are watched. |
