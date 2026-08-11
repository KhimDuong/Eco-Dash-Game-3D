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

        int placed = PlaceFromLayout();
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
                    break;
                case "wall":
                    Slab(Kit + "Greybox_FactoryWall.prefab", "Wall", envRoot,
                         new Vector3(x, WallHeight * 0.5f, z), new Vector3(a, WallHeight, b));
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
                // The Mega-Smog is C3's. Leave its spot marked so the boss arena reads
                // correctly and C3 has an anchor to drop the prefab onto.
                case "boss": Marker("BossSpawn_MegaSmog", pos); break;
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

    static void SetupLighting()
    {
        var sun = Object.FindFirstObjectByType<UnityEngine.Light>();
        if (sun == null)
        {
            var go = new GameObject("Directional Light");
            sun = go.AddComponent<UnityEngine.Light>();
            sun.type = LightType.Directional;
        }
        sun.transform.SetPositionAndRotation(new Vector3(0f, 12f, 0f), Quaternion.Euler(58f, 20f, 0f));
        sun.color = new Color(0.78f, 0.80f, 0.92f);      // cold factory strip-lighting
        sun.intensity = 0.85f;
        sun.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.30f, 0.32f, 0.40f);
        RenderSettings.ambientEquatorColor = new Color(0.24f, 0.25f, 0.30f);
        RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.15f);
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
