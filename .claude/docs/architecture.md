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

Five things that bite, in the order they bit:

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

- **`Fit` must turn the model before it measures.** Grounding and horizontal centring
  both read world-space bounds, so they have to run on the *final* orientation. `Fit`
  used to centre first and apply `rotY` last, and several borrowed models — oopi, the
  whole Kenney factory kit — pivot on a **tile corner** rather than the mesh centre.
  Centring moves the node so the mesh lands on it, which leaves the node offset from
  the mesh by `d`; the later rotation then spins the mesh about that node and puts it
  back off by `R·d − d`. Only `rotY` callers were affected, which is why it hid for so
  long. Greenie was the worst of them: he is the only 270° caller *and*
  `PlayerController` turns his `Visual` toward travel every frame, so the offset
  **orbited** his real position at 1.80 m — against a `CharacterController` 0.70 m
  across, the robot you saw never overlapped the body that took damage. Anything a
  builder bakes straight into a scene with a rotation is affected too, and its
  clearance test (`TooClose`, `InsideWall`, `NearGameplay`) checks the position
  *before* the offset, so it was vetting a spot the prop did not end up in.

Fit-by-height assumes a roughly cubic model. `rock_largeA` is a flat slab and fitting
it to 0.55 m tall made it **2.15 m across**, wide enough to close corridors the 2D
layout leaves open — the pass logs every final size for exactly this reason. Cycle 2
added two more: a Fantasy Town fountain basin is 2 × 0.28 × 2 m, so asking for 0.9 m
tall gave a **6.4 m** bowl, and the Nature Kit's ground tiles are 5 cm thick, so a
height request scales them by 60×. Flat things are placed by `SpawnModule` at an
explicit scale instead.

### The palette a pack ships is not the palette it means (cycle 2)

Kenney's Nature Kit is the one pack here with an **empty `Textures/` folder** — it is
flat-shaded by design, so its per-material colours *are* its art. Those colours import
wrong: `leafsGreen` is `(0.44, 0.90, 0.84)` (turquoise), `grass` `(0.45, 0.93, 0.87)`,
`dirt` and `stone` near-white. Level 1's trees, grass, rocks, bushes and fences rendered
cyan for the whole of cycle 1 and no one caught it, because any one asset looks plausible
in isolation and the whole scene shifting together reads as "a look".

`ArtKit.NaturePalette` re-authors all 23. Two properties matter:

- **Keyed by material name, not by model.** All ~300 Nature Kit models share one `grass`
  and one `stone` material asset, so the batch survives. Keying by model — which is what
  the existing glTF path does, correctly, for a handful of characters — would have
  produced hundreds of duplicates.
- **A `recolour` therefore must not write through.** `Repaint` gives an overridden colour
  its own cache key (the caller's `variant`, or the colour's hex). Without that, the dead
  tree asking for a brown canopy would repaint every living tree in the valley. This is
  the same shape of bug as the property-block one above: shared state, one caller wanting
  an exception.

The textured packs have the mirror-image hazard. A Kenney FBX asks for a texture named
`colormap`; Unity's material search is **recursive-up**, so a pack whose texture is named
anything else silently binds to a *different pack's* atlas. The Fantasy Town Kit ships
`variation-a.png` and imported wearing Cube Pets' colours until it was renamed.

### Modular kits pivot off-centre on purpose

`ArtKit.Spawn` centres what it places, which is exactly right when one model replaces one
greybox mesh. A modular building kit is the opposite: Fantasy Town's wall panel sits on the
**−X edge** of its 1 m cell so that four instances at 0/90/180/270° enclose the cell.
Centring each one stacks all four in the middle. `ArtKit.SpawnModule` instantiates at a
plain uniform scale and leaves the pivot alone; `ArtPass.Cottage` uses it to assemble the
village houses, and `TerrainKit` uses it for every cliff cube.

## Terrain is scenery, and it is generated (cycle 2)

[TerrainKit.cs](../../Assets/Editor/TerrainKit.cs) builds everything the 2D layout could
not supply, because the 2D scene is a flat tilemap: the highland mesa and its spring inside
Level 1's walls, the hills and lake outside them, the village district, and the plain that
stops the world ending in void. `Level1Builder` calls `TerrainKit.Level1` between wiring
and dressing; `HubBuilder` calls the cheaper `Surround`; `Level2Builder` calls `Underlay`.

**None of it is climbable.** Golden rule #1 still holds — the play surface is the same flat
slab at y = 0. The mesa is a `BoxCollider` per built column, the spring is a trigger with no
solid in it at all, and everything beyond the boundary walls has no collider whatsoever.

Three things this pass learned the hard way:

- **Never stretch a cliff block.** Kenney's cliff cube carries a grass cap that is a fixed
  fraction of its own height, so scaling Y alone turns the cap into a metre-thick green
  slab and the result reads as a layer cake. `TerrainKit.Stack` scales uniformly and stacks,
  which also hides each block's cap under the next one.
- **Water coplanar with the ground z-fights it.** The spring's disc first sat with its top
  face at exactly y = 0 and rendered as a mottled mushroom-shaped stain. It sits 3 cm proud
  now; the bank props hide the lip.
- **A blocker has to be sized against the cross-section it blocks at, not its widest one.**
  The spring's blocker was a sphere of the water's own 3.40 m radius sunk to `center.y = -1.4`,
  so that its widest part would sit below ground and the player could stand on the bank. It
  does the opposite: cut at Greenie's shins the sphere is only 3.09 m across, and his own
  0.35 m body radius stopped him **0.45 m short of water he could see** (QA C1). The same
  sphere reached `y = 2.00` — four times his 0.60 m fire height — so it also destroyed every
  Seed fired across the pool, fizzle VFX and all, in mid-air over open water (QA C2). Two
  symptoms, one sphere.
- **The camera decides how far out is worth building.** Pitch 50° with a 60° FOV puts the
  frustum's top plane on the ground 26.6 m ahead of the camera — about **19 m past
  Greenie**, and *nothing* beyond that is on screen at any height. The first ring of hills
  is 10 m outside the wall for that reason, and a lake placed 38 m out was invisible until
  it was moved in. See [PRODUCT-BACKLOG.md](../../PRODUCT-BACKLOG.md) for the numbers at
  other pitches.

### Water you can stand in, and rock that is only where the rock is (cycle 2 QA)

Two of the cycle-2 environment defects were the same mistake in different clothes: a collider
standing in for a shape it does not have.

**The spring is a wade volume now.** There is no solid over the water — deleting the blocker
is what fixes the Seed-eating dome, because nothing non-trigger is left to fizzle on.
[WaterWade.cs](../../Assets/Scripts/World/WaterWade.cs) is a trigger on the Water layer that
borrows `PlayerController.EnterMud()` / `ExitMud()` for the slow, exactly as `ToxicMud` does,
and eases `PlayerAnimator.SinkOffset` to -0.15 m so Greenie reads as standing *in* the pool.
Three things it has to respect:

- **`PlayerAnimator` owns `visual.localPosition`.** It rewrites it from `baseLocalPos` every
  frame for the hover bob, so a dip written straight onto the transform is scrubbed off on the
  next Update — the same ownership trap as `HitFlash` and an enemy's resting colour. Offsets go
  through `SinkOffset`.
- **The slimes are kept out by a `NavMeshModifierVolume`, not by physics.** Carving the NavMesh
  stops what walks without standing in the way of what flies over it. The blocker used to be
  what the bake tripped on; now nothing physical is involved.
- **Enter/exit must be counted by collider, not by event.** Disabling a `CharacterController`
  inside a trigger does not reliably raise `OnTriggerExit`, so a teleport or a respawn can
  deliver a second `OnTriggerEnter` with no exit between — and one unbalanced pair leaves
  Greenie permanently at half speed. `WaterWade` keeps a `HashSet<Collider>`, which cannot
  double-count. `ToxicMud` has the same exposure and has simply never been teleported into.

**The mesa gets one box per column.** `Mesa()` builds a ragged radial mound and deliberately
skips cells that roll zero storeys — that broken edge is the whole point of the silhouette.
Sizing one `BoxCollider` to `Bounds.Encapsulate` of the cells that *were* built gives an
axis-aligned rectangle, and every skipped corner cell falls inside it: **6.5 m² of invisible
wall in open ground, up to 1.75 m deep** (QA C3). `TerrainKit.Stack(..., solid: true)` now adds
a box per column instead, which traces the real outline for free and re-measures to zero
phantom points. `Stack` is shared with `OuterHills` and `Surround`, which must stay
collider-free, so it is a parameter and not the default.

### `ArtKit.Spawn` places a visual and never a collider (cycle 2 QA)

Level 1's props look solid because they are greybox **prefabs** that carry their own colliders.
Anything a generator spawns straight from an art kit is a ghost — the hub's entire 25-prop yard,
Ông Bear, the recycling counter, the crafting bench, Level 1's three 2.6 m village lanterns and
the beached canoe. The hub scene contained exactly five solid colliders: the floor and four
walls (QA C4/C7). `ArtKit.Solidify(holder, art, ...)` fits a `BoxCollider` on the holder, on the
Obstacle layer, and it is the generator that calls it — never the art pass, because colliders
are the gameplay contract and B5 only ever swaps the visual.

- **It fits in the holder's frame, so turn the holder and not the art.** A model rotated by
  `Spawn`'s `rotY` can only be given its *bounding rectangle*: the beached canoe is a 3.4 × 1.0 m
  boat lying at 25°, which comes out as a 2.3 × 3.5 m box — twice the footprint, right where the
  player now walks along the bank. Rotating the holder instead gets 0.9 × 3.5 m.
- **A tree is not its canopy.** An axis-aligned box around an oak is a box around the leaves;
  two of them would swallow ~10 m² of the hub's 18 × 14 m room. Trees and lanterns pass
  `maxHalfExtent` and get their trunk.
- **An interactable's trigger radius is the contract; the body goes beside it.** The three hub
  interactables were built by `Interactable()`, which grants one trigger sphere — the interaction
  *range*, not a shape, which is why Greenie stood inside Ông Bear and vanished. They get a
  separate `Solid` child carrying no `IInteractable`, clamped so `clamp + 0.35` (Greenie's own
  radius) stays inside the trigger: the player stops 0.90 / 0.85 / 0.94 m out against radii of
  1.10 / 1.00 / 1.00, so every E-prompt still fires.

### The floor's smallest unit is a 4 m tile, and two beats forgot it (cycle 2 QA)

Level 1's ground is 192 separate 4 m tiles — it has to be, because they are re-tinted one at a
time. Two effects quantised to that grid and looked it:

- **`ReclamationPatch` repainted whole tiles.** `TintWithin` does an `OverlapSphere` and re-tints
  the entire renderer of each ground collider it touches, so a 3.5 m circle turned up to eight
  16 m² squares solid green — the valley's payoff beat rendered as a Tetris shape with 90°
  corners, 432 m² of green from four discs totalling 154 m² (QA C5). `tintSurroundings` is off;
  the decal disc is already a circle, already animates outward, and its radius went 3.5 → 4.5 to
  compensate. Tinting the terrain itself needs the ground to stop being 192 flat tiles, which is
  the same structural change undulating terrain would need — cost them together.
- **The three earth tones read as a checkerboard.** The spread was 0.035 on the widest channel,
  8.6% of the base and about triple what "a few percent of variation" meant, and the material was
  drawn per tile from a die roll — so two neighbours could differ by the whole spread along a
  dead-straight 4 m seam, the one thing natural ground never has (QA C6). It is ±0.006 now
  (2.9%), picked from `Mathf.PerlinNoise` over `(i, j)` at ~7 tiles per period, which drops the
  edges where the tone changes from 67% (random) to 29%.

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

## The perspective toggle is one rotation (B6)

`P` swaps between the fixed ¾ camera and a first-person view from Greenie's eyes. This is the
first change to golden rule #1 since the project started, so the shape of it matters more than
the feature: **the game did not grow a second control scheme, it grew a frame.**

`PerspectiveMode` is a static service holding the view and, in first person, the look yaw and
pitch. Everything that used to be written in world axes now multiplies by
`PerspectiveMode.MoveFrame`:

```csharp
moveInput = PerspectiveMode.MoveFrame * dir;   // PlayerController.ReadInput
```

Under the ¾ camera that rotation is the **identity** — the house camera's yaw is 0 — so every
top-down path behaves exactly as it did before B6 and `W` is still world `+Z`. In first person
it is the look yaw, which is what stops the controls inverting the moment the player turns
round. There is no `if (firstPerson)` in the movement code, and there should never be one:
a branch is two behaviours to keep in sync, a multiply is one.

The same frame carries the aim. `PlayerShooter` was **not touched** — it still reads
`PlayerController.FacingDirection`, which in first person follows the look instead of the last
move direction, so Greenie shoots where the player is looking even while strafing or standing
still. Seeds keep flying flat on XZ, so **looking up or down changes what you see and never
where you shoot**; `FirstPersonReticle`'s centre dot exists to say so, because at eye height
Greenie's body is no longer on screen to indicate his facing.

### Four things a perspective swap drags behind it

`PerspectiveRig` (on `CameraRig.prefab`, beside `CameraFollow`) owns all four, and three of
them are traps this repo has already documented in another form:

1. **The second camera is built in code, not authored.** It is a `CinemachineCamera` +
   `CinemachineFollow` at eye height with **zero position damping** (`CameraFollow`'s 0.15 s
   damping is right at 12 m and nausea at 1 m), created in `Start` and priority-swapped on
   toggle so the brain blends the two framings for free. Built rather than authored because it
   then cannot drift out of sync with this file, and every scene that already has a `CameraRig`
   gets first person with no prefab surgery. The prefab's own 2 s default blend is shortened to
   0.3 s on the way past — a cutscene blend, applied to a toggle, leaves the controls
   camera-relative to a camera that is still overhead.
2. **Greenie is hidden by his renderers, never by his `Visual` node.** `PlayerAnimator` caches
   `baseLocalPos`/`baseScale` in `Awake` and rewrites `visual.localPosition` every frame;
   deactivating that node is the documented way to lose his rest pose. `Renderer.enabled`
   touches nothing the animator or the colliders own.
3. **The cursor is shared, and it has to be given back.** It is locked only while first person
   is live *and* `UiModal.AnyOpen` is false, and released again in `OnDisable` — otherwise
   quitting play mode in first person leaves the editor without a pointer.
4. **`Time.timeScale` gained no seventh owner.** The toggle does not touch the clock at all; it
   reads it.

### `UiModal`: one answer to "is a screen up?"

B6 needed a question nothing in the project could answer. Most modals announce themselves
through the clock — `PauseController`, `DialogueRunner`, `TutorialPopup` and the end screens all
park `Time.timeScale` at exactly 0, and `GameFeel`'s hit-stop crawls at 0.02 precisely so it
never reads as one — but the **four runtime-built panels (bag, codex, quest log, crafting) and
the shop never touch it**. Those five now call `UiModal.Set(this, open)`, and `UiModal.AnyOpen`
folds them together with the clock-based ones. It gates the `P` key, the mouse look, the cursor
lock and the reticle.

Owners are tracked as a set of instance ids, not as a counter: a counter is the classic
Fast-Enter-Play-Mode leak — one panel open when play mode exits and the count never returns to
zero — and both `UiModal` and `PerspectiveMode` clear themselves from
`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`. The mode is *deliberately* static
past a scene load (that is the "survives a scene change" requirement); it just must not survive
the play session.

### What B6 does not do

It does not tilt the ground, move Seeds off the XZ plane, or let Greenie leave it. **B8 and B9
remain unstarted and still break rule #1 in ways this does not** — B8 needs `GroundCleanser`'s
`OverlapSphere` tinting rewritten, B9 needs the `CharacterController` replaced with a
surface-aligned `Rigidbody`. The projectile "gate" the cycle-3 draft named is satisfied *for
orientation* (a Seed follows the aim frame in either view) and untouched *for terrain*.

## A horizon is one colour and one roof (B7)

B5 gave every scene a tuned procedural sky and B2 put hills outside Level 1's walls, and at
pitch 50° both were correct and neither was ever really seen — the ¾ camera's frustum tops out
27.9° below horizontal, so the sky was a gradient smear along the top of frame. **B6 made the
horizon a surface**, and everything behind it was suddenly load-bearing. B7 is what makes B6 not
look broken.

### The seam was two colours that should have been one

Distant geometry fades toward `RenderSettings.fogColor`. Where the geometry stops, what shows
through is the procedural sky *below its own horizon line*, which is exactly `_GroundColor`.
B5 authored those independently — warm tan fog (0.72, 0.69, 0.58) against dark-brown sky-ground
(0.32, 0.28, 0.22) — so every far object ended on a visible edge, and Level 1's hills read as
cardboard boxes cut out and pasted onto a different sky.

`SceneLook.Horizon(look)` is now a single value used for **both**. There is no way to author
them apart any more, and the ground has nowhere to end. The other half of the seam — the hill
tops, which stand *above* the sky's horizon line and so meet open sky rather than sky-ground —
is handled by `_AtmosphereThickness`, which whitens the band just above the skyline: 1.75 → 2.30
on the farm, 1.05 → 1.35 on the hub.

Fog density is now sized against the real distances rather than picked by eye. On the farm the
outer hills stand ~68 m out and the outer ground stops at 110 m, so 0.0138 puts the far ridge
~55% into haze and the edge of the world ~90%, while leaving the ~19 m the ¾ camera can actually
see almost untouched (7% at 20 m) — **the framing the game is tuned for barely moves.** The hub
had no fog at all and now has a light 0.0095.

### Ridges, not a skyline

Colour was only half of it. `TerrainKit.Stack` built the outer hills from `cliff_block_*` cubes,
which have flat tops — invisible at pitch 50°, a row of packing crates at eye height. The
`ridge: true` path caps each column with `cliff_blockSlope_*` turned a random quarter-turn, and
the three bands step back and up (10/22/38 m out) so the silhouette recedes. The mesa
deliberately does **not** get this: its per-column colliders trace the rock that is there (QA C3)
and a sloped cap would promise a walkable surface the collider does not have.

> **One trap this uncovered, worth more than the feature.** The five Level 1 generators shared a
> single `System.Random`, so every draw depended on how many the *previous* generator had spent.
> Re-profiling the hills changed the number of ring points, which silently re-rolled the mesa
> behind them — 34 rock cubes became 27, in geometry that cycle-2 QA had signed off. Each
> generator now seeds its own stream (`20260820`…`20260824`). **A shared RNG makes every
> generator a dependency of every generator before it.**

### What is above a factory maze

Level 2 shipped as an open-topped box: 3 m walls and a procedural sky over them, which from eye
height reads as a night sky over an indoor level — a bug, not a choice. `TerrainKit.FactoryHall`
closes it with a roof at 12 m, a shell 25 m beyond the maze, trusses and strip lights.

**The instinct QA C11 rules out is building the walls taller** — they already occlude the ¾
camera. A roof does not, and the reason is exact rather than approximate: the ¾ camera sits
9.693 m up at pitch 50° with a 60° vertical FOV, so its highest frustum ray — a top *corner*,
which is higher than the top edge — still points **27.9° below horizontal**. Nothing at or above
the camera's own height is ever in frame. The lowest thing the hall hangs is a strip light at
10.84 m; the biggest camera shake in the game is the Mega-Smog's 0.32 m. That is 0.83 m of
margin, and it was checked the other way too: rendering Level 2's ¾ framing from three positions
with the hall switched on and off gives **0 differing pixels out of 291 600, three times over.**

The shell is the half that *is* at eye level, so it is placed by a different argument: the camera
trails 7.71 m behind Greenie and would otherwise end up outside a nearer wall and shoot the level
through it. At maze + 25 m it cannot.

Everything in the hall is emissive rather than lit, because a ceiling's underside faces down —
Trilight ambient shades it with `ambientGroundColor` (near black) and the directional light never
reaches it. A lit material gives back a black void. Nothing casts shadows either, or the roof
would put the whole plant in shade. And nothing has a collider, which is also why the NavMesh
never sees it: `Level2Builder` bakes from `PhysicsColliders`.

## The ground is a function (B8)

Level 1's valley floor rises and falls now. QA raised it as
[R1](../../QA/exploratory-pass-2026-08-26.md#r1--undulating-ground-change-request-not-a-defect)
and the backlog promoted it to **B8**; the relief peaks at **24.7°** and spans **2.20 m**, from
−1.18 m to +1.02 m.

**Golden rule #1 survives it.** The rule was never "the ground is flat" — it was "no
platforming": no jumping, nothing to fall off, gravity not a mechanic. All three still hold.
What changed is one word in the rule and one assumption underneath it.

### B8 and B9 are not one piece of work, and QA said so first

The backlog scopes them together: B9's **ordering note** claims the two "want the *same*
rewrite — a controller that follows a surface normal handles both a hill and a wall, and doing
them separately means doing it twice." **That is wrong for B8, and QA had already said why.**
[R1](../../QA/exploratory-pass-2026-08-26.md#r1--undulating-ground-change-request-not-a-defect)
lists under "three things already support it, for free": *"`PlayerController` already applies a
constant `Vector3.down * 9.81` ground-stick every frame, and Greenie's `CharacterController` is
already configured for terrain: `slopeLimit = 45°`, `stepOffset = 0.3`. Greenie would walk up
and down slopes today, with no code change."*

He does. **`PlayerController` needed no edit at all** — not one line — and neither did
`PlayerAnimator`, `PlayerHealth`'s knockback, the hazards, or the contact-damage geometry, every
one of which B9's cost estimate lists as a landing site. The rewrite is B9's alone: a wall is
90° and a `CharacterController`'s capsule is permanently world-Y aligned.

That is why the relief is capped at 24.7° rather than at whatever looked good: the number is
chosen against the 45° the controller and the NavMesh both already accept, with most of a
factor of two in hand. Steepen it past 45° and every one of those costs comes back at once.

### One function, five consumers

`GroundHeight.At(x, z)` is the only answer to "where is the ground", and everything that needs
one asks it:

| Consumer | Why it cannot use a mesh instead |
|---|---|
| The 192 tile meshes | Two neighbours must agree along a shared edge **to the float** or there is a seam |
| Their normals | `RecalculateNormals` averages only the faces on *this* tile, so a shared vertex gets two different normals and the grid reappears as a lighting seam |
| 685 settled objects | They were authored at y = 0 across four generator files and a CSV |
| Seeds and smog orbs | At 60 fps, per projectile, a raycast is the wrong shape of answer |
| The generator, before anything exists | There is no mesh yet to sample |

`GroundProfile` is two octaves of Perlin times a mask; `GroundHeightField` publishes it from
`OnEnable` (never `Awake` — rule 5) and hands it back in `OnDisable`, so nothing carries the
valley's hills into the flat factory. `Profile` has an **internal setter**: gameplay reads the
ground, it never reshapes it.

**The noise window was chosen, not typed.** Perlin is not stationary — which patch of it you
sample decides whether a valley rolls both ways or merely dips. The first one tried gave 0.49 m
of range over 14% of the floor: a dented plane. Fourteen windows were measured and the one that
ships gives the widest range with the rises and hollows in balance.

**And then it was tuned by looking at it, which is the only way this part can be settled.** The
first tuning that satisfied every number — 1.56 m of range, 14.4° peak, all 30 invariants green —
*rendered as a flat plane* from both framings. There is no occlusion cue available at this scale
(a hill would need to be 9 m tall to hide anything from a camera 9.7 m up) and no self-shadowing
either (the sun sits at 48°, so nothing under a 42° slope can shade itself), which leaves the
diffuse term as the only cue there is. At 14.4° that is a few percent under a strong Trilight
ambient, and invisible. At 24.7° it is a **41% swing** in `N·L` across a hillside, and the ground
reads. The relief is still understated from the ¾ camera and clearest at eye height — that is
inherent to a 65 × 49 m valley seen from 12 m, not something more amplitude would fix without
putting crates on slopes they would visibly slide down.

### The relief is masked, and the mask is the design

`GroundProfile.Mask` holds the ground at exactly y = 0 wherever something flat has to stand on
it: the boundary and its 112 fence posts (a 4 m band, feathered over 5 m more), the mesa —
whose 18 per-column colliders were grown down to y = 0 by QA C3 and would float or bury a tier
otherwise — the spring, the village, the boss grove, and **the four reclamation discs**. That
last one is the interesting case: a `ReclamationPatch` blooms into a *9 m flat disc*, and on a
slope it buries its uphill half and floats a visible lip along the downhill one. Level ground
under each is far cheaper than conforming geometry, and a healed glade being flat reads as
intent rather than as a bug.

Ten flat zones, all verified dead level to within a millimetre.

### `TerrainKit.Drop`: one pass instead of a hundred edits

Around 1 500 objects in this scene were authored at y = 0, across `Level1Builder`, `TerrainKit`,
`ArtPass` and a CSV exported from the 2D project. Threading a height lookup through every
placement call would mean touching all of them and trusting the next person who adds one to
remember. `TerrainKit.Drop` runs once, after everything is placed and before the NavMesh bakes,
and settles 685 of them. It **adds** the ground height rather than assigning it, so an authored
lift survives; a `maxLift` guard leaves anything deliberately off the ground alone. A short list
of names — the sludge pools and the ground scatter — is also tilted to the surface normal,
because those lie on the ground rather than stand on it. **Nothing built or grown is in that
list**: a cottage or a tree leaning 20° reads as a bug, not as terrain.

### Seeds fly flat over the ground, not flat in world Y

This is the "projectile gate" the backlog names as the prerequisite for B8, and it came out as
seven lines. `SeedProjectile` and `EnemyProjectile` record their **clearance** at launch — the
height above the ground beneath the muzzle, which on flat ground is the same 0.60 m it always
was — and hold it for the whole flight. XZ velocity, aim, spread fan and `travelDir` are all
untouched: a shot still goes exactly where it was pointed. `GroundHeight.Hug` steers with
velocity rather than writing `rb.position`, because teleporting a dynamic trigger body past an
enemy hurtbox is how you lose a hit. When `Profile` is null it returns immediately, which is why
Level 2 and the hub are provably unaffected.

**Measured, both directions, against a control.** Over 147 cm of rise across 11 m the shot hits
with B8, clearance held at 0.58 m; with the ground field switched off it **misses**, ending
0.35 m *underground* and carrying on to 18 m. Over 150 cm of fall it hits with B8 (0.61 m) and
misses without, ending **1.49 m above the ground beneath it** — sailing over the target's head,
exactly the failure the backlog predicted.

One layer fact worth recording, because it bounds the risk: **`PlayerProjectile` does not collide
with `Ground`** in this project's physics matrix (nor does `EnemyProjectile`). A seed therefore
passes straight through a hillside rather than fizzling on it, which is why the un-hugged uphill
shot ends up under the terrain instead of stopping at it. The relief can never make a shot die
early; the only failure mode it could introduce is the vertical miss, and that is the one the
clearance fixes.

### What the camera does about it, honestly

`CameraFollow` damped all three axes at 0.15 s, and the acceptance criterion asks for Y to be
slacker so the frame stops rocking. It is now `(0.15, 0.55, 0.15)`.

**The play-mode A/B could not measure an improvement in total camera travel, and that is the
expected result rather than a disappointment.** Damping is a first-order *lag*: over a sustained
traverse the camera covers the same ground whatever the time constant, so walking a ramp shows
nothing (76 cm of camera travel against 75 cm). What a longer constant buys is how far the
camera is allowed to fall behind — Greenie riding up and down *inside* the frame instead of the
frame riding with him, which only shows on a reversal. Four attempts at measuring it (peak
vertical speed, RMS speed, vertical path length, lag) each came back either dominated by editor
frame-pacing hitches or too small to separate from run-to-run noise. The setting is right by
construction and costs nothing; the improvement is **unmeasured**, and it is recorded that way
rather than dressed up with the one number that happened to look good.

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
