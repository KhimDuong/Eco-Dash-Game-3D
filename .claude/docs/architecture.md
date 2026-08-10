# Architecture — Eco-Dash 3D

> Target architecture for the 3D port. The 2D repo's
> `d:\Y4-Sem3\Eco-Dash-Game\.claude\docs\architecture.md` remains the detailed
> reference for **system internals** (event graphs, shop/persistence flow,
> quest/codex data model) — those are ported unchanged, so that doc still
> describes them accurately. This doc covers what's *different* in 3D and how
> the port is organized.

## Folder layout (Assets/Scripts) — mirrors the 2D repo

```
Player/       PlayerController, PlayerHealth, PlayerShooter, PlayerInteractor, PlayerAnimator
Enemies/      IDamageable, IKnockbackable, PlasticSlime, PollutionFlyBot, SlimeKing, MegaSmogBoss, EnemyProjectile
Systems/      GameManager, Inventory, SaveSystem, QuestLog, Codex, PlayerProgress, GameSettings,
              Crafting, ItemUse, SceneProgress, CameraFollow (3D ¾ rig + Shake), catalogs
Items/        ItemDef, ItemDatabase, CraftingRecipe, SeedProjectile, pickups
UI/           HudController, ObjectiveTracker, InventoryUI, Hotbar, QuestLogUI, CodexUI,
              CraftingUI, BossHealthBar, Pause/EndScreen/Settings, Dialogue/, UIFactory
World/        IInteractable, Chest, NPCs, portals/gates, BossDoor, CraftingBench,
              ReclamationPatch, Litter, LoreNote, Keycard
Hazards/      ToxicMud, ToxicGasZone, ManholeTrap, SweepingLaser
Shop/         ShopController, ShopNPC, ShopUpgradeRow
```

## Porting tiers (the core organizing idea)

Every 2D script lands in exactly one tier. When touching a script, know its tier.

### Tier 0 — copied verbatim (the game's "brain")
No physics, no rendering assumptions → byte-identical copies from the 2D repo:
all of `Systems/` (except `CameraFollow` and `ItemUse`), all of `UI/` (except
`DynamicYSorter`), `Shop/` (except `ShopNPC`), `Dialogue/`, `MenuController`,
item data classes, `IDamageable`, `IInteractable`, catalogs. **Never "improve"
these while porting** — divergence from the 2D repo is a bug.

Three exceptions the original plan listed as Tier 0 but which are **not**
(verified against the 2D source during A2):

| Script | Why it isn't Tier 0 |
|---|---|
| `Systems/ItemUse` | Seed Bomb AoE uses `Physics2D.OverlapCircleAll` → Tier 1 |
| `Shop/ShopNPC` | `Collider2D` trigger for the shop prompt → Tier 1 (Dev B places it) |
| `Enemies/IKnockbackable` | its impulse parameter is a `Vector2` → see contract changes below |

`UI/BossHealthBar` is Tier-0 code but has a hard reference to `MegaSmogBoss`, so
it ships with Dev C's **C3** rather than with A2.

A5 made the only other edit to Tier-0 UI so far — **additive only, no behaviour
changed**: `EndScreenController.RestartLevel()/LoadMainMenu()` and
`PauseController.LoadMainMenu()` are new pass-throughs to `GameManager.Instance`,
added so `HUD.prefab` can own its own button wiring (see the HUD contract below).

The `sortingOrder` calls all over `UI/` are `Canvas.sortingOrder` (via
`UIFactory.EnsureCanvas`) and are perfectly valid in 3D — only
`DynamicYSorter`'s `SpriteRenderer.sortingOrder` was 2D-specific.

## 3D contract changes (Dev A owns these — everyone codes against them)

Ported 2D→3D, the shared signatures below changed dimension. Callers that pass a
`transform.position` need **no edit** (a `Vector3` was already being truncated);
implementers do.

| Contract | 2D | 3D |
|---|---|---|
| `IKnockbackable.ApplyKnockback` | `(Vector2 impulse, float duration)` | `(Vector3 impulse, float duration)` — keep it on XZ |
| `PlayerHealth.TakeDamage` | `(int, Vector2 sourcePosition, float)` | `(int, Vector3 sourcePosition, float)` |
| `PlayerController.ApplyKnockback` | `(Vector2 velocity, float)` | `(Vector3 velocity, float)`; y is dropped |
| `PlayerController.FacingDirection` | `Vector2`, defaults `Vector2.down` | `Vector3` on XZ (y always 0), defaults `Vector3.back` |
| `PlayerHealth` hit flash | `SpriteRenderer` tint | `Renderer` + `MaterialPropertyBlock` on `_BaseColor`/`_Color` |

`IDamageable.TakeDamage(int)` and all of `IInteractable` are unchanged —
`IInteractable.InteractPosition` was already a `Vector3`.

### Tier 1 — mechanical port (logic identical, physics API swapped)
`Rigidbody2D→CharacterController/Rigidbody`, `Collider2D→Collider`,
`OnTriggerEnter2D→OnTriggerEnter`, `Physics2D.Raycast→Physics.Raycast`,
`Vector2 (x,y) → Vector3 (x,0,z)`. Applies to: `Player/*`, projectiles,
pickups, `World/` triggers, `ToxicMud`/`ToxicGasZone`/`ManholeTrap`.
Tunable values stay numerically identical (1 tile = 1 m).

### Tier 2 — redesigned for 3D
| Script | 3D design |
|---|---|
| `PlasticSlime`, `SlimeKing` | **NavMeshAgent** chase (baked NavMesh per level); contact damage + knockback unchanged |
| `PollutionFlyBot` | hovers at flight height on Y (no NavMesh); 3D raycast LOS masked to `Obstacle`; kite + fire SmogOrbs |
| `MegaSmogBoss` | same phase machine; 8-dir spray → **horizontal ring** of orbs on XZ; gas zones spawn on the arena floor |
| `SweepingLaser` | telegraph→sweep cycle unchanged; beam = emissive scaled cylinder (or LineRenderer) + trigger collider |
| `ReclamationPatch` | **done (B2).** 2D crossfaded two layers of sprite tiles; opaque URP meshes have no alpha, so the 3D patch does what the brief asks — *swap materials in a radius*. A flat disc grows from 0 to `radius` while its colour lerps barren → lush, and ground tiles the wave sweeps over are re-tinted as it passes |
| `CameraFollow` | keeps the **class name and `Instance`/`Shake(duration, magnitude)` API** so ported callers (`PlayerHealth`, bosses) compile untouched. **A4 (done):** the component now lives on the `CinemachineCamera` and only *authors* the rig — pitch/yaw/distance drive `CinemachineFollow.FollowOffset`, and `Shake` fires a `CinemachineImpulseSource` (camera-space listener, so the jitter is still screen-plane). `magnitude` is still peak offset in metres (measured: 0.18 → 0.178 m) |
| `DynamicYSorter` | **deleted** — depth is free in 3D |
| `Environment/*` cosmetics | rebuilt as cheap particles/transform animation, or dropped |

## Runtime object graph (per level — deltas from 2D only)

```
Player.prefab
├─ CharacterController + PlayerController (XZ move) + PlayerHealth/Shooter/Interactor
├─ Visual (child)  ← rotates to face movement; PlayerAnimator hover-bob/squash; mesh here
└─ (no Rigidbody2D, no SpriteRenderer, no sorting)

CameraRig.prefab
├─ Main Camera (CinemachineBrain)
└─ CM_PlayerCam (CinemachineCamera + CinemachineFollow + ImpulseSource/Listener
                 + CameraFollow) ← binds to the "Player" tag on Start, no wiring

HUD.prefab   (one Screen-Space-Overlay canvas — everything the UI layer needs)
├─ HudController / EndScreenController / PauseController  (on the root)
├─ HPBar_BG (Fill + HPText) · CoreText (inactive — ObjectivePanel counts) · TrashText
├─ ObjectivePanel  → ObjectiveTracker (objectives authored per scene as overrides)
├─ InventorySystem → InventoryUI · Hotbar · QuestLogUI · CodexUI (self-build at runtime)
├─ DialogueSystem  → DialogueRunner + AudioSource
├─ WinPanel · LosePanel · PausePanel · Settings (SettingsPanel)
└─ TutorialPopup   (builds its overlay at runtime; H, auto-shows on a fresh run)

GameManager.prefab: bare GameManager (requiredCores authored per scene)

Level geometry: greybox-kit prefab instances (colliders, static) + baked NavMesh
Everything else (chests, portals, NPCs, quest/codex/inventory wiring) is
structurally identical to the 2D scenes — see the 2D architecture doc.
```

### HUD.prefab contract (Dev B: drop it in, don't rewire)

A5 folded the 2D per-scene HUD additions (ObjectivePanel, Settings,
DialogueSystem, TutorialPopup) into the single prefab, so a level scene needs
exactly **one `HUD.prefab` instance + one `GameManager.prefab` instance + one
`CameraRig.prefab` instance** and no manual references.

One deliberate deviation from the 2D wiring: the Win/Lose/Pause "Chơi lại" and
"Về Menu" buttons used to call the **scene's** `GameManager` directly, which a
prefab *asset* cannot reference — in 2D every scene had to re-drag it, and the
reference is silently lost the moment the prefab is re-saved. They now target
pass-throughs on the HUD's own components (`EndScreenController.RestartLevel/
LoadMainMenu`, `PauseController.LoadMainMenu`), which forward to
`GameManager.Instance`. Same behaviour, nothing to re-drag.

Per-scene overrides Dev B *is* expected to set: `ObjectiveTracker.objectives`
(+ `missionTitle`), `HudController.objectiveLabel` (Level 2 counts keycards,
not cores), `EndScreenController.completeScene` (`Ending_Story` after the L2
boss; blank elsewhere), and `GameManager.requiredCores`.

## Level 1 is generated from the 2D layout, not hand-placed

`Assets/Editor/Level1Builder.cs` (menu: **Eco-Dash → Rebuild Level 1 from the 2D
layout**) rebuilds `Level1_BarrenFarm` from `Tools/level1_layout.csv`, which
`Tools/dump_scene.py` + `Tools/export_layout.py` extract from the 2D scene's YAML.
2D `(x, y)` becomes 3D `(x, z)` at 1 tile = 1 m, so the farm keeps its exact
proportions and every landmark sits where players remember it. Re-run it after
changing the CSV; it is idempotent and re-bakes the NavMesh.

Two small helpers came out of the port:

- **`MaterialTint`** — the 2D scripts tinted `SpriteRenderer.color` all over the
  place. Meshes have no colour channel and `renderer.material.color` clones the
  material per object, so tints go through a shared `MaterialPropertyBlock`.
- **`Billboard`** — world-space "Nhấn E" prompts would lie flat on the ground
  under the fixed ¾ rig; this keeps them square to the camera.

### Two traps worth knowing before debugging Level 1

- **`DialogueRunner` pins `Time.timeScale` to 0 while a line is up**, so physics
  stops dead — no trigger events, no gravity. `TutorialPopup` does the same. Since
  `DialogueNPC`'s auto-briefing is an `Invoke` (scaled time), Bà Tư cannot even
  *start* talking until the tutorial popup is dismissed. Anything that looks like
  "triggers are broken" is usually a modal holding the clock.
- **Walk-over triggers are 0.75–0.8 m, not the 2D 0.4 m.** A CharacterController is
  only sampled by the physics step once per frame; at a high or uneven frame rate
  Greenie can step clean over a small sphere without registering.

## Communication patterns (unchanged — frozen contract)

- `GameManager` static-instance + C# events (`OnCoresChanged`,
  `OnAllCoresCollected`, `OnLevelComplete`…); UI subscribes, never polls.
- `IDamageable`/`IKnockbackable` for combat; `IInteractable` + E-key
  `PlayerInteractor` for world objects.
- Persistence via static PlayerPrefs-backed classes (`PlayerProgress`,
  `Inventory`, `QuestLog`, `Codex`, `SceneProgress`) + `SaveSystem` reset.
- Scene flow: `MainMenu(0) → Intro_Story(4) → Level1(1) → [Hub(3) portals] →
  Level2(2) → boss → Ending_Story(5) → MainMenu`.

**These contracts are owned by Dev A** — propose changes, don't just make them.

## Design principles

1. **Parity first, polish second.** A ported feature is done when it behaves
   like the 2D build; only then make it prettier.
2. **The 2D repo is the spec.** Numbers, text, and layouts are copied, not
   re-invented. When a doc here and the 2D repo disagree about *design*, the
   2D repo wins; about *3D technique*, this repo wins.
3. **Prefab-first, scene ownership** — see [../../TEAM-TASKS.md](../../TEAM-TASKS.md).
4. Keep this doc updated: new/changed systems get a row in the Tier-2 table or
   a note here, plus a roadmap tick.
