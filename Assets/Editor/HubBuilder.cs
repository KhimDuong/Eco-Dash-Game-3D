using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// B4: builds the hub — <c>Assets/_Scenes/Shop_RecyclingStation.unity</c> — and the
/// prefabs it needs, from the 2D hub's own layout.
///
/// <para>The 2D hub is small and hand-placed rather than tilemapped for its props, so
/// unlike Levels 1 and 2 there is no CSV: the six placements are written out below at
/// their 2D coordinates (2D <c>(x, y)</c> → 3D <c>(x, z)</c>, 1 tile = 1 m). Its ground
/// tilemap spans cells x −9..8, y −7..6 with the Grid at the origin, which is an
/// <b>18 × 14 m</b> room.</para>
///
/// <para>Two deliberate corrections to the 2D scene, both the same kind B1 made when it
/// replaced the single <c>TEST_LoreNote</c> with the valley's four real notes:</para>
/// <list type="bullet">
/// <item>The 2D hub had <b>no walls</b> — the ground tilemap just stopped and Greenie
/// could walk into the void. The hub gets a boundary.</item>
/// <item>Ông Bear's <c>bear_recycle</c> side quest was test-placed in <b>Level 1</b> as
/// "Ông Bear (TEST)" wanting 2 scrap + 2 bottles. <see cref="QuestCatalog"/> — the
/// authority — puts it in the hub ("Trạm Trung Tâm") at <b>10 and 10</b>. It lives here
/// now, on its own recycling counter beside him, because
/// <see cref="PlayerInteractor"/> resolves one IInteractable per collider and Ông Bear's
/// own is the shop.</item>
/// </list>
///
/// Menu: <b>Eco-Dash → Rebuild the hub</b>. Idempotent.
/// </summary>
public static class HubBuilder
{
    const string ScenePath = "Assets/_Scenes/Shop_RecyclingStation.unity";
    const string Dir = "Assets/Prefabs/Hub/";
    const string MatDir = "Assets/Models/Materials/";

    // The 2D hub's ground tilemap, in metres.
    const float HalfX = 9f, HalfZ = 7f, WallHeight = 3f;

    static StringBuilderLog log;
    static Transform envRoot, playRoot;

    [MenuItem("Eco-Dash/Rebuild the hub")]
    public static void Rebuild() => Debug.Log(Execute());

    public static string Execute()
    {
        log = new StringBuilderLog();
        System.IO.Directory.CreateDirectory(Dir);

        BuildShopUI();
        BuildMrBear();
        BuildRecyclingCounter();
        BuildCraftingBench();
        BuildStagePortal();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // These five prefabs were just rebuilt from primitives, which throws B5's art away —
        // and C5's clips with it. Put both back before the scene instantiates them.
        log.Line(ArtPass.ReapplyHub().TrimEnd());
        log.Line(AudioPass.ReapplyHub().TrimEnd());
        SolidifyInteractables();

        BuildScene();
        return log.ToString();
    }

    // --- the scene ----------------------------------------------------------------------

    static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Main Camera") Object.DestroyImmediate(go);

        envRoot = new GameObject("Environment").transform;
        playRoot = new GameObject("Gameplay").transform;

        Floor();
        Walls();
        TerrainKit.Surround(envRoot, log.Builder, HalfX, HalfZ, 20260821);
        Yard();

        // The 2D hub's own placements, (x, y) -> (x, z).
        Place("MrBear", new Vector3(0f, 0f, 2.4f), "MrBear");
        Place("RecyclingCounter", new Vector3(2.2f, 0f, 2.4f), "RecyclingCounter");
        Place("CraftingBench", new Vector3(0f, 0f, -2.7f), "Hub_CraftingBench");
        var toL1 = Place("StagePortal", new Vector3(2.5f, 0f, -4f), "Hub_Portal_To_Stage1");
        var toL2 = Place("StagePortal", new Vector3(-2.5f, 0f, -4f), "Hub_Portal_To_Stage2");

        ConfigurePortal(toL1, "Level1_BarrenFarm", 0, "");
        // Stage 2 is the broken gate: it needs Portal Shards to power up (M9/K7).
        ConfigurePortal(toL2, "Level2_FactoryMaze", 1, "portal_stage2_powered");

        Inst("Assets/Prefabs/Player.prefab", "Greenie", new Vector3(0f, 0.7f, -4.5f));
        Inst("Assets/Prefabs/CameraRig.prefab", "CameraRig", Vector3.zero);
        var hud = Inst("Assets/Prefabs/HUD.prefab", "HUD", Vector3.zero);
        var shop = Inst(Dir + "ShopUI.prefab", "ShopCanvas", Vector3.zero);
        WireShop(shop);
        ConfigureHud(hud);

        Lighting();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        log.Line("saved " + ScenePath);
    }

    static void Floor()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Factory/Greybox_FactoryFloor.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, envRoot);
        go.name = "Floor";
        go.transform.position = new Vector3(0f, -0.25f, 0f);      // slab top at y = 0
        go.transform.localScale = new Vector3(HalfX * 2f, 0.5f, HalfZ * 2f);
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        log.Line("  floor " + (HalfX * 2f) + " x " + (HalfZ * 2f) + " m");
    }

    // The 2D hub had none — its ground tilemap simply ended and Greenie walked off it.
    static void Walls()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Factory/Greybox_FactoryWall.prefab");
        Wall(prefab, "Wall_West", new Vector3(-HalfX, WallHeight * 0.5f, 0f), new Vector3(1f, WallHeight, HalfZ * 2f + 1f));
        Wall(prefab, "Wall_East", new Vector3(HalfX, WallHeight * 0.5f, 0f), new Vector3(1f, WallHeight, HalfZ * 2f + 1f));
        Wall(prefab, "Wall_North", new Vector3(0f, WallHeight * 0.5f, HalfZ), new Vector3(HalfX * 2f + 1f, WallHeight, 1f));
        Wall(prefab, "Wall_South", new Vector3(0f, WallHeight * 0.5f, -HalfZ), new Vector3(HalfX * 2f + 1f, WallHeight, 1f));
        log.Line("  4 boundary walls (the 2D hub had none)");
    }

    static void Wall(GameObject prefab, string name, Vector3 pos, Vector3 scale)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, envRoot);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
    }

    /// <summary>
    /// Cycle 2: the station yard. The hub was five objects in an 18 × 14 m grey box — the one
    /// place the player returns to between stages, and the emptiest room in the game. This fills
    /// the edges with the recycling work the station is supposedly doing, and with the greenery
    /// the design doc's "Stardew community-centre" note asks for.
    ///
    /// <para>Everything here is dressing spawned straight from the art kits rather than prefab
    /// instances, exactly as Level 1's ground scatter is: no colliders, nothing interactive, so
    /// none of it can get between the player and Ông Bear, the bench or the two portals.</para>
    /// </summary>
    static void Yard()
    {
        var yard = new GameObject("Yard").transform;
        yard.SetParent(envRoot, false);

        // Sorted recycling along the north wall, behind the shop counter.
        //
        // `clamp` is the largest XZ half-extent the prop's collider may take. 0 means "fit the
        // model" — right for a box or a barrel, whose silhouette *is* its footprint. A tree is
        // the opposite: an axis-aligned box around an oak is a box around its canopy, and two of
        // those would swallow ~10 m² of an 18 x 14 m room, so a tree gets its trunk.
        var stock = new (string pack, string model, float height, float x, float z, float rotY, float clamp)[]
        {
            (ArtKit.Survival, "box-large", 1.10f, -7.2f, 5.4f, 15f, 0f),
            (ArtKit.Survival, "box", 0.85f, -5.7f, 5.9f, -25f, 0f),
            (ArtKit.Survival, "box", 0.85f, -6.6f, 3.9f, 40f, 0f),
            (ArtKit.Survival, "barrel", 0.95f, -4.8f, 5.7f, 0f, 0f),
            (ArtKit.Survival, "barrel-open", 0.95f, 5.0f, 5.6f, 0f, 0f),
            (ArtKit.Survival, "box-large", 1.10f, 6.4f, 5.3f, -12f, 0f),
            (ArtKit.Survival, "resource-planks", 0.55f, 7.3f, 4.2f, 70f, 0f),
            (ArtKit.Factory, "pipe-large-long", 1.00f, -8.0f, 0.5f, 90f, 0f),
            (ArtKit.Factory, "pipe-large-long", 1.00f, -8.0f, -1.5f, 90f, 0f),
            (ArtKit.Factory, "screen-flat", 1.20f, 8.0f, 1.4f, -90f, 0f),
            (ArtKit.Town, "cart", 0.90f, -7.4f, -3.2f, 30f, 0f),
            (ArtKit.Town, "stall-red", 2.20f, 7.0f, -1.6f, -90f, 0f),
        };
        int props = 0, solid = 0;
        foreach (var (pack, model, height, x, z, rotY, clamp) in stock)
        {
            var holder = new GameObject(model);
            holder.transform.SetParent(yard, false);
            holder.transform.position = new Vector3(x, 0f, z);
            // The turn goes on the holder so ArtKit.Solidify can fit an oriented box; the prop
            // ends up in the same place either way, because Spawn centres it on the pivot.
            holder.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            var art = ArtKit.Spawn(pack + model + ".fbx", holder.transform, height);
            if (art == null) continue;
            if (ArtKit.Solidify(holder, art, maxHalfExtent: clamp) != null) solid++;
            GameObjectUtility.SetStaticEditorFlags(holder, StaticEditorFlags.BatchingStatic);
            props++;
        }

        // Green things: what the station is for.
        var green = new (string model, float height, float x, float z, float clamp)[]
        {
            ("tree_oak", 3.40f, -7.8f, 2.6f, 0.28f),
            ("tree_oak", 3.00f, 7.9f, 3.4f, 0.28f),
            ("plant_bush", 0.70f, -3.2f, 5.9f, 0f),
            ("plant_bush", 0.60f, 3.4f, 5.9f, 0f),
            ("plant_bushLarge", 0.85f, -8.1f, -5.4f, 0f),
            ("plant_bushLarge", 0.85f, 8.1f, -5.4f, 0f),
            ("flower_yellowA", 0.35f, -2.0f, 6.0f, 0f),
            ("flower_redA", 0.35f, 2.2f, 6.0f, 0f),
            ("grass_large", 0.0f, -5.6f, -5.8f, 0f),
            ("grass_large", 0.0f, 5.6f, -5.8f, 0f),
        };
        foreach (var (model, height, x, z, clamp) in green)
        {
            var holder = new GameObject(model);
            holder.transform.SetParent(yard, false);
            holder.transform.position = new Vector3(x, 0f, z);
            var art = ArtKit.Spawn(ArtKit.Nature + model + ".fbx", holder.transform, height);
            if (art == null) continue;
            // The flowers and the grass fall under Solidify's minimum height on their own —
            // walking through a tuft of grass is correct, and stopping dead on one is not.
            if (ArtKit.Solidify(holder, art, maxHalfExtent: clamp) != null) solid++;
            GameObjectUtility.SetStaticEditorFlags(holder, StaticEditorFlags.BatchingStatic);
            props++;
        }

        foreach (var (x, z) in new[] { (-4.2f, -4.6f), (4.2f, -4.6f), (0f, 5.8f) })
        {
            var holder = new GameObject("Lantern");
            holder.transform.SetParent(yard, false);
            holder.transform.position = new Vector3(x, 0f, z);
            var post = ArtKit.Spawn(ArtKit.Town + "lantern.fbx", holder.transform, 2.6f);
            if (ArtKit.Solidify(holder, post, maxHalfExtent: 0.3f) != null) solid++;
            props++;
        }

        log.Line("  yard: " + props + " dressing props, " + solid + " of them solid");
    }

    /// <summary>
    /// C4: give Ông Bear, the recycling counter and the crafting bench a body.
    ///
    /// <para>All three were built by <see cref="Interactable"/>, which grants exactly one
    /// <see cref="SphereCollider"/> and marks it a trigger. That is the interaction range, not a
    /// shape — so Greenie walked <i>into</i> the shopkeeper and vanished inside him, with the
    /// "Nhấn E" prompt floating over the pair of them.</para>
    ///
    /// <para>The trigger is the gameplay contract and is left exactly as it was (CLAUDE.md
    /// rule 2). The body is a separate <c>Solid</c> child on the Obstacle layer, carrying no
    /// <see cref="IInteractable"/>, so <see cref="PlayerInteractor"/> still resolves the root's
    /// trigger and nothing about the E-prompt changes. It runs <b>after</b> the art pass because
    /// the box is fitted to the model the art pass just put on, and its XZ half-extent is capped
    /// well inside the trigger radius so the player can always still reach the prompt: contact
    /// lands at <c>clamp + 0.35</c> (Greenie's own body radius) from the centre.</para>
    /// </summary>
    static void SolidifyInteractables()
    {
        foreach (var (prefab, clamp, trigger) in new[]
        {
            ("MrBear", 0.55f, 1.1f),
            ("RecyclingCounter", 0.45f, 1.0f),
            ("CraftingBench", 0.45f, 1.0f),
        })
        {
            string path = Dir + prefab + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { log.Line("  MISSING " + path); continue; }
            try
            {
                var solid = new GameObject("Solid");
                solid.transform.SetParent(root.transform, false);
                var box = ArtKit.Solidify(solid, root, maxHalfExtent: clamp);
                if (box == null)
                {
                    Object.DestroyImmediate(solid);
                    log.Line("  " + prefab + ": nothing to solidify");
                    continue;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                float reach = Mathf.Max(box.size.x, box.size.z) * 0.5f + 0.35f;
                log.Line("  " + prefab + ": solid " + box.size.x.ToString("F2") + " x " +
                         box.size.z.ToString("F2") + " m body (player reaches " +
                         reach.ToString("F2") + " m, trigger " + trigger.ToString("F2") + " m)");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    static GameObject Place(string prefabName, Vector3 pos, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + prefabName + ".prefab");
        if (prefab == null) { log.Line("  MISSING " + prefabName); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, playRoot);
        go.name = name;
        go.transform.position = pos;
        return go;
    }

    static void ConfigurePortal(GameObject go, string target, int shardCost, string flag)
    {
        if (go == null) return;
        var so = new SerializedObject(go.GetComponent<StagePortal>());
        so.FindProperty("targetScene").stringValue = target;
        so.FindProperty("shardCost").intValue = shardCost;
        so.FindProperty("poweredFlag").stringValue = flag;
        // Gated gates must be E-interact so shards are never spent by walking past.
        so.FindProperty("walkOver").boolValue = shardCost <= 0;
        so.ApplyModifiedPropertiesWithoutUndo();
        log.Line("  " + go.name + " -> " + target + (shardCost > 0 ? " (needs " + shardCost + " shard)" : ""));
    }

    static void WireShop(GameObject shopCanvas)
    {
        if (shopCanvas == null) return;
        var bear = GameObject.Find("MrBear");
        if (bear == null) return;
        var so = new SerializedObject(bear.GetComponent<ShopNPC>());
        so.FindProperty("shop").objectReferenceValue = shopCanvas.GetComponent<ShopController>();
        so.ApplyModifiedPropertiesWithoutUndo();
        log.Line("  MrBear -> ShopController");
    }

    static void ConfigureHud(GameObject hud)
    {
        if (hud == null) return;
        var tracker = hud.GetComponentInChildren<ObjectiveTracker>(true);
        if (tracker == null) return;
        var so = new SerializedObject(tracker);
        so.FindProperty("missionTitle").stringValue = "Trạm Tái Chế";
        // The hub has no cores to collect; the panel just carries the M8 quest chain.
        var objectives = so.FindProperty("objectives");
        var rows = new (string label, int goal)[]
        {
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
        log.Line("  HUD objectives -> hub (quest chain only)");
    }

    static void Lighting()
    {
        // B5 owns the look now — bright and even, the one safe room in the game.
        log.Line("  " + SceneLook.Apply(SceneLook.Look.Hub, "Assets/_Scenes/Shop_RecyclingStation"));
    }

    // --- prefabs -------------------------------------------------------------------------

    static void BuildMrBear()
    {
        var fur = Mat("Greybox_Bear", new Color(0.45f, 0.32f, 0.22f), 0.25f);
        var root = Interactable("MrBear", 1.1f);
        Part(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.75f, 0f),
             Vector3.zero, new Vector3(0.8f, 0.75f, 0.8f), fur);
        Part(root.transform, "Ear_L", PrimitiveType.Sphere, new Vector3(-0.26f, 1.42f, 0f),
             Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), fur);
        Part(root.transform, "Ear_R", PrimitiveType.Sphere, new Vector3(0.26f, 1.42f, 0f),
             Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), fur);
        Prompt(root.transform, "Nhấn E", 2.1f);

        var npc = root.AddComponent<ShopNPC>();
        var so = new SerializedObject(npc);
        so.FindProperty("prompt").objectReferenceValue = root.transform.Find("Prompt").gameObject;
        SetLines(so.FindProperty("greetingLines"), new[]
        {
            ("Ông Bear", "Chào cậu bé máy! Mang rác tới đây, ta đổi cho cậu đồ nâng cấp."),
        });
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(root, "MrBear");
    }

    static void BuildRecyclingCounter()
    {
        var wood = Mat("Greybox_Counter", new Color(0.50f, 0.38f, 0.26f), 0.3f);
        var bin = Mat("Greybox_RecycleBin", new Color(0.25f, 0.55f, 0.35f), 0.4f);
        var root = Interactable("RecyclingCounter", 1.0f);
        Part(root.transform, "Counter", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f),
             Vector3.zero, new Vector3(1.4f, 0.9f, 0.7f), wood);
        Part(root.transform, "Bin", PrimitiveType.Cylinder, new Vector3(0.5f, 1.15f, 0f),
             Vector3.zero, new Vector3(0.45f, 0.25f, 0.45f), bin);
        Prompt(root.transform, "Nhấn E", 1.8f);

        var npc = root.AddComponent<SideQuestNPC>();
        var so = new SerializedObject(npc);
        so.FindProperty("questId").stringValue = QuestCatalog.BearRecycle;
        so.FindProperty("completion").enumValueIndex = 1;    // TurnInItems
        SetStrings(so.FindProperty("turnInIds"), new[] { "scrap", "bottle" });
        SetInts(so.FindProperty("turnInCounts"), new[] { 10, 10 });   // QuestCatalog says 10 + 10
        so.FindProperty("prompt").objectReferenceValue = root.transform.Find("Prompt").gameObject;
        SetLines(so.FindProperty("offerLines"), new[]
        {
            ("Ông Bear", "Mang cho ta 10 Mảnh Kim Loại và 10 Chai Nhựa nhé! Ta sẽ mở khóa công thức chế tạo nâng cao."),
        });
        SetLines(so.FindProperty("inProgressLines"), new[]
        {
            ("Ông Bear", "Chưa đủ đồ rồi, cố lên cháu!"),
        });
        SetLines(so.FindProperty("readyLines"), new[]
        {
            ("Ông Bear", "Tuyệt! Ta mở khóa công thức nâng cao cho cháu."),
        });
        SetLines(so.FindProperty("doneLines"), new[]
        {
            ("Ông Bear", "Cảm ơn cháu, thợ tái chế cừ khôi!"),
        });
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(root, "RecyclingCounter");
    }

    static void BuildCraftingBench()
    {
        var wood = Mat("Greybox_Counter", new Color(0.50f, 0.38f, 0.26f), 0.3f);
        var tool = Mat("Greybox_BenchTool", new Color(0.62f, 0.64f, 0.68f), 0.7f);
        var root = Interactable("CraftingBench", 1.0f);
        Part(root.transform, "Top", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f),
             Vector3.zero, new Vector3(1.8f, 0.16f, 0.9f), wood);
        Part(root.transform, "Leg_L", PrimitiveType.Cube, new Vector3(-0.75f, 0.4f, 0f),
             Vector3.zero, new Vector3(0.16f, 0.8f, 0.8f), wood);
        Part(root.transform, "Leg_R", PrimitiveType.Cube, new Vector3(0.75f, 0.4f, 0f),
             Vector3.zero, new Vector3(0.16f, 0.8f, 0.8f), wood);
        Part(root.transform, "Vice", PrimitiveType.Cube, new Vector3(0.55f, 1.02f, 0.1f),
             new Vector3(0f, 20f, 0f), new Vector3(0.3f, 0.2f, 0.25f), tool);
        Prompt(root.transform, "Nhấn E", 1.7f);

        var bench = root.AddComponent<CraftingBench>();
        var so = new SerializedObject(bench);
        so.FindProperty("prompt").objectReferenceValue = root.transform.Find("Prompt").gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(root, "CraftingBench");
    }

    static void BuildStagePortal()
    {
        var frame = Mat("Greybox_PortalFrame", new Color(0.34f, 0.35f, 0.40f), 0.6f);
        var openMat = Mat("Greybox_PortalOpen", new Color(0.45f, 1f, 0.75f), 0.85f, new Color(0.2f, 0.7f, 0.5f));
        var lockedMat = Mat("Greybox_PortalLocked", new Color(0.35f, 0.36f, 0.40f), 0.4f);

        var root = Interactable("StagePortal", 1.2f);
        Part(root.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f),
             Vector3.zero, new Vector3(2.2f, 0.05f, 2.2f), frame);
        Part(root.transform, "Arch_L", PrimitiveType.Cube, new Vector3(-0.95f, 1.2f, 0f),
             Vector3.zero, new Vector3(0.22f, 2.4f, 0.3f), frame);
        Part(root.transform, "Arch_R", PrimitiveType.Cube, new Vector3(0.95f, 1.2f, 0f),
             Vector3.zero, new Vector3(0.22f, 2.4f, 0.3f), frame);

        var open = new GameObject("Visual_Open");
        open.transform.SetParent(root.transform, false);
        Part(open.transform, "Sheet", PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f),
             Vector3.zero, new Vector3(1.7f, 2.2f, 0.1f), openMat);

        var locked = new GameObject("Visual_Locked");
        locked.transform.SetParent(root.transform, false);
        Part(locked.transform, "Sheet", PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f),
             Vector3.zero, new Vector3(1.7f, 2.2f, 0.1f), lockedMat);
        locked.SetActive(false);

        Prompt(root.transform, "Nhấn E", 2.6f);

        var portal = root.AddComponent<StagePortal>();
        var so = new SerializedObject(portal);
        so.FindProperty("prompt").objectReferenceValue = root.transform.Find("Prompt").gameObject;
        so.FindProperty("openVisual").objectReferenceValue = open;
        so.FindProperty("lockedVisual").objectReferenceValue = locked;
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(root, "StagePortal");
    }

    // --- the shop window -------------------------------------------------------------------

    // ShopController is Tier-0 and does NOT self-build (unlike CraftingUI), so its window
    // has to exist. Built here in UIFactory's visual language so the two match.
    static void BuildShopUI()
    {
        var root = new GameObject("ShopCanvas");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<AudioSource>().playOnAwake = false;

        // Always-visible "back to menu" button, as in the 2D hub. It sits BELOW the HUD's
        // left column (HP bar ends at y -84, "Rác" at -194) — at the old y -44 it was drawn
        // straight over the HP bar (QA E6).
        var back = Button(root.transform, "BackButton", "← Menu", new Vector2(0f, 1f),
                          new Vector2(120f, 56f), new Vector2(90f, -232f));

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        panel.GetComponent<Image>().color = UIFactory.PanelColor;
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(820f, 560f);
        prt.anchoredPosition = Vector2.zero;

        var title = UIFactory.Text("Title", panel.transform, "TRẠM TÁI CHẾ — ÔNG BEAR", 40f);
        Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(720f, 60f), new Vector2(0f, -48f));
        title.color = UIFactory.AccentColor;

        var trash = UIFactory.Text("TrashText", panel.transform, "Rác: 0", 32f);
        Anchor(trash.rectTransform, new Vector2(0.5f, 1f), new Vector2(720f, 44f), new Vector2(0f, -104f));

        var close = Button(panel.transform, "CloseButton", "×", new Vector2(1f, 1f),
                           new Vector2(56f, 56f), new Vector2(-46f, -40f));

        var rows = new List<ShopUpgradeRow>();
        var defs = new (int upgrade, string name)[]
        {
            (0, "Máu tối đa (+2 HP)"),
            (1, "Tốc độ (+12%)"),
            (2, "Bắn tỏa (nhiều hạt)"),
        };
        for (int i = 0; i < defs.Length; i++)
            rows.Add(Row(panel.transform, "Row" + i, defs[i].upgrade, defs[i].name, -180f - i * 110f));

        var shop = root.AddComponent<ShopController>();
        var so = new SerializedObject(shop);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("trashText").objectReferenceValue = trash;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("backButton").objectReferenceValue = back;
        var arr = so.FindProperty("rows");
        arr.arraySize = rows.Count;
        for (int i = 0; i < rows.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        Save(root, "ShopUI");
    }

    static ShopUpgradeRow Row(Transform parent, string name, int upgrade, string display, float y)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = UIFactory.SlotColor;
        Anchor(row.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(720f, 92f), new Vector2(0f, y));

        var nameText = UIFactory.Text("Name", row.transform, display, 28f, TextAlignmentOptions.Left);
        Anchor(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(340f, 40f), new Vector2(190f, 16f));

        var tierText = UIFactory.Text("Tier", row.transform, "Cấp 0/3", 22f, TextAlignmentOptions.Left);
        Anchor(tierText.rectTransform, new Vector2(0f, 0.5f), new Vector2(340f, 32f), new Vector2(190f, -18f));

        var costText = UIFactory.Text("Cost", row.transform, "10 rác", 26f, TextAlignmentOptions.Right);
        // Ends 20px clear of the MUA button's left edge (-160); the old 200-wide box at
        // -230 reached -130 and the word "rác" disappeared under the button (QA E7).
        Anchor(costText.rectTransform, new Vector2(1f, 0.5f), new Vector2(180f, 40f), new Vector2(-270f, 0f));

        var buy = Button(row.transform, "Buy", "MUA", new Vector2(1f, 0.5f), new Vector2(140f, 56f), new Vector2(-90f, 0f));

        var comp = row.AddComponent<ShopUpgradeRow>();
        var so = new SerializedObject(comp);
        so.FindProperty("upgrade").enumValueIndex = upgrade;
        so.FindProperty("displayName").stringValue = display;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("tierText").objectReferenceValue = tierText;
        so.FindProperty("costText").objectReferenceValue = costText;
        so.FindProperty("buyButton").objectReferenceValue = buy;
        so.ApplyModifiedPropertiesWithoutUndo();
        return comp;
    }

    static Button Button(Transform parent, string name, string label, Vector2 anchor,
                         Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = UIFactory.AccentColor;
        Anchor(go.GetComponent<RectTransform>(), anchor, size, pos);
        var text = UIFactory.Text("Label", go.transform, label, 26f);
        UIFactory.Fill(text.rectTransform);
        text.color = Color.black;
        return go.GetComponent<Button>();
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    // --- shared -------------------------------------------------------------------------------

    static GameObject Interactable(string name, float radius)
    {
        var root = new GameObject(name);
        root.layer = LayerMask.NameToLayer("Trigger");
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0f, 0.6f, 0f);
        col.radius = radius;
        return root;
    }

    static void Prompt(Transform parent, string text, float height)
    {
        var go = new GameObject("Prompt");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, height, 0f);
        go.transform.localScale = Vector3.one * 0.01f;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        go.AddComponent<Billboard>();

        var label = new GameObject("Label");
        label.transform.SetParent(go.transform, false);
        var tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(400f, 80f);
        go.SetActive(false);
    }

    static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 pos,
                           Vector3 euler, Vector3 scale, Material mat)
    {
        var p = GameObject.CreatePrimitive(type);
        p.name = name;
        p.transform.SetParent(parent, false);
        p.transform.localPosition = pos;
        p.transform.localRotation = Quaternion.Euler(euler);
        p.transform.localScale = scale;
        Object.DestroyImmediate(p.GetComponent<Collider>());
        p.GetComponent<Renderer>().sharedMaterial = mat;
        return p;
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

    static void SetStrings(SerializedProperty array, string[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    static void SetInts(SerializedProperty array, int[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).intValue = values[i];
    }

    static Material Mat(string name, Color color, float smoothness) => Mat(name, color, smoothness, Color.black);

    static Material Mat(string name, Color color, float smoothness, Color emission)
    {
        string path = MatDir + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        bool glows = emission.maxColorComponent > 0f;
        if (glows) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
        mat.globalIlluminationFlags = glows
            ? MaterialGlobalIlluminationFlags.RealtimeEmissive
            : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        mat.SetColor("_EmissionColor", emission);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static GameObject Inst(string path, string name, Vector3 pos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { log.Line("  MISSING " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.position = pos;
        return go;
    }

    static void Save(GameObject root, string name)
    {
        string path = Dir + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        log.Line("built " + path);
    }

    static void AddToBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == ScenePath)) return;
        list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        for (int i = 0; i < list.Count; i++) log.Line("  build [" + i + "] " + list[i].path);
    }

    class StringBuilderLog
    {
        readonly System.Text.StringBuilder sb = new();
        public System.Text.StringBuilder Builder => sb;
        public void Line(string s) => sb.AppendLine(s);
        public override string ToString() => sb.ToString();
    }
}
