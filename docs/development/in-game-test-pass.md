# In-game test pass (8 Nov 2026, refreshed 9 Nov 2026)

A short list of the checks that matter most among what is built and not yet tested in game, for the owner to run in their colony
(owner's request of 8 November 2026). The most consequential come first. Nothing here counts as tested until the owner says so; each
result goes into that mod's changelog.

## Read this first

- **Framework 0.36.0 or later and Patches 0.16.0 are enough for checks 1 to 6.** Install only through `python scripts/deploy.py --build
  --apply --all`, with RimWorld closed, and close any Explorer window open in a mod folder.
- **After any update, first load an existing save and look at the map and a vehicle** (added 9 Nov 2026). Framework 0.40.0 and 0.40.1 left every vehicle unable to spawn on a save made before them, and the map black behind the interface; unit tests cannot see that. **If the map is black or vehicles are missing, quit without saving.**
- **The quick way (Framework 0.42.0):** with dev mode on, the dev action **"Vehicle systems: run the self-test"** reads every vehicle you have, then runs fittings, tunes, a destroyed part's replacement and grades on a throwaway vehicle, and opens a pass or fail report. It touches nothing in your colony. It proves the code runs and the right figures move; it does not replace looking at it in play. **First run by the owner, 9 Nov 2026: 76 passed, 0 failed, 0 skipped.** It works through the same service calls as a mechanic's job, so the haulers, the job itself and the window are still for checks 1, 8 and 9. **Every load in dev mode also checks your vehicles by itself** and tells you if one failed.
- **Use a copy of the save for check 1:** it destroys parts on purpose.
- **Dev mode is needed for checks 1, 2 and 5** (Options, then Development mode). The dev actions are in the D'thorga group.
- **After the session, say "done"**, or tell whichever session you are in: the log and save can be read with `python scripts/triage-log.py`
  and `python scripts/query-save.py`. Say anything that looked wrong, even if it seems small.

## The ones that matter

| # | What it proves | What to do | What passes |
| --- | --- | --- | --- |
| 1 | **A destroyed part needs replacing** (Framework 0.36.0). This changes repairs on every vehicle you own, so it is the riskiest new thing | Select a vehicle. **"Vehicle systems: hit a part"**, choose **Radiator**, twice, until it reads 0%. Open **Health**. Then leave the vehicle parked with steel and components stored nearby, and give a mechanic time | The Health tab says it is destroyed and what it needs. Haulers bring the supplies, a mechanic fits it, a message says so, and the part returns at 25%. **Ordinary repair then finishes it for free. Watch for a mechanic walking back and forth doing nothing** |
| 2 | **Part conditions** (Framework 0.29.0): a hit radiator stays holed | Same dev action on a healthy radiator, **once**. Look under the radiator on **Health**. Press **Patch leaks** on the vehicle. Then let a mechanic repair it | **"Holed: leaking"** under the radiator with a clock line ("Coolant level ... in about ... hours"). After the patch it reads "patched in the field" and leaks more slowly. A repair to full clears it |
| 3 | **Temperatures** (Framework 0.29.1) | Run the Abrams for an in-game hour or two, driving some of it. Fly the Cherokee, then shut it down | Oil climbs to 55 degrees C or more, the gearbox warms well above the air, and a stopped turbine cools gradually |
| 4 | **Repairs in a caravan** (Framework 0.36.0) | Form a caravan with a damaged vehicle carrying supplies, and stop it | A **Repair** button appears per vehicle needing it, says why when it cannot, and works at a halt |
| 5 | **The game's log comes back after its limit** (Framework 0.28.0) | **"Framework: log probe T2, one line through our category"**, then **"Framework: log probe T1/T4, flood past the game's limit"**, then **"Framework: game log status"** | The game pauses for a moment, the in-game log window keeps taking new messages, and the status says it revived once. **Say whether the T2 line showed in the in-game log window: the logging design waits on that** |
| 6 | **The Health tab on the world map and the shared logger** (Framework 0.26.0, 0.33.0) | Open **Health** on a caravan's vehicle. Launch once and look at the load lines | The tab draws with no red error. Player.log shows lines as [D'thorga Framework] and [D'thorga Patches: Harrier], not [Verse] |

**The Visit Settlements fix (Patches 0.14.1) looks good from the log of 8 November:** its counter fired and reset with no error. Say so and it is marked tested.

## If play brings them up

| # | What | When it comes up | What passes |
| --- | --- | --- | --- |
| 4 | The Harrier over a RimPacts battle (Patches 0.11.0) | A RimPacts client state is under attack | Press **Join** first; the jet then flies sorties. Without joining, the jet's button says why it cannot go |
| 5 | Caustic gas (Medical 0.8.0: slow-acting, eye scarring, wilting plants, rinsing) | A tox gas event, or a tox weapon in a fight | Exposed pawns worsen slowly rather than at once; a pawn who rinses at a shower or eyewash improves. Rinsing at Dubs Bad Hygiene's showers is the open question |
| 6 | Machining bays reading every stack in a deep factory hopper (Factory Additions 0.11.4) | A bay waits for an ingredient that a deep input hopper holds | The bay takes it from any stack in the hopper, not only the first |
| 7 | Vehicle systems on the world map: climate control by era and bleed air (Framework 0.22.0 and 0.24.0, Vehicles 0.10.0) | A caravan travels with a vehicle in heat or cold | The cab's temperature stays comfortable, and the Systems tab shows what is running |

| 8 | Fittings and tunes (Framework 0.40.0) | Open **Fittings and tunes** from a vehicle's Dashboard. Order a turbocharger and a power tune, with the materials to hand and a mechanic of Construction 8 | The materials are hauled, the work is done, the Health tab says Fitted; a lower-skilled mechanic is refused and told why |
| 9 | Part grades (Framework 0.41.0) | **After check 1.** On a working part, press **Grade** and choose heavy-duty; on a part destroyed with the dev action, choose salvaged before it is replaced. Save and load | The part's health on the Health tab rises by about 30% or falls by about 30%, and is the same after the load |
| 10 | Stations (Framework 0.43.0) | Open the **motor pool** tab and build a **vehicle parts bench** and a **tuning rig**. Order a tune on a vehicle parked away from the rig, then drive it within the outline | The bench lists only vehicle parts; the machining table lists none of ours; the tune waits and says why, then a mechanic does it once the vehicle is within reach |
| 11 | Generator set (Framework 0.44.0, Vehicles 0.17.0) | Build one from the **motor pool** tab (dev mode's instant build is fine). Park it, fuel it, press **Set up**, and wire a lamp to the cable end. Then press **Pack up** | The cable end appears and shows on the power overlay; the lamp lights; the truck cannot be driven, but since Framework 0.54.0 its engine can be started; the fuel falls slowly; packing up removes the cable end and the lamp goes out |
| 12 | Depot bay (Framework 0.45.0) | Build a **depot bay** from the **motor pool** tab, power it and fill its tank. Press **Assign vehicle** and pick one. With a driver aboard, select the vehicle and press **Go to bay** | It drives to the outlined pad and switches off; its fuel rises with nobody attending; a repair done there is quicker |
| 13 | Survey truck (Framework 0.46.0, Vehicles 0.18.0; its art and swinging boom Framework 0.51.0, Vehicles 0.23.0: **the boom should swing out over two seconds about the bracket on the deck, facing every way, and fold back on Pack up**) | Build one from the **motor pool** tab (dev mode's instant build is fine). Park it outdoors, fuel it, press **Set up**, and have a colonist with Research work enabled. The dev gizmo **Find now** skips the wait | A colonist stands by the truck and surveys; the engine idles and fuel falls slowly; a find sends a letter within a day (about 7 hours on average since Framework 0.49.1) and, with the truck selected, the ore shows on the ground. A second find takes longer. **Passed: the owner, 9 Nov 2026, on Framework 0.51.0 and Vehicles 0.23.0: the truck and its boom work well (session log kept in .local/owner-test-logs). Upgrades not reported on separately** |
| 14 | Drill rig (Framework 0.46.0, Vehicles 0.18.0) | Park the rig so the cell behind its rear is over that ore (the ring shows reach when set up). Press **Set up**, with a colonist on Mining work | A colonist drills at the rear; portions land beside the hole, one every 20 to 30 minutes of game time at Mining 8 (ten times a deep drill since Framework 0.49.1); fuel falls; when the ore is gone it says so and brings up stone |
| 15 | Deepchem head (Framework 0.47.0, Vehicles 0.19.0, with Vanilla Chemfuel Expanded) | Fit **deepchem head** to the rig and set it up over a deepchem lump. Park a tanker with a deepchem tank alongside, set up and loading | The rig's buffer fills without any items on the ground, then the tanker's level rises and the buffer falls; or lay a deepchem pipe to the rig and it empties into that |
| 16 | Oil and groundwater (Framework 0.48.0, Vehicles 0.20.0, with Rimefeller or Dubs Bad Hygiene) | **Rimefeller:** fit the **oil well kit**, load steel in the cargo, and set up over oil with a resource console running. **Hygiene:** fit the **groundwater sensor**, set the truck up and select it | The rig counts lengths of casing and leaves a well head a derrick can be built over. A set-up survey truck also lets you place a deep well. The water shows on the ground, with the richest spot marked |
| 17 | Tipper truck (Framework 0.49.0, Vehicles 0.21.0) | Build one from the **motor pool** tab, load it with chunks and steel through its cargo window, and with a driver aboard, stopped, select it: the outline shows where the load lands. Press **Tip** | The load slides out in a fan behind the truck, it cannot be moved meanwhile, and a message says how many stacks came down. Tip with the outline full of walls: it says the ground is full and how many stay aboard |
| 18 | Overdrive (Framework 0.50.0) | On a set-up rig or survey truck, press **Overdrive** and let a colonist work it | The pace doubles, fuel falls faster, the working parts' health falls on the Health tab, and it drops out by itself with a message when one reaches half |
| 19 | Harsher damage (Framework 0.53.0) | With dev mode, **"Vehicle systems: hit a part"** on a healthy engine, then look at **Health** and the oil level. Run it until the oil pressure goes | The engine shows a cracked sump with a clock line, the oil falls on its own, and with no pressure the engine loses health quickly and a message says so. On a Traveller, Bang Bus or Highwayman the row reads plain **Engine** |
| 20 | Engine while set up (Framework 0.54.0) | Set up the survey truck, put a driver aboard, press the **engine key** on the Dashboard, then right-click the map and press **Go to bay** if it has one | The engine runs, the truck will not take a move order and says to pack up, and Overdrive still heats it |
| 21 | **Pipes beneath a parked vehicle** (Framework 0.57.0, needs Dubs Bad Hygiene). The newest and riskiest piece: **use a copy of the save** | Park a vehicle with a plumbing pipe ending at one side and another run starting at the far side. Wait about half a minute. Open the plumbing overlay. Then drive it away | The overlay shows pipe under the vehicle and the two runs join; a coolant top-up can draw water; driving off removes them and the runs part. **Watch for the vehicle refusing to move, or any red error** |

## Not on this list, on purpose

- The IRBM strike model (Patches 0.8.0 and 0.9.0) needs a strategic warhead to land: run it when you choose to; its bug report
  waits for that test ([outstanding work](outstanding-work.md), item J).
- The probes T3 and T5 (stream counts, timing) are for measuring later, not for this pass.

## The bulk tanker (Framework 0.34.0, Vehicles 0.12.0)

Three checks, on a copy of a save, after installing with RimWorld closed.

1. **Build one, then fit the general liquid tank.** Does the truck look right, with the tank on its deck in every facing? Does the Upgrades tab show the tanks, and refuse a second one?
2. **Choose chemfuel, park beside chemfuel stacks, Set up, then Load.** Does the tank fill, does the hose appear, and is the truck refused a move while set up? Then Pack up and use Unload to refuel a parked vehicle.
3. **Change contents.** Empty it, try to choose something else (it should say to purge), then right-click the truck with a colonist to purge it.
4. **Fit two compartments.** Is there a Compartment button? Set one to chemfuel and try beer in the other: the menu should refuse it and say why.
5. **Take it in a caravan to a friendly settlement** with silver aboard. Does Bulk order appear, list fluids and prices, and fill the tank?
