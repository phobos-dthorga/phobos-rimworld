# Sound catalogue: every sound we need, now and planned (10 Nov 2026)

**Status:** an audit and a plan. Four sounds are shipped ([sound generation](sound-generation.md)); everything else here is a
proposal. Nothing in this record is built or tested in game. Each row has an ID (S-numbers) so request files, commits and the
owner's picks can name it.

## Read this first (gotchas)

- **Owner's direction (10 Nov 2026):** our vehicles get a full set of sounds, as rich as GTek's Oshkosh L-ATV (doors, engine start,
  running and stop), **a unique sound for every vehicle of ours**, and a sample shared only where it is truly generic. The quiet rule
  still applies: nothing alarming, and the one volume setting ("D'thorga sounds volume") covers every sound.
- **What "truly generic" means here (agent's test):** the same physical thing makes the sound wherever it happens, so a player would
  expect it to sound the same: the same switch, the same fuel nozzle, the same jump-start kit, the same oxygen regulator, the
  same dashboard. **Engines, doors, brakes and working machinery are never shared**: each vehicle's are its own.
- **About 130 sounds** in all: roughly 60 for our seven vehicles, 15 generic vehicle-systems sounds, 25 medical, 10 for other parts,
  and about 25 for designed things not yet built. At the pilot's pace (distilled model, 4 to 9 s a sound on the processor, beside the
  game), generating candidates is the cheap part; the owner's listening and picking is the real work, so batches stay small.
- **Three kinds of hook,** marked in every table: **XML** needs only Defs (Vehicle Framework's events, the game's own sound fields);
  **Code** needs a small C# hook (a sound field on one of our comps or jobs); **Feature** waits for the thing itself to be built.
- **Engines follow rpm and load** (Framework 0.76.0): our systems play our vehicles' engine loops and steer their pitch and blend, so
  Vehicle Framework's own event loops (which nothing can change while they play) are not used for them.

## What the L-ATV does, and what we can do

GTek's L-ATV (read 10 Nov 2026 with `query-defs`) has nine SoundDefs: five weapon shots, and four on Vehicle Framework events:

| Event | L-ATV sound |
| --- | --- |
| A pawn gets in | an armoured door |
| A pawn gets out | the same door |
| Ignition on | engine start |
| Ignition off | engine shut-down |
| Ignition on until off (a loop) | heavy engine running |

Vehicle Framework offers 36 events (`Vehicles.VehicleEventDefOf`). The ones worth a sound: ignition on and off, braking, moving off
and stopping, a pawn getting in or out, cargo added or removed, refuelled, out of fuel, deployed and undeployed, damage taken,
repaired, upgrade completed, and the aircraft events. **Every one of these is XML only** (`soundOneShotsOnEvent`,
`soundSustainersOnEvent`). Our set-up, pumping, tipping, drilling, diving and surveying are our own comps, so those need a code hook.

**Today our vehicles inherit only the game's draft blip** on ignition (`DraftOn`, `DraftOff`, from Vehicle Framework's
`BaseVehiclePawn`). The tanker borrows an engine note from Vanilla Vehicles Expanded when that mod is active
(`Mods/VanillaVehiclesExpanded/Patches/Tanker_Sound.xml`), and the drill rig's work borrows the game's own drill (`Interact_Drill`).

## Generic library (shared by design)

| ID | Sound | Used by | Hook | Status |
| --- | --- | --- | --- | --- |
| S1 | Switch click, on and a lower off | Every physical switch on our vehicles | Code | **Shipped** (Framework 0.71.0; the dashboard's switches click too since 0.75.0) |
| S2 | Sonar ping | A submerged hull spotted (Crawler, and the test hull since Vehicles 0.34.0); later the active sonar set (the listening-array session plans to reuse it) | Code | **Shipped** (Vehicles 0.31.0) |
| S3 | Fuel nozzle and glug | Refuelled, on all seven vehicles; the depot bay filling a vehicle | XML (Refueled) | Proposed. One chemfuel nozzle, so one sound |
| S4 | Jump-start kit: clamps on, a short crank | The jump-start job, wherever it is used | Code (job) | Proposed |
| S5 | Booster pack: clamps on, a soft relay click, a crank | The booster pack | Code (job) | Proposed |
| S6 | Dashboard warning chime, one soft tone | A red telltale lighting on a vehicle the player owns | Code | Proposed. **Never alarming**: one tone, no repeat |
| S7 | Tyre bursting, then flapping | Tyre breakdown, any vehicle with our systems | Code (schema effect) | Proposed |
| S8 | Coolant boiling over, steam hiss (loop) | Coolant boiling, any vehicle | Code | Proposed |
| S9 | Failed start, flat battery: solenoid clicks | Start refused for a flat battery or a dead starter | Code | Proposed |
| S10 | Failed start, slow crank that dies: diesel, petrol (two sounds) | Start refused cold or weak, by engine class | Code | Proposed. Shared because it covers other mods' vehicles too |
| S11 | Engine knocking (loop): diesel, petrol (two sounds) | Oil pressure gone or a worn engine, over the vehicle's own engine | Code | Proposed |
| S12 | Worn brakes squealing | Stopping with brakes worn | Code | Proposed |
| S13 | Air compressor charging, governor purge | Vehicles with air brakes, after stops | Code | Proposed |
| S14 | Oxygen flowing from a regulator (loop) | Bedside oxygen stand, cylinder trolley, field oxygen kit | Code | Proposed. The same regulator and mask in all three |
| S15 | Steam safety valve lifting | Boiler blowing off (Trains of the Rim loadouts) | Code | Proposed |
| S16 | Battery thermal runaway, venting hiss | Electric vehicles' traction packs | Code | Proposed |
| S17 | Cargo set down and taken off (soft) | Cargo added or removed, all vehicles | XML | **Owner to decide**: it fires per stack, so it may chatter |

## Our vehicles (each set unique)

Every vehicle replaces the inherited draft blips with its own sounds (`Inherit="False"`, as the L-ATV does). "Door" means one
clip for getting in and out, with two or three takes so a crew boarding does not repeat one sound.

### Bulk tanker (`Phobos_Tanker`; heavy diesel, air brakes)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| S20 | Engine start: big diesel | XML | Replaces the borrowed Vanilla Vehicles Expanded note; that patch is removed |
| S21 | Engine running (loop): big diesel, laden | XML | |
| S22 | Engine stop | XML | |
| S23 | Cab door | XML | |
| S24 | Air brakes hiss | XML (Braking) | |
| S25 | Out of fuel: sputter and stall | XML | |
| S26 | Hoses coupled and uncoupled | Code (set up, pack up) | |
| S27 | Pump and power take-off running (loop) | Code (Load and Unload modes) | Engine at idle under it |
| S28 | Relief valve venting | Code (tank event) | |
| S29 | Tank leaking (hiss or trickle, by contents) | Code | |
| S30 | Purge: air blowing through the tank (loop) | Code (purge job) | |

### Tipper truck (`Phobos_Tipper`; heavy diesel)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| S31 | Engine start | XML | Its own diesel, not the tanker's |
| S32 | Engine running (loop) | XML | |
| S33 | Engine stop | XML | |
| S34 | Cab door | XML | |
| S35 | Air brakes hiss | XML (Braking) | |
| S36 | Out of fuel | XML | |
| S37 | Power take-off engaging, then hydraulic rams lifting (loop) | Code (tip) | |
| S38 | Load sliding out: rock body (clatter) | Code | By body upgrade |
| S39 | Load sliding out: bulk body (pour) and the standard body (thuds) | Code | Two sounds |
| S40 | Tailgate swinging and banging shut, body seating | Code (tip done) | |

### Drill rig (`Phobos_DrillRig`; heavy diesel, hydraulic mast)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| S41 | Engine start | XML | |
| S42 | Engine running (loop) | XML | |
| S43 | Engine stop | XML | |
| S44 | Cab door | XML | |
| S45 | Air brakes hiss | XML (Braking) | |
| S46 | Out of fuel | XML | |
| S47 | Outriggers down, mast raising (hydraulics) | Code (set up) | Packing up plays it lowering, or its own take |
| S48 | Drilling (loop): rotary head, compressor, flushing | Code | Replaces the game's drill |
| S49 | Drilling with the hammer bit (percussive) | Code | By upgrade |
| S50 | Drilling on shore power (electric pack, no engine under it) | Code | By upgrade; the record calls it quieter |
| S51 | A portion dropping down the cuttings chute | Code | |
| S52 | Winding down when the ground is spent | Code | |

### Survey truck (`Phobos_SurveyTruck`; light 4x4 diesel)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| S53 | Engine start | XML | A lighter diesel; it also starts by itself to recharge |
| S54 | Engine running (loop) | XML | |
| S55 | Engine stop | XML | |
| S56 | Door | XML | |
| S57 | Out of fuel | XML | |
| S58 | Sensor boom swinging out and back (hydraulics) | Code (set up) | Matches the boom's 2 s motion |
| S59 | Surveying (loop): console and recorder | Code | The survey job is silent now |
| S60 | Powered array humming (loop) | Code (array load on) | |
| S61 | Long-range dish turning | Code | |
| S62 | A find: one soft tone | Code (find letter) | Never alarming |

### Generator truck (`Phobos_GeneratorSet`; diesel-electric)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| — | Generator running (loop) | Code | **Shipped** (Vehicles 0.31.0) |
| S63 | Engine start | XML | |
| S64 | Driving (loop): diesel with traction-motor whine | XML | Different from the set-up loop |
| S65 | Engine stop | XML | |
| S66 | Door | XML | |
| S67 | Out of fuel | XML | |
| S68 | Outriggers down, cable reel paying out | Code (set up) | Reeling in on pack up |

### Seabed Crawler (`Phobos_SeabedCrawler`; electric drive under water)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| — | Spotted ping | Code | **Shipped** (S2) |
| S69 | Power up: contactors closing, motors spinning up | XML | |
| S70 | Underway (loop): motors and propeller | XML | |
| S71 | Power down | XML | |
| S72 | Hatch, with its wheel | XML | |
| S73 | Diving: tanks flooding, vents (15 s) | Code | |
| S74 | Surfacing: blowing tanks (10 s) | Code | Also a forced surfacing |
| S75 | Settling on the bottom: a soft bump, then creaks | Code | Lifting off too |
| S76 | Hull creaking while submerged (very quiet loop) | Code | |
| S77 | Air scrubber fan (loop) | Code | By upgrade |
| S78 | Laboratory at work (quiet loop) | Code | By upgrade, while research runs |
| S79 | Listening array: a contact heard | Code | A soft cue, never a ping (the array gives nothing away) |

### Submarine test hull (`Phobos_SubmarineTestHull`)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| S80 | Power up, underway, power down, hatch, dive, surface | XML and Code | **Owner to decide:** agent recommends sharing the Crawler's set and the ping, as a test hull of the same technology. Otherwise six unique sounds |

## Vehicle stations (D'thorga Framework)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| S81 | Tuning rig at work (loop): analyser, air tools | Code | The parts bench keeps the game's machining |
| S82 | Depot bay pump running (loop) | Code | Its nozzle is S3 |

## Medical (D'thorga Medical; each machine and device its own)

| ID | Sound | Hook | Note |
| --- | --- | --- | --- |
| — | Oxygen concentrator hum | XML | **Shipped** (Medical 0.12.0) |
| S83 | Ventilator: bellows breathing (loop) | Code (while ventilating) | Not merely powered: only with a patient |
| S84 | Nebuliser: compressor and mist (loop) | Code (while treating) | |
| S85 | Anaesthesia machine: soft bellows (loop) | Code (during surgery) | |
| S86 | Burn unit bed: air-cell blower (loop) | XML (powered) | |
| S87 | Hyperbaric chamber: door sealing, pressurising, then a hum | Code | |
| S88 | Oxygen electrolyser: bubbling and a transformer hum (loop) | XML (powered) | |
| S89 | Liquid oxygen store: cooler cycling (loop) | XML (powered) | |
| S90 | Liquid oxygen boiling off, venting | Code (unpowered) | |
| S91 | Safety shower drenching (loop) | Code (rinse) | |
| S92 | Rinsing a patient: a bucket poured | Code | |
| S93 | Steriliser: burner and steam | **Owner to decide** | It never ticks, so it has no working moment yet |
| S94 | Manual resuscitator: bag squeezed, steady rhythm (loop) | Code (device job) | |
| S95 | Intubation: laryngoscope click, tube | Code (device job) | |
| S96 | Fluids kit: bag hung, line primed | Code (device job) | |
| S97 | Needle decompression: a short hiss of air | Code (device job) | |
| S98 | Field oxygen kit: cylinder valve cracked | Code (device job) | Then S14 |
| S99 | Gas alarm | **Owner to decide** | Silent today by the earlier sound rule; its description says it "sounds" |
| S100 | Coughing and wheezing (smoke, airway burns) | **Owner to decide** | Pawn sounds; may wear thin |

The device sounds share one small hook: a sound field on the device's Def that `JobDriver_PhobosUseDevice` plays, so each device
stays its own sound with no new code per device.

## Other mods of ours

| ID | Sound | Mod | Hook | Note |
| --- | --- | --- | --- | --- |
| S101 | Water running into bottles (short) | Water Tap | Code (a fill) | |
| S102 | Base station: squelch and static (loop) | Radio | Code | **Back burner**, like all radio work |
| S103 | Hand-held: squelch tail and key-up | Radio | Code | Back burner; the private recording library with VK3PKT could supply real ones |

D'thorga Factory Additions' fabrication bay and processes already use Vanilla Furniture Expanded - Factory's own sounds, which suit
them, and D'thorga Patches adds nothing of its own to hear.

## Vehicle systems on other mods' vehicles

Our loadouts give other mods' vehicles our systems, so the **generic** sounds (S6 to S16) reach them. Their engines, doors and
weapons stay their authors' own: we never replace another mod's vehicle sounds.

## Planned, not built (sounds wait for the feature)

| ID | Sound | For | Source |
| --- | --- | --- | --- |
| S110 | Hydraulic pump whine, and a line leaking | Hydraulics readers (another session is building them) | vehicle-crosslinks-round-two.md T1 |
| S111 | Drive belt squeal, then a snap | Drive belt (being built) | round two T2 |
| S112 | Clutch slipping | Brake and clutch wear | round two |
| S113 | Exhaust brake | Fitting F6 | round two |
| S114 | Turbo whistle (loop over the engine) | The turbocharger fitting, built | vehicle-tuning-and-upgrades.md |
| S115 | Inspection pit and hoist | Stations (being built) | round two S1 to S4 |
| S116 | Active sonar ping | Sonar set | vehicle-seabed-crawler.md; may reuse S2 |
| S117 | Manipulator arm servo, a grab | Manipulator arm | vehicle-seabed-crawler.md |
| S118 | Oxygen candle lit, chemical hiss | Oxygen candles | vehicle-seabed-crawler.md |
| S119 | Radar mast raising, radar turning | Radar mast | vehicle-seabed-crawler.md |
| S120 | Seabed drill | Seabed drill | vehicle-seabed-crawler.md |
| S121 | Compact reactor hum | Compact reactor | vehicle-seabed-crawler.md |
| S122 | Torpedo launched; a hostile boat heard | Torpedoes, enemy submarines | submarine-framework.md |
| S123 | Gas turbine hung start and hot start | Turbines on others' aircraft and tanks | vehicle-turbine-design.md |
| S124 | Extractor fan, fume hood | Ventilation (deferred) | ventilation-and-hydrogen.md |
| S125 | Hydrogen detector | Deferred by the owner | ventilation-and-hydrogen.md; same question as S99 |
| S126 | Each new utility vehicle (excavator, bulldozer, crane, recovery truck, flatbed winch, ambulance without a siren, and more) | Sketched in the catalogue | utility-vehicles-design.md: a unique set each, as above, when built |
| S127 | Radio phases 2 to 4 (HF static crashes, dish rotator, direction finding, moonbounce) | Back burner | radio-framework-design.md |

## Order of work (agent's proposal)

1. **XML only, our vehicles' voices:** engines, doors, brakes, out of fuel and the shared nozzle for all seven (S3, S20 to S25,
   S31 to S36, S41 to S46, S53 to S57, S63 to S67, S69 to S72), plus the dashboard click gap and the test hull's ping. About 35
   sounds, no new code. The biggest change a player hears.
2. **Our vehicles at work:** set-up, pumping, tipping, drilling, surveying, diving (S26 to S30, S37 to S40, S47 to S52, S58 to S62,
   S68, S73 to S79). One shared hook design (a set-up sound and a work loop on our comps), coordinated with the session working on
   the rig, tipper and survey truck. Since Framework 0.73.0 their work runs at a hydraulic pace (`CompPhobosRigWork.WorkPace`; the
   tipper's rate times `CompPhobosVehicleSystems.HydraulicSupply`) and stops on `HydraulicRefusal()`: a work loop starts and ends with
   that, and a stalled lift is where S110's strain belongs. The tuning rig's S81 goes on the new Diagnose job
   (`JobDriver_PhobosDiagnose`), and S115 on the inspection pit, both also new in 0.73.0.
3. **Medical machines and devices** (S83 to S98), with the one device hook.
4. **Generic vehicle systems** (S4 to S16), and the stations (S81, S82).
5. **Planned features** take their sounds as they are built (S110 onwards).

## Shipped from the generic library (10 Nov 2026; Framework 0.75.0, Medical 0.13.0, Vehicles 0.34.0; not yet tested in game)

S3 to S17 are in, except the field oxygen kit's part of S14, which waits for the device hook (S98). How each is hooked: stage sounds
through `Effect_Sound` and `Effect_SoundLoop` in the system XML (tyre burst on the tyre breakdown, boiling, the knock while oil pressure
is gone and the engine runs, the squeal on a stop with brake wear above 0.7, the compressor on each stop with air brakes, the safety
valve on blowing off, battery venting in a runaway); the nozzle and cargo on the fuel and running-gear systems' events; failed starts
on `Effect_RefuseDraft` (played from the dashboard's key only, as Vehicle Framework greys its own button); the chime from the stage
rule `StageSpec.Severe`; the jump-start sounds in its job. One-shots and the chime wait ten seconds after a vehicle spawns or loads.
The setting "Vehicle system sounds on other mods' vehicles too" (on) sits under "D'thorga sounds volume". The game's own references
for very short clips are matched by true peak (`scripts/art/game_sounds.py`), since a loudness meter reads them as silence.

## Phase 1 shipped (11 Nov 2026; Framework 0.76.0, Vehicles 0.35.0; not yet tested in game)

Engines, doors, air brakes and sputter for all seven vehicles (S20 to S25, S31 to S36, S41 to S46, S53 to S57, S63 to S67, S69 to S72;
the test hull shares the Crawler's). **Engines follow the drive** (owner's choice: two loops blended): `PhobosEngineSoundExtension` on a
vehicle's Def names an idling and a labouring loop, D'thorga Framework plays them in place of a Vehicle Framework event loop and sets
`PhobosEnginePitch` (0.9 at idle to 1.35 at rated, from `CompPhobosVehicleSystems.EngineRpm`) and `PhobosEngineLoad` (from `EngineLoad`,
which covers rigs working while parked and accessories), and each loop's SoundDef maps them onto pitch and volume every frame, as the
game's wind turbine does. Doors are one clip with two or three takes (owner's choice). The borrowed Vanilla Vehicles Expanded tanker
engine is gone. The export now tries four crossfade lengths and both curves and keeps the join with the least dip: a rough, pulsing
engine (the tipper) joined cleanly at one length and dipped at others.

## Owner's answers (10 Nov 2026)

- **The generic library comes first**, generated with the distilled model, 3 seeds a sound
  (`assets/phobos-sounds/requests/generic-library.json`).
- **Cargo sounds (S17): included**, soft, one at a time.
- **Dashboard chime (S6): red lamps only**, once, never repeating.
- **Our system sounds on other mods' vehicles: yes, with a setting to turn them off** there (the owner: "a main menu setting to
  disable"). It goes in D'thorga Framework's settings beside "D'thorga sounds volume".
- **The test hull (S80) shares the Crawler's sounds**, and gets the spotted ping.

## Open questions for the owner

Still open (questions 2 and 3 are answered above):

1. **The gas alarm (S99), and the hydrogen detector later:** keep them silent (the earlier rule: never alarming), or give them a
   soft, plain tone now that a sound belongs in the game? An alarm that cannot be heard is an odd alarm.
2. **The test hull (S80):** share the Crawler's sounds (agent's recommendation), or a set of its own?
3. **Cargo sounds (S17):** worth having, given they fire for every stack loaded?
4. **Coughing (S100):** wanted, or too much over a long fire?
5. **Doors:** one clip for getting in and out, as the L-ATV does, or separate opening and closing sounds?
6. **The generic list:** anything there you would rather have unique, or anything unique you would share?
