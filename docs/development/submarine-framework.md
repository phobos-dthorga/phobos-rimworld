# The submersibles framework: design (9 Nov 2026, updated 10 Nov 2026)

**Status: built in D'thorga Framework 0.58.0 and 0.59.0 (spotting, staying down, chest-deep water), with a dev-only test hull in D'thorga Vehicles 0.26.0. Not yet tested in game.** The owner asked on 9 November 2026 to begin a framework
for submarines inside the existing framework. The ideas it serves are in [unique military vehicles and submarines](military-vehicles-research.md). Agent-authored design, for the owner's review.

## Read this first

- **It is part of D'thorga Framework's vehicle systems**, not a mod (owner, 9 Nov 2026). It loads only with Vehicle Framework. Any author's Sea vehicle becomes a submersible with one comp and two stats, in XML.
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
| Stay down (owner, 10 Nov 2026) | Keep hiding after the reserve is gone, when coming up would be worse | Staying for ever: the crew breathes stale air, gets worse and passes out, and then the boat comes up | Air becomes a price the player can choose to pay |
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
- **Staying down (owner, 10 Nov 2026).** Off by default, and the boat surfaces when it runs out. On, it stays, and everyone aboard who breathes gains stale air: 0.3 an hour against 0.1 cleared by the body, so a net 0.2. Headaches from 0.1, gasping from 0.3, confusion from 0.55, unconscious from 0.8, fatal at 1, so about four hours past empty to pass out. **Stages and rates authored.** Stale air stands for carbon dioxide building up in a sealed hull and is not a dose-response model. An unconscious crew cannot work the boat, and a boat with nobody able to work it comes up **[agent default]**: otherwise it would sit hidden for ever.
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
- **Spotting happens on a map only.** A caravan that is under the water is simply out of reach.
- **A hostile boat that nobody has spotted is drawn as nothing but could still be clicked.** No hostile boats exist yet.
- **Firing from below does not give a boat away yet**, because no boat has a gun that fires from below. Torpedo tubes must call `Spot()`.
- **Combat Extended's guns are judged by their default shell**, not the one loaded, so a mortar loaded with something other than its default may be judged wrongly.
- **Fuel burns as usual while under**, and the batteries and engine systems do not know about the dive.
- **A submerged caravan is not stopped from visiting a settlement.** It comes up if the map it enters is shallow where it arrives.
- **Combat Extended's targeting and Vehicle Framework's enemy turrets are unverified.** Both should go through `ThreatDisabled` and the damage route, but that is inferred and not seen in play. **The damage that reaches a hull is matched to the game's `Bomb`**, and whether Combat Extended's shells use it is unverified.
- **A player can still order a shot at their own hidden boat by hand.** Only choosing a target automatically is stopped.

## Safe on a save

- **Safe to add.** Nothing changes for a vehicle without the comp, and no existing vehicle has it.
- **Removing it:** the saved keys are ignored. The test hull goes the way any removed vehicle does, so delete it first.

## How to test it (the owner's)

Short list, highest consequence first. Enable dev mode and use a map with a lake or sea.

1. Debug actions, "Vehicle systems: spawn a submersible", click deep water. It arrives crewed. Press **Dive**: after 15 seconds it should vanish, leaving a faint mark at its middle.
2. Select it and use "Vehicle systems: spot a submersible". It should show as a dark shape and say it has been spotted, and about 30 seconds later say the enemy has lost it. With enemies near, check that riflemen ignore it while a mortar or grenadier goes for it once it is spotted.
3. Drive it into shallows and see it surface and say why. Use "Vehicle systems: nearly empty a submersible" and see it come up. Do that again with **Stay down** on: it should stay, and the crew should gain stale air (use the game's dev speed), then pass out and come up.
4. Save and load while under: it should still be hidden, with the same reserve.
5. Form a caravan on the sea and try the caravan's **Dive** button.

## Next slices (agent proposal)

1. The first real boat, [the Seabed Crawler](vehicle-seabed-crawler.md), with its own picture, overlays and upgrade tree (owner, 10 Nov 2026).
2. The reserve drawn from the electrical system's batteries, and diesel only on the surface.
3. Torpedo tubes through `turretsThatFireBelow`, and refusing move orders into shallows.
4. A chemical reserve for the Ictíneo-style boat, once the owner has settled its mass balance.
5. Depth as more than a yes or no, if play shows the need.

## Who chose what

- **Owner (9 Nov 2026):** build a framework for submarines; it joins the existing framework. No stand-in body art, so the test hull borrows a picture.
- **Owner (10 Nov 2026):** explosions at half strength are right; a detection mechanic, with the shape in the water seen only once detected; a boat surfaces when empty, with a button to override; chest-deep water is an option for each submarine, thinking of midget boats; the first real boat is the Seabed Crawler.
- **The owner's "an option for each submarine" was read as a choice in each design's XML**, not a button in play. Say if a button was meant.
- **Agent choices, owner to revise:** a dive as a state; one reserve number; all the spotting rules and figures; the faint mark on the player's own boat; only weapons that can reach it aim at a spotted hull; stale air as the price of staying down, and its figures; a boat with nobody able to work it comes up; the figures (15 and 10 seconds, three times the burn on the move, four hours regained per hour on the surface, a floor of half for the trim, a tenth of the reserve to dive); rivers not deep enough; a caravan goes under whole or not at all; a dev-only hull as the first consumer.

## Open questions for the owner

The four questions of 9 November are answered, under [Who chose what](#who-chose-what). Two remain from the answers:

1. Should a crew that passes out bring the boat up by itself, as it does now, or should staying down be able to kill everyone aboard?
2. Should the player's own unspotted boat show the faint mark, or nothing at all until it is selected?

## Sources

Our own reading of Vehicle Framework by SmashPhil (MIT; decompile kept in `.local/`, never committed) and of RimWorld 1.7. The real boats behind these ideas are sourced in [the research record](military-vehicles-research.md).
