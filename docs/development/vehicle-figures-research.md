# Vehicle drivetrain and turbine figures: research record

Researched 8 November 2026 by a research agent for the drivetrain and turbine models (agent-authored, for the owner's review). Used so far for the helicopters' drive limits and the Harrier's engine label ([power and lift design](vehicle-power-and-lift-design.md)); the ground vehicles' figures feed part 2. **Nothing here is tested in game.**

How to read this file:

- **[P]** = primary source (manufacturer, US Army/DoD, Army technical manual, engine maker). **[S]** = secondary (Wikipedia, FAS mirrors of DoD pages count as [P] only where FAS reproduces an Army fact file; marked case by case).
- **"(snippet)"** = I saw the figure only in a search-engine excerpt of that page, because the page itself would not load. Treat these as weaker than a figure I read on the page.
- Where sources disagree, both are given. Nothing here is invented: a blank figure is "not found".
- Unit conversions in *italics* are mine, not the source's.

---

## Gotchas first (the surprises)

1. **JLTV engine power is not published officially.** Oshkosh's own pages give no horsepower. Secondary figures range 300 to 425 hp. The Allison 2500 general-purpose rating (Allison's own sheet) is **254 kW (340 hp) / 895 N·m** with engine torque limiting, and **224 kW (300 hp) / 780 N·m** without; the 2500 **SP** (the JLTV's) rating is "contact your Allison representative". So the oft-quoted 400 hp / 850 lb-ft (1,152 N·m) engine figure **exceeds the published 2500-series transmission rating**: either the SP rating is higher, or the engine is torque-limited by the transmission in some gears. Unresolved.
2. **UH-1H: the transmission limits the engine.** T53-L-13 is 1,400 shp but the airframe is **limited to 1,100 shp by the transmission** (Wikipedia from Jane's 1987-88).
3. **UH-1N: same story, harder.** Twin-Pac takeoff 1,800 shp is **flat-rated to 1,290 shp** (takeoff) and 1,600 shp max continuous **flat-rated to 1,135 shp**; the de-rating is described as the main transmission limit. One power section alone can give 900 shp for 30 min or 765 shp continuous.
4. **A129 Mangusta: gearbox capacity 970 kW (1,300 shp)** on the Gem-engined original, against two engines of 664 kW (890 shp) = 1,328 kW total: the gearbox is below the engines' sum (Flight International, snippet only).
5. **The "Pegasus 11-61" 64.3 kN / 95.9 kN figures belong to the 11-21 (F402-RR-406), not the 11-61.** Wikipedia attributes "14,450 lbf (64.3 kN) max continuous at 91% RPM" and "21,550 lbf (95.9 kN) 15 s wet at 107% RPM" to the **11-21**, sourced to NAVAIR's *AV-8B Standard Aircraft Characteristics* (October 1986). The 11-61 (F402-RR-408) is 23,800 lbf (106 kN) maximum. If the mod's Harrier is a GR7/GR9/AV-8B Plus with the -408, those lift figures are for the older engine.
6. **The M10 Booker's engine sits right at its transmission's limit.** MTU: 8V 199 = 600 kW (800 hp). Allison: 3040 MX accepts engines **up to 800 hp (596 kW)** and vehicles up to 40 tons. Booker weight reported 38 to 42 t, so it also sits at or over the 3040 MX's weight class.
7. **"M1075 HEMTT" is a mix-up.** The M1075 is the **PLS** (Palletized Load System) 10x10 truck. Oshkosh built both; the HEMTT is the M977-series 8x8. Figures below are for the PLS A1 M1075A1.
8. **AGT1500 idle fuel burn: no official gallons-per-hour figure found.** What is published: SFC at full power and at 1,200 shp (below), and percentage idle reductions from the DECU upgrade.

---

## 1. Oshkosh L-ATV / JLTV (the owner's favourite)

The Oshkosh L-ATV won the US JLTV competition (2015). The JLTV A2 (2023 onward) is built by AM General after the re-compete.

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine (A1, Oshkosh) | Banks 866T, 6.6 L V8 turbo-diesel, based on GM Duramax architecture | [S] | https://en.wikipedia.org/wiki/Joint_Light_Tactical_Vehicle |
| Engine (A1) | Banks lists the 866T as a "defense-specific 6.6-liter diesel base engine" built at GM's Moraine, Ohio Duramax plant; "866T V8 Turbo Diesel engine" | [P] | https://official.bankspower.com/project/custom-projects/banks-powered-oshkosh-jltv/ |
| Engine (A2, AM General) | "MY 2024 L5P Duramax engine"; "Duramax Turbo Diesel engine with Allison 2500SP 6-speed automatic transmission" | [P] | https://www.amgeneral.com/wp-content/uploads/2024/01/JLTV-A2.pdf |
| Rated power (A1), source 1 | 400 hp (298 kW), 850 lb-ft (1,152 N·m); no rpm | [S] | https://en.wikipedia.org/wiki/Joint_Light_Tactical_Vehicle |
| Rated power (A1), source 2 | 340 hp; "over 900 Nm" torque; no rpm | [S] | https://militaryvehiclesdata.com/specsheets/land-vehicles/wheeled-armored/4-wheel-armored-vehicles/oshkosh-jltv-a1-family/oshkosh-jltv-a1-general-purpose-specsheet/ |
| Banks 866T catalogue rating (generic engine, not JLTV-specific) | Rated power 425 hp; peak torque 725 lb-ft; **rated speed 3,200 rpm**; optional range 275 to 575 hp at 3,000 to 3,800 rpm; dry weight 912 lb without accessories (the same page's table also says 300 hp and 365 kg, conflicting) | [S] | https://army-guide.com/eng/product5833.html |
| Banks 866T range (Banks' own article) | 275 to 575 hp, 600 to 900 lb-ft | [P] | https://archive.bankspower.com/?p=4364 |
| Commercial L5P Duramax (for comparison) | up to 445 hp, 910 lb-ft | [S] | https://en.wikipedia.org/wiki/Joint_Light_Tactical_Vehicle |
| Transmission | Allison 2500 SP (Specialty Series), "since the first version (A1) began production in 2015"; six-speed fully automatic | [P] | https://allisontransmission.com/company/news-article/2023/03/28/allison-transmission-selected-to-equip-am-general-s-a2-version-of-the-joint-light-tactical-vehicle |
| Allison 2500-series gear ratios (sheet SA5340EN, 2013/06; covers 2500/2550; SP ratios not separately listed) | 1st 3.51, 2nd 1.90, 3rd 1.44, 4th 1.00, 5th 0.74, 6th 0.64, reverse -5.09 (mechanical, excluding converter) | [P] (Allison sheet, hosted by GM Authority) | https://gmauthority.com/blog/wp-content/uploads/2018/03/Allison-Transmission-International-2500-2550-specs.pdf |
| Allison 2500 torque converters (same sheet) | stall torque ratio TC-210 2.05, TC-211 1.91, TC-221 1.73, TC-222 1.58 (which one the JLTV uses: not found) | [P] | same |
| Allison 2500 ratings (same sheet) | General: 780 N·m / 224 kW (300 hp); with SEM engine torque limiting: 895 N·m / 254 kW (340 hp). **2500 SP: "Contact your Allison representative"** | [P] | same |
| Allison 2500 engine-speed window (same sheet) | full-load governed speed 2,200 to 3,800 rpm; **idle in Drive 500 to 820 rpm**; max output shaft speed 4,500 rpm at 105 km/h | [P] | same |
| Allison 2500 other (same sheet) | dry weight 150 kg; PTO turbine-driven, 1.00 x turbine speed, 339 N·m continuous (one PTO), 271 N·m total (two) | [P] | same |
| Transfer case | Two-speed (militaryvehiclesdata); Oshkosh-made per search snippets. Ratios: not found | [S] | militaryvehiclesdata URL above |
| Drive | 4x4, independent suspension front and rear | [P] | AM General A2 PDF above |
| Suspension | TAK-4i intelligent independent suspension; **20 in (508 mm) wheel travel**; ride height raised and lowered from inside the cab; disc brakes | [P] | https://oshkoshdefense.com/wp-content/uploads/2018/12/L-ATV_SinglePgs-A4size_web.pdf and https://oshkoshdefense.com/vehicles/jltv/ |
| Suspension type | Wikipedia says TAK-4i is "not coil-sprung"; militaryvehiclesdata says "coil springs ... and torsion bar". **Conflict.** | [S] | both URLs above |
| Payload | 5,100 lb (2,313 kg) 2-door; 3,500 lb (1,587.5 kg) 4-door | [P] | https://oshkoshdefense.com/vehicles/jltv/ |
| Curb weight | 14,000 lb (6,400 kg) (also the programme's requirement figure) | [S] | Wikipedia JLTV |
| Curb weight (GP variant) | ~6,080 kg; GVW 10,190 kg; payload ~4,110 kg | [S] | militaryvehiclesdata GP sheet |
| Gross weight | 22,500 lb (10,200 kg) | [S] | Wikipedia JLTV |
| Tyres | 385/80 R20 run-flat (militaryvehiclesdata). An EDR Magazine article says Michelin's X Force ZL **365/80 R20** "is the size used on the JLTV" (snippet only; page refused). **Conflict, unverified.** | [S] | militaryvehiclesdata GP sheet; https://www.edrmagazine.eu/michelin-x-force-zl-higher-load-and-speed-for-military-wheeled-vehicles (snippet) |
| Top speed | 70 mph (110 km/h) road; reverse 8 mph (13 km/h) | [S] | Wikipedia JLTV |
| Range | 300 mi (480 km) (Wikipedia); ~500 km (militaryvehiclesdata) | [S] | both |
| Fuel capacity | not found (a knowledge-graph page says 25 gal with no source: not used) | | |
| Electrical: battery | A2: "24 Volt lithium ion battery" | [P] | AM General A2 PDF |
| Electrical: alternator | A2: 740 A C.E. Niehoff alternator | [S] | Wikipedia JLTV (cites an Army re-compete article) |
| Electrical: alternator (USMC SBIR topic N222-088, 2022) | Existing JLTV alternator is the Niehoff **N1630**; a replacement system "must provide **20.7 kW (740 amp at 28VDC)**"; a second unit could go on the transmission PTO; driven from the "GM Duramax 6.6L Turbodiesel V-8" | [P] (snippet; navysbir.com returned 403) | https://navysbir.com/n22_2/N222-088.htm |
| Export power: ProPulse option | Oshkosh ProPulse diesel-electric hybrid option, "up to 120 kW of AC power for external operations" | [S] | Wikipedia JLTV (cites Oshkosh ProPulse product sheet) |
| Export power: eJLTV demonstrator | hybrid; "can export up to 115 kW" | [S] | Wikipedia JLTV |
| Integrated starter-generator on production JLTV | not found (none on the production A1/A2 as far as these sources show) | | |
| Anti-idle | Army "Tactical Vehicle Electrification Kit (TVEK) anti-idle" demonstrated; Allison cites "anti-idle capability" for the 2500 SP | [P] | https://ndia.dtic.mil/wp-content/uploads/2023/tactical/JPO_JLTV.pdf ; Allison press release above |
| Transportability | CH-47, CH-53, fixed-wing aircraft, amphibious ships | [P] | AM General A2 PDF |

**Not found (JLTV):** official engine power/torque and their rpm; transfer-case and axle ratios; fuel capacity; tyre size from a primary source; production ISG.

---

## 2. Navistar (International) MaxxPro MRAP

Closest to a "MaxxPro with an M2HB": the base MaxxPro and the **MaxxPro Dash (M1235)** both carry a roof-mounted crew-served weapon; the Dash is the shorter, lighter, more common later variant. Navistar's defence site no longer resolves (DNS failure), so manufacturer figures are snippet-only.

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine (base) | MaxxForce D9.3I6, 9.3 L inline-6; 330 hp at 2,100 rpm | [S] | https://en.wikipedia.org/wiki/International_MaxxPro |
| Engine (Dash) | 375 hp (280 kW) at 2,000 rpm; 450 hp (336 kW) optional; 1,250 lb-ft torque | [P] (snippet; site offline) | https://www.navistardefense.com/StaticFiles/navistardefense/vehicles/maxxpromrap/maxxpro_dash/MaxxPro.pdf |
| Engine (Dash) | 375 hp 9.3 L six; "over 1,694 Nm" torque (*1,250 lb-ft = 1,695 N·m, consistent*) | [S] | https://militaryvehiclesdata.com/specsheets/land-vehicles/wheeled-armored/navistar-maxxpro-family/navistar-maxxpro-dash-specsheet/ |
| Engine naming conflict | Wikipedia text says "MaxxForce 10" for Plus and Dash; Trucking Info says "9.3-liter MaxxForce 10 D9.3I6 ... 330 horsepower", 375 hp on some variants | [S] | Wikipedia; https://www.truckinginfo.com/articles/great-american-trucks-the-international-maxxpro-mrap |
| Transmission | Allison 3000 Series 5-speed automatic (Wikipedia, Trucking Info); Navistar utility-variants sheet: **Allison 3200SP 5-speed** (snippet) | [S] / [P] snippet | as above; https://www.navistardefense.com/StaticFiles/navistardefense/vehicles/maxxpromrap/maxxpro_recovery_vehicle/MaxxPro%20Utility%20Variants.pdf |
| Transfer case | two-speed, permanent 4WD | [S] | militaryvehiclesdata Dash |
| Drive | 4x4, semi-elliptical leaf springs | [S] | Trucking Info |
| Weight | Curb 21 to 24 short tons depending on configuration (Trucking Info); Dash curb 15,422 kg, GVW 22,226 kg, payload 4,535 kg (militaryvehiclesdata); Wikipedia mass list: base 21 t, Plus 24 t, Dash 22.2 t, Dash DXM 21 t | [S] | as above |
| Tyres (Dash) | 395/85 R20 or 16R20, run-flat, CTIS | [S] | militaryvehiclesdata Dash |
| Top speed (Dash) | 105 km/h | [S] | militaryvehiclesdata Dash |
| Range (Dash) | 400 km | [S] | militaryvehiclesdata Dash |
| Electrical | 400 A alternator, four 12 V batteries (*24 V system*) | [S] | Trucking Info |
| Other | wheelbase 153 in; ground clearance 14 in; fording 36 in | [S] | Trucking Info |
| Fuel capacity | not found (a 75 gal *secondary* tank is mentioned for the recovery variant only) | | |

---

## 3. HMMWV / Humvee

Army TM figures are primary (TM 9-2320-280-20-1, Table 1-2 Tabulated Data, Change 1, pages 1-31 to 1-33, M998 series).

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine 6.2 L | DDA 6.2 L, diesel, naturally aspirated, liquid-cooled V8; **150 hp at 3,600 rpm (111.9 kW)**; **idle 650 ± 25 rpm**; operating speed 1,500 to 2,300 rpm | [P] | https://hummer-hmmwv.tpub.com/TM-9-2320-280-20-1/css/TM-9-2320-280-20-1_56.htm |
| Engine 6.5 L | DDA 6.5 L, diesel, naturally aspirated V8; **160 hp at 3,400 rpm (119.4 kW)**; idle 650 ± 25 rpm; operating 1,500 to 2,300 rpm | [P] | same |
| 6.5 L torque | 290 lb-ft (393 N·m); PBS gives 290 lb-ft at 1,700 rpm (snippet) | [S] | https://en.wikipedia.org/wiki/Humvee |
| 6.5 L turbo | 190 to 205 hp, 385 to 440 lb-ft; GEP Optimizer 6500: 205 hp, 440 lb-ft | [S] | Wikipedia Humvee |
| Transmission (early) | GM 3L80 (TH400), 3-speed automatic | [P] | https://hummer-hmmwv.tpub.com/TM-9-2320-280-20-1/css/TM-9-2320-280-20-1_57.htm |
| Transmission (later) | GM 4L80-E, 4-speed automatic with lock-up converter; **ratios 1st 2.48, 2nd 1.45, 3rd 1.00, 4th 0.75, reverse 2.08** | [P] | same |
| Transfer case | NPG 218 or NPG 242 (with cooler), 2-speed; permanently 4WD; three ranges plus neutral | [P] | pages _57 and _60 |
| Final drive | geared hubs at each wheel act as the final drive. Hub ratio: not found in what I read (1.92 often quoted: unverified) | [P] | https://hummer-hmmwv.tpub.com/TM-9-2320-280-20-1/css/TM-9-2320-280-20-1_60.htm |
| Electrical | **alternators 60 A, 100 A, 200 A or 400 A at 28 V**; two 12 V batteries; starter 14 V (as printed) | [P] | page _56 |
| Fuel tank | **25 gal (94.6 L)** | [P] | https://hummer-hmmwv.tpub.com/TM-9-2320-280-20-1/css/TM-9-2320-280-20-1_55.htm |
| Payload | M998/M1038 2,500 lb (1,135 kg); M1097, M1123, A1 and A2 series 4,400 lb (1,998 kg) | [P] | page _55 |
| Curb / gross weight | 5,200 to 5,900 lb curb; 7,700 to 8,500 lb gross (all variants) | [S] | Wikipedia Humvee |
| Tyres | 37 x 12.5 radial with run-flat inserts | [S] | Wikipedia Humvee |
| Top speed | over 70 mph; 55 mph at max gross | [S] | Wikipedia Humvee |
| Range | 250 to 350 mi | [S] | Wikipedia Humvee |
| M1151A1 | crew 4; 2,000 lb payload; 4,000 lb towing; 50 mph road; 300 mi | [S] | Wikipedia Humvee |

---

## 4a. Oshkosh PLS A1 M1075A1 (the "M1075"; see gotcha 7)

All from the Oshkosh product sheet VEH_PLSA1_M1075_SEL_UEN_4/15-1 (2015).

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | Caterpillar C15 ACERT, 600 hp, 15.2 L (rpm not given) | [P] | https://oshkoshdefense.com/wp-content/uploads/2018/12/17392_PLS-M1075A1_LowRes_4.29.2015.pdf |
| Transmission | Allison 4500SP, 6-speed automatic (Wikipedia's infobox says 5-speed: Oshkosh wins) | [P] | same |
| Transfer case | Oshkosh 36000 Series, 2-speed | [P] | same |
| Axles | front Oshkosh TAK-4 with AxleTech carrier; rear AxleTech; diff locks and planetary wheel-ends | [P] | same |
| Drive | 10x10; steering on axles 1, 2 and 5 | [P] | same |
| Suspension | front TAK-4 independent; rear Hendrickson RT400 | [P] | same |
| Curb weight | 53,000 lb (24,040 kg) including flatrack | [P] | same |
| GVWR / GCWR | 86,000 lb (39,009 kg) / 135,500 lb (61,462 kg) | [P] | same |
| Payload | 16.5 ton (14,969 kg) flatrack | [P] | same |
| Tyres | 16.00 R20 XZLT Michelin, 10 + spare; CTIS 4 presets | [P] | same |
| Top speed | 62 mph (100 km/h) | [P] | same |
| Fuel / range | 186 gal (704 L); 300 mi (483 km) | [P] | same |
| Electrical | 24 V / 260 A | [P] | same |
| Gradeability | 60 % alone at GVW; 30 % with trailer | [P] | same |
| Original M1075 (for comparison) | Detroit Diesel 8V92TA 500 hp; Allison CLT-755 ATEC 5F/1R; curb 54,750 lb | [S] (FAS mirror of DoD page) | https://man.fas.org/dod-101/sys/land/pls.htm |

## 4b. M113 APC and variants

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engines | M113: Chrysler 75M petrol V8, 209 hp; M113A1/A2: Detroit Diesel 6V53, 212 to 215 hp; M113A3: 6V53T turbo, 275 hp | [S] | https://en.wikipedia.org/wiki/M113_armored_personnel_carrier ; https://man.fas.org/dod-101/sys/land/m113.htm |
| Power/torque with rpm (table mapping uncertain) | A1: 215 hp at 4,000 rpm (?), 332 lb-ft at 2,800 rpm; A2: 212 hp at 2,800 rpm, 492 lb-ft at 1,300 rpm; A3: 275 hp at 2,800 rpm, 627 lb-ft at 1,600 rpm | [S] | Wikipedia M113 (fetch tool could not read column labels: verify before use) |
| Transmission | M113 to A2: Allison TX-100 3-speed automatic (with controlled-differential steering); A3: **Allison X200-4/4A** hydrostatic-steer cross-drive | [S] | Wikipedia; FAS M113 |
| Allison X200 rating | engines up to 400 hp (298 kW); 4 forward, 2 reverse; hydrostatic steering; vehicles up to 20 tons (another line on the page says 22) | [P] | https://allisontransmission.com/applications--products/products/x-models |
| Weights (FAS) | combat: M113 23,520 lb; A1 24,594; A2 24,728; A3 27,000 | [S] | FAS M113 |
| Top speed | 37 mph (M113 to A2), 41 mph (A3) land; 3.6 mph (5.8 km/h) swimming | [S] | FAS M113 |
| Range / fuel | 300 mi (483 km); 95 gal (360 L) | [S] | FAS M113 |
| Running gear | 5 road wheels per side, torsion bar | [S] | Wikipedia |

## 4c. M2 Bradley (M2A3 the likely target)

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | Cummins VTA-903T, V8 turbo diesel; M2/M2A1 500 hp at 2,600 rpm; M2A2/M2A3 600 hp at 2,600 rpm | [S] | https://afvdatabase.com/usa/m2bradley.html |
| M2A4 | 675 hp at 2,800 rpm (column mapping uncertain) | [S] | https://en.wikipedia.org/wiki/M2_Bradley |
| Transmission | M2/M2A1: HMPT-500 hydromechanical; M2A2/M2A3: HMPT-500-3 or HMPT-500-3EC, automatic range selection (GE originally; later L-3 Combat Propulsion Systems per Army Technology) | [S] | afvdatabase |
| Combat weight | M2 22,797 kg; M2A2 27,135 kg (with armour kit); M2A3 34,200 kg (afvdatabase) vs 66,000 lb / 29,940 kg (Wikipedia). **Conflict.** | [S] | both |
| Top speed | M2 66 km/h; M2A2 56 km/h; M2A3 61 km/h (afvdatabase); FAS says 45 mph for the family | [S] | afvdatabase; https://man.fas.org/dod-101/sys/land/m2.htm |
| Fuel | M2/A1 197 gal (746 L); A2 175 gal (662 L); A3 155 gal (587 L) | [S] | afvdatabase |
| Range | ~300 mi (A0 to A2); ~250 mi (A3) | [S] | afvdatabase |
| Running gear | 6 road wheels per side; track 21 in (53 cm) | [S] | afvdatabase |

## 4d. M48H "Brave Tiger" (Taiwan's CM11)

The US designated it M48H ("H" for hybrid): M48A3-type turret on an M60A3-type hull with M1-derived fire control.

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | Continental AVDS-1790-2C, air-cooled twin-turbo diesel V12, 750 hp (560 kW); rpm not given | [S] | https://en.wikipedia.org/wiki/CM-11_Brave_Tiger |
| Transmission | Allison CD-850-6A (some sources) or CD-850-6A1 (GlobalSecurity, snippet: "inherits the power system of M60A3") | [S] | snippets via search; GlobalSecurity page returned 403 |
| Mass | 50 t (Wikipedia); 52.0 t (GlobalMilitary, snippet) | [S] | Wikipedia CM-11 |
| Top speed | 48 km/h road; 25 km/h cross-country (snippet) | [S] | Wikipedia CM-11 |
| Range | 480 km (Wikipedia); 463 km (GlobalMilitary, snippet) | [S] | |
| Suspension | torsion bar | [S] | Wikipedia CM-11 |
| Fuel, gears, ratios | not found | | |

## 4e. M10 Booker (programme cancelled 2025)

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | MTU 8V 199 PowerPack, **600 kW (800 hp)**; MTU adds "The 8V 199 TE23 will deliver 800kW" (a future rating). Wikipedia says the Booker's is the 8V199 TE23 at 600 kW, so the press release and Wikipedia disagree on which designation gives which power | [P] | https://www.mtu-solutions.com/na/en/pressreleases/2023/rolls-royce-delivers-first-mtu-series-199-powerpacks-for-m10-booker-combat-vehicle-program.html |
| Transmission | Allison 3040 MX cross-drive | [S] | https://en.wikipedia.org/wiki/M10_Booker |
| 3040 MX rating | engines **up to 800 hp (596 kW)**; vehicles up to 40 tons; **4 forward, 2 reverse**; mechanical ratio "4:8:1" (as printed); developed from the X300 | [P] | https://allisontransmission.com/applications--products/products/x-models |
| 3040 MX dry weight | 975 kg; vehicle class 36 t / 40 ton (2024 flyer, snippet) | [P] snippet | https://allisontransmission.bynder.com/m/754a182771e7c0c9/original/3040-MX-Flyer-2024.pdf |
| Weight | roughly 38 to 42 t | [S] | Wikipedia M10 |
| Top speed | 40 mph (64 km/h) (Wikipedia cites army.mil) | [S] | Wikipedia M10 |
| Range | 250 to 350 mi | [S] | Wikipedia M10 |

## 4f. T-54 and T-55

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | V-54 (T-54) / V-55 (T-55), V12 water-cooled diesel, 38.88 L | [S] | https://en.wikipedia.org/wiki/T-54/T-55 |
| Power | T-54 520 hp; T-55 580 hp (FAS text: "580 hp rather than 520 hp"). Several pages give **2,000 rpm** for both (snippet only). Late upgrades up to 800 hp | [S] | https://man.fas.org/dod-101/sys/land/row/t54tank.htm ; Wikipedia |
| Transmission | mechanical (synchromesh), 5 forward, 1 reverse | [S] | Wikipedia |
| Mass | 36 t (Wikipedia, T-55); combat 40.5 t (FAS table, T-55) | [S] | both |
| Top speed | 51 km/h road (Wikipedia); 50 km/h (FAS) | [S] | |
| Fuel | 580 L internal + 320 L external + 400 L jettisonable drums; T-55 internal raised to 680 L | [S] | Wikipedia |
| Range | T-54 400 km (600 with auxiliary); T-55 500 km (715) (FAS text) | [S] | FAS |
| Running gear | 5 road wheels per side, torsion bar, no return rollers | [S] | Wikipedia |
| Power-to-weight | 14.6 hp/t | [S] | Wikipedia |

## 4g. BMP-1

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | UTD-20 V6 diesel, 300 hp (220 kW) at 2,600 rpm | [S] | https://en.wikipedia.org/wiki/BMP-1 |
| Transmission | manual, 5 forward, 1 reverse | [S] | Wikipedia (cites Jane's Armour and Artillery 2005-06) |
| Mass | 13.2 t | [S] | Wikipedia |
| Speed | 65 km/h road; 7 to 8 km/h water | [S] | Wikipedia |
| Range | 600 km road, 500 km off-road | [S] | Wikipedia |
| Fuel | 462 L | [S] | Wikipedia |
| Running gear | 6 road wheels per side, torsion bar | [S] | Wikipedia |

## 4h. Wiesel 1 (AWC)

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | Audi 5-cylinder in-line turbo-diesel, 64 kW (86 hp); displacement 2.0 L (infobox) or 2.1 L (text): conflict; rpm not given | [S] | https://en.wikipedia.org/wiki/Wiesel_AWC |
| Mass | 2.75 t (Wiesel 1 base) | [S] | Wikipedia |
| Top speed | 70 km/h | [S] | Wikipedia |
| Range | 200 km | [S] | Wikipedia |
| Transmission (Wiesel 1) | not found (Wikipedia names a ZF automatic only for the Wiesel 2) | | |
| Fuel | not found | | |

## 4i. Toyota Land Cruiser J70 (military and utility diesels)

Engines span 1984 to now: 2L/2L-T/3L, 3B/13B-T, 1PZ, 2H, **1HZ** (4.2 L I6, the long-running utility/UN engine), 1HD-FTE, **1VD-FTV** (4.5 L V8 turbo, 2007 on), 1GD-FTV (2.8 L, 2023 on).

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine list by code and years | (see above) | [S] | https://en.wikipedia.org/wiki/Toyota_Land_Cruiser_(J70) |
| 1VD-FTV (2017 LC70 GX cab-chassis, Australia) | 4,461 cc; **151 kW at 3,400 rpm; 430 N·m from 1,200 rpm**; 5-speed manual H152F; low/high range; kerb 2,165 kg; GVM 3,400 kg; 265/70 R16; **130 L tank** | [S] | https://www.carexpert.com.au/toyota/landcruiser-70-series/2017/gx/features-and-specs |
| 1HZ | 4,164 cc, 6 cylinders; **96 kW at 3,800 rpm; 285 N·m at 2,200 rpm** (Toyota Gibraltar Stockholdings export listings: snippet only, pages redirected to their homepage); NRCan mining approval 129 hp (96 kW) at 3,800 rpm (snippet); a Japanese listing gives 99 kW at 4,000 rpm (conflict) | [P] snippet | https://toyota-gib.com/eng/models/heavy-duty-utility-type/land-cruiser-70/ (snippet) |
| 1HZ max engine speed | 4,400 rpm (Wikipedia HZ engine, snippet) | [S] snippet | https://en.wikipedia.org/wiki/Toyota_HZ_engine |
| Fuel tank (other years) | 2012 double-cab single 130 L; a 2012 GXL listing gives 180 L | [S] | Wikipedia J70; GoAuto (snippet) |
| Gear ratios, final drive | not found | | |

---

## 5. M1 Abrams: AGT1500 turbine and drivetrain

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Power | 1,500 shp (1,118 kW) **at 3,000 rpm output speed** | [S] (Chalmers archive of AlliedSignal data, 1997) | https://www.tfd.chalmers.se/~thgr/gasturbiner/Material_for_generating_slides/Lecture2/AlliedSignal%20AGT%201500%20-%20Archived%203-1997.htm |
| Torque | 2,750 lb-ft (3,754 N·m) at 3,000 rpm | [S] | same |
| Power turbine speed | 22,500 rpm reduced to 3,000 rpm output | [S] | same |
| SFC | standard: 0.495 lb/shp/hr (0.30 kg/kWh) at full power; 0.475 (0.289) at 1,200 shp. With DECU: 0.45 (0.274) full; 0.43 (0.261) at 1,200 shp | [S] | same |
| Idle fuel | DECU gives "11-percent to 26-percent lower fuel consumption at idle" (Chalmers); Honeywell markets "20% lower idle fuel consumption". **No absolute idle figure found.** | [S] | same; Honeywell (snippet) |
| Pressure ratio | 14.0 to 14.5:1 | [S] | same |
| Turbine inlet temperature | approximately 2,180 °F (1,193 °C) | [S] | same |
| Recuperator | Solar Turbines, stacked hydroformed IN625 plates; air in at over 850 °F; combustion air 1,050 °F at 206 psi | [S] | same |
| Weight / size | 2,500 lb (1,134 kg) dry; 1,685 x 990 x 846 mm | [S] | same |
| Weight (Honeywell) | "under 2,500 pounds", "less than 48 cubic feet" | [P] | https://www.honeywellaerospace.com/us/en/insights/articles/15-surprising-facts-about-honeywell-1500-horsepower-turbine-tank-engine |
| Accessory gearbox | extracts 35 to 100 hp from the HP spool | [S] | https://en.wikipedia.org/wiki/Honeywell_AGT1500 |
| Transmission | Allison X1100-3B, cross-drive, **4 forward, 2 reverse**; accepts engines up to 1,500 hp; vehicles up to 75 tons; in production since 1979 for the M1 | [P] | https://allisontransmission.com/applications--products/products/x-models |
| Fuel capacity | 504.4 US gal (1,909 L) | [S] | https://en.wikipedia.org/wiki/M1_Abrams |
| Weights | M1 60 short tons; M1A1 63; M1A2 69.54 (FAS spec table) or 68.7 (FAS variant table); SEPv3 73.6 short tons (Wikipedia) | [S] | https://man.fas.org/dod-101/sys/land/m1.htm ; Wikipedia |
| Top speed (governed) | M1 45 mph; M1A1/M1A2 42 mph (FAS); Wikipedia gives M1A1 45 mph governed | [S] | both |
| APU (M1A2 under-armour APU) | **6 kW electrical at 214 A, 28 V DC**; 10 kW hydraulic; 510 lb; burns 3 to 5 gal per operating hour | [S] (FAS) | https://man.fas.org/dod-101/sys/land/m1.htm |
| Earlier APU concept | a 75 hp Combat Support Module APU (AGT1500 TME work) | [S] | Chalmers archive |

---

## 6. Helicopters

### Bell UH-1H Iroquois

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | 1 x Lycoming T53-L-13 turboshaft, 1,400 shp | [S] | https://en.wikipedia.org/wiki/Bell_UH-1_Iroquois (data from Jane's All the World's Aircraft 1987-88, p. 365) |
| **Transmission limit** | **"limited to 1,100 shp (820 kW) by transmission"**. A DTIC document (snippet only) says the T53-L-13 is "rated at 1400 shp ... derated to 1100 shp for installation in UH-1 series"; a UH-1B test plan says "transmission is limited to 1100 SHP at 324 rotor RPM" | [S]; [P] snippet | Wikipedia; https://apps.dtic.mil/sti/pdfs/AD0839031.pdf (403) |
| Gearing | main transmission input 90° bevel, 2.14:1, then planetary stages | [S] | Wikipedia |
| Empty / max take-off | 5,210 lb (2,363 kg) / 9,500 lb (4,309 kg); mission gross 9,039 lb | [S] | Wikipedia |
| Rotor | 2-blade semi-rigid, 48 ft (14.63 m) | [S] | Wikipedia |
| Speed | 110 kn max (also Vne at MTOW) | [S] | Wikipedia |
| Torque gauge | a StandardAero upgrade brochure mentions a 1,290 hp / 56.5 psi torque upgrade (snippet) | [P] snippet | https://standardaero.com/wp-content/uploads/2022/10/StandardAero-Bell-UH-1H-Upgrade-Capabilities.pdf |

### Bell UH-1N Twin Huey (for comparison)

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engine | P&WC PT6T-3 Twin-Pac (military T400-CP-400): two power sections into one combining gearbox | [S] | https://en.wikipedia.org/wiki/Bell_UH-1N_Twin_Huey |
| Take-off | **1,800 shp at 33,000 rpm power turbine, flat-rated to 1,290 shp** | [S] | https://en.wikipedia.org/wiki/Pratt_%26_Whitney_Canada_PT6T (Wilkinson, Aircraft Engines of the World 1970, p. 166) |
| Max continuous | **1,600 shp, flat-rated to 1,135 shp** | [S] | same |
| Single power section (OEI) | 900 shp (671 kW) for up to 30 min; 765 shp (571 kW) continuous | [S] | Wikipedia UH-1N |
| Why flat-rated | "de-rated to the main transmission limit of 1,290 shaft horsepower" (snippet) | [S] snippet | search results |
| Max take-off | 10,500 lb (4,763 kg) | [S] | Wikipedia UH-1N (USMC fact sheet) |
| Rotor | 2-blade, 48 ft (14.63 m) | [S] | Wikipedia UH-1N |
| Dry weight (PT6T) | 617 lb | [S] | Wikipedia PT6T |

### Boeing CH-47D/F Chinook

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engines (D) | 2 x T55-L-712, 3,750 shp each (FAS; Wikipedia T55 citing Leyes & Fleming 1999). Other pages give 4,500 shp (snippet): conflict, probably different rating levels | [S] | https://man.fas.org/dod-101/sys/ac/h-47.htm ; https://en.wikipedia.org/wiki/Honeywell_T55 |
| Engines (later D / F) | 2 x T55-GA-714A: 4,733 shp each (Wikipedia CH-47F specs) or 4,777 shp (Wikipedia T55, citing Army Weapon Systems 1999) or 4,868 shp (Wikipedia CH-47F text) | [S] | Wikipedia |
| -714A rating levels | contingency 5,069 shp; maximum 4,867; intermediate 4,527; max continuous 4,168 (conditions not visible); at 4,000 ft / 95 °F: contingency 4,000, maximum 3,750 (Honeywell brochure, snippet only; the copy I downloaded no longer has this table) | [P] snippet | search results for Honeywell T55 brochure |
| T55 output shaft | 15,066 rpm; -714A "5,000 shp class", -714C "6,000 shp class" | [P] | https://www.honeywellaerospace.com/content/dam/aerobt/en/documents/landing-pages/brochures/Brochure_Honeywell_T55_Turboshaft_Engine.pdf |
| **Transmission limits (combining, rotor transmissions)** | **not found.** A CH-47A/B Army document says "transmission torque limit is reached before engine power limit" (snippet only). Unofficial study cards: 100 % dual-engine, 123 % single-engine torque (not used) | | |
| Drive train layout | engine transmissions -> combining transmission -> drive shafting -> forward and aft rotor (reduction) transmissions | [P] snippet | TM 55-1520-240-23-1 via tpub (snippet) |
| Weights (F) | empty 24,578 lb (11,148 kg); gross 54,000 lb (24,494 kg) | [S] | https://en.wikipedia.org/wiki/Boeing_CH-47_Chinook |
| Weights (FAS, variant unclear) | empty 23,401 lb; max gross 50,000 lb | [S] | FAS |
| Rotors | tandem, 3 blades each, 2 x 60 ft (18 m); 225 rpm (FAS) | [S] | Wikipedia; FAS |
| Speed | 170 kn | [S] | Wikipedia |

### Mil Mi-24 Hind

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engines | 2 x Isotov TV3-117, 1,600 kW (2,200 shp) each | [S] | https://en.wikipedia.org/wiki/Mil_Mi-24 |
| TV3-117VMA Series 2 | take-off 1,640 kW (2,200 shp); SFC 0.308 kg/kWh; TIT 920 to 990 °C; OPR 9.4; dry 295 kg | [S] | https://en.wikipedia.org/wiki/Klimov_TV3-117 (Brassey's 1999/2000) |
| Emergency / OEI rating | not found | | |
| Main gearbox (VR-24) limit | not found | | |
| Weights | empty 8,500 kg; MTOW 12,000 kg | [S] | Wikipedia |
| Rotor | 5-blade main, 17.2 to 17.3 m; 3-blade tail | [S] | Wikipedia |
| Speed | 320 km/h | [S] | Wikipedia |

### Agusta A129 Mangusta

| Figure | Value | Src | Source URL |
|---|---|---|---|
| Engines | 2 x Rolls-Royce Gem 2-1004D (Piaggio licence), 664 kW (890 shp) each | [S] | https://en.wikipedia.org/wiki/Agusta_A129_Mangusta |
| Other ratings | 615 to 664 kW (825 to 890 shp); emergency 772 kW (1,035 shp) each (snippets, unnamed secondary pages) | [S] snippet | |
| **Gearbox capacity** | **970 kW (1,300 shp)** original, raised to 1,340 kW with the LHTEC T800 version (Flight International, snippet); GlobalSecurity: new stronger transmission "1,700shp" (snippet) | [S] snippet | https://www.flightglobal.com/italian-a129s-assessed-for-upgrade/50255.article |
| Gem 42 (related engine) | take-off 1,000 shp; max contingency 1,120 shp | [S] | https://en.wikipedia.org/wiki/Rolls-Royce_Gem |
| Weights | empty 2,530 kg; MTOW 4,600 kg (original 4,100 kg; International 5,000 kg) | [S] | Wikipedia |
| Rotor | 5-blade, 11.9 m | [S] | Wikipedia |
| Speed | 278 km/h | [S] | Wikipedia |

### Harrier GR / AV-8B: Pegasus

| Figure | Value | Src | Source URL |
|---|---|---|---|
| 11-61 / F402-RR-408 (Mk 107) max thrust | 23,800 lbf (106 kN) (Wikipedia, citing Rolls-Royce 2004 and AW&ST 2009); GlobalSecurity 23,400 lb (snippet) | [S] | https://en.wikipedia.org/wiki/Rolls-Royce_Pegasus |
| 11-61 other | OPR 16.3; SFC 0.76 lb/lbf-hr; dry 3,960 lb (1,796 kg); 3-stage LP, 8-stage HP; 2+2 turbine stages | [S] | same |
| 11-61 vs earlier (Rolls-Royce) | "up to 15 per cent more thrust at high ambient temperatures"; "twice the hot-end life of earlier versions" | [P] | https://www.rolls-royce.com/products-and-services/defence/aerospace/combat-jets/pegasus.aspx |
| **11-21 / F402-RR-406** lift ratings | **14,450 lbf (64.3 kN) max continuous at 91 % RPM to 21,550 lbf (95.9 kN) 15 s wet at 107 % RPM** (NAVAIR AV-8B SAC, Oct 1986). See gotcha 5 | [S] | same |
| Water injection | about 35 imp gal/min, about 90 s maximum; tank up to 500 lb (50 imp gal) | [S] | same |
| Fan airflow | 432 lb/s | [S] | same |
| **Turbine (JPT) temperature limits** | **not found** in any primary or encyclopaedic source. Only forum claims (PPRuNe: short-lift dry JPT limit "715", later GR7A/GR9A engines RPM-limited only): unverified, not used | | |

---

## Not found (all vehicles)

- JLTV: official engine power/torque and rpm; transfer-case and axle ratios; fuel capacity; primary tyre size; production ISG.
- MaxxPro: fuel capacity; anything primary readable (Navistar defence site offline).
- HMMWV: geared-hub ratio (1.92 commonly quoted, unverified); 3L80 ratios.
- PLS: engine rpm and torque.
- M113: rpm-to-variant mapping needs a TM; transmission ratios.
- Bradley: HMPT-500 ranges and limits.
- M48H: transmission ratios; fuel capacity.
- Booker: transmission ratios; fuel.
- T-54/T-55: rpm from a page I could read in full.
- Wiesel 1: transmission; fuel; rpm.
- Land Cruiser J70: gear and final-drive ratios; 1HZ from a page I could read in full.
- AGT1500: absolute idle fuel flow.
- Chinook: combining and rotor transmission limits (shp or torque).
- Mi-24: OEI rating, VR-24 gearbox limit.
- A129: Gem 2-1004D rating set from a primary source; gearbox figure from a page I read in full.
- Pegasus 11-61: JPT limits; normal-lift / short-lift ratings for the -408.

## Sources read (pages that loaded)

- https://oshkoshdefense.com/vehicles/jltv/
- https://oshkoshdefense.com/wp-content/uploads/2018/12/L-ATV_SinglePgs-A4size_web.pdf
- https://en.wikipedia.org/wiki/Joint_Light_Tactical_Vehicle
- https://militaryvehiclesdata.com/specsheets/land-vehicles/wheeled-armored/4-wheel-armored-vehicles/oshkosh-jltv-a1-family/oshkosh-jltv-a1-general-purpose-specsheet/
- https://official.bankspower.com/project/custom-projects/banks-powered-oshkosh-jltv/
- https://archive.bankspower.com/?p=4364
- https://army-guide.com/eng/product5833.html
- https://allisontransmission.com/company/news-article/2023/03/28/allison-transmission-selected-to-equip-am-general-s-a2-version-of-the-joint-light-tactical-vehicle
- https://gmauthority.com/blog/wp-content/uploads/2018/03/Allison-Transmission-International-2500-2550-specs.pdf (Allison sheet SA5340EN)
- https://www.amgeneral.com/wp-content/uploads/2024/01/JLTV-A2.pdf
- https://ndia.dtic.mil/wp-content/uploads/2023/tactical/JPO_JLTV.pdf
- https://en.wikipedia.org/wiki/International_MaxxPro
- https://www.truckinginfo.com/articles/great-american-trucks-the-international-maxxpro-mrap
- https://militaryvehiclesdata.com/specsheets/land-vehicles/wheeled-armored/navistar-maxxpro-family/navistar-maxxpro-dash-specsheet/
- https://warwheels.net/MaxxProDash_DXMmrapIndex.html
- https://hummer-hmmwv.tpub.com/TM-9-2320-280-20-1/css/TM-9-2320-280-20-1_55.htm (and _56, _57, _60)
- https://en.wikipedia.org/wiki/Humvee
- https://en.wikipedia.org/wiki/Palletized_load_system
- https://man.fas.org/dod-101/sys/land/pls.htm
- https://oshkoshdefense.com/wp-content/uploads/2018/12/17392_PLS-M1075A1_LowRes_4.29.2015.pdf
- https://oshkoshdefense.com/wp-content/uploads/2019/02/HeavyTacVehicles_Bro_3-23-2011.pdf
- https://en.wikipedia.org/wiki/M113_armored_personnel_carrier
- https://man.fas.org/dod-101/sys/land/m113.htm
- https://en.wikipedia.org/wiki/M2_Bradley
- https://man.fas.org/dod-101/sys/land/m2.htm
- https://afvdatabase.com/usa/m2bradley.html
- https://en.wikipedia.org/wiki/M10_Booker
- https://www.mtu-solutions.com/na/en/pressreleases/2023/rolls-royce-delivers-first-mtu-series-199-powerpacks-for-m10-booker-combat-vehicle-program.html
- https://allisontransmission.com/applications--products/products/x-models
- https://man.fas.org/dod-101/sys/land/m1.htm
- https://en.wikipedia.org/wiki/Honeywell_AGT1500
- https://www.tfd.chalmers.se/~thgr/gasturbiner/Material_for_generating_slides/Lecture2/AlliedSignal%20AGT%201500%20-%20Archived%203-1997.htm
- https://www.honeywellaerospace.com/us/en/insights/articles/15-surprising-facts-about-honeywell-1500-horsepower-turbine-tank-engine
- https://en.wikipedia.org/wiki/M1_Abrams
- https://en.wikipedia.org/wiki/CM-11_Brave_Tiger
- https://en.wikipedia.org/wiki/T-54/T-55
- https://man.fas.org/dod-101/sys/land/row/t54tank.htm
- https://en.wikipedia.org/wiki/BMP-1
- https://en.wikipedia.org/wiki/Wiesel_AWC
- https://en.wikipedia.org/wiki/Toyota_Land_Cruiser_(J70)
- https://www.carexpert.com.au/toyota/landcruiser-70-series/2017/gx/features-and-specs
- https://en.wikipedia.org/wiki/Bell_UH-1_Iroquois
- https://en.wikipedia.org/wiki/Bell_UH-1N_Twin_Huey
- https://en.wikipedia.org/wiki/Pratt_%26_Whitney_Canada_PT6T
- https://en.wikipedia.org/wiki/Boeing_CH-47_Chinook
- https://man.fas.org/dod-101/sys/ac/h-47.htm
- https://en.wikipedia.org/wiki/Honeywell_T55
- https://www.honeywellaerospace.com/content/dam/aerobt/en/documents/landing-pages/brochures/N61-1492-000-001_T55_Turboshaft-bro.pdf
- https://www.honeywellaerospace.com/content/dam/aerobt/en/documents/landing-pages/brochures/Brochure_Honeywell_T55_Turboshaft_Engine.pdf
- https://en.wikipedia.org/wiki/Mil_Mi-24
- https://en.wikipedia.org/wiki/Klimov_TV3-117
- https://en.wikipedia.org/wiki/Agusta_A129_Mangusta
- https://en.wikipedia.org/wiki/Rolls-Royce_Gem
- https://en.wikipedia.org/wiki/Rolls-Royce_Pegasus
- https://www.rolls-royce.com/products-and-services/defence/aerospace/combat-jets/pegasus.aspx

**Seen only as search-engine snippets (pages refused or offline):** Navistar MaxxPro Dash brochure and utility-variants sheet (navistardefense.com offline); navysbir.com N222-088 (403); apps.dtic.mil AD0839031 (403); globalsecurity.org CM11, HMMWV and F402 pages (403); edrmagazine.eu Michelin X Force ZL (403); Toyota Gibraltar LC70 pages (redirect to homepage); Allison 3040 MX 2024 flyer; Flight International A129 upgrade article; Honeywell T55 rating table; StandardAero UH-1H brochure.

**Tooling note:** one attempt to fetch more TM 9-2320-280-20-1 pages (_58, _59, likely holding tyre size, geared-hub ratio and weights) was blocked by the session's permission classifier; those pages remain unread.
