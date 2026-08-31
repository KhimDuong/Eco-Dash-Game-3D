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
  - [x] `SlimeKing` landed with **C3** — a grove in the empty south-west corner, guarding the
    Mảnh Cổng. It is the third source of a Portal Shard, alongside Cô Lan's recipe and the
    cleanliness reward
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
  - [x] `MegaSmogBoss` landed with **C3** — on the layout's own boss coordinate, so the marker
    is gone and the sealed arena finally has something in it
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
- **C3** `SlimeKing` + `MegaSmogBoss` — **done**
  - [x] **`IBoss`** — the three events (`OnEngaged` / `OnHealthChanged` / `OnDefeated`) plus
    name and counters, extracted as a real contract. The 2D `BossHealthBar` held a hard
    `MegaSmogBoss` reference and `SlimeKing` raised identical events that nothing listened to
  - [x] **`BossHealthBar` builds itself in code** (like `InventoryUI` / `Hotbar`) and a boss
    calls `BossHealthBar.Bind(this)` as it wakes. Nothing is wired in any scene, so both
    bosses share one bar and a boss dropped into a level brings its own. It parents itself
    under the HUD's canvas when there is one, and drives the fill through the RectTransform's
    anchor — a Filled `Image` needs a sprite, and a bar built from bare `Image`s has none
  - [x] **`SlimeKing`** (Tier 2, the Plastic Slime one size up): NavMeshAgent chase, distance-test
    contact damage, splits into 3 real `PlasticSlime`s at half HP — placed on a ring and sampled
    onto the mesh, never a random scatter — and drops the **Mảnh Cổng** the hub's Stage-2 portal
    wants. All 2D stats (20 HP, 2 contact damage, 9 knockback, 2.2 m/s, 7 m aggro)
  - [x] **the King's grove is the one Level 1 placement not ported from 2D**, because there was
    nothing to port: the 2D scene's only Slime King is a `TEST_SlimeKing` parked 6 m from the
    player's spawn with half the prefab's health. The farm's other three corners each hold a
    guarded chest and the south-west one was empty, so the grove goes there — 7 dead trees with
    a gap facing the approach, two sludge pools, 30 m from the start. A deliberate detour, which
    is what the bestiary already said it was
  - [x] **`MegaSmogBoss`** (Tier 2): stationary, 8-orb ring spray rotated 11° a volley and fired
    at **Greenie's chest** (a ring leaving the top of a 2.4 m machine sails over his head), gas
    waves pulled onto the NavMesh so they land on floor he can stand on, enrage under 35 % HP
    (12 orbs, 0.6× interval, permanent angry tint), then a collapse into
    `GameManager.CompleteLevel` → **Ending_Story**
  - [x] **waking up is line-of-sight, not distance.** The machine sits 4 m from the blast door,
    inside its own 7 m radius, so the 2D check had it firing through a locked door at a player
    who could not answer. The linecast runs against Obstacle — the layer `BossDoor`'s blocker is
    on — so it wakes exactly when the door opens and Greenie steps in
  - [x] **bug found: the enrage never fired on time.** `health <= maxHealth * enrageThreshold` is
    not what it reads as — `0.35f` is 0.34999999, so `40 * 0.35f` is 13.9999998 and the boss
    skipped its enrage at 14 HP entirely. Resolved once in `Awake` with `Mathf.CeilToInt`
  - [x] **bug found: a boss cannot tint itself.** `HitFlash` caches the resting colour in `Awake`
    and repaints it after every flash, so an enrage tint applied any other way is scrubbed off by
    the next Seed that lands. `HitFlash.SetBaseTint` now owns it, multiplying the authored colour
    so a machine built from a dozen materials goes angry rather than flat red
  - [x] `SlimeKing.prefab` + `MegaSmogBoss.prefab` generated by `EnemyPrefabBuilder`, art applied
    by `ArtPass` (the King is the same Quaternius slime recoloured; the machine is Kenney's
    `machine-fortified` flanked by two hoppers). Placed by `Level1Builder` / `Level2Builder`
  - [x] play-mode verified **37/37** (King) and **41/41** (Mega-Smog): placement and layers, the
    hurtbox crossing the Seed lane, idle-until-approached, the bar appearing/tracking/hiding, the
    chase, contact damage, real-Seed damage and knockback, the split landing on the mesh, the
    ring's 45° spacing at chest height, gas on walkable floor, no wake through a locked door,
    enrage at exactly 14/40 with the tint surviving a hit flash, and death → `CompleteLevel` →
    **Ending_Story** with time running again
  - [x] no regressions: the C1/C2/B2/B3/B4 suite re-run green — **193/193**
- [x] Port gate: full start→ending playthrough possible

### P3 — Polish (Week 3)
- **B5** Free low-poly art pass + lighting/post volumes — **done**
  - [x] five CC0 packs imported to `Assets/Models/ThirdParty/`: **Kenney** Nature Kit,
    Survival Kit, Factory Kit, Cube Pets (one bear) and three **Quaternius** models
    via Poly Pizza. FBX only — each Kenney zip's OBJ/GLTF/DAE/STL duplicates and
    isometric sprite renders are dropped, which is most of the download
  - [x] **the art pass is generated, like everything else**: `Assets/Editor/ArtKit.cs`
    (size to a height in metres, pivot on the floor, convert materials, strip lights,
    build an Idle controller) + `Assets/Editor/ArtPass.cs` (one entry per prefab;
    menu: *Eco-Dash → Run the art pass (B5)*; idempotent) +
    `Assets/Editor/SceneLook.cs` (sun/ambient/fog + a post-processing volume profile
    saved beside each scene)
  - [x] **Greenie is Kenney's `oopi`** — a mint one-eyed robot that already looked the
    part; `PlasticSlime` and `PollutionFlyBot` are Quaternius' slime and flying-gun
    robot; every human NPC is one Quaternius farmer mesh, with Bà Tư and Tí as
    recolours at different heights
  - [x] per-scene look: Level 1 warm/hazy with fog, Level 2 dark and high-contrast so
    the lasers and screens are the brightest things on screen, the hub bright and even
  - [x] Level 1 gained a tiled boundary fence (112 posts) and 181 scattered ground
    details; Level 2 gained 38 machinery props hugging the maze walls — both
    deterministic, collider-free, and kept clear of anything interactive
  - [x] **bug found: a property block is per material slot, not per renderer.**
    The greybox enemies were one material per renderer, which hid it. The real slime is
    *one* renderer with a body and an eye material, so `HitFlash` restoring from
    `sharedMaterial` repainted its eyes body-green after the first hit. Rewritten to
    walk `sharedMaterials` and use the indexed property-block overload
  - [x] **trap found: a prefab generator silently undoes the art.** `HubBuilder`,
    `FactoryKitBuilder` and `EnemyPrefabBuilder` rebuild their prefabs from primitives;
    each now calls `ArtPass.Reapply*()` at the end
  - [x] **trap found: models bring luggage.** The Quaternius flying robot ships two
    baked-in *directional lights* at intensity 4.3 — one per fly-bot would have blown
    out Level 2. `ArtKit.Spawn` always strips lights
  - [x] verified: **no `Greybox_*` material is left in any of the three scenes**, every
    size measured, nothing floating or sunk, and the gameplay probes still pass —
    slime **31/31**, Level 1 **28/28**, Level 2, hub and fly-bot re-run green
  - [ ] `.glb` needs the **`com.unity.cloud.gltfast`** package (added to
    `Packages/manifest.json`) — B and C re-resolve packages on next pull
- **C4** Game-feel (knockback, shake, flashes, death/clean VFX) — **done**
  - [x] **`Vfx`** — `Poof` / `CleanBurst` / `Impact`, each built from code and destroying itself
    when it stops, like `UIFactory` builds the windows. No VFX prefab exists, so no generator
    can throw one away and nothing needs wiring per scene
  - [x] the particles are **little meshes on URP/Lit, not billboards**: a billboard needs one of
    the URP *Particles* shaders and no material in this project references one, so
    `Shader.Find` would return null in a build — perfect in the editor, magenta in the
    submission. Tint therefore comes from a small material cache keyed on the colour, with
    matching emission, because mesh particles don't carry `startColor` into URP/Lit
  - [x] **`Vfx.ColorOf`** reads an enemy's colour off its own art, so a death poof is the right
    colour with **nothing serialized on any prefab** — recolour a slime in `ArtPass` and its
    poof follows. Not one enemy prefab had to be rebuilt for C4
  - [x] **`GameFeel`** — `Shake` (the null-safe wrapper the ported callers were open-coding) and
    `HitStop`, with the durations as constants in one place rather than serialized fields on
    four prefabs. Wired into every beat: slime/fly-bot deaths, the King's split and death, the
    Mega-Smog's enrage and collapse, Seed impacts, and Greenie taking a hit
  - [x] **hit-stop crawls the clock at 2%, it never parks it at 0.** `Time.timeScale` has six
    owners here and all of them use exactly 0, so a hit-stop that also used 0 could not tell
    its own freeze from a dialogue that opened during it — and restoring the wrong one
    un-pauses a modal under the player. Crawling makes ownership checkable
  - [x] **`GroundCleanser`** — the cleaning loop `game-design` §4.7.5 has specified since M9 and
    **nobody ever wrote**: in the 2D build nothing ever called `Codex.AddCleanliness`, so the
    codex's Độ Sạch tab showed two bars frozen at 0% for the whole game. Clearing trash now
    greens the ground around it, sparkles, and moves the stage meter — with the 50% / 100%
    payouts (the 100% one being Level 1's third Portal Shard) finally reachable
  - [x] the Seed Bomb's **other half** landed with it: §4.7.2 always said it clears trash as
    well as damaging, and `ItemUse` had carried a comment pointing at the missing component
    since M9
  - [x] **the share per piece is derived, not accumulated** — `100 × cleaned / authored`, only
    the difference handed to the codex. Accumulating would have repeated C3's enrage bug in a
    new costume: seven pieces at 100/7 sum to 99.99999, and a meter that stops a
    ten-thousandth short never pays out
  - [x] **Level 2 gained 10 pieces of waste** (deterministic, like B5's dressing, and kept out
    of the sealed boss arena). The 2D factory has no litter at all, which was invisible until
    the loop existed and the factory's meter turned out to be one that could never move
  - [x] **bug found: the tally reset per level, not per level *load*.** Dying and taking "Chơi
    lại" reloads the same scene, so the name never changes — sixteen authored pieces in an
    eight-piece field, every piece worth half, and 100% permanently out of reach
  - [x] **bug found: `ItemDatabase` had been dying on every Play but the first.** It caches
    runtime `ScriptableObject`s, Unity destroys those when play mode ends, and Fast Enter Play
    Mode keeps the dictionary — so from the second Play on, every id looked unknown:
    consumables refused to be used and display names fell back to raw ids, silently. A build
    never sees it, which is what made it expensive. `GroundCleanser` / `GameFeel` / `Vfx` clear
    themselves the same way
  - [x] play-mode verified **43/43** (Level 1: the kit, hit-stop ownership, the whole cleaning
    loop to 100% and its rewards, the Seed Bomb, impacts/poofs/stops on real Seed fire, and a
    revisit that neither forgets its cleaned ground nor pays out twice) and **9/9** (Level 2)
  - [x] no regressions: Mega-Smog **41/41**, fly-bot, slime, Level 1 and the Slime King re-run green
- **C5** Audio ported + rewired; settings sliders verified; `HOW_TO_PLAY.md` for 3D — **done**
  - [x] the eight clips came over in A5 and had been sitting unused: only `HUD.prefab` had any
    of them, three of six scenes had **no music at all**, and every other `AudioClip` field in
    the game was empty. C5 is the wiring
  - [x] **`Sfx`** — one pooled service every sound goes out through. `AudioSource.PlayClipAtPoint`,
    which every ported caller used, builds a **3D** sound (measured: `spatialBlend = 1`,
    logarithmic rolloff, `minDistance = 1`), and the listener rides the camera 12.4 m behind
    Greenie — a gain of about **0.08**, so the whole game would have played at 8% volume. The
    2D build had already met a milder version and hand-patched two call sites to play at
    `Camera.main.position`
  - [x] sounds are **2D, attenuated by distance from Greenie**: under a fixed camera real 3D
    audio tells the player nothing (10 m away is 15.9 m from the camera against 12.4 m at his
    feet), and moving the listener onto Greenie would make the stereo image **rotate with him**,
    since his visual turns to face travel. Full volume within 9 m, silent past 34 m
  - [x] a service, not a component, for `GameFeel`'s reason: **the sound outlives the thing that
    made it**, and a slime's own `AudioSource` dies with it on the frame its death sound starts
  - [x] **bug found (mine): cosmetic randomness was spending the gameplay RNG.** The ±7% pitch
    scatter drew from `UnityEngine.Random` — the same single sequence `PlasticSlime` takes its
    wander target and repath timer from. One draw per sound shifted every draw after it: a
    wandering slime moved 1.8 m → 2.4 m off its spawn and failed a combat test that had nothing
    to do with audio. `Sfx` now owns a private `System.Random`
  - [x] **`MusicPlayer`** — one `DontDestroyOnLoad` owner reading `Resources/MusicKit.asset`,
    replacing the per-scene sources. The three generated scenes had no music *because* a source
    placed in them dies at the next Rebuild; and a per-scene source **restarts the track on every
    load**, which in a build that portals hub ↔ L1 ↔ L2 constantly means one loop forever starting
    over. A scene change is now inaudible — same object, same playhead
  - [x] **`AudioPass`** (menu: *Eco-Dash → Run the audio pass (C5)*) — 26 fields written from one
    table, the same way `ArtPass` writes the art and for the same reason: the enemy, factory and
    hub prefabs are rebuilt from primitives, so a hand-dragged clip vanishes at the next Rebuild.
    All three builders now call `AudioPass.Reapply*` beside their `ArtPass.Reapply*`
  - [x] two fields deliberately left empty: `PlayerHealth.deathSfx` (the lose jingle already fires
    that frame) and `HealthPickup`/`SpeedBoostPickup.collectSfx` (no prefab uses those scripts)
  - [x] **bug found: the settings panel was overwriting the player's settings with the prefab's.**
    `SettingsPanel.Awake` attached its callbacks *before* syncing the widgets, and the prefab was
    saved with mute **ON** and music at **100%** — the project's stored settings were found sitting
    at exactly that. Sync-then-listen makes the panel a view of the store, never a second source
    of truth
  - [x] **bug found: camera shake had been dead.** `CameraFollow` is `[ExecuteAlways]`, so its
    `Awake` ran when the scene was *opened* in edit mode and correctly declined to claim
    `Instance`; Fast Enter Play Mode then reuses those objects rather than reloading them, so it
    never ran again and `Instance` stayed **null all session**. Every `GameFeel.Shake` silently
    did nothing while the impulse chain underneath was healthy. Claiming in `OnEnable` too fixes it
  - [x] `HOW_TO_PLAY.md` ported from the 2D repo and brought up to the 3D build; `CREDITS.md`
    audio section now says what each of the eight clips actually plays
  - [x] play-mode verified **64/64** (the pool and its 2D voices, the attenuation curve, clips
    reaching live scene objects, a near kill heard and a far one not, music volume vs both
    sliders and mute, the panel not writing the store, a scene reload that does not restart the
    track, and the menu no longer double-playing it)
  - [x] no regressions: C4 **43/43**, Mega-Smog **41/41**, fly-bot **52/52**, slime **31/31**,
    Slime King **37/37**, Level 1 **28/28**, Level 2 cleaning **9/9** — 305 checks green
- [ ] Full manual playthrough + ~30-min time-budget check
- [ ] Submission build

### Cycle 3 — movement & perspective ([PRODUCT-BACKLOG.md](../../PRODUCT-BACKLOG.md) B6–B9)
- **B6** `P` toggles the ¾ view and first person — **done**
  - [x] **golden rule #1 rewritten first** (CLAUDE.md), because B6 changes the project's
    founding constraint: the world is still one flat XZ plane with no jumping and no gravity,
    but the ¾ camera is now the *default* framing rather than the only one
  - [x] `PerspectiveMode` — static owner of the view and the look angles; `MoveFrame` is the
    identity under the ¾ camera, so every top-down path is bit-for-bit unchanged
  - [x] `PerspectiveRig` on `CameraRig.prefab` — polls `P`, builds the first-person
    `CinemachineCamera` at eye height in code, priority-swaps it (0.3 s blend), reads the mouse,
    hides Greenie by his **renderers** and manages the cursor lock
  - [x] `UiModal` — the "is a screen up?" question the project could not answer: the bag, codex,
    quest log, crafting bench and shop never touched `Time.timeScale`, so they now register
  - [x] `FirstPersonReticle` — a centre dot, because at eye height Greenie's body is no longer
    on screen to show which way he aims
  - [x] `PlayerShooter` untouched: aim rides `FacingDirection`, which follows the look in first
    person. Seeds still fly flat, so looking down does not tilt a shot
  - [x] `HOW_TO_PLAY.md` + the in-game tutorial popup both teach `P`
  - [x] play-mode verified **49/49** — rig build, top-down parity, the dive to the eye, renderer
    hiding with the `Visual` node left alone, aim in both views, seeds flat in both views,
    real-key WASD walking `+Z` in top-down and east in first person while looking east, `A`
    strafing, `P` gated by the bag, and the mode surviving a load into the hub
- **B7** a sky and a horizon that survive being looked at — **done**
  - [x] `SceneLook.Horizon(look)` — **one** colour for both `RenderSettings.fogColor` and the
    procedural sky's `_GroundColor`, so distant ground has nowhere to end. B5 authored them
    apart, which is why Level 1's hills read as boxes pasted onto a different sky
  - [x] fog sized against the real distances (farm 0.0138: ~55% haze on the 68 m ridge, ~90% at
    the 110 m edge, 7% at 20 m so the ¾ framing barely moves); the hub had **no fog at all** and
    now has a light 0.0095; thicker atmosphere whitens the band above the skyline
  - [x] `TerrainKit.Stack(ridge: true)` — outer ridges capped with `cliff_blockSlope_*` instead
    of flat cubes, bands stepped back to 10/22/38 m; the hub's pushed out from 8/18 to 16/34 m.
    The mesa deliberately keeps its flat steps (its colliders trace the rock — QA C3)
  - [x] `TerrainKit.FactoryHall` — Level 2 was an open-topped box under a night sky. Roof at
    12 m, shell 25 m beyond the maze, 8 trusses, 4 strip lights; no colliders, no shadows,
    emissive so the underside is not a black void
  - [x] **the roof is provably invisible from the ¾ camera** — its frustum tops out 27.9° below
    horizontal, the lowest hall surface hangs at 10.84 m, and the biggest shake in the game is
    0.32 m. Rendered A/B from three positions in the maze: **0 of 291 600 pixels differ**
  - [x] **bug found: the five Level 1 generators shared one `System.Random`.** Re-profiling the
    hills changed how many ring points there were and silently re-rolled the mesa behind them
    (34 rock cubes → 27) — cycle-2 QA-verified geometry moving because something upstream of it
    changed. One seed per generator now
  - [x] verified 20/20 (fog/sky matched in all three scenes, ridge caps, the mesa's per-column
    colliders intact, the hall collider-less and shadow-less, roof height, shell standoff, and
    the NavMesh still pathing to both keycards and the boss door), plus B6 re-run 13/13 after
    the rebuilds and a clean error log. Evidence in [../../QA/screenshots/](../../QA/screenshots/)
    (`b7_*`)
- **B8** hill-like ground: tilts, rises and dips instead of a flat plane — **done**
  - [x] `GroundHeight` / `GroundProfile` — one height function, and the only answer to "where is
    the ground". Two octaves of Perlin times a mask; **2.20 m of range, peaking at 24.7°**
  - [x] `GroundHeightField` publishes it from `OnEnable` (never `Awake` — rule 5) and hands it
    back in `OnDisable`; `Profile` has an **internal setter**, so gameplay reads the ground and
    never reshapes it. Level 2 and the hub have no field at all, so they are provably still flat
  - [x] `Level1Builder.BuildGround` — 192 generated tile meshes instead of 192 flat slabs, each
    sampling the shared function so neighbours meet **to the float** (no seam), with **analytic
    normals** so they do not disagree on lighting either. Still 192 renderers, because
    `GroundCleanser` tints them one at a time
  - [x] `GroundProfile.Mask` — ten flat zones, all verified dead level: the boundary and its 112
    fence posts, the mesa (QA C3's per-column colliders were grown to y = 0), the spring, the
    village, the boss grove, and the four 9 m reclamation discs
  - [x] `TerrainKit.Drop` — one settle pass puts **685 objects** back on the ground instead of
    threading a height lookup through four generator files; it adds the height rather than
    assigning it, so authored lifts survive
  - [x] **the projectile gate, in seven lines** — `SeedProjectile` and `EnemyProjectile` record
    their clearance at launch and hold it. Aim, spread and `travelDir` untouched; a null profile
    returns immediately, so the flat scenes are bit-for-bit unchanged
  - [x] **`PlayerController` needed no edit at all** — the `CharacterController`'s 45°
    `slopeLimit` and the existing ground-stick already walk slopes, exactly as QA R1 predicted.
    The backlog's claim that B8 and B9 need the same rewrite is wrong for B8
  - [x] `CameraFollow` damps Y at 0.55 s against 0.15 s on X and Z
  - [x] verified **30/30** static (seams, normals, flat zones, settled objects, NavMesh paths to
    both far chests and the boss grove, the cleanser's footprint cap, the two flat scenes) and in
    play: 4.0 cm of capsule drift over a 146 cm climb, **a shot uphill over 147 cm of rise hits
    where the control misses underground, and one downhill over 150 cm hits where the control
    sails 1.49 m over its head**, and the cleanser repaints 4 of 4 tiles on 24.7° ground
  - [x] **tuned by looking at it.** The first tuning passed every number and *rendered flat*;
    at this scale the diffuse term is the only cue available, so the slope had to go to 24.7°
    before the ground read as terrain. Evidence in [../../QA/screenshots/](../../QA/screenshots/)
    (`b8_*`)
- **B9** Greenie climbs vertical walls like an ant — **done**
  - [x] `SurfaceFrame` — one owner for "which way is up", `Up` and `Rotation` exact (physics and
    the movement frame), `VisualRotation` eased at 420 deg/s (the mesh and the first-person
    camera). **The identity while he is on the ground**, so every expression that now routes
    through it collapses back to the arithmetic it replaced
  - [x] `Climbable` — climbing is a permission per collider, not a rule about geometry. Without
    it every boundary wall, cottage and fence is a vertical face and Greenie climbs out of the
    level. `TerrainKit.Column` hangs one on each of the mesa's 18 columns as it builds them
  - [x] `WallClimber` — attach on 0.25 s of pushing into a face steeper than 60 deg, hold across
    the seams between columns, step over the lip at the top, let go at the bottom. Movement on a
    wall runs at half ground speed: at the full 5 m/s a 1.4 m tier is over in 0.28 s
  - [x] **`PlayerController` needed one substitution**, not a rewrite: `Vector3.down` became
    `-SurfaceFrame.Up`. The `CharacterController` is untouched (r 0.35, h 1.15, slope 45,
    step 0.3), and so are the hazards, the contact-damage geometry and `CameraFollow` — all of
    which B9's own cost estimate listed as landing sites
  - [x] `PerspectiveMode.MoveFrame` and `LookRotation` compose with the surface frame, so `W`
    climbs and `A`/`D` traverse on every face with no branch; `PlayerAnimator` bobs, squashes and
    turns in it; `PlayerHealth`/`ApplyKnockback` project onto it instead of zeroing `y`
  - [x] **firing is disabled on a wall, by design** — a seed would hold its B8 ground clearance
    and curve into the sky, and there is nothing up there to shoot. `PlayerShooter` returns early
  - [x] verified **22/22 static** (18 markers and only 18, all on the collider itself, under the
    Highlands root, on a mesa that is stepped 1.4/2.8/4.2 m over dead-level B8 ground; Level 2
    and the hub carry none) and **31/31 in play** driving the real keyboard: 3 attaches and 3
    dismounts to the summit in 3.1 s, no frame off a lip over 5 cm, hangs with 0.0 cm of drift
    for 1.5 s, traverses 2.09 m across a seam between two colliders and **stops** at the end of
    the rock, walks back down to 0 m, fires nothing while climbing and normally again on the
    ground, cannot be knocked off a face (1.6 cm along the normal), rolls 90 deg in first person
    with the eye 1.05 m out along it — and a control run with the climber off gets 0.00 m up
  - [x] known and reported, not papered over: on a wall the **hitbox is not the silhouette**
    (the capsule stays world-Y aligned), first person on a 4.2 m rock **shows mostly sky**, and
    the ¾ camera **cannot see the north face** — the last of which is pre-existing and measurable
    at ground level with no climbing involved

## Recent log

- _(2026-08-31)_ **First person is now the framing the game opens in.** A PO call, taken once B9
  was in: `PerspectiveMode.Default` went from `TopDown` to `FirstPerson`, so `P` now goes *out*
  to the ¾ camera instead of into first person. One constant, because the mode is read on the
  first frame — before any component's `Start` — by `PlayerController` for its move frame and
  `PlayerShooter` for its aim.
  Three things already made it cheap: `MoveFrame` is the identity at yaw 0, so `W` is still world
  +Z on the opening frame either way; only the three gameplay scenes carry a `CameraRig`, so the
  cursor lock can never strand a player in a menu; and the opening is a **cut**, not a dive —
  measured, the camera never rises above 1.05 m in the first 60 frames against the ¾ rig's 9.7 m.
  **The ¾ camera stays canonical** — every layout, sightline and QA pass is still tuned at it, and
  it is still what nothing else may control. What this does change is which caveats are on the
  default path: B9's first-person notes (the camera rolls on a wall; a 4.2 m rock mostly shows
  sky) now apply to a player who never presses anything.
  14/14 checks on the opening state and the round trip, and the B9 suite re-run at 31/31 with no
  regression. Opening view in
  [../../QA/screenshots/](../../QA/screenshots/) (`fp_default_level1_opening*`).

- _(2026-08-31)_ **B9 — which way is up is a state.** Greenie ant-walks the Level 1 mesa: into a
  rock face, up it, over the lip, across the roof, up the next one, summit at 4.2 m, and back
  down. Cycle 3 is complete.
  **The backlog costed this as a `Rigidbody` rewrite of `PlayerController` and it was wrong** —
  the same way it was wrong about B8, and for the same reason. The estimate reasons from a
  *component's* limit ("a `CharacterController`'s capsule is permanently world-Y aligned"), which
  is true, rather than from the *API's* — `CharacterController.Move` takes a world-space delta and
  has no opinion about gravity, so a capsule pressed against a wall climbs it the moment you hand
  it a delta pointing up the face. `PlayerController`'s share of B9 is **one substitution**:
  `Vector3.down` became `-SurfaceFrame.Up`. The hazards, the contact-damage geometry and
  `CameraFollow` — all named as landing sites — were not touched at all.
  The work went into a **frame**, the third of cycle 3 after B6's and B8's and the same shape as
  both: `SurfaceFrame` answers "which way is up for Greenie right now", is the identity whenever
  he is on the ground, and composes into `PerspectiveMode.MoveFrame` so that `W` climbs and
  `A`/`D` traverse without a branch anywhere. And into a **permission**: `Climbable`, on 18
  colliders, because the natural rule — push into a vertical face — applied to geometry lets him
  climb the boundary wall and stand on the skybox.
  **Two bugs worth keeping, both invisible from the code.** "Stop climbing when you are back at
  ground level" fired on the frame after every attach, because every climb *starts* at ground
  level: 15 attach/dismount cycles in nine seconds and he never left the floor. And
  `climber != null` does not disable anything — a disabled `MonoBehaviour` still answers a direct
  call, so the control run whose whole job was to show the pre-B9 behaviour climbed the mesa.
  **Three things are reported rather than smoothed over.** The hitbox on a wall is not the
  silhouette (the capsule cannot tilt) — free today because nothing can reach him up there, and
  the moment something shoots at him on a wall it is not. First person on a wall rolls into his
  frame, which is the answer the item asked for and which, on a rock only 4.2 m tall, means
  looking at the sky. And the ¾ camera cannot see the mesa's north face at all — measured
  *blocked by Column at 9.0 m*, and pre-existing: the same ray at ground level with no climbing
  involved is blocked at 7.2 m.
  22/22 static checks, 31/31 in play driving the real keyboard, clean error log. Evidence in
  [../../QA/screenshots/](../../QA/screenshots/) (`b9_*`).

- _(2026-08-31)_ **B8 — the ground is a function.** Level 1's floor rises and falls now: 2.20 m
  of range at up to 24.7°, which is QA's R1 promoted to a backlog item and delivered.
  **The expensive assumption turned out to be false.** The backlog scopes B8 with B9 because
  they "want the same rewrite — a controller that follows a surface normal"; QA's own R1
  write-up had already said otherwise, and it was right. A `CharacterController` walks slopes:
  45° `slopeLimit`, 0.3 m `stepOffset`, and a ground-stick that was already there.
  **`PlayerController` needed no edit at all** — nor did `PlayerAnimator`, the knockback, the
  hazards or the contact-damage geometry, every one of which B9's cost estimate lists as a
  landing site. The relief is capped at 24.7° precisely so that stays true.
  The work went into the other four things. **One height function**, because five consumers have
  to agree on where the ground is to the millimetre — the tile meshes (or there is a seam), their
  normals (analytic, because `RecalculateNormals` gives a shared vertex two different answers and
  the grid comes back as a lighting seam), 685 props authored at y = 0, the projectiles, and the
  generator itself before any mesh exists. **A mask**, because ten things have flat footprints
  and would otherwise float or bury themselves — the mesa's per-column colliders, the village,
  and the four 9 m reclamation discs above all. **One settle pass** (`TerrainKit.Drop`) instead
  of a height lookup threaded through four generator files. And **the projectile gate in seven
  lines**: a Seed records its clearance at launch and holds it, so it still flies dead flat in
  XZ but flat *over the ground*. Measured against a control: 147 cm of rise hits where the old
  behaviour ends up underground, 150 cm of fall hits where the old behaviour sails 1.49 m over
  the target's head.
  **Two things are recorded as they came out rather than as they were hoped.** The first tuning
  satisfied every acceptance number and rendered as a flat plane — at 65 × 49 m under a camera
  9.7 m up there is no occlusion cue and no self-shadowing, so the diffuse term is all there is,
  and 14.4° of slope is a few percent of it. 24.7° gives a 41% swing in `N·L` and the ground
  reads; it is still understated from the ¾ camera and clearest at eye height. And the camera's
  new vertical damping (0.55 s against 0.15 s) is right by construction but its benefit is
  **unmeasured** — four different metrics came back either dominated by editor frame pacing or
  too small to separate from noise.
  30/30 static checks, the play-mode set above, clean error log.

- _(2026-08-31)_ **B7 — a horizon is one colour and one roof.** B6 turned the sky from a
  gradient smear along the top of frame into a surface, and everything behind it was suddenly
  load-bearing. Two things were wrong and neither was a shortage of assets.
  **First, the seam was two colours that should have been one.** Distant geometry fades toward
  the fog colour; where it stops, what shows through is the sky *below its own horizon line*,
  which is `_GroundColor`. B5 authored those independently — warm tan fog against dark-brown
  sky-ground — so every far object ended on a visible edge. One value now feeds both, and the
  fog density is sized against the real distances (the far ridge is 68 m out, the world stops at
  110 m) rather than picked by eye, which leaves the ~19 m the ¾ camera can see almost untouched.
  **Second, the hills had flat tops** — invisible at pitch 50°, a row of packing crates at eye
  height. Each column is capped with `cliff_blockSlope_*` now and the bands step back.
  **Level 2 got a roof.** It was an open-topped box under a procedural night sky, and QA C11
  rules out the obvious fix of building the walls taller. A roof is exempt: the ¾ camera's
  frustum tops out **27.9° below horizontal**, so nothing at or above the camera's own 9.69 m is
  ever in frame. Rendered A/B from three positions with the hall on and off: **0 of 291 600
  pixels differ**, three times over — the framing every QA pass was run at is bit-identical.
  One bug found on the way, and it is the more useful half: **the five Level 1 generators shared
  a single `System.Random`**, so re-profiling the hills changed how many ring points there were
  and silently re-rolled the mesa behind them — 34 rock cubes became 27, in geometry cycle-2 QA
  had signed off. A shared RNG makes every generator a dependency of every generator before it.
  One seed each now. 20/20 verified, B6 re-run 13/13 after the rebuilds, clean error log.
- _(2026-08-31)_ **B6 — the perspective toggle, and the frame that made it cheap.** `P` now
  drops to Greenie's eyes and back. The interesting part is not the camera (a second
  `CinemachineCamera` at eye height, priority-swapped, is a dozen lines) but the movement: WASD
  was authored in **world** axes with the camera's yaw locked at 0 *because* of that, so first
  person would have inverted the controls the moment the player turned round. Rather than
  branching, `PlayerController` now multiplies its input by `PerspectiveMode.MoveFrame` — the
  identity under the ¾ camera, the look yaw in first person — which is why the top-down game is
  provably unchanged (real-key probe: `W` still walks `+Z` at 2.03 m in 0.4 s, zero cross-axis
  drift) and `PlayerShooter` needed no edit at all.
  Three things the repo had already written down bit exactly as documented: Greenie had to be
  hidden by his **renderers**, not by deactivating the `Visual` node `PlayerAnimator` rewrites
  every frame; the toggle had to stay off `Time.timeScale`, which already has six owners; and
  the statics had to be cleared from `SubsystemRegistration` — the mode is deliberately allowed
  to outlive a scene load, and must not outlive the play session.
  One gap found while wiring the gate: **the project had no way to ask "is a screen open?"**
  Most modals park the clock at 0, which is a usable signal, but the bag, codex, quest log,
  crafting bench and shop never touch it — so pressing `P` with the bag up would have swapped
  the camera under the player, and the cursor would have stayed locked away from the very panel
  they had just opened. `UiModal` is now the single answer, and those five register with it.
  49 play-mode checks green, including the mode surviving a load from the farm into the hub.
- _(2026-08-20)_ **Cycle 2's environment pass (backlog B1–B5) — and the reason the valley
  looked cheap was a bug, not a shortage of assets.** Kenney's Nature Kit is the one pack in
  the project with **no texture at all** — it is flat-shaded off its material colours — and
  those colours import wrong: `leafsGreen` arrives as turquoise `(0.44, 0.90, 0.84)`, `dirt`
  and `stone` as near-white. Every tree, grass tuft, rock, bush and fence in Level 1 had been
  rendering **cyan** since B5, through a full QA pass, because no single asset looks wrong on
  its own and a whole scene shifting together reads as a deliberate look. `ArtKit.NaturePalette`
  now re-authors all 23, keyed by material name so the shared batch survives — which in turn
  meant the dead trees had to start asking for their dead colour explicitly, or the palette fix
  would have brought all 38 of them back to life. The mirror-image hazard bit on the way in: a
  Kenney FBX asks for a texture called `colormap`, Unity's material search is recursive-*up*,
  and the newly imported Fantasy Town Kit (whose texture ships as `variation-a.png`) silently
  wore **Cube Pets'** atlas until it was renamed.
  On top of that: Level 1 gained a 4.2 m stepped rock mesa with a spring at its foot, three
  rings of hills and a lake beyond the boundary walls, and green land under all of it so the
  world stops ending in void; a village district of Fantasy Town cottages north of the 2D
  layout's pen, with the layout's own four huts rebuilt in place so the CSV never changed;
  living trees, 3 earth tones across the 192 floor tiles, and a denser green scatter. The hub
  went from five objects in a grey box to a dressed yard, and Level 2 no longer shows skybox
  through the gaps between its rooms. Every scene now tunes the built-in procedural sky —
  **no skybox and no ground texture were sourced**, exactly as the shopping list predicted;
  the one pack that was sourced (Fantasy Town Kit, CC0) went in because the Survival Kit's
  "structure" pieces turn out to be open scaffold frames, not houses.
  Elevation is scenery: the play surface is still one flat slab, the mesa is one box collider
  and the spring one sphere, and play-mode verification shows the NavMesh re-baking to 719
  triangles with complete paths from both far corners. New generator:
  [TerrainKit.cs](../../Assets/Editor/TerrainKit.cs).
  One measurement worth keeping: at pitch 50° / 60° FOV the camera can see **~19 m past
  Greenie and nothing beyond, at any height** — which is why the hills start 10 m outside the
  wall, and which the PO now has in hand for the camera-angle decision.

- _(2026-08-15)_ **C5 done — the game makes noise, and three things that were silently broken
  aren't.** The clips had been in the repo since A5 doing nothing: one prefab used three of
  them, half the scenes had no music, and every other sound field was empty.
  **`AudioSource.PlayClipAtPoint` is a 3D sound** — measured, not assumed — and the listener
  rides the camera 12.4 m behind Greenie, so every sound in the game would have played at **8%
  volume**. The 2D build had already been bitten by a milder version of this and worked around
  it twice by playing sounds at the camera's position, which reads as nonsense until you know
  what it is for. `Sfx` plays flat and does the distance itself, measured from Greenie, because
  under a fixed camera real 3D audio has nothing to say — and putting the listener on Greenie to
  give it something would rotate the stereo image every time he turned around.
  **`MusicPlayer` owns the soundtrack now**, one object across every scene. The three generated
  scenes had no music precisely because anything placed in them dies at the next Rebuild, and a
  per-scene source restarts the track on every load — in a build with portals in both directions
  and a reload on death, that is one thirty-second loop starting over all night. Now a scene
  change is inaudible.
  Three bugs fell out. **The pitch jitter was spending the gameplay RNG** — `UnityEngine.Random`
  is one sequence and the slimes draw their wander from it, so adding one draw per sound moved a
  slime 1.8 m → 2.4 m off its spawn and broke a combat test; cosmetic randomness gets its own
  generator now. **The settings panel was writing the prefab's values over the player's** — it
  attached its callbacks before syncing its widgets, and the prefab was saved muted at 100%
  music, which is exactly what the stored settings were found sitting at. And **camera shake had
  been dead**: `CameraFollow` is `[ExecuteAlways]`, so its `Awake` ran at scene-open in edit mode
  and declined to claim `Instance`, and Fast Enter Play Mode never ran it again — `GameFeel.Shake`
  had been a no-op with a perfectly healthy impulse chain underneath it. That one is the C4 rule's
  sibling: Fast Enter Play Mode changes which **lifecycle callbacks** you may assume, not just how
  long statics live.
  64/64 on the new surface, 241 regression checks green.

- _(2026-08-14)_ **C4 done — hits land, and cleaning is finally a loop.** The juice itself was
  the easy half: `Vfx` builds its bursts from code and throws them away when they stop, `GameFeel`
  holds the shake wrapper and the hit-stop, and both are wired into every beat that deserves one.
  Not a single prefab had to be rebuilt, because `Vfx.ColorOf` reads an enemy's colour off its own
  art — recolour the slime in `ArtPass` and its death poof follows.
  **The particles are meshes on URP/Lit rather than billboards**, and that is a build decision, not
  a taste one: a billboard needs one of the URP *Particles* shaders, nothing in this project
  references one, and `Shader.Find` only returns what a build actually kept — so the pretty version
  would have looked perfect in the editor and shipped as magenta squares.
  **Hit-stop crawls the clock at 2%; it never parks it at 0.** Six things in this project set
  `Time.timeScale = 0`, so a hit-stop that used 0 could not tell its own freeze from a dialogue
  that opened during it, and restoring the wrong one un-pauses a modal under the player. Crawling
  makes ownership checkable, and at 2% a 60 ms stop still reads as a freeze.
  **The bigger find was that the cleaning loop never existed.** `game-design` §4.7.5 has specified
  `GroundCleanser.CleanRadius` since M9 and both `Codex` and `ItemUse` carry comments pointing at
  it, but nothing in the 2D build ever called `AddCleanliness` — the codex's third tab showed two
  bars frozen at 0% for the entire game, and the Seed Bomb's "clears trash" half was never wired.
  Both work now, and Level 2 got 10 pieces of waste of its own, because the factory's meter turned
  out to be one that could never have moved.
  **Two bugs, both invisible, both about state that outlives what created it.** The cleanliness
  tally reset per *level* rather than per level **load**, so dying and taking "Chơi lại" — which
  reloads the same scene — doubled the authored count and put 100% permanently out of reach. And
  `ItemDatabase` had been dying on every Play but the first since M9: it caches runtime
  `ScriptableObject`s, Unity destroys those when play mode ends, and with Fast Enter Play Mode the
  dictionary survives holding all of them destroyed — so from the second Play onwards every item id
  looked unknown, consumables silently refused to work, and a build never sees any of it.
  Verified **43/43** and **9/9**, with the Mega-Smog re-run at **41/41** and the rest of the suite
  green. Probe lesson to file with C1's and C2's: **check which scene a probe was written for** —
  `ProbeFlyBot`'s arena is Level 1's open field, and running it on Level 2 drops Greenie off the
  edge of the floor slab and produces 17 confident, meaningless failures.
- _(2026-08-13)_ **C3 done — both bosses are in, and the game can be finished.** The
  Slime King and the Mega-Smog are the last two things the port was missing, and with
  them the whole chain runs: menu → intro → farm → hub → factory → boss → outro.
  The interesting work was not the bosses' behaviour — that ports almost verbatim — but
  the three things around them.
  **The HP bar became a contract.** In 2D it was authored into the Level 2 scene holding a
  hard `MegaSmogBoss` reference, and `SlimeKing` raised an identical set of events that
  nothing ever listened to. Now there is an `IBoss`, the bar builds itself in code the way
  `InventoryUI` does, and a boss calls `BossHealthBar.Bind(this)` on waking. No scene
  wiring at all, and the Slime King gets a bar he never had.
  **Where the King fights was a design call, not a port.** The 2D scene's only Slime King
  is a `TEST_SlimeKing` six metres from the player's spawn with half the prefab's health.
  The farm's other three corners each hold a guarded chest and the south-west one was
  empty, so that is where the grove went — and the bestiary line about him guarding a
  Mảnh Cổng "trong khu rừng độc" is finally true.
  **Two bugs, and both were invisible.** `health <= maxHealth * enrageThreshold` does not
  mean what it reads as: `0.35f` is really 0.34999999, so `40 * 0.35f` is 13.9999998 and
  the boss skipped its enrage at 14 HP entirely — it fired a point later or not at all,
  depending on the numbers. And a boss **cannot tint itself**: `HitFlash` caches the
  resting colour in `Awake` and repaints it after every flash, so an enrage tint applied
  any other way is scrubbed off by the very next Seed that lands. `HitFlash.SetBaseTint`
  owns the resting colour now, and multiplies rather than replaces so a machine built from
  a dozen materials goes angry instead of flat red.
  One thing worth keeping: **waking a boss is line-of-sight, not distance.** The Mega-Smog
  stands 4 m from the blast door, inside its own 7 m activation radius, so a pure distance
  check had it spraying orbs through a locked door at a player with no way to answer.
  Verified **37/37** and **41/41**, with the C1/C2/B2/B3/B4 suite re-run green at 193/193.
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
