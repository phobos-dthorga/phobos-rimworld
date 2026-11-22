# Aircraft munitions in line with the Mk. 82 (23 Nov 2026)

**Status (23 Nov 2026):** built in D'thorga Patches 0.37.0: real weights, recipes and the two shared parts (the Mk. 82 bomb case for
three bombs, the new missile seeker head). Not yet tested in game: row 380 of the [test pass](in-game-test-pass.md).

## Read this first

- **Under Combat Extended only.** Without CE these jets and helicopters load straight from steel, and nothing here applies.
- **Bombs and missiles already made keep their old cost**; only new ones are made the new way.
- **The owner's question (23 Nov 2026):** the new aircraft's weapons were "far too cheap"; bring them in line with the Mk. 82 and Mk. 84,
  in value and in what they are made of. FSX, then components, limit the owner's colony; for other players it is more often steel or
  components.
- **The owner's cards (23 Nov 2026):** **real weights**, as the Mk. 82 and Mk. 84 have; **intermediate parts** (a bomb case for the
  FAB-250 and RBK-250, a seeker head for the Hellfire and HARM); **the jamming pod raised as suggested** (80 steel, 20 gold, 10
  components, 2 advanced components).
- **One change from the table the owner agreed** (agent's choice, for the owner to revise): the CBU-87 and RBK-250 take their fill by
  the Mk. 82's rule, per kg of real explosive, so less FSX and more steel than the table, for much the same value. Said in the reply.

## The rule they follow

The Mk. 82 and Mk. 84 (since D'thorga Patches 0.6.0, `Mods/HarrierVtol/Patches/Bombs_HarrierVtol.xml`) set the pattern:

- **The weight is the real one.**
- **The case is a part of its own**, made from steel at the machining table, at its real weight (0.5 kg of steel a unit).
- **The fill is about 0.23 FSX and 3.5 chemfuel per kg of real explosive.** Matching the full weight in FSX took far too long to make,
  so chemfuel carries most of it (owner's choice, 2 Nov 2026).
- **No fixed market value**: the game works it out from the ingredients (`StatWorker_MarketValue`).

Missiles add a motor: chemfuel stands for its propellant, and their FSX is authored from the owner's table, not the bomb ratio.

## What each costs now

Values are what the game derives from the ingredients and work, rounded; the "before" column is the authors' figure under CE.

| Munition (mod) | Weight, before → now | Recipe now | Value, before → now |
| --- | --- | --- | --- |
| Mk. 82 (Harrier, A-10) | 227 kg | Mk. 82 case, 20 FSX, 300 chemfuel, 2 components (unchanged) | about 1,440 |
| FAB-250 (Su-25) | 250 kg | Mk. 82 case, 20 steel, 23 FSX, 350 chemfuel, 2 components | 280 → about 1,630 |
| RBK-250 cluster (Su-25) | 40 → 273 kg | Mk. 82 case, 8 FSX, 115 chemfuel, 6 components | 190 → about 1,060 |
| CBU-87 cluster (A-10) | 60 → 430 kg | 600 steel, 12 FSX, 185 chemfuel, 10 components | 380 → about 2,000 |
| 9M114 Shturm (Hind) | 46 → 46.5 kg | 40 steel, 8 FSX, 40 chemfuel, 8 components | 310 → about 520 |
| AGM-114 Hellfire (Mongoose) | 47 → 45 kg | seeker head, 40 steel, 10 FSX, 50 chemfuel, 6 components | 410 → about 790 |
| AGM-88 HARM (Prowler) | 80 → 360 kg | seeker head, 140 steel, 15 FSX, 150 chemfuel, 10 components, 1 advanced component | 400 → about 1,580 |
| AN/ALQ-99 pod (Prowler) | 40 → 431 kg | 80 steel, 20 gold, 10 components, 2 advanced components | 340 → about 1,100 |

The two new or shared parts:

| Part | Made from | Where | Used by |
| --- | --- | --- | --- |
| Mk. 82 bomb case (`PhobosBombMk82Case`, existing) | 280 steel, 140 kg | Machining table; Dead Man's Switch - Expedition's rotary forger | Mk. 82, FAB-250, RBK-250 |
| Missile seeker head (`PhobosMissileSeekerHead`, new) | 10 steel, 2 components, 1 advanced component; 5 kg | Machining table or fabrication bench; Microelectronics | Hellfire, HARM |

**Where the figures come from:**

- **FAB-250:** 250 kg with 100 kg of fill (secondary: Wikipedia, "FAB-250"; ISW gives 99 kg). Russian Wikipedia's variant table gives
  the M-62 227 kg with 100 kg and the M-54 268 kg with 97 kg (secondary). Body 150 kg (250 less the fill), so the Mk. 82's 140 kg case
  and 20 more steel. Its explosive type was not found.
- **RBK-250:** 273 kg with 150 AO-1 bomblets (secondary: Russian Wikipedia, "РБК-250", flagged there as unsourced); the RBK-250-275 is
  275 kg (secondary, militaryperiscope.com, from a search summary only). The Su-25's usual PTAB-2.5M load (42 bomblets) has no total
  weight or fill found. **The fill is authored**: the CBU-87's share of explosive by weight (12 %) gives 33 kg, so 8 FSX and 115
  chemfuel.
- **CBU-87:** 430 kg (950 lb) with 202 BLU-97/B bomblets of 1.5 kg (secondary: Wikipedia). Net explosive weight **52.7 kg** (116.1 lb;
  primary: US Department of Defense, *Iraq Ordnance Identification Guide*, 2004, dispensers chapter, hosted by James Madison
  University's CISR: jmu.edu/cisr/research/iraq-oig/02-dispenser.pdf). Wikipedia's 287 g a bomblet would give 58 kg; the DoD figure is
  used. The empty SUU-65/B dispenser's weight was not found; the metal comes to about 377 kg (430 less the explosive), and 600 steel
  (300 kg) stands for it, short on purpose as the Mk. 82's fill is.
- **9M114 Shturm:** 31.4 kg as a missile (secondary: Wikipedia, "9K114 Shturm"), 46.5 kg in its launch tube (secondary: Russian
  Wikipedia); warhead 5.3 kg. Radio command guidance (SACLOS), steered from the helicopter, so no seeker head.
- **AGM-114K Hellfire II:** 45 kg (100 lb), 9 kg tandem shaped charge, semi-active laser seeker (secondary: Wikipedia, "AGM-114
  Hellfire"; designation-systems.net gives 45.7 kg). The US Navy's fact file refused automated reads.
- **AGM-88 HARM:** 360 kg (800 lb), warhead 68 kg (150 lb), radar homing (primary: US Navy fact file, archived 2004,
  webharvest.gov/peth04/20041016160604/http:/www.chinfo.navy.mil/navpalib/factfile/missiles/wep-harm.html). Its 15 FSX follows the bomb
  ratio for the warhead.
- **AN/ALQ-99 pod:** 431 kg (950 lb; secondary: Carlo Kopp, "Tactical jamming", *Australian Aviation*, January 1989,
  ausairpower.net/TE-Tacjammer.html). A wargame database gives 435 kg for the ALQ-99D (weak).
- Gathered by an agent on 23 Nov 2026 from the pages above; the US Air Force's and NAVAIR's pages refused automated reads.

**Authored** (gameplay choices, for the owner to revise): every component, advanced component, gold and chemfuel count of the
missiles and the pod; the seeker head's whole recipe, weight and work; the RBK-250's fill; the CBU-87's steel; one case shared by the
Mk. 82, FAB-250 and RBK-250, and one seeker head shared by two very different seekers (the Hellfire's homes on laser light, the HARM's
on radar emissions).

## How it is built

- **Shared parts:** `mods/PhobosPatches/Mods/AircraftMunitions/Patches/Munitions_Shared.xml`, a folder loaded while any of the
  Harrier, Hind, Mongoose, Close Air Support or Prowler is active. The Mk. 82 case and the Mk. 82's recipe moved here from the
  Harrier's folder, unchanged, with their picture: the A-10 loads the same Mk. 82, so with Close Air Support alone the Mk. 82 had
  kept its author's 60 steel, 45 chemfuel and two components. Their guard is the Mk. 82's 227 kg, which both mods' CE patches set.
  The seeker head exists while the Hellfire or the HARM does (both only under CE).
- **Each mod's munitions** in its own folder: `Munitions_CloseAirSupport.xml` (inside the same 227 kg guard),
  `Munitions_EA6BProwler.xml`, and two new folders for the helicopters, `Mods/HindHelicopter` and `Mods/MongooseHelicopter`, each
  waiting on its item.
- **Weights** replace the authors' figures; bulk, work, skill and research stay theirs.
- **Weight in play:** a colonist carries one at a time, as the Mk. 82 and Mk. 84 (227 and 907 kg) already are. Stored on a vehicle or
  in a caravan they now weigh what they really do. The pods are used up into the Prowler's count when mounted (its `EWPods.cs`),
  so their weight never rides on the jet.

**Risk: low** (agent's grade, for the owner to confirm): costs and weights only. A bill made before keeps working; things already made
keep their old cost. Removing the mod leaves seeker heads and bomb cases without a Def: use them up or sell them first.

## Next game

Nothing here is RimWorld-free: it is XML for these mods' items. A next game would need only the rule above (real weight, a case
part, fill by kg of real explosive, value from ingredients), written for its own items.

## Picture

The seeker head's picture is paid art (an item lying in the world), two tries made on 23 Nov 2026
(`assets/vehicle-patches/requests/seeker-head.json`), offered on D'thorga Companion's Picks page for the owner.
