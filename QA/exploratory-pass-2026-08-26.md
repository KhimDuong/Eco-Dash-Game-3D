# Exploratory pass — 2026-08-26 · the cycle-2 environment commit

> **Scope: the terrain, not the game.** This pass covers commit
> **`2f51336` ("BA PO improvement 1")** — the cycle-2 environment work (`TerrainKit.cs`,
> the Nature palette re-author, the village, the hub yard, Level 2's underlay). That is the
> **only** commit landed since the QA folder was last updated
> ([exploratory-pass-2026-08-18.md](exploratory-pass-2026-08-18.md)), so **none of it has
> ever been through QA**. Everything below is new ground.
>
> Severity language is § 0 of [CYCLE-2-TASKS.md](../CYCLE-2-TASKS.md).
> **Nothing in the project was changed by this pass.** No scene was saved, no script edited;
> `git status` afterwards shows only the six new `QA/screenshots/c2_*.png` evidence files.

## How this run was done (and what it therefore cannot tell you)

Four of the eight findings were **reported by Khiêm from his own keyboard playthrough**; this
pass reproduced and *measured* each one rather than taking it on trust, then went looking for
whatever else shared a root cause. The other four are new.

Driven in the editor across three Play sessions (Level 1 ×2, hub ×1). As in the 2026-08-18
pass, **simulated keys don't reach an unfocused Game view**, so movement was driven by calling
`CharacterController.Move` in a loop — the *same call* `PlayerController.Move` makes, against
the same colliders, so stop positions are authentic. Geometry was cross-checked independently
with `Physics.CapsuleCast` / `CheckCapsule` using Greenie's real capsule (r 0.35, h 1.15).
Every distance below was measured at least twice, by two different methods, and they agree.

Consequences, all still open:

- **No key binding was exercised.** Same gap as the last pass.
- **The Play sessions inherited Khiêm's save** (`Rác: 10`, cores 3/3, side quests done), so
  the screenshots show a mid-game valley, not a fresh start. This is why the reclaimed
  ground is already green in them — useful here, but it is not a clean-run baseline.
- **No end-to-end playthrough, no timing, no build, one aspect ratio.** This was a targeted
  environment pass; Level 2 was only checked for the underlay.
- **Console is clean.** Zero errors or exceptions across all three sessions; the only warning
  is the pre-existing, unrelated Coplay editor-toolbar one.

---

## Summary

| # | Severity | What | Where | Reported by | Status |
|---|---|---|---|---|---|
| C1 | S3 | An invisible wall stops Greenie **0.45 m short of the spring** — the water can never be touched | `TerrainKit.cs:344` | **Khiêm** | **fixed** |
| C2 | **S2** | The same blocker is a **2 m invisible dome** that destroys Seeds in mid-air over open water | `TerrainKit.cs:347` | new | **fixed** |
| C3 | S3 | The mesa's collider stands in **6.5 m² of open ground**; three corners are phantom walls up to **1.75 m** deep | `TerrainKit.cs:292` | **Khiêm** | **fixed** |
| C4 | S3 | **Every object in the hub is walk-through** — 25 yard props, Ông Bear, the counter, the bench | `HubBuilder.cs:154` | **Khiêm** | **fixed** |
| C5 | S3 | Reclamation repaints **whole 4 m tiles**: a 3.5 m patch greens 96–128 m², 2.5–3.3× its own disc | `ReclamationPatch.cs:99` | new | **fixed** |
| C6 | S4 | The three earth tones differ by **8.6%** on 4 m tiles and read as a visible checkerboard | `Level1Builder.cs:86` | new | **fixed** |
| C7 | S4 | Level 1 walk-through props: **3 village lanterns (2.6 m)** and the beached canoe | `TerrainKit.cs:464` | new | **fixed** |
| C8 | S4 | The mesa reads as a stacked layer-cake rather than a rock formation | `TerrainKit.cs:255` | new | **open** — deferred, see [C8](#c8--the-mesa-reads-as-a-stacked-layer-cake-s4) |
| R1 | — | *Undulating ground* — **change request, not a defect**; asks to change golden rule #1 | golden rule #1 | **Khiêm** | **done 2026-08-31** as cycle-3 **B8** — see [R1](#r1--undulating-ground-change-request-not-a-defect) |

**C1–C7 were fixed in commit `e3d42ac` ("Fixed bug")** and re-verified on 2026-08-26 — see
[Fix log](#fix-log--2026-08-26) at the bottom for what changed and how each one was measured.
The finding write-ups below are left as they were written, so the evidence trail still reads
straight. **C8 is deliberately deferred** as a PO call — cosmetic polish not worth spending
before the A2 build. **R1 was deferred here and has since been taken**: it shipped on
2026-08-31 as cycle-3 **B8**, and this pass's costing of it is what the work was scoped from.

**C1 and C2 are one bug with two symptoms** — a single oversized sphere — and one change fixes
both. **C4 and C7 are also one bug**: `ArtKit.Spawn` places a visual and never a collider, so
every prop that goes through it directly (rather than through a greybox prefab) is a ghost.

Khiêm's fourth report — *"the ground should be ups and downs"* — is **not a defect**; it asks
to change golden rule #1. It is written up as **[R1](#r1--undulating-ground-change-request-not-a-defect)**
at the bottom, with what it would actually cost.

### Evidence

Six Game-view captures in [`QA/screenshots/`](screenshots/), taken at the game's own framing
(pitch 50°, FOV 60) with the world frozen so nothing could interfere:

| Finding | Screenshot |
|---|---|
| C1 pond wall | `c2_01_pond_invisible_wall` — Greenie pressed against nothing, a clear gap to the water |
| C3 mesa corner | `c2_02_mesa_corner_wall` — Greenie stopped ~2 m clear of the rock |
| C5 + C6 ground | `c2_03_reclamation_tiles` — green **squares**, and the brown checkerboard behind them |
| C4 hub | `c2_04_hub_inside_stall`, `c2_05_hub_inside_bear` — Greenie is *inside* the stall and *inside* Ông Bear, invisible in both |
| C8 mesa shape | `c2_02_mesa_corner_wall` (same shot) |

---

## C1 — An invisible wall stops Greenie 0.45 m short of the spring *(S3)*

- **Severity:** S3 — works, but wrong. Nothing is blocked; it just feels broken.
- **Scene / system:** `Level1_BarrenFarm` · `TerrainKit.Pond`
- **Steps:** 1. Go to the spring at the mesa's foot, around (−24.5, 10.5). 2. Walk into it
  from any direction.
- **Expected:** reach the water. Khiêm's words: *"I should sink a little when I enter the puddle."*
- **Actual:** Greenie stops dead in open dirt with a visible gap to the water's edge. He
  cannot touch the pond from any angle.

**Measured** — three approach directions, two independent methods, all agreeing:

```
water disc visible radius        3.40 m
body edge stops at               2.95 m from centre   (south, east, north)
                                 -> 0.45 m of ground + all of the water unreachable
```

The blocker is a `SphereCollider` of radius 3.40 sunk to `center.y = −1.4`. Sinking it was
deliberate — the comment says it *"lets the player stand on the bank instead of stopping short
of it"* — but the arithmetic goes the other way. A sphere cut at Greenie's shins is
*narrower* than at its equator: at his capsule's lowest point the blocking radius is only
3.09 m, and his own 0.35 m body radius takes the contact point out to 3.30 m of centre
distance. **The blocker is doing exactly what it was built to do; it was just sized against
the wrong cross-section.**

- **Cheapest fix that keeps the current design:** give the water disc its own convex
  `MeshCollider` instead (the cylinder primitive is already the right shape), scaled to
  `(6.8, 0.45, 6.8)`. Its rim then coincides with the *visible* rim at 3.40 m, so Greenie
  stops exactly where the water starts — **and it also fixes [C2](#c2--the-same-blocker-is-a-2-m-invisible-dome-that-eats-seeds-s2)**, because
  its top face sits below seed height.
- **Fix that gives Khiêm what he actually asked for:** delete the blocker and make the pond a
  *wade* volume — a trigger that slows Greenie and drops his visual ~0.15 m. `PlayerController`
  already has the hook: `EnterMud()` / `ExitMud()` with `mudSlowMultiplier`, exactly the
  `ToxicMud` pattern, so this is a new trigger + a visual offset and **no new movement code**.
  Real sinking (the ground itself dipping) is [R1](#r1--undulating-ground-change-request-not-a-defect) and much more expensive.

---

## C2 — The same blocker is a 2 m invisible dome that eats Seeds *(S2)*

**New, and the most surprising thing this pass found.** The pond blocker is not just a wall
around the water — it is a **dome over it**, and it destroys the player's only weapon in
mid-air above what looks like open water.

- **Severity:** S2 — a designed feature (shooting) silently fails in a region of the map.
  Downgrade to S3 if the PO reads it as local rather than broken.
- **Steps:** 1. Stand south of the spring. 2. Fire north (`J`) at anything on the far side —
  the slime spawn at (−19.3, 12.2) is in range. 3. Watch the seed vanish over the water.

**Measured:**

```
seed fire height (firePoint)          y = 0.60 m        (matches CLAUDE.md rule 1)
blocker radius at that height             2.75 m
blocker top                           y = 2.00 m
a seed fired across the pond dies at  (-24.50, 0.52, 7.69)
                                      = 2.81 m from the pond centre, 0.52 m up in clear air
```

**Cause.** The sphere reaches `y = 2.00`, four times Greenie's own fire height. `SeedProjectile`
is a trigger with a Rigidbody and fizzles on contact with *anything* non-trigger
([`SeedProjectile.cs:59`](../Assets/Scripts/Items/SeedProjectile.cs#L59)):

```csharp
if (!other.isTrigger) { Vfx.Impact(...); Destroy(gameObject); }
```

The blocker is non-trigger and on `Obstacle`. `Seed.prefab` is on **`PlayerProjectile`**
(layer 10), and `PlayerProjectile ↔ Obstacle` is one of only two pairings that layer has
switched **on** — deliberately, so that scenery stops shots. So the seed is destroyed exactly
as if it had hit a rock, fizzle VFX and all, in the middle of the air. Nothing about the layer
setup is wrong here; the collider was.

> **Correction (re-verification, 2026-08-26).** This paragraph originally read *"of the six
> gameplay layers checked … **every one collides with every other**, and there is **no
> `Projectile` layer in the project at all** — `Projectile` exists only as a tag."* Both
> halves are false, and the corrected text above replaces them. `TagManager.asset` defines
> **`PlayerProjectile` (10)** and **`EnemyProjectile` (11)**; `Seed.prefab` is on layer 10; and
> `DynamicsManager.asset` carries a hand-configured matrix, not an all-on one — decoded, layer
> 10 collides with **`Enemy` and `Obstacle` only**, and not with `Default`, `Water`, `Player`,
> `Ground` or `Trigger`. The finding itself is unaffected: the old blocker really was a
> non-trigger `Obstacle` collider reaching `y = 2.00` (confirmed against
> `git show 2f51336:Assets/Editor/TerrainKit.cs`), so it really did eat seeds, and the fix
> below is the right one. Only the root-cause narrative was wrong.

- **Cheapest fix:** the same `MeshCollider` swap as C1 — a 0.45 m-tall disc tops out at
  `y = 0.45`, below the seed's 0.60 m flight line, so seeds sail over the pond and Greenie
  still can't walk into it. One change closes both findings.
- **Not the backlog item this originally proposed.** The first draft asked for *"add a
  `Projectile` layer and turn off `Projectile ↔ Obstacle` collision"* — but the layers already
  exist and that checkbox is already there and already **on**, on purpose. Whoever picks this
  up is making a one-checkbox design decision (*should scenery eat Seeds at all?*), not
  building new infrastructure. Still a Dev A call, since the collision matrix is a shared
  contract, but a much smaller one than it was written up as.

---

## C3 — The mesa's collider stands in 6.5 m² of open ground *(S3)*

Khiêm: *"the mountain has corners that I should expect to stand in between."* Confirmed, and
it is three of the four corners.

- **Steps:** 1. Go to the mesa in the north-west, around (−24.5, 17.5). 2. Walk into any
  corner except the south-east.
- **Expected:** walk up to the rock, and into the notches its ragged outline leaves.
- **Actual:** stopped by a rectangle of thin air well before the rock.

**Measured** — 1026 sample points at 0.25 m across the collider footprint:

```
mesa box                8.4 x 5.6 m,  (-28.70, 14.70) .. (-20.30, 20.30)
capsule blocked at      922 of 1026 sample points
   of those, no rock within 0.7 m:   104 points  =  6.5 m2 of invisible wall in open ground

phantom depth by corner    NE  49 spots, deepest 1.75 m
                           NW  40 spots, deepest 1.57 m
                           SW  15 spots, deepest 1.42 m
                           SE   0 spots            <- the only honest corner
```

1.75 m is **two and a half times Greenie's own width** — not a rounding error, a wall you can
see past.

**Cause.** `Mesa()` builds a *ragged radial mound*: each grid cell gets
`n = clamp(round(3.5 − r·3.1 + jitter), 0, 3)` storeys, and cells that roll `n = 0` are skipped
entirely — which is what gives the silhouette its nice broken edge. The collider is then one
`BoxCollider` sized to `footprint`, the bounds of every cell that *was* built. The comment at
[`TerrainKit.cs:289`](../Assets/Editor/TerrainKit.cs#L289) shows the intent was exactly right —
*"sized to the rock that actually got built… a full-grid box would stop the player short of
thin air"* — but `Bounds.Encapsulate` produces an **axis-aligned rectangle**, and the whole
point of the shape is that it is not a rectangle. The empty corner cells fall inside it.

- **Cheapest fix:** drop the single box and give **each built column its own collider** —
  `Stack()` already knows exactly where every cube went, so one `BoxCollider` per column
  (34 of them, static, batched) traces the real outline for free. Delete the
  `footprint` / `box` block in `Mesa()` and add the collider inside `Stack()`.
- Note `Stack()` is also used by `OuterHills` and `Surround`, which must stay collider-free —
  so gate it on a parameter rather than always adding one.

---

## C4 — Every object in the hub is walk-through *(S3)*

Khiêm reported *"small objects I can walk through"*. It is not just the small ones.

**The entire `Shop_RecyclingStation` scene contains exactly five solid colliders: the floor
and the four walls.** Everything else is a ghost.

```
Environment/Floor        x1        <- these five are the whole of the hub's physics
Environment/Wall_North   x1
Environment/Wall_South   x1
Environment/Wall_East    x1
Environment/Wall_West    x1

Environment/Yard:  25 props, 27 renderers, 0 colliders
```

Walk-through, by height:

| Prop | Height | | Prop | Height |
|---|---|---|---|---|
| `tree_oak` ×2 | 3.40 / 3.00 m | | `pipe-large-long` ×2 | 1.00 m |
| `lantern` ×3 | 2.60 m | | `barrel`, `barrel-open` | 0.95 m |
| `stall-red` | 2.20 m | | `cart` | 0.90 m |
| `screen-flat` | 1.20 m | | `box` ×2, `plant_bushLarge` | 0.85 m |
| `box-large` ×2 | 1.10 m | | `resource-planks` | 0.55 m |

And the three **interactables** are trigger-only, so you can stand inside them:

```
MrBear             1 collider  — SphereCollider(trigger)
RecyclingCounter   1 collider  — SphereCollider(trigger)
Hub_CraftingBench  1 collider  — SphereCollider(trigger)
```

`c2_05_hub_inside_bear` is the clearest shot in this report: Greenie is at Ông Bear's exact
coordinates and is **completely invisible**, swallowed by the shopkeeper, with the "Nhấn E"
prompt floating over the pair of them.

- **Cause.** `Yard()` builds every prop as a bare holder + `ArtKit.Spawn(...)`, and
  `ArtKit.Spawn` attaches a *visual* — it has never added a collider. Level 1's props look
  solid because they are greybox **prefabs** that carry their own colliders; the hub yard was
  written as direct spawns and inherited none. The builder's own log line has been saying so
  all along: `"yard: 25 dressing props (no colliders)"`.
- **Cheapest fix:** in `HubBuilder.Yard()`, after each successful `Spawn`, add a `BoxCollider`
  fitted to the renderer bounds. Skip the flowers and `grass_large` (walking through a tuft of
  grass is correct); keep everything ≥ 0.5 m. For the three interactables, add a solid
  collider *alongside* the existing trigger — the trigger radius is the gameplay contract and
  must not change (CLAUDE.md rule 2).
- **Check the NavMesh after**: the hub has no enemies today, so nothing paths there, but the
  portals do.

---

## C5 — Reclamation repaints whole 4 m tiles *(S3)*

**New.** The valley's payoff beat — barren soil turning green — renders as **hard-edged
squares**. `c2_03_reclamation_tiles` shows it plainly: a Tetris shape of green rectangles with
90° corners in the middle of the farm.

**Measured:**

```
ground: 192 floor tiles, each 4.00 x 4.00 m

patch at (7.5, -6.5)  r=3.50 : disc covers 38.5 m2, repaints 6 whole tiles =  96.0 m2  (2.5x)
patch at (8.5,  4.5)  r=3.50 : disc covers 38.5 m2, repaints 8 whole tiles = 128.0 m2  (3.3x)
patch at (0.0,  7.0)  r=3.50 : disc covers 38.5 m2, repaints 6 whole tiles =  96.0 m2  (2.5x)

currently lush: 27 of 192 tiles = 432 m2 of green, from four discs totalling ~154 m2
```

- **Cause.** `TintWithin` does `Physics.OverlapSphere` and then re-tints **the whole
  renderer** of every ground collider it touches
  ([`ReclamationPatch.cs:109`](../Assets/Scripts/World/ReclamationPatch.cs#L109)). The floor's
  unit of granularity is a 4 m tile, so a 3.5 m circle turns up to eight 16 m² squares solid
  green. The wave's smooth `SmoothStep` growth is real and correct — it just quantises to a
  4 m grid on the way out.
- **Cheapest fix, and the one I'd take before submission:** set `tintSurroundings = false` and
  let the **decal disc alone** carry the effect — it is already a circle, already animates
  outward, and already lands in the right place. Raise `radius` a little to compensate. One
  serialized field, no new geometry, and the beat reads as a bloom instead of a spreadsheet.
- **Proper fix (post-submission):** the ground would have to stop being 192 flat tiles —
  either a single mesh with vertex colours, or a projected decal. That is the same structural
  change [R1](#r1--undulating-ground-change-request-not-a-defect) needs, so the two should be
  costed together if either is taken.

---

## C6 — The three earth tones read as a checkerboard *(S4)*

**New.** `BuildGround()` gives each of the 192 tiles one of three browns, and the comment at
[`Level1Builder.cs:83`](../Assets/Editor/Level1Builder.cs#L83) states the goal precisely:
*"a few percent of variation per tile breaks that up **without ever looking like a
checkerboard**."* It does look like a checkerboard. Visible across the whole barren area in
`c2_01`, `c2_02` and `c2_03`.

```
FarmGround    (0.420, 0.380, 0.260)
FarmGround_B  (0.440, 0.395, 0.275)
FarmGround_C  (0.405, 0.365, 0.250)
largest channel difference: 0.035  =  8.6% of base    <- "a few percent" is 8.6%
```

Two things stack: the delta is ~3× what the comment intends, and the tiles are **4 m squares
assigned at random**, so two adjacent tiles can differ by the full 8.6% along a perfectly
straight 4 m seam. Natural ground has no straight seams.

- **Cheapest fix:** cut the spread to ~2–3% (e.g. ±0.008 on each channel), **and** pick the
  material from smooth noise over `(i, j)` rather than `soil.Next()`, so neighbours land in
  the same band and the grid stops being legible. Both are inside `BuildGround()`.

---

## C7 — Level 1 walk-through props: three lanterns and the canoe *(S4)*

**New**, and the same root cause as [C4](#c4--every-object-in-the-hub-is-walk-through-s3):
props spawned through `ArtKit.Spawn` directly rather than as greybox prefabs get no collider.
Level 1 is *mostly* fine because its props are prefabs — only the pieces `TerrainKit` places
by hand are ghosts:

```
Props/Village/Lantern/Art_lantern            h=2.60   at (1.0, 22.0)
Props/Village/Lantern/Art_lantern            h=2.60   at (-2.6, 18.2)
Props/Village/Lantern/Art_lantern            h=2.60   at (4.6, 18.2)
Environment/Terrain/Spring/Canoe/Art_canoe   h=0.55   at (-22.6, 7.4)
```

A 2.6 m lantern post you walk through is the same tell as the hub's. The canoe is worse in one
way — it is beached half on the bank and half over the water, so it reads as a solid object
sitting in the one place the player is *also* blocked by an invisible wall ([C1](#c1--an-invisible-wall-stops-greenie-045-m-short-of-the-spring-s3)).

**Not defects, checked and cleared:** the `TeleportGate` ring (3.00 m) and its panel, and both
hub portal doorways — you are *meant* to walk into those. The four `LoreNote` signs (0.85 m)
are walk-through too; that is arguable either way and I have left it to the PO.

- **Fix:** same as C4 — a fitted `BoxCollider` after `Spawn` in `TerrainKit.Village()` and
  `Pond()`.

## C8 — The mesa reads as a stacked layer-cake *(S4)*

**New, aesthetic.** Visible in `c2_02_mesa_corner_wall`. `Stack()` deliberately scales its
cliff cubes **uniformly** rather than stretching them — that decision is right, and it is why
the grass caps stay in proportion (the docstring records that the first attempt looked like a
chocolate cake). But the result still reads as stacked boxes, because every column is the same
1.4 m cell, every tier steps by exactly one cube, and the cubes are axis-aligned.

Cheap improvements, in order of effort: jitter each column's horizontal position by ±0.2 m so
the tiers stop lining up; vary `cell` per column by ±15%; skirt the base with `stone_smallA` /
`plant_bushSmall` (the pond bank already does this and looks better for it). None of this is
worth doing before the A2 build — logged so it is not re-discovered.

---

## R1 — Undulating ground *(change request, not a defect)*

Khiêm: *"the ground in Level 1 should be ups and downs, like slightly tilted in many ways,
making the robot able to go up and down in height."*

> **Resolved 2026-08-31 — taken as cycle-3 [B8](../PRODUCT-BACKLOG.md#b8--hill-like-ground-tilts-rises-and-dips-instead-of-a-flat-plane).**
> Level 1's floor now spans **2.20 m at up to 24.7°**. The costing below held up almost exactly,
> including the part everyone else got wrong: *"Greenie would walk up and down slopes today,
> with no code change"* was correct, and `PlayerController` was not edited. The combat break
> named as item 1 was answered by making Seeds hold their **clearance above the ground** rather
> than a world-Y line. See
> [architecture.md § The ground is a function](../.claude/docs/architecture.md#the-ground-is-a-function-b8).

**This is not a bug — it asks to change golden rule #1**, which says the ground is the flat XZ
plane and gravity is never a mechanic. So it is a PO decision, not something QA can just file
and someone quietly fixes. Here is what it would actually cost, because the answer is less
obvious than it looks.

**Three things already support it, for free:**

- `PlayerController` already applies a constant `Vector3.down * 9.81` ground-stick every frame,
  and Greenie's `CharacterController` is already configured for terrain: `slopeLimit = 45°`,
  `stepOffset = 0.3`. **Greenie would walk up and down slopes today, with no code change.**
- `PlasticSlime` is a `NavMeshAgent`, and a baked NavMesh follows slopes automatically.
- The mesa already proves elevation reads well at this camera angle.

**Five things break, and the first one is serious:**

1. **Combat breaks.** `SeedProjectile.Launch` zeroes the Y of its direction and sets
   `useGravity = false`; seeds fly dead flat at 0.60 m. On a rising slope a seed ploughs into
   the hillside a metre or two out; on a falling one it sails over the target's head.
   `EnemyProjectile` mirrors it. This is the core loop, and CLAUDE.md rule 1 already warns
   about exactly this class of problem ("height is presentation, hitting things is XZ").
   Any real slope needs the projectiles to become terrain-aware first.
2. **The floor is 192 separate 4 m tiles**, each its own prefab instance with its own collider
   and renderer. Tilting them individually leaves a step at every seam; making one heightmapped
   mesh instead **breaks `ReclamationPatch`**, whose whole mechanism is one renderer per tile
   found via `OverlapSphere` and filtered on `layer == Ground`. C5 wants that same rewrite, so
   the two should be costed together.
3. **Everything is authored at `y = 0`** — every prop, fence post, chest, herb, NPC, enemy
   spawn and the whole village, across four generator files. Each would need a
   raycast-to-ground pass or it floats/sinks.
4. **The camera would bob.** `CameraFollow` sets `PositionDamping` uniformly on all three axes
   (0.15 s), so Y is tracked as tightly as X and Z and every bump rocks the whole frame.
5. **`GroundCleanser` and the Độ Sạch metric** work off the same flat-tile assumption.

**Recommendation:** do not take this before the A2 submission. It touches the generators, the
projectiles, the reclamation system and the camera, and it puts the one thing that currently
works end to end — combat — at risk. If the goal is *"the valley shouldn't look like a flat
plane"*, C6 (kill the checkerboard) and C8 (break up the mesa) buy most of that appearance for
a fraction of the risk, and C1's wade volume gives the pond the "sinking" feel specifically.
If the PO does want true terrain, it deserves its own cycle and its own backlog entry, starting
with the projectiles.

---

## Not covered by this pass

- Every keyboard control — unchanged gap from 2026-08-18
- A full end-to-end playthrough; wall-clock timing / the ~31-minute budget
- Any aspect ratio but the editor Game view; a real build
- `Level2_FactoryMaze` beyond confirming the underlay is present
- Save/load across an application quit
- The three missing NPCs (D3) — already logged, not re-checked
- Whether the hub's five-collider state affects the NavMesh (no enemies there today)

---

## Fix log — 2026-08-26

Applied in commit **`e3d42ac`** and re-verified the same day. Each row says how it was
measured, so T knows what is *not* covered. The same limitations as the pass itself still
apply: **no key binding was exercised, one aspect ratio (1100×533), no build, no timing**, and
the Play sessions inherited Khiêm's save.

| # | Change | Verified by |
|---|---|---|
| C1 | The sunk `SphereCollider` blocker is **deleted**. The pool is now a trigger — `Wade` (`SphereCollider` r 3.40 at `center.y = 0.4`, `isTrigger`) driving the new `WaterWade.cs`, which borrows `PlayerController.EnterMud()` / `ExitMud()` and eases `PlayerAnimator.SinkOffset` down 0.15 m. Slimes are kept out by a `NavMeshModifierVolume` instead of by physics, so the water stops what walks without standing in the way of what flies | walked Greenie's real capsule (r 0.35, spheres at y 0.375 / 0.825) in at 1 cm steps from south and east — **reaches the pond centre**, no stop. The only colliders under `Spring` are `Wade` (trigger) and the canoe |
| C2 | Same change — there is nothing solid over the water any more | 8 raycasts at exactly **y = 0.60 m**, straight across the pool on 8 headings: **0 solid hits**. The seed flight line is clear |
| C3 | The single `BoxCollider` in `Mesa()` is gone; `Stack()` now emits **one `BoxCollider` per built column**, grown down to `y = 0`, behind a `solid:` parameter so `OuterHills` and `Surround` stay collider-free | **19** column colliders trace the real outline. 1302 sample points at 0.25 m across the footprint: 765 blocked, and **0** of them with no rock within 0.7 m — was 104 points / **6.5 m²**, deepest 1.75 m |
| C4 | New `ArtKit.Solidify` fits a `BoxCollider` to the art's real bounds **on the holder**, so the next art pass can swap the model without touching physics. `HubBuilder.Yard()` calls it for every prop (flowers and grass fall under the 0.5 m minimum on their own); `SolidifyInteractables()` adds a solid box **alongside** the existing trigger | hub is **35 colliders, 30 solid** (was 5). `Yard`: 27 renderers → **21** colliders. `MrBear` / `RecyclingCounter` / `Hub_CraftingBench` each carry `SphereCollider(trigger)` + `BoxCollider(SOLID)`, **trigger radii unchanged** at 1.1 / 1.0 / 1.0 (CLAUDE.md rule 2). `StagePortal` correctly left trigger-only — you are meant to walk into it |
| C5 | `tintSurroundings` now defaults to `false` and is serialised `0` on `ReclamationPatch.prefab`; `radius` raised **3.5 → 4.5** so the decal disc alone carries the beat | all **4** patches in Level 1 read `radius 4.50, tintSurroundings False`, and there is **no scene override** of either field anywhere in `Assets/_Scenes` |
| C6 | Spread cut to **±0.006** per channel, and the material is picked from `Mathf.PerlinNoise` over `(i, j)` (~7 tiles per period) instead of `soil.Next()`, so neighbours land in the same band | largest channel spread **0.035 → 0.012** (8.6% → **2.9%** of base). Tone changes at **102 of 356** tile seams = **29%**, down from a random ≈⅔ |
| C7 | `ArtKit.Solidify` after `Spawn` in `TerrainKit.Village()` (lanterns, XZ half-extent clamped to 0.3 m) and `Pond()` (canoe, turned by its *holder* so the box is oriented rather than its bounding rectangle) | 3 lanterns + the canoe all report **solid `BoxCollider`** in the live scene. The canoe's box tops out at **y = 0.55**, under the 0.60 m seed line — so making it solid did **not** re-create C2 |

**Rebuilds.** *Rebuild Level 1* and *Rebuild the hub* were run to carry C1–C7 into
`Level1_BarrenFarm.unity` and `Shop_RecyclingStation.unity`. **Level 2 was not rebuilt and did
not need to be** — the `ArtKit` diff in `e3d42ac` is purely additive (the new `Solidify`);
`ArtKit.Fit` is untouched, so Level 2's art placement is unaffected.

**Not changed, on purpose:**

- **C8** (the mesa's layer-cake silhouette) — cosmetic, not worth spending before the A2 build.
  Confirmed still open: `Stack()` uses a fixed 1.4 m cell with no per-column XZ jitter.
- **R1** (undulating ground) — was a PO call and was taken: **done 2026-08-31 as cycle-3 B8**.
  The Level 1 floor is 192 generated meshes over a shared height field now, spanning 2.20 m at
  up to 24.7°, and golden rule #1 was rewritten to match. *(Stated as of this pass: it was still
  one flat plane of 192 tiles.)*
- **The four `LoreNote` signs** (0.85 m) are still walk-through — left to the PO, as originally
  written up in [C7](#c7--level-1-walk-through-props-three-lanterns-and-the-canoe-s4).
- Everything under [Not covered by this pass](#not-covered-by-this-pass) is still not covered.

**One incidental, not a defect and not a tracked finding.** On a late-game save the hub
objective panel renders `[x] Tìm thảo dược (0/3)` — a struck-through *done* row carrying a 0/3
counter. `done` is set from `HasAntidote` / `Stage == TiSaved` while the label prints live
`QuestProgress.HerbCount`, which is 0 once the herbs have been handed in
([`ObjectiveTracker.cs:161`](../Assets/Scripts/UI/ObjectiveTracker.cs#L161)). Cosmetic, and
only visible after the antidote hand-in. Logged so it is not re-discovered.

**Console:** zero errors or exceptions across the re-verification sessions; the only warning is
the pre-existing, unrelated Coplay editor-toolbar one. **`check_compile_errors`:** clean.
**Nothing in the project was changed by the re-verification** — `git status` clean afterwards.
