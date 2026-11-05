# BedsideOxygenStand

**AI-generated, then picked by the owner on 5 November 2026.** Made on 2026-11-05 with FLUX.2 Klein 4B base (Black Forest Labs, Apache 2.0) in local ComfyUI: 50 steps, guidance 4, seed 11.

**Made with a LoRA trained on game art.** The run used a private LoRA that was trained on 60 upscaled vanilla RimWorld sprites, and two vanilla sprites (the vitals monitor and the oxygen pump) as references. The LoRA and its dataset are not shared and not in this repository. No game art is in this file. Ludeon's EULA allows its resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers this is unsettled. The owner chose to ship it: see the [design record](../../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. Against the training sprites and references, the closest was FleshmassPieceC.png (structure 0.496, hash distance 30). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Prompt:** phobosvanilla, A bedside oxygen stand: a slim pole unit with a flowmeter, a small clear humidifier bottle and a coil of clear tubing with a face mask hanging from it. Draw it in exactly the art style of the reference images: a game sprite seen from above at a slight angle, with a thick near-black outline around every shape, soft flat cel shading, a muted palette, rounded panel corners and a few small clean details. One object, centred, on a plain pure white background. No floor, no cast shadow, no text, no labels, no people.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- LoRA SHA-256: `392b164f1bbe52b0cfd96c629e31e3214bd2571c0de1ab1ab3e8f8d366a88ef6`
- Master SHA-256: `361400259f9fa8164ed1fd114f3acfd76a3a7e413a23bda1b6cdec555c665e0a`
- This file: 128 x 128 px, from `scripts/art/export_sprite.py --all-white`. SHA-256 `934a2da5bcb0086955a7aba7a4d834b1ff05345646fa49cb573c2cf4a849f5a9`.
