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

1. **Two framings, two frames, one XZ world — and still no platforming.** **B8 made
   Level 1's ground rise and fall** (2.20 m of range, peaking at 24.7°, chosen against
   the `CharacterController`'s 45° `slopeLimit`) and **B9 let Greenie ant-walk the
   mesa's rock faces**. Neither made gravity a mechanic: **there is still nothing to
   jump, nothing to fall off and no air control.** A climb is *walking on a wall* — he
   hangs there indefinitely with no key held, and lets go only at the top or the bottom.
   Two things follow, and both are non-negotiable:
   - **Height is `GroundHeight`'s to answer**, in the editor and at runtime alike, and
     it is null (0 everywhere) in every flat scene; never raycast for the ground or
     re-derive it. Seeds still fly **flat**, but flat means *flat over the ground* — a
     constant ~0.6 m above whatever is beneath them, never on a world-Y line, never on
     an arc.
   - **Up is `SurfaceFrame`'s to answer.** `SurfaceFrame.Up` is `Vector3.up` unless
     Greenie is on a wall, and `SurfaceFrame.Rotation` is the identity there. Never
     write `Vector3.up` (or `v.y = 0`) in player, camera or aim code again — ask the
     frame, so the expression stays correct on a rock face and identical on the ground.
     Only a collider carrying **`Climbable`** may be climbed; the mesa's 18 columns are
     the only ones in the game, and adding the marker anywhere else is a design change.

   The **default and canonical framing is the fixed ¾ Cinemachine camera** (pitch 50°,
   yaw 0) — every layout, sightline and QA pass is tuned at it, and **it never rolls,
   even on a wall**. **B6 added one alternative: `P` drops to first person with mouse
   look, and back**; in first person on a wall the look *does* roll into the surface
   frame, because up there his up is the answer. So movement and aim are
   **camera- and surface-relative**: WASD is read in screen axes and turned into world
   axes by `PerspectiveMode.MoveFrame`, which is the identity under the ¾ camera on the
   ground (input Y still maps to **world Z** there), the look yaw in first person, and
   the wall frame while climbing — `W` is always up the face. Never author movement, aim
   or camera code in raw world axes again — go through `PerspectiveMode` and
   `SurfaceFrame`. Never add side-scroller logic, and nothing else may control the camera.
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
   last resort — ask first, it costs money**. Full rules: [ASSETS.md](ASSETS.md).
   The art itself is applied by a **generator**
   (`Eco-Dash → Run the art pass (B5)`), not by hand — add a row to `ArtPass.cs`,
   never drag a mesh onto a prefab. When importing a new Kenney pack, check that
   its texture file is named what its FBX materials ask for (`colormap.png`), or
   Unity binds all of it to a *different* pack's atlas without warning.
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

`W/A/S/D` move on the ground plane · `P` toggle ¾ ⇄ first person (mouse looks around
in first person) · `J` shoot Seed projectile · `E` interact (NPC/chest) · `Esc` pause ·
`I/Tab` bag · `1–4` hotbar · `Q` quest log · `C` codex · `H` how-to-play.
Read exactly as in the 2D repo: **direct polling of `Keyboard.current`**
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
**P0 done; A1–A6, B1–B5, C1–C5 done — the game is finishable end to end, with sound:**
menu → intro → farm → hub → factory → boss → outro. The URP-3D project, Tier-0 + Tier-1 scripts,
`Player.prefab`, the Cinemachine `CameraRig.prefab`, the whole UI layer (`HUD.prefab`,
`GameManager.prefab`, `MainMenu` / `Intro_Story` / `Ending_Story`),
**`Level1_BarrenFarm`** with its 29 `PlasticSlime`s and the **`SlimeKing`**'s grove,
save/continue parity, the **`PollutionFlyBot` + `SmogOrb`** combat kit,
**`Level2_FactoryMaze`** with its lasers, manholes, keycard chain, boss door and the
**`MegaSmogBoss`** behind it, the **`Shop_RecyclingStation`** hub with Ông Bear's shop,
the crafting bench and the two stage portals, **B5's art pass** — five CC0 packs, real
models on every prefab, and a per-scene lighting/post look — **C4's game-feel layer**
(`Vfx` bursts, `GameFeel` shake + hit-stop, and the `GroundCleanser` cleaning loop that
finally makes the codex's Độ Sạch tab move) and **C5's audio layer** (`Sfx`'s pooled,
distance-attenuated one-shots, the `MusicPlayer` that keeps one track running across every
scene, and the `AudioPass` that puts a clip in all 26 sound fields) are all in and
play-mode verified (305 checks).

**Cycle 2's environment pass is in** ([PRODUCT-BACKLOG.md](PRODUCT-BACKLOG.md) B1–B5): the
Nature Kit's broken material palette is re-authored (Level 1's vegetation was rendering
cyan), Level 1 has a 4.2 m rock mesa, a spring, hills and a lake beyond every wall, a real
village of Fantasy Town cottages around the 2D layout's four huts, living trees, denser
ground cover and a tuned procedural sky; the hub has a dressed yard instead of five objects
in a grey box, and Level 2 no longer shows skybox through the gaps in its maze floor.

The environment pass then went through QA
([QA/exploratory-pass-2026-08-26.md](QA/exploratory-pass-2026-08-26.md)) and **C1–C7 are
fixed**: the spring is a wade volume Greenie walks into (and Seeds fly over), the mesa is a
collider per column instead of one box standing in 6.5 m² of open ground, the hub yard and
the three interactables are solid, reclamation blooms as a circle instead of repainting whole
4 m tiles, and the ground's three earth tones no longer read as a checkerboard. C8 (the mesa's
layer-cake silhouette) is deliberately deferred. **R1 — undulating ground — is now done as
cycle 3's B8**: the valley floor rises and falls by 2.20 m at up to 24.7°, and the combat risk
R1 named was answered by making Seeds fly flat *over the ground* rather than flat in world Y.

**Cycle 3 is complete: B6, B7, B8 and B9 are all in.** **B9** is the last of them — Greenie
ant-walks the mesa's rock faces, three 1.4 m tiers to the 4.2 m summit and back down, on a new
`SurfaceFrame` that is the identity everywhere he is not on a wall. Like B8, it did **not** need
the `Rigidbody` rewrite the backlog costed it at: `CharacterController.Move` takes a world-space
delta, so the capsule climbs a face it is pressed against without ever being re-oriented. What
that costs is a hitbox that does not match the silhouette on a wall — paid deliberately, and
recorded in
[architecture.md](.claude/docs/architecture.md#which-way-is-up-is-a-state-b9).

Next up: the three unplaced side-quest NPCs (Bé Mây, Ông Tài, Cô Lan — the village district
B4 built is where they would live), a full manual playthrough + ~30-min time-budget check,
the A2 demo video, and the submission build.

**Cycle 2 is a validation cycle before it is a build cycle** — the other two devs take
QA / Product-Owner / Business-Analyst roles, play the cycle-1 build and produce the
backlog. Assignments, the pre-seeded defect list and the exit gate:
[CYCLE-2-TASKS.md](CYCLE-2-TASKS.md).

Seven generated things — **don't hand-edit their output**:

| What | Menu command | Source |
|---|---|---|
| Level 1 | **Eco-Dash → Rebuild Level 1** | [Tools/level1_layout.csv](Tools/level1_layout.csv) + `Assets/Editor/TerrainKit.cs` |
| Level 2 | **Eco-Dash → Rebuild Level 2** | [Tools/level2_layout.csv](Tools/level2_layout.csv) |
| The hub | **Eco-Dash → Rebuild the hub** | `Assets/Editor/HubBuilder.cs` |
| Enemy prefabs | **Eco-Dash → Rebuild enemy prefabs** | `Assets/Editor/EnemyPrefabBuilder.cs` |
| Factory kit | **Eco-Dash → Rebuild factory kit** | `Assets/Editor/FactoryKitBuilder.cs` |
| The art | **Eco-Dash → Run the art pass (B5)** | `Assets/Editor/ArtPass.cs` + `ArtKit.cs` + `SceneLook.cs` |
| The sound | **Eco-Dash → Run the audio pass (C5)** | `Assets/Editor/AudioPass.cs` (+ `Resources/MusicKit.asset`) |

The enemy, factory and hub builders rebuild their prefabs from primitives, so each one
**re-runs its slice of the art pass and the audio pass at the end**. Change art by editing
`ArtPass.cs` and sound by editing `AudioPass.cs`, never by dragging a mesh or a clip onto a
prefab — the next rebuild would throw it away.

**Six rules that keep biting:**

0. **A model's imported colours are not the colours the pack means.** Kenney's Nature
   Kit ships **no texture at all** — every model is flat-shaded off its material colour —
   and the values baked into its FBX files are a pastel placeholder set: `leafsGreen`
   imports as turquoise `(0.44, 0.90, 0.84)`, `dirt` and `stone` as near-white. Level 1's
   grass, trees and rocks rendered *cyan* for the whole of cycle 1 and nobody spotted it,
   because each asset looks plausible alone. `ArtKit.NaturePalette` re-authors all 23,
   keyed by material name so 300 models keep sharing one "grass" and stay in one batch —
   which means a prop that wants its own colour (the dead tree) must pass `recolour` and a
   `variant`, or it repaints every other model using that material. A textured pack is the
   opposite hazard: Unity's recursive-up search binds a pack whose texture is named
   anything but `colormap.png` to **another pack's** atlas, silently.



1. **Height is presentation, hitting things is XZ.** Greenie's Seeds fly flat at y ≈ 0.6,
   so anything that leaves the ground still needs a hurtbox reaching the ground plane or
   it is simply unkillable — and anything that *shoots* him has to fire at his chest, not
   from the top of its own model. The same arithmetic runs the other way for a blocker:
   **size it against the cross-section it blocks at, not its widest one.** The spring's
   sphere was sunk so its equator sat underground, which left it 3.09 m wide at Greenie's
   shins and stopped him 0.45 m short of the water — while its *top* reached y = 2.00 and
   ate every Seed fired across the pool. One sphere, two defects.
   See [architecture.md](.claude/docs/architecture.md#flying-enemies-need-two-colliders-c2).
2. **Only the visual is ever swapped.** Colliders, trigger radii and the toggled-child
   pairs (`Visual_Open`/`Visual_Locked`, `Lid`/`Hole`, `Visual_Unconscious`/`Visual_Awake`)
   are the gameplay contract. And a `MaterialPropertyBlock` is **per material slot, not
   per renderer** — the thing that made the new slime's eyes turn green after one hit.
   `ArtKit.Fit` **turns a model before it measures it**: centring a corner-pivoted mesh
   and rotating afterwards leaves it displaced by `R·d − d`, which is how Greenie's art
   ended up orbiting his own hitbox at 1.8 m while every `rotY: 0` prop looked fine.
   `Fit` also **centres what it places**, which is right for a one-mesh swap and fatal for a
   modular kit — Fantasy Town's wall panel sits on the −X edge of its cell on purpose, so
   four of them rotated 0/90/180/270° enclose it; centring each would stack all four in the
   middle. Modular pieces go through `ArtKit.SpawnModule`, which keeps their own pivot.
   And **fitting by height only works on a model roughly as tall as it is wide**: a 5 cm
   ground tile asked for a 3 m height scales 60×, and a 2 m fountain basin asked for 0.9 m
   comes out 6.4 m across. The flip side of "only the visual": **`ArtKit.Spawn` places a
   visual and never a collider**, so anything a generator spawns straight from an art kit is
   a ghost — the hub's whole 25-prop yard, Ông Bear, and Level 1's 2.6 m lanterns all were.
   The generator calls `ArtKit.Solidify` for that (never the art pass), and it fits its box
   in the *holder's* frame, so a turned prop is turned by its holder or it only gets its
   bounding rectangle.
   See [architecture.md](.claude/docs/architecture.md#the-art-pass-is-generated-too-b5).
3. **`HitFlash` owns an enemy's resting colour.** It caches that colour in `Awake` and
   repaints it after every flash, so a lasting tint (the boss's enrage) must go through
   `HitFlash.SetBaseTint` or the next hit scrubs it off. While you're there: a
   percentage-of-max-HP gate belongs in whole HP — `40 * 0.35f` is 13.9999998, and that
   one float cost the Mega-Smog its enrage.
   See [architecture.md](.claude/docs/architecture.md#bosses-bring-their-own-ui-c3).
4. **Statics survive Play, so reset them there.** Fast Enter Play Mode is on: the domain is
   **not** reloaded between sessions, so every static keeps last run's value. The save stores
   get away with it because they re-read PlayerPrefs; a counter or a cache does not. Anything
   holding a runtime-created Unity object is worse — `ItemDatabase` handed back destroyed
   `ScriptableObject`s from the second Play onwards, and every item id silently looked unknown.
   Clear it from `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`. And on the clock:
   **`Time.timeScale = 0` has six owners; never add a seventh** — `GameFeel`'s hit-stop crawls
   at 2% precisely so it can tell its own freeze from a modal's.
   See [architecture.md](.claude/docs/architecture.md#game-feel-is-a-service-cleaning-is-a-loop-c4).
5. **Fast Enter Play Mode also decides which lifecycle callbacks run.** It reuses the scene's
   existing objects instead of reloading them, so on an `[ExecuteAlways]` component `Awake`
   already ran at scene-open in *edit* mode and **never runs again** — which is how
   `CameraFollow.Instance` stayed null and every `GameFeel.Shake` in the game silently did
   nothing while the impulse chain under it was perfectly healthy. Claim singletons in
   `OnEnable`, not just `Awake`. And while you're picking a random number: `UnityEngine.Random`
   is **one global sequence that gameplay is spending** — the slimes draw their wander from it —
   so anything cosmetic (audio pitch, particle scatter) needs its own generator or it will
   quietly change what the enemies do. And one that is not about lifecycle but lives in the same
   family of "the object is there, so it must be running": **a disabled `MonoBehaviour` still
   answers a direct method call.** `if (thing != null) thing.Tick()` runs `Tick` on a component
   whose `enabled` is false — which is how B9's control run, whose entire job was to demonstrate
   the *pre*-B9 behaviour with `WallClimber.enabled = false`, climbed the mesa. Guard with
   `isActiveAndEnabled`.
   See [architecture.md](.claude/docs/architecture.md#audio-is-two-services-and-a-generated-table-c5).
