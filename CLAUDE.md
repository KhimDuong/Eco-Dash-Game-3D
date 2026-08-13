# CLAUDE.md — Eco-Dash 3D: Biệt Đội Giải Cứu Xanh

> This file is auto-loaded by Claude Code at the start of every session. Keep it
> short and high-signal. Detailed docs live in [.claude/docs/](.claude/docs/) —
> read the one relevant to your task before making changes.

## What this project is

**Eco-Dash 3D** is the **3D remake** of the finished 2D game *Eco-Dash: Biệt
Đội Giải Cứu Xanh* — a **top-down adventure / action-RPG** built in **Unity 6
(6000.3.16f1)** with the **Universal Render Pipeline (3D)**. Think *Tunic* /
*Death's Door*: a fixed **¾ top-down camera** over a 3D world, with the
eco-cleanup robot **Greenie** reclaiming a polluted valley from the "Black
Smoke" corporation.

**The 2D project is the spec.** It lives at `d:\Y4-Sem3\Eco-Dash-Game` —
layouts, dialogue, item stats, quest logic, and most scripts come from there.
The conversion strategy is [3D-CONVERSION-PLAN.md](3D-CONVERSION-PLAN.md); the
per-developer breakdown is [TEAM-TASKS.md](TEAM-TASKS.md). Creative brief
(Vietnamese): [Eco-Dash-Game.md](Eco-Dash-Game.md); working design spec:
[.claude/docs/game-design.md](.claude/docs/game-design.md).

## The golden rules (read these first)

1. **3D top-down on the XZ plane, no platforming.** WASD moves Greenie on flat
   ground; input Y maps to **world Z**. Fixed ¾ Cinemachine camera (no player
   camera control), no jumping, gravity is never a mechanic. Never add
   side-scroller or first-person logic.
2. **Port, don't reinvent.** Before writing any gameplay script, check the 2D
   repo for its counterpart and apply the **porting tiers** in
   [.claude/docs/architecture.md](.claude/docs/architecture.md): Tier 0 copies
   verbatim, Tier 1 is a mechanical 2D→3D API swap, only Tier 2 is redesigned.
   Use the [/port-script](.claude/commands/port-script.md) command.
3. **Scripts go in [Assets/Scripts/](Assets/Scripts/)**, organized by feature
   subfolder (`Player/`, `Enemies/`, `Systems/`, `Items/`, `UI/`, `World/`,
   `Hazards/`, `Shop/`) — same tree as the 2D repo. One `MonoBehaviour` per
   file, file name = class name.
4. **The Unity Editor is live and driven via the Coplay MCP.** Use the
   `mcp__coplay-mcp__*` tools against the open editor — do **not** hand-edit
   `.unity` / `.prefab` YAML. Known pitfalls (save-in-place, scene-dirty,
   asset-ref assignment) are listed in
   [.claude/docs/unity-workflow.md](.claude/docs/unity-workflow.md).
5. **Use free 3D assets first; always credit them.** University course project,
   not published/monetized — licensing isn't a blocker, but **every borrowed
   asset goes in [CREDITS.md](CREDITS.md)**. Source order: **Kenney →
   Quaternius → KayKit → itch.io low-poly**, then other free sources. Greybox
   with ProBuilder first; **only generate models (Coplay 3D gen, billed) as a
   last resort**. Full rules: [ASSETS.md](ASSETS.md). The art itself is applied
   by a **generator** (`Eco-Dash → Run the art pass (B5)`), not by hand — add a
   row to `ArtPass.cs`, never drag a mesh onto a prefab.
6. **After any script change, verify it compiles** with
   `mcp__coplay-mcp__check_compile_errors` before saying you're done.
7. **Three devs work in parallel — respect scene ownership.** One owner per
   scene; everything placed in a scene is a **prefab instance**; shared
   contracts (interfaces, `GameManager` events, tags/layers) change only via
   Dev A. See [TEAM-TASKS.md](TEAM-TASKS.md).

## Where things are

| Path | What |
|------|------|
| [Assets/Scripts/](Assets/Scripts/) | All gameplay C# (by feature folder, mirrors 2D repo) |
| [Assets/_Scenes/](Assets/_Scenes/) | Game scenes (`MainMenu`, `Level1_BarrenFarm`, …) |
| [Assets/Models/](Assets/Models/) | 3D models & materials (`ThirdParty/<Pack>/` per pack) |
| [Assets/Prefabs/](Assets/Prefabs/) | Reusable prefabs (Player, enemies, props, greybox kit) |
| [Assets/Audio/](Assets/Audio/) | Music & SFX (mostly ported from the 2D repo) |
| [.claude/docs/](.claude/docs/) | Deep-dive docs (design, architecture, conventions, workflow) |
| [.claude/commands/](.claude/commands/) | Reusable `/slash` workflows (`/port-script`, `/new-enemy`, `/new-level`) |

## Tags & Layers (canonical — keep in sync with code)

Tags: `Player`, `Enemy`, `EnergyCore`, `Pickup`, `Projectile`, `Hazard`.
Physics layers and the collision matrix live in
[.claude/docs/unity-workflow.md](.claude/docs/unity-workflow.md).
(No sorting layers — depth sorting is free in 3D; `DynamicYSorter` was deleted.)

## Controls (design contract — unchanged from 2D)

`W/A/S/D` move on the ground plane · `J` shoot Seed projectile · `E` interact
(NPC/chest) · `Esc` pause · `I/Tab` bag · `1–4` hotbar · `Q` quest log ·
`C` codex. Read exactly as in the 2D repo: **direct polling of `Keyboard.current`**
from the Input System package (`kb.wKey.isPressed`, `kb.jKey.isPressed`, …), *not*
through action maps. [Assets/InputSystem_Actions.inputactions](Assets/InputSystem_Actions.inputactions)
is copied from the 2D repo and set as the project-wide asset, but gameplay
doesn't bind to it — don't mix in legacy `Input.GetAxis` either.

## Working agreements for the AI

- **Plan before large changes.** Restate the goal and the files you'll touch.
- **Match existing conventions.** See
  [.claude/docs/conventions.md](.claude/docs/conventions.md).
- **Keep the design doc & glossary authoritative.** Vietnamese ⇄ English term
  map: [.claude/docs/glossary.md](.claude/docs/glossary.md). Player-facing text
  is **Vietnamese**; code and comments are English.
- **When porting, diff against the 2D original.** Behavior parity is the
  acceptance test — same numbers, same events, same flow.
- **Update docs when you change architecture.** New system → add it to
  [.claude/docs/architecture.md](.claude/docs/architecture.md) and tick
  [.claude/docs/roadmap.md](.claude/docs/roadmap.md).
- **Don't invent assets silently.** Missing art → clearly-labeled greybox
  placeholder + note in [ASSETS.md](ASSETS.md) / [CREDITS.md](CREDITS.md).

## Current status

See [.claude/docs/roadmap.md](.claude/docs/roadmap.md) for the live backlog.
**P0 done; A1–A6, B1–B5, C1–C2 done — every scene exists and none of them is grey any
more.** The URP-3D project, Tier-0 + Tier-1 scripts, `Player.prefab`, the Cinemachine
`CameraRig.prefab`, the whole UI layer (`HUD.prefab`, `GameManager.prefab`,
`MainMenu` / `Intro_Story` / `Ending_Story`), **`Level1_BarrenFarm`** with its 29
`PlasticSlime`s, save/continue parity, the **`PollutionFlyBot` + `SmogOrb`** combat kit,
**`Level2_FactoryMaze`** with its lasers, manholes, keycard chain and boss door, the
**`Shop_RecyclingStation`** hub with Ông Bear's shop, the crafting bench and the two
stage portals, and **B5's art pass** — five CC0 packs, real models on every prefab, and a
per-scene lighting/post look — are all in and play-mode verified.

Next up: **C3** (`SlimeKing` + `MegaSmogBoss`) — the last thing between here and a full
start-to-ending playthrough. Six generated things — **don't hand-edit their output**:

| What | Menu command | Source |
|---|---|---|
| Level 1 | **Eco-Dash → Rebuild Level 1** | [Tools/level1_layout.csv](Tools/level1_layout.csv) |
| Level 2 | **Eco-Dash → Rebuild Level 2** | [Tools/level2_layout.csv](Tools/level2_layout.csv) |
| The hub | **Eco-Dash → Rebuild the hub** | `Assets/Editor/HubBuilder.cs` |
| Enemy prefabs | **Eco-Dash → Rebuild enemy prefabs** | `Assets/Editor/EnemyPrefabBuilder.cs` |
| Factory kit | **Eco-Dash → Rebuild factory kit** | `Assets/Editor/FactoryKitBuilder.cs` |
| The art | **Eco-Dash → Run the art pass (B5)** | `Assets/Editor/ArtPass.cs` + `ArtKit.cs` + `SceneLook.cs` |

The last three of those rebuild their prefabs from primitives, so each one **re-runs its
slice of the art pass at the end**. Change art by editing `ArtPass.cs`, never by dragging
a mesh onto a prefab — the next rebuild would throw it away.

**Two rules that keep biting:**

1. **Height is presentation, hitting things is XZ.** Greenie's Seeds fly flat at y ≈ 0.6,
   so anything that leaves the ground still needs a hurtbox reaching the ground plane or
   it is simply unkillable.
   See [architecture.md](.claude/docs/architecture.md#flying-enemies-need-two-colliders-c2).
2. **Only the visual is ever swapped.** Colliders, trigger radii and the toggled-child
   pairs (`Visual_Open`/`Visual_Locked`, `Lid`/`Hole`, `Visual_Unconscious`/`Visual_Awake`)
   are the gameplay contract. And a `MaterialPropertyBlock` is **per material slot, not
   per renderer** — the thing that made the new slime's eyes turn green after one hit.
   See [architecture.md](.claude/docs/architecture.md#the-art-pass-is-generated-too-b5).
