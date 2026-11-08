# Real figures for named vehicles (9 Nov 2026)

A design record, built the same day in D'thorga Vehicles 0.4.0 (Draft). **Not yet tested in game.** Part 2 of the owner's approved
proposals of 9 Nov 2026 ("real figures for the single-engine military vehicles, the L-ATV first"), after part 1,
[power and lift](vehicle-power-and-lift-design.md). Every figure comes from [the research record](vehicle-figures-research.md) and
is labelled in the Defs: **[P]** primary, **[S]** secondary, **[I]** our inference, **[A]** authored.

## Read this first

- **Named drivetrains replace the archetypes** for the vehicles below: their engines' real power (`ratedKilowatts`), idle and
  rated speeds, gear ratios where published, real curb or combat weights, tyre sizes and drive. Everything not published is
  authored and says so.
- **The final drive is worked out from the real top speed**: the rated rpm in top gear at that speed **[I]**. For the Humvee this
  gives 7.11, close to the hub and axle ratios people quote for it, though those were not verified.
- **Vehicle Framework still decides the speed**; these figures only change gear, rpm, load, heat and where power or grip runs out.
- **All the named drivetrains are in one file** (`mods/PhobosVehicles/Defs/Drivetrains_RealVehicles.xml`), loaded always because
  it names no other mod; each loadout loads only with its vehicle's mod. Both M1A2 mods share one AGT1500 Def.
- **The "no engine part" Defs were our scan's mistake**: the BMP-1 variants, the MaxxPro and the L-ATV have their engines in parent
  Defs (see [power and lift](vehicle-power-and-lift-design.md#which-vehicles-from-their-own-parts)). They are all fitted here.

## The vehicles

| Vehicle (mod) | Engine | Gearbox | Weight | Notes |
| --- | --- | --- | --- | --- |
| **L-ATV** (GTek's Vehicles) | 276 kW (370 hp) at 3,200 rpm **[owner's choice, see below]** | Allison 2500, six ratios **[P]** | 6,400 kg **[S]** | 740 A Niehoff alternator **[S]**; four-wheel drive **[P]** |
| MaxxPro (GTek's Vehicles) | 280 kW at 2,000 rpm **[P, excerpt]** | Allison 3000-series, five speeds **[S]**; ratios **[A]** | 15,422 kg **[S]** | 400 A alternator, 24 V **[S]** |
| Humvee (HUMVEE) | 119.4 kW at 3,400 rpm, idle 650 **[P: TM 9-2320-280-20-1]** | 4L80-E, 2.48 to 0.75 **[P]** | 2,500 kg **[S]** | Alternators 60 to 400 A at 28 V **[P]**; we keep 60 |
| Land Cruiser J70 (Frontline Workshop) | 1VD-FTV, 151 kW at 3,400 rpm **[S]** | H152F five-speed manual **[S]**; ratios **[A]** | 2,165 kg **[S]** | Part-time four-wheel drive: half the weight driven on the road **[A]** |
| "M1075 HEMTT" (Roaring Tiger) | C15, 447 kW **[P]**; rpm **[A]** | Allison 4500SP, six speeds **[P]**; ratios **[A]** | 24,040 kg **[P]** | **It is the PLS**, not a HEMTT; 260 A, 24 V **[P]** |
| M113 and M113(M) (Roaring Tiger) | 6V53, 158 kW at 2,800 rpm **[S, to verify]** | TX-100, three speeds **[S]** | 11,216 kg **[S]** | |
| M48H (Roaring Tiger) | AVDS-1790-2C, 560 kW **[S]**; rpm **[A]** | CD-850, two ranges **[S]** | 50 t **[S]** | Taiwan's CM-11 Brave Tiger |
| M10 Booker (Desert Sabre) | MTU 8V 199, 600 kW **[P]** | Allison 3040 MX, four **[P]** | 40 t **[S]** | At the top of its gearbox's rating |
| M2A3 Bradley (Desert Sabre) | VTA-903T, 447 kW at 2,600 rpm **[S]** | HMPT-500-3, three ranges **[S]** | 30 t **[S; 34.2 t elsewhere]** | |
| M1A2 and M1A2 SEP (both RimThunder mods) | AGT1500, 1,118 kW at 3,000 rpm **[S]** | X1100-3B, four **[P]** | 63,080 kg **[S]** | Turbine, as in 0.2.0 |
| T-54 (Frontline Workshop) | V-54, 388 kW at 2,000 rpm **[S]** | five-speed manual **[S]** | 36 t **[S]** | |
| BMP-1, -1P, -1PG, -1AM (BMP-1 IFV) | UTD-20, 220 kW at 2,600 rpm **[S]** | five-speed manual **[S]** | 13.2 t **[S]** | Swimming is not modelled |
| Wiesel (Frontline Workshop) | Audi turbo-diesel, 64 kW **[S]**; rpm **[A]** | not found; three **[A]** | 2.75 t **[S]** | |

**The L-ATV's power.** Oshkosh never published it. Secondary sources give 340, 400 or 425 hp. The Allison 2500's published rating is
254 kW (340 hp) with engine torque limiting **[P: Allison sheet SA5340EN]**, and the 2500 SP the L-ATV uses is "contact your Allison
representative". The first build used 254 kW, that published ceiling. **The owner chose the middle of 340 and 400 hp (9 Nov 2026):
370 hp, 276 kW**, which sits above the general 2500's published rating; the SP's own may be higher. In play: on a road at its in-game speed it cruises in fourth to sixth with power in hand; fully laden in
soft sand, four-wheel drive keeps its grip and the engine sets the pace (a rule test holds both).

## Simplifications

- Combat weights include crew and ammunition, which the model adds again (a few per cent; small).
- Fuel mass is left out: no density was sourced.
- Torque curves, efficiencies, shift points, drag areas and sprocket radii are authored.
- Transfer cases' low ranges are not modelled.

## To test in game (owner)

1. **The L-ATV** in Operator mode on a road and in sand, empty and laden: sensible gears, and it slows in deep sand without bogging.
2. The PLS laden on a hill: it labours but climbs.
3. A tracked vehicle (an M113 or a BMP-1) across soft ground.

## Who chose what

- **Owner (9 Nov 2026):** real figures for the single-engine military vehicles, the L-ATV first.
- **Owner (9 Nov 2026, later):** the L-ATV at 370 hp, the middle of the published figures.
- **Agent choices, owner to revise:** the final drives from top speed; every authored figure listed above.
