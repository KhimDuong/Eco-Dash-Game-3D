# Eco-Dash 3D — Conversion Plan

> Goal: rebuild **Eco-Dash: Biệt Đội Giải Cứu Xanh** (2D top-down, Unity 6 URP-2D)
> as a **3D top-down action-RPG** in this repo, reusing as much of the 2D
> project's code and design as possible. Source project:
> `d:\Y4-Sem3\Eco-Dash-Game`. Team: 3 developers vibe-coding with
> **Claude + Unity Coplay MCP**. Per-developer tasks: [TEAM-TASKS.md](TEAM-TASKS.md).

---

## 1. Vision & guardrails

**Same game, new dimension.** Think *Tunic* / *Death's Door* / 3D Zelda camera:
a fixed **¾ top-down camera** looking down at Greenie on a flat ground plane.
The design contract does not change:

- `W/A/S/D` move on the ground plane · `J` shoot Seed · `E` interact · `Esc` pause.
- **No jumping, no platforming, no camera control by the player.** Gravity exists
  (things sit on the ground) but is never a gameplay mechanic.
- Same campaign flow: `MainMenu → Intro_Story → Level1_BarrenFarm →
  Level2_FactoryMaze (boss) → Ending_Story`, with the **Hub (Recycling Station)**
  reachable by portals; same quests, inventory, crafting, codex, shop upgrades.
- Vietnamese UI text and the glossary from the 2D repo stay authoritative.

**Engine:** Unity 6 (6000.3.16f1) + **URP 3D** template. New project lives at the
root of this repo.

**Assets:** free-first, exactly as in the 2D project — low-poly packs from
**Kenney (kenney.nl), Quaternius, KayKit, itch.io** (nature/farm packs for L1,
sci-fi/factory packs for L2, robot character for Greenie). Every borrowed asset
goes in `CREDITS.md`. Coplay's `generate_3d_model_*` tools are the **last
resort** only when no free asset fits. Greybox everything with **ProBuilder**
first; art is a replaceable skin.

**Workflow:** all scene/prefab work through the **Coplay MCP** against the live
editor — never hand-edit `.unity`/`.prefab` YAML. After any script change, run
`check_compile_errors` before calling it done.

---

## 2. 2D → 3D technical map

| 2D (current) | 3D (replacement) |
|---|---|
| Tilemap + Tilemap/Composite Collider walls | ProBuilder greybox (floor plane + wall blocks) → later modular 3D props; Box/Mesh colliders |
| `Rigidbody2D` (gravityScale 0) on player | **CharacterController**, movement on the **XZ plane** |
| `Rigidbody2D` on enemies | Rigidbody (freeze X/Z rotation) or NavMeshAgent for chasers |
| `Collider2D` / `OnTriggerEnter2D` / `OnCollisionEnter2D` | `Collider` / `OnTriggerEnter` / `OnCollisionEnter` |
| `Physics2D.Raycast` (enemy LOS) | `Physics.Raycast` |
| `Vector2` movement (`input.x, input.y`) | `Vector3(input.x, 0, input.y)` — **input Y maps to world Z** |
| `SpriteRenderer` + `DynamicYSorter` | MeshRenderer; **delete Y-sorting entirely** (depth is free in 3D) |
| Directional sprite flip | rotate the visual child to face move direction (`Quaternion.LookRotation`) |
| `PlayerAnimator` hover-bob + squash-stretch | **keep the same trick** — animate the visual child Transform in 3D |
| White hit-flash (sprite tint) | material **emission/color flash** (MaterialPropertyBlock) |
| `CameraFollow` + shake | **Cinemachine** follow cam at fixed ¾ angle (pitch ~50°, distance ~12) + impulse shake |
| 2D VFX (sway, mud bubbles, wisps) | simple Particle Systems / animated transforms |
| Screen-space Canvas UI (HUD, menus, bag, codex…) | **unchanged** — ports as-is |
| Boss/NPC overhead labels | world-space canvas billboards (optional) |

**Tags & layers (copy from 2D, canonical):** `Player`, `Enemy`, `EnergyCore`,
`Pickup`, `Projectile`, `Hazard`. Recreate the same physics-layer collision
matrix in P0.

---

## 3. Script porting tiers

The 2D repo has ~75 scripts. Sort them into three buckets:

**Tier 0 — copy verbatim (no physics, dimension-agnostic).**
`Systems/` (GameManager, Inventory, SaveSystem, QuestLog, QuestProgress, Codex,
PlayerProgress, GameSettings, Crafting, ItemUse, SceneProgress, all catalogs),
`Items/ItemDef`, `ItemDatabase`, `CraftingRecipe`, all of `UI/` except
`DynamicYSorter`, all of `Shop/`, `UI/Dialogue/`, `MenuController`,
`World/IInteractable`, `Enemies/IDamageable`, `Enemies/IKnockbackable`.
→ These are the game's brain. **Do not rewrite them; copy and compile.**

**Tier 1 — mechanical port (find/replace 2D physics API, logic unchanged).**
`Player/*` (Controller, Health, Shooter, Interactor, Animator),
`Items/SeedProjectile`, `EnemyProjectile`, all pickups (`ItemPickup`,
`HealthPickup`, `SpeedBoostPickup`, `QuestItemPickup`, `EnergyCore`),
`World/*` triggers (Chest, Keycard, Litter, LoreNote, portals/gates, BossDoor,
CraftingBench, NPC interactables, ReclamationPatch), `Hazards/ToxicMud`,
`ToxicGasZone`, `ManholeTrap`.

**Tier 2 — redesign for 3D.**
- **Enemies:** `PlasticSlime` (ground chaser — consider NavMeshAgent),
  `PollutionFlyBot` (now *actually flies* — hover on Y, LOS via 3D raycast),
  `SlimeKing`, `MegaSmogBoss` (8-dir spray becomes a horizontal ring on XZ).
- `Hazards/SweepingLaser` (3D beam: LineRenderer/scaled cylinder + trigger).
- `Systems/CameraFollow` → replaced by Cinemachine (keep the `Shake` API as a
  thin wrapper so callers don't change).
- `UI/DynamicYSorter` → **deleted**.
- `Environment/*` cosmetic effects → rebuilt as cheap 3D equivalents or dropped.
- Level layouts: **rebuilt by hand in 3D**, using the 2D scenes as the map
  reference (same room shapes, same item/NPC placement).

---

## 4. Scenes to build (same six)

| Scene | 3D approach |
|---|---|
| `MainMenu` | port canvas as-is; optional low-poly diorama behind it |
| `Intro_Story` / `Ending_Story` | pure UI slides — port unchanged |
| `Level1_BarrenFarm` | ground plane + farm greybox; toxic-mud pools as flat trigger decals; chests → reclamation patches → teleport gate |
| `Level2_FactoryMaze` | ProBuilder corridor maze; lasers, manholes, gas; keycards → boss door → Mega-Smog arena |
| `Shop_RecyclingStation` (Hub) | small interior/plaza: Ông Bear shop, crafting bench, Portal Nexus, relocated NPCs |

---

## 5. Phases & gates

**P0 — Bootstrap (Day 1–2, Dev A leads, others assist).**
New Unity 6 URP-3D project at repo root; folder tree mirrors the 2D repo
(`Assets/Scripts/{Player,Enemies,Systems,Items,UI,World,Hazards,Shop}`,
`_Scenes`, `Prefabs`, `Models`, `Audio`); tags/layers/collision matrix; copy
`InputSystem_Actions.inputactions`; copy Tier-0 scripts; Unity Smart Merge for
YAML. The vibe-coding docs (`CLAUDE.md`, `.claude/docs/`, `.claude/commands/`,
`ASSETS.md`, `CREDITS.md`, `.gitignore`) are **already seeded** — keep them
current.
**Gate:** project opens, Tier-0 code compiles clean, repo pushed.

**P1 — Vertical slice (Week 1).**
Greenie (capsule placeholder) moves/shoots/takes damage in a greybox L1 corner;
one PlasticSlime chases and dies; one chest → energy core → HUD counter ticks;
Cinemachine follows. **Gate:** playable 2-minute loop, no exceptions in Play mode.

**P2 — Full port (Week 2).**
Complete L1 (3 cores, mud, quest NPCs, gate), L2 (maze, lasers, manholes,
fly-bots, keycards, Mega-Smog boss), Hub (shop, bench, portals), quest chain
(Ông Sáu/Tí), inventory/crafting/codex live, story scenes, save/persistence.
**Gate:** full start→ending playthrough possible.

**P3 — Polish (Week 3).**
Free-asset art pass (character model, props, factory kit), lighting + post
(URP volumes), audio port + new ambience, hit/clean VFX, settings verified,
`CREDITS.md` complete, full manual playthrough + time-budget check.
**Gate:** submission build.

---

## 6. Three-dev coordination rules (read before touching the editor)

Vibe-coding in parallel against Unity scenes is merge-conflict bait. Rules:

1. **One owner per scene.** Never have two people with the same `.unity` open
   for editing. Cross-scene needs go through the scene's owner.
2. **Prefab-first.** Everything placed in a scene is a prefab instance. You can
   safely edit a prefab someone else's scene uses — that's the integration point.
3. **Contracts freeze in P0.** `IDamageable` / `IInteractable` / `IKnockbackable`,
   `GameManager` events, tags/layers, and the `Inventory`/`Codex` APIs are
   copied from 2D and then **owned by Dev A** — propose changes, don't just make them.
4. **Branch per task** (`feat/a3-player-controller`), PR to `main`, and run
   `check_compile_errors` before every push. Never push a red project.
5. **Coplay pitfalls (learned in the 2D project):**
   - `save_scene` does a "Save As" into `Assets/` root — save in place via
     `execute_script` (`EditorSceneManager.SaveScene(activeScene)`).
   - Objects created in `execute_script` may not dirty the scene — call
     `MarkSceneDirty` before saving, then verify the file on disk changed.
   - `set_property` can't assign asset refs (AudioClip/Material/Mesh) in prefab
     context — use `execute_script` + `SerializedObject`.
   - Don't trust `Debug.Log` read-back — write probe results to
     `Temp/CoplayExec/*.txt` and read the file.
6. **Reference, don't invent.** The 2D repo is the spec: layouts, dialogue
   text, item stats, quest logic all come from `d:\Y4-Sem3\Eco-Dash-Game` and
   its `.claude/docs/` (game-design.md, glossary.md). When in doubt, open the
   2D scene and copy its numbers.
