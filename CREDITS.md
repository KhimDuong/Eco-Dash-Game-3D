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
row below. We credit anyway, per our policy. Imported in **B5** (the P3 art pass).

### Nature Kit — by Kenney
- **Used for:** Level 1's trees, dead trees, rocks, bushes, grass, flowers,
  mushrooms, stumps, fence posts and the teleport gate's stone ring.
- **Source:** https://kenney.nl/assets/nature-kit
- **Author:** Kenney (Kenney Vleugels) — https://kenney.nl
- **License:** CC0 1.0
- **Files:** `Assets/Models/ThirdParty/Kenney_NatureKit/` (FBX + License.txt)
- **Modifications:** none to the models. The pack's OBJ/GLTF/DAE/STL copies and
  isometric sprite renders were dropped on import — FBX only, to keep the repo small.

### Survival Kit — by Kenney
- **Used for:** the chest, crates, barrels, litter bottles, item pickups, the
  rustic hut and the hub's crafting bench.
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

> Read via `.glb`, which Unity cannot import on its own — hence the
> **`com.unity.cloud.gltfast`** package added to `Packages/manifest.json` in B5.

## Audio — third-party

`Assets/Audio/` was copied verbatim from the 2D repo during **A5** (the migrated
`HUD.prefab` and the menu/story scenes reference these clips by GUID). Rewiring
them per scene and any new ambience remains **Dev C, task C5**.

### "Good Morning" — by You're Perfect Studio (composer: Cakeflaps)
- **Used for:** background music (Main Menu, story scenes, Level 1 — looping).
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

| File | Used for |
|------|----------|
| `Assets/Audio/SFX/seed_shoot.wav` | Seed projectile fire |
| `Assets/Audio/SFX/core_collect.wav` | Energy Core pickup |
| `Assets/Audio/SFX/player_hurt.wav` | Greenie takes damage |
| `Assets/Audio/SFX/slime_death.wav` | Plastic Slime defeated |
| `Assets/Audio/SFX/item_pickup.wav` | pickup / story-slide advance cue |
| `Assets/Audio/SFX/win_fanfare.wav` | Level-clear victory cue |

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
