# Sling loads: a helicopter carries a vehicle under it (11 Nov 2026)

Design record. **Nothing here is built or tested in game yet.** The owner asked for a vehicle airlift of our own, "with our own flavour on
it", after seeing Yaroslav's [Cherokee Sling — Vehicle Airlift](https://steamcommunity.com/sharedfiles/filedetails/?id=3836765380)
(Workshop, updated 9 Nov 2026). That mod's published source was read on 11 Nov 2026 only to learn how such a mod meets Vehicle Framework's
flight; nothing of it is copied or adapted, and ours is designed from the questions below.

## Read this first

- **What it is:** any helicopter given a **cargo hook** (a fitting, in the fittings window) can lift one vehicle on a sling, fly it on the
  world map and set it down at the far end, with the vehicle's passengers, cargo and fuel aboard.
- **Our flavour (owner's answers, 11 Nov 2026):** real weights against the aircraft's rated hook load, less lift on hot days, a colonist on
  the ground hooks the load up, weather can ground a lift, a slung load flies slower and burns more, and an emergency release drops it.
  **Each of these is a setting the player can switch off** (owner: "configurable settings ... so the player can turn on/off").
- **Pictures:** about five small icons from the OpenAI image API, in the same family as today's (owner's choice); the slung vehicle is drawn
  from its own picture with the strops drawn in code, so it needs no art.
- **Another author's mod:** when Cherokee Sling is active, ours **stands down on the Cherokee** (no cargo hook offered there), so the two
  never offer two airlifts on one aircraft and that author's vision for the Cherokee is left alone (agent default, from the owner's rule of
  11 Nov 2026 about other authors' art and vision). Our hook still works on every other helicopter.

## What the player does

1. **Fit a cargo hook** to a helicopter in the fittings window (materials and a mechanic, as any fitting).
2. **Make a sling set** at the vehicle parts bench and load it aboard the helicopter: strops, a swivel and a spreader, kept, never used up
   (agent default; it is what a lift needs and what a caravan must carry, like the jump-start kit).
3. With the helicopter ready to take off, press **Lift a vehicle** and pick a vehicle parked outdoors on the map. The button says why not
   when it cannot: no hook, no sling set, too heavy today, too windy, or no one to hook up.
4. **A colonist hooks up**: they walk to the vehicle, and the helicopter hovers over it while they fit the strops (a short job; the lift
   waits for it). With the ground crew setting off, the helicopter hooks up by itself.
5. **Choose a destination** on the world map, as for any flight; the planned range accounts for the load.
6. **At the destination**, place where the load is set down, then where the helicopter lands. The crew aboard release the hook.
7. **Emergency release** drops the load where it hangs. On a map it lands hard there and is damaged; an engine failure in the air drops it
   by itself.

## Lets, stops, changes

- **Lets the player:** move a vehicle where no road goes, over water and mountains; reach a second colony with a truck; get a broken vehicle
  home from far away; drop an armoured car into a fight.
- **Stops them:** lifting more than the aircraft can carry, lifting in storms, lifting without a sling set or a hook-up crew (each a setting).
- **Changes how they play:** heavy vehicles stay land vehicles; a light truck or a buggy becomes air-mobile; weather and the time of day
  (heat) become part of planning a lift; a helicopter fleet gains a real job.

## The rules

| Rule | What we do | Source or label |
| --- | --- | --- |
| **The load's weight** | The vehicle's real kerb mass (our drivetrain model's figure, the same the drive and the repair costs use) plus what it carries (its cargo and passengers, as Vehicle Framework's cargo tab counts them) | Kerb masses as recorded in `utility-vehicles-design.md` |
| **What a hook takes** | Each helicopter's **rated hook load**, in its flight profile or loadout, in kilograms | Named per aircraft below |
| **The Cherokee** | 11,800 kg, the CH-47F Chinook's centre hook (26,000 lb), the Cherokee being VVE's twin-rotor cargo helicopter | US Army CH-47 fact sheet, republished by the [127th Wing, Air National Guard](https://www.127wg.ang.af.mil/About-127th-Wing/Fact-Sheets/Article/3694458/ch-47-chinook/) |
| **A medium helicopter** (for other mods' helicopters of that class) | 4,080 kg, the UH-60M Black Hawk's external load (9,000 lb) | [Lockheed Martin, UH-60M](https://lockheedmartin.com/en-us/products/sikorsky-uh-60m-black-hawk-helicopter.html) |
| **A hot day** | The hook load falls 1% for each °C of outdoor temperature above 15 °C, to at most 30% less (**authored**). Real helicopters lose lift and engine power as heat thins the air: the FAA defines density altitude and says air density falls with rising temperature ([FAA Helicopter Flying Handbook, glossary](https://www.faa.gov/sites/faa.gov/files/regulations_policies/handbooks_manuals/aviation/helicopter_flying_handbook/hfh_gloss_index.pdf); [chapter 7](https://www.faa.gov/sites/faa.gov/files/regulations_policies/handbooks_manuals/aviation/helicopter_flying_handbook/hfh_ch07.pdf) names density altitude as a major factor). The CH-47F fact sheet quotes 16,000 lb of payload in high and hot conditions against its 26,000 lb hook, which our cap is gentler than. | Authored, from the direction those sources give |
| **Weather** | No lift while the map's wind is strong or a storm is on (thresholds **authored**, named in the rules Def) | Authored |
| **The slung flight** | Cruise speed ×0.7 and fuel per tile ×1.3 while a load hangs (**authored**: an external load's drag slows a helicopter and costs fuel; real figures vary with the load's shape) | Authored |
| **A dropped load** | Damage on landing from the height it fell, spread over its parts as a heavy impact (**authored** figures in the rules Def) | Authored |

Figures the table calls authored live in a rules Def (`PhobosSlingRulesDef`), so they can be tuned in play without code.

## Settings (all on by default; the owner's request)

| Setting | Off means |
| --- | --- |
| Hot days lift less | The rated hook load whatever the weather's temperature |
| A ground crew hooks up | The helicopter hooks up by itself |
| Weather can ground a lift | Lifts in any weather |
| Slung loads fly slower and burn more | A slung flight costs what an empty one does |
| Emergency release and drops | No release button, and an engine failure keeps the load |

A sling set aboard is part of the rules, not a setting (agent default); the owner may ask for a setting for it too.

## How it meets Vehicle Framework (to read before building)

The other mod showed that a sling can ride on Vehicle Framework's own launch: the load leaves the map held inside the aircraft, and an
arrival action of the aircraft's places it again. We will read Vehicle Framework's launch, flight and arrival code with
`scripts/query-code.py` before building and record the members we use; any reflected name goes through `scripts/check-targets.py`. Our comps on
the carried vehicle already catch up the hours they were away when it is spawned again (the engine has cooled, the battery has rested).

## Slices

1. **Lift and carry**: the hook fitting, the sling set, the weight rule with the hot-day derating, the Lift button with its reasons, the
   hook-up job, the load carried through the flight, and set-down at the destination. Settings for derating and the ground crew.
2. **Weather, flight and release**: the weather limit, the slower and thirstier slung flight (and the range drawn for it), emergency
   release on a map, the drop on engine failure. Their settings.
3. **Pictures and sounds**: the five icons (OpenAI, the owner's choice), and a hook latch sound through the sound pipeline.

## Open questions

- **A load dropped on the world map** (an engine failure in flight): a wreck site the player can recover from, using Vehicle Framework's
  crash site, or the load is lost. Agent lean: a wreck site, so the loss becomes a recovery mission ("a lost thing is future content").
- **A sling set's cost**: the agent will author it from real strops (polyester round slings and a steel swivel) and say so.
- **Other mods' helicopters**: each needs a flight profile and a hook rating. The Cherokee comes first; others as the owner names them.

## Who chose what

- **Owner (11 Nov 2026):** our own airlift with our own flavour; any helicopter, by fitting; real masses with heat derating; ground crew,
  weather limits, slower and thirstier slung flight, emergency release; all of them as settings; pictures from the OpenAI API.
- **Agent defaults (for the owner to revise):** the sling set as an item kept aboard; the authored figures in the table; standing down on
  the Cherokee when Cherokee Sling is active; the order of the slices; a wreck site for a load dropped in flight (open).
