# Asset Shopping List — BA5

> Owned by **Gia Khang & Đức Anh** (Business Analyst, [CYCLE-2-TASKS.md § 4](CYCLE-2-TASKS.md)).
> Feeds into `GAP-ANALYSIS.md`'s asset section and expands
> [CYCLE-2-TASKS.md § 6](CYCLE-2-TASKS.md).
>
> **Scope of this draft.** Focused on "make the environment look less cheap and
> close the A2 terrain/decoration gap" — the thing Khang was specifically asked
> to chase. It does not re-research the other rows already tracked in
> [CYCLE-2-TASKS.md § 6](CYCLE-2-TASKS.md) (character animations for D5, the 3
> NPC models, UI sounds, ambience loops, footsteps, extra music) — those are
> still open and still Khang's, just not re-covered here.
>
> Per [ASSETS.md](ASSETS.md)/BA5: **Khang's half ends at licence + staging.**
> Whoever has the editor (Đức Anh) imports and writes the `CREDITS.md` row —
> rows below are written ready to paste, per the BA5 template.

---

## Tier 0 — already owned, zero sourcing needed, zero new licence

**The single biggest finding of this pass: the project already imported roughly
130 terrain/decoration assets it has never used.** `Kenney_NatureKit/` and
`Kenney_SurvivalKit/` were both downloaded whole (per [ASSETS.md](ASSETS.md)'s
workflow — "drop the pack in, don't cherry-pick files"), but `ArtPass.cs` and
the level builders only ever wired up a handful of rows from each. Nothing
here needs Khang's sourcing time at all — it's already licensed (CC0, already
in `CREDITS.md`), already downloaded, already sitting in
`Assets/Models/ThirdParty/`. It only needs **Đức Anh's editor time**: a new
`ArtPass.cs` row or a small addition to a level builder, per
[PRODUCT-BACKLOG.md](PRODUCT-BACKLOG.md)'s B1–B4.

| Gap it closes | Files (in `Kenney_NatureKit/` unless noted) | Count |
|---|---|---|
| **Living trees** (B1) | `tree_oak.fbx`, `tree_default.fbx`, `tree_pineRoundA-F.fbx`, `tree_pineTallA-D.fbx`, `tree_fat.fbx`, `tree_plateau.fbx`, `tree_detailed.fbx`, + `_fall` (autumn) variants of each | ~60 living variants (vs. the 1 dead variant currently used) |
| **Terrain elevation — cliffs** (B2) | `cliff_block*`, `cliff_corner*`, `cliff_slope*`, `cliff_steps*`, `cliff_waterfall*` (`_rock` and `_stone` finishes) | ~60 modular pieces |
| **Terrain elevation — water** (B2) | `ground_riverBend/Corner/Cross/End/Open/Rocks/Side/Split/Straight/Tile.fbx` | 13 tiles |
| **Terrain elevation — crossings** | `bridge_center_stone/stoneRound/wood/woodRound.fbx`, `bridge_side_*`, `bridge_stone.fbx`, `bridge_stoneNarrow.fbx` | ~10 pieces |
| **Ground that reads as ground** (B3) | `ground_grass.fbx` — a grass-textured floor **tile mesh**, not a scatter prop; a direct drop-in candidate for `Greybox_Floor` | 1 (tileable) |
| **Village structures** (B4) | `Kenney_SurvivalKit/structure.fbx`, `structure-floor.fbx`, `structure-metal.fbx`, `structure-metal-floor.fbx`, `structure-metal-wall.fbx`, `structure-metal-doorway.fbx`, `structure-canvas.fbx` (only `structure-roof.fbx` is currently used, for `Greybox_Hut`) | 7 unused pieces |

**Nothing to stage or credit here — already in `CREDITS.md` under "Nature Kit —
by Kenney" and "Survival Kit — by Kenney."** Just hand this table to whoever
does the B1–B4 editor work.

---

## Tier 1 — genuinely needs new sourcing

### Ground/wall texture — recommend AGAINST sourcing one

Investigated and deliberately **not** recommending this, so the reasoning
doesn't get re-derived later: `Kenney_NatureKit/`'s `Textures/` folder is
empty — confirmed on disk — because Kenney's Nature Kit is a flat-shaded,
vertex-color pack by design; it was never meant to carry a ground texture.
Sourcing an external tileable dirt/grass texture (e.g. OpenGameArt's
["CC0 Terrain Textures"](https://opengameart.org/content/cc0-terrain-textures)
collection, by *thrashplay*, CC0 — but it's a meta-collection bundling six
separate sub-packs with no single direct-download link, so staging it is more
work than it looks) would also be a **style mismatch**: photographic/PBR-ish
textures next to Kenney's flat-shaded low-poly models tend to look worse, not
better. Kenney's own **Prototype Textures**
(https://kenney.nl/media/pages/assets/prototype-textures/a88c69fa18-1677578307/kenney_prototype-textures.zip,
CC0, 75 files, confirmed) is the "proper" Kenney texture product here, but
it's explicitly a grid/whitebox debug texture set — using it on the farm
ground would read as *more* placeholder-looking, not less.
→ **Use Tier 0's `ground_grass.fbx` / denser prop scatter instead.** No
licence step needed because nothing new is imported.

### Skybox — try the free option first

Unity's built-in procedural sky is tunable per-scene (sun angle, atmosphere
thickness, exposure — all just `RenderSettings`/Lighting-window values, no
asset import) and `SceneLook.cs` already differentiates the three scenes'
ambient/fog/post-processing, so tuning the procedural sky to match (warm haze
for the farm, cold overcast for the factory) is very likely free. **Only
source a real skybox asset if that isn't enough** — if so, come back to this
row and Khang can pull a CC0 skybox cubemap set (search terms: "CC0 skybox
low poly", Poly Haven's HDRIs are CC0 and free but photographic, which would
need the same style-mismatch judgment call as the ground texture above).

---

## Pre-written CREDITS.md rows

Only needed **if** the Tier 1 skybox path is taken (Tier 0 needs no new
credit — it's already covered by the existing Nature Kit / Survival Kit
entries). Template, ready to paste and fill in once a specific asset is
picked:

```
### <Skybox pack name> — by <Author>
- **Used for:** per-scene sky (Level 1 / Level 2 / hub)
- **Source:** <URL>
- **Author:** <name / handle>
- **License:** <CC0 / CC-BY / pack terms>
- **Files:** Assets/Models/ThirdParty/<PackName>/ (kept intact with its LICENSE/README)
- **Modifications:** <none, or per-scene tint>
```

---

## Handoff

Per [CYCLE-2-TASKS.md § 4](CYCLE-2-TASKS.md): Khang's half (this document)
ends here. **Đức Anh (has the editor) owns the import** for anything in Tier
1 that gets picked, and owns **all** of Tier 0 — which is pure editor/code
work (`ArtPass.cs` rows, level-builder additions), not sourcing, so it never
needed Khang's browser time in the first place.
