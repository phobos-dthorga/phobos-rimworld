# OxygenConcentrator

**AI-generated, then picked by the owner on 7 November 2026.** Made on 2026-11-05 with FLUX.2 Klein 4B base (Black Forest Labs, Apache 2.0) in local ComfyUI: 50 steps, guidance 4, seed 103.

**No LoRA.** The generator was given our own drawing `OxygenConcentrator-large.png` (kept on the `art-references` branch as `drawings/OxygenConcentrator-large.png`) and the game's vitals monitor sprite as references, for the layout and the game's look. No game art is in this file. See the [design record](../../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. The closest was FleshmassPieceC.png (structure 0.374, hash distance 25). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Prompt:** The machine in the first reference image, a bedside oxygen concentrator: a squat pale grey medical cabinet on small castors, with a dark window showing two upright green sieve columns, a round blue flow dial, a small dark slot under the dial, a darker grey band along the base and a loop of clear tubing at the lower right. Keep its shape, layout and colours. Redraw it in exactly the art style of the second reference image: a game sprite seen from above at a slight angle, with a thick near-black outline around every shape, soft flat cel shading, a muted palette, rounded panel corners and a few small clean details. One object, centred, on a plain pure white background. No floor, no cast shadow, no text, no labels, no people.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- Master SHA-256: `d11b6400ece14c522da422960703e51b790598681f4c98f8433fe87c533cff7f`
- This file: 128 x 128 px, from `scripts/art/export_sprite.py --all-white --threshold 250 --soft 20`. SHA-256 `1b253239e6e80ab044003c07f0ca4c1d07ed25fe9adb542fdb2f8a02bb4eba6d`.
