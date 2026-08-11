# Roadmap / Backlog — Eco-Dash 3D

Living checklist of what's built and what's next. Update the boxes as work
lands; keep newest status at the top of "Recent log". Milestones = the phases
in [../../3D-CONVERSION-PLAN.md](../../3D-CONVERSION-PLAN.md); task details &
owners in [../../TEAM-TASKS.md](../../TEAM-TASKS.md).

## Milestones

### P0 — Bootstrap 🔧 (active)
- **A1** Unity 6 (6000.3.16f1) URP-3D project at repo root
  - [x] project seeded from the editor's own `com.unity.template.3d-cross-platform`
    (URP assets, `GraphicsSettings`, volume profiles) — no Hub "create" needed
  - [x] folder tree mirroring the 2D repo; `SampleScene` moved to `Assets/_Scenes/`
  - [x] tags (6) + physics layers at fixed indices 8–14
  - [x] `Packages/manifest.json`: URP 17.3.0 / Input System 1.19.0 matching the 2D
    repo, `com.unity.ai.navigation`, Coplay plugin
  - [x] `InputSystem_Actions.inputactions` copied from 2D (original GUID kept) and
    wired as the project-wide asset
  - [x] Unity `.gitignore` (seeded) + `.gitattributes` Smart Merge rules + local
    merge driver ([clone setup](unity-workflow.md#clone-setup-run-once-per-machine--dev-b--dev-c-too))
  - [x] first open verified: packages resolved, URP materials upgraded to 17.3.0,
    project loads in ~245 s
  - [x] collision matrix set + all 528 layer pairs read back clean
  - [x] ProBuilder 6.1.2 + Cinemachine 3.1.7 installed
  - [ ] pushed to `main`
- [x] Vibe-coding docs seeded (`CLAUDE.md`, `.claude/docs/`, `.claude/commands/`,
  `ASSETS.md`, `CREDITS.md`, plan + team tasks)
- **A2** Tier-0 scripts copied from 2D repo
  - [x] 38 Tier-0 files copied verbatim with their `.meta` GUIDs (so 2D prefab YAML
    can still resolve them in A5); zero 2D-only API references remain
  - [x] tier list corrected: `ItemUse` + `ShopNPC` are Tier 1; `BossHealthBar`
    deferred to C3; `IKnockbackable` widened to `Vector3`
  - [x] Tier-1 closure the Tier-0 UI needs, ported: `Player/*`, `SeedProjectile`,
    `ItemUse`, `CameraFollow` (¾ rig keeping `Instance`/`Shake`)
  - [x] `check_compile_errors` clean (46 scripts, `Assembly-CSharp.dll` builds)

### P1 — Vertical slice (Week 1)
- **A3** Player 3D (`CharacterController`, XZ move, shoot J, health, interact E,
  hover-bob visual child) as `Player.prefab`
  - [x] all five `Player/*` scripts ported to XZ + `SeedProjectile`
  - [x] `Player.prefab` (CharacterController r=0.35 h=1.15, `Visual` + `Nose`
    greybox, `FirePoint`) and `Seed.prefab`, fully wired; placed as a prefab
    instance in `Assets/_Scenes/SampleScene.unity` (Dev A's test scene)
  - [x] Play-mode smoke test: grounded, HP 6/6, i-frames block the second hit,
    shield/heal clamp, seed launches flat at speed 10 — 24 checks, no exceptions
  - [x] input check **automated** (A5): synthesised Input System key events prove
    W→+Z and D→+X at exactly 5.00 m/s (2D `moveSpeed`), the visual child turns 90°
    to face travel, and J spawns a `SeedProjectile`
  - [ ] E-interact end-to-end — needs a real `IInteractable` in the scene
    (arrives with B2's `Chest`)
- **A4** ¾ follow cam + `Shake` API kept — **done**
  - [x] `CameraFollow` 3D rig: pitch 50°/dist 12 verified in play mode
    (camera at `(0, 9.69, -7.71)`, no yaw/roll), `Instance`/`Shake` intact
  - [x] follow half swapped to Cinemachine: `CameraRig.prefab` = Main Camera
    (`CinemachineBrain`) + `CM_PlayerCam` (`CinemachineCamera` + `CinemachineFollow`
    + impulse source/listener + `CameraFollow`). Binds to the `Player` tag on Start,
    so B and C just drop the prefab in
  - [x] verified in play mode: rest offset exactly `(0, 9.693, -7.713)`, rotation
    `(50, 0, 0)`, zero drift after a teleport, and `Shake(0.18, 0.18)` peaks at
    0.178 m then settles — magnitude keeps its 2D meaning (peak offset in metres)
- **B1** Greybox kit + `Level1_BarrenFarm` blockout — **done**
  - [x] greybox kit in `Assets/Prefabs/Greybox/`: ProBuilder floor/wall (so the
    shapes stay editable) + primitive crate/barrel/rock/hut/fence/dead-tree
  - [x] `Level1_BarrenFarm` generated from the **2D scene's own layout** —
    `Tools/dump_scene.py` → `Tools/export_layout.py` → `Tools/level1_layout.csv` →
    `Assets/Editor/Level1Builder.cs` (menu: *Eco-Dash → Rebuild Level 1*).
    64 × 48 m walled field, 192 ground tiles, 91 props, all at 1 tile = 1 m
  - [x] NavMesh baked (`NavMeshSurface`, physics colliders) and sampled clean at
    the centre and both far corners
  - [x] the 2D encounter layout preserved as 29 spawn points; **C1** has since
    replaced the markers with real slimes under `Enemies/`
- **C1** `PlasticSlime` 3D — **done**
  - [x] `PlasticSlime` as a `NavMeshAgent`: 2D wander preserved (1.5 m/s, 3 m radius,
    1.5–3.5 s repath) plus the aggro the brief asks for — 6 m engage, 9 m give-up
    hysteresis, 2.5 m/s chase (under Greenie's 5 m/s), and a `provokeDuration` so a
    slime sniped from beyond the give-up radius still comes for you
  - [x] contact damage is a **distance test**, not `OnCollisionStay` — a NavMeshAgent
    moves kinematically and the player is a CharacterController, so that pair never
    generates collision callbacks at all. Stats are the 2D ones (2 HP, 1 damage,
    7 knockback, 1 trash, 50% bottle/scrap)
  - [x] `HitFlash` extracted as the shared enemy-foundation piece (C2/C3 reuse it):
    base colour **and** emission through a `MaterialPropertyBlock`
  - [x] `Assets/Prefabs/Enemies/PlasticSlime.prefab` generated by
    `Assets/Editor/EnemyPrefabBuilder.cs` (menu: *Eco-Dash → Rebuild enemy prefabs*);
    29 placed at the 2D spawn points by `Level1Builder`
  - [x] play-mode verified **31/31**: placement + NavMesh + layers, wander, aggro,
    chase, contact damage, i-frame gating, knockback (1.27 m for a 6 m/s impulse),
    de-aggro, provoke-from-range, death by 2 Seeds → trash + bestiary + material
    drop, and kill persistence
- [x] Slice gate: chest → core → HUD tick, slime fight, no Play-mode exceptions

### P2 — Full port (Week 2)
- **A5** HUD/menus wired — **landed early** (it unblocks B2's objective flow)
  - [x] `Assets/Audio/**` + TMP Essential Resources imported; the migrated UI binds
    to `LiberationSans SDF` by its stock GUID and Vietnamese renders via the
    dynamic fallback, same as 2D
  - [x] `HUD.prefab` rebuilt from the 2D **Level 1 HUD instance** (the only place
    the full HUD was assembled) with `TutorialPopup` folded in — see the
    [HUD contract](architecture.md#hudprefab-contract-dev-b-drop-it-in-dont-rewire)
  - [x] `GameManager.prefab` banked for B; both placed in `SampleScene`
  - [x] `MainMenu` / `Intro_Story` / `Ending_Story` migrated verbatim (zero missing
    scripts); menu cleanup: three stray `MenuController` copies nested under button
    labels deleted, URP 2D light removed
  - [x] Build Settings: `MainMenu` at index 0 (`GameManager.LoadMainMenu` → scene 0),
    then `Intro_Story`, `Ending_Story`, `SampleScene` (disabled — Dev A sandbox)
  - [x] play-mode verified, 22/22 + 10/10 + 5/5 + 5/5: I/Tab bag · Q quest log ·
    C codex · Esc pause+freeze/resume · H tutorial (auto-shows on a fresh run) ·
    HP/objective/trash readouts · win + lose screens with working buttons ·
    MainMenu → settings → "Chơi Mới" → all five intro slides
  - [x] `menu → intro → L1` end-to-end — unblocked by B1: `Level1_BarrenFarm`
    now exists at build index 1, which `Intro_Story.nextScene` already pointed at
- **A6** Persistence parity (save/continue, New Game reset) — **done**
  - [x] all eight stores diffed against the 2D repo: seven are **byte-identical**;
    the only divergence is the `SceneProgress` id fix below
  - [x] **`SceneProgress.IdFor` keyed on `x, y`** — the 2D ground plane. In 3D `y = 0`
    for everything on the floor, so any two objects sharing an `x` collapsed into one
    save id (kill one chest-guard slime, its neighbour dies with it on reload). Now
    keyed on `x, z`
  - [x] moving enemies bank the id captured at **spawn**, not where they fell —
    see [architecture.md](architecture.md#enemy-persistence-id-by-spawn-point-not-by-death-spot-c1a6)
  - [x] play-mode verified **29/29**: a run's progress (trash, upgrade tier, bag,
    quest state + flags, bestiary, lore notes, per-scene cores, consumed objects)
    survives a simulated relaunch, and New Game wipes every one of them
  - [x] the relaunch is simulated by nulling each store's private static cache by
    reflection, so the next read comes off PlayerPrefs — asserting straight after a
    write only re-reads RAM and proves nothing. The same trick after New Game is what
    proves it cleared the **keys**, not just the memory
  - note: `SaveSystem`'s class comment claims shop upgrades survive New Game, but
    `ResetNewGame()` calls `PlayerProgress.ResetAll()` and wipes them. The code and
    `MenuController` agree; the comment is stale. Left byte-identical to 2D on purpose
- **B2** L1 interactables & flow — **done** (landed with B1)
  - [x] Tier-1/2 world scripts ported: `Chest`, `EnergyCore`, `ReclamationPatch`,
    `TeleportGate`, `ToxicMud`, `Litter`, `ItemPickup`, `HealthPickup`,
    `SpeedBoostPickup`, `QuestItemPickup`, `LoreNote`, `DialogueNPC`,
    `QuestGiverNPC` (+ new `MaterialTint` / `Billboard` helpers)
  - [x] placed at their 2D coordinates: 3 chests → 4 reclamation patches,
    teleport gate (+ pad, exits to the hub), 3 mud pools, 8 litter, Bà Tư,
    Ông Sáu, 3 herbs, 4 valley lore notes, spring water ×2 / energy drink /
    materials
  - [x] play-mode verified **28/28**: walls hold, mud slows and releases, litter
    feeds the counter + HUD, chest → core → patch → gate with the objective list
    ticking to `[x] Tìm Lõi Năng Lượng (3/3)` and `[x] Mở cổng dịch chuyển`
  - [x] Ông Sáu's M8 herb quest **7/7**: offer → 3 herbs → `HerbsReady` → antidote
  - [ ] `SlimeKing` mini-boss and the enemy population wait on **C1/C3**
- **B3** L2 Factory Maze — **done**
  - [x] **generated from the 2D level, like B1** — but Level 2 is authored differently:
    the maze is a pair of **tilemaps** (1 360 floor cells, 926 obstacle cells) and every
    piece of gameplay is a `PrefabInstance` whose position lives in the modification list.
    `dump_scene.py` reads neither, so `Tools/dump_level2.py` is its Level 2 sibling
  - [x] `Tools/export_level2.py` merges the obstacle grid into **maximal rectangles**
    (926 cells → **23 boxes**) — same solid shape, two orders of magnitude less for the
    renderer, the physics scene and the NavMesh bake
  - [x] 7 scripts ported: `Keycard`, `BossDoor`, `ReturnPortal`, `RescueNPC` (Tier 1),
    `ManholeTrap` (Tier 1), `SweepingLaser` + `ToxicGasZone` (Tier 2)
  - [x] `Assets/Editor/FactoryKitBuilder.cs` (menu: *Eco-Dash → Rebuild factory kit*)
    builds the 9 greybox prefabs; `Assets/Editor/Level2Builder.cs` (menu: *Eco-Dash →
    Rebuild Level 2 from the 2D layout*) builds the scene and bakes the NavMesh
  - [x] the **3-keycard chain** is intact: 2 on the floor + the one Tí hands over when
    rescued with Ông Sáu's antidote, which is what ties L1's M8 quest to L2's boss door
  - [x] per-scene HUD contract authored from the 2D scene's own values: *Nhiệm Vụ: Phá
    Nhà Máy*, 5 objectives, `objectiveLabel` = "Thẻ từ", `completeScene` = `Ending_Story`
  - [x] **bug found: `Keycard` never banked its own pickup.** It checked
    `SceneProgress.IsConsumed` in `Awake` but never called `Consume`, unlike its sibling
    `EnergyCore` — so on reload both cards respawned while the count stayed, and
    re-collecting pushed the objective past 3/3. Fixed to match `EnergyCore`
  - [x] play-mode verified **52/52**: blockout counts, NavMesh paths to both keycards and
    the door, the sealed arena, walls holding, the keycard → Tí → door chain, manhole
    bite + root, laser telegraph/burn/dark, gas telegraph → live → clear, hovering
    fly-bots, and the whole scene contract
  - [ ] `MegaSmogBoss` waits on **C3** — its spot is marked `BossSpawn_MegaSmog`
- **B4** Hub (shop, crafting bench, stage portals, side-quest NPC) — **done**
  - [x] `Shop_RecyclingStation` built by `Assets/Editor/HubBuilder.cs` (menu: *Eco-Dash →
    Rebuild the hub*). No CSV — the 2D hub's props are hand-placed, not tilemapped, so
    its six placements are written out at their 2D coordinates. 18 × 14 m room
  - [x] 4 scripts ported Tier-1: `StagePortal`, `CraftingBench`, `ShopNPC`, `SideQuestNPC`
  - [x] **`ShopUI.prefab` built from code.** `ShopController` is Tier-0 but, unlike
    `CraftingUI`, it does *not* self-build — it needs `panel`, `trashText`, `rows[]`,
    `closeButton`, `backButton` wired. Built in `UIFactory`'s visual language so the shop
    and crafting windows match
  - [x] two stage portals with the M9 shard gate: Stage 1 always open (walk-over),
    Stage 2 broken until a Mảnh Cổng powers it, persisted via a `QuestLog` flag and
    **E-interact so shards can't be spent by walking past**
  - [x] two corrections to the 2D scene, the same kind B1 made for `TEST_LoreNote`:
    the 2D hub had **no walls at all** (the ground tilemap just stopped), and Ông Bear's
    `bear_recycle` quest was test-placed in *Level 1* as "Ông Bear (TEST)" wanting 2+2.
    `QuestCatalog` — the authority — puts it in the hub at **10 + 10**; it lives on its own
    recycling counter beside him, because `PlayerInteractor` resolves one `IInteractable`
    per collider and Ông Bear's own is the shop
  - [x] play-mode verified **30/30**: walls, greeting → shop → buy (tier up, trash spent,
    +2 max HP), crafting window, the side quest offer → turn-in → `recipe_advanced`
    unlock, and both portals' gate state
- [ ] **Known gap (not B4's):** three of `QuestCatalog`'s four side quests still have no
  NPC — `may_pet` + `tai_pond` belong in Level 1 (B2) and `lan_intel` in Level 2 (B3).
  **The 2D repo never placed them either**, and `QuestCompleteTrigger` (which the two
  "External" ones need) is in **zero** 2D scenes, so they are un-completable there too.
  Porting parity is intact; finishing them is a design decision, not a port task
- **C2** Projectiles + `PollutionFlyBot` (true Y-hover, 3D LOS) — **done**
  - [x] `EnemyProjectile` (Smog Orb) ported Tier-1 from 2D, built to mirror `Seed.prefab`
    component for component; flattened onto XZ like the Seed, so neither projectile arcs
  - [x] `PollutionFlyBot` **genuinely hovers**: a downward probe holds it 1.6 m above
    whatever is *beneath* it, so it clears crates and follows the factory floor instead
    of sliding on one plane the way the 2D "hover" did
  - [x] line of sight is cast **along the orb's own flat path from the fire point**, not
    centre-to-centre, so the bot can't claim a shot its orbs would splash on a crate
  - [x] kites at `preferredRange` 4 m and fires on the 1.6 s cadence; all 2D stats kept
    (3 HP, 1 contact damage, 2 trash, 7 knockback, 6 m sight / 9 m give-up, 2.8 m/s chase)
  - [x] **bug found: a hovering enemy was unhittable.** Greenie's Seeds fly flat at his
    fire point (y = 0.59, measured); the bot's body collider starts at 1.15 m, so every
    shot he can take passed underneath. Fixed with a second collider — a trigger capsule
    from the body down to 0.1 m — while the solid sphere stays high so the bot still
    clears crates. Written up in
    [architecture.md](architecture.md#flying-enemies-need-two-colliders-c2)
  - [x] `SmogOrb.prefab` + `PollutionFlyBot.prefab` generated by `EnemyPrefabBuilder`
  - [x] play-mode verified **52/52**: hover from above/below/over a crate, patrol, LOS
    blocked and restored, firing cadence + flat 6 m/s orbs at chest height, kiting in and
    out, orb damage + knockback, orbs fizzling on walls but passing through fellow
    enemies, knockback with hover recovery, provoke-from-range, death by real Seed fire →
    2 trash + bestiary, and kill persistence
- [ ] **C3** `SlimeKing` + `MegaSmogBoss` (ring spray, gas, enrage, boss bar,
  death → ending flow)
- [ ] Port gate: full start→ending playthrough possible

### P3 — Polish (Week 3)
- [ ] **B5** Free low-poly art pass (farm/factory/hub packs) + lighting/post
  volumes; no greybox visible; CREDITS complete
- [ ] **C4** Game-feel (knockback, shake, flashes, death/clean VFX — 2D-parity punch)
- [ ] **C5** Audio ported + rewired; settings sliders verified; `HOW_TO_PLAY.md`
  updated for 3D
- [ ] Full manual playthrough + ~30-min time-budget check
- [ ] Submission build

## Recent log

- _(2026-08-11)_ **B4 done — the hub closes the travel loop.** `Shop_RecyclingStation`
  is built by `HubBuilder`, without a CSV: the 2D hub's props are hand-placed rather than
  tilemapped, so its six placements are written out at their 2D coordinates.
  The one piece of real work was the **shop window**. `ShopController` is Tier-0 and does
  *not* self-build the way `CraftingUI` does — it expects `panel`, `trashText`, `rows[]`
  and two buttons already wired — so `ShopUI.prefab` is built from code in `UIFactory`'s
  visual language, and the two windows now match.
  Two corrections to the 2D scene, both the kind B1 made when it replaced `TEST_LoreNote`:
  the 2D hub had **no walls at all** (its ground tilemap simply stopped and Greenie walked
  into the void), and Ông Bear's `bear_recycle` quest was test-placed in *Level 1* as
  "Ông Bear (TEST)" wanting 2 scrap + 2 bottles. `QuestCatalog` is the authority and puts
  it in the hub ("Trạm Trung Tâm") at 10 + 10.
  It sits on its own recycling counter rather than on Ông Bear, because
  `PlayerInteractor` resolves **one `IInteractable` per collider** and his is the shop —
  worth remembering for any NPC that wants two jobs. Verified **30/30**.
- _(2026-08-11)_ **B3 done — the Factory Maze is playable entrance → boss door.**
  Generated from the 2D level like Level 1, but Level 2 is authored differently and
  needed its own extractor: the maze is a pair of **tilemaps** and every piece of
  gameplay is a `PrefabInstance` whose position lives in the modification list, neither
  of which `dump_scene.py` reads. The 926 obstacle cells are merged into **23 rectangles**
  before they ever reach Unity — the same solid shape with two orders of magnitude less
  for the renderer, physics and the NavMesh bake to chew on.
  Seven scripts ported; the two that needed real thought were `SweepingLaser` (sweeps
  around **Y**, not Z — in 2D "rotate in the screen plane" and "sweep across the floor"
  were the same operation and in 3D they are different axes) and `ToxicGasZone` (a flat
  disc on the ground, because what the player must read is *which floor is about to be
  unsafe*, and a billowing volume would hide exactly that).
  One real bug: **`Keycard` never banked its own pickup** — it checked
  `SceneProgress.IsConsumed` but never called `Consume`, unlike `EnergyCore`, so on
  reload both cards respawned while the collected count stayed and re-collecting pushed
  the objective past 3/3. Verified **52/52**, first run.
- _(2026-08-11)_ **C2 done — the fly-bot flies, sees and shoots.** Y is a real axis for
  it: a downward probe holds it 1.6 m above whatever is under it, so it clears crates and
  follows the floor rather than sliding on one plane like the 2D "hover". Line of sight is
  cast along the orb's own flat path from the fire point, so it can't claim a shot its own
  orbs would splash on a crate. Verified **52/52**.
  One real bug, and the kind that hides in plain sight: **a hovering enemy was
  unhittable.** Greenie's Seeds fly flat at his fire point — measured y = 0.59 — and the
  bot's body collider starts at 1.15 m, so every shot he can take passed a clear metre
  underneath while everything *looked* correct. The fix is two colliders: the solid sphere
  stays high (that's what lets it clear crates) and a trigger capsule hangs from the body
  down to 0.1 m as the hurtbox. The general rule now written down for C3 — **height is
  presentation, hitting things is XZ.**
  Probe lesson to go with C1's: **tear down probe scenery the moment its phase ends.**
  A backstop wall left standing from the "orbs fizzle on walls" test sat exactly across
  the Seed lane four phases later, and presented as "the bot won't die, drops don't
  happen, persistence is broken" — five failures, one forgotten cube.
- _(2026-08-11)_ **C1 + A6 done — Level 1 is populated and the save survives a
  relaunch.** `PlasticSlime` walks the baked NavMesh instead of steering a body at a
  point (the 2D approach only works because the 2D farm is an open field), keeps every
  2D stat, and gains the aggro the C1 brief asks for — which the bestiary had already
  promised ("lao thẳng vào Greenie") even though the 2D code only ever wandered.
  Contact damage had to become a distance test: a NavMeshAgent moves kinematically and
  the player is a CharacterController, so the pair never generates collision callbacks.
  Verified **31/31** on the slime and **29/29** on persistence.
  Two real bugs found on the way. **Anything that moves and bakes needs
  `NavMeshModifier(ignoreFromBuild)`** — `Level1Builder` bakes from physics colliders on
  every layer, so 29 slimes would have punched 29 holes in the mesh they walk on;
  `Player.prefab` was quietly doing the same at the level's start point.
  And **`SceneProgress.IdFor` was keyed on `x, y`**, the 2D ground plane — in 3D `y = 0`
  for everything on the floor, so objects sharing an `x` shared a save id. Keyed on
  `x, z` now, and moving enemies bank the id captured at spawn rather than where they
  fell, which no re-placed slime would ever match.
  Two probe lessons worth keeping: **a dead Greenie freezes the scene**
  (`OnPlayerDied` pins `Time.timeScale = 0`, so everything afterwards reads a frozen
  world and one death looks like a page of unrelated failures), and
  **`sceneLoaded` does not fire for a scene already open when you press Play**, so
  `SceneProgress.LastScene` looks broken in the editor while the shipped
  menu → intro → `LoadScene` path records it fine.
- _(2026-08-10)_ **B1 + B2 done — Level 1 is playable start→gate.** The blockout is
  **generated, not hand-placed**: `Tools/dump_scene.py` parses the 2D scene YAML
  (including the `PrefabInstance` records a naive pass misses — the player, the
  herbs and ~30 slimes all live there), `export_layout.py` maps 2D `(x, y)` to 3D
  `(x, z)`, and `Assets/Editor/Level1Builder.cs` rebuilds the scene from the CSV.
  Re-runnable from the **Eco-Dash** menu, so the map can be re-derived rather than
  re-drawn. 13 world scripts ported (all Tier-1 collider swaps except
  `ReclamationPatch`, redesigned to swap materials in a radius as the brief asks).
  Verified 28/28 on the level and 7/7 on Ông Sáu's herb quest.
  Two traps cost real time and are now written up in
  [architecture.md](architecture.md#two-traps-worth-knowing-before-debugging-level-1):
  `DialogueRunner`/`TutorialPopup` pin `Time.timeScale` to 0, which stops physics
  dead — every "triggers are broken" symptom was actually a modal holding the
  clock, and because the auto-briefing is an `Invoke` (scaled time) Bà Tư can't
  even start until the tutorial is dismissed. Second: walk-over triggers had to
  grow from the 2D 0.4 m to 0.75–0.8 m, because a CharacterController is sampled
  once per physics step and can step clean over a small sphere.
  Enemy placement is left to **C1** — the 29 spawn points from the 2D scene are
  preserved as markers under `Spawns/`.
- _(2026-08-09)_ **A4 done, A5 landed early.** The camera follow moved onto a
  Cinemachine vcam (`CameraRig.prefab`); `CameraFollow` now only authors the rig
  and turns `Shake` into an impulse — measured 0.178 m peak for magnitude 0.18, so
  the 2D contract holds. The whole 2D UI layer migrated by **file copy**: A2's
  GUID-preserving script port paid off, and `HUD.prefab` + the three UI-only
  scenes imported with **zero missing scripts**. `HUD.prefab` was rebuilt from the
  2D Level 1 HUD *instance* (the base prefab alone lacks the objective panel,
  settings and dialogue system) and `TutorialPopup` folded in, so B places one
  prefab per scene. Three real bugs found and fixed on the way: the 2D MainMenu
  carried **four** `MenuController` copies (three parented under button labels,
  only the root wired to the settings overlay); the Win/Lose/Pause "Về Menu" and
  "Chơi lại" buttons pointed at the *scene's* GameManager, a reference a prefab
  asset can't hold, so they arrived dead — now routed through additive
  pass-throughs on the HUD's own components; and the migrated menu still had a
  URP **2D** light. Testing note: MCP-driven play-mode probes must run from a
  `MonoBehaviour` in the player loop, not `EditorApplication.update`, and must set
  `backgroundBehavior = IgnoreFocus` — an unfocused editor otherwise **disables the
  keyboard device** and silently drops synthesised events (see
  [unity-workflow.md](unity-workflow.md#play-mode-probes)). With that fixed, A3's
  open "human check" is now automated: 5.00 m/s on both axes, 90° facing turn, J fires.
- _(2026-07-25)_ **A1 + A2 green; A3/A4 scaffolded.** Project opened clean:
  `check_compile_errors` passes on all 46 scripts, ProBuilder 6.1.2 + Cinemachine
  3.1.7 installed, collision matrix verified across all 528 layer pairs.
  Two bootstrap bugs found and fixed: hand-written `TagManager.asset` YAML had
  bare `-` entries so Unity **silently dropped every layer name** (rewritten via
  `SerializedObject`), and the 2D repo's custom `Player` tag duplicated Unity's
  built-in one. `Player.prefab` + `Seed.prefab` built and smoke-tested in play
  mode (24 checks, no exceptions). Remaining on A3: a human WASD/J pass and
  E-interact once a real `IInteractable` exists.
- _(2026-07-25)_ **A1 + A2 code landed (compile not yet verified).** URP-3D
  project seeded into the repo root straight from the installed editor's template
  — Hub can't "create" into a non-empty folder, and the template tarball's
  `ProjectData~` is exactly what Hub copies. Tags/layers/manifest/input written on
  disk while the editor was closed. 38 Tier-0 scripts copied; the Tier-1 closure
  they depend on (`Player/*`, `SeedProjectile`, `ItemUse`, `CameraFollow`) ported
  to XZ. Contract changes recorded in [architecture.md](architecture.md) — Dev C
  must implement `IKnockbackable.ApplyKnockback(Vector3, float)`.
- _(2026-07-09)_ **Repo seeded.** Conversion plan, 3-dev task split, and the
  full vibe-coding doc ecosystem (CLAUDE.md, docs, commands, ASSETS/CREDITS,
  Unity .gitignore) created. Unity project itself not yet created — A1 is next.
