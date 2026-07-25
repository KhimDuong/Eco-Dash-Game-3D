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
| `CameraFollow` | keeps the **class name and `Instance`/`Shake(duration, magnitude)` API** so ported callers (`PlayerHealth`, bosses) compile untouched. A2 shipped it as a plain ¾ follow rig (pitch/yaw/distance knobs, screen-plane shake); **A4 swaps the follow half for a Cinemachine vcam + impulse source behind the same two members** |
| `DynamicYSorter` | **deleted** — depth is free in 3D |
| `Environment/*` cosmetics | rebuilt as cheap particles/transform animation, or dropped |

## Runtime object graph (per level — deltas from 2D only)

```
Player.prefab
├─ CharacterController + PlayerController (XZ move) + PlayerHealth/Shooter/Interactor
├─ Visual (child)  ← rotates to face movement; PlayerAnimator hover-bob/squash; mesh here
└─ (no Rigidbody2D, no SpriteRenderer, no sorting)

CameraRig: Main Camera + CameraFollow (fixed ¾) → A4: + CinemachineCamera (follow=Player)

Level geometry: greybox-kit prefab instances (colliders, static) + baked NavMesh
Everything else (GameManager, HUD canvas, chests, portals, NPCs, quest/codex/
inventory wiring) is structurally identical to the 2D scenes — see the 2D
architecture doc.
```

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
