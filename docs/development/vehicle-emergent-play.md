# Emergent play around the vehicles: where unplanned stories could come from (20 Nov 2026)

**Status (20 Nov 2026):** batch 1 built in D'thorga Framework 0.181.0 (EM24, EM23, EM13, EM25 and EM1: faults that say why, the
logbook, records and tales, the trip report, stranded convoys) and batch 2 in 0.185.0 (EM29(a): four unwise orders go ahead with a
warning), batch 3 in 0.187.0 (EM20, EM21(a) and (c), EM27's small links) and batch 4 in the same version (EM2 and EM3: hulls,
salvaged parts, lost convoys), and a small round in 0.188.0 (EM29(b) brakes, EM16 the snow forecast, EM18 the gravship's warning),
and EM11 with EM12 in 0.190.0 (hours on type, "their" vehicle), and EM5 in 0.192.0 (raiders' fuel by distance), and EM21(b)'s hull lift in
0.197.0; not yet tested in game. The four batches the owner asked for are built
(the owner's word, 20 Nov 2026); every other row is an idea, not built. The owner answered four of the questions the same day
([the answers](#questions-for-the-owner)).

It began as a research round the owner asked for on 20 Nov 2026: "suggestions/ideas/thoughts/brainstorming/etc on how we could perhaps
obtain/encourage further, emergent gameplay which I so dearly crave like ambrosia, with a focus surrounding our vehicle mods", and
"strictly a research and record purview". **Emergent play** here means stories nobody scripted, arising when rules meet each other.

**How to read the marks.** **[code]** read in code or Defs, with the place. **[P]** a primary source: the author's own page or words.
**[S]** a secondary source: someone else's account. **[I]** our inference. **[A]** an agent's choice or authored figure, for the owner to
revise. **[not read]** says so plainly. Short forms: `V/` is `src/PhobosFramework.Vehicles/`; `Comp` is
`V/Systems/CompPhobosVehicleSystems.cs`; **G:n** is a line of the game's decompile (`.local/decompiled/full/all.cs`); **VF:n** a line of
the installed Vehicle Framework 1.7 build's decompile (`.local/decompiled/VehicleFramework-1.7-2026-11-18/Vehicles.decompiled.cs`);
**VFsrc** its public source at commit 026420b5 (17 Nov 2026, not checked against the installed build); **VFxml** the XML it ships.

**Who read what.** Five survey agents read the code on 20 Nov 2026 with `scripts/query-code.py`, `scripts/query-defs.py` and
`scripts/query-patches.py`. The agent writing this record re-read the lines the main findings rest on (listed under
[Sources](#sources)); every other place is as the surveys reported it. **Re-read a line before building on it.**

## Read this first

- **Our vehicles are deep inside and thin outside.** Each carries a detailed model. But our vehicle code has no incident, quest,
  storyteller part, record, tale or memory thought of its own **[code]**. Emergence needs links outward: to other factions, places,
  colonists' minds and the game's own incidents. **The agent's candid view:** the next effort belongs there, not in another gauge.
- **People aboard a vehicle are not on the map, as the game counts it.** So any vehicle, an open bike included, is today a perfect,
  free shelter from toxic fallout, pollution, Anomaly's unnatural darkness and manhunting animals **[code]**
  ([audit 2](#audit-2-the-games-incidents-and-the-vehicles)). It is a rescue nobody asked for.
- **Our verbs judge the load by weight and the object by ownership.** The winch and the sling accept only the player's vehicles;
  nothing else can be pulled or lifted, whatever it weighs **[code]** ([audit 1](#audit-1-verbs-and-objects)).
- **Sixteen of our refusals stop something unwise, not something impossible.** Four have their consequence already built; three
  have none, so lifting those today would cost the player nothing **[code]** ([audit 3](#audit-3-refusals-impossible-or-only-unwise)).
  **A correction to the plan this record came from:** it gave "driving on worn-out brakes" as the example to lift. Nothing models a
  vehicle that cannot stop, so that one needs a consequence first.
- **A fault's cause is known when the fault is made, then dropped** at two places in the code. The event log keeps 15 days and
  nothing records who drove or who repaired **[code]**.
- **What is lost vanishes.** A convoy whose last colonist dies is deleted with its vehicles after one letter, and a destroyed
  vehicle leaves no hull unless a caller asks, and none does **[code]**.
- **Four ideas changed size once the code was read.** Smaller: exhaust indoors (our carbon monoxide gas exists) and the gravship
  as an expedition ship (the game already carries vehicles). Larger: a stolen vehicle and traders arriving by road (the game and
  Vehicle Framework have no opening for either).
- **A vehicle counts toward colony wealth as a pawn, at full value**, where a building counts half **[code]**. A large fleet
  already raises raid strength.
- **Submarines and radio are on the back burner** (owner, 20 Nov and 9 Nov 2026). The audits list the submarine's refusals for
  completeness; no row proposes work on either.
- **The owner's answers (20 Nov 2026):** random rolls only as a last resort; shelter aboard a vehicle waits for the cab-air model;
  the four unwise orders whose consequence is built are to warn and then be allowed; the groups of most interest are reading the
  story (F), a failure opening play (A), and home and incidents (E, G).
- **[The short list](#the-short-list)** and **[the questions for the owner](#questions-for-the-owner)** are at the end.

## The principle

### What designers of such games say

| Who | The point | Source |
| --- | --- | --- |
| Harvey Smith, lead designer of Deus Ex | "You could define emergence as an event that occurs, but that could not have simply been inferred from a system's rules." He counts as failures the moments that "rely on special-case triggering or scripting", and says: "As designers, we are creating a possibility space for the player to explore." | **[P]** [The Future of Game Design](https://www.witchboy.net/?p=22), a lecture transcript on his own site; the page names the MIM conference; its date was not seen on the page |
| Tynan Sylvester, designer of RimWorld | His Player Model Principle: "The whole value of a game is in the mental model of itself it projects into the player's mind." Whatever in the game's model does not reach the player's model "is worthless". Too many relationships and "the whole system appears as a gigantic hairball of complexity". Story-richness is his term for the share of interactions that interest the player. Hair complexity is his term for "pieces of the simulation that don't affect anything outside themselves": cheap, and ignorable. | **[P]** [The Simulation Dream](https://tynansylvester.com/2013/06/the-simulation-dream/), 2 June 2013, his own site; also hosted by Game Developer |
| The same essay | "Apophenia is seeing meaningful patterns in random or meaningless data." Players read intent and personality into a system that holds neither. | **[P]** as above |
| Nintendo's Breath of the Wild team (H. Fujibayashi, S. Takizawa, T. Dohta), GDC 2017 | "Multiplicative gameplay", and a "chemistry engine" with three rules: "Elements can change the state of materials"; "Elements are able to change each other's states"; "Materials cannot influence each other's states". | **[S]** Daniel New, [Thumbsticks, 2 Mar 2017](https://www.thumbsticks.com/gdc-17-breath-of-the-wild-science-lies). Only this write-up lists the three rules. The talk is on the [GDC Vault](https://www.gdcvault.com/play/1024562/Change-and-Constant-Breaking-Conventions); its video was **not watched** |
| Clint Hocking, creative director of Far Cry 2 | Small losses (a jammed rifle, a malaria attack) push the player from carrying out a plan back to making one: "you rarely die from these events – unjamming a weapon takes no more time or effort than reloading does." | **[S]** Matthew Gallant quoting Hocking's GDC 2009 talk, [Intentionality & Improvisation, 28 Feb 2018](https://gangles.ca/?p=465). Hocking's own slides could **not** be retrieved |

### What follows for us (our inference, not the sources')

Six conditions, and where our vehicles stand on each **[I]**:

| # | Condition | Where we stand |
| --- | --- | --- |
| 1 | **Systems meet through shared quantities**: fuel, power, heat, weight, time, goodwill | Strong inside a vehicle (the engine loads ledger). Weak outward: little of it reaches the colony or the world |
| 2 | **State decides, not a hidden roll** | Strong. Earlier records argued against random jams (T11) and fuses (G19), and the owner chose "No random crashes" for [hotboxing](vehicle-hotboxing.md) |
| 3 | **A failure leaves a situation**, not an end | Partial. Field fixes and the recovery tow exist; a stranded convoy only stops, and what is lost vanishes |
| 4 | **The same rules for everyone** | Partial. Other factions run the lite tier (owner, 11 Nov 2026, not reopened here). People aboard are exempt from most of the map |
| 5 | **The world remembers** | Weak. A 15-day log; no former owner; no records or tales |
| 6 | **The player can read the cause** (Sylvester's principle) | Partial. The diagnosis report and "every refusal says why" are good; the cause of a fault is dropped |

**In a chemist's terms [I]:** Nintendo's three rules read like general reactivity rules. A few reagents with general rules give more
products than a list of named reactions. A verb that tests properties (weight, fluid, power) is the general rule; a verb that
tests a list of names or an owner is the named reaction. The repository's own "match effects, not names" says the same of Defs.

**Agent default, for the owner to confirm (question 1) [A]:** *state decides what happens; the game's own storyteller decides when
pressure arrives; we add no random rolls of our own.* Every row below is judged against it.

**The owner's answer (20 Nov 2026, card): rolls only as a last resort.** The default stands, with one opening: a roll is allowed
where a real cause exists and cannot reasonably be modelled, and each such roll is named in its design record.

### What other games do with vehicles and supply

| Game | The mechanism | Source | What it suggests here [I] |
| --- | --- | --- | --- |
| SnowRunner (Saber Interactive) | The ground is the opponent. Lead designer Vitaliy Yaruta: before each mud pool the player asks "Do I have an object to winch to if I do get stuck?", and "Off-roading is a battle, not a safe sightseeing journey, after all." | **[P]** [GamingBolt interview, 5 May 2020](https://gamingbolt.com/snowrunner-interview-physics-progression-customization-and-more) | A winch needs something to pull against and something to pull: EM21 |
| Workers & Resources: Soviet Republic (3Division) | Vehicles age and need repair stations: "broken-down vehicles can cause traffic jams and delays, leading to decreased productivity and unhappy citizens." The player sets the repair interval. | **[P]** [Report for the Community #72, 16 Apr 2023](http://www.sovietrepublic.net/post/report-for-the-community-72/) | Supply runs by real vehicles that can fail: EM15 |
| Far Cry 2 (Ubisoft Montreal) | Weapons and vehicles degrade; the player gets out to work on the engine | **[S]** Gallant, above; [Engadget, 29 Aug 2008](https://www.engadget.com/2008/08/29/far-cry-2-trailer-talks-player-immersion/) | Our field fixes are the same idea: keep them quick and disruptive, never fatal |
| Farming Simulator 25 (GIANTS Software) | The harsher parts are optional. Martin Rabl's advice to beginners: "deactivate many optional elements like seasons, weeds, need for lime" | **[P]** [UnGeek interview, 16 Dec 2024](https://ungeek.ph/2024/12/interview-giants-softwares-martin-rabl-on-the-stand-out-features-of-farming-simulator-25-and-more) | The owner's rule already: balance sets the default, and a harsher mode is a setting (EM29) |
| HighFleet (Konstantin Koshutin) | Switching radar on lets enemy fleets find you | **[S]** [Cultured Vultures guide, 26 Jul 2021](https://culturedvultures.com/?p=161087). Fuel and tankers: **unverified** | Radio is on the back burner; noted only |
| Kenshi (Lo-Fi Games) | "World states": the world reacts to the deaths of notable people; towns change hands | **[S]** [Wikipedia](https://en.wikipedia.org/wiki/Kenshi_(video_game)), an uncited sentence. Weak | Where enemies live should change what they can do to you: EM5 |
| Jagged Alliance 2 (Sir-Tech) | Vehicles move squads between sectors and must be paid for and refuelled; militia hold towns against counter-attacks | **[S]** two reviews ([Turn Based Lovers](https://turnbasedlovers.com/review/jagged-alliance-2-cult-classic-review/), [Just Games Retro](https://www.justgamesretro.com/?p=8379)). Enemies retaking towns: **unverified** | Noted only |

**Two real cases, for colour.** Mawson's Australasian Antarctic Expedition took a Vickers monoplane whose "wings were damaged beyond
repair on 5 October 1911, during a practice flight in Adelaide"; it went south wingless as an "air tractor" and made "a successful
depot-laying trip with 700 pounds of supplies on 2 December" 1912 before its engine failed on its next trip (**[P]** Dr Chris
Henderson, [Australian Antarctic Magazine 18, 2010](https://www.antarctica.gov.au/magazine/issue-18-2010/history/the-search-for-mawsons-air-tractor/),
Australian Antarctic Division). A machine built for one job, pressed into another, laying fuel ahead of the journey: EM4 and EM21
in one story. And the 2021 Ig Nobel Transportation Prize went to a study of rhinoceroses hung upside down by the feet, as they are
when moved by helicopter (**[P]** [the prize list](https://improbable.com/ig/winners/); Radcliffe and colleagues, Journal of Wildlife
Diseases 57(2), 2021; the animals in the study hung from a crane, not a helicopter). A sling that judges by weight would take one.

## What exists today

### Outward hooks our vehicles have

All **[code]**. This is the whole list.

| Hook | Where |
| --- | --- |
| Claiming a non-hostile faction's vehicle costs 30 goodwill and fires our one history event, `PhobosVehicleTaken` | `V/Jobs/ClaimVehicle.cs:61-79`, `Claim.xml` |
| The drill rig rolls the game's own deep-drill infestation with the storyteller's figures | `V/Systems/CompPhobosDrillRig.cs:303-335` |
| The survey truck's long-range survey starts the game's own mineral-lump quest | `V/Systems/CompPhobosSurveyTruck.cs:192-198` |
| Raiders may arrive by Osprey | `V/Flight/RaiderFlight.cs` |
| The cab's temperature feeds one mood thought; an engine's heat goes into the room it stands in | `V/Service/ThoughtWorker_PhobosCab.cs`; `V/Schema/Effects.cs:179-193` |
| Letters: the diagnosis report, the parked-vehicle warning, a garage closing. One alert: crew in danger | `Letters.xml`; `V/Service/FleetService.cs:121-152` |

A search of every mod of ours found **no** RecordDef, TaleDef, memory ThoughtDef, InteractionDef, ritual, precept, IncidentDef or
QuestScriptDef. What exists of those kinds: two situational thoughts (`PhobosVehicleSystems_CabComfort`, `PhobosRadio_InTouch`), one
HistoryEventDef, one LetterDef and two world objects (`PhobosSlingWreck`, `PhobosOutpostGarage`).

### Pieces already recorded elsewhere

So that nothing is proposed twice. A row here points at these and adds only what is new.

| Already recorded | Where | State |
| --- | --- | --- |
| A dropped sling load becomes a world-map wreck site | [the sling](archive/vehicle-sling-design.md) | Built |
| Wreck on death and salvage | [vehicle health](vehicle-health-design.md), slice 3 | "Not now", open |
| Crews bail out; turret blown off; thrown tracks; field re-fit | [knocked out, not destroyed](vehicle-knocked-out-systems.md) | Mostly built |
| Raiders kidnap into a cage trailer; captives at enemy bases | [prisoner transport](vehicle-prisoner-transport.md) | Answered, not built |
| Remote farms deliver home by real convoys (RF6) | [farms on other tiles](farming-remote-farms.md) | Record only |
| The vehicle as the heart of a camp (Y1 to Y23) | [accessories](vehicle-accessories.md) | Only the power budget built |
| Driver health, weather grip, wet brakes, dust, darkness (G1 to G20) | [vehicle cross-links](vehicle-crosslinks.md) | Mostly built |
| Descents and engine braking (WE3) | [world elevation](world-elevation-and-descents.md) | Held |
| Smoking in the cab, and a cab-air model | [hotboxing](vehicle-hotboxing.md) | Record only |
| Standing crews and convoys (G5) | [garage and fleet tools](vehicle-stations.md) | Set aside: another mod does crews |
| The claim order; other factions on the lite tier | [other factions' audit](vehicle-other-factions-audit.md) | Built |

## Audit 1: verbs and objects

**The question:** for each thing a vehicle of ours can *do* to something else, what may that something be, and how does the code
decide? All **[code]**.

**The pattern:** the load is tested by a property (weight, pull needed, fluid kind), but the object is tested by class and by
owner. Every verb that acts on another vehicle asks for the player's faction. Only the claim order reaches another faction's
vehicle; after a claim every other verb opens.

| Verb | The test | Works on today | Left out, with nothing physical against it |
| --- | --- | --- | --- |
| **Tow and hitch** | A trailer comp, or with a winch fitted a land vehicle under the tractor's mass times a ratio; then the player's faction (`V/Hitch/CompPhobosHitch.cs:84-112`, `:98`; `V/Hitch/Recovery.cs:45-75`) | Our trailers; any of the player's land vehicles | Another faction's abandoned vehicle before a claim; an aircraft on its wheels |
| **Winch** | The player's land vehicle, within reach; pull needed against pull available (`Recovery.cs:118-154`, `:137`). The targeter accepts only a vehicle (`:314`) | The player's land vehicles, stuck or not | Chunks, hulls, items, a carcass, a tree, a wall |
| **Sling** | The targeter accepts only a vehicle (`V/Sling/CompPhobosSling.cs:833`); the hook holds only vehicles (`:38`); the player's faction (`:158`); no aircraft; then weight against the rated load and the hover margin (`:80-96`, `:172-181`) | The player's ground vehicles and trailers | Item stacks, packed buildings, chunks, a downed aircraft, a hull |
| **Hoist and litter** | The hoist: any pawn that is ours, or downed and not hostile, by body mass and gear. The litter's manual order: downed and not hostile (`V/Litter/CompPhobosLitter.cs:72`, `:338`). Automatic collection: the player's people only (`V/Litter/LitterService.cs:92`) | Colonists; by hand, any downed stranger who is not hostile | A downed hostile carried as a prisoner; a stranger collected automatically |
| **Pump and tank** | Fluids are Defs matched by phase, open to XML (`V/Tanks/CompPhobosTank.cs:689-713`). Ports: ground stacks, any same-faction vehicle by fuel type, any same-faction refuelable building, pipe networks (`V/Tanks/TankPorts.cs`) | Other mods' vehicles too, by fuel type | Rivers and lakes (no port draws water from the ground); another faction's tank |
| **Water drop** | Any unroofed water cell; puts out any fire (`V/Sling/WaterDrop.cs:118`, `:173`) | Rivers, lakes, sea | Filling from a water tank or bowser |
| **Power** | The service lead: the player's vehicle with our battery (`V/Systems/CompPhobosGeneratorSet.cs:459-469`). The generator set feeds a whole power net | The grid; the player's vehicles | One building off the grid; a vehicle without our systems |
| **Light** | A glower in the game's own glow grid, with no owner test (`V/Systems/VehicleLight.cs:51-68`) | Whatever the game does with light **[I]** | Nothing |
| **Gather, tip, deliver** | Items by the player's filter; the grab refuses corpses and packed buildings (`V/Systems/CompPhobosTipper.cs:428-432`) | Chunks and any item | Packed buildings |
| **Claim** | Any vehicle not ours, unattended and unguarded; never mechanoids' (`V/Jobs/ClaimVehicle.cs:22-34`) | Any faction's vehicle | Nothing |
| **Repair, jump-start** | The player's vehicle with our systems (`V/Jobs/ServiceJobs.cs:29-61`; `V/Jobs/JumpStart.cs:182`) | The player's vehicles | Nothing physical: an owner test |

**Knock-on effects that already exist**, and are the good kind: towed and slung weight load the engine and wear the brakes;
weather grounds a lift; a crash drops the load; the pump needs the engine or the electric pump; the winch is an electrical load.

## Audit 2: the game's incidents and the vehicles

**The question:** for each incident and condition in the owner's game, what happens to a vehicle and its crew today?

**The core fact [code]:** people in a vehicle are not "spawned". Vehicle Framework's own test asserts that the map's list of
spawned pawns holds the vehicle and not its passenger (VFsrc `Test_MapPawns.cs:113-119`), and it refuses every health condition on
the vehicle itself (VFsrc `Patch_HealthAndStats.cs:170-177`). Most conditions that act on people walk that list. So a vehicle is
a total shelter, and it does not matter whether it has a roof.

**Tool limits:** Def lists are as written, because the patched-Defs file is out of date (written 8 Nov 2026 with 335 mods; 364 are
active). `query-patches.py` cannot list Vehicle Framework's own patches, so its source was read.

| Incident or condition | What happens to a vehicle or its crew today | The smallest missing link |
| --- | --- | --- |
| **Solar flare**; Royalty's EMI field | Nothing. Only the power net and a few comps read it (G:423505, 579070); neither Vehicle Framework nor our code mentions a flare. Our EMP hardening acts on EMP damage only (`V/Service/VehicleEmp.cs:14-21`) | Read the flare by hardening class (`V/Rules/EmpRule.cs`): mechanical engines untouched, others will not start. **A gameplay choice [A]:** it follows the game's fiction of a flare, not real space weather |
| **Cold snap, heat wave** | Built. A closed cab answers for its crew's temperature (`V/Service/CabClimate.cs`); the air's temperature feeds the engine, battery and cold starts; a heat wave raises the chance a landing sets dry growth alight (`V/Flight/LandingSurface.cs:91`) | One alert when a snap begins, naming the vehicles that will not start |
| **Toxic fallout**; Rimatomics' fallout | Crew aboard take nothing, in any vehicle: the condition walks spawned pawns only (G:382937) | An open vehicle passes it to its crew; a closed cab shelters (EM22) |
| **Pollution, acidic smog** | No pollution build-up aboard (G:73719). The smog *mood* still lands on crew in the open, closed cab or not (G:382693-382726) | A closed cab counts as indoors for the mood |
| **Unnatural darkness** (Anomaly) | Crew aboard are immune (G:383078, 383094) | Headlights as a lit pool for a crew on foot |
| **Eclipse, volcanic winter** | Built: driving unlit in the dark slows the vehicle (`V/Schema/Inputs.cs:226`) | None |
| **Flashstorm** | Already a situation: lightning is a fire blast on an open cell (G:429878-429883); explosions reach a vehicle's edge cells (VFsrc `Patch_Combat.cs:207-216`); then our cook-off, fire on tyres and battery, and the crew alert run | None |
| **Rain, fog, snow** | Built: Vehicle Framework's speed, our grip and dust | None |
| **Psychic drone** | Crew aboard are affected: the thought reads the map they are held on (G:338071-338078) | None |
| **Manhunter pack, mad animals** | A vehicle is not a target unless its Def says so (VF:51161, `manhunterTargetsVehicle`, off by default). Of the active vehicle mods only two bikes set it. Crew cannot be bitten | It is a per-vehicle setting of Vehicle Framework's own, which the owner can tick today |
| **Raids, infestations, mech clusters** | A vehicle is a target only with someone aboard or a working turret (VFsrc `Patch_PawnAi.cs:26-33`). An empty, unarmed one is ignored | Raiders who burn or take empty vehicles (EM6, EM28) |
| **Meteorite, ship chunk** | The landing-spot check has no vehicle test (G:209071-209128); the blast reaches edge cells | A winch or crane that salvages the chunk (EM21) |
| **Short circuit** | Never from a vehicle: the culprit is the game's own conduit Def only (G:710975-710990), and ours is its own Def | A short on a hooked-up vehicle's net gives it our shorted wiring |
| **Caravan ambush, demand, meeting** | They fire for a vehicle convoy: it is a caravan (VF:93316) and Vehicle Framework patches no caravan incident. The ambush map is built and the vehicles placed at its edge (G:788350-788366) | Nothing needed for the default; a "drive through" choice would be a roll |
| **Refugee pod crash** | The litter's manual order takes a downed stranger who is not hostile; automatic collection does not (audit 1) | Automatic collection for a downed, non-hostile stranger: one test |
| **Trader caravan, visitors** | Nothing touches vehicles (EM7) | See EM7 |

**Not read:** the chased-refugee quest, gravship events, wastepack infestation, mech cluster placement, drop pods landing on a vehicle.

**Wealth [code].** `WealthWatcher.ForceRecount` adds the market value of every player pawn on the map, and a vehicle is one
(G:429329-429338). Map wealth is items, plus half of buildings, plus pawns (G:76614), so a vehicle weighs twice what a building of
the same value does. Its cargo counts as items (G:429393-429404); a caravan counts at 0.7 (G:785879-785892). A vehicle adds no
per-pawn raid points (G:391752-391805). Neither Vehicle Framework nor our code touches any of it.

## Audit 3: refusals, impossible or only unwise

**The question:** the owner's view is that a choice the player knowingly made should have its full consequence. For every place we
stop the player: is the thing **impossible** (A), only **unwise** (B), a **rule** of the game or another mod (C), or a guard against
a **bug** (D)? All **[code]**. Counts, leaving out the work rigs' small guards: A about 22, **B 16**, C about 9, D about 5.

**How refusals work.** A system's stage carries a refusal effect; the first one in force answers, and **only for the player's
vehicles** (`Comp:2471-2474`: "another faction's raid or caravan must never be left stranded by a flat battery"). One setting,
`showOnly` ("figures only"), already switches every stage refusal off at once (`V/Schema/Effects.cs:25`). It does not touch the
flight, sling, hitch or dive checks, which are written in code.

### The class B refusals

| The player tried | Where | If the refusal were lifted, what would our model do? |
| --- | --- | --- |
| **Cut the engines in the air** | `V/Flight/CompPhobosFlight.cs:967-989`: refused, the engines restarted, and a landing made within 15 cells | **Built:** the fall and the hurt aboard exist (`:979`, `:1405-1413`) |
| **Lift a load over the hook's rating, or in a storm** | `V/Sling/CompPhobosSling.cs:120-128`, `:172-181`; settings `slingWeather`, `slingHeatDerating` | **Built:** the drop, the crash and the fall damage (`:681-705`) |
| **Drive on a worn-through clutch** | `Systems_Chassis.xml:513` | **Built:** the clutch keeps a quarter of the drive (`V/Rules/BrakeWearRule.cs:35-45`) |
| **Tow more than 1.5 times the tractor's mass** | `V/Hitch/Recovery.cs:69-72` | **Built:** towed weight already raises brake and clutch wear (`V/Schema/Inputs.cs:334`) |
| Hover or lift off too heavy | `V/Service/HoverLimits.cs:30-37` | **Already a setting** (`hoverLimit`): refuse, allow with damage, or warn only |
| Take off blind (broken instruments, dark or fog) | `Systems_Avionics.xml:70,84` | **Already a setting** (`blindFlightWarnOnly`), but a message only: **no** disorientation or crash model |
| **Drive on worn-through or wrecked brakes** | `Systems_Chassis.xml:282,338` | **Nothing.** Wear stops at its limit; no stopping distance, no collision |
| Fly with flight controls too damaged | `Systems_FlightControls.xml:34` | **Nothing:** Vehicle Framework wires no crash |
| Drive an electric vehicle whose battery is in thermal runaway | `Systems_Electric.xml:165` | The fire and pack damage already run; lifting adds a burning car that drives |
| Move while set up with hoses out; mix cargoes in a tank; lower the generator's outriggers on bad ground | `V/Systems/CompPhobosSetUp.cs:520-523`; `V/Tanks/CompPhobosTank.cs:701-709`; `V/Systems/CompPhobosGeneratorSet.cs:212` | Nothing modelled **[I]** |
| Dive on a low reserve or with damaged trim | `V/Systems/CompPhobosSubmersible.cs:152-166` | Partly built (the reserve drain, stale air, the owner's "stay down" order). **On the back burner with submarines** |

### Rescues nobody asked for

| Rescue | Where | Ordered by the player, or something that happened to them? |
| --- | --- | --- |
| Engines cut in the air are restarted and the aircraft landed | `V/Flight/CompPhobosFlight.cs:967-989`, `:1420-1447` | **Ordered** |
| A tow that is blocked stops the tractor before the trailer hits | `V/Hitch/CompPhobosHitch.cs:277,296` | **Ordered** |
| An engine lost on a world-map flight turns the aircraft back | `V/Service/CruiseHeight.cs:210-221` | Happened to them |
| A low-fuel aircraft flies on where it may not set down | `V/Service/CargoRefuel.cs:206-218` | Happened to them |
| A submarine surfaces by itself (two cases the owner's "stay down" order overrides, two it does not) | `V/Systems/CompPhobosSubmersible.cs:200-207` | Happened to them; back burner |
| **Anyone aboard any vehicle is sheltered from most of the map** | Audit 2's core fact; Vehicle Framework's, not ours | Neither: it is free |

`Effect_ForceUndraft` (`V/Schema/Effects.cs:167-172`) has no user in any Def.

## The ideas

Each row says what the reads found, then what the idea **lets** the player do, **stops** them doing, and how it **changes** the way
they play. "Size" and every verdict are the agent's, for the owner to overrule **[A]**.

### A. A failure opens play instead of ending it

| # | Idea | What the reads found | Lets | Stops | Changes | Other factions | Size and verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EM1 | **Stranded is a state with choices.** An alert names the convoy, the vehicle and the one thing it waits for, what aboard would fix it, and how far home is. Supplies are counted across the whole convoy | A refusal halts the convoy (`V/Service/Patches.cs:359-373`). The player gets an inspect line and a red row; no alert, no letter (`V/Service/CaravanComp.cs:48-54`). A repair at a halt takes supplies from **that vehicle's own cargo only** (`Comp:2737-2740`). The game's Camp command works for a vehicle convoy (VF:93316; G:786311) | Choose between fixing it here, camping, fetching a part from home, or leaving it | A convoy sitting stopped and unnoticed | What you load before leaving (spares, a booster pack) becomes a decision | None: only the player's vehicles refuse | Small. **Take, early** |
| EM2 | **What is lost stays in the world.** A convoy that loses its last colonist becomes a site holding its vehicles and cargo; a vehicle destroyed on a map leaves a hull | A convoy whose last owner dies is cleared and destroyed, vehicles too, after one letter (VF:93569-93592). `Kill` spawns a hull only when asked, and no caller asks (VF:18182-18288). Our sling wreck holds any vehicle with its riders, never expires, and is general in code (`V/Sling/SlingWreck.cs:57-81`, `:150-195`) | Go back for it; learn what happened | — | A loss becomes a later journey. No value from nothing: a hull needs parts and work | A raider's hull is salvage after a claim, as now | Medium. **Take after EM1.** The owner already chose wreck sites for sling loads |
| EM3 | **Strip a hull for salvaged parts**, which can go in only at the salvaged grade | Parts are plain stackable items with no wear (`Parts.xml`). The salvaged grade exists on the slot: cost x0.7, health x0.7, fault x1.5, holds no fitting (`PartGrades.xml`; `V/Systems/VehicleRepairs.cs:146-165`). A replaced part is used up; nothing is recovered (`:301`, `:423-433`) | Get home on what the field offers | — | A wreck is worth a detour; a salvaged part fails sooner, by the factor that exists | A captured vehicle can be stripped like any | Medium. **Hold until EM2.** The yield must stay under the cost of making the parts [A] |
| EM4 | **Depots and forward fuel points.** Leave a bowser or fuel cans at a tile; an aircraft sets down at it. **Designed 20 Nov 2026** in [its own record](vehicle-depots.md) (DP1 to DP6), nothing built | An aircraft refuels only from its own hold (`V/Service/CargoRefuel.cs:112-127`). Nothing of ours leaves a trailer or cans on a tile (`V/Hitch/TowTravel.cs:9-31`). Vehicle Framework's parked stash is deleted after 15 to 30 days (VF:98192-98217); our warning letter exists | Reach past one tank's range by planning | — | Range is built by earlier trips: a tanker run before a long flight | Not proposed that others raid a depot: that would be a roll | Medium. **Take when aircraft range matters in the owner's play** |

### B. Everyone drives, not only the player

| # | Idea | What the reads found | Lets | Stops | Changes | Other factions | Size and verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EM5 | **Raiders arrive with the journey behind them.** A raid vehicle's fuel at arrival is a full tank less the trip from that faction's nearest settlement [A formula], set once | A raid carries its faction only, no origin (G:383647-383752). The distance is the least `ApproxDistanceInTiles` over that faction's settlements (G:781901). One clear place sets a non-player vehicle up as it appears (`Comp:611-620`). On a retreat a vehicle drives out with those aboard (VF:3838-3868). **A vehicle that cannot move is sabotaged by its own side** (VF:4046). Ground raid vehicles' starting fuel is Vehicle Framework's: **not read** | Outlast a raid's fuel; destroy a near base so later raids arrive drier | — | Where enemies live matters | **This row is for them.** The lite tier is untouched: fuel is Vehicle Framework's own figure | Small, after a read. **Read first** what the sabotage order does to a dry truck, then take |
| EM6 | **A stolen vehicle comes back.** Raiders drive off in an unattended, fuelled vehicle; it keeps its name, paint and log and turns up later | **Larger than first thought.** The game steals haulable items worth 320 or more within 12 cells (G:299076-299142); a vehicle is a pawn, never such an item. Nothing in Vehicle Framework lets an AI pawn board a vehicle not its own (VF:3241-3272). Nothing remembers a former owner (VF:16813-16816; `V/Jobs/ClaimVehicle.cs:68-76`) | Guard the motor pool, or bait it | Leaving fuelled vehicles unattended at no risk | Parking, fuel left in the tank and who is nearby become decisions | The whole row | Large: a raid behaviour and a job of our own, and a saved field. **Hold** |
| EM7 | **Traders and guests by road** | **Larger than first thought.** The game refuses a whole trader caravan if any carrier is not a pack animal (G:448948-448953); a vehicle is legal only as a guard. Hospitality destroys any generated guest that is not humanlike (its `IncidentWorker_VisitorGroup.cs:352-369`, public source). Vehicle Framework can put vehicles into outlander traders' stock behind its own setting (VFxml). RimMarket's convoys are world objects with a cargo number, no vehicles | Sell fuel, parts and repairs to callers | — | — | The whole row | Large: an arrival incident of our own. **Hold** |
| EM8 | **The roadhouse.** A colony on a road as a fuel and repair stop | Roads built with Rails and Roads of the Rim do speed vehicles (VF:78286-78328). Everything else waits on EM7 | — | — | — | — | **A direction, not a proposal** |
| EM9 | **Calls for help built from real state.** A quest whose request is a named part from our fault table, a tow, or a casualty flight | No quest of ours or of any vehicle mod exists. The game's closest shapes: `TradeRequest` and `OpportunitySite_DownedRefugee`. The survey truck already starts a game quest. The storyteller decides when, which fits the default on rolls | Use the fleet for others, for goodwill | — | Spares and a recovery truck earn goodwill; each call differs because the fault differs | An ally's stranded vehicle is the subject | Medium: quest XML and one part in code. **Take after EM1.** Its text would be agent-authored and held for review |
| EM10 | **Repaint to hide a taken vehicle** from its old owners | Paint lives in Vehicle Framework's own pattern data (VF:13457-13471); no former owner is remembered | — | — | — | — | **Idea only.** A gameplay simplification [A]; needs EM6's saved field |

### C. People in the machine

| # | Idea | What the reads found | Lets | Stops | Changes | Other factions | Size and verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EM11 | **Hours on type.** A colonist's hours on one kind of vehicle give small gains in wear and landing margin [A], never speed | The driver rule reads consciousness, sight, manipulation and rest (`V/Schema/Inputs.cs:259-280`; `V/Rules/DriverRule.cs:16-29`). No skill or trait is read for driving or flying anywhere, and nothing is saved per colonist | Grow a pilot or a driver | Nothing: anyone may still drive | Who you send matters; losing the one seasoned pilot costs | None (lite tier) | Small to medium. **Take with EM23**, whose log supplies the hours |
| EM12 | **"Their" vehicle.** A regular driver's memory thoughts when it is lost, taken or brought home | Two thoughts exist, both situational | — | — | A loss is felt by someone | None | Small, Defs. **Take with EM13.** Text held for review |
| EM13 | **Records and tales.** Distance driven, vehicles repaired and casualties flown in a colonist's records; "towed home" and "walked away from a crash landing" as tales, so art and life stories carry them | None of ours exist. One game record is used once (`V/Service/FieldResearch.cs:77`) | Read who did what, in the game's own places | — | Nothing in what the player does: this is Sylvester's hair complexity, cheap, and it says so | None | Small. **Take** |
| EM14 | **Rituals** (Ideology): naming a new vehicle, a farewell for a hull. Mood only | Not read | — | — | — | — | **Idea only**, lowest |

### D. Scarcity across distance

| # | Idea | What the reads found | Lets | Stops | Changes | Other factions | Size and verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EM15 | **Standing supply runs** by real vehicles on a set route | Recorded for remote farms (RF6). Standing crews were set aside | Run a network | Free transport | Routes, fuel and breakdowns shape the colony's reach | — | Large. **Hold until remote farms or the garage see play** |
| EM16 | **Roads that close, told in advance.** The route planner shows the winter cost at the date of arrival | The world ground already reads the game's winter offset (`src/PhobosFramework/WorldMap/BiomeGround.cs:44-52`; `V/Rules/WorldRules.cs:51-62`). No mud or thaw; weather grip is map-only (`V/Systems/Powertrain.cs:677-684`) | Plan around winter | — | The season enters route choice | — | Small, presentation. **Take when a route planner is next touched** |
| EM17 | **Fuel prices that move with the world** | A bulk order's price is the game's own (`V/Tanks/BulkOrders.cs:78-80`). RimMarket already has a price model: a thing's price falls as it is sold, to half, and recovers (its `MarketDemand.cs:10-50`) | — | — | — | — | **Argued against** a model of our own: one exists |
| EM18 | **The gravship as an expedition ship** | **Smaller than first thought: it already works.** The game carries any thing whose centre cell is on the ship's floor, pawns included, and puts it down at the same place on landing (G:771692-771775, 706858-706868). Vehicle Framework and Vanilla Gravship Expanded add nothing to it. Our pipes survive the flight (`Comp:651-652`). **Not read:** a landing that turns a vehicle of several cells | Launch knowing what stays behind | — | — | — | Small: a line before launch naming vehicles whose centre is off the floor. **Take after the turning read** |
| EM19 | **Finish the camp rows** (Y1 to Y23): the fridge that flattens the battery by morning | Designed; only the power budget is built | — | — | — | — | **Already recorded.** The agent's view: finishing them is an investment in emergence |

### E. Cascades at home

| # | Idea | What the reads found | Lets | Stops | Changes | Other factions | Size and verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EM20 | **Exhaust in closed rooms.** The stage that warms the room also adds carbon monoxide, in proportion to fuel burnt [A; the figures need a source first] | **Smaller than first thought.** Our `PhobosGas_CarbonMonoxide` exists (`Gases.xml:7-19`); fires emit it; D'thorga Medical turns breathing it into a condition (`src/PhobosMedical/BurnsAndSmoke/Services/SmokeExposureService.cs:66-70`). Engine heat goes into the room at 12 per second times load (`V/Schema/Effects.cs:179-193`; `Systems_Cooling.xml:212-219`). "Running" means fuelled and drafted, moving in a convoy or flying (`Comp:973`). No vehicle code calls the gas service | — | Free heat or power from an engine in a closed room | Garages get doors and vents; the generator set stands outside or takes its heat exchanger; a cold snap forces a choice | None unless it reads Vehicle Framework's engine state: the lite tier runs no stages | Small. **Take, on the owner's word** (question 4): a new hazard, on an existing gas |
| EM21 | **Each verb acts on anything suitable.** (a) The winch pulls by pull needed: chunks, hulls, a carcass. (b) The sling lifts by weight: a net of items, a packed building, a hull. (c) Tanks fill from a river; the water drop fills from a tank. (d) The litter carries a downed enemy as a prisoner | Audit 1 | Use a tool on whatever it could physically handle | — | Fewer special cases; more improvised uses | A hull or chunk has no owner; (d) is about them | Each small to medium. **Take (a) and (c) first.** Each widening is new ground to test |
| EM22 | **Shelter that costs something.** An open vehicle passes to its crew what acts on people outdoors; a closed cab still shelters | Audit 2's core fact. Every seat on our own vehicles is enclosed, so this row acts on other authors' open vehicles. Vehicles do not seal a wall: they are passable at a cost of 250 (VFxml `VehiclePawnBase.xml:12-13`) | Shelter a crew on purpose | Riding an open bike through toxic fallout unharmed | The kind of cab matters when the air turns bad | Works for every faction: it reads the seat, not our stages | Small to medium; needs a hook on the game's conditions. **Wait for the cab-air model** (owner, 20 Nov 2026): open vehicles and a time limit on closed cabs are then done together, with hotboxing's cab air |

### F. Memory, and reading the story

| # | Idea | What the reads found | Lets | Stops | Changes | Other factions | Size and verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EM23 | **The logbook.** A lasting list of milestones beside the 15-day log: trips, crews, tows, battles, captures, who repaired what [A cap per vehicle] | The log keeps 64 events for 15 days, of four kinds, for the player's vehicles only (`V/Systems/VehicleEventLog.cs:54-56`; `Comp:211`). A part replacement records nothing (only a service does, `Comp:2830`). Nothing records the driver, the repairer, distance or trips | Read a vehicle's life | — | Little by itself; it feeds EM11, EM13 and EM25 | A captured vehicle's log starts at its capture | Small; grows a save a little. **Take with EM24** |
| EM24 | **Say why.** A fault keeps its cause and the place, and the diagnosis report and the stranded alert say it in the game world's words | The cause is known and dropped twice: a hit has its damage kind and does not pass it on (`Comp:1849`, `:1868`); a stage passes only its system's name (`V/Schema/Effects.cs:352-354`) | Learn from a failure | — | Habits change (ford less, service sooner) because the chain can be seen | A fault from before a capture reads "cause unknown" | Small: one saved field. **Take first** |
| EM25 | **The trip report** at a journey's end: distance, fuel, faults, who drove. Behind a setting | Needs EM23 | Read the journey as one story | — | — | — | Small. **Take with EM23** |
| EM26 | **A measure for us**: outcomes that three or more systems fed, read from the log and the seen-in-play notes | — | — | — | — | — | **Idea only**, for phobos-modkit, after EM24 |

### G. Pressure

| # | Idea | What the reads found | Lets | Stops | Changes | Other factions | Size and verdict |
| --- | --- | --- | --- | --- | --- | --- | --- |
| EM27 | **The game's incidents meet the vehicles**: the small links in audit 2's last column | Audit 2 | — | — | An incident the player already knows gets a vehicle side | The flare and the shelter links work on any vehicle | Each small. **Take:** the cold snap alert, the short on a hooked-up vehicle, automatic collection of a stranger. **The owner's choice:** the solar flare |
| EM28 | **Raids that know the motor pool**: raiders who burn empty vehicles and the fuel store | The storyteller already scales with the fleet's price (audit 2, wealth). Nothing targets an empty vehicle | — | — | — | The whole row | Large: a raid behaviour of our own. **Hold.** The wealth fact is worth the owner knowing now |
| EM29 | **Unwise orders are allowed, with a warning.** (a) Lift the four whose consequence is built: engines cut in the air, an overloaded or storm lift, a worn-through clutch, an over-ratio tow. (b) Build a consequence before lifting: brakes, flight controls, blind flight | Audit 3. `showOnly` is the blunt version that exists | Take a risk knowingly | Nothing: that is the point | The player, not the game, decides how much risk; a harsher mode is a setting, off by default [A] | None: refusals are the player's only | (a) small. **(a) taken as a direction: warn, then allow** (owner, 20 Nov 2026); not built. (b) each needs a design |

## Other factions' vehicles

| | What these rows do |
| --- | --- |
| **In a raid on us** | EM5 sets a raider vehicle's fuel once from the journey. EM22 exposes raiders on open vehicles as it does ours. EM6 and EM28 are raid behaviours, both held. Everything built on our stages (EM20, EM29) does not act on them: the lite tier runs none |
| **Parked at a base we attack** | EM2 and EM3: a hull left there is salvage after a claim. EM21(a) and (b) would let a winch or sling move it once claimed |
| **Captured** | EM23 and EM24 start at the capture: its log is empty and older faults read "cause unknown". EM10 would need a saved former owner |

**Cost with several on a map:** EM5 is one loop over a faction's settlements when a vehicle appears. EM22 adds vehicles' crews to a
walk the game already makes at long intervals. Nothing here adds a per-tick scan.

## Saves

New saved state would be: a cause on a condition (EM24); a lasting log per vehicle, capped (EM23); hours per colonist per vehicle
kind (EM11); a site holding a lost convoy (EM2), which is lost if the mod is removed, so its README must say so. Records, tales
and thoughts (EM12, EM13) are the game's own. Every key would carry the `Phobos` prefix and stay stable.

## Found on the way (listed, not fixed)

1. **Anyone aboard any vehicle is immune to fallout, pollution build-up, unnatural darkness and bites.** Vehicle Framework's
   behaviour, not a fault of ours. Row EM22.
2. **A convoy whose last colonist dies is deleted with its vehicles and cargo** (VF:93569-93592). Vehicle Framework's. Row EM2.
3. **A repair at a halt counts supplies in that vehicle's own cargo only** (`Comp:2737-2740`), though a ground convoy refuels from
   every vehicle's cargo (`V/Service/CargoRefuel.cs:27-33`). Whether that is meant is the owner's to say. Row EM1. **Fixed in
   Framework 0.181.0** with batch 1: supplies are counted across the whole convoy.
4. **Three refusals have no consequence behind them**: brakes, flight controls, blind flight. Row EM29(b).
5. **`Effect_ForceUndraft` has no user** in any Def.
6. **The patched-Defs file is out of date** (8 Nov 2026, 335 mods; 364 active). The owner's step: Framework's dev action "write
   patched Defs at the next start", then a restart, when convenient.
7. **Automatic litter collection and the manual order differ** on a downed stranger (audit 1). Row EM27.

## The short list

In the order the agent would take them **[A]**, redrawn after the owner's answers of 20 Nov 2026 (interest in groups F, A, and E
with G). **Nothing is built until the owner says so.**

| Order | Rows | Why |
| --- | --- | --- |
| 1 | **EM24, EM23, EM13, EM25** | Say why, the logbook, records and tales, the trip report. Small, and they make every other system's stories readable (Sylvester's principle) |
| 2 | **EM1** | Stranded becomes a state: an alert, and supplies counted across the convoy |
| 3 | **EM29(a)** | Four unwise orders whose consequence is already built: warn, then allow (owner) |
| 4 | **EM20, EM27's small links, EM21(a) and (c)** | Exhaust indoors on a gas that exists; the game's incidents get a vehicle side; the winch and the tanks work on more |
| 5 | **EM2, then EM3** | What is lost stays; salvage |

**Not picked for now:** the groups "everyone drives" and "people in the machine" (EM5, EM11, EM12). **Waiting:** EM22, for the cab-air
model (owner). **Later:** EM4, EM9, EM16, EM18's warning, the rest of EM21. **Held:** EM6, EM7, EM15, EM28. **A direction:** EM8.
**Ideas only:** EM10, EM14, EM26. **Argued against:** a price model of our own (EM17). **Already recorded:** EM19.

## Questions for the owner

1. **Random rolls.** The agent's default: state decides what happens, the game's storyteller decides when pressure arrives, and we
   add no rolls of our own. Confirm it, or allow a roll where a real cause cannot be modelled?
2. **Shelter (EM22).** Leave vehicles as total shelters; or have open vehicles pass fallout and the like to their crews while
   closed cabs shelter (recommended); or wait for the cab-air model and do both together?
3. **Unwise orders (EM29).** Lift the four whose consequence is built (recommended), asked each time with a warning, or behind one
   setting? And should **engines cut in the air** fall, where today they are restarted and the aircraft landed?
4. **Exhaust indoors (EM20).** Wanted? On or off by default?
5. **What is lost (EM2).** A lost convoy becomes a site, a destroyed vehicle leaves a hull, both, or neither?
6. **Raiders' fuel by distance (EM5).** Wanted, once its read is done? **The read is done** ([Read: EM5](#read-em5-raiders-fuel-20-nov-2026)):
   raiders blow up their own vehicle when it cannot move, so the question is now which of three shapes.
7. **What to take first**, if anything: the short list in order, or another pick?

**Answered by the owner, 20 Nov 2026 (cards):**

- **Question 1: rolls only as a last resort.** State decides; a roll is allowed only where a real cause exists and cannot
  reasonably be modelled, and each is named in its design record.
- **Question 2: wait for the cab-air model.** Nothing changes for shelter now; open vehicles and a time limit on closed cabs come
  together with hotboxing's cab air.
- **Question 3: warn, then allow**, for the four unwise orders whose consequence is built. The button stays live and its text
  says plainly what will happen. Engines cut in the air then fall, where today they are restarted and the aircraft landed.
- **Question 7: most interest** in reading the story (group F), a failure opening play (group A), and home and incidents (groups
  E and G). Not picked: everyone drives (B) and people in the machine (C). **This sets what is detailed first; it is not a word
  to build.**

**Still open:** 4 (exhaust indoors: on or off by default), 5 (a lost convoy as a site, a hull on the map, or both) and 6 (raiders'
fuel by distance, in a group not picked for now).

## As built: life on the road in the logbook and the Fleet board (22 Nov 2026, Framework 0.230.0)

The owner, 22 Nov 2026: "a lot of possibility for integrating much of the work we've done already and about to into our Vehicle Log and
perhaps the Fleet window as well". Rows 351 and 352 of [the in-game test pass](in-game-test-pass.md). Not yet tested in game.

| Question | Answer |
| --- | --- |
| What it lets the player do | Read a trip's camps, night drives, meals and troubles afterwards; see from the Fleet board which convoys are camped, halted for a meal, driving on, or short of food |
| What it stops them doing | Nothing |
| How it changes play | A convoy's life between towns becomes something to read back, and a hungry convoy shows before its people starve |
| What outside the vehicle notices | The logbook (EM23) and the Fleet board; nothing else new |

**Risk: none** (agent's grade): it only writes and shows; no setting.

- **The logbook's new kinds of entry** (`Stories.xml`, agent-authored words held for the owner's review): `PhobosLog_Camped` (the camp
  order, set up from the cargo), `PhobosLog_NightDrive` (once a night at most, with everyone at the wheel), `PhobosLog_HandOver`,
  `PhobosLog_RoadMeals` (the meals and how they were cooked), `PhobosLog_WaterDry`, `PhobosLog_HeaterDry` and `PhobosLog_TentWoken`. A
  convoy's entries go in its first vehicle of the player's that keeps a logbook (`VehicleStory.Convoy`); one vehicle's in its own
  (`VehicleStory.Of`).
- **The Fleet board's lines** (`FleetService`): a convoy "camped for the night" (resting on its way), "halted for a meal", "driving
  through the night"; a vehicle on a map "camped" while its camp is pitched; and a convoy's need "food: less than a day left", from the
  caravan's own days of food, which counts towed food since Framework 0.228.0. **Agent's choice:** a day, where the game's own figure turns
  to its warning colour. A water line was left out: the board cannot tell a convoy with empty tanks from one that never carried any.
- **A key for the board** (`PhobosMainTab_Fleet`, in the game's main tab keys): the game makes such a key only for a tab with a default
  key, so the board had none. **Agent's choice:** no default key, so it never takes one another mod uses; the owner picks one.
- **The fleet in D'thorga Companion's atlas** is a separate piece for later (the owner's choice, 22 Nov 2026): recorded in the
  Companion's `docs/later.md`, "Atlas: the fleet layer", to build when the Companion is next worked on.

## As built: batch 1, reading the story (20 Nov 2026, Framework 0.181.0)

The owner's word, 20 Nov 2026: "Proceed with Batches #1 through #4, step-wise if necessary", and "reuse/genericize code in the
Framework where it would be appropriate", so other mods can use it. Not yet tested in game; rows 256 to 258 of the test pass.

| Row | What was built | Where |
| --- | --- | --- |
| EM24 | A condition keeps its cause: the kind of damage of the hit that gave it, or the system stage that strained it, with when and where. The Health tab and the diagnosis report say it under the fault | Core: `Persistence/CauseNote.cs`. Vehicles: `VehicleConditions`, `Notify_PartHit`, `Effect_AddCondition`, `VehicleStory.CauseLine` |
| EM23 | A logbook any thing can keep, with its own tab; entry kinds are Defs, so other mods add their own in XML. Vehicles log journeys, faults with causes, repairs and services with the mechanic, tows, winch recoveries, coming under fire (once a day of fighting), being taken. 200 entries, oldest dropped first [A] | Core: `Persistence/Logbook.cs`, `UI/ITab_PhobosLogbook.cs`, `Rules/StoryRules.cs` (`LogbookRule`). Vehicles: `Service/VehicleStory.cs`, `Stories.xml` |
| EM13 | Three colonists' records (kilometres driven, vehicle repairs, vehicles recovered) and three tales for art (a winch recovery, a roadside repair, a long journey of 10 tiles or more [A]) | `Stories.xml` |
| EM25 | A journey opens when a vehicle is found in a convoy or in flight, follows it at the rare look it already gets, and closes on a map: distance (30 km a tile, the owner's figure, now `WorldScaleRule`), fuel burnt, new faults, drivers. A letter only when something went wrong, behind the new setting **Letters for troubled journeys** (on) | Vehicles: `Service/VehicleTrips.cs`; core: `TripRule` |
| EM1 | The **Convoy stranded** alert: the vehicle, why, what the convoy could fix it with, how far home is. Repairs and services at a halt now count supplies across the whole convoy's cargo (the record's "Found on the way", 3) | Vehicles: `Service/Stranding.cs`; core: `WorldMap/WorldPlaces.cs` |

**Generic, for other mods:** `CauseNote`, `Logbook` with `ILogbookHolder` and `PhobosLogEntryDef`, `ITab_PhobosLogbook`,
`WorldPlaces` (a place in words, the nearest home, distances in km) and `WorldScaleRule` live in the core and name no vehicle.

**Other factions:** causes are kept on every faction's vehicle (hits are counted on the lite tier), so a captured vehicle's faults
say why; logbooks, journeys and the alert are the player's only, and a captured vehicle's logbook begins at its capture.

**Saves:** new saved fields, all on the vehicle's systems comp: the logbook (`PhobosVehicleSystems_Logbook`), the journey under way
(`PhobosVehicleSystems_Trip`), the last hit's tick, and a cause on each condition. Older saves load with none of them. Removing the
mod drops them with the rest of the comp.

## As built: batch 2, unwise orders (20 Nov 2026, Framework 0.185.0)

The owner's answer to question 3: "warn, then allow", and "build all four now" (not after the Osprey's first flight, as the agent
suggested). Not yet tested in game; row 259.

| Order | Before | Now | The consequence, from what was already built |
| --- | --- | --- | --- |
| Cut the engines in the air | Switching off in the air was refused, the engines restarted and the aircraft landed on the nearest clear spot | A new button, **Cut the engines**, whose text says what follows. The ordinary switch-off still lands first (the owner's decision of Framework 0.174.0, kept) | The fall that existed for engines that cannot hold it up, judged as one with no power at all: the harshest landing and harm aboard, a slung load dropped, a wreck over bad ground |
| Lift over the hook's rating | Refused | Warned when chosen; lifted | New: the hook's mount loses the overload's share of its health at lift-off (`SlingRule.HookStrain`, an authored model); a mount at nothing gives way and the load drops back 3 m [A], through the existing fall |
| Lift in a storm or high wind | Refused | Warned on the button and when chosen; lifted | The same strain, plus a quarter of the mount's health [A] |
| Tow over 1.5 times the tractor | Refused | Warned when chosen and hitched | Already built: towed weight wears brakes and clutch |
| Drive on a worn-through clutch | Refused | A message, once | Already built: the clutch takes up a quarter of the drive (only with the full drivetrain model) |

**Kept refused, and why** (agent's reading of the audit): driving on worn-through brakes, flying with wrecked controls and blind
take-offs beyond the existing warn-only setting have no consequence modelled yet (EM29(b)); lifting a load the helicopter could not
hover with is impossible, not unwise. **Logbook and tale:** a hard landing the aircraft survives goes in its logbook, and everyone aboard
who walks away from it may turn up in art (`PhobosTale_CrashSurvived`).

## As built: batch 3, home and incidents (20 Nov 2026, Framework 0.187.0, Vehicles 0.90.0)

Not yet tested in game; row 260.

| Row | What was built | The figures, and where they come from |
| --- | --- | --- |
| EM20 | **Exhaust in closed rooms**, behind a new setting, off by default (owner, 20 Nov 2026). A running engine puts carbon monoxide into the closed room it stands in, by weight: grams an hour, scaled by the engine's load, turned into the framework's room gas through the gas's molar mass and what its full concentration stands for (`GasMassRule`; core `Effect_PushGas`, generic for any gas a source knows the grams of). D'thorga Medical turns breathing it into poisoning | 140 g an hour at full load, 70 at idle [A], from a petrol car with no working catalytic converter: the US EPA's fleet idle figure of 71 g/h (EPA420-F-08-025, 2008; **unverified**, a search summary only) and Singer and colleagues' 223 g a gallon of warm exhaust ([LBNL, J. Air and Waste Management Association 49(2), 1999](https://eta-publications.lbl.gov/publications/fuel-based-approach-estimating-motor)). Diesels idle far lower (25 to 37 g/h for buses: [US EPA EPA420-F-08-026](https://nepis.epa.gov/Exe/ZyPDF.cgi?Dockey=P100EVY1.PDF)) and a small generator far higher (about 500 g/h and up: Emmerich, Persily and Wang, [NIST TN 1781, 2013](https://nvlpubs.nist.gov/nistpubs/TechnicalNotes/NIST.TN.1781.pdf)); one figure for every engine is a simplification. A cell holds 3 m3 of air [A]; a full room stands for 1,600 ppm [A], matched to Medical's poisoning and the commonly cited table (an OSHA-hosted training handout, 2017: death within 2 hours at 1,600 ppm). The NIOSH limits for reference: 35 ppm over a shift, 1,200 ppm immediately dangerous ([NIOSH Pocket Guide](https://www.cdc.gov/niosh/npg/npgd0105.html)). In a closed single garage of 18 cells an idling truck adds about 1,100 ppm an hour |
| EM27 | **Vehicles won't start**: an alert while a cold snap holds a map, or it is colder than -15 C [A], naming parked vehicles that would not start and why (their own systems' answer) | — |
| EM27 | **A short circuit reaches a plugged-in vehicle**: a prefix on the game's `ShortCircuitUtility.DoShortCircuit` gives every player vehicle whose hook-up lead joins that power net shorted wiring at 0.4 severity [A], with the cause "a surge from the grid" (core `CauseNote.Words`, a cause told in its own words) | — |
| EM27 | **The air ambulance collects strangers**: a downed, non-hostile stranger is a casualty for automatic collection, as the manual order already allowed. Set down, a colonist must still be told to rescue them, as the game asks | — |
| EM21(a) | **The winch takes another faction's vehicle** where the claim order could: nobody aboard, nobody of theirs armed nearby. It stays theirs | — |
| EM21(c) | **A tanker fills from a river, lake or sea** beside it (Dubs Bad Hygiene's tie-in: `Port_OpenWater`, untreated water), after plumbing. **The water bucket fills from a parked tank of water** (`putsOutFires` on a fluid; the water leaves the tank when the run is ordered, a simplification) | — |

**Since built (Framework 0.197.0, row 286; the owner, 20 Nov 2026):** the sling lifts a destroyed vehicle's hull
(`vehicle-sling-design.md`, "Hulls on the hook"). The agent had called it small; the sling was written for vehicles throughout, so it
became a rework that lets the sling carry either.

**Kept out:** the solar flare (the owner's choice, not taken), and the rest of EM21: the sling lifting items, a hull or a packed building
(EM21(b)), and the litter carrying a downed enemy as a prisoner (EM21(d)).

## As built: batch 4, what is lost stays (20 Nov 2026, Framework 0.187.0)

The owner's answer to question 5: "both" (a lost convoy as a site, and a hull on the map). Not yet tested in game; row 261.

| Row | What was built | Where |
| --- | --- | --- |
| EM2 | **A destroyed vehicle leaves its hull** (setting, on): a prefix on Vehicle Framework's `VehiclePawn.Kill` asks for the hull its own code already makes (`spawnWreckage`), for any faction's vehicle; a raiders' aircraft brought down is still never a prize (the owner's choice, kept by a suppress scope round that one call) | `Service/VehicleHulls.cs` |
| EM3 | **Taking a hull apart gives salvaged parts**, not materials: a quarter of the vehicle's worth (`SalvageRule.HullShare` [A]) beside Vehicle Framework's own quarter of materials at destruction, so a wreck is never worth more than half of what it cost. Every vehicle frame gets the hull comp at start-up and gives no materials when deconstructed (a frame exists only as a hull once a vehicle is built). **Salvaged parts pay for a part at the salvaged grade** (`paidWithSalvage` on the grade), at the worth of the new materials | Core `Rules/SalvageRule.cs`; `Salvage.xml`; `VehicleRepairs.ReplacementOptions` |
| EM2 | **A lost convoy stays** (setting, on): a prefix on Vehicle Framework's `VehicleCaravan.Notify_MemberDied` takes the vehicles out, with their cargo and anyone aboard, just before Vehicle Framework would clear the caravan, into the place a dropped sling load becomes, generalised to hold several vehicles (`PhobosLostConvoy`); older single-vehicle wrecks load as before | `Service/LostConvoys.cs`; `Sling/SlingWreck.cs` |

**Other factions:** hulls are left by every faction's vehicles; a lost convoy is the player's only.

## As built: the small round (20 Nov 2026, Framework 0.188.0)

The owner's pick of 20 Nov 2026, "stage the next small round", with "it overruns and hits things" for brakes. Not yet tested in game;
rows 269 and 270.

| Row | What was built | Lets | Stops | Changes | What outside the vehicle notices |
| --- | --- | --- | --- | --- | --- |
| EM29(b) brakes | Worn-through or wrecked brakes no longer refuse to move (`Effect_NoBrakes` in place of the refusal). On Vehicle Framework's arrival (a prefix and postfix on `VehiclePathFollower.PatherArrived`, neither skipping it) the vehicle rolls on straight ahead, a cell at a time: `OverrunRule` gives 0.6 s of travel for a 5-tonne vehicle, more by the square root of its weight, 8 cells at most [A]. People in the way are struck and it goes on; a wall, cover of 0.4 fill or more, or another vehicle stops it, each taking 4 damage a tonne a cell-a-second (5 to 400), the vehicle half of that on its leading end [A] | Drive a vehicle whose brakes are gone, knowingly | Stopping where it is told | Where it is driven, and who stands near | Whoever and whatever is in its way; its logbook; colonists working by themselves, who leave it alone |
| EM16 | The season ahead at a tile: the game's own winter figure for a future date, crossing 1.0 of 2 as "snow on the ground", looked for 30 days ahead [A] (`WinterForecastRule`, `WorldSeasons`, both core). A convoy's inspect pane says it for where it is and where it is going | Leave before the snow, or wait it out | — | The season enters route choice | The world's seasons reach the convoy before it is slowed |
| EM18 | A postfix on the game's `GravshipUtility.PreLaunchConfirmation` adds a paragraph to its window naming vehicles whose centre is on the floor with a cell off it. **Read on the way:** the game already lists vehicles whose centre is off the floor, among "animals" that will be left behind | Launch knowing | — | — | The gravship's launch |

**Not built of EM18:** what a turned landing does to a vehicle of several cells: **read on 20 Nov 2026**
([Read: EM18](#read-em18-a-turned-gravship-landing-20-nov-2026)); nothing built.

**With it, the fleet board was redrawn** (the owner's request in the same answer; `vehicle-presentation-design.md`, "P2, refreshed"): it
is where the batches' stories are read in one place. Each row says what the vehicle is doing and needs; lost vehicles (EM2), hulls (EM3)
and vehicles that could be claimed are listed; and the pane shows a fault's cause (EM24) and the newest logbook lines (EM23). Row 271.

## As built: people in the machine (20 Nov 2026, Framework 0.190.0)

The agent's pick, built under the owner's overnight go-ahead of 20 Nov 2026 ("proceed ... where risk remains low-to-medium ... and
at the very minimum, has been researched and recorded"): EM11 with EM12, the one group of this record left untouched, and the first of
the agent's three next picks. Not yet tested in game; row 275. Every figure is [A].

| Row | What was built | Lets | Stops | Changes | What outside the vehicle notices |
| --- | --- | --- | --- | --- | --- |
| EM11 | While a player's vehicle moves, everyone of theirs in a driving seat gains practice at that kind of vehicle (by its defName), counted on the rare look it already gets, on a map or away. Practice gives a share of a gain: half at 40 hours, three quarters at 80 (`PracticeRule`, core). The driver system's `driverExperience` quantity eases brake and tyre wear to x0.85 at full share; a landing's wear (`WearLanding`: the rolling landing and the forced landing) to x0.6. Colonists' records gain **hours at the wheel** | Grow a driver or a pilot | Nothing: anyone may still drive | Who is sent with which vehicle; losing the one seasoned pilot costs | The colonist's records; nothing else |
| EM12 | The colonist with the most hours at one vehicle, at least 12, is its **regular driver** (hours saved on the vehicle by load ID). Memories naming the vehicle: destroyed -6 for 10 days (a prefix on Vehicle Framework's `VehiclePawn.Kill` that never skips it), lost -4 for 8 days (where `PhobosSlingWreck.Make` takes it: a dropped sling load, a lost convoy), brought home +5 for 6 days (when a lost vehicle stands on a home map again, with a logbook line). The fleet board's pane names the regular driver | Read who a vehicle belongs to, in the game's own moods | — | A loss is felt by a person, not only the ledger | Its regular driver's mood |

**Not built of EM12:** "taken": nothing in the game or Vehicle Framework takes a player's vehicle today (EM6). A mechanic's pride in a
vehicle kept well was in the first sketch and was left out: it needs a measure of "kept well" first.

**Generic in the core:** `Practice` and `GameComponent_PhobosPractice` keep any person's hours at any kind of thing, keyed by load ID, for
any mod: hours at a surgery table or a combine's controls would use the same record. `PracticeRule` holds the curve and the regular's
threshold, with tests.

**Other factions' vehicles:** their drivers gain nothing and their vehicles have no regular driver; the light tier keeps no history, by
the owner's decision of 11 Nov 2026. A captured vehicle starts with no hours.

**Saves:** two new saved entries on each vehicle (its hours by person, when it was lost) and one game component; safe to add mid-game.

## Read: EM5, raiders' fuel (20 Nov 2026)

The read EM5 waited on (EP5), done overnight under the same go-ahead. **Not built: what it found turns the row into the owner's
decision.** Vehicle Framework 1.7 as installed, decompiled 18 Nov 2026 (`.local/decompiled/VehicleFramework-1.7-2026-11-18`).

- **Starting fuel:** every vehicle not the player's is given a random 45% to 85% of its tank when it is made
  (`CompFueledTravel.PostGeneration`, decompile line 22385-22393). Raids make their vehicles through that path (the pawn-group
  postfix that puts raiders into vehicles, line 51333). A vehicle spawned by the debug and quest helper `SpawnVehicleRandomized` is
  topped up to full instead (line 36207).
- **The sabotage order:** a raider who is a tool user, able to do violence and construction, picks a vehicle of its own raid within
  10 cells that **cannot move**, has no charge on it yet and has over 35% of its health, and straps a timed charge to it: 100 bomb
  damage, armour penetration 2, a blast as wide as the vehicle's narrower side, after 1 second per cell of its footprint (10 seconds
  at most) (`JobGiver_SabotageVehicle`, line 4032-4089; `JobDriver_SabotageVehicle`, line 2889-2915; the raider think tree,
  Vehicle Framework's `1.7/Defs/ThinkTree/ThinkTree_Raider.xml`, line 57). Raiders deny the player a stranded vehicle; they do not
  leave it as a prize.
- **Whether an empty tank stops a raid vehicle at all is not settled.** "Cannot move" reads the vehicle's speed stat and its movement
  status (line 16256-16266); no stat part of Vehicle Framework's reads fuel (line 10171-10215), and an empty tank refuses drafting
  (line 21783-21800). Whether the raid's own orders still drive a dry vehicle needs a look in game, or a longer read of its AI.

**What follows (the agent's reading):** EM5 as written ("a far faction's trucks arrive nearly dry and may be left behind, a prize with a
cost") would most likely give raid trucks that run dry near the colony and are **blown up by their own side**, if a dry truck stops at
all. Three shapes for the owner: (a) keep EM5's fuel by distance and accept the charges (a raid that came far ends with its trucks
burning: dramatic, and costly to anyone near); (b) fuel by distance with a floor that keeps them moving (about a fifth of a tank [A]),
so distance shows only in how long they can press an attack before leaving; (c) set it aside. The agent leans to (b). Question 6 below
is still the owner's.

**The owner's answer (20 Nov 2026): the fuel floor (b).** Worked through with real figures, a raid burns only a few units on the map
(the Bang Bus about 0.6 an hour, the M113 0.8, the MaxxPro 1.4), and a trip of 3 to 30 tiles leaves 70% to 97% of a tank, so the agent
then advised setting it aside. The owner kept it, for the leak: "especially if the chemfuel tank gets a puncture and starts leaking".
Read on: Vehicle Framework drains a tank whose fuel-tank part is under 80% health at 1 unit a second, rising to 10 as the part fails,
with a puddle of the fuel's filth each unit (`Reactor_FuelLeak`, its decompile line 9981; `CompFueledTravel.TicksPerLeak`, line
22209). A holed 80-unit tank is dry in 8 to 80 seconds, so arrival fuel decides how long a holed raid truck moves, how much fuel it
spills, and how soon its crew blows it up. The owner was right; the agent's advice had left the leak out.

**As built (Framework 0.192.0, row 278):** on another faction's ground vehicle's first spawn on a map not its faction's settlement, its
tank is set once to a full tank less the trip from that faction's nearest settlement (Vehicle Framework's own ticks per tile and fuel
rate, `ArrivalFuelRule`), never below a fifth [A], in place of the random 45-85%. Aircraft, vehicles at their own settlements, the
player's and figures-only are left alone; a saved flag keeps it to once. Lets: hole a far raid's tank and watch it run dry. Stops:
nothing. Changes: where an enemy comes from shows in a firefight. What outside notices: the raid's crew (the sabotage order), and the
ground, which takes the spilt fuel.

## Read: EM18, a turned gravship landing (20 Nov 2026)

The second read EP5 listed, done the same night. **Nothing built.** The game's full decompile (`.local/decompiled/full/all.cs`) and
Vehicle Framework's of 18 Nov 2026.

- **How the game turns what it carries:** on landing, every pawn the ship carries (a vehicle is one) is spawned at its launch offset
  turned with the ship (`PrefabUtility.GetAdjustedLocalPosition`), facing its old facing turned the same way
  (`Gravship.GetPlacementValues`, line 771928; `GravshipPlacementUtility.SpawnPawns`, line 706858). A building lands on the same deck
  cells because the game's footprint rule shifts an even-sized footprint by one cell when it faces east, south or west
  (`GenAdj.AdjustForRotation`, line 14658), and the turn is made to match that rule.
- **How Vehicle Framework draws a vehicle's footprint:** with the game's rule, but facing south counted as north and west as east
  (`AdjustForVehicleOccupiedRect`, its decompile line 71680, called by `VehicleRect`), so a vehicle's footprint never moves when
  it turns about.
- **What follows (the agent's reading, not seen in game):** a vehicle whose length or width is an **even** number of cells can cover a
  footprint one cell along from the deck cells it stood on at launch: one end one cell further out, the other one cell short. It
  happens when the ship's turn carries the vehicle's footprint, as Vehicle Framework draws it (facing north or east), to face south or
  west: **always on a half turn**, and on a quarter turn for some facings. A vehicle with odd sides always lands exactly, and so does
  every vehicle when the ship lands unturned. Parked near the deck's edge, its end can hang a cell over the edge or overlap a wall;
  GenSpawn does not check.
- **Whose it is:** neither author's code is wrong alone; the two footprint rules disagree. Not extreme: a vehicle a cell out of place,
  no lost data. Under the owner's rule (another author's fault is theirs to fix unless it is extreme), **left to Vehicle Framework's
  author**; a bug report could be drafted for the owner if they want one.
- **The owner's answer (20 Nov 2026): fix it.** Built as `GravshipLandingAlignment` (Framework, Unreleased; row 284; temporary
  patch row 21): a postfix after the game places a landed ship's contents moves each carried vehicle back by the difference between
  where its launch footprint lands and where Vehicle Framework now draws it, so nothing moves after an unturned landing or once the two
  rules agree. Checked by a simulation of both rules over sizes 1x1 to 4x6, every facing, turn and position: the target is always the
  turned launch footprint, and without the fix 2,352 of those cases were off.
- **Cheap ways round it, for the owner:** (a) the launch warning (EM18, built) could also name a vehicle with an even side parked within a
  cell of the edge; (b) park such vehicles facing north or east, or away from walls, which a guide line could say. A fix of our own (a
  postfix after the ship lands that moves each such vehicle one cell back) is possible but would be a patch on another author's fault,
  with a row in the temporary-patches list.

## Who chose what

- **Owner (20 Nov 2026):** the round, its focus on the vehicle mods, and record only; the plan for this record, with its three
  audits; the four answers above.
- **Agent choices, for the owner to revise:** every row, verdict, size and figure marked [A]; the default on random rolls; the six
  conditions and the chemist's reading of Nintendo's rules; the short list; the readings of other authors' code.

## Sources

- **Our code**, read 20 Nov 2026. Re-read by the writing agent: `Comp:211`, `:973-975`, `:1849`, `:1868`, `:2471-2474`,
  `:2737-2740`; `V/Schema/Effects.cs:25`, `:167-193`, `:352-354`; `V/Hitch/Recovery.cs:137`, `:314`;
  `V/Hitch/CompPhobosHitch.cs:98`; `V/Sling/CompPhobosSling.cs:38`, `:158`, `:833`; `V/Sling/WaterDrop.cs:118`;
  `V/Litter/CompPhobosLitter.cs:72`, `:338`; `V/Litter/LitterService.cs:92`; `V/Systems/VehicleConditions.cs:98`;
  `V/Systems/VehicleEventLog.cs`; `V/Service/MoveRefusals.cs`. Everything else as the survey agents reported it.
- **Vehicle Framework 1.7** by SmashPhil: our decompile of the installed build, kept in `.local/` (VF:18182, 51161 and
  93569-93592 re-read), and its public source at commit 026420b5. **RimWorld 1.7** by Ludeon Studios: our decompile, kept in
  `.local/`. **Hospitality**, **RimMarket**, **RimPacts**, **Rails and Roads of the Rim**, **Combat Extended**: their public source
  or our decompiles, as cited. All summarised in our own words; no author endorses this record.
- Harvey Smith, [The Future of Game Design: Moving Beyond Deus Ex and Other Dated Paradigms](https://www.witchboy.net/?p=22), his
  own site, read 20 Nov 2026.
- Tynan Sylvester, [The Simulation Dream](https://tynansylvester.com/2013/06/the-simulation-dream/), 2 June 2013, his own site, read
  20 Nov 2026.
- Daniel New, [Breath of the Wild, science, and clever little lies](https://www.thumbsticks.com/gdc-17-breath-of-the-wild-science-lies),
  Thumbsticks, 2 Mar 2017, on Nintendo's GDC talk [Change and Constant](https://www.gdcvault.com/play/1024562/Change-and-Constant-Breaking-Conventions).
- Matthew Gallant, [Intentionality & Improvisation](https://gangles.ca/?p=465), 28 Feb 2018, quoting Clint Hocking's GDC 2009 talk
  "Fault Tolerance: From Intentionality to Improvisation".
- Vitaliy Yaruta of Saber Interactive, interviewed by [GamingBolt](https://gamingbolt.com/snowrunner-interview-physics-progression-customization-and-more),
  5 May 2020. 3Division, [Report for the Community #72](http://www.sovietrepublic.net/post/report-for-the-community-72/), 16 Apr 2023.
  Martin Rabl of GIANTS Software, interviewed by [UnGeek](https://ungeek.ph/2024/12/interview-giants-softwares-martin-rabl-on-the-stand-out-features-of-farming-simulator-25-and-more),
  16 Dec 2024.
- Dr Chris Henderson, [The search for Mawson's air tractor](https://www.antarctica.gov.au/magazine/issue-18-2010/history/the-search-for-mawsons-air-tractor/),
  Australian Antarctic Magazine 18, 2010, Australian Antarctic Division. [The Ig Nobel Prize winners](https://improbable.com/ig/winners/),
  Improbable Research.
- **Every quotation above was checked word for word against its page on 20 Nov 2026.**
- **Not read or not verified:** Hocking's own slides; the video of Nintendo's talk; Smith and Smith's GDC 2004 talk; any developer's
  word on HighFleet's fuel, Kenshi's world states or Jagged Alliance 2's counter-attacks; Farming Simulator 25's vehicle wear.
  Nothing here rests on them.
