---
description: Port a script from the 2D repo into this 3D project by tier rules
---

Port the 2D script **$ARGUMENTS** (a path or class name from
`d:\Y4-Sem3\Eco-Dash-Game\Assets\Scripts\`) into this project.

1. Read the 2D source file, and determine its **tier** from the table in
   [.claude/docs/architecture.md](../docs/architecture.md) §Porting tiers.
2. Port by tier:
   - **Tier 0:** copy verbatim to the same subfolder under `Assets/Scripts/`.
     Change nothing but what's needed to compile. Any urge to "improve" it →
     stop and flag it instead.
   - **Tier 1:** copy, then apply only the mechanical swaps —
     `Rigidbody2D`→`Rigidbody`/`CharacterController`, `Collider2D`→`Collider`,
     `OnTriggerEnter2D/Exit2D/OnCollisionEnter2D`→3D names,
     `Physics2D.Raycast`→`Physics.Raycast` (layer-masked to `Obstacle`),
     `Vector2 (x,y)`→`Vector3 (x,0,z)`, sprite flip → visual-child
     `LookRotation`, sprite tint flash → MaterialPropertyBlock emission flash.
     **All tunable values stay numerically identical** (1 tile = 1 m).
   - **Tier 2:** read the redesign row for this script in architecture.md and
     implement that design, preserving the 2D script's public API, events, and
     tunable names wherever possible.
3. Check every reference the script makes (interfaces, `GameManager` events,
   tags) already exists here; if a dependency isn't ported yet, port it first
   (same rules) or stub it with a clearly-marked `// TODO(port)`.
4. Verify with `mcp__coplay-mcp__check_compile_errors`.
5. Diff your result against the 2D original and report: tier used, every
   non-mechanical change you made, and anything that still needs scene/prefab
   wiring (which the scene owner must do — don't edit scenes you don't own).
