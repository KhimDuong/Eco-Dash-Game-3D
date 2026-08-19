# Product Backlog — Eco-Dash 3D

> Owned by **Thanh Tùng & Đức Anh** (Product Owner, [CYCLE-2-TASKS.md § 3](CYCLE-2-TASKS.md)).
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
| Trees | **Present but all dead.** 30 `Greybox_DeadTree` (`tree_thin_dark.fbx`) in Level 1, 8 more ringing the Slime King's grove. Zero living tree anywhere, despite ~60 living variants (`tree_oak`, `tree_pineTallA-D`, `tree_default`, …) already sitting imported and unused in `Kenney_NatureKit/`. |
| Grass | **Present but thin.** Level 1's scatter pass draws from an 8-model palette where only 3 (`grass.fbx`, `grass_large.fbx`, `plant_bushSmall.fbx`) are grass-like; the rest are rocks/flowers/mushroom/stump. The ground itself is a flat brown solid-color material, not grass. |
| Sky | **Present, stock only.** Unity's default procedural skybox, un-authored. No per-scene sky look (Level 1/2/hub differ only in ambient+fog+post-processing, per `SceneLook.cs`). |
| Terrain elevation (mountains/hills/rivers/lakes) | **Absent.** No `UnityEngine.Terrain` exists anywhere in the project — confirmed by a project-wide grep. Every scene's ground is a flat ProBuilder slab at `y=0`. Zero water anywhere. |
| Village / houses / structures | **Minimal.** 4 `Greybox_Hut` (one Survival-Kit roof piece each) + a fence ring in Level 1; nothing house-like in the hub beyond the shop counter and crafting bench. Reads as "farm boundary," not "village." |
| Controllable 3D character | **Solid, no notes.** `Player.prefab` (Greenie = Kenney's `oopi` robot), full WASD movement on the ground plane, working as intended. |

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

**Not decided here, and deliberately left open for Tùng/Đức Anh:**
- **P3** — shard economy cost (3 vs. 1); depends on P4.
- **P4** — whether Bé Mây/Ông Tài/Cô Lan get placed this cycle; **B4 above should be decided in the same breath as this one.**
- **The ship-line** — none of B1–B5 above are drawn above or below a line. That's the PO's call once P2's pillar scoring is done.
