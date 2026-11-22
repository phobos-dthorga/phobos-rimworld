# V-22 Osprey textures: asset record

Made on 18 and 19 Nov 2026: **AI-generated**, every master picked by the owner. The body is Google's Nano Banana Pro through fal.ai, the owner's pick of a four-service trial; the overlays and parts are edits by the same model, the later ones drawn on a piece of the body only so the model had no aircraft to redraw. The only references uploaded were our own: the picked body, pieces cut from it, our plan drawing and our own parts. No game or other author's art was uploaded or used. Masters and their records are on the `art-masters` branch (`vehicles/osprey/`); every other attempt is on `art-references` (`generated/vehicles/osprey/`).

**How they were made** (the record of all three rounds, the owner's verdicts and the spend: [osprey-overlays-record.md](../../../../../../assets/phobos-vehicles/requests/osprey-overlays-record.md)):

- **Exported** by `assets/phobos-vehicles/export_osprey.py` at 128 px a cell: the body 20.5 cells nose to tail on a 19 x 21 canvas; east and south are the north turned. The background AI looked at every master first.
- **Paint:** the airframe's grey is the main paint, the orange bands the second; glass and the overlays' hardware stay bare; thin seam lines take the paint of the panels around them (the owner's look of 19 Nov 2026). Repaired with Segment Anything's parts map.
- **Turrets** (the ramp guns) are centred on their mount, barrel up. **Rotor and nacelles** are pieces for our own renderer, at the body's scale; `Osprey.layout.json` gives where they sit.
- **Damage looks** are our bulk tanker's damage marks (OpenAI gpt-image-2, 8 Nov 2026), cut to each part's mask by `derive_osprey_parts.py`. Agent placements, for the owner to revise.
- Not yet seen in game.

| Texture | Master | Made | Master SHA-256 |
| --- | --- | --- | --- |
| Osprey (body, paint mask, icon) | `osprey-trial-nano-banana-pro-1` | The owner's pick of the four-service trial, 18 Nov 2026 | `1de0291ec5c8191e0a37cba45d998bdf9acd6b1f8d98aa3a6d601ee564562f18` |
| Osprey_Probe | `osprey-probe-1` | An edit of the body; lifted with Segment Anything | `fc53dae8e003b809e76d63ae731544d86b1b53388170f4517138adc7131e4fe9` |
| Osprey_RampGun762 | `osprey-ramp-gun-762-1` | An edit of the body; lifted, then shrunk to its real 1.25 m | `2198cd6275dca7599711fbed52b7e4247fffe8bc922e2500c094878bf665aa4d` |
| Osprey_Jammer | `osprey-jammer-add-1` | An edit of the body; lifted and clipped to each dome's measured circle | `bb0769ac3e1aa170218ab92ddf959d5aa9b23b9e84477c2a8b0511c5589117fb` |
| Osprey_RampGun50 | `osprey-ramp-gun-50-piece-2` | Drawn on a piece of the body (the ramp); the owner's pick (b), shrunk to its real 1.65 m | `dd758b8f281eda33de174883a6a5e2f956a5678378faad5c98c93929e01f0687` |
| Osprey_Hoist | `osprey-hoist-piece-2` | Drawn on the ramp piece; the owner's pick (b), its plate measured and added back | `429ab4f2de11d86bdb0bd514feb7d8b2b237ba9b24458bc8f1f48454eb65351c` |
| Osprey_Nacelle*Barrier | `osprey-inlet-barrier-piece-1` | Drawn on a piece holding one nacelle; the owner's pick (a) | `684438c9736559e7b97e9926eb6f85a2a39d1a377bcf45860a3256dc12d553f4` |
| Osprey_Rotor, Osprey_RotorSpin | `osprey-rotor-clean-4k-1` | Redrawn clean alone at 4K; the disc made from it by script | `941a55ad4ce052c91d5535fb4fcf44c501133fe3036bf64b0227594f9ff5997c` |
| Osprey_Nacelle* (pointed cap) | `osprey-nacelle-90-1` | Only its pointed caps are used, on the script-made tilted nacelles | `d12c5edb76480044e2edc338c742fceae08eca9f6b2fbe7ab6244dca258d5e98` |

**The rotor cut again on 23 Nov 2026** (the owner's go, by card): `derive_osprey_parts.py` had cleared only the near-white ground round the redraw, so its fade from the dark outline to the paper stayed as a pale line outside the blades (the edge check's fringe measure). The ground now grows through that pale grey up to the outline, and the softened edge takes the blades' own colours. Osprey_Rotor and Osprey_RotorSpin re-exported; the hub found at the same place.

## Upgrade icons, interim (19 Nov 2026)

`Upgrades/SecondHook`, `ArmourKit`, `InfraredJammer`, `TerrainRadar` and `InletBarrier` are made free by `export_osprey.py` from the
Osprey's own art until the owner picks the icons drawn for them (`assets/phobos-vehicles/requests/osprey-upgrade-icons.json`, queued for
the local model on 19 Nov 2026): the inlet barrier's grille, one jammer dome, the radar nose cap, our cargo hook fitting's icon twice, and
the cockpit and cabins in steel grey. Nothing new was generated for them.
