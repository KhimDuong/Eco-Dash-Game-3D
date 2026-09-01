# Asset Sourcing & Pipeline — 3D

How we get models, materials, audio, and fonts into Eco-Dash 3D, and the rules
around it.

## Policy (read this first)

> **Eco-Dash 3D is a university Game-Development course project — a
> midterm/final submission. It is NOT published and NOT monetized.** Therefore
> asset license and copyright are **not a blocker**. The one hard rule is:
> **every borrowed asset must be acknowledged in [CREDITS.md](CREDITS.md)**
> (source, author, URL, and license if known).

### Source priority (use in this order)

1. **[Kenney](https://kenney.nl/assets)** — CC0, huge modular low-poly 3D kits
   ("Nature Kit", "Farm Kit"-adjacent packs, "Sci-Fi/Industrial Kit",
   "City Kit", characters). First stop for greybox-replacing props.
2. **[Quaternius](https://quaternius.com/)** (CC0) and
   **[KayKit / Kay Lousberg](https://kaylousberg.itch.io/)** (mostly CC0) —
   stylized low-poly characters, dungeons, nature; great fit for a cute robot
   hero and factory interiors.
3. **[itch.io](https://itch.io/game-assets/free)** — filter **Free + 3D**;
   search "low poly farm", "low poly factory", "robot character low poly".
   Also [Poly Pizza](https://poly.pizza/) (CC0/CC-BY aggregator) and
   [Sketchfab](https://sketchfab.com) (filter CC-licensed, downloadable).
4. **Unity Asset Store free section** — fine too; note the license in CREDITS.
5. **Generate it (last resort, costs real money).** Only if no suitable free
   asset exists, use Coplay's gen tools (`generate_3d_model_from_text` /
   `generate_3d_model_from_image`, `generate_3d_model_texture`,
   `generate_sfx`, `generate_music`). **Billed per call** to the project
   owner's Coplay account (each returns `cost_usd`; 3D model gen is much more
   expensive than SFX). **Confirm with the owner before generating anything
   non-trivial.** Generated meshes usually need cleanup (scale, pivot,
   materials) — a free asset is almost always the better deal.

> Do **not** hand-model or spend hours generating if a free asset exists —
> find it, drop it in, credit it. And greybox (ProBuilder) is always an
> acceptable stand-in until the P3 art pass.

## What each scene needs

| Need | Good search terms / sources |
|------|------------------------------|
| **Greenie** (small robot hero, ~1 m) | Quaternius "robots"; Kenney characters; itch "low poly robot"; needs idle/walk anims or none (we hover-bob in code) |
| **Plastic Slime / Slime King** | itch "slime 3d low poly"; or a CC0 blob mesh + green translucent URP material |
| **Pollution Fly-Bot** | Kenney/Quaternius drones, "sci-fi enemy" |
| **Mega-Smog boss** | large industrial robot/mech, Quaternius mechs; scale up + dark smoke particles |
| **L1 Barren Farm** | Kenney Nature Kit (trees/rocks/fences/crops), itch "low poly farm pack"; dead-vs-lush material swap for reclamation patches |
| **L2 Factory Maze** | Kenney Sci-Fi/Industrial kits; itch "low poly factory", pipes/crates/barrels |
| **Hub (Recycling Station)** | Kenney City/Industrial props, recycling bins, market stall for Ông Bear |
| **NPCs** (Bà Tư, Ông Sáu, Tí, Bé Mây, Ông Tài, Cô Lan, Ông Bear) | Quaternius/KayKit character packs (villagers, workers, animals — a bear!) |
| **Pickups** (core, herbs, bottles, shards) | Kenney "gems/food" packs; simple emissive URP materials sell "energy" items |
| **SFX/Music** | **port everything from the 2D repo's `Assets/Audio/` first** (already credited); new ambience from freesound.org / Kenney audio |

## Import settings (keep models consistent)

For every imported model (FBX/glTF):

- **Scale:** 1 unit = 1 metre. Check the pack's scale factor on import —
  Greenie ≈ 1 m tall, props sized relative to that; tweak the importer's Scale
  Factor, not per-instance transforms.
- **Materials:** convert/assign **URP Lit** (built-in materials render
  magenta). Extract materials into `Assets/Models/Materials/` (or the pack's
  folder), don't leave them embedded if you need to edit them.
- **Read/Write** off, **mesh compression** off (course project, keep it simple).
- Mark environment/static props **Static** (lighting + NavMesh bake).
- Low-poly flat-shaded packs: no normal-map fuss; one texture atlas per pack
  is normal — keep it.

## Folder layout

```
Assets/Models/
├── ThirdParty/<PackName>/   # imported packs kept intact (incl. LICENSE/README)
├── Materials/               # project-made URP materials
├── Characters/
├── Environment/
└── Items/
Assets/Audio/                # ported 2D audio + any new clips
```

Keep each downloaded pack in its own `ThirdParty/<PackName>/` folder with any
LICENSE/README it shipped with, so attribution stays traceable.

## Workflow to add an asset

1. Find it (source priority above). Note author + URL + license.
2. Download; drop files in `Assets/Models/ThirdParty/<PackName>/`.
3. Apply the import settings above (scale check + URP materials).
4. **Add an entry to [CREDITS.md](CREDITS.md).** (Non-negotiable.)
5. Use it in prefabs (swap the `Greybox*` placeholder mesh, keep colliders
   and scripts); commit.

## The art pass (B5) — how real art gets in

**Done.** The greybox tables below are kept as the record of what each prefab used
to be; every one of them now carries a real model. Two generated things own it:

| What | Where | Notes |
|---|---|---|
| `Assets/Editor/ArtKit.cs` | the plumbing | loads a model, sizes it to a **height in metres**, pivots it on the floor, converts glTF materials to URP/Lit, strips lights, builds an Idle controller |
| `Assets/Editor/ArtPass.cs` | the mapping | one entry per prefab: what replaces what. Menu: **Eco-Dash → Run the art pass (B5)**. Idempotent |
| `Assets/Editor/SceneLook.cs` | the look | per-scene sun, ambient, fog and a **post-processing volume profile** saved beside the scene |

Rules that keep it working:

- **Only the visual is replaced.** Colliders, trigger radii, scripts, prompt canvases
  and the toggled-child contracts (`Visual_Open`/`Visual_Locked`, `Lid`/`Hole`,
  `Visual_Unconscious`/`Visual_Awake`) are gameplay and are never touched. The loose
  props are the one exception: their colliders are **refit from the model's measured
  bounds**, because a real model pivots on the floor where a primitive is centred on
  its own origin — which is also why `Level1Builder` no longer lifts props.
- **`HubBuilder`, `FactoryKitBuilder` and `EnemyPrefabBuilder` rebuild their prefabs
  from primitives**, so each one calls `ArtPass.Reapply*()` at the end. Without that,
  rebuilding the hub silently reverts Ông Bear to a grey capsule.
- **Fit by height only works on roughly cubic models.** `rock_largeA` is a flat slab;
  fitting it to 0.55 m tall made it 2.15 m across and closed corridors the 2D layout
  leaves open. Check the printed size in the pass's log.
- **`.glb` needs `com.unity.cloud.gltfast`** (added in B5). Kenney ships `.fbx`, which
  Unity reads natively and which imports straight to URP/Lit.
- **Anything a glTF model brings with it is suspect.** The Quaternius flying robot
  ships two baked-in **directional lights**; one per fly-bot would blow out the level.
  `ArtKit.Spawn` always strips lights.

### Two import checks to run on every new pack (learned in cycle 2)

1. **Is the texture named what the models ask for?** A Kenney FBX asks for `colormap`.
   Unity's material search is **recursive-up**, so a pack whose texture is called anything
   else binds to whichever *other* pack under `ThirdParty/` has a `colormap.png`. The
   Fantasy Town Kit ships `variation-a.png` and all 167 of its models imported wearing Cube
   Pets' atlas — no warning, no error, just wrong colours. Rename to `colormap.png` on import.
2. **Is `Textures/` empty?** Then the pack is flat-shaded and its material colours *are* its
   art — and they may be wrong. The Nature Kit's are a pastel placeholder set (`leafsGreen`
   imports as turquoise), which is why Level 1's vegetation rendered cyan through all of
   cycle 1. Render one model before trusting it; corrections go in `ArtKit.NaturePalette`,
   keyed by material name so the shared batch survives, and get noted in
   [CREDITS.md](CREDITS.md) under **Modifications**.

Also worth knowing: **the Survival Kit has no houses.** Its `structure*.fbx` pieces are open
scaffold frames, not buildings — which is what B4 discovered the hard way. Village buildings
come from the Fantasy Town Kit, assembled from modular walls and a roof cap by
`ArtPass.Cottage` via `ArtKit.SpawnModule` (which, unlike `Spawn`, keeps a module's
deliberately off-centre pivot).

## Greybox placeholders (the P3 art pass replaced these)

Until art is sourced, everything visual is **ProBuilder/primitive greybox**,
named `Greybox*` and using a small shared set of flat URP materials
(`Greybox_Ground`, `Greybox_Wall`, `Greybox_Enemy`, `Greybox_Prop`…). These
are original and need no CREDITS entry; they're listed here so the P3 art pass
(Dev B task B5) knows to hunt the `Greybox` prefix to zero.

**Currently in the project** (created in A3; materials live in
`Assets/Models/Placeholder/`, kept separate from `Materials/` so B5 can find
them by folder as well as by prefix):

| Placeholder | Used by | Replace with |
|---|---|---|
| `Greybox_Greenie` on a scaled capsule + `Greybox_Accent` "Nose" cube | `Player.prefab` → `Visual` child | a ~1 m low-poly robot (see the Greenie row above); keep the `Visual` child, its collider-free setup and the CharacterController on the root |
| `Greybox_Seed` on a 0.28 m sphere | `Seed.prefab` | small seed//energy-pellet mesh + emissive material |
| `Greybox_Ground` on a 40×40 m plane | `Ground_Greybox` in Dev A's test scene | nothing — B1's greybox kit and real L1 floor supersede it |

### B1 greybox kit — `Assets/Prefabs/Greybox/`

Built by `Assets/Editor/Level1Builder.cs`'s companion scripts; floors and walls
are real **ProBuilder** meshes so they stay editable with the ProBuilder tools,
small props are primitives. Materials live in `Assets/Models/Materials/Greybox_*`.

| Prefab | What it stands in for | Notes for the P3 art pass |
|---|---|---|
| `Greybox_Floor` | 4 × 4 m ground tile (192 of them make Level 1) | keep the **tiling** — `ReclamationPatch` re-tints tiles as its wave passes; one giant slab would flip the whole map green at once |
| `Greybox_Wall` | level boundary, scaled per side | any wall/hedge mesh; keep the `Obstacle` layer |
| `Greybox_Crate` / `_Barrel` / `_Rock` | crates, barrels, exhaust pipes, rocks, bushes, pots | farm-set props |
| `Greybox_Hut` | `RusticHut` | village hut |
| `Greybox_Fence` | fence post (36 ring the village) | fence section |
| `Greybox_DeadTree` | dead tree (30 across the field) | withered tree; the crown has no collider on purpose |
| `Chest` · `EnergyCore` · `Litter` · `ToxicMud` · `ReclamationPatch` · `TeleportGate` · `NPC_Villager` · `Herb` · `LoreNote` · `ItemPickup` | every interactable in L1 | swap the meshes only — the scripts, trigger colliders and "Nhấn E" prompts are the contract. **`ToxicMud` is switched off in the build** (2026-08-31) because its flat pale sheet reads badly at eye height — it is the one on this list that needs a *redraw* rather than a swap. Turn it back on with `Level1Builder.ToxicMudEnabled` and rebuild Level 1. |

### C1/C2 enemy kit — `Assets/Prefabs/Enemies/`

Generated by `Assets/Editor/EnemyPrefabBuilder.cs` (menu: **Eco-Dash → Rebuild
enemy prefabs**), so the prefabs can be regenerated after a stat or component
change instead of being re-dragged. C3 extends the same builder.

| Prefab | Stands in for | Replace with |
|---|---|---|
| `PlasticSlime` — squashed sphere (`Greybox_Slime`) + two rubbish shards (`Greybox_SlimeTrim`) | Quái Rác Nhựa ×29 in L1 | a low-poly slime/blob. **Keep the root's `SphereCollider`** — `SeedProjectile` resolves `IDamageable` off the collider it hits, so a hitbox moved onto a child makes the slime unkillable |
| `PollutionFlyBot` — pod body + 2 rotors + belly nozzle + emissive eye (`Greybox_FlyBot`/`_Trim`/`_Eye`) | Phi Cơ Ô Nhiễm, Level 2's ranged enemy | a low-poly drone. Mesh goes under the **`Visual`** child (it bobs and turns; the root is physics). Keep the `Eye` or something like it — it is the only cue to which way the bot is facing |
| `SmogOrb` — 0.36 m emissive sphere (`Greybox_SmogOrb`) | the fly-bot's and (later) the boss's ordnance | a smoke/toxic orb + particle trail. Built to mirror `Seed.prefab` exactly; keep it that way so both projectiles behave alike |

Load-bearing details, easy to delete by accident:

- **`NavMeshModifier(ignoreFromBuild)`** — level builders bake from physics
  colliders on every layer, so without it 29 slimes punch 29 holes in the NavMesh
  they walk on. `PollutionFlyBot.prefab` and `Player.prefab` carry one too.
- **Emission enabled on `Greybox_Slime`** — `HitFlash` pushes `_EmissionColor`
  through a property block, and URP strips emission from the shader variant when
  the keyword is off, which makes the hit flash silently invisible.
- **The fly-bot's two colliders.** The solid sphere is its movement collider and
  must stay high (it is what lets the bot clear crates); the trigger capsule is the
  hurtbox and must reach down to ~0.1 m, because Seeds fly flat at Greenie's fire
  point (y ≈ 0.6) and a body-only hitbox at 1.6 m makes the bot **unkillable**. See
  [architecture.md](.claude/docs/architecture.md#flying-enemies-need-two-colliders-c2).
- **`FirePoint` hangs 0.72 m below the fly-bot's body, on the root, not on `Visual`.**
  Below, so its flat orbs meet Greenie's capsule rather than sailing over his head;
  on the root, so the hover bob doesn't jitter every shot.

**Do not shrink the walk-over trigger radii** (0.75–0.8 m) when swapping meshes:
a CharacterController is only sampled once per physics step, so the 2D-sized
0.4 m spheres could be stepped clean over on a slow frame.

### B3 factory kit — `Assets/Prefabs/Factory/`

Generated by `Assets/Editor/FactoryKitBuilder.cs` (menu: **Eco-Dash → Rebuild
factory kit**). Level 2's floor and walls are **unit cubes the level builder
stretches** per merged tilemap rectangle, not fixed tiles like Level 1's.

| Prefab | Stands in for | Notes for the P3 art pass |
|---|---|---|
| `Greybox_FactoryFloor` / `_FactoryWall` | the maze (1 slab + 23 wall boxes) | keep them **scalable** — the builder sets `localScale` per rectangle |
| `Keycard` | Thẻ Từ ×2 | any keycard/passcard mesh |
| `SweepingLaser` | the two corridor lasers | emitter + a `Beam` child stretched along **+X**; the script resizes it from `beamLength/Width/Height` in `Awake`, so don't hand-scale `Bar` |
| `ManholeTrap` ×3 | Bẫy hố ga | `Lid` and `Hole` are toggled objects, not one mesh — keep both |
| `ToxicGasZone` | the boss's gas pools (C3 spawns them) | keep the cloud a **flat disc**; a billowing volume hides the floor the player must read |
| `BossDoor` | Cửa Boss | the `Blocker` child is the real seal — the panels are show. Keep `Light_L`/`Light_R` as separate renderers; `BossDoor` tints them red→green |
| `ReturnPortal` | Cổng Về Trạm | walk-over, returns to the hub |
| `RescueNPC_Ti` | Tí | **two poses, not two textures**: `Visual_Unconscious` (slumped) and `Visual_Awake` (standing) are toggled. A texture swap barely reads under the ¾ rig |

### B4 hub kit — `Assets/Prefabs/Hub/`

Generated by `Assets/Editor/HubBuilder.cs` (menu: **Eco-Dash → Rebuild the hub**),
which builds the prefabs *and* the scene.

| Prefab | Stands in for | Notes for the P3 art pass |
|---|---|---|
| `MrBear` | Ông Bear, the shopkeeper | a bear character (KayKit/Quaternius have them). He carries the **shop only** — see `RecyclingCounter` |
| `RecyclingCounter` | Ông Bear's `bear_recycle` side quest | a market stall / recycling bin. It is a **separate object on purpose**: `PlayerInteractor` resolves one `IInteractable` per collider |
| `CraftingBench` | Bàn Chế Tạo | a workbench. Its window is built by `CraftingUI` at runtime — nothing to author |
| `StagePortal` | the two hub gates | keep `Visual_Open` / `Visual_Locked` as **toggled children**; `StagePortal` swaps them for the shard gate |
| `ShopUI` | Ông Bear's shop window | a **screen-space canvas**, not world art. Rebuild rather than re-drag if rows change — `ShopController` needs every field wired |

Greenie's proportions are the contract, not the mesh: **~1.15 m tall, 0.35 m
radius**, pivot at the feet, `Visual` child centred at y = 0.6.

Carried over from the 2D project's audio work: L1 SFX were Coplay-generated
(kept); music/jingles are CC0/CC-BY from OpenGameArt — copy the corresponding
entries into this repo's CREDITS.md when the audio is ported (Dev C task C5).
