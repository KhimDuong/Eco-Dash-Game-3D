# Credits & Attributions — Eco-Dash 3D

Eco-Dash 3D is a **university Game-Development course project (not published,
not monetized)**. We still acknowledge every borrowed asset here, per our
[asset policy](ASSETS.md). If you add/borrow any asset, **add a row below.**

Template for a new entry:

```
### <Pack/Asset name> — by <Author>
- **Used for:** <what in the game>
- **Source:** <URL>
- **Author:** <name / handle>
- **License:** <CC0 / CC-BY / pack terms / unknown>
- **Files:** Assets/Models/ThirdParty/<PackName>/ (kept intact with its LICENSE/README)
- **Modifications:** <recolors, rescale, splits — or "none">
```

## 3D models & materials — third-party

All of it is **CC0** (public domain) — no attribution is legally required for any
row below. We credit anyway, per our policy. Imported in **B5** (the P3 art pass),
except the Fantasy Town Kit, which cycle 2 added.

### Nature Kit — by Kenney
- **Used for:** Level 1's trees (living and dead), rocks, bushes, grass, flowers,
  mushrooms, stumps, fence posts, the teleport gate's stone ring, and — new in
  cycle 2 — the cliff blocks that make up the highland mesa and the hills beyond
  every boundary wall, plus the spring's bank props, lilies and canoe.
- **Source:** https://kenney.nl/assets/nature-kit
- **Author:** Kenney (Kenney Vleugels) — https://kenney.nl
- **License:** CC0 1.0
- **Files:** `Assets/Models/ThirdParty/Kenney_NatureKit/` (FBX + License.txt)
- **Modifications:** no mesh is touched. The pack's OBJ/GLTF/DAE/STL copies and
  isometric sprite renders were dropped on import — FBX only, to keep the repo small.
  **All 23 of its material colours are re-authored at spawn time** by
  `ArtKit.NaturePalette`: this is the one pack here that ships no texture, and the
  colours baked into its FBX files are a washed-out pastel set (`leafsGreen` imports
  as turquoise, `dirt` and `stone` as near-white), which is why the valley's grass
  and rocks rendered cyan until cycle 2. The originals are left untouched on disk;
  the repaint writes shared copies under `Assets/Models/Materials/ThirdParty/`.

### Fantasy Town Kit — by Kenney
- **Used for:** the village. `Greybox_Hut` (the 2D layout's four huts) and
  `Greybox_House` are assembled from its modular `wall-wood` / `wall` panels and
  `roof-point` cap; the market stalls, hand cart, fountain and lanterns are its
  single-piece props, in both Level 1's village district and the hub's yard.
- **Source:** https://kenney.nl/assets/fantasy-town-kit
- **Author:** Kenney — https://kenney.nl
- **License:** CC0 1.0 (stated in the pack's own `License.txt`, kept alongside it)
- **Files:** `Assets/Models/ThirdParty/Kenney_FantasyTownKit/` (167 FBX + Textures +
  License.txt); the GLB and OBJ copies and the per-model preview renders were dropped
  on import.
- **Modifications:** the pack's texture ships as `variation-a.png` while its FBX
  materials ask for one named `colormap`, so Unity's recursive-up material search
  bound all 167 models to **Cube Pets'** atlas instead. Renamed to `colormap.png`
  inside the pack so each model finds its own. Roofs are painted with a solid
  thatch/terracotta material because the shipped variation's roof band is lavender.

### Survival Kit — by Kenney
- **Used for:** the chest, crates, barrels, litter bottles, item pickups, the
  hub's crafting bench and the crates, barrels and planks stacked in its yard.
  (It supplied the rustic hut until cycle 2 — this pack has no houses in it, only
  open scaffold frames, so the village moved to the Fantasy Town Kit.)
- **Source:** https://kenney.nl/assets/survival-kit
- **Author:** Kenney — https://kenney.nl
- **License:** CC0 1.0
- **Files:** `Assets/Models/ThirdParty/Kenney_SurvivalKit/` (FBX + Textures + License.txt)
- **Modifications:** none to the models; non-FBX format copies dropped on import.

### Factory Kit — by Kenney
- **Used for:** **Greenie himself** (the `oopi` robot), the **Mega-Smog boss**
  (`machine-fortified` flanked by two `hopper-high-round`), Level 2's machinery,
  pipes, screens, crates, warning posts, the sweeping-laser emitter, the manhole
  lid, the stage/return portal doorways and the hub's recycling hopper.
- **Source:** https://kenney.nl/assets/factory-kit
- **Author:** Kenney — https://kenney.nl
- **License:** CC0 1.0
- **Files:** `Assets/Models/ThirdParty/Kenney_FactoryKit/` (FBX + Textures + License.txt)
- **Modifications:** none to the models; non-FBX format copies dropped on import.

### Cube Pets — by Kenney
- **Used for:** Ông Bear, the hub shopkeeper (`animal-polar`).
- **Source:** https://kenney.nl/assets/cube-pets
- **Author:** Kenney — https://kenney.nl
- **License:** CC0 1.0
- **Files:** `Assets/Models/ThirdParty/Kenney_CubePets/` (one model + Textures + License.txt)
- **Modifications:** only `animal-polar` was imported out of the 24-animal pack, and
  it is **tinted warm brown** at runtime (`MrBearFur` material) — Kenney's only bear
  is a polar bear and Ông Bear is a brown one.

### Slime · Robot Enemy Flying Gun · Farmer — by Quaternius
- **Used for:** `PlasticSlime` (Quái Rác Nhựa), **`SlimeKing`** (Slime Chúa — the same
  slime mesh at twice the size in bruised-purple), `PollutionFlyBot`, and every human
  NPC — Ông Sáu as-is, Bà Tư and Tí as recolours of the same farmer mesh.
- **Source:** https://poly.pizza/m/LyjSUKHKnh · https://poly.pizza/m/UDTM6X1y9a ·
  https://poly.pizza/m/7pn3R6hPvE (via [Poly Pizza](https://poly.pizza))
- **Author:** Quaternius — https://quaternius.com
- **License:** CC0 1.0
- **Files:** `Assets/Models/ThirdParty/Quaternius/` (GLB + License.txt)
- **Modifications:** materials converted from glTFast's Shader Graph to URP/Lit so
  `HitFlash`/`MaterialTint` can drive `_BaseColor`; Bà Tư and Tí are colour variants
  of the farmer; the flying robot's two **baked-in directional lights were deleted**;
  a one-state Idle animator controller is generated per model. All by
  `Assets/Editor/ArtPass.cs` — the source files are untouched.

### 3D Medicinal Herb Model (Marijuana Plant)
- **Used for:** Ông Sáu's Medicinal Herb quest collectible (`Herb.prefab`) in Level 1.
- **Source:** User provided package (`6y992g5owvi8-marijuanna.rar`).
- **Files:** `Assets/Models/Custom/HerbModel/` (`marijuanna.fbx`, `marijuanna.obj`, textures).
- **Modifications:** Model rotated -90° X-axis upright, local Y position aligned to -0.05m to sit flush on terrain ground, scaled to 0.46m height (1/3 scale), and textured with URP Lit materials (`Mat_Marijuana_Leaf` & `Mat_Marijuana_Branch`).

> Read via `.glb`, which Unity cannot import on its own — hence the
> **`com.unity.cloud.gltfast`** package added to `Packages/manifest.json` in B5.

## Audio — third-party

`Assets/Audio/` was copied verbatim from the 2D repo during **A5** (the migrated
`HUD.prefab` and the menu/story scenes reference these clips by GUID). **C5 wired
them up**: every sound now goes out through `Sfx`, and the music through
`MusicPlayer` reading `Assets/Resources/MusicKit.asset`. No new audio was sourced
or generated — the eight files below are the whole soundtrack, which is why
several of them do double duty (see `Assets/Editor/AudioPass.cs` for the table of
what plays where).

### "Good Morning" — by You're Perfect Studio (composer: Cakeflaps)
- **Used for:** the background music, in **every** scene — menu, both story
  scenes, both levels and the hub — looping continuously across scene changes.
  File: `Assets/Audio/Music/good_morning.ogg`.
- **Source:** https://opengameart.org/content/good-morning (OpenGameArt.org)
- **License:** CC-BY 4.0 / OGA-BY 3.0 / CC0 (multi-licensed). We credit per CC-BY.
- **Modifications:** none.

### "Game Over Sound (Old School)" — by den_yes
- **Used for:** the lose/defeat jingle (`EndScreenController.loseSfx`). File:
  `Assets/Audio/SFX/lose_jingle.ogg`.
- **Source:** https://opengameart.org/content/game-over-soundold-school (OpenGameArt.org)
- **License:** CC0 (public domain).
- **Modifications:** none.

## Audio — generated (AI, Coplay), carried over from the 2D project

Generated with Coplay's `generate_sfx` (a paid AI audio service) for the 2D game
and reused here as project-original. Per [ASSETS.md](ASSETS.md), generation stays
a last resort — these are already paid for, so they carry over rather than
being regenerated.

| File | Used for (after C5's audio pass) |
|------|----------|
| `Assets/Audio/SFX/seed_shoot.wav` | Greenie's Seed, the fly-bot's orb, the Mega-Smog's spray, the sweeping laser |
| `Assets/Audio/SFX/core_collect.wav` | Energy Core, keycard, chest core, both portals, the boss door, the shop till |
| `Assets/Audio/SFX/player_hurt.wav` | Greenie takes damage |
| `Assets/Audio/SFX/slime_death.wav` | every enemy death (slime, King, fly-bot, boss) and the manhole trap |
| `Assets/Audio/SFX/item_pickup.wav` | pickups, cleaning trash, chests, lore notes, the crafting bench, dialogue blips, story-slide advance |
| `Assets/Audio/SFX/win_fanfare.wav` | Level-clear victory cue, and powering a stage portal open |

## Original (made for this project)

| Asset | Where | Notes |
|-------|-------|-------|
| Greybox kit (floors/walls/props/enemy stand-ins) | `Assets/Prefabs/Greybox*`, `Assets/Models/Materials/Greybox_*` | ProBuilder/primitive placeholders — replaced in P3, listed in [ASSETS.md](ASSETS.md) |

## Fonts

**TMP Essential Resources** (LiberationSans SDF + shaders) were imported from
`com.unity.ugui` in A5 — they ship with Unity, so no external attribution is
needed. Vietnamese diacritics render through the stock
`LiberationSans SDF - Fallback` (dynamic atlas), exactly as in the 2D build.
Add a row here only if another font is imported.
