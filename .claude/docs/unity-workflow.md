# Unity Workflow (Coplay MCP) — 3D

This project's Unity Editor is driven live through the **Coplay MCP** tools
(`mcp__coplay-mcp__*`). Prefer them over hand-editing `.unity`/`.prefab` YAML,
which is fragile and easy to corrupt.

## Before you touch the scene

1. Confirm the editor is connected: `list_unity_project_roots`.
2. Check state: `get_unity_editor_state` (play mode? selection? active scene?).
3. **Never edit scene objects while `playMode: true`** — changes are lost on stop.
4. **Check scene ownership** ([../../TEAM-TASKS.md](../../TEAM-TASKS.md)): only
   the scene's owner edits it. Everyone else works in prefabs (or their own
   sandbox scene) and hands prefabs to the owner.

## Common operations → tool

| Goal | Tool(s) |
|------|---------|
| New scene | `create_scene`, then `open_scene` |
| New GameObject | `create_game_object` |
| Add a script/component | `add_component` |
| Set a field/property | `set_property` (⚠ not asset refs — see Gotchas) |
| Position/rotation/scale | `set_transform` (world) / `set_rect_transform` (UI) |
| Parent objects | `parent_game_object` |
| Tags / layers | `set_tag`, `set_layer` |
| Make a prefab | `create_prefab`; instances via `place_asset_in_scene` |
| Materials | `create_material`, `assign_material`, `assign_material_to_fbx` |
| Terrain (if used) | `create_terrain` |
| Animator / clips | `create_animator_controller`, `create_animation_clip`, `create_blend_tree_state` |
| Rigged-model animation | `search_animation_library`, `apply_animation_to_rigged_model`, `auto_rig_3d_model` |
| UI | `create_ui_element`, `set_ui_text`, `set_ui_layout` |
| Generate 3D model (💰 last resort) | `generate_3d_model_from_text` / `_from_image` — see [ASSETS.md](../../ASSETS.md) |
| SFX / music (💰) | `generate_sfx`, `generate_music` |
| Inspect | `get_game_object_info`, `list_game_objects_in_hierarchy`, `capture_scene_object` |
| Verify build | `check_compile_errors` |
| Save | **in place via `execute_script`** — see Gotchas; not bare `save_scene` |

## Canonical Tags & Layers

Create these in the editor and keep them matching the code.

**Tags:** `Player`, `Enemy`, `EnergyCore`, `Pickup`, `Projectile`, `Hazard`.

**Physics layers:** `Player`, `Enemy`, `PlayerProjectile`, `EnemyProjectile`,
`Obstacle`, `Trigger`, `Ground`. Collision matrix: `PlayerProjectile` hits
`Enemy` + `Obstacle` but **not** `Player`; `EnemyProjectile` hits `Player` +
`Obstacle` but **not** `Enemy`; `Trigger` collides with `Player` only.

**No sorting layers.** Depth sorting is free in 3D — the 2D repo's
`DynamicYSorter` has no 3D counterpart; never port it.

## Scene-build checklist (Level template)

1. `create_scene` → name `LevelN_Name`, save to `Assets/_Scenes/`.
2. Add to **Build Settings** in the canonical order
   (`MainMenu`=0, `Level1_BarrenFarm`=1, `Level2_FactoryMaze`=2,
   `Shop_RecyclingStation`=3, `Intro_Story`=4, `Ending_Story`=5 — same as 2D).
3. **Ground & walls:** greybox-kit prefabs (floor slabs, wall blocks) on layer
   `Ground`/`Obstacle` with Box/Mesh colliders. Use the 2D scene's tilemap as
   the map reference — same room shapes and distances (1 tile ≈ 1 m).
4. **NavMesh:** mark greybox static → bake (chaser enemies use NavMeshAgent).
5. **Player:** instance `Player.prefab` at spawn; confirm the Cinemachine
   camera targets it (fixed ¾ angle: pitch ~50°, distance ~12, no player control).
6. **Enemies / hazards / pickups:** place prefab instances per the design spec.
7. **Canvas + HUD:** `HUD.prefab` instance; `HudController` subscribed to
   `GameManager` events.
8. **GameManager:** one per scene; set the level's objective config
   (`requiredCores` etc.).
9. **Lighting:** one Directional Light + URP global volume (per-level profile).
10. `check_compile_errors` → save scene **in place** → verify the `.unity` file
    on disk actually changed.

## Gotchas (hard-won — do not rediscover these)

- **`save_scene` does a "Save As" into `Assets/` root.** Save in place instead
  via `execute_script`:
  `EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());`
- **Objects created inside `execute_script` may not dirty the scene** — call
  `EditorSceneManager.MarkSceneDirty(...)` before saving, then confirm the
  scene file's timestamp/content changed on disk, or your work silently vanishes.
- **`set_property` cannot assign asset references** (Material, Mesh, AudioClip,
  prefab refs) in prefab context — use `execute_script` with `SerializedObject`
  / `AssetDatabase.LoadAssetAtPath`.
- **Don't trust `Debug.Log` read-back** from `execute_script` via
  `get_unity_logs` — write probe output to `Temp/CoplayExec/<name>.txt` and
  read the file instead.
- **Unity 6 renamed `Rigidbody.velocity` → `linearVelocity`** (3D too, not just
  2D). Use the new name.
- The **new Input System** is the active backend
  (`Assets/InputSystem_Actions.inputactions`, copied from the 2D repo). Don't
  mix in legacy `Input.GetAxis`.
- **Imported models:** check scale on import (1 unit = 1 m; Greenie ≈ 1 m tall),
  and convert/assign **URP Lit** materials — built-in-pipeline materials render
  magenta. See [ASSETS.md](../../ASSETS.md).
- Always save (in place) at the end; MCP changes to the live editor are not on
  disk until saved.
