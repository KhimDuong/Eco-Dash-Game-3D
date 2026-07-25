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
  - [ ] human check: WASD moves / J fires / visual turns to face (keyboard input
    can't be synthesised from the MCP)
  - [ ] E-interact end-to-end — needs a real `IInteractable` in the scene
    (arrives with B2's `Chest`)
- **A4** ¾ follow cam + `Shake` API kept
  - [x] `CameraFollow` 3D rig: pitch 50°/dist 12 verified in play mode
    (camera at `(0, 9.69, -7.71)`, no yaw/roll), `Instance`/`Shake` intact
  - [ ] swap the follow half to a Cinemachine vcam + impulse source
- [ ] **B1** Greybox kit + `Level1_BarrenFarm` blockout (2D layout as map) + NavMesh
- [ ] **C1** `PlasticSlime` 3D (NavMeshAgent chase, contact damage, hit-flash,
  death drop) in sandbox + placed in L1
- [ ] Slice gate: chest → core → HUD tick, slime fight, no Play-mode exceptions

### P2 — Full port (Week 2)
- [ ] **A5** HUD/menus wired (bag, hotbar, quest log, codex, pause, settings,
  end screens); `MainMenu` + story scenes; Build Settings order
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
