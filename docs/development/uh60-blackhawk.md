# The UH-60M Black Hawk in the fold (22 Nov 2026)

**Status (22 Nov 2026):** built, not yet tested in game: D'thorga Vehicles 0.95.0 (its systems), D'thorga Patches 0.31.0 (its build cost as a
parts bill). Test row 313.

## Read this first

- **What it is:** UH-60M Black Hawk (Vehicle Framework) by z3nt3 (`z3nt3.uh60blackhawk`, Workshop 3841587792), released 17 Nov 2026 and
  updated 18 Nov 2026. The owner added it on 22 Nov 2026 and asked for it to be taken in "provided it's a decent and well-made mod".
- **The judgement (agent's, 22 Nov 2026): decent and well made, but new and unproven.**
  - For it: a careful part list (19 parts: two engines with covers, transmission, gearbox, rotor mast, servos, dashboard, three landing
    wheels, side panels, fuel tank), sensible armour, and its own features built on the game's own systems: a walkable interior through
    RimWorld's pocket maps, made only when someone goes in, and airdrop and EVAC hover modes that hold the helicopter safely and bring it
    home through Vehicle Framework's own arrival. Three Harmony patches, none skipping the game's code (`query-patches.py mod "UH-60*"`):
    its arrival options, and two that keep its interior from being thrown away while someone is in it.
  - Against it: four days old, 84 subscribers, one comment (a joke), no reply from its author yet, and no public source (read from a
    decompile in `.local/decompiled/UH60Blackhawk-1.7-2026-11-18`, never committed). One place swallows an error without a word.
- **Its author's features are left alone**: no rescue hoist, fast-rope bar or litter kit of ours is offered on it, since it hoists,
  ropes down and carries patients in its own interior.

## The four questions

| Question | Answer |
| --- | --- |
| What it lets the player do | Fly a Black Hawk whose engines, rotor drive, hydraulics and avionics wear, fail and are repaired like the rest of the fleet's, and lift a vehicle of up to 4 tonnes on its hook |
| What it stops them doing | Launching heavy on a hot day beyond its engines' lift; building it from steel and components alone |
| How it changes play | A lost engine is a reason to lighten the load and go home on the other's one-engine-out rating; its hook makes it the fleet's light lifter beside the Cherokee |
| What outside it notices | Another mod's air defence sees its real size and speed and is fooled by its flares; colonists repair its parts and load its dispensers; a raider's Black Hawk runs the same systems |

## Figures

In the loadout file with their sources (all secondary): the T700-GE-701D's 1,716 shp continuous, 1,994 shp for 10 minutes and 2,000 shp
with one engine out (Army Recognition); 4,819 kg empty (Helis); 9,000 lb on the hook and a 4,627 m ceiling for the family (PlanesDB); the
APR-39 and ALE-47 (DOT&E; FAS). Authored: the rotor drive's limit, the fuel mass, the flare count and its air-defence track.

## Other factions' vehicles

A raider's or an enemy base's Black Hawk gets the same systems; its parts can be shot out on the ground, and a captured one keeps them.

## Risk

Nothing new puts the player's things at risk: it joins the existing systems, each behind its own setting.
