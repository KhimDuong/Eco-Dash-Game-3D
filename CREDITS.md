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

*(none yet — greybox placeholders only; entries land with the P1 character
pass and the P3 art pass)*

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
