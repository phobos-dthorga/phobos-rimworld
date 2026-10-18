# Radio reference checks

18 Oct 2026 · agent record, for the owner's review

**Result:** 9 of the 13 figures in the design's Reference table check out: 6 by calculation and 3 against
a primary source or the game's own data. The Australian licence limits check out with an important twist:
the higher limits apply only to SSB (and FM for Advanced), and every other mode is capped lower. The two
dish formulas are standard approximations whose sources were not opened. The last two (voice SNR and
amplifier efficiency) were unverified on 18 Oct; on 19 Oct a convention and a secondary source were
found for them (rows below), enough for Phase 1 Defs that say "approximate".

The design is [radio-framework-design.md](radio-framework-design.md); its Reference table is unchanged.
This record sits beside it so the snapshot stays as written.

## Status of each figure

**Calculated** means checked here from SI-defined constants (c = 299,792,458 m/s; k = 1.380649 × 10⁻²³ J/K),
with no outside source needed. **Sourced** means checked against the primary source linked in the row.

| Quantity | Design figure | Status | Finding |
| --- | --- | --- | --- |
| Free-space path loss | 20 log₁₀ d(km) + 20 log₁₀ f(MHz) + 32.44 dB | Calculated | The constant is 20 log₁₀(4π × 10⁹ / c) = 32.45 dB. Either 32.44 or 32.45 is fine. |
| Distance rule | 2× distance = +6 dB = 4× power | Calculated | 20 log₁₀ 2 = 6.02 dB, a power ratio of 4.0. |
| Thermal noise | −174 dBm/Hz at 290 K | Calculated | 10 log₁₀(kT × 1000) = −173.98 dBm/Hz. |
| Noise in 2.5 kHz | About −140 dBm, before noise figure | Calculated | −173.98 + 10 log₁₀ 2500 = −140.0 dBm. |
| Radio horizon | ≈ 4.12 × (√h₁ + √h₂) km | Calculated | √(2 × 4/3 × 6,371 km) gives 4.12 km per √m. It assumes the standard 4/3-Earth refraction; with no refraction it is 3.57. |
| Bounce path | Loss rises with d⁴; moonbounce ≈ 252 dB at 144 MHz | Calculated | Radar equation, Moon radius 1,737.4 km, lunar reflectivity 0.065: 250.8 dB at perigee (356,400 km), **252.1 dB at mean distance**, 253.1 dB at apogee (406,700 km). The 0.065 is the figure commonly used for the Moon; its primary source was not opened. |
| Weak-signal digital | Decodes near −21 dB SNR in 2.5 kHz | Sourced | FT8 decodes half the time at **−20.8 dB** (no prior information, steady channel) and −22.7 dB with the most prior information. On a disturbed mid-latitude path it is −18.6 dB; on a high-latitude (auroral) path, **−8.6 dB**. [Franke, Somerville and Taylor, "The FT4 and FT8 Communication Protocols", QEX July/August 2020, Table 6](https://wsjt.sourceforge.io/FT4_FT8_QEX.pdf). |
| Vanilla comms console draw | 200 W | Sourced | `basePowerConsumption` is 200 in RimWorld 1.6.4871's `Data/Core/Defs/ThingDefs_Buildings/Buildings_Misc.xml` (read 18 Oct 2026). Also there: 3 × 2 cells, 120 steel and 4 industrial components, research Microelectronics Basics, short-circuits in rain. |
| Australian licence limits | Foundation 10 W, Standard 100 W, Advanced 400 W | Sourced, with conditions | See below. |
| Dish gain | η × (πD/λ)², η ≈ 0.55–0.7 | Standard formula, source not opened | Example: a 3 m dish at 10 GHz gives 47.4 to 48.4 dBi. |
| Dish beamwidth | ≈ 70 × λ/D degrees | Standard approximation, source not opened | Example: 0.7° for that same dish. |
| Voice | Comfortable copy near +10 dB SNR | Sourced convention (19 Oct 2026) | Narrowband FM sensitivity is quoted at **12 dB SINAD** by convention ([Repeater Builder](https://repeater-builder.com/tech-info/receiver-sensitivity.html)); ITU-R SM.1840 specifies 20 dB SINAD for VHF/UHF FM tests. SINAD is not SNR (it includes distortion), so the ModeDef takes 12 dB and labels that a simplification. |
| Amplifier efficiency | Linear SSB ≈ 50%; FM ≈ 60–70% | Secondary source (19 Oct 2026) | A licence study guide gives class AB (linear, SSB) 50–70 % and class C (FM, CW) 75–85 % as theoretical maxima ([open-exam-prep](https://open-exam-prep.com/study-guides/au-amateur-radio-standard-theory/transmitters-and-receivers/amplifier-classes)). Real amplifiers run lower. The ARRL Handbook was not opened. The design's 50 % and 60–70 % are reasonable practical figures; the Defs label them approximate. |

## Australian licence limits

The limits come from the [Radiocommunications (Amateur Stations) Class Licence 2023 (F2023L01648)](https://www.legislation.gov.au/F2023L01648/asmade/text),
Schedule 2, Tables A to C, in force since 19 February 2024. Checked against the text as made (12 Dec 2023);
later amendments were not checked. pX is peak envelope power; pY is mean power.

| Qualification | SSB (J3E, R3E) | FM (C3F) | Every other mode | Bands (HF and up, summarised) |
| --- | --- | --- | --- | --- |
| Foundation | 10 W pX | 10 W pX | 10 W pX | 80 m, 40 m, 15 m, 10 m, 2 m, 70 cm |
| Standard | 100 W pX | 30 W pY (100 W pX on 50–52 MHz only) | 30 W pY | Adds 20 m, 6 m, and 23 cm to 5.8 GHz |
| Advanced | 400 W pX | 400 W pX | 120 W pY | Adds 160 m, the WARC bands, LF and MF (1 W and 5 W pX EIRP), and microwave to 250 GHz |

**What this means for the design.** "Licence tiers as research" mirrors the classes correctly at the
headline figures. If tiers ship, a mode-aware cap would be both more faithful and more interesting: SSB
voice gets the full power, while CW and digital modes (which need far less signal) are held to roughly a
third. That is a design choice for the owner, not a finding.

## Notes for later phases

- **Aurora and FT8.** Table 6's high-latitude row shows why the design's "aurora can add flutter" is
  real. A 10 Hz Doppler spread is wider than FT8's 6.25 Hz tone spacing, so FT8 loses about 12 dB. FT4's
  wider tones lose less (−12.2 dB). That gives a reason to carry more than one weak-signal mode.
- **Moonbounce swing.** The Moon's distance moves the path loss by about 2.3 dB between perigee and
  apogee. That is small enough to ignore in Phase 4, or a nice detail for Engineer mode.

## Method

Calculations were run in Python on 18 Oct 2026. The QEX paper and the class licence were downloaded and
read in full text. The comms console figures were read from the installed game's Def file, which is not
redistributed here.
