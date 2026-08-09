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
- [ ] **B1** Greybox kit + `Level1_BarrenFarm` blockout (2D layout as map) + NavMesh
- [ ] **C1** `PlasticSlime` 3D (NavMeshAgent chase, contact damage, hit-flash,
  death drop) in sandbox + placed in L1
- [ ] Slice gate: chest → core → HUD tick, slime fight, no Play-mode exceptions

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
  - [ ] `menu → intro → L1` end-to-end: `Intro_Story.nextScene` is already
    `Level1_BarrenFarm`; the last hop needs **B1**'s scene to exist
- [ ] **A6** Persistence parity (save/continue, New Game reset)
- [ ] **B2** L1 complete (3 chests/cores, reclamation patches, mud, litter,
  pickups, Bà Tư + Ông Sáu + herbs, teleport gate)
- [ ] **B3** L2 Factory Maze (corridors, lasers, manholes, gas, keycards ×3,
  boss door, Tí at entrance)
- [ ] **B4** Hub (shop, crafting bench, Portal Nexus + return portals,
  side-quest NPCs)
- [ ] **C2** Projectiles + `PollutionFlyBot` (true Y-hover, 3D LOS)
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
