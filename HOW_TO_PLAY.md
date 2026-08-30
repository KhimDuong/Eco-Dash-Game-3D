# How to Play — Eco-Dash 3D: Biệt Đội Giải Cứu Xanh

> **For players, testers, and graders.** This single page explains what the game
> is, how to run it, every control, every mechanic, and how to reach and test
> each feature. The on-screen text is **Vietnamese**; this guide gives the exact
> Vietnamese labels in **"quotes"** so you can match them instantly.
>
> *Hướng dẫn dành cho người chơi, người kiểm thử và giảng viên chấm điểm. Trò chơi
> hiển thị tiếng Việt; tài liệu này chú thích nhãn tiếng Việt trong "ngoặc kép".*
>
> This document is the **source of truth** for the in-game tutorial popup
> (press **H** anytime to read it in-game — see §14).

---

## 1. What is Eco-Dash?

Eco-Dash 3D is a **3D top-down action-adventure / RPG** (think *Tunic* or
*Death's Door* — a fixed three-quarter camera looking down on a 3D world) built in
**Unity 6 / URP 3D**. It is the 3D remake of the finished 2D game, and plays the
same: the camera angle changed, the rules did not. You play
**Greenie**, a small eco-cleanup robot reclaiming a polluted valley from the
**"Black Smoke" / "Khói Đen"** chemical corporation. You clear waste, fight
pollution monsters, collect energy cores, help villagers with quests, craft and
use items, save a poisoned boy named **Tí**, beat a factory boss, and spend
recycled trash on permanent upgrades at Mr. Bear's shop.

**Game flow:**

```
Main Menu  →  Intro slides  →  Level 1 (Barren Farm)  ⇄  Central Hub (Mr. Bear's Station)  ⇄  Level 2 (Factory Maze)  →  Boss  →  Victory
```

Travel is now **two-way**: finishing Level 1 sends you to the **central hub**,
and from the hub you portal into either stage and back again at will.

---

## 2. How to run it

**Option A — in the Unity Editor (recommended for grading):**
1. Open the project in **Unity 6 (6000.3.16f1)**. The first open takes a few
   minutes while it imports the models and bakes shaders.
2. Open the scene [`Assets/_Scenes/MainMenu.unity`](Assets/_Scenes/MainMenu.unity).
3. Press the **▶ Play** button at the top of the editor.
4. Click **"Chơi Mới"** (New Game) to start a fresh run (this wipes previous
   progress), or **"Tiếp Tục"** (Continue) to resume where you left off.

**Option B — a built executable:** `File → Build And Run`.

> The game uses the **keyboard only**. Click the Game view once so it has focus,
> then use the keys below. On your very first run, a **tutorial popup** appears
> automatically (§14).

---

## 3. Controls (điều khiển)

| Key | Action | Vietnamese |
|-----|--------|-----------|
| **W / A / S / D** | Move around the ground plane — **W is always "away from the camera"**, whichever way Greenie is facing | Di chuyển |
| **P** | Switch between the fixed ¾ view and **first person**, and back. In first person, move the **mouse** to look around; W follows where you are looking | Đổi góc nhìn |
| **J** | Shoot a **Seed** projectile in the direction you're facing (hold to auto-fire) | Bắn hạt mầm |
| **E** | Interact — open chests, grab keycards, talk to NPCs, use portals & the crafting bench, read lore notes | Tương tác |
| **1 / 2 / 3 / 4** | Use the consumable in that **hotbar** slot (bottom of screen) | Dùng vật phẩm nhanh |
| **I** or **Tab** | Open / close the **bag** ("Túi Đồ") — items & materials | Túi đồ |
| **Q** | Open / close the **quest log** ("Nhật Ký Nhiệm Vụ") | Nhật ký nhiệm vụ |
| **C** | Open / close the **codex** ("Sổ Tay Greenie") — bestiary, lore, cleanliness | Sổ tay |
| **H** | Open / close the **how-to-play tutorial** popup | Hướng dẫn |
| **Esc** | Pause / unpause (opens the pause menu) | Tạm dừng |

There is **no jump and no gravity as a mechanic**. The world is 3D, but everything
you fight and collect is reached by walking to it on the ground; height is
scenery, never a platform.

The ¾ view is the game's default and is fixed — no yaw, no zoom — so W never
stops meaning "up the screen" there. **P** is the one exception: it drops you to
Greenie's eye level, where the mouse turns the camera and the movement keys turn
with it. Seeds still fly flat along the ground in first person, so looking up or
down changes what you see, never where you shoot; the centre dot marks where a
Seed will go.

---

## 4. The HUD & panels (on-screen display)

**Always-on HUD** (top-left, during a level):
- **Green HP bar** — Greenie's health (**"thanh máu"**). Drains on damage, refills
  from healing items.
- **Objective counter** — Level 1 shows **"Lõi NL: 0/3"** (Energy Cores); Level 2
  shows **"Thẻ từ: 0/3"** (security keycards). When complete it adds a hint
  (e.g. **"Cửa Boss đã mở!"** — "Boss door is open!").
- **"Rác: N"** — your **recycled-trash wallet** (shop currency). This number is
  **permanent** — it carries across levels and play sessions.

**Hotbar** (bottom-center): four slots (**1–4**) that auto-fill with the first
four consumables in your bag. Press the number to use one.

**Pop-up panels** (toggle keys, overlay the game):
- **Bag "Túi Đồ" (I / Tab)** — a slot grid of materials & consumables, plus a
  bottom strip for **key / quest items**. Click a consumable to use it.
- **Quest log "Nhật Ký Nhiệm Vụ" (Q)** — every quest you've started, with its
  giver, description, counter and status ("Đang làm" / "Hoàn thành").
- **Codex "Sổ Tay Greenie" (C)** — three tabs: **"Hồ Sơ Quái"** (bestiary),
  **"Mẩu Nhật Ký"** (lore notes) and **"Độ Sạch"** (per-stage cleanliness %).

---

## 5. Core mechanics — what hurts you, what helps you

### Taking damage
- **Enemies deal contact damage** when they touch you. On a hit you are
  **knocked back**, the camera **shakes**, the world **lurches for a moment**
  (a brief hit-stop), Greenie **flashes red**, and a hurt sound plays. Killing
  something does the same, so a hit that lands is felt as well as seen.
- After any hit you get **~0.8 s of invulnerability** — use it to escape.
- If the HP bar empties, you lose (see §12).

### Hazards
- **Toxic Mud** (purple pools, **"bùn lầy hóa chất"**) — **only slows you to 50%
  speed**; it does **NOT** deal damage.
- **Toxic Gas Zones** (Level 2 / boss) — circles that **flash a harmless warning
  first**, then turn toxic-green and deal damage while you stand in them.
- **Sweeping lasers** and **manhole traps** (Level 2) — timed factory hazards.

### Your weapon
- **J fires a Seed** in your facing direction. Seeds destroy enemies. The **shop
  spread upgrade** makes you fire **1 → 3 → 5 → 7 seeds** in a fan.

### Cleaning trash → materials & cleanliness
- **Walk over litter** to clear it. Each piece rewards **crafting materials**
  (see §6), **greens the ground around it**, and raises that stage's
  **"Độ Sạch"** (Cleanliness %) in the codex. Reaching **50%** and **100%**
  grants rewards — clearing a whole stage is worth a **Portal Shard**.
- Both stages have trash to clear: **8 pieces** in the Barren Farm and **10** on
  the factory floor, so either meter can be taken to 100%.
- A **Seed Bomb** clears every piece of trash in its blast as well as damaging
  what it hits.

### Consumables (used from the hotbar or bag)
| Item (Vietnamese) | Effect |
|-------------------|--------|
| **Bình Nước Suối** (Spring Water) | +2 HP |
| **Nước Tăng Lực Mầm Xanh** (Energy Drink) | +50% move speed for 8 s |
| **Lá Chắn Mầm** (Sprout Shield) | Temporary damage shield (~6 s) |
| **Bom Hạt Giống** (Seed Bomb) | Throwable AoE — clears trash + damages enemies |
| **Bình Hồi Phục Lớn** (Large Heal) | Full heal |

### Audio
- **Background music plays continuously**, and — unlike the 2D build — it does not
  restart when you change scene. Portalling between the farm, the hub and the
  factory, or restarting after a death, leaves the track running; you should never
  hear it begin again except when you quit to the menu and start over.
- Short **SFX** fire on shooting, pickups, damage, enemy death, cleaning trash,
  crafting, portals, and win/lose. **Sounds fade with distance from Greenie**, so
  a slime dying across the field is quieter than one at your feet and something
  far off-screen is silent altogether.
- All volumes are adjustable in **Settings** (§11).

---

## 6. Inventory, crafting, codex & the quest log (M9)

These four systems persist across every scene and reset only on **"Chơi Mới"**.

### Bag / inventory — "Túi Đồ" (I / Tab)
Stackable slot grid. **Materials** dropped by cleaning trash and beating enemies:
- **Chai Nhựa** (Plastic Bottle), **Mảnh Kim Loại** (Scrap Metal),
  **Lá Thuốc** (Medicinal Herb), **Tinh Thể Năng Lượng** (Energy Shard).
Consumables sit here too (click to use). **Key / quest items** (antidote,
keycards, portal shards) live in the separate bottom strip.

### Crafting — the **"Bàn Chế Tạo"** (Crafting Bench) in the hub (press **E**)
| Craft | Recipe | Unlocked by |
|-------|--------|-------------|
| **Bình Nước Suối** | 3× Chai Nhựa | default |
| **Nước Tăng Lực Mầm Xanh** | 2× Lá Thuốc + 1× Chai Nhựa | default |
| **Lá Chắn Mầm** | 5× Mảnh Kim Loại | Ông Bear's "Tái Chế Nâng Cao" quest |
| **Bom Hạt Giống** | 3× Mảnh Kim Loại + 2× Chai Nhựa | Ông Bear's quest |
| **Bình Hồi Phục Lớn** | 2× Lá Thuốc + 1× Tinh Thể Năng Lượng | Ông Sáu (after the antidote quest) |
| **Mảnh Cổng** (Portal Shard) | 4× Tinh Thể Năng Lượng | Cô Lan's factory quest |

The craftable **Portal Shard** is an anti-soft-lock safety net (see §9).

### Codex — "Sổ Tay Greenie" (C)
Fills in automatically: a **bestiary** entry the first time you meet each enemy,
**lore notes** you find in the world, and each stage's **cleanliness %**.

### Quest log — "Nhật Ký Nhiệm Vụ" (Q)
Tracks the main antidote quest plus the four NPC side quests, with live counters
and status. **There is no quest board** — every quest comes from talking to an NPC.

---

## 7. The main quest — save Tí (find an NPC → mission → item → rescue)

This is the game's Stardew-style quest loop:

1. **Find the giver.** In Level 1, seek out **Ông Sáu** (Uncle Sáu, the herbalist)
   and press **E**. He asks for medicine to cure his grandson **Tí**.
2. **Do the mission.** Gather **3× Lá Thuốc** (Medicinal Herb) scattered around
   Level 1. The quest log counter (**Q**) tracks `0/3 → 3/3`.
3. **Turn it in.** Return to **Ông Sáu**; he brews and gives you the **"Thuốc Giải
   Mầm Xanh"** (Green Sprout Antidote) — a key item that persists into Level 2.
4. **Use the item to save the NPC.** In Level 2 you find **Tí** unconscious.
   Interact with the antidote in your bag to **cure him**. Saving Tí also grants
   the **3rd security keycard** you need for the boss door.

---

## 8. Side quests & villagers (M9)

Four optional NPC quests — accept them by talking (**E**), track them in the log
(**Q**). Finishing a villager's quest makes them reappear at the reviving hub.

| Quest ("...") | Giver | What to do | Reward |
|---------------|-------|-----------|--------|
| **"Người Bạn Nhỏ Của Bé Mây"** | Bé Mây | Find her lost pet robot in a valley corner (a Slime guards it) | — |
| **"Làm Sạch Ao Độc"** | Ông Tài | Clean the trash around the toxic pond | **1× Mảnh Cổng** |
| **"Tin Tức Từ Bên Trong"** | Cô Lan | Collect **3 lore notes** in the factory | Unlocks the **Portal Shard** recipe |
| **"Tái Chế Nâng Cao"** | Ông Bear | Turn in **10× Mảnh Kim Loại + 10× Chai Nhựa** | Unlocks **advanced crafting** recipes |

---

## 9. The levels, objectives & travel

### Level 1 — "Nông Trại Hoang Hóa" / The Barren Farm
- **Goal:** collect **3 Clean Energy Cores** (hidden in wooden chests — walk up and
  press **E**), then step into the **teleport gate** to the north. The gate now
  sends you to the **central hub**, not straight to Level 2.
- **Enemies:** **Plastic Slime** (wanders near junk piles); and the **Slime King
  ("Slime Chúa")** — a **mini-boss** that wakes when you get close, chases you,
  **splits into smaller slimes at half health**, and **drops a Portal Shard**
  when defeated.
- **Hazards:** toxic-mud pools (slow you), rusty debris (solid walls).
- Do **Ông Sáu's** antidote quest and the **Bé Mây / Ông Tài** side quests here.

### Central Hub — "Trạm Tái Chế Của Ông Bear" (Mr. Bear's Recycling Station)
Your safe home base (see §10). It holds the **shop**, the **crafting bench**, the
**codex stand**, and the **Portal Nexus** with gates to both stages. Rescued
villagers reappear here as you complete their quests.

### Level 2 — "Mê Cung Nhà Máy" / The Factory Maze
- **Goal:** collect **3 security keycards ("Thẻ từ")** to open the **boss door**.
  Two are found in the maze; the **third is granted when you cure Tí** (§7).
- **Enemy:** **Pollution Fly-Bot** — a drone that genuinely **hovers above the
  floor**, patrols, needs **line of sight** to notice you (a wall between you is
  real cover), then chases, keeps its distance, and fires toxic smog orbs at your
  chest. Your Seeds fly flat and still reach it.
- **Hazards:** toxic-gas zones, sweeping lasers, manhole traps.
- **Boss:** **"Mega-Smog" Destruction Machine** — sprays a **ring of 8 smog orbs**
  across the floor and spawns **toxic-gas zones**. Below **35% health** it
  **enrages**: it turns visibly red and the ring grows to **12 orbs**. Keep moving,
  dodge, and shoot it down; its **health bar spans the top of the screen**.
  Beating it wins the game.
- Do **Cô Lan's** lore-note side quest here.

### Travel & portals (two-way)
- **Level 1 north gate → hub** (after 3 cores).
- **Hub Portal Nexus → Level 1 or Level 2** (walk in / press **E**).
- **Return Portal ("Cổng Về Trạm")** in each stage → **back to the hub**.
- A **shard-gated shortcut portal** in Level 1 can jump straight to Level 2 if you
  spend a **Portal Shard** — otherwise use the hub. (Short on shards? Beat the
  Slime King, finish Ông Tài's quest, or craft one via Cô Lan's recipe.)

---

## 10. The Hub / Shop — "Trạm Tái Chế Của Ông Bear"

A safe green zone. Reach it from Level 1's north gate or any Return Portal.

**Shop — spend trash ("Rác") on permanent upgrades:**
1. Walk Greenie up to **Mr. Bear** (the bear NPC) and press **E**. A **"Nhấn E"**
   prompt appears when you're close enough.
2. Each upgrade row shows its **current tier** (**"Cấp X/3"**) and **cost**. Click
   **"MUA"** (Buy) to purchase; the button greys out when you can't afford it or
   it's maxed (**"TỐI ĐA"**).
3. Press **"ĐÓNG"** to close.

| Upgrade | Effect per tier | At max (tier 3) |
|---------|-----------------|-----------------|
| **Max HP** ("Máu tối đa") | +2 max health | +6 HP |
| **Move Speed** ("Tốc độ") | +12% speed | +36% speed |
| **Seed Spread** ("Số hạt") | more seeds per shot | fires **7 seeds** in a fan |

Costs are `10 → 18 → 30` trash per tier. Upgrades are **saved permanently** and
apply automatically when you spawn into a level.

Also at the hub: the **Crafting Bench** (§6, press **E**) and the **codex stand**.

---

## 11. Settings (Cài Đặt) — audio options

Open **Settings** two ways: from the **Main Menu** → **"Cài Đặt"**, or **during a
level** → **Esc** → **"Cài đặt"** in the pause menu.

The panel (**"CÀI ĐẶT"**) has **"Âm lượng tổng"** (Master volume), **"Nhạc nền"**
(Music volume), **"Tắt tiếng"** (Mute), and **"ĐÓNG"** (close). All settings are
**saved automatically** and persist across scenes and restarts.

**Âm lượng tổng** controls everything — music and sound effects alike.
**Nhạc nền** scales only the music on top of that, so you can turn the soundtrack
down and keep the combat audible. Both take effect while you drag them.

---

## 12. Pausing, winning, losing

### Pause menu (press Esc in a level)
Time freezes and you get: **"Tiếp tục"** (Resume), **"Chơi lại"** (Restart level),
**"Cài đặt"** (Settings), **"Về Menu"** (quit to Main Menu).

### Win / Lose
- **Win:** reaching the gate (L1) or defeating the boss (L2) shows a victory panel
  with **Restart / Next / Menu** and a fanfare.
- **Lose:** if your HP hits zero, a defeat panel appears with **Restart / Menu**.

---

## 13. Quick checklist for testers / graders

A pass that touches every feature:

1. **Main Menu** — confirm **"Chơi Mới / Tiếp Tục / Cài Đặt / Thoát"** work. Open
   **Cài Đặt**, drag both volumes and toggle mute **while the menu music plays**,
   so you can hear each one take effect, then close.
2. **Tutorial** — on a fresh **Chơi Mới**, the how-to-play popup appears; page
   through it with **Tiếp theo / Quay lại**, then close. Re-open anytime with **H**.
3. **Level 1** — move (WASD), shoot (J), walk a purple **mud pool** (slow, no
   damage), open chests (E) for **3 cores**, clean trash and watch **"Rác"** and
   materials rise. Fight the **Slime King** and grab its **Portal Shard**.
4. **Quests** — talk to **Ông Sáu** (E), gather **3 Lá Thuốc**, turn in for the
   **antidote**; check it in the log (**Q**). Accept **Bé Mây / Ông Tài** quests.
5. **Panels** — open the bag (**I/Tab**), quest log (**Q**), codex (**C**); use a
   consumable from the **1–4** hotbar.
6. **Hub** — take the north gate to the hub. **Buy** a shop upgrade (E at Mr.
   Bear), **craft** at the bench (E), then **portal** to Level 2.
7. **Level 2** — fight **Fly-Bots**, collect **2 keycards**, **cure Tí** with the
   antidote for the **3rd keycard**, open the boss door, and beat **Mega-Smog**.
8. **Return trip** — use a **Return Portal** to go back to the hub mid-run.
9. **Audio** — listen across a portal: the music should **carry straight through
   the scene change** without restarting. Clear a piece of trash next to you and
   one at the far end of the field, and note that the second is quieter.
10. **Cleanliness** — clear all the trash in a stage and check **"Độ Sạch"** in the
    codex (**C**) reads **100%**, paying out its reward.

**Fresh save:** **"Chơi Mới"** wipes run progress, inventory, quests, codex, trash
and upgrades. Progress and settings live in Unity **PlayerPrefs**; to fully wipe,
use *Edit → Clear All PlayerPrefs* in the editor.

---

## 14. The in-game tutorial popup

A **multi-page tutorial** built from this document. It appears **automatically the
first time you enter a fresh game** (each **"Chơi Mới"** re-arms it) and can be
opened **anytime with the H key**. Page through it with **"Tiếp theo"** / **"Quay
lại"**, jump around with the page dots, and dismiss with **"Bỏ qua"** / **Esc** / the
last page's **"Bắt đầu chơi!"** button. While it's open the game pauses.

---

## 15. Credits & assets

This is a **university course project** (not published or monetized). All borrowed
art and audio are credited in [CREDITS.md](CREDITS.md); placeholder art is clearly
labeled there. The 3D models come from five CC0 low-poly packs; the music and sound
effects are carried over from the 2D build. Design details live in
[.claude/docs/game-design.md](.claude/docs/game-design.md) and
[.claude/docs/architecture.md](.claude/docs/architecture.md), and the 2D original
this remakes is at `d:\Y4-Sem3\Eco-Dash-Game`.
