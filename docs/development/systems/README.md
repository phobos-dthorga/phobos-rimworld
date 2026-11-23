# How each system works now

Living pages, one a system, written fresh from the code and kept current by every change that alters the system. Why a system
is the way it is lives in its [decision log](../decisions/README.md) and in the [archived records](../archive/README.md).

- [Engines and drivetrains](engines-and-drivetrain.md) (23 Nov 2026): engine kinds, gears and the speed cap, loads, fuel,
  starting, failure, thin air and descents. Living; the pilot.

## The template

1. Title, then `**Status (date):**` in one line: what is built, and which test-list rows the owner has passed.
2. `## Read this first (gotchas)`, then `## What it does` (what a player meets, as mechanism) and `## How it works` (data
   flow in Mermaid, the per-tick and per-rare-tick path, where the numbers come from).
3. `## Code` (file, what it holds), `## Defs and XML` (each type linked to the reference site), `## Settings` (labels and
   defaults; link the settings reference), `## Saved data and removal`, `## Tie-ins`, `## Performance` (measured, with source).
4. `## Limits and known gaps`, `## Tests` (rule tests, Python tests, test-list rows), `## Why it is this way` (links only).
5. No dated history and no "the owner said" outside the Status line and the decision log; real-world figures keep their
   sources, and authored values and simplifications say so.
