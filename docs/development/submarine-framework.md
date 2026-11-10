# The submersibles framework: design (9 Nov 2026, updated 10 Nov 2026)

**Status: built in D'thorga Framework 0.58.0, 0.59.0 (spotting, staying down, chest-deep water) and 0.60.0 (a boat can be lost), with a dev-only test hull in D'thorga Vehicles 0.26.0. Not yet tested in game.** The owner asked on 9 November 2026 to begin a framework
for submarines inside the existing framework. The ideas it serves are in [unique military vehicles and submarines](military-vehicles-research.md). Agent-authored design, for the owner's review.

## Read this first

- **It is part of D'thorga Framework's vehicle systems**, not a mod (owner, 9 Nov 2026). It loads only with Vehicle Framework. Any author's Sea vehicle becomes a submersible with one comp and two stats, in XML.
- **No boat interiors, by the owner's choice** (10 Nov 2026). The owner first wanted submarines built like gravships, with a walkable interior, but has seen recent mods take that approach with terrible performance. Depth comes from research, upgrades and our vehicle systems instead. That approach is also what makes **enemy submarines** affordable, and the owner wants them in time.
- **Vehicle Framework has no depth, so a dive is a state of ours.** The hull stays on its cells. It cannot be aimed at until it is spotted, takes no hits but explosions, cannot fire or land anyone, and runs down a reserve. This is a gameplay simplification **[authored]**, not a model of water.
- **The reserve is one number**, hours of air and charge at rest. Tying it to the batteries, and giving a boat a chemical engine, are later slices.
- **The first real boat is [the Seabed Crawler](vehicle-seabed-crawler.md)** (owner, 10 Nov 2026), which is designed and not built.
- **No playable submarine exists yet.** The test hull is spawned with a dev action, looks like a truck on the water (it borrows the tanker's picture) and has no upgrade tree because it is not a design.
- **Vehicle Framework runs here as its 1.6 build**, which the 1.7 game loads. Everything below was read from that build's decompile, and `python scripts/check-targets.py` holds every patch to the installed assemblies. Those checks say the names exist, not that the behaviour is right.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Dive and surface | Take a boat out of sight over deep water, with a short delay either way | Diving in shallows, without the crew, with damaged ballast or pressure hull, or on a nearly empty reserve; each refusal says why and names the part | Water becomes somewhere to hide, and coasts become places to reach unseen |
| Hidden until spotted | Sit under a fight or slip past one, unseen | Diving in full view and getting away with it: anyone watching follows it down | Where and when to dive matters, and the safest dive is out of sight |
| Being spotted | Know it from the messages and the dark shape, and run or settle until they lose it | Staying near watchers for long, more so on the move and in clear daylight | A boat near a shore is on a clock, and more enemies shorten it |
| Only explosions reach it, at half strength (owner, 10 Nov 2026) | Shrug off gunfire | Ignoring bombs and shells once spotted; only someone with an explosive weapon will aim at it | Spotting plus heavy weapons is the answer to a submarine |
| Guns held, nobody lands | Nothing | Firing most guns and landing anyone from below; a design can name turrets, such as torpedo tubes, that do fire | The player chooses between being safe and being useful |
| The reserve | Stay down for hours, longer when still | Staying down for ever; it goes three times as fast on the move by default and forces the boat up when empty | Each dive is a budget, and surfacing to recharge is a risk to plan |
| Stay down (owner, 10 Nov 2026) | Keep hiding after the reserve is gone, when coming up would be worse | Changing their mind too late: once nobody aboard can work the boat the order stands, the crew dies and the boat is lost | An extreme decision with a real price, and a story either way |
| Chest-deep water, per design (owner, 10 Nov 2026) | Take a small boat up a river out of sight | Nothing for a boat that does not ask for it | A midget boat opens routes a big one cannot use |
| A caravan goes under as one | Cross hostile sea with no ambushes, demands or meetings | Going under with anyone on foot or any vehicle that cannot dive; on land or shallow tiles it comes up | The sea is a safe road when the land is not |

## How it works

| Piece | Where | Job |
| --- | --- | --- |
| `DiveRule` | `src/PhobosFramework.Vehicles/Rules/DiveRules.cs` | The arithmetic, free of the game, with tests in `tests/PhobosFramework.Vehicles.Rules.Tests/DiveRuleTests.cs` |
| `CompPhobosSubmersible` | `src/PhobosFramework.Vehicles/Systems/CompPhobosSubmersible.cs` | The one vehicle's state: phase, reserve, who is following it, the standing order, refusals, the reasons it must come up. The saved keys start `PhobosDive` |
| `Submersibles` | `src/PhobosFramework.Vehicles/Service/Submersibles.cs` | What other code asks, caravan orders, the shared button, the shadow, the caravan comp |
| Six patches | `src/PhobosFramework.Vehicles/Service/SubmersiblePatches.cs` | Each asks `Submersibles` one question |
| Stats, text, caravan patch | `mods/PhobosFramework/Mods/VehicleFramework/Common/` | `Submersible.xml`, `PhobosSubmersible.xml`, `SubmersibleCaravan.xml` |

- **Phases:** surfaced, diving, submerged, surfacing. Only submerged is hidden. A boat flooding its tanks has not got away yet, and one blowing them is in sight at once. The reserve burns only while submerged and refills only on the surface.
- **Deep enough** means every cell of the footprint is water nobody can wade (`IsWater` and impassable), or a terrain the design lists. On the world map it means a water-covered tile. **Each design chooses whether chest-deep water counts** with `divesInChestDeepWater`, off by default (owner, 10 Nov 2026); a design that says yes can also dive on a world tile with a river. Chest-deep is read from the path cost, 40 or more (the game's shallows cost 30 and its chest-deep 42), so another mod's water counts without naming it.
- **Spotting (owner's idea, 10 Nov 2026; the rules are the agent's).** About every four seconds, everyone hostile on the map gets a look: `chance = 2% x closeness x 3 if moving x visibility x the design's noise`, where closeness falls from 1 on top of the hull to 0 at 25 cells, and visibility is the game's own weather accuracy times a darkness factor that bottoms out at half. Line of sight is needed. A hit means watchers follow it for 30 seconds, renewed by each new sighting. Anyone in range with a clear line as it finishes going down spots it for certain. A blast that hurts it spots it too. **All figures authored.**
- **What the player sees (owner's idea, 10 Nov 2026).** The dark shape shows only once a hull is spotted. A hull of the player's own, unspotted, shows a small faint mark at its middle, because the player still has to find and click it **[agent default]**.
- **Who may aim at a spotted hull:** only someone whose current weapon deals damage that reaches it. Without this, a spotted boat would soak up gunfire that could never hurt it.
- **Staying down (owner, 10 Nov 2026).** Off by default, and the boat surfaces when it runs out. On, it stays, and everyone aboard who breathes gains stale air: 0.3 an hour against 0.1 cleared by the body, so a net 0.2. Headaches from 0.1, gasping from 0.3, confusion from 0.55, unconscious from 0.8, fatal at 1, so about four hours past empty to pass out and five to die. **Stages and rates authored.** Stale air stands for carbon dioxide building up in a sealed hull and is not a dose-response model; it may become part of our own health mechanics later (owner, 10 Nov 2026).
- **A boat can be lost (owner, 10 Nov 2026: RimWorld is about extreme decisions and the story they make).** Without the order, a boat that loses everyone who can work it comes up by itself. With the order, it does not: the order can no longer be changed, Surface is refused with the reason, and a letter says so. It stays where it is, its own faint mark still showing. Damage that wrecks its ballast or hull can still force it up.
- **Time is charged by elapsed ticks**, from whichever of the comp's rare tick or the caravan comp's tick runs first, so nothing is charged twice and a boat is charged correctly on either map.
- **Cost:** one rare-tick update per boat. Patches that run for every pawn start with one check of an empty set while no boat is under. The set of hidden hulls is derived and never saved.

### To make a Sea vehicle a submersible (for modders)

```xml
<vehicleStats>
  <Phobos_DiveEndurance>12</Phobos_DiveEndurance> <!-- hours at rest -->
  <Phobos_DiveControl>1</Phobos_DiveControl>
</vehicleStats>
<!-- List Phobos_DiveControl in the categories of the ballast and pressure hull parts, and Phobos_DiveEndurance in those of the air plant and batteries. -->
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosSubmersible">
    <turretsThatFireBelow><li>torpedoTube</li></turretsThatFireBelow> <!-- optional -->
  </li>
</comps>
```

The other optional fields are `diveSeconds`, `surfaceSeconds`, `controlFloor`, `minimumReserveShare`, `moveBurnFactor`, `rechargeFactor`, `damageThatReaches`, `damageFactor`, `alsoDeepEnough`, `divesInChestDeepWater`, `spotRange`, `spotChance`, `movingSpotFactor`, `darkSpotFloor`, `noise`, `trackedSeconds` and `staleAirPerHour`. Call `Spot()` on the comp to have something, such as sonar, find a boat.

## What was read, and why each patch exists

Vehicle Framework line numbers are in `.local/decompiled/VehicleFramework-1.6/Vehicles.decompiled.cs`.

| Patch | Kind | Why there is no other way |
| --- | --- | --- |
| `Pawn.ThreatDisabled` | Postfix | Vehicle Framework's own postfix treats any crewed vehicle as a threat (`VehiclePawn.IsThreatToAttackTargetSearcher`, 14535), and `VehicleComp.IsThreat` can only add to that. It receives who is searching, so a spotted hull can be left alone by anyone who could not hurt it |
| `VehiclePawn.DynamicDrawPhaseAt` | Prefix that skips | One method draws the body, turrets, overlays and crew (16034), with no way to leave them out |
| `VehicleStatHandler.ApplyDamage` | Prefix that skips | Every hit, from `PreApplyDamage` (15461) and the cell route (15451), ends here (10097), so one patch covers all weapon frameworks |
| `VehicleTurret.TurretDisabled` | Postfix | Firing, auto-targeting and the threat test all read it (63026, 63087, 63806, 20942) |
| `VehiclePawn.DisembarkPawn` | Prefix that skips | It would place the person beside the hull, in deep water (15256), and has no check to extend |
| `WorldObject.IncidentTargetTags` | Postfix | The storyteller matches incidents to these tags, and comps can only add tags |

Two things needed no patch. Caravan buttons and text come from a `WorldObjectComp`, as the existing caravan comp's do, and the shadow is the game's own `Things/Skyfaller/SkyfallerShadowRectangle`.

## Known limits of this slice

- **A hidden hull still claims its cells**, so another boat cannot pass through it, and it still leaves a wake.
- **A rare tick is about four seconds**, so a boat can run a little way into shallows before it comes up. A move order into the shallows is not refused.
- **A stray shot can stop at a hidden hull** and do nothing. Only explosive weapons aim at one, so this should be rare.
- **Nothing can raise a lost boat yet.** A lost caravan is likewise stuck where it is.
- **Spotting happens on a map only.** A caravan that is under the water is simply out of reach.
- **A hostile boat that nobody has spotted is drawn as nothing but could still be clicked.** No hostile boats exist yet.
- **Firing from below does not give a boat away yet**, because no boat has a gun that fires from below. Torpedo tubes must call `Spot()`.
- **Combat Extended's guns are judged by their default shell**, not the one loaded, so a mortar loaded with something other than its default may be judged wrongly.
- **Fuel burns as usual while under**, and the batteries and engine systems do not know about the dive.
- **A submerged caravan is not stopped from visiting a settlement.** It comes up if the map it enters is shallow where it arrives.
- **Combat Extended's targeting and Vehicle Framework's enemy turrets are unverified.** Both should go through `ThreatDisabled` and the damage route, but that is inferred and not seen in play. **The damage that reaches a hull is matched to the game's `Bomb`**, and whether Combat Extended's shells use it is unverified.
- **A player can still order a shot at their own hidden boat by hand.** Only choosing a target automatically is stopped.

## Ready for enemy submarines?

**Owner, 10 Nov 2026: enemy submarines are coming "not later, but very soon", and everything from now on is designed with them in mind.** Every mechanic must work for any faction's boat, cost little per hull with several hostile ones on a map, and never show the player what their own crew could not know. Read from our code on 10 Nov 2026; nothing here is tested in game.

| Piece | Works for a hostile boat? | What is missing |
| --- | --- | --- |
| Dive state, reserve, coming up when empty or damaged | Yes: it lives on the vehicle, for any faction | Nothing |
| Being spotted by watchers | Yes: the player's people and turrets are the watchers | **The player is not told** when they spot an enemy boat; messages go only to a boat's own side |
| Not aimed at until spotted; only weapons that reach it once spotted | Yes (`Pawn.ThreatDisabled` postfix) | Nothing |
| Drawn as nothing until spotted, then a dark shape | Yes | **Its name label, selection and inspect panel may still show**: a hostile hull nobody has found must not be clickable or labelled. Not checked |
| Hits below, turrets held, nobody landing | Yes, for any vehicle | Nothing |
| Settling on the bottom | Yes, once ordered | Nothing to order it |
| Hearing a hidden hull (Framework 0.72.0) | Yes, both ways: an enemy boat with an array spots ours | Nothing |
| Active sonar (Framework 0.81.0) | Yes, both ways: an enemy boat's ping finds and spots ours, and gives it away; it pings by itself now and then while it hunts, and the player's boats hear the bearing with or without a set | Its pings come from a simple chance, not from a boat's own AI |
| Its own behaviour: when to dive, hunt, settle, surface, fire | **No** | A boat's own AI, through the raid's lord. Not designed |
| Arriving: from the map's sea edge, or as a world-map threat | **No** | Not designed; how Vehicle Framework brings hostile boats at all is not yet read |
| Torpedoes | **No** | Planned below as torpedo tubes through `turretsThatFireBelow` |
| Wreck and salvage when one is sunk | **No** | Ties to recovery equipment and the Crawler's manipulator |
| Cost with several hulls | Expected small: each hull's work runs every rare tick; the per-pawn patches cost one empty check while none is under | **Not measured.** Profile a map with several hostile hulls before they ship |

## Safe on a save

- **Safe to add.** Nothing changes for a vehicle without the comp, and no existing vehicle has it.
- **Removing it:** the saved keys are ignored. The test hull goes the way any removed vehicle does, so delete it first.

## How to test it (the owner's)

Short list, highest consequence first. Enable dev mode and use a map with a lake or sea.

1. Debug actions, "Vehicle systems: spawn a submersible", click deep water. It arrives crewed. Press **Dive**: after 15 seconds it should vanish, leaving a faint mark at its middle.
2. Select it and use "Vehicle systems: spot a submersible". It should show as a dark shape and say it has been spotted, and about 30 seconds later say the enemy has lost it. With enemies near, check that riflemen ignore it while a mortar or grenadier goes for it once it is spotted.
3. Drive it into shallows and see it surface and say why. Use "Vehicle systems: nearly empty a submersible" and see it come up. Do that again with **Stay down** on: it should stay, and the crew should gain stale air (use the game's dev speed), pass out, and die. When they pass out a letter should say the boat is lost, and **Surface** and **Stay down** should go grey and say why.
4. Save and load while under: it should still be hidden, with the same reserve.
5. Form a caravan on the sea and try the caravan's **Dive** button.

## Next slices (agent proposal)

- **Enemy submarines, very soon** (owner, 10 Nov 2026). See [Ready for enemy submarines?](#ready-for-enemy-submarines) for what already works and what is missing.
- **Our own interfaces and mechanics in place of a walkable interior** (owner, 10 Nov 2026). These could mimic or replace some of what an interior gives, and could also bring mechanics of our own beside the gravship way. One idea, not designed: a control-room window with compartments, stations and who is in them, the air, and a sonar screen plotting what the array has heard.
- **Equipment for recovering lost boats** (owner's idea, 10 Nov 2026). The research record's recovery vehicle and the Crawler's manipulator are natural starting points.

1. The first real boat, [the Seabed Crawler](vehicle-seabed-crawler.md), with its own picture, overlays and upgrade tree (owner, 10 Nov 2026).
2. The reserve drawn from the electrical system's batteries, and diesel only on the surface.
3. Torpedo tubes through `turretsThatFireBelow`, and refusing move orders into shallows.
4. A chemical reserve for the Ictíneo-style boat, once the owner has settled its mass balance.
5. Depth as more than a yes or no, if play shows the need.

## Who chose what

- **Owner (9 Nov 2026):** build a framework for submarines; it joins the existing framework. No stand-in body art, so the test hull borrows a picture.
- **Owner (10 Nov 2026):** explosions at half strength are right; a detection mechanic, with the shape in the water seen only once detected; a boat surfaces when empty, with a button to override; chest-deep water is an option for each submarine, thinking of midget boats; the first real boat is the Seabed Crawler.
- **Owner, later on 10 Nov 2026:** forced to stay down kills everyone, possibly through our own health mechanics later; lost boats may have recovery equipment later; keep the faint mark on our own boat.
- **Owner, after the listening array (10 Nov 2026):** no gravship-style interiors, for performance; research and many upgrades, with our vehicle systems, are the halfway point instead; enemy submarines wanted in time. Submarines may be "one of the best features" of D'thorga Vehicles. Then: unique interfaces and mechanics of our own may mimic or replace what an interior gives; **design everything with enemy submarines in mind, as coming "not later, but very soon"**.
- **The owner's "an option for each submarine" was read as a choice in each design's XML**, not a button in play. Say if a button was meant.
- **Agent choices, owner to revise:** a dive as a state; one reserve number; all the spotting rules and figures; the faint mark on the player's own boat; only weapons that can reach it aim at a spotted hull; stale air as the price of staying down, and its figures; a boat without the order comes up when nobody can work it; the figures (15 and 10 seconds, three times the burn on the move, four hours regained per hour on the surface, a floor of half for the trim, a tenth of the reserve to dive); rivers not deep enough; a caravan goes under whole or not at all; a dev-only hull as the first consumer.

## Open questions for the owner

All are answered, under [Who chose what](#who-chose-what).

## Sources

Our own reading of Vehicle Framework by SmashPhil (MIT; decompile kept in `.local/`, never committed) and of RimWorld 1.7. The real boats behind these ideas are sourced in [the research record](military-vehicles-research.md).
