# Roadmap / Backlog — Eco-Dash 3D

Living checklist of what's built and what's next. Update the boxes as work
lands; keep newest status at the top of "Recent log". Milestones = the phases
in [../../3D-CONVERSION-PLAN.md](../../3D-CONVERSION-PLAN.md); task details &
owners in [../../TEAM-TASKS.md](../../TEAM-TASKS.md).

## Milestones

### P0 — Bootstrap 🔧 (active)
- [ ] **A1** Unity 6 (6000.3.16f1) URP-3D project at repo root; folder tree;
  tags/layers + collision matrix; Unity `.gitignore` (done — seeded) + Smart
  Merge; `InputSystem_Actions.inputactions` copied; ProBuilder + Cinemachine
  installed; pushed to `main`
- [x] Vibe-coding docs seeded (`CLAUDE.md`, `.claude/docs/`, `.claude/commands/`,
  `ASSETS.md`, `CREDITS.md`, plan + team tasks)
- [ ] **A2** Tier-0 scripts copied from 2D repo; compiles clean

### P1 — Vertical slice (Week 1)
- [ ] **A3** Player 3D (`CharacterController`, XZ move, shoot J, health,
  interact E, hover-bob visual child) as `Player.prefab`
- [ ] **A4** Cinemachine ¾ follow cam + `CameraShaker` (old `Shake` API kept)
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

- _(2026-07-09)_ **Repo seeded.** Conversion plan, 3-dev task split, and the
  full vibe-coding doc ecosystem (CLAUDE.md, docs, commands, ASSETS/CREDITS,
  Unity .gitignore) created. Unity project itself not yet created — A1 is next.
