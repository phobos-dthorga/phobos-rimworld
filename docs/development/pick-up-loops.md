# Pick-up and put-down loops: the loop watch (21 Nov 2026)

**Status (21 Nov 2026):** built in D'thorga Framework 0.205.0. Not yet tested in game.

On 21 Nov 2026 the owner traced a repeated pick-up and drop sound to Carlotta, then Yunxing: Simple Sidearms fetched a second copy of
their gun and Combat Extended's loadout dropped it, over and over (D'thorga Patches' guard for that case is in
[soldiers keep their guns](soldiers-keep-their-guns.md#sidearms-that-loop)). The owner chose, by question card, a general watch as well,
for loops from any mods: **"notice and tell"**, never stepping in.

## Read this first

- **It tells, once, and changes nothing**: a message naming the colonist and the thing, with the number of turns, then quiet for a day
  for that colonist and thing.
- **A loop** is a colonist who fetches the same thing (the same item, by its ID) and puts it away again three times within two in-game
  hours. All three figures are authored, labelled in `LoopWatchRule`.
- **What counts as putting away:** a job run by the game's hauling drivers (to a cell, into a container) or a mod's driver built on
  them. Any other job whose first target is that item counts as fetching it.
- **The player's colonists only.** Its memory is a few small records, forgotten once quiet, never saved.
- **A setting**, Notice when a colonist keeps fetching and putting away the same thing, on by default (Mod settings, D'thorga Framework).

## What the behaviour answers for

- **What it lets the player do:** find a loop between two mods' orders at once, by name, instead of by the sound of it.
- **What it stops:** nothing; it never cancels a job or forbids a thing.
- **How it changes the way they play:** a loop becomes a message to act on (change the order that disagrees), not a mystery.
- **What outside it notices:** the player, through the message. Nothing in the game itself.
- **Risk:** none to the player's things or people. The setting is there because a message can be unwanted.

## How it works

- A Harmony postfix on `Pawn_JobTracker.StartJob` (no extension point for a job's start), counting a job only if it really became the
  colonist's current one, since several mods' prefixes may put another in its place (`query-patches.py`, 21 Nov 2026: eight mods patch
  the method).
- `LoopWatchRule` (game-free, tested) counts turns: a turn ends when a putting away follows a fetch; two of the same in a row are one.
- Cost: one setting read and a few field reads per job started; a dictionary step for a colonist's job on an item.

## Limits

- **A loop that swaps the item each time** (a stack split, a new copy fetched) is not seen: it is counted by the item.
- **A loop that never hauls** (taken into a pack, dropped where it stands) is not seen: the putting away must be a hauling job.
