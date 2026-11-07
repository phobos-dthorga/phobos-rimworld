# BurnUnitBed

**AI-generated, then picked by the owner on 5 November 2026.** Made on 2026-11-05 with FLUX.2 Klein 4B base (Black Forest Labs, Apache 2.0) in local ComfyUI: 50 steps, guidance 4, seed 11.

**Made with a LoRA trained on game art.** The run used a private LoRA that was trained on 60 upscaled vanilla RimWorld sprites, and two vanilla sprites (the hospital bed and the vitals monitor) as references. The LoRA and its dataset are not shared and not in this repository. No game art is in these files. Ludeon's EULA allows its resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers this is unsettled. The owner chose to ship it: see the [design record](../../../../../../../docs/development/art-automation-and-medical-equipment.md).

**Near-copy check:** passed. Against the training sprites and references, the closest was DoubleSlabBed_south.png (structure 0.428, hash distance 22). Flagged at a hash distance of 12 or less, or structure of 0.6 or more. A slightly tilted copy can still pass.

**Prompt:** phobosvanilla, A burn unit bed: a hospital bed with a steel frame and a thick air mattress made of many soft pale inflated cells, a small blower unit at the foot and a raised frame that holds the sheets off the patient. Draw it in exactly the art style of the reference images: a game sprite seen from above at a slight angle, with a thick near-black outline around every shape, soft flat cel shading, a muted palette, rounded panel corners and a few small clean details. One object, centred, on a plain pure white background. No floor, no cast shadow, no text, no labels, no people.

- Model SHA-256: `44bab3a86fe98b85d21dd2a4729ebdc3ae51fb8a39f76e457e18c724219e6840`
- LoRA SHA-256: `392b164f1bbe52b0cfd96c629e31e3214bd2571c0de1ab1ab3e8f8d366a88ef6`
- Master SHA-256: `25451621cf9465c74babfc76c5d14cc74d03992a97575a93e823cffadb70f842`
- South: 256 x 256 px, from `scripts/art/export_sprite.py`. SHA-256 `c3ae4fee5079c082f81bb94bde1d0db3e8bf74f71555616879784fe8bcb6e8f0`.
- North and east: the south view turned 180 and 90 degrees with Pillow, since the art is from directly above. SHA-256 `5c20799b626a75c415a7859d2baee509f508a87ab27189b0399dfee89db3e979` and `dfe51b93a43d34b91841c350671d77c05137ee9b02abd0060234dc5c3d5c0e7a`.
