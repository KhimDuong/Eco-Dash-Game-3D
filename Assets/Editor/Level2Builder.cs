using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// B3: build Assets/_Scenes/Level2_FactoryMaze.unity from the 2D level's own layout.
/// Every placement comes out of Tools/level2_layout.csv, exported straight from the 2D
/// scene YAML, so the maze keeps its exact proportions: 2D (x, y) maps to 3D (x, z) on
/// the ground plane, 1 tile = 1 m.
///
/// <para>Level 2 differs from Level 1 in where the geometry comes from. Level 1's props
/// were individual objects; Level 2's maze is a pair of <b>tilemaps</b> — 1 360 floor
/// cells and 926 obstacle cells. <c>Tools/export_level2.py</c> merges the obstacle grid
/// into maximal rectangles first (926 cells → 23 boxes), which is the same solid shape
/// with two orders of magnitude less for the renderer, the physics scene and the NavMesh
/// bake to chew on.</para>
///
/// Idempotent — re-running rebuilds the scene from scratch. Regenerate the CSV with
/// <c>Tools/dump_level2.py</c> + <c>Tools/export_level2.py</c> if the 2D level changes.
/// </summary>
public static class Level2Builder
{
    [MenuItem("Eco-Dash/Rebuild Level 2 from the 2D layout")]
    public static void Rebuild() => Debug.Log(Execute());

    const string ScenePath = "Assets/_Scenes/Level2_FactoryMaze.unity";
    const string LayoutCsv = "Tools/level2_layout.csv";
    const string Kit = "Assets/Prefabs/Factory/";
    const string Greybox = "Assets/Prefabs/Greybox/";

    const float WallHeight = 3f;
    const float FloorThickness = 0.5f;

    static StringBuilder log;
    static Transform envRoot, hazardRoot, playRoot, enemyRoot;
    static Vector3 playerStart = new Vector3(-0.5f, 0f, -12.5f);
    static int botCount;

    public static string Execute()
    {
        log = new StringBuilder();
        if (!File.Exists(LayoutCsv)) return "ERROR: " + LayoutCsv + " missing — run export_level2.py first";

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Main Camera") Object.DestroyImmediate(go);   // CameraRig brings its own

        envRoot = new GameObject("Environment").transform;
        hazardRoot = new GameObject("Hazards").transform;
        playRoot = new GameObject("Gameplay").transform;
        enemyRoot = new GameObject("Enemies").transform;
        botCount = 0;
        walls.Clear(); floors.Clear(); occupied.Clear();

        int placed = PlaceFromLayout();
        occupied.Add(new Vector2(playerStart.x, playerStart.z));
        Dress();
        PlaceSystems();
        SetupLighting();
        int navOk = BakeNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();

        log.AppendLine("placed " + placed + " objects from the 2D layout; navmesh areas=" + navOk);
        log.AppendLine("saved " + ScenePath);
        return log.ToString();
    }

    // --- layout ---------------------------------------------------------------------------

    static int PlaceFromLayout()
    {
        int n = 0;
        foreach (var line in File.ReadAllLines(LayoutCsv))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var f = line.Split(',');
            string kind = f[0], name = f[1];
            float x = P(f[2]), z = P(f[3]), a = P(f[4]), b = P(f[5]), rotY = P(f[6]);
            var pos = new Vector3(x, 0f, z);

            switch (kind)
            {
                case "floor":
                    // Slab top sits at y = 0, like Level 1's ground.
                    Slab(Kit + "Greybox_FactoryFloor.prefab", "Floor", envRoot,
                         new Vector3(x, -FloorThickness * 0.5f, z), new Vector3(a, FloorThickness, b));
                    floors.Add((new Vector2(x, z), new Vector2(a, b)));
                    break;
                case "wall":
                    Slab(Kit + "Greybox_FactoryWall.prefab", "Wall", envRoot,
                         new Vector3(x, WallHeight * 0.5f, z), new Vector3(a, WallHeight, b));
                    walls.Add((new Vector2(x, z), new Vector2(a, b)));
                    break;
                case "keycard": Spawn(Kit, "Keycard", playRoot, pos).name = name; break;
                case "manhole": Spawn(Kit, "ManholeTrap", hazardRoot, pos).name = name; break;
                case "laser": Spawn(Kit, "SweepingLaser", hazardRoot, pos, rotY).name = name; break;
                case "returnportal": Spawn(Kit, "ReturnPortal", playRoot, pos).name = name; break;
                case "rescuenpc": Spawn(Kit, "RescueNPC_Ti", playRoot, pos).name = name; break;
                case "bossdoor": Spawn(Kit, "BossDoor", playRoot, pos).name = name; break;
                case "npc": FleeingWorker(pos); break;
                case "flybot": PlaceFlyBot(name, pos); break;
                case "player": playerStart = pos; break;
                case "boss": PlaceBoss(pos); break;
                default: continue;
            }
            n++;
        }
        return n;
    }

    static void PlaceFlyBot(string name, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/PollutionFlyBot.prefab");
        if (prefab == null) { log.AppendLine("  MISSING PollutionFlyBot.prefab"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemyRoot);
        go.name = $"{name}_{++botCount:00}";      // unique name -> unique SceneProgress id
        go.transform.position = pos + Vector3.up * 1.6f;   // start at hover height
    }

    static void FleeingWorker(Vector3 pos)
    {
        var go = Spawn(Greybox, "NPC_Villager", playRoot, pos);
        go.name = "FleeingWorker";
        var npc = go.AddComponent<DialogueNPC>();
        var so = new SerializedObject(npc);
        SetLines(so.FindProperty("lines"), new[]
        {
            ("Công nhân bỏ trốn", "Quay lại đi, robot! Bên trong toàn laser quét với drone tuần tra..."),
            ("Công nhân bỏ trốn", "Tìm 2 Thẻ Từ để mở cửa khu trung tâm. Nhưng coi chừng 'nó' — cỗ máy Mega-Smog!"),
        });
        so.FindProperty("autoBriefOnStart").boolValue = true;
        so.FindProperty("briefDelay").floatValue = 0.6f;
        so.FindProperty("swapOnAllCores").boolValue = false;
        so.FindProperty("talkOnce").boolValue = false;
        so.FindProperty("prompt").objectReferenceValue = go.transform.Find("Prompt").gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // C3: the arena's centrepiece. The 2D layout's own boss coordinate, so the machine lands
    // exactly where the tilemap's sealed room was built around it.
    static void PlaceBoss(Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/MegaSmogBoss.prefab");
        if (prefab == null)
        {
            log.AppendLine("  MISSING MegaSmogBoss.prefab — left the spawn marker instead");
            Marker("BossSpawn_MegaSmog", pos);
            return;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemyRoot);
        go.name = "MegaSmogBoss";
        go.transform.position = pos;
        occupied.Add(new Vector2(pos.x, pos.z));   // B5's dressing keeps out of the arena centre
        log.AppendLine("  MegaSmogBoss placed at " + pos);
    }

    static void Marker(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(enemyRoot, false);
        go.transform.position = pos;
    }

    // --- systems ---------------------------------------------------------------------------

    static void PlaceSystems()
    {
        Inst("Assets/Prefabs/Player.prefab", "Greenie", playerStart + Vector3.up * 0.7f);
        Inst("Assets/Prefabs/CameraRig.prefab", "CameraRig", Vector3.zero);
        var hud = Inst("Assets/Prefabs/HUD.prefab", "HUD", Vector3.zero);

        var gm = Inst("Assets/Prefabs/GameManager.prefab", "GameManager", Vector3.zero);
        if (gm != null)
        {
            var so = new SerializedObject(gm.GetComponent<GameManager>());
            // Three keycards: two on the floor, and the one Tí hands over when rescued.
            so.FindProperty("requiredCores").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  GameManager.requiredCores = 3");
        }

        if (hud != null) ConfigureHud(hud);
    }

    // The per-scene HUD overrides the contract expects Dev B to set (see architecture.md).
    // Values are the 2D Level 2 scene's own.
    static void ConfigureHud(GameObject hud)
    {
        var hudC = hud.GetComponentInChildren<HudController>(true);
        if (hudC != null)
        {
            var so = new SerializedObject(hudC);
            so.FindProperty("objectiveLabel").stringValue = "Thẻ từ";
            so.FindProperty("objectiveCompleteHint").stringValue = "Cửa Boss đã mở!";
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  HUD objectiveLabel = 'Thẻ từ'");
        }

        var tracker = hud.GetComponentInChildren<ObjectiveTracker>(true);
        if (tracker != null)
        {
            var so = new SerializedObject(tracker);
            so.FindProperty("missionTitle").stringValue = "Nhiệm Vụ: Phá Nhà Máy";
            var objectives = so.FindProperty("objectives");
            var rows = new (string label, int goal)[]
            {
                ("Thu thập Thẻ Từ", 0),
                ("Mở cửa khu trung tâm", 1),
                ("Phá hủy Mega-Smog", 2),
                ("Tìm thảo dược", 3),
                ("Lấy thuốc giải từ Ông Sáu", 4),
            };
            objectives.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var e = objectives.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("label").stringValue = rows[i].label;
                e.FindPropertyRelative("goal").intValue = rows[i].goal;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  ObjectiveTracker: 5 objectives, 'Nhiệm Vụ: Phá Nhà Máy'");
        }

        var end = hud.GetComponentInChildren<EndScreenController>(true);
        if (end != null)
        {
            var so = new SerializedObject(end);
            // Beating the factory boss is the end of the game, so the win screen rolls
            // the outro rather than returning to the level select.
            so.FindProperty("completeScene").stringValue = "Ending_Story";
            so.FindProperty("completeDelay").floatValue = 1.4f;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("  EndScreenController.completeScene = Ending_Story");
        }
    }

    // --- B5 dressing ------------------------------------------------------------------------

    static readonly List<(Vector2 centre, Vector2 size)> walls = new();
    static readonly List<(Vector2 centre, Vector2 size)> floors = new();
    static readonly List<Vector2> occupied = new();

    /// <summary>
    /// Machinery pushed up against the maze walls: pipes, hoppers, screens, crates. Decoration
    /// only — nothing here has a collider, so the corridors the player and the NavMesh see are
    /// exactly the ones the tilemap authored.
    /// </summary>
    static void Dress()
    {
        if (walls.Count == 0) return;
        var parent = new GameObject("Dressing").transform;
        parent.SetParent(envRoot, false);

        string[] kit =
        {
            ArtKit.Factory + "pipe-large-long.fbx", ArtKit.Factory + "machine.fbx",
            ArtKit.Factory + "machine-window.fbx", ArtKit.Factory + "box-large.fbx",
            ArtKit.Factory + "box-small.fbx", ArtKit.Factory + "hopper-round.fbx",
            ArtKit.Factory + "screen-wide.fbx", ArtKit.Factory + "warning-orange.fbx",
            ArtKit.Factory + "cone.fbx", ArtKit.Factory + "conveyor.fbx",
        };

        var rng = new System.Random(20260812);
        int placed = 0;
        // Most candidate points land inside one of the 23 merged wall rectangles or too close
        // to something the player needs, so the accept rate is low by design — spend attempts.
        for (int attempt = 0; attempt < 6000 && placed < 130; attempt++)
        {
            var (centre, size) = walls[rng.Next(walls.Count)];

            // A random point just off one of the four faces, facing away from the wall.
            float halfX = size.x * 0.5f, halfZ = size.y * 0.5f;
            const float standoff = 0.85f;
            int face = rng.Next(4);
            Vector2 p; float rotY;
            switch (face)
            {
                case 0: p = centre + new Vector2(Lerp(rng, -halfX, halfX), halfZ + standoff); rotY = 180f; break;
                case 1: p = centre + new Vector2(Lerp(rng, -halfX, halfX), -halfZ - standoff); rotY = 0f; break;
                case 2: p = centre + new Vector2(halfX + standoff, Lerp(rng, -halfZ, halfZ)); rotY = 270f; break;
                default: p = centre + new Vector2(-halfX - standoff, Lerp(rng, -halfZ, halfZ)); rotY = 90f; break;
            }

            if (!OnFloor(p) || InsideWall(p, 0.6f) || NearGameplay(p, 2.4f)) continue;

            var holder = new GameObject("FactoryProp_" + placed);
            holder.transform.SetParent(parent, false);
            holder.transform.position = new Vector3(p.x, 0f, p.y);
            if (ArtKit.Spawn(kit[rng.Next(kit.Length)], holder.transform, 0f, rotY) == null)
            {
                Object.DestroyImmediate(holder);
                continue;
            }
            GameObjectUtility.SetStaticEditorFlags(holder, StaticEditorFlags.BatchingStatic);
            occupied.Add(p);
            placed++;
        }
        log.AppendLine("  dressing: " + placed + " factory props against the walls");
    }

    static float Lerp(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

    static bool OnFloor(Vector2 p)
    {
        foreach (var (c, s) in floors)
            if (Mathf.Abs(p.x - c.x) <= s.x * 0.5f && Mathf.Abs(p.y - c.y) <= s.y * 0.5f) return true;
        return false;
    }

    static bool InsideWall(Vector2 p, float margin)
    {
        foreach (var (c, s) in walls)
            if (Mathf.Abs(p.x - c.x) <= s.x * 0.5f + margin &&
                Mathf.Abs(p.y - c.y) <= s.y * 0.5f + margin) return true;
        return false;
    }

    static bool NearGameplay(Vector2 p, float radius)
    {
        foreach (var q in occupied)
            if ((p - q).sqrMagnitude < radius * radius) return true;
        return false;
    }

    static void SetupLighting()
    {
        // B5 owns the look now — cold, dark, high-contrast so the lasers and screens are the
        // brightest things on screen.
        log.AppendLine("  " + SceneLook.Apply(SceneLook.Look.Factory, "Assets/_Scenes/Level2_FactoryMaze"));
    }

    static int BakeNavMesh()
    {
        var go = new GameObject("NavMesh");
        go.transform.SetParent(envRoot, false);
        var surface = go.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
        if (surface.navMeshData == null) return 0;

        // Same reason as Level 1: an in-memory bake would be embedded in the .unity file
        // as a binary blob and defeat Smart Merge. Persist it beside the scene.
        const string dir = "Assets/_Scenes/Level2_FactoryMaze";
        Directory.CreateDirectory(dir);
        const string assetPath = dir + "/NavMesh-Level2.asset";
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
        AssetDatabase.SaveAssets();
        log.AppendLine("  navmesh baked to " + assetPath);
        return 1;
    }

    static void AddToBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == ScenePath)) return;
        // GameManager.NextLevel is buildIndex + 1, so Level 2 slots in right after Level 1.
        int at = list.FindIndex(s => s.path.EndsWith("Level1_BarrenFarm.unity"));
        list.Insert(at >= 0 ? at + 1 : list.Count, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        for (int i = 0; i < list.Count; i++) log.AppendLine("  build [" + i + "] " + list[i].path);
    }

    // --- helpers ----------------------------------------------------------------------------

    static float P(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    static GameObject Spawn(string dir, string prefabName, Transform parent, Vector3 pos, float rotY = 0f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dir + prefabName + ".prefab");
        if (prefab == null) { log.AppendLine("  MISSING PREFAB " + prefabName); return new GameObject(prefabName); }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        if (!Mathf.Approximately(rotY, 0f)) go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        occupied.Add(new Vector2(pos.x, pos.z));   // B5 dressing keeps clear of these
        return go;
    }

    static void Slab(string path, string name, Transform parent, Vector3 pos, Vector3 scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { log.AppendLine("  MISSING PREFAB " + path); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = $"{name}_{parent.childCount:000}";
        go.transform.position = pos;
        go.transform.localScale = scale;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
    }

    static GameObject Inst(string path, string name, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { log.AppendLine("  MISSING " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        return go;
    }

    static void SetLines(SerializedProperty array, (string speaker, string text)[] lines)
    {
        array.arraySize = lines.Length;
        for (int i = 0; i < lines.Length; i++)
        {
            var e = array.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("speaker").stringValue = lines[i].speaker;
            e.FindPropertyRelative("text").stringValue = lines[i].text;
            e.FindPropertyRelative("portrait").objectReferenceValue = null;
        }
    }
}
