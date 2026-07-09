# Glossary — Vietnamese ⇄ English game terms

The Vietnamese brief ([../../Eco-Dash-Game.md](../../Eco-Dash-Game.md)) is the
creative source of truth. Code uses the English term; UI text shown to players is
**Vietnamese**. Use this table to keep naming consistent.

| Vietnamese | English (code term) | Notes |
|------------|---------------------|-------|
| Greenie | `Greenie` / Player | The cleanup-robot hero |
| Hạt mầm vĩnh cửu | Eternal Seed (core) | Greenie's power source (lore) |
| Hạt mầm (đạn) | `Seed` projectile | The player's weapon shot (`SeedProjectile`) |
| Lõi Năng Lượng Sạch | Clean Energy Core | `EnergyCore` sprite, now held inside chests (×3) |
| Rương gỗ cũ | Old wooden chest | `Chest` — E-to-open, holds a core (`IInteractable`) |
| Nhấn E | "Press E" | World prompt shown over the nearest interactable |
| Vùng hồi sinh | Reclamation patch | `ReclamationPatch` — dead soil → lush green on reveal |
| Cổng dịch chuyển | Teleport gate | `TeleportGate` — arms at 3 cores, walk in to advance |
| Quái Rác Nhựa | Plastic Slime | `PlasticSlime` — Level 1 patrol enemy |
| Fly-Bot Ô Nhiễm | Pollution Fly-Bot | Level 2 chasing drone enemy |
| Cỗ Máy Hủy Diệt "Mega-Smog" | "Mega-Smog" boss | Level 2 boss |
| Bùn lầy hóa chất (tím) | Toxic Mud | `ToxicMud` hazard, −50% speed |
| Đống đổ nát rỉ sét | Rusty debris | Solid obstacle tiles |
| Nông Trại Hoang Hóa | The Barren Farm | Level 1 (`Level1_BarrenFarm`) |
| Mê Cung Nhà Máy | The Factory Maze | Level 2 (`Level2_FactoryMaze`) |
| Black Smoke | Black Smoke | Antagonist corporation |
| Greenvale 2026 | Greenvale 2026 | The town/setting |
| Bình Nước Suối Tinh Khiết | Pure Spring Water | Item: +2 HP instantly |
| Nước Tăng Lực Mầm Xanh | Green Sprout Energy Drink | Item: +50% speed for 8s |
| Trạm Tái Chế Của Ông Bear | Mr. Bear's Recycling Station | Shop scene/zone |
| Ông Bear | Mr. Bear | Shop NPC |
| Bà Tư | Old Auntie Tư | Level 1 farmer NPC / quest-giver (M7) |
| Ông Sáu | Uncle Sáu | Level 1 herbalist; M8 side-quest giver (`QuestGiverNPC`) you must find |
| Tí | Tí | Ông Sáu's grandson = the (now-named) fleeing worker; M8 rescue target (`RescueNPC`) in Level 2 |
| Lá Thuốc | Medicinal Herb | M8 quest collectible ×3 in Level 1 (`QuestItemPickup`) |
| Thuốc Giải Mầm Xanh | Green Sprout Antidote | M8 single-use quest item; saves Tí; persists L1→L2 |
| Nhiệm vụ phụ | Side quest / quest chain | M8 antidote fetch-and-deliver chain (`QuestProgress`); M9 quest log (`QuestLog`) |
| Túi đồ | Inventory / bag | M9 slot-grid stackable inventory (`Inventory` + `InventoryUI`) |
| Bàn Chế Tạo | Crafting bench | M9 hub workbench (`CraftingBench` + recipes) |
| Sổ Tay Greenie | Codex / Journal | M9 bestiary + lore notes + cleanliness tracker (`Codex`) |
| Hồ Sơ Quái | Bestiary | M9 codex tab; one entry per enemy |
| Mẩu Nhật Ký | Lore Note | M9 hidden collectible note (`LoreNote`) — backstory |
| Độ Sạch Thung Lũng | Valley Cleanliness % | M9 per-stage clean tracker, fed by the trash-cleaning loop |
| Cổng Nexus | Portal Nexus | M9 hub portal hub linking both stages (`PortalNexus`) |
| Cổng Về Trạm | Return Portal | M9 in-stage portal back to the hub (`ReturnPortal`) — the new reverse travel |
| Mảnh Cổng | Portal Shard | M9 key item ×3; powers the broken Stage-2 portal |
| Chai Nhựa | Plastic Bottle | M9 crafting material (from trash) |
| Mảnh Kim Loại | Scrap Metal | M9 crafting material (from enemies/debris) |
| Tinh Thể Năng Lượng | Energy Shard | M9 rare crafting material |
| Lá Chắn Mầm | Sprout Shield | M9 consumable: temporary damage shield |
| Bom Hạt Giống | Seed Bomb | M9 consumable: throwable AoE that clears trash + damages |
| Slime Chúa | Slime King | M9 Stage-1 mini-boss guarding a Portal Shard |
| Bé Mây / Ông Tài / Cô Lan | (names kept) | M9 new NPCs (child / fisherman / ex-worker informant) |
| Nhiệm vụ | Mission / Objective | on-screen objective tracker (M7) |
| Hộp thoại | Dialogue | NPC dialogue panel / `DialogueRunner` (M7) |
| Bảng kể chuyện | Story slides | intro/ending cutscene sequence / `StorySequence` (M7) |
| Thanh máu (HP) | HP / Health | `PlayerHealth` |
| Rác thải (đã gom) | Trash collected | HUD counter / shop currency |
| Rác thải (vật thể) | Litter | `Litter` — walk over to clean, +1 trash |
| Auto-Clean | Autoplay | Auto-pathfind + auto-fire toggle — ❌ **cut, out of scope** |

## UI string guidance

When you add player-facing text, write it in **Vietnamese** (matching the brief)
and, where helpful, keep an English comment in code. Example button labels:
"Bắt Đầu" (Start), "Thoát" (Quit), "Cửa Hàng" (Shop).
