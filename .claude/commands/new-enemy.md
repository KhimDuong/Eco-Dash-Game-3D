---
description: Scaffold a new 3D enemy (script + prefab) following project conventions
---

Create a new enemy for Eco-Dash 3D named **$ARGUMENTS**.

Follow the project conventions before writing anything:
1. Read [.claude/docs/conventions.md](../docs/conventions.md) and the enemy
   Tier-2 table in [.claude/docs/architecture.md](../docs/architecture.md).
2. Look at `Assets/Scripts/Enemies/PlasticSlime.cs` (ported) as the reference
   pattern. If a similar enemy exists in the 2D repo
   (`d:\Y4-Sem3\Eco-Dash-Game\Assets\Scripts\Enemies\`), start from it via
   [/port-script](port-script.md) instead of writing from scratch.

Then:
1. Create `Assets/Scripts/Enemies/<Name>.cs` — a `MonoBehaviour` with:
   - `[Header]`-grouped `[SerializeField]` tunables (speed, HP, contact damage, aggro radius…).
   - **Ground chaser:** NavMeshAgent (`SetDestination` toward the player when
     aggroed). **Flyer:** Rigidbody (freeze X/Z rotation), hover on Y,
     `linearVelocity` in `FixedUpdate`.
   - LOS via `Physics.Raycast` layer-masked to `Obstacle`.
   - `CompareTag("Player")` contact damage → `PlayerHealth.TakeDamage`.
   - Implement `IDamageable` (Seeds kill it) and `IKnockbackable`; hit-flash
     via MaterialPropertyBlock emission.
2. Verify compile with `mcp__coplay-mcp__check_compile_errors`.
3. Build a prefab in the live editor via Coplay MCP: GameObject + greybox mesh
   child (labelled `Greybox*` until art is sourced) + collider + the script;
   tag `Enemy`; layer `Enemy`. Save to `Assets/Prefabs/`. **Do not place it in
   a level scene yourself** — hand the prefab to the scene's owner
   ([TEAM-TASKS.md](../../TEAM-TASKS.md)); test in your sandbox scene.
4. Update [.claude/docs/roadmap.md](../docs/roadmap.md) and the architecture
   Tier-2 table; add a bestiary entry hook if the codex needs one.
5. If new art is used, log it in [CREDITS.md](../../CREDITS.md) /
   [ASSETS.md](../../ASSETS.md).

Confirm what you created and how the scene owner should place it.
