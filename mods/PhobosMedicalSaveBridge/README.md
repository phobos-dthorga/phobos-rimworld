# D'thorga Medical Save Bridge

Lets you add **More Injuries (Continued)** to a colony you are already playing, with **Medical System Expansion 2** or without
it, without wounds vanishing, lost limbs growing back, bionics moving or corpses coming back to life. It acts once, on the first
load of a save written without More Injuries, and does nothing after you save.

Version 0.1.1, built 3 November 2026 for House D'thorga. **The first load was tested in game by the owner on 3 November 2026**
on a copy of a 330-mod colony with Medical System Expansion 2 and Combat Extended, and went cleanly, with 0.1.0 and again with 0.1.1. The
save-and-reload step is not yet tested in game.

## Read this first (gotchas)

- **Back up your save first.** Copy the save file and load the copy. The bridge changes nothing on disk, but the first load after
  adding More Injuries is the one that matters, and a backup costs nothing.
- **Load it after More Injuries and Medical System Expansion 2.** The About file asks for this, so RimSort does it for you. It needs
  Harmony and nothing else. **You have to switch it on yourself**: nothing requires it.
- **Switch on More Injuries and this mod in the same step**, then load the save. The bridge decides what to do from the mod list
  the save was written with: a save that already lists More Injuries gets nothing, because its parts are already numbered the new way.
- **More Injuries' add-ons** (More Injuries: Organ Failure, Medieval Solutions for More Injuries) were not studied. In the owner's
  one test both were switched on with More Injuries in the same step and nothing went wrong; neither adds body parts. If you want
  them, the safest order is still the one the owner used: everything on at once, then read the log.
- **Read the log on that first load.** Every line starts with [D'thorga Medical Save Bridge]. The first says ACTIVE or INERT and names
  the mods the save lacked. After loading it says how many body parts it re-numbered and how many wounds it carried over, then
  checks every pawn, dead or alive, and lists anything that sits on a part the save never had (there should be none).
- **Then save.** A save written with More Injuries makes the bridge inert for good on that colony. You can leave the mod on or
  remove it; neither matters after that.
- **Safe to remove** at any time: it saves nothing. Removing More Injuries itself is More Injuries' business, not this mod's.
- **It helps with any mod that adds body parts**, not just More Injuries: anything the save lacked that adds parts to a body is
  re-numbered the same way. A mod that moves or deletes an existing part is beyond it.
- **Tested in game twice**, by the owner on 3 November 2026: the first load of a copy of the live colony, with 0.1.0 and with 0.1.1.
  The reload after saving and a fresh colony are not yet tested. Everything else here is read from the game's and the mods' code.

## What goes wrong without it

RimWorld saves a hediff's body part as a number: the part's position in the body's part list. More Injuries adds eight parts to
the human body (an abdomen holding two intestines, an artery pair in each leg, and the spinal cord). Every part after the first
new one moves down the list, so on the first load every old wound, lost limb, prosthetic, bionic, implant and surgery bill lands on
the wrong part. Three things then happen at once:

1. Old wounds load as the game's own injury class while More Injuries' XML says they should be its class. Medical System Expansion 2
   sees the mismatch and re-installs them, which fails for every part under a lost limb.
2. More Injuries knows the numbers are wrong and, on its first load, removes every wound and missing part from every pawn, clears
   their surgery bills, and moves bionics it finds on the wrong part to a random right one or deletes them.
3. That clean-up treats every dead pawn as a victim of the bad numbering and sets it back to Mobile. The corpses in the owner's
   colony, 105 of them, came back as living pawns with no mind, needs or guest state, and the colonist bar crashed every frame.

## What it does

All of it only while the gate is open: More Injuries (or another part-adding mod) is active, and the save's own mod list does not
include it.

| Step | What | Where it hooks |
| --- | --- | --- |
| Re-number | Each saved part number is translated from the old list to the new one, for every body that gained parts | The game's own hook for this, BackCompatibility.GetBackCompatibleBodyPartIndex, which every saved part reference passes through |
| Upgrade | An old wound or missing part is created as More Injuries' class straight away, with the bleeding state a live wound would carry; old blood loss and hypothermia get the comp-carrying class More Injuries gives them | The game's own hook for choosing a saved object's class, BackCompatibility.GetBackCompatibleType |
| Hold | More Injuries' first-load flag is cleared as it is set, so its clean-up finds nothing to do | More Injuries' pawn comp, reached by name |
| Report | After the load, what was done and a check of every pawn, written to the log | Game.LoadGame |

It never touches Medical System Expansion 2: with the wounds upgraded there is nothing for it to re-install.

## Checking it worked

Open the log (dev mode, the log button) and search for the bridge's lines. On the first load after adding More Injuries:

- ACTIVE, naming th3fr3d.extendedinjuries and this mod, and "Human (8 new parts)".
- A "Load finished" line with the number of parts re-numbered and hediff classes upgraded, and wipes prevented equal to the number
  of pawns that have More Injuries' comp.
- "Hediffs sitting on parts newer than the save: none."
- No lines from More Injuries saying "Compatibility patch removed some hediff(s)", and none from MSE2 saying "Reinstalling Hediffs
  with wrong type". (0.1.0 still let MSE2 re-make old blood loss and hypothermia; 0.1.1 covers them.)

Then open the health tab of a colonist with a lost limb or a bionic and compare with what you saw before. Two dev-mode actions
help: **Medical Save Bridge: log body part indices** writes the human body's part list with each part's old number, and **check
every pawn's parts** repeats the after-load check.

## Settings

None. Its behaviour is a rule, not a preference.
