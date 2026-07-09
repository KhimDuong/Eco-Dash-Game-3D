# Coding Conventions — 3D

Follow these so generated code reads like one author wrote it. They match the
2D repo's conventions except where 3D physics differs — **ported code should
diff minimally against its 2D original**.

## C# style

- **Naming:** `PascalCase` for types, methods, properties; `camelCase` for
  locals/params and private fields (no leading underscore — this project
  doesn't use them).
- **Serialized private fields** over public fields for tunables:
  `[SerializeField] float moveSpeed = 5f;` — encapsulated but Inspector-editable.
- **Group serialized fields** under `[Header("…")]` so the Inspector is readable.
- **Cache components in `Awake`** (`controller = GetComponent<CharacterController>();`),
  read input in `Update`, do Rigidbody physics in `FixedUpdate`.
- **`Time.deltaTime`** for per-frame rates; `Time.fixedDeltaTime` in FixedUpdate.
- Prefer `TryGetComponent` over `GetComponent` + null check in hot paths.
- Keep methods short; one responsibility each.

## Comments / language

- Code, identifiers, and comments are **English**. Player-facing strings are
  **Vietnamese** ([glossary.md](glossary.md) is the canonical term map).
- Comment the *why*, not the *what*. When porting, keep the 2D file's comments
  unless the 3D change makes them wrong.

## Unity patterns (3D)

- **Movement plane is XZ.** Input `Vector2 (x, y)` maps to world
  `Vector3(x, 0, y)`. Normalize diagonal input. Y is only for hover-bob
  visuals and the FlyBot's flight height — never for gameplay-relevant player motion.
- **Player** uses a **CharacterController** (`controller.Move(...)` in
  `Update`); it is *not* a Rigidbody. Rotate only the **visual child** to face
  movement (`Quaternion.LookRotation`), keep the root unrotated.
- **Enemies** use Rigidbody (`linearVelocity` in `FixedUpdate`, freeze X/Z
  rotation, interpolate) or a **NavMeshAgent** for chasers — pick per enemy,
  see [architecture.md](architecture.md).
- **Triggers vs collisions:** pickups, mud, gas, and portal zones use
  `isTrigger` colliders and `OnTriggerEnter/Exit`; solid walls use non-trigger
  colliders. (Straight rename from the 2D `…2D` callbacks.)
- **Raycasts:** `Physics.Raycast` for LOS; layer-mask it to `Obstacle` so
  enemies don't see through walls.
- **Hit feedback:** tint via **MaterialPropertyBlock** (emission/base-color
  flash) — never `renderer.material` (leaks a material instance).
- **Compare tags** with `CompareTag("Player")`, never `gameObject.tag ==`.
- **Events:** `public event Action<int> OnSomethingChanged;` invoked with
  `OnSomethingChanged?.Invoke(value);` — the `GameManager`/UI event graph from
  the 2D repo is frozen; subscribe, don't restructure.
- **Scene flow by build index/name** exactly as the 2D `MenuController` /
  `EndScreenController` do it.

## File / asset conventions

- One MonoBehaviour per `.cs` file; filename == class name; same feature
  folders as the 2D repo (`Player/`, `Enemies/`, `Systems/`, `Items/`, `UI/`,
  `World/`, `Hazards/`, `Shop/`).
- Prefabs: PascalCase, descriptive (`PlasticSlime.prefab`, `GreyboxWall2m.prefab`).
- **Scale: 1 unit = 1 metre.** One 2D tile ≈ 1 m, Greenie ≈ 1 m tall — keep
  ported distances/speeds numerically identical to the 2D values.
- Models/materials: imported packs stay intact in
  `Assets/Models/ThirdParty/<PackName>/`; project materials in
  `Assets/Models/Materials/`, **URP Lit** shader. Never delete `.meta` files.
- Greybox placeholders are named `Greybox*` so the P3 art pass can find them.

## Definition of done for a code change

1. Compiles cleanly — verify with `mcp__coplay-mcp__check_compile_errors`.
2. Public tunables are `[SerializeField]` with sane defaults (ported values
   match the 2D original).
3. **Ported scripts: behavior parity with the 2D version** — same events
   raised, same numbers, verified in Play mode.
4. New systems are reflected in [architecture.md](architecture.md) and
   [roadmap.md](roadmap.md).
5. Any new third-party asset is logged in [../../CREDITS.md](../../CREDITS.md).
6. If it changes a scene: you are the scene's owner, and the scene is saved
   **in place** (see [unity-workflow.md](unity-workflow.md) gotchas).
