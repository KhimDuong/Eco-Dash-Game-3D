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
   last resort**. Full rules: [ASSETS.md](ASSETS.md).
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
`C` codex. Implemented via the Input System asset copied from the 2D repo
([Assets/InputSystem_Actions.inputactions](Assets/InputSystem_Actions.inputactions)).

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
As of repo creation: **P0 bootstrap** is the active target — the Unity project
itself hasn't been created yet (Dev A task A1 in
[TEAM-TASKS.md](TEAM-TASKS.md)).
