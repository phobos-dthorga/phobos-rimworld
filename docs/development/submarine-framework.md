# The submersibles framework: design (9 Nov 2026)

**Status: first slice built in D'thorga Framework 0.58.0, with a dev-only test hull in D'thorga Vehicles 0.25.0. Not yet tested in game.** The owner asked on 9 November 2026 to begin a framework
for submarines inside the existing framework. The ideas it serves are in [unique military vehicles and submarines](military-vehicles-research.md). Agent-authored design, for the owner's review.

## Read this first

- **It is part of D'thorga Framework's vehicle systems**, not a mod (owner, 9 Nov 2026). It loads only with Vehicle Framework. Any author's Sea vehicle becomes a submersible with one comp and two stats, in XML.
- **Vehicle Framework has no depth, so a dive is a state of ours.** The hull stays on its cells. It is drawn as a shadow, cannot be picked as a target, takes no hits but explosions, cannot fire or land anyone, and runs down a reserve. This is a gameplay simplification **[authored]**, not a model of water.
- **The reserve is one number**, hours of air and charge at rest. Tying it to the batteries, and giving a boat a chemical engine, are later slices.
- **No playable submarine exists yet.** The test hull is spawned with a dev action, looks like a truck on the water (it borrows the tanker's picture) and has no upgrade tree because it is not a design.
- **Vehicle Framework runs here as its 1.6 build**, which the 1.7 game loads. Everything below was read from that build's decompile, and `python scripts/check-targets.py` holds every patch to the installed assemblies. Those checks say the names exist, not that the behaviour is right.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Dive and surface | Take a boat out of sight over deep water, with a short delay either way | Diving in shallows, without the crew, with damaged ballast or pressure hull, or on a nearly empty reserve; each refusal says why and names the part | Water becomes somewhere to hide, and coasts become places to reach unseen |
| Hidden from enemies | Sit under a fight or slip past one | Nothing | A boat is safe while under, so the danger moves to the moments it comes up |
| Only explosions reach it | Shrug off gunfire | Ignoring bombs and shells: half of an explosion still gets through by default | Heavy weapons stay the answer to a submarine |
| Guns held, nobody lands | Nothing | Firing most guns and landing anyone from below; a design can name turrets, such as torpedo tubes, that do fire | The player chooses between being safe and being useful |
| The reserve | Stay down for hours, longer when still | Staying down for ever; it goes three times as fast on the move by default and forces the boat up when empty | Each dive is a budget, and surfacing to recharge is a risk to plan |
| A caravan goes under as one | Cross hostile sea with no ambushes, demands or meetings | Going under with anyone on foot or any vehicle that cannot dive; on land or shallow tiles it comes up | The sea is a safe road when the land is not |

## How it works

| Piece | Where | Job |
| --- | --- | --- |
| `DiveRule` | `src/PhobosFramework.Vehicles/Rules/DiveRules.cs` | The arithmetic, free of the game, with tests in `tests/PhobosFramework.Vehicles.Rules.Tests/DiveRuleTests.cs` |
| `CompPhobosSubmersible` | `src/PhobosFramework.Vehicles/Systems/CompPhobosSubmersible.cs` | The one vehicle's state: phase, reserve, refusals, the reasons it must come up. The saved keys start `PhobosDive` |
| `Submersibles` | `src/PhobosFramework.Vehicles/Service/Submersibles.cs` | What other code asks, caravan orders, the shared button, the shadow, the caravan comp |
| Six patches | `src/PhobosFramework.Vehicles/Service/SubmersiblePatches.cs` | Each asks `Submersibles` one question |
| Stats, text, caravan patch | `mods/PhobosFramework/Mods/VehicleFramework/Common/` | `Submersible.xml`, `PhobosSubmersible.xml`, `SubmersibleCaravan.xml` |

- **Phases:** surfaced, diving, submerged, surfacing. Only submerged is hidden. A boat flooding its tanks has not got away yet, and one blowing them is in sight at once. The reserve burns only while submerged and refills only on the surface.
- **Deep enough** means every cell of the footprint is water nobody can wade (`IsWater` and impassable), or a terrain the design lists. On the world map it means a water-covered tile. Chest-deep moving water does not count by default, so rivers are for surfaced boats **[agent default]**.
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

The other optional fields are `diveSeconds`, `surfaceSeconds`, `controlFloor`, `minimumReserveShare`, `moveBurnFactor`, `rechargeFactor`, `damageThatReaches`, `damageFactor` and `alsoDeepEnough`.

## What was read, and why each patch exists

Vehicle Framework line numbers are in `.local/decompiled/VehicleFramework-1.6/Vehicles.decompiled.cs`.

| Patch | Kind | Why there is no other way |
| --- | --- | --- |
| `Pawn.ThreatDisabled` | Postfix | Vehicle Framework's own postfix treats any crewed vehicle as a threat (`VehiclePawn.IsThreatToAttackTargetSearcher`, 14535), and `VehicleComp.IsThreat` can only add to that |
| `VehiclePawn.DynamicDrawPhaseAt` | Prefix that skips | One method draws the body, turrets, overlays and crew (16034), with no way to leave them out |
| `VehicleStatHandler.ApplyDamage` | Prefix that skips | Every hit, from `PreApplyDamage` (15461) and the cell route (15451), ends here (10097), so one patch covers all weapon frameworks |
| `VehicleTurret.TurretDisabled` | Postfix | Firing, auto-targeting and the threat test all read it (63026, 63087, 63806, 20942) |
| `VehiclePawn.DisembarkPawn` | Prefix that skips | It would place the person beside the hull, in deep water (15256), and has no check to extend |
| `WorldObject.IncidentTargetTags` | Postfix | The storyteller matches incidents to these tags, and comps can only add tags |

Two things needed no patch. Caravan buttons and text come from a `WorldObjectComp`, as the existing caravan comp's do, and the shadow is the game's own `Things/Skyfaller/SkyfallerShadowRectangle`.

## Known limits of this slice

- **A hidden hull still claims its cells**, so another boat cannot pass through it, and it still leaves a wake.
- **A rare tick is about four seconds**, so a boat can run a little way into shallows before it comes up. A move order into the shallows is not refused.
- **A stray shot can stop at a hidden hull** and do nothing. Nobody aims at one, so this should be rare.
- **Fuel burns as usual while under**, and the batteries and engine systems do not know about the dive.
- **A submerged caravan is not stopped from visiting a settlement.** It comes up if the map it enters is shallow where it arrives.
- **Combat Extended's targeting and Vehicle Framework's enemy turrets are unverified.** Both should go through `ThreatDisabled` and the damage route, but that is inferred and not seen in play.
- **A player can still order a shot at their own hidden boat by hand.** Only choosing a target automatically is stopped.

## Safe on a save

- **Safe to add.** Nothing changes for a vehicle without the comp, and no existing vehicle has it.
- **Removing it:** the saved keys are ignored. The test hull goes the way any removed vehicle does, so delete it first.

## How to test it (the owner's)

Short list, highest consequence first. Enable dev mode and use a map with a lake or sea.

1. Debug actions, "Vehicle systems: spawn a submersible", click deep water. It arrives crewed. Press **Dive**: after 15 seconds it should become a shadow.
2. With enemies present, check they ignore it, and that a mortar or bomb still hurts it while gunfire does not.
3. Drive it into shallows and see it surface and say why. Use "Vehicle systems: nearly empty a submersible" and see it come up.
4. Save and load while under: it should still be a shadow, with the same reserve.
5. Form a caravan on the sea and try the caravan's **Dive** button.

## Next slices (agent proposal)

1. The first real boat, the cargo submarine, with its own picture, overlays and upgrade tree.
2. The reserve drawn from the electrical system's batteries, and diesel only on the surface.
3. Torpedo tubes through `turretsThatFireBelow`, and refusing move orders into shallows.
4. A chemical reserve for the Ictíneo-style boat, once the owner has settled its mass balance.
5. Depth as more than a yes or no, if play shows the need.

## Who chose what

- **Owner (9 Nov 2026):** build a framework for submarines; it joins the existing framework. No stand-in body art, so the test hull borrows a picture.
- **Agent choices, owner to revise:** a dive as a state; one reserve number; explosions alone reach a hull, at half strength; the figures (15 and 10 seconds, three times the burn on the move, four hours regained per hour on the surface, a floor of half for the trim, a tenth of the reserve to dive); rivers not deep enough; a caravan goes under whole or not at all; a dev-only hull as the first consumer.

## Open questions for the owner

1. Should only explosions reach a boat that is under, and at half strength?
2. Should a boat run out of reserve simply surface, or should the crew start to suffer first?
3. Should chest-deep rivers count as deep enough?
4. Which boat comes first? The research record recommends the cargo submarine.

## Sources

Our own reading of Vehicle Framework by SmashPhil (MIT; decompile kept in `.local/`, never committed) and of RimWorld 1.7. The real boats behind these ideas are sourced in [the research record](military-vehicles-research.md).
