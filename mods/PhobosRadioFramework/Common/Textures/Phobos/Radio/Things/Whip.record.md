# Whip

**AI-generated, then picked by the owner on 7 November 2026.** Made on 2026-11-07 with ComfyUI 0.41.0, local, FLUX.2 Klein 4B base fp8 (Black Forest Labs, Apache 2.0): 50 steps, guidance 4, seed 12.

**A stand-in:** a thin lattice mast drawn for the mast, used for the whip antenna until a whip is picked (owner, 7 November 2026).

**Made with a LoRA trained on game art.** The run used a private LoRA that was trained on 60 upscaled vanilla RimWorld sprites. The generator was given the game's vitals monitor and pod launcher sprites as references. The LoRA and its dataset are not shared and not in this repository. No game art is in this file. Ludeon's EULA allows its resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers this is unsettled. The owner chose to ship it: see the [design record](../../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. The closest was AngledGravshipHull_northwest.png (structure 0.302, hash distance 31). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Prompt:** phobosvanilla, A tall steel lattice radio mast standing upright, with three guy wires, a small antenna at the top and a concrete footing, drawn tall and narrow. Draw it in exactly the art style of the reference images: a game sprite seen from above at a slight angle, with a thick near-black outline around every shape, soft flat cel shading, a muted palette, rounded panel corners and a few small clean details. One object, centred, on a plain pure white background. No floor, no cast shadow, no text, no labels, no people.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- LoRA SHA-256: `392b164f1bbe52b0cfd96c629e31e3214bd2571c0de1ab1ab3e8f8d366a88ef6` (the first private LoRA, phobos-private-vanilla)
- Master SHA-256: `ce620387bc537a5207b59676f2f99ce8398bd02fef107396b6d66433d859012a`
- This file: 128 x 192 px, from `scripts/art/export_sprite.py --all-white --threshold 250 --soft 20 --aspect 2:3`. SHA-256 `7767fdcaa2e2f97bb2464db76558ec4e7a5e0600b1bbb820754dbca0b1c07e76`.
