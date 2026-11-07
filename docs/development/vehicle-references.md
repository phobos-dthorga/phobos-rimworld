# Vehicle simulation references (8 Nov 2026)

A study library for [vehicle systems](vehicle-systems-design.md) and the
[drivetrain and engine speed design](vehicle-drivetrain-design.md): the books, papers, open code and game documentation
worth learning from, what each is good for, and on what terms we may use it. The owner asked for it on 8 Nov 2026
("a body of references we can learn and study from"). **Nothing here has been built or tested in game.**

## Read this first

- **Ideas and figures, not code.** The repository holds no third-party code (`THIRD_PARTY_NOTICES.md`). Learning from
  anything below is fine. Porting code is a separate decision: MIT, Apache 2.0 and BSD code may be ported only if the
  owner accepts adding a notice and dropping the "no third-party code" line. GPL code and code with no licence are
  read-only: never copy or adapt them.
- **Every figure taken from here keeps its label** in Defs and records: primary source, secondary source, our inference
  or authored (AGENTS.md, "Research attribution").
- **"Read here" means opened and checked on 8 Nov 2026.** "Listed" means its existence, authors and edition were checked
  but its contents were not read in this session. Verify a listed source's figures before a Def cites them.
- **Low-quality sources are marked.** Engineering-toolbox sites and forums are fine for checking orders of magnitude.
  They are never a figure's only source.

## Suggested study order

1. Guzzella and Sciarretta, chapters 2 and 3: the quasi-static method and engine and gearbox models. This is the
   backbone of the drivetrain design.
2. Wipke, Cuddy and Burch on ADVISOR: why backward-facing models are fast, and what they give up.
3. Gillespie, chapters 2 and 4: acceleration limits, road loads and rolling resistance.
4. Jolt's `VehicleTransmission` and BeamNG's thermal settings, for how games expose these models to designers.
5. Wong, chapters 2 and 6, when tracked vehicles and soft ground come up.
6. Tremblay and Dessaint, and an alternator pulley guide, for the electrical side.

## Method: how to model a powertrain cheaply

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| L. Guzzella and A. Sciarretta, *Vehicle Propulsion Systems: Introduction to Modeling and Optimization*, 3rd ed., Springer 2013, [doi:10.1007/978-3-642-35913-2](https://doi.org/10.1007/978-3-642-35913-2) | Textbook (ETH Zürich, IFP Energies Nouvelles) | The **quasi-static (backward-facing) approach**: start from the vehicle's speed, work back through wheels, gearbox and engine to fuel. Its contents cover normalised engine variables, engine efficiency representation (the Willans approach) and gearbox models. It is limited to longitudinal behaviour, which is all RimWorld needs. | Listed (contents pages read) |
| K. Wipke, M. Cuddy, S. Burch, "ADVISOR 2.1: A User-Friendly Advanced Powertrain Simulation Using a Combined Backward/Forward Approach", *IEEE Trans. Vehicular Technology* 48(6), 1999, pp. 1751–1761 ([NREL record](https://research-hub.nrel.gov/en/publications/advisor-21-a-user-friendly-advanced-powertrain-simulation-using-a-2/)) | Peer-reviewed paper (NREL) | Its abstract reports 2.6 to 8 times faster runs than a representative forward-facing model, with acceleration time within 0.7% and US06 energy use within 0.6%. That is the speed-for-fidelity trade we are making. | Read here (abstract) |
| [ADVISOR documentation](https://adv-vehicle-sim.sourceforge.net/) (NREL, 2003) | Manual | Section 3.4: the backward- and forward-facing calculation paths drawn as data flow. | Listed |
| [NREL FASTSim](https://github.com/NREL/fastsim) | Open code, Apache 2.0 | A modern descendant of the same method, plus lumped thermal models for engine, cabin and catalyst. The best open code to read for structure. | Read here (README, release notes) |

## Vehicle dynamics and road loads

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| T. D. Gillespie, *Fundamentals of Vehicle Dynamics*, Revised Edition, SAE International 2021, [doi:10.4271/R-506](https://doi.org/10.4271/R-506) (1st ed. 1992, R-114) | Textbook | Chapter 2: power-limited and traction-limited acceleration (our speed cap). Chapter 4: aerodynamics, rolling resistance and total road load. Chapter 10: tyre tractive properties. | Listed (contents read) |
| J. Y. Wong, *Theory of Ground Vehicles*, 5th ed., Wiley 2022 ([Wiley](https://onlinelibrary.wiley.com/doi/book/10.1002/9781119719984)) | Textbook (Carleton University) | Chapter 2, terramechanics: soft-ground motion resistance and tractive effort, for wheels against tracks. Chapter 6: skid-steering of tracked vehicles and its power cost. | Listed (contents read) |
| J. Y. Wong, *Terramechanics and Off-Road Vehicle Engineering*, 3rd ed., Elsevier 2024 ([Elsevier](https://www.elsevier.com/books/catalog/isbn/9780443156144)) | Textbook | Deeper soft-ground mobility, if mud, snow and sand become a feature. | Listed |
| Rolling resistance tables: [Engineering ToolBox](https://engineeringtoolbox.com/rolling-friction-resistance-d_1303.html), [x-engineer](https://x-engineer.org/?p=4832), [HPWizard (rolling and friction coefficients)](https://hpwizard.com/tire-friction-coefficient.html) | Web tables, secondary and low quality | Orders of magnitude only: about 0.01 on asphalt, 0.05 on unpaved roads, and 0.15 to 0.35 on sand and fields. HPWizard also gives peak tyre friction (dry asphalt 0.8 to 0.9, gravel 0.6, wet earth 0.55). Replace with Gillespie or Wong before a Def cites them. | Read here |
| B. Beckman, *The Physics of Racing* ([series index](http://phors.locost7.info/contents.htm)) | Article series by a physicist, free to read | Readable derivations of traction, weight transfer and power. Good for intuition. | Listed |

## Engines

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| J. B. Heywood, *Internal Combustion Engine Fundamentals*, 2nd ed., McGraw-Hill 2018 | Textbook | The standard text: engine energy balance (fuel to work, coolant, exhaust), friction mean effective pressure and its rise with speed, and heat transfer. The source to replace our authored "share of fuel heat into coolant". | Listed (not opened here; edition from memory, check it) |
| [JSBSim `FGPiston`](https://jsbsim-team.github.io/jsbsim/classJSBSim_1_1FGPiston.html) and the [FlightGear engine page](https://wiki.flightgear.org/JSBSim_Engines) | Open code, LGPL (read only for us) | A game-grade piston engine: oil pressure rises linearly with rpm to a relief-valve value, then is corrected for oil temperature and viscosity. Cylinder-head mass sets how fast temperature changes. Our phase-C aircraft reference. | Read here |
| David Luff's cylinder-head temperature model, [FlightGear commit notes, Oct 2000](https://git.nayala.duckdns.org/fly/flightgear/commits/commit/c1313f2ecb831bd60fe17f024ed196db7d9d0193/src/FDM?page=28) | Developer notes | A worked example of a single-lump temperature with free and forced convection terms. Its tuning principle: the thermal mass sets the rate; the heat terms set where it settles. | Read here |
| [BeamNG wiki: Engine Thermal Simulation](https://wiki.beamng.com/Engine_Thermal_Simulation) | Game documentation; closed code | A designer-facing list of thermal and damage settings: air-cooled engines, oil thermostat and radiator, radiator leak on deformation, head-gasket damage that builds up over time, and engines that spawn already damaged. Compare our schema against it. | Read here |
| [BeamNG 0.5.3 release notes](https://beamng.com/game/news/patch/version-0-5-3-released) | Game news | The thermal temperatures BeamNG tracks (coolant, oil, cylinder walls, block, exhaust manifold) and its failure modes. | Read here |
| [Engine Simulator](https://github.com/ange-yaghi/engine-sim) (Ange Yaghi) | Open code, MIT | Crank-angle-resolved engine simulation for sound. Not for runtime; its author says it is not a scientific tool. A future idea for recording engine sounds offline. | Read here (README) |

## Gearboxes and drivetrains in games

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| [Jolt Physics `VehicleTransmission`](https://jrouwe.github.io/JoltPhysics/class_vehicle_transmission_settings.html) (Jorrit Rouwe) | Open code, MIT (`SPDX-License-Identifier: MIT` in the header) | Automatic shifting a designer can tune: gear ratios, an upshift rpm, a downshift rpm, shift and clutch-release times. Its defaults (2.66, 1.78, 1.30, 1.00, 0.74; up at 4,000 rpm, down at 2,000) are a game engine's defaults, not a measured vehicle. | Read here |
| [PhysX 5 Vehicles](https://nvidia-omniverse.github.io/PhysX/physx/5.6.0/docs/Vehicles.html) (NVIDIA) | Open code; check the licence file before porting anything | The forward-facing model we are *not* using: an engine as a rotating body coupled to the wheels through a torsion clutch, integrated every physics step. Read it to see what quasi-static modelling leaves out. Its tuning guide is a good parameter checklist. | Read here |
| [Randomation Vehicle Physics](https://github.com/JustInvoke/Randomation-Vehicle-Physics) (JustInvoke) | Open code (C#, Unity), MIT | Gearbox and CVT code in C#, the closest language match. | Read here (README) |
| M. Monster, *Car Physics for Games*, v1.9, 2003 ([mirror](https://www.asawicki.info/Mirror/Car%20Physics%20for%20Games/Car%20Physics%20for%20Games.html)) | Tutorial; no licence stated, read only | The classic game tutorial: longitudinal forces (traction, drag, rolling resistance), torque curves, gear ratios and wheel speed. | Listed (mirror checked) |
| [FlyByWire A32NX systems](https://docs-5lkcdbdu4-flybywire.vercel.app/dev-corner/dev-guide/a32nx-repo-docs/systems) | Open code, GPL-3 (read only for us) | Architecture: a simulation model unaware of the simulator, run in read, execute and write phases; electrical buses powering named loads. The same split as our game-free model and thin comp. | Read here |
| Vanilla Vehicles Expanded, `CompVehicleMovementController` and its `CostToPayThisTick` postfix ([repository](https://github.com/Vanilla-Expanded/VanillaVehiclesExpanded)) | Mod code, no licence (read only) | How another mod gives Vehicle Framework vehicles a current speed with acceleration: it scales the path cost paid each tick by current speed over stat speed. Our speed cap must multiply with it cleanly. | Read here (1.7 source, 8 Nov 2026) |

## Gas turbines

Added with [gas turbines](vehicle-turbine-design.md).

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| [JSBSim `FGTurbine`](https://jsbsim-team.github.io/jsbsim/classJSBSim_1_1FGTurbine.html) and its [source](https://jsbsim.sourceforge.net/JSBSim/FGTurbine_8cpp_source.html) | Open code, LGPL (read only for us) | A game-grade turbine with explicit phases (off, run, spin-up, start, stall, seize). A ground start spins the core to about 25% N2 and needs 15% before fuel. Thrust rises with the square of normalised core speed. Temperature is a straight line against throttle. It also has water injection and time-limited augmentation. | Read here |
| Boeing CH-47, operator's manual TM 1-1520-240-10, engine section ([mirror](https://ch-47helicopters.tpub.com/TM-1-1520-240-10/css/TM-1-1520-240-10_38.htm)) | US Army manual (public mirror) | How a free-turbine helicopter engine is governed: the power turbine governor trims fuel to the gas producer so rotor speed stays nearly constant as collective pitch changes the load. | Read here |
| [Honeywell T55](https://aerospace.honeywell.com/en/learn/products/engines/t55-turboshaft-engine) | Manufacturer page | The Chinook's engine: from 1,600 shp at introduction to 5,000 shp today, with a 6,000 shp class next; output shaft speed 15,066 rpm. | Read here |
| All-Aero, "Bristol BE53 Pegasus / Rolls-Royce Pegasus / F402" ([page](https://all-aero.com/2024/01/10/bristol-be53-pegasus-vtol-rolls-royce-pegasus-f402)) | Secondary | The Pegasus's lift ratings, from 64.3 kN maximum continuous at 91% rpm to 95.9 kN 15 s wet at 107% rpm. The water tank (227 kg), its flow (about 35 imperial gallons a minute) and its duration (about 90 s). | Read here |
| [Rolls-Royce Pegasus](https://en.wikipedia.org/wiki/Rolls-Royce_Pegasus) (Wikipedia) and *Aircraft Engineering and Aerospace Technology* 42(1), 1970, "Powerplant: Water Injection System", [doi:10.1108/eb034594](https://doi.org/10.1108/eb034594) | Secondary; the paper is primary but paywalled | Take-off thrust limited by turbine blade temperature, worse at higher ambient temperatures. Water sprayed into the combustion chamber and turbine to keep blades cool. Replace All-Aero's figures with the paper's when it can be read. | Wikipedia read here (through mirrors); paper listed |
| [Honeywell AGT1500](https://aerospace.honeywell.com/us/en/products-and-services/products/power-and-propulsion/engines/gas-turbine-engines/agt1500-gas-turbine-engine.view-all) | Manufacturer page | The Abrams's turbine; the digital control unit's 20% lower idle fuel consumption. | Read here |
| [StrategyPage on Abrams fuel use (2003)](https://www.strategypage.com/htmw/htarm/articles/20031218.aspx) and [STRASAM, Gulf War and the M1A1 (part 7)](https://strasam.org/en/defense/land-weapons-and-systems/gulf-war-and-the-m1-a1-abrams-main-battle-tank-part-7-3630) | Defence journalism, secondary | Idle burn close to moving burn; 2.5 s from idle to full power; about 4.6 L/km at 40 km/h against a Leopard 2's 2.2. Orders of magnitude, not Def figures. | Read here |
| Ambient derate of industrial gas turbines ([Università di Bologna record](https://cris.unibo.it/handle/11585/54312)) | Paper abstract | About 0.5% to 0.9% less power per °C of ambient rise. Industrial machines; aircraft engines differ. | Read here (abstract) |
| "Engine Limitations Are Not Mere Suggestions, Part 1", [Aviation Week](https://aviationweek.com/business-aviation/aircraft-propulsion/engine-limitations-are-not-mere-suggestions-part-1) | Trade journalism | Hot starts from a weak battery; over-temperature damage and blade growth; wear from eroded compressor blades, worn seals and tip clearance. The failure modes for the turbine system's stages. | Read here |
| J. G. Leishman, *Principles of Helicopter Aerodynamics*, 2nd ed., Cambridge University Press 2006 | Textbook | Momentum theory for hover power (rising with weight to the 1.5 power and with lower air density), and the power curve against forward speed. | Listed (not opened here; edition from memory, check it) |

## Engine cooling and thermal networks

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| C. Baker et al., "Ambient and Initial Temperature Effects on BEV Range and Energy Consumption Rate Modeled in FASTSim", SAE WCX 2026, [2026-01-0277](https://saemobilus.sae.org/papers/ambient-initial-temperature-effects-battery-electric-vehicle-bev-range-energy-consumption-rate-modeled-fastsim-2026-01-0277) | Paper (NREL) | A lumped thermal-mass approach chosen to stay cheap: our philosophy, published. | Read here (abstract) |
| C. Baker, "Extending NREL's FASTSim to model catalyst transient warmup behavior", SAE 2022-01-0578 ([SAE](https://saemobilus-test.cld.sae.org/papers/extending-nrels-fastsim-model-catalyst-transient-warmup-behavior-2022-01-0578)) | Paper (NREL) | Cold-start warm-up with parameters scaled from engine peak power. A pattern for scaling from what a design already says. | Read here (abstract) |
| P. Shayler et al., "Progress on Modelling Engine Thermal Behaviour for VTMS Applications", SAE 971852, 1997, [doi:10.4271/971852](https://doi.org/10.4271/971852) | Paper | Engine thermal mass and coolant volume from a few key parameters and a template, solved by the lumped-capacity method. Close to how our loadouts infer by effect. | Read here (abstract) |
| "A Lumped Parameter Approach for Simulation of ICE Cooling Systems", SAE 2009-01-2760 ([SAE](https://saemobilus.sae.org/papers/a-lumped-parameter-approach-simulation-ice-cooling-systems-2009-01-2760)) | Paper | A lumped cooling circuit validated cold and hot across engine speeds. | Read here (abstract) |
| D. Caicedo Parra et al., "A Lumped-Parameter Thermal Model for System Level Simulations of Hybrid Vehicles", SAE 2020-01-0150 ([SAE](https://saemobilus.sae.org/content/2020-01-0150)) | Paper | Several cooling loops as resistances and capacitances, coefficients fitted from test data. A future multi-loop design (motor, battery, engine). | Read here (abstract) |
| [Modelica Standard Library](https://build.openmodelica.org/Documentation/Modelica.html) 4.1 (Modelica Association), with OpenModelica | Open models, 3-Clause BSD | `Thermal.HeatTransfer` and `Thermal.FluidHeatFlow`: reference formulations to cross-check our model offline. | Read here (index) |
| [CoolProp](https://coolprop.sourceforge.net/coolprop/wrappers/Csharp/index.html) and [SharpProp](https://www.nuget.org/packages/SharpProp) | Open code, MIT; native library | Glycol-water coolant properties (`INCOMP::MEG-50%`), to bake into rules curves offline. Never shipped. | Read here |

## Electrical

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| O. Tremblay and L.-A. Dessaint, "Experimental Validation of a Battery Dynamic Model for EV Applications", *World Electric Vehicle Journal* 3(2), 2009, pp. 289–298, [doi:10.3390/wevj3020289](https://doi.org/10.3390/wevj3020289) ([EVS24 copy](https://citelec.vub.ac.be/EVS24/files/proceedings/EVS-24-2230080.pdf)) | Open-access paper | A closed-form battery voltage model with a lead-acid form, parameterised from a manufacturer's discharge curve. Cheap enough for the rare tick; gives sag under starter current. | Read here (equations) |
| [PyBaMM](https://docs.pybamm.org/en/stable/source/api/models/index.html) | Open code, BSD | Lead-acid models and a Thevenin equivalent circuit, to generate capacity-against-temperature curves offline. | Read here (model index) |
| Delco Remy, *Charging Systems: Pulley Selection* ([PDF](https://www.newindo.com/uploads/kb/1779015519045-ChargingSystems-PulleySelection.pdf)) | Manufacturer guide | Choose a pulley ratio so the alternator carries half the electrical load at engine idle; rotor rpm is engine rpm times the ratio (their example: 2.5 to 1). Grounds the alternator-against-rpm curve. | Read here |
| E-Z-GO 1600XD repair manual, alternator section ([ManualsLib](https://www.manualslib.com/manual/1501087/Ezgo-1600xd.html?page=367)) | Product manual | A worked example of a cut-in speed (no output below 1,200 rotor rpm) and a 2.16 pulley ratio. One product, not a general figure. | Read here |
| [Battery University BU-502](https://batteryuniversity.com/article/bu-502-discharging-at-high-and-low-temperatures) | Educational site (Cadex) | Capacity against temperature; already cited in the systems design. | Cited in the systems design |

## Boats

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| The propeller law (for a fixed-pitch propeller, absorbed power rises with the cube of shaft speed), as taught in marine engineering ([explainer](https://www.merchantnavydecoded.com/?p=93234), low quality) | Engineering rule, secondary | One curve gives a boat's engine load against speed with no gearbox. | Read here; **replace with a naval-architecture text** (J. B. Woodward, *Matching Engine and Propeller*, University of Michigan Dept. of Naval Architecture, 1973, seen only as a third-party copy: unverified) |

## Numerics and game loops

| Reference | Kind and terms | What to take | Status |
| --- | --- | --- | --- |
| G. Fiedler, ["Fix Your Timestep!"](https://gafferongames.com/post/fix_your_timestep/) | Article | Why a simulation misbehaves when handed a step larger than it can take, and how to subdivide. Our rare tick is that large step. The systems design's closed-form updates are the other remedy. | Read here |
| [Map scale research](map-scale-research.md) (ours, 19 Oct 2026) | Our record | The two clocks: the action clock (60 ticks a second; 1 m per cell from how things look) and the day clock (2,500 ticks an hour; 26 m per cell from walking speed). The drivetrain design uses both and says where. | Ours |

## Sources

Every source is linked in its row. Checked on 8 Nov 2026 through web search and the pages themselves. The Vanilla
Vehicles Expanded source was read from its GitHub repository's 1.7 folder that day.
