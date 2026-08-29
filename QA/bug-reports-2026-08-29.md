# QA Bug Report — 2026-08-29

> **Reporter:** Đức Anh  
> **Target Scope:** Level 1 (`Assets/_Scenes/Level1_BarrenFarm.unity`)  
> **Severity Standard:** § 0 of [CYCLE-2-TASKS.md](../CYCLE-2-TASKS.md)

---

## 1. Summary Table

| ID | Severity | What | Location / Component | Reported by | Status |
|---|---|---|---|---|---|
| **C9** | **S1 (Critical)** | Game crashes immediately when Greenie enters the green slow zone / toxic gas area | `Level1_BarrenFarm` (`ToxicGasZone.cs` / `ToxicMud.cs` / `WaterWade.cs`) | **Đức Anh** | **open** |

---

## 2. Defect Details

### C9 — Game crash upon entering the green slow zone (S1)

- **Reported by:** Đức Anh
- **Severity:** **S1 (Critical)** — Crash/Hard freeze. Prevents gameplay completion.
- **Affected Environment:** Level 1 (`Assets/_Scenes/Level1_BarrenFarm.unity`).

#### Description
When the player controls Greenie to walk into the green slow zone / toxic gas puddle in Level 1, the game immediately crashes or unhandles an exception, abruptly terminating the play session.

#### Suspected Root Causes
1. **`ToxicGasZone.cs`**:
   - The active toxic green cloud (`activeColor = toxic green`).
   - Potential `NullReferenceException` or unhandled exception during `OnTriggerEnter` or `Update()` loop when calculating damage ticks.
2. **`PlayerController.cs` / `PlayerHealth.cs`**:
   - Exception thrown inside `EnterMud()` or when applying damage/speed modification to uninitialized components.
3. **`WaterWade.cs` / Trigger Overlap**:
   - `HashSet<Collider>` iteration or `PlayerAnimator.SinkOffset` easing conflict when Greenie enters wading triggers.

---

## 3. Action Items & Verification
- [ ] Reproduce the crash in Play Mode within Unity 6 Editor.
- [ ] Inspect the Console Log for the exact stack trace and NullPointerException.
- [ ] Implement null-checks and guard clauses in trigger handlers (`OnTriggerEnter` / `OnTriggerExit`).
