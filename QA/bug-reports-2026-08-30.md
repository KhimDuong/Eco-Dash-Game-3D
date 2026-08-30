# QA Bug Report — 2026-08-30

> **Reporter:** User  
> **Target Scope:** UI / Shop (`ShopUI.cs` / `HudController.cs`) & Level 2 (`Assets/_Scenes/Level2_FactoryMaze.unity`)  
> **Severity Standard:** § 0 of [CYCLE-2-TASKS.md](../CYCLE-2-TASKS.md)

---

## 1. Summary Table

| ID | Severity | What | Location / Component | Reported by | Status |
|---|---|---|---|---|---|
| **C10** | **S3 (Normal)** | Purchasing health in Shop does not update HUD immediately; health display only refreshes after exiting the Shop window | `ShopUI` / `PlayerHealth` / `HudController` | **User** | **open** |
| **C11** | **S3 (Normal)** | Environment walls in Map 2 are too tall, occasionally obstructing camera line-of-sight to walkable pathways | Level 2 (`Level2_FactoryMaze.unity`) / Camera | **User** | **open** |
| **C12** | **S2 (Major)** | Environmental props/objects in Map 2 lack collision, allowing the player to walk directly through them | Level 2 (`Level2_FactoryMaze.unity`) / Props | **User** | **open** |

---

## 2. Defect Details

### C10 — Health purchase does not update HUD immediately until exiting Shop UI (S3)

- **Reported by:** User
- **Severity:** **S3 (Normal)** — Delayed UI update / UX inconsistency.
- **Affected Component:** Shop UI & HUD (`ShopUI.cs`, `PlayerHealth.cs`, `HudController.cs`).

#### Description
When the player purchases health restoration / health potions inside the Shop interface, the HUD health bar/indicator does not update immediately in real-time. The health display on the HUD only refreshes after the player closes/exits the Shop window.

#### Suspected Root Causes
1. **Deferred HUD Refresh**:
   - `ShopUI` updates `PlayerHealth` or player stats upon purchase, but does not invoke `OnHealthChanged` or trigger an immediate HUD redraw while the Shop modal is active.
2. **UI Layering / Event Suspension**:
   - HUD updates might be suspended or covered while `ShopUI` is open, and only re-query `PlayerHealth.CurrentHealth` upon closing `ShopUI`.

---

### C11 — Tall walls in Map 2 obstruct camera view of player pathway (S3)

- **Reported by:** User
- **Severity:** **S3 (Normal)** — Camera occlusion / visibility issue.
- **Affected Environment:** Level 2 (`Assets/_Scenes/Level2_FactoryMaze.unity`).

#### Description
In Map 2 (`Level2_FactoryMaze.unity`), certain wall meshes and environmental structures are built too tall. Due to the fixed 3/4 top-down camera perspective, these high walls frequently block the player's view of Greenie and hide upcoming pathways/corridors behind them.

#### Suspected Root Causes
1. **Wall Geometry Height**:
   - Wall mesh height in `Level2_FactoryMaze` is scaled too high for the fixed Cinemachine camera angle.
2. **Missing Camera Occlusion Fading**:
   - The project lacks a camera obstacle fade / dither shader to make foreground walls semi-transparent when they occlude the player.

---

### C12 — Environmental props in Map 2 lack colliders (Player clips through objects) (S2)

- **Reported by:** User
- **Severity:** **S2 (Major)** — Environment physics / collision defect.
- **Affected Environment:** Level 2 (`Assets/_Scenes/Level2_FactoryMaze.unity`).

#### Description
In Map 2 (`Level2_FactoryMaze.unity`), various environmental objects/props (e.g. factory obstacles, machinery, crates, or decorative props) do not have active physical collision. The player character Greenie can walk directly through solid environmental objects without being blocked.

#### Suspected Root Causes
1. **Missing Collider Components**:
   - 3D prop prefabs or greybox meshes instantiated in `Level2_FactoryMaze.unity` do not have `BoxCollider` or `MeshCollider` components attached.
2. **Trigger or Layer Misconfiguration**:
   - Prop colliders might be set to `Is Trigger = true` instead of solid physics collision, or assigned to a physics layer that is ignored by the Player physics matrix.

---

## 3. Action Items & Verification
- [ ] **Fix C10**: Update `ShopUI.cs` to trigger `PlayerHealth.Heal()` / `OnHealthChanged` immediately upon item purchase so the HUD updates without requiring the player to exit the Shop.
- [ ] **Fix C11**: Adjust wall height scale in `Level2_FactoryMaze.unity` or implement camera occlusion dithering/fading for high walls.
- [ ] **Fix C12**: Audit prop prefabs in `Level2_FactoryMaze.unity` and attach `BoxCollider` / `MeshCollider` components with solid physics enabled.
- [ ] Verify all fixes in Play Mode.
