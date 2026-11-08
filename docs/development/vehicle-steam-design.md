# Steam boilers (9 Nov 2026)

A design record, built the same day in D'thorga Framework 0.13.0 and D'thorga Vehicles 0.5.0 (Drafts). **Not yet tested in game.**
Part 3 of the owner's approved proposals of 9 Nov 2026, after [power and lift](vehicle-power-and-lift-design.md) and
[real figures](vehicle-real-figures-design.md). Figures come from
[the steam and diesel-electric research](steam-and-diesel-electric-research.md); labels **[P]** primary, **[S]** secondary,
**[I]** our inference, **[A]** authored.

## Read this first

- **Trains of the Rim's four steam locomotives** (the Frisco 4-8-4 and the Tommy 0-6-0T, each in a chemfuel and a wood version)
  get a boiler. Until now they had no systems at all.
- **The fire is lit while the locomotive is drafted with fuel aboard** (Vehicle Framework's "engine running"). The mod's own fuel
  and burn rates stand: the wood versions already need far more fuel, which is right, as air-dry logs carry only 15 to 20% of
  heating oil's energy by stacked volume **[I from P: UK Forest Research]**.
- **Raising steam takes hours.** A cold engine **refuses to move** until it has half its working pressure, and says why.
- **Low water melts the fusible plug**: a letter, the boiler vented, and no steam until a mechanic fits a new plug.
- **Water stops**: the tender or side tanks refill as a service, drawing only the shortfall, from Dubs Bad Hygiene's water towers
  when that mod is active, otherwise free (owner, 9 Nov 2026). **By default a stop draws a tenth of the real capacity** (owner, 9 Nov
  2026): 6,800 L for the Frisco, under one Dubs water tower's 8,000 (the huge tower holds 50,000, from the mod's own descriptions).
  **The realistic feed water setting** restores the real 68,000 L **[S]**.
- **Boiler explosions** (owner, 9 Nov 2026): **off by default**; a main-menu setting with a warning turns them on. Then a boiler
  without a fusible plug whose crown sheet is uncovered with the fire lit explodes.

## The model

Four quantities, all saved by system and name like the coolant:

| Quantity | What it is | How it moves |
| --- | --- | --- |
| Steam pressure | A share of the boiler's working pressure, 0 to 1.06 | The fire adds it while lit; the cylinders take it with the load; the boiler cools toward nothing, slowly |
| Water in the glass | 0 to 1; the crown sheet is uncovered at 0.1 **[A]** | The load evaporates it; the injector holds it near 0.8 from the feed water |
| Feed water | The tender or tanks, 0 to 1 | The injector draws it; a water stop refills it |
| Fusible plug | Intact or melted | Melts if the crown is uncovered with the fire lit; a mechanic replaces it |

- **Safety valves** cap pressure at 1.06, 6% over the working pressure, the most US rules allow **[P: 49 CFR 230.48 and 230.49]**.
  At the valves it is "blowing off": steam rises and water is lost faster.
- **The crown sheet**: covered by water, the firebox plate stays near the water's temperature; uncovered, it heats toward the
  fire's 1,500 to 2,500 F, and steel has lost about 85% of its strength by 1,600 F **[P: NTSB SIR-96/05]**. A fusible plug melts
  first (UK: pure lead, 327 degrees C **[P: HRA boiler code]**), sprays steam onto the fire and vents the boiler, and "the locomotive
  must be taken to a maintenance facility for repair" **[P: NTSB]**. US rules never required plugs **[P: 49 CFR 230.59]**. **The Frisco carries none** (we found no record either way for the 4500
  class **[A]**); the Tommy follows British practice and carries one. With the boiler explosions setting on, a plugless dry crown
  sheet tears: the water flashes to steam of "perhaps 1500 or 2000 times" its volume, and at only 60 psi each cubic foot of boiler
  water holds the energy of a pound of black powder **[P: NTSB, quoting Railway Mechanical Engineer and G. B. Airy]**. The game's
  explosion (5.9 cells, 60 blast damage **[A]**, far smaller than a real one) wrecks the boiler, with a letter. With the setting
  off, every boiler behaves as if plugged.
- **What saturated steam means for the player**: a boiler at 255 psi is only about 20 degrees C hotter than one at 160 psi (209
  against 188 degrees C, from NIST's IAPWS-95 tables **[P]**), but holds far more energy; the model shows pressure, not temperature.
- **Priming** (water carried into the cylinders from too high a level) is not modelled: the injector holds the glass below it.
- **Water treatment and scale** (hard water costing fuel, salts causing priming) are in the research record for a later part; not
  modelled.

## The locomotives

| Locomotive | Real class | Feed water | From cold to move / to full pressure | Feed water at hard work |
| --- | --- | --- | --- | --- |
| Frisco 4-8-4 | St. Louis-San Francisco 4500 class **[S]** | 68,000 L tender (18,000 US gal) **[S]**; 6,800 L drawn by default **[owner: a tenth]** | about 3 1/3 h / 8 h **[A, inside the 6 to 10 h quoted for big engines, S anecdotal]** | about 3 h **[I: 100 to 200 US gal a mile, S]** |
| Tommy 0-6-0T | LB&SCR E2, the class Thomas was drawn from **[S]** | 4,955 L side tanks (1,090 imp gal) **[S]**; 496 L drawn by default | about 1 1/3 h / 2 3/4 h **[A, inside the GCR's 3 to 7 h, P]** | about 1 2/3 h **[I: about 30 imp gal a mile, S]** |

A boiler held hot cools slowly (8% of the way to cold each hour **[A]**), so a locomotive run yesterday raises steam again in about
two hours, as heritage crews describe **[S]**. Rule tests hold the raising times and the feed water's endurance.

## For modders

- `PhobosVehicleSystem_Boiler` (role `Boiler`) with parameters `firingRate`, `steamUse`, `coolingRate`, `waterUse`, `injectorRate`,
  `feedShare` and `feedWaterLitres`, each overridable in a loadout's `set`; `PhobosVehicleSystem_FusiblePlug` (its own system, so it
  has its own service).
- A service may take its water from a system parameter (`waterLitresParameter`), use a scaled one unless the player's realistic
  setting is on (`scaledWaterLitresParameter`, `realisticWaterSetting`), and draw only the shortfall (`waterForShortfall`).
- `fusiblePlugFitted` (0 or 1) on the boiler; `Effect_Explosion`; `When_ParameterBelow`. The schema check reads them all.
- The loadout kind `Steam`.

## To test in game (owner)

1. Draft a cold Tommy: it refuses to move with the reason, and moves after about an hour and a third of game time.
2. Run it until the feed water is low: the message, then a water stop (with and without Dubs Bad Hygiene).
3. Let the feed water run dry while working: the low-water message, then the fusible plug letter; a mechanic fits a new plug.

## Who chose what

- **Owner (9 Nov 2026):** a boiler system for the steam locomotives (approved with parts 1 and 2); water from Dubs Bad Hygiene
  plumbing, else free.
- **Owner (9 Nov 2026, later):** boiler explosions allowed, off by default, behind a main-menu setting with a warning; the tender
  scaled down by default with a realistic setting.
- **Agent choices, owner to revise:** no plug on the Frisco and a plug on the Tommy; a tenth as the scale; the explosion's size; a
  damaged boiler steaming slower; the firing, cooling, water and injector
  rates; the glass's crown line; steel as the plug's supplies; pressure shown as a share rather than in psi.
