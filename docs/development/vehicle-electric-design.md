# Electric vehicles: design (9 Nov 2026)

A design record, built the same day in D'thorga Framework 0.20.0 and D'thorga Vehicles 0.9.0 (Drafts). **Not yet tested in game.**

## Read this first

- **None of the owner's 72 vehicles is electric** (read with `query-defs`, 9 Nov 2026). The owner chose **the framework's systems plus
  a test vehicle**: Vanilla Vehicles Expanded's Lightning becomes an electric van while the setting **"The Lightning runs on
  batteries"** is on (off by default; needs a restart). The owner added that **one or more of our own utility vehicles will be
  diesel-electric**, for mechanics of their own (outstanding-work.md, V3).
- **Vehicle Framework already runs electric vehicles**: their fuel is the charge; they plug into any powered building in or beside
  their cells and draw from the grid's stored energy, 1 Wd per unit of charge (read in its 1.6 assembly). Nothing models the battery
  itself. These systems add its temperature, wear and draw, and a motor that heats.
- **Battery fires are a setting, off by default with a warning**, as boiler explosions are (agent default, owner to revise).

## What an electric vehicle gets

Any land vehicle Vehicle Framework runs on electricity, matched by that, never by name (`Loadouts_Electric.xml`):

| System | Quantities | What goes wrong | What fixes it |
| --- | --- | --- | --- |
| Traction battery | Pack temperature (on the Dashboard); battery health (what is left, on the Dashboard) | Frozen below -10: 70% power, charging at 15%; cold to 5: 90% power, 40% charging; warm above 40: 85% power, half charging; overheating above 55: half power, no charging, damage; thermal runaway above 85: will not start, more damage, and fire with the setting. A worn pack holds less charge (its health is its capacity); failing below 40%: 80% power | The thermal management holds it near 25 °C while switched on or charging; park it warm; **new battery modules** (10 components, 20 steel) |
| Electric drive | Motor temperature | Hot above 120: 80% power; overheating above 150: half power and damage. A damaged motor heats three times as much | Ease off; repair the motor |
| Low-voltage electrics | 12 V charge | As a combustion vehicle's: a flat 12 V battery will not wake the vehicle; jump-starts work | The DC-DC converter charges it whenever the vehicle is on, at any speed |
| Tyres and brakes | As combustion vehicles | Brakes wear a third as fast (regenerative braking) | As before |
| Cab | As combustion vehicles | The heater and air conditioner draw on the traction battery: winter and summer cost range | Switch them off to save charge |
| Service | Motor hours | Breakdowns as before | A motor service: one component, no chemfuel |

No coolant, oil, gearbox, fuel-in-the-cold or night heater (the night heater's fuel is in chemfuel units; a later version can give
electric vehicles their own).

## Range and draw

Vehicle Framework's own consumption stays (the vehicle author's figure). On top, as shares of capacity an hour so they work whatever a
mod's units of charge are (`Effect_ConsumeFuel ofCapacity`): the pack heater 4% while below 15 °C and on or charging; the cab
heater 3%; the air conditioner 2% (all authored; real winter range losses of 10 to 40% are commonly reported, general). The load-
dependent fuel setting does not apply to electric vehicles (the drivetrain model already said so).

## Charging

`ChargeRatePatch` scales Vehicle Framework's `CompFueledTravel.ChargeRate` getter (a protected property: no extension point;
postfix) by the product of the stages' `Effect_ChargeFactor`s. Vehicle Framework draws that rate from the grid and adds the same to
the vehicle, so both sides stay equal. Charging below freezing also wears the pack (lithium plating, general).

## The test vehicle

`PhobosVehicles/Mods/VanillaVehiclesExpandedTier3/Patches/Figures_VanillaVehiclesExpandedTier3.xml`, through the Framework core's
`PatchOperationPhobosFlag` (a patch that applies only while a registered setting flag is on; the vehicle part registers
`PhobosVehicleSystems.electricLightning` in its Mod constructor, which the game runs before any patch). All authored: 3,000 units of
charge at 1 Wd each, about 72 kWh (a large van's pack, general); 1,369 a day for the same range as its 80 chemfuel at 36.5 a day;
charging at 14 Wd every 120 ticks, about 7 kW, so about 10 hours from empty and about five RimWorld batteries' worth from the grid;
2,300 kg empty (500 kg of pack). Its description is not changed (that would copy its author's text); Vehicle Framework's fuel gizmo
says "electricity". Its gearbox and chemfuel tank parts stay (parts may only be appended).

## Figures and sources

Every figure is in `Systems_Electric.xml` and `Loadouts_Electric.xml` with its label. General knowledge, approximate: lithium-ion
cells work best at about 15 to 35 °C, lose power when cold, and should not be charged fast below freezing; packs are heavy and slow to
change temperature; electric cars keep a 12 V battery for their computers and contactors; their brake pads last two to three times
as long. Everything else is authored. The rule tests in `tests/PhobosFramework.Vehicles.Rules.Tests/ElectricRuleTests.cs` pin the
pack's temperature behaviour.

## To test in game (owner)

1. Turn on **"The Lightning runs on batteries"**, restart, build or spawn a Lightning: its fuel gizmo says electricity; the Dashboard
   shows the traction battery and battery health.
2. Park it on a conduit with charged batteries on the grid: it charges (dev "log charging": charging True, charge factor 1 at room
   temperature).
3. Dev "set a quantity" on the pack temperature low (frozen): charging slows (factor 0.15), the inspect line says frozen, it drives
   slower.
4. Drive it with the cab heater on in the cold: charge falls faster than with it off.
5. With **Battery fires** on, set the pack temperature very high: it bursts into flame once; with the setting off, it only overheats.

## Who chose what

- **Owner (9 Nov 2026):** framework systems plus a test vehicle; our own diesel-electric utility vehicles later.
- **Agent choices, owner to revise:** every figure; battery fires as an off-by-default setting; the Lightning as the test vehicle
  and the conversion off by default; no night heater for electric vehicles yet.
