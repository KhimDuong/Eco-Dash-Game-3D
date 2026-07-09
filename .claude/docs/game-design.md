# Game Design Document — Eco-Dash: Biệt Đội Giải Cứu Xanh

> English-structured version of [../../Eco-Dash-Game.md](../../Eco-Dash-Game.md).
> The Vietnamese brief is the source of truth for creative intent; this file is
> the working spec the code follows. Term map: [glossary.md](glossary.md).

> **⚠️ 3D-port note.** This document was copied verbatim from the finished 2D
> project (`d:\Y4-Sem3\Eco-Dash-Game`) — the game *design* (story, levels,
> enemies, items, quests, systems, Vietnamese UI text) is **unchanged** and this
> file stays authoritative for all of it. Only the *presentation* differs in
> this repo: read every "2D top-down / tilemap / sprite / Y-sort" as
> "**3D top-down** (fixed ¾ camera) / **ProBuilder-greybox → low-poly 3D** /
> **mesh + material**". The technical mapping lives in
> [../../3D-CONVERSION-PLAN.md](../../3D-CONVERSION-PLAN.md) §2 and
> [architecture.md](architecture.md).

## 1. Vision

| | |
|---|---|
| **Title** | Eco-Dash: Biệt Đội Giải Cứu Xanh ("Eco-Dash: Green Rescue Squad") |
| **Genre** | 2D top-down adventure / action-RPG |
| **Reference feel** | *Stardew Valley* movement, camera, and tile world — but combat + level-clear objectives instead of farming |
| **Art style** | Bright, rustic **pixel art**. Strong contrast between gray polluted ground and lush green reclaimed ground |
| **Engine** | Unity 6 (6000.3.16f1), URP 2D |
| **Theme** | Environmental rescue: a small cleanup robot heals a poisoned valley |

## 2. Story

The town of **Greenvale 2026** was a peaceful green valley, now ravaged by the
ruthless **"Black Smoke"** chemical corporation. They dump waste, turn soil into
barren desert, and spawn "Smog Monsters" and "Plastic Waste Monsters" to drive
residents out and seize the land.

The player is **Greenie** — a small cleanup robot powered by an **Eternal Seed**
energy core. Greenie travels the polluted lands, clears waste, fights the
corporation's minions, and revives the valley's water and plant life.

### Narrative arc (acts & beats) — *M7 layer*

> The mechanics below (levels, shop, boss) are all built. This arc is the
> **narrative + mission layer** (M7) that dramatizes the story onto the existing
> two levels — no new levels. It surfaces a story the player currently never sees.

- **Prologue (intro slides):** Greenvale was green. Black Smoke poisoned it,
  spawned monsters, and drove the residents out. In the ruins, a discarded
  Eternal Seed core flickers awake — **Greenie** boots up.
- **Act 1 — The Barren Farm (Level 1):** Greenie meets **Bà Tư** (Old Auntie Tư),
  a stubborn farmer hiding in the fields. She begs Greenie to find **3 Clean
  Energy Cores** to purify the village well. Clearing them reclaims the soil and
  opens the teleport gate. Closing beat: Bà Tư warns the poison flows from the
  factory. *(M8 side thread:* tucked in a back corner is **Ông Sáu**, an old
  herbalist — find him to take the **antidote quest** for his grandson Tí.*)*
- **Interlude — Mr. Bear's Recycling Station:** **Ông Bear** (Mr. Bear), a gruff
  old mechanic, trades upgrades for recycled trash (the existing shop, now framed).
- **Act 2 — The Factory Maze (Level 2):** Greenie finds **2 keycards**, breaches
  the core room, and destroys the boss. *(M8:* the **fleeing worker is Tí**, now
  found **collapsed** at the entrance — saving him with the antidote is what
  triggers his hazard warning + a keycard reward.*)*
- **Epilogue (ending slides):** the machine falls, water runs clean, plants
  bloom, residents return — Greenie stays to watch over a green Greenvale.
  *(M8: if Tí was saved, a warm **Tí ↔ Ông Sáu reunion** slide is added.)*

### Cast

| Character | Role |
|---|---|
| **Greenie** | The player — cleanup robot. |
| **Bà Tư** | Act 1 farmer NPC; gives the well-purification mission + the factory hook. |
| **Ông Bear** | Recycling-station shopkeeper; *(M9)* now the **hub-keeper** — runs the shop and the **crafting bench**. |
| **Ông Sáu** | *(M8)* Act 1 herbalist hidden in a back corner of the farm; the **side-quest giver** you must seek out. Brews the antidote and rewards it on turn-in. |
| **Tí** | *(M8)* Ông Sáu's reckless grandson — **this is the "fleeing worker"**, now named. Found **collapsed/poisoned** at the Level 2 entrance; the **rescue target** you save with the antidote, after which he warns of the hazards and hands over a keycard. |
| **Bé Mây** | *(M9)* S1 child; **lost-pet** side quest; comic relief. Moves to the hub once helped. |
| **Ông Tài** | *(M9)* S1 old fisherman by the toxic pond; **pond-cleanse** quest → rewards a **Portal Shard**. |
| **Cô Lan** | *(M9)* ex-Black-Smoke worker / **informant**; S2 lore arc that surfaces the Director's plan + unlocks the Portal-Shard recipe. |
| **Director Black Smoke** | Unseen antagonist; a pre-boss taunt slide *(optional)*; *(M9)* fleshed out via the lore-note arc. |

## 3. Levels

### Level 1 — "Nông Trại Hoang Hóa" / The Barren Farm  ⟵ *current build target*

- **Setting:** Valley outskirts. Gray dead soil, withered plants, ponds turned
  toxic purple.
- **Hazards:**
  - **Toxic mud pools (purple):** walking in them = **−50% move speed**.
  - **Rusty debris piles:** solid obstacles forcing detours.
- **Enemy (simple AI):** **Plastic Slime** — blobs that **patrol to random points**
  near junk piles (intermittent random-destination wander). Touching the player
  deals contact damage.
- **Objective:** Collect **3 Clean Energy Cores** hidden in old wooden chests to
  purify the water and open the teleport gate to Level 2.

### Level 2 — "Mê Cung Nhà Máy" / The Factory Maze

- **Setting:** Inside the Black Smoke core factory. Iron-brick floors, tangled pipes.
- **Hazards:** Sweeping **toxic lasers** on a timed cycle; **manhole traps** that
  open and close.
- **Enemy (advanced AI):** **Pollution Fly-Bot** — hovering drone. When Greenie
  enters its sight (`Physics2D.OverlapCircle` / `Vector2.Distance`), it chases and
  fires toxic smog orbs at the player.
- **Boss:** **"Mega-Smog" Destruction Machine** — stationary center-of-room boss
  that fires bullets in 8 directions and spawns random toxic-gas zones on the
  floor. Demands constant dodging.

## 4. Core gameplay & features

### Controls / UI (basic)

- **W/A/S/D** move up/left/down/right. The Animator swaps the 4-direction
  animation (Up/Down/Left/Right) like Stardew Valley.
- **J** fires the **Seed** projectile (the eco-cleanup weapon).
- **E** interacts with NPCs / chests.
- **HUD:** HP bar + collected-trash count, top of screen.

### Support items

- **Pure Spring Water:** instantly restores **2 HP**.
- **Green Sprout Energy Drink:** **+50% move speed for 8 seconds**.

### Shop area (advanced) — "Trạm Tái Chế Của Ông Bear" / Mr. Bear's Recycling Station

A safe scene/zone. Spend collected bottles/trash to buy **permanent upgrades** via
an NPC: increase max HP, increase base move speed, or upgrade the Seed gun to a
wider spread.

### Autoplay (advanced) — "Auto-Clean"  ❌ CUT — will NOT be implemented

> **Descoped (2026-06-09).** The team decided to drop the Autoplay feature for
> this submission. Kept here for reference only — do not build it.
>
> *Original concept:* a UI toggle that auto-pathfinds to the nearest Energy Core
> and `Raycast2D`-scans around Greenie to auto-face and auto-fire at any monster
> blocking the path.

## 4.5 Narrative & mission systems (M7 — planned)

All four pieces are lightweight and reuse existing systems (`GameManager` events,
`IInteractable`/`PlayerInteractor`, the HUD canvas). Keep player-facing text in
**Vietnamese**.

- **Mission / objective tracker.** A HUD widget showing the active mission title +
  a sub-objective checklist (e.g. `Tìm Lõi Năng Lượng (1/3)`, `Mở cổng dịch
  chuyển`). It **listens** to existing events — `OnCoresChanged`,
  `OnAllCoresCollected`, boss `OnDefeated` — and never polls. Per-scene objective
  data lives on one small scene component (a serialized list of objectives, each
  with a label + a completion source). Replaces nothing; sits alongside the HP/
  trash HUD.
- **Dialogue system.** A reusable `DialogueRunner` UI panel (speaker name + line,
  advance with E/Space, optional portrait). NPCs are `IInteractable`s that hand a
  `DialogueLine[]` to the runner on E. Movement/shooting pause while a line is up.
- **Cutscene / story slides.** A `StorySequence` that plays full-screen slides
  (text + optional sprite), advanced by a key, then loads a target scene. Used for
  the **Intro** (before Level 1) and **Ending** (after the boss). Can reuse the
  dialogue panel styling.
- **NPCs.** `Bà Tư` (Act 1 quest-giver), `Ông Bear` (existing shop, +flavor),
  optional fleeing worker (Act 2). Each is a tagged scene object with a collider,
  a "Nhấn E" prompt, and a `DialogueLine[]`. Quest-givers may flip an objective or
  unlock a beat on first talk.

### Scene & story flow (M7)

```
MainMenu → Intro (slides) → Level 1 → Level 2 → Boss → Ending (slides) → MainMenu/Credits
                                         (Mr. Bear's shop stays Main-Menu-accessible)
```

Two new scenes — `Intro_Story` and `Ending_Story` — register in Build Settings;
`MenuController.PlayGame` loads the Intro instead of Level 1, and the boss-defeat
path routes to the Ending before returning to the menu.

## 4.6 Quest chain & rescue (M8 — planned)

A full **Stardew-style fetch-and-deliver quest** layered on the existing two
levels — **no new levels**. It delivers two requested mechanics: **(1)** a *find an
NPC → accept a described mission → complete it → return to confirm → receive a
specific item* loop, and **(2)** *use that item to save an NPC*. It **reuses M7
wholesale** (`DialogueRunner`, `ObjectiveTracker`, `IInteractable`/
`PlayerInteractor`, `StorySequence`) and the `PlayerProgress`/`GameSettings`
static-state pattern. Player-facing text stays **Vietnamese**.

### The loop

| Step | Where | What happens |
|---|---|---|
| **1. Find** | L1, back corner | Seek out **Ông Sáu** (unlike Bà Tư he does **not** auto-brief). E → he explains his grandson **Tí** ran into the factory and is being poisoned. |
| **2. Accept** | L1 | Finishing his offer **starts** the quest: tracker adds `Hái Lá Thuốc (0/3)`; the 3 **Lá Thuốc** pickups activate. |
| **3. Complete** | L1 | Collect 3 herbs (tucked past mud / near slimes). 3/3 flips the objective to `Mang lá thuốc về cho Ông Sáu`. |
| **4. Return / turn-in** | L1 | Talk to Ông Sáu at 3/3 → he brews and **grants the `Thuốc Giải Mầm Xanh` (Green Sprout Antidote)** quest item. Objective → `Tìm và cứu Tí trong nhà máy`. |
| **5. Use to rescue** | **L2** | Find **Tí collapsed** at the entrance. E **while holding the antidote** → revive him (item consumed). No antidote → `"Cậu ấy bất tỉnh vì khói độc... mình cần thuốc giải!"`, no progress. |
| **6. Reward** | L2 | Saved Tí thanks Greenie, delivers the **hazard warning** (the old M7 worker line, now earned), and hands over a **keycard**. Sets a "Tí saved" flag for the epilogue reunion slide. |

The item is earned in **L1** but only usable in **L2** — this is the "needed
later" cross-level carry. The quest is **strongly incentivized but not a hard
gate**: the 2 keycards are still independently findable, so skipping the quest
can't soft-lock the main path; it only forfeits Tí's reward and the reunion beat.

### New items

- **`Lá Thuốc` (Medicinal Herb) ×3** — quest pickups in L1. Walk-over or E-grab
  like `Litter`/`Keycard`; each increments a quest herb count (not trash, not cores).
- **`Thuốc Giải Mầm Xanh` (Green Sprout Antidote)** — single-use quest item held in
  inventory, **persists across the L1→L2 scene load**, consumed on rescuing Tí.

### Quest state & systems (planned)

- **`QuestProgress`** (`Systems/`, static, PlayerPrefs — mirrors `PlayerProgress`):
  the single source of truth for the antidote quest. A `QuestStage` enum
  (`NotMet → Offered → HerbsInProgress → HerbsReady → AntidoteHeld → TíSaved`), a
  herb counter, and a `HasAntidote` flag; raises `OnChanged`. Persisting it (vs.
  on the per-scene `GameManager`) is what lets the antidote survive the L1→L2 hop.
- **`QuestGiverNPC`** (`World/`) — Ông Sáu: a `DialogueNPC` whose line set is
  **keyed by `QuestStage`** (offer → on dialogue-done advance to `HerbsInProgress`;
  turn-in branch at `HerbsReady` → grant antidote → `AntidoteHeld`).
- **`QuestItemPickup`** (`Items/`) — the herb: increments `QuestProgress` herb
  count, only counts while the quest is active.
- **`RescueNPC`** (`World/`) — collapsed Tí: an `IInteractable` that gates on
  `QuestProgress.HasAntidote`. On rescue it consumes the antidote, advances to
  `TíSaved`, plays the revive dialogue, swaps to a standing/awake visual, and
  grants the keycard reward.
- **`ObjectiveTracker`** gains a quest-driven row source that listens to
  `QuestProgress.OnChanged` (same "listen, never poll" rule as its event rows).
- **`Ending_Story`** reads the `TíSaved` flag to optionally include the reunion slide.

## 4.7 The 30-minute content layer (M9 — planned)

> **Goal (owner: Khiêm):** stretch a full playthrough from the current sub-10-min
> speedrun to **~30 minutes** by deepening systems — **still exactly two stages, no
> new levels**. Models Stardew Valley loops (inventory, crafting, codex,
> collectibles, backtracking) on top of the action-RPG core. **Pacing is pure
> content — no day/energy clock.** Chosen systems: **central hub portal**,
> **slot-grid inventory with stacks**, **crafting bench**, **collectibles & codex**.
> *(NPC gratitude/affinity meter and a repeatable-bounty **quest board** were both
> considered and **dropped** — side quests are **NPC-given** only.)*
> Player-facing text stays **Vietnamese**.

### 4.7.1 Central hub & two-way travel (replaces the one-way gate)

**Mr. Bear's Recycling Station becomes the central hub** — the connective tissue
that makes backtracking (and therefore the longer loop) possible. It gains a
**Portal Nexus** (`Cổng Nexus`), the existing **shop**, a **crafting bench**, and a
**codex stand**; rescued/freed villagers visibly **gather here** as you progress
(the valley reviving — a Stardew community-center feel).

New flow (supersedes the M7 one-way chain):

```
MainMenu → Intro → HUB ⇄ Stage 1 (Barren Farm)
                    HUB ⇄ Stage 2 (Factory Maze) → Boss → Ending
```

- Each stage has a **Return Portal** (`Cổng Về Trạm`) → back to the hub, available
  the whole time you're in the stage. **This is the missing reverse travel** the
  current one-way `TeleportGate` lacks (Stage 2 → hub → Stage 1).
- The hub **Portal Nexus** has a gate per stage. **Stage 1** opens after the intro.
  **Stage 2 is broken** and must be **powered with 3 `Mảnh Cổng` (Portal Shards)** —
  a mid-game goal that forces engagement with side content (see 4.7.4). Shards come
  from: clearing Stage 1's wells, the Stage-1 mini-boss, and Ông Tài's pond quest.
- Migration note: the L1 `TeleportGate` is **re-pointed to the hub** (not directly
  to L2); the hub's Stage-2 portal is the new way into the Factory once powered.

### 4.7.2 Inventory & expanded items (Khiêm's system)

A **slot-grid bag with stacks** (Stardew/Minecraft style), opened with **I / Tab**,
plus a **1–4 quick-use hotbar** for consumables. Pickups now **go into the bag**
(stack if possible) instead of applying instantly; consumables are used from the
bag or hotbar. The bag **persists across scenes** (same spirit as `PlayerProgress`).

Item taxonomy:

| Category | Items (VN) | Notes |
|---|---|---|
| **Consumable** | Bình Nước Suối (+2 HP), Nước Tăng Lực Mầm Xanh (+50% spd 8s), **Lá Chắn Mầm** (temp shield), **Bom Hạt Giống** (throw → AoE that *clears trash* + damages), **Bình Hồi Phục Lớn** (full heal, crafted) | existing two + 3 new; used from hotbar |
| **Material** | **Chai Nhựa** (Plastic Bottle), **Mảnh Kim Loại** (Scrap Metal), **Lá Thuốc** (Herb), **Tinh Thể Năng Lượng** (Energy Shard, rare) | stack to 99; drops from trash/enemies/nodes; feed crafting + quests |
| **Key / Quest** | Thẻ Từ (Keycards), Thuốc Giải Mầm Xanh (antidote), **Mảnh Cổng** (Portal Shard ×3), per-quest items | non-stacking key items; don't clutter the bag grid |

### 4.7.3 Crafting bench (`Bàn Chế Tạo`, at the hub)

Spend **materials** → **consumables/ammo/upgrade mats**. Recipes **unlock** via
quests/NPCs (learn-by-doing). Starter recipe table (tunable):

| Output | Cost | Unlocked by |
|---|---|---|
| Bình Nước Suối | 3× Chai Nhựa | default |
| Nước Tăng Lực Mầm Xanh | 2× Lá Thuốc + 1× Chai Nhựa | default |
| Lá Chắn Mầm (shield) | 5× Mảnh Kim Loại | Ông Bear quest "Tái Chế Nâng Cao" |
| Bom Hạt Giống | 3× Mảnh Kim Loại + 2× Chai Nhựa | Ông Bear quest |
| Bình Hồi Phục Lớn | 2× Lá Thuốc + 1× Tinh Thể Năng Lượng | Ông Sáu, after the antidote quest |
| Mảnh Cổng (Portal Shard) | 4× Tinh Thể Năng Lượng | Cô Lan, mid-game |

The crafted **Mảnh Cổng** path means a player short on quest-drop shards can still
power the Stage 2 portal by grinding Energy Shards — **no soft-lock**.

### 4.7.4 Side quests (NPC-given)

**Story side quests** (multi-step, **NPC-given** — there is no quest board) — the M8
antidote quest plus new ones:

| # | Quest (VN) | Giver / Stage | Reward |
|---|---|---|---|
| 1 | Liều Thuốc Giải *(M8)* | Ông Sáu / S1 → S2 | save Tí, keycard, reunion slide |
| 2 | Người Bạn Nhỏ Của Bé Mây | Bé Mây / S1 | find her lost robot-pet (slime-guarded corner) → recipe + materials; Bé Mây moves to hub |
| 3 | Làm Sạch Ao Độc | Ông Tài / S1 | clean the toxic pond (destroy trash / use a Filter) → **1× Mảnh Cổng** |
| 4 | Tin Tức Từ Bên Trong | Cô Lan / S2 | collect 3 lore notes in the factory → unlocks shortcut + the Portal-Shard recipe + antagonist lore |
| 5 | Tái Chế Nâng Cao | Ông Bear / hub | bring 10 Scrap + 10 Bottles → unlocks advanced craft recipes |

### 4.7.5 Collectibles, codex & the cleaning loop (incl. Anh's effect)

- **Anh's mechanic is now a core loop:** destroying trash/litter triggers a **radial
  ground-cleanse** (a `ReclamationPatch`-style reveal around the trash) **and** (a)
  raises a per-stage **`Độ Sạch Thung Lũng` (Cleanliness %)**, (b) can **drop a
  material** (bottle/scrap). So cleaning = visual payoff + progression + resources.
- **`Sổ Tay Greenie` (Codex/Journal)**, opened from the bag/menu, has three tabs:
  - **`Hồ Sơ Quái` (Bestiary):** an entry per enemy, filled on first kill.
  - **`Mẩu Nhật Ký` (Lore Notes):** 8–10 hidden note pickups across both stages that
    reveal Black Smoke's plot, the village's past, and Greenie's origin — **more
    storyline** with no new scenes.
  - **`Độ Sạch` (Cleanliness):** the % tracker per stage; hitting **50% / 100%**
    grants rewards (materials, a Portal Shard, a cosmetic "valley bloom").
- A **Stage-1 mini-boss — `Slime Chúa` (Slime King)** — guards a Portal Shard in a
  polluted grove, adding a combat beat and a reason to gear up via crafting first.

### 4.7.6 Cast additions & story threads

New NPCs (reuse `DialogueNPC`/`QuestGiverNPC`; they **relocate to the hub** once
helped, repopulating it):

| NPC | Where | Role |
|---|---|---|
| **Bé Mây** | S1 | child; lost-pet side quest; comic relief + heart |
| **Ông Tài** | S1 (pond) | old fisherman; pond-cleanse quest → Portal Shard |
| **Cô Lan** | hub → S2 | ex-Black-Smoke worker / informant; factory intel + lore arc, surfaces **Director Black Smoke**'s plan |

Story deepening, no new scenes: the **hub repopulates** as villagers are freed; the
**lore-note arc** exposes the antagonist; **Portal Shards** frame a mid-game "repair
the broken gate" goal.

### 4.7.7 ~30-minute time budget (estimate)

| Beat | Est. |
|---|---|
| Intro + first hub onboarding (shop/bench/codex tour) | ~2 min |
| Stage 1 main (3 cores → purify well → unlock) | ~5 min |
| Stage 1 side (antidote herbs, Bé Mây pet, pond cleanse, Slime King, gather to 50% clean) | ~7 min |
| Hub loops across visits (craft, shop, quest turn-ins) | ~4 min |
| Stage 2 main (2 keycards + power the portal with 3 shards + boss) | ~7 min |
| Stage 2 side (Cô Lan intel, rescue Tí, lore notes, clean) | ~4 min |
| Ending (+ reunion if Tí saved) | ~2 min |
| **Total** | **~31 min** (100%-clean adds replay padding) |

### 4.7.8 Planned scripts & reuse

- **Inventory:** `Items/ItemDef` (id, VN name, icon, category, max-stack, use-effect),
  `Systems/Inventory` (persistent list of stacks; `OnChanged`), `UI/InventoryUI`
  (slot grid + drag), `UI/Hotbar`. Existing pickups refactor to a generic
  `Items/ItemPickup` that **adds to `Inventory`** instead of instant effect.
- **Crafting:** `Items/CraftingRecipe` defs + `Systems/Crafting` + `UI/CraftingUI`
  (at a `World/CraftingBench` interactable); recipe-unlock flags on `QuestProgress`.
- **Quests:** generalize M8's `QuestProgress` into a `Systems/QuestLog` (multiple
  active quests + states) + `UI/QuestLogUI`, for the **NPC-given** story side quests.
- **Codex & cleaning:** `Systems/Codex` (bestiary + lore-note flags + per-stage
  cleanliness); `World/LoreNote` pickup; `World/GroundCleanser.CleanRadius(pos,r)`
  called by `Litter`/trash destruction (Anh's effect) → reveal + `Codex` % + drop.
- **Hub & portals:** `World/PortalNexus` + `World/StagePortal` (two-way, gated on
  `PortalShard` count) + `World/ReturnPortal`; hub villager-spawn driven by quest flags.
- **Persistence:** `Inventory`/`QuestLog`/`Codex` persist across scenes like
  `PlayerProgress` (PlayerPrefs-serialized or a `DontDestroyOnLoad` manager); resettable on New Game.

## 5. Technical guidance from the brief

- Build maps with **Unity Tilemap**: a flat **Grid**, paint Grass/Dirt ground
  tiles, place obstacles (cliffs, pipes) carrying a **Tilemap Collider 2D**.
  Top-down means no "falling into pits" risk like side-scrollers — collisions are
  clean.
- Implement 4-direction Animator state switching (Up/Down/Left/Right) for the
  character.

## 6. Scope notes for implementation order

The roadmap ([roadmap.md](roadmap.md)) sequences this. High level:
1. ✅ **Level 1 vertical slice** (movement, shooting, one enemy, mud, cores, HUD, win).
2. ✅ Items + pickups.
3. ✅ Level 2 (advanced AI, boss).
4. ✅ Shop + upgrades (persistent).
5. ~~Autoplay mode.~~ — **cut (out of scope)**
6. ✅ **Narrative & mission layer (M7)** — story slides, objective tracker, NPC
   dialogue, intro/ending.
7. **Quest chain & rescue (M8)** — find Ông Sáu → antidote fetch quest → save Tí
   with the item; see §4.6.
8. **30-minute content layer (M9)** — central hub + two-way portals, slot-grid
   inventory, crafting bench, NPC-given side quests, codex/collectibles + the
   cleaning loop, new NPCs; see §4.7. ← *now (stretches the playthrough to ~30 min,
   still two stages)*
