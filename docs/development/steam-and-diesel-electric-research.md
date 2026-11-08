# Research: steam boilers, diesel-electric shunters, thrust-lifted engine-out

Compiled 8 November 2026 by a research agent (agent-authored, for the owner's review) for Trains of the Rim's boilers, the Goat DE2 and the drop pods. Used so far for the Goat and the traction motors ([power and lift design](vehicle-power-and-lift-design.md)); the boiler figures feed part 3. **Nothing here is tested in game.**

**How to read the labels**

- **[P] primary**: the organisation that made the rule, ran the test, built the machine or wrote the report (regulator, investigator, manufacturer, standards body, original handbook).
- **[S] secondary**: an encyclopedia, a heritage society's summary, a magazine or a forum. Forum posts are flagged **(forum, anecdotal)**.
- **Derived** means our own arithmetic from the cited figures. It is not a source finding.
- Every row was read in the source named. Where the page could not be opened, the item is in the "not found" list, not in a table.
- Units: locomotive boiler pressures are **gauge** pressures (psi above atmosphere), as is standard in locomotive data. Steam tables use **absolute** pressure. Add 0.101 MPa (14.7 psi) to convert. That conversion is ours.

---

## Gotchas for the game model (read first)

1. **The crown sheet is the failure point.** While water covers it, the firebox plate sits near the water's temperature (about 400 F, roughly 200 C). Uncovered, it heats towards the fire's 1,500 to 2,500 F, and steel loses about 85% of its strength by 1,600 F. Then it tears and the boiler's contents flash to steam (NTSB, quoting Railway Mechanical Engineer, 1943) [P].
2. **Fusible plugs are a warning, not a fix.** UK practice fills them with pure lead (melts at 327 C). US alloy rings melt at 500 to 575 F (260 to 302 C). A melted plug sprays steam onto the fire and **disables the locomotive until a shop repairs it**. Fitting them was never compulsory in the US, and some railways chose not to.
3. **Safety valves stop overpressure, but they don't stop low water.** US rules say at least one valve opens at or below the maximum allowable working pressure (MAWP), all open by MAWP plus 6 psi, and pressure may never rise more than 6% above MAWP with the fire bright and every outlet shut.
4. **Too much water is also a fault.** Priming carries water into the cylinders. Water won't compress, so it can break a cylinder head or piston, and it washes away the lubrication. Dissolved salts make it worse: the LMS saw softened-water engines start priming after 400 to 500 miles.
5. **Diesel-electric overheating is a time budget, not a cliff.** The US Army's manual for a 500 hp switcher allows full current indefinitely, then 20, 10 or 5 minutes in rising overload bands. After that it needs 15 to 30 minutes idling to cool, or a return to the continuous band.
6. **Engine-out in hover usually needs either a lot of spare thrust or a mechanical cross-link.** Even the V-22, whose cross-shaft lets one engine turn both rotors, generally cannot hover on one engine. The Yak-38 simply ejected the pilot automatically if a lift engine failed.

---

## Part A: steam locomotive boilers

### A1. St. Louis–San Francisco (Frisco) 4500 class 4-8-4

The mod's "Frisco 4-8-4" matches this class: 25 built by Baldwin, 1942–43. **Nos. 4500–4502 were built as oil burners** for the Meteor passenger train. Nos. 4503–4524 burned coal. No source found says any of them was later converted.

| Fact | Value | Label | URL |
|---|---|---|---|
| Number built, builder, dates | 25 (Nos. 4500–4524), Baldwin, 1942–1943 | [S] | https://en.wikipedia.org/wiki/St._Louis%E2%80%93San_Francisco_class_4500 |
| Boiler pressure | 255 psi (1.76 MPa), gauge | [S] | same; also https://railarchive.net/randomsteam/slsf4503.htm |
| Boiler pressure, conflict | The 4500 article's own Specifications section says 250 psi; its infobox says 255 | [S] | https://en.wikipedia.org/wiki/St._Louis%E2%80%93San_Francisco_4500 |
| Grate area | 88 sq ft (8.2 m²) | [S] | class article |
| Firebox heating surface | 524.5 sq ft (48.7 m²) | [S] | class article |
| Evaporative heating surface | 4,766 sq ft | [S] | railarchive.net slsf4503 |
| Superheater surface | 1,508 sq ft (140.1 m²) | [S] | class article; railarchive.net |
| Tender water | 18,000 US gal (68,000 L) | [S] | class article |
| Tender fuel, oil burners 4500–4502 | 6,500 US gal (25,000 L) of fuel oil | [S] | class article |
| Tender fuel, coal burners 4503–4524 | 22 t (24 short tons) of coal | [S] | class article |
| Tractive effort | 71,200 lbf (317 kN); 80,800 lbf (359 kN) with the booster | [S] | class article |
| Tractive effort, conflict | railarchive gives 69,800 lbf, "raised to 71,200 with booster" | [S] | railarchive.net slsf4503 |
| Driver diameter, cylinders | 74 in; two cylinders, 28 × 31 in | [S] | class article |
| Locomotive weight | 464,850 lb (4500–4502, oil); 474,040 to 479,300 lb (coal batches) | [S] | class article |
| Tender weight | 346,200 lb (4500–4502); 341,300 lb (others) | [S] | class article |
| Adhesive weight, axle load | 280,340 lb; 70,620 lb per axle | [S] | 4500 article |
| Top speed | 90 mph (145 km/h) | [S] | class and 4500 articles |
| Power | 5,600 hp | [S] | class article (no source cited for it) |
| Retirement | Class "dismissed from service" October 1956; 4500, 4501, 4516 and 4524 preserved | [S] | search summary of the Wikipedia articles; the date was not seen on a page that was read |

**For comparison: Union Pacific 844, an oil-fired 4-8-4 still in steam**

| Fact | Value | Label | URL |
|---|---|---|---|
| Water / fuel capacity | 23,500 US gal water; 6,200 US gal of No. 5 oil | [P] | https://www.up.com/heritage/steam/844/index.htm |
| Boiler pressure, tractive effort | 300 lbs; 63,800 lbs | [P] | same |
| Water consumption | 100–200 US gal of water per mile | [S] | https://en.wikipedia.org/wiki/Union_Pacific_844 (infobox; the UP page does not give it) |
| Oil consumption | 15–25 US gal of fuel oil per mile | [S] | same |
| Coal-to-oil conversion | The FEF class was "converted to run on fuel oil in 1946" | [S] | same |

### A2. A typical 0-6-0 tank engine ("Tommy")

**Thomas's own prototype**: in 1946 the illustrator Reginald Payne based Thomas on the later LB&SCR E2 class, the series with extended side tanks (Wikipedia, "Popular culture") [S].

| Fact | LB&SCR E2 (UK, the Thomas basis) | LMS Fowler 3F "Jinty" (UK) | USATC S100 (US-built, coal **or oil**) |
|---|---|---|---|
| Built | 10, 1913–1916 | 422, 1924–1931 | 382, 1942–1944 |
| Boiler pressure | 170 psi | 160 psi | 210 psi |
| Grate area | 17.35 sq ft (1.61 m²) | 16 sq ft (1.5 m²) | 19.4 sq ft (first 50); 18.3 sq ft (rest) |
| Heating surface | firebox 97, tubes 983, total 1,080 sq ft | firebox 97, tubes 967 sq ft | firebox 86, tubes 790, total 876 sq ft |
| Superheater | not stated | none | none |
| Water | 1,090 or 1,256 imp gal | 1,200 imp gal | 1,200 US gal (1,000 imp gal) |
| Fuel | 2.46 long tons coal | 2.25 long tons coal | 2,500 lb coal **or 300 US gal oil** |
| Tractive effort | 21,305 lbf | 20,835 lbf | 21,630 lbf |
| Weight | 52 tons 15 cwt to 53 tons 10 cwt | 49.5 long tons | 100,650 lb (45.7 t) |
| Driving wheels | 4 ft 6 in | 4 ft 7 in | 4 ft 6 in |
| Top speed | not stated | "up to 60 mph" (article body) | not stated |
| Label, URL | [S] https://en.wikipedia.org/wiki/LB%26SCR_E2_class | [S] https://en.wikipedia.org/wiki/LMS_Fowler_Class_3F | [S] https://en.wikipedia.org/wiki/USATC_S100_Class |

**Derived, for the game**: a small tank engine carries roughly 1,000 to 1,250 imperial gallons (4.5 to 5.7 t) of water and about 2.3 to 2.5 t of coal. That is about one-thirteenth of the Frisco's tender water. The S100 gives a real precedent for an oil-fired 0-6-0T: **300 US gallons of oil replaces 2,500 lb of coal**.

### A3. How a boiler behaves

#### Time to raise steam

| Fact | Value | Label | URL |
|---|---|---|---|
| Heritage railway, typical engine | "approximately 3 to 7 hours to get a locomotive in steam"; pressure must rise slowly to avoid damaging the boiler | [P] (operator's own page) | https://www.gcrailway.co.uk/about/engine-shed/engine-ready/ |
| Union Pacific heritage fleet | Boilers are filled and left hot at the end of each operating day ("we fill the boiler completely, and let it warm"), not started cold | [S] (search summary quoting UP's steam manager; that page was not opened) | https://www.up.com/news/heritage/steam-update-10-12-2016 |
| Hot boiler held overnight | "only takes a couple of hours of firing in the morning" | [S] (photo caption) | https://railpictures.net/photo/699083 |
| Fast cold start on a big engine | UP once reached operating pressure on a cold 4000-class engine in about 40 minutes, but called it a test that would be destructive if done routinely | [S] (mailing list, anecdotal; search summary only) | https://archives.nauer.org/BRHSLIST/2002-10/msg00006.html |
| Large engines from cold | 8–10 hours for a big engine to about 150 psi; 12 hours for a cautious start after a long lay-up | [S] (forum, anecdotal; seen only in search summaries, pages blocked) | ogrforum.com "you-re-fired"; cs.trains.com thread 120824 |

**Model suggestion (agent's inference, label as authored)**: small engine about 3 hours from cold; large engine 6 to 10 hours; a banked or held-hot boiler about 1 to 2 hours. Forcing the fire faster should cost boiler wear, not be impossible.

#### Water consumption at work

| Fact | Value | Label | URL |
|---|---|---|---|
| LMS worked example | Feed water used "at the rate of 30 gallons per mile" (imperial), for an engine with a 1,000-gallon boiler | [S] (LMS Society monograph by L. G. Warburton, drawing on LMS and ILocoE papers) | https://www.lmssociety.org.uk/monographs/M13.pdf |
| Heavy goods train, UK | One ton of coal lasts about 40 miles, but needs 6 to 9 tons of water | [S] | https://en.wikipedia.org/wiki/Track_pan |
| Holyhead run, 1860 | 84¾ miles used 1,800–1,900 gal of water, 2,400 gal in rough weather; "The largest tenders only held 2,000 gallons" | [S] | same |
| Big oil-fired 4-8-4 (UP 844) | 100–200 US gal per mile | [S] | https://en.wikipedia.org/wiki/Union_Pacific_844 |

**Derived**: 6 to 9 t per 40 miles is 150 to 225 kg per mile, about 33 to 50 imperial gallons. An 18,000 US gallon Frisco tender at 100 to 200 US gallons per mile would run dry in 90 to 180 miles. Planned water stops would come well before that.

#### Safety valves (US federal rule, 49 CFR Part 230)

| Fact | Value | Label | URL |
|---|---|---|---|
| Number and capacity | At least two safety relief valves, able to prevent "an accumulation of pressure greater than 6 percent above the MAWP" under any conditions of service | [P] | https://www.law.cornell.edu/cfr/text/49/230.48 |
| Accumulation test | Tested "with the fire in good, bright condition and all steam outlets closed"; more valve capacity is required if pressure goes over 6% | [P] | same |
| Set pressures | At least one valve opens at or below MAWP; all open at no more than MAWP + 6 psi | [P] | https://www.law.cornell.edu/cfr/text/49/230.49 |
| Steam test after repair | Pressure raised to 95–100% of MAWP | [P] | https://www.law.cornell.edu/cfr/text/49/230.37 |
| History | The first US locomotive boiler explosion (Best Friend of Charleston, 17 June 1831) happened because a man annoyed by the hissing valve sat on it | [P] (NTSB report's history section) | https://www.ntsb.gov/safety/safety-studies/Documents/SIR9605.pdf |

**Derived for the Frisco**: MAWP 255 psi means the valves start lifting at or below 255 psi, are all open by 261 psi, and pressure must never pass 270 psi (255 × 1.06 = 270.3).

#### Water level, the crown sheet and fusible plugs

| Fact | Value | Label | URL |
|---|---|---|---|
| Minimum glass reading | "The lowest reading of the water glasses shall not be less than 3 inches above the highest part of the crown sheet" | [P] | https://www.law.cornell.edu/cfr/text/49/230.51 |
| Daily check | Water glasses blown out and gauge cocks tested at the start of each day the locomotive is used | [P] | https://www.law.cornell.edu/cfr/text/49/230.54 |
| Two independent level indicators | Every boiler needs at least one water glass and three gauge cocks; two independent feeds (injector and feed pump) | [P] (NTSB, citing the CFR of the time) | NTSB SIR-96/05 (URL above) |
| Firebox plate temperature | Fire gases "1500 F to 2500 F"; plates covered by water stay near the water's temperature, "in the neighborhood of 400 F" | [P] (NTSB, quoting Railway Mechanical Engineer, Oct 1943) | NTSB SIR-96/05 |
| Strength of steel | Firebox steel gets slightly stronger up to about 500 F, then weakens rapidly; at 1,600 F it "has lost about 85 percent of its strength" | [P] (same quotation) | NTSB SIR-96/05 |
| What a failure does | The water flashes to steam occupying "perhaps 1500 or 2000 times" its volume; this can tear the boiler off the frame ("rocket-like behavior") | [P] (same quotation) | NTSB SIR-96/05 |
| Explosive energy | G. B. Airy (1863): at only 60 psi, each cubic foot of boiler water holds the destructive energy of a pound of black powder | [P] (NTSB, citing Airy) | NTSB SIR-96/05 |
| Real case: Gettysburg 1278, 16 June 1995 | Crown sheet failed at about 15 mph; fire and steam blew through the firebox door into the cab; engineer burned over 65% of his body (third degree); two firemen hurt; none of the 310 passengers hurt | [P] | NTSB SIR-96/05 |
| Causes found | Crew let water fall below the crown sheet; the water glass was restricted by scale, so it showed a misleading level; only 4 of the boiler's 29 washout plugs were removed at washouts; no documented water treatment | [P] | NTSB SIR-96/05 |
| Pressure fell first | On the climb before the failure, boiler pressure dropped from about 230 to 175 psi; the crew assumed the glass would warn them | [P] | NTSB SIR-96/05 |
| Staged-failure design | Straight-thread crown stays at the front, button-head stays behind, so a dry crown sheet fails in stages; the NTSB notes this still fails suddenly and explosively | [P] | NTSB SIR-96/05 |
| How a fusible plug works (UK) | Screwed into the firebox crown; low water lets the plate heat, a low-melting filling melts, and steam escapes into the firebox. "This escape of steam, if serious, alerts the crew to the low water level" | [P] (heritage-railway boiler code, developed with HM Railway Inspectorate) | https://www.hra.uk.com/storage/HGR-B9008-Is03%20-Fusible%20Plugs.pdf |
| Plug materials (UK) | Body: leaded gunmetal LG4 or brass CZ121. Core: "Pure lead 99.97%, melting point 327 degrees C". Alloying lowers the melting point quickly, so only new pure lead is used | [P] | same (HRA HGR-B9008 Issue 03, 27 Jan 2026) |
| Plug life (UK) | Lead filling lasts typically 2 or 3 washout periods; a plug body 4 to 6; re-leading is allowed only once | [P] | same |
| How a fusible plug works (US) | A brass plug held by a ring of fusible alloy that "softens or melts at temperatures between 500 and 575 F"; it drops into the firebox, steam sprays the fire, attracts the crew and relieves pressure | [P] | NTSB SIR-96/05 |
| Cost of a melted plug | "the locomotive must be taken to a maintenance facility for repair"; this made plugs and low-water alarms controversial, and railways varied | [P] | NTSB SIR-96/05 |
| Not compulsory in the US | US rules don't require fusible plugs or low-water alarms; plugs that are fitted must be removed and cleaned of scale at each washout and at least every 31 service days | [P] | https://www.law.cornell.edu/cfr/text/49/230.59 ; NTSB SIR-96/05 |
| Low-water alarms | Made by Nathan, Ohio Injector and Barco; a whistle sounds until the water rises or the fire is dumped into the ashpan; crews sometimes muffled them as a nuisance | [P] | NTSB SIR-96/05 |

**Derived**: a pure-lead plug at 327 C sits about 120 C above saturated water at 255 psi gauge (208.7 C, see A4). A US alloy ring at 260–302 C sits 50–95 C above it. Either way, the plug only melts once the plate it is screwed into is dry and heating.

**Model suggestion (agent's inference)**: track water above the crown sheet (glass reading). Below 0, plate temperature climbs towards the fire temperature. Fusible plug melts (disabled until repaired, pressure vents) at about 327 C (UK lead) or 260–300 C (US alloy). Rupture risk rises sharply as the plate passes about 500 F (260 C) and becomes near-certain towards 1,600 F (870 C). Without a plug, low water goes straight to the explosion path.

#### Priming (water too high or dirty)

| Fact | Value | Label | URL |
|---|---|---|---|
| Main cause | "The most frequent cause is running the locomotive with too high a level of water in the boiler", most apparent when the regulator is opened sharply | [S] | https://en.wikipedia.org/wiki/Priming_(steam_locomotive) |
| Damage | Water in the cylinders "is not compressible and if trapped may fracture the cylinder head or piston"; lubrication is washed away | [S] | same |
| Crew response | Open the cylinder cocks; foam can make the gauge glass read wrongly, so "correcting" it can expose the crown | [S] | same |
| Salts cause it too | Increasing soda ash in 1933 caused "a serious epidemic" of priming: Rugby engines used to 1,500–2,500 miles between washouts were priming at 400–500 miles | [S] (Warburton, LMS Society) | https://www.lmssociety.org.uk/monographs/M13.pdf |
| Priming threshold, worked example | Feed water at 12 grains per gallon, used at 30 gal/mile, reached the "priming concentration of 180 grains per gallon" at 450 miles in a 1,000-gallon boiler | [S] | same |
| Continuous blowdown | 1 to 2.7 gal per mile, about 7% of the water used; kept water in the steam under about 1% between washouts; engines then ran over 14,000 miles | [S] | same |
| Intermittent blowdown | 15–30 s on the road, discharging 100–200 gal; 500–800 gal at the shed on the largest engines | [S] | same |
| Antifoams | 1950s polyamide and polyoxide antifoams let the boiler water reach up to 2,500 grains per gallon without priming; dose 1 lb per 300,000 gal | [S] | same |

#### Water treatment and scale (the chemistry angle)

| Fact | Value | Label | URL |
|---|---|---|---|
| How hardness was measured | Grains of "lime" per gallon (70,000 grains of water); soft water is under 3 degrees, hard water 20–30 or more | [S] | LMS monograph (URL above) |
| English waters | Mean hardness about 15 degrees; many supplies 20–25; Fenchurch Street 20, Tilbury 34, Shoeburyness 40 | [S] | same |
| Scale deposited | Evaporating 10,000 gal of 20-degree water deposited about 30 lb of scale | [S] | same |
| Fuel penalty | A ¼ in coating of scale on the tubes added about 5% to fuel; ⅜ in about 10% | [S] | same |
| Scale at railway scale | The LMS reckoned at least 70,000 tons of coal a year were wasted on its English Division through hard water | [S] | same |
| Maintenance without softening | Hard-water engines came out of service every 25,000–50,000 miles to remove tubes and clean out scale | [S] | same |
| Softening methods | Lime-soda plants at depots (producing a whitish sludge to be dumped), then base-exchange (zeolite); later briquettes of sodium carbonate, tannin, Calgon and antifoam added to lineside water | [S] | same; https://www.nrmfriends.org.uk/post/sludge |
| Copper firebox protection | Tannin extract (about 2 grains per gallon) added to inhibit corrosion | [S] | LMS monograph |
| US rule on washing | Boilers washed "as often as the water conditions require, but not less frequently than at each 31 service day inspection"; every washout plug removed | [P] | https://www.law.cornell.edu/cfr/text/49/230.60 |
| NTSB recommendation | Operators should have a documented water-treatment programme | [P] | NTSB SIR-96/05 |

**Chemistry notes (agent's own; standard chemistry, not from a source above)**

- 1 grain per imperial gallon = 1/70,000 by mass, about 14.3 mg/L. This is the Clark degree, usually expressed as CaCO3.
- Temporary (carbonate) hardness comes out on heating: Ca(HCO3)2 → CaCO3↓ + CO2 + H2O.
- Calcium sulphate has retrograde solubility, so it deposits hardest on the hottest surfaces: the firebox and tube plates.
- Lime-soda softening: lime precipitates the bicarbonate hardness as CaCO3 and Mg(OH)2; soda ash converts the permanent hardness (CaSO4) to CaCO3, leaving sodium sulphate in solution.
- Those soluble sodium salts are what then built up and caused the LMS's priming, hence blowdown.

### A4. Saturated steam: pressure against temperature

Source: NIST Chemistry WebBook, saturation properties of water, computed from the IAPWS-95 formulation (Wagner and Pruss) [P]. Query URL: https://webbook.nist.gov/cgi/fluid.cgi?Action=Data&Wide=on&ID=C7732185&Type=SatT&Digits=5&RefState=DEF&TUnit=C&PUnit=MPa&DUnit=kg%2Fm3&HUnit=kJ%2Fkg&WUnit=m%2Fs&VisUnit=uPa*s&STUnit=N%2Fm . The NIST range settings were ignored, so it returned its default grid; the rows below are copied from that grid.

| Absolute pressure (MPa) | Saturation temperature (C) | Liquid enthalpy hf (kJ/kg) | Vapour enthalpy hg (kJ/kg) | Vapour density (kg/m³) |
|---|---|---|---|---|
| 0.11093 | 102.53 | 429.85 | 2679.5 | 0.650 |
| 0.47865 | 150.19 | 633.02 | 2746.2 | 2.561 |
| 0.80960 | 170.90 | 723.03 | 2768.8 | 4.208 |
| 1.0302 | 181.18 | 768.25 | 2778.2 | 5.293 |
| 1.2141 | 188.49 | 800.68 | 2784.1 | 6.194 |
| 1.2876 | 191.17 | 812.64 | 2786.1 | 6.554 |
| 1.5450 | 199.70 | 850.90 | 2791.8 | 7.813 |
| 1.8760 | 209.16 | 893.82 | 2796.9 | 9.433 |
| 2.1702 | 216.54 | 927.63 | 2799.9 | 10.879 |
| 22.064 (critical point) | 373.95 | | | |

**Derived, at each engine's working pressure** (gauge + 0.1013 MPa, interpolated linearly between the NIST rows):

| Engine | Working pressure (gauge) | Absolute (MPa) | Boiler water temperature (C) |
|---|---|---|---|
| LMS 3F Jinty | 160 psi | 1.205 | about 188.2 |
| LB&SCR E2 | 170 psi | 1.273 | about 190.7 |
| USATC S100 | 210 psi | 1.549 | about 199.8 |
| Frisco 4500 | 255 psi | 1.860 | about 208.7 |
| UP 844 | 300 psi | 2.170 | about 216.5 |

**Derived**: latent heat at Frisco pressure is hg − hf ≈ 2796.9 − 893.8 ≈ 1,903 kJ/kg. Heating feed water from 15 C to boiling at that pressure takes another roughly 830 kJ/kg (hf at 208.7 C ≈ 892 kJ/kg, minus about 63 kJ/kg at 15 C; the 15 C value is standard steam-table data, not from the query above).

**Model suggestion**: above about 0.5 MPa the curve is gentle. A boiler at 255 psi is only about 20 C hotter than one at 160 psi, but much more dangerous because of the stored energy (Airy's comparison above).

### A5. Fuel and efficiency

| Fact | Value | Label | URL |
|---|---|---|---|
| Wood, solid, oven-dry | 19 GJ/t (5.3 kWh/kg), **net** (lower heating value) | [P] (UK Forest Research, Biomass Energy Centre data) | https://www.forestresearch.gov.uk/tools-and-resources/biomass-energy-resources/reference-biomass/facts-figures/typical-calorific-values-of-fuels/ |
| Log wood, stacked, **air-dry at 20% moisture** | 14.7 GJ/t (4.1 kWh/kg), net; bulk density 350–500 kg/m³ | [P] | same |
| Wood chips, 30% moisture | 12.5 GJ/t, net | [P] | same |
| House coal; anthracite | 27–31 GJ/t; 33 GJ/t, net | [P] | same |
| Heating oil | 42.5 GJ/t (11.8 kWh/kg), net; density 845 kg/m³ | [P] | same |
| Residual fuel oil (the heavy oil steam locomotives burned, e.g. UP's No. 5 oil) | 6.287 million Btu per barrel, **gross** (higher heating value); a Bureau of Mines value adopted in 1950 | [P] (US EIA, Monthly Energy Review, Sept 2026, Table A1 and notes) | https://www.eia.gov/totalenergy/data/monthly/pdf/mer_A_FULL.pdf |
| Heating oil, diesel, cord of wood (EIA sample factors) | 138,500 Btu/gal; 137,381 Btu/gal; 20,000,000 Btu per cord | [P] | https://www.eia.gov/energyexplained/units-and-calculators/british-thermal-units.php |
| Wood, oven-dry, gross (commonly cited) | about 8,600 Btu/lb for hardwoods, about 9,000 for resinous softwoods | [S] (search summary of Missouri Extension and Baker/FPL; neither PDF could be opened) | https://extension.missouri.edu/media/wysiwyg/Extensiondata/Pub/pdf/agguides/forestry/g05450.pdf |
| Steam locomotive thermal efficiency, conventional | **7.8% overall at the drawbar** for a standard Chinese QJ 2-10-2, the product of boiler combustion 78% × boiler absorption 78.2% × auxiliaries 93.1% × cylinders 16.4% × transmission 89% × drawbar 94% | [S] (Advanced Steam Traction Trust, quoting David Wardale's design calculations, *The Red Devil*) | https://advanced-steam.org/5at/5at-project/5at-features/thermal-efficiency/ |
| Same source, improved designs | Modified QJ 11.0%; Wardale's "third generation" non-condensing 16.3%; Porta's condensing estimate 25% | [S] | same |
| Measured US figures | About 6% at the drawbar is widely quoted (C&O 614 tests, 1985; N&W estimate); a NYC Niagara figure of 4.77% at 2,500 dbhp | [S] (forum, anecdotal; search summary only) | https://forum.trains.com/t/american-steam-locomotive-efficiency-the-effect-of-blastpipe-size-and-superheat-levels/224508 |

**Derived**:

- 6.287 MMBtu/bbl ÷ 42 US gal ≈ 149,700 Btu/US gal ≈ 41.7 MJ/L (gross).
- Air-dry wood at 14.7 GJ/t net carries about 35% of heating oil's 42.5 GJ/t per tonne. By stacked volume (5.2–7.4 GJ/m³ against 36 GJ/m³) it is only **15–20% of oil's**. So **a wood-fired engine needs about five to seven times the bunker volume of an oil-fired one for the same range**. That is a strong, real reason the wood variants should need fuel far more often.
- The boiler alone (combustion × absorption) is about 61% efficient in Wardale's standard-QJ case. Most of the loss is in the cylinders (16.4%), not the firebox.

### A6. Water stops

| Fact | Value | Label | URL |
|---|---|---|---|
| Early US practice | Water stops "every 7–10 miles"; a Mojave desert section of the Atlantic & Pacific had them every 5–10 miles | [S] (citation is a dead link) | https://en.wikipedia.org/wiki/Water_stop |
| With tenders | "trains could run 100–150 miles without a refill" | [S] (tagged citation needed) | same |
| Track pans (water troughs) | First at Mochdre, LNWR, 23 June 1860; used in the US mainly by the New York Central and the Pennsylvania | [S] | https://en.wikipedia.org/wiki/Track_pan |
| Trough length and spacing (GWR) | 524–620 yards long; typically 40–50 miles apart; UK main-line pairs ranged from about 15 to 64 miles apart in 1934 | [S] | same |
| Water picked up | GWR: 944 imp gal in 440 yards at 15 mph; best speed 45 mph; Ramsbottom measured 1,060–1,150 gal per pick-up at 22–50 mph | [S] | same |
| Tender capacities | Frisco 4500: 18,000 US gal; UP 844: 23,500 US gal; small tank engines about 1,000–1,250 imp gal (tables above) | [S]/[P] | as above |

---

## Part B: diesel-electric shunting locomotives

### B1. How the transmission works

| Fact | Value | Label | URL |
|---|---|---|---|
| Power path | The diesel turns a main three-phase traction alternator; its rectified output goes over a DC bus to the traction motors' armatures (modern GE practice) | [P] (GE patent US6725134B2, filed 2002) | https://patents.google.com/patent/US6725134 |
| Throttle | "a locomotive throttle conventionally has eight power positions or notches (N1-N8), plus an idle position"; N1 is minimum engine speed and horsepower, N8 maximum speed and full power | [P] | same |
| Older DC switchers | Main generator 600 V DC, 350 kW, belted exciter and auxiliary generator; four traction motors, gear ratio 14:72 (Army 500 hp RS-4-TC-1A) | [P] (US Army TM 55-2210-223-12) | http://railroadmanuals.tpub.com/TM-55-2210-223-12/css/TM-55-2210-223-12_18.htm |
| UK shunter | Class 08: English Electric 6KT engine, an EE 801 DC generator, two EE 506 motors with **double-reduction gearing**; no gearbox | [S] | https://en.wikipedia.org/wiki/British_Rail_Class_08 |
| Modern two-axle shunter | EffiShunter 300 has AC/DC transmission (Siemens traction alternator), motors nose-suspended on the axles | [P] (manufacturer) | https://czloko.com/products/lokomotivy-1/effishunter-300-2.htm |

### B2. Traction motor ratings: continuous against short-time

**The best primary table found**: US Army TM 55-2210-223-12 (also TO 45A2-2-14-41), for the Baldwin-Lima-Hamilton **RS-4-TC-1A**. This is a 500 bhp, 122,100 lb (about 61 short tons), four-motor switcher, repowered with a Caterpillar 3508 engine at 1,200 rpm. Its traction motors are Westinghouse 974-A, 640 V DC, 600 A. The load meter reads half the main generator's output [P].
URL: http://railroadmanuals.tpub.com/TM-55-2210-223-12/css/TM-55-2210-223-12_63.htm

| Main generator current | Load meter reading | Time allowed |
|---|---|---|
| 0 to 1,200 A | 0 to 600 | Continuously |
| 1,200 to 1,400 A | 600 to 700 | 20 min |
| 1,400 to 1,600 A | 700 to 800 | 10 min |
| 1,600 to 1,920 A | 800 to 960 | 5 min |

- **At the limit**: "When the time limit for that overload range is reached, the generator and motors are at their maximum allowable temperature." To avoid severe damage, **either idle with the load off for 15 to 30 minutes**, after which an overload band may be used again, **or reduce to the continuous range and carry on** [P].
- **Sister manual**: TM 55-2210-224-12, for a 60-ton, 500 hp, 0-4-4-0 RS-4-TC-1A, uses a green continuous segment and an orange overload segment marked in minutes, with the same 15–30 minute cooling rule [P]. URL: http://railroadmanuals.tpub.com/TM-55-2210-224-12/css/TM-55-2210-224-12_62.htm
- **Same manual, wheel slip**: "If slipping occurs, reduce throttle first. Never apply sand while the wheels are slipping, as this could cause a broken traction motor gear." [P]

**EMD road-locomotive motors, for shape** [S] (forum transcriptions, anecdotal): https://www.rypn.org/forums/viewtopic.php?p=339143

| Motor | Continuous | 1 h | ½ h | ¼ h | 10 min | 5 min |
|---|---|---|---|---|---|---|
| EMD D37 (F9) | 900 A | 925 A | 970 A | 1,065 A | 1,140 A | 1,275 A |
| EMD D57 (GP30), cited to "Table HFR dated 3-28-68; based on ERC-922" | 980 A | 1,000 A | 1,025 A | 1,075 A | — | — |

The same thread quotes "an EMD operator's manual": the short-time ratings apply **only at throttle 8**, because they assume maximum cooling air through the motors; "These values decrease as engine speed and cooling air is decreased" [S, forum].

**Why motors overheat when pulling hard at low speed**

What the sources say:

- **The Army manual**: the time limits exist because the generator and motors reach their maximum allowable temperature [P].
- **A GE patent**: overload protection exists to prevent "excessive overheating of the electrical winding insulation system". Systems may "operate for short times beyond the continuous ratings" (WO1993020608A1, GE, Vanek and McGrath, priority 1992) [P]. https://patents.google.com/patent/WO1993020608A1/en
- **Trains magazine** (per a search summary; the page was blocked): below its minimum continuous speed, a DC locomotive's engineer must follow the short-time rating on the cab ammeter; the higher the amps, the shorter the time allowed [S]. https://www.trains.com/trn/train-basics/ask-trains/locomotives-minimum-speed

The physics, which is the agent's explanation from standard electrical engineering and is not quoted from a source:

- In a series DC motor, torque, and so tractive effort, rises with current.
- Heating in the windings goes as I²R.
- At low speed the engine's fixed horsepower turns into high tractive effort and so high current. Heat grows with the square of current while the useful work done is small.
- On many switchers the motor blowers are belt-driven from the engine (this manual lists "traction motor blower belts"). So cooling also depends on engine speed, which is why the ratings assume notch 8.

### B3. Reference figures for small diesel-electric shunters

| Fact | BR Class 08 (0-6-0 DE, UK) | EffiShunter 300 (two-axle Bo, Czech, current) | RS-4-TC-1A (US Army, four motors) |
|---|---|---|---|
| Engine | English Electric 6KT, 400 hp (298 kW) at 680 rpm; speed range 300–680 rpm; some 350 hp | CAT C13B, 328 kW nominal | Caterpillar 3508, 500 bhp (373 kW), governed 1,200 rpm |
| Transmission | DC generator, 2 × EE 506 axle-hung motors, double reduction | AC/DC, Siemens alternator, nose-suspended motors | Westinghouse 1604C generator (600 V, 350 kW), 4 × 974-A motors |
| Max tractive effort | 35,000 lbf (156 kN) | 113 kN (maker's page); 124 kN quoted in trade press | not stated in pages read |
| **Continuous tractive effort** | **11,100 lbf at 8.8 mph** | not stated | continuous 1,200 A main generator current (see B2) |
| Top speed | 15 mph (or 20 mph variant) | 60 km/h | 30 mph |
| Weight | about 50 tons (49–51 long tons) | 36 t (catalogue gives 30 t standard) | 122,100 lb |
| Fuel | 668 imp gal | 700 L | 500 US gal |
| Brake force / dynamic brake | 19 tonnes-force (vacuum or air); no dynamic brake mentioned | DAKO air brake, stored-energy parking brake; no electric brake mentioned | Westinghouse 26-NL air brake |
| Label | [S] Wikipedia; continuous TE from the Keighley & Worth Valley Railway's data file for 08266 [S] | [P] maker | [P] Army TM |
| URL | https://en.wikipedia.org/wiki/British_Rail_Class_08 ; https://kwvr.co.uk/?p=16950 | https://czloko.com/products/lokomotivy-1/effishunter-300-2.htm ; https://railwaygazette.com/traction-and-rolling-stock/d-orders-effishunter-300-locomotives/44888.article (seen only in a search summary) | TM 55-2210-223-12 pages 17–18 |

**Derived**:

- **Class 08**: 11,100 lbf × 8.8 mph ÷ 375 ≈ 260 hp at the rail, about 65% of the 400 hp engine. Starting tractive effort is about 3.2 times the continuous figure. So the "pulling hard at low speed" zone runs from standstill to about 9 mph, and **the 08 can only stay there for a limited time**.
- **Goat DE2**: the mod's two-axle machine sits closest to the EffiShunter 300. Its 113 kN on 36 t is an adhesion coefficient of about 0.32 (113 ÷ (36 × 9.81)).

### B4. Dynamic braking (brief)

| Fact | Value | Label | URL |
|---|---|---|---|
| Principle | In dynamic braking the traction motors act as generators; the energy goes to a grid of resistive elements "for absorbing and dissipating electrical energy", cooled by a fan | [P] (GE patent) | https://patents.google.com/patent/US6725134 |
| Small shunters | None of the shunter pages read (Class 08, EffiShunter 300, RS-4-TC-1A) lists a dynamic brake | — | as above |

**Agent's view**: dynamic braking is worth little on a yard shunter at 15–60 km/h. If the Goat gets it, label it a gameplay choice.

---

## Part C: multi-engine thrust-lifted craft and engine-out

| Craft | What happened or was designed | Thrust margin | Label | URL |
|---|---|---|---|---|
| Falcon 9 (CRS-1, 7 Oct 2012) | Engine 1 of 9 lost pressure and shut down about 1 min 19 s into flight. The rocket "is designed to withstand an engine failure"; the remaining engines burned longer and Dragon reached its intended orbit. The secondary Orbcomm satellite went to a much lower orbit, because NASA safety rules forbade the second-stage burn it needed | not stated | [S] (Planetary Society, Jason Davis, 8 Oct 2012, paraphrasing SpaceX) | https://www.planetary.org/blogs/guest-blogs/jason-davis/20121008-spacexs-first-paid-cargo-run.html |
| Apollo LM descent engine | **One** engine; no engine-out. Throttled from 10% to 65%, and fixed at 92.5% of the 10,500 lbf rated thrust (65–92.5% was a non-operating region). Reliability came from redundant valves, regulators and a triple-redundant throttle actuator; the fallback was to abort with the separate ascent engine | throttle range 10–92.5% | [P] (NASA TN D-7143, Hammock, Currie and Fisher, March 1973) | https://www.sierrafoot.org/x-15/NASA_TN_D-7143.pdf (mirror; NTRS 19730011150) |
| Yakovlev Yak-38 | One vectoring main engine (66.7 kN) plus two lift jets (31.9 kN each). It had an automatic ejection seat: if a lift engine failed, or the aircraft rolled past 60 degrees, the seat fired without pilot action | VTOL max take-off weight 10,300 kg, 11,300 kg for the Yak-38M | [S] (Wikipedia, specs citing Wilson 2000; the ejection text is unsourced) | https://en.wikipedia.org/wiki/Yakovlev_Yak-38 |
| Dornier Do 31 (1967 VTOL transport) | 2 Pegasus vectored-thrust engines (68.95 kN each) inboard plus 8 RB162 lift jets (19.57 kN each) in wingtip pods. No engine-out provision is described in the article | **Derived**: total static thrust ≈ 294.5 kN; about **1.34** of weight at the 22,453 kg VTOL gross weight, 1.09 at the 27,422 kg maximum take-off weight | [S] (Wikipedia, specs citing Green 1968) | https://en.wikipedia.org/wiki/Dornier_Do_31 |
| Bell Boeing V-22 Osprey | The two engines are cross-linked: "Either engine can power both proprotors through the wing driveshaft". **But "the V-22 is generally not capable of hovering on one engine"** | one engine is not enough to hover | [S] (Wikipedia, citing Norton and v22fly) | https://en.wikipedia.org/wiki/Bell_Boeing_V-22_Osprey |
| Quadrocopter research | Mueller and D'Andrea (ETH Zürich), "Stability and control of a quadrocopter despite the complete loss of one, two, or three propellers", ICRA 2014, pp. 45–52, DOI 10.1109/ICRA.2014.6906588. Per a search summary of the abstract (full text not read), the vehicle survives by spinning about an axis and tilting it to steer; one- and two-propeller loss was shown in flight | not read | [P] metadata (Crossref); content [S] search snippet | https://api.crossref.org/works/10.1109%2FICRA.2014.6906588 |

**Derived for the drop pods (agent's inference, label as authored)**:

- For N equal thrusters to hold a hover after one fails, each surviving thruster must give W/(N−1). So the full-thrust T/W needed is **N/(N−1)** just to hover: 1.33 for four thrusters, 1.5 for three. That is before any control margin.
- A **four-thruster pod** that loses one thruster also loses its symmetry. It needs either throttling back the opposite thruster (leaving two), a cross-link like the V-22's, or thrust vectoring.
- A **three-thruster pod** that loses one is left with two thrusters on a line, so it can't hold attitude without vectoring.
- The real precedents cluster around two answers. **Have lots of spare thrust and redundancy (Falcon 9's nine engines), or don't attempt engine-out at all** and provide an escape instead (Apollo's ascent-stage abort, the Yak-38's automatic ejection).

---

## Not found (searched, but no source read gives it)

- **Frisco 4500**: total heating surface and tubes/flues breakdown in one source (only the evaporative 4,766 sq ft); any coal-to-oil conversion of 4503–4524; Frisco's water-stop spacing; a builder's or Frisco primary data sheet.
- **Wood firing** of either class: no real wood-burning 4-8-4 or 0-6-0T data. Wood-burning grate or firebox proportions were not researched.
- **LB&SCR E2 and S100 top speeds.**
- **A primary or official source for raising-steam times on large engines** (only heritage-operator and forum figures). A BR *Handbook for Railway Steam Locomotive Enginemen* (1957) text was not available online.
- **Water consumption per hour** for any named engine (per-mile figures only).
- **An official EMD short-time rating table**: the SW1500 operator manual link was dead; the EMD figures above are forum transcriptions.
- **Class 08**: notch count and the power controller's description; its one-hour rating; any dynamic braking.
- **A two-axle diesel-electric shunter's continuous tractive effort** (EffiShunter's isn't published on the page read).
- **A primary source for Falcon 9 landing thrust-to-weight** (claims that one Merlin at minimum throttle exceeds the empty stage's weight seen only on unsourced pages); SpaceX's own CRS-1 statement text (spaceref page blocked).
- **The USDA Forest Products Laboratory's own wood heating-value page** (the PDFs were scanned images or blocked); the Missouri Extension guide (blocked).

## Sources read

**Regulation, investigation and standards [P]**

- 49 CFR Part 230, sections 230.37, 230.48, 230.49, 230.51, 230.54, 230.59, 230.60 (via Cornell LII): https://www.law.cornell.edu/cfr/text/49/part-230/subpart-B
- NTSB Special Investigation Report SIR-96/05, firebox explosion of Gettysburg Passenger Services locomotive 1278, 16 June 1995: https://www.ntsb.gov/safety/safety-studies/Documents/SIR9605.pdf
- Heritage Railway Association, Boiler Code of Practice guidance note HGR-B9008 *Fusible Plugs*, Issue 03, 27 Jan 2026: https://www.hra.uk.com/storage/HGR-B9008-Is03%20-Fusible%20Plugs.pdf
- NIST Chemistry WebBook, saturation properties of water (IAPWS-95): https://webbook.nist.gov/cgi/fluid.cgi?ID=C7732185&Action=Page
- US EIA, Monthly Energy Review Appendix A (Sept 2026): https://www.eia.gov/totalenergy/data/monthly/pdf/mer_A_FULL.pdf ; EIA British thermal units page: https://www.eia.gov/energyexplained/units-and-calculators/british-thermal-units.php
- UK Forest Research, Typical calorific values of fuels: https://www.forestresearch.gov.uk/tools-and-resources/biomass-energy-resources/reference-biomass/facts-figures/typical-calorific-values-of-fuels/
- US Army TM 55-2210-223-12 (load limits, equipment data) and TM 55-2210-224-12 (load limits, wheel slip): http://railroadmanuals.tpub.com/TM-55-2210-223-12/ and http://railroadmanuals.tpub.com/TM-55-2210-224-12/
- GE patents US6725134B2 and WO1993020608A1: https://patents.google.com/patent/US6725134 ; https://patents.google.com/patent/WO1993020608A1/en
- CZ LOKO, EffiShunter 300 product page: https://czloko.com/products/lokomotivy-1/effishunter-300-2.htm
- Union Pacific, No. 844 page: https://www.up.com/heritage/steam/844/index.htm
- NASA TN D-7143, Apollo Experience Report: Descent Propulsion System (1973): https://www.sierrafoot.org/x-15/NASA_TN_D-7143.pdf
- Great Central Railway, Getting an Engine Ready: https://www.gcrailway.co.uk/about/engine-shed/engine-ready/
- Crossref record for Mueller and D'Andrea (2014): https://api.crossref.org/works/10.1109%2FICRA.2014.6906588

**Secondary [S]**

- Wikipedia: St. Louis–San Francisco class 4500; St. Louis–San Francisco 4500; LB&SCR E2 class; LMS Fowler Class 3F; USATC S100 Class; Union Pacific 844; Priming (steam locomotive); Track pan; Water stop; British Rail Class 08; British Rail Class D1/3; GE 45-ton switcher; Traction motor; Yakovlev Yak-38; Dornier Do 31; Bell Boeing V-22 Osprey (URLs in the tables)
- railarchive.net, SLSF 4503: https://railarchive.net/randomsteam/slsf4503.htm
- L. G. Warburton, *Boiler Feed Water and Water Softening on the LMS and the London Midland Region*, LMS Society Monograph 13: https://www.lmssociety.org.uk/monographs/M13.pdf
- Friends of the National Railway Museum, "Sludge!" (2020): https://www.nrmfriends.org.uk/post/sludge
- Advanced Steam Traction Trust, Thermal Efficiency (Wardale's figures): https://advanced-steam.org/5at/5at-project/5at-features/thermal-efficiency/
- Keighley & Worth Valley Railway, Class 08 data file (08266): https://kwvr.co.uk/?p=16950
- Planetary Society, Jason Davis, "SpaceX's first paid cargo run" (8 Oct 2012): https://www.planetary.org/blogs/guest-blogs/jason-davis/20121008-spacexs-first-paid-cargo-run.html
- RYPN forum, traction motor short-time ratings (forum): https://www.rypn.org/forums/viewtopic.php?p=339143

**Seen only through search summaries, not opened (treat as weaker)**: the trains.com and ogrforum raise-steam threads; the UP steam update (2016); the railpictures.net caption; the BRHS mailing-list post; the trains.com efficiency thread; Trains magazine's "Locomotives' minimum speed"; Railway Gazette on the EffiShunter 300; the Missouri Extension wood-fuel guide.
