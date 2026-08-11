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
| `PlasticSlime` | **done (C1).** **NavMeshAgent** chase on the baked NavMesh — the 2D version steered a `Rigidbody2D` straight at a random point, which only works because the 2D farm is an open field. Every stat, drop and damage number is the 2D one. Two things are new: an **aggro radius** (6 m, 9 m hysteresis, 2.5 m/s chase — under Greenie's 5 m/s, plus a `provokeDuration` so a slime sniped from range actually comes for you), and contact damage as a **distance test** rather than `OnCollisionStay`, because a NavMeshAgent moves kinematically and the player is a CharacterController, so that pair never generates collision callbacks at all |
| `SlimeKing` | **NavMeshAgent** chase like the slime; contact damage + knockback unchanged (C3) |
| `PollutionFlyBot` | **done (C2).** Hovers at flight height on Y (no NavMesh) — a downward probe holds it 1.6 m above *whatever is beneath it*, so it clears crates and follows the factory floor instead of sliding on one plane like the 2D "hover" did. LOS is a 3D raycast masked to `Obstacle` **cast along the orb's own flat path from the fire point**, so it can never claim a shot its own orbs would splash on a crate. Kites at `preferredRange` and fires SmogOrbs. Contact damage is a distance test (same reason as the slime). Every stat is the 2D one; `provokeDuration` is carried over from C1 |
| `MegaSmogBoss` | same phase machine; 8-dir spray → **horizontal ring** of orbs on XZ; gas zones spawn on the arena floor |
| `SweepingLaser` | **done (B3).** Telegraph→sweep cycle and every timing unchanged. It **sweeps around Y, not Z**: in 2D "rotate the sprite in the screen plane" and "sweep the beam across the floor" were the same operation, and in 3D they are different axes. The beam is a stretched mesh, so the 2D alpha ramp becomes a renderer toggle plus a colour swap, and detection is `Physics.OverlapBox` **filtered to the Player layer** — the 2D version scanned everything the beam touched, which in a factory full of crates is a lot of wasted overlap |
| `ToxicGasZone` | **done (B3).** Logic and timings are the 2D ones. The cloud is a **flat disc on the ground** rather than a sprite: under the fixed ¾ camera what the player must read is *which floor is about to be unsafe*, and a billowing volume would hide exactly that. The telegraph pulses scale as well as colour, since opaque greybox has no alpha to ramp |
| `ManholeTrap` | **done (B3).** Tier-1 collider swap; the lid rattle jitters across **XZ**, and the bite still roots via `ApplyKnockback(Vector3.zero, …)` |
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

## Level 2 is generated too — but from tilemaps (B3)

`Assets/Editor/Level2Builder.cs` (menu: **Eco-Dash → Rebuild Level 2 from the 2D
layout**) rebuilds `Level2_FactoryMaze` from `Tools/level2_layout.csv`. The pipeline
is the same idea as Level 1's but needed its own extractor, because the 2D Level 2 is
authored completely differently:

| | Level 1 | Level 2 |
|---|---|---|
| geometry | individual prop objects | **two Tilemaps** (1 360 floor cells, 926 obstacle cells) |
| gameplay | plain scene objects + some prefab instances | almost entirely **`PrefabInstance`** records |
| extractor | `Tools/dump_scene.py` | `Tools/dump_level2.py` |

`dump_scene.py` reads neither tilemap data nor a prefab instance's modification list,
which is where its position lives — so a naive re-run reports an empty level. The Level 2
dumper reads both, and `Tools/export_level2.py` then **merges the obstacle grid into
maximal rectangles** (greedy: extend right, then down while the full width matches).
926 cells become 23 boxes: the same solid shape, two orders of magnitude fewer objects
for the renderer, the physics scene and the NavMesh bake.

The 2D Grid sits at `(-20, -17)` with 1 m cells, so cell `(cx, cy)` centres on world
`(cx + 0.5 - 20, cy + 0.5 - 17)` — verified against Greenie's own spawn before anything
was generated. The factory comes out 40 × 34 m.

Level 2 also carries the **3-keycard chain** that ties the two levels together: two cards
lie in the side wings, and the third is handed over by Tí when he is rescued with the
antidote from Ông Sáu's Level 1 herb quest. `RescueNPC` credits the objective directly
rather than dropping a physical card, so the level cannot soft-lock.

`MegaSmogBoss` is **C3's**; the builder leaves a `BossSpawn_MegaSmog` marker at its 2D
position so the arena reads correctly and C3 has an anchor.

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
- **`HitFlash`** (C1) — the enemies' "I got hit" flash. Same reason as
  `MaterialTint`: the 2D enemies set `spriteRenderer.color = Color.white` for
  0.07 s, meshes have no colour channel and `renderer.material` clones per
  instance. It flashes emission alongside base colour, which is what actually
  reads under the ¾ rig. Shared so C2's fly-bot and C3's bosses reuse it.

### Flying enemies need two colliders (C2)

The single most expensive thing to rediscover in C2: **Greenie's Seeds fly flat at
his fire point, `y ≈ 0.6`.** Give a hovering enemy one collider that rides with its
body at 1.6 m and the player's only weapon passes a clear metre underneath it — the
enemy is not "hard to hit", it is *unhittable*, and nothing in the scene looks wrong.

`PollutionFlyBot.prefab` therefore carries **two colliders on the root**, and both are
load-bearing:

| Collider | Purpose | Why it must stay as it is |
|---|---|---|
| `SphereCollider` (solid, r 0.45, centred on the body) | movement — what walls push against | drop it low and the bot stops clearing crates, which is the entire point of flying |
| `CapsuleCollider` (**trigger**, r 0.40, body down to 0.1 m) | the hurtbox projectiles hit | drop it and the bot is unkillable; move it to a child and it is unkillable too — `SeedProjectile` resolves `IDamageable` off the collider it hits |

The general rule for the rest of the port: **height is presentation, hitting things is
XZ.** Under a fixed ¾ camera the player aims on the ground plane, so anything that
leaves the ground still needs a ground-plane footprint you can shoot. C3's flying boss
phases inherit this.

### Enemy persistence: id by spawn point, not by death spot (C1/A6)

`SceneProgress` ids a placed object by **name + position**, which is fine for a
chest and wrong for anything that walks: a slime killed 10 m from its spawn banks
an id no freshly-placed slime ever matches, so it is back on the next load.
`PlasticSlime` captures `SceneProgress.IdFor(gameObject)` in `Awake` and marks
*that* id on death. Any future moving enemy must do the same.

### Two traps worth knowing before debugging Level 1

- **`DialogueRunner` pins `Time.timeScale` to 0 while a line is up**, so physics
  stops dead — no trigger events, no gravity. `TutorialPopup` does the same. Since
  `DialogueNPC`'s auto-briefing is an `Invoke` (scaled time), Bà Tư cannot even
  *start* talking until the tutorial popup is dismissed. Anything that looks like
  "triggers are broken" is usually a modal holding the clock.
- **Walk-over triggers are 0.75–0.8 m, not the 2D 0.4 m.** A CharacterController is
  only sampled by the physics step once per frame; at a high or uneven frame rate
  Greenie can step clean over a small sphere without registering.
- **A dead Greenie freezes the whole scene.** `GameManager.OnPlayerDied` raises the
  lose screen, which pins `Time.timeScale = 0` — same clock-holding trap as the
  modals above. In a play-mode probe this is especially nasty: everything after the
  death reads a frozen world, so enemies "stop moving", projectiles "miss" and drops
  "never happen", and you get a page of unrelated-looking failures from one cause.
  Probes should top the player up between phases and assert he is still alive.
- **Anything that moves and bakes must carry `NavMeshModifier(ignoreFromBuild)`.**
  `Level1Builder` bakes from physics colliders across every layer, so a placed
  agent otherwise carves a hole in the mesh it is standing on. Both
  `PlasticSlime.prefab` and `Player.prefab` set it.

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
