# D'thorga Burns and Smoke

Burns and smoke that matter the way More Injuries' wounds do: a burned body fraction, burn shock that kills within days
unless treated, airway burns that swell shut over hours, smoke that scars the lungs, and carbon monoxide from fires in closed
rooms.

Version 0.2.0, built 4 November 2026 for House D'thorga. **Not yet tested in game.** Everything here is read from the game's and
the mods' code.

## Read this first (gotchas)

- **Needs D'thorga Framework**, loaded before it. More Injuries, More Injuries: Organ Failure and Combat Extended are optional
  and are used when present. No Harmony.
- **Safe to add to a running colony.** Its conditions are recomputed from the burns a pawn has, so old burns count at once.
- **Before removing it, press the settings button "Remove every burn condition from every pawn", save, then switch the mod off.**
  Otherwise the next load prints one line per missing condition and carries on.
- **With More Injuries, its own "fire inhalation" feature is switched off by this mod** (a patch in the More Injuries folder; delete
  that file to keep both). More Injuries put plain burns on both lungs whenever a burning pawn was hurt; this mod models the
  inhalation itself. Carbon monoxide and a closing airway feed More Injuries' hypoxia and brain damage.
- **With Combat Extended's smoke running, CE's smoke inhalation is the signal.** This mod adds the lasting lung damage and
  the carbon monoxide on top. A gas mask stops the smoke effects but **not the carbon monoxide**.
- **Without Combat Extended's smoke, burning rooms fill with the game's own smoke** (the framework does this), which also
  penalises aim and sight.
- **Tend the shock itself.** Burn shock is a tendable condition: tending it stands in for giving fluids. Tend every burn and keep
  the patient in bed as well; that lowers where the shock is heading.
- **The tracheotomy** is a surgery on the neck. With More Injuries it needs its Emergency Medicine research; without, no research.
- **Plain mode shows no percentages.** Switch the display mode to Operator or Engineer in the settings to see the figures.
- **Only humanlike pawns** get these conditions in this version. Animals burn as before.
- **A part burned off counts as all of its skin burned** while the stump is fresh, so a pawn who loses an arm and a leg to
  fire goes straight to a serious burn and burn shock; once the stump is tended it fades out over about two weeks.

## What it does

| Condition | Comes from | Gets worse with | Treated by |
| --- | --- | --- | --- |
| burns (burned body fraction) | every burn on the skin and every part burned off, weighted by each part's share of the skin | more and deeper burns, lost parts | tending the burns and stumps; it is recomputed every 300 ticks |
| burn shock | 15% or more of the body burned; always at least its first stage from there | age, area, an airway burn or carbon monoxide (the revised Baux score) | tending the burns, bed rest, tending the shock |
| airway burn | burning, or burns to the head and neck | time: swelling peaks at 8 hours, holds to 24, gone by 48 | a tracheotomy; or waiting, if it never closes |
| smoke-damaged lungs | breathing smoke | thicker smoke, longer | tending; rest; heals 4% a day untended, 16% tended well |
| pneumonia | severe lung damage | | tending and immunity, like a wound infection |
| carbon monoxide poisoning | the framework's carbon monoxide in a closed burning room | staying in the room | clean air: halves every 5.3 hours under a roof, 3 in the open |
| smoke inhalation (acute) | breathing the game's smoke, only when Combat Extended's smoke is not running | thicker smoke | clean air; clears in a day or so |

Burned body fraction stages: minor from 5%, serious from 15% (hunger, cold sensitivity, blood pumping), major from 30%,
critical from 50%. Burn shock stages: compensated, shock from 30%, severe from 55% (a four-day death clock), terminal from 80%
(one day). Carbon monoxide: headache from 10%, dizzy 20%, confused 30%, collapsed 40%, dying from 55% (a half-day clock).
Every figure is an authored starting value; the sources behind the shape are in the design record.

A worked case: a 40-year-old with a quarter of the body burned and no inhalation injury scores 65 and heads for stage 1 untreated;
the same pawn with an airway burn scores 82 and heads for the severe stage; tended and in bed, either settles at compensated.

## Settings

Toggles for each of the five conditions; **Also apply this mod's smoke inhalation with Combat Extended** (off); the display mode;
and the uninstall button.

## For modders

Mark an injury Def as a burn for the burned fraction with the extension this mod patches onto the game's burns:

```xml
<modExtensions>
  <li Class="Phobos.BurnsAndSmoke.Defs.PhobosBurnLikeExtension">
    <tbsaWeight>1</tbsaWeight>
  </li>
</modExtensions>
```

Balance lives in `Common/Defs/Phobos/BurnsAndSmoke/Rules.xml` (one `PhobosBurnRulesDef`, first loaded wins) and in the stage
tables of the hediff Defs. Smoke and carbon monoxide are read from D'thorga Framework's `PhobosGases`.

## Checks done (4 November 2026)

- Built against the RimWorld 1.7 and 1.6 reference assemblies. The game-free rules (skin shares, burned fraction, burned-off
  parts, the Baux score, its floor and treatment factor, carbon monoxide uptake and clearance, the airway curve) pass 18 unit
  tests.
- The owner's first look in game (0.1.0, an incendiary raid) showed the mod running without errors but counting far too
  little: see the changelog's 0.2.0 fixes. 0.2.0 itself is **not yet tested in game**.
- **Not checked:** anything in game for 0.2.0. The giver's cadence, the stage tables' feel, the tracheotomy, the More Injuries handlers
  and Organ Failure's interception of a closed airway are all untested.
