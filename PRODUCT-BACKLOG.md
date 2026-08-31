# Product Backlog — Eco-Dash 3D

> Owned by **Thanh Tùng & Đức Anh** (Product Owner, [CYCLE-2-TASKS.md § 3](CYCLE-2-TASKS.md)).
>
> **Status: B1–B5 are built.** Everything in [Backlog items](#backlog-items) has been
> implemented and play-mode verified — see [Delivered](#delivered) for what changed and
> what is still open. The findings are kept as written so the reasoning survives.
>
> **B6 and B7 are built** (2026-08-31) — see [Cycle 3 draft](#cycle-3-draft--the-movement--perspective-slice-b6b9),
> [Delivered — B6](#delivered--b6-the-perspective-toggle) and
> [Delivered — B7](#delivered--b7-the-horizon). Khiêm took the slice and chose to start with B6,
> so **[golden rule #1](CLAUDE.md) has been rewritten** and the change is announced here for the
> other two devs: the world is still one flat XZ plane with no jumping and no gravity, but the ¾
> camera is now the *default* framing rather than the only one, and **movement and aim are
> camera-relative from now on** — go through `PerspectiveMode.MoveFrame`, never raw world axes.
>
> **B8 is built** (2026-08-31) — see [Delivered — B8](#delivered--b8-the-ground). **Golden
> rule #1 has been rewritten again** and the change is announced here: Level 1's ground now
> rises and falls (2.20 m of range, peaking at 24.7°), so *"Greenie walks on flat ground"* is
> gone and *"Seeds fly flat at y ≈ 0.6 in world axes"* becomes *flat over the ground* — a
> constant clearance above whatever is beneath them. **Ask `GroundHeight` for the ground; never
> raycast for it and never assume y = 0.** It is null in every flat scene, so Level 2, the hub
> and the story scenes are unchanged.
>
> **B9 is built too** (2026-08-31) — see [Delivered — B9](#delivered--b9-the-wall). Greenie
> ant-walks the Level 1 mesa. **It did not make gravity a mechanic**: there is still nothing to
> jump and nothing to fall off, and a climb is walking on a wall — he hangs there indefinitely
> with no key held. **Up is now `SurfaceFrame`'s to answer**, the way height is `GroundHeight`'s;
> it is the identity everywhere he is not on a wall, so the other scenes are untouched. Only a
> collider carrying `Climbable` may be climbed, and the mesa's 18 columns are the only ones.
>
> **This backlog's ordering note was wrong about both of them.** It said B8 and B9 need the same
> `Rigidbody` rewrite. B8 needed no controller change, exactly as
> [QA R1](QA/exploratory-pass-2026-08-26.md#r1--undulating-ground-change-request-not-a-defect)
> predicted — and **B9 did not either**. Both estimates reasoned from what a `CharacterController`
> *is* (a capsule that cannot tilt, which is true) instead of from what `Move` *takes* (any
> world-space vector, gravity not included).
>
> **Cycle 3 is complete: B6, B7, B8, B9.**
>
> **PO decision, 2026-08-31: the game now opens in first person.** `P` goes *out* to the ¾
> camera rather than into first person. One constant (`PerspectiveMode.Default`) — see
> [architecture.md](.claude/docs/architecture.md#which-framing-the-game-opens-in-changed-2026-08-31).
> The ¾ camera stays **canonical**: every layout, sightline and QA pass is tuned at it. What the
> flip moves onto the default path is B9's first-person caveats — on a wall the camera rolls, and
> a 4.2 m rock mostly shows sky — for a player who never presses anything.
>
> **Scope of this draft.** This covers one slice only: environment feel — terrain
> elevation, map scale, camera angle, and "does this look cheap" — gathered by
> playtesting the current build and cross-checking it against the **A2 course
> rubric** (terrain with elevation, trees/grass/sky, village/house decoration,
> a controllable 3D character) plus a full read of `Assets/Editor/ArtPass.cs`,
> the level builders, and the Cinemachine rig. It does **not** cover P1's full
> verdict, P2's pillar scoring, **P3 (shard economy cost)**, **P4 (the three
> missing NPCs)**, or P5 (demo script) — those need Tùng and Đức Anh's own
> judgment call, not an environment audit. No ship-line is drawn here for the
> same reason: line-drawing is the PO's call, not a finding.

---

## A2 rubric check — where the environment actually stands

| A2 asks for | Status | Evidence |
|---|---|---|
| Trees | ~~**Present but all dead.**~~ **Fixed (B1).** 30 `Greybox_DeadTree` (`tree_thin_dark.fbx`) in Level 1, 8 more ringing the Slime King's grove, and zero living trees — despite ~60 living variants sitting imported and unused. Living oaks and pines now stand round the village and on the mesa, and ~34 more on the hills outside. |
| Grass | ~~**Present but thin.**~~ **Fixed (B3).** The 8-model scatter palette had only 3 grass-like entries and drew ~300 of them over 3185 m². Now 12 entries weighted green, ~390 placed — and, far more importantly, the grass was rendering *cyan* (see defect 1 below). |
| Sky | ~~**Present, stock only.**~~ **Fixed (B5).** Each scene now tunes the built-in procedural sky to match its own `SceneLook` mood. |
| Terrain elevation (mountains/hills/rivers/lakes) | ~~**Absent.**~~ **Fixed (B2).** Still no `UnityEngine.Terrain` and the play surface is still a flat slab — deliberately, per golden rule #1 — but the world now has a 4.2 m mesa and a spring inside the walls and three rings of hills plus a lake outside them. |
| Village / houses / structures | ~~**Minimal.**~~ **Fixed (B4).** The four huts are real cottages, with four more buildings, a fountain square, stalls, a cart and lanterns beside them; the hub has a dressed yard. |
| Controllable 3D character | **Solid, no notes.** `Player.prefab` (Greenie = Kenney's `oopi` robot), full WASD movement on the ground plane, working as intended. (One caveat added after building the rest — see the last bullet under [Delivered](#delivered).) |

**Bottom line:** the character and controls are done; the *environment* half of A2 is the weak half, and specifically the "elevation" and "living green" clauses — not because assets are missing, but because assets **already sitting in the repo, imported and unused**, were never wired into a builder pass. See the shopping list for exactly what's already there.

---

## Camera angle — confirmed working as designed, not a defect

Read directly from `Assets/Scripts/Systems/CameraFollow.cs:34` and the baked rotation in `CameraRig.prefab`:

- **Pitch 50°**, yaw locked at 0° (no player camera control — this is CLAUDE.md's golden rule #1).
- **12 m back**, **60° FOV**, perspective (not orthographic).
- Code comment: *"~50° is the Eco-Dash 3D house angle"* — a deliberate choice, landing between a classic isometric RPG camera (~30–35°) and a true top-down (80–90°), matching the stated *Tunic* / *Death's Door* reference.

So: this is not a bug, and I did not change it. If the playtest verdict is still "the angle feels off," that's a genuine design call for the PO, not a fix — three concrete options, so the decision isn't abstract:

| Option | Pitch | Trade-off |
|---|---|---|
| **Keep 50°** (current) | 50° | Matches the stated reference games; already tuned against both levels' sightlines (corridors in Level 2, open field in Level 1). |
| **Steeper, more top-down** | ~65–70° | Better sightlines around Level 2's factory walls and the sweeping lasers; Greenie and enemies read smaller, less character detail visible. |
| **Shallower, more isometric** | ~35–40° | More of Greenie's model (and the new art, if trees/cliffs go in) is visible; worse sightlines — Level 1's fence lines and Level 2's corridor walls would occlude more often. |

If the team wants to try an alternative, it's a one-line change (`CameraFollow.cs:34` + the baked prefab rotation) — cheap to prototype, but changing the house angle is exactly the kind of shared-contract change CLAUDE.md's golden rules reserve for one owner, so flag it with Dev A before touching it.

**One measurement the team should have before deciding** (worked out while building the terrain, and the single most decision-relevant number here). The camera sits 9.7 m up, and with a 60° FOV the top edge of the frame points 20° below horizontal — so the frustum's top plane meets the ground **26.6 m in front of the camera, i.e. ~19 m past Greenie.** Nothing beyond that is on screen at all, at any height. Concretely:

| Pitch | Camera height | Ground visible past Greenie |
|---|---|---|
| **50°** (current) | 9.7 m | **~19 m** |
| 40° | 8.2 m | ~39 m |
| 35° | 7.4 m | ~77 m |

That is why the hills now start 10 m beyond the boundary wall rather than further out: at 50° the player only sees them while hugging a wall. It also bears directly on the A2 video — the rubric wants the terrain and its elevation on screen, and at 50° a demo has to walk to the mesa to show any.

> **Decided (2026-08-20): keep 50°.** The house angle stands — it is the stated *Tunic* / *Death's Door* framing, it is already tuned against Level 2's corridors, and cycle 1's QA pass was run at it. **The A2 demo route absorbs the cost instead:** the video should walk Greenie past the village square and up to the highland mesa and its spring, which puts the houses, the living trees, the elevation and the water on screen in sequence. That is a 30–60 s route on its own, which is exactly the length the rubric asks for.

---

## Backlog items

Format per [CYCLE-2-TASKS.md § 7](CYCLE-2-TASKS.md): *"As a player, I want… so that…"*, with an acceptance criterion. Ordered roughly by payoff-per-hour — the first three need **zero new asset sourcing**, only new `ArtPass.cs` / level-builder rows against models the project already owns (see [ASSET-SHOPPING-LIST.md](ASSET-SHOPPING-LIST.md) for exactly which files).

### B1 — Living trees instead of only dead ones
**As a player**, I want Level 1 to have some green, living trees, **so that** the "reclaiming a polluted valley" story reads visually — right now every tree is already dead, so there's no before/after to see.
- **Acceptance:** at least the Slime King's grove (the "reclaimed" area) uses living tree variants (`tree_oak`/`tree_default`/`tree_pineRoundA-F`) instead of `tree_thin_dark`; dead trees can stay elsewhere as the "still polluted" read.
- **Cost:** zero new assets — swap/add rows in `ArtPass.cs`'s tree entries. Small.

### B2 — Terrain elevation on at least one level
**As a player**, I want the ground to not be perfectly flat everywhere, **so that** the world reads as terrain and not as a game-board.
- **Acceptance:** Level 1 (65×49 m, room to work with) gets at least one non-flat feature — a low cliff edge, a dry creek bed, or a small pond — built from the already-imported `cliff_*` / `ground_river*` modular kit.
- **Cost:** zero new assets, but real new work — this is stacked modular pieces on the existing flat plane (not a heightmap terrain), so it needs a new pass in `Level1Builder.cs`/`ArtPass.cs`, not just a config row. Medium — this is the one item on this list worth actually scoping with a day estimate before committing.

### B3 — Ground reads as ground, not as a painted plane
**As a player**, I want the dirt/grass under my feet to look like dirt/grass, **so that** the world doesn't read as a solid-color placeholder.
- **Acceptance:** `Greybox_Floor` tiles in Level 1 use the already-imported `ground_grass.fbx` tile mesh (or a denser prop scatter) instead of a flat untextured material, **without** breaking `ReclamationPatch`'s per-tile clean/dirty re-tint (it needs one mesh instance per tile, same as today).
- **Cost:** zero new assets. **Do not chase a downloaded ground texture for this** — see the shopping list for why; Kenney's Nature Kit ships no textures at all, so a texture pack would be an external, style-mismatched addition for a problem the pack's own included geometry already solves for free.

### B4 — A real village cluster in the hub or Level 1
**As a player**, I want the hub (or Level 1's village edge) to look like a place people used to live, **so that** "rescue the villagers" has a home for them to return to — the design doc already promises this (`game-design.md § 4.7.6`: freed villagers relocate to the hub, "a Stardew community-center feel") and it isn't built.
- **Acceptance:** at least 2–3 more structures beyond the current 4 huts, built from Survival Kit's unused modular pieces (`structure.fbx`, `structure-floor.fbx`, `structure-metal-wall.fbx`, `structure-metal-doorway.fbx` — currently only `structure-roof.fbx` is used).
- **Cost:** zero new assets. Note this item **overlaps D3/D7** in [CYCLE-2-TASKS.md § 5](CYCLE-2-TASKS.md) (the three missing NPCs, and the hub's un-built villager-relocation feature) — worth deciding B4 and P4 together rather than separately, since placing the structures and placing the NPCs are the same conversation.

### B5 — A skybox that isn't the Unity default
**As a player**, I want the sky to look intentional, **so that** the two levels don't end at "a wall with nothing beyond it" (already flagged in `CYCLE-2-TASKS.md § 6`'s shopping list).
- **Acceptance:** each scene has a tuned sky (procedural sky settings, tinted per `SceneLook.cs`'s existing warm-farm/cold-factory/bright-hub split) — a custom skybox asset only if tuning the built-in procedural sky isn't enough.
- **Cost:** likely zero new assets (Unity's procedural sky is tunable via `RenderSettings`/Lighting, no download needed) — try that first, fall back to sourcing only if it's not enough. Small.

---

## Cycle 3 draft — the movement & perspective slice (B6–B9)

> **Requested by Khiêm, 2026-08-26.** Four items, written up here with acceptance criteria and
> real costs. **Read this paragraph before scheduling any of them.**
>
> **Three of the four break [golden rule #1](CLAUDE.md).** The rule reads: *"3D top-down on the
> XZ plane, no platforming. WASD moves Greenie on flat ground; input Y maps to world Z. Fixed ¾
> Cinemachine camera (no player camera control), no jumping, gravity is never a mechanic. Never
> add side-scroller or first-person logic."* B6 adds first-person logic and player camera
> control; B8 makes the ground non-flat; B9 makes gravity and surface-climbing a mechanic. That
> does **not** mean "no" — it means these are **PO decisions to change the project's founding
> constraint**, not tickets a dev can quietly pick up, and CLAUDE.md's rule 7 reserves
> shared-contract changes like this for Dev A. If they are taken, golden rule #1 has to be
> **rewritten first** and the change announced to the other two devs, or three people will be
> building against three different contracts.
>
> **The load-bearing risk, stated once:** the one thing that currently works end to end is
> **combat**, and all three of B6/B8/B9 sit on top of it. `SeedProjectile.Launch` zeroes the Y
> of its direction and the prefab ships `m_UseGravity: 0`
> ([`SeedProjectile.cs:36`](Assets/Scripts/Items/SeedProjectile.cs#L36)) — seeds fly dead flat
> at y = 0.60 m, in **world** axes. Tilt the ground, turn the camera, or put Greenie on a wall
> and that projectile is aiming at nothing. **Whichever of these is taken first, the
> projectiles become terrain- and orientation-aware before anything else does** — that is the
> gate, and it is shared by all three.
>
> **B7 is the exception:** it is a straight improvement, breaks no rule, and is worth doing on
> its own merits whether or not B6 ships.

### B6 — Toggle between the ¾ view and first person with **P** — ✅ **built 2026-08-31**

**As a player**, I want to press **P** to switch between the current ¾ top-down camera and a
first-person view from Greenie's own eyes, **so that** I can look around the valley I am
cleaning up instead of only ever seeing it from above.

- **Acceptance:**
  - **P** toggles the two framings and toggles back; the key is polled the way every other
    control is — `kb.pKey.wasPressedThisFrame` straight off `Keyboard.current`, **not** through
    the action asset (CLAUDE.md's controls contract). **`P` is currently unbound** — verified
    against every key the project polls (`W/A/S/D`, `J`, `E`, `I`, `Tab`, `Q`, `C`, `H`, `Esc`,
    `1–4`, `Space`, `Enter`, `←`/`→`) — so there is no conflict to resolve.
  - Greenie's own model does not fill the first-person frame.
  - The toggle survives a scene change and cannot be triggered while a modal (dialogue, pause,
    bag, tutorial) owns the screen.
  - Both framings are playable: a player who toggles mid-fight is not killed by the switch.
- **Cost: Large, and it is not the camera that makes it large.** Honestly scoped:
  - **The camera half is genuinely small.** `CameraFollow` already authors framing from
    serialized `pitch` / `yaw` / `distance` / `targetHeightOffset`
    ([`CameraFollow.cs:34`](Assets/Scripts/Systems/CameraFollow.cs#L34)) and pushes them into
    `CinemachineFollow.FollowOffset`. A second `CinemachineCamera` at `distance ≈ 0`, eye
    height, priority-swapped on toggle, is the clean Cinemachine way to do it and gets a free
    blend between the two.
  - **The movement half is the real work.** Movement is authored in **world axes** — `wKey`
    adds to `dir.z` and `Move()` does `body.Move(horizontal + Vector3.down * groundStickSpeed)`
    ([`PlayerController.cs:75`](Assets/Scripts/Player/PlayerController.cs#L75)) — and the yaw
    is locked at 0 precisely *"so W always moves 'up' the screen."* The moment the camera can
    yaw, W must become **camera-relative** or the controls invert whenever the player turns
    around. That is a change to the movement contract, and `FacingDirection` (which drives the
    animator and the shooting direction) goes with it.
  - **Three traps this codebase specifically sets**, all documented in CLAUDE.md and all live
    here: (a) **do not hide Greenie by deactivating the `Visual` node** — `PlayerAnimator`
    caches `baseLocalPos`/`baseScale` in `Awake` and writes `visual.localPosition` every frame;
    disable the **renderers** instead. (b) `CameraFollow` is `[ExecuteAlways]` and claims
    `Instance` in `OnEnable` *because* Fast Enter Play Mode never re-runs its `Awake` — a
    second camera object must follow the same pattern or `GameFeel.Shake` silently dies again
    (rule 5). (c) `Time.timeScale` already has six owners; the toggle must not become a
    seventh (rule 4).
  - **What it invalidates:** every framing decision in this document. The "~19 m of ground
    visible past Greenie" measurement, the A2 demo route, cycle-1 and cycle-2 QA (both run at
    50°), and Level 2's corridor sightlines were all tuned for a fixed 50° yaw-0 camera.
    First person also exposes **B7** immediately — see below.

### B7 — A sky and a horizon that survive being looked at — ✅ **built 2026-08-31**

**As a player**, I want the sky to look like a real sky when I can actually see it, **so that**
the world does not visibly end at the top of the boundary wall.

- **Why this is a separate item.** B5 shipped a tuned procedural sky per scene, and it is
  correct — but at pitch 50° you never see the horizon, so today the sky is a gradient smear
  along the top of frame. **The moment B6 lands, the sky becomes a major surface** and every
  shortcut behind it is visible. This item is what makes B6 not look broken.
- **Acceptance:**
  - From eye height, in both levels, looking at the horizon in any of the four cardinal
    directions shows a deliberate horizon — not a hard edge where the world stops.
  - Level 1's boundary reads as *distance* (the B2 hills, the lake and fog doing the work), not
    as a 3 m wall with sky above it.
  - `Level2_FactoryMaze` gets an answer for "what is above a factory maze?" — it is an
    open-topped box today, and B2's outer hills do not exist there. A ceiling, a roofline, or a
    smog dome; a bare procedural sky over an indoor level will read as a bug.
  - Fog / atmosphere is tuned per scene alongside `SceneLook`, so the cut-off distance is a
    choice rather than an accident.
- **Cost: Medium, and mostly Level 2.** Level 1 is close — B2 already put three rings of hills
  and a lake outside every wall. Level 2 has nothing above the walls at all. **Zero new assets
  expected**; this is `RenderSettings` / `SceneLook.cs` tuning plus one backdrop pass in
  `Level2Builder`. **Worth doing on its own** even if B6 is deferred: it costs little and it
  helps the A2 video, which wants sky in shot.

### B8 — Hill-like ground: tilts, rises and dips instead of a flat plane — ✅ **built 2026-08-31**

**As a player**, I want Level 1's ground to rise and fall — small hills and shallow dips —
**so that** the valley reads as terrain rather than as a game-board.

- **This is [QA R1](QA/exploratory-pass-2026-08-26.md#r1--undulating-ground-change-request-not-a-defect), promoted from a QA change-request to a backlog item.** QA's costing stands; read it
  before scoping. Two things have changed since it was written, one cheaper and one not:
  - **Cheaper now.** R1 listed `ReclamationPatch` as a blocker because its whole mechanism was
    one renderer per 4 m tile found by `OverlapSphere`. **C5's fix removed that dependency** —
    `tintSurroundings` is now `false` and the reclamation beat is carried by the decal disc
    alone, which does not care what shape the ground is.
  - **Still a blocker.** `GroundCleanser.TintGround` **still** does exactly that
    (`Physics.OverlapSphere` → per-renderer tint, filtered on `layer == Ground`,
    [`GroundCleanser.cs:148`](Assets/Scripts/World/GroundCleanser.cs#L148)), and it is what
    drives the codex's Độ Sạch metric. So the "192 separate flat tiles" assumption is now held
    by **one** system instead of two — the rewrite got smaller, not optional.
- **Acceptance:**
  - Level 1's play surface has real elevation change that Greenie walks up and down, with no
    step or seam at tile boundaries.
  - **Combat still works on a slope** — this is the acceptance criterion that matters. A seed
    fired uphill hits an enemy uphill; one fired downhill does not sail over its head.
  - Everything authored at `y = 0` sits on the new ground: props, the 112 fence posts, chests,
    herbs, NPCs, enemy spawns and the whole village, across four generator files.
  - `GroundCleanser` / Độ Sạch still register, and the NavMesh re-bakes and still paths from
    both far corners.
  - The camera does not bob — `CameraFollow` damps Y as tightly as X and Z (0.15 s on all three
    axes), so every bump currently rocks the whole frame.
- **Cost: Large.** Four generator files, the projectiles, `GroundCleanser`, and the camera.
  **Do not start here** — start with the projectile gate named at the top of this section.

### B9 — Greenie climbs vertical walls like an ant — ✅ **built 2026-08-31**

**As a player**, I want Greenie to walk up vertical surfaces the way an ant does, **so that** I
can climb the mesa in Level 1's north-west corner instead of walking around it.

- **Acceptance:**
  - Greenie can leave the ground plane onto a vertical face, walk on it under his own
    orientation, and return to the ground without being launched or stuck.
  - The Level 1 mesa is climbable end to end. Note what it is made of now: **C3's fix replaced
    the single box with 19 per-column `BoxCollider`s** grown down to `y = 0`, so the mesa is a
    stepped stack of vertical faces — good news for climbing, but it means the climb surface is
    19 separate colliders with seams between them, and the transition *between columns* is the
    hard case, not the flat face.
  - The camera keeps Greenie in frame and readable while he is on a wall.
  - Combat, damage and pickups behave on a wall, or are explicitly disabled there by design.
- **Cost: Large, and it needs a different character controller.** `Player.prefab` uses a
  **`CharacterController`** (`radius 0.35, height 1.15, slopeLimit 45°, stepOffset 0.3`) and
  `PlayerController.Move` adds a constant `Vector3.down * groundStickSpeed` in **world** axes.
  A `CharacterController`'s capsule is permanently world-Y aligned and cannot be re-oriented —
  so ant-walking means **replacing it with a `Rigidbody` plus a surface-aligned controller**
  (raycast the surface, align `transform.up` to its normal, move in the tangent plane, apply
  "gravity" along −normal). That is a rewrite of the one script the whole game's feel rests on,
  and it lands on: `PlayerAnimator` (bob/squash assume world up), `PlayerHealth`'s knockback
  (`knockbackVelocity` is world-space), every hazard that calls `EnterMud`/`ExitMud`, the
  contact-damage geometry, and `CameraFollow`.
- **Ordering note:** B9 and B8 want the *same* rewrite — a controller that follows a surface
  normal handles both a hill and a wall, and doing them separately means doing it twice. If
  both are wanted, scope them as one piece of work. B9 combined with B6 also raises a design
  question worth answering before any code: **in first person, on a wall, which way is up?**
  - > **Resolved 2026-08-31.** The first half was wrong twice over: neither item needed that
    > rewrite, and they share no code — B8 is a height function, B9 is an orientation frame. The
    > design question was the useful half. **The answer shipped is "his up":** in first person on
    > a wall the camera rolls into the surface frame so the rock reads as the floor. It is
    > self-consistent and it is also, on a 4.2 m rock, a screen full of sky — see
    > [Delivered — B9](#delivered--b9-the-wall), which has the screenshot.

### Recommended order, if the PO takes this slice

| # | Item | Take it? |
|---|---|---|
| 1 | **B7** sky & horizon | ✅ **Built.** Breaks no rule, and it turned out to be worth more than "polish": the reason Level 1's boundary looked cheap was a two-colour seam nobody had spotted, and Level 2 had no answer above its walls at all. |
| 2 | *(gate)* projectiles become orientation- and terrain-aware | ✅ **Both halves done.** Orientation with B6 — a Seed follows `PerspectiveMode.AimForward` in either framing; terrain with B8 — a Seed holds its clearance above the ground for the whole flight. B9 needed no third rule: firing is switched off on a wall. |
| 3 | **B6** perspective toggle | ✅ **Built.** The camera was easy, as predicted; camera-relative movement was the work, and it came out as one rotation rather than a second control scheme. |
| 4 | **B8** hill ground | ✅ **Built.** And the premise of this row was wrong: B8 needed **no** controller change, so it was not the biggest item on the list — the work was the height function, the mask, the settle pass and the projectile clearance. |
| 5 | **B9** wall-walking | ✅ **Built.** And this row was wrong as well: B9 needed no `Rigidbody` rewrite either — `CharacterController.Move` takes a world-space delta, so a capsule pressed against a wall climbs it. The work was the frame, the permission and the lip/edge rules. |

**The order was taken out of sequence, deliberately — and it paid off.** Khiêm asked for the
slice starting with its first item, so B6 went first rather than B7. B6's own write-up predicted
that would "expose B7 immediately", and it did. What was not predicted is that having first
person available made B7 *diagnosable*: the horizon could be rendered from eye height and looked
at, which is how the two-colour seam and the flat-topped hills were found. Doing B7 first, blind
at pitch 50°, would have meant tuning a sky nobody could see.

**That was written of B8 and B9 together, and B8 has since been taken and shipped:**
*"B8/B9 are still not recommended before the A2 submission. Cycle 2 closed with the game
finishable end to end and QA clean. B6 was contained — it added a framing without touching the
ground, the projectiles' flight or the character controller — but B8 and B9 rewrite exactly
those, and the game's one fully-working system is combat."*

**Half of that held and half did not.** B8 did touch the ground and the projectiles' flight, and
the projectile work was indeed the load-bearing part — it is measured against a control run
below. But it did **not** touch the character controller, which is where the "rewrite" cost was
supposed to live.

**And B9 has since been taken as well, with the same result.** It touched `PlayerController` for
exactly one substitution — `Vector3.down` became `-SurfaceFrame.Up` — and did not touch the
hazards, the contact-damage geometry or `CameraFollow` at all, every one of which its own cost
estimate named. Combat is unchanged on the ground and explicitly switched off on a wall.
**The one part of the warning that held is worth keeping**: the shortcut that made this cheap is
a hitbox that no longer matches the silhouette while Greenie is on a rock face, and the day
something can shoot at him up there, that stops being free.

---

## Delivered

All five items are built and play-mode verified (no errors; NavMesh re-bakes to 719 triangles and paths from both far corners to the player still complete, so nothing new blocks the level). Rebuild with **Eco-Dash → Run the art pass (B5)**, then **Rebuild Level 1 / the hub / Level 2**.

| Item | What shipped |
|---|---|
| **B1** Living trees | `Greybox_TreeOak` / `Greybox_TreePine` — variants of the dead-tree prefab, so the trunk collider and walk-under canopy are identical. Planted round the village and on the mesa. The dead trees now ask for their dead colour explicitly (see the palette note below), so the polluted plain still reads as dead. |
| **B2** Terrain elevation | A 4.2 m stepped rock mesa in the empty north-west quarter with pines on top and a spring pool at its foot, plus three rings of hills outside every boundary wall and a lake beyond the north-east one. One box collider and one sphere collider between them; nothing is climbable, per golden rule #1. |
| **B3** Ground reads as ground | Three earth tones across the 192 floor tiles instead of one, ~390 scattered grass/bush/flower/stone details (up from ~300, and weighted green), and green land visible past the walls. **No ground texture was sourced** — the shopping list's reasoning held up. |
| **B4** Village | The 2D layout's four huts are real cottages now — `Greybox_Hut` is rebuilt from Fantasy Town wall + roof modules, so the CSV never changed. Four more buildings, a fountain square, two market stalls, a cart and lanterns fill the empty strip north of the pen. The hub got the same treatment: it was five objects in a grey box and now has a working yard. |
| **B5** Sky | A tuned procedural sky per scene (`SceneLook.Sky`) — smoggy yellow-grey over the farm, cold overcast over the factory, clean blue over the hub. Zero assets sourced, as predicted. |

**Two defects found while doing it, both fixed, neither previously reported:**

1. **Kenney's Nature Kit imports with the wrong colours.** It is the only pack here that ships no texture, and the colours baked into its FBX materials are a washed-out pastel set — `leafsGreen` imports as turquoise `(0.44, 0.90, 0.84)`, `dirt` and `stone` as near-white. Every tree, grass tuft, rock, bush and fence in Level 1 was rendering cyan. All 23 materials are now re-authored in `ArtKit.NaturePalette`. This is the single biggest visual change in the cycle and it was never on anyone's list.
2. **The knee-high fence.** `fence_planksDouble` at import proportions stands 0.35 m, which next to a 1.75 m villager read as a ladder lying on the ground. Stretched to 0.85 m; the collider follows, and since a CharacterController never stepped over either height, nothing about what it blocks changed.

**Still open, deliberately:**
- **P3** — shard economy cost (3 vs. 1); depends on P4.
- **P4** — whether Bé Mây/Ông Tài/Cô Lan get placed this cycle. **B4 built the houses they would live in** (the village district north of the pen has four buildings and a square), so placing them is now purely a dialogue/quest job.
- ~~**The camera angle**~~ — decided: stays at 50°, and the A2 demo route covers the terrain instead. See the measurement above.
- **The ship-line** — still the PO's call once P2's pillar scoring is done.
- **Greenie reads as a plain white sphere from behind.** The model (Kenney's `oopi`) is a mint character in a white shell with a face, but the fixed camera looks at his back whenever he walks away from it, which is most of the time. Not a bug and not touched — flagging it because "the character looks fine" in the rubric table above was judged from the front.

---

## Delivered — B6: the perspective toggle

Built and play-mode verified on 2026-08-31 (**49 checks green, no exceptions**). Nothing needs
rebuilding — B6 is code plus one component on `CameraRig.prefab`; the level, art and audio
generators are untouched.

**Press `P`.** The camera dives from the ¾ rig to Greenie's eyes over 0.3 s; the mouse looks
around; `P` again brings it back. The mode survives a scene change, so walking a portal into the
hub keeps you in first person.

| Acceptance criterion | Result |
|---|---|
| `P` toggles both ways, polled off `Keyboard.current`, no action-asset binding | ✅ verified through synthesised real key events, not by calling the toggle |
| Greenie's model does not fill the frame | ✅ his **renderers** are disabled — the `Visual` node stays active, because `PlayerAnimator` owns its transform and rewrites it every frame |
| Survives a scene change | ✅ farm → hub keeps the mode, and the hub's own rig builds its own first-person camera |
| Cannot fire while a modal owns the screen | ✅ and this needed a new piece — see below |
| Both framings playable; a mid-fight toggle does not kill you | ✅ the world never pauses, the blend is 0.3 s, and the look starts at the ¾ camera's own heading so `W` means the same thing the frame after the toggle as the frame before |

**What it cost, against the estimate.** The write-up said "Large, and it is not the camera that
makes it large" — right on both counts, but the movement half came out smaller than feared,
because it did not become a second control scheme:

```csharp
moveInput = PerspectiveMode.MoveFrame * dir;   // identity under the ¾ camera
```

`MoveFrame` is the identity rotation while the ¾ camera is live (its yaw is locked at 0), so
every top-down path is unchanged rather than merely intended to be — a real-key probe walks
`W` 2.03 m along `+Z` in top-down with zero cross-axis drift, and 2.02 m due east in first
person while looking east. `PlayerShooter` needed **no edit at all**: it reads
`FacingDirection`, which now follows the look in first person.

**Two things found while building it, neither previously reported:**

1. **The project had no way to ask "is a screen open?"** Most modals announce themselves by
   parking `Time.timeScale` at 0 — and `GameFeel`'s hit-stop crawls at 0.02 precisely so it
   never reads as one — but the **bag, codex, quest log, crafting bench and shop never touch the
   clock**. Without a shared answer, `P` would have swapped the camera under a player reading
   their inventory, and the cursor would have stayed locked away from the panel they had just
   opened. `UiModal` is now that answer and those five register with it. It is a small piece of
   plumbing that any future modal-aware feature gets for free.
2. **First person has no facing cue.** Under the ¾ camera Greenie's body *is* the aim indicator;
   at eye height there is nothing. `FirstPersonReticle` puts a centre dot on screen — and it is
   a *horizontal* aim marker, not a 3D crosshair, because Seeds still fly flat: looking down
   does not tilt a shot. Verified.

**Open, deliberately:**

- **B7 is now visibly the next item.** B6's own write-up predicted it would expose the sky, and
  it does — from eye height Level 1's horizon is a gradient smear and Level 2 is an
  open-topped box with a procedural sky over an indoor level.
- **Every framing measurement in this document still assumes 50°.** The "~19 m of ground visible
  past Greenie" figure, the A2 demo route and both QA passes were tuned for the ¾ camera, which
  is still the default and still the one to demo. First person is an addition, not a
  replacement.
- **Mouse sensitivity is a serialized field on `PerspectiveRig`, not a settings-panel slider.**
  0.12°/pixel felt right; if playtesting disagrees it belongs in `GameSettings` beside the
  volume controls, which is a small job nobody has asked for yet.

---

## Delivered — B7: the horizon

Built and verified on 2026-08-31 (**20 invariant checks green**, B6 re-run **13/13** after the
rebuilds, clean error log). Rebuild with **Eco-Dash → Rebuild Level 1 / Rebuild Level 2 /
Rebuild the hub** — all three were re-run and committed. Evidence:
[`QA/screenshots/b7_*`](QA/screenshots/).

| Acceptance criterion | Result |
|---|---|
| From eye height, in both levels, all four cardinal directions show a deliberate horizon | ✅ rendered from Greenie's eye height (1.05 m, level) in N/E/S/W for all three scenes and looked at, rather than reasoned about |
| Level 1's boundary reads as *distance*, not a 3 m wall with sky above it | ✅ ridges now recede behind the wall in three bands and dissolve into haze — see `b7_farm_horizon_south.png` |
| Level 2 gets an answer for "what is above a factory maze?" | ✅ a roof at 12 m, a shell 25 m out, trusses and strip lights — `b7_factory_hall_interior.png` |
| Fog / atmosphere tuned per scene alongside `SceneLook`, so the cut-off is a choice | ✅ and sized against the actual distances in the scene, not picked by eye |
| Zero new assets | ✅ as predicted — `RenderSettings`, `SceneLook.cs`, and Nature Kit models already in the repo |

**The Level 1 problem was not the sky. It was two colours that should have been one.**

Distant geometry fades toward `RenderSettings.fogColor`. Where the geometry stops, what shows
through is the procedural sky *below its own horizon line* — which is `_GroundColor`. B5 authored
those independently, warm tan fog against dark-brown sky-ground, so **every far object ended on a
visible seam**: the hills read as cardboard boxes cut out and pasted onto a different sky. There
is now one value, `SceneLook.Horizon(look)`, feeding both, and no way to author them apart.

Two supporting changes: fog density sized against the real distances (the far ridge is 68 m out,
the world stops at 110 m — so 0.0138 puts the ridge ~55% into haze and leaves the ~19 m the ¾
camera can see at 7%, i.e. **the framing the game is tuned for barely moves**), and the outer
hills capped with `cliff_blockSlope_*` instead of flat cubes, which is what stops them reading as
packing crates at eye level. The hub, which had **no fog at all**, got a light 0.0095 and its
ridges pushed from 8/18 m out to 16/34 m so there is air for it to work in.

**Level 2 got a roof, and QA C11 is the reason it is a roof and not a taller wall.**

C11 reports Level 2's walls occluding the ¾ camera, so building upward is exactly the wrong
instinct. A roof is exempt, and the argument is exact rather than hopeful: the ¾ camera sits
9.693 m up at pitch 50° with a 60° vertical FOV, so its highest frustum ray — a top *corner*,
higher than the top edge — still points **27.9° below horizontal**. Nothing at or above the
camera's own height is ever in frame. The lowest thing the hall hangs is a strip light at
10.84 m and the biggest camera shake in the game is the Mega-Smog's 0.32 m, leaving 0.83 m of
margin. Checked empirically too: Level 2's ¾ framing rendered from three positions with the hall
switched on and off gives **0 differing pixels out of 291 600, three times over.** Every QA pass
ever run on Level 2 is still valid.

The hall has no colliders (so the NavMesh, which bakes from `PhysicsColliders`, never sees it),
casts no shadows (a roof that did would put the whole plant in shade), and is emissive rather
than lit (a ceiling's underside faces down, so ambient shades it near-black and a lit material
would give back a void).

**One bug found on the way, and it is the more useful half of this ticket.**

**The five Level 1 generators shared a single `System.Random`.** Every draw therefore depended on
how many the *previous* generator had spent — so re-profiling the outer hills changed the number
of ring points and silently re-rolled the mesa standing behind them: **34 rock cubes became 27**,
in geometry cycle-2 QA had already signed off. Nothing about the mesa had been edited. Each
generator now seeds its own stream. Worth knowing generally: *a shared RNG makes every generator
a dependency of every generator before it*, and the failure is invisible until someone compares
counts.

**Still open, deliberately:**

- **The mesa did move once, unavoidably** — 34 rock cubes → 28 — because insulating it means
  drawing from a fresh stream rather than continuing the shared one. It is still a 4.2 m stepped
  mesa with one box collider per column (QA C3's fix is structural, not random) and the spring at
  its foot is at a fixed position. From here on it is stable against edits elsewhere.
- **The toxic mud pools read as flat pale sheets** from eye height. Pre-existing, unrelated to
  B7, and a gameplay element rather than scenery — flagging it because it was mistaken for a
  rendering defect while diagnosing this.
- **QA C10 / C11 / C12 are untouched** — B7 sits next to C11 and deliberately works around it
  rather than fixing it. C12 in particular is a design-vs-QA disagreement, not a coding bug:
  `Level2Builder.Dress()` states in the code that its 38 factory props carry no colliders on
  purpose, "so the corridors the player and the NavMesh see are exactly the ones the tilemap
  authored". That is a PO call, not a dev fix.

---

## Delivered — B8, the ground

**Level 1's valley floor rises and falls: 2.20 m of range (−1.18 m to +1.02 m), peaking at
24.7°.** QA raised this as
[R1](QA/exploratory-pass-2026-08-26.md#r1--undulating-ground-change-request-not-a-defect) and
deferred it as a PO call; it is now built. Full write-up:
[architecture.md § The ground is a function](.claude/docs/architecture.md#the-ground-is-a-function-b8).

**Golden rule #1 was rewritten first**, as B6's change was. It never said "the ground is flat" —
it said *no platforming* — and all of that still holds: no jumping, nothing to fall off, nothing
to climb, gravity still not a mechanic.

### The costing in this document was wrong in one expensive place

B9's **ordering note** says the two items "want the *same* rewrite — a controller that follows a
surface normal handles both a hill and a wall, and doing them separately means doing it twice."
**QA had already said otherwise and QA was right.** R1 lists under "three things already support
it, for free": *"`PlayerController` already applies a constant `Vector3.down * 9.81` ground-stick
every frame, and Greenie's `CharacterController` is already configured for terrain:
`slopeLimit = 45°`, `stepOffset = 0.3`. Greenie would walk up and down slopes today, with no
code change."*

He does. **`PlayerController` was not edited** — not one line — and neither were `PlayerAnimator`,
`PlayerHealth`'s knockback, the hazards or the contact-damage geometry, every one of which B9's
cost estimate lists as a landing site. The relief is capped at 24.7° for exactly that reason: it
is chosen against the 45° the controller *and* the NavMesh both already accept.

### What the work actually was

| | |
|---|---|
| **One height function** | `GroundHeight.At(x, z)` — five things must agree on where the ground is to the millimetre: the tile meshes (or there is a seam), their normals, 685 props authored at y = 0, the projectiles, and the generator itself *before any mesh exists*. |
| **192 generated tiles** | Not one continuous mesh, because `GroundCleanser` tints the ground **a renderer at a time** and that is what drives Độ Sạch. Each tile samples the shared function, so neighbours meet to the float. **Normals are analytic** — `RecalculateNormals` gives a shared vertex two different answers and the 4 m grid comes back as a lighting seam. |
| **Ten flat zones** | The boundary and its 112 fence posts, the mesa (QA C3 grew its per-column colliders down to y = 0), the spring, the village, the boss grove, and **the four 9 m reclamation discs** — a flat disc on a slope buries its uphill half and floats a lip along the downhill one. |
| **One settle pass** | `TerrainKit.Drop` puts **685 objects** back on the ground after everything is placed, instead of threading a height lookup through four generator files and a CSV. It *adds* the height, so authored lifts survive. |
| **The projectile gate, in seven lines** | A Seed records its **clearance** at launch and holds it for the flight. Aim, spread fan and `travelDir` untouched — a shot still goes exactly where it was pointed. A null profile returns immediately, so Level 2 and the hub are provably unaffected. |

### Combat on a slope, measured against a control

The acceptance criterion this item says *matters*, run both ways with the ground field switched
off as the control — i.e. exactly what a Seed did before B8:

| Shot | With B8 | Control |
|---|---|---|
| **147 cm of rise over 11 m** | **HIT** — clearance held at 0.58 m | **MISS** — ends 0.35 m *underground*, carries on to 18 m |
| **150 cm of fall over 11 m** | **HIT** — clearance held at 0.61 m | **MISS** — ends **1.49 m above the ground beneath it**, sailing over its head |

One layer fact that bounds the risk: **`PlayerProjectile` does not collide with `Ground`** in
this project's physics matrix. A seed passes through a hillside rather than fizzling on it, so
the relief can never make a shot die early — the only failure it could introduce is the vertical
miss, and that is the one the clearance fixes.

### Two things recorded as they came out

**The first tuning rendered as a flat plane.** It satisfied every acceptance number — 1.56 m of
range, 14.4° peak, 30 of 30 invariants green — and looked like nothing. At 65 × 49 m under a
camera 9.7 m up there is no occlusion cue (a hill would need to be 9 m tall to hide anything)
and no self-shadowing either (the sun sits at 48°, so nothing under a 42° slope can shade
itself), which leaves the diffuse term as the only cue there is; at 14.4° that is a few percent
under a strong ambient. At 24.7° it is a **41% swing** in `N·L` and the ground reads. It is still
understated from the ¾ camera and clearest at eye height, and that is inherent to the scale
rather than something more amplitude would fix without putting crates on slopes they would
visibly slide down. Evidence: [QA/screenshots/](QA/screenshots/) `b8_*`.

**The camera's new vertical damping is unmeasured.** `CameraFollow` now damps Y at 0.55 s against
0.15 s on X and Z, which is the remedy this item's acceptance criterion names. Four attempts at
measuring the benefit (peak vertical speed, RMS speed, vertical path length, lag) came back
either dominated by editor frame-pacing hitches or too small to separate from run-to-run noise.
The setting is right by construction and costs nothing; the improvement is not evidenced, and is
recorded that way rather than dressed up with whichever number looked best.

### Verification

**30/30 static** — 192 tiles on the Ground layer with a `MeshCollider` each, neighbours meeting
with no gap and no normal disagreement, all ten flat zones dead level to a millimetre, the
boundary untouched, every settled prop / gameplay object / enemy sitting on the ground, the
NavMesh still pathing to both far chests and the boss grove, the cleanser's footprint cap still
cleared, and **no `GroundHeightField` in Level 2 or the hub**, so both are provably still flat.

**In play** — 4.0 cm of capsule drift over a 146 cm climb; the two shots above; `GroundCleanser`
repainting 4 of 4 tiles on 24.7° ground. Clean error log.

---

## Delivered — B9, the wall

**Greenie ant-walks the Level 1 mesa: three 1.4 m faces to the 4.2 m summit in 3.1 s, and back
down.** Full write-up:
[architecture.md § Which way is up is a state](.claude/docs/architecture.md#which-way-is-up-is-a-state-b9).

### The costing in this document was wrong a second time

> *"A `CharacterController`'s capsule is permanently world-Y aligned and cannot be re-oriented —
> so ant-walking means replacing it with a `Rigidbody` plus a surface-aligned controller. That is
> a rewrite of the one script the whole game's feel rests on, and it lands on `PlayerAnimator`,
> `PlayerHealth`'s knockback, every hazard that calls `EnterMud`/`ExitMud`, the contact-damage
> geometry, and `CameraFollow`."*

The capsule really cannot be re-oriented. **But `CharacterController.Move` takes a world-space
delta and has no opinion about gravity**, so a capsule pressed against a wall climbs it the moment
you hand it a delta pointing up the face — collide-and-slide holds him against the rock for free,
exactly as it does on the ground. What actually changed:

| Named as a landing site | What happened |
|---|---|
| `PlayerController` (rewrite) | **One substitution**: `Vector3.down` → `-SurfaceFrame.Up`, plus two lines handing the velocity to the climber |
| `PlayerAnimator` | Bob, squash and turn read the frame instead of `Vector3.up` — identical on the ground |
| `PlayerHealth` knockback | `away.y = 0` → `ProjectOnPlane(away, SurfaceFrame.Up)` — the same operation on the ground |
| Hazards (`EnterMud`/`ExitMud`) | **Untouched** |
| Contact-damage geometry | **Untouched** |
| `CameraFollow` | **Untouched** — the ¾ camera never rolls |

**Both of this document's cycle-3 estimates made the same mistake**, and that is the part worth
carrying forward: they reasoned from what a component *is* rather than from what its API *takes*.

### What the work actually was

| | |
|---|---|
| **One frame** | `SurfaceFrame` — the single answer to "which way is up for Greenie right now". Six places used to say `Vector3.up` in their own hand. **It is the identity on the ground**, so `-Up * stick` *is* `Vector3.down * stick` and `ProjectOnPlane(v, Up)` *is* `v.y = 0`. Physics reads the exact frame; the mesh and the first-person camera read an eased one, or a dismount shoves him off the top. |
| **One permission** | `Climbable`, on 18 colliders. The natural rule — push into a vertical face — applied to *geometry* lets Greenie climb the boundary wall and stand on the skybox. `TerrainKit.Column` generates the marker with the rock. |
| **Two rules at the edges** | Off the **top** is a dismount (step `radius + 0.3` m over the lip, then let go). Off the **side** is an edge (cancel that component, keep the rest) — because a climber handed to the ground stick at 4 m falls the whole way at 9.81 m/s, which is the "launched or stuck" failure this item forbids. |
| **Half speed on a wall** | At the full 5 m/s a 1.4 m tier is over in 0.28 s, which reads as teleporting up the rock. |
| **No firing on a wall** | Chosen, not overlooked: the acceptance criterion allows it, a seed would hold its B8 ground clearance and curve into the sky, and nothing up there can be shot. It leaves the projectile system B8 just settled completely alone. |

### Verification

**22/22 static** — 18 markers and only 18 (of 140 solid obstacles), each on the collider itself,
all under the Highlands root, on a mesa stepped 1.4 / 2.8 / 4.2 m over dead-level B8 ground; the
boundary is not climbable; Level 2 and the hub carry no marker at all; the `CharacterController`
is byte-for-byte the one B8 left.

**31/31 in play**, driving the real keyboard through the Input System rather than calling the
climber directly: 3 attaches and 3 dismounts to the summit in 3.1 s · no frame off a lip over
5 cm · hangs on the face with **0.0 cm of drift in 1.5 s** with no key held · traverses 2.09 m
across a seam, handed between two separate column colliders, and **stops** at the end of the rock
· walks back down to 0 m and returns the frame to exactly the identity · fires nothing while
climbing and normally again on the ground · **cannot be knocked off** a face (1.6 cm along the
normal) · rolls 90.0° in first person with the eye 1.05 m out along the normal · and a control
run with the climber switched off gets **0.00 m** up the same rock.

### Three things reported rather than smoothed over

**The hitbox on a wall is not the silhouette.** The capsule stays world-Y aligned and flush with
the face while the mesh lies flat against it. Free today — the enemies are NavMesh-bound to the
floor and seeds fly at ground clearance — and *not* free the day something can shoot at Greenie
on a wall. That is the point at which the `Rigidbody` rewrite becomes worth its price.

**First person on a wall shows a lot of sky.** The design question this item raised — *"on a
wall, which way is up?"* — is answered with **his** up: the camera rolls 90° and the rock reads as
the floor. His forward is then up the wall, so at level pitch he is looking at sky with the valley
sideways at the edge of frame. Correct for an ant, and on a rock only 4.2 m tall there is almost
no wall left above him to look at. Evidence:
[`QA/screenshots/b9_climb_firstperson_wall.png`](QA/screenshots/b9_climb_firstperson_wall.png).
Whether it is *good* at this scale is a PO judgement, and the screenshot is there so it can be
made from evidence.

**The ¾ camera cannot see the mesa's north face.** Measured *blocked by Column at 9.0 m*, and
[the screenshot](QA/screenshots/b9_climb_threequarter_north.png) is an empty rock. **B9 did not
introduce this**: the same ray at ground level, standing north of the mesa with no climbing
involved, is blocked at 7.2 m. It is a pre-existing camera limitation that applies to the village
cottages too; the fix is an occluder pass for the whole game, and first person (`P`) is the
workaround that exists today.
