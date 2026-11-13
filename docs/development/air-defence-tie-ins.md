# Air-defence tie-ins: InterRim Ballistic Missile, The Dead Man's Switch and our aircraft (13 Nov 2026)

**A research record. Nothing is built, and nothing here is tested in game.** The owner, 13 November 2026, asked that the
[soft-dependency exploration](soft-dependency-crosslinks.md) include InterRim Ballistic Missile and The Dead Man's Switch "for their
anti-air, RADAR, FCS, etc.". This record answers that part. It joins four existing designs: [avionics](vehicle-avionics.md) (A10 to
A13, A16), [cross-map strikes](cross-map-strikes.md), the [submarine framework](submarine-framework.md) and the
[seabed crawler](vehicle-seabed-crawler.md)'s radar mast.

How to read it:

- Facts marked "from the code" were read on 13 Nov 2026 with `scripts/query-code.py` from decompiles made that day of the installed
  builds: `DMSE` is DMS Expedition's `DMSE.dll` of 12 Nov 2026 (tree `DMSE-1.7-2026-11-12`), `Fortified` its framework's `Fortified.dll`
  of 12 Nov 2026, `IRBM` InterRim Ballistic Missile's 1.6 assembly (unchanged since 30 Jun 2026), `VF` Vehicle Framework 1.6. Decompiles
  stay in `.local/` and are never committed. Defs come from `scripts/query-defs.py` as written; the patched-Defs file (8 Nov 2026) is
  older than DMS's 12 Nov update, so DMS figures are as its authors wrote them before any other mod's patch.
- **AD rows** are proposals; every name in them is proposed. Kinds as in the cross-links record: **XML**, **C#** (by name),
  **assembly** (a tie-in assembly naming their types), **hook** (a Harmony patch into their code).
- The owner's earlier answer for aircraft still holds: **our conditions may scale other authors' figures through small non-skipping
  hooks** (13 Nov 2026), while their art and designs stay theirs.

## Read this first

- **Neither mod can see a Vehicle Framework aircraft, in the air or landing.** From the code:
  - On a map, IRBM's CIWS intercepts the game's `Projectile` and `Skyfaller` only (`IRBM/Building_CIWS.cs:518`, `:535`), and DMS
    Expedition queues only things made through the game's `SkyfallerMaker` (`DMSE/Patch_MakeDropPodAt.cs:17`). Vehicle Framework's
    landing and take-off use `VehicleSkyfaller`, which derives from `Thing`, not `Skyfaller` (`VF:24237`).
  - On the world map, DMS Expedition's blockades shoot at transport pods, gravships and its own transfer ships only
    (`DMSE/BlockadeHitUtility.cs:17-32`); nothing in DMSE or Fortified mentions Vehicle Framework's `AerialVehicleInFlight` (`VF:90137`).
  - So every aircraft, ours and raiders' VTOLs alike, flies through both mods' air defences untouched. **That is the biggest gap and
    the biggest opportunity** (AD3).
- **The air-defence code is in DMS Expedition**, not DMS core. Core has guns named CIWS (ordinary guns), a fire-control crafting part
  and a facility alert network; MobileDragoon has no code.
- **DMS Expedition has a public plug-in interface** for other mods, in namespace `Fortified` inside `DMSE.dll` (still there in the 12 Nov
  build): `AirDefenseExtensionDef` (an XML Def) holding modifiers, listeners and track providers. Much of what follows needs no Harmony.
- **In the owner's Combat Extended game, IRBM's CIWS cannot shoot down mortar or artillery shells**: it knows only the game's
  `Projectile` type, and CE's shells are `ProjectileCE`. It still takes skyfallers, world missiles and DMS's missiles. IRBM has had no
  update since 30 Jun 2026, declares 1.6 only, and no uploader reply was seen (AD14).
- **DMS Expedition's heat seekers changed on 12 Nov**: they now aim at the hottest room first, then the biggest pawn
  (`DMSE/GuidanceType_HeatSource.cs:22-46`). Vehicles are prime targets in the open (AD5).

## What each mod has

### DMS Expedition (`aoba.deadmanswitch.expedition`; updated 12 Nov 2026, 1.7; uploader replied 8 Nov)

**Search radars** (`CompProperties_SearchRadar`, `DMSE/CompProperties_SearchRadar.cs:5-25`). They make their map a node in the world
air-defence network (`WorldComponent_BVRNetwork`), set the map's warning window (`MapComponent_BVRCombat.ComputeWarningWindow`,
`:203-226`), decide which faction defends the map (`ResolveDefender`, `:84-111`) and close the airspace to AI air-drops.

| Def | World range (tiles) | Map search | Anti-stealth | Power | Research |
| --- | --- | --- | --- | --- | --- |
| `DMSE_AcousticEarlyWarningRadar` | 8 | 120 | 0 | none | `DMSE_RadarBasis` |
| `DMSE_SearchRadar` | 12 | 200 | 1 | 2,500 W | `DMSE_RadarNetwork` |
| `DMSE_PhaseArrayRadar` | 12 | 200 | 2 | 9,000 W | `DMSE_IntegratedAirDefense` |
| `DMSE_OTHWarningRadar` | 40 | 360 | 0 | 12,000 W | `DMSE_ElectronicWarfare` |
| `DMSE_Building_FieldAirDefense` | 4 | 100 | 0 | 800 W | `DMSE_FireControlNetwork` |

**Fire-control radars** (`CompProperties_FireControlRadar`, `:9-110`). Hit chance is the maximum × the share of guidance time × an
altitude factor × a speed factor × a stealth factor per level of stealth × a chaff penalty × a power factor
(`DMSE/CompFireControlRadar.cs:55-72`).

| Def | Targets | Max hit | Lock (ticks) | Altitude (m) |
| --- | --- | --- | --- | --- |
| `DMSE_EarlyFireControlRadar` (needs a crew) | 1 | 0.75 | 200 | 100 to 20,000 |
| `DMSE_FireControlRadar` | 4 | 0.85 | 150 | 50 to 40,000 |
| `DMSE_Building_FieldAirDefense` | 2 | 0.8 | 150 | |
| `DMSE_PhaseArrayRadar` | 24 | 0.9 | 120 | 30 to 150,000 |

**Shooters.** Interceptor launchers (`CompMissileLauncher_Interceptor`) on the rotary launchers and the field system; terminal CIWS
(`CompTerminalCIWS`) on `DMSE_Turret_LegacyFlak` (0.2), `DMSE_Turret_Vulcan_WingBack` (0.25), the field system (0.35) and
`DMSE_Turret_Millennium` (0.85), engaging only targets already queued; a man-portable SAM (`DMSE_Apparel_SamLauncher`, 40 cells). Each
turret also has an ordinary gun for ground fire.

**What is queued for interception**: skyfallers made through `SkyfallerMaker` whose contents have a faction, the player's shuttle
arrivals, and DMS's own missiles.

**World map**: settlements get air defence (`WorldObjectComp_AirDefense`: detection 6 to 12 tiles, channels, stock); airspace blockades
shoot at pods, shuttles, gravships and transfer ships in flight (`BlockadeHitUtility.ApplyHit`, `:34-56`); a prompt warns of the risk
before a launch (`InterceptRiskUtility.ConfirmOrDefer`, `:37-89`). Tuning, chaff included, is in `DMSE_BVRWorldTuning`.

**Electronic warfare and aiming**: a jammer station (`DMSE_JammerStation`) that also blocks comms; a crewed war-room console whose
operator's skills scale radar range and launcher cooldown; laser designators (`LaserDesignatorUtility.LatestMarkFor`, `:55-75`, public
static); seekers by laser mark, heat (hottest room, then the biggest pawn), toughest building (anti-ship) and active radar buildings
(anti-radiation).

**A target's signature** is an XML extension, `BVRTargetProps` (`DMSE/BVRTargetProps.cs:6-41`): distance level, stealth level, speed,
altitude, and since 12 Nov a combat domain, a terminal intercept factor and a debris profile.

### DMS core and the alert network (`aoba.deadmanswitch.core`; updated 12 Nov 2026, 1.7; uploader replied 3 Nov)

- Guns named CIWS (`DMS_Gun_Tunguska`, `DMS_Gun_2A38M`, `DMS_Gun_MK44_*`) are ordinary guns; under CE the Tunguska fires
  `Ammo_9M311_SAM`. `ComponentFireControl` is a crafting part.
- The facility alert network (`Fortified/CompAlertScanner.cs`) feeds alert effects. One is the **airspace blockade**
  (`Fortified/AlertEffectWorker_AirspaceBlockade.cs:7-100`): when a facility's alert goes off, typically at a base the player attacks, it
  blocks the map's airspace for 5,000 ticks and calls an air strike every 1,800, registered with `MapComponent_AlertCounter`.

### InterRim Ballistic Missile (`kazepsi.irbm`; updated 30 Jun 2026, 1.6 only, no uploader reply seen)

- **Radars**: Doppler (`IRBM_AntiMissileRadar_Basic`: 120 tiles, level 1, 300 W) and phased array (`IRBM_AntiMissileRadar_Advanced`:
  300 tiles, level 2, 800 W), on the player's home maps only (`IRBM/RadarUtility.cs:17-41`). Detection compares range × tile size × the
  missile's stealth, which depends on altitude and body (`RadarUtility.IsMissileDetected`, `:63-102`, public static). AI factions get the
  same figures by tech level.
- **CIWS**: guns (range 24, 600 W), laser (30, 1,500 W) and Stonehenge (250, 12 kW), firing every tick while unroofed and powered
  (`IRBM/Building_CIWS.cs:342-421`); on a map they cover the whole map, with no range check. They take world missiles, hostile overhead
  `Projectile`s heading for the player's things (`:518-533`) and skyfallers carrying hostile pawns (`:535-557`).
- **Silo interceptors**: automatic anti-air from silos (`Building_MissileSilo.TickAutoAntiAir`), warheads 0.5 to 3.0 efficiency.
- **Satellites**: recon, bombardment, energy beam and tractor. A recon satellite keeps the tile it watches loaded; an AI recon
  satellite gives its faction vision of every map (`AIFactionMissileUtility.cs:97-111`).
- **AI launches**: with no launch base in range, the AI fires from a random ocean tile (`AIFactionMissileUtility.cs:406-410`), a
  submarine launch with no submarine.

### Combat Extended

- IRBM: **no CE patches**; its CIWS cannot see CE shells (above). Its ground-mode damage was written for vanilla armour; how it fares
  against CE armour is not checked.
- DMS Expedition ships its own CE patch for ground fire (CE guns and ammunition for the flak turrets); terminal interception stays its
  own and is unchanged. Its field system needs CE's autoloader to keep firing.
- CE's own CIWS (`Building_CIWS_CE`) sees only CE projectiles and skyfallers, so it would not see DMS's missiles.

## What already happens without any work

- IRBM's CIWS shoots down DMS Expedition's incoming missiles on a map: they are ordinary overhead projectiles.
- DMS heat seekers fall back on the biggest pawn, so vehicles in the open draw them.
- With a DMS search radar running, DMS takes hostile drop pods into its own queue; whether IRBM's CIWS still sees them is not traced.

## The links

| # | Link | Reads → changes | Lets · Stops · Changes | Kind | Serves | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| AD1 | **Radar-directed guns hit the Harrier more often** | An active hostile fire-control radar on the map (`MapComponent_BVRCombat.fireControlRadars`, `:15`, public) → scale the Harrier's evasion (`Evasion.ChanceFor`), on the hook A12 already plans | **Lets** a DMS base feel defended. **Stops** the Harrier dodging radar-laid guns as easily as rifles. **Changes** the first job of a strike: kill the radar | Hook (one postfix, shared with A12) | Avionics A12 | **Take, with A12** |
| AD2 | **A warning receiver** | DMS's air-combat events (`AirCombatEvents.Raised`, `Fortified/AirCombatEvents.cs:24`: detected, lock started, launched) on a track that is ours → a message, and the avionics Warning capability decides whether the crew hears it | **Lets** a crew know they are locked. **Stops** silent shoot-downs. **Changes** when to turn back | Assembly (a listener in an `AirDefenseExtensionDef`) | Avionics A16 | Take with AD3; it needs our aircraft to be tracks first |
| AD3 | **Our flights become tracks DMS can shoot at** | A track provider (`AirTrackProvider.CollectTracks`) exposes each `AerialVehicleInFlight` as an `IAirTrack` (`Fortified/IAirTrack.cs:7-45`); `Notify_Intercepted` damages its parts through our part-hit rule; a modifier (`AirDefenseModifier`) reads our Countermeasures and Warning capabilities; `InterceptRiskUtility.ConfirmOrDefer` runs before our launch | **Lets** a SAM belt and blockade matter to the player's air force, and a defended base deserve an escort. **Stops** aircraft flying free over defended settlements. **Changes** routes: around defences, or through them with countermeasures | Assembly, plus a fallback postfix on `BlockadeHitUtility.InFlight` | Avionics A16; cross-map strikes | **Take, second**: the largest row, and it gives the aircraft an opponent |
| AD4 | **A laser designator guides Harrier bombs** | `LaserDesignatorUtility.LatestMarkFor(map, faction)` → the A10 bomb-aim postfix snaps to the mark when the sight works | **Lets** a spotter on the ground call a precise strike. **Stops** nothing. **Changes** how a strike is set up | Hook (A10's) | Avionics A10 | With A10 |
| AD5 | **Hot engines draw heat seekers** | Our engine temperature → in the open, a running or recently run vehicle is preferred over a cold one by the heat seeker's fallback (`GuidanceType_HeatSource.PickTarget`, `:22-46`) | **Lets** a cold, parked vehicle hide. **Stops** the biggest body always being the target. **Changes** when to start engines before a fight | Hook into their targeting | Round three's combat rows | **Ask**: it changes their targeting, not only a figure |
| AD6 | **Strikes we cannot see are still reported** | IRBM's recon satellite coverage (`SatelliteRegistry.IsTileObserved`) or a DMS node that sees the tile → the strike report says what hit | **Lets** the player know a strike landed on an unloaded map. **Stops** silent results. **Changes** the value of a recon satellite | C#, read only | Cross-map strike rule 6 | Take with cross-map strikes |
| AD7 | **A blockade grounds our aircraft too** | `MapComponent_AlertCounter`'s blockade (`AlertEffectWorker_AirspaceBlockade.IsAirspaceBlocked`) → our launch refusal, with the reason, as it grounds shuttles | **Lets** a DMS base trap an air assault that sets off its alert. **Stops** an easy air escape. **Changes** raids on DMS bases: kill the scanners first | C# by name | Launch refusals (A1) | Take with A1 |
| AD8 | **Our radar mast or a radar vehicle is a DMS search radar** | `CompProperties_SearchRadar` added by `MayRequire` to our Defs → the map becomes a node; a postfix on `CompBVRDevice.IsOperational` (`:30-67`) ties it to our Radar capability. Anti-radiation seekers pick only buildings, so a vehicle radar is safe from them | **Lets** a mobile early-warning radar. **Stops** nothing. **Changes** convoy and crawler roles | XML, plus a small hook; a comp on a vehicle pawn is untested | Crawler radar mast; avionics A13 | Read first: whether its comp runs on a vehicle |
| AD9 | **Our sensors count for IRBM's radar** | A postfix on `RadarUtility.IsMissileDetected` adds our radar's range and level | **Lets** a radar vehicle or the crawler's mast warn of missiles. **Stops** nothing. **Changes** where radars go | Hook | Crawler; avionics A13 | Take with the mast |
| AD10 | **Enemy submarines fire IRBM's sea launches** | Our hostile boat's tile → the launch tile in `AIFactionMissileUtility.LaunchMissile` (`:595`), in place of a random ocean; our sonar or listening array can then hear the launch | **Lets** a submarine hunt prevent strikes. **Stops** launches from nowhere. **Changes** naval play into strategic defence | Hook | Submarine framework (enemy boats) | Take when enemy submarines exist |
| AD11 | **Our future missiles can be intercepted** | Launch them as the game's skyfallers or projectiles with `BVRTargetProps` → enemy DMS defences engage them at no cost to us | **Lets** a defended target be hard to hit. **Stops** a free strike. **Changes** the case for stealth and saturation | XML signature | Cross-map strike rule 7 | Take when the missiles are built |
| AD12 | **A flak or SAM vehicle defends the base** | Our vehicle implements `IAirEffector` (`Fortified/IAirEffector.cs:6-23`) and registers with `MapComponent_AirEffectors` (`:12-18`) | **Lets** air defence that moves. **Stops** nothing. **Changes** convoy escort | Assembly | A future vehicle | Later: needs a vehicle to carry it |
| AD13 | **Each mod sees the other's missiles** | IRBM's `WorldObject_Missile` as a DMS track; DMS radars count for IRBM (AD9's postfix reading DMS nodes) | **Lets** one air-defence picture. **Stops** world missiles slipping between two systems. **Changes** little unless both are used against each other | Assembly | None of ours | Low: a bridge between two other mods, with nothing of ours in it |
| AD14 | **IRBM's CIWS sees Combat Extended's shells** | IRBM's target test (`Building_CIWS.cs:518`) → also CE's `ProjectileCE`, hostile and heading for the player's things | **Lets** the player's CIWS stop mortars under CE. **Stops** a costly turret sitting idle. **Changes** base defence against artillery | Assembly in Patches, loaded with both IRBM and CE | IRBM in the owner's CE game | **Ask**: it patches another author's gap; IRBM looks quiet, which by our rule would allow it |

## Avionics rows revisited

- **A16 countermeasures** was "later", partly because "the anti-air in this game is guns and rockets". DMS Expedition has chaff values,
  fire-control lock and seekers: with AD3, flares and chaff have something to beat. **Fresh verdict: with AD3.**
- **A13 radar strike warning** stays with the crawler's mast, now with AD8 and AD9 as the way in.
- **A12 evasion** gains AD1 as its first input from another mod.

## Other factions' vehicles

- **Raids on us**: raiders' VTOLs are invisible to DMS and IRBM today, like ours. A player with DMS radars expects them to fight raiders'
  aircraft; AD3 counts only world-map flights, so **how VTOL Raids brings its aircraft onto the map is not checked** and decides whether
  the player's DMS defences can ever engage them. Under the lite tier, a hostile aircraft has no avionics fit, so AD2's warning and A16's
  countermeasures would not apply to it unless the lite tier gains them.
- **A base we attack**: AD1 and AD7 are about exactly this. A DMS base with fire control and an alert network makes an air assault
  costly, and a blockade grounds the attackers.
- **Captured**: a captured aircraft becomes the player's and is a track like any of ours under AD3.

## Cost per tick

- AD1 reads a public list once per evasion roll. AD2 and AD3 run on DMS's own world-network refresh, not our ticks. AD7 is one look at
  launch. AD9 runs only when IRBM checks a missile. AD5 runs only when a heat seeker picks a target. None needs a tick of its own.

## Questions for the owner

Answered by the owner, 13 Nov 2026:

- **AD3: yes, with countermeasures**: our aircraft become targets for DMS Expedition's world-map air defence, with the warning receiver
  (AD2) and avionics A16's flares and chaff, as one aircraft batch.
- **AD14: patch it**, recorded as a temporary patch that stands down if IRBM fixes it. **Built: Patches 0.19.1**, with a setting (on by
  default); row 16 in [patches for other authors' faults](temporary-patches.md). Not yet tested in game.

Still open:

1. **AD5**: may our engines change DMS's heat-seeker choice? It changes their targeting, not only a figure.
2. **Raiders' aircraft**: should a decompile of VTOL Raids come first, to see whether the player's DMS defences can ever face them?

## Who chose what

- **Owner (13 Nov 2026)**: IRBM and DMS in scope "for their anti-air, RADAR, FCS"; record only; scaling other authors' figures through
  small hooks allowed (from the avionics record).
- **Agent choices**: the AD rows and their verdicts; reading DMS Expedition's plug-in interface as the way in rather than Harmony where
  possible. All are for the owner to revise.

## Sources

- The Dead Man's Switch, DMS Expedition and Fortified Features Framework, by AobaKuma and co-authors: decompiles of the builds
  installed on 13 Nov 2026 (`DMSE.dll`, `Fortified.dll`, `DMS.dll`, `FortifiedCE.dll`), their XML, and DMS Expedition's own CE patch.
- InterRim Ballistic Missile, by Psionic Zephyr: decompile of its 1.6 assembly.
- Vehicle Framework, by SmashPhil: decompile of 1.6.
- Author activity: `scripts/workshop-research.py report`, 13 Nov 2026.
