---
description: Scaffold a new 3D top-down level scene following the scene-build checklist
---

Create a new level scene for Eco-Dash 3D named **$ARGUMENTS** (e.g. "Level3_Riverside").

1. Read the **Scene-build checklist** and **Tags & Layers** in
   [.claude/docs/unity-workflow.md](../docs/unity-workflow.md), plus the level
   spec in [.claude/docs/game-design.md](../docs/game-design.md). If this level
   exists in the 2D repo, open its `.unity` there as the layout reference —
   same room shapes and placements, 1 tile = 1 m.
2. Confirm the editor is connected (`list_unity_project_roots`), not in play
   mode, and that **you own this scene** ([TEAM-TASKS.md](../../TEAM-TASKS.md)).
3. Build via Coplay MCP, in this order:
   - `create_scene` → `Assets/_Scenes/<Name>.unity`; add to Build Settings.
   - Greybox-kit floor/wall prefab instances (colliders, static) → bake NavMesh.
   - `Player.prefab` instance at spawn; Cinemachine vcam follows it (fixed ¾).
   - One `GameManager` with the level's objective config.
   - Enemies / hazards / pickups as **prefab instances** per the design spec.
   - `HUD.prefab` canvas subscribed to `GameManager` events.
   - Directional light + URP volume (per-level profile).
4. `check_compile_errors` → save the scene **in place** (see workflow gotchas —
   not bare `save_scene`) → verify the file on disk changed.
5. Update [.claude/docs/roadmap.md](../docs/roadmap.md).

Report the scene path, build index, and how to play-test it.
