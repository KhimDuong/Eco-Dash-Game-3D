# Eco-Dash 3D — Team Tasks (3 developers)

> Companion to [3D-CONVERSION-PLAN.md](3D-CONVERSION-PLAN.md). Each dev
> vibe-codes with Claude + the Unity Coplay MCP. Every task ends with:
> `check_compile_errors` clean → Play-mode smoke test → save scene **in place**
> → commit on a task branch → PR.
>
> Roles: **Dev A — Core & Player**, **Dev B — World & Levels**,
> **Dev C — Combat & Presentation**. Scene ownership: A owns `MainMenu` +
> story scenes; B owns `Level1`, `Level2`, `Hub`; C owns no scene (works in
> prefabs + a private `Sandbox_Combat` test scene).

---

## Dev A — Core & Player (foundation everyone builds on)

**A1. Project bootstrap** *(P0 — blocks everyone; do first, same day)*
Create the Unity 6 (6000.3.16f1) **URP 3D** project at repo root. Folder tree
mirroring the 2D repo; tags (`Player`, `Enemy`, `EnergyCore`, `Pickup`,
`Projectile`, `Hazard`), layers + collision matrix; Unity `.gitignore` +
Smart Merge; copy `InputSystem_Actions.inputactions` from the 2D repo; install
ProBuilder + Cinemachine. The vibe-coding docs (`CLAUDE.md`, `.claude/docs/`,
`.claude/commands/`, `ASSETS.md`, `CREDITS.md`, `.gitignore`) are **already
seeded in this repo** — review them and fix anything the real project setup
contradicts. ✅ *Done when: empty project compiles, pushed to `main`, B and C
can clone.*

**A2. Tier-0 systems port** *(P0)*
Copy all Tier-0 scripts (see plan §3) from the 2D repo unchanged: `Systems/`,
`UI/` (minus `DynamicYSorter`), `Shop/`, `Items` data classes, dialogue,
interfaces. Fix only using/namespace issues. ✅ *Done when: compiles clean with
zero 2D API references in Tier-0 files.*

**A3. Player 3D** *(P1 — the heart of the port)*
`PlayerController` on **CharacterController**, WASD → XZ movement, visual child
rotates to face movement; port `PlayerHealth` (i-frames, hurt flash → emission
flash), `PlayerShooter` (J, spawns 3D `SeedProjectile`, spread upgrade intact),
`PlayerInteractor` (E, `Physics.OverlapSphere` for `IInteractable`),
`PlayerAnimator` (hover-bob + squash on the visual child — same code, 3D axes).
Capsule/free robot placeholder mesh. Make `Player.prefab`.
✅ *Done when: move/shoot/take damage/interact all work in a test scene.*

**A4. Camera** *(P1)*
Cinemachine follow at fixed ¾ angle (pitch ~50°, dist ~12, no player control).
Keep a `CameraFollow.Shake(…)`-compatible wrapper (impulse source) so ported
callers (`PlayerHealth`, boss) don't change. ✅ *Done when: camera tracks
smoothly and shake fires on player hit.*

**A5. HUD & menus wiring** *(P1–P2)*
Bring over `HUD.prefab` behavior: HP bar, counters, `ObjectiveTracker`, hotbar,
bag (I/Tab), quest log (Q), codex (C), pause (Esc) + `SettingsPanel`,
`EndScreenController`. Rebuild the canvas via Coplay if the prefab won't
migrate cleanly. `MainMenu` + `Intro_Story`/`Ending_Story` scenes (UI-only —
near-verbatim port), Build Settings order.
✅ *Done when: menu → intro → L1 flow runs and every panel opens in Play mode.*

**A6. Persistence & New Game** *(P2)*
Verify `SaveSystem`/`PlayerProgress`/`Inventory`/`QuestLog`/`Codex`/
`SceneProgress` survive scene loads and reset on New Game, exactly like 2D.
✅ *Done when: quit-and-continue keeps upgrades/items; New Game wipes them.*

---

## Dev B — World & Levels (owns all gameplay scenes)

**B1. Greybox kit + L1 blockout** *(P1)*
ProBuilder floor/wall/prop greybox prefabs. Block out `Level1_BarrenFarm` in 3D
using the 2D scene as the map: same room shapes, mud-pool positions, chest
spots. Baked NavMesh for enemies. ✅ *Done when: A's player can walk the whole
level with working collisions.*

**B2. L1 interactables & flow** *(P1–P2)*
Port trigger scripts as you place them: 3 chests → `EnergyCore` →
`ReclamationPatch` (dead ground → green — swap materials in a radius) →
`TeleportGate` to hub. `ToxicMud` trigger pools, `Litter` + cleaning loop,
`HealthPickup`/`SpeedBoostPickup`, lore notes. NPCs placed: Bà Tư
(quest-giver), Ông Sáu (M8 quest), herbs ×3. Objective flow = 2D parity.
✅ *Done when: L1 start→gate is completable with objectives ticking.*

**B3. L2 Factory Maze** *(P2)*
ProBuilder corridor maze from the 2D layout. Port + place `SweepingLaser`
(beam = scaled emissive cylinder + trigger), `ManholeTrap`, `ToxicGasZone`.
Keycards ×3, `BossDoor`, boss arena room, Tí (`RescueNPC`) at the entrance.
✅ *Done when: maze is navigable, all hazards damage the player, 3 keycards
open the boss door.*

**B4. Hub (Recycling Station)** *(P2)*
Small plaza/interior: Ông Bear (`ShopNPC`), `CraftingBench`, `PortalNexus` with
per-stage gates (Stage 2 gated on 3× Mảnh Cổng), `ReturnPortal` placed in L1+L2.
Relocated side-quest NPCs (Bé Mây, Ông Tài, Cô Lan). ✅ *Done when: full portal
round-trip works (L1 ↔ hub ↔ L2) and shop/bench UIs open.*

**B5. Art & lighting pass** *(P3)*
Replace greybox with free low-poly packs (Kenney/Quaternius/KayKit/itch.io):
farm set for L1, factory set for L2. URP lighting + post volumes (L1 warm/hazy,
L2 dark industrial). Log every pack in `CREDITS.md` **as you import it**.
✅ *Done when: no greybox visible in a normal playthrough; CREDITS complete.*

---

## Dev C — Combat & Presentation (prefabs only; test in own sandbox scene)

**C1. Enemy foundation + PlasticSlime** *(P1)*
Port `IDamageable`/`IKnockbackable` consumers to 3D. `PlasticSlime` as a
NavMeshAgent (or simple Rigidbody chase): aggro radius, contact damage, HP,
white-flash → emission flash, death → material drops via `Inventory` contract.
Prefab handed to B. ✅ *Done when: slime chases, hurts, dies, drops — in
sandbox and in B's L1.*

**C2. Projectiles & FlyBot** *(P2)*
3D `SeedProjectile` + `EnemyProjectile` (SmogOrb): trigger colliders,
knockback, lifetime. `PollutionFlyBot` now genuinely hovers on Y: 3D-raycast
LOS → chase → fire orbs. ✅ *Done when: J-shots kill enemies at range; fly-bot
kites and shoots the player.*

**C3. Bosses** *(P2)*
`SlimeKing` mini-boss (L1 — drops Mảnh Cổng) and `MegaSmogBoss` (L2): engage on
approach, ring-spray on the XZ plane, random `ToxicGasZone` spawns, enrage
phase, death → `CompleteLevel` → Ending_Story. `BossHealthBar` wired.
✅ *Done when: force-kill and fair-fight paths both reach the Win flow.*

**C4. Game-feel** *(P2–P3)*
Knockback on both sides, camera shake via A4's wrapper, hit-stop optional,
damage flashes, death poofs (particles), cleaning VFX (trash-destroy →
`Codex.AddCleanliness` radius reveal — parity with the 2D GroundCleanser loop).
✅ *Done when: combat "feels" at least as punchy as the 2D build.*
> **Correction, found while doing it:** there is no 2D `GroundCleanser` to be at parity
> with. It was specified in `game-design` §4.7.5/§4.7.8 and never written, so nothing in
> the 2D build ever called `AddCleanliness` and the codex's Độ Sạch tab sat at 0% all
> game. C4 wrote the loop rather than porting it.

**C5. Audio & credits sweep** *(P3)*
Copy all `Assets/Audio` from 2D (music, SFX, jingles) and rewire via
`MusicVolume`/`GameSettings`. New ambience only if free-first sourcing finds
something better. Final `CREDITS.md` + `HOW_TO_PLAY.md` update for 3D.
✅ *Done when: every scene has music + SFX obeying the settings sliders.*

---

## Dependency order (who blocks whom)

```
A1 → A2 → { A3, A4 } → A5/A6
A1 → B1 → B2 → B3 → B4 → B5
A2+A3 → C1 → C2 → C3 → C4 → C5
Integration points: B places C's enemy prefabs; A's Player.prefab used by B & C;
all three meet at the P1 gate (vertical slice) before P2 starts.
```

**Weekly sync:** at each phase gate, one person runs the full playthrough and
files issues; nobody starts the next phase with a red gate.
