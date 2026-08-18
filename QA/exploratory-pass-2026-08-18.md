# Exploratory end-to-end pass — 2026-08-18

> **Not the cycle-2 test report.** `QA/test-report-cycle2.md` is Đức Anh's deliverable (T7);
> this is an unassigned extra pass done to give T and P something to start from. Everything
> below is **unverified by a second person** — treat each row as a candidate defect and
> confirm it with the T2 procedure before anyone fixes it.
>
> Severity language is § 0 of [CYCLE-2-TASKS.md](../CYCLE-2-TASKS.md).

## How this run was done (and what it therefore cannot tell you)

One pass, `MainMenu → Intro → Level 1 → Hub → Level 2 → Boss → Ending`, driven in the
editor in **a single Play session** (so "which Play was it?" is *first* for every row below,
except where noted).

**Simulated keyboard input does not work against an unfocused editor Game view** — the
Input System resets non-background devices every frame, so queued key events never reach
`Keyboard.current`. The run therefore drove the game through its **component APIs**
(`StorySequence.Advance`, `IInteractable.Interact`, `InventoryUI.SetOpen`,
`PauseController.TogglePause`, …) rather than through keys.

Three consequences, all of which are still **T's** job:

- **No key binding was exercised.** W/A/S/D, J, E, I/Tab, Q, C, H, Esc and 1–4 are all
  unverified. If a key is mis-polled, this pass would not have seen it.
- **No timing data.** The ~31-minute budget question (T1) is untouched.
- **One aspect ratio only** (the editor Game view, 1100×533). T4's 1920×1080 / 1280×720 /
  ultrawide checks are untouched.

Nothing in the project was changed. Two `InputSystem` project settings were flipped during
the run and restored; the TMP dynamic atlas that grew during play was reverted.
`check_compile_errors` is clean.

---

## Summary

| # | Severity | What | Where | Status |
|---|---|---|---|---|
| E1 | **S2** | Hotbar renders at screen centre, on top of Greenie, with 0×0 slots | `HUD.prefab` + `Hotbar.cs:60` | **fixed** |
| E2 | **S2** | Pause during dialogue un-freezes the world while you stay locked in the dialogue | `PauseController.cs:35` | **fixed** |
| E3 | **S2** | Hub HUD shows the wrong Rác wallet (says 0, shop says 1) | `Shop_RecyclingStation.unity` | **fixed** |
| E4 | S3 | Three UI glyphs have no font glyph and render as `□` | 4 `file:line` below | **fixed** |
| E5 | S3 | Pause / Win / Lose / Settings use legacy `UI.Text`, not TMP | `HUD.prefab` | **fixed** |
| E6 | S3 | "← Menu" button overlaps the HP bar in the hub | `HubBuilder.cs` | **fixed** |
| E7 | S4 | Shop price text runs underneath the MUA button | `ShopUpgradeRow.cs` | **fixed** |
| E8 | S4 | Main-menu title "ECO-DASH" sits ~134 px right of centre | `MainMenu.unity` | **fixed** |
| E9 | S4 | Hub objective panel is an empty black box | `ObjectiveTracker` | **fixed** |
| E10 | `ArtKit.Fit` applies `rotY` **before** it measures, so grounding and centring both run on the final orientation; re-ran **Run the art pass (B5)**, **Rebuild Level 1** and **Rebuild Level 2** to carry it into the 6 affected prefabs and the two scenes | art-vs-hurtbox distance is **0.000 m at every yaw** through a full turn (was 1.80 m); on screen the model sits 1 px from the capsule centre; walked him into a slime — HP fell 6/6 → 5/6 with the two bodies visibly touching (0.12 m overlap) · `e10_01`–`e10_04` |
| E10 | **S2** | Greenie's model is drawn 1.80 m from the body that takes damage | `ArtKit.cs:189` | **fixed** |

> All nine were fixed on the same day — see [Fix log](#fix-log--2026-08-18) at the bottom for
> what changed, and re-test them with the T2 procedure before closing anything. The finding
> write-ups below are left as they were written, so the evidence trail still reads straight.

**Cleared** (findings that died under verification — see the bottom section, and please
don't re-spend the hours): Vietnamese diacritics are fine, there are no missing shaders,
and the console is error-free for a whole playthrough.

### Evidence

Twenty-six Game-view captures are in [`QA/screenshots/`](screenshots/), one per screen,
in play order — this is also the screenshot set BA6 asked T for.

| Finding | Screenshots |
|---|---|
| E1 hotbar | `06_level1_hud`, `08_hotbar_filled`, `10_bag_real_items` (and the smudge is visible in every in-game shot) |
| E1 key-item row | `11_bag_keyitems` |
| E2 pause/dialogue | `15_pause_during_dialogue` |
| E3 wallet | `19_shop` (HUD "Rác: 0" and panel "Rác: 1" in one frame) |
| E4 glyphs | `19_shop` (shop close), `20_crafting` (crafting close + 4 lock rows) |
| E5 legacy text | `07_death_screen`, `15_pause_during_dialogue` |
| E6 Menu/HP overlap | `18_hub`, `19_shop`, `20_crafting` |
| E7 shop layout | `19_shop` |
| E8 menu title | `01_mainmenu` |
| E9 empty panel | `18_hub` |
| E10 art vs hurtbox | `e10_01_player_centred`, `e10_03_contact` (after the fix — there is no "before" shot, it was measured not eyeballed) |
| Journey works | `01` → `26` end to end |

---

## E1 — The hotbar renders at the centre of the screen, on top of Greenie *(S2)*

- **Severity:** S2 — a designed feature is unusable
- **Scene / system:** every gameplay scene · `Hotbar` / `InventoryUI`
- **Which Play was it?** first
- **Steps:** 1. Start a new game. 2. Reach Level 1. 3. Look at the middle of the screen.
- **Expected:** four 88×88 slots along the bottom-centre, per
  [HOW_TO_PLAY § 4](../HOW_TO_PLAY.md) ("Hotbar (bottom-center): four slots").
- **Actual:** the digits `1 2 3 4` are crammed into ~12 px at the exact centre of the
  screen, permanently overlapping the player character. No slot frames, no item icons,
  no stack counts. Picking up consumables does not make it legible — it just adds more
  characters to the smudge.
- **Evidence:** runtime `RectTransform` dump —
  `Slot1..Slot4  anchoredPosition=(176,-44)(188,-44)(200,-44)(212,-44)  sizeDelta=(0,0)`

**Guess at cause — two independent bugs stack here:**

1. **Wrong parent rect.** `Assets/Prefabs/HUD.prefab` → `InventorySystem` has
   `m_AnchorMin/Max: {0.5, 0.5}`, `m_SizeDelta: {100, 100}` — a 100×100 box pinned at
   screen centre. `Hotbar.Build()` (`Hotbar.cs:57`) anchors `HotbarRoot` to
   `(0.5, 0)` — bottom-centre **of that little box**, not of the screen. Stretching
   `InventorySystem` to full-screen (anchors `0,0`–`1,1`, sizeDelta `0,0`) fixes the
   position.
2. **Slots collapse to 0×0.** `Hotbar.cs:60` adds a `HorizontalLayoutGroup` and sets
   `childForceExpandWidth/Height = false`, but leaves `childControlWidth/Height` at their
   default **`true`**. The layout group then sizes each child to its *preferred* size,
   and a sprite-less `Image` prefers 0 — so the explicit
   `sizeDelta = new Vector2(cellSize, cellSize)` on line 69 is overwritten every layout
   pass. Either set `childControlWidth/Height = false`, or give each slot a
   `LayoutElement` with `preferredWidth/Height = cellSize`.

> **The same layout-group mistake is at `InventoryUI.cs:100`** — the bag's
> "Vật phẩm nhiệm vụ" (key/quest item) row. With an antidote and portal shards in the bag
> it renders as a ~10 px smudge. Worth fixing in the same commit.
>
> The bag's main grid is *fine* because it uses `GridLayoutGroup` with an explicit
> `cellSize`, which forces child size. That contrast is the confirmation.

---

## E2 — Pause during a dialogue un-freezes the world while you stay locked in it *(S2)*

- **Severity:** S2 — reachable in normal play, and it kills you
- **Scene / system:** any scene with an NPC · `PauseController` / `DialogueRunner`
- **Which Play was it?** first
- **Steps:** 1. Talk to Bà Tư at the Level 1 spawn. 2. Press **Esc** (pause opens on top
  of the dialogue). 3. Press **Esc** again to resume.
- **Expected:** the world stays frozen, because the dialogue is still up.
- **Actual:** `Time.timeScale` becomes **1** while `DialogueRunner.IsActive` is still
  **true**. Enemies resume; Greenie cannot move or shoot (both scripts early-out on
  `DialogueRunner.IsActive`) but `PlayerHealth.TakeDamage` has no dialogue guard, so he
  takes contact damage he cannot avoid. **This killed Greenie during the run** — 6/6 to
  0/6 while standing in the opening conversation.
- **Guess at cause:** `PauseController.Update` (`PauseController.cs:33`) guards
  `TutorialPopup.IsOpen` — *"while the how-to-play tutorial is up it owns Esc"* — but has
  no equivalent guard for `DialogueRunner.IsActive`. `DialogueRunner` sets
  `Time.timeScale = 0f` at `:73` and restores `1f` at `:118`; `PauseController.SetPaused(false)`
  sets `1f` unconditionally. Two of the six clock owners, not talking to each other.
- **Cheapest fix:** mirror the tutorial guard — `if (DialogueRunner.IsActive) return;` —
  or make the resume path restore `DialogueRunner.IsActive ? 0f : 1f`.

This is the concrete instance of the risk BA4 lists ("six things own `Time.timeScale`; a
modal opening during a hit-stop is a plausible soft-lock and nobody has tried it") and of
T1 run 5's "pause during a boss volley, quit mid-dialogue".

---

## E3 — The hub HUD shows the wrong Rác wallet *(S2/S3 — PO call)*

- **Severity:** S2 if you think a wrong shop balance is a broken feature, S3 if cosmetic
- **Scene / system:** `Shop_RecyclingStation` · `HudController`
- **Which Play was it?** first
- **Steps:** 1. Collect any trash in Level 1 (HUD reads "Rác: 1"). 2. Take the north gate
  to the hub. 3. Read the HUD, then open Ông Bear's shop.
- **Expected:** the same number in both places. [HOW_TO_PLAY § 4](../HOW_TO_PLAY.md) is
  explicit: *"This number is **permanent** — it carries across levels and play sessions."*
- **Actual:** the HUD reads **"Rác: 0"** and the shop panel reads **"Rác: 1"** —
  simultaneously, on the same screen. The underlying `PlayerProgress.Trash` is **1**, so
  nothing is actually lost; only the HUD lies. Buying an upgrade will not update the HUD
  either, because nothing is subscribed.
- **Evidence:** runtime — `PlayerProgress.Trash = 1`, `GameManager.Instance = NULL`,
  `HudController.trashText.text = "Rác: 0"`, `coreText.text = "Lõi NL: 0/3"` (also a stale
  placeholder).
- **Guess at cause:** **the hub scene has no `GameManager` at all.**
  ```
  GameManager.prefab guid f4761436e9d5d3240ae3285c1ab1af4d
    Level1_BarrenFarm      12 references
    Level2_FactoryMaze     12 references
    Shop_RecyclingStation   0 references
  ```
  `HudController.Start()` wraps the whole counter setup in `if (GameManager.Instance != null)`,
  so in the hub it never reads the initial value and never subscribes to `OnTrashChanged`.
  The label keeps whatever was authored in the prefab.
- **Cheapest fix:** either add the `GameManager` prefab to the hub (matches the other two
  scenes), or make `HudController` read `PlayerProgress.Trash` directly when there is no
  `GameManager`. The first is more consistent; the second is safer if the hub is
  deliberately manager-free.

---

## E4 — Three UI glyphs have no font glyph and render as `□` *(S3)*

Confirmed twice: visually in the shop and crafting panels, **and** by Unity's own console,
which logs *"The character with Unicode value \uXXXX was not found in the
[LiberationSans SDF] font asset or any potential fallbacks. It was replaced by Unicode
character □"*.

| `file:line` | Glyph | Where the player sees it |
|---|---|---|
| `Assets/Editor/HubBuilder.cs:363` | `✕` U+2715 | Ông Bear's shop — **close button** |
| `Assets/Scripts/UI/CraftingUI.cs:64` | `✕` U+2715 | Crafting bench — **close button** |
| `Assets/Scripts/UI/CraftingUI.cs:109` | `🔒` U+1F512 | Every locked recipe row (4 of 6 rows) |
| `Assets/Scripts/UI/ObjectiveTracker.cs:48` | `✓` U+2713 | **Latent** — masked because `HUD.prefab` overrides `doneBullet` with `[x]` |

The two close buttons are the ones that matter: both are the only way to shut a modal with
the mouse, and both currently show a meaningless box.

**Cheapest fix:** use characters LiberationSans actually has — `×` (U+00D7, verified
renderable) for close, and an ASCII marker such as `[khoá]` or the existing `[ ]`/`[x]`
convention for the lock. Adding a symbol font to the fallback list also works but costs
atlas space and a new asset to credit.

Note the fourth row for the doc-coherence register (BA7): `ObjectiveTracker.cs` declares
`pendingBullet = "•"` / `doneBullet = "✓"` but `HUD.prefab` serialises `'[ ]'` / `'[x]'`,
so the code defaults are dead and one of them is a tofu waiting for whoever adds the next
`ObjectiveTracker` without touching the inspector.

---

## E5 — Pause / Win / Lose / Settings render through legacy `UnityEngine.UI.Text` *(S3)*

Thirteen player-facing strings do not go through TMP at all. They use the built-in
`LegacyRuntime` font:

```
HUD/PausePanel/Title                  [TẠM DỪNG]
HUD/PausePanel/Button_Resume/Text     [Tiếp Tục]
HUD/PausePanel/Button_Restart/Text    [Chơi lại]
HUD/PausePanel/Button_Settings/Text   [Cài đặt]
HUD/PausePanel/Button_Menu/Text       [Về Menu]
HUD/WinPanel/Title                    [THANH LỌC THÀNH CÔNG!]
HUD/WinPanel/Button_Restart/Text      [Chơi Lại]
HUD/WinPanel/Button_Menu/Text         [Về Menu]
HUD/LosePanel/Title                   [GREENIE ĐÃ GỤC NGÃ]
HUD/LosePanel/Button_Restart/Text     [Thử Lại]
HUD/LosePanel/Button_Menu/Text        [Về Menu]
HUD/Settings/.../MuteToggle/Label     [Tắt tiếng]
HUD/Settings/.../CloseButton/Text     [ĐÓNG]
```

Three reasons this is worth a row rather than a shrug:

1. **It looks wrong.** These are white slab buttons with near-black text, against a game
   whose every other button is green-on-dark. On the pause menu the four buttons have no
   gap between them and read as one white block. Neither the pause nor the lose screen
   dims the world behind it, so the dialogue box underneath competes with the menu.
2. **It is invisible to BA6.** The whole text audit and the glyph checklist are built
   around TMP and `LiberationSans SDF - Fallback`. These thirteen strings are on a
   different font pipeline, so a clean TMP glyph report would still say nothing about the
   pause, win, lose and settings screens — which everyone sees.
3. **It is a second thing that can break in a build** (T4), for different reasons than
   the TMP atlas.

---

## E6 — "← Menu" overlaps the HP bar in the hub *(S3)*

In `Shop_RecyclingStation` a green **"← Menu"** button is drawn on top of the HP bar in
the top-left corner — the button occupies roughly x 14–89, y 12–40 and the HP bar
x 20–232, y 25–48, so the button covers the bar's left cap and part of the "6/6" area.
Visible in every hub screenshot. The hub is also the only scene with this button.

---

## E7 — Shop price text runs underneath the MUA button *(S4)*

Every row of Ông Bear's shop reads `10 rác` immediately followed by the green **MUA**
button, and the word **"rác" is partly covered by the button's left edge**. The price
label and the button are laid out without enough separation.

Two smaller things in the same panel:

- There is no visual difference between *"unlocked but you can't afford it"* and
  *"unlocked and craftable"* at the crafting bench — both show a green name and an
  identically-styled (disabled) button. The only tell is the ingredient line's colour.
- The bottom third of both the shop and the crafting panel is empty.

---

## E8 — Main-menu title "ECO-DASH" is ~134 px right of centre *(S4)*

Everything else on the menu is centred on the canvas: the subtitle, all four buttons.
The title is not — it sits noticeably right.

- **Evidence:** the title's `RectTransform` *is* centred (`rect x 231.0 → 869.0,
  centre 550.0`) but its TMP component carries
  `margin = (0, 0, -267.88, -40.46)` — a **negative right margin**, which shifts the laid-out
  text by half of it. `textBounds.center = (138.84, -20.23)` instead of `(0, 0)`.
- **Guess at cause:** stray margin values baked into `MainMenu.unity`, the usual result of
  dragging TMP's margin handles in the scene view. Zeroing the margin fixes it.

---

## E9 — The hub's objective panel is an empty black box *(S4)*

In the hub the panel draws its title ("Trạm Tái Chế") and then a large empty black
rectangle, because there are no objectives in the hub. It should collapse to the title or
hide entirely.

---

## E10 — Greenie's model is drawn 1.80 m from the body that takes damage *(S2)*

**Reported by Khiêm after the fix pass above**, and the more serious half of what E1 looked
like: with the hotbar moved out of the way, the robot is still not where the player is.

`ArtKit.Fit` — the shared helper every generated prop, NPC and enemy goes through — did its
work in the wrong order:

```csharp
var after = Measure(go);                       // centre the mesh on the node...
go.transform.position += ...;
if (!Mathf.Approximately(rotY, 0f))            // ...then turn it
    go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
```

Several of the borrowed models — oopi and the whole Kenney factory kit — pivot on a **tile
corner**, not on the mesh centre. Centring moves the *node* so the mesh lands on it, which
leaves the node offset from the mesh by `d`. Rotating afterwards spins the mesh about that
node, so the mesh swings back off by exactly `R·d − d`. Every model given a `rotY` came out
displaced; every model without one was fine, which is why this survived the art pass.

Greenie is the worst case because he is the only 270° caller *and* `PlayerController` turns
his `Visual` toward travel every frame — so the offset **rotated with him**, orbiting his
real position at a radius of 1.80 m.

Measured, before:

```
Player.prefab  Art_oopi   1.80 m from the Visual node   (dx=-1.80, dz=0.00)
```

His `CharacterController` is `radius 0.35, height 1.15` — a body 0.70 m across. At 1.80 m
the visible robot had **no overlap at all** with the capsule that takes contact damage,
which is exactly the "hitbox is off centre by a big distance" that was reported. On screen
it was ~70 px, about twice his own width.

Six other prefabs were displaced by the same bug, less badly:

| Prefab | Was off by |
|---|---|
| `Player.prefab` (Greenie) | **1.80 m** |
| `RescueNPC_Ti.prefab` (standing Tí) | 0.18 m |
| `PollutionFlyBot.prefab` | 0.44 m |
| `MrBear.prefab` | 0.25 m |
| `NPC_Villager.prefab` | 0.24 m |
| `NPC_VillagerWoman.prefab` (Bà Tư) | 0.22 m |

…plus everything the two level builders bake straight into a scene with a rotation:
Level 1's 112 fence posts and 173 ground details, and Level 2's 38 factory props, which are
placed against walls with 90/180/270° turns and could be pushed *into* the wall by the
offset. Note the clearance tests (`TooClose`, `InsideWall`, `NearGameplay`) all check the
position **before** the offset was applied, so they were being checked against a spot the
prop did not end up in.

Two things that look like this bug but are not, and should be left alone:

- `MegaSmogBoss`'s `Art_StackL` / `Art_StackR` at ±1.45 m — `Rig()` places those holders
  deliberately, they are the two flanking smokestacks.
- `RescueNPC_Ti`'s *unconscious* Tí at 0.99 m — hand-posed by the caller at −78° about X
  with a −0.30 nudge. A body lying on the floor should not be centred on its stand-point.

---

## Findings that died under verification — please don't re-spend these hours

**Vietnamese diacritics render correctly. Do not chase this.**
`TMP_FontAsset.HasCharacter()` reports **45 of 134** Vietnamese diacritic letters as
missing from `LiberationSans SDF` *and* its fallback — including `Ặ`, `Ố`, `Ọ`, `Ừ`, `ẻ`,
`ẳ`, which appear in "Thẻ Từ", "TỐI ĐA", "THANH LỌC THÀNH CÔNG!" and more. **That API is
misleading here**: it does not consult the *dynamic* atlas, which pulls the glyph from the
source TTF on demand. Painting a test string through a live label and reading
`TMP_TextInfo.characterInfo` back shows every one of them resolving to
`LiberationSans SDF - Fallback` with `isVisible = true`, and they are legible in the
screenshots. Only `✕`, `✓` and `🔒` genuinely fail (E4) — and those are the only three
Unity logs a warning for.

*This one is worth carrying into BA6:* the character-set checklist should be built from
what TMP actually resolves at runtime, not from `HasCharacter`, or it will produce 45
false alarms.

**No magenta materials / missing shaders.** Every renderer in `Level2_FactoryMaze` —
184 of them — uses `Universal Render Pipeline/Lit`. Zero null materials, zero
`Hidden/InternalErrorShader`. C4's decision to keep particles on meshes/URP-Lit is
holding. Still needs re-checking *in an actual build* (T4), but the editor is clean.

**No console errors for the entire playthrough.** The only warnings were the three glyph
substitutions in E4 plus an unrelated Coplay editor-toolbar warning.

**D2 confirmed in play mode.** The crafting bench lists **"Mảnh Cổng"** as
*"🔒 Chưa mở khóa — hoàn thành nhiệm vụ liên quan."* and there is no way to unlock it —
matching the § 5 D2 prediction exactly. `Bình Hồi Phục Lớn` is also locked, which is
*correct* at that point in the run (Tí not yet saved), consistent with D1 being a
non-defect.

**D4 confirmed.** `Hub_Portal_To_Stage2` reports `shardCost = 1` at runtime; the design
says 3. Straight into P3.

**The game is finishable.** `MainMenu → Intro (4 slides) → Level 1 → gate → Hub →
portal → Level 2 → boss door → Mega-Smog → Ending (4 slides)` all connect, and the
ending plays out. Chests, the shop, the crafting bench, the codex's Độ Sạch tab (it
moves — it read 6% after a few metres of walking), the quest log's empty state, dialogue,
the boss health bar and the death/restart loop all work.

---

## Not covered by this pass

- Every keyboard control (see the note at the top)
- Wall-clock timing / the ~31-minute budget
- Any aspect ratio other than the editor Game view
- A real build (T4) — still nobody's done one
- Save/load across an application quit (T3)
- Balance data (T5) — though note that a **sweeping laser took 3 of 6 HP** while Greenie
  stood in it, and idling at the Level 1 spawn was enough to be killed by the slimes,
  which is at least a hint for T5's "can a `PlasticSlime` ever actually catch you?"
- The three missing NPCs (D3) — not re-checked, already logged

---

## Fix log — 2026-08-18

Applied after the pass above, by the same (unassigned) person. **Nothing here has been
re-tested by a second pair of eyes**, and the same limitation applies as to the pass
itself: no key binding was exercised, one aspect ratio only, no build. Each row says how
it was verified so T knows what is *not* covered.

| # | Change | Verified by |
|---|---|---|
| E1 | `HUD.prefab` → `InventorySystem` stretched to fill the canvas (was a 100×100 rect pinned at screen centre); `Hotbar.cs` + `InventoryUI.cs` set `childControlWidth/Height = false` on their `HorizontalLayoutGroup` | runtime rect dump — four 88×88 slots, centred on x, 24 px off the bottom · `fix_02` |
| E2 | `PauseController.Update` returns early while `DialogueRunner.IsActive` (mirrors the existing `TutorialPopup.IsOpen` guard); `SetPaused` now restores `0f` rather than `1f` when a dialogue still owns the clock | drove `TogglePause` twice inside Bà Tư's opening line — `timeScale` stayed **0** where it used to go to 1 |
| E3 | `HudController` reads `PlayerProgress.Trash` and subscribes to `PlayerProgress.OnChanged` instead of `GameManager.OnTrashChanged`, and hides the core counter when there is no `GameManager` | hub HUD reads "Rác: 1" matching the shop panel · buying an upgrade moved the HUD from 21 → 11, which never worked before · `fix_04`, `fix_05` |
| E4 | `✕` → `×` (`HubBuilder.cs`, `CraftingUI.cs`); the `🔒` prefix dropped from the locked-recipe line; `ObjectiveTracker`'s dead `•`/`✓` defaults replaced with the `[ ]`/`[x]` the prefab already serialises | both close buttons and all four locked rows render clean · `fix_05`, `fix_06` |
| E5 | All 13 legacy `UnityEngine.UI.Text` labels in `HUD.prefab` converted to `TextMeshProUGUI` with `LiberationSans SDF` pinned explicitly; the white button plates repainted to the accent green the rest of the game uses; the four pause buttons spaced 120 apart instead of 105 | `FindObjectsByType<Text>()` returns **0** at runtime · `fix_03`, `fix_07`, `fix_08` |
| E6 | `HubBuilder` back button y −44 → −232, i.e. under the Rác counter instead of over the HP bar | `fix_04` |
| E7 | `HubBuilder` shop cost label 200 wide @ −230 → 180 wide @ −270, ending 20 px clear of the MUA button | `fix_05` |
| E8 | The stray `margin = (0, 0, -267.88, -40.46)` on `MainMenu.unity`'s title zeroed. A scan of all scenes found this was the **only** non-zero TMP margin in the project | `fix_01` |
| E9 | `ObjectiveTracker.RefreshPanelVisibility` hides the backing plate, title and list while no row is visible | panel gone in the hub, still drawn in Level 1 · `fix_03`, `fix_04` |

Two things were changed that are not in the list above, both noticed while fixing E5 and
both in the settings window: the mute toggle's checkbox hung ~20 px outside the window's
left edge (moved to line up with the sliders), and `Text (Legacy)` was renamed to `Text`
now that it is a TMP object.

**Not changed, on purpose:**

- **D4** (`Hub_Portal_To_Stage2` wants 1 Portal Shard, the design says 3) is a balance
  number, not a defect — P's call which one is right.
- **The pause overlay's z-order.** Pausing mid-dialogue leaves the dialogue box drawn
  through the pause menu. E2's guard makes that state unreachable with the keyboard, so
  it is cosmetic and only visible if something calls `TogglePause` directly.
- **The report's "neither screen dims the world" line.** Re-shot after the fix, both the
  pause and the lose overlay clearly darken the world — that sub-claim did not reproduce.
- Everything under "Not covered by this pass" is still not covered.

Rebuilding the hub (**Eco-Dash → Rebuild the hub**) is what carries E4/E6/E7 into
`Shop_RecyclingStation.unity`; it was run, so the scene and the five hub prefabs are
regenerated in the same change.

E10 is a fix to `ArtKit`, which every generated thing goes through, so the **art pass** and
**both level rebuilds** were run for the same reason. Those three regenerations are most of
the diff. They are reproducible and were checked for drift: the prefab diffs are fileID
churn plus exactly the six transforms listed in E10, and both scenes come back with an
identical object count (Level 1: 584 GameObjects / 363 prefab instances; Level 2: 87 / 54).
`AudioPass` writes to prefabs only, so a level rebuild cannot drop the C5 sound layer.
