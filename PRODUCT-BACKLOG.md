# Product Backlog — Eco-Dash 3D

> Owned by **Thanh Tùng & Đức Anh** (Product Owner, [CYCLE-2-TASKS.md § 3](CYCLE-2-TASKS.md)).
>
> **Status: B1–B5 are built.** Everything below the rubric table has been implemented
> and play-mode verified — see [Delivered](#delivered) for what changed and what is
> still open. The findings are kept as written so the reasoning survives.
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
