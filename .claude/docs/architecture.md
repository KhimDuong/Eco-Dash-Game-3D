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

## The hub (B4)

`Assets/Editor/HubBuilder.cs` (menu: **Eco-Dash → Rebuild the hub**) builds
`Shop_RecyclingStation` **and the prefabs it needs**. There is no CSV: the 2D hub's
props are hand-placed rather than tilemapped, so its six placements are written out at
their 2D coordinates. The ground tilemap spans cells x −9..8, y −7..6 with the Grid at
the origin — an 18 × 14 m room.

Two things are worth knowing before touching it:

- **`ShopController` does not self-build.** `CraftingUI` constructs its whole window
  through `UIFactory` in `Awake`, so `CraftingBench` works dropped into any scene.
  `ShopController` is Tier-0 too but expects `panel`, `trashText`, `rows[]`,
  `closeButton` and `backButton` already wired — hence `Assets/Prefabs/Hub/ShopUI.prefab`,
  built from code in `UIFactory`'s visual language so the two windows match.
- **`PlayerInteractor` resolves one `IInteractable` per collider.** Ông Bear therefore
  carries the shop and nothing else; his `bear_recycle` side quest lives on a separate
  recycling counter beside him. Any NPC that wants two jobs needs two colliders.

The **stage-portal shard gate** (M9/K7) is the hub's own mechanic: Stage 1 is always
open and walk-over, Stage 2 is broken until a Mảnh Cổng powers it. Gated portals are
**E-interact on purpose** — walking past must never spend a shard — and the powered
state persists through a `QuestLog` flag, not just the session.

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
leaves the ground still needs a ground-plane footprint you can shoot. C3's bosses both
stand on the floor, so a single box rising from `y = 0` already crosses the Seed lane —
but the same rule decided where the Mega-Smog's **orbs leave from**; see below.

## The art pass is generated too (B5)

Real art arrives the same way the levels do: from a re-runnable generator, not by
dragging meshes onto prefabs. Three files own it —
[ArtKit.cs](../../Assets/Editor/ArtKit.cs) (how to turn a model file into a usable
visual), [ArtPass.cs](../../Assets/Editor/ArtPass.cs) (one entry per prefab: what
replaces what), and [SceneLook.cs](../../Assets/Editor/SceneLook.cs) (per-scene sun,
ambient, fog and post volume). Menu: **Eco-Dash → Run the art pass (B5)**.

**Only the visual is replaced.** Colliders, trigger radii, scripts, prompt canvases
and the toggled-child contracts are gameplay and are never touched. The loose props
(crate, barrel, rock, fence, hut) are the deliberate exception: their colliders are
refit from the model's measured bounds, because a real model **pivots on the floor**
where a primitive is centred on its own origin — which is why `Level1Builder` no
longer lifts props by hand-measured amounts.

Four things that bite, in the order they bit:

- **A prefab generator undoes the art.** `HubBuilder`, `FactoryKitBuilder` and
  `EnemyPrefabBuilder` rebuild their prefabs from primitives, so each calls
  `ArtPass.Reapply*()` at the end. Without it, rebuilding the hub silently reverts
  Ông Bear to a grey capsule and nothing warns you.
- **glTF materials are not URP materials.** glTFast assigns its own Shader Graph whose
  colour property is `baseColorFactor`, not `_BaseColor` — so `HitFlash` and
  `MaterialTint` become *silent no-ops* on any raw glTF import. `ArtKit` converts them
  to URP/Lit assets. Models whose colour lives only in a missing texture atlas import
  pure white and cannot be rescued; check before committing to one.
- **A property block is per material slot, not per renderer.** The greybox enemies were
  one material per renderer, which hid this. The real slime is *one* renderer with a
  body and an eye material, so `HitFlash` restoring from `sharedMaterial` alone
  repainted the eyes body-green after the first hit. It now walks
  `sharedMaterials` and uses the indexed `Get/SetPropertyBlock` overload.
- **Models bring luggage.** The Quaternius flying robot ships two baked-in
  **directional lights** at intensity 4.3; one per fly-bot would blow out the level.
  `ArtKit.Spawn` always strips lights, and fits by a requested height in metres because
  the packs disagree wildly on scale (Kenney Survival is authored at 0.5 m, its Nature
  Kit at 1 m, one Quaternius character imports 5.6 m tall).

Fit-by-height assumes a roughly cubic model. `rock_largeA` is a flat slab and fitting
it to 0.55 m tall made it **2.15 m across**, wide enough to close corridors the 2D
layout leaves open — the pass logs every final size for exactly this reason.

## Bosses bring their own UI (C3)

[IBoss.cs](../../Assets/Scripts/Enemies/IBoss.cs) is the whole contract: a name, two
counters, and `OnEngaged` / `OnHealthChanged` / `OnDefeated`. In 2D there was no
contract — `BossHealthBar` held a hard `MegaSmogBoss` reference and lived in the Level 2
scene, and `SlimeKing` raised an identical set of events that nothing ever subscribed to.

[BossHealthBar.cs](../../Assets/Scripts/UI/BossHealthBar.cs) **builds itself in code**,
like `InventoryUI` and `Hotbar`, and a boss calls `BossHealthBar.Bind(this)` as it wakes.
Consequences worth knowing:

- **Nothing is wired in any scene.** Drop a boss prefab into a level and it arrives with
  a working HP bar; delete it and nothing dangles. Both bosses share one implementation.
- It parents itself under the **HUD's canvas** when the scene has one, so it inherits the
  same `CanvasScaler`, and falls back to its own overlay canvas at sorting order 85 —
  above the hotbar, below the bag/codex/quest panels and the tutorial popup.
- The fill is driven through the RectTransform's **anchor**, not `Image.fillAmount`: a
  Filled image needs a sprite to fill, and a bar built from bare `Image`s has none.

### Three things C3 had to get right (and two that were quietly wrong)

- **A boss cannot tint itself.** `HitFlash` caches each slot's resting colour in `Awake`
  and repaints it after every flash, so an "enrage tint" applied through `MaterialTint`
  survives until the next Seed lands and is then scrubbed off — visible for one frame,
  in a way that reads as "the tint didn't apply". `HitFlash.SetBaseTint(multiplier)` owns
  the resting colour instead, and **multiplies** the authored colour rather than replacing
  it, so a machine assembled from a dozen materials goes angry instead of flat red.
- **`health <= maxHealth * threshold` is not what it reads as.** `0.35f` is really
  0.34999999, so `40 * 0.35f` is 13.9999998 and the Mega-Smog skipped its enrage at 14 HP
  entirely. Any percentage-of-max gate belongs in whole HP, resolved once with
  `Mathf.CeilToInt`, not recomputed as a float every hit.
- **Waking a boss is line-of-sight, not distance.** The Mega-Smog stands 4 m from the
  blast door and its activation radius is 7 m, so a pure distance check had it spraying
  orbs through a locked door at a player who could not shoot back. The linecast runs
  against **Obstacle** — the layer `BossDoor`'s blocker sits on, and which the door
  disables when it opens — so the fight starts exactly when Greenie walks in.
- **A ring attack fires at the player's chest, not the boss's.** Orbs fly flat
  (`EnemyProjectile` zeroes Y), so a ring emitted from the top of a 2.4 m machine passes
  over Greenie's 1.15 m capsule entirely. Same rule as the fly-bot's nozzle, one size up.
- **Area attacks get sampled onto the NavMesh.** Level 2's arena is 24 × 6 m; a raw ±5 m
  scatter around the player drops half of every gas wave inside a wall, where it
  threatens nobody and looks like the attack is broken.

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

## Game feel is a service; cleaning is a loop (C4)

Two new statics and one new system, all reachable from anywhere and none of them wired
into a scene:

- [Vfx.cs](../../Assets/Scripts/Systems/Vfx.cs) — `Poof` / `CleanBurst` / `Impact`. Each
  call builds a `ParticleSystem` in code, plays it once and destroys itself
  (`stopAction = Destroy`). No VFX prefab exists, so no generator can throw one away.
- [GameFeel.cs](../../Assets/Scripts/Systems/GameFeel.cs) — `Shake` (a null-safe wrapper
  over `CameraFollow.Instance.Shake`, so callers stop copying the null check around) and
  `HitStop`. Durations live here as constants rather than as serialized fields on four
  enemy prefabs.
- [GroundCleanser.cs](../../Assets/Scripts/World/GroundCleanser.cs) — clearing trash
  cleans the ground around it and raises that stage's **Độ Sạch**.

### The particles are meshes, and their colour is in the material

A billboard needs one of the URP *Particles* shaders, and **no material in this project
references one**. `Shader.Find` can only return shaders a build actually kept, so that
puff would look perfect in the editor and be a magenta square in the submission build.
Every burst uses URP/Lit — which is on every material in the game — with little cubes as
the particle mesh, which suits the low-poly art better than a soft puff anyway.

That choice has a consequence: mesh particles only carry `startColor` into the shader if
the shader reads the vertex-colour stream, and URP/Lit does not. So tint comes from a
**small material cache keyed on the colour** (a handful of tints in the whole game), and
each cached material carries matching emission — the same reason `HitFlash` flashes
emission alongside base colour. Under the fixed ¾ camera a tint swap barely registers; a
lit blob pops.

`Vfx.ColorOf(go, fallback)` reads an object's own colour off its first renderer, so an
enemy poofs in its own colour with **nothing serialized on the prefab**. Recolour the
slime in `ArtPass` and its death poof follows on the next run.

### Hit-stop slows the clock; it must never park it at 0

`Time.timeScale` has six owners in this project — `PauseController`, `TutorialPopup`,
`DialogueRunner`, `ShopController`, the end screens and `GameManager` — and every one of
them uses exactly `0`. A hit-stop that also used 0 could not tell, when its wait ended,
whether the 0 it was looking at was still its own or a dialogue that had opened in the
meantime; restoring the wrong one un-pauses a modal under the player. `GameFeel` crawls
at `StopScale = 0.02` instead, and only restores if the clock is *still* exactly that.
At 2% speed a 60 ms stop reads as a freeze anyway, and coroutines measuring scaled time
(the Mega-Smog's collapse) keep inching forward instead of deadlocking.

Two corollaries: the runner is `DontDestroyOnLoad`, because a stop that starts as a boss
dies has to finish even though that death loads the next scene; and hit-stop is
deliberately **not** applied on the killing blow to the player, where `OnPlayerDied`
already pins the clock for the lose screen.

### The cleaning loop was specified in M9 and never written

`game-design.md` §4.7.5/§4.7.8 have called for `GroundCleanser.CleanRadius(pos, r)` since
M9, `Codex` and `ItemUse` both carry comments pointing at it — and in the 2D build
**nothing ever called `AddCleanliness`**, so the codex's third tab showed two bars frozen
at 0% for the whole game. Three things the 3D version had to get right:

- **The share per piece is derived, not accumulated.** The meter is recomputed as
  `100 × cleaned / authored` and only the difference is handed to the codex. Accumulating
  a per-piece share would repeat C3's enrage bug in a new costume: seven pieces at 100/7
  each sum to 99.99999, and a meter one ten-thousandth short of 100 never pays out its
  Portal Shard.
- **The authored total is counted in `Awake`.** A `Litter` cleaned on an earlier visit
  deletes itself in `Start`, so a count taken any later sees only the leftovers and
  inflates every remaining piece's share. Pieces register themselves in their own `Awake`
  — before any `Start` runs — and the ones deleting themselves report as already-cleaned
  on the way out, which keeps the count honest *and* repaints the ground they cleared.
- **The tally resets per level *load*, not per level.** Keying the reset on the scene name
  misses the case the player hits most: dying and taking "Chơi lại" reloads the same
  scene, the name never changes, and the tally carries on counting — sixteen authored
  pieces in an eight-piece field, and 100% permanently out of reach. Both levels load
  whole, so every Litter registers in one `Awake` burst; a registration a frame or more
  after the last one belongs to a new load.

The ground repaint carries a **footprint cap**, and it is load-bearing rather than a
tweak. Tinting works per renderer, and the two levels build floors completely
differently: Level 1 lays 192 four-metre tiles, so a cleansed piece greens the tile it
sits on; Level 2's floor is a *single* 40 × 34 m slab that `export_level2.py` merged out
of 1 360 cells. Without the cap, one bottle in the factory repaints the entire level in
one frame. The factory therefore gets the sparkle and the meter but no ground tint —
correct, and the reason Level 2's cleanse looks quieter than Level 1's.

### Fast Enter Play Mode: statics survive, and one of them was dying quietly

`ProjectSettings` has Fast Enter Play Mode on, so **the domain is not reloaded between
play sessions** and every static keeps its value from the last run. The persistence
stores are immune by construction — they re-read themselves from PlayerPrefs — but a
plain counter is not, so `GroundCleanser`, `GameFeel` and `Vfx` each clear themselves
from a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.

C4 found one that had been broken since M9: **`ItemDatabase` caches runtime
`ScriptableObject`s**, which Unity destroys when play mode ends. Its dictionary survived
into the next session still holding all of them, destroyed, so `Get` returned an object
that compares equal to null and from the **second** Play onwards every id looked unknown
— consumables refused to be used, display names fell back to raw ids, and nothing logged
a word. A build never sees it (each run is a fresh process), which is exactly what made
it expensive: it only bites in the editor, where all the testing happens. Same
`SubsystemRegistration` reset. **Any static holding a runtime-created Unity object needs
one.**

## Audio is two services and a generated table (C5)

The eight clips came over from the 2D repo in A5 and then sat there: only
`HUD.prefab` had any of them wired, three of the six scenes had no music at all, and
**every other `AudioClip` field in the game was empty**. C5 is the wiring, and it needed
two runtime services and one editor pass.

### `PlayClipAtPoint` is a 3D sound, and the listener is 12 m away

Every ported caller played its sound with `AudioSource.PlayClipAtPoint(clip, pos)`. In
2D that was fine. Measured in play mode here, that call builds a source with
`spatialBlend = 1`, logarithmic rolloff and `minDistance = 1` — a **fully 3D** sound —
and the `AudioListener` rides the camera, which the ¾ rig parks **12.4 m** behind and
above Greenie. Logarithmic rolloff at 12.4 m is a gain of about **0.08**: the whole game
would have played at 8% volume. The 2D build had already met a milder version of this and
patched two call sites by hand (`QuestItemPickup` and `EndScreenController` both played at
`Camera.main.position`, i.e. right on top of the listener) — a workaround that reads as
noise until you know what it is working around.

`Sfx` fixes the cause. Sounds are **2D (`spatialBlend = 0`), attenuated by distance from
Greenie** rather than from the camera. Real 3D audio has nothing to tell the player under
a fixed camera — the listener sits at a constant offset, so a slime 10 m away is 15.9 m
from the camera against 12.4 m for one at your feet, a difference the ear cannot use. And
moving the listener onto Greenie to fix *that* would make the stereo image **rotate with
him**, because his visual child turns to face travel: a sound on the left of the screen
would pan right the moment he walked south. So panning is dropped and only the useful half
kept. Full volume within 9 m, silent past 34 m, and a sound past the cutoff never even
claims a voice.

It is a pooled service rather than a component for the same reason `GameFeel` is one:
**the sound has to outlive the thing that made it**, and a slime's own `AudioSource` dies
with the slime on the frame its death sound starts. Eight voices, because `PlayOneShot`
reads the source's pitch when it starts — two sounds sharing a source must share a pitch,
and the pool is what lets each one carry its own.

### Cosmetic randomness must not spend the gameplay RNG

`Sfx` scatters pitch by ±7% so the slime death does not sound like one clip looping 29
times. Drawing that from `UnityEngine.Random` — the obvious thing — **changed where the
slimes walked**. That generator is one global sequence and gameplay is spending it:
`PlasticSlime` takes its wander target and repath timer from it, `Litter` and the enemies
roll their drops from it. One extra draw per sound effect shifts every draw after it.
Measured: it moved a wandering slime from 1.8 m to 2.4 m off its spawn and failed a combat
test that had nothing to do with audio. `Sfx` now owns a private `System.Random`.
**Anything cosmetic that needs a random number should.**

### One music player, not one per scene

`MusicPlayer` is a `DontDestroyOnLoad` singleton that bootstraps itself from
`RuntimeInitializeOnLoadMethod` and reads `Assets/Resources/MusicKit.asset` for the track
each scene wants. Two reasons it is not an `AudioSource` per scene, the way the 2D build
did it:

- Three of the six scenes are **generated** (`Level1_BarrenFarm`, `Level2_FactoryMaze`,
  `Shop_RecyclingStation`), so a source placed in them survives until the next *Rebuild*.
  That is exactly why they had no music.
- A per-scene source **restarts the track on every load**. This build portals hub ↔ L1 ↔ L2
  constantly and reloads on death, so one thirty-second loop would be forever starting over.
  A scene change is now inaudible: same object, same playhead.

The kit lives in `Resources` so it resolves from whatever scene the editor started in —
levels are entered directly all day during development, and music that only exists if you
came from the main menu is music nobody hears while building a level. The three
hand-built scenes had their own `Music` objects removed so there is exactly one owner.
`MusicVolume` survives as the helper for any music source placed by hand.

### The clips are laid down by a generator, like the art

`AudioPass` (menu: **Eco-Dash → Run the audio pass (C5)**) holds the table of
(prefab, component, field, clip) and writes it with `SerializedObject`. Same reason
`ArtPass` exists: most prefabs that need a clip are rebuilt from primitives by
`EnemyPrefabBuilder` / `FactoryKitBuilder` / `HubBuilder`, so a hand-dragged clip lives
until the next rebuild and then vanishes silently. Those three builders call
`AudioPass.Reapply*` right where they already call `ArtPass.Reapply*`. **Add a row to
`AudioPass.cs`; never drag a clip onto a prefab.**

Two fields are left empty on purpose and should stay that way: `PlayerHealth.deathSfx`
(`EndScreenController` already plays `lose_jingle` on the same frame, and two cues on one
frame just smear each other) and `HealthPickup`/`SpeedBoostPickup.collectSfx` (no prefab
uses those scripts — the 3D build folded both into the generic `ItemPickup`).

### A settings panel is a view, not a second source of truth

`SettingsPanel.Awake` wired its slider/toggle callbacks and *then* left the widgets showing
whatever the prefab was saved with — in this project a mute toggle that is **ON** and a
music slider at **100%**, neither of which is what `GameSettings` defaults to. A `Toggle`
or `Slider` that reports its own value through those callbacks writes it into the store and
saves it to `PlayerPrefs`, and this actually happened: the project's saved settings were
found sitting at exactly `muted=1, music=1.0` — the prefab's values, not any player's
choice. The fix is one line of ordering: **sync the widgets from the store before attaching
the listeners**, so the controls can only ever echo the store, never define it.

### `[ExecuteAlways]` + Fast Enter Play Mode = `Awake` may never run

`CameraFollow` is `[ExecuteAlways]` so the framing updates live in the scene view, and it
claimed `CameraFollow.Instance` in `Awake` under `if (Application.isPlaying)`. That `Awake`
already ran when the scene was **opened** in edit mode, where it correctly declined to
claim — and Fast Enter Play Mode reuses the scene's existing objects instead of reloading
them, so that one edit-mode call is the only one there is. `Instance` stayed **null for the
whole session** and every `GameFeel.Shake` in the game silently did nothing; the impulse
chain underneath was healthy the entire time, which is why nothing looked broken. Claiming
in `OnEnable` as well fixes it. Same family as the statics rule below: **Fast Enter Play
Mode changes which lifecycle callbacks you may assume, not only how long statics live.**

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
