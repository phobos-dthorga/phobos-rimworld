# FlatbedTrailer textures: asset record

**Its own picture since D'thorga Vehicles 0.49.0.** Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** (attempt 2 of 4) from a labelled sheet that showed each attempt alone and behind the tipper at in-game size. Its only reference was our own Talus tipper body (`tipper-body-north-3.png`, for style). No game or other author's art was uploaded or used as a reference, and it passed the near-copy check against the game's art. The prompt is in `assets/phobos-vehicles/requests/trailer-pilot.json`; the review, spend and picks are in [the record](../../../../../../assets/phobos-vehicles/requests/round-three-record.md). Until then it borrowed the tipper's body (owner, 11 Nov 2026).

**Body:** `flatbed-trailer-body-north-2.png`, SHA-256 `ba35c6fef2d9610ca2cce3084210cd2b36ca2d336bfee67fc06401c2e31121d6`, exported by `scripts/art/export_vehicle.py body --cells-long 20/3 --draw 4 8 --min-saturation 0.16`, with five `--hole` points: four clearing the background the drawbar's frame encloses, and one (384 66) the towing eye's opening, which the background AI took for metal. East and south are the north turned by the script, with colour masks (suffix m) and an icon. 2.06 cells wide, at 128 px a cell.

**Re-exported 13 Nov 2026 for D'thorga Vehicles 0.54.1**, when the trailer grew to 3 x 7 cells drawn at 4 x 8: from the same original, at the same look (5 cells long in a 3 x 6 picture became 20/3 in 4 x 8), so it is drawn at 128 px a cell instead of being stretched to about 96. The boards were refitted with `assets/phobos-vehicles/export_round_three.py --only FlatbedTrailer`. Checked by eye on dark ground beside the old picture.

- `FlatbedTrailer_east.png`: SHA-256 `4a6ae2bb30e7300423688331879a3d14a6560f8214504b5ccc735c7bfbfe68ca`
- `FlatbedTrailer_eastm.png`: SHA-256 `645c69e424e3d5c3ef5489d30e625defa5412d75da750cf40c706787a1732629`
- `FlatbedTrailer_icon.png`: SHA-256 `e55c9a49640384cabe67a8d726b6798ffbaca59202e769be34e7e369edffbb64`
- `FlatbedTrailer_north.png`: SHA-256 `3e58e0d0101acceae9f3bd5681bc1b9e53e5336c60b0fb0f461d4574931d2f5a`
- `FlatbedTrailer_northm.png`: SHA-256 `30a3415991944365e786e72df8bae9bfb0a8905115f862f542dbcb7fc03129a0`
- `FlatbedTrailer_south.png`: SHA-256 `9824b0b2980f6838af4531e6a98a6ac8c1d0309a0d4d2c94c756eed48425ccbc`
- `FlatbedTrailer_southm.png`: SHA-256 `b5fcc355ba2fa0099fbe506e83edceb4861501cee34b85b5bd9758aaf978de47`

## The greedy boards

`FlatbedTrailer_GreedyBoards`, shown while the greedy boards upgrade is fitted: `flatbed-trailer-greedy-boards-2-shaped-324x1244.png` (pick 2 of 2, SHA-256 of the original `b1e178eb6cc04e4a7152c61156bd7d312e78fe77373e73eaa081406d550df08d`), stretched by script to the deck's rails as a new file and fitted over the deck; its empty middle is cleared as background. North SHA-256 `201216855b15171999b6f4340d5b28f0cd56d36237a7f7292c006a23a424020f`, east `0df38abbcbbe986e2ec7cc5eb423174938eebd9c21487e21509aaedbc63e5f38`, south `2fef799330d8abca1122c134b95e8021cdc4dbc6c832466bcc254f41a9ce6999` (refitted 13 Nov 2026, Vehicles 0.54.1).

## The deck tent (pilot, 22 Nov 2026)

`FlatbedTrailer_DeckTent`, shown while the deck tent fitting is fitted (D'thorga Vehicles 0.101.0): the owner's "one pilot first" of camp gear
drawn on our vehicles. **AI-generated**: OpenAI gpt-image-2 through the image API, medium quality, an edit of a crop of the picked body's deck
(`assets/phobos-vehicles/references/deck-tent-crops.json`, box 64,256 to 704,1536, our own art the only reference; paid rule 13), two attempts,
about US$0.06. The tent was lifted out by `scripts/art/sam_lift.py --cpu` (Segment Anything 2.1, Meta, Apache 2.0) and laid back at the crop's
place, then exported by `scripts/art/export_vehicle.py overlay` at the body's own transform. **The owner kept both** (22 Nov 2026: "Keep both ... with slightly different qualities to each"): attempt 2 is the light deck tent,
attempt 1 (`FlatbedTrailer_DeckTentRoomy`, D'thorga Vehicles 0.102.0) the roomy one. Request and review: `assets/phobos-vehicles/requests/deck-tent-pilot.json`,
`deck-tent-pilot-record.md`.

- Edit `deck-tent-flatbed-2`, SHA-256 `a4963fa05d8756151c0350cbee1c34111903dc4af98692caa98b5c520b8efbde`
- North `b6e88eedae58518789fcab24a07bf6f66ca45f581627351dd8e09864c9a8d3ac`, east `e7554cfe30b94ca5e47c6b3e33915451fc9aa0162bb201402a86ba9efd7e9c0d`, south `ab03a27624f4c8bf53b0847ba823f46519a37b5e60c6cba2fc155f90a5bfa14b`
- Roomy, edit `deck-tent-flatbed-1`, SHA-256 `487b94765d29b721eb12e38eb1d531533ca2c4355617662c5f6afc512fccff3d`: north `8bf8c51d54b185397c81f9c6f03ebdc5607ecdd5dc776d670c3963eb1ecb7ae3`, east `72719f13a8d0a3ef4014fe405c2846e4a2303a9a25b15397bdf1100bd4a4fa7c`, south `3ddd4eb4ed2de83bee8b2bffc7fdbb2896d26d75e3d27b0af15b55902fde4ab9`
