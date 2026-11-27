# Radio ties with the game's and other mods' things (27 Nov 2026)

**Status (27 Nov 2026):** a research record. Rows RT1 to RT18 are proposals, each with the agent's lean; the owner answered three of
its questions by card the same day (RT1, RT6, RT18). No row is built, and nothing here is tested in game.

**A research record.** The owner, 27 November 2026: "I wish to start integrating more third-party items with our radio mod, vanilla
or otherwise, and the deep simulation areas of it", as "strictly a research and record purview". The owner's picture with it showed
the game's own search for "radio", with Combat Extended's radio pack open.

It is one of two records written together:

1. This record: every radio-like thing in the game, its DLCs and the owner's mod list, and what each could mean to the radio.
2. [Electronic warfare for every aircraft and vehicle](electronic-warfare.md): the EA-6B Prowler's jamming carried into the radio
   and out to every aircraft and vehicle. **Jammers, enemy signals and radars are judged there**, not here; this record only lists them.

Related records: [the tie-in register](tie-in-register.md) (what exists today),
[soft-dependency cross-links](soft-dependency-crosslinks.md) (SD rows, the other mods) and
[air-defence tie-ins](air-defence-tie-ins.md) (AD rows, radars against our aircraft).

How to read it:

- **[C]** read from code, **[D]** read from the Def as patched in the owner's game, **[d]** taken from a description only,
  **[inferred]** the agent's reading, **[authored]** a figure we would make up and label, **[unverified]** a real-world claim with no
  source read for this record.
- **Kind**: **XML** (patches and `MayRequire` only), **C#** (a small change in our code), **hook** (a Harmony patch into the game's or
  another mod's code, in a tie-in assembly loaded only with that mod).
- **Risk** is graded by the shared rule (`external/phobos-modkit/rules/designs.md`): what the player can lose, how much at once, and
  whether they can see it coming. A grade is the agent's proposal until the owner confirms it.
- Every row answers what it **lets** the player do, what it **stops**, how it **changes** play, and **who else notices**.

## Read this first

- **No radio patch touches another mod's thing today.** The radio patches only vanilla generators, batteries and wall materials.
- **Three doors are already open, with no new code:**

  | A thing joins the radio as | By | Limit today |
  | --- | --- | --- |
  | A radio someone wears | `CompProperties_Radio` on any apparel, **any layer** [C] (`src/PhobosRadio/Services/RadioNet.cs`) | The colony's free colonists only |
  | A radio a pawn carries in its body | The same comp on a vehicle, animal or mechanoid of the colony [C] | A rule may switch it off (`ICarriedRadioRule`) |
  | Electrical noise | `RadioNoiseExtension` on a building [C] (`NoiseService.cs`) | **The colony's own buildings only**; another faction's building is never read |
  | Something in the way | `RadioObstructionExtension` on a material, a building or rock [C] | None |

- **What has no door:** a weapon or an item in the hands or the inventory (the net never reads them); a building with a radio that is
  not a station or a remote turret (it is never collected) [C]; anything of another faction.
- **Combat Extended's radio pack is a real rule, with a gap in it.** Binoculars mark a target for artillery only if the spotter
  wears apparel flagged `isRadioPack`, **or the faction has a Combat Extended mortar on the same map**
  (`Verb_MarkForArtillery.MarkingConditionsMet`, `Verbs/Verb_MarkForArtilleryCE.cs:59-82` [C]). So the pack decides only when the guns
  are somewhere else. Eleven other things carry the flag: Biotech's mechanitor headsets and bandwidth pack, the recon helmets, and
  helmets from four other mods [D].
- **Our radios work two amateur bands**: 2 m (144 to 148 MHz) and 70 cm (430 to 450 MHz). A real army manpack works 30 to 76 MHz.
  A pack given real figures cannot talk to our stations without kit for its band (row RT1).
- **Two things the searches found in our own radio**, both of which matter to these rows:
  - a link uses the first end's mode and never checks the other end has it (`LinkBudgetService.cs:46` [C]);
  - local noise is one figure "in 12.5 kHz" and is not scaled to a mode's bandwidth (`NoiseFloor.cs` [C]). The second record's row EW2
    takes this up.
- **The Dead Man's Switch Expedition already jams the comms console**: its jammer, while running, switches off the player's own
  comms console on that map (`DMSE.Patch_CommsJammed`, a postfix on `Building_CommsConsole.CanUseCommsNow` [C]). Anything of ours on
  the console sits beside that patch.
- **Not on the owner's list, or not ready**: Surveillance Shift (the design's first tie-in) is not installed. InterRim Ballistic
  Missile declares 1.6 only and was last updated on 30 June 2026.

## The mods these rows lean on

From `python scripts/workshop-research.py report`, 27 Nov 2026 (the newest 50 comments of each). "None seen" means none in those
comments, not that the author is gone.

| Mod | Updated | Declares | Uploader's last reply | Note |
| --- | --- | --- | --- | --- |
| Combat Extended | 15 Oct 2026 | 1.6 | none seen | Its Workshop build declares 1.6 only; the owner's game runs it on 1.7 (which build was not checked) |
| The Dead Man's Switch II (core) | 12 Nov 2026 | 1.7 | 20 Nov 2026 | Active |
| The Dead Man's Switch - Expedition | 18 Nov 2026 | 1.7, 1.6 | 8 Nov 2026 | Active |
| Fortified Features Framework | 22 Nov 2026 | 1.7, 1.6, 1.5 | 24 Oct 2026 | Active; the engine under The Dead Man's Switch |
| The Dead Man's Switch - Legion | 4 Nov 2026 | 1.7, 1.6 | 4 Nov 2026 | Active |
| Vanilla Gravship Expanded - Chapter 2 | 20 Oct 2026 | 1.7, 1.6 | 20 Nov 2026 | Active |
| Vanilla Furniture Expanded | 14 Oct 2026 | 1.7, 1.6, 1.5 | 6 Nov 2026 | Active |
| Dubs Rimatomics | 23 Oct 2026 | 1.0 to 1.7 | none seen | Updated recently |
| InterRim Ballistic Missile | 30 Jun 2026 | 1.6 | none seen | **Quiet, and not declared for 1.7** |
| EA-6B Prowler | 25 Nov 2026 | 1.7 | 15 Nov 2026 | Active; eight comments, none of a fault |
| RimPacts | 26 Nov 2026 | 1.7, 1.6 | 27 Nov 2026 | Active |

## The rows

### Worn and carried

| # | Tie | Reads → changes | Lets · Stops · Changes · Who else notices | Kind and cost | Risk and lean |
| --- | --- | --- | --- | --- | --- |
| RT1 | **Combat Extended's radio pack is a real manpack radio** | `CE_Apparel_RadioPack` (Backpack layer, 7 kg [D]) → gains `CompProperties_Radio`. Real figures to start from: the AN/PRC-77 gives 1.5 to 2.0 W on 30 to 75.95 MHz, FM, about 8 km "dependent on conditions" (Wikipedia, citing the maker's data sheet) | **Lets** a spotter stay in touch farther out than a belt hand-held, and gives a pack nearly every Combat Extended player owns a second use. **Stops** nothing. **Changes** who carries the pack. **Noticed by** the channel ledger, the "in touch" mood, and RT4 | XML in the radio's `Mods/CombatExtended` folder, plus a `BandDef` if it keeps its real band. One more worn radio in the net's pass | None. **Take first.** **Chosen by the owner (card 1): its real band**, military low VHF, so a station needs an antenna for that band and the pack cannot talk straight to our hand-helds |
| RT2 | **Helmets and headsets Combat Extended counts as radio packs are low-power sets** | The eleven flagged items [D] → a small radio each, half a watt to a watt, at head height [authored] | **Lets** a soldier stay in touch with the belt free. **Stops** nothing. **Changes** little. **Noticed by** the ledger: more radios share a station's channels | XML. A radio per wearer; a colony of armoured soldiers adds many links to each pass | None. **Take a short list** (the recon helmets, the mech commander helmets). The mechanitor headsets are bandwidth gear: ask before making them voice radios |
| RT3 | **Our hand-helds count as a radio pack for Combat Extended** | Our two hand-helds → the `isRadioPack` flag, by `MayRequire` | **Lets** a spotter mark with our radio on the belt. **Stops** nothing. **Changes**: the pack is no longer needed for marking. **Noticed by** Combat Extended's marking rule | XML. Nothing at run time | None. **Only together with RT4**; alone it only makes their pack pointless |
| RT4 | **Marking for artillery asks the link** | A postfix on `Verb_MarkForArtillery.MarkingConditionsMet` (nobody else patches it: `query-patches.py who`, 27 Nov) → where the pack was the reason it passed, the spotter's radio must be in touch: with a station on the map, or with the guns' tile through the world links | **Lets** a relay or a tall mast earn a fire mission. **Stops** calling fire from a dead spot behind rock, beyond reach, or (second record) under jamming. **Changes** where a spotter stands and where relays go. **Noticed by** the gun crews, and whoever the shells were meant for | Hook in the Combat Extended tie-in assembly. Combat Extended already asks only once every 250 ticks per spotter; ours is one lookup in the net's last pass | **Moderate**: a fire mission refused, seen beforehand on the link sheet. A setting, on. **Take, after RT1.** Combat Extended's same-map rule is left alone |
| RT5 | A radio held in the hands or kept in the inventory | The net would read `pawn.equipment` and `pawn.inventory` too | **Lets** a carried set count. **Stops** nothing. **Changes** little: real radios are worn | C# in the net: every colonist's hands and pack read on every pass | None. **Not now**: apparel covers it, and the cost is paid by every colony |

### Fixed stations and beacons

| # | Tie | Reads → changes | Lets · Stops · Changes · Who else notices | Kind and cost | Risk and lean |
| --- | --- | --- | --- | --- | --- |
| RT6 | **The comms console is a long-range station** | `Building_CommsConsole` (and Vanilla Gravship Expanded's `VGE_CommsTerminal`, the same class [D]) → a call to a faction needs reach to its nearest settlement, through our world links and relays; an orbital trader needs open sky | **Lets** the world radio matter to trade and diplomacy. **Stops** calling a faction across a mountain range with no relay. **Changes** relays from a convenience into diplomacy's road. **Noticed by** other factions, traders, RimPacts (it gates diplomacy on a working console) and The Dead Man's Switch Expedition (it already switches the console off under its own jammer) | Hook beside five other mods' patches on the console (`query-patches.py who "Building_CommsConsole.*"`). One world-reach lookup per call | **High**: an aid call refused in the middle of a raid. A setting, **off** by default. **Chosen by the owner (card 5): later, with HF** (the design's Phase 2): on VHF alone the horizon would cut off most of the world |
| RT7 | The comms console and the terminal as noise | Both → `RadioNoiseExtension` | See RT11 | XML | With RT11's first batch |
| RT8 | Beacons as homing beacons for aircraft | The orbital trade beacon, Royalty's ship landing beacon, the Prowler's runway beacon → an aircraft whose navigation is degraded can still find a powered beacon | **Lets** a damaged aircraft come home in bad weather. **Stops** nothing. **Changes** where a beacon is worth building. **Noticed by** the aircraft's crew | C# in the vehicle framework's flight rules; one lookup at landing | None. **An idea only**, for the aircraft's records |
| RT9 | A child's radio talk on our station | Biotech's "radio talking" looks only for a powered `CommsConsole` [C] → our base station would count too | **Lets** a colony with our station and no console keep the lesson. **Stops** nothing. **Noticed by** the children | Hook into `LearningGiver_Radiotalking` | None. **Look at it when School of the Air is next touched**: the two overlap |

### Noise and walls, by XML alone

| # | Tie | Reads → changes | Lets · Stops · Changes · Who else notices | Kind and cost | Risk and lean |
| --- | --- | --- | --- | --- | --- |
| RT10 | **Other mods' heavy walls block signal properly** | Their concrete, armour plate and shielding (The Dead Man's Switch's fortifications, Rimatomics' shielding) → `RadioObstructionExtension`; today they get the default 10 dB, scaled by how full the cell is | **Lets** a bunker be a real radio shadow. **Stops** nothing new. **Changes** antenna siting in fortified bases. **Noticed by** the coverage picture | XML. Nothing more than today: one figure read per wall crossed | **Low**. **Take a small batch**, each figure authored and labelled |
| RT11 | **Other machines are colony noise** | Televisions, sun lamps, Rimatomics' machinery, the comms console, the long-range and ground-penetrating scanners, other mods' radars and surveillance consoles → `RadioNoiseExtension`, each with a level and a radius | **Lets** the noise picture show a real colony. **Stops** nothing outright. **Changes** where a station goes: away from the hydroponics hall and the screen room. **Noticed by** every link that ends near one | XML. **This is the row to cost with care**: every noisy building is one more source, summed for each end of each link and for each cell the coverage picture paints. Forty sun lamps are forty sources | **Low**: weaker links, shown on the noise picture. **Take a first batch of few, strong sources** (televisions, the comms console, Rimatomics' reactor gear), measure with `scripts/perf.py`, and only then decide on sun lamps |

Two notes on RT11's realism. Grow-lamp ballasts and older television sets are well known among radio amateurs as noise sources
**[unverified: from the agent's memory, no source read]**. A radar transmits far above our two bands; the noise it would add down
here comes from its power supply and pulse circuits, so its figure is authored, not derived.

### Calls made by signal

| # | Tie | Reads → changes | Lets · Stops · Changes · Who else notices | Kind and cost | Risk and lean |
| --- | --- | --- | --- | --- | --- |
| RT12 | Royal permits and other calls for aid ask a link | Royalty's permit calls, The Dead Man's Switch's artillery and air support permits, Legion's air support in the comms dialogue → the caller must be in touch | **Lets** radio cover matter in a crisis. **Stops** a call from a dead spot. **Changes** a noble's or commander's place in a fight. **Noticed by** the empire and the allied faction | Hooks, one per permit worker | **High**: aid refused when it is needed most. Off by default. **Ask**, and only with RT6 |
| RT13 | Vanilla Outposts Expanded's artillery outpost fires by radio | `Outpost_Artillery` [d] → its fire on a map needs reach from that map to the outpost, as RT4 asks for Combat Extended | **Lets** the relay post we built serve a second purpose. **Stops** fire onto a map the outpost cannot hear. **Noticed by** the target | Hook; not read in code | Moderate. **Later, with RT4** |
| RT14 | Real Ruins' "radio-locate ruins" follows our reach | Its scanner comp on the comms console [D] → how far it finds ruins follows our station's world reach | **Lets** a better station find more. **Stops** nothing. **Noticed by** the world map | Hook; not read in code | None. **Low**, with RT6 if ever |

### Command links

| # | Tie | Reads → changes | Lets · Stops · Changes · Who else notices | Kind and cost | Risk and lean |
| --- | --- | --- | --- | --- | --- |
| RT15 | **Mech command through the colony's radio** | The game lets a mechanitor command within 24.9 cells [C] (`Pawn_MechanitorTracker.CanCommandTo`) → also where a station covers both the mechanitor's radio and the place | **Lets** a mechanitor command across the map through the colony's own stations. **Stops** nothing of the game's. **Changes** stations into mech infrastructure; under jamming, command falls back to 24.9 cells. **Noticed by** the mechs and whoever they are sent at | Hook, **beside four other mods' patches** on that method (Combat Extended, Alpha Genes, Apex Mechanoids, and a prefix of The Dead Man's Switch - Synthriada that may skip the original). The method is asked often while a mech is being ordered, so ours must be one lookup in a ready-made cover map, never a link worked out | None to the player. **Ask, and read the four patches first**: The Dead Man's Switch already widens command with its own relays, and ours must not fight them |
| RT16 | Remote charges fire by radio | Vanilla Factions Expanded - Deserters' remote charges, fired from a button on the charge [C, outline only] → the button needs a link from a station or a hand-held | **Lets** jamming protect or deny a minefield. **Stops** firing from out of reach. **Noticed by** raiders | Hook; its code is a 1.6 decompile only | Moderate. **Low: ask** |

### Broadcast and the game's own conditions

| # | Tie | Reads → changes | Lets · Stops · Changes · Who else notices | Kind and cost | Risk and lean |
| --- | --- | --- | --- | --- | --- |
| RT17 | Vanilla Furniture Expanded's radios hear the colony's net | `Radio_Industrial`, `Radio_Spacer` ("longwave radio"): music for recreation today [D] → a colonist resting beside one counts as hearing the net | **Lets** the sick and the off-duty stay in touch without a hand-held. **Stops** nothing. **Changes** little. **Noticed by** the "in touch" mood | C#: receive-only ends in the net; one more end per radio | None. **Low**: pleasant, with little play in it |
| RT18 | **Solar flares and the EMI field, decided** | Both switch off powered things (`GameCondition_DisableElectricity` [C]); our hand-helds have no power comp, so they very likely keep working through one **[inferred, not checked in game]** | **Lets** the hand-held be the one thing that works in a flare, or **stops** it too for consistency: a choice. **Noticed by** everyone, in the hour it matters | C#: one condition check per pass | Low. **Chosen by the owner (card 6): they keep working.** A real flare blacks out HF on the sunlit side and leaves VHF line of sight largely alone; the game's flare kills all electrics |

### Listed here, judged in the second record

| Things | Mod | Where |
| --- | --- | --- |
| Reinforcement uplink, fire support uplink, the alert network | The Dead Man's Switch | [EW18](electronic-warfare.md) |
| Mech drop beacon, mech ammo beacon | Royalty, Combat Extended | EW19 |
| Enemy and mechanoid signal jammers, the ancient signal jammer, the gravship's own signal jammer | Vanilla Gravship Expanded - Chapter 2, Odyssey | EW21 |
| Electronic jammer station, the comms-jammed rule, radars | The Dead Man's Switch - Expedition | EW21, and the radar sketch |
| EMI dynamo | Royalty | EW21 |
| Drones and their control | The Dead Man's Switch, Odyssey | EW22 |
| Decoy radios and antennas | The Dead Man's Switch | EW15 |
| TACS radar, anti-missile radars | Rimatomics, InterRim Ballistic Missile | The radar sketch |

### Argued against

| Thing | Why not |
| --- | --- |
| Psychic droner, suppressor, emanator; the psychic foil helmet | Not radio. Tying them to a link budget would be a second, false physics |
| Altered Carbon's casting relays | A fictional signal with its own range in world tiles; nothing of ours to add |
| Band nodes and bandwidth | Bandwidth is their capacity, as channels are ours. Two capacities do not need joining |
| Fortified Features Framework's signal attenuation on mechs | Their own link model, already in play. Leave it whole |
| Mech-cluster activators and count-down activators | Wired signals inside a cluster (`CompSendSignalOnMotion` [C]), not transmissions |
| The laser designator | Light, not radio; [air-defence tie-ins](air-defence-tie-ins.md) row AD4 already has it |
| Orbital targeters | Each is its own uplink to orbit, worn by the caller; a ground link adds nothing |
| Legion's fleet communicator and air-support pager | Their story is a forged signal to a fleet off the map; a link to our station would contradict it |
| Props and decor, the telescope, loudspeakers | No mechanic to join |
| InterRim Ballistic Missile's satellites as relays | A good idea in the design, but the mod is quiet and not declared for 1.7. Look again if it returns |

## The short list

In the order the agent would take them, were building open:

1. **RT1**, the radio pack as a manpack: XML, no risk, and it is the item the owner pointed at.
2. **RT11's first batch** and **RT10**: XML, with one measurement.
3. **RT4**, marking asks the link: the first row where another mod's rule is decided by our physics.
4. **RT2**, the short list of helmets.

**Answered by card**: RT1's band (its real one), RT6 (later, with HF), RT18 (hand-helds keep working). **Still to ask before
anything**: RT15 (mech command).

## Other factions

- The net reads the colony's free colonists only. A raider wearing a radio pack is in no net of ours and costs nothing.
- Whether raiders spawn with Combat Extended's radio pack was not checked.
- Other factions' radios are not modelled (`radio-world-coverage.md` says so). The second record's row EW17 says what a first,
  small model would be.

## Cost, in one place

| Kind of row | What it adds | When it runs |
| --- | --- | --- |
| A worn radio (RT1, RT2) | One end in the net's pass, as a hand-held today | About once a second, with the rest of the pass |
| A noise source (RT7, RT11) | One source in the snapshot; summed per link end and per painted cell | When the snapshot is retaken; the coverage picture only while it is shown |
| A wall figure (RT10) | Nothing new | Read as walls are today |
| A hook that asks the link (RT4, RT6, RT12, RT15) | One lookup in what the net last worked out | When the other mod asks; never a link worked out on the spot |

Nothing in this record needs a tick of its own. **Any row that is built is measured with `python scripts/perf.py` first** (asking the
owner before each recording), as the radio's rules require.

## Where a tie would live

- XML for another mod's things goes in the radio's own `Mods/<Mod>/Common/Patches`, loaded through `LoadFolders.xml` only when that
  mod is active; XML for the game's own things goes in `Common/Patches`, as the noise and wall patches do now.
- A hook goes in a tie-in assembly (`src/PhobosRadio.<Mod>`), never in the radio's core. The Combat Extended one exists already.
- Each new folder gets its row in [the tie-in register](tie-in-register.md) in the same commit.

## Questions for the owner

Put as cards on 27 Nov 2026 and answered that day. Cards 1, 5 and 6 were this record's: **the radio pack works its real band; the comms
console waits for HF; hand-helds keep working in a solar flare.** Every card, with every option as it was offered, is in
[the second record](electronic-warfare.md#questions-for-the-owner).

## Who chose what

- **Owner (27 Nov 2026):** the scope: third-party things, the game's own or other mods', tied to the radio and its simulation;
  research and record only.
- **Owner (27 Nov 2026, by card):** the radio pack on its real band (RT1); the comms console later, with HF (RT6); hand-helds keep
  working in a solar flare (RT18).
- **Agent:** every other row, lean, risk grade and figure here, for the owner to revise.

## Commands to run again

```text
# defName and label search across the owner's Defs (one GLOB per word)
python scripts/query-defs.py sql "SELECT def_type, def_name, label, class, mod_name, package_id FROM def_view WHERE wins AND loads AND coalesce(package_id,'') NOT LIKE 'phobos%' AND lower(coalesce(def_name,'')||' '||coalesce(label,'')) GLOB '*radio*' ORDER BY mod_position" --top 3000
python scripts/query-defs.py patched CE_Apparel_RadioPack --type ThingDef --lines 0
python scripts/query-code.py show CombatExtended.Verb_MarkForArtillery.MarkingConditionsMet --in CombatExtended-src
python scripts/query-code.py show MechanitorUtility.InMechanitorCommandRange --in full
python scripts/query-code.py show DMSE.Patch_CommsJammed --in DMSE-1.7-2026-11-23
python scripts/query-patches.py who "Verb_MarkForArtillery.MarkingConditionsMet"
python scripts/query-patches.py who "Pawn_MechanitorTracker.CanCommandTo"
python scripts/query-patches.py who "Building_CommsConsole.*"
python scripts/workshop-research.py report ceteam.combatextended aoba.deadmanswitch.core aoba.deadmanswitch.expedition aoba.framework kazepsi.irbm
```

Limits of the search: the Def index holds names, labels and classes, not descriptions, and has no column for comps; descriptions
and comp classes were read one Def at a time, or from the patched-Defs file directly. That file may be older than the newest
Workshop update, and it is written before inheritance.

## Sources

- The game's code and the mods' Defs and code, read with our tools on 27 Nov 2026 and summarised in our own words: Combat Extended
  (its public source), Fortified Features Framework, The Dead Man's Switch family, Vanilla Gravship Expanded - Chapter 2, Rimatomics
  and the EA-6B Prowler (decompiled for reading; nothing copied).
- Wikipedia, "AN/PRC-77 Portable Transceiver" (secondary; its figures cite the maker's data sheet, Associated Industries):
  en.wikipedia.org/wiki/AN/PRC-77_Portable_Transceiver. Read 27 Nov 2026.
- Steam Workshop pages and comments, through `scripts/workshop-research.py`, 27 Nov 2026.
- Our own records: [the radio's design](radio-framework-design.md), [the tie-in register](tie-in-register.md).
