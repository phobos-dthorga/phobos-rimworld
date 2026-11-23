# Decisions: medical equipment

**Status (23 Nov 2026):** living; harvested from the records' "Who chose what", owner's decisions and answered questions. One row
a decision. How it works now: [the system page](../systems/medical-equipment.md). Dates are 2026. **Who**: owner; owner's card (a
decision card answered); agent default or agent choice (the owner may revise either). A later row that overturns an earlier one
says so. Decisions shared with the gas layer and supplied oxygen are in [gases and air](gases-and-air.md); those on lapsing
treatments and the chamber's surgery bar in [the health layer](health-layer.md).

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 4 Nov | We may add our own medical equipment, for hospital and field | owner | Nothing treats a patient yet | [me], The first design's medical half |
| 4 Nov | Vanilla Expanded Framework a required dependency | owner | Its years of track record | [me], Decisions |
| 5 Nov | Our own oxygen: a concentrator, an electrolyser, a liquid store | owner | Electrolysis for the chemistry | [me], The first design's medical half |
| 5 Nov | The frost on the liquid store, not the concentrator | agent choice | A concentrator runs at room temperature | [me], The first design's medical half |
| 5 Nov | Equipment its own mod (merged into D'thorga Medical, 7 Nov) | agent choice | Kept apart from Burns and Smoke | [me], Decisions |
| 5 Nov | A patient in a linked bed who needs oxygen gets it, with no job | owner | As the IV stand and vitals monitor | [me], Decisions |
| 5 Nov | A concentrator as well as the stand | owner | Works without Vanilla Gravship Expanded | [me], Decisions |
| 5 Nov | No Questionable Ethics; a ventilator of our own later | owner | Its version is done poorly | [me], Decisions |
| 5 Nov | Beds found by accepting a vitals monitor, never by name | agent choice | Modded hospital beds qualify | [me], Decisions |
| 5 Nov | The bedside stand exists only with Vanilla Gravship Expanded | agent choice | Never waits for an item that cannot exist | [me], Decisions |
| 5 Nov | A burn unit bed on the hospital bed; infection cut via tend quality | agent choice | The owner's go; no patch needed | [me], Decisions |
| 5 Nov | The stand uses a picked generated image; concentrator a placeholder | owner | Art as it came | [me], Decisions |
| 6 Nov | A machine names an occupant it leaves alone, and why | agent choice | A correct hold-back read as a fault | [me], Decisions |
| 7 Nov | Concentrator and stand use generated images picked by the owner | owner | Shipped in 0.2.3 | [me], Decisions |
| 7 Nov | Build the chamber, electrolyser, liquid store, field kit, dressings, pump | owner | The outstanding pieces | [me], Decisions |
| 7 Nov | Everything that stores or pipes oxygen joins Vanilla Gravship Expanded's network | agent choice | No second network or pipe art | [me], Decisions |
| 7 Nov | One oxygen network, Vanilla Gravship Expanded's | owner | Confirms the agent's choice | [me], Decisions |
| 7 Nov | The chamber a bed, 1 by 3 | agent choice | A bed sleeps one per tile of width | [me], Decisions |
| 7 Nov | The burn dressing a medicine, not burn-only | agent choice | Infection has no patient-side modifier | [me], Decisions |
| 7 Nov | Field kit and hand pump one Doctor job, driven by a Def extension | agent choice | Other mods add devices in XML | [me], Decisions |
| 7 Nov | The device job lives in Medical until a second mod needs it | agent choice | No shared need yet | [me], Decisions |
| 7 Nov | Treatments of the gravship-only pieces defined with the core Defs | agent choice | A save loads without that mod | [me], Decisions |
| 7 Nov | The electrolyser costs what real electrolysis costs (3,000 W) | owner | Confirmed the agent's choice; vacuum is its place | [me], Decisions |
| 7 Nov | Vanilla Gravship Expanded optional but recommended by a one-time letter | owner | It needs Odyssey; the letter an exception | [me], Decisions |
| 7 Nov | With Dubs Bad Hygiene the electrolyser draws plumbing water | owner | Water assumed without it | [me], Decisions |
| 7 Nov | An unpowered liquid store stops filling, still gives out, boils off | owner | Its liquefier is off | [me], Decisions |
| 7 Nov | The store's frost waits for its art; the code follows | owner | Art first | [me], Decisions |
| 7 Nov | Build the second set, all eight pieces | owner | From the art backlog's proposals | [me], Decisions |
| 7 Nov | Order: ventilator, shower, alarm, trolley, nebuliser, anaesthesia, needle, steriliser | owner | The owner's order | [m2], Read this first |
| 7 Nov | Each piece ships with a scripted placeholder and an art request | agent default | Logic never waits for art | [m2], Read this first |
| 7 Nov | Intubation a doctor job, not a surgery bill | owner | Answered question | [m2], Open questions |
| 7 Nov | Intubation through the device system with a kit; Medicine 4 | agent choice | One job for every device | [m2], Built |
| 7 Nov | A tube when an airway is closing or a ventilator waits; works at zero | agent choice | Getting past a shut airway is its point | [m2], Built |
| 7 Nov | The tube stops talking, lasts two days, removes the bypassed airway | agent choice | Past the airway burn's 48 hours | [m2], Built |
| 7 Nov | Ventilator: 200 W, one patient, six steps of 0.1 to a floor of 0.6 | agent choice | Authored; nothing at zero | [m2], Built |
| 7 Nov | Acid on the skin keeps burning about three hours until rinsed | agent choice | A rinse needs something to stop | [m2], Built |
| 7 Nov | Rinse water: 75.7 L/min shower, 1.5 eyewash, 15 minutes | agent choice | ANSI/ISEA Z358.1, via ISHN | [m2], Built |
| 7 Nov | Plumbing water through the Framework's reader; dry for an hour on a failure | agent choice | Nobody keeps running to it | [m2], Built |
| 7 Nov | Walkers rinse within 60 cells; patients by Doctor work at 92 | agent choice | First aid where it is needed | [m2], Built |
| 7 Nov | The safety shower needs no research | agent choice | Life-saving first aid | [m2], Built |
| 7 Nov | After a rinse, cool water eases burn pain (×0.85) for about four hours | agent choice | Authored | [m2], Built |
| 7 Nov | Two alarms: carbon monoxide and smoke now, hydrogen with the vents | owner | As in Ostranauts | [m2], Built |
| 7 Nov | Alarm reads smoke and carbon monoxide at its cell; 0.05 and 0.1, quiet at half | agent choice | The game's fractions; authored | [m2], Built |
| 7 Nov | The alarm wakes everyone asleep in its room | owner | Owner's choice | [m2], Built |
| 7 Nov | The alarm silent; message and inspect line carry it (overturned 11 Nov) | agent choice | Nothing alarming | [m2], Built |
| 7 Nov | The alarm a ceiling fixture, 5 W, Electricity research | agent choice | Shares a cell with furniture | [m2], Built |
| 7 Nov | The trolley needs no bed: serves anyone lying beside it | agent choice | The stand was already movable | [m2], Built |
| 7 Nov | Trolley: one patient, 40 canisters, quick to pick up | agent choice | Authored | [m2], Built |
| 7 Nov | Cylinders in the network's colour, not a real colour code | agent choice | Australian code unverified | [m2], Built |
| 7 Nov | The nebuliser's drug its own vial, four from one neutroamine | agent choice | Nothing in the game opens airways | [m2], Built |
| 7 Nov | Nebuliser by a tag on the condition; Combat Extended's smoke from 0.1 | agent choice | Any mod's condition joins | [m2], Built |
| 7 Nov | Nebuliser: +0.15 breathing, a vial per four hours, 60 W | agent choice | Authored | [m2], Built |
| 7 Nov | Nebuliser and anaesthesia built in Medical, not in dosing | agent choice | Own vial; no drug for anaesthesia | [m2], Open questions |
| 7 Nov | Anaesthesia machine: +0.08 surgery, 150 W, no drug | agent choice | The operation gives the anaesthetic | [m2], Built |
| 7 Nov | Needle for More Injuries' collapsed lung: Medicine 5, 0.5 off, a trace left | agent choice | Buys time; operation still offered | [m2], Built |
| 7 Nov | Steriliser: Cleanliness 4, unpowered | agent choice | The game's room stats do the rest | [m2], Built |
| 11 Nov | The alarm plays a soft tone once (overturns 7 Nov) | owner | Never alarming | [sc], Medical |
| 11 Nov | No steriliser sound for now | owner | It never ticks | [sc], Medical |
| 12 Nov | The store's frost drawn in code from its outline | agent choice | Neither one overlay nor frames | [me], The first design's medical half |
| 13 Nov | Take SD22 (Search and Rescue's medics) and SD23 (the IV drip) | owner | The medical picks | [sd], Questions for the owner |
| 16 Nov | Build SD22 next, then the aircraft batch | owner | Left out of the last batch | [sd], Questions for the owner |
| 16 Nov | SD22: two lanes of our device work in its field rescue; no Harmony | agent choice | Its treatments cannot be joined | [sd], Questions for the owner |
| 16 Nov | Lanes at 121 (airway devices) and 106 (oxygen, fluids) | agent choice | Around its own treatment | [sd], Questions for the owner |
| 16 Nov | A lane asks for Doctor work on, as its own lanes do | agent choice | Same rule as its responders | [sd], Questions for the owner |
| 16 Nov | Its rescue ending at a vehicle's litter set aside; a request drafted | agent choice | Its planner is hard-coded | [sd], Questions for the owner |

[me]: ../archive/medical-equipment.md
[m2]: ../archive/medical-equipment-2.md
[sd]: ../soft-dependency-crosslinks.md
[sc]: ../sound-catalogue.md
